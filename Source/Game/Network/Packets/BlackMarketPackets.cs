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
    class BlackMarketOpen : ClientPacket
    {
        public BlackMarketOpen(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    class BlackMarketOpenResult : ServerPacket
    {
        public BlackMarketOpenResult() : base(ServerOpcodes.BlackMarketOpenResult) { }

        public override void Write()
        {
            _worldPacket .WritePackedGuid(Guid);
            _worldPacket.WriteBit(Enable);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Guid;
        public bool Enable = true;
    }

    class BlackMarketRequestItems : ClientPacket
    {
        public BlackMarketRequestItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            LastUpdateID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Guid;
        public uint LastUpdateID;
    }

    public class BlackMarketRequestItemsResult : ServerPacket
    {
        public BlackMarketRequestItemsResult() : base(ServerOpcodes.BlackMarketRequestItemsResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(LastUpdateID);
            _worldPacket.WriteUInt32(Items.Count);

            foreach (BlackMarketItem item in Items)
                item.Write(_worldPacket);
        }

        public int LastUpdateID;
        public List<BlackMarketItem> Items = new List<BlackMarketItem>();
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
        public ItemInstance Item = new ItemInstance();
        public ulong BidAmount;
    }

    class BlackMarketBidOnItemResult : ServerPacket
    {
        public BlackMarketBidOnItemResult() : base(ServerOpcodes.BlackMarketBidOnItemResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MarketID);
            _worldPacket.WriteUInt32(Result);
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
            data.WriteInt32(MarketID);
            data.WriteInt32(SellerNPC);
            data.WriteInt32(Quantity);
            data.WriteUInt64(MinBid);
            data.WriteUInt64(MinIncrement);
            data.WriteUInt64(CurrentBid);
            data.WriteInt32(SecondsRemaining);
            data.WriteInt32(NumBids);
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
