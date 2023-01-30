// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Game.DataStorage
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FieldMetaData
    {
        public short Bits;
        public ushort Offset;

        public int ByteCount
        {
            get
            {
                int value = (32 - Bits) >> 3;

                return (value < 0 ? Math.Abs(value) + 4 : value);
            }
        }

        public int BitCount
        {
            get
            {
                int bitSize = 32 - Bits;

                if (bitSize < 0)
                    bitSize = (bitSize * -1) + 32;

                return bitSize;
            }
        }

        public FieldMetaData(short bits, ushort offset)
        {
            Bits = bits;
            Offset = offset;
        }
    }
}