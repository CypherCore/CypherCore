// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	internal class CalendarGetCalendar : ClientPacket
	{
		public CalendarGetCalendar(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class CalendarGetEvent : ClientPacket
	{
		public ulong EventID;

		public CalendarGetEvent(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EventID = _worldPacket.ReadUInt64();
		}
	}

	internal class CalendarCommunityInviteRequest : ClientPacket
	{
		public ulong ClubId;
		public byte MaxLevel = 100;
		public byte MaxRankOrder;
		public byte MinLevel = 1;

		public CalendarCommunityInviteRequest(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			ClubId       = _worldPacket.ReadUInt64();
			MinLevel     = _worldPacket.ReadUInt8();
			MaxLevel     = _worldPacket.ReadUInt8();
			MaxRankOrder = _worldPacket.ReadUInt8();
		}
	}

	internal class CalendarAddEvent : ClientPacket
	{
		public CalendarAddEventInfo EventInfo = new();

		public uint MaxSize = 100;

		public CalendarAddEvent(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EventInfo.Read(_worldPacket);
			MaxSize = _worldPacket.ReadUInt32();
		}
	}

	internal class CalendarUpdateEvent : ClientPacket
	{
		public CalendarUpdateEventInfo EventInfo;

		public uint MaxSize;

		public CalendarUpdateEvent(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EventInfo.Read(_worldPacket);
			MaxSize = _worldPacket.ReadUInt32();
		}
	}

	internal class CalendarRemoveEvent : ClientPacket
	{
		public ulong ClubID;
		public ulong EventID;
		public uint Flags;

		public ulong ModeratorID;

		public CalendarRemoveEvent(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EventID     = _worldPacket.ReadUInt64();
			ModeratorID = _worldPacket.ReadUInt64();
			ClubID      = _worldPacket.ReadUInt64();
			Flags       = _worldPacket.ReadUInt32();
		}
	}

	internal class CalendarCopyEvent : ClientPacket
	{
		public long Date;
		public ulong EventClubID;
		public ulong EventID;

		public ulong ModeratorID;

		public CalendarCopyEvent(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EventID     = _worldPacket.ReadUInt64();
			ModeratorID = _worldPacket.ReadUInt64();
			EventClubID = _worldPacket.ReadUInt64();
			Date        = _worldPacket.ReadPackedTime();
		}
	}

	internal class CalendarInviteAdded : ServerPacket
	{
		public bool ClearPending;
		public ulong EventID;
		public ObjectGuid InviteGuid;

		public ulong InviteID;
		public byte Level = 100;
		public long ResponseTime;
		public CalendarInviteStatus Status;
		public byte Type;

		public CalendarInviteAdded() : base(ServerOpcodes.CalendarInviteAdded)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(InviteGuid);
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WriteUInt64(InviteID);
			_worldPacket.WriteUInt8(Level);
			_worldPacket.WriteUInt8((byte)Status);
			_worldPacket.WriteUInt8(Type);
			_worldPacket.WritePackedTime(ResponseTime);

			_worldPacket.WriteBit(ClearPending);
			_worldPacket.FlushBits();
		}
	}

	internal class CalendarSendCalendar : ServerPacket
	{
		public List<CalendarSendCalendarEventInfo> Events = new();
		public List<CalendarSendCalendarInviteInfo> Invites = new();
		public List<CalendarSendCalendarRaidLockoutInfo> RaidLockouts = new();

		public long ServerTime;

		public CalendarSendCalendar() : base(ServerOpcodes.CalendarSendCalendar)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedTime(ServerTime);
			_worldPacket.WriteInt32(Invites.Count);
			_worldPacket.WriteInt32(Events.Count);
			_worldPacket.WriteInt32(RaidLockouts.Count);

			foreach (var invite in Invites)
				invite.Write(_worldPacket);

			foreach (var lockout in RaidLockouts)
				lockout.Write(_worldPacket);

			foreach (var Event in Events)
				Event.Write(_worldPacket);
		}
	}

	internal class CalendarSendEvent : ServerPacket
	{
		public long Date;
		public string Description;
		public ObjectGuid EventGuildID;
		public ulong EventID;
		public string EventName;
		public CalendarSendEventType EventType;
		public CalendarFlags Flags;
		public CalendarEventType GetEventType;
		public List<CalendarEventInviteInfo> Invites = new();
		public long LockDate;

		public ObjectGuid OwnerGuid;
		public int TextureID;

		public CalendarSendEvent() : base(ServerOpcodes.CalendarSendEvent)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt8((byte)EventType);
			_worldPacket.WritePackedGuid(OwnerGuid);
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WriteUInt8((byte)GetEventType);
			_worldPacket.WriteInt32(TextureID);
			_worldPacket.WriteUInt32((uint)Flags);
			_worldPacket.WritePackedTime(Date);
			_worldPacket.WriteUInt32((uint)LockDate);
			_worldPacket.WritePackedGuid(EventGuildID);
			_worldPacket.WriteInt32(Invites.Count);

			_worldPacket.WriteBits(EventName.GetByteCount(), 8);
			_worldPacket.WriteBits(Description.GetByteCount(), 11);
			_worldPacket.FlushBits();

			foreach (var invite in Invites)
				invite.Write(_worldPacket);

			_worldPacket.WriteString(EventName);
			_worldPacket.WriteString(Description);
		}
	}

	internal class CalendarInviteAlert : ServerPacket
	{
		public long Date;
		public ObjectGuid EventGuildID;
		public ulong EventID;
		public string EventName;
		public CalendarEventType EventType;
		public CalendarFlags Flags;
		public ObjectGuid InvitedByGuid;
		public ulong InviteID;
		public CalendarModerationRank ModeratorStatus;

		public ObjectGuid OwnerGuid;
		public CalendarInviteStatus Status;
		public int TextureID;

		public CalendarInviteAlert() : base(ServerOpcodes.CalendarInviteAlert)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WritePackedTime(Date);
			_worldPacket.WriteUInt32((uint)Flags);
			_worldPacket.WriteUInt8((byte)EventType);
			_worldPacket.WriteInt32(TextureID);
			_worldPacket.WritePackedGuid(EventGuildID);
			_worldPacket.WriteUInt64(InviteID);
			_worldPacket.WriteUInt8((byte)Status);
			_worldPacket.WriteUInt8((byte)ModeratorStatus);

			// Todo: check order
			_worldPacket.WritePackedGuid(InvitedByGuid);
			_worldPacket.WritePackedGuid(OwnerGuid);

			_worldPacket.WriteBits(EventName.GetByteCount(), 8);
			_worldPacket.FlushBits();
			_worldPacket.WriteString(EventName);
		}
	}

	internal class CalendarInvitePkt : ClientPacket
	{
		public ulong ClubID;
		public bool Creating = true;
		public ulong EventID;
		public bool IsSignUp;

		public ulong ModeratorID;
		public string Name;

		public CalendarInvitePkt(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EventID     = _worldPacket.ReadUInt64();
			ModeratorID = _worldPacket.ReadUInt64();
			ClubID      = _worldPacket.ReadUInt64();

			ushort nameLen = _worldPacket.ReadBits<ushort>(9);
			Creating = _worldPacket.HasBit();
			IsSignUp = _worldPacket.HasBit();

			Name = _worldPacket.ReadString(nameLen);
		}
	}

	internal class HandleCalendarRsvp : ClientPacket
	{
		public ulong EventID;

		public ulong InviteID;
		public CalendarInviteStatus Status;

		public HandleCalendarRsvp(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EventID  = _worldPacket.ReadUInt64();
			InviteID = _worldPacket.ReadUInt64();
			Status   = (CalendarInviteStatus)_worldPacket.ReadUInt8();
		}
	}

	internal class CalendarInviteStatusPacket : ServerPacket
	{
		public bool ClearPending;
		public long Date;
		public ulong EventID;

		public CalendarFlags Flags;
		public ObjectGuid InviteGuid;
		public long ResponseTime;
		public CalendarInviteStatus Status;

		public CalendarInviteStatusPacket() : base(ServerOpcodes.CalendarInviteStatus)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(InviteGuid);
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WritePackedTime(Date);
			_worldPacket.WriteUInt32((uint)Flags);
			_worldPacket.WriteUInt8((byte)Status);
			_worldPacket.WritePackedTime(ResponseTime);

			_worldPacket.WriteBit(ClearPending);
			_worldPacket.FlushBits();
		}
	}

	internal class CalendarInviteRemoved : ServerPacket
	{
		public bool ClearPending;
		public ulong EventID;
		public uint Flags;

		public ObjectGuid InviteGuid;

		public CalendarInviteRemoved() : base(ServerOpcodes.CalendarInviteRemoved)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(InviteGuid);
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WriteUInt32(Flags);

			_worldPacket.WriteBit(ClearPending);
			_worldPacket.FlushBits();
		}
	}

	internal class CalendarModeratorStatus : ServerPacket
	{
		public bool ClearPending;
		public ulong EventID;

		public ObjectGuid InviteGuid;
		public CalendarInviteStatus Status;

		public CalendarModeratorStatus() : base(ServerOpcodes.CalendarModeratorStatus)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(InviteGuid);
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WriteUInt8((byte)Status);

			_worldPacket.WriteBit(ClearPending);
			_worldPacket.FlushBits();
		}
	}

	internal class CalendarInviteRemovedAlert : ServerPacket
	{
		public long Date;

		public ulong EventID;
		public CalendarFlags Flags;
		public CalendarInviteStatus Status;

		public CalendarInviteRemovedAlert() : base(ServerOpcodes.CalendarInviteRemovedAlert)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WritePackedTime(Date);
			_worldPacket.WriteUInt32((uint)Flags);
			_worldPacket.WriteUInt8((byte)Status);
		}
	}

	internal class CalendarClearPendingAction : ServerPacket
	{
		public CalendarClearPendingAction() : base(ServerOpcodes.CalendarClearPendingAction)
		{
		}

		public override void Write()
		{
		}
	}

	internal class CalendarEventUpdatedAlert : ServerPacket
	{
		public bool ClearPending;
		public long Date;
		public string Description;

		public ulong EventID;
		public string EventName;
		public CalendarEventType EventType;
		public CalendarFlags Flags;
		public long LockDate;
		public long OriginalDate;
		public int TextureID;

		public CalendarEventUpdatedAlert() : base(ServerOpcodes.CalendarEventUpdatedAlert)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(EventID);

			_worldPacket.WritePackedTime(OriginalDate);
			_worldPacket.WritePackedTime(Date);
			_worldPacket.WriteUInt32((uint)LockDate);
			_worldPacket.WriteUInt32((uint)Flags);
			_worldPacket.WriteInt32(TextureID);
			_worldPacket.WriteUInt8((byte)EventType);

			_worldPacket.WriteBits(EventName.GetByteCount(), 8);
			_worldPacket.WriteBits(Description.GetByteCount(), 11);
			_worldPacket.WriteBit(ClearPending);
			_worldPacket.FlushBits();

			_worldPacket.WriteString(EventName);
			_worldPacket.WriteString(Description);
		}
	}

	internal class CalendarEventRemovedAlert : ServerPacket
	{
		public bool ClearPending;
		public long Date;

		public ulong EventID;

		public CalendarEventRemovedAlert() : base(ServerOpcodes.CalendarEventRemovedAlert)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WritePackedTime(Date);

			_worldPacket.WriteBit(ClearPending);
			_worldPacket.FlushBits();
		}
	}

	internal class CalendarSendNumPending : ServerPacket
	{
		public uint NumPending;

		public CalendarSendNumPending(uint numPending) : base(ServerOpcodes.CalendarSendNumPending)
		{
			NumPending = numPending;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(NumPending);
		}
	}

	internal class CalendarGetNumPending : ClientPacket
	{
		public CalendarGetNumPending(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class CalendarEventSignUp : ClientPacket
	{
		public ulong ClubID;
		public ulong EventID;

		public bool Tentative;

		public CalendarEventSignUp(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			EventID   = _worldPacket.ReadUInt64();
			ClubID    = _worldPacket.ReadUInt64();
			Tentative = _worldPacket.HasBit();
		}
	}

	internal class CalendarRemoveInvite : ClientPacket
	{
		public ulong EventID;

		public ObjectGuid Guid;
		public ulong InviteID;
		public ulong ModeratorID;

		public CalendarRemoveInvite(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Guid        = _worldPacket.ReadPackedGuid();
			InviteID    = _worldPacket.ReadUInt64();
			ModeratorID = _worldPacket.ReadUInt64();
			EventID     = _worldPacket.ReadUInt64();
		}
	}

	internal class CalendarStatus : ClientPacket
	{
		public ulong EventID;

		public ObjectGuid Guid;
		public ulong InviteID;
		public ulong ModeratorID;
		public byte Status;

		public CalendarStatus(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Guid        = _worldPacket.ReadPackedGuid();
			EventID     = _worldPacket.ReadUInt64();
			InviteID    = _worldPacket.ReadUInt64();
			ModeratorID = _worldPacket.ReadUInt64();
			Status      = _worldPacket.ReadUInt8();
		}
	}

	internal class SetSavedInstanceExtend : ClientPacket
	{
		public uint DifficultyID;
		public bool Extend;

		public int MapID;

		public SetSavedInstanceExtend(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			MapID        = _worldPacket.ReadInt32();
			DifficultyID = _worldPacket.ReadUInt32();
			Extend       = _worldPacket.HasBit();
		}
	}

	internal class CalendarModeratorStatusQuery : ClientPacket
	{
		public ulong EventID;

		public ObjectGuid Guid;
		public ulong InviteID;
		public ulong ModeratorID;
		public byte Status;

		public CalendarModeratorStatusQuery(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Guid        = _worldPacket.ReadPackedGuid();
			EventID     = _worldPacket.ReadUInt64();
			InviteID    = _worldPacket.ReadUInt64();
			ModeratorID = _worldPacket.ReadUInt64();
			Status      = _worldPacket.ReadUInt8();
		}
	}

	internal class CalendarCommandResult : ServerPacket
	{
		public byte Command;
		public string Name;
		public CalendarError Result;

		public CalendarCommandResult() : base(ServerOpcodes.CalendarCommandResult)
		{
		}

		public CalendarCommandResult(byte command, CalendarError result, string name) : base(ServerOpcodes.CalendarCommandResult)
		{
			Command = command;
			Result  = result;
			Name    = name;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt8(Command);
			_worldPacket.WriteUInt8((byte)Result);

			_worldPacket.WriteBits(Name.GetByteCount(), 9);
			_worldPacket.FlushBits();
			_worldPacket.WriteString(Name);
		}
	}

	internal class CalendarRaidLockoutAdded : ServerPacket
	{
		public Difficulty DifficultyID;

		public ulong InstanceID;
		public int MapID;
		public uint ServerTime;
		public int TimeRemaining;

		public CalendarRaidLockoutAdded() : base(ServerOpcodes.CalendarRaidLockoutAdded)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(InstanceID);
			_worldPacket.WriteUInt32(ServerTime);
			_worldPacket.WriteInt32(MapID);
			_worldPacket.WriteUInt32((uint)DifficultyID);
			_worldPacket.WriteInt32(TimeRemaining);
		}
	}

	internal class CalendarRaidLockoutRemoved : ServerPacket
	{
		public Difficulty DifficultyID;

		public ulong InstanceID;
		public int MapID;

		public CalendarRaidLockoutRemoved() : base(ServerOpcodes.CalendarRaidLockoutRemoved)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(InstanceID);
			_worldPacket.WriteInt32(MapID);
			_worldPacket.WriteUInt32((uint)DifficultyID);
		}
	}

	internal class CalendarRaidLockoutUpdated : ServerPacket
	{
		public uint DifficultyID;
		public int MapID;
		public int NewTimeRemaining;
		public int OldTimeRemaining;

		public long ServerTime;

		public CalendarRaidLockoutUpdated() : base(ServerOpcodes.CalendarRaidLockoutUpdated)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedTime(ServerTime);
			_worldPacket.WriteInt32(MapID);
			_worldPacket.WriteUInt32(DifficultyID);
			_worldPacket.WriteInt32(OldTimeRemaining);
			_worldPacket.WriteInt32(NewTimeRemaining);
		}
	}

	internal class CalendarCommunityInvite : ServerPacket
	{
		public List<CalendarEventInitialInviteInfo> Invites = new();

		public CalendarCommunityInvite() : base(ServerOpcodes.CalendarCommunityInvite)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Invites.Count);

			foreach (var invite in Invites)
			{
				_worldPacket.WritePackedGuid(invite.InviteGuid);
				_worldPacket.WriteUInt8(invite.Level);
			}
		}
	}

	internal class CalendarInviteStatusAlert : ServerPacket
	{
		public long Date;

		public ulong EventID;
		public uint Flags;
		public byte Status;

		public CalendarInviteStatusAlert() : base(ServerOpcodes.CalendarInviteStatusAlert)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(EventID);
			_worldPacket.WritePackedTime(Date);
			_worldPacket.WriteUInt32(Flags);
			_worldPacket.WriteUInt8(Status);
		}
	}

	internal class CalendarInviteNotesAlert : ServerPacket
	{
		public ulong EventID;
		public string Notes;

		public CalendarInviteNotesAlert(ulong eventID, string notes) : base(ServerOpcodes.CalendarInviteNotesAlert)
		{
			EventID = eventID;
			Notes   = notes;
		}

		public override void Write()
		{
			_worldPacket.WriteUInt64(EventID);

			_worldPacket.WriteBits(Notes.GetByteCount(), 8);
			_worldPacket.FlushBits();
			_worldPacket.WriteString(Notes);
		}
	}

	internal class CalendarInviteNotes : ServerPacket
	{
		public bool ClearPending;
		public ulong EventID;

		public ObjectGuid InviteGuid;
		public string Notes = "";

		public CalendarInviteNotes() : base(ServerOpcodes.CalendarInviteNotes)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(InviteGuid);
			_worldPacket.WriteUInt64(EventID);

			_worldPacket.WriteBits(Notes.GetByteCount(), 8);
			_worldPacket.WriteBit(ClearPending);
			_worldPacket.FlushBits();
			_worldPacket.WriteString(Notes);
		}
	}

	internal class CalendarComplain : ClientPacket
	{
		private ulong EventID;

		private ObjectGuid InvitedByGUID;
		private ulong InviteID;

		public CalendarComplain(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			InvitedByGUID = _worldPacket.ReadPackedGuid();
			EventID       = _worldPacket.ReadUInt64();
			InviteID      = _worldPacket.ReadUInt64();
		}
	}

	//Structs
	internal struct CalendarAddEventInviteInfo
	{
		public void Read(WorldPacket data)
		{
			Guid      = data.ReadPackedGuid();
			Status    = data.ReadUInt8();
			Moderator = data.ReadUInt8();

			bool hasUnused801_1 = data.HasBit();
			bool hasUnused801_2 = data.HasBit();
			bool hasUnused801_3 = data.HasBit();

			if (hasUnused801_1)
				Unused801_1 = data.ReadPackedGuid();

			if (hasUnused801_2)
				Unused801_2 = data.ReadUInt64();

			if (hasUnused801_3)
				Unused801_3 = data.ReadUInt64();
		}

		public ObjectGuid Guid;
		public byte Status;
		public byte Moderator;
		public ObjectGuid? Unused801_1;
		public ulong? Unused801_2;
		public ulong? Unused801_3;
	}

	internal class CalendarAddEventInfo
	{
		public ulong ClubId;
		public string Description;
		public byte EventType;
		public uint Flags;
		public CalendarAddEventInviteInfo[] Invites = new CalendarAddEventInviteInfo[(int)SharedConst.CalendarMaxInvites];
		public int TextureID;
		public long Time;
		public string Title;

		public void Read(WorldPacket data)
		{
			ClubId    = data.ReadUInt64();
			EventType = data.ReadUInt8();
			TextureID = data.ReadInt32();
			Time      = data.ReadPackedTime();
			Flags     = data.ReadUInt32();
			var InviteCount = data.ReadUInt32();

			byte   titleLength       = data.ReadBits<byte>(8);
			ushort descriptionLength = data.ReadBits<ushort>(11);

			for (var i = 0; i < InviteCount; ++i)
			{
				CalendarAddEventInviteInfo invite = new();
				invite.Read(data);
				Invites[i] = invite;
			}

			Title       = data.ReadString(titleLength);
			Description = data.ReadString(descriptionLength);
		}
	}

	internal struct CalendarUpdateEventInfo
	{
		public void Read(WorldPacket data)
		{
			ClubID      = data.ReadUInt64();
			EventID     = data.ReadUInt64();
			ModeratorID = data.ReadUInt64();
			EventType   = data.ReadUInt8();
			TextureID   = data.ReadUInt32();
			Time        = data.ReadPackedTime();
			Flags       = data.ReadUInt32();

			byte   titleLen = data.ReadBits<byte>(8);
			ushort descLen  = data.ReadBits<ushort>(11);

			Title       = data.ReadString(titleLen);
			Description = data.ReadString(descLen);
		}

		public ulong ClubID;
		public ulong EventID;
		public ulong ModeratorID;
		public string Title;
		public string Description;
		public byte EventType;
		public uint TextureID;
		public long Time;
		public uint Flags;
	}

	internal struct CalendarSendCalendarInviteInfo
	{
		public void Write(WorldPacket data)
		{
			data.WriteUInt64(EventID);
			data.WriteUInt64(InviteID);
			data.WriteUInt8((byte)Status);
			data.WriteUInt8((byte)Moderator);
			data.WriteUInt8(InviteType);
			data.WritePackedGuid(InviterGuid);
		}

		public ulong EventID;
		public ulong InviteID;
		public ObjectGuid InviterGuid;
		public CalendarInviteStatus Status;
		public CalendarModerationRank Moderator;
		public byte InviteType;
	}

	internal struct CalendarSendCalendarRaidLockoutInfo
	{
		public void Write(WorldPacket data)
		{
			data.WriteUInt64(InstanceID);
			data.WriteInt32(MapID);
			data.WriteUInt32(DifficultyID);
			data.WriteInt32(ExpireTime);
		}

		public ulong InstanceID;
		public int MapID;
		public uint DifficultyID;
		public int ExpireTime;
	}

	internal struct CalendarSendCalendarEventInfo
	{
		public void Write(WorldPacket data)
		{
			data.WriteUInt64(EventID);
			data.WriteUInt8((byte)EventType);
			data.WritePackedTime(Date);
			data.WriteUInt32((uint)Flags);
			data.WriteInt32(TextureID);
			data.WriteUInt64(EventClubID);
			data.WritePackedGuid(OwnerGuid);

			data.WriteBits(EventName.GetByteCount(), 8);
			data.FlushBits();
			data.WriteString(EventName);
		}

		public ulong EventID;
		public string EventName;
		public CalendarEventType EventType;
		public long Date;
		public CalendarFlags Flags;
		public int TextureID;
		public ulong EventClubID;
		public ObjectGuid OwnerGuid;
	}

	internal class CalendarEventInviteInfo
	{
		public ObjectGuid Guid;
		public ulong InviteID;
		public byte InviteType;
		public byte Level = 1;
		public CalendarModerationRank Moderator;
		public string Notes;
		public long ResponseTime;
		public CalendarInviteStatus Status;

		public void Write(WorldPacket data)
		{
			data.WritePackedGuid(Guid);
			data.WriteUInt64(InviteID);

			data.WriteUInt8(Level);
			data.WriteUInt8((byte)Status);
			data.WriteUInt8((byte)Moderator);
			data.WriteUInt8(InviteType);

			data.WritePackedTime(ResponseTime);

			data.WriteBits(Notes.GetByteCount(), 8);
			data.FlushBits();
			data.WriteString(Notes);
		}
	}

	internal class CalendarEventInitialInviteInfo
	{
		public ObjectGuid InviteGuid;
		public byte Level = 100;

		public CalendarEventInitialInviteInfo(ObjectGuid inviteGuid, byte level)
		{
			InviteGuid = inviteGuid;
			Level      = level;
		}
	}
}