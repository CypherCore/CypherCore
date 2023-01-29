// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public struct MapFileHeader
    {
        public uint MapMagic;
        public uint VersionMagic;
        public uint BuildMagic;
        public uint AreaMapOffset;
        public uint AreaMapSize;
        public uint HeightMapOffset;
        public uint HeightMapSize;
        public uint LiquidMapOffset;
        public uint LiquidMapSize;
        public uint HolesOffset;
        public uint HolesSize;
    }
}