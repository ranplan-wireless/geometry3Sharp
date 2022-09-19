using System;
using System.IO;
using System.Threading;

namespace g3
{
    public class OBJParser : MeshIOLogger
    {
        private const int CancellationCheckParseStep = 200;

        private static readonly string[] splitDoubleSlash = new string[] { "//" };
        private static readonly char[] splitSlash = new char[] { '/' };

        protected DVector<double> VertexPositions { get; private set; }
        protected DVector<float> VertexNormals { get; private set; }
        protected DVector<float> VertexUVs { get; private set; }
        protected DVector<float> VertexColors { get; private set; }
        protected DVector<Triangle> Triangles { get; private set; }

        protected Tokens MatFileTokens { get; private set; }
        protected Tokens ObjectTokens { get; private set; }
        protected Tokens GroupTokens { get; private set; }
        protected Tokens MaterialTokens { get; private set; }

        bool m_bOBJHasPerVertexColors;
        int m_nUVComponents;

        private bool m_bOBJHasTriangleGroups;
        private int m_nSetInvalidGroupsTo;

        public bool HasPerVertexColors
        {
            get { return m_bOBJHasPerVertexColors; }
        }
        public int UVDimension
        {
            get { return m_nUVComponents; }
        }

        public bool HasTriangleGroups => m_bOBJHasTriangleGroups;
        public int SetInvalidGroupsTo => m_nSetInvalidGroupsTo;

        // if this is true, means during parsing we found vertices of faces that
        //  had different indices for vtx/normal/uv
        public bool HasComplexVertices { get; set; }

        public IOReadResult ParseInput(TextReader reader, CancellationToken cancellationToken)
        {
            VertexPositions = new DVector<double>();
            VertexNormals = new DVector<float>();
            VertexUVs = new DVector<float>();
            VertexColors = new DVector<float>();
            Triangles = new DVector<Triangle>();

            MatFileTokens = new Tokens(Triangle.InvalidMatFileID);
            ObjectTokens = new Tokens(Triangle.InvalidObjectID);
            GroupTokens = new Tokens(Triangle.InvalidGroupID);
            MaterialTokens = new Tokens(Triangle.InvalidMaterialID);

            var bVerticesHaveColors = false;
            var nMaxUVLength = 0;
            HasComplexVertices = false;

            var nLines = 0;
            while (reader.Peek() >= 0)
            {
                var line = reader.ReadLine();
                nLines++;

                if (cancellationToken.IsCancellationRequested && nLines % CancellationCheckParseStep == 0)
                    return new IOReadResult(IOCode.Cancelled, "Cancelled by manual when parsing");

                var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                // [RMS] this will hang VS on large models...
                //if (nWarningLevel >= 2)
                //    emit_warning("Parsing line " + line);
                try
                {
                    if (tokens[0][0] == 'v')
                    {
                        if (tokens[0].Length == 1)
                        {
                            if (tokens.Length == 7)
                            {
                                VertexPositions.Add(Double.Parse(tokens[1]));
                                VertexPositions.Add(Double.Parse(tokens[2]));
                                VertexPositions.Add(Double.Parse(tokens[3]));

                                VertexColors.Add(float.Parse(tokens[4]));
                                VertexColors.Add(float.Parse(tokens[5]));
                                VertexColors.Add(float.Parse(tokens[6]));
                                bVerticesHaveColors = true;
                            }
                            else if (tokens.Length >= 4)
                            {
                                VertexPositions.Add(Double.Parse(tokens[1]));
                                VertexPositions.Add(Double.Parse(tokens[2]));
                                VertexPositions.Add(Double.Parse(tokens[3]));
                            }

                            if (tokens.Length != 4 && tokens.Length != 7)
                                emit_warning("[OBJReader] vertex has unknown format: " + line);
                        }
                        else if (tokens[0][1] == 'n')
                        {
                            if (tokens.Length >= 4)
                            {
                                VertexNormals.Add(float.Parse(tokens[1]));
                                VertexNormals.Add(float.Parse(tokens[2]));
                                VertexNormals.Add(float.Parse(tokens[3]));
                            }

                            if (tokens.Length != 4)
                                emit_warning("[OBJReader] normal has more than 3 coordinates: " + line);
                        }
                        else if (tokens[0][1] == 't')
                        {
                            if (tokens.Length >= 3)
                            {
                                VertexUVs.Add(float.Parse(tokens[1]));
                                VertexUVs.Add(float.Parse(tokens[2]));
                                nMaxUVLength = Math.Max(nMaxUVLength, tokens.Length);
                            }

                            if (tokens.Length != 3)
                                emit_warning("[OBJReader] UV has unknown format: " + line);
                        }
                    }
                    else if (tokens[0][0] == 'f')
                    {
                        if (tokens.Length < 4)
                        {
                            emit_warning("[OBJReader] degenerate face specified : " + line);
                        }
                        else if (tokens.Length == 4)
                        {
                            var tri = new Triangle();
                            parse_triangle(tokens, ref tri);

                            tri.nMatFileID = MatFileTokens.ActiveID;
                            tri.nObjectID = ObjectTokens.ActiveID;
                            tri.nGroupID = GroupTokens.ActiveID;
                            tri.nMaterialID = MaterialTokens.ActiveID;

                            Triangles.Add(tri);
                            if (tri.is_complex())
                                HasComplexVertices = true;
                        }
                        else
                        {
                            append_face(tokens);
                        }
                    }
                    else if (tokens[0][0] == 'g')
                    {
                        GroupTokens.Append(line, tokens);
                    }
                    else if (tokens[0][0] == 'o')
                    {
                        ObjectTokens.Append(line, tokens);
                    }
                    else if (tokens[0] == "mtllib")
                    {
                        MatFileTokens.Append(line, tokens);
                    }
                    else if (tokens[0] == "usemtl")
                    {
                        MaterialTokens.Append(line, tokens);
                    }
                }
                catch (Exception e)
                {
                    emit_warning("error parsing line " + nLines.ToString() + ": " + line + ", exception " + e.Message);
                }
            }

            m_bOBJHasPerVertexColors = bVerticesHaveColors;
            m_bOBJHasTriangleGroups = (GroupTokens.ActiveID != Triangle.InvalidGroupID);
            m_nSetInvalidGroupsTo = GroupTokens.Counter++;
            m_nUVComponents = nMaxUVLength;

            return new IOReadResult(IOCode.Ok, "");
        }

        private int parse_v(string sToken)
        {
            var vi = int.Parse(sToken);
            if (vi < 0)
                vi = (VertexPositions.Length / 3) + vi + 1;
            return vi;
        }

        private int parse_n(string sToken)
        {
            var vi = int.Parse(sToken);
            if (vi < 0)
                vi = (VertexNormals.Length / 3) + vi + 1;
            return vi;
        }

        private int parse_u(string sToken)
        {
            var vi = int.Parse(sToken);
            if (vi < 0)
                vi = (VertexUVs.Length / 2) + vi + 1;
            return vi;
        }

        private void append_face(string[] tokens)
        {
            var nMode = 0;
            if (tokens[1].IndexOf("//") != -1)
                nMode = 1;
            else if (tokens[1].IndexOf('/') != -1)
                nMode = 2;

            var t = new Triangle();
            t.clear();
            for (var ti = 0; ti < tokens.Length - 1; ++ti)
            {
                var j = (ti < 3) ? ti : 2;
                if (ti >= 3)
                    t.move_vertex(2, 1);

                // parse next vertex
                if (nMode == 0)
                {
                    // "f v1 v2 v3"
                    t.set_vertex(j, parse_v(tokens[ti + 1]));
                }
                else if (nMode == 1)
                {
                    // "f v1//vn1 v2//vn2 v3//vn3"
                    var parts = tokens[ti + 1].Split(splitDoubleSlash, StringSplitOptions.RemoveEmptyEntries);
                    t.set_vertex(j, parse_v(parts[0]), parse_n(parts[1]));
                }
                else if (nMode == 2)
                {
                    var parts = tokens[ti + 1].Split(splitSlash, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        // "f v1/vt1 v2/vt2 v3/vt3"
                        t.set_vertex(j, parse_v(parts[0]), -1, parse_u(parts[1]));
                    }
                    else if (parts.Length == 3)
                    {
                        // "f v1/vt1/vn1 v2/vt2/vn2 v3/vt3/vn3"
                        t.set_vertex(j, parse_v(parts[0]), parse_n(parts[2]), parse_u(parts[1]));
                    }
                    else
                    {
                        emit_warning("parse_triangle unexpected face component " + tokens[j]);
                    }
                }

                // do append
                if (ti >= 2)
                {
                    t.nMatFileID = MatFileTokens.ActiveID;
                    t.nObjectID = ObjectTokens.ActiveID;
                    t.nGroupID = GroupTokens.ActiveID;
                    t.nMaterialID = MaterialTokens.ActiveID;
                    Triangles.Add(t);
                    if (t.is_complex())
                        HasComplexVertices = true;
                }
            }
        }

        private void parse_triangle(string[] tokens, ref Triangle t)
        {
            var nMode = 0;
            if (tokens[1].IndexOf("//") != -1)
                nMode = 1;
            else if (tokens[1].IndexOf('/') != -1)
                nMode = 2;

            t.clear();

            for (var j = 0; j < 3; ++j)
            {
                if (nMode == 0)
                {
                    // "f v1 v2 v3"
                    t.set_vertex(j, parse_v(tokens[j + 1]));
                }
                else if (nMode == 1)
                {
                    // "f v1//vn1 v2//vn2 v3//vn3"
                    var parts = tokens[j + 1].Split(splitDoubleSlash, StringSplitOptions.RemoveEmptyEntries);
                    t.set_vertex(j, parse_v(parts[0]), parse_n(parts[1]));
                }
                else if (nMode == 2)
                {
                    var parts = tokens[j + 1].Split(splitSlash, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        // "f v1/vt1 v2/vt2 v3/vt3"
                        t.set_vertex(j, parse_v(parts[0]), -1, parse_u(parts[1]));
                    }
                    else if (parts.Length == 3)
                    {
                        // "f v1/vt1/vn1 v2/vt2/vn2 v3/vt3/vn3"
                        t.set_vertex(j, parse_v(parts[0]), parse_n(parts[2]), parse_u(parts[1]));
                    }
                    else
                    {
                        emit_warning("parse_triangle unexpected face component " + tokens[j]);
                    }
                }
            }
        }
    }
}