// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Networking.Packets;

namespace Game.Entities
{
    internal class CombatLogSender : IDoWork<Player>
    {
        private readonly CombatLogServerPacket _message;

        public CombatLogSender(CombatLogServerPacket msg)
        {
            _message = msg;
        }

        public void Invoke(Player player)
        {
            _message.Clear();
            _message.SetAdvancedCombatLogging(player.IsAdvancedCombatLoggingEnabled());

            player.SendPacket(_message);
        }
    }
}