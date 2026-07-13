// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class DeclineNeighborhoodInvites(WorldPacket packet) : ClientPacket(packet)
    {
        public override void Read()
        {
            Allow = _worldPacket.HasBit();
        }

        public bool Allow;
    }
}
