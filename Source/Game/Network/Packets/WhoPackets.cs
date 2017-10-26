/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class WhoIsRequest : ClientPacket
    {
        public WhoIsRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CharName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
        }

        public string CharName;
    }

    public class WhoIsResponse : ServerPacket
    {
        public WhoIsResponse() : base(ServerOpcodes.WhoIs) { }

        public override void Write()
        {
            _worldPacket.WriteBits(AccountName.Length, 11);
            _worldPacket.WriteString(AccountName);
        }

        public string AccountName { get; set; }
    }

    public class WhoRequestPkt : ClientPacket
    {
        public WhoRequestPkt(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint areasCount = _worldPacket.ReadBits<uint>(4);

            Request.Read(_worldPacket);

            for (int i = 0; i < areasCount; ++i)
                Areas.Add(_worldPacket.ReadInt32());
        }

        public WhoRequest Request { get; set; } = new WhoRequest();
        public List<int> Areas{ get; set; } = new List<int>();
    }

    public class WhoResponsePkt : ServerPacket
    {
        public WhoResponsePkt() : base(ServerOpcodes.Who) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Response.Count, 6);
            _worldPacket.FlushBits();

            Response.ForEach(p => p.Write(_worldPacket));
        }

        public List<WhoEntry> Response { get; set; } = new List<WhoEntry>();
    }

    public struct WhoRequestServerInfo
    {
        public void Read(WorldPacket data)
        {
            FactionGroup = data.ReadInt32();
            Locale = data.ReadInt32();
            RequesterVirtualRealmAddress = data.ReadUInt32();
        }

        public int FactionGroup { get; set; }
        public int Locale { get; set; }
        public uint RequesterVirtualRealmAddress { get; set; }
    }

    public class WhoRequest
    {
        public void Read(WorldPacket data)
        {
            MinLevel = data.ReadInt32();
            MaxLevel = data.ReadInt32();
            RaceFilter = data.ReadInt32();
            ClassFilter = data.ReadInt32();

            uint nameLength = data.ReadBits<uint>(6);
            uint virtualRealmNameLength = data.ReadBits<uint>(9);
            uint guildNameLength = data.ReadBits<uint>(7);
            uint guildVirtualRealmNameLength = data.ReadBits<uint>(9);
            uint wordsCount = data.ReadBits<uint>(3);

            ShowEnemies = data.HasBit();
            ShowArenaPlayers = data.HasBit();
            ExactName = data.HasBit();
            ServerInfo.HasValue = data.HasBit();
            data.ResetBitPos();

            for (int i = 0; i < wordsCount; ++i)
            {
                Words.Add(data.ReadString(data.ReadBits<uint>(7)));
                data.ResetBitPos();
            }

            Name = data.ReadString(nameLength);
            VirtualRealmName = data.ReadString(virtualRealmNameLength);
            Guild = data.ReadString(guildNameLength);
            GuildVirtualRealmName = data.ReadString(guildVirtualRealmNameLength);

            if (ServerInfo.HasValue)
                ServerInfo.Value.Read(data);
        }

        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public string Name { get; set; }
        public string VirtualRealmName { get; set; }
        public string Guild { get; set; }
        public string GuildVirtualRealmName { get; set; }
        public int RaceFilter { get; set; } = -1;
        public int ClassFilter { get; set; } = -1;
        public List<string> Words { get; set; } = new List<string>();
        public bool ShowEnemies { get; set; }
        public bool ShowArenaPlayers { get; set; }
        public bool ExactName { get; set; }
        public Optional<WhoRequestServerInfo> ServerInfo;
    }

    public class WhoEntry
    {
        public void Write(WorldPacket data)
        {
            PlayerData.Write(data);

            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(GuildVirtualRealmAddress);
            data.WriteInt32(AreaID);

            data.WriteBits(GuildName.Length, 7);
            data.WriteBit(IsGM);
            data.WriteString(GuildName);

            data.FlushBits();
        }

        public PlayerGuidLookupData PlayerData { get; set; } = new PlayerGuidLookupData();
        public ObjectGuid GuildGUID { get; set; }
        public uint GuildVirtualRealmAddress { get; set; }
        public string GuildName { get; set; } = "";
        public int AreaID { get; set; }
        public bool IsGM { get; set; }
    }
}
