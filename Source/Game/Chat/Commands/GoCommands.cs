﻿/*
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
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.SupportSystem;
using System;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("go", RBACPermissions.CommandGo)]
    class GoCommands
    {
        [Command("creature", RBACPermissions.CommandGoCreature)]
        static bool HandleGoCreatureCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var player = handler.GetSession().GetPlayer();

            // "id" or number or [name] Shift-click form |color|Hcreature_entry:creature_id|h[name]|h|r
            var param1 = handler.ExtractKeyFromLink(args, "Hcreature");
            if (param1.IsEmpty())
                return false;

            var whereClause = "";

            // User wants to teleport to the NPC's template entry
            if (param1.Equals("id"))
            {
                // Get the "creature_template.entry"
                // number or [name] Shift-click form |color|Hcreature_entry:creature_id|h[name]|h|r
                var id = handler.ExtractKeyFromLink(args, "Hcreature_entry");
                if (id.IsEmpty())
                    return false;

                if (!uint.TryParse(id, out var entry))
                    return false;

                whereClause += "WHERE id = '" + entry + '\'';
            }
            else
            {
                ulong.TryParse(param1, out var guidLow);
                if (guidLow != 0)
                {
                    whereClause += "WHERE guid = '" + guidLow + '\'';
                }
                else
                {
                    // param1 is not a number, must be mob's name
                    whereClause += ", creature_template WHERE creature.id = creature_template.entry AND creature_template.name LIKE '" + args.GetString() + '\'';
                }
            }

            var result = DB.World.Query("SELECT position_x, position_y, position_z, orientation, map FROM creature {0}", whereClause);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);
                return false;
            }

            var x = result.Read<float>(0);
            var y = result.Read<float>(1);
            var z = result.Read<float>(2);
            var o = result.Read<float>(3);
            uint mapId = result.Read<ushort>(4);

            if (result.NextRow())
                handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);

            if (!GridDefines.IsValidMapCoord(mapId, x, y, z, o) || Global.ObjectMgr.IsTransportMap(mapId))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            player.TeleportTo(mapId, x, y, z, o);

            return true;
        }

        [Command("graveyard", RBACPermissions.CommandGoGraveyard)]
        static bool HandleGoGraveyardCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();

            if (args.Empty())
                return false;

            var graveyardId = args.NextUInt32();
            if (graveyardId == 0)
                return false;

            var gy = Global.ObjectMgr.GetWorldSafeLoc(graveyardId);
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

            // stop flight if need
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            player.TeleportTo(gy.Loc);
            return true;
        }

        //teleport to grid
        [Command("grid", RBACPermissions.CommandGoGrid)]
        static bool HandleGoGridCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var player = handler.GetSession().GetPlayer();

            if (!float.TryParse(args.NextString(), out var gridX))
                return false;

            if (!float.TryParse(args.NextString(), out var gridY))
                return false;

            if (!uint.TryParse(args.NextString(), out var mapId))
                mapId = player.GetMapId();

            // center of grid
            var x = (gridX - MapConst.CenterGridId + 0.5f) * MapConst.SizeofGrids;
            var y = (gridY - MapConst.CenterGridId + 0.5f) * MapConst.SizeofGrids;

            if (!GridDefines.IsValidMapCoord(mapId, x, y))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            var map = Global.MapMgr.CreateBaseMap(mapId);
            var z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(mapId, x, y, z, player.GetOrientation());
            return true;
        }

        //teleport to gameobject
        [Command("object", RBACPermissions.CommandGoObject)]
        static bool HandleGoObjectCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var player = handler.GetSession().GetPlayer();

            // number or [name] Shift-click form |color|Hgameobject:go_guid|h[name]|h|r
            var id = handler.ExtractKeyFromLink(args, "Hgameobject");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!ulong.TryParse(id, out var guidLow) || guidLow == 0)
                return false;

            // by DB guid
            var goData = Global.ObjectMgr.GetGameObjectData(guidLow);
            if (goData == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGoobjnotfound);
                return false;
            }

            if (!GridDefines.IsValidMapCoord(goData.spawnPoint) || Global.ObjectMgr.IsTransportMap(goData.spawnPoint.GetMapId()))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, goData.spawnPoint.GetPositionX(), goData.spawnPoint.GetPositionY(), goData.spawnPoint.GetMapId());
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            player.TeleportTo(goData.spawnPoint);
            return true;
        }

        [Command("quest", RBACPermissions.CommandGoQuest)]
        static bool HandleGoQuestCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var player = handler.GetSession().GetPlayer();

            var id = handler.ExtractKeyFromLink(args, "Hquest");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out var questID) || questID == 0)
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
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            var map = Global.MapMgr.CreateBaseMap(mapId);
            z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(mapId, x, y, z, 0.0f);
            return true;
        }

        [Command("taxinode", RBACPermissions.CommandGoTaxinode)]
        static bool HandleGoTaxinodeCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();

            if (args.Empty())
                return false;

            var id = handler.ExtractKeyFromLink(args, "Htaxinode");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out var nodeId) || nodeId == 0)
                return false;

            var node = CliDB.TaxiNodesStorage.LookupByKey(nodeId);
            if (node == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGotaxinodenotfound, nodeId);
                return false;
            }

            if ((node.Pos.X == 0.0f && node.Pos.Y == 0.0f && node.Pos.Z == 0.0f) ||
                !GridDefines.IsValidMapCoord(node.ContinentID, node.Pos.X, node.Pos.Y, node.Pos.Z))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, node.Pos.X, node.Pos.Y, node.ContinentID);
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            player.TeleportTo(node.ContinentID, node.Pos.X, node.Pos.Y, node.Pos.Z, player.GetOrientation());
            return true;
        }

        [Command("trigger", RBACPermissions.CommandGoTrigger)]
        static bool HandleGoTriggerCommand(StringArguments args, CommandHandler handler)
        {
            var player = handler.GetSession().GetPlayer();

            if (args.Empty())
                return false;

            var areaTriggerId = args.NextUInt32();
            if (areaTriggerId == 0)
                return false;

            var at = CliDB.AreaTriggerStorage.LookupByKey(areaTriggerId);
            if (at == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGoareatrnotfound, areaTriggerId);
                return false;
            }

            if (!GridDefines.IsValidMapCoord(at.ContinentID, at.Pos.X, at.Pos.Y, at.Pos.Z))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, at.Pos.X, at.Pos.Y, at.ContinentID);
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            player.TeleportTo(at.ContinentID, at.Pos.X, at.Pos.Y, at.Pos.Z, player.GetOrientation());
            return true;
        }

        //teleport at coordinates
        [Command("zonexy", RBACPermissions.CommandGoZonexy)]
        static bool HandleGoZoneXYCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var player = handler.GetSession().GetPlayer();

            if (!float.TryParse(args.NextString(), out var x))
                return false;

            if (!float.TryParse(args.NextString(),out var y))
                return false;

            // prevent accept wrong numeric args
            if (x == 0.0f || y == 0.0f)
                return false;

            var idStr = handler.ExtractKeyFromLink(args, "Harea");       // string or [name] Shift-click form |color|Harea:area_id|h[name]|h|r
            if (!uint.TryParse(idStr, out var areaId))
                areaId = player.GetZoneId();

            var areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (x < 0 || x > 100 || y < 0 || y > 100 || areaEntry == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidZoneCoord, x, y, areaId);
                return false;
            }

            // update to parent zone if exist (client map show only zones without parents)
            var zoneEntry = areaEntry.ParentAreaID != 0 ? CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID) : areaEntry;
            Cypher.Assert(zoneEntry != null);

            var map = Global.MapMgr.CreateBaseMap(zoneEntry.ContinentID);

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
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            var z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(zoneEntry.ContinentID, x, y, z, player.GetOrientation());
            return true;
        }

        //teleport at coordinates, including Z and orientation
        [Command("xyz", RBACPermissions.CommandGoXyz)]
        static bool HandleGoXYZCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var player = handler.GetSession().GetPlayer();

            if (!float.TryParse(args.NextString(), out var x))
                return false;

            if (!float.TryParse(args.NextString(), out var y))
                return false;

            var goZ = args.NextString();

            if (!uint.TryParse(args.NextString(), out var mapId))
                mapId = player.GetMapId();

            if (!float.TryParse(args.NextString(), out var ort))
                ort =  player.GetOrientation();

            float z;
            if (!goZ.IsEmpty())
            {
                if (!float.TryParse(goZ, out z))
                    return false;

                if (!GridDefines.IsValidMapCoord(mapId, x, y, z))
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
                var map = Global.MapMgr.CreateBaseMap(mapId);
                z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));
            }

            // stop flight if need
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            player.TeleportTo(mapId, x, y, z, ort);
            return true;
        }

        [Command("bugticket", RBACPermissions.CommandGoBugTicket)]
        static bool HandleGoBugTicketCommand(StringArguments args, CommandHandler handler)
        {
            return HandleGoTicketCommand<BugTicket>(args, handler);
        }

        [Command("complaintticket", RBACPermissions.CommandGoComplaintTicket)]
        static bool HandleGoComplaintTicketCommand(StringArguments args, CommandHandler handler)
        {
            return HandleGoTicketCommand<ComplaintTicket>(args, handler);
        }

        [Command("suggestionticket", RBACPermissions.CommandGoSuggestionTicket)]
        static bool HandleGoSuggestionTicketCommand(StringArguments args, CommandHandler handler)
        {
            return HandleGoTicketCommand<SuggestionTicket>(args, handler);
        }

        static bool HandleGoTicketCommand<T>(StringArguments args, CommandHandler handler)where T : Ticket
        {
            if (args.Empty())
                return false;

            var ticketId = args.NextUInt32();
            if (ticketId == 0)
                return false;

            var ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            var player = handler.GetSession().GetPlayer();
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            else
                player.SaveRecallPosition();

            ticket.TeleportTo(player);
            return true;
        }

        [Command("offset", RBACPermissions.CommandGoOffset)]
        static bool HandleGoOffsetCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var player = handler.GetSession().GetPlayer();

            var goX = args.NextString();
            var goY = args.NextString();
            var goZ = args.NextString();
            var port = args.NextString();

            float x, y, z, o;
            player.GetPosition(out x, out y, out z, out o);
            if (!goX.IsEmpty())
                x += float.Parse(goX);
            if (!goY.IsEmpty())
                y += float.Parse(goY);
            if (!goZ.IsEmpty())
                z += float.Parse(goZ);
            if (!port.IsEmpty())
                o += float.Parse(port);

            if (!GridDefines.IsValidMapCoord(x, y, z, o))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, player.GetMapId());
                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            player.TeleportTo(player.GetMapId(), x, y, z, o);
            return true;
        }
    }
}
