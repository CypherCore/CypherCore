// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Groups;
using Game.Guilds;
using Game.Loots;
using Game.Mails;
using Game.Maps;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Entities
{
    public partial class Player
    {
        //Refund
        void AddRefundReference(ObjectGuid it)
        {
            m_refundableItems.Add(it);
        }
        public void DeleteRefundReference(ObjectGuid it)
        {
            m_refundableItems.Remove(it);
        }
        public void RefundItem(Item item)
        {
            if (!item.IsRefundable())
            {
                Log.outDebug(LogFilter.Player, "Item refund: item not refundable!");
                return;
            }

            if (item.IsRefundExpired())    // item refund has expired
            {
                item.SetNotRefundable(this);
                SendItemRefundResult(item, null, 10);
                return;
            }

            if (GetGUID() != item.GetRefundRecipient()) // Formerly refundable item got traded
            {
                Log.outDebug(LogFilter.Player, "Item refund: item was traded!");
                item.SetNotRefundable(this);
                return;
            }

            ItemExtendedCostRecord iece = CliDB.ItemExtendedCostStorage.LookupByKey(item.GetPaidExtendedCost());
            if (iece == null)
            {
                Log.outDebug(LogFilter.Player, "Item refund: cannot find extendedcost data.");
                return;
            }

            bool store_error = false;
            for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
            {
                uint count = iece.ItemCount[i];
                uint itemid = iece.ItemID[i];

                if (count != 0 && itemid != 0)
                {
                    List<ItemPosCount> dest = new();
                    InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemid, count);
                    if (msg != InventoryResult.Ok)
                    {
                        store_error = true;
                        break;
                    }
                }
            }

            if (store_error)
            {
                SendItemRefundResult(item, iece, 10);
                return;
            }

            SendItemRefundResult(item, iece, 0);

            ulong moneyRefund = item.GetPaidMoney();  // item. will be invalidated in DestroyItem

            // Save all relevant data to DB to prevent desynchronisation exploits
            SQLTransaction trans = new();

            // Delete any references to the refund data
            item.SetNotRefundable(this, true, trans, false);
            GetSession().GetCollectionMgr().RemoveTemporaryAppearance(item);

            // Destroy item
            DestroyItem(item.GetBagSlot(), item.GetSlot(), true);

            // Grant back extendedcost items
            for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
            {
                uint count = iece.ItemCount[i];
                uint itemid = iece.ItemID[i];
                if (count != 0 && itemid != 0)
                {
                    List<ItemPosCount> dest = new();
                    InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemid, count);
                    Cypher.Assert(msg == InventoryResult.Ok); // Already checked before
                    Item it = StoreNewItem(dest, itemid, true);
                    SendNewItem(it, count, true, false, true);
                }
            }

            // Grant back currencies
            for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
            {
                if (iece.Flags.HasAnyFlag((byte)((int)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
                    continue;

                uint count = iece.CurrencyCount[i];
                uint currencyid = iece.CurrencyID[i];
                if (count != 0 && currencyid != 0)
                    AddCurrency(currencyid, count, CurrencyGainSource.ItemRefund);
            }

            // Grant back money
            if (moneyRefund != 0)
                ModifyMoney((long)moneyRefund); // Saved in SaveInventoryAndGoldToDB

            SaveInventoryAndGoldToDB(trans);

            DB.Characters.CommitTransaction(trans);
        }
        public void SendRefundInfo(Item item)
        {
            if (item.IsRefundable() && item.IsRefundExpired())
                item.SetNotRefundable(this);

            if (!item.IsRefundable())
            {
                Log.outDebug(LogFilter.Player, "Item refund: item not refundable!");
                return;
            }

            if (GetGUID() != item.GetRefundRecipient()) // Formerly refundable item got traded
            {
                Log.outDebug(LogFilter.Player, "Item refund: item was traded!");
                item.SetNotRefundable(this);
                return;
            }

            ItemExtendedCostRecord iece = CliDB.ItemExtendedCostStorage.LookupByKey(item.GetPaidExtendedCost());
            if (iece == null)
            {
                Log.outDebug(LogFilter.Player, "Item refund: cannot find extendedcost data.");
                return;
            }
            SetItemPurchaseData setItemPurchaseData = new();
            setItemPurchaseData.ItemGUID = item.GetGUID();
            setItemPurchaseData.PurchaseTime = item.m_itemData.CreatePlayedTime;
            setItemPurchaseData.Contents.Money = item.GetPaidMoney();

            for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)                             // item cost data
            {
                setItemPurchaseData.Contents.Items[i].ItemCount = iece.ItemCount[i];
                setItemPurchaseData.Contents.Items[i].ItemID = iece.ItemID[i];
            }

            for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)                       // currency cost data
            {
                if (iece.Flags.HasAnyFlag((byte)((int)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
                    continue;

                setItemPurchaseData.Contents.Currencies[i].CurrencyCount = iece.CurrencyCount[i];
                setItemPurchaseData.Contents.Currencies[i].CurrencyID = iece.CurrencyID[i];
            }

            SendPacket(setItemPurchaseData);
        }
        public void SendItemRefundResult(Item item, ItemExtendedCostRecord iece, byte error)
        {
            ItemPurchaseRefundResult itemPurchaseRefundResult = new();
            itemPurchaseRefundResult.ItemGUID = item.GetGUID();
            itemPurchaseRefundResult.Result = error;
            if (error == 0)
            {
                itemPurchaseRefundResult.Contents = new();
                itemPurchaseRefundResult.Contents.Money = item.GetPaidMoney();
                for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i) // item cost data
                {
                    itemPurchaseRefundResult.Contents.Items[i].ItemCount = iece.ItemCount[i];
                    itemPurchaseRefundResult.Contents.Items[i].ItemID = iece.ItemID[i];
                }

                for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i) // currency cost data
                {
                    if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
                        continue;

                    itemPurchaseRefundResult.Contents.Currencies[i].CurrencyCount = iece.CurrencyCount[i];
                    itemPurchaseRefundResult.Contents.Currencies[i].CurrencyID = iece.CurrencyID[i];
                }
            }

            SendPacket(itemPurchaseRefundResult);
        }

        //Trade 
        void AddTradeableItem(Item item)
        {
            m_itemSoulboundTradeable.Add(item.GetGUID());
        }
        public void RemoveTradeableItem(Item item)
        {
            m_itemSoulboundTradeable.Remove(item.GetGUID());
        }
        void UpdateSoulboundTradeItems()
        {
            // also checks for garbage data
            foreach (var guid in m_itemSoulboundTradeable.ToList())
            {
                Item item = GetItemByGuid(guid);
                if (item == null || item.GetOwnerGUID() != GetGUID() || item.CheckSoulboundTradeExpire())
                    m_itemSoulboundTradeable.Remove(guid);
            }
        }
        public void SetTradeData(TradeData data) { m_trade = data; }
        public Player GetTrader() { return m_trade?.GetTrader(); }
        public TradeData GetTradeData() { return m_trade; }
        public void TradeCancel(bool sendback)
        {
            if (m_trade != null)
            {
                Player trader = m_trade.GetTrader();

                // send yellow "Trade canceled" message to both traders
                if (sendback)
                    GetSession().SendCancelTrade();

                trader.GetSession().SendCancelTrade();

                // cleanup
                m_trade = null;
                trader.m_trade = null;
            }
        }

        //Durability
        public void DurabilityLossAll(double percent, bool inventory)
        {
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    DurabilityLoss(pItem, percent);
            }

            if (inventory)
            {
                int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
                for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
                {
                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem != null)
                        DurabilityLoss(pItem, percent);
                }

                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                {
                    Bag pBag = GetBagByPos(i);
                    if (pBag != null)
                    {
                        for (byte j = 0; j < pBag.GetBagSize(); j++)
                        {
                            Item pItem = GetItemByPos(i, j);
                            if (pItem != null)
                                DurabilityLoss(pItem, percent);
                        }
                    }
                }
            }
        }
        public void DurabilityLoss(Item item, double percent)
        {
            if (item == null)
                return;

            uint pMaxDurability = item.m_itemData.MaxDurability;

            if (pMaxDurability == 0)
                return;

            percent /= GetTotalAuraMultiplier(AuraType.ModDurabilityLoss);

            int pDurabilityLoss = (int)(pMaxDurability * percent);

            if (pDurabilityLoss < 1)
                pDurabilityLoss = 1;

            DurabilityPointsLoss(item, pDurabilityLoss);
        }
        public void DurabilityPointsLossAll(int points, bool inventory)
        {
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    DurabilityPointsLoss(pItem, points);
            }

            if (inventory)
            {
                int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
                for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
                {
                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem != null)
                        DurabilityPointsLoss(pItem, points);
                }

                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                {
                    Bag pBag = (Bag)GetItemByPos(InventorySlots.Bag0, i);
                    if (pBag != null)
                        for (byte j = 0; j < pBag.GetBagSize(); j++)
                        {
                            Item pItem = GetItemByPos(i, j);
                            if (pItem != null)
                                DurabilityPointsLoss(pItem, points);
                        }
                }
            }
        }
        public void DurabilityPointsLoss(Item item, int points)
        {
            if (HasAuraType(AuraType.PreventDurabilityLoss))
                return;

            uint pMaxDurability = item.m_itemData.MaxDurability;
            uint pOldDurability = item.m_itemData.Durability;
            int pNewDurability = (int)(pOldDurability - points);

            if (pNewDurability < 0)
                pNewDurability = 0;
            else if (pNewDurability > pMaxDurability)
                pNewDurability = (int)pMaxDurability;

            if (pOldDurability != pNewDurability)
            {
                // modify item stats _before_ Durability set to 0 to pass _ApplyItemMods internal check
                if (pNewDurability == 0 && pOldDurability > 0 && item.IsEquipped())
                    _ApplyItemMods(item, item.GetSlot(), false);

                item.SetDurability((uint)pNewDurability);

                // modify item stats _after_ restore durability to pass _ApplyItemMods internal check
                if (pNewDurability > 0 && pOldDurability == 0 && item.IsEquipped())
                    _ApplyItemMods(item, item.GetSlot(), true);

                item.SetState(ItemUpdateState.Changed, this);
            }
        }
        public void DurabilityPointLossForEquipSlot(byte slot)
        {
            if (HasAuraType(AuraType.PreventDurabilityLossFromCombat))
                return;

            Item pItem = GetItemByPos(InventorySlots.Bag0, slot);
            if (pItem != null)
                DurabilityPointsLoss(pItem, 1);
        }
        public void DurabilityRepairAll(bool takeCost, float discountMod, bool guildBank)
        {
            // Collecting all items that can be repaired and repair costs
            List<(Item item, ulong cost)> itemRepairCostStore = new();

            // equipped, backpack, bags itself
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; i++)
            {
                Item item = GetItemByPos((ushort)((InventorySlots.Bag0 << 8) | i));
                if (item != null)
                {
                    ulong cost = item.CalculateDurabilityRepairCost(discountMod);
                    if (cost != 0)
                        itemRepairCostStore.Add((item, cost));
                }
            }

            // items in inventory bags
            for (byte j = InventorySlots.BagStart; j < InventorySlots.ReagentBagEnd; j++)
            {
                for (byte i = 0; i < ItemConst.MaxBagSize; i++)
                {
                    Item item = GetItemByPos((ushort)((j << 8) | i));
                    if (item != null)
                    {
                        ulong cost = item.CalculateDurabilityRepairCost(discountMod);
                        if (cost != 0)
                            itemRepairCostStore.Add((item, cost));
                    }
                }
            }

            // Handling a free repair case - just repair every item without taking cost.
            if (!takeCost)
            {
                foreach (var (item, _) in itemRepairCostStore)
                    DurabilityRepair(item.GetPos(), false, 0.0f);
                return;
            }

            if (guildBank)
            {
                // Handling a repair for guild money case.
                // We have to repair items one by one until the guild bank has enough money available for withdrawal or until all items are repaired.

                Guild guild = GetGuild();
                if (guild == null)
                    return; // silent return, client shouldn't display this button for players without guild.

                ulong availableGuildMoney = guild.GetMemberAvailableMoneyForRepairItems(GetGUID());
                if (availableGuildMoney == 0)
                    return;

                // Sort the items by repair cost from lowest to highest
                itemRepairCostStore.OrderByDescending(a => a.cost);

                // We must calculate total repair cost and take money once to avoid spam in the guild bank log and reduce number of transactions in the database
                ulong totalCost = 0;

                foreach (var (item, cost) in itemRepairCostStore)
                {
                    ulong newTotalCost = totalCost + cost;
                    if (newTotalCost > availableGuildMoney || newTotalCost > PlayerConst.MaxMoneyAmount)
                        break;

                    totalCost = newTotalCost;
                    // Repair item without taking cost. We'll do it later.
                    DurabilityRepair(item.GetPos(), false, 0.0f);
                }
                // Take money for repairs from the guild bank
                guild.HandleMemberWithdrawMoney(GetSession(), totalCost, true);
            }
            else
            {
                // Handling a repair for player's money case.
                // Unlike repairing for guild money, in this case we must first check if player has enough money to repair all the items at once.

                ulong totalCost = 0;
                foreach (var (_, cost) in itemRepairCostStore)
                    totalCost += cost;

                if (!HasEnoughMoney(totalCost))
                    return; // silent return, client should display error by itself and not send opcode.

                ModifyMoney(-(int)totalCost);

                // Payment for repair has already been taken, so just repair every item without taking cost.
                foreach (var (item, cost) in itemRepairCostStore)
                    DurabilityRepair(item.GetPos(), false, 0.0f);
            }
        }

        public void DurabilityRepair(ushort pos, bool takeCost, float discountMod)
        {
            Item item = GetItemByPos(pos);
            if (item == null)
                return;


            if (takeCost)
            {
                ulong cost = item.CalculateDurabilityRepairCost(discountMod);
                if (!HasEnoughMoney(cost))
                {
                    Log.outDebug(LogFilter.PlayerItems, $"Player::DurabilityRepair: Player '{GetName()}' ({GetGUID()}) has not enough money to repair item");
                    return;
                }

                ModifyMoney(-(int)cost);
            }

            bool isBroken = item.IsBroken();

            item.SetDurability(item.m_itemData.MaxDurability);
            item.SetState(ItemUpdateState.Changed, this);

            // reapply mods for total broken and repaired item if equipped
            if (IsEquipmentPos(pos) && isBroken)
                _ApplyItemMods(item, (byte)(pos & 255), true);
        }

        //Store Item
        public InventoryResult CanStoreItem(byte bag, byte slot, List<ItemPosCount> dest, Item pItem, bool swap = false)
        {
            if (pItem == null)
                return InventoryResult.ItemNotFound;

            return CanStoreItem(bag, slot, dest, pItem.GetEntry(), pItem.GetCount(), pItem, swap);
        }

        InventoryResult CanStoreItem(byte bag, byte slot, List<ItemPosCount> dest, uint entry, uint count, Item pItem, bool swap)
        {
            return CanStoreItem(bag, slot, dest, entry, count, pItem, swap, out _);
        }

        InventoryResult CanStoreItem(byte bag, byte slot, List<ItemPosCount> dest, uint entry, uint count, Item pItem, bool swap, out uint no_space_count)
        {
            no_space_count = 0;
            Log.outDebug(LogFilter.Player, "STORAGE: CanStoreItem bag = {0}, slot = {1}, item = {2}, count = {3}", bag, slot, entry, count);

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(entry);
            if (pProto == null)
            {
                no_space_count = count;
                return swap ? InventoryResult.CantSwap : InventoryResult.ItemNotFound;
            }

            if (pItem != null)
            {
                // item used
                if (pItem.m_lootGenerated)
                {
                    no_space_count = count;
                    return InventoryResult.LootGone;
                }

                if (pItem.IsBindedNotWith(this))
                {
                    no_space_count = count;
                    return InventoryResult.NotOwner;
                }
            }

            // check count of items (skip for auto move for same player from bank)
            uint no_similar_count = 0;                            // can't store this amount similar items
            InventoryResult? tryHandleInvStoreResult(InventoryResult res, ref uint no_space_count)
            {
                if (res != InventoryResult.Ok)
                {
                    no_space_count = count + no_space_count;
                    return res;
                }

                if (count == 0)
                {
                    if (no_space_count == 0)
                        return InventoryResult.Ok;
                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }

                // not handled
                return null;
            }

            InventoryResult? tryHandleBagStoreResult(InventoryResult res, ref uint no_space_count)
            {
                if (res == InventoryResult.Ok && count == 0)
                {
                    if (no_similar_count == 0)
                        return InventoryResult.Ok;

                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }

                // not handled
                return null;
            }

            InventoryResult res = CanTakeMoreSimilarItems(entry, count, pItem, ref no_similar_count);
            InventoryResult? res2;
            if (res != InventoryResult.Ok)
            {
                if (count == no_similar_count)
                {
                    no_space_count = no_similar_count;
                    return res;
                }
                count -= no_similar_count;
            }

            // in specific slot
            if (bag != ItemConst.NullBag && slot != ItemConst.NullSlot)
            {
                res = CanStoreItem_InSpecificSlot(bag, slot, dest, pProto, ref count, swap, pItem);
                res2 = tryHandleInvStoreResult(res, ref no_space_count);
                if (res2.HasValue)
                    return res2.Value;
            }

            // not specific slot or have space for partly store only in specific slot
            byte inventorySlotEnd = (byte)(InventorySlots.ItemStart + GetInventorySlotCount());

            // in specific bag
            if (bag != ItemConst.NullBag)
            {
                // search stack in bag for merge to
                if (pProto.GetMaxStackSize() != 1)
                {
                    if (bag == InventorySlots.Bag0)               // inventory
                    {
                        res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, true, pItem, bag, slot);
                        res2 = tryHandleInvStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;

                        res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventorySlotEnd, dest, pProto, ref count, true, pItem, bag, slot);
                        res2 = tryHandleInvStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;
                    }
                    else                                            // equipped bag
                    {
                        // we need check 2 time (specialized/non_specialized), use NULL_BAG to prevent skipping bag
                        res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, false, pItem, ItemConst.NullBag, slot);
                        if (res != InventoryResult.Ok)
                            res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, true, pItem, ItemConst.NullBag, slot);

                        res2 = tryHandleInvStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;
                    }
                }

                // search free slot in bag for place to
                if (bag == InventorySlots.Bag0)                     // inventory
                {
                    if (pItem != null && pItem.HasItemFlag(ItemFieldFlags.Child))
                    {
                        res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, false, pItem, bag, slot);
                        res2 = tryHandleInvStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;
                    }

                    {
                        res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventorySlotEnd, dest, pProto, ref count, false, pItem, bag, slot);
                        res2 = tryHandleInvStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;
                    }
                }
                else                                                // equipped bag
                {
                    res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, false, pItem, ItemConst.NullBag, slot);
                    if (res != InventoryResult.Ok)
                        res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, true, pItem, ItemConst.NullBag, slot);

                    res2 = tryHandleInvStoreResult(res, ref no_space_count);
                    if (res2.HasValue)
                        return res2.Value;
                }
            }

            // not specific bag or have space for partly store only in specific bag

            // search stack for merge to
            if (pProto.GetMaxStackSize() != 1)
            {
                res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, true, pItem, bag, slot);

                res2 = tryHandleInvStoreResult(res, ref no_space_count);
                if (res2.HasValue)
                    return res2.Value;

                res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventorySlotEnd, dest, pProto, ref count, true, pItem, bag, slot);
                res2 = tryHandleInvStoreResult(res, ref no_space_count);
                if (res2.HasValue)
                    return res2.Value;

                if (pProto.GetBagFamily() != 0)
                {
                    for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                    {
                        res = CanStoreItem_InBag(i, dest, pProto, ref count, true, false, pItem, bag, slot);
                        res2 = tryHandleBagStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;
                    }
                }

                if (pProto.IsCraftingReagent())
                {
                    for (byte i = InventorySlots.ReagentBagStart; i < InventorySlots.ReagentBagEnd; i++)
                    {
                        res = CanStoreItem_InBag(i, dest, pProto, ref count, true, true, pItem, bag, slot);
                        res2 = tryHandleBagStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;
                    }
                }

                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                {
                    res = CanStoreItem_InBag(i, dest, pProto, ref count, true, true, pItem, bag, slot);
                    res2 = tryHandleBagStoreResult(res, ref no_space_count);
                    if (res2.HasValue)
                        return res2.Value;
                }
            }

            // search free slot - special bag case
            if (pProto.GetBagFamily() != 0)
            {
                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                {
                    res = CanStoreItem_InBag(i, dest, pProto, ref count, false, false, pItem, bag, slot);
                    res2 = tryHandleBagStoreResult(res, ref no_space_count);
                    if (res2.HasValue)
                        return res2.Value;
                }
            }

            if (pProto.IsCraftingReagent())
            {
                for (byte i = InventorySlots.ReagentBagStart; i < InventorySlots.ReagentBagEnd; i++)
                {
                    res = CanStoreItem_InBag(i, dest, pProto, ref count, false, true, pItem, bag, slot);
                    res2 = tryHandleBagStoreResult(res, ref no_space_count);
                    if (res2.HasValue)
                        return res2.Value;
                }
            }

            if (pItem != null && pItem.IsNotEmptyBag())
                return InventoryResult.BagInBag;

            if (pItem != null && pItem.HasItemFlag(ItemFieldFlags.Child))
            {
                res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, false, pItem, bag, slot);
                res2 = tryHandleInvStoreResult(res, ref no_space_count);
                if (res2.HasValue)
                    return res2.Value;
            }

            // search free slot
            // new bags can be directly equipped
            if (pItem == null && pProto.GetClass() == ItemClass.Container && (pProto.GetBonding() == ItemBondingType.None || pProto.GetBonding() == ItemBondingType.OnAcquire))
            {
                switch ((ItemSubClassContainer)pProto.GetSubClass())
                {
                    case ItemSubClassContainer.Container:
                        res = CanStoreItem_InInventorySlots(InventorySlots.BagStart, InventorySlots.BagEnd, dest, pProto, ref count, false, pItem, bag, slot);
                        res2 = tryHandleInvStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;
                        break;
                    case ItemSubClassContainer.ReagentContainer:
                        res = CanStoreItem_InInventorySlots(InventorySlots.ReagentBagStart, InventorySlots.ReagentBagEnd, dest, pProto, ref count, false, pItem, bag, slot);
                        res2 = tryHandleInvStoreResult(res, ref no_space_count);
                        if (res2.HasValue)
                            return res2.Value;
                        break;
                    default:
                        break;
                }
            }

            res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventorySlotEnd, dest, pProto, ref count, false, pItem, bag, slot);
            res2 = tryHandleInvStoreResult(res, ref no_space_count);
            if (res2.HasValue)
                return res2.Value;

            for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
            {
                res = CanStoreItem_InBag(i, dest, pProto, ref count, false, true, pItem, bag, slot);
                res2 = tryHandleBagStoreResult(res, ref no_space_count);
                if (res2.HasValue)
                    return res2.Value;
            }

            no_space_count = count + no_similar_count;

            return InventoryResult.InvFull;
        }
        public InventoryResult CanStoreItems(Item[] items, int count, ref uint offendingItemId)
        {
            Item item2;

            // fill space tables, creating a mock-up of the player's inventory

            // counts
            uint[] inventoryCounts = new uint[InventorySlots.ItemEnd - InventorySlots.ItemStart];
            uint[][] bagCounts = new uint[InventorySlots.ReagentBagEnd - InventorySlots.BagStart][];

            // Item array
            Item[] inventoryPointers = new Item[InventorySlots.ItemEnd - InventorySlots.ItemStart];
            Item[][] bagPointers = new Item[InventorySlots.ReagentBagEnd - InventorySlots.BagStart][];

            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

            // filling inventory
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
            {
                // build items in stock backpack
                item2 = GetItemByPos(InventorySlots.Bag0, i);
                if (item2 != null && !item2.IsInTrade())
                {
                    inventoryCounts[i - InventorySlots.ItemStart] = item2.GetCount();
                    inventoryPointers[i - InventorySlots.ItemStart] = item2;
                }
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.ReagentBagEnd; i++)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                {
                    bagCounts[i - InventorySlots.BagStart] = new uint[ItemConst.MaxBagSize];
                    bagPointers[i - InventorySlots.BagStart] = new Item[ItemConst.MaxBagSize];
                    for (byte j = 0; j < pBag.GetBagSize(); j++)
                    {
                        // build item counts in equippable bags
                        item2 = GetItemByPos(i, j);
                        if (item2 != null && !item2.IsInTrade())
                        {
                            bagCounts[i - InventorySlots.BagStart][j] = item2.GetCount();
                            bagPointers[i - InventorySlots.BagStart][j] = item2;
                        }
                    }
                }
            }

            // check free space for all items that we wish to add
            for (int k = 0; k < count; ++k)
            {
                // Incoming item
                Item item = items[k];

                // no item
                if (item == null)
                    continue;

                uint remaining_count = item.GetCount();

                Log.outDebug(LogFilter.Player, $"STORAGE: CanStoreItems {k + 1}. item = {item.GetEntry()}, count = {remaining_count}");
                ItemTemplate pProto = item.GetTemplate();

                // strange item
                if (pProto == null)
                    return InventoryResult.ItemNotFound;

                // item used
                if (item.m_lootGenerated)
                    return InventoryResult.LootGone;

                // item it 'bind'
                if (item.IsBindedNotWith(this))
                    return InventoryResult.NotOwner;

                ItemTemplate pBagProto;

                // item is 'one item only'
                InventoryResult res = CanTakeMoreSimilarItems(item, ref offendingItemId);
                if (res != InventoryResult.Ok)
                    return res;

                bool b_found = false;
                // search stack for merge to
                if (pProto.GetMaxStackSize() != 1)
                {
                    for (byte t = InventorySlots.ItemStart; t < inventoryEnd; ++t)
                    {
                        item2 = inventoryPointers[t - InventorySlots.ItemStart];
                        if (item2 != null && item2.CanBeMergedPartlyWith(pProto) == InventoryResult.Ok && inventoryCounts[t - InventorySlots.ItemStart] < pProto.GetMaxStackSize())
                        {
                            inventoryCounts[t - InventorySlots.ItemStart] += remaining_count;
                            remaining_count = inventoryCounts[t - InventorySlots.ItemStart] < pProto.GetMaxStackSize() ? 0 : inventoryCounts[t - InventorySlots.ItemStart] - pProto.GetMaxStackSize();

                            b_found = remaining_count == 0;
                            // if no pieces of the stack remain, then stop checking stock bag
                            if (b_found)
                                break;
                        }
                    }

                    if (b_found)
                        continue;

                    for (byte t = InventorySlots.BagStart; !b_found && t < InventorySlots.ReagentBagEnd; ++t)
                    {
                        Bag bag = GetBagByPos(t);
                        if (bag != null)
                        {
                            if (!Item.ItemCanGoIntoBag(item.GetTemplate(), bag.GetTemplate()))
                                continue;

                            for (byte j = 0; j < bag.GetBagSize(); j++)
                            {
                                item2 = bagPointers[t - InventorySlots.BagStart][j];
                                if (item2 != null && item2.CanBeMergedPartlyWith(pProto) == InventoryResult.Ok && bagCounts[t - InventorySlots.BagStart][j] < pProto.GetMaxStackSize())
                                {
                                    // add count to stack so that later items in the list do not double-book
                                    bagCounts[t - InventorySlots.BagStart][j] += remaining_count;
                                    remaining_count = bagCounts[t - InventorySlots.BagStart][j] < pProto.GetMaxStackSize() ? 0 : bagCounts[t - InventorySlots.BagStart][j] - pProto.GetMaxStackSize();

                                    b_found = remaining_count == 0;

                                    // if no pieces of the stack remain, then stop checking equippable bags
                                    if (b_found)
                                        break;
                                }
                            }
                        }
                    }

                    if (b_found)
                        continue;
                }
                b_found = false;
                // special bag case
                if (pProto.GetBagFamily() != 0)
                {
                    for (byte t = InventorySlots.BagStart; !b_found && t < InventorySlots.ReagentBagEnd; ++t)
                    {
                        Bag bag = GetBagByPos(t);
                        if (bag != null)
                        {
                            pBagProto = bag.GetTemplate();

                            // not plain container check
                            if (pBagProto != null && (pBagProto.GetClass() != ItemClass.Container || (pBagProto.GetSubClass() != (uint)ItemSubClassContainer.Container && pBagProto.GetSubClass() != (uint)ItemSubClassContainer.ReagentContainer)) &&
                                Item.ItemCanGoIntoBag(pProto, pBagProto))
                            {
                                for (uint j = 0; j < bag.GetBagSize(); j++)
                                {
                                    if (bagCounts[t - InventorySlots.BagStart][j] == 0)
                                    {
                                        bagCounts[t - InventorySlots.BagStart][j] = remaining_count;
                                        bagPointers[t - InventorySlots.BagStart][j] = item;

                                        b_found = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (b_found)
                        continue;
                }

                // search free slot
                b_found = false;
                for (int t = InventorySlots.ItemStart; t < inventoryEnd; ++t)
                {
                    if (inventoryCounts[t - InventorySlots.ItemStart] == 0)
                    {
                        inventoryCounts[t - InventorySlots.ItemStart] = 1;
                        inventoryPointers[t - InventorySlots.ItemStart] = item;

                        b_found = true;
                        break;
                    }
                }

                if (b_found)
                    continue;

                // search free slot in bags
                for (byte t = InventorySlots.BagStart; !b_found && t < InventorySlots.ReagentBagEnd; ++t)
                {
                    Bag bag = GetBagByPos(t);
                    if (bag != null)
                    {
                        pBagProto = bag.GetTemplate();

                        // special bag already checked
                        if (pBagProto != null && (pBagProto.GetClass() != ItemClass.Container || (pBagProto.GetSubClass() != (uint)ItemSubClassContainer.Container && pBagProto.GetSubClass() != (uint)ItemSubClassContainer.ReagentContainer)))
                            continue;

                        for (uint j = 0; j < bag.GetBagSize(); j++)
                        {
                            if (bagCounts[t - InventorySlots.BagStart][j] == 0)
                            {
                                bagCounts[t - InventorySlots.BagStart][j] = remaining_count;
                                bagPointers[t - InventorySlots.BagStart][j] = item;

                                b_found = true;
                                break;
                            }
                        }
                    }
                }

                // if no free slot found for all pieces of the item, then return an error
                if (!b_found)
                    return InventoryResult.BagFull;
            }

            return InventoryResult.Ok;
        }

        public InventoryResult CanStoreNewItem(byte bag, byte slot, List<ItemPosCount> dest, uint item, uint count, out uint no_space_count)
        {
            return CanStoreItem(bag, slot, dest, item, count, null, false, out no_space_count);
        }

        public InventoryResult CanStoreNewItem(byte bag, byte slot, List<ItemPosCount> dest, uint item, uint count)
        {
            return CanStoreItem(bag, slot, dest, item, count, null, false, out _);
        }

        Item _StoreItem(ushort pos, Item pItem, uint count, bool clone, bool update)
        {
            if (pItem == null)
                return null;

            byte bag = (byte)(pos >> 8);
            byte slot = (byte)(pos & 255);

            Log.outDebug(LogFilter.Player, "STORAGE: StoreItem bag = {0}, slot = {1}, item = {2}, count = {3}, guid = {4}", bag, slot, pItem.GetEntry(), count, pItem.GetGUID().ToString());

            Item pItem2 = GetItemByPos(bag, slot);

            if (pItem2 == null)
            {
                if (clone)
                    pItem = pItem.CloneItem(count, this);
                else
                    pItem.SetCount(count);

                if (pItem == null)
                    return null;

                if (pItem.GetBonding() == ItemBondingType.OnAcquire ||
                    pItem.GetBonding() == ItemBondingType.Quest ||
                    (pItem.GetBonding() == ItemBondingType.OnEquip && IsBagPos(pos)))
                    pItem.SetBinding(true);

                Bag pBag = bag == InventorySlots.Bag0 ? null : GetBagByPos(bag);
                if (pBag == null)
                {
                    m_items[slot] = pItem;
                    SetInvSlot(slot, pItem.GetGUID());
                    pItem.SetContainedIn(GetGUID());
                    pItem.SetOwnerGUID(GetGUID());

                    pItem.SetSlot(slot);
                    pItem.SetContainer(null);
                }
                else
                    pBag.StoreItem(slot, pItem, update);

                if (IsInWorld && update)
                {
                    pItem.AddToWorld();
                    pItem.SendUpdateToPlayer(this);
                }

                pItem.SetState(ItemUpdateState.Changed, this);
                if (pBag != null)
                    pBag.SetState(ItemUpdateState.Changed, this);

                AddEnchantmentDurations(pItem);
                AddItemDurations(pItem);

                if (bag == InventorySlots.Bag0 || (bag >= InventorySlots.BagStart && bag < InventorySlots.ReagentBagEnd))
                    ApplyItemObtainSpells(pItem, true);

                return pItem;
            }
            else
            {
                if (pItem2.GetBonding() == ItemBondingType.OnAcquire ||
                    pItem2.GetBonding() == ItemBondingType.Quest ||
                    (pItem2.GetBonding() == ItemBondingType.OnEquip && IsBagPos(pos)))
                    pItem2.SetBinding(true);

                pItem2.SetCount(pItem2.GetCount() + count);
                if (IsInWorld && update)
                    pItem2.SendUpdateToPlayer(this);

                if (!clone)
                {
                    // delete item (it not in any slot currently)
                    if (IsInWorld && update)
                    {
                        pItem.RemoveFromWorld();
                        pItem.DestroyForPlayer(this);
                    }

                    RemoveEnchantmentDurations(pItem);
                    RemoveItemDurations(pItem);

                    pItem.SetOwnerGUID(GetGUID());                 // prevent error at next SetState in case trade/mail/buy from vendor
                    pItem.SetNotRefundable(this);
                    pItem.ClearSoulboundTradeable(this);
                    RemoveTradeableItem(pItem);
                    pItem.SetState(ItemUpdateState.Removed, this);
                }

                AddEnchantmentDurations(pItem2);

                pItem2.SetState(ItemUpdateState.Changed, this);

                if (bag == InventorySlots.Bag0 || (bag >= InventorySlots.BagStart && bag < InventorySlots.ReagentBagEnd))
                    ApplyItemObtainSpells(pItem2, true);

                return pItem2;
            }
        }

        public Item StoreItem(List<ItemPosCount> dest, Item pItem, bool update)
        {
            if (pItem == null)
                return null;

            Item lastItem = pItem;
            for (var i = 0; i < dest.Count; i++)
            {
                var itemPosCount = dest[i];
                ushort pos = itemPosCount.pos;
                uint count = itemPosCount.count;

                if (i == dest.Count - 1)
                {
                    lastItem = _StoreItem(pos, pItem, count, false, update);
                    break;
                }

                lastItem = _StoreItem(pos, pItem, count, true, update);
            }

            AutoUnequipChildItem(lastItem);

            return lastItem;
        }

        bool StoreNewItemInBestSlots(uint itemId, uint amount, ItemContext context)
        {
            Log.outDebug(LogFilter.Player, "STORAGE: Creating initial item, itemId = {0}, count = {1}", itemId, amount);

            InventoryResult msg;
            // attempt equip by one
            while (amount > 0)
            {
                msg = CanEquipNewItem(ItemConst.NullSlot, out ushort eDest, itemId, false);
                if (msg != InventoryResult.Ok)
                    break;

                EquipNewItem(eDest, itemId, context, true);
                AutoUnequipOffhandIfNeed();
                --amount;
            }

            if (amount == 0)
                return true;                                        // equipped

            // attempt store
            List<ItemPosCount> sDest = new();
            // store in main bag to simplify second pass (special bags can be not equipped yet at this moment)
            msg = CanStoreNewItem(InventorySlots.Bag0, ItemConst.NullSlot, sDest, itemId, amount);
            if (msg == InventoryResult.Ok)
            {
                StoreNewItem(sDest, itemId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(itemId), null, context);
                return true;                                        // stored
            }

            // item can't be added
            Log.outError(LogFilter.Player, "STORAGE: Can't equip or store initial item {0} for race {1} class {2}, error msg = {3}", itemId, GetRace(), GetClass(), msg);
            return false;
        }

        public Item StoreNewItem(List<ItemPosCount> pos, uint itemId, bool update, uint randomBonusListId = 0, List<ObjectGuid> allowedLooters = null, ItemContext context = 0, List<uint> bonusListIDs = null, bool addToCollection = true)
        {
            uint count = 0;
            foreach (var itemPosCount in pos)
                count += itemPosCount.count;

            // quest objectives must be processed twice - QUEST_OBJECTIVE_FLAG_2_QUEST_BOUND_ITEM prevents item creation
            ItemAddedQuestCheck(itemId, count, true, out bool hadBoundItemObjective);
            if (hadBoundItemObjective)
                return null;

            Item item = Item.CreateItem(itemId, count, context, this, bonusListIDs == null);
            if (item != null)
            {
                item.SetItemFlag(ItemFieldFlags.NewItem);

                if (bonusListIDs != null)
                    item.SetBonuses(bonusListIDs);

                item.SetFixedLevel(GetLevel());
                item.SetItemRandomBonusList(randomBonusListId);
                item.SetCreatePlayedTime(GetTotalPlayedTime());

                item = StoreItem(pos, item, update);

                ItemAddedQuestCheck(itemId, count, false);
                UpdateCriteria(CriteriaType.ObtainAnyItem, itemId, count);
                UpdateCriteria(CriteriaType.AcquireItem, itemId, count);

                if (allowedLooters != null && allowedLooters.Count > 1 && item.GetTemplate().GetMaxStackSize() == 1 && item.IsSoulBound())
                {
                    item.SetSoulboundTradeable(allowedLooters);
                    AddTradeableItem(item);

                    // save data
                    StringBuilder ss = new();
                    foreach (var guid in allowedLooters)
                        ss.AppendFormat("{0} ", guid);

                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_BOP_TRADE);
                    stmt.AddValue(0, item.GetGUID().GetCounter());
                    stmt.AddValue(1, ss.ToString());
                    DB.Characters.Execute(stmt);
                }

                if (addToCollection)
                    GetSession().GetCollectionMgr().OnItemAdded(item);

                ItemChildEquipmentRecord childItemEntry = Global.DB2Mgr.GetItemChildEquipment(itemId);
                if (childItemEntry != null)
                {
                    ItemTemplate childTemplate = Global.ObjectMgr.GetItemTemplate(childItemEntry.ChildItemID);
                    if (childTemplate != null)
                    {
                        List<ItemPosCount> childDest = new();
                        CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, childDest, childTemplate, ref count, false, null, ItemConst.NullBag, ItemConst.NullSlot);
                        Item childItem = StoreNewItem(childDest, childTemplate.GetId(), update, 0, null, context, null, addToCollection);
                        if (childItem != null)
                        {
                            childItem.SetCreator(item.GetGUID());
                            childItem.SetItemFlag(ItemFieldFlags.Child);
                            item.SetChildItem(childItem.GetGUID());
                        }
                    }
                }

                if (item.GetTemplate().GetInventoryType() != InventoryType.NonEquip)
                    UpdateAverageItemLevelTotal();
            }
            return item;
        }

        //Move Item
        InventoryResult CanTakeMoreSimilarItems(Item pItem)
        {
            uint notused = 0;
            return CanTakeMoreSimilarItems(pItem.GetEntry(), pItem.GetCount(), pItem, ref notused);
        }
        InventoryResult CanTakeMoreSimilarItems(Item pItem, ref uint offendingItemId)
        {
            uint notused = 0;
            return CanTakeMoreSimilarItems(pItem.GetEntry(), pItem.GetCount(), pItem, ref notused, ref offendingItemId);
        }
        InventoryResult CanTakeMoreSimilarItems(uint entry, uint count, Item pItem, ref uint no_space_count)
        {
            uint notused = 0;
            return CanTakeMoreSimilarItems(entry, count, pItem, ref no_space_count, ref notused);
        }
        InventoryResult CanTakeMoreSimilarItems(uint entry, uint count, Item pItem, ref uint no_space_count, ref uint offendingItemId)
        {
            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(entry);
            if (pProto == null)
            {
                no_space_count = count;
                return InventoryResult.ItemMaxCount;
            }

            if (pItem != null && pItem.m_lootGenerated)
                return InventoryResult.LootGone;

            uint limitCategory = pItem != null ? pItem.GetItemLimitCategory() : pProto.GetItemLimitCategory();

            // no maximum
            if ((pProto.GetMaxCount() <= 0 && limitCategory == 0) || pProto.GetMaxCount() == 2147483647)
                return InventoryResult.Ok;

            if (pProto.GetMaxCount() > 0)
            {
                uint curcount = GetItemCount(pProto.GetId(), true, pItem);
                if (curcount + count > pProto.GetMaxCount())
                {
                    no_space_count = count + curcount - pProto.GetMaxCount();
                    return InventoryResult.ItemMaxCount;
                }
            }

            // check unique-equipped limit
            if (limitCategory != 0)
            {
                ItemLimitCategoryRecord limitEntry = CliDB.ItemLimitCategoryStorage.LookupByKey(limitCategory);
                if (limitEntry == null)
                {
                    no_space_count = count;
                    return InventoryResult.NotEquippable;
                }

                if (limitEntry.Flags == 0)
                {
                    byte limitQuantity = GetItemLimitCategoryQuantity(limitEntry);
                    uint curcount = GetItemCountWithLimitCategory(limitCategory, pItem);
                    if (curcount + count > limitQuantity)
                    {
                        no_space_count = count + curcount - limitQuantity;
                        offendingItemId = pProto.GetId();
                        return InventoryResult.ItemMaxLimitCategoryCountExceededIs;
                    }
                }
            }

            return InventoryResult.Ok;
        }

        //UseItem
        public InventoryResult CanUseItem(Item pItem, bool not_loading = true)
        {
            if (pItem != null)
            {
                Log.outDebug(LogFilter.Player, "ItemStorage: CanUseItem item = {0}", pItem.GetEntry());

                if (!IsAlive() && not_loading)
                    return InventoryResult.PlayerDead;

                ItemTemplate pProto = pItem.GetTemplate();
                if (pProto != null)
                {
                    if (pItem.IsBindedNotWith(this))
                        return InventoryResult.NotOwner;

                    if (GetLevel() < pItem.GetRequiredLevel())
                        return InventoryResult.CantEquipLevelI;

                    InventoryResult res = CanUseItem(pProto);
                    if (res != InventoryResult.Ok)
                        return res;

                    if (pItem.GetSkill() != 0)
                    {
                        bool allowEquip = false;
                        SkillType itemSkill = pItem.GetSkill();
                        // Armor that is binded to account can "morph" from plate to mail, etc. if skill is not learned yet.
                        if (pProto.GetQuality() == ItemQuality.Heirloom && pProto.GetClass() == ItemClass.Armor && !HasSkill(itemSkill))
                        {
                            // TODO: when you right-click already equipped item it throws EQUIP_ERR_PROFICIENCY_NEEDED.

                            // In fact it's a visual bug, everything works properly... I need sniffs of operations with
                            // binded to account items from off server.

                            switch (GetClass())
                            {
                                case Class.Hunter:
                                case Class.Shaman:
                                    allowEquip = (itemSkill == SkillType.Mail);
                                    break;
                                case Class.Paladin:
                                case Class.Warrior:
                                    allowEquip = (itemSkill == SkillType.PlateMail);
                                    break;
                            }
                        }
                        if (!allowEquip && GetSkillValue(itemSkill) == 0)
                            return InventoryResult.ProficiencyNeeded;
                    }

                    return InventoryResult.Ok;
                }
            }
            return InventoryResult.ItemNotFound;
        }
        public InventoryResult CanUseItem(ItemTemplate proto, bool skipRequiredLevelCheck = false)
        {
            // Used by group, function GroupLoot, to know if a prototype can be used by a player

            if (proto == null)
                return InventoryResult.ItemNotFound;

            if (proto.HasFlag(ItemFlags2.InternalItem))
                return InventoryResult.CantEquipEver;

            if (proto.HasFlag(ItemFlags2.FactionHorde) && GetTeam() != Team.Horde)
                return InventoryResult.CantEquipEver;

            if (proto.HasFlag(ItemFlags2.FactionAlliance) && GetTeam() != Team.Alliance)
                return InventoryResult.CantEquipEver;

            if ((proto.GetAllowableClass() & GetClassMask()) == 0 || !proto.GetAllowableRace().HasRace(GetRace()))
                return InventoryResult.CantEquipEver;

            if (proto.GetRequiredSkill() != 0)
            {
                if (GetSkillValue((SkillType)proto.GetRequiredSkill()) == 0)
                    return InventoryResult.ProficiencyNeeded;
                else if (GetSkillValue((SkillType)proto.GetRequiredSkill()) < proto.GetRequiredSkillRank())
                    return InventoryResult.CantEquipSkill;
            }

            if (proto.GetRequiredSpell() != 0 && !HasSpell(proto.GetRequiredSpell()))
                return InventoryResult.ProficiencyNeeded;

            if (!skipRequiredLevelCheck && GetLevel() < proto.GetBaseRequiredLevel())
                return InventoryResult.CantEquipLevelI;

            // If World Event is not active, prevent using event dependant items
            if (proto.GetHolidayID() != 0 && !Global.GameEventMgr.IsHolidayActive(proto.GetHolidayID()))
                return InventoryResult.ClientLockedOut;

            if (proto.GetRequiredReputationFaction() != 0 && (uint)GetReputationRank(proto.GetRequiredReputationFaction()) < proto.GetRequiredReputationRank())
                return InventoryResult.CantEquipReputation;

            // learning (recipes, mounts, pets, etc.)
            if (proto.Effects.Count >= 2)
            {
                if (proto.Effects[0].SpellID == 483 || proto.Effects[0].SpellID == 55884)
                    if (HasSpell((uint)proto.Effects[1].SpellID))
                        return InventoryResult.InternalBagError;
            }

            ArtifactRecord artifact = CliDB.ArtifactStorage.LookupByKey(proto.GetArtifactID());
            if (artifact != null)
                if ((ChrSpecialization)artifact.ChrSpecializationID != GetPrimarySpecialization())
                    return InventoryResult.CantUseItem;

            return InventoryResult.Ok;
        }

        //Equip/Unequip Item
        InventoryResult CanUnequipItems(uint item, uint count)
        {
            InventoryResult res = InventoryResult.Ok;

            uint tempcount = 0;
            bool result = ForEachItem(ItemSearchLocation.Equipment, pItem =>
            {
                if (pItem.GetEntry() == item)
                {
                    InventoryResult ires = CanUnequipItem(pItem.GetPos(), false);
                    if (ires == InventoryResult.Ok)
                    {
                        tempcount += pItem.GetCount();
                        if (tempcount >= count)
                            return false;
                    }
                    else
                        res = ires;
                }
                return true;
            });

            if (!result) // we stopped early due to a sucess
                return InventoryResult.Ok;

            return res; // return latest error if any
        }

        public Item EquipNewItem(ushort pos, uint item, ItemContext context, bool update)
        {
            Item pItem = Item.CreateItem(item, 1, context, this);
            if (pItem != null)
            {
                UpdateCriteria(CriteriaType.ObtainAnyItem, item, 1);
                Item equippedItem = EquipItem(pos, pItem, update);
                ItemAddedQuestCheck(item, 1);
                return equippedItem;
            }

            return null;
        }
        public Item EquipItem(ushort pos, Item pItem, bool update)
        {
            AddEnchantmentDurations(pItem);
            AddItemDurations(pItem);

            byte bag = (byte)(pos >> 8);
            byte slot = (byte)(pos & 255);

            Item pItem2 = GetItemByPos(bag, slot);

            if (pItem2 == null)
            {
                VisualizeItem(slot, pItem);

                if (IsAlive())
                {
                    ItemTemplate pProto = pItem.GetTemplate();

                    // item set bonuses applied only at equip and removed at unequip, and still active for broken items
                    if (pProto != null && pProto.GetItemSet() != 0)
                        Item.AddItemsSetItem(this, pItem);

                    _ApplyItemMods(pItem, slot, true);

                    if (pProto != null && IsInCombat() && (pProto.GetClass() == ItemClass.Weapon || pProto.GetInventoryType() == InventoryType.Relic) && m_weaponChangeTimer == 0)
                    {
                        uint cooldownSpell = (uint)(GetClass() == Class.Rogue ? 6123 : 6119);
                        var spellProto = Global.SpellMgr.GetSpellInfo(cooldownSpell, Difficulty.None);

                        if (spellProto == null)
                            Log.outError(LogFilter.Player, "Weapon switch cooldown spell {0} couldn't be found in Spell.dbc", cooldownSpell);
                        else
                        {
                            m_weaponChangeTimer = spellProto.StartRecoveryTime;

                            GetSpellHistory().AddGlobalCooldown(spellProto, TimeSpan.FromMilliseconds(m_weaponChangeTimer));

                            SpellCooldownPkt spellCooldown = new();
                            spellCooldown.Caster = GetGUID();
                            spellCooldown.Flags = SpellCooldownFlags.IncludeGCD;
                            spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(cooldownSpell, 0));
                            SendPacket(spellCooldown);
                        }
                    }
                }

                pItem.SetItemZoneFlag(ItemZoneFlags.Equipped);

                if (IsInWorld && update)
                {
                    pItem.AddToWorld();
                    pItem.SendUpdateToPlayer(this);
                }

                ApplyEquipCooldown(pItem);

                // update expertise and armor penetration - passive auras may need it

                if (slot == EquipmentSlot.MainHand)
                    UpdateExpertise(WeaponAttackType.BaseAttack);
                else if (slot == EquipmentSlot.OffHand)
                    UpdateExpertise(WeaponAttackType.OffAttack);

                switch (slot)
                {
                    case EquipmentSlot.MainHand:
                    case EquipmentSlot.OffHand:
                        RecalculateRating(CombatRating.ArmorPenetration);
                        break;
                }
            }
            else
            {
                pItem2.SetCount(pItem2.GetCount() + pItem.GetCount());
                if (IsInWorld && update)
                    pItem2.SendUpdateToPlayer(this);

                if (IsInWorld && update)
                {
                    pItem.RemoveFromWorld();
                    pItem.DestroyForPlayer(this);
                }

                RemoveEnchantmentDurations(pItem);
                RemoveItemDurations(pItem);

                pItem.SetOwnerGUID(GetGUID());                     // prevent error at next SetState in case trade/mail/buy from vendor
                pItem.SetNotRefundable(this);
                pItem.ClearSoulboundTradeable(this);
                RemoveTradeableItem(pItem);
                pItem.SetState(ItemUpdateState.Removed, this);
                pItem2.SetState(ItemUpdateState.Changed, this);

                ApplyEquipCooldown(pItem2);

                return pItem2;
            }

            if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand)
                CheckTitanGripPenalty();

            // only for full equip instead adding to stack
            UpdateCriteria(CriteriaType.EquipItem, pItem.GetEntry());
            UpdateCriteria(CriteriaType.EquipItemInSlot, slot, pItem.GetEntry());

            UpdateAverageItemLevelEquipped();

            return pItem;
        }
        public void EquipChildItem(byte parentBag, byte parentSlot, Item parentItem)
        {
            ItemChildEquipmentRecord itemChildEquipment = Global.DB2Mgr.GetItemChildEquipment(parentItem.GetEntry());
            if (itemChildEquipment != null)
            {
                Item childItem = GetChildItemByGuid(parentItem.GetChildItem());
                if (childItem != null)
                {
                    ushort childDest = (ushort)((InventorySlots.Bag0 << 8) | itemChildEquipment.ChildItemEquipSlot);
                    if (childItem.GetPos() != childDest)
                    {
                        Item dstItem = GetItemByPos(childDest);
                        if (dstItem == null)                                      // empty slot, simple case
                        {
                            RemoveItem(childItem.GetBagSlot(), childItem.GetSlot(), true);
                            EquipItem(childDest, childItem, true);
                            AutoUnequipOffhandIfNeed();
                        }
                        else                                                    // have currently equipped item, not simple case
                        {
                            byte dstbag = dstItem.GetBagSlot();
                            byte dstslot = dstItem.GetSlot();

                            InventoryResult msg = CanUnequipItem(childDest, !childItem.IsBag());
                            if (msg != InventoryResult.Ok)
                            {
                                SendEquipError(msg, dstItem);
                                return;
                            }

                            // check dest.src move possibility but try to store currently equipped item in the bag where the parent item is
                            List<ItemPosCount> sSrc = new();
                            ushort eSrc = 0;
                            if (IsInventoryPos(parentBag, parentSlot))
                            {
                                msg = CanStoreItem(parentBag, ItemConst.NullSlot, sSrc, dstItem, true);
                                if (msg != InventoryResult.Ok)
                                    msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, sSrc, dstItem, true);
                            }
                            else if (IsBankPos(parentBag, parentSlot))
                            {
                                msg = CanBankItem(parentBag, ItemConst.NullSlot, sSrc, dstItem, true);
                                if (msg != InventoryResult.Ok)
                                    msg = CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, sSrc, dstItem, true);
                            }
                            else if (IsEquipmentPos(parentBag, parentSlot))
                            {
                                msg = CanEquipItem(parentSlot, out eSrc, dstItem, true);
                                if (msg == InventoryResult.Ok)
                                    msg = CanUnequipItem(eSrc, true);
                            }

                            if (msg != InventoryResult.Ok)
                            {
                                SendEquipError(msg, dstItem, childItem);
                                return;
                            }

                            // now do moves, remove...
                            RemoveItem(dstbag, dstslot, false);
                            RemoveItem(childItem.GetBagSlot(), childItem.GetSlot(), false);

                            // add to dest
                            EquipItem(childDest, childItem, true);

                            // add to src
                            if (IsInventoryPos(parentBag, parentSlot))
                                StoreItem(sSrc, dstItem, true);
                            else if (IsBankPos(parentBag, parentSlot))
                                BankItem(sSrc, dstItem, true);
                            else if (IsEquipmentPos(parentBag, parentSlot))
                                EquipItem(eSrc, dstItem, true);

                            AutoUnequipOffhandIfNeed();
                        }
                    }
                }
            }
        }
        public void AutoUnequipChildItem(Item parentItem)
        {
            if (Global.DB2Mgr.GetItemChildEquipment(parentItem.GetEntry()) != null)
            {
                Item childItem = GetChildItemByGuid(parentItem.GetChildItem());
                if (childItem != null)
                {
                    if (IsChildEquipmentPos(childItem.GetPos()))
                        return;

                    List<ItemPosCount> dest = new();
                    uint count = childItem.GetCount();
                    InventoryResult result = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, childItem.GetTemplate(), ref count, false, childItem, ItemConst.NullBag, ItemConst.NullSlot);
                    if (result != InventoryResult.Ok)
                        return;

                    RemoveItem(childItem.GetBagSlot(), childItem.GetSlot(), true);
                    StoreItem(dest, childItem, true);
                }
            }
        }
        void QuickEquipItem(ushort pos, Item pItem)
        {
            if (pItem != null)
            {
                AddEnchantmentDurations(pItem);
                AddItemDurations(pItem);

                byte slot = (byte)(pos & 255);
                VisualizeItem(slot, pItem);

                pItem.SetItemZoneFlag(ItemZoneFlags.Equipped);

                if (IsInWorld)
                {
                    pItem.AddToWorld();
                    pItem.SendUpdateToPlayer(this);
                }

                if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand)
                    CheckTitanGripPenalty();

                UpdateCriteria(CriteriaType.EquipItem, pItem.GetEntry());
                UpdateCriteria(CriteriaType.EquipItemInSlot, slot, pItem.GetEntry());
            }
        }
        public void SendEquipError(InventoryResult msg, Item item1 = null, Item item2 = null, uint itemId = 0)
        {
            InventoryChangeFailure failure = new();
            failure.BagResult = msg;

            if (msg != InventoryResult.Ok)
            {
                if (item1 != null)
                    failure.Item[0] = item1.GetGUID();

                if (item2 != null)
                    failure.Item[1] = item2.GetGUID();

                failure.ContainerBSlot = 0; // bag equip slot, used with EQUIP_ERR_EVENT_AUTOEQUIP_BIND_CONFIRM and EQUIP_ERR_ITEM_DOESNT_GO_INTO_BAG2

                switch (msg)
                {
                    case InventoryResult.CantEquipLevelI:
                    case InventoryResult.PurchaseLevelTooLow:
                    {
                        failure.Level = (item1 != null ? item1.GetRequiredLevel() : 0);
                        break;
                    }
                    case InventoryResult.EventAutoequipBindConfirm:    // no idea about this one...
                    {
                        //failure.SrcContainer
                        //failure.SrcSlot
                        //failure.DstContainer
                        break;
                    }
                    case InventoryResult.ItemMaxLimitCategoryCountExceededIs:
                    case InventoryResult.ItemMaxLimitCategorySocketedExceededIs:
                    case InventoryResult.ItemMaxLimitCategoryEquippedExceededIs:
                    {
                        if (item1 != null)
                            failure.LimitCategory = (int)item1.GetItemLimitCategory();
                        else
                        {
                            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemId);
                            if (proto != null)
                                failure.LimitCategory = (int)proto.GetItemLimitCategory();
                        }
                        break;
                    }
                    default:
                        break;
                }
            }

            SendPacket(failure);
        }

        //Add/Remove/Misc Item 
        public bool AddItem(uint itemId, uint count)
        {
            uint noSpaceForCount;
            List<ItemPosCount> dest = new();
            InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, count, out noSpaceForCount);
            if (msg != InventoryResult.Ok)
                count -= noSpaceForCount;

            if (count == 0 || dest.Empty())
            {
                // @todo Send to mailbox if no space
                SendSysMessage("You don't have any space in your bags.");
                return false;
            }

            Item item = StoreNewItem(dest, itemId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(itemId));
            if (item != null)
                SendNewItem(item, count, true, false);
            else
                return false;
            return true;
        }
        public void RemoveItem(byte bag, byte slot, bool update)
        {
            // note: removeitem does not actually change the item
            // it only takes the item out of storage temporarily
            // note2: if removeitem is to be used for delinking
            // the item must be removed from the player's updatequeue

            Item pItem = GetItemByPos(bag, slot);
            if (pItem != null)
            {
                Log.outDebug(LogFilter.Player, "STORAGE: RemoveItem bag = {0}, slot = {1}, item = {2}", bag, slot, pItem.GetEntry());

                RemoveEnchantmentDurations(pItem);
                RemoveItemDurations(pItem);
                RemoveTradeableItem(pItem);

                if (bag == InventorySlots.Bag0)
                {
                    if (slot < InventorySlots.ReagentBagEnd)
                    {
                        // item set bonuses applied only at equip and removed at unequip, and still active for broken items
                        ItemTemplate pProto = pItem.GetTemplate();
                        if (pProto != null && pProto.GetItemSet() != 0)
                            Item.RemoveItemsSetItem(this, pItem);

                        _ApplyItemMods(pItem, slot, false, update);

                        pItem.RemoveItemZoneFlag(ItemZoneFlags.Equipped);

                        // remove item dependent auras and casts (only weapon and armor slots)
                        if (slot < ProfessionSlots.End)
                        {
                            // update expertise
                            if (slot == EquipmentSlot.MainHand)
                            {
                                // clear main hand only enchantments
                                for (EnchantmentSlot enchantSlot = 0; enchantSlot < EnchantmentSlot.Max; ++enchantSlot)
                                {
                                    var enchantment = CliDB.SpellItemEnchantmentStorage.LookupByKey(pItem.GetEnchantmentId(enchantSlot));
                                    if (enchantment != null && enchantment.HasFlag(SpellItemEnchantmentFlags.MainhandOnly))
                                        pItem.ClearEnchantment(enchantSlot);
                                }

                                UpdateExpertise(WeaponAttackType.BaseAttack);
                            }
                            else if (slot == EquipmentSlot.OffHand)
                                UpdateExpertise(WeaponAttackType.OffAttack);
                            // update armor penetration - passive auras may need it
                            switch (slot)
                            {
                                case EquipmentSlot.MainHand:
                                case EquipmentSlot.OffHand:
                                    RecalculateRating(CombatRating.ArmorPenetration);
                                    break;
                            }
                        }
                    }

                    m_items[slot] = null;
                    SetInvSlot(slot, ObjectGuid.Empty);

                    if (slot < EquipmentSlot.End)
                    {
                        SetVisibleItemSlot(slot, null);
                        if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand)
                            CheckTitanGripPenalty();
                    }
                }
                Bag pBag = GetBagByPos(bag);
                if (pBag != null)
                    pBag.RemoveItem(slot, update);

                pItem.SetContainedIn(ObjectGuid.Empty);
                pItem.SetSlot(ItemConst.NullSlot);
                if (IsInWorld && update)
                    pItem.SendUpdateToPlayer(this);

                AutoUnequipChildItem(pItem);

                if (bag == InventorySlots.Bag0)
                    UpdateAverageItemLevelEquipped();
            }
        }
        public void SplitItem(ushort src, ushort dst, uint count)
        {
            byte srcbag = (byte)(src >> 8);
            byte srcslot = (byte)(src & 255);

            byte dstbag = (byte)(dst >> 8);
            byte dstslot = (byte)(dst & 255);

            Item pSrcItem = GetItemByPos(srcbag, srcslot);
            if (pSrcItem == null)
            {
                SendEquipError(InventoryResult.ItemNotFound, pSrcItem);
                return;
            }

            if (pSrcItem.m_lootGenerated)                           // prevent split looting item (item
            {
                //best error message found for attempting to split while looting
                SendEquipError(InventoryResult.SplitFailed, pSrcItem);
                return;
            }

            // not let split all items (can be only at cheating)
            if (pSrcItem.GetCount() == count)
            {
                SendEquipError(InventoryResult.SplitFailed, pSrcItem);
                return;
            }

            // not let split more existed items (can be only at cheating)
            if (pSrcItem.GetCount() < count)
            {
                SendEquipError(InventoryResult.TooFewToSplit, pSrcItem);
                return;
            }

            //! If trading
            TradeData tradeData = GetTradeData();
            if (tradeData != null)
            {
                //! If current item is in trade window (only possible with packet spoofing - silent return)
                if (tradeData.GetTradeSlotForItem(pSrcItem.GetGUID()) != TradeSlots.Invalid)
                    return;
            }

            Log.outDebug(LogFilter.Player, "STORAGE: SplitItem bag = {0}, slot = {1}, item = {2}, count = {3}", dstbag, dstslot, pSrcItem.GetEntry(), count);
            Item pNewItem = pSrcItem.CloneItem(count, this);
            if (pNewItem == null)
            {
                SendEquipError(InventoryResult.ItemNotFound, pSrcItem);
                return;
            }

            if (IsInventoryPos(dst))
            {
                // change item amount before check (for unique max count check)
                pSrcItem.SetCount(pSrcItem.GetCount() - count);

                List<ItemPosCount> dest = new();
                InventoryResult msg = CanStoreItem(dstbag, dstslot, dest, pNewItem, false);
                if (msg != InventoryResult.Ok)
                {
                    pSrcItem.SetCount(pSrcItem.GetCount() + count);
                    SendEquipError(msg, pSrcItem);
                    return;
                }

                if (IsInWorld)
                    pSrcItem.SendUpdateToPlayer(this);
                pSrcItem.SetState(ItemUpdateState.Changed, this);
                StoreItem(dest, pNewItem, true);
            }
            else if (IsBankPos(dst))
            {
                // change item amount before check (for unique max count check)
                pSrcItem.SetCount(pSrcItem.GetCount() - count);

                List<ItemPosCount> dest = new();
                InventoryResult msg = CanBankItem(dstbag, dstslot, dest, pNewItem, false);
                if (msg != InventoryResult.Ok)
                {
                    pSrcItem.SetCount(pSrcItem.GetCount() + count);
                    SendEquipError(msg, pSrcItem);
                    return;
                }

                if (IsInWorld)
                    pSrcItem.SendUpdateToPlayer(this);
                pSrcItem.SetState(ItemUpdateState.Changed, this);
                BankItem(dest, pNewItem, true);
            }
            else if (IsEquipmentPos(dst))
            {
                // change item amount before check (for unique max count check), provide space for splitted items
                pSrcItem.SetCount(pSrcItem.GetCount() - count);

                ushort dest;
                InventoryResult msg = CanEquipItem(dstslot, out dest, pNewItem, false);
                if (msg != InventoryResult.Ok)
                {
                    pSrcItem.SetCount(pSrcItem.GetCount() + count);
                    SendEquipError(msg, pSrcItem);
                    return;
                }

                if (IsInWorld)
                    pSrcItem.SendUpdateToPlayer(this);
                pSrcItem.SetState(ItemUpdateState.Changed, this);
                EquipItem(dest, pNewItem, true);
                AutoUnequipOffhandIfNeed();
            }
        }
        public void SwapItem(ushort src, ushort dst)
        {
            byte srcbag = (byte)(src >> 8);
            byte srcslot = (byte)(src & 255);

            byte dstbag = (byte)(dst >> 8);
            byte dstslot = (byte)(dst & 255);

            Item pSrcItem = GetItemByPos(srcbag, srcslot);
            Item pDstItem = GetItemByPos(dstbag, dstslot);

            if (pSrcItem == null)
                return;

            if (pSrcItem.HasItemFlag(ItemFieldFlags.Child))
            {
                Item parentItem = GetItemByGuid(pSrcItem.m_itemData.Creator);
                if (parentItem != null)
                {
                    if (IsEquipmentPos(src))
                    {
                        AutoUnequipChildItem(parentItem);   // we need to unequip child first since it cannot go into whatever is going to happen next
                        SwapItem(dst, src);                 // src is now empty
                        SwapItem(parentItem.GetPos(), dst);// dst is now empty
                        return;
                    }
                }
            }
            else if (pDstItem != null && pDstItem.HasItemFlag(ItemFieldFlags.Child))
            {
                Item parentItem = GetItemByGuid(pDstItem.m_itemData.Creator);
                if (parentItem != null)
                {
                    if (IsEquipmentPos(dst))
                    {
                        AutoUnequipChildItem(parentItem);   // we need to unequip child first since it cannot go into whatever is going to happen next
                        SwapItem(src, dst);                 // dst is now empty
                        SwapItem(parentItem.GetPos(), src);// src is now empty
                        return;
                    }
                }
            }

            Log.outDebug(LogFilter.Player, "STORAGE: SwapItem bag = {0}, slot = {1}, item = {2}", dstbag, dstslot, pSrcItem.GetEntry());

            if (!IsAlive())
            {
                SendEquipError(InventoryResult.PlayerDead, pSrcItem, pDstItem);
                return;
            }

            // SRC checks

            // check unequip potability for equipped items and bank bags
            if (IsEquipmentPos(src) || IsBagPos(src))
            {
                // bags can be swapped with empty bag slots, or with empty bag (items move possibility checked later)
                InventoryResult msg = CanUnequipItem(src, !IsBagPos(src) || IsBagPos(dst) || (pDstItem != null && pDstItem.ToBag() != null && pDstItem.ToBag().IsEmpty()));
                if (msg != InventoryResult.Ok)
                {
                    SendEquipError(msg, pSrcItem, pDstItem);
                    return;
                }
            }

            // prevent put equipped/bank bag in self
            if (IsBagPos(src) && srcslot == dstbag)
            {
                SendEquipError(InventoryResult.BagInBag, pSrcItem, pDstItem);
                return;
            }

            // prevent equipping bag in the same slot from its inside
            if (IsBagPos(dst) && srcbag == dstslot)
            {
                SendEquipError(InventoryResult.CantSwap, pSrcItem, pDstItem);
                return;
            }

            // DST checks
            if (pDstItem != null)
            {
                // check unequip potability for equipped items and bank bags
                if (IsEquipmentPos(dst) || IsBagPos(dst))
                {
                    // bags can be swapped with empty bag slots, or with empty bag (items move possibility checked later)
                    InventoryResult msg = CanUnequipItem(dst, !IsBagPos(dst) || IsBagPos(src) || (pSrcItem.ToBag() != null && pSrcItem.ToBag().IsEmpty()));
                    if (msg != InventoryResult.Ok)
                    {
                        SendEquipError(msg, pSrcItem, pDstItem);
                        return;
                    }
                }
            }

            // NOW this is or item move (swap with empty), or swap with another item (including bags in bag possitions)
            // or swap empty bag with another empty or not empty bag (with items exchange)

            // Move case
            if (pDstItem == null)
            {
                if (IsInventoryPos(dst))
                {
                    List<ItemPosCount> dest = new();
                    InventoryResult msg = CanStoreItem(dstbag, dstslot, dest, pSrcItem, false);
                    if (msg != InventoryResult.Ok)
                    {
                        SendEquipError(msg, pSrcItem);
                        return;
                    }

                    RemoveItem(srcbag, srcslot, true);
                    StoreItem(dest, pSrcItem, true);
                    if (IsBankPos(src))
                        ItemAddedQuestCheck(pSrcItem.GetEntry(), pSrcItem.GetCount());
                }
                else if (IsBankPos(dst))
                {
                    List<ItemPosCount> dest = new();
                    InventoryResult msg = CanBankItem(dstbag, dstslot, dest, pSrcItem, false);
                    if (msg != InventoryResult.Ok)
                    {
                        SendEquipError(msg, pSrcItem);
                        return;
                    }

                    RemoveItem(srcbag, srcslot, true);
                    BankItem(dest, pSrcItem, true);
                    ItemRemovedQuestCheck(pSrcItem.GetEntry(), pSrcItem.GetCount());
                }
                else if (IsEquipmentPos(dst))
                {
                    ushort _dest;
                    InventoryResult msg = CanEquipItem(dstslot, out _dest, pSrcItem, false);
                    if (msg != InventoryResult.Ok)
                    {
                        SendEquipError(msg, pSrcItem);
                        return;
                    }

                    RemoveItem(srcbag, srcslot, true);
                    EquipItem(_dest, pSrcItem, true);
                    AutoUnequipOffhandIfNeed();
                }

                return;
            }

            // attempt merge to / fill target item
            if (!pSrcItem.IsBag() && !pDstItem.IsBag())
            {
                InventoryResult msg;
                List<ItemPosCount> sDest = new();
                ushort eDest = 0;
                if (IsInventoryPos(dst))
                    msg = CanStoreItem(dstbag, dstslot, sDest, pSrcItem, false);
                else if (IsBankPos(dst))
                    msg = CanBankItem(dstbag, dstslot, sDest, pSrcItem, false);
                else if (IsEquipmentPos(dst))
                    msg = CanEquipItem(dstslot, out eDest, pSrcItem, false);
                else
                    return;

                if (msg == InventoryResult.Ok && IsEquipmentPos(dst) && !pSrcItem.GetChildItem().IsEmpty())
                    msg = CanEquipChildItem(pSrcItem);

                // can be merge/fill
                if (msg == InventoryResult.Ok)
                {
                    if (pSrcItem.GetCount() + pDstItem.GetCount() <= pSrcItem.GetTemplate().GetMaxStackSize())
                    {
                        RemoveItem(srcbag, srcslot, true);

                        if (IsInventoryPos(dst))
                            StoreItem(sDest, pSrcItem, true);
                        else if (IsBankPos(dst))
                            BankItem(sDest, pSrcItem, true);
                        else if (IsEquipmentPos(dst))
                        {
                            EquipItem(eDest, pSrcItem, true);
                            if (!pSrcItem.GetChildItem().IsEmpty())
                                EquipChildItem(srcbag, srcslot, pSrcItem);

                            AutoUnequipOffhandIfNeed();
                        }
                    }
                    else
                    {
                        pSrcItem.SetCount(pSrcItem.GetCount() + pDstItem.GetCount() - pSrcItem.GetTemplate().GetMaxStackSize());
                        pDstItem.SetCount(pSrcItem.GetTemplate().GetMaxStackSize());
                        pSrcItem.SetState(ItemUpdateState.Changed, this);
                        pDstItem.SetState(ItemUpdateState.Changed, this);
                        if (IsInWorld)
                        {
                            pSrcItem.SendUpdateToPlayer(this);
                            pDstItem.SendUpdateToPlayer(this);
                        }
                    }
                    SendRefundInfo(pDstItem);
                    return;
                }
            }

            // impossible merge/fill, do real swap
            InventoryResult _msg = InventoryResult.Ok;

            // check src.dest move possibility
            List<ItemPosCount> _sDest = new();
            ushort _eDest = 0;
            if (IsInventoryPos(dst))
                _msg = CanStoreItem(dstbag, dstslot, _sDest, pSrcItem, true);
            else if (IsBankPos(dst))
                _msg = CanBankItem(dstbag, dstslot, _sDest, pSrcItem, true);
            else if (IsEquipmentPos(dst))
            {
                _msg = CanEquipItem(dstslot, out _eDest, pSrcItem, true);
                if (_msg == InventoryResult.Ok)
                    _msg = CanUnequipItem(_eDest, true);
            }

            if (_msg != InventoryResult.Ok)
            {
                SendEquipError(_msg, pSrcItem, pDstItem);
                return;
            }

            // check dest.src move possibility
            List<ItemPosCount> sDest2 = new();
            ushort eDest2 = 0;
            if (IsInventoryPos(src))
                _msg = CanStoreItem(srcbag, srcslot, sDest2, pDstItem, true);
            else if (IsBankPos(src))
                _msg = CanBankItem(srcbag, srcslot, sDest2, pDstItem, true);
            else if (IsEquipmentPos(src))
            {
                _msg = CanEquipItem(srcslot, out eDest2, pDstItem, true);
                if (_msg == InventoryResult.Ok)
                    _msg = CanUnequipItem(eDest2, true);
            }

            if (_msg == InventoryResult.Ok && IsEquipmentPos(dst) && !pSrcItem.GetChildItem().IsEmpty())
                _msg = CanEquipChildItem(pSrcItem);

            if (_msg != InventoryResult.Ok)
            {
                SendEquipError(_msg, pDstItem, pSrcItem);
                return;
            }

            // Check bag swap with item exchange (one from empty in not bag possition (equipped (not possible in fact) or store)
            Bag srcBag = pSrcItem.ToBag();
            if (srcBag != null)
            {
                Bag dstBag = pDstItem.ToBag();
                if (dstBag != null)
                {
                    Bag emptyBag = null;
                    Bag fullBag = null;
                    if (srcBag.IsEmpty() && !IsBagPos(src))
                    {
                        emptyBag = srcBag;
                        fullBag = dstBag;
                    }
                    else if (dstBag.IsEmpty() && !IsBagPos(dst))
                    {
                        emptyBag = dstBag;
                        fullBag = srcBag;
                    }

                    // bag swap (with items exchange) case
                    if (emptyBag != null && fullBag != null)
                    {
                        ItemTemplate emptyProto = emptyBag.GetTemplate();
                        byte count = 0;

                        for (byte i = 0; i < fullBag.GetBagSize(); ++i)
                        {
                            Item bagItem = fullBag.GetItemByPos(i);
                            if (bagItem == null)
                                continue;

                            ItemTemplate bagItemProto = bagItem.GetTemplate();
                            if (bagItemProto == null || !Item.ItemCanGoIntoBag(bagItemProto, emptyProto))
                            {
                                // one from items not go to empty target bag
                                SendEquipError(InventoryResult.BagInBag, pSrcItem, pDstItem);
                                return;
                            }

                            ++count;
                        }

                        if (count > emptyBag.GetBagSize())
                        {
                            // too small targeted bag
                            SendEquipError(InventoryResult.CantSwap, pSrcItem, pDstItem);
                            return;
                        }

                        // Items swap
                        count = 0;                                      // will pos in new bag
                        for (byte i = 0; i < fullBag.GetBagSize(); ++i)
                        {
                            Item bagItem = fullBag.GetItemByPos(i);
                            if (bagItem == null)
                                continue;

                            fullBag.RemoveItem(i, true);
                            emptyBag.StoreItem(count, bagItem, true);
                            bagItem.SetState(ItemUpdateState.Changed, this);

                            ++count;
                        }
                    }
                }
            }

            // now do moves, remove...
            RemoveItem(dstbag, dstslot, false);
            RemoveItem(srcbag, srcslot, false);

            // add to dest
            if (IsInventoryPos(dst))
                StoreItem(_sDest, pSrcItem, true);
            else if (IsBankPos(dst))
                BankItem(_sDest, pSrcItem, true);
            else if (IsEquipmentPos(dst))
            {
                EquipItem(_eDest, pSrcItem, true);
                if (!pSrcItem.GetChildItem().IsEmpty())
                    EquipChildItem(srcbag, srcslot, pSrcItem);
            }

            // add to src
            if (IsInventoryPos(src))
                StoreItem(sDest2, pDstItem, true);
            else if (IsBankPos(src))
                BankItem(sDest2, pDstItem, true);
            else if (IsEquipmentPos(src))
                EquipItem(eDest2, pDstItem, true);

            // if inventory item was moved, check if we can remove dependent auras, because they were not removed in Player::RemoveItem (update was set to false)
            // do this after swaps are done, we pass nullptr because both weapons could be swapped and none of them should be ignored
            if ((srcbag == InventorySlots.Bag0 && srcslot < InventorySlots.ReagentBagEnd) || (dstbag == InventorySlots.Bag0 && dstslot < InventorySlots.ReagentBagEnd))
                ApplyItemDependentAuras(null, false);

            // if player is moving bags and is looting an item inside this bag
            // release the loot
            if (!GetAELootView().Empty())
            {
                bool released = false;
                if (IsBagPos(src))
                {
                    Bag bag = pSrcItem.ToBag();
                    for (byte i = 0; i < bag.GetBagSize(); ++i)
                    {
                        Item bagItem = bag.GetItemByPos(i);
                        if (bagItem != null)
                        {
                            if (GetLootByWorldObjectGUID(bagItem.GetGUID()) != null)
                            {
                                GetSession().DoLootReleaseAll();
                                released = true;                    // so we don't need to look at dstBag
                                break;
                            }
                        }
                    }
                }

                if (!released && IsBagPos(dst))
                {
                    Bag bag = pDstItem.ToBag();
                    for (byte i = 0; i < bag.GetBagSize(); ++i)
                    {
                        Item bagItem = bag.GetItemByPos(i);
                        if (bagItem != null)
                        {
                            if (GetLootByWorldObjectGUID(bagItem.GetGUID()) != null)
                            {
                                GetSession().DoLootReleaseAll();
                                break;
                            }
                        }
                    }
                }
            }
            AutoUnequipOffhandIfNeed();
        }
        bool _StoreOrEquipNewItem(uint vendorslot, uint item, byte count, byte bag, byte slot, long price, ItemTemplate pProto, Creature pVendor, VendorItem crItem, bool bStore)
        {
            uint stacks = count / pProto.GetBuyCount();
            List<ItemPosCount> vDest = new();
            ushort uiDest = 0;
            InventoryResult msg = bStore ? CanStoreNewItem(bag, slot, vDest, item, count) : CanEquipNewItem(slot, out uiDest, item, false);
            if (msg != InventoryResult.Ok)
            {
                SendEquipError(msg, null, null, item);
                return false;
            }

            ModifyMoney(-price);

            if (crItem.ExtendedCost != 0) // case for new honor system
            {
                var iece = CliDB.ItemExtendedCostStorage.LookupByKey(crItem.ExtendedCost);
                for (int i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
                {
                    if (iece.ItemID[i] != 0)
                        DestroyItemCount(iece.ItemID[i], iece.ItemCount[i] * stacks, true);
                }

                for (int i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
                {
                    if (iece.Flags.HasAnyFlag((byte)((int)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
                        continue;

                    if (iece.CurrencyID[i] != 0)
                        RemoveCurrency(iece.CurrencyID[i], (int)(iece.CurrencyCount[i] * stacks), CurrencyDestroyReason.Vendor);
                }
            }

            Item it = bStore ? StoreNewItem(vDest, item, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(item), null, ItemContext.Vendor, crItem.BonusListIDs, false) : EquipNewItem(uiDest, item, ItemContext.Vendor, true);
            if (it != null)
            {
                uint new_count = pVendor.UpdateVendorItemCurrentCount(crItem, count);

                BuySucceeded packet = new();
                packet.VendorGUID = pVendor.GetGUID();
                packet.Muid = vendorslot + 1;
                packet.NewQuantity = crItem.maxcount > 0 ? new_count : 0xFFFFFFFF;
                packet.QuantityBought = count;
                SendPacket(packet);

                SendNewItem(it, count, true, false, false);

                if (!bStore)
                    AutoUnequipOffhandIfNeed();

                if (pProto.HasFlag(ItemFlags.ItemPurchaseRecord) && crItem.ExtendedCost != 0 && pProto.GetMaxStackSize() == 1)
                {
                    it.SetItemFlag(ItemFieldFlags.Refundable);
                    it.SetRefundRecipient(GetGUID());
                    it.SetPaidMoney((uint)price);
                    it.SetPaidExtendedCost(crItem.ExtendedCost);
                    it.SaveRefundDataToDB();
                    AddRefundReference(it.GetGUID());
                }

                GetSession().GetCollectionMgr().OnItemAdded(it);
            }
            return true;
        }

        void SendItemPassives()
        {
            if (m_itemPassives.Empty())
                return;

            SendItemPassives sendItemPassives = new();
            sendItemPassives.SpellID.AddRange(m_itemPassives);
            SendPacket(sendItemPassives);
        }

        public void SendNewItem(Item item, uint quantity, bool pushed, bool created, bool broadcast = false, uint dungeonEncounterId = 0)
        {
            if (item == null) // prevent crash
                return;

            ItemPushResult packet = new();

            packet.PlayerGUID = GetGUID();

            packet.Slot = item.GetBagSlot();
            packet.SlotInBag = item.GetCount() == quantity ? item.GetSlot() : -1;

            packet.Item = new ItemInstance(item);

            packet.ProxyItemID = item.GetTemplate().QuestLogItemId;
            packet.Quantity = quantity;
            packet.QuantityInInventory = GetItemCount(item.GetEntry());

            QuestObjective questObjective = GetQuestObjectiveForItem(item.GetEntry(), false);
            if (questObjective != null)
                packet.QuantityInQuestLog = GetQuestObjectiveData(questObjective);

            packet.BattlePetSpeciesID = (int)item.GetModifier(ItemModifier.BattlePetSpeciesId);
            packet.BattlePetBreedID = (int)item.GetModifier(ItemModifier.BattlePetBreedData) & 0xFFFFFF;
            packet.BattlePetBreedQuality = (byte)((item.GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF);
            packet.BattlePetLevel = (int)item.GetModifier(ItemModifier.BattlePetLevel);

            packet.ItemGUID = item.GetGUID();

            packet.Pushed = pushed;
            packet.ChatNotifyType = ItemPushResult.DisplayType.Normal;
            packet.Created = created;
            //packet.IsBonusRoll;

            if (dungeonEncounterId != 0)
            {
                packet.ChatNotifyType = ItemPushResult.DisplayType.EncounterLoot;
                packet.EncounterID = (int)dungeonEncounterId;
                packet.IsPersonalLoot = true;
            }

            if (broadcast && GetGroup() != null && !item.GetTemplate().HasFlag(ItemFlags3.DontReportLootLogToParty))
                GetGroup().BroadcastPacket(packet, true);
            else
                SendPacket(packet);
        }

        //Item Durations
        void RemoveItemDurations(Item item)
        {
            m_itemDuration.Remove(item);
        }
        void AddItemDurations(Item item)
        {
            if (item.m_itemData.Expiration != 0)
            {
                m_itemDuration.Add(item);
                item.SendTimeUpdate(this);
            }
        }
        void UpdateItemDuration(uint time, bool realtimeonly = false)
        {
            if (m_itemDuration.Empty())
                return;

            Log.outDebug(LogFilter.Player, "Player:UpdateItemDuration({0}, {1})", time, realtimeonly);

            foreach (var item in m_itemDuration)
            {
                if (!realtimeonly || item.GetTemplate().HasFlag(ItemFlags.RealDuration))
                    item.UpdateDuration(this, time);
            }
        }
        void SendEnchantmentDurations()
        {
            foreach (var enchantDuration in m_enchantDuration)
                GetSession().SendItemEnchantTimeUpdate(GetGUID(), enchantDuration.item.GetGUID(), (uint)enchantDuration.slot, enchantDuration.leftduration / 1000);
        }
        void SendItemDurations()
        {
            foreach (var item in m_itemDuration)
                item.SendTimeUpdate(this);
        }

        public void ToggleMetaGemsActive(uint exceptslot, bool apply)
        {
            //cycle all equipped items
            for (byte slot = EquipmentSlot.Start; slot < EquipmentSlot.End; ++slot)
            {
                //enchants for the slot being socketed are handled by WorldSession.HandleSocketOpcode(WorldPacket& recvData)
                if (slot == exceptslot)
                    continue;

                Item pItem = GetItemByPos(InventorySlots.Bag0, slot);

                if (pItem == null || pItem.GetSocketColor(0) == 0)   //if item has no sockets or no item is equipped go to next item
                    continue;

                //cycle all (gem)enchants
                for (EnchantmentSlot enchant_slot = EnchantmentSlot.Sock1; enchant_slot < EnchantmentSlot.Sock1 + 3; ++enchant_slot)
                {
                    uint enchant_id = pItem.GetEnchantmentId(enchant_slot);
                    if (enchant_id == 0)                                 //if no enchant go to next enchant(slot)
                        continue;

                    SpellItemEnchantmentRecord enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                    if (enchantEntry == null)
                        continue;

                    //only metagems to be (de)activated, so only enchants with condition
                    uint condition = enchantEntry.ConditionID;
                    if (condition != 0)
                        ApplyEnchantment(pItem, enchant_slot, apply);
                }
            }
        }

        public float GetAverageItemLevel()
        {
            float sum = 0;
            uint count = 0;

            for (int i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                // don't check tabard, ranged, offhand or shirt
                if (i == EquipmentSlot.Tabard || i == EquipmentSlot.Ranged || i == EquipmentSlot.OffHand || i == EquipmentSlot.Shirt)
                    continue;

                if (m_items[i] != null)
                    sum += m_items[i].GetItemLevel(this);

                ++count;
            }

            return sum / count;
        }

        public List<Item> GetCraftingReagentItemsToDeposit()
        {
            List<Item> itemList = new();
            ForEachItem(ItemSearchLocation.Inventory, item =>
            {
                if (item.GetTemplate().IsCraftingReagent())
                    itemList.Add(item);

                return true;
            });

            return itemList;
        }

        public Item GetItemByGuid(ObjectGuid guid)
        {
            Item result = null;
            ForEachItem(ItemSearchLocation.Everywhere, item =>
            {
                if (item.GetGUID() == guid)
                {
                    result = item;
                    return false;
                }

                return true;
            });

            return result;
        }
        public uint GetItemCount(uint item, bool inBankAlso = false, Item skipItem = null)
        {
            bool countGems = skipItem != null && skipItem.GetTemplate().GetGemProperties() != 0;

            ItemSearchLocation location = ItemSearchLocation.Equipment | ItemSearchLocation.Inventory | ItemSearchLocation.ReagentBank;
            if (inBankAlso)
                location |= ItemSearchLocation.Bank;

            uint count = 0;
            ForEachItem(location, pItem =>
            {
                if (pItem != skipItem)
                {
                    if (pItem.GetEntry() == item)
                        count += pItem.GetCount();

                    if (countGems)
                        count += pItem.GetGemCountWithID(item);
                }
                return true;
            });

            return count;
        }
        public Item GetUseableItemByPos(byte bag, byte slot)
        {
            Item item = GetItemByPos(bag, slot);
            if (item == null)
                return null;

            if (!CanUseAttackType(GetAttackBySlot(slot, item.GetTemplate().GetInventoryType())))
                return null;

            return item;
        }
        public Item GetItemByPos(ushort pos)
        {
            byte bag = (byte)(pos >> 8);
            byte slot = (byte)(pos & 255);

            return GetItemByPos(bag, slot);
        }
        public Item GetItemByPos(byte bag, byte slot)
        {
            if (bag == InventorySlots.Bag0 && slot < (int)PlayerSlots.End && (slot < InventorySlots.BuyBackStart || slot >= InventorySlots.BuyBackEnd))
                return m_items[slot];

            Bag pBag = GetBagByPos(bag);
            if (pBag != null)
                return pBag.GetItemByPos(slot);

            return null;
        }
        public Item GetItemByEntry(uint entry, ItemSearchLocation where = ItemSearchLocation.Default)
        {
            Item result = null;
            ForEachItem(where, item =>
            {
                if (item.GetEntry() == entry)
                {
                    result = item;
                    return false;
                }

                return true;
            });

            return result;
        }
        public List<Item> GetItemListByEntry(uint entry, bool inBankAlso = false)
        {
            ItemSearchLocation location = ItemSearchLocation.Equipment | ItemSearchLocation.Inventory | ItemSearchLocation.ReagentBank;
            if (inBankAlso)
                location |= ItemSearchLocation.Bank;

            List<Item> itemList = new();
            ForEachItem(location, item =>
            {
                if (item.GetEntry() == entry)
                    itemList.Add(item);

                return true;
            });

            return itemList;
        }
        public bool HasItemCount(uint item, uint count = 1, bool inBankAlso = false)
        {
            ItemSearchLocation location = ItemSearchLocation.Equipment | ItemSearchLocation.Inventory | ItemSearchLocation.ReagentBank;
            if (inBankAlso)
                location |= ItemSearchLocation.Bank;

            uint currentCount = 0;
            return !ForEachItem(location, pItem =>
            {
                if (pItem != null && pItem.GetEntry() == item && !pItem.IsInTrade())
                {
                    currentCount += pItem.GetCount();
                    if (currentCount >= count)
                        return false;
                }

                return true;
            });
        }
        public static bool IsChildEquipmentPos(byte bag, byte slot)
        {
            return bag == InventorySlots.Bag0 && (slot >= InventorySlots.ChildEquipmentStart && slot < InventorySlots.ChildEquipmentEnd);
        }
        public bool IsValidPos(byte bag, byte slot, bool explicit_pos)
        {
            // post selected
            if (bag == ItemConst.NullBag && !explicit_pos)
                return true;

            if (bag == InventorySlots.Bag0)
            {
                // any post selected
                if (slot == ItemConst.NullSlot && !explicit_pos)
                    return true;

                // equipment
                if (slot < EquipmentSlot.End)
                    return true;

                // profession equipment
                if (slot >= ProfessionSlots.Start && slot < ProfessionSlots.End)
                    return true;

                // bag equip slots
                if (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd)
                    return true;

                // reagent bag equip slots
                if (slot >= InventorySlots.ReagentBagStart && slot < InventorySlots.ReagentBagEnd)
                    return true;

                // backpack slots
                if (slot >= InventorySlots.ItemStart && slot < InventorySlots.ItemStart + GetInventorySlotCount())
                    return true;

                // bank bag slots
                if (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd)
                    return true;

                return false;
            }

            // bag content slots
            // bank bag content slots
            Bag pBag = GetBagByPos(bag);
            if (pBag != null)
            {
                // any post selected
                if (slot == ItemConst.NullSlot && !explicit_pos)
                    return true;

                return slot < pBag.GetBagSize();
            }

            // where this?
            return false;
        }

        public Item GetChildItemByGuid(ObjectGuid guid)
        {
            Item result = null;
            ForEachItem(ItemSearchLocation.Equipment | ItemSearchLocation.Inventory, item =>
            {
                if (item.GetGUID() == guid)
                {
                    result = item;
                    return false;
                }

                return true;
            });

            return result;
        }
        uint GetItemCountWithLimitCategory(uint limitCategory, Item skipItem)
        {
            uint count = 0;
            ForEachItem(ItemSearchLocation.Everywhere, item =>
            {
                if (item != skipItem)
                    if (item.GetItemLimitCategory() == limitCategory)
                        count += item.GetCount();
                return true;
            });

            return count;
        }
        public byte GetItemLimitCategoryQuantity(ItemLimitCategoryRecord limitEntry)
        {
            byte limit = limitEntry.Quantity;

            var limitConditions = Global.DB2Mgr.GetItemLimitCategoryConditions(limitEntry.Id);
            foreach (ItemLimitCategoryConditionRecord limitCondition in limitConditions)
            {
                if (ConditionManager.IsPlayerMeetingCondition(this, limitCondition.PlayerConditionID))
                    limit += (byte)limitCondition.AddQuantity;
            }

            return limit;
        }

        public void DestroyConjuredItems(bool update)
        {
            // used when entering arena
            // destroys all conjured items
            Log.outDebug(LogFilter.Player, "STORAGE: DestroyConjuredItems");

            // in inventory
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                {
                    if (pItem.IsConjuredConsumable())
                        DestroyItem(InventorySlots.Bag0, i, update);
                }
            }

            // in inventory bags
            for (byte i = InventorySlots.BagStart; i < InventorySlots.ReagentBagEnd; i++)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                {
                    for (byte j = 0; j < pBag.GetBagSize(); j++)
                    {
                        Item pItem = pBag.GetItemByPos(j);
                        if (pItem != null)
                            if (pItem.IsConjuredConsumable())
                                DestroyItem(i, j, update);
                    }
                }
            }

            // in equipment and bag list
            for (byte i = EquipmentSlot.Start; i < InventorySlots.ReagentBagEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    if (pItem.IsConjuredConsumable())
                        DestroyItem(InventorySlots.Bag0, i, update);
            }
        }
        void DestroyZoneLimitedItem(bool update, uint new_zone)
        {
            Log.outDebug(LogFilter.Player, "STORAGE: DestroyZoneLimitedItem in map {0} and area {1}", GetMapId(), new_zone);

            // in inventory
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    if (pItem.IsLimitedToAnotherMapOrZone(GetMapId(), new_zone))
                        DestroyItem(InventorySlots.Bag0, i, update);
            }

            // in inventory bags
            for (byte i = InventorySlots.BagStart; i < InventorySlots.ReagentBagEnd; i++)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                {
                    for (byte j = 0; j < pBag.GetBagSize(); j++)
                    {
                        Item pItem = pBag.GetItemByPos(j);
                        if (pItem != null)
                            if (pItem.IsLimitedToAnotherMapOrZone(GetMapId(), new_zone))
                                DestroyItem(i, j, update);
                    }
                }
            }

            // in equipment and bag list
            for (byte i = EquipmentSlot.Start; i < InventorySlots.ReagentBagEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    if (pItem.IsLimitedToAnotherMapOrZone(GetMapId(), new_zone))
                        DestroyItem(InventorySlots.Bag0, i, update);
            }
        }

        public InventoryResult CanRollNeedForItem(ItemTemplate proto, Map map, bool restrictOnlyLfg)
        {
            if (restrictOnlyLfg)
            {
                if (GetGroup() == null || !GetGroup().IsLFGGroup())
                    return InventoryResult.Ok;    // not in LFG group

                // check if looted object is inside the lfg dungeon
                if (!Global.LFGMgr.InLfgDungeonMap(GetGroup().GetGUID(), map.GetId(), map.GetDifficultyID()))
                    return InventoryResult.Ok;
            }

            if (proto == null)
                return InventoryResult.ItemNotFound;

            // Used by group, function GroupLoot, to know if a prototype can be used by a player
            if ((proto.GetAllowableClass() & GetClassMask()) == 0 || !proto.GetAllowableRace().HasRace(GetRace()))
                return InventoryResult.CantEquipEver;

            if (proto.GetRequiredSpell() != 0 && !HasSpell(proto.GetRequiredSpell()))
                return InventoryResult.ProficiencyNeeded;

            if (proto.GetRequiredSkill() != 0)
            {
                if (GetSkillValue((SkillType)proto.GetRequiredSkill()) == 0)
                    return InventoryResult.ProficiencyNeeded;
                else if (GetSkillValue((SkillType)proto.GetRequiredSkill()) < proto.GetRequiredSkillRank())
                    return InventoryResult.CantEquipSkill;
            }

            if (proto.GetClass() == ItemClass.Weapon && GetSkillValue(proto.GetSkill()) == 0)
                return InventoryResult.ProficiencyNeeded;

            if (proto.GetClass() == ItemClass.Armor && proto.GetInventoryType() != InventoryType.Cloak)
            {
                ChrClassesRecord classesEntry = CliDB.ChrClassesStorage.LookupByKey(GetClass());
                if ((classesEntry.ArmorTypeMask & 1 << (int)proto.GetSubClass()) == 0)
                    return InventoryResult.ClientLockedOut;
            }

            return InventoryResult.Ok;
        }

        public void AddItemToBuyBackSlot(Item pItem)
        {
            if (pItem != null)
            {
                uint slot = m_currentBuybackSlot;
                // if current back slot non-empty search oldest or free
                if (m_items[slot] != null)
                {
                    long oldest_time = m_activePlayerData.BuybackTimestamp[0];
                    uint oldest_slot = InventorySlots.BuyBackStart;

                    for (byte i = InventorySlots.BuyBackStart + 1; i < InventorySlots.BuyBackEnd; ++i)
                    {
                        // found empty
                        if (m_items[i] == null)
                        {
                            oldest_slot = i;
                            break;
                        }

                        long i_time = m_activePlayerData.BuybackTimestamp[i - InventorySlots.BuyBackStart];
                        if (oldest_time > i_time)
                        {
                            oldest_time = i_time;
                            oldest_slot = i;
                        }
                    }

                    // find oldest
                    slot = oldest_slot;
                }

                RemoveItemFromBuyBackSlot(slot, true);
                Log.outDebug(LogFilter.Player, "STORAGE: AddItemToBuyBackSlot item = {0}, slot = {1}", pItem.GetEntry(), slot);

                m_items[slot] = pItem;
                var time = GameTime.GetGameTime();
                uint etime = (uint)(time - m_logintime + (30 * 3600));
                uint eslot = slot - InventorySlots.BuyBackStart;

                SetInvSlot(slot, pItem.GetGUID());
                SetBuybackPrice(eslot, pItem.GetSellPrice(this) * pItem.GetCount());

                SetBuybackTimestamp(eslot, etime);

                // move to next (for non filled list is move most optimized choice)
                if (m_currentBuybackSlot < InventorySlots.BuyBackEnd - 1)
                    ++m_currentBuybackSlot;
            }
        }

        public bool BuyCurrencyFromVendorSlot(ObjectGuid vendorGuid, uint vendorSlot, uint currency, uint count)
        {
            // cheating attempt
            if (count < 1)
                count = 1;

            if (!IsAlive())
                return false;

            CurrencyTypesRecord proto = CliDB.CurrencyTypesStorage.LookupByKey(currency);
            if (proto == null)
            {
                SendBuyError(BuyResult.CantFindItem, null, currency);
                return false;
            }

            Creature creature = GetNPCIfCanInteractWith(vendorGuid, NPCFlags.Vendor, NPCFlags2.None);
            if (creature == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: BuyCurrencyFromVendorSlot - {0} not found or you can't interact with him.", vendorGuid.ToString());
                SendBuyError(BuyResult.DistanceTooFar, null, currency);
                return false;
            }

            VendorItemData vItems = creature.GetVendorItems();
            if (vItems == null || vItems.Empty())
            {
                SendBuyError(BuyResult.CantFindItem, creature, currency);
                return false;
            }

            if (vendorSlot >= vItems.GetItemCount())
            {
                SendBuyError(BuyResult.CantFindItem, creature, currency);
                return false;
            }

            VendorItem crItem = vItems.GetItem(vendorSlot);
            // store diff item (cheating)
            if (crItem == null || crItem.item != currency || crItem.Type != ItemVendorType.Currency)
            {
                SendBuyError(BuyResult.CantFindItem, creature, currency);
                return false;
            }

            if ((count % crItem.maxcount) != 0)
            {
                SendEquipError(InventoryResult.CantBuyQuantity);
                return false;
            }

            uint stacks = count / crItem.maxcount;
            ItemExtendedCostRecord iece;
            if (crItem.ExtendedCost != 0)
            {
                iece = CliDB.ItemExtendedCostStorage.LookupByKey(crItem.ExtendedCost);
                if (iece == null)
                {
                    Log.outError(LogFilter.Player, "Currency {0} have wrong ExtendedCost field value {1}", currency, crItem.ExtendedCost);
                    return false;
                }

                for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
                {
                    if (iece.ItemID[i] != 0 && !HasItemCount(iece.ItemID[i], (iece.ItemCount[i] * stacks)))
                    {
                        SendEquipError(InventoryResult.VendorMissingTurnins);
                        return false;
                    }
                }

                for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
                {
                    if (iece.CurrencyID[i] == 0)
                        continue;

                    CurrencyTypesRecord entry = CliDB.CurrencyTypesStorage.LookupByKey(iece.CurrencyID[i]);
                    if (entry == null)
                    {
                        SendBuyError(BuyResult.CantFindItem, creature, currency); // Find correct error
                        return false;
                    }

                    if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
                    {
                        // Not implemented
                        SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error
                        return false;
                    }
                    else if (!HasCurrency(iece.CurrencyID[i], (iece.CurrencyCount[i] * stacks)))
                    {
                        SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error
                        return false;
                    }
                }

                // check for personal arena rating requirement
                if (GetMaxPersonalArenaRatingRequirement(iece.ArenaBracket) < iece.RequiredArenaRating)
                {
                    // probably not the proper equip err
                    SendEquipError(InventoryResult.CantEquipRank);
                    return false;
                }

                if (iece.MinFactionID != 0 && (uint)GetReputationRank(iece.MinFactionID) < iece.RequiredAchievement)
                {
                    SendBuyError(BuyResult.ReputationRequire, creature, currency);
                    return false;
                }

                if (iece.Flags.HasAnyFlag((byte)ItemExtendedCostFlags.RequireGuild) && GetGuildId() == 0)
                {
                    SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error
                    return false;
                }

                if (iece.RequiredAchievement != 0 && !HasAchieved(iece.RequiredAchievement))
                {
                    SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error
                    return false;
                }
            }
            else // currencies have no price defined, can only be bought with ExtendedCost
            {
                SendBuyError(BuyResult.CantFindItem, null, currency);
                return false;
            }

            AddCurrency(currency, count, CurrencyGainSource.Vendor);
            if (iece != null)
            {
                for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
                {
                    if (iece.ItemID[i] == 0)
                        continue;

                    DestroyItemCount(iece.ItemID[i], iece.ItemCount[i] * stacks, true);
                }

                for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
                {
                    if (iece.CurrencyID[i] == 0)
                        continue;

                    if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
                        continue;

                    RemoveCurrency(iece.CurrencyID[i], (int)(iece.CurrencyCount[i] * stacks), CurrencyDestroyReason.Vendor);
                }
            }

            return true;
        }

        public bool BuyItemFromVendorSlot(ObjectGuid vendorguid, uint vendorslot, uint item, uint count, byte bag, byte slot)
        {
            // cheating attempt
            if (count < 1)
                count = 1;

            // cheating attempt
            if (slot > ItemConst.MaxBagSize && slot != ItemConst.NullSlot)
                return false;

            if (!IsAlive())
                return false;

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(item);
            if (pProto == null)
            {
                SendBuyError(BuyResult.CantFindItem, null, item);
                return false;
            }

            if (!Convert.ToBoolean(pProto.GetAllowableClass() & GetClassMask()) && pProto.GetBonding() == ItemBondingType.OnAcquire && !IsGameMaster())
            {
                SendBuyError(BuyResult.CantFindItem, null, item);
                return false;
            }

            if (!IsGameMaster() && ((pProto.HasFlag(ItemFlags2.FactionHorde) && GetTeam() == Team.Alliance) || (pProto.HasFlag(ItemFlags2.FactionAlliance) && GetTeam() == Team.Horde)))
                return false;

            Creature creature = GetNPCIfCanInteractWith(vendorguid, NPCFlags.Vendor, NPCFlags2.None);
            if (creature == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: BuyItemFromVendor - {0} not found or you can't interact with him.", vendorguid.ToString());
                SendBuyError(BuyResult.DistanceTooFar, null, item);
                return false;
            }

            if (!Global.ConditionMgr.IsObjectMeetingVendorItemConditions(creature.GetEntry(), item, this, creature))
            {
                Log.outDebug(LogFilter.Condition, "BuyItemFromVendor: conditions not met for creature entry {0} item {1}", creature.GetEntry(), item);
                SendBuyError(BuyResult.CantFindItem, creature, item);
                return false;
            }

            VendorItemData vItems = creature.GetVendorItems();
            if (vItems == null || vItems.Empty())
            {
                SendBuyError(BuyResult.CantFindItem, creature, item);
                return false;
            }

            if (vendorslot >= vItems.GetItemCount())
            {
                SendBuyError(BuyResult.CantFindItem, creature, item);
                return false;
            }

            VendorItem crItem = vItems.GetItem(vendorslot);
            // store diff item (cheating)
            if (crItem == null || crItem.item != item)
            {
                SendBuyError(BuyResult.CantFindItem, creature, item);
                return false;
            }

            if (!ConditionManager.IsPlayerMeetingCondition(this, crItem.PlayerConditionId))
            {
                SendEquipError(InventoryResult.ItemLocked);
                return false;
            }

            // check current item amount if it limited
            if (crItem.maxcount != 0)
            {
                if (creature.GetVendorItemCurrentCount(crItem) < count)
                {
                    SendBuyError(BuyResult.ItemAlreadySold, creature, item);
                    return false;
                }
            }

            if (pProto.GetRequiredReputationFaction() != 0 && ((uint)GetReputationRank(pProto.GetRequiredReputationFaction()) < pProto.GetRequiredReputationRank()))
            {
                SendBuyError(BuyResult.ReputationRequire, creature, item);
                return false;
            }

            if (crItem.ExtendedCost != 0)
            {
                // Can only buy full stacks for extended cost
                if ((count % pProto.GetBuyCount()) != 0)
                {
                    SendEquipError(InventoryResult.CantBuyQuantity);
                    return false;
                }

                uint stacks = count / pProto.GetBuyCount();
                var iece = CliDB.ItemExtendedCostStorage.LookupByKey(crItem.ExtendedCost);
                if (iece == null)
                {
                    Log.outError(LogFilter.Player, "Item {0} have wrong ExtendedCost field value {1}", pProto.GetId(), crItem.ExtendedCost);
                    return false;
                }

                for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
                {
                    if (iece.ItemID[i] != 0 && !HasItemCount(iece.ItemID[i], iece.ItemCount[i] * stacks))
                    {
                        SendEquipError(InventoryResult.VendorMissingTurnins);
                        return false;
                    }
                }

                for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
                {
                    if (iece.CurrencyID[i] == 0)
                        continue;

                    var entry = CliDB.CurrencyTypesStorage.LookupByKey(iece.CurrencyID[i]);
                    if (entry == null)
                    {
                        SendBuyError(BuyResult.CantFindItem, creature, item);
                        return false;
                    }

                    if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
                    {
                        SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error
                        return false;
                    }
                    else if (!HasCurrency(iece.CurrencyID[i], iece.CurrencyCount[i] * stacks))
                    {
                        SendEquipError(InventoryResult.VendorMissingTurnins);
                        return false;
                    }
                }

                // check for personal arena rating requirement
                if (GetMaxPersonalArenaRatingRequirement(iece.ArenaBracket) < iece.RequiredArenaRating)
                {
                    // probably not the proper equip err
                    SendEquipError(InventoryResult.CantEquipRank);
                    return false;
                }

                if (iece.MinFactionID != 0 && (uint)GetReputationRank(iece.MinFactionID) < iece.MinReputation)
                {
                    SendBuyError(BuyResult.ReputationRequire, creature, item);
                    return false;
                }

                if (iece.Flags.HasAnyFlag((byte)ItemExtendedCostFlags.RequireGuild) && GetGuildId() == 0)
                {
                    SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error
                    return false;
                }

                if (iece.RequiredAchievement != 0 && !HasAchieved(iece.RequiredAchievement))
                {
                    SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error
                    return false;
                }
            }

            ulong price = 0;
            if (pProto.GetBuyPrice() > 0) //Assume price cannot be negative (do not know why it is int32)
            {
                float buyPricePerItem = (float)pProto.GetBuyPrice() / pProto.GetBuyCount();
                ulong maxCount = (ulong)(PlayerConst.MaxMoneyAmount / buyPricePerItem);
                if (count > maxCount)
                {
                    Log.outError(LogFilter.Player, "Player {0} tried to buy {1} item id {2}, causing overflow", GetName(), count, pProto.GetId());
                    count = (uint)maxCount;
                }
                price = (ulong)(buyPricePerItem * count); //it should not exceed MAX_MONEY_AMOUNT

                // reputation discount
                price = (ulong)Math.Floor(price * GetReputationPriceDiscount(creature));
                price = pProto.GetBuyPrice() > 0 ? Math.Max(1ul, price) : price;

                int priceMod = GetTotalAuraModifier(AuraType.ModVendorItemsPrices);
                if (priceMod != 0)
                    price -= MathFunctions.CalculatePct(price, priceMod);

                if (!HasEnoughMoney(price))
                {
                    SendBuyError(BuyResult.NotEnoughtMoney, creature, item);
                    return false;
                }
            }

            if ((bag == ItemConst.NullBag && slot == ItemConst.NullSlot) || IsInventoryPos(bag, slot))
            {
                if (!_StoreOrEquipNewItem(vendorslot, item, (byte)count, bag, slot, (int)price, pProto, creature, crItem, true))
                    return false;
            }
            else if (IsEquipmentPos(bag, slot))
            {
                if (count != 1)
                {
                    SendEquipError(InventoryResult.NotEquippable);
                    return false;
                }
                if (!_StoreOrEquipNewItem(vendorslot, item, (byte)count, bag, slot, (int)price, pProto, creature, crItem, false))
                    return false;
            }
            else
            {
                SendEquipError(InventoryResult.WrongSlot);
                return false;
            }

            UpdateCriteria(CriteriaType.BuyItemsFromVendors, 1);

            if (pProto.GetQuality() > ItemQuality.Epic || (pProto.GetQuality() == ItemQuality.Epic && pProto.GetBaseItemLevel() >= GuildConst.MinNewsItemLevel))
            {
                Guild guild = GetGuild();
                if (guild != null)
                    guild.AddGuildNews(GuildNews.ItemPurchased, GetGUID(), 0, item);
            }

            return crItem.maxcount != 0;
        }

        public uint GetMaxPersonalArenaRatingRequirement(uint minarenaslot)
        {
            // returns the maximal personal arena rating that can be used to purchase items requiring this condition
            // so return max[in arenateams](personalrating[teamtype])
            uint max_personal_rating = 0;
            for (byte i = (byte)minarenaslot; i < SharedConst.MaxArenaSlot; ++i)
            {
                uint p_rating = GetArenaPersonalRating(i);
                if (max_personal_rating < p_rating)
                    max_personal_rating = p_rating;
            }
            return max_personal_rating;
        }

        public void SendItemRetrievalMail(uint itemEntry, uint count, ItemContext context)
        {
            MailSender sender = new(MailMessageType.Creature, 34337);
            MailDraft draft = new("Recovered Item", "We recovered a lost item in the twisting nether and noted that it was yours.$B$BPlease find said object enclosed."); // This is the text used in Cataclysm, it probably wasn't changed.
            SQLTransaction trans = new();

            Item item = Item.CreateItem(itemEntry, count, context, null);
            if (item != null)
            {
                item.SaveToDB(trans);
                draft.AddItem(item);
            }

            draft.SendMailTo(trans, new MailReceiver(this, GetGUID().GetCounter()), sender);
            DB.Characters.CommitTransaction(trans);
        }
        public void SetBuybackPrice(uint slot, uint price) { SetUpdateFieldValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BuybackPrice, (int)slot), price); }
        public void SetBuybackTimestamp(uint slot, long timestamp) { SetUpdateFieldValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BuybackTimestamp, (int)slot), timestamp); }

        public Item GetItemFromBuyBackSlot(uint slot)
        {
            Log.outDebug(LogFilter.Player, "STORAGE: GetItemFromBuyBackSlot slot = {0}", slot);
            if (slot >= InventorySlots.BuyBackStart && slot < InventorySlots.BuyBackEnd)
                return m_items[slot];
            return null;
        }
        public void RemoveItemFromBuyBackSlot(uint slot, bool del)
        {
            Log.outDebug(LogFilter.Player, "STORAGE: RemoveItemFromBuyBackSlot slot = {0}", slot);
            if (slot >= InventorySlots.BuyBackStart && slot < InventorySlots.BuyBackEnd)
            {
                Item pItem = m_items[slot];
                if (pItem != null)
                {
                    pItem.RemoveFromWorld();
                    if (del)
                    {
                        ItemTemplate itemTemplate = pItem.GetTemplate();
                        if (itemTemplate != null)
                            if (itemTemplate.HasFlag(ItemFlags.HasLoot))
                                Global.LootItemStorage.RemoveStoredLootForContainer(pItem.GetGUID().GetCounter());

                        pItem.SetState(ItemUpdateState.Removed, this);
                    }
                }

                m_items[slot] = null;

                uint eslot = slot - InventorySlots.BuyBackStart;
                SetInvSlot(slot, ObjectGuid.Empty);
                SetBuybackPrice(eslot, 0);
                SetBuybackTimestamp(eslot, 0);

                // if current backslot is filled set to now free slot
                if (m_items[m_currentBuybackSlot] != null)
                    m_currentBuybackSlot = slot;
            }
        }

        public bool HasItemTotemCategory(uint TotemCategory)
        {
            foreach (AuraEffect providedTotemCategory in GetAuraEffectsByType(AuraType.ProvideTotemCategory))
                if (Global.DB2Mgr.IsTotemCategoryCompatibleWith((uint)providedTotemCategory.GetMiscValueB(), TotemCategory))
                    return true;

            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; ++i)
            {
                Item item = GetUseableItemByPos(InventorySlots.Bag0, i);
                if (item != null && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.GetTemplate().GetTotemCategory(), TotemCategory))
                    return true;
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag bag = GetBagByPos(i);
                if (bag != null)
                {
                    for (byte j = 0; j < bag.GetBagSize(); ++j)
                    {
                        Item item = GetUseableItemByPos(i, j);
                        if (item != null && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.GetTemplate().GetTotemCategory(), TotemCategory))
                            return true;
                    }
                }
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item item = GetUseableItemByPos(InventorySlots.Bag0, i);
                if (item != null && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.GetTemplate().GetTotemCategory(), TotemCategory))
                    return true;
            }

            return false;
        }

        public void _ApplyItemMods(Item item, byte slot, bool apply, bool updateItemAuras = true)
        {
            if (slot >= InventorySlots.ReagentBagEnd || item == null)
                return;

            ItemTemplate proto = item.GetTemplate();

            if (proto == null)
                return;

            // not apply/remove mods for broken item
            if (item.IsBroken())
                return;

            Log.outInfo(LogFilter.Player, "applying mods for item {0} ", item.GetGUID().ToString());

            if (item.GetSocketColor(0) != 0)                              //only (un)equipping of items with sockets can influence metagems, so no need to waste time with normal items
                CorrectMetaGemEnchants(slot, apply);

            _ApplyItemBonuses(item, slot, apply);
            ApplyItemEquipSpell(item, apply);

            if (updateItemAuras)
            {
                ApplyItemDependentAuras(item, apply);
                WeaponAttackType attackType = Player.GetAttackBySlot(slot, item.GetTemplate().GetInventoryType());
                if (attackType != WeaponAttackType.Max)
                    UpdateWeaponDependentAuras(attackType);
            }

            ApplyArtifactPowers(item, apply);
            ApplyAzeritePowers(item, apply);
            ApplyEnchantment(item, apply);

            Log.outDebug(LogFilter.Player, "_ApplyItemMods complete.");
        }

        public void _ApplyItemBonuses(Item item, byte slot, bool apply)
        {
            ItemTemplate proto = item.GetTemplate();
            if (slot >= InventorySlots.ReagentBagEnd || proto == null)
                return;

            uint itemLevel = item.GetItemLevel(this);
            float combatRatingMultiplier = 1.0f;
            GtGenericMultByILvlRecord ratingMult = CliDB.CombatRatingsMultByILvlGameTable.GetRow(itemLevel);
            if (ratingMult != null)
                combatRatingMultiplier = CliDB.GetIlvlStatMultiplier(ratingMult, proto.GetInventoryType());

            for (byte i = 0; i < ItemConst.MaxStats; ++i)
            {
                int statType = item.GetItemStatType(i);
                if (statType == -1)
                    continue;

                float val = item.GetItemStatValue(i, this);
                if (val == 0)
                    continue;

                switch ((ItemModType)statType)
                {
                    case ItemModType.Mana:
                        HandleStatFlatModifier(UnitMods.Mana, UnitModifierFlatType.Base, (float)val, apply);
                        break;
                    case ItemModType.Health:                           // modify HP
                        HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Base, (float)val, apply);
                        break;
                    case ItemModType.Agility:                          // modify agility
                        HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Base, (float)val, apply);
                        UpdateStatBuffMod(Stats.Agility);
                        break;
                    case ItemModType.Strength:                         //modify strength
                        HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Base, (float)val, apply);
                        UpdateStatBuffMod(Stats.Strength);
                        break;
                    case ItemModType.Intellect:                        //modify intellect
                        HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Base, (float)val, apply);
                        UpdateStatBuffMod(Stats.Intellect);
                        break;
                    //case ItemModType.Spirit:                           //modify spirit
                    //HandleStatModifier(UnitMods.StatSpirit, UnitModifierType.BaseValue, (float)val, apply);
                    //ApplyStatBuffMod(Stats.Spirit, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatSpirit, UnitModifierType.BasePCTExcludeCreate)), apply);
                    //break;
                    case ItemModType.Stamina:                          //modify stamina
                        GtGenericMultByILvlRecord staminaMult = CliDB.StaminaMultByILvlGameTable.GetRow(itemLevel);
                        if (staminaMult != null)
                            val = (int)(val * CliDB.GetIlvlStatMultiplier(staminaMult, proto.GetInventoryType()));

                        HandleStatFlatModifier(UnitMods.StatStamina, UnitModifierFlatType.Base, (float)val, apply);
                        UpdateStatBuffMod(Stats.Stamina);
                        break;
                    case ItemModType.DefenseSkillRating:
                        ApplyRatingMod(CombatRating.DefenseSkill, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.DodgeRating:
                        ApplyRatingMod(CombatRating.Dodge, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.ParryRating:
                        ApplyRatingMod(CombatRating.Parry, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.BlockRating:
                        ApplyRatingMod(CombatRating.Block, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.HitMeleeRating:
                        ApplyRatingMod(CombatRating.HitMelee, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.HitRangedRating:
                        ApplyRatingMod(CombatRating.HitRanged, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.HitSpellRating:
                        ApplyRatingMod(CombatRating.HitSpell, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.CritMeleeRating:
                        ApplyRatingMod(CombatRating.CritMelee, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.CritRangedRating:
                        ApplyRatingMod(CombatRating.CritRanged, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.CritSpellRating:
                        ApplyRatingMod(CombatRating.CritSpell, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.CritTakenRangedRating:
                        ApplyRatingMod(CombatRating.CritRanged, (int)val, apply);
                        break;
                    case ItemModType.HasteMeleeRating:
                        ApplyRatingMod(CombatRating.HasteMelee, (int)val, apply);
                        break;
                    case ItemModType.HasteRangedRating:
                        ApplyRatingMod(CombatRating.HasteRanged, (int)val, apply);
                        break;
                    case ItemModType.HasteSpellRating:
                        ApplyRatingMod(CombatRating.HasteSpell, (int)val, apply);
                        break;
                    case ItemModType.HitRating:
                        ApplyRatingMod(CombatRating.HitMelee, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.HitRanged, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.HitSpell, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.CritRating:
                        ApplyRatingMod(CombatRating.CritMelee, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.CritRanged, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.CritSpell, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.ResilienceRating:
                        ApplyRatingMod(CombatRating.ResiliencePlayerDamage, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.HasteRating:
                        ApplyRatingMod(CombatRating.HasteMelee, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.HasteRanged, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.HasteSpell, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.ExpertiseRating:
                        ApplyRatingMod(CombatRating.Expertise, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.AttackPower:
                        HandleStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Total, (float)val, apply);
                        HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, (float)val, apply);
                        break;
                    case ItemModType.RangedAttackPower:
                        HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, (float)val, apply);
                        break;
                    case ItemModType.Versatility:
                        ApplyRatingMod(CombatRating.VersatilityDamageDone, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.VersatilityDamageTaken, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.VersatilityHealingDone, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.ManaRegeneration:
                        ApplyManaRegenBonus((int)val, apply);
                        break;
                    case ItemModType.ArmorPenetrationRating:
                        ApplyRatingMod(CombatRating.ArmorPenetration, (int)val, apply);
                        break;
                    case ItemModType.SpellPower:
                        ApplySpellPowerBonus((int)val, apply);
                        break;
                    case ItemModType.HealthRegen:
                        ApplyHealthRegenBonus((int)val, apply);
                        break;
                    case ItemModType.SpellPenetration:
                        ApplySpellPenetrationBonus((int)val, apply);
                        break;
                    case ItemModType.MasteryRating:
                        ApplyRatingMod(CombatRating.Mastery, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.ExtraArmor:
                        HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Total, (float)val, apply);
                        break;
                    case ItemModType.FireResistance:
                        HandleStatFlatModifier(UnitMods.ResistanceFire, UnitModifierFlatType.Base, (float)val, apply);
                        break;
                    case ItemModType.FrostResistance:
                        HandleStatFlatModifier(UnitMods.ResistanceFrost, UnitModifierFlatType.Base, (float)val, apply);
                        break;
                    case ItemModType.HolyResistance:
                        HandleStatFlatModifier(UnitMods.ResistanceHoly, UnitModifierFlatType.Base, (float)val, apply);
                        break;
                    case ItemModType.ShadowResistance:
                        HandleStatFlatModifier(UnitMods.ResistanceShadow, UnitModifierFlatType.Base, (float)val, apply);
                        break;
                    case ItemModType.NatureResistance:
                        HandleStatFlatModifier(UnitMods.ResistanceNature, UnitModifierFlatType.Base, (float)val, apply);
                        break;
                    case ItemModType.ArcaneResistance:
                        HandleStatFlatModifier(UnitMods.ResistanceArcane, UnitModifierFlatType.Base, (float)val, apply);
                        break;
                    case ItemModType.PvpPower:
                        ApplyRatingMod(CombatRating.PvpPower, (int)val, apply);
                        break;
                    case ItemModType.Corruption:
                        ApplyRatingMod(CombatRating.Corruption, (int)val, apply);
                        break;
                    case ItemModType.CorruptionResistance:
                        ApplyRatingMod(CombatRating.CorruptionResistance, (int)val, apply);
                        break;
                    case ItemModType.CrSpeed:
                        ApplyRatingMod(CombatRating.Speed, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.CrLifesteal:
                        ApplyRatingMod(CombatRating.Lifesteal, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.CrAvoidance:
                        ApplyRatingMod(CombatRating.Avoidance, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.CrSturdiness:
                        ApplyRatingMod(CombatRating.Studiness, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.AgiStrInt:
                        HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Base, val, apply);
                        HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Base, val, apply);
                        HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Base, val, apply);
                        UpdateStatBuffMod(Stats.Agility);
                        UpdateStatBuffMod(Stats.Strength);
                        UpdateStatBuffMod(Stats.Intellect);
                        break;
                    case ItemModType.AgiStr:
                        HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Base, val, apply);
                        HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Base, val, apply);
                        UpdateStatBuffMod(Stats.Agility);
                        UpdateStatBuffMod(Stats.Strength);
                        break;
                    case ItemModType.AgiInt:
                        HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Base, val, apply);
                        HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Base, val, apply);
                        UpdateStatBuffMod(Stats.Agility);
                        UpdateStatBuffMod(Stats.Intellect);
                        break;
                    case ItemModType.StrInt:
                        HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Base, val, apply);
                        HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Base, val, apply);
                        UpdateStatBuffMod(Stats.Strength);
                        UpdateStatBuffMod(Stats.Intellect);
                        break;
                }
            }

            uint armor = proto.GetArmor(itemLevel);
            if (armor != 0)
            {
                HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Total, (float)armor, apply);
                if (proto.GetClass() == ItemClass.Armor && (ItemSubClassArmor)proto.GetSubClass() == ItemSubClassArmor.Shield)
                    SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.ShieldBlock), apply ? (uint)(armor * 2.5f) : 0);
            }

            WeaponAttackType attType = GetAttackBySlot(slot, proto.GetInventoryType());
            if (attType != WeaponAttackType.Max)
                _ApplyWeaponDamage(slot, item, apply);
        }

        void ApplyItemEquipSpell(Item item, bool apply, bool formChange = false)
        {
            if (item == null || item.GetTemplate().HasFlag(ItemFlags.Legacy))
                return;

            foreach (ItemEffectRecord effectData in item.GetEffects())
            {
                // wrong triggering type
                if (apply && effectData.TriggerType != ItemSpelltriggerType.OnEquip)
                    continue;

                // check if it is valid spell
                SpellInfo spellproto = Global.SpellMgr.GetSpellInfo((uint)effectData.SpellID, Difficulty.None);
                if (spellproto == null)
                    continue;

                if (effectData.ChrSpecializationID != 0 && (ChrSpecialization)effectData.ChrSpecializationID != GetPrimarySpecialization())
                    continue;

                ApplyEquipSpell(spellproto, item, apply, formChange);
            }
        }

        public void ApplyEquipSpell(SpellInfo spellInfo, Item item, bool apply, bool formChange = false)
        {
            if (apply)
            {
                // Cannot be used in this stance/form
                if (spellInfo.CheckShapeshift(GetShapeshiftForm()) != SpellCastResult.SpellCastOk)
                    return;

                if (formChange)                                    // check aura active state from other form
                {
                    var range = GetAppliedAuras();
                    foreach (var pair in range)
                    {
                        if (pair.Key != spellInfo.Id)
                            continue;

                        if (item == null || pair.Value.GetBase().GetCastItemGUID() == item.GetGUID())
                            return;
                    }
                }

                Log.outDebug(LogFilter.Player, "WORLD: cast {0} Equip spellId - {1}", (item != null ? "item" : "itemset"), spellInfo.Id);

                if (spellInfo.HasAttribute(SpellAttr9.ItemPassiveOnClient))
                {
                    m_itemPassives.Add(spellInfo.Id);
                    if (IsInWorld)
                    {
                        AddItemPassive addItemPassive = new();
                        addItemPassive.SpellID = spellInfo.Id;
                        SendPacket(addItemPassive);
                    }
                }

                CastSpell(this, spellInfo.Id, new CastSpellExtraArgs(item));
            }
            else
            {
                if (formChange)                                     // check aura compatibility
                {
                    // Cannot be used in this stance/form
                    if (spellInfo.CheckShapeshift(GetShapeshiftForm()) == SpellCastResult.SpellCastOk)
                        return;                                     // and remove only not compatible at form change
                }

                if (spellInfo.HasAttribute(SpellAttr9.ItemPassiveOnClient))
                {
                    m_itemPassives.Remove(spellInfo.Id);
                    if (IsInWorld)
                    {
                        RemoveItemPassive removeItemPassive = new();
                        removeItemPassive.SpellID = spellInfo.Id;
                        SendPacket(removeItemPassive);
                    }
                }

                if (item != null)
                    RemoveAurasDueToItemSpell(spellInfo.Id, item.GetGUID());  // un-apply all spells, not only at-equipped
                else
                    RemoveAurasDueToSpell(spellInfo.Id);           // un-apply spell (item set case)
            }
        }

        void ApplyEquipCooldown(Item pItem)
        {
            if (pItem.GetTemplate().HasFlag(ItemFlags.NoEquipCooldown))
                return;

            DateTime now = GameTime.Now();
            foreach (ItemEffectRecord effectData in pItem.GetEffects())
            {
                SpellInfo effectSpellInfo = Global.SpellMgr.GetSpellInfo((uint)effectData.SpellID, Difficulty.None);
                if (effectSpellInfo == null)
                    continue;

                // apply proc cooldown to equip auras if we have any
                if (effectData.TriggerType == ItemSpelltriggerType.OnEquip)
                {
                    SpellProcEntry procEntry = Global.SpellMgr.GetSpellProcEntry(effectSpellInfo);
                    if (procEntry == null)
                        continue;

                    Aura itemAura = GetAura((uint)effectData.SpellID, GetGUID(), pItem.GetGUID());
                    if (itemAura != null)
                        itemAura.AddProcCooldown(procEntry, now);
                    continue;
                }

                // no spell
                if (effectData.SpellID == 0)
                    continue;

                // wrong triggering type
                if (effectData.TriggerType != ItemSpelltriggerType.OnUse)
                    continue;

                // Don't replace longer cooldowns by equip cooldown if we have any.
                if (GetSpellHistory().GetRemainingCooldown(effectSpellInfo) > TimeSpan.FromSeconds(30))
                    continue;

                GetSpellHistory().AddCooldown((uint)effectData.SpellID, pItem.GetEntry(), TimeSpan.FromSeconds(30));

                ItemCooldown data = new();
                data.ItemGuid = pItem.GetGUID();
                data.SpellID = (uint)effectData.SpellID;
                data.Cooldown = 30 * Time.InMilliseconds; //Always 30secs?
                SendPacket(data);
            }
        }

        public void ApplyItemLootedSpell(Item item, bool apply)
        {
            if (item.GetTemplate().HasFlag(ItemFlags.Legacy))
                return;

            var lootedEffect = item.GetEffects().FirstOrDefault(effectData => effectData.TriggerType == ItemSpelltriggerType.OnLooted);
            if (lootedEffect != null)
            {
                if (apply)
                    CastSpell(this, (uint)lootedEffect.SpellID, item);
                else
                    RemoveAurasDueToItemSpell((uint)lootedEffect.SpellID, item.GetGUID());
            }
        }

        public void ApplyItemLootedSpell(ItemTemplate itemTemplate)
        {
            if (itemTemplate.HasFlag(ItemFlags.Legacy))
                return;

            foreach (var effect in itemTemplate.Effects)
            {
                if (effect.TriggerType != ItemSpelltriggerType.OnLooted)
                    continue;

                CastSpell(this, (uint)effect.SpellID, true);
            }
        }

        void _RemoveAllItemMods()
        {
            Log.outDebug(LogFilter.Player, "_RemoveAllItemMods start.");

            for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    ItemTemplate proto = m_items[i].GetTemplate();
                    if (proto == null)
                        continue;

                    // item set bonuses not dependent from item broken state
                    if (proto.GetItemSet() != 0)
                        Item.RemoveItemsSetItem(this, m_items[i]);

                    if (m_items[i].IsBroken() || !CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    ApplyItemEquipSpell(m_items[i], false);
                    ApplyEnchantment(m_items[i], false);
                    ApplyArtifactPowers(m_items[i], false);
                }
            }

            for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    if (m_items[i].IsBroken() || !CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    ApplyItemDependentAuras(m_items[i], false);
                    _ApplyItemBonuses(m_items[i], i, false);
                }
            }

            Log.outDebug(LogFilter.Player, "_RemoveAllItemMods complete.");
        }

        void _ApplyAllItemMods()
        {
            Log.outDebug(LogFilter.Player, "_ApplyAllItemMods start.");

            for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    if (m_items[i].IsBroken() || !CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    ApplyItemDependentAuras(m_items[i], true);
                    _ApplyItemBonuses(m_items[i], i, true);

                    WeaponAttackType attackType = Player.GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType());
                    if (attackType != WeaponAttackType.Max)
                        UpdateWeaponDependentAuras(attackType);
                }
            }

            for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    ItemTemplate proto = m_items[i].GetTemplate();
                    if (proto == null)
                        continue;

                    // item set bonuses not dependent from item broken state
                    if (proto.GetItemSet() != 0)
                        Item.AddItemsSetItem(this, m_items[i]);

                    if (m_items[i].IsBroken() || !CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    ApplyItemEquipSpell(m_items[i], true);
                    ApplyArtifactPowers(m_items[i], true);
                    ApplyEnchantment(m_items[i], true);
                }
            }

            Log.outDebug(LogFilter.Player, "_ApplyAllItemMods complete.");
        }

        public void _ApplyAllLevelScaleItemMods(bool apply)
        {
            for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    if (!CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    _ApplyItemMods(m_items[i], i, apply);

                    // Update item sets for heirlooms
                    if (Global.DB2Mgr.GetHeirloomByItemId(m_items[i].GetEntry()) != null && m_items[i].GetTemplate().GetItemSet() != 0)
                    {
                        if (apply)
                            Item.AddItemsSetItem(this, m_items[i]);
                        else
                            Item.RemoveItemsSetItem(this, m_items[i]);
                    }
                }
            }
        }

        void ApplyAllAzeriteItemMods(bool apply)
        {
            for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    if (!m_items[i].IsAzeriteItem() || m_items[i].IsBroken() || !CanUseAttackType(Player.GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    ApplyAzeritePowers(m_items[i], apply);
                }
            }
        }

        public void ApplyAllAzeriteEmpoweredItemMods(bool apply)
        {
            for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    if (!m_items[i].IsAzeriteEmpoweredItem() || m_items[i].IsBroken() || !CanUseAttackType(Player.GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    ApplyAzeritePowers(m_items[i], apply);
                }
            }
        }

        public Loot GetLootByWorldObjectGUID(ObjectGuid lootWorldObjectGuid)
        {
            return m_AELootView.FirstOrDefault(pair => pair.Value.GetOwnerGUID() == lootWorldObjectGuid).Value;
        }

        public LootRoll GetLootRoll(ObjectGuid lootObjectGuid, byte lootListId)
        {
            return m_lootRolls.Find(roll => roll.IsLootItem(lootObjectGuid, lootListId));
        }

        public void AddLootRoll(LootRoll roll) { m_lootRolls.Add(roll); }

        public void RemoveLootRoll(LootRoll roll)
        {
            m_lootRolls.Remove(roll);
        }

        //Inventory
        public bool IsInventoryPos(ushort pos)
        {
            return IsInventoryPos((byte)(pos >> 8), (byte)(pos & 255));
        }

        public static bool IsInventoryPos(byte bag, byte slot)
        {
            if (bag == InventorySlots.Bag0 && slot == ItemConst.NullSlot)
                return true;
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ItemStart && slot < InventorySlots.ItemEnd))
                return true;
            if (bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd)
                return true;
            if (bag >= InventorySlots.ReagentBagStart && bag < InventorySlots.ReagentBagEnd)
                return true;
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ChildEquipmentStart && slot < InventorySlots.ChildEquipmentEnd))
                return true;
            return false;
        }

        InventoryResult CanStoreItem_InInventorySlots(byte slot_begin, byte slot_end, List<ItemPosCount> dest, ItemTemplate pProto, ref uint count, bool merge, Item pSrcItem, byte skip_bag, byte skip_slot)
        {
            //this is never called for non-bag slots so we can do this
            if (pSrcItem != null && pSrcItem.IsNotEmptyBag())
                return InventoryResult.DestroyNonemptyBag;

            for (var j = slot_begin; j < slot_end; j++)
            {
                // skip specific slot already processed in first called CanStoreItem_InSpecificSlot
                if (InventorySlots.Bag0 == skip_bag && j == skip_slot)
                    continue;

                Item pItem2 = GetItemByPos(InventorySlots.Bag0, j);

                // ignore move item (this slot will be empty at move)
                if (pItem2 == pSrcItem)
                    pItem2 = null;

                // if merge skip empty, if !merge skip non-empty
                if ((pItem2 != null) != merge)
                    continue;

                uint need_space = pProto.GetMaxStackSize();

                if (pItem2 != null)
                {
                    // can be merged at least partly
                    InventoryResult res = pItem2.CanBeMergedPartlyWith(pProto);
                    if (res != InventoryResult.Ok)
                        continue;

                    // descrease at current stacksize
                    need_space -= pItem2.GetCount();
                }

                if (need_space > count)
                    need_space = count;

                ItemPosCount newPosition = new((ushort)(InventorySlots.Bag0 << 8 | j), need_space);
                if (!newPosition.IsContainedIn(dest))
                {
                    dest.Add(newPosition);
                    count -= need_space;

                    if (count == 0)
                        return InventoryResult.Ok;
                }
            }
            return InventoryResult.Ok;
        }

        InventoryResult CanStoreItem_InSpecificSlot(byte bag, byte slot, List<ItemPosCount> dest, ItemTemplate pProto, ref uint count, bool swap, Item pSrcItem)
        {
            Item pItem2 = GetItemByPos(bag, slot);

            // ignore move item (this slot will be empty at move)
            if (pItem2 == pSrcItem)
                pItem2 = null;

            uint need_space;

            if (pSrcItem != null)
            {
                if (pSrcItem.IsNotEmptyBag() && !IsBagPos((ushort)((ushort)bag << 8 | slot)))
                    return InventoryResult.DestroyNonemptyBag;

                if (pSrcItem.HasItemFlag(ItemFieldFlags.Child) && !IsEquipmentPos(bag, slot) && !IsChildEquipmentPos(bag, slot))
                    return InventoryResult.WrongBagType3;

                if (!pSrcItem.HasItemFlag(ItemFieldFlags.Child) && IsChildEquipmentPos(bag, slot))
                    return InventoryResult.WrongBagType3;
            }

            // empty specific slot - check item fit to slot
            if (pItem2 == null || swap)
            {
                if (bag == InventorySlots.Bag0)
                {
                    // prevent cheating
                    if ((slot >= InventorySlots.BuyBackStart && slot < InventorySlots.BuyBackEnd) || slot >= (byte)PlayerSlots.End)
                        return InventoryResult.WrongBagType;
                }
                else
                {
                    Bag pBag = GetBagByPos(bag);
                    if (pBag == null)
                        return InventoryResult.WrongBagType;

                    ItemTemplate pBagProto = pBag.GetTemplate();
                    if (pBagProto == null)
                        return InventoryResult.WrongBagType;

                    if (slot >= pBagProto.GetContainerSlots())
                        return InventoryResult.WrongBagType;

                    if (!Item.ItemCanGoIntoBag(pProto, pBagProto))
                        return InventoryResult.WrongBagType;
                }

                // non empty stack with space
                need_space = pProto.GetMaxStackSize();
            }
            // non empty slot, check item type
            else
            {
                // can be merged at least partly
                InventoryResult res = pItem2.CanBeMergedPartlyWith(pProto);
                if (res != InventoryResult.Ok)
                    return res;

                // free stack space or infinity
                need_space = pProto.GetMaxStackSize() - pItem2.GetCount();
            }

            if (need_space > count)
                need_space = count;

            ItemPosCount newPosition = new((ushort)(bag << 8 | slot), need_space);
            if (!newPosition.IsContainedIn(dest))
            {
                dest.Add(newPosition);
                count -= need_space;
            }
            return InventoryResult.Ok;
        }

        public void MoveItemFromInventory(byte bag, byte slot, bool update)
        {
            Item it = GetItemByPos(bag, slot);
            if (it != null)
            {
                RemoveItem(bag, slot, update);
                ItemRemovedQuestCheck(it.GetEntry(), it.GetCount());
                it.SetNotRefundable(this, false, null, false);
                Item.RemoveItemFromUpdateQueueOf(it, this);
                GetSession().GetCollectionMgr().RemoveTemporaryAppearance(it);
                if (it.IsInWorld)
                {
                    it.RemoveFromWorld();
                    it.DestroyForPlayer(this);
                }
            }
        }

        public void MoveItemToInventory(List<ItemPosCount> dest, Item pItem, bool update, bool in_characterInventoryDB = false)
        {
            uint itemId = pItem.GetEntry();
            uint count = pItem.GetCount();

            // store item
            Item pLastItem = StoreItem(dest, pItem, update);

            // only set if not merged to existed stack
            if (pLastItem == pItem)
            {
                // update owner for last item (this can be original item with wrong owner
                if (pLastItem.GetOwnerGUID() != GetGUID())
                    pLastItem.SetOwnerGUID(GetGUID());

                // if this original item then it need create record in inventory
                // in case trade we already have item in other player inventory
                pLastItem.SetState(in_characterInventoryDB ? ItemUpdateState.Changed : ItemUpdateState.New, this);

                if (pLastItem.IsBOPTradeable())
                    AddTradeableItem(pLastItem);
            }

            // update quest counters
            ItemAddedQuestCheck(itemId, count);
            UpdateCriteria(CriteriaType.ObtainAnyItem, itemId, count);
        }

        //Bank
        public static bool IsBankPos(ushort pos)
        {
            return IsBankPos((byte)(pos >> 8), (byte)(pos & 255));
        }

        public static bool IsBankPos(byte bag, byte slot)
        {
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd))
                return true;
            if (bag >= InventorySlots.BankBagStart && bag < InventorySlots.BankBagEnd)
                return true;
            return false;
        }

        public InventoryResult CanBankItem(byte bag, byte slot, List<ItemPosCount> dest, Item pItem, bool swap, bool not_loading = true, bool reagentBankOnly = false)
        {
            if (pItem == null)
                return swap ? InventoryResult.CantSwap : InventoryResult.ItemNotFound;

            // different slots range if we're trying to store item in Reagent Bank
            if (reagentBankOnly)
            {
                Cypher.Assert(bag == ItemConst.NullBag && slot == ItemConst.NullSlot); // when reagentBankOnly is true then bag & slot must be hardcoded constants, not client input
            }

            uint count = pItem.GetCount();

            Log.outDebug(LogFilter.Player, "STORAGE: CanBankItem bag = {0}, slot = {1}, item = {2}, count = {3}", bag, slot, pItem.GetEntry(), count);
            ItemTemplate pProto = pItem.GetTemplate();
            if (pProto == null)
                return swap ? InventoryResult.CantSwap : InventoryResult.ItemNotFound;

            // item used
            if (pItem.m_lootGenerated)
                return InventoryResult.LootGone;

            if (pItem.IsBindedNotWith(this))
                return InventoryResult.NotOwner;

            // Currency tokens are not supposed to be swapped out of their hidden bag
            if (pItem.IsCurrencyToken())
            {
                Log.outError(LogFilter.Player, "Possible hacking attempt: Player {0} [guid: {1}] tried to move token [guid: {2}, entry: {3}] out of the currency bag!",
                    GetName(), GetGUID().ToString(), pItem.GetGUID().ToString(), pProto.GetId());
                return InventoryResult.CantSwap;
            }

            // check count of items (skip for auto move for same player from bank)
            InventoryResult res = CanTakeMoreSimilarItems(pItem);
            if (res != InventoryResult.Ok)
                return res;

            // in specific slot
            if (bag != ItemConst.NullBag && slot != ItemConst.NullSlot)
            {
                if (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd)
                {
                    if (!pItem.IsBag())
                        return InventoryResult.WrongSlot;

                    if (slot - InventorySlots.BagStart >= GetCharacterBankTabCount())
                        return InventoryResult.NoBankSlot;

                    res = CanUseItem(pItem, not_loading);
                    if (res != InventoryResult.Ok)
                        return res;
                }

                res = CanStoreItem_InSpecificSlot(bag, slot, dest, pProto, ref count, swap, pItem);
                if (res != InventoryResult.Ok)
                    return res;

                if (count == 0)
                    return InventoryResult.Ok;
            }

            // not specific slot or have space for partly store only in specific slot

            // in specific bag
            if (bag != ItemConst.NullBag)
            {
                if (pItem.IsNotEmptyBag())
                    return InventoryResult.BagInBag;

                // search stack in bag for merge to
                if (pProto.GetMaxStackSize() != 1)
                {
                    if (bag == InventorySlots.Bag0)
                    {
                        return InventoryResult.WrongSlot; // TODO: check if INVENTORY_SLOT_BAG_0 condition is neccessary
                    }
                    else
                    {
                        res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, false, pItem, ItemConst.NullBag, slot);
                        if (res != InventoryResult.Ok)
                            res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, true, pItem, ItemConst.NullBag, slot);

                        if (res != InventoryResult.Ok)
                            return res;

                        if (count == 0)
                            return InventoryResult.Ok;
                    }
                }

                // search free slot in bag
                if (bag == InventorySlots.Bag0)
                {
                    return InventoryResult.WrongSlot; // TODO: check if INVENTORY_SLOT_BAG_0 condition is neccessary
                }
                else
                {
                    res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, false, pItem, ItemConst.NullBag, slot);
                    if (res != InventoryResult.Ok)
                        res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, true, pItem, ItemConst.NullBag, slot);

                    if (res != InventoryResult.Ok)
                        return res;

                    if (count == 0)
                        return InventoryResult.Ok;
                }
            }

            // not specific bag or have space for partly store only in specific bag

            // search stack for merge to
            if (pProto.GetMaxStackSize() != 1)
            {
                // in regular bags
                for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
                {
                    // only consider tabs marked as reagents if requested
                    if (reagentBankOnly && (m_activePlayerData.CharacterBankTabSettings[i - InventorySlots.BankBagStart].DepositFlags & (int)BagSlotFlags.PriorityReagents) == 0)
                        continue;

                    res = CanStoreItem_InBag(i, dest, pProto, ref count, true, true, pItem, bag, slot);
                    if (res != InventoryResult.Ok)
                        continue;

                    if (count == 0)
                        return InventoryResult.Ok;
                }
            }

            // search free space in regular bags
            for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
            {
                // only consider tabs marked as reagents if requested
                if (reagentBankOnly && (m_activePlayerData.CharacterBankTabSettings[i - InventorySlots.BankBagStart].DepositFlags & (int)BagSlotFlags.PriorityReagents) == 0)
                    continue;

                res = CanStoreItem_InBag(i, dest, pProto, ref count, false, true, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                    continue;

                if (count == 0)
                    return InventoryResult.Ok;
            }

            return reagentBankOnly ? InventoryResult.ReagentBankFull : InventoryResult.BankFull;
        }

        public Item BankItem(List<ItemPosCount> dest, Item pItem, bool update)
        {
            return StoreItem(dest, pItem, update);
        }

        public uint GetFreeInventorySlotCount(ItemSearchLocation location = ItemSearchLocation.Inventory)
        {
            uint freeSlotCount = 0;

            if (location.HasFlag(ItemSearchLocation.Equipment))
            {
                for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                    if (GetItemByPos(InventorySlots.Bag0, i) == null)
                        ++freeSlotCount;

                for (byte i = ProfessionSlots.Start; i < ProfessionSlots.End; ++i)
                    if (GetItemByPos(InventorySlots.Bag0, i) == null)
                        ++freeSlotCount;
            }

            if (location.HasFlag(ItemSearchLocation.Inventory))
            {
                int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
                for (byte i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
                    if (GetItemByPos(InventorySlots.Bag0, i) == null)
                        ++freeSlotCount;

                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
                {
                    Bag bag = GetBagByPos(i);
                    if (bag != null)
                    {
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                            if (bag.GetItemByPos(j) == null)
                                ++freeSlotCount;
                    }
                }
            }

            if (location.HasFlag(ItemSearchLocation.Bank))
            {
                for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; ++i)
                {
                    Bag bag = GetBagByPos(i);
                    if (bag != null)
                    {
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                            if (bag.GetItemByPos(j) == null)
                                ++freeSlotCount;
                    }
                }
            }

            if (location.HasFlag(ItemSearchLocation.ReagentBank))
            {
                for (byte i = InventorySlots.ReagentBagStart; i < InventorySlots.ReagentBagEnd; ++i)
                {
                    Bag bag = GetBagByPos(i);
                    if (bag != null)
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                            if (bag.GetItemByPos(j) == null)
                                ++freeSlotCount;
                }
            }

            return freeSlotCount;
        }

        //Bags
        public Bag GetBagByPos(byte bag)
        {
            if ((bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd)
                || (bag >= InventorySlots.BankBagStart && bag < InventorySlots.BankBagEnd)
                || (bag >= InventorySlots.ReagentBagStart && bag < InventorySlots.ReagentBagEnd))
            {
                Item item = GetItemByPos(InventorySlots.Bag0, bag);
                if (item != null)
                    return item.ToBag();
            }
            return null;
        }

        public static bool IsBagPos(ushort pos)
        {
            byte bag = (byte)(pos >> 8);
            byte slot = (byte)(pos & 255);
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd))
                return true;
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd))
                return true;
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentBagStart && slot < InventorySlots.ReagentBagEnd))
                return true;
            return false;
        }

        InventoryResult CanStoreItem_InBag(byte bag, List<ItemPosCount> dest, ItemTemplate pProto, ref uint count, bool merge, bool non_specialized, Item pSrcItem, byte skip_bag, byte skip_slot)
        {
            // skip specific bag already processed in first called CanStoreItem_InBag
            if (bag == skip_bag)
                return InventoryResult.WrongBagType;

            // skip not existed bag or self targeted bag
            Bag pBag = GetBagByPos(bag);
            if (pBag == null || pBag == pSrcItem)
                return InventoryResult.WrongBagType;

            if (pSrcItem != null)
            {
                if (pSrcItem.IsNotEmptyBag())
                    return InventoryResult.DestroyNonemptyBag;

                if (pSrcItem.HasItemFlag(ItemFieldFlags.Child))
                    return InventoryResult.WrongBagType3;
            }

            ItemTemplate pBagProto = pBag.GetTemplate();
            if (pBagProto == null)
                return InventoryResult.WrongBagType;

            // specialized bag mode or non-specilized
            if (non_specialized != (pBagProto.GetClass() == ItemClass.Container && (pBagProto.GetSubClass() == (uint)ItemSubClassContainer.Container || pBagProto.GetSubClass() == (uint)ItemSubClassContainer.ReagentContainer)))
                return InventoryResult.WrongBagType;

            if (!Item.ItemCanGoIntoBag(pProto, pBagProto))
                return InventoryResult.WrongBagType;

            for (byte j = 0; j < pBag.GetBagSize(); j++)
            {
                // skip specific slot already processed in first called CanStoreItem_InSpecificSlot
                if (j == skip_slot)
                    continue;

                Item pItem2 = GetItemByPos(bag, j);

                // ignore move item (this slot will be empty at move)
                if (pItem2 == pSrcItem)
                    pItem2 = null;

                // if merge skip empty, if !merge skip non-empty
                if ((pItem2 != null) != merge)
                    continue;

                uint need_space = pProto.GetMaxStackSize();

                if (pItem2 != null)
                {
                    // can be merged at least partly
                    InventoryResult res = pItem2.CanBeMergedPartlyWith(pProto);
                    if (res != InventoryResult.Ok)
                        continue;

                    // descrease at current stacksize
                    need_space -= pItem2.GetCount();
                }

                if (need_space > count)
                    need_space = count;

                ItemPosCount newPosition = new((ushort)(bag << 8 | j), need_space);
                if (!newPosition.IsContainedIn(dest))
                {
                    dest.Add(newPosition);
                    count -= need_space;

                    if (count == 0)
                        return InventoryResult.Ok;
                }
            }

            return InventoryResult.Ok;
        }

        //Equipment
        public static bool IsEquipmentPos(ushort pos)
        {
            return IsEquipmentPos((byte)(pos >> 8), (byte)(pos & 255));
        }

        public static bool IsEquipmentPos(byte bag, byte slot)
        {
            if (bag == InventorySlots.Bag0 && (slot < EquipmentSlot.End))
                return true;
            if (bag == InventorySlots.Bag0 && (slot >= ProfessionSlots.Start && slot < ProfessionSlots.End))
                return true;
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd))
                return true;
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentBagStart && slot < InventorySlots.ReagentBagEnd))
                return true;
            return false;
        }

        byte FindEquipSlot(Item item, byte slot, bool swap)
        {
            byte[] slots = [ItemConst.NullSlot, ItemConst.NullSlot, ItemConst.NullSlot, ItemConst.NullSlot, ItemConst.NullSlot, ItemConst.NullSlot];
            switch (item.GetTemplate().GetInventoryType())
            {
                case InventoryType.Head:
                    slots[0] = EquipmentSlot.Head;
                    break;
                case InventoryType.Neck:
                    slots[0] = EquipmentSlot.Neck;
                    break;
                case InventoryType.Shoulders:
                    slots[0] = EquipmentSlot.Shoulders;
                    break;
                case InventoryType.Body:
                    slots[0] = EquipmentSlot.Shirt;
                    break;
                case InventoryType.Chest:
                    slots[0] = EquipmentSlot.Chest;
                    break;
                case InventoryType.Robe:
                    slots[0] = EquipmentSlot.Chest;
                    break;
                case InventoryType.Waist:
                    slots[0] = EquipmentSlot.Waist;
                    break;
                case InventoryType.Legs:
                    slots[0] = EquipmentSlot.Legs;
                    break;
                case InventoryType.Feet:
                    slots[0] = EquipmentSlot.Feet;
                    break;
                case InventoryType.Wrists:
                    slots[0] = EquipmentSlot.Wrist;
                    break;
                case InventoryType.Hands:
                    slots[0] = EquipmentSlot.Hands;
                    break;
                case InventoryType.Finger:
                    slots[0] = EquipmentSlot.Finger1;
                    slots[1] = EquipmentSlot.Finger2;
                    break;
                case InventoryType.Trinket:
                    slots[0] = EquipmentSlot.Trinket1;
                    slots[1] = EquipmentSlot.Trinket2;
                    break;
                case InventoryType.Cloak:
                    slots[0] = EquipmentSlot.Cloak;
                    break;
                case InventoryType.Weapon:
                {
                    slots[0] = EquipmentSlot.MainHand;

                    // suggest offhand slot only if know dual wielding
                    // (this will be replace mainhand weapon at auto equip instead unwonted "you don't known dual wielding" ...
                    if (CanDualWield())
                        slots[1] = EquipmentSlot.OffHand;
                    break;
                }
                case InventoryType.Shield:
                    slots[0] = EquipmentSlot.OffHand;
                    break;
                case InventoryType.Ranged:
                    slots[0] = EquipmentSlot.MainHand;
                    break;
                case InventoryType.Weapon2Hand:
                    slots[0] = EquipmentSlot.MainHand;
                    if (CanDualWield() && CanTitanGrip())
                        slots[1] = EquipmentSlot.OffHand;
                    break;
                case InventoryType.Tabard:
                    slots[0] = EquipmentSlot.Tabard;
                    break;
                case InventoryType.WeaponMainhand:
                    slots[0] = EquipmentSlot.MainHand;
                    break;
                case InventoryType.WeaponOffhand:
                    slots[0] = EquipmentSlot.OffHand;
                    break;
                case InventoryType.Holdable:
                    slots[0] = EquipmentSlot.OffHand;
                    break;
                case InventoryType.RangedRight:
                    slots[0] = EquipmentSlot.MainHand;
                    break;
                case InventoryType.Bag:
                    if (item.GetTemplate().GetId() == 208392)
                        slots = [InventorySlots.AccountBankBagStart + 0, InventorySlots.AccountBankBagStart + 1, InventorySlots.AccountBankBagStart + 2, InventorySlots.AccountBankBagStart + 3, InventorySlots.AccountBankBagStart + 4, ItemConst.NullSlot];
                    else if (item.GetTemplate().GetId() == 242709)
                        slots = [InventorySlots.BankBagStart + 0, InventorySlots.BankBagStart + 1, InventorySlots.BankBagStart + 2, InventorySlots.BankBagStart + 3, InventorySlots.BankBagStart + 4, InventorySlots.BankBagStart + 5];
                    else if (item.GetTemplate().GetClass() != ItemClass.Container || item.GetTemplate().GetSubClass() != (int)ItemSubClassContainer.ReagentContainer)
                        slots = [InventorySlots.BagStart + 0, InventorySlots.BagStart + 1, InventorySlots.BagStart + 2, InventorySlots.BagStart + 3, ItemConst.NullSlot, ItemConst.NullSlot];
                    else
                        slots[0] = InventorySlots.ReagentBagStart;
                    break;
                case InventoryType.ProfessionTool:
                case InventoryType.ProfessionGear:
                {
                    bool isProfessionTool = item.GetTemplate().GetInventoryType() == InventoryType.ProfessionTool;

                    // Validate item class
                    if (item.GetTemplate().GetClass() != ItemClass.Profession)
                        return ItemConst.NullSlot;

                    // Check if player has profession skill
                    uint itemSkill = (uint)item.GetTemplate().GetSkill();
                    if (!HasSkill(itemSkill))
                        return ItemConst.NullSlot;

                    switch ((ItemSubclassProfession)item.GetTemplate().GetSubClass())
                    {
                        case ItemSubclassProfession.Cooking:
                            slots[0] = isProfessionTool ? ProfessionSlots.CookingTool : ProfessionSlots.CookingGear1;
                            break;
                        case ItemSubclassProfession.Fishing:
                        {
                            // Fishing doesn't make use of gear slots (clientside)
                            if (!isProfessionTool)
                                return ItemConst.NullSlot;

                            slots[0] = ProfessionSlots.FishingTool;
                            break;
                        }
                        case ItemSubclassProfession.Blacksmithing:
                        case ItemSubclassProfession.Leatherworking:
                        case ItemSubclassProfession.Alchemy:
                        case ItemSubclassProfession.Herbalism:
                        case ItemSubclassProfession.Mining:
                        case ItemSubclassProfession.Tailoring:
                        case ItemSubclassProfession.Engineering:
                        case ItemSubclassProfession.Enchanting:
                        case ItemSubclassProfession.Skinning:
                        case ItemSubclassProfession.Jewelcrafting:
                        case ItemSubclassProfession.Inscription:
                        {
                            int professionSlot = GetProfessionSlotFor(itemSkill);
                            if (professionSlot == -1)
                                return ItemConst.NullSlot;

                            if (isProfessionTool)
                                slots[0] = (byte)(ProfessionSlots.Profession1Tool + professionSlot * ProfessionSlots.MaxCount);
                            else
                            {
                                slots[0] = (byte)(ProfessionSlots.Profession1Gear1 + professionSlot * ProfessionSlots.MaxCount);
                                slots[1] = (byte)(ProfessionSlots.Profession1Gear2 + professionSlot * ProfessionSlots.MaxCount);
                            }

                            break;
                        }
                        default:
                            return ItemConst.NullSlot;
                    }
                    break;
                }
                default:
                    return ItemConst.NullSlot;
            }

            if (slot != ItemConst.NullSlot)
            {
                if (swap || GetItemByPos(InventorySlots.Bag0, slot) == null)
                    if (slots.Contains(slot))
                        return slot;
            }
            else
            {
                // search free slot at first
                var freeSlot = slots.FirstOrDefault(candidateSlot =>
                {
                    if (candidateSlot != ItemConst.NullSlot && GetItemByPos(InventorySlots.Bag0, candidateSlot) == null)
                        // in case 2hand equipped weapon (without titan grip) offhand slot empty but not free
                        if (candidateSlot != EquipmentSlot.OffHand || !IsTwoHandUsed())
                            return true;
                    return false;
                });

                if (freeSlot != 0)
                    return freeSlot;

                // if not found free and can swap return slot with lower item level equipped
                if (swap)
                {
                    freeSlot = (byte)slots.Min(candidateSlot =>
                    {
                        if (candidateSlot == ItemConst.NullSlot)
                            return uint.MaxValue;

                        Item equipped = GetItemByPos(InventorySlots.Bag0, candidateSlot);
                        if (equipped != null)
                            return equipped.GetItemLevel(this);

                        return 0u;
                    });

                    if (freeSlot != 0)
                        return freeSlot;
                }
            }

            // no free position
            return ItemConst.NullSlot;
        }

        public InventoryResult CanEquipNewItem(byte slot, out ushort dest, uint item, bool swap)
        {
            dest = 0;
            Item pItem = Item.CreateItem(item, 1, ItemContext.None, this);
            if (pItem != null)
            {
                InventoryResult result = CanEquipItem(slot, out dest, pItem, swap);
                return result;
            }

            return InventoryResult.ItemNotFound;
        }

        public InventoryResult CanEquipItem(byte slot, out ushort dest, Item pItem, bool swap, bool not_loading = true)
        {
            dest = 0;
            if (pItem != null)
            {
                Log.outDebug(LogFilter.Player, "STORAGE: CanEquipItem slot = {0}, item = {1}, count = {2}", slot, pItem.GetEntry(), pItem.GetCount());
                ItemTemplate pProto = pItem.GetTemplate();
                if (pProto != null)
                {
                    // item used
                    if (pItem.m_lootGenerated)
                        return InventoryResult.LootGone;

                    if (pItem.IsBindedNotWith(this))
                        return InventoryResult.NotOwner;

                    // check count of items (skip for auto move for same player from bank)
                    InventoryResult res = CanTakeMoreSimilarItems(pItem);
                    if (res != InventoryResult.Ok)
                        return res;

                    // check this only in game
                    if (not_loading)
                    {
                        // May be here should be more stronger checks; STUNNED checked
                        // ROOT, CONFUSED, DISTRACTED, FLEEING this needs to be checked.
                        if (HasUnitState(UnitState.Stunned))
                            return InventoryResult.GenericStunned;

                        if (IsCharmed())
                            return InventoryResult.CantDoThatRightNow; // @todo is this the correct error?

                        // do not allow equipping gear except weapons, offhands, projectiles, relics in
                        // - combat
                        // - in-progress arenas
                        if (!pProto.CanChangeEquipStateInCombat())
                        {
                            if (IsInCombat())
                                return InventoryResult.NotInCombat;
                            Battleground bg = GetBattleground();
                            if (bg != null)
                                if (bg.IsArena() && bg.GetStatus() == BattlegroundStatus.InProgress)
                                    return InventoryResult.NotDuringArenaMatch;
                        }

                        if (IsInCombat() && (pProto.GetClass() == ItemClass.Weapon || pProto.GetInventoryType() == InventoryType.Relic) && m_weaponChangeTimer != 0)
                            return InventoryResult.ItemCooldown;

                        Spell currentGenericSpell = GetCurrentSpell(CurrentSpellTypes.Generic);
                        if (currentGenericSpell != null)
                            if (!currentGenericSpell.GetSpellInfo().HasAttribute(SpellAttr6.AllowEquipWhileCasting))
                                return InventoryResult.ClientLockedOut;

                        Spell currentChanneledSpell = GetCurrentSpell(CurrentSpellTypes.Channeled);
                        if (currentChanneledSpell != null)
                            if (!currentChanneledSpell.GetSpellInfo().HasAttribute(SpellAttr6.AllowEquipWhileCasting))
                                return InventoryResult.ClientLockedOut;
                    }

                    ContentTuningLevels? requiredLevels = null;
                    // check allowed level (extend range to upper values if MaxLevel more or equal max player level, this let GM set high level with 1...max range items)
                    if (pItem.GetQuality() == ItemQuality.Heirloom)
                        requiredLevels = Global.DB2Mgr.GetContentTuningData(pItem.GetScalingContentTuningId(), 0, true);

                    if (requiredLevels.HasValue && requiredLevels.Value.MaxLevel < SharedConst.DefaultMaxLevel && requiredLevels.Value.MaxLevel < GetLevel() && Global.DB2Mgr.GetHeirloomByItemId(pProto.GetId()) == null)
                        return InventoryResult.NotEquippable;

                    byte eslot = FindEquipSlot(pItem, slot, swap);
                    if (eslot == ItemConst.NullSlot)
                        return InventoryResult.NotEquippable;

                    res = CanUseItem(pItem, not_loading);
                    if (res != InventoryResult.Ok)
                        return res;

                    if (!swap && GetItemByPos(InventorySlots.Bag0, eslot) != null)
                        return InventoryResult.NoSlotAvailable;

                    // if we are swapping 2 equiped items, CanEquipUniqueItem check
                    // should ignore the item we are trying to swap, and not the
                    // destination item. CanEquipUniqueItem should ignore destination
                    // item only when we are swapping weapon from bag
                    byte ignore = ItemConst.NullSlot;
                    switch (eslot)
                    {
                        case EquipmentSlot.MainHand:
                            ignore = EquipmentSlot.OffHand;
                            break;
                        case EquipmentSlot.OffHand:
                            ignore = EquipmentSlot.MainHand;
                            break;
                        case EquipmentSlot.Finger1:
                            ignore = EquipmentSlot.Finger2;
                            break;
                        case EquipmentSlot.Finger2:
                            ignore = EquipmentSlot.Finger1;
                            break;
                        case EquipmentSlot.Trinket1:
                            ignore = EquipmentSlot.Trinket2;
                            break;
                        case EquipmentSlot.Trinket2:
                            ignore = EquipmentSlot.Trinket1;
                            break;
                        case ProfessionSlots.Profession1Gear1:
                            ignore = ProfessionSlots.Profession1Gear2;
                            break;
                        case ProfessionSlots.Profession1Gear2:
                            ignore = ProfessionSlots.Profession1Gear1;
                            break;
                        case ProfessionSlots.Profession2Gear1:
                            ignore = ProfessionSlots.Profession2Gear2;
                            break;
                        case ProfessionSlots.Profession2Gear2:
                            ignore = ProfessionSlots.Profession2Gear1;
                            break;
                    }

                    if (ignore == ItemConst.NullSlot || pItem != GetItemByPos(InventorySlots.Bag0, ignore))
                        ignore = eslot;

                    // if swap ignore item (equipped also)
                    InventoryResult res2 = CanEquipUniqueItem(pItem, swap ? ignore : ItemConst.NullSlot);
                    if (res2 != InventoryResult.Ok)
                        return res2;

                    // check unique-equipped special item classes
                    if (pProto.GetClass() == ItemClass.Quiver)
                        for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
                        {
                            Item pBag = GetItemByPos(InventorySlots.Bag0, i);
                            if (pBag != null)
                            {
                                if (pBag != pItem)
                                {
                                    ItemTemplate pBagProto = pBag.GetTemplate();
                                    if (pBagProto != null)
                                        if (pBagProto.GetClass() == pProto.GetClass() && (!swap || pBag.GetSlot() != eslot))
                                            return (pBagProto.GetSubClass() == (uint)ItemSubClassQuiver.AmmoPouch)
                                                ? InventoryResult.OnlyOneAmmo
                                                : InventoryResult.OnlyOneQuiver;
                                }
                            }
                        }

                    InventoryType type = pProto.GetInventoryType();

                    if (eslot == EquipmentSlot.OffHand)
                    {
                        // Do not allow polearm to be equipped in the offhand (rare case for the only 1h polearm 41750)
                        if (type == InventoryType.Weapon && pProto.GetSubClass() == (uint)ItemSubClassWeapon.Polearm)
                            return InventoryResult.WrongSlot;
                        else if (type == InventoryType.Weapon)
                        {
                            if (!CanDualWield())
                                return InventoryResult.TwoHandSkillNotFound;
                        }
                        else if (type == InventoryType.WeaponOffhand)
                        {
                            if (!CanDualWield() && !pProto.HasFlag(ItemFlags3.AlwaysAllowDualWield))
                                return InventoryResult.TwoHandSkillNotFound;
                        }
                        else if (type == InventoryType.Weapon2Hand)
                        {
                            if (!CanDualWield() || !CanTitanGrip())
                                return InventoryResult.TwoHandSkillNotFound;
                        }

                        if (IsTwoHandUsed())
                            return InventoryResult.Equipped2handed;
                    }

                    // equip two-hand weapon case (with possible unequip 2 items)
                    if (type == InventoryType.Weapon2Hand)
                    {
                        if (eslot == EquipmentSlot.OffHand)
                        {
                            if (!CanTitanGrip())
                                return InventoryResult.NotEquippable;
                        }
                        else if (eslot != EquipmentSlot.MainHand)
                            return InventoryResult.NotEquippable;

                        if (!CanTitanGrip())
                        {
                            // offhand item must can be stored in inventory for offhand item and it also must be unequipped
                            Item offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                            List<ItemPosCount> off_dest = new();
                            if (offItem != null && (!not_loading || CanUnequipItem(((int)InventorySlots.Bag0 << 8) | (int)EquipmentSlot.OffHand, false) != InventoryResult.Ok ||
                                CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, off_dest, offItem, false) != InventoryResult.Ok))
                                return swap ? InventoryResult.CantSwap : InventoryResult.InvFull;
                        }
                    }
                    dest = (ushort)(((uint)InventorySlots.Bag0 << 8) | eslot);
                    return InventoryResult.Ok;
                }
            }
            return !swap ? InventoryResult.ItemNotFound : InventoryResult.CantSwap;
        }
        public InventoryResult CanEquipChildItem(Item parentItem)
        {
            Item childItem = GetChildItemByGuid(parentItem.GetChildItem());
            if (childItem == null)
                return InventoryResult.Ok;

            ItemChildEquipmentRecord childEquipement = Global.DB2Mgr.GetItemChildEquipment(parentItem.GetEntry());
            if (childEquipement == null)
                return InventoryResult.Ok;

            Item dstItem = GetItemByPos(InventorySlots.Bag0, childEquipement.ChildItemEquipSlot);
            if (dstItem == null)
                return InventoryResult.Ok;

            ushort childDest = (ushort)((InventorySlots.Bag0 << 8) | childEquipement.ChildItemEquipSlot);
            InventoryResult msg = CanUnequipItem(childDest, !childItem.IsBag());
            if (msg != InventoryResult.Ok)
                return msg;

            // check dest.src move possibility
            ushort src = parentItem.GetPos();
            List<ItemPosCount> dest = new();
            if (IsInventoryPos(src))
            {
                msg = CanStoreItem(parentItem.GetBagSlot(), ItemConst.NullSlot, dest, dstItem, true);
                if (msg != InventoryResult.Ok)
                    msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, dstItem, true);
            }
            else if (IsBankPos(src))
            {
                msg = CanBankItem(parentItem.GetBagSlot(), ItemConst.NullSlot, dest, dstItem, true);
                if (msg != InventoryResult.Ok)
                    msg = CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, dstItem, true);
            }
            else if (IsEquipmentPos(src))
                return InventoryResult.CantSwap;

            return msg;
        }
        public InventoryResult CanEquipUniqueItem(Item pItem, byte eslot = ItemConst.NullSlot, uint limit_count = 1)
        {
            ItemTemplate pProto = pItem.GetTemplate();

            // proto based limitations
            InventoryResult res = CanEquipUniqueItem(pProto, pItem.GetBonus(), eslot, limit_count);
            if (res != InventoryResult.Ok)
                return res;

            // check unique-equipped on gems
            foreach (SocketedGem gemData in pItem.m_itemData.Gems)
            {
                ItemTemplate pGem = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);
                if (pGem == null)
                    continue;

                BonusData gemBonus = new(pGem);

                foreach (ushort bonusListID in gemData.BonusListIDs)
                    gemBonus.AddBonusList(bonusListID);

                // include for check equip another gems with same limit category for not equipped item (and then not counted)
                uint gem_limit_count = (uint)(!pItem.IsEquipped() && gemBonus.LimitCategory != 0 ? pItem.GetGemCountWithLimitCategory(gemBonus.LimitCategory) : 1);

                InventoryResult ress = CanEquipUniqueItem(pGem, gemBonus, eslot, gem_limit_count);
                if (ress != InventoryResult.Ok)
                    return ress;
            }

            return InventoryResult.Ok;
        }
        public InventoryResult CanEquipUniqueItem(ItemTemplate itemProto, BonusData itemBonus, byte except_slot = ItemConst.NullSlot, uint limit_count = 1)
        {
            // check unique-equipped on item
            if (itemProto.HasFlag(ItemFlags.UniqueEquippable))
            {
                // there is an equip limit on this item
                if (HasItemOrGemWithIdEquipped(itemProto.GetId(), 1, except_slot))
                    return InventoryResult.ItemUniqueEquippable;
            }

            // check unique-equipped limit
            if (itemBonus.LimitCategory != 0)
            {
                ItemLimitCategoryRecord limitEntry = CliDB.ItemLimitCategoryStorage.LookupByKey(itemBonus.LimitCategory);
                if (limitEntry == null)
                    return InventoryResult.NotEquippable;

                // NOTE: limitEntry.mode not checked because if item have have-limit then it applied and to equip case
                byte limitQuantity = GetItemLimitCategoryQuantity(limitEntry);

                if (limit_count > limitQuantity)
                    return InventoryResult.ItemMaxLimitCategoryEquippedExceededIs;

                // there is an equip limit on this item
                if (HasItemWithLimitCategoryEquipped(itemBonus.LimitCategory, limitQuantity - limit_count + 1, except_slot))
                    return InventoryResult.ItemMaxLimitCategoryEquippedExceededIs;
                else if (HasGemWithLimitCategoryEquipped(itemBonus.LimitCategory, limitQuantity - limit_count + 1, except_slot))
                    return InventoryResult.ItemMaxCountEquippedSocketed;
            }

            return InventoryResult.Ok;
        }
        public InventoryResult CanUnequipItem(ushort pos, bool swap)
        {
            // Applied only to equipped items and bank bags
            if (!IsEquipmentPos(pos) && !IsBagPos(pos))
                return InventoryResult.Ok;

            Item pItem = GetItemByPos(pos);

            // Applied only to existed equipped item
            if (pItem == null)
                return InventoryResult.Ok;

            Log.outDebug(LogFilter.Player, "STORAGE: CanUnequipItem slot = {0}, item = {1}, count = {2}", pos, pItem.GetEntry(), pItem.GetCount());

            ItemTemplate pProto = pItem.GetTemplate();
            if (pProto == null)
                return InventoryResult.ItemNotFound;

            // item used
            if (pItem.m_lootGenerated)
                return InventoryResult.LootGone;

            if (IsCharmed())
                return InventoryResult.CantDoThatRightNow; // @todo is this the correct error?

            // do not allow unequipping gear except weapons, offhands, projectiles, relics in
            // - combat
            // - in-progress arenas
            if (!pProto.CanChangeEquipStateInCombat())
            {
                if (IsInCombat())
                    return InventoryResult.NotInCombat;
                Battleground bg = GetBattleground();
                if (bg != null)
                    if (bg.IsArena() && bg.GetStatus() == BattlegroundStatus.InProgress)
                        return InventoryResult.NotDuringArenaMatch;
            }

            if (!swap && pItem.IsNotEmptyBag())
                return InventoryResult.DestroyNonemptyBag;

            return InventoryResult.Ok;
        }

        //Child
        public static bool IsChildEquipmentPos(ushort pos) { return IsChildEquipmentPos((byte)(pos >> 8), (byte)(pos & 255)); }

        public static bool IsAccountBankPos(ushort pos) { return IsBankPos((byte)(pos >> 8), (byte)(pos & 255)); }
        public static bool IsAccountBankPos(byte bag, byte slot)
        {
            if (bag >= (int)InventorySlots.AccountBankBagStart && bag < InventorySlots.AccountBankBagEnd)
                return true;
            return false;
        }

        //Artifact
        void ApplyArtifactPowers(Item item, bool apply)
        {
            if (item.IsArtifactDisabled())
                return;

            foreach (ArtifactPower artifactPower in item.m_itemData.ArtifactPowers)
            {
                byte rank = artifactPower.CurrentRankWithBonus;
                if (rank == 0)
                    continue;

                if (CliDB.ArtifactPowerStorage[artifactPower.ArtifactPowerId].HasFlag(ArtifactPowerFlag.ScalesWithNumPowers))
                    rank = 1;

                ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(rank - 1));
                if (artifactPowerRank == null)
                    continue;

                ApplyArtifactPowerRank(item, artifactPowerRank, apply);
            }

            ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(item.GetModifier(ItemModifier.ArtifactAppearanceId));
            if (artifactAppearance != null)
                if (artifactAppearance.OverrideShapeshiftDisplayID != 0 && GetShapeshiftForm() == (ShapeShiftForm)artifactAppearance.OverrideShapeshiftFormID)
                    RestoreDisplayId();
        }

        public void ApplyArtifactPowerRank(Item artifact, ArtifactPowerRankRecord artifactPowerRank, bool apply)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(artifactPowerRank.SpellID, Difficulty.None);
            if (spellInfo == null)
                return;

            if (spellInfo.IsPassive())
            {
                AuraApplication powerAura = GetAuraApplication(artifactPowerRank.SpellID, ObjectGuid.Empty, artifact.GetGUID());
                if (powerAura != null)
                {
                    if (apply)
                    {
                        foreach (AuraEffect auraEffect in powerAura.GetBase().GetAuraEffects())
                        {
                            if (auraEffect == null)
                                continue;

                            if (powerAura.HasEffect(auraEffect.GetEffIndex()))
                                auraEffect.ChangeAmount((int)(artifactPowerRank.AuraPointsOverride != 0 ? artifactPowerRank.AuraPointsOverride : auraEffect.GetSpellEffectInfo().CalcValue()));
                        }
                    }
                    else
                        RemoveAura(powerAura);
                }
                else if (apply)
                {
                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                    args.SetCastItem(artifact);
                    if (artifactPowerRank.AuraPointsOverride != 0)
                    {
                        foreach (var spellEffectInfo in spellInfo.GetEffects())
                            args.AddSpellMod(SpellValueMod.BasePoint0 + (int)spellEffectInfo.EffectIndex, (int)artifactPowerRank.AuraPointsOverride);
                    }

                    CastSpell(this, artifactPowerRank.SpellID, args);
                }
            }
            else
            {
                if (apply && !HasSpell(artifactPowerRank.SpellID))
                {
                    AddTemporarySpell(artifactPowerRank.SpellID);
                    LearnedSpells learnedSpells = new();
                    LearnedSpellInfo learnedSpellInfo = new();
                    learnedSpellInfo.SpellID = artifactPowerRank.SpellID;
                    learnedSpells.SuppressMessaging = true;
                    learnedSpells.ClientLearnedSpellData.Add(learnedSpellInfo);
                    SendPacket(learnedSpells);
                }
                else if (!apply)
                {
                    RemoveTemporarySpell(artifactPowerRank.SpellID);
                    UnlearnedSpells unlearnedSpells = new();
                    unlearnedSpells.SuppressMessaging = true;
                    unlearnedSpells.SpellID.Add(artifactPowerRank.SpellID);
                    SendPacket(unlearnedSpells);
                }
            }
        }

        void ApplyAzeritePowers(Item item, bool apply)
        {
            AzeriteItem azeriteItem = item.ToAzeriteItem();
            if (azeriteItem != null)
            {
                // milestone powers
                foreach (uint azeriteItemMilestonePowerId in azeriteItem.m_azeriteItemData.UnlockedEssenceMilestones)
                    ApplyAzeriteItemMilestonePower(azeriteItem, CliDB.AzeriteItemMilestonePowerStorage.LookupByKey(azeriteItemMilestonePowerId), apply);

                // essences
                SelectedAzeriteEssences selectedEssences = azeriteItem.GetSelectedAzeriteEssences();
                if (selectedEssences != null)
                {
                    for (byte slot = 0; slot < SharedConst.MaxAzeriteEssenceSlot; ++slot)
                        if (selectedEssences.AzeriteEssenceID[slot] != 0)
                            ApplyAzeriteEssence(azeriteItem, selectedEssences.AzeriteEssenceID[slot], azeriteItem.GetEssenceRank(selectedEssences.AzeriteEssenceID[slot]),
                                (AzeriteItemMilestoneType)Global.DB2Mgr.GetAzeriteItemMilestonePower(slot).Type == AzeriteItemMilestoneType.MajorEssence, apply);
                }
            }
            else
            {
                AzeriteEmpoweredItem azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();
                if (azeriteEmpoweredItem != null)
                {
                    if (!apply || GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Equipment) != null)
                    {
                        for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                        {
                            AzeritePowerRecord azeritePower = CliDB.AzeritePowerStorage.LookupByKey(azeriteEmpoweredItem.GetSelectedAzeritePower(i));
                            if (azeritePower != null)
                                ApplyAzeritePower(azeriteEmpoweredItem, azeritePower, apply);
                        }
                    }
                }
            }
        }

        public void ApplyAzeriteItemMilestonePower(AzeriteItem item, AzeriteItemMilestonePowerRecord azeriteItemMilestonePower, bool apply)
        {
            AzeriteItemMilestoneType type = (AzeriteItemMilestoneType)azeriteItemMilestonePower.Type;
            if (type == AzeriteItemMilestoneType.BonusStamina)
            {
                AzeritePowerRecord azeritePower = CliDB.AzeritePowerStorage.LookupByKey(azeriteItemMilestonePower.AzeritePowerID);
                if (azeritePower != null)
                {
                    if (apply)
                        CastSpell(this, azeritePower.SpellID, item);
                    else
                        RemoveAurasDueToItemSpell(azeritePower.SpellID, item.GetGUID());
                }
            }
        }

        public void ApplyAzeriteEssence(AzeriteItem item, uint azeriteEssenceId, uint rank, bool major, bool apply)
        {
            for (uint currentRank = 1; currentRank <= rank; ++currentRank)
            {
                AzeriteEssencePowerRecord azeriteEssencePower = Global.DB2Mgr.GetAzeriteEssencePower(azeriteEssenceId, currentRank);
                if (azeriteEssencePower != null)
                {
                    ApplyAzeriteEssencePower(item, azeriteEssencePower, major, apply);
                    if (major && currentRank == 1)
                    {
                        if (apply)
                        {
                            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                            args.AddSpellMod(SpellValueMod.BasePoint0, (int)azeriteEssencePower.MajorPowerDescription);
                            CastSpell(this, PlayerConst.SpellIdHeartEssenceActionBarOverride, args);
                        }
                        else
                            RemoveAurasDueToSpell(PlayerConst.SpellIdHeartEssenceActionBarOverride);
                    }
                }
            }
        }

        void ApplyAzeriteEssencePower(AzeriteItem item, AzeriteEssencePowerRecord azeriteEssencePower, bool major, bool apply)
        {
            SpellInfo powerSpell = Global.SpellMgr.GetSpellInfo(azeriteEssencePower.MinorPowerDescription, Difficulty.None);
            if (powerSpell != null)
            {
                if (apply)
                    CastSpell(this, powerSpell.Id, item);
                else
                    RemoveAurasDueToItemSpell(powerSpell.Id, item.GetGUID());
            }

            if (major)
            {
                powerSpell = Global.SpellMgr.GetSpellInfo(azeriteEssencePower.MajorPowerDescription, Difficulty.None);
                if (powerSpell != null)
                {
                    if (powerSpell.IsPassive())
                    {
                        if (apply)
                            CastSpell(this, powerSpell.Id, item);
                        else
                            RemoveAurasDueToItemSpell(powerSpell.Id, item.GetGUID());
                    }
                    else
                    {
                        if (apply)
                            LearnSpell(powerSpell.Id, true, 0, true);
                        else
                            RemoveSpell(powerSpell.Id, false, false, true);
                    }
                }
            }
        }

        public void ApplyAzeritePower(AzeriteEmpoweredItem item, AzeritePowerRecord azeritePower, bool apply)
        {
            if (apply)
            {
                if (azeritePower.SpecSetID == 0 || Global.DB2Mgr.IsSpecSetMember(azeritePower.SpecSetID, (uint)GetPrimarySpecialization()))
                    CastSpell(this, azeritePower.SpellID, item);
            }
            else
                RemoveAurasDueToItemSpell(azeritePower.SpellID, item.GetGUID());
        }

        public bool HasItemOrGemWithIdEquipped(uint item, uint count, byte except_slot = ItemConst.NullSlot)
        {
            uint tempcount = 0;

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(item);
            bool includeGems = pProto?.GetGemProperties() != 0;
            return !ForEachItem(ItemSearchLocation.Equipment, pItem =>
            {
                if (pItem.GetSlot() != except_slot)
                {
                    if (pItem.GetEntry() == item)
                        tempcount += pItem.GetCount();

                    if (includeGems)
                        tempcount += pItem.GetGemCountWithID(item);

                    if (tempcount >= count)
                        return false;
                }
                return true;
            });
        }
        bool HasItemWithLimitCategoryEquipped(uint limitCategory, uint count, byte except_slot)
        {
            uint tempcount = 0;
            return !ForEachItem(ItemSearchLocation.Equipment, pItem =>
            {
                if (pItem.GetSlot() == except_slot)
                    return true;

                if (pItem.GetItemLimitCategory() != limitCategory)
                    return true;

                tempcount += pItem.GetCount();
                if (tempcount >= count)
                    return false;

                return true;
            });
        }

        bool HasGemWithLimitCategoryEquipped(uint limitCategory, uint count, byte except_slot)
        {
            uint tempcount = 0;
            return !ForEachItem(ItemSearchLocation.Equipment, pItem =>
            {
                if (pItem.GetSlot() == except_slot)
                    return true;

                ItemTemplate pProto = pItem.GetTemplate();
                if (pProto == null)
                    return true;

                tempcount += pItem.GetGemCountWithLimitCategory(limitCategory);
                if (tempcount >= count)
                    return false;

                return true;
            });
        }

        //Visual
        public void SetVisibleItemSlot(uint slot, Item pItem)
        {
            var itemField = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.VisibleItems, (int)slot);
            if (pItem != null)
            {
                SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemID), pItem.GetVisibleEntry(this));
                SetUpdateFieldValue(itemField.ModifyValue(itemField.SecondaryItemModifiedAppearanceID), pItem.GetVisibleSecondaryModifiedAppearanceId(this));
                SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemAppearanceModID), pItem.GetVisibleAppearanceModId(this));
                SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemVisual), pItem.GetVisibleItemVisual(this));
            }
            else
            {
                SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemID), 0u);
                SetUpdateFieldValue(itemField.ModifyValue(itemField.SecondaryItemModifiedAppearanceID), 0u);
                SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemAppearanceModID), (ushort)0);
                SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemVisual), (ushort)0);
            }
        }
        void VisualizeItem(uint slot, Item pItem)
        {
            if (pItem == null)
                return;

            // check also  BIND_WHEN_PICKED_UP and BIND_QUEST_ITEM for .additem or .additemset case by GM (not binded at adding to inventory)
            if (pItem.GetBonding() == ItemBondingType.OnEquip || pItem.GetBonding() == ItemBondingType.OnAcquire || pItem.GetBonding() == ItemBondingType.Quest)
            {
                pItem.SetBinding(true);
                if (IsInWorld)
                    GetSession().GetCollectionMgr().AddItemAppearance(pItem);
            }

            Log.outDebug(LogFilter.Player, "STORAGE: EquipItem slot = {0}, item = {1}", slot, pItem.GetEntry());

            m_items[slot] = pItem;
            SetInvSlot(slot, pItem.GetGUID());
            pItem.SetContainedIn(GetGUID());
            pItem.SetOwnerGUID(GetGUID());
            pItem.SetSlot((byte)slot);
            pItem.SetContainer(null);

            if (slot < EquipmentSlot.End)
                SetVisibleItemSlot(slot, pItem);

            pItem.SetState(ItemUpdateState.Changed, this);
        }

        public void DestroyItem(byte bag, byte slot, bool update)
        {
            Item pItem = GetItemByPos(bag, slot);
            if (pItem != null)
            {
                Log.outDebug(LogFilter.Player, "STORAGE: DestroyItem bag = {0}, slot = {1}, item = {2}", bag, slot, pItem.GetEntry());
                // Also remove all contained items if the item is a bag.
                // This if () prevents item saving crashes if the condition for a bag to be empty before being destroyed was bypassed somehow.
                if (pItem.IsNotEmptyBag())
                    for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
                        DestroyItem(slot, i, update);

                if (pItem.IsWrapped())
                {
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GIFT);
                    stmt.AddValue(0, pItem.GetGUID().GetCounter());
                    DB.Characters.Execute(stmt);
                }

                RemoveEnchantmentDurations(pItem);
                RemoveItemDurations(pItem);

                pItem.SetNotRefundable(this);
                pItem.ClearSoulboundTradeable(this);
                RemoveTradeableItem(pItem);

                ApplyItemObtainSpells(pItem, false);
                ApplyItemLootedSpell(pItem, false);

                Global.ScriptMgr.OnItemRemove(this, pItem);

                Bag pBag;
                ItemTemplate pProto = pItem.GetTemplate();
                if (bag == InventorySlots.Bag0)
                {
                    SetInvSlot(slot, ObjectGuid.Empty);

                    // equipment and equipped bags can have applied bonuses
                    if (slot < InventorySlots.ReagentBagEnd)
                    {
                        // item set bonuses applied only at equip and removed at unequip, and still active for broken items
                        if (pProto != null && pProto.GetItemSet() != 0)
                            Item.RemoveItemsSetItem(this, pItem);

                        _ApplyItemMods(pItem, slot, false);
                    }

                    if (slot < EquipmentSlot.End)
                    {
                        // update expertise and armor penetration - passive auras may need it
                        switch (slot)
                        {
                            case EquipmentSlot.MainHand:
                            case EquipmentSlot.OffHand:
                                RecalculateRating(CombatRating.ArmorPenetration);
                                break;
                            default:
                                break;
                        }

                        if (slot == EquipmentSlot.MainHand)
                            UpdateExpertise(WeaponAttackType.BaseAttack);
                        else if (slot == EquipmentSlot.OffHand)
                            UpdateExpertise(WeaponAttackType.OffAttack);

                        // equipment visual show
                        SetVisibleItemSlot(slot, null);
                    }

                    m_items[slot] = null;
                }
                else if ((pBag = GetBagByPos(bag)) != null)
                    pBag.RemoveItem(slot, update);

                // Delete rolled money / loot from db.
                // MUST be done before RemoveFromWorld() or GetTemplate() fails
                if (pProto.HasFlag(ItemFlags.HasLoot))
                    Global.LootItemStorage.RemoveStoredLootForContainer(pItem.GetGUID().GetCounter());

                ItemRemovedQuestCheck(pItem.GetEntry(), pItem.GetCount());

                if (IsInWorld && update)
                {
                    pItem.RemoveFromWorld();
                    pItem.DestroyForPlayer(this);
                }

                //pItem.SetOwnerGUID(ObjectGuid.Empty);
                pItem.SetContainedIn(ObjectGuid.Empty);
                pItem.SetSlot(ItemConst.NullSlot);
                pItem.SetState(ItemUpdateState.Removed, this);

                if (pProto.GetInventoryType() != InventoryType.NonEquip)
                    UpdateAverageItemLevelTotal();

                if (bag == InventorySlots.Bag0)
                    UpdateAverageItemLevelEquipped();
            }
        }

        public uint DestroyItemCount(uint itemEntry, uint count, bool update, bool unequip_check = true)
        {
            Log.outDebug(LogFilter.Player, "STORAGE: DestroyItemCount item = {0}, count = {1}", itemEntry, count);
            uint remcount = 0;

            // in inventory
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                {
                    if (item.GetEntry() == itemEntry && !item.IsInTrade())
                    {
                        if (item.GetCount() + remcount <= count)
                        {
                            // all items in inventory can unequipped
                            remcount += item.GetCount();
                            DestroyItem(InventorySlots.Bag0, i, update);

                            if (remcount >= count)
                                return remcount;
                        }
                        else
                        {
                            item.SetCount(item.GetCount() - count + remcount);
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return count;
                        }
                    }
                }
            }

            // in inventory bags
            for (byte i = InventorySlots.BagStart; i < InventorySlots.ReagentBagEnd; i++)
            {
                Bag bag = GetBagByPos(i);
                if (bag != null)
                {
                    for (byte j = 0; j < bag.GetBagSize(); j++)
                    {
                        Item item = bag.GetItemByPos(j);
                        if (item != null)
                        {
                            if (item.GetEntry() == itemEntry && !item.IsInTrade())
                            {
                                // all items in bags can be unequipped
                                if (item.GetCount() + remcount <= count)
                                {
                                    remcount += item.GetCount();
                                    DestroyItem(i, j, update);

                                    if (remcount >= count)
                                        return remcount;
                                }
                                else
                                {
                                    item.SetCount(item.GetCount() - count + remcount);
                                    ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                                    if (IsInWorld && update)
                                        item.SendUpdateToPlayer(this);
                                    item.SetState(ItemUpdateState.Changed, this);
                                    return count;
                                }
                            }
                        }
                    }
                }
            }

            // in equipment and bag list
            for (byte i = EquipmentSlot.Start; i < InventorySlots.ReagentBagEnd; i++)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                {
                    if (item.GetEntry() == itemEntry && !item.IsInTrade())
                    {
                        if (item.GetCount() + remcount <= count)
                        {
                            if (!unequip_check || CanUnequipItem((ushort)(InventorySlots.Bag0 << 8 | i), false) == InventoryResult.Ok)
                            {
                                remcount += item.GetCount();
                                DestroyItem(InventorySlots.Bag0, i, update);

                                if (remcount >= count)
                                    return remcount;
                            }
                        }
                        else
                        {
                            item.SetCount(item.GetCount() - count + remcount);
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return count;
                        }
                    }
                }
            }

            // in bank bags
            for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
            {
                Bag bag = GetBagByPos(i);
                if (bag != null)
                {
                    for (byte j = 0; j < bag.GetBagSize(); j++)
                    {
                        Item item = bag.GetItemByPos(j);
                        if (item != null)
                        {
                            if (item.GetEntry() == itemEntry && !item.IsInTrade())
                            {
                                // all items in bags can be unequipped
                                if (item.GetCount() + remcount <= count)
                                {
                                    remcount += item.GetCount();
                                    DestroyItem(i, j, update);

                                    if (remcount >= count)
                                        return remcount;
                                }
                                else
                                {
                                    item.SetCount(item.GetCount() - count + remcount);
                                    ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                                    if (IsInWorld && update)
                                        item.SendUpdateToPlayer(this);
                                    item.SetState(ItemUpdateState.Changed, this);
                                    return count;
                                }
                            }
                        }
                    }
                }
            }

            // in bank bag list
            for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                {
                    if (item.GetEntry() == itemEntry && !item.IsInTrade())
                    {
                        if (item.GetCount() + remcount <= count)
                        {
                            if (!unequip_check || CanUnequipItem((ushort)(InventorySlots.Bag0 << 8 | i), false) == InventoryResult.Ok)
                            {
                                remcount += item.GetCount();
                                DestroyItem(InventorySlots.Bag0, i, update);
                                if (remcount >= count)
                                    return remcount;
                            }
                        }
                        else
                        {
                            item.SetCount(item.GetCount() - count + remcount);
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return count;
                        }
                    }
                }
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                {
                    if (item.GetEntry() == itemEntry && !item.IsInTrade())
                    {
                        if (item.GetCount() + remcount <= count)
                        {
                            // all keys can be unequipped
                            remcount += item.GetCount();
                            DestroyItem(InventorySlots.Bag0, i, update);

                            if (remcount >= count)
                                return remcount;
                        }
                        else
                        {
                            item.SetCount(item.GetCount() - count + remcount);
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return count;
                        }
                    }
                }
            }

            return remcount;
        }
        public void DestroyItemCount(Item pItem, ref uint count, bool update)
        {
            if (pItem == null)
                return;

            Log.outDebug(LogFilter.Player, "STORAGE: DestroyItemCount item (GUID: {0}, Entry: {1}) count = {2}", pItem.GetGUID().ToString(), pItem.GetEntry(), count);

            if (pItem.GetCount() <= count)
            {
                count -= pItem.GetCount();

                DestroyItem(pItem.GetBagSlot(), pItem.GetSlot(), update);
            }
            else
            {
                ItemRemovedQuestCheck(pItem.GetEntry(), count);
                pItem.SetCount(pItem.GetCount() - count);
                count = 0;
                if (IsInWorld && update)
                    pItem.SendUpdateToPlayer(this);
                pItem.SetState(ItemUpdateState.Changed, this);
            }
        }
        public void AutoStoreLoot(uint loot_id, LootStore store, ItemContext context = 0, bool broadcast = false, bool createdByPlayer = false) { AutoStoreLoot(ItemConst.NullBag, ItemConst.NullSlot, loot_id, store, context, broadcast); }

        void AutoStoreLoot(byte bag, byte slot, uint loot_id, LootStore store, ItemContext context = 0, bool broadcast = false, bool createdByPlayer = false)
        {
            Loot loot = new(null, ObjectGuid.Empty, LootType.None, null);
            loot.FillLoot(loot_id, store, this, true, false, LootModes.Default, context);

            loot.AutoStore(this, bag, slot, broadcast, createdByPlayer);
            Unit.ProcSkillsAndAuras(this, null, new ProcFlagsInit(ProcFlags.Looted), new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
        }

        public byte GetInventorySlotCount() { return m_activePlayerData.NumBackpackSlots; }
        public void SetInventorySlotCount(byte slots)
        {
            //ASSERT(slots <= (INVENTORY_SLOT_ITEM_END - INVENTORY_SLOT_ITEM_START));

            if (slots < GetInventorySlotCount())
            {
                List<Item> unstorableItems = new();

                for (byte slot = (byte)(InventorySlots.ItemStart + slots); slot < InventorySlots.ItemEnd; ++slot)
                {
                    Item unstorableItem = GetItemByPos(InventorySlots.Bag0, slot);
                    if (unstorableItem != null)
                        unstorableItems.Add(unstorableItem);
                }

                if (!unstorableItems.Empty())
                {
                    int fullBatches = unstorableItems.Count / SharedConst.MaxMailItems;
                    int remainder = unstorableItems.Count % SharedConst.MaxMailItems;
                    SQLTransaction trans = new();

                    var sendItemsBatch = new Action<int, int>((batchNumber, batchSize) =>
                    {
                        MailDraft draft = new(Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem), "There were problems with equipping item(s).");
                        for (int j = 0; j < batchSize; ++j)
                            draft.AddItem(unstorableItems[batchNumber * SharedConst.MaxMailItems + j]);

                        draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);
                    });

                    for (int batch = 0; batch < fullBatches; ++batch)
                        sendItemsBatch(batch, SharedConst.MaxMailItems);

                    if (remainder != 0)
                        sendItemsBatch(fullBatches, remainder);

                    DB.Characters.CommitTransaction(trans);

                    SendPacket(new InventoryFullOverflow());
                }
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.NumBackpackSlots), slots);
        }

        public byte GetBankBagSlotCount() { return m_activePlayerData.NumBankSlots; }
        public void SetBankBagSlotCount(byte count) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.NumBankSlots), count); }
        public byte GetCharacterBankTabCount() { return m_activePlayerData.NumCharacterBankTabs; }
        public void SetCharacterBankTabCount(byte count) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.NumCharacterBankTabs), count); }
        public byte GetAccountBankTabCount() { return m_activePlayerData.NumAccountBankTabs; }
        public void SetAccountBankTabCount(byte count) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.NumAccountBankTabs), count); }
        public void SetCharacterBankTabSettings(uint tabId, string name, string icon, string description, BagSlotFlags depositFlags)
        {
            var setter = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.CharacterBankTabSettings, (int)tabId);
            SetBankTabSettings(setter, name, icon, description, depositFlags);
        }
        public void SetAccountBankTabSettings(uint tabId, string name, string icon, string description, BagSlotFlags depositFlags)
        {
            var setter = m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.AccountBankTabSettings, (int)tabId);
            SetBankTabSettings(setter, name, icon, description, depositFlags);
        }
        public void SetBankTabSettings(BankTabSettings bantTabSettings, string name, string icon, string description, BagSlotFlags depositFlags)
        {
            SetUpdateFieldValue(bantTabSettings.ModifyValue(bantTabSettings.Name), name);
            SetUpdateFieldValue(bantTabSettings.ModifyValue(bantTabSettings.Icon), icon);
            SetUpdateFieldValue(bantTabSettings.ModifyValue(bantTabSettings.Description), description);
            SetUpdateFieldValue(bantTabSettings.ModifyValue(bantTabSettings.DepositFlags), (int)depositFlags);
        }

        public bool IsBackpackAutoSortDisabled() { return m_activePlayerData.BackpackAutoSortDisabled; }
        public void SetBackpackAutoSortDisabled(bool disabled) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BackpackAutoSortDisabled), disabled); }
        public bool IsBackpackSellJunkDisabled() { return m_activePlayerData.BackpackSellJunkDisabled; }
        public void SetBackpackSellJunkDisabled(bool disabled) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BackpackSellJunkDisabled), disabled); }
        public bool IsBankAutoSortDisabled() { return m_activePlayerData.BankAutoSortDisabled; }
        public void SetBankAutoSortDisabled(bool disabled) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BankAutoSortDisabled), disabled); }
        public BagSlotFlags GetBagSlotFlags(int bagIndex) { return (BagSlotFlags)m_activePlayerData.BagSlotFlags[bagIndex]; }
        public void SetBagSlotFlag(int bagIndex, BagSlotFlags flags) { SetUpdateFieldFlagValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BagSlotFlags, bagIndex), (uint)flags); }
        public void RemoveBagSlotFlag(int bagIndex, BagSlotFlags flags) { RemoveUpdateFieldFlagValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BagSlotFlags, bagIndex), (uint)flags); }
        public void ReplaceAllBagSlotFlags(int bagIndex, BagSlotFlags flags) { SetUpdateFieldValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.BagSlotFlags, bagIndex), (uint)flags); }

        //Loot
        public ObjectGuid GetLootGUID() { return m_playerData.LootTargetGUID; }
        public void SetLootGUID(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.LootTargetGUID), guid); }
        public void StoreLootItem(ObjectGuid lootWorldObjectGuid, byte lootSlot, Loot loot, AELootResult aeResult = null)
        {
            LootItem item = loot.LootItemInSlot(lootSlot, this, out NotNormalLootItem ffaItem);
            if (item == null || item.is_looted)
            {
                SendEquipError(InventoryResult.LootGone);
                return;
            }

            if (!item.HasAllowedLooter(GetGUID()))
            {
                SendLootReleaseAll();
                return;
            }

            if (item.is_blocked)
            {
                SendLootReleaseAll();
                return;
            }

            // dont allow protected item to be looted by someone else
            if (!item.rollWinnerGUID.IsEmpty() && item.rollWinnerGUID != GetGUID())
            {
                SendLootReleaseAll();
                return;
            }

            switch (item.type)
            {
                case LootItemType.Item:
                    List<ItemPosCount> dest = new();
                    InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.itemid, item.count);
                    if (msg != InventoryResult.Ok)
                    {
                        SendEquipError(msg, null, null, item.itemid);
                        return;
                    }

                    Item newitem = StoreNewItem(dest, item.itemid, true, item.randomBonusListId, item.GetAllowedLooters(), item.context);
                    if (newitem != null && (newitem.GetQuality() > ItemQuality.Epic || (newitem.GetQuality() == ItemQuality.Epic && newitem.GetItemLevel(this) >= GuildConst.MinNewsItemLevel)))
                    {
                        Guild guild = GetGuild();
                        if (guild != null)
                            guild.AddGuildNews(GuildNews.ItemLooted, GetGUID(), 0, item.itemid);
                    }

                    // if aeLooting then we must delay sending out item so that it appears properly stacked in chat
                    if (aeResult == null || newitem == null)
                    {
                        SendNewItem(newitem, item.count, false, false, true, loot.GetDungeonEncounterId());
                        UpdateCriteria(CriteriaType.LootItem, item.itemid, item.count);
                        UpdateCriteria(CriteriaType.GetLootByType, item.itemid, item.count, (uint)SharedConst.GetLootTypeForClient(loot.loot_type));
                        UpdateCriteria(CriteriaType.LootAnyItem, item.itemid, item.count);
                    }
                    else
                        aeResult.Add(newitem, (byte)item.count, SharedConst.GetLootTypeForClient(loot.loot_type), loot.GetDungeonEncounterId());

                    if (newitem != null)
                        ApplyItemLootedSpell(newitem, true);
                    else
                        ApplyItemLootedSpell(Global.ObjectMgr.GetItemTemplate(item.itemid));
                    break;
                case LootItemType.Currency:
                    ModifyCurrency(item.itemid, (int)item.count, CurrencyGainSource.Loot);
                    break;
                case LootItemType.TrackingQuest:
                    // nothing to do, already handled
                    break;
            }

            if (ffaItem != null)
            {
                //freeforall case, notify only one player of the removal
                ffaItem.is_looted = true;
                SendNotifyLootItemRemoved(loot.GetGUID(), loot.GetOwnerGUID(), lootSlot);
            }
            else    //not freeforall, notify everyone
                loot.NotifyItemRemoved(lootSlot, GetMap());

            //if only one person is supposed to loot the item, then set it to looted
            if (!item.freeforall)
                item.is_looted = true;

            --loot.unlootedCount;

            // LootItem is being removed (looted) from the container, delete it from the DB.
            if (loot.loot_type == LootType.Item)
                Global.LootItemStorage.RemoveStoredLootItemForContainer(lootWorldObjectGuid.GetCounter(), item.type, item.itemid, item.count, item.LootListId);
        }

        public Dictionary<ObjectGuid, Loot> GetAELootView() { return m_AELootView; }

        /// <summary>
        /// if in a Battleground a player dies, and an enemy removes the insignia, the player's bones is lootable
        /// Called by remove insignia spell effect
        /// </summary>
        /// <param name="looterPlr"></param>
        public void RemovedInsignia(Player looterPlr)
        {
            // If player is not in battleground and not in worldpvpzone
            if (GetBattlegroundId() == 0 && !IsInWorldPvpZone())
                return;

            // If not released spirit, do it !
            if (m_deathTimer > 0)
            {
                m_deathTimer = 0;
                BuildPlayerRepop();
                RepopAtGraveyard();
            }

            _corpseLocation = new WorldLocation();

            // We have to convert player corpse to bones, not to be able to resurrect there
            // SpawnCorpseBones isn't handy, 'cos it saves player while he in BG
            Corpse bones = GetMap().ConvertCorpseToBones(GetGUID(), true);
            if (bones == null)
                return;

            // Now we must make bones lootable, and send player loot
            bones.SetCorpseDynamicFlag(CorpseDynFlags.Lootable);

            bones.loot = new Loot(GetMap(), bones.GetGUID(), LootType.Insignia, looterPlr.GetGroup());

            // For AV Achievement
            Battleground bg = GetBattleground();
            if (bg != null)
            {
                if (bg.GetTypeID() == BattlegroundTypeId.AV)
                    bones.loot.FillLoot(1, LootStorage.Creature, this, true);
            }
            // For wintergrasp Quests
            else if (GetZoneId() == (uint)AreaId.Wintergrasp)
                bones.loot.FillLoot(1, LootStorage.Creature, this, true);

            // It may need a better formula
            // Now it works like this: lvl10: ~6copper, lvl70: ~9silver
            bones.loot.gold = (uint)(RandomHelper.URand(50, 150) * 0.016f * Math.Pow((float)GetLevel() / 5.76f, 2.5f) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
            bones.lootRecipient = looterPlr;
            looterPlr.SendLoot(bones.loot);
        }

        public void SendLootRelease(ObjectGuid guid)
        {
            LootReleaseResponse packet = new();
            packet.LootObj = guid;
            packet.Owner = GetGUID();
            SendPacket(packet);
        }

        public void SendLootReleaseAll()
        {
            SendPacket(new LootReleaseAll());
        }

        public void SendLoot(Loot loot, bool aeLooting = false)
        {
            if (!GetLootGUID().IsEmpty() && !aeLooting)
                _session.DoLootReleaseAll();

            Log.outDebug(LogFilter.Loot, $"Player::SendLoot: Player: '{GetName()}' ({GetGUID()}), Loot: {loot.GetOwnerGUID()}");

            if (!loot.GetOwnerGUID().IsItem() && !aeLooting)
                SetLootGUID(loot.GetOwnerGUID());

            LootResponse packet = new();
            packet.Owner = loot.GetOwnerGUID();
            packet.LootObj = loot.GetGUID();
            packet.LootMethod = loot.GetLootMethod();
            packet.AcquireReason = (byte)SharedConst.GetLootTypeForClient(loot.loot_type);
            packet.Acquired = true; // false == No Loot (this too^^)
            packet.AELooting = aeLooting;
            loot.BuildLootResponse(packet, this);
            SendPacket(packet);

            // add 'this' player as one of the players that are looting 'loot'
            loot.OnLootOpened(GetMap(), this);
            m_AELootView[loot.GetGUID()] = loot;

            if (loot.loot_type == LootType.Corpse && !loot.GetOwnerGUID().IsItem())
                SetUnitFlag(UnitFlags.Looting);
        }

        public void SendLootError(ObjectGuid lootObj, ObjectGuid owner, LootError error)
        {
            LootResponse packet = new();
            packet.LootObj = lootObj;
            packet.Owner = owner;
            packet.Acquired = false;
            packet.FailureReason = error;
            SendPacket(packet);
        }

        public void SendNotifyLootMoneyRemoved(ObjectGuid lootObj)
        {
            CoinRemoved packet = new();
            packet.LootObj = lootObj;
            SendPacket(packet);
        }

        public void SendNotifyLootItemRemoved(ObjectGuid lootObj, ObjectGuid owner, byte lootListId)
        {
            LootRemoved packet = new();
            packet.LootObj = lootObj;
            packet.Owner = owner;
            packet.LootListID = lootListId;
            SendPacket(packet);
        }

        void SendEquipmentSetList()
        {
            LoadEquipmentSet data = new();

            foreach (var pair in _equipmentSets)
            {
                if (pair.Value.state == EquipmentSetUpdateState.Deleted)
                    continue;

                data.SetData.Add(pair.Value.Data);
            }

            SendPacket(data);
        }

        public void SetEquipmentSet(EquipmentSetInfo.EquipmentSetData newEqSet)
        {
            if (newEqSet.Guid != 0)
            {
                // something wrong...
                var equipmentSetInfo = _equipmentSets.LookupByKey(newEqSet.Guid);
                if (equipmentSetInfo == null || equipmentSetInfo.Data.Guid != newEqSet.Guid)
                {
                    Log.outError(LogFilter.Player, "Player {0} tried to save equipment set {1} (index: {2}), but that equipment set not found!", GetName(), newEqSet.Guid, newEqSet.SetID);
                    return;
                }
            }

            ulong setGuid = (newEqSet.Guid != 0) ? newEqSet.Guid : Global.ObjectMgr.GenerateEquipmentSetGuid();

            if (!_equipmentSets.ContainsKey(setGuid))
                _equipmentSets[setGuid] = new EquipmentSetInfo();

            EquipmentSetInfo eqSlot = _equipmentSets[setGuid];
            eqSlot.Data = newEqSet;

            if (eqSlot.Data.Guid == 0)
            {
                eqSlot.Data.Guid = setGuid;

                EquipmentSetID data = new();
                data.GUID = eqSlot.Data.Guid;
                data.Type = (int)eqSlot.Data.Type;
                data.SetID = eqSlot.Data.SetID;
                SendPacket(data);
            }

            eqSlot.state = eqSlot.state == EquipmentSetUpdateState.New ? EquipmentSetUpdateState.New : EquipmentSetUpdateState.Changed;
        }

        public void DeleteEquipmentSet(ulong id)
        {
            foreach (var pair in _equipmentSets)
            {
                if (pair.Value.Data.Guid == id)
                {
                    if (pair.Value.state == EquipmentSetUpdateState.New)
                        _equipmentSets.Remove(pair.Key);
                    else
                        pair.Value.state = EquipmentSetUpdateState.Deleted;
                    break;
                }
            }
        }

        //Misc
        void UpdateItemLevelAreaBasedScaling()
        {
            // @todo Activate pvp item levels during world pvp
            Map map = GetMap();
            bool pvpActivity = map.IsBattlegroundOrArena() || ((int)map.GetEntry().Flags[1]).HasAnyFlag(0x40) || HasPvpRulesEnabled();

            if (_usePvpItemLevels != pvpActivity)
            {
                float healthPct = GetHealthPct();
                _RemoveAllItemMods();
                ActivatePvpItemLevels(pvpActivity);
                _ApplyAllItemMods();
                SetHealth(MathFunctions.CalculatePct(GetMaxHealth(), healthPct));
            }
            // @todo other types of power scaling such as timewalking
        }

        public bool ForEachItem(ItemSearchLocation location, Func<Item, bool> callback)
        {
            if (location.HasAnyFlag(ItemSearchLocation.Equipment))
            {
                for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
                {
                    Item item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                        if (!callback(item))
                            return false;
                }

                for (byte i = ProfessionSlots.Start; i < ProfessionSlots.End; ++i)
                {
                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem != null)
                        if (!callback(pItem))
                            return false;
                }
            }

            if (location.HasAnyFlag(ItemSearchLocation.Inventory))
            {
                int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
                for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
                {
                    Item item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                        if (!callback(item))
                            return false;
                }

                for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
                {
                    Item item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item != null)
                        if (!callback(item))
                            return false;
                }

                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                {
                    Bag bag = GetBagByPos(i);
                    if (bag != null)
                    {
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                        {
                            Item pItem = bag.GetItemByPos(j);
                            if (pItem != null)
                                if (!callback(pItem))
                                    return false;
                        }
                    }
                }
            }

            if (location.HasAnyFlag(ItemSearchLocation.Bank))
            {
                for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; ++i)
                {
                    Bag bag = GetBagByPos(i);
                    if (bag != null)
                    {
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                        {
                            Item pItem = bag.GetItemByPos(j);
                            if (pItem != null)
                                if (!callback(pItem))
                                    return false;
                        }
                    }
                }
            }

            if (location.HasAnyFlag(ItemSearchLocation.ReagentBank))
            {
                for (byte i = InventorySlots.ReagentBagStart; i < InventorySlots.ReagentBagEnd; ++i)
                {
                    Bag bag = GetBagByPos(i);
                    if (bag != null)
                    {
                        for (byte j = 0; j < bag.GetBagSize(); ++j)
                        {
                            Item pItem = bag.GetItemByPos(j);
                            if (pItem != null)
                                if (!callback(pItem))
                                    return false;
                        }
                    }
                }
            }

            return true;
        }

        delegate void EquipmentSlotDelegate(byte equipmentSlot, bool checkDuplicateGuid = false);
        bool ForEachEquipmentSlot(InventoryType inventoryType, bool canDualWield, bool canTitanGrip, EquipmentSlotDelegate callback)
        {
            switch (inventoryType)
            {
                case InventoryType.Head:
                    callback(EquipmentSlot.Head);
                    return true;
                case InventoryType.Neck:
                    callback(EquipmentSlot.Neck);
                    return true;
                case InventoryType.Shoulders:
                    callback(EquipmentSlot.Shoulders);
                    return true;
                case InventoryType.Body:
                    callback(EquipmentSlot.Shirt);
                    return true;
                case InventoryType.Robe:
                case InventoryType.Chest:
                    callback(EquipmentSlot.Chest);
                    return true;
                case InventoryType.Waist:
                    callback(EquipmentSlot.Waist);
                    return true;
                case InventoryType.Legs:
                    callback(EquipmentSlot.Legs);
                    return true;
                case InventoryType.Feet:
                    callback(EquipmentSlot.Feet);
                    return true;
                case InventoryType.Wrists:
                    callback(EquipmentSlot.Wrist);
                    return true;
                case InventoryType.Hands:
                    callback(EquipmentSlot.Hands);
                    return true;
                case InventoryType.Cloak:
                    callback(EquipmentSlot.Cloak);
                    return true;
                case InventoryType.Finger:
                    callback(EquipmentSlot.Finger1);
                    callback(EquipmentSlot.Finger2, true);
                    return true;
                case InventoryType.Trinket:
                    callback(EquipmentSlot.Trinket1);
                    callback(EquipmentSlot.Trinket2, true);
                    return true;
                case InventoryType.Weapon:
                    callback(EquipmentSlot.MainHand);
                    if (canDualWield)
                        callback(EquipmentSlot.OffHand, true);
                    return true;
                case InventoryType.Weapon2Hand:
                    callback(EquipmentSlot.MainHand);
                    if (canDualWield && canTitanGrip)
                        callback(EquipmentSlot.OffHand, true);
                    return true;
                case InventoryType.Ranged:
                case InventoryType.RangedRight:
                case InventoryType.WeaponMainhand:
                    callback(EquipmentSlot.MainHand);
                    return true;
                case InventoryType.Shield:
                case InventoryType.Holdable:
                case InventoryType.WeaponOffhand:
                    callback(EquipmentSlot.OffHand);
                    return true;
                default:
                    return false;
            }
        }

        public void UpdateAverageItemLevelTotal()
        {
            var bestItemLevels = new (InventoryType inventoryType, uint itemLevel, ObjectGuid guid)[EquipmentSlot.End];
            float sum = 0;

            ForEachItem(ItemSearchLocation.Everywhere, item =>
            {
                ItemTemplate itemTemplate = item.GetTemplate();
                if (itemTemplate != null && itemTemplate.GetInventoryType() < InventoryType.ProfessionTool)
                {
                    ushort dest;
                    if (item.IsEquipped())
                    {
                        uint itemLevel = item.GetItemLevel(this);
                        InventoryType inventoryType = itemTemplate.GetInventoryType();
                        ref var slotData = ref bestItemLevels[item.GetSlot()];
                        if (itemLevel > slotData.Item2)
                        {
                            sum += itemLevel - slotData.Item2;
                            slotData = (inventoryType, itemLevel, item.GetGUID());
                        }
                    }
                    else if (CanEquipItem(ItemConst.NullSlot, out dest, item, true, false) == InventoryResult.Ok)
                    {
                        uint itemLevel = item.GetItemLevel(this);
                        InventoryType inventoryType = itemTemplate.GetInventoryType();
                        ForEachEquipmentSlot(inventoryType, m_canDualWield, m_canTitanGrip, (slot, checkDuplicateGuid) =>
                        {
                            if (checkDuplicateGuid)
                            {
                                foreach (var slotData1 in bestItemLevels)
                                    if (slotData1.guid == item.GetGUID())
                                        return;
                            }

                            ref var slotData = ref bestItemLevels[slot];
                            if (itemLevel > slotData.itemLevel)
                            {
                                sum += itemLevel - slotData.itemLevel;
                                slotData = (inventoryType, itemLevel, item.GetGUID());
                            }
                        });
                    }
                }
                return true;
            });

            // If main hand is a 2h weapon, count it twice
            var mainHand = bestItemLevels[EquipmentSlot.MainHand];
            if (!m_canTitanGrip && mainHand.inventoryType == InventoryType.Weapon2Hand)
                sum += mainHand.itemLevel;

            sum /= 16.0f;
            SetAverageItemLevel(sum, AvgItemLevelCategory.Base);
        }

        public void UpdateAverageItemLevelEquipped()
        {
            float totalItemLevel = 0;
            float totalItemLevelEffective = 0;
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                {
                    uint azeriteLevel = 0;
                    AzeriteItem azeriteItem = item.ToAzeriteItem();
                    if (azeriteItem != null)
                        azeriteLevel = azeriteItem.GetEffectiveLevel();

                    uint itemLevel = Item.GetItemLevel(item.GetTemplate(), item.GetBonus(), GetLevel(), item.GetModifier(ItemModifier.TimewalkerLevel), 0, 0, 0, false, azeriteLevel);
                    uint itemLevelEffective = Item.GetItemLevel(item.GetTemplate(), item.GetBonus(), GetEffectiveLevel(), item.GetModifier(ItemModifier.TimewalkerLevel), m_unitData.MinItemLevel,
                        m_unitData.MinItemLevelCutoff, IsUsingPvpItemLevels() && item.GetTemplate().HasFlag(ItemFlags3.IgnoreItemLevelCapInPvp) ? 0 : m_unitData.MaxItemLevel, IsUsingPvpItemLevels(), azeriteLevel);
                    totalItemLevel += itemLevel;
                    totalItemLevelEffective += itemLevelEffective;
                    if (!m_canTitanGrip && i == EquipmentSlot.MainHand && item.GetTemplate().GetInventoryType() == InventoryType.Weapon2Hand) // 2h weapon counts twice
                    {
                        totalItemLevel += itemLevel;
                        totalItemLevelEffective += itemLevelEffective;
                    }
                }
            }

            totalItemLevel /= 16.0f;
            totalItemLevelEffective /= 16.0f;
            SetAverageItemLevel(totalItemLevel, AvgItemLevelCategory.EquippedBase);
            SetAverageItemLevel(totalItemLevelEffective, AvgItemLevelCategory.EquippedEffective);
        }
    }
}
