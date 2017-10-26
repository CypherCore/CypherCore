/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

        public ObjectGuid Guid { get; set; }
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

        public ObjectGuid Guid { get; set; }
        public bool OpenForBusiness { get; set; } = true;
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
        public AuctionAction Command { get; set; }
        public AuctionError ErrorCode { get; set; }
        public ulong Money; // the amount of money that the player bid in copper
        public AuctionError BagResult { get; set; }
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
                AuctionItemForSale item = new AuctionItemForSale();
                item.Guid = _worldPacket.ReadPackedGuid();
                item.UseCount = _worldPacket.ReadUInt32();
                Items.Add(item);
            }
        }

        public ulong BuyoutPrice { get; set; }
        public ObjectGuid Auctioneer { get; set; }
        public ulong MinBid { get; set; }
        public uint RunTime { get; set; }
        public Array<AuctionItemForSale> Items { get; set; } = new Array<AuctionItemForSale>(32);

        public struct AuctionItemForSale
        {
            public ObjectGuid Guid { get; set; }
            public uint UseCount { get; set; }
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

        public ObjectGuid Auctioneer { get; set; }
        public ulong BidAmount { get; set; }
        public uint AuctionItemID { get; set; }
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

        public uint Offset { get; set; }
        public List<uint> AuctionItemIDs { get; set; } = new List<uint>();
        public ObjectGuid Auctioneer { get; set; }
    }

    class AuctionRemoveItem : ClientPacket
    {
        public AuctionRemoveItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            AuctionItemID = _worldPacket.ReadInt32();
        }

        public ObjectGuid Auctioneer { get; set; }
        public int AuctionItemID { get; set; }
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

        public ObjectGuid Auctioneer { get; set; }
        public uint Count { get; set; }
        public uint ChangeNumberGlobal { get; set; }
        public uint ChangeNumberCursor { get; set; }
        public uint ChangeNumberTombstone { get; set; }
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

        public uint DesiredDelay { get; set; }
        public List<AuctionItem> Items { get; set; } = new List<AuctionItem>();
        public bool OnlyUsable { get; set; } = true;
        public uint TotalCount { get; set; }
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

        public uint DesiredDelay { get; set; }
        public uint TotalCount;
        public List<AuctionItem> Items { get; set; } = new List<AuctionItem>();
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

        public uint DesiredDelay { get; set; }
        public uint TotalCount;
        public List<AuctionItem> Items { get; set; } = new List<AuctionItem>();
    }

    class AuctionListOwnerItems : ClientPacket
    {
        public AuctionListOwnerItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Auctioneer = _worldPacket.ReadPackedGuid();
            Offset = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Auctioneer { get; set; }
        public uint Offset { get; set; }
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
                    ClassFilter.SubClassFilter subClassFilter = new ClassFilter.SubClassFilter();
                    subClassFilter.ItemSubclass = _worldPacket.ReadInt32();
                    subClassFilter.InvTypeMask = _worldPacket.ReadUInt32();
                    classFilter.SubClassFilters.Add(subClassFilter);
                }
                ClassFilters.Add(classFilter);
            }

            _worldPacket.Skip(4); // DataSize = (SortCount * 2)
            for (int i = 0; i < SortCount; i++)
            {
                AuctionListItems.Sort sort = new Sort();
                sort.Type = _worldPacket.ReadUInt8();
                sort.Direction = _worldPacket.ReadUInt8();
                DataSort.Add(sort);
            }
        }

        public uint Offset { get; set; }
        public ObjectGuid Auctioneer { get; set; }
        public byte MinLevel { get; set; } = 1;
        public byte MaxLevel { get; set; } = 100;
        public uint Quality { get; set; }
        public byte SortCount { get; set; }
        public Array<byte> KnownPets { get; set; }
        public sbyte MaxPetLevel { get; set; }
        public string Name { get; set; } = "";
        public Array<ClassFilter> ClassFilters { get; set; } = new Array<ClassFilter>(7);
        public bool ExactMatch { get; set; } = true;
        public bool OnlyUsable { get; set; }
        public List<Sort> DataSort { get; set; } = new List<Sort>();

        public struct Sort
        {
            public byte Type { get; set; }
            public byte Direction { get; set; }
        }

        public class ClassFilter
        {
            public struct SubClassFilter
            {
                public int ItemSubclass { get; set; }
                public uint InvTypeMask { get; set; }
            }

            public int ItemClass { get; set; }
            public Array<SubClassFilter> SubClassFilters { get; set; } = new Array<SubClassFilter>(31);
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

        public List<MailListEntry> Mails { get; set; } = new List<MailListEntry>();
        public int TotalNumRecords { get; set; }
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

        public AuctionOwnerNotification Info { get; set; }
        public float ProceedsMailDelay { get; set; }
        public bool Sold { get; set; } = true;
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

        public AuctionOwnerNotification Info { get; set; }
        public ObjectGuid Bidder { get; set; }
        public ulong MinIncrement { get; set; }
    }

    class AuctionWonNotification : ServerPacket
    {
        public AuctionWonNotification() : base(ServerOpcodes.AuctionWonNotification) { }

        public override void Write()
        {
            Info.Write(_worldPacket);
        }

        public AuctionBidderNotification Info { get; set; }
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

        public AuctionBidderNotification Info { get; set; }
        public ulong BidAmount { get; set; }
        public ulong MinIncrement { get; set; }
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

        public uint ChangeNumberCursor { get; set; }
        public uint ChangeNumberGlobal { get; set; }
        public uint DesiredDelay { get; set; }
        public uint ChangeNumberTombstone { get; set; }
        public uint Result { get; set; }
        public List<AuctionItem> Items { get; set; } = new List<AuctionItem>();
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

        public ItemInstance Item { get; set; }
        public int Count { get; set; }
        public int Charges { get; set; }
        public List<ItemEnchantData> Enchantments { get; set; } = new List<ItemEnchantData>();
        public int Flags { get; set; }
        public int AuctionItemID { get; set; }
        public ObjectGuid Owner { get; set; }
        public ulong MinBid { get; set; }
        public ulong MinIncrement { get; set; }
        public ulong BuyoutPrice { get; set; }
        public int DurationLeft { get; set; }
        public byte DeleteReason { get; set; }
        public bool CensorServerSideInfo { get; set; }
        public bool CensorBidInfo { get; set; }
        public ObjectGuid ItemGuid { get; set; }
        public ObjectGuid OwnerAccountID { get; set; }
        public uint EndTime { get; set; }
        public ObjectGuid Bidder { get; set; }
        public ulong BidAmount { get; set; }
        public List<ItemGemData> Gems { get; set; } = new List<ItemGemData>();
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

        public int AuctionItemID { get; set; }
        public ulong BidAmount { get; set; }
        public ItemInstance Item { get; set; }
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

        public int AuctionItemID { get; set; }
        public ObjectGuid Bidder { get; set; }
        public ItemInstance Item { get; set; }
    }
}
