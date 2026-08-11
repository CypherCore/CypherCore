// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Maps
{
    class ObjectGridLoaderBase : Notifier
    {
        internal Grid i_grid;
        internal Map i_map;
        internal uint i_gameObjects;
        internal uint i_creatures;
        internal uint i_corpses;
        internal uint i_areaTriggers;

        public ObjectGridLoaderBase(Grid grid, Map map)
        {
            i_grid = grid;
            i_map = map;
        }

        public uint GetLoadedCreatures() { return i_creatures; }
        public uint GetLoadedGameObjects() { return i_gameObjects; }
        public uint GetLoadedCorpses() { return i_corpses; }
        public uint GetLoadedAreaTriggers() { return i_areaTriggers; }

        public static void AddToMap<T>(T obj, Map map, ref uint objectCount) where T : WorldObject
        {
            CellCoord cellCoord = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
            Cell cell = new(cellCoord);

            map.AddToGrid<T>(obj, cell);
            obj.AddToWorld();
            if (obj.IsActiveObject())
                map.AddToActive(obj);

            ++objectCount;
        }

        internal void LoadHelper<T>(SortedSet<ulong> guid_set, ref uint count, Map map, uint phaseId = 0, ObjectGuid? phaseOwner = null) where T : WorldObject, new()
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

                AddToMap(obj, map, ref count);
            }
        }
    }

    class ObjectGridLoader : ObjectGridLoaderBase
    {
        public ObjectGridLoader(Grid grid, Map map) : base(grid, map) { }

        public void LoadN()
        {
            i_creatures = 0;
            i_gameObjects = 0;
            i_corpses = 0;

            //Load creatures and game objects
            var grid_guids = Global.ObjectMgr.GetGridObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), i_grid.GetGridId());
            if (grid_guids != null)
            {
                LoadHelper<GameObject>(grid_guids.gameobjects, ref i_gameObjects, i_map);
                LoadHelper<Creature>(grid_guids.creatures, ref i_creatures, i_map);
            }

            //Load areatriggers
            var areaTriggers = Global.AreaTriggerDataStorage.GetAreaTriggersForMapAndGrid(i_map.GetId(), i_map.GetDifficultyID(), i_grid.GetGridId());
            if (areaTriggers != null)
                LoadHelper<AreaTrigger>(areaTriggers, ref i_areaTriggers, i_map);


            //Load corpses (not bones)
            var corpses = i_map.GetCorpsesInGrid(i_grid.GetGridId());
            if (corpses != null)
            {
                foreach (Corpse corpse in corpses)
                    AddToMap(corpse, i_map, ref i_corpses);
            }

            Log.outDebug(LogFilter.Maps, $"{i_gameObjects} GameObjects, {i_creatures} Creatures, {i_areaTriggers} AreaTrriggers and {i_corpses} Corpses/Bones loaded for grid {i_grid.GetGridId()} on map {i_map.GetId()}");
        }
    }

    class PersonalPhaseGridLoader : ObjectGridLoaderBase
    {
        ObjectGuid _phaseOwner;

        public PersonalPhaseGridLoader(Grid grid, Map map, ObjectGuid phaseOwner) : base(grid, map)
        {
            _phaseOwner = phaseOwner;
        }

        public void Load(uint phaseId)
        {
            var grid_guids = Global.ObjectMgr.GetCellPersonalObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), phaseId, i_grid.GetGridId());
            if (grid_guids != null)
            {
                LoadHelper<GameObject>(grid_guids.gameobjects, ref i_gameObjects, i_map, phaseId, _phaseOwner);
                LoadHelper<Creature>(grid_guids.creatures, ref i_creatures, i_map, phaseId, _phaseOwner);
            }
        }
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
