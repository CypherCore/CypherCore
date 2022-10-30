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

using Framework.Constants;
using Framework.IO;

namespace Game.Networking.Packets
{
    public enum WardenOpcodes
    {
        // Client.Server
        CmsgModuleMissing = 0,
        CmsgModuleOk = 1,
        CmsgCheatChecksResult = 2,
        CmsgMemChecksResult = 3,        // Only Sent If Mem_Check Bytes Doesn'T Match
        CmsgHashResult = 4,
        CmsgModuleFailed = 5,        // This Is Sent When Client Failed To Load Uploaded Module Due To Cache Fail

        // Server.Client
        SmsgModuleUse = 0,
        SmsgModuleCache = 1,
        SmsgCheatChecksRequest = 2,
        SmsgModuleInitialize = 3,
        SmsgMemChecksRequest = 4,        // Byte Len; While (!Eof) { Byte Unk(1); Byte Index(++); String Module(Can Be 0); Int Offset; Byte Len; Byte[] Bytes_To_Compare[Len]; }
        SmsgHashRequest = 5
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

    class Warden3DataServer : ServerPacket
    {
        public Warden3DataServer() : base(ServerOpcodes.Warden3Data) { }

        public override void Write()
        {
            _worldPacket.WriteBytes(Data);
        }

        public ByteBuffer Data;
    }
}
