// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CommerceTokenGetLog)]
        void HandleCommerceTokenGetLog(CommerceTokenGetLog commerceTokenGetLog)
        {
            CommerceTokenGetLogResponse response = new();

            // @todo: fix 6.x implementation
            response.UnkInt = commerceTokenGetLog.UnkInt;
            response.Result = TokenResult.Success;

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.CommerceTokenGetMarketPrice)]
        void HandleCommerceTokenGetMarketPrice(CommerceTokenGetMarketPrice commerceTokenGetMarketPrice)
        {
            CommerceTokenGetMarketPriceResponse response = new();

            // @todo: 6.x fix implementation
            response.CurrentMarketPrice = 300000000;
            response.UnkInt = commerceTokenGetMarketPrice.UnkInt;
            response.Result = TokenResult.Success;
            //packet.ReadUInt32("UnkInt32");

            SendPacket(response);
        }
    }
}
