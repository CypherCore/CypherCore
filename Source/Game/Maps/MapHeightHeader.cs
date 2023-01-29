// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Maps
{
    public struct MapHeightHeader
    {
        public uint Fourcc;
        public HeightHeaderFlags Flags;
        public float GridHeight;
        public float GridMaxHeight;
    }
}