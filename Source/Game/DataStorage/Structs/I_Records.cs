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
        public ItemClass ClassID;
        public byte SubclassID;
        public byte Material;
        public InventoryType inventoryType;
        public byte SheatheType;
        public sbyte SoundOverrideSubclassID;
        public int IconFileDataID;
        public byte ItemGroupSoundsID;
    }

    public sealed class ItemAppearanceRecord
    {
        public uint Id;
        public byte DisplayType;
        public uint ItemDisplayInfoID;
        public int DefaultIconFileDataID;
        public int UiOrder;
    }

    public sealed class ItemArmorQualityRecord
    {
        public uint Id;
        public float[] QualityMod = new float[7];
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
        public short ItemLevel;
        public float Cloth;
        public float Leather;
        public float Mail;
        public float Plate;
    }

    public sealed class ItemBagFamilyRecord
    {
        public uint Id;
        public string Name;
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
        public byte ItemContext;
        public ushort ChildItemBonusTreeID;
        public ushort ChildItemBonusListID;
        public ushort ChildItemLevelSelectorID;
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
        public sbyte ClassID;
        public float PriceModifier;
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
        public ushort ItemLevel;
        public float[] Quality = new float[7];
    }

    public sealed class ItemDisenchantLootRecord
    {
        public uint Id;
        public sbyte Subclass;
        public byte Quality;
        public ushort MinLevel;
        public ushort MaxLevel;
        public ushort SkillRequired;
        public sbyte ExpansionID;
        public uint Class;
    }

    public sealed class ItemEffectRecord
    {
        public uint Id;
        public byte LegacySlotIndex;
        public ItemSpelltriggerType TriggerType;
        public short Charges;
        public int CoolDownMSec;
        public int CategoryCoolDownMSec;
        public ushort SpellCategoryID;
        public int SpellID;
        public ushort ChrSpecializationID;
        public uint ParentItemID;
    }

    public sealed class ItemExtendedCostRecord
    {
        public uint Id;
        public ushort RequiredArenaRating;
        public byte ArenaBracket;                                             // arena slot restrictions (min slot value)
        public byte Flags;
        public byte MinFactionID;
        public byte MinReputation;
        public byte RequiredAchievement;                                      // required personal arena rating
        public uint[] ItemID = new uint[ItemConst.MaxItemExtCostItems];                          // required item id
        public ushort[] ItemCount = new ushort[ItemConst.MaxItemExtCostItems];                      // required count of 1st item
        public ushort[] CurrencyID = new ushort[ItemConst.MaxItemExtCostCurrencies];                // required curency id
        public uint[] CurrencyCount = new uint[ItemConst.MaxItemExtCostCurrencies];              // required curency count
    }

    public sealed class ItemLevelSelectorRecord
    {
        public uint Id;
        public ushort MinItemLevel;
        public ushort ItemLevelSelectorQualitySetID;
    }

    public sealed class ItemLevelSelectorQualityRecord
    {
        public uint Id;
        public uint QualityItemBonusListID;
        public sbyte Quality;
        public uint ParentILSQualitySetID;
    }

    public sealed class ItemLevelSelectorQualitySetRecord
    {
        public uint Id;
        public short IlvlRare;
        public short IlvlEpic;
    }

    public sealed class ItemLimitCategoryRecord
    {
        public uint Id;
        public string Name;
        public byte Quantity;
        public byte Flags;
    }

    public sealed class ItemLimitCategoryConditionRecord
    {
        public uint Id;
        public sbyte AddQuantity;
        public uint PlayerConditionID;
        public uint ParentItemLimitCategoryID;
    }

    public sealed class ItemModifiedAppearanceRecord
    {
        public uint Id;
        public uint ItemID;
        public byte ItemAppearanceModifierID;
        public ushort ItemAppearanceID;
        public byte OrderIndex;
        public sbyte TransmogSourceTypeEnum;
    }

    public sealed class ItemPriceBaseRecord
    {
        public uint Id;
        public ushort ItemLevel;
        public float Armor;
        public float Weapon;
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
        public long AllowableRace;
        public string Display;
        public uint Id;
        public byte OverallQualityID;
        public byte ExpansionID;
        public ushort MinFactionID;
        public byte MinReputation;
        public int AllowableClass;
        public sbyte RequiredLevel;
        public ushort RequiredSkill;
        public ushort RequiredSkillRank;
        public uint RequiredAbility;
        public ushort ItemLevel;
        public int[] Flags = new int[4];
    }

    public sealed class ItemSetRecord
    {
        public uint Id;
        public LocalizedString Name;
        public ItemSetFlags SetFlags;
        public uint RequiredSkill;
        public ushort RequiredSkillRank;
        public uint[] ItemID = new uint[ItemConst.MaxItemSetItems];
    }

    public sealed class ItemSetSpellRecord
    {
        public uint Id;
        public ushort ChrSpecID;
        public uint SpellID;
        public byte Threshold;
        public uint ItemSetID;
    }

    public sealed class ItemSparseRecord
    {
        public uint Id;
        public long AllowableRace;
        public string Description;
        public string Display3;
        public string Display2;
        public string Display1;
        public LocalizedString Display;
        public float DmgVariance;
        public uint DurationInInventory;
        public float QualityModifier;
        public uint BagFamily;
        public float ItemRange;
        public float[] StatPercentageOfSocket = new float[ItemConst.MaxStats];
        public int[] StatPercentEditor = new int[ItemConst.MaxStats];
        public uint Stackable;
        public uint MaxCount;
        public uint RequiredAbility;
        public uint SellPrice;
        public uint BuyPrice;
        public uint VendorStackCount;
        public float PriceVariance;
        public float PriceRandomValue;
        public uint[] Flags = new uint[4];
        public int FactionRelated;
        public ushort ItemNameDescriptionID;
        public ushort RequiredTransmogHoliday;
        public ushort RequiredHoliday;
        public ushort LimitCategory;
        public ushort GemProperties;
        public ushort SocketMatchEnchantmentId;
        public ushort TotemCategoryID;
        public ushort InstanceBound;
        public ushort ZoneBound;
        public ushort ItemSet;
        public ushort ItemRandomSuffixGroupID;
        public ushort RandomSelect;
        public ushort LockID;
        public ushort StartQuestID;
        public ushort PageID;
        public ushort ItemDelay;
        public ushort ScalingStatDistributionID;
        public ushort MinFactionID;
        public ushort RequiredSkillRank;
        public ushort RequiredSkill;
        public ushort ItemLevel;
        public short AllowableClass;
        public byte ExpansionID;
        public byte ArtifactID;
        public byte SpellWeight;
        public byte SpellWeightCategory;
        public byte[] SocketType = new byte[ItemConst.MaxGemSockets];
        public byte SheatheType;
        public byte Material;
        public byte PageMaterialID;
        public byte LanguageID;
        public byte Bonding;
        public byte DamageType;
        public sbyte[] StatModifierBonusStat = new sbyte[ItemConst.MaxStats];
        public byte ContainerSlots;
        public byte MinReputation;
        public byte RequiredPVPMedal;
        public byte RequiredPVPRank;
        public sbyte RequiredLevel;
        public InventoryType inventoryType;
        public byte OverallQualityID;
    }

    public sealed class ItemSpecRecord
    {
        public uint Id;
        public byte MinLevel;
        public byte MaxLevel;
        public byte ItemType;
        public ItemSpecStat PrimaryStat;
        public ItemSpecStat SecondaryStat;
        public ushort SpecializationID;
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
        public byte ItemUpgradePathID;
        public byte ItemLevelIncrement;
        public ushort PrerequisiteID;
        public ushort CurrencyType;
        public uint CurrencyAmount;
    }

    public sealed class ItemXBonusTreeRecord
    {
        public uint Id;
        public ushort ItemBonusTreeID;
        public uint ItemID;
    }
}
