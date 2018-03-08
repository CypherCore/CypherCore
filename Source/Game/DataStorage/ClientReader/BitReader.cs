/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

using Framework.IO;
using System;

namespace Game.DataStorage
{
    public class BitReader
    {
        public BitReader(byte[] data)
        {
            m_array = data;
        }

        public BitReader(byte[] data, int offset)
        {
            m_array = data;
            m_readOffset = offset;
        }

        public uint ReadUInt32(int numBits)
        {
            uint result = BitConverter.ToUInt32(m_array, m_readOffset + (m_readPos >> 3)) << (32 - numBits - (m_readPos & 7)) >> (32 - numBits);
            m_readPos += numBits;
            return result;
        }

        public ulong ReadUInt64(int bitWidth, int bitOffset)
        {
            int bitsToRead = bitOffset & 7;
            ulong result = BitConverter.ToUInt64(m_array, m_readOffset + (m_readPos >> 3)) << (64 - bitsToRead - bitWidth) >> (64 - bitWidth);
            m_readPos += bitWidth;
            return result;
        }

        public byte[] ReadValue(int bitWidth, int bitOffset = 0, bool isSigned = false)
        {
            ulong result = ReadUInt64(bitWidth, bitOffset);
            if (isSigned)
            {
                ulong mask = 1ul << (bitWidth - 1);
                result = (result ^ mask) - mask;
            }

            var ulongBytes = BitConverter.GetBytes(result);
            byte[] data = new byte[NextPow2((bitWidth + 7) / 8)];
            Buffer.BlockCopy(ulongBytes, 0, data, 0, data.Length);

            return data;
        }

        private int NextPow2(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return Math.Max(v, 1);
        }

        public int Position { get => m_readPos; set => m_readPos = value; }
        public int Offset { get => m_readOffset; set => m_readOffset = value; }

        private byte[] m_array;
        private int m_readPos;
        private int m_readOffset;
    }
}
