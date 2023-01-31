// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;

namespace Game.DataStorage
{
    public class BitReader
    {
        public int Position { get; set; }
        public int Offset { get; set; }
        public byte[] Data { get; set; }

        public BitReader(byte[] data)
        {
            Data = data;
        }

        public BitReader(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }

        public T Read<T>(int numBits) where T : unmanaged
        {
            ulong result = Unsafe.As<byte, ulong>(ref Data[Offset + (Position >> 3)]) << (64 - numBits - (Position & 7)) >> (64 - numBits);
            Position += numBits;
            return Unsafe.As<ulong, T>(ref result);
        }

        public T ReadSigned<T>(int numBits) where T : unmanaged
        {
            ulong result = Unsafe.As<byte, ulong>(ref Data[Offset + (Position >> 3)]) << (64 - numBits - (Position & 7)) >> (64 - numBits);
            Position += numBits;
            ulong signedShift = (1UL << (numBits - 1));
            result = (signedShift ^ result) - signedShift;
            return Unsafe.As<ulong, T>(ref result);
        }

        public string ReadCString()
        {
            int start = Position;

            while (Data[Offset + (Position >> 3)] != 0)
                Position += 8;

            string result = Encoding.UTF8.GetString(Data, Offset + (start >> 3), (Position - start) >> 3);
            Position += 8;
            return result;
        }
    }
}
