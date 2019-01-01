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

using System;

namespace Game.Network
{
    public class CompactArray
    {
        public void Insert(int index, int value)
        {
            Cypher.Assert(index < 0x20);

            _mask |= 1u << index;
            if (_contents.Length <= index)
                Array.Resize(ref _contents, index + 1);
            _contents[index] = value;
        }

        void Clear()
        {
            _mask = 0;
            _contents = new int[1];
        }

        public void Read(WorldPacket data)
        {
            uint mask = data.ReadUInt32();

            for (int index = 0; mask != 0; mask >>= 1, ++index)
            {
                if ((mask & 1) != 0)
                {
                    int value = data.ReadInt32();
                    Insert(index, value);
                }
            }
        }

        public void Write(WorldPacket data)
        {
            uint mask = GetMask();
            data.WriteUInt32(mask);
            for (int i = 0; i < GetSize(); ++i)
            {
                if (Convert.ToBoolean(mask & (1 << i)))
                    data.WriteInt32(this[i]);
            }
        }

        public override int GetHashCode()
        {
            return _mask.GetHashCode() ^ _contents.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is CompactArray)
                return (CompactArray)obj == this;

            return false;
        }

        public static bool operator ==(CompactArray left, CompactArray right)
        {
            if (left._mask != right._mask)
                return false;

            return left._contents == right._contents;
        }

        public static bool operator !=(CompactArray left, CompactArray right)
        {
            return !(left == right);
        }

        uint GetMask() { return _mask; }
        public int this[int index] { get { return _contents[index]; } }
        int GetSize() { return _contents.Length; }

        uint _mask;
        int[] _contents = new int[1];
    }
}
