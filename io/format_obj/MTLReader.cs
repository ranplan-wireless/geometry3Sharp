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

using System;
using System.Collections.Generic;
using System.IO;

namespace g3
{
    // ReSharper disable once InconsistentNaming
    public class MTLReader
    {
        private readonly MeshIOLogger _logger;

        public IList<OBJMaterial> Materials { get; private set; } = new List<OBJMaterial>();

        public MTLReader(MeshIOLogger logger)
        {
            _logger = logger;
        }

        public IOReadResult Read(string sPath)
        {
            if (_logger.nWarningLevel >= 1)
                _logger.emit_warning($"[MTLReader] Read materials from file: {sPath}");

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

            return ReadImplement(reader);
        }

        public IOReadResult Read(TextReader reader)
        {
            if (_logger.nWarningLevel >= 1)
                _logger.emit_warning("[MTLReader] Read materials from TextReader");

            return ReadImplement(reader);
        }

        private IOReadResult ReadImplement(TextReader reader)
        {
            Materials = new List<OBJMaterial>();

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

                    if (_logger.nWarningLevel >= 1)
                        _logger.emit_warning("[MTLReader] parsing material " + curMaterial.name);
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
                    if (curMaterial != null) curMaterial.d = float.Parse(tokens[1]);
                }
                else if (tokens[0] == "Tr")
                {
                    // alternate to d/alpha, [Tr]ansparency is 1-d
                    if (curMaterial != null) curMaterial.d = 1.0f - float.Parse(tokens[1]);
                }
                else if (tokens[0] == "Ns")
                {
                    if (curMaterial != null) curMaterial.Ns = float.Parse(tokens[1]);
                }
                else if (tokens[0] == "sharpness")
                {
                    if (curMaterial != null) curMaterial.sharpness = float.Parse(tokens[1]);
                }
                else if (tokens[0] == "Ni")
                {
                    if (curMaterial != null) curMaterial.Ni = float.Parse(tokens[1]);
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
                    _logger.emit_warning("unknown material command " + tokens[0]);
                }
            }

            if (_logger.nWarningLevel >= 1)
                _logger.emit_warning("[MTLReader] ReadMaterials completed");

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
                _logger.emit_warning("MTLReader::parse_material_color : spectral color not supported!");
                return new Vector3f(1, 0, 0);
            }
            else if (tokens[1] == "xyz")
            {
                _logger.emit_warning("MTLReader::parse_material_color : xyz color not supported!");
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
    }
}