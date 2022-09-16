using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace g3
{
    public class OBJReader : OBJParser, IMeshReader
    {
        Dictionary<string, OBJMaterial> Materials;

        public OBJReader()
        {
            MTLFileSearchPaths = new List<string>();
        }

        // you need to initialize this with paths if you want .MTL files to load
        public List<string> MTLFileSearchPaths { get; set; }

        public IOReadResult Read(BinaryReader reader, ReadOptions options, IMeshBuilder builder)
        {
            throw new NotImplementedException();
        }

        public IOReadResult Read(TextReader reader, ReadOptions options, IMeshBuilder builder)
        {
            Materials = new Dictionary<string, OBJMaterial>();
            HasComplexVertices = false;

            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] starting parse obj.");
            var parseResult = ParseInput(reader);
            if (parseResult.code != IOCode.Ok)
                return parseResult;

            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] completed parse obj.");

            if (options.ReadMaterials && MTLFileSearchPaths.Count > 0 && materialTokens.Count > 0)
            {
                if (nWarningLevel >= 1)
                    emit_warning("[OBJReader] starting parse mtl.");

                foreach (var sMTLPathString in matFileTokens.ListName())
                {
                    var sFile = FindMTLFile(sMTLPathString);
                    if (sFile != null)
                    {
                        var result = ReadMaterials(sFile);
                        if (result.code != IOCode.Ok)
                            emit_warning("error parsing " + sFile + " : " + result.message);
                    }
                    else
                        emit_warning("material file " + sMTLPathString + " could not be found in material search paths");
                }

                if (nWarningLevel >= 1)
                    emit_warning("[OBJReader] completed parse mtl.");
            }

            var buildResult =
                (Materials.Count > 1 || HasComplexVertices) ? BuildMeshes_ByMaterial(options, builder) : BuildMeshes_Simple(options, builder);

            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] build complete.");

            if (buildResult.code != IOCode.Ok)
                return buildResult;

            return new IOReadResult(IOCode.Ok, "");
        }

        int append_vertex(IMeshBuilder builder, Index3i vertIdx, bool bHaveNormals, bool bHaveColors, bool bHaveUVs)
        {
            var vi = 3 * vertIdx.a;
            if (vertIdx.a < 0 || vertIdx.a >= vPositions.Length / 3)
            {
                emit_warning("[OBJReader] append_vertex() referencing invalid vertex " + vertIdx.a.ToString());
                return -1;
            }

            if (bHaveNormals == false && bHaveColors == false && bHaveUVs == false)
                return builder.AppendVertex(vPositions[vi], vPositions[vi + 1], vPositions[vi + 2]);

            var vinfo = new NewVertexInfo();
            vinfo.bHaveC = vinfo.bHaveN = vinfo.bHaveUV = false;
            vinfo.v = new Vector3d(vPositions[vi], vPositions[vi + 1], vPositions[vi + 2]);
            if (bHaveNormals)
            {
                vinfo.bHaveN = true;
                var ni = 3 * vertIdx.b;
                vinfo.n = new Vector3f(vNormals[ni], vNormals[ni + 1], vNormals[ni + 2]);
            }

            if (bHaveColors)
            {
                vinfo.bHaveC = true;
                vinfo.c = new Vector3f(vColors[vi], vColors[vi + 1], vColors[vi + 2]);
            }

            if (bHaveUVs)
            {
                vinfo.bHaveUV = true;
                var ui = 2 * vertIdx.c;
                vinfo.uv = new Vector2f(vUVs[ui], vUVs[ui + 1]);
            }

            return builder.AppendVertex(vinfo);
        }

        int append_triangle(IMeshBuilder builder, int nTri, int[] mapV)
        {
            var t = vTriangles[nTri];
            var v0 = mapV[t.vIndices[0] - 1];
            var v1 = mapV[t.vIndices[1] - 1];
            var v2 = mapV[t.vIndices[2] - 1];
            if (v0 == -1 || v1 == -1 || v2 == -1)
            {
                emit_warning(string.Format("[OBJReader] invalid triangle:  {0} {1} {2}  mapped to {3} {4} {5}",
                    t.vIndices[0], t.vIndices[1], t.vIndices[2], v0, v1, v2));
                return -1;
            }

            var gid = (vTriangles[nTri].nGroupID == Triangle.InvalidGroupID) ? m_nSetInvalidGroupsTo : vTriangles[nTri].nGroupID;
            return builder.AppendTriangle(v0, v1, v2, gid);
        }

        int append_triangle(IMeshBuilder builder, Triangle t)
        {
            if (t.vIndices[0] < 0 || t.vIndices[1] < 0 || t.vIndices[2] < 0)
            {
                emit_warning(string.Format("[OBJReader] invalid triangle:  {0} {1} {2}",
                    t.vIndices[0], t.vIndices[1], t.vIndices[2]));
                return -1;
            }

            var gid = (t.nGroupID == Triangle.InvalidGroupID) ? m_nSetInvalidGroupsTo : t.nGroupID;
            return builder.AppendTriangle(t.vIndices[0], t.vIndices[1], t.vIndices[2], gid);
        }

        IOReadResult BuildMeshes_Simple(ReadOptions options, IMeshBuilder builder)
        {
            if (vPositions.Length == 0)
                return new IOReadResult(IOCode.GarbageDataError, "No vertices in file");
            if (vTriangles.Length == 0)
                return new IOReadResult(IOCode.GarbageDataError, "No triangles in file");

            // [TODO] support non-per-vertex normals/colors
            var bHaveNormals = (vNormals.Length == vPositions.Length);
            var bHaveColors = (vColors.Length == vPositions.Length);
            var bHaveUVs = (vUVs.Length / 2 == vPositions.Length / 3);

            var nVertices = vPositions.Length / 3;
            var mapV = new int[nVertices];

            var meshID = builder.AppendNewMesh(bHaveNormals, bHaveColors, bHaveUVs, m_bOBJHasTriangleGroups);
            for (var k = 0; k < nVertices; ++k)
            {
                var vk = new Index3i(k, k, k);
                mapV[k] = append_vertex(builder, vk, bHaveNormals, bHaveColors, bHaveUVs);
            }

            // [TODO] this doesn't handle missing vertices...
            for (var k = 0; k < vTriangles.Length; ++k)
                append_triangle(builder, k, mapV);

            if (materialTokens.Count == 1)
            {
                // [RMS] should not be in here otherwise
                var material_id = materialTokens.ListID().First();
                var sMatName = materialTokens[material_id];
                var useMat = Materials[sMatName];
                var matID = builder.BuildMaterial(useMat);
                builder.AssignMaterial(matID, meshID);
            }

            return new IOReadResult(IOCode.Ok, "");
        }

        IOReadResult BuildMeshes_ByMaterial(ReadOptions options, IMeshBuilder builder)
        {
            if (vPositions.Length == 0)
                return new IOReadResult(IOCode.GarbageDataError, "No vertices in file");
            if (vTriangles.Length == 0)
                return new IOReadResult(IOCode.GarbageDataError, "No triangles in file");

            var bHaveNormals = (vNormals.Length > 0);
            var bHaveColors = (vColors.Length > 0);
            var bHaveUVs = (vUVs.Length > 0);

            var usedMaterialIDs = new List<int>(materialTokens.ListID());
            usedMaterialIDs.Add(Triangle.InvalidMaterialID);
            foreach (var material_id in usedMaterialIDs)
            {
                var matID = Triangle.InvalidMaterialID;
                if (material_id != Triangle.InvalidMaterialID)
                {
                    var sMatName = materialTokens[material_id];
                    var useMat = Materials[sMatName];
                    matID = builder.BuildMaterial(useMat);
                }

                var bMatHaveUVs = (material_id == Triangle.InvalidMaterialID) ? false : bHaveUVs;

                // don't append mesh until we actually see triangles
                var meshID = -1;

                var mapV = new Dictionary<Index3i, int>();

                for (var k = 0; k < vTriangles.Length; ++k)
                {
                    var t = vTriangles[k];
                    if (t.nMaterialID == material_id)
                    {
                        if (meshID == -1)
                            meshID = builder.AppendNewMesh(bHaveNormals, bHaveColors, bMatHaveUVs, false);

                        var t2 = new Triangle();
                        for (var j = 0; j < 3; ++j)
                        {
                            var vk = new Index3i(
                                t.vIndices[j] - 1, t.vNormals[j] - 1, t.vUVs[j] - 1);

                            var use_vtx = -1;
                            if (mapV.ContainsKey(vk) == false)
                            {
                                use_vtx = append_vertex(builder, vk, bHaveNormals, bHaveColors, bMatHaveUVs);
                                mapV[vk] = use_vtx;
                            }
                            else
                                use_vtx = mapV[vk];

                            t2.vIndices[j] = use_vtx;
                        }

                        append_triangle(builder, t2);
                    }
                }

                if (matID != Triangle.InvalidMaterialID)
                    builder.AssignMaterial(matID, meshID);
            }

            return new IOReadResult(IOCode.Ok, "");
        }

        string FindMTLFile(string sMTLFilePath)
        {
            foreach (var sPath in MTLFileSearchPaths)
            {
                var sFullPath = Path.Combine(sPath, sMTLFilePath);
                if (File.Exists(sFullPath))
                    return sFullPath;
            }

            return null;
        }

        public IOReadResult ReadMaterials(string sPath)
        {
            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] ReadMaterials " + sPath);

            StreamReader reader;
            try
            {
                reader = new StreamReader(sPath);
                if (reader.EndOfStream)
                    return new IOReadResult(IOCode.FileAccessError, "");
            }
            catch
            {
                return new IOReadResult(IOCode.FileAccessError, "");
            }

            OBJMaterial curMaterial = null;

            while (reader.Peek() >= 0)
            {
                var line = reader.ReadLine();
                var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                if (tokens[0][0] == '#')
                {
                    continue;
                }
                else if (tokens[0] == "newmtl")
                {
                    curMaterial = new OBJMaterial();
                    curMaterial.name = tokens[1];
                    curMaterial.id = Materials.Count;

                    if (Materials.ContainsKey(curMaterial.name))
                        emit_warning("Material file " + sPath + " / material " + curMaterial.name + " : already exists in Material set. Replacing.");
                    if (nWarningLevel >= 1)
                        emit_warning("[OBJReader] parsing material " + curMaterial.name);

                    Materials[curMaterial.name] = curMaterial;
                }
                else if (tokens[0] == "Ka")
                {
                    if (curMaterial != null) curMaterial.Ka = parse_mtl_color(tokens);
                }
                else if (tokens[0] == "Kd")
                {
                    if (curMaterial != null) curMaterial.Kd = parse_mtl_color(tokens);
                }
                else if (tokens[0] == "Ks")
                {
                    if (curMaterial != null) curMaterial.Ks = parse_mtl_color(tokens);
                }
                else if (tokens[0] == "Ke")
                {
                    if (curMaterial != null) curMaterial.Ke = parse_mtl_color(tokens);
                }
                else if (tokens[0] == "Tf")
                {
                    if (curMaterial != null) curMaterial.Tf = parse_mtl_color(tokens);
                }
                else if (tokens[0] == "illum")
                {
                    if (curMaterial != null) curMaterial.illum = int.Parse(tokens[1]);
                }
                else if (tokens[0] == "d")
                {
                    if (curMaterial != null) curMaterial.d = Single.Parse(tokens[1]);
                }
                else if (tokens[0] == "Tr")
                {
                    // alternate to d/alpha, [Tr]ansparency is 1-d
                    if (curMaterial != null) curMaterial.d = 1.0f - Single.Parse(tokens[1]);
                }
                else if (tokens[0] == "Ns")
                {
                    if (curMaterial != null) curMaterial.Ns = Single.Parse(tokens[1]);
                }
                else if (tokens[0] == "sharpness")
                {
                    if (curMaterial != null) curMaterial.sharpness = Single.Parse(tokens[1]);
                }
                else if (tokens[0] == "Ni")
                {
                    if (curMaterial != null) curMaterial.Ni = Single.Parse(tokens[1]);
                }
                else if (tokens[0] == "map_Ka")
                {
                    if (curMaterial != null) curMaterial.map_Ka = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "map_Kd")
                {
                    if (curMaterial != null) curMaterial.map_Kd = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "map_Ks")
                {
                    if (curMaterial != null) curMaterial.map_Ks = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "map_Ke")
                {
                    if (curMaterial != null) curMaterial.map_Ke = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "map_d")
                {
                    if (curMaterial != null) curMaterial.map_d = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "map_Ns")
                {
                    if (curMaterial != null) curMaterial.map_Ns = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "bump" || tokens[0] == "map_bump")
                {
                    if (curMaterial != null) curMaterial.bump = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "disp")
                {
                    if (curMaterial != null) curMaterial.disp = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "decal")
                {
                    if (curMaterial != null) curMaterial.decal = parse_mtl_path(line, tokens);
                }
                else if (tokens[0] == "refl")
                {
                    if (curMaterial != null) curMaterial.refl = parse_mtl_path(line, tokens);
                }
                else
                {
                    emit_warning("unknown material command " + tokens[0]);
                }
            }

            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] ReadMaterials completed");

            return new IOReadResult(IOCode.Ok, "ok");
        }

        private string parse_mtl_path(string line, string[] tokens)
        {
            if (tokens.Length == 2)
                return tokens[1];
            else
                return line.Substring(line.IndexOf(tokens[1]));
        }

        private Vector3f parse_mtl_color(string[] tokens)
        {
            if (tokens[1] == "spectral")
            {
                emit_warning("OBJReader::parse_material_color : spectral color not supported!");
                return new Vector3f(1, 0, 0);
            }
            else if (tokens[1] == "xyz")
            {
                emit_warning("OBJReader::parse_material_color : xyz color not supported!");
                return new Vector3f(1, 0, 0);
            }
            else
            {
                var r = float.Parse(tokens[1]);
                var g = float.Parse(tokens[2]);
                var b = float.Parse(tokens[3]);
                return new Vector3f(r, g, b);
            }
        }

        private OBJMaterial find_material(string sName)
        {
            if (Materials.ContainsKey(sName))
                return Materials[sName];

            // try case-insensitive search
            try
            {
                return Materials.First(x => String.Equals(x.Key, sName, StringComparison.OrdinalIgnoreCase)).Value;
            }
            catch
            {
                // didn't work
            }

            emit_warning("unknown material " + sName + " referenced");
            return null;
        }
    }
}