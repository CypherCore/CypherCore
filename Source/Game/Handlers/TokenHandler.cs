// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
