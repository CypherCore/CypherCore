// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Movement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Game
{
    public class MMapManager : Singleton<MMapManager>
    {
        MMapManager() { }

        const string MAP_FILE_NAME_FORMAT = "{0}/mmaps/{1:D4}.mmap";
        const string TILE_FILE_NAME_FORMAT = "{0}/mmaps/{1:D4}_{2:D2}_{3:D2}.mmtile";

        public void Initialize(MultiMap<uint, uint> mapData)
        {
            foreach (var pair in mapData)
                parentMapData[pair.Value] = pair.Key;
        }

        MMapData GetMMapData(uint mapId)
        {
            return loadedMMaps.LookupByKey(mapId);
        }

        MMapLoadResult LoadMapData(string basePath, uint mapId)
        {
            // we already have this map loaded?
            if (loadedMMaps.ContainsKey(mapId) && loadedMMaps[mapId] != null)
                return MMapLoadResult.AlreadyLoaded;

            // load and init dtNavMesh - read parameters from file
            MMapLoadResult paramsResult = parseNavMeshParamsFile(basePath, mapId, out Detour.dtNavMeshParams Params);
            if (paramsResult != MMapLoadResult.Success)
                return paramsResult;

            Detour.dtNavMesh mesh = new();
            if (Detour.dtStatusFailed(mesh.init(Params)))
            {
                Log.outError(LogFilter.Maps, $"MMAP:loadMapData: Failed to initialize dtNavMesh for mmap {mapId:D4}");
                return MMapLoadResult.LibraryError;
            }

            Log.outDebug(LogFilter.Maps, $"MMAP:loadMapData: Loaded {mapId:04}.mmap");

            // store inside our map list
            loadedMMaps[mapId] = new MMapData(mesh);
            return MMapLoadResult.Success;
        }

        MMapLoadResult parseNavMeshParamsFile(string basePath, uint mapId, out Detour.dtNavMeshParams Params, List<OffMeshData> offmeshConnections = null)
        {
            Params = new();

            string fileName = string.Format(MAP_FILE_NAME_FORMAT, basePath, mapId);
            if (!File.Exists(fileName))
            {
                Log.outError(LogFilter.Maps, $"Could not open mmap file {fileName}");
                return MMapLoadResult.FileNotFound;
            }

            using BinaryReader reader = new(new FileStream(fileName, FileMode.Open, FileAccess.Read), Encoding.UTF8);
            if (reader.ReadUInt32() != MapConst.mmapMagic)
            {
                Log.outError(LogFilter.Maps, $"MMAP:loadMap: Bad header in mmap {mapId:04}.mmap");
                return MMapLoadResult.VersionMismatch;
            }

            uint mmapVersion = reader.ReadUInt32();
            if (mmapVersion != MapConst.mmapVersion)
            {
                Log.outError(LogFilter.Maps, $"MMAP:loadMap: {mapId:04}.mmap was built with generator v{mmapVersion}, expected v{MapConst.mmapVersion}");
                return MMapLoadResult.VersionMismatch;
            }

            Params.orig[0] = reader.ReadSingle();
            Params.orig[1] = reader.ReadSingle();
            Params.orig[2] = reader.ReadSingle();

            Params.tileWidth = reader.ReadSingle();
            Params.tileHeight = reader.ReadSingle();
            Params.maxTiles = reader.ReadInt32();
            Params.maxPolys = reader.ReadInt32();

            if (offmeshConnections != null)
            {
                var offmeshConnectionCount = reader.ReadUInt32();
                for (var i = 0; i < offmeshConnectionCount; ++i)
                {
                    var offMeshData = new OffMeshData
                    {
                        MapId = reader.ReadUInt32(),
                        TileX = reader.ReadUInt32(),
                        TileY = reader.ReadUInt32(),
                        From = reader.ReadArray<float>(3),
                        To = reader.ReadArray<float>(3),
                        Radius = reader.ReadSingle(),
                        ConnectionFlags = (OffMeshConnectionFlag)reader.ReadByte(),
                        AreaId = reader.ReadByte(),
                        Flags = (NavTerrainFlag)reader.ReadUInt16()
                    };
                    offmeshConnections.Add(new OffMeshData());
                }

                if (offmeshConnectionCount != offmeshConnections.Count)
                {
                    offmeshConnections.Clear();
                   Log.outDebug(LogFilter.Maps, $"MMAP:loadMapData: Error: Could not read offmesh connections from file '{fileName}'");
                    return MMapLoadResult.ReadFromFileFailed;
                }
            }

            return MMapLoadResult.Success;
        }

        uint PackTileID(int x, int y)
        {
            return (uint)(x << 16 | y);
        }

        public MMapLoadResult LoadMap(string basePath, uint mapId, int x, int y)
        {
            // make sure the mmap is loaded and ready to load tiles
            MMapLoadResult mapResult = LoadMapData(basePath, mapId);
            switch (mapResult)
            {
                case MMapLoadResult.Success:
                case MMapLoadResult.AlreadyLoaded:
                    break;
                default:
                    return mapResult;
            }

            // get this mmap data
            MMapData mmap = loadedMMaps[mapId];
            Cypher.Assert(mmap.navMesh != null);

            // check if we already have this tile loaded
            uint packedGridPos = PackTileID(x, y);
            if (mmap.loadedTileRefs.ContainsKey(packedGridPos))
                return MMapLoadResult.AlreadyLoaded;

            // load this tile . mmaps/MMM_XX_YY.mmtile
            string fileName = string.Format(TILE_FILE_NAME_FORMAT, basePath, mapId, x, y);
            if (!File.Exists(fileName))
            {
                if (parentMapData.ContainsKey(mapId))
                    fileName = string.Format(TILE_FILE_NAME_FORMAT, basePath, parentMapData[mapId], x, y);
            }

            if (!File.Exists(fileName))
            {
                Log.outDebug(LogFilter.Maps, "MMAP:loadMap: Could not open mmtile file '{0}'", fileName);
                return MMapLoadResult.FileNotFound;
            }

            using BinaryReader reader = new(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            MmapTileHeader fileHeader = reader.Read<MmapTileHeader>();
            if (fileHeader.mmapMagic != MapConst.mmapMagic)
            {
                Log.outError(LogFilter.Maps, "MMAP:loadMap: Bad header in mmap {0:D4}_{1:D2}_{2:D2}.mmtile", mapId, x, y);
                return MMapLoadResult.VersionMismatch;
            }
            if (fileHeader.mmapVersion != MapConst.mmapVersion)
            {
                Log.outError(LogFilter.Maps, "MMAP:loadMap: {0:D4}_{1:D2}_{2:D2}.mmtile was built with generator v{3}, expected v{4}",
                    mapId, x, y, fileHeader.mmapVersion, MapConst.mmapVersion);
                return MMapLoadResult.VersionMismatch;
            }

            var bytes = reader.ReadBytes((int)fileHeader.size);
            Detour.dtRawTileData data = new();
            data.FromBytes(bytes, 0);

            ulong tileRef = 0;
            // memory allocated for data is now managed by detour, and will be deallocated when the tile is removed
            if (Detour.dtStatusSucceed(mmap.navMesh.addTile(data, 1, 0, ref tileRef)))
            {
                mmap.loadedTileRefs.Add(packedGridPos, tileRef);
                ++loadedTiles;
                Log.outInfo(LogFilter.Maps, "MMAP:loadMap: Loaded mmtile {0:D4}[{1:D2}, {2:D2}]", mapId, x, y);
                return MMapLoadResult.Success;
            }

            Log.outError(LogFilter.Maps, "MMAP:loadMap: Could not load {0:D4}_{1:D2}_{2:D2}.mmtile into navmesh", mapId, x, y);
            return MMapLoadResult.LibraryError;
        }

        public bool LoadMapInstance(string basePath, uint meshMapId, uint instanceMapId, uint instanceId)
        {
            switch (LoadMapData(basePath, meshMapId))
            {
                case MMapLoadResult.Success:
                case MMapLoadResult.AlreadyLoaded:
                    break;
                default:
                    return false;
            }

            MMapData mmap = loadedMMaps[meshMapId];
            if (mmap.navMeshQueries.ContainsKey((instanceMapId, instanceId)))
                return true;

            // allocate mesh query
            Detour.dtNavMeshQuery query = new();
            if (Detour.dtStatusFailed(query.init(mmap.navMesh, 1024)))
            {
                Log.outError(LogFilter.Maps, $"MMAP.GetNavMeshQuery: Failed to initialize dtNavMeshQuery for mapId {instanceMapId:D4} instanceId {instanceId}");
                return false;
            }

            Log.outDebug(LogFilter.Maps, $"MMAP.GetNavMeshQuery: created dtNavMeshQuery for mapId {instanceMapId:D4} instanceId {instanceId}");
            mmap.navMeshQueries.Add((instanceMapId, instanceId), query);
            return true;
        }

        public bool UnloadMap(uint mapId, int x, int y)
        {
            // check if we have this map loaded
            MMapData mmap = GetMMapData(mapId);
            if (mmap == null)
            {
                // file may not exist, therefore not loaded
                Log.outDebug(LogFilter.Maps, "MMAP:unloadMap: Asked to unload not loaded navmesh map. {0:D4}_{1:D2}_{2:D2}.mmtile", mapId, x, y);
                return false;
            }

            // check if we have this tile loaded
            uint packedGridPos = PackTileID(x, y);
            if (!mmap.loadedTileRefs.ContainsKey(packedGridPos))
            {
                // file may not exist, therefore not loaded
                Log.outDebug(LogFilter.Maps, "MMAP:unloadMap: Asked to unload not loaded navmesh tile. {0:D4}_{1:D2}_{2:D2}.mmtile", mapId, x, y);
                return false;
            }

            ulong tileRef = mmap.loadedTileRefs[packedGridPos];

            // unload, and mark as non loaded
            if (Detour.dtStatusFailed(mmap.navMesh.removeTile(tileRef, out _)))
            {
                // this is technically a memory leak
                // if the grid is later reloaded, dtNavMesh.addTile will return error but no extra memory is used
                // we cannot recover from this error - assert out
                Log.outError(LogFilter.Maps, "MMAP:unloadMap: Could not unload {0:D4}_{1:D2}_{2:D2}.mmtile from navmesh", mapId, x, y);
                Cypher.Assert(false);
            }
            else
            {
                mmap.loadedTileRefs.Remove(packedGridPos);
                --loadedTiles;
                Log.outInfo(LogFilter.Maps, "MMAP:unloadMap: Unloaded mmtile {0:D4}[{1:D2}, {2:D2}] from {3:D4}", mapId, x, y, mapId);
                return true;
            }

            return false;
        }

        public bool UnloadMap(uint mapId)
        {
            if (!loadedMMaps.ContainsKey(mapId))
            {
                // file may not exist, therefore not loaded
                Log.outDebug(LogFilter.Maps, "MMAP:unloadMap: Asked to unload not loaded navmesh map {0:D4}", mapId);
                return false;
            }

            // unload all tiles from given map
            MMapData mmap = loadedMMaps.LookupByKey(mapId);
            foreach (var i in mmap.loadedTileRefs)
            {
                uint x = (i.Key >> 16);
                uint y = (i.Key & 0x0000FFFF);
                if (Detour.dtStatusFailed(mmap.navMesh.removeTile(i.Value, out _)))
                    Log.outError(LogFilter.Maps, "MMAP:unloadMap: Could not unload {0:D4}_{1:D2}_{2:D2}.mmtile from navmesh", mapId, x, y);
                else
                {
                    --loadedTiles;
                    Log.outInfo(LogFilter.Maps, "MMAP:unloadMap: Unloaded mmtile {0:D4} [{1:D2}, {2:D2}] from {3:D4}", mapId, x, y, mapId);
                }
            }

            loadedMMaps.Remove(mapId);
            Log.outInfo(LogFilter.Maps, "MMAP:unloadMap: Unloaded {0:D4}.mmap", mapId);

            return true;
        }

        public bool UnloadMapInstance(uint meshMapId, uint instanceMapId, uint instanceId)
        {
            // check if we have this map loaded
            MMapData mmap = GetMMapData(meshMapId);
            if (mmap == null)
            {
                // file may not exist, therefore not loaded
                Log.outDebug(LogFilter.Maps, $"MMAP:unloadMapInstance: Asked to unload not loaded navmesh map {meshMapId}");
                return false;
            }

            if (!mmap.navMeshQueries.ContainsKey((instanceMapId, instanceId)))
            {
                Log.outDebug(LogFilter.Maps, $"MMAP:unloadMapInstance: Asked to unload not loaded dtNavMeshQuery mapId {instanceMapId} instanceId {instanceId}");
                return false;
            }

            mmap.navMeshQueries.Remove((instanceMapId, instanceId));
            Log.outInfo(LogFilter.Maps, $"MMAP:unloadMapInstance: Unloaded mapId {instanceMapId} instanceId {instanceId}");

            return true;
        }

        public Detour.dtNavMesh GetNavMesh(uint mapId)
        {
            MMapData mmap = GetMMapData(mapId);
            if (mmap == null)
                return null;

            return mmap.navMesh;
        }

        public Detour.dtNavMeshQuery GetNavMeshQuery(uint meshMapId, uint instanceMapId, uint instanceId)
        {
            MMapData mmap = GetMMapData(meshMapId);
            if (mmap == null)
                return null;

            return mmap.navMeshQueries.LookupByKey((instanceMapId, instanceId));
        }

        public uint GetLoadedTilesCount() { return loadedTiles; }
        public int GetLoadedMapsCount() { return loadedMMaps.Count; }

        Dictionary<uint, MMapData> loadedMMaps = new();
        uint loadedTiles;

        Dictionary<uint, uint> parentMapData = new();
    }

    public class MMapData
    {
        public MMapData(Detour.dtNavMesh mesh)
        {
            navMesh = mesh;
        }

        public Dictionary<(uint, uint), Detour.dtNavMeshQuery> navMeshQueries = new();     // instanceId to query

        public Detour.dtNavMesh navMesh;
        public Dictionary<uint, ulong> loadedTileRefs = new(); // maps [map grid coords] to [dtTile]
    }

    public struct MmapTileHeader
    {
        public uint mmapMagic;
        public uint dtVersion;
        public uint mmapVersion;
        public uint size;
        public byte usesLiquids;
    }

    public class OffMeshData
    {
        public uint MapId;
        public uint TileX;
        public uint TileY;
        public float[] From = new float[3];
        public float[] To = new float[3];
        public float Radius;
        public OffMeshConnectionFlag ConnectionFlags;
        public byte AreaId;
        public NavTerrainFlag Flags;
    }

    public enum MMapLoadResult
    {
        Success,
        AlreadyLoaded,
        FileNotFound,
        VersionMismatch,
        ReadFromFileFailed,
        LibraryError
    }

    public enum OffMeshConnectionFlag : byte
    {
        OFFMESH_CONNECTION_FLAG_BIDIRECTIONAL = 0x01
    }
}
