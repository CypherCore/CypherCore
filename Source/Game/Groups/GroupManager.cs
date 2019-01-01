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
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using System.Collections.Generic;

namespace Game.Groups
{
    public class GroupManager : Singleton<GroupManager>
    {
        GroupManager()
        {
            NextGroupDbStoreId = 1;
            NextGroupId = 1;
        }

        public uint GenerateNewGroupDbStoreId()
        {
            uint newStorageId = NextGroupDbStoreId;

            for (uint i = ++NextGroupDbStoreId; i < 0xFFFFFFFF; ++i)
            {
                if ((i < GroupDbStore.Count && GroupDbStore[i] == null) || i >= GroupDbStore.Count)
                {
                    NextGroupDbStoreId = i;
                    break;
                }
            }

            if (newStorageId == NextGroupDbStoreId)
            {
                Log.outError(LogFilter.Server, "Group storage ID overflow!! Can't continue, shutting down server. ");
                Global.WorldMgr.StopNow();
            }

            return newStorageId;
        }

        public void RegisterGroupDbStoreId(uint storageId, Group group)
        {
            GroupDbStore[storageId] = group;
        }

        public void FreeGroupDbStoreId(Group group)
        {
            uint storageId = group.GetDbStoreId();

            if (storageId < NextGroupDbStoreId)
                NextGroupDbStoreId = storageId;

            GroupDbStore[storageId - 1] = null;
        }

        public Group GetGroupByDbStoreId(uint storageId)
        {
            if (storageId < GroupDbStore.Count)
                return GroupDbStore[storageId];

            return null;
        }

        public ulong GenerateGroupId()
        {
            if (NextGroupId >= 0xFFFFFFFE)
            {
                Log.outError(LogFilter.Server, "Group guid overflow!! Can't continue, shutting down server. ");
                Global.WorldMgr.StopNow();
            }
            return NextGroupId++;
        }

        public Group GetGroupByGUID(ObjectGuid groupId)
        {
            return GroupStore.LookupByKey(groupId.GetCounter());
        }

        public void AddGroup(Group group)
        {
            GroupStore[group.GetGUID().GetCounter()] = group;
        }

        public void RemoveGroup(Group group)
        {
            GroupStore.Remove(group.GetGUID().GetCounter());
        }

        public void LoadGroups()
        {
            {
                uint oldMSTime = Time.GetMSTime();

                // Delete all groups whose leader does not exist
                DB.Characters.DirectExecute("DELETE FROM groups WHERE leaderGuid NOT IN (SELECT guid FROM characters)");
                // Delete all groups with less than 2 members
                DB.Characters.DirectExecute("DELETE FROM groups WHERE guid NOT IN (SELECT guid FROM group_member GROUP BY guid HAVING COUNT(guid) > 1)");

                //                                                    0              1           2             3                 4      5          6      7         8       9
                SQLResult result = DB.Characters.Query("SELECT g.leaderGuid, g.lootMethod, g.looterGuid, g.lootThreshold, g.icon1, g.icon2, g.icon3, g.icon4, g.icon5, g.icon6" +
                    //  10         11          12         13              14                  15                     16             17          18         19
                    ", g.icon7, g.icon8, g.groupType, g.difficulty, g.raiddifficulty, g.legacyRaidDifficulty, g.masterLooterGuid, g.guid, lfg.dungeon, lfg.state FROM groups g LEFT JOIN lfg_data lfg ON lfg.guid = g.guid ORDER BY g.guid ASC");
                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 group definitions. DB table `groups` is empty!");
                    return;
                }

                uint count = 0;
                do
                {
                    Group group = new Group();
                    group.LoadGroupFromDB(result.GetFields());
                    AddGroup(group);

                    // Get the ID used for storing the group in the database and register it in the pool.
                    uint storageId = group.GetDbStoreId();

                    RegisterGroupDbStoreId(storageId, group);

                    // Increase the next available storage ID
                    if (storageId == NextGroupDbStoreId)
                        NextGroupDbStoreId++;

                    ++count;
                }
                while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} group definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Group members...");
            {
                uint oldMSTime = Time.GetMSTime();

                // Delete all rows from group_member or group_instance with no group
                DB.Characters.DirectExecute("DELETE FROM group_member WHERE guid NOT IN (SELECT guid FROM groups)");
                DB.Characters.DirectExecute("DELETE FROM group_instance WHERE guid NOT IN (SELECT guid FROM groups)");
                // Delete all members that does not exist
                DB.Characters.DirectExecute("DELETE FROM group_member WHERE memberGuid NOT IN (SELECT guid FROM characters)");

                //                                                0        1           2            3       4
                SQLResult result = DB.Characters.Query("SELECT guid, memberGuid, memberFlags, subgroup, roles FROM group_member ORDER BY guid");
                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 group members. DB table `group_member` is empty!");
                    return;
                }

                uint count = 0;

                do
                {
                    Group group = GetGroupByDbStoreId(result.Read<uint>(0));

                    if (group)
                        group.LoadMemberFromDB(result.Read<uint>(1), result.Read<byte>(2), result.Read<byte>(3), (LfgRoles)result.Read<byte>(4));
                    else
                        Log.outError(LogFilter.Server, "GroupMgr:LoadGroups: Consistency failed, can't find group (storage id: {0})", result.Read<uint>(0));

                    ++count;
                }
                while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} group members in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Group instance saves...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                0           1        2              3             4             5            6           7
                SQLResult result = DB.Characters.Query("SELECT gi.guid, i.map, gi.instance, gi.permanent, i.difficulty, i.resettime, i.entranceId, COUNT(g.guid) " +
                    "FROM group_instance gi INNER JOIN instance i ON gi.instance = i.id " +
                    "LEFT JOIN character_instance ci LEFT JOIN groups g ON g.leaderGuid = ci.guid ON ci.instance = gi.instance AND ci.permanent = 1 GROUP BY gi.instance ORDER BY gi.guid");
                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 group-instance saves. DB table `group_instance` is empty!");
                    return;
                }

                uint count = 0;
                do
                {
                    Group group = GetGroupByDbStoreId(result.Read<uint>(0));
                    // group will never be NULL (we have run consistency sql's before loading)

                    MapRecord mapEntry = CliDB.MapStorage.LookupByKey(result.Read<ushort>(1));
                    if (mapEntry == null || !mapEntry.IsDungeon())
                    {
                        Log.outError(LogFilter.Sql, "Incorrect entry in group_instance table : no dungeon map {0}", result.Read<ushort>(1));
                        continue;
                    }

                    uint diff = result.Read<byte>(4);
                    DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(diff);
                    if (difficultyEntry == null || difficultyEntry.InstanceType != mapEntry.InstanceType)
                        continue;

                    InstanceSave save = Global.InstanceSaveMgr.AddInstanceSave(mapEntry.Id, result.Read<uint>(2), (Difficulty)diff, result.Read<uint>(5), result.Read<uint>(6), result.Read<ulong>(7) != 0, true);
                    group.BindToInstance(save, result.Read<bool>(3), true);
                    ++count;
                }
                while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} group-instance saves in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        public void Update(uint diff)
        {
            foreach (var group in GroupStore.Values)
            {
                if (group)
                    group.Update(diff);
            }
        }

        Dictionary<ulong, Group> GroupStore = new Dictionary<ulong, Group>();
        Dictionary<uint, Group> GroupDbStore = new Dictionary<uint, Group>();
        ulong NextGroupId;
        uint NextGroupDbStoreId;
    }
}
