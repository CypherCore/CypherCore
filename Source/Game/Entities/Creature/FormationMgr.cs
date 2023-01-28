// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Framework.Database;
using Game.Maps;

namespace Game.Entities
{
	public static class FormationMgr
	{
		private static readonly Dictionary<ulong, FormationInfo> _creatureGroupMap = new();
		private static readonly Dictionary<ulong, List<ulong>> _leaderToMembers = new();

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
			Log.outDebug(LogFilter.Unit, "Deleting member GUID: {0} from group {1}", group.GetLeaderSpawnId(), member.GetSpawnId());
			group.RemoveMember(member);

			if (group.IsEmpty())
			{
				Map map = member.GetMap();

				Log.outDebug(LogFilter.Unit, "Deleting group with InstanceID {0}", member.GetInstanceId());
				Cypher.Assert(map.CreatureGroupHolder.ContainsKey(group.GetLeaderSpawnId()), $"Not registered group {group.GetLeaderSpawnId()} in map {map.GetId()}");
				map.CreatureGroupHolder.Remove(group.GetLeaderSpawnId());
			}
		}

		public static void LoadCreatureFormations()
		{
			uint oldMSTime = Time.GetMSTime();

			//Get group _data
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
				//Load group member _data
				FormationInfo member = new();
				member.LeaderSpawnId = result.Read<ulong>(0);
				ulong memberSpawnId = result.Read<ulong>(1);
				member.FollowDist  = 0f;
				member.FollowAngle = 0f;

				//If creature is group leader we may skip loading of dist/angle
				if (member.LeaderSpawnId != memberSpawnId)
				{
					member.FollowDist  = result.Read<float>(2);
					member.FollowAngle = result.Read<float>(3) * MathFunctions.PI / 180;
				}

				member.GroupAI = result.Read<uint>(4);

				for (var i = 0; i < 2; ++i)
					member.LeaderWaypointIDs[i] = result.Read<ushort>(5 + i);

				// check _data correctness
				{
					if (Global.ObjectMgr.GetCreatureData(member.LeaderSpawnId) == null)
					{
						Log.outError(LogFilter.Sql, $"creature_formations table leader Guid {member.LeaderSpawnId} incorrect (not exist)");

						continue;
					}

					if (Global.ObjectMgr.GetCreatureData(memberSpawnId) == null)
					{
						Log.outError(LogFilter.Sql, $"creature_formations table member Guid {memberSpawnId} incorrect (not exist)");

						continue;
					}

					leaderSpawnIds.Add(member.LeaderSpawnId);
				}

                _leaderToMembers.AddToDictList(member.LeaderSpawnId, memberSpawnId);

                _creatureGroupMap.Add(memberSpawnId, member);
				++count;
			} while (result.NextRow());

			foreach (ulong leaderSpawnId in leaderSpawnIds)
				if (!_creatureGroupMap.ContainsKey(leaderSpawnId))
				{
					Log.outError(LogFilter.Sql, $"creature_formation contains leader spawn {leaderSpawnId} which is not included on its formation, removing");

					foreach (var itr in _creatureGroupMap.ToList())
						if (itr.Value.LeaderSpawnId == leaderSpawnId)
							_creatureGroupMap.Remove(itr.Key);
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
			member.FollowDist    = followDist;
			member.FollowAngle   = followAng;
			member.GroupAI       = groupAI;

			for (var i = 0; i < 2; ++i)
				member.LeaderWaypointIDs[i] = 0;

			_creatureGroupMap.Add(spawnId, member);
		}

        public static List<ulong> GetMembers(ulong leaderId)
        {
            return _leaderToMembers.LookupByKey(leaderId);
        }
	}
}