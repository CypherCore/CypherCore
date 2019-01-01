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
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.DungeonFinding
{
    public class LFGQueue
    {
        public static string ConcatenateGuids(List<ObjectGuid> guids)
        {
            if (guids.Empty())
                return "";

            // need the guids in order to avoid duplicates
            StringBuilder val = new StringBuilder();
            guids.Sort();
            var it = guids.First();
            val.Append(it);
            foreach (var guid in guids)
            {
                if (guid == it)
                    continue;
                val.AppendFormat("|{0}", guid);
            }

            return val.ToString();
        }

        public static string GetRolesString(LfgRoles roles)
        {
            StringBuilder rolesstr = new StringBuilder();

            if (roles.HasAnyFlag(LfgRoles.Tank))
                rolesstr.Append("Tank");

            if (roles.HasAnyFlag(LfgRoles.Healer))
            {
                if (rolesstr.Capacity != 0)
                    rolesstr.Append(", ");
                rolesstr.Append("Healer");
            }

            if (roles.HasAnyFlag(LfgRoles.Damage))
            {
                if (rolesstr.Capacity != 0)
                    rolesstr.Append(", ");
                rolesstr.Append("Damage");
            }

            if (roles.HasAnyFlag(LfgRoles.Leader))
            {
                if (rolesstr.Capacity != 0)
                    rolesstr.Append(", ");
                rolesstr.Append("Leader");
            }

            if (rolesstr.Capacity == 0)
                rolesstr.Append("None");

            return rolesstr.ToString();
        }

        public static string ConcatenateDungeons(List<uint> dungeons)
        {
            string str = "";
            if (!dungeons.Empty())
            {
                foreach (var it in dungeons)
                {
                    if (!string.IsNullOrEmpty(str))
                        str += ", ";
                    str += it;
                }
            }
            return str;
        }

        string GetCompatibleString(LfgCompatibility compatibles)
        {
            switch (compatibles)
            {
                case LfgCompatibility.Pending:
                    return "Pending";
                case LfgCompatibility.BadStates:
                    return "Compatibles (Bad States)";
                case LfgCompatibility.Match:
                    return "Match";
                case LfgCompatibility.WithLessPlayers:
                    return "Compatibles (Not enough players)";
                case LfgCompatibility.HasIgnores:
                    return "Has ignores";
                case LfgCompatibility.MultipleLfgGroups:
                    return "Multiple Lfg Groups";
                case LfgCompatibility.NoDungeons:
                    return "Incompatible dungeons";
                case LfgCompatibility.NoRoles:
                    return "Incompatible roles";
                case LfgCompatibility.TooMuchPlayers:
                    return "Too much players";
                case LfgCompatibility.WrongGroupSize:
                    return "Wrong group size";
                default:
                    return "Unknown";
            }
        }

        public void AddToQueue(ObjectGuid guid, bool reAdd = false)
        {
            if (!QueueDataStore.ContainsKey(guid))
            {
                Log.outError(LogFilter.Lfg, "AddToQueue: Queue data not found for [{0}]", guid);
                return;
            }

            if (reAdd)
                AddToFrontCurrentQueue(guid);
            else
                AddToNewQueue(guid);
        }

        public void RemoveFromQueue(ObjectGuid guid)
        {
            RemoveFromNewQueue(guid);
            RemoveFromCurrentQueue(guid);
            RemoveFromCompatibles(guid);

            string sguid = guid.ToString();

            var itDelete = QueueDataStore.LastOrDefault().Key;
            foreach (var key in QueueDataStore.Keys.ToList())
            {
                var data = QueueDataStore[key];
                if (key != guid)
                {
                    if (data.bestCompatible.Contains(sguid))
                    {
                        data.bestCompatible = "";
                        FindBestCompatibleInQueue(key, data);
                    }
                }
                else
                    itDelete = key;
            }

            if (!itDelete.IsEmpty())
                QueueDataStore.Remove(itDelete);
        }

        public void AddToNewQueue(ObjectGuid guid)
        {
            newToQueueStore.Add(guid);
        }

        public void RemoveFromNewQueue(ObjectGuid guid)
        {
            newToQueueStore.Remove(guid);
        }

        public void AddToCurrentQueue(ObjectGuid guid)
        {
            currentQueueStore.Add(guid);
        }

        void AddToFrontCurrentQueue(ObjectGuid guid)
        {
            currentQueueStore.Insert(0, guid);
        }

        public void RemoveFromCurrentQueue(ObjectGuid guid)
        {
            currentQueueStore.Remove(guid);
        }

        public void AddQueueData(ObjectGuid guid, long joinTime, List<uint> dungeons, Dictionary<ObjectGuid, LfgRoles> rolesMap)
        {
            QueueDataStore[guid] = new LfgQueueData(joinTime, dungeons, rolesMap);
            AddToQueue(guid);
        }

        public void RemoveQueueData(ObjectGuid guid)
        {
            QueueDataStore.Remove(guid);
        }

        public void UpdateWaitTimeAvg(int waitTime, uint dungeonId)
        {
            LfgWaitTime wt = waitTimesAvgStore[dungeonId];
            uint old_number = wt.number++;
            wt.time = (int)((wt.time * old_number + waitTime) / wt.number);
        }

        public void UpdateWaitTimeTank(int waitTime, uint dungeonId)
        {
            LfgWaitTime wt = waitTimesTankStore[dungeonId];
            uint old_number = wt.number++;
            wt.time = (int)((wt.time * old_number + waitTime) / wt.number);
        }

        public void UpdateWaitTimeHealer(int waitTime, uint dungeonId)
        {
            LfgWaitTime wt = waitTimesHealerStore[dungeonId];
            uint old_number = wt.number++;
            wt.time = (int)((wt.time * old_number + waitTime) / wt.number);
        }

        public void UpdateWaitTimeDps(int waitTime, uint dungeonId)
        {
            LfgWaitTime wt = waitTimesDpsStore[dungeonId];
            uint old_number = wt.number++;
            wt.time = (int)((wt.time * old_number + waitTime) / wt.number);
        }

        void RemoveFromCompatibles(ObjectGuid guid)
        {
            string strGuid = guid.ToString();

            Log.outDebug(LogFilter.Lfg, "RemoveFromCompatibles: Removing [{0}]", guid);
            foreach (var itNext in CompatibleMapStore.ToList())
            {
                if (itNext.Key.Contains(strGuid))
                    CompatibleMapStore.Remove(itNext.Key);
            }
        }

        void SetCompatibles(string key, LfgCompatibility compatibles)
        {
            if (!CompatibleMapStore.ContainsKey(key))
                CompatibleMapStore[key] = new LfgCompatibilityData();

            CompatibleMapStore[key].compatibility = compatibles;
        }

        void SetCompatibilityData(string key, LfgCompatibilityData data)
        {
            CompatibleMapStore[key] = data;
        }

        LfgCompatibility GetCompatibles(string key)
        {
            var compatibilityData = CompatibleMapStore.LookupByKey(key);
            if (compatibilityData != null)
                return compatibilityData.compatibility;

            return LfgCompatibility.Pending;
        }

        LfgCompatibilityData GetCompatibilityData(string key)
        {
            var compatibilityData = CompatibleMapStore.LookupByKey(key);
            if (compatibilityData != null)
                return compatibilityData;

            return null;
        }

        public byte FindGroups()
        {
            byte proposals = 0;
            List<ObjectGuid> firstNew = new List<ObjectGuid>();
            while (!newToQueueStore.Empty())
            {
                ObjectGuid frontguid = newToQueueStore.First();
                Log.outDebug(LogFilter.Lfg, "FindGroups: checking [{0}] newToQueue({1}), currentQueue({2})", frontguid, newToQueueStore.Count, currentQueueStore.Count);
                firstNew.Clear();
                firstNew.Add(frontguid);
                RemoveFromNewQueue(frontguid);

                List<ObjectGuid> temporalList = new List<ObjectGuid>(currentQueueStore);
                LfgCompatibility compatibles = FindNewGroups(firstNew, temporalList);

                if (compatibles == LfgCompatibility.Match)
                    ++proposals;
                else
                    AddToCurrentQueue(frontguid);                  // Lfg group not found, add this group to the queue.
            }
            return proposals;
        }

        LfgCompatibility FindNewGroups(List<ObjectGuid> check, List<ObjectGuid> all)
        {
            string strGuids = ConcatenateGuids(check);
            LfgCompatibility compatibles = GetCompatibles(strGuids);

            Log.outDebug(LogFilter.Lfg, "FindNewGroup: ({0}): {1} - all({2})", strGuids, GetCompatibleString(compatibles), ConcatenateGuids(all));
            if (compatibles == LfgCompatibility.Pending) // Not previously cached, calculate
                compatibles = CheckCompatibility(check);

            if (compatibles == LfgCompatibility.BadStates && Global.LFGMgr.AllQueued(check))
            {
                Log.outDebug(LogFilter.Lfg, "FindNewGroup: ({0}) compatibles (cached) changed from bad states to match", strGuids);
                SetCompatibles(strGuids, LfgCompatibility.Match);
                return LfgCompatibility.Match;
            }

            if (compatibles != LfgCompatibility.WithLessPlayers)
                return compatibles;

            // Try to match with queued groups
            while (!all.Empty())
            {
                check.Add(all.First());
                all.RemoveAt(0);
                LfgCompatibility subcompatibility = FindNewGroups(check, all);
                if (subcompatibility == LfgCompatibility.Match)
                    return LfgCompatibility.Match;
                check.RemoveAt(check.Count - 1);
            }
            return compatibles;
        }

        LfgCompatibility CheckCompatibility(List<ObjectGuid> check)
        {
            string strGuids = ConcatenateGuids(check);
            LfgProposal proposal = new LfgProposal();
            List<uint> proposalDungeons;
            Dictionary<ObjectGuid, ObjectGuid> proposalGroups = new Dictionary<ObjectGuid, ObjectGuid>();
            Dictionary<ObjectGuid, LfgRoles> proposalRoles = new Dictionary<ObjectGuid, LfgRoles>();

            // Check for correct size
            if (check.Count > MapConst.MaxGroupSize || check.Empty())
            {
                Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}): Size wrong - Not compatibles", strGuids);
                return LfgCompatibility.WrongGroupSize;
            }

            // Check all-but-new compatiblitity
            if (check.Count > 2)
            {
                ObjectGuid frontGuid = check.First();
                check.RemoveAt(0);

                // Check all-but-new compatibilities (New, A, B, C, D) -. check(A, B, C, D)
                LfgCompatibility child_compatibles = CheckCompatibility(check);
                if (child_compatibles < LfgCompatibility.WithLessPlayers) // Group not compatible
                {
                    Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) child {1} not compatibles", strGuids, ConcatenateGuids(check));
                    SetCompatibles(strGuids, child_compatibles);
                    return child_compatibles;
                }
                check.Insert(0, frontGuid);
            }

            // Check if more than one LFG group and number of players joining
            byte numPlayers = 0;
            byte numLfgGroups = 0;
            foreach (var guid in check)
            {
                if (!(numLfgGroups < 2) && !(numPlayers <= MapConst.MaxGroupSize))
                    break;

                var itQueue = QueueDataStore.LookupByKey(guid);
                if (itQueue == null)
                {
                    Log.outError(LogFilter.Lfg, "CheckCompatibility: [{0}] is not queued but listed as queued!", guid);
                    RemoveFromQueue(guid);
                    return LfgCompatibility.Pending;
                }

                // Store group so we don't need to call Mgr to get it later (if it's player group will be 0 otherwise would have joined as group)
                foreach (var it2 in itQueue.roles)
                    proposalGroups[it2.Key] = guid.IsPlayer() ? guid : ObjectGuid.Empty;

                numPlayers += (byte)itQueue.roles.Count;

                if (Global.LFGMgr.IsLfgGroup(guid))
                {
                    if (numLfgGroups == 0)
                        proposal.group = guid;
                    ++numLfgGroups;
                }
            }

            // Group with less that MAXGROUPSIZE members always compatible
            if (check.Count == 1 && numPlayers != MapConst.MaxGroupSize)
            {
                Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) sigle group. Compatibles", strGuids);
                var guid = check.First();
                var itQueue = QueueDataStore.LookupByKey(guid);

                LfgCompatibilityData data = new LfgCompatibilityData(LfgCompatibility.WithLessPlayers);
                data.roles = itQueue.roles;
                Global.LFGMgr.CheckGroupRoles(data.roles);

                UpdateBestCompatibleInQueue(guid, itQueue, strGuids, data.roles);
                SetCompatibilityData(strGuids, data);
                return LfgCompatibility.WithLessPlayers;
            }

            if (numLfgGroups > 1)
            {
                Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) More than one Lfggroup ({1})", strGuids, numLfgGroups);
                SetCompatibles(strGuids, LfgCompatibility.MultipleLfgGroups);
                return LfgCompatibility.MultipleLfgGroups;
            }

            if (numPlayers > MapConst.MaxGroupSize)
            {
                Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) Too much players ({1})", strGuids, numPlayers);
                SetCompatibles(strGuids, LfgCompatibility.TooMuchPlayers);
                return LfgCompatibility.TooMuchPlayers;
            }

            // If it's single group no need to check for duplicate players, ignores, bad roles or bad dungeons as it's been checked before joining
            if (check.Count > 1)
            {
                foreach (var it in check)
                {
                    Dictionary<ObjectGuid, LfgRoles> roles = QueueDataStore[it].roles;
                    foreach (var rolePair in roles)
                    {
                        KeyValuePair<ObjectGuid, LfgRoles> itPlayer = new KeyValuePair<ObjectGuid, LfgRoles>();
                        foreach (var _player in proposalRoles)
                        {
                            itPlayer = _player;
                            if (rolePair.Key == itPlayer.Key)
                                Log.outError(LogFilter.Lfg, "CheckCompatibility: ERROR! Player multiple times in queue! [{0}]", rolePair.Key);
                            else if (Global.LFGMgr.HasIgnore(rolePair.Key, itPlayer.Key))
                                break;
                        }
                        if (itPlayer.Key == proposalRoles.LastOrDefault().Key)
                            proposalRoles[rolePair.Key] = rolePair.Value;
                    }
                }

                byte playersize = (byte)(numPlayers - proposalRoles.Count);
                if (playersize != 0)
                {
                    Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) not compatible, {1} players are ignoring each other", strGuids, playersize);
                    SetCompatibles(strGuids, LfgCompatibility.HasIgnores);
                    return LfgCompatibility.HasIgnores;
                }
                StringBuilder o;
                Dictionary<ObjectGuid, LfgRoles> debugRoles = proposalRoles;
                if (!Global.LFGMgr.CheckGroupRoles(proposalRoles))
                {
                    o = new StringBuilder();
                    foreach (var it in debugRoles)
                        o.AppendFormat(", {0}: {1}", it.Key, GetRolesString(it.Value));

                    Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) Roles not compatible{1}", strGuids, o.ToString());
                    SetCompatibles(strGuids, LfgCompatibility.NoRoles);
                    return LfgCompatibility.NoRoles;
                }

                var itguid = check.First();
                proposalDungeons = QueueDataStore[itguid].dungeons;
                o = new StringBuilder();
                o.AppendFormat(", {0}: ({1})", itguid, Global.LFGMgr.ConcatenateDungeons(proposalDungeons));
                foreach (var guid in check)
                {
                    if (guid == itguid)
                        continue;

                    List<uint> dungeons = QueueDataStore[itguid].dungeons;
                    o.AppendFormat(", {0}: ({1})", guid, Global.LFGMgr.ConcatenateDungeons(dungeons));
                    List<uint> temporal = proposalDungeons.Intersect(dungeons).ToList();
                    proposalDungeons = temporal;
                }

                if (proposalDungeons.Empty())
                {
                    Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) No compatible dungeons{1}", strGuids, o.ToString());
                    SetCompatibles(strGuids, LfgCompatibility.NoDungeons);
                    return LfgCompatibility.NoDungeons;
                }
            }
            else
            {
                ObjectGuid gguid = check.First();
                LfgQueueData queue = QueueDataStore[gguid];
                proposalDungeons = queue.dungeons;
                proposalRoles = queue.roles;
                Global.LFGMgr.CheckGroupRoles(proposalRoles);          // assing new roles
            }

            // Enough players?
            if (numPlayers != MapConst.MaxGroupSize)
            {
                Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) Compatibles but not enough players({1})", strGuids, numPlayers);
                LfgCompatibilityData data = new LfgCompatibilityData(LfgCompatibility.WithLessPlayers);
                data.roles = proposalRoles;

                foreach (var guid in check)
                {
                    var queueData = QueueDataStore.LookupByKey(guid);
                    UpdateBestCompatibleInQueue(guid, queueData, strGuids, data.roles);
                }

                SetCompatibilityData(strGuids, data);
                return LfgCompatibility.WithLessPlayers;
            }

            ObjectGuid _guid = check.First();
            proposal.queues = check;
            proposal.isNew = numLfgGroups != 1 || Global.LFGMgr.GetOldState(_guid) != LfgState.Dungeon;

            if (!Global.LFGMgr.AllQueued(check))
            {
                Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) Group MATCH but can't create proposal!", strGuids);
                SetCompatibles(strGuids, LfgCompatibility.BadStates);
                return LfgCompatibility.BadStates;
            }

            // Create a new proposal
            proposal.cancelTime = Time.UnixTime + SharedConst.LFGTimeProposal;
            proposal.state = LfgProposalState.Initiating;
            proposal.leader = ObjectGuid.Empty;
            proposal.dungeonId = proposalDungeons.SelectRandom();

            bool leader = false;
            foreach (var rolePair in proposalRoles)
            {
                // Assing new leader
                if (rolePair.Value.HasAnyFlag(LfgRoles.Leader))
                {
                    if (!leader || proposal.leader.IsEmpty() || Convert.ToBoolean(RandomHelper.IRand(0, 1)))
                        proposal.leader = rolePair.Key;
                    leader = true;
                }
                else if (!leader && (proposal.leader.IsEmpty() || Convert.ToBoolean(RandomHelper.IRand(0, 1))))
                    proposal.leader = rolePair.Key;

                // Assing player data and roles
                LfgProposalPlayer data = new LfgProposalPlayer();
                data.role = rolePair.Value;
                data.group = proposalGroups.LookupByKey(rolePair.Key);
                if (!proposal.isNew && !data.group.IsEmpty() && data.group == proposal.group) // Player from existing group, autoaccept
                    data.accept = LfgAnswer.Agree;

                proposal.players[rolePair.Key] = data;
            }

            // Mark proposal members as not queued (but not remove queue data)
            foreach (var guid in proposal.queues)
            {
                RemoveFromNewQueue(guid);
                RemoveFromCurrentQueue(guid);
            }

            Global.LFGMgr.AddProposal(proposal);

            Log.outDebug(LogFilter.Lfg, "CheckCompatibility: ({0}) MATCH! Group formed", strGuids);
            SetCompatibles(strGuids, LfgCompatibility.Match);
            return LfgCompatibility.Match;
        }

        public void UpdateQueueTimers(byte queueId, long currTime)
        {
            Log.outDebug(LogFilter.Lfg, "Updating queue timers...");
            foreach (var itQueue in QueueDataStore)
            {
                LfgQueueData queueinfo = itQueue.Value;
                uint dungeonId = queueinfo.dungeons.FirstOrDefault();
                uint queuedTime = (uint)(currTime - queueinfo.joinTime);
                LfgRoles role = LfgRoles.None;
                int waitTime = -1;

                if (!waitTimesTankStore.ContainsKey(dungeonId))
                    waitTimesTankStore[dungeonId] = new LfgWaitTime();
                if (!waitTimesHealerStore.ContainsKey(dungeonId))
                    waitTimesHealerStore[dungeonId] = new LfgWaitTime();
                if (!waitTimesDpsStore.ContainsKey(dungeonId))
                    waitTimesDpsStore[dungeonId] = new LfgWaitTime();
                if (!waitTimesAvgStore.ContainsKey(dungeonId))
                    waitTimesAvgStore[dungeonId] = new LfgWaitTime();

                int wtTank = waitTimesTankStore[dungeonId].time;
                int wtHealer = waitTimesHealerStore[dungeonId].time;
                int wtDps = waitTimesDpsStore[dungeonId].time;
                int wtAvg = waitTimesAvgStore[dungeonId].time;

                foreach (var itPlayer in queueinfo.roles)
                    role |= itPlayer.Value;
                role &= ~LfgRoles.Leader;

                switch (role)
                {
                    case LfgRoles.None:                                // Should not happen - just in case
                        waitTime = -1;
                        break;
                    case LfgRoles.Tank:
                        waitTime = wtTank;
                        break;
                    case LfgRoles.Healer:
                        waitTime = wtHealer;
                        break;
                    case LfgRoles.Damage:
                        waitTime = wtDps;
                        break;
                    default:
                        waitTime = wtAvg;
                        break;
                }

                if (string.IsNullOrEmpty(queueinfo.bestCompatible))
                    FindBestCompatibleInQueue(itQueue.Key, itQueue.Value);

                LfgQueueStatusData queueData = new LfgQueueStatusData(queueId, dungeonId, waitTime, wtAvg, wtTank, wtHealer, wtDps, queuedTime, queueinfo.tanks, queueinfo.healers, queueinfo.dps);
                foreach (var itPlayer in queueinfo.roles)
                {
                    ObjectGuid pguid = itPlayer.Key;
                    Global.LFGMgr.SendLfgQueueStatus(pguid, queueData);
                }
            }
        }

        public long GetJoinTime(ObjectGuid guid)
        {
            var queueData = QueueDataStore.LookupByKey(guid);
            if (queueData != null)
                return queueData.joinTime;

            return 0;
        }

        public string DumpQueueInfo()
        {
            uint players = 0;
            uint groups = 0;
            uint playersInGroup = 0;

            for (byte i = 0; i < 2; ++i)
            {
                List<ObjectGuid> queue = i != 0 ? newToQueueStore : currentQueueStore;
                foreach (var guid in queue)
                {
                    if (guid.IsParty())
                    {
                        groups++;
                        playersInGroup += Global.LFGMgr.GetPlayerCount(guid);
                    }
                    else
                        players++;
                }
            }

            return $"Queued Players: {players} (in group: {playersInGroup}) Groups: {groups}\n";
        }

        public string DumpCompatibleInfo(bool full = false)
        {
            string str = "Compatible Map size: " + CompatibleMapStore.Count + "\n";
            if (full)
            {
                foreach (var pair in CompatibleMapStore)
                    str += "(" + pair.Key + "): " + GetCompatibleString(pair.Value.compatibility) + "\n";
            }
            return str;
        }

        void FindBestCompatibleInQueue(ObjectGuid guid, LfgQueueData data)
        {
            Log.outDebug(LogFilter.Lfg, "FindBestCompatibleInQueue: {0}", guid);

            foreach (var pair in CompatibleMapStore)
            {
                if (pair.Value.compatibility == LfgCompatibility.WithLessPlayers && pair.Key.Contains(guid.ToString()))
                    UpdateBestCompatibleInQueue(guid, data, pair.Key, pair.Value.roles);
            }
        }

        public void UpdateBestCompatibleInQueue(ObjectGuid guid, LfgQueueData queueData, string key, Dictionary<ObjectGuid, LfgRoles> roles)
        {
            byte storedSize = (byte)(string.IsNullOrEmpty(queueData.bestCompatible) ? 0 : queueData.bestCompatible.Count(p => p == '|') + 1);

            byte size = (byte)(key.Count(p => p == '|') + 1);

            if (size <= storedSize)
                return;

            Log.outDebug(LogFilter.Lfg, "UpdateBestCompatibleInQueue: Changed ({0}) to ({1}) as best compatible group for {2}",
                queueData.bestCompatible, key, guid);

            queueData.bestCompatible = key;
            queueData.tanks = SharedConst.LFGTanksNeeded;
            queueData.healers = SharedConst.LFGHealersNeeded;
            queueData.dps = SharedConst.LFGDPSNeeded;
            foreach (var it in roles)
            {
                LfgRoles role = it.Value;
                if (role.HasAnyFlag(LfgRoles.Tank))
                    --queueData.tanks;
                else if (role.HasAnyFlag(LfgRoles.Healer))
                    --queueData.healers;
                else
                    --queueData.dps;
            }
        }

        // Queue
        Dictionary<ObjectGuid, LfgQueueData> QueueDataStore = new Dictionary<ObjectGuid, LfgQueueData>();
        Dictionary<string, LfgCompatibilityData> CompatibleMapStore = new Dictionary<string, LfgCompatibilityData>();

        Dictionary<uint, LfgWaitTime> waitTimesAvgStore = new Dictionary<uint, LfgWaitTime>();
        Dictionary<uint, LfgWaitTime> waitTimesTankStore = new Dictionary<uint, LfgWaitTime>();
        Dictionary<uint, LfgWaitTime> waitTimesHealerStore = new Dictionary<uint, LfgWaitTime>();
        Dictionary<uint, LfgWaitTime> waitTimesDpsStore = new Dictionary<uint, LfgWaitTime>();
        List<ObjectGuid> currentQueueStore = new List<ObjectGuid>();
        List<ObjectGuid> newToQueueStore = new List<ObjectGuid>();
    }

    public class LfgCompatibilityData
    {
        public LfgCompatibilityData()
        {
            compatibility = LfgCompatibility.Pending;
        }

        public LfgCompatibilityData(LfgCompatibility _compatibility)
        {
            compatibility = _compatibility;
        }

        public LfgCompatibilityData(LfgCompatibility _compatibility, Dictionary<ObjectGuid, LfgRoles> _roles)
        {
            compatibility = _compatibility;
            roles = _roles;
        }

        public LfgCompatibility compatibility;
        public Dictionary<ObjectGuid, LfgRoles> roles;
    }

    // Stores player or group queue info
    public class LfgQueueData
    {
        public LfgQueueData()
        {
            joinTime = Time.UnixTime;
            tanks = SharedConst.LFGTanksNeeded;
            healers = SharedConst.LFGHealersNeeded;
            dps = SharedConst.LFGDPSNeeded;
        }

        public LfgQueueData(long _joinTime, List<uint> _dungeons, Dictionary<ObjectGuid, LfgRoles> _roles)
        {
            joinTime = _joinTime;
            tanks = SharedConst.LFGTanksNeeded;
            healers = SharedConst.LFGHealersNeeded;
            dps = SharedConst.LFGDPSNeeded;
            dungeons = _dungeons;
            roles = _roles;
        }

        public long joinTime;
        public byte tanks;
        public byte healers;
        public byte dps;
        public List<uint> dungeons;
        public Dictionary<ObjectGuid, LfgRoles> roles;
        public string bestCompatible = "";
    }

    public struct LfgWaitTime
    {
        public int time;
        public uint number;
    }
}
