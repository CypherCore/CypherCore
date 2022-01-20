/*
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

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.SupportSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Chat.Commands
{
    [CommandGroup("go", RBACPermissions.CommandGo)]
    class GoCommands
    {
        [Command("areatrigger", RBACPermissions.CommandGo)]
        static bool HandleGoAreaTriggerCommand(CommandHandler handler, uint areaTriggerId)
        {
            var at = CliDB.AreaTriggerStorage.LookupByKey(areaTriggerId);
            if (at == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGoareatrnotfound, areaTriggerId);
                return false;
            }
            return DoTeleport(handler, new Position(at.Pos.X, at.Pos.Y, at.Pos.Z), at.ContinentID);
        }

        [Command("bugticket", RBACPermissions.CommandGo)]
        static bool HandleGoBugTicketCommand(CommandHandler handler, uint ticketId)
        {
            return HandleGoTicketCommand<BugTicket>(handler, ticketId);
        }

        [Command("complaintticket", RBACPermissions.CommandGo)]
        static bool HandleGoComplaintTicketCommand(CommandHandler handler, uint ticketId)
        {
            return HandleGoTicketCommand<ComplaintTicket>(handler, ticketId);
        }

        [Command("graveyard", RBACPermissions.CommandGo)]
        static bool HandleGoGraveyardCommand(CommandHandler handler, uint graveyardId)
        {
            WorldSafeLocsEntry gy = Global.ObjectMgr.GetWorldSafeLoc(graveyardId);
            if (gy == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGraveyardnoexist, graveyardId);
                return false;
            }

            if (!GridDefines.IsValidMapCoord(gy.Loc))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, gy.Loc.GetPositionX(), gy.Loc.GetPositionY(), gy.Loc.GetMapId());
                return false;
            }

            Player player = handler.GetSession().GetPlayer();
            // stop flight if need
            if (player.IsInFlight())
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition(); // save only in non-flight case

            player.TeleportTo(gy.Loc);
            return true;
        }

        [Command("grid", RBACPermissions.CommandGo)]
        static bool HandleGoGridCommand(CommandHandler handler, float gridX, float gridY, uint mapId)
        {
            Player player = handler.GetSession().GetPlayer();

            // center of grid
            float x = (gridX - MapConst.CenterGridId + 0.5f) * MapConst.SizeofGrids;
            float y = (gridY - MapConst.CenterGridId + 0.5f) * MapConst.SizeofGrids;

            if (!GridDefines.IsValidMapCoord(mapId, x, y))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition(); // save only in non-flight case

            Map map = Global.MapMgr.CreateBaseMap(mapId);
            float z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(mapId, x, y, z, player.GetOrientation());
            return true;
        }

        [Command("instance", RBACPermissions.CommandGo)]
        static bool HandleGoInstanceCommand(CommandHandler handler, string[] labels)
        {
            if (labels.Empty())
                return false;

            MultiMap<uint, Tuple<uint, string, string>> matches = new();
            foreach (var pair in Global.ObjectMgr.GetInstanceTemplates())
            {
                uint count = 0;
                string scriptName = Global.ObjectMgr.GetScriptName(pair.Value.ScriptId);
                string mapName1 = CliDB.MapStorage.LookupByKey(pair.Key).MapName[handler.GetSessionDbcLocale()];
                foreach (var label in labels)
                    if (scriptName.Contains(label))
                        ++count;

                if (count != 0)
                    matches.Add(count, Tuple.Create(pair.Key, mapName1, scriptName));
            }

            if (matches.Empty())
            {
                handler.SendSysMessage(CypherStrings.CommandNoInstancesMatch);
                return false;
            }

            var it = matches.Last();
            uint mapId = it.Value.Item1;
            string mapName = it.Value.Item2;

            Player player = handler.GetSession().GetPlayer();
            if (player.IsInFlight())
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition();

            // try going to entrance
            AreaTriggerStruct exit = Global.ObjectMgr.GetGoBackTrigger(mapId);
            if (exit != null)
            {
                if (player.TeleportTo(exit.target_mapId, exit.target_X, exit.target_Y, exit.target_Z, exit.target_Orientation + MathF.PI))
                {
                    handler.SendSysMessage(CypherStrings.CommandWentToInstanceGate, mapName, mapId);
                    return true;
                }
                else
                {
                    uint parentMapId = exit.target_mapId;
                    string parentMapName = CliDB.MapStorage.LookupByKey(parentMapId).MapName[handler.GetSessionDbcLocale()];
                    handler.SendSysMessage(CypherStrings.CommandGoInstanceGateFailed, mapName, mapId, parentMapName, parentMapId);
                }
            }
            else
                handler.SendSysMessage(CypherStrings.CommandInstanceNoExit, mapName, mapId);

            // try going to start
            AreaTriggerStruct entrance = Global.ObjectMgr.GetMapEntranceTrigger(mapId);
            if (entrance != null)
            {
                if (player.TeleportTo(entrance.target_mapId, entrance.target_X, entrance.target_Y, entrance.target_Z, entrance.target_Orientation))
                {
                    handler.SendSysMessage(CypherStrings.CommandWentToInstanceStart, mapName, mapId);
                    return true;
                }
                else
                    handler.SendSysMessage(CypherStrings.CommandGoInstanceStartFailed, mapName, mapId);
            }
            else
                handler.SendSysMessage(CypherStrings.CommandInstanceNoEntrance, mapName, mapId);

            return false;
        }

        [Command("offset", RBACPermissions.CommandGo)]
        static bool HandleGoOffsetCommand(CommandHandler handler, float dX, float dY, float dZ, float dO)
        {
            Position loc = handler.GetSession().GetPlayer().GetPosition();
            loc.RelocateOffset(new Position(dX, dY, dZ, dO));

            return DoTeleport(handler, loc);
        }

        [Command("quest", RBACPermissions.CommandGo)]
        static bool HandleGoQuestCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player player = handler.GetSession().GetPlayer();

            string id = handler.ExtractKeyFromLink(args, "Hquest");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out uint questID) || questID == 0)
                return false;

            if (Global.ObjectMgr.GetQuestTemplate(questID) == null)
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, questID);
                return false;
            }

            float x, y, z;
            uint mapId;

            var poiData = Global.ObjectMgr.GetQuestPOIData(questID);
            if (poiData != null)
            {
                var data = poiData.Blobs[0];

                mapId = (uint)data.MapID;

                x = data.Points[0].X;
                y = data.Points[0].Y;
            }
            else
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, questID);
                return false;
            }

            if (!GridDefines.IsValidMapCoord(mapId, x, y) || Global.ObjectMgr.IsTransportMap(mapId))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition(); // save only in non-flight case

            Map map = Global.MapMgr.CreateBaseMap(mapId);
            z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(mapId, x, y, z, 0.0f);
            return true;
        }

        [Command("suggestionticket", RBACPermissions.CommandGo)]
        static bool HandleGoSuggestionTicketCommand(CommandHandler handler, uint ticketId)
        {
            return HandleGoTicketCommand<SuggestionTicket>(handler, ticketId);
        }

        [Command("taxinode", RBACPermissions.CommandGo)]
        static bool HandleGoTaxinodeCommand(CommandHandler handler, uint nodeId)
        {
            var node = CliDB.TaxiNodesStorage.LookupByKey(nodeId);
            if (node == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGotaxinodenotfound, nodeId);
                return false;
            }
            return DoTeleport(handler, new Position(node.Pos.X, node.Pos.Y, node.Pos.Z), node.ContinentID);
        }

        //teleport at coordinates, including Z and orientation
        [Command("xyz", RBACPermissions.CommandGo)]
        static bool HandleGoXYZCommand(CommandHandler handler, float x, float y, float? z, uint? id, float? o)
        {
            Player player = handler.GetSession().GetPlayer();
            uint mapId = id.HasValue ? id.Value : player.GetMapId();
            if (z.HasValue)
            {
                if (!GridDefines.IsValidMapCoord(mapId, x, y, z.Value))
                {
                    handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);
                    return false;
                }
            }
            else
            {
                if (!GridDefines.IsValidMapCoord(mapId, x, y))
                {
                    handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);
                    return false;
                }
                Map map = Global.MapMgr.CreateBaseMap(mapId);
                z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));
            }

            return DoTeleport(handler, new Position(x, y, z.Value, o.Value), mapId);
        }

        //teleport at coordinates
        [Command("zonexy", RBACPermissions.CommandGo)]
        static bool HandleGoZoneXYCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player player = handler.GetSession().GetPlayer();

            if (!float.TryParse(args.NextString(), out float x))
                return false;

            if (!float.TryParse(args.NextString(),out float y))
                return false;

            // prevent accept wrong numeric args
            if (x == 0.0f || y == 0.0f)
                return false;

            string idStr = handler.ExtractKeyFromLink(args, "Harea");       // string or [name] Shift-click form |color|Harea:area_id|h[name]|h|r
            if (!uint.TryParse(idStr, out uint areaId))
                areaId = player.GetZoneId();

            AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (x < 0 || x > 100 || y < 0 || y > 100 || areaEntry == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidZoneCoord, x, y, areaId);
                return false;
            }

            // update to parent zone if exist (client map show only zones without parents)
            AreaTableRecord zoneEntry = areaEntry.ParentAreaID != 0 ? CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID) : areaEntry;
            Cypher.Assert(zoneEntry != null);

            Map map = Global.MapMgr.CreateBaseMap(zoneEntry.ContinentID);

            if (map.Instanceable())
            {
                handler.SendSysMessage(CypherStrings.InvalidZoneMap, areaId, areaEntry.AreaName[handler.GetSessionDbcLocale()], map.GetId(), map.GetMapName());
                return false;
            }

            x /= 100.0f;
            y /= 100.0f;

            Global.DB2Mgr.Zone2MapCoordinates(areaEntry.ParentAreaID != 0 ? areaEntry.ParentAreaID : areaId, ref x, ref y);

            if (!GridDefines.IsValidMapCoord(zoneEntry.ContinentID, x, y))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, zoneEntry.ContinentID);
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition(); // save only in non-flight case

            float z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(zoneEntry.ContinentID, x, y, z, player.GetOrientation());
            return true;
        }

        static bool HandleGoTicketCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
        {
            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            Player player = handler.GetSession().GetPlayer();

            // stop flight if need
            if (player.IsInFlight())
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition(); // save only in non-flight case

            ticket.TeleportTo(player);
            return true;
        }

        static bool DoTeleport(CommandHandler handler, Position pos, uint mapId = 0xFFFFFFFF)
        {
            Player player = handler.GetSession().GetPlayer();

            if (mapId == 0xFFFFFFFF)
                mapId = player.GetMapId();

            if (!GridDefines.IsValidMapCoord(mapId, pos) || Global.ObjectMgr.IsTransportMap(mapId))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, pos.GetPositionX(), pos.GetPositionY(), mapId);
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition(); // save only in non-flight case

            player.TeleportTo(new WorldLocation(mapId, pos));
            return true;
        }

        [CommandGroup("creature", RBACPermissions.CommandGo)]
        class GoCommandCreature
        {
            [Command("", RBACPermissions.CommandGo)]
            static bool HandleGoCreatureSpawnIdCommand(CommandHandler handler, ulong spawnId)
            {
                CreatureData spawnpoint = Global.ObjectMgr.GetCreatureData(spawnId);
                if (spawnpoint == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);
                    return false;
                }

                return DoTeleport(handler, spawnpoint.SpawnPoint, spawnpoint.MapId);
            }

            [Command("id", RBACPermissions.CommandGo)]
            static bool HandleGoCreatureCIdCommand(CommandHandler handler, uint id)
            {
                CreatureData spawnpoint = null;
                foreach (var pair in Global.ObjectMgr.GetAllCreatureData())
                {
                    if (pair.Value.Id != id)
                        continue;

                    if (spawnpoint == null)
                        spawnpoint = pair.Value;
                    else
                    {
                        handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);
                        break;
                    }
                }

                if (spawnpoint == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);
                    return false;
                }

                return DoTeleport(handler, spawnpoint.SpawnPoint, spawnpoint.MapId);
            }
        }

        [CommandGroup("gameobject", RBACPermissions.CommandGo)]
        class GoCommandGameobject
        {
            [Command("", RBACPermissions.CommandGo)]
            static bool HandleGoGameObjectSpawnIdCommand(CommandHandler handler, uint spawnId)
            {
                GameObjectData spawnpoint = Global.ObjectMgr.GetGameObjectData(spawnId);
                if (spawnpoint == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandGoobjnotfound);
                    return false;
                }

                return DoTeleport(handler, spawnpoint.SpawnPoint, spawnpoint.MapId);
            }

            [Command("id", RBACPermissions.CommandGo)]
            static bool HandleGoGameObjectGOIdCommand(CommandHandler handler, uint goId)
            {
                GameObjectData spawnpoint = null;
                foreach (var pair in Global.ObjectMgr.GetAllGameObjectData())
        {
                    if (pair.Value.Id != goId)
                        continue;

                    if (spawnpoint == null)
                        spawnpoint = pair.Value;
                    else
                    {
                        handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);
                        break;
                    }
                }

                if (spawnpoint == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandGoobjnotfound);
                    return false;
                }

                return DoTeleport(handler, spawnpoint.SpawnPoint, spawnpoint.MapId);
            }
        }
    }
}
