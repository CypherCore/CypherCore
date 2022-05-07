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

namespace Framework.Dynamic
{
    public class FlagsArray<T> where T : struct
    {
        protected dynamic[] _storage;

        public FlagsArray(uint length)
        {
            _storage = new dynamic[length];
        }

        public FlagsArray(params T[] parts)
        {
            _storage = new dynamic[parts.Length];
            for (var i = 0; i < parts.Length; ++i)
                _storage[i] = parts[i];
        }

        public FlagsArray(T[] parts, uint length)
        {
            _storage = new dynamic[length];
            for (var i = 0; i < length && i < parts.Length; ++i)
                _storage[i] = parts[i];
        }

        public static bool operator <(FlagsArray<T> left, FlagsArray<T> right)
        {
            for (var i = left._storage.Length; i > 0; --i)
            {
                if ((dynamic)left._storage[i - 1] < right._storage[i - 1])
                    return true;
                else if ((dynamic)left._storage[i - 1] > right._storage[i - 1])
                    return false;
            }
            return false;
        }
        public static bool operator >(FlagsArray<T> left, FlagsArray<T> right)
        {
            for (var i = left._storage.Length; i > 0; --i)
            {
                if ((dynamic)left._storage[i - 1] > right._storage[i - 1])
                    return true;
                else if ((dynamic)left._storage[i - 1] < right._storage[i - 1])
                    return false;
            }
            return false;
        }

        public static FlagArray128 operator &(FlagsArray<T> left, FlagsArray<T> right)
        {
            FlagArray128 fl = new();
            for (var i = 0; i < left._storage.Length; ++i)
                fl[i] = left._storage[i] & right._storage[i];
            return fl;
        }
        public static FlagArray128 operator |(FlagsArray<T> left, FlagsArray<T> right)
        {
            FlagArray128 fl = new();
            for (var i = 0; i < left._storage.Length; ++i)
                fl[i] = left._storage[i] | right._storage[i];
            return fl;
        }
        public static FlagArray128 operator ^(FlagsArray<T> left, FlagsArray<T> right)
        {
            FlagArray128 fl = new();
            for (var i = 0; i < left._storage.Length; ++i)
                fl[i] = left._storage[i] ^ right._storage[i];
            return fl;
        }

        public static implicit operator bool(FlagsArray<T> left)
        {
            for (var i = 0; i < left._storage.Length; ++i)
                if (left._storage[i] != 0)
                    return true;

            return false;
        }

        public T this[int i]
        {
            get
            {
                return _storage[i];
            }
            set
            {
                _storage[i] = value;
            }
        }
    }

    public class FlagArray128 : FlagsArray<uint>
    {
        public FlagArray128(params uint[] parts) : base(parts, 4) { }

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
    }

    public class FlaggedArray<T> where T : struct
    {
        int[] m_values;
        uint m_flags;

        public FlaggedArray(byte arraysize)
        {
            m_values = new int[4 * arraysize];
        }

        public uint GetFlags() { return m_flags; }
        public bool HasFlag(T flag) { return Convert.ToBoolean(Convert.ToInt32(m_flags) & 1 << Convert.ToInt32(flag)); }
        public void AddFlag(T flag) { m_flags |= (uint)(1 << Convert.ToInt32(flag)); }
        public void DelFlag(T flag) { m_flags &= ~(uint)(1 << Convert.ToInt32(flag)); }

        public int GetValue(T flag) { return m_values[Convert.ToInt32(flag)]; }
        public void SetValue(T flag, object value) { m_values[Convert.ToInt32(flag)] = Convert.ToInt32(value); }
        public void AddValue(T flag, object value) { m_values[Convert.ToInt32(flag)] += Convert.ToInt32(value); }
    }
}
