namespace g3
{
    /// <summary>
    /// gradientspace OBJ mesh format parser
    /// 
    /// Basic structure is:
    ///   1) parse OBJ into internal data structures that represent OBJ exactly
    ///   2) convert to mesh objects based on options/etc
    /// 
    /// [TODO] major current limitation is that we do not support multiple UVs per-vertex
    ///   (this is a limitation of DMesh3). Similarly no multiple normals per-vertex. So,
    ///   in the current code, such vertices are duplicated. See append_vertex() and use
    ///   of Index3i triplet (vi,ni,ui) to represent vertex in BuildMeshes_X() functions
    /// 
    /// [TODO] only a single material can be assigned to a mesh, this is a current limitation
    ///   of DMesh3Builder. So, in this case we are splitting the input mesh by material, IE
    ///   multiple meshes are returned for a single input mesh, each with one material.
    /// 
    /// 
    /// </summary>
    public struct Triangle
    {
        public const int InvalidMaterialID = -1;
        public const int InvalidGroupID = -1;
        public const int InvalidObjectID = -1;
        public const int InvalidMatFileID = -1;

        public Index3i vIndices;
        public Index3i vNormals;
        public Index3i vUVs;

        public int nMaterialID;
        public int nGroupID;
        public int nObjectID;
        public int nMatFileID;

        public void clear()
        {
            nMaterialID = InvalidMaterialID;
            nGroupID = InvalidGroupID;
            vIndices = vNormals = vUVs = new Index3i(-1, -1, -1);
        }

        public void set_vertex(int j, int vi, int ni = -1, int ui = -1)
        {
            vIndices[j] = vi;
            if (ni != -1) vNormals[j] = ni;
            if (ui != -1) vUVs[j] = ui;
        }

        public void move_vertex(int jFrom, int jTo)
        {
            vIndices[jTo] = vIndices[jFrom];
            vNormals[jTo] = vNormals[jFrom];
            vUVs[jTo] = vUVs[jFrom];
        }

        public bool is_complex()
        {
            for (int j = 0; j < 3; ++j)
            {
                if (vNormals[j] != -1 && vNormals[j] != vNormals[j])
                    return true;
                if (vUVs[j] != -1 && vUVs[j] != vUVs[j])
                    return true;
            }

            return false;
        }
    }
}