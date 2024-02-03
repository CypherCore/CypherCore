// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("wp")]
    class WPCommands
    {
        [Command("add", RBACPermissions.CommandWpAdd)]
        static bool HandleWpAddCommand(CommandHandler handler, uint? optionalPathId)
        {
            uint point = 0;
            Creature target = handler.GetSelectedCreature();

            PreparedStatement stmt;
            uint pathId;

            if (!optionalPathId.HasValue)
            {
                if (target != null)
                    pathId = target.GetWaypointPath();
                else
                {
                    stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_MAX_ID);
                    SQLResult result1 = DB.World.Query(stmt);

                    uint maxpathid = result1.Read<uint>(0);
                    pathId = maxpathid + 1;
                    handler.SendSysMessage("|cff00ff00New path started.|r");
                }
            }
            else
                pathId = optionalPathId.Value;

            // path_id . ID of the Path
            // point   . number of the waypoint (if not 0)

            if (pathId == 0)
            {
                handler.SendSysMessage("|cffff33ffCurrent creature haven't loaded path.|r");
                return true;
            }

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_MAX_POINT);
            stmt.AddValue(0, pathId);
            SQLResult result = DB.World.Query(stmt);

            if (result.IsEmpty())
                point = result.Read<uint>(0);

            Player player = handler.GetSession().GetPlayer();

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.INS_WAYPOINT_DATA);
            stmt.AddValue(0, pathId);
            stmt.AddValue(1, point + 1);
            stmt.AddValue(2, player.GetPositionX());
            stmt.AddValue(3, player.GetPositionY());
            stmt.AddValue(4, player.GetPositionZ());
            stmt.AddValue(5, player.GetOrientation());

            DB.World.Execute(stmt);

            handler.SendSysMessage("|cff00ff00PathID: |r|cff00ffff{0} |r|cff00ff00: Waypoint |r|cff00ffff{1}|r|cff00ff00 created.|r", pathId, point + 1);
            return true;
        }

        [Command("load", RBACPermissions.CommandWpLoad)]
        static bool HandleWpLoadCommand(CommandHandler handler, uint? optionalPathId)
        {
            Creature target = handler.GetSelectedCreature();

            // Did player provide a path_id?
            if (!optionalPathId.HasValue)
                return false;

            uint pathId = optionalPathId.Value;

            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            if (target.GetEntry() == 1)
            {
                handler.SendSysMessage("|cffff33ffYou want to load path to a waypoint? Aren't you?|r");
                return false;
            }

            if (pathId == 0)
            {
                handler.SendSysMessage("|cffff33ffNo valid path number provided.|r");
                return true;
            }

            ulong guidLow = target.GetSpawnId();

            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_CREATURE_ADDON_BY_GUID);
            stmt.AddValue(0, guidLow);
            SQLResult result = DB.World.Query(stmt);

            if (!result.IsEmpty())
            {
                stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_ADDON_PATH);
                stmt.AddValue(0, pathId);
                stmt.AddValue(1, guidLow);
            }
            else
            {
                stmt = WorldDatabase.GetPreparedStatement(WorldStatements.INS_CREATURE_ADDON);
                stmt.AddValue(0, guidLow);
                stmt.AddValue(1, pathId);
            }

            DB.World.Execute(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_MOVEMENT_TYPE);
            stmt.AddValue(0, (byte)MovementGeneratorType.Waypoint);
            stmt.AddValue(1, guidLow);

            DB.World.Execute(stmt);

            target.LoadPath(pathId);
            target.SetDefaultMovementType(MovementGeneratorType.Waypoint);
            target.GetMotionMaster().Initialize();
            target.Say("Path loaded.", Language.Universal);

            return true;
        }

        [Command("modify", RBACPermissions.CommandWpModify)]
        static bool HandleWpModifyCommand(CommandHandler handler, string subCommand, [OptionalArg] string arg)
        {
            // first arg: add del text emote spell waittime move
            if (subCommand.IsEmpty())
                return false;

            // Check
            // Remember: "show" must also be the name of a column!
            if ((subCommand != "delay") && (subCommand != "action") && (subCommand != "action_chance")
                && (subCommand != "move_flag") && (subCommand != "del") && (subCommand != "move"))
            {
                return false;
            }

            // Did user provide a GUID
            // or did the user select a creature?
            // . variable lowguid is filled with the GUID of the NPC
            uint pathid;
            uint point;
            Creature target = handler.GetSelectedCreature();

            // User did select a visual waypoint?
            if (target == null || target.GetEntry() != 1)
            {
                handler.SendSysMessage("|cffff33ffERROR: You must select a waypoint.|r");
                return false;
            }

            // Check the creature
            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_WPGUID);
            stmt.AddValue(0, target.GetSpawnId());
            SQLResult result = DB.World.Query(stmt);

            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.WaypointNotfoundsearch, target.GetGUID().ToString());
                // Select waypoint number from database
                // Since we compare float values, we have to deal with
                // some difficulties.
                // Here we search for all waypoints that only differ in one from 1 thousand
                // See also: http://dev.mysql.com/doc/refman/5.0/en/problems-with-float.html
                string maxDiff = "0.01";

                stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_POS);
                stmt.AddValue(0, target.GetPositionX());
                stmt.AddValue(1, maxDiff);
                stmt.AddValue(2, target.GetPositionY());
                stmt.AddValue(3, maxDiff);
                stmt.AddValue(4, target.GetPositionZ());
                stmt.AddValue(5, maxDiff);
                result = DB.World.Query(stmt);

                if (result.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.WaypointNotfounddbproblem, target.GetGUID().ToString());
                    return true;
                }
            }

            do
            {
                pathid = result.Read<uint>(0);
                point = result.Read<uint>(1);
            }
            while (result.NextRow());

            // We have the waypoint number and the GUID of the "master npc"
            // Text is enclosed in "<>", all other arguments not

            // Check for argument
            if (subCommand != "del" && subCommand != "move")
            {
                handler.SendSysMessage(CypherStrings.WaypointArgumentreq, subCommand);
                return false;
            }

            if (subCommand == "del")
            {
                handler.SendSysMessage("|cff00ff00DEBUG: wp modify del, PathID: |r|cff00ffff{0}|r", pathid);

                if (Creature.DeleteFromDB(target.GetSpawnId()))
                {

                    stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_WAYPOINT_DATA);
                    stmt.AddValue(0, pathid);
                    stmt.AddValue(1, point);
                    DB.World.Execute(stmt);

                    stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_WAYPOINT_DATA_POINT);
                    stmt.AddValue(0, pathid);
                    stmt.AddValue(1, point);
                    DB.World.Execute(stmt);

                    handler.SendSysMessage(CypherStrings.WaypointRemoved);
                    return true;
                }
                else
                {
                    handler.SendSysMessage(CypherStrings.WaypointNotremoved);
                    return false;
                }
            }                                                       // del

            if (subCommand == "move")
            {
                handler.SendSysMessage("|cff00ff00DEBUG: wp move, PathID: |r|cff00ffff{0}|r", pathid);

                Player chr = handler.GetSession().GetPlayer();
                Map map = chr.GetMap();
                // What to do:
                // Move the visual spawnpoint
                // Respawn the owner of the waypoints
                if (Creature.DeleteFromDB(target.GetSpawnId()))
                {
                    handler.SendSysMessage(CypherStrings.WaypointVpNotcreated, 1);
                    return false;
                }

                // re-create
                Creature creature = Creature.CreateCreature(1, map, chr.GetPosition());
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.WaypointVpNotcreated, 1);
                    return false;
                }

                PhasingHandler.InheritPhaseShift(creature, chr);
                creature.SaveToDB(map.GetId(), new List<Difficulty>() { map.GetDifficultyID() });

                ulong dbGuid = creature.GetSpawnId();

                // current "wpCreature" variable is deleted and created fresh new, otherwise old values might trigger asserts or cause undefined behavior
                creature.CleanupsBeforeDelete();
                creature.Dispose();

                // To call _LoadGoods(); _LoadQuests(); CreateTrainerSpells();
                creature = Creature.CreateCreatureFromDB(dbGuid, map, true, true);
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.WaypointVpNotcreated, 1);
                    return false;
                }

                stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_WAYPOINT_DATA_POSITION);
                stmt.AddValue(0, chr.GetPositionX());
                stmt.AddValue(1, chr.GetPositionY());
                stmt.AddValue(2, chr.GetPositionZ());
                stmt.AddValue(3, chr.GetOrientation());
                stmt.AddValue(4, pathid);
                stmt.AddValue(5, point);
                DB.World.Execute(stmt);

                handler.SendSysMessage(CypherStrings.WaypointChanged);

                return true;
            }                                                     // move

            if (arg.IsEmpty())
            {
                // show_str check for present in list of correct values, no sql injection possible
                DB.World.Execute("UPDATE waypoint_data SET {0}=null WHERE id='{1}' AND point='{2}'", subCommand, pathid, point); // Query can't be a prepared statement
            }
            else
            {
                // show_str check for present in list of correct values, no sql injection possible
                DB.World.Execute("UPDATE waypoint_data SET {0}='{1}' WHERE id='{2}' AND point='{3}'", subCommand, arg, pathid, point); // Query can't be a prepared statement
            }

            handler.SendSysMessage(CypherStrings.WaypointChangedNo, subCommand);
            return true;
        }

        [Command("reload", RBACPermissions.CommandWpReload)]
        static bool HandleWpReloadCommand(CommandHandler handler, uint pathId)
        {
            if (pathId == 0)
                return false;

            handler.SendSysMessage("|cff00ff00Loading Path: |r|cff00ffff{0}|r", pathId);
            Global.WaypointMgr.ReloadPath(pathId);
            return true;
        }

        [Command("show", RBACPermissions.CommandWpShow)]
        static bool HandleWpShowCommand(CommandHandler handler, string subCommand, uint? optionalPathId)
        {
            // first arg: on, off, first, last
            if (subCommand.IsEmpty())
                return false;

            Creature target = handler.GetSelectedCreature();

            // Did player provide a PathID?
            uint pathId;

            if (!optionalPathId.HasValue)
            {
                // No PathID provided
                // . Player must have selected a creature

                if (target == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);    
                    return false;
                }

                pathId = target.GetWaypointPath();
            }
            else
            {
                // PathID provided
                // Warn if player also selected a creature
                // . Creature selection is ignored <-
                if (target != null)
                    handler.SendSysMessage(CypherStrings.WaypointCreatselected);

                pathId = optionalPathId.Value;
            }

            // Show info for the selected waypoint
            if (subCommand == "info")
            {
                // Check if the user did specify a visual waypoint
                if (target == null || target.GetEntry() != 1)
                {
                    handler.SendSysMessage(CypherStrings.WaypointVpSelect);    
                    return false;
                }

                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_ALL_BY_WPGUID);
                stmt.AddValue(0, target.GetSpawnId());
                SQLResult result = DB.World.Query(stmt);

                if (result.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.WaypointNotfounddbproblem, target.GetSpawnId());
                    return true;
                }

                handler.SendSysMessage("|cff00ffffDEBUG: wp show info:|r");
                do
                {
                    pathId = result.Read<uint>(0);
                    uint point = result.Read<uint>(1);
                    uint delay = result.Read<uint>(2);
                    uint flag = result.Read<uint>(3);

                    handler.SendSysMessage("|cff00ff00Show info: for current point: |r|cff00ffff{0}|r|cff00ff00, Path ID: |r|cff00ffff{1}|r", point, pathId);
                    handler.SendSysMessage("|cff00ff00Show info: delay: |r|cff00ffff{0}|r", delay);
                    handler.SendSysMessage("|cff00ff00Show info: Move flag: |r|cff00ffff{0}|r", flag);
                }
                while (result.NextRow());

                return true;
            }

            if (subCommand == "on")
            {
                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_POS_BY_ID);
                stmt.AddValue(0, pathId);
                SQLResult result = DB.World.Query(stmt);

                if (result.IsEmpty())
                {
                    handler.SendSysMessage("|cffff33ffPath no found.|r");    
                    return false;
                }

                handler.SendSysMessage("|cff00ff00DEBUG: wp on, PathID: |cff00ffff{0}|r", pathId);

                // Delete all visuals for this NPC
                stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_WPGUID_BY_ID);
                stmt.AddValue(0, pathId);
                SQLResult result2 = DB.World.Query(stmt);

                if (!result2.IsEmpty())
                {
                    bool hasError = false;
                    do
                    {
                        ulong wpguid = result2.Read<ulong>(0);

                        if (Creature.DeleteFromDB(wpguid))
                        {
                            handler.SendSysMessage(CypherStrings.WaypointNotremoved, wpguid);
                            hasError = true;
                        }

                    }
                    while (result2.NextRow());

                    if (hasError)
                    {
                        handler.SendSysMessage(CypherStrings.WaypointToofar1);
                        handler.SendSysMessage(CypherStrings.WaypointToofar2);
                        handler.SendSysMessage(CypherStrings.WaypointToofar3);
                    }
                }

                do
                {
                    uint point = result.Read<uint>(0);
                    float x = result.Read<float>(1);
                    float y = result.Read<float>(2);
                    float z = result.Read<float>(3);
                    float o = result.Read<float>(4);

                    uint id = 1;

                    Player chr = handler.GetSession().GetPlayer();
                    Map map = chr.GetMap();

                    Creature creature = Creature.CreateCreature(id, map, new Position(x, y, z, o));
                    if (creature == null)
                    {
                        handler.SendSysMessage(CypherStrings.WaypointVpNotcreated, id);
                        return false;
                    }

                    PhasingHandler.InheritPhaseShift(creature, chr);
                    creature.SaveToDB(map.GetId(), new List<Difficulty>() { map.GetDifficultyID() });

                    ulong dbGuid = creature.GetSpawnId();

                    // current "wpCreature" variable is deleted and created fresh new, otherwise old values might trigger asserts or cause undefined behavior
                    creature.CleanupsBeforeDelete();
                    creature.Dispose();

                    // To call _LoadGoods(); _LoadQuests(); CreateTrainerSpells();
                    creature = Creature.CreateCreatureFromDB(dbGuid, map, true, true);
                    if (creature == null)
                    {
                        handler.SendSysMessage(CypherStrings.WaypointVpNotcreated, id);
                        return false;
                    }

                    if (target != null)
                    {
                        creature.SetDisplayId(target.GetDisplayId());
                        creature.SetObjectScale(0.5f);
                        creature.SetLevel(Math.Min(point, SharedConst.StrongMaxLevel));
                    }

                    // Set "wpguid" column to the visual waypoint
                    stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_WAYPOINT_DATA_WPGUID);
                    stmt.AddValue(0, creature.GetSpawnId());
                    stmt.AddValue(1, pathId);
                    stmt.AddValue(2, point);
                    DB.World.Execute(stmt);
                }
                while (result.NextRow());

                handler.SendSysMessage("|cff00ff00Showing the current creature's path.|r");
                return true;
            }

            if (subCommand == "first")
            {
                handler.SendSysMessage("|cff00ff00DEBUG: wp first, pathid: {0}|r", pathId);

                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_POS_FIRST_BY_ID);
                stmt.AddValue(0, pathId);
                SQLResult result = DB.World.Query(stmt);

                if (result.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.WaypointNotfound, pathId);    
                    return false;
                }

                float x = result.Read<float>(0);
                float y = result.Read<float>(1);
                float z = result.Read<float>(2);
                float o = result.Read<float>(3);

                Player chr = handler.GetSession().GetPlayer();
                Map map = chr.GetMap();

                Creature creature = Creature.CreateCreature(1, map, new Position(x, y, z, 0));
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.WaypointVpNotcreated, 1);
                    return false;
                }

                PhasingHandler.InheritPhaseShift(creature, chr);
                creature.SaveToDB(map.GetId(), new List<Difficulty>() { map.GetDifficultyID() });

                ulong dbGuid = creature.GetSpawnId();

                // current "creature" variable is deleted and created fresh new, otherwise old values might trigger asserts or cause undefined behavior
                creature.CleanupsBeforeDelete();
                creature.Dispose();

                creature = Creature.CreateCreatureFromDB(dbGuid, map, true, true);
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.WaypointVpNotcreated, 1);
                    return false;
                }

                if (target != null)
                {
                    creature.SetDisplayId(target.GetDisplayId());
                    creature.SetObjectScale(0.5f);
                }

                return true;
            }

            if (subCommand == "last")
            {
                handler.SendSysMessage("|cff00ff00DEBUG: wp last, PathID: |r|cff00ffff{0}|r", pathId);

                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_POS_LAST_BY_ID);
                stmt.AddValue(0, pathId);
                SQLResult result = DB.World.Query(stmt);

                if (result.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.WaypointNotfoundlast, pathId);    
                    return false;
                }

                float x = result.Read<float>(0);
                float y = result.Read<float>(1);
                float z = result.Read<float>(2);
                float o = result.Read<float>(3);

                Player chr = handler.GetSession().GetPlayer();
                Map map = chr.GetMap();
                Position pos = new(x, y, z, o);

                Creature creature = Creature.CreateCreature(1, map, pos);
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.WaypointNotcreated, 1);
                    return false;
                }

                PhasingHandler.InheritPhaseShift(creature, chr);
                creature.SaveToDB(map.GetId(), new List<Difficulty>() { map.GetDifficultyID() });

                ulong dbGuid = creature.GetSpawnId();

                // current "creature" variable is deleted and created fresh new, otherwise old values might trigger asserts or cause undefined behavior
                creature.CleanupsBeforeDelete();
                creature.Dispose();

                creature = Creature.CreateCreatureFromDB(dbGuid, map, true, true);
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.WaypointNotcreated, 1);
                    return false;
                }

                if (target != null)
                {
                    creature.SetDisplayId(target.GetDisplayId());
                    creature.SetObjectScale(0.5f);
                }

                return true;
            }

            if (subCommand == "off")
            {
                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_CREATURE_BY_ID);
                stmt.AddValue(0, 1);
                SQLResult result = DB.World.Query(stmt);

                if (result.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.WaypointVpNotfound);    
                    return false;
                }
                bool hasError = false;
                do
                {
                    ulong lowguid = result.Read<ulong>(0);

                    if (Creature.DeleteFromDB(lowguid))
                    {
                        handler.SendSysMessage(CypherStrings.WaypointNotremoved, lowguid);
                        hasError = true;
                    }
                }
                while (result.NextRow());
                // set "wpguid" column to "empty" - no visual waypoint spawned
                stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_WAYPOINT_DATA_ALL_WPGUID);

                DB.World.Execute(stmt);
                //DB.World.PExecute("UPDATE creature_movement SET wpguid = '0' WHERE wpguid <> '0'");

                if (hasError)
                {
                    handler.SendSysMessage(CypherStrings.WaypointToofar1);
                    handler.SendSysMessage(CypherStrings.WaypointToofar2);
                    handler.SendSysMessage(CypherStrings.WaypointToofar3);
                }

                handler.SendSysMessage(CypherStrings.WaypointVpAllremoved);
                return true;
            }

            handler.SendSysMessage("|cffff33ffDEBUG: wpshow - no valid command found|r");
            return true;
        }

        [Command("unload", RBACPermissions.CommandWpUnload)]
        static bool HandleWpUnLoadCommand(CommandHandler handler)
        {
            Creature target = handler.GetSelectedCreature();
            if (target == null)
            {
                handler.SendSysMessage("|cff33ffffYou must select a target.|r");
                return true;
            }

            ulong guidLow = target.GetSpawnId();
            if (guidLow == 0)
            {
                handler.SendSysMessage("|cffff33ffTarget is not saved to DB.|r");
                return true;
            }

            CreatureAddon addon = Global.ObjectMgr.GetCreatureAddon(guidLow);
            if (addon == null || addon.path_id == 0)
            {
                handler.SendSysMessage("|cffff33ffTarget does not have a loaded path.|r");
                return true;
            }

            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_CREATURE_ADDON);
            stmt.AddValue(0, guidLow);
            DB.World.Execute(stmt);

            target.UpdateCurrentWaypointInfo(0, 0);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_MOVEMENT_TYPE);
            stmt.AddValue(0, (byte)MovementGeneratorType.Idle);
            stmt.AddValue(1, guidLow);
            DB.World.Execute(stmt);

            target.LoadPath(0);
            target.SetDefaultMovementType(MovementGeneratorType.Idle);
            target.GetMotionMaster().MoveTargetedHome();
            target.GetMotionMaster().Initialize();
            target.Say("Path unloaded.", Language.Universal);
            return true;
        }
    }
}
