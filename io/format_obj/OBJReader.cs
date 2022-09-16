using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace g3
{
    // ReSharper disable once InconsistentNaming
    public class OBJReader : OBJParser, IMeshReader
    {
        private Dictionary<string, OBJMaterial> _materials;

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
            _materials = new Dictionary<string, OBJMaterial>();
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
                        var mtlReader = new MTLReader(this);
                        var result = mtlReader.Read(sFile);
                        if (result.code != IOCode.Ok)
                            emit_warning("error parsing " + sFile + " : " + result.message);
                        else
                        {
                            foreach (var curMaterial in mtlReader.Materials)
                            {
                                if (_materials.ContainsKey(curMaterial.name))
                                    emit_warning("Material file " + sFile + " / material " + curMaterial.name + " : already exists in Material set. Replacing.");
                                if (nWarningLevel >= 1)
                                    emit_warning("[OBJReader] parsing material " + curMaterial.name);

                                _materials[curMaterial.name] = curMaterial;
                            }
                        }
                    }
                    else
                        emit_warning("material file " + sMTLPathString + " could not be found in material search paths");
                }

                if (nWarningLevel >= 1)
                    emit_warning("[OBJReader] completed parse mtl.");
            }

            var buildResult = (_materials.Count > 1 || HasComplexVertices) ? BuildMeshes_ByMaterial(builder) : BuildMeshes_Simple(builder);

            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] build complete.");

            if (buildResult.code != IOCode.Ok)
                return buildResult;

            return new IOReadResult(IOCode.Ok, "");
        }

        private int append_vertex(IMeshBuilder builder, Index3i vertIdx, bool bHaveNormals, bool bHaveColors, bool bHaveUVs)
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

        private int append_triangle(IMeshBuilder builder, int nTri, int[] mapV)
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

        private int append_triangle(IMeshBuilder builder, Triangle t)
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

        private IOReadResult BuildMeshes_Simple(IMeshBuilder builder)
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
                var useMat = _materials[sMatName];
                var matID = builder.BuildMaterial(useMat);
                builder.AssignMaterial(matID, meshID);
            }

            return new IOReadResult(IOCode.Ok, "");
        }

        private IOReadResult BuildMeshes_ByMaterial(IMeshBuilder builder)
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
                    var useMat = _materials[sMatName];
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

        private string FindMTLFile(string sMTLFilePath)
        {
            foreach (var sPath in MTLFileSearchPaths)
            {
                var sFullPath = Path.Combine(sPath, sMTLFilePath);
                if (File.Exists(sFullPath))
                    return sFullPath;
            }

            return null;
        }
    }
}