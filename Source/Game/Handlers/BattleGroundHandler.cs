/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BattlemasterHello)]
        void HandleBattlemasterHello(Hello hello)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(hello.Unit, NPCFlags.BattleMaster, NPCFlags2.None);
            if (!unit)
                return;

            // Stop the npc if moving
            unit.PauseMovement(WorldConfig.GetUIntValue(WorldCfg.CreatureStopForPlayer));
            unit.SetHomePosition(unit.GetPosition());

            BattlegroundTypeId bgTypeId = Global.BattlegroundMgr.GetBattleMasterBG(unit.GetEntry());

            if (!GetPlayer().GetBGAccessByLevel(bgTypeId))
            {
                // temp, must be gossip message...
                SendNotification(CypherStrings.YourBgLevelReqError);
                return;
            }

            Global.BattlegroundMgr.SendBattlegroundList(GetPlayer(), hello.Unit, bgTypeId);
        }

        [WorldPacketHandler(ClientOpcodes.BattlemasterJoin)]
        void HandleBattlemasterJoin(BattlemasterJoin battlemasterJoin)
        {
            bool isPremade = false;

            if (battlemasterJoin.QueueIDs.Empty())
            {
                Log.outError(LogFilter.Network, $"Battleground: no bgtype received. possible cheater? {_player.GetGUID()}");
                return;
            }

            BattlegroundQueueTypeId bgQueueTypeId = BattlegroundQueueTypeId.FromPacked(battlemasterJoin.QueueIDs[0]);
            if (!Global.BattlegroundMgr.IsValidQueueId(bgQueueTypeId))
            {
                Log.outError(LogFilter.Network, $"Battleground: invalid bg queue {bgQueueTypeId} received. possible cheater? {_player.GetGUID()}");
                return;
            }

            BattlemasterListRecord battlemasterListEntry = CliDB.BattlemasterListStorage.LookupByKey(bgQueueTypeId.BattlemasterListId);
            if (Global.DisableMgr.IsDisabledFor(DisableType.Battleground, bgQueueTypeId.BattlemasterListId, null) || battlemasterListEntry.Flags.HasAnyFlag(BattlemasterListFlags.Disabled))
            {
                GetPlayer().SendSysMessage(CypherStrings.BgDisabled);
                return;
            }

            BattlegroundTypeId bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;

            // ignore if player is already in BG
            if (GetPlayer().InBattleground())
                return;

            // get bg instance or bg template if instance not found
            Battleground bg = Global.BattlegroundMgr.GetBattlegroundTemplate(bgTypeId);
            if (!bg)
                return;

            // expected bracket entry
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), GetPlayer().GetLevel());
            if (bracketEntry == null)
                return;

            GroupJoinBattlegroundResult err = GroupJoinBattlegroundResult.None;

            Group grp = _player.GetGroup();

            BattlefieldStatusFailed battlefieldStatusFailed;
            // check queue conditions
            if (grp == null)
            {
                if (GetPlayer().IsUsingLfg())
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bg, GetPlayer(), 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check Deserter debuff
                if (!GetPlayer().CanJoinToBattleground(bg))
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bg, GetPlayer(), 0, GroupJoinBattlegroundResult.Deserters);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                bool isInRandomBgQueue = _player.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RB, BattlegroundQueueIdType.Battleground, false, 0))
                    || _player.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RandomEpic, BattlegroundQueueIdType.Battleground, false, 0));
                if (bgTypeId != BattlegroundTypeId.RB && bgTypeId != BattlegroundTypeId.RandomEpic && isInRandomBgQueue)
                {
                    // player is already in random queue
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bg, GetPlayer(), 0, GroupJoinBattlegroundResult.InRandomBg);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                if (_player.InBattlegroundQueue() && !isInRandomBgQueue && (bgTypeId == BattlegroundTypeId.RB || bgTypeId == BattlegroundTypeId.RandomEpic))
                {
                    // player is already in queue, can't start random queue
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bg, GetPlayer(), 0, GroupJoinBattlegroundResult.InNonRandomBg);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check if already in queue
                if (GetPlayer().GetBattlegroundQueueIndex(bgQueueTypeId) < SharedConst.MaxPlayerBGQueues)
                    return;  // player is already in this queue

                // check if has free queue slots
                if (!GetPlayer().HasFreeBattlegroundQueueId())
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bg, GetPlayer(), 0, GroupJoinBattlegroundResult.TooManyQueues);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check Freeze debuff
                if (_player.HasAura(9454))
                    return;

                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo = bgQueue.AddGroup(GetPlayer(), null, bgTypeId, bracketEntry, 0, false, isPremade, 0, 0);

                uint avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                uint queueSlot = GetPlayer().AddBattlegroundQueueId(bgQueueTypeId);

                Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, bg, GetPlayer(), queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, ginfo.ArenaType, false);
                SendPacket(battlefieldStatusQueued);

                Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for bg queue {bgQueueTypeId}, {_player.GetGUID()}, NAME {_player.GetName()}");
            }
            else
            {
                if (grp.GetLeaderGUID() != GetPlayer().GetGUID())
                    return;

                ObjectGuid errorGuid;
                err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, 0, bg.GetMaxPlayersPerTeam(), false, 0, out errorGuid);
                isPremade = (grp.GetMembersCount() >= bg.GetMinPlayersPerTeam());

                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo = null;
                uint avgTime = 0;

                if (err == 0)
                {
                    Log.outDebug(LogFilter.Battleground, "Battleground: the following players are joining as group:");
                    ginfo = bgQueue.AddGroup(GetPlayer(), grp, bgTypeId, bracketEntry, 0, false, isPremade, 0, 0);
                    avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                }

                for (GroupReference refe = grp.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player member = refe.GetSource();
                    if (!member)
                        continue;   // this should never happen

                    if (err != 0)
                    {
                        BattlefieldStatusFailed battlefieldStatus;
                        Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatus, bg, GetPlayer(), 0, err, errorGuid);
                        member.SendPacket(battlefieldStatus);
                        continue;
                    }

                    // add to queue
                    uint queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

                    Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, bg, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, ginfo.ArenaType, true);
                    member.SendPacket(battlefieldStatusQueued);
                    Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for bg queue {bgQueueTypeId}, {member.GetGUID()}, NAME {member.GetName()}");
                }
                Log.outDebug(LogFilter.Battleground, "Battleground: group end");
            }

            Global.BattlegroundMgr.ScheduleQueueUpdate(0, bgQueueTypeId, bracketEntry.GetBracketId());
        }

        [WorldPacketHandler(ClientOpcodes.PvpLogData)]
        void HandlePVPLogData(PVPLogDataRequest packet)
        {
            Battleground bg = GetPlayer().GetBattleground();
            if (!bg)
                return;

            // Prevent players from sending BuildPvpLogDataPacket in an arena except for when sent in Battleground.EndBattleground.
            if (bg.IsArena())
                return;

            PVPLogDataMessage pvpLogData = new PVPLogDataMessage();
            bg.BuildPvPLogDataPacket(out pvpLogData.Data);
            SendPacket(pvpLogData);
        }

        [WorldPacketHandler(ClientOpcodes.BattlefieldList)]
        void HandleBattlefieldList(BattlefieldListRequest battlefieldList)
        {
            BattlemasterListRecord bl = CliDB.BattlemasterListStorage.LookupByKey(battlefieldList.ListID);
            if (bl == null)
            {
                Log.outDebug(LogFilter.Battleground, "BattlegroundHandler: invalid bgtype ({0}) with player (Name: {1}, GUID: {2}) received.", battlefieldList.ListID, GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            Global.BattlegroundMgr.SendBattlegroundList(GetPlayer(), ObjectGuid.Empty, (BattlegroundTypeId)battlefieldList.ListID);
        }

        [WorldPacketHandler(ClientOpcodes.BattlefieldPort)]
        void HandleBattleFieldPort(BattlefieldPort battlefieldPort)
        {
            if (!GetPlayer().InBattlegroundQueue())
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Player not in queue!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite);
                return;
            }

            BattlegroundQueueTypeId bgQueueTypeId = GetPlayer().GetBattlegroundQueueTypeId(battlefieldPort.Ticket.Id);
            if (bgQueueTypeId == default)
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Invalid queueSlot!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite);
                return;
            }

            BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);

            //we must use temporary variable, because GroupQueueInfo pointer can be deleted in BattlegroundQueue.RemovePlayer() function
            GroupQueueInfo ginfo;
            if (!bgQueue.GetPlayerGroupInfoData(GetPlayer().GetGUID(), out ginfo))
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Player not in queue (No player Group Info)!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite);
                return;
            }
            // if action == 1, then instanceId is required
            if (ginfo.IsInvitedToBGInstanceGUID == 0 && battlefieldPort.AcceptedInvite)
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Player is not invited to any bg!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite);
                return;
            }

            BattlegroundTypeId bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;
            // BGTemplateId returns Battleground_AA when it is arena queue.
            // Do instance id search as there is no AA bg instances.
            Battleground bg = Global.BattlegroundMgr.GetBattleground(ginfo.IsInvitedToBGInstanceGUID, bgTypeId == BattlegroundTypeId.AA ? BattlegroundTypeId.None : bgTypeId);
            if (!bg)
            {
                if (battlefieldPort.AcceptedInvite)
                {
                    Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Cant find BG with id {5}!",
                        GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite, ginfo.IsInvitedToBGInstanceGUID);
                    return;
                }

                bg = Global.BattlegroundMgr.GetBattlegroundTemplate(bgTypeId);
                if (!bg)
                {
                    Log.outError(LogFilter.Network, "BattlegroundHandler: bg_template not found for type id {0}.", bgTypeId);
                    return;
                }
            }

            // get real bg type
            bgTypeId = bg.GetTypeID();

            // expected bracket entry
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), GetPlayer().GetLevel());
            if (bracketEntry == null)
                return;

            //some checks if player isn't cheating - it is not exactly cheating, but we cannot allow it
            if (battlefieldPort.AcceptedInvite && ginfo.ArenaType == 0)
            {
                //if player is trying to enter Battleground(not arena!) and he has deserter debuff, we must just remove him from queue
                if (!GetPlayer().CanJoinToBattleground(bg))
                {
                    // send bg command result to show nice message
                    BattlefieldStatusFailed battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatus, bg, GetPlayer(), battlefieldPort.Ticket.Id, GroupJoinBattlegroundResult.Deserters);
                    SendPacket(battlefieldStatus);
                    battlefieldPort.AcceptedInvite = false;
                    Log.outDebug(LogFilter.Battleground, "Player {0} ({1}) has a deserter debuff, do not port him to Battleground!", GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                }
                //if player don't match Battlegroundmax level, then do not allow him to enter! (this might happen when player leveled up during his waiting in queue
                if (GetPlayer().GetLevel() > bg.GetMaxLevel())
                {
                    Log.outDebug(LogFilter.Network, "Player {0} ({1}) has level ({2}) higher than maxlevel ({3}) of Battleground({4})! Do not port him to Battleground!",
                        GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), GetPlayer().GetLevel(), bg.GetMaxLevel(), bg.GetTypeID());
                    battlefieldPort.AcceptedInvite = false;
                }
            }

            if (battlefieldPort.AcceptedInvite)
            {
                // check Freeze debuff
                if (GetPlayer().HasAura(9454))
                    return;

                if (!GetPlayer().IsInvitedForBattlegroundQueueType(bgQueueTypeId))
                    return;                                 // cheating?

                if (!GetPlayer().InBattleground())
                    GetPlayer().SetBattlegroundEntryPoint();

                // resurrect the player
                if (!GetPlayer().IsAlive())
                {
                    GetPlayer().ResurrectPlayer(1.0f);
                    GetPlayer().SpawnCorpseBones();
                }
                // stop taxi flight at port
                if (GetPlayer().IsInFlight())
                {
                    GetPlayer().GetMotionMaster().MovementExpired();
                    GetPlayer().CleanupAfterTaxiFlight();
                }

                BattlefieldStatusActive battlefieldStatus;
                Global.BattlegroundMgr.BuildBattlegroundStatusActive(out battlefieldStatus, bg, GetPlayer(), battlefieldPort.Ticket.Id, GetPlayer().GetBattlegroundQueueJoinTime(bgQueueTypeId), bg.GetArenaType());
                SendPacket(battlefieldStatus);

                // remove BattlegroundQueue status from BGmgr
                bgQueue.RemovePlayer(GetPlayer().GetGUID(), false);
                // this is still needed here if Battleground"jumping" shouldn't add deserter debuff
                // also this is required to prevent stuck at old Battlegroundafter SetBattlegroundId set to new
                Battleground currentBg = GetPlayer().GetBattleground();
                if (currentBg)
                    currentBg.RemovePlayerAtLeave(GetPlayer().GetGUID(), false, true);

                // set the destination instance id
                GetPlayer().SetBattlegroundId(bg.GetInstanceID(), bgTypeId);
                // set the destination team
                GetPlayer().SetBGTeam(ginfo.Team);

                Global.BattlegroundMgr.SendToBattleground(GetPlayer(), ginfo.IsInvitedToBGInstanceGUID, bgTypeId);
                Log.outDebug(LogFilter.Battleground, $"Battleground: player {_player.GetName()} ({_player.GetGUID()}) joined battle for bg {bg.GetInstanceID()}, bgtype {bg.GetTypeID()}, queue {bgQueueTypeId}.");
            }
            else // leave queue
            {
                // if player leaves rated arena match before match start, it is counted as he played but he lost
                if (ginfo.IsRated && ginfo.IsInvitedToBGInstanceGUID != 0)
                {
                    ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById((uint)ginfo.Team);
                    if (at != null)
                    {
                        Log.outDebug(LogFilter.Battleground, "UPDATING memberLost's personal arena rating for {0} by opponents rating: {1}, because he has left queue!", GetPlayer().GetGUID().ToString(), ginfo.OpponentsTeamRating);
                        at.MemberLost(GetPlayer(), ginfo.OpponentsMatchmakerRating);
                        at.SaveToDB();
                    }
                }
                BattlefieldStatusNone battlefieldStatus = new BattlefieldStatusNone();
                battlefieldStatus.Ticket = battlefieldPort.Ticket;
                SendPacket(battlefieldStatus);

                GetPlayer().RemoveBattlegroundQueueId(bgQueueTypeId);  // must be called this way, because if you move this call to queue.removeplayer, it causes bugs
                bgQueue.RemovePlayer(GetPlayer().GetGUID(), true);
                // player left queue, we should update it - do not update Arena Queue
                if (ginfo.ArenaType == 0)
                    Global.BattlegroundMgr.ScheduleQueueUpdate(ginfo.ArenaMatchmakerRating, bgQueueTypeId, bracketEntry.GetBracketId());

                Log.outDebug(LogFilter.Battleground, $"Battleground: player {_player.GetName()} ({_player.GetGUID()}) left queue for bgtype { bg.GetTypeID()}, queue {bgQueueTypeId}.");
            }
        }

        [WorldPacketHandler(ClientOpcodes.BattlefieldLeave)]
        void HandleBattlefieldLeave(BattlefieldLeave packet)
        {
            // not allow leave Battlegroundin combat
            if (GetPlayer().IsInCombat())
            {
                Battleground bg = GetPlayer().GetBattleground();
                if (bg)
                    if (bg.GetStatus() != BattlegroundStatus.WaitLeave)
                        return;
            }

            GetPlayer().LeaveBattleground();
        }

        [WorldPacketHandler(ClientOpcodes.RequestBattlefieldStatus)]
        void HandleRequestBattlefieldStatus(RequestBattlefieldStatus packet)
        {
            // we must update all queues here
            Battleground bg = null;
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            {
                BattlegroundQueueTypeId bgQueueTypeId = GetPlayer().GetBattlegroundQueueTypeId(i);
                if (bgQueueTypeId == default)
                    continue;

                BattlegroundTypeId bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;
                ArenaTypes arenaType = (ArenaTypes)bgQueueTypeId.TeamSize;
                bg = _player.GetBattleground();
                if (bg && bg.GetQueueId() == bgQueueTypeId)
                {
                    //i cannot check any variable from player class because player class doesn't know if player is in 2v2 / 3v3 or 5v5 arena
                    //so i must use bg pointer to get that information
                    Global.BattlegroundMgr.BuildBattlegroundStatusActive(out BattlefieldStatusActive battlefieldStatus, bg, _player, i, _player.GetBattlegroundQueueJoinTime(bgQueueTypeId), arenaType);
                    SendPacket(battlefieldStatus);
                    continue;
                }

                //we are sending update to player about queue - he can be invited there!
                //get GroupQueueInfo for queue status
                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo;
                if (!bgQueue.GetPlayerGroupInfoData(GetPlayer().GetGUID(), out ginfo))
                    continue;

                if (ginfo.IsInvitedToBGInstanceGUID != 0)
                {
                    bg = Global.BattlegroundMgr.GetBattleground(ginfo.IsInvitedToBGInstanceGUID, bgTypeId);
                    if (!bg)
                        continue;

                    BattlefieldStatusNeedConfirmation battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusNeedConfirmation(out battlefieldStatus, bg, GetPlayer(), i, GetPlayer().GetBattlegroundQueueJoinTime(bgQueueTypeId), Time.GetMSTimeDiff(Time.GetMSTime(), ginfo.RemoveInviteTime), arenaType);
                    SendPacket(battlefieldStatus);
                }
                else
                {
                    bg = Global.BattlegroundMgr.GetBattlegroundTemplate(bgTypeId);
                    if (!bg)
                        continue;

                    // expected bracket entry
                    PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), GetPlayer().GetLevel());
                    if (bracketEntry == null)
                        continue;

                    uint avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                    BattlefieldStatusQueued battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out battlefieldStatus, bg, GetPlayer(), i, GetPlayer().GetBattlegroundQueueJoinTime(bgQueueTypeId), bgQueueTypeId, avgTime, arenaType, ginfo.Players.Count > 1);
                    SendPacket(battlefieldStatus);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.BattlemasterJoinArena)]
        void HandleBattlemasterJoinArena(BattlemasterJoinArena packet)
        {
            // ignore if we already in BG or BG queue
            if (GetPlayer().InBattleground())
                return;

            ArenaTypes arenatype = (ArenaTypes)ArenaTeam.GetTypeBySlot(packet.TeamSizeIndex);

            //check existence
            Battleground bg = Global.BattlegroundMgr.GetBattlegroundTemplate(BattlegroundTypeId.AA);
            if (!bg)
            {
                Log.outError(LogFilter.Network, "Battleground: template bg (all arenas) not found");
                return;
            }

            if (Global.DisableMgr.IsDisabledFor(DisableType.Battleground, (uint)BattlegroundTypeId.AA, null))
            {
                GetPlayer().SendSysMessage(CypherStrings.ArenaDisabled);
                return;
            }

            BattlegroundTypeId bgTypeId = bg.GetTypeID();
            BattlegroundQueueTypeId bgQueueTypeId = Global.BattlegroundMgr.BGQueueTypeId((ushort)bgTypeId, BattlegroundQueueIdType.Arena, true, arenatype);
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bg.GetMapId(), GetPlayer().GetLevel());
            if (bracketEntry == null)
                return;

            Group grp = GetPlayer().GetGroup();
            // no group found, error
            if (!grp)
                return;
            if (grp.GetLeaderGUID() != GetPlayer().GetGUID())
                return;

            uint ateamId = GetPlayer().GetArenaTeamId(packet.TeamSizeIndex);
            // check real arenateam existence only here (if it was moved to group.CanJoin .. () then we would ahve to get it twice)
            ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById(ateamId);
            if (at == null)
                return;

            // get the team rating for queuing
            uint arenaRating = at.GetRating();
            uint matchmakerRating = at.GetAverageMMR(grp);
            // the arenateam id must match for everyone in the group

            if (arenaRating <= 0)
                arenaRating = 1;

            BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);

            uint avgTime = 0;
            GroupQueueInfo ginfo = null;

            ObjectGuid errorGuid;
            var err = grp.CanJoinBattlegroundQueue(bg, bgQueueTypeId, (uint)arenatype, (uint)arenatype, true, packet.TeamSizeIndex, out errorGuid);
            if (err == 0)
            {
                Log.outDebug(LogFilter.Battleground, "Battleground: arena team id {0}, leader {1} queued with matchmaker rating {2} for type {3}", GetPlayer().GetArenaTeamId(packet.TeamSizeIndex), GetPlayer().GetName(), matchmakerRating, arenatype);

                ginfo = bgQueue.AddGroup(GetPlayer(), grp, bgTypeId, bracketEntry, arenatype, true, false, arenaRating, matchmakerRating, ateamId);
                avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
            }

            for (GroupReference refe = grp.GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player member = refe.GetSource();
                if (!member)
                    continue;

                if (err != 0)
                {
                    BattlefieldStatusFailed battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatus, bg, GetPlayer(), 0, err, errorGuid);
                    member.SendPacket(battlefieldStatus);
                    continue;
                }

                // add to queue
                uint queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

                Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, bg, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, arenatype, true);
                member.SendPacket(battlefieldStatusQueued);

                Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for arena as group bg queue {bgQueueTypeId}, {member.GetGUID()}, NAME {member.GetName()}");
            }

            Global.BattlegroundMgr.ScheduleQueueUpdate(matchmakerRating, bgQueueTypeId, bracketEntry.GetBracketId());
        }

        [WorldPacketHandler(ClientOpcodes.ReportPvpPlayerAfk)]
        void HandleReportPvPAFK(ReportPvPPlayerAFK reportPvPPlayerAFK)
        {
            Player reportedPlayer = Global.ObjAccessor.FindPlayer(reportPvPPlayerAFK.Offender);
            if (!reportedPlayer)
            {
                Log.outDebug(LogFilter.Battleground, "WorldSession.HandleReportPvPAFK: player not found");
                return;
            }

            Log.outDebug(LogFilter.BattlegroundReportPvpAfk, "WorldSession.HandleReportPvPAFK:  {0} [IP: {1}] reported {2}", _player.GetName(), _player.GetSession().GetRemoteAddress(), reportedPlayer.GetGUID().ToString());

            reportedPlayer.ReportedAfkBy(GetPlayer());
        }

        [WorldPacketHandler(ClientOpcodes.RequestRatedBattlefieldInfo)]
        void HandleRequestRatedBattlefieldInfo(RequestRatedBattlefieldInfo packet)
        {
            RatedBattlefieldInfo ratedBattlefieldInfo = new RatedBattlefieldInfo();
            SendPacket(ratedBattlefieldInfo);
        }

        [WorldPacketHandler(ClientOpcodes.GetPvpOptionsEnabled, Processing = PacketProcessing.Inplace)]
        void HandleGetPVPOptionsEnabled(GetPVPOptionsEnabled packet)
        {
            // This packet is completely irrelevant, it triggers PVP_TYPES_ENABLED lua event but that is not handled in interface code as of 6.1.2
            PVPOptionsEnabled pvpOptionsEnabled = new PVPOptionsEnabled();
            pvpOptionsEnabled.PugBattlegrounds = true;
            SendPacket(new PVPOptionsEnabled());
        }

        [WorldPacketHandler(ClientOpcodes.RequestPvpRewards)]
        void HandleRequestPvpReward(RequestPVPRewards packet)
        {
            GetPlayer().SendPvpRewards();
        }

        [WorldPacketHandler(ClientOpcodes.AreaSpiritHealerQuery)]
        void HandleAreaSpiritHealerQuery(AreaSpiritHealerQuery areaSpiritHealerQuery)
        {
            Creature unit = ObjectAccessor.GetCreature(GetPlayer(), areaSpiritHealerQuery.HealerGuid);
            if (!unit)
                return;

            if (!unit.IsSpiritService())                            // it's not spirit service
                return;

            Battleground bg = GetPlayer().GetBattleground();
            if (bg != null)
                Global.BattlegroundMgr.SendAreaSpiritHealerQuery(GetPlayer(), bg, areaSpiritHealerQuery.HealerGuid);

            BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(GetPlayer().GetZoneId());
            if (bf != null)
                bf.SendAreaSpiritHealerQuery(GetPlayer(), areaSpiritHealerQuery.HealerGuid);
        }

        [WorldPacketHandler(ClientOpcodes.AreaSpiritHealerQueue)]
        void HandleAreaSpiritHealerQueue(AreaSpiritHealerQueue areaSpiritHealerQueue)
        {
            Creature unit = ObjectAccessor.GetCreature(GetPlayer(), areaSpiritHealerQueue.HealerGuid);
            if (!unit)
                return;

            if (!unit.IsSpiritService())                            // it's not spirit service
                return;

            Battleground bg = GetPlayer().GetBattleground();
            if (bg)
                bg.AddPlayerToResurrectQueue(areaSpiritHealerQueue.HealerGuid, GetPlayer().GetGUID());

            BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(GetPlayer().GetZoneId());
            if (bf != null)
                bf.AddPlayerToResurrectQueue(areaSpiritHealerQueue.HealerGuid, GetPlayer().GetGUID());
        }

        [WorldPacketHandler(ClientOpcodes.HearthAndResurrect)]
        void HandleHearthAndResurrect(HearthAndResurrect packet)
        {
            if (GetPlayer().IsInFlight())
                return;

            BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(GetPlayer().GetZoneId());
            if (bf != null)
            {
                bf.PlayerAskToLeave(_player);
                return;
            }

            AreaTableRecord atEntry = CliDB.AreaTableStorage.LookupByKey(GetPlayer().GetAreaId());
            if (atEntry == null || !atEntry.Flags[0].HasAnyFlag(AreaFlags.CanHearthAndResurrect))
                return;

            GetPlayer().BuildPlayerRepop();
            GetPlayer().ResurrectPlayer(1.0f);
            GetPlayer().TeleportTo(GetPlayer().GetHomebind());
        }
    }
}
