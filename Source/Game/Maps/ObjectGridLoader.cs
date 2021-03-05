﻿/*
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
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Maps
{
    internal class ObjectGridLoader : Notifier
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

                    var worker = new ObjectWorldLoader(this);
                    visitor = new Visitor(worker, GridMapTypeMask.AllWorld);
                    i_grid.VisitGrid(x, y, visitor);
                }
            }
            Log.outDebug(LogFilter.Maps, "{0} GameObjects, {1} Creatures, and {2} Corpses/Bones loaded for grid {3} on map {4}", i_gameObjects, i_creatures, i_corpses, i_grid.GetGridId(), i_map.GetId());
        }

        public override void Visit(IList<GameObject> objs)
        {
            var cellCoord = i_cell.GetCellCoord();
            CellObjectGuids cellguids = Global.ObjectMgr.GetCellObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), cellCoord.GetId());
            if (cellguids == null)
                return;

            LoadHelper<GameObject>(cellguids.gameobjects, cellCoord, ref i_gameObjects, i_map);
        }

        public override void Visit(IList<Creature> objs)
        {
            var cellCoord = i_cell.GetCellCoord();
            CellObjectGuids cellguids = Global.ObjectMgr.GetCellObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), cellCoord.GetId());
            if (cellguids == null)
                return;

            LoadHelper<Creature>(cellguids.creatures, cellCoord, ref i_creatures, i_map);
        }

        public override void Visit(IList<AreaTrigger> objs)
        {
            var cellCoord = i_cell.GetCellCoord();
            var areaTriggers = Global.AreaTriggerDataStorage.GetAreaTriggersForMapAndCell(i_map.GetId(), cellCoord.GetId());
            if (areaTriggers == null)
                return;

            LoadHelper<AreaTrigger>(areaTriggers, cellCoord, ref i_areaTriggers, i_map);
        }

        private void LoadHelper<T>(SortedSet<ulong> guid_set, CellCoord cell, ref uint count, Map map) where T : WorldObject, new()
        {
            foreach (var guid in guid_set)
            {
                var obj = new T();
                // Don't spawn at all if there's a respawn time
                if ((obj.IsTypeId(TypeId.Unit) && map.GetCreatureRespawnTime(guid) == 0) || (obj.IsTypeId(TypeId.GameObject) && map.GetGORespawnTime(guid) == 0) || obj.IsTypeId(TypeId.AreaTrigger))
                {
                    //TC_LOG_INFO("misc", "DEBUG: LoadHelper from table: %s for (guid: %u) Loading", table, guid);
                    if (obj.IsTypeId(TypeId.Unit))
                    {
                        CreatureData cdata = Global.ObjectMgr.GetCreatureData(guid);
                        Cypher.Assert(cdata != null, $"Tried to load creature with spawnId {guid}, but no such creature exists.");

                        var group = cdata.spawnGroupData;
                        // If creature in manual spawn group, don't spawn here, unless group is already active.
                        if (!group.flags.HasAnyFlag(SpawnGroupFlags.System))
                        {
                            if (!map.IsSpawnGroupActive(group.groupId))
                            {
                                obj.Dispose();
                                continue;
                            }
                        }

                        // If script is blocking spawn, don't spawn but queue for a re-check in a little bit
                        if (!group.flags.HasFlag(SpawnGroupFlags.CompatibilityMode) && !Global.ScriptMgr.CanSpawn(guid, cdata.Id, cdata, map))
                        {
                            map.SaveRespawnTime(SpawnObjectType.Creature, guid, cdata.Id, Time.UnixTime + RandomHelper.URand(4, 7), map.GetZoneId(PhasingHandler.EmptyPhaseShift, cdata.spawnPoint), GridDefines.ComputeGridCoord(cdata.spawnPoint.GetPositionX(), cdata.spawnPoint.GetPositionY()).GetId(), false);
                            obj.Dispose();
                            continue;
                        }
                    }
                    else if (obj.IsTypeId(TypeId.GameObject))
                    {
                        // If gameobject in manual spawn group, don't spawn here, unless group is already active.
                        GameObjectData godata = Global.ObjectMgr.GetGameObjectData(guid);
                        Cypher.Assert(godata != null, $"Tried to load gameobject with spawnId {guid}, but no such object exists.");

                        if (!godata.spawnGroupData.flags.HasAnyFlag(SpawnGroupFlags.System))
                        {
                            if (!map.IsSpawnGroupActive(godata.spawnGroupData.groupId))
                            {
                                obj.Dispose();
                                continue;
                            }
                        }
                    }

                    if (!obj.LoadFromDB(guid, map, false, false))
                    {
                        obj.Dispose();
                        continue;
                    }
                    AddObjectHelper(cell, ref count, map, obj);
                }
                else
                    obj.Dispose();
            }
        }

        private void AddObjectHelper<T>(CellCoord cellCoord, ref uint count, Map map, T obj) where T : WorldObject
        {
            var cell = new Cell(cellCoord);
            map.AddToGrid(obj, cell);
            obj.AddToWorld();

            if (obj.IsCreature())
                if (obj.IsActiveObject())
                    map.AddToActive(obj);

            ++count;
        }

        public Cell i_cell;
        public Grid i_grid;
        public Map i_map;
        private uint i_gameObjects;
        private uint i_creatures;
        public uint i_corpses;
        private uint i_areaTriggers;
    }

    internal class ObjectWorldLoader : Notifier
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
            var cellCoord = i_cell.GetCellCoord();
            var corpses = i_map.GetCorpsesInCell(cellCoord.GetId());
            if (corpses != null)
            {
                foreach (var corpse in corpses)
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

        private Cell i_cell;
        private Map i_map;
        private Grid i_grid;

        public uint i_corpses;
    }

    //Stop the creatures before unloading the NGrid
    internal class ObjectGridStoper : Notifier
    {
        public override void Visit(IList<Creature> objs)
        {
            // stop any fights at grid de-activation and remove dynobjects/areatriggers created at cast by creatures
            for (var i = 0; i < objs.Count; ++i)
            {  
                var creature = objs[i];
                creature.RemoveAllDynObjects();
                creature.RemoveAllAreaTriggers();

                if (creature.IsInCombat() || !creature.GetThreatManager().IsThreatListsEmpty())
                {
                    creature.CombatStop();
                    creature.GetThreatManager().ClearAllThreat();
                    if (creature.IsAIEnabled)
                        creature.GetAI().EnterEvadeMode();
                }
            }
        }
    }

    //Move the foreign creatures back to respawn positions before unloading the NGrid
    internal class ObjectGridEvacuator : Notifier
    {
        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                var creature = objs[i];
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
                var gameObject = objs[i];
                // gameobject in unloading grid can have respawn point in another grid
                // if it will be unloaded then it will not respawn in original grid until unload/load original grid
                // move to respawn point to prevent this case. For player view in respawn grid this will be normal respawn.
                gameObject.GetMap().GameObjectRespawnRelocation(gameObject, true);
            }
        }
    }

    //Clean up and remove from world
    internal class ObjectGridCleaner : Notifier
    {
        public override void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                var obj = objs[i];

                if (obj.IsTypeId(TypeId.Player))
                    continue;

                obj.CleanupsBeforeDelete();
            }       
        }
    }

    //Delete objects before deleting NGrid
    internal class ObjectGridUnloader : Notifier
    {
        public override void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                var obj = objs[i];

                if (obj.IsTypeId(TypeId.Corpse))
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
