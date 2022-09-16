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

namespace g3.io
{
    public abstract class AbstractMeshBuilder
    {
        private readonly List<TinyMeshItem> _items = new List<TinyMeshItem>();
        public IEnumerable<TinyMeshItem> Items => _items;

        public DMesh3 ActiveMesh => _items[_nActiveMesh].Mesh;
        private int _nActiveMesh = -1;

        public int AppendNewMesh(DMesh3 existingMesh)
        {
            var index = _items.Count;
            _items.Add(new TinyMeshItem(existingMesh));

            _nActiveMesh = index;
            return index;
        }

        public int AppendVertex(double x, double y, double z)
        {
            return _items[_nActiveMesh].Mesh.AppendVertex(new Vector3d(x, y, z));
        }

        public int AppendVertex(NewVertexInfo info)
        {
            return _items[_nActiveMesh].Mesh.AppendVertex(info);
        }

        public void AssignObject(int objectID, string objectName)
        {
            _items[_nActiveMesh].SetObject(objectID, objectName);
        }

        public void AssignGroup(int groupID, string groupName)
        {
            _items[_nActiveMesh].SetGroup(groupID, groupName);
        }

        public void AssignMaterial(int materialID, string materialName)
        {
            _items[_nActiveMesh].SetMaterial(materialID, materialName);
        }

        public void AssignMatFile(int matFileID, string matFileName)
        {
            _items[_nActiveMesh].SetMatFile(matFileID, matFileName);
        }

        public void AppendMetaData(string identifier, object data)
        {
            _items[_nActiveMesh].AppendMetaData(identifier, data);
        }
    }
}