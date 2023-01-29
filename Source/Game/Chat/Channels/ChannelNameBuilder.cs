// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat
{
    // initial packet _data (notify Type and channel Name)
    internal class ChannelNameBuilder : MessageBuilder
    {
        private readonly IChannelAppender _modifier;

        private readonly Channel _source;

        public ChannelNameBuilder(Channel source, IChannelAppender modifier)
        {
            _source = source;
            _modifier = modifier;
        }

        public override PacketSenderOwning<ChannelNotify> Invoke(Locale locale = Locale.enUS)
        {
            // LocalizedPacketDo sends client DBC locale, we need to get available to server locale
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChannelNotify> sender = new();
            sender.Data.Type = _modifier.GetNotificationType();
            sender.Data.Channel = _source.GetName(localeIdx);
            _modifier.Append(sender.Data);
            sender.Data.Write();

            return sender;
        }
    }
}