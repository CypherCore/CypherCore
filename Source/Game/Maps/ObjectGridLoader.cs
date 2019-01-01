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
using System.Collections.Concurrent;

namespace Game.Maps
{
    class ObjectGridLoader : Notifier
    {
        public ObjectGridLoader(Grid grid, Map map, Cell cell)
        {
            i_cell = new Cell(cell);

            i_grid = grid;
            i_map = map;
        }

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

                    ObjectWorldLoader worker = new ObjectWorldLoader(this);
                    visitor = new Visitor(worker, GridMapTypeMask.AllWorld);
                    i_grid.VisitGrid(x, y, visitor);
                }
            }
            Log.outDebug(LogFilter.Maps, "{0} GameObjects, {1} Creatures, and {2} Corpses/Bones loaded for grid {3} on map {4}", i_gameObjects, i_creatures, i_corpses, i_grid.GetGridId(), i_map.GetId());
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

        void LoadHelper<T>(SortedSet<ulong> guid_set, CellCoord cell, ref uint count, Map map) where T : WorldObject, new()
        {
            foreach (var guid in guid_set)
            {
                T obj = new T();
                if (!obj.LoadFromDB(guid, map))
                    continue;

                AddObjectHelper(cell, ref count, map, obj);
            }
        }

        void AddObjectHelper<T>(CellCoord cellCoord, ref uint count, Map map, T obj) where T : WorldObject
        {
            var cell = new Cell(cellCoord);
            map.AddToGrid(obj, cell);
            obj.AddToWorld();
            ++count;
        }

        void AddObjectHelper(CellCoord cellCoord, ref uint count, Map map, Creature obj)
        {
            map.AddToGrid(obj, new Cell(cellCoord));
            obj.AddToWorld();
            if (obj.isActiveObject())
                map.AddToActive(obj);

            ++count;
        }

        public Cell i_cell;
        public Grid i_grid;
        public Map i_map;
        uint i_gameObjects;
        uint i_creatures;
        public uint i_corpses;
    }

    class ObjectWorldLoader : Notifier
    {
        public ObjectWorldLoader(ObjectGridLoader gloader)
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
                {
                    creature.CombatStop();
                    creature.DeleteThreatList();
                    if (creature.IsAIEnabled)
                        creature.GetAI().EnterEvadeMode();
                }
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
                GameObject go = objs[i];
                // gameobject in unloading grid can have respawn point in another grid
                // if it will be unloaded then it will not respawn in original grid until unload/load original grid
                // move to respawn point to prevent this case. For player view in respawn grid this will be normal respawn.
                go.GetMap().GameObjectRespawnRelocation(go, true);
            }
        }
    }

    //Clean up and remove from world
    class ObjectGridCleaner : Notifier
    {
        public override void Visit(IList<WorldObject> objs)
        {
            foreach (var obj in objs)
            {
                if (obj.IsTypeId(TypeId.Player))
                    continue;

                obj.CleanupsBeforeDelete();
            }       
        }
    }

    //Delete objects before deleting NGrid
    class ObjectGridUnloader : Notifier
    {
        public override void Visit(IList<WorldObject> objs)
        {
            foreach (var obj in objs)
            {
                if (obj.IsTypeId(TypeId.Player) || obj.IsTypeId(TypeId.Corpse))
                    continue;

                // if option set then object already saved at this moment
                if (!WorldConfig.GetBoolValue(WorldCfg.SaveRespawnTimeImmediately))
                    obj.SaveRespawnTime();
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
