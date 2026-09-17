// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.GameMath;
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
            _verticesUpdatePreviousRotation = new();
            _reachedDestination = true;

            ObjectTypeId = TypeId.AreaTrigger;

            m_updateFlag.Stationary = true;

            EntityFragments.Add(EntityFragment.Tag_AreaTrigger, false);

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
                HandleUnitEnterExit(new List<Unit>(), AreaTriggerExitReason.ByExpire);

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
            {
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTargetScale), GetCreateProperties().TimeToTargetScale != 0 ? GetCreateProperties().TimeToTargetScale : m_areaTriggerData.Duration);
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTargetPos), m_areaTriggerData.Duration);
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTargetShape), m_areaTriggerData.Duration);
            }
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.BoundsRadius2D), GetCreateProperties().Shape.GetMaxSearchRadius());
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.DecalPropertiesID), GetCreateProperties().DecalPropertiesId);
            if (IsServerSide())
                SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.DecalPropertiesID), 24u); // Blue decal, for .debug areatrigger visibility

            SetOverrideCurve(areaTriggerData.ModifyValue(m_areaTriggerData.ExtraScaleCurve), 1.0f);

            if (caster != null && spellInfo != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                {
                    float multiplier = 1.0f;
                    int flat = 0;
                    modOwner.GetSpellModValues(spellInfo, SpellModOp.Radius, spell, (float)m_areaTriggerData.BoundsRadius2D, ref flat, ref multiplier);
                    if (multiplier != 1.0f)
                    {
                        OverrideCurveData overrideScale = new();
                        overrideScale.Curve = multiplier;
                        SetOverrideCurve(areaTriggerData.ModifyValue(m_areaTriggerData.OverrideScaleCurve), overrideScale);
                    }
                }
            }

            VisualAnim visualAnim = areaTriggerData.ModifyValue(m_areaTriggerData.VisualAnim);
            if (GetCreateProperties().AnimId != -1)
                SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.AnimationDataID), (ushort)GetCreateProperties().AnimId);
            SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.AnimKitID), GetCreateProperties().AnimKitId);
            if (GetCreateProperties() != null && GetCreateProperties().Flags.HasFlag(AreaTriggerCreatePropertiesFlag.VisualAnimIsDecay))
                SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.IsDecay), true);

            SetUpdateFieldValue(areaTriggerData.ModifyValue(areaTriggerData.PositionalSoundKitID), GetCreateProperties().PositionalSoundKitId);

            AreaTriggerFieldFlags fieldFlags()
            {
                var flags = GetCreateProperties().Flags;
                AreaTriggerFieldFlags fieldFlags = AreaTriggerFieldFlags.None;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.AbsoluteOrientation))
                    fieldFlags |= AreaTriggerFieldFlags.AbsoluteOrientation;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.FaceMovementDir))
                    fieldFlags |= AreaTriggerFieldFlags.FaceMovementDir;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.FollowsTerrain))
                    fieldFlags |= AreaTriggerFieldFlags.FollowsTerrain;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.AlwaysExterior))
                    fieldFlags |= AreaTriggerFieldFlags.AlwaysExterior;
                if (flags.HasFlag(AreaTriggerCreatePropertiesFlag.UsesUnitRawFacing))
                    fieldFlags |= AreaTriggerFieldFlags.UsesUnitRawFacing;
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

            if (target != null && aurEff != null)
            {
                SetAreaTriggerFlag(AreaTriggerFieldFlags.Attached);
                m_movementInfo.transport.guid = target.GetGUID();
                m_updateFlag.MovementTransport = true;
            }

            // movement on transport of areatriggers on unit is handled by themself
            ITransport transport = null;
            if (caster != null)
            {
                transport = m_movementInfo.transport.guid.IsEmpty() ? caster.GetTransport() : null;
                if (transport != null)
                {
                    // This object must be added to transport before adding to map for the client to properly display it
                    transport.AddPassenger(this, transport.GetPositionOffsetTo(pos));
                }
            }

            if (!IsStaticSpawn())
                UpdatePositionData();

            GetCreateProperties().Movement.Switch(
                _ => SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.PathType), (byte)AreaTriggerPathType.Stationary),
                splineInfo => InitSplineOffsets(splineInfo),
                orbitInfo =>
                {
                    AreaTriggerOrbitInfo orbit = orbitInfo;
                    if (target != null && HasAreaTriggerFlag(AreaTriggerFieldFlags.Attached))
                        orbit.PathTarget = target.GetGUID();
                    else
                        orbit.Center = pos;

                    InitOrbit(orbit);
                });

            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.Facing), _stationaryPosition.GetOrientation());

            SetRollPitchYaw(GetCreateProperties().RollPitchYaw, GetCreateProperties().TargetRollPitchYaw);

            AI_Initialize();

            UpdateDynamicShapeFlag();

            // Calculate initial position and rotation
            {
                var (_, movedPos, movedRot) = CalculateWorldPositionAndRotation();
                Relocate(movedPos);
                _rotation = new(movedRot.X, movedRot.Y, movedRot.Z, movedRot.W);
            }

            UpdateShape();

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
                UpdatePositionAndRotation();

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
            SetOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideScaleCurve), overrideScale);
            UpdateDynamicShapeFlag();
        }

        void SetOverrideScaleCurve(Vector2[] points, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            SetOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideScaleCurve), points, startTimeOffset, interpolation);
            SetAreaTriggerFlag(AreaTriggerFieldFlags.DynamicShape);
        }

        void ClearOverrideScaleCurve()
        {
            ClearOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideScaleCurve));
            UpdateDynamicShapeFlag();
        }

        void SetExtraScaleCurve(float extraScale)
        {
            SetOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.ExtraScaleCurve), extraScale);
            UpdateDynamicShapeFlag();
        }

        void SetExtraScaleCurve(Vector2[] points, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            SetOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.ExtraScaleCurve), points, startTimeOffset, interpolation);
            SetAreaTriggerFlag(AreaTriggerFieldFlags.DynamicShape);
        }

        void ClearExtraScaleCurve()
        {
            ClearOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.ExtraScaleCurve));
            UpdateDynamicShapeFlag();
        }

        void SetOverrideMoveCurve(float x, float y, float z)
        {
            AreaTriggerFieldData areaTriggerData = m_values.ModifyValue(m_areaTriggerData);
            SetOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveX), x);
            SetOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveY), y);
            SetOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveZ), z);
            UpdateDynamicShapeFlag();
        }

        void SetOverrideMoveCurve(Vector2[] xCurvePoints, Vector2[] yCurvePoints, Vector2[] zCurvePoints, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);
            SetOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveX), xCurvePoints, startTimeOffset, interpolation);
            SetOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveY), yCurvePoints, startTimeOffset, interpolation);
            SetOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveZ), zCurvePoints, startTimeOffset, interpolation);
            SetAreaTriggerFlag(AreaTriggerFieldFlags.DynamicShape);
        }

        void ClearOverrideMoveCurve()
        {
            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);
            ClearOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveX));
            ClearOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveY));
            ClearOverrideCurve(areaTriggerData.ModifyValue(areaTriggerData.OverrideMoveCurveZ));
            UpdateDynamicShapeFlag();
        }

        void SetOverrideShapeCurve(float overrideShape)
        {
            SetOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideShapeCurve), overrideShape);
        }

        void SetOverrideShapeCurve(Vector2[] points, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            SetOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideShapeCurve), points, startTimeOffset, interpolation);
        }

        void ClearOverrideShapeCurve()
        {
            ClearOverrideCurve(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OverrideShapeCurve));
        }

        public void SetSpellVisual(SpellCastVisual visual)
        {
            SpellCastVisualField spellCastVisual = m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.SpellVisual);
            SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, visual.SpellXSpellVisualID);
            SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, visual.ScriptVisualID);
        }

        void SetRollPitchYaw(float roll, float pitch, float yaw, float? targetRoll, float? targetPitch, float? targetYaw)
        {
            Position rollPitchYaw = new Position(roll, pitch, yaw);
            Position targetRollPitchYaw = null;

            if (targetRoll.HasValue && targetPitch.HasValue && targetYaw.HasValue)
                targetRollPitchYaw = new Position(targetRoll.Value, targetPitch.Value, targetYaw.Value);

            SetRollPitchYaw(rollPitchYaw, targetRollPitchYaw);
        }

        void SetRollPitchYaw(Position rollPitchYaw, Position targetRollPitchYaw = null)
        {
            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);

            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.RollPitchYaw), rollPitchYaw);
            if (targetRollPitchYaw != null)
            {
                SetUpdateFieldValue(areaTriggerData.ModifyValue(areaTriggerData.TargetRollPitchYaw), targetRollPitchYaw);
                SetAreaTriggerFlag(AreaTriggerFieldFlags.DynamicShape);
            }
            else
            {
                RemoveOptionalUpdateFieldValue(areaTriggerData.ModifyValue(areaTriggerData.TargetRollPitchYaw));
                UpdateDynamicShapeFlag();
            }
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
                scale *= Math.Max(GetOverrideCurveValue(m_areaTriggerData.OverrideScaleCurve, m_areaTriggerData.TimeToTargetScale), 0.000001f);
            else if (m_areaTriggerData.ScaleCurveId != 0)
                scale *= Math.Max(Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.ScaleCurveId, GetScaleProgress()), 0.000001f);

            scale *= Math.Max(GetOverrideCurveValue(m_areaTriggerData.ExtraScaleCurve, m_areaTriggerData.TimeToTargetExtraScale), 0.000001f);

            return scale;
        }

        float GetScaleProgress() => GetOverrideCurveProgress(m_areaTriggerData.OverrideScaleCurve, m_areaTriggerData.TimeToTargetScale);
        float GetExtraScaleProgress() => GetOverrideCurveProgress(m_areaTriggerData.ExtraScaleCurve, m_areaTriggerData.TimeToTargetExtraScale);
        float GetOverridePosProgress() => GetOverrideCurveProgress(m_areaTriggerData.OverrideMoveCurveX, m_areaTriggerData.TimeToTargetPos);
        float GetShapeProgress() => GetOverrideCurveProgress(m_areaTriggerData.OverrideShapeCurve, m_areaTriggerData.TimeToTargetShape);

        float GetOverrideCurveProgress(OverrideCurve overrideCurve, uint timeTo)
        {
            if (timeTo == 0)
                return 0.0f;

            return Math.Clamp((float)(GetTimeSinceCreated() - overrideCurve.StartTimeOffset) / (float)timeTo, 0.0f, 1.0f);
        }

        float GetOverrideCurveValueAtProgress(OverrideCurve overrideCurve, float x)
        {
            Cypher.Assert(overrideCurve.OverrideActive, "OverrideCurve must be active to evaluate it");

            // unpack ParameterCurve
            if ((overrideCurve.ParameterCurve & 1u) != 0)
                return BitConverter.UInt32BitsToSingle((uint)(overrideCurve.ParameterCurve & ~1u));

            Vector2[] points = new Vector2[2];
            for (var i = 0; i < overrideCurve.Points.GetSize(); ++i)
                points[i] = new(overrideCurve.Points[i].X, overrideCurve.Points[i].Y);

            CurveInterpolationMode mode = (CurveInterpolationMode)(overrideCurve.ParameterCurve >> 1 & 0x7);
            int pointCount = (int)(overrideCurve.ParameterCurve >> 24 & 0xFF);

            return Global.DB2Mgr.GetCurveValueAt(mode, points.AsSpan(0, pointCount).ToArray(), x);
        }

        float GetOverrideCurveValue(OverrideCurve overrideCurve, uint timeTo)
        {
            return GetOverrideCurveValueAtProgress(overrideCurve, GetOverrideCurveProgress(overrideCurve, timeTo));
        }

        void SetOverrideCurve(OverrideCurve overrideCurve, float constantValue)
        {
            OverrideCurveData curveTemplate = new();
            curveTemplate.Curve = constantValue;
            SetOverrideCurve(overrideCurve, curveTemplate);
        }

        void SetOverrideCurve(OverrideCurve overrideCurve, Vector2[] points, uint? startTimeOffset, CurveInterpolationMode interpolation)
        {
            OverrideCurveData curveTemplate = new();
            curveTemplate.StartTimeOffset = startTimeOffset.GetValueOrDefault(GetTimeSinceCreated());
            curveTemplate.Mode = interpolation;
            curveTemplate.CurvePoints = points;

            SetOverrideCurve(overrideCurve, curveTemplate);
        }

        void ClearOverrideCurve(OverrideCurve overrideCurve)
        {
            SetOverrideCurve(overrideCurve, null);
        }

        void SetOverrideCurve(OverrideCurve overrideCurve, OverrideCurveData curve)
        {
            if (curve == null)
            {
                SetUpdateFieldValue(overrideCurve.ModifyValue(overrideCurve.OverrideActive), false);
                return;
            }

            SetUpdateFieldValue(overrideCurve.ModifyValue(overrideCurve.OverrideActive), true);
            SetUpdateFieldValue(overrideCurve.ModifyValue(overrideCurve.StartTimeOffset), curve.StartTimeOffset);

            Position point = new Position();
            // ParameterCurve packing information
            // (not_using_points & 1) | ((interpolation_mode & 0x7) << 1) | ((first_point_offset & 0xFFFFF) << 4) | ((point_count & 0xFF) << 24)
            //   if not_using_points is set then the entire field is simply read as a float (ignoring that lowest bit)
            float simpleFloat = curve.Curve;
            if (simpleFloat != 0)
            {
                uint packedCurve = BitConverter.SingleToUInt32Bits(simpleFloat);
                packedCurve |= 1;

                SetUpdateFieldValue(overrideCurve.ModifyValue(overrideCurve.ParameterCurve), packedCurve);

                // clear points
                for (var i = 0; i < overrideCurve.Points.GetSize(); ++i)
                    SetUpdateFieldValue(ref overrideCurve.ModifyValue(overrideCurve.Points, i), point);
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
                    SetUpdateFieldValue(overrideCurve.ModifyValue(overrideCurve.ParameterCurve), packedCurve);

                    for (var i = 0; i < curvePoints.Length; ++i)
                    {
                        point.Relocate(curvePoints[i].X, curvePoints[i].Y);
                        SetUpdateFieldValue(ref overrideCurve.ModifyValue(overrideCurve.Points, i), point);
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
            float progress = GetShapeProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float radius = MathFunctions.Lerp(sphere.Radius, sphere.RadiusTarget, progress) * scale;

            SearchUnits(targetList, radius, true);

            targetList.RemoveAll(unit => !unit.IsWithinDist(this, radius));
        }

        void SearchUnitInBox(AreaTriggerBox box, List<Unit> targetList)
        {
            float progress = GetShapeProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            unsafe
            {
                float scale = CalcCurrentScale();
                float extentsX = MathFunctions.Lerp(box.Extents.GetValue().X, box.ExtentsTarget.GetValue().X, progress) * scale;
                float extentsY = MathFunctions.Lerp(box.Extents.GetValue().Y, box.ExtentsTarget.GetValue().Y, progress) * scale;
                float extentsZ = MathFunctions.Lerp(box.Extents.GetValue().Z, box.ExtentsTarget.GetValue().Z, progress) * scale;
                float radius = MathF.Sqrt(extentsX * extentsX + extentsY * extentsY + extentsZ * extentsZ);

                SearchUnits(targetList, radius, false);

                if (targetList.Empty())
                    return;

                Box rotatedBox = new CoordinateFrame(new Quaternion(_rotation.X, _rotation.Y, _rotation.Z, _rotation.W).ToRotationMatrix(), GetPosition())
                    .ToWorldSpace(new AxisAlignedBox(new Vector3(-extentsX, -extentsY, -extentsZ / 2), new Vector3(extentsX, extentsY, extentsZ / 2)));

                targetList.RemoveAll(unit => !rotatedBox.Contains(unit));

# if DEBUG
                // DebugVisualizeShape
                Player caster = GetCaster()?.ToPlayer();
                if (caster != null && caster.IsDebugAreaTriggers)
                    for (int corner = 0; corner < 8; ++corner)
                        caster.SummonCreature(1, rotatedBox.Corner(corner), TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(50));
#endif
            }
        }

        void SearchUnitInPolygon(AreaTriggerPolygon polygon, List<Unit> targetList)
        {
            float progress = GetShapeProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            float height = MathFunctions.Lerp(polygon.Height, polygon.HeightTarget, progress);
            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            SearchUnits(targetList, GetMaxSearchRadius(), false);

            if (targetList.Empty())
                return;

            targetList.RemoveAll(unit => unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ || !unit.IsInPolygon2D(this, _polygonVertices));

#if DEBUG
            // DebugVisualizeShape
            Player caster = GetCaster()?.ToPlayer();
            if (caster != null && caster.IsDebugAreaTriggers)
            {
                foreach (Position vertex in _polygonVertices)
                {
                    Vector3 pos = (Vector3)GetPosition() + vertex;
                    caster.SummonCreature(1, pos + new Vector3(0, 0, -height), TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(50));
                    caster.SummonCreature(1, pos + new Vector3(0, 0, +height), TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(50));
                }
            }
#endif
        }

        void SearchUnitInCylinder(AreaTriggerCylinder cylinder, List<Unit> targetList)
        {
            float progress = GetShapeProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float radius = MathFunctions.Lerp(cylinder.Radius, cylinder.RadiusTarget, progress) * scale;
            float height = MathFunctions.Lerp(cylinder.Height, cylinder.HeightTarget, progress);
            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.HeightIgnoresScale))
                height *= scale;

            float zOffset = MathFunctions.Lerp(cylinder.LocationZOffset, cylinder.LocationZOffsetTarget, progress) * scale;

            SearchUnits(targetList, MathF.Sqrt(radius * radius + height * height), false);

            if (targetList.Empty())
                return;

            Cylinder rotatedCylinder = new CoordinateFrame(new Quaternion(_rotation.X, _rotation.Y, _rotation.Z, _rotation.W).ToRotationMatrix(), GetPosition())
                .ToWorldSpace(new Cylinder(new Vector3(0.0f, 0.0f, 0.0f + zOffset), new Vector3(0.0f, 0.0f, 0.0f + zOffset + height), radius));

            targetList.RemoveAll(unit => !rotatedCylinder.Contains(unit));

#if DEBUG
            // DebugVisualizeShape
            Player caster = GetCaster()?.ToPlayer();
            if (caster != null && caster.IsDebugAreaTriggers)
            {
                for (int end = 0; end < 2; ++end)
                    caster.SummonCreature(1, rotatedCylinder.GetPoint(end), TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(50));
            }
#endif
        }

        void SearchUnitInDisk(AreaTriggerDisk disk, List<Unit> targetList)
        {
            float progress = GetShapeProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            float scale = CalcCurrentScale();
            float innerRadius = MathFunctions.Lerp(disk.InnerRadius, disk.InnerRadiusTarget, progress) * scale;
            float outerRadius = MathFunctions.Lerp(disk.OuterRadius, disk.OuterRadiusTarget, progress) * scale;
            float height = MathFunctions.Lerp(disk.Height, disk.HeightTarget, progress);
            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.HeightIgnoresScale))
                height *= scale;

            float zOffset = MathFunctions.Lerp(disk.LocationZOffset, disk.LocationZOffsetTarget, progress) * scale;

            SearchUnits(targetList, MathF.Sqrt(outerRadius * outerRadius + height * height), false);

            if (targetList.Empty())
                return;

            Cylinder innerCylinder = new CoordinateFrame(new Quaternion(_rotation.X, _rotation.Y, _rotation.Z, _rotation.W).ToRotationMatrix(), GetPosition())
                .ToWorldSpace(new Cylinder(new Vector3(0.0f, 0.0f, 0.0f + zOffset), new Vector3(0.0f, 0.0f, 0.0f + zOffset + height), innerRadius));

            Cylinder outerCylinder = new CoordinateFrame(new Quaternion(_rotation.X, _rotation.Y, _rotation.Z, _rotation.W).ToRotationMatrix(), GetPosition())
                .ToWorldSpace(new Cylinder(new Vector3(0.0f, 0.0f, 0.0f + zOffset), new Vector3(0.0f, 0.0f, 0.0f + zOffset + height), outerRadius));

            targetList.RemoveAll(unit =>
            {
                Vector3 position = unit;
                return innerCylinder.Contains(position) || !outerCylinder.Contains(position);
            });

#if DEBUG
            // DebugVisualizeShape
            Player caster = GetCaster()?.ToPlayer();
            if (caster != null && caster.IsDebugAreaTriggers)
            {
                for (int end = 0; end < 2; ++end)
                    caster.SummonCreature(1, innerCylinder.GetPoint(end), TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(50));
            }
#endif
        }

        void SearchUnitInBoundedPlane(AreaTriggerBoundedPlane boundedPlane, List<Unit> targetList)
        {
            float progress = GetShapeProgress();
            if (m_areaTriggerData.MorphCurveId != 0)
                progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

            unsafe
            {
                float scale = CalcCurrentScale();
                float extentsY = MathFunctions.Lerp(boundedPlane.ExtentsY, boundedPlane.ExtentsTargetY, progress) * scale;
                float extentsZ = MathFunctions.Lerp(boundedPlane.ExtentsZ, boundedPlane.ExtentsTargetZ, progress) * scale;
                float radius = MathF.Sqrt(extentsY * extentsY + extentsZ * extentsZ);

                SearchUnits(targetList, radius, false);

                if (targetList.Empty())
                    return;

                CoordinateFrame coordinateFrame = new CoordinateFrame(new Quaternion(_rotation.X, _rotation.Y, _rotation.Z, _rotation.W).ToRotationMatrix(), GetPosition());

                Vector3[] corners =
                {
                    coordinateFrame.PointToWorldSpace(new Vector3(0.0f, -extentsY, -extentsZ)),
                    coordinateFrame.PointToWorldSpace(new Vector3(0.0f,  extentsY, -extentsZ)),
                    coordinateFrame.PointToWorldSpace(new Vector3(0.0f, -extentsY,  extentsZ)),
                    coordinateFrame.PointToWorldSpace(new Vector3(0.0f,  extentsY,  extentsZ))
                };

                targetList.RemoveAll(unit =>
                {
                    Vector3 closestPoint = CollisionDetection.closestPointToRectangle(corners[0], corners[1], corners[2], corners[3], unit);
                    return !unit.IsInDist(closestPoint.X, closestPoint.Y, closestPoint.Z, unit.GetCombatReach());
                });

#if DEBUG
                // DebugVisualizeShape
                Player caster = GetCaster()?.ToPlayer();
                if (caster != null && caster.IsDebugAreaTriggers)
                {
                    foreach (Vector3 corner in corners)
                        caster.SummonCreature(1, corner, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(50));
                }
#endif
            }
        }

        void HandleUnitEnterExit(List<Unit> newTargetList, AreaTriggerExitReason exitMode = AreaTriggerExitReason.NotInside)
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
                    HandleUnitExitInternal(leavingUnit, exitMode);
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

            // OnUnitEnter script can despawn this areatrigger or teleport player to a different map
            if (!IsInWorld || !IsInMap(unit))
                return;

            // Register areatrigger in Unit after actions/scripts to allow them to determine
            // if the unit is in one or more areatriggers with the same id
            // without forcing every script to have additional logic excluding this areatrigger

            unit.EnterAreaTrigger(this);
        }

        void HandleUnitExitInternal(Unit unit, AreaTriggerExitReason exitMode = AreaTriggerExitReason.NotInside)
        {
            bool canTriggerOnExit = exitMode != AreaTriggerExitReason.ByExpire || !HasActionSetFlag(AreaTriggerActionSetFlag.DontRunOnLeaveWhenExpiring);

            Player player = unit.ToPlayer();
            if (player != null)
            {
                if (player.IsDebugAreaTriggers)
                    player.SendSysMessage(CypherStrings.DebugAreatriggerEntityLeft, GetEntry(), IsCustom(), IsStaticSpawn(), _spawnId);

                if (canTriggerOnExit)
                {
                    player.UpdateQuestObjectiveProgress(QuestObjectiveType.AreaTriggerExit, (int)GetEntry(), 1);

                    if (GetTemplate().ActionSetId != 0)
                        player.UpdateCriteria(CriteriaType.LeaveAreaTriggerWithActionSet, GetTemplate().ActionSetId);
                }
            }

            UndoActions(unit);

            // OnUnitExit script can teleport player to another map, causing it to attempt to exit the areatrigger again (from Unit::ExitAllAreaTriggers)
            unit.ExitAreaTrigger(this);

            if (canTriggerOnExit)
                _ai.OnUnitExit(unit, exitMode);
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
                    SetUpdateFieldValue(boundedPlane.ModifyValue(boundedPlane.ExtentsY), boundedPlaneInfo.ExtentsY);
                    SetUpdateFieldValue(boundedPlane.ModifyValue(boundedPlane.ExtentsZ), boundedPlaneInfo.ExtentsZ);
                    SetUpdateFieldValue(boundedPlane.ModifyValue(boundedPlane.ExtentsTargetY), boundedPlaneInfo.ExtentsTargetY);
                    SetUpdateFieldValue(boundedPlane.ModifyValue(boundedPlane.ExtentsTargetZ), boundedPlaneInfo.ExtentsTargetZ);
                }
            );

            if (IsInWorld)
                UpdateDynamicShapeFlag();
        }

        public float GetMaxSearchRadius()
        {
            return m_areaTriggerData.BoundsRadius2D * CalcCurrentScale();
        }

        void UpdatePolygonVertices()
        {
            AreaTriggerPolygon shape = m_areaTriggerData.ShapeData.Get<AreaTriggerPolygon>();

            // No need to recalculate, orientation didn't change
            if (_verticesUpdatePreviousRotation == _rotation && shape.VerticesTarget.Empty())
                return;

            _polygonVertices.AddRange(shape.Vertices._values.Select(p => new Position(p.X, p.Y)));

            if (!shape.Vertices.Empty())
            {
                float progress = GetShapeProgress();
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

            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(new Quaternion(_rotation.X, _rotation.Y, _rotation.Z, _rotation.W));

            // This is needed to rotate the vertices, following orientation
            foreach (Position vertice in _polygonVertices)
            {
                Vector3 transformed = rotationMatrix.Multiply(vertice);
                vertice.Relocate(transformed.X, transformed.Y);
            }

            _verticesUpdatePreviousRotation = _rotation;
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

        void UpdatePositionAndRotation()
        {
            var (movementUpdateResult, newPosition, newRotation) = CalculateWorldPositionAndRotation();

            if (HasAreaTriggerFlag(AreaTriggerFieldFlags.Attached))
            {
                Unit target = GetTarget();
                if (target != null)
                    RelocateStationaryPosition(target.GetPosition());
            }

            Position oldPosition = GetPosition();
            Quaternion oldRotation = _rotation;
            _rotation = new Quaternion(newRotation.X, newRotation.Y, newRotation.Z, newRotation.W);

            if (newPosition != oldPosition)
            {
                GetMap().AreaTriggerRelocation(this, newPosition.GetPositionX(), newPosition.GetPositionY(), newPosition.GetPositionZ(), newPosition.GetOrientation());
            }
            else
            {
                SetOrientation(newPosition.GetOrientation());
                if (oldRotation != _rotation)
                    UpdateShape();
            }

#if DEBUG
            if (movementUpdateResult != MovementUpdateResult.None)
                DebugVisualizePosition();
#endif

            if (movementUpdateResult == MovementUpdateResult.Arrived)
            {
                if (!_reachedDestination)
                    _ai.OnDestinationReached();

                _reachedDestination = true;
            }
        }

        MovementUpdateWorldResult CalculateWorldPositionAndRotation()
        {
            var (status, localPosition, localRotation) = CalculateLocalPositionAndRotation();
            MovementUpdateWorldResult worldResult = new()
            {
                Status = status,
                Position = localPosition,
                Rotation = Quaternion.CreateFromYawPitchRoll(localRotation.Z, localRotation.Y, localRotation.X)
            };

            if (HasAreaTriggerFlag(AreaTriggerFieldFlags.Attached))
            {
                Unit target = GetTarget();
                if (target != null)
                {
                    worldResult.Position = target.GetPositionWithOffset(worldResult.Position);

                    if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
                        worldResult.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, target.GetOrientation()) * worldResult.Rotation;

                    if (worldResult.Status == MovementUpdateResult.None)
                        worldResult.Status = MovementUpdateResult.Moved;
                }
            }
            else
            {
                ITransport transport = GetTransport();
                if (transport != null)
                {
                    worldResult.Position = transport.GetPositionWithOffset(worldResult.Position);

                    if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
                        worldResult.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, transport.GetTransportOrientation()) * worldResult.Rotation;
                }
                else
                    worldResult.Position = GetMovementOrigin().GetPositionWithOffset(worldResult.Position);
            }

            worldResult.Rotation.toEulerAnglesZYX(out localRotation.Z, out localRotation.Y, out localRotation.X);
            worldResult.Position.SetOrientation(localRotation.Z);

            return worldResult;
        }

        MovementUpdateLocalResult CalculateLocalPositionAndRotation()
        {
            MovementUpdateLocalResult result = m_areaTriggerData.PathData.Visit(shape =>
            {
                if (shape is AreaTriggerSplineCalculator)
                    return CalculateLocalSplinePositionAndRotation();
                else if (shape is AreaTriggerOrbit orbit)
                    return CalculateLocalOrbitPositionAndRotation(orbit);
                else if (shape is AreaTriggerMovementScript)
                {
                    Cypher.Assert(false, "AreaTriggerMovementScript is not implemented");
                    return null;
                }
                else
                    return CalculateLocalStationaryPositionAndRotation();
            });

            if (m_areaTriggerData.TargetRollPitchYaw.HasValue())
            {
                float progress = GetShapeProgress();
                if (m_areaTriggerData.MorphCurveId != 0)
                    progress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MorphCurveId, progress);

                result.Rotation += Vector3.Lerp(m_areaTriggerData.TargetRollPitchYaw.GetValue(), m_areaTriggerData.RollPitchYaw.GetValue(), progress);
            }
            else
                result.Rotation += m_areaTriggerData.RollPitchYaw.GetValue();

            if (HasOverridePosition())
            {
                result.Status = MovementUpdateResult.Moved;
                float progress = GetOverridePosProgress();
                result.Position.X = GetOverrideCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveX, progress);
                result.Position.Y = GetOverrideCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveY, progress);
                result.Position.Z = GetOverrideCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveZ, progress);

                if (m_movementInfo.transport.guid.IsEmpty())
                    result.Position = GetMovementOrigin().GetPositionOffsetTo(result.Position);
            }

            return result;
        }

        void InitSplineOffsets(List<Vector3> offsets, float? overrideSpeed = null, bool? speedIsTimeInSeconds = null)
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

        public void InitSplines(Span<Vector3> splinePoints, float? overrideSpeed = null, bool? speedIsTimeInSeconds = null)
        {
            if (splinePoints.Length < 2)
                return;

            _spline = new Spline<float>();
            _reachedDestination = false;

            List<Vector3> offsets = [];
            for (int i = 0; i < splinePoints.Length; ++i)
                offsets.Add(GetStationaryPosition().GetPositionOffsetTo(splinePoints[i]));

            _spline.InitSpline(offsets.ToArray(), offsets.Count, offsets.Count > 2 ? EvaluationMode.Catmullrom : EvaluationMode.Linear, _stationaryPosition.GetOrientation());
            _spline.InitLengths();

            float speed = overrideSpeed.GetValueOrDefault(GetCreateProperties().Speed);
            if (speed <= 0.0f)
                speed = 1.0f;

            uint timeToTarget = (speedIsTimeInSeconds.GetValueOrDefault(GetCreateProperties().SpeedIsTime) ? speed : _spline.Length() / speed) * Time.InMilliseconds;

            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTarget), timeToTarget);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.MovementStartTime), GameTime.GetGameTimeMS());

            SetAreaTriggerFlag(AreaTriggerFieldFlags.DynamicShape);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.PathType), (int)AreaTriggerPathType.Spline);
            var pathData = areaTriggerData.ModifyValue<AreaTriggerSplineCalculator>(m_areaTriggerData.PathData);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.Linear), _spline.m_mode == EvaluationMode.Linear);
            var points = pathData.ModifyValue(pathData.Points);

            ClearDynamicUpdateFieldValues(points);
            if (m_movementInfo.transport.guid.IsEmpty())
            {
                foreach (Vector3 point in _spline.GetPoints())
                    AddDynamicUpdateFieldValue(points, GetStationaryPosition().GetPositionWithOffset(point));
            }
            else
            {
                foreach (Vector3 point in _spline.GetPoints())
                {
                    AddDynamicUpdateFieldValue(points, point);
                }
            }
        }

        uint GetElapsedTimeForMovement()
        {
            uint now = GameTime.GetGameTimeMS();
            if (now >= m_areaTriggerData.MovementStartTime)
                return now - m_areaTriggerData.MovementStartTime;
            return 0;
        }

        void InitOrbit(AreaTriggerOrbitInfo orbit, float? overrideSpeed = null, bool? speedIsTimeInSeconds = null)
        {
            // Circular movement requires either a center position or an attached unit
            Cypher.Assert(orbit.Center.HasValue || orbit.PathTarget.HasValue);

            float speed = overrideSpeed.GetValueOrDefault(GetCreateProperties().Speed);
            if (speed <= 0.0f)
                speed = 1.0f;

            uint timeToTarget = (uint)(speedIsTimeInSeconds.GetValueOrDefault(GetCreateProperties().SpeedIsTime) ? speed : (uint)(orbit.Radius * 2.0f * MathF.PI / speed)) * Time.InMilliseconds;

            var areaTriggerData = m_values.ModifyValue(m_areaTriggerData);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.TimeToTarget), timeToTarget);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.MovementStartTime), GameTime.GetGameTimeMS());
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.OrbitPathTarget), orbit.PathTarget.GetValueOrDefault(ObjectGuid.Empty));
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.ZOffset), orbit.ZOffset);
            if (orbit.CanLoop)
                SetAreaTriggerFlag(AreaTriggerFieldFlags.CanLoop);
            else
                RemoveAreaTriggerFlag(AreaTriggerFieldFlags.CanLoop);

            SetAreaTriggerFlag(AreaTriggerFieldFlags.DynamicShape);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(m_areaTriggerData.PathType), (int)AreaTriggerPathType.Orbit);
            var pathData = areaTriggerData.ModifyValue<AreaTriggerOrbit>(m_areaTriggerData.PathData);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.CounterClockwise), orbit.CounterClockwise);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.Radius), orbit.Radius);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.InitialAngle), orbit.InitialAngle);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.BlendFromRadius), orbit.BlendFromRadius);
            SetUpdateFieldValue(pathData.ModifyValue(pathData.ExtraTimeForBlending), orbit.ExtraTimeForBlending);

            Vector3 center = orbit.Center.GetValueOrDefault(new Position());
            if (orbit.Center.HasValue)
            {
                WorldObject attachedTo = Global.ObjAccessor.GetWorldObject(this, m_movementInfo.transport.guid);
                if (attachedTo != null)
                    center = attachedTo.GetPositionOffsetTo(orbit.Center.Value);
            }

            SetUpdateFieldValue(pathData.ModifyValue(pathData.Center), center);
        }

        MovementUpdateLocalResult CalculateLocalSplinePositionAndRotation()
        {
            float currentTimePercent = Math.Clamp(GetElapsedTimeForMovement() / GetTimeToTarget(), 0.0f, 1.0f);
            bool reachedDestination = currentTimePercent >= 1.0f;

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

            _spline.Evaluate_Percent(lastPositionIndex, percentFromLastPoint, out Vector3 currentPosition);

            float orientation = 0.0f;
            if (m_areaTriggerData.FacingCurveId != 0)
                orientation += Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetShapeProgress());

            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
            {
                _spline.Evaluate_Derivative(lastPositionIndex, percentFromLastPoint, out Vector3 derivative);
                if (derivative.X != 0.0f || derivative.Y != 0.0f)
                    orientation += MathF.Atan2(derivative.Y, derivative.X);
            }

            return new MovementUpdateLocalResult()
            {
                Status = reachedDestination ? MovementUpdateResult.Arrived : MovementUpdateResult.Moved,
                Position = currentPosition,
                Rotation = new Vector3(0.0f, 0.0f, orientation)
            };
        }

        MovementUpdateLocalResult CalculateLocalOrbitPositionAndRotation(AreaTriggerOrbit orbit)
        {
            uint movementTime = GetElapsedTimeForMovement();
            uint timeToTarget = (uint)(GetTimeToTarget() + orbit.ExtraTimeForBlending);
            bool firstLoop = true;
            if (HasAreaTriggerFlag(AreaTriggerFieldFlags.CanLoop) && timeToTarget != 0)
            {
                // remove ExtraTimeForBlending if not on first loop
                if (movementTime > timeToTarget)
                {
                    timeToTarget -= (uint)(int)orbit.ExtraTimeForBlending;
                    movementTime = (uint)((movementTime - orbit.ExtraTimeForBlending) % timeToTarget);
                    firstLoop = false;
                }
            }

            float pathProgress = (float)movementTime / (float)timeToTarget;
            if (m_areaTriggerData.MoveCurveId != 0)
                pathProgress = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.MoveCurveId, pathProgress);

            // We already made one circle and can't loop
            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.CanLoop))
                pathProgress = Math.Min(1.0f, pathProgress);

            float radius = orbit.Radius;
            if (firstLoop && MathFunctions.fuzzyNe(orbit.BlendFromRadius, radius))
            {
                float blendCurve = Math.Clamp((orbit.BlendFromRadius - radius) / radius, 1.0f, 4.0f);
                float blendProgress = Math.Clamp(Math.Abs(4.0f * pathProgress / blendCurve), 0.0f, 1.0f);
                radius = MathFunctions.Lerp(orbit.BlendFromRadius, radius, blendProgress);
            }

            float angle = 2.0f * MathF.PI * pathProgress;

            // Adapt angle depending of circle direction
            if (!orbit.CounterClockwise)
                angle *= -1;

            angle += orbit.InitialAngle;

            Vector3 position = new(radius * MathF.Cos(angle), radius * MathF.Sin(angle), m_areaTriggerData.ZOffset);

            float orientation = 0.0f;
            if (m_areaTriggerData.FacingCurveId != 0)
                orientation = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetShapeProgress());

            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
            {
                orientation += angle;
                orientation += orbit.CounterClockwise ? MathFunctions.PiOver4 : -MathFunctions.PiOver4;
            }

            return new MovementUpdateLocalResult()
            {
                Status = MovementUpdateResult.Moved,
                Position = position,
                Rotation = new Vector3(0.0f, 0.0f, orientation)
            };
        }

        MovementUpdateLocalResult CalculateLocalStationaryPositionAndRotation()
        {
            float orientation = 0.0f;
            if (m_areaTriggerData.FacingCurveId != 0)
                orientation = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetShapeProgress());

            if (!HasAreaTriggerFlag(AreaTriggerFieldFlags.AbsoluteOrientation))
                orientation += m_areaTriggerData.Facing;

            return new MovementUpdateLocalResult()
            {
                Status = MovementUpdateResult.None,
                Position = Vector3.Zero,
                Rotation = new Vector3(0.0f, 0.0f, orientation)
            };
        }

        Position GetMovementOrigin()
        {
            return m_areaTriggerData.PathData.Visit(shape =>
            {
                if (shape is AreaTriggerSplineCalculator)
                    return GetStationaryPosition();
                else if (shape is AreaTriggerOrbit orbit)
                {
                    if (!m_areaTriggerData.OrbitPathTarget.GetValue().IsEmpty())
                    {
                        WorldObject pathTarget = Global.ObjAccessor.GetWorldObject(this, m_areaTriggerData.OrbitPathTarget);
                        if (pathTarget != null)
                            return pathTarget;
                        return GetStationaryPosition();
                    }
                    return orbit.Center.GetValue();
                }
                else if (shape is AreaTriggerMovementScript script)
                    return script.Center.GetValue();
                else
                    return GetStationaryPosition();
            });
        }

        void UpdateOverridePosition()
        {
            float progress = GetOverrideCurveProgress(m_areaTriggerData.OverrideMoveCurveX, m_areaTriggerData.TimeToTargetPos);

            float x = GetOverrideCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveX, progress);
            float y = GetOverrideCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveY, progress);
            float z = GetOverrideCurveValueAtProgress(m_areaTriggerData.OverrideMoveCurveZ, progress);
            float orientation = GetOrientation();

            if (m_areaTriggerData.FacingCurveId != 0)
            {
                orientation = Global.DB2Mgr.GetCurveValueAt(m_areaTriggerData.FacingCurveId, GetOverrideCurveProgress(m_areaTriggerData.OverrideShapeCurve, m_areaTriggerData.TimeToTargetShape));
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

        public override void BuildValuesCreate(UpdateFieldFlag flags, WorldPacket data, Player target)
        {
            m_objectData.WriteCreate(flags, data, target, this);
            m_areaTriggerData.WriteCreate(flags, data, target, this);
        }

        public override void BuildValuesUpdate(UpdateFieldFlag flags, WorldPacket data, Player target)
        {
            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(flags, data, target, this);

            if (m_values.HasChanged(TypeId.AreaTrigger))
                m_areaTriggerData.WriteUpdate(flags, data, target, this);
        }

        public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedAreaTriggerMask, Player target, bool ignoreNestedChangesMask)
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
                m_objectData.WriteUpdate(requestedObjectMask, buffer, target, this, ignoreNestedChangesMask);

            if (valuesMask[(int)TypeId.AreaTrigger])
                m_areaTriggerData.WriteUpdate(requestedAreaTriggerMask, buffer, target, this, ignoreNestedChangesMask);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearValuesChangesMask()
        {
            m_values.ClearChangesMask(m_areaTriggerData);
            base.ClearValuesChangesMask();
        }

        public T GetAI<T>() where T : AreaTriggerAI { return (T)_ai; }

        public bool IsCustom() { return _areaTriggerTemplate.Id.IsCustom; }
        public bool IsServerSide() { return _areaTriggerTemplate.Flags.HasFlag(AreaTriggerFlag.IsServerSide); }
        public bool IsStaticSpawn() { return _spawnId != 0; }
        public bool HasActionSetFlag(AreaTriggerActionSetFlag flag) { return _areaTriggerTemplate.ActionSetFlags.HasFlag(flag); }

        void UpdateDynamicShapeFlag()
        {
            if (m_areaTriggerData.PathType != (int)AreaTriggerPathType.Stationary
                || HasAreaTriggerFlag(AreaTriggerFieldFlags.Attached)
                || (m_areaTriggerData.OverrideScaleCurve.GetValue().OverrideActive && (m_areaTriggerData.OverrideScaleCurve.GetValue().ParameterCurve & 1) == 0)
                || m_areaTriggerData.ScaleCurveId != 0
                || (m_areaTriggerData.ExtraScaleCurve.GetValue().OverrideActive && (m_areaTriggerData.ExtraScaleCurve.GetValue().ParameterCurve & 1) == 0)
                || (HasOverridePosition()
                    && ((m_areaTriggerData.OverrideMoveCurveX.GetValue().ParameterCurve & 1) == 0
                        || (m_areaTriggerData.OverrideMoveCurveY.GetValue().ParameterCurve & 1) == 0
                        || (m_areaTriggerData.OverrideMoveCurveZ.GetValue().ParameterCurve & 1) == 0))
                || (m_areaTriggerData.TargetRollPitchYaw.HasValue() && m_areaTriggerData.RollPitchYaw.GetValue() != m_areaTriggerData.TargetRollPitchYaw.GetValue())
                || GetCreateProperties().Shape.IsDynamic())
                SetAreaTriggerFlag(AreaTriggerFieldFlags.DynamicShape);
            else
                RemoveAreaTriggerFlag(AreaTriggerFieldFlags.DynamicShape);
        }

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
                        player.SummonCreature(1, this, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(250));
            }
        }

        public override Position GetStationaryPosition() { return _stationaryPosition; }
        public void RelocateStationaryPosition(Position pos) { _stationaryPosition.Relocate(pos); }

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

        public uint GetTimeToTargetShape() { return m_areaTriggerData.TimeToTargetShape; }
        public void SetTimeToTargetShape(uint timeToTargetShape) { SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.TimeToTargetShape), timeToTargetShape); }

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

        public void SetPathTarget(ObjectGuid pathTarget) { SetUpdateFieldValue(m_values.ModifyValue(m_areaTriggerData).ModifyValue(m_areaTriggerData.OrbitPathTarget), pathTarget); }

        public AreaTriggerFieldData m_areaTriggerData;

        ulong _spawnId;

        ObjectGuid _targetGuid;

        AuraEffect _aurEff;

        Position _stationaryPosition;
        Quaternion _rotation;
        int _duration;
        int _totalDuration;
        Quaternion _verticesUpdatePreviousRotation;
        bool _isRemoved;

        List<Position> _polygonVertices = new();
        Spline<float> _spline;

        bool _reachedDestination;

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
            bool IgnoreNestedChangesMask;

            public ValuesUpdateForPlayerWithMaskSender(AreaTrigger owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetChangesMask(), AreaTriggerMask.GetChangesMask(), player, IgnoreNestedChangesMask);

                udata.BuildPacket(out UpdateObject updateObject);
                player.SendPacket(updateObject);
            }

            public static implicit operator IDoWork<Player>(ValuesUpdateForPlayerWithMaskSender obj) => obj.Invoke;
        }

        class OverrideCurveData
        {
            public uint StartTimeOffset;
            public CurveInterpolationMode Mode;

            public Vector2[] CurvePoints;
            public float Curve;
        }

        public enum MovementUpdateResult
        {
            None,
            Moved,
            Arrived
        }

        struct MovementUpdateWorldResult
        {
            public MovementUpdateResult Status;
            public Position Position;
            public Quaternion Rotation;

            public void Deconstruct(out MovementUpdateResult status, out Position position, out Quaternion rotation)
            {
                status = this.Status;
                position = this.Position;
                rotation = this.Rotation;
            }
        }

        struct MovementUpdateLocalResult
        {
            public MovementUpdateResult Status;
            public Vector3 Position;
            public Vector3 Rotation;

            public void Deconstruct(out MovementUpdateResult status, out Vector3 position, out Vector3 rotation)
            {
                status = this.Status;
                position = this.Position;
                rotation = this.Rotation;
            }
        }
    }
}
