// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Chat;
using Game.Maps.Dos;
using Game.Networking.Packets;

namespace Game.PvP
{
    internal class DefenseMessageBuilder : MessageBuilder
    {
        private readonly uint _id; // BroadcastTextId

        private readonly uint _zoneId; // ZoneId

        public DefenseMessageBuilder(uint zoneId, uint id)
        {
            _zoneId = zoneId;
            _id = id;
        }

        public override PacketSenderOwning<DefenseMessage> Invoke(Locale locale = Locale.enUS)
        {
            string text = Global.OutdoorPvPMgr.GetDefenseMessage(_zoneId, _id, locale);

            PacketSenderOwning<DefenseMessage> defenseMessage = new();
            defenseMessage.Data.ZoneID = _zoneId;
            defenseMessage.Data.MessageText = text;

            return defenseMessage;
        }
    }
}