/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CommerceTokenGetLog)]
        void HandleCommerceTokenGetLog(CommerceTokenGetLog commerceTokenGetLog)
        {
            CommerceTokenGetLogResponse response = new CommerceTokenGetLogResponse();

            // @todo: fix 6.x implementation
            response.UnkInt = commerceTokenGetLog.UnkInt;
            response.Result = TokenResult.Success;

            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.CommerceTokenGetMarketPrice)]
        void HandleCommerceTokenGetMarketPrice(CommerceTokenGetMarketPrice commerceTokenGetMarketPrice)
        {
            CommerceTokenGetMarketPriceResponse response = new CommerceTokenGetMarketPriceResponse();

            // @todo: 6.x fix implementation
            response.CurrentMarketPrice = 300000000;
            response.UnkInt = commerceTokenGetMarketPrice.UnkInt;
            response.Result = TokenResult.Success;
            //packet.ReadUInt32("UnkInt32");

            SendPacket(response);
        }
    }
}
