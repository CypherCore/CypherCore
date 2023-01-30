// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal readonly struct YouLeftAppend : IChannelAppender
    {
        public YouLeftAppend(Channel channel)
        {
            _channel = channel;
        }

        public ChatNotify GetNotificationType()
        {
            return ChatNotify.YouLeftNotice;
        }

        public void Append(ChannelNotify data)
        {
            data.ChatChannelID = (int)_channel.GetChannelId();
        }

        private readonly Channel _channel;
    }
}