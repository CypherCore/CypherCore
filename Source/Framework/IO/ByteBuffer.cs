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

using Framework.GameMath;
using System;
using System.IO;
using System.Text;

namespace Framework.IO
{
    public class ByteBuffer : IDisposable
    {
        public ByteBuffer()
        {
            writeStream = new BinaryWriter(new MemoryStream());
        }

        public ByteBuffer(byte[] data)
        {
            readStream = new BinaryReader(new MemoryStream(data));
        }

        public void Dispose()
        {
            if (writeStream != null)
                writeStream.Dispose();

            if (readStream != null)
                readStream.Dispose();
        }

        #region Read Methods
        public sbyte ReadInt8()
        {
            ResetBitPos();
            return readStream.ReadSByte();
        }

        public short ReadInt16()
        {
            ResetBitPos();
            return readStream.ReadInt16();
        }

        public int ReadInt32()
        {
            ResetBitPos();
            return readStream.ReadInt32();
        }

        public long ReadInt64()
        {
            ResetBitPos();
            return readStream.ReadInt64();
        }

        public byte ReadUInt8()
        {
            ResetBitPos();
            return readStream.ReadByte();
        }

        public ushort ReadUInt16()
        {
            ResetBitPos();
            return readStream.ReadUInt16();
        }

        public uint ReadUInt32()
        {
            ResetBitPos();
            return readStream.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            ResetBitPos();
            return readStream.ReadUInt64();
        }

        public float ReadFloat()
        {
            ResetBitPos();
            return readStream.ReadSingle();
        }

        public double ReadDouble()
        {
            ResetBitPos();
            return readStream.ReadDouble();
        }

        public string ReadCString()
        {
            ResetBitPos();
            StringBuilder tmpString = new StringBuilder();
            char tmpChar = readStream.ReadChar();
            char tmpEndChar = Convert.ToChar(Encoding.UTF8.GetString(new byte[] { 0 }));

            while (tmpChar != tmpEndChar)
            {
                tmpString.Append(tmpChar);
                tmpChar = readStream.ReadChar();
            }

            return tmpString.ToString();
        }

        public string ReadString(uint length)
        {
            if (length == 0)
                return "";

            ResetBitPos();
            return Encoding.UTF8.GetString(ReadBytes(length));
        }

        public bool ReadBool()
        {
            ResetBitPos();
            return readStream.ReadBoolean();
        }

        public byte[] ReadBytes(uint count)
        {
            ResetBitPos();
            return readStream.ReadBytes((int)count);
        }

        public void Skip(int count)
        {
            ResetBitPos();
            readStream.BaseStream.Position += count;
        }

        public uint ReadPackedTime()
        {
            uint packedDate = ReadUInt32();
            var time = new DateTime((int)((packedDate >> 24) & 0x1F) + 2000, (int)((packedDate >> 20) & 0xF) + 1, (int)((packedDate >> 14) & 0x3F) + 1, (int)(packedDate >> 6) & 0x1F, (int)(packedDate & 0x3F), 0);
            return (uint)Time.DateTimeToUnixTime(time);
        }

        public Vector3 ReadVector3()
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }

        //BitPacking
        public byte ReadBit()
        {
            if (BitPosition == 8)
            {
                BitValue = ReadUInt8();
                BitPosition = 0;
            }

            int returnValue = BitValue;
            BitValue = (byte)(2 * returnValue);
            ++BitPosition;

            return (byte)(returnValue >> 7);
        }

        public bool HasBit()
        {
            if (BitPosition == 8)
            {
                BitValue = ReadUInt8();
                BitPosition = 0;
            }

            int returnValue = BitValue;
            BitValue = (byte)(2 * returnValue);
            ++BitPosition;

            return Convert.ToBoolean(returnValue >> 7);
        }

        public T ReadBits<T>(int bitCount)
        {
            int value = 0;

            for (var i = bitCount - 1; i >= 0; --i)
                if (HasBit())
                    value |= (1 << i);

            return (T)Convert.ChangeType(value, typeof(T));
        }
        #endregion

        #region Write Methods
        public void WriteInt8<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToSByte(data));
        }

        public void WriteInt16<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToInt16(data));
        }

        public void WriteInt32<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToInt32(data));
        }

        public void WriteInt64<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToInt64(data));
        }

        public void WriteUInt8<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToByte(data));
        }

        public void WriteUInt16<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToUInt16(data));
        }

        public void WriteUInt32<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToUInt32(data));
        }

        public void WriteUInt64<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToUInt64(data));
        }

        public void WriteFloat<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToSingle(data));
        }

        public void WriteDouble<T>(T data)
        {
            FlushBits();
            writeStream.Write(Convert.ToDouble(data));
        }

        /// <summary>
        /// Writes a string to the packet with a null terminated (0)
        /// </summary>
        /// <param name="str"></param>
        public void WriteCString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                WriteUInt8(0);
                return;
            }

            WriteString(str);
            WriteUInt8(0);
        }

        public void WriteString(string str)
        {
            if (str.IsEmpty())
                return;

            byte[] sBytes = Encoding.UTF8.GetBytes(str);
            WriteBytes(sBytes);
        }

        public void WriteBytes(byte[] data)
        {
            FlushBits();
            writeStream.Write(data, 0, data.Length);
        }

        public void WriteBytes(byte[] data, uint count)
        {
            FlushBits();
            writeStream.Write(data, 0, (int)count);
        }

        public void WriteBytes(ByteBuffer buffer)
        {
            WriteBytes(buffer.GetData());
        }

        public void WriteVector3(Vector3 pos)
        {
            WriteFloat(pos.X);
            WriteFloat(pos.Y);
            WriteFloat(pos.Z);
        }

        public void WriteVector2(Vector2 pos)
        {
            WriteFloat(pos.X);
            WriteFloat(pos.Y);
        }

        public void WritePackXYZ(Vector3 pos)
        {
            uint packed = 0;
            packed |= ((uint)(pos.X / 0.25f) & 0x7FF);
            packed |= ((uint)(pos.Y / 0.25f) & 0x7FF) << 11;
            packed |= ((uint)(pos.Z / 0.25f) & 0x3FF) << 22;
            WriteUInt32(packed);
        }

        public bool WriteBit(object bit)
        {
            --BitPosition;

            if (Convert.ToBoolean(bit))
                BitValue |= (byte)(1 << BitPosition);

            if (BitPosition == 0)
            {
                writeStream.Write(BitValue);

                BitPosition = 8;
                BitValue = 0;
            }
            return Convert.ToBoolean(bit);
        }

        public void WriteBits(object bit, int count)
        {
            for (int i = count - 1; i >= 0; --i)
                WriteBit((Convert.ToInt32(bit) >> i) & 1);
        }

        public void WritePackedTime(long time)
        {
            var now = Time.UnixTimeToDateTime(time);
            WriteUInt32(Convert.ToUInt32((now.Year - 2000) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute));
        }

        public void WritePackedTime()
        {
            DateTime now = DateTime.Now;
            WriteUInt32(Convert.ToUInt32((now.Year - 2000) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute));
        }
        #endregion

        public void FlushBits()
        {
            if (BitPosition == 8)
                return;

            writeStream.Write(BitValue);
            BitValue = 0;
            BitPosition = 8;
        }

        public void ResetBitPos()
        {
            if (BitPosition > 7)
                return;

            BitPosition = 8;
            BitValue = 0;
        }

        public byte[] GetData()
        {
            Stream stream = GetCurrentStream();

            var data = new byte[stream.Length];

            long pos = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)stream.ReadByte();

            stream.Seek(pos, SeekOrigin.Begin);
            return data;
        }

        public uint GetSize()
        {
            return (uint)GetCurrentStream().Length;
        }

        public Stream GetCurrentStream()
        {
            if (writeStream != null)
                return writeStream.BaseStream;
            else
                return readStream.BaseStream;
        }

        public void Clear()
        {
            BitPosition = 8;
            BitValue = 0;
            writeStream = new BinaryWriter(new MemoryStream());
        }

        byte BitPosition = 8;
        byte BitValue;
        BinaryWriter writeStream;
        BinaryReader readStream;
    }
}
