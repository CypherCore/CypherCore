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

using Framework.Database;
using Game.Maps;
using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    public class FormationMgr
    {
        public static void AddCreatureToGroup(uint groupId, Creature member)
        {
            Map map = member.GetMap();
            if (!map)
                return;

            var creatureGroup = map.CreatureGroupHolder.LookupByKey(groupId);

            //Add member to an existing group
            if (creatureGroup != null)
            {
                Log.outDebug(LogFilter.Unit, "Group found: {0}, inserting creature GUID: {1}, Group InstanceID {2}", groupId, member.GetGUID().ToString(), member.GetInstanceId());
                creatureGroup.AddMember(member);
            }
            //Create new group
            else
            {
                Log.outDebug(LogFilter.Unit, "Group not found: {0}. Creating new group.", groupId);
                CreatureGroup group = new CreatureGroup(groupId);
                map.CreatureGroupHolder[groupId] = group;
                group.AddMember(member);
            }
        }

        public static void RemoveCreatureFromGroup(CreatureGroup group, Creature member)
        {
            Log.outDebug(LogFilter.Unit, "Deleting member GUID: {0} from group {1}", group.GetId(), member.GetSpawnId());
            group.RemoveMember(member);

            if (group.isEmpty())
            {
                Map map = member.GetMap();
                if (!map)
                    return;

                Log.outDebug(LogFilter.Unit, "Deleting group with InstanceID {0}", member.GetInstanceId());
                map.CreatureGroupHolder.Remove(group.GetId());
            }
        }

        public static void LoadCreatureFormations()
        {
            uint oldMSTime = Time.GetMSTime();

            CreatureGroupMap.Clear();

            //Get group data
            SQLResult result = DB.World.Query("SELECT leaderGUID, memberGUID, dist, angle, groupAI, point_1, point_2 FROM creature_formations ORDER BY leaderGUID");

            if (result.IsEmpty())
            {
                Log.outError(LogFilter.ServerLoading, "Loaded 0 creatures in formations. DB table `creature_formations` is empty!");
                return;
            }

            uint count = 0;
            FormationInfo group_member;
            do
            {
                //Load group member data
                group_member = new FormationInfo();
                group_member.leaderGUID = result.Read<uint>(0);
                uint memberGUID = result.Read<uint>(1);
                group_member.groupAI = result.Read<uint>(4);
                group_member.point_1 = result.Read<ushort>(5);
                group_member.point_2 = result.Read<ushort>(6);
                //If creature is group leader we may skip loading of dist/angle
                if (group_member.leaderGUID != memberGUID)
                {
                    group_member.follow_dist = result.Read<float>(2);
                    group_member.follow_angle = result.Read<float>(3) * MathFunctions.PI / 180;
                }
                else
                {
                    group_member.follow_dist = 0;
                    group_member.follow_angle = 0;
                }

                // check data correctness
                {
                    if (Global.ObjectMgr.GetCreatureData(group_member.leaderGUID) == null)
                    {
                        Log.outError(LogFilter.Sql, "creature_formations table leader guid {0} incorrect (not exist)", group_member.leaderGUID);
                        continue;
                    }

                    if (Global.ObjectMgr.GetCreatureData(memberGUID) == null)
                    {
                        Log.outError(LogFilter.Sql, "creature_formations table member guid {0} incorrect (not exist)", memberGUID);
                        continue;
                    }
                }

                CreatureGroupMap[memberGUID] = group_member;
                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creatures in formations in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public static Dictionary<ulong, FormationInfo> CreatureGroupMap = new Dictionary<ulong, FormationInfo>();
    }

    public class FormationInfo
    {
        public uint leaderGUID;
        public float follow_dist;
        public float follow_angle;
        public uint groupAI;
        public ushort point_1;
        public ushort point_2;
    }

    public class CreatureGroup
    {
        public CreatureGroup(uint id)
        {
            m_groupID = id;
        }

        public void AddMember(Creature member)
        {
            Log.outDebug(LogFilter.Unit, "CreatureGroup.AddMember: Adding unit GUID: {0}.", member.GetGUID().ToString());

            //Check if it is a leader
            if (member.GetSpawnId() == m_groupID)
            {
                Log.outDebug(LogFilter.Unit, "Unit GUID: {0} is formation leader. Adding group.", member.GetGUID().ToString());
                m_leader = member;
            }

            m_members[member] = FormationMgr.CreatureGroupMap.LookupByKey(member.GetSpawnId());
            member.SetFormation(this);
        }

        public void RemoveMember(Creature member)
        {
            if (m_leader == member)
                m_leader = null;

            m_members.Remove(member);
            member.SetFormation(null);
        }

        public void MemberAttackStart(Creature member, Unit target)
        {
            GroupAIFlags groupAI = (GroupAIFlags)FormationMgr.CreatureGroupMap[member.GetSpawnId()].groupAI;
            if (groupAI == 0)
                return;

            if (member == m_leader)
            {
                if (!groupAI.HasAnyFlag(GroupAIFlags.MembersAssistLeader))
                    return;
            }
            else if (!groupAI.HasAnyFlag(GroupAIFlags.LeaderAssistsMember))
                return;

            foreach (var pair in m_members)
            {
                if (m_leader) // avoid crash if leader was killed and reset.
                    Log.outDebug(LogFilter.Unit, "GROUP ATTACK: group instance id {0} calls member instid {1}", m_leader.GetInstanceId(), member.GetInstanceId());

                Creature other = pair.Key;

                // Skip self
                if (other == member)
                    continue;

                if (!other.IsAlive())
                    continue;

                if (other.GetVictim())
                    continue;

                if (((other != m_leader && groupAI.HasAnyFlag(GroupAIFlags.MembersAssistLeader)) || (other == m_leader && groupAI.HasAnyFlag(GroupAIFlags.LeaderAssistsMember))) && other.IsValidAttackTarget(target))
                    other.GetAI().AttackStart(target);
            }
        }

        public void FormationReset(bool dismiss)
        {
            foreach (var creature in m_members.Keys)
            {
                if (creature != m_leader && creature.IsAlive())
                {
                    if (dismiss)
                        creature.GetMotionMaster().Initialize();
                    else
                        creature.GetMotionMaster().MoveIdle();
                    Log.outDebug(LogFilter.Unit, "Set {0} movement for member GUID: {1}", dismiss ? "default" : "idle", creature.GetGUID().ToString());
                }
            }
            m_Formed = !dismiss;
        }

        public void LeaderMoveTo(float x, float y, float z)
        {
            //! To do: This should probably get its own movement generator or use WaypointMovementGenerator.
            //! If the leader's path is known, member's path can be plotted as well using formation offsets.
            if (!m_leader)
                return;

            float pathangle = (float)Math.Atan2(m_leader.GetPositionY() - y, m_leader.GetPositionX() - x);

            foreach (var pair in m_members)
            {
                Creature member = pair.Key;
                if (member == m_leader || !member.IsAlive() || member.GetVictim() || !pair.Value.groupAI.HasAnyFlag((uint)GroupAIFlags.IdleInFormation))
                    continue;

                if (pair.Value.point_1 != 0)
                    if (m_leader.GetCurrentWaypointID() == pair.Value.point_1 - 1 || m_leader.GetCurrentWaypointID() == pair.Value.point_2 - 1)
                        pair.Value.follow_angle = (float)Math.PI * 2 - pair.Value.follow_angle;

                float angle = pair.Value.follow_angle;
                float dist = pair.Value.follow_dist;

                float dx = x + (float)Math.Cos(angle + pathangle) * dist;
                float dy = y + (float)Math.Sin(angle + pathangle) * dist;
                float dz = z;

                GridDefines.NormalizeMapCoord(ref dx);
                GridDefines.NormalizeMapCoord(ref dy);

                if (!member.IsFlying())
                    member.UpdateGroundPositionZ(dx, dy, ref dz);

                if (member.IsWithinDist(m_leader, dist + 5.0f))
                    member.SetUnitMovementFlags(m_leader.GetUnitMovementFlags());
                else
                    member.SetWalk(false);

                member.GetMotionMaster().MovePoint(0, dx, dy, dz);
                member.SetHomePosition(dx, dy, dz, pathangle);
            }
        }

        public Creature getLeader() { return m_leader; }
        public uint GetId() { return m_groupID; }
        public bool isEmpty() { return m_members.Empty(); }
        public bool isFormed() { return m_Formed; }

        Creature m_leader;
        Dictionary<Creature, FormationInfo> m_members = new Dictionary<Creature, FormationInfo>();

        uint m_groupID;
        bool m_Formed;
    }
}
