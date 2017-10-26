/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using System.Collections.Generic;

namespace Game.Network.Packets
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

        public ulong EventID { get; set; }
    }

    class CalendarGuildFilter : ClientPacket
    {
        public CalendarGuildFilter(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MinLevel = _worldPacket.ReadUInt8();
            MaxLevel = _worldPacket.ReadUInt8();
            MaxRankOrder = _worldPacket.ReadUInt8();
        }

        public byte MinLevel { get; set; } = 1;
        public byte MaxLevel { get; set; } = 100;
        public byte MaxRankOrder { get; set; }
    }

    class CalendarAddEvent : ClientPacket
    {
        public CalendarAddEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventInfo.Read(_worldPacket);
            MaxSize = _worldPacket.ReadUInt32();
        }

        public uint MaxSize { get; set; } = 100;
        public CalendarAddEventInfo EventInfo { get; set; } = new CalendarAddEventInfo();
    }

    class CalendarUpdateEvent : ClientPacket
    {
        public CalendarUpdateEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventInfo.EventID = _worldPacket.ReadUInt64();
            EventInfo.ModeratorID = _worldPacket.ReadUInt64();
            EventInfo.EventType = _worldPacket.ReadUInt8();
            EventInfo.TextureID = _worldPacket.ReadUInt32();
            EventInfo.Time = _worldPacket.ReadPackedTime();
            EventInfo.Flags = _worldPacket.ReadUInt32();

            byte titleLen = _worldPacket.ReadBits<byte>(8);
            ushort descLen = _worldPacket.ReadBits<ushort>(11);

            EventInfo.Title = _worldPacket.ReadString(titleLen);
            EventInfo.Description = _worldPacket.ReadString(descLen);
            MaxSize = _worldPacket.ReadUInt32();
        }

        public uint MaxSize { get; set; }
        public CalendarUpdateEventInfo EventInfo;
    }

    class CalendarRemoveEvent : ClientPacket
    {
        public CalendarRemoveEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            Flags = _worldPacket.ReadUInt32();
        }

        public ulong ModeratorID { get; set; }
        public ulong EventID { get; set; }
        public uint Flags { get; set; }
    }

    class CalendarCopyEvent : ClientPacket
    {
        public CalendarCopyEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            Date = _worldPacket.ReadPackedTime();
        }

        public ulong ModeratorID { get; set; }
        public ulong EventID { get; set; }
        public long Date { get; set; }
    }

    class SCalendarEventInvite : ServerPacket
    {
        public SCalendarEventInvite() : base(ServerOpcodes.CalendarEventInvite) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InviteGuid);
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WriteUInt64(InviteID);
            _worldPacket.WriteUInt8(Level);
            _worldPacket.WriteUInt8(Status);
            _worldPacket.WriteUInt8(Type);
            _worldPacket.WritePackedTime(ResponseTime);

            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
        }

        public ulong InviteID { get; set; }
        public long ResponseTime { get; set; }
        public byte Level { get; set; } = 100;
        public ObjectGuid InviteGuid { get; set; }
        public ulong EventID { get; set; }
        public byte Type { get; set; }
        public bool ClearPending { get; set; }
        public CalendarInviteStatus Status { get; set; }
    }

    class CalendarSendCalendar : ServerPacket
    {
        public CalendarSendCalendar() : base(ServerOpcodes.CalendarSendCalendar) { }

        public override void Write()
        {
            _worldPacket.WritePackedTime(ServerTime);
            _worldPacket.WriteUInt32(Invites.Count);
            _worldPacket.WriteUInt32(Events.Count);
            _worldPacket.WriteUInt32(RaidLockouts.Count);

            foreach (var invite in Invites)
                invite.Write(_worldPacket);

            foreach (var lockout in RaidLockouts)
                lockout.Write(_worldPacket);

            foreach (var Event in Events)
                Event.Write(_worldPacket);
        }

        public long ServerTime { get; set; }
        public List<CalendarSendCalendarInviteInfo> Invites { get; set; } = new List<CalendarSendCalendarInviteInfo>();
        public List<CalendarSendCalendarRaidLockoutInfo> RaidLockouts { get; set; } = new List<CalendarSendCalendarRaidLockoutInfo>();
        public List<CalendarSendCalendarEventInfo> Events { get; set; } = new List<CalendarSendCalendarEventInfo>();
    }

    class CalendarSendEvent : ServerPacket
    {
        public CalendarSendEvent() : base(ServerOpcodes.CalendarSendEvent) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(EventType);
            _worldPacket.WritePackedGuid(OwnerGuid);
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WriteUInt8(GetEventType);
            _worldPacket.WriteInt32(TextureID);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WritePackedTime(Date);
            _worldPacket.WriteUInt32(LockDate);
            _worldPacket.WritePackedGuid(EventGuildID);
            _worldPacket.WriteUInt32(Invites.Count);

            _worldPacket.WriteBits(EventName.Length, 8);
            _worldPacket.WriteBits(Description.Length, 11);
            _worldPacket.FlushBits();

            foreach (var invite in Invites)
                invite.Write(_worldPacket);

            _worldPacket.WriteString(EventName);
            _worldPacket.WriteString(Description);
        }

        public ObjectGuid OwnerGuid { get; set; }
        public ObjectGuid EventGuildID { get; set; }
        public ulong EventID { get; set; }
        public long Date { get; set; }
        public long LockDate { get; set; }
        public CalendarFlags Flags { get; set; }
        public int TextureID { get; set; }
        public CalendarEventType GetEventType { get; set; }
        public CalendarSendEventType EventType { get; set; }
        public string Description { get; set; }
        public string EventName { get; set; }
        public List<CalendarEventInviteInfo> Invites { get; set; } = new List<CalendarEventInviteInfo>();
    }

    class CalendarEventInviteAlert : ServerPacket
    {
        public CalendarEventInviteAlert() : base(ServerOpcodes.CalendarEventInviteAlert) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WritePackedTime(Date);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt8(EventType);
            _worldPacket.WriteInt32(TextureID);
            _worldPacket.WritePackedGuid(EventGuildID);
            _worldPacket.WriteUInt64(InviteID);
            _worldPacket.WriteUInt8(Status);
            _worldPacket.WriteUInt8(ModeratorStatus);

            // Todo: check order
            _worldPacket.WritePackedGuid(InvitedByGuid);
            _worldPacket.WritePackedGuid(OwnerGuid);

            _worldPacket.WriteBits(EventName.Length, 8);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(EventName);
        }

        public ObjectGuid OwnerGuid { get; set; }
        public ObjectGuid EventGuildID { get; set; }
        public ObjectGuid InvitedByGuid { get; set; }
        public ulong InviteID { get; set; }
        public ulong EventID { get; set; }
        public CalendarFlags Flags { get; set; }
        public long Date { get; set; }
        public int TextureID { get; set; }
        public CalendarInviteStatus Status { get; set; }
        public CalendarEventType EventType { get; set; }
        public CalendarModerationRank ModeratorStatus { get; set; }
        public string EventName { get; set; }
    }

    class CalendarEventInvite : ClientPacket
    {
        public CalendarEventInvite(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();

            ushort nameLen = _worldPacket.ReadBits<ushort>(9);
            Creating = _worldPacket.HasBit();
            IsSignUp = _worldPacket.HasBit();

            Name = _worldPacket.ReadString(nameLen);
        }

        public ulong ModeratorID { get; set; }
        public bool IsSignUp { get; set; }
        public bool Creating { get; set; } = true;
        public ulong EventID { get; set; }
        public string Name { get; set; }
    }

    class CalendarEventRSVP : ClientPacket
    {
        public CalendarEventRSVP(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            EventID = _worldPacket.ReadUInt64();
            InviteID = _worldPacket.ReadUInt64();
            Status = (CalendarInviteStatus)_worldPacket.ReadUInt8();
        }

        public ulong InviteID { get; set; }
        public ulong EventID { get; set; }
        public CalendarInviteStatus Status { get; set; }
    }

    class CalendarEventInviteStatus : ServerPacket
    {
        public CalendarEventInviteStatus() : base(ServerOpcodes.CalendarEventInviteStatus) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InviteGuid);
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WritePackedTime(Date);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt8(Status);
            _worldPacket.WritePackedTime(ResponseTime);

            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
        }

        public CalendarFlags Flags { get; set; }
        public ulong EventID { get; set; }
        public CalendarInviteStatus Status { get; set; }
        public bool ClearPending { get; set; }
        public long ResponseTime { get; set; }
        public long Date { get; set; }
        public ObjectGuid InviteGuid { get; set; }
    }

    class CalendarEventInviteRemoved : ServerPacket
    {
        public CalendarEventInviteRemoved() : base(ServerOpcodes.CalendarEventInviteRemoved) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InviteGuid);
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WriteUInt32(Flags);

            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
        }

        public ObjectGuid InviteGuid { get; set; }
        public ulong EventID { get; set; }
        public uint Flags { get; set; }
        public bool ClearPending { get; set; }
    }

    class CalendarEventInviteModeratorStatus : ServerPacket
    {
        public CalendarEventInviteModeratorStatus() : base(ServerOpcodes.CalendarEventInviteModeratorStatus) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InviteGuid);
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WriteUInt8(Status);

            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
        }

        public ObjectGuid InviteGuid { get; set; }
        public ulong EventID { get; set; }
        public CalendarInviteStatus Status { get; set; }
        public bool ClearPending { get; set; }
    }

    class CalendarEventInviteRemovedAlert : ServerPacket
    {
        public CalendarEventInviteRemovedAlert() : base(ServerOpcodes.CalendarEventInviteRemovedAlert) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WritePackedTime(Date);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt8(Status);
        }

        public ulong EventID { get; set; }
        public long Date { get; set; }
        public CalendarFlags Flags { get; set; }
        public CalendarInviteStatus Status { get; set; }
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
            _worldPacket.WriteUInt32(LockDate);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt32(TextureID);
            _worldPacket.WriteUInt8(EventType);

            _worldPacket.WriteBits(EventName.Length, 8);
            _worldPacket.WriteBits(Description.Length, 11);
            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(EventName);
            _worldPacket.WriteString(Description);
        }

        public ulong EventID { get; set; }
        public long Date { get; set; }
        public CalendarFlags Flags { get; set; }
        public long LockDate { get; set; }
        public long OriginalDate { get; set; }
        public int TextureID { get; set; }
        public CalendarEventType EventType { get; set; }
        public bool ClearPending { get; set; }
        public string Description { get; set; }
        public string EventName { get; set; }
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

        public ulong EventID { get; set; }
        public long Date { get; set; }
        public bool ClearPending { get; set; }
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

        public uint NumPending { get; set; }
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
            Tentative = _worldPacket.HasBit();
        }

        public bool Tentative { get; set; }
        public ulong EventID { get; set; }
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

        public ObjectGuid Guid { get; set; }
        public ulong EventID { get; set; }
        public ulong ModeratorID { get; set; }
        public ulong InviteID { get; set; }
    }

    class CalendarEventStatus : ClientPacket
    {
        public CalendarEventStatus(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            EventID = _worldPacket.ReadUInt64();
            InviteID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            Status = _worldPacket.ReadUInt8();
        }

        public ObjectGuid Guid { get; set; }
        public ulong EventID { get; set; }
        public ulong ModeratorID { get; set; }
        public ulong InviteID { get; set; }
        public byte Status { get; set; }
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

        public int MapID { get; set; }
        public bool Extend { get; set; }
        public uint DifficultyID { get; set; }
    }

    class CalendarEventModeratorStatus : ClientPacket
    {
        public CalendarEventModeratorStatus(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            EventID = _worldPacket.ReadUInt64();
            InviteID = _worldPacket.ReadUInt64();
            ModeratorID = _worldPacket.ReadUInt64();
            Status = _worldPacket.ReadUInt8();
        }

        public ObjectGuid Guid { get; set; }
        public ulong EventID { get; set; }
        public ulong InviteID { get; set; }
        public ulong ModeratorID { get; set; }
        public byte Status { get; set; }
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
            _worldPacket.WriteUInt8(Result);

            _worldPacket.WriteBits(Name.Length, 9);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Name);
        }

        public byte Command { get; set; }
        public CalendarError Result { get; set; }
        public string Name { get; set; }
    }

    class CalendarRaidLockoutAdded : ServerPacket
    {
        public CalendarRaidLockoutAdded() : base(ServerOpcodes.CalendarRaidLockoutAdded) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(InstanceID);
            _worldPacket.WriteUInt32(ServerTime);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32(DifficultyID);
            _worldPacket.WriteInt32(TimeRemaining);
        }

        public ulong InstanceID { get; set; }
        public Difficulty DifficultyID { get; set; }
        public int TimeRemaining { get; set; }
        public uint ServerTime { get; set; }
        public int MapID { get; set; }
    }

    class CalendarRaidLockoutRemoved : ServerPacket
    {
        public CalendarRaidLockoutRemoved() : base(ServerOpcodes.CalendarRaidLockoutRemoved) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(InstanceID);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32(DifficultyID);
        }

        public ulong InstanceID { get; set; }
        public int MapID { get; set; }
        public Difficulty DifficultyID { get; set; }
    }

    class CalendarRaidLockoutUpdated : ServerPacket
    {
        public CalendarRaidLockoutUpdated() : base(ServerOpcodes.CalendarRaidLockoutUpdated) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ServerTime);
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteUInt32(DifficultyID);
            _worldPacket.WriteInt32(NewTimeRemaining);
            _worldPacket.WriteInt32(OldTimeRemaining);
        }

        public int MapID { get; set; }
        public int OldTimeRemaining { get; set; }
        public long ServerTime { get; set; }
        public uint DifficultyID { get; set; }
        public int NewTimeRemaining { get; set; }
    }

    class CalendarEventInitialInvites : ServerPacket
    {
        public CalendarEventInitialInvites() : base(ServerOpcodes.CalendarEventInitialInvites) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Invites.Count);
            foreach (var invite in Invites)
            {
                _worldPacket.WritePackedGuid(invite.InviteGuid);
                _worldPacket.WriteUInt8(invite.Level);
            }
        }

        public List<CalendarEventInitialInviteInfo> Invites { get; set; } = new List<CalendarEventInitialInviteInfo>();
    }

    class CalendarEventInviteStatusAlert : ServerPacket
    {
        public CalendarEventInviteStatusAlert() : base(ServerOpcodes.CalendarEventInviteStatusAlert) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(EventID);
            _worldPacket.WritePackedTime(Date);
            _worldPacket.WriteUInt32(Flags);
            _worldPacket.WriteUInt8(Status);
        }

        public ulong EventID { get; set; }
        public uint Flags { get; set; }
        public long Date { get; set; }
        public byte Status { get; set; }
    }

    class CalendarEventInviteNotesAlert : ServerPacket
    {
        public CalendarEventInviteNotesAlert(ulong eventID, string notes) : base(ServerOpcodes.CalendarEventInviteNotesAlert)
        {
            EventID = eventID;
            Notes = notes;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt64(EventID);

            _worldPacket.WriteBits(Notes.Length, 8);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Notes);
        }

        public ulong EventID { get; set; }
        public string Notes { get; set; }
    }

    class CalendarEventInviteNotes : ServerPacket
    {
        public CalendarEventInviteNotes() : base(ServerOpcodes.CalendarEventInviteNotes) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(InviteGuid);
            _worldPacket.WriteUInt64(EventID);

            _worldPacket.WriteBits(Notes.Length, 8);
            _worldPacket.WriteBit(ClearPending);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Notes);
        }

        public ObjectGuid InviteGuid { get; set; }
        public ulong EventID { get; set; }
        public string Notes { get; set; } = "";
        public bool ClearPending { get; set; }
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
        }

        public ObjectGuid Guid { get; set; }
        public byte Status { get; set; }
        public byte Moderator { get; set; }
    }

    class CalendarAddEventInfo
    {
        public void Read(WorldPacket data)
        {
            byte titleLength = data.ReadBits<byte>(8);
            ushort descriptionLength = data.ReadBits<ushort>(11);

            EventType = data.ReadUInt8();
            TextureID = data.ReadInt32();
            Time = data.ReadPackedTime();
            Flags = data.ReadUInt32();
            var InviteCount = data.ReadUInt32();

            Title = data.ReadString(titleLength);
            Description = data.ReadString(descriptionLength);

            for (var i = 0; i < InviteCount; ++i)
            {
                CalendarAddEventInviteInfo invite = new CalendarAddEventInviteInfo();
                invite.Read(data);
                Invites[i] = invite;
            }
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public byte EventType { get; set; }
        public int TextureID { get; set; }
        public long Time { get; set; }
        public uint Flags { get; set; }
        public CalendarAddEventInviteInfo[] Invites { get; set; } = new CalendarAddEventInviteInfo[(int)SharedConst.CalendarMaxInvites];
    }

    struct CalendarUpdateEventInfo
    {
        public ulong EventID { get; set; }
        public ulong ModeratorID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte EventType { get; set; }
        public uint TextureID { get; set; }
        public long Time { get; set; }
        public uint Flags { get; set; }
    }

    struct CalendarSendCalendarInviteInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt64(EventID);
            data.WriteUInt64(InviteID);
            data.WriteUInt8(Status);
            data.WriteUInt8(Moderator);
            data.WriteUInt8(InviteType);
            data.WritePackedGuid(InviterGuid);
        }

        public ulong EventID { get; set; }
        public ulong InviteID { get; set; }
        public ObjectGuid InviterGuid { get; set; }
        public CalendarInviteStatus Status { get; set; }
        public CalendarModerationRank Moderator { get; set; }
        public byte InviteType { get; set; }
    }
    struct CalendarSendCalendarRaidLockoutInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt64(InstanceID);
            data.WriteInt32(MapID);
            data.WriteUInt32(DifficultyID);
            data.WriteUInt32(ExpireTime);
        }

        public ulong InstanceID { get; set; }
        public int MapID { get; set; }
        public uint DifficultyID { get; set; }
        public long ExpireTime { get; set; }
    }

    struct CalendarSendCalendarEventInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt64(EventID);
            data.WriteUInt8(EventType);
            data.WritePackedTime(Date);
            data.WriteUInt32(Flags);
            data.WriteInt32(TextureID);
            data.WritePackedGuid(EventGuildID);
            data.WritePackedGuid(OwnerGuid);

            data.WriteBits(EventName.Length, 8);
            data.FlushBits();
            data.WriteString(EventName);
        }

        public ulong EventID { get; set; }
        public string EventName { get; set; }
        public CalendarEventType EventType { get; set; }
        public long Date { get; set; }
        public CalendarFlags Flags { get; set; }
        public int TextureID { get; set; }
        public ObjectGuid EventGuildID { get; set; }
        public ObjectGuid OwnerGuid { get; set; }
    }

    class CalendarEventInviteInfo
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Guid);
            data.WriteUInt64(InviteID);

            data.WriteUInt8(Level);
            data.WriteUInt8(Status);
            data.WriteUInt8(Moderator);
            data.WriteUInt8(InviteType);

            data.WritePackedTime(ResponseTime);

            data.WriteBits(Notes.Length, 8);
            data.FlushBits();
            data.WriteString(Notes);
        }

        public ObjectGuid Guid { get; set; }
        public ulong InviteID { get; set; }
        public long ResponseTime { get; set; }
        public byte Level { get; set; } = 1;
        public CalendarInviteStatus Status { get; set; }
        public CalendarModerationRank Moderator { get; set; }
        public byte InviteType { get; set; }
        public string Notes { get; set; }
    }

    class CalendarEventInitialInviteInfo
    {
        public CalendarEventInitialInviteInfo(ObjectGuid inviteGuid, byte level)
        {
            InviteGuid = inviteGuid;
            Level = level;
        }

        public ObjectGuid InviteGuid { get; set; }
        public byte Level { get; set; } = 100;
    }
}
