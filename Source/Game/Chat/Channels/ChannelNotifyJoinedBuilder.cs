// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal class ChannelNotifyJoinedBuilder : MessageBuilder
    {
        private readonly Channel _source;

        public ChannelNotifyJoinedBuilder(Channel source)
        {
            _source = source;
        }

        public override PacketSenderOwning<ChannelNotifyJoined> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChannelNotifyJoined> notify = new();
            //notify.ChannelWelcomeMsg = "";
            notify.Data.ChatChannelID = (int)_source.GetChannelId();
            //notify.InstanceID = 0;
            notify.Data.ChannelFlags = _source.GetFlags();
            notify.Data.Channel = _source.GetName(localeIdx);
            notify.Data.ChannelGUID = _source.GetGUID();

            return notify;
        }
    }
}