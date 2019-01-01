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
using Framework.Database;
using Game.DataStorage;
using System.Collections.Generic;

namespace Game.Entities
{
    public class ItemEnchantment
    {
        static ItemEnchantment()
        {
            RandomItemEnch = new EnchantmentStore();
        }

        public static void LoadRandomEnchantmentsTable()
        {
            // for reload case
            RandomItemEnch[ItemRandomEnchantmentType.Property].Clear();
            RandomItemEnch[ItemRandomEnchantmentType.Suffix].Clear();

            //                                         0      1     2     3
            SQLResult result = DB.World.Query("SELECT entry, type, ench, chance FROM item_enchantment_template");

            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Player, "Loaded 0 Item Enchantment definitions. DB table `item_enchantment_template` is empty.");
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
                        if (!CliDB.ItemRandomPropertiesStorage.ContainsKey(ench))
                        {
                            Log.outError(LogFilter.Sql, "Property {0} used in `item_enchantment_template` by entry {1} doesn't have exist in ItemRandomProperties.db2", ench, entry);
                            continue;
                        }
                        break;
                    case ItemRandomEnchantmentType.Suffix:
                        if (!CliDB.ItemRandomSuffixStorage.ContainsKey(ench))
                        {
                            Log.outError(LogFilter.Sql, "Suffix {0} used in `item_enchantment_template` by entry {1} doesn't have exist in ItemRandomSuffix.db2", ench, entry);
                            continue;
                        }
                        break;
                    case ItemRandomEnchantmentType.BonusList:
                        if (Global.DB2Mgr.GetItemBonusList(ench) == null)
                        {
                            Log.outError(LogFilter.Sql, "Bonus list {0} used in `item_enchantment_template` by entry {1} doesn't have exist in ItemBonus.db2", ench, entry);
                            continue;
                        }
                        break;
                    default:
                        Log.outError(LogFilter.Sql, "Invalid random enchantment type specified in `item_enchantment_template` table for `entry` {0} `ench` {1}", entry, ench);
                        break;
                }

                if (chance < 0.000001f || chance > 100.0f)
                {
                    Log.outError(LogFilter.Sql, "Random item enchantment for entry {0} type {1} ench {2} has invalid chance {3}", entry, type, ench, chance);
                    continue;
                }

                switch (type)
                {
                    case ItemRandomEnchantmentType.Property:
                        RandomItemEnch[ItemRandomEnchantmentType.Property].Add(entry, new EnchStoreItem(type, ench, chance));
                        break;
                    case ItemRandomEnchantmentType.Suffix:
                    case ItemRandomEnchantmentType.BonusList: // random bonus lists use RandomSuffix field in Item-sparse.db2
                        RandomItemEnch[ItemRandomEnchantmentType.Suffix].Add(entry, new EnchStoreItem(type, ench, chance));
                        break;
                    default:
                        break;
                }

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.Player, "Loaded {0} Item Enchantment definitions", count);
        }

        public static ItemRandomEnchantmentId GetItemEnchantMod(int entry, ItemRandomEnchantmentType type)
        {
            if (entry == 0)
                return ItemRandomEnchantmentId.Empty;

            if (entry == -1)
                return ItemRandomEnchantmentId.Empty;

            var tab = RandomItemEnch[type].LookupByKey(entry);
            if (tab == null)
            {
                Log.outError(LogFilter.Player, "Item RandomProperty / RandomSuffix id #{0} used in `item_template` but it does not have records in `item_enchantment_template` table.", entry);
                return ItemRandomEnchantmentId.Empty;
            }

            var selectedItem = tab.SelectRandomElementByWeight(enchant => enchant.chance);

            return new ItemRandomEnchantmentId(selectedItem.type, selectedItem.ench);
        }

        public static ItemRandomEnchantmentId GenerateItemRandomPropertyId(uint item_id)
        {
            ItemTemplate itemProto = Global.ObjectMgr.GetItemTemplate(item_id);
            if (itemProto == null)
                return ItemRandomEnchantmentId.Empty;

            // item must have one from this field values not null if it can have random enchantments
            if (itemProto.GetRandomProperty() == 0 && itemProto.GetRandomSuffix() == 0)
                return ItemRandomEnchantmentId.Empty;

            // item can have not null only one from field values
            if (itemProto.GetRandomProperty() != 0 && itemProto.GetRandomSuffix() != 0)
            {
                Log.outError(LogFilter.Sql, "Item template {0} have RandomProperty == {1} and RandomSuffix == {2}, but must have one from field =0", itemProto.GetId(), itemProto.GetRandomProperty(), itemProto.GetRandomSuffix());
                return ItemRandomEnchantmentId.Empty;
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

            // Select rare/epic modifier
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

        static EnchantmentStore RandomItemEnch;

        public class EnchStoreItem
        {
            public EnchStoreItem()
            {
                ench = 0;
                chance = 0;
            }
            public EnchStoreItem(ItemRandomEnchantmentType _type, uint _ench, float _chance)
            {
                type = _type;
                ench = _ench;
                chance = _chance;
            }

            public ItemRandomEnchantmentType type;
            public uint ench;
            public float chance;
        }

        class EnchantmentStore
        {
            public EnchantmentStore()
            {
                _data[(byte)ItemRandomEnchantmentType.Property] = new MultiMap<uint, EnchStoreItem>();
                _data[(byte)ItemRandomEnchantmentType.Suffix] = new MultiMap<uint, EnchStoreItem>();
            }

            public MultiMap<uint, EnchStoreItem> this[ItemRandomEnchantmentType type]
            {
                get
                {
                    //(type != ItemRandomEnchantmentType.BonusList, "Random bonus lists do not have their own storage, use Suffix for them");
                    return _data[(byte)type];
                }
            }

            MultiMap<uint, EnchStoreItem>[] _data = new MultiMap<uint, EnchStoreItem>[2];
        }
    }

    public struct ItemRandomEnchantmentId
    {
        public static ItemRandomEnchantmentId Empty = default(ItemRandomEnchantmentId);

        public ItemRandomEnchantmentId(ItemRandomEnchantmentType type, uint id)
        {
            Type = type;
            Id = id;
        }

        public ItemRandomEnchantmentType Type;
        public uint Id;
    }

    public enum ItemRandomEnchantmentType
    {
        Property = 0,
        Suffix = 1,
        BonusList = 2
    }
}
