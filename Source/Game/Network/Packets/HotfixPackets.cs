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
using Framework.Dynamic;
using Framework.IO;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
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
                Queries.Add(new DBQueryRecord(_worldPacket.ReadPackedGuid(), _worldPacket.ReadUInt32()));
            }
        }

        public uint TableHash;
        public List<DBQueryRecord> Queries = new List<DBQueryRecord>();

        public struct DBQueryRecord
        {
            public DBQueryRecord(ObjectGuid guid, uint recordId)
            {
                GUID = guid;
                RecordID = recordId;
            }

            public ObjectGuid GUID;
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
            _worldPacket.WriteBit(Allow);
            _worldPacket.WriteUInt32(Data.GetSize());
            _worldPacket.WriteBytes(Data.GetData());
        }

        public uint TableHash;
        public uint Timestamp;
        public uint RecordID;
        public bool Allow;

        public ByteBuffer Data = new ByteBuffer();
    }

    class AvailableHotfixes : ServerPacket
    {
        public AvailableHotfixes(int hotfixCacheVersion, Dictionary<ulong, int> hotfixes) : base(ServerOpcodes.AvailableHotfixes)
        {
            HotfixCacheVersion = hotfixCacheVersion;
            Hotfixes = hotfixes;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(HotfixCacheVersion);

            _worldPacket.WriteUInt32(Hotfixes.Count);
            foreach (var hotfixEntry in Hotfixes)
                _worldPacket.WriteInt64(hotfixEntry.Key);
        }

        public int HotfixCacheVersion;
        public Dictionary<ulong, int> Hotfixes = new Dictionary<ulong, int>();
    }

    class HotfixRequest : ClientPacket
    {
        public HotfixRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint hotfixCount = _worldPacket.ReadUInt32();
            //if (hotfixCount > Global.DB2Mgr.GetHotfixData().Count)
                //throw PacketArrayMaxCapacityException(hotfixCount, sDB2Manager.GetHotfixData().size());

            for (var i = 0; i < hotfixCount; ++i)
                Hotfixes.Add(_worldPacket.ReadUInt64());
        }

        public List<ulong> Hotfixes = new List<ulong>();
    }

    class HotfixResponse : ServerPacket
    {
        public HotfixResponse() : base(ServerOpcodes.HotfixResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Hotfixes.Count);
            foreach (HotfixData hotfix in Hotfixes)
                hotfix.Write(_worldPacket);
        }

        public List<HotfixData> Hotfixes = new List<HotfixData>();

        public class HotfixData
        {
            public void Write(WorldPacket data)
            {
                data.WriteInt64(ID);
                data.WriteInt32(RecordID);
                data.WriteBit(Data.HasValue);
                if (Data.HasValue)
                {
                    data.WriteUInt32(Data.Value.GetSize());
                    data.WriteBytes(Data.Value);
                }
            }

            public ulong ID;
            public int RecordID;
            public Optional<ByteBuffer> Data;
        }
    }
}
