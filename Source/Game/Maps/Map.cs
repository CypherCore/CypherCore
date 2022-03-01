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
using Framework.Database;
using Framework.Dynamic;
using Game.BattleGrounds;
using Game.Collision;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scenarios;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Game.Maps
{
    public class Map : IDisposable
    {
        public Map(uint id, long expiry, uint instanceId, Difficulty spawnmode, Map parent = null)
        {
            i_mapRecord = CliDB.MapStorage.LookupByKey(id);
            i_spawnMode = spawnmode;
            i_InstanceId = instanceId;
            m_VisibleDistance = SharedConst.DefaultVisibilityDistance;
            m_VisibilityNotifyPeriod = SharedConst.DefaultVisibilityNotifyPeriod;
            i_gridExpiry = expiry;

            if (parent)
            {
                m_parentMap = parent;
                m_parentTerrainMap = m_parentMap.m_parentTerrainMap;
                m_childTerrainMaps = m_parentMap.m_childTerrainMaps;
            }
            else
            {
                m_parentMap = this;
                m_parentTerrainMap = this;
                m_childTerrainMaps = new List<Map>();
            }

            for (uint x = 0; x < MapConst.MaxGrids; ++x)
            {
                i_grids[x] = new Grid[MapConst.MaxGrids];
                GridMaps[x] = new GridMap[MapConst.MaxGrids];
                GridMapReference[x] = new ushort[MapConst.MaxGrids];
                for (uint y = 0; y < MapConst.MaxGrids; ++y)
                {
                    //z code
                    GridMaps[x][y] = null;
                    SetGrid(null, x, y);
                }
            }

            _zonePlayerCountMap.Clear();

            //lets initialize visibility distance for map
            InitVisibilityDistance();
            _weatherUpdateTimer = new IntervalTimer();
            _weatherUpdateTimer.SetInterval(1 * Time.InMilliseconds);

            GetGuidSequenceGenerator(HighGuid.Transport).Set(Global.ObjectMgr.GetGenerator(HighGuid.Transport).GetNextAfterMaxUsed());

            Global.MMapMgr.LoadMapInstance(Global.WorldMgr.GetDataPath(), GetId(), i_InstanceId);

            Global.ScriptMgr.OnCreateMap(this);
        }

        public void Dispose()
        {
            Global.ScriptMgr.OnDestroyMap(this);

            // Delete all waiting spawns
            // This doesn't delete from database.
            UnloadAllRespawnInfos();

            for (var i = 0; i < i_worldObjects.Count; ++i)
            {
                WorldObject obj = i_worldObjects[i];
                Cypher.Assert(obj.IsWorldObject());
                obj.RemoveFromWorld();
                obj.ResetMap();
            }

            if (!m_scriptSchedule.Empty())
                Global.MapMgr.DecreaseScheduledScriptCount((uint)m_scriptSchedule.Count);

            if (m_parentMap == this)
                m_childTerrainMaps = null;

            Global.MMapMgr.UnloadMapInstance(GetId(), i_InstanceId);
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
                    for (uint gx = 0; gx < MapConst.MaxGrids; ++gx)
                        for (uint gy = 0; gy < MapConst.MaxGrids; ++gy)
                            i_gridFileExists[(int)(gx * MapConst.MaxGrids + gy)] = tilesData[(int)(gx * MapConst.MaxGrids + gy)] == 49; // char of 1

                    return;
                }
            }

            for (uint gx = 0; gx < MapConst.MaxGrids; ++gx)
                for (uint gy = 0; gy < MapConst.MaxGrids; ++gy)
                    i_gridFileExists[(int)(gx * MapConst.MaxGrids + gy)] = ExistMap(GetId(), gx, gy);
        }

        Map GetRootParentTerrainMap()
        {
            Map map = this;
            while (map != map.m_parentTerrainMap)
                map = map.m_parentTerrainMap;

            return map;
        }

        public static bool ExistMap(uint mapid, uint gx, uint gy)
        {
            string fileName = $"{Global.WorldMgr.GetDataPath()}/maps/{mapid:D4}_{gx:D2}_{gy:D2}.map";
            if (!File.Exists(fileName))
            {
                Log.outError(LogFilter.Maps, "Map file '{0}': does not exist!", fileName);
                return false;
            }

            using var reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            var header = reader.Read<MapFileHeader>();
            if (header.mapMagic != MapConst.MapMagic || (header.versionMagic != MapConst.MapVersionMagic && header.versionMagic != MapConst.MapVersionMagic2)) // Hack for some different extractors using v2.0 header
            {
                Log.outError(LogFilter.Maps, "Map file '{0}' is from an incompatible map version ({1}), {2} is expected. Please recreate using the mapextractor.",
                    fileName, header.versionMagic, MapConst.MapVersionMagic);
                return false;
            }
            return true;
        }

        public static bool ExistVMap(uint mapid, uint gx, uint gy)
        {
            if (Global.VMapMgr.IsMapLoadingEnabled())
            {
                LoadResult result = Global.VMapMgr.ExistsMap(mapid, gx, gy);
                string name = VMapManager.GetMapFileName(mapid);
                switch (result)
                {
                    case LoadResult.Success:
                        break;
                    case LoadResult.FileNotFound:
                        Log.outError(LogFilter.Maps, $"VMap file '{Global.WorldMgr.GetDataPath() + "vmaps/" + name}' does not exist");
                        Log.outError(LogFilter.Maps, $"Please place VMAP files (*.vmtree and *.vmtile) in the vmap directory ({Global.WorldMgr.GetDataPath() + "vmaps/"}), or correct the DataDir setting in your worldserver.conf file.");
                        return false;
                    case LoadResult.VersionMismatch:
                        Log.outError(LogFilter.Maps, $"VMap file '{Global.WorldMgr.GetDataPath() + "vmaps/" + name}e' couldn't b loaded");
                        Log.outError(LogFilter.Maps, "This is because the version of the VMap file and the version of this module are different, please re-extract the maps with the tools compiled with this module.");
                        return false;
                    case LoadResult.ReadFromFileFailed:
                        Log.outError(LogFilter.Maps, $"VMap file '{Global.WorldMgr.GetDataPath() + "vmaps/" + name}' couldn't be loaded");
                        Log.outError(LogFilter.Maps, "This is because VMAP files are corrupted, please re-extract the maps with the tools compiled with this module.");
                        return false;
                }
            }
            return true;
        }

        void LoadMMap(uint gx, uint gy)
        {
            if (!Global.DisableMgr.IsPathfindingEnabled(GetId()))
                return;

            if (Global.MMapMgr.LoadMap(Global.WorldMgr.GetDataPath(), GetId(), gx, gy))
                Log.outInfo(LogFilter.Maps, "MMAP loaded name:{0}, id:{1}, x:{2}, y:{3} (mmap rep.: x:{4}, y:{5})", GetMapName(), GetId(), gx, gy, gx, gy);
            else
                Log.outWarn(LogFilter.Maps, "Could not load MMAP name:{0}, id:{1}, x:{2}, y:{3} (mmap rep.: x:{4}, y:{5})", GetMapName(), GetId(), gx, gy, gx, gy);
        }

        void LoadVMap(uint gx, uint gy)
        {
            if (!Global.VMapMgr.IsMapLoadingEnabled())
                return;

            // x and y are swapped !!
            VMAPLoadResult vmapLoadResult = Global.VMapMgr.LoadMap(GetId(), gx, gy);
            switch (vmapLoadResult)
            {
                case VMAPLoadResult.OK:
                    Log.outInfo(LogFilter.Maps, "VMAP loaded name:{0}, id:{1}, x:{2}, y:{3} (vmap rep.: x:{4}, y:{5})", GetMapName(), GetId(), gx, gy, gx, gy);
                    break;
                case VMAPLoadResult.Error:
                    Log.outInfo(LogFilter.Maps, "Could not load VMAP name:{0}, id:{1}, x:{2}, y:{3} (vmap rep.: x:{4}, y:{5})", GetMapName(), GetId(), gx, gy, gx, gy);
                    break;
                case VMAPLoadResult.Ignored:
                    Log.outDebug(LogFilter.Maps, "Ignored VMAP name:{0}, id:{1}, x:{2}, y:{3} (vmap rep.: x:{4}, y:{5})", GetMapName(), GetId(), gx, gy, gx, gy);
                    break;
            }
        }

        void LoadMap(uint gx, uint gy)
        {
            LoadMapImpl(this, gx, gy);
            foreach (Map childBaseMap in m_childTerrainMaps)
                childBaseMap.LoadMap(gx, gy);
        }

        void LoadMapImpl(Map map, uint gx, uint gy)
        {
            if (map.GridMaps[gx][gy] != null)
                return;

            // map file name
            string fileName = $"{Global.WorldMgr.GetDataPath()}/maps/{map.GetId():D4}_{gx:D2}_{gy:D2}.map";
            Log.outInfo(LogFilter.Maps, "Loading map {0}", fileName);

            // loading data
            GridMap gridMap = new();
            LoadResult gridMapLoadResult = gridMap.LoadData(fileName);
            if (gridMapLoadResult == LoadResult.Success)
                map.GridMaps[gx][gy] = gridMap;
            else
            {
                map.i_gridFileExists[(int)(gx * MapConst.MaxGrids + gy)] = false;
                Map parentTerrain = map;
                while (parentTerrain != parentTerrain.m_parentTerrainMap)
                {
                    map.GridMaps[gx][gy] = parentTerrain.m_parentTerrainMap.GridMaps[gx][gy];
                    if (map.GridMaps[gx][gy] != null)
                        break;

                    parentTerrain = parentTerrain.m_parentTerrainMap;
                }
            }

            if (map.GridMaps[gx][gy] != null)
                Global.ScriptMgr.OnLoadGridMap(map, map.GridMaps[gx][gy], gx, gy);
            else if (gridMapLoadResult == LoadResult.ReadFromFileFailed)
                Log.outError(LogFilter.Maps, $"Error loading map file: {fileName}");
        }

        void UnloadMap(uint gx, uint gy)
        {
            foreach (Map childBaseMap in m_childTerrainMaps)
                childBaseMap.UnloadMap(gx, gy);

            UnloadMapImpl(this, gx, gy);
        }

        void UnloadMapImpl(Map map, uint gx, uint gy)
        {
            map.GridMaps[gx][gy] = null;
        }

        void LoadMapAndVMap(uint gx, uint gy)
        {
            LoadMap(gx, gy);
            // Only load the data for the base map
            if (this == m_parentMap)
            {
                LoadVMap(gx, gy);
                LoadMMap(gx, gy);
            }
        }

        public void LoadAllCells()
        {
            for (uint cellX = 0; cellX < MapConst.TotalCellsPerMap; cellX++)
                for (uint cellY = 0; cellY < MapConst.TotalCellsPerMap; cellY++)
                    LoadGrid((cellX + 0.5f - MapConst.CenterGridCellId) * MapConst.SizeofCells, (cellY + 0.5f - MapConst.CenterGridCellId) * MapConst.SizeofCells);
        }

        public virtual void InitVisibilityDistance()
        {
            //init visibility for continents
            m_VisibleDistance = Global.WorldMgr.GetMaxVisibleDistanceOnContinents();
            m_VisibilityNotifyPeriod = Global.WorldMgr.GetVisibilityNotifyPeriodOnContinents();
        }

        public void AddToGrid<T>(T obj, Cell cell) where T : WorldObject
        {
            Grid grid = GetGrid(cell.GetGridX(), cell.GetGridY());
            switch (obj.GetTypeId())
            {
                case TypeId.Corpse:
                    if (grid.IsGridObjectDataLoaded())
                    {
                        // Corpses are a special object type - they can be added to grid via a call to AddToMap
                        // or loaded through ObjectGridLoader.
                        // Both corpses loaded from database and these freshly generated by Player::CreateCoprse are added to _corpsesByCell
                        // ObjectGridLoader loads all corpses from _corpsesByCell even if they were already added to grid before it was loaded
                        // so we need to explicitly check it here (Map::AddToGrid is only called from Player::BuildPlayerRepop, not from ObjectGridLoader)
                        // to avoid failing an assertion in GridObject::AddToGrid
                        if (obj.IsWorldObject())
                        {
                            obj.SetCurrentCell(cell);
                            grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddWorldObject(obj);
                        }
                        else
                            grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);
                    }
                    return;
                case TypeId.GameObject:
                case TypeId.AreaTrigger:
                    grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);
                    break;
                case TypeId.DynamicObject:
                default:
                    if (obj.IsWorldObject())
                        grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddWorldObject(obj);
                    else
                        grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).AddGridObject(obj);
                    break;
            }

            obj.SetCurrentCell(cell);
        }

        public void RemoveFromGrid(WorldObject obj, Cell cell)
        {
            if (cell == null)
                return;

            Grid grid = GetGrid(cell.GetGridX(), cell.GetGridY());
            if (grid == null)
                return;

            if (obj.IsWorldObject())
                grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).RemoveWorldObject(obj);
            else
                grid.GetGridCell(cell.GetCellX(), cell.GetCellY()).RemoveGridObject(obj);

            obj.SetCurrentCell(null);
        }

        void SwitchGridContainers(WorldObject obj, bool on)
        {
            if (obj.IsPermanentWorldObject())
                return;

            CellCoord p = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
            if (!p.IsCoordValid())
            {
                Log.outError(LogFilter.Maps, "Map.SwitchGridContainers: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]",
                    obj.GetGUID(), obj.GetPositionX(), obj.GetPositionY(), p.X_coord, p.Y_coord);
                return;
            }

            var cell = new Cell(p);
            if (!IsGridLoaded(new GridCoord(cell.GetGridX(), cell.GetGridY())))
                return;

            Log.outDebug(LogFilter.Maps, "Switch object {0} from grid[{1}, {2}] {3}", obj.GetGUID(), cell.GetGridX(), cell.GetGridY(), on);
            Grid ngrid = GetGrid(cell.GetGridX(), cell.GetGridY());
            Cypher.Assert(ngrid != null);

            RemoveFromGrid(obj, cell);

            GridCell gridCell = ngrid.GetGridCell(cell.GetCellX(), cell.GetCellY());
            if (on)
            {
                gridCell.AddWorldObject(obj);
                AddWorldObject(obj);
            }
            else
            {
                gridCell.AddGridObject(obj);
                RemoveWorldObject(obj);
            }

            obj.SetCurrentCell(cell);
            obj.ToCreature().m_isTempWorldObject = on;
        }

        void DeleteFromWorld(Player player)
        {
            Global.ObjAccessor.RemoveObject(player);
            RemoveUpdateObject(player); // @todo I do not know why we need this, it should be removed in ~Object anyway
            player.Dispose();
        }

        void DeleteFromWorld(Transport transport)
        {
            Global.ObjAccessor.RemoveObject(transport);
            transport.Dispose();
        }

        void DeleteFromWorld(WorldObject obj)
        {
            obj.Dispose();
        }

        void EnsureGridCreated(GridCoord p)
        {
            lock (_gridLock)
            {
                EnsureGridCreated_i(p);
            }
        }

        void EnsureGridCreated_i(GridCoord p)
        {
            if (GetGrid(p.X_coord, p.Y_coord) == null)
            {
                Log.outDebug(LogFilter.Maps, "Creating grid[{0}, {1}] for map {2} instance {3}", p.X_coord, p.Y_coord,
                    GetId(), i_InstanceId);

                var grid = new Grid(p.X_coord * MapConst.MaxGrids + p.Y_coord, p.X_coord, p.Y_coord, i_gridExpiry, WorldConfig.GetBoolValue(WorldCfg.GridUnload));
                grid.SetGridState(GridState.Idle);
                SetGrid(grid, p.X_coord, p.Y_coord);

                //z coord
                uint gx = (MapConst.MaxGrids - 1) - p.X_coord;
                uint gy = (MapConst.MaxGrids - 1) - p.Y_coord;

                if (GridMaps[gx][gy] == null)
                {
                    Map rootParentTerrainMap = m_parentMap.GetRootParentTerrainMap();

                    if (m_parentMap.GridMaps[gx][gy] == null)
                        rootParentTerrainMap.LoadMapAndVMap(gx, gy);

                    if (m_parentMap.GridMaps[gx][gy] != null)
                    {
                        GridMaps[gx][gy] = m_parentMap.GridMaps[gx][gy];
                        ++rootParentTerrainMap.GridMapReference[gx][gy];
                    }
                }
            }
        }

        void EnsureGridLoadedForActiveObject(Cell cell, WorldObject obj)
        {
            EnsureGridLoaded(cell);
            Grid grid = GetGrid(cell.GetGridX(), cell.GetGridY());

            if (obj.IsPlayer())
                GetMultiPersonalPhaseTracker().LoadGrid(obj.GetPhaseShift(), grid, this, cell);

            // refresh grid state & timer
            if (grid.GetGridState() != GridState.Active)
            {
                Log.outDebug(LogFilter.Maps, "Active object {0} triggers loading of grid [{1}, {2}] on map {3}",
                    obj.GetGUID(), cell.GetGridX(), cell.GetGridY(), GetId());
                ResetGridExpiry(grid, 0.1f);
                grid.SetGridState(GridState.Active);
            }
        }

        private bool EnsureGridLoaded(Cell cell)
        {
            EnsureGridCreated(new GridCoord(cell.GetGridX(), cell.GetGridY()));
            Grid grid = GetGrid(cell.GetGridX(), cell.GetGridY());

            if (!IsGridObjectDataLoaded(cell.GetGridX(), cell.GetGridY()))
            {
                Log.outDebug(LogFilter.Maps, "Loading grid[{0}, {1}] for map {2} instance {3}", cell.GetGridX(),
                    cell.GetGridY(), GetId(), i_InstanceId);

                SetGridObjectDataLoaded(true, cell.GetGridX(), cell.GetGridY());

                LoadGridObjects(grid, cell);

                Balance();
                return true;
            }

            return false;
        }

        public virtual void LoadGridObjects(Grid grid, Cell cell)
        {
            ObjectGridLoader loader = new(grid, this, cell);
            loader.LoadN();
        }

        void GridMarkNoUnload(uint x, uint y)
        {
            // First make sure this grid is loaded
            float gX = (((float)x - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
            float gY = (((float)y - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
            Cell cell = new(gX, gY);
            EnsureGridLoaded(cell);

            // Mark as don't unload
            var grid = GetGrid(x, y);
            grid.SetUnloadExplicitLock(true);
        }

        void GridUnmarkNoUnload(uint x, uint y)
        {
            // If grid is loaded, clear unload lock
            if (IsGridLoaded(new GridCoord(x, y)))
            {
                var grid = GetGrid(x, y);
                grid.SetUnloadExplicitLock(false);
            }
        }

        public void LoadGrid(float x, float y)
        {
            EnsureGridLoaded(new Cell(x, y));
        }

        public void LoadGridForActiveObject(float x, float y, WorldObject obj)
        {
            EnsureGridLoadedForActiveObject(new Cell(x, y), obj);
        }
        
        public virtual bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            CellCoord cellCoord = GridDefines.ComputeCellCoord(player.GetPositionX(), player.GetPositionY());
            if (!cellCoord.IsCoordValid())
            {
                Log.outError(LogFilter.Maps, "Map.AddPlayer (GUID: {0}) has invalid coordinates X:{1} Y:{2}",
                    player.GetGUID().ToString(), player.GetPositionX(), player.GetPositionY());
                return false;
            }
            var cell = new Cell(cellCoord);
            EnsureGridLoadedForActiveObject(cell, player);
            AddToGrid(player, cell);

            Cypher.Assert(player.GetMap() == this);
            player.SetMap(this);
            player.AddToWorld();

            if (initPlayer)
                SendInitSelf(player);

            SendInitTransports(player);

            if (initPlayer)
                player.m_clientGUIDs.Clear();

            player.UpdateObjectVisibility(false);
            PhasingHandler.SendToPlayer(player);

            if (player.IsAlive())
                ConvertCorpseToBones(player.GetGUID());

            m_activePlayers.Add(player);

            Global.ScriptMgr.OnPlayerEnterMap(this, player);
            return true;
        }

        public void UpdatePersonalPhasesForPlayer(Player player)
        {
            Cell cell = new(player.GetPositionX(), player.GetPositionY());
            GetMultiPersonalPhaseTracker().OnOwnerPhaseChanged(player, GetGrid(cell.GetGridX(), cell.GetGridY()), this, cell);
        }
        
        void InitializeObject(WorldObject obj)
        {
            if (!obj.IsTypeId(TypeId.Unit) || !obj.IsTypeId(TypeId.GameObject))
                return;
            obj._moveState = ObjectCellMoveState.None;
        }

        public bool AddToMap(WorldObject obj)
        {
            //TODO: Needs clean up. An object should not be added to map twice.
            if (obj.IsInWorld)
            {
                obj.UpdateObjectVisibility(true);
                return true;
            }

            CellCoord cellCoord = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
            if (!cellCoord.IsCoordValid())
            {
                Log.outError(LogFilter.Maps,
                    "Map.Add: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]", obj.GetGUID(),
                    obj.GetPositionX(), obj.GetPositionY(), cellCoord.X_coord, cellCoord.Y_coord);
                return false; //Should delete object
            }

            var cell = new Cell(cellCoord);
            if (obj.IsActiveObject())
                EnsureGridLoadedForActiveObject(cell, obj);
            else
                EnsureGridCreated(new GridCoord(cell.GetGridX(), cell.GetGridY()));
            AddToGrid(obj, cell);
            Log.outDebug(LogFilter.Maps, "Object {0} enters grid[{1}, {2}]", obj.GetGUID().ToString(), cell.GetGridX(), cell.GetGridY());

            obj.AddToWorld();

            InitializeObject(obj);

            if (obj.IsActiveObject())
                AddToActive(obj);

            //something, such as vehicle, needs to be update immediately
            //also, trigger needs to cast spell, if not update, cannot see visual
            obj.SetIsNewObject(true);
            obj.UpdateObjectVisibilityOnCreate();
            obj.SetIsNewObject(false);
            return true;
        }

        public bool AddToMap(Transport obj)
        {
            //TODO: Needs clean up. An object should not be added to map twice.
            if (obj.IsInWorld)
                return true;

            CellCoord cellCoord = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
            if (!cellCoord.IsCoordValid())
            {
                Log.outError(LogFilter.Maps,
                    "Map.Add: Object {0} has invalid coordinates X:{1} Y:{2} grid cell [{3}:{4}]", obj.GetGUID(),
                    obj.GetPositionX(), obj.GetPositionY(), cellCoord.X_coord, cellCoord.Y_coord);
                return false; //Should delete object
            }

            obj.AddToWorld();
            _transports.Add(obj);

            // Broadcast creation to players
            foreach (Player player in GetPlayers())
            {
                if (player.GetTransport() != obj)
                {
                    var data = new UpdateData(GetId());
                    obj.BuildCreateUpdateBlockForPlayer(data, player);
                    player.m_visibleTransports.Add(obj.GetGUID());
                    UpdateObject packet;
                    data.BuildPacket(out packet);
                    player.SendPacket(packet);
                }
            }

            return true;
        }

        public bool IsGridLoaded(uint gridId) { return IsGridLoaded(new GridCoord(gridId % MapConst.MaxGrids, gridId / MapConst.MaxGrids)); }

        public bool IsGridLoaded(float x, float y) { return IsGridLoaded(GridDefines.ComputeGridCoord(x, y)); }

        public bool IsGridLoaded(Position pos) { return IsGridLoaded(pos.GetPositionX(), pos.GetPositionY()); }

        public bool IsGridLoaded(GridCoord p)
        {
            return (GetGrid(p.X_coord, p.Y_coord) != null && IsGridObjectDataLoaded(p.X_coord, p.Y_coord));
        }

        void VisitNearbyCellsOf(WorldObject obj, Visitor gridVisitor, Visitor worldVisitor)
        {
            // Check for valid position
            if (!obj.IsPositionValid())
                return;

            // Update mobs/objects in ALL visible cells around object!
            CellArea area = Cell.CalculateCellArea(obj.GetPositionX(), obj.GetPositionY(), obj.GetGridActivationRange());

            for (uint x = area.low_bound.X_coord; x <= area.high_bound.X_coord; ++x)
            {
                for (uint y = area.low_bound.Y_coord; y <= area.high_bound.Y_coord; ++y)
                {
                    // marked cells are those that have been visited
                    // don't visit the same cell twice
                    uint cell_id = (y * MapConst.TotalCellsPerMap) + x;
                    if (IsCellMarked(cell_id))
                        continue;

                    MarkCell(cell_id);
                    var pair = new CellCoord(x, y);
                    var cell = new Cell(pair);
                    cell.SetNoCreate();
                    Visit(cell, gridVisitor);
                    Visit(cell, worldVisitor);
                }
            }
        }

        public void UpdatePlayerZoneStats(uint oldZone, uint newZone)
        {
            // Nothing to do if no change
            if (oldZone == newZone)
                return;

            if (oldZone != MapConst.InvalidZone)
            {
                Cypher.Assert(_zonePlayerCountMap[oldZone] != 0, $"A player left zone {oldZone} (went to {newZone}) - but there were no players in the zone!");
                --_zonePlayerCountMap[oldZone];
            }

            if (!_zonePlayerCountMap.ContainsKey(newZone))
                _zonePlayerCountMap[newZone] = 0;

            ++_zonePlayerCountMap[newZone];
        }

        public virtual void Update(uint diff)
        {
            _dynamicTree.Update(diff);

            // update worldsessions for existing players
            for (var i = 0; i < m_activePlayers.Count; ++i)
            {
                Player player = m_activePlayers[i];
                if (player.IsInWorld)
                {
                    WorldSession session = player.GetSession();
                    var updater = new MapSessionFilter(session);
                    session.Update(diff, updater);
                }
            }

            /// process any due respawns
            if (_respawnCheckTimer <= diff)
            {
                ProcessRespawns();
                _respawnCheckTimer = WorldConfig.GetUIntValue(WorldCfg.RespawnMinCheckIntervalMs);
            }
            else
                _respawnCheckTimer -= diff;

            // update active cells around players and active objects
            ResetMarkedCells();

            var update = new UpdaterNotifier(diff);

            var grid_object_update = new Visitor(update, GridMapTypeMask.AllGrid);
            var world_object_update = new Visitor(update, GridMapTypeMask.AllWorld);

            for (var i = 0; i < m_activePlayers.Count; ++i)
            {
                Player player = m_activePlayers[i];
                if (!player.IsInWorld)
                    continue;

                // update players at tick
                player.Update(diff);

                VisitNearbyCellsOf(player, grid_object_update, world_object_update);

                // If player is using far sight or mind vision, visit that object too
                WorldObject viewPoint = player.GetViewpoint();
                if (viewPoint)
                    VisitNearbyCellsOf(viewPoint, grid_object_update, world_object_update);

                // Handle updates for creatures in combat with player and are more than 60 yards away
                if (player.IsInCombat())
                {
                    List<Unit> toVisit = new();
                    foreach (var pair in player.GetCombatManager().GetPvECombatRefs())
                    {
                        Creature unit = pair.Value.GetOther(player).ToCreature();
                        if (unit != null)
                            if (unit.GetMapId() == player.GetMapId() && !unit.IsWithinDistInMap(player, GetVisibilityRange(), false))
                                toVisit.Add(unit);
                    }

                    foreach (Unit unit in toVisit)
                        VisitNearbyCellsOf(unit, grid_object_update, world_object_update);
                }

                { // Update any creatures that own auras the player has applications of
                    List<Unit> toVisit = new();
                    foreach (var pair in player.GetAppliedAuras())
                    {
                        Unit caster = pair.Value.GetBase().GetCaster();
                        if (caster != null)
                            if (!caster.IsPlayer() && !caster.IsWithinDistInMap(player, GetVisibilityRange(), false))
                                toVisit.Add(caster);
                    }
                    foreach (Unit unit in toVisit)
                        VisitNearbyCellsOf(unit, grid_object_update, world_object_update);
                }
            }

            for (var i = 0; i < m_activeNonPlayers.Count; ++i)
            {
                WorldObject obj = m_activeNonPlayers[i];
                if (!obj.IsInWorld)
                    continue;

                VisitNearbyCellsOf(obj, grid_object_update, world_object_update);
            }

            for (var i = 0; i < _transports.Count; ++i)
            {
                Transport transport = _transports[i];
                if (!transport || !transport.IsInWorld)
                    continue;

                transport.Update(diff);
            }

            SendObjectUpdates();

            // Process necessary scripts
            if (!m_scriptSchedule.Empty())
            {
                i_scriptLock = true;
                ScriptsProcess();
                i_scriptLock = false;
            }

            _weatherUpdateTimer.Update(diff);
            if (_weatherUpdateTimer.Passed())
            {
                foreach (var zoneInfo in _zoneDynamicInfo)
                {
                    if (zoneInfo.Value.DefaultWeather != null && !zoneInfo.Value.DefaultWeather.Update((uint)_weatherUpdateTimer.GetInterval()))
                        zoneInfo.Value.DefaultWeather = null;
                }

                _weatherUpdateTimer.Reset();
            }

            // update phase shift objects
            GetMultiPersonalPhaseTracker().Update(this, diff);

            MoveAllCreaturesInMoveList();
            MoveAllGameObjectsInMoveList();
            MoveAllAreaTriggersInMoveList();

            if (!m_activePlayers.Empty() || !m_activeNonPlayers.Empty())
                ProcessRelocationNotifies(diff);

            Global.ScriptMgr.OnMapUpdate(this, diff);
        }

        void ProcessRelocationNotifies(uint diff)
        {
            for (uint x = 0; x < MapConst.MaxGrids; ++x)
            {
                for (uint y = 0; y < MapConst.MaxGrids; ++y)
                {
                    Grid grid = GetGrid(x, y);
                    if (grid == null)
                        continue;

                    if (grid.GetGridState() != GridState.Active)
                        continue;

                    grid.GetGridInfoRef().GetRelocationTimer().TUpdate((int)diff);
                    if (!grid.GetGridInfoRef().GetRelocationTimer().TPassed())
                        continue;

                    uint gx = grid.GetX();
                    uint gy = grid.GetY();

                    var cell_min = new CellCoord(gx * MapConst.MaxCells, gy * MapConst.MaxCells);
                    var cell_max = new CellCoord(cell_min.X_coord + MapConst.MaxCells, cell_min.Y_coord + MapConst.MaxCells);

                    for (uint xx = cell_min.X_coord; xx < cell_max.X_coord; ++xx)
                    {
                        for (uint yy = cell_min.Y_coord; yy < cell_max.Y_coord; ++yy)
                        {
                            uint cell_id = (yy * MapConst.TotalCellsPerMap) + xx;
                            if (!IsCellMarked(cell_id))
                                continue;

                            var pair = new CellCoord(xx, yy);
                            var cell = new Cell(pair);
                            cell.SetNoCreate();

                            var cell_relocation = new DelayedUnitRelocation(cell, pair, this, SharedConst.MaxVisibilityDistance);
                            var grid_object_relocation = new Visitor(cell_relocation, GridMapTypeMask.AllGrid);
                            var world_object_relocation = new Visitor(cell_relocation, GridMapTypeMask.AllWorld);

                            Visit(cell, grid_object_relocation);
                            Visit(cell, world_object_relocation);
                        }
                    }
                }
            }
            var reset = new ResetNotifier();
            var grid_notifier = new Visitor(reset, GridMapTypeMask.AllGrid);
            var world_notifier = new Visitor(reset, GridMapTypeMask.AllWorld);

            for (uint x = 0; x < MapConst.MaxGrids; ++x)
            {
                for (uint y = 0; y < MapConst.MaxGrids; ++y)
                {
                    Grid grid = GetGrid(x, y);
                    if (grid == null)
                        continue;

                    if (grid.GetGridState() != GridState.Active)
                        continue;

                    if (!grid.GetGridInfoRef().GetRelocationTimer().TPassed())
                        continue;

                    grid.GetGridInfoRef().GetRelocationTimer().TReset((int)diff, m_VisibilityNotifyPeriod);

                    uint gx = grid.GetX();
                    uint gy = grid.GetY();

                    var cell_min = new CellCoord(gx * MapConst.MaxCells, gy * MapConst.MaxCells);
                    var cell_max = new CellCoord(cell_min.X_coord + MapConst.MaxCells,
                        cell_min.Y_coord + MapConst.MaxCells);

                    for (uint xx = cell_min.X_coord; xx < cell_max.X_coord; ++xx)
                    {
                        for (uint yy = cell_min.Y_coord; yy < cell_max.Y_coord; ++yy)
                        {
                            uint cell_id = (yy * MapConst.TotalCellsPerMap) + xx;
                            if (!IsCellMarked(cell_id))
                                continue;

                            var pair = new CellCoord(xx, yy);
                            var cell = new Cell(pair);
                            cell.SetNoCreate();
                            Visit(cell, grid_notifier);
                            Visit(cell, world_notifier);
                        }
                    }
                }
            }
        }

        public virtual void RemovePlayerFromMap(Player player, bool remove)
        {
            // Before leaving map, update zone/area for stats
            player.UpdateZone(MapConst.InvalidZone, 0);
            Global.ScriptMgr.OnPlayerLeaveMap(this, player);

            GetMultiPersonalPhaseTracker().MarkAllPhasesForDeletion(player.GetGUID());

            player.CombatStop();

            bool inWorld = player.IsInWorld;
            player.RemoveFromWorld();
            SendRemoveTransports(player);

            if (!inWorld) // if was in world, RemoveFromWorld() called DestroyForNearbyPlayers()
                player.DestroyForNearbyPlayers(); // previous player.UpdateObjectVisibility(true)

            Cell cell = player.GetCurrentCell();
            RemoveFromGrid(player, cell);

            m_activePlayers.Remove(player);

            if (remove)
                DeleteFromWorld(player);
        }

        public void RemoveFromMap(WorldObject obj, bool remove)
        {
            bool inWorld = obj.IsInWorld && obj.GetTypeId() >= TypeId.Unit && obj.GetTypeId() <= TypeId.GameObject;
            obj.RemoveFromWorld();
            if (obj.IsActiveObject())
                RemoveFromActive(obj);

            GetMultiPersonalPhaseTracker().UnregisterTrackedObject(obj);

            if (!inWorld) // if was in world, RemoveFromWorld() called DestroyForNearbyPlayers()
                obj.DestroyForNearbyPlayers(); // previous obj.UpdateObjectVisibility(true)

            Cell cell = obj.GetCurrentCell();
            RemoveFromGrid(obj, cell);

            obj.ResetMap();

            if (remove)
                DeleteFromWorld(obj);
        }

        public void RemoveFromMap(Transport obj, bool remove)
        {
            obj.RemoveFromWorld();

            var players = GetPlayers();
            if (!players.Empty())
            {
                UpdateData data = new(GetId());
                obj.BuildOutOfRangeUpdateBlock(data);
                UpdateObject packet;
                data.BuildPacket(out packet);

                foreach (var player in players)
                {
                    if (player.GetTransport() != obj)
                    {
                        player.SendPacket(packet);
                        player.m_visibleTransports.Remove(obj.GetGUID());
                    }
                }
            }

            if (!_transports.Contains(obj))
                return;

            _transports.Remove(obj);

            obj.ResetMap();
            if (remove)
                DeleteFromWorld(obj);
        }

        bool CheckGridIntegrity<T>(T obj, bool moved) where T : WorldObject
        {
            Cell cur_cell = obj.GetCurrentCell();
            Cell xy_cell = new(obj.GetPositionX(), obj.GetPositionY());
            if (xy_cell != cur_cell)
            {
                //$"grid[{GetGridX()}, {GetGridY()}]cell[{GetCellX()}, {GetCellY()}]";
                Log.outDebug(LogFilter.Maps, $"{obj.GetTypeId()} ({obj.GetGUID()}) X: {obj.GetPositionX()} Y: {obj.GetPositionY()} ({(moved ? "final" : "original")}) is in {cur_cell} instead of {xy_cell}");
                return true;                                        // not crash at error, just output error in debug mode
            }

            return true;
        }

        public void PlayerRelocation(Player player, float x, float y, float z, float orientation)
        {
            var oldcell = player.GetCurrentCell();
            var newcell = new Cell(x, y);

            player.Relocate(x, y, z, orientation);
            if (player.IsVehicle())
                player.GetVehicleKit().RelocatePassengers();

            if (oldcell.DiffGrid(newcell) || oldcell.DiffCell(newcell))
            {
                Log.outDebug(LogFilter.Maps, "Player {0} relocation grid[{1}, {2}]cell[{3}, {4}].grid[{5}, {6}]cell[{7}, {8}]",
                    player.GetName(), oldcell.GetGridX(), oldcell.GetGridY(), oldcell.GetCellX(), oldcell.GetCellY(),
                    newcell.GetGridX(), newcell.GetGridY(), newcell.GetCellX(), newcell.GetCellY());

                RemoveFromGrid(player, oldcell);
                if (oldcell.DiffGrid(newcell))
                    EnsureGridLoadedForActiveObject(newcell, player);

                AddToGrid(player, newcell);
            }

            player.UpdatePositionData();
            player.UpdateObjectVisibility(false);
        }

        public void CreatureRelocation(Creature creature, float x, float y, float z, float ang, bool respawnRelocationOnFail = true)
        {
            Cypher.Assert(CheckGridIntegrity(creature, false));

            var new_cell = new Cell(x, y);

            if (!respawnRelocationOnFail && GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
                return;

            Cell old_cell = creature.GetCurrentCell();
            // delay creature move for grid/cell to grid/cell moves
            if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
            {
                AddCreatureToMoveList(creature, x, y, z, ang);
                // in diffcell/diffgrid case notifiers called at finishing move creature in MoveAllCreaturesInMoveList
            }
            else
            {
                creature.Relocate(x, y, z, ang);
                if (creature.IsVehicle())
                    creature.GetVehicleKit().RelocatePassengers();
                creature.UpdateObjectVisibility(false);
                creature.UpdatePositionData();
                RemoveCreatureFromMoveList(creature);
            }

            Cypher.Assert(CheckGridIntegrity(creature, true));
        }

        public void GameObjectRelocation(GameObject go, float x, float y, float z, float orientation, bool respawnRelocationOnFail = true)
        {
            Cypher.Assert(CheckGridIntegrity(go, false));

            var new_cell = new Cell(x, y);
            if (!respawnRelocationOnFail && GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
                return;

            Cell old_cell = go.GetCurrentCell();
            // delay creature move for grid/cell to grid/cell moves
            if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
            {
                Log.outDebug(LogFilter.Maps,
                    "GameObject (GUID: {0} Entry: {1}) added to moving list from grid[{2}, {3}]cell[{4}, {5}] to grid[{6}, {7}]cell[{8}, {9}].",
                    go.GetGUID().ToString(), go.GetEntry(), old_cell.GetGridX(), old_cell.GetGridY(), old_cell.GetCellX(),
                    old_cell.GetCellY(), new_cell.GetGridX(), new_cell.GetGridY(), new_cell.GetCellX(),
                    new_cell.GetCellY());
                AddGameObjectToMoveList(go, x, y, z, orientation);
                // in diffcell/diffgrid case notifiers called at finishing move go in Map.MoveAllGameObjectsInMoveList
            }
            else
            {
                go.Relocate(x, y, z, orientation);
                go.UpdateModelPosition();
                go.UpdatePositionData();
                go.UpdateObjectVisibility(false);
                RemoveGameObjectFromMoveList(go);
            }

            Cypher.Assert(CheckGridIntegrity(go, true));
        }

        public void DynamicObjectRelocation(DynamicObject dynObj, float x, float y, float z, float orientation)
        {
            Cypher.Assert(CheckGridIntegrity(dynObj, false));
            Cell new_cell = new(x, y);

            if (GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
                return;

            Cell old_cell = dynObj.GetCurrentCell();
            // delay creature move for grid/cell to grid/cell moves
            if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
            {

                Log.outDebug(LogFilter.Maps, "DynamicObject (GUID: {0}) added to moving list from grid[{1}, {2}]cell[{3}, {4}] to grid[{5}, {6}]cell[{7}, {8}].",
                    dynObj.GetGUID().ToString(), old_cell.GetGridX(), old_cell.GetGridY(), old_cell.GetCellX(), old_cell.GetCellY(), new_cell.GetGridX(), new_cell.GetGridY(), new_cell.GetCellX(), new_cell.GetCellY());

                AddDynamicObjectToMoveList(dynObj, x, y, z, orientation);
                // in diffcell/diffgrid case notifiers called at finishing move dynObj in Map.MoveAllGameObjectsInMoveList
            }
            else
            {
                dynObj.Relocate(x, y, z, orientation);
                dynObj.UpdatePositionData();
                dynObj.UpdateObjectVisibility(false);
                RemoveDynamicObjectFromMoveList(dynObj);
            }

            Cypher.Assert(CheckGridIntegrity(dynObj, true));
        }

        public void AreaTriggerRelocation(AreaTrigger at, float x, float y, float z, float orientation)
        {
            Cypher.Assert(CheckGridIntegrity(at, false));
            Cell new_cell = new(x, y);

            if (GetGrid(new_cell.GetGridX(), new_cell.GetGridY()) == null)
                return;

            Cell old_cell = at.GetCurrentCell();
            // delay areatrigger move for grid/cell to grid/cell moves
            if (old_cell.DiffCell(new_cell) || old_cell.DiffGrid(new_cell))
            {
                Log.outDebug(LogFilter.Maps, "AreaTrigger ({0}) added to moving list from {1} to {2}.", at.GetGUID().ToString(), old_cell.ToString(), new_cell.ToString());

                AddAreaTriggerToMoveList(at, x, y, z, orientation);
                // in diffcell/diffgrid case notifiers called at finishing move at in Map::MoveAllAreaTriggersInMoveList
            }
            else
            {
                at.Relocate(x, y, z, orientation);
                at.UpdateShape();
                at.UpdateObjectVisibility(false);
                RemoveAreaTriggerFromMoveList(at);
            }

            Cypher.Assert(CheckGridIntegrity(at, true));
        }

        void AddCreatureToMoveList(Creature c, float x, float y, float z, float ang)
        {
            if (_creatureToMoveLock) //can this happen?
                return;

            if (c._moveState == ObjectCellMoveState.None)
                creaturesToMove.Add(c);

            c.SetNewCellPosition(x, y, z, ang);
        }

        void AddGameObjectToMoveList(GameObject go, float x, float y, float z, float ang)
        {
            if (_gameObjectsToMoveLock) //can this happen?
                return;

            if (go._moveState == ObjectCellMoveState.None)
                _gameObjectsToMove.Add(go);
            go.SetNewCellPosition(x, y, z, ang);
        }

        void RemoveGameObjectFromMoveList(GameObject go)
        {
            if (_gameObjectsToMoveLock) //can this happen?
                return;

            if (go._moveState == ObjectCellMoveState.Active)
                go._moveState = ObjectCellMoveState.Inactive;
        }

        void RemoveCreatureFromMoveList(Creature c)
        {
            if (_creatureToMoveLock) //can this happen?
                return;

            if (c._moveState == ObjectCellMoveState.Active)
                c._moveState = ObjectCellMoveState.Inactive;
        }

        void AddDynamicObjectToMoveList(DynamicObject dynObj, float x, float y, float z, float ang)
        {
            if (_dynamicObjectsToMoveLock) //can this happen?
                return;

            if (dynObj._moveState == ObjectCellMoveState.None)
                _dynamicObjectsToMove.Add(dynObj);
            dynObj.SetNewCellPosition(x, y, z, ang);
        }

        void RemoveDynamicObjectFromMoveList(DynamicObject dynObj)
        {
            if (_dynamicObjectsToMoveLock) //can this happen?
                return;

            if (dynObj._moveState == ObjectCellMoveState.Active)
                dynObj._moveState = ObjectCellMoveState.Inactive;
        }

        void AddAreaTriggerToMoveList(AreaTrigger at, float x, float y, float z, float ang)
        {
            if (_areaTriggersToMoveLock) //can this happen?
                return;

            if (at._moveState == ObjectCellMoveState.None)
                _areaTriggersToMove.Add(at);
            at.SetNewCellPosition(x, y, z, ang);
        }

        void RemoveAreaTriggerFromMoveList(AreaTrigger at)
        {
            if (_areaTriggersToMoveLock) //can this happen?
                return;

            if (at._moveState == ObjectCellMoveState.Active)
                at._moveState = ObjectCellMoveState.Inactive;
        }

        void MoveAllCreaturesInMoveList()
        {
            _creatureToMoveLock = true;

            for (var i = 0; i < creaturesToMove.Count; ++i)
            {
                Creature creature = creaturesToMove[i];
                if (creature.GetMap() != this) //pet is teleported to another map
                    continue;

                if (creature._moveState != ObjectCellMoveState.Active)
                {
                    creature._moveState = ObjectCellMoveState.None;
                    continue;
                }

                creature._moveState = ObjectCellMoveState.None;
                if (!creature.IsInWorld)
                    continue;

                // do move or do move to respawn or remove creature if previous all fail
                if (CreatureCellRelocation(creature, new Cell(creature._newPosition.posX, creature._newPosition.posY)))
                {
                    // update pos
                    creature.Relocate(creature._newPosition);
                    if (creature.IsVehicle())
                        creature.GetVehicleKit().RelocatePassengers();
                    creature.UpdatePositionData();
                    creature.UpdateObjectVisibility(false);
                }
                else
                {
                    // if creature can't be move in new cell/grid (not loaded) move it to repawn cell/grid
                    // creature coordinates will be updated and notifiers send
                    if (!CreatureRespawnRelocation(creature, false))
                    {
                        // ... or unload (if respawn grid also not loaded)
                        //This may happen when a player just logs in and a pet moves to a nearby unloaded cell
                        //To avoid this, we can load nearby cells when player log in
                        //But this check is always needed to ensure safety
                        // @todo pets will disappear if this is outside CreatureRespawnRelocation
                        //need to check why pet is frequently relocated to an unloaded cell
                        if (creature.IsPet())
                            ((Pet)creature).Remove(PetSaveMode.NotInSlot, true);
                        else
                            AddObjectToRemoveList(creature);
                    }
                }
            }

            creaturesToMove.Clear();
            _creatureToMoveLock = false;
        }

        void MoveAllGameObjectsInMoveList()
        {
            _gameObjectsToMoveLock = true;

            for (var i = 0; i < _gameObjectsToMove.Count; ++i)
            {
                GameObject go = _gameObjectsToMove[i];
                if (go.GetMap() != this) //transport is teleported to another map
                    continue;

                if (go._moveState != ObjectCellMoveState.Active)
                {
                    go._moveState = ObjectCellMoveState.None;
                    continue;
                }

                go._moveState = ObjectCellMoveState.None;
                if (!go.IsInWorld)
                    continue;

                // do move or do move to respawn or remove creature if previous all fail
                if (GameObjectCellRelocation(go, new Cell(go._newPosition.posX, go._newPosition.posY)))
                {
                    // update pos
                    go.Relocate(go._newPosition);
                    go.UpdateModelPosition();
                    go.UpdatePositionData();
                    go.UpdateObjectVisibility(false);
                }
                else
                {
                    // if GameObject can't be move in new cell/grid (not loaded) move it to repawn cell/grid
                    // GameObject coordinates will be updated and notifiers send
                    if (!GameObjectRespawnRelocation(go, false))
                    {
                        // ... or unload (if respawn grid also not loaded)
                        Log.outDebug(LogFilter.Maps,
                            "GameObject (GUID: {0} Entry: {1}) cannot be move to unloaded respawn grid.",
                            go.GetGUID().ToString(), go.GetEntry());
                        AddObjectToRemoveList(go);
                    }
                }
            }

            _gameObjectsToMove.Clear();
            _gameObjectsToMoveLock = false;
        }

        void MoveAllDynamicObjectsInMoveList()
        {
            _dynamicObjectsToMoveLock = true;

            for (var i = 0; i < _dynamicObjectsToMove.Count; ++i)
            {
                DynamicObject dynObj = _dynamicObjectsToMove[i];
                if (dynObj.GetMap() != this) //transport is teleported to another map
                    continue;

                if (dynObj._moveState != ObjectCellMoveState.Active)
                {
                    dynObj._moveState = ObjectCellMoveState.None;
                    continue;
                }

                dynObj._moveState = ObjectCellMoveState.None;
                if (!dynObj.IsInWorld)
                    continue;

                // do move or do move to respawn or remove creature if previous all fail
                if (DynamicObjectCellRelocation(dynObj, new Cell(dynObj._newPosition.posX, dynObj._newPosition.posY)))
                {
                    // update pos
                    dynObj.Relocate(dynObj._newPosition);
                    dynObj.UpdatePositionData();
                    dynObj.UpdateObjectVisibility(false);
                }
                else
                    Log.outDebug(LogFilter.Maps, "DynamicObject (GUID: {0}) cannot be moved to unloaded grid.", dynObj.GetGUID().ToString());
            }

            _dynamicObjectsToMove.Clear();
            _dynamicObjectsToMoveLock = false;
        }

        void MoveAllAreaTriggersInMoveList()
        {
            _areaTriggersToMoveLock = true;

            for (var i = 0; i < _areaTriggersToMove.Count; ++i)
            {
                AreaTrigger at = _areaTriggersToMove[i];
                if (at.GetMap() != this) //transport is teleported to another map
                    continue;

                if (at._moveState != ObjectCellMoveState.Active)
                {
                    at._moveState = ObjectCellMoveState.None;
                    continue;
                }

                at._moveState = ObjectCellMoveState.None;
                if (!at.IsInWorld)
                    continue;

                // do move or do move to respawn or remove creature if previous all fail
                if (AreaTriggerCellRelocation(at, new Cell(at._newPosition.posX, at._newPosition.posY)))
                {
                    // update pos
                    at.Relocate(at._newPosition);
                    at.UpdateShape();
                    at.UpdateObjectVisibility(false);
                }
                else
                {
                    Log.outDebug(LogFilter.Maps, "AreaTrigger ({0}) cannot be moved to unloaded grid.", at.GetGUID().ToString());
                }
            }

            _areaTriggersToMove.Clear();
            _areaTriggersToMoveLock = false;
        }

        bool MapObjectCellRelocation<T>(T obj, Cell new_cell) where T : WorldObject
        {
            Cell old_cell = obj.GetCurrentCell();
            if (!old_cell.DiffGrid(new_cell)) // in same grid
            {
                // if in same cell then none do
                if (old_cell.DiffCell(new_cell))
                {
                    RemoveFromGrid(obj, old_cell);
                    AddToGrid(obj, new_cell);
                }

                return true;
            }

            // in diff. grids but active creature
            if (obj.IsActiveObject())
            {
                EnsureGridLoadedForActiveObject(new_cell, obj);

                Log.outDebug(LogFilter.Maps,
                    "Active creature (GUID: {0} Entry: {1}) moved from grid[{2}, {3}] to grid[{4}, {5}].",
                    obj.GetGUID().ToString(), obj.GetEntry(), old_cell.GetGridX(),
                    old_cell.GetGridY(), new_cell.GetGridX(), new_cell.GetGridY());
                RemoveFromGrid(obj, old_cell);
                AddToGrid(obj, new_cell);

                return true;
            }

            Creature c = obj.ToCreature();
            if (c != null && c.GetCharmerOrOwnerGUID().IsPlayer())
                EnsureGridLoaded(new_cell);

            // in diff. loaded grid normal creature
            var grid = new GridCoord(new_cell.GetGridX(), new_cell.GetGridY());
            if (IsGridLoaded(grid))
            {
                RemoveFromGrid(obj, old_cell);
                EnsureGridCreated(grid);
                AddToGrid(obj, new_cell);
                return true;
            }

            // fail to move: normal creature attempt move to unloaded grid
            return false;
        }

        bool CreatureCellRelocation(Creature c, Cell new_cell)
        {
            return MapObjectCellRelocation(c, new_cell);
        }

        bool GameObjectCellRelocation(GameObject go, Cell new_cell)
        {
            return MapObjectCellRelocation(go, new_cell);
        }

        bool DynamicObjectCellRelocation(DynamicObject go, Cell new_cell)
        {
            return MapObjectCellRelocation(go, new_cell);
        }

        bool AreaTriggerCellRelocation(AreaTrigger at, Cell new_cell)
        {
            return MapObjectCellRelocation(at, new_cell);
        }

        public bool CreatureRespawnRelocation(Creature c, bool diffGridOnly)
        {
            float resp_x, resp_y, resp_z, resp_o;
            c.GetRespawnPosition(out resp_x, out resp_y, out resp_z, out resp_o);
            var resp_cell = new Cell(resp_x, resp_y);

            //creature will be unloaded with grid
            if (diffGridOnly && !c.GetCurrentCell().DiffGrid(resp_cell))
                return true;

            c.CombatStop();
            c.GetMotionMaster().Clear();

            // teleport it to respawn point (like normal respawn if player see)
            if (CreatureCellRelocation(c, resp_cell))
            {
                c.Relocate(resp_x, resp_y, resp_z, resp_o);
                c.GetMotionMaster().Initialize(); // prevent possible problems with default move generators
                c.UpdatePositionData();
                c.UpdateObjectVisibility(false);
                return true;
            }

            return false;
        }

        public bool GameObjectRespawnRelocation(GameObject go, bool diffGridOnly)
        {
            float resp_x, resp_y, resp_z, resp_o;
            go.GetRespawnPosition(out resp_x, out resp_y, out resp_z, out resp_o);
            var resp_cell = new Cell(resp_x, resp_y);

            //GameObject will be unloaded with grid
            if (diffGridOnly && !go.GetCurrentCell().DiffGrid(resp_cell))
                return true;

            Log.outDebug(LogFilter.Maps,
                "GameObject (GUID: {0} Entry: {1}) moved from grid[{2}, {3}] to respawn grid[{4}, {5}].",
                go.GetGUID().ToString(), go.GetEntry(), go.GetCurrentCell().GetGridX(), go.GetCurrentCell().GetGridY(),
                resp_cell.GetGridX(), resp_cell.GetGridY());

            // teleport it to respawn point (like normal respawn if player see)
            if (GameObjectCellRelocation(go, resp_cell))
            {
                go.Relocate(resp_x, resp_y, resp_z, resp_o);
                go.UpdatePositionData();
                go.UpdateObjectVisibility(false);
                return true;
            }
            return false;
        }

        public bool UnloadGrid(Grid grid, bool unloadAll)
        {
            uint x = grid.GetX();
            uint y = grid.GetY();

            if (!unloadAll)
            {
                //pets, possessed creatures (must be active), transport passengers
                if (grid.GetWorldObjectCountInNGrid<Creature>() != 0)
                    return false;

                if (ActiveObjectsNearGrid(grid))
                    return false;
            }

            Log.outDebug(LogFilter.Maps, "Unloading grid[{0}, {1}] for map {2}", x, y, GetId());

            if (!unloadAll)
            {
                // Finish creature moves, remove and delete all creatures with delayed remove before moving to respawn grids
                // Must know real mob position before move
                MoveAllCreaturesInMoveList();
                MoveAllGameObjectsInMoveList();
                MoveAllAreaTriggersInMoveList();

                // move creatures to respawn grids if this is diff.grid or to remove list
                ObjectGridEvacuator worker = new();
                var visitor = new Visitor(worker, GridMapTypeMask.AllGrid);
                grid.VisitAllGrids(visitor);

                // Finish creature moves, remove and delete all creatures with delayed remove before unload
                MoveAllCreaturesInMoveList();
                MoveAllGameObjectsInMoveList();
                MoveAllAreaTriggersInMoveList();
            }

            {
                ObjectGridCleaner worker = new();
                var visitor = new Visitor(worker, GridMapTypeMask.AllGrid);
                grid.VisitAllGrids(visitor);
            }

            RemoveAllObjectsInRemoveList();

            // After removing all objects from the map, purge empty tracked phases
            GetMultiPersonalPhaseTracker().UnloadGrid(grid);

            {
                ObjectGridUnloader worker = new();
                var visitor = new Visitor(worker, GridMapTypeMask.AllGrid);
                grid.VisitAllGrids(visitor);
            }

            Cypher.Assert(i_objectsToRemove.Empty());
            SetGrid(null, x, y);

            uint gx = (MapConst.MaxGrids - 1) - x;
            uint gy = (MapConst.MaxGrids - 1) - y;

            // delete grid map, but don't delete if it is from parent map (and thus only reference)
            if (GridMaps[gx][gy] != null)
            {
                Map terrainRoot = m_parentMap.GetRootParentTerrainMap();
                if (--terrainRoot.GridMapReference[gx][gy] == 0)
                {
                    m_parentMap.GetRootParentTerrainMap().UnloadMap(gx, gy);
                    Global.VMapMgr.UnloadMap(terrainRoot.GetId(), gx, gy);
                    Global.MMapMgr.UnloadMap(terrainRoot.GetId(), gx, gy);
                }
            }

            Log.outDebug(LogFilter.Maps, "Unloading grid[{0}, {1}] for map {2} finished", x, y, GetId());
            return true;
        }

        public virtual void RemoveAllPlayers()
        {
            if (HavePlayers())
            {
                foreach (Player pl in m_activePlayers)
                {
                    if (!pl.IsBeingTeleportedFar())
                    {
                        // this is happening for bg
                        Log.outError(LogFilter.Maps, $"Map.UnloadAll: player {pl.GetName()} is still in map {GetId()} during unload, this should not happen!");
                        pl.TeleportTo(pl.GetHomebind());
                    }
                }
            }
        }

        public virtual void UnloadAll()
        {
            // clear all delayed moves, useless anyway do this moves before map unload.
            creaturesToMove.Clear();
            _gameObjectsToMove.Clear();

            for (uint x = 0; x < MapConst.MaxGrids; ++x)
            {
                for (uint y = 0; y < MapConst.MaxGrids; ++y)
                {
                    var grid = GetGrid(x, y);
                    if (grid == null)
                        continue;

                    UnloadGrid(grid, true); // deletes the grid and removes it from the GridRefManager
                }
            }

            for (var i = 0; i < _transports.Count; ++i)
                RemoveFromMap(_transports[i], true);

            _transports.Clear();

            foreach (var corpse in _corpsesByCell.Values.ToList())
            {
                corpse.RemoveFromWorld();
                corpse.ResetMap();
                corpse.Dispose();
            }

            _corpsesByCell.Clear();
            _corpsesByPlayer.Clear();
            _corpseBones.Clear();
        }

        GridMap GetGridMap(uint mapId, float x, float y)
        {
            // half opt method
            uint gx = (uint)(MapConst.CenterGridId - x / MapConst.SizeofGrids);                   //grid x
            uint gy = (uint)(MapConst.CenterGridId - y / MapConst.SizeofGrids);                   //grid y

            // ensure GridMap is loaded
            EnsureGridCreated(new GridCoord((MapConst.MaxGrids - 1) - gx, (MapConst.MaxGrids - 1) - gy));

            GridMap grid = GridMaps[gx][gy];
            var childMap = m_childTerrainMaps.Find(childTerrainMap => childTerrainMap.GetId() == mapId);
            if (childMap != null && childMap.GridMaps[gx][gy] != null)
                grid = childMap.GridMaps[gx][gy];

            return grid;
        }

        public bool HasChildMapGridFile(uint mapId, uint gx, uint gy)
        {
            var childMap = m_childTerrainMaps.Find(childTerrainMap => childTerrainMap.GetId() == mapId);
            return childMap != null && childMap.i_gridFileExists[(int)(gx * MapConst.MaxGrids + gy)];
        }

        public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            float ground = 0f;
            return GetWaterOrGroundLevel(phaseShift, x, y, z, ref ground);
        }

        public float GetWaterOrGroundLevel(PhaseShift phaseShift, float x, float y, float z, ref float ground, bool swim = false, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            if (GetGridMap(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y) != null)
            {
                // we need ground level (including grid height version) for proper return water level in point
                float ground_z = GetHeight(phaseShift, x, y, z + collisionHeight, true, 50.0f);
                ground = ground_z;

                LiquidData liquid_status;

                ZLiquidStatus res = GetLiquidStatus(phaseShift, x, y, ground_z, LiquidHeaderTypeFlags.AllLiquids, out liquid_status, collisionHeight);
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

                    return mapHeight; // better use .map surface height
                }

                return vmapHeight; // we have only vmapHeight (if have)
            }

            return mapHeight; // explicitly use map data
        }

        public float GetHeight(PhaseShift phaseShift, float x, float y, float z, bool vmap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
        {
            return Math.Max(GetStaticHeight(phaseShift, x, y, z, vmap, maxSearchDist), GetGameObjectFloor(phaseShift, x, y, z, maxSearchDist));
        }

        public float GetHeight(PhaseShift phaseShift, Position pos, bool vmap = true, float maxSearchDist = MapConst.DefaultHeightSearch)
        {
            return GetHeight(phaseShift, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), vmap, maxSearchDist);
        }

        public float GetGridHeight(PhaseShift phaseShift, float x, float y)
        {
            GridMap gmap = GetGridMap(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y);
            if (gmap != null)
                return gmap.GetHeight(x, y);

            return MapConst.VMAPInvalidHeightValue;
        }
        
        public float GetMinHeight(PhaseShift phaseShift, float x, float y)
        {
            GridMap grid = GetGridMap(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y);
            if (grid != null)
                return grid.GetMinHeight(x, y);

            return -500.0f;
        }

        public static bool IsInWMOInterior(uint mogpFlags)
        {
            return (mogpFlags & 0x2000) != 0;
        }

        private bool GetAreaInfo(PhaseShift phaseShift, float x, float y, float z, out uint mogpflags, out int adtId, out int rootId, out int groupId)
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
            uint dflags;
            int dadtId;
            int drootId;
            int dgroupId;

            bool hasVmapAreaInfo = Global.VMapMgr.GetAreaInfo(terrainMapId, x, y, ref vmap_z, out vflags, out vadtId, out vrootId, out vgroupId);
            bool hasDynamicAreaInfo = _dynamicTree.GetAreaInfo(x, y, ref dynamic_z, phaseShift, out dflags, out dadtId, out drootId, out dgroupId);

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
                GridMap gmap = GetGridMap(terrainMapId, x, y);
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

        public uint GetAreaId(PhaseShift phaseShift, float x, float y, float z)
        {
            uint mogpFlags;
            int adtId, rootId, groupId;
            float vmapZ = z;
            bool hasVmapArea = GetAreaInfo(phaseShift, x, y, vmapZ, out mogpFlags, out adtId, out rootId, out groupId);

            uint gridAreaId = 0;
            float gridMapHeight = MapConst.InvalidHeight;
            GridMap gmap = GetGridMap(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y);
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
                areaId = i_mapRecord.AreaTableID;

            return areaId;
        }

        public uint GetAreaId(PhaseShift phaseShift, Position pos) { return GetAreaId(phaseShift, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()); }

        public uint GetZoneId(PhaseShift phaseShift, float x, float y, float z)
        {
            uint areaId = GetAreaId(phaseShift, x, y, z);
            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (area != null)
                if (area.ParentAreaID != 0)
                    return area.ParentAreaID;

            return areaId;
        }

        public uint GetZoneId(PhaseShift phaseShift, Position pos) { return GetZoneId(phaseShift, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()); }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, float x, float y, float z)
        {
            areaid = zoneid = GetAreaId(phaseShift, x, y, z);
            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(areaid);
            if (area != null)
                if (area.ParentAreaID != 0)
                    zoneid = area.ParentAreaID;
        }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, Position pos) { GetZoneAndAreaId(phaseShift, out zoneid, out areaid, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()); }

        public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            return GetLiquidStatus(phaseShift, x, y, z, reqLiquidType, out _, collisionHeight);
        }

        public ZLiquidStatus GetLiquidStatus(PhaseShift phaseShift, float x, float y, float z, LiquidHeaderTypeFlags reqLiquidType, out LiquidData data, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            data = new LiquidData();
            var result = ZLiquidStatus.NoWater;
            float liquid_level = MapConst.InvalidHeight;
            float ground_level = MapConst.InvalidHeight;
            uint liquid_type = 0;
            uint mogpFlags = 0;
            bool useGridLiquid = true;
            uint terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, this, x, y);

            if (Global.VMapMgr.GetLiquidLevel(terrainMapId, x, y, z, (byte)reqLiquidType, ref liquid_level, ref ground_level, ref liquid_type, ref mogpFlags))
            {
                useGridLiquid = !IsInWMOInterior(mogpFlags);
                Log.outDebug(LogFilter.Maps, "getLiquidStatus(): vmap liquid level: {0} ground: {1} type: {2}",
                    liquid_level, ground_level, liquid_type);
                // Check water level and ground level
                if (liquid_level > ground_level && MathFunctions.fuzzyGe(z, ground_level - MapConst.GroundHeightTolerance))
                {
                    // All ok in water . store data
                    // hardcoded in client like this
                    if (GetId() == 530 && liquid_type == 2)
                        liquid_type = 15;

                    uint liquidFlagType = 0;
                    LiquidTypeRecord liq = CliDB.LiquidTypeStorage.LookupByKey(liquid_type);
                    if (liq != null)
                        liquidFlagType = liq.SoundBank;

                    if (liquid_type != 0 && liquid_type < 21)
                    {
                        AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(GetAreaId(phaseShift, x, y, z));
                        if (area != null)
                        {
                            uint overrideLiquid = area.LiquidTypeID[liquidFlagType];
                            if (overrideLiquid == 0 && area.ParentAreaID != 0)
                            {
                                area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
                                if (area != null)
                                    overrideLiquid = area.LiquidTypeID[liquidFlagType];
                            }

                            liq = CliDB.LiquidTypeStorage.LookupByKey(overrideLiquid);
                            if (liq != null)
                            {
                                liquid_type = overrideLiquid;
                                liquidFlagType = liq.SoundBank;
                            }
                        }
                    }

                    data.level = liquid_level;
                    data.depth_level = ground_level;

                    data.entry = liquid_type;
                    data.type_flags = (LiquidHeaderTypeFlags)(1 << (int)liquidFlagType);

                    float delta = liquid_level - z;

                    // Get position delta
                    if (delta > collisionHeight) // Under water
                        return ZLiquidStatus.UnderWater;
                    if (delta > 0.0f) // In water
                        return ZLiquidStatus.InWater;
                    if (delta > -0.1f) // Walk on water
                        return ZLiquidStatus.WaterWalk;
                    result = ZLiquidStatus.AboveWater;
                }
            }

            if (useGridLiquid)
            {
                GridMap gmap = GetGridMap(terrainMapId, x, y);
                if (gmap != null)
                {
                    var map_data = new LiquidData();
                    ZLiquidStatus map_result = gmap.GetLiquidStatus(x, y, z, reqLiquidType, map_data, collisionHeight);
                    // Not override LIQUID_MAP_ABOVE_WATER with LIQUID_MAP_NO_WATER:
                    if (map_result != ZLiquidStatus.NoWater && (map_data.level > ground_level))
                    {
                        // hardcoded in client like this
                        if (GetId() == 530 && map_data.entry == 2)
                            map_data.entry = 15;

                        data = map_data;

                        return map_result;
                    }
                }
            }
            return result;
        }

        public void GetFullTerrainStatusForPosition(PhaseShift phaseShift, float x, float y, float z, PositionFullTerrainStatus data, LiquidHeaderTypeFlags reqLiquidType, float collisionHeight = MapConst.DefaultCollesionHeight)
        {
            uint terrainMapId = PhasingHandler.GetTerrainMapId(phaseShift, this, x, y);
            GridMap gmap = GetGridMap(terrainMapId, x, y);
            AreaAndLiquidData vmapData = Global.VMapMgr.GetAreaAndLiquidData(terrainMapId, x, y, z, (byte)reqLiquidType);
            AreaAndLiquidData dynData = _dynamicTree.GetAreaAndLiquidData(x, y, z, phaseShift, (byte)reqLiquidType);

            uint gridAreaId = 0;
            float gridMapHeight = MapConst.InvalidHeight;
            if (gmap != null)
            {
                gridAreaId = gmap.GetArea(x, y);
                gridMapHeight = gmap.GetHeight(x, y);
            }

            bool useGridLiquid = true;
            AreaAndLiquidData wmoData = null;

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
                data.AreaId = i_mapRecord.AreaTableID;

            var areaEntry = CliDB.AreaTableStorage.LookupByKey(data.AreaId);

            // liquid processing
            data.LiquidStatus = ZLiquidStatus.NoWater;
            if (wmoData != null && wmoData.liquidInfo.HasValue && wmoData.liquidInfo.Value.Level > wmoData.floorZ)
            {
                uint liquidType = wmoData.liquidInfo.Value.LiquidType;
                if (GetId() == 530 && liquidType == 2) // gotta love blizzard hacks
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

        public float GetWaterLevel(PhaseShift phaseShift, float x, float y)
        {
            GridMap gmap = GetGridMap(PhasingHandler.GetTerrainMapId(phaseShift, this, x, y), x, y);
            if (gmap != null)
                return gmap.GetLiquidLevel(x, y);
            return 0;
        }

        public bool IsInLineOfSight(PhaseShift phaseShift, float x1, float y1, float z1, float x2, float y2, float z2, LineOfSightChecks checks, ModelIgnoreFlags ignoreFlags)
        {
            if (checks.HasAnyFlag(LineOfSightChecks.Vmap) && !Global.VMapMgr.IsInLineOfSight(PhasingHandler.GetTerrainMapId(phaseShift, this, x1, y1), x1, y1, z1, x2, y2, z2, ignoreFlags))
                return false;

            if (WorldConfig.GetBoolValue(WorldCfg.CheckGobjectLos) && checks.HasAnyFlag(LineOfSightChecks.Gobject) && !_dynamicTree.IsInLineOfSight(new Vector3(x1, y1, z1), new Vector3(x2, y2, z2), phaseShift))
                return false;

            return true;
        }

        public bool GetObjectHitPos(PhaseShift phaseShift, float x1, float y1, float z1, float x2, float y2, float z2, out float rx, out float ry, out float rz, float modifyDist)
        {
            var startPos = new Vector3(x1, y1, z1);
            var dstPos = new Vector3(x2, y2, z2);

            var resultPos = new Vector3();
            bool result = _dynamicTree.GetObjectHitPos(startPos, dstPos, ref resultPos, modifyDist, phaseShift);

            rx = resultPos.X;
            ry = resultPos.Y;
            rz = resultPos.Z;
            return result;
        }

        public bool IsInWater(PhaseShift phaseShift, float x, float y, float pZ)
        {
            return Convert.ToBoolean(GetLiquidStatus(phaseShift, x, y, pZ, LiquidHeaderTypeFlags.AllLiquids) & (ZLiquidStatus.InWater | ZLiquidStatus.UnderWater));
        }

        public bool IsUnderWater(PhaseShift phaseShift, float x, float y, float z)
        {
            return Convert.ToBoolean(GetLiquidStatus(phaseShift, x, y, z, LiquidHeaderTypeFlags.Water | LiquidHeaderTypeFlags.Ocean) & ZLiquidStatus.UnderWater);
        }

        public string GetMapName()
        {
            return i_mapRecord != null ? i_mapRecord.MapName[Global.WorldMgr.GetDefaultDbcLocale()] : "UNNAMEDMAP";
        }

        public void SendInitSelf(Player player)
        {
            var data = new UpdateData(player.GetMapId());

            // attach to player data current transport data
            Transport transport = player.GetTransport();
            if (transport != null)
            {
                transport.BuildCreateUpdateBlockForPlayer(data, player);
                player.m_visibleTransports.Add(transport.GetGUID());
            }

            player.BuildCreateUpdateBlockForPlayer(data, player);

            // build other passengers at transport also (they always visible and marked as visible and will not send at visibility update at add to map
            if (transport != null)
            {
                foreach (WorldObject passenger in transport.GetPassengers())
                {
                    if (player != passenger && player.HaveAtClient(passenger))
                        passenger.BuildCreateUpdateBlockForPlayer(data, player);
                }
            }
            UpdateObject packet;
            data.BuildPacket(out packet);
            player.SendPacket(packet);
        }

        void SendInitTransports(Player player)
        {
            var transData = new UpdateData(player.GetMapId());

            foreach (Transport transport in _transports)
            {
                if (transport != player.GetTransport() && player.IsInPhase(transport))
                {
                    transport.BuildCreateUpdateBlockForPlayer(transData, player);
                    player.m_visibleTransports.Add(transport.GetGUID());
                }
            }

            UpdateObject packet;
            transData.BuildPacket(out packet);
            player.SendPacket(packet);
        }

        void SendRemoveTransports(Player player)
        {
            var transData = new UpdateData(player.GetMapId());
            foreach (Transport transport in _transports)
            {
                if (transport != player.GetTransport())
                {
                    transport.BuildOutOfRangeUpdateBlock(transData);
                    player.m_visibleTransports.Remove(transport.GetGUID());
                }
            }

            UpdateObject packet;
            transData.BuildPacket(out packet);
            player.SendPacket(packet);
        }

        public void SendUpdateTransportVisibility(Player player)
        {
            // Hack to send out transports
            UpdateData transData = new(player.GetMapId());
            foreach (var transport in _transports)
            {
                var hasTransport = player.m_visibleTransports.Contains(transport.GetGUID());
                if (player.IsInPhase(transport))
                {
                    if (!hasTransport)
                    {
                        transport.BuildCreateUpdateBlockForPlayer(transData, player);
                        player.m_visibleTransports.Add(transport.GetGUID());
                    }
                }
                else
                {
                    transport.BuildOutOfRangeUpdateBlock(transData);
                    player.m_visibleTransports.Remove(transport.GetGUID());
                }
            }

            UpdateObject packet;
            transData.BuildPacket(out packet);
            player.SendPacket(packet);
        }

        void SetGrid(Grid grid, uint x, uint y)
        {
            if (x >= MapConst.MaxGrids || y >= MapConst.MaxGrids)
            {
                Log.outError(LogFilter.Maps, "Map.setNGrid Invalid grid coordinates found: {0}, {1}!", x, y);
                return;
            }
            i_grids[x][y] = grid;
        }

        void SendObjectUpdates()
        {
            Dictionary<Player, UpdateData> update_players = new();

            while (!_updateObjects.Empty())
            {
                WorldObject obj = _updateObjects[0];
                Cypher.Assert(obj.IsInWorld);
                _updateObjects.RemoveAt(0);
                obj.BuildUpdate(update_players);
            }

            UpdateObject packet;
            foreach (var iter in update_players)
            {
                iter.Value.BuildPacket(out packet);
                iter.Key.SendPacket(packet);
            }
        }

        bool CheckRespawn(RespawnInfo info)
        {
            SpawnData data = Global.ObjectMgr.GetSpawnData(info.type, info.spawnId);
            Cypher.Assert(data != null, $"Invalid respawn info with type {info.type}, spawnID {info.spawnId} in respawn queue.");

            // First, check if this creature's spawn group is inactive
            if (!IsSpawnGroupActive(data.spawnGroupData.groupId))
            {
                info.respawnTime = 0;
                return false;
            }

            uint poolId = info.spawnId != 0 ? Global.PoolMgr.IsPartOfAPool(info.type, info.spawnId) : 0;
            // Next, check if there's already an instance of this object that would block the respawn
            // Only do this for unpooled spawns
            if (poolId == 0)
            {
                bool doDelete = false;
                switch (info.type)
                {
                    case SpawnObjectType.Creature:
                    {
                        // escort check for creatures only (if the world config boolean is set)
                        bool isEscort = WorldConfig.GetBoolValue(WorldCfg.RespawnDynamicEscortNpc) && data.spawnGroupData.flags.HasFlag(SpawnGroupFlags.EscortQuestNpc);

                        var range = _creatureBySpawnIdStore.LookupByKey(info.spawnId);
                        foreach (var creature in range)
                        {
                            if (!creature.IsAlive())
                                continue;

                            // escort NPCs are allowed to respawn as long as all other instances are already escorting
                            if (isEscort && creature.IsEscorted())
                                continue;

                            doDelete = true;
                            break;
                        }
                        break;
                    }
                    case SpawnObjectType.GameObject:
                        // gameobject check is simpler - they cannot be dead or escorting
                        if (_gameobjectBySpawnIdStore.ContainsKey(info.spawnId))
                            doDelete = true;
                        break;
                    default:
                        Cypher.Assert(false, $"Invalid spawn type {info.type} with spawnId {info.spawnId} on map {GetId()}");
                        return true;
                }
                if (doDelete)
                {
                    info.respawnTime = 0;
                    return false;
                }
            }

            // next, check linked respawn time
            ObjectGuid thisGUID = info.type == SpawnObjectType.GameObject ? ObjectGuid.Create(HighGuid.GameObject, GetId(), info.entry, info.spawnId) : ObjectGuid.Create(HighGuid.Creature, GetId(), info.entry, info.spawnId);
            long linkedTime = GetLinkedRespawnTime(thisGUID);
            if (linkedTime != 0)
            {
                long now = GameTime.GetGameTime();
                long respawnTime;
                if (linkedTime == long.MaxValue)
                    respawnTime = linkedTime;
                else if (Global.ObjectMgr.GetLinkedRespawnGuid(thisGUID) == thisGUID) // never respawn, save "something" in DB
                    respawnTime = now + Time.Week;
                else // set us to check again shortly after linked unit
                    respawnTime = Math.Max(now, linkedTime) + RandomHelper.URand(5, 15);
                info.respawnTime = respawnTime;
                return false;
            }

            // now, check if we're part of a pool
            if (poolId != 0)
            {
                // ok, part of a pool - hand off to pool logic to handle this, we're just going to remove the respawn and call it a day
                if (info.type == SpawnObjectType.GameObject)
                    Global.PoolMgr.UpdatePool<GameObject>(poolId, info.spawnId);
                else if (info.type == SpawnObjectType.Creature)
                    Global.PoolMgr.UpdatePool<Creature>(poolId, info.spawnId);
                else
                    Cypher.Assert(false, $"Invalid spawn type {info.type} (spawnid {info.spawnId}) on map {GetId()}");
                info.respawnTime = 0;
                return false;
            }

            // everything ok, let's spawn
            return true;
        }

        public void Respawn(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null)
        {
            RespawnInfo info = GetRespawnInfo(type, spawnId);
            if (info != null)
                Respawn(info, dbTrans);
        }

        public void Respawn(RespawnInfo info, SQLTransaction dbTrans = null)
        {
            info.respawnTime = GameTime.GetGameTime();
            SaveRespawnInfoDB(info, dbTrans);
        }

        public void RemoveRespawnTime(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null, bool alwaysDeleteFromDB = false)
        {
            RespawnInfo info = GetRespawnInfo(type, spawnId);
            if (info != null)
                DeleteRespawnInfo(info, dbTrans);
            // Some callers might need to make sure the database doesn't contain any respawn time
            else if (alwaysDeleteFromDB)
                DeleteRespawnInfoFromDB(type, spawnId, dbTrans);
        }

        int DespawnAll(SpawnObjectType type, ulong spawnId)
        {
            List<WorldObject> toUnload = new();
            switch (type)
            {
                case SpawnObjectType.Creature:
                    foreach (var creature in GetCreatureBySpawnIdStore().LookupByKey(spawnId))
                        toUnload.Add(creature);
                    break;
                case SpawnObjectType.GameObject:
                    foreach (var obj in GetGameObjectBySpawnIdStore().LookupByKey(spawnId))
                        toUnload.Add(obj);
                    break;
                default:
                    break;
            }

            foreach (WorldObject o in toUnload)
                AddObjectToRemoveList(o);

            return toUnload.Count;
        }

        bool AddRespawnInfo(RespawnInfo info)
        {
            if (info.spawnId == 0)
            {
                Log.outError(LogFilter.Maps, $"Attempt to insert respawn info for zero spawn id (type {info.type})");
                return false;
            }

            var bySpawnIdMap = GetRespawnMapForType(info.type);
            if (bySpawnIdMap == null)
                return false;

            // check if we already have the maximum possible number of respawns scheduled
            if (SpawnData.TypeHasData(info.type))
            {
                var existing = bySpawnIdMap.LookupByKey(info.spawnId);
                if (existing != null) // spawnid already has a respawn scheduled
                {
                    if (info.respawnTime <= existing.respawnTime) // delete existing in this case
                        DeleteRespawnInfo(existing);
                    else
                        return false;
                }
                Cypher.Assert(!bySpawnIdMap.ContainsKey(info.spawnId), $"Insertion of respawn info with id ({info.type},{info.spawnId}) into spawn id map failed - state desync.");
            }
            else
                Cypher.Assert(false, $"Invalid respawn info for spawn id ({info.type},{info.spawnId}) being inserted");

            RespawnInfo ri = new(info);
            _respawnTimes.Add(ri);
            bySpawnIdMap.Add(ri.spawnId, ri);
            return true;
        }

        static void PushRespawnInfoFrom(List<RespawnInfo> data, Dictionary<ulong, RespawnInfo> map)
        {
            foreach (var pair in map)
                data.Add(pair.Value);
        }

        public void GetRespawnInfo(List<RespawnInfo> respawnData, SpawnObjectTypeMask types)
        {
            if ((types & SpawnObjectTypeMask.Creature) != 0)
                PushRespawnInfoFrom(respawnData, _creatureRespawnTimesBySpawnId);
            if ((types & SpawnObjectTypeMask.GameObject) != 0)
                PushRespawnInfoFrom(respawnData, _gameObjectRespawnTimesBySpawnId);
        }
        
        public RespawnInfo GetRespawnInfo(SpawnObjectType type, ulong spawnId)
        {
            var map = GetRespawnMapForType(type);
            if (map == null)
                return null;

            var respawnInfo = map.LookupByKey(spawnId);
            if (respawnInfo == null)
                return null;

            return respawnInfo;
        }

        Dictionary<ulong, RespawnInfo> GetRespawnMapForType(SpawnObjectType type)
        {
            switch (type)
            {
                case SpawnObjectType.Creature:
                    return _creatureRespawnTimesBySpawnId;
                case SpawnObjectType.GameObject:
                    return _gameObjectRespawnTimesBySpawnId;
                case SpawnObjectType.AreaTrigger:
                    return null;
                default:
                    Cypher.Assert(false);
                    return null;
            }            
        }

        void UnloadAllRespawnInfos() // delete everything from memory
        {
            _respawnTimes.Clear();
            _creatureRespawnTimesBySpawnId.Clear();
            _gameObjectRespawnTimesBySpawnId.Clear();
        }

        void DeleteRespawnInfo(RespawnInfo info, SQLTransaction dbTrans = null)
        {
            // Delete from all relevant containers to ensure consistency
            Cypher.Assert(info != null);

            // spawnid store
            var spawnMap = GetRespawnMapForType(info.type);
            if (spawnMap == null)
                return;

            var respawnInfo = spawnMap.LookupByKey(info.spawnId);
            Cypher.Assert(respawnInfo != info, $"Respawn stores inconsistent for map {GetId()}, spawnid {info.spawnId} (type {info.type})");
            spawnMap.Remove(info.spawnId);

            // respawn heap
            _respawnTimes.Remove(info);

            // database
            DeleteRespawnInfoFromDB(info.type, info.spawnId, dbTrans);
        }

        void DeleteRespawnInfoFromDB(SpawnObjectType type, ulong spawnId, SQLTransaction dbTrans = null)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_RESPAWN);
            stmt.AddValue(0, (ushort)type);
            stmt.AddValue(1, spawnId);
            stmt.AddValue(2, GetId());
            stmt.AddValue(3, GetInstanceId());
            DB.Characters.ExecuteOrAppend(dbTrans, stmt);
        }

        void DoRespawn(SpawnObjectType type, ulong spawnId, uint gridId)
        {
            if (!IsGridLoaded(gridId)) // if grid isn't loaded, this will be processed in grid load handler
                return;

            switch (type)
            {
                case SpawnObjectType.Creature:
                {
                    Creature obj = new();
                    if (!obj.LoadFromDB(spawnId, this, true, true))
                        obj.Dispose();
                    break;
                }
                case SpawnObjectType.GameObject:
                {
                    GameObject obj = new();
                    if (!obj.LoadFromDB(spawnId, this, true))
                        obj.Dispose();
                    break;
                }
                default:
                    Cypher.Assert(false, $"Invalid spawn type {type} (spawnid {spawnId}) on map {GetId()}");
                    break;
            }
        }

        void ProcessRespawns()
        {
            long now = GameTime.GetGameTime();
            while (!_respawnTimes.Empty())
            {
                RespawnInfo next = _respawnTimes.First();
                if (now < next.respawnTime) // done for this tick
                    break;

                if (CheckRespawn(next)) // see if we're allowed to respawn
                {
                    // ok, respawn
                    _respawnTimes.Remove(next);
                    GetRespawnMapForType(next.type).Remove(next.spawnId);
                    DoRespawn(next.type, next.spawnId, next.gridId);
                }
                else if (next.respawnTime == 0) // just remove respawn entry without rescheduling
                {
                    _respawnTimes.Remove(next);
                    GetRespawnMapForType(next.type).Remove(next.spawnId);
                }
                else // value changed, update heap position
                {
                    Cypher.Assert(now < next.respawnTime); // infinite loop guard
                                                           //_respawnTimes.decrease(next.handle);
                    SaveRespawnInfoDB(next);
                }
            }
        }

        public void ApplyDynamicModeRespawnScaling(WorldObject obj, ulong spawnId, ref uint respawnDelay, uint mode)
        {
            Cypher.Assert(mode == 1);
            Cypher.Assert(obj.GetMap() == this);

            if (IsBattlegroundOrArena())
                return;

            SpawnObjectType type;
            switch (obj.GetTypeId())
            {
                case TypeId.Unit:
                    type = SpawnObjectType.Creature;
                    break;
                case TypeId.GameObject:
                    type = SpawnObjectType.GameObject;
                    break;
                default:
                    return;
            }

            SpawnMetadata data = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);
            if (data == null)
                return;

            if (!data.spawnGroupData.flags.HasFlag(SpawnGroupFlags.DynamicSpawnRate))
                return;

            if (!_zonePlayerCountMap.ContainsKey(obj.GetZoneId()))
                return;

            uint playerCount = _zonePlayerCountMap[obj.GetZoneId()];
            if (playerCount == 0)
                return;

            double adjustFactor = WorldConfig.GetFloatValue(type == SpawnObjectType.GameObject ? WorldCfg.RespawnDynamicRateGameobject : WorldCfg.RespawnDynamicRateCreature) / playerCount;
            if (adjustFactor >= 1.0) // nothing to do here
                return;

            uint timeMinimum = WorldConfig.GetUIntValue(type == SpawnObjectType.GameObject ? WorldCfg.RespawnDynamicMinimumGameObject : WorldCfg.RespawnDynamicMinimumCreature);
            if (respawnDelay <= timeMinimum)
                return;

            respawnDelay = (uint)Math.Max(Math.Ceiling(respawnDelay * adjustFactor), timeMinimum);
        }

        public bool ShouldBeSpawnedOnGridLoad<T>(ulong spawnId) { return ShouldBeSpawnedOnGridLoad(SpawnData.TypeFor<T>(), spawnId); }
        
        bool ShouldBeSpawnedOnGridLoad(SpawnObjectType type, ulong spawnId)
        {
            Cypher.Assert(SpawnData.TypeHasData(type));
            // check if the object is on its respawn timer
            if (GetRespawnTime(type, spawnId) != 0)
                return false;

            SpawnMetadata spawnData = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);
            // check if the object is part of a spawn group
            SpawnGroupTemplateData spawnGroup = spawnData.spawnGroupData;
            if (!spawnGroup.flags.HasFlag(SpawnGroupFlags.System))
                if (!IsSpawnGroupActive(spawnGroup.groupId))
                    return false;

            return true;
        }

        SpawnGroupTemplateData GetSpawnGroupData(uint groupId)
        {
            SpawnGroupTemplateData data = Global.ObjectMgr.GetSpawnGroupData(groupId);
            if (data != null && (data.flags.HasAnyFlag(SpawnGroupFlags.System) || data.mapId == GetId()))
                return data;

            return null;
        }

        public bool SpawnGroupSpawn(uint groupId, bool ignoreRespawn = false, bool force = false, List<WorldObject> spawnedObjects = null)
        {
            var groupData = GetSpawnGroupData(groupId);
            if (groupData == null || groupData.flags.HasAnyFlag(SpawnGroupFlags.System))
            {
                Log.outError(LogFilter.Maps, $"Tried to spawn non-existing (or system) spawn group {groupId}. on map {GetId()} Blocked.");
                return false;
            }

            SetSpawnGroupActive(groupId, true); // start processing respawns for the group

            List<SpawnData> toSpawn = new();
            foreach (var data in Global.ObjectMgr.GetSpawnMetadataForGroup(groupId))
            {
                Cypher.Assert(groupData.mapId == data.MapId);

                var respawnMap = GetRespawnMapForType(data.type);
                if (respawnMap == null)
                    continue;

                if (force || ignoreRespawn)
                    RemoveRespawnTime(data.type, data.SpawnId);

                bool hasRespawnTimer = respawnMap.ContainsKey(data.SpawnId);
                if (SpawnData.TypeHasData(data.type))
                {
                    // has a respawn timer
                    if (hasRespawnTimer)
                        continue;

                    // has a spawn already active
                    if (!force)
                    {
                        WorldObject obj = GetWorldObjectBySpawnId(data.type, data.SpawnId);
                        if (obj != null)
                            if ((data.type != SpawnObjectType.Creature) || obj.ToCreature().IsAlive())
                                continue;
                    }

                    toSpawn.Add(data.ToSpawnData());
                }
            }

            foreach (SpawnData data in toSpawn)
            {
                // don't spawn if the grid isn't loaded (will be handled in grid loader)
                if (!IsGridLoaded(data.SpawnPoint))
                    continue;

                // now do the actual (re)spawn
                switch (data.type)
                {
                    case SpawnObjectType.Creature:
                    {
                        Creature creature = new();
                        if (!creature.LoadFromDB(data.SpawnId, this, true, force))
                            creature.Dispose();
                        else if (spawnedObjects != null)
                            spawnedObjects.Add(creature);
                        break;
                    }
                    case SpawnObjectType.GameObject:
                    {
                        GameObject gameobject = new();
                        if (!gameobject.LoadFromDB(data.SpawnId, this, true))
                            gameobject.Dispose();
                        else if (spawnedObjects != null)
                            spawnedObjects.Add(gameobject);
                        break;
                    }
                    case SpawnObjectType.AreaTrigger:
                    {
                        AreaTrigger areaTrigger = new AreaTrigger();
                        if (!areaTrigger.LoadFromDB(data.SpawnId, this, true, false))
                            areaTrigger.Dispose();
                        else if (spawnedObjects != null)
                            spawnedObjects.Add(areaTrigger);
                        break;
                    }
                    default:
                        Cypher.Assert(false, $"Invalid spawn type {data.type} with spawnId {data.SpawnId}");
                        return false;
                }
            }

            return true;
        }

        public bool SpawnGroupDespawn(uint groupId, bool deleteRespawnTimes)
        {
            return SpawnGroupDespawn(groupId, deleteRespawnTimes, out _);
        }

        public bool SpawnGroupDespawn(uint groupId, bool deleteRespawnTimes, out int count)
        {
            count = 0;
            SpawnGroupTemplateData groupData = GetSpawnGroupData(groupId);
            if (groupData == null || groupData.flags.HasAnyFlag(SpawnGroupFlags.System))
            {
                Log.outError(LogFilter.Maps, $"Tried to despawn non-existing (or system) spawn group {groupId} on map {GetId()}. Blocked.");
                return false;
            }

            foreach (var data in Global.ObjectMgr.GetSpawnMetadataForGroup(groupId))
            {
                Cypher.Assert(groupData.mapId == data.MapId);
                if (deleteRespawnTimes)
                    RemoveRespawnTime(data.type, data.SpawnId);
                count += DespawnAll(data.type, data.SpawnId);
            }

            SetSpawnGroupActive(groupId, false); // stop processing respawns for the group, too
            return true;
        }

        public void SetSpawnGroupActive(uint groupId, bool state)
        {
            SpawnGroupTemplateData data = GetSpawnGroupData(groupId);
            if (data == null || data.flags.HasAnyFlag(SpawnGroupFlags.System))
            {
                Log.outError(LogFilter.Maps, $"Tried to set non-existing (or system) spawn group {groupId} to {(state ? "active" : "inactive")} on map {GetId()}. Blocked.");
                return;
            }
            if (state != !data.flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn)) // toggled
                _toggledSpawnGroupIds.Add(groupId);
            else
                _toggledSpawnGroupIds.Remove(groupId);
        }

        // Disable the spawn group, which prevents any creatures in the group from respawning until re-enabled
        // This will not affect any already-present creatures in the group
        public void SetSpawnGroupInactive(uint groupId) { SetSpawnGroupActive(groupId, false); }

        public bool IsSpawnGroupActive(uint groupId)
        {
            SpawnGroupTemplateData data = GetSpawnGroupData(groupId);
            if (data == null)
            {
                Log.outError(LogFilter.Maps, $"Tried to query state of non-existing spawn group {groupId} on map {GetId()}.");
                return false;
            }

            if (data.flags.HasAnyFlag(SpawnGroupFlags.System))
                return true;

            // either manual spawn group and toggled, or not manual spawn group and not toggled...
            return _toggledSpawnGroupIds.Contains(groupId) != !data.flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn);
        }

        public void AddFarSpellCallback(FarSpellCallback callback)
        {
            _farSpellCallbacks.Enqueue(new FarSpellCallback(callback));
        }

        public virtual void DelayedUpdate(uint diff)
        {
            while (_farSpellCallbacks.TryDequeue(out FarSpellCallback callback))
                callback(this);

            for (var i = 0; i < _transports.Count; ++i)
            {
                Transport transport = _transports[i];
                if (!transport.IsInWorld)
                    continue;

                transport.DelayedUpdate(diff);
            }

            RemoveAllObjectsInRemoveList();

            // Don't unload grids if it's Battleground, since we may have manually added GOs, creatures, those doesn't load from DB at grid re-load !
            // This isn't really bother us, since as soon as we have instanced BG-s, the whole map unloads as the BG gets ended
            if (!IsBattlegroundOrArena())
            {
                for (uint x = 0; x < MapConst.MaxGrids; ++x)
                {
                    for (uint y = 0; y < MapConst.MaxGrids; ++y)
                    {
                        Grid grid = GetGrid(x, y);
                        if (grid != null)
                            grid.Update(this, diff);
                    }
                }
            }
        }

        public void AddObjectToRemoveList(WorldObject obj)
        {
            Cypher.Assert(obj.GetMapId() == GetId() && obj.GetInstanceId() == GetInstanceId());

            obj.CleanupsBeforeDelete(false); // remove or simplify at least cross referenced links

            i_objectsToRemove.Add(obj);
        }

        public void AddObjectToSwitchList(WorldObject obj, bool on)
        {
            Cypher.Assert(obj.GetMapId() == GetId() && obj.GetInstanceId() == GetInstanceId());
            // i_objectsToSwitch is iterated only in Map::RemoveAllObjectsInRemoveList() and it uses
            // the contained objects only if GetTypeId() == TYPEID_UNIT , so we can return in all other cases
            if (!obj.IsTypeId(TypeId.Unit))
                return;

            if (!i_objectsToSwitch.ContainsKey(obj))
                i_objectsToSwitch.Add(obj, on);
            else if (i_objectsToSwitch[obj] != on)
                i_objectsToSwitch.Remove(obj);
            else
                Cypher.Assert(false);
        }

        void RemoveAllObjectsInRemoveList()
        {
            while (!i_objectsToSwitch.Empty())
            {
                KeyValuePair<WorldObject, bool> pair = i_objectsToSwitch.First();
                WorldObject obj = pair.Key;
                bool on = pair.Value;
                i_objectsToSwitch.Remove(pair.Key);

                if (!obj.IsPermanentWorldObject())
                {
                    switch (obj.GetTypeId())
                    {
                        case TypeId.Unit:
                            SwitchGridContainers(obj.ToCreature(), on);
                            break;
                        default:
                            break;
                    }
                }
            }

            while (!i_objectsToRemove.Empty())
            {
                WorldObject obj = i_objectsToRemove.First();

                switch (obj.GetTypeId())
                {
                    case TypeId.Corpse:
                    {
                        Corpse corpse = ObjectAccessor.GetCorpse(obj, obj.GetGUID());
                        if (corpse == null)
                            Log.outError(LogFilter.Maps, "Tried to delete corpse/bones {0} that is not in map.", obj.GetGUID().ToString());
                        else
                            RemoveFromMap(corpse, true);
                        break;
                    }
                    case TypeId.DynamicObject:
                        RemoveFromMap(obj, true);
                        break;
                    case TypeId.AreaTrigger:
                        RemoveFromMap(obj, true);
                        break;
                    case TypeId.Conversation:
                        RemoveFromMap(obj, true);
                        break;
                    case TypeId.GameObject:
                        GameObject go = obj.ToGameObject();
                        Transport transport = go.ToTransport();
                        if (transport)
                            RemoveFromMap(transport, true);
                        else
                            RemoveFromMap(go, true);
                        break;
                    case TypeId.Unit:
                        // in case triggered sequence some spell can continue casting after prev CleanupsBeforeDelete call
                        // make sure that like sources auras/etc removed before destructor start
                        obj.ToCreature().CleanupsBeforeDelete();
                        RemoveFromMap(obj.ToCreature(), true);
                        break;
                    default:
                        Log.outError(LogFilter.Maps, "Non-grid object (TypeId: {0}) is in grid object remove list, ignored.", obj.GetTypeId());
                        break;
                }

                i_objectsToRemove.Remove(obj);
            }
        }

        public uint GetPlayersCountExceptGMs()
        {
            uint count = 0;
            foreach (Player pl in m_activePlayers)
                if (!pl.IsGameMaster())
                    ++count;
            return count;
        }

        public void SendToPlayers(ServerPacket data)
        {
            foreach (Player pl in m_activePlayers)
                pl.SendPacket(data);
        }

        public bool ActiveObjectsNearGrid(Grid grid)
        {
            var cell_min = new CellCoord(grid.GetX() * MapConst.MaxCells,
                grid.GetY() * MapConst.MaxCells);
            var cell_max = new CellCoord(cell_min.X_coord + MapConst.MaxCells,
                cell_min.Y_coord + MapConst.MaxCells);

            //we must find visible range in cells so we unload only non-visible cells...
            float viewDist = GetVisibilityRange();
            uint cell_range = (uint)Math.Ceiling(viewDist / MapConst.SizeofCells) + 1;

            cell_min.Dec_x(cell_range);
            cell_min.Dec_y(cell_range);
            cell_max.Inc_x(cell_range);
            cell_max.Inc_y(cell_range);

            foreach (Player pl in m_activePlayers)
            {
                CellCoord p = GridDefines.ComputeCellCoord(pl.GetPositionX(), pl.GetPositionY());
                if ((cell_min.X_coord <= p.X_coord && p.X_coord <= cell_max.X_coord) &&
                    (cell_min.Y_coord <= p.Y_coord && p.Y_coord <= cell_max.Y_coord))
                    return true;
            }

            foreach (WorldObject obj in m_activeNonPlayers)
            {
                CellCoord p = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
                if ((cell_min.X_coord <= p.X_coord && p.X_coord <= cell_max.X_coord) &&
                    (cell_min.Y_coord <= p.Y_coord && p.Y_coord <= cell_max.Y_coord))
                    return true;
            }

            return false;
        }

        public void AddToActive(WorldObject obj)
        {
            AddToActiveHelper(obj);

            if (obj.IsTypeId(TypeId.Unit))
            {
                Creature c = obj.ToCreature();
                // also not allow unloading spawn grid to prevent creating creature clone at load
                if (!c.IsPet() && c.GetSpawnId() != 0)
                {
                    float x, y;
                    c.GetRespawnPosition(out x, out y, out _);
                    GridCoord p = GridDefines.ComputeGridCoord(x, y);
                    if (GetGrid(p.X_coord, p.Y_coord) != null)
                        GetGrid(p.X_coord, p.Y_coord).IncUnloadActiveLock();
                    else
                    {
                        GridCoord p2 = GridDefines.ComputeGridCoord(c.GetPositionX(), c.GetPositionY());
                        Log.outError(LogFilter.Maps,
                            "Active creature (GUID: {0} Entry: {1}) added to grid[{2}, {3}] but spawn grid[{4}, {5}] was not loaded.",
                            c.GetGUID().ToString(), c.GetEntry(), p.X_coord, p.Y_coord, p2.X_coord, p2.Y_coord);
                    }
                }
            }
        }

        void AddToActiveHelper(WorldObject obj)
        {
            m_activeNonPlayers.Add(obj);
        }

        public void RemoveFromActive(WorldObject obj)
        {
            RemoveFromActiveHelper(obj);

            if (obj.IsTypeId(TypeId.Unit))
            {
                Creature c = obj.ToCreature();
                // also allow unloading spawn grid
                if (!c.IsPet() && c.GetSpawnId() != 0)
                {
                    float x, y;
                    c.GetRespawnPosition(out x, out y, out _);
                    GridCoord p = GridDefines.ComputeGridCoord(x, y);
                    if (GetGrid(p.X_coord, p.Y_coord) != null)
                        GetGrid(p.X_coord, p.Y_coord).DecUnloadActiveLock();
                    else
                    {
                        GridCoord p2 = GridDefines.ComputeGridCoord(c.GetPositionX(), c.GetPositionY());
                        Log.outDebug(LogFilter.Maps,
                            "Active creature (GUID: {0} Entry: {1}) removed from grid[{2}, {3}] but spawn grid[{4}, {5}] was not loaded.",
                            c.GetGUID().ToString(), c.GetEntry(), p.X_coord, p.Y_coord, p2.X_coord, p2.Y_coord);
                    }
                }
            }
        }

        void RemoveFromActiveHelper(WorldObject obj)
        {
            m_activeNonPlayers.Remove(obj);
        }

        public void SaveRespawnTime(SpawnObjectType type, ulong spawnId, uint entry, long respawnTime, uint gridId = 0, SQLTransaction dbTrans = null, bool startup = false)
        {
            SpawnMetadata data = Global.ObjectMgr.GetSpawnMetadata(type, spawnId);
            if (data == null)
            {
                Log.outError(LogFilter.Maps, $"Map {GetId()} attempt to save respawn time for nonexistant spawnid ({type},{spawnId}).");
                return;
            }

            if (respawnTime == 0)
            {
                // Delete only
                RemoveRespawnTime(data.type, data.SpawnId, dbTrans);
                return;
            }

            RespawnInfo ri = new();
            ri.type = data.type;
            ri.spawnId = data.SpawnId;
            ri.entry = entry;
            ri.respawnTime = respawnTime;
            ri.gridId = gridId;
            bool success = AddRespawnInfo(ri);

            if (startup)
            {
                if (!success)
                    Log.outError(LogFilter.Maps, $"Attempt to load saved respawn {respawnTime} for ({type},{spawnId}) failed - duplicate respawn? Skipped.");
            }
            else if (success)
                SaveRespawnInfoDB(ri, dbTrans);
        }

        public void SaveRespawnInfoDB(RespawnInfo info, SQLTransaction dbTrans = null)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_RESPAWN);
            stmt.AddValue(0, (ushort)info.type);
            stmt.AddValue(1, info.spawnId);
            stmt.AddValue(2, info.respawnTime);
            stmt.AddValue(3, GetId());
            stmt.AddValue(4, GetInstanceId());
            DB.Characters.ExecuteOrAppend(dbTrans, stmt);
        }

        public void LoadRespawnTimes()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_RESPAWNS);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                do
                {
                    SpawnObjectType type = (SpawnObjectType)result.Read<ushort>(0);
                    var spawnId = result.Read<ulong>(1);
                    var respawnTime = result.Read<long>(2);

                    if (SpawnData.TypeHasData(type))
                    {
                        SpawnData data = Global.ObjectMgr.GetSpawnData(type, spawnId);
                        if (data != null)
                            SaveRespawnTime(type, spawnId, data.Id, respawnTime, GridDefines.ComputeGridCoord(data.SpawnPoint.GetPositionX(), data.SpawnPoint.GetPositionY()).GetId(), null, true);
                        else
                            Log.outError(LogFilter.Maps, $"Loading saved respawn time of {respawnTime} for spawnid ({type},{spawnId}) - spawn does not exist, ignoring");
                    }
                    else
                        Log.outError(LogFilter.Maps, $"Loading saved respawn time of {respawnTime} for spawnid ({type},{spawnId}) - invalid spawn type, ignoring");

                } while (result.NextRow());
            }
        }

        public void DeleteRespawnTimes()
        {
            UnloadAllRespawnInfos();
            DeleteRespawnTimesInDB(GetId(), GetInstanceId());
        }

        public static void DeleteRespawnTimesInDB(uint mapId, uint instanceId)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_RESPAWNS);
            stmt.AddValue(0, mapId);
            stmt.AddValue(1, instanceId);
            DB.Characters.Execute(stmt);
        }

        public long GetLinkedRespawnTime(ObjectGuid guid)
        {
            ObjectGuid linkedGuid = Global.ObjectMgr.GetLinkedRespawnGuid(guid);
            switch (linkedGuid.GetHigh())
            {
                case HighGuid.Creature:
                    return GetCreatureRespawnTime(linkedGuid.GetCounter());
                case HighGuid.GameObject:
                    return GetGORespawnTime(linkedGuid.GetCounter());
                default:
                    break;
            }

            return 0L;
        }

        public void LoadCorpseData()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CORPSES);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());

            //        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
            // SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, race, class, gender, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
                return;

            MultiMap<ulong, uint> phases = new();
            MultiMap<ulong, ChrCustomizationChoice> customizations = new();

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CORPSE_PHASES);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());

            //        0          1
            // SELECT OwnerGuid, PhaseId FROM corpse_phases cp LEFT JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
            SQLResult phaseResult = DB.Characters.Query(stmt);
            if (!phaseResult.IsEmpty())
            {
                do
                {
                    ulong guid = phaseResult.Read<ulong>(0);
                    uint phaseId = phaseResult.Read<uint>(1);

                    phases.Add(guid, phaseId);

                } while (phaseResult.NextRow());
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CORPSE_CUSTOMIZATIONS);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());

            //        0             1                            2
            // SELECT cc.ownerGuid, cc.chrCustomizationOptionID, cc.chrCustomizationChoiceID FROM corpse_customizations cc LEFT JOIN corpse c ON cc.ownerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
            SQLResult customizationResult = DB.Characters.Query(stmt);
            if (!customizationResult.IsEmpty())
            {
                do
                {
                    ulong guid = customizationResult.Read<ulong>(0);

                    ChrCustomizationChoice choice = new();
                    choice.ChrCustomizationOptionID = customizationResult.Read<uint>(1);
                    choice.ChrCustomizationChoiceID = customizationResult.Read<uint>(2);
                    customizations.Add(guid, choice);

                } while (customizationResult.NextRow());
            }

            do
            {
                CorpseType type = (CorpseType)result.Read<byte>(13);
                ulong guid = result.Read<ulong>(15);
                if (type >= CorpseType.Max || type == CorpseType.Bones)
                {
                    Log.outError(LogFilter.Maps, "Corpse (guid: {0}) have wrong corpse type ({1}), not loading.", guid, type);
                    continue;
                }

                Corpse corpse = new(type);
                if (!corpse.LoadCorpseFromDB(GenerateLowGuid(HighGuid.Corpse), result.GetFields()))
                    continue;

                foreach (var phaseId in phases[guid])
                    PhasingHandler.AddPhase(corpse, phaseId, false);

                corpse.SetCustomizations(customizations[guid]);

                AddCorpse(corpse);
            } while (result.NextRow());
        }

        public void DeleteCorpseData()
        {
            // DELETE cp, c FROM corpse_phases cp INNER JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CORPSES_FROM_MAP);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetInstanceId());
            DB.Characters.Execute(stmt);
        }

        public void AddCorpse(Corpse corpse)
        {
            corpse.SetMap(this);

            _corpsesByCell.Add(corpse.GetCellCoord().GetId(), corpse);
            if (corpse.GetCorpseType() != CorpseType.Bones)
                _corpsesByPlayer[corpse.GetOwnerGUID()] = corpse;
            else
                _corpseBones.Add(corpse);
        }

        void RemoveCorpse(Corpse corpse)
        {
            Cypher.Assert(corpse);

            corpse.DestroyForNearbyPlayers();
            if (corpse.GetCurrentCell() != null)
                RemoveFromMap(corpse, false);
            else
            {
                corpse.RemoveFromWorld();
                corpse.ResetMap();
            }

            _corpsesByCell.Remove(corpse.GetCellCoord().GetId(), corpse);
            if (corpse.GetCorpseType() != CorpseType.Bones)
                _corpsesByPlayer.Remove(corpse.GetOwnerGUID());
            else
                _corpseBones.Remove(corpse);
        }

        public Corpse ConvertCorpseToBones(ObjectGuid ownerGuid, bool insignia = false)
        {
            Corpse corpse = GetCorpseByPlayer(ownerGuid);
            if (!corpse)
                return null;

            RemoveCorpse(corpse);

            // remove corpse from DB
            SQLTransaction trans = new();
            corpse.DeleteFromDB(trans);
            DB.Characters.CommitTransaction(trans);

            Corpse bones = null;

            // create the bones only if the map and the grid is loaded at the corpse's location
            // ignore bones creating option in case insignia
            if ((insignia ||
                (IsBattlegroundOrArena() ? WorldConfig.GetBoolValue(WorldCfg.DeathBonesBgOrArena) : WorldConfig.GetBoolValue(WorldCfg.DeathBonesWorld))) &&
                !IsRemovalGrid(corpse.GetPositionX(), corpse.GetPositionY()))
            {
                // Create bones, don't change Corpse
                bones = new Corpse();
                bones.Create(corpse.GetGUID().GetCounter(), this);

                bones.SetCorpseDynamicFlags((CorpseDynFlags)(byte)corpse.m_corpseData.DynamicFlags);
                bones.SetOwnerGUID(corpse.m_corpseData.Owner);
                bones.SetPartyGUID(corpse.m_corpseData.PartyGUID);
                bones.SetGuildGUID(corpse.m_corpseData.GuildGUID);
                bones.SetDisplayId(corpse.m_corpseData.DisplayID);
                bones.SetRace(corpse.m_corpseData.RaceID);
                bones.SetSex(corpse.m_corpseData.Sex);
                bones.SetClass(corpse.m_corpseData.Class);
                bones.SetCustomizations(corpse.m_corpseData.Customizations);
                bones.SetFlags((CorpseFlags)(corpse.m_corpseData.Flags | (uint)CorpseFlags.Bones));
                bones.SetFactionTemplate(corpse.m_corpseData.FactionTemplate);
                for (int i = 0; i < EquipmentSlot.End; ++i)
                    bones.SetItem((uint)i, corpse.m_corpseData.Items[i]);

                bones.SetCellCoord(corpse.GetCellCoord());
                bones.Relocate(corpse.GetPositionX(), corpse.GetPositionY(), corpse.GetPositionZ(), corpse.GetOrientation());

                PhasingHandler.InheritPhaseShift(bones, corpse);

                AddCorpse(bones);

                // add bones in grid store if grid loaded where corpse placed
                AddToMap(bones);
            }

            // all references to the corpse should be removed at this point
            corpse.Dispose();

            return bones;
        }

        public void RemoveOldCorpses()
        {
            long now = GameTime.GetGameTime();

            List<ObjectGuid> corpses = new();

            foreach (var p in _corpsesByPlayer)
                if (p.Value.IsExpired(now))
                    corpses.Add(p.Key);

            foreach (ObjectGuid ownerGuid in corpses)
                ConvertCorpseToBones(ownerGuid);

            List<Corpse> expiredBones = new();
            foreach (Corpse bones in _corpseBones)
                if (bones.IsExpired(now))
                    expiredBones.Add(bones);

            foreach (Corpse bones in expiredBones)
            {
                RemoveCorpse(bones);
                bones.Dispose();
            }
        }

        public void SendZoneDynamicInfo(uint zoneId, Player player)
        {
            var zoneInfo = _zoneDynamicInfo.LookupByKey(zoneId);
            if (zoneInfo == null)
                return;

            uint music = zoneInfo.MusicId;
            if (music != 0)
                player.SendPacket(new PlayMusic(music));

            SendZoneWeather(zoneInfo, player);

            foreach (var lightOverride in zoneInfo.LightOverrides)
            {
                OverrideLight overrideLight = new();
                overrideLight.AreaLightID = lightOverride.AreaLightId;
                overrideLight.OverrideLightID = lightOverride.OverrideLightId;
                overrideLight.TransitionMilliseconds = lightOverride.TransitionMilliseconds;
                player.SendPacket(overrideLight);
            }
        }

        public void SendZoneWeather(uint zoneId, Player player)
        {
            if (!player.HasAuraType(AuraType.ForceWeather))
            {
                var zoneInfo = _zoneDynamicInfo.LookupByKey(zoneId);
                if (zoneInfo == null)
                    return;

                SendZoneWeather(zoneInfo, player);
            }
        }

        void SendZoneWeather(ZoneDynamicInfo zoneDynamicInfo, Player player)
        {
            WeatherState weatherId = zoneDynamicInfo.WeatherId;
            if (weatherId != 0)
            {
                WeatherPkt weather = new(weatherId, zoneDynamicInfo.Intensity);
                player.SendPacket(weather);
            }
            else if (zoneDynamicInfo.DefaultWeather != null)
            {
                zoneDynamicInfo.DefaultWeather.SendWeatherUpdateToPlayer(player);
            }
            else
                Weather.SendFineWeatherUpdateToPlayer(player);
        }

        public void SetZoneMusic(uint zoneId, uint musicId)
        {
            if (!_zoneDynamicInfo.ContainsKey(zoneId))
                _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

            _zoneDynamicInfo[zoneId].MusicId = musicId;

            var players = GetPlayers();
            if (!players.Empty())
            {
                PlayMusic playMusic = new(musicId);

                foreach (var player in players)
                    if (player.GetZoneId() == zoneId && !player.HasAuraType(AuraType.ForceWeather))
                        player.SendPacket(playMusic);
            }
        }

        public Weather GetOrGenerateZoneDefaultWeather(uint zoneId)
        {
            WeatherData weatherData = Global.WeatherMgr.GetWeatherData(zoneId);
            if (weatherData == null)
                return null;

            if (!_zoneDynamicInfo.ContainsKey(zoneId))
                _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

            ZoneDynamicInfo info = _zoneDynamicInfo[zoneId];
            if (info.DefaultWeather == null)
            {
                info.DefaultWeather = new Weather(zoneId, weatherData);
                info.DefaultWeather.ReGenerate();
                info.DefaultWeather.UpdateWeather();
            }

            return info.DefaultWeather;
        }

        public WeatherState GetZoneWeather(uint zoneId)
        {
            ZoneDynamicInfo zoneDynamicInfo = _zoneDynamicInfo.LookupByKey(zoneId);
            if (zoneDynamicInfo != null)
            {
                if (zoneDynamicInfo.WeatherId != 0)
                    return zoneDynamicInfo.WeatherId;

                if (zoneDynamicInfo.DefaultWeather != null)
                    return zoneDynamicInfo.DefaultWeather.GetWeatherState();
            }

            return WeatherState.Fine;
        }

        public void SetZoneWeather(uint zoneId, WeatherState weatherId, float intensity)
        {
            if (!_zoneDynamicInfo.ContainsKey(zoneId))
                _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

            ZoneDynamicInfo info = _zoneDynamicInfo[zoneId];
            info.WeatherId = weatherId;
            info.Intensity = intensity;

            var players = GetPlayers();
            if (!players.Empty())
            {
                WeatherPkt weather = new(weatherId, intensity);

                foreach (var player in players)
                {
                    if (player.GetZoneId() == zoneId)
                        player.SendPacket(weather);
                }
            }
        }

        public void SetZoneOverrideLight(uint zoneId, uint areaLightId, uint overrideLightId, uint transitionMilliseconds)
        {
            if (!_zoneDynamicInfo.ContainsKey(zoneId))
                _zoneDynamicInfo[zoneId] = new ZoneDynamicInfo();

            ZoneDynamicInfo info = _zoneDynamicInfo[zoneId];
            // client can support only one override for each light (zone independent)
            info.LightOverrides.RemoveAll(lightOverride => lightOverride.AreaLightId == areaLightId);

            // set new override (if any)
            if (overrideLightId != 0)
            {
                ZoneDynamicInfo.LightOverride lightOverride = new();
                lightOverride.AreaLightId = areaLightId;
                lightOverride.OverrideLightId = overrideLightId;
                lightOverride.TransitionMilliseconds = transitionMilliseconds;
                info.LightOverrides.Add(lightOverride);
            }

            var players = GetPlayers();

            if (!players.Empty())
            {
                OverrideLight overrideLight = new();
                overrideLight.AreaLightID = areaLightId;
                overrideLight.OverrideLightID = overrideLightId;
                overrideLight.TransitionMilliseconds = transitionMilliseconds;

                foreach (var player in players)
                    if (player.GetZoneId() == zoneId)
                        player.SendPacket(overrideLight);
            }
        }

        public void UpdateAreaDependentAuras()
        {
            var players = GetPlayers();
            foreach (var player in players)
            {
                if (player)
                {
                    if (player.IsInWorld)
                    {
                        player.UpdateAreaDependentAuras(player.GetAreaId());
                        player.UpdateZoneDependentAuras(player.GetZoneId());
                    }
                }
            }
        }

        public virtual string GetDebugInfo()
        {
            return $"Id: {GetId()} InstanceId: {GetInstanceId()} Difficulty: {GetDifficultyID()} HasPlayers: {HavePlayers()}";
        }
        
        public MapRecord GetEntry()
        {
            return i_mapRecord;
        }

        public bool CanUnload(uint diff)
        {
            if (m_unloadTimer == 0)
                return false;

            if (m_unloadTimer <= diff)
                return true;

            m_unloadTimer -= diff;
            return false;
        }

        public float GetVisibilityRange()
        {
            return m_VisibleDistance;
        }

        public bool IsRemovalGrid(float x, float y)
        {
            GridCoord p = GridDefines.ComputeGridCoord(x, y);
            return GetGrid(p.X_coord, p.Y_coord) == null ||
                   GetGrid(p.X_coord, p.Y_coord).GetGridState() == GridState.Removal;
        }
        public bool IsRemovalGrid(Position pos) { return IsRemovalGrid(pos.GetPositionX(), pos.GetPositionY()); }

        private bool GetUnloadLock(GridCoord p)
        {
            return GetGrid(p.X_coord, p.Y_coord).GetUnloadLock();
        }

        void SetUnloadLock(GridCoord p, bool on)
        {
            GetGrid(p.X_coord, p.Y_coord).SetUnloadExplicitLock(on);
        }

        public void ResetGridExpiry(Grid grid, float factor = 1)
        {
            grid.ResetTimeTracker((long)(i_gridExpiry * factor));
        }

        public long GetGridExpiry()
        {
            return i_gridExpiry;
        }

        public void AddChildTerrainMap(Map map)
        {
            m_childTerrainMaps.Add(map);
            map.m_parentTerrainMap = this;
        }

        public void UnlinkAllChildTerrainMaps() { m_childTerrainMaps.Clear(); }

        public uint GetInstanceId()
        {
            return i_InstanceId;
        }

        public virtual EnterState CannotEnter(Player player) { return EnterState.CanEnter; }

        public Difficulty GetDifficultyID()
        {
            return i_spawnMode;
        }

        public MapDifficultyRecord GetMapDifficulty()
        {
            return Global.DB2Mgr.GetMapDifficultyData(GetId(), GetDifficultyID());
        }

        public ItemContext GetDifficultyLootItemContext()
        {
            MapDifficultyRecord mapDifficulty = GetMapDifficulty();
            if (mapDifficulty != null && mapDifficulty.ItemContext != 0)
                return (ItemContext)mapDifficulty.ItemContext;

            DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(GetDifficultyID());
            if (difficulty != null)
                return (ItemContext)difficulty.ItemContext;

            return ItemContext.None;
        }

        public uint GetId()
        {
            return i_mapRecord.Id;
        }

        public bool Instanceable()
        {
            return i_mapRecord != null && i_mapRecord.Instanceable();
        }

        public bool IsDungeon()
        {
            return i_mapRecord != null && i_mapRecord.IsDungeon();
        }

        public bool IsNonRaidDungeon()
        {
            return i_mapRecord != null && i_mapRecord.IsNonRaidDungeon();
        }

        public bool IsRaid()
        {
            return i_mapRecord != null && i_mapRecord.IsRaid();
        }

        public bool IsRaidOrHeroicDungeon()
        {
            return IsRaid() || IsHeroic();
        }

        public bool IsHeroic()
        {
            DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(i_spawnMode);
            if (difficulty != null)
                return difficulty.Flags.HasAnyFlag(DifficultyFlags.Heroic);
            return false;
        }

        public bool Is25ManRaid()
        {
            // since 25man difficulties are 1 and 3, we can check them like that
            return IsRaid() && (i_spawnMode == Difficulty.Raid25N || i_spawnMode == Difficulty.Raid25HC);
        }

        public bool IsBattleground()
        {
            return i_mapRecord != null && i_mapRecord.IsBattleground();
        }

        public bool IsBattleArena()
        {
            return i_mapRecord != null && i_mapRecord.IsBattleArena();
        }

        public bool IsBattlegroundOrArena()
        {
            return i_mapRecord != null && i_mapRecord.IsBattlegroundOrArena();
        }

        public bool IsGarrison()
        {
            return i_mapRecord != null && i_mapRecord.IsGarrison();
        }

        private bool GetEntrancePos(out uint mapid, out float x, out float y)
        {
            mapid = 0;
            x = 0;
            y = 0;

            if (i_mapRecord == null)
                return false;

            return i_mapRecord.GetEntrancePos(out mapid, out x, out y);
        }

        void ResetMarkedCells()
        {
            marked_cells.SetAll(false);
        }

        private bool IsCellMarked(uint pCellId)
        {
            return marked_cells.Get((int)pCellId);
        }

        void MarkCell(uint pCellId)
        {
            marked_cells.Set((int)pCellId, true);
        }

        public bool HavePlayers()
        {
            return !m_activePlayers.Empty();
        }

        public void AddWorldObject(WorldObject obj)
        {
            i_worldObjects.Add(obj);
        }

        public void RemoveWorldObject(WorldObject obj)
        {
            i_worldObjects.Remove(obj);
        }

        public List<Player> GetPlayers()
        {
            return m_activePlayers;
        }

        public Dictionary<ObjectGuid, WorldObject> GetObjectsStore() { return _objectsStore; }

        public MultiMap<ulong, Creature> GetCreatureBySpawnIdStore() { return _creatureBySpawnIdStore; }

        public MultiMap<ulong, GameObject> GetGameObjectBySpawnIdStore() { return _gameobjectBySpawnIdStore; }

        public MultiMap<ulong, AreaTrigger> GetAreaTriggerBySpawnIdStore() { return _areaTriggerBySpawnIdStore; }

        public List<Corpse> GetCorpsesInCell(uint cellId)
        {
            return _corpsesByCell.LookupByKey(cellId);
        }

        public Corpse GetCorpseByPlayer(ObjectGuid ownerGuid)
        {
            return _corpsesByPlayer.LookupByKey(ownerGuid);
        }

        public MapInstanced ToMapInstanced() { return Instanceable() ? (this as MapInstanced) : null; }
        public InstanceMap ToInstanceMap() { return IsDungeon() ? (this as InstanceMap) : null; }
        public BattlegroundMap ToBattlegroundMap() { return IsBattlegroundOrArena() ? (this as BattlegroundMap) : null; }

        public void Balance()
        {
            _dynamicTree.Balance();
        }

        public void RemoveGameObjectModel(GameObjectModel model)
        {
            _dynamicTree.Remove(model);
        }

        public void InsertGameObjectModel(GameObjectModel model)
        {
            _dynamicTree.Insert(model);
        }

        public bool ContainsGameObjectModel(GameObjectModel model)
        {
            return _dynamicTree.Contains(model);
        }

        public float GetGameObjectFloor(PhaseShift phaseShift, float x, float y, float z, float maxSearchDist = MapConst.DefaultHeightSearch)
        {
            return _dynamicTree.GetHeight(x, y, z, maxSearchDist, phaseShift);
        }

        public virtual uint GetOwnerGuildId(Team team = Team.Other)
        {
            return 0;
        }

        public long GetRespawnTime(SpawnObjectType type, ulong spawnId)
        {
            var map = GetRespawnMapForType(type);
            if (map != null)
            {
                var respawnInfo = map.LookupByKey(spawnId);
                return (respawnInfo == null) ? 0 : respawnInfo.respawnTime;
            }
            return 0;
        }

        public long GetCreatureRespawnTime(ulong spawnId) { return GetRespawnTime(SpawnObjectType.Creature, spawnId); }

        public long GetGORespawnTime(ulong spawnId) { return GetRespawnTime(SpawnObjectType.GameObject, spawnId); }
        
        void SetTimer(uint t)
        {
            i_gridExpiry = t < MapConst.MinGridDelay ? MapConst.MinGridDelay : t;
        }

        private Grid GetGrid(uint x, uint y)
        {
            if (x > MapConst.MaxGrids || y > MapConst.MaxGrids)
                return null;

            return i_grids[x][y];
        }

        private bool IsGridObjectDataLoaded(uint x, uint y)
        {
            return GetGrid(x, y).IsGridObjectDataLoaded();
        }

        void SetGridObjectDataLoaded(bool pLoaded, uint x, uint y)
        {
            GetGrid(x, y).SetGridObjectDataLoaded(pLoaded);
        }

        public AreaTrigger GetAreaTrigger(ObjectGuid guid)
        {
            if (!guid.IsAreaTrigger())
                return null;

            return (AreaTrigger)_objectsStore.LookupByKey(guid);
        }

        public SceneObject GetSceneObject(ObjectGuid guid)
        {
            return _objectsStore.LookupByKey(guid) as SceneObject;
        }
        
        public Conversation GetConversation(ObjectGuid guid)
        {
            return (Conversation)_objectsStore.LookupByKey(guid);
        }

        public Player GetPlayer(ObjectGuid guid)
        {
            return Global.ObjAccessor.GetPlayer(this, guid);
        }

        public Corpse GetCorpse(ObjectGuid guid)
        {
            if (!guid.IsCorpse())
                return null;

            return (Corpse)_objectsStore.LookupByKey(guid);
        }

        public Creature GetCreature(ObjectGuid guid)
        {
            if (!guid.IsCreatureOrVehicle())
                return null;

            return (Creature)_objectsStore.LookupByKey(guid);
        }

        public DynamicObject GetDynamicObject(ObjectGuid guid)
        {
            if (!guid.IsDynamicObject())
                return null;

            return (DynamicObject)_objectsStore.LookupByKey(guid);
        }

        public GameObject GetGameObject(ObjectGuid guid)
        {
            if (!guid.IsAnyTypeGameObject())
                return null;

            return (GameObject)_objectsStore.LookupByKey(guid);
        }

        public Pet GetPet(ObjectGuid guid)
        {
            if (!guid.IsPet())
                return null;

            return (Pet)_objectsStore.LookupByKey(guid);
        }

        public Transport GetTransport(ObjectGuid guid)
        {
            if (!guid.IsMOTransport())
                return null;

            GameObject go = GetGameObject(guid);
            return go ? go.ToTransport() : null;
        }

        public Creature GetCreatureBySpawnId(ulong spawnId)
        {
            var bounds = GetCreatureBySpawnIdStore().LookupByKey(spawnId);
            if (bounds.Empty())
                return null;

            var foundCreature = bounds.Find(creature => creature.IsAlive());

            return foundCreature != null ? foundCreature : bounds[0];
        }

        public GameObject GetGameObjectBySpawnId(ulong spawnId)
        {
            var bounds = GetGameObjectBySpawnIdStore().LookupByKey(spawnId);
            if (bounds.Empty())
                return null;

            var foundGameObject = bounds.Find(gameobject => gameobject.IsSpawned());

            return foundGameObject != null ? foundGameObject : bounds[0];
        }

        public AreaTrigger GetAreaTriggerBySpawnId(ulong spawnId)
        {
            var bounds = GetAreaTriggerBySpawnIdStore().LookupByKey(spawnId);
            if (bounds.Empty())
                return null;

            return bounds.FirstOrDefault();
        }

        public WorldObject GetWorldObjectBySpawnId(SpawnObjectType type, ulong spawnId)
        {
            switch (type)
            {
                case SpawnObjectType.Creature:
                    return GetCreatureBySpawnId(spawnId);
                case SpawnObjectType.GameObject:
                    return GetGameObjectBySpawnId(spawnId);
                case SpawnObjectType.AreaTrigger:
                    return GetAreaTriggerBySpawnId(spawnId);
                default:
                    return null;
            }
        }

        public void Visit(Cell cell, Visitor visitor)
        {
            uint x = cell.GetGridX();
            uint y = cell.GetGridY();
            uint cell_x = cell.GetCellX();
            uint cell_y = cell.GetCellY();

            if (!cell.NoCreate() || IsGridLoaded(new GridCoord(x, y)))
            {
                EnsureGridLoaded(cell);
                GetGrid(x, y).VisitGrid(cell_x, cell_y, visitor);
            }
        }

        public TempSummon SummonCreature(uint entry, Position pos, SummonPropertiesRecord properties = null, uint duration = 0, WorldObject summoner = null, uint spellId = 0, uint vehId = 0, ObjectGuid privateObjectOwner = default)
        {
            var mask = UnitTypeMask.Summon;
            if (properties != null)
            {
                switch (properties.Control)
                {
                    case SummonCategory.Pet:
                        mask = UnitTypeMask.Guardian;
                        break;
                    case SummonCategory.Puppet:
                        mask = UnitTypeMask.Puppet;
                        break;
                    case SummonCategory.Vehicle:
                        mask = UnitTypeMask.Minion;
                        break;
                    case SummonCategory.Wild:
                    case SummonCategory.Ally:
                    case SummonCategory.Unk:
                    {
                        switch (properties.Title)
                        {
                            case SummonTitle.Minion:
                            case SummonTitle.Guardian:
                            case SummonTitle.Runeblade:
                                mask = UnitTypeMask.Guardian;
                                break;
                            case SummonTitle.Totem:
                            case SummonTitle.LightWell:
                                mask = UnitTypeMask.Totem;
                                break;
                            case SummonTitle.Vehicle:
                            case SummonTitle.Mount:
                                mask = UnitTypeMask.Summon;
                                break;
                            case SummonTitle.Companion:
                                mask = UnitTypeMask.Minion;
                                break;
                            default:
                                if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup)) // Mirror Image, Summon Gargoyle
                                    mask = UnitTypeMask.Guardian;
                                break;
                        }
                        break;
                    }
                    default:
                        return null;
                }
            }

            Unit summonerUnit = summoner != null ? summoner.ToUnit() : null;

            TempSummon summon;
            switch (mask)
            {
                case UnitTypeMask.Summon:
                    summon = new TempSummon(properties, summonerUnit, false);
                    break;
                case UnitTypeMask.Guardian:
                    summon = new Guardian(properties, summonerUnit, false);
                    break;
                case UnitTypeMask.Puppet:
                    summon = new Puppet(properties, summonerUnit);
                    break;
                case UnitTypeMask.Totem:
                    summon = new Totem(properties, summonerUnit);
                    break;
                case UnitTypeMask.Minion:
                    summon = new Minion(properties, summonerUnit, false);
                    break;
                default:
                    return null;
            }

            if (!summon.Create(GenerateLowGuid(HighGuid.Creature), this, entry, pos, null, vehId, true))
                return null;

            // Set the summon to the summoner's phase
            if (summoner != null && !(properties != null && properties.GetFlags().HasFlag(SummonPropertiesFlags.IgnoreSummonerPhase)))
                PhasingHandler.InheritPhaseShift(summon, summoner);

            summon.SetCreatedBySpell(spellId);
            summon.SetHomePosition(pos);
            summon.InitStats(duration);
            summon.SetPrivateObjectOwner(privateObjectOwner);

            AddToMap(summon.ToCreature());
            summon.InitSummon();

            // call MoveInLineOfSight for nearby creatures
            AIRelocationNotifier notifier = new(summon);
            Cell.VisitAllObjects(summon, notifier, GetVisibilityRange());

            return summon;
        }

        public ulong GenerateLowGuid(HighGuid high)
        {
            return GetGuidSequenceGenerator(high).Generate();
        }

        public ulong GetMaxLowGuid(HighGuid high)
        {
            return GetGuidSequenceGenerator(high).GetNextAfterMaxUsed();
        }

        ObjectGuidGenerator GetGuidSequenceGenerator(HighGuid high)
        {
            if (!_guidGenerators.ContainsKey(high))
                _guidGenerators[high] = new ObjectGuidGenerator(high);

            return _guidGenerators[high];
        }

        public void AddUpdateObject(WorldObject obj)
        {
            _updateObjects.Add(obj);
        }

        public void RemoveUpdateObject(WorldObject obj)
        {
            _updateObjects.Remove(obj);
        }

        public static implicit operator bool(Map map)
        {
            return map != null;
        }

        public MultiPersonalPhaseTracker GetMultiPersonalPhaseTracker() { return _multiPersonalPhaseTracker; }

        #region Scripts

        // Put scripts in the execution queue
        public void ScriptsStart(ScriptsType scriptsType, uint id, WorldObject source, WorldObject target)
        {
            var scripts = Global.ObjectMgr.GetScriptsMapByType(scriptsType);

            // Find the script map
            MultiMap<uint, ScriptInfo> list = scripts.LookupByKey(id);
            if (list == null)
                return;

            // prepare static data
            ObjectGuid sourceGUID = source != null ? source.GetGUID() : ObjectGuid.Empty; //some script commands doesn't have source
            ObjectGuid targetGUID = target != null ? target.GetGUID() : ObjectGuid.Empty;
            ObjectGuid ownerGUID = (source != null && source.IsTypeMask(TypeMask.Item)) ? ((Item)source).GetOwnerGUID() : ObjectGuid.Empty;

            // Schedule script execution for all scripts in the script map
            bool immedScript = false;
            foreach (var script in list)
            {
                ScriptAction sa;
                sa.sourceGUID = sourceGUID;
                sa.targetGUID = targetGUID;
                sa.ownerGUID = ownerGUID;

                sa.script = script.Value;
                m_scriptSchedule.Add(GameTime.GetGameTime() + script.Key, sa);
                if (script.Key == 0)
                    immedScript = true;

                Global.MapMgr.IncreaseScheduledScriptsCount();
            }
            // If one of the effects should be immediate, launch the script execution
            if (immedScript && !i_scriptLock)
            {
                i_scriptLock = true;
                ScriptsProcess();
                i_scriptLock = false;
            }
        }

        public void ScriptCommandStart(ScriptInfo script, uint delay, WorldObject source, WorldObject target)
        {
            // NOTE: script record _must_ exist until command executed

            // prepare static data
            ObjectGuid sourceGUID = source != null ? source.GetGUID() : ObjectGuid.Empty;
            ObjectGuid targetGUID = target != null ? target.GetGUID() : ObjectGuid.Empty;
            ObjectGuid ownerGUID = (source != null && source.IsTypeMask(TypeMask.Item)) ? ((Item)source).GetOwnerGUID() : ObjectGuid.Empty;

            var sa = new ScriptAction();
            sa.sourceGUID = sourceGUID;
            sa.targetGUID = targetGUID;
            sa.ownerGUID = ownerGUID;

            sa.script = script;
            m_scriptSchedule.Add(GameTime.GetGameTime() + delay, sa);

            Global.MapMgr.IncreaseScheduledScriptsCount();

            // If effects should be immediate, launch the script execution
            if (delay == 0 && !i_scriptLock)
            {
                i_scriptLock = true;
                ScriptsProcess();
                i_scriptLock = false;
            }
        }

        // Helpers for ScriptProcess method.
        private Player _GetScriptPlayerSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo)
        {
            Player player = null;
            if (source == null && target == null)
                Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", scriptInfo.GetDebugInfo());
            else
            {
                // Check target first, then source.
                if (target != null)
                    player = target.ToPlayer();
                if (player == null && source != null)
                    player = source.ToPlayer();

                if (player == null)
                    Log.outError(LogFilter.Scripts, "{0} neither source nor target object is player (source: TypeId: {1}, Entry: {2}, {3}; target: TypeId: {4}, Entry: {5}, {6}), skipping.",
                        scriptInfo.GetDebugInfo(), source ? source.GetTypeId() : 0, source ? source.GetEntry() : 0, source ? source.GetGUID().ToString() : "",
                        target ? target.GetTypeId() : 0, target ? target.GetEntry() : 0, target ? target.GetGUID().ToString() : "");
            }
            return player;
        }

        private Creature _GetScriptCreatureSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo, bool bReverse = false)
        {
            Creature creature = null;
            if (source == null && target == null)
                Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", scriptInfo.GetDebugInfo());
            else
            {
                if (bReverse)
                {
                    // Check target first, then source.
                    if (target != null)
                        creature = target.ToCreature();
                    if (creature == null && source != null)
                        creature = source.ToCreature();
                }
                else
                {
                    // Check source first, then target.
                    if (source != null)
                        creature = source.ToCreature();
                    if (creature == null && target != null)
                        creature = target.ToCreature();
                }

                if (creature == null)
                    Log.outError(LogFilter.Scripts, "{0} neither source nor target are creatures (source: TypeId: {1}, Entry: {2}, {3}; target: TypeId: {4}, Entry: {5}, {6}), skipping.",
                        scriptInfo.GetDebugInfo(), source ? source.GetTypeId() : 0, source ? source.GetEntry() : 0, source ? source.GetGUID().ToString() : "",
                        target ? target.GetTypeId() : 0, target ? target.GetEntry() : 0, target ? target.GetGUID().ToString() : "");
            }
            return creature;
        }

        private GameObject _GetScriptGameObjectSourceOrTarget(WorldObject source, WorldObject target, ScriptInfo scriptInfo, bool bReverse)
        {
            GameObject gameobject = null;
            if (source == null && target == null)
                Log.outError(LogFilter.MapsScript, $"{scriptInfo.GetDebugInfo()} source and target objects are NULL.");
            else
            {
                if (bReverse)
                {
                    // Check target first, then source.
                    if (target != null)
                        gameobject = target.ToGameObject();
                    if (gameobject == null && source != null)
                        gameobject = source.ToGameObject();
                }
                else
                {
                    // Check source first, then target.
                    if (source != null)
                        gameobject = source.ToGameObject();
                    if (gameobject == null && target != null)
                        gameobject = target.ToGameObject();
                }

                if (gameobject == null)
                    Log.outError(LogFilter.MapsScript, $"{scriptInfo.GetDebugInfo()} neither source nor target are gameobjects " +
                        $"(source: TypeId: {(source != null ? source.GetTypeId() : 0)}, Entry: {(source != null ? source.GetEntry() : 0)}, {(source != null ? source.GetGUID() : ObjectGuid.Empty)}; " +
                        $"target: TypeId: {(target != null ? target.GetTypeId() : 0)}, Entry: {(target != null ? target.GetEntry() : 0)}, {(target != null ? target.GetGUID() : ObjectGuid.Empty)}), skipping.");
            }
            return gameobject;
        }
        
        private Unit _GetScriptUnit(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
        {
            Unit unit = null;
            if (obj == null)
                Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(),
                    isSource ? "source" : "target");
            else if (!obj.IsTypeMask(TypeMask.Unit))
                Log.outError(LogFilter.Scripts,
                    "{0} {1} object is not unit (TypeId: {2}, Entry: {3}, GUID: {4}), skipping.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target", obj.GetTypeId(), obj.GetEntry(), obj.GetGUID().ToString());
            else
            {
                unit = obj.ToUnit();
                if (unit == null)
                    Log.outError(LogFilter.Scripts, "{0} {1} object could not be casted to unit.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
            }
            return unit;
        }

        private Player _GetScriptPlayer(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
        {
            Player player = null;
            if (obj == null)
                Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(),
                    isSource ? "source" : "target");
            else
            {
                player = obj.ToPlayer();
                if (player == null)
                    Log.outError(LogFilter.Scripts, "{0} {1} object is not a player (TypeId: {2}, Entry: {3}, GUID: {4}).",
                        scriptInfo.GetDebugInfo(), isSource ? "source" : "target", obj.GetTypeId(), obj.GetEntry(), obj.GetGUID().ToString());
            }
            return player;
        }

        private Creature _GetScriptCreature(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
        {
            Creature creature = null;
            if (obj == null)
                Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
            else
            {
                creature = obj.ToCreature();
                if (creature == null)
                    Log.outError(LogFilter.Scripts,
                        "{0} {1} object is not a creature (TypeId: {2}, Entry: {3}, GUID: {4}).", scriptInfo.GetDebugInfo(), isSource ? "source" : "target", obj.GetTypeId(), obj.GetEntry(), obj.GetGUID().ToString());
            }
            return creature;
        }

        private WorldObject _GetScriptWorldObject(WorldObject obj, bool isSource, ScriptInfo scriptInfo)
        {
            WorldObject pWorldObject = null;
            if (obj == null)
                Log.outError(LogFilter.Scripts, "{0} {1} object is NULL.", scriptInfo.GetDebugInfo(), isSource ? "source" : "target");
            else
            {
                pWorldObject = obj;
                if (pWorldObject == null)
                    Log.outError(LogFilter.Scripts,
                        "{0} {1} object is not a world object (TypeId: {2}, Entry: {3}, GUID: {4}).", scriptInfo.GetDebugInfo(), isSource ? "source" : "target", obj.GetTypeId(), obj.GetEntry(), obj.GetGUID().ToString());
            }
            return pWorldObject;
        }

        void _ScriptProcessDoor(WorldObject source, WorldObject target, ScriptInfo scriptInfo)
        {
            bool bOpen = false;
            ulong guid = scriptInfo.ToggleDoor.GOGuid;
            int nTimeToToggle = Math.Max(15, (int)scriptInfo.ToggleDoor.ResetDelay);
            switch (scriptInfo.command)
            {
                case ScriptCommands.OpenDoor:
                    bOpen = true;
                    break;
                case ScriptCommands.CloseDoor:
                    break;
                default:
                    Log.outError(LogFilter.Scripts, "{0} unknown command for _ScriptProcessDoor.", scriptInfo.GetDebugInfo());
                    return;
            }
            if (guid == 0)
                Log.outError(LogFilter.Scripts, "{0} door guid is not specified.", scriptInfo.GetDebugInfo());
            else if (source == null)
                Log.outError(LogFilter.Scripts, "{0} source object is NULL.", scriptInfo.GetDebugInfo());
            else if (!source.IsTypeMask(TypeMask.Unit))
                Log.outError(LogFilter.Scripts,
                    "{0} source object is not unit (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.", scriptInfo.GetDebugInfo(), source.GetTypeId(), source.GetEntry(), source.GetGUID().ToString());
            else
            {
                if (source == null)
                    Log.outError(LogFilter.Scripts,
                        "{0} source object could not be casted to world object (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.", scriptInfo.GetDebugInfo(), source.GetTypeId(), source.GetEntry(), source.GetGUID().ToString());
                else
                {
                    GameObject pDoor = _FindGameObject(source, guid);
                    if (pDoor == null)
                        Log.outError(LogFilter.Scripts, "{0} gameobject was not found (guid: {1}).", scriptInfo.GetDebugInfo(), guid);
                    else if (pDoor.GetGoType() != GameObjectTypes.Door)
                        Log.outError(LogFilter.Scripts, "{0} gameobject is not a door (GoType: {1}, Entry: {2}, GUID: {3}).", scriptInfo.GetDebugInfo(), pDoor.GetGoType(), pDoor.GetEntry(), pDoor.GetGUID().ToString());
                    else if (bOpen == (pDoor.GetGoState() == GameObjectState.Ready))
                    {
                        pDoor.UseDoorOrButton((uint)nTimeToToggle);

                        if (target != null && target.IsTypeMask(TypeMask.GameObject))
                        {
                            GameObject goTarget = target.ToGameObject();
                            if (goTarget != null && goTarget.GetGoType() == GameObjectTypes.Button)
                                goTarget.UseDoorOrButton((uint)nTimeToToggle);
                        }
                    }
                }
            }
        }

        private GameObject _FindGameObject(WorldObject searchObject, ulong guid)
        {
            var bounds = searchObject.GetMap().GetGameObjectBySpawnIdStore().LookupByKey(guid);
            if (bounds.Empty())
                return null;

            return bounds[0];
        }

        // Process queued scripts
        void ScriptsProcess()
        {
            if (m_scriptSchedule.Empty())
                return;

            // Process overdue queued scripts
            KeyValuePair<long, ScriptAction> iter = m_scriptSchedule.First();
            while (!m_scriptSchedule.Empty() && (iter.Key <= GameTime.GetGameTime()))
            {
                ScriptAction step = iter.Value;

                WorldObject source = null;
                if (!step.sourceGUID.IsEmpty())
                {
                    switch (step.sourceGUID.GetHigh())
                    {
                        case HighGuid.Item: // as well as HIGHGUID_CONTAINER
                            Player player = GetPlayer(step.ownerGUID);
                            if (player != null)
                                source = player.GetItemByGuid(step.sourceGUID);
                            break;
                        case HighGuid.Creature:
                        case HighGuid.Vehicle:
                            source = GetCreature(step.sourceGUID);
                            break;
                        case HighGuid.Pet:
                            source = GetPet(step.sourceGUID);
                            break;
                        case HighGuid.Player:
                            source = GetPlayer(step.sourceGUID);
                            break;
                        case HighGuid.GameObject:
                        case HighGuid.Transport:
                            source = GetGameObject(step.sourceGUID);
                            break;
                        case HighGuid.Corpse:
                            source = GetCorpse(step.sourceGUID);
                            break;
                        default:
                            Log.outError(LogFilter.Scripts, "{0} source with unsupported high guid (GUID: {1}, high guid: {2}).",
                                step.script.GetDebugInfo(), step.sourceGUID, step.sourceGUID.ToString());
                            break;
                    }
                }

                WorldObject target = null;
                if (!step.targetGUID.IsEmpty())
                {
                    switch (step.targetGUID.GetHigh())
                    {
                        case HighGuid.Creature:
                        case HighGuid.Vehicle:
                            target = GetCreature(step.targetGUID);
                            break;
                        case HighGuid.Pet:
                            target = GetPet(step.targetGUID);
                            break;
                        case HighGuid.Player:
                            target = GetPlayer(step.targetGUID);
                            break;
                        case HighGuid.GameObject:
                        case HighGuid.Transport:
                            target = GetGameObject(step.targetGUID);
                            break;
                        case HighGuid.Corpse:
                            target = GetCorpse(step.targetGUID);
                            break;
                        default:
                            Log.outError(LogFilter.Scripts, "{0} target with unsupported high guid {1}.", step.script.GetDebugInfo(), step.targetGUID.ToString());
                            break;
                    }
                }

                switch (step.script.command)
                {
                    case ScriptCommands.Talk:
                    {
                        if (step.script.Talk.ChatType > ChatMsg.Whisper && step.script.Talk.ChatType != ChatMsg.RaidBossWhisper)
                        {
                            Log.outError(LogFilter.Scripts, "{0} invalid chat type ({1}) specified, skipping.",
                                step.script.GetDebugInfo(), step.script.Talk.ChatType);
                            break;
                        }

                        if (step.script.Talk.Flags.HasAnyFlag(eScriptFlags.TalkUsePlayer))
                            source = _GetScriptPlayerSourceOrTarget(source, target, step.script);
                        else
                            source = _GetScriptCreatureSourceOrTarget(source, target, step.script);

                        if (source)
                        {
                            Unit sourceUnit = source.ToUnit();
                            if (!sourceUnit)
                            {
                                Log.outError(LogFilter.Scripts, "{0} source object ({1}) is not an unit, skipping.", step.script.GetDebugInfo(), source.GetGUID().ToString());
                                break;
                            }

                            switch (step.script.Talk.ChatType)
                            {
                                case ChatMsg.Say:
                                    sourceUnit.Say((uint)step.script.Talk.TextID, target);
                                    break;
                                case ChatMsg.Yell:
                                    sourceUnit.Yell((uint)step.script.Talk.TextID, target);
                                    break;
                                case ChatMsg.Emote:
                                case ChatMsg.RaidBossEmote:
                                    sourceUnit.TextEmote((uint)step.script.Talk.TextID, target, step.script.Talk.ChatType == ChatMsg.RaidBossEmote);
                                    break;
                                case ChatMsg.Whisper:
                                case ChatMsg.RaidBossWhisper:
                                {
                                    Player receiver = target ? target.ToPlayer() : null;
                                    if (!receiver)
                                        Log.outError(LogFilter.Scripts, "{0} attempt to whisper to non-player unit, skipping.", step.script.GetDebugInfo());
                                    else
                                        sourceUnit.Whisper((uint)step.script.Talk.TextID, receiver, step.script.Talk.ChatType == ChatMsg.RaidBossWhisper);
                                    break;
                                }
                                default:
                                    break; // must be already checked at load
                            }
                        }
                        break;
                    }
                    case ScriptCommands.Emote:
                    {
                        // Source or target must be Creature.
                        Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script);
                        if (cSource)
                        {
                            if (step.script.Emote.Flags.HasAnyFlag(eScriptFlags.EmoteUseState))
                                cSource.SetEmoteState((Emote)step.script.Emote.EmoteID);
                            else
                                cSource.HandleEmoteCommand((Emote)step.script.Emote.EmoteID);
                        }
                        break;
                    }
                    case ScriptCommands.MoveTo:
                    {
                        // Source or target must be Creature.
                        Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script);
                        if (cSource)
                        {
                            Unit unit = cSource.ToUnit();
                            if (step.script.MoveTo.TravelTime != 0)
                            {
                                float speed =
                                    unit.GetDistance(step.script.MoveTo.DestX, step.script.MoveTo.DestY,
                                        step.script.MoveTo.DestZ) / (step.script.MoveTo.TravelTime * 0.001f);
                                unit.MonsterMoveWithSpeed(step.script.MoveTo.DestX, step.script.MoveTo.DestY,
                                    step.script.MoveTo.DestZ, speed);
                            }
                            else
                                unit.NearTeleportTo(step.script.MoveTo.DestX, step.script.MoveTo.DestY,
                                    step.script.MoveTo.DestZ, unit.GetOrientation());
                        }
                        break;
                    }
                    case ScriptCommands.TeleportTo:
                    {
                        if (step.script.TeleportTo.Flags.HasAnyFlag(eScriptFlags.TeleportUseCreature))
                        {
                            // Source or target must be Creature.
                            Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script);
                            if (cSource)
                                cSource.NearTeleportTo(step.script.TeleportTo.DestX, step.script.TeleportTo.DestY,
                                    step.script.TeleportTo.DestZ, step.script.TeleportTo.Orientation);
                        }
                        else
                        {
                            // Source or target must be Player.
                            Player player = _GetScriptPlayerSourceOrTarget(source, target, step.script);
                            if (player)
                                player.TeleportTo(step.script.TeleportTo.MapID, step.script.TeleportTo.DestX,
                                    step.script.TeleportTo.DestY, step.script.TeleportTo.DestZ, step.script.TeleportTo.Orientation);
                        }
                        break;
                    }
                    case ScriptCommands.QuestExplored:
                    {
                        if (!source)
                        {
                            Log.outError(LogFilter.Scripts, "{0} source object is NULL.", step.script.GetDebugInfo());
                            break;
                        }
                        if (!target)
                        {
                            Log.outError(LogFilter.Scripts, "{0} target object is NULL.", step.script.GetDebugInfo());
                            break;
                        }

                        // when script called for item spell casting then target == (unit or GO) and source is player
                        WorldObject worldObject;
                        Player player = target.ToPlayer();
                        if (player != null)
                        {
                            if (!source.IsTypeId(TypeId.Unit) && !source.IsTypeId(TypeId.GameObject) && !source.IsTypeId(TypeId.Player))
                            {
                                Log.outError(LogFilter.Scripts, "{0} source is not unit, gameobject or player (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.",
                                    step.script.GetDebugInfo(), source.GetTypeId(), source.GetEntry(), source.GetGUID().ToString());
                                break;
                            }
                            worldObject = source;
                        }
                        else
                        {
                            player = source.ToPlayer();
                            if (player != null)
                            {
                                if (!target.IsTypeId(TypeId.Unit) && !target.IsTypeId(TypeId.GameObject) && !target.IsTypeId(TypeId.Player))
                                {
                                    Log.outError(LogFilter.Scripts,
                                        "{0} target is not unit, gameobject or player (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.", step.script.GetDebugInfo(), target.GetTypeId(), target.GetEntry(), target.GetGUID().ToString());
                                    break;
                                }
                                worldObject = target;
                            }
                            else
                            {
                                Log.outError(LogFilter.Scripts, "{0} neither source nor target is player (Entry: {0}, GUID: {1}; target: Entry: {2}, GUID: {3}), skipping.",
                                    step.script.GetDebugInfo(), source.GetEntry(), source.GetGUID().ToString(), target.GetEntry(), target.GetGUID().ToString());
                                break;
                            }
                        }

                        // quest id and flags checked at script loading
                        if ((!worldObject.IsTypeId(TypeId.Unit) || worldObject.ToUnit().IsAlive()) &&
                            (step.script.QuestExplored.Distance == 0 ||
                             worldObject.IsWithinDistInMap(player, step.script.QuestExplored.Distance)))
                            player.AreaExploredOrEventHappens(step.script.QuestExplored.QuestID);
                        else
                            player.FailQuest(step.script.QuestExplored.QuestID);

                        break;
                    }

                    case ScriptCommands.KillCredit:
                    {
                        // Source or target must be Player.
                        Player player = _GetScriptPlayerSourceOrTarget(source, target, step.script);
                        if (player)
                        {
                            if (step.script.KillCredit.Flags.HasAnyFlag(eScriptFlags.KillcreditRewardGroup))
                                player.RewardPlayerAndGroupAtEvent(step.script.KillCredit.CreatureEntry, player);
                            else
                                player.KilledMonsterCredit(step.script.KillCredit.CreatureEntry, ObjectGuid.Empty);
                        }
                        break;
                    }
                    case ScriptCommands.RespawnGameobject:
                    {
                        if (step.script.RespawnGameObject.GOGuid == 0)
                        {
                            Log.outError(LogFilter.Scripts, "{0} gameobject guid (datalong) is not specified.", step.script.GetDebugInfo());
                            break;
                        }

                        // Source or target must be WorldObject.
                        WorldObject pSummoner = _GetScriptWorldObject(source, true, step.script);
                        if (pSummoner)
                        {
                            GameObject pGO = _FindGameObject(pSummoner, step.script.RespawnGameObject.GOGuid);
                            if (pGO == null)
                            {
                                Log.outError(LogFilter.Scripts, "{0} gameobject was not found (guid: {1}).", step.script.GetDebugInfo(), step.script.RespawnGameObject.GOGuid);
                                break;
                            }

                            if (pGO.GetGoType() == GameObjectTypes.FishingNode ||
                                pGO.GetGoType() == GameObjectTypes.Door || pGO.GetGoType() == GameObjectTypes.Button ||
                                pGO.GetGoType() == GameObjectTypes.Trap)
                            {
                                Log.outError(LogFilter.Scripts,
                                    "{0} can not be used with gameobject of type {1} (guid: {2}).", step.script.GetDebugInfo(), pGO.GetGoType(), step.script.RespawnGameObject.GOGuid);
                                break;
                            }

                            // Check that GO is not spawned
                            if (!pGO.IsSpawned())
                            {
                                int nTimeToDespawn = Math.Max(5, (int)step.script.RespawnGameObject.DespawnDelay);
                                pGO.SetLootState(LootState.Ready);
                                pGO.SetRespawnTime(nTimeToDespawn);

                                pGO.GetMap().AddToMap(pGO);
                            }
                        }
                        break;
                    }
                    case ScriptCommands.TempSummonCreature:
                    {
                        // Source must be WorldObject.
                        WorldObject pSummoner = _GetScriptWorldObject(source, true, step.script);
                        if (pSummoner)
                        {
                            if (step.script.TempSummonCreature.CreatureEntry == 0)
                                Log.outError(LogFilter.Scripts, "{0} creature entry (datalong) is not specified.", step.script.GetDebugInfo());
                            else
                            {
                                float x = step.script.TempSummonCreature.PosX;
                                float y = step.script.TempSummonCreature.PosY;
                                float z = step.script.TempSummonCreature.PosZ;
                                float o = step.script.TempSummonCreature.Orientation;

                                if (pSummoner.SummonCreature(step.script.TempSummonCreature.CreatureEntry, x, y, z, o, TempSummonType.TimedOrDeadDespawn, step.script.TempSummonCreature.DespawnDelay) == null)
                                    Log.outError(LogFilter.Scripts, "{0} creature was not spawned (entry: {1}).", step.script.GetDebugInfo(), step.script.TempSummonCreature.CreatureEntry);
                            }
                        }
                        break;
                    }

                    case ScriptCommands.OpenDoor:
                    case ScriptCommands.CloseDoor:
                        _ScriptProcessDoor(source, target, step.script);
                        break;
                    case ScriptCommands.ActivateObject:
                    {
                        // Source must be Unit.
                        Unit unit = _GetScriptUnit(source, true, step.script);
                        if (unit)
                        {
                            // Target must be GameObject.
                            if (target == null)
                            {
                                Log.outError(LogFilter.Scripts, "{0} target object is NULL.", step.script.GetDebugInfo());
                                break;
                            }

                            if (!target.IsTypeId(TypeId.GameObject))
                            {
                                Log.outError(LogFilter.Scripts,
                                    "{0} target object is not gameobject (TypeId: {1}, Entry: {2}, GUID: {3}), skipping.", step.script.GetDebugInfo(), target.GetTypeId(), target.GetEntry(),
                                    target.GetGUID().ToString());
                                break;
                            }
                            GameObject pGO = target.ToGameObject();
                            if (pGO)
                                pGO.Use(unit);
                        }
                        break;
                    }
                    case ScriptCommands.RemoveAura:
                    {
                        // Source (datalong2 != 0) or target (datalong2 == 0) must be Unit.
                        bool bReverse = step.script.RemoveAura.Flags.HasAnyFlag(eScriptFlags.RemoveauraReverse);
                        Unit unit = _GetScriptUnit(bReverse ? source : target, bReverse, step.script);
                        if (unit)
                            unit.RemoveAurasDueToSpell(step.script.RemoveAura.SpellID);
                        break;
                    }
                    case ScriptCommands.CastSpell:
                    {
                        if (source == null && target == null)
                        {
                            Log.outError(LogFilter.Scripts, "{0} source and target objects are NULL.", step.script.GetDebugInfo());
                            break;
                        }

                        WorldObject uSource = null;
                        WorldObject uTarget = null;
                        // source/target cast spell at target/source (script.datalong2: 0: s.t 1: s.s 2: t.t 3: t.s
                        switch (step.script.CastSpell.Flags)
                        {
                            case eScriptFlags.CastspellSourceToTarget: // source . target
                                uSource = source;
                                uTarget = target;
                                break;
                            case eScriptFlags.CastspellSourceToSource: // source . source
                                uSource = source;
                                uTarget = uSource;
                                break;
                            case eScriptFlags.CastspellTargetToTarget: // target . target
                                uSource = target;
                                uTarget = uSource;
                                break;
                            case eScriptFlags.CastspellTargetToSource: // target . source
                                uSource = target;
                                uTarget = source;
                                break;
                            case eScriptFlags.CastspellSearchCreature: // source . creature with entry
                                uSource = source;
                                uTarget = uSource?.FindNearestCreature((uint)Math.Abs(step.script.CastSpell.CreatureEntry), step.script.CastSpell.SearchRadius);
                                break;
                        }

                        if (uSource == null)
                        {
                            Log.outError(LogFilter.Scripts, "{0} no source worldobject found for spell {1}", step.script.GetDebugInfo(), step.script.CastSpell.SpellID);
                            break;
                        }

                        if (uTarget == null)
                        {
                            Log.outError(LogFilter.Scripts, "{0} no target worldobject found for spell {1}", step.script.GetDebugInfo(), step.script.CastSpell.SpellID);
                            break;
                        }

                        bool triggered = ((int)step.script.CastSpell.Flags != 4)
                            ? step.script.CastSpell.CreatureEntry.HasAnyFlag((int)eScriptFlags.CastspellTriggered)
                            : step.script.CastSpell.CreatureEntry < 0;
                        uSource.CastSpell(uTarget, step.script.CastSpell.SpellID, triggered);
                        break;
                    }

                    case ScriptCommands.PlaySound:
                        // Source must be WorldObject.
                        WorldObject obj = _GetScriptWorldObject(source, true, step.script);
                        if (obj)
                        {
                            // PlaySound.Flags bitmask: 0/1=anyone/target
                            Player player2 = null;
                            if (step.script.PlaySound.Flags.HasAnyFlag(eScriptFlags.PlaysoundTargetPlayer))
                            {
                                // Target must be Player.
                                player2 = _GetScriptPlayer(target, false, step.script);
                                if (target == null)
                                    break;
                            }

                            // PlaySound.Flags bitmask: 0/2=without/with distance dependent
                            if (step.script.PlaySound.Flags.HasAnyFlag(eScriptFlags.PlaysoundDistanceSound))
                                obj.PlayDistanceSound(step.script.PlaySound.SoundID, player2);
                            else
                                obj.PlayDirectSound(step.script.PlaySound.SoundID, player2);
                        }
                        break;

                    case ScriptCommands.CreateItem:
                        // Target or source must be Player.
                        Player pReceiver = _GetScriptPlayerSourceOrTarget(source, target, step.script);
                        if (pReceiver)
                        {
                            var dest = new List<ItemPosCount>();
                            InventoryResult msg = pReceiver.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, step.script.CreateItem.ItemEntry, step.script.CreateItem.Amount);
                            if (msg == InventoryResult.Ok)
                            {
                                Item item = pReceiver.StoreNewItem(dest, step.script.CreateItem.ItemEntry, true);
                                if (item != null)
                                    pReceiver.SendNewItem(item, step.script.CreateItem.Amount, false, true);
                            }
                            else
                                pReceiver.SendEquipError(msg, null, null, step.script.CreateItem.ItemEntry);
                        }
                        break;

                    case ScriptCommands.DespawnSelf:
                    {
                        // First try with target or source creature, then with target or source gameobject
                        Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script, true);
                        if (cSource != null)
                            cSource.DespawnOrUnsummon(step.script.DespawnSelf.DespawnDelay);
                        else
                        {
                            GameObject goSource = _GetScriptGameObjectSourceOrTarget(source, target, step.script, true);
                            if (goSource != null)
                                goSource.DespawnOrUnsummon(TimeSpan.FromMilliseconds(step.script.DespawnSelf.DespawnDelay));
                        }
                        break;
                    }
                    case ScriptCommands.LoadPath:
                    {
                        // Source must be Unit.
                        Unit unit = _GetScriptUnit(source, true, step.script);
                        if (unit)
                        {
                            if (Global.WaypointMgr.GetPath(step.script.LoadPath.PathID) == null)
                                Log.outError(LogFilter.Scripts, "{0} source object has an invalid path ({1}), skipping.", step.script.GetDebugInfo(), step.script.LoadPath.PathID);
                            else
                                unit.GetMotionMaster().MovePath(step.script.LoadPath.PathID, step.script.LoadPath.IsRepeatable != 0);
                        }
                        break;
                    }
                    case ScriptCommands.CallscriptToUnit:
                    {
                        if (step.script.CallScript.CreatureEntry == 0)
                        {
                            Log.outError(LogFilter.Scripts, "{0} creature entry is not specified, skipping.", step.script.GetDebugInfo());
                            break;
                        }
                        if (step.script.CallScript.ScriptID == 0)
                        {
                            Log.outError(LogFilter.Scripts, "{0} script id is not specified, skipping.", step.script.GetDebugInfo());
                            break;
                        }

                        Creature cTarget = null;
                        var creatureBounds = _creatureBySpawnIdStore.LookupByKey(step.script.CallScript.CreatureEntry);
                        if (!creatureBounds.Empty())
                        {
                            // Prefer alive (last respawned) creature
                            var foundCreature = creatureBounds.Find(creature => creature.IsAlive());

                            cTarget = foundCreature ?? creatureBounds[0];
                        }

                        if (cTarget == null)
                        {
                            Log.outError(LogFilter.Scripts, "{0} target was not found (entry: {1})", step.script.GetDebugInfo(), step.script.CallScript.CreatureEntry);
                            break;
                        }

                        // Insert script into schedule but do not start it
                        ScriptsStart((ScriptsType)step.script.CallScript.ScriptType, step.script.CallScript.ScriptID, cTarget, null);
                        break;
                    }

                    case ScriptCommands.Kill:
                    {
                        // Source or target must be Creature.
                        Creature cSource = _GetScriptCreatureSourceOrTarget(source, target, step.script);
                        if (cSource)
                        {
                            if (cSource.IsDead())
                                Log.outError(LogFilter.Scripts, "{0} creature is already dead (Entry: {1}, GUID: {2})", step.script.GetDebugInfo(), cSource.GetEntry(), cSource.GetGUID().ToString());
                            else
                            {
                                cSource.SetDeathState(DeathState.JustDied);
                                if (step.script.Kill.RemoveCorpse == 1)
                                    cSource.RemoveCorpse();
                            }
                        }
                        break;
                    }
                    case ScriptCommands.Orientation:
                    {
                        // Source must be Unit.
                        Unit sourceUnit = _GetScriptUnit(source, true, step.script);
                        if (sourceUnit)
                        {
                            if (step.script.Orientation.Flags.HasAnyFlag(eScriptFlags.OrientationFaceTarget))
                            {
                                // Target must be Unit.
                                Unit targetUnit = _GetScriptUnit(target, false, step.script);
                                if (targetUnit == null)
                                    break;

                                sourceUnit.SetFacingToObject(targetUnit);
                            }
                            else
                                sourceUnit.SetFacingTo(step.script.Orientation._Orientation);
                        }
                        break;
                    }
                    case ScriptCommands.Equip:
                    {
                        // Source must be Creature.
                        Creature cSource = _GetScriptCreature(source, target, step.script);
                        if (cSource)
                            cSource.LoadEquipment((int)step.script.Equip.EquipmentID);
                        break;
                    }
                    case ScriptCommands.Model:
                    {
                        // Source must be Creature.
                        Creature cSource = _GetScriptCreature(source, target, step.script);
                        if (cSource)
                            cSource.SetDisplayId(step.script.Model.ModelID);
                        break;
                    }
                    case ScriptCommands.CloseGossip:
                    {
                        // Source must be Player.
                        Player player = _GetScriptPlayer(source, true, step.script);
                        if (player != null)
                            player.PlayerTalkClass.SendCloseGossip();
                        break;
                    }
                    case ScriptCommands.Playmovie:
                    {
                        // Source must be Player.
                        Player player = _GetScriptPlayer(source, true, step.script);
                        if (player)
                            player.SendMovieStart(step.script.PlayMovie.MovieID);
                        break;
                    }
                    case ScriptCommands.Movement:
                    {
                        // Source must be Creature.
                        Creature cSource = _GetScriptCreature(source, true, step.script);
                        if (cSource)
                        {
                            if (!cSource.IsAlive())
                                return;

                            cSource.GetMotionMaster().MoveIdle();

                            switch ((MovementGeneratorType)step.script.Movement.MovementType)
                            {
                                case MovementGeneratorType.Random:
                                    cSource.GetMotionMaster().MoveRandom(step.script.Movement.MovementDistance);
                                    break;
                                case MovementGeneratorType.Waypoint:
                                    cSource.GetMotionMaster().MovePath((uint)step.script.Movement.Path, false);
                                    break;
                            }
                        }
                        break;
                    }
                    case ScriptCommands.PlayAnimkit:
                    {
                        // Source must be Creature.
                        Creature cSource = _GetScriptCreature(source, true, step.script);
                        if (cSource)
                            cSource.PlayOneShotAnimKitId((ushort)step.script.PlayAnimKit.AnimKitID);
                        break;
                    }
                    default:
                        Log.outError(LogFilter.Scripts, "Unknown script command {0}.", step.script.GetDebugInfo());
                        break;
                }

                m_scriptSchedule.Remove(iter);
                iter = m_scriptSchedule.FirstOrDefault();
                Global.MapMgr.DecreaseScheduledScriptCount();
            }
        }

        #endregion

        #region Fields
        internal object _mapLock = new();
        object _gridLock = new();

        bool _creatureToMoveLock;
        List<Creature> creaturesToMove = new();

        bool _gameObjectsToMoveLock;
        List<GameObject> _gameObjectsToMove = new();

        bool _dynamicObjectsToMoveLock;
        List<DynamicObject> _dynamicObjectsToMove = new();

        bool _areaTriggersToMoveLock;
        List<AreaTrigger> _areaTriggersToMove = new();

        GridMap[][] GridMaps = new GridMap[MapConst.MaxGrids][];
        ushort[][] GridMapReference = new ushort[MapConst.MaxGrids][];
        DynamicMapTree _dynamicTree = new();

        SortedSet<RespawnInfo> _respawnTimes = new(new CompareRespawnInfo());
        Dictionary<ulong, RespawnInfo> _creatureRespawnTimesBySpawnId = new();
        Dictionary<ulong, RespawnInfo> _gameObjectRespawnTimesBySpawnId = new();
        List<uint> _toggledSpawnGroupIds = new();
        uint _respawnCheckTimer;
        Dictionary<uint, uint> _zonePlayerCountMap = new();

        List<Transport> _transports = new();
        Grid[][] i_grids = new Grid[MapConst.MaxGrids][];
        MapRecord i_mapRecord;
        List<WorldObject> i_objectsToRemove = new();
        Dictionary<WorldObject, bool> i_objectsToSwitch = new();
        Difficulty i_spawnMode;
        List<WorldObject> i_worldObjects = new();
        protected List<WorldObject> m_activeNonPlayers = new();
        protected List<Player> m_activePlayers = new();
        Map m_parentMap; // points to MapInstanced or self (always same map id)
        Map m_parentTerrainMap; // points to m_parentMap of MapEntry::ParentMapID
        List<Map> m_childTerrainMaps = new(); // contains m_parentMap of maps that have MapEntry::ParentMapID == GetId()
        SortedMultiMap<long, ScriptAction> m_scriptSchedule = new();

        BitSet i_gridFileExists = new(MapConst.MaxGrids * MapConst.MaxGrids); // cache what grids are available for this map (not including parent/child maps)
        BitSet marked_cells = new(MapConst.TotalCellsPerMap * MapConst.TotalCellsPerMap);
        public Dictionary<ulong, CreatureGroup> CreatureGroupHolder = new();
        internal uint i_InstanceId;
        long i_gridExpiry;
        bool i_scriptLock;

        public int m_VisibilityNotifyPeriod;
        public float m_VisibleDistance;
        internal uint m_unloadTimer;

        Dictionary<uint, ZoneDynamicInfo> _zoneDynamicInfo = new();
        IntervalTimer _weatherUpdateTimer;
        Dictionary<HighGuid, ObjectGuidGenerator> _guidGenerators = new();
        Dictionary<ObjectGuid, WorldObject> _objectsStore = new();
        MultiMap<ulong, Creature> _creatureBySpawnIdStore = new();
        MultiMap<ulong, GameObject> _gameobjectBySpawnIdStore = new();
        MultiMap<ulong, AreaTrigger> _areaTriggerBySpawnIdStore = new();
        MultiMap<uint, Corpse> _corpsesByCell = new();
        Dictionary<ObjectGuid, Corpse> _corpsesByPlayer = new();
        List<Corpse> _corpseBones = new();

        List<WorldObject> _updateObjects = new();

        public delegate void FarSpellCallback(Map map);
        Queue<FarSpellCallback> _farSpellCallbacks = new();

        MultiPersonalPhaseTracker _multiPersonalPhaseTracker = new();
        #endregion
    }

    public class InstanceMap : Map
    {
        public InstanceMap(uint id, long expiry, uint InstanceId, Difficulty spawnMode, Map _parent)
            : base(id, expiry, InstanceId, spawnMode, _parent)
        {
            //lets initialize visibility distance for dungeons
            InitVisibilityDistance();

            // the timer is started by default, and stopped when the first player joins
            // this make sure it gets unloaded if for some reason no player joins
            m_unloadTimer = (uint)Math.Max(WorldConfig.GetIntValue(WorldCfg.InstanceUnloadDelay), 1);
        }

        public override void InitVisibilityDistance()
        {
            //init visibility distance for instances
            m_VisibleDistance = Global.WorldMgr.GetMaxVisibleDistanceInInstances();
            m_VisibilityNotifyPeriod = Global.WorldMgr.GetVisibilityNotifyPeriodInInstances();
        }

        public override EnterState CannotEnter(Player player)
        {
            if (player.GetMap() == this)
            {
                Log.outError(LogFilter.Maps, "InstanceMap:CannotEnter - player {0} ({1}) already in map {2}, {3}, {4}!", player.GetName(), player.GetGUID().ToString(), GetId(), GetInstanceId(), GetDifficultyID());
                Cypher.Assert(false);
                return EnterState.CannotEnterAlreadyInMap;
            }

            // allow GM's to enter
            if (player.IsGameMaster())
                return base.CannotEnter(player);

            // cannot enter if the instance is full (player cap), GMs don't count
            uint maxPlayers = GetMaxPlayers();
            if (GetPlayersCountExceptGMs() >= maxPlayers)
            {
                Log.outInfo(LogFilter.Maps, "MAP: Instance '{0}' of map '{1}' cannot have more than '{2}' players. Player '{3}' rejected", GetInstanceId(), GetMapName(), maxPlayers, player.GetName());
                return EnterState.CannotEnterMaxPlayers;
            }

            // cannot enter while an encounter is in progress (unless this is a relog, in which case it is permitted)
            if (!player.IsLoading() && IsRaid() && GetInstanceScript() != null && GetInstanceScript().IsEncounterInProgress())
                return EnterState.CannotEnterZoneInCombat;

            // cannot enter if player is permanent saved to a different instance id
            InstanceBind playerBind = player.GetBoundInstance(GetId(), GetDifficultyID());
            if (playerBind != null)
                if (playerBind.perm && playerBind.save != null)
                    if (playerBind.save.GetInstanceId() != GetInstanceId())
                        return EnterState.CannotEnterInstanceBindMismatch;

            return base.CannotEnter(player);
        }

        public override bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            // @todo Not sure about checking player level: already done in HandleAreaTriggerOpcode
            // GMs still can teleport player in instance.
            // Is it needed?
            lock (_mapLock)
            {
                // Dungeon only code
                if (IsDungeon())
                {
                    Group group = player.GetGroup();

                    // increase current instances (hourly limit)
                    if (!group || !group.IsLFGGroup())
                        player.AddInstanceEnterTime(GetInstanceId(), GameTime.GetGameTime());

                    // get or create an instance save for the map
                    InstanceSave mapSave = Global.InstanceSaveMgr.GetInstanceSave(GetInstanceId());
                    if (mapSave == null)
                    {
                        Log.outInfo(LogFilter.Maps, "InstanceMap.Add: creating instance save for map {0} spawnmode {1} with instance id {2}", GetId(), GetDifficultyID(), GetInstanceId());
                        mapSave = Global.InstanceSaveMgr.AddInstanceSave(GetId(), GetInstanceId(), GetDifficultyID(), 0, 0, true);
                    }

                    Cypher.Assert(mapSave != null);

                    // check for existing instance binds
                    InstanceBind playerBind = player.GetBoundInstance(GetId(), GetDifficultyID());
                    if (playerBind != null && playerBind.perm)
                    {
                        // cannot enter other instances if bound permanently
                        if (playerBind.save != mapSave)
                        {
                            Log.outError(LogFilter.Maps, "InstanceMap.Add: player {0}({1}) is permanently bound to instance {2} {3}, {4}, {5}, {6}, {7}, {8} but he is being put into instance {9} {10}, {11}, {12}, {13}, {14}, {15}",
                                player.GetName(), player.GetGUID().ToString(), GetMapName(), playerBind.save.GetMapId(),
                                playerBind.save.GetInstanceId(), playerBind.save.GetDifficultyID(),
                                playerBind.save.GetPlayerCount(), playerBind.save.GetGroupCount(),
                                playerBind.save.CanReset(), GetMapName(), mapSave.GetMapId(), mapSave.GetInstanceId(),
                                mapSave.GetDifficultyID(), mapSave.GetPlayerCount(), mapSave.GetGroupCount(),
                                mapSave.CanReset());
                            return false;
                        }
                    }
                    else
                    {
                        if (group)
                        {
                            // solo saves should have been reset when the map was loaded
                            InstanceBind groupBind = group.GetBoundInstance(this);
                            if (playerBind != null && playerBind.save != mapSave)
                            {
                                Log.outError(LogFilter.Maps,
                                    "InstanceMapAdd: player {0}({1}) is being put into instance {2} {3}, {4}, {5}, {6}, {7}, {8} but he is in group {9} and is bound to instance {10}, {11}, {12}, {13}, {14}, {15}!",
                                    player.GetName(), player.GetGUID().ToString(), GetMapName(), mapSave.GetMapId(), mapSave.GetInstanceId(),
                                    mapSave.GetDifficultyID(), mapSave.GetPlayerCount(), mapSave.GetGroupCount(),
                                    mapSave.CanReset(), group.GetLeaderGUID().ToString(),
                                    playerBind.save.GetMapId(), playerBind.save.GetInstanceId(),
                                    playerBind.save.GetDifficultyID(), playerBind.save.GetPlayerCount(),
                                    playerBind.save.GetGroupCount(), playerBind.save.CanReset());
                                if (groupBind != null)
                                    Log.outError(LogFilter.Maps,
                                        "InstanceMap.Add: the group is bound to the instance {0} {1}, {2}, {3}, {4}, {5}, {6}",
                                        GetMapName(), groupBind.save.GetMapId(), groupBind.save.GetInstanceId(),
                                        groupBind.save.GetDifficultyID(), groupBind.save.GetPlayerCount(),
                                        groupBind.save.GetGroupCount(), groupBind.save.CanReset());
                                Cypher.Assert(false);
                                return false;
                            }
                            // bind to the group or keep using the group save
                            if (groupBind == null)
                                group.BindToInstance(mapSave, false);
                            else
                            {
                                // cannot jump to a different instance without resetting it
                                if (groupBind.save != mapSave)
                                {
                                    Log.outError(LogFilter.Maps,
                                        "InstanceMap.Add: player {0}({1}) is being put into instance {2}, {3}, {4} but he is in group {5} which is bound to instance {6}, {7}, {8}!",
                                        player.GetName(), player.GetGUID().ToString(), mapSave.GetMapId(), mapSave.GetInstanceId(),
                                        mapSave.GetDifficultyID(), group.GetLeaderGUID().ToString(),
                                        groupBind.save.GetMapId(), groupBind.save.GetInstanceId(),
                                        groupBind.save.GetDifficultyID());
                                    Log.outError(LogFilter.Maps, "MapSave players: {0}, group count: {1}",
                                        mapSave.GetPlayerCount(), mapSave.GetGroupCount());
                                    if (groupBind.save != null)
                                        Log.outError(LogFilter.Maps, "GroupBind save players: {0}, group count: {1}",
                                            groupBind.save.GetPlayerCount(), groupBind.save.GetGroupCount());
                                    else
                                        Log.outError(LogFilter.Maps, "GroupBind save NULL");
                                    return false;
                                }
                                // if the group/leader is permanently bound to the instance
                                // players also become permanently bound when they enter
                                if (groupBind.perm)
                                {
                                    PendingRaidLock pendingRaidLock = new();
                                    pendingRaidLock.TimeUntilLock = 60000;
                                    pendingRaidLock.CompletedMask = i_data != null ? i_data.GetCompletedEncounterMask() : 0;
                                    pendingRaidLock.Extending = false;
                                    pendingRaidLock.WarningOnly = false; // events it throws:  1 : INSTANCE_LOCK_WARNING   0 : INSTANCE_LOCK_STOP / INSTANCE_LOCK_START
                                    player.SendPacket(pendingRaidLock);
                                    player.SetPendingBind(mapSave.GetInstanceId(), 60000);
                                }
                            }
                        }
                        else
                        {
                            // set up a solo bind or continue using it
                            if (playerBind == null)
                                player.BindToInstance(mapSave, false);
                            else
                                // cannot jump to a different instance without resetting it
                                Cypher.Assert(playerBind.save == mapSave);
                        }
                    }
                }

                // for normal instances cancel the reset schedule when the
                // first player enters (no players yet)
                SetResetSchedule(false);

                Log.outInfo(LogFilter.Maps, "MAP: Player '{0}' entered instance '{1}' of map '{2}'", player.GetName(),
                    GetInstanceId(), GetMapName());
                // initialize unload state
                m_unloadTimer = 0;
                m_resetAfterUnload = false;
                m_unloadWhenEmpty = false;
            }

            // this will acquire the same mutex so it cannot be in the previous block
            base.AddPlayerToMap(player, initPlayer);

            if (i_data != null)
                i_data.OnPlayerEnter(player);

            if (i_scenario != null)
                i_scenario.OnPlayerEnter(player);

            return true;
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            if (i_data != null)
            {
                i_data.Update(diff);
                i_data.UpdateCombatResurrection(diff);
            }

            if (i_scenario != null)
                i_scenario.Update(diff);
        }

        public override void RemovePlayerFromMap(Player player, bool remove)
        {
            Log.outInfo(LogFilter.Maps, "MAP: Removing player '{0}' from instance '{1}' of map '{2}' before relocating to another map", player.GetName(), GetInstanceId(), GetMapName());

            if (i_data != null)
                i_data.OnPlayerLeave(player);

            // if last player set unload timer
            if (m_unloadTimer == 0 && GetPlayers().Count == 1)
                m_unloadTimer = m_unloadWhenEmpty ? 1 : (uint)Math.Max(WorldConfig.GetIntValue(WorldCfg.InstanceUnloadDelay), 1);

            if (i_scenario != null)
                i_scenario.OnPlayerExit(player);

            base.RemovePlayerFromMap(player, remove);

            // for normal instances schedule the reset after all players have left
            SetResetSchedule(true);
            Global.InstanceSaveMgr.UnloadInstanceSave(GetInstanceId());
        }

        public void CreateInstanceData(bool load)
        {
            if (i_data != null)
                return;

            InstanceTemplate mInstance = Global.ObjectMgr.GetInstanceTemplate(GetId());
            if (mInstance != null)
            {
                i_script_id = mInstance.ScriptId;
                i_data = Global.ScriptMgr.CreateInstanceData(this);
            }

            if (i_data == null)
                return;

            i_data.Initialize();

            if (load)
            {
                // @todo make a global storage for this
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_INSTANCE);
                stmt.AddValue(0, GetId());
                stmt.AddValue(1, i_InstanceId);
                SQLResult result = DB.Characters.Query(stmt);

                if (!result.IsEmpty())
                {
                    var data = result.Read<string>(0);
                    i_data.SetCompletedEncountersMask(result.Read<uint>(1));
                    i_data.SetEntranceLocation(result.Read<uint>(2));
                    if (data != "")
                    {
                        Log.outDebug(LogFilter.Maps, "Loading instance data for `{0}` with id {1}",
                            Global.ObjectMgr.GetScriptName(i_script_id), i_InstanceId);
                        i_data.Load(data);
                    }
                }
            }
            else
                i_data.Create();
        }

        public bool Reset(InstanceResetMethod method)
        {
            // note: since the map may not be loaded when the instance needs to be reset
            // the instance must be deleted from the DB by InstanceSaveManager

            if (HavePlayers())
            {
                if (method == InstanceResetMethod.All || method == InstanceResetMethod.ChangeDifficulty)
                {
                    // notify the players to leave the instance so it can be reset
                    foreach (Player player in GetPlayers())
                        player.SendResetFailedNotify(GetId());
                }
                else
                {
                    bool doUnload = true;
                    if (method == InstanceResetMethod.Global)
                    {
                        // set the homebind timer for players inside (1 minute)
                        foreach (Player player in GetPlayers())
                        {
                            InstanceBind bind = player.GetBoundInstance(GetId(), GetDifficultyID());
                            if (bind != null && bind.extendState != 0 && bind.save.GetInstanceId() == GetInstanceId())
                                doUnload = false;
                            else
                                player.m_InstanceValid = false;
                        }

                        if (doUnload && HasPermBoundPlayers()) // check if any unloaded players have a nonexpired save to this
                            doUnload = false;
                    }

                    if (doUnload)
                    {
                        // the unload timer is not started
                        // instead the map will unload immediately after the players have left
                        m_unloadWhenEmpty = true;
                        m_resetAfterUnload = true;
                    }
                }
            }
            else
            {
                // unloaded at next update
                m_unloadTimer = 1;
                m_resetAfterUnload = !(method == InstanceResetMethod.Global && HasPermBoundPlayers());
            }

            return GetPlayers().Empty();
        }

        public string GetScriptName()
        {
            return Global.ObjectMgr.GetScriptName(i_script_id);
        }

        public void PermBindAllPlayers()
        {
            if (!IsDungeon())
                return;

            InstanceSave save = Global.InstanceSaveMgr.GetInstanceSave(GetInstanceId());
            if (save == null)
            {
                Log.outError(LogFilter.Maps, "Cannot bind players to instance map (Name: {0}, Entry: {1}, Difficulty: {2}, ID: {3}) because no instance save is available!", GetMapName(), GetId(), GetDifficultyID(), GetInstanceId());
                return;
            }

            // perm bind all players that are currently inside the instance
            foreach (Player player in GetPlayers())
            {
                // never instance bind GMs with GM mode enabled
                if (player.IsGameMaster())
                    continue;

                InstanceBind bind = player.GetBoundInstance(save.GetMapId(), save.GetDifficultyID());
                if (bind != null && bind.perm)
                {
                    if (bind.save != null && bind.save.GetInstanceId() != save.GetInstanceId())
                    {
                        Log.outError(LogFilter.Maps, "Player (GUID: {0}, Name: {1}) is in instance map (Name: {2}, Entry: {3}, Difficulty: {4}, ID: {5}) that is being bound, but already has a save for the map on ID {6}!",
                            player.GetGUID().GetCounter(), player.GetName(), GetMapName(), save.GetMapId(), save.GetDifficultyID(), save.GetInstanceId(), bind.save.GetInstanceId());
                    }
                    else if (bind.save == null)
                    {
                        Log.outError(LogFilter.Maps, "Player (GUID: {0}, Name: {1}) is in instance map (Name: {2}, Entry: {3}, Difficulty: {4}, ID: {5}) that is being bound, but already has a bind (without associated save) for the map!",
                            player.GetGUID().GetCounter(), player.GetName(), GetMapName(), save.GetMapId(), save.GetDifficultyID(), save.GetInstanceId());
                    }
                }
                else
                {
                    player.BindToInstance(save, true);
                    InstanceSaveCreated data = new();
                    data.Gm = player.IsGameMaster();
                    player.SendPacket(data);

                    player.GetSession().SendCalendarRaidLockout(save, true);

                    // if group leader is in instance, group also gets bound
                    Group group = player.GetGroup();
                    if (group)
                        if (group.GetLeaderGUID() == player.GetGUID())
                            group.BindToInstance(save, true);
                }
            }
        }

        public override void UnloadAll()
        {
            Cypher.Assert(!HavePlayers());

            if (m_resetAfterUnload)
            {
                DeleteRespawnTimes();
                DeleteCorpseData();
            }

            base.UnloadAll();
        }

        public void SendResetWarnings(uint timeLeft)
        {
            foreach (Player player in GetPlayers())
                player.SendInstanceResetWarning(GetId(), player.GetDifficultyID(GetEntry()), timeLeft, true);
        }

        void SetResetSchedule(bool on)
        {
            // only for normal instances
            // the reset time is only scheduled when there are no payers inside
            // it is assumed that the reset time will rarely (if ever) change while the reset is scheduled
            if (IsDungeon() && !HavePlayers() && !IsRaidOrHeroicDungeon())
            {
                InstanceSave save = Global.InstanceSaveMgr.GetInstanceSave(GetInstanceId());
                if (save != null)
                    Global.InstanceSaveMgr.ScheduleReset(on, save.GetResetTime(),
                        new InstanceSaveManager.InstResetEvent(0, GetId(), GetDifficultyID(), GetInstanceId()));
                else
                    Log.outError(LogFilter.Maps,
                        "InstanceMap.SetResetSchedule: cannot turn schedule {0}, there is no save information for instance (map [id: {1}, name: {2}], instance id: {3}, difficulty: {4})",
                        on ? "on" : "off", GetId(), GetMapName(), GetInstanceId(), GetDifficultyID());
            }
        }

        bool HasPermBoundPlayers()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PERM_BIND_BY_INSTANCE);
            stmt.AddValue(0, GetInstanceId());
            return !DB.Characters.Query(stmt).IsEmpty();
        }

        public uint GetMaxPlayers()
        {
            MapDifficultyRecord mapDiff = GetMapDifficulty();
            if (mapDiff != null && mapDiff.MaxPlayers != 0)
                return mapDiff.MaxPlayers;

            return GetEntry().MaxPlayers;
        }

        private uint GetMaxResetDelay()
        {
            MapDifficultyRecord mapDiff = GetMapDifficulty();
            return mapDiff != null ? mapDiff.GetRaidDuration() : 0;
        }

        public uint GetScriptId()
        {
            return i_script_id;
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nScriptId: {GetScriptId()} ScriptName: {GetScriptName()}";
        }
        
        public InstanceScript GetInstanceScript()
        {
            return i_data;
        }

        public InstanceScenario GetInstanceScenario() { return i_scenario; }
        public void SetInstanceScenario(InstanceScenario scenario) { i_scenario = scenario; }

        InstanceScript i_data;
        uint i_script_id;
        InstanceScenario i_scenario;
        bool m_resetAfterUnload;
        bool m_unloadWhenEmpty;
    }

    public class BattlegroundMap : Map
    {
        public BattlegroundMap(uint id, uint expiry, uint InstanceId, Map _parent, Difficulty spawnMode)
            : base(id, expiry, InstanceId, spawnMode, _parent)
        {
            InitVisibilityDistance();
        }

        public override void InitVisibilityDistance()
        {
            m_VisibleDistance = Global.WorldMgr.GetMaxVisibleDistanceInBGArenas();
            m_VisibilityNotifyPeriod = Global.WorldMgr.GetVisibilityNotifyPeriodInBGArenas();
        }

        public override EnterState CannotEnter(Player player)
        {
            if (player.GetMap() == this)
            {
                Log.outError(LogFilter.Maps, "BGMap:CannotEnter - player {0} is already in map!", player.GetGUID().ToString());
                Cypher.Assert(false);
                return EnterState.CannotEnterAlreadyInMap;
            }

            if (player.GetBattlegroundId() != GetInstanceId())
                return EnterState.CannotEnterInstanceBindMismatch;

            return base.CannotEnter(player);
        }

        public override bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            lock (_mapLock)
                player.m_InstanceValid = true;

            return base.AddPlayerToMap(player, initPlayer);
        }

        public override void RemovePlayerFromMap(Player player, bool remove)
        {
            Log.outInfo(LogFilter.Maps,
                "MAP: Removing player '{0}' from bg '{1}' of map '{2}' before relocating to another map", player.GetName(),
                GetInstanceId(), GetMapName());
            base.RemovePlayerFromMap(player, remove);
        }

        public void SetUnload()
        {
            m_unloadTimer = 1;
        }

        public override void RemoveAllPlayers()
        {
            if (HavePlayers())
                foreach (Player player in m_activePlayers)
                    if (!player.IsBeingTeleportedFar())
                        player.TeleportTo(player.GetBattlegroundEntryPoint());
        }

        public Battleground GetBG() { return m_bg; }
        public void SetBG(Battleground bg) { m_bg = bg; }

        Battleground m_bg;
    }

    public struct ScriptAction
    {
        public ObjectGuid ownerGUID;

        // owner of source if source is item
        public ScriptInfo script;

        public ObjectGuid sourceGUID;
        public ObjectGuid targetGUID;
    }

    public class ZoneDynamicInfo
    {
        public uint MusicId;
        public Weather DefaultWeather;
        public WeatherState WeatherId;
        public float Intensity;
        public List<LightOverride> LightOverrides = new();

        public struct LightOverride
        {
            public uint AreaLightId;
            public uint OverrideLightId;
            public uint TransitionMilliseconds;
        }
    }

    public class PositionFullTerrainStatus
    {
        public struct AreaInfo
        {
            public int AdtId;
            public int RootId;
            public int GroupId;
            public uint MogpFlags;

            public AreaInfo(int adtId, int rootId, int groupId, uint flags)
            {
                AdtId = adtId;
                RootId = rootId;
                GroupId = groupId;
                MogpFlags = flags;
            }
        }

        public uint AreaId;
        public float FloorZ;
        public bool outdoors = true;
        public ZLiquidStatus LiquidStatus;
        public AreaInfo? areaInfo;
        public LiquidData LiquidInfo;
    }

    public class RespawnInfo
    {
        public SpawnObjectType type;
        public ulong spawnId;
        public uint entry;
        public long respawnTime;
        public uint gridId;

        public RespawnInfo() { }
        public RespawnInfo(RespawnInfo info)
        {
            type = info.type;
            spawnId = info.spawnId;
            entry = info.entry;
            respawnTime = info.respawnTime;
            gridId = info.gridId;
        }
    }

    struct CompareRespawnInfo : IComparer<RespawnInfo>
    {
        public int Compare(RespawnInfo a, RespawnInfo b)
        {
            if (a == b)
                return 0;
            if (a.respawnTime != b.respawnTime)
                return a.respawnTime.CompareTo(b.respawnTime);
            if (a.spawnId != b.spawnId)
                return a.spawnId.CompareTo(b.spawnId);

            Cypher.Assert(a.type != b.type, $"Duplicate respawn entry for spawnId ({a.type},{a.spawnId}) found!");
            return a.type.CompareTo(b.type);
        }
    }
}