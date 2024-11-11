// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Cache;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat
{
    interface IChannelAppender
    {
        void Append(ChannelNotify data);
        ChatNotify GetNotificationType();
    }

    // initial packet data (notify type and channel name)
    class ChannelNameBuilder : MessageBuilder
    {
        public ChannelNameBuilder(Channel source, IChannelAppender modifier)
        {
            _source = source;
            _modifier = modifier;
        }

        public override PacketSenderOwning<ChannelNotify> Invoke(Locale locale = Locale.enUS)
        {
            // LocalizedPacketDo sends client DBC locale, we need to get available to server locale
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChannelNotify> sender = new();
            sender.Data.Type = _modifier.GetNotificationType();
            sender.Data.Channel = _source.GetName(localeIdx);
            _modifier.Append(sender.Data);
            sender.Data.Write();
            return sender;
        }

        Channel _source;
        IChannelAppender _modifier;
    }

    class ChannelNotifyJoinedBuilder : MessageBuilder
    {
        public ChannelNotifyJoinedBuilder(Channel source)
        {
            _source = source;
        }

        public override PacketSenderOwning<ChannelNotifyJoined> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChannelNotifyJoined> notify = new();
            //notify.ChannelWelcomeMsg = "";
            notify.Data.ChatChannelID = (int)_source.GetChannelId();
            //notify.InstanceID = 0;
            notify.Data.ChannelFlags = _source.GetFlags();
            notify.Data.Channel = _source.GetName(localeIdx);
            notify.Data.ChannelGUID = _source.GetGUID();
            return notify;
        }

        Channel _source;
    }

    class ChannelNotifyLeftBuilder : MessageBuilder
    {
        public ChannelNotifyLeftBuilder(Channel source, bool suspend)
        {
            _source = source;
            _suspended = suspend;
        }

        public override PacketSenderOwning<ChannelNotifyLeft> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChannelNotifyLeft> notify = new();
            notify.Data.Channel = _source.GetName(localeIdx);
            notify.Data.ChatChannelID = _source.GetChannelId();
            notify.Data.Suspended = _suspended;
            return notify;
        }

        Channel _source;
        bool _suspended;
    }

    class ChannelSayBuilder : MessageBuilder
    {
        public ChannelSayBuilder(Channel source, Language lang, string what, ObjectGuid guid, ObjectGuid channelGuid)
        {
            _source = source;
            _lang = lang;
            _what = what;
            _guid = guid;
            _channelGuid = channelGuid;
        }

        public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChatPkt> packet = new();
            Player player = Global.ObjAccessor.FindConnectedPlayer(_guid);
            if (player != null)
                packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx));
            else
            {
                packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx));
                packet.Data.SenderGUID = _guid;
                packet.Data.TargetGUID = _guid;
            }

            packet.Data.ChannelGUID = _channelGuid;

            return packet;
        }
        
        Channel _source;
        Language _lang;
        string _what;
        ObjectGuid _guid;
        ObjectGuid _channelGuid;
    }

    class ChannelWhisperBuilder : MessageBuilder
    {
        public ChannelWhisperBuilder(Channel source, Language lang, string what, string prefix, ObjectGuid guid)
        {
            _source = source;
            _lang = lang;
            _what = what;
            _prefix = prefix;
            _guid = guid;
        }

        public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
        {
            Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

            PacketSenderOwning<ChatPkt> packet = new();
            Player player = Global.ObjAccessor.FindConnectedPlayer(_guid);
            if (player != null)
                packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
            else
            {
                packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
                packet.Data.SenderGUID = _guid;
                packet.Data.TargetGUID = _guid;
            }

            return packet;
        }

        Channel _source;
        Language _lang;
        string _what;
        string _prefix;
        ObjectGuid _guid;
    }

    class ChannelUserlistAddBuilder : MessageBuilder
    {
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

        Channel _source;
        ObjectGuid _guid;
    }

    class ChannelUserlistUpdateBuilder : MessageBuilder
    {
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

        Channel _source;
        ObjectGuid _guid;
    }

    class ChannelUserlistRemoveBuilder : MessageBuilder
    {
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

        Channel _source;
        ObjectGuid _guid;
    }

    //Appenders
    struct JoinedAppend : IChannelAppender
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

        ObjectGuid _guid;
    }

    struct LeftAppend : IChannelAppender
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

        ObjectGuid _guid;
    }

    struct YouJoinedAppend : IChannelAppender
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

        Channel _channel;
    }

    struct YouLeftAppend : IChannelAppender
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

        Channel _channel;
    }

    struct WrongPasswordAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.WrongPasswordNotice;

        public void Append(ChannelNotify data) { }
    }

    struct NotMemberAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotMemberNotice;

        public void Append(ChannelNotify data) { }
    }

    struct NotModeratorAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotModeratorNotice;

        public void Append(ChannelNotify data) { }
    }

    struct PasswordChangedAppend : IChannelAppender
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

        ObjectGuid _guid;
    }

    struct OwnerChangedAppend : IChannelAppender
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

        ObjectGuid _guid;
    }

    struct PlayerNotFoundAppend : IChannelAppender
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

        string _playerName;
    }

    struct NotOwnerAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotOwnerNotice;

        public void Append(ChannelNotify data) { }
    }

    struct ChannelOwnerAppend : IChannelAppender
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

        public ChatNotify GetNotificationType() => ChatNotify.ChannelOwnerNotice;

        public void Append(ChannelNotify data)
        {
            data.Sender = ((_channel.IsConstant() || _ownerGuid.IsEmpty()) ? "Nobody" : _ownerName);
        }

        Channel _channel;
        ObjectGuid _ownerGuid;

        string _ownerName;
    }

    struct ModeChangeAppend : IChannelAppender
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

        ObjectGuid _guid;
        ChannelMemberFlags _oldFlags;
        ChannelMemberFlags _newFlags;
    }

    struct AnnouncementsOnAppend : IChannelAppender
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

        ObjectGuid _guid;
    }

    struct AnnouncementsOffAppend : IChannelAppender
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

        ObjectGuid _guid;
    }

    struct MutedAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.MutedNotice;

        public void Append(ChannelNotify data) { }
    }

    struct PlayerKickedAppend : IChannelAppender
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

        ObjectGuid _kicker;
        ObjectGuid _kickee;
    }

    struct BannedAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.BannedNotice;

        public void Append(ChannelNotify data) { }
    }

    struct PlayerBannedAppend : IChannelAppender
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

        ObjectGuid _moderator;
        ObjectGuid _banned;
    }

    struct PlayerUnbannedAppend : IChannelAppender
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

        ObjectGuid _moderator;
        ObjectGuid _unbanned;
    }

    struct PlayerNotBannedAppend : IChannelAppender
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

        string _playerName;
    }

    struct PlayerAlreadyMemberAppend : IChannelAppender
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

        ObjectGuid _guid;
    }

    struct InviteAppend : IChannelAppender
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

        ObjectGuid _guid;
    }

    struct InviteWrongFactionAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.InviteWrongFactionNotice;

        public void Append(ChannelNotify data) { }
    }

    struct WrongFactionAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.WrongFactionNotice;

        public void Append(ChannelNotify data) { }
    }

    struct InvalidNameAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.InvalidNameNotice;

        public void Append(ChannelNotify data) { }
    }

    struct NotModeratedAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotModeratedNotice;

        public void Append(ChannelNotify data) { }
    }

    struct PlayerInvitedAppend : IChannelAppender
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

        string _playerName;
    }

    struct PlayerInviteBannedAppend : IChannelAppender
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

        string _playerName;
    }

    struct ThrottledAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.ThrottledNotice;

        public void Append(ChannelNotify data) { }
    }

    struct NotInAreaAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotInAreaNotice;

        public void Append(ChannelNotify data) { }
    }

    struct NotInLFGAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType() => ChatNotify.NotInLfgNotice;

        public void Append(ChannelNotify data) { }
    }

    struct VoiceOnAppend : IChannelAppender
    {
        ChatNotify notificationType;
        ObjectGuid _guid;

        public VoiceOnAppend(ObjectGuid guid, bool announce = true)
        {
            _guid = guid;
            notificationType = announce ? ChatNotify.VoiceOnNotice : ChatNotify.VoiceOnNoAnnounceNotice;
        }

        public ChatNotify GetNotificationType() => notificationType;

        public void Append(ChannelNotify data)
        {
            data.SenderGuid = _guid;
        }
    }

    struct VoiceOffAppend : IChannelAppender
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

        ObjectGuid _guid;
    }
}
