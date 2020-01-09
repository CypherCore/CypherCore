/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

namespace Game.Entities
{
    public class UpdateMask
    {
        int _blockCount;
        int _blocksMaskCount;
        uint[] _blocks;
        uint[] _blocksMask;

        public UpdateMask(int bits, uint[] input = null)
        {
            _blockCount = (bits + 31) / 32;
            _blocksMaskCount = (_blockCount + 31) / 32;

            _blocks = new uint[_blockCount];
            _blocksMask = new uint[_blocksMaskCount];

            if (input != null)
            {
                int block = 0;
                for (; block < input.Length; ++block)
                    if ((_blocks[block] = input[block]) != 0)
                        _blocksMask[GetBlockIndex(block)] |= (uint)GetBlockFlag(block);

                for (; block < _blockCount; ++block)
                    _blocks[block] = 0;
            }
        }

        public uint GetBlocksMask(uint index)
        {
            return _blocksMask[index];
        }

        public uint GetBlock(uint index)
        {
            return _blocks[index];
        }

        public bool this[int index]
        {
            get
            {
                return (_blocks[index / 32] & (1 << (index % 32))) != 0;
            }
        }

        public void Reset(int index)
        {
            int blockIndex = GetBlockIndex(index);
            if ((_blocks[blockIndex] &= ~(uint)GetBlockFlag(index)) == 0)
                _blocksMask[GetBlockIndex(blockIndex)] &= ~(uint)GetBlockFlag(blockIndex);
        }

        public void ResetAll()
        {
            Array.Clear(_blocks, 0, _blocks.Length);
            Array.Clear(_blocksMask, 0, _blocksMask.Length);
        }

        public void Set(int index)
        {
            int blockIndex = GetBlockIndex(index);
            _blocks[blockIndex] |= (uint)GetBlockFlag(index);
            _blocksMask[GetBlockIndex(blockIndex)] |= (uint)GetBlockFlag(blockIndex);
        }

        public void SetAll()
        {
            for (int i = 0; i < _blocksMaskCount; ++i)
                _blocksMask[i] = 0xFFFFFFFF;
            for (int i = 0; i < _blockCount; ++i)
                _blocks[i] = 0xFFFFFFFF;

            if ((_blocksMaskCount % 32) != 0)
            {
                int unused = 32 - (_blocksMaskCount % 32);
                _blocksMask[_blocksMaskCount - 1] &= (0xFFFFFFFF >> unused);
            }

            if ((_blockCount % 32) != 0)
            {
                int unused = 32 - (_blockCount % 32);
                _blocks[_blockCount - 1] &= (0xFFFFFFFF >> unused);
            }
        }

        public void AND(UpdateMask right)
        {
            for (int i = 0; i < _blocksMaskCount; ++i)
                _blocksMask[i] &= right._blocksMask[i];

            for (int i = 0; i < _blockCount; ++i)
            {
                if (!Convert.ToBoolean(_blocks[i] &= right._blocks[i]))
                    _blocksMask[GetBlockIndex(i)] &= ~(uint)GetBlockFlag(i);
            }
        }

        public void OR(UpdateMask right)
        {
            for (int i = 0; i < _blocksMaskCount; ++i)
                _blocksMask[i] |= right._blocksMask[i];

            for (int i = 0; i < _blockCount; ++i)
                _blocks[i] |= right._blocks[i];
        }

        public static UpdateMask operator &(UpdateMask left, UpdateMask right)
        {
            UpdateMask result = left;
            result.AND(right);
            return result;
        }

        public static UpdateMask operator |(UpdateMask left, UpdateMask right)
        {
            UpdateMask result = left;
            result.OR(right);
            return result;
        }

        //helpers
        public static int GetBlockIndex(int bit) { return bit / 32; }
        public static int GetBlockFlag(int bit) { return 1 << (bit % 32); }
    }
}
