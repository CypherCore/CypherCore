// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
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

        public InstanceScenario CreateInstanceScenario(InstanceMap map, int team)
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

                ScenarioDBData data = new();
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

            Dictionary<uint, Dictionary<byte, ScenarioStepRecord>> scenarioSteps = new();
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
                ScenarioData data = new();
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

            //                                         0               1          2     3      4        5         6      7              8                  9
            SQLResult result = DB.World.Query("SELECT CriteriaTreeID, BlobIndex, Idx1, MapID, UiMapID, Priority, Flags, WorldEffectID, PlayerConditionID, NavigationPlayerConditionID FROM scenario_poi ORDER BY CriteriaTreeID, Idx1");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 scenario POI definitions. DB table `scenario_poi` is empty.");
                return;
            }

            Dictionary<uint, MultiMap<int, ScenarioPOIPoint>> allPoints = new();

            //                                               0               1    2  3  4
            SQLResult pointsResult = DB.World.Query("SELECT CriteriaTreeID, Idx1, X, Y, Z FROM scenario_poi_points ORDER BY CriteriaTreeID DESC, Idx1, Idx2");
            if (!pointsResult.IsEmpty())
            {
                do
                {
                    uint CriteriaTreeID = pointsResult.Read<uint>(0);
                    int Idx1 = pointsResult.Read<int>(1);
                    int X = pointsResult.Read<int>(2);
                    int Y = pointsResult.Read<int>(3);
                    int Z = pointsResult.Read<int>(4);

                    if (!allPoints.ContainsKey(CriteriaTreeID))
                        allPoints[CriteriaTreeID] = new MultiMap<int, ScenarioPOIPoint>();

                    allPoints[CriteriaTreeID].Add(Idx1, new ScenarioPOIPoint(X, Y, Z));

                } while (pointsResult.NextRow());
            }

            do
            {
                uint criteriaTreeID = result.Read<uint>(0);
                int blobIndex = result.Read<int>(1);
                int idx1 = result.Read<int>(2);
                int mapID = result.Read<int>(3);
                int uiMapID = result.Read<int>(4);
                int priority = result.Read<int>(5);
                int flags = result.Read<int>(6);
                int worldEffectID = result.Read<int>(7);
                int playerConditionID = result.Read<int>(8);
                int navigationPlayerConditionID = result.Read<int>(9);

                if (Global.CriteriaMgr.GetCriteriaTree(criteriaTreeID) == null)
                    Log.outError(LogFilter.Sql, $"`scenario_poi` CriteriaTreeID ({criteriaTreeID}) Idx1 ({idx1}) does not correspond to a valid criteria tree");

                var blobs = allPoints.LookupByKey(criteriaTreeID);
                if (blobs != null)
                {
                    var points = blobs.LookupByKey(idx1);
                    if (!points.Empty())
                    {
                        _scenarioPOIStore.Add(criteriaTreeID, new ScenarioPOI(blobIndex, mapID, uiMapID, priority, flags, worldEffectID, playerConditionID, navigationPlayerConditionID, points));
                        ++count;
                        continue;
                    }
                }

                Log.outError(LogFilter.Sql, $"Table scenario_poi references unknown scenario poi points for criteria tree id {criteriaTreeID} POI id {blobIndex}");

            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} scenario POI definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public List<ScenarioPOI> GetScenarioPOIs(uint CriteriaTreeID)
        {
            if (!_scenarioPOIStore.ContainsKey(CriteriaTreeID))
                return null;

            return _scenarioPOIStore[CriteriaTreeID];
        }

        Dictionary<uint, ScenarioData> _scenarioData = new();
        MultiMap<uint, ScenarioPOI> _scenarioPOIStore = new();
        Dictionary<Tuple<uint, byte>, ScenarioDBData> _scenarioDBData = new();
    }

    public class ScenarioData
    {
        public ScenarioRecord Entry;
        public Dictionary<byte, ScenarioStepRecord> Steps = new();
    }

    class ScenarioDBData
    {
        public uint MapID;
        public byte DifficultyID;
        public uint Scenario_A;
        public uint Scenario_H;
    }

    public struct ScenarioPOIPoint
    {
        public int X;
        public int Y;
        public int Z;

        public ScenarioPOIPoint(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class ScenarioPOI
    {
        public int BlobIndex;
        public int MapID;
        public int UiMapID;
        public int Priority;
        public int Flags;
        public int WorldEffectID;
        public int PlayerConditionID;
        public int NavigationPlayerConditionID;
        public List<ScenarioPOIPoint> Points = new();

        public ScenarioPOI(int blobIndex, int mapID, int uiMapID, int priority, int flags, int worldEffectID, int playerConditionID, int navigationPlayerConditionID, List<ScenarioPOIPoint> points)
        {
            BlobIndex = blobIndex;
            MapID = mapID;
            UiMapID = uiMapID;
            Priority = priority;
            Flags = flags;
            WorldEffectID = worldEffectID;
            PlayerConditionID = playerConditionID;
            NavigationPlayerConditionID = navigationPlayerConditionID;
            Points = points;
        }
    }
}
