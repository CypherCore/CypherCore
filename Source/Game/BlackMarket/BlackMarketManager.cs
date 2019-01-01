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
using Game.Mails;
using Game.Network.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Game.BlackMarket
{
    public class BlackMarketManager : Singleton<BlackMarketManager>
    {
        BlackMarketManager() { }

        public void LoadTemplates()
        {
            uint oldMSTime = Time.GetMSTime();

            // Clear in case we are reloading
            _templates.Clear();

            SQLResult result = DB.World.Query("SELECT marketId, sellerNpc, itemEntry, quantity, minBid, duration, chance, bonusListIDs FROM blackmarket_template");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 black market templates. DB table `blackmarket_template` is empty.");
                return;
            }

            do
            {
                BlackMarketTemplate templ = new BlackMarketTemplate();

                if (!templ.LoadFromDB(result.GetFields())) // Add checks
                    continue;

                AddTemplate(templ);
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} black market templates in {1} ms.", _templates.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadAuctions()
        {
            uint oldMSTime = Time.GetMSTime();

            // Clear in case we are reloading
            _auctions.Clear();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_BLACKMARKET_AUCTIONS);
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 black market auctions. DB table `blackmarket_auctions` is empty.");
                return;
            }

            _lastUpdate = Time.UnixTime; //Set update time before loading

            SQLTransaction trans = new SQLTransaction();
            do
            {
                BlackMarketEntry auction = new BlackMarketEntry();

                if (!auction.LoadFromDB(result.GetFields()))
                {
                    auction.DeleteFromDB(trans);
                    continue;
                }

                if (auction.IsCompleted())
                {
                    auction.DeleteFromDB(trans);
                    continue;
                }

                AddAuction(auction);
            } while (result.NextRow());

            DB.Characters.CommitTransaction(trans);

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} black market auctions in {1} ms.", _auctions.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void Update(bool updateTime = false)
        {
            SQLTransaction trans = new SQLTransaction();
            long now = Time.UnixTime;
            foreach (var entry in _auctions.Values)
            {
                if (entry.IsCompleted() && entry.GetBidder() != 0)
                    SendAuctionWonMail(entry, trans);

                if (updateTime)
                    entry.Update(now);
            }

            if (updateTime)
                _lastUpdate = now;

            DB.Characters.CommitTransaction(trans);
        }

        public void RefreshAuctions()
        {
            SQLTransaction trans = new SQLTransaction();
            // Delete completed auctions
            foreach (var pair in _auctions)
            {
                if (!pair.Value.IsCompleted())
                    continue;

                pair.Value.DeleteFromDB(trans);
                _auctions.Remove(pair.Key);
            }

            DB.Characters.CommitTransaction(trans);
            trans = new SQLTransaction();

            List<BlackMarketTemplate> templates = new List<BlackMarketTemplate>();
            foreach (var pair in _templates)
            {
                if (GetAuctionByID(pair.Value.MarketID) != null)
                    continue;
                if (!RandomHelper.randChance(pair.Value.Chance))
                    continue;

                templates.Add(pair.Value);
            }

            templates.RandomResize(WorldConfig.GetUIntValue(WorldCfg.BlackmarketMaxAuctions));

            foreach (BlackMarketTemplate templat in templates)
            {
                BlackMarketEntry entry = new BlackMarketEntry();
                entry.Initialize(templat.MarketID, (uint)templat.Duration);
                entry.SaveToDB(trans);
                AddAuction(entry);
            }

            DB.Characters.CommitTransaction(trans);

            Update(true);
        }

        public bool IsEnabled()
        {
            return WorldConfig.GetBoolValue(WorldCfg.BlackmarketEnabled);
        }

        public void BuildItemsResponse(BlackMarketRequestItemsResult packet, Player player)
        {
            packet.LastUpdateID = (int)_lastUpdate;
            foreach (var pair in _auctions)
            {
                BlackMarketTemplate templ = pair.Value.GetTemplate();

                BlackMarketItem item = new BlackMarketItem();
                item.MarketID = pair.Value.GetMarketId();
                item.SellerNPC = templ.SellerNPC;
                item.Item = templ.Item;
                item.Quantity = templ.Quantity;

                // No bids yet
                if (pair.Value.GetNumBids() == 0)
                {
                    item.MinBid = templ.MinBid;
                    item.MinIncrement = 1;
                }
                else
                {
                    item.MinIncrement = pair.Value.GetMinIncrement(); // 5% increment minimum
                    item.MinBid = pair.Value.GetCurrentBid() + item.MinIncrement;
                }

                item.CurrentBid = pair.Value.GetCurrentBid();
                item.SecondsRemaining = pair.Value.GetSecondsRemaining();
                item.HighBid = (pair.Value.GetBidder() == player.GetGUID().GetCounter());
                item.NumBids = pair.Value.GetNumBids();

                packet.Items.Add(item);
            }
        }

        public void AddAuction(BlackMarketEntry auction)
        {
            _auctions[auction.GetMarketId()] = auction;
        }

        public void AddTemplate(BlackMarketTemplate templ)
        {
            _templates[templ.MarketID] = templ;
        }

        public void SendAuctionWonMail(BlackMarketEntry entry, SQLTransaction trans)
        {
            // Mail already sent
            if (entry.GetMailSent())
                return;

            uint bidderAccId = 0;
            ObjectGuid bidderGuid = ObjectGuid.Create(HighGuid.Player, entry.GetBidder());
            Player bidder = Global.ObjAccessor.FindConnectedPlayer(bidderGuid);
            // data for gm.log
            string bidderName = "";
            bool logGmTrade = false;

            if (bidder)
            {
                bidderAccId = bidder.GetSession().GetAccountId();
                bidderName = bidder.GetName();
                logGmTrade = bidder.GetSession().HasPermission(RBACPermissions.LogGmTrade);
            }
            else
            {
                bidderAccId = ObjectManager.GetPlayerAccountIdByGUID(bidderGuid);
                if (bidderAccId == 0) // Account exists
                    return;

                logGmTrade = Global.AccountMgr.HasPermission(bidderAccId, RBACPermissions.LogGmTrade, Global.WorldMgr.GetRealmId().Realm);

                if (logGmTrade && !ObjectManager.GetPlayerNameByGUID(bidderGuid, out bidderName))
                    bidderName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);
            }

            // Create item
            BlackMarketTemplate templ = entry.GetTemplate();
            Item item = Item.CreateItem(templ.Item.ItemID, templ.Quantity);
            if (!item)
                return;

            if (templ.Item.ItemBonus.HasValue)
            {
                foreach (uint bonusList in templ.Item.ItemBonus.Value.BonusListIDs)
                    item.AddBonuses(bonusList);
            }

            item.SetOwnerGUID(bidderGuid);

            item.SaveToDB(trans);

            // Log trade
            if (logGmTrade)
                Log.outCommand(bidderAccId, "GM {0} (Account: {1}) won item in blackmarket auction: {2} (Entry: {3} Count: {4}) and payed gold : {5}.",
                    bidderName, bidderAccId, item.GetTemplate().GetName(), item.GetEntry(), item.GetCount(), entry.GetCurrentBid() / MoneyConstants.Gold);

            if (bidder)
                bidder.GetSession().SendBlackMarketWonNotification(entry, item);

            new MailDraft(entry.BuildAuctionMailSubject(BMAHMailAuctionAnswers.Won), entry.BuildAuctionMailBody())
                .AddItem(item)
                .SendMailTo(trans, new MailReceiver(bidder, entry.GetBidder()),new MailSender(entry), MailCheckMask.Copied);

            entry.MailSent();
        }

        public void SendAuctionOutbidMail(BlackMarketEntry entry, SQLTransaction trans)
        {
            ObjectGuid oldBidder_guid = ObjectGuid.Create(HighGuid.Player, entry.GetBidder());
            Player oldBidder = Global.ObjAccessor.FindConnectedPlayer(oldBidder_guid);

            uint oldBidder_accId = 0;
            if (!oldBidder)
                oldBidder_accId = ObjectManager.GetPlayerAccountIdByGUID(oldBidder_guid);

            // old bidder exist
            if (!oldBidder && oldBidder_accId == 0)
                return;

            if (oldBidder)
                oldBidder.GetSession().SendBlackMarketOutbidNotification(entry.GetTemplate());

            new MailDraft(entry.BuildAuctionMailSubject(BMAHMailAuctionAnswers.Outbid), entry.BuildAuctionMailBody())
                .AddMoney(entry.GetCurrentBid())
                .SendMailTo(trans, new MailReceiver(oldBidder, entry.GetBidder()), new MailSender(entry), MailCheckMask.Copied);
        }

        public BlackMarketEntry GetAuctionByID(uint marketId)
        {
            return _auctions.LookupByKey(marketId);
        }

        public BlackMarketTemplate GetTemplateByID(uint marketId)
        {
            return _templates.LookupByKey(marketId);
        }

        public long GetLastUpdate() { return _lastUpdate; }

        Dictionary<uint, BlackMarketEntry> _auctions = new Dictionary<uint, BlackMarketEntry>();
        Dictionary<uint, BlackMarketTemplate> _templates = new Dictionary<uint, BlackMarketTemplate>();
        long _lastUpdate;
    }
}
