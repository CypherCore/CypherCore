// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Arenas;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.BattleGrounds
{
    public class BattlegroundQueue
    {
        public BattlegroundQueue(BattlegroundQueueTypeId queueId)
        {
            m_queueId = queueId;

            for (var i = 0; i < (int)BattlegroundBracketId.Max; ++i)
            {
                m_QueuedGroups[i] = new List<GroupQueueInfo>[BattlegroundConst.BgQueueTypesCount];

                for (var c = 0; c < BattlegroundConst.BgQueueTypesCount; ++c)
                    m_QueuedGroups[i][c] = new List<GroupQueueInfo>();
            }

            for (var i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                m_WaitTimes[i] = new uint[(int)BattlegroundBracketId.Max][];
                for (var c = 0; c < (int)BattlegroundBracketId.Max; ++c)
                    m_WaitTimes[i][c] = new uint[SharedConst.CountOfPlayersToAverageWaitTime];

                m_WaitTimeLastPlayer[i] = new uint[(int)BattlegroundBracketId.Max];
                m_SumOfWaitTimes[i] = new uint[(int)BattlegroundBracketId.Max];
            }

            m_SelectionPools[0] = new SelectionPool();
            m_SelectionPools[1] = new SelectionPool();
        }

        // add group or player (grp == null) to bg queue with the given leader and bg specifications
        public GroupQueueInfo AddGroup(Player leader, Group group, Team team, PvpDifficultyRecord bracketEntry, bool isPremade, uint ArenaRating, uint MatchmakerRating, uint arenateamid = 0)
        {
            BattlegroundBracketId bracketId = bracketEntry.GetBracketId();

            // create new ginfo
            GroupQueueInfo ginfo = new();
            ginfo.ArenaTeamId = arenateamid;
            ginfo.IsInvitedToBGInstanceGUID = 0;
            ginfo.JoinTime = GameTime.GetGameTimeMS();
            ginfo.RemoveInviteTime = 0;
            ginfo.Team = team;
            ginfo.ArenaTeamRating = ArenaRating;
            ginfo.ArenaMatchmakerRating = MatchmakerRating;
            ginfo.OpponentsTeamRating = 0;
            ginfo.OpponentsMatchmakerRating = 0;

            ginfo.Players.Clear();

            //compute index (if group is premade or joined a rated match) to queues
            uint index = 0;
            if (!m_queueId.Rated && !isPremade)
                index += SharedConst.PvpTeamsCount;
            if (ginfo.Team == Team.Horde)
                index++;
            Log.outDebug(LogFilter.Battleground, "Adding Group to BattlegroundQueue bgTypeId : {0}, bracket_id : {1}, index : {2}", m_queueId.BattlemasterListId, bracketId, index);

            uint lastOnlineTime = GameTime.GetGameTimeMS();

            //announce world (this don't need mutex)
            if (m_queueId.Rated && WorldConfig.GetBoolValue(WorldCfg.ArenaQueueAnnouncerEnable))
            {
                ArenaTeam arenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(arenateamid);
                if (arenaTeam != null)
                    Global.WorldMgr.SendWorldText( CypherStrings.ArenaQueueAnnounceWorldJoin, arenaTeam.GetName(), m_queueId.TeamSize, m_queueId.TeamSize, ginfo.ArenaTeamRating);
            }

            //add players from group to ginfo
            if (group != null)
            {
                foreach (GroupReference groupRef in group.GetMembers())
                {
                    Player member = groupRef.GetSource();
                    PlayerQueueInfo pl_info = new();
                    pl_info.LastOnlineTime = lastOnlineTime;
                    pl_info.GroupInfo = ginfo;

                    m_QueuedPlayers[member.GetGUID()] = pl_info;
                    // add the pinfo to ginfo's list
                    ginfo.Players[member.GetGUID()] = pl_info;
                }
            }
            else
            {
                PlayerQueueInfo pl_info = new();
                pl_info.LastOnlineTime = lastOnlineTime;
                pl_info.GroupInfo = ginfo;

                m_QueuedPlayers[leader.GetGUID()] = pl_info;
                ginfo.Players[leader.GetGUID()] = pl_info;
            }

            //add GroupInfo to m_QueuedGroups
            {
                //ACE_Guard<ACE_Recursive_Thread_Mutex> guard(m_Lock);
                m_QueuedGroups[(int)bracketId][index].Add(ginfo);

                //announce to world, this code needs mutex
                if (!m_queueId.Rated && !isPremade && WorldConfig.GetBoolValue(WorldCfg.BattlegroundQueueAnnouncerEnable))
                {
                    BattlegroundTemplate bg = Global.BattlegroundMgr.GetBattlegroundTemplateByTypeId((BattlegroundTypeId)m_queueId.BattlemasterListId);
                    if (bg != null)
                    {
                        uint MinPlayers = bg.GetMinPlayersPerTeam();
                        uint qHorde = 0;
                        uint qAlliance = 0;
                        uint q_min_level = bracketEntry.MinLevel;
                        uint q_max_level = bracketEntry.MaxLevel;

                        foreach (var groupQueueInfo in m_QueuedGroups[(int)bracketId][BattlegroundConst.BgQueueNormalAlliance])
                            if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0)
                                qAlliance += (uint)groupQueueInfo.Players.Count;
                        foreach (var groupQueueInfo in m_QueuedGroups[(int)bracketId][BattlegroundConst.BgQueueNormalHorde])
                            if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0)
                                qHorde += (uint)groupQueueInfo.Players.Count;

                        // Show queue status to player only (when joining queue)
                        if (WorldConfig.GetBoolValue(WorldCfg.BattlegroundQueueAnnouncerPlayeronly))
                        {
                            leader.SendSysMessage(CypherStrings.BgQueueAnnounceSelf, bg.BattlemasterEntry.Name[Global.WorldMgr.GetDefaultDbcLocale()], q_min_level, q_max_level,
                                qAlliance, (MinPlayers > qAlliance) ? MinPlayers - qAlliance : 0, qHorde, (MinPlayers > qHorde) ? MinPlayers - qHorde : 0);
                        }
                        // System message
                        else
                        {
                            Global.WorldMgr.SendWorldText(CypherStrings.BgQueueAnnounceWorld, bg.BattlemasterEntry.Name[Global.WorldMgr.GetDefaultDbcLocale()], q_min_level, q_max_level,
                                qAlliance, (MinPlayers > qAlliance) ? MinPlayers - qAlliance : 0, qHorde, (MinPlayers > qHorde) ? MinPlayers - qHorde : 0);
                        }
                    }
                }
            }

            return ginfo;
        }

        void PlayerInvitedToBGUpdateAverageWaitTime(GroupQueueInfo ginfo, BattlegroundBracketId bracket_id)
        {
            uint timeInQueue = Time.GetMSTimeDiff(ginfo.JoinTime, GameTime.GetGameTimeMS());
            uint team_index = BattleGroundTeamId.Alliance;                    //default set to TeamIndex.Alliance - or non rated arenas!
            if (m_queueId.TeamSize == 0)
            {
                if (ginfo.Team == Team.Horde)
                    team_index = BattleGroundTeamId.Horde;
            }
            else
            {
                if (m_queueId.Rated)
                    team_index = BattleGroundTeamId.Horde;                     //for rated arenas use TeamIndex.Horde
            }

            //store pointer to arrayindex of player that was added first
            uint lastPlayerAddedPointer = m_WaitTimeLastPlayer[team_index][(int)bracket_id];
            //remove his time from sum
            m_SumOfWaitTimes[team_index][(int)bracket_id] -= m_WaitTimes[team_index][(int)bracket_id][lastPlayerAddedPointer];
            //set average time to new
            m_WaitTimes[team_index][(int)bracket_id][lastPlayerAddedPointer] = timeInQueue;
            //add new time to sum
            m_SumOfWaitTimes[team_index][(int)bracket_id] += timeInQueue;
            //set index of last player added to next one
            lastPlayerAddedPointer++;
            m_WaitTimeLastPlayer[team_index][(int)bracket_id] = lastPlayerAddedPointer % SharedConst.CountOfPlayersToAverageWaitTime;
        }

        public uint GetAverageQueueWaitTime(GroupQueueInfo ginfo, BattlegroundBracketId bracket_id)
        {
            uint team_index = BattleGroundTeamId.Alliance;                    //default set to TeamIndex.Alliance - or non rated arenas!
            if (m_queueId.TeamSize == 0)
            {
                if (ginfo.Team == Team.Horde)
                    team_index = BattleGroundTeamId.Horde;
            }
            else
            {
                if (m_queueId.Rated)
                    team_index = BattleGroundTeamId.Horde;                     //for rated arenas use TeamIndex.Horde
            }
            //check if there is enought values(we always add values > 0)
            if (m_WaitTimes[team_index][(int)bracket_id][SharedConst.CountOfPlayersToAverageWaitTime - 1] != 0)
                return (m_SumOfWaitTimes[team_index][(int)bracket_id] / SharedConst.CountOfPlayersToAverageWaitTime);
            else
                //if there aren't enough values return 0 - not available
                return 0;
        }

        //remove player from queue and from group info, if group info is empty then remove it too
        public void RemovePlayer(ObjectGuid guid, bool decreaseInvitedCount)
        {
            int bracket_id = -1;                                     // signed for proper for-loop finish

            //remove player from map, if he's there
            var playerQueueInfo = m_QueuedPlayers.LookupByKey(guid);
            if (playerQueueInfo == null)
            {
                string playerName = "Unknown";
                Player player = Global.ObjAccessor.FindPlayer(guid);
                if (player != null)
                    playerName = player.GetName();
                Log.outDebug(LogFilter.Battleground, "BattlegroundQueue: couldn't find player {0} ({1})", playerName, guid.ToString());
                return;
            }

            GroupQueueInfo group = playerQueueInfo.GroupInfo;
            GroupQueueInfo groupQueseInfo = null;
            // mostly people with the highest levels are in Battlegrounds, thats why
            // we count from MAX_Battleground_QUEUES - 1 to 0

            uint index = (group.Team == Team.Horde) ? BattlegroundConst.BgQueuePremadeHorde : BattlegroundConst.BgQueuePremadeAlliance;

            for (int bracket_id_tmp = (int)BattlegroundBracketId.Max - 1; bracket_id_tmp >= 0 && bracket_id == -1; --bracket_id_tmp)
            {
                //we must check premade and normal team's queue - because when players from premade are joining bg,
                //they leave groupinfo so we can't use its players size to find out index
                for (uint j = index; j < BattlegroundConst.BgQueueTypesCount; j += SharedConst.PvpTeamsCount)
                {
                    foreach (var k in m_QueuedGroups[bracket_id_tmp][j])
                    {
                        if (k == group)
                        {
                            bracket_id = bracket_id_tmp;
                            groupQueseInfo = k;
                            //we must store index to be able to erase iterator
                            index = j;
                            break;
                        }
                    }
                }
            }

            //player can't be in queue without group, but just in case
            if (bracket_id == -1)
            {
                Log.outError(LogFilter.Battleground, "BattlegroundQueue: ERROR Cannot find groupinfo for {0}", guid.ToString());
                return;
            }
            Log.outDebug(LogFilter.Battleground, "BattlegroundQueue: Removing {0}, from bracket_id {1}", guid.ToString(), bracket_id);

            // ALL variables are correctly set
            // We can ignore leveling up in queue - it should not cause crash
            // remove player from group
            // if only one player there, remove group

            // remove player queue info from group queue info
            if (group.Players.ContainsKey(guid))
                group.Players.Remove(guid);

            // if invited to bg, and should decrease invited count, then do it
            if (decreaseInvitedCount && group.IsInvitedToBGInstanceGUID != 0)
            {
                Battleground bg = Global.BattlegroundMgr.GetBattleground(group.IsInvitedToBGInstanceGUID, (BattlegroundTypeId)m_queueId.BattlemasterListId);
                if (bg != null)
                    bg.DecreaseInvitedCount(group.Team);
            }

            // remove player queue info
            m_QueuedPlayers.Remove(guid);

            // announce to world if arena team left queue for rated match, show only once
            if (m_queueId.TeamSize != 0 && m_queueId.Rated && group.Players.Empty() && WorldConfig.GetBoolValue(WorldCfg.ArenaQueueAnnouncerEnable))
            {
                ArenaTeam team = Global.ArenaTeamMgr.GetArenaTeamById(group.ArenaTeamId);
                if (team != null)
                    Global.WorldMgr.SendWorldText( CypherStrings.ArenaQueueAnnounceWorldExit, team.GetName(), m_queueId.TeamSize, m_queueId.TeamSize, group.ArenaTeamRating);
            }

            // if player leaves queue and he is invited to rated arena match, then he have to lose
            if (group.IsInvitedToBGInstanceGUID != 0 && m_queueId.Rated && decreaseInvitedCount)
            {
                ArenaTeam at = Global.ArenaTeamMgr.GetArenaTeamById(group.ArenaTeamId);
                if (at != null)
                {
                    Log.outDebug(LogFilter.Battleground, "UPDATING memberLost's personal arena rating for {0} by opponents rating: {1}", guid.ToString(), group.OpponentsTeamRating);
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                        at.MemberLost(player, group.OpponentsMatchmakerRating);
                    else
                        at.OfflineMemberLost(guid, group.OpponentsMatchmakerRating);
                    at.SaveToDB();
                }
            }

            // remove group queue info if needed
            if (group.Players.Empty())
            {
                m_QueuedGroups[bracket_id][index].Remove(groupQueseInfo);
                return;
            }

            // if group wasn't empty, so it wasn't deleted, and player have left a rated
            // queue . everyone from the group should leave too
            // don't remove recursively if already invited to bg!
            if (group.IsInvitedToBGInstanceGUID == 0 && m_queueId.Rated)
            {
                // remove next player, this is recursive
                // first send removal information
                Player plr2 = Global.ObjAccessor.FindConnectedPlayer(group.Players.FirstOrDefault().Key);
                if (plr2 != null)
                {
                    uint queueSlot = plr2.GetBattlegroundQueueIndex(m_queueId);

                    plr2.RemoveBattlegroundQueueId(m_queueId); // must be called this way, because if you move this call to
                                                                   // queue.removeplayer, it causes bugs

                    BattlefieldStatusNone battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusNone(out battlefieldStatus, plr2, queueSlot, plr2.GetBattlegroundQueueJoinTime(m_queueId));
                    plr2.SendPacket(battlefieldStatus);
                }
                // then actually delete, this may delete the group as well!
                RemovePlayer(group.Players.First().Key, decreaseInvitedCount);
            }
        }

        //returns true when player pl_guid is in queue and is invited to bgInstanceGuid
        public bool IsPlayerInvited(ObjectGuid pl_guid, uint bgInstanceGuid, uint removeTime)
        {
            var queueInfo = m_QueuedPlayers.LookupByKey(pl_guid);
            return (queueInfo != null
                && queueInfo.GroupInfo.IsInvitedToBGInstanceGUID == bgInstanceGuid
                && queueInfo.GroupInfo.RemoveInviteTime == removeTime);
        }

        public bool GetPlayerGroupInfoData(ObjectGuid guid, out GroupQueueInfo ginfo)
        {
            ginfo = null;
            var playerQueueInfo = m_QueuedPlayers.LookupByKey(guid);
            if (playerQueueInfo == null)
                return false;

            ginfo = playerQueueInfo.GroupInfo;
            return true;
        }

        uint GetPlayersInQueue(uint id)
        {
            return m_SelectionPools[id].GetPlayerCount();
        }

        bool InviteGroupToBG(GroupQueueInfo ginfo, Battleground bg, Team side)
        {
            // set side if needed
            if (side != 0)
                ginfo.Team = side;

            if (ginfo.IsInvitedToBGInstanceGUID == 0)
            {
                // not yet invited
                // set invitation
                ginfo.IsInvitedToBGInstanceGUID = bg.GetInstanceID();
                BattlegroundTypeId bgTypeId = (BattlegroundTypeId)m_queueId.BattlemasterListId;
                BattlegroundQueueTypeId bgQueueTypeId = m_queueId;
                BattlegroundBracketId bracket_id = bg.GetBracketId();

                // set ArenaTeamId for rated matches
                if (bg.IsArena() && bg.IsRated())
                    bg.SetArenaTeamIdForTeam(ginfo.Team, ginfo.ArenaTeamId);

                ginfo.RemoveInviteTime = GameTime.GetGameTimeMS() + BattlegroundConst.InviteAcceptWaitTime;

                // loop through the players
                foreach (var guid in ginfo.Players.Keys)
                {
                    // get the player
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    // if offline, skip him, this should not happen - player is removed from queue when he logs out
                    if (player == null)
                        continue;

                    // invite the player
                    PlayerInvitedToBGUpdateAverageWaitTime(ginfo, bracket_id);

                    // set invited player counters
                    bg.IncreaseInvitedCount(ginfo.Team);

                    player.SetInviteForBattlegroundQueueType(bgQueueTypeId, ginfo.IsInvitedToBGInstanceGUID);

                    // create remind invite events
                    BGQueueInviteEvent inviteEvent = new(player.GetGUID(), ginfo.IsInvitedToBGInstanceGUID, bgTypeId, ginfo.RemoveInviteTime, m_queueId);
                    m_events.AddEvent(inviteEvent, m_events.CalculateTime(TimeSpan.FromMilliseconds(BattlegroundConst.InvitationRemindTime)));
                    // create automatic remove events
                    BGQueueRemoveEvent removeEvent = new(player.GetGUID(), ginfo.IsInvitedToBGInstanceGUID, bgQueueTypeId, ginfo.RemoveInviteTime);
                    m_events.AddEvent(removeEvent, m_events.CalculateTime(TimeSpan.FromMilliseconds(BattlegroundConst.InviteAcceptWaitTime)));

                    uint queueSlot = player.GetBattlegroundQueueIndex(bgQueueTypeId);

                    Log.outDebug(LogFilter.Battleground, "Battleground: invited player {0} ({1}) to BG instance {2} queueindex {3} bgtype {4}",
                         player.GetName(), player.GetGUID().ToString(), bg.GetInstanceID(), queueSlot, m_queueId.BattlemasterListId);

                    BattlefieldStatusNeedConfirmation battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusNeedConfirmation(out battlefieldStatus, bg, player, queueSlot, player.GetBattlegroundQueueJoinTime(bgQueueTypeId), BattlegroundConst.InviteAcceptWaitTime, bgQueueTypeId);
                    player.SendPacket(battlefieldStatus);
                }
                return true;
            }

            return false;
        }

        /*
        This function is inviting players to already running Battlegrounds
        Invitation type is based on config file
        large groups are disadvantageous, because they will be kicked first if invitation type = 1
        */
        void FillPlayersToBG(Battleground bg, BattlegroundBracketId bracket_id)
        {
            uint hordeFree = bg.GetFreeSlotsForTeam(Team.Horde);
            uint aliFree = bg.GetFreeSlotsForTeam(Team.Alliance);
            int aliCount = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance].Count;
            int hordeCount = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalHorde].Count;

            // try to get even teams
            if (WorldConfig.GetIntValue(WorldCfg.BattlegroundInvitationType) == (int)BattlegroundQueueInvitationType.Even)
            {
                // check if the teams are even
                if (hordeFree == 1 && aliFree == 1)
                {
                    // if we are here, the teams have the same amount of players
                    // then we have to allow to join the same amount of players
                    int hordeExtra = hordeCount - aliCount;
                    int aliExtra = aliCount - hordeCount;

                    hordeExtra = Math.Max(hordeExtra, 0);
                    aliExtra = Math.Max(aliExtra, 0);

                    if (aliCount != hordeCount)
                    {
                        aliFree -= (uint)aliExtra;
                        hordeFree -= (uint)hordeExtra;

                        aliFree = Math.Max(aliFree, 0);
                        hordeFree = Math.Max(hordeFree, 0);
                    }
                }
            }

            //count of groups in queue - used to stop cycles 
            int alyIndex = 0;
            {
                int listIndex = 0;
                var info = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance].FirstOrDefault();
                for (; alyIndex < aliCount && m_SelectionPools[BattleGroundTeamId.Alliance].AddGroup(info, aliFree); alyIndex++)
                    info = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance][listIndex++];
            }

            //the same thing for horde
            int hordeIndex = 0;
            {
                int listIndex = 0;
                var info = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalHorde].FirstOrDefault();
                for (; hordeIndex < hordeCount && m_SelectionPools[BattleGroundTeamId.Horde].AddGroup(info, hordeFree); hordeIndex++)
                    info = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalHorde][listIndex++];
            }

            //if ofc like BG queue invitation is set in config, then we are happy
            if (WorldConfig.GetIntValue(WorldCfg.BattlegroundInvitationType) == (int)BattlegroundQueueInvitationType.NoBalance)
                return;
            /*
            if we reached this code, then we have to solve NP - complete problem called Subset sum problem
            So one solution is to check all possible invitation subgroups, or we can use these conditions:
            1. Last time when BattlegroundQueue.Update was executed we invited all possible players - so there is only small possibility
                that we will invite now whole queue, because only 1 change has been made to queues from the last BattlegroundQueue.Update call
            2. Other thing we should consider is group order in queue
            */

            // At first we need to compare free space in bg and our selection pool
            int diffAli = (int)(aliFree - m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount());
            int diffHorde = (int)(hordeFree - m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount());
            while (Math.Abs(diffAli - diffHorde) > 1 && (m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() > 0 || m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount() > 0))
            {
                //each cycle execution we need to kick at least 1 group
                if (diffAli < diffHorde)
                {
                    //kick alliance group, add to pool new group if needed
                    if (m_SelectionPools[BattleGroundTeamId.Alliance].KickGroup((uint)(diffHorde - diffAli)))
                    {
                        for (; alyIndex < aliCount && m_SelectionPools[BattleGroundTeamId.Alliance].AddGroup(m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance][alyIndex], (uint)((aliFree >= diffHorde) ? aliFree - diffHorde : 0)); alyIndex++)
                            ++alyIndex;
                    }
                    //if ali selection is already empty, then kick horde group, but if there are less horde than ali in bg - break;
                    if (m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount() == 0)
                    {
                        if (aliFree <= diffHorde + 1)
                            break;
                        m_SelectionPools[BattleGroundTeamId.Horde].KickGroup((uint)(diffHorde - diffAli));
                    }
                }
                else
                {
                    //kick horde group, add to pool new group if needed
                    if (m_SelectionPools[BattleGroundTeamId.Horde].KickGroup((uint)(diffAli - diffHorde)))
                    {
                        for (; hordeIndex < hordeCount && m_SelectionPools[BattleGroundTeamId.Horde].AddGroup(m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalHorde][hordeIndex], (uint)((hordeFree >= diffAli) ? hordeFree - diffAli : 0)); hordeIndex++)
                            ++hordeIndex;
                    }
                    if (m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() == 0)
                    {
                        if (hordeFree <= diffAli + 1)
                            break;
                        m_SelectionPools[BattleGroundTeamId.Alliance].KickGroup((uint)(diffAli - diffHorde));
                    }
                }
                //count diffs after small update
                diffAli = (int)(aliFree - m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount());
                diffHorde = (int)(hordeFree - m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount());
            }
        }

        // this method checks if premade versus premade Battleground is possible
        // then after 30 mins (default) in queue it moves premade group to normal queue
        // it tries to invite as much players as it can - to MaxPlayersPerTeam, because premade groups have more than MinPlayersPerTeam players
        bool CheckPremadeMatch(BattlegroundBracketId bracket_id, uint MinPlayersPerTeam, uint MaxPlayersPerTeam)
        {
            //check match
            if (!m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance].Empty() && !m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeHorde].Empty())
            {
                //start premade match
                //if groups aren't invited
                GroupQueueInfo ali_group = null;
                GroupQueueInfo horde_group = null;
                foreach (var groupQueueInfo in m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance])
                {
                    ali_group = groupQueueInfo;
                    if (ali_group.IsInvitedToBGInstanceGUID == 0)
                        break;
                }
                foreach (var groupQueueInfo in m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeHorde])
                {
                    horde_group = groupQueueInfo;
                    if (horde_group.IsInvitedToBGInstanceGUID == 0)
                        break;
                }

                if (ali_group != null && horde_group != null)
                {
                    m_SelectionPools[BattleGroundTeamId.Alliance].AddGroup(ali_group, MaxPlayersPerTeam);
                    m_SelectionPools[BattleGroundTeamId.Horde].AddGroup(horde_group, MaxPlayersPerTeam);
                    //add groups/players from normal queue to size of bigger group
                    uint maxPlayers = Math.Min(m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount(), m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount());
                    for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
                    {
                        foreach (var groupQueueInfo in m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + i])
                        {
                            //if groupQueueInfo can join BG and player count is less that maxPlayers, then add group to selectionpool
                            if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0 && !m_SelectionPools[i].AddGroup(groupQueueInfo, maxPlayers))
                                break;
                        }
                    }
                    //premade selection pools are set
                    return true;
                }
            }
            // now check if we can move group from Premade queue to normal queue (timer has expired) or group size lowered!!
            // this could be 2 cycles but i'm checking only first team in queue - it can cause problem -
            // if first is invited to BG and seconds timer expired, but we can ignore it, because players have only 80 seconds to click to enter bg
            // and when they click or after 80 seconds the queue info is removed from queue
            uint time_before = (uint)(GameTime.GetGameTimeMS() - WorldConfig.GetIntValue(WorldCfg.BattlegroundPremadeGroupWaitForMatch));
            for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
            {
                if (!m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance + i].Empty())
                {
                    var groupQueueInfo = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance + i].First();
                    if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0 && (groupQueueInfo.JoinTime < time_before || groupQueueInfo.Players.Count < MinPlayersPerTeam))
                    {
                        //we must insert group to normal queue and erase pointer from premade queue
                        m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + i].Insert(0, groupQueueInfo);
                        m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance + i].Remove(groupQueueInfo);
                    }
                }
            }
            //selection pools are not set
            return false;
        }

        // this method tries to create Battleground or arena with MinPlayersPerTeam against MinPlayersPerTeam
        bool CheckNormalMatch(BattlegroundBracketId bracketId, uint minPlayers, uint maxPlayers)
        {
            int[] teamIndex = new int[SharedConst.PvpTeamsCount];
            for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
            {
                teamIndex[i] = 0;
                for (; teamIndex[i] != m_QueuedGroups[(int)bracketId][BattlegroundConst.BgQueueNormalAlliance + i].Count; ++teamIndex[i])
                {
                    var groupQueueInfo = m_QueuedGroups[(int)bracketId][BattlegroundConst.BgQueueNormalAlliance + i][teamIndex[i]];
                    if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0)
                    {
                        m_SelectionPools[i].AddGroup(groupQueueInfo, maxPlayers);
                        if (m_SelectionPools[i].GetPlayerCount() >= minPlayers)
                            break;
                    }
                }
            }
            //try to invite same number of players - this cycle may cause longer wait time even if there are enough players in queue, but we want ballanced bg
            uint j = BattleGroundTeamId.Alliance;
            if (m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() < m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount())
                j = BattleGroundTeamId.Horde;

            if (WorldConfig.GetIntValue(WorldCfg.BattlegroundInvitationType) != (int)BattlegroundQueueInvitationType.NoBalance
                && m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() >= minPlayers && m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount() >= minPlayers)
            {
                //we will try to invite more groups to team with less players indexed by j
                ++(teamIndex[j]);                                         //this will not cause a crash, because for cycle above reached break;
                for (; teamIndex[j] != m_QueuedGroups[(int)bracketId][BattlegroundConst.BgQueueNormalAlliance + j].Count; ++teamIndex[j])
                {
                    var groupQueueInfo = m_QueuedGroups[(int)bracketId][BattlegroundConst.BgQueueNormalAlliance + j][teamIndex[j]];
                    if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0)
                        if (!m_SelectionPools[j].AddGroup(groupQueueInfo, m_SelectionPools[(j + 1) % SharedConst.PvpTeamsCount].GetPlayerCount()))
                            break;
                }
                // do not allow to start bg with more than 2 players more on 1 faction
                if (Math.Abs((m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() - m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount())) > 2)
                    return false;
            }
            //allow 1v0 if debug bg
            if (Global.BattlegroundMgr.IsTesting() && (m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount() != 0 || m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() != 0))
                return true;
            //return true if there are enough players in selection pools - enable to work .debug bg command correctly
            return m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount() >= minPlayers && m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() >= minPlayers;
        }

        // this method will check if we can invite players to same faction skirmish match
        bool CheckSkirmishForSameFaction(BattlegroundBracketId bracket_id, uint minPlayersPerTeam)
        {
            if (m_SelectionPools[BattleGroundTeamId.Alliance].GetPlayerCount() < minPlayersPerTeam && m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() < minPlayersPerTeam)
                return false;

            uint teamIndex = BattleGroundTeamId.Alliance;
            uint otherTeam = BattleGroundTeamId.Horde;
            Team otherTeamId = Team.Horde;
            if (m_SelectionPools[BattleGroundTeamId.Horde].GetPlayerCount() == minPlayersPerTeam)
            {
                teamIndex = BattleGroundTeamId.Horde;
                otherTeam = BattleGroundTeamId.Alliance;
                otherTeamId = Team.Alliance;
            }
            //clear other team's selection
            m_SelectionPools[otherTeam].Init();
            //store last ginfo pointer
            GroupQueueInfo ginfo = m_SelectionPools[teamIndex].SelectedGroups.Last();
            //set itr_team to group that was added to selection pool latest
            int team = 0;
            foreach (var groupQueueInfo in m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + teamIndex])
                if (ginfo == groupQueueInfo)
                    break;

            if (team == m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + teamIndex].Count - 1)
                return false;

            var team2 = team;
            ++team2;
            //invite players to other selection pool
            for (; team2 != m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + teamIndex].Count - 1; ++team2)
            {
                var groupQueueInfo = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + teamIndex][team2];
                //if selection pool is full then break;
                if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0 && !m_SelectionPools[otherTeam].AddGroup(groupQueueInfo, minPlayersPerTeam))
                    break;
            }
            if (m_SelectionPools[otherTeam].GetPlayerCount() != minPlayersPerTeam)
                return false;

            //here we have correct 2 selections and we need to change one teams team and move selection pool teams to other team's queue
            foreach (var groupQueueInfo in m_SelectionPools[otherTeam].SelectedGroups)
            {
                //set correct team
                groupQueueInfo.Team = otherTeamId;
                //add team to other queue
                m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + otherTeam].Insert(0, groupQueueInfo);
                //remove team from old queue
                var team3 = team;
                ++team3;
                for (; team3 != m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + teamIndex].Count - 1; ++team3)
                {
                    var groupQueueInfo1 = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + teamIndex][team3];
                    if (groupQueueInfo1 == groupQueueInfo)
                    {
                        m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance + teamIndex].Remove(groupQueueInfo1);
                        break;
                    }
                }
            }
            return true;
        }

        public void UpdateEvents(uint diff)
        {
            m_events.Update(diff);
        }

        /// <summary>
        /// this method is called when group is inserted, or player / group is removed from BG Queue - there is only one player's status changed, so we don't use while (true) cycles to invite whole queue
        /// it must be called after fully adding the members of a group to ensure group joining
        /// should be called from Battleground.RemovePlayer function in some cases
        /// </summary>
        /// <param name="diff"></param>
        /// <param name="bgTypeId"></param>
        /// <param name="bracket_id"></param>
        /// <param name="arenaType"></param>
        /// <param name="isRated"></param>
        /// <param name="arenaRating"></param>
        public void BattlegroundQueueUpdate(uint diff, BattlegroundBracketId bracket_id, uint arenaRating)
        {
            //if no players in queue - do nothing
            if (m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance].Empty() &&
                m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeHorde].Empty() &&
                m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalAlliance].Empty() &&
                m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueueNormalHorde].Empty())
                return;

            // Battleground with free slot for player should be always in the beggining of the queue
            // maybe it would be better to create bgfreeslotqueue for each bracket_id
            BattlegroundTemplate bg_template = Global.BattlegroundMgr.GetBattlegroundTemplateByTypeId((BattlegroundTypeId)m_queueId.BattlemasterListId);
            if (bg_template == null)
            {
                Log.outError(LogFilter.Battleground, $"Battleground: Update: bg template not found for {m_queueId.BattlemasterListId}");
                return;
            }

            // loop over queues for every map
            foreach (var mapId in bg_template.MapIDs)
            {
                var bgQueues = Global.BattlegroundMgr.GetBGFreeSlotQueueStore((uint)mapId);
                foreach (var bg in bgQueues)
                {
                    // DO NOT allow queue manager to invite new player to rated games
                    if (!bg.IsRated() && bg.GetBracketId() == bracket_id &&
                        bg.GetStatus() > BattlegroundStatus.WaitQueue && bg.GetStatus() < BattlegroundStatus.WaitLeave)
                    {
                        // clear selection pools
                        m_SelectionPools[BattleGroundTeamId.Alliance].Init();
                        m_SelectionPools[BattleGroundTeamId.Horde].Init();

                        // call a function that does the job for us
                        FillPlayersToBG(bg, bracket_id);

                        // now everything is set, invite players
                        foreach (var queueInfo in m_SelectionPools[BattleGroundTeamId.Alliance].SelectedGroups)
                            InviteGroupToBG(queueInfo, bg, queueInfo.Team);

                        foreach (var queueInfo in m_SelectionPools[BattleGroundTeamId.Horde].SelectedGroups)
                            InviteGroupToBG(queueInfo, bg, queueInfo.Team);

                        if (!bg.HasFreeSlots())
                            bg.RemoveFromBGFreeSlotQueue();
                    }
                }
            }

            // finished iterating through the bgs with free slots, maybe we need to create a new bg

            // get the min. players per team, properly for larger arenas as well. (must have full teams for arena matches!)
            uint MinPlayersPerTeam = bg_template.GetMinPlayersPerTeam();
            uint MaxPlayersPerTeam = bg_template.GetMaxPlayersPerTeam();

            if (bg_template.IsArena())
            {
                MaxPlayersPerTeam = m_queueId.TeamSize;
                MinPlayersPerTeam = Global.BattlegroundMgr.IsArenaTesting() ? 1u : m_queueId.TeamSize;
            }
            else if (Global.BattlegroundMgr.IsTesting())
                MinPlayersPerTeam = 1;

            m_SelectionPools[BattleGroundTeamId.Alliance].Init();
            m_SelectionPools[BattleGroundTeamId.Horde].Init();

            if (!bg_template.IsArena())
            {
                if (CheckPremadeMatch(bracket_id, MinPlayersPerTeam, MaxPlayersPerTeam))
                {
                    // create new Battleground
                    Battleground bg2 = Global.BattlegroundMgr.CreateNewBattleground(m_queueId, bracket_id);
                    if (bg2 == null)
                    {
                        Log.outError(LogFilter.Battleground, $"BattlegroundQueue.Update - Cannot create Battleground: {m_queueId.BattlemasterListId}");
                        return;
                    }
                    // invite those selection pools
                    for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
                        foreach (var queueInfo in m_SelectionPools[BattleGroundTeamId.Alliance + i].SelectedGroups)
                            InviteGroupToBG(queueInfo, bg2, queueInfo.Team);

                    bg2.StartBattleground();
                    //clear structures
                    m_SelectionPools[BattleGroundTeamId.Alliance].Init();
                    m_SelectionPools[BattleGroundTeamId.Horde].Init();
                }
            }

            // now check if there are in queues enough players to start new game of (normal Battleground, or non-rated arena)
            if (!m_queueId.Rated)
            {
                // if there are enough players in pools, start new Battleground or non rated arena
                if (CheckNormalMatch(bracket_id, MinPlayersPerTeam, MaxPlayersPerTeam)
                    || (bg_template.IsArena() && CheckSkirmishForSameFaction(bracket_id, MinPlayersPerTeam)))
                {
                    // we successfully created a pool
                    Battleground bg2 = Global.BattlegroundMgr.CreateNewBattleground(m_queueId, bracket_id);
                    if (bg2 == null)
                    {
                        Log.outError(LogFilter.Battleground, $"BattlegroundQueue.Update - Cannot create Battleground: {m_queueId.BattlemasterListId}");
                        return;
                    }

                    // invite those selection pools
                    for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
                    {
                        foreach (var queueInfo in m_SelectionPools[BattleGroundTeamId.Alliance + i].SelectedGroups)
                            InviteGroupToBG(queueInfo, bg2, queueInfo.Team);
                    }
                    // start bg
                    bg2.StartBattleground();
                }
            }
            else if (bg_template.IsArena())
            {
                // found out the minimum and maximum ratings the newly added team should battle against
                // arenaRating is the rating of the latest joined team, or 0
                // 0 is on (automatic update call) and we must set it to team's with longest wait time
                if (arenaRating == 0)
                {
                    GroupQueueInfo front1 = null;
                    GroupQueueInfo front2 = null;
                    if (!m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance].Empty())
                    {
                        front1 = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance].First();
                        arenaRating = front1.ArenaMatchmakerRating;
                    }
                    if (!m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeHorde].Empty())
                    {
                        front2 = m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeHorde].First();
                        arenaRating = front2.ArenaMatchmakerRating;
                    }
                    if (front1 != null && front2 != null)
                    {
                        if (front1.JoinTime < front2.JoinTime)
                            arenaRating = front1.ArenaMatchmakerRating;
                    }
                    else if (front1 == null && front2 == null)
                        return; //queues are empty
                }

                //set rating range
                uint arenaMinRating = (arenaRating <= Global.BattlegroundMgr.GetMaxRatingDifference()) ? 0 : arenaRating - Global.BattlegroundMgr.GetMaxRatingDifference();
                uint arenaMaxRating = arenaRating + Global.BattlegroundMgr.GetMaxRatingDifference();
                // if max rating difference is set and the time past since server startup is greater than the rating discard time
                // (after what time the ratings aren't taken into account when making teams) then
                // the discard time is current_time - time_to_discard, teams that joined after that, will have their ratings taken into account
                // else leave the discard time on 0, this way all ratings will be discarded
                int discardTime = (int)(GameTime.GetGameTimeMS() - Global.BattlegroundMgr.GetRatingDiscardTimer());

                // we need to find 2 teams which will play next game
                GroupQueueInfo[] queueArray = new GroupQueueInfo[SharedConst.PvpTeamsCount];
                byte found = 0;
                byte team = 0;

                for (byte i = (byte)BattlegroundConst.BgQueuePremadeAlliance; i < BattlegroundConst.BgQueueNormalAlliance; i++)
                {
                    // take the group that joined first
                    foreach (var queueInfo in m_QueuedGroups[(int)bracket_id][i])
                    {
                        // if group match conditions, then add it to pool
                        if (queueInfo.IsInvitedToBGInstanceGUID == 0
                            && ((queueInfo.ArenaMatchmakerRating >= arenaMinRating && queueInfo.ArenaMatchmakerRating <= arenaMaxRating)
                                || queueInfo.JoinTime < discardTime))
                        {
                            queueArray[found++] = queueInfo;
                            team = i;
                            break;
                        }
                    }
                }

                if (found == 0)
                    return;

                if (found == 1)
                {
                    foreach (var queueInfo in m_QueuedGroups[(int)bracket_id][team])
                    {
                        if (queueInfo.IsInvitedToBGInstanceGUID == 0
                            && ((queueInfo.ArenaMatchmakerRating >= arenaMinRating && queueInfo.ArenaMatchmakerRating <= arenaMaxRating)
                                || queueInfo.JoinTime < discardTime)
                            && queueArray[0].ArenaTeamId != queueInfo.ArenaTeamId)
                        {
                            queueArray[found++] = queueInfo;
                            break;
                        }
                    }
                }

                //if we have 2 teams, then start new arena and invite players!
                if (found == 2)
                {
                    GroupQueueInfo aTeam = queueArray[BattleGroundTeamId.Alliance];
                    GroupQueueInfo hTeam = queueArray[BattleGroundTeamId.Horde];
                    Battleground arena = Global.BattlegroundMgr.CreateNewBattleground(m_queueId, bracket_id);
                    if (arena == null)
                    {
                        Log.outError(LogFilter.Battleground, "BattlegroundQueue.Update couldn't create arena instance for rated arena match!");
                        return;
                    }

                    aTeam.OpponentsTeamRating = hTeam.ArenaTeamRating;
                    hTeam.OpponentsTeamRating = aTeam.ArenaTeamRating;
                    aTeam.OpponentsMatchmakerRating = hTeam.ArenaMatchmakerRating;
                    hTeam.OpponentsMatchmakerRating = aTeam.ArenaMatchmakerRating;
                    Log.outDebug(LogFilter.Battleground, "setting oposite teamrating for team {0} to {1}", aTeam.ArenaTeamId, aTeam.OpponentsTeamRating);
                    Log.outDebug(LogFilter.Battleground, "setting oposite teamrating for team {0} to {1}", hTeam.ArenaTeamId, hTeam.OpponentsTeamRating);

                    // now we must move team if we changed its faction to another faction queue, because then we will spam log by errors in Queue.RemovePlayer
                    if (aTeam.Team != Team.Alliance)
                    {
                        m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance].Insert(0, aTeam);
                        m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeHorde].Remove(queueArray[BattleGroundTeamId.Alliance]);
                    }
                    if (hTeam.Team != Team.Horde)
                    {
                        m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeHorde].Insert(0, hTeam);
                        m_QueuedGroups[(int)bracket_id][BattlegroundConst.BgQueuePremadeAlliance].Remove(queueArray[BattleGroundTeamId.Horde]);
                    }

                    arena.SetArenaMatchmakerRating(Team.Alliance, aTeam.ArenaMatchmakerRating);
                    arena.SetArenaMatchmakerRating(Team.Horde, hTeam.ArenaMatchmakerRating);
                    InviteGroupToBG(aTeam, arena, Team.Alliance);
                    InviteGroupToBG(hTeam, arena, Team.Horde);

                    Log.outDebug(LogFilter.Battleground, "Starting rated arena match!");
                    arena.StartBattleground();
                }
            }
        }

        public BattlegroundQueueTypeId GetQueueId() { return m_queueId; }
        
        BattlegroundQueueTypeId m_queueId;

        Dictionary<ObjectGuid, PlayerQueueInfo> m_QueuedPlayers = new();

        /// <summary>
        /// This two dimensional array is used to store All queued groups
        /// First dimension specifies the bgTypeId
        /// Second dimension specifies the player's group types -
        /// BG_QUEUE_PREMADE_ALLIANCE  is used for premade alliance groups and alliance rated arena teams
        /// BG_QUEUE_PREMADE_HORDE     is used for premade horde groups and horde rated arena teams
        /// BattlegroundConst.BgQueueNormalAlliance   is used for normal (or small) alliance groups or non-rated arena matches
        /// BattlegroundConst.BgQueueNormalHorde      is used for normal (or small) horde groups or non-rated arena matches
        /// </summary>
        List<GroupQueueInfo>[][] m_QueuedGroups = new List<GroupQueueInfo>[(int)BattlegroundBracketId.Max][];

        uint[][][] m_WaitTimes = new uint[SharedConst.PvpTeamsCount][][];
        uint[][] m_WaitTimeLastPlayer = new uint[SharedConst.PvpTeamsCount][];
        uint[][] m_SumOfWaitTimes = new uint[SharedConst.PvpTeamsCount][];

        // Event handler
        EventSystem m_events = new();

        SelectionPool[] m_SelectionPools = new SelectionPool[SharedConst.PvpTeamsCount];
        // class to select and invite groups to bg
        class SelectionPool
        {
            public void Init()
            {
                SelectedGroups.Clear();
                PlayerCount = 0;
            }
            public bool AddGroup(GroupQueueInfo ginfo, uint desiredCount)
            {
                //if group is larger than desired count - don't allow to add it to pool
                if (ginfo.IsInvitedToBGInstanceGUID == 0 && desiredCount >= PlayerCount + ginfo.Players.Count)
                {
                    SelectedGroups.Add(ginfo);
                    // increase selected players count
                    PlayerCount += (uint)ginfo.Players.Count;
                    return true;
                }
                if (PlayerCount < desiredCount)
                    return true;

                return false;
            }
            public bool KickGroup(uint size)
            {
                //find maxgroup or LAST group with size == size and kick it
                bool found = false;
                GroupQueueInfo groupToKick = null;
                foreach (var groupQueueInfo in SelectedGroups)
                {
                    if (Math.Abs(groupQueueInfo.Players.Count - size) <= 1)
                    {
                        groupToKick = groupQueueInfo;
                        found = true;
                    }
                    else if (!found && groupQueueInfo.Players.Count >= groupToKick.Players.Count)
                        groupToKick = groupQueueInfo;
                }
                //if pool is empty, do nothing
                if (GetPlayerCount() != 0)
                {
                    //update player count
                    GroupQueueInfo ginfo = groupToKick;
                    SelectedGroups.Remove(groupToKick);
                    PlayerCount -= (uint)ginfo.Players.Count;
                    //return false if we kicked smaller group or there are enough players in selection pool
                    if (ginfo.Players.Count <= size + 1)
                        return false;
                }
                return true;
            }
            public uint GetPlayerCount() { return PlayerCount; }

            public List<GroupQueueInfo> SelectedGroups = new();

            uint PlayerCount;
        }

    }

    public struct BattlegroundQueueTypeId
    {  
        public ushort BattlemasterListId;
        public byte BgType;
        public bool Rated;
        public byte TeamSize;

        public BattlegroundQueueTypeId(ushort battlemasterListId, byte bgType, bool rated, byte teamSize)
        {
            BattlemasterListId = battlemasterListId;
            BgType = bgType;
            Rated = rated;
            TeamSize = teamSize;
        }

        public static BattlegroundQueueTypeId FromPacked(ulong packedQueueId)
        {
            return new BattlegroundQueueTypeId((ushort)(packedQueueId & 0xFFFF), (byte)((packedQueueId >> 16) & 0xF), ((packedQueueId >> 20) & 1) != 0, (byte)((packedQueueId >> 24) & 0x3F));
        }

        public ulong GetPacked()
        {
            return (ulong)BattlemasterListId
                | ((ulong)(BgType & 0xF) << 16)
                | ((ulong)(Rated ? 1 : 0) << 20)
                | ((ulong)(TeamSize & 0x3F) << 24)
                | 0x1F10000000000000;
        }

        public static bool operator ==(BattlegroundQueueTypeId left, BattlegroundQueueTypeId right)
        {
            return left.BattlemasterListId == right.BattlemasterListId
                && left.BgType == right.BgType
                && left.Rated == right.Rated
                && left.TeamSize == right.TeamSize;
        }

        public static bool operator !=(BattlegroundQueueTypeId left, BattlegroundQueueTypeId right)
        {
            return !(left == right);
        }

        public static bool operator <(BattlegroundQueueTypeId left, BattlegroundQueueTypeId right)
        {
            if (left.BattlemasterListId != right.BattlemasterListId)
                return left.BattlemasterListId < right.BattlemasterListId;
            if (left.BgType != right.BgType)
                return left.BgType < right.BgType;
            if (left.Rated != right.Rated)
                return (left.Rated ? 1 : 0) < (right.Rated ? 1 : 0);
            return left.TeamSize < right.TeamSize;
        }
        public static bool operator >(BattlegroundQueueTypeId left, BattlegroundQueueTypeId right)
        {
            if (left.BattlemasterListId != right.BattlemasterListId)
                return left.BattlemasterListId > right.BattlemasterListId;
            if (left.BgType != right.BgType)
                return left.BgType > right.BgType;
            if (left.Rated != right.Rated)
                return (left.Rated ? 1 : 0) > (right.Rated ? 1 : 0);
            return left.TeamSize > right.TeamSize;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return BattlemasterListId.GetHashCode() ^ BgType.GetHashCode() ^ Rated.GetHashCode() ^ TeamSize.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{ BattlemasterListId: {BattlemasterListId}, Type: {BgType}, Rated: {Rated}, TeamSize: {TeamSize} }}";
        }
    }

    /// <summary>
    /// stores information for players in queue
    /// </summary>
    public class PlayerQueueInfo
    {
        public uint LastOnlineTime;                                 // for tracking and removing offline players from queue after 5 minutes
        public GroupQueueInfo GroupInfo;                             // pointer to the associated groupqueueinfo
    }

    /// <summary>
    /// stores information about the group in queue (also used when joined as solo!)
    /// </summary>
    public class GroupQueueInfo
    {
        public Dictionary<ObjectGuid, PlayerQueueInfo> Players = new();             // player queue info map
        public Team Team;                                           // Player team (ALLIANCE/HORDE)
        public uint ArenaTeamId;                                    // team id if rated match
        public uint JoinTime;                                       // time when group was added
        public uint RemoveInviteTime;                               // time when we will remove invite for players in group
        public uint IsInvitedToBGInstanceGUID;                      // was invited to certain BG
        public uint ArenaTeamRating;                                // if rated match, inited to the rating of the team
        public uint ArenaMatchmakerRating;                          // if rated match, inited to the rating of the team
        public uint OpponentsTeamRating;                            // for rated arena matches
        public uint OpponentsMatchmakerRating;                      // for rated arena matches
    }

    /// <summary>
    /// This class is used to invite player to BG again, when minute lasts from his first invitation
    /// it is capable to solve all possibilities
    /// </summary>
    class BGQueueInviteEvent : BasicEvent
    {
        public BGQueueInviteEvent(ObjectGuid plGuid, uint bgInstanceGUID, BattlegroundTypeId bgTypeId, uint removeTime, BattlegroundQueueTypeId queueId)
        {
            m_PlayerGuid = plGuid;
            m_BgInstanceGUID = bgInstanceGUID;
            m_BgTypeId = bgTypeId;
            m_RemoveTime = removeTime;
            m_QueueId = queueId;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            Player player = Global.ObjAccessor.FindPlayer(m_PlayerGuid);
            // player logged off (we should do nothing, he is correctly removed from queue in another procedure)
            if (player == null)
                return true;

            Battleground bg = Global.BattlegroundMgr.GetBattleground(m_BgInstanceGUID, m_BgTypeId);
            //if Battleground ended and its instance deleted - do nothing
            if (bg == null)
                return true;

            uint queueSlot = player.GetBattlegroundQueueIndex(m_QueueId);
            if (queueSlot < SharedConst.PvpTeamsCount)         // player is in queue or in Battleground
            {
                // check if player is invited to this bg
                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(m_QueueId);
                if (bgQueue.IsPlayerInvited(m_PlayerGuid, m_BgInstanceGUID, m_RemoveTime))
                {
                    BattlefieldStatusNeedConfirmation battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusNeedConfirmation(out battlefieldStatus, bg, player, queueSlot, player.GetBattlegroundQueueJoinTime(m_QueueId), BattlegroundConst.InviteAcceptWaitTime - BattlegroundConst.InvitationRemindTime, m_QueueId);
                    player.SendPacket(battlefieldStatus);
                }
            }
            return true;                                            //event will be deleted
        }

        public override void Abort(ulong e_time) { }

        ObjectGuid m_PlayerGuid;
        uint m_BgInstanceGUID;
        BattlegroundTypeId m_BgTypeId;
        uint m_RemoveTime;
        BattlegroundQueueTypeId m_QueueId;
    }

    /// <summary>
    /// This class is used to remove player from BG queue after 1 minute 20 seconds from first invitation
    /// We must store removeInvite time in case player left queue and joined and is invited again
    /// We must store bgQueueTypeId, because Battleground can be deleted already, when player entered it
    /// </summary>
    class BGQueueRemoveEvent : BasicEvent
    {
        public BGQueueRemoveEvent(ObjectGuid plGuid, uint bgInstanceGUID, BattlegroundQueueTypeId bgQueueTypeId, uint removeTime)
        {
            m_PlayerGuid = plGuid;
            m_BgInstanceGUID = bgInstanceGUID;
            m_RemoveTime = removeTime;
            m_BgQueueTypeId = bgQueueTypeId;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            Player player = Global.ObjAccessor.FindPlayer(m_PlayerGuid);
            if (player == null)
                // player logged off (we should do nothing, he is correctly removed from queue in another procedure)
                return true;

            Battleground bg = Global.BattlegroundMgr.GetBattleground(m_BgInstanceGUID, (BattlegroundTypeId)m_BgQueueTypeId.BattlemasterListId);
            //Battleground can be deleted already when we are removing queue info
            //bg pointer can be NULL! so use it carefully!

            uint queueSlot = player.GetBattlegroundQueueIndex(m_BgQueueTypeId);
            if (queueSlot < SharedConst.PvpTeamsCount)         // player is in queue, or in Battleground
            {
                // check if player is in queue for this BG and if we are removing his invite event
                BattlegroundQueue bgQueue = Global.BattlegroundMgr.GetBattlegroundQueue(m_BgQueueTypeId);
                if (bgQueue.IsPlayerInvited(m_PlayerGuid, m_BgInstanceGUID, m_RemoveTime))
                {
                    Log.outDebug(LogFilter.Battleground, "Battleground: removing player {0} from bg queue for instance {1} because of not pressing enter battle in time.", player.GetGUID().ToString(), m_BgInstanceGUID);

                    player.RemoveBattlegroundQueueId(m_BgQueueTypeId);
                    bgQueue.RemovePlayer(m_PlayerGuid, true);
                    //update queues if Battleground isn't ended
                    if (bg != null && bg.IsBattleground() && bg.GetStatus() != BattlegroundStatus.WaitLeave)
                        Global.BattlegroundMgr.ScheduleQueueUpdate(0, m_BgQueueTypeId, bg.GetBracketId());

                    BattlefieldStatusNone battlefieldStatus;
                    Global.BattlegroundMgr.BuildBattlegroundStatusNone(out battlefieldStatus, player, queueSlot, player.GetBattlegroundQueueJoinTime(m_BgQueueTypeId));
                    player.SendPacket(battlefieldStatus);
                }
            }

            //event will be deleted
            return true;
        }

        public override void Abort(ulong e_time) { }

        ObjectGuid m_PlayerGuid;
        uint m_BgInstanceGUID;
        uint m_RemoveTime;
        BattlegroundQueueTypeId m_BgQueueTypeId;
    }
}
