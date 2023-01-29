// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game
{
    public class CalendarEvent
    {
        public CalendarEvent(CalendarEvent calendarEvent, ulong eventId)
        {
            EventId = eventId;
            OwnerGuid = calendarEvent.OwnerGuid;
            GuildId = calendarEvent.GuildId;
            EventType = calendarEvent.EventType;
            TextureId = calendarEvent.TextureId;
            Date = calendarEvent.Date;
            Flags = calendarEvent.Flags;
            LockDate = calendarEvent.LockDate;
            Title = calendarEvent.Title;
            Description = calendarEvent.Description;
        }

        public CalendarEvent(ulong eventId, ObjectGuid ownerGuid, ulong guildId, CalendarEventType type, int textureId, long date, CalendarFlags flags, string title, string description, long lockDate)
        {
            EventId = eventId;
            OwnerGuid = ownerGuid;
            GuildId = guildId;
            EventType = type;
            TextureId = textureId;
            Date = date;
            Flags = flags;
            LockDate = lockDate;
            Title = title;
            Description = description;
        }

        public CalendarEvent()
        {
            EventId = 1;
            EventType = CalendarEventType.Other;
            TextureId = -1;
            Title = "";
            Description = "";
        }

        public ulong EventId { get; set; }
        public ObjectGuid OwnerGuid { get; set; }
        public ulong GuildId { get; set; }
        public CalendarEventType EventType { get; set; }
        public int TextureId { get; set; }
        public long Date { get; set; }
        public CalendarFlags Flags { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public long LockDate { get; set; }

        public string BuildCalendarMailSubject(ObjectGuid remover)
        {
            return remover + ":" + Title;
        }

        public string BuildCalendarMailBody()
        {
            var now = Time.UnixTimeToDateTime(Date);
            uint time = Convert.ToUInt32((((now.Year - 1900) - 100) << 24) | ((now.Month - 1) << 20) | ((now.Day - 1) << 14) | ((int)now.DayOfWeek << 11) | (now.Hour << 6) | now.Minute);

            return time.ToString();
        }

        public bool IsGuildEvent()
        {
            return Flags.HasAnyFlag(CalendarFlags.GuildEvent);
        }

        public bool IsGuildAnnouncement()
        {
            return Flags.HasAnyFlag(CalendarFlags.WithoutInvites);
        }

        public bool IsLocked()
        {
            return Flags.HasAnyFlag(CalendarFlags.InvitesLocked);
        }

        public static bool IsGuildEvent(uint flags)
        {
            return (flags & (uint)CalendarFlags.GuildEvent) != 0;
        }

        public static bool IsGuildAnnouncement(uint flags)
        {
            return (flags & (uint)CalendarFlags.WithoutInvites) != 0;
        }
    }
}