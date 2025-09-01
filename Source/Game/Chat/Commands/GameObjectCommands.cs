// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Chat
{
    [CommandGroup("gobject")]
    class GameObjectCommands
    {
        [Command("activate", RBACPermissions.CommandGobjectActivate)]
        static bool HandleGameObjectActivateCommand(CommandHandler handler, VariantArg<GameobjectLinkData, ulong> guidLow)
        {
            GameObject obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);
            if (obj == null)
            {
                handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);
                return false;
            }

            uint autoCloseTime = obj.GetGoInfo().GetAutoCloseTime() != 0 ? 10000u : 0u;

            // Activate
            obj.SetLootState(LootState.Ready);
            obj.UseDoorOrButton(autoCloseTime, false, handler.GetSession().GetPlayer());

            handler.SendSysMessage("Object activated!");
            return true;
        }

        [Command("delete", RBACPermissions.CommandGobjectDelete)]
        static bool HandleGameObjectDeleteCommand(CommandHandler handler, VariantArg<GameobjectLinkData, ulong> spawnId)
        {
            GameObject obj = handler.GetObjectFromPlayerMapByDbGuid(spawnId);
            if (obj != null)
            {
                Player player = handler.GetSession().GetPlayer();
                ObjectGuid ownerGuid = obj.GetOwnerGUID();
                if (!ownerGuid.IsEmpty())
                {
                    Unit owner = Global.ObjAccessor.GetUnit(player, ownerGuid);
                    if (owner == null || !ownerGuid.IsPlayer())
                    {
                        handler.SendSysMessage(CypherStrings.CommandDelobjrefercreature, ownerGuid.ToString(), obj.GetGUID().ToString());
                        return false;
                    }

                    owner.RemoveGameObject(obj, false);
                }
            }

            if (GameObject.DeleteFromDB(spawnId))
            {
                handler.SendSysMessage(CypherStrings.CommandDelobjmessage, spawnId);
                return true;
            }

            handler.SendSysMessage(CypherStrings.CommandObjnotfound, obj.GetGUID().ToString());
            return false;
        }

        [Command("despawngroup", RBACPermissions.CommandGobjectDespawngroup)]
        static bool HandleGameObjectDespawnGroup(CommandHandler handler, string[] opts)
        {
            if (opts == null || opts.Empty())
                return false;

            bool deleteRespawnTimes = false;
            uint groupId = 0;

            // Decode arguments
            foreach (var variant in opts)
            {
                if (variant.Equals("removerespawntime", StringComparison.OrdinalIgnoreCase))
                    deleteRespawnTimes = true;
                else
                    uint.TryParse(variant, out groupId);
            }

            Player player = handler.GetSession().GetPlayer();

            int n = 0;
            if (!player.GetMap().SpawnGroupDespawn(groupId, deleteRespawnTimes, out n))
            {
                handler.SendSysMessage(CypherStrings.SpawngroupBadgroup, groupId);
                return false;
            }

            handler.SendSysMessage($"Despawned a total of {n} objects.");

            return true;
        }

        [Command("info", RBACPermissions.CommandGobjectInfo)]
        static bool HandleGameObjectInfoCommand(CommandHandler handler, OptionalArg<string> isGuid, VariantArg<GameobjectEntryLinkData, GameobjectLinkData, uint> data)
        {
            GameObject thisGO = null;
            GameObjectData spawnData = null;

            uint entry;
            ulong spawnId = 0;
            if (!isGuid.HasValue && isGuid.Value.Equals("guid", StringComparison.OrdinalIgnoreCase))
            {
                spawnId = data;
                spawnData = Global.ObjectMgr.GetGameObjectData(spawnId);
                if (spawnData == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandObjnotfound, spawnId);
                    return false;
                }

                entry = spawnData.Id;
                thisGO = handler.GetObjectFromPlayerMapByDbGuid(spawnId);
            }
            else
            {
                entry = (uint)data;
            }

            GameObjectTemplate gameObjectInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);
            if (gameObjectInfo == null)
            {
                handler.SendSysMessage(CypherStrings.GameobjectNotExist, entry);
                return false;
            }

            GameObjectTypes type = gameObjectInfo.type;
            uint displayId = gameObjectInfo.displayId;
            string name = gameObjectInfo.name;
            uint lootId = gameObjectInfo.GetLootId();
            if (type == GameObjectTypes.Chest && lootId == 0)
                lootId = gameObjectInfo.Chest.chestPersonalLoot;

            // If we have a real object, send some info about it
            if (thisGO != null)
            {
                handler.SendSysMessage(CypherStrings.SpawninfoGuidinfo, thisGO.GetGUID().ToString());
                handler.SendSysMessage(CypherStrings.SpawninfoCompatibilityMode, thisGO.GetRespawnCompatibilityMode());

                if (thisGO.GetGameObjectData() != null && thisGO.GetGameObjectData().spawnGroupData.groupId != 0)
                {
                    SpawnGroupTemplateData groupData = thisGO.ToGameObject().GetGameObjectData().spawnGroupData;
                    handler.SendSysMessage(CypherStrings.SpawninfoGroupId, groupData.name, groupData.groupId, groupData.flags, thisGO.GetMap().IsSpawnGroupActive(groupData.groupId));
                }

                GameObjectOverride goOverride = Global.ObjectMgr.GetGameObjectOverride(spawnId);
                if (goOverride == null)
                    goOverride = Global.ObjectMgr.GetGameObjectTemplateAddon(entry);
                if (goOverride != null)
                    handler.SendSysMessage(CypherStrings.GoinfoAddon, goOverride.Faction, goOverride.Flags);
            }

            if (spawnData != null)
            {
                spawnData.rotation.toEulerAnglesZYX(out float yaw, out float pitch, out float roll);
                handler.SendSysMessage(CypherStrings.SpawninfoSpawnidLocation, spawnData.SpawnId, spawnData.SpawnPoint.GetPositionX(), spawnData.SpawnPoint.GetPositionY(), spawnData.SpawnPoint.GetPositionZ());
                handler.SendSysMessage(CypherStrings.SpawninfoRotation, yaw, pitch, roll);
            }

            handler.SendSysMessage(CypherStrings.GoinfoEntry, entry);
            handler.SendSysMessage(CypherStrings.GoinfoType, type);
            handler.SendSysMessage(CypherStrings.GoinfoLootid, lootId);
            handler.SendSysMessage(CypherStrings.GoinfoDisplayid, displayId);
            handler.SendSysMessage(CypherStrings.GoinfoName, name);
            handler.SendSysMessage(CypherStrings.GoinfoSize, gameObjectInfo.size);

            handler.SendSysMessage(CypherStrings.ObjectinfoAiInfo, gameObjectInfo.AIName, Global.ObjectMgr.GetScriptName(gameObjectInfo.ScriptId));
            var ai = thisGO != null ? thisGO.GetAI() : null;
            if (ai != null)
                handler.SendSysMessage(CypherStrings.ObjectinfoAiType, nameof(ai));

            GameObjectDisplayInfoRecord modelInfo = CliDB.GameObjectDisplayInfoStorage.LookupByKey(displayId);
            if (modelInfo != null)
                handler.SendSysMessage(CypherStrings.GoinfoModel, modelInfo.GeoBoxMax.X, modelInfo.GeoBoxMax.Y, modelInfo.GeoBoxMax.Z, modelInfo.GeoBoxMin.X, modelInfo.GeoBoxMin.Y, modelInfo.GeoBoxMin.Z);

            return true;
        }

        [Command("move", RBACPermissions.CommandGobjectMove)]
        static bool HandleGameObjectMoveCommand(CommandHandler handler, VariantArg<GameobjectLinkData, ulong> guidLow, OptionalArg<float[]> xyz)
        {
            GameObject obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);
            if (obj == null)
            {
                handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);
                return false;
            }

            Position pos;
            if (xyz.HasValue)
            {
                pos = new Position(xyz.Value[0], xyz.Value[1], xyz.Value[2]);
                if (!GridDefines.IsValidMapCoord(obj.GetMapId(), pos))
                {
                    handler.SendSysMessage(CypherStrings.InvalidTargetCoord, pos.GetPositionX(), pos.GetPositionY(), obj.GetMapId());
                    return false;
                }
            }
            else
            {
                pos = handler.GetSession().GetPlayer().GetPosition();
            }

            Map map = obj.GetMap();

            pos.SetOrientation(obj.GetOrientation());
            obj.Relocate(pos);

            // update which cell has this gameobject registered for loading
            Global.ObjectMgr.RemoveGameObjectFromGrid(obj.GetGameObjectData());
            obj.SaveToDB();
            Global.ObjectMgr.AddGameObjectToGrid(obj.GetGameObjectData());

            // Generate a completely new spawn with new guid
            // client caches recently deleted objects and brings them back to life
            // when CreateObject block for this guid is received again
            // however it entirely skips parsing that block and only uses already known location
            obj.Delete();

            obj = GameObject.CreateGameObjectFromDB(guidLow, map);
            if (obj == null)
                return false;

            handler.SendSysMessage(CypherStrings.CommandMoveobjmessage, obj.GetSpawnId(), obj.GetGoInfo().name, obj.GetGUID().ToString());

            return true;
        }

        [Command("near", RBACPermissions.CommandGobjectNear)]
        static bool HandleGameObjectNearCommand(CommandHandler handler, OptionalArg<float> dist)
        {
            float distance = dist.GetValueOrDefault(10f);
            uint count = 0;

            Player player = handler.GetPlayer();

            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_GAMEOBJECT_NEAREST);
            stmt.AddValue(0, player.GetPositionX());
            stmt.AddValue(1, player.GetPositionY());
            stmt.AddValue(2, player.GetPositionZ());
            stmt.AddValue(3, player.GetMapId());
            stmt.AddValue(4, player.GetPositionX());
            stmt.AddValue(5, player.GetPositionY());
            stmt.AddValue(6, player.GetPositionZ());
            stmt.AddValue(7, distance * distance);
            SQLResult result = DB.World.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    ulong guid = result.Read<ulong>(0);
                    uint entry = result.Read<uint>(1);
                    float x = result.Read<float>(2);
                    float y = result.Read<float>(3);
                    float z = result.Read<float>(4);
                    ushort mapId = result.Read<ushort>(5);

                    GameObjectTemplate gameObjectInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);
                    if (gameObjectInfo == null)
                        continue;

                    handler.SendSysMessage(CypherStrings.GoListChat, guid, entry, guid, gameObjectInfo.name, x, y, z, mapId, "", "");

                    ++count;
                } while (result.NextRow());
            }

            handler.SendSysMessage(CypherStrings.CommandNearobjmessage, distance, count);
            return true;
        }

        [Command("spawngroup", RBACPermissions.CommandGobjectSpawngroup)]
        static bool HandleGameObjectSpawnGroup(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            bool ignoreRespawn = false;
            bool force = false;
            uint groupId = 0;

            // Decode arguments
            string arg = args.NextString();
            while (!arg.IsEmpty())
            {
                string thisArg = arg.ToLower();
                if (thisArg == "ignorerespawn")
                    ignoreRespawn = true;
                else if (thisArg == "force")
                    force = true;
                else if (thisArg.IsEmpty() || !thisArg.IsNumber())
                    return false;
                else
                    groupId = uint.Parse(thisArg);

                arg = args.NextString();
            }

            Player player = handler.GetSession().GetPlayer();

            List<WorldObject> creatureList = new();
            if (!player.GetMap().SpawnGroupSpawn(groupId, ignoreRespawn, force, creatureList))
            {
                handler.SendSysMessage(CypherStrings.SpawngroupBadgroup, groupId);
                return false;
            }

            handler.SendSysMessage(CypherStrings.SpawngroupSpawncount, creatureList.Count);
            foreach (WorldObject obj in creatureList)
                handler.SendSysMessage($"{obj.GetName()} ({obj.GetGUID()})");

            return true;
        }

        [Command("target", RBACPermissions.CommandGobjectTarget)]
        static bool HandleGameObjectTargetCommand(CommandHandler handler, OptionalArg<VariantArg<VariantArg<GameobjectEntryLinkData, uint>, string>> objectId)
        {
            Player player = handler.GetSession().GetPlayer();
            SQLResult result;
            var activeEventsList = Global.GameEventMgr.GetActiveEventList();

            if (objectId.HasValue)
            {
                if (objectId.Value.Is<VariantArg<GameobjectEntryLinkData, uint>>())
                    result = DB.World.Query("SELECT guid, id, position_x, position_y, position_z, orientation, map, PhaseId, PhaseGroup, (POW(position_x - '{0}', 2) + POW(position_y - '{1}', 2) + POW(position_z - '{2}', 2)) AS order_ FROM gameobject WHERE map = '{3}' AND id = '{4}' ORDER BY order_ ASC LIMIT 1",
                    player.GetPositionX(), player.GetPositionY(), player.GetPositionZ(), player.GetMapId(), objectId);
                else
                {
                    string name = objectId.Value.GetValue();
                    result = DB.World.Query(
                        "SELECT guid, id, position_x, position_y, position_z, orientation, map, PhaseId, PhaseGroup, (POW(position_x - {0}, 2) + POW(position_y - {1}, 2) + POW(position_z - {2}, 2)) AS order_ " +
                        "FROM gameobject LEFT JOIN gameobject_template ON gameobject_template.entry = gameobject.id WHERE map = {3} AND name LIKE CONCAT('%%', '{4}', '%%') ORDER BY order_ ASC LIMIT 1",
                        player.GetPositionX(), player.GetPositionY(), player.GetPositionZ(), player.GetMapId(), name);
                }
            }
            else
            {
                StringBuilder eventFilter = new();
                eventFilter.Append(" AND (eventEntry IS NULL ");
                bool initString = true;

                foreach (var entry in activeEventsList)
                {
                    if (initString)
                    {
                        eventFilter.Append("OR eventEntry IN (" + entry);
                        initString = false;
                    }
                    else
                        eventFilter.Append(',' + entry);
                }

                if (!initString)
                    eventFilter.Append("))");
                else
                    eventFilter.Append(')');

                result = DB.World.Query("SELECT gameobject.guid, id, position_x, position_y, position_z, orientation, map, PhaseId, PhaseGroup, " +
                    "(POW(position_x - {0}, 2) + POW(position_y - {1}, 2) + POW(position_z - {2}, 2)) AS order_ FROM gameobject " +
                    "LEFT OUTER JOIN game_event_gameobject on gameobject.guid = game_event_gameobject.guid WHERE map = '{3}' {4} ORDER BY order_ ASC LIMIT 10",
                    handler.GetSession().GetPlayer().GetPositionX(), handler.GetSession().GetPlayer().GetPositionY(), handler.GetSession().GetPlayer().GetPositionZ(),
                    handler.GetSession().GetPlayer().GetMapId(), eventFilter.ToString());
            }

            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.CommandTargetobjnotfound);
                return true;
            }

            bool found = false;
            float x, y, z, o;
            ulong guidLow;
            uint id, phaseId, phaseGroup;
            ushort mapId;
            uint poolId;

            do
            {
                guidLow = result.Read<ulong>(0);
                id = result.Read<uint>(1);
                x = result.Read<float>(2);
                y = result.Read<float>(3);
                z = result.Read<float>(4);
                o = result.Read<float>(5);
                mapId = result.Read<ushort>(6);
                phaseId = result.Read<uint>(7);
                phaseGroup = result.Read<uint>(8);
                poolId = Global.PoolMgr.IsPartOfAPool<GameObject>(guidLow);
                if (poolId == 0 || Global.PoolMgr.IsSpawnedObject<GameObject>(guidLow))
                    found = true;
            } while (result.NextRow() && !found);

            if (!found)
            {
                handler.SendSysMessage(CypherStrings.GameobjectNotExist, id);
                return false;
            }

            GameObjectTemplate objectInfo = Global.ObjectMgr.GetGameObjectTemplate(id);

            if (objectInfo == null)
            {
                handler.SendSysMessage(CypherStrings.GameobjectNotExist, id);
                return false;
            }

            GameObject target = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

            handler.SendSysMessage(CypherStrings.GameobjectDetail, guidLow, objectInfo.name, guidLow, id, x, y, z, mapId, o, phaseId, phaseGroup);

            if (target != null)
            {
                int curRespawnDelay = (int)(target.GetRespawnTimeEx() - GameTime.GetGameTime());
                if (curRespawnDelay < 0)
                    curRespawnDelay = 0;

                string curRespawnDelayStr = Time.secsToTimeString((uint)curRespawnDelay, TimeFormat.ShortText);
                string defRespawnDelayStr = Time.secsToTimeString(target.GetRespawnDelay(), TimeFormat.ShortText);

                handler.SendSysMessage(CypherStrings.CommandRawpawntimes, defRespawnDelayStr, curRespawnDelayStr);
            }
            return true;
        }

        [Command("turn", RBACPermissions.CommandGobjectTurn)]
        static bool HandleGameObjectTurnCommand(CommandHandler handler, VariantArg<GameobjectLinkData, ulong> guidLow, OptionalArg<float> oz, OptionalArg<float> oy, OptionalArg<float> ox)
        {
            GameObject obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);
            if (obj == null)
            {
                handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);
                return false;
            }

            if (!oz.HasValue)
                oz = handler.GetSession().GetPlayer().GetOrientation();

            Map map = obj.GetMap();

            obj.Relocate(obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ(), oz.Value);
            obj.SetLocalRotationAngles(oz.Value, oy.GetValueOrDefault(0f), ox.GetValueOrDefault(0f));
            obj.SaveToDB();

            // Generate a completely new spawn with new guid
            // client caches recently deleted objects and brings them back to life
            // when CreateObject block for this guid is received again
            // however it entirely skips parsing that block and only uses already known location
            obj.Delete();

            obj = GameObject.CreateGameObjectFromDB(guidLow, map);
            if (obj == null)
                return false;

            handler.SendSysMessage(CypherStrings.CommandTurnobjmessage, obj.GetSpawnId(), obj.GetGoInfo().name, obj.GetGUID().ToString(), obj.GetOrientation());

            return true;
        }

        [CommandGroup("add")]
        class AddCommands
        {
            [Command("", RBACPermissions.CommandGobjectAdd)]
            static bool HandleGameObjectAddCommand(CommandHandler handler, VariantArg<GameobjectEntryLinkData, uint> objectId, OptionalArg<int> spawnTimeSecs)
            {
                if (objectId == 0)
                    return false;

                GameObjectTemplate objectInfo = Global.ObjectMgr.GetGameObjectTemplate(objectId);
                if (objectInfo == null)
                {
                    handler.SendSysMessage(CypherStrings.GameobjectNotExist, objectId);
                    return false;
                }

                if (objectInfo.displayId != 0 && !CliDB.GameObjectDisplayInfoStorage.ContainsKey(objectInfo.displayId))
                {
                    // report to DB errors log as in loading case
                    Log.outError(LogFilter.Sql, "Gameobject (Entry {0} GoType: {1}) have invalid displayId ({2}), not spawned.", objectId, objectInfo.type, objectInfo.displayId);
                    handler.SendSysMessage(CypherStrings.GameobjectHaveInvalidData, objectId);
                    return false;
                }

                Player player = handler.GetPlayer();
                Map map = player.GetMap();

                GameObject obj = GameObject.CreateGameObject(objectInfo.entry, map, player, Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(player.GetOrientation(), 0.0f, 0.0f)), 255, GameObjectState.Ready);
                if (obj == null)
                    return false;

                PhasingHandler.InheritPhaseShift(obj, player);

                if (spawnTimeSecs.HasValue)
                    obj.SetRespawnTime(spawnTimeSecs.Value);

                // fill the gameobject data and save to the db
                obj.SaveToDB(map.GetId(), new List<Difficulty>() { map.GetDifficultyID() });
                ulong spawnId = obj.GetSpawnId();

                // this will generate a new guid if the object is in an instance
                obj = GameObject.CreateGameObjectFromDB(spawnId, map);
                if (obj == null)
                    return false;

                // TODO: is it really necessary to add both the real and DB table guid here ?
                Global.ObjectMgr.AddGameObjectToGrid(Global.ObjectMgr.GetGameObjectData(spawnId));
                handler.SendSysMessage(CypherStrings.GameobjectAdd, objectId, objectInfo.name, spawnId, player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());
                return true;
            }

            [Command("temp", RBACPermissions.CommandGobjectAddTemp)]
            static bool HandleGameObjectAddTempCommand(CommandHandler handler, VariantArg<GameobjectEntryLinkData, uint> objectId, OptionalArg<ulong> spawntime)
            {
                Player player = handler.GetPlayer();
                TimeSpan spawntm = TimeSpan.FromSeconds(spawntime.GetValueOrDefault(300));

                Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(player.GetOrientation(), 0.0f, 0.0f));
                if (Global.ObjectMgr.GetGameObjectTemplate(objectId) == null)
                {
                    handler.SendSysMessage(CypherStrings.GameobjectNotExist, objectId);
                    return false;
                }

                player.SummonGameObject(objectId, player, rotation, spawntm);

                return true;
            }
        }

        [CommandGroup("set")]
        class SetCommands
        {
            [Command("phase", RBACPermissions.CommandGobjectSetPhase)]
            static bool HandleGameObjectSetPhaseCommand(CommandHandler handler, VariantArg<GameobjectLinkData, ulong> guidLow, uint phaseId)
            {
                if (guidLow == 0)
                    return false;

                GameObject obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);
                if (obj == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);
                    return false;
                }

                if (phaseId == 0)
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                PhasingHandler.AddPhase(obj, phaseId, true);
                obj.SaveToDB();
                return true;
            }

            [Command("state", RBACPermissions.CommandGobjectSetState)]
            static bool HandleGameObjectSetStateCommand(CommandHandler handler, VariantArg<GameobjectLinkData, ulong> guidLow, int objectType, OptionalArg<uint> objectState)
            {
                if (guidLow == 0)
                    return false;

                GameObject obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);
                if (obj == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);
                    return false;
                }

                if (objectType < 0)
                {
                    if (objectType == -1)
                        obj.SendGameObjectDespawn();
                    else if (objectType == -2)
                        return false;
                    return true;
                }

                if (!objectState.HasValue)
                    return false;

                switch (objectType)
                {
                    case 0:
                        obj.SetGoState((GameObjectState)objectState.Value);
                        break;
                    case 1:
                        obj.SetGoType((GameObjectTypes)objectState.Value);
                        break;
                    case 2:
                        obj.SetGoArtKit(objectState.Value);
                        break;
                    case 3:
                        obj.SetGoAnimProgress(objectState.Value);
                        break;
                    case 4:
                        obj.SendCustomAnim(objectState.Value);
                        break;
                    case 5:
                        if (objectState.Value < 0 || objectState.Value > (uint)GameObjectDestructibleState.Rebuilding)
                            return false;

                        obj.SetDestructibleState((GameObjectDestructibleState)objectState.Value);
                        break;
                    default:
                        break;
                }

                handler.SendSysMessage("Set gobject type {0} state {1}", objectType, objectState);
                return true;
            }
        }
    }
}
