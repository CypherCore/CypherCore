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
using Framework.Database;
using Game.Achievements;
using Game.DataStorage;
using Game.Maps;
using Game.Network;
using System;
using System.Collections.Generic;

namespace Game.Scenarios
{
    public class InstanceScenario : Scenario
    {
        public InstanceScenario(Map map, ScenarioData scenarioData) : base(scenarioData)
        {
            _map = map;

            //ASSERT(_map);
            LoadInstanceData(_map.GetInstanceId());

            var players = map.GetPlayers();
            foreach (var player in players)
                SendScenarioState(player);
        }

        public void SaveToDB()
        {
            if (_criteriaProgress.Empty())
                return;

            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(_map.GetDifficultyID());
            if (difficultyEntry == null || difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.ChallengeMode)) // Map should have some sort of "CanSave" boolean that returns whether or not the map is savable. (Challenge modes cannot be saved for example)
                return;

            uint id = _map.GetInstanceId();
            if (id == 0)
            {
                Log.outDebug(LogFilter.Scenario, "Scenario.SaveToDB: Can not save scenario progress without an instance save. Map.GetInstanceId() did not return an instance save.");
                return;
            }

            SQLTransaction trans = new SQLTransaction();
            foreach (var iter in _criteriaProgress)
            {
                if (!iter.Value.Changed)
                    continue;

                Criteria criteria = Global.CriteriaMgr.GetCriteria(iter.Key);
                switch (criteria.Entry.Type)
                {
                    // Blizzard only appears to store creature kills
                    case CriteriaTypes.KillCreature:
                        break;
                    default:
                        continue;
                }

                if (iter.Value.Counter != 0)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_SCENARIO_INSTANCE_CRITERIA);
                    stmt.AddValue(0, id);
                    stmt.AddValue(1, iter.Key);
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_SCENARIO_INSTANCE_CRITERIA);
                    stmt.AddValue(0, id);
                    stmt.AddValue(1, iter.Key);
                    stmt.AddValue(2, iter.Value.Counter);
                    stmt.AddValue(3, (uint)iter.Value.Date);
                    trans.Append(stmt);
                }

                iter.Value.Changed = false;
            }

            DB.Characters.CommitTransaction(trans);
        }

        void LoadInstanceData(uint instanceId)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_SCENARIO_INSTANCE_CRITERIA_FOR_INSTANCE);
            stmt.AddValue(0, instanceId);

            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                SQLTransaction trans = new SQLTransaction();
                long now = Time.UnixTime;

                List<CriteriaTree> criteriaTrees = new List<CriteriaTree>();
                do
                {
                    uint id = result.Read<uint>(0);
                    ulong counter = result.Read<ulong>(1);
                    long date = result.Read<uint>(2);

                    Criteria criteria = Global.CriteriaMgr.GetCriteria(id);
                    if (criteria == null)
                    {
                        // Removing non-existing criteria data for all instances
                        Log.outError(LogFilter.Scenario, "Removing scenario criteria {0} data from the table `instance_scenario_progress`.", id);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_SCENARIO_INSTANCE_CRITERIA);
                        stmt.AddValue(0, instanceId);
                        stmt.AddValue(1, id);
                        trans.Append(stmt);
                        continue;
                    }

                    if (criteria.Entry.StartTimer != 0 && (date + criteria.Entry.StartTimer) < now)
                        continue;

                    switch (criteria.Entry.Type)
                    {
                        // Blizzard appears to only stores creatures killed progress for unknown reasons. Either technical shortcoming or intentional
                        case CriteriaTypes.KillCreature:
                            break;
                        default:
                            continue;
                    }

                    SetCriteriaProgress(criteria, counter, null, ProgressType.Set);

                    List<CriteriaTree> trees = Global.CriteriaMgr.GetCriteriaTreesByCriteria(criteria.ID);
                    if (trees != null)
                    {
                        foreach (CriteriaTree tree in trees)
                            criteriaTrees.Add(tree);
                    }
                }
                while (result.NextRow());

                DB.Characters.CommitTransaction(trans);

                foreach (CriteriaTree tree in criteriaTrees)
                {
                    ScenarioStepRecord step = tree.ScenarioStep;
                    if (step == null)
                        continue;

                    if (IsCompletedCriteriaTree(tree))
                        SetStepState(step, ScenarioStepState.Done);
                }
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

        Map _map;
        Dictionary<byte, Dictionary<uint, CriteriaProgress>> _stepCriteriaProgress = new Dictionary<byte, Dictionary<uint, CriteriaProgress>>();
    }
}
