// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Maps;
using Game.Maps.Notifiers;

namespace Game
{
    public class MultiPersonalPhaseTracker
    {
        private readonly Dictionary<ObjectGuid, PlayerPersonalPhasesTracker> _playerData = new();

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

            playerTracker?.UnregisterTrackedObject(obj);
        }

        public void OnOwnerPhaseChanged(WorldObject phaseOwner, Grid grid, Map map, Cell cell)
        {
            PlayerPersonalPhasesTracker playerTracker = _playerData.LookupByKey(phaseOwner.GetGUID());

            playerTracker?.OnOwnerPhasesChanged(phaseOwner);

            if (grid != null)
                LoadGrid(phaseOwner.GetPhaseShift(), grid, map, cell);
        }

        public void MarkAllPhasesForDeletion(ObjectGuid phaseOwner)
        {
            PlayerPersonalPhasesTracker playerTracker = _playerData.LookupByKey(phaseOwner);

            playerTracker?.MarkAllPhasesForDeletion();
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