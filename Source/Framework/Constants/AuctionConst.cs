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

namespace Framework.Constants
{
    public enum AuctionResult
    {
        Ok = 0,
        Inventory = 1,
        DatabaseError = 2,
        NotEnoughMoney = 3,
        ItemNotFound = 4,
        HigherBid = 5,
        BidIncrement = 7,
        BidOwn = 10,
        RestrictedAccountTrial = 13,
        HasRestriction = 17,
        AuctionHouseBusy = 18,
        AuctionHouseUnavailable = 19,
        CommodityPurchaseFailed = 21,
        ItemHasQuote = 23
    }

    public enum AuctionCommand
    {
        SellItem = 0,
        Cancel = 1,
        PlaceBid = 2
    }

    public enum AuctionMailType
    {
        Outbid = 0,
        Won = 1,
        Sold = 2,
        Expired = 3,
        Removed = 4,
        Cancelled = 5,
        Invoice = 6
    }

    public enum AuctionHouseResultLimits
    {
        Browse = 500,
        Items = 50
    }

    public enum AuctionHouseFilterMask
    {
        None = 0x0,
        UncollectedOnly = 0x1,
        UsableOnly = 0x2,
        UpgradesOnly = 0x4,
        ExactMatch = 0x8,
        PoorQuality = 0x10,
        CommonQuality = 0x20,
        UncommonQuality = 0x40,
        RareQuality = 0x80,
        EpicQuality = 0x100,
        LegendaryQuality = 0x200,
        ArtifactQuality = 0x400,
    }

    public enum AuctionHouseSortOrder
    {
        Price = 0,
        Name = 1,
        Level = 2,
        Bid = 3,
        Buyout = 4
    }

    public enum AuctionHouseBrowseMode
    {
        Search = 0,
        SpecificKeys = 1
    }

    public enum AuctionHouseListType
    {
        Commodities = 1,
        Items = 2
    }
}
