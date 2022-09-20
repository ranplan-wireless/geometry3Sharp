//  ****************************************************************************
//  Ranplan Wireless Network Design Ltd.
//  __________________
//   All Rights Reserved. [2022]
// 
//  NOTICE:
//  All information contained herein is, and remains the property of
//  Ranplan Wireless Network Design Ltd. and its suppliers, if any.
//  The intellectual and technical concepts contained herein are proprietary
//  to Ranplan Wireless Network Design Ltd. and its suppliers and may be
//  covered by U.S. and Foreign Patents, patents in process, and are protected
//  by trade secret or copyright law.
//  Dissemination of this information or reproduction of this material
//  is strictly forbidden unless prior written permission is obtained
//  from Ranplan Wireless Network Design Ltd.
// ****************************************************************************

using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace g3
{
    /// <summary>
    /// Read the obj file by tiny size
    /// Give an example obj:
    /// <code>
    /// mtllib box3_1.mtl
    /// 
    /// o Cube1
    /// v 0 0 0
    /// v 5 0 0
    /// v 0 0 5
    /// v 5 0 5
    /// usemtl Material
    /// s off
    /// g Cube1_Group1
    /// f 1 2 3
    /// g Cube1_Group2
    /// f 1 2 4
    /// 
    /// o Cube2
    /// v 0 1 0
    /// v 5 1 0
    /// v 0 1 5
    /// v 5 1 5
    /// usemtl SecondMaterial
    /// g Cube2_Group1
    /// f 5 6 7
    /// f 5 6 8
    /// 
    /// mtllib box3_2.mtl
    /// o Cube3
    /// v 0 2 0
    /// v 5 2 0
    /// v 0 2 5
    /// usemtl Material
    /// s off
    /// f 9 10 11
    /// </code>
    /// the reader will return 4 meshes.
    /// </summary>
    /// <remarks>
    /// The usage are following:
    /// <code>
    /// var filePath = @"E:\Outdoor\box\box3.obj";
    /// using (var streamReader = new StreamReader(filePath))
    /// {
    ///     var builder = new TinyMeshBuilder();
    ///     var objReader = new TinyOBJReader();
    ///     objReader.Read(streamReader, builder);
    /// 
    ///     var result = builder.Items;
    /// }
    /// </code>
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    public class TinyOBJReader : OBJParser
    {
        private const int CancellationCheckBuildStep = 100;

        public IOReadResult Read(TextReader reader, ReadOptions options, TinyMeshBuilder builder)
        {
            HasComplexVertices = false;

            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] starting parse obj.");
            var parseResult = ParseInput(reader, options.CancellationToken);
            if (parseResult.code != IOCode.Ok)
                return parseResult;

            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] completed parse obj.");

            var buildResult = BuildMeshes(options.CancellationToken, builder);

            if (nWarningLevel >= 1)
                emit_warning("[OBJReader] build complete.");

            if (buildResult.code != IOCode.Ok)
                return buildResult;

            return new IOReadResult(IOCode.Ok, "");
        }

        private IOReadResult BuildMeshes(CancellationToken cancellationToken, TinyMeshBuilder builder)
        {
            if (VertexPositions.Length == 0)
                return new IOReadResult(IOCode.GarbageDataError, "No vertices in file");
            if (Triangles.Length == 0)
                return new IOReadResult(IOCode.GarbageDataError, "No triangles in file");

            var bHaveNormals = (VertexNormals.Length > 0);
            var bHaveColors = (VertexColors.Length > 0);
            var bHaveUVs = (VertexUVs.Length > 0);
            var bMatHaveUVs = false;

            // don't append mesh until we actually see triangles
            var meshID = -1;

            var mapV = new Dictionary<Index3i, int>();

            var nActiveMaterial = Triangle.InvalidMaterialID;
            var nActiveGroup = Triangle.InvalidGroupID;
            var nActiveObject = Triangle.InvalidObjectID;
            for (var k = 0; k < Triangles.Length; ++k)
            {
                if (cancellationToken.IsCancellationRequested && k % CancellationCheckBuildStep == 0)
                    return new IOReadResult(IOCode.Cancelled, "Cancelled by manual when building");

                var t = Triangles[k];

                if (k == 0
                    || t.nMaterialID != nActiveMaterial
                    || t.nGroupID != nActiveGroup
                    || t.nObjectID != nActiveObject)
                {
                    nActiveMaterial = t.nMaterialID;
                    nActiveGroup = t.nGroupID;
                    nActiveObject = t.nObjectID;

                    bMatHaveUVs = (nActiveMaterial == Triangle.InvalidMaterialID) ? false : bHaveUVs;
                    meshID = builder.AppendNewMesh(bHaveNormals, bHaveColors, bMatHaveUVs, false);

                    builder.AssignObject(t.nObjectID, ObjectTokens.GetName(t.nObjectID));
                    builder.AssignGroup(t.nGroupID, GroupTokens.GetName(t.nGroupID));
                    builder.AssignMaterial(t.nMaterialID, MaterialTokens.GetName(t.nMaterialID));
                    builder.AssignMatFile(t.nMatFileID, MatFileTokens.GetName(t.nMatFileID));

                    mapV = new Dictionary<Index3i, int>();
                }

                var t2 = new Triangle();
                for (var j = 0; j < 3; ++j)
                {
                    var vk = new Index3i(t.vIndices[j] - 1, t.vNormals[j] - 1, t.vUVs[j] - 1);

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

            return new IOReadResult(IOCode.Ok, "");
        }

        private int append_vertex(TinyMeshBuilder builder, Index3i vertIdx, bool bHaveNormals, bool bHaveColors, bool bHaveUVs)
        {
            var vi = 3 * vertIdx.a;
            if (vertIdx.a < 0 || vertIdx.a >= VertexPositions.Length / 3)
            {
                emit_warning("[OBJReader] append_vertex() referencing invalid vertex " + vertIdx.a.ToString());
                return -1;
            }

            if (bHaveNormals == false && bHaveColors == false && bHaveUVs == false)
                return builder.AppendVertex(VertexPositions[vi], VertexPositions[vi + 1], VertexPositions[vi + 2]);

            var vinfo = new NewVertexInfo();
            vinfo.bHaveC = vinfo.bHaveN = vinfo.bHaveUV = false;
            vinfo.v = new Vector3d(VertexPositions[vi], VertexPositions[vi + 1], VertexPositions[vi + 2]);
            if (bHaveNormals)
            {
                vinfo.bHaveN = true;
                var ni = 3 * vertIdx.b;
                vinfo.n = new Vector3f(VertexNormals[ni], VertexNormals[ni + 1], VertexNormals[ni + 2]);
            }

            if (bHaveColors)
            {
                vinfo.bHaveC = true;
                vinfo.c = new Vector3f(VertexColors[vi], VertexColors[vi + 1], VertexColors[vi + 2]);
            }

            if (bHaveUVs && vertIdx.c > 0)
            {
                vinfo.bHaveUV = true;
                var ui = 2 * vertIdx.c;
                vinfo.uv = new Vector2f(VertexUVs[ui], VertexUVs[ui + 1]);
            }

            return builder.AppendVertex(vinfo);
        }

        private int append_triangle(TinyMeshBuilder builder, Triangle t)
        {
            if (t.vIndices[0] < 0 || t.vIndices[1] < 0 || t.vIndices[2] < 0)
            {
                emit_warning(string.Format("[OBJReader] invalid triangle:  {0} {1} {2}",
                    t.vIndices[0], t.vIndices[1], t.vIndices[2]));
                return -1;
            }

            var gid = (t.nGroupID == Triangle.InvalidGroupID) ? SetInvalidGroupsTo : t.nGroupID;
            return builder.AppendTriangle(t.vIndices[0], t.vIndices[1], t.vIndices[2], gid);
        }
    }
}