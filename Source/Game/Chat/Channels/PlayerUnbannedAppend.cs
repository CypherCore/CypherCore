// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal struct PlayerUnbannedAppend : IChannelAppender
    {
        public PlayerUnbannedAppend(ObjectGuid moderator, ObjectGuid unbanned)
        {
            _moderator = moderator;
            _unbanned = unbanned;
        }

        public ChatNotify GetNotificationType()
        {
            return ChatNotify.PlayerUnbannedNotice;
        }

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _moderator;
            data.TargetGuid = _unbanned;
        }

        private ObjectGuid _moderator;
        private ObjectGuid _unbanned;
    }
}