// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Cache;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat
{
	internal interface IChannelAppender
	{
		void Append(ChannelNotify data);
		ChatNotify GetNotificationType();
	}

	// initial packet data (notify Type and channel name)
	internal class ChannelNameBuilder : MessageBuilder
	{
		private IChannelAppender _modifier;

		private Channel _source;

		public ChannelNameBuilder(Channel source, IChannelAppender modifier)
		{
			_source   = source;
			_modifier = modifier;
		}

		public override PacketSenderOwning<ChannelNotify> Invoke(Locale locale = Locale.enUS)
		{
			// LocalizedPacketDo sends client DBC locale, we need to get available to server locale
			Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

			PacketSenderOwning<ChannelNotify> sender = new();
			sender.Data.Type    = _modifier.GetNotificationType();
			sender.Data.Channel = _source.GetName(localeIdx);
			_modifier.Append(sender.Data);
			sender.Data.Write();

			return sender;
		}
	}

	internal class ChannelNotifyJoinedBuilder : MessageBuilder
	{
		private Channel _source;

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
			notify.Data.Channel      = _source.GetName(localeIdx);
			notify.Data.ChannelGUID  = _source.GetGUID();

			return notify;
		}
	}

	internal class ChannelNotifyLeftBuilder : MessageBuilder
	{
		private Channel _source;
		private bool _suspended;

		public ChannelNotifyLeftBuilder(Channel source, bool suspend)
		{
			_source    = source;
			_suspended = suspend;
		}

		public override PacketSenderOwning<ChannelNotifyLeft> Invoke(Locale locale = Locale.enUS)
		{
			Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

			PacketSenderOwning<ChannelNotifyLeft> notify = new();
			notify.Data.Channel       = _source.GetName(localeIdx);
			notify.Data.ChatChannelID = _source.GetChannelId();
			notify.Data.Suspended     = _suspended;

			return notify;
		}
	}

	internal class ChannelSayBuilder : MessageBuilder
	{
		private ObjectGuid _channelGuid;
		private ObjectGuid _guid;
		private Language _lang;

		private Channel _source;
		private string _what;

		public ChannelSayBuilder(Channel source, Language lang, string what, ObjectGuid guid, ObjectGuid channelGuid)
		{
			_source      = source;
			_lang        = lang;
			_what        = what;
			_guid        = guid;
			_channelGuid = channelGuid;
		}

		public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
		{
			Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

			PacketSenderOwning<ChatPkt> packet = new();
			Player                      player = Global.ObjAccessor.FindConnectedPlayer(_guid);

			if (player)
			{
				packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx));
			}
			else
			{
				packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx));
				packet.Data.SenderGUID = _guid;
				packet.Data.TargetGUID = _guid;
			}

			packet.Data.ChannelGUID = _channelGuid;

			return packet;
		}
	}

	internal class ChannelWhisperBuilder : MessageBuilder
	{
		private ObjectGuid _guid;
		private Language _lang;
		private string _prefix;

		private Channel _source;
		private string _what;

		public ChannelWhisperBuilder(Channel source, Language lang, string what, string prefix, ObjectGuid guid)
		{
			_source = source;
			_lang   = lang;
			_what   = what;
			_prefix = prefix;
			_guid   = guid;
		}

		public override PacketSenderOwning<ChatPkt> Invoke(Locale locale = Locale.enUS)
		{
			Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

			PacketSenderOwning<ChatPkt> packet = new();
			Player                      player = Global.ObjAccessor.FindConnectedPlayer(_guid);

			if (player)
			{
				packet.Data.Initialize(ChatMsg.Channel, _lang, player, player, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
			}
			else
			{
				packet.Data.Initialize(ChatMsg.Channel, _lang, null, null, _what, 0, _source.GetName(localeIdx), Locale.enUS, _prefix);
				packet.Data.SenderGUID = _guid;
				packet.Data.TargetGUID = _guid;
			}

			return packet;
		}
	}

	internal class ChannelUserlistAddBuilder : MessageBuilder
	{
		private ObjectGuid _guid;

		private Channel _source;

		public ChannelUserlistAddBuilder(Channel source, ObjectGuid guid)
		{
			_source = source;
			_guid   = guid;
		}

		public override PacketSenderOwning<UserlistAdd> Invoke(Locale locale = Locale.enUS)
		{
			Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

			PacketSenderOwning<UserlistAdd> userlistAdd = new();
			userlistAdd.Data.AddedUserGUID = _guid;
			userlistAdd.Data.ChannelFlags  = _source.GetFlags();
			userlistAdd.Data.UserFlags     = _source.GetPlayerFlags(_guid);
			userlistAdd.Data.ChannelID     = _source.GetChannelId();
			userlistAdd.Data.ChannelName   = _source.GetName(localeIdx);

			return userlistAdd;
		}
	}

	internal class ChannelUserlistUpdateBuilder : MessageBuilder
	{
		private ObjectGuid _guid;

		private Channel _source;

		public ChannelUserlistUpdateBuilder(Channel source, ObjectGuid guid)
		{
			_source = source;
			_guid   = guid;
		}

		public override PacketSenderOwning<UserlistUpdate> Invoke(Locale locale = Locale.enUS)
		{
			Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

			PacketSenderOwning<UserlistUpdate> userlistUpdate = new();
			userlistUpdate.Data.UpdatedUserGUID = _guid;
			userlistUpdate.Data.ChannelFlags    = _source.GetFlags();
			userlistUpdate.Data.UserFlags       = _source.GetPlayerFlags(_guid);
			userlistUpdate.Data.ChannelID       = _source.GetChannelId();
			userlistUpdate.Data.ChannelName     = _source.GetName(localeIdx);

			return userlistUpdate;
		}
	}

	internal class ChannelUserlistRemoveBuilder : MessageBuilder
	{
		private ObjectGuid _guid;

		private Channel _source;

		public ChannelUserlistRemoveBuilder(Channel source, ObjectGuid guid)
		{
			_source = source;
			_guid   = guid;
		}

		public override PacketSenderOwning<UserlistRemove> Invoke(Locale locale = Locale.enUS)
		{
			Locale localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

			PacketSenderOwning<UserlistRemove> userlistRemove = new();
			userlistRemove.Data.RemovedUserGUID = _guid;
			userlistRemove.Data.ChannelFlags    = _source.GetFlags();
			userlistRemove.Data.ChannelID       = _source.GetChannelId();
			userlistRemove.Data.ChannelName     = _source.GetName(localeIdx);

			return userlistRemove;
		}
	}

	//Appenders
	internal struct JoinedAppend : IChannelAppender
	{
		public JoinedAppend(ObjectGuid guid)
		{
			_guid = guid;
		}

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.JoinedNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.LeftNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.YouJoinedNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.YouLeftNotice;
		}

		public void Append(ChannelNotify data)
		{
			data.ChatChannelID = (int)_channel.GetChannelId();
		}

		private Channel _channel;
	}

	internal struct WrongPasswordAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.WrongPasswordNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct NotMemberAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.NotMemberNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct NotModeratorAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.NotModeratorNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct PasswordChangedAppend : IChannelAppender
	{
		public PasswordChangedAppend(ObjectGuid guid)
		{
			_guid = guid;
		}

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.PasswordChangedNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.OwnerChangedNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.PlayerNotFoundNotice;
		}

		public void Append(ChannelNotify data)
		{
			data.Sender = _playerName;
		}

		private string _playerName;
	}

	internal struct NotOwnerAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.NotOwnerNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct ChannelOwnerAppend : IChannelAppender
	{
		public ChannelOwnerAppend(Channel channel, ObjectGuid ownerGuid)
		{
			_channel   = channel;
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

		private Channel _channel;
		private ObjectGuid _ownerGuid;

		private string _ownerName;
	}

	internal struct ModeChangeAppend : IChannelAppender
	{
		public ModeChangeAppend(ObjectGuid guid, ChannelMemberFlags oldFlags, ChannelMemberFlags newFlags)
		{
			_guid     = guid;
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
			data.OldFlags   = _oldFlags;
			data.NewFlags   = _newFlags;
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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.AnnouncementsOnNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.AnnouncementsOffNotice;
		}

		public void Append(ChannelNotify data)
		{
			data.SenderGuid = _guid;
		}

		private ObjectGuid _guid;
	}

	internal struct MutedAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.MutedNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

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

	internal struct BannedAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.BannedNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct PlayerBannedAppend : IChannelAppender
	{
		public PlayerBannedAppend(ObjectGuid moderator, ObjectGuid banned)
		{
			_moderator = moderator;
			_banned    = banned;
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

	internal struct PlayerUnbannedAppend : IChannelAppender
	{
		public PlayerUnbannedAppend(ObjectGuid moderator, ObjectGuid unbanned)
		{
			_moderator = moderator;
			_unbanned  = unbanned;
		}

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.PlayerUnbannedNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.PlayerNotBannedNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.PlayerAlreadyMemberNotice;
		}

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

	internal struct InviteWrongFactionAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.InviteWrongFactionNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct WrongFactionAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.WrongFactionNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct InvalidNameAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.InvalidNameNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct NotModeratedAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.NotModeratedNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct PlayerInvitedAppend : IChannelAppender
	{
		public PlayerInvitedAppend(string playerName)
		{
			_playerName = playerName;
		}

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.PlayerInvitedNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.PlayerInviteBannedNotice;
		}

		public void Append(ChannelNotify data)
		{
			data.Sender = _playerName;
		}

		private string _playerName;
	}

	internal struct ThrottledAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.ThrottledNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct NotInAreaAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.NotInAreaNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct NotInLFGAppend : IChannelAppender
	{
		public ChatNotify GetNotificationType()
		{
			return ChatNotify.NotInLfgNotice;
		}

		public void Append(ChannelNotify data)
		{
		}
	}

	internal struct VoiceOnAppend : IChannelAppender
	{
		public VoiceOnAppend(ObjectGuid guid)
		{
			_guid = guid;
		}

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.VoiceOnNotice;
		}

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

		public ChatNotify GetNotificationType()
		{
			return ChatNotify.VoiceOffNotice;
		}

		public void Append(ChannelNotify data)
		{
			data.SenderGuid = _guid;
		}

		private ObjectGuid _guid;
	}
}