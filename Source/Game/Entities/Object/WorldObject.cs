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
using Framework.Dynamic;
using Framework.GameMath;
using Game.AI;
using Game.BattleFields;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scenarios;
using System;
using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Entities
{
    public abstract class WorldObject : WorldLocation, IDisposable
    {
        public WorldObject(bool isWorldObject)
        {
            _name = "";
            m_isWorldObject = isWorldObject;

            m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive | GhostVisibilityType.Ghost);
            m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);

            ObjectTypeId = TypeId.Object;
            ObjectTypeMask = TypeMask.Object;

            m_values = new UpdateFieldHolder(this);

            m_movementInfo = new MovementInfo();
            m_updateFlag.Clear();

            m_objectData = new ObjectFieldData();

            m_staticFloorZ = MapConst.VMAPInvalidHeightValue;
        }

        public virtual void Dispose()
        {
            // this may happen because there are many !create/delete
            if (IsWorldObject() && _currMap)
            {
                if (IsTypeId(TypeId.Corpse))
                {
                    Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() Corpse Type: {0} ({1}) deleted but still in map!!", ToCorpse().GetCorpseType(), GetGUID().ToString());
                    Cypher.Assert(false);
                }
                ResetMap();
            }

            if (IsInWorld)
            {
                Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() {0} deleted but still in world!!", GetGUID().ToString());
                if (IsTypeMask(TypeMask.Item))
                    Log.outFatal(LogFilter.Misc, "Item slot {0}", ((Item)this).GetSlot());
                Cypher.Assert(false);
            }

            if (m_objectUpdated)
            {
                Log.outFatal(LogFilter.Misc, "WorldObject.Dispose() {0} deleted but still in update list!!", GetGUID().ToString());
                Cypher.Assert(false);
            }
        }

        public void _Create(ObjectGuid guid)
        {
            m_objectUpdated = false;
            m_guid = guid;
        }

        public virtual void AddToWorld()
        {
            if (IsInWorld)
                return;

            IsInWorld = true;
            ClearUpdateMask(true);

            if (GetMap() != null)
                GetMap().GetZoneAndAreaId(_phaseShift, out m_zoneId, out m_areaId, GetPositionX(), GetPositionY(), GetPositionZ());
        }

        public virtual void RemoveFromWorld()
        {
            if (!IsInWorld)
                return;

            if (!ObjectTypeMask.HasAnyFlag(TypeMask.Item | TypeMask.Container))
                DestroyForNearbyPlayers();

            IsInWorld = false;
            ClearUpdateMask(true);
        }

        public void UpdatePositionData()
        {
            var data = new PositionFullTerrainStatus();
            GetMap().GetFullTerrainStatusForPosition(_phaseShift, GetPositionX(), GetPositionY(), GetPositionZ(), data);
            ProcessPositionDataChanged(data);
        }

        public virtual void ProcessPositionDataChanged(PositionFullTerrainStatus data)
        {
            m_zoneId = m_areaId = data.AreaId;

            var area = CliDB.AreaTableStorage.LookupByKey(m_areaId);
            if (area != null)
                if (area.ParentAreaID != 0)
                    m_zoneId = area.ParentAreaID;

            m_staticFloorZ = data.FloorZ;
        }
        
        public virtual void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
        {
            if (!target)
                return;

            var updateType = UpdateType.CreateObject;
            var tempObjectType = ObjectTypeId;
            var tempObjectTypeMask = ObjectTypeMask;
            var flags = m_updateFlag;

            if (target == this)
            {
                flags.ThisIsYou = true;
                flags.ActivePlayer = true;
                tempObjectType = TypeId.ActivePlayer;
                tempObjectTypeMask |= TypeMask.ActivePlayer;
            }

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
                    var summon = ToUnit().ToTempSummon();
                    if (summon)
                        if (summon.GetSummonerGUID().IsPlayer())
                            updateType = UpdateType.CreateObject2;
                    break;
                case HighGuid.GameObject:
                    if (ToGameObject().GetOwnerGUID().IsPlayer())
                        updateType = UpdateType.CreateObject2;
                    break;
            }

            if (!flags.MovementUpdate && !m_movementInfo.transport.guid.IsEmpty())
                flags.MovementTransport = true;

            if (GetAIAnimKitId() != 0 || GetMovementAnimKitId() != 0 || GetMeleeAnimKitId() != 0)
                flags.AnimKit = true;

            if (flags.Stationary)
            {
                // UPDATETYPE_CREATE_OBJECT2 for some gameobject types...
                if (IsTypeMask(TypeMask.GameObject))
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
            var unit = ToUnit();
            if (unit)
                if (unit.GetVictim())
                    flags.CombatVictim = true;

            var buffer = new WorldPacket();
            buffer.WriteUInt8((byte)updateType);
            buffer.WritePackedGuid(GetGUID());
            buffer.WriteUInt8((byte)tempObjectType);

            BuildMovementUpdate(buffer, flags);
            BuildValuesCreate(buffer, target);
            data.AddUpdateBlock(buffer);
        }

        public void SendUpdateToPlayer(Player player)
        {
            // send create update to player
            var upd = new UpdateData(player.GetMapId());
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
            var buffer = new WorldPacket();
            buffer.WriteUInt8((byte)UpdateType.Values);
            buffer.WritePackedGuid(GetGUID());

            BuildValuesUpdate(buffer, target);

            data.AddUpdateBlock(buffer);
        }

        public void BuildValuesUpdateBlockForPlayerWithFlag(UpdateData data, UpdateFieldFlag flags, Player target)
        {
            var buffer = new WorldPacket();
            buffer.WriteUInt8((byte)UpdateType.Values);
            buffer.WritePackedGuid(GetGUID());

            BuildValuesUpdateWithFlag(buffer, flags, target);

            data.AddUpdateBlock(buffer);
        }

        private void BuildDestroyUpdateBlock(UpdateData data)
        {
            data.AddDestroyObject(GetGUID());
        }
        
        public void BuildOutOfRangeUpdateBlock(UpdateData data)
        {
            data.AddOutOfRangeGUID(GetGUID());
        }

        public virtual void DestroyForPlayer(Player target)
        {
            var updateData = new UpdateData(target.GetMapId());
            BuildDestroyUpdateBlock(updateData);
            UpdateObject packet;
            updateData.BuildPacket(out packet);
            target.SendPacket(packet);
        }

        public void BuildMovementUpdate(WorldPacket data, CreateObjectBits flags)
        {
            var PauseTimesCount = 0;

            var go = ToGameObject();
            if (go)
            {
                if (go.GetGoType() == GameObjectTypes.Transport)
                    PauseTimesCount = go.GetGoValue().Transport.StopFrames.Count;
            }

            data.WriteBit(flags.NoBirthAnim);
            data.WriteBit(flags.EnablePortals);
            data.WriteBit(flags.PlayHoverAnim);
            data.WriteBit(flags.MovementUpdate);
            data.WriteBit(flags.MovementTransport);
            data.WriteBit(flags.Stationary);
            data.WriteBit(flags.CombatVictim);
            data.WriteBit(flags.ServerTime);
            data.WriteBit(flags.Vehicle);
            data.WriteBit(flags.AnimKit);
            data.WriteBit(flags.Rotation);
            data.WriteBit(flags.AreaTrigger);
            data.WriteBit(flags.GameObject);
            data.WriteBit(flags.SmoothPhasing);
            data.WriteBit(flags.ThisIsYou);
            data.WriteBit(flags.SceneObject);
            data.WriteBit(flags.ActivePlayer);
            data.WriteBit(flags.Conversation);
            data.FlushBits();

            if (flags.MovementUpdate)
            {
                var unit = ToUnit();
                var HasFallDirection = unit.HasUnitMovementFlag(MovementFlag.Falling);
                var HasFall = HasFallDirection || unit.m_movementInfo.jump.fallTime != 0;
                var HasSpline = unit.IsSplineEnabled();

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
                data.WriteBit(!unit.m_movementInfo.transport.guid.IsEmpty());  // HasTransport
                data.WriteBit(HasFall);                                        // HasFall
                data.WriteBit(HasSpline);                                      // HasSpline - marks that the unit uses spline movement
                data.WriteBit(false);                                          // HeightChangeFailed
                data.WriteBit(false);                                          // RemoteTimeValid

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

                var movementForces = unit.GetMovementForces();
                if (movementForces != null)
                {
                    data.WriteInt32(movementForces.GetForces().Count);
                    data.WriteFloat(movementForces.GetModMagnitude());          // MovementForcesModMagnitude
                }
                else
                {
                    data.WriteUInt32(0);
                    data.WriteFloat(1.0f);                                       // MovementForcesModMagnitude
                }

                data.WriteBit(HasSpline);
                data.FlushBits();

                if (movementForces != null)
                    foreach (var force in movementForces.GetForces())
                        MovementExtensions.WriteMovementForceWithDirection(force, data, unit);

                // HasMovementSpline - marks that spline data is present in packet
                if (HasSpline)
                    MovementExtensions.WriteCreateObjectSplineDataBlock(unit.MoveSpline, data);
            }

            data.WriteInt32(PauseTimesCount);

            if (flags.Stationary)
            {
                var self = this;
                data.WriteFloat(self.GetStationaryX());
                data.WriteFloat(self.GetStationaryY());
                data.WriteFloat(self.GetStationaryZ());
                data.WriteFloat(self.GetStationaryO());
            }

            if (flags.CombatVictim)
                data.WritePackedGuid(ToUnit().GetVictim().GetGUID());                      // CombatVictim

            if (flags.ServerTime)
            {
                var go1 = ToGameObject();
                /** @TODO Use IsTransport() to also handle type 11 (TRANSPORT)
                    Currently grid objects are not updated if there are no nearby players,
                    this causes clients to receive different PathProgress
                    resulting in players seeing the object in a different position
                */
                if (go1 && go1.ToTransport())                                    // ServerTime
                    data.WriteUInt32(go1.GetGoValue().Transport.PathProgress);
                else
                    data.WriteUInt32(GameTime.GetGameTimeMS());
            }

            if (flags.Vehicle)
            {
                var unit = ToUnit();
                data.WriteUInt32(unit.GetVehicleKit().GetVehicleInfo().Id); // RecID
                data.WriteFloat(unit.GetOrientation());                         // InitialRawFacing
            }

            if (flags.AnimKit)
            {
                data.WriteUInt16(GetAIAnimKitId());                        // AiID
                data.WriteUInt16(GetMovementAnimKitId());                  // MovementID
                data.WriteUInt16(GetMeleeAnimKitId());                     // MeleeID
            }

            if (flags.Rotation)
                data.WriteInt64(ToGameObject().GetPackedWorldRotation());                 // Rotation

            if (go)
            {
                for (var i = 0; i < PauseTimesCount; ++i)
                    data.WriteUInt32(go.GetGoValue().Transport.StopFrames[i]);
            }

            if (flags.MovementTransport)
            {
                var self = this;
                MovementExtensions.WriteTransportInfo(data, self.m_movementInfo.transport);
            }

            if (flags.AreaTrigger)
            {
                var areaTrigger = ToAreaTrigger();
                var areaTriggerMiscTemplate = areaTrigger.GetMiscTemplate();
                var areaTriggerTemplate = areaTrigger.GetTemplate();

                data.WriteUInt32(areaTrigger.GetTimeSinceCreated());

                data.WriteVector3(areaTrigger.GetRollPitchYaw());

                var hasAbsoluteOrientation = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAbsoluteOrientation);
                var hasDynamicShape = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasDynamicShape);
                var hasAttached = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAttached);
                var hasFaceMovementDir = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFaceMovementDir);
                var hasFollowsTerrain = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFollowsTerrain);
                var hasUnk1 = areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk1);
                var hasTargetRollPitchYaw = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasTargetRollPitchYaw);
                var hasScaleCurveID = areaTriggerMiscTemplate.ScaleCurveId != 0;
                var hasMorphCurveID = areaTriggerMiscTemplate.MorphCurveId != 0;
                var hasFacingCurveID = areaTriggerMiscTemplate.FacingCurveId != 0;
                var hasMoveCurveID = areaTriggerMiscTemplate.MoveCurveId != 0;
                var hasAnimation = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAnimID);
                var hasUnk3 = areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk3);
                var hasAnimKitID = areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAnimKitID);
                var hasAnimProgress = false;
                var hasAreaTriggerSphere = areaTriggerTemplate.IsSphere();
                var hasAreaTriggerBox = areaTriggerTemplate.IsBox();
                var hasAreaTriggerPolygon = areaTriggerTemplate.IsPolygon();
                var hasAreaTriggerCylinder = areaTriggerTemplate.IsCylinder();
                var hasAreaTriggerSpline = areaTrigger.HasSplines();
                var hasOrbit = areaTrigger.HasOrbit();
                var hasMovementScript = false;

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
                data.WriteBit(hasAnimation);
                data.WriteBit(hasAnimKitID);
                data.WriteBit(hasUnk3);
                data.WriteBit(hasAnimProgress);
                data.WriteBit(hasAreaTriggerSphere);
                data.WriteBit(hasAreaTriggerBox);
                data.WriteBit(hasAreaTriggerPolygon);
                data.WriteBit(hasAreaTriggerCylinder);
                data.WriteBit(hasAreaTriggerSpline);
                data.WriteBit(hasOrbit);
                data.WriteBit(hasMovementScript);

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

                if (hasAnimation)
                    data.WriteUInt32(areaTriggerMiscTemplate.AnimId);

                if (hasAnimKitID)
                    data.WriteUInt32(areaTriggerMiscTemplate.AnimKitId);

                if (hasAnimProgress)
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
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.Extents[0]);
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.Extents[1]);
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.Extents[2]);

                        data.WriteFloat(areaTriggerTemplate.BoxDatas.ExtentsTarget[0]);
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.ExtentsTarget[1]);
                        data.WriteFloat(areaTriggerTemplate.BoxDatas.ExtentsTarget[2]);
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

                //if (hasMovementScript)
                //    *data << *areaTrigger->GetMovementScript(); // AreaTriggerMovementScriptInfo

                if (hasOrbit)
                   areaTrigger.GetCircularMovementInfo().Value.Write(data);
            }

            if (flags.GameObject)
            {
                var bit8 = false;
                uint Int1 = 0;

                var gameObject = ToGameObject();

                data.WriteUInt32(gameObject.GetWorldEffectID());

                data.WriteBit(bit8);
                data.FlushBits();
                if (bit8)
                    data.WriteUInt32(Int1);
            }

            //if (flags.SmoothPhasing)
            //{
            //    data.WriteBit(ReplaceActive);
            //    data.WriteBit(HasReplaceObjectt);
            //    data.FlushBits();
            //    if (HasReplaceObject)
            //        *data << ObjectGuid(ReplaceObject);
            //}

            //if (flags.SceneObject)
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

            if (flags.ActivePlayer)
            {
                var HasSceneInstanceIDs = false;
                var HasRuneState = ToUnit().GetPowerIndex(PowerType.Runes) != (int)PowerType.Max;

                data.WriteBit(HasSceneInstanceIDs);
                data.WriteBit(HasRuneState);
                data.FlushBits();
                //if (HasSceneInstanceIDs)
                //{
                //    *data << uint32(SceneInstanceIDs.size());
                //    for (std::size_t i = 0; i < SceneInstanceIDs.size(); ++i)
                //        *data << uint32(SceneInstanceIDs[i]);
                //}
                if (HasRuneState)
                {
                    var player = ToPlayer();
                    float baseCd = player.GetRuneBaseCooldown();
                    var maxRunes = (uint)player.GetMaxPower(PowerType.Runes);

                    data.WriteUInt8((byte)((1 << (int)maxRunes) - 1u));
                    data.WriteUInt8(player.GetRunesState());
                    data.WriteUInt32(maxRunes);
                    for (byte i = 0; i < maxRunes; ++i)
                        data.WriteUInt8((byte)((baseCd - (float)player.GetRuneCooldown(i)) / baseCd * 255));
                }
            }

            if (flags.Conversation)
            {
                var self = ToConversation();
                if (data.WriteBit(self.GetTextureKitId() != 0))
                    data.WriteUInt32(self.GetTextureKitId());
                data.FlushBits();
            }
        }

        public void DoWithSuppressingObjectUpdates(Action action)
        {
            var wasUpdatedBeforeAction = m_objectUpdated;
            action();
            if (m_objectUpdated && !wasUpdatedBeforeAction)
            {
                RemoveFromObjectUpdate();
                m_objectUpdated = false;
            }
        }

        public virtual UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
        {
            return UpdateFieldFlag.None;
        }

        public virtual void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            data.WriteUInt32(0);
            data.WriteUInt32(0);
        }

        public void AddToObjectUpdateIfNeeded()
        {
            if (IsInWorld && !m_objectUpdated)
            {
                AddToObjectUpdate();
                m_objectUpdated = true;
            }
        }

        public virtual void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_objectData);

            if (m_objectUpdated)
            {
                if (remove)
                    RemoveFromObjectUpdate();

                m_objectUpdated = false;
            }
        }

        public void BuildFieldsUpdate(Player player, Dictionary<Player, UpdateData> data_map)
        {
            if (!data_map.ContainsKey(player))
                data_map.Add(player, new UpdateData(player.GetMapId()));

            BuildValuesUpdateBlockForPlayer(data_map[player], player);
        }

        public abstract void BuildValuesCreate(WorldPacket data, Player target);
        public abstract void BuildValuesUpdate(WorldPacket data, Player target);

        public void SetUpdateFieldValue<T>(IUpdateField<T> updateField, T newValue) where T : new()
        {
            if (!newValue.Equals(updateField.GetValue()))
            {
                updateField.SetValue(newValue);
                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetUpdateFieldValue<T>(ref T value, T newValue) where T : new()
        {
            if (!newValue.Equals(value))
            {
                value = newValue;
                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetUpdateFieldValue<T>(DynamicUpdateField<T> updateField, int index, T newValue) where T : new()
        {
            if (!newValue.Equals(updateField[index]))
            {
                updateField[index] = newValue;
                AddToObjectUpdateIfNeeded();
            }
        }

        public void SetUpdateFieldFlagValue<T>(IUpdateField<T> updateField, T flag) where T : new()
        {
            //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
            SetUpdateFieldValue(updateField, (T)(updateField.GetValue() | (dynamic)flag));
        }

        public void SetUpdateFieldFlagValue<T>(ref T value, T flag) where T : new()
        {
            //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
            SetUpdateFieldValue(ref value, (T)(value | (dynamic)flag));
        }

        public void RemoveUpdateFieldFlagValue<T>(IUpdateField<T> updateField, T flag) where T : new()
        {
            //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
            SetUpdateFieldValue(updateField, (T)(updateField.GetValue() & ~(dynamic)flag));
        }

        public void RemoveUpdateFieldFlagValue<T>(ref T value, T flag) where T : new()
        {
            //static_assert(std::is_integral < T >::value, "RemoveUpdateFieldFlagValue must be used with integral types");
            SetUpdateFieldValue(ref value, (T)(value & ~(dynamic)flag));
        }

        public void AddDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, T value) where T : new()
        {
            AddToObjectUpdateIfNeeded();
            updateField.AddValue(value);
        }

        public void InsertDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, int index, T value) where T : new()
        {
            AddToObjectUpdateIfNeeded();
            updateField.InsertValue(index, value);
        }

        public void RemoveDynamicUpdateFieldValue<T>(DynamicUpdateField<T> updateField, int index) where T : new()
        {
            AddToObjectUpdateIfNeeded();
            updateField.RemoveValue(index);
        }

        public void ClearDynamicUpdateFieldValues<T>(DynamicUpdateField<T> updateField) where T : new()
        {
            AddToObjectUpdateIfNeeded();
            updateField.Clear();
        }

        // stat system helpers
        public void SetUpdateFieldStatValue<T>(IUpdateField<T> updateField, T value) where T : new()
        {
            //static_assert(std::is_arithmetic < T >::value, "SetUpdateFieldStatValue must be used with arithmetic types");
            SetUpdateFieldValue(updateField, Math.Max((dynamic)value, 0));
        }

        public void SetUpdateFieldStatValue<T>(ref T oldValue, T value)
        {      
            //static_assert(std::is_arithmetic < T >::value, "SetUpdateFieldStatValue must be used with arithmetic types");
            SetUpdateFieldValue(ref oldValue, Math.Max((dynamic)value, 0));
        }

        public void ApplyModUpdateFieldValue<T>(IUpdateField<T> updateField, T mod, bool apply) where T : new()
        {
            //static_assert(std::is_arithmetic < T >::value, "SetUpdateFieldStatValue must be used with arithmetic types");
            dynamic value = updateField.GetValue();
            if (apply)
                value += mod;
            else
                value -= mod;

            SetUpdateFieldValue(updateField, value);
        }

        public void ApplyModUpdateFieldValue<T>(ref T oldvalue, T mod, bool apply) where T : new()
        {
            //static_assert(std::is_arithmetic < T >::value, "SetUpdateFieldStatValue must be used with arithmetic types");

            dynamic value = oldvalue;
            if (apply)
                value += mod;
            else
                value -= mod;

            SetUpdateFieldValue(ref oldvalue, value);
        }

        public void ApplyPercentModUpdateFieldValue<T>(IUpdateField<T> updateField, float percent, bool apply) where T : new()
        {
            //static_assert(std::is_arithmetic < T >::value, "SetUpdateFieldStatValue must be used with arithmetic types");

            dynamic value = updateField.GetValue();

            if (percent == -100.0f)
                percent = -99.99f;
            value *= (apply ? (100.0f + percent) / 100.0f : 100.0f / (100.0f + percent));

            SetUpdateFieldValue(updateField, value);
        }

        public void ApplyPercentModUpdateFieldValue<T>(ref T oldValue, float percent, bool apply) where T : new()
        {
            //static_assert(std::is_arithmetic < T >::value, "SetUpdateFieldStatValue must be used with arithmetic types");

            dynamic value = oldValue;

            if (percent == -100.0f)
                percent = -99.99f;
            value *= (apply ? (100.0f + percent) / 100.0f : 100.0f / (100.0f + percent));

            SetUpdateFieldValue(ref oldValue, value);
        }

        public void ForceUpdateFieldChange()
        {
            AddToObjectUpdateIfNeeded();
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
        public void SetActive(bool on)
        {
            if (m_isActive == on)
                return;

            if (IsTypeId(TypeId.Player))
                return;

            m_isActive = on;

            if (!IsInWorld)
                return;

            var map = GetMap();
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

        private bool IsVisibilityOverridden() { return m_visibilityDistanceOverride.HasValue; }

        public void SetVisibilityDistanceOverride(VisibilityDistanceType type)
        {
            Cypher.Assert(type < VisibilityDistanceType.Max);
            if (GetTypeId() == TypeId.Player)
                return;

            m_visibilityDistanceOverride.Set(SharedConst.VisibilityDistances[(int)type]);
        }

        public virtual void CleanupsBeforeDelete(bool finalCleanup = true)
        {
            if (IsInWorld)
                RemoveFromWorld();

            var transport = GetTransport();
            if (transport)
                transport.RemovePassenger(this);
        }
        
        public uint GetZoneId()  { return m_zoneId; }
        public uint GetAreaId()  { return m_areaId; }

        public void GetZoneAndAreaId(out uint zoneid, out uint areaid) { zoneid = m_zoneId; areaid = m_areaId; }
        
        public bool IsInWorldPvpZone()
        {
            switch (GetZoneId())
            {
                case 4197: // Wintergrasp
                case 5095: // Tol Barad
                case 6941: // Ashran
                    return true;
                default:
                    return false;
            }
        }

        public InstanceScript GetInstanceScript()
        {
            var map = GetMap();
            return map.IsDungeon() ? ((InstanceMap)map).GetInstanceScript() : null;
        }

        public float GetGridActivationRange()
        {
            if (IsActiveObject())
            {
                if (GetTypeId() == TypeId.Player && ToPlayer().GetCinematicMgr().IsOnCinematic())
                    return Math.Max(SharedConst.DefaultVisibilityInstance, GetMap().GetVisibilityRange());

                return GetMap().GetVisibilityRange();
            }
            var thisCreature = ToCreature();
            if (thisCreature != null)
                return thisCreature.m_SightDistance;

            return 0.0f;
        }

        public float GetVisibilityRange()
        {
            if (IsVisibilityOverridden() && !IsTypeId(TypeId.Player))
                return m_visibilityDistanceOverride.Value;
            else if (IsActiveObject() && !IsTypeId(TypeId.Player))
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
                    if (target != null && target.IsVisibilityOverridden() && !target.IsTypeId(TypeId.Player))
                        return target.m_visibilityDistanceOverride.Value;
                    else if (target != null && target.IsActiveObject() && !target.IsTypeId(TypeId.Player))
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

            if (IsTypeId(TypeId.DynamicObject) && IsActiveObject())
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

            var corpseVisibility = false;
            if (distanceCheck)
            {
                var corpseCheck = false;
                var thisPlayer = ToPlayer();
                if (thisPlayer != null)
                {
                    if (thisPlayer.IsDead() && thisPlayer.GetHealth() > 0 && // Cheap way to check for ghost state
                    !Convert.ToBoolean(obj.m_serverSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & m_serverSideVisibility.GetValue(ServerSideVisibilityType.Ghost) & (uint)GhostVisibilityType.Ghost))
                    {
                        var corpse = thisPlayer.GetCorpse();
                        if (corpse != null)
                        {
                            corpseCheck = true;
                            if (corpse.IsWithinDist(thisPlayer, GetSightRange(obj), false))
                                if (corpse.IsWithinDist(obj, GetSightRange(obj), false))
                                    corpseVisibility = true;
                        }
                    }

                    var target = obj.ToUnit();
                    if (target)
                    {
                        // Don't allow to detect vehicle accessories if you can't see vehicle
                        var vehicle = target.GetVehicleBase();
                        if (vehicle)
                            if (!thisPlayer.HaveAtClient(vehicle))
                                return false;
                    }
                }

                var viewpoint = this;
                var player = ToPlayer();
                if (player != null)
                {
                    viewpoint = player.GetViewpoint();

                    var creature = obj.ToCreature();
                    if (creature)
                        if (TempSummon.IsPersonalSummonOfAnotherPlayer(creature, GetGUID()))
                            return false;
                }

                var go = obj.ToGameObject();
                if ( go != null)
                    if (go.IsVisibleByUnitOnly() && GetGUID() != go.GetVisibleByUnitOnly())
                        return false;

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
                var thisPlayer = ToPlayer();
                if (thisPlayer != null)
                {
                    var objPlayer = obj.ToPlayer();
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

        private bool CanNeverSee(WorldObject obj)
        {
            return GetMap() != obj.GetMap() || !IsInPhase(obj);
        }

        public virtual bool CanAlwaysSee(WorldObject obj) { return false; }

        private bool CanDetect(WorldObject obj, bool ignoreStealth, bool checkAlert = false)
        {
            var seer = this;

            // Pets don't have detection, they use the detection of their masters
            var thisUnit = ToUnit();
            if (thisUnit != null)
            {
                var controller = thisUnit.GetCharmerOrOwner();
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

        private bool CanDetectInvisibilityOf(WorldObject obj)
        {
            var mask = obj.m_invisibility.GetFlags() & m_invisibilityDetect.GetFlags();

            // Check for not detected types
            if (mask != obj.m_invisibility.GetFlags())
                return false;

            for (var i = 0; i < (int)InvisibilityType.Max; ++i)
            {
                if (!Convert.ToBoolean(mask & (1 << i)))
                    continue;

                var objInvisibilityValue = obj.m_invisibility.GetValue((InvisibilityType)i);
                var ownInvisibilityDetectValue = m_invisibilityDetect.GetValue((InvisibilityType)i);

                // Too low value to detect
                if (ownInvisibilityDetectValue < objInvisibilityValue)
                    return false;
            }

            return true;
        }

        private bool CanDetectStealthOf(WorldObject obj, bool checkAlert = false)
        {
            // Combat reach is the minimal distance (both in front and behind),
            //   and it is also used in the range calculation.
            // One stealth point increases the visibility range by 0.3 yard.

            if (obj.m_stealth.GetFlags() == 0)
                return true;

            var distance = GetExactDist(obj);
            var combatReach = 0.0f;

            var unit = ToUnit();
            if (unit != null)
                combatReach = unit.GetCombatReach();

            if (distance < combatReach)
                return true;

            if (!HasInArc(MathFunctions.PI, obj))
                return false;

            var go = obj.ToGameObject();
            for (var i = 0; i < (int)StealthType.Max; ++i)
            {
                if (!Convert.ToBoolean(obj.m_stealth.GetFlags() & (1 << i)))
                    continue;

                if (unit != null && unit.HasAuraTypeWithMiscvalue(AuraType.DetectStealth, i))
                    return true;

                // Starting points
                var detectionValue = 30;

                // Level difference: 5 point / level, starting from level 1.
                // There may be spells for this and the starting points too, but
                // not in the DBCs of the client.
                detectionValue += (int)(GetLevelForTarget(obj) - 1) * 5;

                // Apply modifiers
                detectionValue += m_stealthDetect.GetValue((StealthType)i);
                if (go != null)
                {
                    var owner = go.GetOwner();
                    if (owner != null)
                        detectionValue -= (int)(owner.GetLevelForTarget(this) - 1) * 5;
                }

                detectionValue -= obj.m_stealth.GetValue((StealthType)i);

                // Calculate max distance
                var visibilityRange = detectionValue * 0.3f + combatReach;

                // If this unit is an NPC then player detect range doesn't apply
                if (unit && unit.IsTypeId(TypeId.Player) && visibilityRange > SharedConst.MaxPlayerStealthDetectRange)
                    visibilityRange = SharedConst.MaxPlayerStealthDetectRange;

                // When checking for alert state, look 8% further, and then 1.5 yards more than that.
                if (checkAlert)
                    visibilityRange += (visibilityRange * 0.08f) + 1.5f;

                // If checking for alert, and creature's visibility range is greater than aggro distance, No alert
                var tunit = obj.ToUnit();
                if (checkAlert && unit && unit.ToCreature() && visibilityRange >= unit.ToCreature().GetAttackDistance(tunit) + unit.ToCreature().m_CombatDistance)
                    return false;

                if (distance > visibilityRange)
                    return false;
            }

            return true;
        }

        public virtual void SendMessageToSet(ServerPacket packet, bool self)
        {
            if (IsInWorld)
                SendMessageToSetInRange(packet, GetVisibilityRange(), self);
        }

        public virtual void SendMessageToSetInRange(ServerPacket data, float dist, bool self)
        {
            var notifier = new MessageDistDeliverer(this, data, dist);
            Cell.VisitWorldObjects(this, notifier, dist);
        }

        public virtual void SendMessageToSet(ServerPacket data, Player skip)
        {
            var notifier = new MessageDistDeliverer(this, data, GetVisibilityRange(), false, skip);
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
        }

        public virtual void SetMap(Map map)
        {
            Cypher.Assert(map != null);
            Cypher.Assert(!IsInWorld);

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
            if (_currMap == null)
                return;

            Cypher.Assert(_currMap != null);
            Cypher.Assert(!IsInWorld);
            if (IsWorldObject())
                _currMap.RemoveWorldObject(this);
            _currMap = null;
        }

        public Map GetMap() { return _currMap; }

        public void AddObjectToRemoveList()
        {
            var map = GetMap();
            if (map == null)
            {
                Log.outError(LogFilter.Server, "Object (TypeId: {0} Entry: {1} GUID: {2}) at attempt add to move list not have valid map (Id: {3}).", GetTypeId(), GetEntry(), GetGUID().ToString(), GetMapId());
                return;
            }

            map.AddObjectToRemoveList(this);
        }

        public void SetZoneScript()
        {
            var map = GetMap();
            if (map != null)
            {
                if (map.IsDungeon())
                    m_zoneScript = ((InstanceMap)map).GetInstanceScript();
                else if (!map.IsBattlegroundOrArena())
                {
                    var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(GetZoneId());
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
                var instanceMap = GetMap().ToInstanceMap();
                if (instanceMap != null)
                    return instanceMap.GetInstanceScenario();
            }

            return null;
        }

        public TempSummon SummonCreature(uint entry, float x, float y, float z, float o = 0, TempSummonType despawnType = TempSummonType.ManualDespawn, uint despawnTime = 0, bool visibleBySummonerOnly = false)
        {
            if (x == 0.0f && y == 0.0f && z == 0.0f)
                GetClosePoint(out x, out y, out z, GetCombatReach());

            if (o == 0.0f)
                o = GetOrientation();

            return SummonCreature(entry, new Position(x, y, z, o), despawnType, despawnTime, 0, visibleBySummonerOnly);
        }

        public TempSummon SummonCreature(uint entry, Position pos, TempSummonType despawnType = TempSummonType.ManualDespawn, uint despawnTime = 0, uint vehId = 0, bool visibleBySummonerOnly = false)
        {
            var map = GetMap();
            if (map != null)
            {
                var summon = map.SummonCreature(entry, pos, null, despawnTime, ToUnit(), 0, vehId, visibleBySummonerOnly);
                if (summon != null)
                {
                    summon.SetTempSummonType(despawnType);
                    return summon;
                }
            }

            return null;
        }

        public GameObject SummonGameObject(uint entry, float x, float y, float z, float ang, Quaternion rotation, uint respawnTime)
        {
            if (x == 0 && y == 0 && z == 0)
            {
                GetClosePoint(out x, out y, out z, GetCombatReach());
                ang = GetOrientation();
            }

            var pos = new Position(x, y, z, ang);
            return SummonGameObject(entry, pos, rotation, respawnTime);
        }

        public GameObject SummonGameObject(uint entry, Position pos, Quaternion rotation, uint respawnTime)
        {
            if (!IsInWorld)
                return null;

            var goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);
            if (goinfo == null)
            {
                Log.outError(LogFilter.Sql, "Gameobject template {0} not found in database!", entry);
                return null;
            }

            var map = GetMap();
            var go = GameObject.CreateGameObject(entry, map, pos, rotation, 255, GameObjectState.Ready);
            if (!go)
                return null;

            PhasingHandler.InheritPhaseShift(go, this);

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
            var summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;
            Creature summon = SummonCreature(SharedConst.WorldTrigger, x, y, z, ang, summonType, duration);
            if (summon == null)
                return null;

            if (IsTypeId(TypeId.Player) || IsTypeId(TypeId.Unit))
            {
                summon.SetFaction(ToUnit().GetFaction());
                summon.SetLevel(ToUnit().GetLevel());
            }

            if (AI != null)
                summon.InitializeAI(new CreatureAI(summon));
            return summon;
        }

        public void SummonCreatureGroup(byte group)
        {
            SummonCreatureGroup(group, out _);
        }

        public void SummonCreatureGroup(byte group, out List<TempSummon> list)
        {
            Cypher.Assert((IsTypeId(TypeId.GameObject) || IsTypeId(TypeId.Unit)), "Only GOs and creatures can summon npc groups!");
            list = new List<TempSummon>();
            var data = Global.ObjectMgr.GetSummonGroup(GetEntry(), IsTypeId(TypeId.GameObject) ? SummonerType.GameObject : SummonerType.Creature, group);
            if (data.Empty())
            {
                Log.outWarn(LogFilter.Scripts, "{0} ({1}) tried to summon non-existing summon group {2}.", GetName(), GetGUID().ToString(), group);
                return;
            }

            foreach (var tempSummonData in data)
            {
                var summon = SummonCreature(tempSummonData.entry, tempSummonData.pos, tempSummonData.type, tempSummonData.time);
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

        public Player SelectNearestPlayer(float distance)
        {
            var checker = new NearestPlayerInObjectRangeCheck(this, distance);
            var searcher = new PlayerLastSearcher(this, checker);
            Cell.VisitAllObjects(this, searcher, distance);
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

        public List<Unit> GetPlayerListInGrid(float maxSearchRange)
        {
            var playerList = new List<Unit>();
            var checker = new AnyPlayerInObjectRangeCheck(this, maxSearchRange);
            var searcher = new PlayerListSearcher(this, playerList, checker);

            Cell.VisitWorldObjects(this, searcher, maxSearchRange);
            return playerList;
        }

        public bool IsInPhase(WorldObject obj)
        {
            return GetPhaseShift().CanSee(obj.GetPhaseShift());
        }
        
        public virtual float GetCombatReach() { return 0.0f;    } // overridden (only) in Unit
        public PhaseShift GetPhaseShift() { return _phaseShift; }
        public void SetPhaseShift(PhaseShift phaseShift) { _phaseShift = new PhaseShift(phaseShift); }
        public PhaseShift GetSuppressedPhaseShift() { return _suppressedPhaseShift; }
        public void SetSuppressedPhaseShift(PhaseShift phaseShift) { _suppressedPhaseShift = new PhaseShift(phaseShift); }
        public int GetDBPhase() { return _dbPhase; }

        // if negative it is used as PhaseGroupId
        public void SetDBPhase(int p) { _dbPhase = p; }

        public void PlayDistanceSound(uint soundId, Player target = null)
        {
            var playSpeakerBoxSound = new PlaySpeakerBoxSound(GetGUID(), soundId);
            if (target != null)
                target.SendPacket(playSpeakerBoxSound);
            else
                SendMessageToSet(playSpeakerBoxSound, true);
        }

        public void PlayDirectSound(uint soundId, Player target = null, uint broadcastTextId = 0)
        {
            var sound = new PlaySound(GetGUID(), soundId, broadcastTextId);
            if (target)
                target.SendPacket(sound);
            else
                SendMessageToSet(sound, true);
        }

        public void PlayDirectMusic(uint musicId, Player target = null)
        {
            if (target)
                target.SendPacket(new PlayMusic(musicId));
            else
                SendMessageToSet(new PlayMusic(musicId), true);
        }

        public void DestroyForNearbyPlayers()
        {
            if (!IsInWorld)
                return;

            var targets = new List<Unit>();
            var check = new AnyPlayerInObjectRangeCheck(this, GetVisibilityRange(), false);
            var searcher = new PlayerListSearcher(this, targets, check);

            Cell.VisitWorldObjects(this, searcher, GetVisibilityRange());
            foreach (Player player in targets)
            {
                if (player == this)
                    continue;

                if (!player.HaveAtClient(this))
                    continue;

                if (IsTypeMask(TypeMask.Unit) && (ToUnit().GetCharmerGUID() == player.GetGUID()))// @todo this is for puppet
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

        public virtual string GetName(Locale locale = Locale.enUS) { return _name; }
        public void SetName(string name) { _name = name; }

        public ObjectGuid GetGUID() { return m_guid; }
        public uint GetEntry() { return m_objectData.EntryId; }
        public void SetEntry(uint entry) { SetUpdateFieldValue(m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.EntryId), entry); }

        public float GetObjectScale() { return m_objectData.Scale; }
        public virtual void SetObjectScale(float scale) { SetUpdateFieldValue(m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.Scale), scale); }

        public UnitDynFlags GetDynamicFlags() { return (UnitDynFlags)(uint)m_objectData.DynamicFlags; }
        public bool HasDynamicFlag(UnitDynFlags flag) { return (m_objectData.DynamicFlags & (uint)flag) != 0; }
        public void AddDynamicFlag(UnitDynFlags flag) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.DynamicFlags), (uint)flag); }
        public void RemoveDynamicFlag(UnitDynFlags flag) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.DynamicFlags), (uint)flag); }
        public void SetDynamicFlags(UnitDynFlags flag) { SetUpdateFieldValue(m_values.ModifyValue(m_objectData).ModifyValue(m_objectData.DynamicFlags), (uint)flag); }

        public TypeId GetTypeId() { return ObjectTypeId; }
        public bool IsTypeId(TypeId typeId) { return GetTypeId() == typeId; }
        public bool IsTypeMask(TypeMask mask) { return Convert.ToBoolean(mask & ObjectTypeMask); }

        public virtual bool HasQuest(uint questId) { return false; }
        public virtual bool HasInvolvedQuest(uint questId) { return false; }

        public bool IsCreature() { return GetTypeId() == TypeId.Unit; }
        public bool IsPlayer() { return GetTypeId() == TypeId.Player; }
        public bool IsGameObject() { return GetTypeId() == TypeId.GameObject; }
        public bool IsUnit() { return IsTypeMask(TypeMask.Unit); }
        public bool IsCorpse() { return GetTypeId() == TypeId.Corpse; }
        public bool IsDynObject() { return GetTypeId() == TypeId.DynamicObject; }
        public bool IsAreaTrigger() { return GetTypeId() == TypeId.AreaTrigger; }
        public bool IsConversation() { return GetTypeId() == TypeId.Conversation; }

        public Creature ToCreature() { return IsCreature() ? (this as Creature) : null; }
        public Player ToPlayer() { return IsPlayer() ? (this as Player) : null; }
        public GameObject ToGameObject() { return IsGameObject() ? (this as GameObject) : null; }
        public Unit ToUnit() { return IsUnit() ? (this as Unit) : null; }
        public Corpse ToCorpse() { return IsCorpse() ? (this as Corpse) : null; }
        public DynamicObject ToDynamicObject() { return IsDynObject() ? (this as DynamicObject) : null; }
        public AreaTrigger ToAreaTrigger() { return IsAreaTrigger() ? (this as AreaTrigger) : null; }
        public Conversation ToConversation() { return IsConversation() ? (this as Conversation) : null; }

        public virtual void Update(uint diff) { }

        public virtual uint GetLevelForTarget(WorldObject target) { return 1; }

        public virtual void SaveRespawnTime(uint forceDelay = 0, bool saveToDB = true) { }

        public ZoneScript GetZoneScript() { return m_zoneScript; }

        public void AddToNotify(NotifyFlags f) { m_notifyflags |= f; }
        public bool IsNeedNotify(NotifyFlags f) { return Convert.ToBoolean(m_notifyflags & f); }
        private NotifyFlags GetNotifyFlags() { return m_notifyflags; }
        public void ResetAllNotifies() { m_notifyflags = 0; }

        public bool IsActiveObject() { return m_isActive; }
        public bool IsPermanentWorldObject() { return m_isWorldObject; }

        public Transport GetTransport() { return m_transport; }
        public float GetTransOffsetX() { return m_movementInfo.transport.pos.GetPositionX(); }
        public float GetTransOffsetY() { return m_movementInfo.transport.pos.GetPositionY(); }
        public float GetTransOffsetZ() { return m_movementInfo.transport.pos.GetPositionZ(); }
        public float GetTransOffsetO() { return m_movementInfo.transport.pos.GetOrientation(); }
        private Position GetTransOffset() { return m_movementInfo.transport.pos; }
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

        public virtual bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate) { return true; }

        //Position

        public float GetDistanceZ(WorldObject obj)
        {
            var dz = Math.Abs(GetPositionZ() - obj.GetPositionZ());
            var sizefactor = GetCombatReach() + obj.GetCombatReach();
            var dist = dz - sizefactor;
            return (dist > 0 ? dist : 0);
        }

        public virtual bool _IsWithinDist(WorldObject obj, float dist2compare, bool is3D, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            float sizefactor = 0;
            sizefactor += incOwnRadius ? GetCombatReach() : 0.0f;
            sizefactor += incTargetRadius ? obj.GetCombatReach() : 0.0f;
            var maxdist = dist2compare + sizefactor;

            Position thisOrTransport = this;
            Position objOrObjTransport = obj;

            if (GetTransport() && obj.GetTransport() != null && obj.GetTransport().GetGUID() == GetTransport().GetGUID())
            {
                thisOrTransport = m_movementInfo.transport.pos;
                objOrObjTransport = obj.m_movementInfo.transport.pos;
            }


            if (is3D)
                return thisOrTransport.IsInDist(objOrObjTransport, maxdist);
            else
                return thisOrTransport.IsInDist2d(objOrObjTransport, maxdist);
        }

        public float GetDistance(WorldObject obj)
        {
            var d = GetExactDist(obj.GetPosition()) - GetCombatReach() - obj.GetCombatReach();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance(Position pos)
        {
            var d = GetExactDist(pos) - GetCombatReach();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance(float x, float y, float z)
        {
            var d = GetExactDist(x, y, z) - GetCombatReach();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance2d(WorldObject obj)
        {
            var d = GetExactDist2d(obj.GetPosition()) - GetCombatReach() - obj.GetCombatReach();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance2d(float x, float y)
        {
            var d = GetExactDist2d(x, y) - GetCombatReach();
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
            if (obj != null)
                return IsInWorld && obj.IsInWorld && GetMap().GetId() == obj.GetMap().GetId();

            return false;
        }

        public bool IsWithinDist3d(float x, float y, float z, float dist)
        {
            return IsInDist(x, y, z, dist + GetCombatReach());
        }

        public bool IsWithinDist3d(Position pos, float dist)
        {
            return IsInDist(pos, dist + GetCombatReach());
        }

        public bool IsWithinDist2d(float x, float y, float dist)
        {
            return IsInDist2d(x, y, dist + GetCombatReach());
        }

        public bool IsWithinDist2d(Position pos, float dist)
        {
            return IsInDist2d(pos, dist + GetCombatReach());
        }

        public bool IsWithinDist(WorldObject obj, float dist2compare, bool is3D = true)
        {
            return obj != null && _IsWithinDist(obj, dist2compare, is3D);
        }

        public bool IsWithinDistInMap(WorldObject obj, float dist2compare, bool is3D = true, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            return obj && IsInMap(obj) && IsInPhase(obj) && _IsWithinDist(obj, dist2compare, is3D, incOwnRadius, incTargetRadius);
        }

        public bool IsWithinLOS(float ox, float oy, float oz, LineOfSightChecks checks = LineOfSightChecks.All, ModelIgnoreFlags ignoreFlags = ModelIgnoreFlags.Nothing)
        {
            if (IsInWorld)
            {
                float x, y, z;
                if (IsTypeId(TypeId.Player))
                    GetPosition(out x, out y, out z);
                else
                    GetHitSpherePointFor(new Position(ox, oy, oz), out x, out y, out z);

                return GetMap().IsInLineOfSight(GetPhaseShift(), x, y, z + 2.0f, ox, oy, oz + 2.0f, checks, ignoreFlags);
            }

            return true;
        }

        public bool IsWithinLOSInMap(WorldObject obj, LineOfSightChecks checks = LineOfSightChecks.All, ModelIgnoreFlags ignoreFlags = ModelIgnoreFlags.Nothing)
        {
            if (!IsInMap(obj))
                return false;

            float x, y, z;
            if (obj.IsTypeId(TypeId.Player))
                obj.GetPosition(out x, out y, out z);
            else
                obj.GetHitSpherePointFor(GetPosition(), out x, out y, out z);

            return IsWithinLOS(x, y, z, checks, ignoreFlags);
        }

        private Position GetHitSpherePointFor(Position dest)
        {
            var vThis = new Vector3(GetPositionX(), GetPositionY(), GetPositionZ());
            var vObj = new Vector3(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ());
            var contactPoint = vThis + (vObj - vThis).directionOrZero() * Math.Min(dest.GetExactDist(GetPosition()), GetCombatReach());

            return new Position(contactPoint.X, contactPoint.Y, contactPoint.Z, GetAngle(contactPoint.X, contactPoint.Y));
        }

        private void GetHitSpherePointFor(Position dest, out float x, out float y, out float z)
        {
            var pos = GetHitSpherePointFor(dest);
            x = pos.GetPositionX();
            y = pos.GetPositionY();
            z = pos.GetPositionZ();
        }

        public bool GetDistanceOrder(WorldObject obj1, WorldObject obj2, bool is3D = true)
        {
            var dx1 = GetPositionX() - obj1.GetPositionX();
            var dy1 = GetPositionY() - obj1.GetPositionY();
            var distsq1 = dx1 * dx1 + dy1 * dy1;
            if (is3D)
            {
                var dz1 = GetPositionZ() - obj1.GetPositionZ();
                distsq1 += dz1 * dz1;
            }

            var dx2 = GetPositionX() - obj2.GetPositionX();
            var dy2 = GetPositionY() - obj2.GetPositionY();
            var distsq2 = dx2 * dx2 + dy2 * dy2;
            if (is3D)
            {
                var dz2 = GetPositionZ() - obj2.GetPositionZ();
                distsq2 += dz2 * dz2;
            }

            return distsq1 < distsq2;
        }

        public bool IsInRange(WorldObject obj, float minRange, float maxRange, bool is3D = true)
        {
            var dx = GetPositionX() - obj.GetPositionX();
            var dy = GetPositionY() - obj.GetPositionY();
            var distsq = dx * dx + dy * dy;
            if (is3D)
            {
                var dz = GetPositionZ() - obj.GetPositionZ();
                distsq += dz * dz;
            }

            var sizefactor = GetCombatReach() + obj.GetCombatReach();

            // check only for real range
            if (minRange > 0.0f)
            {
                var mindist = minRange + sizefactor;
                if (distsq < mindist * mindist)
                    return false;
            }

            var maxdist = maxRange + sizefactor;
            return distsq < maxdist * maxdist;
        }

        public bool IsInBetween(WorldObject obj1, WorldObject obj2, float size = 0)
        {
            return obj1 && obj2 && IsInBetween(obj1.GetPosition(), obj2.GetPosition(), size);
        }

        private bool IsInBetween(Position pos1, Position pos2, float size)
        {
            var dist = GetExactDist2d(pos1);

            // not using sqrt() for performance
            if ((dist * dist) >= pos1.GetExactDist2dSq(pos2))
                return false;

            if (size == 0)
                size = GetCombatReach() / 2;

            var angle = pos1.GetAngle(pos2);

            // not using sqrt() for performance
            return (size * size) >= GetExactDist2dSq(pos1.GetPositionX() + (float)Math.Cos(angle) * dist, pos1.GetPositionY() + (float)Math.Sin(angle) * dist);
        }

        public bool IsInFront(WorldObject target, float arc = MathFunctions.PI)
        {
            return HasInArc(arc, target);
        }

        public bool IsInBack(WorldObject target, float arc = MathFunctions.PI)
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
            var angle = (float)RandomHelper.NextDouble() * (2 * MathFunctions.PI);
            var new_dist = (float)RandomHelper.NextDouble() + (float)RandomHelper.NextDouble();
            new_dist = distance * (new_dist > 1 ? new_dist - 2 : new_dist);

            rand_x = (float)(pos.posX + new_dist * Math.Cos(angle));
            rand_y = (float)(pos.posY + new_dist * Math.Sin(angle));
            rand_z = pos.posZ;

            GridDefines.NormalizeMapCoord(ref rand_x);
            GridDefines.NormalizeMapCoord(ref rand_y);
            UpdateGroundPositionZ(rand_x, rand_y, ref rand_z);            // update to LOS height if available
        }

        public Position GetRandomPoint(Position srcPos, float distance)
        {
            float x, y, z;
            GetRandomPoint(srcPos, distance, out x, out y, out z);
            return new Position(x, y, z, GetOrientation());
        }

        public void UpdateGroundPositionZ(float x, float y, ref float z)
        {
            var new_z = GetMap().GetHeight(GetPhaseShift(), x, y, z, true);
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
                            var canSwim = ToCreature().CanSwim();
                            var ground_z = z;
                            var max_z = canSwim
                                ? GetMap().GetWaterOrGroundLevel(GetPhaseShift(), x, y, z, ref ground_z, !ToUnit().HasAuraType(AuraType.WaterWalk))
                                : ((ground_z = GetMap().GetHeight(GetPhaseShift(), x, y, z, true)));
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
                            var ground_z = GetMap().GetHeight(GetPhaseShift(), x, y, z, true);
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
                            var ground_z = z;
                            var max_z = GetMap().GetWaterOrGroundLevel(GetPhaseShift(), x, y, z, ref ground_z, !ToUnit().HasAuraType(AuraType.WaterWalk));
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
                            var ground_z = GetMap().GetHeight(GetPhaseShift(), x, y, z, true);
                            if (z < ground_z)
                                z = ground_z;
                        }
                        break;
                    }
                default:
                    {
                        var ground_z = GetMap().GetHeight(GetPhaseShift(), x, y, z, true);
                        if (ground_z > MapConst.InvalidHeight)
                            z = ground_z;
                        break;
                    }
            }
        }

        public void GetNearPoint2D(out float x, out float y, float distance2d, float absAngle)
        {
            x = (float)(GetPositionX() + (GetCombatReach() + distance2d) * Math.Cos(absAngle));
            y = (float)(GetPositionY() + (GetCombatReach() + distance2d) * Math.Sin(absAngle));

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
            var first_x = x;
            var first_y = y;
            var first_z = z;

            // loop in a circle to look for a point in LoS using small steps
            for (var angle = MathFunctions.PI / 8; angle < Math.PI * 2; angle += MathFunctions.PI / 8)
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
            GetNearPoint(obj, out x, out y, out z, obj.GetCombatReach(), distance2d, GetAngle(obj));
        }

        public void MovePosition(ref Position pos, float dist, float angle)
        {
            angle += GetOrientation();
            var destx = pos.posX + dist * (float)Math.Cos(angle);
            var desty = pos.posY + dist * (float)Math.Sin(angle);

            // Prevent invalid coordinates here, position is unchanged
            if (!GridDefines.IsValidMapCoord(destx, desty, pos.posZ))
            {
                Log.outError(LogFilter.Server, "WorldObject.MovePosition invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);
                return;
            }

            var ground = GetMap().GetHeight(GetPhaseShift(), destx, desty, MapConst.MaxHeight, true);
            var floor = GetMap().GetHeight(GetPhaseShift(), destx, desty, pos.posZ, true);
            var destz = Math.Abs(ground - pos.posZ) <= Math.Abs(floor - pos.posZ) ? ground : floor;

            var step = dist / 10.0f;

            for (byte j = 0; j < 10; ++j)
            {
                // do not allow too big z changes
                if (Math.Abs(pos.posZ - destz) > 6)
                {
                    destx -= step * (float)Math.Cos(angle);
                    desty -= step * (float)Math.Sin(angle);
                    ground = GetMap().GetHeight(GetPhaseShift(), destx, desty, MapConst.MaxHeight, true);
                    floor = GetMap().GetHeight(GetPhaseShift(), destx, desty, pos.posZ, true);
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

        private float NormalizeZforCollision(WorldObject obj, float x, float y, float z)
        {
            var ground = obj.GetMap().GetHeight(obj.GetPhaseShift(), x, y, MapConst.MaxHeight, true);
            var floor = obj.GetMap().GetHeight(obj.GetPhaseShift(), x, y, z + 2.0f, true);
            var helper = Math.Abs(ground - z) <= Math.Abs(floor - z) ? ground : floor;
            if (z > helper) // must be above ground
            {
                var unit = obj.ToUnit();
                if (unit)
                {
                    if (unit.CanFly())
                        return z;
                }
                LiquidData liquid_status;
                var res = obj.GetMap().GetLiquidStatus(obj.GetPhaseShift(), x, y, z, MapConst.MapAllLiquidTypes, out liquid_status);
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
            var destx = pos.posX + dist * (float)Math.Cos(angle);
            var desty = pos.posY + dist * (float)Math.Sin(angle);

            // Prevent invalid coordinates here, position is unchanged
            if (!GridDefines.IsValidMapCoord(destx, desty))
            {
                Log.outError(LogFilter.Server, "WorldObject.MovePositionToFirstCollision invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);
                return;
            }

            var destz = NormalizeZforCollision(this, destx, desty, pos.GetPositionZ());
            var col = Global.VMapMgr.GetObjectHitPos(PhasingHandler.GetTerrainMapId(GetPhaseShift(), GetMap(), pos.posX, pos.posY), pos.posX, pos.posY, pos.posZ + 0.5f, destx, desty, destz + 0.5f, out destx, out desty, out destz, -0.5f);

            // collision occured
            if (col)
            {
                // move back a bit
                destx -= SharedConst.ContactDistance * (float)Math.Cos(angle);
                desty -= SharedConst.ContactDistance * (float)Math.Sin(angle);
                dist = (float)Math.Sqrt((pos.posX - destx) * (pos.posX - destx) + (pos.posY - desty) * (pos.posY - desty));
            }

            // check dynamic collision
            col = GetMap().GetObjectHitPos(GetPhaseShift(), pos.posX, pos.posY, pos.posZ + 0.5f, destx, desty, destz + 0.5f, out destx, out desty, out destz, -0.5f);

            // Collided with a gameobject
            if (col)
            {
                destx -= SharedConst.ContactDistance * (float)Math.Cos(angle);
                desty -= SharedConst.ContactDistance * (float)Math.Sin(angle);
                dist = (float)Math.Sqrt((pos.posX - destx) * (pos.posX - destx) + (pos.posY - desty) * (pos.posY - desty));
            }

            var step = dist / 10.0f;

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

        public float GetFloorZ()
        {
            if (!IsInWorld)
                return m_staticFloorZ;
            return Math.Max(m_staticFloorZ, GetMap().GetGameObjectFloor(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZ()));
        }
        
        public void SetLocationInstanceId(uint _instanceId) { instanceId = _instanceId; }

        #region Fields
        public TypeMask ObjectTypeMask { get; set; }
        protected TypeId ObjectTypeId { get; set; }
        protected CreateObjectBits m_updateFlag;
        private ObjectGuid m_guid;

        public UpdateFieldHolder m_values;
        public ObjectFieldData m_objectData;

        public uint LastUsedScriptID;

        private bool m_objectUpdated;

        private uint m_zoneId;
        private uint m_areaId;
        private float m_staticFloorZ;

        public MovementInfo m_movementInfo;
        private string _name;
        protected bool m_isActive;
        private Optional<float> m_visibilityDistanceOverride;
        private bool m_isWorldObject;
        public ZoneScript m_zoneScript;

        private Transport m_transport;
        private Map _currMap;
        private uint instanceId;
        private PhaseShift _phaseShift= new PhaseShift();
        private PhaseShift _suppressedPhaseShift = new PhaseShift();                   // contains phases for current area but not applied due to conditions
        private int _dbPhase;
        public bool IsInWorld { get; set; }

        private NotifyFlags m_notifyflags;

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
        public ObjectGuid Guid { get; set; }
        private MovementFlag flags;
        private MovementFlag2 flags2;
        public Position Pos { get; set; }
        public uint Time { get; set; }
        public TransportInfo transport;
        public float Pitch { get; set; }
        public JumpInfo jump;
        public float SplineElevation { get; set; }

        public MovementInfo()
        {
            Guid = ObjectGuid.Empty;
            flags = MovementFlag.None;
            flags2 = MovementFlag2.None;
            Time = 0;
            Pitch = 0.0f;

            Pos = new Position();
            transport.Reset();
            jump.Reset();
        }

        public MovementFlag GetMovementFlags() { return flags; }
        public void SetMovementFlags(MovementFlag f) { flags = f; }
        public void AddMovementFlag(MovementFlag f) { flags |= f; }
        public void RemoveMovementFlag(MovementFlag f) { flags &= ~f; }
        public bool HasMovementFlag(MovementFlag f) { return Convert.ToBoolean(flags & f); }

        public MovementFlag2 GetMovementFlags2() { return flags2; }
        public void SetMovementFlags2(MovementFlag2 f) { flags2 = f; }
        public void AddMovementFlag2(MovementFlag2 f) { flags2 |= f; }
        public void RemoveMovementFlag2(MovementFlag2 f) { flags2 &= ~f; }
        public bool HasMovementFlag2(MovementFlag2 f) { return Convert.ToBoolean(flags2 & f); }

        public void SetFallTime(uint time) { jump.fallTime = time; }

        public void ResetTransport()
        {
            transport.Reset();
        }

        public void ResetJump()
        {
            jump.Reset();
        }



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

    public class MovementForce
    {
        public ObjectGuid ID;
        public Vector3 Origin;
        public Vector3 Direction;
        public uint TransportID;
        public float Magnitude;
        public byte Type;

        public void Read(WorldPacket data)
        {
            ID = data.ReadPackedGuid();
            Origin = data.ReadVector3();
            Direction = data.ReadVector3();
            TransportID = data.ReadUInt32();
            Magnitude = data.ReadFloat();
            Type = data.ReadBits<byte>(2);
            
        }

        public void Write(WorldPacket data)
        {
            MovementExtensions.WriteMovementForceWithDirection(this, data);
        }
    }

    public class MovementForces
    {
        private List<MovementForce> _forces = new List<MovementForce>();
        private float _modMagnitude = 1.0f;

        public List<MovementForce> GetForces() { return _forces; }

        public bool Add(MovementForce newForce)
        {
            var movementForce = FindMovementForce(newForce.ID);
            if (movementForce == null)
            {
                _forces.Add(newForce);
                return true;
            }

            return false;
        }

        public bool Remove(ObjectGuid id)
        {
            var movementForce = FindMovementForce(id);
            if (movementForce != null)
            {
                _forces.Remove(movementForce);
                return true;
            }

            return false;
        }

        public float GetModMagnitude() { return _modMagnitude; }
        public void SetModMagnitude(float modMagnitude) { _modMagnitude = modMagnitude; }

        public bool IsEmpty() { return _forces.Empty() && _modMagnitude == 1.0f; }

        private MovementForce FindMovementForce(ObjectGuid id)
        {
            return _forces.Find(force => force.ID == id);
        }
    }

    public struct CreateObjectBits
    {
        public bool NoBirthAnim;
        public bool EnablePortals;
        public bool PlayHoverAnim;
        public bool MovementUpdate;
        public bool MovementTransport;
        public bool Stationary;
        public bool CombatVictim;
        public bool ServerTime;
        public bool Vehicle;
        public bool AnimKit;
        public bool Rotation;
        public bool AreaTrigger;
        public bool GameObject;
        public bool SmoothPhasing;
        public bool ThisIsYou;
        public bool SceneObject;
        public bool ActivePlayer;
        public bool Conversation;

        public void Clear()
        {
            NoBirthAnim = false;
            EnablePortals = false;
            PlayHoverAnim = false;
            MovementUpdate = false;
            MovementTransport = false;
            Stationary = false;
            CombatVictim = false;
            ServerTime = false;
            Vehicle = false;
            AnimKit = false;
            Rotation = false;
            AreaTrigger = false;
            GameObject = false;
            SmoothPhasing = false;
            ThisIsYou = false;
            SceneObject = false;
            ActivePlayer = false;
            Conversation = false;
        }
    }
}