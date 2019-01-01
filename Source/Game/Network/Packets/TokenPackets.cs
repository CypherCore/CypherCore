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
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class UpdateListedAuctionableTokens : ClientPacket
    {
        public UpdateListedAuctionableTokens(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UnkInt = _worldPacket.ReadUInt32();
        }

        public uint UnkInt;
    }

    class UpdateListedAuctionableTokensResponse : ServerPacket
    {
        public UpdateListedAuctionableTokensResponse() : base(ServerOpcodes.WowTokenUpdateAuctionableListResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket .WriteUInt32( UnkInt);
            _worldPacket .WriteUInt32( Result);
            _worldPacket .WriteUInt32(AuctionableTokenAuctionableList.Count);

            foreach (AuctionableTokenAuctionable auctionableTokenAuctionable in AuctionableTokenAuctionableList)
            {
                _worldPacket.WriteUInt64(auctionableTokenAuctionable.UnkInt1);
                _worldPacket.WriteUInt32(auctionableTokenAuctionable.UnkInt2);
                _worldPacket.WriteUInt32(auctionableTokenAuctionable.Owner);
                _worldPacket.WriteUInt64(auctionableTokenAuctionable.BuyoutPrice);
                _worldPacket.WriteUInt32(auctionableTokenAuctionable.EndTime);
            }
        }

        public uint UnkInt; // send CMSG_UPDATE_WOW_TOKEN_AUCTIONABLE_LIST
        public TokenResult Result;
        List<AuctionableTokenAuctionable> AuctionableTokenAuctionableList =new List<AuctionableTokenAuctionable>();

        struct AuctionableTokenAuctionable
        {
            public ulong UnkInt1;
            public uint UnkInt2;
            public uint Owner;
            public ulong BuyoutPrice;
            public uint EndTime;
        }
    }

    class RequestWowTokenMarketPrice : ClientPacket
    {
        public RequestWowTokenMarketPrice(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UnkInt = _worldPacket.ReadUInt32();
        }

        public uint UnkInt;
    }

    class WowTokenMarketPriceResponse : ServerPacket
    {
        public WowTokenMarketPriceResponse() : base(ServerOpcodes.WowTokenMarketPriceResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(CurrentMarketPrice);
            _worldPacket.WriteUInt32(UnkInt);
            _worldPacket.WriteUInt32(Result);
            _worldPacket.WriteUInt32(UnkInt2);
        }

        public ulong CurrentMarketPrice;
        public uint UnkInt; // send CMSG_REQUEST_WOW_TOKEN_MARKET_PRICE
        public TokenResult Result;
        public uint UnkInt2 = 0;
    }
}
