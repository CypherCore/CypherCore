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
    public class Inspect : ClientPacket
    {
        public Inspect(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Target = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Target { get; set; }
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
            _worldPacket.FlushBits();

            Items.ForEach(p => p.Write(_worldPacket));

            if (GuildData.HasValue)
                GuildData.Value.Write(_worldPacket);
        }

        public ObjectGuid InspecteeGUID { get; set; }
        public List<InspectItemData> Items { get; set; } = new List<InspectItemData>();
        public List<ushort> Glyphs { get; set; } = new List<ushort>();
        public List<ushort> Talents { get; set; } = new List<ushort>();
        public List<ushort> PvpTalents { get; set; } = new List<ushort>();
        public Class ClassID { get; set; } = Class.None;
        public Gender GenderID { get; set; } = Gender.None;
        public Optional<InspectGuildData> GuildData = new Optional<InspectGuildData>();
        public int SpecializationID { get; set; }
    }

    public class RequestHonorStats : ClientPacket
    {
        public RequestHonorStats(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TargetGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid TargetGUID { get; set; }
    }

    public class InspectHonorStats : ServerPacket
    {
        public InspectHonorStats() : base(ServerOpcodes.InspectHonorStats) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGUID);
            _worldPacket.WriteUInt8(LifetimeMaxRank);
            _worldPacket.WriteUInt16(YesterdayHK); /// @todo: confirm order
            _worldPacket.WriteUInt16(TodayHK); /// @todo: confirm order
            _worldPacket.WriteUInt32(LifetimeHK);
        }

        public ObjectGuid PlayerGUID { get; set; }
        public uint LifetimeHK { get; set; }
        public ushort YesterdayHK { get; set; }
        public ushort TodayHK { get; set; }
        public byte LifetimeMaxRank { get; set; }
    }

    public class InspectPVPRequest : ClientPacket
    {
        public InspectPVPRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            InspectTarget = _worldPacket.ReadPackedGuid();
            InspectRealmAddress = _worldPacket.ReadUInt32();
        }

        public ObjectGuid InspectTarget { get; set; }
        public uint InspectRealmAddress { get; set; }
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

        public List<PVPBracketData> Bracket { get; set; }
        public ObjectGuid ClientGUID { get; set; }
    }

    public class QueryInspectAchievements : ClientPacket
    {
        public QueryInspectAchievements(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid { get; set; }
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

        public uint Id { get; set; }
        public byte Index { get; set; }
    }

    public class InspectItemData
    {
        public InspectItemData(Item item, byte index)
        {
            CreatorGUID = item.GetGuidValue(ItemFields.Creator);

            Item = new ItemInstance(item);
            Index = index;
            Usable = true; /// @todo

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

        public ObjectGuid CreatorGUID { get; set; }
        public ItemInstance Item { get; set; }
        public byte Index { get; set; }
        public bool Usable { get; set; }
        public List<InspectEnchantData> Enchants { get; set; } = new List<InspectEnchantData>();
        public List<ItemGemData> Gems { get; set; } = new List<ItemGemData>();
    }

    public struct InspectGuildData
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(GuildGUID);
            data.WriteInt32(NumGuildMembers);
            data.WriteInt32(AchievementPoints);
        }

        public ObjectGuid GuildGUID { get; set; }
        public int NumGuildMembers { get; set; }
        public int AchievementPoints { get; set; }
    }

    public struct PVPBracketData
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(Rating);
            data.WriteInt32(Rank);
            data.WriteInt32(WeeklyPlayed);
            data.WriteInt32(WeeklyWon);
            data.WriteInt32(SeasonPlayed);
            data.WriteInt32(SeasonWon);
            data.WriteInt32(WeeklyBestRating);
            data.WriteInt32(Unk710);
            data.WriteUInt8(Bracket);
        }

        public int Rating { get; set; }
        public int Rank { get; set; }
        public int WeeklyPlayed { get; set; }
        public int WeeklyWon { get; set; }
        public int SeasonPlayed { get; set; }
        public int SeasonWon { get; set; }
        public int WeeklyBestRating { get; set; }
        public int Unk710 { get; set; }
        public byte Bracket { get; set; }
    }
}

