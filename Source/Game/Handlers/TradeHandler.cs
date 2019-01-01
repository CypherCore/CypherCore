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
using Game.Entities;
using Game.Network;
using Game.Network.Packets;
using Game.Spells;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        public void SendTradeStatus(TradeStatusPkt info)
        {
            info.Clear();   // reuse packet
            Player trader = GetPlayer().GetTrader();
            info.PartnerIsSameBnetAccount = trader && trader.GetSession().GetBattlenetAccountId() == GetBattlenetAccountId();
            SendPacket(info);
        }

        [WorldPacketHandler(ClientOpcodes.IgnoreTrade)]
        void HandleIgnoreTradeOpcode(IgnoreTrade packet) { }

        [WorldPacketHandler(ClientOpcodes.BusyTrade)]
        void HandleBusyTradeOpcode(BusyTrade packet) { }

        public void SendUpdateTrade(bool trader_data = true)
        {
            TradeData view_trade = trader_data ? GetPlayer().GetTradeData().GetTraderData() : GetPlayer().GetTradeData();

            TradeUpdated tradeUpdated = new TradeUpdated();
            tradeUpdated.WhichPlayer = (byte)(trader_data ? 1 : 0);
            tradeUpdated.ClientStateIndex = view_trade.GetClientStateIndex();
            tradeUpdated.CurrentStateIndex = view_trade.GetServerStateIndex();
            tradeUpdated.Gold = view_trade.GetMoney();
            tradeUpdated.ProposedEnchantment = (int)view_trade.GetSpell();

            for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
            {
                Item item = view_trade.GetItem((TradeSlots)i);
                if (item)
                {
                    TradeUpdated.TradeItem tradeItem = new TradeUpdated.TradeItem();
                    tradeItem.Slot = i;
                    tradeItem.Item = new ItemInstance(item);
                    tradeItem.StackCount = (int)item.GetCount();
                    tradeItem.GiftCreator = item.GetGuidValue(ItemFields.GiftCreator);
                    if (!item.HasFlag(ItemFields.Flags, ItemFieldFlags.Wrapped))
                    {
                        tradeItem.Unwrapped.HasValue = true;
                        TradeUpdated.UnwrappedTradeItem unwrappedItem = tradeItem.Unwrapped.Value;
                        unwrappedItem.EnchantID = (int)item.GetEnchantmentId(EnchantmentSlot.Perm);
                        unwrappedItem.OnUseEnchantmentID = (int)item.GetEnchantmentId(EnchantmentSlot.Use);
                        unwrappedItem.Creator = item.GetGuidValue(ItemFields.Creator);
                        unwrappedItem.Charges = item.GetSpellCharges();
                        unwrappedItem.Lock = item.GetTemplate().GetLockID() != 0 && !item.HasFlag(ItemFields.Flags, ItemFieldFlags.Unlocked);
                        unwrappedItem.MaxDurability = item.GetUInt32Value(ItemFields.MaxDurability);
                        unwrappedItem.Durability = item.GetUInt32Value(ItemFields.Durability);

                        byte g = 0;
                        foreach (ItemDynamicFieldGems gemData in item.GetGems())
                        {
                            if (gemData.ItemId != 0)
                            {
                                ItemGemData gem = new ItemGemData();
                                gem.Slot = g;
                                gem.Item = new ItemInstance(gemData);
                                tradeItem.Unwrapped.Value.Gems.Add(gem);
                            }
                            ++g;
                        }
                    }
                    tradeUpdated.Items.Add(tradeItem);
                }
            }

            SendPacket(tradeUpdated);
        }

        void moveItems(Item[] myItems, Item[] hisItems)
        {
            Player trader = GetPlayer().GetTrader();
            if (!trader)
                return;

            for (byte i = 0; i < (int)TradeSlots.TradedCount; ++i)
            {
                List<ItemPosCount> traderDst = new List<ItemPosCount>();
                List<ItemPosCount> playerDst = new List<ItemPosCount>();
                bool traderCanTrade = (myItems[i] == null || trader.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, traderDst, myItems[i], false) == InventoryResult.Ok);
                bool playerCanTrade = (hisItems[i] == null || GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, playerDst, hisItems[i], false) == InventoryResult.Ok);
                if (traderCanTrade && playerCanTrade)
                {
                    // Ok, if trade item exists and can be stored
                    // If we trade in both directions we had to check, if the trade will work before we actually do it
                    // A roll back is not possible after we stored it
                    if (myItems[i])
                    {
                        // logging
                        Log.outDebug(LogFilter.Network, "partner storing: {0}", myItems[i].GetGUID().ToString());

                        if (HasPermission(RBACPermissions.LogGmTrade))
                        {
                            Log.outCommand(_player.GetSession().GetAccountId(), "GM {0} (Account: {1}) trade: {2} (Entry: {3} Count: {4}) to player: {5} (Account: {6})",
                                GetPlayer().GetName(), GetPlayer().GetSession().GetAccountId(), myItems[i].GetTemplate().GetName(), myItems[i].GetEntry(), myItems[i].GetCount(),
                                trader.GetName(), trader.GetSession().GetAccountId());
                        }

                        // adjust time (depends on /played)
                        if (myItems[i].HasFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable))
                            myItems[i].SetUInt32Value(ItemFields.CreatePlayedTime, trader.GetTotalPlayedTime() - (GetPlayer().GetTotalPlayedTime() - myItems[i].GetUInt32Value(ItemFields.CreatePlayedTime)));
                        // store
                        trader.MoveItemToInventory(traderDst, myItems[i], true, true);
                    }
                    if (hisItems[i])
                    {
                        // logging
                        Log.outDebug(LogFilter.Network, "player storing: {0}", hisItems[i].GetGUID().ToString());

                        if (HasPermission(RBACPermissions.LogGmTrade))
                        {
                            Log.outCommand(trader.GetSession().GetAccountId(), "GM {0} (Account: {1}) trade: {2} (Entry: {3} Count: {4}) to player: {5} (Account: {6})",
                                trader.GetName(), trader.GetSession().GetAccountId(), hisItems[i].GetTemplate().GetName(), hisItems[i].GetEntry(), hisItems[i].GetCount(),
                                GetPlayer().GetName(), GetPlayer().GetSession().GetAccountId());
                        }
                        

                        // adjust time (depends on /played)
                        if (hisItems[i].HasFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable))
                            hisItems[i].SetUInt32Value(ItemFields.CreatePlayedTime, GetPlayer().GetTotalPlayedTime() - (trader.GetTotalPlayedTime() - hisItems[i].GetUInt32Value(ItemFields.CreatePlayedTime)));
                        // store
                        GetPlayer().MoveItemToInventory(playerDst, hisItems[i], true, true);
                    }
                }
                else
                {
                    // in case of fatal error log error message
                    // return the already removed items to the original owner
                    if (myItems[i])
                    {
                        if (!traderCanTrade)
                            Log.outError(LogFilter.Network, "trader can't store item: {0}", myItems[i].GetGUID().ToString());
                        if (GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, playerDst, myItems[i], false) == InventoryResult.Ok)
                            GetPlayer().MoveItemToInventory(playerDst, myItems[i], true, true);
                        else
                            Log.outError(LogFilter.Network, "player can't take item back: {0}", myItems[i].GetGUID().ToString());
                    }
                    // return the already removed items to the original owner
                    if (hisItems[i])
                    {
                        if (!playerCanTrade)
                            Log.outError(LogFilter.Network, "player can't store item: {0}", hisItems[i].GetGUID().ToString());
                        if (trader.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, traderDst, hisItems[i], false) == InventoryResult.Ok)
                            trader.MoveItemToInventory(traderDst, hisItems[i], true, true);
                        else
                            Log.outError(LogFilter.Network, "trader can't take item back: {0}", hisItems[i].GetGUID().ToString());
                    }
                }
            }
        }

        static void setAcceptTradeMode(TradeData myTrade, TradeData hisTrade, Item[] myItems, Item[] hisItems)
        {
            myTrade.SetInAcceptProcess(true);
            hisTrade.SetInAcceptProcess(true);

            // store items in local list and set 'in-trade' flag
            for (byte i = 0; i < (int)TradeSlots.Count; ++i)
            {
                Item item = myTrade.GetItem((TradeSlots)i);
                if (item)
                {
                    Log.outDebug(LogFilter.Network, "player trade item {0} bag: {1} slot: {2}", item.GetGUID().ToString(), item.GetBagSlot(), item.GetSlot());
                    //Can return null
                    myItems[i] = item;
                    myItems[i].SetInTrade();
                }
                item = hisTrade.GetItem((TradeSlots)i);
                if (item)
                {
                    Log.outDebug(LogFilter.Network, "partner trade item {0} bag: {1} slot: {2}", item.GetGUID().ToString(), item.GetBagSlot(), item.GetSlot());
                    hisItems[i] = item;
                    hisItems[i].SetInTrade();
                }
            }
        }

        static void clearAcceptTradeMode(TradeData myTrade, TradeData hisTrade)
        {
            myTrade.SetInAcceptProcess(false);
            hisTrade.SetInAcceptProcess(false);
        }

        static void clearAcceptTradeMode(Item[] myItems, Item[] hisItems)
        {
            // clear 'in-trade' flag
            for (byte i = 0; i < (int)TradeSlots.Count; ++i)
            {
                if (myItems[i])
                    myItems[i].SetInTrade(false);
                if (hisItems[i])
                    hisItems[i].SetInTrade(false);
            }
        }

        [WorldPacketHandler(ClientOpcodes.AcceptTrade)]
        void HandleAcceptTrade(AcceptTrade acceptTrade)
        {
            TradeData my_trade = GetPlayer().GetTradeData();
            if (my_trade == null)
                return;

            Player trader = my_trade.GetTrader();

            TradeData his_trade = trader.GetTradeData();
            if (his_trade == null)
                return;

            Item[] myItems = new Item[(int)TradeSlots.Count];
            Item[] hisItems = new Item[(int)TradeSlots.Count];

            // set before checks for propertly undo at problems (it already set in to client)
            my_trade.SetAccepted(true);

            TradeStatusPkt info = new TradeStatusPkt();
            if (his_trade.GetServerStateIndex() != acceptTrade.StateIndex)
            {
                info.Status = TradeStatus.StateChanged;
                SendTradeStatus(info);
                my_trade.SetAccepted(false);
                return;
            }

            if (!GetPlayer().IsWithinDistInMap(trader, 11.11f, false))
            {
                info.Status = TradeStatus.TooFarAway;
                SendTradeStatus(info);
                my_trade.SetAccepted(false);
                return;
            }

            // not accept case incorrect money amount
            if (!GetPlayer().HasEnoughMoney(my_trade.GetMoney()))
            {
                info.Status = TradeStatus.Failed;
                info.BagResult = InventoryResult.NotEnoughMoney;
                SendTradeStatus(info);
                my_trade.SetAccepted(false, true);
                return;
            }

            // not accept case incorrect money amount
            if (!trader.HasEnoughMoney(his_trade.GetMoney()))
            {
                info.Status = TradeStatus.Failed;
                info.BagResult = InventoryResult.NotEnoughMoney;
                trader.GetSession().SendTradeStatus(info);
                his_trade.SetAccepted(false, true);
                return;
            }

            if (GetPlayer().GetMoney() >= PlayerConst.MaxMoneyAmount - his_trade.GetMoney())
            {
                info.Status = TradeStatus.Failed;
                info.BagResult = InventoryResult.TooMuchGold;
                SendTradeStatus(info);
                my_trade.SetAccepted(false, true);
                return;
            }

            if (trader.GetMoney() >= PlayerConst.MaxMoneyAmount - my_trade.GetMoney())
            {
                info.Status = TradeStatus.Failed;
                info.BagResult = InventoryResult.TooMuchGold;
                trader.GetSession().SendTradeStatus(info);
                his_trade.SetAccepted(false, true);
                return;
            }

            // not accept if some items now can't be trade (cheating)
            for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
            {
                Item item = my_trade.GetItem((TradeSlots)i);
                if (item)
                {
                    if (!item.CanBeTraded(false, true))
                    {
                        info.Status = TradeStatus.Cancelled;
                        SendTradeStatus(info);
                        return;
                    }

                    if (item.IsBindedNotWith(trader))
                    {
                        info.Status = TradeStatus.Failed;
                        info.BagResult = InventoryResult.TradeBoundItem;
                        SendTradeStatus(info);
                        return;
                    }
                }
                item = his_trade.GetItem((TradeSlots)i);
                if (item)
                {
                    if (!item.CanBeTraded(false, true))
                    {
                        info.Status = TradeStatus.Cancelled;
                        SendTradeStatus(info);
                        return;
                    }
                }
            }

            if (his_trade.IsAccepted())
            {
                setAcceptTradeMode(my_trade, his_trade, myItems, hisItems);

                Spell my_spell = null;
                SpellCastTargets my_targets = new SpellCastTargets();

                Spell his_spell = null;
                SpellCastTargets his_targets = new SpellCastTargets();

                // not accept if spell can't be casted now (cheating)
                uint my_spell_id = my_trade.GetSpell();
                if (my_spell_id != 0)
                {
                    SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(my_spell_id);
                    Item castItem = my_trade.GetSpellCastItem();

                    if (spellEntry == null || !his_trade.GetItem(TradeSlots.NonTraded) ||
                        (my_trade.HasSpellCastItem() && !castItem))
                    {
                        clearAcceptTradeMode(my_trade, his_trade);
                        clearAcceptTradeMode(myItems, hisItems);

                        my_trade.SetSpell(0);
                        return;
                    }

                    my_spell = new Spell(GetPlayer(), spellEntry, TriggerCastFlags.FullMask);
                    my_spell.m_CastItem = castItem;
                    my_targets.SetTradeItemTarget(GetPlayer());
                    my_spell.m_targets = my_targets;

                    SpellCastResult res = my_spell.CheckCast(true);
                    if (res != SpellCastResult.SpellCastOk)
                    {
                        my_spell.SendCastResult(res);

                        clearAcceptTradeMode(my_trade, his_trade);
                        clearAcceptTradeMode(myItems, hisItems);

                        my_spell.Dispose();
                        my_trade.SetSpell(0);
                        return;
                    }
                }

                // not accept if spell can't be casted now (cheating)
                uint his_spell_id = his_trade.GetSpell();
                if (his_spell_id != 0)
                {
                    SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(his_spell_id);
                    Item castItem = his_trade.GetSpellCastItem();

                    if (spellEntry == null || !my_trade.GetItem(TradeSlots.NonTraded) || (his_trade.HasSpellCastItem() && !castItem))
                    {
                        his_trade.SetSpell(0);

                        clearAcceptTradeMode(my_trade, his_trade);
                        clearAcceptTradeMode(myItems, hisItems);
                        return;
                    }

                    his_spell = new Spell(trader, spellEntry, TriggerCastFlags.FullMask);
                    his_spell.m_CastItem = castItem;
                    his_targets.SetTradeItemTarget(trader);
                    his_spell.m_targets = his_targets;

                    SpellCastResult res = his_spell.CheckCast(true);
                    if (res != SpellCastResult.SpellCastOk)
                    {
                        his_spell.SendCastResult(res);

                        clearAcceptTradeMode(my_trade, his_trade);
                        clearAcceptTradeMode(myItems, hisItems);

                        my_spell.Dispose();
                        his_spell.Dispose();

                        his_trade.SetSpell(0);
                        return;
                    }
                }

                // inform partner client
                info.Status = TradeStatus.Accepted;
                trader.GetSession().SendTradeStatus(info);

                // test if item will fit in each inventory
                TradeStatusPkt myCanCompleteInfo = new TradeStatusPkt();
                TradeStatusPkt hisCanCompleteInfo = new TradeStatusPkt();
                hisCanCompleteInfo.BagResult = trader.CanStoreItems(myItems, (int)TradeSlots.TradedCount, ref hisCanCompleteInfo.ItemID);
                myCanCompleteInfo.BagResult = GetPlayer().CanStoreItems(hisItems, (int)TradeSlots.TradedCount, ref myCanCompleteInfo.ItemID);

                clearAcceptTradeMode(myItems, hisItems);

                // in case of missing space report error
                if (myCanCompleteInfo.BagResult != InventoryResult.Ok)
                {
                    clearAcceptTradeMode(my_trade, his_trade);

                    myCanCompleteInfo.Status = TradeStatus.Failed;
                    trader.GetSession().SendTradeStatus(myCanCompleteInfo);
                    myCanCompleteInfo.FailureForYou = true;
                    SendTradeStatus(myCanCompleteInfo);
                    my_trade.SetAccepted(false);
                    his_trade.SetAccepted(false);
                    return;
                }
                else if (hisCanCompleteInfo.BagResult != InventoryResult.Ok)
                {
                    clearAcceptTradeMode(my_trade, his_trade);

                    hisCanCompleteInfo.Status = TradeStatus.Failed;
                    SendTradeStatus(hisCanCompleteInfo);
                    hisCanCompleteInfo.FailureForYou = true;
                    trader.GetSession().SendTradeStatus(hisCanCompleteInfo);
                    my_trade.SetAccepted(false);
                    his_trade.SetAccepted(false);
                    return;
                }

                // execute trade: 1. remove
                for (byte i = 0; i < (int)TradeSlots.TradedCount; ++i)
                {
                    if (myItems[i])
                    {
                        myItems[i].SetGuidValue(ItemFields.GiftCreator, GetPlayer().GetGUID());
                        GetPlayer().MoveItemFromInventory(myItems[i].GetBagSlot(), myItems[i].GetSlot(), true);
                    }
                    if (hisItems[i])
                    {
                        hisItems[i].SetGuidValue(ItemFields.GiftCreator, trader.GetGUID());
                        trader.MoveItemFromInventory(hisItems[i].GetBagSlot(), hisItems[i].GetSlot(), true);
                    }
                }

                // execute trade: 2. store
                moveItems(myItems, hisItems);

                // logging money                
                if (HasPermission(RBACPermissions.LogGmTrade))
                {
                    if (my_trade.GetMoney() > 0)
                    {
                        Log.outCommand(GetPlayer().GetSession().GetAccountId(), "GM {0} (Account: {1}) give money (Amount: {2}) to player: {3} (Account: {4})",
                            GetPlayer().GetName(), GetPlayer().GetSession().GetAccountId(), my_trade.GetMoney(), trader.GetName(), trader.GetSession().GetAccountId());
                    }

                    if (his_trade.GetMoney() > 0)
                    {
                        Log.outCommand(GetPlayer().GetSession().GetAccountId(), "GM {0} (Account: {1}) give money (Amount: {2}) to player: {3} (Account: {4})",
                            trader.GetName(), trader.GetSession().GetAccountId(), his_trade.GetMoney(), GetPlayer().GetName(), GetPlayer().GetSession().GetAccountId());
                    }
                }
                

                // update money
                GetPlayer().ModifyMoney(-(long)my_trade.GetMoney());
                GetPlayer().ModifyMoney((long)his_trade.GetMoney());
                trader.ModifyMoney(-(long)his_trade.GetMoney());
                trader.ModifyMoney((long)my_trade.GetMoney());

                if (my_spell)
                    my_spell.prepare(my_targets);

                if (his_spell)
                    his_spell.prepare(his_targets);

                // cleanup
                clearAcceptTradeMode(my_trade, his_trade);
                GetPlayer().SetTradeData(null);
                trader.SetTradeData(null);

                // desynchronized with the other saves here (SaveInventoryAndGoldToDB() not have own transaction guards)
                SQLTransaction trans = new SQLTransaction();
                GetPlayer().SaveInventoryAndGoldToDB(trans);
                trader.SaveInventoryAndGoldToDB(trans);
                DB.Characters.CommitTransaction(trans);

                info.Status = TradeStatus.Complete;
                trader.GetSession().SendTradeStatus(info);
                SendTradeStatus(info);
            }
            else
            {
                info.Status = TradeStatus.Accepted;
                trader.GetSession().SendTradeStatus(info);
            }
        }

        [WorldPacketHandler(ClientOpcodes.UnacceptTrade)]
        void HandleUnacceptTrade(UnacceptTrade packet)
        {
            TradeData my_trade = GetPlayer().GetTradeData();
            if (my_trade == null)
                return;

            my_trade.SetAccepted(false, true);
        }

        [WorldPacketHandler(ClientOpcodes.BeginTrade)]
        void HandleBeginTrade(BeginTrade packet)
        {
            TradeData my_trade = GetPlayer().GetTradeData();
            if (my_trade == null)
                return;

            TradeStatusPkt info = new TradeStatusPkt();
            my_trade.GetTrader().GetSession().SendTradeStatus(info);
            SendTradeStatus(info);
        }

        public void SendCancelTrade()
        {
            if (PlayerRecentlyLoggedOut() || PlayerLogout())
                return;

            TradeStatusPkt info = new TradeStatusPkt();
            info.Status = TradeStatus.Cancelled;
            SendTradeStatus(info);
        }

        [WorldPacketHandler(ClientOpcodes.CancelTrade, Status = SessionStatus.LoggedinOrRecentlyLogout)]
        void HandleCancelTrade(CancelTrade cancelTrade)
        {
            // sent also after LOGOUT COMPLETE
            if (GetPlayer())                                             // needed because STATUS_LOGGEDIN_OR_RECENTLY_LOGGOUT
                GetPlayer().TradeCancel(true);
        }

        [WorldPacketHandler(ClientOpcodes.InitiateTrade)]
        void HandleInitiateTrade(InitiateTrade initiateTrade)
        {
            if (GetPlayer().GetTradeData() != null)
                return;

            TradeStatusPkt info = new TradeStatusPkt();
            if (!GetPlayer().IsAlive())
            {
                info.Status = TradeStatus.Dead;
                SendTradeStatus(info);
                return;
            }

            if (GetPlayer().HasUnitState(UnitState.Stunned))
            {
                info.Status = TradeStatus.Stunned;
                SendTradeStatus(info);
                return;
            }

            if (isLogingOut())
            {
                info.Status = TradeStatus.LoggingOut;
                SendTradeStatus(info);
                return;
            }

            if (GetPlayer().IsInFlight())
            {
                info.Status = TradeStatus.TooFarAway;
                SendTradeStatus(info);
                return;
            }

            if (GetPlayer().getLevel() < WorldConfig.GetIntValue(WorldCfg.TradeLevelReq))
            {
                SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.TradeReq), WorldConfig.GetIntValue(WorldCfg.TradeLevelReq));
                return;
            }


            Player pOther = Global.ObjAccessor.FindPlayer(initiateTrade.Guid);
            if (!pOther)
            {
                info.Status = TradeStatus.NoTarget;
                SendTradeStatus(info);
                return;
            }

            if (pOther == GetPlayer() || pOther.GetTradeData() != null)
            {
                info.Status = TradeStatus.PlayerBusy;
                SendTradeStatus(info);
                return;
            }

            if (!pOther.IsAlive())
            {
                info.Status = TradeStatus.TargetDead;
                SendTradeStatus(info);
                return;
            }

            if (pOther.IsInFlight())
            {
                info.Status = TradeStatus.TooFarAway;
                SendTradeStatus(info);
                return;
            }

            if (pOther.HasUnitState(UnitState.Stunned))
            {
                info.Status = TradeStatus.TargetStunned;
                SendTradeStatus(info);
                return;
            }

            if (pOther.GetSession().isLogingOut())
            {
                info.Status = TradeStatus.TargetLoggingOut;
                SendTradeStatus(info);
                return;
            }

            if (pOther.GetSocial().HasIgnore(GetPlayer().GetGUID()))
            {
                info.Status = TradeStatus.PlayerIgnored;
                SendTradeStatus(info);
                return;
            }

            if ((pOther.GetTeam() != GetPlayer().GetTeam() || 
                pOther.HasFlag(PlayerFields.FlagsEx, PlayerFlagsEx.MercenaryMode) ||
                GetPlayer().HasFlag(PlayerFields.FlagsEx, PlayerFlagsEx.MercenaryMode)) &&
                (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideTrade) &&
                !HasPermission(RBACPermissions.AllowTwoSideTrade)))
            {
                info.Status = TradeStatus.WrongFaction;
                SendTradeStatus(info);
                return;
            }

            if (!pOther.IsWithinDistInMap(GetPlayer(), 11.11f, false))
            {
                info.Status = TradeStatus.TooFarAway;
                SendTradeStatus(info);
                return;
            }

            if (pOther.getLevel() < WorldConfig.GetIntValue(WorldCfg.TradeLevelReq))
            {
                SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.TradeOtherReq), WorldConfig.GetIntValue(WorldCfg.TradeLevelReq));
                return;
            }

            // OK start trade
            GetPlayer().SetTradeData(new TradeData(GetPlayer(), pOther));
            pOther.SetTradeData(new TradeData(pOther, GetPlayer()));

            info.Status = TradeStatus.Proposed;
            info.Partner = GetPlayer().GetGUID();
            pOther.GetSession().SendTradeStatus(info);
        }

        [WorldPacketHandler(ClientOpcodes.SetTradeGold)]
        void HandleSetTradeGold(SetTradeGold setTradeGold)
        {
            TradeData my_trade = GetPlayer().GetTradeData();
            if (my_trade == null)
                return;

            my_trade.UpdateClientStateIndex();
            my_trade.SetMoney(setTradeGold.Coinage);
        }

        [WorldPacketHandler(ClientOpcodes.SetTradeItem)]
        void HandleSetTradeItem(SetTradeItem setTradeItem)
        {
            TradeData my_trade = GetPlayer().GetTradeData();
            if (my_trade == null)
                return;

            TradeStatusPkt info = new TradeStatusPkt();
            // invalid slot number
            if (setTradeItem.TradeSlot >= (byte)TradeSlots.Count)
            {
                info.Status = TradeStatus.Cancelled;
                SendTradeStatus(info);
                return;
            }

            // check cheating, can't fail with correct client operations
            Item item = GetPlayer().GetItemByPos(setTradeItem.PackSlot, setTradeItem.ItemSlotInPack);
            if (!item || (setTradeItem.TradeSlot != (byte)TradeSlots.NonTraded && !item.CanBeTraded(false, true)))
            {
                info.Status = TradeStatus.Cancelled;
                SendTradeStatus(info);
                return;
            }

            ObjectGuid iGUID = item.GetGUID();

            // prevent place single item into many trade slots using cheating and client bugs
            if (my_trade.HasItem(iGUID))
            {
                // cheating attempt
                info.Status = TradeStatus.Cancelled;
                SendTradeStatus(info);
                return;
            }

            my_trade.UpdateClientStateIndex();
            if (setTradeItem.TradeSlot != (byte)TradeSlots.NonTraded && item.IsBindedNotWith(my_trade.GetTrader()))
            {
                info.Status = TradeStatus.NotOnTaplist;
                info.TradeSlot = setTradeItem.TradeSlot;
                SendTradeStatus(info);
                return;
            }

            my_trade.SetItem((TradeSlots)setTradeItem.TradeSlot, item);
        }

        [WorldPacketHandler(ClientOpcodes.ClearTradeItem)]
        void HandleClearTradeItem(ClearTradeItem clearTradeItem)
        {
            TradeData my_trade = GetPlayer().GetTradeData();
            if (my_trade == null)
                return;

            my_trade.UpdateClientStateIndex();

            // invalid slot number
            if (clearTradeItem.TradeSlot >= (byte)TradeSlots.Count)
                return;

            my_trade.SetItem((TradeSlots)clearTradeItem.TradeSlot, null);
        }

        [WorldPacketHandler(ClientOpcodes.SetTradeCurrency)]
        void HandleSetTradeCurrency(SetTradeCurrency setTradeCurrency)
        {
        }
    }
}
