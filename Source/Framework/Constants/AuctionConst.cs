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
 */﻿

namespace Framework.Constants
{
    public enum AuctionError
    {
        Ok = 0,
        Inventory = 1,
        DatabaseError = 2,
        NotEnoughtMoney = 3,
        ItemNotFound = 4,
        HigherBid = 5,
        BidIncrement = 7,
        BidOwn = 10,
        RestrictedAccount = 13
    }

    public enum AuctionAction
    {
        SellItem = 0,
        Cancel = 1,
        PlaceBid = 2
    }

    public enum MailAuctionAnswers
    {
        Outbidded = 0,
        Won = 1,
        Successful = 2,
        Expired = 3,
        CancelledToBidder = 4,
        Canceled = 5,
        SalePending = 6
    }
}
