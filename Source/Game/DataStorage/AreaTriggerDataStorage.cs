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
            MultiMap<AreaTriggerId, Vector2> verticesByCreateProperties = new();
            MultiMap<AreaTriggerId, Vector2> verticesTargetByCreateProperties = new();
            MultiMap<AreaTriggerId, Vector3> splinesByCreateProperties = new();
            MultiMap<AreaTriggerId, AreaTriggerAction> actionsByAreaTrigger = new();

            //                                                  0              1         2           3            4
            SQLResult templateActions = DB.World.Query("SELECT AreaTriggerId, IsCustom, ActionType, ActionParam, TargetType FROM `areatrigger_template_actions`");
            if (!templateActions.IsEmpty())
            {
                do
                {
                    AreaTriggerId areaTriggerId = new(templateActions.Read<uint>(0), templateActions.Read<bool>(1));

                    AreaTriggerAction action;
                    action.Param = templateActions.Read<uint>(3);
                    action.ActionType = (AreaTriggerActionTypes)templateActions.Read<uint>(2);
                    action.TargetType = (AreaTriggerActionUserTypes)templateActions.Read<uint>(4);

                    if (action.ActionType >= AreaTriggerActionTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_template_actions` has invalid ActionType ({action.ActionType}, for AreaTriggerId ({areaTriggerId.Id},{areaTriggerId.IsCustom}) and Param {action.Param}");
                        continue;
                    }

                    if (action.TargetType >= AreaTriggerActionUserTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_template_actions` has invalid TargetType ({action.TargetType} for AreaTriggerId ({areaTriggerId.Id},{areaTriggerId.IsCustom}) and Param {action.Param}");
                        continue;
                    }


                    if (action.ActionType == AreaTriggerActionTypes.Teleport)
                    {
                        if (Global.ObjectMgr.GetWorldSafeLoc(action.Param) == null)
                        {
                            Log.outError(LogFilter.Sql, $"Table `areatrigger_template_actions` has invalid (Id: {areaTriggerId}, for AreaTriggerId ({areaTriggerId.Id},{areaTriggerId.IsCustom}) with TargetType=Teleport and Param ({action.Param}) not a valid world safe loc entry");
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

            //                                           0                              1         2    3         4         5               6
            SQLResult vertices = DB.World.Query("SELECT AreaTriggerCreatePropertiesId, IsCustom, Idx, VerticeX, VerticeY, VerticeTargetX, VerticeTargetY FROM `areatrigger_create_properties_polygon_vertex` ORDER BY `AreaTriggerCreatePropertiesId`, `IsCustom`, `Idx`");
            if (!vertices.IsEmpty())
            {
                do
                {
                    AreaTriggerId createPropertiesId = new(vertices.Read<uint>(0), vertices.Read<bool>(1));

                    verticesByCreateProperties.Add(createPropertiesId, new Vector2(vertices.Read<float>(3), vertices.Read<float>(4)));

                    if (!vertices.IsNull(5) && !vertices.IsNull(6))
                        verticesTargetByCreateProperties.Add(createPropertiesId, new Vector2(vertices.Read<float>(5), vertices.Read<float>(6)));
                    else if (vertices.IsNull(5) != vertices.IsNull(6))
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_polygon_vertex` has listed invalid target vertices (AreaTriggerCreatePropertiesId: (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}), Index: {vertices.Read<uint>(1)}).");
                }
                while (vertices.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger polygon polygon vertices. DB table `areatrigger_create_properties_polygon_vertex` is empty.");
            }

            //                                         0                              1         2  3  4
            SQLResult splines = DB.World.Query("SELECT AreaTriggerCreatePropertiesId, IsCustom, X, Y, Z FROM `areatrigger_create_properties_spline_point` ORDER BY `AreaTriggerCreatePropertiesId`, `IsCustom`, `Idx`");
            if (!splines.IsEmpty())
            {
                do
                {
                    AreaTriggerId createPropertiesId = new(splines.Read<uint>(0), splines.Read<bool>(1));
                    splinesByCreateProperties.Add(createPropertiesId, new(splines.Read<float>(2), splines.Read<float>(3), splines.Read<float>(4)));
                }
                while (splines.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger splines. DB table `areatrigger_create_properties_spline_point` is empty.");
            }

            //                                            0   1         2      3            4
            SQLResult templates = DB.World.Query("SELECT Id, IsCustom, Flags, ActionSetId, ActionSetFlags FROM `areatrigger_template`");
            if (!templates.IsEmpty())
            {
                do
                {
                    AreaTriggerTemplate areaTriggerTemplate = new();
                    areaTriggerTemplate.Id = new(templates.Read<uint>(0), templates.Read<bool>(1));
                    areaTriggerTemplate.Flags = (AreaTriggerFlag)templates.Read<uint>(2);
                    areaTriggerTemplate.ActionSetId = templates.Read<uint>(3);
                    areaTriggerTemplate.ActionSetFlags = (AreaTriggerActionSetFlag)templates.Read<uint>(4);
                    areaTriggerTemplate.Actions = actionsByAreaTrigger[areaTriggerTemplate.Id];

                    _areaTriggerTemplateStore[areaTriggerTemplate.Id] = areaTriggerTemplate;
                }
                while (templates.NextRow());
            }

            //                                                              0   1         2              3                    4
            SQLResult areatriggerCreateProperties = DB.World.Query("SELECT Id, IsCustom, AreaTriggerId, IsAreatriggerCustom, Flags, " +
                //5            6             7             8              9       10         11                 12               13                 14     15
                "MoveCurveId, ScaleCurveId, MorphCurveId, FacingCurveId, AnimId, AnimKitId, DecalPropertiesId, SpellForVisuals, TimeToTargetScale, Speed, SpeedIsTime, " +
                //16     17          18          19          20          21          22          23          24          25
                "Shape, ShapeData0, ShapeData1, ShapeData2, ShapeData3, ShapeData4, ShapeData5, ShapeData6, ShapeData7, ScriptName FROM `areatrigger_create_properties`");
            if (!areatriggerCreateProperties.IsEmpty())
            {
                do
                {
                    AreaTriggerCreateProperties createProperties = new();
                    AreaTriggerId createPropertiesId = new(areatriggerCreateProperties.Read<uint>(0), areatriggerCreateProperties.Read<bool>(1));
                    createProperties.Id = createPropertiesId;

                    AreaTriggerId areaTriggerId = new(areatriggerCreateProperties.Read<uint>(2), areatriggerCreateProperties.Read<bool>(3));
                    createProperties.Template = GetAreaTriggerTemplate(areaTriggerId);

                    createProperties.Flags = (AreaTriggerCreatePropertiesFlag)areatriggerCreateProperties.Read<uint>(4);

                    AreaTriggerShapeType shape = (AreaTriggerShapeType)areatriggerCreateProperties.Read<byte>(16);

                    if (areaTriggerId.Id != 0 && createProperties.Template == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties` references invalid AreaTrigger (Id: {areaTriggerId.Id}, IsCustom: {areaTriggerId.IsCustom}) for AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom})");
                        continue;
                    }

                    if (shape == AreaTriggerShapeType.Unk || shape >= AreaTriggerShapeType.Max)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties` has listed AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) with invalid shape {shape}.");
                        continue;
                    }

                    uint ValidateAndSetCurve(uint value)
                    {
                        if (value != 0 && !CliDB.CurveStorage.HasRecord(value))
                        {
                            Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties` has listed AreaTrigger (Id: {areaTriggerId.Id}, IsCustom: {areaTriggerId.IsCustom}) for AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) with invalid Curve ({value}), set to 0!");
                            return 0;
                        }

                        return value;
                    }

                    createProperties.MoveCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(5));
                    createProperties.ScaleCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(6));
                    createProperties.MorphCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(7));
                    createProperties.FacingCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(8));

                    createProperties.AnimId = areatriggerCreateProperties.Read<int>(9);
                    createProperties.AnimKitId = areatriggerCreateProperties.Read<uint>(10);
                    createProperties.DecalPropertiesId = areatriggerCreateProperties.Read<uint>(11);

                    if (!areatriggerCreateProperties.IsNull(12))
                    {
                        createProperties.SpellForVisuals = areatriggerCreateProperties.Read<uint>(12);
                        if (!Global.SpellMgr.HasSpellInfo(createProperties.SpellForVisuals.Value, Difficulty.None))
                        {
                            Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties` has AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) with invalid SpellForVisual {createProperties.SpellForVisuals.Value}, set to none.");
                            createProperties.SpellForVisuals = null;
                        }
                    }

                    createProperties.TimeToTargetScale = areatriggerCreateProperties.Read<uint>(13);
                    createProperties.Speed = areatriggerCreateProperties.Read<float>(14);
                    createProperties.SpeedIsTime = areatriggerCreateProperties.Read<bool>(15);

                    float[] shapeData = new float[SharedConst.MaxAreatriggerEntityData];
                    for (byte i = 0; i < SharedConst.MaxAreatriggerEntityData; ++i)
                        shapeData[i] = areatriggerCreateProperties.Read<float>(17 + i);

                    switch (shape)
                    {
                        case AreaTriggerShapeType.Sphere:
                            createProperties.Shape.Data = new AreaTriggerShapeInfo.Sphere(shapeData);
                            break;
                        case AreaTriggerShapeType.Box:
                            createProperties.Shape.Data = new AreaTriggerShapeInfo.Box(shapeData);
                            break;
                        case AreaTriggerShapeType.Polygon:
                            AreaTriggerShapeInfo.Polygon polygon = new AreaTriggerShapeInfo.Polygon(shapeData);
                            if (polygon.Height <= 0.0f)
                            {
                                polygon.Height = 1.0f;
                                if (polygon.HeightTarget <= 0.0f)
                                    polygon.HeightTarget = 1.0f;
                            }
                            var vertices1 = verticesByCreateProperties.LookupByKey(createProperties.Id);
                            if (vertices1 != null)
                                polygon.PolygonVertices = vertices1;
                            vertices1 = verticesTargetByCreateProperties.LookupByKey(createProperties.Id);
                            if (vertices != null)
                                polygon.PolygonVerticesTarget = vertices1;
                            if (!polygon.PolygonVerticesTarget.Empty() && polygon.PolygonVertices.Count != polygon.PolygonVerticesTarget.Count)
                            {
                                Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_polygon_vertex` has invalid target vertices, either all or none vertices must have a corresponding target vertex (AreaTriggerCreatePropertiesId: (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom})).");
                                polygon.PolygonVerticesTarget.Clear();
                            }

                            createProperties.Shape.Data = polygon;
                            break;
                        case AreaTriggerShapeType.Cylinder:
                            createProperties.Shape.Data = new AreaTriggerShapeInfo.Cylinder(shapeData);
                            break;
                        case AreaTriggerShapeType.Disk:
                            createProperties.Shape.Data = new AreaTriggerShapeInfo.Disk(shapeData);
                            break;
                        case AreaTriggerShapeType.BoundedPlane:
                            createProperties.Shape.Data = new AreaTriggerShapeInfo.BoundedPlane(shapeData);
                            break;
                        default:
                            break;
                    }

                    createProperties.ScriptId = Global.ObjectMgr.GetScriptId(areatriggerCreateProperties.Read<string>(25));

                    var spline = splinesByCreateProperties.LookupByKey(createProperties.Id);
                    if (spline != null)
                        createProperties.Movement = spline;

                    _areaTriggerCreateProperties[createProperties.Id] = createProperties;
                }
                while (areatriggerCreateProperties.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger create properties. DB table `areatrigger_create_properties` is empty.");
            }

            //                                                        0                              1         2                     3             4                5             6        7                 8
            SQLResult circularMovementInfos = DB.World.Query("SELECT AreaTriggerCreatePropertiesId, IsCustom, ExtraTimeForBlending, CircleRadius, BlendFromRadius, InitialAngle, ZOffset, CounterClockwise, CanLoop FROM `areatrigger_create_properties_orbit`");
            if (!circularMovementInfos.IsEmpty())
            {
                do
                {
                    AreaTriggerId createPropertiesId = new(circularMovementInfos.Read<uint>(0), circularMovementInfos.Read<bool>(1));

                    var createProperties = _areaTriggerCreateProperties.LookupByKey(createPropertiesId);
                    if (createProperties == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_orbit` reference invalid AreaTriggerCreatePropertiesId: (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom})");
                        continue;
                    }

                    AreaTriggerOrbitInfo orbitInfo = new();

                    orbitInfo.ExtraTimeForBlending = circularMovementInfos.Read<int>(2);

                    float ValidateAndSetFloat(float value)
                    {
                        if (!float.IsFinite(value))
                        {
                            Log.outError(LogFilter.Sql, $"Table `areatrigger_create_properties_orbit` has listed areatrigger (AreaTriggerCreatePropertiesId: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) with invalid Float ({value}), set to 0!");
                            return 0;
                        }

                        return value;
                    }

                    orbitInfo.Radius = ValidateAndSetFloat(circularMovementInfos.Read<float>(3));
                    orbitInfo.BlendFromRadius = circularMovementInfos.Read<float>(4);
                    orbitInfo.InitialAngle = circularMovementInfos.Read<float>(5);
                    orbitInfo.ZOffset = circularMovementInfos.Read<float>(6);

                    orbitInfo.CounterClockwise = circularMovementInfos.Read<bool>(7);
                    orbitInfo.CanLoop = circularMovementInfos.Read<bool>(8);

                    createProperties.Movement = orbitInfo;
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
            //                                         0        1                              2         3      4                  5     6     7     8            9              10       11          12
            SQLResult result = DB.World.Query("SELECT SpawnId, AreaTriggerCreatePropertiesId, IsCustom, MapId, SpawnDifficulties, PosX, PosY, PosZ, Orientation, PhaseUseFlags, PhaseId, PhaseGroup, ScriptName FROM `areatrigger`");
            if (!result.IsEmpty())
            {
                do
                {
                    ulong spawnId = result.Read<ulong>(0);
                    AreaTriggerId createPropertiesId = new(result.Read<uint>(1), result.Read<bool>(2));
                    WorldLocation location = new(result.Read<uint>(3), result.Read<float>(5), result.Read<float>(6), result.Read<float>(7), result.Read<float>(8));

                    AreaTriggerCreateProperties createProperties = GetAreaTriggerCreateProperties(createPropertiesId);
                    if (createProperties == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) that doesn't exist for SpawnId {spawnId}");
                        continue;
                    }

                    if (createProperties.Flags != AreaTriggerCreatePropertiesFlag.None)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) with non - zero flags");
                        continue;
                    }

                    if (createProperties.ScaleCurveId != 0 || createProperties.MorphCurveId != 0 || createProperties.FacingCurveId != 0 || createProperties.MoveCurveId != 0)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) with curve values");
                        continue;
                    }

                    if (createProperties.TimeToTargetScale != 0)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) with time to target values");
                        continue;
                    }

                    if (!createProperties.Movement.IsT0)
                    {
                        string movementType = createProperties.Movement.Match(
                            _ => "",
                            splineInfo => "spline",
                            OrbitInfo => "orbit"
                        );

                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed AreaTriggerCreatePropertiesId (Id: {createPropertiesId.Id}, IsCustom: {createPropertiesId.IsCustom}) with {movementType}");
                        continue;
                    }

                    if (!GridDefines.IsValidMapCoord(location))
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger` has listed an invalid position: SpawnId: {spawnId}, MapId {location.GetMapId()}, Position {location}");
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
                    spawn.Id = createPropertiesId;
                    spawn.SpawnPoint = new Position(location);

                    spawn.PhaseUseFlags = (PhaseUseFlagsValues)result.Read<byte>(9);
                    spawn.PhaseId = result.Read<uint>(10);
                    spawn.PhaseGroup = result.Read<uint>(11);

                    spawn.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(12));
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

        public AreaTriggerCreateProperties GetAreaTriggerCreateProperties(AreaTriggerId areaTriggerCreatePropertiesId)
        {
            return _areaTriggerCreateProperties.LookupByKey(areaTriggerCreatePropertiesId);
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
        Dictionary<AreaTriggerId, AreaTriggerCreateProperties> _areaTriggerCreateProperties = new();
    }
}
