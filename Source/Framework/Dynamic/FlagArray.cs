// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Dynamic
{
    public class FlagsArray<T> where T : struct
    {
        protected dynamic[] _storage;

        public FlagsArray(uint length)
        {
            _storage = new dynamic[length];
        }

        public FlagsArray(T[] parts)
        {
            _storage = new dynamic[parts.Length];

            for (var i = 0; i < parts.Length; ++i)
                _storage[i] = parts[i];
        }

        public FlagsArray(T[] parts, uint length)
        {
            for (var i = 0; i < parts.Length; ++i)
                _storage[i] = parts[i];
        }

        public T this[int i]
        {
            get => _storage[i];
            set => _storage[i] = value;
        }

        public static bool operator <(FlagsArray<T> left, FlagsArray<T> right)
        {
            for (int i = (int)left.GetSize(); i > 0; --i)
                if ((dynamic)left[i - 1] < right[i - 1])
                    return true;
                else if ((dynamic)left[i - 1] > right[i - 1])
                    return false;

            return false;
        }

        public static bool operator >(FlagsArray<T> left, FlagsArray<T> right)
        {
            for (int i = (int)left.GetSize(); i > 0; --i)
                if ((dynamic)left[i - 1] > right[i - 1])
                    return true;
                else if ((dynamic)left[i - 1] < right[i - 1])
                    return false;

            return false;
        }

        public static FlagsArray<T> operator &(FlagsArray<T> left, FlagsArray<T> right)
        {
            FlagsArray<T> fl = new(left.GetSize());

            for (var i = 0; i < left.GetSize(); ++i)
                fl[i] = (dynamic)left[i] & right[i];

            return fl;
        }

        public static FlagsArray<T> operator |(FlagsArray<T> left, FlagsArray<T> right)
        {
            FlagsArray<T> fl = new(left.GetSize());

            for (var i = 0; i < left.GetSize(); ++i)
                fl[i] = (dynamic)left[i] | right[i];

            return fl;
        }

        public static FlagsArray<T> operator ^(FlagsArray<T> left, FlagsArray<T> right)
        {
            FlagsArray<T> fl = new(left.GetSize());

            for (var i = 0; i < left.GetSize(); ++i)
                fl[i] = (dynamic)left[i] ^ right[i];

            return fl;
        }

        public static implicit operator bool(FlagsArray<T> left)
        {
            for (var i = 0; i < left.GetSize(); ++i)
                if ((dynamic)left[i] != 0)
                    return true;

            return false;
        }

        public uint GetSize()
        {
            return (uint)_storage.Length;
        }
    }

    public class FlagArray128 : FlagsArray<uint>
    {
        public FlagArray128(uint p1 = 0, uint p2 = 0, uint p3 = 0, uint p4 = 0) : base(4)
        {
            _storage[0] = p1;
            _storage[1] = p2;
            _storage[2] = p3;
            _storage[3] = p4;
        }

        public FlagArray128(uint[] parts) : base(4)
        {
            _storage[0] = parts[0];
            _storage[1] = parts[1];
            _storage[2] = parts[2];
            _storage[3] = parts[3];
        }

        public bool IsEqual(params uint[] parts)
        {
            for (var i = 0; i < _storage.Length; ++i)
                if (_storage[i] == parts[i])
                    return false;

            return true;
        }

        public bool HasFlag(params uint[] parts)
        {
            return (_storage[0] & parts[0] || _storage[1] & parts[1] || _storage[2] & parts[2] || _storage[3] & parts[3]);
        }

        public void Set(params uint[] parts)
        {
            for (var i = 0; i < parts.Length; ++i)
                _storage[i] = parts[i];
        }

        public static FlagArray128 operator &(FlagArray128 left, FlagArray128 right)
        {
            FlagArray128 fl = new();

            for (var i = 0; i < left._storage.Length; ++i)
                fl[i] = left._storage[i] & right._storage[i];

            return fl;
        }

        public static FlagArray128 operator |(FlagArray128 left, FlagArray128 right)
        {
            FlagArray128 fl = new();

            for (var i = 0; i < left._storage.Length; ++i)
                fl[i] = left._storage[i] | right._storage[i];

            return fl;
        }

        public static FlagArray128 operator ^(FlagArray128 left, FlagArray128 right)
        {
            FlagArray128 fl = new();

            for (var i = 0; i < left._storage.Length; ++i)
                fl[i] = left._storage[i] ^ right._storage[i];

            return fl;
        }
    }

    public class FlaggedArray32<T> where T : struct
    {
        private readonly int[] _values;
        private uint _flags;

        public FlaggedArray32(byte arraysize)
        {
            _values = new int[4 * arraysize];
        }

        public uint GetFlags()
        {
            return _flags;
        }

        public bool HasFlag(T flag)
        {
            return (_flags & (1 << Convert.ToInt32(flag))) != 0;
        }

        public void AddFlag(T flag)
        {
            _flags |= (dynamic)(1 << Convert.ToInt32(flag));
        }

        public void DelFlag(T flag)
        {
            _flags &= ~(dynamic)(1 << Convert.ToInt32(flag));
        }

        public int GetValue(T flag)
        {
            return _values[Convert.ToInt32(flag)];
        }

        public void SetValue(T flag, object value)
        {
            _values[Convert.ToInt32(flag)] = Convert.ToInt32(value);
        }

        public void AddValue(T flag, object value)
        {
            _values[Convert.ToInt32(flag)] += Convert.ToInt32(value);
        }
    }

    public class FlaggedArray64<T> where T : struct
    {
        private readonly int[] _values;
        private ulong _flags;

        public FlaggedArray64(byte arraysize)
        {
            _values = new int[4 * arraysize];
        }

        public ulong GetFlags()
        {
            return _flags;
        }

        public bool HasFlag(T flag)
        {
            return (_flags & (1ul << Convert.ToInt32(flag))) != 0;
        }

        public void AddFlag(T flag)
        {
            _flags |= (dynamic)(1ul << Convert.ToInt32(flag));
        }

        public void DelFlag(T flag)
        {
            _flags &= ~(dynamic)(1ul << Convert.ToInt32(flag));
        }

        public int GetValue(T flag)
        {
            return _values[Convert.ToInt32(flag)];
        }

        public void SetValue(T flag, object value)
        {
            _values[Convert.ToInt32(flag)] = Convert.ToInt32(value);
        }

        public void AddValue(T flag, object value)
        {
            _values[Convert.ToInt32(flag)] += Convert.ToInt32(value);
        }
    }
}