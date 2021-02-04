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
using Framework.GameMath;
using Game.Entities;
using Game.Maps;
using System.Collections.Generic;

namespace Game.DataStorage
{
    public class AreaTriggerDataStorage : Singleton<AreaTriggerDataStorage>
    {
        AreaTriggerDataStorage() { }

        public AreaTriggerTemplate GetAreaTriggerServerTemplate(uint areaTriggerId)
        {
            return _areaTriggerTemplateMap.LookupByKey(new AreaTriggerTemplateKey(areaTriggerId, true));
        }

        public SortedSet<ulong> GetAreaTriggersForMapAndCell(uint mapId, uint cellId)
        {
            if (!_areaTriggersGrid.ContainsKey(mapId))
                return null;

            return _areaTriggersGrid[mapId].LookupByKey(cellId);
        }

        public AreaTriggerServerPosition GetAreaTriggerServerPosition(uint spawnId)
        {
            return _areaTriggerSpawnMap.LookupByKey(spawnId);
        }

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


                    if (action.ActionType == AreaTriggerActionTypes.Teleport)
                    {
                        WorldSafeLocsEntry safeLoc = Global.ObjectMgr.GetWorldSafeLoc(action.Param);
                        if (safeLoc == null)
                        {
                            Log.outError(LogFilter.Sql, $"Table `areatrigger_template_actions` has invalid entry ({areaTriggerId}) with TargetType=Teleport and Param ({action.Param}) not a valid world safe loc entry");
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

            //                                            0   1         2     3      4      5      6      7      8      9      10
            SQLResult templates = DB.World.Query("SELECT Id, IsServer, Type, Flags, Data0, Data1, Data2, Data3, Data4, Data5, ScriptName FROM `areatrigger_template`");
            if (!templates.IsEmpty())
            {
                do
                {
                    AreaTriggerTemplate areaTriggerTemplate = new AreaTriggerTemplate();
                    areaTriggerTemplate.Id = templates.Read<uint>(0);
                    bool isServer = templates.Read<byte>(1) == 1;
                    AreaTriggerTypes type = (AreaTriggerTypes)templates.Read<byte>(2);

                    if (type >= AreaTriggerTypes.Max)
                    {
                        Log.outError(LogFilter.Sql, "Table `areatrigger_template` has listed areatrigger (Id: {0}) with invalid type {1}.", areaTriggerTemplate.Id, type);
                        continue;
                    }

                    areaTriggerTemplate.TriggerType = type;
                    areaTriggerTemplate.Flags = (AreaTriggerFlags)templates.Read<uint>(3);

                    if (isServer && areaTriggerTemplate.Flags != 0)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_template` has listed server-side areatrigger (Id: {areaTriggerTemplate.Id}) with none-zero flags");
                        continue;
                    }

                    unsafe
                    {
                        for (byte i = 0; i < SharedConst.MaxAreatriggerEntityData; ++i)
                            areaTriggerTemplate.DefaultDatas.Data[i] = templates.Read<float>(4 + i);
                    }

                    areaTriggerTemplate.ScriptId = Global.ObjectMgr.GetScriptId(templates.Read<string>(10));
                    areaTriggerTemplate.PolygonVertices = verticesByAreaTrigger[areaTriggerTemplate.Id];
                    areaTriggerTemplate.PolygonVerticesTarget = verticesTargetByAreaTrigger[areaTriggerTemplate.Id];
                    areaTriggerTemplate.Actions = actionsByAreaTrigger[areaTriggerTemplate.Id];
                    areaTriggerTemplate.IsServerSide = isServer;

                    areaTriggerTemplate.InitMaxSearchRadius();
                    _areaTriggerTemplateMap[new AreaTriggerTemplateKey(areaTriggerTemplate.Id, isServer)] = areaTriggerTemplate;
                }
                while (templates.NextRow());
            }

            // Load area trigger positions (to put them on the server)
            //                                            0        1   2         3      4     5     6     7        8           9              10
            SQLResult templatePos = DB.World.Query("SELECT SpawnId, Id, IsServer, MapId, PosX, PosY, PosZ, PhaseId, PhaseGroup, PhaseUseFlags, Comment FROM `areatrigger_positions`");
            if (!templatePos.IsEmpty())
    {
                do
                {
                    AreaTriggerServerPosition position = new AreaTriggerServerPosition();
                    position.SpawnId = templatePos.Read<ulong>(0);
                    position.Id = templatePos.Read<uint>(1);
                    position.IsServer = templatePos.Read<byte>(2) == 1;
                    uint mapId = templatePos.Read<uint>(3);

                    float posX = templatePos.Read<float>(4);
                    float posY = templatePos.Read<float>(5);
                    float posZ = templatePos.Read<float>(6);
                    position.Location = new WorldLocation(mapId, posX, posY, posZ);

                    if (!GridDefines.IsValidMapCoord(position.Location))
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_srv_position` has listed an invalid position: Id: {position.Id}, IsServer: {position.IsServer}. MapId ({mapId}), Position ({posX}, {posY}, {posZ})");
                        continue;
                    }

                    position.PhaseId = templatePos.Read<uint>(7);
                    position.PhaseGroup = templatePos.Read<uint>(8);
                    position.PhaseUseFlags = templatePos.Read<byte>(9);

                    var template = _areaTriggerTemplateMap.LookupByKey(new AreaTriggerTemplateKey(position.Id, position.IsServer));
                    if (template == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `areatrigger_srv_position` has listed areatrigger that doesn't exist: Id: {position.Id}, IsServer: {position.IsServer}");
                        continue;
                    }

                    // Add the trigger to a map::cell map, which is later used by GridLoader to query
                    CellCoord cellCoord = GridDefines.ComputeCellCoord(position.Location.GetPositionX(), position.Location.GetPositionY());
                    if (!_areaTriggersGrid.ContainsKey(mapId))
                        _areaTriggersGrid[mapId] = new Dictionary<uint, SortedSet<ulong>>();

                    if (!_areaTriggersGrid[mapId].ContainsKey(cellCoord.GetId()))
                        _areaTriggersGrid[mapId][cellCoord.GetId()] = new SortedSet<ulong>();

                    _areaTriggersGrid[mapId][cellCoord.GetId()].Add(position.SpawnId);

                    // add  the position to the map
                    _areaTriggerSpawnMap[(uint)position.SpawnId] = position;
                } while (templates.NextRow());
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

                    AreaTriggerOrbitInfo orbitInfo = new AreaTriggerOrbitInfo();

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

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell areatrigger templates in {1} ms.", _areaTriggerTemplateMap.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public AreaTriggerTemplate GetAreaTriggerTemplate(uint areaTriggerId)
        {
           return _areaTriggerTemplateMap.LookupByKey(new AreaTriggerTemplateKey(areaTriggerId, false));
        }

        public AreaTriggerMiscTemplate GetAreaTriggerMiscTemplate(uint spellMiscValue)
        {
            return _areaTriggerTemplateSpellMisc.LookupByKey(spellMiscValue);
        }

        Dictionary<uint, Dictionary<uint, SortedSet<ulong>>> _areaTriggersGrid = new Dictionary<uint, Dictionary<uint, SortedSet<ulong>>>();
        Dictionary<uint, AreaTriggerServerPosition> _areaTriggerSpawnMap = new Dictionary<uint, AreaTriggerServerPosition>();
        Dictionary<AreaTriggerTemplateKey, AreaTriggerTemplate> _areaTriggerTemplateMap = new Dictionary<AreaTriggerTemplateKey, AreaTriggerTemplate>();
        Dictionary<uint, AreaTriggerMiscTemplate> _areaTriggerTemplateSpellMisc = new Dictionary<uint, AreaTriggerMiscTemplate>();
    }
}
