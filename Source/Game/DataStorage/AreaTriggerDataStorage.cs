// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Maps;
using System.Collections.Generic;
using System.Numerics;

namespace Game.DataStorage
{
    public class AreaTriggerDataStorage : Singleton<AreaTriggerDataStorage>
    {
        AreaTriggerDataStorage() { }

        public void LoadAreaTriggerTemplates()
        {
            uint oldMSTime = Time.GetMSTime();
            MultiMap<uint, Vector2> verticesByCreateProperties = new();
            MultiMap<uint, Vector2> verticesTargetByCreateProperties = new();
            MultiMap<uint, Vector3> splinesByCreateProperties = new();
            MultiMap<AreaTriggerId, AreaTriggerAction> actionsByAreaTrigger = new();

            //                                                       0         1             2            3           4
            SQLResult templateActions = DB.World.Query("SELECT AreaTriggerId, IsServerSide, ActionType, ActionParam, TargetType FROM `areatrigger_template_actions`");
            if (!templateActions.IsEmpty())
            {
                do
                {
                    AreaTriggerId areaTriggerId = new(templateActions.Read<uint>(0), templateActions.Read<byte>(1) == 1);

                    AreaTriggerAction action;
                    action.Param = templateActions.Read<uint>(3);
                    action.ActionType = (AreaTriggerActionTypes)templateActions.Read<uint>(2);
                    action.TargetType = (AreaTriggerActionUserTypes)templateActions.Read<uint>(4);

                    if (action.ActionType >= AreaTriggerActionTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_template_actions` has invalid ActionType ({action.ActionType}, IsServerSide: {areaTriggerId.IsServerSide}) for AreaTriggerId {areaTriggerId.Id} and Param {action.Param}");
                        continue;
                    }

                    if (action.TargetType >= AreaTriggerActionUserTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_template_actions` has invalid TargetType ({action.TargetType}, IsServerSide: {areaTriggerId.IsServerSide}) for AreaTriggerId {areaTriggerId} and Param {action.Param}");
                        continue;
                    }


                    if (action.ActionType == AreaTriggerActionTypes.Teleport)
                    {
                        if (Global.ObjectMgr.GetWorldSafeLoc(action.Param) == null)
                        {
                            Log.outError(LogFilter.Sql, $"Table `areatrigger_template_actions` has invalid (Id: {areaTriggerId}, IsServerSide: {areaTriggerId.IsServerSide}) with TargetType=Teleport and Param ({action.Param}) not a valid world safe loc entry");
                            continue;
                        }
                    }

                    actionsByAreaTrigger.Add(areaTriggerId, action);
                }
                while (templateActions.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger templates actions. DB table `areatrigger_template_actions` is empty.");
            }

            //                                           0                              1    2         3         4               5
            SQLResult vertices = DB.World.Query("SELECT AreaTriggerCreatePropertiesId, Idx, VerticeX, VerticeY, VerticeTargetX, VerticeTargetY FROM `areatrigger_create_properties_polygon_vertex` ORDER BY `AreaTriggerCreatePropertiesId`, `Idx`");
            if (!vertices.IsEmpty())
            {
                do
                {
                    uint areaTriggerCreatePropertiesId = vertices.Read<uint>(0);

                    verticesByCreateProperties.Add(areaTriggerCreatePropertiesId, new Vector2(vertices.Read<float>(2), vertices.Read<float>(3)));

                    if (!vertices.IsNull(4) && !vertices.IsNull(5))
                        verticesTargetByCreateProperties.Add(areaTriggerCreatePropertiesId, new Vector2(vertices.Read<float>(4), vertices.Read<float>(5)));
                    else if (vertices.IsNull(4) != vertices.IsNull(5))
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_polygon_vertex` has listed invalid target vertices (AreaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}, Index: {vertices.Read<uint>(1)}).");
                }
                while (vertices.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger polygon polygon vertices. DB table `areatrigger_create_properties_polygon_vertex` is empty.");
            }

            //                                         0                              1  2  3
            SQLResult splines = DB.World.Query("SELECT AreaTriggerCreatePropertiesId, X, Y, Z FROM `areatrigger_create_properties_spline_point` ORDER BY `AreaTriggerCreatePropertiesId`, `Idx`");
            if (!splines.IsEmpty())
            {
                do
                {
                    uint areaTriggerCreatePropertiesId = splines.Read<uint>(0);
                    Vector3 spline = new(splines.Read<float>(1), splines.Read<float>(2), splines.Read<float>(3));

                    splinesByCreateProperties.Add(areaTriggerCreatePropertiesId, spline);
                }
                while (splines.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger splines. DB table `areatrigger_create_properties_spline_point` is empty.");
            }

            //                                            0   1             2
            SQLResult templates = DB.World.Query("SELECT Id, IsServerSide, Flags FROM `areatrigger_template`");
            if (!templates.IsEmpty())
            {
                do
                {
                    AreaTriggerTemplate areaTriggerTemplate = new();
                    areaTriggerTemplate.Id = new(templates.Read<uint>(0), templates.Read<byte>(1) == 1);

                    areaTriggerTemplate.Flags = (AreaTriggerFlags)templates.Read<uint>(2);

                    if (areaTriggerTemplate.Id.IsServerSide && areaTriggerTemplate.Flags != 0)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_template` has listed server-side areatrigger (Id: {areaTriggerTemplate.Id.Id}, IsServerSide: {areaTriggerTemplate.Id.IsServerSide}) with none-zero flags");
                        continue;
                    }

                    areaTriggerTemplate.Actions = actionsByAreaTrigger[areaTriggerTemplate.Id];

                    _areaTriggerTemplateStore[areaTriggerTemplate.Id] = areaTriggerTemplate;
                }
                while (templates.NextRow());
            }

            //                                                              0   1              2            3             4             5              6       7          8                  9             10
            SQLResult areatriggerCreateProperties = DB.World.Query("SELECT Id, AreaTriggerId, MoveCurveId, ScaleCurveId, MorphCurveId, FacingCurveId, AnimId, AnimKitId, DecalPropertiesId, TimeToTarget, TimeToTargetScale, " +
                //11     12          13          14          15          16          17          18          19          20
                "Shape, ShapeData0, ShapeData1, ShapeData2, ShapeData3, ShapeData4, ShapeData5, ShapeData6, ShapeData7, ScriptName FROM `areatrigger_create_properties`");
            if (!areatriggerCreateProperties.IsEmpty())
            {
                do
                {
                    AreaTriggerCreateProperties createProperties = new();
                    createProperties.Id = areatriggerCreateProperties.Read<uint>(0);

                    uint areatriggerId = areatriggerCreateProperties.Read<uint>(1);
                    createProperties.Template = GetAreaTriggerTemplate(new AreaTriggerId(areatriggerId, false));

                    AreaTriggerTypes shape = (AreaTriggerTypes)areatriggerCreateProperties.Read<byte>(11);

                    if (areatriggerId != 0 && createProperties.Template == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties` reference invalid AreaTriggerId {areatriggerId} for AreaTriggerCreatePropertiesId {createProperties.Id}");
                        continue;
                    }

                    if (shape >= AreaTriggerTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties` has listed areatrigger create properties {createProperties.Id} with invalid shape {shape}.");
                        continue;
                    }

                    uint ValidateAndSetCurve(uint value)
                    {
                        if (value != 0 && !CliDB.CurveStorage.ContainsKey(value))
                        {
                            Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties` has listed areatrigger (AreaTriggerCreatePropertiesId: {createProperties.Id}, Id: {areatriggerId}) with invalid Curve ({value}), set to 0!");
                            return 0;
                        }

                        return value;
                    }

                    createProperties.MoveCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(2));
                    createProperties.ScaleCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(3));
                    createProperties.MorphCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(4));
                    createProperties.FacingCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(5));

                    createProperties.AnimId = areatriggerCreateProperties.Read<int>(6);
                    createProperties.AnimKitId = areatriggerCreateProperties.Read<uint>(7);
                    createProperties.DecalPropertiesId = areatriggerCreateProperties.Read<uint>(8);

                    createProperties.TimeToTarget = areatriggerCreateProperties.Read<uint>(9);
                    createProperties.TimeToTargetScale = areatriggerCreateProperties.Read<uint>(10);

                    createProperties.Shape.TriggerType = shape;
                    unsafe
                    {
                        for (byte i = 0; i < SharedConst.MaxAreatriggerEntityData; ++i)
                            createProperties.Shape.DefaultDatas.Data[i] = areatriggerCreateProperties.Read<float>(12 + i);
                    }

                    createProperties.ScriptId = Global.ObjectMgr.GetScriptId(areatriggerCreateProperties.Read<string>(20));

                    if (shape == AreaTriggerTypes.Polygon)
                    {
                        if (createProperties.Shape.PolygonDatas.Height <= 0.0f)
                        {
                            createProperties.Shape.PolygonDatas.Height = 1.0f;
                            if (createProperties.Shape.PolygonDatas.HeightTarget <= 0.0f)
                                createProperties.Shape.PolygonDatas.HeightTarget = 1.0f;
                        }
                    }

                    createProperties.PolygonVertices = verticesByCreateProperties[createProperties.Id];
                    createProperties.PolygonVerticesTarget = verticesTargetByCreateProperties[createProperties.Id];
                    if (!createProperties.PolygonVerticesTarget.Empty() && createProperties.PolygonVertices.Count != createProperties.PolygonVerticesTarget.Count)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_polygon_vertex` has invalid target vertices, either all or none vertices must have a corresponding target vertex (AreaTriggerCreatePropertiesId: {createProperties.Id}).");
                        createProperties.PolygonVerticesTarget.Clear();
                    }

                    createProperties.SplinePoints = splinesByCreateProperties[createProperties.Id];

                    _areaTriggerCreateProperties[createProperties.Id] = createProperties;
                }
                while (areatriggerCreateProperties.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger create properties. DB table `areatrigger_create_properties` is empty.");
            }

            //                                                       0                               1           2             3                4             5        6                 7
            SQLResult circularMovementInfos = DB.World.Query("SELECT AreaTriggerCreatePropertiesId, StartDelay, CircleRadius, BlendFromRadius, InitialAngle, ZOffset, CounterClockwise, CanLoop FROM `areatrigger_create_properties_orbit`");
            if (!circularMovementInfos.IsEmpty())
            {
                do
                {
                    uint areaTriggerCreatePropertiesId = circularMovementInfos.Read<uint>(0);

                    var createProperties = _areaTriggerCreateProperties.LookupByKey(areaTriggerCreatePropertiesId);
                    if (createProperties == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_orbit` reference invalid AreaTriggerCreatePropertiesId {areaTriggerCreatePropertiesId}");
                        continue;
                    }

                    AreaTriggerOrbitInfo orbitInfo = new();

                    orbitInfo.StartDelay = circularMovementInfos.Read<uint>(1);
                    orbitInfo.Radius = circularMovementInfos.Read<float>(2);
                    if (!float.IsFinite(orbitInfo.Radius))
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_orbit` has listed areatrigger (AreaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) with invalid Radius ({orbitInfo.Radius}), set to 0!");
                        orbitInfo.Radius = 0.0f;
                    }

                    orbitInfo.BlendFromRadius = circularMovementInfos.Read<float>(3);
                    if (!float.IsFinite(orbitInfo.BlendFromRadius))
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_orbit` has listed areatrigger (AreaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) with invalid BlendFromRadius ({orbitInfo.BlendFromRadius}), set to 0!");
                        orbitInfo.BlendFromRadius = 0.0f;
                    }

                    orbitInfo.InitialAngle = circularMovementInfos.Read<float>(4);
                    if (!float.IsFinite(orbitInfo.InitialAngle))
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_orbit` has listed areatrigger (AreaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) with invalid InitialAngle ({orbitInfo.InitialAngle}), set to 0!");
                        orbitInfo.InitialAngle = 0.0f;
                    }

                    orbitInfo.ZOffset = circularMovementInfos.Read<float>(5);
                    if (!float.IsFinite(orbitInfo.ZOffset))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {areaTriggerCreatePropertiesId}) with invalid ZOffset ({orbitInfo.ZOffset}), set to 0!");
                        orbitInfo.ZOffset = 0.0f;
                    }

                    orbitInfo.CounterClockwise = circularMovementInfos.Read<bool>(6);
                    orbitInfo.CanLoop = circularMovementInfos.Read<bool>(7);

                    createProperties.OrbitInfo = orbitInfo;
                }
                while (circularMovementInfos.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger templates circular movement infos. DB table `areatrigger_create_properties_orbit` is empty.");
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_areaTriggerTemplateStore.Count} spell areatrigger templates in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }

        public void LoadAreaTriggerSpawns()
        {
            // build single time for check spawnmask
            MultiMap<uint, Difficulty> spawnMasks = new();
            foreach (var mapDifficulty in CliDB.MapDifficultyStorage.Values)
                spawnMasks.Add(mapDifficulty.MapID, (Difficulty)mapDifficulty.DifficultyID);

            uint oldMSTime = Time.GetMSTime();
            // Load area trigger positions (to put them on the server)
            //                                         0        1              2             3      4                  5     6     7     8            9              10       11
            SQLResult result = DB.World.Query("SELECT SpawnId, AreaTriggerId, IsServerSide, MapId, SpawnDifficulties, PosX, PosY, PosZ, Orientation, PhaseUseFlags, PhaseId, PhaseGroup, " +
                //12     13          14          15          16          17          18          19          20          21               22
                "Shape, ShapeData0, ShapeData1, ShapeData2, ShapeData3, ShapeData4, ShapeData5, ShapeData6, ShapeData7, SpellForVisuals, ScriptName FROM `areatrigger`");
            if (!result.IsEmpty())
            {
                do
                {
                    ulong spawnId = result.Read<ulong>(0);
                    AreaTriggerId areaTriggerId = new(result.Read<uint>(1), result.Read<byte>(2) == 1);
                    WorldLocation location = new(result.Read<uint>(3), result.Read<float>(5), result.Read<float>(6), result.Read<float>(7), result.Read<float>(8));
                    AreaTriggerTypes shape = (AreaTriggerTypes)result.Read<byte>(12);

                    if (GetAreaTriggerTemplate(areaTriggerId) == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed areatrigger that doesn't exist: Id: {areaTriggerId.Id}, IsServerSide: {areaTriggerId.IsServerSide} for SpawnId {spawnId}");
                        continue;
                    }

                    if (!GridDefines.IsValidMapCoord(location))
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed an invalid position: SpawnId: {spawnId}, MapId: {location.GetMapId()}, Position: {location}");
                        continue;
                    }

                    if (shape >= AreaTriggerTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed areatrigger SpawnId: {spawnId} with invalid shape {shape}.");
                        continue;
                    }

                    var difficulties = Global.ObjectMgr.ParseSpawnDifficulties(result.Read<string>(4), "areatrigger", spawnId, location.GetMapId(), spawnMasks[location.GetMapId()]);
                    if (difficulties.Empty())
                    {
                        Log.outDebug(LogFilter.Sql, $"Table `areatrigger` has areatrigger (GUID: {spawnId}) that is not spawned in any difficulty, skipped.");
                        continue;
                    }

                    AreaTriggerSpawn spawn = new();
                    spawn.SpawnId = spawnId;
                    spawn.MapId = location.GetMapId();
                    spawn.TriggerId = areaTriggerId;
                    spawn.SpawnPoint = new Position(location);

                    spawn.PhaseUseFlags = (PhaseUseFlagsValues)result.Read<byte>(9);
                    spawn.PhaseId = result.Read<uint>(10);
                    spawn.PhaseGroup = result.Read<uint>(11);

                    spawn.Shape.TriggerType = shape;
                    unsafe
                    {
                        for (var i = 0; i < SharedConst.MaxAreatriggerEntityData; ++i)
                            spawn.Shape.DefaultDatas.Data[i] = result.Read<float>(13 + i);
                    }

                    if (!result.IsNull(21))
                    {
                        spawn.SpellForVisuals = result.Read<uint>(21);
                        if (!Global.SpellMgr.HasSpellInfo(spawn.SpellForVisuals.Value, Difficulty.None))
                        {
                            Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed areatrigger SpawnId: {spawnId} with invalid SpellForVisual {spawn.SpellForVisuals}, set to none.");
                            spawn.SpellForVisuals = null;
                        }
                    }

                    spawn.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(22));
                    spawn.spawnGroupData = Global.ObjectMgr.GetLegacySpawnGroup();

                    // Add the trigger to a map::cell map, which is later used by GridLoader to query
                    CellCoord cellCoord = GridDefines.ComputeCellCoord(spawn.SpawnPoint.GetPositionX(), spawn.SpawnPoint.GetPositionY());

                    foreach (Difficulty difficulty in difficulties)
                    {
                        if (!_areaTriggerSpawnsByLocation.ContainsKey((spawn.MapId, difficulty)))
                            _areaTriggerSpawnsByLocation[(spawn.MapId, difficulty)] = new Dictionary<uint, SortedSet<ulong>>();

                        if (!_areaTriggerSpawnsByLocation[(spawn.MapId, difficulty)].ContainsKey(cellCoord.GetId()))
                            _areaTriggerSpawnsByLocation[(spawn.MapId, difficulty)][cellCoord.GetId()] = new SortedSet<ulong>();

                        _areaTriggerSpawnsByLocation[(spawn.MapId, difficulty)][cellCoord.GetId()].Add(spawnId);
                    }

                    // add the position to the map
                    _areaTriggerSpawnsBySpawnId[spawnId] = spawn;
                } while (result.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_areaTriggerSpawnsBySpawnId.Count} areatrigger spawns in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }

        public AreaTriggerTemplate GetAreaTriggerTemplate(AreaTriggerId areaTriggerId)
        {
           return _areaTriggerTemplateStore.LookupByKey(areaTriggerId);
        }

        public AreaTriggerCreateProperties GetAreaTriggerCreateProperties(uint spellMiscValue)
        {
            return _areaTriggerCreateProperties.LookupByKey(spellMiscValue);
        }

        public SortedSet<ulong> GetAreaTriggersForMapAndCell(uint mapId, Difficulty difficulty, uint cellId)
        {
            var atForMapAndDifficulty = _areaTriggerSpawnsByLocation.LookupByKey((mapId, difficulty));
            if (atForMapAndDifficulty != null)
                return atForMapAndDifficulty.LookupByKey(cellId);

            return null;
        }

        public AreaTriggerSpawn GetAreaTriggerSpawn(ulong spawnId)
        {
            return _areaTriggerSpawnsBySpawnId.LookupByKey(spawnId);
        }

        Dictionary<(uint, Difficulty), Dictionary<uint, SortedSet<ulong>>> _areaTriggerSpawnsByLocation = new();
        Dictionary<ulong, AreaTriggerSpawn> _areaTriggerSpawnsBySpawnId = new();
        Dictionary<AreaTriggerId, AreaTriggerTemplate> _areaTriggerTemplateStore = new();
        Dictionary<uint, AreaTriggerCreateProperties> _areaTriggerCreateProperties = new();
    }
}
