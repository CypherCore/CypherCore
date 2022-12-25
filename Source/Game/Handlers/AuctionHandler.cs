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

            SQLTransaction trans = new();
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
            if (HasPermission(RBACPermissions.LogGmTrade))
                auction.ServerFlags |= AuctionPostingServerFlag.GmLogBuyer;
            else
                auction.ServerFlags &= ~AuctionPostingServerFlag.GmLogBuyer;

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
                stmt.AddValue(2, (byte)auction.ServerFlags);
                stmt.AddValue(3, auction.Id);
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
                        GetPlayer().UpdateCriteria(CriteriaType.HighestAuctionBid, placeBid.BidAmount);
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

            SQLTransaction trans = new();
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

            AuctionReplicateResponse response = new();

            auctionHouse.BuildReplicate(response, GetPlayer(), replicateItems.ChangeNumberGlobal, replicateItems.ChangeNumberCursor, replicateItems.ChangeNumberTombstone, replicateItems.Count);

            response.DesiredDelay = WorldConfig.GetUIntValue(WorldCfg.AuctionSearchDelay) * 5;
            response.Result = 0;

            SendPacket(response);
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
                item.GetTemplate().HasFlag(ItemFlags.Conjured) || item.m_itemData.Expiration != 0 ||
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

            AuctionPosting auction = new();
            auction.Id = auctionId;
            auction.Owner = _player.GetGUID();
            auction.OwnerAccount = GetAccountGUID();
            auction.MinBid = sellItem.MinBid;
            auction.BuyoutOrUnitPrice = sellItem.BuyoutPrice;
            auction.Deposit = deposit;
            auction.BidAmount = sellItem.MinBid;
            auction.StartTime = GameTime.GetSystemTime();
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

            SQLTransaction trans = new();
            item.DeleteFromInventoryDB(trans);
            item.SaveToDB(trans);

            auctionHouse.AddAuction(trans, auction);
            _player.SaveInventoryAndGoldToDB(trans);

            var auctionPlayerGuid = _player.GetGUID();
            AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans)).AfterComplete(success =>
            {
                if (GetPlayer() && GetPlayer().GetGUID() == auctionPlayerGuid)
                {
                    if (success)
                    {
                        GetPlayer().UpdateCriteria(CriteriaType.ItemsPostedAtAuction, 1);
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

            SQLTransaction trans = new();

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

            AuctionHelloResponse auctionHelloResponse = new();
            auctionHelloResponse.Guid = guid;
            auctionHelloResponse.OpenForBusiness = true;
            SendPacket(auctionHelloResponse);
        }

        public void SendAuctionCommandResult(uint auctionId, AuctionCommand command, AuctionResult errorCode, TimeSpan delayForNextAction, InventoryResult bagError = 0)
        {
            AuctionCommandResult auctionCommandResult = new();
            auctionCommandResult.AuctionID = auctionId;
            auctionCommandResult.Command = (int)command;
            auctionCommandResult.ErrorCode = (int)errorCode;
            auctionCommandResult.BagResult = (int)bagError;
            auctionCommandResult.DesiredDelay = (uint)delayForNextAction.TotalSeconds;
            SendPacket(auctionCommandResult);
        }

        public void SendAuctionClosedNotification(AuctionPosting auction, float mailDelay, bool sold)
        {
            AuctionClosedNotification packet = new();
            packet.Info.Initialize(auction);
            packet.ProceedsMailDelay = mailDelay;
            packet.Sold = sold;
            SendPacket(packet);
        }

        public void SendAuctionOwnerBidNotification(AuctionPosting auction)
        {
            AuctionOwnerBidNotification packet = new();
            packet.Info.Initialize(auction);
            packet.Bidder = auction.Bidder;
            packet.MinIncrement = auction.CalculateMinIncrement();
            SendPacket(packet);
        }
    }
}
