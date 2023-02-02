// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.ServerTimeOffsetRequest, Status = SessionStatus.Authed, Processing = PacketProcessing.Inplace)]
        void HandleServerTimeOffsetRequest(ServerTimeOffsetRequest packet)
        {
            ServerTimeOffset response = new();
            response.Time = GameTime.GetGameTime();
            SendPacket(response);
        }
    }
}
