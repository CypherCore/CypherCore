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

using Framework.Constants;
using Framework.IO;

namespace Game.Network.Packets
{
    public enum WardenOpcodes
    {
        // Client->Server
        CMSG_ModuleMissing = 0,
        Cmsg_ModuleOk = 1,
        Cmsg_CheatChecksResult = 2,
        Cmsg_MemChecksResult = 3,        // Only Sent If Mem_Check Bytes Doesn'T Match
        Cmsg_HashResult = 4,
        Cmsg_ModuleFailed = 5,        // This Is Sent When Client Failed To Load Uploaded Module Due To Cache Fail

        // Server->Client
        Smsg_ModuleUse = 0,
        Smsg_ModuleCache = 1,
        Smsg_CheatChecksRequest = 2,
        Smsg_ModuleInitialize = 3,
        Smsg_MemChecksRequest = 4,        // Byte Len; While (!Eof) { Byte Unk(1); Byte Index(++); String Module(Can Be 0); Int Offset; Byte Len; Byte[] Bytes_To_Compare[Len]; }
        Smsg_HashRequest = 5
    }

    class WardenData : ClientPacket
    {
        public WardenData(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint size = _worldPacket.ReadUInt32();

            if (size != 0)
                Data = new ByteBuffer(_worldPacket.ReadBytes(size));
        }

        public ByteBuffer Data;
    }

    class WardenDataServer : ServerPacket
    {
        public WardenDataServer() : base(ServerOpcodes.WardenData) { }

        public override void Write()
        {
            _worldPacket.WriteBytes(Data);
        }

        public ByteBuffer Data;
    }

    class WardenModuleTransfer : ByteBuffer
    {
        public byte[] Write()
        {
            WriteUInt8(Command);
            WriteUInt16(DataSize);
            WriteBytes(Data, 500);

            return GetData();
        }

        public WardenOpcodes Command;
        public ushort DataSize;
        public byte[] Data = new byte[500];
    }

    class WardenModuleUse : ByteBuffer
    {
        public byte[] Write()
        {
            WriteUInt8(Command);
            WriteBytes(ModuleId, 16);
            WriteBytes(ModuleKey, 16);
            WriteUInt32(Size);

            return GetData();
        }

        public WardenOpcodes Command;
        public byte[] ModuleId = new byte[16];
        public byte[] ModuleKey = new byte[16];
        public uint Size;
    }

    class WardenHashRequest : ByteBuffer
    {
        public byte[] Write()
        {
            WriteUInt8(Command);
            WriteBytes(Seed);

            return GetData();
        }

        public WardenOpcodes Command;
        public byte[] Seed = new byte[16];
    }

    class WardenInitModuleRequest : ByteBuffer
    {
        public byte[] Write()
        {
            WriteUInt8(Command1);
            WriteUInt16(Size1);
            WriteUInt32(CheckSumm1);
            WriteUInt8(Unk1);
            WriteUInt8(Unk2);
            WriteUInt8(Type);
            WriteUInt8(String_library1);
            foreach (var function in Function1)
                WriteUInt32(function);

            WriteUInt8(Command2);
            WriteUInt16(Size2);
            WriteUInt32(CheckSumm2);
            WriteUInt8(Unk3);
            WriteUInt8(Unk4);
            WriteUInt8(String_library2);
            WriteUInt32(Function2);
            WriteUInt8(Function2_set);

            WriteUInt8(Command3);
            WriteUInt16(Size3);
            WriteUInt32(CheckSumm3);
            WriteUInt8(Unk5);
            WriteUInt8(Unk6);
            WriteUInt8(String_library3);
            WriteUInt32(Function3);
            WriteUInt8(Function3_set);

            return GetData();
        }

        public WardenOpcodes Command1;
        public ushort Size1;
        public uint CheckSumm1;
        public byte Unk1;
        public byte Unk2;
        public byte Type;
        public byte String_library1;
        public uint[] Function1 = new uint[4];

        public WardenOpcodes Command2;
        public ushort Size2;
        public uint CheckSumm2;
        public byte Unk3;
        public byte Unk4;
        public byte String_library2;
        public uint Function2;
        public byte Function2_set;

        public WardenOpcodes Command3;
        public ushort Size3;
        public uint CheckSumm3;
        public byte Unk5;
        public byte Unk6;
        public byte String_library3;
        public uint Function3;
        public byte Function3_set;
    }
}
