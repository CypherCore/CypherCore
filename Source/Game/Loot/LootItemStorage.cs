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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Entities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Game.Loots
{
    public class LootItemStorage : Singleton<LootItemStorage>
    {
        LootItemStorage() { }

        public void LoadStorageFromDB()
        {
            uint oldMSTime = Time.GetMSTime();
            _lootItemStorage.Clear();
            uint count = 0;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEMCONTAINER_ITEMS);
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                do
                {
                    ulong key = result.Read<ulong>(0);
                    var itr = _lootItemStorage.LookupByKey(key);
                    if (!_lootItemStorage.ContainsKey(key))
                        _lootItemStorage[key] = new StoredLootContainer(key);

                    StoredLootContainer storedContainer = _lootItemStorage[key];

                    LootItem lootItem = new LootItem();
                    lootItem.itemid = result.Read<uint>(1);
                    lootItem.count = result.Read<byte>(2);
                    lootItem.follow_loot_rules = result.Read<bool>(3);
                    lootItem.freeforall = result.Read<bool>(4);
                    lootItem.is_blocked = result.Read<bool>(5);
                    lootItem.is_counted = result.Read<bool>(6);
                    lootItem.is_underthreshold = result.Read<bool>(7);
                    lootItem.needs_quest = result.Read<bool>(8);
                    lootItem.randomBonusListId = result.Read<uint>(9);
                    lootItem.context = (ItemContext)result.Read<byte>(10);
                    StringArray bonusLists = new StringArray(result.Read<string>(11), ' ');

                    foreach (string str in bonusLists)
                        lootItem.BonusListIDs.Add(uint.Parse(str));

                    storedContainer.AddLootItem(lootItem, null);

                    ++count;
                } while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} stored item loots in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
            }
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 stored item loots");

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEMCONTAINER_MONEY);
            result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                count = 0;
                do
                {
                    ulong key = result.Read<ulong>(0);
                    if (!_lootItemStorage.ContainsKey(key))  
                         _lootItemStorage.TryAdd(key, new StoredLootContainer(key));

                    StoredLootContainer storedContainer = _lootItemStorage[key];
                    storedContainer.AddMoney(result.Read<uint>(1), null);

                    ++count;
                } while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} stored item money in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
            }
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 stored item money");
        }

        public bool LoadStoredLoot(Item item, Player player)
        {
            Loot loot = item.loot;
            if (!_lootItemStorage.ContainsKey(loot.containerID.GetCounter()))
                return false;

            var container = _lootItemStorage[loot.containerID.GetCounter()];
            loot.gold = container.GetMoney();

            LootTemplate lt = LootStorage.Items.GetLootFor(item.GetEntry());
            if (lt != null)
            {
                foreach (var storedItemPair in container.GetLootItems())
                {
                    LootItem li = new LootItem();
                    li.itemid = storedItemPair.Key;
                    li.count = (byte)storedItemPair.Value.Count;
                    li.follow_loot_rules = storedItemPair.Value.FollowRules;
                    li.freeforall = storedItemPair.Value.FFA;
                    li.is_blocked = storedItemPair.Value.Blocked;
                    li.is_counted = storedItemPair.Value.Counted;
                    li.is_underthreshold = storedItemPair.Value.UnderThreshold;
                    li.needs_quest = storedItemPair.Value.NeedsQuest;
                    li.randomBonusListId = storedItemPair.Value.RandomBonusListId;
                    li.context = storedItemPair.Value.Context;
                    li.BonusListIDs = storedItemPair.Value.BonusListIDs;

                    // Copy the extra loot conditions from the item in the loot template
                    lt.CopyConditions(li);

                    // If container item is in a bag, add that player as an allowed looter
                    if (item.GetBagSlot() != 0)
                        li.AddAllowedLooter(player);

                    // Finally add the LootItem to the container
                    loot.items.Add(li);

                    // Increment unlooted count
                    ++loot.unlootedCount;
                }
            }

            // Mark the item if it has loot so it won't be generated again on open
            item.m_lootGenerated = true;
            return true;
        }

        public void RemoveStoredMoneyForContainer(ulong containerId)
        {
            if (!_lootItemStorage.ContainsKey(containerId))
                return;

            _lootItemStorage[containerId].RemoveMoney();
        }

        public void RemoveStoredLootForContainer(ulong containerId)
        {
            _lootItemStorage.TryRemove(containerId, out _);

            SQLTransaction trans = new SQLTransaction();
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
            stmt.AddValue(0, containerId);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
            stmt.AddValue(0, containerId);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public void RemoveStoredLootItemForContainer(ulong containerId, uint itemId, uint count)
        {
            if (!_lootItemStorage.ContainsKey(containerId))
                return;

            _lootItemStorage[containerId].RemoveItem(itemId, count);
        }

        public void AddNewStoredLoot(Loot loot, Player player)
        {
            // Saves the money and item loot associated with an openable item to the DB
            if (loot.IsLooted()) // no money and no loot
                return;
            
            if (_lootItemStorage.ContainsKey(loot.containerID.GetCounter()))
            {
                Log.outError(LogFilter.Misc, $"Trying to store item loot by player: {player.GetGUID()} for container id: {loot.containerID.GetCounter()} that is already in storage!");
                return;
            }

            StoredLootContainer container = new StoredLootContainer(loot.containerID.GetCounter());

            SQLTransaction trans = new SQLTransaction();
            if (loot.gold != 0)
                container.AddMoney(loot.gold, trans);

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
            stmt.AddValue(0, loot.containerID.GetCounter());
            trans.Append(stmt);

            foreach (LootItem li in loot.items)
            {
                // Conditions are not checked when loot is generated, it is checked when loot is sent to a player.
                // For items that are lootable, loot is saved to the DB immediately, that means that loot can be
                // saved to the DB that the player never should have gotten. This check prevents that, so that only
                // items that the player should get in loot are in the DB.
                // IE: Horde items are not saved to the DB for Ally players.
                if (!li.AllowedForPlayer(player))
                    continue;

                // Don't save currency tokens
                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(li.itemid);
                if (itemTemplate == null || itemTemplate.IsCurrencyToken())
                    continue;

                container.AddLootItem(li, trans);
            }

            DB.Characters.CommitTransaction(trans);

            _lootItemStorage.TryAdd(loot.containerID.GetCounter(), container);
        }

        ConcurrentDictionary<ulong, StoredLootContainer> _lootItemStorage = new ConcurrentDictionary<ulong, StoredLootContainer>();
    }

    class StoredLootContainer
    {
        public StoredLootContainer(ulong containerId)
        {
            _containerId = containerId;
        }

        public void AddLootItem(LootItem lootItem, SQLTransaction trans)
        {
            _lootItems.Add(lootItem.itemid, new StoredLootItem(lootItem));
            if (trans == null)
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_ITEMS);

            // container_id, item_id, item_count, follow_rules, ffa, blocked, counted, under_threshold, needs_quest, rnd_prop, rnd_suffix
            stmt.AddValue(0, _containerId);
            stmt.AddValue(1, lootItem.itemid);
            stmt.AddValue(2, lootItem.count);
            stmt.AddValue(3, lootItem.follow_loot_rules);
            stmt.AddValue(4, lootItem.freeforall);
            stmt.AddValue(5, lootItem.is_blocked);
            stmt.AddValue(6, lootItem.is_counted);
            stmt.AddValue(7, lootItem.is_underthreshold);
            stmt.AddValue(8, lootItem.needs_quest);
            stmt.AddValue(9, lootItem.randomBonusListId);
            stmt.AddValue(10, (uint)lootItem.context);

            foreach (uint token in lootItem.BonusListIDs)
            {
                StringBuilder bonusListIDs = new StringBuilder();
                foreach (int bonusListID in lootItem.BonusListIDs)
                    bonusListIDs.Append(bonusListID + ' ');

                stmt.AddValue(11, bonusListIDs.ToString());
                trans.Append(stmt);
            }
        }

        public void AddMoney(uint money, SQLTransaction trans)
        {
            _money = money;
            if (trans == null)
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
            stmt.AddValue(0, _containerId);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_MONEY);
            stmt.AddValue(0, _containerId);
            stmt.AddValue(1, _money);
            trans.Append(stmt);
        }

        public void RemoveMoney()
        {
            _money = 0;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
            stmt.AddValue(0, _containerId);
            DB.Characters.Execute(stmt);
        }

        public void RemoveItem(uint itemId, uint count)
        {
            var bounds = _lootItems.LookupByKey(itemId);
            foreach (var itr in bounds)
            {
                if (itr.Count == count)
                {
                    _lootItems.Remove(itr.ItemId);
                    break;
                }
            }

            // Deletes a single item associated with an openable item from the DB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEM);
            stmt.AddValue(0, _containerId);
            stmt.AddValue(1, itemId);
            stmt.AddValue(2, count);
            DB.Characters.Execute(stmt);
        }

        ulong GetContainer() { return _containerId; }

        public uint GetMoney() { return _money; }

        public MultiMap<uint, StoredLootItem> GetLootItems() { return _lootItems; }

        MultiMap<uint, StoredLootItem> _lootItems = new MultiMap<uint, StoredLootItem>();
        ulong _containerId;
        uint _money;
    }

    class StoredLootItem
    {
        public StoredLootItem(LootItem lootItem)
        {
            ItemId = lootItem.itemid;
            Count = lootItem.count;
            FollowRules = lootItem.follow_loot_rules;
            FFA = lootItem.freeforall;
            Blocked = lootItem.is_blocked;
            Counted = lootItem.is_counted;
            UnderThreshold = lootItem.is_underthreshold;
            NeedsQuest = lootItem.needs_quest;
            RandomBonusListId = lootItem.randomBonusListId;
            Context = lootItem.context;
            BonusListIDs = lootItem.BonusListIDs;
        }

        public uint ItemId;
        public uint Count;
        public bool FollowRules;
        public bool FFA;
        public bool Blocked;
        public bool Counted;
        public bool UnderThreshold;
        public bool NeedsQuest;
        public uint RandomBonusListId;
        public ItemContext Context;
        public List<uint> BonusListIDs = new List<uint>();
    }
}
