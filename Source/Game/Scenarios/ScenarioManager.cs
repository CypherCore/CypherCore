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
using Framework.GameMath;
using Game.Achievements;
using Game.DataStorage;
using Game.Maps;
using System;
using System.Collections.Generic;
namespace Game.Scenarios
{
    public class ScenarioManager : Singleton<ScenarioManager>
    {
        ScenarioManager() { }

        public InstanceScenario CreateInstanceScenario(Map map, int team)
        {
            var dbData = _scenarioDBData.LookupByKey(Tuple.Create(map.GetId(), (byte)map.GetDifficultyID()));
            // No scenario registered for this map and difficulty in the database
            if (dbData == null)
                return null;

            uint scenarioID = 0;
            switch (team)
            {
                case TeamId.Alliance:
                    scenarioID = dbData.Scenario_A;
                    break;
                case TeamId.Horde:
                    scenarioID = dbData.Scenario_H;
                    break;
                default:
                    break;
            }

            var scenarioData = _scenarioData.LookupByKey(scenarioID);
            if (scenarioData == null)
            {
                Log.outError(LogFilter.Scenario, "Table `scenarios` contained data linking scenario (Id: {0}) to map (Id: {1}), difficulty (Id: {2}) but no scenario data was found related to that scenario Id.", 
                    scenarioID, map.GetId(), map.GetDifficultyID());
                return null;
            }

            return new InstanceScenario(map, scenarioData);
        }

        public void LoadDBData()
        {
            _scenarioDBData.Clear();

            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.World.Query("SELECT map, difficulty, scenario_A, scenario_H FROM scenarios");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 scenarios. DB table `scenarios` is empty!");
                return;
            }

            do
            {
                uint mapId = result.Read<uint>(0);
                byte difficulty = result.Read<byte>(1);

                uint scenarioAllianceId = result.Read<uint>(2);
                if (scenarioAllianceId > 0 && !_scenarioData.ContainsKey(scenarioAllianceId))
                {
                    Log.outError(LogFilter.Sql, "ScenarioMgr.LoadDBData: DB Table `scenarios`, column scenario_A contained an invalid scenario (Id: {0})!", scenarioAllianceId);
                    continue;
                }

                uint scenarioHordeId = result.Read<uint>(3);
                if (scenarioHordeId > 0 && !_scenarioData.ContainsKey(scenarioHordeId))
                {
                    Log.outError(LogFilter.Sql, "ScenarioMgr.LoadDBData: DB Table `scenarios`, column scenario_H contained an invalid scenario (Id: {0})!", scenarioHordeId);
                    continue;
                }

                if (scenarioHordeId == 0)
                    scenarioHordeId = scenarioAllianceId;

                ScenarioDBData data = new ScenarioDBData();
                data.MapID = mapId;
                data.DifficultyID = difficulty;
                data.Scenario_A = scenarioAllianceId;
                data.Scenario_H = scenarioHordeId;
                _scenarioDBData[Tuple.Create(mapId, difficulty)] = data;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} instance scenario entries in {1} ms", _scenarioDBData.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadDB2Data()
        {
            _scenarioData.Clear();

            Dictionary<uint, Dictionary<byte, ScenarioStepRecord>> scenarioSteps = new Dictionary<uint, Dictionary<byte, ScenarioStepRecord>>();
            uint deepestCriteriaTreeSize = 0;

            foreach (ScenarioStepRecord step in CliDB.ScenarioStepStorage.Values)
            {
                if (!scenarioSteps.ContainsKey(step.ScenarioID))
                    scenarioSteps[step.ScenarioID] = new Dictionary<byte, ScenarioStepRecord>();

                scenarioSteps[step.ScenarioID][step.OrderIndex] = step;
                CriteriaTree tree = Global.CriteriaMgr.GetCriteriaTree(step.CriteriaTreeId);
                if (tree != null)
                {
                    uint criteriaTreeSize = 0;
                    CriteriaManager.WalkCriteriaTree(tree, treeFunc =>
                    {
                        ++criteriaTreeSize;
                    });
                    deepestCriteriaTreeSize = Math.Max(deepestCriteriaTreeSize, criteriaTreeSize);
                }
            }

            //ASSERT(deepestCriteriaTreeSize < MAX_ALLOWED_SCENARIO_POI_QUERY_SIZE, "MAX_ALLOWED_SCENARIO_POI_QUERY_SIZE must be at least {0}", deepestCriteriaTreeSize + 1);

            foreach (ScenarioRecord scenario in CliDB.ScenarioStorage.Values)
            {
                ScenarioData data = new ScenarioData();
                data.Entry = scenario;
                data.Steps = scenarioSteps.LookupByKey(scenario.Id);
                _scenarioData[scenario.Id] = data;
            }
        }

        public void LoadScenarioPOI()
        {
            uint oldMSTime = Time.GetMSTime();

            _scenarioPOIStore.Clear(); // need for reload case

            uint count = 0;

            //                                         0               1          2     3      4        5         6      7              8
            SQLResult result = DB.World.Query("SELECT CriteriaTreeID, BlobIndex, Idx1, MapID, UiMapID, Priority, Flags, WorldEffectID, PlayerConditionID FROM scenario_poi ORDER BY CriteriaTreeID, Idx1");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 scenario POI definitions. DB table `scenario_poi` is empty.");
                return;
            }

            //                                                0        1    2  3
            SQLResult points = DB.World.Query("SELECT CriteriaTreeID, Idx1, X, Y FROM scenario_poi_points ORDER BY CriteriaTreeID DESC, Idx1, Idx2");

            List<Vector2>[][] POIs = new List<Vector2>[0][];

            if (!points.IsEmpty())
            {
                // The first result should have the highest criteriaTreeId
                uint criteriaTreeIdMax = result.Read<uint>(0);
                POIs = new List<Vector2>[criteriaTreeIdMax + 1][];

                do
                {
                    int CriteriaTreeID = points.Read<int>(0);
                    int Idx1 = points.Read<int>(1);
                    int X = points.Read<int>(2);
                    int Y = points.Read<int>(3);

                    if (POIs[CriteriaTreeID].Length <= Idx1 + 1)
                        POIs[CriteriaTreeID] = new List<Vector2>[Idx1 + 10];

                    POIs[CriteriaTreeID][Idx1].Add(new Vector2(X, Y));
                } while (points.NextRow());
            }

            do
            {
                int criteriaTreeID = result.Read<int>(0);
                int blobIndex = result.Read<int>(1);
                int idx1 = result.Read<int>(2);
                int mapID = result.Read<int>(3);
                int uiMapID = result.Read<int>(4);
                int priority = result.Read<int>(5);
                int flags = result.Read<int>(6);
                int worldEffectID = result.Read<int>(7);
                int playerConditionID = result.Read<int>(8);

                if (Global.CriteriaMgr.GetCriteriaTree((uint)criteriaTreeID) == null)
                    Log.outError(LogFilter.Sql, "`scenario_poi` CriteriaTreeID ({0}) Idx1 ({1}) does not correspond to a valid criteria tree", criteriaTreeID, idx1);

                if (criteriaTreeID < POIs.Length && idx1 < POIs[criteriaTreeID].Length)
                    _scenarioPOIStore.Add((uint)criteriaTreeID, new ScenarioPOI(blobIndex, mapID, uiMapID, priority, flags, worldEffectID, playerConditionID, POIs[criteriaTreeID][idx1]));
                else
                    Log.outError(LogFilter.ServerLoading, "Table scenario_poi references unknown scenario poi points for criteria tree id {0} POI id {1}", criteriaTreeID, blobIndex);

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} scenario POI definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public List<ScenarioPOI> GetScenarioPOIs(uint CriteriaTreeID)
        {
            if (!_scenarioPOIStore.ContainsKey(CriteriaTreeID))
                return null;

            return _scenarioPOIStore[CriteriaTreeID];
        }

        Dictionary<uint, ScenarioData> _scenarioData = new Dictionary<uint, ScenarioData>();
        MultiMap<uint, ScenarioPOI> _scenarioPOIStore = new MultiMap<uint, ScenarioPOI>();
        Dictionary<Tuple<uint, byte>, ScenarioDBData> _scenarioDBData = new Dictionary<Tuple<uint, byte>, ScenarioDBData>();
    }

    public class ScenarioData
    {
        public ScenarioRecord Entry;
        public Dictionary<byte, ScenarioStepRecord> Steps = new Dictionary<byte, ScenarioStepRecord>();
    }

    class ScenarioDBData
    {
        public uint MapID;
        public byte DifficultyID;
        public uint Scenario_A;
        public uint Scenario_H;
    }

    public class ScenarioPOI
    {
        public ScenarioPOI(int blobIndex, int mapID, int uiMapID, int priority, int flags, int worldEffectID, int playerConditionID, List<Vector2> points)
        {
            BlobIndex = blobIndex;
            MapID = mapID;
            UiMapID = uiMapID;
            Priority = priority;
            Flags = flags;
            WorldEffectID = worldEffectID;
            PlayerConditionID = playerConditionID;
            Points = points;
        }

        public int BlobIndex;
        public int MapID;
        public int UiMapID;
        public int Priority;
        public int Flags;
        public int WorldEffectID;
        public int PlayerConditionID;
        public List<Vector2> Points = new List<Vector2>();
    }
}
