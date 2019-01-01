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

            Player player = handler.GetSession().GetPlayer();

            // "id" or number or [name] Shift-click form |color|Hcreature_entry:creature_id|h[name]|h|r
            string param1 = handler.extractKeyFromLink(args, "Hcreature", "Hcreature_entry");
            if (string.IsNullOrEmpty(param1))
                return false;

            string whereClause = "";

            // User wants to teleport to the NPC's template entry
            if (param1.IsNumber())
            {
                if (!int.TryParse(param1, out int entry) || entry == 0)
                {
                    if (!ulong.TryParse(param1, out ulong guidLow) || guidLow == 0)
                        return false;

                    whereClause += "WHERE guid = '" + guidLow + '\'';
                }
                else
                    whereClause += "WHERE id = '" + entry + '\'';
            }
            else
            {
                // param1 is not a number, must be mob's name
                whereClause += ", creature_template WHERE creature.id = creature_template.entry AND creature_template.name LIKE '" + param1 + '\'';
            }

            SQLResult result = DB.World.Query("SELECT position_x, position_y, position_z, orientation, map FROM creature {0}", whereClause);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);
                return false;
            }

            float x = result.Read<float>(0);
            float y = result.Read<float>(1);
            float z = result.Read<float>(2);
            float o = result.Read<float>(3);
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
            Player player = handler.GetSession().GetPlayer();

            if (args.Empty())
                return false;

            uint graveyardId = args.NextUInt32();
            if (graveyardId == 0)
                return false;

            WorldSafeLocsRecord gy = CliDB.WorldSafeLocsStorage.LookupByKey(graveyardId);
            if (gy == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGraveyardnoexist, graveyardId);
                return false;
            }

            if (!GridDefines.IsValidMapCoord(gy.MapID, gy.Loc.X, gy.Loc.Y, gy.Loc.Z))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, gy.Loc.X, gy.Loc.Y, gy.MapID);
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

            player.TeleportTo(gy.MapID, gy.Loc.X, gy.Loc.Y, gy.Loc.Z, (gy.Facing * MathFunctions.PI) / 180); // Orientation is initially in degrees
            return true;
        }

        //teleport to grid
        [Command("grid", RBACPermissions.CommandGoGrid)]
        static bool HandleGoGridCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player player = handler.GetSession().GetPlayer();

            if (!float.TryParse(args.NextString(), out float gridX))
                return false;

            if (!float.TryParse(args.NextString(), out float gridY))
                return false;

            if (!uint.TryParse(args.NextString(), out uint mapId))
                mapId = player.GetMapId();

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
            {
                player.GetMotionMaster().MovementExpired();
                player.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                player.SaveRecallPosition();

            Map map = Global.MapMgr.CreateBaseMap(mapId);
            float z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(mapId, x, y, z, player.GetOrientation());
            return true;
        }

        //teleport to gameobject
        [Command("object", RBACPermissions.CommandGoObject)]
        static bool HandleGoObjectCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player player = handler.GetSession().GetPlayer();

            // number or [name] Shift-click form |color|Hgameobject:go_guid|h[name]|h|r
            string id = handler.extractKeyFromLink(args, "Hgameobject");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!ulong.TryParse(id, out ulong guidLow) || guidLow == 0)
                return false;

            float x, y, z, o;
            uint mapId;

            // by DB guid
            GameObjectData goData = Global.ObjectMgr.GetGOData(guidLow);
            if (goData != null)
            {
                x = goData.posX;
                y = goData.posY;
                z = goData.posZ;
                o = goData.orientation;
                mapId = goData.mapid;
            }
            else
            {
                handler.SendSysMessage(CypherStrings.CommandGoobjnotfound);
                return false;
            }

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

        [Command("quest", RBACPermissions.CommandGoQuest)]
        static bool HandleGoQuestCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player player = handler.GetSession().GetPlayer();

            string id = handler.extractKeyFromLink(args, "Hquest");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out uint questID) || questID == 0)
                return false;

            if (Global.ObjectMgr.GetQuestTemplate(questID) == null)
            {
                handler.SendSysMessage(CypherStrings.CommandQuestNotfound, questID);
                return false;
            }

            float x, y, z = 0;
            uint mapId = 0;

            var poiData = Global.ObjectMgr.GetQuestPOIList(questID);
            if (poiData != null)
            {
                var data = poiData[0];

                mapId = (uint)data.MapID;

                x = data.points[0].X;
                y = data.points[0].Y;
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

            Map map = Global.MapMgr.CreateBaseMap(mapId);
            z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(mapId, x, y, z, 0.0f);
            return true;
        }

        [Command("taxinode", RBACPermissions.CommandGoTaxinode)]
        static bool HandleGoTaxinodeCommand(StringArguments args, CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();

            if (args.Empty())
                return false;

            string id = handler.extractKeyFromLink(args, "Htaxinode");
            if (string.IsNullOrEmpty(id))
                return false;

            if (!uint.TryParse(id, out uint nodeId) || nodeId == 0)
                return false;

            TaxiNodesRecord node = CliDB.TaxiNodesStorage.LookupByKey(nodeId);
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
            Player player = handler.GetSession().GetPlayer();

            if (args.Empty())
                return false;

            uint areaTriggerId = args.NextUInt32();
            if (areaTriggerId == 0)
                return false;

            AreaTriggerRecord at = CliDB.AreaTriggerStorage.LookupByKey(areaTriggerId);
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

            Player player = handler.GetSession().GetPlayer();

            if (!float.TryParse(args.NextString(), out float x))
                return false;

            if (!float.TryParse(args.NextString(),out float y))
                return false;

            // prevent accept wrong numeric args
            if (x == 0.0f || y == 0.0f)
                return false;

            string idStr = handler.extractKeyFromLink(args, "Harea");       // string or [name] Shift-click form |color|Harea:area_id|h[name]|h|r
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

            float z = Math.Max(map.GetStaticHeight(PhasingHandler.EmptyPhaseShift, x, y, MapConst.MaxHeight), map.GetWaterLevel(PhasingHandler.EmptyPhaseShift, x, y));

            player.TeleportTo(zoneEntry.ContinentID, x, y, z, player.GetOrientation());
            return true;
        }

        //teleport at coordinates, including Z and orientation
        [Command("xyz", RBACPermissions.CommandGoXyz)]
        static bool HandleGoXYZCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player player = handler.GetSession().GetPlayer();

            if (!float.TryParse(args.NextString(), out float x))
                return false;

            if (!float.TryParse(args.NextString(), out float y))
                return false;

            string goZ = args.NextString();

            if (!uint.TryParse(args.NextString(), out uint mapId))
                mapId = player.GetMapId();

            if (!float.TryParse(args.NextString(), out float ort))
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
                Map map = Global.MapMgr.CreateBaseMap(mapId);
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

            uint ticketId = args.NextUInt32();
            if (ticketId == 0)
                return false;

            T ticket = Global.SupportMgr.GetTicket<T>(ticketId);
            if (ticket == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTicketnotexist);
                return true;
            }

            Player player = handler.GetSession().GetPlayer();
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

            Player player = handler.GetSession().GetPlayer();

            string goX = args.NextString();
            string goY = args.NextString();
            string goZ = args.NextString();
            string port = args.NextString();

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
