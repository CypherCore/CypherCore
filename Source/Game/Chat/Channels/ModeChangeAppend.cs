// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal struct ModeChangeAppend : IChannelAppender
    {
        public ModeChangeAppend(ObjectGuid guid, ChannelMemberFlags oldFlags, ChannelMemberFlags newFlags)
        {
            _guid = guid;
            _oldFlags = oldFlags;
            _newFlags = newFlags;
        }

        public ChatNotify GetNotificationType()
        {
            return ChatNotify.ModeChangeNotice;
        }

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
            data.OldFlags = _oldFlags;
            data.NewFlags = _newFlags;
        }

        private ObjectGuid _guid;
        private readonly ChannelMemberFlags _oldFlags;
        private readonly ChannelMemberFlags _newFlags;
    }
}