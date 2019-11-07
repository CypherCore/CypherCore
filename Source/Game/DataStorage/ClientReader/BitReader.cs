/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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
