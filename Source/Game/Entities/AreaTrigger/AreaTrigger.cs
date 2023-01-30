// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Framework.Constants;
using Game.AI;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Movement;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IAreaTriggerEntity;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Entities
{
    public class AreaTrigger : WorldObject
    {
        private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            private readonly AreaTriggerFieldData AreaTriggerMask = new();
            private readonly ObjectFieldData ObjectMask = new();
            private readonly AreaTrigger Owner;

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

        private readonly AreaTriggerFieldData _areaTriggerData;
        private readonly List<ObjectGuid> _insideUnits = new();
        private readonly Spline<int> _spline;
        private AreaTriggerAI _ai;

        private AreaTriggerCreateProperties _areaTriggerCreateProperties;
        private AreaTriggerTemplate _areaTriggerTemplate;

        private AuraEffect _aurEff;
        private int _duration;
        private bool _isRemoved;
        private int _lastSplineIndex;
        private float _maxSearchRadius;
        private uint _movementTime;

        private AreaTriggerOrbitInfo _orbitInfo;
        private List<Vector2> _polygonVertices;
        private float _previousCheckOrientation;

        private bool _reachedDestination;

        private Vector3 _rollPitchYaw;

        private AreaTriggerShapeInfo _shape;

        private ulong _spawnId;

        private ObjectGuid _targetGuid;
        private Vector3 _targetRollPitchYaw;
        private uint _timeSinceCreated;
        private int _totalDuration;

        public AreaTrigger() : base(false)
        {
            _previousCheckOrientation = float.PositiveInfinity;
            _reachedDestination = true;

            ObjectTypeMask |= TypeMask.AreaTrigger;
            ObjectTypeId = TypeId.AreaTrigger;

            UpdateFlag.Stationary = true;
            UpdateFlag.AreaTrigger = true;

            _areaTriggerData = new AreaTriggerFieldData();

            _spline = new Spline<int>();
        }

        public override void AddToWorld()
        {
            // Register the AreaTrigger for Guid lookup and for caster
            if (!IsInWorld)
            {
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
                _isRemoved = true;

                Unit caster = GetCaster();

                if (caster)
                    caster._UnregisterAreaTrigger(this);

                // Handle removal of all units, calling OnUnitExit & deleting Auras if needed
                HandleUnitEnterExit(new List<Unit>());

                _ai.OnRemove();

                base.RemoveFromWorld();

                if (_spawnId != 0)
                    GetMap().GetAreaTriggerBySpawnIdStore().Remove(_spawnId, this);

                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public static AreaTrigger CreateAreaTrigger(uint areaTriggerCreatePropertiesId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, SpellCastVisualField spellVisual, ObjectGuid castId = default, AuraEffect aurEff = null)
        {
            AreaTrigger at = new();

            if (!at.Create(areaTriggerCreatePropertiesId, caster, target, spell, pos, duration, spellVisual, castId, aurEff))
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

            AreaTriggerSpawn position = Global.AreaTriggerDataStorage.GetAreaTriggerSpawn(spawnId);

            if (position == null)
                return false;

            AreaTriggerTemplate areaTriggerTemplate = Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(position.TriggerId);

            if (areaTriggerTemplate == null)
                return false;

            return CreateServer(map, areaTriggerTemplate, position);
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
                else if (GetTemplate() != null &&
                         GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
                {
                    Unit target = GetTarget();

                    if (target)
                        GetMap().AreaTriggerRelocation(this, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), target.GetOrientation());
                }
                else
                {
                    UpdateSplinePosition(diff);
                }

                if (GetDuration() != -1)
                {
                    if (GetDuration() > diff)
                    {
                        _UpdateDuration((int)(_duration - diff));
                    }
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
            SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.Duration), (uint)Math.Max(newDuration, 0));
        }

        public AreaTriggerTemplate GetTemplate()
        {
            return _areaTriggerTemplate;
        }

        public uint GetScriptId()
        {
            if (_spawnId != 0)
                return Global.AreaTriggerDataStorage.GetAreaTriggerSpawn(_spawnId).ScriptId;

            if (GetCreateProperties() != null)
                return GetCreateProperties().ScriptId;

            return 0;
        }

        public Unit GetCaster()
        {
            return Global.ObjAccessor.GetUnit(this, GetCasterGuid());
        }

        public override uint GetFaction()
        {
            Unit caster = GetCaster();

            if (caster)
                return caster.GetFaction();

            return 0;
        }

        public void UpdateShape()
        {
            if (_shape.IsPolygon())
                UpdatePolygonOrientation();
        }

        public void InitSplines(List<Vector3> splinePoints, uint timeToTarget)
        {
            if (splinePoints.Count < 2)
                return;

            _movementTime = 0;

            _spline.InitSpline(splinePoints.ToArray(), splinePoints.Count, EvaluationMode.Linear);
            _spline.InitLengths();

            // should be sent in object create packets only
            DoWithSuppressingObjectUpdates(() =>
                                           {
                                               SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.TimeToTarget), timeToTarget);
                                               _areaTriggerData.ClearChanged(_areaTriggerData.TimeToTarget);
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
                reshape.AreaTriggerSpline = new AreaTriggerSplineInfo();
                reshape.AreaTriggerSpline.ElapsedTimeForMovement = GetElapsedTimeForMovement();
                reshape.AreaTriggerSpline.TimeToTarget = timeToTarget;
                reshape.AreaTriggerSpline.Points = splinePoints;
                SendMessageToSet(reshape, true);
            }

            _reachedDestination = false;
        }

        public bool HasOrbit()
        {
            return _orbitInfo != null;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            ObjectData.WriteCreate(buffer, flags, this, target);
            _areaTriggerData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

            if (Values.HasChanged(TypeId.Object))
                ObjectData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.AreaTrigger))
                _areaTriggerData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedAreaTriggerMask, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);

            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedAreaTriggerMask.IsAnySet())
                valuesMask.Set((int)TypeId.AreaTrigger);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.AreaTrigger])
                _areaTriggerData.WriteUpdate(buffer, requestedAreaTriggerMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            Values.ClearChangesMask(_areaTriggerData);
            base.ClearUpdateMask(remove);
        }

        public T GetAI<T>() where T : AreaTriggerAI
        {
            return (T)_ai;
        }

        public bool IsServerSide()
        {
            return _areaTriggerTemplate.Id.IsServerSide;
        }

        public override bool IsNeverVisibleFor(WorldObject seer)
        {
            return base.IsNeverVisibleFor(seer) || IsServerSide();
        }

        public bool IsRemoved()
        {
            return _isRemoved;
        }

        public uint GetSpellId()
        {
            return _areaTriggerData.SpellID;
        }

        public AuraEffect GetAuraEffect()
        {
            return _aurEff;
        }

        public uint GetTimeSinceCreated()
        {
            return _timeSinceCreated;
        }

        public uint GetTimeToTarget()
        {
            return _areaTriggerData.TimeToTarget;
        }

        public uint GetTimeToTargetScale()
        {
            return _areaTriggerData.TimeToTargetScale;
        }

        public int GetDuration()
        {
            return _duration;
        }

        public int GetTotalDuration()
        {
            return _totalDuration;
        }

        public void Delay(int delaytime)
        {
            SetDuration(GetDuration() - delaytime);
        }

        public List<ObjectGuid> GetInsideUnits()
        {
            return _insideUnits;
        }

        public AreaTriggerCreateProperties GetCreateProperties()
        {
            return _areaTriggerCreateProperties;
        }

        public override ObjectGuid GetOwnerGUID()
        {
            return GetCasterGuid();
        }

        public ObjectGuid GetCasterGuid()
        {
            return _areaTriggerData.Caster;
        }

        public AreaTriggerShapeInfo GetShape()
        {
            return _shape;
        }

        public Vector3 GetRollPitchYaw()
        {
            return _rollPitchYaw;
        }

        public Vector3 GetTargetRollPitchYaw()
        {
            return _targetRollPitchYaw;
        }

        public bool HasSplines()
        {
            return !_spline.Empty();
        }

        public Spline<int> GetSpline()
        {
            return _spline;
        }

        public uint GetElapsedTimeForMovement()
        {
            return GetTimeSinceCreated();
        } // @todo: research the right value, in sniffs both timers are nearly identical

        public AreaTriggerOrbitInfo GetCircularMovementInfo()
        {
            return _orbitInfo;
        }

        private bool Create(uint areaTriggerCreatePropertiesId, Unit caster, Unit target, SpellInfo spell, Position pos, int duration, SpellCastVisualField spellVisual, ObjectGuid castId, AuraEffect aurEff)
        {
            _targetGuid = target ? target.GetGUID() : ObjectGuid.Empty;
            _aurEff = aurEff;

            SetMap(caster.GetMap());
            Relocate(pos);

            if (!IsPositionValid())
            {
                Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (areaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) not created. Invalid coordinates (X: {GetPositionX()} Y: {GetPositionY()})");

                return false;
            }

            _areaTriggerCreateProperties = Global.AreaTriggerDataStorage.GetAreaTriggerCreateProperties(areaTriggerCreatePropertiesId);

            if (_areaTriggerCreateProperties == null)
            {
                Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (areaTriggerCreatePropertiesId {areaTriggerCreatePropertiesId}) not created. Invalid areatrigger create properties Id ({areaTriggerCreatePropertiesId})");

                return false;
            }

            _areaTriggerTemplate = _areaTriggerCreateProperties.Template;

            _Create(ObjectGuid.Create(HighGuid.AreaTrigger, GetMapId(), GetTemplate() != null ? GetTemplate().Id.Id : 0, caster.GetMap().GenerateLowGuid(HighGuid.AreaTrigger)));

            if (GetTemplate() != null)
                SetEntry(GetTemplate().Id.Id);

            SetDuration(duration);

            SetObjectScale(1.0f);

            _shape = GetCreateProperties().Shape;
            _maxSearchRadius = GetCreateProperties().GetMaxSearchRadius();

            var areaTriggerData = Values.ModifyValue(_areaTriggerData);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.Caster), caster.GetGUID());
            SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.CreatingEffectGUID), castId);

            SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.SpellID), spell.Id);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.SpellForVisuals), spell.Id);

            SpellCastVisualField spellCastVisual = areaTriggerData.ModifyValue(_areaTriggerData.SpellVisual);
            SetUpdateFieldValue(ref spellCastVisual.SpellXSpellVisualID, spellVisual.SpellXSpellVisualID);
            SetUpdateFieldValue(ref spellCastVisual.ScriptVisualID, spellVisual.ScriptVisualID);

            SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.TimeToTargetScale), GetCreateProperties().TimeToTargetScale != 0 ? GetCreateProperties().TimeToTargetScale : _areaTriggerData.Duration);
            SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.BoundsRadius2D), GetMaxSearchRadius());
            SetUpdateFieldValue(areaTriggerData.ModifyValue(_areaTriggerData.DecalPropertiesID), GetCreateProperties().DecalPropertiesId);

            ScaleCurve extraScaleCurve = areaTriggerData.ModifyValue(_areaTriggerData.ExtraScaleCurve);

            if (GetCreateProperties().ExtraScale.Structured.StartTimeOffset != 0)
                SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.StartTimeOffset), GetCreateProperties().ExtraScale.Structured.StartTimeOffset);

            if (GetCreateProperties().ExtraScale.Structured.X != 0 ||
                GetCreateProperties().ExtraScale.Structured.Y != 0)
            {
                Vector2 point = new(GetCreateProperties().ExtraScale.Structured.X, GetCreateProperties().ExtraScale.Structured.Y);
                SetUpdateFieldValue(ref extraScaleCurve.ModifyValue(extraScaleCurve.Points, 0), point);
            }

            if (GetCreateProperties().ExtraScale.Structured.Z != 0 ||
                GetCreateProperties().ExtraScale.Structured.W != 0)
            {
                Vector2 point = new(GetCreateProperties().ExtraScale.Structured.Z, GetCreateProperties().ExtraScale.Structured.W);
                SetUpdateFieldValue(ref extraScaleCurve.ModifyValue(extraScaleCurve.Points, 1), point);
            }

            unsafe
            {
                if (GetCreateProperties().ExtraScale.Raw.Data[5] != 0)
                    SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.ParameterCurve), GetCreateProperties().ExtraScale.Raw.Data[5]);

                if (GetCreateProperties().ExtraScale.Structured.OverrideActive != 0)
                    SetUpdateFieldValue(extraScaleCurve.ModifyValue(extraScaleCurve.OverrideActive), GetCreateProperties().ExtraScale.Structured.OverrideActive != 0);
            }

            VisualAnim visualAnim = areaTriggerData.ModifyValue(_areaTriggerData.VisualAnim);
            SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.AnimationDataID), GetCreateProperties().AnimId);
            SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.AnimKitID), GetCreateProperties().AnimKitId);

            if (GetTemplate() != null &&
                GetTemplate().HasFlag(AreaTriggerFlags.Unk3))
                SetUpdateFieldValue(visualAnim.ModifyValue(visualAnim.Field_C), true);

            PhasingHandler.InheritPhaseShift(this, caster);

            if (target &&
                GetTemplate() != null &&
                GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
                MovementInfo.Transport.Guid = target.GetGUID();

            UpdatePositionData();
            SetZoneScript();

            UpdateShape();

            uint timeToTarget = GetCreateProperties().TimeToTarget != 0 ? GetCreateProperties().TimeToTarget : _areaTriggerData.Duration;

            if (GetCreateProperties().OrbitInfo != null)
            {
                AreaTriggerOrbitInfo orbit = GetCreateProperties().OrbitInfo;

                if (target &&
                    GetTemplate() != null &&
                    GetTemplate().HasFlag(AreaTriggerFlags.HasAttached))
                    orbit.PathTarget = target.GetGUID();
                else
                    orbit.Center = new Vector3(pos.X, pos.Y, pos.Z);

                InitOrbit(orbit, timeToTarget);
            }
            else if (GetCreateProperties().HasSplines())
            {
                InitSplineOffsets(GetCreateProperties().SplinePoints, timeToTarget);
            }

            // movement on Transport of areatriggers on unit is handled by themself
            ITransport transport = MovementInfo.Transport.Guid.IsEmpty() ? caster.GetTransport() : null;

            if (transport != null)
            {
                float x, y, z, o;
                pos.GetPosition(out x, out y, out z, out o);
                transport.CalculatePassengerOffset(ref x, ref y, ref z, ref o);
                MovementInfo.Transport.Pos.Relocate(x, y, z, o);

                // This object must be added to Transport before adding to map for the client to properly display it
                transport.AddPassenger(this);
            }

            AI_Initialize();

            // Relocate areatriggers with circular movement again
            if (HasOrbit())
                Relocate(CalculateOrbitPosition());

            if (!GetMap().AddToMap(this))
            {
                // Returning false will cause the object to be deleted - remove from Transport
                transport?.RemovePassenger(this);

                return false;
            }

            caster._RegisterAreaTrigger(this);

            _ai.OnCreate();

            return true;
        }

        private bool CreateServer(Map map, AreaTriggerTemplate areaTriggerTemplate, AreaTriggerSpawn position)
        {
            SetMap(map);
            Relocate(position.SpawnPoint);

            if (!IsPositionValid())
            {
                Log.outError(LogFilter.AreaTrigger, $"AreaTriggerServer (Id {areaTriggerTemplate.Id}) not created. Invalid coordinates (X: {GetPositionX()} Y: {GetPositionY()})");

                return false;
            }

            _areaTriggerTemplate = areaTriggerTemplate;

            _Create(ObjectGuid.Create(HighGuid.AreaTrigger, GetMapId(), areaTriggerTemplate.Id.Id, GetMap().GenerateLowGuid(HighGuid.AreaTrigger)));

            SetEntry(areaTriggerTemplate.Id.Id);

            SetObjectScale(1.0f);

            _shape = position.Shape;
            _maxSearchRadius = _shape.GetMaxSearchRadius();

            if (position.PhaseUseFlags != 0 ||
                position.PhaseId != 0 ||
                position.PhaseGroup != 0)
                PhasingHandler.InitDbPhaseShift(GetPhaseShift(), (PhaseUseFlagsValues)position.PhaseUseFlags, position.PhaseId, position.PhaseGroup);

            UpdateShape();

            AI_Initialize();

            _ai.OnCreate();

            return true;
        }

        private void _UpdateDuration(int newDuration)
        {
            _duration = newDuration;

            // should be sent in object create packets only
            DoWithSuppressingObjectUpdates(() =>
                                           {
                                               SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.Duration), (uint)_duration);
                                               _areaTriggerData.ClearChanged(_areaTriggerData.Duration);
                                           });
        }

        private float GetProgress()
        {
            return GetTimeSinceCreated() < GetTimeToTargetScale() ? (float)GetTimeSinceCreated() / GetTimeToTargetScale() : 1.0f;
        }

        private void UpdateTargetList()
        {
            List<Unit> targetList = new();

            switch (_shape.TriggerType)
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
                case AreaTriggerTypes.Disk:
                    SearchUnitInDisk(targetList);

                    break;
                case AreaTriggerTypes.BoundedPlane:
                    SearchUnitInBoundedPlane(targetList);

                    break;
                default:
                    break;
            }

            if (GetTemplate() != null)
            {
                var conditions = Global.ConditionMgr.GetConditionsForAreaTrigger(GetTemplate().Id.Id, GetTemplate().Id.IsServerSide);

                if (!conditions.Empty())
                    targetList.RemoveAll(target => !Global.ConditionMgr.IsObjectMeetToConditions(target, conditions));
            }

            HandleUnitEnterExit(targetList);
        }

        private void SearchUnits(List<Unit> targetList, float radius, bool check3D)
        {
            var check = new AnyUnitInObjectRangeCheck(this, radius, check3D);

            if (IsServerSide())
            {
                var searcher = new PlayerListSearcher(this, targetList, check);
                Cell.VisitWorldObjects(this, searcher, GetMaxSearchRadius());
            }
            else
            {
                var searcher = new UnitListSearcher(this, targetList, check);
                Cell.VisitAllObjects(this, searcher, GetMaxSearchRadius());
            }
        }

        private void SearchUnitInSphere(List<Unit> targetList)
        {
            float radius = _shape.SphereDatas.Radius;

            if (GetTemplate() != null &&
                GetTemplate().HasFlag(AreaTriggerFlags.HasDynamicShape))
                if (GetCreateProperties().MorphCurveId != 0)
                    radius = MathFunctions.lerp(_shape.SphereDatas.Radius, _shape.SphereDatas.RadiusTarget, Global.DB2Mgr.GetCurveValueAt(GetCreateProperties().MorphCurveId, GetProgress()));

            SearchUnits(targetList, radius, true);
        }

        private void SearchUnitInBox(List<Unit> targetList)
        {
            SearchUnits(targetList, GetMaxSearchRadius(), false);

            Position boxCenter = GetPosition();
            float extentsX, extentsY, extentsZ;

            unsafe
            {
                extentsX = _shape.BoxDatas.Extents[0];
                extentsY = _shape.BoxDatas.Extents[1];
                extentsZ = _shape.BoxDatas.Extents[2];
            }

            targetList.RemoveAll(unit => !unit.IsWithinBox(boxCenter, extentsX, extentsY, extentsZ));
        }

        private void SearchUnitInPolygon(List<Unit> targetList)
        {
            SearchUnits(targetList, GetMaxSearchRadius(), false);

            float height = _shape.PolygonDatas.Height;
            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            targetList.RemoveAll(unit => !CheckIsInPolygon2D(unit) || unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        private void SearchUnitInCylinder(List<Unit> targetList)
        {
            SearchUnits(targetList, GetMaxSearchRadius(), false);

            float height = _shape.CylinderDatas.Height;
            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            targetList.RemoveAll(unit => unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        private void SearchUnitInDisk(List<Unit> targetList)
        {
            SearchUnits(targetList, GetMaxSearchRadius(), false);

            float innerRadius = _shape.DiskDatas.InnerRadius;
            float height = _shape.DiskDatas.Height;
            float minZ = GetPositionZ() - height;
            float maxZ = GetPositionZ() + height;

            targetList.RemoveAll(unit => unit.IsInDist2d(this, innerRadius) || unit.GetPositionZ() < minZ || unit.GetPositionZ() > maxZ);
        }

        private void SearchUnitInBoundedPlane(List<Unit> targetList)
        {
            SearchUnits(targetList, GetMaxSearchRadius(), false);

            Position boxCenter = GetPosition();
            float extentsX, extentsY;

            unsafe
            {
                extentsX = _shape.BoxDatas.Extents[0];
                extentsY = _shape.BoxDatas.Extents[1];
            }

            targetList.RemoveAll(unit => { return !unit.IsWithinBox(boxCenter, extentsX, extentsY, MapConst.MapSize); });
        }

        private void HandleUnitEnterExit(List<Unit> newTargetList)
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

                if (player)
                {
                    if (player.IsDebugAreaTriggers)
                        player.SendSysMessage(CypherStrings.DebugAreatriggerEntered, GetEntry());

                    player.UpdateQuestObjectiveProgress(QuestObjectiveType.AreaTriggerEnter, (int)GetEntry(), 1);
                }

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
                    {
                        if (player.IsDebugAreaTriggers)
                            player.SendSysMessage(CypherStrings.DebugAreatriggerLeft, GetEntry());

                        player.UpdateQuestObjectiveProgress(QuestObjectiveType.AreaTriggerExit, (int)GetEntry(), 1);
                    }

                    UndoActions(leavingUnit);

                    _ai.OnUnitExit(leavingUnit);
                }
            }
        }

        private Unit GetTarget()
        {
            return Global.ObjAccessor.GetUnit(this, _targetGuid);
        }

        private void UpdatePolygonOrientation()
        {
            float newOrientation = GetOrientation();

            // No need to recalculate, orientation didn't change
            if (MathFunctions.fuzzyEq(_previousCheckOrientation, newOrientation))
                return;

            _polygonVertices = GetCreateProperties().PolygonVertices;

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

        private bool CheckIsInPolygon2D(Position pos)
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
                    //if i is the last vertex, let j be the first vertex
                    nextVertex = 0;
                else
                    //for all-else, let j=(i+1)th vertex
                    nextVertex = vertex + 1;

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
                        //this statement changes true to false (and vice-versa)
                        locatedInPolygon = !locatedInPolygon; //end if (isLeftToLine)
                }                                             //end if (withinYsEdges
            }

            return locatedInPolygon;
        }

        private bool UnitFitToActionRequirement(Unit unit, Unit caster, AreaTriggerAction action)
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

        private void DoActions(Unit unit)
        {
            Unit caster = IsServerSide() ? unit : GetCaster();

            if (caster != null &&
                GetTemplate() != null)
                foreach (AreaTriggerAction action in GetTemplate().Actions)
                    if (IsServerSide() ||
                        UnitFitToActionRequirement(unit, caster, action))
                        switch (action.ActionType)
                        {
                            case AreaTriggerActionTypes.Cast:
                                caster.CastSpell(unit,
                                                 action.Param,
                                                 new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                     .SetOriginalCastId(_areaTriggerData.CreatingEffectGUID.Value.IsCast() ? _areaTriggerData.CreatingEffectGUID : ObjectGuid.Empty));

                                break;
                            case AreaTriggerActionTypes.AddAura:
                                caster.AddAura(action.Param, unit);

                                break;
                            case AreaTriggerActionTypes.Teleport:
                                WorldSafeLocsEntry safeLoc = Global.ObjectMgr.GetWorldSafeLoc(action.Param);

                                if (safeLoc != null)
                                {
                                    Player player = caster.ToPlayer();

                                    player?.TeleportTo(safeLoc.Loc);
                                }

                                break;
                            default:
                                break;
                        }
        }

        private void UndoActions(Unit unit)
        {
            if (GetTemplate() != null)
                foreach (AreaTriggerAction action in GetTemplate().Actions)
                    if (action.ActionType == AreaTriggerActionTypes.Cast ||
                        action.ActionType == AreaTriggerActionTypes.AddAura)
                        unit.RemoveAurasDueToSpell(action.Param, GetCasterGuid());
        }

        private void InitSplineOffsets(List<Vector3> offsets, uint timeToTarget)
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

            InitSplines(rotatedPoints, timeToTarget);
        }

        private void InitOrbit(AreaTriggerOrbitInfo orbit, uint timeToTarget)
        {
            // Circular movement requires either a center position or an attached unit
            Cypher.Assert(orbit.Center.HasValue || orbit.PathTarget.HasValue);

            // should be sent in object create packets only
            DoWithSuppressingObjectUpdates(() =>
                                           {
                                               SetUpdateFieldValue(Values.ModifyValue(_areaTriggerData).ModifyValue(_areaTriggerData.TimeToTarget), timeToTarget);
                                               _areaTriggerData.ClearChanged(_areaTriggerData.TimeToTarget);
                                           });

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

        private Position GetOrbitCenterPosition()
        {
            if (_orbitInfo == null)
                return null;

            if (_orbitInfo.PathTarget.HasValue)
            {
                WorldObject center = Global.ObjAccessor.GetWorldObject(this, _orbitInfo.PathTarget.Value);

                if (center)
                    return center;
            }

            if (_orbitInfo.Center.HasValue)
                return new Position(_orbitInfo.Center.Value);

            return null;
        }

        private Position CalculateOrbitPosition()
        {
            Position centerPos = GetOrbitCenterPosition();

            if (centerPos == null)
                return GetPosition();

            AreaTriggerOrbitInfo cmi = _orbitInfo;

            // AreaTrigger make exactly "Duration / TimeToTarget" loops during his life Time
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

        private void UpdateOrbitPosition(uint diff)
        {
            if (_orbitInfo.StartDelay > GetElapsedTimeForMovement())
                return;

            _orbitInfo.ElapsedTimeForMovement = (int)(GetElapsedTimeForMovement() - _orbitInfo.StartDelay);

            Position pos = CalculateOrbitPosition();

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

            if (GetCreateProperties().MoveCurveId != 0)
            {
                float progress = Global.DB2Mgr.GetCurveValueAt(GetCreateProperties().MoveCurveId, currentTimePercent);

                if (progress < 0.0f ||
                    progress > 1.0f)
                    Log.outError(LogFilter.AreaTrigger, $"AreaTrigger (Id: {GetEntry()}, AreaTriggerCreatePropertiesId: {GetCreateProperties().Id}) has wrong progress ({progress}) caused by curve calculation (MoveCurveId: {GetCreateProperties().MorphCurveId})");
                else
                    currentTimePercent = progress;
            }

            int lastPositionIndex = 0;
            float percentFromLastPoint = 0;
            _spline.ComputeIndex(currentTimePercent, ref lastPositionIndex, ref percentFromLastPoint);

            Vector3 currentPosition;
            _spline.Evaluate_Percent(lastPositionIndex, percentFromLastPoint, out currentPosition);

            float orientation = GetOrientation();

            if (GetTemplate() != null &&
                GetTemplate().HasFlag(AreaTriggerFlags.HasFaceMovementDir))
            {
                Vector3 nextPoint = _spline.GetPoint(lastPositionIndex + 1);
                orientation = GetAbsoluteAngle(nextPoint.X, nextPoint.Y);
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
            AreaTriggerAI ai = Global.ScriptMgr.RunScriptRet<IAreaTriggerEntityGetAI, AreaTriggerAI>(p => p.GetAI(this), GetScriptId(), null);

            if (ai == null)
                ai = new NullAreaTriggerAI(this);

            _ai = ai;
            _ai.OnInitialize();
        }

        private void AI_Destroy()
        {
            _ai = null;
        }

        [Conditional("DEBUG")]
        private void DebugVisualizePosition()
        {
            Unit caster = GetCaster();

            if (caster)
            {
                Player player = caster.ToPlayer();

                if (player)
                    if (player.IsDebugAreaTriggers)
                        player.SummonCreature(1, this, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(GetTimeToTarget()));
            }
        }

        private float GetMaxSearchRadius()
        {
            return _maxSearchRadius;
        }
    }
}