// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
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
            if (unit == null)
                return;

            // Stop the npc if moving
            uint pause = unit.GetMovementTemplate().GetInteractionPauseTimer();
            if (pause != 0)
                unit.PauseMovement(pause);
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
            if (Global.DisableMgr.IsDisabledFor(DisableType.Battleground, bgQueueTypeId.BattlemasterListId, null) || battlemasterListEntry.HasFlag(BattlemasterListFlags.Disabled))
            {
                GetPlayer().SendSysMessage(CypherStrings.BgDisabled);
                return;
            }

            BattlegroundTypeId bgTypeId = (BattlegroundTypeId)bgQueueTypeId.BattlemasterListId;

            // ignore if player is already in BG
            if (GetPlayer().InBattleground())
                return;

            // get bg instance or bg template if instance not found
            BattlegroundTemplate bgTemplate = Global.BattlegroundMgr.GetBattlegroundTemplateByTypeId(bgTypeId);
            if (bgTemplate == null)
                return;

            // expected bracket entry
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel((uint)bgTemplate.MapIDs[0], GetPlayer().GetLevel());
            if (bracketEntry == null)
                return;

            GroupJoinBattlegroundResult err = GroupJoinBattlegroundResult.None;

            Group group = _player.GetGroup();

            Team getQueueTeam()
            {
                // mercenary applies only to unrated battlegrounds
                if (!bgQueueTypeId.Rated && !bgTemplate.IsArena())
                {
                    if (_player.HasAura(BattlegroundConst.SpellMercenaryContractHorde))
                        return Team.Horde;

                    if (_player.HasAura(BattlegroundConst.SpellMercenaryContractAlliance))
                        return Team.Alliance;
                }

                return _player.GetTeam();
            }

            BattlefieldStatusFailed battlefieldStatusFailed;
            // check queue conditions
            if (group == null)
            {
                if (GetPlayer().IsUsingLfg())
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, GetPlayer(), 0, GroupJoinBattlegroundResult.LfgCantUseBattleground);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check RBAC permissions
                if (!GetPlayer().CanJoinToBattleground(bgTemplate))
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, GetPlayer(), 0, GroupJoinBattlegroundResult.JoinTimedOut);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check Deserter debuff
                if (GetPlayer().IsDeserter())
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, GetPlayer(), 0, GroupJoinBattlegroundResult.Deserters);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }
                
                bool isInRandomBgQueue = _player.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RB, BattlegroundQueueIdType.Battleground, false, 0))
                    || _player.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RandomEpic, BattlegroundQueueIdType.Battleground, false, 0));
                if (!Global.BattlegroundMgr.IsRandomBattleground(bgTypeId) && isInRandomBgQueue)
                {
                    // player is already in random queue
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, GetPlayer(), 0, GroupJoinBattlegroundResult.InRandomBg);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                if (_player.InBattlegroundQueue(true) && !isInRandomBgQueue && Global.BattlegroundMgr.IsRandomBattleground(bgTypeId))
                {
                    // player is already in queue, can't start random queue
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, GetPlayer(), 0, GroupJoinBattlegroundResult.InNonRandomBg);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check if already in queue
                if (GetPlayer().GetBattlegroundQueueIndex(bgQueueTypeId) < SharedConst.MaxPlayerBGQueues)
                    return;  // player is already in this queue

                // check if has free queue slots
                if (!GetPlayer().HasFreeBattlegroundQueueId())
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatusFailed, bgQueueTypeId, GetPlayer(), 0, GroupJoinBattlegroundResult.TooManyQueues);
                    SendPacket(battlefieldStatusFailed);
                    return;
                }

                // check Freeze debuff
                if (_player.HasAura(9454))
                    return;

                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo = bgQueue.AddGroup(GetPlayer(), null, getQueueTeam(), bracketEntry, isPremade, 0, 0);

                uint avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                uint queueSlot = GetPlayer().AddBattlegroundQueueId(bgQueueTypeId);

                Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, GetPlayer(), queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, false);
                SendPacket(battlefieldStatusQueued);

                Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for bg queue {bgQueueTypeId}, {_player.GetGUID()}, NAME {_player.GetName()}");
            }
            else
            {
                if (group.GetLeaderGUID() != GetPlayer().GetGUID())
                    return;

                ObjectGuid errorGuid;
                err = group.CanJoinBattlegroundQueue(bgTemplate, bgQueueTypeId, 0, bgTemplate.GetMaxPlayersPerTeam(), false, 0, out errorGuid);
                isPremade = (group.GetMembersCount() >= bgTemplate.GetMinPlayersPerTeam());

                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                GroupQueueInfo ginfo = null;
                uint avgTime = 0;

                if (err == 0)
                {
                    Log.outDebug(LogFilter.Battleground, "Battleground: the following players are joining as group:");
                    ginfo = bgQueue.AddGroup(GetPlayer(), group, getQueueTeam(), bracketEntry, isPremade, 0, 0);
                    avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                }

                foreach (GroupReference groupRef in group.GetMembers())
                {
                    Player member = groupRef.GetSource();
                    if (err != 0)
                    {
                        BattlefieldStatusFailed battlefieldStatus;
                        Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatus, bgQueueTypeId, GetPlayer(), 0, err, errorGuid);
                        member.SendPacket(battlefieldStatus);
                        continue;
                    }

                    // add to queue
                    uint queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

                    Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, true);
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
            if (bg == null)
                return;

            // Prevent players from sending BuildPvpLogDataPacket in an arena except for when sent in Battleground.EndBattleground.
            if (bg.IsArena())
                return;

            PVPMatchStatisticsMessage pvpMatchStatistics = new();
            bg.BuildPvPLogDataPacket(out pvpMatchStatistics.Data);
            SendPacket(pvpMatchStatistics);
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
            BattlegroundTemplate bgTemplate = Global.BattlegroundMgr.GetBattlegroundTemplateByTypeId(bgTypeId);
            if (bgTemplate == null)
            {
                Log.outError(LogFilter.Network, $"BattlegroundHandle: BattlegroundTemplate not found for type id {bgTypeId}.");
                return;
            }

            uint mapId = (uint)bgTemplate.MapIDs[0];

            // BGTemplateId returns Battleground_AA when it is arena queue.
            // Do instance id search as there is no AA bg instances.
            Battleground bg = Global.BattlegroundMgr.GetBattleground(ginfo.IsInvitedToBGInstanceGUID, bgTypeId == BattlegroundTypeId.AA ? BattlegroundTypeId.None : bgTypeId);
            if (bg == null && battlefieldPort.AcceptedInvite)
            {
                Log.outDebug(LogFilter.Battleground, "CMSG_BATTLEFIELD_PORT {0} Slot: {1}, Unk: {2}, Time: {3}, AcceptedInvite: {4}. Cant find BG with id {5}!",
                    GetPlayerInfo(), battlefieldPort.Ticket.Id, battlefieldPort.Ticket.Type, battlefieldPort.Ticket.Time, battlefieldPort.AcceptedInvite, ginfo.IsInvitedToBGInstanceGUID);
                return;
            }
            else if (bg != null)
                mapId = bg.GetMapId();

            // expected bracket entry
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(mapId, GetPlayer().GetLevel());
            if (bracketEntry == null)
                return;

            //some checks if player isn't cheating - it is not exactly cheating, but we cannot allow it
            if (battlefieldPort.AcceptedInvite && bgQueue.GetQueueId().TeamSize == 0)
            {
                //if player is trying to enter Battleground(not arena!) and he has deserter debuff, we must just remove him from queue
                if (!GetPlayer().IsDeserter())
                {
                    // send bg command result to show nice message
                    BattlefieldStatusFailed battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatus, bgQueueTypeId, GetPlayer(), battlefieldPort.Ticket.Id, GroupJoinBattlegroundResult.Deserters);
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
                GetPlayer().FinishTaxiFlight();

                BattlefieldStatusActive battlefieldStatus;
                Global.BattlegroundMgr.BuildBattlegroundStatusActive(out battlefieldStatus, bg, GetPlayer(), battlefieldPort.Ticket.Id, GetPlayer().GetBattlegroundQueueJoinTime(bgQueueTypeId), bgQueueTypeId);
                SendPacket(battlefieldStatus);

                // remove BattlegroundQueue status from BGmgr
                bgQueue.RemovePlayer(GetPlayer().GetGUID(), false);
                // this is still needed here if Battleground"jumping" shouldn't add deserter debuff
                // also this is required to prevent stuck at old Battlegroundafter SetBattlegroundId set to new
                Battleground currentBg = GetPlayer().GetBattleground();
                if (currentBg != null)
                    currentBg.RemovePlayerAtLeave(GetPlayer().GetGUID(), false, true);

                // set the destination instance id
                GetPlayer().SetBattlegroundId(bg.GetInstanceID(), bg.GetTypeID(), bgQueueTypeId);
                // set the destination team
                GetPlayer().SetBGTeam(ginfo.Team);

                Global.BattlegroundMgr.SendToBattleground(GetPlayer(), ginfo.IsInvitedToBGInstanceGUID, bgTypeId);
                Log.outDebug(LogFilter.Battleground, $"Battleground: player {_player.GetName()} ({_player.GetGUID()}) joined battle for bg {bg.GetInstanceID()}, bgtype {bg.GetTypeID()}, queue {bgQueueTypeId}.");
            }
            else // leave queue
            {
                // if player leaves rated arena match before match start, it is counted as he played but he lost
                if (bgQueue.GetQueueId().Rated && ginfo.IsInvitedToBGInstanceGUID != 0)
                {
                    ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById((uint)ginfo.Team);
                    if (at != null)
                    {
                        Log.outDebug(LogFilter.Battleground, "UPDATING memberLost's personal arena rating for {0} by opponents rating: {1}, because he has left queue!", GetPlayer().GetGUID().ToString(), ginfo.OpponentsTeamRating);
                        at.MemberLost(GetPlayer(), ginfo.OpponentsMatchmakerRating);
                        at.SaveToDB();
                    }
                }
                BattlefieldStatusNone battlefieldStatus = new();
                battlefieldStatus.Ticket = battlefieldPort.Ticket;
                SendPacket(battlefieldStatus);

                GetPlayer().RemoveBattlegroundQueueId(bgQueueTypeId);  // must be called this way, because if you move this call to queue.removeplayer, it causes bugs
                bgQueue.RemovePlayer(GetPlayer().GetGUID(), true);
                // player left queue, we should update it - do not update Arena Queue
                if (bgQueue.GetQueueId().TeamSize == 0)
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
                if (bg != null)
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
                bg = _player.GetBattleground();
                if (bg != null)
                {
                    BattlegroundPlayer bgPlayer = bg.GetBattlegroundPlayerData(_player.GetGUID());
                    if (bgPlayer != null && bgPlayer.queueTypeId == bgQueueTypeId)
                    {
                        //i cannot check any variable from player class because player class doesn't know if player is in 2v2 / 3v3 or 5v5 arena
                        //so i must use bg pointer to get that information
                        Global.BattlegroundMgr.BuildBattlegroundStatusActive(out BattlefieldStatusActive battlefieldStatus, bg, _player, i, _player.GetBattlegroundQueueJoinTime(bgQueueTypeId), bgQueueTypeId);
                        SendPacket(battlefieldStatus);
                        continue;
                    }
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
                    if (bg == null)
                        continue;

                    BattlefieldStatusNeedConfirmation battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusNeedConfirmation(out battlefieldStatus, bg, GetPlayer(), i, GetPlayer().GetBattlegroundQueueJoinTime(bgQueueTypeId), Time.GetMSTimeDiff(GameTime.GetGameTimeMS(), ginfo.RemoveInviteTime), bgQueueTypeId);
                    SendPacket(battlefieldStatus);
                }
                else
                {
                    BattlegroundTemplate bgTemplate = Global.BattlegroundMgr.GetBattlegroundTemplateByTypeId(bgTypeId);
                    if (bgTemplate == null)
                        continue;

                    // expected bracket entry
                    PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel((uint)bgTemplate.MapIDs[0], _player.GetLevel());
                    if (bracketEntry == null)
                        continue;

                    uint avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
                    BattlefieldStatusQueued battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out battlefieldStatus, _player, i, _player.GetBattlegroundQueueJoinTime(bgQueueTypeId), bgQueueTypeId, avgTime, ginfo.Players.Count > 1);
                    SendPacket(battlefieldStatus);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.BattlemasterJoinArena)]
        public void HandleBattlemasterJoinArena(BattlemasterJoinArena packet)
        {
            // ignore if we already in BG or BG queue
            if (GetPlayer().InBattleground())
                return;

            ArenaTypes arenatype = (ArenaTypes)ArenaTeam.GetTypeBySlot(packet.TeamSizeIndex);

            //check existence
            BattlegroundTemplate bgTemplate = Global.BattlegroundMgr.GetBattlegroundTemplateByTypeId(BattlegroundTypeId.AA);
            if (bgTemplate == null)
            {
                Log.outError(LogFilter.Network, "Battleground: template bg (all arenas) not found");
                return;
            }

            if (Global.DisableMgr.IsDisabledFor(DisableType.Battleground, (uint)BattlegroundTypeId.AA, null))
            {
                GetPlayer().SendSysMessage(CypherStrings.ArenaDisabled);
                return;
            }

            BattlegroundTypeId bgTypeId = bgTemplate.Id;
            BattlegroundQueueTypeId bgQueueTypeId = Global.BattlegroundMgr.BGQueueTypeId((ushort)bgTypeId, BattlegroundQueueIdType.Arena, true, arenatype);
            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel((uint)bgTemplate.MapIDs[0], _player.GetLevel());
            if (bracketEntry == null)
                return;

            Group group = GetPlayer().GetGroup();
            if (group == null)
            {
                group = new Group();
                group.Create(_player);
            }

            // no group found, error
            if (group == null)
                return;

            if (group.GetLeaderGUID() != GetPlayer().GetGUID())
                return;

            uint ateamId = GetPlayer().GetArenaTeamId(packet.TeamSizeIndex);
            // check real arenateam existence only here (if it was moved to group.CanJoin .. () then we would ahve to get it twice)
            ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById(ateamId);
            if (at == null)
                return;

            // get the team rating for queuing
            uint arenaRating = at.GetRating();
            uint matchmakerRating = at.GetAverageMMR(group);
            // the arenateam id must match for everyone in the group

            if (arenaRating <= 0)
                arenaRating = 1;

            BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);

            uint avgTime = 0;
            GroupQueueInfo ginfo = null;

            ObjectGuid errorGuid = ObjectGuid.Empty;
            GroupJoinBattlegroundResult err = GroupJoinBattlegroundResult.None;
            if (!Global.BattlegroundMgr.IsArenaTesting())
                err = group.CanJoinBattlegroundQueue(bgTemplate, bgQueueTypeId, (uint)arenatype, (uint)arenatype, true, packet.TeamSizeIndex, out errorGuid);
            if (err == 0)
            {
                Log.outDebug(LogFilter.Battleground, "Battleground: arena team id {0}, leader {1} queued with matchmaker rating {2} for type {3}", GetPlayer().GetArenaTeamId(packet.TeamSizeIndex), GetPlayer().GetName(), matchmakerRating, arenatype);

                ginfo = bgQueue.AddGroup(GetPlayer(), group, _player.GetTeam(), bracketEntry, false, arenaRating, matchmakerRating, ateamId);
                avgTime = bgQueue.GetAverageQueueWaitTime(ginfo, bracketEntry.GetBracketId());
            }

            foreach (GroupReference groupRef in group.GetMembers())
            {
                Player member = groupRef.GetSource();
                if (err != 0)
                {
                    BattlefieldStatusFailed battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatus, bgQueueTypeId, GetPlayer(), 0, err, errorGuid);
                    member.SendPacket(battlefieldStatus);
                    continue;
                }

                if (!GetPlayer().CanJoinToBattleground(bgTemplate))
                {
                    BattlefieldStatusFailed battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusFailed(out battlefieldStatus, bgQueueTypeId, GetPlayer(), 0, GroupJoinBattlegroundResult.BattlegroundJoinFailed, errorGuid);
                    member.SendPacket(battlefieldStatus);
                    return;
                }

                // add to queue
                uint queueSlot = member.AddBattlegroundQueueId(bgQueueTypeId);

                Global.BattlegroundMgr.BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatusQueued, member, queueSlot, ginfo.JoinTime, bgQueueTypeId, avgTime, true);
                member.SendPacket(battlefieldStatusQueued);

                Log.outDebug(LogFilter.Battleground, $"Battleground: player joined queue for arena as group bg queue {bgQueueTypeId}, {member.GetGUID()}, NAME {member.GetName()}");
            }

            Global.BattlegroundMgr.ScheduleQueueUpdate(matchmakerRating, bgQueueTypeId, bracketEntry.GetBracketId());
        }

        [WorldPacketHandler(ClientOpcodes.ReportPvpPlayerAfk)]
        void HandleReportPvPAFK(ReportPvPPlayerAFK reportPvPPlayerAFK)
        {
            Player reportedPlayer = Global.ObjAccessor.FindPlayer(reportPvPPlayerAFK.Offender);
            if (reportedPlayer == null)
            {
                Log.outDebug(LogFilter.Battleground, "WorldSession.HandleReportPvPAFK: player not found");
                return;
            }

            Log.outDebug(LogFilter.BattlegroundReportPvpAfk, "WorldSession.HandleReportPvPAFK:  {0} [IP: {1}] reported {2}", _player.GetName(), _player.GetSession().GetRemoteAddress(), reportedPlayer.GetGUID().ToString());

            reportedPlayer.ReportedAfkBy(GetPlayer());
        }

        [WorldPacketHandler(ClientOpcodes.RequestRatedPvpInfo)]
        void HandleRequestRatedPvpInfo(RequestRatedPvpInfo packet)
        {
            RatedPvpInfo ratedPvpInfo = new();
            SendPacket(ratedPvpInfo);
        }

        [WorldPacketHandler(ClientOpcodes.GetPvpOptionsEnabled, Processing = PacketProcessing.Inplace)]
        void HandleGetPVPOptionsEnabled(GetPVPOptionsEnabled packet)
        {
            // This packet is completely irrelevant, it triggers PVP_TYPES_ENABLED lua event but that is not handled in interface code as of 6.1.2
            PVPOptionsEnabled pvpOptionsEnabled = new();
            pvpOptionsEnabled.RatedBattlegrounds = false;
            pvpOptionsEnabled.PugBattlegrounds = true;
            pvpOptionsEnabled.WargameBattlegrounds = false;
            pvpOptionsEnabled.WargameArenas = false;
            pvpOptionsEnabled.RatedArenas = false;
            pvpOptionsEnabled.ArenaSkirmish = false;
            pvpOptionsEnabled.SoloShuffle = false;
            pvpOptionsEnabled.RatedSoloShuffle = false;
            pvpOptionsEnabled.BattlegroundBlitz = false;
            pvpOptionsEnabled.RatedBattlegroundBlitz = false;
            SendPacket(pvpOptionsEnabled);
        }

        [WorldPacketHandler(ClientOpcodes.RequestPvpRewards, Processing = PacketProcessing.Inplace)]
        void HandleRequestPvpReward(RequestPVPRewards packet)
        {
            GetPlayer().SendPvpRewards();
        }

        [WorldPacketHandler(ClientOpcodes.AreaSpiritHealerQuery)]
        void HandleAreaSpiritHealerQuery(AreaSpiritHealerQuery areaSpiritHealerQuery)
        {
            Player player = GetPlayer();
            Creature spiritHealer = ObjectAccessor.GetCreature(player, areaSpiritHealerQuery.HealerGuid);
            if (spiritHealer == null)
                return;

            if (!spiritHealer.IsAreaSpiritHealer())
                return;

            if (!_player.IsWithinDistInMap(spiritHealer, PlayerConst.MaxAreaSpiritHealerRange))
                return;

            if (spiritHealer.IsAreaSpiritHealerIndividual())
            {
                Aura aura = player.GetAura(BattlegroundConst.SpellSpiritHealPlayerAura);
                if (aura != null)
                {
                    player.SendAreaSpiritHealerTime(spiritHealer.GetGUID(), aura.GetDuration());
                }
                else
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(BattlegroundConst.SpellSpiritHealPlayerAura, Difficulty.None);
                    if (spellInfo != null)
                    {
                        spiritHealer.CastSpell(player, BattlegroundConst.SpellSpiritHealPlayerAura);
                        player.SendAreaSpiritHealerTime(spiritHealer.GetGUID(), spellInfo.GetDuration());
                        spiritHealer.CastSpell(null, BattlegroundConst.SpellSpiritHealChannelSelf);
                    }
                }
            }
            else
                _player.SendAreaSpiritHealerTime(spiritHealer);
        }

        [WorldPacketHandler(ClientOpcodes.AreaSpiritHealerQueue)]
        void HandleAreaSpiritHealerQueue(AreaSpiritHealerQueue areaSpiritHealerQueue)
        {
            Creature spiritHealer = ObjectAccessor.GetCreature(GetPlayer(), areaSpiritHealerQueue.HealerGuid);
            if (spiritHealer == null)
                return;

            if (!spiritHealer.IsAreaSpiritHealer())
                return;

            if (!_player.IsWithinDistInMap(spiritHealer, PlayerConst.MaxAreaSpiritHealerRange))
                return;

            _player.SetAreaSpiritHealer(spiritHealer);
        }

        [WorldPacketHandler(ClientOpcodes.HearthAndResurrect)]
        void HandleHearthAndResurrect(HearthAndResurrect packet)
        {
            if (GetPlayer().IsInFlight())
                return;

            BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(GetPlayer().GetMap(), GetPlayer().GetZoneId());
            if (bf != null)
            {
                bf.PlayerAskToLeave(_player);
                return;
            }

            AreaTableRecord atEntry = CliDB.AreaTableStorage.LookupByKey(GetPlayer().GetAreaId());
            if (atEntry == null || !atEntry.HasFlag(AreaFlags.AllowHearthAndRessurectFromArea))
                return;

            GetPlayer().BuildPlayerRepop();
            GetPlayer().ResurrectPlayer(1.0f);
            GetPlayer().TeleportTo(GetPlayer().GetHomebind());
        }
    }
}
