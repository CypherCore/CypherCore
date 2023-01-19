// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

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

    [Flags]
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
        LegendaryCraftedItemOnly = 0x800,
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

    public enum AuctionPostingServerFlag
    {
        None = 0x0,
        GmLogBuyer = 0x1  // write transaction to gm log file for buyer (optimization flag - avoids querying database for offline player permissions)
    }
}
