// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class ImportPriceArmorRecord
    {
        public float ChainModifier;
        public float ClothModifier;
        public uint Id;
        public float LeatherModifier;
        public float PlateModifier;
    }

    public sealed class ImportPriceQualityRecord
    {
        public float Data;
        public uint Id;
    }

    public sealed class ImportPriceShieldRecord
    {
        public float Data;
        public uint Id;
    }

    public sealed class ImportPriceWeaponRecord
    {
        public float Data;
        public uint Id;
    }

    public sealed class ItemRecord
    {
        public ItemClass ClassID;
        public int ContentTuningID;
        public int CraftingQualityID;
        public int IconFileDataID;
        public uint Id;
        public InventoryType inventoryType;
        public byte ItemGroupSoundsID;
        public byte Material;
        public int ModifiedCraftingReagentItemID;
        public byte SheatheType;
        public sbyte SoundOverrideSubclassID;
        public byte SubclassID;
    }

    public sealed class ItemAppearanceRecord
    {
        public int DefaultIconFileDataID;
        public int DisplayType;
        public uint Id;
        public uint ItemDisplayInfoID;
        public int PlayerConditionID;
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
        public ushort ItemLevel;
        public float[] Quality = new float[7];
    }

    public sealed class ItemArmorTotalRecord
    {
        public float Cloth;
        public uint Id;
        public short ItemLevel;
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
        public ItemBonusType BonusType;
        public uint Id;
        public byte OrderIndex;
        public ushort ParentItemBonusListID;
        public int[] Value = new int[4];
    }

    public sealed class ItemBonusListGroupEntryRecord
    {
        public uint Id;
        public uint ItemBonusListGroupID;
        public int ItemBonusListID;
        public int ItemExtendedCostID;
        public int ItemLevelSelectorID;
        public int OrderIndex;
        public int PlayerConditionID;
    }

    public sealed class ItemBonusListLevelDeltaRecord
    {
        public uint Id;
        public short ItemLevelDelta;
    }

    public sealed class ItemBonusSequenceSpellRecord
    {
        public uint Id;
        public int ItemID;
        public int SpellID;
    }

    public sealed class ItemBonusTreeNodeRecord
    {
        public uint ChildItemBonusListGroupID;
        public ushort ChildItemBonusListID;
        public ushort ChildItemBonusTreeID;
        public ushort ChildItemLevelSelectorID;
        public uint IblGroupPointsModSetID;
        public uint Id;
        public byte ItemContext;
        public uint ParentItemBonusTreeID;
    }

    public sealed class ItemChildEquipmentRecord
    {
        public byte ChildItemEquipSlot;
        public uint ChildItemID;
        public uint Id;
        public uint ParentItemID;
    }

    public sealed class ItemClassRecord
    {
        public sbyte ClassID;
        public string ClassName;
        public byte Flags;
        public uint Id;
        public float PriceModifier;
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
        public uint Class;
        public sbyte ExpansionID;
        public uint Id;
        public ushort MaxLevel;
        public ushort MinLevel;
        public byte Quality;
        public ushort SkillRequired;
        public sbyte Subclass;
    }

    public sealed class ItemEffectRecord
    {
        public int CategoryCoolDownMSec;
        public short Charges;
        public ushort ChrSpecializationID;
        public int CoolDownMSec;
        public uint Id;
        public byte LegacySlotIndex;
        public ushort SpellCategoryID;
        public int SpellID;
        public ItemSpelltriggerType TriggerType;
    }

    public sealed class ItemExtendedCostRecord
    {
        public byte ArenaBracket;                                                    // arena Slot restrictions (min Slot value)
        public uint[] CurrencyCount = new uint[ItemConst.MaxItemExtCostCurrencies];  // required curency Count
        public ushort[] CurrencyID = new ushort[ItemConst.MaxItemExtCostCurrencies]; // required curency Id
        public byte Flags;
        public uint Id;
        public ushort[] ItemCount = new ushort[ItemConst.MaxItemExtCostItems]; // required Count of 1st Item
        public uint[] ItemID = new uint[ItemConst.MaxItemExtCostItems];        // required Item Id
        public byte MinFactionID;
        public int MinReputation;
        public byte RequiredAchievement; // required personal arena rating
        public ushort RequiredArenaRating;
    }

    public sealed class ItemLevelSelectorRecord
    {
        public ushort AzeriteUnlockMappingSet;
        public uint Id;
        public ushort ItemLevelSelectorQualitySetID;
        public ushort MinItemLevel;
    }

    public sealed class ItemLevelSelectorQualityRecord
    {
        public uint Id;
        public uint ParentILSQualitySetID;
        public sbyte Quality;
        public uint QualityItemBonusListID;
    }

    public sealed class ItemLevelSelectorQualitySetRecord
    {
        public uint Id;
        public short IlvlEpic;
        public short IlvlRare;
    }

    public sealed class ItemLimitCategoryRecord
    {
        public byte Flags;
        public uint Id;
        public string Name;
        public byte Quantity;
    }

    public sealed class ItemLimitCategoryConditionRecord
    {
        public sbyte AddQuantity;
        public uint Id;
        public uint ParentItemLimitCategoryID;
        public uint PlayerConditionID;
    }

    public sealed class ItemModifiedAppearanceRecord
    {
        public uint Id;
        public int ItemAppearanceID;
        public int ItemAppearanceModifierID;
        public uint ItemID;
        public int OrderIndex;
        public byte TransmogSourceTypeEnum;
    }

    public sealed class ItemModifiedAppearanceExtraRecord
    {
        public sbyte DisplayInventoryType;
        public sbyte DisplayWeaponSubclassID;
        public int IconFileDataID;
        public uint Id;
        public byte SheatheType;
        public int UnequippedIconFileDataID;
    }

    public sealed class ItemNameDescriptionRecord
    {
        public int Color;
        public LocalizedString Description;
        public uint Id;
    }

    public sealed class ItemPriceBaseRecord
    {
        public float Armor;
        public uint Id;
        public ushort ItemLevel;
        public float Weapon;
    }

    public sealed class ItemSearchNameRecord
    {
        public int AllowableClass;
        public long AllowableRace;
        public string Display;
        public int ExpansionID;
        public int[] Flags = new int[4];
        public uint Id;
        public ushort ItemLevel;
        public ushort MinFactionID;
        public int MinReputation;
        public byte OverallQualityID;
        public uint RequiredAbility;
        public sbyte RequiredLevel;
        public ushort RequiredSkill;
        public ushort RequiredSkillRank;
    }

    public sealed class ItemSetRecord
    {
        public uint Id;
        public uint[] ItemID = new uint[ItemConst.MaxItemSetItems];
        public LocalizedString Name;
        public uint RequiredSkill;
        public ushort RequiredSkillRank;
        public ItemSetFlags SetFlags;
    }

    public sealed class ItemSetSpellRecord
    {
        public ushort ChrSpecID;
        public uint Id;
        public uint ItemSetID;
        public uint SpellID;
        public byte Threshold;
    }

    public sealed class ItemSparseRecord
    {
        public short AllowableClass;
        public long AllowableRace;
        public byte ArtifactID;
        public uint BagFamily;
        public byte Bonding;
        public uint BuyPrice;
        public byte ContainerSlots;
        public uint ContentTuningID;
        public byte DamageType;
        public string Description;
        public LocalizedString Display;
        public string Display1;
        public string Display2;
        public string Display3;
        public float DmgVariance;
        public uint DurationInInventory;
        public int ExpansionID;
        public uint FactionRelated;
        public int[] Flags = new int[4];
        public ushort GemProperties;
        public uint Id;
        public ushort InstanceBound;
        public InventoryType inventoryType;
        public ushort ItemDelay;
        public ushort ItemLevel;
        public ushort ItemNameDescriptionID;
        public float ItemRange;
        public ushort ItemSet;
        public int LanguageID;
        public uint LimitCategory;
        public ushort LockID;
        public byte Material;
        public uint MaxCount;
        public ushort MinFactionID;
        public uint MinReputation;
        public int ModifiedCraftingReagentItemID;
        public sbyte OverallQualityID;
        public ushort PageID;
        public byte PageMaterialID;
        public uint PlayerLevelToItemLevelCurveID;
        public float PriceRandomValue;
        public float PriceVariance;
        public float QualityModifier;
        public uint RequiredAbility;
        public ushort RequiredHoliday;
        public sbyte RequiredLevel;
        public byte RequiredPVPMedal;
        public byte RequiredPVPRank;
        public ushort RequiredSkill;
        public ushort RequiredSkillRank;
        public ushort RequiredTransmogHoliday;
        public uint SellPrice;
        public byte SheatheType;
        public ushort SocketMatchEnchantmentId;
        public byte[] SocketType = new byte[ItemConst.MaxGemSockets];
        public byte SpellWeight;
        public byte SpellWeightCategory;
        public uint Stackable;
        public uint StartQuestID;
        public sbyte[] StatModifierBonusStat = new sbyte[ItemConst.MaxStats];
        public float[] StatPercentageOfSocket = new float[ItemConst.MaxStats];
        public int[] StatPercentEditor = new int[ItemConst.MaxStats];
        public ushort TotemCategoryID;
        public uint VendorStackCount;
        public ushort[] ZoneBound = new ushort[2];
    }

    public sealed class ItemSpecRecord
    {
        public uint Id;
        public byte ItemType;
        public byte MaxLevel;
        public byte MinLevel;
        public ItemSpecStat PrimaryStat;
        public ItemSpecStat SecondaryStat;
        public ushort SpecializationID;
    }

    public sealed class ItemSpecOverrideRecord
    {
        public uint Id;
        public uint ItemID;
        public ushort SpecID;
    }

    public sealed class ItemXBonusTreeRecord
    {
        public uint Id;
        public ushort ItemBonusTreeID;
        public uint ItemID;
    }

    public sealed class ItemXItemEffectRecord
    {
        public uint Id;
        public int ItemEffectID;
        public uint ItemID;
    }
}