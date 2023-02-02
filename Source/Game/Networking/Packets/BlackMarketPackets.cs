// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class BlackMarketOpen : ClientPacket
    {
        public BlackMarketOpen(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    class BlackMarketRequestItems : ClientPacket
    {
        public BlackMarketRequestItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            LastUpdateID = _worldPacket.ReadInt64();
        }

        public ObjectGuid Guid;
        public long LastUpdateID;
    }

    public class BlackMarketRequestItemsResult : ServerPacket
    {
        public BlackMarketRequestItemsResult() : base(ServerOpcodes.BlackMarketRequestItemsResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt64(LastUpdateID);
            _worldPacket.WriteInt32(Items.Count);

            foreach (BlackMarketItem item in Items)
                item.Write(_worldPacket);
        }

        public long LastUpdateID;
        public List<BlackMarketItem> Items = new();
    }

    class BlackMarketBidOnItem : ClientPacket
    {
        public BlackMarketBidOnItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            MarketID = _worldPacket.ReadUInt32();
            BidAmount = _worldPacket.ReadUInt64();
            Item.Read(_worldPacket);
        }

        public ObjectGuid Guid;
        public uint MarketID;
        public ItemInstance Item = new();
        public ulong BidAmount;
    }

    class BlackMarketBidOnItemResult : ServerPacket
    {
        public BlackMarketBidOnItemResult() : base(ServerOpcodes.BlackMarketBidOnItemResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MarketID);
            _worldPacket.WriteUInt32((uint)Result);
            Item.Write(_worldPacket);
        }

        public uint MarketID;
        public ItemInstance Item;
        public BlackMarketError Result;
    }

    class BlackMarketOutbid : ServerPacket
    {
        public BlackMarketOutbid() : base(ServerOpcodes.BlackMarketOutbid) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MarketID);
            _worldPacket.WriteUInt32(RandomPropertiesID);
            Item.Write(_worldPacket);
        }

        public uint MarketID;
        public ItemInstance Item;
        public uint RandomPropertiesID;
    }

    class BlackMarketWon : ServerPacket
    {
        public BlackMarketWon() : base(ServerOpcodes.BlackMarketWon) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MarketID);
            _worldPacket.WriteInt32(RandomPropertiesID);
            Item.Write(_worldPacket);
        }

        public uint MarketID;
        public ItemInstance Item;
        public int RandomPropertiesID;
    }

    public struct BlackMarketItem
    {
        public void Read(WorldPacket data)
        {
            MarketID = data.ReadUInt32();
            SellerNPC = data.ReadUInt32();
            Item.Read(data);
            Quantity = data.ReadUInt32();
            MinBid = data.ReadUInt64();
            MinIncrement = data.ReadUInt64();
            CurrentBid = data.ReadUInt64();
            SecondsRemaining = data.ReadUInt32();
            NumBids = data.ReadUInt32();
            HighBid = data.HasBit();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(MarketID);
            data.WriteUInt32(SellerNPC);
            data.WriteUInt32(Quantity);
            data.WriteUInt64(MinBid);
            data.WriteUInt64(MinIncrement);
            data.WriteUInt64(CurrentBid);
            data.WriteUInt32(SecondsRemaining);
            data.WriteUInt32(NumBids);
            Item.Write(data);
            data.WriteBit(HighBid);
            data.FlushBits();
        }

        public uint MarketID;
        public uint SellerNPC;
        public ItemInstance Item;
        public uint Quantity;
        public ulong MinBid;
        public ulong MinIncrement;
        public ulong CurrentBid;
        public uint SecondsRemaining;
        public uint NumBids;
        public bool HighBid;
    }
}
