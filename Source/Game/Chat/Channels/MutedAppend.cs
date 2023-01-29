// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal struct MutedAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType()
        {
            return ChatNotify.MutedNotice;
        }

        public void Append(ChannelNotify data)
        {
        }
    }
}