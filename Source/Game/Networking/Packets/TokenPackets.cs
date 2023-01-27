// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets
{
	internal class CommerceTokenGetLog : ClientPacket
	{
		public uint UnkInt;

		public CommerceTokenGetLog(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			UnkInt = _worldPacket.ReadUInt32();
		}
	}

	internal class CommerceTokenGetLogResponse : ServerPacket
	{
		private List<AuctionableTokenInfo> AuctionableTokenAuctionableList = new();
		public TokenResult Result;

		public uint UnkInt; // send CMSG_UPDATE_WOW_TOKEN_AUCTIONABLE_LIST

		public CommerceTokenGetLogResponse() : base(ServerOpcodes.CommerceTokenGetLogResponse, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(UnkInt);
			_worldPacket.WriteUInt32((uint)Result);
			_worldPacket.WriteInt32(AuctionableTokenAuctionableList.Count);

			foreach (AuctionableTokenInfo auctionableTokenAuctionable in AuctionableTokenAuctionableList)
			{
				_worldPacket.WriteUInt64(auctionableTokenAuctionable.UnkInt1);
				_worldPacket.WriteInt64(auctionableTokenAuctionable.UnkInt2);
				_worldPacket.WriteUInt64(auctionableTokenAuctionable.BuyoutPrice);
				_worldPacket.WriteUInt32(auctionableTokenAuctionable.Owner);
				_worldPacket.WriteUInt32(auctionableTokenAuctionable.DurationLeft);
			}
		}

		private struct AuctionableTokenInfo
		{
			public ulong UnkInt1;
			public long UnkInt2;
			public uint Owner;
			public ulong BuyoutPrice;
			public uint DurationLeft;
		}
	}

	internal class CommerceTokenGetMarketPrice : ClientPacket
	{
		public uint UnkInt;

		public CommerceTokenGetMarketPrice(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			UnkInt = _worldPacket.ReadUInt32();
		}
	}

	internal class CommerceTokenGetMarketPriceResponse : ServerPacket
	{
		public uint AuctionDuration; // preset auction duration enum

		public ulong CurrentMarketPrice;
		public TokenResult Result;
		public uint UnkInt; // send CMSG_REQUEST_WOW_TOKEN_MARKET_PRICE

		public CommerceTokenGetMarketPriceResponse() : base(ServerOpcodes.CommerceTokenGetMarketPriceResponse)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(CurrentMarketPrice);
			_worldPacket.WriteUInt32(UnkInt);
			_worldPacket.WriteUInt32((uint)Result);
			_worldPacket.WriteUInt32(AuctionDuration);
		}
	}
}