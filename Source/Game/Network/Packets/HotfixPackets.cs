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
                Queries.Add(new DBQueryRecord(_worldPacket.ReadUInt32()));
            }
        }

        public uint TableHash;
        public List<DBQueryRecord> Queries = new List<DBQueryRecord>();

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
        public AvailableHotfixes(int hotfixCacheVersion, uint hotfixCount, MultiMap<int, Tuple<uint, int>> hotfixes) : base(ServerOpcodes.AvailableHotfixes)
        {
            HotfixCacheVersion = hotfixCacheVersion;
            HotfixCount = hotfixCount;
            Hotfixes = hotfixes;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(HotfixCacheVersion);
            _worldPacket.WriteUInt32(HotfixCount);

            foreach (var key in Hotfixes.Keys)
            {
                foreach (var tableRecord in Hotfixes[key])
                {
                    _worldPacket.WriteUInt32(tableRecord.Item1);
                    _worldPacket.WriteInt32(tableRecord.Item2);
                    _worldPacket.WriteInt32(key);
                }
            }
        }

        public int HotfixCacheVersion;
        public uint HotfixCount;
        public MultiMap<int, Tuple<uint, int>> Hotfixes;
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
                Hotfixes.Add(new HotfixRecord(_worldPacket));
        }

        public uint ClientBuild;
        public uint DataBuild;
        public List<HotfixRecord> Hotfixes = new List<HotfixRecord>();
    }

    class HotfixResponse : ServerPacket
    {
        public HotfixResponse() : base(ServerOpcodes.HotfixResponse) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Hotfixes.Count);
            foreach (HotfixData hotfix in Hotfixes)
                hotfix.Write(_worldPacket);

            _worldPacket.WriteUInt32(HotfixContent.GetSize());
            _worldPacket.WriteBytes(HotfixContent);
        }

        public List<HotfixData> Hotfixes = new List<HotfixData>();
        public ByteBuffer HotfixContent = new ByteBuffer();

        public class HotfixData
        {
            public void Write(WorldPacket data)
            {
                Record.Write(data);
                if (Size.HasValue)
                {
                    data.WriteUInt32(Size.Value);
                    data.WriteBit(true);
                }
                else
                {
                    data.WriteUInt32(0);
                    data.WriteBit(false);
                }
                data.FlushBits();
            }

            public HotfixRecord Record;
            public Optional<uint> Size;
        }
    }

    public struct HotfixRecord
    {
        public HotfixRecord(WorldPacket data)
        {
            TableHash = data.ReadUInt32();
            RecordID = data.ReadInt32();
            HotfixID = data.ReadInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(TableHash);
            data.WriteInt32(RecordID);
            data.WriteInt32(HotfixID);
        }

        public uint TableHash;
        public int RecordID;
        public int HotfixID;
    }
}
