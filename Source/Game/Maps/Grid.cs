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
using Game.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Game.Maps
{
    public class GridInfo
    {
        public GridInfo()
        {
            i_timer = new TimeTracker(0);
            vis_Update = new PeriodicTimer(0, RandomHelper.IRand(0, 1000));
            i_unloadActiveLockCount = 0;
            i_unloadExplicitLock = false;
        }

        public GridInfo(long expiry, bool unload = true)
        {
            i_timer = new TimeTracker((int)expiry);
            vis_Update = new PeriodicTimer(0, RandomHelper.IRand(0, 1000));
            i_unloadActiveLockCount = 0;
            i_unloadExplicitLock = !unload;
        }

        public TimeTracker getTimeTracker()
        {
            return i_timer;
        }

        public bool getUnloadLock()
        {
            return i_unloadActiveLockCount != 0 || i_unloadExplicitLock;
        }

        public void setUnloadExplicitLock(bool on)
        {
            i_unloadExplicitLock = on;
        }

        public void incUnloadActiveLock()
        {
            ++i_unloadActiveLockCount;
        }

        public void decUnloadActiveLock()
        {
            if (i_unloadActiveLockCount != 0) --i_unloadActiveLockCount;
        }

        private void setTimer(TimeTracker pTimer)
        {
            i_timer = pTimer;
        }

        public void ResetTimeTracker(long interval)
        {
            i_timer.Reset(interval);
        }

        public void UpdateTimeTracker(long diff)
        {
            i_timer.Update(diff);
        }

        public PeriodicTimer getRelocationTimer()
        {
            return vis_Update;
        }

        TimeTracker i_timer;
        PeriodicTimer vis_Update;

        ushort i_unloadActiveLockCount; // lock from active object spawn points (prevent clone loading)
        bool i_unloadExplicitLock; // explicit manual lock or config setting
    }

    public class Grid
    {
        public Grid(uint id, uint x, uint y, long expiry, bool unload = true)
        {
            gridId = id;
            gridX = x;
            gridY = y;
            gridInfo = new GridInfo(expiry, unload);
            gridState = GridState.Invalid;
            gridObjectDataLoaded = false;

            for (uint xx = 0; xx < MapConst.MaxCells; ++xx)
            {
                i_cells[xx] = new GridCell[MapConst.MaxCells];
                for (uint yy = 0; yy < MapConst.MaxCells; ++yy)
                    i_cells[xx][yy] = new GridCell();
            }
        }

        public Grid(Cell cell, uint expiry, bool unload = true) : this(cell.GetId(), cell.GetGridX(), cell.GetGridY(), expiry, unload) { }

        public GridCell GetGridCell(uint x, uint y)
        {
            return i_cells[x][y];
        }

        public uint GetGridId()
        {
            return gridId;
        }

        private void SetGridId(uint id)
        {
            gridId = id;
        }

        public GridState GetGridState()
        {
            return gridState;
        }

        public void SetGridState(GridState s)
        {
            gridState = s;
        }

        public uint getX()
        {
            return gridX;
        }

        public uint getY()
        {
            return gridY;
        }

        public bool isGridObjectDataLoaded()
        {
            return gridObjectDataLoaded;
        }

        public void setGridObjectDataLoaded(bool pLoaded)
        {
            gridObjectDataLoaded = pLoaded;
        }

        public GridInfo getGridInfoRef()
        {
            return gridInfo;
        }

        private TimeTracker getTimeTracker()
        {
            return gridInfo.getTimeTracker();
        }

        public bool getUnloadLock()
        {
            return gridInfo.getUnloadLock();
        }

        public void setUnloadExplicitLock(bool on)
        {
            gridInfo.setUnloadExplicitLock(on);
        }

        public void incUnloadActiveLock()
        {
            gridInfo.incUnloadActiveLock();
        }

        public void decUnloadActiveLock()
        {
            gridInfo.decUnloadActiveLock();
        }

        public void ResetTimeTracker(long interval)
        {
            gridInfo.ResetTimeTracker(interval);
        }

        public void UpdateTimeTracker(long diff)
        {
            gridInfo.UpdateTimeTracker(diff);
        }

        public void Update(Map map, uint diff)
        {
            switch (GetGridState())
            {
                case GridState.Active:
                    // Only check grid activity every (grid_expiry/10) ms, because it's really useless to do it every cycle
                    getGridInfoRef().UpdateTimeTracker(diff);
                    if (getGridInfoRef().getTimeTracker().Passed())
                    {
                        if (GetWorldObjectCountInNGrid<Player>() == 0 && !map.ActiveObjectsNearGrid(this))
                        {
                            ObjectGridStoper worker = new ObjectGridStoper();
                            var visitor = new Visitor(worker, GridMapTypeMask.AllGrid);
                            VisitAllGrids(visitor);
                            SetGridState(GridState.Idle);
                            Log.outDebug(LogFilter.Maps, "Grid[{0}, {1}] on map {2} moved to IDLE state", getX(), getY(),
                                map.GetId());
                        }
                        else
                            map.ResetGridExpiry(this, 0.1f);
                    }
                    break;
                case GridState.Idle:
                    map.ResetGridExpiry(this);
                    SetGridState(GridState.Removal);
                    Log.outDebug(LogFilter.Maps, "Grid[{0}, {1}] on map {2} moved to REMOVAL state", getX(), getY(),
                        map.GetId());
                    break;
                case GridState.Removal:
                    if (!getGridInfoRef().getUnloadLock())
                    {
                        getGridInfoRef().UpdateTimeTracker(diff);
                        if (getGridInfoRef().getTimeTracker().Passed())
                        {
                            if (!map.UnloadGrid(this, false))
                            {
                                Log.outDebug(LogFilter.Maps,
                                    "Grid[{0}, {1}] for map {2} differed unloading due to players or active objects nearby",
                                    getX(), getY(), map.GetId());
                                map.ResetGridExpiry(this);
                            }
                        }
                    }
                    break;
            }
        }

        public void VisitAllGrids(Visitor visitor)
        {
            for (uint x = 0; x < MapConst.MaxCells; ++x)
                for (uint y = 0; y < MapConst.MaxCells; ++y)
                    GetGridCell(x, y).Visit(visitor);
        }

        public void VisitGrid(uint x, uint y, Visitor visitor)
        {
            GetGridCell(x, y).Visit(visitor);
        }

        public uint GetWorldObjectCountInNGrid<T>() where T : WorldObject
        {
            uint count = 0;
            for (uint x = 0; x < MapConst.MaxCells; ++x)
                for (uint y = 0; y < MapConst.MaxCells; ++y)
                    count += i_cells[x][y].GetWorldObjectCountInGrid<T>();
            return count;
        }

        uint gridId;
        uint gridX;
        uint gridY;
        GridInfo gridInfo;
        GridState gridState;
        bool gridObjectDataLoaded;
        GridCell[][] i_cells = new GridCell[MapConst.MaxCells][];
    }

    public class GridCell
    {
        public GridCell()
        {
            _objects = new MultiTypeContainer();
            _container = new MultiTypeContainer();
        }

        public void Visit(Visitor visitor)
        {
            switch (visitor._mask)
            {
                case GridMapTypeMask.AllGrid:
                    visitor.Visit(_container.gameObjects);
                    visitor.Visit(_container.creatures);
                    visitor.Visit(_container.dynamicObjects);
                    visitor.Visit(_container.corpses);
                    visitor.Visit(_container.areaTriggers);
                    visitor.Visit(_container.conversations);
                    visitor.Visit(_container.worldObjects);
                    break;
                case GridMapTypeMask.AllWorld:
                    visitor.Visit(_objects.players);
                    visitor.Visit(_objects.creatures);
                    visitor.Visit(_objects.corpses);
                    visitor.Visit(_objects.dynamicObjects);
                    visitor.Visit(_objects.worldObjects);
                    break;
                default:
                    Log.outError(LogFilter.Server, "{0} called Visit with Unknown Mask {1}.", visitor.ToString(), visitor._mask);
                    break;
            }
        }

        public uint GetWorldObjectCountInGrid<T>() where T : WorldObject
        {
            return (uint)_objects.GetCount<T>();
        }

        public void AddWorldObject(WorldObject obj)
        {
            _objects.Insert(obj);
        }

        public void AddGridObject(WorldObject obj)
        {
            _container.Insert(obj);
        }

        public void RemoveWorldObject(WorldObject obj)
        {
            _objects.Remove(obj);
        }

        public void RemoveGridObject(WorldObject obj)
        {
            _container.Remove(obj);
        }

        public bool HasWorldObject(WorldObject obj)
        {
            return _objects.Contains(obj);
        }

        public bool HasGridObject(WorldObject obj)
        {
            return _container.Contains(obj);
        }

        /// <summary>
        /// Holds all World objects - Player, Pets, Corpse(resurrectable), DynamicObject(farsight)
        /// </summary>
        MultiTypeContainer _objects;

        /// <summary>
        /// Holds all Grid objects - GameObjects, Creatures(except pets), DynamicObject, Corpse(Bones), AreaTrigger, Conversation
        /// </summary>
        MultiTypeContainer _container;
    }

    public class MultiTypeContainer
    {
        public void Insert(WorldObject obj)
        {
            worldObjects.Add(obj);
            switch (obj.GetTypeId())
            {
                case TypeId.Unit:
                    creatures.Add((Creature)obj);
                    break;
                case TypeId.Player:
                    players.Add((Player)obj);
                    break;
                case TypeId.GameObject:
                    gameObjects.Add((GameObject)obj);
                    break;
                case TypeId.DynamicObject:
                    dynamicObjects.Add((DynamicObject)obj);
                    break;
                case TypeId.Corpse:
                    corpses.Add((Corpse)obj);
                    break;
                case TypeId.AreaTrigger:
                    areaTriggers.Add((AreaTrigger)obj);
                    break;
                case TypeId.Conversation:
                    conversations.Add((Conversation)obj);
                    break;
            }
        }

        public void Remove(WorldObject obj)
        {
            worldObjects.Remove(obj);
            switch (obj.GetTypeId())
            {
                case TypeId.Unit:
                    creatures.Remove((Creature)obj);
                    break;
                case TypeId.Player:
                    players.Remove((Player)obj);
                    break;
                case TypeId.GameObject:
                    gameObjects.Remove((GameObject)obj);
                    break;
                case TypeId.DynamicObject:
                    dynamicObjects.Remove((DynamicObject)obj);
                    break;
                case TypeId.Corpse:
                    corpses.Remove((Corpse)obj);
                    break;
                case TypeId.AreaTrigger:
                    areaTriggers.Remove((AreaTrigger)obj);
                    break;
                case TypeId.Conversation:
                    conversations.Remove((Conversation)obj);
                    break;
            }
        }

        public bool Contains(WorldObject obj)
        {
            return worldObjects.Contains(obj);
        }

        public int GetCount<T>()
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    return creatures.Count;
                case "Player":
                    return players.Count;
                case "GameObject":
                    return gameObjects.Count;
                case "DynamicObject":
                    return dynamicObjects.Count;
                case "Corpse":
                    return corpses.Count;
                case "AreaTrigger":
                    return areaTriggers.Count;
                case "Conversation":
                    return conversations.Count;
            }

            return 0;
        }

        public List<Player> players = new List<Player>();
        public List<Creature> creatures = new List<Creature>();
        public List<Corpse> corpses = new List<Corpse>();
        public List<DynamicObject> dynamicObjects = new List<DynamicObject>();
        public List<AreaTrigger> areaTriggers = new List<AreaTrigger>();
        public List<Conversation> conversations = new List<Conversation>();
        public List<GameObject> gameObjects = new List<GameObject>();
        public List<WorldObject> worldObjects = new List<WorldObject>();
    }

    public enum GridState
    {
        Invalid = 0,
        Active = 1,
        Idle = 2,
        Removal = 3,
        Max = 4
    }
}