// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps.Dos;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal class ChannelNotifyLeftBuilder : MessageBuilder
    {
        private readonly Channel _source;
        private readonly bool _suspended;

        public ChannelNotifyLeftBuilder(Channel source, bool suspend)
        {
            _source = source;
            _suspended = suspend;
        }

        public override PacketSenderOwning<ChannelNotifyLeft> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChannelNotifyLeft> notify = new();
            notify.Data.Channel = _source.GetName(localeIdx);
            notify.Data.ChatChannelID = _source.GetChannelId();
            notify.Data.Suspended = _suspended;

            return notify;
        }
    }
}