// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal struct InviteAppend : IChannelAppender
    {
        public InviteAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType()
        {
            return ChatNotify.InviteNotice;
        }

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }
}