// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using System.Collections.Generic;
using System;

namespace Game.Networking.Packets
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

            TalentInfo.Write(_worldPacket);

            _worldPacket.WriteBit(GuildData.HasValue);
            _worldPacket.WriteBit(AzeriteLevel.HasValue);
            _worldPacket.FlushBits();

            foreach (PVPBracketData bracket in Bracket)
                bracket.Write(_worldPacket);

            if (GuildData.HasValue)
                GuildData.Value.Write(_worldPacket);

            if (AzeriteLevel.HasValue)
                _worldPacket.WriteUInt32(AzeriteLevel.Value);

            TraitsInfo.Write(_worldPacket);
        }

        public PlayerModelDisplayInfo DisplayInfo;
        public List<ushort> Glyphs = new();
        public List<ushort> Talents = new();
        public Array<ushort> PvpTalents = new(PlayerConst.MaxPvpTalentSlots, 0);
        public InspectGuildData? GuildData;
        public Array<PVPBracketData> Bracket = new(7, default);
        public uint? AzeriteLevel;
        public int ItemLevel;
        public uint LifetimeHK;
        public uint HonorLevel;
        public ushort TodayHK;
        public ushort YesterdayHK;
        public byte LifetimeMaxRank;
        public ClassicTalentInfoUpdate TalentInfo;
        public TraitInspectInfo TraitsInfo = new();
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
                    ItemGemData gem = new();
                    gem.Slot = i;
                    gem.Item = new ItemInstance(gemData);
                    Gems.Add(gem);
                }
                ++i;
            }

            AzeriteItem azeriteItem = item.ToAzeriteItem();
            if (azeriteItem != null)
            {
                SelectedAzeriteEssences essences = azeriteItem.GetSelectedAzeriteEssences();
                if (essences != null)
                {
                    for (byte slot = 0; slot < essences.AzeriteEssenceID.GetSize(); ++slot)
                    {
                        AzeriteEssenceData essence = new();
                        essence.Index = slot;
                        essence.AzeriteEssenceID = essences.AzeriteEssenceID[slot];
                        if (essence.AzeriteEssenceID != 0)
                        {
                            essence.Rank = azeriteItem.GetEssenceRank(essence.AzeriteEssenceID);
                            essence.SlotUnlocked = true;
                        }
                        else
                            essence.SlotUnlocked = azeriteItem.HasUnlockedEssenceSlot(slot);

                        AzeriteEssences.Add(essence);
                    }
                }
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
        public List<InspectEnchantData> Enchants = new();
        public List<ItemGemData> Gems = new();
        public List<int> AzeritePowers = new();
        public List<AzeriteEssenceData> AzeriteEssences = new();
    }

    public class PlayerModelDisplayInfo
    {
        public ObjectGuid GUID;
        public List<InspectItemData> Items = new();
        public string Name;
        public uint SpecializationID;
        public byte GenderID;
        public byte Race;
        public byte ClassID;
        public List<ChrCustomizationChoice> Customizations = new();

        public void Initialize(Player player)
        {
            GUID = player.GetGUID();
            SpecializationID = (uint)player.GetPrimarySpecialization();
            Name = player.GetName();
            GenderID = (byte)player.GetNativeGender();
            Race = (byte)player.GetRace();
            ClassID = (byte)player.GetClass();

            foreach (var customization in player.m_playerData.Customizations)
                Customizations.Add(customization);

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
            data.WriteUInt8(Race);
            data.WriteUInt8(ClassID);
            data.WriteInt32(Customizations.Count);
            data.WriteString(Name);

            foreach (var customization in Customizations)
            {
                data.WriteUInt32(customization.ChrCustomizationOptionID);
                data.WriteUInt32(customization.ChrCustomizationChoiceID);
            }

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
        public int Rating;
        public int RatingID;
        public int Rank;
        public int WeeklyPlayed;
        public int WeeklyWon;
        public int SeasonPlayed;
        public int SeasonWon;
        public int WeeklyBestRating;
        public int LastWeeksBestRating;
        public int Tier;
        public int WeeklyBestTier;
        public int SeasonBestRating;
        public byte SeasonBestTierEnum;
        public int RoundsSeasonPlayed;
        public int RoundsSeasonWon;
        public int RoundsWeeklyPlayed;
        public int RoundsWeeklyWon;
        public byte Bracket;
        public bool Disqualified;

        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Bracket);
            data.WriteInt32(RatingID);
            data.WriteInt32(Rating);
            data.WriteInt32(Rank);
            data.WriteInt32(WeeklyPlayed);
            data.WriteInt32(WeeklyWon);
            data.WriteInt32(SeasonPlayed);
            data.WriteInt32(SeasonWon);
            data.WriteInt32(WeeklyBestRating);
            data.WriteInt32(LastWeeksBestRating);
            data.WriteInt32(Tier);
            data.WriteInt32(WeeklyBestTier);
            data.WriteInt32(SeasonBestRating);
            data.WriteUInt8(SeasonBestTierEnum);
            data.WriteInt32(RoundsSeasonPlayed);
            data.WriteInt32(RoundsSeasonWon);
            data.WriteInt32(RoundsWeeklyPlayed);
            data.WriteInt32(RoundsWeeklyWon);
            data.WriteBit(Disqualified);
            data.FlushBits();
        }
    }

    public class TraitInspectInfo
    {
        public int PlayerLevel;
        public int SpecID;
        public TraitConfigPacket ActiveCombatTraits = new();

        public void Write(WorldPacket data)
        {
            data.WriteInt32(PlayerLevel);
            data.WriteInt32(SpecID);
            ActiveCombatTraits.Write(data);
        }
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

