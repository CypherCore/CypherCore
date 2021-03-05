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
using Framework.Dynamic;
using Framework.GameMath;
using Game.AI;
using Game.Maps;
using Game.Movement;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public class AreaTrigger : WorldObject
    {
        public AreaTrigger() : base(false)
        {
            _previousCheckOrientation = float.PositiveInfinity;
            _reachedDestination = true;

            ObjectTypeMask |= TypeMask.AreaTrigger;
            ObjectTypeId = TypeId.AreaTrigger;

            m_updateFlag.Stationary = true;
            m_updateFlag.AreaTrigger = true;

            m_areaTriggerData = new AreaTriggerFieldData();

            _spline = new Spline();
        }

        public override void AddToWorld()
        {
            // Register the AreaTrigger for guid lookup and for caster
            if (!IsInWorld)
            {
                GetMap().GetObjectsStore().Add(GetGUID(), this);
                base.AddToWorld();
            }
        }

        public override void RemoveFromWorld()
        {
            // Remove the AreaTrigger from the accessor and from all lists of objects in world
            if (IsInWorld)
            {
                _isRemoved = true;

                var caster = GetCaster();
                if (caster)
                    caster._UnregisterAreaTrigger(this);

                // Handle removal of all units, calling OnUnitExit & deleting auras if needed
                HandleUnitEnterExit(new List<Unit>());

                _ai.OnRemove();

                base.RemoveFromWorld();
                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        private bool Create(uint spellMiscId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, SpellCastVisualField spellVisual, ObjectGuid castId, AuraEffect aurEff)
        {
            _targetGuid = target ? target.GetGUID() : ObjectGuid.Empty;
            _aurEff = aurEff;

            SetMap(caster.GetMap());
            Relocate(pos);
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (spell {spell.Id}) not created. Invalid coordinates (X: {GetPositionX()} Y: {GetPositionY()})");
                return false;
            }

            _areaTriggerMiscTemplate = Global.AreaTriggerDataStorage.GetAreaTriggerMiscTemplate(spellMiscId);
            if (_areaTriggerMiscTemplate == null)
            {
                Log.outError(LogFilter.AreaTrigger, "AreaTrigger (spellMiscId {0}) not created. Invalid areatrigger miscid ({1})", spellMiscId, spellMiscId);
                return false;
            }

            _areaTriggerTemplate = _areaTriggerMiscTemplate.Template;

            _Create(ObjectGuid.Create(HighGuid.AreaTrigger, GetMapId(), GetTemplate().Id.Id, caster.GetMap().GenerateLowGuid(HighGuid.AreaTrigger)));

            SetEntry(GetTemplate().Id.Id);
            SetDuration(duration);

            SetObjectScale(1.0f);

            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.Caster), caster.GetGUID());
            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.CreatingEffectGUID), castId);

            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.SpellID), spell.Id);
            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.SpellForVisuals), spell.Id);

            SpellCastVisualField spellCastVisual = m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.SpellVisual);
            SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, spellVisual.SpellXSpellVisualID);
            SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, spellVisual.ScriptVisualID);

            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTargetScale), GetMiscTemplate().TimeToTargetScale != 0 ? GetMiscTemplate().TimeToTargetScale : m_areaTriggerData.Duration);
            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.BoundsRadius2D), GetTemplate().MaxSearchRadius);
            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.DecalPropertiesID), GetMiscTemplate().DecalPropertiesId);

            ScaleCurve extraScaleCurve = m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.ExtraScaleCurve);

            if (GetMiscTemplate().ExtraScale.Structured.StartTimeOffset != 0)
                SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.StartTimeOffset), GetMiscTemplate().ExtraScale.Structured.StartTimeOffset);
            if (GetMiscTemplate().ExtraScale.Structured.X != 0 || GetMiscTemplate().ExtraScale.Structured.Y != 0)
            {
                var point = new Vector2(GetMiscTemplate().ExtraScale.Structured.X, GetMiscTemplate().ExtraScale.Structured.Y);
                SetUpdateFieldValue(ref extraScaleCurve.ModifyValue(extraScaleCurve.Points, 0), point);
            }
            if (GetMiscTemplate().ExtraScale.Structured.Z != 0 || GetMiscTemplate().ExtraScale.Structured.W != 0)
            {
                var point = new Vector2(GetMiscTemplate().ExtraScale.Structured.Z, GetMiscTemplate().ExtraScale.Structured.W);
                SetUpdateFieldValue(ref extraScaleCurve.ModifyValue(extraScaleCurve.Points, 1), point);
            }
            unsafe
            {
                if (GetMiscTemplate().ExtraScale.Raw.Data[5] != 0)
                    SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.ParameterCurve), GetMiscTemplate().ExtraScale.Raw.Data[5]);
                if (GetMiscTemplate().ExtraScale.Structured.OverrideActive != 0)
                    SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.OverrideActive), GetMiscTemplate().ExtraScale.Structured.OverrideActive != 0 ? true : false);
            }


            PhasingHandler.InheritPhaseShift(this, caster);

            if (target && GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
            {
                m_movementInfo.transport.guid = target.GetGUID();
            }

            UpdateShape();

            var timeToTarget = GetMiscTemplate().TimeToTarget != 0 ? GetMiscTemplate().TimeToTarget : m_areaTriggerData.Duration;

            if (GetTemplate().HasFlag(AreaTriggerFlags.HasCircularMovement))
            {
                var cmi = GetMiscTemplate().OrbitInfo;
                if (target && GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
                    cmi.PathTarget.Set(target.GetGUID());
                else
                    cmi.Center.Set(new Vector3(pos.posX, pos.posY, pos.posZ));

                InitOrbit(cmi, timeToTarget);
            }
            else if (GetMiscTemplate().HasSplines())
            {
                InitSplineOffsets(GetMiscTemplate().SplinePoints, timeToTarget);
            }

            // movement on transport of areatriggers on unit is handled by themself
            var transport = m_movementInfo.transport.guid.IsEmpty() ? caster.GetTransport() : null;
            if (transport)
            {
                float x, y, z, o;
                pos.GetPosition(out x, out y, out z, out o);
                transport.CalculatePassengerOffset(ref x, ref y, ref z, ref o);
                m_movementInfo.transport.pos.Relocate(x, y, z, o);

                // This object must be added to transport before adding to map for the client to properly display it
                transport.AddPassenger(this);
            }

            AI_Initialize();

            // Relocate areatriggers with circular movement again
            if (HasOrbit())
                Relocate(CalculateOrbitPosition());

            if (!GetMap().AddToMap(this))
            {         // Returning false will cause the object to be deleted - remove from transport
                if (transport)
                    transport.RemovePassenger(this);
                return false;
            }

            caster._RegisterAreaTrigger(this);

            _ai.OnCreate();

            return true;
        }

        public static AreaTrigger CreateAreaTrigger(uint spellMiscId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, SpellCastVisualField spellVisual, ObjectGuid castId = default, AuraEffect aurEff = null)
        {
            var at = new AreaTrigger();
            if (!at.Create(spellMiscId, caster, target, spell, pos, duration, spellVisual, castId, aurEff))
                return null;

            return at;
        }

        public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
        {
            var position = Global.AreaTriggerDataStorage.GetAreaTriggerSpawn(spawnId);
            if (position == null)
                return false;

            var areaTriggerTemplate = Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(position.Id);
            if (areaTriggerTemplate == null)
                return false;

            return CreateServer(map, areaTriggerTemplate, position);
        }

        private bool CreateServer(Map map, AreaTriggerTemplate areaTriggerTemplate, AreaTriggerSpawn position)
        {
            SetMap(map);
            Relocate(position.Location);
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.AreaTrigger, $"AreaTriggerServer (id {areaTriggerTemplate.Id}) not created. Invalid coordinates (X: {GetPositionX()} Y: {GetPositionY()})");
                return false;
            }

            _areaTriggerTemplate = areaTriggerTemplate;

            _Create(ObjectGuid.Create(HighGuid.AreaTrigger, GetMapId(), areaTriggerTemplate.Id.Id, GetMap().GenerateLowGuid(HighGuid.AreaTrigger)));

            SetEntry(areaTriggerTemplate.Id.Id);

            SetObjectScale(1.0f);

            if (position.PhaseUseFlags != 0 || position.PhaseId != 0 || position.PhaseGroup != 0)
                PhasingHandler.InitDbPhaseShift(GetPhaseShift(), (PhaseUseFlagsValues)position.PhaseUseFlags, position.PhaseId, position.PhaseGroup);

            UpdateShape();

            AI_Initialize();

            _ai.OnCreate();

            return true;
        }
        
        public override void Update(uint diff)
        {
            base.Update(diff);
            _timeSinceCreated += diff;

            if (!IsServerSide())
            {
                // "If" order matter here, Orbit > Attached > Splines
                if (HasOrbit())
                {
                    UpdateOrbitPosition(diff);
                }
                else if (GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
                {
                    var target = GetTarget();
                    if (target)
                        GetMap().AreaTriggerRelocation(this, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), target.GetOrientation());
                }
                else
                    UpdateSplinePosition(diff);

                if (GetDuration() != -1)
                {
                    if (GetDuration() > diff)
                        _UpdateDuration((int)(_duration - diff));
                    else
                    {
                        Remove(); // expired
                        return;
                    }
                }
            }

            _ai.OnUpdate(diff);

            UpdateTargetList();
        }

        public void Remove()
        {
            if (IsInWorld)           
                AddObjectToRemoveList();
        }

        public void SetDuration(int newDuration)
        {
            _duration = newDuration;
            _totalDuration = newDuration;

            // negative duration (permanent areatrigger) sent as 0
            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.Duration), (uint)Math.Max(newDuration, 0));
        }

        private void _UpdateDuration(int newDuration)
        {
            _duration = newDuration;

            // should be sent in object create packets only
            DoWithSuppressingObjectUpdates(() =>
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.Duration), (uint)_duration);
                m_areaTriggerData.ClearChanged(m_areaTriggerData.Duration);
            });
        }

        private float GetProgress()
        {
            return GetTimeSinceCreated() < GetTimeToTargetScale() ? (float)GetTimeSinceCreated() / GetTimeToTargetScale() : 1.0f;
        }

        private void UpdateTargetList()
        {
            var targetList = new List<Unit>();

            switch (GetTemplate().TriggerType)
            {
                case AreaTriggerTypes.Sphere:
                    SearchUnitInSphere(targetList);
                    break;
                case AreaTriggerTypes.Box:
                    SearchUnitInBox(targetList);
                    break;
                case AreaTriggerTypes.Polygon:
                    SearchUnitInPolygon(targetList);
                    break;
                case AreaTriggerTypes.Cylinder:
                    SearchUnitInCylinder(targetList);
                    break;
                default:
                    break;
            }

            HandleUnitEnterExit(targetList);
        }

        private void SearchUnits(List<Unit> targetList, float radius, bool check3D)
        {
            var check = new AnyUnitInObjectRangeCheck(this, radius, check3D);
            if (IsServerSide())
            {
                var searcher = new PlayerListSearcher(this, targetList, check);
                Cell.VisitWorldObjects(this, searcher, GetTemplate().MaxSearchRadius);
            }
            else
            {
                var searcher = new UnitListSearcher(this, targetList, check);
                Cell.VisitAllObjects(this, searcher, GetTemplate().MaxSearchRadius);
            }
        }

        private void SearchUnitInSphere(List<Unit> targetList)
        {
            var radius = GetTemplate().SphereDatas.Radius;
            if (GetTemplate().HasFlag(AreaTriggerFlags.HasDynamicShape))
            {
                if (GetMiscTemplate().MorphCurveId != 0)
                {
                    radius = MathFunctions.lerp(GetTemplate().SphereDatas.Radius, GetTemplate().SphereDatas.RadiusTarget, Global.DB2Mgr.GetCurveValueAt(GetMiscTemplate().MorphCurveId, GetProgress()));
                }
            }

            SearchUnits(targetList, radius, true);
        }

        private void SearchUnitInBox(List<Unit> targetList)
        {
            float extentsX, extentsY, extentsZ;

            unsafe
            {
                extentsX = GetTemplate().BoxDatas.Extents[0];
                extentsY = GetTemplate().BoxDatas.Extents[1];
                extentsZ = GetTemplate().BoxDatas.Extents[2];
            }

            SearchUnits(targetList, GetTemplate().MaxSearchRadius, false);

            var halfExtentsX = extentsX / 2.0f;
            var halfExtentsY = extentsY / 2.0f;
            var halfExtentsZ = extentsZ / 2.0f;

            var minX = GetPositionX() - halfExtentsX;
            var maxX = GetPositionX() + halfExtentsX;

            var minY = GetPositionY() - halfExtentsY;
            var maxY = GetPositionY() + halfExtentsY;

            var minZ = GetPositionZ() - halfExtentsZ;
            var maxZ = GetPositionZ() + halfExtentsZ;

            var box = new AxisAlignedBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));

            targetList.RemoveAll(unit => !box.contains(new Vector3(unit.GetPositionX(), unit.GetPositionY(), unit.GetPositionZ())));
        }

        private void SearchUnitInPolygon(List<Unit> targetList)
        {
            SearchUnits(targetList, GetTemplate().MaxSearchRadius, false);

            var height = GetTemplate().PolygonDatas.Height;
            var minZ = GetPositionZ() - height;
            var maxZ = GetPositionZ() + height;

            targetList.RemoveAll(unit => !CheckIsInPolygon2D(unit) || unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        private void SearchUnitInCylinder(List<Unit> targetList)
        {
            SearchUnits(targetList, GetTemplate().MaxSearchRadius, false);

            var height = GetTemplate().CylinderDatas.Height;
            var minZ = GetPositionZ() - height;
            var maxZ = GetPositionZ() + height;

            targetList.RemoveAll(unit => unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        private void HandleUnitEnterExit(List<Unit> newTargetList)
        {
            var exitUnits = _insideUnits;
            _insideUnits.Clear();

            var enteringUnits = new List<Unit>();

            foreach (var unit in newTargetList)
            {
                if (!exitUnits.Remove(unit.GetGUID())) // erase(key_type) returns number of elements erased
                    enteringUnits.Add(unit);

                _insideUnits.Add(unit.GetGUID());
            }

            // Handle after _insideUnits have been reinserted so we can use GetInsideUnits() in hooks
            foreach (var unit in enteringUnits)
            {
                var player = unit.ToPlayer();
                if (player)
                    if (player.IsDebugAreaTriggers)
                        player.SendSysMessage(CypherStrings.DebugAreatriggerEntered, GetTemplate().Id);

                DoActions(unit);

                _ai.OnUnitEnter(unit);
            }

            foreach (var exitUnitGuid in exitUnits)
            {
                var leavingUnit = Global.ObjAccessor.GetUnit(this, exitUnitGuid);
                if (leavingUnit)
                {
                    var player = leavingUnit.ToPlayer();
                    if (player)
                        if (player.IsDebugAreaTriggers)
                            player.SendSysMessage(CypherStrings.DebugAreatriggerLeft, GetTemplate().Id);

                    UndoActions(leavingUnit);

                    _ai.OnUnitExit(leavingUnit);
                }
            }
        }

        public AreaTriggerTemplate GetTemplate()
        {
            return _areaTriggerTemplate;
        }

        public uint GetScriptId()
        {
            return GetTemplate().ScriptId;
        }

        public Unit GetCaster()
        {
            return Global.ObjAccessor.GetUnit(this, GetCasterGuid());
        }

        private Unit GetTarget()
        {
            return Global.ObjAccessor.GetUnit(this, _targetGuid);
        }

        private void UpdatePolygonOrientation()
        {
            var newOrientation = GetOrientation();

            // No need to recalculate, orientation didn't change
            if (MathFunctions.fuzzyEq(_previousCheckOrientation, newOrientation))
                return;

            _polygonVertices = GetTemplate().PolygonVertices;

            var angleSin = (float)Math.Sin(newOrientation);
            var angleCos = (float)Math.Cos(newOrientation);

            // This is needed to rotate the vertices, following orientation
            for (var i = 0; i < _polygonVertices.Count; ++i)
            {
                var vertice = _polygonVertices[i];

                vertice.X = vertice.X * angleCos - vertice.Y * angleSin;
                vertice.Y = vertice.Y * angleCos + vertice.X * angleSin;
            }

            _previousCheckOrientation = newOrientation;
        }

        private bool CheckIsInPolygon2D(Position pos)
        {
            var testX = pos.GetPositionX();
            var testY = pos.GetPositionY();

            //this method uses the ray tracing algorithm to determine if the point is in the polygon
            var locatedInPolygon = false;

            for (var vertex = 0; vertex < _polygonVertices.Count; ++vertex)
            {
                int nextVertex;

                //repeat loop for all sets of points
                if (vertex == (_polygonVertices.Count - 1))
                {
                    //if i is the last vertex, let j be the first vertex
                    nextVertex = 0;
                }
                else
                {
                    //for all-else, let j=(i+1)th vertex
                    nextVertex = vertex + 1;
                }

                var vertX_i = GetPositionX() + _polygonVertices[vertex].X;
                var vertY_i = GetPositionY() + _polygonVertices[vertex].Y;
                var vertX_j = GetPositionX() + _polygonVertices[nextVertex].X;
                var vertY_j = GetPositionY() + _polygonVertices[nextVertex].Y;

                // following statement checks if testPoint.Y is below Y-coord of i-th vertex
                var belowLowY = vertY_i > testY;
                // following statement checks if testPoint.Y is below Y-coord of i+1-th vertex
                var belowHighY = vertY_j > testY;

                /* following statement is true if testPoint.Y satisfies either (only one is possible)
                -.(i).Y < testPoint.Y < (i+1).Y        OR
                -.(i).Y > testPoint.Y > (i+1).Y

                (Note)
                Both of the conditions indicate that a point is located within the edges of the Y-th coordinate
                of the (i)-th and the (i+1)- th vertices of the polygon. If neither of the above
                conditions is satisfied, then it is assured that a semi-infinite horizontal line draw
                to the right from the testpoint will NOT cross the line that connects vertices i and i+1
                of the polygon
                */
                var withinYsEdges = belowLowY != belowHighY;

                if (withinYsEdges)
                {
                    // this is the slope of the line that connects vertices i and i+1 of the polygon
                    var slopeOfLine = (vertX_j - vertX_i) / (vertY_j - vertY_i);

                    // this looks up the x-coord of a point lying on the above line, given its y-coord
                    var pointOnLine = (slopeOfLine * (testY - vertY_i)) + vertX_i;

                    //checks to see if x-coord of testPoint is smaller than the point on the line with the same y-coord
                    var isLeftToLine = testX < pointOnLine;

                    if (isLeftToLine)
                    {
                        //this statement changes true to false (and vice-versa)
                        locatedInPolygon = !locatedInPolygon;
                    }//end if (isLeftToLine)
                }//end if (withinYsEdges
            }

            return locatedInPolygon;
        }

        public void UpdateShape()
        {
            if (GetTemplate().IsPolygon())
                UpdatePolygonOrientation();
        }

        private bool UnitFitToActionRequirement(Unit unit, Unit caster, AreaTriggerAction action)
        {
            switch (action.TargetType)
            {
                case AreaTriggerActionUserTypes.Friend:
                        return caster._IsValidAssistTarget(unit, Global.SpellMgr.GetSpellInfo(action.Param, caster.GetMap().GetDifficultyID()));
                case AreaTriggerActionUserTypes.Enemy:
                        return caster._IsValidAttackTarget(unit, Global.SpellMgr.GetSpellInfo(action.Param, caster.GetMap().GetDifficultyID()));
                case AreaTriggerActionUserTypes.Raid:
                        return caster.IsInRaidWith(unit);
                case AreaTriggerActionUserTypes.Party:
                        return caster.IsInPartyWith(unit);
                case AreaTriggerActionUserTypes.Caster:
                        return unit.GetGUID() == caster.GetGUID();
                case AreaTriggerActionUserTypes.Any:
                default:
                    break;
            }

            return true;
        }

        private void DoActions(Unit unit)
        {
            var caster = IsServerSide() ? unit : GetCaster();
            if (caster)
            {
                foreach (var action in GetTemplate().Actions)
                {
                    if (IsServerSide() || UnitFitToActionRequirement(unit, caster, action))
                    {
                        switch (action.ActionType)
                        {
                            case AreaTriggerActionTypes.Cast:
                                caster.CastSpell(unit, action.Param, true);
                                break;
                            case AreaTriggerActionTypes.AddAura:
                                caster.AddAura(action.Param, unit);
                                break;
                            case AreaTriggerActionTypes.Teleport:
                                var safeLoc = Global.ObjectMgr.GetWorldSafeLoc(action.Param);
                                if (safeLoc != null)
                                {
                                    var player = caster.ToPlayer();
                                    if (player != null)
                                        player.TeleportTo(safeLoc.Loc);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void UndoActions(Unit unit)
        {
            foreach (var action in GetTemplate().Actions)
            {
                if (action.ActionType == AreaTriggerActionTypes.Cast || action.ActionType == AreaTriggerActionTypes.AddAura)
                    unit.RemoveAurasDueToSpell(action.Param, GetCasterGuid());
            }
        }

        private void InitSplineOffsets(List<Vector3> offsets, uint timeToTarget)
        {
            var angleSin = (float)Math.Sin(GetOrientation());
            var angleCos = (float)Math.Cos(GetOrientation());

            // This is needed to rotate the spline, following caster orientation
            var rotatedPoints = new List<Vector3>();
            for (var i = 0; i < offsets.Count; ++i)
            {
                var offset = offsets[i];
                var tempX = offset.X;
                var tempY = offset.Y;
                var tempZ = GetPositionZ();

                offset.X = (tempX * angleCos - tempY * angleSin) + GetPositionX();
                offset.Y = (tempX * angleSin + tempY * angleCos) + GetPositionY();
                UpdateAllowedPositionZ(offset.X, offset.Y, ref tempZ);
                offset.Z += tempZ;

                var x = GetPositionX() + (offset.X * angleCos - offset.Y * angleSin);
                var y = GetPositionY() + (offset.Y * angleCos + offset.X * angleSin);
                var z = GetPositionZ();

                UpdateAllowedPositionZ(x, y, ref z);
                z += offset.Z;

                rotatedPoints.Add(new Vector3(x, y, z));
            }

            InitSplines(rotatedPoints, timeToTarget);
        }

        private void InitSplines(List<Vector3> splinePoints, uint timeToTarget)
        {
            if (splinePoints.Count < 2)
                return;

            _movementTime = 0;

            _spline.InitSpline(splinePoints.ToArray(), splinePoints.Count, Spline.EvaluationMode.Linear);
            _spline.InitLengths();

            // should be sent in object create packets only
            DoWithSuppressingObjectUpdates(() =>
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTarget), timeToTarget);
                m_areaTriggerData.ClearChanged(m_areaTriggerData.TimeToTarget);
            });

            if (IsInWorld)
            {
                if (_reachedDestination)
                {
                    var reshapeDest = new AreaTriggerRePath();
                    reshapeDest.TriggerGUID = GetGUID();
                    SendMessageToSet(reshapeDest, true);
                }

                var reshape = new AreaTriggerRePath();
                reshape.TriggerGUID = GetGUID();
                reshape.AreaTriggerSpline.HasValue = true;
                reshape.AreaTriggerSpline.Value.ElapsedTimeForMovement = GetElapsedTimeForMovement();
                reshape.AreaTriggerSpline.Value.TimeToTarget = timeToTarget;
                reshape.AreaTriggerSpline.Value.Points = splinePoints;
                SendMessageToSet(reshape, true);
            }

            _reachedDestination = false;
        }

        private void InitOrbit(AreaTriggerOrbitInfo cmi, uint timeToTarget)
        {
            // Circular movement requires either a center position or an attached unit
            Cypher.Assert(cmi.Center.HasValue || cmi.PathTarget.HasValue);

            // should be sent in object create packets only
            DoWithSuppressingObjectUpdates(() =>
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTarget), timeToTarget);
                m_areaTriggerData.ClearChanged(m_areaTriggerData.TimeToTarget);
            });

            _orbitInfo.Set(cmi);

            _orbitInfo.Value.TimeToTarget = timeToTarget;
            _orbitInfo.Value.ElapsedTimeForMovement = 0;

            if (IsInWorld)
            {
                var reshape = new AreaTriggerRePath();
                reshape.TriggerGUID = GetGUID();
                reshape.AreaTriggerOrbit = _orbitInfo;

                SendMessageToSet(reshape, true);
            }
        }

        public bool HasOrbit()
        {
            return _orbitInfo.HasValue;
        }

        private Position GetOrbitCenterPosition()
        {
            if (!_orbitInfo.HasValue)
                return null;

            if (_orbitInfo.Value.PathTarget.HasValue)
            {
                var center = Global.ObjAccessor.GetWorldObject(this, _orbitInfo.Value.PathTarget.Value);
                if (center)
                    return center;
            }

            if (_orbitInfo.Value.Center.HasValue)
                return new Position(_orbitInfo.Value.Center.Value);

            return null;
        }

        private Position CalculateOrbitPosition()
        {
            var centerPos = GetOrbitCenterPosition();
            if (centerPos == null)
                return GetPosition();

            var cmi = _orbitInfo.Value;

            // AreaTrigger make exactly "Duration / TimeToTarget" loops during his life time
            var pathProgress = (float)cmi.ElapsedTimeForMovement / cmi.TimeToTarget;

            // We already made one circle and can't loop
            if (!cmi.CanLoop)
                pathProgress = Math.Min(1.0f, pathProgress);

            var radius = cmi.Radius;
            if (MathFunctions.fuzzyNe(cmi.BlendFromRadius, radius))
            {
                var blendCurve = (cmi.BlendFromRadius - radius) / radius;
                // 4.f Defines four quarters
                blendCurve = MathFunctions.RoundToInterval(ref blendCurve, 1.0f, 4.0f) / 4.0f;
                var blendProgress = Math.Min(1.0f, pathProgress / blendCurve);
                radius = MathFunctions.lerp(cmi.BlendFromRadius, cmi.Radius, blendProgress);
            }

            // Adapt Path progress depending of circle direction
            if (!cmi.CounterClockwise)
                pathProgress *= -1;

            var angle = cmi.InitialAngle + 2.0f * (float)Math.PI * pathProgress;
            var x = centerPos.GetPositionX() + (radius * (float)Math.Cos(angle));
            var y = centerPos.GetPositionY() + (radius * (float)Math.Sin(angle));
            var z = centerPos.GetPositionZ() + cmi.ZOffset;

            return new Position(x, y, z, angle);
        }

        private void UpdateOrbitPosition(uint diff)
        {
            if (_orbitInfo.Value.StartDelay > GetElapsedTimeForMovement())
                return;

            _orbitInfo.Value.ElapsedTimeForMovement = (int)(GetElapsedTimeForMovement() - _orbitInfo.Value.StartDelay);

            var pos = CalculateOrbitPosition();

            GetMap().AreaTriggerRelocation(this, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation());

            DebugVisualizePosition();
        }

        private void UpdateSplinePosition(uint diff)
        {
            if (_reachedDestination)
                return;

            if (!HasSplines())
                return;

            _movementTime += diff;

            if (_movementTime >= GetTimeToTarget())
            {
                _reachedDestination = true;
                _lastSplineIndex = _spline.Last();

                var lastSplinePosition = _spline.GetPoint(_lastSplineIndex);
                GetMap().AreaTriggerRelocation(this, lastSplinePosition.X, lastSplinePosition.Y, lastSplinePosition.Z, GetOrientation());

                DebugVisualizePosition();

                _ai.OnSplineIndexReached(_lastSplineIndex);
                _ai.OnDestinationReached();
                return;
            }

            var currentTimePercent = (float)_movementTime / GetTimeToTarget();

            if (currentTimePercent <= 0.0f)
                return;

            if (GetMiscTemplate().MoveCurveId != 0)
            {
                var progress = Global.DB2Mgr.GetCurveValueAt(GetMiscTemplate().MoveCurveId, currentTimePercent);
                if (progress < 0.0f || progress > 1.0f)
                {
                    Log.outError(LogFilter.AreaTrigger, "AreaTrigger (Id: {0}, SpellMiscId: {1}) has wrong progress ({2}) caused by curve calculation (MoveCurveId: {3})",
                        GetTemplate().Id, GetMiscTemplate().MiscId, progress, GetMiscTemplate().MorphCurveId);
                }
                else
                    currentTimePercent = progress;
            }

            var lastPositionIndex = 0;
            float percentFromLastPoint = 0;
            _spline.ComputeIndex(currentTimePercent, ref lastPositionIndex, ref percentFromLastPoint);

            Vector3 currentPosition;
            _spline.Evaluate_Percent(lastPositionIndex, percentFromLastPoint, out currentPosition);

            var orientation = GetOrientation();
            if (GetTemplate().HasFlag(AreaTriggerFlags.HasFaceMovementDir))
            {
                var nextPoint = _spline.GetPoint(lastPositionIndex + 1);
                orientation = GetAngle(nextPoint.X, nextPoint.Y);
            }

            GetMap().AreaTriggerRelocation(this, currentPosition.X, currentPosition.Y, currentPosition.Z, orientation);

            DebugVisualizePosition();

            if (_lastSplineIndex != lastPositionIndex)
            {
                _lastSplineIndex = lastPositionIndex;
                _ai.OnSplineIndexReached(_lastSplineIndex);
            }
        }

        private void AI_Initialize()
        {
            AI_Destroy();
            var ai = Global.ScriptMgr.GetAreaTriggerAI(this);
            if (ai == null)
                ai = new NullAreaTriggerAI(this);

            _ai = ai;
            _ai.OnInitialize();
        }

        private void AI_Destroy()
        {
            _ai = null;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            var flags = GetUpdateFieldFlagsFor(target);
            var buffer = new WorldPacket();

            buffer.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(buffer, flags, this, target);
            m_areaTriggerData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            var flags = GetUpdateFieldFlagsFor(target);
            var buffer = new WorldPacket();

            buffer.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.AreaTrigger))
                m_areaTriggerData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedAreaTriggerMask, Player target)
        {
            var valuesMask = new UpdateMask((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedAreaTriggerMask.IsAnySet())
                valuesMask.Set((int)TypeId.AreaTrigger);

            var buffer = new WorldPacket();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.AreaTrigger])
                m_areaTriggerData.WriteUpdate(buffer, requestedAreaTriggerMask, true, this, target);

            var buffer1 = new WorldPacket();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }
        
        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_areaTriggerData);
            base.ClearUpdateMask(remove);
        }

        public T GetAI<T>() where T : AreaTriggerAI { return (T)_ai; }

        public bool IsServerSide() { return _areaTriggerTemplate.Id.IsServerSide; }

        public override bool IsNeverVisibleFor(WorldObject seer)
        {
            if (IsServerSide())
                return true;

            return base.IsNeverVisibleFor(seer);
        }
        
        [System.Diagnostics.Conditional("DEBUG")]
        private void DebugVisualizePosition()
        {
            var caster = GetCaster();
            if (caster)
            {
                var player = caster.ToPlayer();
                if (player)
                    if (player.IsDebugAreaTriggers)
                        player.SummonCreature(1, this, TempSummonType.TimedDespawn, GetTimeToTarget());
            }
        }

        public bool IsRemoved() { return _isRemoved; }
        public uint GetSpellId() { return m_areaTriggerData.SpellID; }
        public AuraEffect GetAuraEffect() { return _aurEff; }
        public uint GetTimeSinceCreated() { return _timeSinceCreated; }
        public uint GetTimeToTarget() { return m_areaTriggerData.TimeToTarget; }
        public uint GetTimeToTargetScale() { return m_areaTriggerData.TimeToTargetScale; }
        public int GetDuration() { return _duration; }
        public int GetTotalDuration() { return _totalDuration; }

        public void Delay(int delaytime) { SetDuration(GetDuration() - delaytime); }

        public List<ObjectGuid> GetInsideUnits() { return _insideUnits; }

        public AreaTriggerMiscTemplate GetMiscTemplate() { return _areaTriggerMiscTemplate; }

        public ObjectGuid GetCasterGuid() { return m_areaTriggerData.Caster; }

        public Vector3 GetRollPitchYaw() { return _rollPitchYaw; }
        public Vector3 GetTargetRollPitchYaw() { return _targetRollPitchYaw; }

        public bool HasSplines() { return !_spline.Empty(); }
        public Spline GetSpline() { return _spline; }
        public uint GetElapsedTimeForMovement() { return GetTimeSinceCreated(); } // @todo: research the right value, in sniffs both timers are nearly identical

        public Optional<AreaTriggerOrbitInfo> GetCircularMovementInfo() { return _orbitInfo; }

        private AreaTriggerFieldData m_areaTriggerData;

        private ObjectGuid _targetGuid;

        private AuraEffect _aurEff;

        private int _duration;
        private int _totalDuration;
        private uint _timeSinceCreated;
        private float _previousCheckOrientation;
        private bool _isRemoved;

        private Vector3 _rollPitchYaw;
        private Vector3 _targetRollPitchYaw;
        private List<Vector2> _polygonVertices;
        private Spline _spline;

        private bool _reachedDestination;
        private int _lastSplineIndex;
        private uint _movementTime;

        private Optional<AreaTriggerOrbitInfo> _orbitInfo;

        private AreaTriggerMiscTemplate _areaTriggerMiscTemplate;
        private AreaTriggerTemplate _areaTriggerTemplate;
        private List<ObjectGuid> _insideUnits = new List<ObjectGuid>();

        private AreaTriggerAI _ai;
    }
}
