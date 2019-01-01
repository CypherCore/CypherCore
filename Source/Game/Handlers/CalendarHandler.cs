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
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using System;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CalendarGet)]
        void HandleCalendarGetCalendar(CalendarGetCalendar calendarGetCalendar)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            long currTime = Time.UnixTime;

            CalendarSendCalendar packet = new CalendarSendCalendar();
            packet.ServerTime = currTime;

            var invites = Global.CalendarMgr.GetPlayerInvites(guid);
            foreach (var invite in invites)
            {
                CalendarSendCalendarInviteInfo inviteInfo = new CalendarSendCalendarInviteInfo();
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

            for (Difficulty i = 0; i < Difficulty.Max; ++i)
            {
                var boundInstances = _player.GetBoundInstances(i);
                if (boundInstances != null)
                {
                    foreach (var boundInstance in boundInstances.Values)
                    {
                        if (boundInstance.perm)
                        {
                            CalendarSendCalendarRaidLockoutInfo lockoutInfo;

                            InstanceSave save = boundInstance.save;
                            lockoutInfo.MapID = (int)save.GetMapId();
                            lockoutInfo.DifficultyID = (uint)save.GetDifficultyID();
                            lockoutInfo.ExpireTime = save.GetResetTime() - currTime;
                            lockoutInfo.InstanceID = save.GetInstanceId(); // instance save id as unique instance copy id

                            packet.RaidLockouts.Add(lockoutInfo);
                        }
                    }
                }
            }

            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarGetEvent)]
        void HandleCalendarGetEvent(CalendarGetEvent calendarGetEvent)
        {
            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarGetEvent.EventID);
            if (calendarEvent != null)
                Global.CalendarMgr.SendCalendarEvent(GetPlayer().GetGUID(), calendarEvent, CalendarSendEventType.Get);
            else
                Global.CalendarMgr.SendCalendarCommandResult(GetPlayer().GetGUID(), CalendarError.EventInvalid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarCommunityFilter)]
        void HandleCalendarCommunityFilter(CalendarCommunityFilter calendarCommunityFilter)
        {
            Guild guild = Global.GuildMgr.GetGuildById(GetPlayer().GetGuildId());
            if (guild)
                guild.MassInviteToEvent(this, calendarCommunityFilter.MinLevel, calendarCommunityFilter.MaxLevel, calendarCommunityFilter.MaxRankOrder);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarAddEvent)]
        void HandleCalendarAddEvent(CalendarAddEvent calendarAddEvent)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            // prevent events in the past
            // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
            if (calendarAddEvent.EventInfo.Time < (Time.UnixTime - 86400L))
                return;

            CalendarEvent calendarEvent = new CalendarEvent(Global.CalendarMgr.GetFreeEventId(), guid, 0, (CalendarEventType)calendarAddEvent.EventInfo.EventType, calendarAddEvent.EventInfo.TextureID,
                calendarAddEvent.EventInfo.Time, (CalendarFlags)calendarAddEvent.EventInfo.Flags, calendarAddEvent.EventInfo.Title, calendarAddEvent.EventInfo.Description, 0);

            if (calendarEvent.IsGuildEvent() || calendarEvent.IsGuildAnnouncement())
            {
                Player creator = Global.ObjAccessor.FindPlayer(guid);
                if (creator)
                    calendarEvent.GuildId = creator.GetGuildId();
            }

            if (calendarEvent.IsGuildAnnouncement())
            {
                CalendarInvite invite = new CalendarInvite(0, calendarEvent.EventId, ObjectGuid.Empty, guid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.NotSignedUp, CalendarModerationRank.Player, "");
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
                    CalendarInvite invite = new CalendarInvite(Global.CalendarMgr.GetFreeInviteId(), calendarEvent.EventId,
                        calendarAddEvent.EventInfo.Invites[i].Guid, guid, SharedConst.CalendarDefaultResponseTime, (CalendarInviteStatus)calendarAddEvent.EventInfo.Invites[i].Status,
                        (CalendarModerationRank)calendarAddEvent.EventInfo.Invites[i].Moderator, "");
                    Global.CalendarMgr.AddInvite(calendarEvent, invite, trans);
                }

                if (calendarAddEvent.EventInfo.Invites.Length > 1)
                    DB.Characters.CommitTransaction(trans);
            }

            Global.CalendarMgr.AddEvent(calendarEvent, CalendarSendEventType.Add);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarUpdateEvent)]
        void HandleCalendarUpdateEvent(CalendarUpdateEvent calendarUpdateEvent)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            long oldEventTime;

            // prevent events in the past
            // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
            if (calendarUpdateEvent.EventInfo.Time < (Time.UnixTime - 86400L))
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
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarRemoveEvent)]
        void HandleCalendarRemoveEvent(CalendarRemoveEvent calendarRemoveEvent)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            Global.CalendarMgr.RemoveEvent(calendarRemoveEvent.EventID, guid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarCopyEvent)]
        void HandleCalendarCopyEvent(CalendarCopyEvent calendarCopyEvent)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            // prevent events in the past
            // To Do: properly handle timezones and remove the "- time_t(86400L)" hack
            if (calendarCopyEvent.Date < (Time.UnixTime - 86400L))
                return;

            CalendarEvent oldEvent = Global.CalendarMgr.GetEvent(calendarCopyEvent.EventID);
            if (oldEvent != null)
            {
                CalendarEvent newEvent = new CalendarEvent(oldEvent, Global.CalendarMgr.GetFreeEventId());
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
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarEventInvite)]
        void HandleCalendarEventInvite(CalendarEventInvite calendarEventInvite)
        {
            ObjectGuid playerGuid = GetPlayer().GetGUID();

            ObjectGuid inviteeGuid = ObjectGuid.Empty;
            Team inviteeTeam = 0;
            ulong inviteeGuildId = 0;

            Player player = Global.ObjAccessor.FindPlayerByName(calendarEventInvite.Name);
            if (player)
            {
                // Invitee is online
                inviteeGuid = player.GetGUID();
                inviteeTeam = player.GetTeam();
                inviteeGuildId = player.GetGuildId();
            }
            else
            {
                // Invitee offline, get data from database
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUID_RACE_ACC_BY_NAME);
                stmt.AddValue(0, calendarEventInvite.Name);
                SQLResult result = DB.Characters.Query(stmt);
                if (!result.IsEmpty())
                {
                    inviteeGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                    inviteeTeam = Player.TeamForRace((Race)result.Read<byte>(1));
                    inviteeGuildId = Player.GetGuildIdFromDB(inviteeGuid);
                }
            }

            if (inviteeGuid.IsEmpty())
            {
                Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.PlayerNotFound);
                return;
            }

            if (GetPlayer().GetTeam() != inviteeTeam && !WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionCalendar))
            {
                Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NotAllied);
                return;
            }

            SQLResult result1 = DB.Characters.Query("SELECT flags FROM character_social WHERE guid = {0} AND friend = {1}", inviteeGuid, playerGuid);
            if (!result1.IsEmpty())
            {

                if (Convert.ToBoolean(result1.Read<byte>(0) & (byte)SocialFlag.Ignored))
                {
                    Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.IgnoringYouS, calendarEventInvite.Name);
                    return;
                }
            }

            if (!calendarEventInvite.Creating)
            {
                CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarEventInvite.EventID);
                if (calendarEvent != null)
                {
                    if (calendarEvent.IsGuildEvent() && calendarEvent.GuildId == inviteeGuildId)
                    {
                        // we can't invite guild members to guild events
                        Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);
                        return;
                    }

                    CalendarInvite invite = new CalendarInvite(Global.CalendarMgr.GetFreeInviteId(), calendarEventInvite.EventID, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
                    Global.CalendarMgr.AddInvite(calendarEvent, invite);
                }
                else
                    Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.EventInvalid);
            }
            else
            {
                if (calendarEventInvite.IsSignUp && inviteeGuildId == GetPlayer().GetGuildId())
                {
                    Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);
                    return;
                }

                CalendarInvite invite = new CalendarInvite(calendarEventInvite.EventID, 0, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
                Global.CalendarMgr.SendCalendarEventInvite(invite);
            }
        }

        [WorldPacketHandler(ClientOpcodes.CalendarEventSignUp)]
        void HandleCalendarEventSignup(CalendarEventSignUp calendarEventSignUp)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarEventSignUp.EventID);
            if (calendarEvent != null)
            {
                if (calendarEvent.IsGuildEvent() && calendarEvent.GuildId != GetPlayer().GetGuildId())
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);
                    return;
                }

                CalendarInviteStatus status = calendarEventSignUp.Tentative ? CalendarInviteStatus.Tentative : CalendarInviteStatus.SignedUp;
                CalendarInvite invite = new CalendarInvite(Global.CalendarMgr.GetFreeInviteId(), calendarEventSignUp.EventID, guid, guid, Time.UnixTime, status, CalendarModerationRank.Player, "");
                Global.CalendarMgr.AddInvite(calendarEvent, invite);
                Global.CalendarMgr.SendCalendarClearPendingAction(guid);
            }
            else
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarEventRsvp)]
        void HandleCalendarEventRsvp(CalendarEventRSVP calendarEventRSVP)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarEventRSVP.EventID);
            if (calendarEvent != null)
            {
                // i think we still should be able to remove self from locked events
                if (calendarEventRSVP.Status != CalendarInviteStatus.Removed && calendarEvent.IsLocked())
                {
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventLocked);
                    return;
                }

                CalendarInvite invite = Global.CalendarMgr.GetInvite(calendarEventRSVP.InviteID);
                if (invite != null)
                {
                    invite.Status = calendarEventRSVP.Status;
                    invite.ResponseTime = Time.UnixTime;

                    Global.CalendarMgr.UpdateInvite(invite);
                    Global.CalendarMgr.SendCalendarEventStatus(calendarEvent, invite);
                    Global.CalendarMgr.SendCalendarClearPendingAction(guid);
                }
                else
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
            }
            else
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarRemoveInvite)]
        void HandleCalendarEventRemoveInvite(CalendarRemoveInvite calendarRemoveInvite)
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
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarEventStatus)]
        void HandleCalendarEventStatus(CalendarEventStatus calendarEventStatus)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarEventStatus.EventID);
            if (calendarEvent != null)
            {
                CalendarInvite invite = Global.CalendarMgr.GetInvite(calendarEventStatus.InviteID);
                if (invite != null)
                {
                    invite.Status = (CalendarInviteStatus)calendarEventStatus.Status;

                    Global.CalendarMgr.UpdateInvite(invite);
                    Global.CalendarMgr.SendCalendarEventStatus(calendarEvent, invite);
                    Global.CalendarMgr.SendCalendarClearPendingAction(calendarEventStatus.Guid);
                }
                else
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
            }
            else
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarEventModeratorStatus)]
        void HandleCalendarEventModeratorStatus(CalendarEventModeratorStatus calendarEventModeratorStatus)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            CalendarEvent calendarEvent = Global.CalendarMgr.GetEvent(calendarEventModeratorStatus.EventID);
            if (calendarEvent != null)
            {
                CalendarInvite invite = Global.CalendarMgr.GetInvite(calendarEventModeratorStatus.InviteID);
                if (invite != null)
                {
                    invite.Rank = (CalendarModerationRank)calendarEventModeratorStatus.Status;
                    Global.CalendarMgr.UpdateInvite(invite);
                    Global.CalendarMgr.SendCalendarEventModeratorStatusAlert(calendarEvent, invite);
                }
                else
                    Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
            }
            else
                Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
        }

        [WorldPacketHandler(ClientOpcodes.CalendarComplain)]
        void HandleCalendarComplain(CalendarComplain calendarComplain)
        {
            // what to do with complains?
        }

        [WorldPacketHandler(ClientOpcodes.CalendarGetNumPending)]
        void HandleCalendarGetNumPending(CalendarGetNumPending calendarGetNumPending)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            uint pending = Global.CalendarMgr.GetPlayerNumPending(guid);

            SendPacket(new CalendarSendNumPending(pending));
        }

        [WorldPacketHandler(ClientOpcodes.SetSavedInstanceExtend)]
        void HandleSetSavedInstanceExtend(SetSavedInstanceExtend setSavedInstanceExtend)
        {
            Player player = GetPlayer();
            if (player)
            {
                InstanceBind instanceBind = player.GetBoundInstance((uint)setSavedInstanceExtend.MapID, (Difficulty)setSavedInstanceExtend.DifficultyID, setSavedInstanceExtend.Extend); // include expired instances if we are toggling extend on
                if (instanceBind == null || instanceBind.save == null || !instanceBind.perm)
                    return;

                BindExtensionState newState;
                if (!setSavedInstanceExtend.Extend || instanceBind.extendState == BindExtensionState.Expired)
                    newState = BindExtensionState.Normal;
                else
                    newState = BindExtensionState.Extended;

                player.BindToInstance(instanceBind.save, true, newState, false);
            }
            /*
            InstancePlayerBind* instanceBind = GetPlayer().GetBoundInstance(setSavedInstanceExtend.MapID, Difficulty(setSavedInstanceExtend.DifficultyID);
            if (!instanceBind || !instanceBind.save)
                return;

            InstanceSave* save = instanceBind.save;
            // http://www.wowwiki.com/Instance_Lock_Extension
            // SendCalendarRaidLockoutUpdated(save);
            */
        }

        public void SendCalendarRaidLockout(InstanceSave save, bool add)
        {
            long currTime = Time.UnixTime;

            if (add)
            {
                CalendarRaidLockoutAdded calendarRaidLockoutAdded = new CalendarRaidLockoutAdded();
                calendarRaidLockoutAdded.InstanceID = save.GetInstanceId();
                calendarRaidLockoutAdded.ServerTime = (uint)currTime;
                calendarRaidLockoutAdded.MapID = (int)save.GetMapId();
                calendarRaidLockoutAdded.DifficultyID = save.GetDifficultyID();
                calendarRaidLockoutAdded.TimeRemaining = (int)(save.GetResetTime() - currTime);
                SendPacket(calendarRaidLockoutAdded);
            }
            else
            {
                CalendarRaidLockoutRemoved calendarRaidLockoutRemoved = new CalendarRaidLockoutRemoved();
                calendarRaidLockoutRemoved.InstanceID = save.GetInstanceId();
                calendarRaidLockoutRemoved.MapID = (int)save.GetMapId();
                calendarRaidLockoutRemoved.DifficultyID = save.GetDifficultyID();
                SendPacket(calendarRaidLockoutRemoved);
            }
        }

        public void SendCalendarRaidLockoutUpdated(InstanceSave save)
        {
            if (save == null)
                return;

            long currTime = Time.UnixTime;

            CalendarRaidLockoutUpdated packet = new CalendarRaidLockoutUpdated();
            packet.DifficultyID = (uint)save.GetDifficultyID();
            packet.MapID = (int)save.GetMapId();
            packet.NewTimeRemaining = 0; // FIXME
            packet.OldTimeRemaining = (int)(save.GetResetTime() - currTime);

            SendPacket(packet);
        }
    }
}
