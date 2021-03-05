﻿/*
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
using Framework.GameMath;
using Game.Entities;
using Game.Maps;
using System.Collections.Generic;

namespace Game.DataStorage
{
    public class AreaTriggerDataStorage : Singleton<AreaTriggerDataStorage>
    {
        AreaTriggerDataStorage() { }

        public void LoadAreaTriggerTemplates()
        {
            var oldMSTime = Time.GetMSTime();
            var verticesByAreaTrigger = new MultiMap<uint, Vector2>();
            var verticesTargetByAreaTrigger = new MultiMap<uint, Vector2>();
            var splinesBySpellMisc = new MultiMap<uint, Vector3>();
            var actionsByAreaTrigger = new MultiMap<AreaTriggerId, AreaTriggerAction>();

            //                                                       0         1             2            3           4
            var templateActions = DB.World.Query("SELECT AreaTriggerId, IsServerSide, ActionType, ActionParam, TargetType FROM `areatrigger_template_actions`");
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

            //                                           0              1    2         3         4               5
            var vertices = DB.World.Query("SELECT AreaTriggerId, Idx, VerticeX, VerticeY, VerticeTargetX, VerticeTargetY FROM `areatrigger_template_polygon_vertices` ORDER BY `AreaTriggerId`, `Idx`");
            if (!vertices.IsEmpty())
            {
                do
                {
                    var areaTriggerId = vertices.Read<uint>(0);

                    verticesByAreaTrigger.Add(areaTriggerId, new Vector2(vertices.Read<float>(2), vertices.Read<float>(3)));

                    if (!vertices.IsNull(4) && !vertices.IsNull(5))
                        verticesTargetByAreaTrigger.Add(areaTriggerId, new Vector2(vertices.Read<float>(4), vertices.Read<float>(5)));
                    else if (vertices.IsNull(4) != vertices.IsNull(5))
                        Log.outError(LogFilter.Sql, "Table `areatrigger_template_polygon_vertices` has listed invalid target vertices (AreaTrigger: {0}, Index: {1}).", areaTriggerId, vertices.Read<uint>(1));
                }
                while (vertices.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger templates polygon vertices. DB table `areatrigger_template_polygon_vertices` is empty.");
            }

            //                                         0            1  2  3
            var splines = DB.World.Query("SELECT SpellMiscId, X, Y, Z FROM `spell_areatrigger_splines` ORDER BY `SpellMiscId`, `Idx`");
            if (!splines.IsEmpty())
            {
                do
                {
                    var spellMiscId = splines.Read<uint>(0);

                    var spline = new Vector3(splines.Read<float>(1), splines.Read<float>(2), splines.Read<float>(3));

                    splinesBySpellMisc.Add(spellMiscId, spline);
                }
                while (splines.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger templates splines. DB table `spell_areatrigger_splines` is empty.");
            }

            //                                            0   1             2     3      4      5      6      7      8      9      10
            var templates = DB.World.Query("SELECT Id, IsServerSide, Type, Flags, Data0, Data1, Data2, Data3, Data4, Data5, ScriptName FROM `areatrigger_template`");
            if (!templates.IsEmpty())
            {
                do
                {
                    var areaTriggerTemplate = new AreaTriggerTemplate();
                    areaTriggerTemplate.Id = new(templates.Read<uint>(0), templates.Read<byte>(1) == 1);
                    var type = (AreaTriggerTypes)templates.Read<byte>(2);

                    if (type >= AreaTriggerTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_template` has listed areatrigger (Id: {areaTriggerTemplate.Id.Id}, IsServerSide: {areaTriggerTemplate.Id.IsServerSide}) with invalid type {type}.");
                        continue;
                    }

                    areaTriggerTemplate.TriggerType = type;
                    areaTriggerTemplate.Flags = (AreaTriggerFlags)templates.Read<uint>(3);

                    if (areaTriggerTemplate.Id.IsServerSide && areaTriggerTemplate.Flags != 0)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_template` has listed server-side areatrigger (Id: {areaTriggerTemplate.Id.Id}, IsServerSide: {areaTriggerTemplate.Id.IsServerSide}) with none-zero flags");
                        continue;
                    }

                    unsafe
                    {
                        for (byte i = 0; i < SharedConst.MaxAreatriggerEntityData; ++i)
                            areaTriggerTemplate.DefaultDatas.Data[i] = templates.Read<float>(4 + i);
                    }

                    areaTriggerTemplate.ScriptId = Global.ObjectMgr.GetScriptId(templates.Read<string>(10));
                    if (!areaTriggerTemplate.Id.IsServerSide)
                    {
                        areaTriggerTemplate.PolygonVertices = verticesByAreaTrigger[areaTriggerTemplate.Id.Id];
                        areaTriggerTemplate.PolygonVerticesTarget = verticesTargetByAreaTrigger[areaTriggerTemplate.Id.Id];
                    }
                    areaTriggerTemplate.Actions = actionsByAreaTrigger[areaTriggerTemplate.Id];

                    areaTriggerTemplate.InitMaxSearchRadius();
                    _areaTriggerTemplateStore[areaTriggerTemplate.Id] = areaTriggerTemplate;
                }
                while (templates.NextRow());
            }

            //                                                        0            1              2            3             4             5              6       7          8                  9             10
            var areatriggerSpellMiscs = DB.World.Query("SELECT SpellMiscId, AreaTriggerId, MoveCurveId, ScaleCurveId, MorphCurveId, FacingCurveId, AnimId, AnimKitId, DecalPropertiesId, TimeToTarget, TimeToTargetScale FROM `spell_areatrigger`");
            if (!areatriggerSpellMiscs.IsEmpty())
            {
                do
                {
                    var miscTemplate = new AreaTriggerMiscTemplate();
                    miscTemplate.MiscId = areatriggerSpellMiscs.Read<uint>(0);

                    var areatriggerId = areatriggerSpellMiscs.Read<uint>(1);
                    miscTemplate.Template = GetAreaTriggerTemplate(new AreaTriggerId(areatriggerId, false));

                    if (miscTemplate.Template == null)
                    {
                        Log.outError(LogFilter.Sql, "Table `spell_areatrigger` reference invalid AreaTriggerId {0} for miscId {1}", areatriggerId, miscTemplate.MiscId);
                        continue;
                    }

                    uint ValidateAndSetCurve(uint value)
                    {
                        if (value != 0 && !CliDB.CurveStorage.ContainsKey(value))
                        {
                            Log.outError(LogFilter.Sql, "Table `spell_areatrigger` has listed areatrigger (MiscId: {0}, Id: {1}) with invalid Curve ({2}), set to 0!", miscTemplate.MiscId, areatriggerId, value);
                            return 0;
                        }

                        return value;
                    }

                    miscTemplate.MoveCurveId = ValidateAndSetCurve(areatriggerSpellMiscs.Read<uint>(2));
                    miscTemplate.ScaleCurveId = ValidateAndSetCurve(areatriggerSpellMiscs.Read<uint>(3));
                    miscTemplate.MorphCurveId = ValidateAndSetCurve(areatriggerSpellMiscs.Read<uint>(4));
                    miscTemplate.FacingCurveId = ValidateAndSetCurve(areatriggerSpellMiscs.Read<uint>(5));

                    miscTemplate.AnimId = areatriggerSpellMiscs.Read<uint>(6);
                    miscTemplate.AnimKitId = areatriggerSpellMiscs.Read<uint>(7);
                    miscTemplate.DecalPropertiesId = areatriggerSpellMiscs.Read<uint>(8);

                    miscTemplate.TimeToTarget = areatriggerSpellMiscs.Read<uint>(9);
                    miscTemplate.TimeToTargetScale = areatriggerSpellMiscs.Read<uint>(10);

                    miscTemplate.SplinePoints = splinesBySpellMisc[miscTemplate.MiscId];

                    _areaTriggerTemplateSpellMisc[miscTemplate.MiscId] = miscTemplate;
                }
                while (areatriggerSpellMiscs.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Spell AreaTrigger templates. DB table `spell_areatrigger` is empty.");
            }

            //                                                       0            1           2             3                4             5        6                 7
            var circularMovementInfos = DB.World.Query("SELECT SpellMiscId, StartDelay, CircleRadius, BlendFromRadius, InitialAngle, ZOffset, CounterClockwise, CanLoop FROM `spell_areatrigger_circular` ORDER BY `SpellMiscId`");
            if (!circularMovementInfos.IsEmpty())
            {
                do
                {
                    var spellMiscId = circularMovementInfos.Read<uint>(0);

                    var atSpellMisc = _areaTriggerTemplateSpellMisc.LookupByKey(spellMiscId);
                    if (atSpellMisc == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` reference invalid SpellMiscId {spellMiscId}");
                        continue;
                    }

                    var orbitInfo = new AreaTriggerOrbitInfo();

                    orbitInfo.StartDelay = circularMovementInfos.Read<uint>(1);
                    orbitInfo.Radius = circularMovementInfos.Read<float>(2);
                    if (!float.IsInfinity(orbitInfo.Radius))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {spellMiscId}) with invalid Radius ({orbitInfo.Radius}), set to 0!");
                        orbitInfo.Radius = 0.0f;
                    }

                    orbitInfo.BlendFromRadius = circularMovementInfos.Read<float>(3);
                    if (!float.IsInfinity(orbitInfo.BlendFromRadius))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {spellMiscId}) with invalid BlendFromRadius ({orbitInfo.BlendFromRadius}), set to 0!");
                        orbitInfo.BlendFromRadius = 0.0f;
                    }

                    orbitInfo.InitialAngle = circularMovementInfos.Read<float>(4);
                    if (!float.IsInfinity(orbitInfo.InitialAngle))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {spellMiscId}) with invalid InitialAngle ({orbitInfo.InitialAngle}), set to 0!");
                        orbitInfo.InitialAngle = 0.0f;
                    }

                    orbitInfo.ZOffset = circularMovementInfos.Read<float>(5);
                    if (!float.IsInfinity(orbitInfo.ZOffset))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {spellMiscId}) with invalid ZOffset ({orbitInfo.ZOffset}), set to 0!");
                        orbitInfo.ZOffset = 0.0f;
                    }

                    orbitInfo.CounterClockwise = circularMovementInfos.Read<bool>(6);
                    orbitInfo.CanLoop = circularMovementInfos.Read<bool>(7);

                    atSpellMisc.OrbitInfo = orbitInfo;
                }
                while (circularMovementInfos.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger templates circular movement infos. DB table `spell_areatrigger_circular` is empty.");
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_areaTriggerTemplateStore.Count} spell areatrigger templates in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }

        public void LoadAreaTriggerSpawns()
        {
            var oldMSTime = Time.GetMSTime();
            // Load area trigger positions (to put them on the server)
            //                                            0        1              2             3      4     5     6     7            8              9        10
            var templates = DB.World.Query("SELECT SpawnId, AreaTriggerId, IsServerSide, MapId, PosX, PosY, PosZ, Orientation, PhaseUseFlags, PhaseId, PhaseGroup FROM `areatrigger`");
            if (!templates.IsEmpty())
            {
                do
                {
                    var spawnId = templates.Read<ulong>(0);
                    AreaTriggerId areaTriggerId = new(templates.Read<uint>(1), templates.Read<byte>(2) == 1);
                    WorldLocation location = new(templates.Read<uint>(3), templates.Read<float>(4), templates.Read<float>(5), templates.Read<float>(6), templates.Read<float>(7));

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

                    var spawn = new AreaTriggerSpawn();
                    spawn.SpawnId = spawnId;
                    spawn.Id = areaTriggerId;
                    spawn.Location = new WorldLocation(location);

                    spawn.PhaseUseFlags = templates.Read<byte>(8);
                    spawn.PhaseId = templates.Read<uint>(9);
                    spawn.PhaseGroup = templates.Read<uint>(10);

                    // Add the trigger to a map::cell map, which is later used by GridLoader to query
                    var cellCoord = GridDefines.ComputeCellCoord(spawn.Location.GetPositionX(), spawn.Location.GetPositionY());
                    if (!_areaTriggerSpawnsByLocation.ContainsKey((spawn.Location.GetMapId(), cellCoord.GetId())))
                        _areaTriggerSpawnsByLocation[(spawn.Location.GetMapId(), cellCoord.GetId())] = new SortedSet<ulong>();

                    _areaTriggerSpawnsByLocation[(spawn.Location.GetMapId(), cellCoord.GetId())].Add(spawnId);

                    // add the position to the map
                    _areaTriggerSpawnsBySpawnId[spawnId] = spawn;
                } while (templates.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_areaTriggerSpawnsBySpawnId.Count} areatrigger spawns in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }

        public AreaTriggerTemplate GetAreaTriggerTemplate(AreaTriggerId areaTriggerId)
        {
           return _areaTriggerTemplateStore.LookupByKey(areaTriggerId);
        }

        public AreaTriggerMiscTemplate GetAreaTriggerMiscTemplate(uint spellMiscValue)
        {
            return _areaTriggerTemplateSpellMisc.LookupByKey(spellMiscValue);
        }

        public SortedSet<ulong> GetAreaTriggersForMapAndCell(uint mapId, uint cellId)
        {
            return _areaTriggerSpawnsByLocation.LookupByKey((mapId, cellId));
        }

        public AreaTriggerSpawn GetAreaTriggerSpawn(ulong spawnId)
        {
            return _areaTriggerSpawnsBySpawnId.LookupByKey(spawnId);
        }

        Dictionary<(uint mapId, uint cellId), SortedSet<ulong>> _areaTriggerSpawnsByLocation = new Dictionary<(uint mapId, uint cellId), SortedSet<ulong>>();
        Dictionary<ulong, AreaTriggerSpawn> _areaTriggerSpawnsBySpawnId = new Dictionary<ulong, AreaTriggerSpawn>();
        Dictionary<AreaTriggerId, AreaTriggerTemplate> _areaTriggerTemplateStore = new Dictionary<AreaTriggerId, AreaTriggerTemplate>();
        Dictionary<uint, AreaTriggerMiscTemplate> _areaTriggerTemplateSpellMisc = new Dictionary<uint, AreaTriggerMiscTemplate>();
    }
}
