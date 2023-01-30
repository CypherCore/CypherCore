// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.IO;

namespace Game.Collision
{
    internal class TileFileOpenResult
    {
        public FileStream File;
        public string Name { get; set; }
        public uint UsedMapId { get; set; }
    }
}