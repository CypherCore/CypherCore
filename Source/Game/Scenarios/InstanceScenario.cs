// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        public InstanceScenario(InstanceMap map, ScenarioData scenarioData) : base(map, scenarioData)
        {
            LoadInstanceData();

            var players = map.GetPlayers();
            foreach (var player in players)
                SendScenarioState(player);
        }

        void LoadInstanceData()
        {
            InstanceScript instanceScript = _map.ToInstanceMap().GetInstanceScript();
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
            _map?.SendToPlayers(data);
        }
    }
}
