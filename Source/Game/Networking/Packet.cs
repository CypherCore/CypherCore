// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using System;
using System.Numerics;

namespace Game.Networking
{
    public abstract class ClientPacket : IDisposable
    {
        protected ClientPacket(WorldPacket worldPacket)
        {
            _worldPacket = worldPacket;
        }

        public abstract void Read();

        public void Dispose()
        {
            _worldPacket.Dispose();
        }

        public ClientOpcodes GetOpcode() { return (ClientOpcodes)_worldPacket.GetOpcode(); }

        public void LogPacket(WorldSession session)
        {
            Log.outDebug(LogFilter.Network, "Received ClientOpcode: {0} From: {1}", GetOpcode(), session != null ? session.GetPlayerInfo() : "Unknown IP");
        }

        public WorldPacket GetWorldPacket()
        {
            return _worldPacket;
        }

        protected WorldPacket _worldPacket;
    }

    public abstract class ServerPacket
    {
        protected ServerPacket(ServerOpcodes opcode)
        {
            connectionType = ConnectionType.Realm;
            _worldPacket = new WorldPacket(opcode);
        }

        protected ServerPacket(ServerOpcodes opcode, ConnectionType type = ConnectionType.Realm)
        {
            connectionType = type;
            _worldPacket = new WorldPacket(opcode);
        }

        public void Clear()
        {
            _worldPacket.Clear();
            buffer = null;
        }

        public ServerOpcodes GetOpcode()
        {
            return (ServerOpcodes)_worldPacket.GetOpcode();
        }

        public byte[] GetData()
        {
            return buffer;
        }

        public void LogPacket(WorldSession session)
        {
            Log.outDebug(LogFilter.Network, "Sent ServerOpcode: {0} To: {1}", GetOpcode(), session != null ? session.GetPlayerInfo() : "");
        }

        public abstract void Write();

        public void WritePacketData()
        {
            if (buffer != null)
                return;

            Write();

            buffer = _worldPacket.GetData();
            _worldPacket.Dispose();
        }

        public ConnectionType GetConnection() { return connectionType; }

        public bool IsValidOpcode()
        {
            return _worldPacket.IsValidOpcode();
        }

        byte[] buffer;
        ConnectionType connectionType;
        protected WorldPacket _worldPacket;
    }

    public class WorldPacket : ByteBuffer
    {
        public WorldPacket(ServerOpcodes opcode = ServerOpcodes.None)
        {
            this.opcode = (uint)opcode;
        }

        public WorldPacket(ClientOpcodes opcode)
        {
            this.opcode = (uint)opcode;
        }

        public WorldPacket(byte[] data) : base(data)
        {
            opcode = ReadUInt32();
        }

        public ObjectGuid ReadPackedGuid()
        {
            var loLength = ReadUInt8();
            var hiLength = ReadUInt8();
            var low = ReadPackedUInt64(loLength);
            return new ObjectGuid(ReadPackedUInt64(hiLength), low);
        }

        private ulong ReadPackedUInt64(byte length)
        {
            if (length == 0)
                return 0;

            var guid = 0ul;

            for (var i = 0; i < 8; i++)
                if ((1 << i & length) != 0)
                    guid |= (ulong)ReadUInt8() << (i * 8);

            return guid;
        }

        public Position ReadPosition()
        {
            return new Position(ReadFloat(), ReadFloat(), ReadFloat());
        }

        public void WritePackedGuid(ObjectGuid guid)
        {
            if (guid.IsEmpty())
            {
                WriteUInt8(0);
                WriteUInt8(0);
                return;
            }

            byte lowMask, highMask;
            byte[] lowPacked, highPacked;

            var loSize = PackUInt64(guid.GetLowValue(), out lowMask, out lowPacked);
            var hiSize = PackUInt64(guid.GetHighValue(), out highMask, out highPacked);

            WriteUInt8(lowMask);
            WriteUInt8(highMask);
            WriteBytes(lowPacked, loSize);
            WriteBytes(highPacked, hiSize);
        }

        public void WritePackedUInt64(ulong guid)
        {
            byte mask;
            byte[] packed;
            var packedSize = PackUInt64(guid, out mask, out packed);

            WriteUInt8(mask);
            WriteBytes(packed, packedSize);
        }

        uint PackUInt64(ulong value, out byte mask, out byte[] result)
        {
            uint resultSize = 0;
            mask = 0;
            result = new byte[8];

            for (byte i = 0; value != 0; ++i)
            {
                if ((value & 0xFF) != 0)
                {
                    mask |= (byte)(1 << i);
                    result[resultSize++] = (byte)(value & 0xFF);
                }

                value >>= 8;
            }

            return resultSize;
        }

        public void WriteBytes(WorldPacket data)
        {
            FlushBits();
            WriteBytes(data.GetData());
        }

        public void WriteXYZ(Position pos)
        {
            if (pos == null)
                return;

            float x, y, z;
            pos.GetPosition(out x, out y, out z);
            WriteFloat(x);
            WriteFloat(y);
            WriteFloat(z);
        }
        public void WriteXYZO(Position pos)
        {
            float x, y, z, o;
            pos.GetPosition(out x, out y, out z, out o);
            WriteFloat(x);
            WriteFloat(y);
            WriteFloat(z);
            WriteFloat(o);
        }

        public void Write<T>(T value)
        {
            switch (value)
            {
                case sbyte v: WriteInt8(v); break;
                case short v: WriteInt16(v); break;
                case int v: WriteInt32(v); break;
                case long v: WriteInt64(v); break;
                case byte v: WriteUInt8(v); break;
                case ushort v: WriteUInt16(v); break;
                case uint v: WriteUInt32(v); break;
                case ulong v: WriteUInt64(v); break;
                case float v: WriteFloat(v); break;
                case double v: WriteDouble(v); break;
                case string v: WriteString(v); break;
                case bool v: WriteBit(v); break;
                case byte[] v: WriteBytes(v); break;
                case Vector3 v: WriteVector3(v); break;
                case ObjectGuid v: WritePackedGuid(v); break;
                default:
                    throw new InvalidOperationException($"Type {typeof(T)} is not supported for writing.");
            }
        }

        public uint GetOpcode() { return opcode; }

        public DateTime GetReceivedTime() { return m_receivedTime; }
        public void SetReceiveTime(DateTime receivedTime) { m_receivedTime = receivedTime; }

        public bool IsValidOpcode()
        {
            int opcodeArrayIndex = GetOpcodeArrayIndex(opcode);
            return opcodeArrayIndex >= 0 && opcodeArrayIndex < 1977;
        }

        int GetOpcodeArrayIndex(uint opcode)
        {
            int idInGroup = (int)(opcode & 0xFFFF);
            switch (opcode >> 16)
            {
                case 0x2A: return idInGroup < 55 ? idInGroup + 0 : -1;
                case 0x2C: return idInGroup < 51 ? idInGroup + 55 : -1;
                case 0x2D: return idInGroup < 3 ? idInGroup + 106 : -1;
                case 0x2E: return idInGroup < 39 ? idInGroup + 109 : -1;
                case 0x2F: return idInGroup < 2 ? idInGroup + 148 : -1;
                case 0x30: return idInGroup < 2 ? idInGroup + 150 : -1;
                case 0x31: return idInGroup < 9 ? idInGroup + 152 : -1;
                case 0x32: return idInGroup < 20 ? idInGroup + 161 : -1;
                case 0x33: return idInGroup < 8 ? idInGroup + 181 : -1;
                case 0x34: return idInGroup < 9 ? idInGroup + 189 : -1;
                case 0x35: return idInGroup < 39 ? idInGroup + 198 : -1;
                case 0x37: return idInGroup < 9 ? idInGroup + 237 : -1;
                case 0x39: return idInGroup < 8 ? idInGroup + 246 : -1;
                case 0x3A: return idInGroup < 16 ? idInGroup + 254 : -1;
                case 0x3B: return idInGroup < 16 ? idInGroup + 270 : -1;
                case 0x3D: return idInGroup < 786 ? idInGroup + 286 : -1;
                case 0x3E: return idInGroup < 311 ? idInGroup + 1072 : -1;
                case 0x3F: return idInGroup < 1 ? idInGroup + 1383 : -1;
                case 0x40: return idInGroup < 10 ? idInGroup + 1384 : -1;
                case 0x41: return idInGroup < 137 ? idInGroup + 1394 : -1;
                case 0x43: return idInGroup < 428 ? idInGroup + 1531 : -1;
                case 0x44: return idInGroup < 18 ? idInGroup + 1959 : -1;
                case 0x45: return idInGroup < 903 ? idInGroup + 0 : -1;
                case 0x46: return idInGroup < 5 ? idInGroup + 903 : -1;
                case 0x49: return idInGroup < 20 ? idInGroup + 908 : -1;
                case 0x4A: return idInGroup < 37 ? idInGroup + 928 : -1;
                case 0x4B: return idInGroup < 49 ? idInGroup + 965 : -1;
                case 0x4C: return idInGroup < 15 ? idInGroup + 1014 : -1;
                case 0x4D: return idInGroup < 12 ? idInGroup + 1029 : -1;
                case 0x4F: return idInGroup < 82 ? idInGroup + 1041 : -1;
                case 0x51: return idInGroup < 72 ? idInGroup + 1123 : -1;
                case 0x53: return idInGroup < 2 ? idInGroup + 1195 : -1;
                case 0x54: return idInGroup < 8 ? idInGroup + 1197 : -1;
                case 0x55: return idInGroup < 13 ? idInGroup + 1205 : -1;
                case 0x56: return idInGroup < 8 ? idInGroup + 1218 : -1;
                case 0x57: return idInGroup < 8 ? idInGroup + 1226 : -1;
                case 0x58: return idInGroup < 37 ? idInGroup + 1234 : -1;
                case 0x59: return idInGroup < 10 ? idInGroup + 1271 : -1;
                case 0x5A: return idInGroup < 35 ? idInGroup + 1281 : -1;
                case 0x5C: return idInGroup < 1 ? idInGroup + 1316 : -1;
                case 0x5E: return idInGroup < 130 ? idInGroup + 1317 : -1;
                case 0x5F: return idInGroup < 6 ? idInGroup + 1447 : -1;
                case 0x60: return idInGroup < 20 ? idInGroup + 1453 : -1;
                case 0x62: return idInGroup < 1 ? idInGroup + 1473 : -1;
                case 0x63: return idInGroup < 8 ? idInGroup + 1474 : -1;
                case 0x64: return idInGroup < 52 ? idInGroup + 1482 : -1;
                case 0x65: return idInGroup < 41 ? idInGroup + 1534 : -1;
                case 0x67: return idInGroup < 87 ? idInGroup + 1575 : -1;
                case 0x68: return idInGroup < 8 ? idInGroup + 1662 : -1;
                case 0x6A: return idInGroup < 1 ? idInGroup + 1670 : -1;
                default: return -1;
            }
        }

        uint opcode;
        DateTime m_receivedTime; // only set for a specific set of opcodes, for performance reasons.
    }

    class PacketHeader
    {
        public int Size;
        public byte[] Tag = new byte[12];

        public void Read(byte[] buffer)
        {
            Size = BitConverter.ToInt32(buffer, 0);
            Buffer.BlockCopy(buffer, 4, Tag, 0, 12);
        }

        public void Write(ByteBuffer byteBuffer)
        {
            byteBuffer.WriteInt32(Size);
            byteBuffer.WriteBytes(Tag, 12);
        }

        public bool IsValidSize() { return Size < 0x40000; }
    }
}
