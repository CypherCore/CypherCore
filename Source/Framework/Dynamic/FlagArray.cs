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

namespace Framework.Dynamic
{
    public class FlagArray128
    {
        public FlagArray128(params uint[] parts)
        {
            _values = new uint[4];
            for (var i = 0; i < parts.Length; ++i)
                _values[i] = parts[i];
        }

        public bool IsEqual(params uint[] parts)
        {
            for (var i = 0; i < _values.Length; ++i)
                if (_values[i] == parts[i])
                    return false;

            return true;
        }

        public void Set(params uint[] parts)
        {
            for (var i = 0; i < parts.Length; ++i)
                _values[i] = parts[i];
        }

        public static bool operator <(FlagArray128 left, FlagArray128 right)
        {
            for (var i = left._values.Length; i > 0; --i)
            {
                if (left._values[i - 1] < right._values[i - 1])
                    return true;
                else if (left._values[i - 1] > right._values[i - 1])
                    return false;
            }
            return false;
        }
        public static bool operator >(FlagArray128 left, FlagArray128 right)
        {
            for (var i = left._values.Length; i > 0; --i)
            {
                if (left._values[i - 1] > right._values[i - 1])
                    return true;
                else if (left._values[i - 1] < right._values[i - 1])
                    return false;
            }
            return false;
        }

        public static FlagArray128 operator &(FlagArray128 left, FlagArray128 right)
        {
            FlagArray128 fl = new FlagArray128();
            for (var i = 0; i < left._values.Length; ++i)
                fl[i] = left._values[i] & right._values[i];
            return fl;
        }
        public static FlagArray128 operator |(FlagArray128 left, FlagArray128 right)
        {
            FlagArray128 fl = new FlagArray128();
            for (var i = 0; i < left._values.Length; ++i)
                fl[i] = left._values[i] | right._values[i];
            return fl;
        }
        public static FlagArray128 operator ^(FlagArray128 left, FlagArray128 right)
        {
            FlagArray128 fl = new FlagArray128();
            for (var i = 0; i < left._values.Length; ++i)
                fl[i] = left._values[i] ^ right._values[i];
            return fl;
        }

        public static implicit operator bool (FlagArray128 left)
        {
            for (var i = 0; i < left._values.Length; ++i)
                if (left._values[i] != 0)
                    return true;

            return false;
        }

        public uint this[int i]
        {
            get
            {
                return _values[i];
            }
            set
            {
                _values[i] = value;
            }
        }

        uint[] _values { get; set; }
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
