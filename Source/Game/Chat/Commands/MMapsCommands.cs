// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;
using Game.Maps;
using Game.Movement;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("mmap")]
    class MMapsCommands
    {
        [Command("path", RBACPermissions.CommandMmapPath)]
        static bool HandleMmapPathCommand(CommandHandler handler, StringArguments args)
        {
            if (Global.MMapMgr.GetNavMesh(handler.GetPlayer().GetMapId()) == null)
            {
                handler.SendSysMessage("NavMesh not loaded for current map.");
                return true;
            }

            handler.SendSysMessage("mmap path:");

            // units
            Player player = handler.GetPlayer();
            Unit target = handler.GetSelectedUnit();
            if (player == null || target == null)
            {
                handler.SendSysMessage("Invalid target/source selection.");
                return true;
            }

            string para = args.NextString();

            bool useStraightPath = false;
            if (para.Equals("true", StringComparison.OrdinalIgnoreCase))
                useStraightPath = true;

            bool useRaycast = false;
            if (para.Equals("line", StringComparison.OrdinalIgnoreCase) || para.Equals("ray", StringComparison.OrdinalIgnoreCase) || para.Equals("raycast", StringComparison.OrdinalIgnoreCase))
                useRaycast = true;

            // unit locations
            float x, y, z;
            player.GetPosition(out x, out y, out z);

            // path
            PathGenerator path = new(target);
            path.SetUseStraightPath(useStraightPath);
            path.SetUseRaycast(useRaycast);
            bool result = path.CalculatePath(x, y, z, false);

            var pointPath = path.GetPath();
            handler.SendSysMessage("{0}'s path to {1}:", target.GetName(), player.GetName());
            handler.SendSysMessage("Building: {0}", useStraightPath ? "StraightPath" : useRaycast ? "Raycast" : "SmoothPath");
            handler.SendSysMessage("Result: {0} - Length: {1} - Type: {2}", (result ? "true" : "false"), pointPath.Length, path.GetPathType());

            var start = path.GetStartPosition();
            var end = path.GetEndPosition();
            var actualEnd = path.GetActualEndPosition();

            handler.SendSysMessage("StartPosition     ({0:F3}, {1:F3}, {2:F3})", start.X, start.Y, start.Z);
            handler.SendSysMessage("EndPosition       ({0:F3}, {1:F3}, {2:F3})", end.X, end.Y, end.Z);
            handler.SendSysMessage("ActualEndPosition ({0:F3}, {1:F3}, {2:F3})", actualEnd.X, actualEnd.Y, actualEnd.Z);

            if (!player.IsGameMaster())
                handler.SendSysMessage("Enable GM mode to see the path points.");

            for (uint i = 0; i < pointPath.Length; ++i)
                player.SummonCreature(1, pointPath[i].X, pointPath[i].Y, pointPath[i].Z, 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(9));

            return true;
        }

        [Command("loc", RBACPermissions.CommandMmapLoc)]
        static bool HandleMmapLocCommand(CommandHandler handler)
        {
            handler.SendSysMessage("mmap tileloc:");

            // grid tile location
            Player player = handler.GetPlayer();

            int gx = (int)(32 - player.GetPositionX() / MapConst.SizeofGrids);
            int gy = (int)(32 - player.GetPositionY() / MapConst.SizeofGrids);

            float x, y, z;
            player.GetPosition(out x, out y, out z);

            handler.SendSysMessage("{0:D4}{1:D2}{2:D2}.mmtile", player.GetMapId(), gy, gx);
            handler.SendSysMessage("tileloc [{0}, {1}]", gx, gy);

            // calculate navmesh tile location
            uint terrainMapId = PhasingHandler.GetTerrainMapId(player.GetPhaseShift(), player.GetMapId(), player.GetMap().GetTerrain(), x, y);
            Detour.dtNavMesh navmesh = Global.MMapMgr.GetNavMesh(terrainMapId);
            Detour.dtNavMeshQuery navmeshquery = Global.MMapMgr.GetNavMeshQuery(terrainMapId, player.GetMapId(), player.GetInstanceId());
            if (navmesh == null || navmeshquery == null)
            {
                handler.SendSysMessage("NavMesh not loaded for current map.");
                return true;
            }

            float[] min = navmesh.getParams().orig;
            float[] location = { y, z, x };
            float[] extents = { 3.0f, 5.0f, 3.0f };

            int tilex = (int)((y - min[0]) / MapConst.SizeofGrids);
            int tiley = (int)((x - min[2]) / MapConst.SizeofGrids);

            handler.SendSysMessage("Calc   [{0:D2}, {1:D2}]", tilex, tiley);

            // navmesh poly . navmesh tile location
            Detour.dtQueryFilter filter = new();
            float[] nothing = new float[3];
            ulong polyRef = 0;
            if (Detour.dtStatusFailed(navmeshquery.findNearestPoly(location, extents, filter, ref polyRef, ref nothing)))
            {
                handler.SendSysMessage("Dt     [??,??] (invalid poly, probably no tile loaded)");
                return true;
            }

            if (polyRef == 0)
                handler.SendSysMessage("Dt     [??, ??] (invalid poly, probably no tile loaded)");
            else
            {
                Detour.dtMeshTile tile = new();
                Detour.dtPoly poly = new();
                if (Detour.dtStatusSucceed(navmesh.getTileAndPolyByRef(polyRef, ref tile, ref poly)))
                {
                    if (tile != null)
                    {
                        handler.SendSysMessage("Dt     [{0:D2},{1:D2}]", tile.header.x, tile.header.y);
                        return true;
                    }
                }

                handler.SendSysMessage("Dt     [??,??] (no tile loaded)");
            }
            return true;
        }

        [Command("loadedtiles", RBACPermissions.CommandMmapLoadedtiles)]
        static bool HandleMmapLoadedTilesCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            uint terrainMapId = PhasingHandler.GetTerrainMapId(player.GetPhaseShift(), player.GetMapId(), player.GetMap().GetTerrain(), player.GetPositionX(), player.GetPositionY());
            Detour.dtNavMesh navmesh = Global.MMapMgr.GetNavMesh(terrainMapId);
            Detour.dtNavMeshQuery navmeshquery = Global.MMapMgr.GetNavMeshQuery(terrainMapId, player.GetMapId(), player.GetInstanceId());
            if (navmesh == null || navmeshquery == null)
            {
                handler.SendSysMessage("NavMesh not loaded for current map.");
                return true;
            }

            handler.SendSysMessage("mmap loadedtiles:");

            for (int i = 0; i < navmesh.getMaxTiles(); ++i)
            {
                Detour.dtMeshTile tile = navmesh.getTile(i);
                if (tile.header == null)
                    continue;

                handler.SendSysMessage("[{0:D2}, {1:D2}]", tile.header.x, tile.header.y);
            }
            return true;
        }

        [Command("stats", RBACPermissions.CommandMmapStats)]
        static bool HandleMmapStatsCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();
            uint terrainMapId = PhasingHandler.GetTerrainMapId(player.GetPhaseShift(), player.GetMapId(), player.GetMap().GetTerrain(), player.GetPositionX(), player.GetPositionY());
            handler.SendSysMessage("mmap stats:");
            handler.SendSysMessage("  global mmap pathfinding is {0}abled", Global.DisableMgr.IsPathfindingEnabled(player.GetMapId()) ? "En" : "Dis");
            handler.SendSysMessage(" {0} maps loaded with {1} tiles overall", Global.MMapMgr.GetLoadedMapsCount(), Global.MMapMgr.GetLoadedTilesCount());

            Detour.dtNavMesh navmesh = Global.MMapMgr.GetNavMesh(terrainMapId);
            if (navmesh == null)
            {
                handler.SendSysMessage("NavMesh not loaded for current map.");
                return true;
            }

            uint tileCount = 0;
            int nodeCount = 0;
            int polyCount = 0;
            int vertCount = 0;
            int triCount = 0;
            int triVertCount = 0;
            for (int i = 0; i < navmesh.getMaxTiles(); ++i)
            {
                Detour.dtMeshTile tile = navmesh.getTile(i);
                if (tile.header == null)
                    continue;

                tileCount++;
                nodeCount += tile.header.bvNodeCount;
                polyCount += tile.header.polyCount;
                vertCount += tile.header.vertCount;
                triCount += tile.header.detailTriCount;
                triVertCount += tile.header.detailVertCount;
            }

            handler.SendSysMessage("Navmesh stats:");
            handler.SendSysMessage(" {0} tiles loaded", tileCount);
            handler.SendSysMessage(" {0} BVTree nodes", nodeCount);
            handler.SendSysMessage(" {0} polygons ({1} vertices)", polyCount, vertCount);
            handler.SendSysMessage(" {0} triangles ({1} vertices)", triCount, triVertCount);
            return true;
        }

        [Command("testarea", RBACPermissions.CommandMmapTestarea)]
        static bool HandleMmapTestArea(CommandHandler handler)
        {
            float radius = 40.0f;
            WorldObject obj = handler.GetPlayer();

            // Get Creatures
            List<Unit> creatureList = new();

            var go_check = new AnyUnitInObjectRangeCheck(obj, radius);
            var go_search = new UnitListSearcher(obj, creatureList, go_check);

            Cell.VisitGridObjects(obj, go_search, radius);
            if (creatureList.Empty())
            {
                handler.SendSysMessage("Found {0} Creatures.", creatureList.Count);

                uint paths = 0;
                uint uStartTime = Time.GetMSTime();

                float gx, gy, gz;
                obj.GetPosition(out gx, out gy, out gz);
                foreach (var creature in creatureList)
                {
                    PathGenerator path = new(creature);
                    path.CalculatePath(gx, gy, gz);
                    ++paths;
                }

                uint uPathLoadTime = Time.GetMSTimeDiffToNow(uStartTime);
                handler.SendSysMessage("Generated {0} paths in {1} ms", paths, uPathLoadTime);
            }
            else
                handler.SendSysMessage("No creatures in {0} yard range.", radius);

            return true;
        }
    }
}
