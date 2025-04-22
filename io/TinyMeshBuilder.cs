using System;
using g3.io;

namespace g3
{
    public class TinyMeshBuilder<T> : AbstractMeshBuilder<T> where T : IDeformableMesh
    {
        public enum AddTriangleFailBehaviors
        {
            DiscardTriangle = 0,
            DuplicateAllVertices = 1
        }

        /// <summary>
        /// What should we do when AddTriangle() fails because triangle is non-manifold?
        /// </summary>
        public AddTriangleFailBehaviors NonManifoldTriBehavior = AddTriangleFailBehaviors.DuplicateAllVertices;

        /// <summary>
        /// What should we do when AddTriangle() fails because the triangle already exists?
        /// </summary>
        public AddTriangleFailBehaviors DuplicateTriBehavior = AddTriangleFailBehaviors.DiscardTriangle;

        public bool SupportsMetaData => true;

        public int AppendNewMesh(bool bHaveVtxNormals, bool bHaveVtxColors, bool bHaveVtxUVs, bool bHaveFaceGroups)
        {
            return AppendNewMesh((T)Activator.CreateInstance(typeof(T), bHaveVtxNormals, bHaveVtxColors, bHaveVtxUVs, bHaveFaceGroups));
        }

        public int AppendTriangle(int i, int j, int k)
        {
            return AppendTriangle(i, j, k, -1);
        }

        public int AppendTriangle(int i, int j, int k, int g)
        {
            if (typeof(T) == typeof(SimpleMesh))
            {
                return AppendTriangleToSimpleMesh(ActiveMesh as SimpleMesh, i, j, k, g);
            }
            else if (typeof(T) == typeof(DMesh3))
            {
                return AppendTriangleToDMesh3(ActiveMesh as DMesh3, i, j, k, g);
            }
            else
            {
                return DMesh3.InvalidID;
            }
        }

        private int AppendTriangleToSimpleMesh(SimpleMesh mesh, int i, int j, int k, int g)
        {
            return mesh.AppendTriangle(i, j, k, g);
        }

        private int AppendTriangleToDMesh3(DMesh3 mesh, int i, int j, int k, int g)
        {
            // [RMS] What to do here? We definitely do not want to add a duplicate triangle!!
            //   But is silently ignoring the right thing to do?
            var existing_tid = mesh.FindTriangle(i, j, k);
            if (existing_tid != DMesh3.InvalidID)
            {
                if (DuplicateTriBehavior == AddTriangleFailBehaviors.DuplicateAllVertices)
                    return append_duplicate_triangle(mesh, i, j, k, g);
                else
                    return existing_tid;
            }

            var tid = mesh.AppendTriangle(i, j, k, g);
            if (tid == DMesh3.NonManifoldID)
            {
                if (NonManifoldTriBehavior == AddTriangleFailBehaviors.DuplicateAllVertices)
                    return append_duplicate_triangle(mesh, i, j, k, g);
                else
                    return DMesh3.NonManifoldID;
            }

            return tid;
        }

        int append_duplicate_triangle(DMesh3 mesh, int i, int j, int k, int g)
        {
            var vertexInfo = new NewVertexInfo();
            mesh.GetVertex(i, ref vertexInfo, true, true, true);
            var new_i = mesh.AppendVertex(vertexInfo);
            mesh.GetVertex(j, ref vertexInfo, true, true, true);
            var new_j = mesh.AppendVertex(vertexInfo);
            mesh.GetVertex(k, ref vertexInfo, true, true, true);
            var new_k = mesh.AppendVertex(vertexInfo);
            return mesh.AppendTriangle(new_i, new_j, new_k, g);
        }
    }
}