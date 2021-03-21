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
using Framework.Dynamic;
using Framework.IO;
using Game.Entities;
using System.Collections.Generic;
using System;
using Game.DataStorage;

namespace Game.Networking.Packets
{
    class DBQueryBulk : ClientPacket
    {
        public DBQueryBulk(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TableHash = _worldPacket.ReadUInt32();

            uint count = _worldPacket.ReadBits<uint>(13);
            for (uint i = 0; i < count; ++i)
            {
                Queries.Add(new DBQueryRecord(_worldPacket.ReadUInt32()));
            }
        }

        public uint TableHash;
        public List<DBQueryRecord> Queries = new();

        public struct DBQueryRecord
        {
            public DBQueryRecord(uint recordId)
            {
                RecordID = recordId;
            }

            public uint RecordID;
        }
    }

    public class DBReply : ServerPacket
    {
        public DBReply() : base(ServerOpcodes.DbReply) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(TableHash);
            _worldPacket.WriteUInt32(RecordID);
            _worldPacket.WriteUInt32(Timestamp);
            _worldPacket.WriteBits((byte)Status, 2);
            _worldPacket.WriteUInt32(Data.GetSize());
            _worldPacket.WriteBytes(Data.GetData());
        }

        public uint TableHash;
        public uint Timestamp;
        public uint RecordID;
        public HotfixRecord.Status Status = HotfixRecord.Status.Invalid;

        public ByteBuffer Data = new();
    }

    class AvailableHotfixes : ServerPacket
    {
        public AvailableHotfixes(uint virtualRealmAddress, uint hotfixCount, List<HotfixRecord> hotfixes) : base(ServerOpcodes.AvailableHotfixes)
        {
            VirtualRealmAddress = virtualRealmAddress;
            HotfixCount = hotfixCount;
            Hotfixes = hotfixes;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(VirtualRealmAddress);
            _worldPacket.WriteUInt32(HotfixCount);

            foreach (HotfixRecord hotfixRecord in Hotfixes)
                hotfixRecord.Write(_worldPacket);
        }

        public uint VirtualRealmAddress;
        public uint HotfixCount;
        public List<HotfixRecord> Hotfixes;
    }

    class HotfixRequest : ClientPacket
    {
        public HotfixRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ClientBuild = _worldPacket.ReadUInt32();
            DataBuild = _worldPacket.ReadUInt32();

            uint hotfixCount = _worldPacket.ReadUInt32();
            for (var i = 0; i < hotfixCount; ++i)
            {
                HotfixRecord hotfixRecord = new();
                hotfixRecord.Read(_worldPacket);
                Hotfixes.Add(hotfixRecord);
            }
        }

        public uint ClientBuild;
        public uint DataBuild;
        public List<HotfixRecord> Hotfixes = new();
    }

    class HotfixConnect : ServerPacket
    {
        public HotfixConnect() : base(ServerOpcodes.HotfixConnect) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Hotfixes.Count);
            foreach (HotfixData hotfix in Hotfixes)
                hotfix.Write(_worldPacket);

            _worldPacket.WriteUInt32(HotfixContent.GetSize());
            _worldPacket.WriteBytes(HotfixContent);
        }

        public List<HotfixData> Hotfixes = new();
        public ByteBuffer HotfixContent = new();

        public class HotfixData
        {
            public void Write(WorldPacket data)
            {
                Record.Write(data);
                data.WriteUInt32(Size);
                data.WriteBits((byte)Record.HotfixStatus, 2);
                data.FlushBits();
            }

            public HotfixRecord Record = new();
            public uint Size;
        }
    }
}
