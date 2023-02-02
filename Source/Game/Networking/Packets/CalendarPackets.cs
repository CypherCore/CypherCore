// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;
using Framework.Dynamic;

namespace Game.Networking.Packets
{
    class CalendarGetCalendar : ClientPacket
    {
        public CalendarGetCalendar(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class CalendarGetEvent : ClientPacket
    {
        public CalendarGetEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
        }

        public ulong EventID;
    }

    class CalendarCommunityInviteRequest : ClientPacket
    {
        public CalendarCommunityInviteRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ClubId = _worldPacket.ReadUInt64();
            MinLevel = _worldPacket.ReadUInt8();
            MaxLevel = _worldPacket.ReadUInt8();
            MaxRankOrder = _worldPacket.ReadUInt8();
        }

        public ulong ClubId;
        public byte MinLevel = 1;
        public byte MaxLevel = 100;
        public byte MaxRankOrder;
    }

    class CalendarAddEvent : ClientPacket
    {
        public CalendarAddEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventInfo.Read(_worldPacket);
            MaxSize = _worldPacket.ReadUInt32();
        }

        public uint MaxSize = 100;
        public CalendarAddEventInfo EventInfo = new();
    }

    class CalendarUpdateEvent : ClientPacket
    {
        public CalendarUpdateEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventInfo.Read(_worldPacket);
            MaxSize = _worldPacket.ReadUInt32();
        }

        public uint MaxSize;
        public CalendarUpdateEventInfo EventInfo;
    }

    class CalendarRemoveEvent : ClientPacket
    {
        public CalendarRemoveEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            ClubID = _worldPacket.ReadUInt64();
            Flags = _worldPacket.ReadUInt32();
        }

        public ulong ModeratorID;
        public ulong EventID;
        public ulong ClubID;
        public uint Flags;
    }

    class CalendarCopyEvent : ClientPacket
    {
        public CalendarCopyEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            EventClubID = _worldPacket.ReadUInt64();
            Date = _worldPacket.ReadPackedTime();
        }

        public ulong ModeratorID;
        public ulong EventID;
        public ulong EventClubID;
        public long Date;
    }

    class CalendarInviteAdded : ServerPacket
    {
        public CalendarInviteAdded() : base(ServerOpcodes.CalendarInviteAdded) { }

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

        public ulong InviteID;
        public long ResponseTime;
        public byte Level = 100;
        public ObjectGuid InviteGuid;
        public ulong EventID;
        public byte Type;
        public bool ClearPending;
        public CalendarInviteStatus Status;
    }

    class CalendarSendCalendar : ServerPacket
    {
        public CalendarSendCalendar() : base(ServerOpcodes.CalendarSendCalendar) { }

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

        public long ServerTime;
        public List<CalendarSendCalendarInviteInfo> Invites = new();
        public List<CalendarSendCalendarRaidLockoutInfo> RaidLockouts = new();
        public List<CalendarSendCalendarEventInfo> Events = new();
    }

    class CalendarSendEvent : ServerPacket
    {
        public CalendarSendEvent() : base(ServerOpcodes.CalendarSendEvent) { }

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

        public ObjectGuid OwnerGuid;
        public ObjectGuid EventGuildID;
        public ulong EventID;
        public long Date;
        public long LockDate;
        public CalendarFlags Flags;
        public int TextureID;
        public CalendarEventType GetEventType;
        public CalendarSendEventType EventType;
        public string Description;
        public string EventName;
        public List<CalendarEventInviteInfo> Invites = new();
    }

    class CalendarInviteAlert : ServerPacket
    {
        public CalendarInviteAlert() : base(ServerOpcodes.CalendarInviteAlert) { }

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

        public ObjectGuid OwnerGuid;
        public ObjectGuid EventGuildID;
        public ObjectGuid InvitedByGuid;
        public ulong InviteID;
        public ulong EventID;
        public CalendarFlags Flags;
        public long Date;
        public int TextureID;
        public CalendarInviteStatus Status;
        public CalendarEventType EventType;
        public CalendarModerationRank ModeratorStatus;
        public string EventName;
    }

    class CalendarInvitePkt : ClientPacket
    {
        public CalendarInvitePkt(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            ClubID = _worldPacket.ReadUInt64();

            ushort nameLen = _worldPacket.ReadBits<ushort>(9);
            Creating = _worldPacket.HasBit();
            IsSignUp = _worldPacket.HasBit();

            Name = _worldPacket.ReadString(nameLen);
        }

        public ulong ModeratorID;
        public bool IsSignUp;
        public bool Creating = true;
        public ulong EventID;
        public ulong ClubID;
        public string Name;
    }

    class HandleCalendarRsvp : ClientPacket
    {
        public HandleCalendarRsvp(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            InviteID = _worldPacket.ReadUInt64();
            Status = (CalendarInviteStatus)_worldPacket.ReadUInt8();
        }

        public ulong InviteID;
        public ulong EventID;
        public CalendarInviteStatus Status;
    }

    class CalendarInviteStatusPacket : ServerPacket
    {
        public CalendarInviteStatusPacket() : base(ServerOpcodes.CalendarInviteStatus) { }

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

        public CalendarFlags Flags;
        public ulong EventID;
        public CalendarInviteStatus Status;
        public bool ClearPending;
        public long ResponseTime;
        public long Date;
        public ObjectGuid InviteGuid;
    }

    class CalendarInviteRemoved : ServerPacket
    {
        public CalendarInviteRemoved() : base(ServerOpcodes.CalendarInviteRemoved) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InviteGuid);
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WriteUInt32(Flags);

            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
        }

        public ObjectGuid InviteGuid;
        public ulong EventID;
        public uint Flags;
        public bool ClearPending;
    }

    class CalendarModeratorStatus : ServerPacket
    {
        public CalendarModeratorStatus() : base(ServerOpcodes.CalendarModeratorStatus) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InviteGuid);
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WriteUInt8((byte)Status);

            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
        }

        public ObjectGuid InviteGuid;
        public ulong EventID;
        public CalendarInviteStatus Status;
        public bool ClearPending;
    }

    class CalendarInviteRemovedAlert : ServerPacket
    {
        public CalendarInviteRemovedAlert() : base(ServerOpcodes.CalendarInviteRemovedAlert) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WritePackedTime(Date);
            _worldPacket.WriteUInt32((uint)Flags);
            _worldPacket.WriteUInt8((byte)Status);
        }

        public ulong EventID;
        public long Date;
        public CalendarFlags Flags;
        public CalendarInviteStatus Status;
    }

    class CalendarClearPendingAction : ServerPacket
    {
        public CalendarClearPendingAction() : base(ServerOpcodes.CalendarClearPendingAction) { }

        public override void Write() { }
    }

    class CalendarEventUpdatedAlert : ServerPacket
    {
        public CalendarEventUpdatedAlert() : base(ServerOpcodes.CalendarEventUpdatedAlert) { }

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

        public ulong EventID;
        public long Date;
        public CalendarFlags Flags;
        public long LockDate;
        public long OriginalDate;
        public int TextureID;
        public CalendarEventType EventType;
        public bool ClearPending;
        public string Description;
        public string EventName;
    }

    class CalendarEventRemovedAlert : ServerPacket
    {
        public CalendarEventRemovedAlert() : base(ServerOpcodes.CalendarEventRemovedAlert) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WritePackedTime(Date);

            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
        }

        public ulong EventID;
        public long Date;
        public bool ClearPending;
    }

    class CalendarSendNumPending : ServerPacket
    {
        public CalendarSendNumPending(uint numPending) : base(ServerOpcodes.CalendarSendNumPending)
        {
            NumPending = numPending;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(NumPending);
        }

        public uint NumPending;
    }

    class CalendarGetNumPending : ClientPacket
    {
        public CalendarGetNumPending(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class CalendarEventSignUp : ClientPacket
    {
        public CalendarEventSignUp(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            ClubID = _worldPacket.ReadUInt64();
            Tentative = _worldPacket.HasBit();
        }

        public bool Tentative;
        public ulong EventID;
        public ulong ClubID;
    }

    class CalendarRemoveInvite : ClientPacket
    {
        public CalendarRemoveInvite(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            InviteID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            EventID = _worldPacket.ReadUInt64();
        }

        public ObjectGuid Guid;
        public ulong EventID;
        public ulong ModeratorID;
        public ulong InviteID;
    }

    class CalendarStatus : ClientPacket
    {
        public CalendarStatus(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            EventID = _worldPacket.ReadUInt64();
            InviteID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            Status = _worldPacket.ReadUInt8();
        }

        public ObjectGuid Guid;
        public ulong EventID;
        public ulong ModeratorID;
        public ulong InviteID;
        public byte Status;
    }

    class SetSavedInstanceExtend : ClientPacket
    {
        public SetSavedInstanceExtend(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MapID = _worldPacket.ReadInt32();
            DifficultyID = _worldPacket.ReadUInt32();
            Extend = _worldPacket.HasBit();
        }

        public int MapID;
        public bool Extend;
        public uint DifficultyID;
    }

    class CalendarModeratorStatusQuery : ClientPacket
    {
        public CalendarModeratorStatusQuery(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            EventID = _worldPacket.ReadUInt64();
            InviteID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            Status = _worldPacket.ReadUInt8();
        }

        public ObjectGuid Guid;
        public ulong EventID;
        public ulong InviteID;
        public ulong ModeratorID;
        public byte Status;
    }

    class CalendarCommandResult : ServerPacket
    {
        public CalendarCommandResult() : base(ServerOpcodes.CalendarCommandResult) { }
        public CalendarCommandResult(byte command, CalendarError result, string name) : base(ServerOpcodes.CalendarCommandResult)
        {
            Command = command;
            Result = result;
            Name = name;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Command);
            _worldPacket.WriteUInt8((byte)Result);

            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Name);
        }

        public byte Command;
        public CalendarError Result;
        public string Name;
    }

    class CalendarRaidLockoutAdded : ServerPacket
    {
        public CalendarRaidLockoutAdded() : base(ServerOpcodes.CalendarRaidLockoutAdded) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(InstanceID);
            _worldPacket.WriteUInt32(ServerTime);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32((uint)DifficultyID);
            _worldPacket.WriteInt32(TimeRemaining);
        }

        public ulong InstanceID;
        public Difficulty DifficultyID;
        public int TimeRemaining;
        public uint ServerTime;
        public int MapID;
    }

    class CalendarRaidLockoutRemoved : ServerPacket
    {
        public CalendarRaidLockoutRemoved() : base(ServerOpcodes.CalendarRaidLockoutRemoved) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(InstanceID);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32((uint)DifficultyID);
        }

        public ulong InstanceID;
        public int MapID;
        public Difficulty DifficultyID;
    }

    class CalendarRaidLockoutUpdated : ServerPacket
    {
        public CalendarRaidLockoutUpdated() : base(ServerOpcodes.CalendarRaidLockoutUpdated) { }

        public override void Write()
        {
            _worldPacket.WritePackedTime(ServerTime);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32(DifficultyID);
            _worldPacket.WriteInt32(OldTimeRemaining);
            _worldPacket.WriteInt32(NewTimeRemaining);
        }

        public long ServerTime;
        public int MapID;
        public uint DifficultyID;
        public int NewTimeRemaining;
        public int OldTimeRemaining;
    }

    class CalendarCommunityInvite : ServerPacket
    {
        public CalendarCommunityInvite() : base(ServerOpcodes.CalendarCommunityInvite) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Invites.Count);
            foreach (var invite in Invites)
            {
                _worldPacket.WritePackedGuid(invite.InviteGuid);
                _worldPacket.WriteUInt8(invite.Level);
            }
        }

        public List<CalendarEventInitialInviteInfo> Invites = new();
    }

    class CalendarInviteStatusAlert : ServerPacket
    {
        public CalendarInviteStatusAlert() : base(ServerOpcodes.CalendarInviteStatusAlert) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WritePackedTime(Date);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt8(Status);
        }

        public ulong EventID;
        public uint Flags;
        public long Date;
        public byte Status;
    }

    class CalendarInviteNotesAlert : ServerPacket
    {
        public CalendarInviteNotesAlert(ulong eventID, string notes) : base(ServerOpcodes.CalendarInviteNotesAlert)
        {
            EventID = eventID;
            Notes = notes;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt64(EventID);

            _worldPacket.WriteBits(Notes.GetByteCount(), 8);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Notes);
        }

        public ulong EventID;
        public string Notes;
    }

    class CalendarInviteNotes : ServerPacket
    {
        public CalendarInviteNotes() : base(ServerOpcodes.CalendarInviteNotes) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InviteGuid);
            _worldPacket.WriteUInt64(EventID);

            _worldPacket.WriteBits(Notes.GetByteCount(), 8);
            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Notes);
        }

        public ObjectGuid InviteGuid;
        public ulong EventID;
        public string Notes = "";
        public bool ClearPending;
    }

    class CalendarComplain : ClientPacket
    {
        public CalendarComplain(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            InvitedByGUID = _worldPacket.ReadPackedGuid();
            EventID = _worldPacket.ReadUInt64();
            InviteID = _worldPacket.ReadUInt64();
        }

        ObjectGuid InvitedByGUID;
        ulong InviteID;
        ulong EventID;
    }

    //Structs
    struct CalendarAddEventInviteInfo
    {
        public void Read(WorldPacket data)
        {
            Guid = data.ReadPackedGuid();
            Status = data.ReadUInt8();
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

    class CalendarAddEventInfo
    {
        public void Read(WorldPacket data)
        {
            ClubId = data.ReadUInt64();
            EventType = data.ReadUInt8();
            TextureID = data.ReadInt32();
            Time = data.ReadPackedTime();
            Flags = data.ReadUInt32();
            var InviteCount = data.ReadUInt32();

            byte titleLength = data.ReadBits<byte>(8);
            ushort descriptionLength = data.ReadBits<ushort>(11);

            for (var i = 0; i < InviteCount; ++i)
            {
                CalendarAddEventInviteInfo invite = new();
                invite.Read(data);
                Invites[i] = invite;
            }

            Title = data.ReadString(titleLength);
            Description = data.ReadString(descriptionLength);
        }

        public ulong ClubId;
        public string Title;
        public string Description;
        public byte EventType;
        public int TextureID;
        public long Time;
        public uint Flags;
        public CalendarAddEventInviteInfo[] Invites = new CalendarAddEventInviteInfo[(int)SharedConst.CalendarMaxInvites];
    }

    struct CalendarUpdateEventInfo
    {
        public void Read(WorldPacket data)
        {
            ClubID = data.ReadUInt64();
            EventID = data.ReadUInt64();
            ModeratorID = data.ReadUInt64();
            EventType = data.ReadUInt8();
            TextureID = data.ReadUInt32();
            Time = data.ReadPackedTime();
            Flags = data.ReadUInt32();

            byte titleLen = data.ReadBits<byte>(8);
            ushort descLen = data.ReadBits<ushort>(11);

            Title = data.ReadString(titleLen);
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

    struct CalendarSendCalendarInviteInfo
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
    struct CalendarSendCalendarRaidLockoutInfo
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

    struct CalendarSendCalendarEventInfo
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

    class CalendarEventInviteInfo
    {
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

        public ObjectGuid Guid;
        public ulong InviteID;
        public long ResponseTime;
        public byte Level = 1;
        public CalendarInviteStatus Status;
        public CalendarModerationRank Moderator;
        public byte InviteType;
        public string Notes;
    }

    class CalendarEventInitialInviteInfo
    {
        public CalendarEventInitialInviteInfo(ObjectGuid inviteGuid, byte level)
        {
            InviteGuid = inviteGuid;
            Level = level;
        }

        public ObjectGuid InviteGuid;
        public byte Level = 100;
    }
}
