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

using System.Diagnostics.Contracts;

namespace System.Collections
{
    public class BitSet : ICollection, ICloneable
    {
        public BitSet(int length, bool defaultValue = false)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            Contract.EndContractBlock();

            m_array = new uint[GetArrayLength(length, BitsPerInt32)];
            m_length = length;

            uint fillValue = defaultValue ? 0xffffffff : 0;
            for (int i = 0; i < m_array.Length; i++)
            {
                m_array[i] = fillValue;
            }

            _version = 0;
        }

        public BitSet(uint[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            Contract.EndContractBlock();
            // this value is chosen to prevent overflow when computing m_length
            if (values.Length > UInt32.MaxValue / BitsPerInt32)
            {
                throw new ArgumentException();
            }

            m_array = new uint[values.Length];
            m_length = values.Length * BitsPerInt32;

            Array.Copy(values, m_array, values.Length);

            _version = 0;
        }

        public BitSet(BitSet bits)
        {
            if (bits == null)
            {
                throw new ArgumentNullException("bits");
            }
            Contract.EndContractBlock();

            int arrayLength = GetArrayLength(bits.m_length, BitsPerInt32);
            m_array = new uint[arrayLength];
            m_length = bits.m_length;

            Array.Copy(bits.m_array, m_array, arrayLength);

            _version = bits._version;
        }

        public bool this[int index]
        {
            get
            {
                return Get(index);
            }
            set
            {
                Set(index, value);
            }
        }

        public bool Get(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            Contract.EndContractBlock();

            return (Convert.ToInt64(m_array[index / 32]) & (1 << (index % 32))) != 0;
        }

        public void Set(int index, bool value)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            Contract.EndContractBlock();

            if (value)
            {
                m_array[index / 32] |= (1u << (index % 32));
            }
            else
            {
                m_array[index / 32] &= ~(1u << (index % 32));
            }

            _version++;
        }

        public void SetAll(bool value)
        {
            uint fillValue = value ? 0xffffffff : 0u;
            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] = fillValue;
            }

            _version++;
        }

        public BitSet And(BitSet value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (Length != value.Length)
                throw new ArgumentException();
            Contract.EndContractBlock();

            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] &= value.m_array[i];
            }

            _version++;
            return this;
        }

        public BitSet Or(BitSet value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (Length != value.Length)
                throw new ArgumentException();
            Contract.EndContractBlock();

            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] |= value.m_array[i];
            }

            _version++;
            return this;
        }

        public BitSet Xor(BitSet value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (Length != value.Length)
                throw new ArgumentException();
            Contract.EndContractBlock();

            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] ^= value.m_array[i];
            }

            _version++;
            return this;
        }

        public BitSet Not()
        {
            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] = ~m_array[i];
            }

            _version++;
            return this;
        }

        public int Length
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return m_length;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                Contract.EndContractBlock();

                int newints = GetArrayLength(value, BitsPerInt32);
                if (newints > m_array.Length || newints + _ShrinkThreshold < m_array.Length)
                {
                    // grow or shrink (if wasting more than _ShrinkThreshold ints)
                    uint[] newarray = new uint[newints];
                    Array.Copy(m_array, newarray, newints > m_array.Length ? m_array.Length : newints);
                    m_array = newarray;
                }

                if (value > m_length)
                {
                    // clear high bit values in the last int
                    int last = GetArrayLength(m_length, BitsPerInt32) - 1;
                    int bits = m_length % 32;
                    if (bits > 0)
                    {
                        m_array[last] &= (1u << bits) - 1;
                    }

                    // clear remaining int values
                    Array.Clear(m_array, last + 1, newints - last - 1);
                }

                m_length = value;
                _version++;
            }
        }

        // ICollection implementation
        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (array.Rank != 1)
                throw new ArgumentException();

            Contract.EndContractBlock();

            if (array is uint[])
            {
                Array.Copy(m_array, 0, array, index, GetArrayLength(m_length, BitsPerInt32));
            }
            else if (array is byte[])
            {
                int arrayLength = GetArrayLength(m_length, BitsPerByte);
                if ((array.Length - index) < arrayLength)
                    throw new ArgumentException();

                byte[] b = (byte[])array;
                for (int i = 0; i < arrayLength; i++)
                    b[index + i] = (byte)((m_array[i / 4] >> ((i % 4) * 8)) & 0x000000FF); // Shift to bring the required byte to LSB, then mask
            }
            else if (array is bool[])
            {
                if (array.Length - index < m_length)
                    throw new ArgumentException();

                bool[] b = (bool[])array;
                for (int i = 0; i < m_length; i++)
                    b[index + i] = ((m_array[i / 32] >> (i % 32)) & 0x00000001) != 0;
            }
            else
                throw new ArgumentException();
        }

        public int Count
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);

                return (int)m_length;
            }
        }

        public Object Clone()
        {
            Contract.Ensures(Contract.Result<Object>() != null);
            Contract.Ensures(((BitArray)Contract.Result<Object>()).Length == this.Length);

            BitSet bitArray = new BitSet(m_array);
            bitArray._version = _version;
            bitArray.m_length = m_length;
            return bitArray;
        }

        public Object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new BitArrayEnumeratorSimple(this);
        }

        // XPerY=n means that n Xs can be stored in 1 Y. 
        private const int BitsPerInt32 = 32;
        private const int BytesPerInt32 = 4;
        private const int BitsPerByte = 8;

        private static int GetArrayLength(int n, int div)
        {
            Contract.Assert(div > 0, "GetArrayLength: div arg must be greater than 0");
            return n > 0 ? (((n - 1) / div) + 1) : 0;
        }

        [Serializable]
        private class BitArrayEnumeratorSimple : IEnumerator, ICloneable
        {
            private BitSet bitarray;
            private int index;
            private int version;
            private bool currentElement;

            internal BitArrayEnumeratorSimple(BitSet bitarray)
            {
                this.bitarray = bitarray;
                this.index = -1;
                version = bitarray._version;
            }

            public Object Clone()
            {
                return MemberwiseClone();
            }

            public bool MoveNext()
            {
                //if (version != bitarray._version) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                if (index < (bitarray.Count - 1))
                {
                    index++;
                    currentElement = bitarray.Get(index);
                    return true;
                }
                else
                    index = bitarray.Count;

                return false;
            }

            public virtual Object Current
            {
                get
                {
                    if (index == -1)
                        throw new InvalidOperationException();
                    if (index >= bitarray.Count)
                        throw new InvalidOperationException();
                    return currentElement;
                }
            }

            public void Reset()
            {
                //if (version != bitarray._version) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                index = -1;
            }
        }

        private uint[] m_array;
        private int m_length;
        private int _version;
        [NonSerialized]
        private Object _syncRoot;

        private const int _ShrinkThreshold = 256;
    }
}