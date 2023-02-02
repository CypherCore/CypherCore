// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Maps
{
    class ObjectGridLoaderBase : Notifier
    {
        internal Cell i_cell;
        internal Grid i_grid;
        internal Map i_map;
        internal uint i_gameObjects;
        internal uint i_creatures;
        internal uint i_corpses;
        internal uint i_areaTriggers;

        public ObjectGridLoaderBase(Grid grid, Map map, Cell cell)
        {
            i_cell = new Cell(cell);
            i_grid = grid;
            i_map = map;
        }

        public uint GetLoadedCreatures() { return i_creatures; }
        public uint GetLoadedGameObjects() { return i_gameObjects; }
        public uint GetLoadedCorpses() { return i_corpses; }
        public uint GetLoadedAreaTriggers() { return i_areaTriggers; }

        internal void LoadHelper<T>(SortedSet<ulong> guid_set, CellCoord cell, ref uint count, Map map, uint phaseId = 0, ObjectGuid? phaseOwner = null) where T : WorldObject, new()
        {
            foreach (var guid in guid_set)
            {
                // Don't spawn at all if there's a respawn timer
                if (!map.ShouldBeSpawnedOnGridLoad<T>(guid))
                    continue;

                T obj = new();
                if (!obj.LoadFromDB(guid, map, false, phaseOwner.HasValue /*allowDuplicate*/))
                {
                    obj.Dispose();
                    continue;
                }

                if (phaseOwner.HasValue)
                {
                    PhasingHandler.InitDbPersonalOwnership(obj.GetPhaseShift(), phaseOwner.Value);
                    map.GetMultiPersonalPhaseTracker().RegisterTrackedObject(phaseId, phaseOwner.Value, obj);
                }

                AddObjectHelper(cell, ref count, map, obj);
            }
        }

        void AddObjectHelper<T>(CellCoord cellCoord, ref uint count, Map map, T obj) where T : WorldObject
        {
            var cell = new Cell(cellCoord);
            map.AddToGrid(obj, cell);
            obj.AddToWorld();

            if (obj.IsCreature())
                if (obj.IsActiveObject())
                    map.AddToActive(obj);

            ++count;
        }
    }

    class ObjectGridLoader : ObjectGridLoaderBase
    {
        public ObjectGridLoader(Grid grid, Map map, Cell cell) : base(grid, map, cell) { }

        public void LoadN()
        {
            i_creatures = 0;
            i_gameObjects = 0;
            i_corpses = 0;
            i_cell.data.cell_y = 0;
            for (uint x = 0; x < MapConst.MaxCells; ++x)
            {
                i_cell.data.cell_x = x;
                for (uint y = 0; y < MapConst.MaxCells; ++y)
                {
                    i_cell.data.cell_y = y;

                    var visitor = new Visitor(this, GridMapTypeMask.AllGrid);
                    i_grid.VisitGrid(x, y, visitor);

                    ObjectWorldLoader worker = new(this);
                    visitor = new Visitor(worker, GridMapTypeMask.AllWorld);
                    i_grid.VisitGrid(x, y, visitor);
                }
            }
            Log.outDebug(LogFilter.Maps, $"{i_gameObjects} GameObjects, {i_creatures} Creatures, {i_areaTriggers} AreaTrriggers and {i_corpses} Corpses/Bones loaded for grid {i_grid.GetGridId()} on map {i_map.GetId()}");
        }

        public override void Visit(IList<GameObject> objs)
        {
            CellCoord cellCoord = i_cell.GetCellCoord();
            CellObjectGuids cellguids = Global.ObjectMgr.GetCellObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), cellCoord.GetId());
            if (cellguids == null)
                return;

            LoadHelper<GameObject>(cellguids.gameobjects, cellCoord, ref i_gameObjects, i_map);
        }

        public override void Visit(IList<Creature> objs)
        {
            CellCoord cellCoord = i_cell.GetCellCoord();
            CellObjectGuids cellguids = Global.ObjectMgr.GetCellObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), cellCoord.GetId());
            if (cellguids == null)
                return;

            LoadHelper<Creature>(cellguids.creatures, cellCoord, ref i_creatures, i_map);
        }

        public override void Visit(IList<AreaTrigger> objs)
        {
            CellCoord cellCoord = i_cell.GetCellCoord();
            SortedSet<ulong> areaTriggers = Global.AreaTriggerDataStorage.GetAreaTriggersForMapAndCell(i_map.GetId(), cellCoord.GetId());
            if (areaTriggers == null)
                return;

            LoadHelper<AreaTrigger>(areaTriggers, cellCoord, ref i_areaTriggers, i_map);
        }
    }

    class PersonalPhaseGridLoader : ObjectGridLoaderBase
    {
        uint _phaseId;
        ObjectGuid _phaseOwner;

        public PersonalPhaseGridLoader(Grid grid, Map map, Cell cell, ObjectGuid phaseOwner) : base(grid, map, cell)
        {
            _phaseId = 0;
            _phaseOwner = phaseOwner;
        }

        public override void Visit(IList<GameObject> objs)
        {
            CellCoord cellCoord = i_cell.GetCellCoord();
            CellObjectGuids cell_guids = Global.ObjectMgr.GetCellPersonalObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), _phaseId, cellCoord.GetId());
            if (cell_guids != null)
                LoadHelper<GameObject>(cell_guids.gameobjects, cellCoord, ref i_gameObjects, i_map, _phaseId, _phaseOwner);
        }

        public override void Visit(IList<Creature> objs)
        {
            CellCoord cellCoord = i_cell.GetCellCoord();
            CellObjectGuids cell_guids = Global.ObjectMgr.GetCellPersonalObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), _phaseId, cellCoord.GetId());
            if (cell_guids != null)
                LoadHelper<Creature>(cell_guids.creatures, cellCoord, ref i_creatures, i_map, _phaseId, _phaseOwner);
        }

        public void Load(uint phaseId)
        {
            _phaseId = phaseId;
            i_cell.data.cell_y = 0;
            for (uint x = 0; x < MapConst.MaxCells; ++x)
            {
                i_cell.data.cell_x = x;
                for (uint y = 0; y < MapConst.MaxCells; ++y)
                {
                    i_cell.data.cell_y = y;

                    //Load creatures and game objects
                    var visitor = new Visitor(this, GridMapTypeMask.AllGrid);
                    i_grid.VisitGrid(x, y, visitor);
                }
            }
        }
    }

    class ObjectWorldLoader : Notifier
    {
        public ObjectWorldLoader(ObjectGridLoaderBase gloader)
        {
            i_cell = gloader.i_cell;
            i_map = gloader.i_map;
            i_grid = gloader.i_grid;
            i_corpses = gloader.i_corpses;
        }

        public override void Visit(IList<Corpse> objs)
        {
            CellCoord cellCoord = i_cell.GetCellCoord();
            var corpses = i_map.GetCorpsesInCell(cellCoord.GetId());
            if (corpses != null)
            {
                foreach (Corpse corpse in corpses)
                {
                    corpse.AddToWorld();
                    var cell = i_grid.GetGridCell(i_cell.GetCellX(), i_cell.GetCellY());
                    if (corpse.IsWorldObject())
                    {
                        i_map.AddToGrid(corpse, new Cell(cellCoord));
                        cell.AddWorldObject(corpse);
                    }
                    else
                        cell.AddGridObject(corpse);

                    ++i_corpses;
                }
            }
        }

        Cell i_cell;
        Map i_map;
        Grid i_grid;

        public uint i_corpses;
    }

    //Stop the creatures before unloading the NGrid
    class ObjectGridStoper : Notifier
    {
        public override void Visit(IList<Creature> objs)
        {
            // stop any fights at grid de-activation and remove dynobjects/areatriggers created at cast by creatures
            for (var i = 0; i < objs.Count; ++i)
            {  
                Creature creature = objs[i];
                creature.RemoveAllDynObjects();
                creature.RemoveAllAreaTriggers();

                if (creature.IsInCombat())
                    creature.CombatStop();
            }
        }
    }

    //Move the foreign creatures back to respawn positions before unloading the NGrid
    class ObjectGridEvacuator : Notifier
    {
        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                // creature in unloading grid can have respawn point in another grid
                // if it will be unloaded then it will not respawn in original grid until unload/load original grid
                // move to respawn point to prevent this case. For player view in respawn grid this will be normal respawn.
                creature.GetMap().CreatureRespawnRelocation(creature, true);
            }
        }

        public override void Visit(IList<GameObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];
                // gameobject in unloading grid can have respawn point in another grid
                // if it will be unloaded then it will not respawn in original grid until unload/load original grid
                // move to respawn point to prevent this case. For player view in respawn grid this will be normal respawn.
                gameObject.GetMap().GameObjectRespawnRelocation(gameObject, true);
            }
        }
    }

    //Clean up and remove from world
    class ObjectGridCleaner : Notifier
    {
        public override void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                WorldObject obj = objs[i];

                if (obj.IsTypeId(TypeId.Player))
                    continue;

                obj.SetDestroyedObject(true);
                obj.CleanupsBeforeDelete();
            }       
        }
    }

    //Delete objects before deleting NGrid
    class ObjectGridUnloader : Notifier
    {
        public override void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                WorldObject obj = objs[i];

                if (obj.IsTypeId(TypeId.Corpse))
                    continue;

                //Some creatures may summon other temp summons in CleanupsBeforeDelete()
                //So we need this even after cleaner (maybe we can remove cleaner)
                //Example: Flame Leviathan Turret 33139 is summoned when a creature is deleted
                //TODO: Check if that script has the correct logic. Do we really need to summons something before deleting?
                obj.CleanupsBeforeDelete();
                obj.Dispose();
            }
        }
    }
}
