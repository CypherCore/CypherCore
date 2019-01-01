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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Network.Packets;
using System.Collections.Generic;

namespace Game.BlackMarket
{
    public class BlackMarketTemplate
    {
        public bool LoadFromDB(SQLFields fields)
        {
            MarketID = fields.Read<uint>(0);
            SellerNPC = fields.Read<uint>(1);
            Item.ItemID = fields.Read<uint>(2);
            Quantity = fields.Read<uint>(3);
            MinBid = fields.Read<ulong>(4);
            Duration = fields.Read<uint>(5);
            Chance = fields.Read<float>(6);

            var bonusListIDsTok = new StringArray(fields.Read<string>(7), ' ');
            List<uint> bonusListIDs = new List<uint>();
            if (!bonusListIDsTok.IsEmpty())
            {
                foreach (string token in bonusListIDsTok)
                {
                    if (uint.TryParse(token, out uint id))
                        bonusListIDs.Add(id);
                }
            }

            if (!bonusListIDs.Empty())
            {
                Item.ItemBonus.HasValue = true;
                Item.ItemBonus.Value.BonusListIDs = bonusListIDs;
            }

            if (Global.ObjectMgr.GetCreatureTemplate(SellerNPC) == null)
            {
                Log.outError(LogFilter.Misc, "Black market template {0} does not have a valid seller. (Entry: {1})", MarketID, SellerNPC);
                return false;
            }

            if (Global.ObjectMgr.GetItemTemplate(Item.ItemID) == null)
            {
                Log.outError(LogFilter.Misc, "Black market template {0} does not have a valid item. (Entry: {1})", MarketID, Item.ItemID);
                return false;
            }

            return true;
        }

        public uint MarketID;
        public uint SellerNPC;
        public uint Quantity;
        public ulong MinBid;
        public long Duration;
        public float Chance;
        public ItemInstance Item;
    }

    public class BlackMarketEntry
    {
        public void Initialize(uint marketId, uint duration)
        {
            _marketId = marketId;
            _secondsRemaining = duration;
        }

        public void Update(long newTimeOfUpdate)
        {
            _secondsRemaining = (uint)(_secondsRemaining - (newTimeOfUpdate - Global.BlackMarketMgr.GetLastUpdate()));
        }

        public BlackMarketTemplate GetTemplate()
        {
            return Global.BlackMarketMgr.GetTemplateByID(_marketId);
        }

        public uint GetSecondsRemaining()
        {
            return (uint)(_secondsRemaining - (Time.UnixTime - Global.BlackMarketMgr.GetLastUpdate()));
        }

        long GetExpirationTime()
        {
            return Time.UnixTime + GetSecondsRemaining();
        }

        public bool IsCompleted()
        {
            return GetSecondsRemaining() <= 0;
        }

        public bool LoadFromDB(SQLFields fields)
        {
            _marketId = fields.Read<uint>(0);

            // Invalid MarketID
            BlackMarketTemplate templ = Global.BlackMarketMgr.GetTemplateByID(_marketId);
            if (templ == null)
            {
                Log.outError(LogFilter.Misc, "Black market auction {0} does not have a valid id.", _marketId);
                return false;
            }

            _currentBid = fields.Read<ulong>(1);
            _secondsRemaining = (uint)(fields.Read<uint>(2) - Global.BlackMarketMgr.GetLastUpdate());
            _numBids = fields.Read<uint>(3);
            _bidder = fields.Read<ulong>(4);

            // Either no bidder or existing player
            if (_bidder != 0 && ObjectManager.GetPlayerAccountIdByGUID(ObjectGuid.Create(HighGuid.Player, _bidder)) == 0) // Probably a better way to check if player exists
            {
                Log.outError(LogFilter.Misc, "Black market auction {0} does not have a valid bidder (GUID: {1}).", _marketId, _bidder);
                return false;
            }

            return true;
        }

        public void SaveToDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_BLACKMARKET_AUCTIONS);

            stmt.AddValue(0, _marketId);
            stmt.AddValue(1, _currentBid);
            stmt.AddValue(2, GetExpirationTime());
            stmt.AddValue(3, _numBids);
            stmt.AddValue(4, _bidder);

            trans.Append(stmt);
        }

        public void DeleteFromDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_BLACKMARKET_AUCTIONS);
            stmt.AddValue(0, _marketId);
            trans.Append(stmt);
        }

        public bool ValidateBid(ulong bid)
        {
            if (bid <= _currentBid)
                return false;

            if (bid < _currentBid + GetMinIncrement())
                return false;

            if (bid >= BlackMarketConst.MaxBid)
                return false;

            return true;
        }

        public void PlaceBid(ulong bid, Player player, SQLTransaction trans)   //Updated
        {
            if (bid < _currentBid)
                return;

            _currentBid = bid;
            ++_numBids;

            if (GetSecondsRemaining() < 30 * Time.Minute)
                _secondsRemaining += 30 * Time.Minute;

            _bidder = player.GetGUID().GetCounter();

            player.ModifyMoney(-(long)bid);


            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_BLACKMARKET_AUCTIONS);

            stmt.AddValue(0, _currentBid);
            stmt.AddValue(1, GetExpirationTime());
            stmt.AddValue(2, _numBids);
            stmt.AddValue(3, _bidder);
            stmt.AddValue(4, _marketId);

            trans.Append(stmt);

            Global.BlackMarketMgr.Update(true);
        }

        public string BuildAuctionMailSubject(BMAHMailAuctionAnswers response)
        {
            return GetTemplate().Item.ItemID + ":0:" + response + ':' + GetMarketId() + ':' + GetTemplate().Quantity;
        }

        public string BuildAuctionMailBody()
        {
            return GetTemplate().SellerNPC + ":" + _currentBid;
        }


        public uint GetMarketId() { return _marketId; }

        public ulong GetCurrentBid() { return _currentBid; }
        void SetCurrentBid(ulong bid) { _currentBid = bid; }

        public uint GetNumBids() { return _numBids; }
        void SetNumBids(uint numBids) { _numBids = numBids; }

        public ulong GetBidder() { return _bidder; }
        void SetBidder(ulong bidder) { _bidder = bidder; }

        public ulong GetMinIncrement() { return (_currentBid / 20) - ((_currentBid / 20) % MoneyConstants.Gold); } //5% increase every bid (has to be round gold value)

        public void MailSent() { _mailSent = true; } // Set when mail has been sent
        public bool GetMailSent() { return _mailSent; }

        uint _marketId;
        ulong _currentBid;
        uint _numBids;
        ulong _bidder;
        uint _secondsRemaining;
        bool _mailSent;
    }
}
