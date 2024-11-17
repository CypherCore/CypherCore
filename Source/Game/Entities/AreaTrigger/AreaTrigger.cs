// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Maps;
using Game.Movement;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Entities
{
    public class AreaTrigger : WorldObject
    {
        public AreaTrigger() : base(false)
        {
            _verticesUpdatePreviousOrientation = float.PositiveInfinity;
            _reachedDestination = true;

            ObjectTypeMask |= TypeMask.AreaTrigger;
            ObjectTypeId = TypeId.AreaTrigger;

            m_updateFlag.Stationary = true;
            m_updateFlag.AreaTrigger = true;

            m_entityFragments.Add(EntityFragment.Tag_AreaTrigger, false);

            m_areaTriggerData = new AreaTriggerFieldData();

            _spline = new();
            _stationaryPosition = new();
        }

        public override void AddToWorld()
        {
            // Register the AreaTrigger for guid lookup and for caster
            if (!IsInWorld)
            {
                if (m_zoneScript != null)
                    m_zoneScript.OnAreaTriggerCreate(this);

                GetMap().GetObjectsStore().Add(GetGUID(), this);
                if (_spawnId != 0)
                    GetMap().GetAreaTriggerBySpawnIdStore().Add(_spawnId, this);

                base.AddToWorld();
            }
        }

        public override void RemoveFromWorld()
        {
            // Remove the AreaTrigger from the accessor and from all lists of objects in world
            if (IsInWorld)
            {
                if (m_zoneScript != null)
                    m_zoneScript.OnAreaTriggerRemove(this);

                _isRemoved = true;

                Unit caster = GetCaster();
                if (caster != null)
                    caster._UnregisterAreaTrigger(this);

                _ai.OnRemove();

                // Handle removal of all units, calling OnUnitExit & deleting auras if needed
                HandleUnitEnterExit(new List<Unit>());

                base.RemoveFromWorld();
                if (IsStaticSpawn())
                    GetMap().GetAreaTriggerBySpawnIdStore().Remove(_spawnId, this);

                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        void PlaySpellVisual(uint spellVisualId)
        {
            AreaTriggerPlaySpellVisual packet = new();
            packet.AreaTriggerGUID = GetGUID();
            packet.SpellVisualID = spellVisualId;
            SendMessageToSet(packet, false);
        }

        bool Create(AreaTriggerId areaTriggerCreatePropertiesId, Map map, Position pos, int duration, AreaTriggerSpawn spawnData = null, Unit caster = null, Unit target = null, SpellCastVisual spellVisual = default, SpellInfo spellInfo = null, Spell spell = null, AuraEffect aurEff = null)
        {
            _targetGuid = target != null ? target.GetGUID() : ObjectGuid.Empty;
            _aurEff = aurEff;

            SetMap(map);
            Relocate(pos);
            RelocateStationaryPosition(pos);
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (AreaTriggerCreatePropertiesId: (Id: {areaTriggerCreatePropertiesId.Id}, IsCustom: {areaTriggerCreatePropertiesId.IsCustom})) not created. Invalid coordinates (X: {GetPositionX()} Y: {GetPositionY()})");
                return false;
            }

            _areaTriggerCreateProperties = Global.AreaTriggerDataStorage.GetAreaTriggerCreateProperties(areaTriggerCreatePropertiesId);
            if (_areaTriggerCreateProperties == null)
            {
                Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (AreaTriggerCreatePropertiesId: (Id: {areaTriggerCreatePropertiesId.Id}, IsCustom: {areaTriggerCreatePropertiesId.IsCustom})) not created. Invalid areatrigger create properties id");
                return false;
            }

            SetZoneScript();

            _areaTriggerTemplate = _areaTriggerCreateProperties.Template;

            _Create(ObjectGuid.Create(HighGuid.AreaTrigger, GetMapId(), GetTemplate() != null ? GetTemplate().Id.Id : 0, GetMap().GenerateLowGuid(HighGuid.AreaTrigger)));

            if (GetTemplate() != null)
                SetEntry(GetTemplate().Id.Id);

            SetObjectScale(1.0f);
            SetDuration(duration);

            _shape = GetCreateProperties().Shape;

            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);
            if (caster != null)
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.Caster), caster.GetGUID());
            if (spell != null)
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.CreatingEffectGUID), spell.m_castId);

            if (spellInfo != null && !IsStaticSpawn())
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.SpellID), spellInfo.Id);

            SpellInfo spellForVisuals = spellInfo;
            if (GetCreateProperties().SpellForVisuals.HasValue)
            {
                spellForVisuals = Global.SpellMgr.GetSpellInfo(GetCreateProperties().SpellForVisuals.Value, Difficulty.None);

                if (spellForVisuals != null)
                    spellVisual.SpellXSpellVisualID = spellForVisuals.GetSpellXSpellVisualId();
            }
            if (spellForVisuals != null)
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.SpellForVisuals), spellForVisuals.Id);

            SpellCastVisualField spellCastVisual = areaTriggerData.ModifyValue(m_areaTriggerData.SpellVisual);
            SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, spellVisual.SpellXSpellVisualID);
            SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, spellVisual.ScriptVisualID);

            if (!IsStaticSpawn())
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTargetScale), GetCreateProperties().TimeToTargetScale != 0 ? GetCreateProperties().TimeToTargetScale : m_areaTriggerData.Duration);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.BoundsRadius2D), GetCreateProperties().Shape.GetMaxSearchRadius());
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.DecalPropertiesID), GetCreateProperties().DecalPropertiesId);
            if (IsServerSide())
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.DecalPropertiesID), 24u); // Blue decal, for .debug areatrigger visibility

            AreaTriggerScaleCurveTemplate extraScaleCurve = IsStaticSpawn() ? new AreaTriggerScaleCurveTemplate() : GetCreateProperties().ExtraScale;
            SetScaleCurve(areaTriggerData.ModifyValue(m_areaTriggerData.ExtraScaleCurve), extraScaleCurve);

            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                {
                    float multiplier = 1.0f;
                    int flat = 0;
                    modOwner.GetSpellModValues(spellInfo, SpellModOp.Radius, spell, (float)m_areaTriggerData.BoundsRadius2D, ref flat, ref multiplier);
                    if (multiplier != 1.0f)
                    {
                        AreaTriggerScaleCurveTemplate overrideScale = new();
                        overrideScale.Curve = multiplier;
                        SetScaleCurve(areaTriggerData.ModifyValue(m_areaTriggerData.OverrideScaleCurve), overrideScale);
                    }
                }
            }

            VisualAnim visualAnim = areaTriggerData.ModifyValue(m_areaTriggerData.VisualAnim);
            SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.AnimationDataID), GetCreateProperties().AnimId);
            SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.AnimKitID), GetCreateProperties().AnimKitId);
            if (GetCreateProperties() != null && GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.VisualAnimIsDecay))
                SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.IsDecay), true);

            if (caster != null)
                PhasingHandler.InheritPhaseShift(this, caster);
            else if (IsStaticSpawn() && spawnData != null)
            {
                if (spawnData.PhaseUseFlags != 0 || spawnData.PhaseId != 0 || spawnData.PhaseGroup != 0)
                    PhasingHandler.InitDbPhaseShift(GetPhaseShift(), spawnData.PhaseUseFlags, spawnData.PhaseId, spawnData.PhaseGroup);
            }

            if (target != null && GetCreateProperties() != null && GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAttached))
                m_movementInfo.transport.guid = target.GetGUID();

            if (!IsStaticSpawn())
                UpdatePositionData();

            UpdateShape();

            uint timeToTarget = GetCreateProperties().TimeToTarget != 0 ? GetCreateProperties().TimeToTarget : m_areaTriggerData.Duration;

            if (GetCreateProperties().OrbitInfo != null)
            {
                AreaTriggerOrbitInfo orbit = GetCreateProperties().OrbitInfo;
                if (target != null && GetCreateProperties() != null && GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAttached))
                    orbit.PathTarget = target.GetGUID();
                else
                    orbit.Center = new(pos.posX, pos.posY, pos.posZ);

                InitOrbit(orbit, timeToTarget);
            }
            else if (GetCreateProperties().HasSplines())
            {
                InitSplineOffsets(GetCreateProperties().SplinePoints, timeToTarget);
            }

            // movement on transport of areatriggers on unit is handled by themself
            ITransport transport = null;
            if (caster != null)
            {
                transport = m_movementInfo.transport.guid.IsEmpty() ? caster.GetTransport() : null;

                if (transport != null)
                {
                    float x, y, z, o;
                    pos.GetPosition(out x, out y, out z, out o);
                    transport.CalculatePassengerOffset(ref x, ref y, ref z, ref o);
                    m_movementInfo.transport.pos.Relocate(x, y, z, o);

                    // This object must be added to transport before adding to map for the client to properly display it
                    transport.AddPassenger(this);
                }
            }

            AI_Initialize();

            // Relocate areatriggers with circular movement again
            if (HasOrbit())
                Relocate(CalculateOrbitPosition());

            if (!IsStaticSpawn())
            {
                if (!GetMap().AddToMap(this))
                {         // Returning false will cause the object to be deleted - remove from transport
                    if (transport != null)
                        transport.RemovePassenger(this);
                    return false;
                }
            }

            caster?._RegisterAreaTrigger(this);

            _ai.OnCreate(spell);

            return true;
        }

        public static AreaTrigger CreateAreaTrigger(AreaTriggerId areaTriggerCreatePropertiesId, Position pos, int duration, Unit caster, Unit target, SpellCastVisual spellVisual = default, SpellInfo spellInfo = null, Spell spell = null, AuraEffect aurEff = null)
        {
            AreaTrigger at = new();
            if (!at.Create(areaTriggerCreatePropertiesId, caster.GetMap(), pos, duration, null, caster, target, spellVisual, spellInfo, spell, aurEff))
                return null;

            return at;
        }

        public static ObjectGuid CreateNewMovementForceId(Map map, uint areaTriggerId)
        {
            return ObjectGuid.Create(HighGuid.AreaTrigger, map.GetId(), areaTriggerId, map.GenerateLowGuid(HighGuid.AreaTrigger));
        }

        public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
        {
            _spawnId = spawnId;

            AreaTriggerSpawn spawnData = Global.AreaTriggerDataStorage.GetAreaTriggerSpawn(spawnId);
            if (spawnData == null)
                return false;

            AreaTriggerCreateProperties createProperties = Global.AreaTriggerDataStorage.GetAreaTriggerCreateProperties(spawnData.Id);
            if (createProperties == null)
                return false;

            return Create(spawnData.Id, map, spawnData.SpawnPoint, -1, spawnData);
        }

        public override void Update(uint diff)
        {
            base.Update(diff);
            _timeSinceCreated += diff;

            if (!IsStaticSpawn())
            {
                // "If" order matter here, Orbit > Attached > Splines
                if (HasOverridePosition())
                {
                    UpdateOverridePosition();
                }
                else if (HasOrbit())
                {
                    UpdateOrbitPosition(diff);
                }
                else if (GetCreateProperties() != null && GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAttached))
                {
                    Unit target = GetTarget();
                    if (target != null)
                    {
                        float orientation = 0.0f;
                        AreaTriggerCreateProperties createProperties = GetCreateProperties();
                        if (createProperties != null && createProperties.FacingCurveId != 0)
                            orientation = Global.DB2Mgr.GetCurveValueAt(createProperties.FacingCurveId, GetProgress());

                        if (GetCreateProperties() == null || !GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAbsoluteOrientation))
                            orientation += target.GetOrientation();

                        GetMap().AreaTriggerRelocation(this, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), orientation);
                    }
                }
                else if (HasSplines())
                {
                    UpdateSplinePosition(diff);
                }
                else
                {
                    AreaTriggerCreateProperties createProperties = GetCreateProperties();
                    if (createProperties != null && createProperties.FacingCurveId != 0)
                    {
                        float orientation = Global.DB2Mgr.GetCurveValueAt(createProperties.FacingCurveId, GetProgress());
                        if (GetCreateProperties() == null || !GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAbsoluteOrientation))
                            orientation += GetStationaryO();

                        SetOrientation(orientation);
                    }

                    UpdateShape();
                }
            }

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

        void SetOverrideScaleCurve(float overrideScale)
        {
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideScaleCurve), overrideScale);
        }

        void SetOverrideScaleCurve(Vector2[] points, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideScaleCurve), points, startTimeOffset, interpolation);
        }

        void ClearOverrideScaleCurve()
        {
            ClearScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideScaleCurve));
        }

        void SetExtraScaleCurve(float extraScale)
        {
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.ExtraScaleCurve), extraScale);
        }

        void SetExtraScaleCurve(Vector2[] points, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.ExtraScaleCurve), points, startTimeOffset, interpolation);
        }

        void ClearExtraScaleCurve()
        {
            ClearScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.ExtraScaleCurve));
        }

        void SetOverrideMoveCurve(float x, float y, float z)
        {
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveX), x);
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveY), y);
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveZ), z);
        }

        void SetOverrideMoveCurve(Vector2[] xCurvePoints, Vector2[] yCurvePoints, Vector2[] zCurvePoints, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveX), xCurvePoints, startTimeOffset, interpolation);
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveY), yCurvePoints, startTimeOffset, interpolation);
            SetScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveZ), zCurvePoints, startTimeOffset, interpolation);
        }

        void ClearOverrideMoveCurve()
        {
            ClearScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveX));
            ClearScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveY));
            ClearScaleCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideMoveCurveZ));
        }

        public void SetDuration(int newDuration)
        {
            _duration = newDuration;
            _totalDuration = newDuration;

            // negative duration (permanent areatrigger) sent as 0
            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.Duration), (uint)Math.Max(newDuration, 0));
        }

        void _UpdateDuration(int newDuration)
        {
            _duration = newDuration;

            // should be sent in object create packets only
            DoWithSuppressingObjectUpdates(() =>
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.Duration), (uint)_duration);
                m_areaTriggerData.ClearChanged(m_areaTriggerData.Duration);
            });
        }

        float CalcCurrentScale()
        {
            float scale = 1.0f;
            if (m_areaTriggerData.OverrideScaleCurve.GetValue().OverrideActive)
                scale *= Math.Max(GetScaleCurveValue(m_areaTriggerData.OverrideScaleCurve, m_areaTriggerData.TimeToTargetScale), 0.000001f);
            else
            {
                AreaTriggerCreateProperties createProperties = GetCreateProperties();
                if (createProperties != null && createProperties.ScaleCurveId != 0)
                    scale *= Math.Max(Global.DB2Mgr.GetCurveValueAt(createProperties.ScaleCurveId, GetScaleCurveProgress(m_areaTriggerData.OverrideScaleCurve, m_areaTriggerData.TimeToTargetScale)), 0.000001f);
            }

            scale *= Math.Max(GetScaleCurveValue(m_areaTriggerData.ExtraScaleCurve, m_areaTriggerData.TimeToTargetExtraScale), 0.000001f);

            return scale;
        }

        float GetProgress()
        {
            if (_totalDuration <= 0)
                return 1.0f;

            return Math.Clamp((float)GetTimeSinceCreated() / (float)GetTotalDuration(), 0.0f, 1.0f);
        }

        float GetScaleCurveProgress(ScaleCurve scaleCurve, uint timeTo)
        {
            if (timeTo == 0)
                return 0.0f;

            return Math.Clamp((float)(GetTimeSinceCreated() - scaleCurve.StartTimeOffset) / (float)timeTo, 0.0f, 1.0f);
        }

        float GetScaleCurveValueAtProgress(ScaleCurve scaleCurve, float x)
        {
            Cypher.Assert(scaleCurve.OverrideActive, "ScaleCurve must be active to evaluate it");

            // unpack ParameterCurve
            if ((scaleCurve.ParameterCurve & 1u) != 0)
                return BitConverter.UInt32BitsToSingle((uint)(scaleCurve.ParameterCurve & ~1u));

            Vector2[] points = new Vector2[2];
            for (var i = 0; i < scaleCurve.Points.GetSize(); ++i)
                points[i] = new(scaleCurve.Points[i].X, scaleCurve.Points[i].Y);

            CurveInterpolationMode mode = (CurveInterpolationMode)(scaleCurve.ParameterCurve >> 1 & 0x7);
            int pointCount = (int)(scaleCurve.ParameterCurve >> 24 & 0xFF);

            return Global.DB2Mgr.GetCurveValueAt(mode, points.AsSpan(0, pointCount).ToArray(), x);
        }

        float GetScaleCurveValue(ScaleCurve scaleCurve, uint timeTo)
        {
            return GetScaleCurveValueAtProgress(scaleCurve, GetScaleCurveProgress(scaleCurve, timeTo));
        }

        void SetScaleCurve(ScaleCurve scaleCurve, float constantValue)
        {
            AreaTriggerScaleCurveTemplate curveTemplate = new();
            curveTemplate.Curve = constantValue;
            SetScaleCurve(scaleCurve, curveTemplate);
        }

        void SetScaleCurve(ScaleCurve scaleCurve, Vector2[] points, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            AreaTriggerScaleCurvePointsTemplate curve = new();
            curve.Mode = interpolation;
            curve.Points = points;

            AreaTriggerScaleCurveTemplate curveTemplate = new();
            curveTemplate.StartTimeOffset = startTimeOffset.GetValueOrDefault(GetTimeSinceCreated());
            curveTemplate.CurveTemplate = curve;

            SetScaleCurve(scaleCurve, curveTemplate);
        }

        void ClearScaleCurve(ScaleCurve scaleCurve)
        {
            SetScaleCurve(scaleCurve, null);
        }

        void SetScaleCurve(ScaleCurve scaleCurve, AreaTriggerScaleCurveTemplate curve)
        {
            if (curve == null)
            {
                SetUpdateFieldValue(scaleCurve.ModifyValue(scaleCurve.OverrideActive), false);
                return;
            }

            SetUpdateFieldValue(scaleCurve.ModifyValue(scaleCurve.OverrideActive), true);
            SetUpdateFieldValue(scaleCurve.ModifyValue(scaleCurve.StartTimeOffset), curve.StartTimeOffset);

            Position point = new Position();
            // ParameterCurve packing information
            // (not_using_points & 1) | ((interpolation_mode & 0x7) << 1) | ((first_point_offset & 0xFFFFF) << 4) | ((point_count & 0xFF) << 24)
            //   if not_using_points is set then the entire field is simply read as a float (ignoring that lowest bit)
            float simpleFloat = curve.Curve;
            if (simpleFloat != 0)
            {
                uint packedCurve = BitConverter.SingleToUInt32Bits(simpleFloat);
                packedCurve |= 1;

                SetUpdateFieldValue(scaleCurve.ModifyValue(scaleCurve.ParameterCurve), packedCurve);

                // clear points
                for (var i = 0; i < scaleCurve.Points.GetSize(); ++i)
                    SetUpdateFieldValue(ref scaleCurve.ModifyValue(scaleCurve.Points, i), point);
            }
            else
            {
                var curvePoints = curve.CurveTemplate;
                if (curvePoints != null)
                {
                    CurveInterpolationMode mode = curvePoints.Mode;
                    if (curvePoints.Points[1].X < curvePoints.Points[0].X)
                        mode = CurveInterpolationMode.Constant;

                    switch (mode)
                    {
                        case CurveInterpolationMode.CatmullRom:
                            // catmullrom requires at least 4 points, impossible here
                            mode = CurveInterpolationMode.Cosine;
                            break;
                        case CurveInterpolationMode.Bezier3:
                        case CurveInterpolationMode.Bezier4:
                        case CurveInterpolationMode.Bezier:
                            // bezier requires more than 2 points, impossible here
                            mode = CurveInterpolationMode.Linear;
                            break;
                        default:
                            break;
                    }

                    uint pointCount = 2;
                    if (mode == CurveInterpolationMode.Constant)
                        pointCount = 1;

                    uint packedCurve = ((uint)mode << 1) | (pointCount << 24);
                    SetUpdateFieldValue(scaleCurve.ModifyValue(scaleCurve.ParameterCurve), packedCurve);

                    for (var i = 0; i < curvePoints.Points.Length; ++i)
                    {
                        point.Relocate(curvePoints.Points[i].X, curvePoints.Points[i].Y);
                        SetUpdateFieldValue(ref scaleCurve.ModifyValue(scaleCurve.Points, i), point);
                    }
                }
            }
        }

        void UpdateTargetList()
        {
            List<Unit> targetList = new();

            switch (_shape.TriggerType)
            {
                case AreaTriggerShapeType.Sphere:
                    SearchUnitInSphere(targetList);
                    break;
                case AreaTriggerShapeType.Box:
                    SearchUnitInBox(targetList);
                    break;
                case AreaTriggerShapeType.Polygon:
                    SearchUnitInPolygon(targetList);
                    break;
                case AreaTriggerShapeType.Cylinder:
                    SearchUnitInCylinder(targetList);
                    break;
                case AreaTriggerShapeType.Disk:
                    SearchUnitInDisk(targetList);
                    break;
                case AreaTriggerShapeType.BoundedPlane:
                    SearchUnitInBoundedPlane(targetList);
                    break;
                default:
                    break;
            }

            if (GetTemplate() != null)
            {
                var conditions = Global.ConditionMgr.GetConditionsForAreaTrigger(GetTemplate().Id.Id, GetTemplate().Id.IsCustom);
                targetList.RemoveAll(target =>
                {
                    if (GetCasterGuid() == target.GetGUID())
                    {
                        if (HasActionSetFlag(AreaTriggerActionSetFlag.NotTriggeredbyCaster))
                            return true;
                    }
                    else
                    {
                        if (HasActionSetFlag(AreaTriggerActionSetFlag.OnlyTriggeredByCaster))
                            return true;

                        if (HasActionSetFlag(AreaTriggerActionSetFlag.CreatorsPartyOnly))
                        {
                            Unit caster = GetCaster();
                            if (caster == null)
                                return true;

                            if (!caster.IsInRaidWith(target))
                                return true;
                        }
                    }

                    Player player = target.ToPlayer();
                    if (player != null)
                    {
                        switch (player.GetDeathState())
                        {
                            case DeathState.Dead:
                                if (!HasActionSetFlag(AreaTriggerActionSetFlag.AllowWhileGhost))
                                    return true;
                                break;
                            case DeathState.Corpse:
                                if (!HasActionSetFlag(AreaTriggerActionSetFlag.AllowWhileDead))
                                    return true;
                                break;
                            default:
                                break;
                        }
                    }

                    if (!HasActionSetFlag(AreaTriggerActionSetFlag.CanAffectUninteractible) && target.IsUninteractible())
                        return true;

                    if (conditions != null)
                        return !Global.ConditionMgr.IsObjectMeetToConditions(target, conditions);

                    return false;
                });
            }

            HandleUnitEnterExit(targetList);
        }

        void SearchUnits(List<Unit> targetList, float radius, bool check3D)
        {
            var check = new AnyUnitInObjectRangeCheck(this, radius, check3D, false);
            if (IsStaticSpawn())
            {
                List<Player> temp = new List<Player>();
                var searcher = new PlayerListSearcher(this, temp, check);
                Cell.VisitWorldObjects(this, searcher, GetMaxSearchRadius());
                targetList.AddRange(temp);
            }
            else
            {
                var searcher = new UnitListSearcher(this, targetList, check);
                Cell.VisitAllObjects(this, searcher, GetMaxSearchRadius());
            }
        }

        void SearchUnitInSphere(List<Unit> targetList)
        {
            float progress = GetProgress();
            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            if (createProperties != null && createProperties.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(createProperties.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float radius = MathFunctions.Lerp(_shape.SphereDatas.Radius, _shape.SphereDatas.RadiusTarget, progress) * scale;

            SearchUnits(targetList, radius, true);
        }

        void SearchUnitInBox(List<Unit> targetList)
        {
            float progress = GetProgress();
            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            if (createProperties != null && createProperties.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(createProperties.MorphCurveId, progress);

            unsafe
            {
                float scale = CalcCurrentScale();
                float extentsX = MathFunctions.Lerp(_shape.BoxDatas.Extents[0], _shape.BoxDatas.ExtentsTarget[0], progress) * scale;
                float extentsY = MathFunctions.Lerp(_shape.BoxDatas.Extents[1], _shape.BoxDatas.ExtentsTarget[1], progress) * scale;
                float extentsZ = MathFunctions.Lerp(_shape.BoxDatas.Extents[2], _shape.BoxDatas.ExtentsTarget[2], progress) * scale;
                float radius = MathF.Sqrt(extentsX * extentsX + extentsY * extentsY);

                SearchUnits(targetList, radius, false);

                Position boxCenter = GetPosition();
                targetList.RemoveAll(unit => !unit.IsWithinBox(boxCenter, extentsX, extentsY, extentsZ / 2));
            }
        }

        void SearchUnitInPolygon(List<Unit> targetList)
        {
            float progress = GetProgress();
            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            if (createProperties != null && createProperties.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(createProperties.MorphCurveId, progress);

            float height = MathFunctions.Lerp(_shape.PolygonDatas.Height, _shape.PolygonDatas.HeightTarget, progress);
            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            SearchUnits(targetList, GetMaxSearchRadius(), false);

            targetList.RemoveAll(unit => unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ || !unit.IsInPolygon2D(this, _polygonVertices));
        }

        void SearchUnitInCylinder(List<Unit> targetList)
        {
            float progress = GetProgress();
            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            if (createProperties != null && createProperties.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(createProperties.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float radius = MathFunctions.Lerp(_shape.CylinderDatas.Radius, _shape.CylinderDatas.RadiusTarget, progress) * scale;
            float height = MathFunctions.Lerp(_shape.CylinderDatas.Height, _shape.CylinderDatas.HeightTarget, progress);
            if (!m_areaTriggerData.HeightIgnoresScale)
                height *= scale;

            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            SearchUnits(targetList, radius, false);

            targetList.RemoveAll(unit => unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        void SearchUnitInDisk(List<Unit> targetList)
        {
            float progress = GetProgress();
            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            if (createProperties != null && createProperties.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(createProperties.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float innerRadius = MathFunctions.Lerp(_shape.DiskDatas.InnerRadius, _shape.DiskDatas.InnerRadiusTarget, progress) * scale;
            float outerRadius = MathFunctions.Lerp(_shape.DiskDatas.OuterRadius, _shape.DiskDatas.OuterRadiusTarget, progress) * scale;
            float height = MathFunctions.Lerp(_shape.DiskDatas.Height, _shape.DiskDatas.HeightTarget, progress);
            if (!m_areaTriggerData.HeightIgnoresScale)
                height *= scale;

            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            SearchUnits(targetList, outerRadius, false);

            targetList.RemoveAll(unit => unit.IsInDist2d(this, innerRadius) || unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        void SearchUnitInBoundedPlane(List<Unit> targetList)
        {
            float progress = GetProgress();
            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            if (createProperties != null && createProperties.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(createProperties.MorphCurveId, progress);

            unsafe
            {
                float scale = CalcCurrentScale();
                float extentsX = MathFunctions.Lerp(_shape.BoundedPlaneDatas.Extents[0], _shape.BoundedPlaneDatas.ExtentsTarget[0], progress) * scale;
                float extentsY = MathFunctions.Lerp(_shape.BoundedPlaneDatas.Extents[1], _shape.BoundedPlaneDatas.ExtentsTarget[1], progress) * scale;
                float radius = MathF.Sqrt(extentsX * extentsX + extentsY * extentsY);

                SearchUnits(targetList, radius, false);

                Position boxCenter = GetPosition();
                targetList.RemoveAll(unit => !unit.IsWithinBox(boxCenter, extentsX, extentsY, MapConst.MapSize));
            }
        }

        void HandleUnitEnterExit(List<Unit> newTargetList)
        {
            List<ObjectGuid> exitUnits = _insideUnits;
            _insideUnits.Clear();

            List<Unit> enteringUnits = new();

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
                if (player != null)
                {
                    if (player.IsDebugAreaTriggers)
                        player.SendSysMessage(CypherStrings.DebugAreatriggerEntityEntered, GetEntry(), IsCustom(), IsStaticSpawn(), _spawnId);

                    player.UpdateQuestObjectiveProgress(QuestObjectiveType.AreaTriggerEnter, (int)GetEntry(), 1);

                    if (GetTemplate().ActionSetId != 0)
                        player.UpdateCriteria(CriteriaType.EnterAreaTriggerWithActionSet, GetTemplate().ActionSetId);
                }

                DoActions(unit);

                _ai.OnUnitEnter(unit);
            }

            foreach (ObjectGuid exitUnitGuid in exitUnits)
            {
                Unit leavingUnit = Global.ObjAccessor.GetUnit(this, exitUnitGuid);
                if (leavingUnit != null)
                {
                    Player player = leavingUnit.ToPlayer();
                    if (player != null)
                    {
                        if (player.IsDebugAreaTriggers)
                            player.SendSysMessage(CypherStrings.DebugAreatriggerEntityLeft, GetEntry(), IsCustom(), IsStaticSpawn(), _spawnId);

                        player.UpdateQuestObjectiveProgress(QuestObjectiveType.AreaTriggerExit, (int)GetEntry(), 1);

                        if (GetTemplate().ActionSetId != 0)
                            player.UpdateCriteria(CriteriaType.LeaveAreaTriggerWithActionSet, GetTemplate().ActionSetId);
                    }

                    UndoActions(leavingUnit);

                    _ai.OnUnitExit(leavingUnit);
                }
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.NumUnitsInside), _insideUnits.Count);
            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.NumPlayersInside), _insideUnits.Count(guid => guid.IsPlayer()));
        }

        public AreaTriggerTemplate GetTemplate()
        {
            return _areaTriggerTemplate;
        }

        public uint GetScriptId()
        {
            if (_spawnId != 0)
            {
                AreaTriggerSpawn spawn = Global.AreaTriggerDataStorage.GetAreaTriggerSpawn(_spawnId);
                if (spawn != null && spawn.ScriptId != 0)
                    return spawn.ScriptId;

            }

            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            if (createProperties != null)
                return createProperties.ScriptId;

            return 0;
        }

        public Unit GetCaster()
        {
            return Global.ObjAccessor.GetUnit(this, GetCasterGuid());
        }

        Unit GetTarget()
        {
            return Global.ObjAccessor.GetUnit(this, _targetGuid);
        }

        public override uint GetFaction()
        {
            Unit caster = GetCaster();
            if (caster != null)
                return caster.GetFaction();

            return 0;
        }

        float GetMaxSearchRadius()
        {
            return m_areaTriggerData.BoundsRadius2D * CalcCurrentScale();
        }

        void UpdatePolygonVertices()
        {
            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            AreaTriggerShapeInfo shape = GetShape();
            float newOrientation = GetOrientation();

            // No need to recalculate, orientation didn't change
            if (MathFunctions.fuzzyEq(_verticesUpdatePreviousOrientation, newOrientation) && shape.PolygonVerticesTarget.Empty())
                return;

            _polygonVertices.AddRange(shape.PolygonVertices.Select(p => new Position(p.X, p.Y)));

            if (!shape.PolygonVerticesTarget.Empty())
            {
                float progress = GetProgress();
                if (createProperties.MorphCurveId != 0)
                    progress = Global.DB2Mgr.GetCurveValueAt(createProperties.MorphCurveId, progress);

                for (var i = 0; i < _polygonVertices.Count; ++i)
                {
                    Vector2 vertex = _polygonVertices[i];
                    Vector2 vertexTarget = shape.PolygonVerticesTarget[i];

                    vertex.X = MathFunctions.Lerp(vertex.X, vertexTarget.X, progress);
                    vertex.Y = MathFunctions.Lerp(vertex.Y, vertexTarget.Y, progress);
                }
            }

            float angleSin = (float)Math.Sin(newOrientation);
            float angleCos = (float)Math.Cos(newOrientation);

            // This is needed to rotate the vertices, following orientation
            for (var i = 0; i < _polygonVertices.Count; ++i)
            {
                Vector2 vertice = _polygonVertices[i];

                vertice.X = vertice.X * angleCos - vertice.Y * angleSin;
                vertice.Y = vertice.Y * angleCos + vertice.X * angleSin;
            }

            _verticesUpdatePreviousOrientation = newOrientation;
        }

        bool HasOverridePosition()
        {
            return m_areaTriggerData.OverrideMoveCurveX.GetValue().OverrideActive
                && m_areaTriggerData.OverrideMoveCurveY.GetValue().OverrideActive
                && m_areaTriggerData.OverrideMoveCurveZ.GetValue().OverrideActive;
        }

        public void UpdateShape()
        {
            if (_shape.IsPolygon())
                UpdatePolygonVertices();
        }

        bool UnitFitToActionRequirement(Unit unit, Unit caster, AreaTriggerAction action)
        {
            switch (action.TargetType)
            {
                case AreaTriggerActionUserTypes.Friend:
                    return caster.IsValidAssistTarget(unit, Global.SpellMgr.GetSpellInfo(action.Param, caster.GetMap().GetDifficultyID()));
                case AreaTriggerActionUserTypes.Enemy:
                    return caster.IsValidAttackTarget(unit, Global.SpellMgr.GetSpellInfo(action.Param, caster.GetMap().GetDifficultyID()));
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
            Unit caster = IsStaticSpawn() ? unit : GetCaster();
            if (caster != null && GetTemplate() != null)
            {
                foreach (AreaTriggerAction action in GetTemplate().Actions)
                {
                    if (IsStaticSpawn() || UnitFitToActionRequirement(unit, caster, action))
                    {
                        switch (action.ActionType)
                        {
                            case AreaTriggerActionTypes.Cast:
                                caster.CastSpell(unit, action.Param, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                    .SetOriginalCastId(m_areaTriggerData.CreatingEffectGUID._value.IsCast() ? m_areaTriggerData.CreatingEffectGUID : ObjectGuid.Empty));
                                break;
                            case AreaTriggerActionTypes.AddAura:
                                caster.AddAura(action.Param, unit);
                                break;
                            case AreaTriggerActionTypes.Teleport:
                                WorldSafeLocsEntry safeLoc = Global.ObjectMgr.GetWorldSafeLoc(action.Param);
                                if (safeLoc != null)
                                {
                                    Player player = caster.ToPlayer();
                                    if (player != null)
                                    {
                                        if (player.GetMapId() != safeLoc.Loc.GetMapId())
                                        {
                                            WorldSafeLocsEntry instanceEntrance = player.GetInstanceEntrance(safeLoc.Loc.GetMapId());
                                            if (instanceEntrance != null)
                                                safeLoc = instanceEntrance;
                                        }
                                        player.TeleportTo(safeLoc.Loc);
                                    }
                                }
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
            if (GetTemplate() != null)
            {
                foreach (AreaTriggerAction action in GetTemplate().Actions)
                {
                    if (action.ActionType == AreaTriggerActionTypes.Cast || action.ActionType == AreaTriggerActionTypes.AddAura)
                        unit.RemoveAurasDueToSpell(action.Param, GetCasterGuid());
                }
            }
        }

        void InitSplineOffsets(List<Vector3> offsets, uint timeToTarget)
        {
            float angleSin = (float)Math.Sin(GetOrientation());
            float angleCos = (float)Math.Cos(GetOrientation());

            // This is needed to rotate the spline, following caster orientation
            List<Vector3> rotatedPoints = new();
            foreach (var offset in offsets)
            {
                float x = GetPositionX() + (offset.X * angleCos - offset.Y * angleSin);
                float y = GetPositionY() + (offset.Y * angleCos + offset.X * angleSin);
                float z = GetPositionZ();

                UpdateAllowedPositionZ(x, y, ref z);
                z += offset.Z;

                rotatedPoints.Add(new Vector3(x, y, z));
            }

            InitSplines(rotatedPoints.ToArray(), timeToTarget);
        }

        public void InitSplines(Vector3[] splinePoints, uint timeToTarget)
        {
            if (splinePoints.Length < 2)
                return;

            _movementTime = 0;

            _spline.InitSpline(splinePoints, splinePoints.Length, EvaluationMode.Linear);
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
                    AreaTriggerRePath reshapeDest = new();
                    reshapeDest.TriggerGUID = GetGUID();
                    SendMessageToSet(reshapeDest, true);
                }

                AreaTriggerRePath reshape = new();
                reshape.TriggerGUID = GetGUID();
                reshape.AreaTriggerSpline = new();
                reshape.AreaTriggerSpline.ElapsedTimeForMovement = GetElapsedTimeForMovement();
                reshape.AreaTriggerSpline.TimeToTarget = timeToTarget;
                reshape.AreaTriggerSpline.Points = splinePoints;
                SendMessageToSet(reshape, true);
            }

            _reachedDestination = false;
        }

        void InitOrbit(AreaTriggerOrbitInfo orbit, uint timeToTarget)
        {
            // Circular movement requires either a center position or an attached unit
            Cypher.Assert(orbit.Center.HasValue || orbit.PathTarget.HasValue);

            // should be sent in object create packets only
            DoWithSuppressingObjectUpdates(() =>
            {
                SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTarget), timeToTarget);
                m_areaTriggerData.ClearChanged(m_areaTriggerData.TimeToTarget);
            });

            SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OrbitPathTarget), orbit.PathTarget.GetValueOrDefault(ObjectGuid.Empty));

            _orbitInfo = orbit;

            _orbitInfo.TimeToTarget = timeToTarget;
            _orbitInfo.ElapsedTimeForMovement = 0;

            if (IsInWorld)
            {
                AreaTriggerRePath reshape = new();
                reshape.TriggerGUID = GetGUID();
                reshape.AreaTriggerOrbit = _orbitInfo;

                SendMessageToSet(reshape, true);
            }
        }

        public bool HasOrbit()
        {
            return _orbitInfo != null;
        }

        Position GetOrbitCenterPosition()
        {
            if (_orbitInfo == null)
                return null;

            if (_orbitInfo.PathTarget.HasValue)
            {
                WorldObject center = Global.ObjAccessor.GetWorldObject(this, _orbitInfo.PathTarget.Value);
                if (center != null)
                    return center;
            }

            if (_orbitInfo.Center.HasValue)
                return new Position(_orbitInfo.Center.Value);

            return null;
        }

        Position CalculateOrbitPosition()
        {
            Position centerPos = GetOrbitCenterPosition();
            if (centerPos == null)
                return GetPosition();

            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            AreaTriggerOrbitInfo cmi = _orbitInfo;

            // AreaTrigger make exactly "Duration / TimeToTarget" loops during his life time
            float pathProgress = (float)cmi.ElapsedTimeForMovement / cmi.TimeToTarget;
            if (createProperties != null && createProperties.MoveCurveId != 0)
                pathProgress = Global.DB2Mgr.GetCurveValueAt(createProperties.MoveCurveId, pathProgress);

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
                radius = MathFunctions.Lerp(cmi.BlendFromRadius, cmi.Radius, blendProgress);
            }

            // Adapt Path progress depending of circle direction
            if (!cmi.CounterClockwise)
                pathProgress *= -1;

            float angle = cmi.InitialAngle + 2.0f * (float)Math.PI * pathProgress;
            float x = centerPos.GetPositionX() + (radius * (float)Math.Cos(angle));
            float y = centerPos.GetPositionY() + (radius * (float)Math.Sin(angle));
            float z = centerPos.GetPositionZ() + cmi.ZOffset;

            float orientation = 0.0f;
            if (createProperties != null && createProperties.FacingCurveId != 0)
                orientation = Global.DB2Mgr.GetCurveValueAt(createProperties.FacingCurveId, GetProgress());

            if (GetCreateProperties() == null || !GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAbsoluteOrientation))
            {
                orientation += angle;
                orientation += cmi.CounterClockwise ? MathFunctions.PiOver4 : -MathFunctions.PiOver4;
            }

            return new Position(x, y, z, orientation);
        }

        void UpdateOrbitPosition(uint diff)
        {
            if (_orbitInfo.StartDelay > GetElapsedTimeForMovement())
                return;

            _orbitInfo.ElapsedTimeForMovement = (int)(GetElapsedTimeForMovement() - _orbitInfo.StartDelay);

            Position pos = CalculateOrbitPosition();

            GetMap().AreaTriggerRelocation(this, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation());

            DebugVisualizePosition();
        }

        void UpdateSplinePosition(uint diff)
        {
            if (_reachedDestination)
                return;

            _movementTime += diff;

            if (_movementTime >= GetTimeToTarget())
            {
                _reachedDestination = true;
                _lastSplineIndex = _spline.Last();

                Vector3 lastSplinePosition = _spline.GetPoint(_lastSplineIndex);
                GetMap().AreaTriggerRelocation(this, lastSplinePosition.X, lastSplinePosition.Y, lastSplinePosition.Z, GetOrientation());

                DebugVisualizePosition();

                _ai.OnSplineIndexReached(_lastSplineIndex);
                _ai.OnDestinationReached();
                return;
            }

            float currentTimePercent = (float)_movementTime / GetTimeToTarget();

            if (currentTimePercent <= 0.0f)
                return;

            AreaTriggerCreateProperties createProperties = GetCreateProperties();
            if (createProperties != null && createProperties.MoveCurveId != 0)
            {
                float progress = Global.DB2Mgr.GetCurveValueAt(createProperties.MoveCurveId, currentTimePercent);
                if (progress < 0.0f || progress > 1.0f)
                {
                    Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (Id: {GetEntry()}, AreaTriggerCreatePropertiesId: (Id: {createProperties.Id.Id}, IsCustom: {createProperties.Id.IsCustom})) has wrong progress ({progress}) caused by curve calculation (MoveCurveId: {createProperties.MoveCurveId})");
                }
                else
                    currentTimePercent = progress;
            }

            int lastPositionIndex = 0;
            float percentFromLastPoint = 0;
            _spline.ComputeIndex(currentTimePercent, ref lastPositionIndex, ref percentFromLastPoint);

            Vector3 currentPosition;
            _spline.Evaluate_Percent(lastPositionIndex, percentFromLastPoint, out currentPosition);

            float orientation = GetStationaryO();
            if (createProperties != null && createProperties.FacingCurveId != 0)
                orientation += Global.DB2Mgr.GetCurveValueAt(createProperties.FacingCurveId, GetProgress());

            if (GetCreateProperties() != null && !GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAbsoluteOrientation) && GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasFaceMovementDir))
            {
                _spline.Evaluate_Derivative(lastPositionIndex, percentFromLastPoint, out Vector3 derivative);
                if (derivative.X != 0.0f || derivative.Y != 0.0f)
                    orientation += MathF.Atan2(derivative.Y, derivative.X);
            }

            GetMap().AreaTriggerRelocation(this, currentPosition.X, currentPosition.Y, currentPosition.Z, orientation);

            DebugVisualizePosition();

            if (_lastSplineIndex != lastPositionIndex)
            {
                _lastSplineIndex = lastPositionIndex;
                _ai.OnSplineIndexReached(_lastSplineIndex);
            }
        }

        void UpdateOverridePosition()
        {
            float progress = GetScaleCurveProgress(m_areaTriggerData.OverrideMoveCurveX, m_areaTriggerData.TimeToTargetPos);

            float x = GetScaleCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveX, progress);
            float y = GetScaleCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveY, progress);
            float z = GetScaleCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveZ, progress);
            float orientation = GetOrientation();

            if (GetCreateProperties().FacingCurveId != 0)
            {
                orientation = Global.DB2Mgr.GetCurveValueAt(GetCreateProperties().FacingCurveId, GetProgress());
                if (GetCreateProperties() == null || !GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAbsoluteOrientation))
                    orientation += GetStationaryO();
            }

            GetMap().AreaTriggerRelocation(this, x, y, z, orientation);
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

        public override bool IsNeverVisibleFor(WorldObject seer, bool allowServersideObjects)
        {
            if (base.IsNeverVisibleFor(seer, allowServersideObjects))
                return true;

            if (IsCustom() && !allowServersideObjects)
            {
                Player seerPlayer = seer.ToPlayer();
                if (seerPlayer != null)
                    return !seerPlayer.IsDebugAreaTriggers;

                return true;
            }

            return false;
        }

        public override void BuildValuesCreate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            m_objectData.WriteCreate(data, flags, this, target);
            m_areaTriggerData.WriteCreate(data, flags, this, target);
        }

        public override void BuildValuesUpdate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(data, flags, this, target);

            if (m_values.HasChanged(TypeId.AreaTrigger))
                m_areaTriggerData.WriteUpdate(data, flags, this, target);
        }

        public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedAreaTriggerMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedAreaTriggerMask.IsAnySet())
                valuesMask.Set((int)TypeId.AreaTrigger);

            WorldPacket buffer = new();
            BuildEntityFragmentsForValuesUpdateForPlayerWithMask(buffer, flags);
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.AreaTrigger])
                m_areaTriggerData.WriteUpdate(buffer, requestedAreaTriggerMask, true, this, target);

            WorldPacket buffer1 = new();
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

        public bool IsCustom() { return _areaTriggerTemplate.Id.IsCustom; }
        public bool IsServerSide() { return _areaTriggerTemplate.Flags.HasFlag(AreaTriggerFlag.IsServerSide); }
        public bool IsStaticSpawn() { return _spawnId != 0; }
        public bool HasActionSetFlag(AreaTriggerActionSetFlag flag) { return _areaTriggerTemplate.ActionSetFlags.HasFlag(flag); }

        [System.Diagnostics.Conditional("DEBUG")]
        void DebugVisualizePosition()
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Player player = caster.ToPlayer();
                if (player != null)
                    if (player.IsDebugAreaTriggers)
                        player.SummonCreature(1, this, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(GetTimeToTarget()));
            }
        }

        public override float GetStationaryX() { return _stationaryPosition.GetPositionX(); }
        public override float GetStationaryY() { return _stationaryPosition.GetPositionY(); }
        public override float GetStationaryZ() { return _stationaryPosition.GetPositionZ(); }
        public override float GetStationaryO() { return _stationaryPosition.GetOrientation(); }
        void RelocateStationaryPosition(Position pos) { _stationaryPosition.Relocate(pos); }

        public bool IsRemoved() { return _isRemoved; }
        public uint GetSpellId() { return m_areaTriggerData.SpellID; }
        public AuraEffect GetAuraEffect() { return _aurEff; }
        public uint GetTimeSinceCreated() { return _timeSinceCreated; }

        public void SetHeightIgnoresScale(bool heightIgnoresScale) { SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.HeightIgnoresScale), heightIgnoresScale); }

        public uint GetTimeToTarget() { return m_areaTriggerData.TimeToTarget; }
        public void SetTimeToTarget(uint timeToTarget) { SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTarget), timeToTarget); }

        public uint GetTimeToTargetScale() { return m_areaTriggerData.TimeToTargetScale; }
        public void SetTimeToTargetScale(uint timeToTargetScale) { SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTargetScale), timeToTargetScale); }

        public uint GetTimeToTargetExtraScale() { return m_areaTriggerData.TimeToTargetExtraScale; }
        public void SetTimeToTargetExtraScale(uint timeToTargetExtraScale) { SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTargetExtraScale), timeToTargetExtraScale); }

        public uint GetTimeToTargetPos() { return m_areaTriggerData.TimeToTargetPos; }
        public void SetTimeToTargetPos(uint timeToTargetPos) { SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTargetPos), timeToTargetPos); }

        public int GetDuration() { return _duration; }
        public int GetTotalDuration() { return _totalDuration; }

        public void Delay(int delaytime) { SetDuration(GetDuration() - delaytime); }

        public List<ObjectGuid> GetInsideUnits() { return _insideUnits; }

        public AreaTriggerCreateProperties GetCreateProperties() { return _areaTriggerCreateProperties; }

        public override ObjectGuid GetCreatorGUID() { return GetCasterGuid(); }
        public override ObjectGuid GetOwnerGUID() { return GetCasterGuid(); }
        public ObjectGuid GetCasterGuid() { return m_areaTriggerData.Caster; }

        public AreaTriggerShapeInfo GetShape() { return _shape; }
        public Vector3 GetRollPitchYaw() { return _rollPitchYaw; }
        public Vector3 GetTargetRollPitchYaw() { return _targetRollPitchYaw; }

        public bool HasSplines() { return !_spline.Empty(); }
        public Spline<int> GetSpline() { return _spline; }
        public uint GetElapsedTimeForMovement() { return GetTimeSinceCreated(); } // @todo: research the right value, in sniffs both timers are nearly identical

        public AreaTriggerOrbitInfo GetOrbit() { return _orbitInfo; }

        AreaTriggerFieldData m_areaTriggerData;

        ulong _spawnId;

        ObjectGuid _targetGuid;

        AuraEffect _aurEff;

        Position _stationaryPosition;
        AreaTriggerShapeInfo _shape;
        int _duration;
        int _totalDuration;
        uint _timeSinceCreated;
        float _verticesUpdatePreviousOrientation;
        bool _isRemoved;

        Position _rollPitchYaw;
        Position _targetRollPitchYaw;
        List<Position> _polygonVertices;
        Spline<int> _spline;

        bool _reachedDestination;
        int _lastSplineIndex;
        uint _movementTime;

        AreaTriggerOrbitInfo _orbitInfo;

        AreaTriggerCreateProperties _areaTriggerCreateProperties;
        AreaTriggerTemplate _areaTriggerTemplate;
        List<ObjectGuid> _insideUnits = new();

        AreaTriggerAI _ai;

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            AreaTrigger Owner;
            ObjectFieldData ObjectMask = new();
            AreaTriggerFieldData AreaTriggerMask = new();

            public ValuesUpdateForPlayerWithMaskSender(AreaTrigger owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), AreaTriggerMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject updateObject);
                player.SendPacket(updateObject);
            }
        }
    }
}
