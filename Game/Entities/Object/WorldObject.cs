﻿/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

using Framework.Collections;
using Framework.Constants;
using Framework.Dynamic;
using Framework.GameMath;
using Framework.IO;
using Game.AI;
using Game.BattleFields;
using Game.DataStorage;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using Game.Scenarios;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Game.Entities
{
    public class WorldObject : WorldLocation, IDisposable
    {
        public WorldObject(bool isWorldObject)
        {
            _name = "";
            m_isWorldObject = isWorldObject;
            phaseMask = PhaseMasks.Normal;

            m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive | GhostVisibilityType.Ghost);
            m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);

            objectTypeId = TypeId.Object;
            objectTypeMask = TypeMask.Object;

            _fieldNotifyFlags = UpdateFieldFlags.Dynamic;

            m_movementInfo = new MovementInfo();
            m_updateFlag = UpdateFlag.None;
        }

        public virtual void Dispose()
        {
            // this may happen because there are many !create/delete
            if (IsWorldObject() && _currMap)
            {
                if (IsTypeId(TypeId.Corpse))
                {
                    Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() Corpse Type: {0} ({1}) deleted but still in map!!",
                        ToCorpse().GetCorpseType(), GetGUID().ToString());
                    Contract.Assert(false);
                }
                ResetMap();
            }

            if (IsInWorld)
            {
                Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() {0} deleted but still in world!!", GetGUID().ToString());
                if (isTypeMask(TypeMask.Item))
                    Log.outFatal(LogFilter.Misc, "Item slot {0}", ((Item)this).GetSlot());
                Contract.Assert(false);
            }

            if (m_objectUpdated)
            {
                Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() {0} deleted but still in update list!!", GetGUID().ToString());
                Contract.Assert(false);
            }
        }

        void InitValues()
        {
            UpdateData = new Hashtable((int)valuesCount);
            for (int i = 0; i < valuesCount; ++i)
                UpdateData[i] = 0u;

            _changesMask = new BitArray((int)valuesCount);

            if (_dynamicValuesCount != 0)
            {
                _dynamicValues = new Dictionary<int, uint>[_dynamicValuesCount];
                _dynamicChangesArrayMask = new BitArray[_dynamicValuesCount];

                for (var i = 0; i < _dynamicValuesCount; ++i)
                {
                    _dynamicValues[i] = new Dictionary<int, uint>();
                    _dynamicChangesArrayMask[i] = new BitArray(0);
                    _dynamicChangesMask[i] = DynamicFieldChangeType.Unchanged;
                }
            }

            m_objectUpdated = false;
        }

        public void _Create(ObjectGuid guid)
        {
            if (UpdateData == null)
                InitValues();

            SetGuidValue(ObjectFields.Guid, guid);
            SetUInt16Value(ObjectFields.Type, 0, (ushort)objectTypeMask);
        }

        public string _ConcatFields(object startIndex, uint size)
        {
            StringBuilder sb = new StringBuilder();
            for (int index = 0; index < size; ++index)
                sb.AppendFormat("{0} ", GetUInt32Value(index + (int)startIndex));
            return sb.ToString();
        }

        public virtual void AddToWorld()
        {
            if (IsInWorld)
                return;

            IsInWorld = true;
            ClearUpdateMask(true);
        }

        public virtual void RemoveFromWorld()
        {
            if (!IsInWorld)
                return;

            if (!objectTypeMask.HasAnyFlag(TypeMask.Item | TypeMask.Container))
                DestroyForNearbyPlayers();

            IsInWorld = false;
            ClearUpdateMask(true);
        }

        public virtual void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
        {
            if (!target)
                return;

            UpdateType updateType = UpdateType.CreateObject;
            UpdateFlag flags = m_updateFlag;

            if (target == this)
                flags |= UpdateFlag.Self;

            switch (GetGUID().GetHigh())
            {
                case HighGuid.Player:
                case HighGuid.Pet:
                case HighGuid.Corpse:
                case HighGuid.DynamicObject:
                case HighGuid.AreaTrigger:
                case HighGuid.Conversation:
                    updateType = UpdateType.CreateObject2;
                    break;
                case HighGuid.Creature:
                case HighGuid.Vehicle:
                    TempSummon summon = ToUnit().ToTempSummon();
                    if (summon)
                        if (summon.GetSummonerGUID().IsPlayer())
                            updateType = UpdateType.CreateObject2;
                    break;
                case HighGuid.GameObject:
                    if (ToGameObject().GetOwnerGUID().IsPlayer())
                        updateType = UpdateType.CreateObject2;
                    break;
            }

            if (!flags.HasAnyFlag(UpdateFlag.Living))
            {
                if (!m_movementInfo.transport.guid.IsEmpty())
                    flags |= UpdateFlag.TransportPosition;

                if (GetAIAnimKitId() != 0 || GetMovementAnimKitId() != 0 || GetMeleeAnimKitId() != 0)
                    flags |= UpdateFlag.AnimKits;
            }

            if (flags.HasAnyFlag(UpdateFlag.StationaryPosition))
            {
                // UPDATETYPE_CREATE_OBJECT2 for some gameobject types...
                if (isTypeMask(TypeMask.GameObject))
                {
                    switch (ToGameObject().GetGoType())
                    {
                        case GameObjectTypes.Trap:
                        case GameObjectTypes.DuelArbiter:
                        case GameObjectTypes.FlagStand:
                        case GameObjectTypes.FlagDrop:
                            updateType = UpdateType.CreateObject2;
                            break;
                        default:
                            break;
                    }
                }
            }
            Unit unit = ToUnit();
            if (unit)
                if (unit.GetVictim())
                    flags |= UpdateFlag.HasTarget;

            if (target.m_clientGUIDs.Contains(GetGUID()))
            {

            }

            WorldPacket buffer = new WorldPacket();
            buffer.WriteUInt8(updateType);
            buffer.WritePackedGuid(GetGUID());
            buffer.WriteUInt8(objectTypeId);

            BuildMovementUpdate(buffer, flags);
            BuildValuesUpdate(updateType, buffer, target);
            BuildDynamicValuesUpdate(updateType, buffer, target);
            data.AddUpdateBlock(buffer);
        }

        public void SendUpdateToPlayer(Player player)
        {
            // send create update to player
            UpdateData upd = new UpdateData(player.GetMapId());
            UpdateObject packet;

            if (player.HaveAtClient(this))
                BuildValuesUpdateBlockForPlayer(upd, player);
            else
                BuildCreateUpdateBlockForPlayer(upd, player);

            upd.BuildPacket(out packet);
            player.SendPacket(packet);
        }

        public void BuildValuesUpdateBlockForPlayer(UpdateData data, Player target)
        {
            WorldPacket buffer = new WorldPacket();

            buffer.WriteUInt8(UpdateType.Values);
            buffer.WritePackedGuid(GetGUID());

            BuildValuesUpdate(UpdateType.Values, buffer, target);
            BuildDynamicValuesUpdate(UpdateType.Values, buffer, target);

            data.AddUpdateBlock(buffer);
        }

        public void BuildOutOfRangeUpdateBlock(UpdateData data)
        {
            data.AddOutOfRangeGUID(GetGUID());
        }

        public virtual void DestroyForPlayer(Player target)
        {
            UpdateData updateData = new UpdateData(target.GetMapId());
            BuildOutOfRangeUpdateBlock(updateData);
            UpdateObject packet;
            updateData.BuildPacket(out packet);
            target.SendPacket(packet);
        }

        public int GetInt32Value(object index)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, false));
            return Convert.ToInt32(UpdateData[(int)index]);
        }

        public uint GetUInt32Value(object index)
        {
            Contract.Assert(Convert.ToInt32(index) < valuesCount || PrintIndexError(index, false));
            return Convert.ToUInt32(UpdateData[Convert.ToInt32(index)]);
        }

        public ulong GetUInt64Value(object index)
        {
            Contract.Assert((int)index + 1 < valuesCount || PrintIndexError(index, false));
            return (Convert.ToUInt64(UpdateData[(int)index + 1]) << 32 | (uint)UpdateData[(int)index]);
        }

        public float GetFloatValue(object index)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, false));
            return Convert.ToSingle(UpdateData[(int)index]);
        }

        public byte GetByteValue(object index, byte offset)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, false));
            Contract.Assert(offset < 4);
            return Convert.ToByte(((uint)UpdateData[(int)index] >> (offset * 8)) & 0xFF);
        }

        public ushort GetUInt16Value(object index, byte offset)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, false));
            Contract.Assert(offset < 2);
            return Convert.ToUInt16(((uint)UpdateData[(int)index] >> (offset * 16)) & 0xFFFF);
        }

        public ObjectGuid GetGuidValue(object index)
        {
            Contract.Assert((int)index + 3 < valuesCount || PrintIndexError(index, false));
            return new ObjectGuid(GetUInt64Value((int)index + 2), GetUInt64Value(index));
        }

        public void BuildMovementUpdate(WorldPacket data, UpdateFlag flags)
        {
            bool NoBirthAnim = false;
            bool EnablePortals = false;
            bool PlayHoverAnim = false;
            bool HasMovementUpdate = flags.HasAnyFlag(UpdateFlag.Living);
            bool HasMovementTransport = flags.HasAnyFlag(UpdateFlag.TransportPosition);
            bool Stationary = flags.HasAnyFlag(UpdateFlag.StationaryPosition);
            bool CombatVictim = flags.HasAnyFlag(UpdateFlag.HasTarget);
            bool ServerTime = flags.HasAnyFlag(UpdateFlag.Transport);
            bool VehicleCreate = flags.HasAnyFlag(UpdateFlag.Vehicle);
            bool AnimKitCreate = flags.HasAnyFlag(UpdateFlag.AnimKits);
            bool Rotation = flags.HasAnyFlag(UpdateFlag.Rotation);
            bool HasAreaTrigger = flags.HasAnyFlag(UpdateFlag.Areatrigger);
            bool HasGameObject = false;
            bool ThisIsYou = flags.HasAnyFlag(UpdateFlag.Self);
            bool SmoothPhasing = false;
            bool SceneObjCreate = false;
            bool PlayerCreateData = false;
            int PauseTimesCount = 0;

            GameObject go = ToGameObject();
            if (go)
            {
                if (go.GetGoType() == GameObjectTypes.Transport)
                    PauseTimesCount = go.m_goValue.Transport.StopFrames.Count;
            }

            data.WriteBit(NoBirthAnim);
            data.WriteBit(EnablePortals);
            data.WriteBit(PlayHoverAnim);
            data.WriteBit(HasMovementUpdate);
            data.WriteBit(HasMovementTransport);
            data.WriteBit(Stationary);
            data.WriteBit(CombatVictim);
            data.WriteBit(ServerTime);
            data.WriteBit(VehicleCreate);
            data.WriteBit(AnimKitCreate);
            data.WriteBit(Rotation);
            data.WriteBit(HasAreaTrigger);
            data.WriteBit(HasGameObject);
            data.WriteBit(SmoothPhasing);
            data.WriteBit(ThisIsYou);
            data.WriteBit(SceneObjCreate);
            data.WriteBit(PlayerCreateData);
            data.FlushBits();

            if (HasMovementUpdate)
            {
                Unit unit = ToUnit();
                bool HasFallDirection = unit.HasUnitMovementFlag(MovementFlag.Falling);
                bool HasFall = HasFallDirection || unit.m_movementInfo.jump.fallTime != 0;
                bool HasSpline = unit.IsSplineEnabled();

                data.WritePackedGuid(GetGUID());                                         // MoverGUID

                data.WriteUInt32(unit.m_movementInfo.Time);                     // MoveTime
                data.WriteFloat(unit.GetPositionX());
                data.WriteFloat(unit.GetPositionY());
                data.WriteFloat(unit.GetPositionZ());
                data.WriteFloat(unit.GetOrientation());

                data.WriteFloat(unit.m_movementInfo.Pitch);                     // Pitch
                data.WriteFloat(unit.m_movementInfo.SplineElevation);           // StepUpStartElevation

                data.WriteUInt32(0);                                             // RemoveForcesIDs.size()
                data.WriteUInt32(0);                                             // MoveIndex

                //for (public uint i = 0; i < RemoveForcesIDs.Count; ++i)
                //    *data << ObjectGuid(RemoveForcesIDs);

                data.WriteBits((uint)unit.GetUnitMovementFlags(), 30);
                data.WriteBits((uint)unit.GetUnitMovementFlags2(), 18);
                data.WriteBit(!unit.m_movementInfo.transport.guid.IsEmpty()); // HasTransport
                data.WriteBit(HasFall);                                        // HasFall
                data.WriteBit(HasSpline);                                      // HasSpline - marks that the unit uses spline movement
                data.WriteBit(0);                                              // HeightChangeFailed
                data.WriteBit(0);                                              // RemoteTimeValid

                if (!unit.m_movementInfo.transport.guid.IsEmpty())
                    MovementExtensions.WriteTransportInfo(data, unit.m_movementInfo.transport);

                if (HasFall)
                {
                    data.WriteUInt32(unit.m_movementInfo.jump.fallTime);              // Time
                    data.WriteFloat(unit.m_movementInfo.jump.zspeed);                 // JumpVelocity

                    if (data.WriteBit(HasFallDirection))
                    {
                        data.WriteFloat(unit.m_movementInfo.jump.sinAngle);           // Direction
                        data.WriteFloat(unit.m_movementInfo.jump.cosAngle);
                        data.WriteFloat(unit.m_movementInfo.jump.xyspeed);            // Speed
                    }
                }

                data.WriteFloat(unit.GetSpeed(UnitMoveType.Walk));
                data.WriteFloat(unit.GetSpeed(UnitMoveType.Run));
                data.WriteFloat(unit.GetSpeed(UnitMoveType.RunBack));
                data.WriteFloat(unit.GetSpeed(UnitMoveType.Swim));
                data.WriteFloat(unit.GetSpeed(UnitMoveType.SwimBack));
                data.WriteFloat(unit.GetSpeed(UnitMoveType.Flight));
                data.WriteFloat(unit.GetSpeed(UnitMoveType.FlightBack));
                data.WriteFloat(unit.GetSpeed(UnitMoveType.TurnRate));
                data.WriteFloat(unit.GetSpeed(UnitMoveType.PitchRate));

                data.WriteUInt32(0); // unit.m_movementInfo.forces.size()

                data.WriteBit(HasSpline);
                data.FlushBits();

                //for (public uint i = 0; i < unit.m_movementInfo.forces.Count; ++i)
                //{
                //    *data << ObjectGuid(ID);
                //    *data << Vector3(Origin);
                //    *data << Vector3(Direction);
                //    *data << uint32(TransportID);
                //    *data.WriteFloat(Magnitude);
                //    *data.WriteBits(Type, 2);
                //}

                // HasMovementSpline - marks that spline data is present in packet
                if (HasSpline)
                    MovementExtensions.WriteCreateObjectSplineDataBlock(unit.moveSpline, data);
            }

            data.WriteUInt32(PauseTimesCount);

            if (Stationary)
            {
                WorldObject self = this;
                data.WriteFloat(self.GetStationaryX());
                data.WriteFloat(self.GetStationaryY());
                data.WriteFloat(self.GetStationaryZ());
                data.WriteFloat(self.GetStationaryO());
            }

            if (CombatVictim)
                data.WritePackedGuid(ToUnit().GetVictim().GetGUID());                      // CombatVictim

            if (ServerTime)
            {
                GameObject go1 = ToGameObject();
                /** @TODO Use IsTransport() to also handle type 11 (TRANSPORT)
                    Currently grid objects are not updated if there are no nearby players,
                    this causes clients to receive different PathProgress
                    resulting in players seeing the object in a different position
                */
                if (go1 && go1.ToTransport())                                    // ServerTime
                    data.WriteUInt32(go1.m_goValue.Transport.PathProgress);
                else
                    data.WriteUInt32(Time.GetMSTime());
            }

            if (VehicleCreate)
            {
                Unit unit = ToUnit();
                data.WriteUInt32(unit.GetVehicleKit().GetVehicleInfo().Id); // RecID
                data.WriteFloat(unit.GetOrientation());                         // InitialRawFacing
            }

            if (AnimKitCreate)
            {
                data.WriteUInt16(GetAIAnimKitId());                        // AiID
                data.WriteUInt16(GetMovementAnimKitId());                  // MovementID
                data.WriteUInt16(GetMeleeAnimKitId());                     // MeleeID
            }

            if (Rotation)
                data.WriteInt64(ToGameObject().GetPackedWorldRotation());                 // Rotation

            if (go)
            {
                for (int i = 0; i < PauseTimesCount; ++i)
                    data.WriteUInt32(go.m_goValue.Transport.StopFrames[i]);
            }

            if (HasMovementTransport)
            {
                WorldObject self = this;
                MovementExtensions.WriteTransportInfo(data, self.m_movementInfo.transport);
            }

            if (HasAreaTrigger)
            {
                AreaTrigger areaTrigger = ToAreaTrigger();
                AreaTriggerMiscTemplate areaTriggerMiscTemplate = areaTrigger.GetMiscTemplate();
                AreaTriggerTemplate areaTriggerTemplate = areaTrigger.GetTemplate();

                data.WriteUInt32(areaTrigger.GetTimeSinceCreated());

                data.WriteVector3(areaTrigger.GetRollPitchYaw());

                bool hasAbsoluteOrientation = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAbsoluteOrientation);
                bool hasDynamicShape = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasDynamicShape);
                bool hasAttached = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAttached);
                bool hasFaceMovementDir = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFaceMovementDir);
                bool hasFollowsTerrain = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFollowsTerrain);
                bool hasUnk1 = areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk1);
                bool hasTargetRollPitchYaw = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasTargetRollPitchYaw);
                bool hasScaleCurveID = areaTriggerMiscTemplate.ScaleCurveId != 0;
                bool hasMorphCurveID = areaTriggerMiscTemplate.MorphCurveId != 0;
                bool hasFacingCurveID = areaTriggerMiscTemplate.FacingCurveId != 0;
                bool hasMoveCurveID = areaTriggerMiscTemplate.MoveCurveId != 0;
                bool hasUnk2 = areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk2);
                bool hasUnk3 = areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk3);
                bool hasUnk4 = areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk4);
                bool hasAreaTriggerSphere = areaTriggerTemplate.IsSphere();
                bool hasAreaTriggerBox = areaTriggerTemplate.IsBox();
                bool hasAreaTriggerPolygon = areaTriggerTemplate.IsPolygon();
                bool hasAreaTriggerCylinder = areaTriggerTemplate.IsCylinder();
                bool hasAreaTriggerSpline = areaTrigger.HasSplines();
                bool hasAreaTriggerUnkType = false; // areaTriggerTemplate.HasFlag(AREATRIGGER_FLAG_UNK5);

                data.WriteBit(hasAbsoluteOrientation);
                data.WriteBit(hasDynamicShape);
                data.WriteBit(hasAttached);
                data.WriteBit(hasFaceMovementDir);
                data.WriteBit(hasFollowsTerrain);
                data.WriteBit(hasUnk1);
                data.WriteBit(hasTargetRollPitchYaw);
                data.WriteBit(hasScaleCurveID);
                data.WriteBit(hasMorphCurveID);
                data.WriteBit(hasFacingCurveID);
                data.WriteBit(hasMoveCurveID);
                data.WriteBit(hasUnk2);
                data.WriteBit(hasUnk3);
                data.WriteBit(hasUnk4);
                data.WriteBit(hasAreaTriggerSphere);
                data.WriteBit(hasAreaTriggerBox);
                data.WriteBit(hasAreaTriggerPolygon);
                data.WriteBit(hasAreaTriggerCylinder);
                data.WriteBit(hasAreaTriggerSpline);
                data.WriteBit(hasAreaTriggerUnkType);

                if (hasUnk3)
                    data.WriteBit(0);

                data.FlushBits();

                if (hasAreaTriggerSpline)
                {
                    data.WriteUInt32(areaTrigger.GetTimeToTarget());
                    data.WriteUInt32(areaTrigger.GetElapsedTimeForMovement());

                    MovementExtensions.WriteCreateObjectAreaTriggerSpline(areaTrigger.GetSpline(), data);
                }

                if (hasTargetRollPitchYaw)
                    data.WriteVector3(areaTrigger.GetTargetRollPitchYaw());

                if (hasScaleCurveID)
                    data.WriteUInt32(areaTriggerMiscTemplate.ScaleCurveId);

                if (hasMorphCurveID)
                    data.WriteUInt32(areaTriggerMiscTemplate.MorphCurveId);

                if (hasFacingCurveID)
                    data.WriteUInt32(areaTriggerMiscTemplate.FacingCurveId);

                if (hasMoveCurveID)
                    data.WriteUInt32(areaTriggerMiscTemplate.MoveCurveId);

                if (hasUnk2)
                    data.WriteInt32(0);

                if (hasUnk4)
                    data.WriteUInt32(0);

                if (hasAreaTriggerSphere)
                {
                    data.WriteFloat(areaTriggerTemplate.SphereDatas.Radius);
                    data.WriteFloat(areaTriggerTemplate.SphereDatas.RadiusTarget);
                }

                if (hasAreaTriggerBox)
                {
                    unsafe
                    {
                        fixed (float* ptr = areaTriggerTemplate.BoxDatas.Extents)
                        {
                            data.WriteFloat(ptr[0]);
                            data.WriteFloat(ptr[1]);
                            data.WriteFloat(ptr[2]);
                        }

                        fixed (float* ptr = areaTriggerTemplate.BoxDatas.ExtentsTarget)
                        {
                            data.WriteFloat(ptr[0]);
                            data.WriteFloat(ptr[1]);
                            data.WriteFloat(ptr[2]);
                        }
                    }
                }

                if (hasAreaTriggerPolygon)
                {
                    data.WriteInt32(areaTriggerTemplate.PolygonVertices.Count);
                    data.WriteInt32(areaTriggerTemplate.PolygonVerticesTarget.Count);
                    data.WriteFloat(areaTriggerTemplate.PolygonDatas.Height);
                    data.WriteFloat(areaTriggerTemplate.PolygonDatas.HeightTarget);

                    foreach (var vertice in areaTriggerTemplate.PolygonVertices)
                        data.WriteVector2(vertice);

                    foreach (var vertice in areaTriggerTemplate.PolygonVerticesTarget)
                        data.WriteVector2(vertice);
                }

                if (hasAreaTriggerCylinder)
                {
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.Radius);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.RadiusTarget);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.Height);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.HeightTarget);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.LocationZOffset);
                    data.WriteFloat(areaTriggerTemplate.CylinderDatas.LocationZOffsetTarget);
                }

                if (hasAreaTriggerUnkType)
                {
                    /*packet.ResetBitReader();
                    var unk1 = packet.ReadBit("AreaTriggerUnk1");
                    var hasCenter = packet.ReadBit("HasCenter", index);
                    packet.ReadBit("Unk bit 703 1", index);
                    packet.ReadBit("Unk bit 703 2", index);

                    packet.ReadUInt32();
                    packet.ReadInt32();
                    packet.ReadUInt32();
                    packet.ReadSingle("Radius", index);
                    packet.ReadSingle("BlendFromRadius", index);
                    packet.ReadSingle("InitialAngel", index);
                    packet.ReadSingle("ZOffset", index);

                    if (unk1)
                        packet.ReadPackedGuid128("AreaTriggerUnkGUID", index);

                    if (hasCenter)
                        packet.ReadVector3("Center", index);*/
                }
            }

            //if (HasGameObject)
            //{
            //    *data << uint32(WorldEffectID);

            //    data.WriteBit(bit8);
            //    if (bit8)
            //        *data << uint32(Int1);
            //}

            //if (SmoothPhasing)
            //{
            //    data.WriteBit(ReplaceActive);
            //    data.WriteBit(HasReplaceObjectt);
            //    data.FlushBits();
            //    if (HasReplaceObject)
            //        *data << ObjectGuid(ReplaceObject);
            //}

            //if (SceneObjCreate)
            //{
            //    data.WriteBit(HasLocalScriptData);
            //    data.WriteBit(HasPetBattleFullUpdate);
            //    data.FlushBits();

            //    if (HasLocalScriptData)
            //    {
            //        data.WriteBits(Data.length(), 7);
            //        data.FlushBits();
            //        data.WriteString(Data);
            //    }

            //    if (HasPetBattleFullUpdate)
            //    {
            //        for (std::size_t i = 0; i < 2; ++i)
            //        {
            //            *data << ObjectGuid(Players[i].CharacterID);
            //            *data << int32(Players[i].TrapAbilityID);
            //            *data << int32(Players[i].TrapStatus);
            //            *data << uint16(Players[i].RoundTimeSecs);
            //            *data << int8(Players[i].FrontPet);
            //            *data << uint8(Players[i].InputFlags);

            //            data.WriteBits(Players[i].Pets.size(), 2);
            //            data.FlushBits();
            //            for (std::size_t j = 0; j < Players[i].Pets.size(); ++j)
            //            {
            //                *data << ObjectGuid(Players[i].Pets[j].BattlePetGUID);
            //                *data << int32(Players[i].Pets[j].SpeciesID);
            //                *data << int32(Players[i].Pets[j].DisplayID);
            //                *data << int32(Players[i].Pets[j].CollarID);
            //                *data << int16(Players[i].Pets[j].Level);
            //                *data << int16(Players[i].Pets[j].Xp);
            //                *data << int32(Players[i].Pets[j].CurHealth);
            //                *data << int32(Players[i].Pets[j].MaxHealth);
            //                *data << int32(Players[i].Pets[j].Power);
            //                *data << int32(Players[i].Pets[j].Speed);
            //                *data << int32(Players[i].Pets[j].NpcTeamMemberID);
            //                *data << uint16(Players[i].Pets[j].BreedQuality);
            //                *data << uint16(Players[i].Pets[j].StatusFlags);
            //                *data << int8(Players[i].Pets[j].Slot);

            //                *data << uint32(Players[i].Pets[j].Abilities.size());
            //                *data << uint32(Players[i].Pets[j].Auras.size());
            //                *data << uint32(Players[i].Pets[j].States.size());
            //                for (std::size_t k = 0; k < Players[i].Pets[j].Abilities.size(); ++k)
            //                {
            //                    *data << int32(Players[i].Pets[j].Abilities[k].AbilityID);
            //                    *data << int16(Players[i].Pets[j].Abilities[k].CooldownRemaining);
            //                    *data << int16(Players[i].Pets[j].Abilities[k].LockdownRemaining);
            //                    *data << int8(Players[i].Pets[j].Abilities[k].AbilityIndex);
            //                    *data << uint8(Players[i].Pets[j].Abilities[k].Pboid);
            //                }

            //                for (std::size_t k = 0; k < Players[i].Pets[j].Auras.size(); ++k)
            //                {
            //                    *data << int32(Players[i].Pets[j].Auras[k].AbilityID);
            //                    *data << uint32(Players[i].Pets[j].Auras[k].InstanceID);
            //                    *data << int32(Players[i].Pets[j].Auras[k].RoundsRemaining);
            //                    *data << int32(Players[i].Pets[j].Auras[k].CurrentRound);
            //                    *data << uint8(Players[i].Pets[j].Auras[k].CasterPBOID);
            //                }

            //                for (std::size_t k = 0; k < Players[i].Pets[j].States.size(); ++k)
            //                {
            //                    *data << uint32(Players[i].Pets[j].States[k].StateID);
            //                    *data << int32(Players[i].Pets[j].States[k].StateValue);
            //                }

            //                data.WriteBits(Players[i].Pets[j].CustomName.length(), 7);
            //                data.FlushBits();
            //                data.WriteString(Players[i].Pets[j].CustomName);
            //            }
            //        }

            //        for (std::size_t i = 0; i < 3; ++i)
            //        {
            //            *data << uint32(Enviros[j].Auras.size());
            //            *data << uint32(Enviros[j].States.size());
            //            for (std::size_t j = 0; j < Enviros[j].Auras.size(); ++j)
            //            {
            //                *data << int32(Enviros[j].Auras[j].AbilityID);
            //                *data << uint32(Enviros[j].Auras[j].InstanceID);
            //                *data << int32(Enviros[j].Auras[j].RoundsRemaining);
            //                *data << int32(Enviros[j].Auras[j].CurrentRound);
            //                *data << uint8(Enviros[j].Auras[j].CasterPBOID);
            //            }

            //            for (std::size_t j = 0; j < Enviros[j].States.size(); ++j)
            //            {
            //                *data << uint32(Enviros[i].States[j].StateID);
            //                *data << int32(Enviros[i].States[j].StateValue);
            //            }
            //        }

            //        *data << uint16(WaitingForFrontPetsMaxSecs);
            //        *data << uint16(PvpMaxRoundTime);
            //        *data << int32(CurRound);
            //        *data << uint32(NpcCreatureID);
            //        *data << uint32(NpcDisplayID);
            //        *data << int8(CurPetBattleState);
            //        *data << uint8(ForfeitPenalty);
            //        *data << ObjectGuid(InitialWildPetGUID);
            //        data.WriteBit(IsPVP);
            //        data.WriteBit(CanAwardXP);
            //        data.FlushBits();
            //    }
            //}

            //if (PlayerCreateData)
            //{
            //    data.WriteBit(HasSceneInstanceIDs);
            //    data.WriteBit(HasRuneState);
            //    data.FlushBits();
            //    if (HasSceneInstanceIDs)
            //    {
            //        *data << uint32(SceneInstanceIDs.size());
            //        for (std::size_t i = 0; i < SceneInstanceIDs.size(); ++i)
            //            *data << uint32(SceneInstanceIDs[i]);
            //    }
            //    if (HasRuneState)
            //    {
            //        *data << uint8(RechargingRuneMask);
            //        *data << uint8(UsableRuneMask);
            //        *data << uint32(ToUnit().GetMaxPower(POWER_RUNES));
            //        for (uint32 i = 0; i < ToUnit().GetMaxPower(POWER_RUNES); ++i)
            //            *data << uint8(255 - (ToUnit().ToPlayer().GetRuneCooldown(i) * 51));
            //    }
            //}
        }

        public virtual void BuildValuesUpdate(UpdateType updatetype, ByteBuffer data, Player target)
        {
            if (!target)
                return;

            ByteBuffer fieldBuffer = new ByteBuffer();
            UpdateMask updateMask = new UpdateMask(valuesCount);

            uint[] flags;
            uint visibleFlag = GetUpdateFieldData(target, out flags);
            for (int index = 0; index < valuesCount; ++index)
            {
                if (Convert.ToBoolean(_fieldNotifyFlags & flags[index]) ||
                    ((updatetype == UpdateType.Values ? _changesMask.Get(index) : HasUpdateValue(index)) && Convert.ToBoolean(flags[index] & visibleFlag)))
                {
                    updateMask.SetBit(index);
                    WriteValue(fieldBuffer, index);
                }
            }

            updateMask.AppendToPacket(data);
            data.WriteBytes(fieldBuffer);
        }

        public virtual void BuildDynamicValuesUpdate(UpdateType updateType, WorldPacket data, Player target)
        {
            if (!target)
                return;

            ByteBuffer fieldBuffer = new ByteBuffer();
            UpdateMask fieldMask = new UpdateMask(_dynamicValuesCount);

            uint[] flags;
            uint visibleFlag = GetDynamicUpdateFieldData(target, out flags);

            for (var index = 0; index < _dynamicValuesCount; ++index)
            {
                ByteBuffer valueBuffer = new ByteBuffer();
                var values = _dynamicValues[index];
                if (_fieldNotifyFlags.HasAnyFlag(flags[index]) ||
                    ((updateType == UpdateType.Values ? _dynamicChangesMask[index] != DynamicFieldChangeType.Unchanged : !values.Empty()) && flags[index].HasAnyFlag(visibleFlag)))
                {
                    fieldMask.SetBit(index);

                    DynamicUpdateMask arrayMask = new DynamicUpdateMask((uint)(values.Empty() ? 0 : values.Keys.Max() + 1));

                    arrayMask.EncodeDynamicFieldChangeType(_dynamicChangesMask[index], updateType);
                    if (updateType == UpdateType.Values && _dynamicChangesMask[index] == DynamicFieldChangeType.ValueAndSizeChanged)
                        arrayMask.ValueCount = values.Empty() ? 0 : values.Keys.Max() + 1;

                    foreach (var pair in values)
                    {
                        if (updateType != UpdateType.Values || _dynamicChangesArrayMask[index].Get(pair.Key))
                        {
                            arrayMask.SetBit(pair.Key);
                            valueBuffer.WriteUInt32(pair.Value);
                        }
                    }

                    arrayMask.AppendToPacket(fieldBuffer);
                    fieldBuffer.WriteBytes(valueBuffer);
                }
            }

            fieldMask.AppendToPacket(data);
            data.WriteBytes(fieldBuffer);
        }

        public void WriteValue(ByteBuffer data, int index)
        {
            switch (UpdateData[index].GetType().Name)
            {
                default:
                case "UInt32":
                    data.WriteUInt32(UpdateData[index]);
                    break;
                case "Single":
                    data.WriteFloat((float)UpdateData[index]);
                    break;
                case "Int32":
                    data.WriteInt32((int)UpdateData[index]);
                    break;
            }
        }

        public bool HasUpdateValue(int index)
        {
            return UpdateData.ContainsKey(index);
        }

        public void AddToObjectUpdateIfNeeded()
        {
            if (IsInWorld && !m_objectUpdated)
            {
                AddToObjectUpdate();
                m_objectUpdated = true;
            }
        }

        public void ClearUpdateMask(bool remove)
        {
            _changesMask.SetAll(false);
            for (int i = 0; i < _dynamicValuesCount; ++i)
            {
                _dynamicChangesMask[i] = DynamicFieldChangeType.Unchanged;
                _dynamicChangesArrayMask[i].SetAll(false);
            }

            if (m_objectUpdated)
            {
                if (remove)
                    RemoveFromObjectUpdate();

                m_objectUpdated = false;
            }
        }

        public void BuildFieldsUpdate(Player player, Dictionary<Player, UpdateData> data_map)
        {
            var data = data_map.LookupByKey(player);

            if (data == null)
            {
                data_map.Add(player, new UpdateData(player.GetMapId()));
                data = data_map.LookupByKey(player);
            }

            BuildValuesUpdateBlockForPlayer(data, player);
        }

        uint GetUpdateFieldData(Player target, out uint[] flags)
        {
            var visibleFlag = UpdateFieldFlags.Public;
            flags = null;

            if (target == this)
                visibleFlag |= UpdateFieldFlags.Private;

            switch (GetTypeId())
            {
                case TypeId.Item:
                case TypeId.Container:
                    flags = UpdateFieldFlags.ItemUpdateFieldFlags;
                    if (((Item)this).GetOwnerGUID() == target.GetGUID())
                        visibleFlag |= UpdateFieldFlags.Owner | UpdateFieldFlags.ItemOwner;
                    break;
                case TypeId.Unit:
                case TypeId.Player:
                    {
                        Player plr = ToUnit().GetCharmerOrOwnerPlayerOrPlayerItself();
                        flags = UpdateFieldFlags.UnitUpdateFieldFlags;
                        if (ToUnit().GetOwnerGUID() == target.GetGUID())
                            visibleFlag |= UpdateFieldFlags.Owner;

                        if (HasFlag(ObjectFields.DynamicFlags, UnitDynFlags.SpecialInfo))
                            if (ToUnit().HasAuraTypeWithCaster(AuraType.Empathy, target.GetGUID()))
                                visibleFlag |= UpdateFieldFlags.SpecialInfo;

                        if (plr != null && plr.IsInSameGroupWith(target))
                            visibleFlag |= UpdateFieldFlags.PartyMember;
                        break;
                    }
                case TypeId.GameObject:
                    flags = UpdateFieldFlags.GameObjectUpdateFieldFlags;
                    if (ToGameObject().GetOwnerGUID() == target.GetGUID())
                        visibleFlag |= UpdateFieldFlags.Owner;
                    break;
                case TypeId.DynamicObject:
                    flags = UpdateFieldFlags.DynamicObjectUpdateFieldFlags;
                    if (ToDynamicObject().GetCasterGUID() == target.GetGUID())
                        visibleFlag |= UpdateFieldFlags.Owner;
                    break;
                case TypeId.Corpse:
                    flags = UpdateFieldFlags.CorpseUpdateFieldFlags;
                    if (ToCorpse().GetOwnerGUID() == target.GetGUID())
                        visibleFlag |= UpdateFieldFlags.Owner;
                    break;
                case TypeId.AreaTrigger:
                    flags = UpdateFieldFlags.AreaTriggerUpdateFieldFlags;
                    break;
                case TypeId.SceneObject:
                    flags = UpdateFieldFlags.SceneObjectUpdateFieldFlags;
                    break;
                case TypeId.Conversation:
                    flags = UpdateFieldFlags.ConversationUpdateFieldFlags;
                    break;
                case TypeId.Object:
                    Contract.Assert(false);
                    break;
            }
            return visibleFlag;
        }

        public uint GetDynamicUpdateFieldData(Player target, out uint[] flags)
        {
            uint visibleFlag = UpdateFieldFlags.Public;

            if (target == this)
                visibleFlag |= UpdateFieldFlags.Private;

            switch (GetTypeId())
            {
                case TypeId.Item:
                case TypeId.Container:
                    flags = UpdateFieldFlags.ItemDynamicUpdateFieldFlags;
                    if (((Item)this).GetOwnerGUID() == target.GetGUID())
                        visibleFlag |= UpdateFieldFlags.Owner | UpdateFieldFlags.ItemOwner;
                    break;
                case TypeId.Unit:
                case TypeId.Player:
                    {
                        Player plr = ToUnit().GetCharmerOrOwnerPlayerOrPlayerItself();
                        flags = UpdateFieldFlags.UnitDynamicUpdateFieldFlags;
                        if (ToUnit().GetOwnerGUID() == target.GetGUID())
                            visibleFlag |= UpdateFieldFlags.Owner;

                        if (HasFlag(ObjectFields.DynamicFlags, UnitDynFlags.SpecialInfo))
                            if (ToUnit().HasAuraTypeWithCaster(AuraType.Empathy, target.GetGUID()))
                                visibleFlag |= UpdateFieldFlags.SpecialInfo;

                        if (plr && plr.IsInSameRaidWith(target))
                            visibleFlag |= UpdateFieldFlags.PartyMember;
                        break;
                    }
                case TypeId.GameObject:
                    flags = UpdateFieldFlags.GameObjectDynamicUpdateFieldFlags;
                    break;
                case TypeId.Conversation:
                    flags = UpdateFieldFlags.ConversationDynamicUpdateFieldFlags;

                    if (ToConversation().GetCreatorGuid() == target.GetGUID())
                        visibleFlag |= UpdateFieldFlags.Unknownx100;
                    break;
                default:
                    flags = null;
                    break;
            }

            return visibleFlag;
        }

        public void _LoadIntoDataField(string data, uint startOffset, uint count)
        {
            if (string.IsNullOrEmpty(data))
                return;

            var lines = new StringArray(data, ' ');
            if (lines.Length != count)
                return;

            for (var index = 0; index < count; ++index)
            {
                UpdateData[(int)startOffset + index] = uint.Parse(lines[index]);
                _changesMask.Set((int)(startOffset + index), true);
            }
        }

        public void SetInt32Value(object index, int value)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, true));

            if (Convert.ToInt32(UpdateData[(int)index]) != value)
            {
                UpdateData[(int)index] = value;
                _changesMask.Set((int)index, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetUInt32Value(object index, uint value)
        {
            int _index = Convert.ToInt32(index);
            Contract.Assert(_index < valuesCount || PrintIndexError(index, true));

            if (Convert.ToUInt32(UpdateData[_index]) != value)
            {
                UpdateData[_index] = value;
                _changesMask.Set(_index, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void UpdateUInt32Value(object index, uint value)
        {
            UpdateData[(int)index] = value;
            _changesMask.Set((int)index, true);
        }

        public void SetUInt64Value(object index, ulong value)
        {
            Contract.Assert((int)index + 1 < valuesCount || PrintIndexError(index, true));
            if (GetUInt64Value(index) != value)
            {
                UpdateData[(int)index] = MathFunctions.Pair64_LoPart(value);
                UpdateData[(int)index + 1] = MathFunctions.Pair64_HiPart(value);
                _changesMask.Set((int)index, true);
                _changesMask.Set((int)index + 1, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public bool AddUInt64Value(object index, ulong value)
        {
            if (value != 0 && GetUInt64Value(index) == 0)
            {
                SetUInt64Value(index, value);
                return true;
            }

            return false;
        }

        public bool RemoveUInt64Value(object index, ulong value)
        {
            if (value != 0 && GetUInt64Value(index) == value)
            {
                SetUInt64Value(index, 0);
                return true;
            }

            return false;
        }

        public bool AddGuidValue(object index, ObjectGuid value)
        {
            if (!value.IsEmpty() && GetGuidValue(index).IsEmpty())
            {
                SetGuidValue(index, value);
                return true;
            }

            return false;
        }

        public bool RemoveGuidValue(object index, ObjectGuid value)
        {
            if (!value.IsEmpty() && GetGuidValue(index) == value)
            {
                SetGuidValue(index, ObjectGuid.Empty);
                return true;
            }

            return false;
        }

        public void SetFloatValue(object index, float value)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, true));

            if (GetFloatValue(index) != value)
            {
                UpdateData[(int)index] = value;
                _changesMask.Set((int)index, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetByteValue(object index, byte offset, byte value)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, true));

            if (offset > 3)
            {
                Log.outError(LogFilter.Server, "Object.SetByteValue: wrong offset {0}", offset);
                return;
            }

            if ((byte)(GetUInt32Value(index) >> (offset * 8)) != value)
            {
                UpdateData[(int)index] = (uint)UpdateData[(int)index] & ~(uint)(0xFF << (offset * 8));
                UpdateData[(int)index] = (uint)UpdateData[(int)index] | (uint)value << (offset * 8);
                _changesMask.Set((int)index, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetUInt16Value(object index, byte offset, ushort value)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, true));

            if (offset > 2)
            {
                Log.outError(LogFilter.Server, "WorldObject.SetUInt16Value: wrong offset {0}", offset);
                return;
            }

            if ((ushort)(GetUInt32Value(index) >> (offset * 16)) != value)
            {
                UpdateData[(int)index] = (uint)UpdateData[(int)index] & ~((uint)0xFFFF << (offset * 16));
                UpdateData[(int)index] = (uint)UpdateData[(int)index] | (uint)value << (offset * 16);
                _changesMask.Set((int)index, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetGuidValue(object index, ObjectGuid value)
        {
            Contract.Assert((int)index + 3 < valuesCount || PrintIndexError(index, true));
            if (new ObjectGuid(GetUInt64Value((int)index + 2), GetUInt64Value(index)) != value)
            {
                SetUInt64Value(index, value.GetLowValue());
                SetUInt64Value((int)index + 2, value.GetHighValue());

                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetStatFloatValue(object index, float value)
        {
            if (value < 0)
                value = 0.0f;

            SetFloatValue(index, value);
        }

        public void SetStatInt32Value(object index, int value)
        {
            if (value < 0)
                value = 0;

            SetUInt32Value(index, (uint)value);
        }

        public void ApplyModInt32Value(object index, int val, bool apply)
        {
            int cur = GetInt32Value(index);
            cur += (apply ? val : -val);
            if (cur < 0)
                cur = 0;
            SetInt32Value(index, cur);
        }

        public void ApplyModUInt32Value(object index, int val, bool apply)
        {
            int cur = (int)GetUInt32Value(index);
            cur += (apply ? val : -val);
            if (cur < 0)
                cur = 0;
            SetUInt32Value(index, (uint)cur);
        }

        public void ApplyModUInt16Value(object index, byte offset, short val, bool apply)
        {
            short cur = (short)GetUInt16Value(index, offset);
            cur += (short)(apply ? val : -val);
            if (cur < 0)
                cur = 0;
            SetUInt16Value(index, offset, (ushort)cur);
        }

        public void ApplyModSignedFloatValue(object index, float val, bool apply)
        {
            float cur = GetFloatValue(index);
            cur += (apply ? val : -val);
            SetFloatValue(index, cur);
        }

        public void ApplyPercentModFloatValue(object index, float val, bool apply)
        {
            float value = GetFloatValue(index);
            MathFunctions.ApplyPercentModFloatVar(ref value, val, apply);
            SetFloatValue(index, value);
        }

        public void SetFlag(object index, object newflag)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, true));
            uint oldval = (uint)UpdateData[(int)index];
            uint newval = oldval | Convert.ToUInt32(newflag);

            if (oldval != newval)
            {
                UpdateData[(int)index] = newval;
                _changesMask.Set((int)index, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void RemoveFlag(object index, object oldFlag)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, true));
            Contract.Assert(UpdateData != null);

            uint oldval = (uint)UpdateData[(int)index];
            uint newval = (oldval & ~Convert.ToUInt32(oldFlag));

            if (oldval != newval)
            {
                UpdateData[(int)index] = newval;
                _changesMask.Set((int)index, true);
                AddToObjectUpdateIfNeeded();
            }
        }

        public void ToggleFlag(object index, object flag)
        {
            if (HasFlag(index, flag))
                RemoveFlag(index, flag);
            else
                SetFlag(index, flag);
        }

        public bool HasFlag(object index, object flag)
        {
            if ((int)index >= valuesCount && !PrintIndexError(index, false))
                return false;

            return (GetUInt32Value(index) & Convert.ToUInt32(flag)) != 0;
        }

        public void ApplyModFlag<T>(object index, T flag, bool apply)
        {
            if (apply)
                SetFlag(index, flag);
            else
                RemoveFlag(index, flag);
        }

        public void SetByteFlag(object index, byte offset, object newFlag)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, true));

            if (offset > 4)
            {
                Log.outError(LogFilter.Server, "Object.SetByteFlag: wrong offset {0}", offset);
                return;
            }

            if (!Convert.ToBoolean((uint)UpdateData[(int)index] >> (offset * 8) & Convert.ToUInt32(newFlag)))
            {
                UpdateData[(int)index] = (uint)UpdateData[(int)index] | Convert.ToUInt32(newFlag) << (offset * 8);
                _changesMask.Set((int)index, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void RemoveByteFlag(object index, byte offset, object oldFlag)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, true));

            if (offset > 4)
            {
                Log.outError(LogFilter.Server, "Object.RemoveByteFlag: wrong offset {0}", offset);
                return;
            }

            if (Convert.ToBoolean((uint)UpdateData[(int)index] >> (offset * 8) & Convert.ToUInt32(oldFlag)))
            {
                UpdateData[(int)index] = (uint)UpdateData[(int)index] & ~(Convert.ToUInt32(oldFlag) << (offset * 8));
                _changesMask.Set((int)index, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void ToggleByteFlag(object index, byte offset, byte flag)
        {
            if (HasByteFlag(index, offset, flag))
                RemoveByteFlag(index, offset, flag);
            else
                SetByteFlag(index, offset, flag);
        }

        public bool HasByteFlag(object index, byte offset, object flag)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, false));
            Contract.Assert(offset < 4);
            return Convert.ToBoolean((uint)UpdateData[(int)index] >> (offset * 8) & Convert.ToUInt32(flag));
        }

        public void SetFlag64(object index, object newFlag)
        {
            ulong oldval = GetUInt64Value(index);
            ulong newval = oldval | Convert.ToUInt64(newFlag);
            SetUInt64Value(index, newval);
        }

        public void RemoveFlag64(object index, object oldFlag)
        {
            ulong oldval = GetUInt64Value(index);
            ulong newval = oldval & ~Convert.ToUInt64(oldFlag);
            SetUInt64Value(index, newval);
        }

        public void ToggleFlag64(object index, object flag)
        {
            if (HasFlag64(index, flag))
                RemoveFlag64(index, flag);
            else
                SetFlag64(index, flag);
        }

        public bool HasFlag64(object index, object flag)
        {
            Contract.Assert((int)index < valuesCount || PrintIndexError(index, false));
            return (GetUInt64Value(index) & Convert.ToUInt64(flag)) != 0;
        }

        void ApplyModFlag64(object index, ulong flag, bool apply)
        {
            if (apply)
                SetFlag64(index, flag);
            else
                RemoveFlag64(index, flag);
        }

        public Dictionary<int, uint> GetDynamicKeyAndValues(object index)
        {
            Contract.Assert((int)index < _dynamicValuesCount || PrintIndexError(index, false));
            return _dynamicValues[(int)index];
        }

        public ICollection<uint> GetDynamicValues(object index)
        {
            Contract.Assert((int)index < _dynamicValuesCount || PrintIndexError(index, false));
            return _dynamicValues[(int)index].Values;
        }

        public uint GetDynamicValue(object index, ushort offset)
        {
            Contract.Assert((int)index < _dynamicValuesCount || PrintIndexError(index, false));
            if (!_dynamicValues[(int)index].ContainsKey(offset))
                return 0;

            return _dynamicValues[(int)index][offset];
        }

        public void AddDynamicValue(object index, uint value)
        {
            Contract.Assert((int)index < _dynamicValuesCount || PrintIndexError(index, true));

            SetDynamicValue(index, (byte)_dynamicValues[(int)index].Count, value);
        }

        public void RemoveDynamicValue(object index, uint value)
        {
            Contract.Assert((int)index < _dynamicValuesCount || PrintIndexError(index, false));

            // TODO: Research if this is blizzlike to just set value to 0
            var values = _dynamicValues[(int)index];
            foreach (var pair in values.ToList())
            {
                if (pair.Value == value)
                {
                    values[pair.Key] = 0;
                    _dynamicChangesMask[(int)index] = DynamicFieldChangeType.ValueChanged;
                    _dynamicChangesArrayMask[(int)index].Set(pair.Key, true);

                    AddToObjectUpdateIfNeeded();
                }
            }
        }

        public void ClearDynamicValue(object index)
        {
            Contract.Assert((int)index < _dynamicValuesCount || PrintIndexError(index, false));

            if (!_dynamicValues[(int)index].Empty())
            {
                _dynamicValues[(int)index].Clear();
                _dynamicChangesMask[(int)index] = DynamicFieldChangeType.ValueAndSizeChanged;
                _dynamicChangesArrayMask[(int)index].SetAll(false);

                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetDynamicValue(object index, ushort offset, uint value)
        {
            Contract.Assert((int)index < _dynamicValuesCount || PrintIndexError(index, true));

            DynamicFieldChangeType changeType = DynamicFieldChangeType.ValueChanged;
            var values = _dynamicValues[(int)index];
            if (!values.ContainsKey(offset))
            {
                values.Add(offset, 0u);
                changeType = DynamicFieldChangeType.ValueAndSizeChanged;
            }

            if (_dynamicChangesArrayMask[(int)index].Count <= offset)
                _dynamicChangesArrayMask[(int)index].Length = offset + 1;

            if (values[offset] != value || changeType == DynamicFieldChangeType.ValueAndSizeChanged)
            {
                values[offset] = value;
                _dynamicChangesMask[(int)index] = changeType;
                _dynamicChangesArrayMask[(int)index].Set(offset, true);

                AddToObjectUpdateIfNeeded();
            }
        }

        public List<T> GetDynamicStructuredValues<T>(object index)
        {
            var values = _dynamicValues[(int)index].Values;
            return new List<T>(values.DeserializeObjects<T>());
        }

        public T GetDynamicStructuredValue<T>(object index, ushort offset)
        {
            return GetDynamicStructuredValues<T>(index)[offset];
        }

        public void SetDynamicStructuredValue<T>(object index, ushort offset, T value)
        {
            int BlockCount = Marshal.SizeOf<T>() / sizeof(uint);
            SetDynamicValue(index, (ushort)((offset + 1) * BlockCount - 1), 0); // reserve space

            for (ushort i = 0; i < BlockCount; ++i)
                SetDynamicValue(index, (ushort)(offset * BlockCount + i), Extensions.SerializeObject(value)[i]);
        }

        public void AddDynamicStructuredValue<T>(object index, T value)
        {
            int BlockCount = Marshal.SizeOf<T>() / sizeof(uint);
            var values = _dynamicValues[(int)index];
            ushort offset = (ushort)(values.Count / BlockCount);
            SetDynamicValue(index, (ushort)((offset + 1) * BlockCount - 1), 0); // reserve space
            for (ushort i = 0; i < BlockCount; ++i)
                SetDynamicValue(index, (ushort)(offset * BlockCount + i), Extensions.SerializeObject(value)[i]);
        }

        bool PrintIndexError(object index, bool set)
        {
            Log.outError(LogFilter.Server, "Attempt {0} non-existed value field: {1} (count: {2}) for object typeid: {3} type mask: {4}", (set ? "set value to" : "get value from"), index, valuesCount, GetTypeId(), objectTypeId);

            // ASSERT must fail after function call
            return false;
        }

        public bool IsWorldObject()
        {
            if (m_isWorldObject)
                return true;

            if (IsTypeId(TypeId.Unit) && ToCreature().m_isTempWorldObject)
                return true;

            return false;
        }
        public void SetWorldObject(bool on)
        {
            if (!IsInWorld)
                return;

            GetMap().AddObjectToSwitchList(this, on);
        }
        public void setActive(bool on)
        {
            if (m_isActive == on)
                return;

            if (IsTypeId(TypeId.Player))
                return;

            m_isActive = on;

            if (!IsInWorld)
                return;

            Map map = GetMap();
            if (map == null)
                return;

            if (on)
            {
                if (IsTypeId(TypeId.Unit))
                    map.AddToActive(ToCreature());
                else if (IsTypeId(TypeId.DynamicObject))
                    map.AddToActive(ToDynamicObject());
            }
            else
            {
                if (IsTypeId(TypeId.Unit))
                    map.RemoveFromActive(ToCreature());
                else if (IsTypeId(TypeId.DynamicObject))
                    map.RemoveFromActive(ToDynamicObject());
            }
        }

        public virtual void CleanupsBeforeDelete(bool finalCleanup = true)
        {
            if (IsInWorld)
                RemoveFromWorld();

            Transport transport = GetTransport();
            if (transport)
                transport.RemovePassenger(this);
        }

        public uint GetZoneId()
        {
            return GetMap().GetZoneId(GetPositionX(), GetPositionY(), GetPositionZ());
        }

        public uint GetAreaId()
        {
            return GetMap().GetAreaId(GetPositionX(), GetPositionY(), GetPositionZ());
        }

        public void GetZoneAndAreaId(out uint zoneid, out uint areaid)
        {
            GetMap().GetZoneAndAreaId(out zoneid, out areaid, posX, posY, posZ);
        }

        public InstanceScript GetInstanceScript()
        {
            Map map = GetMap();
            return map.IsDungeon() ? ((InstanceMap)map).GetInstanceScript() : null;
        }

        public float GetGridActivationRange()
        {
            if (IsTypeId(TypeId.Player))
            {
                if (ToPlayer().GetCinematicMgr().IsOnCinematic())
                    return SharedConst.DefaultVisibilityInstance;

                return GetMap().GetVisibilityRange();
            }
            else if (IsTypeId(TypeId.Unit))
                return ToCreature().m_SightDistance;
            else if (IsTypeId(TypeId.DynamicObject))
            {
                if (isActiveObject())
                    return GetMap().GetVisibilityRange();
                else
                    return 0.0f;
            }
            else
                return 0.0f;
        }

        public float GetVisibilityRange()
        {
            if (isActiveObject() && !IsTypeId(TypeId.Player))
                return SharedConst.MaxVisibilityDistance;
            else
                return GetMap().GetVisibilityRange();
        }

        public float GetSightRange(WorldObject target = null)
        {
            if (IsTypeId(TypeId.Player) || IsTypeId(TypeId.Unit))
            {
                if (IsTypeId(TypeId.Player))
                {
                    if (target != null && target.isActiveObject() && target.ToPlayer() == null)
                        return SharedConst.MaxVisibilityDistance;
                    else if (ToPlayer().GetCinematicMgr().IsOnCinematic())
                        return SharedConst.DefaultVisibilityInstance;
                    else
                        return GetMap().GetVisibilityRange();
                }
                else if (IsTypeId(TypeId.Unit))
                    return ToCreature().m_SightDistance;
                else
                    return SharedConst.SightRangeUnit;
            }

            if (IsTypeId(TypeId.DynamicObject) && isActiveObject())
            {
                return GetMap().GetVisibilityRange();
            }

            return 0.0f;
        }

        public bool CanSeeOrDetect(WorldObject obj, bool ignoreStealth = false, bool distanceCheck = false, bool checkAlert = false)
        {
            if (this == obj)
                return true;

            if (obj.IsNeverVisibleFor(this) || CanNeverSee(obj))
                return false;

            if (obj.IsAlwaysVisibleFor(this) || CanAlwaysSee(obj))
                return true;

            bool corpseVisibility = false;
            if (distanceCheck)
            {
                bool corpseCheck = false;
                Player thisPlayer = ToPlayer();
                if (thisPlayer != null)
                {
                    if (thisPlayer.IsDead() && thisPlayer.GetHealth() > 0 && // Cheap way to check for ghost state
                    !Convert.ToBoolean(obj.m_serverSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & m_serverSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & (uint)GhostVisibilityType.Ghost))
                    {
                        Corpse corpse = thisPlayer.GetCorpse();
                        if (corpse != null)
                        {
                            corpseCheck = true;
                            if (corpse.IsWithinDist(thisPlayer, GetSightRange(obj), false))
                                if (corpse.IsWithinDist(obj, GetSightRange(obj), false))
                                    corpseVisibility = true;
                        }
                    }
                }

                WorldObject viewpoint = this;
                Player player = ToPlayer();
                if (player != null)
                    viewpoint = player.GetViewpoint();

                if (viewpoint == null)
                    viewpoint = this;

                if (!corpseCheck && !viewpoint.IsWithinDist(obj, GetSightRange(obj), false))
                    return false;
            }

            // GM visibility off or hidden NPC
            if (obj.m_serverSideVisibility.GetValue(ServerSideVisibilityType.GM) == 0)
            {
                // Stop checking other things for GMs
                if (m_serverSideVisibilityDetect.GetValue(ServerSideVisibilityType.GM) != 0)
                    return true;
            }
            else
                return m_serverSideVisibilityDetect.GetValue(ServerSideVisibilityType.GM) >= obj.m_serverSideVisibility.GetValue(ServerSideVisibilityType.GM);

            // Ghost players, Spirit Healers, and some other NPCs
            if (!corpseVisibility && !Convert.ToBoolean(obj.m_serverSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & m_serverSideVisibilityDetect.GetValue(ServerSideVisibilityType.Ghost)))
            {
                // Alive players can see dead players in some cases, but other objects can't do that
                Player thisPlayer = ToPlayer();
                if (thisPlayer != null)
                {
                    Player objPlayer = obj.ToPlayer();
                    if (objPlayer != null)
                    {
                        if (thisPlayer.GetTeam() != objPlayer.GetTeam() || !thisPlayer.IsGroupVisibleFor(objPlayer))
                            return false;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }

            if (obj.IsInvisibleDueToDespawn())
                return false;

            if (!CanDetect(obj, ignoreStealth, checkAlert))
                return false;

            return true;
        }

        bool CanNeverSee(WorldObject obj)
        {
            return GetMap() != obj.GetMap() || !IsInPhase(obj);
        }

        public virtual bool CanAlwaysSee(WorldObject obj) { return false; }

        bool CanDetect(WorldObject obj, bool ignoreStealth, bool checkAlert = false)
        {
            WorldObject seer = this;

            // Pets don't have detection, they use the detection of their masters
            Unit thisUnit = ToUnit();
            if (thisUnit != null)
            {
                Unit controller = thisUnit.GetCharmerOrOwner();
                if (controller != null)
                    seer = controller;
            }

            if (obj.IsAlwaysDetectableFor(seer))
                return true;

            if (!ignoreStealth && !seer.CanDetectInvisibilityOf(obj))
                return false;

            if (!ignoreStealth && !seer.CanDetectStealthOf(obj, checkAlert))
                return false;

            return true;
        }

        bool CanDetectInvisibilityOf(WorldObject obj)
        {
            uint mask = obj.m_invisibility.GetFlags() & m_invisibilityDetect.GetFlags();

            // Check for not detected types
            if (mask != obj.m_invisibility.GetFlags())
                return false;

            for (int i = 0; i < (int)InvisibilityType.Max; ++i)
            {
                if (!Convert.ToBoolean(mask & (1 << i)))
                    continue;

                int objInvisibilityValue = obj.m_invisibility.GetValue((InvisibilityType)i);
                int ownInvisibilityDetectValue = m_invisibilityDetect.GetValue((InvisibilityType)i);

                // Too low value to detect
                if (ownInvisibilityDetectValue < objInvisibilityValue)
                    return false;
            }

            return true;
        }

        bool CanDetectStealthOf(WorldObject obj, bool checkAlert = false)
        {
            // Combat reach is the minimal distance (both in front and behind),
            //   and it is also used in the range calculation.
            // One stealth point increases the visibility range by 0.3 yard.

            if (obj.m_stealth.GetFlags() == 0)
                return true;

            float distance = GetExactDist(obj);
            float combatReach = 0.0f;

            Unit unit = ToUnit();
            if (unit != null)
                combatReach = unit.GetCombatReach();

            if (distance < combatReach)
                return true;

            if (!HasInArc(MathFunctions.PI, obj))
                return false;

            GameObject go = ToGameObject();
            for (int i = 0; i < (int)StealthType.Max; ++i)
            {
                if (!Convert.ToBoolean(obj.m_stealth.GetFlags() & (1 << i)))
                    continue;

                if (unit != null && unit.HasAuraTypeWithMiscvalue(AuraType.DetectStealth, i))
                    return true;

                // Starting points
                int detectionValue = 30;

                // Level difference: 5 point / level, starting from level 1.
                // There may be spells for this and the starting points too, but
                // not in the DBCs of the client.
                detectionValue += (int)(GetLevelForTarget(obj) - 1) * 5;

                // Apply modifiers
                detectionValue += m_stealthDetect.GetValue((StealthType)i);
                if (go != null)
                {
                    Unit owner = go.GetOwner();
                    if (owner != null)
                        detectionValue -= (int)(owner.GetLevelForTarget(this) - 1) * 5;
                }

                detectionValue -= obj.m_stealth.GetValue((StealthType)i);

                // Calculate max distance
                float visibilityRange = detectionValue * 0.3f + combatReach;

                // If this unit is an NPC then player detect range doesn't apply
                if (unit && unit.IsTypeId(TypeId.Player) && visibilityRange > SharedConst.MaxPlayerStealthDetectRange)
                    visibilityRange = SharedConst.MaxPlayerStealthDetectRange;

                // When checking for alert state, look 8% further, and then 1.5 yards more than that.
                if (checkAlert)
                    visibilityRange += (visibilityRange * 0.08f) + 1.5f;

                // If checking for alert, and creature's visibility range is greater than aggro distance, No alert
                Unit tunit = obj.ToUnit();
                if (checkAlert && unit && unit.ToCreature() && visibilityRange >= unit.ToCreature().GetAttackDistance(tunit) + unit.ToCreature().m_CombatDistance)
                    return false;

                if (distance > visibilityRange)
                    return false;
            }

            return true;
        }

        public void ForceValuesUpdateAtIndex(object _index)
        {
            int index = (int)_index;
            _changesMask.Set(index, true);

            AddToObjectUpdateIfNeeded();
        }

        public virtual void SendMessageToSet(ServerPacket packet, bool self)
        {
            if (IsInWorld)
                SendMessageToSetInRange(packet, GetVisibilityRange(), self);
        }

        public virtual void SendMessageToSetInRange(ServerPacket data, float dist, bool self)
        {
            MessageDistDeliverer notifier = new MessageDistDeliverer(this, data, dist);
            Cell.VisitWorldObjects(this, notifier, dist);
        }

        public virtual void SendMessageToSet(ServerPacket data, Player skip)
        {
            var notifier = new MessageDistDeliverer(this, data, GetVisibilityRange(), false, skip);
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
        }

        public virtual void SetMap(Map map)
        {
            Contract.Assert(map != null);
            Contract.Assert(!IsInWorld);

            if (_currMap == map)
                return;

            _currMap = map;
            SetMapId(map.GetId());
            instanceId = map.GetInstanceId();
            if (IsWorldObject())
                _currMap.AddWorldObject(this);
        }

        public virtual void ResetMap()
        {
            Contract.Assert(_currMap != null);
            Contract.Assert(!IsInWorld);
            if (IsWorldObject())
                _currMap.RemoveWorldObject(this);
            _currMap = null;
        }

        public Map GetMap() { return _currMap; }

        public void AddObjectToRemoveList()
        {
            Map map = GetMap();
            if (map == null)
            {
                Log.outError(LogFilter.Server, "Object (TypeId: {0} Entry: {1} GUID: {2}) at attempt add to move list not have valid map (Id: {3}).", GetTypeId(), GetEntry(), GetGUID().ToString(), GetMapId());
                return;
            }

            map.AddObjectToRemoveList(this);
        }

        public void SetZoneScript()
        {
            Map map = GetMap();
            if (map != null)
            {
                if (map.IsDungeon())
                    m_zoneScript = ((InstanceMap)map).GetInstanceScript();
                else if (!map.IsBattlegroundOrArena())
                {
                    BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(GetZoneId());
                    if (bf != null)
                        m_zoneScript = bf;
                    else
                        m_zoneScript = Global.OutdoorPvPMgr.GetZoneScript(GetZoneId());
                }
            }
        }

        public Scenario GetScenario()
        {
            if (IsInWorld)
            {
                InstanceMap instanceMap = GetMap().ToInstanceMap();
                if (instanceMap != null)
                    return instanceMap.GetInstanceScenario();
            }

            return null;
        }

        public TempSummon SummonCreature(uint id, float x, float y, float z, float ang = 0, TempSummonType spwtype = TempSummonType.ManualDespawn, uint despwtime = 0)
        {
            if (x == 0.0f && y == 0.0f && z == 0.0f)
            {
                GetClosePoint(out x, out y, out z, GetObjectSize());
                ang = GetOrientation();
            }
            Position pos = new Position();
            pos.Relocate(x, y, z, ang);
            return SummonCreature(id, pos, spwtype, despwtime, 0);
        }

        public TempSummon SummonCreature(uint entry, Position pos, TempSummonType spwtype = TempSummonType.ManualDespawn, uint duration = 0, uint vehId = 0)
        {
            Map map = GetMap();
            if (map != null)
            {
                TempSummon summon = map.SummonCreature(entry, pos, null, duration, isTypeMask(TypeMask.Unit) ? ToUnit() : null);
                if (summon != null)
                {
                    summon.SetTempSummonType(spwtype);
                    return summon;
                }
            }

            return null;
        }

        public GameObject SummonGameObject(uint entry, float x, float y, float z, float ang, Quaternion rotation, uint respawnTime)
        {
            if (x == 0 && y == 0 && z == 0)
            {
                GetClosePoint(out x, out y, out z, GetObjectSize());
                ang = GetOrientation();
            }

            Position pos = new Position(x, y, z, ang);
            return SummonGameObject(entry, pos, rotation, respawnTime);
        }

        public GameObject SummonGameObject(uint entry, Position pos, Quaternion rotation, uint respawnTime)
        {
            if (!IsInWorld)
                return null;

            GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);
            if (goinfo == null)
            {
                Log.outError(LogFilter.Sql, "Gameobject template {0} not found in database!", entry);
                return null;
            }

            Map map = GetMap();
            GameObject go = new GameObject();
            if (!go.Create(entry, map, GetPhaseMask(), pos,  rotation, 255, GameObjectState.Ready))
                return null;

            go.CopyPhaseFrom(this);

            go.SetRespawnTime((int)respawnTime);
            if (IsTypeId(TypeId.Player) || IsTypeId(TypeId.Unit)) //not sure how to handle this
                ToUnit().AddGameObject(go);
            else
                go.SetSpawnedByDefault(false);

            map.AddToMap(go);
            return go;
        }

        public Creature SummonTrigger(float x, float y, float z, float ang, uint duration, CreatureAI AI = null)
        {
            TempSummonType summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;
            Creature summon = SummonCreature(SharedConst.WorldTrigger, x, y, z, ang, summonType, duration);
            if (summon == null)
                return null;

            if (IsTypeId(TypeId.Player) || IsTypeId(TypeId.Unit))
            {
                summon.SetFaction(ToUnit().getFaction());
                summon.SetLevel(ToUnit().getLevel());
            }

            if (AI != null)
                summon.InitializeAI(new CreatureAI(summon));
            return summon;
        }

        public void SummonCreatureGroup(byte group, out List<TempSummon> list)
        {
            Contract.Assert((IsTypeId(TypeId.GameObject) || IsTypeId(TypeId.Unit)), "Only GOs and creatures can summon npc groups!");
            list = new List<TempSummon>();
            var data = Global.ObjectMgr.GetSummonGroup(GetEntry(), IsTypeId(TypeId.GameObject) ? SummonerType.GameObject : SummonerType.Creature, group);
            if (data.Empty())
            {
                Log.outWarn(LogFilter.Scripts, "{0} ({1}) tried to summon non-existing summon group {2}.", GetName(), GetGUID().ToString(), group);
                return;
            }

            foreach (var tempSummonData in data)
            {
                TempSummon summon = SummonCreature(tempSummonData.entry, tempSummonData.pos, tempSummonData.type, tempSummonData.time);
                if (summon)
                    list.Add(summon);
            }
        }

        public Creature FindNearestCreature(uint entry, float range, bool alive = true)
        {
            var checker = new NearestCreatureEntryWithLiveStateInObjectRangeCheck(this, entry, alive, range);
            var searcher = new CreatureLastSearcher(this, checker);

            Cell.VisitAllObjects(this, searcher, range);
            return searcher.GetTarget();
        }

        public GameObject FindNearestGameObject(uint entry, float range)
        {
            var checker = new NearestGameObjectEntryInObjectRangeCheck(this, entry, range);
            var searcher = new GameObjectLastSearcher(this, checker);

            Cell.VisitGridObjects(this, searcher, range);
            return searcher.GetTarget();
        }

        public GameObject FindNearestGameObjectOfType(GameObjectTypes type, float range)
        {
            var checker = new NearestGameObjectTypeInObjectRangeCheck(this, type, range);
            var searcher = new GameObjectLastSearcher(this, checker);

            Cell.VisitGridObjects(this, searcher, range);
            return searcher.GetTarget();
        }

        public void GetGameObjectListWithEntryInGrid(List<GameObject> gameobjectList, uint entry = 0, float maxSearchRange = 250.0f)
        {
            var check = new AllGameObjectsWithEntryInRange(this, entry, maxSearchRange);
            var searcher = new GameObjectListSearcher(this, gameobjectList, check);

            Cell.VisitGridObjects(this, searcher, maxSearchRange);
        }

        public void GetCreatureListWithEntryInGrid(List<Creature> creatureList, uint entry = 0, float maxSearchRange = 250.0f)
        {
            var check = new AllCreaturesOfEntryInRange(this, entry, maxSearchRange);
            var searcher = new CreatureListSearcher(this, creatureList, check);

            Cell.VisitGridObjects(this, searcher, maxSearchRange);
        }

        public List<Player> GetPlayerListInGrid(float maxSearchRange)
        {
            List<Player> playerList = new List<Player>();
            var checker = new AnyPlayerInObjectRangeCheck(this, maxSearchRange);
            var searcher = new PlayerListSearcher(this, playerList, checker);

            Cell.VisitWorldObjects(this, searcher, maxSearchRange);
            return playerList;
        }

        public float GetObjectSize()
        {
            return (valuesCount > (uint)UnitFields.CombatReach) ? GetFloatValue(UnitFields.CombatReach) : SharedConst.DefaultWorldObjectSize;
        }

        public virtual void SetPhaseMask(uint newPhaseMask, bool update)
        {
            phaseMask = newPhaseMask;

            if (update && IsInWorld)
                UpdateObjectVisibility();
        }

        bool HasInPhaseList(uint phase)
        {
            return _phases.Contains(phase);
        }

        /// <summary>
        /// Updates Area based phases, does not remove phases from auras
        /// Phases from gm commands are not taken into calculations, they can be lost!!
        /// </summary>
        public void UpdateAreaAndZonePhase()
        {
            bool updateNeeded = false;
            var phases = Global.ObjectMgr.GetAreaAndZonePhases();
            foreach (var areaOrZoneId in phases.Keys)
            {
                foreach (var phase in phases[areaOrZoneId])
                {
                    if (areaOrZoneId == GetAreaId() || areaOrZoneId == GetZoneId())
                    {
                        if (Global.ConditionMgr.IsObjectMeetToConditions(this, phase.Conditions))
                        {
                            // add new phase if condition passed, true if it wasnt added before
                            bool up = SetInPhase(phase.Id, false, true);
                            if (!updateNeeded && up)
                                updateNeeded = true;
                        }
                        else
                        {
                            // condition failed, remove phase, true if there was something removed
                            bool up = SetInPhase(phase.Id, false, false);
                            if (!updateNeeded && up)
                                updateNeeded = true;
                        }
                    }
                    else
                    {
                        // not in area, remove phase, true if there was something removed
                        bool up = SetInPhase(phase.Id, false, false);
                        if (!updateNeeded && up)
                            updateNeeded = true;
                    }
                }
            }

            // do not remove a phase if it would be removed by an area but we have the same phase from an aura
            Unit unit = ToUnit();
            if (unit)
            {
                var auraPhaseList = unit.GetAuraEffectsByType(AuraType.Phase);
                foreach (var eff in auraPhaseList)
                {
                    uint phase = (uint)eff.GetMiscValueB();
                    bool up = SetInPhase(phase, false, true);
                    if (!updateNeeded && up)
                        updateNeeded = true;
                }
                var auraPhaseGroupList = unit.GetAuraEffectsByType(AuraType.PhaseGroup);
                foreach (var eff in auraPhaseGroupList)
                {
                    bool up = false;
                    uint phaseGroup = (uint)eff.GetMiscValueB();
                    foreach (uint phase in Global.DB2Mgr.GetPhasesForGroup(phaseGroup))
                        up = SetInPhase(phase, false, true);

                    if (!updateNeeded && up)
                        updateNeeded = true;
                }
            }

            // only update visibility and send packets if there was a change in the phase list

            if (updateNeeded && IsTypeId(TypeId.Player) && IsInWorld)
                ToPlayer().GetSession().SendSetPhaseShift(GetPhases(), GetTerrainSwaps(), GetWorldMapAreaSwaps());

            // only update visibilty once, to prevent objects appearing for a moment while adding in multiple phases
            if (updateNeeded && IsInWorld)
                UpdateObjectVisibility();
        }

        public virtual bool SetInPhase(uint id, bool update, bool apply)
        {
            if (id != 0)
            {
                if (apply)
                {
                    if (HasInPhaseList(id)) // do not run the updates if we are already in this phase
                        return false;

                    _phases.Add(id);
                }
                else
                {
                    // if area phase passes the condition we should not remove it (ie: if remove called from aura remove)
                    // this however breaks the .mod phase command, you wont be able to remove any area based phases with it
                    var phases = Global.ObjectMgr.GetPhasesForArea(GetAreaId());
                    if (phases != null)
                    {
                        foreach (PhaseInfoStruct phase in phases)
                        {
                            if (id == phase.Id)
                                if (Global.ConditionMgr.IsObjectMeetToConditions(this, phase.Conditions))
                                    return false;
                        }
                    }

                    if (!HasInPhaseList(id)) // do not run the updates if we are not in this phase
                        return false;
                    _phases.Remove(id);
                }
            }
            RebuildTerrainSwaps();

            if (update && IsInWorld)
                UpdateObjectVisibility();
            return true;
        }

        public void CopyPhaseFrom(WorldObject obj, bool update = false)
        {
            if (!obj)
                return;

            foreach (uint phase in obj.GetPhases())
                SetInPhase(phase, false, true);

            if (update && IsInWorld)
                UpdateObjectVisibility();
        }

        public void ClearPhases(bool update = false)
        {
            _phases.Clear();

            RebuildTerrainSwaps();

            if (update && IsInWorld)
                UpdateObjectVisibility();
        }

        public bool IsInPhase(uint phase) { return _phases.Contains(phase); }

        public bool IsInPhase(List<uint> phases)
        {
            // PhaseId 169 is the default fallback phase
            if (_phases.Empty() && phases.Empty())
                return true;

            if (_phases.Empty() && phases.Contains(169))
                return true;

            if (phases.Empty() && _phases.Contains(169))
                return true;

            return _phases.Intersect(phases).Any();
        }

        public bool IsInPhase(WorldObject obj)
        {
            // PhaseId 169 is the default fallback phase
            if (_phases.Empty() && obj.GetPhases().Empty())
                return true;

            if (_phases.Empty() && obj.IsInPhase(169))
                return true;

            if (obj.GetPhases().Empty() && IsInPhase(169))
                return true;

            if (IsTypeId(TypeId.Player) && ToPlayer().IsGameMaster())
                return true;

            return IsInPhase(obj.GetPhases());
        }

        public bool IsInTerrainSwap(uint terrainSwap) { return _terrainSwaps.Contains(terrainSwap); }

        public void RebuildTerrainSwaps()
        {
            // Clear all terrain swaps, will be rebuilt below
            // Reason for this is, multiple phases can have the same terrain swap, we should not remove the swap if another phase still use it
            _terrainSwaps.Clear();

            // Check all applied phases for terrain swap and add it only once
            foreach (uint phaseId in _phases)
            {
                var swaps = Global.ObjectMgr.GetPhaseTerrainSwaps(phaseId);
                foreach (uint swap in swaps)
                {
                    // only add terrain swaps for current map
                    MapRecord mapEntry = CliDB.MapStorage.LookupByKey(swap);
                    if (mapEntry == null || mapEntry.ParentMapID != GetMapId())
                        continue;

                    if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, swap, this))
                        _terrainSwaps.Add(swap);
                }
            }

            // get default terrain swaps, only for current map always
            var mapSwaps = Global.ObjectMgr.GetDefaultTerrainSwaps(GetMapId());
            foreach (uint swap in mapSwaps)
            {
                if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, swap, this))
                    _terrainSwaps.Add(swap);
            }

            // online players have a game client with world map display
            if (IsTypeId(TypeId.Player))
                RebuildWorldMapAreaSwaps();
        }

        void RebuildWorldMapAreaSwaps()
        {
            // Clear all world map area swaps, will be rebuilt below
            _worldMapAreaSwaps.Clear();

            // get ALL default terrain swaps, if we are using it (condition is true) 
            // send the worldmaparea for it, to see swapped worldmaparea in client from other maps too, not just from our current
            var defaults = Global.ObjectMgr.GetDefaultTerrainSwapStore();
            foreach (uint swap in defaults.Values)
            {
                var uiMapSwaps = Global.ObjectMgr.GetTerrainWorldMaps(swap);
                if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, swap, this))
                {
                    foreach (uint worldMapAreaId in uiMapSwaps)
                        _worldMapAreaSwaps.Add(worldMapAreaId);
                }
            }

            // Check all applied phases for world map area swaps
            foreach (uint phaseId in _phases)
            {
                var swaps = Global.ObjectMgr.GetPhaseTerrainSwaps(phaseId);
                foreach (uint swap in swaps)
                {
                    var uiMapSwaps = Global.ObjectMgr.GetTerrainWorldMaps(swap);
                    if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, swap, this))
                        foreach (uint worldMapAreaId in uiMapSwaps)
                            _worldMapAreaSwaps.Add(worldMapAreaId);
                }
            }
        }

        public uint GetPhaseMask() { return phaseMask; }
        public List<uint> GetPhases() { return _phases; }
        public List<uint> GetTerrainSwaps() { return _terrainSwaps; }
        public List<uint> GetWorldMapAreaSwaps() { return _worldMapAreaSwaps; }
        public int GetDBPhase() { return _dbPhase; }

        // if negative it is used as PhaseGroupId
        public void SetDBPhase(int p) { _dbPhase = p; }

        public void PlayDistanceSound(uint soundId, Player target = null)
        {
            PlaySpeakerBoxSound playSpeakerBoxSound = new PlaySpeakerBoxSound(GetGUID(), soundId);
            if (target != null)
                target.SendPacket(playSpeakerBoxSound);
            else
                SendMessageToSet(playSpeakerBoxSound, true);
        }

        public void PlayDirectSound(uint soundId, Player target = null)
        {
            PlaySound sound = new PlaySound(GetGUID(), soundId);
            if (target)
                target.SendPacket(sound);
            else
                SendMessageToSet(sound, true);
        }

        public void DestroyForNearbyPlayers()
        {
            if (!IsInWorld)
                return;
            List<Player> targets = new List<Player>();
            var check = new AnyPlayerInObjectRangeCheck(this, GetVisibilityRange(), false);
            var searcher = new PlayerListSearcher(this, targets, check);

            Cell.VisitWorldObjects(this, searcher, GetVisibilityRange());
            foreach (var player in targets)
            {
                if (player == this)
                    continue;

                if (!player.HaveAtClient(this))
                    continue;

                if (isTypeMask(TypeMask.Unit) && (ToUnit().GetCharmerGUID() == player.GetGUID()))// @todo this is for puppet
                    continue;

                DestroyForPlayer(player);
                player.m_clientGUIDs.Remove(GetGUID());
            }
        }

        public virtual void UpdateObjectVisibility(bool force = true)
        {
            //updates object's visibility for nearby players
            var notifier = new VisibleChangesNotifier(this);
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
        }

        public virtual void UpdateObjectVisibilityOnCreate()
        {
            UpdateObjectVisibility(true);
        }

        public virtual void BuildUpdate(Dictionary<Player, UpdateData> data)
        {
            var notifier = new WorldObjectChangeAccumulator(this, data);
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());

            ClearUpdateMask(false);
        }

        public virtual void AddToObjectUpdate()
        {
            GetMap().AddUpdateObject(this);
        }

        public virtual void RemoveFromObjectUpdate()
        {
            GetMap().RemoveUpdateObject(this);
        }

        public uint GetInstanceId() { return instanceId; }

        public virtual ushort GetAIAnimKitId() { return 0; }
        public virtual ushort GetMovementAnimKitId() { return 0; }
        public virtual ushort GetMeleeAnimKitId() { return 0; }

        public virtual string GetName(LocaleConstant locale_idx = LocaleConstant.enUS) { return _name; }
        public void SetName(string name) { _name = name; }

        public ObjectGuid GetGUID() { return GetGuidValue(ObjectFields.Guid); }
        public uint GetEntry() { return GetUInt32Value(ObjectFields.Entry); }
        public void SetEntry(uint entry) { SetUInt32Value(ObjectFields.Entry, entry); }

        public float GetObjectScale() { return GetFloatValue(ObjectFields.ScaleX); }
        public virtual void SetObjectScale(float scale) { SetFloatValue(ObjectFields.ScaleX, scale); }

        public TypeId GetTypeId() { return objectTypeId; }
        public bool IsTypeId(TypeId typeId) { return GetTypeId() == typeId; }
        public bool isTypeMask(TypeMask mask) { return Convert.ToBoolean(mask & objectTypeMask); }

        public virtual bool hasQuest(uint questId) { return false; }
        public virtual bool hasInvolvedQuest(uint questId) { return false; }

        public void SetFieldNotifyFlag(uint flag) { _fieldNotifyFlags |= flag; }
        public void RemoveFieldNotifyFlag(uint flag) { _fieldNotifyFlags &= ~flag; }

        public Creature ToCreature() { return IsTypeId(TypeId.Unit) ? (this as Creature) : null; }
        public Player ToPlayer() { return IsTypeId(TypeId.Player) ? (this as Player) : null; }
        public GameObject ToGameObject() { return IsTypeId(TypeId.GameObject) ? (this as GameObject) : null; }
        public Unit ToUnit() { return IsTypeId(TypeId.Unit) || IsTypeId(TypeId.Player) ? (this as Unit) : null; }
        public Corpse ToCorpse() { return IsTypeId(TypeId.Corpse) ? (this as Corpse) : null; }
        public DynamicObject ToDynamicObject() { return IsTypeId(TypeId.DynamicObject) ? (this as DynamicObject) : null; }
        public AreaTrigger ToAreaTrigger() { return IsTypeId(TypeId.AreaTrigger) ? (this as AreaTrigger) : null; }
        public Conversation ToConversation() { return IsTypeId(TypeId.Conversation) ? (this as Conversation) : null; }

        public virtual void Update(uint diff) { }

        public virtual uint GetLevelForTarget(WorldObject target) { return 1; }

        public virtual void SaveRespawnTime() { }

        public ZoneScript GetZoneScript() { return m_zoneScript; }

        public void AddToNotify(NotifyFlags f) { m_notifyflags |= f; }
        public bool isNeedNotify(NotifyFlags f) { return Convert.ToBoolean(m_notifyflags & f); }
        NotifyFlags GetNotifyFlags() { return m_notifyflags; }
        bool NotifyExecuted(NotifyFlags f) { return Convert.ToBoolean(m_executed_notifies & f); }
        void SetNotified(NotifyFlags f) { m_executed_notifies |= f; }
        public void ResetAllNotifies() { m_notifyflags = 0; m_executed_notifies = 0; }

        public bool isActiveObject() { return m_isActive; }
        public bool IsPermanentWorldObject() { return m_isWorldObject; }

        public Transport GetTransport() { return m_transport; }
        public float GetTransOffsetX() { return m_movementInfo.transport.pos.GetPositionX(); }
        public float GetTransOffsetY() { return m_movementInfo.transport.pos.GetPositionY(); }
        public float GetTransOffsetZ() { return m_movementInfo.transport.pos.GetPositionZ(); }
        public float GetTransOffsetO() { return m_movementInfo.transport.pos.GetOrientation(); }
        Position GetTransOffset() { return m_movementInfo.transport.pos; }
        public uint GetTransTime() { return m_movementInfo.transport.time; }
        public sbyte GetTransSeat() { return m_movementInfo.transport.seat; }
        public virtual ObjectGuid GetTransGUID()
        {
            if (GetTransport())
                return GetTransport().GetGUID();

            return ObjectGuid.Empty;
        }
        public void SetTransport(Transport t) { m_transport = t; }

        public virtual float GetStationaryX() { return GetPositionX(); }
        public virtual float GetStationaryY() { return GetPositionY(); }
        public virtual float GetStationaryZ() { return GetPositionZ(); }
        public virtual float GetStationaryO() { return GetOrientation(); }

        public virtual bool IsNeverVisibleFor(WorldObject seer) { return !IsInWorld; }
        public virtual bool IsAlwaysVisibleFor(WorldObject seer) { return false; }
        public virtual bool IsInvisibleDueToDespawn() { return false; }
        public virtual bool IsAlwaysDetectableFor(WorldObject seer) { return false; }

        public virtual bool LoadFromDB(ulong guid, Map map) { return true; }

        //Position
        public bool HasInLine(WorldObject target, float width)
        {
            if (!HasInArc(MathFunctions.PI, target))
                return false;
            width += target.GetObjectSize();
            float angle = GetRelativeAngle(target);
            return Math.Abs(Math.Sin(angle)) * GetExactDist2d(target.GetPositionX(), target.GetPositionY()) < width;
        }

        public float GetDistanceZ(WorldObject obj)
        {
            float dz = Math.Abs(GetPositionZ() - obj.GetPositionZ());
            float sizefactor = GetObjectSize() + obj.GetObjectSize();
            float dist = dz - sizefactor;
            return (dist > 0 ? dist : 0);
        }

        public virtual bool _IsWithinDist(WorldObject obj, float dist2compare, bool is3D)
        {
            float sizefactor = GetObjectSize() + obj.GetObjectSize();
            float maxdist = dist2compare + sizefactor;

            if (GetTransport() && obj.GetTransport() != null && obj.GetTransport().GetGUID() == GetTransport().GetGUID())
            {
                float dtx = m_movementInfo.transport.pos.posX - obj.m_movementInfo.transport.pos.posX;
                float dty = m_movementInfo.transport.pos.posY - obj.m_movementInfo.transport.pos.posY;
                float disttsq = dtx * dtx + dty * dty;
                if (is3D)
                {
                    float dtz = m_movementInfo.transport.pos.posZ - obj.m_movementInfo.transport.pos.posZ;
                    disttsq += dtz * dtz;
                }
                return disttsq < (maxdist * maxdist);
            }

            float dx = GetPositionX() - obj.GetPositionX();
            float dy = GetPositionY() - obj.GetPositionY();
            float distsq = dx * dx + dy * dy;
            if (is3D)
            {
                float dz = GetPositionZ() - obj.GetPositionZ();
                distsq += dz * dz;
            }

            return distsq < maxdist * maxdist;
        }

        public bool IsWithinLOSInMap(WorldObject obj)
        {
            if (!IsInMap(obj))
                return false;

            float x, y, z;
            if (obj.IsTypeId(TypeId.Player))
                obj.GetPosition(out x, out y, out z);
            else
                obj.GetHitSpherePointFor(GetPosition(), out x, out y, out z);

            return IsWithinLOS(x, y, z);
        }

        public float GetDistance(WorldObject obj)
        {
            float d = GetExactDist(obj.GetPosition()) - GetObjectSize() - obj.GetObjectSize();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance(Position pos)
        {
            float d = GetExactDist(pos) - GetObjectSize();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance(float x, float y, float z)
        {
            float d = GetExactDist(x, y, z) - GetObjectSize();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance2d(WorldObject obj)
        {
            float d = GetExactDist2d(obj.GetPosition()) - GetObjectSize() - obj.GetObjectSize();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance2d(float x, float y)
        {
            float d = GetExactDist2d(x, y) - GetObjectSize();
            return d > 0.0f ? d : 0.0f;
        }

        public bool IsSelfOrInSameMap(WorldObject obj)
        {
            if (this == obj)
                return true;
            return IsInMap(obj);
        }

        public bool IsInMap(WorldObject obj)
        {
            var m = GetMap().GetId();
            var b = obj.GetMap().GetId();
            if (obj != null)
                return IsInWorld && obj.IsInWorld && m == b;
            return false;
        }

        public bool IsWithinDist3d(float x, float y, float z, float dist)
        {
            return IsInDist(x, y, z, dist + GetObjectSize());
        }

        public bool IsWithinDist3d(Position pos, float dist)
        {
            return IsInDist(pos, dist + GetObjectSize());
        }

        public bool IsWithinDist2d(float x, float y, float dist)
        {
            return IsInDist2d(x, y, dist + GetObjectSize());
        }

        public bool IsWithinDist2d(Position pos, float dist)
        {
            return IsInDist2d(pos, dist + GetObjectSize());
        }

        public bool IsWithinDist(WorldObject obj, float dist2compare, bool is3D = true)
        {
            return obj != null && _IsWithinDist(obj, dist2compare, is3D);
        }

        public bool IsWithinDistInMap(WorldObject obj, float dist2compare, bool is3D = true)
        {
            return obj && IsInMap(obj) && IsInPhase(obj) && _IsWithinDist(obj, dist2compare, is3D);
        }

        public bool IsWithinLOS(float ox, float oy, float oz)
        {
            if (IsInWorld)
            {
                float x, y, z;
                if (IsTypeId(TypeId.Player))
                    GetPosition(out x, out y, out z);
                else
                    GetHitSpherePointFor(new Position(ox, oy, oz), out x, out y, out z);

                return GetMap().isInLineOfSight(x, y, z + 2.0f, ox, oy, oz + 2.0f, GetPhases());
            }

            return true;
        }

        Position GetHitSpherePointFor(Position dest)
        {
            Vector3 vThis = new Vector3(GetPositionX(), GetPositionY(), GetPositionZ());
            Vector3 vObj = new Vector3(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ());
            Vector3 contactPoint = vThis + (vObj - vThis).directionOrZero() * GetObjectSize();

            return new Position(contactPoint.X, contactPoint.Y, contactPoint.Z, GetAngle(contactPoint.X, contactPoint.Y));
        }

        void GetHitSpherePointFor(Position dest, out float x, out float y, out float z)
        {
            Position pos = GetHitSpherePointFor(dest);
            x = pos.GetPositionX();
            y = pos.GetPositionY();
            z = pos.GetPositionZ();
        }

        public bool GetDistanceOrder(WorldObject obj1, WorldObject obj2, bool is3D = true)
        {
            float dx1 = GetPositionX() - obj1.GetPositionX();
            float dy1 = GetPositionY() - obj1.GetPositionY();
            float distsq1 = dx1 * dx1 + dy1 * dy1;
            if (is3D)
            {
                float dz1 = GetPositionZ() - obj1.GetPositionZ();
                distsq1 += dz1 * dz1;
            }

            float dx2 = GetPositionX() - obj2.GetPositionX();
            float dy2 = GetPositionY() - obj2.GetPositionY();
            float distsq2 = dx2 * dx2 + dy2 * dy2;
            if (is3D)
            {
                float dz2 = GetPositionZ() - obj2.GetPositionZ();
                distsq2 += dz2 * dz2;
            }

            return distsq1 < distsq2;
        }

        public bool IsInRange(WorldObject obj, float minRange, float maxRange, bool is3D = true)
        {
            float dx = GetPositionX() - obj.GetPositionX();
            float dy = GetPositionY() - obj.GetPositionY();
            float distsq = dx * dx + dy * dy;
            if (is3D)
            {
                float dz = GetPositionZ() - obj.GetPositionZ();
                distsq += dz * dz;
            }

            float sizefactor = GetObjectSize() + obj.GetObjectSize();

            // check only for real range
            if (minRange > 0.0f)
            {
                float mindist = minRange + sizefactor;
                if (distsq < mindist * mindist)
                    return false;
            }

            float maxdist = maxRange + sizefactor;
            return distsq < maxdist * maxdist;
        }

        bool IsInBetween(WorldObject obj1, WorldObject obj2, float size = 0) { return obj1 && obj2 && IsInBetween(obj1.GetPosition(), obj2.GetPosition(), size); }
        bool IsInBetween(Position pos1, Position pos2, float size)
        {
            float dist = GetExactDist2d(pos1);

            // not using sqrt() for performance
            if ((dist * dist) >= pos1.GetExactDist2dSq(pos2))
                return false;

            if (size == 0)
                size = GetObjectSize() / 2;

            float angle = pos1.GetAngle(pos2);

            // not using sqrt() for performance
            return (size * size) >= GetExactDist2dSq(pos1.GetPositionX() + (float)Math.Cos(angle) * dist, pos1.GetPositionY() + (float)Math.Sin(angle) * dist);
        }

        public bool isInFront(WorldObject target, float arc = MathFunctions.PI)
        {
            return HasInArc(arc, target);
        }

        public bool isInBack(WorldObject target, float arc = MathFunctions.PI)
        {
            return !HasInArc(2 * MathFunctions.PI - arc, target);
        }

        public void GetRandomPoint(Position pos, float distance, out float rand_x, out float rand_y, out float rand_z)
        {
            if (distance == 0)
            {
                pos.GetPosition(out rand_x, out rand_y, out rand_z);
                return;
            }

            // angle to face `obj` to `this`
            float angle = (float)RandomHelper.NextDouble() * (2 * MathFunctions.PI);
            float new_dist = (float)RandomHelper.NextDouble() + (float)RandomHelper.NextDouble();
            new_dist = distance * (new_dist > 1 ? new_dist - 2 : new_dist);

            rand_x = (float)(pos.posX + new_dist * Math.Cos(angle));
            rand_y = (float)(pos.posY + new_dist * Math.Sin(angle));
            rand_z = pos.posZ;

            GridDefines.NormalizeMapCoord(ref rand_x);
            GridDefines.NormalizeMapCoord(ref rand_y);
            UpdateGroundPositionZ(rand_x, rand_y, ref rand_z);            // update to LOS height if available
        }

        public void GetRandomPoint(Position srcPos, float distance, out Position pos)
        {
            pos = new Position();
            float x, y, z;
            GetRandomPoint(srcPos, distance, out x, out y, out z);
            pos.Relocate(x, y, z, GetOrientation());
        }

        public void UpdateGroundPositionZ(float x, float y, ref float z)
        {
            float new_z = GetMap().GetHeight(GetPhases(), x, y, z, true);
            if (new_z > MapConst.InvalidHeight)
                z = new_z + 0.05f;                                   // just to be sure that we are not a few pixel under the surface
        }

        public void UpdateAllowedPositionZ(float x, float y, ref float z)
        {
            // TODO: Allow transports to be part of dynamic vmap tree
            if (GetTransport())
                return;

            switch (GetTypeId())
            {
                case TypeId.Unit:
                    {
                        // non fly unit don't must be in air
                        // non swim unit must be at ground (mostly speedup, because it don't must be in water and water level check less fast
                        if (!ToCreature().CanFly())
                        {
                            bool canSwim = ToCreature().CanSwim();
                            float ground_z = z;
                            float max_z = canSwim
                                ? GetMap().GetWaterOrGroundLevel(GetPhases(), x, y, z, ref ground_z, !ToUnit().HasAuraType(AuraType.WaterWalk))
                                : ((ground_z = GetMap().GetHeight(GetPhases(), x, y, z, true)));
                            if (max_z > MapConst.InvalidHeight)
                            {
                                if (z > max_z)
                                    z = max_z;
                                else if (z < ground_z)
                                    z = ground_z;
                            }
                        }
                        else
                        {
                            float ground_z = GetMap().GetHeight(GetPhases(), x, y, z, true);
                            if (z < ground_z)
                                z = ground_z;
                        }
                        break;
                    }
                case TypeId.Player:
                    {
                        // for server controlled moves playr work same as creature (but it can always swim)
                        if (!ToPlayer().CanFly())
                        {
                            float ground_z = z;
                            float max_z = GetMap().GetWaterOrGroundLevel(GetPhases(), x, y, z, ref ground_z, !ToUnit().HasAuraType(AuraType.WaterWalk));
                            if (max_z > MapConst.InvalidHeight)
                            {
                                if (z > max_z)
                                    z = max_z;
                                else if (z < ground_z)
                                    z = ground_z;
                            }
                        }
                        else
                        {
                            float ground_z = GetMap().GetHeight(GetPhases(), x, y, z, true);
                            if (z < ground_z)
                                z = ground_z;
                        }
                        break;
                    }
                default:
                    {
                        float ground_z = GetMap().GetHeight(GetPhases(), x, y, z, true);
                        if (ground_z > MapConst.InvalidHeight)
                            z = ground_z;
                        break;
                    }
            }
        }

        public void GetNearPoint2D(out float x, out float y, float distance2d, float absAngle)
        {
            x = (float)(GetPositionX() + (GetObjectSize() + distance2d) * Math.Cos(absAngle));
            y = (float)(GetPositionY() + (GetObjectSize() + distance2d) * Math.Sin(absAngle));

            GridDefines.NormalizeMapCoord(ref x);
            GridDefines.NormalizeMapCoord(ref y);
        }

        public void GetNearPoint(WorldObject searcher, out float x, out float y, out float z, float searcher_size, float distance2d, float absAngle)
        {
            GetNearPoint2D(out x, out y, distance2d + searcher_size, absAngle);
            z = GetPositionZ();
            UpdateAllowedPositionZ(x, y, ref z);

            // if detection disabled, return first point
            if (!WorldConfig.GetBoolValue(WorldCfg.DetectPosCollision))
                return;

            // return if the point is already in LoS
            if (IsWithinLOS(x, y, z))
                return;

            // remember first point
            float first_x = x;
            float first_y = y;
            float first_z = z;

            // loop in a circle to look for a point in LoS using small steps
            for (float angle = MathFunctions.PI / 8; angle < Math.PI * 2; angle += MathFunctions.PI / 8)
            {
                GetNearPoint2D(out x, out y, distance2d + searcher_size, absAngle + angle);
                z = GetPositionZ();
                UpdateAllowedPositionZ(x, y, ref z);
                if (IsWithinLOS(x, y, z))
                    return;
            }

            // still not in LoS, give up and return first position found
            x = first_x;
            y = first_y;
            z = first_z;
        }

        public void GetClosePoint(out float x, out float y, out float z, float size, float distance2d = 0, float angle = 0)
        {
            // angle calculated from current orientation
            GetNearPoint(null, out x, out y, out z, size, distance2d, GetOrientation() + angle);
        }

        public Position GetNearPosition(float dist, float angle)
        {
            var pos = GetPosition();
            MovePosition(ref pos, dist, angle);
            return pos;
        }

        public Position GetFirstCollisionPosition(float dist, float angle)
        {
            var pos = GetPosition();
            MovePositionToFirstCollision(ref pos, dist, angle);
            return pos;
        }

        public Position GetRandomNearPosition(float radius)
        {
            var pos = GetPosition();
            MovePosition(ref pos, radius * (float)RandomHelper.NextDouble(), (float)RandomHelper.NextDouble() * MathFunctions.PI * 2);
            return pos;
        }

        public void GetContactPoint(WorldObject obj, out float x, out float y, out float z, float distance2d = 0.5f)
        {
            // angle to face `obj` to `this` using distance includes size of `obj`
            GetNearPoint(obj, out x, out y, out z, obj.GetObjectSize(), distance2d, GetAngle(obj));
        }

        public void MovePosition(ref Position pos, float dist, float angle)
        {
            angle += GetOrientation();
            float destx, desty, destz, ground, floor;
            destx = pos.posX + dist * (float)Math.Cos(angle);
            desty = pos.posY + dist * (float)Math.Sin(angle);

            // Prevent invalid coordinates here, position is unchanged
            if (!GridDefines.IsValidMapCoord(destx, desty, pos.posZ))
            {
                Log.outError(LogFilter.Server, "WorldObject.MovePosition invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);
                return;
            }

            ground = GetMap().GetHeight(GetPhases(), destx, desty, MapConst.MaxHeight, true);
            floor = GetMap().GetHeight(GetPhases(), destx, desty, pos.posZ, true);
            destz = Math.Abs(ground - pos.posZ) <= Math.Abs(floor - pos.posZ) ? ground : floor;

            float step = dist / 10.0f;

            for (byte j = 0; j < 10; ++j)
            {
                // do not allow too big z changes
                if (Math.Abs(pos.posZ - destz) > 6)
                {
                    destx -= step * (float)Math.Cos(angle);
                    desty -= step * (float)Math.Sin(angle);
                    ground = GetMap().GetHeight(GetPhases(), destx, desty, MapConst.MaxHeight, true);
                    floor = GetMap().GetHeight(GetPhases(), destx, desty, pos.posZ, true);
                    destz = Math.Abs(ground - pos.posZ) <= Math.Abs(floor - pos.posZ) ? ground : floor;
                }
                // we have correct destz now
                else
                {
                    pos.Relocate(destx, desty, destz);
                    break;
                }
            }

            GridDefines.NormalizeMapCoord(ref pos.posX);
            GridDefines.NormalizeMapCoord(ref pos.posY);
            UpdateGroundPositionZ(pos.posX, pos.posY, ref pos.posZ);
            pos.SetOrientation(GetOrientation());
        }

        float NormalizeZforCollision(WorldObject obj, float x, float y, float z)
        {
            float ground = obj.GetMap().GetHeight(obj.GetPhases(), x, y, MapConst.MaxHeight, true);
            float floor = obj.GetMap().GetHeight(obj.GetPhases(), x, y, z + 2.0f, true);
            float helper = Math.Abs(ground - z) <= Math.Abs(floor - z) ? ground : floor;
            if (z > helper) // must be above ground
            {
                Unit unit = obj.ToUnit();
                if (unit)
                {
                    if (unit.CanFly())
                        return z;
                }
                LiquidData liquid_status;
                ZLiquidStatus res = obj.GetMap().getLiquidStatus(x, y, z, MapConst.MapAllLiquidTypes, out liquid_status);
                if (res != 0 && liquid_status.level > helper) // water must be above ground
                {
                    if (liquid_status.level > z) // z is underwater
                        return z;
                    else
                        return Math.Abs(liquid_status.level - z) <= Math.Abs(helper - z) ? liquid_status.level : helper;
                }
            }
            return helper;
        }

        public void MovePositionToFirstCollision(ref Position pos, float dist, float angle)
        {
            angle += GetOrientation();
            float destx, desty, destz;
            destx = pos.posX + dist * (float)Math.Cos(angle);
            desty = pos.posY + dist * (float)Math.Sin(angle);

            // Prevent invalid coordinates here, position is unchanged
            if (!GridDefines.IsValidMapCoord(destx, desty))
            {
                Log.outError(LogFilter.Server, "WorldObject.MovePositionToFirstCollision invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);
                return;
            }

            destz = NormalizeZforCollision(this, destx, desty, pos.GetPositionZ());
            bool col = Global.VMapMgr.getObjectHitPos(GetMapId(), pos.posX, pos.posY, pos.posZ + 0.5f, destx, desty, destz + 0.5f, out destx, out desty, out destz, -0.5f);

            // collision occured
            if (col)
            {
                // move back a bit
                destx -= SharedConst.ContactDistance * (float)Math.Cos(angle);
                desty -= SharedConst.ContactDistance * (float)Math.Sin(angle);
                dist = (float)Math.Sqrt((pos.posX - destx) * (pos.posX - destx) + (pos.posY - desty) * (pos.posY - desty));
            }

            // check dynamic collision
            col = GetMap().getObjectHitPos(GetPhases(), pos.posX, pos.posY, pos.posZ + 0.5f, destx, desty, destz + 0.5f, out destx, out desty, out destz, -0.5f);

            // Collided with a gameobject
            if (col)
            {
                destx -= SharedConst.ContactDistance * (float)Math.Cos(angle);
                desty -= SharedConst.ContactDistance * (float)Math.Sin(angle);
                dist = (float)Math.Sqrt((pos.posX - destx) * (pos.posX - destx) + (pos.posY - desty) * (pos.posY - desty));
            }

            float step = dist / 10.0f;

            for (byte j = 0; j < 10; ++j)
            {
                // do not allow too big z changes
                if (Math.Abs(pos.posZ - destz) > 6f)
                {
                    destx -= step * (float)Math.Cos(angle);
                    desty -= step * (float)Math.Sin(angle);
                    destz = NormalizeZforCollision(this, destx, desty, pos.GetPositionZ());
                }
                // we have correct destz now
                else
                {
                    pos.Relocate(destx, desty, destz);
                    break;
                }
            }

            GridDefines.NormalizeMapCoord(ref pos.posX);
            GridDefines.NormalizeMapCoord(ref pos.posY);
            pos.posZ = NormalizeZforCollision(this, destx, desty, pos.GetPositionZ());
            pos.SetOrientation(GetOrientation());
        }

        public void SetLocationInstanceId(uint _instanceId) { instanceId = _instanceId; }

        #region Fields
        public TypeMask objectTypeMask { get; set; }
        protected TypeId objectTypeId { get; set; }
        protected UpdateFlag m_updateFlag { get; set; }

        protected Hashtable UpdateData;
        protected Dictionary<int, uint>[] _dynamicValues;
        protected BitArray _changesMask { get; set; }
        protected Dictionary<int, DynamicFieldChangeType> _dynamicChangesMask = new Dictionary<int, DynamicFieldChangeType>();
        protected BitArray[] _dynamicChangesArrayMask;

        public uint valuesCount;
        protected uint _dynamicValuesCount;
        public uint LastUsedScriptID;

        protected uint _fieldNotifyFlags { get; set; }
        bool m_objectUpdated;

        public MovementInfo m_movementInfo;
        string _name;
        protected bool m_isActive;
        bool m_isWorldObject;
        public ZoneScript m_zoneScript;

        Transport m_transport;
        Map _currMap;
        uint instanceId;
        uint phaseMask;
        List<uint> _phases = new List<uint>();
        List<uint> _terrainSwaps = new List<uint>();
        List<uint> _worldMapAreaSwaps = new List<uint>();
        int _dbPhase;
        public bool IsInWorld { get; set; }

        NotifyFlags m_notifyflags;
        NotifyFlags m_executed_notifies;

        public FlaggedArray<StealthType> m_stealth = new FlaggedArray<StealthType>(2);
        public FlaggedArray<StealthType> m_stealthDetect = new FlaggedArray<StealthType>(2);

        public FlaggedArray<InvisibilityType> m_invisibility = new FlaggedArray<InvisibilityType>((int)InvisibilityType.Max);
        public FlaggedArray<InvisibilityType> m_invisibilityDetect = new FlaggedArray<InvisibilityType>((int)InvisibilityType.Max);

        public FlaggedArray<ServerSideVisibilityType> m_serverSideVisibility = new FlaggedArray<ServerSideVisibilityType>(2);
        public FlaggedArray<ServerSideVisibilityType> m_serverSideVisibilityDetect = new FlaggedArray<ServerSideVisibilityType>(2);
        #endregion

        public static implicit operator bool(WorldObject obj)
        {
            return obj != null;
        }
    }

    public class MovementInfo
    {
        public MovementInfo()
        {
            Guid = ObjectGuid.Empty;
            Flags = MovementFlag.None;
            Flags2 = MovementFlag2.None;
            Time = 0;
            Pitch = 0.0f;

            Pos = new Position();
            transport.Reset();
            jump.Reset();
        }

        public MovementFlag GetMovementFlags() { return Flags; }
        public void SetMovementFlags(MovementFlag f) { Flags = f; }
        public void AddMovementFlag(MovementFlag f) { Flags |= f; }
        public void RemoveMovementFlag(MovementFlag f) { Flags &= ~f; }
        public bool HasMovementFlag(MovementFlag f) { return Convert.ToBoolean(Flags & f); }

        public MovementFlag2 GetMovementFlags2() { return Flags2; }
        public void SetMovementFlags2(MovementFlag2 f) { Flags2 = f; }
        public void AddMovementFlag2(MovementFlag2 f) { Flags2 |= f; }
        public void RemoveMovementFlag2(MovementFlag2 f) { Flags2 &= ~f; }
        public bool HasMovementFlag2(MovementFlag2 f) { return Convert.ToBoolean(Flags2 & f); }

        public void SetFallTime(uint time) { jump.fallTime = time; }

        public void ResetTransport()
        {
            transport.Reset();
        }

        public void ResetJump()
        {
            jump.Reset();
        }

        public ObjectGuid Guid { get; set; }
        MovementFlag Flags { get; set; }
        MovementFlag2 Flags2 { get; set; }
        public Position Pos { get; set; }
        public uint Time { get; set; }
        public TransportInfo transport;
        public float Pitch { get; set; }
        public JumpInfo jump;
        public float SplineElevation { get; set; }

        public struct TransportInfo
        {
            public void Reset()
            {
                guid = ObjectGuid.Empty;
                pos = new Position();
                seat = -1;
                time = 0;
                prevTime = 0;
                vehicleId = 0;
            }

            public ObjectGuid guid;
            public Position pos;
            public sbyte seat;
            public uint time;
            public uint prevTime;
            public uint vehicleId;
        }
        public struct JumpInfo
        {
            public void Reset()
            {
                fallTime = 0;
                zspeed = sinAngle = cosAngle = xyspeed = 0.0f;
            }

            public uint fallTime;
            public float zspeed;
            public float sinAngle;
            public float cosAngle;
            public float xyspeed;
        }
    }
}