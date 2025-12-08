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

            m_entityFragments.Add(EntityFragment.Tag_AreaTrigger, false);

            m_areaTriggerData = new AreaTriggerFieldData();

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

            SetShape(GetCreateProperties().Shape);

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
                    spellVisual.SpellXSpellVisualID = caster != null ? caster.GetCastSpellXSpellVisualId(spellForVisuals) : spellForVisuals.GetSpellXSpellVisualId();
            }
            if (spellForVisuals != null)
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.SpellForVisuals), spellForVisuals.Id);

            SetSpellVisual(spellVisual);

            if (!IsStaticSpawn())
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTargetScale), GetCreateProperties().TimeToTargetScale != 0 ? GetCreateProperties().TimeToTargetScale : m_areaTriggerData.Duration);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.BoundsRadius2D), GetCreateProperties().Shape.GetMaxSearchRadius());
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.DecalPropertiesID), GetCreateProperties().DecalPropertiesId);
            if (IsServerSide())
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.DecalPropertiesID), 24u); // Blue decal, for .debug areatrigger visibility

            SetScaleCurve(areaTriggerData.ModifyValue(m_areaTriggerData.ExtraScaleCurve), 1.0f);

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
                        ScaleCurveData overrideScale = new();
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

            AreaTriggerFieldFlags fieldFlags()
            {
                var flags = GetCreateProperties().Flags;
                AreaTriggerFieldFlags fieldFlags = AreaTriggerFieldFlags.None;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAbsoluteOrientation))
                    fieldFlags |= AreaTriggerFieldFlags.AbsoluteOrientation;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasDynamicShape))
                    fieldFlags |= AreaTriggerFieldFlags.DynamicShape;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasAttached))
                    fieldFlags |= AreaTriggerFieldFlags.Attached;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasFaceMovementDir))
                    fieldFlags |= AreaTriggerFieldFlags.FaceMovementDir;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.HasFollowsTerrain))
                    fieldFlags |= AreaTriggerFieldFlags.FollowsTerrain;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.AlwaysExterior))
                    fieldFlags |= AreaTriggerFieldFlags.AlwaysExterior;
                return fieldFlags;
            }

            ReplaceAllAreaTriggerFlags(fieldFlags());

            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.MovementStartTime), GameTime.GetGameTimeMS());
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.CreationTime), GameTime.GetGameTimeMS());

            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ScaleCurveId), GetCreateProperties().ScaleCurveId);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.FacingCurveId), GetCreateProperties().FacingCurveId);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.MorphCurveId), GetCreateProperties().MorphCurveId);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.MoveCurveId), GetCreateProperties().MoveCurveId);

            if (caster != null)
                PhasingHandler.InheritPhaseShift(this, caster);
            else if (IsStaticSpawn() && spawnData != null)
            {
                if (spawnData.PhaseUseFlags != 0 || spawnData.PhaseId != 0 || spawnData.PhaseGroup != 0)
                    PhasingHandler.InitDbPhaseShift(GetPhaseShift(), spawnData.PhaseUseFlags, spawnData.PhaseId, spawnData.PhaseGroup);
            }

            if (target != null && HasAreaTriggerFlag(AreaTriggerFieldFlags.Attached))
                m_movementInfo.transport.guid = target.GetGUID();

            if (!IsStaticSpawn())
                UpdatePositionData();

            UpdateShape();


            GetCreateProperties().Movement.Switch(
                _ => SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.PathType), (byte)AreaTriggerPathType.None),
                splineInfo => InitSplineOffsets(splineInfo, null, GetCreateProperties().SpeedIsTime),
                orbitInfo =>
                {
                    AreaTriggerOrbitInfo orbit = orbitInfo;
                    if (target != null && HasAreaTriggerFlag(AreaTriggerFieldFlags.Attached))
                        orbit.PathTarget = target.GetGUID();
                    else
                        orbit.Center = pos;

                    InitOrbit(orbit, null, GetCreateProperties().SpeedIsTime);
                });

            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.Facing), _stationaryPosition.GetOrientation());

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

            if (!IsStaticSpawn())
            {
                // "If" order matter here, Orbit > Attached > Splines
                if (HasOverridePosition())
                {
                    UpdateOverridePosition();
                }
                else if (HasOrbit())
                {
                    UpdateOrbitPosition();
                }
                else if (HasAreaTriggerFlag(AreaTriggerFieldFlags.Attached))
                {
                    Unit target = GetTarget();
                    if (target != null)
                    {
                        float orientation = 0.0f;
                        if (m_areaTriggerData.FacingCurveId != 0)
                            orientation = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetProgress());

                        if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
                            orientation += target.GetOrientation();

                        GetMap().AreaTriggerRelocation(this, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), orientation);
                    }
                }
                else if (HasSplines())
                {
                    UpdateSplinePosition(_spline);
                }
                else
                {
                    if (m_areaTriggerData.FacingCurveId != 0)
                    {
                        float orientation = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetProgress());
                        if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
                            orientation += m_areaTriggerData.Facing;

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

        uint GetTimeSinceCreated()
        {
            uint now = GameTime.GetGameTimeMS();
            if (now >= m_areaTriggerData.CreationTime)
                return now - m_areaTriggerData.CreationTime;
            return 0;
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

        public void SetSpellVisual(SpellCastVisual visual)
        {
            SpellCastVisualField spellCastVisual = m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.SpellVisual);
            SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, visual.SpellXSpellVisualID);
            SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, visual.ScriptVisualID);
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
            else if (m_areaTriggerData.ScaleCurveId != 0)
                scale *= Math.Max(Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.ScaleCurveId, GetScaleCurveProgress(m_areaTriggerData.OverrideScaleCurve, m_areaTriggerData.TimeToTargetScale)), 0.000001f);

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
            ScaleCurveData curveTemplate = new();
            curveTemplate.Curve = constantValue;
            SetScaleCurve(scaleCurve, curveTemplate);
        }

        void SetScaleCurve(ScaleCurve scaleCurve, Vector2[] points, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            ScaleCurveData curveTemplate = new();
            curveTemplate.StartTimeOffset = startTimeOffset.GetValueOrDefault(GetTimeSinceCreated());
            curveTemplate.Mode = interpolation;
            curveTemplate.CurvePoints = points;

            SetScaleCurve(scaleCurve, curveTemplate);
        }

        void ClearScaleCurve(ScaleCurve scaleCurve)
        {
            SetScaleCurve(scaleCurve, null);
        }

        void SetScaleCurve(ScaleCurve scaleCurve, ScaleCurveData curve)
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
                var curvePoints = curve.CurvePoints;
                if (curvePoints != null)
                {
                    CurveInterpolationMode mode = curve.Mode;
                    if (curvePoints[1].X < curvePoints[0].X)
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

                    for (var i = 0; i < curvePoints.Length; ++i)
                    {
                        point.Relocate(curvePoints[i].X, curvePoints[i].Y);
                        SetUpdateFieldValue(ref scaleCurve.ModifyValue(scaleCurve.Points, i), point);
                    }
                }
            }
        }

        void UpdateTargetList()
        {
            List<Unit> targetList = new();

            m_areaTriggerData.ShapeData.Visit(shape =>
            {
                if (shape is AreaTriggerSphere)
                    SearchUnitInSphere(shape, targetList);
                else if (shape is AreaTriggerBox)
                    SearchUnitInBox(shape, targetList);
                else if (shape is AreaTriggerPolygon)
                    SearchUnitInPolygon(shape, targetList);
                else if (shape is AreaTriggerCylinder)
                    SearchUnitInCylinder(shape, targetList);
                else if (shape is AreaTriggerDisk)
                    SearchUnitInDisk(shape, targetList);
                else if (shape is AreaTriggerBoundedPlane)
                    SearchUnitInBoundedPlane(shape, targetList);
            });

            if (GetTemplate() != null)
            {
                var conditions = Global.ConditionMgr.GetConditionsForAreaTrigger(GetTemplate().Id.Id, GetTemplate().Id.IsCustom);
                targetList.RemoveAll(target =>
                {
                    if (GetCasterGUID() == target.GetGUID())
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

        void SearchUnitInSphere(AreaTriggerSphere sphere, List<Unit> targetList)
        {
            float progress = GetProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float radius = MathFunctions.Lerp(sphere.Radius, sphere.RadiusTarget, progress) * scale;

            SearchUnits(targetList, radius, true);
        }

        void SearchUnitInBox(AreaTriggerBox box, List<Unit> targetList)
        {
            float progress = GetProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            unsafe
            {
                float scale = CalcCurrentScale();
                float extentsX = MathFunctions.Lerp(box.Extents.GetValue().X, box.ExtentsTarget.GetValue().X, progress) * scale;
                float extentsY = MathFunctions.Lerp(box.Extents.GetValue().Y, box.ExtentsTarget.GetValue().Y, progress) * scale;
                float extentsZ = MathFunctions.Lerp(box.Extents.GetValue().Z, box.ExtentsTarget.GetValue().Z, progress) * scale;
                float radius = MathF.Sqrt(extentsX * extentsX + extentsY * extentsY);

                SearchUnits(targetList, radius, false);

                Position boxCenter = GetPosition();
                targetList.RemoveAll(unit => !unit.IsWithinBox(boxCenter, extentsX, extentsY, extentsZ / 2));
            }
        }

        void SearchUnitInPolygon(AreaTriggerPolygon polygon, List<Unit> targetList)
        {
            float progress = GetProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            float height = MathFunctions.Lerp(polygon.Height, polygon.HeightTarget, progress);
            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            SearchUnits(targetList, GetMaxSearchRadius(), false);

            targetList.RemoveAll(unit => unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ || !unit.IsInPolygon2D(this, _polygonVertices));
        }

        void SearchUnitInCylinder(AreaTriggerCylinder cylinder, List<Unit> targetList)
        {
            float progress = GetProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float radius = MathFunctions.Lerp(cylinder.Radius, cylinder.RadiusTarget, progress) * scale;
            float height = MathFunctions.Lerp(cylinder.Height, cylinder.HeightTarget, progress);
            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.HeightIgnoresScale))
                height *= scale;

            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            SearchUnits(targetList, radius, false);

            targetList.RemoveAll(unit => unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        void SearchUnitInDisk(AreaTriggerDisk disk, List<Unit> targetList)
        {
            float progress = GetProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float innerRadius = MathFunctions.Lerp(disk.InnerRadius, disk.InnerRadiusTarget, progress) * scale;
            float outerRadius = MathFunctions.Lerp(disk.OuterRadius, disk.OuterRadiusTarget, progress) * scale;
            float height = MathFunctions.Lerp(disk.Height, disk.HeightTarget, progress);
            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.HeightIgnoresScale))
                height *= scale;

            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            SearchUnits(targetList, outerRadius, false);

            targetList.RemoveAll(unit => unit.IsInDist2d(this, innerRadius) || unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        void SearchUnitInBoundedPlane(AreaTriggerBoundedPlane boundedPlane, List<Unit> targetList)
        {
            float progress = GetProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            unsafe
            {
                float scale = CalcCurrentScale();
                float extentsX = MathFunctions.Lerp(boundedPlane.Extents.GetValue().X, boundedPlane.ExtentsTarget.GetValue().X, progress) * scale;
                float extentsY = MathFunctions.Lerp(boundedPlane.Extents.GetValue().Y, boundedPlane.ExtentsTarget.GetValue().Y, progress) * scale;
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
                HandleUnitEnter(unit);

            foreach (ObjectGuid exitUnitGuid in exitUnits)
            {
                Unit leavingUnit = Global.ObjAccessor.GetUnit(this, exitUnitGuid);
                if (leavingUnit != null)
                    HandleUnitExitInternal(leavingUnit);
            }

            UpdateHasPlayersFlag();

            if (IsStaticSpawn())
                SetActive(!_insideUnits.Empty());
        }

        void HandleUnitEnter(Unit unit)
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
            unit.EnterAreaTrigger(this);
        }

        void HandleUnitExitInternal(Unit unit)
        {
            Player player = unit.ToPlayer();
            if (player != null)
            {
                if (player.IsDebugAreaTriggers)
                    player.SendSysMessage(CypherStrings.DebugAreatriggerEntityLeft, GetEntry(), IsCustom(), IsStaticSpawn(), _spawnId);

                player.UpdateQuestObjectiveProgress(QuestObjectiveType.AreaTriggerExit, (int)GetEntry(), 1);

                if (GetTemplate().ActionSetId != 0)
                    player.UpdateCriteria(CriteriaType.LeaveAreaTriggerWithActionSet, GetTemplate().ActionSetId);
            }

            UndoActions(unit);

            _ai.OnUnitExit(unit);
            unit.ExitAreaTrigger(this);
        }

        public void HandleUnitExit(Unit unit)
        {
            _insideUnits.Remove(unit.GetGUID());

            HandleUnitExitInternal(unit);

            UpdateHasPlayersFlag();
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
            return Global.ObjAccessor.GetUnit(this, GetCasterGUID());
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

        void SetShape(AreaTriggerShapeInfo shape)
        {
            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);

            shape.Data.Switch
            (
                sphereInfo =>
                {
                    SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ShapeType), (byte)0);
                    var sphere = areaTriggerData.ModifyValue<AreaTriggerSphere>(m_areaTriggerData.ShapeData);
                    SetUpdateFieldValue(sphere.ModifyValue(sphere.Radius), sphereInfo.Radius);
                    SetUpdateFieldValue(sphere.ModifyValue(sphere.RadiusTarget), sphereInfo.RadiusTarget);
                },
                boxInfo =>
                {
                    SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ShapeType), (byte)1);
                    var box = areaTriggerData.ModifyValue<AreaTriggerBox>(m_areaTriggerData.ShapeData);
                    SetUpdateFieldValue(box.ModifyValue(box.Extents), boxInfo.Extents);
                    SetUpdateFieldValue(box.ModifyValue(box.ExtentsTarget), boxInfo.ExtentsTarget);
                },
                polygonInfo =>
                {
                    SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ShapeType), (byte)3);
                    var polygon = areaTriggerData.ModifyValue<AreaTriggerPolygon>(m_areaTriggerData.ShapeData);
                    var vertices = polygon.ModifyValue(polygon.Vertices);
                    ClearDynamicUpdateFieldValues(vertices);
                    foreach (Vector2 vertex in polygonInfo.PolygonVertices)
                        AddDynamicUpdateFieldValue(vertices, vertex);
                    var verticesTarget = polygon.ModifyValue(polygon.VerticesTarget);
                    ClearDynamicUpdateFieldValues(verticesTarget);
                    foreach (Vector2 vertex in polygonInfo.PolygonVerticesTarget)
                        AddDynamicUpdateFieldValue(verticesTarget, vertex);
                    SetUpdateFieldValue(polygon.ModifyValue(polygon.Height), polygonInfo.Height);
                    SetUpdateFieldValue(polygon.ModifyValue(polygon.HeightTarget), polygonInfo.HeightTarget);
                },
                cylinderInfo =>
                {
                    SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ShapeType), (byte)4);
                    var cylinder = areaTriggerData.ModifyValue<AreaTriggerCylinder>(m_areaTriggerData.ShapeData);
                    SetUpdateFieldValue(cylinder.ModifyValue(cylinder.Radius), cylinderInfo.Radius);
                    SetUpdateFieldValue(cylinder.ModifyValue(cylinder.RadiusTarget), cylinderInfo.RadiusTarget);
                    SetUpdateFieldValue(cylinder.ModifyValue(cylinder.Height), cylinderInfo.Height);
                    SetUpdateFieldValue(cylinder.ModifyValue(cylinder.HeightTarget), cylinderInfo.HeightTarget);
                    SetUpdateFieldValue(cylinder.ModifyValue(cylinder.LocationZOffset), cylinderInfo.LocationZOffset);
                    SetUpdateFieldValue(cylinder.ModifyValue(cylinder.LocationZOffsetTarget), cylinderInfo.LocationZOffsetTarget);
                },
                diskInfo =>
                {
                    SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ShapeType), (byte)7);
                    var disk = areaTriggerData.ModifyValue<AreaTriggerDisk>(m_areaTriggerData.ShapeData);
                    SetUpdateFieldValue(disk.ModifyValue(disk.InnerRadius), diskInfo.InnerRadius);
                    SetUpdateFieldValue(disk.ModifyValue(disk.InnerRadiusTarget), diskInfo.InnerRadiusTarget);
                    SetUpdateFieldValue(disk.ModifyValue(disk.OuterRadius), diskInfo.OuterRadius);
                    SetUpdateFieldValue(disk.ModifyValue(disk.OuterRadiusTarget), diskInfo.OuterRadiusTarget);
                    SetUpdateFieldValue(disk.ModifyValue(disk.Height), diskInfo.Height);
                    SetUpdateFieldValue(disk.ModifyValue(disk.HeightTarget), diskInfo.HeightTarget);
                    SetUpdateFieldValue(disk.ModifyValue(disk.LocationZOffset), diskInfo.LocationZOffset);
                    SetUpdateFieldValue(disk.ModifyValue(disk.LocationZOffsetTarget), diskInfo.LocationZOffsetTarget);
                },
                boundedPlaneInfo =>
                {
                    SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ShapeType), (byte)8);
                    var boundedPlane = areaTriggerData.ModifyValue<AreaTriggerBoundedPlane>(m_areaTriggerData.ShapeData);
                    SetUpdateFieldValue(boundedPlane.ModifyValue(boundedPlane.Extents), boundedPlaneInfo.Extents);
                    SetUpdateFieldValue(boundedPlane.ModifyValue(boundedPlane.ExtentsTarget), boundedPlaneInfo.ExtentsTarget);
                }
            );
        }

        public float GetMaxSearchRadius()
        {
            return m_areaTriggerData.BoundsRadius2D * CalcCurrentScale();
        }

        void UpdatePolygonVertices()
        {
            AreaTriggerPolygon shape = m_areaTriggerData.ShapeData.Get<AreaTriggerPolygon>();
            float newOrientation = GetOrientation();

            // No need to recalculate, orientation didn't change
            if (MathFunctions.fuzzyEq(_verticesUpdatePreviousOrientation, newOrientation) && shape.VerticesTarget.Empty())
                return;

            _polygonVertices.AddRange(shape.Vertices._values.Select(p => new Position(p.X, p.Y)));

            if (!shape.Vertices.Empty())
            {
                float progress = GetProgress();
                if (m_areaTriggerData.MorphCurveId != 0)
                    progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

                for (var i = 0; i < _polygonVertices.Count; ++i)
                {
                    Vector2 vertex = _polygonVertices[i];
                    Vector2 vertexTarget = shape.VerticesTarget[i];

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
            if (m_areaTriggerData.ShapeData.Is<AreaTriggerPolygon>())
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
                            case AreaTriggerActionTypes.Tavern:
                            {
                                Player player = caster.ToPlayer();
                                if (player != null)
                                {
                                    player.GetRestMgr().SetInnTrigger(new InnAreaTrigger(false));
                                    player.GetRestMgr().SetRestFlag(RestFlag.Tavern);
                                }
                                break;
                            }
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
                    switch (action.ActionType)
                    {
                        case AreaTriggerActionTypes.Cast:
                            goto case AreaTriggerActionTypes.AddAura;
                        case AreaTriggerActionTypes.AddAura:
                            unit.RemoveAurasDueToSpell(action.Param, GetCasterGUID());
                            break;
                        case AreaTriggerActionTypes.Tavern:
                            Player player = unit.ToPlayer();
                            if (player != null)
                                player.GetRestMgr().SetInnTrigger(null);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        void InitSplineOffsets(List<Vector3> offsets, float? overrideSpeed = null, bool speedIsTimeInSeconds = false)
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

            InitSplines(rotatedPoints.ToArray(), overrideSpeed, speedIsTimeInSeconds);
        }

        public void InitSplines(Vector3[] splinePoints, float? overrideSpeed = null, bool speedIsTimeInSeconds = false)
        {
            if (splinePoints.Length < 2)
                return;

            _spline = new Spline<float>();
            _spline.InitSpline(splinePoints, splinePoints.Length, EvaluationMode.Linear, _stationaryPosition.GetOrientation());
            _spline.InitLengths();

            float speed = overrideSpeed.GetValueOrDefault(GetCreateProperties().Speed);
            if (speed <= 0.0f)
                speed = 1.0f;

            uint timeToTarget = (speedIsTimeInSeconds ? speed : _spline.Length() / speed) * Time.InMilliseconds;

            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTarget), timeToTarget);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.MovementStartTime), GameTime.GetGameTimeMS());

            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.PathType), (int)AreaTriggerPathType.Spline);
            var pathData = areaTriggerData.ModifyValue<AreaTriggerSplineCalculator>(m_areaTriggerData.PathData);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.Catmullrom), splinePoints.Length >= 4);
            var points = pathData.ModifyValue(pathData.Points);
            ClearDynamicUpdateFieldValues(points);
            foreach (Vector3 point in splinePoints)
                AddDynamicUpdateFieldValue(points, point);

            _reachedDestination = false;
        }

        uint GetElapsedTimeForMovement()
        {
            uint now = GameTime.GetGameTimeMS();
            if (now >= m_areaTriggerData.MovementStartTime)
                return now - m_areaTriggerData.MovementStartTime;
            return 0;
        }

        void InitOrbit(AreaTriggerOrbitInfo orbit, float? overrideSpeed = null, bool speedIsTimeInSeconds = false)
        {
            // Circular movement requires either a center position or an attached unit
            Cypher.Assert(orbit.Center.HasValue || orbit.PathTarget.HasValue);

            float speed = overrideSpeed.GetValueOrDefault(GetCreateProperties().Speed);
            if (speed <= 0.0f)
                speed = 1.0f;

            uint timeToTarget = (uint)(speedIsTimeInSeconds ? speed : (uint)(orbit.Radius * 2.0f * MathF.PI / speed)) * Time.InMilliseconds;

            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTarget), timeToTarget);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.MovementStartTime), GameTime.GetGameTimeMS());
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.OrbitPathTarget), orbit.PathTarget.GetValueOrDefault(ObjectGuid.Empty));
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ZOffset), orbit.ZOffset);
            if (orbit.CanLoop)
                SetAreaTriggerFlag(AreaTriggerFieldFlags.CanLoop);
            else
                RemoveAreaTriggerFlag(AreaTriggerFieldFlags.CanLoop);

            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.PathType), (int)AreaTriggerPathType.Orbit);
            var pathData = areaTriggerData.ModifyValue<AreaTriggerOrbit>(m_areaTriggerData.PathData);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.CounterClockwise), orbit.CounterClockwise);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.Center), orbit.Center.GetValueOrDefault(new Position()));
            SetUpdateFieldValue(pathData.ModifyValue(pathData.Radius), orbit.Radius);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.InitialAngle), orbit.InitialAngle);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.BlendFromRadius), orbit.BlendFromRadius);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.ExtraTimeForBlending), orbit.ExtraTimeForBlending);
        }

        Position GetOrbitCenterPosition()
        {
            AreaTriggerOrbit orbit = m_areaTriggerData.PathData.Get<AreaTriggerOrbit>();
            if (orbit == null)
                return null;

            if (!m_areaTriggerData.OrbitPathTarget.GetValue().IsEmpty())
            {
                WorldObject center = Global.ObjAccessor.GetWorldObject(this, m_areaTriggerData.OrbitPathTarget);
                if (center != null)
                    return center;
            }

            return new Position(orbit.Center);
        }

        Position CalculateOrbitPosition()
        {
            Position centerPos = GetOrbitCenterPosition();
            if (centerPos == null)
                return GetPosition();

            AreaTriggerOrbit cmi = m_areaTriggerData.PathData.Get<AreaTriggerOrbit>();

            // AreaTrigger make exactly "Duration / TimeToTarget" loops during his life time
            float pathProgress = (float)(GetElapsedTimeForMovement() + cmi.ExtraTimeForBlending) / (float)GetTimeToTarget();
            if (m_areaTriggerData.MoveCurveId != 0)
                pathProgress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MoveCurveId, pathProgress);

            // We already made one circle and can't loop
            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.CanLoop))
                pathProgress = Math.Min(1.0f, pathProgress);

            float radius = cmi.Radius;
            if (pathProgress <= 1.0f && MathFunctions.fuzzyNe(cmi.BlendFromRadius, radius))
            {
                float blendCurve = (cmi.BlendFromRadius - radius) / radius;
                MathFunctions.RoundToInterval(ref blendCurve, 1.0f, 4.0f);
                float blendProgress = Math.Min(1.0f, pathProgress / blendCurve * 0.63661975f);
                radius = MathFunctions.Lerp(cmi.BlendFromRadius, radius, blendProgress);
            }

            // Adapt Path progress depending of circle direction
            if (!cmi.CounterClockwise)
                pathProgress *= -1;

            float angle = cmi.InitialAngle + 2.0f * (float)Math.PI * pathProgress;
            float x = centerPos.GetPositionX() + (radius * (float)Math.Cos(angle));
            float y = centerPos.GetPositionY() + (radius * (float)Math.Sin(angle));
            float z = centerPos.GetPositionZ() + m_areaTriggerData.ZOffset;

            float orientation = 0.0f;
            if (m_areaTriggerData.FacingCurveId != 0)
                orientation = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetProgress());

            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
            {
                orientation += angle;
                orientation += cmi.CounterClockwise ? MathFunctions.PiOver4 : -MathFunctions.PiOver4;
            }

            return new Position(x, y, z, orientation);
        }

        void UpdateOrbitPosition()
        {
            Position pos = CalculateOrbitPosition();

            GetMap().AreaTriggerRelocation(this, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation());

            DebugVisualizePosition();
        }

        void UpdateSplinePosition(Spline<float> spline)
        {
            if (_reachedDestination)
                return;

            if (GetElapsedTimeForMovement() >= GetTimeToTarget())
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

            float currentTimePercent = (float)GetElapsedTimeForMovement() / GetTimeToTarget();

            if (currentTimePercent <= 0.0f)
                return;

            if (m_areaTriggerData.MoveCurveId != 0)
            {
                float progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MoveCurveId, currentTimePercent);
                if (progress < 0.0f || progress > 1.0f)
                {
                    AreaTriggerCreateProperties createProperties = GetCreateProperties();
                    Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (Id: {GetEntry()}, AreaTriggerCreatePropertiesId: (Id: {createProperties.Id.Id}, IsCustom: {createProperties.Id.IsCustom})) has wrong progress ({progress}) caused by curve calculation (MoveCurveId: {m_areaTriggerData.MoveCurveId})");
                }
                else
                    currentTimePercent = progress;
            }

            int lastPositionIndex = 0;
            float percentFromLastPoint = 0;
            _spline.ComputeIndex(currentTimePercent, ref lastPositionIndex, ref percentFromLastPoint);

            Vector3 currentPosition;
            _spline.Evaluate_Percent(lastPositionIndex, percentFromLastPoint, out currentPosition);

            float orientation = _stationaryPosition.GetOrientation();
            if (m_areaTriggerData.FacingCurveId != 0)
                orientation += Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetProgress());

            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation) && HasAreaTriggerFlag(AreaTriggerFieldFlags.FaceMovementDir))
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

            if (m_areaTriggerData.FacingCurveId != 0)
            {
                orientation = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetProgress());
                if (HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
                    orientation += m_areaTriggerData.Facing;
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

        void UpdateHasPlayersFlag()
        {
            if (_insideUnits.Any(guid => guid.IsPlayer()))
                SetAreaTriggerFlag(AreaTriggerFieldFlags.HasPlayers);
            else
                RemoveAreaTriggerFlag(AreaTriggerFieldFlags.HasPlayers);
        }

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

        public override Position GetStationaryPosition() { return _stationaryPosition; }
        void RelocateStationaryPosition(Position pos) { _stationaryPosition.Relocate(pos); }

        public bool IsRemoved() { return _isRemoved; }
        public uint GetSpellId() { return m_areaTriggerData.SpellID; }
        public AuraEffect GetAuraEffect() { return _aurEff; }

        public AreaTriggerFieldFlags GetAreaTriggerFlags() { return (AreaTriggerFieldFlags)m_areaTriggerData.Flags.GetValue(); }
        public bool HasAreaTriggerFlag(AreaTriggerFieldFlags flag)
        {
            return GetAreaTriggerFlags().HasFlag(flag);
        }
        public void SetAreaTriggerFlag(AreaTriggerFieldFlags flag) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.Flags), (uint)flag); }
        public void RemoveAreaTriggerFlag(AreaTriggerFieldFlags flag) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.Flags), (uint)flag); }
        public void ReplaceAllAreaTriggerFlags(AreaTriggerFieldFlags flag) { SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.Flags), (uint)flag); }

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

        public override ObjectGuid GetCreatorGUID() { return GetCasterGUID(); }
        public override ObjectGuid GetOwnerGUID() { return GetCasterGUID(); }
        public ObjectGuid GetCasterGUID() { return m_areaTriggerData.Caster; }

        public bool HasSplines() { return _spline != null && !_spline.Empty(); }
        public Spline<float> GetSpline() { return _spline; }

        bool HasOrbit() { return m_areaTriggerData.PathData.Is<AreaTriggerOrbit>(); }
        public AreaTriggerOrbit GetOrbit() { return m_areaTriggerData.PathData.Get<AreaTriggerOrbit>(); }

        public AreaTriggerFieldData m_areaTriggerData;

        ulong _spawnId;

        ObjectGuid _targetGuid;

        AuraEffect _aurEff;

        Position _stationaryPosition;
        int _duration;
        int _totalDuration;
        float _verticesUpdatePreviousOrientation;
        bool _isRemoved;

        List<Position> _polygonVertices;
        Spline<float> _spline;

        bool _reachedDestination;
        int _lastSplineIndex;

        AreaTriggerOrbitInfo _orbitInfo;

        AreaTriggerCreateProperties _areaTriggerCreateProperties;
        AreaTriggerTemplate _areaTriggerTemplate;
        List<ObjectGuid> _insideUnits = new();

        AreaTriggerAI _ai;

        class ValuesUpdateForPlayerWithMaskSender
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

            public static implicit operator IDoWork<Player>(ValuesUpdateForPlayerWithMaskSender obj) => obj.Invoke;
        }

        class ScaleCurveData
        {
            public uint StartTimeOffset;
            public CurveInterpolationMode Mode;

            public Vector2[] CurvePoints;
            public float Curve;
        }
    }
}
