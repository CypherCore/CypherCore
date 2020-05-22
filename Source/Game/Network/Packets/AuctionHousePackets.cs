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
using Game.Entities;
using System.Collections.Generic;
using Framework.Dynamic;

namespace Game.Network.Packets
{
    class AuctionBrowseQuery : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint Offset;
        public byte MinLevel = 1;
        public byte MaxLevel = SharedConst.MaxLevel;
        public AuctionHouseFilterMask Filters;
        public Array<byte> KnownPets = new Array<byte>(SharedConst.MaxBattlePetSpeciesId / 8 + 1);
        public sbyte MaxPetLevel;
        public Optional<AddOnInfo> TaintedBy;
        public string Name;
        public Array<AuctionListFilterClass> ItemClassFilters = new Array<AuctionListFilterClass>(7);
        public Array<AuctionSortDef> Sorts = new Array<AuctionSortDef>(2);

        public AuctionBrowseQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();
            MinLevel = _worldPacket.ReadUInt8();
            MaxLevel = _worldPacket.ReadUInt8();
            Filters = (AuctionHouseFilterMask)_worldPacket.ReadUInt32();
            uint knownPetSize = _worldPacket.ReadUInt32();
            MaxPetLevel = _worldPacket.ReadInt8();
            for (var i = 0; i < knownPetSize; ++i)
                KnownPets[i] = _worldPacket.ReadUInt8();

            if (_worldPacket.HasBit())
                TaintedBy.HasValue = true;

            uint nameLength = _worldPacket.ReadBits<uint>(8);
            uint itemClassFilterCount = _worldPacket.ReadBits<uint>(3);
            uint sortSize = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortSize; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            Name = _worldPacket.ReadString(nameLength);
            for (var i = 0; i < itemClassFilterCount; ++i)// AuctionListFilterClass filterClass in ItemClassFilters)
                ItemClassFilters[i] = new AuctionListFilterClass(_worldPacket);
        }
    }

    class AuctionCancelCommoditiesPurchase : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public Optional<AddOnInfo> TaintedBy;

        public AuctionCancelCommoditiesPurchase(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            TaintedBy.HasValue = _worldPacket.HasBit();

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionConfirmCommoditiesPurchase : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public int ItemID;
        public uint Quantity;
        public Optional<AddOnInfo> TaintedBy;

        public AuctionConfirmCommoditiesPurchase(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ItemID = _worldPacket.ReadInt32();
            Quantity = _worldPacket.ReadUInt32();
            TaintedBy.HasValue = _worldPacket.HasBit();

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionHelloRequest : ClientPacket
    {
        public ObjectGuid Guid;

        public AuctionHelloRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }
    }

    class AuctionListBidderItems : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint Offset;
        public List<uint> AuctionItemIDs = new List<uint>();
        public Array<AuctionSortDef> Sorts = new Array<AuctionSortDef>(2);
        public Optional<AddOnInfo> TaintedBy;

        public AuctionListBidderItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
             Auctioneer = _worldPacket.ReadPackedGuid();
             Offset = _worldPacket.ReadUInt32();
            TaintedBy.HasValue = _worldPacket.HasBit();

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

    class AuctionListBucketsByBucketKeys : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public Optional<AddOnInfo> TaintedBy;
        public Array<AuctionBucketKey> BucketKeys = new Array<AuctionBucketKey>(100);
        public Array<AuctionSortDef> Sorts = new Array<AuctionSortDef>(2);

        public AuctionListBucketsByBucketKeys(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            TaintedBy.HasValue = _worldPacket.HasBit();

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

    class AuctionListItemsByBucketKey : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint Offset;
        public sbyte Unknown830;
        public Optional<AddOnInfo> TaintedBy;
        public Array<AuctionSortDef> Sorts = new Array<AuctionSortDef>(2);
        public AuctionBucketKey BucketKey;

        public AuctionListItemsByBucketKey(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();
            Unknown830 = _worldPacket.ReadInt8();
            TaintedBy.HasValue = _worldPacket.HasBit();

            uint sortCount = _worldPacket.ReadBits<uint>(2);
            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            BucketKey = new AuctionBucketKey(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionListItemsByItemID : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint ItemID;
        public int SuffixItemNameDescriptionID;
        public uint Offset;
        public Optional<AddOnInfo> TaintedBy;
        public Array<AuctionSortDef> Sorts = new Array<AuctionSortDef>(2);

        public AuctionListItemsByItemID(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ItemID = _worldPacket.ReadUInt32();
            SuffixItemNameDescriptionID = _worldPacket.ReadInt32();
            Offset = _worldPacket.ReadUInt32();

            if (_worldPacket.HasBit())
                TaintedBy.HasValue = true;

            uint sortCount = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionListOwnerItems : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint Offset;
        public Optional<AddOnInfo> TaintedBy;
        public Array<AuctionSortDef> Sorts = new Array<AuctionSortDef>(2);

        public AuctionListOwnerItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();
            TaintedBy.HasValue = _worldPacket.HasBit();

            uint sortCount = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionPlaceBid : ClientPacket
    {   
        public ObjectGuid Auctioneer;
        public ulong BidAmount;
        public uint AuctionID;
        public Optional<AddOnInfo> TaintedBy;

        public AuctionPlaceBid(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            AuctionID = _worldPacket.ReadUInt32();
            BidAmount = _worldPacket.ReadUInt64();
            TaintedBy.HasValue = _worldPacket.HasBit();

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionRemoveItem : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint AuctionID;
        public Optional<AddOnInfo> TaintedBy;

        public AuctionRemoveItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            AuctionID = _worldPacket.ReadUInt32();
            TaintedBy.HasValue = _worldPacket.HasBit();

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionReplicateItems : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public uint ChangeNumberGlobal;
        public uint ChangeNumberCursor;
        public uint ChangeNumberTombstone;
        public uint Count;
        public Optional<AddOnInfo> TaintedBy;

        public AuctionReplicateItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ChangeNumberGlobal = _worldPacket.ReadUInt32();
            ChangeNumberCursor = _worldPacket.ReadUInt32();
            ChangeNumberTombstone = _worldPacket.ReadUInt32();
            Count = _worldPacket.ReadUInt32();
            TaintedBy.HasValue = _worldPacket.HasBit();

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionSellCommodity : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public ulong UnitPrice;
        public uint RunTime;
        public Optional<AddOnInfo> TaintedBy;
        public Array<AuctionItemForSale> Items = new Array<AuctionItemForSale>(64);

        public AuctionSellCommodity(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            UnitPrice = _worldPacket.ReadUInt64();
            RunTime = _worldPacket.ReadUInt32();
            TaintedBy.HasValue = _worldPacket.HasBit();
            uint itemCount = _worldPacket.ReadBits<uint>(6);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            for (var i = 0; i < itemCount; ++i)
                Items[i] = new AuctionItemForSale(_worldPacket);
        }
    }

    class AuctionSellItem : ClientPacket
    {
        public ulong BuyoutPrice;
        public ObjectGuid Auctioneer;
        public ulong MinBid;
        public uint RunTime;
        public Optional<AddOnInfo> TaintedBy;
        public Array<AuctionItemForSale> Items = new Array<AuctionItemForSale>(1);

        public AuctionSellItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            MinBid = _worldPacket.ReadUInt64();
            BuyoutPrice = _worldPacket.ReadUInt64();
            RunTime = _worldPacket.ReadUInt32();

            TaintedBy.HasValue = _worldPacket.HasBit();
            uint itemCount = _worldPacket.ReadBits<uint>(6);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            for (var i = 0; i < itemCount; ++i)
                Items[i] = new AuctionItemForSale(_worldPacket);
        }
    }

    class AuctionSetFavoriteItem : ClientPacket
    {    
        public AuctionFavoriteInfo Item;
        public bool IsNotFavorite = true;

        public AuctionSetFavoriteItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            IsNotFavorite = _worldPacket.HasBit();
            Item = new AuctionFavoriteInfo(_worldPacket);
        }
    }

    class AuctionStartCommoditiesPurchase : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public int ItemID;
        public uint Quantity;
        public Optional<AddOnInfo> TaintedBy;

        public AuctionStartCommoditiesPurchase(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ItemID = _worldPacket.ReadInt32();
            Quantity = _worldPacket.ReadUInt32();
            TaintedBy.HasValue = _worldPacket.HasBit();

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionClosedNotification : ServerPacket
    { 
        public AuctionOwnerNotification Info;
        public float ProceedsMailDelay;
        public bool Sold = true;

        public AuctionClosedNotification() : base(ServerOpcodes.AuctionClosedNotification) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
            _worldPacket.WriteFloat(ProceedsMailDelay);
            _worldPacket.WriteBit(Sold);
            _worldPacket.FlushBits();
        }
    }

    class AuctionCommandResult : ServerPacket
    {   
        public uint AuctionID; ///< the id of the auction that triggered this notification
        public int Command; ///< the type of action that triggered this notification. Possible values are @ref AuctionAction
        public int ErrorCode; ///< the error code that was generated when trying to perform the action. Possible values are @ref AuctionError
        public int BagResult; ///< the bid error. Possible values are @ref AuctionError
        public ObjectGuid Guid;          ///< the GUID of the bidder for this auction.
        public ulong MinIncrement; ///< the sum of outbid is (1% of current bid) * 5, if the bid is too small, then this value is 1 copper.
        public ulong Money; ///< the amount of money that the player bid in copper
        public uint DesiredDelay;

        public AuctionCommandResult() : base(ServerOpcodes.AuctionCommandResult) { }

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

    class AuctionCommodityQuote : ServerPacket
    {
        public Optional<ulong> TotalPrice;
        public Optional<uint> Quantity;
        public Optional<int> QuoteDuration;
        public int Unknown830;
        public uint DesiredDelay;

        public AuctionCommodityQuote() : base(ServerOpcodes.AuctionCommodityQuote) { }

        public override void Write()
        {
            _worldPacket.WriteBit(TotalPrice.HasValue);
            _worldPacket.WriteBit(Quantity.HasValue);
            _worldPacket.WriteBit(QuoteDuration.HasValue);
            _worldPacket.WriteInt32(Unknown830);
            _worldPacket.WriteUInt32(DesiredDelay);

            if (TotalPrice.HasValue)
                _worldPacket.WriteUInt64(TotalPrice.Value);

            if (Quantity.HasValue)
                _worldPacket.WriteUInt32(Quantity.Value);

            if (QuoteDuration.HasValue)
                _worldPacket.WriteInt32(QuoteDuration.Value);
        }
    }

    class AuctionHelloResponse : ServerPacket
    {
        public ObjectGuid Guid;
        public bool OpenForBusiness = true;

        public AuctionHelloResponse() : base(ServerOpcodes.AuctionHelloResponse) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteBit(OpenForBusiness);
            _worldPacket.FlushBits();
        }
    }

    public class AuctionListBidderItemsResult : ServerPacket
    {   
        public List<AuctionItem> Items = new List<AuctionItem>();
        public uint DesiredDelay;
        public bool HasMoreResults;

        public AuctionListBidderItemsResult() : base(ServerOpcodes.AuctionListBidderItemsResult) { }

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
        public List<BucketInfo> Buckets = new List<BucketInfo>();
        public uint DesiredDelay;
        public int Unknown830_0;
        public int Unknown830_1;
        public AuctionHouseBrowseMode BrowseMode;
        public bool HasMoreResults;

        public AuctionListBucketsResult() : base(ServerOpcodes.AuctionListBucketsResult) { }

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

    class AuctionFavoriteItems : ServerPacket
    {    
        public uint DesiredDelay;
        public List<AuctionFavoriteInfo> Items = new List<AuctionFavoriteInfo>();

        public AuctionFavoriteItems() : base(ServerOpcodes.AuctionFavoriteItems) { }

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
        public List<AuctionItem> Items = new List<AuctionItem>();
        public uint Unknown830;
        public uint TotalCount;
        public uint DesiredDelay;
        public AuctionHouseListType ListType;
        public bool HasMoreResults;
        public AuctionBucketKey BucketKey = new AuctionBucketKey();

        public AuctionListItemsResult() : base(ServerOpcodes.AuctionListItemsResult) { }

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

    public class AuctionListOwnerItemsResult : ServerPacket
    {   
        public List<AuctionItem> Items = new List<AuctionItem>();
        public List<AuctionItem> SoldItems = new List<AuctionItem>();
        public uint DesiredDelay;
        public bool HasMoreResults;

        public AuctionListOwnerItemsResult() : base(ServerOpcodes.AuctionListOwnerItemsResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteInt32(SoldItems.Count);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteBit(HasMoreResults);
            _worldPacket.FlushBits();

            foreach (AuctionItem item in Items)
                item.Write(_worldPacket);
        }
    }

    class AuctionOutbidNotification : ServerPacket
    {    
        public AuctionBidderNotification Info;
        public ulong BidAmount;
        public ulong MinIncrement;

        public AuctionOutbidNotification() : base(ServerOpcodes.AuctionOutbidNotification) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
            _worldPacket.WriteUInt64(BidAmount);
            _worldPacket.WriteUInt64(MinIncrement);
        }
    }

    class AuctionOwnerBidNotification : ServerPacket
    {    
        public AuctionOwnerNotification Info;
        public ObjectGuid Bidder;
        public ulong MinIncrement;

        public AuctionOwnerBidNotification() : base(ServerOpcodes.AuctionOwnerBidNotification) { }

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
        public uint DesiredDelay;
        public uint ChangeNumberTombstone;
        public uint Result;
        public List<AuctionItem> Items = new List<AuctionItem>();

        public AuctionReplicateResponse() : base(ServerOpcodes.AuctionReplicateResponse) { }

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

    class AuctionWonNotification : ServerPacket
    {  
        public AuctionBidderNotification Info;

        public AuctionWonNotification() : base(ServerOpcodes.AuctionWonNotification) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
        }
    }

    //Structs
    public class AuctionBucketKey
    {
        public uint ItemID;
        public ushort ItemLevel;
        public Optional<ushort> BattlePetSpeciesID = new Optional<ushort>();
        public Optional<ushort> SuffixItemNameDescriptionID = new Optional<ushort>();

        public AuctionBucketKey() { }

        public AuctionBucketKey(AuctionsBucketKey key)
        {
            ItemID = key.ItemId;
            ItemLevel = key.ItemLevel;

            if (key.BattlePetSpeciesId != 0)
                BattlePetSpeciesID.Set(key.BattlePetSpeciesId);

            if (key.SuffixItemNameDescriptionId != 0)
                SuffixItemNameDescriptionID.Set(key.SuffixItemNameDescriptionId);
        }

        public AuctionBucketKey(WorldPacket data)
        {
            data.ResetBitPos();
            ItemID = data.ReadBits<uint>(20);

            if (data.HasBit())
                BattlePetSpeciesID.HasValue = true;

            ItemLevel = data.ReadBits<ushort>(11);

            if (data.HasBit())
                SuffixItemNameDescriptionID.HasValue = true;

            if (BattlePetSpeciesID.HasValue)
                BattlePetSpeciesID.Set(data.ReadUInt16());

            if (SuffixItemNameDescriptionID.HasValue)
                SuffixItemNameDescriptionID.Set(data.ReadUInt16());
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
        public uint InvTypeMask;

        public AuctionListFilterSubClass(WorldPacket data)
        {
            ItemSubclass = data.ReadInt32();
            InvTypeMask = data.ReadUInt32();
        }
    }

    public class AuctionListFilterClass
    {
        public int ItemClass;
        public Array<AuctionListFilterSubClass> SubClassFilters = new Array<AuctionListFilterSubClass>(31);

        public AuctionListFilterClass(WorldPacket data)
        {
            ItemClass = data.ReadInt32();
            uint subClassFilterCount = data.ReadBits<uint>(5);

            for (var i =  0; i < subClassFilterCount; ++i)
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
        public AuctionBucketKey Key;
        public int TotalQuantity;
        public ulong MinPrice;
        public List<uint> ItemModifiedAppearanceIDs = new List<uint>();
        public Optional<byte> MaxBattlePetQuality;
        public Optional<byte> MaxBattlePetLevel;
        public Optional<byte> BattlePetBreedID;
        public bool ContainsOwnerItem;
        public bool ContainsOnlyCollectedAppearances;

        public void Write(WorldPacket data)
        {
            Key.Write(data);
            data.WriteInt32(TotalQuantity);
            data.WriteUInt64(MinPrice);
            data.WriteInt32(ItemModifiedAppearanceIDs.Count);
            if (!ItemModifiedAppearanceIDs.Empty())
            {
                foreach (int id in ItemModifiedAppearanceIDs)
                    data.WriteInt32(id);
            }

            data.WriteBit(MaxBattlePetQuality.HasValue);
            data.WriteBit(MaxBattlePetLevel.HasValue);
            data.WriteBit(BattlePetBreedID.HasValue);
            data.WriteBit(ContainsOwnerItem);
            data.WriteBit(ContainsOnlyCollectedAppearances);
            data.FlushBits();

            if (MaxBattlePetQuality.HasValue)
                data.WriteUInt8(MaxBattlePetQuality.Value);

            if (MaxBattlePetLevel.HasValue)
                data.WriteUInt8(MaxBattlePetLevel.Value);

            if (BattlePetBreedID.HasValue)
                data.WriteUInt8(BattlePetBreedID.Value);
        }
    }

    public class AuctionItem
    {
        public Optional<ItemInstance> Item;
        public int Count;
        public int Charges;
        public List<ItemEnchantData> Enchantments = new List<ItemEnchantData>();
        public uint Flags;
        public uint AuctionID;
        public ObjectGuid Owner;
        public Optional<ulong> MinBid;
        public Optional<ulong> MinIncrement;
        public Optional<ulong> BuyoutPrice;
        public Optional<ulong> UnitPrice;
        public int DurationLeft;
        public byte DeleteReason;
        public bool CensorServerSideInfo;
        public bool CensorBidInfo;
        public ObjectGuid ItemGuid;
        public ObjectGuid OwnerAccountID;
        public uint EndTime;
        public Optional<ObjectGuid> Bidder;
        public Optional<ulong> BidAmount;
        public List<ItemGemData> Gems = new List<ItemGemData>();
        public Optional<AuctionBucketKey> AuctionBucketKey;

        public void Write(WorldPacket data)
        {
            data.WriteBit(Item.HasValue);
            data.WriteBits(Enchantments.Count, 4);
            data.WriteBits(Gems.Count, 2);
            data.WriteBit(MinBid.HasValue);
            data.WriteBit(MinIncrement.HasValue);
            data.WriteBit(BuyoutPrice.HasValue);
            data.WriteBit(UnitPrice.HasValue);
            data.WriteBit(CensorServerSideInfo);
            data.WriteBit(CensorBidInfo);
            data.WriteBit(AuctionBucketKey.HasValue);
            if (!CensorBidInfo)
            {
                data.WriteBit(Bidder.HasValue);
                data.WriteBit(BidAmount.HasValue);
            }

            data.FlushBits();

            if (Item.HasValue)
                Item.Value.Write(data);

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

            if (!CensorBidInfo)
            {
                if (Bidder.HasValue)
                    data.WritePackedGuid(Bidder.Value);

                if (BidAmount.HasValue)
                    data.WriteUInt64(BidAmount.Value);
            }

            foreach (ItemGemData gem in Gems)
                gem.Write(data);

            if (AuctionBucketKey.HasValue)
                AuctionBucketKey.Value.Write(data);
        }
    }

    struct AuctionBidderNotification
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
