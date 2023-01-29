// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal class ChannelUserlistAddBuilder : MessageBuilder
    {
        private ObjectGuid _guid;

        private readonly Channel _source;

        public ChannelUserlistAddBuilder(Channel source, ObjectGuid guid)
        {
            _source = source;
            _guid = guid;
        }

        public override PacketSenderOwning<UserlistAdd> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<UserlistAdd> userlistAdd = new();
            userlistAdd.Data.AddedUserGUID = _guid;
            userlistAdd.Data.ChannelFlags = _source.GetFlags();
            userlistAdd.Data.UserFlags = _source.GetPlayerFlags(_guid);
            userlistAdd.Data.ChannelID = _source.GetChannelId();
            userlistAdd.Data.ChannelName = _source.GetName(localeIdx);

            return userlistAdd;
        }
    }
}