// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.BlackMarket;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BlackMarketOpen)]
        void HandleBlackMarketOpen(BlackMarketOpen blackMarketOpen)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(blackMarketOpen.Guid, NPCFlags.BlackMarket, NPCFlags2.BlackMarketView);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketHello - {0} not found or you can't interact with him.", blackMarketOpen.Guid.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendBlackMarketOpenResult(blackMarketOpen.Guid, unit);
        }

        void SendBlackMarketOpenResult(ObjectGuid guid, Creature auctioneer)
        {
            NPCInteractionOpenResult npcInteraction = new();
            npcInteraction.Npc = guid;
            npcInteraction.InteractionType = PlayerInteractionType.BlackMarketAuctioneer;
            npcInteraction.Success = Global.BlackMarketMgr.IsEnabled();
            SendPacket(npcInteraction);
        }

        [WorldPacketHandler(ClientOpcodes.BlackMarketRequestItems)]
        void HandleBlackMarketRequestItems(BlackMarketRequestItems blackMarketRequestItems)
        {
            if (!Global.BlackMarketMgr.IsEnabled())
                return;

            Creature unit = GetPlayer().GetNPCIfCanInteractWith(blackMarketRequestItems.Guid, NPCFlags.BlackMarket, NPCFlags2.BlackMarketView);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketRequestItems - {0} not found or you can't interact with him.", blackMarketRequestItems.Guid.ToString());
                return;
            }

            BlackMarketRequestItemsResult result = new();
            Global.BlackMarketMgr.BuildItemsResponse(result, GetPlayer());
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.BlackMarketBidOnItem)]
        void HandleBlackMarketBidOnItem(BlackMarketBidOnItem blackMarketBidOnItem)
        {
            if (!Global.BlackMarketMgr.IsEnabled())
                return;

            Player player = GetPlayer();
            Creature unit = player.GetNPCIfCanInteractWith(blackMarketBidOnItem.Guid, NPCFlags.BlackMarket, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} not found or you can't interact with him.", blackMarketBidOnItem.Guid.ToString());
                return;
            }

            BlackMarketEntry entry = Global.BlackMarketMgr.GetAuctionByID(blackMarketBidOnItem.MarketID);
            if (entry == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to bid on a nonexistent auction (MarketId: {2}).", player.GetGUID().ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
                SendBlackMarketBidOnItemResult(BlackMarketError.ItemNotFound, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
                return;
            }

            if (entry.GetBidder() == player.GetGUID().GetCounter())
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to place a bid on an item he already bid on. (MarketId: {2}).", player.GetGUID().ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
                SendBlackMarketBidOnItemResult(BlackMarketError.AlreadyBid, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
                return;
            }

            if (!entry.ValidateBid(blackMarketBidOnItem.BidAmount))
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to place an invalid bid. Amount: {2} (MarketId: {3}).", player.GetGUID().ToString(), player.GetName(), blackMarketBidOnItem.BidAmount, blackMarketBidOnItem.MarketID);
                SendBlackMarketBidOnItemResult(BlackMarketError.HigherBid, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
                return;
            }

            if (!player.HasEnoughMoney(blackMarketBidOnItem.BidAmount))
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) does not have enough money to place bid. (MarketId: {2}).", player.GetGUID().ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
                SendBlackMarketBidOnItemResult(BlackMarketError.NotEnoughMoney, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
                return;
            }

            if (entry.GetSecondsRemaining() <= 0)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBlackMarketBidOnItem - {0} (name: {1}) tried to bid on a completed auction. (MarketId: {2}).", player.GetGUID().ToString(), player.GetName(), blackMarketBidOnItem.MarketID);
                SendBlackMarketBidOnItemResult(BlackMarketError.DatabaseError, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
                return;
            }

            SQLTransaction trans = new();

            Global.BlackMarketMgr.SendAuctionOutbidMail(entry, trans);
            entry.PlaceBid(blackMarketBidOnItem.BidAmount, player, trans);

            DB.Characters.CommitTransaction(trans);

            SendBlackMarketBidOnItemResult(BlackMarketError.Ok, blackMarketBidOnItem.MarketID, blackMarketBidOnItem.Item);
        }

        void SendBlackMarketBidOnItemResult(BlackMarketError result, uint marketId, ItemInstance item)
        {
            BlackMarketBidOnItemResult packet = new();

            packet.MarketID = marketId;
            packet.Item = item;
            packet.Result = result;

            SendPacket(packet);
        }

        public void SendBlackMarketWonNotification(BlackMarketEntry entry, Item item)
        {
            BlackMarketWon packet = new();

            packet.MarketID = entry.GetMarketId();
            packet.Item = new ItemInstance(item);

            SendPacket(packet);
        }

        public void SendBlackMarketOutbidNotification(BlackMarketTemplate templ)
        {
            BlackMarketOutbid packet = new();

            packet.MarketID = templ.MarketID;
            packet.Item = templ.Item;
            packet.RandomPropertiesID = 0;

            SendPacket(packet);
        }
    }
}
