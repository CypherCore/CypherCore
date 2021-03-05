/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using Game.Cache;

namespace Game.Chat
{
    internal interface IChannelAppender
    {
        void Append(ChannelNotify data);
        ChatNotify GetNotificationType();
    }

    // initial packet data (notify type and channel name)
    internal class ChannelNameBuilder : MessageBuilder
    {
        public ChannelNameBuilder(Channel source, IChannelAppender modifier)
        {
            _source = source;
            _modifier = modifier;
        }

        public override ServerPacket Invoke(Locale locale = Locale.enUS)
        {
            // LocalizedPacketDo sends client DBC locale, we need to get available to server locale
            var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            var data = new ChannelNotify();
            data.Type = _modifier.GetNotificationType();
            data.Channel = _source.GetName(localeIdx);
            _modifier.Append(data);
            return data;
        }

        private Channel _source;
        private IChannelAppender _modifier;
    }

    internal class ChannelNotifyJoinedBuilder : MessageBuilder
    {
        public ChannelNotifyJoinedBuilder(Channel source)
        {
            _source = source;
        }

        public override ServerPacket Invoke(Locale locale = Locale.enUS)
        {
            var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            var notify = new ChannelNotifyJoined();
            //notify.ChannelWelcomeMsg = "";
            notify.ChatChannelID = (int)_source.GetChannelId();
            //notify.InstanceID = 0;
            notify.ChannelFlags = _source.GetFlags();
            notify.Channel = _source.GetName(localeIdx);
            notify.ChannelGUID = _source.GetChannelGuid();
            return notify;
        }

        private Channel _source;
    }

    internal class ChannelNotifyLeftBuilder : MessageBuilder
    {
        public ChannelNotifyLeftBuilder(Channel source, bool suspend)
        {
            _source = source;
            _suspended = suspend;
        }

        public override ServerPacket Invoke(Locale locale = Locale.enUS)
        {
            var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            var notify = new ChannelNotifyLeft();
            notify.Channel = _source.GetName(localeIdx);
            notify.ChatChannelID = _source.GetChannelId();
            notify.Suspended = _suspended;
            return notify;
        }

        private Channel _source;
        private bool _suspended;
    }

    internal class ChannelSayBuilder : MessageBuilder
    {
        public ChannelSayBuilder(Channel source, Language lang, string what, ObjectGuid guid)
        {
            _source = source;
            _lang = lang;
            _what = what;
            _guid = guid;
        }

        public override ServerPacket Invoke(Locale locale = Locale.enUS)
        {
            var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            var packet = new ChatPkt();
            var player = Global.ObjAccessor.FindConnectedPlayer(_guid);
            if (player)
                packet.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx));
            else
            {
                packet.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx));
                packet.SenderGUID = _guid;
                packet.TargetGUID = _guid;
            }

            return packet;
        }

        private Channel _source;
        private Language _lang;
        private string _what;
        private ObjectGuid _guid;
    }

    internal class ChannelWhisperBuilder : MessageBuilder
    {
        public ChannelWhisperBuilder(Channel source, Language lang, string what, string prefix, ObjectGuid guid)
        {
            _source = source;
            _lang = lang;
            _what = what;
            _prefix = prefix;
            _guid = guid;
        }

        public override ServerPacket Invoke(Locale locale = Locale.enUS)
        {
            var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            var packet = new ChatPkt();
            var player = Global.ObjAccessor.FindConnectedPlayer(_guid);
            if (player)
                packet.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
            else
            {
                packet.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
                packet.SenderGUID = _guid;
                packet.TargetGUID = _guid;
            }

            return packet;
        }

        private Channel _source;
        private Language _lang;
        private string _what;
        private string _prefix;
        private ObjectGuid _guid;
    }

    internal class ChannelUserlistAddBuilder : MessageBuilder
    {
        public ChannelUserlistAddBuilder(Channel source, ObjectGuid guid)
        {
            _source = source;
            _guid = guid;
        }

        public override ServerPacket Invoke(Locale locale = Locale.enUS)
        {
            var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            var userlistAdd = new UserlistAdd();
            userlistAdd.AddedUserGUID = _guid;
            userlistAdd.ChannelFlags = _source.GetFlags();
            userlistAdd.UserFlags = _source.GetPlayerFlags(_guid);
            userlistAdd.ChannelID = _source.GetChannelId();
            userlistAdd.ChannelName = _source.GetName(localeIdx);
            return userlistAdd;
        }

        private Channel _source;
        private ObjectGuid _guid;
    }

    internal class ChannelUserlistUpdateBuilder : MessageBuilder
    {
        public ChannelUserlistUpdateBuilder(Channel source, ObjectGuid guid)
        {
            _source = source;
            _guid = guid;
        }

        public override ServerPacket Invoke(Locale locale = Locale.enUS)
        {
            var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            var userlistUpdate = new UserlistUpdate();
            userlistUpdate.UpdatedUserGUID = _guid;
            userlistUpdate.ChannelFlags = _source.GetFlags();
            userlistUpdate.UserFlags = _source.GetPlayerFlags(_guid);
            userlistUpdate.ChannelID = _source.GetChannelId();
            userlistUpdate.ChannelName = _source.GetName(localeIdx);
            return userlistUpdate;
        }

        private Channel _source;
        private ObjectGuid _guid;
    }

    internal class ChannelUserlistRemoveBuilder : MessageBuilder
    {
        public ChannelUserlistRemoveBuilder(Channel source, ObjectGuid guid)
        {
            _source = source;
            _guid = guid;
        }

        public override ServerPacket Invoke(Locale locale = Locale.enUS)
        {
            var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            var userlistRemove = new UserlistRemove();
            userlistRemove.RemovedUserGUID = _guid;
            userlistRemove.ChannelFlags = _source.GetFlags();
            userlistRemove.ChannelID = _source.GetChannelId();
            userlistRemove.ChannelName = _source.GetName(localeIdx);
            return userlistRemove;
        }

        private Channel _source;
        private ObjectGuid _guid;
    }

    //Appenders
    internal struct JoinedAppend : IChannelAppender
    {
        public JoinedAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.JoinedNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct LeftAppend : IChannelAppender
    {
        public LeftAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.LeftNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct YouJoinedAppend : IChannelAppender
    {
        public YouJoinedAppend(Channel channel)
        {
            _channel = channel;
        }

        public ChatNotify GetNotificationType() => ChatNotify.YouJoinedNotice;

        public void Append(ChannelNotify data)
        {
            data.ChatChannelID = (int)_channel.GetChannelId();
        }

        private Channel _channel;
    }

    internal struct YouLeftAppend : IChannelAppender
    {
        public YouLeftAppend(Channel channel)
        {
            _channel = channel;
        }

        public ChatNotify GetNotificationType() => ChatNotify.YouLeftNotice;

        public void Append(ChannelNotify data)
        {
            data.ChatChannelID = (int)_channel.GetChannelId();
        }

        private Channel _channel;
    }

    internal struct WrongPasswordAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.WrongPasswordNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct NotMemberAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotMemberNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct NotModeratorAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotModeratorNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct PasswordChangedAppend : IChannelAppender
    {
        public PasswordChangedAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PasswordChangedNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct OwnerChangedAppend : IChannelAppender
    {
        public OwnerChangedAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.OwnerChangedNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct PlayerNotFoundAppend : IChannelAppender
    {
        public PlayerNotFoundAppend(string playerName)
        {
            _playerName = playerName;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PlayerNotFoundNotice;

        public void Append(ChannelNotify data)
        {
            data.Sender = _playerName;
        }

        private string _playerName;
    }

    internal struct NotOwnerAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotOwnerNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct ChannelOwnerAppend : IChannelAppender
    {
        public ChannelOwnerAppend(Channel channel, ObjectGuid ownerGuid)
        {
            _channel = channel;
            _ownerGuid = ownerGuid;
            _ownerName = "";

            var characterCacheEntry = Global.CharacterCacheStorage.GetCharacterCacheByGuid(_ownerGuid);
            if (characterCacheEntry != null)
                _ownerName = characterCacheEntry.Name;
        }

        public ChatNotify GetNotificationType() => ChatNotify.ChannelOwnerNotice;

        public void Append(ChannelNotify data)
        {
            data.Sender = ((_channel.IsConstant() || _ownerGuid.IsEmpty()) ? "Nobody" : _ownerName);
        }

        private Channel _channel;
        private ObjectGuid _ownerGuid;

        private string _ownerName;
    }

    internal struct ModeChangeAppend : IChannelAppender
    {
        public ModeChangeAppend(ObjectGuid guid, ChannelMemberFlags oldFlags, ChannelMemberFlags newFlags)
        {
            _guid = guid;
            _oldFlags = oldFlags;
            _newFlags = newFlags;
        }

        public ChatNotify GetNotificationType() => ChatNotify.ModeChangeNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
            data.OldFlags = _oldFlags;
            data.NewFlags = _newFlags;
        }

        private ObjectGuid _guid;
        private ChannelMemberFlags _oldFlags;
        private ChannelMemberFlags _newFlags;
    }

    internal struct AnnouncementsOnAppend : IChannelAppender
    {
        public AnnouncementsOnAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.AnnouncementsOnNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct AnnouncementsOffAppend : IChannelAppender
    {
        public AnnouncementsOffAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.AnnouncementsOffNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct MutedAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.MutedNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct PlayerKickedAppend : IChannelAppender
    {
        public PlayerKickedAppend(ObjectGuid kicker, ObjectGuid kickee)
        {
            _kicker = kicker;
            _kickee = kickee;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PlayerKickedNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _kicker;
            data.TargetGuid = _kickee;
        }

        private ObjectGuid _kicker;
        private ObjectGuid _kickee;
    }

    internal struct BannedAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.BannedNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct PlayerBannedAppend : IChannelAppender
    {
        public PlayerBannedAppend(ObjectGuid moderator, ObjectGuid banned)
        {
            _moderator = moderator;
            _banned = banned;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PlayerBannedNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _moderator;
            data.TargetGuid = _banned;
        }

        private ObjectGuid _moderator;
        private ObjectGuid _banned;
    }

    internal struct PlayerUnbannedAppend : IChannelAppender
    {
        public PlayerUnbannedAppend(ObjectGuid moderator, ObjectGuid unbanned)
        {
            _moderator = moderator;
            _unbanned = unbanned;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PlayerUnbannedNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _moderator;
            data.TargetGuid = _unbanned;
        }

        private ObjectGuid _moderator;
        private ObjectGuid _unbanned;
    }

    internal struct PlayerNotBannedAppend : IChannelAppender
    {
        public PlayerNotBannedAppend(string playerName)
        {
            _playerName = playerName;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PlayerNotBannedNotice;

        public void Append(ChannelNotify data)
        {
            data.Sender = _playerName;
        }

        private string _playerName;
    }

    internal struct PlayerAlreadyMemberAppend : IChannelAppender
    {
        public PlayerAlreadyMemberAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PlayerAlreadyMemberNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct InviteAppend : IChannelAppender
    {
        public InviteAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.InviteNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct InviteWrongFactionAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.InviteWrongFactionNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct WrongFactionAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.WrongFactionNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct InvalidNameAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.InvalidNameNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct NotModeratedAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotModeratedNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct PlayerInvitedAppend : IChannelAppender
    {
        public PlayerInvitedAppend(string playerName)
        {
            _playerName = playerName;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PlayerInvitedNotice;

        public void Append(ChannelNotify data)
        {
            data.Sender = _playerName;
        }

        private string _playerName;
    }

    internal struct PlayerInviteBannedAppend : IChannelAppender
    {
        public PlayerInviteBannedAppend(string playerName)
        {
            _playerName = playerName;
        }

        public ChatNotify GetNotificationType() => ChatNotify.PlayerInviteBannedNotice;

        public void Append(ChannelNotify data)
        {
            data.Sender = _playerName;
        }

        private string _playerName;
    }

    internal struct ThrottledAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.ThrottledNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct NotInAreaAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotInAreaNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct NotInLFGAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotInLfgNotice;

        public void Append(ChannelNotify data) { }
    }

    internal struct VoiceOnAppend : IChannelAppender
    {
        public VoiceOnAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.VoiceOnNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }

    internal struct VoiceOffAppend : IChannelAppender
    {
        public VoiceOffAppend(ObjectGuid guid)
        {
            _guid = guid;
        }

        public ChatNotify GetNotificationType() => ChatNotify.VoiceOffNotice;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }

        private ObjectGuid _guid;
    }
}
