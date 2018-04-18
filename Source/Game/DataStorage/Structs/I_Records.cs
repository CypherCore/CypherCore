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
        public float ClothModifier;
        public float LeatherModifier;
        public float ChainModifier;
        public float PlateModifier;
    }

    public sealed class ImportPriceQualityRecord
    {
        public uint Id;
        public float Data;
    }

    public sealed class ImportPriceShieldRecord
    {
        public uint Id;
        public float Data;
    }

    public sealed class ImportPriceWeaponRecord
    {
        public uint Id;
        public float Data;
    }

    public sealed class ItemRecord
    {
        public uint Id;
        public uint IconFileDataID;
        public ItemClass ClassID;
        public byte SubclassID;
        public sbyte SoundOverrideSubclassID;
        public byte Material;
        public InventoryType inventoryType;
        public byte SheatheType;
        public byte ItemGroupSoundsID;
    }

    public sealed class ItemAppearanceRecord
    {
        public uint Id;
        public uint ItemDisplayInfoID;
        public uint DefaultIconFileDataID;
        public uint UIOrder;
        public byte DisplayType;
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
        public float Cloth;
        public float Leather;
        public float Mail;
        public float Plate;
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
        public ushort ParentItemBonusListID;
        public ItemBonusType Type;
        public byte OrderIndex;
    }

    public sealed class ItemBonusListLevelDeltaRecord
    {
        public short ItemLevelDelta;
        public uint Id;
    }

    public sealed class ItemBonusTreeNodeRecord
    {
        public uint Id;
        public ushort ChildItemBonusTreeID;
        public ushort ChildItemBonusListID;
        public ushort ChildItemLevelSelectorID;
        public byte ItemContext;
        public uint ParentItemBonusTreeID;
    }

    public sealed class ItemChildEquipmentRecord
    {
        public uint Id;
        public uint ChildItemID;
        public byte ChildItemEquipSlot;
        public uint ParentItemID;
    }

    public sealed class ItemClassRecord
    {
        public uint Id;
        public string ClassName;
        public float PriceModifier;
        public byte ClassID;
        public byte Flags;
    }

    public sealed class ItemCurrencyCostRecord
    {
        public uint Id;
        public uint ItemID;
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
        public float[] Quality = new float[7];
        public ushort ItemLevel;
    }

    public sealed class ItemDisenchantLootRecord
    {
        public uint Id;
        public ushort MinLevel;
        public ushort MaxLevel;
        public ushort SkillRequired;
        public sbyte Subclass;
        public byte Quality;
        public sbyte ExpansionID;
        public uint ClassID;
    }

    public sealed class ItemEffectRecord
    {
        public uint Id;
        public int SpellID;
        public int CoolDownMSec;
        public int CategoryCoolDownMSec;
        public short Charges;
        public ushort SpellCategoryID;
        public ushort ChrSpecializationID;
        public byte LegacySlotIndex;
        public ItemSpelltriggerType TriggerType;
        public uint ParentItemID;
    }

    public sealed class ItemExtendedCostRecord
    {
        public uint Id;
        public uint[] ItemID = new uint[ItemConst.MaxItemExtCostItems];                   // required item id
        public uint[] CurrencyCount = new uint[ItemConst.MaxItemExtCostCurrencies];     // required curency count
        public ushort[] ItemCount = new ushort[ItemConst.MaxItemExtCostItems];              // required count of 1st item
        public ushort RequiredArenaRating;                             // required personal arena rating
        public ushort[] CurrencyID = new ushort[ItemConst.MaxItemExtCostCurrencies];          // required curency id
        public byte ArenaBracket;                                        // arena slot restrictions (min slot value)
        public byte MinFactionID;
        public byte MinReputation;
        public byte Flags;
        public byte RequiredAchievement;
    }

    public sealed class ItemLevelSelectorRecord
    {
        public uint ID;
        public ushort MinItemLevel;
        public ushort ItemLevelSelectorQualitySetID;
    }

    public sealed class ItemLevelSelectorQualityRecord
    {
        public uint ID;
        public uint QualityItemBonusListID;
        public byte Quality;
        public uint ParentILSQualitySetID;
    }

    public sealed class ItemLevelSelectorQualitySetRecord
    {
        public uint ID;
        public ushort IlvlRare;
        public ushort IlvlEpic;
    }

    public sealed class ItemLimitCategoryRecord
    {
        public uint Id;
        public LocalizedString Name;
        public byte Quantity;
        public byte Flags;
    }

    public sealed class ItemLimitCategoryConditionRecord
    {
        public uint Id;
        public sbyte AddQuantity;
        public uint PlayerConditionID;
        public int ParentItemLimitCategoryID;
    }

    public sealed class ItemModifiedAppearanceRecord
    {
        public uint ItemID;
        public uint Id;
        public byte ItemAppearanceModifierID;
        public ushort ItemAppearanceID;
        public byte OrderIndex;
        public byte TransmogSourceTypeEnum;
    }

    public sealed class ItemPriceBaseRecord
    {
        public uint Id;
        public float Armor;
        public float Weapon;
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
        public LocalizedString Display;
        public uint Id;
        public uint[] Flags = new uint[3];
        public ushort ItemLevel;
        public byte OverallQualityID;
        public byte ExpansionID;
        public byte RequiredLevel;
        public ushort MinFactionID;
        public byte MinReputation;
        public short AllowableClass;
        public ushort RequiredSkill;
        public ushort RequiredSkillRank;
        public uint RequiredAbility;
    }

    public sealed class ItemSetRecord
    {
        public uint Id;
        public LocalizedString Name;
        public uint[] ItemID = new uint[17];
        public ushort RequiredSkillRank;
        public byte RequiredSkill;
        public ItemSetFlags SetFlags;
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
        public LocalizedString Display;
        public string Display2;
        public string Display3;
        public string Display4;
        public string Description;
        public uint[] Flags = new uint[4];
        public float PriceRandomValue;
        public float PriceVariance;
        public uint VendorStackCount;
        public uint BuyPrice;
        public uint SellPrice;
        public uint RequiredAbility;
        public uint MaxCount;
        public uint Stackable;
        public int[] StatPercentEditor = new int[ItemConst.MaxStats];
        public float[] StatPercentageOfSocket = new float[ItemConst.MaxStats];
        public float ItemRange;
        public uint BagFamily;
        public float QualityModifier;
        public uint DurationInInventory;
        public float DmgVariance;
        public short AllowableClass;
        public ushort ItemLevel;
        public ushort RequiredSkill;
        public ushort RequiredSkillRank;
        public ushort MinFactionID;
        public short[] ItemStatValue = new short[ItemConst.MaxStats];
        public ushort ScalingStatDistributionID;
        public ushort ItemDelay;
        public ushort PageID;
        public ushort StartQuestID;
        public ushort LockID;
        public ushort RandomSelect;
        public ushort ItemRandomSuffixGroupID;
        public ushort ItemSet;
        public ushort ZoneBound;
        public ushort InstanceBound;
        public ushort TotemCategoryID;
        public ushort SocketMatchEnchantmentId;
        public ushort GemProperties;
        public ushort LimitCategory;
        public ushort RequiredHoliday;
        public ushort RequiredTransmogHoliday;
        public ushort ItemNameDescriptionID;
        public byte OverallQualityID;
        public InventoryType inventoryType;
        public sbyte RequiredLevel;
        public byte RequiredPVPRank;
        public byte RequiredPVPMedal;
        public byte MinReputation;
        public byte ContainerSlots;
        public sbyte[] StatModifierBonusStat = new sbyte[ItemConst.MaxStats];
        public byte DamageType;
        public byte Bonding;
        public byte LanguageID;
        public byte PageMaterialID;
        public sbyte Material;
        public byte SheatheType;
        public byte[] SocketType = new byte[ItemConst.MaxGemSockets];
        public byte SpellWeightCategory;
        public byte SpellWeight;
        public byte ArtifactID;
        public byte ExpansionID;
    }

    public sealed class ItemSpecRecord
    {
        public uint Id;
        public ushort SpecializationID;
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
        public uint CurrencyAmount;
        public ushort PrerequisiteID;
        public ushort CurrencyType;
        public byte ItemUpgradePathID;
        public byte ItemLevelIncrement;
    }

    public sealed class ItemXBonusTreeRecord
    {
        public uint Id;
        public ushort ItemBonusTreeID;
        public uint ItemID;
    }
}
