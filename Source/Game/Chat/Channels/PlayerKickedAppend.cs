// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal struct PlayerKickedAppend : IChannelAppender
    {
        public PlayerKickedAppend(ObjectGuid kicker, ObjectGuid kickee)
        {
            _kicker = kicker;
            _kickee = kickee;
        }

        public ChatNotify GetNotificationType()
        {
            return ChatNotify.PlayerKickedNotice;
        }

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _kicker;
            data.TargetGuid = _kickee;
        }

        private ObjectGuid _kicker;
        private ObjectGuid _kickee;
    }
}