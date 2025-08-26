// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
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
            info.PartnerIsSameBnetAccount = trader != null && trader.GetSession().GetBattlenetAccountId() == GetBattlenetAccountId();
            SendPacket(info);
        }

        [WorldPacketHandler(ClientOpcodes.IgnoreTrade)]
        void HandleIgnoreTradeOpcode(IgnoreTrade packet) { }

        [WorldPacketHandler(ClientOpcodes.BusyTrade)]
        void HandleBusyTradeOpcode(BusyTrade packet) { }

        public void SendUpdateTrade(bool trader_data = true)
        {
            TradeData view_trade = trader_data ? GetPlayer().GetTradeData().GetTraderData() : GetPlayer().GetTradeData();

            TradeUpdated tradeUpdated = new();
            tradeUpdated.WhichPlayer = (byte)(trader_data ? 1 : 0);
            tradeUpdated.ClientStateIndex = view_trade.GetClientStateIndex();
            tradeUpdated.CurrentStateIndex = view_trade.GetServerStateIndex();
            tradeUpdated.Gold = view_trade.GetMoney();
            tradeUpdated.ProposedEnchantment = (int)view_trade.GetSpell();

            for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
            {
                Item item = view_trade.GetItem((TradeSlots)i);
                if (item != null)
                {
                    TradeUpdated.TradeItem tradeItem = new();
                    tradeItem.Slot = i;
                    tradeItem.Item = new ItemInstance(item);
                    tradeItem.StackCount = (int)item.GetCount();
                    tradeItem.GiftCreator = item.GetGiftCreator();
                    if (!item.IsWrapped())
                    {
                        TradeUpdated.UnwrappedTradeItem unwrappedItem = new();
                        unwrappedItem.EnchantID = (int)item.GetEnchantmentId(EnchantmentSlot.Perm);
                        unwrappedItem.OnUseEnchantmentID = (int)item.GetEnchantmentId(EnchantmentSlot.Use);
                        unwrappedItem.Creator = item.GetCreator();
                        unwrappedItem.Charges = item.GetSpellCharges();
                        unwrappedItem.Lock = item.GetTemplate().GetLockID() != 0 && !item.HasItemFlag(ItemFieldFlags.Unlocked);
                        unwrappedItem.MaxDurability = item.m_itemData.MaxDurability;
                        unwrappedItem.Durability = item.m_itemData.Durability;

                        tradeItem.Unwrapped = unwrappedItem;

                        byte g = 0;
                        foreach (SocketedGem gemData in item.m_itemData.Gems)
                        {
                            if (gemData.ItemId != 0)
                            {
                                ItemGemData gem = new();
                                gem.Slot = g;
                                gem.Item = new ItemInstance(gemData);
                                tradeItem.Unwrapped.Gems.Add(gem);
                            }
                            ++g;
                        }
                    }
                    tradeUpdated.Items.Add(tradeItem);
                }
            }

            SendPacket(tradeUpdated);
        }

        void MoveItems(Item[] myItems, Item[] hisItems)
        {
            Player trader = GetPlayer().GetTrader();
            if (trader == null)
                return;

            for (byte i = 0; i < (int)TradeSlots.TradedCount; ++i)
            {
                List<ItemPosCount> traderDst = new();
                List<ItemPosCount> playerDst = new();
                bool traderCanTrade = (myItems[i] == null || trader.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, traderDst, myItems[i], false) == InventoryResult.Ok);
                bool playerCanTrade = (hisItems[i] == null || GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, playerDst, hisItems[i], false) == InventoryResult.Ok);
                if (traderCanTrade && playerCanTrade)
                {
                    // Ok, if trade item exists and can be stored
                    // If we trade in both directions we had to check, if the trade will work before we actually do it
                    // A roll back is not possible after we stored it
                    if (myItems[i] != null)
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
                        if (myItems[i].m_itemData.CreatePlayedTime != 0)
                            myItems[i].SetCreatePlayedTime(trader.GetTotalPlayedTime() - (GetPlayer().GetTotalPlayedTime() - myItems[i].m_itemData.CreatePlayedTime));
                        // store
                        trader.MoveItemToInventory(traderDst, myItems[i], true, true);
                    }
                    if (hisItems[i] != null)
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
                        if (hisItems[i].m_itemData.CreatePlayedTime != 0)
                            hisItems[i].SetCreatePlayedTime(GetPlayer().GetTotalPlayedTime() - (trader.GetTotalPlayedTime() - hisItems[i].m_itemData.CreatePlayedTime));
                        // store
                        GetPlayer().MoveItemToInventory(playerDst, hisItems[i], true, true);
                    }
                }
                else
                {
                    // in case of fatal error log error message
                    // return the already removed items to the original owner
                    if (myItems[i] != null)
                    {
                        if (!traderCanTrade)
                            Log.outError(LogFilter.Network, "trader can't store item: {0}", myItems[i].GetGUID().ToString());
                        if (GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, playerDst, myItems[i], false) == InventoryResult.Ok)
                            GetPlayer().MoveItemToInventory(playerDst, myItems[i], true, true);
                        else
                            Log.outError(LogFilter.Network, "player can't take item back: {0}", myItems[i].GetGUID().ToString());
                    }
                    // return the already removed items to the original owner
                    if (hisItems[i] != null)
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

        static void SetAcceptTradeMode(TradeData myTrade, TradeData hisTrade, Item[] myItems, Item[] hisItems)
        {
            myTrade.SetInAcceptProcess(true);
            hisTrade.SetInAcceptProcess(true);

            // store items in local list and set 'in-trade' flag
            for (byte i = 0; i < (int)TradeSlots.Count; ++i)
            {
                Item item = myTrade.GetItem((TradeSlots)i);
                if (item != null)
                {
                    Log.outDebug(LogFilter.Network, "player trade item {0} bag: {1} slot: {2}", item.GetGUID().ToString(), item.GetBagSlot(), item.GetSlot());
                    //Can return null
                    myItems[i] = item;
                    myItems[i].SetInTrade();
                }
                item = hisTrade.GetItem((TradeSlots)i);
                if (item != null)
                {
                    Log.outDebug(LogFilter.Network, "partner trade item {0} bag: {1} slot: {2}", item.GetGUID().ToString(), item.GetBagSlot(), item.GetSlot());
                    hisItems[i] = item;
                    hisItems[i].SetInTrade();
                }
            }
        }

        static void ClearAcceptTradeMode(TradeData myTrade, TradeData hisTrade)
        {
            myTrade.SetInAcceptProcess(false);
            hisTrade.SetInAcceptProcess(false);
        }

        static void ClearAcceptTradeMode(Item[] myItems, Item[] hisItems)
        {
            // clear 'in-trade' flag
            for (byte i = 0; i < (int)TradeSlots.Count; ++i)
            {
                if (myItems[i] != null)
                    myItems[i].SetInTrade(false);
                if (hisItems[i] != null)
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

            TradeStatusPkt info = new();
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
                if (item != null)
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

                    if (Player.IsAccountBankPos(item.GetSlot(), item.GetBagSlot()))
                    {
                        info.Status = TradeStatus.Failed;
                        info.BagResult = InventoryResult.CantTradeAccountItem;
                        SendTradeStatus(info);
                        return;
                    }
                }

                item = his_trade.GetItem((TradeSlots)i);
                if (item != null)
                {
                    if (!item.CanBeTraded(false, true))
                    {
                        info.Status = TradeStatus.Cancelled;
                        SendTradeStatus(info);
                        return;
                    }
                }

                if (Player.IsAccountBankPos(item.GetSlot(), item.GetBagSlot()))
                {
                    info.Status = TradeStatus.Failed;
                    info.BagResult = InventoryResult.CantTradeAccountItem;
                    SendTradeStatus(info);
                    return;
                }
            }

            if (his_trade.IsAccepted())
            {
                SetAcceptTradeMode(my_trade, his_trade, myItems, hisItems);

                Spell my_spell = null;
                SpellCastTargets my_targets = new();

                Spell his_spell = null;
                SpellCastTargets his_targets = new();

                // not accept if spell can't be casted now (cheating)
                uint my_spell_id = my_trade.GetSpell();
                if (my_spell_id != 0)
                {
                    SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(my_spell_id, _player.GetMap().GetDifficultyID());
                    Item castItem = my_trade.GetSpellCastItem();

                    if (spellEntry == null || his_trade.GetItem(TradeSlots.NonTraded) == null ||
                        (my_trade.HasSpellCastItem() && castItem == null))
                    {
                        ClearAcceptTradeMode(my_trade, his_trade);
                        ClearAcceptTradeMode(myItems, hisItems);

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

                        ClearAcceptTradeMode(my_trade, his_trade);
                        ClearAcceptTradeMode(myItems, hisItems);

                        my_spell.Dispose();
                        my_trade.SetSpell(0);
                        return;
                    }
                }

                // not accept if spell can't be casted now (cheating)
                uint his_spell_id = his_trade.GetSpell();
                if (his_spell_id != 0)
                {
                    SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(his_spell_id, trader.GetMap().GetDifficultyID());
                    Item castItem = his_trade.GetSpellCastItem();

                    if (spellEntry == null || my_trade.GetItem(TradeSlots.NonTraded) == null || (his_trade.HasSpellCastItem() && castItem == null))
                    {
                        his_trade.SetSpell(0);

                        ClearAcceptTradeMode(my_trade, his_trade);
                        ClearAcceptTradeMode(myItems, hisItems);
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

                        ClearAcceptTradeMode(my_trade, his_trade);
                        ClearAcceptTradeMode(myItems, hisItems);

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
                TradeStatusPkt myCanCompleteInfo = new();
                TradeStatusPkt hisCanCompleteInfo = new();
                hisCanCompleteInfo.BagResult = trader.CanStoreItems(myItems, (int)TradeSlots.TradedCount, ref hisCanCompleteInfo.ItemID);
                myCanCompleteInfo.BagResult = GetPlayer().CanStoreItems(hisItems, (int)TradeSlots.TradedCount, ref myCanCompleteInfo.ItemID);

                ClearAcceptTradeMode(myItems, hisItems);

                // in case of missing space report error
                if (myCanCompleteInfo.BagResult != InventoryResult.Ok)
                {
                    ClearAcceptTradeMode(my_trade, his_trade);

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
                    ClearAcceptTradeMode(my_trade, his_trade);

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
                    if (myItems[i] != null)
                    {
                        myItems[i].SetGiftCreator(GetPlayer().GetGUID());
                        GetPlayer().MoveItemFromInventory(myItems[i].GetBagSlot(), myItems[i].GetSlot(), true);
                    }
                    if (hisItems[i] != null)
                    {
                        hisItems[i].SetGiftCreator(trader.GetGUID());
                        trader.MoveItemFromInventory(hisItems[i].GetBagSlot(), hisItems[i].GetSlot(), true);
                    }
                }

                // execute trade: 2. store
                MoveItems(myItems, hisItems);

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

                if (my_spell != null)
                    my_spell.Prepare(my_targets);

                if (his_spell != null)
                    his_spell.Prepare(his_targets);

                // cleanup
                ClearAcceptTradeMode(my_trade, his_trade);
                GetPlayer().SetTradeData(null);
                trader.SetTradeData(null);

                // desynchronized with the other saves here (SaveInventoryAndGoldToDB() not have own transaction guards)
                SQLTransaction trans = new();
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

            TradeStatusPkt info = new();
            my_trade.GetTrader().GetSession().SendTradeStatus(info);
            SendTradeStatus(info);
        }

        public void SendCancelTrade()
        {
            if (PlayerRecentlyLoggedOut() || PlayerLogout())
                return;

            TradeStatusPkt info = new();
            info.Status = TradeStatus.Cancelled;
            SendTradeStatus(info);
        }

        [WorldPacketHandler(ClientOpcodes.CancelTrade, Status = SessionStatus.LoggedinOrRecentlyLogout)]
        void HandleCancelTrade(CancelTrade cancelTrade)
        {
            // sent also after LOGOUT COMPLETE
            if (GetPlayer() != null)                                             // needed because STATUS_LOGGEDIN_OR_RECENTLY_LOGGOUT
                GetPlayer().TradeCancel(true);
        }

        [WorldPacketHandler(ClientOpcodes.InitiateTrade)]
        void HandleInitiateTrade(InitiateTrade initiateTrade)
        {
            if (GetPlayer().GetTradeData() != null)
                return;

            TradeStatusPkt info = new();
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

            if (IsLogingOut())
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

            if (GetPlayer().GetLevel() < WorldConfig.GetIntValue(WorldCfg.TradeLevelReq))
            {
                SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.TradeReq), WorldConfig.GetIntValue(WorldCfg.TradeLevelReq));
                info.Status = TradeStatus.Failed;
                SendTradeStatus(info);
                return;
            }


            Player pOther = Global.ObjAccessor.FindPlayer(initiateTrade.Guid);
            if (pOther == null)
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

            if (pOther.GetSession().IsLogingOut())
            {
                info.Status = TradeStatus.TargetLoggingOut;
                SendTradeStatus(info);
                return;
            }

            if (pOther.GetSocial().HasIgnore(GetPlayer().GetGUID(), GetPlayer().GetSession().GetAccountGUID()))
            {
                info.Status = TradeStatus.PlayerIgnored;
                SendTradeStatus(info);
                return;
            }

            if ((pOther.GetTeam() != GetPlayer().GetTeam() || 
                pOther.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode) ||
                GetPlayer().HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode)) &&
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

            if (pOther.GetLevel() < WorldConfig.GetIntValue(WorldCfg.TradeLevelReq))
            {
                SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.TradeOtherReq), WorldConfig.GetIntValue(WorldCfg.TradeLevelReq));
                info.Status = TradeStatus.Failed;
                SendTradeStatus(info);
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

            TradeStatusPkt info = new();
            // invalid slot number
            if (setTradeItem.TradeSlot >= (byte)TradeSlots.Count)
            {
                info.Status = TradeStatus.Cancelled;
                SendTradeStatus(info);
                return;
            }

            // check cheating, can't fail with correct client operations
            Item item = GetPlayer().GetItemByPos(setTradeItem.PackSlot, setTradeItem.ItemSlotInPack);
            if (item == null || (setTradeItem.TradeSlot != (byte)TradeSlots.NonTraded && !item.CanBeTraded(false, true)))
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
