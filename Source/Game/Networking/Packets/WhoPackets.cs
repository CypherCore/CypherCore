// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class WhoIsRequest : ClientPacket
    {
        public string CharName;

        public WhoIsRequest(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            CharName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
        }
    }

    public class WhoIsResponse : ServerPacket
    {
        public string AccountName;

        public WhoIsResponse() : base(ServerOpcodes.WhoIs)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBits(AccountName.GetByteCount(), 11);
            _worldPacket.WriteString(AccountName);
        }
    }

    public class WhoRequestPkt : ClientPacket
    {
        public List<int> Areas = new();

        public WhoRequest Request = new();
        public uint RequestID;

        public WhoRequestPkt(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            uint areasCount = _worldPacket.ReadBits<uint>(4);

            Request.Read(_worldPacket);
            RequestID = _worldPacket.ReadUInt32();

            for (int i = 0; i < areasCount; ++i)
                Areas.Add(_worldPacket.ReadInt32());
        }
    }

    public class WhoResponsePkt : ServerPacket
    {
        public uint RequestID;
        public List<WhoEntry> Response = new();

        public WhoResponsePkt() : base(ServerOpcodes.Who)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(RequestID);
            _worldPacket.WriteBits(Response.Count, 6);
            _worldPacket.FlushBits();

            Response.ForEach(p => p.Write(_worldPacket));
        }
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
        public int ClassFilter = -1;
        public bool ExactName;
        public string Guild;
        public string GuildVirtualRealmName;
        public int MaxLevel;

        public int MinLevel;
        public string Name;
        public long RaceFilter;
        public WhoRequestServerInfo? ServerInfo;
        public bool ShowArenaPlayers;
        public bool ShowEnemies;
        public string VirtualRealmName;
        public List<string> Words = new();

        public void Read(WorldPacket data)
        {
            MinLevel = data.ReadInt32();
            MaxLevel = data.ReadInt32();
            RaceFilter = data.ReadInt64();
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
                ServerInfo = new WhoRequestServerInfo();

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
    }

    public class WhoEntry
    {
        public int AreaID;
        public ObjectGuid GuildGUID;
        public string GuildName = "";
        public uint GuildVirtualRealmAddress;
        public bool IsGM;

        public PlayerGuidLookupData PlayerData = new();

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
    }
}