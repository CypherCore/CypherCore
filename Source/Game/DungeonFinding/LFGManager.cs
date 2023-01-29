// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.DungeonFinding
{
    public class LFGManager : Singleton<LFGManager>
    {
        private uint _lfgProposalId; //< used as internal counter for proposals
        private LfgOptions _options; //< Stores config options

        // General variables
        private uint _queueTimer;                                         //< used to check interval of update
        private readonly Dictionary<ObjectGuid, LfgPlayerBoot> _bootsStore = new(); //< Current player kicks
        private readonly MultiMap<byte, uint> _cachedDungeonMapStore = new();       //< Stores all dungeons by groupType
        private readonly Dictionary<ObjectGuid, LFGGroupData> _groupsStore = new(); //< Group _data
        private readonly Dictionary<uint, LFGDungeonData> _lfgDungeonStore = new();
        private readonly Dictionary<ObjectGuid, LFGPlayerData> _playersStore = new(); //< Player _data
        private readonly Dictionary<uint, LfgProposal> _proposalsStore = new();       //< Current Proposals

        private readonly Dictionary<byte, LFGQueue> _queuesStore = new(); //< Queues

        // Reward System
        private readonly MultiMap<uint, LfgReward> _rewardMapStore = new(); //< Stores rewards for random dungeons

        // Rolecheck - Proposal - Vote Kicks
        private readonly Dictionary<ObjectGuid, LfgRoleCheck> _roleChecksStore = new(); //< Current Role checks

        private LFGManager()
        {
            _lfgProposalId = 1;
            _options = (LfgOptions)ConfigMgr.GetDefaultValue("DungeonFinder.OptionsMask", 1);

            new LFGPlayerScript();
            new LFGGroupScript();
        }

        public string ConcatenateDungeons(List<uint> dungeons)
        {
            StringBuilder dungeonstr = new();

            if (!dungeons.Empty())
                foreach (var id in dungeons)
                    if (dungeonstr.Capacity != 0)
                        dungeonstr.AppendFormat(", {0}", id);
                    else
                        dungeonstr.AppendFormat("{0}", id);

            return dungeonstr.ToString();
        }

        public void _LoadFromDB(SQLFields field, ObjectGuid guid)
        {
            if (field == null)
                return;

            if (!guid.IsParty())
                return;

            SetLeader(guid, ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(0)));

            uint dungeon = field.Read<uint>(18);
            LfgState state = (LfgState)field.Read<byte>(19);

            if (dungeon == 0 ||
                state == 0)
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

            SQLTransaction trans = new();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_LFG_DATA);
            stmt.AddValue(0, db_guid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_LFG_DATA);
            stmt.AddValue(0, db_guid);
            stmt.AddValue(1, GetDungeon(guid));
            stmt.AddValue(2, (uint)GetState(guid));
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public void LoadRewards()
        {
            uint oldMSTime = Time.GetMSTime();

            _rewardMapStore.Clear();

            // ORDER BY is very important for GetRandomDungeonReward!
            SQLResult result = DB.World.Query("SELECT dungeonId, maxLevel, firstQuestId, otherQuestId FROM lfg_dungeon_rewards ORDER BY dungeonId, maxLevel ASC");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 lfg dungeon rewards. DB table `lfg_dungeon_rewards` is empty!");

                return;
            }

            uint count = 0;

            do
            {
                uint dungeonId = result.Read<uint>(0);
                uint maxLevel = result.Read<byte>(1);
                uint firstQuestId = result.Read<uint>(2);
                uint otherQuestId = result.Read<uint>(3);

                if (GetLFGDungeonEntry(dungeonId) == 0)
                {
                    Log.outError(LogFilter.Sql, "Dungeon {0} specified in table `lfg_dungeon_rewards` does not exist!", dungeonId);

                    continue;
                }

                if (maxLevel == 0 ||
                    maxLevel > WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                {
                    Log.outError(LogFilter.Sql, "Level {0} specified for dungeon {1} in table `lfg_dungeon_rewards` can never be reached!", maxLevel, dungeonId);
                    maxLevel = WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel);
                }

                if (firstQuestId == 0 ||
                    Global.ObjectMgr.GetQuestTemplate(firstQuestId) == null)
                {
                    Log.outError(LogFilter.Sql, "First quest {0} specified for dungeon {1} in table `lfg_dungeon_rewards` does not exist!", firstQuestId, dungeonId);

                    continue;
                }

                if (otherQuestId != 0 &&
                    Global.ObjectMgr.GetQuestTemplate(otherQuestId) == null)
                {
                    Log.outError(LogFilter.Sql, "Other quest {0} specified for dungeon {1} in table `lfg_dungeon_rewards` does not exist!", otherQuestId, dungeonId);
                    otherQuestId = 0;
                }

                _rewardMapStore.Add(dungeonId, new LfgReward(maxLevel, firstQuestId, otherQuestId));
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} lfg dungeon rewards in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        private LFGDungeonData GetLFGDungeon(uint id)
        {
            return _lfgDungeonStore.LookupByKey(id);
        }

        public void LoadLFGDungeons(bool reload = false)
        {
            uint oldMSTime = Time.GetMSTime();

            _lfgDungeonStore.Clear();

            // Initialize Dungeon map with _data from dbcs
            foreach (var dungeon in CliDB.LFGDungeonsStorage.Values)
            {
                if (Global.DB2Mgr.GetMapDifficultyData((uint)dungeon.MapID, dungeon.DifficultyID) == null)
                    continue;

                switch (dungeon.TypeID)
                {
                    case LfgType.Dungeon:
                    case LfgType.Raid:
                    case LfgType.Random:
                    case LfgType.Zone:
                        _lfgDungeonStore[dungeon.Id] = new LFGDungeonData(dungeon);

                        break;
                }
            }

            // Fill teleport locations from DB
            SQLResult result = DB.World.Query("SELECT dungeonId, position_x, position_y, position_z, orientation, requiredItemLevel FROM lfg_dungeon_template");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 lfg dungeon templates. DB table `lfg_dungeon_template` is empty!");

                return;
            }

            uint count = 0;

            do
            {
                uint dungeonId = result.Read<uint>(0);

                if (!_lfgDungeonStore.ContainsKey(dungeonId))
                {
                    Log.outError(LogFilter.Sql, "table `lfg_entrances` contains coordinates for wrong dungeon {0}", dungeonId);

                    continue;
                }

                var data = _lfgDungeonStore[dungeonId];
                data.X = result.Read<float>(1);
                data.Y = result.Read<float>(2);
                data.Z = result.Read<float>(3);
                data.O = result.Read<float>(4);
                data.RequiredItemLevel = result.Read<ushort>(5);

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} lfg dungeon templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

            // Fill all other teleport coords from areatriggers
            foreach (var pair in _lfgDungeonStore)
            {
                LFGDungeonData dungeon = pair.Value;

                // No teleport coords in database, load from areatriggers
                if (dungeon.Type != LfgType.Random &&
                    dungeon.X == 0.0f &&
                    dungeon.Y == 0.0f &&
                    dungeon.Z == 0.0f)
                {
                    AreaTriggerStruct at = Global.ObjectMgr.GetMapEntranceTrigger(dungeon.Map);

                    if (at == null)
                    {
                        Log.outError(LogFilter.Lfg, "LoadLFGDungeons: Failed to load dungeon {0} (Id: {1}), cant find areatrigger for map {2}", dungeon.Name, dungeon.Id, dungeon.Map);

                        continue;
                    }

                    dungeon.Map = at.target_mapId;
                    dungeon.X = at.target_X;
                    dungeon.Y = at.target_Y;
                    dungeon.Z = at.target_Z;
                    dungeon.O = at.target_Orientation;
                }

                if (dungeon.Type != LfgType.Random)
                    _cachedDungeonMapStore.Add((byte)dungeon.Group, dungeon.Id);

                _cachedDungeonMapStore.Add(0, dungeon.Id);
            }

            if (reload)
                _cachedDungeonMapStore.Clear();
        }

        public void Update(uint diff)
        {
            if (!IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
                return;

            long currTime = GameTime.GetGameTime();

            // Remove obsolete role checks
            foreach (var pairCheck in _roleChecksStore)
            {
                LfgRoleCheck roleCheck = pairCheck.Value;

                if (currTime < roleCheck.CancelTime)
                    continue;

                roleCheck.State = LfgRoleCheckState.MissingRole;

                foreach (var pairRole in roleCheck.Roles)
                {
                    ObjectGuid guid = pairRole.Key;
                    RestoreState(guid, "Remove Obsolete RoleCheck");
                    SendLfgRoleCheckUpdate(guid, roleCheck);

                    if (guid == roleCheck.Leader)
                        SendLfgJoinResult(guid, new LfgJoinResultData(LfgJoinResult.RoleCheckFailed, LfgRoleCheckState.MissingRole));
                }

                RestoreState(pairCheck.Key, "Remove Obsolete RoleCheck");
                _roleChecksStore.Remove(pairCheck.Key);
            }

            // Remove obsolete proposals
            foreach (var removePair in _proposalsStore.ToList())
                if (removePair.Value.CancelTime < currTime)
                    RemoveProposal(removePair, LfgUpdateType.ProposalFailed);

            // Remove obsolete kicks
            foreach (var itBoot in _bootsStore)
            {
                LfgPlayerBoot boot = itBoot.Value;

                if (boot.CancelTime < currTime)
                {
                    boot.InProgress = false;

                    foreach (var itVotes in boot.Votes)
                    {
                        ObjectGuid pguid = itVotes.Key;

                        if (pguid != boot.Victim)
                            SendLfgBootProposalUpdate(pguid, boot);
                    }

                    SetVoteKick(itBoot.Key, false);
                    _bootsStore.Remove(itBoot.Key);
                }
            }

            uint lastProposalId = _lfgProposalId;

            // Check if a proposal can be formed with the new groups being added
            foreach (var it in _queuesStore)
            {
                byte newProposals = it.Value.FindGroups();

                if (newProposals != 0)
                    Log.outDebug(LogFilter.Lfg, "Update: Found {0} new groups in queue {1}", newProposals, it.Key);
            }

            if (lastProposalId != _lfgProposalId)
                // FIXME lastProposalId ? lastProposalId +1 ?
                foreach (var itProposal in _proposalsStore.SkipWhile(p => p.Key == _lfgProposalId))
                {
                    uint proposalId = itProposal.Key;
                    LfgProposal proposal = _proposalsStore[proposalId];

                    ObjectGuid guid = ObjectGuid.Empty;

                    foreach (var itPlayers in proposal.Players)
                    {
                        guid = itPlayers.Key;
                        SetState(guid, LfgState.Proposal);
                        ObjectGuid gguid = GetGroup(guid);

                        if (!gguid.IsEmpty())
                        {
                            SetState(gguid, LfgState.Proposal);
                            SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.ProposalBegin, GetSelectedDungeons(guid)), true);
                        }
                        else
                        {
                            SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.ProposalBegin, GetSelectedDungeons(guid)), false);
                        }

                        SendLfgUpdateProposal(guid, proposal);
                    }

                    if (proposal.State == LfgProposalState.Success)
                        UpdateProposal(proposalId, guid, true);
                }

            // Update all players status queue info
            if (_queueTimer > SharedConst.LFGQueueUpdateInterval)
            {
                _queueTimer = 0;

                foreach (var it in _queuesStore)
                    it.Value.UpdateQueueTimers(it.Key, currTime);
            }
            else
            {
                _queueTimer += diff;
            }
        }

        public void JoinLfg(Player player, LfgRoles roles, List<uint> dungeons)
        {
            if (!player ||
                player.GetSession() == null ||
                dungeons.Empty())
                return;

            // Sanitize input roles
            roles &= LfgRoles.Any;
            roles = FilterClassRoles(player, roles);

            // At least 1 role must be selected
            if ((roles & (LfgRoles.Tank | LfgRoles.Healer | LfgRoles.Damage)) == 0)
                return;

            Group grp = player.GetGroup();
            ObjectGuid guid = player.GetGUID();
            ObjectGuid gguid = grp ? grp.GetGUID() : guid;
            LfgJoinResultData joinData = new();
            List<ObjectGuid> players = new();
            uint rDungeonId = 0;
            bool isContinue = grp && grp.IsLFGGroup() && GetState(gguid) != LfgState.FinishedDungeon;

            // Do not allow to change dungeon in the middle of a current dungeon
            if (isContinue)
            {
                dungeons.Clear();
                dungeons.Add(GetDungeon(gguid));
            }

            // Already in queue?
            LfgState state = GetState(gguid);

            if (state == LfgState.Queued)
            {
                LFGQueue queue = GetQueue(gguid);
                queue.RemoveFromQueue(gguid);
            }

            // Check player or group member restrictions
            if (!player.GetSession().HasPermission(RBACPermissions.JoinDungeonFinder))
            {
                joinData.Result = LfgJoinResult.NoSlots;
            }
            else if (player.InBattleground() ||
                     player.InArena() ||
                     player.InBattlegroundQueue())
            {
                joinData.Result = LfgJoinResult.CantUseDungeons;
            }
            else if (player.HasAura(SharedConst.LFGSpellDungeonDeserter))
            {
                joinData.Result = LfgJoinResult.DeserterPlayer;
            }
            else if (!isContinue &&
                     player.HasAura(SharedConst.LFGSpellDungeonCooldown))
            {
                joinData.Result = LfgJoinResult.RandomCooldownPlayer;
            }
            else if (dungeons.Empty())
            {
                joinData.Result = LfgJoinResult.NoSlots;
            }
            else if (player.HasAura(9454)) // check Freeze debuff
            {
                joinData.Result = LfgJoinResult.NoSlots;
            }
            else if (grp)
            {
                if (grp.GetMembersCount() > MapConst.MaxGroupSize)
                {
                    joinData.Result = LfgJoinResult.TooManyMembers;
                }
                else
                {
                    byte memberCount = 0;

                    for (GroupReference refe = grp.GetFirstMember(); refe != null && joinData.Result == LfgJoinResult.Ok; refe = refe.Next())
                    {
                        Player plrg = refe.GetSource();

                        if (plrg)
                        {
                            if (!plrg.GetSession().HasPermission(RBACPermissions.JoinDungeonFinder))
                                joinData.Result = LfgJoinResult.NoLfgObject;

                            if (plrg.HasAura(SharedConst.LFGSpellDungeonDeserter))
                            {
                                joinData.Result = LfgJoinResult.DeserterParty;
                            }
                            else if (!isContinue &&
                                     plrg.HasAura(SharedConst.LFGSpellDungeonCooldown))
                            {
                                joinData.Result = LfgJoinResult.RandomCooldownParty;
                            }
                            else if (plrg.InBattleground() ||
                                     plrg.InArena() ||
                                     plrg.InBattlegroundQueue())
                            {
                                joinData.Result = LfgJoinResult.CantUseDungeons;
                            }
                            else if (plrg.HasAura(9454)) // check Freeze debuff
                            {
                                joinData.Result = LfgJoinResult.NoSlots;
                                joinData.PlayersMissingRequirement.Add(plrg.GetName());
                            }

                            ++memberCount;
                            players.Add(plrg.GetGUID());
                        }
                    }

                    if (joinData.Result == LfgJoinResult.Ok &&
                        memberCount != grp.GetMembersCount())
                        joinData.Result = LfgJoinResult.MembersNotPresent;
                }
            }
            else
            {
                players.Add(player.GetGUID());
            }

            // Check if all dungeons are valid
            bool isRaid = false;

            if (joinData.Result == LfgJoinResult.Ok)
            {
                bool isDungeon = false;

                foreach (var it in dungeons)
                {
                    if (joinData.Result != LfgJoinResult.Ok)
                        break;

                    LfgType type = GetDungeonType(it);

                    switch (type)
                    {
                        case LfgType.Random:
                            if (dungeons.Count > 1) // Only allow 1 random dungeon
                                joinData.Result = LfgJoinResult.InvalidSlot;
                            else
                                rDungeonId = dungeons.First();

                            goto case LfgType.Dungeon;
                        case LfgType.Dungeon:
                            if (isRaid)
                                joinData.Result = LfgJoinResult.MismatchedSlots;

                            isDungeon = true;

                            break;
                        case LfgType.Raid:
                            if (isDungeon)
                                joinData.Result = LfgJoinResult.MismatchedSlots;

                            isRaid = true;

                            break;
                        default:
                            Log.outError(LogFilter.Lfg, "Wrong dungeon Type {0} for dungeon {1}", type, it);
                            joinData.Result = LfgJoinResult.InvalidSlot;

                            break;
                    }
                }

                // it could be changed
                if (joinData.Result == LfgJoinResult.Ok)
                {
                    // Expand random dungeons and check restrictions
                    if (rDungeonId != 0)
                        dungeons = GetDungeonsByRandom(rDungeonId);

                    // if we have lockmap then there are no compatible dungeons
                    GetCompatibleDungeons(dungeons, players, joinData.Lockmap, joinData.PlayersMissingRequirement, isContinue);

                    if (dungeons.Empty())
                        joinData.Result = LfgJoinResult.NoSlots;
                }
            }

            // Can't join. Send result
            if (joinData.Result != LfgJoinResult.Ok)
            {
                Log.outDebug(LogFilter.Lfg, "Join: [{0}] joining with {1} members. result: {2}", guid, grp ? grp.GetMembersCount() : 1, joinData.Result);

                if (!dungeons.Empty()) // Only should show lockmap when have no dungeons available
                    joinData.Lockmap.Clear();

                player.GetSession().SendLfgJoinResult(joinData);

                return;
            }

            if (isRaid)
            {
                Log.outDebug(LogFilter.Lfg, "Join: [{0}] trying to join raid browser and it's disabled.", guid);

                return;
            }

            RideTicket ticket = new();
            ticket.RequesterGuid = guid;
            ticket.Id = GetQueueId(gguid);
            ticket.Type = RideType.Lfg;
            ticket.Time = GameTime.GetGameTime();

            string debugNames = "";

            if (grp) // Begin rolecheck
            {
                // Create new rolecheck
                LfgRoleCheck roleCheck = new();
                roleCheck.CancelTime = GameTime.GetGameTime() + SharedConst.LFGTimeRolecheck;
                roleCheck.State = LfgRoleCheckState.Initialiting;
                roleCheck.Leader = guid;
                roleCheck.Dungeons = dungeons;
                roleCheck.DungeonId = rDungeonId;

                _roleChecksStore[gguid] = roleCheck;

                if (rDungeonId != 0)
                {
                    dungeons.Clear();
                    dungeons.Add(rDungeonId);
                }

                SetState(gguid, LfgState.Rolecheck);
                // Send update to player
                LfgUpdateData updateData = new(LfgUpdateType.JoinQueue, dungeons);

                for (GroupReference refe = grp.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player plrg = refe.GetSource();

                    if (plrg)
                    {
                        ObjectGuid pguid = plrg.GetGUID();
                        plrg.GetSession().SendLfgUpdateStatus(updateData, true);
                        SetState(pguid, LfgState.Rolecheck);
                        SetTicket(pguid, ticket);

                        if (!isContinue)
                            SetSelectedDungeons(pguid, dungeons);

                        roleCheck.Roles[pguid] = 0;

                        if (!string.IsNullOrEmpty(debugNames))
                            debugNames += ", ";

                        debugNames += plrg.GetName();
                    }
                }

                // Update leader role
                UpdateRoleCheck(gguid, guid, roles);
            }
            else // Add player to queue
            {
                Dictionary<ObjectGuid, LfgRoles> rolesMap = new();
                rolesMap[guid] = roles;
                LFGQueue queue = GetQueue(guid);
                queue.AddQueueData(guid, GameTime.GetGameTime(), dungeons, rolesMap);

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

            StringBuilder o = new();
            o.AppendFormat("Join: [{0}] joined ({1}{2}) Members: {3}. Dungeons ({4}): ", guid, (grp ? "group" : "player"), debugNames, dungeons.Count, ConcatenateDungeons(dungeons));
            Log.outDebug(LogFilter.Lfg, o.ToString());
        }

        public void LeaveLfg(ObjectGuid guid, bool disconnected = false)
        {
            Log.outDebug(LogFilter.Lfg, "LeaveLfg: [{0}]", guid);

            ObjectGuid gguid = guid.IsParty() ? guid : GetGroup(guid);
            LfgState state = GetState(guid);

            switch (state)
            {
                case LfgState.Queued:
                    if (!gguid.IsEmpty())
                    {
                        LfgState newState = LfgState.None;
                        LfgState oldState = GetOldState(gguid);

                        // Set the new State to LFG_STATE_DUNGEON/LFG_STATE_FINISHED_DUNGEON if the group is already in a dungeon
                        // This is required in case a LFG group vote-kicks a player in a dungeon, queues, then leaves the queue (maybe to queue later again)
                        Group group = Global.GroupMgr.GetGroupByGUID(gguid);

                        if (group != null)
                            if (group.IsLFGGroup() &&
                                GetDungeon(gguid) != 0 &&
                                (oldState == LfgState.Dungeon || oldState == LfgState.FinishedDungeon))
                                newState = oldState;

                        LFGQueue queue = GetQueue(gguid);
                        queue.RemoveFromQueue(gguid);
                        SetState(gguid, newState);
                        List<ObjectGuid> players = GetPlayers(gguid);

                        foreach (var it in players)
                        {
                            SetState(it, newState);
                            SendLfgUpdateStatus(it, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), true);
                        }
                    }
                    else
                    {
                        SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), false);
                        LFGQueue queue = GetQueue(guid);
                        queue.RemoveFromQueue(guid);
                        SetState(guid, LfgState.None);
                    }

                    break;
                case LfgState.Rolecheck:
                    if (!gguid.IsEmpty())
                        UpdateRoleCheck(gguid); // No player to update role = LFG_ROLECHECK_ABORTED

                    break;
                case LfgState.Proposal:
                    {
                        // Remove from Proposals
                        KeyValuePair<uint, LfgProposal> it = new();
                        ObjectGuid pguid = gguid == guid ? GetLeader(gguid) : guid;

                        foreach (var test in _proposalsStore)
                        {
                            it = test;
                            var itPlayer = it.Value.Players.LookupByKey(pguid);

                            if (itPlayer != null)
                            {
                                // Mark the player/leader of group who left as didn't accept the proposal
                                itPlayer.Accept = LfgAnswer.Deny;

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
                    if (guid != gguid &&
                        !disconnected) // Player
                        SetState(guid, LfgState.None);

                    break;
            }
        }

        public RideTicket GetTicket(ObjectGuid guid)
        {
            var palyerData = _playersStore.LookupByKey(guid);

            if (palyerData != null)
                return palyerData.GetTicket();

            return null;
        }

        public void UpdateRoleCheck(ObjectGuid gguid, ObjectGuid guid = default, LfgRoles roles = LfgRoles.None)
        {
            if (gguid.IsEmpty())
                return;

            Dictionary<ObjectGuid, LfgRoles> check_roles;
            var roleCheck = _roleChecksStore.LookupByKey(gguid);

            if (roleCheck == null)
                return;

            // Sanitize input roles
            roles &= LfgRoles.Any;

            if (!guid.IsEmpty())
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);

                if (player != null)
                    roles = FilterClassRoles(player, roles);
                else
                    return;
            }

            bool sendRoleChosen = roleCheck.State != LfgRoleCheckState.Default && !guid.IsEmpty();

            if (guid.IsEmpty())
            {
                roleCheck.State = LfgRoleCheckState.Aborted;
            }
            else if (roles < LfgRoles.Tank) // Player selected no role.
            {
                roleCheck.State = LfgRoleCheckState.NoRole;
            }
            else
            {
                roleCheck.Roles[guid] = roles;

                // Check if all players have selected a role
                bool done = false;

                foreach (var rolePair in roleCheck.Roles)
                {
                    if (rolePair.Value != LfgRoles.None)
                        continue;

                    done = true;
                }

                if (done)
                {
                    // use temporal var to check roles, CheckGroupRoles modifies the roles
                    check_roles = roleCheck.Roles;
                    roleCheck.State = CheckGroupRoles(check_roles) ? LfgRoleCheckState.Finished : LfgRoleCheckState.WrongRoles;
                }
            }

            List<uint> dungeons = new();

            if (roleCheck.DungeonId != 0)
                dungeons.Add(roleCheck.DungeonId);
            else
                dungeons = roleCheck.Dungeons;

            LfgJoinResultData joinData = new(LfgJoinResult.RoleCheckFailed, roleCheck.State);

            foreach (var it in roleCheck.Roles)
            {
                ObjectGuid pguid = it.Key;

                if (sendRoleChosen)
                    SendLfgRoleChosen(pguid, guid, roles);

                SendLfgRoleCheckUpdate(pguid, roleCheck);

                switch (roleCheck.State)
                {
                    case LfgRoleCheckState.Initialiting:
                        continue;
                    case LfgRoleCheckState.Finished:
                        SetState(pguid, LfgState.Queued);
                        SetRoles(pguid, it.Value);
                        SendLfgUpdateStatus(pguid, new LfgUpdateData(LfgUpdateType.AddedToQueue, dungeons), true);

                        break;
                    default:
                        if (roleCheck.Leader == pguid)
                            SendLfgJoinResult(pguid, joinData);

                        SendLfgUpdateStatus(pguid, new LfgUpdateData(LfgUpdateType.RolecheckFailed), true);
                        RestoreState(pguid, "Rolecheck Failed");

                        break;
                }
            }

            if (roleCheck.State == LfgRoleCheckState.Finished)
            {
                SetState(gguid, LfgState.Queued);
                LFGQueue queue = GetQueue(gguid);
                queue.AddQueueData(gguid, GameTime.GetGameTime(), roleCheck.Dungeons, roleCheck.Roles);
                _roleChecksStore.Remove(gguid);
            }
            else if (roleCheck.State != LfgRoleCheckState.Initialiting)
            {
                RestoreState(gguid, "Rolecheck Failed");
                _roleChecksStore.Remove(gguid);
            }
        }

        private void GetCompatibleDungeons(List<uint> dungeons, List<ObjectGuid> players, Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>> lockMap, List<string> playersMissingRequirement, bool isContinue)
        {
            lockMap.Clear();
            Dictionary<uint, uint> lockedDungeons = new();
            List<uint> dungeonsToRemove = new();

            foreach (var guid in players)
            {
                if (dungeons.Empty())
                    break;

                var cachedLockMap = GetLockedDungeons(guid);
                Player player = Global.ObjAccessor.FindConnectedPlayer(guid);

                foreach (var it2 in cachedLockMap)
                {
                    if (dungeons.Empty())
                        break;

                    uint dungeonId = (it2.Key & 0x00FFFFFF); // Compare dungeon ids

                    if (dungeons.Contains(dungeonId))
                    {
                        bool eraseDungeon = true;

                        // Don't remove the dungeon if team members are trying to continue a locked instance
                        if (it2.Value.LockStatus == LfgLockStatusType.RaidLocked && isContinue)
                        {
                            LFGDungeonData dungeon = GetLFGDungeon(dungeonId);
                            Cypher.Assert(dungeon != null);
                            Cypher.Assert(player);
                            MapDb2Entries entries = new(dungeon.Map, dungeon.Difficulty);
                            InstanceLock playerBind = Global.InstanceLockMgr.FindActiveInstanceLock(guid, entries);

                            if (playerBind != null)
                            {
                                uint dungeonInstanceId = playerBind.GetInstanceId();

                                if (!lockedDungeons.TryGetValue(dungeonId, out uint lockedDungeon) ||
                                    lockedDungeon == dungeonInstanceId)
                                    eraseDungeon = false;

                                lockedDungeons[dungeonId] = dungeonInstanceId;
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

            foreach (uint dungeonIdToRemove in dungeonsToRemove)
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

            List<ObjectGuid> keys = new(groles.Keys);

            for (int i = 0; i < keys.Count; i++)
            {
                LfgRoles role = groles[keys[i]] & ~LfgRoles.Leader;

                if (role == LfgRoles.None)
                    return false;

                if (role.HasAnyFlag(LfgRoles.Damage))
                {
                    if (role != LfgRoles.Damage)
                    {
                        groles[keys[i]] -= LfgRoles.Damage;

                        if (CheckGroupRoles(groles))
                            return true;

                        groles[keys[i]] += (byte)LfgRoles.Damage;
                    }
                    else if (damage == SharedConst.LFGDPSNeeded)
                    {
                        return false;
                    }
                    else
                    {
                        damage++;
                    }
                }

                if (role.HasAnyFlag(LfgRoles.Healer))
                {
                    if (role != LfgRoles.Healer)
                    {
                        groles[keys[i]] -= LfgRoles.Healer;

                        if (CheckGroupRoles(groles))
                            return true;

                        groles[keys[i]] += (byte)LfgRoles.Healer;
                    }
                    else if (healer == SharedConst.LFGHealersNeeded)
                    {
                        return false;
                    }
                    else
                    {
                        healer++;
                    }
                }

                if (role.HasAnyFlag(LfgRoles.Tank))
                {
                    if (role != LfgRoles.Tank)
                    {
                        groles[keys[i]] -= LfgRoles.Tank;

                        if (CheckGroupRoles(groles))
                            return true;

                        groles[keys[i]] += (byte)LfgRoles.Tank;
                    }
                    else if (tank == SharedConst.LFGTanksNeeded)
                    {
                        return false;
                    }
                    else
                    {
                        tank++;
                    }
                }
            }

            return (tank + healer + damage) == (byte)groles.Count;
        }

        private void MakeNewGroup(LfgProposal proposal)
        {
            List<ObjectGuid> players = new();
            List<ObjectGuid> tankPlayers = new();
            List<ObjectGuid> healPlayers = new();
            List<ObjectGuid> dpsPlayers = new();
            List<ObjectGuid> playersToTeleport = new();

            foreach (var it in proposal.Players)
            {
                ObjectGuid guid = it.Key;

                if (guid == proposal.Leader)
                    players.Add(guid);
                else
                    switch (it.Value.Role & ~LfgRoles.Leader)
                    {
                        case LfgRoles.Tank:
                            tankPlayers.Add(guid);

                            break;
                        case LfgRoles.Healer:
                            healPlayers.Add(guid);

                            break;
                        case LfgRoles.Damage:
                            dpsPlayers.Add(guid);

                            break;
                        default:
                            Cypher.Assert(false, $"Invalid LFG role {it.Value.Role}");

                            break;
                    }

                if (proposal.IsNew ||
                    GetGroup(guid) != proposal.Group)
                    playersToTeleport.Add(guid);
            }

            players.AddRange(tankPlayers);
            players.AddRange(healPlayers);
            players.AddRange(dpsPlayers);

            // Set the dungeon difficulty
            LFGDungeonData dungeon = GetLFGDungeon(proposal.DungeonId);
            Cypher.Assert(dungeon != null);

            Group grp = !proposal.Group.IsEmpty() ? Global.GroupMgr.GetGroupByGUID(proposal.Group) : null;

            foreach (var pguid in players)
            {
                Player player = Global.ObjAccessor.FindConnectedPlayer(pguid);

                if (!player)
                    continue;

                Group group = player.GetGroup();

                if (group && group != grp)
                    group.RemoveMember(player.GetGUID());

                if (!grp)
                {
                    grp = new Group();
                    grp.ConvertToLFG();
                    grp.Create(player);
                    ObjectGuid gguid = grp.GetGUID();
                    SetState(gguid, LfgState.Proposal);
                    Global.GroupMgr.AddGroup(grp);
                }
                else if (group != grp)
                {
                    grp.AddMember(player);
                }

                grp.SetLfgRoles(pguid, proposal.Players.LookupByKey(pguid).Role);

                // Add the cooldown spell if queued for a random dungeon
                var dungeons = GetSelectedDungeons(player.GetGUID());

                if (!dungeons.Empty())
                {
                    uint rDungeonId = dungeons[0];
                    LFGDungeonData rDungeon = GetLFGDungeon(rDungeonId);

                    if (rDungeon != null &&
                        rDungeon.Type == LfgType.Random)
                        player.CastSpell(player, SharedConst.LFGSpellDungeonCooldown, false);
                }
            }

            grp.SetDungeonDifficultyID(dungeon.Difficulty);
            ObjectGuid _guid = grp.GetGUID();
            SetDungeon(_guid, dungeon.Entry());
            SetState(_guid, LfgState.Dungeon);

            _SaveToDB(_guid, grp.GetDbStoreId());

            // Teleport Player
            foreach (var it in playersToTeleport)
            {
                Player player = Global.ObjAccessor.FindPlayer(it);

                if (player)
                    TeleportPlayer(player, false);
            }

            // Update group info
            grp.SendUpdate();
        }

        public uint AddProposal(LfgProposal proposal)
        {
            proposal.Id = ++_lfgProposalId;
            _proposalsStore[_lfgProposalId] = proposal;

            return _lfgProposalId;
        }

        public void UpdateProposal(uint proposalId, ObjectGuid guid, bool accept)
        {
            // Check if the proposal exists
            var proposal = _proposalsStore.LookupByKey(proposalId);

            if (proposal == null)
                return;

            // Check if proposal have the current player
            var player = proposal.Players.LookupByKey(guid);

            if (player == null)
                return;

            player.Accept = (LfgAnswer)Convert.ToInt32(accept);

            Log.outDebug(LogFilter.Lfg, "UpdateProposal: Player [{0}] of proposal {1} selected: {2}", guid, proposalId, accept);

            if (!accept)
            {
                RemoveProposal(new KeyValuePair<uint, LfgProposal>(proposalId, proposal), LfgUpdateType.ProposalDeclined);

                return;
            }

            // check if all have answered and reorder players (leader first)
            bool allAnswered = true;

            foreach (var itPlayers in proposal.Players)
                if (itPlayers.Value.Accept != LfgAnswer.Agree) // No answer (-1) or not accepted (0)
                    allAnswered = false;

            if (!allAnswered)
            {
                foreach (var it in proposal.Players)
                    SendLfgUpdateProposal(it.Key, proposal);

                return;
            }

            bool sendUpdate = proposal.State != LfgProposalState.Success;
            proposal.State = LfgProposalState.Success;
            long joinTime = GameTime.GetGameTime();

            LFGQueue queue = GetQueue(guid);
            LfgUpdateData updateData = new(LfgUpdateType.GroupFound);

            foreach (var it in proposal.Players)
            {
                ObjectGuid pguid = it.Key;
                ObjectGuid gguid = it.Value.Group;
                uint dungeonId = GetSelectedDungeons(pguid).First();
                int waitTime;

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

                updateData.UpdateType = LfgUpdateType.RemovedFromQueue;
                SendLfgUpdateStatus(pguid, updateData, true);
                SendLfgUpdateStatus(pguid, updateData, false);

                // Update timers
                LfgRoles role = GetRoles(pguid);
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

                // Store the number of players that were present in group when joining RFD, used for Achievement purposes
                Player _player = Global.ObjAccessor.FindConnectedPlayer(pguid);

                if (_player != null)
                {
                    Group group = _player.GetGroup();

                    if (group != null)
                        _playersStore[pguid].SetNumberOfPartyMembersAtJoin((byte)group.GetMembersCount());
                }

                SetState(pguid, LfgState.Dungeon);
            }

            // Remove players/groups from Queue
            foreach (var it in proposal.Queues)
                queue.RemoveFromQueue(it);

            MakeNewGroup(proposal);
            _proposalsStore.Remove(proposalId);
        }

        private void RemoveProposal(KeyValuePair<uint, LfgProposal> itProposal, LfgUpdateType type)
        {
            LfgProposal proposal = itProposal.Value;
            proposal.State = LfgProposalState.Failed;

            Log.outDebug(LogFilter.Lfg, "RemoveProposal: Proposal {0}, State FAILED, UpdateType {1}", itProposal.Key, type);

            // Mark all people that didn't answered as no accept
            if (type == LfgUpdateType.ProposalFailed)
                foreach (var it in proposal.Players)
                    if (it.Value.Accept == LfgAnswer.Pending)
                        it.Value.Accept = LfgAnswer.Deny;

            // Mark players/groups to be removed
            List<ObjectGuid> toRemove = new();

            foreach (var it in proposal.Players)
            {
                if (it.Value.Accept == LfgAnswer.Agree)
                    continue;

                ObjectGuid guid = !it.Value.Group.IsEmpty() ? it.Value.Group : it.Key;

                // Player didn't accept or still pending when no secs left
                if (it.Value.Accept == LfgAnswer.Deny ||
                    type == LfgUpdateType.ProposalFailed)
                {
                    it.Value.Accept = LfgAnswer.Deny;
                    toRemove.Add(guid);
                }
            }

            // Notify players
            foreach (var it in proposal.Players)
            {
                ObjectGuid guid = it.Key;
                ObjectGuid gguid = !it.Value.Group.IsEmpty() ? it.Value.Group : guid;

                SendLfgUpdateProposal(guid, proposal);

                if (toRemove.Contains(gguid)) // Didn't accept or in same group that someone that didn't accept
                {
                    LfgUpdateData updateData = new();

                    if (it.Value.Accept == LfgAnswer.Deny)
                    {
                        updateData.UpdateType = type;
                        Log.outDebug(LogFilter.Lfg, "RemoveProposal: [{0}] didn't accept. Removing from queue and compatible cache", guid);
                    }
                    else
                    {
                        updateData.UpdateType = LfgUpdateType.RemovedFromQueue;
                        Log.outDebug(LogFilter.Lfg, "RemoveProposal: [{0}] in same group that someone that didn't accept. Removing from queue and compatible cache", guid);
                    }

                    RestoreState(guid, "Proposal Fail (didn't accepted or in group with someone that didn't accept");

                    if (gguid != guid)
                    {
                        RestoreState(it.Value.Group, "Proposal Fail (someone in group didn't accepted)");
                        SendLfgUpdateStatus(guid, updateData, true);
                    }
                    else
                    {
                        SendLfgUpdateStatus(guid, updateData, false);
                    }
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
                    {
                        SendLfgUpdateStatus(guid, new LfgUpdateData(LfgUpdateType.AddedToQueue, GetSelectedDungeons(guid)), false);
                    }
                }
            }

            LFGQueue queue = GetQueue(proposal.Players.First().Key);

            // Remove players/groups from queue
            foreach (var guid in toRemove)
            {
                queue.RemoveFromQueue(guid);
                proposal.Queues.Remove(guid);
            }

            // Readd to queue
            foreach (var guid in proposal.Queues)
                queue.AddToQueue(guid, true);

            _proposalsStore.Remove(itProposal.Key);
        }

        public void InitBoot(ObjectGuid gguid, ObjectGuid kicker, ObjectGuid victim, string reason)
        {
            SetVoteKick(gguid, true);

            LfgPlayerBoot boot = _bootsStore[gguid];
            boot.InProgress = true;
            boot.CancelTime = GameTime.GetGameTime() + SharedConst.LFGTimeBoot;
            boot.Reason = reason;
            boot.Victim = victim;

            List<ObjectGuid> players = GetPlayers(gguid);

            // Set votes
            foreach (var guid in players)
                boot.Votes[guid] = LfgAnswer.Pending;

            boot.Votes[victim] = LfgAnswer.Deny;  // Victim auto vote NO
            boot.Votes[kicker] = LfgAnswer.Agree; // Kicker auto vote YES

            // Notify players
            foreach (var it in players)
                SendLfgBootProposalUpdate(it, boot);
        }

        public void UpdateBoot(ObjectGuid guid, bool accept)
        {
            ObjectGuid gguid = GetGroup(guid);

            if (gguid.IsEmpty())
                return;

            var boot = _bootsStore.LookupByKey(gguid);

            if (boot == null)
                return;

            if (boot.Votes[guid] != LfgAnswer.Pending) // Cheat check: Player can't vote twice
                return;

            boot.Votes[guid] = (LfgAnswer)Convert.ToInt32(accept);

            byte agreeNum = 0;
            byte denyNum = 0;

            foreach (var (_, answer) in boot.Votes)
                switch (answer)
                {
                    case LfgAnswer.Pending:
                        break;
                    case LfgAnswer.Agree:
                        ++agreeNum;

                        break;
                    case LfgAnswer.Deny:
                        ++denyNum;

                        break;
                }

            // if we don't have enough votes (agree or deny) do nothing
            if (agreeNum < SharedConst.LFGKickVotesNeeded &&
                (boot.Votes.Count - denyNum) >= SharedConst.LFGKickVotesNeeded)
                return;

            // Send update info to all players
            boot.InProgress = false;

            foreach (var itVotes in boot.Votes)
            {
                ObjectGuid pguid = itVotes.Key;

                if (pguid != boot.Victim)
                    SendLfgBootProposalUpdate(pguid, boot);
            }

            SetVoteKick(gguid, false);

            if (agreeNum == SharedConst.LFGKickVotesNeeded) // Vote passed - Kick player
            {
                Group group = Global.GroupMgr.GetGroupByGUID(gguid);

                if (group)
                    Player.RemoveFromGroup(group, boot.Victim, RemoveMethod.KickLFG);

                DecreaseKicksLeft(gguid);
            }

            _bootsStore.Remove(gguid);
        }

        public void TeleportPlayer(Player player, bool outt, bool fromOpcode = false)
        {
            LFGDungeonData dungeon = null;
            Group group = player.GetGroup();

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
                Log.outDebug(LogFilter.Lfg, "TeleportPlayer: Player {0} is being teleported out. Current Map {1} - Expected Map {2}", player.GetName(), player.GetMapId(), dungeon.Map);

                if (player.GetMapId() == dungeon.Map)
                    player.TeleportToBGEntryPoint();

                return;
            }

            LfgTeleportResult error = LfgTeleportResult.None;

            if (!player.IsAlive())
            {
                error = LfgTeleportResult.Dead;
            }
            else if (player.IsFalling() ||
                     player.HasUnitState(UnitState.Jumping))
            {
                error = LfgTeleportResult.Falling;
            }
            else if (player.IsMirrorTimerActive(MirrorTimerType.Fatigue))
            {
                error = LfgTeleportResult.Exhaustion;
            }
            else if (player.GetVehicle())
            {
                error = LfgTeleportResult.OnTransport;
            }
            else if (!player.GetCharmedGUID().IsEmpty())
            {
                error = LfgTeleportResult.ImmuneToSummons;
            }
            else if (player.HasAura(9454)) // check Freeze debuff
            {
                error = LfgTeleportResult.NoReturnLocation;
            }
            else if (player.GetMapId() != dungeon.Map) // Do not teleport players in dungeon to the entrance
            {
                uint mapid = dungeon.Map;
                float x = dungeon.X;
                float y = dungeon.Y;
                float z = dungeon.Z;
                float orientation = dungeon.O;

                if (!fromOpcode)
                    // Select a player inside to be teleported to
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                    {
                        Player plrg = refe.GetSource();

                        if (plrg &&
                            plrg != player &&
                            plrg.GetMapId() == dungeon.Map)
                        {
                            mapid = plrg.GetMapId();
                            x = plrg.GetPositionX();
                            y = plrg.GetPositionY();
                            z = plrg.GetPositionZ();
                            orientation = plrg.GetOrientation();

                            break;
                        }
                    }

                if (!player.GetMap().IsDungeon())
                    player.SetBattlegroundEntryPoint();

                player.FinishTaxiFlight();

                if (!player.TeleportTo(mapid, x, y, z, orientation))
                    error = LfgTeleportResult.NoReturnLocation;
            }
            else
            {
                error = LfgTeleportResult.NoReturnLocation;
            }

            if (error != LfgTeleportResult.None)
                player.GetSession().SendLfgTeleportError(error);

            Log.outDebug(LogFilter.Lfg, "TeleportPlayer: Player {0} is being teleported in to map {1} (x: {2}, y: {3}, z: {4}) Result: {5}", player.GetName(), dungeon.Map, dungeon.X, dungeon.Y, dungeon.Z, error);
        }

        public void FinishDungeon(ObjectGuid gguid, uint dungeonId, Map currMap)
        {
            uint gDungeonId = GetDungeon(gguid);

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

            List<ObjectGuid> players = GetPlayers(gguid);

            foreach (var guid in players)
            {
                if (GetState(guid) == LfgState.FinishedDungeon)
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} already rewarded");

                    continue;
                }

                uint rDungeonId = 0;
                List<uint> dungeons = GetSelectedDungeons(guid);

                if (!dungeons.Empty())
                    rDungeonId = dungeons.First();

                SetState(guid, LfgState.FinishedDungeon);

                // Give rewards only if its a random dungeon
                LFGDungeonData dungeon = GetLFGDungeon(rDungeonId);

                if (dungeon == null ||
                    (dungeon.Type != LfgType.Random && !dungeon.Seasonal))
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} dungeon {rDungeonId} is not random or seasonal");

                    continue;
                }

                Player player = Global.ObjAccessor.FindPlayer(guid);

                if (player == null)
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} not found in world");

                    continue;
                }

                if (player.GetMap() != currMap)
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} is in a different map");

                    continue;
                }

                player.RemoveAurasDueToSpell(SharedConst.LFGSpellDungeonCooldown);

                LFGDungeonData dungeonDone = GetLFGDungeon(dungeonId);
                uint mapId = dungeonDone != null ? dungeonDone.Map : 0;

                if (player.GetMapId() != mapId)
                {
                    Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} is in map {player.GetMapId()} and should be in {mapId} to get reward");

                    continue;
                }

                // Update achievements
                if (dungeon.Difficulty == Difficulty.Heroic)
                {
                    byte lfdRandomPlayers = 0;
                    byte numParty = _playersStore[guid].GetNumberOfPartyMembersAtJoin();

                    if (numParty != 0)
                        lfdRandomPlayers = (byte)(5 - numParty);
                    else
                        lfdRandomPlayers = 4;

                    player.UpdateCriteria(CriteriaType.CompletedLFGDungeonWithStrangers, lfdRandomPlayers);
                }

                LfgReward reward = GetRandomDungeonReward(rDungeonId, player.GetLevel());

                if (reward == null)
                    continue;

                bool done = false;
                Quest quest = Global.ObjectMgr.GetQuestTemplate(reward.FirstQuest);

                if (quest == null)
                    continue;

                // if we can take the quest, means that we haven't done this kind of "run", IE: First Heroic Random of Day.
                if (player.CanRewardQuest(quest, false))
                {
                    player.RewardQuest(quest, LootItemType.Item, 0, null, false);
                }
                else
                {
                    done = true;
                    quest = Global.ObjectMgr.GetQuestTemplate(reward.OtherQuest);

                    if (quest == null)
                        continue;

                    // we give reward without informing client (retail does this)
                    player.RewardQuest(quest, LootItemType.Item, 0, null, false);
                }

                // Give rewards
                string doneString = done ? "" : "not";
                Log.outDebug(LogFilter.Lfg, $"Group: {gguid}, Player: {guid} done dungeon {GetDungeon(gguid)}, {doneString} previously done.");
                LfgPlayerRewardData data = new(dungeon.Entry(), GetDungeon(gguid, false), done, quest);
                player.GetSession().SendLfgPlayerReward(data);
            }
        }

        private List<uint> GetDungeonsByRandom(uint randomdungeon)
        {
            LFGDungeonData dungeon = GetLFGDungeon(randomdungeon);
            byte group = (byte)(dungeon != null ? dungeon.Group : 0);

            return _cachedDungeonMapStore.LookupByKey(group);
        }

        public LfgReward GetRandomDungeonReward(uint dungeon, uint level)
        {
            LfgReward reward = null;
            var bounds = _rewardMapStore.LookupByKey(dungeon & 0x00FFFFFF);

            foreach (var rew in bounds)
            {
                reward = rew;

                // ordered properly at loading
                if (rew.MaxLevel >= level)
                    break;
            }

            return reward;
        }

        public LfgType GetDungeonType(uint dungeonId)
        {
            LFGDungeonData dungeon = GetLFGDungeon(dungeonId);

            if (dungeon == null)
                return LfgType.None;

            return dungeon.Type;
        }

        public LfgState GetState(ObjectGuid guid)
        {
            LfgState state;

            if (guid.IsParty())
            {
                if (!_groupsStore.ContainsKey(guid))
                    return LfgState.None;

                state = _groupsStore[guid].GetState();
            }
            else
            {
                AddPlayerData(guid);
                state = _playersStore[guid].GetState();
            }

            Log.outDebug(LogFilter.Lfg, "GetState: [{0}] = {1}", guid, state);

            return state;
        }

        public LfgState GetOldState(ObjectGuid guid)
        {
            LfgState state;

            if (guid.IsParty())
            {
                state = _groupsStore[guid].GetOldState();
            }
            else
            {
                AddPlayerData(guid);
                state = _playersStore[guid].GetOldState();
            }

            Log.outDebug(LogFilter.Lfg, "GetOldState: [{0}] = {1}", guid, state);

            return state;
        }

        public bool IsVoteKickActive(ObjectGuid gguid)
        {
            Cypher.Assert(gguid.IsParty());

            bool active = _groupsStore[gguid].IsVoteKickActive();
            Log.outInfo(LogFilter.Lfg, "Group: {0}, Active: {1}", gguid.ToString(), active);

            return active;
        }

        public uint GetDungeon(ObjectGuid guid, bool asId = true)
        {
            if (!_groupsStore.ContainsKey(guid))
                return 0;

            uint dungeon = _groupsStore[guid].GetDungeon(asId);
            Log.outDebug(LogFilter.Lfg, "GetDungeon: [{0}] asId: {1} = {2}", guid, asId, dungeon);

            return dungeon;
        }

        public uint GetDungeonMapId(ObjectGuid guid)
        {
            if (!_groupsStore.ContainsKey(guid))
                return 0;

            uint dungeonId = _groupsStore[guid].GetDungeon(true);
            uint mapId = 0;

            if (dungeonId != 0)
            {
                LFGDungeonData dungeon = GetLFGDungeon(dungeonId);

                if (dungeon != null)
                    mapId = dungeon.Map;
            }

            Log.outError(LogFilter.Lfg, "GetDungeonMapId: [{0}] = {1} (DungeonId = {2})", guid, mapId, dungeonId);

            return mapId;
        }

        public LfgRoles GetRoles(ObjectGuid guid)
        {
            LfgRoles roles = _playersStore[guid].GetRoles();
            Log.outDebug(LogFilter.Lfg, "GetRoles: [{0}] = {1}", guid, roles);

            return roles;
        }

        public List<uint> GetSelectedDungeons(ObjectGuid guid)
        {
            Log.outDebug(LogFilter.Lfg, "GetSelectedDungeons: [{0}]", guid);

            return _playersStore[guid].GetSelectedDungeons();
        }

        public uint GetSelectedRandomDungeon(ObjectGuid guid)
        {
            if (GetState(guid) != LfgState.None)
            {
                var dungeons = GetSelectedDungeons(guid);

                if (!dungeons.Empty())
                {
                    LFGDungeonData dungeon = GetLFGDungeon(dungeons.First());

                    if (dungeon != null &&
                        dungeon.Type == LfgType.Raid)
                        return dungeons.First();
                }
            }

            return 0;
        }

        public Dictionary<uint, LfgLockInfoData> GetLockedDungeons(ObjectGuid guid)
        {
            Dictionary<uint, LfgLockInfoData> lockDic = new();
            Player player = Global.ObjAccessor.FindConnectedPlayer(guid);

            if (!player)
            {
                Log.outWarn(LogFilter.Lfg, "{0} not ingame while retrieving his LockedDungeons.", guid.ToString());

                return lockDic;
            }

            uint level = player.GetLevel();
            Expansion expansion = player.GetSession().GetExpansion();
            var dungeons = GetDungeonsByRandom(0);
            bool denyJoin = !player.GetSession().HasPermission(RBACPermissions.JoinDungeonFinder);

            foreach (var it in dungeons)
            {
                LFGDungeonData dungeon = GetLFGDungeon(it);

                if (dungeon == null) // should never happen - We provide a list from sLFGDungeonStore
                    continue;

                LfgLockStatusType lockStatus = 0;
                AccessRequirement ar;

                if (denyJoin)
                {
                    lockStatus = LfgLockStatusType.RaidLocked;
                }
                else if (dungeon.Expansion > (uint)expansion)
                {
                    lockStatus = LfgLockStatusType.InsufficientExpansion;
                }
                else if (Global.DisableMgr.IsDisabledFor(DisableType.Map, dungeon.Map, player))
                {
                    lockStatus = LfgLockStatusType.NotInSeason;
                }
                else if (Global.DisableMgr.IsDisabledFor(DisableType.LFGMap, dungeon.Map, player))
                {
                    lockStatus = LfgLockStatusType.RaidLocked;
                }
                else if (dungeon.Difficulty > Difficulty.Normal &&
                         Global.InstanceLockMgr.FindActiveInstanceLock(guid, new MapDb2Entries(dungeon.Map, dungeon.Difficulty)) != null)
                {
                    lockStatus = LfgLockStatusType.RaidLocked;
                }
                else if (dungeon.Seasonal &&
                         !IsSeasonActive(dungeon.Id))
                {
                    lockStatus = LfgLockStatusType.NotInSeason;
                }
                else if (dungeon.RequiredItemLevel > player.GetAverageItemLevel())
                {
                    lockStatus = LfgLockStatusType.TooLowGearScore;
                }
                else if ((ar = Global.ObjectMgr.GetAccessRequirement(dungeon.Map, dungeon.Difficulty)) != null)
                {
                    if (ar.Achievement != 0 &&
                        !player.HasAchieved(ar.Achievement))
                    {
                        lockStatus = LfgLockStatusType.MissingAchievement;
                    }
                    else if (player.GetTeam() == Team.Alliance &&
                             ar.Quest_A != 0 &&
                             !player.GetQuestRewardStatus(ar.Quest_A))
                    {
                        lockStatus = LfgLockStatusType.QuestNotCompleted;
                    }
                    else if (player.GetTeam() == Team.Horde &&
                             ar.Quest_H != 0 &&
                             !player.GetQuestRewardStatus(ar.Quest_H))
                    {
                        lockStatus = LfgLockStatusType.QuestNotCompleted;
                    }
                    else if (ar.Item != 0)
                    {
                        if (!player.HasItemCount(ar.Item) &&
                            (ar.Item2 == 0 || !player.HasItemCount(ar.Item2)))
                            lockStatus = LfgLockStatusType.MissingItem;
                    }
                    else if (ar.Item2 != 0 &&
                             !player.HasItemCount(ar.Item2))
                    {
                        lockStatus = LfgLockStatusType.MissingItem;
                    }
                }
                else
                {
                    var levels = Global.DB2Mgr.GetContentTuningData(dungeon.ContentTuningId, player.PlayerData.CtrOptions.GetValue().ContentTuningConditionMask);

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
                    lockDic[dungeon.Entry()] = new LfgLockInfoData(lockStatus, dungeon.RequiredItemLevel, player.GetAverageItemLevel());
            }

            return lockDic;
        }

        public byte GetKicksLeft(ObjectGuid guid)
        {
            byte kicks = _groupsStore[guid].GetKicksLeft();
            Log.outDebug(LogFilter.Lfg, "GetKicksLeft: [{0}] = {1}", guid, kicks);

            return kicks;
        }

        private void RestoreState(ObjectGuid guid, string debugMsg)
        {
            if (guid.IsParty())
            {
                var data = _groupsStore[guid];
                data.RestoreState();
            }
            else
            {
                var data = _playersStore[guid];
                data.RestoreState();
            }
        }

        public void SetState(ObjectGuid guid, LfgState state)
        {
            if (guid.IsParty())
            {
                if (!_groupsStore.ContainsKey(guid))
                    _groupsStore[guid] = new LFGGroupData();

                var data = _groupsStore[guid];
                data.SetState(state);
            }
            else
            {
                var data = _playersStore[guid];
                data.SetState(state);
            }
        }

        private void SetVoteKick(ObjectGuid gguid, bool active)
        {
            Cypher.Assert(gguid.IsParty());

            var data = _groupsStore[gguid];
            Log.outInfo(LogFilter.Lfg, "Group: {0}, New State: {1}, Previous: {2}", gguid.ToString(), active, data.IsVoteKickActive());

            data.SetVoteKick(active);
        }

        private void SetDungeon(ObjectGuid guid, uint dungeon)
        {
            AddPlayerData(guid);
            Log.outDebug(LogFilter.Lfg, "SetDungeon: [{0}] dungeon {1}", guid, dungeon);
            _groupsStore[guid].SetDungeon(dungeon);
        }

        private void SetRoles(ObjectGuid guid, LfgRoles roles)
        {
            AddPlayerData(guid);
            Log.outDebug(LogFilter.Lfg, "SetRoles: [{0}] roles: {1}", guid, roles);
            _playersStore[guid].SetRoles(roles);
        }

        public void SetSelectedDungeons(ObjectGuid guid, List<uint> dungeons)
        {
            AddPlayerData(guid);
            Log.outDebug(LogFilter.Lfg, "SetSelectedDungeons: [{0}] Dungeons: {1}", guid, ConcatenateDungeons(dungeons));
            _playersStore[guid].SetSelectedDungeons(dungeons);
        }

        private void DecreaseKicksLeft(ObjectGuid guid)
        {
            Log.outDebug(LogFilter.Lfg, "DecreaseKicksLeft: [{0}]", guid);
            _groupsStore[guid].DecreaseKicksLeft();
        }

        private void AddPlayerData(ObjectGuid guid)
        {
            if (_playersStore.ContainsKey(guid))
                return;

            _playersStore[guid] = new LFGPlayerData();
        }

        private void SetTicket(ObjectGuid guid, RideTicket ticket)
        {
            _playersStore[guid].SetTicket(ticket);
        }

        private void RemovePlayerData(ObjectGuid guid)
        {
            Log.outDebug(LogFilter.Lfg, "RemovePlayerData: [{0}]", guid);
            _playersStore.Remove(guid);
        }

        public void RemoveGroupData(ObjectGuid guid)
        {
            Log.outDebug(LogFilter.Lfg, "RemoveGroupData: [{0}]", guid);
            var it = _groupsStore.LookupByKey(guid);

            if (it == null)
                return;

            LfgState state = GetState(guid);
            // If group is being formed after proposal success do nothing more
            List<ObjectGuid> players = it.GetPlayers();

            foreach (var _guid in players)
            {
                SetGroup(_guid, ObjectGuid.Empty);

                if (state != LfgState.Proposal)
                {
                    SetState(_guid, LfgState.None);
                    SendLfgUpdateStatus(_guid, new LfgUpdateData(LfgUpdateType.RemovedFromQueue), true);
                }
            }

            _groupsStore.Remove(guid);
        }

        private Team GetTeam(ObjectGuid guid)
        {
            return _playersStore[guid].GetTeam();
        }

        private LfgRoles FilterClassRoles(Player player, LfgRoles roles)
        {
            uint allowedRoles = (uint)LfgRoles.Leader;

            for (uint i = 0; i < PlayerConst.MaxSpecializations; ++i)
            {
                var specialization = Global.DB2Mgr.GetChrSpecializationByIndex(player.GetClass(), i);

                if (specialization != null)
                    allowedRoles |= (1u << (specialization.Role + 1));
            }

            return roles & (LfgRoles)allowedRoles;
        }

        public byte RemovePlayerFromGroup(ObjectGuid gguid, ObjectGuid guid)
        {
            return _groupsStore[gguid].RemovePlayer(guid);
        }

        public void AddPlayerToGroup(ObjectGuid gguid, ObjectGuid guid)
        {
            if (!_groupsStore.ContainsKey(gguid))
                _groupsStore[gguid] = new LFGGroupData();

            _groupsStore[gguid].AddPlayer(guid);
        }

        public void SetLeader(ObjectGuid gguid, ObjectGuid leader)
        {
            if (!_groupsStore.ContainsKey(gguid))
                _groupsStore[gguid] = new LFGGroupData();

            _groupsStore[gguid].SetLeader(leader);
        }

        public void SetTeam(ObjectGuid guid, Team team)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup))
                team = 0;

            _playersStore[guid].SetTeam(team);
        }

        public ObjectGuid GetGroup(ObjectGuid guid)
        {
            AddPlayerData(guid);

            return _playersStore[guid].GetGroup();
        }

        public void SetGroup(ObjectGuid guid, ObjectGuid group)
        {
            AddPlayerData(guid);
            _playersStore[guid].SetGroup(group);
        }

        private List<ObjectGuid> GetPlayers(ObjectGuid guid)
        {
            return _groupsStore[guid].GetPlayers();
        }

        public byte GetPlayerCount(ObjectGuid guid)
        {
            return _groupsStore[guid].GetPlayerCount();
        }

        public ObjectGuid GetLeader(ObjectGuid guid)
        {
            return _groupsStore[guid].GetLeader();
        }

        public bool HasIgnore(ObjectGuid guid1, ObjectGuid guid2)
        {
            Player plr1 = Global.ObjAccessor.FindPlayer(guid1);
            Player plr2 = Global.ObjAccessor.FindPlayer(guid2);

            return plr1 != null && plr2 != null && (plr1.GetSocial().HasIgnore(guid2, plr2.GetSession().GetAccountGUID()) || plr2.GetSocial().HasIgnore(guid1, plr1.GetSession().GetAccountGUID()));
        }

        public void SendLfgRoleChosen(ObjectGuid guid, ObjectGuid pguid, LfgRoles roles)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                player.GetSession().SendLfgRoleChosen(pguid, roles);
        }

        public void SendLfgRoleCheckUpdate(ObjectGuid guid, LfgRoleCheck roleCheck)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                player.GetSession().SendLfgRoleCheckUpdate(roleCheck);
        }

        public void SendLfgUpdateStatus(ObjectGuid guid, LfgUpdateData data, bool party)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                player.GetSession().SendLfgUpdateStatus(data, party);
        }

        public void SendLfgJoinResult(ObjectGuid guid, LfgJoinResultData data)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                player.GetSession().SendLfgJoinResult(data);
        }

        public void SendLfgBootProposalUpdate(ObjectGuid guid, LfgPlayerBoot boot)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                player.GetSession().SendLfgBootProposalUpdate(boot);
        }

        public void SendLfgUpdateProposal(ObjectGuid guid, LfgProposal proposal)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                player.GetSession().SendLfgProposalUpdate(proposal);
        }

        public void SendLfgQueueStatus(ObjectGuid guid, LfgQueueStatusData data)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                player.GetSession().SendLfgQueueStatus(data);
        }

        public bool IsLfgGroup(ObjectGuid guid)
        {
            return !guid.IsEmpty() && guid.IsParty() && _groupsStore[guid].IsLfgGroup();
        }

        public byte GetQueueId(ObjectGuid guid)
        {
            if (guid.IsParty())
            {
                List<ObjectGuid> players = GetPlayers(guid);
                ObjectGuid pguid = players.Empty() ? ObjectGuid.Empty : players.First();

                if (!pguid.IsEmpty())
                    return (byte)GetTeam(pguid);
            }

            return (byte)GetTeam(guid);
        }

        public LFGQueue GetQueue(ObjectGuid guid)
        {
            byte queueId = GetQueueId(guid);

            if (!_queuesStore.ContainsKey(queueId))
                _queuesStore[queueId] = new LFGQueue();

            return _queuesStore[queueId];
        }

        public bool AllQueued(List<ObjectGuid> check)
        {
            if (check.Empty())
                return false;

            foreach (var guid in check)
            {
                LfgState state = GetState(guid);

                if (state != LfgState.Queued)
                {
                    if (state != LfgState.Proposal)
                        Log.outDebug(LogFilter.Lfg, "Unexpected State found while trying to form new group. Guid: {0}, State: {1}", guid.ToString(), state);

                    return false;
                }
            }

            return true;
        }

        public long GetQueueJoinTime(ObjectGuid guid)
        {
            byte queueId = GetQueueId(guid);
            var lfgQueue = _queuesStore.LookupByKey(queueId);

            if (lfgQueue != null)
                return lfgQueue.GetJoinTime(guid);

            return 0;
        }

        // Only for debugging purposes
        public void Clean()
        {
            _queuesStore.Clear();
        }

        public bool IsOptionEnabled(LfgOptions option)
        {
            return _options.HasAnyFlag(option);
        }

        public LfgOptions GetOptions()
        {
            return _options;
        }

        public void SetOptions(LfgOptions options)
        {
            _options = options;
        }

        public LfgUpdateData GetLfgStatus(ObjectGuid guid)
        {
            var playerData = _playersStore[guid];

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
            uint size = (uint)_queuesStore.Count;

            string str = "Number of Queues: " + size + "\n";

            foreach (var pair in _queuesStore)
            {
                string queued = pair.Value.DumpQueueInfo();
                string compatibles = pair.Value.DumpCompatibleInfo(full);
                str += queued + compatibles;
            }

            return str;
        }

        public void SetupGroupMember(ObjectGuid guid, ObjectGuid gguid)
        {
            List<uint> dungeons = new();
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
                List<uint> dungeons = GetSelectedDungeons(guid);

                if (!dungeons.Empty())
                {
                    LFGDungeonData dungeon = GetLFGDungeon(dungeons.First());

                    if (dungeon != null &&
                        (dungeon.Type == LfgType.Random || dungeon.Seasonal))
                        return true;
                }
            }

            return false;
        }

        public bool InLfgDungeonMap(ObjectGuid guid, uint map, Difficulty difficulty)
        {
            if (!guid.IsParty())
                guid = GetGroup(guid);

            uint dungeonId = GetDungeon(guid, true);

            if (dungeonId != 0)
            {
                LFGDungeonData dungeon = GetLFGDungeon(dungeonId);

                if (dungeon != null)
                    if (dungeon.Map == map &&
                        dungeon.Difficulty == difficulty)
                        return true;
            }

            return false;
        }

        public uint GetLFGDungeonEntry(uint id)
        {
            if (id != 0)
            {
                LFGDungeonData dungeon = GetLFGDungeon(id);

                if (dungeon != null)
                    return dungeon.Entry();
            }

            return 0;
        }

        public List<uint> GetRandomAndSeasonalDungeons(uint level, uint expansion, uint contentTuningReplacementConditionMask)
        {
            List<uint> randomDungeons = new();

            foreach (var dungeon in _lfgDungeonStore.Values)
            {
                if (!(dungeon.Type == LfgType.Random || (dungeon.Seasonal && Global.LFGMgr.IsSeasonActive(dungeon.Id))))
                    continue;

                if (dungeon.Expansion > expansion)
                    continue;

                var levels = Global.DB2Mgr.GetContentTuningData(dungeon.ContentTuningId, contentTuningReplacementConditionMask);

                if (levels.HasValue)
                    if (levels.Value.MinLevel > level ||
                        level > levels.Value.MaxLevel)
                        continue;

                randomDungeons.Add(dungeon.Entry());
            }

            return randomDungeons;
        }
    }
}