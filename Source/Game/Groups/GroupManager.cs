// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
            return GroupDbStore.LookupByKey(storageId);
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

        public void Update(uint diff)
        {
            foreach (var group in GroupStore.Values)
                    group.Update(diff);
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

                // Delete all members that does not exist
                DB.Characters.DirectExecute("DELETE FROM group_member WHERE memberGuid NOT IN (SELECT guid FROM characters)");
                // Delete all groups whose leader does not exist
                DB.Characters.DirectExecute("DELETE FROM `groups` WHERE leaderGuid NOT IN (SELECT guid FROM characters)");
                // Delete all groups with less than 2 members
                DB.Characters.DirectExecute("DELETE FROM `groups` WHERE guid NOT IN (SELECT guid FROM group_member GROUP BY guid HAVING COUNT(guid) > 1)");
                // Delete all rows from group_member with no group
                DB.Characters.DirectExecute("DELETE FROM group_member WHERE guid NOT IN (SELECT guid FROM `groups`)");

                //                                                    0              1           2             3                 4      5          6      7         8       9
                SQLResult result = DB.Characters.Query("SELECT g.leaderGuid, g.lootMethod, g.looterGuid, g.lootThreshold, g.icon1, g.icon2, g.icon3, g.icon4, g.icon5, g.icon6" +
                    //  10         11          12         13              14                  15                     16             17          18         19
                    ", g.icon7, g.icon8, g.groupType, g.difficulty, g.raiddifficulty, g.legacyRaidDifficulty, g.masterLooterGuid, g.guid, lfg.dungeon, lfg.state FROM `groups` g LEFT JOIN lfg_data lfg ON lfg.guid = g.guid ORDER BY g.guid ASC");
                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 group definitions. DB table `groups` is empty!");
                    return;
                }

                uint count = 0;
                do
                {
                    Group group = new();
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
        }

        Dictionary<ulong, Group> GroupStore = new();
        Dictionary<uint, Group> GroupDbStore = new();
        ulong NextGroupId;
        uint NextGroupDbStoreId;
    }
}
