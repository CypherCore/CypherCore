// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Miscellaneous;
using System;
using System.Collections.Generic;

namespace Game.Networking.Packets
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
            _worldPacket.WriteBits(AccountName.GetByteCount(), 11);
            _worldPacket.WriteString(AccountName);
        }

        public string AccountName;
    }

    public class WhoRequestPkt : ClientPacket
    {
        public WhoRequestPkt(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint areasCount = _worldPacket.ReadBits<uint>(4);

            Request.Read(_worldPacket);
            RequestID = _worldPacket.ReadUInt32();

            for (int i = 0; i < areasCount; ++i)
                Areas.Add(_worldPacket.ReadInt32());
        }

        public WhoRequest Request = new();
        public uint RequestID;
        public List<int> Areas= new();
    }

    public class WhoResponsePkt : ServerPacket
    {
        public WhoResponsePkt() : base(ServerOpcodes.Who) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(RequestID);
            _worldPacket.WriteBits(Response.Count, 6);
            _worldPacket.FlushBits();

            Response.ForEach(p => p.Write(_worldPacket));
        }

        public uint RequestID;
        public List<WhoEntry> Response = new();
    }

    public struct WhoRequestServerInfo
    {
        public void Read(WorldPacket data)
        {
            FactionGroup = data.ReadInt32();
            Locale = data.ReadInt32();
            RequesterVirtualRealmAddress = data.ReadUInt32();
        }

        public int FactionGroup;
        public int Locale;
        public uint RequesterVirtualRealmAddress;
    }

    public class WhoRequest
    {
        public void Read(WorldPacket data)
        {
            MinLevel = data.ReadInt32();
            MaxLevel = data.ReadInt32();
            RaceFilter = new RaceMask<long>(data.ReadInt64());
            ClassFilter = data.ReadInt32();

            uint nameLength = data.ReadBits<uint>(6);
            uint virtualRealmNameLength = data.ReadBits<uint>(9);
            uint guildNameLength = data.ReadBits<uint>(7);
            uint guildVirtualRealmNameLength = data.ReadBits<uint>(9);
            uint wordsCount = data.ReadBits<uint>(3);

            ShowEnemies = data.HasBit();
            ShowArenaPlayers = data.HasBit();
            ExactName = data.HasBit();
            if (data.HasBit())
                ServerInfo = new();

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

        public int MinLevel;
        public int MaxLevel;
        public string Name;
        public string VirtualRealmName;
        public string Guild;
        public string GuildVirtualRealmName;
        public RaceMask<long> RaceFilter;
        public int ClassFilter = -1;
        public List<string> Words = new();
        public bool ShowEnemies;
        public bool ShowArenaPlayers;
        public bool ExactName;
        public WhoRequestServerInfo? ServerInfo;
    }

    public class WhoEntry
    {
        public void Write(WorldPacket data)
        {
            PlayerData.Write(data);

            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(GuildVirtualRealmAddress);
            data.WriteInt32(AreaID);

            data.WriteBits(GuildName.GetByteCount(), 7);
            data.WriteBit(IsGM);
            data.WriteString(GuildName);

            data.FlushBits();
        }

        public PlayerGuidLookupData PlayerData = new();
        public ObjectGuid GuildGUID;
        public uint GuildVirtualRealmAddress;
        public string GuildName = "";
        public int AreaID;
        public bool IsGM;
    }
}
