// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps.Dos;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal class ChannelUserlistRemoveBuilder : MessageBuilder
    {
        private readonly Channel _source;
        private ObjectGuid _guid;

        public ChannelUserlistRemoveBuilder(Channel source, ObjectGuid guid)
        {
            _source = source;
            _guid = guid;
        }

        public override PacketSenderOwning<UserlistRemove> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<UserlistRemove> userlistRemove = new();
            userlistRemove.Data.RemovedUserGUID = _guid;
            userlistRemove.Data.ChannelFlags = _source.GetFlags();
            userlistRemove.Data.ChannelID = _source.GetChannelId();
            userlistRemove.Data.ChannelName = _source.GetName(localeIdx);

            return userlistRemove;
        }
    }
}