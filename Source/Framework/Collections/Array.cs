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

using System.IO;

namespace System.Collections.Generic
{
    public class Array<T> : List<T>
    {
        int _limit;

        public Array(int size) : base(size)
        {
            _limit = size;
        }

        public Array(params T[] args) : base(args)
        {
            _limit = args.Length;
        }

        public Array(int size, T defaultFillValue) : base(size)
        {
            _limit = size;
            Fill(defaultFillValue);
        }

        public void Fill(T value)
        {
            for (var i = 0; i < _limit; ++i)
                Add(value);
        }

        public new T this[int index]
        {
            get
            {
                return base[index];
            }
            set
            {
                if (index >= Count)
                {
                    if (Count >= _limit)
                        throw new InternalBufferOverflowException("Attempted to read more array elements from packet " + Count + 1 + " than allowed " + _limit);

                    Insert(index, value);
                }
                else
                    base[index] = value;
            }
        }

        public int GetLimit() { return _limit; }

        public static implicit operator T[] (Array<T> array)
        {
            return array.ToArray();
        }
    }
}
