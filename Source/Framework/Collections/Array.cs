﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
