/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
using Game.DataStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Game
{
    public class MMapManager : Singleton<MMapManager>
    {
        MMapManager() { }

        const string MAP_FILE_NAME_FORMAT = "{0}/mmaps/{1:D4}.mmap";
        const string TILE_FILE_NAME_FORMAT = "{0}/mmaps/{1:D4}{2:D2}{3:D2}.mmtile";

        public void Initialize()
        {
            foreach (MapRecord mapEntry in CliDB.MapStorage.Values)
            {
                if (mapEntry.ParentMapID != -1)
                    phaseMapData.Add((uint)mapEntry.ParentMapID, mapEntry.Id);
            }
        }

        MMapData GetMMapData(uint mapId)
        {
            return loadedMMaps.LookupByKey(mapId);
        }

        bool loadMapData(uint mapId)
        {
            // we already have this map loaded?
            if (loadedMMaps.ContainsKey(mapId) && loadedMMaps[mapId] != null)
                return true;

            // load and init dtNavMesh - read parameters from file
            string filename = string.Format(MAP_FILE_NAME_FORMAT, Global.WorldMgr.GetDataPath(), mapId);
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
                loadedMMaps[mapId] = new MMapData(mesh, mapId);
                return true;
            }
        }

        uint packTileID(int x, int y)
        {
            return (uint)(x << 16 | y);
        }

        public bool loadMap(uint mapId, int x, int y)
        {
            // make sure the mmap is loaded and ready to load tiles
            if (!loadMapData(mapId))
                return false;

            // get this mmap data
            MMapData mmap = loadedMMaps[mapId];
            Contract.Assert(mmap.navMesh != null);

            // check if we already have this tile loaded
            uint packedGridPos = packTileID(x, y);
            if (mmap.loadedTileRefs.ContainsKey(packedGridPos))
                return false;

            // load this tile . mmaps/MMMXXYY.mmtile
            string filename = string.Format(TILE_FILE_NAME_FORMAT, Global.WorldMgr.GetDataPath(), mapId, x, y);
            if (!File.Exists(filename))
            {
                Log.outDebug(LogFilter.Maps, "MMAP:loadMap: Could not open mmtile file '{0}'", filename);
                return false;
            }

            using (BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                MmapTileHeader fileHeader = reader.ReadStruct<MmapTileHeader>();
                Array.Reverse(fileHeader.mmapMagic);
                if (new string(fileHeader.mmapMagic) != MapConst.mmapMagic)
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
                if (Detour.dtStatusSucceed(mmap.navMesh.addTile(data, 0, 0, ref tileRef)))
                {
                    mmap.loadedTileRefs.Add(packedGridPos, tileRef);
                    ++loadedTiles;
                    Log.outInfo(LogFilter.Maps, "MMAP:loadMap: Loaded mmtile {0:D4}[{1:D2}, {2:D2}]", mapId, x, y);

                    var phasedMaps = phaseMapData.LookupByKey(mapId);
                    if (!phasedMaps.Empty())
                    {
                        mmap.AddBaseTile(packedGridPos, data, fileHeader, fileHeader.size);
                        LoadPhaseTiles(phasedMaps, x, y);
                    }

                    return true;
                }

                Log.outError(LogFilter.Maps, "MMAP:loadMap: Could not load {0:D4}{1:D2}{2:D2}.mmtile into navmesh", mapId, x, y);
                return false;
            }
        }

        PhasedTile LoadTile(uint mapId, int x, int y)
        {
            // load this tile . mmaps/MMMXXYY.mmtile
            string filename = string.Format(TILE_FILE_NAME_FORMAT, Global.WorldMgr.GetDataPath(), mapId, x, y);
            if (!File.Exists(filename))
            {
                // Not all tiles have phased versions, don't flood this msg
                return null;
            }
            PhasedTile pTile = new PhasedTile();

            using (BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                // read header
                pTile.fileHeader = reader.ReadStruct<MmapTileHeader>();
                Array.Reverse(pTile.fileHeader.mmapMagic);
                if (new string(pTile.fileHeader.mmapMagic) != MapConst.mmapMagic)
                {
                    Log.outError(LogFilter.Maps, "MMAP.LoadTile: Bad header in mmap {0:D4}{1:D2}{2:D2}.mmtile", mapId, x, y);
                    return null;
                }

                if (pTile.fileHeader.mmapVersion != MapConst.mmapVersion)
                {
                    Log.outError(LogFilter.Maps, "MMAP:LoadTile: {0:D4}{1:D2}{2:D2}.mmtile was built with generator v{3}, expected v{4}", mapId, x, y, pTile.fileHeader.mmapVersion, MapConst.mmapVersion);
                    return null;
                }

                pTile.data = new Detour.dtRawTileData();
                pTile.data.FromBytes(reader.ReadBytes((int)pTile.fileHeader.size), 0);

                if (pTile.data.ToBytes().Length == 0)
                {
                    Log.outError(LogFilter.Maps, "MMAP.LoadTile: Bad header or data in mmap {0:D4}{1:D2}{2:D2}.mmtile", mapId, x, y);
                    return null;
                }
            }

            return pTile;
        }

        void LoadPhaseTiles(List<uint> phasedMapData, int x, int y)
        {
            Log.outDebug(LogFilter.Maps, "MMAP.LoadPhaseTiles: Loading phased mmtiles for map {0}, X: {1}, Y: {2}", phasedMapData.FirstOrDefault(), x, y);

            uint packedGridPos = packTileID(x, y);

            foreach (uint phaseMapId in phasedMapData)
            {
                PhasedTile data = LoadTile(phaseMapId, x, y);
                // only a few tiles have terrain swaps, do not write error for them
                if (data != null)
                {
                    Log.outDebug(LogFilter.Maps, "MMAP.LoadPhaseTiles: Loaded phased {0:D4}{1:D2}{2:D2}.mmtile for root phase map {3}", phaseMapId, x, y, phasedMapData.FirstOrDefault());
                    if (!_phaseTiles.ContainsKey(phaseMapId))
                        _phaseTiles[phaseMapId] = new Dictionary<uint, PhasedTile>();

                    _phaseTiles[phaseMapId][packedGridPos] = data;
                }
            }
        }

        void UnloadPhaseTile(List<uint> phasedMapData, int x, int y)
        {
            Log.outDebug(LogFilter.Maps, "MMAP.UnloadPhaseTile: Unloading phased mmtile for map {0}, X: {1}, Y: {2}", phasedMapData.FirstOrDefault(), x, y);

            uint packedGridPos = packTileID(x, y);

            foreach (uint phaseMapId in phasedMapData.ToList())
            {
                var phasedTileDic = _phaseTiles.LookupByKey(phaseMapId);
                if (phasedTileDic == null)
                    continue;

                var phaseTile = phasedTileDic.LookupByKey(packedGridPos);
                if (phaseTile != null)
                {
                    Log.outDebug(LogFilter.Maps, "MMAP.UnloadPhaseTile: Unloaded phased {0:D4}{1:D2}{2:D2}.mmtile for root phase map {3}", phaseMapId, x, y, phasedMapData.FirstOrDefault());
                    phasedTileDic.Remove(packedGridPos);
                }
            }
        }

        public bool unloadMap(uint mapId, uint x, uint y)
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
            uint packedGridPos = packTileID((int)x, (int)y);
            if (!mmap.loadedTileRefs.ContainsKey(packedGridPos))
            {
                // file may not exist, therefore not loaded
                Log.outDebug(LogFilter.Maps, "MMAP:unloadMap: Asked to unload not loaded navmesh tile. {0:D4}{1:D2}{2:D2}.mmtile", mapId, x, y);
                return false;
            }

            ulong tileRef = mmap.loadedTileRefs.LookupByKey(packedGridPos);

            // unload, and mark as non loaded
            Detour.dtRawTileData data;
            if (Detour.dtStatusFailed(mmap.navMesh.removeTile(tileRef, out data)))
            {
                // this is technically a memory leak
                // if the grid is later reloaded, dtNavMesh.addTile will return error but no extra memory is used
                // we cannot recover from this error - assert out
                Log.outError(LogFilter.Maps, "MMAP:unloadMap: Could not unload {0:D4}{1:D2}{2:D2}.mmtile from navmesh", mapId, x, y);
                Contract.Assert(false);
            }
            else
            {
                mmap.loadedTileRefs.Remove(packedGridPos);
                --loadedTiles;
                Log.outInfo(LogFilter.Maps, "MMAP:unloadMap: Unloaded mmtile {0:D4}[{1:D2}, {2:D2}] from {3:D4}", mapId, x, y, mapId);

                var phasedMaps = phaseMapData.LookupByKey(mapId);
                if (!phasedMaps.Empty())
                {
                    mmap.DeleteBaseTile(packedGridPos);
                    UnloadPhaseTile(phasedMaps, (int)x, (int)y);
                }
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
                    var phasedMaps = phaseMapData.LookupByKey(mapId);
                    if (!phasedMaps.Empty())
                    {
                        mmap.DeleteBaseTile(i.Key);
                        UnloadPhaseTile(phasedMaps, (int)x, (int)y);
                    }
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

        public Detour.dtNavMesh GetNavMesh(uint mapId, List<uint> swaps)
        {
            MMapData mmap = GetMMapData(mapId);
            if (mmap == null)
                return null;

            return mmap.GetNavMesh(swaps);
        }

        public Detour.dtNavMeshQuery GetNavMeshQuery(uint mapId, uint instanceId, List<uint> swaps)
        {
            MMapData mmap = GetMMapData(mapId);
            if (mmap == null)
                return null;

            if (!mmap.navMeshQueries.ContainsKey(instanceId))
            {
                // allocate mesh query
                Detour.dtNavMeshQuery query = new Detour.dtNavMeshQuery();
                if (Detour.dtStatusFailed(query.init(mmap.GetNavMesh(swaps), 1024)))
                {
                    Log.outError(LogFilter.Maps, "MMAP:GetNavMeshQuery: Failed to initialize dtNavMeshQuery for mapId {0} instanceId {1}", mapId, instanceId);
                    return null;
                }

                Log.outInfo(LogFilter.Maps, "MMAP:GetNavMeshQuery: created dtNavMeshQuery for mapId {0} instanceId {1}", mapId, instanceId);
                mmap.navMeshQueries.Add(instanceId, query);
            }

            return mmap.navMeshQueries[instanceId];
        }

        public uint getLoadedTilesCount() { return loadedTiles; }
        public int getLoadedMapsCount() { return loadedMMaps.Count; }

        public Dictionary<uint, PhasedTile> GetPhaseTileContainer(uint mapId) { return _phaseTiles.LookupByKey(mapId); }

        Dictionary<uint, MMapData> loadedMMaps = new Dictionary<uint, MMapData>();
        MultiMap<uint, uint> phaseMapData = new MultiMap<uint, uint>();
        Dictionary<uint, Dictionary<uint, PhasedTile>> _phaseTiles = new Dictionary<uint, Dictionary<uint, PhasedTile>>();
        uint loadedTiles;
    }

    public class MMapData
    {
        public MMapData(Detour.dtNavMesh mesh, uint mapId)
        {
            navMesh = mesh;
            _mapId = mapId;
        }

        void RemoveSwap(PhasedTile ptile, uint swap, uint packedXY)
        {
            uint x = (packedXY >> 16);
            uint y = (packedXY & 0x0000FFFF);

            if (!loadedPhasedTiles[swap].Contains(packedXY))
            {
                Log.outDebug(LogFilter.Maps, "MMapData.RemoveSwap: mmtile {0:D4}[{1:D2}, {2:D2}] unload skipped, due to not loaded", swap, x, y);
                return;
            }
            Detour.dtMeshHeader header = ptile.data.header;

            Detour.dtRawTileData data;
            // remove old tile
            if (Detour.dtStatusFailed(navMesh.removeTile(loadedTileRefs[packedXY], out data)))
                Log.outError(LogFilter.Maps, "MMapData.RemoveSwap: Could not unload phased {0:D4}{1:D2}{2:D2}.mmtile from navmesh", swap, x, y);
            else
            {
                Log.outDebug(LogFilter.Maps, "MMapData.RemoveSwap: Unloaded phased {0:D4}{1:D2}{2:D2}.mmtile from navmesh", swap, x, y);

                // restore base tile
                ulong loadedRef = 0;
                if (Detour.dtStatusSucceed(navMesh.addTile(_baseTiles[packedXY].data, 0, 0, ref loadedRef)))
                {
                    Log.outDebug(LogFilter.Maps, "MMapData.RemoveSwap: Loaded base mmtile {0:D4}[{1:D2}, {2:D2}] into {0:D4}[{1:D2}, {2:D2}]", _mapId, x, y, _mapId, header.x, header.y);
                }
                else
                    Log.outError(LogFilter.Maps, "MMapData.RemoveSwap: Could not load base {0:D4}{1:D2}{2:D2}.mmtile to navmesh", _mapId, x, y);

                loadedTileRefs[packedXY] = loadedRef;
            }

            loadedPhasedTiles.Remove(swap, packedXY);

            if (loadedPhasedTiles[swap].Empty())
            {
                _activeSwaps.Remove(swap);
                Log.outDebug(LogFilter.Maps, "MMapData.RemoveSwap: Fully removed swap {0} from map {1}", swap, _mapId);
            }
        }

        void AddSwap(PhasedTile ptile, uint swap, uint packedXY)
        {
            uint x = (packedXY >> 16);
            uint y = (packedXY & 0x0000FFFF);

            if (!loadedTileRefs.ContainsKey(packedXY))
            {
                Log.outDebug(LogFilter.Maps, "MMapData.AddSwap: phased mmtile {0:D4}[{1:D2}, {2:D2}] load skipped, due to not loaded base tile on map {3}", swap, x, y, _mapId);
                return;
            }
            if (loadedPhasedTiles[swap].Contains(packedXY))
            {
                Log.outDebug(LogFilter.Maps, "MMapData.AddSwap: WARNING! phased mmtile {0:D4}[{1:D2}, {2:D2}] load skipped, due to already loaded on map {3}", swap, x, y, _mapId);
                return;
            }

            Detour.dtMeshHeader header = ptile.data.header;

            Detour.dtMeshTile oldTile = navMesh.getTileByRef(loadedTileRefs[packedXY]);
            if (oldTile == null)
            {
                Log.outDebug(LogFilter.Maps, "MMapData.AddSwap: phased mmtile {0:D4}[{1:D2}, {2:D2}] load skipped, due to not loaded base tile ref on map {3}", swap, x, y, _mapId);
                return;
            }

            // header xy is based on the swap map's tile set, wich doesn't have all the same tiles as root map, so copy the xy from the orignal header
            header.x = oldTile.header.x;
            header.y = oldTile.header.y;

            Detour.dtRawTileData data;
            // remove old tile
            if (Detour.dtStatusFailed(navMesh.removeTile(loadedTileRefs[packedXY], out data)))
                Log.outError(LogFilter.Maps, "MMapData.AddSwap: Could not unload {0:D4}{1:D2}{2:D2}.mmtile from navmesh", _mapId, x, y);
            else
            {
                Log.outDebug(LogFilter.Maps, "MMapData.AddSwap: Unloaded {0:D4}{1:D2}{2:D2}.mmtile from navmesh", _mapId, x, y);

                _activeSwaps.Add(swap);
                loadedPhasedTiles.Add(swap, packedXY);

                // add new swapped tile
                ulong loadedRef = 0;
                if (Detour.dtStatusSucceed(navMesh.addTile(ptile.data, 0, 0, ref loadedRef)))
                    Log.outDebug(LogFilter.Maps, "MMapData.AddSwap: Loaded phased mmtile {0:D4}[{1:D2}, {2:D2}] into {0:D4}[{1:D2}, {2:D2}]", swap, x, y, _mapId, header.x, header.y);
                else
                    Log.outError(LogFilter.Maps, "MMapData.AddSwap: Could not load {0:D4}{1:D2}{2:D2}.mmtile to navmesh", swap, x, y);

                loadedTileRefs[packedXY] = loadedRef;
            }
        }

        public Detour.dtNavMesh GetNavMesh(List<uint> swaps)
        {
            foreach (uint swap in _activeSwaps)
            {
                if (!swaps.Contains(swap)) // swap not active
                {
                    var ptc = Global.MMapMgr.GetPhaseTileContainer(swap);
                    foreach (var pair in ptc)
                        RemoveSwap(pair.Value, swap, pair.Key); // remove swap
                }
            }

            // for each of the calling unit's terrain swaps
            foreach (uint swap in swaps)
            {
                if (!_activeSwaps.Contains(swap)) // swap not active
                {
                    // for each of the terrain swap's xy tiles
                    var ptc = Global.MMapMgr.GetPhaseTileContainer(swap);
                    if (ptc != null)
                    {
                        foreach (var pair in ptc)
                            AddSwap(pair.Value, swap, pair.Key); // add swap
                    }
                }
            }

            return navMesh;
        }

        public void AddBaseTile(uint packedGridPos, Detour.dtRawTileData data, MmapTileHeader fileHeader, uint dataSize)
        {
            if (!_baseTiles.ContainsKey(packedGridPos))
            {
                PhasedTile phasedTile = new PhasedTile();
                phasedTile.data = data;
                phasedTile.fileHeader = fileHeader;
                phasedTile.dataSize = (int)dataSize;
                _baseTiles[packedGridPos] = phasedTile;
            }
        }

        public void DeleteBaseTile(uint packedGridPos)
        {
            var phaseTile = _baseTiles.LookupByKey(packedGridPos);
            if (phaseTile != null)
            {
                _baseTiles.Remove(packedGridPos);
            }
        }

        public Dictionary<uint, Detour.dtNavMeshQuery> navMeshQueries = new Dictionary<uint, Detour.dtNavMeshQuery>();     // instanceId to query

        public Detour.dtNavMesh navMesh;
        public Dictionary<uint, ulong> loadedTileRefs = new Dictionary<uint, ulong>();
        MultiMap<uint, uint> loadedPhasedTiles = new MultiMap<uint, uint>();

        uint _mapId;
        Dictionary<uint, PhasedTile> _baseTiles = new Dictionary<uint, PhasedTile>();
        List<uint> _activeSwaps = new List<uint>();
    }

    public class PhasedTile
    {
        public Detour.dtRawTileData data;
        public MmapTileHeader fileHeader;
        public int dataSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MmapTileHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] mmapMagic;
        public uint dtVersion;
        public uint mmapVersion;
        public uint size;
        public byte usesLiquids;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] padding;
    }
}
