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
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AuctionBrowseQuery)]
        private void HandleAuctionBrowseQuery(AuctionBrowseQuery browseQuery)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, browseQuery.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(browseQuery.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {browseQuery.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            Log.outDebug(LogFilter.Auctionhouse, $"Auctionhouse search ({browseQuery.Auctioneer}), searchedname: {browseQuery.Name}, levelmin: {browseQuery.MinLevel}, levelmax: {browseQuery.MaxLevel}, filters: {browseQuery.Filters}");

            var classFilters = new Optional<AuctionSearchClassFilters>();

            var listBucketsResult = new AuctionListBucketsResult();
            if (!browseQuery.ItemClassFilters.Empty())
            {
                classFilters.HasValue = true;

                foreach (var classFilter in browseQuery.ItemClassFilters)
                {
                    if (!classFilter.SubClassFilters.Empty())
                    {
                        foreach (var subClassFilter in classFilter.SubClassFilters)
                        {
                            if (classFilter.ItemClass < (int)ItemClass.Max)
                            {
                                classFilters.Value.Classes[classFilter.ItemClass].SubclassMask |= (AuctionSearchClassFilters.FilterType)(1 << subClassFilter.ItemSubclass);
                                if (subClassFilter.ItemSubclass < ItemConst.MaxItemSubclassTotal)
                                    classFilters.Value.Classes[classFilter.ItemClass].InvTypes[subClassFilter.ItemSubclass] = subClassFilter.InvTypeMask;
                            }
                        }
                    }
                    else
                        classFilters.Value.Classes[classFilter.ItemClass].SubclassMask = AuctionSearchClassFilters.FilterType.SkipSubclass;
                }
            }

            auctionHouse.BuildListBuckets(listBucketsResult, _player, browseQuery.Name, browseQuery.MinLevel, browseQuery.MaxLevel, browseQuery.Filters, classFilters,
                browseQuery.KnownPets, browseQuery.KnownPets.Count, (byte)browseQuery.MaxPetLevel, browseQuery.Offset, browseQuery.Sorts, browseQuery.Sorts.Count);

            listBucketsResult.BrowseMode = AuctionHouseBrowseMode.Search;
            listBucketsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            SendPacket(listBucketsResult);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionCancelCommoditiesPurchase)]
        private void HandleAuctionCancelCommoditiesPurchase(AuctionCancelCommoditiesPurchase cancelCommoditiesPurchase)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, cancelCommoditiesPurchase.TaintedBy.HasValue, AuctionCommand.PlaceBid);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(cancelCommoditiesPurchase.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {cancelCommoditiesPurchase.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());
            auctionHouse.CancelCommodityQuote(_player.GetGUID());
        }

        [WorldPacketHandler(ClientOpcodes.AuctionConfirmCommoditiesPurchase)]
        private void HandleAuctionConfirmCommoditiesPurchase(AuctionConfirmCommoditiesPurchase confirmCommoditiesPurchase)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, confirmCommoditiesPurchase.TaintedBy.HasValue, AuctionCommand.PlaceBid);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(confirmCommoditiesPurchase.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {confirmCommoditiesPurchase.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var trans = new SQLTransaction();
            if (auctionHouse.BuyCommodity(trans, _player, (uint)confirmCommoditiesPurchase.ItemID, confirmCommoditiesPurchase.Quantity, throttle.DelayUntilNext))
            {
                AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans)).AfterComplete(success =>
                {
                    if (GetPlayer() && GetPlayer().GetGUID() == _player.GetGUID())
                    {
                        if (success)
                        {
                            GetPlayer().UpdateCriteria(CriteriaTypes.WonAuctions, 1);
                            SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.Ok, throttle.DelayUntilNext);
                        }
                        else
                            SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, throttle.DelayUntilNext);
                    }
                });
            }
        }

        [WorldPacketHandler(ClientOpcodes.AuctionHelloRequest)]
        private void HandleAuctionHello(AuctionHelloRequest hello)
        {
            var unit = GetPlayer().GetNPCIfCanInteractWith(hello.Guid, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!unit)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionHelloOpcode - {hello.Guid} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendAuctionHello(hello.Guid, unit);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListBiddedItems)]
        private void HandleAuctionListBiddedItems(AuctionListBiddedItems listBiddedItems)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listBiddedItems.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(listBiddedItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionListBidderItems - {listBiddedItems.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var result = new AuctionListBiddedItemsResult();

            var player = GetPlayer();
            auctionHouse.BuildListBiddedItems(result, player, listBiddedItems.Offset, listBiddedItems.Sorts, listBiddedItems.Sorts.Count);
            result.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListBucketsByBucketKeys)]
        private void HandleAuctionListBucketsByBucketKeys(AuctionListBucketsByBucketKeys listBucketsByBucketKeys)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listBucketsByBucketKeys.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(listBucketsByBucketKeys.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {listBucketsByBucketKeys.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var listBucketsResult = new AuctionListBucketsResult();

            auctionHouse.BuildListBuckets(listBucketsResult, _player,
                listBucketsByBucketKeys.BucketKeys, listBucketsByBucketKeys.BucketKeys.Count,
                listBucketsByBucketKeys.Sorts, listBucketsByBucketKeys.Sorts.Count);

            listBucketsResult.BrowseMode = AuctionHouseBrowseMode.SpecificKeys;
            listBucketsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            SendPacket(listBucketsResult);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListItemsByBucketKey)]
        private void HandleAuctionListItemsByBucketKey(AuctionListItemsByBucketKey listItemsByBucketKey)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listItemsByBucketKey.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(listItemsByBucketKey.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItemsByBucketKey - {listItemsByBucketKey.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var listItemsResult = new AuctionListItemsResult();
            listItemsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            listItemsResult.BucketKey = listItemsByBucketKey.BucketKey;
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(listItemsByBucketKey.BucketKey.ItemID);
            listItemsResult.ListType = itemTemplate != null && itemTemplate.GetMaxStackSize() > 1 ? AuctionHouseListType.Commodities : AuctionHouseListType.Items;

            auctionHouse.BuildListAuctionItems(listItemsResult, _player, new AuctionsBucketKey(listItemsByBucketKey.BucketKey), listItemsByBucketKey.Offset,
                listItemsByBucketKey.Sorts, listItemsByBucketKey.Sorts.Count);

            SendPacket(listItemsResult);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListItemsByItemId)]
        private void HandleAuctionListItemsByItemID(AuctionListItemsByItemID listItemsByItemID)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listItemsByItemID.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(listItemsByItemID.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItemsByItemID - {listItemsByItemID.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var listItemsResult = new AuctionListItemsResult();
            listItemsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            listItemsResult.BucketKey.ItemID = listItemsByItemID.ItemID;
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(listItemsByItemID.ItemID);
            listItemsResult.ListType = itemTemplate != null && itemTemplate.GetMaxStackSize() > 1 ? AuctionHouseListType.Commodities : AuctionHouseListType.Items;

            auctionHouse.BuildListAuctionItems(listItemsResult, _player, listItemsByItemID.ItemID, listItemsByItemID.Offset,
                listItemsByItemID.Sorts, listItemsByItemID.Sorts.Count);

            SendPacket(listItemsResult);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListOwnedItems)]
        private void HandleAuctionListOwnedItems(AuctionListOwnedItems listOwnedItems)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listOwnedItems.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(listOwnedItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionListOwnerItems - {listOwnedItems.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var result = new AuctionListOwnedItemsResult();

            auctionHouse.BuildListOwnedItems(result, _player, listOwnedItems.Offset, listOwnedItems.Sorts, listOwnedItems.Sorts.Count);
            result.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionPlaceBid)]
        private void HandleAuctionPlaceBid(AuctionPlaceBid placeBid)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, placeBid.TaintedBy.HasValue, AuctionCommand.PlaceBid);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(placeBid.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionPlaceBid - {placeBid.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // auction house does not deal with copper
            if ((placeBid.BidAmount % MoneyConstants.Silver) != 0)
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.BidIncrement, throttle.DelayUntilNext);
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var auction = auctionHouse.GetAuction(placeBid.AuctionID);
            if (auction == null || auction.IsCommodity())
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                return;
            }

            var player = GetPlayer();

            // check auction owner - cannot buy own auctions
            if (auction.Owner == player.GetGUID() || auction.OwnerAccount == GetAccountGUID())
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.BidOwn, throttle.DelayUntilNext);
                return;
            }

            var canBid = auction.MinBid != 0;
            var canBuyout = auction.BuyoutOrUnitPrice != 0;

            // buyout attempt with wrong amount
            if (!canBid && placeBid.BidAmount != auction.BuyoutOrUnitPrice)
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.BidIncrement, throttle.DelayUntilNext);
                return;
            }

            var minBid = auction.BidAmount != 0 ? auction.BidAmount + auction.CalculateMinIncrement() : auction.MinBid;
            if (canBid && placeBid.BidAmount < minBid)
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.HigherBid, throttle.DelayUntilNext);
                return;
            }

            var trans = new SQLTransaction();
            var priceToPay = placeBid.BidAmount;
            if (!auction.Bidder.IsEmpty())
            {
                // return money to previous bidder
                if (auction.Bidder != player.GetGUID())
                    auctionHouse.SendAuctionOutbid(auction, player.GetGUID(), placeBid.BidAmount, trans);
                else
                    priceToPay = placeBid.BidAmount - auction.BidAmount;
            }

            // check money
            if (!player.HasEnoughMoney(priceToPay))
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                return;
            }

            player.ModifyMoney(-(long)priceToPay);
            auction.Bidder = player.GetGUID();
            auction.BidAmount = placeBid.BidAmount;

            if (canBuyout && placeBid.BidAmount == auction.BuyoutOrUnitPrice)
            {
                // buyout
                auctionHouse.SendAuctionWon(auction, player, trans);
                auctionHouse.SendAuctionSold(auction, null, trans);

                auctionHouse.RemoveAuction(trans, auction);
            }
            else
            {
                // place bid
                var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_AUCTION_BID);
                stmt.AddValue(0, auction.Bidder.GetCounter());
                stmt.AddValue(1, auction.BidAmount);
                stmt.AddValue(2, auction.Id);
                trans.Append(stmt);

                auction.BidderHistory.Add(player.GetGUID());
                if (auction.BidderHistory.Contains(player.GetGUID()))
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AUCTION_BIDDER);
                    stmt.AddValue(0, auction.Id);
                    stmt.AddValue(1, player.GetGUID().GetCounter());
                    trans.Append(stmt);
                }

                // Not sure if we must send this now.
                var owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);
                if (owner != null)
                    owner.GetSession().SendAuctionOwnerBidNotification(auction);
            }

            player.SaveInventoryAndGoldToDB(trans);
            AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans)).AfterComplete(success =>
            {
                if (GetPlayer() && GetPlayer().GetGUID() == _player.GetGUID())
                {
                    if (success)
                    {
                        GetPlayer().UpdateCriteria(CriteriaTypes.HighestAuctionBid, placeBid.BidAmount);
                        SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.Ok, throttle.DelayUntilNext);
                    }
                    else
                        SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                }
            });
        }

        [WorldPacketHandler(ClientOpcodes.AuctionRemoveItem)]
        private void HandleAuctionRemoveItem(AuctionRemoveItem removeItem)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, removeItem.TaintedBy.HasValue, AuctionCommand.Cancel);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(removeItem.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionRemoveItem - {removeItem.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var auction = auctionHouse.GetAuction(removeItem.AuctionID);
            var player = GetPlayer();

            var trans = new SQLTransaction();
            if (auction != null && auction.Owner == player.GetGUID())
            {
                if (auction.Bidder.IsEmpty())                   // If we have a bidder, we have to send him the money he paid
                {
                    var cancelCost = MathFunctions.CalculatePct(auction.BidAmount, 5u);
                    if (!player.HasEnoughMoney(cancelCost))          //player doesn't have enough money
                    {
                        SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                        return;
                    }
                    auctionHouse.SendAuctionCancelledToBidder(auction, trans);
                    player.ModifyMoney(-(long)cancelCost);
                }

                auctionHouse.SendAuctionRemoved(auction, player, trans);
            }
            else
            {
                SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                //this code isn't possible ... maybe there should be assert
                Log.outError(LogFilter.Network, $"CHEATER: {player.GetGUID()} tried to cancel auction (id: {removeItem.AuctionID}) of another player or auction is null");
                return;
            }

            // client bug - instead of removing auction in the UI, it only substracts 1 from visible count
            var auctionIdForClient = auction.IsCommodity() ? 0 : auction.Id;

            // Now remove the auction
            player.SaveInventoryAndGoldToDB(trans);
            auctionHouse.RemoveAuction(trans, auction);
            AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans)).AfterComplete(success =>
            {
                if (GetPlayer() && GetPlayer().GetGUID() == _player.GetGUID())
                {
                    if (success)
                        SendAuctionCommandResult(auctionIdForClient, AuctionCommand.Cancel, AuctionResult.Ok, throttle.DelayUntilNext);        //inform player, that auction is removed
                    else
                        SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                }
            });
        }

        [WorldPacketHandler(ClientOpcodes.AuctionReplicateItems)]
        private void HandleReplicateItems(AuctionReplicateItems replicateItems)
        {
            var creature = GetPlayer().GetNPCIfCanInteractWith(replicateItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleReplicateItems - {replicateItems.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var response = new AuctionReplicateResponse();

            auctionHouse.BuildReplicate(response, GetPlayer(), replicateItems.ChangeNumberGlobal, replicateItems.ChangeNumberCursor, replicateItems.ChangeNumberTombstone, replicateItems.Count);

            response.DesiredDelay = WorldConfig.GetUIntValue(WorldCfg.AuctionSearchDelay) * 5;
            response.Result = 0;

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionSellCommodity)]
        private void HandleAuctionSellCommodity(AuctionSellCommodity sellCommodity)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, sellCommodity.TaintedBy.HasValue, AuctionCommand.SellItem);
            if (throttle.Throttled)
                return;

            if (sellCommodity.UnitPrice == 0 || sellCommodity.UnitPrice > PlayerConst.MaxMoneyAmount)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionSellItem - Player {_player.GetName()} ({_player.GetGUID()}) attempted to sell item with invalid price.");
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                return;
            }

            // auction house does not deal with copper
            if ((sellCommodity.UnitPrice % MoneyConstants.Silver) != 0)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                return;
            }

            var creature = GetPlayer().GetNPCIfCanInteractWith(sellCommodity.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {sellCommodity.Auctioneer} not found or you can't interact with him.");
                return;
            }

            uint houseId = 0;
            var auctionHouseEntry = Global.AuctionHouseMgr.GetAuctionHouseEntry(creature.GetFaction(), ref houseId);
            if (auctionHouseEntry == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionSellItem - Unit ({sellCommodity.Auctioneer}) has wrong faction.");
                return;
            }

            switch (sellCommodity.RunTime)
            {
                case 1 * SharedConst.MinAuctionTime / Time.Minute:
                case 2 * SharedConst.MinAuctionTime / Time.Minute:
                case 4 * SharedConst.MinAuctionTime / Time.Minute:
                    break;
                default:
                    SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.AuctionHouseBusy, throttle.DelayUntilNext);
                    return;
            }

            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            // find all items for sale
            ulong totalCount = 0;
            var items2 = new Dictionary<ObjectGuid, (Item Item, ulong UseCount)>();

            foreach (var itemForSale in sellCommodity.Items)
    {
                Item item = _player.GetItemByGuid(itemForSale.Guid);
                if (!item)
                {
                    SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                    return;
                }

                if (item.GetTemplate().GetMaxStackSize() == 1)
                {
                    // not commodity, must use different packet
                    SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                    return;
                }

                // verify that all items belong to the same bucket
                if (!items2.Empty() && AuctionsBucketKey.ForItem(item) != AuctionsBucketKey.ForItem(items2.FirstOrDefault().Value.Item1))
                {
                    SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                    return;
                }

                if (Global.AuctionHouseMgr.GetAItem(item.GetGUID()) || !item.CanBeTraded() || item.IsNotEmptyBag() ||
                    item.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.Conjured) || item.m_itemData.Expiration != 0 ||
                    item.GetCount() < itemForSale.UseCount)
                {
                    SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                    return;
                }

                var soldItem = items2.LookupByKey(item.GetGUID());
                soldItem.Item = item;
                soldItem.UseCount += itemForSale.UseCount;
                items2[item.GetGUID()] = soldItem;
                if (item.GetCount() < soldItem.UseCount)
                {
                    // check that we have enough of this item to sell
                    SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                    return;
                }

                totalCount += itemForSale.UseCount;
            }

            if (totalCount == 0)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                return;
            }

            var auctionTime = TimeSpan.FromSeconds((long)TimeSpan.FromMinutes(sellCommodity.RunTime).TotalSeconds * WorldConfig.GetFloatValue(WorldCfg.RateAuctionTime));
            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var deposit = Global.AuctionHouseMgr.GetCommodityAuctionDeposit(items2.FirstOrDefault().Value.Item.GetTemplate(), TimeSpan.FromMinutes(sellCommodity.RunTime), (uint)totalCount);
            if (!_player.HasEnoughMoney(deposit))
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                return;
            }

            uint auctionId = Global.ObjectMgr.GenerateAuctionID();
            var auction = new AuctionPosting();
            auction.Id = auctionId;
            auction.Owner = _player.GetGUID();
            auction.OwnerAccount = GetAccountGUID();
            auction.BuyoutOrUnitPrice = sellCommodity.UnitPrice;
            auction.Deposit = deposit;
            auction.StartTime = GameTime.GetGameTimeSystemPoint();
            auction.EndTime = auction.StartTime + auctionTime;

            // keep track of what was cloned to undo/modify counts later
            var clones = new Dictionary<Item, Item>();
            foreach (var pair in items2)
            {
                Item itemForSale;
                if (pair.Value.Item1.GetCount() != pair.Value.Item2)
                {
                    itemForSale = pair.Value.Item1.CloneItem((uint)pair.Value.Item2, _player);
                    if (itemForSale == null)
                    {
                        Log.outError(LogFilter.Network, $"CMSG_AUCTION_SELL_COMMODITY: Could not create clone of item {pair.Value.Item1.GetEntry()}");
                        SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                        return;
                    }

                    clones.Add(pair.Value.Item1, itemForSale);
                }
            }

            if (!Global.AuctionHouseMgr.PendingAuctionAdd(_player, auctionHouse.GetAuctionHouseId(), auction.Id, auction.Deposit))
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                return;
            }

            /*TC_LOG_INFO("network", "CMSG_AUCTION_SELL_COMMODITY: %s %s is selling item %s %s to auctioneer %s with count " UI64FMTD " with with unit price " UI64FMTD " and with time %u (in sec) in auctionhouse %u",
                _player.GetGUID().ToString(), _player.GetName(), items2.begin().second.first.GetNameForLocaleIdx(sWorld.GetDefaultDbcLocale()),
                ([&items2]()
        {
                std.stringstream ss;
                auto itr = items2.begin();
                ss << (itr++).first.ToString();
                for (; itr != items2.end(); ++itr)
                    ss << ',' << itr.first.ToString();
                return ss.str();
            } ()),
        creature.GetGUID().ToString(), totalCount, sellCommodity.UnitPrice, uint32(auctionTime.count()), auctionHouse.GetAuctionHouseId());*/

            if (HasPermission(RBACPermissions.LogGmTrade))
            {
                var logItem = items2.First().Value.Item1;
                Log.outCommand(GetAccountId(), $"GM {GetPlayerName()} (Account: {GetAccountId()}) create auction: {logItem.GetName(Global.WorldMgr.GetDefaultDbcLocale())} (Entry: {logItem.GetEntry()} Count: {totalCount})");
            }

            var trans = new SQLTransaction();

            foreach (var pair in items2)
            {
                var itemForSale = pair.Value.Item1;
                var cloneItr = clones.LookupByKey(pair.Value.Item1);
                if (cloneItr != null)
                {
                    var original = itemForSale;
                    original.SetCount(original.GetCount() - (uint)pair.Value.Item2);
                    original.SetState(ItemUpdateState.Changed, _player);
                    _player.ItemRemovedQuestCheck(original.GetEntry(), (uint)pair.Value.Item2);
                    original.SaveToDB(trans);

                    itemForSale = cloneItr;
                }
                else
                {
                    _player.MoveItemFromInventory(itemForSale.GetBagSlot(), itemForSale.GetSlot(), true);
                    itemForSale.DeleteFromInventoryDB(trans);
                }

                itemForSale.SaveToDB(trans);
                auction.Items.Add(itemForSale);
            }

            auctionHouse.AddAuction(trans, auction);
            _player.SaveInventoryAndGoldToDB(trans);

            AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans)).AfterComplete(success =>
            {
                if (GetPlayer() && GetPlayer().GetGUID() == _player.GetGUID())
                {
                    if (success)
                    {
                        GetPlayer().UpdateCriteria(CriteriaTypes.CreateAuction, 1);
                        SendAuctionCommandResult(auctionId, AuctionCommand.SellItem, AuctionResult.Ok, throttle.DelayUntilNext);
                    }
                    else
                        SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                }
            });
        }

        [WorldPacketHandler(ClientOpcodes.AuctionSellItem)]
        private void HandleAuctionSellItem(AuctionSellItem sellItem)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, sellItem.TaintedBy.HasValue, AuctionCommand.SellItem);
            if (throttle.Throttled)
                return;

            if (sellItem.Items.Count != 1 || sellItem.Items[0].UseCount != 1)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                return;
            }

            if (sellItem.MinBid == 0 && sellItem.BuyoutPrice == 0)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                return;
            }

            if (sellItem.MinBid > PlayerConst.MaxMoneyAmount || sellItem.BuyoutPrice > PlayerConst.MaxMoneyAmount)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionSellItem - Player {_player.GetName()} ({_player.GetGUID()}) attempted to sell item with higher price than max gold amount.");
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.Inventory, throttle.DelayUntilNext, InventoryResult.TooMuchGold);
                return;
            }

            // auction house does not deal with copper
            if ((sellItem.MinBid % MoneyConstants.Silver) != 0 || (sellItem.BuyoutPrice % MoneyConstants.Silver) != 0)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                return;
            }

            var creature = GetPlayer().GetNPCIfCanInteractWith(sellItem.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outError(LogFilter.Network, "WORLD: HandleAuctionSellItem - Unit (%s) not found or you can't interact with him.", sellItem.Auctioneer.ToString());
                return;
            }

            uint houseId = 0;
            var auctionHouseEntry = Global.AuctionHouseMgr.GetAuctionHouseEntry(creature.GetFaction(), ref houseId);
            if (auctionHouseEntry == null)
            {
                Log.outError(LogFilter.Network, "WORLD: HandleAuctionSellItem - Unit (%s) has wrong faction.", sellItem.Auctioneer.ToString());
                return;
            }

            switch (sellItem.RunTime)
            {
                case 1 * SharedConst.MinAuctionTime / Time.Minute:
                case 2 * SharedConst.MinAuctionTime / Time.Minute:
                case 4 * SharedConst.MinAuctionTime / Time.Minute:
                    break;
                default:
                    SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.AuctionHouseBusy, throttle.DelayUntilNext);
                    return;
            }

            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            Item item = _player.GetItemByGuid(sellItem.Items[0].Guid);
            if (!item)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                return;
            }

            if (item.GetTemplate().GetMaxStackSize() > 1)
            {
                // commodity, must use different packet
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                return;
            }

            if (Global.AuctionHouseMgr.GetAItem(item.GetGUID()) || !item.CanBeTraded() || item.IsNotEmptyBag() ||
                item.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.Conjured) || item.m_itemData.Expiration != 0 ||
                item.GetCount() != 1)
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                return;
            }

            var auctionTime = TimeSpan.FromSeconds((long)(TimeSpan.FromMinutes(sellItem.RunTime).TotalSeconds * WorldConfig.GetFloatValue(WorldCfg.RateAuctionTime)));
            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var deposit = Global.AuctionHouseMgr.GetItemAuctionDeposit(_player, item, TimeSpan.FromMinutes(sellItem.RunTime));
            if (!_player.HasEnoughMoney(deposit))
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                return;
            }

            uint auctionId = Global.ObjectMgr.GenerateAuctionID();

            var auction = new AuctionPosting();
            auction.Id = auctionId;
            auction.Owner = _player.GetGUID();
            auction.OwnerAccount = GetAccountGUID();
            auction.MinBid = sellItem.MinBid;
            auction.BuyoutOrUnitPrice = sellItem.BuyoutPrice;
            auction.Deposit = deposit;
            auction.BidAmount = sellItem.MinBid;
            auction.StartTime = GameTime.GetGameTimeSystemPoint();
            auction.EndTime = auction.StartTime + auctionTime;

            if (HasPermission(RBACPermissions.LogGmTrade))
                Log.outCommand(GetAccountId(), $"GM {GetPlayerName()} (Account: {GetAccountId()}) create auction: {item.GetTemplate().GetName()} (Entry: {item.GetEntry()} Count: {item.GetCount()})");

            auction.Items.Add(item);

            Log.outInfo(LogFilter.Network, $"CMSG_AuctionAction.SellItem: {_player.GetGUID()} {_player.GetName()} is selling item {item.GetGUID()} {item.GetTemplate().GetName()} " +
                $"to auctioneer {creature.GetGUID()} with count {item.GetCount()} with initial bid {sellItem.MinBid} with buyout {sellItem.BuyoutPrice} and with time {auctionTime.TotalSeconds} " +
                $"(in sec) in auctionhouse {auctionHouse.GetAuctionHouseId()}");

            // Add to pending auctions, or fail with insufficient funds error
            if (!Global.AuctionHouseMgr.PendingAuctionAdd(_player, auctionHouse.GetAuctionHouseId(), auctionId, auction.Deposit))
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                return;
            }

            _player.MoveItemFromInventory(item.GetBagSlot(), item.GetSlot(), true);

            var trans = new SQLTransaction();
            item.DeleteFromInventoryDB(trans);
            item.SaveToDB(trans);

            auctionHouse.AddAuction(trans, auction);
            _player.SaveInventoryAndGoldToDB(trans);
            AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans)).AfterComplete(success =>
    {
                if (GetPlayer() && GetPlayer().GetGUID() == _player.GetGUID())
                {
                    if (success)
                    {
                        GetPlayer().UpdateCriteria(CriteriaTypes.CreateAuction, 1);
                        SendAuctionCommandResult(auctionId, AuctionCommand.SellItem, AuctionResult.Ok, throttle.DelayUntilNext);
                    }
                    else
                        SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
                }
            });
        }

        [WorldPacketHandler(ClientOpcodes.AuctionSetFavoriteItem)]
        private void HandleAuctionSetFavoriteItem(AuctionSetFavoriteItem setFavoriteItem)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, false);
            if (throttle.Throttled)
                return;

            var trans = new SQLTransaction();

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTION);
            stmt.AddValue(0, _player.GetGUID().GetCounter());
            stmt.AddValue(1, setFavoriteItem.Item.Order);
            trans.Append(stmt);

            if (!setFavoriteItem.IsNotFavorite)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_FAVORITE_AUCTION);
                stmt.AddValue(0, _player.GetGUID().GetCounter());
                stmt.AddValue(1, setFavoriteItem.Item.Order);
                stmt.AddValue(2, setFavoriteItem.Item.ItemID);
                stmt.AddValue(3, setFavoriteItem.Item.ItemLevel);
                stmt.AddValue(4, setFavoriteItem.Item.BattlePetSpeciesID);
                stmt.AddValue(5, setFavoriteItem.Item.SuffixItemNameDescriptionID);
                trans.Append(stmt);
            }

            DB.Characters.CommitTransaction(trans);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionGetCommodityQuote)]
        private void HandleAuctionGetCommodityQuote(AuctionGetCommodityQuote getCommodityQuote)
        {
            var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, getCommodityQuote.TaintedBy.HasValue, AuctionCommand.PlaceBid);
            if (throttle.Throttled)
                return;

            var creature = GetPlayer().GetNPCIfCanInteractWith(getCommodityQuote.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionStartCommoditiesPurchase - {getCommodityQuote.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            var commodityQuoteResult = new AuctionGetCommodityQuoteResult();

            var quote = auctionHouse.CreateCommodityQuote(_player, (uint)getCommodityQuote.ItemID, getCommodityQuote.Quantity);
            if (quote != null)
            {
                commodityQuoteResult.TotalPrice.Set(quote.TotalPrice);
                commodityQuoteResult.Quantity.Set(quote.Quantity);
                commodityQuoteResult.QuoteDuration.Set((int)(quote.ValidTo - GameTime.GetGameTimeSteadyPoint()).TotalMilliseconds);
            }

            commodityQuoteResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;

            SendPacket(commodityQuoteResult);
        }

        public void SendAuctionHello(ObjectGuid guid, Creature unit)
        {
            if (GetPlayer().GetLevel() < WorldConfig.GetIntValue(WorldCfg.AuctionLevelReq))
            {
                SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.AuctionReq), WorldConfig.GetIntValue(WorldCfg.AuctionLevelReq));
                return;
            }

            var ahEntry = Global.AuctionHouseMgr.GetAuctionHouseEntry(unit.GetFaction());
            if (ahEntry == null)
                return;

            var auctionHelloResponse = new AuctionHelloResponse();
            auctionHelloResponse.Guid = guid;
            auctionHelloResponse.OpenForBusiness = true;
            SendPacket(auctionHelloResponse);
        }

        public void SendAuctionCommandResult(uint auctionId, AuctionCommand command, AuctionResult errorCode, TimeSpan delayForNextAction, InventoryResult bagError = 0)
        {
            var auctionCommandResult = new AuctionCommandResult();
            auctionCommandResult.AuctionID = auctionId;
            auctionCommandResult.Command = (int)command;
            auctionCommandResult.ErrorCode = (int)errorCode;
            auctionCommandResult.BagResult = (int)bagError;
            auctionCommandResult.DesiredDelay = (uint)delayForNextAction.TotalSeconds;
            SendPacket(auctionCommandResult);
        }

        public void SendAuctionClosedNotification(AuctionPosting auction, float mailDelay, bool sold)
        {
            var packet = new AuctionClosedNotification();
            packet.Info.Initialize(auction);
            packet.ProceedsMailDelay = mailDelay;
            packet.Sold = sold;
            SendPacket(packet);
        }

        public void SendAuctionOwnerBidNotification(AuctionPosting auction)
        {
            var packet = new AuctionOwnerBidNotification();
            packet.Info.Initialize(auction);
            packet.Bidder = auction.Bidder;
            packet.MinIncrement = auction.CalculateMinIncrement();
            SendPacket(packet);
        }
    }
}
