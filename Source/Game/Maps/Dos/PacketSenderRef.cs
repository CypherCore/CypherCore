// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;
using Game.Networking;

namespace Game.Maps.Dos
{
    public class PacketSenderRef : IDoWork<Player>
    {
        private readonly ServerPacket _data;

        public PacketSenderRef(ServerPacket message)
        {
            _data = message;
        }

        public virtual void Invoke(Player player)
        {
            player.SendPacket(_data);
        }
    }
}