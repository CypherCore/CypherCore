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
        const string TILE_FILE_NAME_FORMAT = "{0}/mmaps/{1:D4}{2:D2}{3:D2}.mmtile";

        public void Initialize(MultiMap<uint, uint> mapData)
        {
            childMapData = mapData;

            foreach (var pair in mapData)
                parentMapData[pair.Value] = pair.Key;
        }

        MMapData GetMMapData(uint mapId)
        {
            return loadedMMaps.LookupByKey(mapId);
        }

        bool loadMapData(string basePath, uint mapId)
        {
            // we already have this map loaded?
            if (loadedMMaps.ContainsKey(mapId) && loadedMMaps[mapId] != null)
                return true;

            // load and init dtNavMesh - read parameters from file
            string filename = string.Format(MAP_FILE_NAME_FORMAT, basePath, mapId);
            if (!File.Exists(filename))
            {
                Log.outError(LogFilter.Maps, "Could not open mmap file {0}", filename);
                return false;
            }

            using (BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read), Encoding.UTF8))
            {
                Detour.dtNavMeshParams Params = new Detour.dtNavMeshParams();
                Params.orig[0] = reader.ReadSingle();
                Params.orig[1] = reader.ReadSingle();
                Params.orig[2] = reader.ReadSingle();

                Params.tileWidth = reader.ReadSingle();
                Params.tileHeight = reader.ReadSingle();
                Params.maxTiles = reader.ReadInt32();
                Params.maxPolys = reader.ReadInt32();

                Detour.dtNavMesh mesh = new Detour.dtNavMesh();
                if (Detour.dtStatusFailed(mesh.init(Params)))
                {
                    Log.outError(LogFilter.Maps, "MMAP:loadMapData: Failed to initialize dtNavMesh for mmap {0:D4} from file {1}", mapId, filename);
                    return false;
                }

                Log.outInfo(LogFilter.Maps, "MMAP:loadMapData: Loaded {0:D4}.mmap", mapId);

                // store inside our map list
                loadedMMaps[mapId] = new MMapData(mesh);
                return true;
            }
        }

        uint packTileID(uint x, uint y)
        {
            return (x << 16 | y);
        }

        public bool loadMap(string basePath, uint mapId, uint x, uint y)
        {
            // make sure the mmap is loaded and ready to load tiles
            if (!loadMapImpl(basePath, mapId, x, y))
                return false;

            bool success = true;
            var childMaps = childMapData.LookupByKey(mapId);
            foreach (uint childMapId in childMaps)
                if (!loadMapImpl(basePath, childMapId, x, y))
                    success = false;

            return success;
        }

        bool loadMapImpl(string basePath, uint mapId, uint x, uint y)
        {
            // make sure the mmap is loaded and ready to load tiles
            if (!loadMapData(basePath, mapId))
                return false;

            // get this mmap data
            MMapData mmap = loadedMMaps[mapId];
            Cypher.Assert(mmap.navMesh != null);

            // check if we already have this tile loaded
            uint packedGridPos = packTileID(x, y);
            if (mmap.loadedTileRefs.ContainsKey(packedGridPos))
                return false;

            // load this tile . mmaps/MMMXXYY.mmtile
            string fileName = string.Format(TILE_FILE_NAME_FORMAT, basePath, mapId, x, y);
            if (!File.Exists(fileName))
            {
                if (parentMapData.ContainsKey(mapId))
                    fileName = string.Format(TILE_FILE_NAME_FORMAT, basePath, parentMapData[mapId], x, y);
            }

            if (!File.Exists(fileName))
            { 
                Log.outDebug(LogFilter.Maps, "MMAP:loadMap: Could not open mmtile file '{0}'", fileName);
                return false;
            }

            using (BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
            {
                MmapTileHeader fileHeader = reader.Read<MmapTileHeader>();
                if (fileHeader.mmapMagic != MapConst.mmapMagic)
                {
                    Log.outError(LogFilter.Maps, "MMAP:loadMap: Bad header in mmap {0:D4}{1:D2}{2:D2}.mmtile", mapId, x, y);
                    return false;
                }
                if (fileHeader.mmapVersion != MapConst.mmapVersion)
                {
                    Log.outError(LogFilter.Maps, "MMAP:loadMap: {0:D4}{1:D2}{2:D2}.mmtile was built with generator v{3}, expected v{4}",
                        mapId, x, y, fileHeader.mmapVersion, MapConst.mmapVersion);
                    return false;
                }

                var bytes = reader.ReadBytes((int)fileHeader.size);
                Detour.dtRawTileData data = new Detour.dtRawTileData();
                data.FromBytes(bytes, 0);

                ulong tileRef = 0;
                // memory allocated for data is now managed by detour, and will be deallocated when the tile is removed
                if (Detour.dtStatusSucceed(mmap.navMesh.addTile(data, 1, 0, ref tileRef)))
                {
                    mmap.loadedTileRefs.Add(packedGridPos, tileRef);
                    ++loadedTiles;
                    Log.outInfo(LogFilter.Maps, "MMAP:loadMap: Loaded mmtile {0:D4}[{1:D2}, {2:D2}]", mapId, x, y);
                    return true;
                }

                Log.outError(LogFilter.Maps, "MMAP:loadMap: Could not load {0:D4}{1:D2}{2:D2}.mmtile into navmesh", mapId, x, y);
                return false;
            }
        }

        public bool loadMapInstance(string basePath, uint mapId, uint instanceId)
        {
            if (!loadMapInstanceImpl(basePath, mapId, instanceId))
                return false;

            bool success = true;
            var childMaps = childMapData.LookupByKey(mapId);
            foreach (uint childMapId in childMaps)
                if (!loadMapInstanceImpl(basePath, childMapId, instanceId))
                    success = false;

            return success;
        }

        bool loadMapInstanceImpl(string basePath, uint mapId, uint instanceId)
        {
            if (!loadMapData(basePath, mapId))
                return false;

            MMapData mmap = loadedMMaps[mapId];
            if (mmap.navMeshQueries.ContainsKey(instanceId))
                return true;

            // allocate mesh query
            Detour.dtNavMeshQuery query = new Detour.dtNavMeshQuery();
            if (Detour.dtStatusFailed(query.init(mmap.navMesh, 1024)))
            {
                Log.outError(LogFilter.Maps, "MMAP.GetNavMeshQuery: Failed to initialize dtNavMeshQuery for mapId {0:D4} instanceId {1}", mapId, instanceId);
                return false;
            }

            Log.outDebug(LogFilter.Maps, "MMAP.GetNavMeshQuery: created dtNavMeshQuery for mapId {0:D4} instanceId {1}", mapId, instanceId);
            mmap.navMeshQueries.Add(instanceId, query);
            return true;
        }

        public bool unloadMap(uint mapId, uint x, uint y)
        {
            var childMaps = childMapData.LookupByKey(mapId);
            foreach (uint childMapId in childMaps)
                unloadMapImpl(childMapId, x, y);

            return unloadMapImpl(mapId, x, y);
        }

        bool unloadMapImpl(uint mapId, uint x, uint y)
        {
            // check if we have this map loaded
            MMapData mmap = GetMMapData(mapId);
            if (mmap == null)
            {
                // file may not exist, therefore not loaded
                Log.outDebug(LogFilter.Maps, "MMAP:unloadMap: Asked to unload not loaded navmesh map. {0:D4}{1:D2}{2:D2}.mmtile", mapId, x, y);
                return false;
            }

            // check if we have this tile loaded
            uint packedGridPos = packTileID(x, y);
            if (!mmap.loadedTileRefs.ContainsKey(packedGridPos))
            {
                // file may not exist, therefore not loaded
                Log.outDebug(LogFilter.Maps, "MMAP:unloadMap: Asked to unload not loaded navmesh tile. {0:D4}{1:D2}{2:D2}.mmtile", mapId, x, y);
                return false;
            }

            ulong tileRef = mmap.loadedTileRefs[packedGridPos];

            // unload, and mark as non loaded
            Detour.dtRawTileData data;
            if (Detour.dtStatusFailed(mmap.navMesh.removeTile(tileRef, out data)))
            {
                // this is technically a memory leak
                // if the grid is later reloaded, dtNavMesh.addTile will return error but no extra memory is used
                // we cannot recover from this error - assert out
                Log.outError(LogFilter.Maps, "MMAP:unloadMap: Could not unload {0:D4}{1:D2}{2:D2}.mmtile from navmesh", mapId, x, y);
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

        public bool unloadMap(uint mapId)
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
                Detour.dtRawTileData data;
                if (Detour.dtStatusFailed(mmap.navMesh.removeTile(i.Value, out data)))
                    Log.outError(LogFilter.Maps, "MMAP:unloadMap: Could not unload {0:D4}{1:D2}{2:D2}.mmtile from navmesh", mapId, x, y);
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

        public bool unloadMapInstance(uint mapId, uint instanceId)
        {
            // check if we have this map loaded
            MMapData mmap = GetMMapData(mapId);
            if (mmap == null)
            {
                // file may not exist, therefore not loaded
                Log.outDebug(LogFilter.Maps, "MMAP:unloadMapInstance: Asked to unload not loaded navmesh map {0}", mapId);
                return false;
            }

            if (!mmap.navMeshQueries.ContainsKey(instanceId))
            {
                Log.outDebug(LogFilter.Maps, "MMAP:unloadMapInstance: Asked to unload not loaded dtNavMeshQuery mapId {0} instanceId {1}", mapId, instanceId);
                return false;
            }

            mmap.navMeshQueries.Remove(instanceId);
            Log.outInfo(LogFilter.Maps, "MMAP:unloadMapInstance: Unloaded mapId {0} instanceId {1}", mapId, instanceId);

            return true;
        }

        public Detour.dtNavMesh GetNavMesh(uint mapId)
        {
            MMapData mmap = GetMMapData(mapId);
            if (mmap == null)
                return null;

            return mmap.navMesh;
        }

        public Detour.dtNavMeshQuery GetNavMeshQuery(uint mapId, uint instanceId)
        {
            MMapData mmap = GetMMapData(mapId);
            if (mmap == null)
                return null;

            return mmap.navMeshQueries[instanceId];
        }

        public uint getLoadedTilesCount() { return loadedTiles; }
        public int getLoadedMapsCount() { return loadedMMaps.Count; }

        Dictionary<uint, MMapData> loadedMMaps = new Dictionary<uint, MMapData>();
        uint loadedTiles;

        MultiMap<uint, uint> childMapData = new MultiMap<uint, uint>();
        Dictionary<uint, uint> parentMapData = new Dictionary<uint, uint>();
    }

    public class MMapData
    {
        public MMapData(Detour.dtNavMesh mesh)
        {
            navMesh = mesh;
        }

        public Dictionary<uint, Detour.dtNavMeshQuery> navMeshQueries = new Dictionary<uint, Detour.dtNavMeshQuery>();     // instanceId to query

        public Detour.dtNavMesh navMesh;
        public Dictionary<uint, ulong> loadedTileRefs = new Dictionary<uint, ulong>(); // maps [map grid coords] to [dtTile]
    }

    public struct MmapTileHeader
    {
        public uint mmapMagic;
        public uint dtVersion;
        public uint mmapVersion;
        public uint size;
        public byte usesLiquids;
    }
}
