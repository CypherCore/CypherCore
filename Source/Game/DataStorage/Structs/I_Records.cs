// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;

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
        public int ContentTuningID;
        public int ModifiedCraftingReagentItemID;
        public int CraftingQualityID;
    }

    public sealed class ItemAppearanceRecord
    {
        public uint Id;
        public sbyte DisplayType;
        public uint ItemDisplayInfoID;
        public int DefaultIconFileDataID;
        public int UiOrder;
        public int PlayerConditionID;
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
        public int[] Value = new int[4];
        public ushort ParentItemBonusListID;
        public ItemBonusType BonusType;
        public byte OrderIndex;
    }

    public sealed class ItemBonusListGroupEntryRecord
    {
        public uint Id;
        public uint ItemBonusListGroupID;
        public int ItemBonusListID;
        public int ItemLevelSelectorID;
        public int SequenceValue;
        public int ItemExtendedCostID;
        public int PlayerConditionID;
        public int Flags;
        public int ItemLogicalCostGroupID;
    }

    public sealed class ItemBonusListLevelDeltaRecord
    {
        public short ItemLevelDelta;
        public uint Id;
    }

    public sealed class ItemBonusSequenceSpellRecord
    {
        public uint Id;
        public int SpellID;
        public int ItemID;
    }

    public sealed class ItemBonusTreeRecord
    {
        public uint Id;
        public int Flags;
        public int InventoryTypeSlotMask;
    }

    public sealed class ItemBonusTreeNodeRecord
    {
        public uint Id;
        public byte ItemContext;
        public ushort ChildItemBonusTreeID;
        public ushort ChildItemBonusListID;
        public ushort ChildItemLevelSelectorID;
        public uint ChildItemBonusListGroupID;
        public uint IblGroupPointsModSetID;
        public int MinMythicPlusLevel;
        public int MaxMythicPlusLevel;
        public uint ParentItemBonusTreeID;
    }

    public sealed class ItemChildEquipmentRecord
    {
        public uint Id;
        public uint ParentItemID;
        public uint ChildItemID;
        public byte ChildItemEquipSlot;
    }

    public sealed class ItemClassRecord
    {
        public uint Id;
        public string ClassName;
        public sbyte ClassID;
        public float PriceModifier;
        public byte Flags;
    }

    public sealed class ItemContextPickerEntryRecord
    {
        public uint Id;
        public byte ItemCreationContext;
        public byte OrderIndex;
        public int PVal;
        public int LabelID;
        public uint Flags;
        public uint PlayerConditionID;
        public uint ItemContextPickerID;
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
    }

    public sealed class ItemExtendedCostRecord
    {
        public uint Id;
        public ushort RequiredArenaRating;
        public byte ArenaBracket;                                             // arena slot restrictions (min slot value)
        public byte Flags;
        public byte MinFactionID;
        public int MinReputation;
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
        public ushort AzeriteUnlockMappingSet;
    }

    public sealed class ItemLevelSelectorQualityRecord : IEquatable<ItemLevelSelectorQualityRecord>, IEquatable<ItemQuality>
    {
        public uint Id;
        public uint QualityItemBonusListID;
        public sbyte Quality;
        public uint ParentILSQualitySetID;

        public bool Equals(ItemLevelSelectorQualityRecord other) { return Quality < other.Quality; }

        public bool Equals(ItemQuality quality) { return Quality < (sbyte)quality; }
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
        public int ItemAppearanceModifierID;
        public int ItemAppearanceID;
        public int OrderIndex;
        public byte TransmogSourceTypeEnum;
        public int Flags;
    }

    public sealed class ItemModifiedAppearanceExtraRecord
    {
        public uint Id;
        public int IconFileDataID;
        public int UnequippedIconFileDataID;
        public byte SheatheType;
        public sbyte DisplayWeaponSubclassID;
        public sbyte DisplayInventoryType;
    }

    public sealed class ItemNameDescriptionRecord
    {
        public uint Id;
        public LocalizedString Description;
        public int Color;
    }

    public sealed class ItemPriceBaseRecord
    {
        public uint Id;
        public ushort ItemLevel;
        public float Armor;
        public float Weapon;
    }

    public sealed class ItemSearchNameRecord
    {
        public uint Id;
        public long AllowableRace;
        public string Display;
        public byte OverallQualityID;
        public int ExpansionID;
        public ushort MinFactionID;
        public int MinReputation;
        public int AllowableClass;
        public sbyte RequiredLevel;
        public ushort RequiredSkill;
        public ushort RequiredSkillRank;
        public uint RequiredAbility;
        public ushort ItemLevel;
        public int[] Flags = new int[5];
    }

    public sealed class ItemSetRecord
    {
        public uint Id;
        public LocalizedString Name;
        public int SetFlags;
        public uint RequiredSkill;
        public ushort RequiredSkillRank;
        public uint[] ItemID = new uint[ItemConst.MaxItemSetItems];

        public bool HasFlag(ItemSetFlags itemSetFlags) { return (SetFlags & (int)itemSetFlags) != 0; }
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
        public int ExpansionID;
        public float DmgVariance;
        public uint LimitCategory;
        public uint DurationInInventory;
        public float QualityModifier;
        public uint BagFamily;
        public uint StartQuestID;
        public int LanguageID;
        public float ItemRange;
        public float[] StatPercentageOfSocket = new float[ItemConst.MaxStats];
        public int[] StatPercentEditor = new int[ItemConst.MaxStats];
        public int[] StatModifierBonusStat = new int[ItemConst.MaxStats];
        public uint Stackable;
        public uint MaxCount;
        public uint MinReputation;
        public uint RequiredAbility;
        public uint SellPrice;
        public uint BuyPrice;
        public uint VendorStackCount;
        public float PriceVariance;
        public float PriceRandomValue;
        public int[] Flags = new int[5];
        public uint FactionRelated;
        public int ModifiedCraftingReagentItemID;
        public uint ContentTuningID;
        public uint PlayerLevelToItemLevelCurveID;
        public ushort ItemNameDescriptionID;
        public ushort RequiredTransmogHoliday;
        public ushort RequiredHoliday;
        public ushort GemProperties;
        public ushort SocketMatchEnchantmentId;
        public ushort TotemCategoryID;
        public ushort InstanceBound;
        public ushort[] ZoneBound = new ushort[2];
        public ushort ItemSet;
        public ushort LockID;
        public ushort PageID;
        public ushort ItemDelay;
        public ushort MinFactionID;
        public ushort RequiredSkillRank;
        public ushort RequiredSkill;
        public ushort ItemLevel;
        public short AllowableClass;
        public byte ArtifactID;
        public byte SpellWeight;
        public byte SpellWeightCategory;
        public byte[] SocketType = new byte[ItemConst.MaxGemSockets];
        public byte SheatheType;
        public byte Material;
        public byte PageMaterialID;
        public byte Bonding;
        public byte DamageType;
        public byte ContainerSlots;
        public byte RequiredPVPMedal;
        public sbyte RequiredPVPRank;
        public sbyte RequiredLevel;
        public InventoryType inventoryType;
        public sbyte OverallQualityID;
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
