// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Entities;

namespace Game.Loots
{
    public class LootItemStorage : Singleton<LootItemStorage>
    {
        private readonly ConcurrentDictionary<ulong, StoredLootContainer> _lootItemStorage = new();

        private LootItemStorage()
        {
        }

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

                    if (!_lootItemStorage.ContainsKey(key))
                        _lootItemStorage[key] = new StoredLootContainer(key);

                    StoredLootContainer storedContainer = _lootItemStorage[key];

                    LootItem lootItem = new();
                    lootItem.Itemid = result.Read<uint>(1);
                    lootItem.Count = result.Read<byte>(2);
                    lootItem.LootListId = result.Read<uint>(3);
                    lootItem.Follow_loot_rules = result.Read<bool>(4);
                    lootItem.Freeforall = result.Read<bool>(5);
                    lootItem.Is_blocked = result.Read<bool>(6);
                    lootItem.Is_counted = result.Read<bool>(7);
                    lootItem.Is_underthreshold = result.Read<bool>(8);
                    lootItem.Needs_quest = result.Read<bool>(9);
                    lootItem.RandomBonusListId = result.Read<uint>(10);
                    lootItem.Context = (ItemContext)result.Read<byte>(11);
                    StringArray bonusLists = new(result.Read<string>(12), ' ');

                    if (bonusLists != null &&
                        !bonusLists.IsEmpty())
                        foreach (string str in bonusLists)
                            lootItem.BonusListIDs.Add(uint.Parse(str));

                    storedContainer.AddLootItem(lootItem, null);

                    ++count;
                } while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} stored Item loots in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 stored Item loots");
            }

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

                Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} stored Item money in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 stored Item money");
            }
        }

        public bool LoadStoredLoot(Item item, Player player)
        {
            if (!_lootItemStorage.ContainsKey(item.GetGUID().GetCounter()))
                return false;

            var container = _lootItemStorage[item.GetGUID().GetCounter()];

            Loot loot = new(player.GetMap(), item.GetGUID(), LootType.Item, null);
            loot.Gold = container.GetMoney();

            LootTemplate lt = LootStorage.Items.GetLootFor(item.GetEntry());

            if (lt != null)
                foreach (var (id, storedItem) in container.GetLootItems())
                {
                    LootItem li = new();
                    li.Itemid = id;
                    li.Count = (byte)storedItem.Count;
                    li.LootListId = storedItem.ItemIndex;
                    li.Follow_loot_rules = storedItem.FollowRules;
                    li.Freeforall = storedItem.FFA;
                    li.Is_blocked = storedItem.Blocked;
                    li.Is_counted = storedItem.Counted;
                    li.Is_underthreshold = storedItem.UnderThreshold;
                    li.Needs_quest = storedItem.NeedsQuest;
                    li.RandomBonusListId = storedItem.RandomBonusListId;
                    li.Context = storedItem.Context;
                    li.BonusListIDs = storedItem.BonusListIDs;

                    // Copy the extra loot conditions from the Item in the loot template
                    lt.CopyConditions(li);

                    // If container Item is in a bag, add that player as an allowed looter
                    if (item.GetBagSlot() != 0)
                        li.AddAllowedLooter(player);

                    // Finally add the LootItem to the container
                    loot.Items.Add(li);

                    // Increment unlooted Count
                    ++loot.UnlootedCount;
                }

            // Mark the Item if it has loot so it won't be generated again on open
            item.loot = loot;
            item._lootGenerated = true;

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

            SQLTransaction trans = new();
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
            stmt.AddValue(0, containerId);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
            stmt.AddValue(0, containerId);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public void RemoveStoredLootItemForContainer(ulong containerId, uint itemId, uint count, uint itemIndex)
        {
            if (!_lootItemStorage.ContainsKey(containerId))
                return;

            _lootItemStorage[containerId].RemoveItem(itemId, count, itemIndex);
        }

        public void AddNewStoredLoot(ulong containerId, Loot loot, Player player)
        {
            // Saves the money and Item loot associated with an openable Item to the DB
            if (loot.IsLooted()) // no money and no loot
                return;

            if (_lootItemStorage.ContainsKey(containerId))
            {
                Log.outError(LogFilter.Misc, $"Trying to store Item loot by player: {player.GetGUID()} for container Id: {containerId} that is already in storage!");

                return;
            }

            StoredLootContainer container = new(containerId);

            SQLTransaction trans = new();

            if (loot.Gold != 0)
                container.AddMoney(loot.Gold, trans);

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
            stmt.AddValue(0, containerId);
            trans.Append(stmt);

            foreach (LootItem li in loot.Items)
            {
                // Conditions are not checked when loot is generated, it is checked when loot is sent to a player.
                // For items that are lootable, loot is saved to the DB immediately, that means that loot can be
                // saved to the DB that the player never should have gotten. This check prevents that, so that only
                // items that the player should get in loot are in the DB.
                // IE: Horde items are not saved to the DB for Ally players.
                if (!li.AllowedForPlayer(player, loot))
                    continue;

                // Don't save currency tokens
                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(li.Itemid);

                if (itemTemplate == null ||
                    itemTemplate.IsCurrencyToken())
                    continue;

                container.AddLootItem(li, trans);
            }

            DB.Characters.CommitTransaction(trans);

            _lootItemStorage.TryAdd(containerId, container);
        }
    }

    internal class StoredLootContainer
    {
        private readonly ulong _containerId;

        private readonly MultiMap<uint, StoredLootItem> _lootItems = new();
        private uint _money;

        public StoredLootContainer(ulong containerId)
        {
            _containerId = containerId;
        }

        public void AddLootItem(LootItem lootItem, SQLTransaction trans)
        {
            _lootItems.Add(lootItem.Itemid, new StoredLootItem(lootItem));

            if (trans == null)
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_ITEMS);

            // container_id, ItemId, item_count, follow_rules, ffa, Blocked, counted, under_threshold, needs_quest, rnd_prop, rnd_suffix
            stmt.AddValue(0, _containerId);
            stmt.AddValue(1, lootItem.Itemid);
            stmt.AddValue(2, lootItem.Count);
            stmt.AddValue(3, lootItem.LootListId);
            stmt.AddValue(4, lootItem.Follow_loot_rules);
            stmt.AddValue(5, lootItem.Freeforall);
            stmt.AddValue(6, lootItem.Is_blocked);
            stmt.AddValue(7, lootItem.Is_counted);
            stmt.AddValue(8, lootItem.Is_underthreshold);
            stmt.AddValue(9, lootItem.Needs_quest);
            stmt.AddValue(10, lootItem.RandomBonusListId);
            stmt.AddValue(11, (uint)lootItem.Context);

            StringBuilder bonusListIDs = new();

            foreach (int bonusListID in lootItem.BonusListIDs)
                bonusListIDs.Append(bonusListID + ' ');

            stmt.AddValue(12, bonusListIDs.ToString());
            trans.Append(stmt);
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

        public void RemoveItem(uint itemId, uint count, uint itemIndex)
        {
            var bounds = _lootItems.LookupByKey(itemId);

            foreach (var itr in bounds)
                if (itr.Count == count)
                {
                    _lootItems.Remove(itr.ItemId);

                    break;
                }

            // Deletes a single Item associated with an openable Item from the DB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEM);
            stmt.AddValue(0, _containerId);
            stmt.AddValue(1, itemId);
            stmt.AddValue(2, count);
            stmt.AddValue(3, itemIndex);
            DB.Characters.Execute(stmt);
        }

        public uint GetMoney()
        {
            return _money;
        }

        public MultiMap<uint, StoredLootItem> GetLootItems()
        {
            return _lootItems;
        }

        private ulong GetContainer()
        {
            return _containerId;
        }
    }

    internal class StoredLootItem
    {
        public bool Blocked;
        public List<uint> BonusListIDs = new();
        public ItemContext Context;
        public uint Count;
        public bool Counted;
        public bool FFA;
        public bool FollowRules;

        public uint ItemId;
        public uint ItemIndex;
        public bool NeedsQuest;
        public uint RandomBonusListId;
        public bool UnderThreshold;

        public StoredLootItem(LootItem lootItem)
        {
            ItemId = lootItem.Itemid;
            Count = lootItem.Count;
            ItemIndex = lootItem.LootListId;
            FollowRules = lootItem.Follow_loot_rules;
            FFA = lootItem.Freeforall;
            Blocked = lootItem.Is_blocked;
            Counted = lootItem.Is_counted;
            UnderThreshold = lootItem.Is_underthreshold;
            NeedsQuest = lootItem.Needs_quest;
            RandomBonusListId = lootItem.RandomBonusListId;
            Context = lootItem.Context;
            BonusListIDs = lootItem.BonusListIDs;
        }
    }
}