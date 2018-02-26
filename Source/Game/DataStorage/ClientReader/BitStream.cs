using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Framework.IO;

namespace Game.DataStorage
{
    public class BitStream : IDisposable
    {
        private byte currentByte;
        private long offset;
        private int bit;
        private Stream stream;
        private Encoding encoding = Encoding.UTF8;
        private bool canWrite = true;

        public long Length => stream.Length;
        public long BitPosition => bit;
        public long Offset => offset;
        private bool ValidPosition => offset < Length;


        public BitStream(int capacity = 0)
        {
            this.stream = new MemoryStream(capacity);
            offset = bit = 0;
            canWrite = true;
            currentByte = 0;
        }

        public BitStream(byte[] buffer)
        {
            this.stream = new MemoryStream(buffer);
            offset = bit = 0;
            canWrite = false;
            currentByte = buffer[0];
        }


        #region Methods		
        public void Seek(long offset, int bit)
        {
            if (offset > Length)
            {
                this.offset = Length;
            }
            else
            {
                if (offset >= 0)
                {
                    this.offset = offset;
                }
                else
                {
                    offset = 0;
                }
            }

            if (bit >= 8)
            {
                this.offset += bit / 8;
                this.bit = bit % 8;
            }
            else
            {
                this.bit = bit;
            }

            UpdateCurrentByte();
        }

        public bool AdvanceBit()
        {
            bit = (bit + 1) % 8;
            if (bit == 0)
            {
                offset++;

                if (canWrite)
                    stream.WriteByte(currentByte);

                UpdateCurrentByte();

                return true;
            }

            return false;
        }

        public byte[] GetStreamData()
        {
            stream.Position = 0;
            MemoryStream s = new MemoryStream();
            stream.CopyTo(s);
            Seek(offset, bit);
            return s.ToArray();
        }

        public bool ChangeLength(long length)
        {
            if (stream.CanSeek && stream.CanWrite)
            {
                stream.SetLength(length);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void CopyStreamTo(Stream stream)
        {
            Seek(0, 0);
            this.stream.CopyTo(stream);
        }

        public MemoryStream CloneAsMemoryStream() => new MemoryStream(GetStreamData());

        #endregion

        #region Bit Read

        private void UpdateCurrentByte()
        {
            stream.Position = offset;

            if (canWrite)
            {
                currentByte = 0;
            }
            else
            {
                currentByte = (byte)stream.ReadByte();
                stream.Position = offset;
            }
        }

        private Bit ReadBit()
        {
            if (!ValidPosition)
                throw new IOException("Cannot read in an offset bigger than the length of the stream");

            byte value = (byte)((currentByte >> (bit)) & 1);
            AdvanceBit();

            return value;
        }

        #endregion

        public byte[] ReadBytes(long length, bool isBytes = false, long byteLength = 0)
        {
            if (isBytes)
                length *= 8;

            byteLength = (byteLength == 0 ? length / 8 : byteLength);

            byte[] data = new byte[byteLength];
            for (long i = 0; i < length;)
            {
                byte value = 0;
                for (int p = 0; p < 8 && i < length; i++, p++)
                    value |= (byte)(ReadBit() << p);

                data[((i + 7) / 8) - 1] = value;
            }

            return data;
        }

        public byte[] ReadBytesPadded(long length)
        {
            int requiredSize = NextPow2((int)(length + 7) / 8);
            byte[] data = ReadBytes(length, false, requiredSize);
            return data;
        }

        public byte ReadByte()
        {
            return ReadBytes(8)[0];
        }

        public byte ReadByte(int bits)
        {
            bits = Math.Min(Math.Max(bits, 0), 8); // clamp values
            return ReadBytes(bits)[0];
        }

        public string ReadString(int length)
        {
            // UTF8 - revert if encoding gets exposed
            return encoding.GetString(ReadBytes(8 * length));
        }

        public short ReadInt16()
        {
            short value = BitConverter.ToInt16(ReadBytes(16), 0);
            return value;
        }

        public int ReadInt32()
        {
            int value = BitConverter.ToInt32(ReadBytes(32), 0);
            return value;
        }

        public long ReadInt64()
        {
            long value = BitConverter.ToInt64(ReadBytes(64), 0);
            return value;
        }

        public ushort ReadUInt16()
        {
            ushort value = BitConverter.ToUInt16(ReadBytes(16), 0);
            return value;
        }

        public uint ReadUInt32(int bitWidth = 32)
        {
            bitWidth = Math.Min(Math.Max(bitWidth, 0), 32); // clamp values

            byte[] data = ReadBytes(bitWidth, false, 4);
            return BitConverter.ToUInt32(data, 0);
        }

        public ulong ReadUInt64()
        {
            ulong value = BitConverter.ToUInt64(ReadBytes(64), 0);
            return value;
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


        public void Dispose()
        {
            ((IDisposable)stream)?.Dispose();
        }

        internal struct Bit
        {
            private byte value;

            public Bit(int value)
            {
                this.value = (byte)(value & 1);
            }

            public static implicit operator Bit(int value) => new Bit(value);

            public static implicit operator byte(Bit bit) => bit.value;
        }

    }

    public class BitReader
    {
        private byte[] m_array;
        private int m_readPos;
        private int m_readOffset;

        public int Position { get => m_readPos; set => m_readPos = value; }
        public int Offset { get => m_readOffset; set => m_readOffset = value; }
        public byte[] Data { get => m_array; set => m_array = value; }

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
            uint result = FastStruct<uint>.ArrayToStructure(ref m_array[m_readOffset + (m_readPos >> 3)]) << (32 - numBits - (m_readPos & 7)) >> (32 - numBits);
            m_readPos += numBits;
            return result;
        }

        public ulong ReadUInt64(int numBits)
        {
            ulong result = FastStruct<ulong>.ArrayToStructure(ref m_array[m_readOffset + (m_readPos >> 3)]) << (64 - numBits - (m_readPos & 7)) >> (64 - numBits);
            m_readPos += numBits;
            return result;
        }

        public Value32 ReadValue32(int numBits)
        {
            unsafe
            {
                ulong result = ReadUInt32(numBits);
                return *(Value32*)&result;
            }
        }

        public Value64 ReadValue64(int numBits)
        {
            unsafe
            {
                ulong result = ReadUInt64(numBits);
                return *(Value64*)&result;
            }
        }

        // this will probably work in C# 7.3 once blittable generic constrain added, or not...
        //public unsafe T Read<T>(int numBits) where T : struct
        //{
        //    //fixed (byte* ptr = &m_array[m_readOffset + (m_readPos >> 3)])
        //    //{
        //    //    T val = *(T*)ptr << (sizeof(T) - numBits - (m_readPos & 7)) >> (sizeof(T) - numBits);
        //    //    m_readPos += numBits;
        //    //    return val;
        //    //}
        //    //T result = FastStruct<T>.ArrayToStructure(ref m_array[m_readOffset + (m_readPos >> 3)]) << (32 - numBits - (m_readPos & 7)) >> (32 - numBits);
        //    //m_readPos += numBits;
        //    //return result;
        //}
    }

    public struct Value32
    {
        unsafe fixed byte Value[4];

        public T GetValue<T>() where T : struct
        {
            unsafe
            {
                fixed (byte* ptr = Value)
                    return FastStruct<T>.ArrayToStructure(ref ptr[0]);
            }
        }

        public byte[] GetBytes(int bitSize)
        {
            byte[] data = new byte[NextPow2((int)(bitSize + 7) / 8)];
            unsafe
            {
                fixed (byte* ptr = Value)
                {
                    for (var i = 0; i < data.Length; ++i)
                        data[i] = ptr[i];
                }
            }

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
    }

    public struct Value64
    {
        unsafe fixed byte Value[8];

        public T GetValue<T>() where T : struct
        {
            unsafe
            {
                fixed (byte* ptr = Value)
                    return FastStruct<T>.ArrayToStructure(ref ptr[0]);
            }
        }

        public byte[] GetBytes(int bitSize)
        {
            byte[] data = new byte[NextPow2((int)(bitSize + 7) / 8)];
            unsafe
            {
                fixed (byte* ptr = Value)
                {
                    for (var i = 0; i < data.Length; ++i)
                        data[i] = ptr[i];
                }
            }

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
    }
}
