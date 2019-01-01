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
using Game.DataStorage;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Game.Entities
{
    public class ItemTemplate
    {
        public ItemTemplate(ItemRecord item, ItemSparseRecord sparse)
        {
            BasicData = item;
            ExtendedData = sparse;

            Specializations[0] = new BitArray((int)Class.Max * PlayerConst.MaxSpecializations);
            Specializations[1] = new BitArray((int)Class.Max * PlayerConst.MaxSpecializations);
            Specializations[2] = new BitArray((int)Class.Max * PlayerConst.MaxSpecializations);
        }

        public string GetName(LocaleConstant locale = SharedConst.DefaultLocale)
        {
            return ExtendedData.Display[locale];
        }

        public bool CanChangeEquipStateInCombat()
        {
            switch (GetInventoryType())
            {
                case InventoryType.Relic:
                case InventoryType.Shield:
                case InventoryType.Holdable:
                    return true;
                default:
                    break;
            }

            switch (GetClass())
            {
                case ItemClass.Weapon:
                case ItemClass.Projectile:
                    return true;
            }

            return false;
        }

        public SkillType GetSkill()
        {
            SkillType[] item_weapon_skills =
            {
                SkillType.Axes,             SkillType.TwoHandedAxes,    SkillType.Bows,     SkillType.Guns,             SkillType.Maces,
                SkillType.TwoHandedMaces,   SkillType.Polearms,         SkillType.Swords,   SkillType.TwoHandedSwords,  SkillType.Warglaives,
                SkillType.Staves,           0,                          0,                  SkillType.FistWeapons,      0,
                SkillType.Daggers,          0,                          0,                  SkillType.Crossbows,        SkillType.Wands,
                SkillType.Fishing
            };

            SkillType[] item_armor_skills =
            {
                0, SkillType.Cloth, SkillType.Leather, SkillType.Mail, SkillType.PlateMail, 0, SkillType.Shield, 0, 0, 0, 0, 0
            };


            switch (GetClass())
            {
                case ItemClass.Weapon:
                    if (GetSubClass() >= (int)ItemSubClassWeapon.Max)
                        return 0;
                    else
                        return item_weapon_skills[GetSubClass()];

                case ItemClass.Armor:
                    if (GetSubClass() >= (int)ItemSubClassArmor.Max)
                        return 0;
                    else
                        return item_armor_skills[GetSubClass()];

                default:
                    return 0;
            }
        }

        public uint GetArmor(uint itemLevel)
        {
            ItemQuality quality = GetQuality() != ItemQuality.Heirloom ? GetQuality() : ItemQuality.Rare;
            if (quality > ItemQuality.Artifact)
                return 0;

            // all items but shields
            if (GetClass() != ItemClass.Armor || GetSubClass() != (uint)ItemSubClassArmor.Shield)
            {
                ItemArmorQualityRecord armorQuality = CliDB.ItemArmorQualityStorage.LookupByKey(itemLevel);
                ItemArmorTotalRecord armorTotal = CliDB.ItemArmorTotalStorage.LookupByKey(itemLevel);
                if (armorQuality == null || armorTotal == null)
                    return 0;

                InventoryType inventoryType = GetInventoryType();
                if (inventoryType == InventoryType.Robe)
                    inventoryType = InventoryType.Chest;

                ArmorLocationRecord location = CliDB.ArmorLocationStorage.LookupByKey(inventoryType);
                if (location == null)
                    return 0;

                if (GetSubClass() < (uint)ItemSubClassArmor.Cloth || GetSubClass() > (uint)ItemSubClassArmor.Plate)
                    return 0;

                float total = 1.0f;
                float locationModifier = 1.0f;
                switch ((ItemSubClassArmor)GetSubClass())
                {
                    case ItemSubClassArmor.Cloth:
                        total = armorTotal.Cloth;
                        locationModifier = location.Clothmodifier;
                        break;
                    case ItemSubClassArmor.Leather:
                        total = armorTotal.Leather;
                        locationModifier = location.Leathermodifier;
                        break;
                    case ItemSubClassArmor.Mail:
                        total = armorTotal.Mail;
                        locationModifier = location.Chainmodifier;
                        break;
                    case ItemSubClassArmor.Plate:
                        total = armorTotal.Plate;
                        locationModifier = location.Platemodifier;
                        break;
                    default:
                        break;
                }

                return (uint)(armorQuality.QualityMod[(int)quality] * total * locationModifier + 0.5f);
            }

            // shields
            ItemArmorShieldRecord shield = CliDB.ItemArmorShieldStorage.LookupByKey(itemLevel);
            if (shield == null)
                return 0;

            return (uint)(shield.Quality[(int)quality] + 0.5f);
        }

        public void GetDamage(uint itemLevel, out float minDamage, out float maxDamage)
        {
            minDamage = maxDamage = 0.0f;
            ItemQuality quality = GetQuality() != ItemQuality.Heirloom ? GetQuality() : ItemQuality.Rare;
            if (GetClass() != ItemClass.Weapon || quality > ItemQuality.Artifact)
                return;

            // get the right store here
            if (GetInventoryType() > InventoryType.RangedRight)
                return;

            float dps = 0.0f;
            switch (GetInventoryType())
            {
                case InventoryType.Ammo:
                    dps = CliDB.ItemDamageAmmoStorage.LookupByKey(itemLevel).Quality[(int)quality];
                    break;
                case InventoryType.Weapon2Hand:
                    if (GetFlags2().HasAnyFlag(ItemFlags2.CasterWeapon))
                        dps = CliDB.ItemDamageTwoHandCasterStorage.LookupByKey(itemLevel).Quality[(int)quality];
                    else
                        dps = CliDB.ItemDamageTwoHandStorage.LookupByKey(itemLevel).Quality[(int)quality];
                    break;
                case InventoryType.Ranged:
                case InventoryType.Thrown:
                case InventoryType.RangedRight:
                    switch ((ItemSubClassWeapon)GetSubClass())
                    {
                        case ItemSubClassWeapon.Wand:
                            dps = CliDB.ItemDamageOneHandCasterStorage.LookupByKey(itemLevel).Quality[(int)quality];
                            break;
                        case ItemSubClassWeapon.Bow:
                        case ItemSubClassWeapon.Gun:
                        case ItemSubClassWeapon.Crossbow:
                            if (GetFlags2().HasAnyFlag(ItemFlags2.CasterWeapon))
                                dps = CliDB.ItemDamageTwoHandCasterStorage.LookupByKey(itemLevel).Quality[(int)quality];
                            else
                                dps = CliDB.ItemDamageTwoHandStorage.LookupByKey(itemLevel).Quality[(int)quality];
                            break;
                        default:
                            return;
                    }
                    break;
                case InventoryType.Weapon:
                case InventoryType.WeaponMainhand:
                case InventoryType.WeaponOffhand:
                    if (GetFlags2().HasAnyFlag(ItemFlags2.CasterWeapon))
                        dps = CliDB.ItemDamageOneHandCasterStorage.LookupByKey(itemLevel).Quality[(int)quality];
                    else
                        dps = CliDB.ItemDamageOneHandStorage.LookupByKey(itemLevel).Quality[(int)quality];
                    break;
                default:
                    return;
            }

            float avgDamage = dps * GetDelay() * 0.001f;
            minDamage = (GetDmgVariance() * -0.5f + 1.0f) * avgDamage;
            maxDamage = (float)Math.Floor(avgDamage * (GetDmgVariance() * 0.5f + 1.0f) + 0.5f);
        }

        public bool IsUsableByLootSpecialization(Player player, bool alwaysAllowBoundToAccount)
        {
            if (GetFlags().HasAnyFlag(ItemFlags.IsBoundToAccount) && alwaysAllowBoundToAccount)
                return true;

            uint spec = player.GetUInt32Value(ActivePlayerFields.LootSpecId);
            if (spec == 0)
                spec = player.GetUInt32Value(PlayerFields.CurrentSpecId);
            if (spec == 0)
                spec = player.GetDefaultSpecId();

            ChrSpecializationRecord chrSpecialization = CliDB.ChrSpecializationStorage.LookupByKey(spec);
            if (chrSpecialization == null)
                return false;

            int levelIndex = 0;
            if (player.getLevel() >= 110)
                levelIndex = 2;
            else if (player.getLevel() > 40)
                levelIndex = 1;

            return Specializations[levelIndex].Get(CalculateItemSpecBit(chrSpecialization));
        }

        public static int CalculateItemSpecBit(ChrSpecializationRecord spec)
        {
            return (int)((spec.ClassID - 1) * PlayerConst.MaxSpecializations + spec.OrderIndex);
        }

        public uint GetId() { return BasicData.Id; }
        public ItemClass GetClass() { return (ItemClass)BasicData.ClassID; }
        public uint GetSubClass() { return BasicData.SubclassID; }
        public ItemQuality GetQuality() { return (ItemQuality)ExtendedData.OverallQualityID; }
        public ItemFlags GetFlags() { return (ItemFlags)ExtendedData.Flags[0]; }
        public ItemFlags2 GetFlags2() { return (ItemFlags2)ExtendedData.Flags[1]; }
        public ItemFlags3 GetFlags3() { return (ItemFlags3)ExtendedData.Flags[2]; }
        public uint GetFlags4() { return ExtendedData.Flags[3]; }
        public float GetPriceRandomValue() { return ExtendedData.PriceRandomValue; }
        public float GetPriceVariance() { return ExtendedData.PriceVariance; }
        public uint GetBuyCount() { return Math.Max(ExtendedData.VendorStackCount, 1u); }
        public uint GetBuyPrice() { return ExtendedData.BuyPrice; }
        public uint GetSellPrice() { return ExtendedData.SellPrice; }
        public InventoryType GetInventoryType() { return ExtendedData.inventoryType; }
        public int GetAllowableClass() { return ExtendedData.AllowableClass; }
        public long GetAllowableRace() { return ExtendedData.AllowableRace; }
        public uint GetBaseItemLevel() { return ExtendedData.ItemLevel; }
        public int GetBaseRequiredLevel() { return ExtendedData.RequiredLevel; }
        public uint GetRequiredSkill() { return ExtendedData.RequiredSkill; }
        public uint GetRequiredSkillRank() { return ExtendedData.RequiredSkillRank; }
        public uint GetRequiredSpell() { return ExtendedData.RequiredAbility; }
        public uint GetRequiredReputationFaction() { return ExtendedData.MinFactionID; }
        public uint GetRequiredReputationRank() { return ExtendedData.MinReputation; }
        public uint GetMaxCount() { return ExtendedData.MaxCount; }
        public uint GetContainerSlots() { return ExtendedData.ContainerSlots; }
        public int GetItemStatType(uint index)
        {
            Cypher.Assert(index < ItemConst.MaxStats);
            return ExtendedData.StatModifierBonusStat[index];
        }
        public int GetItemStatAllocation(uint index)
        {
            Cypher.Assert(index < ItemConst.MaxStats);
            return ExtendedData.StatPercentEditor[index];
        }
        public float GetItemStatSocketCostMultiplier(uint index)
        {
            Cypher.Assert(index < ItemConst.MaxStats);
            return ExtendedData.StatPercentageOfSocket[index];
        }
        public uint GetScalingStatDistribution() { return ExtendedData.ScalingStatDistributionID; }
        public uint GetDamageType() { return ExtendedData.DamageType; }
        public uint GetDelay() { return ExtendedData.ItemDelay; }
        public float GetRangedModRange() { return ExtendedData.ItemRange; }
        public ItemBondingType GetBonding() { return (ItemBondingType)ExtendedData.Bonding; }
        public uint GetPageText() { return ExtendedData.PageID; }
        public uint GetStartQuest() { return ExtendedData.StartQuestID; }
        public uint GetLockID() { return ExtendedData.LockID; }
        public uint GetRandomProperty() { return ExtendedData.RandomSelect; }
        public uint GetRandomSuffix() { return ExtendedData.ItemRandomSuffixGroupID; }
        public uint GetItemSet() { return ExtendedData.ItemSet; }
        public uint GetArea() { return ExtendedData.ZoneBound; }
        public uint GetMap() { return ExtendedData.InstanceBound; }
        public BagFamilyMask GetBagFamily() { return (BagFamilyMask)ExtendedData.BagFamily; }
        public uint GetTotemCategory() { return ExtendedData.TotemCategoryID; }
        public SocketColor GetSocketColor(uint index)
        {
            Cypher.Assert(index < ItemConst.MaxGemSockets);
            return (SocketColor)ExtendedData.SocketType[index];
        }
        public uint GetSocketBonus() { return ExtendedData.SocketMatchEnchantmentId; }
        public uint GetGemProperties() { return ExtendedData.GemProperties; }
        public float GetQualityModifier() { return ExtendedData.QualityModifier; }
        public uint GetDuration() { return ExtendedData.DurationInInventory; }
        public uint GetItemLimitCategory() { return ExtendedData.LimitCategory; }
        public HolidayIds GetHolidayID() { return (HolidayIds)ExtendedData.RequiredHoliday; }
        public float GetDmgVariance() { return ExtendedData.DmgVariance; }
        public byte GetArtifactID() { return ExtendedData.ArtifactID; }
        public byte GetRequiredExpansion() { return ExtendedData.ExpansionID; }

        public bool IsCurrencyToken() { return (GetBagFamily() & BagFamilyMask.CurrencyTokens) != 0; }

        public uint GetMaxStackSize()
        {
            return (ExtendedData.Stackable == 2147483647 || ExtendedData.Stackable <= 0) ? (0x7FFFFFFF - 1) : ExtendedData.Stackable;
        }

        public bool IsPotion() { return GetClass() == ItemClass.Consumable && GetSubClass() == (uint)ItemSubClassConsumable.Potion; }
        public bool IsVellum() { return GetClass() == ItemClass.TradeGoods && GetSubClass() == (uint)ItemSubClassTradeGoods.Enchantment; }
        public bool IsConjuredConsumable() { return GetClass() == ItemClass.Consumable && GetFlags().HasAnyFlag(ItemFlags.Conjured); }
        public bool IsCraftingReagent() { return GetFlags2().HasAnyFlag(ItemFlags2.UsedInATradeskill); }

        public bool IsRangedWeapon()
        {
            return GetClass() == ItemClass.Weapon || GetSubClass() == (uint)ItemSubClassWeapon.Bow ||
                   GetSubClass() == (uint)ItemSubClassWeapon.Gun || GetSubClass() == (uint)ItemSubClassWeapon.Crossbow;
        }

        public uint MaxDurability;
        public List<ItemEffectRecord> Effects = new List<ItemEffectRecord>();

        // extra fields, not part of db2 files
        public uint ScriptId;
        public uint FoodType;
        public uint MinMoneyLoot;
        public uint MaxMoneyLoot;
        public ItemFlagsCustom FlagsCu;
        public float SpellPPMRate;
        public BitArray[] Specializations = new BitArray[3];
        public uint ItemSpecClassMask;

        protected ItemRecord BasicData;
        protected ItemSparseRecord ExtendedData;
    }
}
