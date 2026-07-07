// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Collision;
using System;
using System.Collections.Generic;
using System.IO;
using static Detour;
using static Recast;

namespace Game.MMaps
{
    public class TileBuilder(string inputDirectory, string outputDirectory, float? maxWalkableAngle, float? maxWalkableAngleNotSteep, bool skipLiquid, bool bigBaseUnit, bool debugOutput, List<OffMeshData> offMeshConnections)
    {
        public TerrainBuilder m_terrainBuilder = new(inputDirectory, skipLiquid);
        public bool m_debugOutput = debugOutput;
        // build performance - not really used for now
        rcContext m_rcContext = new(false);

        public static Func<uint, VMapManager> CreateVMapManager;

        void buildTile(uint mapID, uint tileX, uint tileY, dtNavMesh navMesh)
        {
            if (shouldSkipTile(mapID, tileX, tileY))
            {
                OnTileDone();
                return;
            }

            Log.outInfo(LogFilter.Maps, $"{GetProgressText()} [Map {mapID:04}] Building tile [{tileX:02},{tileY:02}]");

            MeshData meshData = new();

            VMapManager vmapManager = CreateVMapManager(mapID);

            // get heightmap data
            m_terrainBuilder.LoadMap(mapID, tileX, tileY, meshData, vmapManager);

            // get model data
            m_terrainBuilder.LoadVMap(mapID, tileX, tileY, meshData, vmapManager);

            // if there is no data, give up now
            if (meshData.solidVerts.Empty() && meshData.liquidVerts.Empty())
            {
                OnTileDone();
                return;
            }

            // remove unused vertices
            TerrainBuilder.cleanVertices(meshData.solidVerts, meshData.solidTris);
            TerrainBuilder.cleanVertices(meshData.liquidVerts, meshData.liquidTris);

            if (meshData.liquidVerts.Empty() && meshData.solidVerts.Empty())
            {
                OnTileDone();
                return;
            }

            // gather all mesh data for final data check, and bounds calculation
            float[] allVerts = [.. meshData.liquidVerts, .. meshData.solidVerts];

            // get bounds of current tile
            float[] bmin = new float[3];
            float[] bmax = new float[3];
            getTileBounds(tileX, tileY, allVerts, allVerts.Length / 3, bmin, bmax);

            if (offMeshConnections != null)
                m_terrainBuilder.loadOffMeshConnections(mapID, tileX, tileY, meshData, offMeshConnections);

            // build navmesh tile
            TileResult tileResult = buildMoveMapTile(mapID, tileX, tileY, meshData, bmin, bmax, navMesh.getParams());
            if (tileResult.data != null)
                saveMoveMapTileToFile(mapID, tileX, tileY, navMesh, tileResult);

            OnTileDone();
        }

        /**************************************************************************/
        public TileResult buildMoveMapTile(uint mapID, uint tileX, uint tileY, MeshData meshData, float[] bmin, float[] bmax, dtNavMeshParams navMeshParams, string fileNameSuffix = "")
        {
            // console output
            string tileString = $"[Map {mapID:04}] [{tileX:02},{tileY:02}]";
            Log.outInfo(LogFilter.Maps, $"{tileString}: Building movemap tile...");

            TileResult tileResult = default;

            float[] tVerts = meshData.solidVerts.ToArray();
            int tVertCount = meshData.solidVerts.Count / 3;
            int[] tTris = meshData.solidTris.ToArray();
            int tTriCount = meshData.solidTris.Count / 3;

            float[] lVerts = meshData.liquidVerts.ToArray();
            int lVertCount = meshData.liquidVerts.Count / 3;
            int[] lTris = meshData.liquidTris.ToArray();
            int lTriCount = meshData.liquidTris.Count / 3;
            byte[] lTriFlags = meshData.liquidType.ToArray();

            TileConfig tileConfig = new TileConfig(bigBaseUnit);
            int TILES_PER_MAP = tileConfig.TILES_PER_MAP;
            float BASE_UNIT_DIM = tileConfig.BASE_UNIT_DIM;
            rcConfig config = GetMapSpecificConfig(mapID, bmin, bmax, tileConfig);

            // this sets the dimensions of the heightfield - should maybe happen before border padding
            rcCalcGridSize(config.bmin, config.bmax, config.cs, out config.width, out config.height);

            // allocate subregions : tiles
            Tile[] tiles = new Tile[TILES_PER_MAP * TILES_PER_MAP];

            // Initialize per tile config.
            rcConfig tileCfg = config;
            tileCfg.width = config.tileSize + config.borderSize * 2;
            tileCfg.height = config.tileSize + config.borderSize * 2;

            // merge per tile poly and detail meshes
            rcPolyMesh[] pmmerge = new rcPolyMesh[TILES_PER_MAP * TILES_PER_MAP];
            rcPolyMeshDetail[] dmmerge = new rcPolyMeshDetail[TILES_PER_MAP * TILES_PER_MAP];
            int nmerge = 0;
            // build all tiles
            for (int y = 0; y < TILES_PER_MAP; ++y)
            {
                for (int x = 0; x < TILES_PER_MAP; ++x)
                {
                    Tile tile = tiles[x + y * TILES_PER_MAP];

                    // Calculate the per tile bounding box.
                    tileCfg.bmin[0] = config.bmin[0] + x * (float)(config.tileSize * config.cs);
                    tileCfg.bmin[2] = config.bmin[2] + y * (float)(config.tileSize * config.cs);
                    tileCfg.bmax[0] = config.bmin[0] + (x + 1) * (float)(config.tileSize * config.cs);
                    tileCfg.bmax[2] = config.bmin[2] + (y + 1) * (float)(config.tileSize * config.cs);

                    tileCfg.bmin[0] -= tileCfg.borderSize * tileCfg.cs;
                    tileCfg.bmin[2] -= tileCfg.borderSize * tileCfg.cs;
                    tileCfg.bmax[0] += tileCfg.borderSize * tileCfg.cs;
                    tileCfg.bmax[2] += tileCfg.borderSize * tileCfg.cs;

                    // build heightfield
                    if (!rcCreateHeightfield(m_rcContext, tile.solid, tileCfg.width, tileCfg.height, tileCfg.bmin, tileCfg.bmax, tileCfg.cs, tileCfg.ch))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed building heightfield!");
                        continue;
                    }

                    // mark all walkable tiles, both liquids and solids

                    /* we want to have triangles with slope less than walkableSlopeAngleNotSteep (<= 55) to have NAV_AREA_GROUND
                     * and with slope between walkableSlopeAngleNotSteep and walkableSlopeAngle (55 < .. <= 70) to have NAV_AREA_GROUND_STEEP.
                     * we achieve this using recast API: memset everything to NAV_AREA_GROUND_STEEP, call rcClearUnwalkableTriangles with 70 so
                     * any area above that will get RC_NULL_AREA (unwalkable), then call rcMarkWalkableTriangles with 55 to set NAV_AREA_GROUND
                     * on anything below 55 . Players and idle Creatures can use NAV_AREA_GROUND, while Creatures in combat can use NAV_AREA_GROUND_STEEP.
                     */
                    byte[] triFlags = new byte[tTriCount];
                    Array.Fill(triFlags, (byte)NavArea.GroundSteep);
                    rcClearUnwalkableTriangles(m_rcContext, tileCfg.walkableSlopeAngle, tVerts, tVertCount, tTris, tTriCount, triFlags);
                    rcMarkWalkableTriangles(m_rcContext, tileCfg.walkableSlopeAngleNotSteep, tVerts, tVertCount, tTris, tTriCount, triFlags, (byte)NavArea.Ground);
                    rcRasterizeTriangles(m_rcContext, tVerts, tVertCount, tTris, triFlags, tTriCount, tile.solid, config.walkableClimb);

                    rcFilterLowHangingWalkableObstacles(m_rcContext, config.walkableClimb, tile.solid);
                    rcFilterLedgeSpans(m_rcContext, tileCfg.walkableHeight, tileCfg.walkableClimb, tile.solid);
                    rcFilterWalkableLowHeightSpans(m_rcContext, tileCfg.walkableHeight, tile.solid);

                    // add liquid triangles
                    rcRasterizeTriangles(m_rcContext, lVerts, lVertCount, lTris, lTriFlags, lTriCount, tile.solid, config.walkableClimb);

                    // compact heightfield spans
                    if (!rcBuildCompactHeightfield(m_rcContext, tileCfg.walkableHeight, tileCfg.walkableClimb, tile.solid, tile.chf))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed compacting heightfield!");
                        continue;
                    }

                    // build polymesh intermediates
                    if (!rcErodeWalkableArea(m_rcContext, config.walkableRadius, tile.chf))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed eroding area!");
                        continue;
                    }

                    if (!rcMedianFilterWalkableArea(m_rcContext, tile.chf))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed filtering area!");
                        continue;
                    }

                    if (!rcBuildDistanceField(m_rcContext, tile.chf))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed building distance field!");
                        continue;
                    }

                    if (!rcBuildRegions(m_rcContext, tile.chf, tileCfg.borderSize, tileCfg.minRegionArea, tileCfg.mergeRegionArea))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed building regions!");
                        continue;
                    }

                    if (!rcBuildContours(m_rcContext, tile.chf, tileCfg.maxSimplificationError, tileCfg.maxEdgeLen, tile.cset))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed building contours!");
                        continue;
                    }

                    // build polymesh
                    if (!rcBuildPolyMesh(m_rcContext, tile.cset, tileCfg.maxVertsPerPoly, tile.pmesh))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed building polymesh!");
                        continue;
                    }

                    if (!rcBuildPolyMeshDetail(m_rcContext, tile.pmesh, tile.chf, tileCfg.detailSampleDist, tileCfg.detailSampleMaxError, tile.dmesh))
                    {
                        Log.outError(LogFilter.MapGen, $"{tileString} Failed building polymesh detail!");
                        continue;
                    }

                    // free those up
                    // we may want to keep them in the future for debug
                    // but right now, we don't have the code to merge them
                    tile.solid = null;
                    tile.chf = null;
                    tile.cset = null;

                    pmmerge[nmerge] = tile.pmesh;
                    dmmerge[nmerge] = tile.dmesh;
                    nmerge++;
                }
            }

            rcPolyMesh polyMesh = new();
            rcMergePolyMeshes(m_rcContext, pmmerge, nmerge, polyMesh);

            rcPolyMeshDetail polyMeshDetail = new();
            rcMergePolyMeshDetails(m_rcContext, dmmerge, nmerge, polyMeshDetail);

            // free things up
            pmmerge = null;
            dmmerge = null;
            tiles = null;

            // set polygons as walkable
            // TODO: special flags for DYNAMIC polygons, ie surfaces that can be turned on and off
            for (int i = 0; i < polyMesh.npolys; ++i)
            {
                byte area = (byte)(polyMesh.areas[i] & (byte)NavArea.AllMask);
                if (area != 0)
                {
                    if (area >= (byte)NavArea.MinValue)
                        polyMesh.flags[i] = (byte)(1 << ((byte)NavArea.MaxValue - area));
                    else
                        polyMesh.flags[i] = (byte)NavArea.Ground; // TODO: these will be dynamic in future
                }
            }

            // setup mesh parameters
            dtNavMeshCreateParams createParams = new()
            {
                verts = polyMesh.verts,
                vertCount = polyMesh.nverts,
                polys = polyMesh.polys,
                polyAreas = polyMesh.areas,
                polyFlags = polyMesh.flags,
                polyCount = polyMesh.npolys,
                nvp = polyMesh.nvp,
                detailMeshes = polyMeshDetail.meshes,
                detailVerts = polyMeshDetail.verts,
                detailVertsCount = polyMeshDetail.nverts,
                detailTris = polyMeshDetail.tris,
                detailTriCount = polyMeshDetail.ntris,

                offMeshConVerts = meshData.offMeshConnections.ToArray(),
                offMeshConCount = meshData.offMeshConnections.Count / 6,
                offMeshConRad = meshData.offMeshConnectionRads.ToArray(),
                offMeshConDir = meshData.offMeshConnectionDirs.ToArray(),
                offMeshConAreas = meshData.offMeshConnectionsAreas.ToArray(),
                offMeshConFlags = meshData.offMeshConnectionsFlags.ToArray(),

                walkableHeight = BASE_UNIT_DIM * config.walkableHeight,    // agent height
                walkableRadius = BASE_UNIT_DIM * config.walkableRadius,    // agent radius
                walkableClimb = BASE_UNIT_DIM * config.walkableClimb,      // keep less that walkableHeight (aka agent height)!
                tileX = (int)((((bmin[0] + bmax[0]) / 2) - navMeshParams.orig[0]) / MapConst.SizeofGrids),
                tileY = (int)((((bmin[2] + bmax[2]) / 2) - navMeshParams.orig[2]) / MapConst.SizeofGrids)
            };
            rcVcopy(createParams.bmin, bmin);
            rcVcopy(createParams.bmax, bmax);
            createParams.cs = config.cs;
            createParams.ch = config.ch;
            createParams.tileLayer = 0;
            createParams.buildBvTree = true;


            /*auto debugOutputWriter = Trinity::make_unique_ptr_with_deleter(m_debugOutput ? iv : null,                [borderSize = static_cast<byte[]>(config.borderSize),
            outputDir = outputDirectory, fileNameSuffix,            mapID, tileX, tileY, meshData](IntermediateValues * intermediate)
            {
                // restore padding so that the debug visualization is correct
                for (std::ptrdiff_t i = 0; i < intermediate.polyMesh.nverts; ++i)
                {
                    unsigned v = intermediate.polyMesh.verts[i * 3];
                    v[0] += borderSize;
                    v[2] += borderSize;
                }

                intermediate.generateObjFile(outputDir, fileNameSuffix, mapID, tileX, tileY, meshData);
                intermediate.writeIV(outputDir, fileNameSuffix, mapID, tileX, tileY);
            }*/
            //});

            // these values are checked within dtCreateNavMeshData - handle them here
            // so we have a clear error message
            if (createParams.nvp > DT_VERTS_PER_POLYGON)
            {
                Log.outError(LogFilter.MapGen, $"{tileString} Invalid verts-per-polygon value!");
                return tileResult;
            }

            if (createParams.vertCount >= 0xffff)
            {
                Log.outError(LogFilter.MapGen, $"{tileString} Too many vertices!");
                return tileResult;
            }

            if (createParams.vertCount == 0 || createParams.verts == null)
            {
                // occurs mostly when adjacent tiles have models
                // loaded but those models don't span into this tile

                // message is an annoyance
                //Log.outError(LogFilter.MapGen, $"{tileString} No vertices to build tile!");
                return tileResult;
            }

            if (createParams.polyCount == 0 || createParams.polys == null)
            {
                // we have flat tiles with no actual geometry - don't build those, its useless
                // keep in mind that we do output those into debug info
                Log.outError(LogFilter.MapGen, $"{tileString} No polygons to build on tile!");
                return tileResult;
            }

            if (createParams.detailMeshes == null || createParams.detailVerts == null || createParams.detailTris == null)
            {
                Log.outError(LogFilter.MapGen, $"{tileString} No detail mesh to build tile!");
                return tileResult;
            }

            Log.outDebug(LogFilter.MapGen, "Building navmesh tile...");
            if (!dtCreateNavMeshData(createParams, out dtRawTileData navData))
            {
                Log.outError(LogFilter.MapGen, $"{tileString} Failed building navmesh tile!");
                return tileResult;
            }

            tileResult.data = navData;
            return tileResult;
        }

        public void saveMoveMapTileToFile(uint mapID, uint tileX, uint tileY, dtNavMesh navMesh, TileResult tileResult, string fileNameSuffix = "")
        {
            ulong tileRef = 0;

            if (navMesh != null)
            {
                Log.outDebug(LogFilter.MapGen, $"[Map {mapID:04}] [{tileX:02},{tileY:02}]: Adding tile to navmesh...");
                // DT_TILE_FREE_DATA tells detour to unallocate memory when the tile
                // is removed via removeTile()
                uint dtResult = navMesh.addTile(tileResult.data, 0, 0, ref tileRef);
                if (tileRef == 0 || !dtStatusSucceed(dtResult))
                {
                    Log.outError(LogFilter.MapGen, "[Map {:04}] [{:02},{:02}]: Failed adding tile to navmesh!", mapID, tileX, tileY);
                    return;
                }

                navMesh.removeTile(tileRef, out _);
            }

            // file output
            string fileName = $"{outputDirectory}/mmaps/{mapID:04}_{tileX:02}_{tileY:02}{fileNameSuffix}.mmtile";
            using (BinaryWriter binaryWriter = new(File.Open(fileName, FileMode.Create, FileAccess.Write)))
            {
                Log.outDebug(LogFilter.MapGen, $"[Map {mapID:04}] [{tileX:02},{tileY:02}]: Writing to file...");

                // write header
                MmapTileHeader header = new()
                {
                    usesLiquids = (byte)(m_terrainBuilder.UsesLiquids() ? 1 : 0),
                    size = (uint)tileResult.size
                };
                binaryWriter.Write(header);

                // write data
                binaryWriter.Write(tileResult.data.ToBytes());
            }
        }

        /**************************************************************************/
        public void getTileBounds(uint tileX, uint tileY, float[] verts, int vertCount, float[] bmin, float[] bmax)
        {
            // this is for elevation
            if (verts != null && vertCount != 0)
                rcCalcBounds(verts, vertCount, bmin, bmax);
            else
            {
                bmin[1] = 1.175494351e-38F;
                bmax[1] = float.MaxValue;
            }

            // this is for width and depth
            bmax[0] = (32 - tileY) * MapConst.SizeofGrids;
            bmax[2] = (32 - tileX) * MapConst.SizeofGrids;
            bmin[0] = bmax[0] - MapConst.SizeofGrids;
            bmin[2] = bmax[2] - MapConst.SizeofGrids;
        }

        /**************************************************************************/
        bool shouldSkipTile(uint mapID, uint tileX, uint tileY)
        {
            if (m_debugOutput)
                return false;

            return true;
        }

        rcConfig GetMapSpecificConfig(uint mapID, float[] bmin, float[] bmax, TileConfig tileConfig)
        {
            rcConfig config = new();

            rcVcopy(config.bmin, bmin);
            rcVcopy(config.bmax, bmax);

            config.maxVertsPerPoly = DT_VERTS_PER_POLYGON;
            config.cs = tileConfig.BASE_UNIT_DIM;
            config.ch = tileConfig.BASE_UNIT_DIM;
            // Keeping these 2 slope angles the same reduces a lot the number of polys.
            // 55 should be the minimum, maybe 70 is ok (keep in mind blink uses mmaps), 85 is too much for players
            config.walkableSlopeAngle = maxWalkableAngle.GetValueOrDefault(55.0f);
            config.walkableSlopeAngleNotSteep = maxWalkableAngleNotSteep.GetValueOrDefault(55.0f);
            config.tileSize = tileConfig.VERTEX_PER_TILE;
            config.walkableRadius = bigBaseUnit ? 1 : 2;
            config.borderSize = config.walkableRadius + 3;
            config.maxEdgeLen = tileConfig.VERTEX_PER_TILE + 1;        // anything bigger than tileSize
            config.walkableHeight = bigBaseUnit ? 3 : 6;
            // a value >= 3|6 allows npcs to walk over some fences
            // a value >= 4|8 allows npcs to walk over all fences
            config.walkableClimb = bigBaseUnit ? 3 : 6;
            config.minRegionArea = 60 * 60;
            config.mergeRegionArea = 50 * 50;
            config.maxSimplificationError = 1.8f;           // eliminates most jagged edges (tiny polygons)
            config.detailSampleDist = config.cs * 16;
            config.detailSampleMaxError = config.ch * 1;

            switch (mapID)
            {
                // Blade's Edge Arena
                case 562:
                    // This allows to walk on the ropes to the pillars
                    config.walkableRadius = 0;
                    break;
                // Blackfathom Deeps
                case 48:
                    // Reduce the chance to have underground levels
                    config.ch *= 2;
                    break;
                default:
                    break;
            }

            return config;
        }

        string GetProgressText()
        {
            return "";
        }

        public virtual void OnTileDone() { }

        public struct TileResult
        {
            public dtRawTileData data;
            public int size;
        }
    }

    struct Tile
    {
        public rcCompactHeightfield chf;
        public rcHeightfield solid;
        public rcContourSet cset;
        public rcPolyMesh pmesh;
        public rcPolyMeshDetail dmesh;
    }

    struct TileConfig
    {
        public float BASE_UNIT_DIM;
        public int VERTEX_PER_MAP;
        public int VERTEX_PER_TILE;
        public int TILES_PER_MAP;

        public TileConfig(bool bigBaseUnit)
        {
            // these are WORLD UNIT based metrics
            // this are basic unit dimentions
            // value have to divide GRID_SIZE(533.3333f) ( aka: 0.5333, 0.2666, 0.3333, 0.1333, etc )
            BASE_UNIT_DIM = bigBaseUnit ? 0.5333333f : 0.2666666f;

            // All are in UNIT metrics!
            VERTEX_PER_MAP = (int)(MapConst.SizeofGrids / BASE_UNIT_DIM + 0.5f);
            VERTEX_PER_TILE = bigBaseUnit ? 40 : 80; // must divide VERTEX_PER_MAP
            TILES_PER_MAP = VERTEX_PER_MAP / VERTEX_PER_TILE;
        }
    }
}
