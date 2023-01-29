// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal struct PlayerBannedAppend : IChannelAppender
    {
        public PlayerBannedAppend(ObjectGuid moderator, ObjectGuid banned)
        {
            _moderator = moderator;
            _banned = banned;
        }

        public ChatNotify GetNotificationType()
        {
            return ChatNotify.PlayerBannedNotice;
        }

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _moderator;
            data.TargetGuid = _banned;
        }

        private ObjectGuid _moderator;
        private ObjectGuid _banned;
    }
}