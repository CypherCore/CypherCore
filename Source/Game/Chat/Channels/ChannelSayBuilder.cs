// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal class ChannelSayBuilder : MessageBuilder
    {
        private ObjectGuid _channelGuid;
        private ObjectGuid _guid;
        private readonly Language _lang;

        private readonly Channel _source;
        private readonly string _what;

        public ChannelSayBuilder(Channel source, Language lang, string what, ObjectGuid guid, ObjectGuid channelGuid)
        {
            _source = source;
            _lang = lang;
            _what = what;
            _guid = guid;
            _channelGuid = channelGuid;
        }

        public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChatPkt> packet = new();
            Player player = Global.ObjAccessor.FindConnectedPlayer(_guid);

            if (player)
            {
                packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx));
            }
            else
            {
                packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx));
                packet.Data.SenderGUID = _guid;
                packet.Data.TargetGUID = _guid;
            }

            packet.Data.ChannelGUID = _channelGuid;

            return packet;
        }
    }
}