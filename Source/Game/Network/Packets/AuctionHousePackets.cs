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
            Offset = _worldPacket.ReadUInt8();
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
        ObjectGuid Auctioneer;
        int ItemID;
        uint Quantity;
        Optional<AddOnInfo> TaintedBy;

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

            BucketKey = new AuctionBucketKey();
            BucketKey.Read(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionListItemsByItemID : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public int ItemID;
        public int SuffixItemNameDescriptionID;
        public uint Offset;
        public Optional<AddOnInfo> TaintedBy;
        public Array<AuctionSortDef> Sorts = new Array<AuctionSortDef>(2);

        public AuctionListItemsByItemID(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            ItemID = _worldPacket.ReadInt32();
            SuffixItemNameDescriptionID = _worldPacket.ReadInt32();
            Offset = _worldPacket.ReadUInt32();
            TaintedBy.HasValue = _worldPacket.HasBit();

            uint sortCount = _worldPacket.ReadBits<uint>(2);
            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);
        }
    }

    class AuctionListItemsByItemKeys : ClientPacket
    {
        public ObjectGuid Auctioneer;
        public Optional<AddOnInfo> TaintedBy;
        public Array<AuctionBucketKey> BucketKeys = new Array<AuctionBucketKey>(100);
        public Array<AuctionSortDef> Sorts = new Array<AuctionSortDef>(2);

        public AuctionListItemsByItemKeys(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();

            if (_worldPacket.HasBit())
                TaintedBy.HasValue = true;

            uint bucketKeyCount = _worldPacket.ReadBits<uint>(7);
            uint sortCount = _worldPacket.ReadBits<uint>(2);

            for (var i = 0; i < sortCount; ++i)
                Sorts[i] = new AuctionSortDef(_worldPacket);

            if (TaintedBy.HasValue)
                TaintedBy.Value.Read(_worldPacket);

            for (var i = 0; i < bucketKeyCount; ++i)
            {
                BucketKeys[i] = new AuctionBucketKey();
                BucketKeys[i].Read(_worldPacket);
            }
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
        public int AuctionID = 0;
        public Optional<AddOnInfo> TaintedBy;

        public AuctionRemoveItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            AuctionID = _worldPacket.ReadInt32();
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
        public Array<AuctionItemForSale> Items = new Array<AuctionItemForSale>(64);

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

        /**
         * @fn  void WorldPackets::AuctionHousePackets::AuctionCommandResult::InitializeAuction(AuctionEntry* auction);
         *
         * @brief   Initialize the following fields: AuctionId, Bid, AuctionOutBid, Bidder
         *
         * @param   auction         The relevant auction object
         */
        public void InitializeAuction(AuctionEntry auction)
        {
            if (auction != null)
            {
                AuctionID = auction.Id;
                Money = auction.bid == auction.buyout ? 0 : auction.bid;
                MinIncrement = auction.bid == auction.buyout ? 0 : auction.GetAuctionOutBid();
                Guid = ObjectGuid.Create(HighGuid.Player, auction.bidder);
            }
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

    class AuctionCommodityPriceUpdate : ServerPacket
    {
        public Optional<ulong> TotalPrice;
        public Optional<uint> Quantity;
        public Optional<int> QuoteDuration;
        public int Unknown830;
        public uint DesiredDelay;

        public AuctionCommodityPriceUpdate() : base(ServerOpcodes.AuctionCommodityPriceUpdate) { }

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

    class AuctionListBucketItemsResult : ServerPacket
    {  
        public List<BucketInfo> Buckets = new List<BucketInfo>();
        public uint DesiredDelay;
        public int Unknown830_0;
        public int Unknown830_1;
        public int BrowseMode;
        public bool HasMoreResults;

        public AuctionListBucketItemsResult() : base(ServerOpcodes.AuctionListBucketItemsResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Buckets.Count);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteInt32(Unknown830_0);
            _worldPacket.WriteInt32(Unknown830_1);
            _worldPacket.WriteBits(BrowseMode, 1);
            _worldPacket.WriteBit(HasMoreResults);
            _worldPacket.FlushBits();

            foreach (BucketInfo bucketInfo in Buckets)
                bucketInfo.Write(_worldPacket);
        }
    }

    class AuctionListFavoriteItemsResult : ServerPacket
    {    
        public uint DesiredDelay;
        public List<AuctionFavoriteInfo> Items = new List<AuctionFavoriteInfo>();

        public AuctionListFavoriteItemsResult() : base(ServerOpcodes.AuctionListFavoriteItemsResult) { }

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
        public uint ListType;
        public bool HasMoreResults;
        public AuctionBucketKey BucketKey;

        public AuctionListItemsResult() : base(ServerOpcodes.AuctionListItemsResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteUInt32(Unknown830);
            _worldPacket.WriteUInt32(TotalCount);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteBits(ListType, 2);
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

    class AuctionReplicateResponse : ServerPacket
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

        public void Read(WorldPacket data)
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
        public uint ItemSuffix;

        public AuctionFavoriteInfo(WorldPacket data)
        {
            Order = data.ReadUInt32();
            ItemID = data.ReadUInt32();
            ItemLevel = data.ReadUInt32();
            BattlePetSpeciesID = data.ReadUInt32();
            ItemSuffix = data.ReadUInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Order);
            data.WriteUInt32(ItemID);
            data.WriteUInt32(ItemLevel);
            data.WriteUInt32(BattlePetSpeciesID);
            data.WriteUInt32(ItemSuffix);
        }
    }

    public struct AuctionOwnerNotification
    {
        public uint AuctionID;
        public ulong BidAmount;
        public ItemInstance Item;

        public void Initialize(AuctionEntry auction, Item item)
        {
            AuctionID = auction.Id;
            Item = new ItemInstance(item);
            BidAmount = auction.bid;
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
        public List<int> ItemModifiedAppearanceIDs = new List<int>();
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
        public int Flags;
        public int AuctionID;
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
            data.WriteInt32(Flags);
            data.WriteInt32(AuctionID);
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
        public int AuctionID;
        public ObjectGuid Bidder;
        public ItemInstance Item;

        public void Initialize(AuctionEntry auction, Item item)
        {
            AuctionID = (int)auction.Id;
            Item = new ItemInstance(item);
            Bidder = ObjectGuid.Create(HighGuid.Player, auction.bidder);
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(AuctionID);
            data.WritePackedGuid(Bidder);
            Item.Write(data);
        }
    }
}
