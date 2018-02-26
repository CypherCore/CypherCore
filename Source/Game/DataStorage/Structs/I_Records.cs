/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

namespace Game.DataStorage
{
    public sealed class ImportPriceArmorRecord
    {
        public uint Id;
        public float ClothFactor;
        public float LeatherFactor;
        public float MailFactor;
        public float PlateFactor;
    }

    public sealed class ImportPriceQualityRecord
    {
        public uint Id;
        public float Factor;
    }

    public sealed class ImportPriceShieldRecord
    {
        public uint Id;
        public float Factor;
    }

    public sealed class ImportPriceWeaponRecord
    {
        public uint Id;
        public float Factor;
    }

    public sealed class ItemRecord
    {
        public uint Id;
        public uint FileDataID;
        public ItemClass Class;
        public byte SubClass;
        public sbyte SoundOverrideSubclass;
        public sbyte Material;
        public InventoryType inventoryType;
        public byte Sheath;
        public byte GroupSoundsID;
    }

    public sealed class ItemAppearanceRecord
    {
        public uint Id;
        public uint DisplayID;
        public uint IconFileDataID;
        public uint UIOrder;
        public byte ObjectComponentSlot;
    }

    public sealed class ItemArmorQualityRecord
    {
        public uint Id;
        public float[] QualityMod = new float[7];
        public ushort ItemLevel;
    }

    public sealed class ItemArmorShieldRecord
    {
        public uint Id;
        public float[] Quality = new float[7];
        public ushort ItemLevel;
    }

    public sealed class ItemArmorTotalRecord
    {
        public uint Id;
        public float[] Value = new float[4];
        public ushort ItemLevel;
    }

    public sealed class ItemBagFamilyRecord
    {
        public uint Id;
        public uint Name;
    }

    public sealed class ItemBonusRecord
    {
        public uint Id;
        public int[] Value = new int[3];
        public ushort BonusListID;
        public ItemBonusType Type;
        public byte Index;
    }

    public sealed class ItemBonusListLevelDeltaRecord
    {
        public short Delta;
        public uint Id;
    }

    public sealed class ItemBonusTreeNodeRecord
    {
        public uint Id;
        public ushort SubTreeID;
        public ushort BonusListID;
        public ushort ItemLevelSelectorID;
        public byte BonusTreeModID;
        public uint BonusTreeID;
    }

    public sealed class ItemChildEquipmentRecord
    {
        public uint Id;
        public uint AltItemID;
        public byte AltEquipmentSlot;
        public uint ItemID;
    }

    public sealed class ItemClassRecord
    {
        public uint Id;
        public string Name;
        public float PriceMod;
        public byte OldEnumValue;
        public byte Flags;
    }

    public sealed class ItemCurrencyCostRecord
    {
        public uint Id;
        public uint ItemId;
    }

    // common struct for:
    // ItemDamageAmmo.dbc
    // ItemDamageOneHand.dbc
    // ItemDamageOneHandCaster.dbc
    // ItemDamageRanged.dbc
    // ItemDamageThrown.dbc
    // ItemDamageTwoHand.dbc
    // ItemDamageTwoHandCaster.dbc
    // ItemDamageWand.dbc
    public sealed class ItemDamageRecord
    {
        public uint Id;
        public float[] DPS = new float[7];
        public ushort ItemLevel;
    }

    public sealed class ItemDisenchantLootRecord
    {
        public uint Id;
        public ushort MinItemLevel;
        public ushort MaxItemLevel;
        public ushort RequiredDisenchantSkill;
        public sbyte ItemSubClass;
        public byte ItemQuality;
        public sbyte Expansion;
        public uint ItemClass;
    }

    public sealed class ItemEffectRecord
    {
        public uint Id;
        public uint SpellID;
        public int Cooldown;
        public int CategoryCooldown;
        public short Charges;
        public ushort Category;
        public ushort ChrSpecializationID;
        public byte OrderIndex;
        public ItemSpelltriggerType Trigger;
        public uint ItemID;
    }

    public sealed class ItemExtendedCostRecord
    {
        public uint Id;
        public uint[] RequiredItem = new uint[ItemConst.MaxItemExtCostItems];                   // required item id
        public uint[] RequiredCurrencyCount = new uint[ItemConst.MaxItemExtCostCurrencies];     // required curency count
        public ushort[] RequiredItemCount = new ushort[ItemConst.MaxItemExtCostItems];              // required count of 1st item
        public ushort RequiredPersonalArenaRating;                             // required personal arena rating
        public ushort[] RequiredCurrency = new ushort[ItemConst.MaxItemExtCostCurrencies];          // required curency id
        public byte RequiredArenaSlot;                                        // arena slot restrictions (min slot value)
        public byte RequiredFactionId;
        public byte RequiredFactionStanding;
        public byte RequirementFlags;
        public byte RequiredAchievement;
    }

    public sealed class ItemLevelSelectorRecord
    {
        public uint ID;
        public ushort ItemLevel;
        public ushort ItemLevelSelectorQualitySetID;
    }

    public sealed class ItemLevelSelectorQualityRecord
    {
        public uint ID;
        public uint ItemBonusListID;
        public byte Quality;
        public uint ItemLevelSelectorQualitySetID;
    }

    public sealed class ItemLevelSelectorQualitySetRecord
    {
        public uint ID;
        public ushort ItemLevelMin;
        public ushort ItemLevelMax;
    }

    public sealed class ItemLimitCategoryRecord
    {
        public uint Id;
        public LocalizedString Name;
        public byte Quantity;
        public byte Flags;
    }

    public sealed class ItemModifiedAppearanceRecord
    {
        public uint ItemID;
        public uint Id;
        public byte AppearanceModID;
        public ushort AppearanceID;
        public byte Index;
        public byte SourceType;
    }

    public sealed class ItemPriceBaseRecord
    {
        public uint Id;
        public float ArmorFactor;
        public float WeaponFactor;
        public ushort ItemLevel;
    }

    public sealed class ItemRandomPropertiesRecord
    {
        public uint Id;
        public LocalizedString Name;
        public ushort[] Enchantment = new ushort[ItemConst.MaxItemRandomProperties];
    }

    public sealed class ItemRandomSuffixRecord
    {
        public uint Id;
        public LocalizedString Name;
        public ushort[] Enchantment = new ushort[ItemConst.MaxItemRandomProperties];
        public ushort[] AllocationPct = new ushort[ItemConst.MaxItemRandomProperties];
    }

    public sealed class ItemSearchNameRecord
    {
        public ulong AllowableRace;
        public LocalizedString Name;
        public uint Id;
        public uint[] Flags = new uint[3];
        public ushort ItemLevel;
        public byte Quality;
        public byte RequiredExpansion;
        public byte RequiredLevel;
        public ushort RequiredReputationFaction;
        public byte RequiredReputationRank;
        public short AllowableClass;
        public ushort RequiredSkill;
        public ushort RequiredSkillRank;
        public uint RequiredSpell;
    }

    public sealed class ItemSetRecord
    {
        public uint Id;
        public LocalizedString Name;
        public uint[] ItemID = new uint[17];
        public ushort RequiredSkillRank;
        public byte RequiredSkill;
        public ItemSetFlags Flags;
    }

    public sealed class ItemSetSpellRecord
    {
        public uint Id;
        public uint SpellID;
        public ushort ChrSpecID;
        public byte Threshold;
        public uint ItemSetID;
    }

    public sealed class ItemSparseRecord
    {
        public uint Id;
        public long AllowableRace;
        public LocalizedString Name;
        public string Name2;
        public string Name3;
        public string Name4;
        public string Description;
        public uint[] Flags = new uint[4];
        public float Unk1;
        public float Unk2;
        public uint BuyCount;
        public uint BuyPrice;
        public uint SellPrice;
        public uint RequiredSpell;
        public uint MaxCount;
        public uint Stackable;
        public int[] ItemStatAllocation = new int[ItemConst.MaxStats];
        public float[] ItemStatSocketCostMultiplier = new float[ItemConst.MaxStats];
        public float RangedModRange;
        public uint BagFamily;
        public float ArmorDamageModifier;//wrong?
        public uint Duration;
        public float StatScalingFactor;
        public short AllowableClass;
        public ushort ItemLevel;
        public ushort RequiredSkill;
        public ushort RequiredSkillRank;
        public ushort RequiredReputationFaction;
        public short[] ItemStatValue = new short[ItemConst.MaxStats];
        public ushort ScalingStatDistribution;
        public ushort Delay;
        public ushort PageText;
        public ushort StartQuest;
        public ushort LockID;
        public ushort RandomProperty;
        public ushort RandomSuffix;
        public ushort ItemSet;
        public ushort Area;
        public ushort Map;
        public ushort TotemCategory;
        public ushort SocketBonus;
        public ushort GemProperties;
        public ushort ItemLimitCategory;
        public ushort HolidayID;
        public ushort RequiredTransmogHolidayID;
        public ushort ItemNameDescriptionID;
        public byte Quality;
        public InventoryType inventoryType;
        public sbyte RequiredLevel;
        public byte RequiredHonorRank;
        public byte RequiredCityRank;
        public byte RequiredReputationRank;
        public byte ContainerSlots;
        public sbyte[] ItemStatType = new sbyte[ItemConst.MaxStats];
        public byte DamageType;
        public byte Bonding;
        public byte LanguageID;
        public byte PageMaterial;
        public sbyte Material;
        public byte Sheath;
        public byte[] SocketColor = new byte[ItemConst.MaxGemSockets];
        public byte CurrencySubstitutionID;
        public byte CurrencySubstitutionCount;
        public byte ArtifactID;
        public byte RequiredExpansion;
    }

    public sealed class ItemSpecRecord
    {
        public uint Id;
        public ushort SpecID;
        public byte MinLevel;
        public byte MaxLevel;
        public byte ItemType;
        public ItemSpecStat PrimaryStat;
        public ItemSpecStat SecondaryStat;
    }

    public sealed class ItemSpecOverrideRecord
    {
        public uint Id;
        public ushort SpecID;
        public uint ItemID;
    }

    public sealed class ItemUpgradeRecord
    {
        public uint Id;
        public uint CurrencyCost;
        public ushort PrevItemUpgradeID;
        public ushort CurrencyID;
        public byte ItemUpgradePathID;
        public byte ItemLevelBonus;
    }

    public sealed class ItemXBonusTreeRecord
    {
        public uint Id;
        public ushort BonusTreeID;
        public uint ItemID;
    }
}
