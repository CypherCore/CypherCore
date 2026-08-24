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
            Item.Read(_worldPacket);
            BidAmount = _worldPacket.ReadUInt64();
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
            Item.Write(_worldPacket);
            _worldPacket.WriteUInt32((uint)Result);
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
            Item.Write(_worldPacket);
            _worldPacket.WriteUInt32(RandomPropertiesID);
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
            Item.Write(_worldPacket);
            _worldPacket.WriteInt32(RandomPropertiesID);
        }

        public uint MarketID;
        public ItemInstance Item;
        public int RandomPropertiesID;
    }

    public struct BlackMarketItem
    {
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

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(MarketID);
            data.WriteUInt32(SellerNPC);
            Item.Write(data);
            data.WriteUInt32(Quantity);
            data.WriteUInt64(MinBid);
            data.WriteUInt64(MinIncrement);
            data.WriteUInt64(CurrentBid);
            data.WriteUInt32(SecondsRemaining);
            data.WriteUInt32(NumBids);
            data.WriteBit(HighBid);
            data.FlushBits();
        }
    }
}
