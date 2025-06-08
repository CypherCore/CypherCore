// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Database;
using Game.Maps;
using System;
using System.Collections.Generic;
using Framework.Constants;
using System.Linq;

namespace Game.Entities
{
    public class FormationMgr
    {
        static Dictionary<ulong, FormationInfo> _creatureGroupMap = new();

        public static void AddCreatureToGroup(ulong leaderSpawnId, Creature creature)
        {
            Map map = creature.GetMap();

            var creatureGroup = map.CreatureGroupHolder.LookupByKey(leaderSpawnId);
            if (creatureGroup != null)
            {
                //Add member to an existing group
                Log.outDebug(LogFilter.Unit, "Group found: {0}, inserting creature GUID: {1}, Group InstanceID {2}", leaderSpawnId, creature.GetGUID().ToString(), creature.GetInstanceId());

                // With dynamic spawn the creature may have just respawned
                // we need to find previous instance of creature and delete it from the formation, as it'll be invalidated
                var bounds = map.GetCreatureBySpawnIdStore().LookupByKey(creature.GetSpawnId());
                foreach (var other in bounds)
                {
                    if (other == creature)
                        continue;

                    if (creatureGroup.HasMember(other))
                        creatureGroup.RemoveMember(other);
                }

                creatureGroup.AddMember(creature);
            }
            else
            {
                //Create new group
                Log.outDebug(LogFilter.Unit, "Group not found: {0}. Creating new group.", leaderSpawnId);
                CreatureGroup group = new(leaderSpawnId);
                map.CreatureGroupHolder[leaderSpawnId] = group;
                group.AddMember(creature);
            }
        }

        public static void RemoveCreatureFromGroup(CreatureGroup group, Creature member)
        {
            ulong leaderSpawnId = group.GetLeaderSpawnId();

            Log.outDebug(LogFilter.Unit, $"Deleting member GUID: {leaderSpawnId} from group {member.GetSpawnId()}");
            group.RemoveMember(member);

            // If removed member was alive we need to check if we have any other alive members
            // if not - fire OnCreatureGroupDepleted
            ZoneScript script = member.GetZoneScript();
            if (script != null && member.IsAlive() && !group.HasAliveMembers())
                script.OnCreatureGroupDepleted(group);

            if (group.IsEmpty())
            {
                if (leaderSpawnId != 0)
                {
                    Map map = member.GetMap();

                    Log.outDebug(LogFilter.Unit, $"Deleting group with InstanceID {member.GetInstanceId()}");
                    bool erased = map.CreatureGroupHolder.Remove(leaderSpawnId);
                    Cypher.Assert(erased, $"Not registered group {leaderSpawnId} in map {map.GetId()}");

                }
            }
        }

        public static void LoadCreatureFormations()
        {
            uint oldMSTime = Time.GetMSTime();

            //Get group data
            SQLResult result = DB.World.Query("SELECT leaderGUID, memberGUID, dist, angle, groupAI, point_1, point_2 FROM creature_formations ORDER BY leaderGUID");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 creatures in formations. DB table `creature_formations` is empty!");
                return;
            }

            uint count = 0;
            List<ulong> leaderSpawnIds = new();
            do
            {
                //Load group member data
                ulong leaderSpawnId = result.Read<ulong>(0);
                ulong memberSpawnId = result.Read<ulong>(1);

                // check data correctness
                {
                    if (Global.ObjectMgr.GetCreatureData(leaderSpawnId) == null)
                    {
                        Log.outError(LogFilter.Sql, $"creature_formations table leader guid {leaderSpawnId} incorrect (not exist)");
                        continue;
                    }

                    if (Global.ObjectMgr.GetCreatureData(memberSpawnId) == null)
                    {
                        Log.outError(LogFilter.Sql, $"creature_formations table member guid {memberSpawnId} incorrect (not exist)");
                        continue;
                    }

                    leaderSpawnIds.Add(leaderSpawnId);
                }

                FormationInfo member = new();
                member.LeaderSpawnId = leaderSpawnId;
                member.FollowDist = 0.0f;
                member.FollowAngle = 0.0f;

                //If creature is group leader we may skip loading of dist/angle
                if (member.LeaderSpawnId != memberSpawnId)
                {
                    member.FollowDist = result.Read<float>(2);
                    member.FollowAngle = result.Read<float>(3) * MathF.PI / 180.0f;
                }

                member.GroupAI = result.Read<uint>(4);
                for (byte i = 0; i < 2; ++i)
                    member.LeaderWaypointIDs[i] = result.Read<ushort>(5 + i);

                _creatureGroupMap.Add(memberSpawnId, member);
                ++count;
            }
            while (result.NextRow());

            foreach (ulong leaderSpawnId in leaderSpawnIds)
            {
                if (!_creatureGroupMap.ContainsKey(leaderSpawnId))
                {
                    Log.outError(LogFilter.Sql, $"creature_formation contains leader spawn {leaderSpawnId} which is not included on its formation, removing");
                    foreach (var itr in _creatureGroupMap.ToList())
                    {
                        if (itr.Value.LeaderSpawnId == leaderSpawnId)
                            _creatureGroupMap.Remove(itr.Key);
                    }
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creatures in formations in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public static FormationInfo GetFormationInfo(ulong spawnId)
        {
            return _creatureGroupMap.LookupByKey(spawnId);
        }

        public static void AddFormationMember(ulong spawnId, float followAng, float followDist, ulong leaderSpawnId, uint groupAI)
        {
            FormationInfo member = new();
            member.LeaderSpawnId = leaderSpawnId;
            member.FollowDist = followDist;
            member.FollowAngle = followAng;
            member.GroupAI = groupAI;
            for (var i = 0; i < 2; ++i)
                member.LeaderWaypointIDs[i] = 0;

            _creatureGroupMap.Add(spawnId, member);
        }
    }

    public class FormationInfo
    {
        public ulong LeaderSpawnId;
        public float FollowDist;
        public float FollowAngle;
        public uint GroupAI;
        public uint[] LeaderWaypointIDs = new uint[2];
    }

    public class CreatureGroup
    {
        Creature _leader;
        Dictionary<Creature, FormationInfo> _members = new();

        ulong _leaderSpawnId;
        bool _formed;
        bool _engaging;

        public CreatureGroup(ulong leaderSpawnId)
        {
            _leaderSpawnId = leaderSpawnId;
        }

        public void AddMember(Creature member)
        {
            Log.outDebug(LogFilter.Unit, $"CreatureGroup.AddMember: Adding {member.GetGUID()}.");

            //Check if it is a leader
            if ((_leaderSpawnId != 0 && member.GetSpawnId() == _leaderSpawnId)
                || (_leaderSpawnId == 0 && _leader == null))   // in formations made of tempsummons first member to be added is leader
            {
                Log.outDebug(LogFilter.Unit, $"{member.GetGUID()} is formation leader. Adding group.");
                _leader = member;
            }

            _members.Add(member, FormationMgr.GetFormationInfo(member.GetSpawnId()));
            member.SetFormation(this);
        }

        public void RemoveMember(Creature member)
        {
            if (_leader == member)
                _leader = null;

            _members.Remove(member);
            member.SetFormation(null);
        }

        public void MemberEngagingTarget(Creature member, Unit target)
        {
            // used to prevent recursive calls
            if (_engaging)
                return;

            FormationInfo formationInfo = _members.LookupByKey(member);
            if (formationInfo == null)
                return;

            GroupAIFlags groupAI = (GroupAIFlags)formationInfo.GroupAI;
            if (groupAI == 0)
                return;

            if (member == _leader)
            {
                if (!groupAI.HasFlag(GroupAIFlags.MembersAssistLeader))
                    return;
            }
            else if (!groupAI.HasFlag(GroupAIFlags.LeaderAssistsMember))
                return;

            _engaging = true;

            foreach (var (other, _) in _members)
            {
                // Skip self
                if (other == member)
                    continue;

                if (!other.IsAlive())
                    continue;

                if (((other != _leader && groupAI.HasFlag(GroupAIFlags.MembersAssistLeader)) || (other == _leader && groupAI.HasFlag(GroupAIFlags.LeaderAssistsMember))) && other.IsValidAttackTarget(target))
                    other.EngageWithTarget(target);
            }

            _engaging = false;
        }

        public void FormationReset(bool dismiss)
        {
            foreach (var (member, _) in _members)
            {
                if (member != _leader && member.IsAlive())
                {
                    if (dismiss)
                        member.GetMotionMaster().Remove(MovementGeneratorType.Formation, MovementSlot.Default);
                    else
                        member.GetMotionMaster().MoveIdle();
                    Log.outDebug(LogFilter.Unit, $"CreatureGroup::FormationReset: Set {(dismiss ? "default" : "idle")} movement for member {member.GetGUID()}");
                }
            }

            _formed = !dismiss;
        }

        public void LeaderStartedMoving()
        {
            if (_leader == null)
                return;

            foreach (var (member, formationInfo) in _members)
            {
                if (member == _leader || !member.IsAlive() || member.IsEngaged() || formationInfo == null || !formationInfo.GroupAI.HasAnyFlag((uint)GroupAIFlags.IdleInFormation))
                    continue;

                float angle = formationInfo.FollowAngle + MathF.PI; // for some reason, someone thought it was a great idea to invert relativ angles...
                float dist = formationInfo.FollowDist;

                if (member.GetMotionMaster().GetCurrentMovementGeneratorType(MovementSlot.Default) != MovementGeneratorType.Formation)
                    member.GetMotionMaster().MoveFormation(_leader, dist, angle, formationInfo.LeaderWaypointIDs[0], formationInfo.LeaderWaypointIDs[1]);
            }
        }

        public bool CanLeaderStartMoving()
        {
            foreach (var (member, _) in _members)
            {
                if (member != _leader && member.IsAlive())
                {
                    if (member.IsEngaged() || member.IsReturningHome())
                        return false;
                }
            }

            return true;
        }

        public bool HasAliveMembers()
        {
            return _members.Any(pair => pair.Key.IsAlive());
        }

        public bool LeaderHasStringId(string id)
        {
            if (_leader != null)
                return _leader.HasStringId(id);

            CreatureData leaderCreatureData = Global.ObjectMgr.GetCreatureData(_leaderSpawnId);
            if (leaderCreatureData != null)
            {
                if (leaderCreatureData.StringId == id)
                    return true;

                if (Global.ObjectMgr.GetCreatureTemplate(leaderCreatureData.Id).StringId == id)
                    return true;
            }

            return false;
        }

        public Creature GetLeader() { return _leader; }
        public ulong GetLeaderSpawnId() { return _leaderSpawnId; }
        public bool IsEmpty() { return _members.Empty(); }
        public bool IsFormed() { return _formed; }
        public bool IsLeader(Creature creature) { return _leader == creature; }

        public bool HasMember(Creature member) { return _members.ContainsKey(member); }
    }
}