// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Framework.Database;
using Game.Cache;
using Game.Entities;
using Game.Guilds;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CalendarGet)]
        private void HandleCalendarGetCalendar(CalendarGetCalendar calendarGetCalendar)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            long currTime = GameTime.GetGameTime();

            CalendarSendCalendar packet = new();
            packet.ServerTime = currTime;

            var invites = Global.CalendarMgr.GetPlayerInvites(guid);

            foreach (var invite in invites)
            {
                CalendarSendCalendarInviteInfo inviteInfo = new();
                inviteInfo.EventID = invite.EventId;
                inviteInfo.InviteID = invite.InviteId;
                inviteInfo.InviterGuid = invite.SenderGuid;
                inviteInfo.Status = invite.Status;
                inviteInfo.Moderator = invite.Rank;
                CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(invite.EventId);

                if (calendarEvent != null)
                    inviteInfo.InviteType = (byte)(calendarEvent.IsGuildEvent() && calendarEvent.GuildId == _player.GetGuildId() ? 1 : 0);

                packet.Invites.Add(inviteInfo);
            }

            var playerEvents = Global.CalendarMgr.GetPlayerEvents(guid);

            foreach (var calendarEvent in playerEvents)
            {
                CalendarSendCalendarEventInfo eventInfo;
                eventInfo.EventID = calendarEvent.EventId;
                eventInfo.Date = calendarEvent.Date;
                eventInfo.EventClubID = calendarEvent.GuildId;
                eventInfo.EventName = calendarEvent.Title;
                eventInfo.EventType = calendarEvent.EventType;
                eventInfo.Flags = calendarEvent.Flags;
                eventInfo.OwnerGuid = calendarEvent.OwnerGuid;
                eventInfo.TextureID = calendarEvent.TextureId;

                packet.Events.Add(eventInfo);
            }

            foreach (InstanceLock instanceLock in Global.InstanceLockMgr.GetInstanceLocksForPlayer(_player.GetGUID()))
            {
                CalendarSendCalendarRaidLockoutInfo lockoutInfo = new();

                lockoutInfo.MapID = (int)instanceLock.GetMapId();
                lockoutInfo.DifficultyID = (uint)instanceLock.GetDifficultyId();
                lockoutInfo.ExpireTime = (int)Math.Max((instanceLock.GetEffectiveExpiryTime() - GameTime.GetSystemTime()).TotalSeconds, 0);
                lockoutInfo.InstanceID = instanceLock.GetInstanceId();

                packet.RaidLockouts.Add(lockoutInfo);
            }

            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarGetEvent)]
        private void HandleCalendarGetEvent(CalendarGetEvent calendarGetEvent)
        {
            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarGetEvent.EventID);

            if (calendarEvent != null)
                Global.CalendarMgr.SendCalendarEvent(GetPlayer().GetGUID(), calendarEvent, CalendarSendEventType.Get);
            else
                Global.CalendarMgr.SendCalendarCommandResult(GetPlayer().GetGUID(), CalendarError.EventInvalid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarCommunityInvite)]
        private void HandleCalendarCommunityInvite(CalendarCommunityInviteRequest calendarCommunityInvite)
        {
            Guild guild = Global.GuildMgr.GetGuildById(GetPlayer().GetGuildId());

            if (guild)
                guild.MassInviteToEvent(this, calendarCommunityInvite.MinLevel, calendarCommunityInvite.MaxLevel, (GuildRankOrder)calendarCommunityInvite.MaxRankOrder);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarAddEvent)]
        private void HandleCalendarAddEvent(CalendarAddEvent calendarAddEvent)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            calendarAddEvent.EventInfo.Time = Time.LocalTimeToUTCTime(calendarAddEvent.EventInfo.Time);

            // prevent events in the past
            // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
            if (calendarAddEvent.EventInfo.Time < (GameTime.GetGameTime() - 86400L))
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventPassed);

                return;
            }

            // If the event is a guild event, check if the player is in a guild
            if (CalendarEvent.IsGuildEvent(calendarAddEvent.EventInfo.Flags) ||
                CalendarEvent.IsGuildAnnouncement(calendarAddEvent.EventInfo.Flags))
                if (_player.GetGuildId() == 0)
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);

                    return;
                }

            // Check if the player reached the max number of events allowed to create
            if (CalendarEvent.IsGuildEvent(calendarAddEvent.EventInfo.Flags) ||
                CalendarEvent.IsGuildAnnouncement(calendarAddEvent.EventInfo.Flags))
            {
                if (Global.CalendarMgr.GetGuildEvents(_player.GetGuildId()).Count >= SharedConst.CalendarMaxGuildEvents)
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildEventsExceeded);

                    return;
                }
            }
            else
            {
                if (Global.CalendarMgr.GetEventsCreatedBy(guid).Count >= SharedConst.CalendarMaxEvents)
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventsExceeded);

                    return;
                }
            }

            if (GetCalendarEventCreationCooldown() > GameTime.GetGameTime())
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.Internal);

                return;
            }

            SetCalendarEventCreationCooldown(GameTime.GetGameTime() + SharedConst.CalendarCreateEventCooldown);

            CalendarEvent calendarEvent = new(Global.CalendarMgr.GetFreeEventId(),
                                              guid,
                                              0,
                                              (CalendarEventType)calendarAddEvent.EventInfo.EventType,
                                              calendarAddEvent.EventInfo.TextureID,
                                              calendarAddEvent.EventInfo.Time,
                                              (CalendarFlags)calendarAddEvent.EventInfo.Flags,
                                              calendarAddEvent.EventInfo.Title,
                                              calendarAddEvent.EventInfo.Description,
                                              0);

            if (calendarEvent.IsGuildEvent() ||
                calendarEvent.IsGuildAnnouncement())
                calendarEvent.GuildId = _player.GetGuildId();

            if (calendarEvent.IsGuildAnnouncement())
            {
                CalendarInvite invite = new(0, calendarEvent.EventId, ObjectGuid.Empty, guid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.NotSignedUp, CalendarModerationRank.Player, "");
                // WARNING: By passing pointer to a local variable, the underlying method(s) must NOT perform any kind
                // of storage of the pointer as it will lead to memory corruption
                Global.CalendarMgr.AddInvite(calendarEvent, invite);
            }
            else
            {
                SQLTransaction trans = null;

                if (calendarAddEvent.EventInfo.Invites.Length > 1)
                    trans = new SQLTransaction();

                for (int i = 0; i < calendarAddEvent.EventInfo.Invites.Length; ++i)
                {
                    CalendarInvite invite = new(Global.CalendarMgr.GetFreeInviteId(),
                                                calendarEvent.EventId,
                                                calendarAddEvent.EventInfo.Invites[i].Guid,
                                                guid,
                                                SharedConst.CalendarDefaultResponseTime,
                                                (CalendarInviteStatus)calendarAddEvent.EventInfo.Invites[i].Status,
                                                (CalendarModerationRank)calendarAddEvent.EventInfo.Invites[i].Moderator,
                                                "");

                    Global.CalendarMgr.AddInvite(calendarEvent, invite, trans);
                }

                if (calendarAddEvent.EventInfo.Invites.Length > 1)
                    DB.Characters.CommitTransaction(trans);
            }

            Global.CalendarMgr.AddEvent(calendarEvent, CalendarSendEventType.Add);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarUpdateEvent)]
        private void HandleCalendarUpdateEvent(CalendarUpdateEvent calendarUpdateEvent)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            long oldEventTime;

            calendarUpdateEvent.EventInfo.Time = Time.LocalTimeToUTCTime(calendarUpdateEvent.EventInfo.Time);

            // prevent events in the past
            // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
            if (calendarUpdateEvent.EventInfo.Time < (GameTime.GetGameTime() - 86400L))
                return;

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarUpdateEvent.EventInfo.EventID);

            if (calendarEvent != null)
            {
                oldEventTime = calendarEvent.Date;

                calendarEvent.EventType = (CalendarEventType)calendarUpdateEvent.EventInfo.EventType;
                calendarEvent.Flags = (CalendarFlags)calendarUpdateEvent.EventInfo.Flags;
                calendarEvent.Date = calendarUpdateEvent.EventInfo.Time;
                calendarEvent.TextureId = (int)calendarUpdateEvent.EventInfo.TextureID;
                calendarEvent.Title = calendarUpdateEvent.EventInfo.Title;
                calendarEvent.Description = calendarUpdateEvent.EventInfo.Description;

                Global.CalendarMgr.UpdateEvent(calendarEvent);
                Global.CalendarMgr.SendCalendarEventUpdateAlert(calendarEvent, oldEventTime);
            }
            else
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarRemoveEvent)]
        private void HandleCalendarRemoveEvent(CalendarRemoveEvent calendarRemoveEvent)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            Global.CalendarMgr.RemoveEvent(calendarRemoveEvent.EventID, guid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarCopyEvent)]
        private void HandleCalendarCopyEvent(CalendarCopyEvent calendarCopyEvent)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            calendarCopyEvent.Date = Time.LocalTimeToUTCTime(calendarCopyEvent.Date);

            // prevent events in the past
            // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
            if (calendarCopyEvent.Date < (GameTime.GetGameTime() - 86400L))
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventPassed);

                return;
            }

            CalendarEvent oldEvent = Global.CalendarMgr.GetEvent(calendarCopyEvent.EventID);

            if (oldEvent != null)
            {
                // Ensure that the player has access to the event
                if (oldEvent.IsGuildEvent() ||
                    oldEvent.IsGuildAnnouncement())
                {
                    if (oldEvent.GuildId != _player.GetGuildId())
                    {
                        Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);

                        return;
                    }
                }
                else
                {
                    if (oldEvent.OwnerGuid != guid)
                    {
                        Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);

                        return;
                    }
                }

                // Check if the player reached the max number of events allowed to create
                if (oldEvent.IsGuildEvent() ||
                    oldEvent.IsGuildAnnouncement())
                {
                    if (Global.CalendarMgr.GetGuildEvents(_player.GetGuildId()).Count >= SharedConst.CalendarMaxGuildEvents)
                    {
                        Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildEventsExceeded);

                        return;
                    }
                }
                else
                {
                    if (Global.CalendarMgr.GetEventsCreatedBy(guid).Count >= SharedConst.CalendarMaxEvents)
                    {
                        Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventsExceeded);

                        return;
                    }
                }

                if (GetCalendarEventCreationCooldown() > GameTime.GetGameTime())
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.Internal);

                    return;
                }

                SetCalendarEventCreationCooldown(GameTime.GetGameTime() + SharedConst.CalendarCreateEventCooldown);

                CalendarEvent newEvent = new(oldEvent, Global.CalendarMgr.GetFreeEventId());
                newEvent.Date = calendarCopyEvent.Date;
                Global.CalendarMgr.AddEvent(newEvent, CalendarSendEventType.Copy);

                var invites = Global.CalendarMgr.GetEventInvites(calendarCopyEvent.EventID);
                SQLTransaction trans = null;

                if (invites.Count > 1)
                    trans = new SQLTransaction();

                foreach (var invite in invites)
                    Global.CalendarMgr.AddInvite(newEvent, new CalendarInvite(invite, Global.CalendarMgr.GetFreeInviteId(), newEvent.EventId), trans);

                if (invites.Count > 1)
                    DB.Characters.CommitTransaction(trans);
                // should we change owner when somebody makes a copy of event owned by another person?
            }
            else
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarInvite)]
        private void HandleCalendarInvite(CalendarInvitePkt calendarInvite)
        {
            ObjectGuid playerGuid = GetPlayer().GetGUID();

            ObjectGuid inviteeGuid = ObjectGuid.Empty;
            Team inviteeTeam = 0;
            ulong inviteeGuildId = 0;

            if (!ObjectManager.NormalizePlayerName(ref calendarInvite.Name))
                return;

            Player player = Global.ObjAccessor.FindPlayerByName(calendarInvite.Name);

            if (player)
            {
                // Invitee is online
                inviteeGuid = player.GetGUID();
                inviteeTeam = player.GetTeam();
                inviteeGuildId = player.GetGuildId();
            }
            else
            {
                // Invitee offline, get _data from database
                ObjectGuid guid = Global.CharacterCacheStorage.GetCharacterGuidByName(calendarInvite.Name);

                if (!guid.IsEmpty())
                {
                    CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(guid);

                    if (characterInfo != null)
                    {
                        inviteeGuid = guid;
                        inviteeTeam = Player.TeamForRace(characterInfo.RaceId);
                        inviteeGuildId = characterInfo.GuildId;
                    }
                }
            }

            if (inviteeGuid.IsEmpty())
            {
                Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.PlayerNotFound);

                return;
            }

            if (GetPlayer().GetTeam() != inviteeTeam &&
                !WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionCalendar))
            {
                Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NotAllied);

                return;
            }

            SQLResult result1 = DB.Characters.Query("SELECT Flags FROM character_social WHERE Guid = {0} AND friend = {1}", inviteeGuid, playerGuid);

            if (!result1.IsEmpty())
                if (Convert.ToBoolean(result1.Read<byte>(0) & (byte)SocialFlag.Ignored))
                {
                    Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.IgnoringYouS, calendarInvite.Name);

                    return;
                }

            if (!calendarInvite.Creating)
            {
                CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarInvite.EventID);

                if (calendarEvent != null)
                {
                    if (calendarEvent.IsGuildEvent() &&
                        calendarEvent.GuildId == inviteeGuildId)
                    {
                        // we can't invite guild members to guild events
                        Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);

                        return;
                    }

                    CalendarInvite invite = new(Global.CalendarMgr.GetFreeInviteId(), calendarInvite.EventID, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
                    Global.CalendarMgr.AddInvite(calendarEvent, invite);
                }
                else
                {
                    Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.EventInvalid);
                }
            }
            else
            {
                if (calendarInvite.IsSignUp &&
                    inviteeGuildId == GetPlayer().GetGuildId())
                {
                    Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);

                    return;
                }

                CalendarInvite invite = new(calendarInvite.EventID, 0, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
                Global.CalendarMgr.SendCalendarEventInvite(invite);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarEventSignUp)]
        private void HandleCalendarEventSignup(CalendarEventSignUp calendarEventSignUp)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarEventSignUp.EventID);

            if (calendarEvent != null)
            {
                if (calendarEvent.IsGuildEvent() &&
                    calendarEvent.GuildId != GetPlayer().GetGuildId())
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);

                    return;
                }

                CalendarInviteStatus status = calendarEventSignUp.Tentative ? CalendarInviteStatus.Tentative : CalendarInviteStatus.SignedUp;
                CalendarInvite invite = new(Global.CalendarMgr.GetFreeInviteId(), calendarEventSignUp.EventID, guid, guid, GameTime.GetGameTime(), status, CalendarModerationRank.Player, "");
                Global.CalendarMgr.AddInvite(calendarEvent, invite);
                Global.CalendarMgr.SendCalendarClearPendingAction(guid);
            }
            else
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarRsvp)]
        private void HandleCalendarRsvp(HandleCalendarRsvp calendarRSVP)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarRSVP.EventID);

            if (calendarEvent != null)
            {
                // i think we still should be able to remove self from locked events
                if (calendarRSVP.Status != CalendarInviteStatus.Removed &&
                    calendarEvent.IsLocked())
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventLocked);

                    return;
                }

                CalendarInvite invite = Global.CalendarMgr.GetInvite(calendarRSVP.InviteID);

                if (invite != null)
                {
                    invite.Status = calendarRSVP.Status;
                    invite.ResponseTime = GameTime.GetGameTime();

                    Global.CalendarMgr.UpdateInvite(invite);
                    Global.CalendarMgr.SendCalendarEventStatus(calendarEvent, invite);
                    Global.CalendarMgr.SendCalendarClearPendingAction(guid);
                }
                else
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
                }
            }
            else
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarRemoveInvite)]
        private void HandleCalendarEventRemoveInvite(CalendarRemoveInvite calendarRemoveInvite)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarRemoveInvite.EventID);

            if (calendarEvent != null)
            {
                if (calendarEvent.OwnerGuid == calendarRemoveInvite.Guid)
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.DeleteCreatorFailed);

                    return;
                }

                Global.CalendarMgr.RemoveInvite(calendarRemoveInvite.InviteID, calendarRemoveInvite.EventID, guid);
            }
            else
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarStatus)]
        private void HandleCalendarStatus(CalendarStatus calendarStatus)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarStatus.EventID);

            if (calendarEvent != null)
            {
                CalendarInvite invite = Global.CalendarMgr.GetInvite(calendarStatus.InviteID);

                if (invite != null)
                {
                    invite.Status = (CalendarInviteStatus)calendarStatus.Status;

                    Global.CalendarMgr.UpdateInvite(invite);
                    Global.CalendarMgr.SendCalendarEventStatus(calendarEvent, invite);
                    Global.CalendarMgr.SendCalendarClearPendingAction(calendarStatus.Guid);
                }
                else
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
                }
            }
            else
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarModeratorStatus)]
        private void HandleCalendarModeratorStatus(CalendarModeratorStatusQuery calendarModeratorStatus)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarModeratorStatus.EventID);

            if (calendarEvent != null)
            {
                CalendarInvite invite = Global.CalendarMgr.GetInvite(calendarModeratorStatus.InviteID);

                if (invite != null)
                {
                    invite.Rank = (CalendarModerationRank)calendarModeratorStatus.Status;
                    Global.CalendarMgr.UpdateInvite(invite);
                    Global.CalendarMgr.SendCalendarEventModeratorStatusAlert(calendarEvent, invite);
                }
                else
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
                }
            }
            else
            {
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarComplain)]
        private void HandleCalendarComplain(CalendarComplain calendarComplain)
        {
            // what to do with complains?
        }

        [WorldPacketHandler(ClientOpcodes.CalendarGetNumPending)]
        private void HandleCalendarGetNumPending(CalendarGetNumPending calendarGetNumPending)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            uint pending = Global.CalendarMgr.GetPlayerNumPending(guid);

            SendPacket(new CalendarSendNumPending(pending));
        }

        [WorldPacketHandler(ClientOpcodes.SetSavedInstanceExtend)]
        private void HandleSetSavedInstanceExtend(SetSavedInstanceExtend setSavedInstanceExtend)
        {
            // cannot modify locks currently in use
            if (_player.GetMapId() == setSavedInstanceExtend.MapID)
                return;

            var expiryTimes = Global.InstanceLockMgr.UpdateInstanceLockExtensionForPlayer(_player.GetGUID(), new MapDb2Entries((uint)setSavedInstanceExtend.MapID, (Difficulty)setSavedInstanceExtend.DifficultyID), setSavedInstanceExtend.Extend);

            if (expiryTimes.Item1 == DateTime.MinValue)
                return;

            CalendarRaidLockoutUpdated calendarRaidLockoutUpdated = new();
            calendarRaidLockoutUpdated.ServerTime = GameTime.GetGameTime();
            calendarRaidLockoutUpdated.MapID = setSavedInstanceExtend.MapID;
            calendarRaidLockoutUpdated.DifficultyID = setSavedInstanceExtend.DifficultyID;
            calendarRaidLockoutUpdated.OldTimeRemaining = (int)Math.Max((expiryTimes.Item1 - GameTime.GetSystemTime()).TotalSeconds, 0);
            calendarRaidLockoutUpdated.NewTimeRemaining = (int)Math.Max((expiryTimes.Item2 - GameTime.GetSystemTime()).TotalSeconds, 0);
            SendPacket(calendarRaidLockoutUpdated);
        }

        public void SendCalendarRaidLockoutAdded(InstanceLock instanceLock)
        {
            CalendarRaidLockoutAdded calendarRaidLockoutAdded = new();
            calendarRaidLockoutAdded.InstanceID = instanceLock.GetInstanceId();
            calendarRaidLockoutAdded.ServerTime = (uint)GameTime.GetGameTime();
            calendarRaidLockoutAdded.MapID = (int)instanceLock.GetMapId();
            calendarRaidLockoutAdded.DifficultyID = instanceLock.GetDifficultyId();
            calendarRaidLockoutAdded.TimeRemaining = (int)(instanceLock.GetEffectiveExpiryTime() - GameTime.GetSystemTime()).TotalSeconds;
            SendPacket(calendarRaidLockoutAdded);
        }

        private void SendCalendarRaidLockoutRemoved(InstanceLock instanceLock)
        {
            CalendarRaidLockoutRemoved calendarRaidLockoutRemoved = new();
            calendarRaidLockoutRemoved.InstanceID = instanceLock.GetInstanceId();
            calendarRaidLockoutRemoved.MapID = (int)instanceLock.GetMapId();
            calendarRaidLockoutRemoved.DifficultyID = instanceLock.GetDifficultyId();
            SendPacket(calendarRaidLockoutRemoved);
        }
    }
}