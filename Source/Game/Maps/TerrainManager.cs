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
using Game.Collision;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Game.Maps
{
    public class TerrainManager : Singleton<TerrainManager>
    {
        Dictionary<uint, TerrainInfo> _terrainMaps = new();

        // parent map links
        MultiMap<uint, uint> _parentMapData = new();

        TerrainManager() { }

        public void InitializeParentMapData(MultiMap<uint, uint> mapData)
        {
            _parentMapData = mapData;
        }

        public TerrainInfo LoadTerrain(uint mapId)
        {
            var entry = CliDB.MapStorage.LookupByKey(mapId);
            if (entry == null)
                return null;

            while (entry.ParentMapID != -1 || entry.CosmeticParentMapID != -1)
            {
                uint parentMapId = (uint)(entry.ParentMapID != -1 ? entry.ParentMapID : entry.CosmeticParentMapID);
                entry = CliDB.MapStorage.LookupByKey(parentMapId);
                if (entry == null)
                    break;

                mapId = parentMapId;
            }

            var terrain = _terrainMaps.LookupByKey(mapId);
            if (terrain != null)
                return terrain;

            TerrainInfo terrainInfo = LoadTerrainImpl(mapId);
            _terrainMaps[mapId] = terrainInfo;
            return terrainInfo;
        }

        public void UnloadAll()
        {
            _terrainMaps.Clear();
        }

        public void Update(uint diff)
        {
            // global garbage collection
            foreach (var (mapId, terrain) in _terrainMaps)
                terrain?.CleanUpGrids(diff);
        }

        public uint GetAreaId(PhaseShift phaseShift, uint mapid, Position pos) { return GetAreaId(phaseShift, mapid, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()); }

        public uint GetAreaId(PhaseShift phaseShift, WorldLocation loc) { return GetAreaId(phaseShift, loc.GetMapId(), loc); }

        public uint GetAreaId(PhaseShift phaseShift, uint mapid, float x, float y, float z)
        {
            TerrainInfo terrain = LoadTerrain(mapid);
            if (terrain != null)
                return terrain.GetAreaId(phaseShift, x, y, z);

            return 0;
        }

        public uint GetZoneId(PhaseShift phaseShift, uint mapid, Position pos) { return GetZoneId(phaseShift, mapid, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()); }

        public uint GetZoneId(PhaseShift phaseShift, WorldLocation loc) { return GetZoneId(phaseShift, loc.GetMapId(), loc); }

        public uint GetZoneId(PhaseShift phaseShift, uint mapid, float x, float y, float z)
        {
            TerrainInfo terrain = LoadTerrain(mapid);
            if (terrain != null)
                return terrain.GetZoneId(phaseShift, x, y, z);

            return 0;
        }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, uint mapid, Position pos) { GetZoneAndAreaId(phaseShift, out zoneid, out areaid, mapid, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()); }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, WorldLocation loc) { GetZoneAndAreaId(phaseShift, out zoneid, out areaid, loc.GetMapId(), loc); }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, uint mapid, float x, float y, float z)
        {
            zoneid = areaid = 0;

            TerrainInfo terrain = LoadTerrain(mapid);
            if (terrain != null)
                terrain.GetZoneAndAreaId(phaseShift, out zoneid, out areaid, x, y, z);
        }

        TerrainInfo LoadTerrainImpl(uint mapId)
        {
            TerrainInfo rootTerrain = new TerrainInfo(mapId);

            rootTerrain.DiscoverGridMapFiles();

            foreach (uint childMapId in _parentMapData[mapId])
                rootTerrain.AddChildTerrain(LoadTerrainImpl(childMapId));

            return rootTerrain;
        }

        public static bool ExistMapAndVMap(uint mapid, float x, float y)
        {
            GridCoord p = GridDefines.ComputeGridCoord(x, y);

            int gx = (int)((MapConst.MaxGrids - 1) - p.X_coord);
            int gy = (int)((MapConst.MaxGrids - 1) - p.Y_coord);

            return TerrainInfo.ExistMap(mapid, gx, gy) && TerrainInfo.ExistVMap(mapid, gx, gy);
        }
    }

    public class TerrainInfo
    {
        uint _mapId;

        TerrainInfo _parentTerrain;
        List<TerrainInfo> _childTerrain = new();

        object _loadLock = new();
        GridMap[][] _gridMap = new GridMap[MapConst.MaxGrids][];
        ushort[][] _referenceCountFromMap = new ushort[MapConst.MaxGrids][];

        BitSet _loadedGrids = new(MapConst.MaxGrids * MapConst.MaxGrids);
        BitSet _gridFileExists = new(MapConst.MaxGrids * MapConst.MaxGrids); // cache what grids are available for this map (not including parent/child maps)

        static TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

        // global garbage collection timer
        TimeTracker _cleanupTimer;

        public TerrainInfo(uint mapId)
        {
            _mapId = mapId;
            _cleanupTimer = new TimeTracker(RandomHelper.RandTime(CleanupInterval / 2, CleanupInterval));

            for (var i = 0; i < MapConst.MaxGrids; ++i)
            {
                _gridMap[i] = new GridMap[MapConst.MaxGrids];
                _referenceCountFromMap[i] = new ushort[MapConst.MaxGrids];
            }
        }

        public string GetMapName()
        {
            return CliDB.MapStorage.LookupByKey(GetId()).MapName[Global.WorldMgr.GetDefaultDbcLocale()];
        }

        public void DiscoverGridMapFiles()
        {
            string tileListName = $"{Global.WorldMgr.GetDataPath()}/maps/{GetId():D4}.tilelist";
            // tile list is optional
            if (File.Exists(tileListName))
            {
                using var reader = new BinaryReader(new FileStream(tileListName, FileMode.Open, FileAccess.Read));
                var mapMagic = reader.ReadUInt32();
                var versionMagic = reader.ReadUInt32();
                if (mapMagic == MapConst.MapMagic && versionMagic == MapConst.MapVersionMagic)
                {
                    var build = reader.ReadUInt32();
                    byte[] tilesData = reader.ReadArray<byte>(MapConst.MaxGrids * MapConst.MaxGrids);
                    Array.Reverse(tilesData);
                    for (int gx = 0; gx < MapConst.MaxGrids; ++gx)
                        for (int gy = 0; gy < MapConst.MaxGrids; ++gy)
                            _gridFileExists[GetBitsetIndex(gx, gy)] = tilesData[GetBitsetIndex(gx, gy)] == 49; // char of 1

                    return;
                }
            }

            for (int gx = 0; gx < MapConst.MaxGrids; ++gx)
                for (int gy = 0; gy < MapConst.MaxGrids; ++gy)
                    _gridFileExists[GetBitsetIndex(gx, gy)] = ExistMap(GetId(), gx, gy, false);
        }

        public static bool ExistMap(uint mapid, int gx, int gy, bool log = true)
        {
            string fileName = $"{Global.WorldMgr.GetDataPath()}/maps/{mapid:D4}_{gx:D2}_{gy:D2}.map";

            bool ret = false;
            if (!File.Exists(fileName))
            {
                if (log)
                {
                    Log.outError(LogFilter.Maps, $"Map file '{fileName}' does not exist!");
                    Log.outError(LogFilter.Maps, $"Please place MAP-files (*.map) in the appropriate directory ({Global.WorldMgr.GetDataPath() + "/maps/"}), or correct the DataDir setting in your worldserver.conf file.");
                }
            }
            else
            {
                using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
                var header = reader.Read<MapFileHeader>();
                if (header.mapMagic != MapConst.MapMagic || (header.versionMagic != MapConst.MapVersionMagic && header.versionMagic != MapConst.MapVersionMagic2)) // Hack for some different extractors using v2.0 header
                {
                    if (log)
                        Log.outError(LogFilter.Maps, $"Map file '{fileName}' is from an incompatible map version ({header.versionMagic}), {MapConst.MapVersionMagic} is expected. Please pull your source, recompile tools and recreate maps using the updated mapextractor, then replace your old map files with new files. If you still have problems search on forum for error TCE00018.");
                }
                else
                    ret = true;
            }

            return ret;
        }

        public static bool ExistVMap(uint mapid, int gx, int gy)
        {
            if (Global.VMapMgr.IsMapLoadingEnabled())
            {
                LoadResult result = Global.VMapMgr.ExistsMap(mapid, gx, gy);
                string name = VMapManager.GetMapFileName(mapid);//, gx, gy);
                switch (result)
                {
                    case LoadResult.Success:
                        break;
                    case LoadResult.FileNotFound:
                        Log.outError(LogFilter.Maps, $"VMap file '{Global.WorldMgr.GetDataPath() + "/vmaps/" + name}' does not exist");
                        Log.outError(LogFilter.Maps, $"Please place VMAP files (*.vmtree and *.vmtile) in the vmap directory ({Global.WorldMgr.GetDataPath() + "/vmaps/"}), or correct the DataDir setting in your worldserver.conf file.");
                        return false;
                    case LoadResult.VersionMismatch:
                        Log.outError(LogFilter.Maps, $"VMap file '{Global.WorldMgr.GetDataPath() + "/vmaps/" + name}' couldn't be loaded");
                        Log.outError(LogFilter.Maps, "This is because the version of the VMap file and the version of this module are different, please re-extract the maps with the tools compiled with this module.");
                        return false;
                    case LoadResult.ReadFromFileFailed:
                        Log.outError(LogFilter.Maps, $"VMap file '{Global.WorldMgr.GetDataPath() + "/vmaps/" + name}' couldn't be loaded");
                        Log.outError(LogFilter.Maps, "This is because VMAP files are corrupted, please re-extract the maps with the tools compiled with this module.");
                        return false;
                    case LoadResult.DisabledInConfig:
                        Log.outError(LogFilter.Maps, $"VMap file '{Global.WorldMgr.GetDataPath() + "/vmaps/" + name}' couldn't be loaded");
                        Log.outError(LogFilter.Maps, "This is because VMAP is disabled in config file.");
                        return false;
                }
            }

            return true;
        }

        public bool HasChildTerrainGridFile(uint mapId, int gx, int gy)
        {
            var childMap = _childTerrain.Find(childTerrain => childTerrain.GetId() == mapId);
            return childMap != null && childMap._gridFileExists[GetBitsetIndex(gx, gy)];
        }

        public void AddChildTerrain(TerrainInfo childTerrain)
        {
            childTerrain._parentTerrain = this;
            _childTerrain.Add(childTerrain);
        }

        public void LoadMapAndVMap(int gx, int gy)
        {
            if (++_referenceCountFromMap[gx][gy] != 1)    // check if already loaded
                return;

            lock(_loadLock)
                LoadMapAndVMapImpl(gx, gy);
        }

        public void LoadMapAndVMapImpl(int gx, int gy)
        {
            LoadMap(gx, gy);
            LoadVMap(gx, gy);
            LoadMMap(gx, gy);

            foreach (TerrainInfo childTerrain in _childTerrain)
                childTerrain.LoadMapAndVMapImpl(gx, gy);

            _loadedGrids[GetBitsetIndex(gx, gy)] = true;
        }

        public void LoadMap(int gx, int gy)
        {
            if (_gridMap[gx][gy] != null)
                return;

            if (!_gridFileExists[GetBitsetIndex(gx, gy)])
                return;

            // map file name
            string fileName = $"{Global.WorldMgr.GetDataPath()}/maps/{GetId():D4}_{gx:D2}_{gy:D2}.map";
            Log.outInfo(LogFilter.Maps, $"Loading map {fileName}");

            // loading data
            GridMap gridMap = new();
            LoadResult gridMapLoadResult = gridMap.LoadData(fileName);
            if (gridMapLoadResult == LoadResult.Success)
                _gridMap[gx][gy] = gridMap;
            else
                _gridFileExists[GetBitsetIndex(gx, gy)] = false;

            if (gridMapLoadResult == LoadResult.ReadFromFileFailed)
                Log.outError(LogFilter.Maps, $"Error loading map file: {fileName}");
        }

        public void LoadVMap(int gx, int gy)
        {
            if (!Global.VMapMgr.IsMapLoadingEnabled())
                return;

            // x and y are swapped !!
            LoadResult vmapLoadResult = Global.VMapMgr.LoadMap(GetId(), gx, gy);
            switch (vmapLoadResult)
            {
                case LoadResult.Success:
                    Log.outDebug(LogFilter.Maps, $"VMAP loaded name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (vmap rep.: x:{gx}, y:{gy})");
                    break;
                case LoadResult.VersionMismatch:
                case LoadResult.ReadFromFileFailed:
                    Log.outError(LogFilter.Maps, $"Could not load VMAP name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (vmap rep.: x:{gx}, y:{gy})");
                    break;
                case LoadResult.DisabledInConfig:
                    Log.outDebug(LogFilter.Maps, $"Ignored VMAP name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (vmap rep.: x:{gx}, y:{gy})");
                    break;
            }
        }

        public void LoadMMap(int gx, int gy)
        {
            if (!Global.DisableMgr.IsPathfindingEnabled(GetId()))
                return;

            bool mmapLoadResult = Global.MMapMgr.LoadMap(Global.WorldMgr.GetDataPath(), GetId(), gx, gy);
            if (mmapLoadResult)
                Log.outDebug(LogFilter.Maps, $"MMAP loaded name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (mmap rep.: x:{gx}, y:{gy})");
            else
                Log.outWarn(LogFilter.Maps, $"Could not load MMAP name:{GetMapName()}, id:{GetId()}, x:{gx}, y:{gy} (mmap rep.: x:{gx}, y:{gy})");
        }

        public void UnloadMap(int gx, int gy)
        {
            --_referenceCountFromMap[gx][gy];
            // unload later
        }

        public void UnloadMapImpl(int gx, int gy)
        {
            _gridMap[gx][gy] = null;
            Global.VMapMgr.UnloadMap(GetId(), gx, gy);
            Global.MMapMgr.UnloadMap(GetId(), gx, gy);

            foreach (var childTerrain in _childTerrain)
                childTerrain.UnloadMapImpl(gx, gy);

            _loadedGrids[GetBitsetIndex(gx, gy)] = false;
        }

        public GridMap GetGrid(uint mapId, float x, float y, bool loadIfMissing = true)
        {
            // half opt method
            int gx = (int)(MapConst.CenterGridId - x / MapConst.SizeofGrids);                   //grid x
            int gy = (int)(MapConst.CenterGridId - y / MapConst.SizeofGrids);                   //grid y

            // ensure GridMap is loaded
            if (!_loadedGrids[GetBitsetIndex(gx, gy)] && loadIfMissing)
            {
                lock(_loadLock)
                    LoadMapAndVMapImpl(gx, gy);
            }

            GridMap grid = _gridMap[gx][gy];
            if (mapId != GetId())
            {
                var childMap = _childTerrain.Find(childTerrain => childTerrain.GetId() == mapId);
                if (childMap != null && childMap._gridMap[gx][gy] != null)
                    grid = childMap.GetGrid(mapId, x, y, false);
            }

            return grid;
        }

        public void CleanUpGrids(uint diff)
        {
            _cleanupTimer.Update(diff);
            if (!_cleanupTimer.Passed())
                return;

            // delete those GridMap objects which have refcount = 0
            for (int x = 0; x < MapConst.MaxGrids; ++x)
                for (int y = 0; y < MapConst.MaxGrids; ++y)
                    if (_loadedGrids[GetBitsetIndex(x, y)] && _referenceCountFromMap[x][y] == 0)
                        UnloadMapImpl(x, y);

            _cleanupTimer.Reset(CleanupInterval);
        }

        public static bool IsInWMOInterior(uint mogpFlags)
        {
            return (mogpFlags & 0x2000) != 0;
        }

        public void GetFullTerrainStatusForPosition(PhaseShift phaseShift, float x, float y, float z, PositionFullTerrainStatus data, LiquidHeaderTypeFlags reqLiquidType = LiquidHeaderTypeFlags.AllLiquids, float collisionHeight = MapConst.DefaultCollesionHeight, DynamicMapTree dynamicMapTree = null)
        {
            AreaAndLiquidData dynData = null;
            AreaAndLiquidData wmoData = null;

            uint terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, this, x, y);
            GridMap gmap = GetGrid(terrainMapId, x, y);
            AreaAndLiquidData vmapData = Global.VMapMgr.GetAreaAndLiquidData(terrainMapId, x, y, z, (byte)reqLiquidType);
            if (dynamicMapTree != null)
                dynData = dynamicMapTree.GetAreaAndLiquidData(x, y, z, phaseShift, (byte)reqLiquidType);

            uint gridAreaId = 0;
            float gridMapHeight = MapConst.InvalidHeight;
            if (gmap != null)
            {
                gridAreaId = gmap.GetArea(x, y);
                gridMapHeight = gmap.GetHeight(x, y);
            }

            bool useGridLiquid = true;

            // floor is the height we are closer to (but only if above)
            data.FloorZ = MapConst.InvalidHeight;
            if (gridMapHeight > MapConst.InvalidHeight && MathFunctions.fuzzyGe(z, gridMapHeight - MapConst.GroundHeightTolerance))
                data.FloorZ = gridMapHeight;
            if (vmapData.floorZ > MapConst.InvalidHeight &&
                MathFunctions.fuzzyGe(z, vmapData.floorZ - MapConst.GroundHeightTolerance) &&
                (MathFunctions.fuzzyLt(z, gridMapHeight - MapConst.GroundHeightTolerance) || vmapData.floorZ > gridMapHeight))
            {
                data.FloorZ = vmapData.floorZ;
                wmoData = vmapData;
            }

            // NOTE: Objects will not detect a case when a wmo providing area/liquid despawns from under them
            // but this is fine as these kind of objects are not meant to be spawned and despawned a lot
            // example: Lich King platform
            if (dynData.floorZ > MapConst.InvalidHeight &&
                MathFunctions.fuzzyGe(z, dynData.floorZ - MapConst.GroundHeightTolerance) &&
                (MathFunctions.fuzzyLt(z, gridMapHeight - MapConst.GroundHeightTolerance) || dynData.floorZ > gridMapHeight) &&
                (MathFunctions.fuzzyLt(z, vmapData.floorZ - MapConst.GroundHeightTolerance) || dynData.floorZ > vmapData.floorZ))
            {
                data.FloorZ = dynData.floorZ;
                wmoData = dynData;
            }

            if (wmoData != null)
            {
                if (wmoData.areaInfo.HasValue)
                {
                    data.areaInfo = new(wmoData.areaInfo.Value.AdtId, wmoData.areaInfo.Value.RootId, wmoData.areaInfo.Value.GroupId, wmoData.areaInfo.Value.MogpFlags);
                    // wmo found
                    var wmoEntry = Global.DB2Mgr.GetWMOAreaTable(wmoData.areaInfo.Value.RootId, wmoData.areaInfo.Value.AdtId, wmoData.areaInfo.Value.GroupId);
                    if (wmoEntry == null)
                        wmoEntry = Global.DB2Mgr.GetWMOAreaTable(wmoData.areaInfo.Value.RootId, wmoData.areaInfo.Value.AdtId, -1);

                    data.outdoors = (wmoData.areaInfo.Value.MogpFlags & 0x8) != 0;
                    if (wmoEntry != null)
                    {
                        data.AreaId = wmoEntry.AreaTableID;
                        if ((wmoEntry.Flags & 4) != 0)
                            data.outdoors = true;
                        else if ((wmoEntry.Flags & 2) != 0)
                            data.outdoors = false;
                    }

                    if (data.AreaId == 0)
                        data.AreaId = gridAreaId;

                    useGridLiquid = !IsInWMOInterior(wmoData.areaInfo.Value.MogpFlags);
                }
            }
            else
            {
                data.outdoors = true;
                data.AreaId = gridAreaId;
                var areaEntry1 = CliDB.AreaTableStorage.LookupByKey(data.AreaId);
                if (areaEntry1 != null)
                    data.outdoors = ((AreaFlags)areaEntry1.Flags[0] & (AreaFlags.Inside | AreaFlags.Outside)) != AreaFlags.Inside;
            }

            if (data.AreaId == 0)
                data.AreaId = CliDB.MapStorage.LookupByKey(GetId()).AreaTableID;

            var areaEntry = CliDB.AreaTableStorage.LookupByKey(data.AreaId);

            // liquid processing
            data.LiquidStatus = ZLiquidStatus.NoWater;
            if (wmoData != null && wmoData.liquidInfo.HasValue && wmoData.liquidInfo.Value.Level > wmoData.floorZ)
            {
                uint liquidType = wmoData.liquidInfo.Value.LiquidType;
                if (GetId() == 530 && liquidType == 2) // gotta love hacks
                    liquidType = 15;

                uint liquidFlagType = 0;
                var liquidData = CliDB.LiquidTypeStorage.LookupByKey(liquidType);
                if (liquidData != null)
                    liquidFlagType = liquidData.SoundBank;

                if (liquidType != 0 && liquidType < 21 && areaEntry != null)
                {
                    uint overrideLiquid = areaEntry.LiquidTypeID[liquidFlagType];
                    if (overrideLiquid == 0 && areaEntry.ParentAreaID != 0)
                    {
                        var zoneEntry = CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID);
                        if (zoneEntry != null)
                            overrideLiquid = zoneEntry.LiquidTypeID[liquidFlagType];
                    }

                    var overrideData = CliDB.LiquidTypeStorage.LookupByKey(overrideLiquid);
                    if (overrideData != null)
                    {
                        liquidType = overrideLiquid;
                        liquidFlagType = overrideData.SoundBank;
                    }
                }

                data.LiquidInfo = new();
                data.LiquidInfo.level = wmoData.liquidInfo.Value.Level;
                data.LiquidInfo.depth_level = wmoData.floorZ;
                data.LiquidInfo.entry = liquidType;
                data.LiquidInfo.type_flags = (LiquidHeaderTypeFlags)(1 << (int)liquidFlagType);

                float delta = wmoData.liquidInfo.Value.Level - z;
                if (delta > collisionHeight)
                    data.LiquidStatus = ZLiquidStatus.UnderWater;
                else if (delta > 0.0f)
                    data.LiquidStatus = ZLiquidStatus.InWater;
                else if (delta > -0.1f)
                    data.LiquidStatus = ZLiquidStatus.WaterWalk;
                else
                    data.LiquidStatus = ZLiquidStatus.AboveWater;
            }
            // look up liquid data from grid map
            if (gmap != null && useGridLiquid)
            {
                LiquidData gridMapLiquid = new();
                ZLiquidStatus gridMapStatus = gmap.GetLiquidStatus(x, y, z, reqLiquidType, gridMapLiquid, collisionHeight);
                if (gridMapStatus != ZLiquidStatus.NoWater && (wmoData == null || gridMapLiquid.level > wmoData.floorZ))
                {
                    if (GetId() == 530 && gridMapLiquid.entry == 2)
                        gridMapLiquid.entry = 15;
                    data.LiquidInfo = gridMapLiquid;
                    data.LiquidStatus = gridMapStatus;
                }
            }
        }

        public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags ReqLiquidType, LiquidData data = null, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            ZLiquidStatus result = ZLiquidStatus.NoWater;
            float liquid_level = MapConst.InvalidHeight;
            float ground_level = MapConst.InvalidHeight;
            uint liquid_type = 0;
            uint mogpFlags = 0;
            bool useGridLiquid = true;
            uint terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, this, x, y);

            if (Global.VMapMgr.GetLiquidLevel(terrainMapId, x, y, z, (byte)ReqLiquidType, ref liquid_level, ref ground_level, ref liquid_type, ref mogpFlags))
            {
                useGridLiquid = !IsInWMOInterior(mogpFlags);
                Log.outDebug(LogFilter.Maps, $"GetLiquidStatus(): vmap liquid level: {liquid_level} ground: {ground_level} type: {liquid_type}");
                // Check water level and ground level
                if (liquid_level > ground_level && MathFunctions.fuzzyGe(z, ground_level - MapConst.GroundHeightTolerance))
                {
                    // All ok in water . store data
                    if (data != null)
                    {
                        // hardcoded in client like this
                        if (GetId() == 530 && liquid_type == 2)
                            liquid_type = 15;

                        uint liquidFlagType = 0;
                        var liq = CliDB.LiquidTypeStorage.LookupByKey(liquid_type);
                        if (liq != null)
                            liquidFlagType = liq.SoundBank;

                        if (liquid_type != 0 && liquid_type < 21)
                        {
                            var area = CliDB.AreaTableStorage.LookupByKey(GetAreaId(phaseShift, x, y, z));
                            if (area != null)
                            {
                                uint overrideLiquid = area.LiquidTypeID[liquidFlagType];
                                if (overrideLiquid == 0 && area.ParentAreaID != 0)
                                {
                                    area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
                                    if (area != null)
                                        overrideLiquid = area.LiquidTypeID[liquidFlagType];
                                }

                                var liq1 = CliDB.LiquidTypeStorage.LookupByKey(overrideLiquid);
                                if (liq1 != null)
                                {
                                    liquid_type = overrideLiquid;
                                    liquidFlagType = liq1.SoundBank;
                                }
                            }
                        }

                        data.level = liquid_level;
                        data.depth_level = ground_level;

                        data.entry = liquid_type;
                        data.type_flags = (LiquidHeaderTypeFlags)(1 << (int)liquidFlagType);
                    }

                    float delta = liquid_level - z;

                    // Get position delta
                    if (delta > collisionHeight)                   // Under water
                        return ZLiquidStatus.UnderWater;
                    if (delta > 0.0f)                   // In water
                        return ZLiquidStatus.InWater;
                    if (delta > -0.1f)                   // Walk on water
                        return ZLiquidStatus.WaterWalk;
                    result = ZLiquidStatus.AboveWater;
                }
            }

            if (useGridLiquid)
            {
                GridMap gmap = GetGrid(terrainMapId, x, y);
                if (gmap != null)
                {
                    LiquidData map_data = new();
                    ZLiquidStatus map_result = gmap.GetLiquidStatus(x, y, z, ReqLiquidType, map_data, collisionHeight);
                    // Not override LIQUID_MAP_ABOVE_WATER with LIQUID_MAP_NO_WATER:
                    if (map_result != ZLiquidStatus.NoWater && (map_data.level > ground_level))
                    {
                        if (data != null)
                        {
                            // hardcoded in client like this
                            if (GetId() == 530 && map_data.entry == 2)
                                map_data.entry = 15;

                            data = map_data;
                        }
                        return map_result;
                    }
                }
            }
            return result;
        }

        public bool GetAreaInfo(PhaseShift phaseShift, float x, float y, float z, out uint mogpflags, out int adtId, out int rootId, out int groupId, DynamicMapTree dynamicMapTree = null)
        {
            mogpflags = 0;
            adtId = 0;
            rootId = 0;
            groupId = 0;

            float vmap_z = z;
            float dynamic_z = z;
            float check_z = z;
            uint terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, this, x, y);

            uint vflags;
            int vadtId;
            int vrootId;
            int vgroupId;
            uint dflags = 0;
            int dadtId = 0;
            int drootId = 0;
            int dgroupId = 0;

            bool hasVmapAreaInfo = Global.VMapMgr.GetAreaInfo(terrainMapId, x, y, ref vmap_z, out vflags, out vadtId, out vrootId, out vgroupId);
            bool hasDynamicAreaInfo = dynamicMapTree != null ? dynamicMapTree.GetAreaInfo(x, y, ref dynamic_z, phaseShift, out dflags, out dadtId, out drootId, out dgroupId) : false;

            if (hasVmapAreaInfo)
            {
                if (hasDynamicAreaInfo && dynamic_z > vmap_z)
                {
                    check_z = dynamic_z;
                    mogpflags = dflags;
                    adtId = dadtId;
                    rootId = drootId;
                    groupId = dgroupId;
                }
                else
                {
                    check_z = vmap_z;
                    mogpflags = vflags;
                    adtId = vadtId;
                    rootId = vrootId;
                    groupId = vgroupId;
                }
            }
            else if (hasDynamicAreaInfo)
            {
                check_z = dynamic_z;
                mogpflags = dflags;
                adtId = dadtId;
                rootId = drootId;
                groupId = dgroupId;
            }

            if (hasVmapAreaInfo || hasDynamicAreaInfo)
            {
                // check if there's terrain between player height and object height
                GridMap gmap = GetGrid(terrainMapId, x, y);
                if (gmap != null)
                {
                    float mapHeight = gmap.GetHeight(x, y);
                    // z + 2.0f condition taken from GetHeight(), not sure if it's such a great choice...
                    if (z + 2.0f > mapHeight && mapHeight > check_z)
                        return false;
                }
                return true;
            }
            return false;
        }

        public uint GetAreaId(PhaseShift phaseShift, Position pos, DynamicMapTree dynamicMapTree = null) { return GetAreaId(phaseShift, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), dynamicMapTree); }
        
        public uint GetAreaId(PhaseShift phaseShift, float x, float y, float z, DynamicMapTree dynamicMapTree = null)
        {
            uint mogpFlags;
            int adtId, rootId, groupId;
            float vmapZ = z;
            bool hasVmapArea = GetAreaInfo(phaseShift, x, y, vmapZ, out mogpFlags, out adtId, out rootId, out groupId, dynamicMapTree);

            uint gridAreaId = 0;
            float gridMapHeight = MapConst.InvalidHeight;
            GridMap gmap = GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y);
            if (gmap != null)
            {
                gridAreaId = gmap.GetArea(x, y);
                gridMapHeight = gmap.GetHeight(x, y);
            }

            uint areaId = 0;

            // floor is the height we are closer to (but only if above)
            if (hasVmapArea && MathFunctions.fuzzyGe(z, vmapZ - MapConst.GroundHeightTolerance) && (MathFunctions.fuzzyLt(z, gridMapHeight - MapConst.GroundHeightTolerance) || vmapZ > gridMapHeight))
            {
                // wmo found
                var wmoEntry = Global.DB2Mgr.GetWMOAreaTable(rootId, adtId, groupId);
                if (wmoEntry != null)
                    areaId = wmoEntry.AreaTableID;

                if (areaId == 0)
                    areaId = gridAreaId;
            }
            else
                areaId = gridAreaId;

            if (areaId == 0)
                areaId = CliDB.MapStorage.LookupByKey(GetId()).AreaTableID;

            return areaId;
        }

        public uint GetZoneId(PhaseShift phaseShift, Position pos, DynamicMapTree dynamicMapTree = null) { return GetZoneId(phaseShift, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), dynamicMapTree); }
        
        public uint GetZoneId(PhaseShift phaseShift, float x, float y, float z, DynamicMapTree dynamicMapTree = null)
        {
            uint areaId = GetAreaId(phaseShift, x, y, z, dynamicMapTree);
            var area = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (area != null)
                if (area.ParentAreaID != 0)
                    return area.ParentAreaID;

            return areaId;
        }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, Position pos, DynamicMapTree dynamicMapTree = null) { GetZoneAndAreaId(phaseShift, out zoneid, out areaid, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), dynamicMapTree); }
        
        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, float x, float y, float z, DynamicMapTree dynamicMapTree = null)
        {
            areaid = zoneid = GetAreaId(phaseShift, x, y, z, dynamicMapTree);
            var area = CliDB.AreaTableStorage.LookupByKey(areaid);
            if (area != null)
                if (area.ParentAreaID != 0)
                    zoneid = area.ParentAreaID;
        }

        public float GetMinHeight(PhaseShift phaseShift, float x, float y)
        {
            GridMap grid = GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y);
            if (grid != null)
                return grid.GetMinHeight(x, y);

            return -500.0f;
        }

        public float GetGridHeight(PhaseShift phaseShift, float x, float y)
        {
            GridMap gmap = GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y);
            if (gmap != null)
                return gmap.GetHeight(x, y);

            return MapConst.VMAPInvalidHeightValue;
        }

        public float GetStaticHeight(PhaseShift phaseShift, Position pos, bool checkVMap = true, float maxSearchDist = MapConst.DefaultHeightSearch) { return GetStaticHeight(phaseShift, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), checkVMap, maxSearchDist); }

        public float GetStaticHeight(PhaseShift phaseShift, float x, float y, float z, bool checkVMap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
        {
            // find raw .map surface under Z coordinates
            float mapHeight = MapConst.VMAPInvalidHeightValue;
            uint terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, this, x, y);

            float gridHeight = GetGridHeight(phaseShift, x, y);
            if (MathFunctions.fuzzyGe(z, gridHeight - MapConst.GroundHeightTolerance))
                mapHeight = gridHeight;

            float vmapHeight = MapConst.VMAPInvalidHeightValue;
            if (checkVMap)
            {
                if (Global.VMapMgr.IsHeightCalcEnabled())
                    vmapHeight = Global.VMapMgr.GetHeight(terrainMapId, x, y, z, maxSearchDist);
            }

            // mapHeight set for any above raw ground Z or <= INVALID_HEIGHT
            // vmapheight set for any under Z value or <= INVALID_HEIGHT
            if (vmapHeight > MapConst.InvalidHeight)
            {
                if (mapHeight > MapConst.InvalidHeight)
                {
                    // we have mapheight and vmapheight and must select more appropriate

                    // vmap height above map height
                    // or if the distance of the vmap height is less the land height distance
                    if (vmapHeight > mapHeight || Math.Abs(mapHeight - z) > Math.Abs(vmapHeight - z))
                        return vmapHeight;

                    return mapHeight;                           // better use .map surface height
                }

                return vmapHeight;                              // we have only vmapHeight (if have)
            }

            return mapHeight;                               // explicitly use map data
        }

        public float GetWaterLevel(PhaseShift phaseShift, float x, float y)
        {
            GridMap gmap = GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y);
            if (gmap != null)
                return gmap.GetLiquidLevel(x, y);
            else
                return 0;
        }

        public bool IsInWater(PhaseShift phaseShift, float x, float y, float pZ, LiquidData data = null)
        {
            LiquidData liquid_status = new();
            LiquidData liquid_ptr = data != null ? data : liquid_status;
            return (GetLiquidStatus(phaseShift, x, y, pZ, LiquidHeaderTypeFlags.AllLiquids, liquid_ptr) & (ZLiquidStatus.InWater | ZLiquidStatus.UnderWater)) != 0;
        }

        public bool IsUnderWater(PhaseShift phaseShift, float x, float y, float z)
        {
            return (GetLiquidStatus(phaseShift, x, y, z, LiquidHeaderTypeFlags.Water | LiquidHeaderTypeFlags.Ocean) & ZLiquidStatus.UnderWater) != 0;
        }

        public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, ref float ground, bool swim = false, float collisionHeight = MapConst.DefaultCollesionHeight, DynamicMapTree dynamicMapTree = null)
        {
            if (GetGrid(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y) != null)
            {
                // we need ground level (including grid height version) for proper return water level in point
                float ground_z = GetStaticHeight(phaseShift, x, y, z + MapConst.ZOffsetFindHeight, true, 50.0f);
                if (dynamicMapTree != null)
                    ground_z = Math.Max(ground_z, dynamicMapTree.GetHeight(x, y, z + MapConst.ZOffsetFindHeight, 50.0f, phaseShift));

                ground = ground_z;

                LiquidData liquid_status = new();
                ZLiquidStatus res = GetLiquidStatus(phaseShift, x, y, ground_z, LiquidHeaderTypeFlags.AllLiquids, liquid_status, collisionHeight);
                switch (res)
                {
                    case ZLiquidStatus.AboveWater:
                        return Math.Max(liquid_status.level, ground_z);
                    case ZLiquidStatus.NoWater:
                        return ground_z;
                    default:
                        return liquid_status.level;
                }
            }

            return MapConst.VMAPInvalidHeightValue;
        }

        public uint GetId() { return _mapId; }

        static int GetBitsetIndex(int gx, int gy) { return gx * MapConst.MaxGrids + gy; }
    }
}
