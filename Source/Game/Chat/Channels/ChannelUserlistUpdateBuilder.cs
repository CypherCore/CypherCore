// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal class ChannelUserlistUpdateBuilder : MessageBuilder
    {
        private ObjectGuid _guid;

        private readonly Channel _source;

        public ChannelUserlistUpdateBuilder(Channel source, ObjectGuid guid)
        {
            _source = source;
            _guid = guid;
        }

        public override PacketSenderOwning<UserlistUpdate> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<UserlistUpdate> userlistUpdate = new();
            userlistUpdate.Data.UpdatedUserGUID = _guid;
            userlistUpdate.Data.ChannelFlags = _source.GetFlags();
            userlistUpdate.Data.UserFlags = _source.GetPlayerFlags(_guid);
            userlistUpdate.Data.ChannelID = _source.GetChannelId();
            userlistUpdate.Data.ChannelName = _source.GetName(localeIdx);

            return userlistUpdate;
        }
    }
}