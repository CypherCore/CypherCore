// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Cache;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal struct ChannelOwnerAppend : IChannelAppender
    {
        public ChannelOwnerAppend(Channel channel, ObjectGuid ownerGuid)
        {
            _channel = channel;
            _ownerGuid = ownerGuid;
            _ownerName = "";

            CharacterCacheEntry characterCacheEntry = Global.CharacterCacheStorage.GetCharacterCacheByGuid(_ownerGuid);

            if (characterCacheEntry != null)
                _ownerName = characterCacheEntry.Name;
        }

        public ChatNotify GetNotificationType()
        {
            return ChatNotify.ChannelOwnerNotice;
        }

        public void Append(ChannelNotify data)
        {
            data.Sender = ((_channel.IsConstant() || _ownerGuid.IsEmpty()) ? "Nobody" : _ownerName);
        }

        private readonly Channel _channel;
        private ObjectGuid _ownerGuid;

        private readonly string _ownerName;
    }
}