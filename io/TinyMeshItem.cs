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
using System.Collections.ObjectModel;

namespace g3
{
    /// <summary>
    /// The mesh read result item by <see cref="TinyOBJReader"/>
    /// </summary>
    public class TinyMeshItem
    {
        private readonly Dictionary<string, object> _metadata = new Dictionary<string, object>();

        /// <summary>
        /// The mesh geometry
        /// </summary>
        public DMesh3 Mesh { get; }

        /// <summary>
        /// The mesh object name, the line start with `o`
        /// </summary>
        public string ObjectName { get; private set; }
        /// <summary>
        /// The mesh object id, indicate the mesh belongs the nth `o` token in obj file
        /// </summary>
        public int ObjectID { get; private set; }

        /// <summary>
        /// The mesh group name, the line start with `g`
        /// </summary>
        public string GroupName { get; private set; }
        /// <summary>
        /// The mesh group id, indicate the mesh belongs the nth `g` token in obj file
        /// </summary>
        public int GroupID { get; private set; }

        /// <summary>
        /// The mesh material name, the line start with `usemtl`
        /// </summary>
        public string MaterialName { get; private set; }
        /// <summary>
        /// The mesh material id, indicate the mesh belongs the nth `usemtl` token in obj file
        /// </summary>
        public int MaterialID { get; private set; }

        /// <summary>
        /// The mesh material file, the line start with `mtllib`
        /// </summary>
        public string MatFileName { get; private set; }
        /// <summary>
        /// The mesh material file, indicate the mesh belongs the nth `mtllib` token in obj file
        /// </summary>
        public int MatFileID { get; private set; }

        /// <summary>
        /// The mesh metadata
        /// </summary>
        public IDictionary<string, object> Metadata { get; }

        public TinyMeshItem(DMesh3 mesh)
        {
            Mesh = mesh;
            SetObject(Triangle.InvalidObjectID, string.Empty);
            SetGroup(Triangle.InvalidGroupID, string.Empty);
            SetMaterial(Triangle.InvalidMaterialID, string.Empty);
            SetMatFile(Triangle.InvalidMatFileID, string.Empty);
            Metadata = new ReadOnlyDictionary<string, object>(_metadata);
        }

        public void SetObject(int objectID, string objectName)
        {
            ObjectID = objectID;
            ObjectName = objectName;
        }

        public void SetGroup(int groupID, string groupName)
        {
            GroupID = groupID;
            GroupName = groupName;
        }

        public void SetMaterial(int materialID, string materialName)
        {
            MaterialID = materialID;
            MaterialName = materialName;
        }

        public void SetMatFile(int matFileID, string matFileName)
        {
            MatFileID = matFileID;
            MatFileName = matFileName;
        }

        public void AppendMetaData(string identifier, object data)
        {
            _metadata.Add(identifier, data);
        }
    }
}