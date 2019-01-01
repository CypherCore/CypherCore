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
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class AuctionHelloRequest : ClientPacket
    {
        public AuctionHelloRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    class AuctionHelloResponse : ServerPacket
    {
        public AuctionHelloResponse() : base(ServerOpcodes.AuctionHelloResponse) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteBit(OpenForBusiness);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Guid;
        public bool OpenForBusiness = true;
    }

    class AuctionCommandResult : ServerPacket
    {
        public AuctionCommandResult() : base(ServerOpcodes.AuctionCommandResult) { }

        public void InitializeAuction(AuctionEntry auction)
        {
            if (auction != null)
            {
                AuctionItemID = auction.Id;
                Money = auction.bid == auction.buyout ? 0 : auction.bid;
                MinIncrement = auction.bid == auction.buyout ? 0 : auction.GetAuctionOutBid();
                Guid = ObjectGuid.Create(HighGuid.Player, auction.bidder);
            }
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AuctionItemID);
            _worldPacket.WriteInt32(Command);
            _worldPacket.WriteInt32(ErrorCode);
            _worldPacket.WriteInt32(BagResult);
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt64(MinIncrement);
            _worldPacket.WriteUInt64(Money);
        }

        public uint AuctionItemID; // the id of the auction that triggered this notification
        public AuctionAction Command;
        public AuctionError ErrorCode;
        public ulong Money; // the amount of money that the player bid in copper
        public AuctionError BagResult;
        public ObjectGuid Guid; // the GUID of the bidder for this auction.
        public ulong MinIncrement; // the sum of outbid is (1% of current bid) * 5, if the bid is too small, then this value is 1 copper.
    }

    class AuctionSellItem : ClientPacket
    {
        public AuctionSellItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            MinBid = _worldPacket.ReadUInt64();
            BuyoutPrice = _worldPacket.ReadUInt64();
            RunTime = _worldPacket.ReadUInt32();

            byte ItemsCount = _worldPacket.ReadBits<byte>(5);
            _worldPacket.ResetBitPos();

            for (byte i = 0; i < ItemsCount; i++)
            {
                AuctionItemForSale item;
                item.Guid = _worldPacket.ReadPackedGuid();
                item.UseCount = _worldPacket.ReadUInt32();
                Items[i] = item;
            }
        }

        public ulong BuyoutPrice;
        public ObjectGuid Auctioneer;
        public ulong MinBid;
        public uint RunTime;
        public Array<AuctionItemForSale> Items = new Array<AuctionItemForSale>(32);

        public struct AuctionItemForSale
        {
            public ObjectGuid Guid;
            public uint UseCount;
        }
    }

    class AuctionPlaceBid : ClientPacket
    {
        public AuctionPlaceBid(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            AuctionItemID = _worldPacket.ReadUInt32();
            BidAmount = _worldPacket.ReadUInt64();
        }

        public ObjectGuid Auctioneer;
        public ulong BidAmount;
        public uint AuctionItemID;
    }

    class AuctionListBidderItems : ClientPacket
    {
        public AuctionListBidderItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();
            byte auctionItemIDsCount = _worldPacket.ReadBits<byte>(7);
            _worldPacket.ResetBitPos();

            for (byte i = 0; i < auctionItemIDsCount; i++)
            {
                uint AuctionItemID = _worldPacket.ReadUInt32();
                AuctionItemIDs.Add(AuctionItemID);
            }
        }

        public uint Offset;
        public List<uint> AuctionItemIDs = new List<uint>();
        public ObjectGuid Auctioneer;
    }

    class AuctionRemoveItem : ClientPacket
    {
        public AuctionRemoveItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            AuctionItemID = _worldPacket.ReadInt32();
        }

        public ObjectGuid Auctioneer;
        public int AuctionItemID;
    }

    class AuctionReplicateItems : ClientPacket
    {
        public AuctionReplicateItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Count = _worldPacket.ReadUInt32();
            ChangeNumberGlobal = _worldPacket.ReadUInt32();
            ChangeNumberCursor = _worldPacket.ReadUInt32();
            ChangeNumberTombstone = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Auctioneer;
        public uint Count;
        public uint ChangeNumberGlobal;
        public uint ChangeNumberCursor;
        public uint ChangeNumberTombstone;
    }

    class AuctionListPendingSales : ClientPacket
    {
        public AuctionListPendingSales(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class AuctionListItemsResult : ServerPacket
    {
        public AuctionListItemsResult() : base(ServerOpcodes.AuctionListItemsResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteInt32(TotalCount);
            _worldPacket.WriteInt32(DesiredDelay);
            _worldPacket.WriteBit(OnlyUsable);
            _worldPacket.FlushBits();

            foreach (var item in Items)
                item.Write(_worldPacket);
        }

        public uint DesiredDelay;
        public List<AuctionItem> Items = new List<AuctionItem>();
        public bool OnlyUsable = true;
        public uint TotalCount;
    }

    public class AuctionListOwnerItemsResult : ServerPacket
    {
        public AuctionListOwnerItemsResult() : base(ServerOpcodes.AuctionListOwnerItemsResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteUInt32(TotalCount);
            _worldPacket.WriteUInt32(DesiredDelay);

            foreach (var item in Items)
                item.Write(_worldPacket);
        }

        public uint DesiredDelay;
        public uint TotalCount;
        public List<AuctionItem> Items = new List<AuctionItem>();
    }

    public class AuctionListBidderItemsResult : ServerPacket
    {
        public AuctionListBidderItemsResult() : base(ServerOpcodes.AuctionListBidderItemsResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Items.Count);
            _worldPacket.WriteUInt32(TotalCount);
            _worldPacket.WriteUInt32(DesiredDelay);

            foreach (var item in Items)
                item.Write(_worldPacket);
        }

        public uint DesiredDelay;
        public uint TotalCount;
        public List<AuctionItem> Items = new List<AuctionItem>();
    }

    class AuctionListOwnerItems : ClientPacket
    {
        public AuctionListOwnerItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Auctioneer;
        public uint Offset;
    }

    class AuctionListItems : ClientPacket
    {
        public AuctionListItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Offset = _worldPacket.ReadUInt32();
            Auctioneer = _worldPacket.ReadPackedGuid();
            MinLevel = _worldPacket.ReadUInt8();
            MaxLevel = _worldPacket.ReadUInt8();
            Quality = _worldPacket.ReadUInt32();
            SortCount = _worldPacket.ReadUInt8();
            KnownPets = new Array<byte>(_worldPacket.ReadInt32());
            for (int i = 0; i < KnownPets.Capacity; ++i)
                KnownPets[i] = _worldPacket.ReadUInt8();

            Name = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(8));
            ClassFilters = new Array<ClassFilter>(_worldPacket.ReadBits<int>(3));
            OnlyUsable = _worldPacket.HasBit();
            ExactMatch = _worldPacket.HasBit();

            for (int i = 0; i < ClassFilters.Capacity; ++i)
            {
                var classFilter = new ClassFilter();
                classFilter.ItemClass = _worldPacket.ReadInt32();
                classFilter.SubClassFilters = new Array<ClassFilter.SubClassFilter>(_worldPacket.ReadBits<int>(5));
                for (int x = 0; x < classFilter.SubClassFilters.Capacity; ++x)
                {
                    ClassFilter.SubClassFilter subClassFilter;
                    subClassFilter.ItemSubclass = _worldPacket.ReadInt32();
                    subClassFilter.InvTypeMask = _worldPacket.ReadUInt32();
                    classFilter.SubClassFilters[x] = subClassFilter;
                }
                ClassFilters[i] = classFilter;
            }

            _worldPacket.Skip(4); // DataSize = (SortCount * 2)
            for (int i = 0; i < SortCount; i++)
            {
                AuctionListItems.Sort sort;
                sort.Type = _worldPacket.ReadUInt8();
                sort.Direction = _worldPacket.ReadUInt8();
                DataSort.Add(sort);
            }
        }

        public uint Offset;
        public ObjectGuid Auctioneer;
        public byte MinLevel = 1;
        public byte MaxLevel = 100;
        public uint Quality;
        public byte SortCount;
        public Array<byte> KnownPets;
        public sbyte MaxPetLevel;
        public string Name = "";
        public Array<ClassFilter> ClassFilters = new Array<ClassFilter>(7);
        public bool ExactMatch = true;
        public bool OnlyUsable;
        public List<Sort> DataSort = new List<Sort>();

        public struct Sort
        {
            public byte Type;
            public byte Direction;
        }

        public class ClassFilter
        {
            public struct SubClassFilter
            {
                public int ItemSubclass;
                public uint InvTypeMask;
            }

            public int ItemClass;
            public Array<SubClassFilter> SubClassFilters = new Array<SubClassFilter>(31);
        }
    }

    class AuctionListPendingSalesResult : ServerPacket
    {
        public AuctionListPendingSalesResult() : base(ServerOpcodes.AuctionListPendingSalesResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Mails.Count);
            _worldPacket.WriteInt32(TotalNumRecords);

            foreach (var mail in Mails)
                mail.Write(_worldPacket);
        }

        public List<MailListEntry> Mails = new List<MailListEntry>();
        public int TotalNumRecords;
    }

    class AuctionClosedNotification : ServerPacket
    {
        public AuctionClosedNotification() : base(ServerOpcodes.AuctionClosedNotification) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
            _worldPacket.WriteFloat(ProceedsMailDelay);
            _worldPacket.WriteBit(Sold);
            _worldPacket.FlushBits();
        }

        public AuctionOwnerNotification Info;
        public float ProceedsMailDelay;
        public bool Sold = true;
    }

    class AuctionOwnerBidNotification : ServerPacket
    {
        public AuctionOwnerBidNotification() : base(ServerOpcodes.AuctionOwnerBidNotification) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
            _worldPacket.WriteUInt64(MinIncrement);
            _worldPacket.WritePackedGuid(Bidder);
        }

        public AuctionOwnerNotification Info;
        public ObjectGuid Bidder;
        public ulong MinIncrement;
    }

    class AuctionWonNotification : ServerPacket
    {
        public AuctionWonNotification() : base(ServerOpcodes.AuctionWonNotification) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
        }

        public AuctionBidderNotification Info;
    }

    class AuctionOutBidNotification : ServerPacket
    {
        public AuctionOutBidNotification() : base(ServerOpcodes.AuctionOutbidNotification) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
            _worldPacket.WriteUInt64(BidAmount);
            _worldPacket.WriteUInt64(MinIncrement);
        }

        public AuctionBidderNotification Info;
        public ulong BidAmount;
        public ulong MinIncrement;
    }

    class AuctionReplicateResponse : ServerPacket
    {
        public AuctionReplicateResponse() : base(ServerOpcodes.AuctionReplicateResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(DesiredDelay);
            _worldPacket.WriteUInt32(ChangeNumberGlobal);
            _worldPacket.WriteUInt32(ChangeNumberCursor);
            _worldPacket.WriteUInt32(ChangeNumberTombstone);
            _worldPacket.WriteUInt32(Items.Count);

            foreach (var item in Items)
                item.Write(_worldPacket);
        }

        public uint ChangeNumberCursor;
        public uint ChangeNumberGlobal;
        public uint DesiredDelay;
        public uint ChangeNumberTombstone;
        public uint Result;
        public List<AuctionItem> Items = new List<AuctionItem>();
    }

    public class AuctionItem
    {
        public void Write(WorldPacket data)
        {
            Item.Write(data);
            data.WriteInt32(Count);
            data.WriteInt32(Charges);
            data.WriteInt32(Flags);
            data.WriteInt32(AuctionItemID);
            data.WritePackedGuid(Owner);
            data.WriteUInt64(MinBid);
            data.WriteUInt64(MinIncrement);
            data.WriteUInt64(BuyoutPrice);
            data.WriteInt32(DurationLeft);
            data.WriteUInt8(DeleteReason);
            data.WriteBits(Enchantments.Count, 4);
            data.WriteBits(Gems.Count, 2);
            data.WriteBit(CensorServerSideInfo);
            data.WriteBit(CensorBidInfo);
            data.FlushBits();

            foreach (ItemGemData gem in Gems)
                gem.Write(data);

            foreach (ItemEnchantData enchant in Enchantments)
                enchant.Write(data);

            if (!CensorServerSideInfo)
            {
                data.WritePackedGuid(ItemGuid);
                data.WritePackedGuid(OwnerAccountID);
                data.WriteInt32(EndTime);
            }

            if (!CensorBidInfo)
            {
                data.WritePackedGuid(Bidder);
                data.WriteUInt64(BidAmount);
            }
        }

        public ItemInstance Item;
        public int Count;
        public int Charges;
        public List<ItemEnchantData> Enchantments = new List<ItemEnchantData>();
        public int Flags;
        public int AuctionItemID;
        public ObjectGuid Owner;
        public ulong MinBid;
        public ulong MinIncrement;
        public ulong BuyoutPrice;
        public int DurationLeft;
        public byte DeleteReason;
        public bool CensorServerSideInfo;
        public bool CensorBidInfo;
        public ObjectGuid ItemGuid;
        public ObjectGuid OwnerAccountID;
        public uint EndTime;
        public ObjectGuid Bidder;
        public ulong BidAmount;
        public List<ItemGemData> Gems = new List<ItemGemData>();
    }

    struct AuctionOwnerNotification
    {
        public void Initialize(AuctionEntry auction, Item item)
        {
            AuctionItemID = (int)auction.Id;
            Item = new ItemInstance(item);
            BidAmount = auction.bid;
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(AuctionItemID);
            data.WriteUInt64(BidAmount);
            Item.Write(data);
        }

        public int AuctionItemID;
        public ulong BidAmount;
        public ItemInstance Item;
    }

    struct AuctionBidderNotification
    {
        public void Initialize(AuctionEntry auction, Item item)
        {
            AuctionItemID = (int)auction.Id;
            Item = new ItemInstance(item);
            Bidder = ObjectGuid.Create(HighGuid.Player, auction.bidder);
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(AuctionItemID);
            data.WritePackedGuid(Bidder);
            Item.Write(data);
        }

        public int AuctionItemID;
        public ObjectGuid Bidder;
        public ItemInstance Item;
    }
}
