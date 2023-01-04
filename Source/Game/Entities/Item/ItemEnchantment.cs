/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Framework.Database;
using Game.DataStorage;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public class ItemEnchantmentManager
    {
        public static void LoadRandomEnchantmentsTable()
        {
            uint oldMSTime = Time.GetMSTime();

            RandomItemEnch[ItemRandomEnchantmentType.Property]?.Clear();
            RandomItemEnch[ItemRandomEnchantmentType.Suffix]?.Clear();

            //                                          0      1    2      3
            SQLResult result = DB.World.Query("SELECT entry, Type, Id, Chance FROM item_enchantment_template");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.Player, "Loaded 0 Item Enchantment definitions. DB table `item_enchantment_template` is empty.");
                return;
            }
            uint count = 0;

            do
            {
                uint entry = result.Read<uint>(0);
                ItemRandomEnchantmentType type = (ItemRandomEnchantmentType)result.Read<byte>(1);
                uint ench = result.Read<uint>(2);
                float chance = result.Read<float>(3);

                switch (type)
                {
                    case ItemRandomEnchantmentType.Property:
                        if (CliDB.ItemRandomPropertiesStorage.LookupByKey(ench) == null)
                        {
                            Log.outError(LogFilter.Sql, $"Property {ench} used in `item_enchantment_template` by entry {entry} doesn't have exist in ItemRandomProperties.db2");
                            continue;
                        }
                        break;
                    case ItemRandomEnchantmentType.Suffix:
                        if (CliDB.ItemRandomSuffixStorage.LookupByKey(ench) == null)
                        {
                            Log.outError(LogFilter.Sql, $"Suffix {ench} used in `item_enchantment_template` by entry {entry} doesn't have exist in ItemRandomSuffix.db2");
                            continue;
                        }
                        break;
                    case ItemRandomEnchantmentType.BonusList:
                        if (Global.DB2Mgr.GetItemBonusList(ench) == null)
                        {
                            Log.outError(LogFilter.Sql, $"Bonus list {ench} used in `item_enchantment_template` by entry {entry} doesn't have exist in ItemBonus.db2");
                            continue;
                        }
                        break;
                    default:
                        Log.outError(LogFilter.Sql, $"Invalid random enchantment Type specified in `item_enchantment_template` table for `entry` {entry} `Id` {ench}");
                        break;
                }

                if (chance < 0.000001f || chance > 100.0f)
                {
                    Log.outError(LogFilter.Sql, $"Random item enchantment for entry {entry} Type {type} Id {ench} has invalid Chance {chance}");
                    continue;
                }

                switch (type)
                {
                    case ItemRandomEnchantmentType.Property:
                        RandomItemEnch[ItemRandomEnchantmentType.Property][entry].Add(new EnchStoreItem(type, ench, chance));
                        break;
                    case ItemRandomEnchantmentType.Suffix:
                    case ItemRandomEnchantmentType.BonusList: // random bonus lists use RandomSuffix field in Item-sparse.db2
                        RandomItemEnch[ItemRandomEnchantmentType.Suffix][entry].Add(new EnchStoreItem(type, ench, chance));
                        break;
                    default:
                        break;
                }

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} Item Enchantment definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public static ItemRandomEnchantmentId GetItemEnchantMod(int entry, ItemRandomEnchantmentType type)
        {
            if (entry <= 0) 
                return new ItemRandomEnchantmentId();

            List<EnchStoreItem> tab = RandomItemEnch[type].LookupByKey(entry);
            if (tab == null)
            {
                Log.outError(LogFilter.Sql, $"Item RandomProperty / RandomSuffix id #{entry} used in ItemSparse.db2 but it does not have records in `item_enchantment_template` table.");
                return new ItemRandomEnchantmentId();
            }

            var selectedItr = tab.SelectRandomElementByWeight(x => x.Chance);


            return selectedItr.itemRandomEnchantmentId;
        }

        public static ItemRandomEnchantmentId GenerateItemRandomPropertyId(uint item_id)
        {
            ItemTemplate itemProto = Global.ObjectMgr.GetItemTemplate(item_id);
            if (itemProto == null)
                return new ItemRandomEnchantmentId();

            // item can have not null only one from field values
            if (itemProto.GetRandomProperty() != 0 && itemProto.GetRandomSuffix() != 0)
            {
                Log.outError(LogFilter.Sql, $"Item template {itemProto.GetId()} have RandomProperty == {itemProto.GetRandomProperty()} and RandomSuffix == {itemProto.GetRandomSuffix()}, but must have one from field =0");
                return new ItemRandomEnchantmentId();
            }

            // RandomProperty case
            if (itemProto.GetRandomProperty() != 0)
                return GetItemEnchantMod((int)itemProto.GetRandomProperty(), ItemRandomEnchantmentType.Property);
            // RandomSuffix case
            else
                return GetItemEnchantMod((int)itemProto.GetRandomSuffix(), ItemRandomEnchantmentType.Suffix);
        }

        public static uint GenerateEnchSuffixFactor(uint item_id)
        {
            ItemTemplate itemProto = Global.ObjectMgr.GetItemTemplate(item_id);

            if (itemProto == null)
                return 0;

            if (itemProto.GetRandomSuffix() == 0)
                return 0;

            return GetRandomPropertyPoints(itemProto.GetBaseItemLevel(), itemProto.GetQuality(), itemProto.GetInventoryType(), itemProto.GetSubClass());
        }

        public static uint GetRandomPropertyPoints(uint itemLevel, ItemQuality quality, InventoryType inventoryType, uint subClass)
        {
            uint propIndex;

            switch (inventoryType)
            {
                case InventoryType.Head:
                case InventoryType.Body:
                case InventoryType.Chest:
                case InventoryType.Legs:
                case InventoryType.Ranged:
                case InventoryType.Weapon2Hand:
                case InventoryType.Robe:
                case InventoryType.Thrown:
                    propIndex = 0;
                    break;
                case InventoryType.RangedRight:
                    if ((ItemSubClassWeapon)subClass == ItemSubClassWeapon.Wand)
                        propIndex = 3;
                    else
                        propIndex = 0;
                    break;
                case InventoryType.Weapon:
                case InventoryType.WeaponMainhand:
                case InventoryType.WeaponOffhand:
                    propIndex = 3;
                    break;
                case InventoryType.Shoulders:
                case InventoryType.Waist:
                case InventoryType.Feet:
                case InventoryType.Hands:
                case InventoryType.Trinket:
                    propIndex = 1;
                    break;
                case InventoryType.Neck:
                case InventoryType.Wrists:
                case InventoryType.Finger:
                case InventoryType.Shield:
                case InventoryType.Cloak:
                case InventoryType.Holdable:
                    propIndex = 2;
                    break;
                case InventoryType.Relic:
                    propIndex = 4;
                    break;
                default:
                    return 0;
            }

            RandPropPointsRecord randPropPointsEntry = CliDB.RandPropPointsStorage.LookupByKey(itemLevel);
            if (randPropPointsEntry == null)
                return 0;

            switch (quality)
            {
                case ItemQuality.Uncommon:
                    return randPropPointsEntry.Good[propIndex];
                case ItemQuality.Rare:
                case ItemQuality.Heirloom:
                    return randPropPointsEntry.Superior[propIndex];
                case ItemQuality.Epic:
                case ItemQuality.Legendary:
                case ItemQuality.Artifact:
                    return randPropPointsEntry.Epic[propIndex];
            }

            return 0;
        }

        static EnchantmentStore RandomItemEnch = new();
    }

    public struct EnchStoreItem
    {
        public EnchStoreItem() { }
        public EnchStoreItem(ItemRandomEnchantmentId itemRandomEnchantmentId, float chance)
        {
            this.itemRandomEnchantmentId = itemRandomEnchantmentId;
            this.Chance = chance;
        }
        public EnchStoreItem(ItemRandomEnchantmentType type, uint id, float chance)
        {
            this.itemRandomEnchantmentId.Type = type;
            this.itemRandomEnchantmentId.Id = id;
            this.Chance = chance;
        }
        public ItemRandomEnchantmentId itemRandomEnchantmentId;
        public float Chance;
    }

    public struct ItemRandomEnchantmentId
    {
        public ItemRandomEnchantmentId() { }
        public ItemRandomEnchantmentId(ItemRandomEnchantmentType type, uint id)
        {
            this.Type = type;
            this.Id = id;
        }

        public ItemRandomEnchantmentType Type;
        public uint Id;
    };

    class EnchantmentStore
    {
        private Dictionary<ItemRandomEnchantmentType, Dictionary<uint,List<EnchStoreItem>>> _data;
        private ItemRandomEnchantmentType Check(ItemRandomEnchantmentType type)
        {
            // random bonus lists use RandomSuffix field in Item-sparse.db2
            //ASSERT(Type != ItemRandomEnchantmentType.BonusList, "Random bonus lists do not have their own storage, use Suffix for them");
            if (type == ItemRandomEnchantmentType.BonusList)
                return ItemRandomEnchantmentType.Suffix;
            return type;
        }

        public EnchantmentStore()
        {
            _data = new()
            {
                { ItemRandomEnchantmentType.Property, new Dictionary<uint,List<EnchStoreItem>>() },
                { ItemRandomEnchantmentType.Suffix, new Dictionary<uint,List<EnchStoreItem>>() },
                //{ ItemRandomEnchantmentType.BonusList, new Dictionary<uint,EnchStoreItem>() }
            };
        }

        public Dictionary<uint, List<EnchStoreItem>> this[ItemRandomEnchantmentType type]
        {
            get => _data[Check(type)];
            set => _data[Check(type)] = value;
        }
    }

    public enum ItemRandomEnchantmentType : byte
    {
        Property = 0,
        Suffix = 1,
        BonusList = 2
    };
}
