// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal class ChannelWhisperBuilder : MessageBuilder
    {
        private readonly Language _lang;
        private readonly string _prefix;

        private readonly Channel _source;
        private readonly string _what;
        private ObjectGuid _guid;

        public ChannelWhisperBuilder(Channel source, Language lang, string what, string prefix, ObjectGuid guid)
        {
            _source = source;
            _lang = lang;
            _what = what;
            _prefix = prefix;
            _guid = guid;
        }

        public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChatPkt> packet = new();
            Player player = Global.ObjAccessor.FindConnectedPlayer(_guid);

            if (player)
            {
                packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
            }
            else
            {
                packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
                packet.Data.SenderGUID = _guid;
                packet.Data.TargetGUID = _guid;
            }

            return packet;
        }
    }
}