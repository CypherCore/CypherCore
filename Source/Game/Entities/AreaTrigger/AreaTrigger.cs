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
using Framework.GameMath;
using Game.AI;
using Game.Maps;
using Game.Movement;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using Game;
using Framework.Dynamic;

namespace Game.Entities
{
    public class AreaTrigger : WorldObject
    {
        public AreaTrigger() : base(false)
        {
            _previousCheckOrientation = float.PositiveInfinity;
            _reachedDestination = true;

            objectTypeMask |= TypeMask.AreaTrigger;
            objectTypeId = TypeId.AreaTrigger;

            m_updateFlag.Stationary = true;
            m_updateFlag.AreaTrigger = true;

            valuesCount = (int)AreaTriggerFields.End;

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

                Unit caster = GetCaster();
                if (caster)
                    caster._UnregisterAreaTrigger(this);

                // Handle removal of all units, calling OnUnitExit & deleting auras if needed
                HandleUnitEnterExit(new List<Unit>());

                _ai.OnRemove();

                base.RemoveFromWorld();
                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public static AreaTrigger CreateAreaTrigger(uint spellMiscId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, uint spellXSpellVisualId, ObjectGuid castId = default(ObjectGuid), AuraEffect aurEff = null)
        {
            AreaTrigger at = new AreaTrigger();
            if (!at.Create(spellMiscId, caster, target, spell, pos, duration, spellXSpellVisualId, castId, aurEff))
                return null;

            return at;
        }

        bool Create(uint spellMiscId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, uint spellXSpellVisualId, ObjectGuid castId, AuraEffect aurEff)
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

            _Create(ObjectGuid.Create(HighGuid.AreaTrigger, GetMapId(), GetTemplate().Id, caster.GetMap().GenerateLowGuid(HighGuid.AreaTrigger)));

            SetEntry(GetTemplate().Id);
            SetDuration(duration);

            SetObjectScale(1.0f);

            SetGuidValue(AreaTriggerFields.Caster, caster.GetGUID());
            SetGuidValue(AreaTriggerFields.CreatingEffectGuid, castId);

            SetUInt32Value(AreaTriggerFields.SpellId, spell.Id);
            SetUInt32Value(AreaTriggerFields.SpellForVisuals, spell.Id);
            SetUInt32Value(AreaTriggerFields.SpellXSpellVisualId, spellXSpellVisualId);
            SetUInt32Value(AreaTriggerFields.TimeToTargetScale, GetMiscTemplate().TimeToTargetScale != 0 ? GetMiscTemplate().TimeToTargetScale : GetUInt32Value(AreaTriggerFields.Duration));
            SetFloatValue(AreaTriggerFields.BoundsRadius2d, GetTemplate().MaxSearchRadius);
            SetUInt32Value(AreaTriggerFields.DecalPropertiesId, GetMiscTemplate().DecalPropertiesId);

            for (byte scaleCurveIndex = 0; scaleCurveIndex < SharedConst.MaxAreatriggerScale; ++scaleCurveIndex)
                if (GetMiscTemplate().ScaleInfo.ExtraScale[scaleCurveIndex].AsInt32 != 0)
                    SetUInt32Value(AreaTriggerFields.ExtraScaleCurve + scaleCurveIndex, (uint)GetMiscTemplate().ScaleInfo.ExtraScale[scaleCurveIndex].AsInt32);

            PhasingHandler.InheritPhaseShift(this, caster);

            if (target && GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
            {
                m_movementInfo.transport.guid = target.GetGUID();
            }

            UpdateShape();

            uint timeToTarget = GetMiscTemplate().TimeToTarget != 0 ? GetMiscTemplate().TimeToTarget : GetUInt32Value(AreaTriggerFields.Duration);

            if (GetTemplate().HasFlag(AreaTriggerFlags.HasCircularMovement))
            {
                AreaTriggerCircularMovementInfo cmi = GetMiscTemplate().CircularMovementInfo;
                if (target && GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
                    cmi.PathTarget.Set(target.GetGUID());
                else
                    cmi.Center.Set(new Vector3(pos.posX, pos.posY, pos.posZ));

                InitCircularMovement(cmi, timeToTarget);
            }
            else if (GetMiscTemplate().HasSplines())
            {
                InitSplineOffsets(GetMiscTemplate().SplinePoints, timeToTarget);
            }

            // movement on transport of areatriggers on unit is handled by themself
            Transport transport = m_movementInfo.transport.guid.IsEmpty() ? caster.GetTransport() : null;
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
            if (HasCircularMovement())
                Relocate(CalculateCircularMovementPosition());

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

        public override void Update(uint diff)
        {
            base.Update(diff);
            _timeSinceCreated += diff;

            // "If" order matter here, Circular Movement > Attached > Splines
            if (HasCircularMovement())
            {
                UpdateCircularMovementPosition(diff);
            }
            else if(GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
            {
                Unit target = GetTarget();
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
            SetUInt32Value(AreaTriggerFields.Duration, (uint)Math.Max(newDuration, 0));
        }

        void _UpdateDuration(int newDuration)
        {
            _duration = newDuration;

            // should be sent in object create packets only
            updateValues[(int)AreaTriggerFields.Duration].SignedValue = _duration;
        }

        float GetProgress()
        {
            return GetTimeSinceCreated() < GetTimeToTargetScale() ? (float)GetTimeSinceCreated() / GetTimeToTargetScale() : 1.0f;
        }

        void UpdateTargetList()
        {
            List<Unit> targetList = new List<Unit>();

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

        void SearchUnitInSphere(List<Unit> targetList)
        {
            float radius = GetTemplate().SphereDatas.Radius;
            if (GetTemplate().HasFlag(AreaTriggerFlags.HasDynamicShape))
            {
                if (GetMiscTemplate().MorphCurveId != 0)
                {
                    radius = MathFunctions.lerp(GetTemplate().SphereDatas.Radius, GetTemplate().SphereDatas.RadiusTarget, Global.DB2Mgr.GetCurveValueAt(GetMiscTemplate().MorphCurveId, GetProgress()));
                }
            }

            var check = new AnyUnitInObjectRangeCheck(this, radius);
            var searcher = new UnitListSearcher(this, targetList, check);
            Cell.VisitAllObjects(this, searcher, GetTemplate().MaxSearchRadius);
        }

        void SearchUnitInBox(List<Unit> targetList)
        {
            float extentsX, extentsY, extentsZ;

            unsafe
            {
                fixed (float* ptr = GetTemplate().BoxDatas.Extents)
                {
                    extentsX = ptr[0];
                    extentsY = ptr[1];
                    extentsZ = ptr[2];
                }
            }

            var check = new AnyUnitInObjectRangeCheck(this, GetTemplate().MaxSearchRadius, false);
            var searcher = new UnitListSearcher(this, targetList, check);
            Cell.VisitAllObjects(this, searcher, GetTemplate().MaxSearchRadius);

            float halfExtentsX = extentsX / 2.0f;
            float halfExtentsY = extentsY / 2.0f;
            float halfExtentsZ = extentsZ / 2.0f;

            float minX = GetPositionX() - halfExtentsX;
            float maxX = GetPositionX() + halfExtentsX;

            float minY = GetPositionY() - halfExtentsY;
            float maxY = GetPositionY() + halfExtentsY;

            float minZ = GetPositionZ() - halfExtentsZ;
            float maxZ = GetPositionZ() + halfExtentsZ;

            AxisAlignedBox box = new AxisAlignedBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));

            targetList.RemoveAll(unit => !box.contains(new Vector3(unit.GetPositionX(), unit.GetPositionY(), unit.GetPositionZ())));
        }

        void SearchUnitInPolygon(List<Unit> targetList)
        {
            var check = new AnyUnitInObjectRangeCheck(this, GetTemplate().MaxSearchRadius, false);
            var searcher = new UnitListSearcher(this, targetList, check);
            Cell.VisitAllObjects(this, searcher, GetTemplate().MaxSearchRadius);

            float height = GetTemplate().PolygonDatas.Height;
            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            targetList.RemoveAll(unit =>
                !CheckIsInPolygon2D(unit) || unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        void SearchUnitInCylinder(List<Unit> targetList)
        {
            var check = new AnyUnitInObjectRangeCheck(this, GetTemplate().MaxSearchRadius, false);
            var searcher = new UnitListSearcher(this, targetList, check);
            Cell.VisitAllObjects(this, searcher, GetTemplate().MaxSearchRadius);

            float height = GetTemplate().CylinderDatas.Height;
            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            targetList.RemoveAll(unit => unit.GetPositionZ() < minZ
                                         || unit.GetPositionZ() > maxZ);
        }

        void HandleUnitEnterExit(List<Unit> newTargetList)
        {
            List<ObjectGuid> exitUnits = _insideUnits;
            _insideUnits.Clear();

            List<Unit> enteringUnits = new List<Unit>();

            foreach (Unit unit in newTargetList)
            {
                if (!exitUnits.Remove(unit.GetGUID())) // erase(key_type) returns number of elements erased
                    enteringUnits.Add(unit);

                _insideUnits.Add(unit.GetGUID());
            }

            // Handle after _insideUnits have been reinserted so we can use GetInsideUnits() in hooks
            foreach (Unit unit in enteringUnits)
            {
                Player player = unit.ToPlayer();
                if (player)
                    if (player.isDebugAreaTriggers)
                        player.SendSysMessage(CypherStrings.DebugAreatriggerEntered, GetTemplate().Id);

                DoActions(unit);

                _ai.OnUnitEnter(unit);
            }

            foreach (ObjectGuid exitUnitGuid in exitUnits)
            {
                Unit leavingUnit = Global.ObjAccessor.GetUnit(this, exitUnitGuid);
                if (leavingUnit)
                {
                    Player player = leavingUnit.ToPlayer();
                    if (player)
                        if (player.isDebugAreaTriggers)
                            player.SendSysMessage(CypherStrings.DebugAreatriggerLeft, GetTemplate().Id);

                    UndoActions(leavingUnit);

                    _ai.OnUnitExit(leavingUnit);
                }
            }
        }

        public AreaTriggerTemplate GetTemplate()
        {
            return _areaTriggerMiscTemplate.Template;
        }

        public uint GetScriptId()
        {
            return GetTemplate().ScriptId;
        }

        public Unit GetCaster()
        {
            return Global.ObjAccessor.GetUnit(this, GetCasterGuid());
        }

        Unit GetTarget()
        {
            return Global.ObjAccessor.GetUnit(this, _targetGuid);
        }

        void UpdatePolygonOrientation()
        {
            float newOrientation = GetOrientation();

            // No need to recalculate, orientation didn't change
            if (MathFunctions.fuzzyEq(_previousCheckOrientation, newOrientation))
                return;

            _polygonVertices = GetTemplate().PolygonVertices;

            float angleSin = (float)Math.Sin(newOrientation);
            float angleCos = (float)Math.Cos(newOrientation);

            // This is needed to rotate the vertices, following orientation
            for (var i = 0; i < _polygonVertices.Count; ++i)
            {
                Vector2 vertice = _polygonVertices[i];

                vertice.X = vertice.X * angleCos - vertice.Y * angleSin;
                vertice.Y = vertice.Y * angleCos + vertice.X * angleSin;
            }

            _previousCheckOrientation = newOrientation;
        }

        bool CheckIsInPolygon2D(Position pos)
        {
            float testX = pos.GetPositionX();
            float testY = pos.GetPositionY();

            //this method uses the ray tracing algorithm to determine if the point is in the polygon
            bool locatedInPolygon = false;

            for (int vertex = 0; vertex < _polygonVertices.Count; ++vertex)
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

                float vertX_i = GetPositionX() + _polygonVertices[vertex].X;
                float vertY_i = GetPositionY() + _polygonVertices[vertex].Y;
                float vertX_j = GetPositionX() + _polygonVertices[nextVertex].X;
                float vertY_j = GetPositionY() + _polygonVertices[nextVertex].Y;

                // following statement checks if testPoint.Y is below Y-coord of i-th vertex
                bool belowLowY = vertY_i > testY;
                // following statement checks if testPoint.Y is below Y-coord of i+1-th vertex
                bool belowHighY = vertY_j > testY;

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
                bool withinYsEdges = belowLowY != belowHighY;

                if (withinYsEdges)
                {
                    // this is the slope of the line that connects vertices i and i+1 of the polygon
                    float slopeOfLine = (vertX_j - vertX_i) / (vertY_j - vertY_i);

                    // this looks up the x-coord of a point lying on the above line, given its y-coord
                    float pointOnLine = (slopeOfLine * (testY - vertY_i)) + vertX_i;

                    //checks to see if x-coord of testPoint is smaller than the point on the line with the same y-coord
                    bool isLeftToLine = testX < pointOnLine;

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

        bool UnitFitToActionRequirement(Unit unit, Unit caster, AreaTriggerAction action)
        {
            switch (action.TargetType)
            {
                case AreaTriggerActionUserTypes.Friend:
                        return caster._IsValidAssistTarget(unit, Global.SpellMgr.GetSpellInfo(action.Param));
                case AreaTriggerActionUserTypes.Enemy:
                        return caster._IsValidAttackTarget(unit, Global.SpellMgr.GetSpellInfo(action.Param));
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

        void DoActions(Unit unit)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                foreach (AreaTriggerAction action in GetTemplate().Actions)
        {
                    if (UnitFitToActionRequirement(unit, caster, action))
                    {
                        switch (action.ActionType)
                        {
                            case AreaTriggerActionTypes.Cast:
                                caster.CastSpell(unit, action.Param, true);
                                break;
                            case AreaTriggerActionTypes.AddAura:
                                caster.AddAura(action.Param, unit);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        void UndoActions(Unit unit)
        {
            foreach (AreaTriggerAction action in GetTemplate().Actions)
            {
                if (action.ActionType == AreaTriggerActionTypes.Cast || action.ActionType == AreaTriggerActionTypes.AddAura)
                    unit.RemoveAurasDueToSpell(action.Param, GetCasterGuid());
            }
        }

        void InitSplineOffsets(List<Vector3> offsets, uint timeToTarget)
        {
            float angleSin = (float)Math.Sin(GetOrientation());
            float angleCos = (float)Math.Cos(GetOrientation());

            // This is needed to rotate the spline, following caster orientation
            List<Vector3> rotatedPoints = new List<Vector3>();
            for (var i = 0; i < offsets.Count; ++i)
            {
                Vector3 offset = offsets[i];
                float tempX = offset.X;
                float tempY = offset.Y;
                float tempZ = GetPositionZ();

                offset.X = (tempX * angleCos - tempY * angleSin) + GetPositionX();
                offset.Y = (tempX * angleSin + tempY * angleCos) + GetPositionY();
                UpdateAllowedPositionZ(offset.X, offset.Y, ref tempZ);
                offset.Z += tempZ;

                float x = GetPositionX() + (offset.X * angleCos - offset.Y * angleSin);
                float y = GetPositionY() + (offset.Y * angleCos + offset.X * angleSin);
                float z = GetPositionZ();

                UpdateAllowedPositionZ(x, y, ref z);
                z += offset.Z;

                rotatedPoints.Add(new Vector3(x, y, z));
            }

            InitSplines(rotatedPoints, timeToTarget);
        }

        void InitSplines(List<Vector3> splinePoints, uint timeToTarget)
        {
            if (splinePoints.Count < 2)
                return;

            _movementTime = 0;

            _spline.Init_Spline(splinePoints.ToArray(), splinePoints.Count, Spline.EvaluationMode.Linear);
            _spline.initLengths();

            // should be sent in object create packets only
            updateValues[(int)AreaTriggerFields.TimeToTarget].UnsignedValue = timeToTarget;

            if (IsInWorld)
            {
                if (_reachedDestination)
                {
                    AreaTriggerRePath reshapeDest = new AreaTriggerRePath();
                    reshapeDest.TriggerGUID = GetGUID();
                    SendMessageToSet(reshapeDest, true);
                }

                AreaTriggerRePath reshape = new AreaTriggerRePath();
                reshape.TriggerGUID = GetGUID();
                reshape.AreaTriggerSpline.HasValue = true;
                reshape.AreaTriggerSpline.Value.ElapsedTimeForMovement = GetElapsedTimeForMovement();
                reshape.AreaTriggerSpline.Value.TimeToTarget = timeToTarget;
                reshape.AreaTriggerSpline.Value.Points = splinePoints;
                SendMessageToSet(reshape, true);
            }

            _reachedDestination = false;
        }

        void InitCircularMovement(AreaTriggerCircularMovementInfo cmi, uint timeToTarget)
        {
            // Circular movement requires either a center position or an attached unit
            Cypher.Assert(cmi.Center.HasValue || cmi.PathTarget.HasValue);

            // should be sent in object create packets only
            updateValues[(int)AreaTriggerFields.TimeToTarget].UnsignedValue = timeToTarget;

            _circularMovementInfo.Set(cmi);

            _circularMovementInfo.Value.TimeToTarget = timeToTarget;
            _circularMovementInfo.Value.ElapsedTimeForMovement = 0;

            if (IsInWorld)
            {
                AreaTriggerRePath reshape = new AreaTriggerRePath();
                reshape.TriggerGUID = GetGUID();
                reshape.AreaTriggerCircularMovement = _circularMovementInfo;

                SendMessageToSet(reshape, true);
            }
        }

        public bool HasCircularMovement()
        {
            return _circularMovementInfo.HasValue;
        }

        Position GetCircularMovementCenterPosition()
        {
            if (!_circularMovementInfo.HasValue)
                return null;

            if (_circularMovementInfo.Value.PathTarget.HasValue)
            {
                WorldObject center = Global.ObjAccessor.GetWorldObject(this, _circularMovementInfo.Value.PathTarget.Value);
                if (center)
                    return center;
            }

            if (_circularMovementInfo.Value.Center.HasValue)
                return new Position(_circularMovementInfo.Value.Center.Value);

            return null;
        }

        Position CalculateCircularMovementPosition()
        {
            Position centerPos = GetCircularMovementCenterPosition();
            if (centerPos == null)
                return GetPosition();

            AreaTriggerCircularMovementInfo cmi = _circularMovementInfo.Value;

            // AreaTrigger make exactly "Duration / TimeToTarget" loops during his life time
            float pathProgress = (float)cmi.ElapsedTimeForMovement / cmi.TimeToTarget;

            // We already made one circle and can't loop
            if (!cmi.CanLoop)
                pathProgress = Math.Min(1.0f, pathProgress);

            float radius = cmi.Radius;
            if (MathFunctions.fuzzyNe(cmi.BlendFromRadius, radius))
            {
                float blendCurve = (cmi.BlendFromRadius - radius) / radius;
                // 4.f Defines four quarters
                blendCurve = MathFunctions.RoundToInterval(ref blendCurve, 1.0f, 4.0f) / 4.0f;
                float blendProgress = Math.Min(1.0f, pathProgress / blendCurve);
                radius = MathFunctions.lerp(cmi.BlendFromRadius, cmi.Radius, blendProgress);
            }

            // Adapt Path progress depending of circle direction
            if (!cmi.CounterClockwise)
                pathProgress *= -1;

            float angle = cmi.InitialAngle + 2.0f * (float)Math.PI * pathProgress;
            float x = centerPos.GetPositionX() + (radius * (float)Math.Cos(angle));
            float y = centerPos.GetPositionY() + (radius * (float)Math.Sin(angle));
            float z = centerPos.GetPositionZ() + cmi.ZOffset;

            return new Position(x, y, z, angle);
        }

        void UpdateCircularMovementPosition(uint diff)
        {
            if (_circularMovementInfo.Value.StartDelay > GetElapsedTimeForMovement())
                return;

            _circularMovementInfo.Value.ElapsedTimeForMovement = (int)(GetElapsedTimeForMovement() - _circularMovementInfo.Value.StartDelay);

            Position pos = CalculateCircularMovementPosition();

            GetMap().AreaTriggerRelocation(this, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation());

            DebugVisualizePosition();
        }

        void UpdateSplinePosition(uint diff)
        {
            if (_reachedDestination)
                return;

            if (!HasSplines())
                return;

            _movementTime += diff;

            if (_movementTime >= GetTimeToTarget())
            {
                _reachedDestination = true;
                _lastSplineIndex = _spline.last();

                Vector3 lastSplinePosition = _spline.getPoint(_lastSplineIndex);
                GetMap().AreaTriggerRelocation(this, lastSplinePosition.X, lastSplinePosition.Y, lastSplinePosition.Z, GetOrientation());

                DebugVisualizePosition();

                _ai.OnSplineIndexReached(_lastSplineIndex);
                _ai.OnDestinationReached();
                return;
            }

            float currentTimePercent = (float)_movementTime / GetTimeToTarget();

            if (currentTimePercent <= 0.0f)
                return;

            if (GetMiscTemplate().MoveCurveId != 0)
            {
                float progress = Global.DB2Mgr.GetCurveValueAt(GetMiscTemplate().MoveCurveId, currentTimePercent);
                if (progress < 0.0f || progress > 1.0f)
                {
                    Log.outError(LogFilter.AreaTrigger, "AreaTrigger (Id: {0}, SpellMiscId: {1}) has wrong progress ({2}) caused by curve calculation (MoveCurveId: {3})",
                        GetTemplate().Id, GetMiscTemplate().MiscId, progress, GetMiscTemplate().MorphCurveId);
                }
                else
                    currentTimePercent = progress;
            }

            int lastPositionIndex = 0;
            float percentFromLastPoint = 0;
            _spline.computeIndex(currentTimePercent, ref lastPositionIndex, ref percentFromLastPoint);

            Vector3 currentPosition;
            _spline.Evaluate_Percent(lastPositionIndex, percentFromLastPoint, out currentPosition);

            float orientation = GetOrientation();
            if (GetTemplate().HasFlag(AreaTriggerFlags.HasFaceMovementDir))
            {
                Vector3 nextPoint = _spline.getPoint(lastPositionIndex + 1);
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

        void AI_Initialize()
        {
            AI_Destroy();
            AreaTriggerAI ai = Global.ScriptMgr.GetAreaTriggerAI(this);
            if (ai == null)
                ai = new NullAreaTriggerAI(this);

            _ai = ai;
            _ai.OnInitialize();
        }

        void AI_Destroy()
        {
            _ai = null;
        }

        AreaTriggerAI GetAI() { return _ai; }

        [System.Diagnostics.Conditional("DEBUG")]
        void DebugVisualizePosition()
        {
            Unit caster = GetCaster();
            if (caster)
            {
                Player player = caster.ToPlayer();
                if (player)
                    if (player.isDebugAreaTriggers)
                        player.SummonCreature(1, this, TempSummonType.TimedDespawn, GetTimeToTarget());
            }
        }

        public bool IsRemoved() { return _isRemoved; }
        public uint GetSpellId() { return GetUInt32Value(AreaTriggerFields.SpellId); }
        public AuraEffect GetAuraEffect() { return _aurEff; }
        public uint GetTimeSinceCreated() { return _timeSinceCreated; }
        public uint GetTimeToTarget() { return GetUInt32Value(AreaTriggerFields.TimeToTarget); }
        public uint GetTimeToTargetScale() { return GetUInt32Value(AreaTriggerFields.TimeToTargetScale); }
        public int GetDuration() { return _duration; }
        public int GetTotalDuration() { return _totalDuration; }

        public void Delay(int delaytime) { SetDuration(GetDuration() - delaytime); }

        public List<ObjectGuid> GetInsideUnits() { return _insideUnits; }

        public AreaTriggerMiscTemplate GetMiscTemplate() { return _areaTriggerMiscTemplate; }

        public ObjectGuid GetCasterGuid() { return GetGuidValue(AreaTriggerFields.Caster); }

        public Vector3 GetRollPitchYaw() { return _rollPitchYaw; }
        public Vector3 GetTargetRollPitchYaw() { return _targetRollPitchYaw; }

        public bool HasSplines() { return !_spline.empty(); }
        public Spline GetSpline() { return _spline; }
        public uint GetElapsedTimeForMovement() { return GetTimeSinceCreated(); } // @todo: research the right value, in sniffs both timers are nearly identical

        public Optional<AreaTriggerCircularMovementInfo> GetCircularMovementInfo() { return _circularMovementInfo; }

        ObjectGuid _targetGuid;

        AuraEffect _aurEff;

        int _duration;
        int _totalDuration;
        uint _timeSinceCreated;
        float _previousCheckOrientation;
        bool _isRemoved;

        Vector3 _rollPitchYaw;
        Vector3 _targetRollPitchYaw;
        List<Vector2> _polygonVertices;
        Spline _spline;

        bool _reachedDestination;
        int _lastSplineIndex;
        uint _movementTime;

        Optional<AreaTriggerCircularMovementInfo> _circularMovementInfo;

        AreaTriggerMiscTemplate _areaTriggerMiscTemplate;
        List<ObjectGuid> _insideUnits = new List<ObjectGuid>();

        AreaTriggerAI _ai;
    }
}
