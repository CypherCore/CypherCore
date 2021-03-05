﻿/*
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

using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Networking.Packets;

namespace Game.DungeonFinding
{
    public class LFGManager : Singleton<LFGManager>
    {
        private LFGManager()
        {
            m_lfgProposalId = 1;
            m_options = (LfgOptions)ConfigMgr.GetDefaultValue("DungeonFinder.OptionsMask", 1);

            new LFGPlayerScript();
            new LFGGroupScript();
        }

        public string ConcatenateDungeons(List<uint> dungeons)
        {
            var dungeonstr = new StringBuilder();
            if (!dungeons.Empty())
            {
                foreach (var id in dungeons)
                {
                    if (dungeonstr.Capacity != 0)
                        dungeonstr.AppendFormat(", {0}", id);
                    else
                        dungeonstr.AppendFormat("{0}", id);
                }
            }
            return dungeonstr.ToString();
        }

        public void _LoadFromDB(SQLFields field, ObjectGuid guid)
        {
            if (field == null)
                return;

            if (!guid.IsParty())
                return;

            SetLeader(guid, ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(0)));

            var dungeon = field.Read<uint>(18);
            var state = (LfgState)field.Read<byte>(19);

            if (dungeon == 0 || state == 0)
                return;

            SetDungeon(guid, dungeon);

            switch (state)
            {
                case LfgState.Dungeon:
                case LfgState.FinishedDungeon:
                    SetState(guid, state);
                    break;
                default:
                    break;
            }
        }

        private void _SaveToDB(ObjectGuid guid, uint db_guid)
        {
            if (!guid.IsParty())
                return;

            var trans = new SQLTransaction();

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_LFG_DATA);
            stmt.AddValue(0, db_guid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_LFG_DATA);
            stmt.AddValue(0, db_guid);
            stmt.AddValue(1, GetDungeon(guid));
            stmt.AddValue(2, GetState(guid));
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public void LoadRewards()
        {
            var oldMSTime = Time.GetMSTime();

            RewardMapStore.Clear();

            // ORDER BY is very important for GetRandomDungeonReward!
            var result = DB.World.Query("SELECT dungeonId, maxLevel, firstQuestId, otherQuestId FROM lfg_dungeon_rewards ORDER BY dungeonId, maxLevel ASC");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 lfg dungeon rewards. DB table `lfg_dungeon_rewards` is empty!");
                return;
            }

            uint count = 0;

            do
            {
                var dungeonId = result.Read<uint>(0);
                uint maxLevel = result.Read<byte>(1);
                var firstQuestId = result.Read<uint>(2);
                var otherQuestId = result.Read<uint>(3);

                if (GetLFGDungeonEntry(dungeonId) == 0)
                {
                    Log.outError(LogFilter.Sql, "Dungeon {0} specified in table `lfg_dungeon_rewards` does not exist!", dungeonId);
                    continue;
                }

                if (maxLevel == 0 || maxLevel > WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                {
                    Log.outError(LogFilter.Sql, "Level {0} specified for dungeon {1} in table `lfg_dungeon_rewards` can never be reached!", maxLevel, dungeonId);
                    maxLevel = WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel);
                }

                if (firstQuestId == 0 || Global.ObjectMgr.GetQuestTemplate(firstQuestId) == null)
                {
                    Log.outError(LogFilter.Sql, "First quest {0} specified for dungeon {1} in table `lfg_dungeon_rewards` does not exist!", firstQuestId, dungeonId);
                    continue;
                }

                if (otherQuestId != 0 && Global.ObjectMgr.GetQuestTemplate(otherQuestId) == null)
                {
                    Log.outError(LogFilter.Sql, "Other quest {0} specified for dungeon {1} in table `lfg_dungeon_rewards` does not exist!", otherQuestId, dungeonId);
                    otherQuestId = 0;
                }

                RewardMapStore.Add(dungeonId, new LfgReward(maxLevel, firstQuestId, otherQuestId));
                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} lfg dungeon rewards in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        private LFGDungeonData GetLFGDungeon(uint id)
        {
            return LfgDungeonStore.LookupByKey(id);
        }

        public void LoadLFGDungeons(bool reload = false)
        {
            var oldMSTime = Time.GetMSTime();

            LfgDungeonStore.Clear();

            // Initialize Dungeon map with data from dbcs
            foreach (var dungeon in CliDB.LFGDungeonsStorage.Values)
            {
                switch (dungeon.TypeID)
                {
                    case LfgType.Dungeon:
                    case LfgType.Raid:
                    case LfgType.RandomDungeon:
                    case LfgType.Zone:
                        LfgDungeonStore[dungeon.Id] = new LFGDungeonData(dungeon);
                        break;
                }
            }

            // Fill teleport locations from DB
            var result = DB.World.Query("SELECT dungeonId, position_x, position_y, position_z, orientation, requiredItemLevel FROM lfg_dungeon_template");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 lfg dungeon templates. DB table `lfg_dungeon_template` is empty!");
                return;
            }

            uint count = 0;

            do
            {
                var dungeonId = result.Read<uint>(0);
                if (!LfgDungeonStore.ContainsKey(dungeonId))
                {
                    Log.outError(LogFilter.Sql, "table `lfg_entrances` contains coordinates for wrong dungeon {0}", dungeonId);
                    continue;
                }

                var data = LfgDungeonStore[dungeonId];
                data.x = result.Read<float>(1);
                data.y = result.Read<float>(2);
                data.z = result.Read<float>(3);
                data.o = result.Read<float>(4);
                data.requiredItemLevel = result.Read<ushort>(5);

                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} lfg dungeon templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

            // Fill all other teleport coords from areatriggers
            foreach (var pair in LfgDungeonStore)
            {
                var dungeon = pair.Value;

                // No teleport coords in database, load from areatriggers
                if (dungeon.type != LfgType.RandomDungeon && dungeon.x == 0.0f && dungeon.y == 0.0f && dungeon.z == 0.0f)
                {
                    var at = Global.ObjectMgr.GetMapEntranceTrigger(dungeon.map);
                    if (at == null)
                    {
                        Log.outError(LogFilter.Lfg, "LoadLFGDungeons: Failed to load dungeon {0} (Id: {1}), cant find areatrigger for map {2}", dungeon.name, dungeon.id, dungeon.map);
                        continue;
                    }

                    dungeon.map = at.target_mapId;
                    dungeon.x = at.target_X;
                    dungeon.y = at.target_Y;
                    dungeon.z = at.target_Z;
                    dungeon.o = at.target_Orientation;
                }

                if (dungeon.type != LfgType.RandomDungeon)
                    CachedDungeonMapStore.Add((byte)dungeon.group, dungeon.id);
                CachedDungeonMapStore.Add(0, dungeon.id);
            }

            if (reload)
                CachedDungeonMapStore.Clear();
        }

        public void Update(uint diff)
        {
            if (!IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            var currTime = Time.UnixTime;

            // Remove obsolete role checks
            foreach (var pairCheck in RoleChecksStore)
            {
                var roleCheck = pairCheck.Value;
                if (currTime < roleCheck.cancelTime)
                    continue;
                roleCheck.state = LfgRoleCheckState.MissingRole;

                foreach (var pairRole in roleCheck.roles)
                {
                    var guid = pairRole.Key;
                    RestoreState(guid, "Remove Obsolete RoleCheck");
                    SendLfgRoleCheckUpdate(guid, roleCheck);
                    if (guid == roleCheck.leader)
                        SendLfgJoinResult(guid, new LfgJoinResultData(LfgJoinResult.RoleCheckFailed, LfgRoleCheckState.MissingRole));
                }

                RestoreState(pairCheck.Key, "Remove Obsolete RoleCheck");
                RoleChecksStore.Remove(pairCheck.Key);
            }

            // Remove obsolete proposals
            foreach (var removePair in ProposalsStore.ToList())
            {
                if (removePair.Value.cancelTime < currTime)
                    RemoveProposal(removePair, LfgUpdateType.ProposalFailed);
            }

            // Remove obsolete kicks
            foreach (var itBoot in BootsStore)
            {
                var boot = itBoot.Value;
                if (boot.cancelTime < currTime)
                {
                    boot.inProgress = false;
                    foreach (var itVotes in boot.votes)
                    {
                        var pguid = itVotes.Key;
                        if (pguid != boot.victim)
                            SendLfgBootProposalUpdate(pguid, boot);
                    }
                    SetVoteKick(itBoot.Key, false);
                    BootsStore.Remove(itBoot.Key);
                }
            }

            var lastProposalId = m_lfgProposalId;
            // Check if a proposal can be formed with the new groups being added
            foreach (var it in QueuesStore)
            {
                var newProposals = it.Value.FindGroups();
                if (newProposals != 0)
                    Log.outDebug(LogFilter.Lfg, "Update: Found {0} new groups in queue {1}", newProposals, it.Key);
            }

            if (lastProposalId != m_lfgProposalId)
            {
                // FIXME lastProposalId ? lastProposalId +1 ?
                foreach (var itProposal in ProposalsStore.SkipWhile(p => p.Key == m_lfgProposalId))
                {
                    var proposalId = itProposal.Key;
                    var proposal = ProposalsStore[proposalId];

                    var guid = ObjectGuid.Empty;
                    foreach (var itPlayers in proposal.players)
                    {
                        guid = itPlayers.Key;
                        SetState(guid, LfgState.Proposal);
                        var gguid = GetGroup(guid);
                        if (!gguid.IsEmpty())
                        {
                            SetState(gguid, LfgState.Proposal);
                            SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.ProposalBegin, GetSelectedDungeons(guid)), true);
                        }
                        else
                            SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.ProposalBegin, GetSelectedDungeons(guid)), false);
                        SendLfgUpdateProposal(guid, proposal);
                    }

                    if (proposal.state == LfgProposalState.Success)
                        UpdateProposal(proposalId, guid, true);
                }
            }

            // Update all players status queue info
            if (m_QueueTimer > SharedConst.LFGQueueUpdateInterval)
            {
                m_QueueTimer = 0;
                foreach (var it in QueuesStore)
                    it.Value.UpdateQueueTimers(it.Key, currTime);
            }
            else
                m_QueueTimer += diff;
        }

        public void JoinLfg(Player player, LfgRoles roles, List<uint> dungeons)
        {
            if (!player || player.GetSession() == null || dungeons.Empty())
                return;

            var grp = player.GetGroup();
            var guid = player.GetGUID();
            var gguid = grp ? grp.GetGUID() : guid;
            var joinData = new LfgJoinResultData();
            var players = new List<ObjectGuid>();
            uint rDungeonId = 0;
            var isContinue = grp && grp.IsLFGGroup() && GetState(gguid) != LfgState.FinishedDungeon;

            // Do not allow to change dungeon in the middle of a current dungeon
            if (isContinue)
            {
                dungeons.Clear();
                dungeons.Add(GetDungeon(gguid));
            }

            // Already in queue?
            var state = GetState(gguid);
            if (state == LfgState.Queued)
            {
                var queue = GetQueue(gguid);
                queue.RemoveFromQueue(gguid);
            }

            // Check player or group member restrictions
            if (!player.GetSession().HasPermission(RBACPermissions.JoinDungeonFinder))
                joinData.result = LfgJoinResult.NoSlots;
            else if (player.InBattleground() || player.InArena() || player.InBattlegroundQueue())
                joinData.result = LfgJoinResult.CantUseDungeons;
            else if (player.HasAura(SharedConst.LFGSpellDungeonDeserter))
                joinData.result = LfgJoinResult.DeserterPlayer;
            else if (player.HasAura(SharedConst.LFGSpellDungeonCooldown))
                joinData.result = LfgJoinResult.RandomCooldownPlayer;
            else if (dungeons.Empty())
                joinData.result = LfgJoinResult.NoSlots;
            else if (player.HasAura(9454)) // check Freeze debuff
                joinData.result = LfgJoinResult.NoSlots;
            else if (grp)
            {
                if (grp.GetMembersCount() > MapConst.MaxGroupSize)
                    joinData.result = LfgJoinResult.TooManyMembers;
                else
                {
                    byte memberCount = 0;
                    for (var refe = grp.GetFirstMember(); refe != null && joinData.result == LfgJoinResult.Ok; refe = refe.Next())
                    {
                        var plrg = refe.GetSource();
                        if (plrg)
                        {
                            if (!plrg.GetSession().HasPermission(RBACPermissions.JoinDungeonFinder))
                                joinData.result = LfgJoinResult.NoLfgObject;
                            if (plrg.HasAura(SharedConst.LFGSpellDungeonDeserter))
                                joinData.result = LfgJoinResult.DeserterParty;
                            else if (plrg.HasAura(SharedConst.LFGSpellDungeonCooldown))
                                joinData.result = LfgJoinResult.RandomCooldownParty;
                            else if (plrg.InBattleground() || plrg.InArena() || plrg.InBattlegroundQueue())
                                joinData.result = LfgJoinResult.CantUseDungeons;
                            else if (plrg.HasAura(9454)) // check Freeze debuff
                            {
                                joinData.result = LfgJoinResult.NoSlots;
                                joinData.playersMissingRequirement.Add(plrg.GetName());
                            }
                            ++memberCount;
                            players.Add(plrg.GetGUID());
                        }
                    }

                    if (joinData.result == LfgJoinResult.Ok && memberCount != grp.GetMembersCount())
                        joinData.result = LfgJoinResult.MembersNotPresent;
                }
            }
            else
                players.Add(player.GetGUID());

            // Check if all dungeons are valid
            var isRaid = false;
            if (joinData.result == LfgJoinResult.Ok)
            {
                var isDungeon = false;
                foreach (var it in dungeons)
                {
                    if (joinData.result != LfgJoinResult.Ok)
                        break;

                    var type = GetDungeonType(it);
                    switch (type)
                    {
                        case LfgType.RandomDungeon:
                            if (dungeons.Count > 1)               // Only allow 1 random dungeon
                                joinData.result = LfgJoinResult.InvalidSlot;
                            else
                                rDungeonId = dungeons.First();
                            goto case LfgType.Dungeon;
                        case LfgType.Dungeon:
                            if (isRaid)
                                joinData.result = LfgJoinResult.MismatchedSlots;
                            isDungeon = true;
                            break;
                        case LfgType.Raid:
                            if (isDungeon)
                                joinData.result = LfgJoinResult.MismatchedSlots;
                            isRaid = true;
                            break;
                        default:
                            Log.outError(LogFilter.Lfg, "Wrong dungeon type {0} for dungeon {1}", type, it);
                            joinData.result = LfgJoinResult.InvalidSlot;
                            break;
                    }
                }

                // it could be changed
                if (joinData.result == LfgJoinResult.Ok)
                {
                    // Expand random dungeons and check restrictions
                    if (rDungeonId != 0)
                        dungeons = GetDungeonsByRandom(rDungeonId);

                    // if we have lockmap then there are no compatible dungeons
                    GetCompatibleDungeons(dungeons, players, joinData.lockmap, joinData.playersMissingRequirement, isContinue);
                    if (dungeons.Empty())
                        joinData.result = LfgJoinResult.NoSlots;
                }
            }

            // Can't join. Send result
            if (joinData.result != LfgJoinResult.Ok)
            {
                Log.outDebug(LogFilter.Lfg, "Join: [{0}] joining with {1} members. result: {2}", guid, grp ? grp.GetMembersCount() : 1, joinData.result);
                if (!dungeons.Empty())                             // Only should show lockmap when have no dungeons available
                    joinData.lockmap.Clear();
                player.GetSession().SendLfgJoinResult(joinData);
                return;
            }

            if (isRaid)
            {
                Log.outDebug(LogFilter.Lfg, "Join: [{0}] trying to join raid browser and it's disabled.", guid);
                return;
            }

            var ticket = new RideTicket();
            ticket.RequesterGuid = guid;
            ticket.Id = GetQueueId(gguid);
            ticket.Type = RideType.Lfg;
            ticket.Time = (int)Time.UnixTime;

            var debugNames = "";
            if (grp)                                               // Begin rolecheck
            {
                // Create new rolecheck
                var roleCheck = new LfgRoleCheck();
                roleCheck.cancelTime = Time.UnixTime + SharedConst.LFGTimeRolecheck;
                roleCheck.state = LfgRoleCheckState.Initialiting;
                roleCheck.leader = guid;
                roleCheck.dungeons = dungeons;
                roleCheck.rDungeonId = rDungeonId;

                RoleChecksStore[gguid] = roleCheck;

                if (rDungeonId != 0)
                {
                    dungeons.Clear();
                    dungeons.Add(rDungeonId);
                }

                SetState(gguid, LfgState.Rolecheck);
                // Send update to player
                var updateData = new LfgUpdateData(LfgUpdateType.JoinQueue, dungeons);
                for (var refe = grp.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    var plrg = refe.GetSource();
                    if (plrg)
                    {
                        var pguid = plrg.GetGUID();
                        plrg.GetSession().SendLfgUpdateStatus(updateData, true);
                        SetState(pguid, LfgState.Rolecheck);
                        SetTicket(pguid, ticket);
                        if (!isContinue)
                            SetSelectedDungeons(pguid, dungeons);
                        roleCheck.roles[pguid] = 0;
                        if (!string.IsNullOrEmpty(debugNames))
                            debugNames += ", ";
                        debugNames += plrg.GetName();
                    }
                }
                // Update leader role
                UpdateRoleCheck(gguid, guid, roles);
            }
            else                                                   // Add player to queue
            {
                var rolesMap = new Dictionary<ObjectGuid, LfgRoles>();
                rolesMap[guid] = roles;
                var queue = GetQueue(guid);
                queue.AddQueueData(guid, Time.UnixTime, dungeons, rolesMap);

                if (!isContinue)
                {
                    if (rDungeonId != 0)
                    {
                        dungeons.Clear();
                        dungeons.Add(rDungeonId);
                    }
                    SetSelectedDungeons(guid, dungeons);
                }
                // Send update to player
                SetTicket(guid, ticket);
                SetRoles(guid, roles);
                player.GetSession().SendLfgUpdateStatus(new LfgUpdateData(LfgUpdateType.JoinQueueInitial, dungeons), false);
                SetState(gguid, LfgState.Queued);
                player.GetSession().SendLfgUpdateStatus(new LfgUpdateData(LfgUpdateType.AddedToQueue, dungeons), false);
                player.GetSession().SendLfgJoinResult(joinData);
                debugNames += player.GetName();
            }
            var o = new StringBuilder();
            o.AppendFormat("Join: [{0}] joined ({1}{2}) Members: {3}. Dungeons ({4}): ", guid, (grp ? "group" : "player"), debugNames, dungeons.Count, ConcatenateDungeons(dungeons));
            Log.outDebug(LogFilter.Lfg, o.ToString());
        }

        public void LeaveLfg(ObjectGuid guid, bool disconnected = false)
        {
            Log.outDebug(LogFilter.Lfg, "LeaveLfg: [{0}]", guid);

            var gguid = guid.IsParty() ? guid : GetGroup(guid);
            var state = GetState(guid);
            switch (state)
            {
                case LfgState.Queued:
                    if (!gguid.IsEmpty())
                    {
                        var queue = GetQueue(gguid);
                        queue.RemoveFromQueue(gguid);
                        SetState(gguid, LfgState.None);
                        var players = GetPlayers(gguid);
                        foreach (var it in players)
                        {
                            SetState(it, LfgState.None);
                            SendLfgUpdateStatus(it, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), true);
                        }
                    }
                    else
                    {
                        SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), false);
                        var queue = GetQueue(guid);
                        queue.RemoveFromQueue(guid);
                        SetState(guid, LfgState.None);
                    }
                    break;
                case LfgState.Rolecheck:
                    if (!gguid.IsEmpty())
                        UpdateRoleCheck(gguid);                    // No player to update role = LFG_ROLECHECK_ABORTED
                    break;
                case LfgState.Proposal:
                    {
                        // Remove from Proposals
                        var it = new KeyValuePair<uint, LfgProposal>();
                        var pguid = gguid == guid ? GetLeader(gguid) : guid;
                        foreach (var test in ProposalsStore)
                        {
                            it = test;
                            var itPlayer = it.Value.players.LookupByKey(pguid);
                            if (itPlayer != null)
                            {
                                // Mark the player/leader of group who left as didn't accept the proposal
                                itPlayer.accept = LfgAnswer.Deny;
                                break;
                            }
                        }

                        // Remove from queue - if proposal is found, RemoveProposal will call RemoveFromQueue
                        if (it.Value != null)
                            RemoveProposal(it, LfgUpdateType.ProposalDeclined);
                        break;
                    }
                case LfgState.None:
                case LfgState.Raidbrowser:
                    break;
                case LfgState.Dungeon:
                case LfgState.FinishedDungeon:
                    if (guid != gguid && !disconnected) // Player
                        SetState(guid, LfgState.None);
                    break;
            }
        }

        public RideTicket GetTicket(ObjectGuid guid)
        {
            var palyerData = PlayersStore.LookupByKey(guid);
            if (palyerData != null)
                return palyerData.GetTicket();

            return null;
        }

        public void UpdateRoleCheck(ObjectGuid gguid, ObjectGuid guid = default, LfgRoles roles = LfgRoles.None)
        {
            if (gguid.IsEmpty())
                return;

            Dictionary<ObjectGuid, LfgRoles> check_roles;
            var roleCheck = RoleChecksStore.LookupByKey(gguid);
            if (roleCheck == null)
                return;

            var sendRoleChosen = roleCheck.state != LfgRoleCheckState.Default && !guid.IsEmpty();

            if (guid.IsEmpty())
                roleCheck.state = LfgRoleCheckState.Aborted;
            else if (roles < LfgRoles.Tank)                            // Player selected no role.
                roleCheck.state = LfgRoleCheckState.NoRole;
            else
            {
                roleCheck.roles[guid] = roles;

                // Check if all players have selected a role
                var done = false;
                foreach (var rolePair in roleCheck.roles)
                {
                    if (rolePair.Value != LfgRoles.None)
                        continue;
                    done = true;
                }

                if (done)
                {
                    // use temporal var to check roles, CheckGroupRoles modifies the roles
                    check_roles = roleCheck.roles;
                    roleCheck.state = CheckGroupRoles(check_roles) ? LfgRoleCheckState.Finished : LfgRoleCheckState.WrongRoles;
                }
            }

            var dungeons = new List<uint>();
            if (roleCheck.rDungeonId != 0)
                dungeons.Add(roleCheck.rDungeonId);
            else
                dungeons = roleCheck.dungeons;

            var joinData = new LfgJoinResultData(LfgJoinResult.RoleCheckFailed, roleCheck.state);
            foreach (var it in roleCheck.roles)
            {
                var pguid = it.Key;

                if (sendRoleChosen)
                    SendLfgRoleChosen(pguid, guid, roles);

                SendLfgRoleCheckUpdate(pguid, roleCheck);
                switch (roleCheck.state)
                {
                    case LfgRoleCheckState.Initialiting:
                        continue;
                    case LfgRoleCheckState.Finished:
                        SetState(pguid, LfgState.Queued);
                        SetRoles(pguid, it.Value);
                        SendLfgUpdateStatus(pguid, new LfgUpdateData(LfgUpdateType.AddedToQueue, dungeons), true);
                        break;
                    default:
                        if (roleCheck.leader == pguid)
                            SendLfgJoinResult(pguid, joinData);
                        SendLfgUpdateStatus(pguid, new LfgUpdateData(LfgUpdateType.RolecheckFailed), true);
                        RestoreState(pguid, "Rolecheck Failed");
                        break;
                }
            }

            if (roleCheck.state == LfgRoleCheckState.Finished)
            {
                SetState(gguid, LfgState.Queued);
                var queue = GetQueue(gguid);
                queue.AddQueueData(gguid, Time.UnixTime, roleCheck.dungeons, roleCheck.roles);
                RoleChecksStore.Remove(gguid);
            }
            else if (roleCheck.state != LfgRoleCheckState.Initialiting)
            {
                RestoreState(gguid, "Rolecheck Failed");
                RoleChecksStore.Remove(gguid);
            }
        }

        private void GetCompatibleDungeons(List<uint> dungeons, List<ObjectGuid> players, Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>> lockMap, List<string> playersMissingRequirement, bool isContinue)
        {
            lockMap.Clear();
            var lockedDungeons = new Dictionary<uint, uint>();
            var dungeonsToRemove = new List<uint>();

            foreach (var guid in players)
            {
                if (dungeons.Empty())
                    break;

                var cachedLockMap = GetLockedDungeons(guid);
                var player = Global.ObjAccessor.FindConnectedPlayer(guid);
                foreach (var it2 in cachedLockMap)
                {
                    if (dungeons.Empty())
                        break;

                    var dungeonId = (it2.Key & 0x00FFFFFF); // Compare dungeon ids
                    if (dungeons.Contains(dungeonId))
                    {
                        var eraseDungeon = true;

                        // Don't remove the dungeon if team members are trying to continue a locked instance
                        if (it2.Value.lockStatus == LfgLockStatusType.RaidLocked && isContinue)
                        {
                            var dungeon = GetLFGDungeon(dungeonId);
                            Cypher.Assert(dungeon != null);
                            Cypher.Assert(player);
                            var playerBind = player.GetBoundInstance(dungeon.map, dungeon.difficulty);
                            if (playerBind != null)
                            {
                                var playerSave = playerBind.save;
                                if (playerSave != null)
                                {
                                    var dungeonInstanceId = playerSave.GetInstanceId();
                                    var itLockedDungeon = lockedDungeons.LookupByKey(dungeonId);
                                    if (itLockedDungeon == 0 || itLockedDungeon == dungeonInstanceId)
                                        eraseDungeon = false;

                                    lockedDungeons[dungeonId] = dungeonInstanceId;
                                }
                            }
                        }

                        if (eraseDungeon)
                            dungeonsToRemove.Add(dungeonId);

                        if (!lockMap.ContainsKey(guid))
                            lockMap[guid] = new Dictionary<uint, LfgLockInfoData>();

                        lockMap[guid][it2.Key] = it2.Value;
                        playersMissingRequirement.Add(player.GetName());
                    }
                }
            }

            foreach (var dungeonIdToRemove in dungeonsToRemove)
                dungeons.Remove(dungeonIdToRemove);

            if (!dungeons.Empty())
                lockMap.Clear();
        }

        public bool CheckGroupRoles(Dictionary<ObjectGuid, LfgRoles> groles)
        {
            if (groles.Empty())
                return false;

            byte damage = 0;
            byte tank = 0;
            byte healer = 0;

            var keys = new List<ObjectGuid>(groles.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var it = groles[keys[i]];
                var role = it & ~LfgRoles.Leader;
                if (role == LfgRoles.None)
                    return false;

                if (role.HasAnyFlag(LfgRoles.Damage))
                {
                    if (role != LfgRoles.Damage)
                    {
                        it -= (byte)LfgRoles.Damage;
                        if (CheckGroupRoles(groles))
                            return true;
                        it += (byte)LfgRoles.Damage;
                    }
                    else if (damage == SharedConst.LFGDPSNeeded)
                        return false;
                    else
                        damage++;
                }

                if (role.HasAnyFlag(LfgRoles.Healer))
                {
                    if (role != LfgRoles.Healer)
                    {
                        it -= (byte)LfgRoles.Healer;
                        if (CheckGroupRoles(groles))
                            return true;
                        it += (byte)LfgRoles.Healer;
                    }
                    else if (healer == SharedConst.LFGHealersNeeded)
                        return false;
                    else
                        healer++;
                }

                if (role.HasAnyFlag(LfgRoles.Tank))
                {
                    if (role != LfgRoles.Tank)
                    {
                        it -= (byte)LfgRoles.Tank;
                        if (CheckGroupRoles(groles))
                            return true;
                        it += (byte)LfgRoles.Tank;
                    }
                    else if (tank == SharedConst.LFGTanksNeeded)
                        return false;
                    else
                        tank++;
                }
            }
            return (tank + healer + damage) == (byte)groles.Count;
        }

        private void MakeNewGroup(LfgProposal proposal)
        {
            var players = new List<ObjectGuid>();
            var playersToTeleport = new List<ObjectGuid>();

            foreach (var it in proposal.players)
            {
                var guid = it.Key;
                if (guid == proposal.leader)
                    players.Insert(0, guid);
                else
                    players.Add(guid);

                if (proposal.isNew || GetGroup(guid) != proposal.group)
                    playersToTeleport.Add(guid);
            }

            // Set the dungeon difficulty
            var dungeon = GetLFGDungeon(proposal.dungeonId);
            Cypher.Assert(dungeon != null);

            var grp = !proposal.group.IsEmpty() ? Global.GroupMgr.GetGroupByGUID(proposal.group) : null;
            foreach (var pguid in players)
            {
                var player = Global.ObjAccessor.FindConnectedPlayer(pguid);
                if (!player)
                    continue;

                var group = player.GetGroup();
                if (group && group != grp)
                    group.RemoveMember(player.GetGUID());

                if (!grp)
                {
                    grp = new Group();
                    grp.ConvertToLFG();
                    grp.Create(player);
                    var gguid = grp.GetGUID();
                    SetState(gguid, LfgState.Proposal);
                    Global.GroupMgr.AddGroup(grp);
                }
                else if (group != grp)
                    grp.AddMember(player);

                grp.SetLfgRoles(pguid, proposal.players.LookupByKey(pguid).role);

                // Add the cooldown spell if queued for a random dungeon
                if (dungeon.type == LfgType.RandomDungeon)
                    player.CastSpell(player, SharedConst.LFGSpellDungeonCooldown, false);
            }

            grp.SetDungeonDifficultyID(dungeon.difficulty);
            var _guid = grp.GetGUID();
            SetDungeon(_guid, dungeon.Entry());
            SetState(_guid, LfgState.Dungeon);

            _SaveToDB(_guid, grp.GetDbStoreId());

            // Teleport Player
            foreach (var it in playersToTeleport)
            {
                var player = Global.ObjAccessor.FindPlayer(it);
                if (player)
                    TeleportPlayer(player, false);
            }

            // Update group info
            grp.SendUpdate();
        }

        public uint AddProposal(LfgProposal proposal)
        {
            proposal.id = ++m_lfgProposalId;
            ProposalsStore[m_lfgProposalId] = proposal;
            return m_lfgProposalId;
        }

        public void UpdateProposal(uint proposalId, ObjectGuid guid, bool accept)
        {
            // Check if the proposal exists
            var proposal = ProposalsStore.LookupByKey(proposalId);
            if (proposal == null)
                return;

            // Check if proposal have the current player
            var player = proposal.players.LookupByKey(guid);
            if (player == null)
                return;

            player.accept = (LfgAnswer)Convert.ToInt32(accept);

            Log.outDebug(LogFilter.Lfg, "UpdateProposal: Player [{0}] of proposal {1} selected: {2}", guid, proposalId, accept);
            if (!accept)
            {
                RemoveProposal(new KeyValuePair<uint, LfgProposal>(proposalId, proposal), LfgUpdateType.ProposalDeclined);
                return;
            }

            // check if all have answered and reorder players (leader first)
            var allAnswered = true;
            foreach (var itPlayers in proposal.players)
                if (itPlayers.Value.accept != LfgAnswer.Agree)   // No answer (-1) or not accepted (0)
                    allAnswered = false;

            if (!allAnswered)
            {
                foreach (var it in proposal.players)
                    SendLfgUpdateProposal(it.Key, proposal);

                return;
            }

            var sendUpdate = proposal.state != LfgProposalState.Success;
            proposal.state = LfgProposalState.Success;
            var joinTime = Time.UnixTime;

            var queue = GetQueue(guid);
            var updateData = new LfgUpdateData(LfgUpdateType.GroupFound);
            foreach (var it in proposal.players)
            {
                var pguid = it.Key;
                var gguid = it.Value.group;
                var dungeonId = GetSelectedDungeons(pguid).First();
                var waitTime = -1;
                if (sendUpdate)
                    SendLfgUpdateProposal(pguid, proposal);

                if (!gguid.IsEmpty())
                {
                    waitTime = (int)((joinTime - queue.GetJoinTime(gguid)) / Time.InMilliseconds);
                    SendLfgUpdateStatus(pguid, updateData, false);
                }
                else
                {
                    waitTime = (int)((joinTime - queue.GetJoinTime(pguid)) / Time.InMilliseconds);
                    SendLfgUpdateStatus(pguid, updateData, false);
                }
                updateData.updateType = LfgUpdateType.RemovedFromQueue;
                SendLfgUpdateStatus(pguid, updateData, true);
                SendLfgUpdateStatus(pguid, updateData, false);

                // Update timers
                var role = GetRoles(pguid);
                role &= ~LfgRoles.Leader;
                switch (role)
                {
                    case LfgRoles.Damage:
                        queue.UpdateWaitTimeDps(waitTime, dungeonId);
                        break;
                    case LfgRoles.Healer:
                        queue.UpdateWaitTimeHealer(waitTime, dungeonId);
                        break;
                    case LfgRoles.Tank:
                        queue.UpdateWaitTimeTank(waitTime, dungeonId);
                        break;
                    default:
                        queue.UpdateWaitTimeAvg(waitTime, dungeonId);
                        break;
                }

                SetState(pguid, LfgState.Dungeon);
            }

            // Remove players/groups from Queue
            foreach (var it in proposal.queues)
                queue.RemoveFromQueue(it);

            MakeNewGroup(proposal);
            ProposalsStore.Remove(proposalId);
        }

        private void RemoveProposal(KeyValuePair<uint, LfgProposal> itProposal, LfgUpdateType type)
        {
            var proposal = itProposal.Value;
            proposal.state = LfgProposalState.Failed;

            Log.outDebug(LogFilter.Lfg, "RemoveProposal: Proposal {0}, state FAILED, UpdateType {1}", itProposal.Key, type);
            // Mark all people that didn't answered as no accept
            if (type == LfgUpdateType.ProposalFailed)
                foreach (var it in proposal.players)
                    if (it.Value.accept == LfgAnswer.Pending)
                        it.Value.accept = LfgAnswer.Deny;

            // Mark players/groups to be removed
            var toRemove = new List<ObjectGuid>();
            foreach (var it in proposal.players)
            {
                if (it.Value.accept == LfgAnswer.Agree)
                    continue;

                var guid = !it.Value.group.IsEmpty() ? it.Value.group : it.Key;
                // Player didn't accept or still pending when no secs left
                if (it.Value.accept == LfgAnswer.Deny || type == LfgUpdateType.ProposalFailed)
                {
                    it.Value.accept = LfgAnswer.Deny;
                    toRemove.Add(guid);
                }
            }

            // Notify players
            foreach (var it in proposal.players)
            {
                var guid = it.Key;
                var gguid = !it.Value.group.IsEmpty() ? it.Value.group : guid;

                SendLfgUpdateProposal(guid, proposal);

                if (toRemove.Contains(gguid))         // Didn't accept or in same group that someone that didn't accept
                {
                    var updateData = new LfgUpdateData();
                    if (it.Value.accept == LfgAnswer.Deny)
                    {
                        updateData.updateType = type;
                        Log.outDebug(LogFilter.Lfg, "RemoveProposal: [{0}] didn't accept. Removing from queue and compatible cache", guid);
                    }
                    else
                    {
                        updateData.updateType = LfgUpdateType.RemovedFromQueue;
                        Log.outDebug(LogFilter.Lfg, "RemoveProposal: [{0}] in same group that someone that didn't accept. Removing from queue and compatible cache", guid);
                    }

                    RestoreState(guid, "Proposal Fail (didn't accepted or in group with someone that didn't accept");
                    if (gguid != guid)
                    {
                        RestoreState(it.Value.group, "Proposal Fail (someone in group didn't accepted)");
                        SendLfgUpdateStatus(guid, updateData, true);
                    }
                    else
                        SendLfgUpdateStatus(guid, updateData, false);
                }
                else
                {
                    Log.outDebug(LogFilter.Lfg, "RemoveProposal: Readding [{0}] to queue.", guid);
                    SetState(guid, LfgState.Queued);
                    if (gguid != guid)
                    {
                        SetState(gguid, LfgState.Queued);
                        SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.AddedToQueue, GetSelectedDungeons(guid)), true);
                    }
                    else
                        SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.AddedToQueue, GetSelectedDungeons(guid)), false);
                }
            }

            var queue = GetQueue(proposal.players.First().Key);
            // Remove players/groups from queue
            foreach (var guid in toRemove)
            {
                queue.RemoveFromQueue(guid);
                proposal.queues.Remove(guid);
            }

            // Readd to queue
            foreach (var guid in proposal.queues)
                queue.AddToQueue(guid, true);

            ProposalsStore.Remove(itProposal.Key);
        }

        public void InitBoot(ObjectGuid gguid, ObjectGuid kicker, ObjectGuid victim, string reason)
        {
            SetVoteKick(gguid, true);

            var boot = BootsStore[gguid];
            boot.inProgress = true;
            boot.cancelTime = Time.UnixTime + SharedConst.LFGTimeBoot;
            boot.reason = reason;
            boot.victim = victim;

            var players = GetPlayers(gguid);

            // Set votes
            foreach (var guid in players)
                boot.votes[guid] = LfgAnswer.Pending;

            boot.votes[victim] = LfgAnswer.Deny;                  // Victim auto vote NO
            boot.votes[kicker] = LfgAnswer.Agree;                 // Kicker auto vote YES

            // Notify players
            foreach (var it in players)
                SendLfgBootProposalUpdate(it, boot);
        }

        public void UpdateBoot(ObjectGuid guid, bool accept)
        {
            var gguid = GetGroup(guid);
            if (gguid.IsEmpty())
                return;

            var boot = BootsStore.LookupByKey(gguid);
            if (boot == null)
                return;

            if (boot.votes[guid] != LfgAnswer.Pending)    // Cheat check: Player can't vote twice
                return;

            boot.votes[guid] = (LfgAnswer)Convert.ToInt32(accept);

            byte votesNum = 0;
            byte agreeNum = 0;
            foreach (var itVotes in boot.votes)
            {
                if (itVotes.Value != LfgAnswer.Pending)
                {
                    ++votesNum;
                    if (itVotes.Value == LfgAnswer.Agree)
                        ++agreeNum;
                }
            }

            // if we don't have enough votes (agree or deny) do nothing
            if (agreeNum < SharedConst.LFGKickVotesNeeded && (votesNum - agreeNum) < SharedConst.LFGKickVotesNeeded)
                return;

            // Send update info to all players
            boot.inProgress = false;
            foreach (var itVotes in boot.votes)
            {
                var pguid = itVotes.Key;
                if (pguid != boot.victim)
                    SendLfgBootProposalUpdate(pguid, boot);
            }

            SetVoteKick(gguid, false);
            if (agreeNum == SharedConst.LFGKickVotesNeeded)           // Vote passed - Kick player
            {
                var group = Global.GroupMgr.GetGroupByGUID(gguid);
                if (group)
                    Player.RemoveFromGroup(group, boot.victim, RemoveMethod.KickLFG);
                DecreaseKicksLeft(gguid);
            }
            BootsStore.Remove(gguid);
        }

        public void TeleportPlayer(Player player, bool outt, bool fromOpcode = false)
        {
            LFGDungeonData dungeon = null;
            var group = player.GetGroup();

            if (group && group.IsLFGGroup())
                dungeon = GetLFGDungeon(GetDungeon(group.GetGUID()));

            if (dungeon == null)
            {
                Log.outDebug(LogFilter.Lfg, "TeleportPlayer: Player {0} not in group/lfggroup or dungeon not found!", player.GetName());
                player.GetSession().SendLfgTeleportError(LfgTeleportResult.NoReturnLocation);
                return;
            }

            if (outt)
            {
                Log.outDebug(LogFilter.Lfg, "TeleportPlayer: Player {0} is being teleported out. Current Map {1} - Expected Map {2}", player.GetName(), player.GetMapId(), dungeon.map);
                if (player.GetMapId() == dungeon.map)
                    player.TeleportToBGEntryPoint();

                return;
            }

            var error = LfgTeleportResult.None;
            if (!player.IsAlive())
                error = LfgTeleportResult.Dead;
            else if (player.IsFalling() || player.HasUnitState(UnitState.Jumping))
                error = LfgTeleportResult.Falling;
            else if (player.IsMirrorTimerActive(MirrorTimerType.Fatigue))
                error = LfgTeleportResult.Exhaustion;
            else if (player.GetVehicle())
                error = LfgTeleportResult.OnTransport;
            else if (!player.GetCharmGUID().IsEmpty())
                error = LfgTeleportResult.ImmuneToSummons;
            else if (player.HasAura(9454)) // check Freeze debuff
                error = LfgTeleportResult.NoReturnLocation;
            else if (player.GetMapId() != dungeon.map)  // Do not teleport players in dungeon to the entrance
            {
                var mapid = dungeon.map;
                var x = dungeon.x;
                var y = dungeon.y;
                var z = dungeon.z;
                var orientation = dungeon.o;

                if (!fromOpcode)
                {
                    // Select a player inside to be teleported to
                    for (var refe = group.GetFirstMember(); refe != null && mapid == 0; refe = refe.Next())
                    {
                        var plrg = refe.GetSource();
                        if (plrg && plrg != player && plrg.GetMapId() == dungeon.map)
                        {
                            mapid = plrg.GetMapId();
                            x = plrg.GetPositionX();
                            y = plrg.GetPositionY();
                            z = plrg.GetPositionZ();
                            orientation = plrg.GetOrientation();
                            break;
                        }
                    }
                }

                if (!player.GetMap().IsDungeon())
                    player.SetBattlegroundEntryPoint();

                if (player.IsInFlight())
                {
                    player.GetMotionMaster().MovementExpired();
                    player.CleanupAfterTaxiFlight();
                }

                if (!player.TeleportTo(mapid, x, y, z, orientation))
                    error = LfgTeleportResult.NoReturnLocation;
            }
            else
                error = LfgTeleportResult.NoReturnLocation;

            if (error != LfgTeleportResult.None)
                player.GetSession().SendLfgTeleportError(error);

            Log.outDebug(LogFilter.Lfg, "TeleportPlayer: Player {0} is being teleported in to map {1} (x: {2}, y: {3}, z: {4}) Result: {5}", player.GetName(), dungeon.map, dungeon.x, dungeon.y, dungeon.z, error);
        }

        public void FinishDungeon(ObjectGuid gguid, uint dungeonId, Map currMap)
        {
            var gDungeonId = GetDungeon(gguid);
            if (gDungeonId != dungeonId)
            {
                Log.outDebug(LogFilter.Lfg, $"Group {gguid} finished dungeon {dungeonId} but queued for {gDungeonId}. Ignoring");
                return;
            }

            if (GetState(gguid) == LfgState.FinishedDungeon) // Shouldn't happen. Do not reward multiple times
            {
                Log.outDebug(LogFilter.Lfg, $"Group {gguid} already rewarded");
                return;
            }

            SetState(gguid, LfgState.FinishedDungeon);

            var players = GetPlayers(gguid);
            foreach (var guid in players)
            {
                if (GetState(guid) == LfgState.FinishedDungeon)
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} already rewarded");
                    continue;
                }

                uint rDungeonId = 0;
                var dungeons = GetSelectedDungeons(guid);
                if (!dungeons.Empty())
                    rDungeonId = dungeons.First();

                SetState(guid, LfgState.FinishedDungeon);

                // Give rewards only if its a random dungeon
                var dungeon = GetLFGDungeon(rDungeonId);

                if (dungeon == null || (dungeon.type != LfgType.RandomDungeon && !dungeon.seasonal))
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} dungeon {rDungeonId} is not random or seasonal");
                    continue;
                }

                var player = Global.ObjAccessor.FindPlayer(guid);
                if (!player || player.GetMap() != currMap)
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} not found in world");
                    continue;
                }

                var dungeonDone = GetLFGDungeon(dungeonId);
                var mapId = dungeonDone != null ? dungeonDone.map : 0;

                if (player.GetMapId() != mapId)
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} is in map {player.GetMapId()} and should be in {mapId} to get reward");
                    continue;
                }

                // Update achievements
                if (dungeon.difficulty == Difficulty.Heroic)
                    player.UpdateCriteria(CriteriaTypes.UseLfdToGroupWithPlayers, 1);

                var reward = GetRandomDungeonReward(rDungeonId, player.GetLevel());
                if (reward == null)
                    continue;

                var done = false;
                var quest = Global.ObjectMgr.GetQuestTemplate(reward.firstQuest);
                if (quest == null)
                    continue;

                // if we can take the quest, means that we haven't done this kind of "run", IE: First Heroic Random of Day.
                if (player.CanRewardQuest(quest, false))
                    player.RewardQuest(quest, LootItemType.Item, 0, null, false);
                else
                {
                    done = true;
                    quest = Global.ObjectMgr.GetQuestTemplate(reward.otherQuest);
                    if (quest == null)
                        continue;
                    // we give reward without informing client (retail does this)
                    player.RewardQuest(quest, LootItemType.Item, 0, null, false);
                }

                // Give rewards
                var doneString = done ? "" : "not";
                Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} done dungeon {GetDungeon(gguid)}, {doneString} previously done.");
                var data = new LfgPlayerRewardData(dungeon.Entry(), GetDungeon(gguid, false), done, quest);
                player.GetSession().SendLfgPlayerReward(data);
            }
        }

        private List<uint> GetDungeonsByRandom(uint randomdungeon)
        {
            var dungeon = GetLFGDungeon(randomdungeon);
            var group = (byte)(dungeon != null ? dungeon.group : 0);
            return CachedDungeonMapStore.LookupByKey(group);
        }

        public LfgReward GetRandomDungeonReward(uint dungeon, uint level)
        {
            LfgReward reward = null;
            var bounds = RewardMapStore.LookupByKey(dungeon & 0x00FFFFFF);
            foreach (var rew in bounds)
            {
                reward = rew;
                // ordered properly at loading
                if (rew.maxLevel >= level)
                    break;
            }

            return reward;
        }

        public LfgType GetDungeonType(uint dungeonId)
        {
            var dungeon = GetLFGDungeon(dungeonId);
            if (dungeon == null)
                return LfgType.None;

            return dungeon.type;
        }

        public LfgState GetState(ObjectGuid guid)
        {
            LfgState state;
            if (guid.IsParty())
            {
                if (!GroupsStore.ContainsKey(guid))
                    return LfgState.None;

                state = GroupsStore[guid].GetState();
            }
            else
            {
                AddPlayerData(guid);
                state = PlayersStore[guid].GetState();
            }

            Log.outDebug(LogFilter.Lfg, "GetState: [{0}] = {1}", guid, state);
            return state;
        }

        public LfgState GetOldState(ObjectGuid guid)
        {
            LfgState state;
            if (guid.IsParty())
                state = GroupsStore[guid].GetOldState();
            else
            {
                AddPlayerData(guid);
                state = PlayersStore[guid].GetOldState();
            }

            Log.outDebug(LogFilter.Lfg, "GetOldState: [{0}] = {1}", guid, state);
            return state;
        }

        public bool IsVoteKickActive(ObjectGuid gguid)
        {
            Cypher.Assert(gguid.IsParty());

            var active = GroupsStore[gguid].IsVoteKickActive();
            Log.outInfo(LogFilter.Lfg, "Group: {0}, Active: {1}", gguid.ToString(), active);

            return active;
        }

        public uint GetDungeon(ObjectGuid guid, bool asId = true)
        {
            if (!GroupsStore.ContainsKey(guid))
                return 0;

            var dungeon = GroupsStore[guid].GetDungeon(asId);
            Log.outDebug(LogFilter.Lfg, "GetDungeon: [{0}] asId: {1} = {2}", guid, asId, dungeon);
            return dungeon;
        }

        public uint GetDungeonMapId(ObjectGuid guid)
        {
            if (!GroupsStore.ContainsKey(guid))
                return 0;

            var dungeonId = GroupsStore[guid].GetDungeon(true);
            uint mapId = 0;
            if (dungeonId != 0)
            {
                var dungeon = GetLFGDungeon(dungeonId);
                if (dungeon != null)
                    mapId = dungeon.map;
            }

            Log.outError(LogFilter.Lfg, "GetDungeonMapId: [{0}] = {1} (DungeonId = {2})", guid, mapId, dungeonId);
            return mapId;
        }

        public LfgRoles GetRoles(ObjectGuid guid)
        {
            var roles = PlayersStore[guid].GetRoles();
            Log.outDebug(LogFilter.Lfg, "GetRoles: [{0}] = {1}", guid, roles);
            return roles;
        }

        public List<uint> GetSelectedDungeons(ObjectGuid guid)
        {
            Log.outDebug(LogFilter.Lfg, "GetSelectedDungeons: [{0}]", guid);
            return PlayersStore[guid].GetSelectedDungeons();
        }

        public uint GetSelectedRandomDungeon(ObjectGuid guid)
        {
            if (GetState(guid) != LfgState.None)
            {
                var dungeons = GetSelectedDungeons(guid);
                if (!dungeons.Empty())
                {
                    var dungeon = GetLFGDungeon(dungeons.First());
                    if (dungeon != null && dungeon.type == LfgType.Raid)
                        return dungeons.First();
                }
            }

            return 0;
        }

        public Dictionary<uint, LfgLockInfoData> GetLockedDungeons(ObjectGuid guid)
        {
            var lockDic = new Dictionary<uint, LfgLockInfoData>();
            var player = Global.ObjAccessor.FindConnectedPlayer(guid);
            if (!player)
            {
                Log.outWarn(LogFilter.Lfg, "{0} not ingame while retrieving his LockedDungeons.", guid.ToString());
                return lockDic;
            }

            var level = player.GetLevel();
            var expansion = player.GetSession().GetExpansion();
            var dungeons = GetDungeonsByRandom(0);
            var denyJoin = !player.GetSession().HasPermission(RBACPermissions.JoinDungeonFinder);

            foreach (var it in dungeons)
            {
                var dungeon = GetLFGDungeon(it);
                if (dungeon == null) // should never happen - We provide a list from sLFGDungeonStore
                    continue;

                LfgLockStatusType lockStatus = 0;
                AccessRequirement ar;
                if (denyJoin)
                    lockStatus = LfgLockStatusType.RaidLocked;
                else if (dungeon.expansion > (uint)expansion)
                    lockStatus = LfgLockStatusType.InsufficientExpansion;
                else if (Global.DisableMgr.IsDisabledFor(DisableType.Map, dungeon.map, player))
                    lockStatus = LfgLockStatusType.NotInSeason;
                else if (Global.DisableMgr.IsDisabledFor(DisableType.LFGMap, dungeon.map, player))
                    lockStatus = LfgLockStatusType.RaidLocked;
                else if (dungeon.difficulty > Difficulty.Normal && player.GetBoundInstance(dungeon.map, dungeon.difficulty) != null)
                    lockStatus = LfgLockStatusType.RaidLocked;
                else if (dungeon.seasonal && !IsSeasonActive(dungeon.id))
                    lockStatus = LfgLockStatusType.NotInSeason;
                else if (dungeon.requiredItemLevel > player.GetAverageItemLevel())
                    lockStatus = LfgLockStatusType.TooLowGearScore;
                else if ((ar = Global.ObjectMgr.GetAccessRequirement(dungeon.map, dungeon.difficulty)) != null)
                {
                    if (ar.achievement != 0 && !player.HasAchieved(ar.achievement))
                        lockStatus = LfgLockStatusType.MissingAchievement;
                    else if (player.GetTeam() == Team.Alliance && ar.quest_A != 0 && !player.GetQuestRewardStatus(ar.quest_A))
                        lockStatus = LfgLockStatusType.QuestNotCompleted;
                    else if (player.GetTeam() == Team.Horde && ar.quest_H != 0 && !player.GetQuestRewardStatus(ar.quest_H))
                        lockStatus = LfgLockStatusType.QuestNotCompleted;
                    else
                        if (ar.item != 0)
                    {
                        if (!player.HasItemCount(ar.item) && (ar.item2 == 0 || !player.HasItemCount(ar.item2)))
                            lockStatus = LfgLockStatusType.MissingItem;
                    }
                    else if (ar.item2 != 0 && !player.HasItemCount(ar.item2))
                        lockStatus = LfgLockStatusType.MissingItem;
                }
                else
                {
                    var levels = Global.DB2Mgr.GetContentTuningData(dungeon.contentTuningId, player.m_playerData.CtrOptions.GetValue().ContentTuningConditionMask);
                    if (levels.HasValue)
                    {
                        if (levels.Value.MinLevel > level)
                            lockStatus = LfgLockStatusType.TooLowLevel;
                        if (levels.Value.MaxLevel < level)
                            lockStatus = LfgLockStatusType.TooHighLevel;
                    }
                }

                /* @todo VoA closed if WG is not under team control (LFG_LOCKSTATUS_RAID_LOCKED)
                lockData = LFG_LOCKSTATUS_TOO_HIGH_GEAR_SCORE;
                lockData = LFG_LOCKSTATUS_ATTUNEMENT_TOO_LOW_LEVEL;
                lockData = LFG_LOCKSTATUS_ATTUNEMENT_TOO_HIGH_LEVEL;
                */
                if (lockStatus != 0)
                    lockDic[dungeon.Entry()] = new LfgLockInfoData(lockStatus, dungeon.requiredItemLevel, player.GetAverageItemLevel());
            }

            return lockDic;
        }

        public byte GetKicksLeft(ObjectGuid guid)
        {
            var kicks = GroupsStore[guid].GetKicksLeft();
            Log.outDebug(LogFilter.Lfg, "GetKicksLeft: [{0}] = {1}", guid, kicks);
            return kicks;
        }

        private void RestoreState(ObjectGuid guid, string debugMsg)
        {
            if (guid.IsParty())
            {
                var data = GroupsStore[guid];
                data.RestoreState();
            }
            else
            {
                var data = PlayersStore[guid];
                data.RestoreState();
            }
        }

        public void SetState(ObjectGuid guid, LfgState state)
        {
            if (guid.IsParty())
            {
                if (!GroupsStore.ContainsKey(guid))
                    GroupsStore[guid] = new LFGGroupData();
                var data = GroupsStore[guid];
                data.SetState(state);
            }
            else
            {
                var data = PlayersStore[guid];
                data.SetState(state);
            }
        }

        private void SetVoteKick(ObjectGuid gguid, bool active)
        {
            Cypher.Assert(gguid.IsParty());

            var data = GroupsStore[gguid];
            Log.outInfo(LogFilter.Lfg, "Group: {0}, New state: {1}, Previous: {2}", gguid.ToString(), active, data.IsVoteKickActive());

            data.SetVoteKick(active);
        }

        private void SetDungeon(ObjectGuid guid, uint dungeon)
        {
            AddPlayerData(guid);
            Log.outDebug(LogFilter.Lfg, "SetDungeon: [{0}] dungeon {1}", guid, dungeon);
            GroupsStore[guid].SetDungeon(dungeon);
        }

        private void SetRoles(ObjectGuid guid, LfgRoles roles)
        {
            AddPlayerData(guid);
            Log.outDebug(LogFilter.Lfg, "SetRoles: [{0}] roles: {1}", guid, roles);
            PlayersStore[guid].SetRoles(roles);
        }

        public void SetSelectedDungeons(ObjectGuid guid, List<uint> dungeons)
        {
            AddPlayerData(guid);
            Log.outDebug(LogFilter.Lfg, "SetSelectedDungeons: [{0}] Dungeons: {1}", guid, ConcatenateDungeons(dungeons));
            PlayersStore[guid].SetSelectedDungeons(dungeons);
        }

        private void DecreaseKicksLeft(ObjectGuid guid)
        {
            Log.outDebug(LogFilter.Lfg, "DecreaseKicksLeft: [{0}]", guid);
            GroupsStore[guid].DecreaseKicksLeft();
        }

        private void AddPlayerData(ObjectGuid guid)
        {
            if (PlayersStore.ContainsKey(guid))
                return;

            PlayersStore[guid] = new LFGPlayerData();
        }

        private void SetTicket(ObjectGuid guid, RideTicket ticket)
        {
            PlayersStore[guid].SetTicket(ticket);
        }

        private void RemovePlayerData(ObjectGuid guid)
        {
            Log.outDebug(LogFilter.Lfg, "RemovePlayerData: [{0}]", guid);
            PlayersStore.Remove(guid);
        }

        public void RemoveGroupData(ObjectGuid guid)
        {
            Log.outDebug(LogFilter.Lfg, "RemoveGroupData: [{0}]", guid);
            var it = GroupsStore.LookupByKey(guid);
            if (it == null)
                return;

            var state = GetState(guid);
            // If group is being formed after proposal success do nothing more
            var players = it.GetPlayers();
            foreach (var _guid in players)
            {
                SetGroup(_guid, ObjectGuid.Empty);
                if (state != LfgState.Proposal)
                {
                    SetState(_guid, LfgState.None);
                    SendLfgUpdateStatus(_guid, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), true);
                }
            }
            GroupsStore.Remove(guid);
        }

        private Team GetTeam(ObjectGuid guid)
        {
            return PlayersStore[guid].GetTeam();
        }

        public byte RemovePlayerFromGroup(ObjectGuid gguid, ObjectGuid guid)
        {
            return GroupsStore[gguid].RemovePlayer(guid);
        }

        public void AddPlayerToGroup(ObjectGuid gguid, ObjectGuid guid)
        {
            if (!GroupsStore.ContainsKey(gguid))
                GroupsStore[gguid] = new LFGGroupData();

            GroupsStore[gguid].AddPlayer(guid);
        }

        public void SetLeader(ObjectGuid gguid, ObjectGuid leader)
        {
            if (!GroupsStore.ContainsKey(gguid))
                GroupsStore[gguid] = new LFGGroupData();

            GroupsStore[gguid].SetLeader(leader);
        }

        public void SetTeam(ObjectGuid guid, Team team)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup))
                team = 0;

            PlayersStore[guid].SetTeam(team);
        }

        public ObjectGuid GetGroup(ObjectGuid guid)
        {
            AddPlayerData(guid);
            return PlayersStore[guid].GetGroup();
        }

        public void SetGroup(ObjectGuid guid, ObjectGuid group)
        {
            AddPlayerData(guid);
            PlayersStore[guid].SetGroup(group);
        }

        private List<ObjectGuid> GetPlayers(ObjectGuid guid)
        {
            return GroupsStore[guid].GetPlayers();
        }

        public byte GetPlayerCount(ObjectGuid guid)
        {
            return GroupsStore[guid].GetPlayerCount();
        }

        public ObjectGuid GetLeader(ObjectGuid guid)
        {
            return GroupsStore[guid].GetLeader();
        }

        public bool HasIgnore(ObjectGuid guid1, ObjectGuid guid2)
        {
            var plr1 = Global.ObjAccessor.FindPlayer(guid1);
            var plr2 = Global.ObjAccessor.FindPlayer(guid2);
            return plr1 && plr2 && (plr1.GetSocial().HasIgnore(guid2) || plr2.GetSocial().HasIgnore(guid1));
        }

        public void SendLfgRoleChosen(ObjectGuid guid, ObjectGuid pguid, LfgRoles roles)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
                player.GetSession().SendLfgRoleChosen(pguid, roles);
        }

        public void SendLfgRoleCheckUpdate(ObjectGuid guid, LfgRoleCheck roleCheck)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
                player.GetSession().SendLfgRoleCheckUpdate(roleCheck);
        }

        public void SendLfgUpdateStatus(ObjectGuid guid, LfgUpdateData data, bool party)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
                player.GetSession().SendLfgUpdateStatus(data, party);
        }

        public void SendLfgJoinResult(ObjectGuid guid, LfgJoinResultData data)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
                player.GetSession().SendLfgJoinResult(data);
        }

        public void SendLfgBootProposalUpdate(ObjectGuid guid, LfgPlayerBoot boot)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
                player.GetSession().SendLfgBootProposalUpdate(boot);
        }

        public void SendLfgUpdateProposal(ObjectGuid guid, LfgProposal proposal)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
                player.GetSession().SendLfgProposalUpdate(proposal);
        }

        public void SendLfgQueueStatus(ObjectGuid guid, LfgQueueStatusData data)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);
            if (player)
                player.GetSession().SendLfgQueueStatus(data);
        }

        public bool IsLfgGroup(ObjectGuid guid)
        {
            return !guid.IsEmpty() && guid.IsParty() && GroupsStore[guid].IsLfgGroup();
        }

        public byte GetQueueId(ObjectGuid guid)
        {
            if (guid.IsParty())
            {
                var players = GetPlayers(guid);
                var pguid = players.Empty() ? ObjectGuid.Empty : players.First();
                if (!pguid.IsEmpty())
                    return (byte)GetTeam(pguid);
            }

            return (byte)GetTeam(guid);
        }

        public LFGQueue GetQueue(ObjectGuid guid)
        {
            var queueId = GetQueueId(guid);
            if (!QueuesStore.ContainsKey(queueId))
                QueuesStore[queueId] = new LFGQueue();

            return QueuesStore[queueId];
        }

        public bool AllQueued(List<ObjectGuid> check)
        {
            if (check.Empty())
                return false;

            foreach (var guid in check)
            {
                var state = GetState(guid);
                if (state != LfgState.Queued)
                {
                    if (state != LfgState.Proposal)
                        Log.outDebug(LogFilter.Lfg, "Unexpected state found while trying to form new group. Guid: {0}, State: {1}", guid.ToString(), state);

                    return false;
                }
            }
            return true;
        }

        public long GetQueueJoinTime(ObjectGuid guid)
        {
            var queueId = GetQueueId(guid);
            var lfgQueue = QueuesStore.LookupByKey(queueId);
            if (lfgQueue != null)
                return lfgQueue.GetJoinTime(guid);

            return 0;
        }

        // Only for debugging purposes
        public void Clean()
        {
            QueuesStore.Clear();
        }

        public bool IsOptionEnabled(LfgOptions option)
        {
            return m_options.HasAnyFlag(option);
        }

        public LfgOptions GetOptions()
        {
            return m_options;
        }

        public void SetOptions(LfgOptions options)
        {
            m_options = options;
        }

        public LfgUpdateData GetLfgStatus(ObjectGuid guid)
        {
            var playerData = PlayersStore[guid];
            return new LfgUpdateData(LfgUpdateType.UpdateStatus, playerData.GetState(), playerData.GetSelectedDungeons());
        }

        private bool IsSeasonActive(uint dungeonId)
        {
            switch (dungeonId)
            {
                case 285: // The Headless Horseman
                    return Global.GameEventMgr.IsHolidayActive(HolidayIds.HallowsEnd);
                case 286: // The Frost Lord Ahune
                    return Global.GameEventMgr.IsHolidayActive(HolidayIds.MidsummerFireFestival);
                case 287: // Coren Direbrew
                    return Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest);
                case 288: // The Crown Chemical Co.
                    return Global.GameEventMgr.IsHolidayActive(HolidayIds.LoveIsInTheAir);
                case 744: // Random Timewalking Dungeon (Burning Crusade)
                    return Global.GameEventMgr.IsHolidayActive(HolidayIds.TimewalkingDungeonEventBcDefault);
                case 995: // Random Timewalking Dungeon (Wrath of the Lich King)
                    return Global.GameEventMgr.IsHolidayActive(HolidayIds.TimewalkingDungeonEventLkDefault);
                case 1146: // Random Timewalking Dungeon (Cataclysm)
                    return Global.GameEventMgr.IsHolidayActive(HolidayIds.TimewalkingDungeonEventCataDefault);
            }
            return false;
        }

        public string DumpQueueInfo(bool full)
        {

            var size = (uint)QueuesStore.Count;

            var str = "Number of Queues: " + size + "\n";
            foreach (var pair in QueuesStore)
            {
                var queued = pair.Value.DumpQueueInfo();
                var compatibles = pair.Value.DumpCompatibleInfo(full);
                str += queued + compatibles;
            }

            return str;
        }

        public void SetupGroupMember(ObjectGuid guid, ObjectGuid gguid)
        {
            var dungeons = new List<uint>();
            dungeons.Add(GetDungeon(gguid));
            SetSelectedDungeons(guid, dungeons);
            SetState(guid, GetState(gguid));
            SetGroup(guid, gguid);
            AddPlayerToGroup(gguid, guid);
        }

        public bool SelectedRandomLfgDungeon(ObjectGuid guid)
        {
            if (GetState(guid) != LfgState.None)
            {
                var dungeons = GetSelectedDungeons(guid);
                if (!dungeons.Empty())
                {
                    var dungeon = GetLFGDungeon(dungeons.First());
                    if (dungeon != null && (dungeon.type == LfgType.RandomDungeon || dungeon.seasonal))
                        return true;
                }
            }

            return false;
        }

        public bool InLfgDungeonMap(ObjectGuid guid, uint map, Difficulty difficulty)
        {
            if (!guid.IsParty())
                guid = GetGroup(guid);

            var dungeonId = GetDungeon(guid, true);
            if (dungeonId != 0)
            {
                var dungeon = GetLFGDungeon(dungeonId);
                if (dungeon != null)
                    if (dungeon.map == map && dungeon.difficulty == difficulty)
                        return true;
            }

            return false;
        }

        public uint GetLFGDungeonEntry(uint id)
        {
            if (id != 0)
            {
                var dungeon = GetLFGDungeon(id);
                if (dungeon != null)
                    return dungeon.Entry();
            }

            return 0;
        }

        public List<uint> GetRandomAndSeasonalDungeons(uint level, uint expansion, uint contentTuningReplacementConditionMask)
        {
            var randomDungeons = new List<uint>();
            foreach (var dungeon in LfgDungeonStore.Values)
            {
                if (!(dungeon.type == LfgType.RandomDungeon || (dungeon.seasonal && Global.LFGMgr.IsSeasonActive(dungeon.id))))
                    continue;

                if (dungeon.expansion > expansion)
                    continue;

                var levels = Global.DB2Mgr.GetContentTuningData(dungeon.contentTuningId, contentTuningReplacementConditionMask);
                if (levels.HasValue)
                    if (levels.Value.MinLevel > level || level > levels.Value.MaxLevel)
                        continue;

                randomDungeons.Add(dungeon.Entry());
            }
            return randomDungeons;
        }

        // General variables
        private uint m_QueueTimer;     //< used to check interval of update
        private uint m_lfgProposalId;  //< used as internal counter for proposals
        private LfgOptions m_options;        //< Stores config options

        private Dictionary<byte, LFGQueue> QueuesStore = new Dictionary<byte, LFGQueue>();                     //< Queues

        private MultiMap<byte, uint> CachedDungeonMapStore = new MultiMap<byte, uint>(); //< Stores all dungeons by groupType
        // Reward System
        private MultiMap<uint, LfgReward> RewardMapStore = new MultiMap<uint, LfgReward>();                    //< Stores rewards for random dungeons

        private Dictionary<uint, LFGDungeonData> LfgDungeonStore = new Dictionary<uint, LFGDungeonData>();
        // Rolecheck - Proposal - Vote Kicks
        private Dictionary<ObjectGuid, LfgRoleCheck> RoleChecksStore = new Dictionary<ObjectGuid, LfgRoleCheck>();       //< Current Role checks
        private Dictionary<uint, LfgProposal> ProposalsStore = new Dictionary<uint, LfgProposal>();            //< Current Proposals
        private Dictionary<ObjectGuid, LfgPlayerBoot> BootsStore = new Dictionary<ObjectGuid, LfgPlayerBoot>();          //< Current player kicks
        private Dictionary<ObjectGuid, LFGPlayerData> PlayersStore = new Dictionary<ObjectGuid, LFGPlayerData>();        //< Player data
        private Dictionary<ObjectGuid, LFGGroupData> GroupsStore = new Dictionary<ObjectGuid, LFGGroupData>();           //< Group data
    }

    public class LfgJoinResultData
    {
        public LfgJoinResultData(LfgJoinResult _result = LfgJoinResult.Ok, LfgRoleCheckState _state = LfgRoleCheckState.Default)
        {
            result = _result;
            state = _state;
        }

        public LfgJoinResult result;
        public LfgRoleCheckState state;
        public Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>> lockmap = new Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>>();
        public List<string> playersMissingRequirement = new List<string>();
    }

    public class LfgUpdateData
    {
        public LfgUpdateData(LfgUpdateType _type = LfgUpdateType.Default)
        {
            updateType = _type;
            state = LfgState.None;
        }
        public LfgUpdateData(LfgUpdateType _type, List<uint> _dungeons)
        {
            updateType = _type;
            state = LfgState.None;
            dungeons = _dungeons;
        }
        public LfgUpdateData(LfgUpdateType _type, LfgState _state, List<uint> _dungeons)
        {
            updateType = _type;
            state = _state;
            dungeons = _dungeons;
        }

        public LfgUpdateType updateType;
        public LfgState state;
        public List<uint> dungeons = new List<uint>();
    }

    public class LfgQueueStatusData
    {
        public LfgQueueStatusData(byte _queueId = 0, uint _dungeonId = 0, int _waitTime = -1, int _waitTimeAvg = -1, int _waitTimeTank = -1, int _waitTimeHealer = -1,
            int _waitTimeDps = -1, uint _queuedTime = 0, byte _tanks = 0, byte _healers = 0, byte _dps = 0)
        {
            queueId = _queueId;
            dungeonId = _dungeonId;
            waitTime = _waitTime;
            waitTimeAvg = _waitTimeAvg;
            waitTimeTank = _waitTimeTank;
            waitTimeHealer = _waitTimeHealer;
            waitTimeDps = _waitTimeDps;
            queuedTime = _queuedTime;
            tanks = _tanks;
            healers = _healers;
            dps = _dps;
        }

        public byte queueId;
        public uint dungeonId;
        public int waitTime;
        public int waitTimeAvg;
        public int waitTimeTank;
        public int waitTimeHealer;
        public int waitTimeDps;
        public uint queuedTime;
        public byte tanks;
        public byte healers;
        public byte dps;
    }

    public class LfgPlayerRewardData
    {
        public LfgPlayerRewardData(uint random, uint current, bool _done, Quest _quest)
        {
            rdungeonEntry = random;
            sdungeonEntry = current;
            done = _done;
            quest = _quest;
        }

        public uint rdungeonEntry;
        public uint sdungeonEntry;
        public bool done;
        public Quest quest;
    }

    public class LfgReward
    {
        public LfgReward(uint _maxLevel = 0, uint _firstQuest = 0, uint _otherQuest = 0)
        {
            maxLevel = _maxLevel;
            firstQuest = _firstQuest;
            otherQuest = _otherQuest;
        }

        public uint maxLevel;
        public uint firstQuest;
        public uint otherQuest;
    }

    public class LfgProposalPlayer
    {
        public LfgProposalPlayer()
        {
            role = 0;
            accept = LfgAnswer.Pending;
            group = ObjectGuid.Empty;
        }

        public LfgRoles role;
        public LfgAnswer accept;
        public ObjectGuid group;
    }

    public class LfgProposal
    {
        public LfgProposal(uint dungeon = 0)
        {
            id = 0;
            dungeonId = dungeon;
            state = LfgProposalState.Initiating;
            group = ObjectGuid.Empty;
            leader = ObjectGuid.Empty;
            cancelTime = 0;
            encounters = 0;
            isNew = true;
        }

        public uint id;
        public uint dungeonId;
        public LfgProposalState state;
        public ObjectGuid group;
        public ObjectGuid leader;
        public long cancelTime;
        public uint encounters;
        public bool isNew;
        public List<ObjectGuid> queues = new List<ObjectGuid>();
        public List<ulong> showorder = new List<ulong>();
        public Dictionary<ObjectGuid, LfgProposalPlayer> players = new Dictionary<ObjectGuid, LfgProposalPlayer>();                  // Players data
    }

    public class LfgRoleCheck
    {
        public long cancelTime;
        public Dictionary<ObjectGuid, LfgRoles> roles = new Dictionary<ObjectGuid, LfgRoles>();
        public LfgRoleCheckState state;
        public List<uint> dungeons = new List<uint>();
        public uint rDungeonId;
        public ObjectGuid leader;
    }

    public class LfgPlayerBoot
    {
        public long cancelTime;
        public bool inProgress;
        public Dictionary<ObjectGuid, LfgAnswer> votes = new Dictionary<ObjectGuid, LfgAnswer>();
        public ObjectGuid victim;
        public string reason;
    }

    public class LFGDungeonData
    {
        public LFGDungeonData(LFGDungeonsRecord dbc)
        {
            id = dbc.Id;
            name = dbc.Name[Global.WorldMgr.GetDefaultDbcLocale()];
            map = (uint)dbc.MapID;
            type = dbc.TypeID;
            expansion = dbc.ExpansionLevel;
            group = dbc.GroupID;
            contentTuningId = dbc.ContentTuningID;
            difficulty = dbc.DifficultyID;
            seasonal = dbc.Flags[0].HasAnyFlag(LfgFlags.Seasonal);
        }

        public uint id;
        public string name;
        public uint map;
        public LfgType type;
        public uint expansion;
        public uint group;
        public uint contentTuningId;
        public Difficulty difficulty;
        public bool seasonal;
        public float x, y, z, o;
        public ushort requiredItemLevel;

        // Helpers
        public uint Entry() { return (uint)(id + ((int)type << 24)); }
    }

    public class LfgLockInfoData
    {
        public LfgLockInfoData(LfgLockStatusType _lockStatus = 0, ushort _requiredItemLevel = 0, float _currentItemLevel = 0)
        {
            lockStatus = _lockStatus;
            requiredItemLevel = _requiredItemLevel;
            currentItemLevel = _currentItemLevel;
        }

        public LfgLockStatusType lockStatus;
        public ushort requiredItemLevel;
        public float currentItemLevel;
    }
}