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
using Game.Mails;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AuctionBrowseQuery)]
        void HandleAuctionBrowseQuery(AuctionBrowseQuery browseQuery)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, browseQuery.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(browseQuery.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {browseQuery.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            Log.outDebug(LogFilter.Auctionhouse, $"Auctionhouse search ({browseQuery.Auctioneer}), searchedname: {browseQuery.Name}, levelmin: {browseQuery.MinLevel}, levelmax: {browseQuery.MaxLevel}, filters: {browseQuery.Filters}");

            Optional<AuctionSearchClassFilters> classFilters = new Optional<AuctionSearchClassFilters>();

            AuctionListBucketsResult listBucketsResult = new AuctionListBucketsResult();
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
        void HandleAuctionCancelCommoditiesPurchase(AuctionCancelCommoditiesPurchase cancelCommoditiesPurchase)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, cancelCommoditiesPurchase.TaintedBy.HasValue, AuctionCommand.PlaceBid);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(cancelCommoditiesPurchase.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {cancelCommoditiesPurchase.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());
            auctionHouse.CancelCommodityQuote(_player.GetGUID());
        }

        [WorldPacketHandler(ClientOpcodes.AuctionConfirmCommoditiesPurchase)]
        void HandleAuctionConfirmCommoditiesPurchase(AuctionConfirmCommoditiesPurchase confirmCommoditiesPurchase)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, confirmCommoditiesPurchase.TaintedBy.HasValue, AuctionCommand.PlaceBid);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(confirmCommoditiesPurchase.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {confirmCommoditiesPurchase.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            SQLTransaction trans = new SQLTransaction();
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
        void HandleAuctionHello(AuctionHelloRequest hello)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(hello.Guid, NPCFlags.Auctioneer, NPCFlags2.None);
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

        [WorldPacketHandler(ClientOpcodes.AuctionListBidderItems)]
        void HandleAuctionListBidderItems(AuctionListBidderItems listBidderItems)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listBidderItems.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(listBidderItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionListBidderItems - {listBidderItems.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionListBidderItemsResult result = new AuctionListBidderItemsResult();

            Player player = GetPlayer();
            auctionHouse.BuildListBidderItems(result, player, listBidderItems.Offset, listBidderItems.Sorts, listBidderItems.Sorts.Count);
            result.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListBucketsByBucketKeys)]
        void HandleAuctionListBucketsByBucketKeys(AuctionListBucketsByBucketKeys listBucketsByBucketKeys)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listBucketsByBucketKeys.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(listBucketsByBucketKeys.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {listBucketsByBucketKeys.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionListBucketsResult listBucketsResult = new AuctionListBucketsResult();

            auctionHouse.BuildListBuckets(listBucketsResult, _player,
                listBucketsByBucketKeys.BucketKeys, listBucketsByBucketKeys.BucketKeys.Count,
                listBucketsByBucketKeys.Sorts, listBucketsByBucketKeys.Sorts.Count);

            listBucketsResult.BrowseMode = AuctionHouseBrowseMode.SpecificKeys;
            listBucketsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            SendPacket(listBucketsResult);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListItemsByBucketKey)]
        void HandleAuctionListItemsByBucketKey(AuctionListItemsByBucketKey listItemsByBucketKey)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listItemsByBucketKey.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(listItemsByBucketKey.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItemsByBucketKey - {listItemsByBucketKey.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionListItemsResult listItemsResult = new AuctionListItemsResult();
            listItemsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            listItemsResult.BucketKey = listItemsByBucketKey.BucketKey;
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(listItemsByBucketKey.BucketKey.ItemID);
            listItemsResult.ListType = itemTemplate != null && itemTemplate.GetMaxStackSize() > 1 ? AuctionHouseListType.Commodities : AuctionHouseListType.Items;

            auctionHouse.BuildListAuctionItems(listItemsResult, _player, new AuctionsBucketKey(listItemsByBucketKey.BucketKey), listItemsByBucketKey.Offset,
                listItemsByBucketKey.Sorts, listItemsByBucketKey.Sorts.Count);

            SendPacket(listItemsResult);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListItemsByItemId)]
        void HandleAuctionListItemsByItemID(AuctionListItemsByItemID listItemsByItemID)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listItemsByItemID.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(listItemsByItemID.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItemsByItemID - {listItemsByItemID.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionListItemsResult listItemsResult = new AuctionListItemsResult();
            listItemsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            listItemsResult.BucketKey.ItemID = listItemsByItemID.ItemID;
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(listItemsByItemID.ItemID);
            listItemsResult.ListType = itemTemplate != null && itemTemplate.GetMaxStackSize() > 1 ? AuctionHouseListType.Commodities : AuctionHouseListType.Items;

            auctionHouse.BuildListAuctionItems(listItemsResult, _player, listItemsByItemID.ItemID, listItemsByItemID.Offset,
                listItemsByItemID.Sorts, listItemsByItemID.Sorts.Count);

            SendPacket(listItemsResult);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionListOwnerItems)]
        void HandleAuctionListOwnerItems(AuctionListOwnerItems listOwnerItems)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listOwnerItems.TaintedBy.HasValue);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(listOwnerItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionListOwnerItems - {listOwnerItems.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionListOwnerItemsResult result = new AuctionListOwnerItemsResult();

            auctionHouse.BuildListOwnerItems(result, _player, listOwnerItems.Offset, listOwnerItems.Sorts, listOwnerItems.Sorts.Count);
            result.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionPlaceBid)]
        void HandleAuctionPlaceBid(AuctionPlaceBid placeBid)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, placeBid.TaintedBy.HasValue, AuctionCommand.PlaceBid);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(placeBid.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
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

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionPosting auction = auctionHouse.GetAuction(placeBid.AuctionID);
            if (auction == null || auction.IsCommodity())
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.ItemNotFound, throttle.DelayUntilNext);
                return;
            }

            Player player = GetPlayer();

            // check auction owner - cannot buy own auctions
            if (auction.Owner == player.GetGUID() || auction.OwnerAccount == GetAccountGUID())
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.BidOwn, throttle.DelayUntilNext);
                return;
            }

            bool canBid = auction.MinBid != 0;
            bool canBuyout = auction.BuyoutOrUnitPrice != 0;

            // buyout attempt with wrong amount
            if (!canBid && placeBid.BidAmount != auction.BuyoutOrUnitPrice)
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.BidIncrement, throttle.DelayUntilNext);
                return;
            }

            ulong minBid = auction.BidAmount != 0 ? auction.BidAmount + auction.CalculateMinIncrement() : auction.MinBid;
            if (canBid && placeBid.BidAmount < minBid)
            {
                SendAuctionCommandResult(placeBid.AuctionID, AuctionCommand.PlaceBid, AuctionResult.HigherBid, throttle.DelayUntilNext);
                return;
            }

            SQLTransaction trans = new SQLTransaction();
            ulong priceToPay = placeBid.BidAmount;
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
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_AUCTION_BID);
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
                Player owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);
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
        void HandleAuctionRemoveItem(AuctionRemoveItem removeItem)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, removeItem.TaintedBy.HasValue, AuctionCommand.Cancel);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(removeItem.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionRemoveItem - {removeItem.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionPosting auction = auctionHouse.GetAuction(removeItem.AuctionID);
            Player player = GetPlayer();

            SQLTransaction trans = new SQLTransaction();
            if (auction != null && auction.Owner == player.GetGUID())
            {
                if (auction.Bidder.IsEmpty())                   // If we have a bidder, we have to send him the money he paid
                {
                    ulong cancelCost = MathFunctions.CalculatePct(auction.BidAmount, 5u);
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
            uint auctionIdForClient = auction.IsCommodity() ? 0 : auction.Id;

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
        void HandleReplicateItems(AuctionReplicateItems replicateItems)
        {
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(replicateItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleReplicateItems - {replicateItems.Auctioneer} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionReplicateResponse response = new AuctionReplicateResponse();

            auctionHouse.BuildReplicate(response, GetPlayer(), replicateItems.ChangeNumberGlobal, replicateItems.ChangeNumberCursor, replicateItems.ChangeNumberTombstone, replicateItems.Count);

            response.DesiredDelay = WorldConfig.GetUIntValue(WorldCfg.AuctionSearchDelay) * 5;
            response.Result = 0;

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.AuctionSellCommodity)]
        void HandleAuctionSellCommodity(AuctionSellCommodity sellCommodity)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, sellCommodity.TaintedBy.HasValue, AuctionCommand.SellItem);
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

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(sellCommodity.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (creature == null)
            {
                Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {sellCommodity.Auctioneer} not found or you can't interact with him.");
                return;
            }

            uint houseId = 0;
            AuctionHouseRecord auctionHouseEntry = Global.AuctionHouseMgr.GetAuctionHouseEntry(creature.GetFaction(), ref houseId);
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
            Dictionary<ObjectGuid, (Item Item, ulong UseCount)> items2 = new Dictionary<ObjectGuid, (Item Item, ulong UseCount)>();

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

            TimeSpan auctionTime = TimeSpan.FromSeconds((long)TimeSpan.FromMinutes(sellCommodity.RunTime).TotalSeconds * WorldConfig.GetFloatValue(WorldCfg.RateAuctionTime));
            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            ulong deposit = Global.AuctionHouseMgr.GetCommodityAuctionDeposit(items2.FirstOrDefault().Value.Item.GetTemplate(), TimeSpan.FromMinutes(sellCommodity.RunTime), (uint)totalCount);
            if (!_player.HasEnoughMoney(deposit))
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                return;
            }

            uint auctionId = Global.ObjectMgr.GenerateAuctionID();
            AuctionPosting auction = new AuctionPosting();
            auction.Id = auctionId;
            auction.Owner = _player.GetGUID();
            auction.OwnerAccount = GetAccountGUID();
            auction.BuyoutOrUnitPrice = sellCommodity.UnitPrice;
            auction.Deposit = deposit;
            auction.StartTime = GameTime.GetGameTimeSystemPoint();
            auction.EndTime = auction.StartTime + auctionTime;

            // keep track of what was cloned to undo/modify counts later
            Dictionary<Item, Item> clones = new Dictionary<Item, Item>();
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
                Item logItem = items2.First().Value.Item1;
                Log.outCommand(GetAccountId(), $"GM {GetPlayerName()} (Account: {GetAccountId()}) create auction: {logItem.GetName(Global.WorldMgr.GetDefaultDbcLocale())} (Entry: {logItem.GetEntry()} Count: {totalCount})");
            }

            SQLTransaction trans = new SQLTransaction();

            foreach (var pair in items2)
            {
                Item itemForSale = pair.Value.Item1;
                var cloneItr = clones.LookupByKey(pair.Value.Item1);
                if (cloneItr != null)
                {
                    Item original = itemForSale;
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
        void HandleAuctionSellItem(AuctionSellItem sellItem)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, sellItem.TaintedBy.HasValue, AuctionCommand.SellItem);
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

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(sellItem.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outError(LogFilter.Network, "WORLD: HandleAuctionSellItem - Unit (%s) not found or you can't interact with him.", sellItem.Auctioneer.ToString());
                return;
            }

            uint houseId = 0;
            AuctionHouseRecord auctionHouseEntry = Global.AuctionHouseMgr.GetAuctionHouseEntry(creature.GetFaction(), ref houseId);
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

            TimeSpan auctionTime = TimeSpan.FromSeconds((long)(TimeSpan.FromMinutes(sellItem.RunTime).TotalSeconds * WorldConfig.GetFloatValue(WorldCfg.RateAuctionTime)));
            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            ulong deposit = Global.AuctionHouseMgr.GetItemAuctionDeposit(_player, item, TimeSpan.FromMinutes(sellItem.RunTime));
            if (!_player.HasEnoughMoney(deposit))
            {
                SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);
                return;
            }

            uint auctionId = Global.ObjectMgr.GenerateAuctionID();

            AuctionPosting auction = new AuctionPosting();
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

            SQLTransaction trans = new SQLTransaction();
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
        void HandleAuctionSetFavoriteItem(AuctionSetFavoriteItem setFavoriteItem)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, false);
            if (throttle.Throttled)
                return;

            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTION);
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

        [WorldPacketHandler(ClientOpcodes.AuctionStartCommoditiesPurchase)]
        void HandleAuctionStartCommoditiesPurchase(AuctionStartCommoditiesPurchase startCommoditiesPurchase)
        {
            AuctionThrottleResult throttle = Global.AuctionHouseMgr.CheckThrottle(_player, startCommoditiesPurchase.TaintedBy.HasValue, AuctionCommand.PlaceBid);
            if (throttle.Throttled)
                return;

            Creature creature = GetPlayer().GetNPCIfCanInteractWith(startCommoditiesPurchase.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);
            if (!creature)
            {
                Log.outError(LogFilter.Network, "WORLD: HandleAuctionStartCommoditiesPurchase - {startCommoditiesPurchase.Auctioneer.ToString()} not found or you can't interact with him.");
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            AuctionHouseObject auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.GetFaction());

            AuctionCommodityQuote auctionCommodityQuote = new AuctionCommodityQuote();

            CommodityQuote quote = auctionHouse.CreateCommodityQuote(_player, (uint)startCommoditiesPurchase.ItemID, startCommoditiesPurchase.Quantity);
            if (quote != null)
            {
                auctionCommodityQuote.TotalPrice.Set(quote.TotalPrice);
                auctionCommodityQuote.Quantity.Set(quote.Quantity);
                auctionCommodityQuote.QuoteDuration.Set((int)(quote.ValidTo - GameTime.GetGameTimeSteadyPoint()).TotalMilliseconds);
            }

            auctionCommodityQuote.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;

            SendPacket(auctionCommodityQuote);
        }

        public void SendAuctionHello(ObjectGuid guid, Creature unit)
        {
            if (GetPlayer().GetLevel() < WorldConfig.GetIntValue(WorldCfg.AuctionLevelReq))
            {
                SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.AuctionReq), WorldConfig.GetIntValue(WorldCfg.AuctionLevelReq));
                return;
            }

            AuctionHouseRecord ahEntry = Global.AuctionHouseMgr.GetAuctionHouseEntry(unit.GetFaction());
            if (ahEntry == null)
                return;

            AuctionHelloResponse auctionHelloResponse = new AuctionHelloResponse();
            auctionHelloResponse.Guid = guid;
            auctionHelloResponse.OpenForBusiness = true;
            SendPacket(auctionHelloResponse);
        }

        public void SendAuctionCommandResult(uint auctionId, AuctionCommand command, AuctionResult errorCode, TimeSpan delayForNextAction, InventoryResult bagError = 0)
        {
            AuctionCommandResult auctionCommandResult = new AuctionCommandResult();
            auctionCommandResult.AuctionID = auctionId;
            auctionCommandResult.Command = (int)command;
            auctionCommandResult.ErrorCode = (int)errorCode;
            auctionCommandResult.BagResult = (int)bagError;
            auctionCommandResult.DesiredDelay = (uint)delayForNextAction.TotalSeconds;
            SendPacket(auctionCommandResult);
        }

        public void SendAuctionClosedNotification(AuctionPosting auction, float mailDelay, bool sold)
        {
            AuctionClosedNotification packet = new AuctionClosedNotification();
            packet.Info.Initialize(auction);
            packet.ProceedsMailDelay = mailDelay;
            packet.Sold = sold;
            SendPacket(packet);
        }

        public void SendAuctionOwnerBidNotification(AuctionPosting auction)
        {
            AuctionOwnerBidNotification packet = new AuctionOwnerBidNotification();
            packet.Info.Initialize(auction);
            packet.Bidder = auction.Bidder;
            packet.MinIncrement = auction.CalculateMinIncrement();
            SendPacket(packet);
        }
    }
}
