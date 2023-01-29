// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game
{
    public class CalendarInvite
    {
        public CalendarInvite()
        {
            InviteId = 1;
            ResponseTime = 0;
            Status = CalendarInviteStatus.Invited;
            Rank = CalendarModerationRank.Player;
            Note = "";
        }

        public CalendarInvite(CalendarInvite calendarInvite, ulong inviteId, ulong eventId)
        {
            InviteId = inviteId;
            EventId = eventId;
            InviteeGuid = calendarInvite.InviteeGuid;
            SenderGuid = calendarInvite.SenderGuid;
            ResponseTime = calendarInvite.ResponseTime;
            Status = calendarInvite.Status;
            Rank = calendarInvite.Rank;
            Note = calendarInvite.Note;
        }

        public CalendarInvite(ulong inviteId, ulong eventId, ObjectGuid invitee, ObjectGuid senderGUID, long responseTime, CalendarInviteStatus status, CalendarModerationRank rank, string note)
        {
            InviteId = inviteId;
            EventId = eventId;
            InviteeGuid = invitee;
            SenderGuid = senderGUID;
            ResponseTime = responseTime;

            Status = status;
            Rank = rank;
            Note = note;
        }

        public ulong InviteId { get; set; }
        public ulong EventId { get; set; }
        public ObjectGuid InviteeGuid { get; set; }
        public ObjectGuid SenderGuid { get; set; }
        public long ResponseTime { get; set; }
        public CalendarInviteStatus Status { get; set; }
        public CalendarModerationRank Rank { get; set; }
        public string Note { get; set; }

        ~CalendarInvite()
        {
            if (InviteId != 0 &&
                EventId != 0)
                Global.CalendarMgr.FreeInviteId(InviteId);
        }
    }
}