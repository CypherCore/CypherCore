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
using Game.Achievements;
using Game.DataStorage;
using Game.Maps;
using Game.Networking;
using System;
using System.Collections.Generic;

namespace Game.Scenarios
{
    public class InstanceScenario : Scenario
    {
        public InstanceScenario(InstanceMap map, ScenarioData scenarioData) : base(scenarioData)
        {
            _map = map;

            //ASSERT(_map);
            LoadInstanceData();

            var players = map.GetPlayers();
            foreach (var player in players)
                SendScenarioState(player);
        }

        void LoadInstanceData()
        {
            InstanceScript instanceScript = _map.GetInstanceScript();
            if (instanceScript == null)
                return;

            List<CriteriaTree> criteriaTrees = new();

            var killCreatureCriteria = Global.CriteriaMgr.GetScenarioCriteriaByTypeAndScenario(CriteriaType.KillCreature, _data.Entry.Id);
            if (!killCreatureCriteria.Empty())
            {
                var spawnGroups = Global.ObjectMgr.GetInstanceSpawnGroupsForMap(_map.GetId());
                if (spawnGroups != null)
                {
                    Dictionary<uint, ulong> despawnedCreatureCountsById = new();
                    foreach (InstanceSpawnGroupInfo spawnGroup in spawnGroups)
                    {
                        if (instanceScript.GetBossState(spawnGroup.BossStateId) != EncounterState.Done)
                            continue;

                        bool isDespawned = ((1 << (int)EncounterState.Done) & spawnGroup.BossStates) == 0 || spawnGroup.Flags.HasFlag(InstanceSpawnGroupFlags.BlockSpawn);
                        if (isDespawned)
                        {
                            foreach (var spawn in Global.ObjectMgr.GetSpawnMetadataForGroup(spawnGroup.SpawnGroupId))
                            {
                                SpawnData spawnData = spawn.ToSpawnData();
                                if (spawnData != null)
                                    ++despawnedCreatureCountsById[spawnData.Id];
                            }
                        }
                    }

                    foreach (Criteria criteria in killCreatureCriteria)
                    {
                        // count creatures in despawned spawn groups
                        ulong progress = despawnedCreatureCountsById.LookupByKey(criteria.Entry.Asset);
                        if (progress != 0)
                        {
                            SetCriteriaProgress(criteria, progress, null, ProgressType.Set);
                            var trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                            if (trees != null)
                                foreach (CriteriaTree tree in trees)
                                    criteriaTrees.Add(tree);
                        }
                    }
                }
            }

            foreach (Criteria criteria in Global.CriteriaMgr.GetScenarioCriteriaByTypeAndScenario(CriteriaType.DefeatDungeonEncounter, _data.Entry.Id))
            {
                if (!instanceScript.IsEncounterCompleted(criteria.Entry.Asset))
                    continue;

                SetCriteriaProgress(criteria, 1, null, ProgressType.Set);
                var trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.Id);
                if (trees != null)
                    foreach (CriteriaTree tree in trees)
                        criteriaTrees.Add(tree);
            }

            foreach (CriteriaTree tree in criteriaTrees)
            {
                ScenarioStepRecord step = tree.ScenarioStep;
                if (step == null)
                    continue;


                if (IsCompletedCriteriaTree(tree))
                    SetStepState(step, ScenarioStepState.Done);
            }
        }

        public override string GetOwnerInfo()
        {
            return $"Instance ID {_map.GetInstanceId()}";
        }

        public override void SendPacket(ServerPacket data)
        {
            //Hack  todo fix me
            if (_map == null)
            {
                return;
            }

            _map.SendToPlayers(data);
        }

        InstanceMap _map;
    }
}
