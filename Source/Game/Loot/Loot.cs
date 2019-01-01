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
using Framework.Dynamic;
using Game.Conditions;
using Game.Entities;
using Game.Groups;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game.Loots
{
    public class LootItem
    {
        public LootItem(LootStoreItem li)
        {
            itemid = li.itemid;
            conditions = li.conditions;

            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemid);
            freeforall = proto != null && proto.GetFlags().HasAnyFlag(ItemFlags.MultiDrop);
            follow_loot_rules = proto != null && proto.FlagsCu.HasAnyFlag(ItemFlagsCustom.FollowLootRules);

            needs_quest = li.needs_quest;

            randomSuffix = ItemEnchantment.GenerateEnchSuffixFactor(itemid);
            randomPropertyId = ItemEnchantment.GenerateItemRandomPropertyId(itemid);
            upgradeId = Global.DB2Mgr.GetRulesetItemUpgrade(itemid);
            canSave = true;
        }

        public LootItem()
        {
            canSave = true;
        }

        public bool AllowedForPlayer(Player player)
        {
            // DB conditions check
            if (!Global.ConditionMgr.IsObjectMeetToConditions(player, conditions))
                return false;

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(itemid);
            if (pProto == null)
                return false;

            // not show loot for players without profession or those who already know the recipe
            if (pProto.GetFlags().HasAnyFlag(ItemFlags.HideUnusableRecipe) && (!player.HasSkill((SkillType)pProto.GetRequiredSkill()) || player.HasSpell((uint)pProto.Effects[1].SpellID)))
                return false;

            // not show loot for not own team
            if (pProto.GetFlags2().HasAnyFlag(ItemFlags2.FactionHorde) && player.GetTeam() != Team.Horde)
                return false;

            if (pProto.GetFlags2().HasAnyFlag(ItemFlags2.FactionAlliance) && player.GetTeam() != Team.Alliance)
                return false;

            // check quest requirements
            if (!pProto.FlagsCu.HasAnyFlag(ItemFlagsCustom.IgnoreQuestStatus)
                && ((needs_quest || (pProto.GetStartQuest() != 0 && player.GetQuestStatus(pProto.GetStartQuest()) != QuestStatus.None)) && !player.HasQuestForItem(itemid)))
                return false;

            return true;
        }

        public void AddAllowedLooter(Player player)
        {
            allowedGUIDs.Add(player.GetGUID());
        }

        public List<ObjectGuid> GetAllowedLooters() { return allowedGUIDs; }

        public uint itemid;
        public uint randomSuffix;
        public ItemRandomEnchantmentId randomPropertyId;
        public uint upgradeId;
        public List<uint> BonusListIDs = new List<uint>();
        public byte context;
        public List<Condition> conditions = new List<Condition>();                               // additional loot condition
        public List<ObjectGuid> allowedGUIDs = new List<ObjectGuid>();
        public ObjectGuid rollWinnerGUID;                                   // Stores the guid of person who won loot, if his bags are full only he can see the item in loot list!
        public byte count;
        public bool is_looted;
        public bool is_blocked;
        public bool freeforall;                          // free for all
        public bool is_underthreshold;
        public bool is_counted;
        public bool needs_quest;                          // quest drop
        public bool follow_loot_rules;
        public bool canSave;
    }

    public class NotNormalLootItem
    {
        public byte index;                                          // position in quest_items or items;
        public bool is_looted;

        public NotNormalLootItem()
        {
            index = 0;
            is_looted = false;
        }

        public NotNormalLootItem(byte _index, bool _islooted = false)
        {
            index = _index;
            is_looted = _islooted;
        }
    }

    public class LootValidatorRef : Reference<Loot, LootValidatorRef>
    {
        public override void targetObjectDestroyLink()
        {

        }

        public override void sourceObjectDestroyLink()
        {

        }
    }

    public class LootValidatorRefManager : RefManager<Loot, LootValidatorRef>
    {
        public new LootValidatorRef getFirst() { return (LootValidatorRef)base.getFirst(); }
        public new LootValidatorRef getLast() { return (LootValidatorRef)base.getLast(); }
    }

    public class Loot
    {
        public Loot(uint _gold = 0)
        {
            gold = _gold;
            unlootedCount = 0;
            loot_type = LootType.None;
            maxDuplicates = 1;
            containerID = ObjectGuid.Empty;
        }

        // Inserts the item into the loot (called by LootTemplate processors)
        public void AddItem(LootStoreItem item)
        {
            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.itemid);
            if (proto == null)
                return;

            uint count = RandomHelper.URand(item.mincount, item.maxcount);
            uint stacks = (uint)(count / proto.GetMaxStackSize() + (Convert.ToBoolean(count % proto.GetMaxStackSize()) ? 1 : 0));

            List<LootItem> lootItems = item.needs_quest ? quest_items : items;
            uint limit = (uint)(item.needs_quest ? SharedConst.MaxNRQuestItems : SharedConst.MaxNRLootItems);

            for (uint i = 0; i < stacks && lootItems.Count < limit; ++i)
            {
                LootItem generatedLoot = new LootItem(item);
                generatedLoot.context = _itemContext;
                generatedLoot.count = (byte)Math.Min(count, proto.GetMaxStackSize());
                if (_itemContext != 0)
                {
                    List<uint> bonusListIDs = Global.DB2Mgr.GetItemBonusTree(generatedLoot.itemid, _itemContext);
                    generatedLoot.BonusListIDs.AddRange(bonusListIDs);
                }
                lootItems.Add(generatedLoot);
                count -= proto.GetMaxStackSize();

                // non-conditional one-player only items are counted here,
                // free for all items are counted in FillFFALoot(),
                // non-ffa conditionals are counted in FillNonQuestNonFFAConditionalLoot()
                if (!item.needs_quest && item.conditions.Empty() && !proto.GetFlags().HasAnyFlag(ItemFlags.MultiDrop))
                    ++unlootedCount;
            }
        }

        public LootItem GetItemInSlot(uint lootSlot)
        {
            if (lootSlot < items.Count)
                return items[(int)lootSlot];

            lootSlot -= (uint)items.Count;
            if (lootSlot < quest_items.Count)
                return quest_items[(int)lootSlot];

            return null;
        }

        // Calls processor of corresponding LootTemplate (which handles everything including references)
        public bool FillLoot(uint lootId, LootStore store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default)
        {
            // Must be provided
            if (lootOwner == null)
                return false;

            LootTemplate tab = store.GetLootFor(lootId);
            if (tab == null)
            {
                if (!noEmptyError)
                    Log.outError(LogFilter.Sql, "Table '{0}' loot id #{1} used but it doesn't have records.", store.GetName(), lootId);
                return false;
            }

            _itemContext = lootOwner.GetMap().GetDifficultyLootItemContext();

            tab.Process(this, store.IsRatesAllowed(), (byte)lootMode);          // Processing is done there, callback via Loot.AddItem()

            // Setting access rights for group loot case
            Group group = lootOwner.GetGroup();
            if (!personal && group != null)
            {
                roundRobinPlayer = lootOwner.GetGUID();

                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                {
                    Player player = refe.GetSource();
                    if (player)   // should actually be looted object instead of lootOwner but looter has to be really close so doesnt really matter
                        FillNotNormalLootFor(player, player.IsAtGroupRewardDistance(lootOwner));
                }

                for (byte i = 0; i < items.Count; ++i)
                {
                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(items[i].itemid);
                    if (proto != null)
                        if (proto.GetQuality() < group.GetLootThreshold())
                            items[i].is_underthreshold = true;
                }
            }
            // ... for personal loot
            else
                FillNotNormalLootFor(lootOwner, true);

            return true;
        }

        void FillNotNormalLootFor(Player player, bool presentAtLooting)
        {
            ObjectGuid plguid = player.GetGUID();

            var questItemList = PlayerQuestItems.LookupByKey(plguid);
            if (questItemList.Empty())
                FillQuestLoot(player);

            questItemList = PlayerFFAItems.LookupByKey(plguid);
            if (questItemList.Empty())
                FillFFALoot(player);

            questItemList = PlayerNonQuestNonFFAConditionalItems.LookupByKey(plguid);
            if (questItemList.Empty())
                FillNonQuestNonFFAConditionalLoot(player, presentAtLooting);

            // if not auto-processed player will have to come and pick it up manually
            if (!presentAtLooting)
                return;

            // Process currency items
            uint max_slot = GetMaxSlotInLootFor(player);
            LootItem item = null;
            int itemsSize = items.Count;
            for (byte i = 0; i < max_slot; ++i)
            {
                if (i < items.Count)
                    item = items[i];
                else
                    item = quest_items[i - itemsSize];

                if (!item.is_looted && item.freeforall && item.AllowedForPlayer(player))
                {
                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.itemid);
                    if (proto != null)
                        if (proto.IsCurrencyToken())
                            player.StoreLootItem(i, this);
                }
            }
        }

        List<NotNormalLootItem> FillFFALoot(Player player)
        {
            List<NotNormalLootItem> ql = new List<NotNormalLootItem>();

            for (byte i = 0; i < items.Count; ++i)
            {
                LootItem item = items[i];
                if (!item.is_looted && item.freeforall && item.AllowedForPlayer(player))
                {
                    ql.Add(new NotNormalLootItem(i));
                    ++unlootedCount;
                }
            }
            if (ql.Empty())
                return null;


            PlayerFFAItems[player.GetGUID()] = ql;
            return ql;
        }

        List<NotNormalLootItem> FillQuestLoot(Player player)
        {
            if (items.Count == SharedConst.MaxNRLootItems)
                return null;

            List<NotNormalLootItem> ql = new List<NotNormalLootItem>();

            for (byte i = 0; i < quest_items.Count; ++i)
            {
                LootItem item = quest_items[i];

                if (!item.is_looted && (item.AllowedForPlayer(player) || (item.follow_loot_rules && player.GetGroup() && ((player.GetGroup().GetLootMethod() == LootMethod.MasterLoot
                    && player.GetGroup().GetMasterLooterGuid() == player.GetGUID()) || player.GetGroup().GetLootMethod() != LootMethod.MasterLoot))))
                {
                    ql.Add(new NotNormalLootItem(i));

                    // quest items get blocked when they first appear in a
                    // player's quest vector
                    //
                    // increase once if one looter only, looter-times if free for all
                    if (item.freeforall || !item.is_blocked)
                        ++unlootedCount;
                    if (!player.GetGroup() || player.GetGroup().GetLootMethod() != LootMethod.GroupLoot)
                        item.is_blocked = true;

                    if (items.Count + ql.Count == SharedConst.MaxNRLootItems)
                        break;
                }
            }
            if (ql.Empty())
                return null;

            PlayerQuestItems[player.GetGUID()] = ql;
            return ql;
        }

        List<NotNormalLootItem> FillNonQuestNonFFAConditionalLoot(Player player, bool presentAtLooting)
        {
            List<NotNormalLootItem> ql = new List<NotNormalLootItem>();

            for (byte i = 0; i < items.Count; ++i)
            {
                LootItem item = items[i];
                if (!item.is_looted && !item.freeforall && item.AllowedForPlayer(player))
                {
                    if (presentAtLooting)
                        item.AddAllowedLooter(player);
                    if (!item.conditions.Empty())
                    {
                        ql.Add(new NotNormalLootItem(i));
                        if (!item.is_counted)
                        {
                            ++unlootedCount;
                            item.is_counted = true;
                        }
                    }
                }
            }
            if (ql.Empty())
                return null;

            PlayerNonQuestNonFFAConditionalItems[player.GetGUID()] = ql;
            return ql;
        }

        public void NotifyItemRemoved(byte lootIndex)
        {
            // notify all players that are looting this that the item was removed
            // convert the index to the slot the player sees
            for (int i = 0; i < PlayersLooting.Count; ++i)
            {
                Player player = Global.ObjAccessor.FindPlayer(PlayersLooting[i]);
                if (player != null)
                    player.SendNotifyLootItemRemoved(GetGUID(), lootIndex);
                else
                    PlayersLooting.RemoveAt(i);
            }
        }

        public void NotifyMoneyRemoved()
        {
            // notify all players that are looting this that the money was removed
            for (var i = 0; i < PlayersLooting.Count; ++i)
            {
                Player player = Global.ObjAccessor.FindPlayer(PlayersLooting[i]);
                if (player != null)
                    player.SendNotifyLootMoneyRemoved(GetGUID());
                else
                    PlayersLooting.RemoveAt(i);
            }
        }

        public void NotifyQuestItemRemoved(byte questIndex)
        {
            // when a free for all questitem is looted
            // all players will get notified of it being removed
            // (other questitems can be looted by each group member)
            // bit inefficient but isn't called often
            for (var i = 0; i < PlayersLooting.Count; ++i)
            {
                Player player = Global.ObjAccessor.FindPlayer(PlayersLooting[i]);
                if (player)
                {
                    var pql = PlayerQuestItems.LookupByKey(player.GetGUID());
                    if (!pql.Empty())
                    {
                        byte j;
                        for (j = 0; j < pql.Count; ++j)
                            if (pql[j].index == questIndex)
                                break;

                        if (j < pql.Count)
                            player.SendNotifyLootItemRemoved(GetGUID(), (byte)(items.Count + j));
                    }
                }
                else
                    PlayersLooting.RemoveAt(i);
            }
        }

        public void generateMoneyLoot(uint minAmount, uint maxAmount)
        {
            if (maxAmount > 0)
            {
                if (maxAmount <= minAmount)
                    gold = (uint)(maxAmount * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
                else if ((maxAmount - minAmount) < 32700)
                    gold = (uint)(RandomHelper.URand(minAmount, maxAmount) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
                else
                    gold = (uint)(RandomHelper.URand(minAmount >> 8, maxAmount >> 8) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney)) << 8;
            }
        }

        public void DeleteLootItemFromContainerItemDB(uint itemID)
        {
            // Deletes a single item associated with an openable item from the DB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEM);
            stmt.AddValue(0, containerID.GetCounter());
            stmt.AddValue(1, itemID);
            DB.Characters.Execute(stmt);

            // Mark the item looted to prevent resaving
            foreach (var lootItem in items)
            {
                if (lootItem.itemid != itemID)
                    continue;

                lootItem.canSave = false;
                break;
            }
        }

        public void DeleteLootMoneyFromContainerItemDB()
        {
            // Deletes money loot associated with an openable item from the DB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
            stmt.AddValue(0, containerID.GetCounter());
            DB.Characters.Execute(stmt);
        }

        public LootItem LootItemInSlot(uint lootSlot, Player player)
        {
            NotNormalLootItem qitem, ffaitem, conditem;
            return LootItemInSlot(lootSlot, player, out qitem, out ffaitem, out conditem);
        }
        public LootItem LootItemInSlot(uint lootSlot, Player player, out NotNormalLootItem qitem, out NotNormalLootItem ffaitem, out NotNormalLootItem conditem)
        {
            qitem = null;
            ffaitem = null;
            conditem = null;

            LootItem item = null;
            bool is_looted = true;
            if (lootSlot >= items.Count)
            {
                uint questSlot = (uint)(lootSlot - items.Count);
                var questItems = PlayerQuestItems.LookupByKey(player.GetGUID());
                if (!questItems.Empty())
                {
                    NotNormalLootItem qitem2 = questItems.Find(p => p.index == questSlot);
                    if (qitem2 != null)
                    {
                        qitem = qitem2;
                        item = quest_items[qitem2.index];
                        is_looted = qitem2.is_looted;
                    }
                }
            }
            else
            {
                item = items[(int)lootSlot];
                is_looted = item.is_looted;
                if (item.freeforall)
                {
                    var questItemList = PlayerFFAItems.LookupByKey(player.GetGUID());
                    if (!questItemList.Empty())
                    {
                        foreach (var c in questItemList)
                        {
                            if (c.index == lootSlot)
                            {
                                NotNormalLootItem ffaitem2 = c;
                                ffaitem = ffaitem2;
                                is_looted = ffaitem2.is_looted;
                                break;
                            }
                        }
                    }
                }
                else if (!item.conditions.Empty())
                {
                    var questItemList = PlayerNonQuestNonFFAConditionalItems.LookupByKey(player.GetGUID());
                    if (!questItemList.Empty())
                    {
                        foreach (var iter in questItemList)
                        {
                            if (iter.index == lootSlot)
                            {
                                NotNormalLootItem conditem2 = iter;
                                conditem = conditem2;
                                is_looted = conditem2.is_looted;
                                break;
                            }
                        }
                    }
                }
            }

            if (is_looted)
                return null;

            return item;
        }

        public uint GetMaxSlotInLootFor(Player player)
        {
            var questItemList = PlayerQuestItems.LookupByKey(player.GetGUID());
            return (uint)(items.Count + questItemList.Count);
        }

        // return true if there is any item that is lootable for any player (not quest item, FFA or conditional)
        public bool hasItemForAll()
        {
            // Gold is always lootable
            if (gold != 0)
                return true;

            foreach (LootItem item in items)
                if (!item.is_looted && !item.freeforall && item.conditions.Empty())
                    return true;

            return false;
        }

        // return true if there is any FFA, quest or conditional item for the player.
        public bool hasItemFor(Player player)
        {
            var lootPlayerQuestItems = GetPlayerQuestItems();
            var q_list = lootPlayerQuestItems.LookupByKey(player.GetGUID());
            if (!q_list.Empty())
            {
                foreach (var qi in q_list)
                {
                    LootItem item = quest_items[qi.index];
                    if (!qi.is_looted && !item.is_looted)
                        return true;
                }
            }

            var lootPlayerFFAItems = GetPlayerFFAItems();
            var ffa_list = lootPlayerFFAItems.LookupByKey(player.GetGUID());
            if (!ffa_list.Empty())
            {
                foreach (var fi in ffa_list)
                {
                    LootItem item = items[fi.index];
                    if (!fi.is_looted && !item.is_looted)
                        return true;
                }
            }

            var lootPlayerNonQuestNonFFAConditionalItems = GetPlayerNonQuestNonFFAConditionalItems();
            var conditional_list = lootPlayerNonQuestNonFFAConditionalItems.LookupByKey(player.GetGUID());
            if (!conditional_list.Empty())
            {
                foreach (var ci in conditional_list)
                {
                    LootItem item = items[ci.index];
                    if (!ci.is_looted && !item.is_looted)
                        return true;
                }
            }

            return false;
        }

        // return true if there is any item over the group threshold (i.e. not underthreshold).
        public bool hasOverThresholdItem()
        {
            for (byte i = 0; i < items.Count; ++i)
            {
                if (!items[i].is_looted && !items[i].is_underthreshold && !items[i].freeforall)
                    return true;
            }

            return false;
        }

        public void BuildLootResponse(LootResponse packet, Player viewer, PermissionTypes permission)
        {
            if (permission == PermissionTypes.None)
                return;

            packet.Coins = gold;

            switch (permission)
            {
                case PermissionTypes.Group:
                case PermissionTypes.Master:
                case PermissionTypes.Restricted:
                    {
                        // if you are not the round-robin group looter, you can only see
                        // blocked rolled items and quest items, and !ffa items
                        for (byte i = 0; i < items.Count; ++i)
                        {
                            if (!items[i].is_looted && !items[i].freeforall && items[i].conditions.Empty() && items[i].AllowedForPlayer(viewer))
                            {
                                LootSlotType slot_type;

                                if (items[i].is_blocked) // for ML & restricted is_blocked = !is_underthreshold
                                {
                                    switch (permission)
                                    {
                                        case PermissionTypes.Group:
                                            slot_type = LootSlotType.RollOngoing;
                                            break;
                                        case PermissionTypes.Master:
                                            {
                                                if (viewer.GetGroup() && viewer.GetGroup().GetMasterLooterGuid() == viewer.GetGUID())
                                                    slot_type = LootSlotType.Master;
                                                else
                                                    slot_type = LootSlotType.Locked;
                                                break;
                                            }
                                        case PermissionTypes.Restricted:
                                            slot_type = LootSlotType.Locked;
                                            break;
                                        default:
                                            continue;
                                    }
                                }
                                else if (roundRobinPlayer.IsEmpty() || viewer.GetGUID() == roundRobinPlayer || !items[i].is_underthreshold)
                                {
                                    // no round robin owner or he has released the loot
                                    // or it IS the round robin group owner
                                    // => item is lootable
                                    slot_type = LootSlotType.AllowLoot;
                                }
                                else if (!items[i].rollWinnerGUID.IsEmpty())
                                {
                                    if (items[i].rollWinnerGUID == viewer.GetGUID())
                                        slot_type = LootSlotType.Owner;
                                    else
                                        continue;
                                }
                                else
                                    // item shall not be displayed.
                                    continue;

                                LootItemData lootItem = new LootItemData();
                                lootItem.LootListID = (byte)(i + 1);
                                lootItem.UIType = slot_type;
                                lootItem.Quantity = items[i].count;
                                lootItem.Loot = new ItemInstance(items[i]);
                                packet.Items.Add(lootItem);
                            }
                        }
                        break;
                    }
                case PermissionTypes.All:
                case PermissionTypes.Owner:
                    {
                        for (byte i = 0; i < items.Count; ++i)
                        {
                            if (!items[i].is_looted && !items[i].freeforall && items[i].conditions.Empty() && items[i].AllowedForPlayer(viewer))
                            {
                                LootItemData lootItem = new LootItemData();
                                lootItem.LootListID = (byte)(i + 1);
                                lootItem.UIType = (permission == PermissionTypes.Owner ? LootSlotType.Owner : LootSlotType.AllowLoot);
                                lootItem.Quantity = items[i].count;
                                lootItem.Loot = new ItemInstance(items[i]);
                                packet.Items.Add(lootItem);
                            }
                        }
                        break;
                    }
                default:
                    return;
            }

            LootSlotType slotType = permission == PermissionTypes.Owner ? LootSlotType.Owner : LootSlotType.AllowLoot;
            var lootPlayerQuestItems = GetPlayerQuestItems();
            var q_list = lootPlayerQuestItems.LookupByKey(viewer.GetGUID());
            if (!q_list.Empty())
            {
                for (var i = 0; i < q_list.Count; ++i)
                {
                    NotNormalLootItem qi = q_list[i];
                    LootItem item = quest_items[qi.index];
                    if (!qi.is_looted && !item.is_looted)
                    {
                        LootItemData lootItem = new LootItemData();
                        lootItem.LootListID = (byte)(items.Count + i + 1);
                        lootItem.Quantity = item.count;
                        lootItem.Loot = new ItemInstance(item);

                        switch (permission)
                        {
                            case PermissionTypes.Master:
                                lootItem.UIType = LootSlotType.Master;
                                break;
                            case PermissionTypes.Restricted:
                                lootItem.UIType = item.is_blocked ? LootSlotType.Locked : LootSlotType.AllowLoot;
                                break;
                            case PermissionTypes.Group:
                                if (!item.is_blocked)
                                    lootItem.UIType = LootSlotType.AllowLoot;
                                else
                                    lootItem.UIType = LootSlotType.RollOngoing;
                                break;
                            default:
                                lootItem.UIType = slotType;
                                break;
                        }

                        packet.Items.Add(lootItem);
                    }
                }
            }

            var lootPlayerFFAItems = GetPlayerFFAItems();
            var ffa_list = lootPlayerFFAItems.LookupByKey(viewer.GetGUID());
            if (!ffa_list.Empty())
            {
                foreach (var fi in ffa_list)
                {
                    LootItem item = items[fi.index];
                    if (!fi.is_looted && !item.is_looted)
                    {
                        LootItemData lootItem = new LootItemData();
                        lootItem.LootListID = (byte)(fi.index + 1);
                        lootItem.UIType = slotType;
                        lootItem.Quantity = item.count;
                        lootItem.Loot = new ItemInstance(item);
                        packet.Items.Add(lootItem);
                    }
                }
            }

            var lootPlayerNonQuestNonFFAConditionalItems = GetPlayerNonQuestNonFFAConditionalItems();
            var conditional_list = lootPlayerNonQuestNonFFAConditionalItems.LookupByKey(viewer.GetGUID());
            if (!conditional_list.Empty())
            {
                foreach (var ci in conditional_list)
                {
                    LootItem item = items[ci.index];
                    if (!ci.is_looted && !item.is_looted)
                    {
                        LootItemData lootItem = new LootItemData();
                        lootItem.LootListID = (byte)(ci.index + 1);
                        lootItem.Quantity = item.count;
                        lootItem.Loot = new ItemInstance(item);

                        if (item.follow_loot_rules)
                        {
                            switch (permission)
                            {
                                case PermissionTypes.Master:
                                    lootItem.UIType = LootSlotType.Master;
                                    break;
                                case PermissionTypes.Restricted:
                                    lootItem.UIType = item.is_blocked ? LootSlotType.Locked : LootSlotType.AllowLoot;
                                    break;
                                case PermissionTypes.Group:
                                    if (!item.is_blocked)
                                        lootItem.UIType = LootSlotType.AllowLoot;
                                    else
                                        lootItem.UIType = LootSlotType.RollOngoing;
                                    break;
                                default:
                                    lootItem.UIType = slotType;
                                    break;
                            }
                        }
                        else
                            lootItem.UIType = slotType;

                        packet.Items.Add(lootItem);
                    }
                }
            }
        }

        public void addLootValidatorRef(LootValidatorRef pLootValidatorRef)
        {
            i_LootValidatorRefManager.InsertFirst(pLootValidatorRef);
        }

        public void clear()
        {
            PlayerQuestItems.Clear();

            PlayerFFAItems.Clear();

            PlayerNonQuestNonFFAConditionalItems.Clear();

            PlayersLooting.Clear();
            items.Clear();
            quest_items.Clear();
            gold = 0;
            unlootedCount = 0;
            roundRobinPlayer = ObjectGuid.Empty;
            i_LootValidatorRefManager.clearReferences();
            _itemContext = 0;
        }

        public bool empty() { return items.Empty() && gold == 0; }
        public bool isLooted() { return gold == 0 && unlootedCount == 0; }

        public void AddLooter(ObjectGuid guid) { PlayersLooting.Add(guid); }
        public void RemoveLooter(ObjectGuid guid) { PlayersLooting.Remove(guid); }

        public ObjectGuid GetGUID() { return _GUID; }
        public void SetGUID(ObjectGuid guid) { _GUID = guid; }

        public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerQuestItems() { return PlayerQuestItems; }
        public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerFFAItems() { return PlayerFFAItems; }
        public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerNonQuestNonFFAConditionalItems() { return PlayerNonQuestNonFFAConditionalItems; }

        public List<LootItem> items = new List<LootItem>();
        public List<LootItem> quest_items = new List<LootItem>();
        public uint gold;
        public byte unlootedCount;
        public ObjectGuid roundRobinPlayer;                                // GUID of the player having the Round-Robin ownership for the loot. If 0, round robin owner has released.
        public LootType loot_type;                                     // required for achievement system
        public byte maxDuplicates;                                    // Max amount of items with the same entry that can drop (default is 1; on 25 man raid mode 3)

        public ObjectGuid containerID;

        List<ObjectGuid> PlayersLooting = new List<ObjectGuid>();
        MultiMap<ObjectGuid, NotNormalLootItem> PlayerQuestItems = new MultiMap<ObjectGuid, NotNormalLootItem>();
        MultiMap<ObjectGuid, NotNormalLootItem> PlayerFFAItems = new MultiMap<ObjectGuid, NotNormalLootItem>();
        MultiMap<ObjectGuid, NotNormalLootItem> PlayerNonQuestNonFFAConditionalItems = new MultiMap<ObjectGuid, NotNormalLootItem>();

        // All rolls are registered here. They need to know, when the loot is not valid anymore
        LootValidatorRefManager i_LootValidatorRefManager = new LootValidatorRefManager();

        // Loot GUID
        ObjectGuid _GUID;
        byte _itemContext;
    }

    public class AELootResult
    {
        public void Add(Item item, byte count, LootType lootType)
        {
            var id = _byItem.LookupByKey(item);
            if (id != 0)
            {
                var resultValue = _byOrder[id];
                resultValue.count += count;
            }
            else
            {
                _byItem[item] = _byOrder.Count;
                ResultValue value;
                value.item = item;
                value.count = count;
                value.lootType = lootType;
                _byOrder.Add(value);
            }
        }

        public List<ResultValue> GetByOrder()
        {
            return _byOrder;
        }

        List<ResultValue> _byOrder = new List<ResultValue>();
        Dictionary<Item, int> _byItem = new Dictionary<Item, int>();

        public struct ResultValue
        {
            public Item item;
            public byte count;
            public LootType lootType;
        }
    }
}
