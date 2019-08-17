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
using System;

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
        public InspectResult() : base(ServerOpcodes.InspectResult)
        {
            DisplayInfo = new PlayerModelDisplayInfo();
        }

        public override void Write()
        {
            DisplayInfo.Write(_worldPacket);
            _worldPacket.WriteInt32(Glyphs.Count);
            _worldPacket.WriteInt32(Talents.Count);
            _worldPacket.WriteInt32(PvpTalents.Count);
            _worldPacket.WriteInt32(ItemLevel);
            _worldPacket.WriteUInt8(LifetimeMaxRank);
            _worldPacket.WriteUInt16(TodayHK);
            _worldPacket.WriteUInt16(YesterdayHK);
            _worldPacket.WriteUInt32(LifetimeHK);
            _worldPacket.WriteUInt32(HonorLevel);

            for (int i = 0; i < Glyphs.Count; ++i)
                _worldPacket.WriteUInt16(Glyphs[i]);

            for (int i = 0; i < Talents.Count; ++i)
                _worldPacket.WriteUInt16(Talents[i]);

            for (int i = 0; i < PvpTalents.Count; ++i)
                _worldPacket.WriteUInt16(PvpTalents[i]);

            _worldPacket.WriteBit(GuildData.HasValue);
            _worldPacket.WriteBit(AzeriteLevel.HasValue);
            _worldPacket.FlushBits();

            foreach (PVPBracketData bracket in Bracket)
                bracket.Write(_worldPacket);

            if (GuildData.HasValue)
                GuildData.Value.Write(_worldPacket);

            if (AzeriteLevel.HasValue)
                _worldPacket.WriteInt32(AzeriteLevel.Value);
        }

        public PlayerModelDisplayInfo DisplayInfo;
        public List<ushort> Glyphs = new List<ushort>();
        public List<ushort> Talents = new List<ushort>();
        public List<ushort> PvpTalents = new List<ushort>();
        public Optional<InspectGuildData> GuildData = new Optional<InspectGuildData>();
        public Array<PVPBracketData> Bracket = new Array<PVPBracketData>(6);
        public Optional<int> AzeriteLevel;
        public int ItemLevel;
        public uint LifetimeHK;
        public uint HonorLevel;
        public ushort TodayHK;
        public ushort YesterdayHK;
        public byte LifetimeMaxRank;
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
            CreatorGUID = item.GetCreator();

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
            foreach (SocketedGem gemData in item.m_itemData.Gems)
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
            data.WriteInt32(AzeritePowers.Count);
            data.WriteInt32(AzeriteEssences.Count);
            foreach (var id in AzeritePowers)
                data.WriteInt32(id);

            Item.Write(data);
            data.WriteBit(Usable);
            data.WriteBits(Enchants.Count, 4);
            data.WriteBits(Gems.Count, 2);
            data.FlushBits();

            foreach (var azeriteEssenceData in AzeriteEssences)
                azeriteEssenceData.Write(data);

            foreach (var enchantData in Enchants)
                enchantData.Write(data);

            foreach (var gem in Gems)
                gem.Write(data);
        }

        public ObjectGuid CreatorGUID;
        public ItemInstance Item;
        public byte Index;
        public bool Usable;
        public List<InspectEnchantData> Enchants = new List<InspectEnchantData>();
        public List<ItemGemData> Gems = new List<ItemGemData>();
        public List<int> AzeritePowers = new List<int>();
        public List<AzeriteEssenceData> AzeriteEssences = new List<AzeriteEssenceData>();
    }

    public class PlayerModelDisplayInfo
    {
        public ObjectGuid GUID;
        public List<InspectItemData> Items = new List<InspectItemData>();
        public string Name;
        public uint SpecializationID;
        public byte GenderID;
        public byte Skin;
        public byte HairColor;
        public byte HairStyle;
        public byte FacialHairStyle;
        public byte Face;
        public byte Race;
        public byte ClassID;
        public Array<byte> CustomDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);

        public void Initialize(Player player)
        {
            GUID = player.GetGUID();
            SpecializationID = player.GetPrimarySpecialization();
            Name = player.GetName();
            GenderID = player.m_playerData.NativeSex;
            Skin = player.m_playerData.SkinID;
            HairColor = player.m_playerData.HairColorID;
            HairStyle = player.m_playerData.HairStyleID;
            FacialHairStyle = player.m_playerData.FacialHairStyleID;
            Face = player.m_playerData.FaceID;
            Race = (byte)player.GetRace();
            ClassID = (byte)player.GetClass();
            CustomDisplay.AddRange(player.m_playerData.CustomDisplayOption._values);

            for (byte i = 0; i < EquipmentSlot.End; ++i)
            {
                Item item = player.GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                    Items.Add(new InspectItemData(item, i));
            }
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(GUID);
            data.WriteUInt32(SpecializationID);
            data.WriteInt32(Items.Count);
            data.WriteBits(Name.GetByteCount(), 6);
            data.WriteUInt8(GenderID);
            data.WriteUInt8(Skin);
            data.WriteUInt8(HairColor);
            data.WriteUInt8(HairStyle);
            data.WriteUInt8(FacialHairStyle);
            data.WriteUInt8(Face);
            data.WriteUInt8(Race);
            data.WriteUInt8(ClassID);
            CustomDisplay.ForEach(id => data.WriteUInt8(id));

            data.WriteString(Name);

            foreach (InspectItemData item in Items)
                item.Write(data);
        }
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

    public struct AzeriteEssenceData
    {
        public uint Index;
        public uint AzeriteEssenceID;
        public uint Rank;
        public bool SlotUnlocked;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Index);
            data.WriteUInt32(AzeriteEssenceID);
            data.WriteUInt32(Rank);
            data.WriteBit(SlotUnlocked);
            data.FlushBits();
        }
    }
}

