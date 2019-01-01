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
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.DataStorage
{
    public class AreaTriggerDataStorage : Singleton<AreaTriggerDataStorage>
    {
        AreaTriggerDataStorage() { }

        public void LoadAreaTriggerTemplates()
        {
            uint oldMSTime = Time.GetMSTime();
            MultiMap<uint, Vector2> verticesByAreaTrigger = new MultiMap<uint, Vector2>();
            MultiMap<uint, Vector2> verticesTargetByAreaTrigger = new MultiMap<uint, Vector2>();
            MultiMap<uint, Vector3> splinesBySpellMisc = new MultiMap<uint, Vector3>();
            MultiMap<uint, AreaTriggerAction> actionsByAreaTrigger = new MultiMap<uint, AreaTriggerAction>();

            //                                                       0              1           2            3
            SQLResult templateActions = DB.World.Query("SELECT AreaTriggerId, ActionType, ActionParam, TargetType FROM `areatrigger_template_actions`");
            if (!templateActions.IsEmpty())
            {
                do
                {
                    uint areaTriggerId = templateActions.Read<uint>(0);

                    AreaTriggerAction action;
                    action.Param = templateActions.Read<uint>(2);
                    action.ActionType = (AreaTriggerActionTypes)templateActions.Read<uint>(1);
                    action.TargetType = (AreaTriggerActionUserTypes)templateActions.Read<uint>(3);

                    if (action.ActionType >= AreaTriggerActionTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, "Table `areatrigger_template_actions` has invalid ActionType ({0}) for AreaTriggerId {1} and Param {2}", action.ActionType, areaTriggerId, action.Param);
                        continue;
                    }

                    if (action.TargetType >= AreaTriggerActionUserTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, "Table `areatrigger_template_actions` has invalid TargetType ({0}) for AreaTriggerId {1} and Param {2}", action.TargetType, areaTriggerId, action.Param);
                        continue;
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
            SQLResult vertices = DB.World.Query("SELECT AreaTriggerId, Idx, VerticeX, VerticeY, VerticeTargetX, VerticeTargetY FROM `areatrigger_template_polygon_vertices` ORDER BY `AreaTriggerId`, `Idx`");
            if (!vertices.IsEmpty())
            {
                do
                {
                    uint areaTriggerId = vertices.Read<uint>(0);

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
            SQLResult splines = DB.World.Query("SELECT SpellMiscId, X, Y, Z FROM `spell_areatrigger_splines` ORDER BY `SpellMiscId`, `Idx`");
            if (!splines.IsEmpty())
            {
                do
                {
                    uint spellMiscId = splines.Read<uint>(0);

                    Vector3 spline = new Vector3(splines.Read<float>(1), splines.Read<float>(2), splines.Read<float>(3));

                    splinesBySpellMisc.Add(spellMiscId, spline);
                }
                while (splines.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger templates splines. DB table `spell_areatrigger_splines` is empty.");
            }

            //                                            0   1     2      3      4      5      6      7      8      9
            SQLResult templates = DB.World.Query("SELECT Id, Type, Flags, Data0, Data1, Data2, Data3, Data4, Data5, ScriptName FROM `areatrigger_template`");
            if (!templates.IsEmpty())
            {
                do
                {
                    AreaTriggerTemplate areaTriggerTemplate = new AreaTriggerTemplate();
                    areaTriggerTemplate.Id = templates.Read<uint>(0);
                    AreaTriggerTypes type = (AreaTriggerTypes)templates.Read<byte>(1);

                    if (type >= AreaTriggerTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, "Table `areatrigger_template` has listed areatrigger (Id: {0}) with invalid type {1}.", areaTriggerTemplate.Id, type);
                        continue;
                    }

                    areaTriggerTemplate.TriggerType = type;
                    areaTriggerTemplate.Flags = (AreaTriggerFlags)templates.Read<uint>(2);

                    unsafe
                    {
                        fixed (float* b = areaTriggerTemplate.DefaultDatas.Data)
                        {
                            for (byte i = 0; i < SharedConst.MaxAreatriggerEntityData; ++i)
                                b[i] = templates.Read<float>(3 + i);
                        }
                    }

                    areaTriggerTemplate.ScriptId = Global.ObjectMgr.GetScriptId(templates.Read<string>(9));
                    areaTriggerTemplate.PolygonVertices = verticesByAreaTrigger[areaTriggerTemplate.Id];
                    areaTriggerTemplate.PolygonVerticesTarget = verticesTargetByAreaTrigger[areaTriggerTemplate.Id];
                    areaTriggerTemplate.Actions = actionsByAreaTrigger[areaTriggerTemplate.Id];

                    areaTriggerTemplate.InitMaxSearchRadius();
                    _areaTriggerTemplateStore[areaTriggerTemplate.Id] = areaTriggerTemplate;
                }
                while (templates.NextRow());
            }

            //                                                        0            1              2            3             4             5              6       7          8                  9             10
            SQLResult areatriggerSpellMiscs = DB.World.Query("SELECT SpellMiscId, AreaTriggerId, MoveCurveId, ScaleCurveId, MorphCurveId, FacingCurveId, AnimId, AnimKitId, DecalPropertiesId, TimeToTarget, TimeToTargetScale FROM `spell_areatrigger`");
            if (!areatriggerSpellMiscs.IsEmpty())
            {
                do
                {
                    AreaTriggerMiscTemplate miscTemplate = new AreaTriggerMiscTemplate();
                    miscTemplate.MiscId = areatriggerSpellMiscs.Read<uint>(0);

                    uint areatriggerId = areatriggerSpellMiscs.Read<uint>(1);
                    miscTemplate.Template = GetAreaTriggerTemplate(areatriggerId);

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
            SQLResult circularMovementInfos = DB.World.Query("SELECT SpellMiscId, StartDelay, CircleRadius, BlendFromRadius, InitialAngle, ZOffset, CounterClockwise, CanLoop FROM `spell_areatrigger_circular` ORDER BY `SpellMiscId`");
            if (!circularMovementInfos.IsEmpty())
            {
                do
                {
                    uint spellMiscId = circularMovementInfos.Read<uint>(0);

                    var atSpellMisc = _areaTriggerTemplateSpellMisc.LookupByKey(spellMiscId);
                    if (atSpellMisc == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` reference invalid SpellMiscId {spellMiscId}");
                        continue;
                    }

                    AreaTriggerCircularMovementInfo circularMovementInfo = new AreaTriggerCircularMovementInfo();

                    circularMovementInfo.StartDelay = circularMovementInfos.Read<uint>(1);
                    circularMovementInfo.Radius = circularMovementInfos.Read<float>(2);
                    if (!float.IsInfinity(circularMovementInfo.Radius))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {spellMiscId}) with invalid Radius ({circularMovementInfo.Radius}), set to 0!");
                        circularMovementInfo.Radius = 0.0f;
                    }

                    circularMovementInfo.BlendFromRadius = circularMovementInfos.Read<float>(3);
                    if (!float.IsInfinity(circularMovementInfo.BlendFromRadius))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {spellMiscId}) with invalid BlendFromRadius ({circularMovementInfo.BlendFromRadius}), set to 0!");
                        circularMovementInfo.BlendFromRadius = 0.0f;
                    }

                    circularMovementInfo.InitialAngle = circularMovementInfos.Read<float>(4);
                    if (!float.IsInfinity(circularMovementInfo.InitialAngle))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {spellMiscId}) with invalid InitialAngle ({circularMovementInfo.InitialAngle}), set to 0!");
                        circularMovementInfo.InitialAngle = 0.0f;
                    }

                    circularMovementInfo.ZOffset = circularMovementInfos.Read<float>(5);
                    if (!float.IsInfinity(circularMovementInfo.ZOffset))
                    {
                        Log.outError(LogFilter.Sql, $"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {spellMiscId}) with invalid ZOffset ({circularMovementInfo.ZOffset}), set to 0!");
                        circularMovementInfo.ZOffset = 0.0f;
                    }

                    circularMovementInfo.CounterClockwise = circularMovementInfos.Read<bool>(6);
                    circularMovementInfo.CanLoop = circularMovementInfos.Read<bool>(7);

                    atSpellMisc.CircularMovementInfo = circularMovementInfo;
                }
                while (circularMovementInfos.NextRow());
            }
            else
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 AreaTrigger templates circular movement infos. DB table `spell_areatrigger_circular` is empty.");
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell areatrigger templates in {1} ms.", _areaTriggerTemplateStore.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public AreaTriggerTemplate GetAreaTriggerTemplate(uint areaTriggerId)
        {
           return _areaTriggerTemplateStore.LookupByKey(areaTriggerId);
        }

        public AreaTriggerMiscTemplate GetAreaTriggerMiscTemplate(uint spellMiscValue)
        {
            return _areaTriggerTemplateSpellMisc.LookupByKey(spellMiscValue);
        }

        Dictionary<uint, AreaTriggerTemplate> _areaTriggerTemplateStore = new Dictionary<uint, AreaTriggerTemplate>();
        Dictionary<uint, AreaTriggerMiscTemplate> _areaTriggerTemplateSpellMisc = new Dictionary<uint, AreaTriggerMiscTemplate>();
    }
}
