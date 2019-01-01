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
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class Inspect : ClientPacket
    {
        public Inspect(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Target = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Target;
    }

    public class InspectResult : ServerPacket
    {
        public InspectResult() : base(ServerOpcodes.InspectResult) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InspecteeGUID);
            _worldPacket.WriteUInt32(Items.Count);
            _worldPacket.WriteUInt32(Glyphs.Count);
            _worldPacket.WriteUInt32(Talents.Count);
            _worldPacket.WriteUInt32(PvpTalents.Count);
            _worldPacket.WriteInt32(ClassID);
            _worldPacket.WriteInt32(SpecializationID);
            _worldPacket.WriteInt32(GenderID);

            for (int i = 0; i < Glyphs.Count; ++i)
                _worldPacket.WriteUInt16(Glyphs[i]);

            for (int i = 0; i < Talents.Count; ++i)
                _worldPacket.WriteUInt16(Talents[i]);

            for (int i = 0; i < PvpTalents.Count; ++i)
                _worldPacket.WriteUInt16(PvpTalents[i]);

            _worldPacket.WriteBit(GuildData.HasValue);
            _worldPacket.WriteBit(AzeriteLevel.HasValue);
            _worldPacket.FlushBits();

            Items.ForEach(p => p.Write(_worldPacket));

            if (GuildData.HasValue)
                GuildData.Value.Write(_worldPacket);

            if (AzeriteLevel.HasValue)
                _worldPacket.WriteInt32(AzeriteLevel.Value);
        }

        public ObjectGuid InspecteeGUID;
        public List<InspectItemData> Items = new List<InspectItemData>();
        public List<ushort> Glyphs = new List<ushort>();
        public List<ushort> Talents = new List<ushort>();
        public List<ushort> PvpTalents = new List<ushort>();
        public Class ClassID = Class.None;
        public Gender GenderID = Gender.None;
        public Optional<InspectGuildData> GuildData = new Optional<InspectGuildData>();
        public int SpecializationID;
        public Optional<int> AzeriteLevel;
    }

    public class RequestHonorStats : ClientPacket
    {
        public RequestHonorStats(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TargetGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid TargetGUID;
    }

    public class InspectHonorStats : ServerPacket
    {
        public InspectHonorStats() : base(ServerOpcodes.InspectHonorStats) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGUID);
            _worldPacket.WriteUInt8(LifetimeMaxRank);
            _worldPacket.WriteUInt16(YesterdayHK); // @todo: confirm order
            _worldPacket.WriteUInt16(TodayHK); // @todo: confirm order
            _worldPacket.WriteUInt32(LifetimeHK);
        }

        public ObjectGuid PlayerGUID;
        public uint LifetimeHK;
        public ushort YesterdayHK;
        public ushort TodayHK;
        public byte LifetimeMaxRank;
    }

    public class InspectPVPRequest : ClientPacket
    {
        public InspectPVPRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            InspectTarget = _worldPacket.ReadPackedGuid();
            InspectRealmAddress = _worldPacket.ReadUInt32();
        }

        public ObjectGuid InspectTarget;
        public uint InspectRealmAddress;
    }

    public class InspectPVPResponse : ServerPacket
    {
        public InspectPVPResponse() : base(ServerOpcodes.InspectPvp) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ClientGUID);

            _worldPacket.WriteBits(Bracket.Count, 3);
            _worldPacket.FlushBits();

            Bracket.ForEach(p => p.Write(_worldPacket));
        }

        public List<PVPBracketData> Bracket = new List<PVPBracketData>();
        public ObjectGuid ClientGUID;
    }

    public class QueryInspectAchievements : ClientPacket
    {
        public QueryInspectAchievements(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    /// RespondInspectAchievements in AchievementPackets


    //Structs
    public struct InspectEnchantData
    {
        public InspectEnchantData(uint id, byte index)
        {
            Id = id;
            Index = index;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Id);
            data.WriteUInt8(Index);
        }

        public uint Id;
        public byte Index;
    }

    public class InspectItemData
    {
        public InspectItemData(Item item, byte index)
        {
            CreatorGUID = item.GetGuidValue(ItemFields.Creator);

            Item = new ItemInstance(item);
            Index = index;
            Usable = true; // @todo

            for (EnchantmentSlot enchant = 0; enchant < EnchantmentSlot.Max; ++enchant)
            {
                uint enchId = item.GetEnchantmentId(enchant);
                if (enchId != 0)
                    Enchants.Add(new InspectEnchantData(enchId, (byte)enchant));
            }

            byte i = 0;
            foreach (ItemDynamicFieldGems gemData in item.GetGems())
            {
                if (gemData.ItemId != 0)
                {
                    ItemGemData gem = new ItemGemData();
                    gem.Slot = i;
                    gem.Item = new ItemInstance(gemData);
                    Gems.Add(gem);
                }
                ++i;
            }
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(CreatorGUID);
            data.WriteUInt8(Index);
            data.WriteUInt32(AzeritePowers.Count);
            foreach (var id in AzeritePowers)
                data.WriteInt32(id);

            Item.Write(data);
            data.WriteBit(Usable);
            data.WriteBits(Enchants.Count, 4);
            data.WriteBits(Gems.Count, 2);
            data.FlushBits();

            foreach (var gem in Gems)
                gem.Write(data);

            for (int i = 0; i < Enchants.Count; ++i)
                Enchants[i].Write(data);
        }

        public ObjectGuid CreatorGUID;
        public ItemInstance Item;
        public byte Index;
        public bool Usable;
        public List<InspectEnchantData> Enchants = new List<InspectEnchantData>();
        public List<ItemGemData> Gems = new List<ItemGemData>();
        public List<int> AzeritePowers = new List<int>();
    }

    public struct InspectGuildData
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(GuildGUID);
            data.WriteInt32(NumGuildMembers);
            data.WriteInt32(AchievementPoints);
        }

        public ObjectGuid GuildGUID;
        public int NumGuildMembers;
        public int AchievementPoints;
    }

    public struct PVPBracketData
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Bracket);
            data.WriteInt32(Rating);
            data.WriteInt32(Rank);
            data.WriteInt32(WeeklyPlayed);
            data.WriteInt32(WeeklyWon);
            data.WriteInt32(SeasonPlayed);
            data.WriteInt32(SeasonWon);
            data.WriteInt32(WeeklyBestRating);
            data.WriteInt32(Unk710);
            data.WriteInt32(Unk801_1);
            data.WriteBit(Unk801_2);
            data.FlushBits();

        }

        public int Rating;
        public int Rank;
        public int WeeklyPlayed;
        public int WeeklyWon;
        public int SeasonPlayed;
        public int SeasonWon;
        public int WeeklyBestRating;
        public int Unk710;
        public int Unk801_1;
        public byte Bracket;
        public bool Unk801_2;
    }
}

