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
using Game.Arenas;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Groups;
using Game.Guilds;
using Game.Loots;
using Game.Mails;
using Game.Maps;
using Game.Network.Packets;
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
            if (!item.HasFlag(ItemFields.Flags, ItemFieldFlags.Refundable))
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
                    List<ItemPosCount> dest = new List<ItemPosCount>();
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
            SQLTransaction trans = new SQLTransaction();

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
                    List<ItemPosCount> dest = new List<ItemPosCount>();
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
                    ModifyCurrency((CurrencyTypes)currencyid, (int)count, true, true);
            }

            // Grant back money
            if (moneyRefund != 0)
                ModifyMoney((long)moneyRefund); // Saved in SaveInventoryAndGoldToDB

            SaveInventoryAndGoldToDB(trans);

            DB.Characters.CommitTransaction(trans);
        }
        public void SendRefundInfo(Item item)
        {
            // This function call unsets ITEM_FLAGS_REFUNDABLE if played time is over 2 hours.
            item.UpdatePlayedTime(this);

            if (!item.HasFlag(ItemFields.Flags, ItemFieldFlags.Refundable))
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
            SetItemPurchaseData setItemPurchaseData = new SetItemPurchaseData();
            setItemPurchaseData.ItemGUID = item.GetGUID();
            setItemPurchaseData.PurchaseTime = GetTotalPlayedTime() - item.GetPlayedTime();
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
            ItemPurchaseRefundResult itemPurchaseRefundResult = new ItemPurchaseRefundResult();
            itemPurchaseRefundResult.ItemGUID = item.GetGUID();
            itemPurchaseRefundResult.Result = error;
            if (error == 0)
            {
                itemPurchaseRefundResult.Contents.HasValue = true;
                itemPurchaseRefundResult.Contents.Value.Money = item.GetPaidMoney();
                for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i) // item cost data
                {
                    itemPurchaseRefundResult.Contents.Value.Items[i].ItemCount = iece.ItemCount[i];
                    itemPurchaseRefundResult.Contents.Value.Items[i].ItemID = iece.ItemID[i];
                }

                for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i) // currency cost data
                {
                    if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
                        continue;

                    itemPurchaseRefundResult.Contents.Value.Currencies[i].CurrencyCount = iece.CurrencyCount[i];
                    itemPurchaseRefundResult.Contents.Value.Currencies[i].CurrencyID = iece.CurrencyID[i];
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
                if (!item || item.GetOwnerGUID() != GetGUID() || item.CheckSoulboundTradeExpire())
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

            uint pMaxDurability = item.GetUInt32Value(ItemFields.MaxDurability);

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

            int pMaxDurability = item.GetInt32Value(ItemFields.MaxDurability);
            int pOldDurability = item.GetInt32Value(ItemFields.Durability);
            int pNewDurability = pOldDurability - points;

            if (pNewDurability < 0)
                pNewDurability = 0;
            else if (pNewDurability > pMaxDurability)
                pNewDurability = pMaxDurability;

            if (pOldDurability != pNewDurability)
            {
                // modify item stats _before_ Durability set to 0 to pass _ApplyItemMods internal check
                if (pNewDurability == 0 && pOldDurability > 0 && item.IsEquipped())
                    _ApplyItemMods(item, item.GetSlot(), false);

                item.SetInt32Value(ItemFields.Durability, pNewDurability);

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
        public uint DurabilityRepairAll(bool cost, float discountMod, bool guildBank)
        {
            uint TotalCost = 0;
            // equipped, backpack, bags itself
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; i++)
                TotalCost += DurabilityRepair((ushort)((InventorySlots.Bag0 << 8) | i), cost, discountMod, guildBank);

            // items in inventory bags
            for (byte j = InventorySlots.BagStart; j < InventorySlots.BagEnd; j++)
                for (byte i = 0; i < ItemConst.MaxBagSize; i++)
                    TotalCost += DurabilityRepair((ushort)((j << 8) | i), cost, discountMod, guildBank);
            return TotalCost;
        }
        public uint DurabilityRepair(ushort pos, bool cost, float discountMod, bool guildBank)
        {
            Item item = GetItemByPos(pos);

            uint TotalCost = 0;
            if (item == null)
                return TotalCost;

            uint maxDurability = item.GetUInt32Value(ItemFields.MaxDurability);
            if (maxDurability == 0)
                return TotalCost;

            uint curDurability = item.GetUInt32Value(ItemFields.Durability);

            if (cost)
            {
                uint LostDurability = maxDurability - curDurability;
                if (LostDurability > 0)
                {
                    ItemTemplate ditemProto = item.GetTemplate();

                    DurabilityCostsRecord dcost = CliDB.DurabilityCostsStorage.LookupByKey(ditemProto.GetBaseItemLevel());
                    if (dcost == null)
                    {
                        Log.outError(LogFilter.Player, "RepairDurability: Wrong item lvl {0}", ditemProto.GetBaseItemLevel());
                        return TotalCost;
                    }

                    uint dQualitymodEntryId = (uint)(ditemProto.GetQuality() + 1) * 2;
                    DurabilityQualityRecord dQualitymodEntry = CliDB.DurabilityQualityStorage.LookupByKey(dQualitymodEntryId);
                    if (dQualitymodEntry == null)
                    {
                        Log.outError(LogFilter.Player, "RepairDurability: Wrong dQualityModEntry {0}", dQualitymodEntryId);
                        return TotalCost;
                    }

                    uint dmultiplier = 0;
                    if (ditemProto.GetClass() == ItemClass.Weapon)
                        dmultiplier = dcost.WeaponSubClassCost[ditemProto.GetSubClass()];
                    else if (ditemProto.GetClass() == ItemClass.Armor)
                        dmultiplier = dcost.ArmorSubClassCost[ditemProto.GetSubClass()];

                    uint costs = (uint)(LostDurability * dmultiplier * (double)dQualitymodEntry.Data * item.GetRepairCostMultiplier());
                    costs = (uint)(costs * discountMod * WorldConfig.GetFloatValue(WorldCfg.RateRepaircost));

                    if (costs == 0)                                   //fix for ITEM_QUALITY_ARTIFACT
                        costs = 1;

                    if (guildBank)
                    {
                        if (GetGuildId() == 0)
                        {
                            Log.outDebug(LogFilter.Player, "You are not member of a guild");
                            return TotalCost;
                        }

                        var guild = Global.GuildMgr.GetGuildById(GetGuildId());
                        if (guild == null)
                            return TotalCost;

                        if (!guild.HandleMemberWithdrawMoney(GetSession(), costs, true))
                            return TotalCost;

                        TotalCost = costs;
                    }
                    else if (!HasEnoughMoney(costs))
                    {
                        Log.outDebug(LogFilter.Player, "You do not have enough money");
                        return TotalCost;
                    }
                    else
                        ModifyMoney(-costs);
                }
            }

            item.SetUInt32Value(ItemFields.Durability, maxDurability);
            item.SetState(ItemUpdateState.Changed, this);

            // reapply mods for total broken and repaired item if equipped
            if (IsEquipmentPos(pos) && curDurability == 0)
                _ApplyItemMods(item, (byte)(pos & 255), true);
            return TotalCost;
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
            uint throwaway;
            return CanStoreItem(bag, slot, dest, entry, count, pItem, swap, out throwaway);
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
            InventoryResult res = CanTakeMoreSimilarItems(entry, count, pItem, ref no_similar_count);
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
                if (res != InventoryResult.Ok)
                {
                    no_space_count = count + no_similar_count;
                    return res;
                }

                if (count == 0)
                {
                    if (no_similar_count == 0)
                        return InventoryResult.Ok;

                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }
            }

            // not specific slot or have space for partly store only in specific slot
            byte inventoryEnd = (byte)(InventorySlots.ItemStart + GetInventorySlotCount());

            // in specific bag
            if (bag != ItemConst.NullBag)
            {
                // search stack in bag for merge to
                if (pProto.GetMaxStackSize() != 1)
                {
                    if (bag == InventorySlots.Bag0)               // inventory
                    {
                        res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, true, pItem, bag, slot);
                        if (res != InventoryResult.Ok)
                        {
                            no_space_count = count + no_similar_count;
                            return res;
                        }

                        if (count == 0)
                        {
                            if (no_similar_count == 0)
                                return InventoryResult.Ok;

                            no_space_count = count + no_similar_count;
                            return InventoryResult.ItemMaxCount;
                        }

                        res = CanStoreItem_InInventorySlots(InventorySlots.ReagentStart, InventorySlots.ReagentEnd, dest, pProto, ref count, true, pItem, bag, slot);
                        if (res != InventoryResult.Ok)
                        {
                            no_space_count = count + no_similar_count;
                            return res;
                        }

                        if (count == 0)
                        {
                            if (no_similar_count == 0)
                                return InventoryResult.Ok;

                            no_space_count = count + no_similar_count;
                            return InventoryResult.ItemMaxCount;
                        }

                        res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventoryEnd, dest, pProto, ref count, true, pItem, bag, slot);
                        if (res != InventoryResult.Ok)
                        {
                            no_space_count = count + no_similar_count;
                            return res;
                        }

                        if (count == 0)
                        {
                            if (no_similar_count == 0)
                                return InventoryResult.Ok;


                            no_space_count = count + no_similar_count;
                            return InventoryResult.ItemMaxCount;
                        }
                    }
                    else                                            // equipped bag
                    {
                        // we need check 2 time (specialized/non_specialized), use NULL_BAG to prevent skipping bag
                        res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, false, pItem, ItemConst.NullBag, slot);
                        if (res != InventoryResult.Ok)
                            res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, true, pItem, ItemConst.NullBag, slot);

                        if (res != InventoryResult.Ok)
                        {
                            no_space_count = count + no_similar_count;
                            return res;
                        }

                        if (count == 0)
                        {
                            if (no_similar_count == 0)
                                return InventoryResult.Ok;

                            no_space_count = count + no_similar_count;
                            return InventoryResult.ItemMaxCount;
                        }
                    }
                }

                // search free slot in bag for place to
                if (bag == InventorySlots.Bag0)                     // inventory
                {
                    if (pItem && pItem.HasFlag(ItemFields.Flags, ItemFieldFlags.Child))
                    {
                        res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, false, pItem, bag, slot);
                        if (res != InventoryResult.Ok)
                        {
                            no_space_count = count + no_similar_count;
                            return res;
                        }

                        if (count == 0)
                        {
                            if (no_similar_count == 0)
                                return InventoryResult.Ok;

                            no_space_count = count + no_similar_count;
                            return InventoryResult.ItemMaxCount;
                        }
                    }
                    else if (pProto.IsCraftingReagent() && HasFlag(PlayerFields.FlagsEx, PlayerFlagsEx.ReagentBankUnlocked))
                    {
                        res = CanStoreItem_InInventorySlots(InventorySlots.ReagentStart, InventorySlots.ReagentEnd, dest, pProto, ref count, false, pItem, bag, slot);
                        if (res != InventoryResult.Ok)
                        {
                            no_space_count = count + no_similar_count;
                            return res;
                        }

                        if (count == 0)
                        {
                            if (no_similar_count == 0)
                                return InventoryResult.Ok;

                            no_space_count = count + no_similar_count;
                            return InventoryResult.ItemMaxCount;
                        }
                    }

                    res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventoryEnd, dest, pProto, ref count, false, pItem, bag, slot);
                    if (res != InventoryResult.Ok)
                    {
                        no_space_count = count + no_similar_count;
                        return res;
                    }

                    if (count == 0)
                    {
                        if (no_similar_count == 0)
                            return InventoryResult.Ok;

                        no_space_count = count + no_similar_count;
                        return InventoryResult.ItemMaxCount;
                    }
                }
                else                                                // equipped bag
                {
                    res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, false, pItem, ItemConst.NullBag, slot);
                    if (res != InventoryResult.Ok)
                        res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, true, pItem, ItemConst.NullBag, slot);

                    if (res != InventoryResult.Ok)
                    {
                        no_space_count = count + no_similar_count;
                        return res;
                    }

                    if (count == 0)
                    {
                        if (no_similar_count == 0)
                            return InventoryResult.Ok;

                        no_space_count = count + no_similar_count;
                        return InventoryResult.ItemMaxCount;
                    }
                }
            }

            // not specific bag or have space for partly store only in specific bag

            // search stack for merge to
            if (pProto.GetMaxStackSize() != 1)
            {
                res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, true, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                {
                    no_space_count = count + no_similar_count;
                    return res;
                }

                if (count == 0)
                {
                    if (no_similar_count == 0)
                        return InventoryResult.Ok;

                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }

                res = CanStoreItem_InInventorySlots(InventorySlots.ReagentStart, InventorySlots.ReagentEnd, dest, pProto, ref count, true, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                {
                    no_space_count = count + no_similar_count;
                    return res;
                }

                if (count == 0)
                {
                    if (no_similar_count == 0)
                        return InventoryResult.Ok;

                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }

                res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventoryEnd, dest, pProto, ref count, true, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                {
                    no_space_count = count + no_similar_count;
                    return res;
                }

                if (count == 0)
                {
                    if (no_similar_count == 0)
                        return InventoryResult.Ok;

                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }

                if (pProto.GetBagFamily() != 0)
                {
                    for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                    {
                        res = CanStoreItem_InBag(i, dest, pProto, ref count, true, false, pItem, bag, slot);
                        if (res != InventoryResult.Ok)
                            continue;

                        if (count == 0)
                        {
                            if (no_similar_count == 0)
                                return InventoryResult.Ok;

                            no_space_count = count + no_similar_count;
                            return InventoryResult.ItemMaxCount;
                        }
                    }
                }

                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                {
                    res = CanStoreItem_InBag(i, dest, pProto, ref count, true, true, pItem, bag, slot);
                    if (res != InventoryResult.Ok)
                        continue;

                    if (count == 0)
                    {
                        if (no_similar_count == 0)
                            return InventoryResult.Ok;

                        no_space_count = count + no_similar_count;
                        return InventoryResult.ItemMaxCount;
                    }
                }
            }

            // search free slot - special bag case
            if (pProto.GetBagFamily() != 0)
            {
                for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
                {
                    res = CanStoreItem_InBag(i, dest, pProto, ref count, false, false, pItem, bag, slot);
                    if (res != InventoryResult.Ok)
                        continue;

                    if (count == 0)
                    {
                        if (no_similar_count == 0)
                            return InventoryResult.Ok;

                        no_space_count = count + no_similar_count;
                        return InventoryResult.ItemMaxCount;
                    }
                }
            }

            if (pItem != null && pItem.IsNotEmptyBag())
                return InventoryResult.BagInBag;

            if (pItem && pItem.HasFlag(ItemFields.Flags, ItemFieldFlags.Child))
            {
                res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, false, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                {
                    no_space_count = count + no_similar_count;
                    return res;
                }

                if (count == 0)
                {
                    if (no_similar_count == 0)
                        return InventoryResult.Ok;

                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }
            }
            else if (pProto.IsCraftingReagent() && HasFlag(PlayerFields.FlagsEx, PlayerFlagsEx.ReagentBankUnlocked))
            {
                res = CanStoreItem_InInventorySlots(InventorySlots.ReagentStart, InventorySlots.ReagentEnd, dest, pProto, ref count, false, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                {
                    no_space_count = count + no_similar_count;
                    return res;
                }

                if (count == 0)
                {
                    if (no_similar_count == 0)
                        return InventoryResult.Ok;

                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }
            }

            // search free slot
            byte searchSlotStart = InventorySlots.ItemStart;
            // new bags can be directly equipped
            if (!pItem && pProto.GetClass() == ItemClass.Container && (ItemSubClassContainer)pProto.GetSubClass() == ItemSubClassContainer.Container &&
                (pProto.GetBonding() == ItemBondingType.None || pProto.GetBonding() == ItemBondingType.OnAcquire))
                searchSlotStart = InventorySlots.BagStart;

            res = CanStoreItem_InInventorySlots(searchSlotStart, InventorySlots.ItemEnd, dest, pProto, ref count, false, pItem, bag, slot);
            if (res != InventoryResult.Ok)
            {
                no_space_count = count + no_similar_count;
                return res;
            }

            if (count == 0)
            {
                if (no_similar_count == 0)
                    return InventoryResult.Ok;

                no_space_count = count + no_similar_count;
                return InventoryResult.ItemMaxCount;
            }

            for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
            {
                res = CanStoreItem_InBag(i, dest, pProto, ref count, false, true, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                    continue;

                if (count == 0)
                {
                    if (no_similar_count == 0)
                        return InventoryResult.Ok;

                    no_space_count = count + no_similar_count;
                    return InventoryResult.ItemMaxCount;
                }
            }

            no_space_count = count + no_similar_count;

            return InventoryResult.InvFull;
        }
        public InventoryResult CanStoreItems(Item[] items, int count, ref uint offendingItemId)
        {
            Item item2;

            // fill space table
            uint[] inventoryCounts = new uint[InventorySlots.ItemEnd - InventorySlots.ItemStart];
            uint[][] bagCounts = new uint[InventorySlots.BagEnd - InventorySlots.BagStart][];

            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; i++)
            {
                item2 = GetItemByPos(InventorySlots.Bag0, i);
                if (item2 && !item2.IsInTrade())
                    inventoryCounts[i - InventorySlots.ItemStart] = item2.GetCount();
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag)
                {
                    bagCounts[i - InventorySlots.BagStart] = new uint[ItemConst.MaxBagSize];
                    for (byte j = 0; j < pBag.GetBagSize(); j++)
                    {
                        item2 = GetItemByPos(i, j);
                        if (item2 && !item2.IsInTrade())
                            bagCounts[i - InventorySlots.BagStart][j] = item2.GetCount();
                    }
                }
            }

            // check free space for all items
            for (int k = 0; k < count; ++k)
            {
                Item item = items[k];

                // no item
                if (!item)
                    continue;

                Log.outDebug(LogFilter.Player, "STORAGE: CanStoreItems {0}. item = {1}, count = {2}", k + 1, item.GetEntry(), item.GetCount());
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
                        item2 = GetItemByPos(InventorySlots.Bag0, t);
                        if (item2 && item2.CanBeMergedPartlyWith(pProto) == InventoryResult.Ok && inventoryCounts[t - InventorySlots.ItemStart] + item.GetCount() <= pProto.GetMaxStackSize())
                        {
                            inventoryCounts[t - InventorySlots.ItemStart] += item.GetCount();
                            b_found = true;
                            break;
                        }
                    }
                    if (b_found)
                        continue;

                    for (byte t = InventorySlots.BagStart; !b_found && t < InventorySlots.BagEnd; ++t)
                    {
                        Bag bag = GetBagByPos(t);
                        if (bag)
                        {
                            if (Item.ItemCanGoIntoBag(item.GetTemplate(), bag.GetTemplate()))
                            {
                                for (byte j = 0; j < bag.GetBagSize(); j++)
                                {
                                    item2 = GetItemByPos(t, j);
                                    if (item2 && item2.CanBeMergedPartlyWith(pProto) == InventoryResult.Ok && bagCounts[t - InventorySlots.BagStart][j] + item.GetCount() <= pProto.GetMaxStackSize())
                                    {
                                        bagCounts[t - InventorySlots.BagStart][j] += item.GetCount();
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
                b_found = false;
                // special bag case
                if (pProto.GetBagFamily() != 0)
                {
                    for (byte t = InventorySlots.BagStart; !b_found && t < InventorySlots.BagEnd; ++t)
                    {
                        Bag bag = GetBagByPos(t);
                        if (bag)
                        {
                            pBagProto = bag.GetTemplate();

                            // not plain container check
                            if (pBagProto != null && (pBagProto.GetClass() != ItemClass.Container || pBagProto.GetSubClass() != (uint)ItemSubClassContainer.Container) &&
                                Item.ItemCanGoIntoBag(pProto, pBagProto))
                            {
                                for (uint j = 0; j < bag.GetBagSize(); j++)
                                {
                                    if (bagCounts[t - InventorySlots.BagStart][j] == 0)
                                    {
                                        bagCounts[t - InventorySlots.BagStart][j] = 1;
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
                        b_found = true;
                        break;
                    }
                }
                if (b_found)
                    continue;

                // search free slot in bags
                for (byte t = InventorySlots.BagStart; !b_found && t < InventorySlots.BagEnd; ++t)
                {
                    Bag bag = GetBagByPos(t);
                    if (bag)
                    {
                        pBagProto = bag.GetTemplate();

                        // special bag already checked
                        if (pBagProto != null && (pBagProto.GetClass() != ItemClass.Container || pBagProto.GetSubClass() != (uint)ItemSubClassContainer.Container))
                            continue;

                        for (uint j = 0; j < bag.GetBagSize(); j++)
                        {
                            if (bagCounts[t - InventorySlots.BagStart][j] == 0)
                            {
                                bagCounts[t - InventorySlots.BagStart][j] = 1;
                                b_found = true;
                                break;
                            }
                        }
                    }
                }

                // no free slot found?
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
            uint notused;
            return CanStoreItem(bag, slot, dest, item, count, null, false, out notused);
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
                    SetGuidValue(ActivePlayerFields.InvSlotHead + (slot * 4), pItem.GetGUID());
                    pItem.SetGuidValue(ItemFields.Contained, GetGUID());
                    pItem.SetGuidValue(ItemFields.Owner, GetGUID());

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

                if (bag == InventorySlots.Bag0 || (bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd))
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

                if (bag == InventorySlots.Bag0 || (bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd))
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
        bool StoreNewItemInBestSlots(uint titem_id, uint titem_amount)
        {
            Log.outDebug(LogFilter.Player, "STORAGE: Creating initial item, itemId = {0}, count = {1}", titem_id, titem_amount);
            InventoryResult msg;
            // attempt equip by one
            while (titem_amount > 0)
            {
                ushort eDest = 0;
                msg = CanEquipNewItem(ItemConst.NullSlot, out eDest, titem_id, false);
                if (msg != InventoryResult.Ok)
                    break;

                EquipNewItem(eDest, titem_id, true);
                AutoUnequipOffhandIfNeed();
                titem_amount--;
            }

            if (titem_amount == 0)
                return true;                                        // equipped

            // attempt store
            List<ItemPosCount> sDest = new List<ItemPosCount>();
            // store in main bag to simplify second pass (special bags can be not equipped yet at this moment)
            msg = CanStoreNewItem(InventorySlots.Bag0, ItemConst.NullSlot, sDest, titem_id, titem_amount);
            if (msg == InventoryResult.Ok)
            {
                StoreNewItem(sDest, titem_id, true, ItemEnchantment.GenerateItemRandomPropertyId(titem_id));
                return true;                                        // stored
            }

            // item can't be added
            Log.outError(LogFilter.Player, "STORAGE: Can't equip or store initial item {0} for race {1} class {2}, error msg = {3}", titem_id, GetRace(), GetClass(), msg);
            return false;
        }
        public Item StoreNewItem(List<ItemPosCount> pos, uint itemId, bool update, ItemRandomEnchantmentId randomPropertyId = default(ItemRandomEnchantmentId), List<ObjectGuid> allowedLooters = null, byte context = 0, List<uint> bonusListIDs = null, bool addToCollection = true)
        {
            uint count = 0;
            foreach (var itemPosCount in pos)
                count += itemPosCount.count;

            Item item = Item.CreateItem(itemId, count, this);
            if (item != null)
            {
                ItemAddedQuestCheck(itemId, count);
                UpdateCriteria(CriteriaTypes.ReceiveEpicItem, itemId, count);
                UpdateCriteria(CriteriaTypes.OwnItem, itemId, 1);

                item.SetFlag(ItemFields.Flags, ItemFieldFlags.NewItem);

                uint upgradeID = Global.DB2Mgr.GetRulesetItemUpgrade(itemId);
                if (upgradeID != 0)
                    item.SetModifier(ItemModifier.UpgradeId, upgradeID);

                item.SetUInt32Value(ItemFields.Context, context);
                if (bonusListIDs != null)
                {
                    foreach (uint bonusListID in bonusListIDs)
                        item.AddBonuses(bonusListID);
                }

                item = StoreItem(pos, item, update);

                item.SetFixedLevel(getLevel());
                item.SetItemRandomProperties(randomPropertyId);

                if (allowedLooters != null && allowedLooters.Count > 1 && item.GetTemplate().GetMaxStackSize() == 1 && item.IsSoulBound())
                {
                    item.SetSoulboundTradeable(allowedLooters);
                    item.SetUInt32Value(ItemFields.CreatePlayedTime, GetTotalPlayedTime());
                    AddTradeableItem(item);

                    // save data
                    StringBuilder ss = new StringBuilder();
                    foreach (var guid in allowedLooters)
                        ss.AppendFormat("{0} ", guid);

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_BOP_TRADE);
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
                        List<ItemPosCount> childDest = new List<ItemPosCount>();
                        CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, childDest, childTemplate, ref count, false, null, ItemConst.NullBag, ItemConst.NullSlot);
                        Item childItem = StoreNewItem(childDest, childTemplate.GetId(), update, ItemRandomEnchantmentId.Empty, null, context, null, addToCollection);
                        if (childItem)
                        {
                            childItem.SetGuidValue(ItemFields.Creator, item.GetGUID());
                            childItem.SetFlag(ItemFields.Flags, ItemFieldFlags.Child);
                            item.SetChildItem(childItem.GetGUID());
                        }
                    }
                }
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

            // no maximum
            if ((pProto.GetMaxCount() <= 0 && pProto.GetItemLimitCategory() == 0) || pProto.GetMaxCount() == 2147483647)
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
            if (pProto.GetItemLimitCategory() != 0)
            {
                ItemLimitCategoryRecord limitEntry = CliDB.ItemLimitCategoryStorage.LookupByKey(pProto.GetItemLimitCategory());
                if (limitEntry == null)
                {
                    no_space_count = count;
                    return InventoryResult.NotEquippable;
                }

                if (limitEntry.Flags == 0)
                {
                    byte limitQuantity = GetItemLimitCategoryQuantity(limitEntry);
                    uint curcount = GetItemCountWithLimitCategory(pProto.GetItemLimitCategory(), pItem);
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

                    if (getLevel() < pItem.GetRequiredLevel())
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
        public InventoryResult CanUseItem(ItemTemplate proto)
        {
            // Used by group, function GroupLoot, to know if a prototype can be used by a player

            if (proto == null)
                return InventoryResult.ItemNotFound;

            if (proto.GetFlags2().HasAnyFlag(ItemFlags2.InternalItem))
                return InventoryResult.CantEquipEver;

            if (Convert.ToBoolean(proto.GetFlags2() & ItemFlags2.FactionHorde) && GetTeam() != Team.Horde)
                return InventoryResult.CantEquipEver;

            if (Convert.ToBoolean(proto.GetFlags2() & ItemFlags2.FactionAlliance) && GetTeam() != Team.Alliance)
                return InventoryResult.CantEquipEver;

            if ((proto.GetAllowableClass() & getClassMask()) == 0 || (proto.GetAllowableRace() & (long)getRaceMask()) == 0)
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

            if (getLevel() < proto.GetBaseRequiredLevel())
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
                if (artifact.ChrSpecializationID != GetUInt32Value(PlayerFields.CurrentSpecId))
                    return InventoryResult.CantUseItem;

            return InventoryResult.Ok;
        }

        //Equip/Unequip Item
        InventoryResult CanUnequipItems(uint item, uint count)
        {
            uint tempcount = 0;

            InventoryResult res = InventoryResult.Ok;

            for (byte i = EquipmentSlot.Start; i < InventorySlots.BagEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                {
                    if (pItem.GetEntry() == item)
                    {
                        InventoryResult ires = CanUnequipItem((ushort)(InventorySlots.Bag0 << 8 | i), false);
                        if (ires == InventoryResult.Ok)
                        {
                            tempcount += pItem.GetCount();
                            if (tempcount >= count)
                                return InventoryResult.Ok;
                        }
                        else
                            res = ires;
                    }
                }
            }

            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                {
                    if (pItem.GetEntry() == item)
                    {
                        tempcount += pItem.GetCount();
                        if (tempcount >= count)
                            return InventoryResult.Ok;
                    }
                }
            }

            for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                {
                    if (pItem.GetEntry() == item)
                    {
                        tempcount += pItem.GetCount();
                        if (tempcount >= count)
                            return InventoryResult.Ok;
                    }
                }
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                {
                    if (pItem.GetEntry() == item)
                    {
                        tempcount += pItem.GetCount();
                        if (tempcount >= count)
                            return InventoryResult.Ok;
                    }
                }
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                {
                    for (byte j = 0; j < pBag.GetBagSize(); ++j)
                    {
                        Item pItem = GetItemByPos(i, j);
                        if (pItem != null)
                        {
                            if (pItem.GetEntry() == item)
                            {
                                tempcount += pItem.GetCount();
                                if (tempcount >= count)
                                    return InventoryResult.Ok;
                            }
                        }
                    }
                }
            }

            // not found req. item count and have unequippable items
            return res;
        }
        Item EquipNewItem(ushort pos, uint item, bool update)
        {
            Item pItem = Item.CreateItem(item, 1, this);
            if (pItem != null)
            {
                ItemAddedQuestCheck(item, 1);
                UpdateCriteria(CriteriaTypes.ReceiveEpicItem, item, 1);
                return EquipItem(pos, pItem, update);
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
                        var spellProto = Global.SpellMgr.GetSpellInfo(cooldownSpell);

                        if (spellProto == null)
                            Log.outError(LogFilter.Player, "Weapon switch cooldown spell {0} couldn't be found in Spell.dbc", cooldownSpell);
                        else
                        {
                            m_weaponChangeTimer = spellProto.StartRecoveryTime;

                            GetSpellHistory().AddGlobalCooldown(spellProto, m_weaponChangeTimer);

                            SpellCooldownPkt spellCooldown = new SpellCooldownPkt();
                            spellCooldown.Caster = GetGUID();
                            spellCooldown.Flags = SpellCooldownFlags.IncludeGCD;
                            spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(cooldownSpell, 0));
                            SendPacket(spellCooldown);
                        }
                    }
                }

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
            UpdateCriteria(CriteriaTypes.EquipItem, pItem.GetEntry());
            UpdateCriteria(CriteriaTypes.EquipEpicItem, pItem.GetEntry(), slot);

            return pItem;
        }
        public void EquipChildItem(byte parentBag, byte parentSlot, Item parentItem)
        {
            ItemChildEquipmentRecord itemChildEquipment = Global.DB2Mgr.GetItemChildEquipment(parentItem.GetEntry());
            if (itemChildEquipment != null)
            {
                Item childItem = GetChildItemByGuid(parentItem.GetChildItem());
                if (childItem)
                {
                    ushort childDest = (ushort)((InventorySlots.Bag0 << 8) | itemChildEquipment.ChildItemEquipSlot);
                    if (childItem.GetPos() != childDest)
                    {
                        Item dstItem = GetItemByPos(childDest);
                        if (!dstItem)                                      // empty slot, simple case
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
                            List<ItemPosCount> sSrc = new List<ItemPosCount>();
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
                if (childItem)
                {
                    if (IsChildEquipmentPos(childItem.GetPos()))
                        return;

                    List<ItemPosCount> dest = new List<ItemPosCount>();
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

                if (IsInWorld)
                {
                    pItem.AddToWorld();
                    pItem.SendUpdateToPlayer(this);
                }

                if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand)
                    CheckTitanGripPenalty();

                UpdateCriteria(CriteriaTypes.EquipItem, pItem.GetEntry());
                UpdateCriteria(CriteriaTypes.EquipEpicItem, pItem.GetEntry(), slot);
            }
        }
        public void SendEquipError(InventoryResult msg, Item item1 = null, Item item2 = null, uint itemId = 0)
        {
            InventoryChangeFailure failure = new InventoryChangeFailure();
            failure.BagResult = msg;

            if (msg != InventoryResult.Ok)
            {
                if (item1)
                    failure.Item[0] = item1.GetGUID();

                if (item2)
                    failure.Item[1] = item2.GetGUID();

                failure.ContainerBSlot = 0; // bag equip slot, used with EQUIP_ERR_EVENT_AUTOEQUIP_BIND_CONFIRM and EQUIP_ERR_ITEM_DOESNT_GO_INTO_BAG2

                switch (msg)
                {
                    case InventoryResult.CantEquipLevelI:
                    case InventoryResult.PurchaseLevelTooLow:
                        {
                            failure.Level = (item1 ? item1.GetRequiredLevel() : 0);
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
                            ItemTemplate proto = item1 ? item1.GetTemplate() : Global.ObjectMgr.GetItemTemplate(itemId);
                            failure.LimitCategory = (int)(proto != null ? proto.GetItemLimitCategory() : 0u);
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
            uint noSpaceForCount = 0;
            List<ItemPosCount> dest = new List<ItemPosCount>();
            InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, count, out noSpaceForCount);
            if (msg != InventoryResult.Ok)
                count -= noSpaceForCount;

            if (count == 0 || dest.Empty())
            {
                // @todo Send to mailbox if no space
                SendSysMessage("You don't have any space in your bags.");
                return false;
            }

            Item item = StoreNewItem(dest, itemId, true, ItemEnchantment.GenerateItemRandomPropertyId(itemId));
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
                    if (slot < InventorySlots.BagEnd)
                    {
                        ItemTemplate pProto = pItem.GetTemplate();
                        // item set bonuses applied only at equip and removed at unequip, and still active for broken items

                        if (pProto != null && pProto.GetItemSet() != 0)
                            Item.RemoveItemsSetItem(this, pProto);

                        _ApplyItemMods(pItem, slot, false, update);

                        // remove item dependent auras and casts (only weapon and armor slots)
                        if (slot < EquipmentSlot.End)
                        {
                            // remove held enchantments, update expertise
                            if (slot == EquipmentSlot.MainHand)
                            {
                                if (pItem.GetItemSuffixFactor() != 0)
                                {
                                    pItem.ClearEnchantment(EnchantmentSlot.Prop3);
                                    pItem.ClearEnchantment(EnchantmentSlot.Prop4);
                                }
                                else
                                {
                                    pItem.ClearEnchantment(EnchantmentSlot.Prop0);
                                    pItem.ClearEnchantment(EnchantmentSlot.Prop1);
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
                    SetGuidValue(ActivePlayerFields.InvSlotHead + (slot * 4), ObjectGuid.Empty);

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

                pItem.SetGuidValue(ItemFields.Contained, ObjectGuid.Empty);
                pItem.SetSlot(ItemConst.NullSlot);
                if (IsInWorld && update)
                    pItem.SendUpdateToPlayer(this);

                AutoUnequipChildItem(pItem);
            }
        }
        public void SplitItem(ushort src, ushort dst, uint count)
        {
            byte srcbag = (byte)(src >> 8);
            byte srcslot = (byte)(src & 255);

            byte dstbag = (byte)(dst >> 8);
            byte dstslot = (byte)(dst & 255);

            Item pSrcItem = GetItemByPos(srcbag, srcslot);
            if (!pSrcItem)
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
            if (!pNewItem)
            {
                SendEquipError(InventoryResult.ItemNotFound, pSrcItem);
                return;
            }

            if (IsInventoryPos(dst))
            {
                // change item amount before check (for unique max count check)
                pSrcItem.SetCount(pSrcItem.GetCount() - count);

                List<ItemPosCount> dest = new List<ItemPosCount>();
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

                List<ItemPosCount> dest = new List<ItemPosCount>();
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

            if (pSrcItem.HasFlag(ItemFields.Flags, ItemFieldFlags.Child))
            {
                Item parentItem = GetItemByGuid(pSrcItem.GetGuidValue(ItemFields.Creator));
                if (parentItem)
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
            else if (pDstItem && pDstItem.HasFlag(ItemFields.Flags, ItemFieldFlags.Child))
            {
                Item parentItem = GetItemByGuid(pDstItem.GetGuidValue(ItemFields.Creator));
                if (parentItem)
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
                    List<ItemPosCount> dest = new List<ItemPosCount>();
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
                    List<ItemPosCount> dest = new List<ItemPosCount>();
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
                List<ItemPosCount> sDest = new List<ItemPosCount>();
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
            List<ItemPosCount> _sDest = new List<ItemPosCount>();
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
            List<ItemPosCount> sDest2 = new List<ItemPosCount>();
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
            if ((srcbag == InventorySlots.Bag0 && srcslot < InventorySlots.BagEnd) || (dstbag == InventorySlots.Bag0 && dstslot < InventorySlots.BagEnd))
                ApplyItemDependentAuras(null, false);

            // if player is moving bags and is looting an item inside this bag
            // release the loot
            if (!GetLootGUID().IsEmpty())
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
                            if (bagItem.m_lootGenerated)
                            {
                                GetSession().DoLootRelease(GetLootGUID());
                                released = true;                    // so we don't need to look at dstBag
                                break;
                            }
                        }
                    }
                }

                if (!released && IsBagPos(dst) && pDstItem != null)
                {
                    Bag bag = pDstItem.ToBag();
                    for (byte i = 0; i < bag.GetBagSize(); ++i)
                    {
                        Item bagItem = bag.GetItemByPos(i);
                        if (bagItem != null)
                        {
                            if (bagItem.m_lootGenerated)
                            {
                                GetSession().DoLootRelease(GetLootGUID());
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
            List<ItemPosCount> vDest = new List<ItemPosCount>();
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
                        ModifyCurrency((CurrencyTypes)iece.CurrencyID[i], -(int)(iece.CurrencyCount[i] * stacks), true, true);
                }
            }

            Item it = bStore ? StoreNewItem(vDest, item, true, ItemEnchantment.GenerateItemRandomPropertyId(item), null, 0, crItem.BonusListIDs, false) : EquipNewItem(uiDest, item, true);
            if (it != null)
            {
                uint new_count = pVendor.UpdateVendorItemCurrentCount(crItem, count);

                BuySucceeded packet = new BuySucceeded();
                packet.VendorGUID = pVendor.GetGUID();
                packet.Muid = vendorslot + 1;
                packet.NewQuantity = crItem.maxcount > 0 ? new_count : 0xFFFFFFFF;
                packet.QuantityBought = count;
                SendPacket(packet);

                SendNewItem(it, count, true, false, false);

                if (!bStore)
                    AutoUnequipOffhandIfNeed();

                if (pProto.GetFlags().HasAnyFlag(ItemFlags.ItemPurchaseRecord) && crItem.ExtendedCost != 0 && pProto.GetMaxStackSize() == 1)
                {
                    it.SetFlag(ItemFields.Flags, ItemFieldFlags.Refundable);
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
        public void SendNewItem(Item item, uint quantity, bool pushed, bool created, bool broadcast = false)
        {
            if (item == null) // prevent crash
                return;

            ItemPushResult packet = new ItemPushResult();

            packet.PlayerGUID = GetGUID();

            packet.Slot = item.GetBagSlot();
            packet.SlotInBag = item.GetCount() == quantity ? item.GetSlot() : -1;

            packet.Item = new ItemInstance(item);

            //packet.QuestLogItemID;
            packet.Quantity = quantity;
            packet.QuantityInInventory = GetItemCount(item.GetEntry());
            //packet.DungeonEncounterID; 
            packet.BattlePetSpeciesID = (int)item.GetModifier(ItemModifier.BattlePetSpeciesId);
            packet.BattlePetBreedID = (int)item.GetModifier(ItemModifier.BattlePetBreedData) & 0xFFFFFF;
            packet.BattlePetBreedQuality = (item.GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;
            packet.BattlePetLevel = (int)item.GetModifier(ItemModifier.BattlePetLevel);

            packet.ItemGUID = item.GetGUID();

            packet.Pushed = pushed;
            packet.DisplayText = ItemPushResult.DisplayType.Normal;
            packet.Created = created;
            //packet.IsBonusRoll;
            //packet.IsEncounterLoot;

            if (broadcast && GetGroup())
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
            if (item.GetUInt32Value(ItemFields.Duration) != 0)
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
                if (!realtimeonly || item.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.RealDuration))
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

                if (!pItem || pItem.GetSocketColor(0) == 0)   //if item has no sockets or no item is equipped go to next item
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
        public Item GetItemByGuid(ObjectGuid guid)
        {
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    if (pItem.GetGUID() == guid)
                        return pItem;
            }

            for (byte i = InventorySlots.BankItemStart; i < InventorySlots.BankBagEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    if (pItem.GetGUID() == guid)
                        return pItem;
            }

            for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                    if (pItem.GetGUID() == guid)
                        return pItem;
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                    if (pItem.GetGUID() == guid)
                        return pItem;
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                    for (byte j = 0; j < pBag.GetBagSize(); ++j)
                    {
                        Item pItem = pBag.GetItemByPos(j);
                        if (pItem != null)
                            if (pItem.GetGUID() == guid)
                                return pItem;
                    }
            }

            for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; ++i)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                    for (byte j = 0; j < pBag.GetBagSize(); ++j)
                    {
                        Item pItem = pBag.GetItemByPos(j);
                        if (pItem != null)
                            if (pItem.GetGUID() == guid)
                                return pItem;
                    }
            }
            return null;
        }
        public uint GetItemCount(uint item, bool inBankAlso = false, Item skipItem = null)
        {
            uint count = 0;
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    if (pItem != skipItem && pItem.GetEntry() == item)
                        count += pItem.GetCount();
            }

            for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                    count += pBag.GetItemCount(item, skipItem);
            }

            if (skipItem != null && skipItem.GetTemplate().GetGemProperties() != 0)
            {
                for (byte i = EquipmentSlot.Start; i < inventoryEnd; ++i)
                {
                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem != null)
                        if (pItem != skipItem && pItem.GetSocketColor(0) != 0)
                            count += pItem.GetGemCountWithID(item);
                }
            }

            if (inBankAlso)
            {
                // checking every item from 39 to 74 (including bank bags)
                for (var i = InventorySlots.BankItemStart; i < InventorySlots.BankBagEnd; ++i)
                {
                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem != null)
                        if (pItem != skipItem && pItem.GetEntry() == item)
                            count += pItem.GetCount();
                }

                for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; ++i)
                {
                    Bag pBag = GetBagByPos(i);
                    if (pBag != null)
                        count += pBag.GetItemCount(item, skipItem);
                }

                if (skipItem != null && skipItem.GetTemplate().GetGemProperties() != 0)
                {
                    for (var i = InventorySlots.BankItemStart; i < InventorySlots.BankItemEnd; ++i)
                    {
                        Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                        if (pItem != null)
                            if (pItem != skipItem && pItem.GetSocketColor(0) != 0)
                                count += pItem.GetGemCountWithID(item);
                    }
                }
            }

            for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                    if (pItem != skipItem && pItem.GetEntry() == item)
                        count += pItem.GetCount();
            }

            if (skipItem && skipItem.GetTemplate().GetGemProperties() != 0)
            {
                for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
                {
                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem)
                        if (pItem != skipItem && pItem.GetSocketColor(0) != 0)
                            count += pItem.GetGemCountWithID(item);
                }
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                {
                    if (pItem != skipItem && pItem.GetEntry() == item)
                        count += pItem.GetCount();
                }
            }

            if (skipItem && skipItem.GetTemplate().GetGemProperties() != 0)
            {
                for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
                {
                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem)
                        if (pItem != skipItem && pItem.GetSocketColor(0) != 0)
                            count += pItem.GetGemCountWithID(item);
                }
            }

            return count;
        }
        public Item GetUseableItemByPos(byte bag, byte slot)
        {
            Item item = GetItemByPos(bag, slot);
            if (!item)
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
        public Item GetItemByEntry(uint entry)
        {
            // in inventory
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    if (pItem.GetEntry() == entry)
                        return pItem;
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                    for (byte j = 0; j < pBag.GetBagSize(); ++j)
                    {
                        Item pItem = pBag.GetItemByPos(j);
                        if (pItem != null)
                        {
                            if (pItem.GetEntry() == entry)
                                return pItem;
                        }
                    }
            }

            for (byte i = EquipmentSlot.Start; i < InventorySlots.BagEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null)
                    if (pItem.GetEntry() == entry)
                        return pItem;
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                    if (pItem.GetEntry() == entry)
                        return pItem;
            }

            return null;
        }
        public List<Item> GetItemListByEntry(uint entry, bool inBankAlso = false)
        {
            List<Item> itemList = new List<Item>();

            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                    if (item.GetEntry() == entry)
                        itemList.Add(item);
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag bag = GetBagByPos(i);
                if (bag)
                {
                    for (byte j = 0; j < bag.GetBagSize(); ++j)
                    {
                        Item item = bag.GetItemByPos(j);
                        if (item != null)
                            if (item.GetEntry() == entry)
                                itemList.Add(item);
                    }
                }
            }

            for (byte i = EquipmentSlot.Start; i < InventorySlots.BagEnd; ++i)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item)
                    if (item.GetEntry() == entry)
                        itemList.Add(item);
            }

            if (inBankAlso)
            {
                for (byte i = InventorySlots.BankItemStart; i < InventorySlots.BankBagEnd; ++i)
                {
                    Item item = GetItemByPos(InventorySlots.Bag0, i);
                    if (item)
                        if (item.GetEntry() == entry)
                            itemList.Add(item);
                }
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item)
                    if (item.GetEntry() == entry)
                        itemList.Add(item);
            }

            return itemList;
        }
        public bool HasItemCount(uint item, uint count = 1, bool inBankAlso = false)
        {
            uint tempcount = 0;
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null && pItem.GetEntry() == item && !pItem.IsInTrade())
                {
                    tempcount += pItem.GetCount();
                    if (tempcount >= count)
                        return true;
                }
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag != null)
                {
                    for (byte j = 0; j < pBag.GetBagSize(); j++)
                    {
                        Item pItem = GetItemByPos(i, j);
                        if (pItem != null && pItem.GetEntry() == item && !pItem.IsInTrade())
                        {
                            tempcount += pItem.GetCount();
                            if (tempcount >= count)
                                return true;
                        }
                    }
                }
            }

            if (inBankAlso)
            {
                for (byte i = InventorySlots.BankItemStart; i < InventorySlots.BankBagEnd; i++)
                {
                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem != null && pItem.GetEntry() == item && !pItem.IsInTrade())
                    {
                        tempcount += pItem.GetCount();
                        if (tempcount >= count)
                            return true;
                    }
                }
                for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
                {
                    Bag pBag = GetBagByPos(i);
                    if (pBag != null)
                    {
                        for (byte j = 0; j < pBag.GetBagSize(); j++)
                        {
                            Item pItem = GetItemByPos(i, j);
                            if (pItem != null && pItem.GetEntry() == item && !pItem.IsInTrade())
                            {
                                tempcount += pItem.GetCount();
                                if (tempcount >= count)
                                    return true;
                            }
                        }
                    }
                }
            }

            for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem && pItem.GetEntry() == item && !pItem.IsInTrade())
                {
                    tempcount += pItem.GetCount();
                    if (tempcount >= count)
                        return true;
                }
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem && pItem.GetEntry() == item && !pItem.IsInTrade())
                {
                    tempcount += pItem.GetCount();
                    if (tempcount >= count)
                        return true;
                }
            }

            return false;
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

                // bag equip slots
                if (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd)
                    return true;

                // backpack slots
                if (slot >= InventorySlots.ItemStart && slot < InventorySlots.ItemStart + GetInventorySlotCount())
                    return true;

                // bank main slots
                if (slot >= InventorySlots.BankItemStart && slot < InventorySlots.BankItemEnd)
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
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                    if (pItem.GetGUID() == guid)
                        return pItem;
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                    if (pItem.GetGUID() == guid)
                        return pItem;
            }

            return null;
        }
        uint GetItemCountWithLimitCategory(uint limitCategory, Item skipItem)
        {
            uint count = 0;
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                {
                    if (pItem != skipItem)
                    {
                        ItemTemplate pProto = pItem.GetTemplate();
                        if (pProto != null)
                            if (pProto.GetItemLimitCategory() == limitCategory)
                                count += pItem.GetCount();
                    }
                }
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag)
                    count += pBag.GetItemCountWithLimitCategory(limitCategory, skipItem);
            }

            for (byte i = InventorySlots.BankItemStart; i < InventorySlots.BankItemEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                {
                    if (pItem != skipItem)
                    {
                        ItemTemplate pProto = pItem.GetTemplate();
                        if (pProto != null)
                            if (pProto.GetItemLimitCategory() == limitCategory)
                                count += pItem.GetCount();
                    }
                }
            }

            for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; ++i)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag)
                    count += pBag.GetItemCountWithLimitCategory(limitCategory, skipItem);
            }

            for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                {
                    if (pItem != skipItem)
                    {
                        ItemTemplate pProto = pItem.GetTemplate();
                        if (pProto != null)
                            if (pProto.GetItemLimitCategory() == limitCategory)
                                count += pItem.GetCount();
                    }
                }
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                {
                    if (pItem != skipItem)
                    {
                        ItemTemplate pProto = pItem.GetTemplate();
                        if (pProto != null)
                            if (pProto.GetItemLimitCategory() == limitCategory)
                                count += pItem.GetCount();
                    }
                }
            }

            return count;
        }
        public byte GetItemLimitCategoryQuantity(ItemLimitCategoryRecord limitEntry)
        {
            byte limit = limitEntry.Quantity;

            var limitConditions = Global.DB2Mgr.GetItemLimitCategoryConditions(limitEntry.Id);
            foreach (ItemLimitCategoryConditionRecord limitCondition in limitConditions)
            {
                PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(limitCondition.PlayerConditionID);
                if (playerCondition == null || ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
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
                if (pItem)
                {
                    if (pItem.IsConjuredConsumable())
                        DestroyItem(InventorySlots.Bag0, i, update);
                }
            }

            // in inventory bags
            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag)
                {
                    for (byte j = 0; j < pBag.GetBagSize(); j++)
                    {
                        Item pItem = pBag.GetItemByPos(j);
                        if (pItem)
                            if (pItem.IsConjuredConsumable())
                                DestroyItem(i, j, update);
                    }
                }
            }

            // in equipment and bag list
            for (byte i = EquipmentSlot.Start; i < InventorySlots.BagEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
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
                if (pItem)
                    if (pItem.IsLimitedToAnotherMapOrZone(GetMapId(), new_zone))
                        DestroyItem(InventorySlots.Bag0, i, update);
            }

            // in inventory bags
            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
            {
                Bag pBag = GetBagByPos(i);
                if (pBag)
                {
                    for (byte j = 0; j < pBag.GetBagSize(); j++)
                    {
                        Item pItem = pBag.GetItemByPos(j);
                        if (pItem)
                            if (pItem.IsLimitedToAnotherMapOrZone(GetMapId(), new_zone))
                                DestroyItem(i, j, update);
                    }
                }
            }

            // in equipment and bag list
            for (byte i = EquipmentSlot.Start; i < InventorySlots.BagEnd; i++)
            {
                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem)
                    if (pItem.IsLimitedToAnotherMapOrZone(GetMapId(), new_zone))
                        DestroyItem(InventorySlots.Bag0, i, update);
            }
        }

        public InventoryResult CanRollForItemInLFG(ItemTemplate proto, WorldObject lootedObject)
        {
            if (!GetGroup() || !GetGroup().isLFGGroup())
                return InventoryResult.Ok;    // not in LFG group

            // check if looted object is inside the lfg dungeon
            Map map = lootedObject.GetMap();
            if (!Global.LFGMgr.inLfgDungeonMap(GetGroup().GetGUID(), map.GetId(), map.GetDifficultyID()))
                return InventoryResult.Ok;

            if (proto == null)
                return InventoryResult.ItemNotFound;

            // Used by group, function GroupLoot, to know if a prototype can be used by a player
            if ((proto.GetAllowableClass() & getClassMask()) == 0 || (proto.GetAllowableRace() & (long)getRaceMask()) == 0)
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

            Class _class = GetClass();
            if (proto.GetClass() == ItemClass.Weapon && GetSkillValue(proto.GetSkill()) == 0)
                return InventoryResult.ProficiencyNeeded;

            if (proto.GetClass() == ItemClass.Armor && proto.GetSubClass() > (uint)ItemSubClassArmor.Miscellaneous
                && proto.GetSubClass() < (uint)ItemSubClassArmor.Cosmetic && proto.GetInventoryType() != InventoryType.Cloak)
            {
                if (_class == Class.Warrior || _class == Class.Paladin || _class == Class.Deathknight)
                {
                    if (getLevel() < 40)
                    {
                        if (proto.GetSubClass() != (uint)ItemSubClassArmor.Mail)
                            return InventoryResult.ClientLockedOut;
                    }
                    else if (proto.GetSubClass() != (uint)ItemSubClassArmor.Plate)
                        return InventoryResult.ClientLockedOut;
                }
                else if (_class == Class.Hunter || _class == Class.Shaman)
                {
                    if (getLevel() < 40)
                    {
                        if (proto.GetSubClass() != (uint)ItemSubClassArmor.Leather)
                            return InventoryResult.ClientLockedOut;
                    }
                    else if (proto.GetSubClass() != (uint)ItemSubClassArmor.Mail)
                        return InventoryResult.ClientLockedOut;
                }

                if (_class == Class.Rogue || _class == Class.Druid)
                    if (proto.GetSubClass() != (uint)ItemSubClassArmor.Leather)
                        return InventoryResult.ClientLockedOut;

                if (_class == Class.Mage || _class == Class.Priest || _class == Class.Warlock)
                    if (proto.GetSubClass() != (uint)ItemSubClassArmor.Cloth)
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
                    uint oldest_time = GetUInt32Value(ActivePlayerFields.BuyBackTimestamp);
                    uint oldest_slot = InventorySlots.BuyBackStart;

                    for (byte i = InventorySlots.BuyBackStart + 1; i < InventorySlots.BuyBackEnd; ++i)
                    {
                        // found empty
                        if (!m_items[i])
                        {
                            oldest_slot = i;
                            break;
                        }

                        uint i_time = GetUInt32Value(ActivePlayerFields.BuyBackTimestamp + i - InventorySlots.BuyBackStart);

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
                var time = Time.UnixTime;
                uint etime = (uint)(time - m_logintime + (30 * 3600));
                int eslot = (int)slot - InventorySlots.BuyBackStart;

                SetGuidValue(ActivePlayerFields.InvSlotHead + ((int)slot * 4), pItem.GetGUID());
                ItemTemplate proto = pItem.GetTemplate();
                if (proto != null)
                    SetUInt32Value(ActivePlayerFields.BuyBackPrice + eslot, proto.GetSellPrice() * pItem.GetCount());
                else
                    SetUInt32Value(ActivePlayerFields.BuyBackPrice + eslot, 0);
                SetUInt32Value(ActivePlayerFields.BuyBackTimestamp + eslot, etime);

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

            Creature creature = GetNPCIfCanInteractWith(vendorGuid, NPCFlags.Vendor);
            if (!creature)
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
            ItemExtendedCostRecord iece = null;
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

            ModifyCurrency((CurrencyTypes)currency, (int)count, true, true);
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

                    ModifyCurrency((CurrencyTypes)iece.CurrencyID[i], -(int)(iece.CurrencyCount[i] * stacks), false, true);
                }
            }

            return true;
        }

        public bool BuyItemFromVendorSlot(ObjectGuid vendorguid, uint vendorslot, uint item, byte count, byte bag, byte slot)
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

            if (!Convert.ToBoolean(pProto.GetAllowableClass() & getClassMask()) && pProto.GetBonding() == ItemBondingType.OnAcquire && !IsGameMaster())
            {
                SendBuyError(BuyResult.CantFindItem, null, item);
                return false;
            }

            if (!IsGameMaster() && ((pProto.GetFlags2().HasAnyFlag(ItemFlags2.FactionHorde) && GetTeam() == Team.Alliance) || (pProto.GetFlags2().HasAnyFlag(ItemFlags2.FactionAlliance) && GetTeam() == Team.Horde)))
                return false;

            Creature creature = GetNPCIfCanInteractWith(vendorguid, NPCFlags.Vendor);
            if (!creature)
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

            PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(crItem.PlayerConditionId);
            if (playerCondition != null)
            {
                if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
                {
                    SendEquipError(InventoryResult.ItemLocked);
                    return false;
                }
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
            if (crItem.IsGoldRequired(pProto) && pProto.GetBuyPrice() > 0) //Assume price cannot be negative (do not know why it is int32)
            {
                float buyPricePerItem = (float)pProto.GetBuyPrice() / pProto.GetBuyCount();
                ulong maxCount = (ulong)(PlayerConst.MaxMoneyAmount / buyPricePerItem);
                if (count > maxCount)
                {
                    Log.outError(LogFilter.Player, "Player {0} tried to buy {1} item id {2}, causing overflow", GetName(), count, pProto.GetId());
                    count = (byte)maxCount;
                }
                price = (ulong)(buyPricePerItem * count); //it should not exceed MAX_MONEY_AMOUNT

                // reputation discount
                price = (ulong)Math.Floor(price * GetReputationPriceDiscount(creature));

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
                if (!_StoreOrEquipNewItem(vendorslot, item, count, bag, slot, (int)price, pProto, creature, crItem, true))
                    return false;
            }
            else if (IsEquipmentPos(bag, slot))
            {
                if (count != 1)
                {
                    SendEquipError(InventoryResult.NotEquippable);
                    return false;
                }
                if (!_StoreOrEquipNewItem(vendorslot, item, count, bag, slot, (int)price, pProto, creature, crItem, false))
                    return false;
            }
            else
            {
                SendEquipError(InventoryResult.WrongSlot);
                return false;
            }

            if (crItem.maxcount != 0) // bought
            {
                if (pProto.GetQuality() > ItemQuality.Epic || (pProto.GetQuality() == ItemQuality.Epic && pProto.GetBaseItemLevel() >= GuildConst.MinNewsItemLevel))
                {
                    Guild guild = GetGuild();
                    if (guild != null)
                        guild.AddGuildNews(GuildNews.ItemPurchased, GetGUID(), 0, item);
                }
                return true;
            }

            return false;
        }

        uint GetMaxPersonalArenaRatingRequirement(uint minarenaslot)
        {
            // returns the maximal personal arena rating that can be used to purchase items requiring this condition
            // the personal rating of the arena team must match the required limit as well
            // so return max[in arenateams](min(personalrating[teamtype], teamrating[teamtype]))
            uint max_personal_rating = 0;
            for (byte i = (byte)minarenaslot; i < SharedConst.MaxArenaSlot; ++i)
            {
                ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById(GetArenaTeamId(i));
                if (at != null)
                {
                    uint p_rating = GetArenaPersonalRating(i);
                    uint t_rating = at.GetRating();
                    p_rating = p_rating < t_rating ? p_rating : t_rating;
                    if (max_personal_rating < p_rating)
                        max_personal_rating = p_rating;
                }
            }
            return max_personal_rating;
        }

        public void SendItemRetrievalMail(uint itemEntry, uint count)
        {
            MailSender sender = new MailSender(MailMessageType.Creature, 34337);
            MailDraft draft = new MailDraft("Recovered Item", "We recovered a lost item in the twisting nether and noted that it was yours.$B$BPlease find said object enclosed."); // This is the text used in Cataclysm, it probably wasn't changed.
            SQLTransaction trans = new SQLTransaction();

            Item item = Item.CreateItem(itemEntry, count, null);
            if (item)
            {
                item.SaveToDB(trans);
                draft.AddItem(item);
            }

            draft.SendMailTo(trans, new MailReceiver(this, GetGUID().GetCounter()), sender);
            DB.Characters.CommitTransaction(trans);
        }

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
                if (pItem)
                {
                    pItem.RemoveFromWorld();
                    if (del)
                        pItem.SetState(ItemUpdateState.Removed, this);
                }

                m_items[slot] = null;

                int eslot = (int)slot - InventorySlots.BuyBackStart;
                SetGuidValue(ActivePlayerFields.InvSlotHead + (int)(slot * 4), ObjectGuid.Empty);
                SetUInt32Value(ActivePlayerFields.BuyBackPrice + eslot, 0);
                SetUInt32Value(ActivePlayerFields.BuyBackTimestamp + eslot, 0);

                // if current backslot is filled set to now free slot
                if (m_items[m_currentBuybackSlot])
                    m_currentBuybackSlot = slot;
            }
        }

        public bool HasItemTotemCategory(uint TotemCategory)
        {
            int inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();
            for (byte i = EquipmentSlot.Start; i < inventoryEnd; ++i)
            {
                Item item = GetUseableItemByPos(InventorySlots.Bag0, i);
                if (item && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.GetTemplate().GetTotemCategory(), TotemCategory))
                    return true;
            }

            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
            {
                Bag bag = GetBagByPos(i);
                if (bag)
                {
                    for (byte j = 0; j < bag.GetBagSize(); ++j)
                    {
                        Item item = GetUseableItemByPos(i, j);
                        if (item && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.GetTemplate().GetTotemCategory(), TotemCategory))
                            return true;
                    }
                }
            }

            for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
            {
                Item item = GetUseableItemByPos(InventorySlots.Bag0, i);
                if (item && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.GetTemplate().GetTotemCategory(), TotemCategory))
                    return true;
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item item = GetUseableItemByPos(InventorySlots.Bag0, i);
                if (item && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.GetTemplate().GetTotemCategory(), TotemCategory))
                    return true;
            }

            return false;
        }

        public void _ApplyItemMods(Item item, byte slot, bool apply, bool updateItemAuras = true)
        {
            if (slot >= InventorySlots.BagEnd || item == null)
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
                ApplyItemDependentAuras(item, apply);
            ApplyArtifactPowers(item, apply);
            ApplyEnchantment(item, apply);

            Log.outDebug(LogFilter.Player, "_ApplyItemMods complete.");
        }
        public void _ApplyItemBonuses(Item item, byte slot, bool apply)
        {
            ItemTemplate proto = item.GetTemplate();
            if (slot >= InventorySlots.BagEnd || proto == null)
                return;

            uint itemLevel = item.GetItemLevel(this);
            float combatRatingMultiplier = 1.0f;
            GtCombatRatingsMultByILvlRecord ratingMult = CliDB.CombatRatingsMultByILvlGameTable.GetRow(itemLevel);
            if (ratingMult != null)
            {
                switch (proto.GetInventoryType())
                {
                    case InventoryType.Weapon:
                    case InventoryType.Shield:
                    case InventoryType.Ranged:
                    case InventoryType.Weapon2Hand:
                    case InventoryType.WeaponMainhand:
                    case InventoryType.WeaponOffhand:
                    case InventoryType.Holdable:
                    case InventoryType.RangedRight:
                        combatRatingMultiplier = ratingMult.WeaponMultiplier;
                        break;
                    case InventoryType.Trinket:
                        combatRatingMultiplier = ratingMult.TrinketMultiplier;
                        break;
                    case InventoryType.Neck:
                    case InventoryType.Finger:
                        combatRatingMultiplier = ratingMult.JewelryMultiplier;
                        break;
                    default:
                        combatRatingMultiplier = ratingMult.ArmorMultiplier;
                        break;
                }
            }

            // req. check at equip, but allow use for extended range if range limit max level, set proper level
            for (byte i = 0; i < ItemConst.MaxStats; ++i)
            {
                int statType = item.GetItemStatType(i);
                if (statType == -1)
                    continue;

                int val = item.GetItemStatValue(i, this);
                if (val == 0)
                    continue;

                switch ((ItemModType)statType)
                {
                    case ItemModType.Mana:
                        HandleStatModifier(UnitMods.Mana, UnitModifierType.BaseValue, (float)val, apply);
                        break;
                    case ItemModType.Health:                           // modify HP
                        HandleStatModifier(UnitMods.Health, UnitModifierType.BaseValue, (float)val, apply);
                        break;
                    case ItemModType.Agility:                          // modify agility
                        HandleStatModifier(UnitMods.StatAgility, UnitModifierType.BaseValue, (float)val, apply);
                        ApplyStatBuffMod(Stats.Agility, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatAgility, UnitModifierType.BasePCTExcludeCreate)), apply);
                        break;
                    case ItemModType.Strength:                         //modify strength
                        HandleStatModifier(UnitMods.StatStrength, UnitModifierType.BaseValue, (float)val, apply);
                        ApplyStatBuffMod(Stats.Strength, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatStrength, UnitModifierType.BasePCTExcludeCreate)), apply);
                        break;
                    case ItemModType.Intellect:                        //modify intellect
                        HandleStatModifier(UnitMods.StatIntellect, UnitModifierType.BaseValue, (float)val, apply);
                        ApplyStatBuffMod(Stats.Intellect, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatIntellect, UnitModifierType.BasePCTExcludeCreate)), apply);
                        break;
                    //case ItemModType.Spirit:                           //modify spirit
                        //HandleStatModifier(UnitMods.StatSpirit, UnitModifierType.BaseValue, (float)val, apply);
                        //ApplyStatBuffMod(Stats.Spirit, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatSpirit, UnitModifierType.BasePCTExcludeCreate)), apply);
                        //break;
                    case ItemModType.Stamina:                          //modify stamina
                        HandleStatModifier(UnitMods.StatStamina, UnitModifierType.BaseValue, (float)val, apply);
                        ApplyStatBuffMod(Stats.Stamina, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatStamina, UnitModifierType.BasePCTExcludeCreate)), apply);
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
                        ApplyRatingMod(CombatRating.CritRanged, val, apply);
                        break;
                    case ItemModType.HasteMeleeRating:
                        ApplyRatingMod(CombatRating.HasteMelee, val, apply);
                        break;
                    case ItemModType.HasteRangedRating:
                        ApplyRatingMod(CombatRating.HasteRanged, val, apply);
                        break;
                    case ItemModType.HasteSpellRating:
                        ApplyRatingMod(CombatRating.HasteSpell, val, apply);
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
                        HandleStatModifier(UnitMods.AttackPower, UnitModifierType.TotalValue, (float)val, apply);
                        HandleStatModifier(UnitMods.AttackPowerRanged, UnitModifierType.TotalValue, (float)val, apply);
                        break;
                    case ItemModType.RangedAttackPower:
                        HandleStatModifier(UnitMods.AttackPowerRanged, UnitModifierType.TotalValue, (float)val, apply);
                        break;
                    case ItemModType.Versatility:
                        ApplyRatingMod(CombatRating.VersatilityDamageDone, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.VersatilityDamageTaken, (int)(val * combatRatingMultiplier), apply);
                        ApplyRatingMod(CombatRating.VersatilityHealingDone, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.ManaRegeneration:
                        ApplyManaRegenBonus(val, apply);
                        break;
                    case ItemModType.ArmorPenetrationRating:
                        ApplyRatingMod(CombatRating.ArmorPenetration, val, apply);
                        break;
                    case ItemModType.SpellPower:
                        ApplySpellPowerBonus(val, apply);
                        break;
                    case ItemModType.HealthRegen:
                        ApplyHealthRegenBonus(val, apply);
                        break;
                    case ItemModType.SpellPenetration:
                        ApplySpellPenetrationBonus(val, apply);
                        break;
                    case ItemModType.MasteryRating:
                        ApplyRatingMod(CombatRating.Mastery, (int)(val * combatRatingMultiplier), apply);
                        break;
                    case ItemModType.ExtraArmor:
                        HandleStatModifier(UnitMods.Armor, UnitModifierType.TotalValue, (float)val, apply);
                        break;
                    case ItemModType.FireResistance:
                        HandleStatModifier(UnitMods.ResistanceFire, UnitModifierType.BaseValue, (float)val, apply);
                        break;
                    case ItemModType.FrostResistance:
                        HandleStatModifier(UnitMods.ResistanceFrost, UnitModifierType.BaseValue, (float)val, apply);
                        break;
                    case ItemModType.HolyResistance:
                        HandleStatModifier(UnitMods.ResistanceHoly, UnitModifierType.BaseValue, (float)val, apply);
                        break;
                    case ItemModType.ShadowResistance:
                        HandleStatModifier(UnitMods.ResistanceShadow, UnitModifierType.BaseValue, (float)val, apply);
                        break;
                    case ItemModType.NatureResistance:
                        HandleStatModifier(UnitMods.ResistanceNature, UnitModifierType.BaseValue, (float)val, apply);
                        break;
                    case ItemModType.ArcaneResistance:
                        HandleStatModifier(UnitMods.ResistanceArcane, UnitModifierType.BaseValue, (float)val, apply);
                        break;
                    case ItemModType.PvpPower:
                        ApplyRatingMod(CombatRating.PvpPower, val, apply);
                        break;
                    case ItemModType.CrAmplify:
                        ApplyRatingMod(CombatRating.Amplify, val, apply);
                        break;
                    case ItemModType.CrMultistrike:
                        ApplyRatingMod(CombatRating.Multistrike, val, apply);
                        break;
                    case ItemModType.CrReadiness:
                        ApplyRatingMod(CombatRating.Readiness, (int)(val * combatRatingMultiplier), apply);
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
                    case ItemModType.CrUnused7:
                        ApplyRatingMod(CombatRating.Unused7, val, apply);
                        break;
                    case ItemModType.CrCleave:
                        ApplyRatingMod(CombatRating.Cleave, val, apply);
                        break;
                    case ItemModType.CrUnused12:
                        ApplyRatingMod(CombatRating.Unused12, val, apply);
                        break;
                    case ItemModType.AgiStrInt:
                        HandleStatModifier(UnitMods.StatAgility, UnitModifierType.BaseValue, val, apply);
                        HandleStatModifier(UnitMods.StatStrength, UnitModifierType.BaseValue, val, apply);
                        HandleStatModifier(UnitMods.StatIntellect, UnitModifierType.BaseValue, val, apply);
                        ApplyStatBuffMod(Stats.Agility, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatAgility, UnitModifierType.BasePCTExcludeCreate)), apply);
                        ApplyStatBuffMod(Stats.Strength, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatStrength, UnitModifierType.BasePCTExcludeCreate)), apply);
                        ApplyStatBuffMod(Stats.Intellect, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatIntellect, UnitModifierType.BasePCTExcludeCreate)), apply);
                        break;
                    case ItemModType.AgiStr:
                        HandleStatModifier(UnitMods.StatAgility, UnitModifierType.BaseValue, val, apply);
                        HandleStatModifier(UnitMods.StatStrength, UnitModifierType.BaseValue, val, apply);
                        ApplyStatBuffMod(Stats.Agility, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatAgility, UnitModifierType.BasePCTExcludeCreate)), apply);
                        ApplyStatBuffMod(Stats.Strength, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatStrength, UnitModifierType.BasePCTExcludeCreate)), apply);
                        break;
                    case ItemModType.AgiInt:
                        HandleStatModifier(UnitMods.StatAgility, UnitModifierType.BaseValue, val, apply);
                        HandleStatModifier(UnitMods.StatIntellect, UnitModifierType.BaseValue, val, apply);
                        ApplyStatBuffMod(Stats.Agility, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatAgility, UnitModifierType.BasePCTExcludeCreate)), apply);
                        ApplyStatBuffMod(Stats.Intellect, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatIntellect, UnitModifierType.BasePCTExcludeCreate)), apply);
                        break;
                    case ItemModType.StrInt:
                        HandleStatModifier(UnitMods.StatStrength, UnitModifierType.BaseValue, val, apply);
                        HandleStatModifier(UnitMods.StatIntellect, UnitModifierType.BaseValue, val, apply);
                        ApplyStatBuffMod(Stats.Strength, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatStrength, UnitModifierType.BasePCTExcludeCreate)), apply);
                        ApplyStatBuffMod(Stats.Intellect, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatIntellect, UnitModifierType.BasePCTExcludeCreate)), apply);
                        break;
                }
            }

            uint armor = item.GetArmor(this);
            if (armor != 0)
                HandleStatModifier(UnitMods.Armor, UnitModifierType.BaseValue, (float)armor, apply);

            WeaponAttackType attType = WeaponAttackType.BaseAttack;
            if (slot == EquipmentSlot.MainHand && (proto.GetInventoryType() == InventoryType.Ranged || proto.GetInventoryType() == InventoryType.RangedRight))
            {
                attType = WeaponAttackType.RangedAttack;
            }
            else if (slot == EquipmentSlot.OffHand)
            {
                attType = WeaponAttackType.OffAttack;
            }

            if (CanUseAttackType(attType))
                _ApplyWeaponDamage(slot, item, apply);
        }

        void ApplyItemEquipSpell(Item item, bool apply, bool formChange = false)
        {
            if (item == null)
                return;

            ItemTemplate proto = item.GetTemplate();
            if (proto == null)
                return;

            for (byte i = 0; i < proto.Effects.Count; ++i)
            {
                var spellData = proto.Effects[i];

                // no spell
                if (spellData.SpellID == 0)
                    continue;

                // wrong triggering type
                if (apply && spellData.TriggerType != ItemSpelltriggerType.OnEquip)
                    continue;

                // check if it is valid spell
                SpellInfo spellproto = Global.SpellMgr.GetSpellInfo((uint)spellData.SpellID);
                if (spellproto == null)
                    continue;

                if (spellproto.HasAura(GetMap().GetDifficultyID(), AuraType.ModXpPct) && !GetSession().GetCollectionMgr().CanApplyHeirloomXpBonus(item.GetEntry(), getLevel())
                    && Global.DB2Mgr.GetHeirloomByItemId(item.GetEntry()) != null)
                    continue;

                if (spellData.ChrSpecializationID != 0 && spellData.ChrSpecializationID != GetUInt32Value(PlayerFields.CurrentSpecId))
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

                CastSpell(this, spellInfo, true, item);
            }
            else
            {
                if (formChange)                                     // check aura compatibility
                {
                    // Cannot be used in this stance/form
                    if (spellInfo.CheckShapeshift(GetShapeshiftForm()) == SpellCastResult.SpellCastOk)
                        return;                                     // and remove only not compatible at form change
                }

                if (item != null)
                    RemoveAurasDueToItemSpell(spellInfo.Id, item.GetGUID());  // un-apply all spells, not only at-equipped
                else
                    RemoveAurasDueToSpell(spellInfo.Id);           // un-apply spell (item set case)
            }
        }

        void ApplyEquipCooldown(Item pItem)
        {
            ItemTemplate proto = pItem.GetTemplate();
            if (proto.GetFlags().HasAnyFlag(ItemFlags.NoEquipCooldown))
                return;

            DateTime now = DateTime.Now;
            for (byte i = 0; i < proto.Effects.Count; ++i)
            {
                var effectData = proto.Effects[i];

                // apply proc cooldown to equip auras if we have any
                if (effectData.TriggerType == ItemSpelltriggerType.OnEquip)
                {
                    SpellProcEntry procEntry = Global.SpellMgr.GetSpellProcEntry((uint)effectData.SpellID);
                    if (procEntry == null)
                        continue;

                    Aura itemAura = GetAura((uint)effectData.SpellID, GetGUID(), pItem.GetGUID());
                    if (itemAura != null)
                        itemAura.AddProcCooldown(now + TimeSpan.FromMilliseconds(procEntry.Cooldown));
                    continue;
                }

                // no spell
                if (effectData.SpellID == 0)
                    continue;

                // wrong triggering type
                if (effectData.TriggerType != ItemSpelltriggerType.OnUse)
                    continue;

                // Don't replace longer cooldowns by equip cooldown if we have any.
                if (GetSpellHistory().GetRemainingCooldown(Global.SpellMgr.GetSpellInfo((uint)effectData.SpellID)) > 30 * Time.InMilliseconds)
                    continue;

                GetSpellHistory().AddCooldown((uint)effectData.SpellID, pItem.GetEntry(), TimeSpan.FromSeconds(30));

                ItemCooldown data = new ItemCooldown();
                data.ItemGuid = pItem.GetGUID();
                data.SpellID = (uint)effectData.SpellID;
                data.Cooldown = 30 * Time.InMilliseconds; //Always 30secs?
                SendPacket(data);
            }
        }

        void _RemoveAllItemMods()
        {
            Log.outDebug(LogFilter.Player, "_RemoveAllItemMods start.");

            for (byte i = 0; i < InventorySlots.BagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    ItemTemplate proto = m_items[i].GetTemplate();
                    if (proto == null)
                        continue;

                    // item set bonuses not dependent from item broken state
                    if (proto.GetItemSet() != 0)
                        Item.RemoveItemsSetItem(this, proto);

                    if (m_items[i].IsBroken() || !CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    ApplyItemEquipSpell(m_items[i], false);
                    ApplyEnchantment(m_items[i], false);
                    ApplyArtifactPowers(m_items[i], false);
                }
            }

            for (byte i = 0; i < InventorySlots.BagEnd; ++i)
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

            for (byte i = 0; i < InventorySlots.BagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    if (m_items[i].IsBroken() || !CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    ApplyItemDependentAuras(m_items[i], true);
                    _ApplyItemBonuses(m_items[i], i, true);
                }
            }

            for (byte i = 0; i < InventorySlots.BagEnd; ++i)
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
            for (byte i = 0; i < InventorySlots.BagEnd; ++i)
            {
                if (m_items[i] != null)
                {
                    if (!CanUseAttackType(GetAttackBySlot(i, m_items[i].GetTemplate().GetInventoryType())))
                        continue;

                    _ApplyItemMods(m_items[i], i, apply);
                }
            }
        }

        public ObjectGuid GetLootWorldObjectGUID(ObjectGuid lootObjectGuid)
        {
            var guid = m_AELootView.LookupByKey(lootObjectGuid);
            if (guid != null)
                return guid;

            return ObjectGuid.Empty;
        }

        public void RemoveAELootedObject(ObjectGuid lootObjectGuid)
        {
            m_AELootView.Remove(lootObjectGuid);
        }

        public bool HasLootWorldObjectGUID(ObjectGuid lootWorldObjectGuid)
        {
            return m_AELootView.Any(lootView => lootView.Value == lootWorldObjectGuid);
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
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentStart && slot < InventorySlots.ReagentEnd))
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

                ItemPosCount newPosition = new ItemPosCount((ushort)(InventorySlots.Bag0 << 8 | j), need_space);
                if (!newPosition.isContainedIn(dest))
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

            if (pSrcItem)
            {
                if (pSrcItem.IsNotEmptyBag() && !IsBagPos((ushort)((ushort)bag << 8 | slot)))
                    return InventoryResult.DestroyNonemptyBag;

                if (pSrcItem.HasFlag(ItemFields.Flags, ItemFieldFlags.Child) && !IsEquipmentPos(bag, slot) && !IsChildEquipmentPos(bag, slot))
                    return InventoryResult.WrongBagType3;

                if (!pSrcItem.HasFlag(ItemFields.Flags, ItemFieldFlags.Child) && IsChildEquipmentPos(bag, slot))
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

            ItemPosCount newPosition = new ItemPosCount((ushort)(bag << 8 | slot), need_space);
            if (!newPosition.isContainedIn(dest))
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
                ItemRemovedQuestCheck(it.GetEntry(), it.GetCount());
                RemoveItem(bag, slot, update);
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
            // update quest counters
            ItemAddedQuestCheck(pItem.GetEntry(), pItem.GetCount());
            UpdateCriteria(CriteriaTypes.ReceiveEpicItem, pItem.GetEntry(), pItem.GetCount());

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

                if (pLastItem.HasFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable))
                    AddTradeableItem(pLastItem);
            }
        }

        //Bank
        public static bool IsBankPos(ushort pos)
        {
            return IsBankPos((byte)(pos >> 8), (byte)(pos & 255));
        }
        public static bool IsBankPos(byte bag, byte slot)
        {
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankItemStart && slot < InventorySlots.BankItemEnd))
                return true;
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd))
                return true;
            if (bag >= InventorySlots.BankBagStart && bag < InventorySlots.BankBagEnd)
                return true;
            return false;
        }
        public InventoryResult CanBankItem(byte bag, byte slot, List<ItemPosCount> dest, Item pItem, bool swap, bool not_loading = true)
        {
            if (pItem == null)
                return swap ? InventoryResult.CantSwap : InventoryResult.ItemNotFound;

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

                    if (slot - InventorySlots.BagStart >= GetBankBagSlotCount())
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
                        res = CanStoreItem_InInventorySlots(InventorySlots.BankItemStart, InventorySlots.BankItemEnd, dest, pProto, ref count, true, pItem, bag, slot);
                        if (res != InventoryResult.Ok)
                            return res;

                        if (count == 0)
                            return InventoryResult.Ok;
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
                    res = CanStoreItem_InInventorySlots(InventorySlots.BankItemStart, InventorySlots.BankItemEnd, dest, pProto, ref count, false, pItem, bag, slot);
                    if (res != InventoryResult.Ok)
                        return res;

                    if (count == 0)
                        return InventoryResult.Ok;
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
                // in slots
                res = CanStoreItem_InInventorySlots(InventorySlots.BankItemStart, InventorySlots.BankItemEnd, dest, pProto, ref count, true, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                    return res;

                if (count == 0)
                    return InventoryResult.Ok;

                // in special bags
                if (pProto.GetBagFamily() != BagFamilyMask.None)
                {
                    for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
                    {
                        res = CanStoreItem_InBag(i, dest, pProto, ref count, true, false, pItem, bag, slot);
                        if (res != InventoryResult.Ok)
                            continue;

                        if (count == 0)
                            return InventoryResult.Ok;
                    }
                }

                for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
                {
                    res = CanStoreItem_InBag(i, dest, pProto, ref count, true, true, pItem, bag, slot);
                    if (res != InventoryResult.Ok)
                        continue;

                    if (count == 0)
                        return InventoryResult.Ok;
                }
            }

            // search free place in special bag
            if (pProto.GetBagFamily() != BagFamilyMask.None)
            {
                for (byte i = InventorySlots.BagStart; i < InventorySlots.BankBagEnd; i++)
                {
                    res = CanStoreItem_InBag(i, dest, pProto, ref count, false, false, pItem, bag, slot);
                    if (res != InventoryResult.Ok)
                        continue;

                    if (count == 0)
                        return InventoryResult.Ok;
                }
            }

            // search free space
            res = CanStoreItem_InInventorySlots(InventorySlots.BankItemStart, InventorySlots.BankItemEnd, dest, pProto, ref count, false, pItem, bag, slot);
            if (res != InventoryResult.Ok)
                return res;

            if (count == 0)
                return InventoryResult.Ok;

            for (byte i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
            {
                res = CanStoreItem_InBag(i, dest, pProto, ref count, false, true, pItem, bag, slot);
                if (res != InventoryResult.Ok)
                    continue;

                if (count == 0)
                    return InventoryResult.Ok;
            }
            return InventoryResult.BankFull;
        }
        public Item BankItem(List<ItemPosCount> dest, Item pItem, bool update)
        {
            return StoreItem(dest, pItem, update);
        }

        //Bags
        public Bag GetBagByPos(byte bag)
        {
            if ((bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd)
                || (bag >= InventorySlots.BankBagStart && bag < InventorySlots.BankBagEnd))
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

            if (pSrcItem)
            {
                if (pSrcItem.IsNotEmptyBag())
                    return InventoryResult.DestroyNonemptyBag;

                if (pSrcItem.HasFlag(ItemFields.Flags, ItemFieldFlags.Child))
                    return InventoryResult.WrongBagType3;
            }

            ItemTemplate pBagProto = pBag.GetTemplate();
            if (pBagProto == null)
                return InventoryResult.WrongBagType;

            // specialized bag mode or non-specilized
            if (non_specialized != (pBagProto.GetClass() == ItemClass.Container && pBagProto.GetSubClass() == (uint)ItemSubClassContainer.Container))
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

                ItemPosCount newPosition = new ItemPosCount((ushort)(bag << 8 | j), need_space);
                if (!newPosition.isContainedIn(dest))
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
            if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd))
                return true;
            return false;
        }
        byte FindEquipSlot(ItemTemplate proto, uint slot, bool swap)
        {
            byte[] slots = new byte[4];
            slots[0] = ItemConst.NullSlot;
            slots[1] = ItemConst.NullSlot;
            slots[2] = ItemConst.NullSlot;
            slots[3] = ItemConst.NullSlot;
            switch (proto.GetInventoryType())
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
                    slots[0] = InventorySlots.BagStart + 0;
                    slots[1] = InventorySlots.BagStart + 1;
                    slots[2] = InventorySlots.BagStart + 2;
                    slots[3] = InventorySlots.BagStart + 3;
                    break;
                default:
                    return ItemConst.NullSlot;
            }

            if (slot != ItemConst.NullSlot)
            {
                if (swap || GetItemByPos(InventorySlots.Bag0, (byte)slot) == null)
                    for (byte i = 0; i < 4; ++i)
                        if (slots[i] == slot)
                            return (byte)slot;
            }
            else
            {
                // search free slot at first
                for (byte i = 0; i < 4; ++i)
                    if (slots[i] != ItemConst.NullSlot && GetItemByPos(InventorySlots.Bag0, slots[i]) == null)
                        // in case 2hand equipped weapon (without titan grip) offhand slot empty but not free
                        if (slots[i] != EquipmentSlot.OffHand || !IsTwoHandUsed())
                            return slots[i];

                // if not found free and can swap return first appropriate from used
                for (byte i = 0; i < 4; ++i)
                    if (slots[i] != ItemConst.NullSlot && swap)
                        return slots[i];
            }

            // no free position
            return ItemConst.NullSlot;
        }
        InventoryResult CanEquipNewItem(byte slot, out ushort dest, uint item, bool swap)
        {
            dest = 0;
            Item pItem = Item.CreateItem(item, 1, this);
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

                        // do not allow equipping gear except weapons, offhands, projectiles, relics in
                        // - combat
                        // - in-progress arenas
                        if (!pProto.CanChangeEquipStateInCombat())
                        {
                            if (IsInCombat())
                                return InventoryResult.NotInCombat;
                            Battleground bg = GetBattleground();
                            if (bg)
                                if (bg.isArena() && bg.GetStatus() == BattlegroundStatus.InProgress)
                                    return InventoryResult.NotDuringArenaMatch;
                        }

                        if (IsInCombat() && (pProto.GetClass() == ItemClass.Weapon || pProto.GetInventoryType() == InventoryType.Relic) && m_weaponChangeTimer != 0)
                            return InventoryResult.ClientLockedOut;         // maybe exist better err

                        if (IsNonMeleeSpellCast(false))
                            return InventoryResult.ClientLockedOut;
                    }

                    ScalingStatDistributionRecord ssd = CliDB.ScalingStatDistributionStorage.LookupByKey(pItem.GetScalingStatDistribution());
                    // check allowed level (extend range to upper values if MaxLevel more or equal max player level, this let GM set high level with 1...max range items)
                    if (ssd != null && ssd.MaxLevel < WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel) && ssd.MaxLevel < getLevel() && Global.DB2Mgr.GetHeirloomByItemId(pProto.GetId()) == null)
                        return InventoryResult.NotEquippable;

                    byte eslot = FindEquipSlot(pProto, slot, swap);
                    if (eslot == ItemConst.NullSlot)
                        return InventoryResult.NotEquippable;

                    res = CanUseItem(pItem, not_loading);
                    if (res != InventoryResult.Ok)
                        return res;

                    if (!swap && GetItemByPos(InventorySlots.Bag0, eslot) != null)
                        return InventoryResult.NoSlotAvailable;

                    // if swap ignore item (equipped also)
                    InventoryResult res2 = CanEquipUniqueItem(pItem, swap ? eslot : ItemConst.NullSlot);
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
                            return InventoryResult.TwoHandSkillNotFound;
                        else if (type == InventoryType.Weapon)
                        {
                            if (!CanDualWield())
                                return InventoryResult.TwoHandSkillNotFound;
                        }
                        else if (type == InventoryType.WeaponOffhand)
                        {
                            if (!CanDualWield() && !pProto.GetFlags3().HasAnyFlag(ItemFlags3.AlwaysAllowDualWield))
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
                            List<ItemPosCount> off_dest = new List<ItemPosCount>();
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
            if (!childItem)
                return InventoryResult.Ok;

            ItemChildEquipmentRecord childEquipement = Global.DB2Mgr.GetItemChildEquipment(parentItem.GetEntry());
            if (childEquipement == null)
                return InventoryResult.Ok;

            Item dstItem = GetItemByPos(InventorySlots.Bag0, childEquipement.ChildItemEquipSlot);
            if (!dstItem)
                return InventoryResult.Ok;

            ushort childDest = (ushort)((InventorySlots.Bag0 << 8) | childEquipement.ChildItemEquipSlot);
            InventoryResult msg = CanUnequipItem(childDest, !childItem.IsBag());
            if (msg != InventoryResult.Ok)
                return msg;

            // check dest.src move possibility
            ushort src = parentItem.GetPos();
            List<ItemPosCount> dest = new List<ItemPosCount>();
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
        public InventoryResult CanEquipUniqueItem(Item pItem, byte eslot, uint limit_count = 1)
        {
            ItemTemplate pProto = pItem.GetTemplate();

            // proto based limitations
            InventoryResult res = CanEquipUniqueItem(pProto, eslot, limit_count);
            if (res != InventoryResult.Ok)
                return res;

            // check unique-equipped on gems
            foreach (ItemDynamicFieldGems gemData in pItem.GetGems())
            {
                ItemTemplate pGem = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);
                if (pGem == null)
                    continue;

                // include for check equip another gems with same limit category for not equipped item (and then not counted)
                uint gem_limit_count = (uint)(!pItem.IsEquipped() && pGem.GetItemLimitCategory() != 0 ? pItem.GetGemCountWithLimitCategory(pGem.GetItemLimitCategory()) : 1);

                InventoryResult ress = CanEquipUniqueItem(pGem, eslot, gem_limit_count);
                if (ress != InventoryResult.Ok)
                    return ress;
            }

            return InventoryResult.Ok;
        }
        public InventoryResult CanEquipUniqueItem(ItemTemplate itemProto, byte except_slot, uint limit_count = 1)
        {
            // check unique-equipped on item
            if (Convert.ToBoolean(itemProto.GetFlags() & ItemFlags.UniqueEquippable))
            {
                // there is an equip limit on this item
                if (HasItemOrGemWithIdEquipped(itemProto.GetId(), 1, except_slot))
                    return InventoryResult.ItemUniqueEquippable;
            }

            // check unique-equipped limit
            if (itemProto.GetItemLimitCategory() != 0)
            {
                ItemLimitCategoryRecord limitEntry = CliDB.ItemLimitCategoryStorage.LookupByKey(itemProto.GetItemLimitCategory());
                if (limitEntry == null)
                    return InventoryResult.NotEquippable;

                // NOTE: limitEntry.mode not checked because if item have have-limit then it applied and to equip case
                byte limitQuantity = GetItemLimitCategoryQuantity(limitEntry);

                if (limit_count > limitQuantity)
                    return InventoryResult.ItemMaxLimitCategoryEquippedExceededIs;

                // there is an equip limit on this item
                if (HasItemWithLimitCategoryEquipped(itemProto.GetItemLimitCategory(), limitQuantity - limit_count + 1, except_slot))
                    return InventoryResult.ItemMaxLimitCategoryEquippedExceededIs;
                else if (HasGemWithLimitCategoryEquipped(itemProto.GetItemLimitCategory(), limitQuantity - limit_count + 1, except_slot))
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

            // do not allow unequipping gear except weapons, offhands, projectiles, relics in
            // - combat
            // - in-progress arenas
            if (!pProto.CanChangeEquipStateInCombat())
            {
                if (IsInCombat())
                    return InventoryResult.NotInCombat;
                Battleground bg = GetBattleground();
                if (bg)
                    if (bg.isArena() && bg.GetStatus() == BattlegroundStatus.InProgress)
                        return InventoryResult.NotDuringArenaMatch;
            }

            if (!swap && pItem.IsNotEmptyBag())
                return InventoryResult.DestroyNonemptyBag;

            return InventoryResult.Ok;
        }

        //Child
        public static bool IsChildEquipmentPos(ushort pos) { return IsChildEquipmentPos((byte)(pos >> 8), (byte)(pos & 255)); }

        //Artifact
        void ApplyArtifactPowers(Item item, bool apply)
        {
            foreach (ItemDynamicFieldArtifactPowers artifactPower in item.GetArtifactPowers())
            {
                byte rank = artifactPower.CurrentRankWithBonus;
                if (rank == 0)
                    continue;

                if (CliDB.ArtifactPowerStorage[artifactPower.ArtifactPowerId].Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
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
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(artifactPowerRank.SpellID);
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
                    Dictionary<SpellValueMod, int> csv = new Dictionary<SpellValueMod, int>();
                    if (artifactPowerRank.AuraPointsOverride != 0)
                        for (int i = 0; i < SpellConst.MaxEffects; ++i)
                            if (spellInfo.GetEffect((uint)i) != null)
                                csv.Add(SpellValueMod.BasePoint0 + i, (int)artifactPowerRank.AuraPointsOverride);

                    CastCustomSpell(artifactPowerRank.SpellID, csv, this, TriggerCastFlags.FullMask, artifact);
                }
            }
            else
            {
                if (apply && !HasSpell(artifactPowerRank.SpellID))
                {
                    AddTemporarySpell(artifactPowerRank.SpellID);
                    LearnedSpells learnedSpells = new LearnedSpells();
                    learnedSpells.SuppressMessaging = true;
                    learnedSpells.SpellID.Add(artifactPowerRank.SpellID);
                    SendPacket(learnedSpells);
                }
                else if (!apply)
                {
                    RemoveTemporarySpell(artifactPowerRank.SpellID);
                    UnlearnedSpells unlearnedSpells = new UnlearnedSpells();
                    unlearnedSpells.SuppressMessaging = true;
                    unlearnedSpells.SpellID.Add(artifactPowerRank.SpellID);
                    SendPacket(unlearnedSpells);
                }
            }
        }

        public bool HasItemOrGemWithIdEquipped(uint item, uint count, byte except_slot = ItemConst.NullSlot)
        {
            uint tempcount = 0;
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                if (i == except_slot)
                    continue;

                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (pItem != null && pItem.GetEntry() == item)
                {
                    tempcount += pItem.GetCount();
                    if (tempcount >= count)
                        return true;
                }
            }

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(item);
            if (pProto != null && pProto.GetGemProperties() != 0)
            {
                for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
                {
                    if (i == except_slot)
                        continue;

                    Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                    if (pItem != null && pItem.GetSocketColor(0) != 0)
                    {
                        tempcount += pItem.GetGemCountWithID(item);
                        if (tempcount >= count)
                            return true;
                    }
                }
            }

            return false;
        }
        bool HasItemWithLimitCategoryEquipped(uint limitCategory, uint count, byte except_slot)
        {
            uint tempcount = 0;
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                if (i == except_slot)
                    continue;

                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (!pItem)
                    continue;

                ItemTemplate pProto = pItem.GetTemplate();
                if (pProto == null)
                    continue;

                if (pProto.GetItemLimitCategory() == limitCategory)
                {
                    tempcount += pItem.GetCount();
                    if (tempcount >= count)
                        return true;
                }
            }

            return false;
        }

        bool HasGemWithLimitCategoryEquipped(uint limitCategory, uint count, byte except_slot)
        {
            uint tempcount = 0;
            for (byte i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
            {
                if (i == except_slot)
                    continue;

                Item pItem = GetItemByPos(InventorySlots.Bag0, i);
                if (!pItem)
                    continue;

                ItemTemplate pProto = pItem.GetTemplate();
                if (pProto == null)
                    continue;

                if (pItem.GetSocketColor(0) != 0 || pItem.GetEnchantmentId(EnchantmentSlot.Prismatic) != 0)
                {
                    tempcount += pItem.GetGemCountWithLimitCategory(limitCategory);
                    if (tempcount >= count)
                        return true;
                }
            }

            return false;
        }

        //Visual
        public void SetVisibleItemSlot(uint slot, Item pItem)
        {
            if (pItem != null)
            {
                SetUInt32Value(PlayerFields.VisibleItem + (int)(slot * 2), pItem.GetVisibleEntry(this));
                SetUInt16Value(PlayerFields.VisibleItem + 1 + (int)(slot * 2), 0, pItem.GetVisibleAppearanceModId(this));
                SetUInt16Value(PlayerFields.VisibleItem + 1 + (int)(slot * 2), 1, pItem.GetVisibleItemVisual(this));
            }
            else
            {
                SetUInt32Value(PlayerFields.VisibleItem + (int)(slot * 2), 0);
                SetUInt32Value(PlayerFields.VisibleItem + 1 + (int)(slot * 2), 0);
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
            SetGuidValue(ActivePlayerFields.InvSlotHead + (int)(slot * 4), pItem.GetGUID());
            pItem.SetGuidValue(ItemFields.Contained, GetGUID());
            pItem.SetGuidValue(ItemFields.Owner, GetGUID());
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

                if (pItem.HasFlag(ItemFields.Flags, ItemFieldFlags.Wrapped))
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GIFT);
                    stmt.AddValue(0, pItem.GetGUID().GetCounter());
                    DB.Characters.Execute(stmt);
                }

                RemoveEnchantmentDurations(pItem);
                RemoveItemDurations(pItem);

                pItem.SetNotRefundable(this);
                pItem.ClearSoulboundTradeable(this);
                RemoveTradeableItem(pItem);

                ApplyItemObtainSpells(pItem, false);

                ItemRemovedQuestCheck(pItem.GetEntry(), pItem.GetCount());
                Bag pBag;
                if (bag == InventorySlots.Bag0)
                {
                    SetGuidValue(ActivePlayerFields.InvSlotHead + (slot * 4), ObjectGuid.Empty);

                    // equipment and equipped bags can have applied bonuses
                    if (slot < InventorySlots.BagEnd)
                    {
                        ItemTemplate pProto = pItem.GetTemplate();

                        // item set bonuses applied only at equip and removed at unequip, and still active for broken items
                        if (pProto != null && pProto.GetItemSet() != 0)
                            Item.RemoveItemsSetItem(this, pProto);

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
                ItemTemplate pTmp = pItem.GetTemplate();
                if (pTmp != null)
                    if (Convert.ToBoolean(pTmp.GetFlags() & ItemFlags.HasLoot))
                        pItem.ItemContainerDeleteLootMoneyAndLootItemsFromDB();

                if (IsInWorld && update)
                {
                    pItem.RemoveFromWorld();
                    pItem.DestroyForPlayer(this);
                }

                //pItem.SetOwnerGUID(ObjectGuid.Empty);
                pItem.SetGuidValue(ItemFields.Contained, ObjectGuid.Empty);
                pItem.SetSlot(ItemConst.NullSlot);
                pItem.SetState(ItemUpdateState.Removed, this);
            }
        }

        public void DestroyItemCount(uint itemEntry, uint count, bool update, bool unequip_check = true)
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
                                return;
                        }
                        else
                        {
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            item.SetCount(item.GetCount() - count + remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return;
                        }
                    }
                }
            }

            // in inventory bags
            for (byte i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
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
                                        return;
                                }
                                else
                                {
                                    ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                                    item.SetCount(item.GetCount() - count + remcount);
                                    if (IsInWorld && update)
                                        item.SendUpdateToPlayer(this);
                                    item.SetState(ItemUpdateState.Changed, this);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // in equipment and bag list
            for (byte i = EquipmentSlot.Start; i < InventorySlots.BagEnd; i++)
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
                                    return;
                            }
                        }
                        else
                        {
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            item.SetCount(item.GetCount() - count + remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return;
                        }
                    }
                }
            }

            // in bank
            for (byte i = InventorySlots.BankItemStart; i < InventorySlots.BankItemEnd; i++)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item != null)
                {
                    if (item.GetEntry() == itemEntry && !item.IsInTrade())
                    {
                        if (item.GetCount() + remcount <= count)
                        {
                            remcount += item.GetCount();
                            DestroyItem(InventorySlots.Bag0, i, update);
                            if (remcount >= count)
                                return;
                        }
                        else
                        {
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            item.SetCount(item.GetCount() - count + remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return;
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
                                        return;
                                }
                                else
                                {
                                    ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                                    item.SetCount(item.GetCount() - count + remcount);
                                    if (IsInWorld && update)
                                        item.SendUpdateToPlayer(this);
                                    item.SetState(ItemUpdateState.Changed, this);
                                    return;
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
                if (item)
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
                                    return;
                            }
                        }
                        else
                        {
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            item.SetCount(item.GetCount() - count + remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return;
                        }
                    }
                }
            }

            for (byte i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item)
                {
                    if (item.GetEntry() == itemEntry && !item.IsInTrade())
                    {
                        if (item.GetCount() + remcount <= count)
                        {
                            // all keys can be unequipped
                            remcount += item.GetCount();
                            DestroyItem(InventorySlots.Bag0, i, update);

                            if (remcount >= count)
                                return;
                        }
                        else
                        {
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            item.SetCount(item.GetCount() - count + remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return;
                        }
                    }
                }
            }

            for (byte i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
            {
                Item item = GetItemByPos(InventorySlots.Bag0, i);
                if (item)
                {
                    if (item.GetEntry() == itemEntry && !item.IsInTrade())
                    {
                        if (item.GetCount() + remcount <= count)
                        {
                            // all keys can be unequipped
                            remcount += item.GetCount();
                            DestroyItem(InventorySlots.Bag0, i, update);

                            if (remcount >= count)
                                return;
                        }
                        else
                        {
                            ItemRemovedQuestCheck(item.GetEntry(), count - remcount);
                            item.SetCount(item.GetCount() - count + remcount);
                            if (IsInWorld && update)
                                item.SendUpdateToPlayer(this);
                            item.SetState(ItemUpdateState.Changed, this);
                            return;
                        }
                    }
                }
            }
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
        public void AutoStoreLoot(uint loot_id, LootStore store, bool broadcast = false) { AutoStoreLoot(ItemConst.NullBag, ItemConst.NullSlot, loot_id, store, broadcast); }
        void AutoStoreLoot(byte bag, byte slot, uint loot_id, LootStore store, bool broadcast = false)
        {
            Loot loot = new Loot();
            loot.FillLoot(loot_id, store, this, true);

            uint max_slot = loot.GetMaxSlotInLootFor(this);
            for (uint i = 0; i < max_slot; ++i)
            {
                LootItem lootItem = loot.LootItemInSlot(i, this);

                List<ItemPosCount> dest = new List<ItemPosCount>();
                InventoryResult msg = CanStoreNewItem(bag, slot, dest, lootItem.itemid, lootItem.count);
                if (msg != InventoryResult.Ok && slot != ItemConst.NullSlot)
                    msg = CanStoreNewItem(bag, ItemConst.NullSlot, dest, lootItem.itemid, lootItem.count);
                if (msg != InventoryResult.Ok && bag != ItemConst.NullBag)
                    msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, lootItem.itemid, lootItem.count);
                if (msg != InventoryResult.Ok)
                {
                    SendEquipError(msg, null, null, lootItem.itemid);
                    continue;
                }

                Item pItem = StoreNewItem(dest, lootItem.itemid, true, lootItem.randomPropertyId, null, lootItem.context, lootItem.BonusListIDs);
                SendNewItem(pItem, lootItem.count, false, false, broadcast);
            }
        }

        public byte GetInventorySlotCount() { return GetByteValue(ActivePlayerFields.Bytes2, PlayerFieldOffsets.FieldBytes2OffsetNumBackpackSlots);    }
        public void SetInventorySlotCount(byte slots)
        {
            //ASSERT(slots <= (INVENTORY_SLOT_ITEM_END - INVENTORY_SLOT_ITEM_START));

            if (slots < GetInventorySlotCount())
            {
                List<Item> unstorableItems = new List<Item>();

                for (byte slot = (byte)(InventorySlots.ItemStart + slots); slot < InventorySlots.ItemEnd; ++slot)
                {
                    Item unstorableItem = GetItemByPos(InventorySlots.Bag0, slot);
                    if (unstorableItem)
                        unstorableItems.Add(unstorableItem);
                }

                if (!unstorableItems.Empty())
                {
                    int fullBatches = unstorableItems.Count / SharedConst.MaxMailItems;
                    int remainder = unstorableItems.Count % SharedConst.MaxMailItems;
                    SQLTransaction trans = new SQLTransaction();

                    var sendItemsBatch = new Action<int, int>((batchNumber, batchSize) =>
                    {
                        MailDraft draft = new MailDraft(Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem), "There were problems with equipping item(s).");
                        for (int j = 0; j < batchSize; ++j)
                            draft.AddItem(unstorableItems[batchNumber * SharedConst.MaxMailItems + j]);

                        draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);
                    });

                    for (int batch = 0; batch < fullBatches; ++batch)
                        sendItemsBatch(batch, SharedConst.MaxMailItems);

                    if (remainder != 0)
                        sendItemsBatch(fullBatches, remainder);

                    DB.Characters.CommitTransaction(trans);

                    SendPacket(new CharacterInventoryOverflowWarning());
                }
            }

            SetByteValue(ActivePlayerFields.Bytes2, PlayerFieldOffsets.FieldBytes2OffsetNumBackpackSlots, slots);
        }

        public byte GetBankBagSlotCount() { return GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetBankBagSlots); }
        public void SetBankBagSlotCount(byte count) { SetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetBankBagSlots, count); }

        //Loot
        public ObjectGuid GetLootGUID() { return GetGuidValue(PlayerFields.LootTargetGuid); }
        public void SetLootGUID(ObjectGuid guid) { SetGuidValue(PlayerFields.LootTargetGuid, guid); }
        public void StoreLootItem(byte lootSlot, Loot loot, AELootResult aeResult = null)
        {
            NotNormalLootItem qitem = null;
            NotNormalLootItem ffaitem = null;
            NotNormalLootItem conditem = null;

            LootItem item = loot.LootItemInSlot(lootSlot, this, out qitem, out ffaitem, out conditem);
            if (item == null)
            {
                SendEquipError(InventoryResult.LootGone);
                return;
            }

            if (!item.AllowedForPlayer(this))
            {
                SendLootReleaseAll();
                return;
            }

            // questitems use the blocked field for other purposes
            if (qitem == null && item.is_blocked)
            {
                SendLootReleaseAll();
                return;
            }

            // dont allow protected item to be looted by someone else
            if (!item.rollWinnerGUID.IsEmpty() && item.rollWinnerGUID != GetGUID())
            {
                SendLootRelease(GetLootGUID());
                return;
            }

            List<ItemPosCount> dest = new List<ItemPosCount>();
            InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.itemid, item.count);
            if (msg == InventoryResult.Ok)
            {
                Item newitem = StoreNewItem(dest, item.itemid, true, item.randomPropertyId, item.GetAllowedLooters(), item.context, item.BonusListIDs);
                if (qitem != null)
                {
                    qitem.is_looted = true;
                    //freeforall is 1 if everyone's supposed to get the quest item.
                    if (item.freeforall || loot.GetPlayerQuestItems().Count == 1)
                        SendNotifyLootItemRemoved(loot.GetGUID(), lootSlot);
                    else
                        loot.NotifyQuestItemRemoved(qitem.index);
                }
                else
                {
                    if (ffaitem != null)
                    {
                        //freeforall case, notify only one player of the removal
                        ffaitem.is_looted = true;
                        SendNotifyLootItemRemoved(loot.GetGUID(), lootSlot);
                    }
                    else
                    {
                        //not freeforall, notify everyone
                        if (conditem != null)
                            conditem.is_looted = true;
                        loot.NotifyItemRemoved(lootSlot);
                    }
                }

                //if only one person is supposed to loot the item, then set it to looted
                if (!item.freeforall)
                    item.is_looted = true;

                --loot.unlootedCount;

                if (Global.ObjectMgr.GetItemTemplate(item.itemid) != null)
                {
                    if (newitem.GetQuality() > ItemQuality.Epic || (newitem.GetQuality() == ItemQuality.Epic && newitem.GetItemLevel(this) >= GuildConst.MinNewsItemLevel))
                    {
                        Guild guild = GetGuild();
                        if (guild)
                            guild.AddGuildNews(GuildNews.ItemLooted, GetGUID(), 0, item.itemid);
                    }
                }

                // if aeLooting then we must delay sending out item so that it appears properly stacked in chat
                if (aeResult == null)
                {
                    SendNewItem(newitem, item.count, false, false, true);
                    UpdateCriteria(CriteriaTypes.LootItem, item.itemid, item.count);
                    UpdateCriteria(CriteriaTypes.LootType, item.itemid, item.count, (ulong)loot.loot_type);
                    UpdateCriteria(CriteriaTypes.LootEpicItem, item.itemid, item.count);
                }
                else
                    aeResult.Add(newitem, item.count, loot.loot_type);

                // LootItem is being removed (looted) from the container, delete it from the DB.
                if (!loot.containerID.IsEmpty())
                    loot.DeleteLootItemFromContainerItemDB(item.itemid);

            }
            else
                SendEquipError(msg, null, null, item.itemid);
        }

        public Dictionary<ObjectGuid, ObjectGuid> GetAELootView() { return m_AELootView; }
        
        /// <summary>
        /// if in a Battleground a player dies, and an enemy removes the insignia, the player's bones is lootable
        /// Called by remove insignia spell effect
        /// </summary>
        /// <param name="looterPlr"></param>
        public void RemovedInsignia(Player looterPlr)
        {
            if (GetBattlegroundId() == 0)
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
            if (!bones)
                return;

            // Now we must make bones lootable, and send player loot
            bones.SetFlag(CorpseFields.DynamicFlags, 0x01);

            // We store the level of our player in the gold field
            // We retrieve this information at Player.SendLoot()
            bones.loot.gold = getLevel();
            bones.lootRecipient = looterPlr;
            looterPlr.SendLoot(bones.GetGUID(), LootType.Insignia);
        }

        public void SendLootRelease(ObjectGuid guid)
        {
            LootReleaseResponse packet = new LootReleaseResponse();
            packet.LootObj = guid;
            packet.Owner = GetGUID();
            SendPacket(packet);
        }

        public void SendLootReleaseAll()
        {
            SendPacket(new LootReleaseAll());
        }

        public void SendLoot(ObjectGuid guid, LootType loot_type, bool aeLooting = false)
        {
            ObjectGuid currentLootGuid = GetLootGUID();
            if (!currentLootGuid.IsEmpty() && !aeLooting)
                Session.DoLootRelease(currentLootGuid);

            Loot loot = null;
            PermissionTypes permission = PermissionTypes.All;

            Log.outDebug(LogFilter.Loot, "Player.SendLoot");
            if (guid.IsGameObject())
            {
                GameObject go = GetMap().GetGameObject(guid);

                // not check distance for GO in case owned GO (fishing bobber case, for example)
                // And permit out of range GO with no owner in case fishing hole
                if (!go || (loot_type != LootType.Fishinghole && ((loot_type != LootType.Fishing && loot_type != LootType.FishingJunk) || go.GetOwnerGUID() != GetGUID())
                    && !go.IsWithinDistInMap(this, SharedConst.InteractionDistance)) || (loot_type == LootType.Corpse && go.GetRespawnTime() != 0 && go.isSpawnedByDefault()))
                {
                    SendLootRelease(guid);
                    return;
                }

                loot = go.loot;

                if (go.getLootState() == LootState.Ready)
                {
                    uint lootid = go.GetGoInfo().GetLootId();
                    Battleground bg = GetBattleground();
                    if (bg)
                    {
                        if (!bg.CanActivateGO((int)go.GetEntry(), (uint)GetTeam()))
                        {
                            SendLootRelease(guid);
                            return;
                        }
                    }

                    if (lootid != 0)
                    {
                        loot.clear();

                        Group group = GetGroup();
                        bool groupRules = (group && go.GetGoInfo().type == GameObjectTypes.Chest && go.GetGoInfo().Chest.usegrouplootrules != 0);

                        // check current RR player and get next if necessary
                        if (groupRules)
                            group.UpdateLooterGuid(go, true);

                        loot.FillLoot(lootid, LootStorage.Gameobject, this, !groupRules, false, go.GetLootMode());

                        // get next RR player (for next loot)
                        if (groupRules)
                            group.UpdateLooterGuid(go);
                    }

                    GameObjectTemplateAddon addon = go.GetTemplateAddon();
                    if (addon != null)
                        loot.generateMoneyLoot(addon.mingold, addon.maxgold);

                    if (loot_type == LootType.Fishing)
                        go.getFishLoot(loot, this);
                    else if (loot_type == LootType.FishingJunk)
                        go.getFishLootJunk(loot, this);

                    if (go.GetGoInfo().type == GameObjectTypes.Chest && go.GetGoInfo().Chest.usegrouplootrules != 0)
                    {
                        var group = GetGroup();
                        if (group)
                        {
                            switch (group.GetLootMethod())
                            {
                                case LootMethod.GroupLoot:
                                    // GroupLoot: rolls items over threshold. Items with quality < threshold, round robin
                                    group.GroupLoot(loot, go);
                                    break;
                                case LootMethod.MasterLoot:
                                    group.MasterLoot(loot, go);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    go.SetLootState(LootState.Activated, this);
                }

                if (go.getLootState() == LootState.Activated)
                {
                    Group group = GetGroup();
                    if (group)
                    {
                        switch (group.GetLootMethod())
                        {
                            case LootMethod.MasterLoot:
                                permission = PermissionTypes.Master;
                                break;
                            case LootMethod.FreeForAll:
                                permission = PermissionTypes.All;
                                break;
                            default:
                                permission = PermissionTypes.Group;
                                break;
                        }
                    }
                    else
                        permission = PermissionTypes.All;
                }
            }
            else if (guid.IsItem())
            {
                Item item = GetItemByGuid(guid);

                if (item == null)
                {
                    SendLootRelease(guid);
                    return;
                }

                permission = PermissionTypes.Owner;

                loot = item.loot;

                // If item doesn't already have loot, attempt to load it. If that
                //  fails then this is first time opening, generate loot
                if (!item.m_lootGenerated && !item.ItemContainerLoadLootFromDB())
                {
                    item.m_lootGenerated = true;
                    loot.clear();

                    switch (loot_type)
                    {
                        case LootType.Disenchanting:
                            loot.FillLoot(item.GetDisenchantLoot(this).Id, LootStorage.Disenchant, this, true);
                            break;
                        case LootType.Prospecting:
                            loot.FillLoot(item.GetEntry(), LootStorage.Prospecting, this, true);
                            break;
                        case LootType.Milling:
                            loot.FillLoot(item.GetEntry(), LootStorage.Milling, this, true);
                            break;
                        default:
                            loot.generateMoneyLoot(item.GetTemplate().MinMoneyLoot, item.GetTemplate().MaxMoneyLoot);
                            loot.FillLoot(item.GetEntry(), LootStorage.Items, this, true, loot.gold != 0);

                            // Force save the loot and money items that were just rolled
                            //  Also saves the container item ID in Loot struct (not to DB)
                            if (loot.gold > 0 || loot.unlootedCount > 0)
                                item.ItemContainerSaveLootToDB();

                            break;
                    }
                }
            }
            else if (guid.IsCorpse())                          // remove insignia
            {
                Corpse bones = ObjectAccessor.GetCorpse(this, guid);

                if (bones == null || !(loot_type == LootType.Corpse || loot_type == LootType.Insignia) || bones.GetCorpseType() != CorpseType.Bones)
                {
                    SendLootRelease(guid);
                    return;
                }

                loot = bones.loot;

                if (!bones.lootForBody)
                {
                    bones.lootForBody = true;
                    uint pLevel = bones.loot.gold;
                    bones.loot.clear();
                    Battleground bg = GetBattleground();
                    if (bg)
                        if (bg.GetTypeID(true) == BattlegroundTypeId.AV)
                            loot.FillLoot(1, LootStorage.Creature, this, true);
                    // It may need a better formula
                    // Now it works like this: lvl10: ~6copper, lvl70: ~9silver
                    bones.loot.gold = (uint)(RandomHelper.URand(50, 150) * 0.016f * Math.Pow(pLevel / 5.76f, 2.5f) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
                }

                if (bones.lootRecipient != this)
                    permission = PermissionTypes.None;
                else
                    permission = PermissionTypes.Owner;
            }
            else
            {
                Creature creature = GetMap().GetCreature(guid);

                // must be in range and creature must be alive for pickpocket and must be dead for another loot
                if (creature == null || creature.IsAlive() != (loot_type == LootType.Pickpocketing) || (!aeLooting && !creature.IsWithinDistInMap(this, SharedConst.InteractionDistance)))
                {
                    SendLootRelease(guid);
                    return;
                }

                if (loot_type == LootType.Pickpocketing && IsFriendlyTo(creature))
                {
                    SendLootRelease(guid);
                    return;
                }

                loot = creature.loot;

                if (loot_type == LootType.Pickpocketing)
                {
                    if (loot.loot_type != LootType.Pickpocketing)
                    {
                        if (creature.CanGeneratePickPocketLoot())
                        {
                            creature.StartPickPocketRefillTimer();
                            loot.clear();

                            uint lootid = creature.GetCreatureTemplate().PickPocketId;
                            if (lootid != 0)
                                loot.FillLoot(lootid, LootStorage.Pickpocketing, this, true);

                            // Generate extra money for pick pocket loot
                            uint a = RandomHelper.URand(0, creature.getLevel() / 2);
                            uint b = RandomHelper.URand(0, getLevel() / 2);
                            loot.gold = (uint)(10 * (a + b) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
                            permission = PermissionTypes.Owner;
                        }
                        else
                        {
                            SendLootError(loot.GetGUID(), guid, LootError.AlreadPickPocketed);
                            return;
                        }
                    }
                }
                else
                {
                    if (loot.loot_type == LootType.None)
                    {
                        // for creature, loot is filled when creature is killed.
                        Group group = creature.GetLootRecipientGroup();
                        if (group)
                        {
                            switch (group.GetLootMethod())
                            {
                                case LootMethod.GroupLoot:
                                    // GroupLoot: rolls items over threshold. Items with quality < threshold, round robin
                                    group.GroupLoot(loot, creature);
                                    break;
                                case LootMethod.MasterLoot:
                                    group.MasterLoot(loot, creature);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    if (loot.loot_type == LootType.Skinning)
                    {
                        loot_type = LootType.Skinning;
                        permission = creature.GetSkinner() == GetGUID() ? PermissionTypes.Owner : PermissionTypes.None;
                    }
                    else if (loot_type == LootType.Skinning)
                    {
                        loot.clear();
                        loot.FillLoot(creature.GetCreatureTemplate().SkinLootId, LootStorage.Skinning, this, true);
                        creature.SetSkinner(GetGUID());
                        permission = PermissionTypes.Owner;
                    }
                    // set group rights only for loot_type != LOOT_SKINNING
                    else
                    {
                        if (creature.GetLootRecipientGroup())
                        {
                            Group group = GetGroup();
                            if (group == creature.GetLootRecipientGroup())
                            {
                                switch (group.GetLootMethod())
                                {
                                    case LootMethod.MasterLoot:
                                        permission = PermissionTypes.Master;
                                        break;
                                    case LootMethod.FreeForAll:
                                        permission = PermissionTypes.All;
                                        break;
                                    default:
                                        permission = PermissionTypes.Group;
                                        break;
                                }
                            }
                            else
                                permission = PermissionTypes.None;
                        }
                        else if (creature.GetLootRecipient() == this)
                            permission = PermissionTypes.Owner;
                        else
                            permission = PermissionTypes.None;
                    }
                }
            }

            // LOOT_INSIGNIA and LOOT_FISHINGHOLE unsupported by client
            switch (loot_type)
            {
                case LootType.Insignia:
                    loot_type = LootType.Skinning;
                    break;
                case LootType.Fishinghole:
                case LootType.FishingJunk:
                    loot_type = LootType.Fishing;
                    break;
                default: break;
            }

            // need know merged fishing/corpse loot type for achievements
            loot.loot_type = loot_type;

            if (permission != PermissionTypes.None)
            {
                LootMethod _lootMethod = LootMethod.FreeForAll;
                Group group = GetGroup();
                if (group)
                {
                    Creature creature = GetMap().GetCreature(guid);
                    if (creature)
                    {
                        Player recipient = creature.GetLootRecipient();
                        if (recipient)
                        {
                            if (group == recipient.GetGroup())
                                _lootMethod = group.GetLootMethod();
                        }
                    }
                }

                if (!aeLooting)
                    SetLootGUID(guid);

                LootResponse packet = new LootResponse();
                packet.Owner = guid;
                packet.LootObj = loot.GetGUID();
                packet.LootMethod = _lootMethod;
                packet.AcquireReason = (byte)loot_type;
                packet.Acquired = true; // false == No Loot (this too^^)
                packet.AELooting = aeLooting;
                loot.BuildLootResponse(packet, this, permission);
                SendPacket(packet);

                // add 'this' player as one of the players that are looting 'loot'
                loot.AddLooter(GetGUID());
                m_AELootView[loot.GetGUID()] = guid;
            }
            else
                SendLootError(loot.GetGUID(), guid, LootError.DidntKill);

            if (loot_type == LootType.Corpse && !guid.IsItem())
                SetFlag(UnitFields.Flags, UnitFlags.Looting);
        }

        public void SendLootError(ObjectGuid lootObj, ObjectGuid owner, LootError error)
        {
            LootResponse packet = new LootResponse();
            packet.LootObj = lootObj;
            packet.Owner = owner;
            packet.Acquired = false;
            packet.FailureReason = error;
            SendPacket(packet);
        }

        public void SendNotifyLootMoneyRemoved(ObjectGuid lootObj)
        {
            CoinRemoved packet = new CoinRemoved();
            packet.LootObj = lootObj;
            SendPacket(packet);
        }

        public void SendNotifyLootItemRemoved(ObjectGuid lootObj, byte lootSlot)
        {
            LootRemoved packet = new LootRemoved();
            packet.Owner = GetLootWorldObjectGUID(lootObj);
            packet.LootObj = lootObj;
            packet.LootListID = (byte)(lootSlot + 1);
            SendPacket(packet);
        }

        void SendEquipmentSetList()
        {
            LoadEquipmentSet data = new LoadEquipmentSet();

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
                var equipmentSetInfo = _equipmentSets.LookupByKey(newEqSet.SetID);
                if (equipmentSetInfo == null || equipmentSetInfo.Data.Guid != newEqSet.Guid)
                {
                    Log.outError(LogFilter.Player, "Player {0} tried to save equipment set {1} (index: {2}), but that equipment set not found!", GetName(), newEqSet.Guid, newEqSet.SetID);
                    return;
                }
            }

            ulong setGuid = (newEqSet.Guid != 0) ? newEqSet.Guid : Global.ObjectMgr.GenerateEquipmentSetGuid();

            EquipmentSetInfo eqSlot = _equipmentSets[setGuid];
            eqSlot.Data = newEqSet;

            if (eqSlot.Data.Guid == 0)
            {
                eqSlot.Data.Guid = setGuid;

                EquipmentSetID data = new EquipmentSetID();
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

        //Void Storage
        public bool IsVoidStorageUnlocked() { return HasFlag(PlayerFields.Flags, PlayerFlags.VoidUnlocked); }
        public void UnlockVoidStorage() { SetFlag(PlayerFields.Flags, PlayerFlags.VoidUnlocked); }
        public void LockVoidStorage() { RemoveFlag(PlayerFields.Flags, PlayerFlags.VoidUnlocked); }

        public byte GetNextVoidStorageFreeSlot()
        {
            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
                if (_voidStorageItems[i] == null) // unused item
                    return i;

            return SharedConst.VoidStorageMaxSlot;
        }

        public byte GetNumOfVoidStorageFreeSlots()
        {
            byte count = 0;

            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
                if (_voidStorageItems[i] == null)
                    count++;

            return count;
        }

        public byte AddVoidStorageItem(VoidStorageItem item)
        {
            byte slot = GetNextVoidStorageFreeSlot();

            if (slot >= SharedConst.VoidStorageMaxSlot)
            {
                GetSession().SendVoidStorageTransferResult(VoidTransferError.Full);
                return 255;
            }

            _voidStorageItems[slot] = item;
            return slot;
        }

        public void DeleteVoidStorageItem(byte slot)
        {
            if (slot >= SharedConst.VoidStorageMaxSlot)
            {
                GetSession().SendVoidStorageTransferResult(VoidTransferError.InternalError1);
                return;
            }

            _voidStorageItems[slot] = null;
        }

        public bool SwapVoidStorageItem(byte oldSlot, byte newSlot)
        {
            if (oldSlot >= SharedConst.VoidStorageMaxSlot || newSlot >= SharedConst.VoidStorageMaxSlot || oldSlot == newSlot)
                return false;
            
            _voidStorageItems.Swap(newSlot, oldSlot);
            return true;
        }

        public VoidStorageItem GetVoidStorageItem(byte slot)
        {
            if (slot >= SharedConst.VoidStorageMaxSlot)
            {
                GetSession().SendVoidStorageTransferResult(VoidTransferError.InternalError1);
                return null;
            }

            return _voidStorageItems[slot];
        }

        public VoidStorageItem GetVoidStorageItem(ulong id, out byte slot)
        {
            slot = 0;
            for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
            {
                if (_voidStorageItems[i] != null && _voidStorageItems[i].ItemId == id)
                {
                    slot = i;
                    return _voidStorageItems[i];
                }
            }

            return null;
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
    }
}
