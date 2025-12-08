// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using System;

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

        public uint GetOpcode() { return opcode; }

        public DateTime GetReceivedTime() { return m_receivedTime; }
        public void SetReceiveTime(DateTime receivedTime) { m_receivedTime = receivedTime; }

        public bool IsValidOpcode()
        {
            int opcodeArrayIndex = GetOpcodeArrayIndex(opcode);
            return opcodeArrayIndex >= 0 && opcodeArrayIndex < 1735;
        }

        int GetOpcodeArrayIndex(uint opcode)
        {
            int idInGroup = (int)(opcode & 0xFFFF);
            switch (opcode >> 16)
            {
                case 0x29: return idInGroup < 31 ? idInGroup + 0 : -1;
                case 0x2B: return idInGroup < 51 ? idInGroup + 31 : -1;
                case 0x2C: return idInGroup < 3 ? idInGroup + 82 : -1;
                case 0x2D: return idInGroup < 37 ? idInGroup + 85 : -1;
                case 0x2F: return idInGroup < 751 ? idInGroup + 122 : -1;
                case 0x30: return idInGroup < 301 ? idInGroup + 873 : -1;
                case 0x31: return idInGroup < 10 ? idInGroup + 1174 : -1;
                case 0x32: return idInGroup < 132 ? idInGroup + 1184 : -1;
                case 0x34: return idInGroup < 404 ? idInGroup + 1316 : -1;
                case 0x35: return idInGroup < 15 ? idInGroup + 1720 : -1;
                case 0x36: return idInGroup < 857 ? idInGroup + 0 : -1;
                case 0x37: return idInGroup < 5 ? idInGroup + 857 : -1;
                case 0x3A: return idInGroup < 18 ? idInGroup + 862 : -1;
                case 0x3B: return idInGroup < 36 ? idInGroup + 880 : -1;
                case 0x3C: return idInGroup < 49 ? idInGroup + 916 : -1;
                case 0x3D: return idInGroup < 11 ? idInGroup + 965 : -1;
                case 0x3E: return idInGroup < 12 ? idInGroup + 976 : -1;
                case 0x40: return idInGroup < 82 ? idInGroup + 988 : -1;
                case 0x42: return idInGroup < 71 ? idInGroup + 1070 : -1;
                case 0x44: return idInGroup < 33 ? idInGroup + 1141 : -1;
                case 0x46: return idInGroup < 1 ? idInGroup + 1174 : -1;
                case 0x48: return idInGroup < 120 ? idInGroup + 1175 : -1;
                case 0x4A: return idInGroup < 49 ? idInGroup + 1295 : -1;
                case 0x4B: return idInGroup < 41 ? idInGroup + 1344 : -1;
                case 0x4D: return idInGroup < 85 ? idInGroup + 1385 : -1;
                case 0x4E: return idInGroup < 8 ? idInGroup + 1470 : -1;
                case 0x50: return idInGroup < 1 ? idInGroup + 1478 : -1;
                default: return -1;
            }
        }

        uint opcode;
        DateTime m_receivedTime; // only set for a specific set of opcodes, for performance reasons.
    }

    public class PacketHeader
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
