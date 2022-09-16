using g3.io;

namespace g3
{
    public class TinyMeshBuilder : AbstractMeshBuilder
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
            var m = new DMesh3(bHaveVtxNormals, bHaveVtxColors, bHaveVtxUVs, bHaveFaceGroups);
            return AppendNewMesh(m);
        }

        public int AppendTriangle(int i, int j, int k)
        {
            return AppendTriangle(i, j, k, -1);
        }

        public int AppendTriangle(int i, int j, int k, int g)
        {
            // [RMS] What to do here? We definitely do not want to add a duplicate triangle!!
            //   But is silently ignoring the right thing to do?
            var existing_tid = ActiveMesh.FindTriangle(i, j, k);
            if (existing_tid != DMesh3.InvalidID)
            {
                if (DuplicateTriBehavior == AddTriangleFailBehaviors.DuplicateAllVertices)
                    return append_duplicate_triangle(i, j, k, g);
                else
                    return existing_tid;
            }

            var tid = ActiveMesh.AppendTriangle(i, j, k, g);
            if (tid == DMesh3.NonManifoldID)
            {
                if (NonManifoldTriBehavior == AddTriangleFailBehaviors.DuplicateAllVertices)
                    return append_duplicate_triangle(i, j, k, g);
                else
                    return DMesh3.NonManifoldID;
            }

            return tid;
        }

        int append_duplicate_triangle(int i, int j, int k, int g)
        {
            var vertexInfo = new NewVertexInfo();
            ActiveMesh.GetVertex(i, ref vertexInfo, true, true, true);
            var new_i = ActiveMesh.AppendVertex(vertexInfo);
            ActiveMesh.GetVertex(j, ref vertexInfo, true, true, true);
            var new_j = ActiveMesh.AppendVertex(vertexInfo);
            ActiveMesh.GetVertex(k, ref vertexInfo, true, true, true);
            var new_k = ActiveMesh.AppendVertex(vertexInfo);
            return ActiveMesh.AppendTriangle(new_i, new_j, new_k, g);
        }
    }
}