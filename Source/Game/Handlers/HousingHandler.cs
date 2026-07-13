// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.DeclineNeighborhoodInvites)]
        void HandleDeclineNeighborhoodInvites(DeclineNeighborhoodInvites declineNeighborhoodInvites)
        {
            if (declineNeighborhoodInvites.Allow)
                GetPlayer().SetPlayerFlagEx(PlayerFlagsEx.AutoDeclineNeighborhood);
            else
                GetPlayer().RemovePlayerFlagEx(PlayerFlagsEx.AutoDeclineNeighborhood);
        }
    }
}
