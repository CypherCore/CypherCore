// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class CommerceTokenGetLog : ClientPacket
    {
        public CommerceTokenGetLog(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ClientToken = _worldPacket.ReadUInt32();
        }

        public uint ClientToken;
    }

    class CommerceTokenGetLogResponse : ServerPacket
    {
        public CommerceTokenGetLogResponse() : base(ServerOpcodes.CommerceTokenGetLogResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ClientToken);
            _worldPacket.WriteUInt32((uint)Result);
            _worldPacket.WriteInt32(AuctionableTokens.Count);

            foreach (AuctionableTokenInfo auctionableTokenAuctionable in AuctionableTokens)
            {
                _worldPacket.WriteUInt64(auctionableTokenAuctionable.Id);
                _worldPacket.WriteInt64(auctionableTokenAuctionable.LastUpdate);
                _worldPacket.WriteUInt64(auctionableTokenAuctionable.Price);
                _worldPacket.WriteUInt32(auctionableTokenAuctionable.Status);
                _worldPacket.WriteUInt32(auctionableTokenAuctionable.DurationLeft);
            }
        }

        public uint ClientToken;
        public TokenResult Result;
        List<AuctionableTokenInfo> AuctionableTokens = new();

        struct AuctionableTokenInfo
        {
            public ulong Id;
            public long LastUpdate;
            public uint Status;
            public ulong Price;
            public uint DurationLeft;
        }
    }

    class CommerceTokenGetMarketPrice : ClientPacket
    {
        public CommerceTokenGetMarketPrice(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ClientToken = _worldPacket.ReadUInt32();
        }

        public uint ClientToken;
    }

    class CommerceTokenGetMarketPriceResponse : ServerPacket
    {
        public CommerceTokenGetMarketPriceResponse() : base(ServerOpcodes.CommerceTokenGetMarketPriceResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(PriceGuarantee);
            _worldPacket.WriteUInt32(ClientToken);
            _worldPacket.WriteUInt32((uint)ServerToken);
            _worldPacket.WriteUInt32(PriceLockDurationSeconds);
        }

        public ulong PriceGuarantee;
        public uint ClientToken;
        public TokenResult ServerToken;
        public uint PriceLockDurationSeconds;
    }
}
