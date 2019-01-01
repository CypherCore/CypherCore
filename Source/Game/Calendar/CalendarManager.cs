/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Framework.Database;
using Game.Entities;
using Game.Guilds;
using Game.Mails;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class CalendarManager : Singleton<CalendarManager>
    {
        CalendarManager()
        {
            _events = new List<CalendarEvent>();
            _invites = new MultiMap<ulong,CalendarInvite>();
        }

        public void LoadFromDB()
        {
            uint count = 0;
            _maxEventId = 0;
            _maxInviteId = 0;

            //                                              0        1      2      3            4          5          6     7      8
            SQLResult result = DB.Characters.Query("SELECT EventID, Owner, Title, Description, EventType, TextureID, Date, Flags, LockDate FROM calendar_events");
            if (!result.IsEmpty())
            {
                do
                {
                    ulong eventID = result.Read<ulong>(0);
                    ObjectGuid ownerGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
                    string title = result.Read<string>(2);
                    string description = result.Read<string>(3);
                    CalendarEventType type = (CalendarEventType)result.Read<byte>(4);
                    int textureID = result.Read<int>(5);
                    uint date = result.Read<uint>(6);
                    CalendarFlags flags = (CalendarFlags)result.Read<uint>(7);
                    uint lockDate = result.Read<uint>(8);
                    ulong guildID = 0;

                    if (flags.HasAnyFlag(CalendarFlags.GuildEvent) || flags.HasAnyFlag(CalendarFlags.WithoutInvites))
                        guildID = Player.GetGuildIdFromDB(ownerGUID);

                    CalendarEvent calendarEvent = new CalendarEvent(eventID, ownerGUID, guildID, type, textureID, date, flags, title, description, lockDate);
                    _events.Add(calendarEvent);

                    _maxEventId = Math.Max(_maxEventId, eventID);

                    ++count;
                }
                while (result.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} calendar events", count);
            count = 0;

            //                                    0         1        2        3       4       5             6               7
            result = DB.Characters.Query("SELECT InviteID, EventID, Invitee, Sender, Status, ResponseTime, ModerationRank, Note FROM calendar_invites");
            if (!result.IsEmpty())
            {
                do
                {
                    ulong inviteId = result.Read<ulong>(0);
                    ulong eventId = result.Read<ulong>(1);
                    ObjectGuid invitee = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
                    ObjectGuid senderGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3));
                    CalendarInviteStatus status = (CalendarInviteStatus)result.Read<byte>(4);
                    uint responseTime = result.Read<uint>(5);
                    CalendarModerationRank rank = (CalendarModerationRank)result.Read<byte>(6);
                    string note = result.Read<string>(7);

                    CalendarInvite invite = new CalendarInvite(inviteId, eventId, invitee, senderGUID, responseTime, status, rank, note);
                    _invites.Add(eventId, invite);

                    _maxInviteId = Math.Max(_maxInviteId, inviteId);

                    ++count;
                }
                while (result.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} calendar invites", count);

            for (ulong i = 1; i < _maxEventId; ++i)
                if (GetEvent(i) == null)
                    _freeEventIds.Add(i);

            for (ulong i = 1; i < _maxInviteId; ++i)
                if (GetInvite(i) == null)
                    _freeInviteIds.Add(i);
        }

        public void AddEvent(CalendarEvent calendarEvent, CalendarSendEventType sendType)
        {
            _events.Add(calendarEvent);
            UpdateEvent(calendarEvent);
            SendCalendarEvent(calendarEvent.OwnerGuid, calendarEvent, sendType);
        }

        public void AddInvite(CalendarEvent calendarEvent, CalendarInvite invite, SQLTransaction trans = null)
        {
            if (!calendarEvent.IsGuildAnnouncement() && calendarEvent.OwnerGuid != invite.InviteeGuid)
                SendCalendarEventInvite(invite);

            if (!calendarEvent.IsGuildEvent() || invite.InviteeGuid == calendarEvent.OwnerGuid)
                SendCalendarEventInviteAlert(calendarEvent, invite);

            if (!calendarEvent.IsGuildAnnouncement())
            {
                _invites.Add(invite.EventId, invite);
                UpdateInvite(invite, trans);
            }
        }

        public void RemoveEvent(ulong eventId, ObjectGuid remover)
        {
            CalendarEvent calendarEvent = GetEvent(eventId);

            if (calendarEvent == null)
            {
                SendCalendarCommandResult(remover, CalendarError.EventInvalid);
                return;
            }

            SendCalendarEventRemovedAlert(calendarEvent);

            SQLTransaction trans = new SQLTransaction();
            PreparedStatement stmt;
            MailDraft mail = new MailDraft(calendarEvent.BuildCalendarMailSubject(remover), calendarEvent.BuildCalendarMailBody());

            var eventInvites = _invites[eventId];
            for (int i = 0; i < eventInvites.Count; ++i)
            {
                CalendarInvite invite = eventInvites[i];
                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CALENDAR_INVITE);
                stmt.AddValue(0, invite.InviteId);
                trans.Append(stmt);

                // guild events only? check invite status here?
                // When an event is deleted, all invited (accepted/declined? - verify) guildies are notified via in-game mail. (wowwiki)
                if (!remover.IsEmpty() && invite.InviteeGuid != remover)
                    mail.SendMailTo(trans, new MailReceiver(invite.InviteeGuid.GetCounter()), new MailSender(calendarEvent), MailCheckMask.Copied);
            }

            _invites.Remove(eventId);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CALENDAR_EVENT);
            stmt.AddValue(0, eventId);
            trans.Append(stmt);
            DB.Characters.CommitTransaction(trans);

            _events.Remove(calendarEvent);
        }

        public void RemoveInvite(ulong inviteId, ulong eventId, ObjectGuid remover)
        {
            CalendarEvent calendarEvent = GetEvent(eventId);

            if (calendarEvent == null)
                return;

            CalendarInvite calendarInvite = null;
            foreach (var invite in _invites[eventId])
            {
                if (invite.InviteId == inviteId)
                {
                    calendarInvite = invite;
                    break;
                }
            }

            if (calendarInvite == null)
                return;

            SQLTransaction trans = new SQLTransaction();
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CALENDAR_INVITE);
            stmt.AddValue(0, calendarInvite.InviteId);
            trans.Append(stmt);
            DB.Characters.CommitTransaction(trans);

            if (!calendarEvent.IsGuildEvent())
                SendCalendarEventInviteRemoveAlert(calendarInvite.InviteeGuid, calendarEvent, CalendarInviteStatus.Removed);

            SendCalendarEventInviteRemove(calendarEvent, calendarInvite, (uint)calendarEvent.Flags);

            // we need to find out how to use CALENDAR_INVITE_REMOVED_MAIL_SUBJECT to force client to display different mail
            //if (itr._invitee != remover)
            //    MailDraft(calendarEvent.BuildCalendarMailSubject(remover), calendarEvent.BuildCalendarMailBody())
            //        .SendMailTo(trans, MailReceiver(itr.GetInvitee()), calendarEvent, MAIL_CHECK_MASK_COPIED);

            _invites.Remove(eventId, calendarInvite);
        }

        public void UpdateEvent(CalendarEvent calendarEvent)
        {
            SQLTransaction trans = new SQLTransaction();
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CALENDAR_EVENT);
            stmt.AddValue(0, calendarEvent.EventId);
            stmt.AddValue(1, calendarEvent.OwnerGuid.GetCounter());
            stmt.AddValue(2, calendarEvent.Title);
            stmt.AddValue(3, calendarEvent.Description);
            stmt.AddValue(4, calendarEvent.EventType);
            stmt.AddValue(5, calendarEvent.TextureId);
            stmt.AddValue(6, calendarEvent.Date);
            stmt.AddValue(7, calendarEvent.Flags);
            stmt.AddValue(8, calendarEvent.LockDate);
            trans.Append(stmt);
            DB.Characters.CommitTransaction(trans);
        }

        public void UpdateInvite(CalendarInvite invite, SQLTransaction trans = null)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CALENDAR_INVITE);
            stmt.AddValue(0, invite.InviteId);
            stmt.AddValue(1, invite.EventId);
            stmt.AddValue(2, invite.InviteeGuid.GetCounter());
            stmt.AddValue(3, invite.SenderGuid.GetCounter());
            stmt.AddValue(4, invite.Status);
            stmt.AddValue(5, invite.ResponseTime);
            stmt.AddValue(6, invite.Rank);
            stmt.AddValue(7, invite.Note);
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        public void RemoveAllPlayerEventsAndInvites(ObjectGuid guid)
        {
            foreach (var calendarEvent in _events)
                if (calendarEvent.OwnerGuid == guid)
                    RemoveEvent(calendarEvent.EventId, ObjectGuid.Empty); // don't send mail if removing a character

            List<CalendarInvite> playerInvites = GetPlayerInvites(guid);
            foreach (var calendarInvite in playerInvites)
                RemoveInvite(calendarInvite.InviteId, calendarInvite.EventId, guid);
        }

        public void RemovePlayerGuildEventsAndSignups(ObjectGuid guid, ulong guildId)
        {
            foreach (var calendarEvent in _events)
                if (calendarEvent.OwnerGuid == guid && (calendarEvent.IsGuildEvent() || calendarEvent.IsGuildAnnouncement()))
                    RemoveEvent(calendarEvent.EventId, guid);

            List<CalendarInvite> playerInvites = GetPlayerInvites(guid);
            foreach (var playerCalendarEvent in playerInvites)
            {
                CalendarEvent calendarEvent = GetEvent(playerCalendarEvent.EventId);
                if (calendarEvent != null)
                    if (calendarEvent.IsGuildEvent() && calendarEvent.GuildId == guildId)
                        RemoveInvite(playerCalendarEvent.InviteId, playerCalendarEvent.EventId, guid);
            }
        }

        public CalendarEvent GetEvent(ulong eventId)
        {
            foreach (var calendarEvent in _events)
                if (calendarEvent.EventId == eventId)
                    return calendarEvent;

            Log.outDebug(LogFilter.Calendar, "CalendarMgr:GetEvent: {0} not found!", eventId);
            return null;
        }

        public CalendarInvite GetInvite(ulong inviteId)
        {
            foreach (var calendarEvent in _invites.Values)
                if (calendarEvent.InviteId == inviteId)
                    return calendarEvent;

            Log.outDebug(LogFilter.Calendar, "CalendarMgr:GetInvite: {0} not found!", inviteId);
            return null;
        }

        void FreeEventId(ulong id)
        {
            if (id == _maxEventId)
                --_maxEventId;
            else
                _freeEventIds.Add(id);
        }

        public ulong GetFreeEventId()
        {
            if (_freeEventIds.Empty())
                return ++_maxEventId;

            ulong eventId = _freeEventIds.FirstOrDefault();
            _freeEventIds.RemoveAt(0);
            return eventId;
        }

        void FreeInviteId(ulong id)
        {
            if (id == _maxInviteId)
                --_maxInviteId;
            else
                _freeInviteIds.Add(id);
        }

        public ulong GetFreeInviteId()
        {
            if (_freeInviteIds.Empty())
                return ++_maxInviteId;

            ulong inviteId = _freeInviteIds.FirstOrDefault();
            _freeInviteIds.RemoveAt(0);
            return inviteId;
        }

        public List<CalendarEvent> GetPlayerEvents(ObjectGuid guid)
        {
            List<CalendarEvent> events = new List<CalendarEvent>();

            foreach (var pair in _invites)
            {
                if (pair.Value.InviteeGuid == guid)
                {
                    CalendarEvent Event = GetEvent(pair.Key);
                    if (Event != null) // null check added as attempt to fix #11512
                        events.Add(Event);
                }
            }
            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
            {
                foreach (var calendarEvent in _events)
                    if (calendarEvent.GuildId == player.GetGuildId())
                        events.Add(calendarEvent);
            }

            return events;
        }

        public List<CalendarInvite> GetEventInvites(ulong eventId)
        {
            return _invites[eventId];
        }

        public List<CalendarInvite> GetPlayerInvites(ObjectGuid guid)
        {
            List<CalendarInvite> invites = new List<CalendarInvite>();

            foreach (var calendarEvent in _invites.Values)
            {
                if (calendarEvent.InviteeGuid == guid)
                    invites.Add(calendarEvent);
            }

            return invites;
        }

        public uint GetPlayerNumPending(ObjectGuid guid)
        {
            List<CalendarInvite> invites = GetPlayerInvites(guid);

            uint pendingNum = 0;
            foreach (var calendarEvent in invites)
            {
                switch (calendarEvent.Status)
                {
                    case CalendarInviteStatus.Invited:
                    case CalendarInviteStatus.Tentative:
                    case CalendarInviteStatus.NotSignedUp:
                        ++pendingNum;
                        break;
                    default:
                        break;
                }
            }

            return pendingNum;
        }

        public void SendCalendarEventInvite(CalendarInvite invite)
        {
            CalendarEvent calendarEvent = GetEvent(invite.EventId);

            ObjectGuid invitee = invite.InviteeGuid;
            Player player = Global.ObjAccessor.FindPlayer(invitee);

            uint level = player ? player.getLevel() : Player.GetLevelFromDB(invitee);

            SCalendarEventInvite packet = new SCalendarEventInvite();
            packet.EventID = calendarEvent != null ? calendarEvent.EventId : 0;
            packet.InviteGuid = invitee;
            packet.InviteID = calendarEvent != null ? invite.InviteId : 0;
            packet.Level = (byte)level;
            packet.ResponseTime = invite.ResponseTime;
            packet.Status = invite.Status;
            packet.Type = (byte)(calendarEvent != null ? calendarEvent.IsGuildEvent() ? 1 : 0 : 0); // Correct ?
            packet.ClearPending = calendarEvent != null ? !calendarEvent.IsGuildEvent() : true; // Correct ?

            if (calendarEvent == null) // Pre-invite
            {
                player = Global.ObjAccessor.FindPlayer(invite.SenderGuid);
                if (player)
                    player.SendPacket(packet);
            }
            else
            {
                if (calendarEvent.OwnerGuid != invite.InviteeGuid) // correct?
                    SendPacketToAllEventRelatives(packet, calendarEvent);
            }
        }

        public void SendCalendarEventUpdateAlert(CalendarEvent calendarEvent, long originalDate)
        {
            CalendarEventUpdatedAlert packet = new CalendarEventUpdatedAlert();
            packet.ClearPending = true; // FIXME
            packet.Date = calendarEvent.Date;
            packet.Description = calendarEvent.Description;
            packet.EventID = calendarEvent.EventId;
            packet.EventName = calendarEvent.Title;
            packet.EventType = calendarEvent.EventType;
            packet.Flags = calendarEvent.Flags;
            packet.LockDate = calendarEvent.LockDate; // Always 0 ?
            packet.OriginalDate = originalDate;
            packet.TextureID = calendarEvent.TextureId;

            SendPacketToAllEventRelatives(packet, calendarEvent);
        }

        public void SendCalendarEventStatus(CalendarEvent calendarEvent, CalendarInvite invite)
        {
            CalendarEventInviteStatus packet = new CalendarEventInviteStatus();
            packet.ClearPending = true; // FIXME
            packet.Date = calendarEvent.Date;
            packet.EventID = calendarEvent.EventId;
            packet.Flags = calendarEvent.Flags;
            packet.InviteGuid = invite.InviteeGuid;
            packet.ResponseTime = invite.ResponseTime;
            packet.Status = invite.Status;

            SendPacketToAllEventRelatives(packet, calendarEvent);
        }

        void SendCalendarEventRemovedAlert(CalendarEvent calendarEvent)
        {
            CalendarEventRemovedAlert packet = new CalendarEventRemovedAlert();
            packet.ClearPending = true; // FIXME
            packet.Date = calendarEvent.Date;
            packet.EventID = calendarEvent.EventId;

            SendPacketToAllEventRelatives(packet, calendarEvent);
        }

        void SendCalendarEventInviteRemove(CalendarEvent calendarEvent, CalendarInvite invite, uint flags)
        {
            CalendarEventInviteRemoved packet = new CalendarEventInviteRemoved();
            packet.ClearPending = true; // FIXME
            packet.EventID = calendarEvent.EventId;
            packet.Flags = flags;
            packet.InviteGuid = invite.InviteeGuid;

            SendPacketToAllEventRelatives(packet, calendarEvent);
        }

        public void SendCalendarEventModeratorStatusAlert(CalendarEvent calendarEvent, CalendarInvite invite)
        {
            CalendarEventInviteModeratorStatus packet = new CalendarEventInviteModeratorStatus();
            packet.ClearPending = true; // FIXME
            packet.EventID = calendarEvent.EventId;
            packet.InviteGuid = invite.InviteeGuid;
            packet.Status = invite.Status;

            SendPacketToAllEventRelatives(packet, calendarEvent);
        }

        void SendCalendarEventInviteAlert(CalendarEvent calendarEvent, CalendarInvite invite)
        {
            CalendarEventInviteAlert packet = new CalendarEventInviteAlert();
            packet.Date = calendarEvent.Date;
            packet.EventID = calendarEvent.EventId;
            packet.EventName = calendarEvent.Title;
            packet.EventType = calendarEvent.EventType;
            packet.Flags = calendarEvent.Flags;
            packet.InviteID = invite.InviteId;
            packet.InvitedByGuid = invite.SenderGuid;
            packet.ModeratorStatus = invite.Rank;
            packet.OwnerGuid = calendarEvent.OwnerGuid;
            packet.Status = invite.Status;
            packet.TextureID = calendarEvent.TextureId;

            Guild guild = Global.GuildMgr.GetGuildById(calendarEvent.GuildId);
            packet.EventGuildID = guild ? guild.GetGUID() : ObjectGuid.Empty;

            if (calendarEvent.IsGuildEvent() || calendarEvent.IsGuildAnnouncement())
            {
                guild = Global.GuildMgr.GetGuildById(calendarEvent.GuildId);
                if (guild)
                    guild.BroadcastPacket(packet);
            }
            else
            {
                Player player = Global.ObjAccessor.FindPlayer(invite.InviteeGuid);
                if (player)
                    player.SendPacket(packet);
            }
        }

        public void SendCalendarEvent(ObjectGuid guid, CalendarEvent calendarEvent, CalendarSendEventType sendType)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (!player)
                return;

            List<CalendarInvite> eventInviteeList = _invites[calendarEvent.EventId];

            CalendarSendEvent packet = new CalendarSendEvent();
            packet.Date = calendarEvent.Date;
            packet.Description = calendarEvent.Description;
            packet.EventID = calendarEvent.EventId;
            packet.EventName = calendarEvent.Title;
            packet.EventType = sendType;
            packet.Flags = calendarEvent.Flags;
            packet.GetEventType = calendarEvent.EventType;
            packet.LockDate = calendarEvent.LockDate; // Always 0 ?
            packet.OwnerGuid = calendarEvent.OwnerGuid;
            packet.TextureID = calendarEvent.TextureId;

            Guild guild = Global.GuildMgr.GetGuildById(calendarEvent.GuildId);
            packet.EventGuildID = (guild ? guild.GetGUID() : ObjectGuid.Empty);

            foreach (var calendarInvite in eventInviteeList)
            {
                ObjectGuid inviteeGuid = calendarInvite.InviteeGuid;
                Player invitee = Global.ObjAccessor.FindPlayer(inviteeGuid);

                uint inviteeLevel = invitee ? invitee.getLevel() : Player.GetLevelFromDB(inviteeGuid);
                uint inviteeGuildId = invitee ? invitee.GetGuildId() : Player.GetGuildIdFromDB(inviteeGuid);

                CalendarEventInviteInfo inviteInfo = new CalendarEventInviteInfo();
                inviteInfo.Guid = inviteeGuid;
                inviteInfo.Level = (byte)inviteeLevel;
                inviteInfo.Status = calendarInvite.Status;
                inviteInfo.Moderator = calendarInvite.Rank;
                inviteInfo.InviteType = (byte)(calendarEvent.IsGuildEvent() && calendarEvent.GuildId == inviteeGuildId ? 1 : 0);
                inviteInfo.InviteID = calendarInvite.InviteId;
                inviteInfo.ResponseTime = calendarInvite.ResponseTime;
                inviteInfo.Notes = calendarInvite.Note;

                packet.Invites.Add(inviteInfo);
            }

            player.SendPacket(packet);
        }

        void SendCalendarEventInviteRemoveAlert(ObjectGuid guid, CalendarEvent calendarEvent, CalendarInviteStatus status)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
            {
                CalendarEventInviteRemovedAlert packet = new CalendarEventInviteRemovedAlert();
                packet.Date = calendarEvent.Date;
                packet.EventID = calendarEvent.EventId;
                packet.Flags = calendarEvent.Flags;
                packet.Status = status;

                player.SendPacket(packet);
            }
        }

        public void SendCalendarClearPendingAction(ObjectGuid guid)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
               player.SendPacket(new CalendarClearPendingAction());
        }

        public void SendCalendarCommandResult(ObjectGuid guid, CalendarError err, string param = null)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
            {
                CalendarCommandResult packet = new CalendarCommandResult();
                packet.Command = 1; // FIXME
                packet.Result = err;

                switch (err)
                {
                    case CalendarError.OtherInvitesExceeded:
                    case CalendarError.AlreadyInvitedToEventS:
                    case CalendarError.IgnoringYouS:
                        packet.Name = param;
                        break;
                }

                player.SendPacket(packet);
            }
        }

        void SendPacketToAllEventRelatives(ServerPacket packet, CalendarEvent calendarEvent)
        {
            // Send packet to all guild members
            if (calendarEvent.IsGuildEvent() || calendarEvent.IsGuildAnnouncement())
            {
                Guild guild = Global.GuildMgr.GetGuildById(calendarEvent.GuildId);
                if (guild)
                    guild.BroadcastPacket(packet);
            }

            // Send packet to all invitees if event is non-guild, in other case only to non-guild invitees (packet was broadcasted for them)
            List<CalendarInvite> invites = _invites[calendarEvent.EventId];
            foreach (var playerCalendarEvent in invites)
            {
                Player player = Global.ObjAccessor.FindPlayer(playerCalendarEvent.InviteeGuid);
                if (player)
                    if (!calendarEvent.IsGuildEvent() || (calendarEvent.IsGuildEvent() && player.GetGuildId() != calendarEvent.GuildId))
                        player.SendPacket(packet);
            }
        }

        List<CalendarEvent> _events;
        MultiMap<ulong, CalendarInvite> _invites;

        List<ulong> _freeEventIds = new List<ulong>();
        List<ulong> _freeInviteIds = new List<ulong>();
        ulong _maxEventId;
        ulong _maxInviteId;
    }

    public class CalendarInvite
    {
        public CalendarInvite()
        {
            InviteId = 1;
            ResponseTime = Time.UnixTime;
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
    }

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

        public string BuildCalendarMailSubject(ObjectGuid remover)
        {
            return remover + ":" + Title;
        }

        public string BuildCalendarMailBody()
        {
            var now = Time.UnixTimeToDateTime(Date);
            uint time = Convert.ToUInt32(((now.Year - 1900) - 100) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute);
            return time.ToString();
        }

        public bool IsGuildEvent() { return Flags.HasAnyFlag(CalendarFlags.GuildEvent); }
        public bool IsGuildAnnouncement() { return Flags.HasAnyFlag(CalendarFlags.WithoutInvites); }
        public bool IsLocked() { return Flags.HasAnyFlag(CalendarFlags.InvitesLocked); }

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
    }
}
