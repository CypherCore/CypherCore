// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;

namespace Game.Networking.Packets
{
    internal class AuctionBrowseQuery : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public AuctionHouseFilterMask Filters;
        public Array<AuctionListFilterClass> ItemClassFilters = new(7);
        public byte[] KnownPets;
        public byte MaxLevel = SharedConst.MaxLevel;
        public sbyte MaxPetLevel;
        public byte MinLevel = 1;
        public string Name;
        public uint Offset;
        public Array<AuctionSortDef> Sorts = new(2);
        public AddOnInfo? TaintedBy;

        public AuctionBrowseQuery(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();
            MinLevel = _worldPacket.ReadUInt8();
            MaxLevel = _worldPacket.ReadUInt8();
            Filters = (AuctionHouseFilterMask)_worldPacket.ReadUInt32();
            uint knownPetSize = _worldPacket.ReadUInt32();
            MaxPetLevel = _worldPacket.ReadInt8();

            uint sizeLimit = CliDB.BattlePetSpeciesStorage.GetNumRows() / 8 + 1;

            if (knownPetSize >= sizeLimit)
                throw new Exception($"Attempted to read more array elements from packet {knownPetSize} than allowed {sizeLimit}");

            KnownPets = new byte[knownPetSize];

            for (var i = 0; i < knownPetSize; ++i)
                KnownPets[i] = _worldPacket.ReadUInt8();

            if (_worldPacket.HasBit())
                TaintedBy = new AddOnInfo();

            uint nameLength = _worldPacket.ReadBits<uint>(8);
            uint itemClassFilterCount = _worldPacket.ReadBits<uint>(3);
            uint sortSize = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortSize; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            Name = _worldPacket.ReadString(nameLength);

            for (var i = 0; i < itemClassFilterCount; ++i) // AuctionListFilterClass filterClass in ItemClassFilters)
                ItemClassFilters[i] = new AuctionListFilterClass(_worldPacket);
        }
    }

    internal class AuctionCancelCommoditiesPurchase : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public AddOnInfo? TaintedBy;

        public AuctionCancelCommoditiesPurchase(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();

            if (_worldPacket.HasBit())
            {
                TaintedBy = new AddOnInfo();
                TaintedBy.Value.Read(_worldPacket);
            }
        }
    }

    internal class AuctionConfirmCommoditiesPurchase : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public int ItemID;
        public uint Quantity;
        public AddOnInfo? TaintedBy;

        public AuctionConfirmCommoditiesPurchase(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ItemID = _worldPacket.ReadInt32();
            Quantity = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
            {
                TaintedBy = new AddOnInfo();
                TaintedBy.Value.Read(_worldPacket);
            }
        }
    }

    internal class AuctionHelloRequest : ClientPacket
    {
        public ObjectGuid Guid;

        public AuctionHelloRequest(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }
    }

    internal class AuctionListBiddedItems : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public List<uint> AuctionItemIDs = new();
        public uint Offset;
        public Array<AuctionSortDef> Sorts = new(2);
        public AddOnInfo? TaintedBy;

        public AuctionListBiddedItems(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
                TaintedBy = new AddOnInfo();

            uint auctionIDCount = _worldPacket.ReadBits<uint>(7);
            uint sortCount = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            for (var i = 0; i < auctionIDCount; ++i)
                AuctionItemIDs[i] = _worldPacket.ReadUInt32();
        }
    }

    internal class AuctionListBucketsByBucketKeys : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public Array<AuctionBucketKey> BucketKeys = new(100);
        public Array<AuctionSortDef> Sorts = new(2);
        public AddOnInfo? TaintedBy;

        public AuctionListBucketsByBucketKeys(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();

            if (_worldPacket.HasBit())
                TaintedBy = new AddOnInfo();

            uint bucketKeysCount = _worldPacket.ReadBits<uint>(7);
            uint sortCount = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            for (var i = 0; i < bucketKeysCount; ++i)
                BucketKeys[i] = new AuctionBucketKey(_worldPacket);
        }
    }

    internal class AuctionListItemsByBucketKey : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public AuctionBucketKey BucketKey;
        public uint Offset;
        public Array<AuctionSortDef> Sorts = new(2);
        public AddOnInfo? TaintedBy;
        public sbyte Unknown830;

        public AuctionListItemsByBucketKey(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();
            Unknown830 = _worldPacket.ReadInt8();

            if (_worldPacket.HasBit())
                TaintedBy = new AddOnInfo();

            uint sortCount = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            BucketKey = new AuctionBucketKey(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    internal class AuctionListItemsByItemID : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint ItemID;
        public uint Offset;
        public Array<AuctionSortDef> Sorts = new(2);
        public int SuffixItemNameDescriptionID;
        public AddOnInfo? TaintedBy;

        public AuctionListItemsByItemID(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ItemID = _worldPacket.ReadUInt32();
            SuffixItemNameDescriptionID = _worldPacket.ReadInt32();
            Offset = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
                TaintedBy = new AddOnInfo();

            uint sortCount = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    internal class AuctionListOwnedItems : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint Offset;
        public Array<AuctionSortDef> Sorts = new(2);
        public AddOnInfo? TaintedBy;

        public AuctionListOwnedItems(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
                TaintedBy = new AddOnInfo();

            uint sortCount = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    internal class AuctionPlaceBid : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint AuctionID;
        public ulong BidAmount;
        public AddOnInfo? TaintedBy;

        public AuctionPlaceBid(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            AuctionID = _worldPacket.ReadUInt32();
            BidAmount = _worldPacket.ReadUInt64();

            if (_worldPacket.HasBit())
            {
                TaintedBy = new AddOnInfo();
                TaintedBy.Value.Read(_worldPacket);
            }
        }
    }

    internal class AuctionRemoveItem : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint AuctionID;
        public int ItemID;
        public AddOnInfo? TaintedBy;

        public AuctionRemoveItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            AuctionID = _worldPacket.ReadUInt32();
            ItemID = _worldPacket.ReadInt32();

            if (_worldPacket.HasBit())
            {
                TaintedBy = new AddOnInfo();
                TaintedBy.Value.Read(_worldPacket);
            }
        }
    }

    internal class AuctionReplicateItems : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint ChangeNumberCursor;
        public uint ChangeNumberGlobal;
        public uint ChangeNumberTombstone;
        public uint Count;
        public AddOnInfo? TaintedBy;

        public AuctionReplicateItems(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ChangeNumberGlobal = _worldPacket.ReadUInt32();
            ChangeNumberCursor = _worldPacket.ReadUInt32();
            ChangeNumberTombstone = _worldPacket.ReadUInt32();
            Count = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
            {
                TaintedBy = new AddOnInfo();
                TaintedBy.Value.Read(_worldPacket);
            }
        }
    }

    internal class AuctionRequestFavoriteList : ClientPacket
    {
        public AuctionRequestFavoriteList(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class AuctionSellCommodity : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public Array<AuctionItemForSale> Items = new(64);
        public uint RunTime;
        public AddOnInfo? TaintedBy;
        public ulong UnitPrice;

        public AuctionSellCommodity(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            UnitPrice = _worldPacket.ReadUInt64();
            RunTime = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
                TaintedBy = new AddOnInfo();

            uint itemCount = _worldPacket.ReadBits<uint>(6);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            for (var i = 0; i < itemCount; ++i)
                Items[i] = new AuctionItemForSale(_worldPacket);
        }
    }

    internal class AuctionSellItem : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public ulong BuyoutPrice;
        public Array<AuctionItemForSale> Items = new(1);
        public ulong MinBid;
        public uint RunTime;
        public AddOnInfo? TaintedBy;

        public AuctionSellItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            MinBid = _worldPacket.ReadUInt64();
            BuyoutPrice = _worldPacket.ReadUInt64();
            RunTime = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
                TaintedBy = new AddOnInfo();

            uint itemCount = _worldPacket.ReadBits<uint>(6);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            for (var i = 0; i < itemCount; ++i)
                Items[i] = new AuctionItemForSale(_worldPacket);
        }
    }

    internal class AuctionSetFavoriteItem : ClientPacket
    {
        public bool IsNotFavorite = true;
        public AuctionFavoriteInfo Item;

        public AuctionSetFavoriteItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            IsNotFavorite = _worldPacket.HasBit();
            Item = new AuctionFavoriteInfo(_worldPacket);
        }
    }

    internal class AuctionGetCommodityQuote : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public int ItemID;
        public uint Quantity;
        public AddOnInfo? TaintedBy;

        public AuctionGetCommodityQuote(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ItemID = _worldPacket.ReadInt32();
            Quantity = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
            {
                TaintedBy = new AddOnInfo();
                TaintedBy.Value.Read(_worldPacket);
            }
        }
    }

    internal class AuctionClosedNotification : ServerPacket
    {
        public AuctionOwnerNotification Info;
        public float ProceedsMailDelay;
        public bool Sold = true;

        public AuctionClosedNotification() : base(ServerOpcodes.AuctionClosedNotification)
        {
        }

        public override void Write()
        {
            Info.Write(_worldPacket);
            _worldPacket.WriteFloat(ProceedsMailDelay);
            _worldPacket.WriteBit(Sold);
            _worldPacket.FlushBits();
        }
    }

    internal class AuctionCommandResult : ServerPacket
    {
        public uint AuctionID;

        /// < the error code that was generated when trying to perform the action. Possible values are @ ref AuctionError
        public int BagResult;

        ///< the Id of the auction that triggered this notification
        public int Command;

        ///< the amount of money that the player bid in copper
        public uint DesiredDelay;

        /// < the Type of Action that triggered this notification. Possible values are @ ref AuctionAction
        public int ErrorCode;

        /// < the bid error. Possible values are @ ref AuctionError
        public ObjectGuid Guid;

        ///< the GUID of the bidder for this auction.
        public ulong MinIncrement;

        ///< the sum of outbid is (1% of current bid) * 5, if the bid is too small, then this value is 1 copper.
        public ulong Money;

        public AuctionCommandResult() : base(ServerOpcodes.AuctionCommandResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AuctionID);
            _worldPacket.WriteInt32(Command);
            _worldPacket.WriteInt32(ErrorCode);
            _worldPacket.WriteInt32(BagResult);
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt64(MinIncrement);
            _worldPacket.WriteUInt64(Money);
            _worldPacket.WriteUInt32(DesiredDelay);
        }
    }

    internal class AuctionGetCommodityQuoteResult : ServerPacket
    {
        public uint DesiredDelay;
        public int ItemID;
        public uint? Quantity;
        public int? QuoteDuration;
        public ulong? TotalPrice;

        public AuctionGetCommodityQuoteResult() : base(ServerOpcodes.AuctionGetCommodityQuoteResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBit(TotalPrice.HasValue);
            _worldPacket.WriteBit(Quantity.HasValue);
            _worldPacket.WriteBit(QuoteDuration.HasValue);
            _worldPacket.WriteInt32(ItemID);
            _worldPacket.WriteUInt32(DesiredDelay);

            if (TotalPrice.HasValue)
                _worldPacket.WriteUInt64(TotalPrice.Value);

            if (Quantity.HasValue)
                _worldPacket.WriteUInt32(Quantity.Value);

            if (QuoteDuration.HasValue)
                _worldPacket.WriteInt32(QuoteDuration.Value);
        }
    }

    internal class AuctionHelloResponse : ServerPacket
    {
        public uint DeliveryDelay;
        public ObjectGuid Guid;
        public bool OpenForBusiness = true;

        public AuctionHelloResponse() : base(ServerOpcodes.AuctionHelloResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(DeliveryDelay);
            _worldPacket.WriteBit(OpenForBusiness);
            _worldPacket.FlushBits();
        }
    }

    public class AuctionListBiddedItemsResult : ServerPacket
    {
        public uint DesiredDelay;
        public bool HasMoreResults;
        public List<AuctionItem> Items = new();

        public AuctionListBiddedItemsResult() : base(ServerOpcodes.AuctionListBiddedItemsResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteBit(HasMoreResults);
            _worldPacket.FlushBits();

            foreach (AuctionItem item in Items)
                item.Write(_worldPacket);
        }
    }

    public class AuctionListBucketsResult : ServerPacket
    {
        public AuctionHouseBrowseMode BrowseMode;
        public List<BucketInfo> Buckets = new();
        public uint DesiredDelay;
        public bool HasMoreResults;
        public int Unknown830_0;
        public int Unknown830_1;

        public AuctionListBucketsResult() : base(ServerOpcodes.AuctionListBucketsResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Buckets.Count);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteInt32(Unknown830_0);
            _worldPacket.WriteInt32(Unknown830_1);
            _worldPacket.WriteBits((int)BrowseMode, 1);
            _worldPacket.WriteBit(HasMoreResults);
            _worldPacket.FlushBits();

            foreach (BucketInfo bucketInfo in Buckets)
                bucketInfo.Write(_worldPacket);
        }
    }

    internal class AuctionFavoriteList : ServerPacket
    {
        public uint DesiredDelay;
        public List<AuctionFavoriteInfo> Items = new();

        public AuctionFavoriteList() : base(ServerOpcodes.AuctionFavoriteList)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteBits(Items.Count, 7);
            _worldPacket.FlushBits();

            foreach (AuctionFavoriteInfo favoriteInfo in Items)
                favoriteInfo.Write(_worldPacket);
        }
    }

    public class AuctionListItemsResult : ServerPacket
    {
        public AuctionBucketKey BucketKey = new();
        public uint DesiredDelay;
        public bool HasMoreResults;
        public List<AuctionItem> Items = new();
        public AuctionHouseListType ListType;
        public uint TotalCount;
        public uint Unknown830;

        public AuctionListItemsResult() : base(ServerOpcodes.AuctionListItemsResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteUInt32(Unknown830);
            _worldPacket.WriteUInt32(TotalCount);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteBits((int)ListType, 2);
            _worldPacket.WriteBit(HasMoreResults);
            _worldPacket.FlushBits();

            BucketKey.Write(_worldPacket);

            foreach (AuctionItem item in Items)
                item.Write(_worldPacket);
        }
    }

    public class AuctionListOwnedItemsResult : ServerPacket
    {
        public uint DesiredDelay;
        public bool HasMoreResults;
        public List<AuctionItem> Items = new();
        public List<AuctionItem> SoldItems = new();

        public AuctionListOwnedItemsResult() : base(ServerOpcodes.AuctionListOwnedItemsResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteInt32(SoldItems.Count);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteBit(HasMoreResults);
            _worldPacket.FlushBits();

            foreach (AuctionItem item in Items)
                item.Write(_worldPacket);

            foreach (AuctionItem item in SoldItems)
                item.Write(_worldPacket);
        }
    }

    internal class AuctionOutbidNotification : ServerPacket
    {
        public ulong BidAmount;
        public AuctionBidderNotification Info;
        public ulong MinIncrement;

        public AuctionOutbidNotification() : base(ServerOpcodes.AuctionOutbidNotification)
        {
        }

        public override void Write()
        {
            Info.Write(_worldPacket);
            _worldPacket.WriteUInt64(BidAmount);
            _worldPacket.WriteUInt64(MinIncrement);
        }
    }

    internal class AuctionOwnerBidNotification : ServerPacket
    {
        public ObjectGuid Bidder;
        public AuctionOwnerNotification Info;
        public ulong MinIncrement;

        public AuctionOwnerBidNotification() : base(ServerOpcodes.AuctionOwnerBidNotification)
        {
        }

        public override void Write()
        {
            Info.Write(_worldPacket);
            _worldPacket.WriteUInt64(MinIncrement);
            _worldPacket.WritePackedGuid(Bidder);
        }
    }

    public class AuctionReplicateResponse : ServerPacket
    {
        public uint ChangeNumberCursor;
        public uint ChangeNumberGlobal;
        public uint ChangeNumberTombstone;
        public uint DesiredDelay;
        public List<AuctionItem> Items = new();
        public uint Result;

        public AuctionReplicateResponse() : base(ServerOpcodes.AuctionReplicateResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteUInt32(ChangeNumberGlobal);
            _worldPacket.WriteUInt32(ChangeNumberCursor);
            _worldPacket.WriteUInt32(ChangeNumberTombstone);
            _worldPacket.WriteInt32(Items.Count);

            foreach (AuctionItem item in Items)
                item.Write(_worldPacket);
        }
    }

    internal class AuctionWonNotification : ServerPacket
    {
        public AuctionBidderNotification Info;

        public AuctionWonNotification() : base(ServerOpcodes.AuctionWonNotification)
        {
        }

        public override void Write()
        {
            Info.Write(_worldPacket);
        }
    }

    //Structs
    public class AuctionBucketKey
    {
        public ushort? BattlePetSpeciesID;
        public uint ItemID;
        public ushort ItemLevel;
        public ushort? SuffixItemNameDescriptionID;

        public AuctionBucketKey()
        {
        }

        public AuctionBucketKey(AuctionsBucketKey key)
        {
            ItemID = key.ItemId;
            ItemLevel = key.ItemLevel;

            if (key.BattlePetSpeciesId != 0)
                BattlePetSpeciesID = key.BattlePetSpeciesId;

            if (key.SuffixItemNameDescriptionId != 0)
                SuffixItemNameDescriptionID = key.SuffixItemNameDescriptionId;
        }

        public AuctionBucketKey(WorldPacket data)
        {
            data.ResetBitPos();
            ItemID = data.ReadBits<uint>(20);
            bool hasBattlePetSpeciesId = data.HasBit();
            ItemLevel = data.ReadBits<ushort>(11);
            bool hasSuffixItemNameDescriptionId = data.HasBit();

            if (hasBattlePetSpeciesId)
                BattlePetSpeciesID = data.ReadUInt16();

            if (hasSuffixItemNameDescriptionId)
                SuffixItemNameDescriptionID = data.ReadUInt16();
        }

        public void Write(WorldPacket data)
        {
            data.WriteBits(ItemID, 20);
            data.WriteBit(BattlePetSpeciesID.HasValue);
            data.WriteBits(ItemLevel, 11);
            data.WriteBit(SuffixItemNameDescriptionID.HasValue);
            data.FlushBits();

            if (BattlePetSpeciesID.HasValue)
                data.WriteUInt16(BattlePetSpeciesID.Value);

            if (SuffixItemNameDescriptionID.HasValue)
                data.WriteUInt16(SuffixItemNameDescriptionID.Value);
        }
    }

    public struct AuctionListFilterSubClass
    {
        public int ItemSubclass;
        public ulong InvTypeMask;

        public AuctionListFilterSubClass(WorldPacket data)
        {
            InvTypeMask = data.ReadUInt64();
            ItemSubclass = data.ReadInt32();
        }
    }

    public class AuctionListFilterClass
    {
        public int ItemClass;
        public Array<AuctionListFilterSubClass> SubClassFilters = new(31);

        public AuctionListFilterClass(WorldPacket data)
        {
            ItemClass = data.ReadInt32();
            uint subClassFilterCount = data.ReadBits<uint>(5);

            for (var i = 0; i < subClassFilterCount; ++i)
                SubClassFilters[i] = new AuctionListFilterSubClass(data);
        }
    }

    public struct AuctionSortDef
    {
        public AuctionHouseSortOrder SortOrder;
        public bool ReverseSort;

        public AuctionSortDef(AuctionHouseSortOrder sortOrder, bool reverseSort)
        {
            SortOrder = sortOrder;
            ReverseSort = reverseSort;
        }

        public AuctionSortDef(WorldPacket data)
        {
            data.ResetBitPos();
            SortOrder = (AuctionHouseSortOrder)data.ReadBits<uint>(4);
            ReverseSort = data.HasBit();
        }
    }

    public struct AuctionItemForSale
    {
        public ObjectGuid Guid;
        public uint UseCount;

        public AuctionItemForSale(WorldPacket data)
        {
            Guid = data.ReadPackedGuid();
            UseCount = data.ReadUInt32();
        }
    }

    public struct AuctionFavoriteInfo
    {
        public uint Order;
        public uint ItemID;
        public uint ItemLevel;
        public uint BattlePetSpeciesID;
        public uint SuffixItemNameDescriptionID;

        public AuctionFavoriteInfo(WorldPacket data)
        {
            Order = data.ReadUInt32();
            ItemID = data.ReadUInt32();
            ItemLevel = data.ReadUInt32();
            BattlePetSpeciesID = data.ReadUInt32();
            SuffixItemNameDescriptionID = data.ReadUInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Order);
            data.WriteUInt32(ItemID);
            data.WriteUInt32(ItemLevel);
            data.WriteUInt32(BattlePetSpeciesID);
            data.WriteUInt32(SuffixItemNameDescriptionID);
        }
    }

    public struct AuctionOwnerNotification
    {
        public uint AuctionID;
        public ulong BidAmount;
        public ItemInstance Item;

        public void Initialize(AuctionPosting auction)
        {
            AuctionID = auction.Id;
            Item = new ItemInstance(auction.Items[0]);
            BidAmount = auction.BidAmount;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(AuctionID);
            data.WriteUInt64(BidAmount);
            Item.Write(data);
        }
    }

    public class BucketInfo
    {
        public byte? BattlePetBreedID;
        public bool ContainsOnlyCollectedAppearances;
        public bool ContainsOwnerItem;
        public List<uint> ItemModifiedAppearanceIDs = new();
        public AuctionBucketKey Key;
        public byte? MaxBattlePetLevel;
        public byte? MaxBattlePetQuality;
        public ulong MinPrice;
        public int RequiredLevel;
        public int TotalQuantity;
        public uint? Unk901_1;

        public void Write(WorldPacket data)
        {
            Key.Write(data);
            data.WriteInt32(TotalQuantity);
            data.WriteInt32(RequiredLevel);
            data.WriteUInt64(MinPrice);
            data.WriteInt32(ItemModifiedAppearanceIDs.Count);

            if (!ItemModifiedAppearanceIDs.Empty())
                foreach (int id in ItemModifiedAppearanceIDs)
                    data.WriteInt32(id);

            data.WriteBit(MaxBattlePetQuality.HasValue);
            data.WriteBit(MaxBattlePetLevel.HasValue);
            data.WriteBit(BattlePetBreedID.HasValue);
            data.WriteBit(Unk901_1.HasValue);
            data.WriteBit(ContainsOwnerItem);
            data.WriteBit(ContainsOnlyCollectedAppearances);
            data.FlushBits();

            if (MaxBattlePetQuality.HasValue)
                data.WriteUInt8(MaxBattlePetQuality.Value);

            if (MaxBattlePetLevel.HasValue)
                data.WriteUInt8(MaxBattlePetLevel.Value);

            if (BattlePetBreedID.HasValue)
                data.WriteUInt8(BattlePetBreedID.Value);

            if (Unk901_1.HasValue)
                data.WriteUInt32(Unk901_1.Value);
        }
    }

    public class AuctionItem
    {
        public AuctionBucketKey AuctionBucketKey;
        public uint AuctionID;
        public ulong? BidAmount;
        public ObjectGuid? Bidder;
        public ulong? BuyoutPrice;
        public bool CensorBidInfo;
        public bool CensorServerSideInfo;
        public int Charges;
        public int Count;
        public ObjectGuid? Creator;
        public byte DeleteReason;
        public int DurationLeft;
        public List<ItemEnchantData> Enchantments = new();
        public uint EndTime;
        public uint Flags;
        public List<ItemGemData> Gems = new();
        public ItemInstance Item;
        public ObjectGuid ItemGuid;
        public ulong? MinBid;
        public ulong? MinIncrement;
        public ObjectGuid Owner;
        public ObjectGuid OwnerAccountID;
        public ulong? UnitPrice;

        public void Write(WorldPacket data)
        {
            data.WriteBit(Item != null);
            data.WriteBits(Enchantments.Count, 4);
            data.WriteBits(Gems.Count, 2);
            data.WriteBit(MinBid.HasValue);
            data.WriteBit(MinIncrement.HasValue);
            data.WriteBit(BuyoutPrice.HasValue);
            data.WriteBit(UnitPrice.HasValue);
            data.WriteBit(CensorServerSideInfo);
            data.WriteBit(CensorBidInfo);
            data.WriteBit(AuctionBucketKey != null);
            data.WriteBit(Creator.HasValue);

            if (!CensorBidInfo)
            {
                data.WriteBit(Bidder.HasValue);
                data.WriteBit(BidAmount.HasValue);
            }

            data.FlushBits();

            Item?.Write(data);

            data.WriteInt32(Count);
            data.WriteInt32(Charges);
            data.WriteUInt32(Flags);
            data.WriteUInt32(AuctionID);
            data.WritePackedGuid(Owner);
            data.WriteInt32(DurationLeft);
            data.WriteUInt8(DeleteReason);

            foreach (ItemEnchantData enchant in Enchantments)
                enchant.Write(data);

            if (MinBid.HasValue)
                data.WriteUInt64(MinBid.Value);

            if (MinIncrement.HasValue)
                data.WriteUInt64(MinIncrement.Value);

            if (BuyoutPrice.HasValue)
                data.WriteUInt64(BuyoutPrice.Value);

            if (UnitPrice.HasValue)
                data.WriteUInt64(UnitPrice.Value);

            if (!CensorServerSideInfo)
            {
                data.WritePackedGuid(ItemGuid);
                data.WritePackedGuid(OwnerAccountID);
                data.WriteUInt32(EndTime);
            }

            if (Creator.HasValue)
                data.WritePackedGuid(Creator.Value);

            if (!CensorBidInfo)
            {
                if (Bidder.HasValue)
                    data.WritePackedGuid(Bidder.Value);

                if (BidAmount.HasValue)
                    data.WriteUInt64(BidAmount.Value);
            }

            foreach (ItemGemData gem in Gems)
                gem.Write(data);

            AuctionBucketKey?.Write(data);
        }
    }

    internal struct AuctionBidderNotification
    {
        public uint AuctionID;
        public ObjectGuid Bidder;
        public ItemInstance Item;

        public void Initialize(AuctionPosting auction, Item item)
        {
            AuctionID = auction.Id;
            Item = new ItemInstance(item);
            Bidder = auction.Bidder;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(AuctionID);
            data.WritePackedGuid(Bidder);
            Item.Write(data);
        }
    }
}