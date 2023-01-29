// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Maps
{
    public struct MapLiquidHeader
    {
        public uint Fourcc;
        public LiquidHeaderFlags Flags;
        public byte LiquidFlags;
        public ushort LiquidType;
        public byte OffsetX;
        public byte OffsetY;
        public byte Width;
        public byte Height;
        public float LiquidLevel;
    }
}