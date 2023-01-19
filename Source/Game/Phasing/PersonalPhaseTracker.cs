// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Dynamic;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    class PersonalPhaseSpawns
    {
        public static TimeSpan DELETE_TIME_DEFAULT = TimeSpan.FromMinutes(1);

        public List<WorldObject> Objects = new();
        public List<ushort> Grids = new();
        public TimeSpan? DurationRemaining;

        public bool IsEmpty() { return Objects.Empty() && Grids.Empty(); }
    }

    class PlayerPersonalPhasesTracker
    {
        Dictionary<uint, PersonalPhaseSpawns> _spawns = new();

        public void RegisterTrackedObject(uint phaseId, WorldObject obj)
        {
            _spawns[phaseId].Objects.Add(obj);
        }

        public void UnregisterTrackedObject(WorldObject obj)
        {
            foreach (var spawns in _spawns.Values)
                spawns.Objects.Remove(obj);
        }

        public void OnOwnerPhasesChanged(WorldObject owner)
        {
            PhaseShift phaseShift = owner.GetPhaseShift();

            // Loop over all our tracked phases. If any don't exist - delete them
            foreach (var (phaseId, spawns) in _spawns)
                if (!spawns.DurationRemaining.HasValue && !phaseShift.HasPhase(phaseId))
                    spawns.DurationRemaining = PersonalPhaseSpawns.DELETE_TIME_DEFAULT;

            // loop over all owner phases. If any exist and marked for deletion - reset delete
            foreach (var phaseRef in phaseShift.GetPhases())
            {
                PersonalPhaseSpawns spawns = _spawns.LookupByKey(phaseRef.Key);
                if (spawns != null)
                    spawns.DurationRemaining = null;
            }
        }

        public void MarkAllPhasesForDeletion()
        {
            foreach (var spawns in _spawns.Values)
                spawns.DurationRemaining = PersonalPhaseSpawns.DELETE_TIME_DEFAULT;
        }

        public void Update(Map map, uint diff)
        {
            foreach (var itr in _spawns.ToList())
            {
                if (itr.Value.DurationRemaining.HasValue)
                {
                    itr.Value.DurationRemaining = itr.Value.DurationRemaining.Value - TimeSpan.FromMilliseconds(diff);
                    if (itr.Value.DurationRemaining.Value <= TimeSpan.Zero)
                    {
                        DespawnPhase(map, itr.Value);
                        _spawns.Remove(itr.Key);
                    }
                }
            }
        }

        public bool IsGridLoadedForPhase(uint gridId, uint phaseId)
        {
            PersonalPhaseSpawns spawns = _spawns.LookupByKey(phaseId);
            if (spawns != null)
                return spawns.Grids.Contains((ushort)gridId);

            return false;
        }

        public void SetGridLoadedForPhase(uint gridId, uint phaseId)
        {
            if (!_spawns.ContainsKey(phaseId))
                _spawns[phaseId] = new();

            PersonalPhaseSpawns group = _spawns[phaseId];
            group.Grids.Add((ushort)gridId);
        }

        public void SetGridUnloaded(uint gridId)
        {
            foreach (var itr in _spawns.ToList())
            {
                itr.Value.Grids.Remove((ushort)gridId);
                if (itr.Value.IsEmpty())
                    _spawns.Remove(itr.Key);
            }
        }

        void DespawnPhase(Map map, PersonalPhaseSpawns spawns)
        {
            foreach (var obj in spawns.Objects)
                map.AddObjectToRemoveList(obj);

            spawns.Objects.Clear();
            spawns.Grids.Clear();
        }

        public bool IsEmpty() { return _spawns.Empty(); }
    }

    public class MultiPersonalPhaseTracker
    {
        Dictionary<ObjectGuid, PlayerPersonalPhasesTracker> _playerData = new();

        public void LoadGrid(PhaseShift phaseShift, Grid grid, Map map, Cell cell)
        {
            if (!phaseShift.HasPersonalPhase())
                return;

            PersonalPhaseGridLoader loader = new(grid, map, cell, phaseShift.GetPersonalGuid());
            PlayerPersonalPhasesTracker playerTracker = _playerData[phaseShift.GetPersonalGuid()];

            foreach (var phaseRef in phaseShift.GetPhases())
            {
                if (!phaseRef.Value.IsPersonal())
                    continue;

                if (!Global.ObjectMgr.HasPersonalSpawns(map.GetId(), map.GetDifficultyID(), phaseRef.Key))
                    continue;

                if (playerTracker.IsGridLoadedForPhase(grid.GetGridId(), phaseRef.Key))
                    continue;

                Log.outDebug(LogFilter.Maps, $"Loading personal phase objects (phase {phaseRef.Key}) in {cell} for map {map.GetId()} instance {map.GetInstanceId()}");

                loader.Load(phaseRef.Key);

                playerTracker.SetGridLoadedForPhase(grid.GetGridId(), phaseRef.Key);
            }

            if (loader.GetLoadedGameObjects() != 0)
                map.Balance();
        }

        public void UnloadGrid(Grid grid)
        {
            foreach (var itr in _playerData.ToList())
            {
                itr.Value.SetGridUnloaded(grid.GetGridId());
                if (itr.Value.IsEmpty())
                    _playerData.Remove(itr.Key);
            }
        }

        public void RegisterTrackedObject(uint phaseId, ObjectGuid phaseOwner, WorldObject obj)
        {
            Cypher.Assert(phaseId != 0);
            Cypher.Assert(!phaseOwner.IsEmpty());
            Cypher.Assert(obj != null);

            _playerData[phaseOwner].RegisterTrackedObject(phaseId, obj);
        }

        public void UnregisterTrackedObject(WorldObject obj)
        {
            PlayerPersonalPhasesTracker playerTracker = _playerData.LookupByKey(obj.GetPhaseShift().GetPersonalGuid());
            if (playerTracker != null)
                playerTracker.UnregisterTrackedObject(obj);
        }

        public void OnOwnerPhaseChanged(WorldObject phaseOwner, Grid grid, Map map, Cell cell)
        {
            PlayerPersonalPhasesTracker playerTracker = _playerData.LookupByKey(phaseOwner.GetGUID());
            if (playerTracker != null)
                playerTracker.OnOwnerPhasesChanged(phaseOwner);

            if (grid != null)
                LoadGrid(phaseOwner.GetPhaseShift(), grid, map, cell);
        }

        public void MarkAllPhasesForDeletion(ObjectGuid phaseOwner)
        {
            PlayerPersonalPhasesTracker playerTracker = _playerData.LookupByKey(phaseOwner);
            if (playerTracker != null)
                playerTracker.MarkAllPhasesForDeletion();
        }

        public void Update(Map map, uint diff)
        {
            foreach (var itr in _playerData.ToList())
            {
                itr.Value.Update(map, diff);
                if (itr.Value.IsEmpty())
                    _playerData.Remove(itr.Key);
            }
        }
    }
}
