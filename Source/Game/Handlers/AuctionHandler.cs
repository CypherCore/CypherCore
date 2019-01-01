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
using Game.DataStorage;
using Game.Entities;
using Game.Mails;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AuctionHelloRequest)]
        void HandleAuctionHelloOpcode(AuctionHelloRequest packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Guid, NPCFlags.Auctioneer);
            if (!unit)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionHelloOpcode - {0} not found or you can't interact with him.", packet.Guid.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendAuctionHello(packet.Guid, unit);
        }

        public void SendAuctionHello(ObjectGuid guid, Creature unit)
        {
            if (GetPlayer().getLevel() < WorldConfig.GetIntValue(WorldCfg.AuctionLevelReq))
            {
                SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.AuctionReq), WorldConfig.GetIntValue(WorldCfg.AuctionLevelReq));
                return;
            }

            AuctionHouseRecord ahEntry = Global.AuctionMgr.GetAuctionHouseEntry(unit.getFaction());
            if (ahEntry == null)
                return;

            AuctionHelloResponse packet = new AuctionHelloResponse();
            packet.Guid = guid;
            packet.OpenForBusiness = true;                         // 3.3.3: 1 - AH enabled, 0 - AH disabled
            SendPacket(packet);
        }

        public void SendAuctionCommandResult(AuctionEntry auction, AuctionAction action, AuctionError errorCode, uint bidError = 0)
        {
            AuctionCommandResult auctionCommandResult = new AuctionCommandResult();
            auctionCommandResult.InitializeAuction(auction);
            auctionCommandResult.Command = action;
            auctionCommandResult.ErrorCode = errorCode;
            SendPacket(auctionCommandResult);
        }

        public void SendAuctionOutBidNotification(AuctionEntry auction, Item item)
        {
            AuctionOutBidNotification packet = new AuctionOutBidNotification();
            packet.BidAmount = auction.bid;
            packet.MinIncrement = auction.GetAuctionOutBid();
            packet.Info.Initialize(auction, item);
            SendPacket(packet);
        }

        public void SendAuctionClosedNotification(AuctionEntry auction, float mailDelay, bool sold, Item item)
        {
            AuctionClosedNotification packet = new AuctionClosedNotification();
            packet.Info.Initialize(auction, item);
            packet.ProceedsMailDelay = mailDelay;
            packet.Sold = sold;
            SendPacket(packet);
        }

        public void SendAuctionWonNotification(AuctionEntry auction, Item item)
        {
            AuctionWonNotification packet = new AuctionWonNotification();
            packet.Info.Initialize(auction, item);
            SendPacket(packet);
        }

        public void SendAuctionOwnerBidNotification(AuctionEntry auction, Item item)
        {
            AuctionOwnerBidNotification packet = new AuctionOwnerBidNotification();
            packet.Info.Initialize(auction, item);
            packet.Bidder = ObjectGuid.Create(HighGuid.Player, auction.bidder);
            packet.MinIncrement = auction.GetAuctionOutBid();
            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionSellItem)]
        void HandleAuctionSellItem(AuctionSellItem packet)
        {
            foreach (var aitem in packet.Items)
                if (aitem.Guid.IsEmpty() || aitem.UseCount == 0 || aitem.UseCount > 1000)
                    return;

            if (packet.MinBid == 0 || packet.RunTime == 0)
                return;

            if (packet.MinBid > PlayerConst.MaxMoneyAmount || packet.BuyoutPrice > PlayerConst.MaxMoneyAmount)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionSellItem - Player {0} ({1}) attempted to sell item with higher price than max gold amount.", GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.DatabaseError);
                return;
            }

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(packet.Auctioneer, NPCFlags.Auctioneer);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionSellItem - {0} not found or you can't interact with him.", packet.Auctioneer.ToString());
                return;
            }

            uint houseId = 0;
            AuctionHouseRecord auctionHouseEntry = Global.AuctionMgr.GetAuctionHouseEntry(creature.getFaction(), ref houseId);
            if (auctionHouseEntry == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionSellItem - {0} has wrong faction.", packet.Auctioneer.ToString());
                return;
            }

            packet.RunTime *= Time.Minute;
            switch (packet.RunTime)
            {
                case 1 * SharedConst.MinAuctionTime:
                case 2 * SharedConst.MinAuctionTime:
                case 4 * SharedConst.MinAuctionTime:
                    break;
                default:
                    return;
            }

            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            uint finalCount = 0;
            Item[] items = new Item[packet.Items.Count];
            for (var i = 0; i < packet.Items.Count; ++i)
            {
                items[i] = GetPlayer().GetItemByGuid(packet.Items[i].Guid);
                if (!items[i])
                {
                    SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.ItemNotFound);
                    return;
                }

                if (Global.AuctionMgr.GetAItem(items[i].GetGUID().GetCounter()) || !items[i].CanBeTraded() || items[i].IsNotEmptyBag() ||
                    items[i].GetTemplate().GetFlags().HasAnyFlag(ItemFlags.Conjured) || items[i].GetUInt32Value(ItemFields.Duration) != 0 ||
                    items[i].GetCount() < packet.Items[i].UseCount)
                {
                    SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.DatabaseError);
                    return;
                }

                finalCount += packet.Items[i].UseCount;
            }

            if (packet.Items.Empty())
            {
                SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.DatabaseError);
                return;
            }

            if (finalCount == 0)
            {
                SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.DatabaseError);
                return;
            }

            // check if there are 2 identical guids, in this case user is most likely cheating
            for (int i = 0; i < packet.Items.Count; ++i)
            {
                for (int j = i + 1; j < packet.Items.Count; ++j)
                {
                    if (packet.Items[i].Guid == packet.Items[j].Guid)
                    {
                        SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.DatabaseError);
                        return;
                    }
                    if (items[i].GetEntry() != items[j].GetEntry())
                    {
                        SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.ItemNotFound);
                        return;
                    }
                }
            }

            for (var i = 0; i < packet.Items.Count; ++i)
            {
                if (items[i].GetMaxStackCount() < finalCount)
                {
                    SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.DatabaseError);
                    return;
                }
            }

            Item item = items[0];

            uint auctionTime = (uint)(packet.RunTime * WorldConfig.GetFloatValue(WorldCfg.RateAuctionTime));
            AuctionHouseObject auctionHouse = Global.AuctionMgr.GetAuctionsMap(creature.getFaction());

            ulong deposit = Global.AuctionMgr.GetAuctionDeposit(auctionHouseEntry, packet.RunTime, item, finalCount);
            if (!GetPlayer().HasEnoughMoney(deposit))
            {
                SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.NotEnoughtMoney);
                return;
            }

            AuctionEntry AH = new AuctionEntry();
            SQLTransaction trans;

            if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionAuction))
                AH.auctioneer = 23442;     //@TODO - HARDCODED DB GUID, BAD BAD BAD
            else
                AH.auctioneer = creature.GetSpawnId();

            // Required stack size of auction matches to current item stack size, just move item to auctionhouse
            if (packet.Items.Count == 1 && item.GetCount() == packet.Items[0].UseCount)
            {
                if (HasPermission(RBACPermissions.LogGmTrade))
                {
                    Log.outCommand(GetAccountId(), "GM {0} (Account: {1}) create auction: {2} (Entry: {3} Count: {4})",
                        GetPlayerName(), GetAccountId(), item.GetTemplate().GetName(), item.GetEntry(), item.GetCount());
                }

                AH.Id = Global.ObjectMgr.GenerateAuctionID();
                AH.itemGUIDLow = item.GetGUID().GetCounter();
                AH.itemEntry = item.GetEntry();
                AH.itemCount = item.GetCount();
                AH.owner = GetPlayer().GetGUID().GetCounter();
                AH.startbid = (uint)packet.MinBid;
                AH.bidder = 0;
                AH.bid = 0;
                AH.buyout = (uint)packet.BuyoutPrice;
                AH.expire_time = Time.UnixTime + auctionTime;
                AH.deposit = deposit;
                AH.etime = packet.RunTime;
                AH.auctionHouseEntry = auctionHouseEntry;

                Log.outInfo(LogFilter.Network, "CMSG_AUCTION_SELL_ITEM: {0} {1} is selling item {2} {3} to auctioneer {4} with count {5} with initial bid {6} with buyout {7} and with time {8} (in sec) in auctionhouse {9}",
                    GetPlayer().GetGUID().ToString(), GetPlayer().GetName(), item.GetGUID().ToString(), item.GetTemplate().GetName(), AH.auctioneer, item.GetCount(), packet.MinBid, packet.BuyoutPrice, auctionTime, AH.GetHouseId());
                Global.AuctionMgr.AddAItem(item);
                auctionHouse.AddAuction(AH);

                GetPlayer().MoveItemFromInventory(item.GetBagSlot(), item.GetSlot(), true);

                trans = new SQLTransaction();
                item.DeleteFromInventoryDB(trans);
                item.SaveToDB(trans);
                AH.SaveToDB(trans);
                GetPlayer().SaveInventoryAndGoldToDB(trans);
                DB.Characters.CommitTransaction(trans);

                SendAuctionCommandResult(AH, AuctionAction.SellItem, AuctionError.Ok);

                GetPlayer().UpdateCriteria(CriteriaTypes.CreateAuction, 1);
            }
            else // Required stack size of auction does not match to current item stack size, clone item and set correct stack size
            {
                Item newItem = item.CloneItem(finalCount, GetPlayer());
                if (!newItem)
                {
                    Log.outError(LogFilter.Network, "CMSG_AuctionAction.SellItem: Could not create clone of item {0}", item.GetEntry());
                    SendAuctionCommandResult(null, AuctionAction.SellItem, AuctionError.DatabaseError);
                    return;
                }

                if (HasPermission(RBACPermissions.LogGmTrade))
                {
                    Log.outCommand(GetAccountId(), "GM {0} (Account: {1}) create auction: {2} (Entry: {3} Count: {4})",
                        GetPlayerName(), GetAccountId(), newItem.GetTemplate().GetName(), newItem.GetEntry(), newItem.GetCount());
                }

                AH.Id = Global.ObjectMgr.GenerateAuctionID();
                AH.itemGUIDLow = newItem.GetGUID().GetCounter();
                AH.itemEntry = newItem.GetEntry();
                AH.itemCount = newItem.GetCount();
                AH.owner = GetPlayer().GetGUID().GetCounter();
                AH.startbid = (uint)packet.MinBid;
                AH.bidder = 0;
                AH.bid = 0;
                AH.buyout = (uint)packet.BuyoutPrice;
                AH.expire_time = Time.UnixTime + auctionTime;
                AH.deposit = deposit;
                AH.etime = packet.RunTime;
                AH.auctionHouseEntry = auctionHouseEntry;

                Log.outInfo(LogFilter.Network, "CMSG_AuctionAction.SellItem: {0} {1} is selling {2} {3} to auctioneer {4} with count {5} with initial bid {6} with buyout {7} and with time {8} (in sec) in auctionhouse {9}",
                     GetPlayer().GetGUID().ToString(), GetPlayer().GetName(), newItem.GetGUID().ToString(), newItem.GetTemplate().GetName(), AH.auctioneer, newItem.GetCount(), packet.MinBid, packet.BuyoutPrice, auctionTime, AH.GetHouseId());
                Global.AuctionMgr.AddAItem(newItem);
                auctionHouse.AddAuction(AH);

                for (var i = 0; i < packet.Items.Count; ++i)
                {
                    Item item2 = items[i];

                    // Item stack count equals required count, ready to delete item - cloned item will be used for auction
                    if (item2.GetCount() == packet.Items[i].UseCount)
                    {
                        GetPlayer().MoveItemFromInventory(item2.GetBagSlot(), item2.GetSlot(), true);

                        trans = new SQLTransaction();
                        item2.DeleteFromInventoryDB(trans);
                        item2.DeleteFromDB(trans);
                        DB.Characters.CommitTransaction(trans);
                    }
                    else // Item stack count is bigger than required count, update item stack count and save to database - cloned item will be used for auction
                    {
                        item2.SetCount(item2.GetCount() - packet.Items[i].UseCount);
                        item2.SetState(ItemUpdateState.Changed, GetPlayer());
                        GetPlayer().ItemRemovedQuestCheck(item2.GetEntry(), packet.Items[i].UseCount);
                        item2.SendUpdateToPlayer(GetPlayer());

                        trans = new SQLTransaction();
                        item2.SaveToDB(trans);
                        DB.Characters.CommitTransaction(trans);
                    }
                }

                trans = new SQLTransaction();
                newItem.SaveToDB(trans);
                AH.SaveToDB(trans);
                GetPlayer().SaveInventoryAndGoldToDB(trans);
                DB.Characters.CommitTransaction(trans);

                SendAuctionCommandResult(AH, AuctionAction.SellItem, AuctionError.Ok);

                GetPlayer().UpdateCriteria(CriteriaTypes.CreateAuction, 1);
            }

            GetPlayer().ModifyMoney(-(long)deposit);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionPlaceBid)]
        void HandleAuctionPlaceBid(AuctionPlaceBid packet)
        {
            if (packet.AuctionItemID == 0 || packet.BidAmount == 0)
                return; // check for cheaters

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(packet.Auctioneer, NPCFlags.Auctioneer);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionPlaceBid - {0} not found or you can't interact with him.", packet.Auctioneer.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionMgr.GetAuctionsMap(creature.getFaction());

            AuctionEntry auction = auctionHouse.GetAuction(packet.AuctionItemID);
            Player player = GetPlayer();

            if (auction == null || auction.owner == player.GetGUID().GetCounter())
            {
                //you cannot bid your own auction:
                SendAuctionCommandResult(null, AuctionAction.PlaceBid, AuctionError.BidOwn);
                return;
            }

            // impossible have online own another character (use this for speedup check in case online owner)
            ObjectGuid ownerGuid = ObjectGuid.Create(HighGuid.Player, auction.owner);
            Player auction_owner = Global.ObjAccessor.FindPlayer(ownerGuid);
            if (!auction_owner && ObjectManager.GetPlayerAccountIdByGUID(ownerGuid) == player.GetSession().GetAccountId())
            {
                //you cannot bid your another character auction:
                SendAuctionCommandResult(null, AuctionAction.PlaceBid, AuctionError.BidOwn);
                return;
            }

            // cheating
            if (packet.BidAmount <= auction.bid || packet.BidAmount < auction.startbid)
                return;

            // price too low for next bid if not buyout
            if ((packet.BidAmount < auction.buyout || auction.buyout == 0) && packet.BidAmount < auction.bid + auction.GetAuctionOutBid())
            {
                // client already test it but just in case ...
                SendAuctionCommandResult(auction, AuctionAction.PlaceBid, AuctionError.HigherBid);
                return;
            }

            if (!player.HasEnoughMoney(packet.BidAmount))
            {
                // client already test it but just in case ...
                SendAuctionCommandResult(auction, AuctionAction.PlaceBid, AuctionError.NotEnoughtMoney);
                return;
            }

            SQLTransaction trans = new SQLTransaction();
            if (packet.BidAmount < auction.buyout || auction.buyout == 0)
            {
                if (auction.bidder > 0)
                {
                    if (auction.bidder == player.GetGUID().GetCounter())
                        player.ModifyMoney(-(long)(packet.BidAmount - auction.bid));
                    else
                    {
                        // mail to last bidder and return money
                        Global.AuctionMgr.SendAuctionOutbiddedMail(auction, packet.BidAmount, GetPlayer(), trans);
                        player.ModifyMoney(-(long)packet.BidAmount);
                    }
                }
                else
                    player.ModifyMoney(-(long)packet.BidAmount);

                auction.bidder = player.GetGUID().GetCounter();
                auction.bid = (uint)packet.BidAmount;
                GetPlayer().UpdateCriteria(CriteriaTypes.HighestAuctionBid, packet.BidAmount);

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_AUCTION_BID);
                stmt.AddValue(0, auction.bidder);
                stmt.AddValue(1, auction.bid);
                stmt.AddValue(2, auction.Id);
                trans.Append(stmt);

                SendAuctionCommandResult(auction, AuctionAction.PlaceBid, AuctionError.Ok);

                // Not sure if we must send this now.
                Player owner = Global.ObjAccessor.FindConnectedPlayer(ObjectGuid.Create(HighGuid.Player, auction.owner));
                Item item = Global.AuctionMgr.GetAItem(auction.itemGUIDLow);
                if (owner && item)
                    owner.GetSession().SendAuctionOwnerBidNotification(auction, item);
            }
            else
            {
                //buyout:
                if (player.GetGUID().GetCounter() == auction.bidder)
                    player.ModifyMoney(-(long)(auction.buyout - auction.bid));
                else
                {
                    player.ModifyMoney(-(long)auction.buyout);
                    if (auction.bidder != 0)                          //buyout for bidded auction ..
                        Global.AuctionMgr.SendAuctionOutbiddedMail(auction, auction.buyout, GetPlayer(), trans);
                }
                auction.bidder = player.GetGUID().GetCounter();
                auction.bid = auction.buyout;
                GetPlayer().UpdateCriteria(CriteriaTypes.HighestAuctionBid, auction.buyout);

                SendAuctionCommandResult(auction, AuctionAction.PlaceBid, AuctionError.Ok);

                //- Mails must be under transaction control too to prevent data loss
                Global.AuctionMgr.SendAuctionSalePendingMail(auction, trans);
                Global.AuctionMgr.SendAuctionSuccessfulMail(auction, trans);
                Global.AuctionMgr.SendAuctionWonMail(auction, trans);

                auction.DeleteFromDB(trans);

                Global.AuctionMgr.RemoveAItem(auction.itemGUIDLow);
                auctionHouse.RemoveAuction(auction);
            }

            player.SaveInventoryAndGoldToDB(trans);
            DB.Characters.CommitTransaction(trans);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionRemoveItem)]
        void HandleAuctionRemoveItem(AuctionRemoveItem packet)
        {
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(packet.Auctioneer, NPCFlags.Auctioneer);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionRemoveItem - {0} not found or you can't interact with him.", packet.Auctioneer.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionMgr.GetAuctionsMap(creature.getFaction());

            AuctionEntry auction = auctionHouse.GetAuction((uint)packet.AuctionItemID);
            Player player = GetPlayer();

            SQLTransaction trans = new SQLTransaction();
            if (auction != null && auction.owner == player.GetGUID().GetCounter())
            {
                Item pItem = Global.AuctionMgr.GetAItem(auction.itemGUIDLow);
                if (pItem)
                {
                    if (auction.bidder > 0)                        // If we have a bidder, we have to send him the money he paid
                    {
                        ulong auctionCut = auction.GetAuctionCut();
                        if (!player.HasEnoughMoney(auctionCut))          //player doesn't have enough money, maybe message needed
                            return;
                        Global.AuctionMgr.SendAuctionCancelledToBidderMail(auction, trans);
                        player.ModifyMoney(-(long)auctionCut);
                    }

                    // item will deleted or added to received mail list
                    new MailDraft(auction.BuildAuctionMailSubject(MailAuctionAnswers.Canceled), AuctionEntry.BuildAuctionMailBody(0, 0, auction.buyout, auction.deposit, 0))
                        .AddItem(pItem)
                        .SendMailTo(trans, new MailReceiver(player), new MailSender(auction), MailCheckMask.Copied);
                }
                else
                {
                    Log.outError(LogFilter.Network, "Auction id: {0} got non existing item (item guid : {1})!", auction.Id, auction.itemGUIDLow);
                    SendAuctionCommandResult(null, AuctionAction.Cancel, AuctionError.DatabaseError);
                    return;
                }
            }
            else
            {
                SendAuctionCommandResult(null, AuctionAction.Cancel, AuctionError.DatabaseError);
                //this code isn't possible ... maybe there should be assert
                Log.outError(LogFilter.Network, "CHEATER: {0} tried to cancel auction (id: {1}) of another player or auction is null", player.GetGUID().ToString(), packet.AuctionItemID);
                return;
            }

            //inform player, that auction is removed
            SendAuctionCommandResult(auction, AuctionAction.Cancel, AuctionError.Ok);

            // Now remove the auction
            player.SaveInventoryAndGoldToDB(trans);
            auction.DeleteFromDB(trans);
            DB.Characters.CommitTransaction(trans);

            Global.AuctionMgr.RemoveAItem(auction.itemGUIDLow);
            auctionHouse.RemoveAuction(auction);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListBidderItems)]
        void HandleAuctionListBidderItems(AuctionListBidderItems packet)
        {
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(packet.Auctioneer, NPCFlags.Auctioneer);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionListBidderItems - {0} not found or you can't interact with him.", packet.Auctioneer.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionMgr.GetAuctionsMap(creature.getFaction());

            AuctionListBidderItemsResult result = new AuctionListBidderItemsResult();

            Player player = GetPlayer();
            auctionHouse.BuildListBidderItems(result, player, ref result.TotalCount);
            result.DesiredDelay = 300;
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListOwnerItems)]
        void HandleAuctionListOwnerItems(AuctionListOwnerItems packet)
        {
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(packet.Auctioneer, NPCFlags.Auctioneer);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionListOwnerItems - {0} not found or you can't interact with him.", packet.Auctioneer.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionMgr.GetAuctionsMap(creature.getFaction());

            AuctionListOwnerItemsResult result = new AuctionListOwnerItemsResult();

            auctionHouse.BuildListOwnerItems(result, GetPlayer(), ref result.TotalCount);
            result.DesiredDelay = 300;
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListItems)]
        void HandleAuctionListItems(AuctionListItems packet)
        {
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(packet.Auctioneer, NPCFlags.Auctioneer);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleAuctionListItems - {0} not found or you can't interact with him.", packet.Auctioneer.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionMgr.GetAuctionsMap(creature.getFaction());


            Optional<AuctionSearchFilters> filters = new Optional<AuctionSearchFilters>();

            AuctionListItemsResult result = new AuctionListItemsResult();
            if (!packet.ClassFilters.Empty())
            {
                filters.HasValue = true;

                foreach (var classFilter in packet.ClassFilters)
                {
                    if (!classFilter.SubClassFilters.Empty())
                    {
                        foreach (var subClassFilter in classFilter.SubClassFilters)
                        {
                            filters.Value.Classes[classFilter.ItemClass].SubclassMask |= (AuctionSearchFilters.FilterType)(1 << subClassFilter.ItemSubclass);
                            filters.Value.Classes[classFilter.ItemClass].InvTypes[subClassFilter.ItemSubclass] = subClassFilter.InvTypeMask;
                        }
                    }
                    else
                        filters.Value.Classes[classFilter.ItemClass].SubclassMask = AuctionSearchFilters.FilterType.SkipSubclass;
                }
            }

            auctionHouse.BuildListAuctionItems(result, GetPlayer(), packet.Name.ToLower(), packet.Offset, packet.MinLevel, packet.MaxLevel, packet.OnlyUsable, filters, packet.Quality);

            result.DesiredDelay = WorldConfig.GetUIntValue(WorldCfg.AuctionSearchDelay);
            result.OnlyUsable = packet.OnlyUsable;
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListPendingSales)]
        void HandleAuctionListPendingSales(AuctionListPendingSales packet)
        {
            AuctionListPendingSalesResult result = new AuctionListPendingSalesResult();
            result.TotalNumRecords = 0;
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionReplicateItems)]
        void HandleReplicateItems(AuctionReplicateItems packet)
        {
           /* Creature* creature = GetPlayer()->GetNPCIfCanInteractWith(packet.Auctioneer, UNIT_NPC_FLAG_AUCTIONEER);
            if (!creature)
            {
                TC_LOG_DEBUG("network", "WORLD: HandleReplicateItems - {0} not found or you can't interact with him.", packet.Auctioneer.ToString().c_str());
                return;
            }

            // remove fake death
            if (GetPlayer()->HasUnitState(UNIT_STATE_DIED))
                GetPlayer()->RemoveAurasByType(SPELL_AURA_FEIGN_DEATH);

            AuctionHouseObject* auctionHouse = sAuctionMgr->GetAuctionsMap(creature->getFaction());

            WorldPackets::AuctionHouse::AuctionReplicateResponse response;

            auctionHouse->BuildReplicate(response, GetPlayer(), packet.ChangeNumberGlobal, packet.ChangeNumberCursor, packet.ChangeNumberTombstone, packet.Count);
            */
            //@todo implement this properly
            AuctionReplicateResponse response = new AuctionReplicateResponse();
            response.ChangeNumberCursor = packet.ChangeNumberCursor;
            response.ChangeNumberGlobal = packet.ChangeNumberGlobal;
            response.ChangeNumberTombstone = packet.ChangeNumberTombstone;
            response.DesiredDelay = WorldConfig.GetUIntValue(WorldCfg.AuctionSearchDelay) * 5;
            response.Result = 0;
            SendPacket(response);
        }
    }
}
