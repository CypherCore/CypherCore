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
using Game.BattleFields;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scenarios;
using System;
using System.Collections.Generic;
using Game.DataStorage;
using Game.Spells;

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
            PositionFullTerrainStatus data = new();
            GetMap().GetFullTerrainStatusForPosition(_phaseShift, GetPositionX(), GetPositionY(), GetPositionZ(), data, LiquidHeaderTypeFlags.AllLiquids);
            ProcessPositionDataChanged(data);
        }

        public virtual void ProcessPositionDataChanged(PositionFullTerrainStatus data)
        {
            m_zoneId = m_areaId = data.AreaId;

            var area = CliDB.AreaTableStorage.LookupByKey(m_areaId);
            if (area != null)
                if (area.ParentAreaID != 0)
                    m_zoneId = area.ParentAreaID;

            m_outdoors = data.outdoors;
            m_staticFloorZ = data.FloorZ;
        }

        public virtual void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
        {
            if (!target)
                return;

            UpdateType updateType = UpdateType.CreateObject;
            TypeId tempObjectType = ObjectTypeId;
            CreateObjectBits flags = m_updateFlag;

            if (target == this)
            {
                flags.ThisIsYou = true;
                flags.ActivePlayer = true;
                tempObjectType = TypeId.ActivePlayer;
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
            Unit unit = ToUnit();
            if (unit)
                if (unit.GetVictim())
                    flags.CombatVictim = true;

            WorldPacket buffer = new();
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
            UpdateData upd = new(player.GetMapId());
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
            WorldPacket buffer = new();
            buffer.WriteUInt8((byte)UpdateType.Values);
            buffer.WritePackedGuid(GetGUID());

            BuildValuesUpdate(buffer, target);

            data.AddUpdateBlock(buffer);
        }

        public void BuildValuesUpdateBlockForPlayerWithFlag(UpdateData data, UpdateFieldFlag flags, Player target)
        {
            WorldPacket buffer = new();
            buffer.WriteUInt8((byte)UpdateType.Values);
            buffer.WritePackedGuid(GetGUID());

            BuildValuesUpdateWithFlag(buffer, flags, target);

            data.AddUpdateBlock(buffer);
        }

        void BuildDestroyUpdateBlock(UpdateData data)
        {
            data.AddDestroyObject(GetGUID());
        }

        public void BuildOutOfRangeUpdateBlock(UpdateData data)
        {
            data.AddOutOfRangeGUID(GetGUID());
        }

        public virtual void DestroyForPlayer(Player target)
        {
            UpdateData updateData = new(target.GetMapId());
            BuildDestroyUpdateBlock(updateData);
            UpdateObject packet;
            updateData.BuildPacket(out packet);
            target.SendPacket(packet);
        }

        public void BuildMovementUpdate(WorldPacket data, CreateObjectBits flags)
        {
            int PauseTimesCount = 0;

            GameObject go = ToGameObject();
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

                MovementForces movementForces = unit.GetMovementForces();
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
                    foreach (MovementForce force in movementForces.GetForces())
                        MovementExtensions.WriteMovementForceWithDirection(force, data, unit);

                // HasMovementSpline - marks that spline data is present in packet
                if (HasSpline)
                    MovementExtensions.WriteCreateObjectSplineDataBlock(unit.MoveSpline, data);
            }

            data.WriteInt32(PauseTimesCount);

            if (flags.Stationary)
            {
                WorldObject self = this;
                data.WriteFloat(self.GetStationaryX());
                data.WriteFloat(self.GetStationaryY());
                data.WriteFloat(self.GetStationaryZ());
                data.WriteFloat(self.GetStationaryO());
            }

            if (flags.CombatVictim)
                data.WritePackedGuid(ToUnit().GetVictim().GetGUID());                      // CombatVictim

            if (flags.ServerTime)
            {
                GameObject go1 = ToGameObject();
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
                Unit unit = ToUnit();
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
                for (int i = 0; i < PauseTimesCount; ++i)
                    data.WriteUInt32(go.GetGoValue().Transport.StopFrames[i]);
            }

            if (flags.MovementTransport)
            {
                WorldObject self = this;
                MovementExtensions.WriteTransportInfo(data, self.m_movementInfo.transport);
            }

            if (flags.AreaTrigger)
            {
                AreaTrigger areaTrigger = ToAreaTrigger();
                AreaTriggerCreateProperties createProperties = areaTrigger.GetCreateProperties();
                AreaTriggerTemplate areaTriggerTemplate = areaTrigger.GetTemplate();
                AreaTriggerShapeInfo shape = areaTrigger.GetShape();

                data.WriteUInt32(areaTrigger.GetTimeSinceCreated());

                data.WriteVector3(areaTrigger.GetRollPitchYaw());

                bool hasAbsoluteOrientation = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAbsoluteOrientation);
                bool hasDynamicShape = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasDynamicShape);
                bool hasAttached = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasAttached);
                bool hasFaceMovementDir = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFaceMovementDir);
                bool hasFollowsTerrain = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasFollowsTerrain);
                bool hasUnk1 = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk1);
                bool hasTargetRollPitchYaw = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.HasTargetRollPitchYaw);
                bool hasScaleCurveID = createProperties != null && createProperties.ScaleCurveId != 0;
                bool hasMorphCurveID = createProperties != null && createProperties.MorphCurveId != 0;
                bool hasFacingCurveID = createProperties != null && createProperties.FacingCurveId != 0;
                bool hasMoveCurveID = createProperties != null && createProperties.MoveCurveId != 0;
                bool hasAnimation = createProperties != null && createProperties.AnimId != 0;
                bool hasUnk3 = areaTriggerTemplate != null && areaTriggerTemplate.HasFlag(AreaTriggerFlags.Unk3);
                bool hasAnimKitID = createProperties != null && createProperties.AnimKitId != 0;
                bool hasAnimProgress = false;
                bool hasAreaTriggerSphere = shape.IsSphere();
                bool hasAreaTriggerBox = shape.IsBox();
                bool hasAreaTriggerPolygon = createProperties != null && shape.IsPolygon();
                bool hasAreaTriggerCylinder = shape.IsCylinder();
                bool hasAreaTriggerSpline = areaTrigger.HasSplines();
                bool hasOrbit = areaTrigger.HasOrbit();
                bool hasMovementScript = false;

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
                    data.WriteBit(false);

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
                    data.WriteUInt32(createProperties.ScaleCurveId);

                if (hasMorphCurveID)
                    data.WriteUInt32(createProperties.MorphCurveId);

                if (hasFacingCurveID)
                    data.WriteUInt32(createProperties.FacingCurveId);

                if (hasMoveCurveID)
                    data.WriteUInt32(createProperties.MoveCurveId);

                if (hasAnimation)
                    data.WriteUInt32(createProperties.AnimId);

                if (hasAnimKitID)
                    data.WriteUInt32(createProperties.AnimKitId);

                if (hasAnimProgress)
                    data.WriteUInt32(0);

                if (hasAreaTriggerSphere)
                {
                    data.WriteFloat(shape.SphereDatas.Radius);
                    data.WriteFloat(shape.SphereDatas.RadiusTarget);
                }

                if (hasAreaTriggerBox)
                {
                    unsafe
                    {
                        data.WriteFloat(shape.BoxDatas.Extents[0]);
                        data.WriteFloat(shape.BoxDatas.Extents[1]);
                        data.WriteFloat(shape.BoxDatas.Extents[2]);

                        data.WriteFloat(shape.BoxDatas.ExtentsTarget[0]);
                        data.WriteFloat(shape.BoxDatas.ExtentsTarget[1]);
                        data.WriteFloat(shape.BoxDatas.ExtentsTarget[2]);
                    }
                }

                if (hasAreaTriggerPolygon)
                {
                    data.WriteInt32(createProperties.PolygonVertices.Count);
                    data.WriteInt32(createProperties.PolygonVerticesTarget.Count);
                    data.WriteFloat(shape.PolygonDatas.Height);
                    data.WriteFloat(shape.PolygonDatas.HeightTarget);

                    foreach (var vertice in createProperties.PolygonVertices)
                        data.WriteVector2(vertice);

                    foreach (var vertice in createProperties.PolygonVerticesTarget)
                        data.WriteVector2(vertice);
                }

                if (hasAreaTriggerCylinder)
                {
                    data.WriteFloat(shape.CylinderDatas.Radius);
                    data.WriteFloat(shape.CylinderDatas.RadiusTarget);
                    data.WriteFloat(shape.CylinderDatas.Height);
                    data.WriteFloat(shape.CylinderDatas.HeightTarget);
                    data.WriteFloat(shape.CylinderDatas.LocationZOffset);
                    data.WriteFloat(shape.CylinderDatas.LocationZOffsetTarget);
                }

                //if (hasMovementScript)
                //    *data << *areaTrigger.GetMovementScript(); // AreaTriggerMovementScriptInfo

                if (hasOrbit)
                    areaTrigger.GetCircularMovementInfo().Value.Write(data);
            }

            if (flags.GameObject)
            {
                bool bit8 = false;
                uint Int1 = 0;

                GameObject gameObject = ToGameObject();

                data.WriteUInt32(gameObject.GetWorldEffectID());

                data.WriteBit(bit8);
                data.FlushBits();
                if (bit8)
                    data.WriteUInt32(Int1);
            }

            //if (flags.SmoothPhasing)
            //{
            //    data.WriteBit(ReplaceActive);
            //    data.WriteBit(StopAnimKits);
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

            //                *data << uint(Players[i].Pets[j].Abilities.size());
            //                *data << uint(Players[i].Pets[j].Auras.size());
            //                *data << uint(Players[i].Pets[j].States.size());
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
            //                    *data << uint(Players[i].Pets[j].Auras[k].InstanceID);
            //                    *data << int32(Players[i].Pets[j].Auras[k].RoundsRemaining);
            //                    *data << int32(Players[i].Pets[j].Auras[k].CurrentRound);
            //                    *data << uint8(Players[i].Pets[j].Auras[k].CasterPBOID);
            //                }

            //                for (std::size_t k = 0; k < Players[i].Pets[j].States.size(); ++k)
            //                {
            //                    *data << uint(Players[i].Pets[j].States[k].StateID);
            //                    *data << int32(Players[i].Pets[j].States[k].StateValue);
            //                }

            //                data.WriteBits(Players[i].Pets[j].CustomName.length(), 7);
            //                data.FlushBits();
            //                data.WriteString(Players[i].Pets[j].CustomName);
            //            }
            //        }

            //        for (std::size_t i = 0; i < 3; ++i)
            //        {
            //            *data << uint(Enviros[j].Auras.size());
            //            *data << uint(Enviros[j].States.size());
            //            for (std::size_t j = 0; j < Enviros[j].Auras.size(); ++j)
            //            {
            //                *data << int32(Enviros[j].Auras[j].AbilityID);
            //                *data << uint(Enviros[j].Auras[j].InstanceID);
            //                *data << int32(Enviros[j].Auras[j].RoundsRemaining);
            //                *data << int32(Enviros[j].Auras[j].CurrentRound);
            //                *data << uint8(Enviros[j].Auras[j].CasterPBOID);
            //            }

            //            for (std::size_t j = 0; j < Enviros[j].States.size(); ++j)
            //            {
            //                *data << uint(Enviros[i].States[j].StateID);
            //                *data << int32(Enviros[i].States[j].StateValue);
            //            }
            //        }

            //        *data << uint16(WaitingForFrontPetsMaxSecs);
            //        *data << uint16(PvpMaxRoundTime);
            //        *data << int32(CurRound);
            //        *data << uint(NpcCreatureID);
            //        *data << uint(NpcDisplayID);
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
                bool HasSceneInstanceIDs = false;
                bool HasRuneState = ToUnit().GetPowerIndex(PowerType.Runes) != (int)PowerType.Max;

                data.WriteBit(HasSceneInstanceIDs);
                data.WriteBit(HasRuneState);
                data.FlushBits();
                //if (HasSceneInstanceIDs)
                //{
                //    *data << uint(SceneInstanceIDs.size());
                //    for (std::size_t i = 0; i < SceneInstanceIDs.size(); ++i)
                //        *data << uint(SceneInstanceIDs[i]);
                //}
                if (HasRuneState)
                {
                    Player player = ToPlayer();
                    float baseCd = player.GetRuneBaseCooldown();
                    uint maxRunes = (uint)player.GetMaxPower(PowerType.Runes);

                    data.WriteUInt8((byte)((1 << (int)maxRunes) - 1u));
                    data.WriteUInt8(player.GetRunesState());
                    data.WriteUInt32(maxRunes);
                    for (byte i = 0; i < maxRunes; ++i)
                        data.WriteUInt8((byte)((baseCd - (float)player.GetRuneCooldown(i)) / baseCd * 255));
                }
            }

            if (flags.Conversation)
            {
                Conversation self = ToConversation();
                if (data.WriteBit(self.GetTextureKitId() != 0))
                    data.WriteUInt32(self.GetTextureKitId());
                data.FlushBits();
            }
        }

        public void DoWithSuppressingObjectUpdates(Action action)
        {
            bool wasUpdatedBeforeAction = m_objectUpdated;
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
            SetUpdateFieldValue(updateField, (T)Math.Max((dynamic)value, 0));
        }

        public void SetUpdateFieldStatValue<T>(ref T oldValue, T value) where T : new()
        {
            SetUpdateFieldValue(ref oldValue, (T)Math.Max((dynamic)value, 0));
        }

        public void ApplyModUpdateFieldValue<T>(IUpdateField<T> updateField, T mod, bool apply) where T : new()
        {
            dynamic value = updateField.GetValue();
            if (apply)
                value += mod;
            else
                value -= mod;

            SetUpdateFieldValue(updateField, (T)value);
        }

        public void ApplyModUpdateFieldValue<T>(ref T oldvalue, T mod, bool apply) where T : new()
        {
            dynamic value = oldvalue;
            if (apply)
                value += mod;
            else
                value -= mod;

            SetUpdateFieldValue(ref oldvalue, (T)value);
        }

        public void ApplyPercentModUpdateFieldValue<T>(IUpdateField<T> updateField, float percent, bool apply) where T : new()
        {
            dynamic value = updateField.GetValue();

            if (percent == -100.0f)
                percent = -99.99f;
            value *= (apply ? (100.0f + percent) / 100.0f : 100.0f / (100.0f + percent));

            SetUpdateFieldValue(updateField, (T)value);
        }

        public void ApplyPercentModUpdateFieldValue<T>(ref T oldValue, float percent, bool apply) where T : new()
        {
            dynamic value = oldValue;

            if (percent == -100.0f)
                percent = -99.99f;
            value *= (apply ? (100.0f + percent) / 100.0f : 100.0f / (100.0f + percent));

            SetUpdateFieldValue(ref oldValue, (T)value);
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

        bool IsFarVisible() { return m_isFarVisible; }

        public void SetFarVisible(bool on)
        {
            if (IsPlayer())
                return;

            m_isFarVisible = on;
        }

        bool IsVisibilityOverridden() { return m_visibilityDistanceOverride.HasValue; }

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

            Transport transport = GetTransport();
            if (transport)
                transport.RemovePassenger(this);
        }

        public uint GetZoneId() { return m_zoneId; }
        public uint GetAreaId() { return m_areaId; }

        public void GetZoneAndAreaId(out uint zoneid, out uint areaid) { zoneid = m_zoneId; areaid = m_areaId; }

        public bool IsOutdoors() { return m_outdoors; }

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
            Map map = GetMap();
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
            Creature thisCreature = ToCreature();
            if (thisCreature != null)
                return thisCreature.m_SightDistance;

            return 0.0f;
        }

        public float GetVisibilityRange()
        {
            if (IsVisibilityOverridden() && !IsPlayer())
                return m_visibilityDistanceOverride.Value;
            else if (IsFarVisible() && !IsPlayer())
                return SharedConst.MaxVisibilityDistance;
            else
                return GetMap().GetVisibilityRange();
        }

        public float GetSightRange(WorldObject target = null)
        {
            if (IsPlayer() || IsCreature())
            {
                if (IsPlayer())
                {
                    if (target != null && target.IsVisibilityOverridden() && !target.IsPlayer())
                        return target.m_visibilityDistanceOverride.Value;
                    else if (target != null && target.IsFarVisible() && !target.IsPlayer())
                        return SharedConst.MaxVisibilityDistance;
                    else if (ToPlayer().GetCinematicMgr().IsOnCinematic())
                        return SharedConst.DefaultVisibilityInstance;
                    else
                        return GetMap().GetVisibilityRange();
                }
                else if (IsCreature())
                    return ToCreature().m_SightDistance;
                else
                    return SharedConst.SightRangeUnit;
            }

            if (IsDynObject() && IsActiveObject())
            {
                return GetMap().GetVisibilityRange();
            }

            return 0.0f;
        }

        public bool CheckPrivateObjectOwnerVisibility(WorldObject seer)
        {
            if (!IsPrivateObject())
                return true;

            // Owner of this private object
            if (_privateObjectOwner == seer.GetGUID())
                return true;

            // Another private object of the same owner
            if (_privateObjectOwner == seer.GetPrivateObjectOwner())
                return true;

            Player playerSeer = seer.ToPlayer();
            if (playerSeer != null)
                if (playerSeer.IsInGroup(_privateObjectOwner))
                    return true;

            return false;
        }

        public bool CanSeeOrDetect(WorldObject obj, bool ignoreStealth = false, bool distanceCheck = false, bool checkAlert = false)
        {
            if (this == obj)
                return true;

            if (obj.IsNeverVisibleFor(this) || CanNeverSee(obj))
                return false;

            if (obj.IsAlwaysVisibleFor(this) || CanAlwaysSee(obj))
                return true;

            if (!obj.CheckPrivateObjectOwnerVisibility(this))
                return false;

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

                    Unit target = obj.ToUnit();
                    if (target)
                    {
                        // Don't allow to detect vehicle accessories if you can't see vehicle
                        Unit vehicle = target.GetVehicleBase();
                        if (vehicle)
                            if (!thisPlayer.HaveAtClient(vehicle))
                                return false;
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

            GameObject go = obj.ToGameObject();
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

        public virtual void SendMessageToSet(ServerPacket packet, bool self)
        {
            if (IsInWorld)
                SendMessageToSetInRange(packet, GetVisibilityRange(), self);
        }

        public virtual void SendMessageToSetInRange(ServerPacket data, float dist, bool self)
        {
            PacketSenderRef sender = new(data);
            MessageDistDeliverer<PacketSenderRef> notifier = new(this, sender, dist);
            Cell.VisitWorldObjects(this, notifier, dist);
        }

        public virtual void SendMessageToSet(ServerPacket data, Player skip)
        {
            PacketSenderRef sender = new(data);
            var notifier = new MessageDistDeliverer<PacketSenderRef>(this, sender, GetVisibilityRange(), false, skip);
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
        }

        public void SendCombatLogMessage(CombatLogServerPacket combatLog)
        {
            CombatLogSender combatLogSender = new(combatLog);

            Player self = ToPlayer();
            if (self != null)
                combatLogSender.Invoke(self);

            MessageDistDeliverer<CombatLogSender> notifier = new(this, combatLogSender, GetVisibilityRange());
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

        public TempSummon SummonCreature(uint entry, float x, float y, float z, float o = 0, TempSummonType despawnType = TempSummonType.ManualDespawn, uint despawnTime = 0, ObjectGuid privateObjectOwner = default)
        {
            if (x == 0.0f && y == 0.0f && z == 0.0f)
                GetClosePoint(out x, out y, out z, GetCombatReach());

            if (o == 0.0f)
                o = GetOrientation();

            return SummonCreature(entry, new Position(x, y, z, o), despawnType, despawnTime, 0, 0, privateObjectOwner);
        }

        public TempSummon SummonCreature(uint entry, Position pos, TempSummonType despawnType = TempSummonType.ManualDespawn, uint despawnTime = 0, uint vehId = 0, uint spellId = 0, ObjectGuid privateObjectOwner = default)
        {
            Map map = GetMap();
            if (map != null)
            {
                TempSummon summon = map.SummonCreature(entry, pos, null, despawnTime, ToUnit(), spellId, vehId, privateObjectOwner);
                if (summon != null)
                {
                    summon.SetTempSummonType(despawnType);
                    return summon;
                }
            }

            return null;
        }

        public GameObject SummonGameObject(uint entry, float x, float y, float z, float ang, Quaternion rotation, uint respawnTime, GameObjectSummonType summonType = GameObjectSummonType.TimedOrCorpseDespawn)
        {
            if (x == 0 && y == 0 && z == 0)
            {
                GetClosePoint(out x, out y, out z, GetCombatReach());
                ang = GetOrientation();
            }

            Position pos = new(x, y, z, ang);
            return SummonGameObject(entry, pos, rotation, respawnTime, summonType);
        }

        public GameObject SummonGameObject(uint entry, Position pos, Quaternion rotation, uint respawnTime, GameObjectSummonType summonType = GameObjectSummonType.TimedOrCorpseDespawn)
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
            GameObject go = GameObject.CreateGameObject(entry, map, pos, rotation, 255, GameObjectState.Ready);
            if (!go)
                return null;

            PhasingHandler.InheritPhaseShift(go, this);

            go.SetRespawnTime((int)respawnTime);
            if (IsPlayer() || (IsCreature() && summonType == GameObjectSummonType.TimedOrCorpseDespawn)) //not sure how to handle this
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

        public Player SelectNearestPlayer(float distance)
        {
            var checker = new NearestPlayerInObjectRangeCheck(this, distance);
            var searcher = new PlayerLastSearcher(this, checker);
            Cell.VisitAllObjects(this, searcher, distance);
            return searcher.GetTarget();
        }

        public ObjectGuid GetCharmerOrOwnerOrOwnGUID()
        {
            ObjectGuid guid = GetCharmerOrOwnerGUID();
            if (!guid.IsEmpty())
                return guid;
            return GetGUID();
        }

        public virtual Unit GetOwner()
        {
            return Global.ObjAccessor.GetUnit(this, GetOwnerGUID());
        }

        public virtual Unit GetCharmerOrOwner()
        {
            Unit unit = ToUnit();
            if (unit != null)
                return unit.GetCharmerOrOwner();
            else
            {
                GameObject go = ToGameObject();
                if (go != null)
                    return go.GetOwner();
            }

            return null;
        }

        public Unit GetCharmerOrOwnerOrSelf()
        {
            Unit u = GetCharmerOrOwner();
            if (u != null)
                return u;

            return ToUnit();
        }

        public Player GetCharmerOrOwnerPlayerOrPlayerItself()
        {
            ObjectGuid guid = GetCharmerOrOwnerGUID();
            if (guid.IsPlayer())
                return Global.ObjAccessor.GetPlayer(this, guid);

            return ToPlayer();
        }

        public Player GetAffectingPlayer()
        {
            if (GetCharmerOrOwnerGUID().IsEmpty())
                return ToPlayer();

            Unit owner = GetCharmerOrOwner();
            if (owner != null)
                return owner.GetCharmerOrOwnerPlayerOrPlayerItself();

            return null;
        }

        public Player GetSpellModOwner()
        {
            Player player = ToPlayer();
            if (player != null)
                return player;

            if (IsCreature())
            {
                Creature creature = ToCreature();
                if (creature.IsPet() || creature.IsTotem())
                {
                    Unit owner = creature.GetOwner();
                    if (owner != null)
                        return owner.ToPlayer();
                }
            }
            else if (IsGameObject())
            {
                GameObject go = ToGameObject();
                Unit owner = go.GetOwner();
                if (owner != null)
                    return owner.ToPlayer();
            }

            return null;
        }
        public int CalculateSpellDamage(Unit target, SpellEffectInfo spellEffectInfo, int? basePoints = null, uint castItemId = 0, int itemLevel = -1)
        {
            return CalculateSpellDamage(out _, target, spellEffectInfo, basePoints, castItemId, itemLevel);
        }

        // function uses real base points (typically value - 1)
        public int CalculateSpellDamage(out float variance, Unit target, SpellEffectInfo spellEffectInfo, int? basePoints = null, uint castItemId = 0, int itemLevel = -1)
        {
            variance = 0.0f;

            return spellEffectInfo != null ? spellEffectInfo.CalcValue(out variance, this, basePoints, target, castItemId, itemLevel) : 0;
        }

        public float GetSpellMaxRangeForTarget(Unit target, SpellInfo spellInfo)
        {
            if (spellInfo.RangeEntry == null)
                return 0.0f;

            if (spellInfo.RangeEntry.RangeMax[0] == spellInfo.RangeEntry.RangeMax[1])
                return spellInfo.GetMaxRange();

            if (!target)
                return spellInfo.GetMaxRange(true);

            return spellInfo.GetMaxRange(!IsHostileTo(target));
        }

        public float GetSpellMinRangeForTarget(Unit target, SpellInfo spellInfo)
        {
            if (spellInfo.RangeEntry == null)
                return 0.0f;

            if (spellInfo.RangeEntry.RangeMin[0] == spellInfo.RangeEntry.RangeMin[1])
                return spellInfo.GetMinRange();

            if (!target)
                return spellInfo.GetMinRange(true);

            return spellInfo.GetMinRange(!IsHostileTo(target));
        }

        public float ApplyEffectModifiers(SpellInfo spellInfo, uint effIndex, float value)
        {
            Player modOwner = GetSpellModOwner();
            if (modOwner != null)
            {
                modOwner.ApplySpellMod(spellInfo, SpellModOp.Points, ref value);
                switch (effIndex)
                {
                    case 0:
                        modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex0, ref value);
                        break;
                    case 1:
                        modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex1, ref value);
                        break;
                    case 2:
                        modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex2, ref value);
                        break;
                    case 3:
                        modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex3, ref value);
                        break;
                    case 4:
                        modOwner.ApplySpellMod(spellInfo, SpellModOp.PointsIndex4, ref value);
                        break;
                }
            }
            return value;
        }

        public int CalcSpellDuration(SpellInfo spellInfo)
        {
            int comboPoints = 0;
            Unit unit = ToUnit();
            if (unit != null)
                comboPoints = unit.GetPower(PowerType.ComboPoints);

            int minduration = spellInfo.GetDuration();
            int maxduration = spellInfo.GetMaxDuration();

            int duration;
            if (comboPoints != 0 && minduration != -1 && minduration != maxduration)
                duration = minduration + ((maxduration - minduration) * comboPoints / 5);
            else
                duration = minduration;

            return duration;
        }

        public int ModSpellDuration(SpellInfo spellInfo, WorldObject target, int duration, bool positive, uint effectMask)
        {
            // don't mod permanent auras duration
            if (duration < 0)
                return duration;

            // some auras are not affected by duration modifiers
            if (spellInfo.HasAttribute(SpellAttr7.IgnoreDurationMods))
                return duration;

            // cut duration only of negative effects
            Unit unitTarget = target.ToUnit();
            if (!unitTarget)
                return duration;

            if (!positive)
            {
                uint mechanicMask = spellInfo.GetSpellMechanicMaskByEffectMask(effectMask);
                bool mechanicCheck(AuraEffect aurEff)
                {
                    if ((mechanicMask & (1 << aurEff.GetMiscValue())) != 0)
                        return true;
                    return false;
                }

                // Find total mod value (negative bonus)
                int durationMod_always = unitTarget.GetTotalAuraModifier(AuraType.MechanicDurationMod, mechanicCheck);
                // Find max mod (negative bonus)
                int durationMod_not_stack = unitTarget.GetMaxNegativeAuraModifier(AuraType.MechanicDurationModNotStack, mechanicCheck);

                // Select strongest negative mod
                int durationMod = Math.Min(durationMod_always, durationMod_not_stack);
                if (durationMod != 0)
                    MathFunctions.AddPct(ref duration, durationMod);

                // there are only negative mods currently
                durationMod_always = unitTarget.GetTotalAuraModifierByMiscValue(AuraType.ModAuraDurationByDispel, (int)spellInfo.Dispel);
                durationMod_not_stack = unitTarget.GetMaxNegativeAuraModifierByMiscValue(AuraType.ModAuraDurationByDispelNotStack, (int)spellInfo.Dispel);

                durationMod = Math.Min(durationMod_always, durationMod_not_stack);
                if (durationMod != 0)
                    MathFunctions.AddPct(ref duration, durationMod);
            }
            else
            {
                // else positive mods here, there are no currently
                // when there will be, change GetTotalAuraModifierByMiscValue to GetMaxPositiveAuraModifierByMiscValue

                // Mixology - duration boost
                if (unitTarget.IsPlayer())
                {
                    if (spellInfo.SpellFamilyName == SpellFamilyNames.Potion && (
                        Global.SpellMgr.IsSpellMemberOfSpellGroup(spellInfo.Id, SpellGroup.ElixirBattle) ||
                        Global.SpellMgr.IsSpellMemberOfSpellGroup(spellInfo.Id, SpellGroup.ElixirGuardian)))
                    {
                        SpellEffectInfo effect = spellInfo.GetEffect(0);
                        if (unitTarget.HasAura(53042) && effect != null && unitTarget.HasSpell(effect.TriggerSpell))
                            duration *= 2;
                    }
                }
            }

            return Math.Max(duration, 0);
        }

        public void ModSpellCastTime(SpellInfo spellInfo, ref int castTime, Spell spell = null)
        {
            if (spellInfo == null || castTime < 0)
                return;

            // called from caster
            Player modOwner = GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(spellInfo, SpellModOp.ChangeCastTime, ref castTime, spell);

            Unit unitCaster = ToUnit();
            if (!unitCaster)
                return;

            if (!(spellInfo.HasAttribute(SpellAttr0.Ability) || spellInfo.HasAttribute(SpellAttr0.Tradespell) || spellInfo.HasAttribute(SpellAttr3.NoDoneBonus)) &&
                ((IsPlayer() && spellInfo.SpellFamilyName != 0) || IsCreature()))
                castTime = unitCaster.CanInstantCast() ? 0 : (int)(castTime * unitCaster.m_unitData.ModCastingSpeed);
            else if (spellInfo.HasAttribute(SpellAttr0.ReqAmmo) && !spellInfo.HasAttribute(SpellAttr2.AutorepeatFlag))
                castTime = (int)(castTime * unitCaster.m_modAttackSpeedPct[(int)WeaponAttackType.RangedAttack]);
            else if (Global.SpellMgr.IsPartOfSkillLine(SkillType.Cooking, spellInfo.Id) && unitCaster.HasAura(67556)) // cooking with Chef Hat.
                castTime = 500;
        }

        public void ModSpellDurationTime(SpellInfo spellInfo, ref int duration, Spell spell = null)
        {
            if (spellInfo == null || duration < 0)
                return;

            if (spellInfo.IsChanneled() && !spellInfo.HasAttribute(SpellAttr5.HasteAffectDuration))
                return;

            // called from caster
            Player modOwner = GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(spellInfo, SpellModOp.ChangeCastTime, ref duration, spell);

            Unit unitCaster = ToUnit();
            if (!unitCaster)
                return;

            if (!(spellInfo.HasAttribute(SpellAttr0.Ability) || spellInfo.HasAttribute(SpellAttr0.Tradespell) || spellInfo.HasAttribute(SpellAttr3.NoDoneBonus)) &&
                ((IsPlayer() && spellInfo.SpellFamilyName != 0) || IsCreature()))
                duration = (int)(duration * unitCaster.m_unitData.ModCastingSpeed);
            else if (spellInfo.HasAttribute(SpellAttr0.ReqAmmo) && !spellInfo.HasAttribute(SpellAttr2.AutorepeatFlag))
                duration = (int)(duration * unitCaster.m_modAttackSpeedPct[(int)WeaponAttackType.RangedAttack]);
        }

        public virtual float MeleeSpellMissChance(Unit victim, WeaponAttackType attType, SpellInfo spellInfo)
        {
            return 0.0f;
        }

        public virtual SpellMissInfo MeleeSpellHitResult(Unit victim, SpellInfo spellInfo)
        {
            return SpellMissInfo.None;
        }

        SpellMissInfo MagicSpellHitResult(Unit victim, SpellInfo spellInfo)
        {
            // Can`t miss on dead target (on skinning for example)
            if (!victim.IsAlive() && !victim.IsPlayer())
                return SpellMissInfo.None;

            SpellSchoolMask schoolMask = spellInfo.GetSchoolMask();
            // PvP - PvE spell misschances per leveldif > 2
            int lchance = victim.IsPlayer() ? 7 : 11;
            uint thisLevel = GetLevelForTarget(victim);
            if (IsCreature() && ToCreature().IsTrigger())
                thisLevel = Math.Max(thisLevel, spellInfo.SpellLevel);
            int leveldif = (int)(victim.GetLevelForTarget(this) - thisLevel);
            int levelBasedHitDiff = leveldif;

            // Base hit chance from attacker and victim levels
            int modHitChance = 100;
            if (levelBasedHitDiff >= 0)
            {
                if (!victim.IsPlayer())
                {
                    modHitChance = 94 - 3 * Math.Min(levelBasedHitDiff, 3);
                    levelBasedHitDiff -= 3;
                }
                else
                {
                    modHitChance = 96 - Math.Min(levelBasedHitDiff, 2);
                    levelBasedHitDiff -= 2;
                }
                if (levelBasedHitDiff > 0)
                    modHitChance -= lchance * Math.Min(levelBasedHitDiff, 7);
            }
            else
                modHitChance = 97 - levelBasedHitDiff;

            // Spellmod from SpellModOp::HitChance
            Player modOwner = GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(spellInfo, SpellModOp.HitChance, ref modHitChance);

            // Spells with SPELL_ATTR3_IGNORE_HIT_RESULT will ignore target's avoidance effects
            if (!spellInfo.HasAttribute(SpellAttr3.IgnoreHitResult))
            {
                // Chance hit from victim SPELL_AURA_MOD_ATTACKER_SPELL_HIT_CHANCE auras
                modHitChance += victim.GetTotalAuraModifierByMiscMask(AuraType.ModAttackerSpellHitChance, (int)schoolMask);
            }

            int HitChance = modHitChance * 100;
            // Increase hit chance from attacker SPELL_AURA_MOD_SPELL_HIT_CHANCE and attacker ratings
            Unit unit = ToUnit();
            if (unit != null)
                HitChance += (int)(unit.ModSpellHitChance * 100.0f);

            MathFunctions.RoundToInterval(ref HitChance, 0, 10000);

            int tmp = 10000 - HitChance;

            int rand = RandomHelper.IRand(0, 9999);
            if (tmp > 0 && rand < tmp)
                return SpellMissInfo.Miss;

            // Chance resist mechanic (select max value from every mechanic spell effect)
            int resist_chance = victim.GetMechanicResistChance(spellInfo) * 100;

            // Roll chance
            if (resist_chance > 0 && rand < (tmp += resist_chance))
                return SpellMissInfo.Resist;

            // cast by caster in front of victim
            if (!victim.HasUnitState(UnitState.Controlled) && (victim.HasInArc(MathF.PI, this) || victim.HasAuraType(AuraType.IgnoreHitDirection)))
            {
                int deflect_chance = victim.GetTotalAuraModifier(AuraType.DeflectSpells) * 100;
                if (deflect_chance > 0 && rand < (tmp += deflect_chance))
                    return SpellMissInfo.Deflect;
            }

            return SpellMissInfo.None;
        }

        // Calculate spell hit result can be:
        // Every spell can: Evade/Immune/Reflect/Sucesful hit
        // For melee based spells:
        //   Miss
        //   Dodge
        //   Parry
        // For spells
        //   Resist
        public SpellMissInfo SpellHitResult(Unit victim, SpellInfo spellInfo, bool canReflect = false)
        {
            // Check for immune
            if (victim.IsImmunedToSpell(spellInfo, this))
                return SpellMissInfo.Immune;

            // Damage immunity is only checked if the spell has damage effects, this immunity must not prevent aura apply
            // returns SPELL_MISS_IMMUNE in that case, for other spells, the SMSG_SPELL_GO must show hit
            if (spellInfo.HasOnlyDamageEffects() && victim.IsImmunedToDamage(spellInfo))
                return SpellMissInfo.Immune;

            // All positive spells can`t miss
            /// @todo client not show miss log for this spells - so need find info for this in dbc and use it!
            if (spellInfo.IsPositive() && !IsHostileTo(victim)) // prevent from affecting enemy by "positive" spell
                return SpellMissInfo.None;

            if (this == victim)
                return SpellMissInfo.None;

            // Return evade for units in evade mode
            if (victim.IsCreature() && victim.ToCreature().IsEvadingAttacks())
                return SpellMissInfo.Evade;

            // Try victim reflect spell
            if (canReflect)
            {
                int reflectchance = victim.GetTotalAuraModifier(AuraType.ReflectSpells);
                reflectchance += victim.GetTotalAuraModifierByMiscMask(AuraType.ReflectSpellsSchool, (int)spellInfo.GetSchoolMask());

                if (reflectchance > 0 && RandomHelper.randChance(reflectchance))
                    return SpellMissInfo.Reflect;
            }

            if (spellInfo.HasAttribute(SpellAttr3.IgnoreHitResult))
                return SpellMissInfo.None;

            switch (spellInfo.DmgClass)
            {
                case SpellDmgClass.Ranged:
                case SpellDmgClass.Melee:
                    return MeleeSpellHitResult(victim, spellInfo);
                case SpellDmgClass.None:
                    return SpellMissInfo.None;
                case SpellDmgClass.Magic:
                    return MagicSpellHitResult(victim, spellInfo);
            }
            return SpellMissInfo.None;
        }

        public void SendSpellMiss(Unit target, uint spellID, SpellMissInfo missInfo)
        {
            SpellMissLog spellMissLog = new();
            spellMissLog.SpellID = spellID;
            spellMissLog.Caster = GetGUID();
            spellMissLog.Entries.Add(new SpellLogMissEntry(target.GetGUID(), (byte)missInfo));
            SendMessageToSet(spellMissLog, true);
        }

        public FactionTemplateRecord GetFactionTemplateEntry()
        {
            var entry = CliDB.FactionTemplateStorage.LookupByKey(GetFaction());
            if (entry == null)
                Log.outError(LogFilter.Server, $"{GetGUID()} has invalid faction {GetFaction()}");

            return entry;
        }

        // function based on function Unit::UnitReaction from 13850 client
        public ReputationRank GetReactionTo(WorldObject target)
        {
            // always friendly to self
            if (this == target)
                return ReputationRank.Friendly;

            // always friendly to charmer or owner
            if (GetCharmerOrOwnerOrSelf() == target.GetCharmerOrOwnerOrSelf())
                return ReputationRank.Friendly;

            Player selfPlayerOwner = GetAffectingPlayer();
            Player targetPlayerOwner = target.GetAffectingPlayer();

            // check forced reputation to support SPELL_AURA_FORCE_REACTION
            if (selfPlayerOwner)
            {
                var targetFactionTemplateEntry = target.GetFactionTemplateEntry();
                if (targetFactionTemplateEntry != null)
                {
                    var repRank = selfPlayerOwner.GetReputationMgr().GetForcedRankIfAny(targetFactionTemplateEntry);
                    if (repRank != ReputationRank.None)
                        return repRank;
                }
            }
            else if (targetPlayerOwner)
            {
                var selfFactionTemplateEntry = GetFactionTemplateEntry();
                if (selfFactionTemplateEntry != null)
                {
                    ReputationRank repRank = targetPlayerOwner.GetReputationMgr().GetForcedRankIfAny(selfFactionTemplateEntry);
                    if (repRank != ReputationRank.None)
                        return repRank;
                }
            }

            Unit unit = ToUnit();
            Unit targetUnit = target.ToUnit();
            if (unit && unit.HasUnitFlag(UnitFlags.PvpAttackable))
            {
                if (targetUnit && targetUnit.HasUnitFlag(UnitFlags.PvpAttackable))
                {
                    if (selfPlayerOwner && targetPlayerOwner)
                    {
                        // always friendly to other unit controlled by player, or to the player himself
                        if (selfPlayerOwner == targetPlayerOwner)
                            return ReputationRank.Friendly;

                        // duel - always hostile to opponent
                        if (selfPlayerOwner.duel != null && selfPlayerOwner.duel.opponent == targetPlayerOwner && selfPlayerOwner.duel.startTime != 0)
                            return ReputationRank.Hostile;

                        // same group - checks dependant only on our faction - skip FFA_PVP for example
                        if (selfPlayerOwner.IsInRaidWith(targetPlayerOwner))
                            return ReputationRank.Friendly; // return true to allow config option AllowTwoSide.Interaction.Group to work
                                                            // however client seems to allow mixed group parties, because in 13850 client it works like:
                                                            // return GetFactionReactionTo(GetFactionTemplateEntry(), target);
                    }

                    // check FFA_PVP
                    if (unit.IsFFAPvP() && targetUnit.IsFFAPvP())
                        return ReputationRank.Hostile;

                    if (selfPlayerOwner)
                    {
                        var targetFactionTemplateEntry = targetUnit.GetFactionTemplateEntry();
                        if (targetFactionTemplateEntry != null)
                        {
                            ReputationRank repRank = selfPlayerOwner.GetReputationMgr().GetForcedRankIfAny(targetFactionTemplateEntry);
                            if (repRank != ReputationRank.None)
                                return repRank;

                            if (!selfPlayerOwner.HasUnitFlag2(UnitFlags2.IgnoreReputation))
                            {
                                var targetFactionEntry = CliDB.FactionStorage.LookupByKey(targetFactionTemplateEntry.Faction);
                                if (targetFactionEntry != null)
                                {
                                    if (targetFactionEntry.CanHaveReputation())
                                    {
                                        // check contested flags
                                        if ((targetFactionTemplateEntry.Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0 && selfPlayerOwner.HasPlayerFlag(PlayerFlags.ContestedPVP))
                                            return ReputationRank.Hostile;

                                        // if faction has reputation, hostile state depends only from AtWar state
                                        if (selfPlayerOwner.GetReputationMgr().IsAtWar(targetFactionEntry))
                                            return ReputationRank.Hostile;
                                        return ReputationRank.Friendly;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // do checks dependant only on our faction
            return GetFactionReactionTo(GetFactionTemplateEntry(), target);
        }

        public static ReputationRank GetFactionReactionTo(FactionTemplateRecord factionTemplateEntry, WorldObject target)
        {
            // always neutral when no template entry found
            if (factionTemplateEntry == null)
                return ReputationRank.Neutral;

            var targetFactionTemplateEntry = target.GetFactionTemplateEntry();
            if (targetFactionTemplateEntry == null)
                return ReputationRank.Neutral;

            Player targetPlayerOwner = target.GetAffectingPlayer();
            if (targetPlayerOwner != null)
            {
                // check contested flags
                if ((factionTemplateEntry.Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0 && targetPlayerOwner.HasPlayerFlag(PlayerFlags.ContestedPVP))
                    return ReputationRank.Hostile;

                var repRank = targetPlayerOwner.GetReputationMgr().GetForcedRankIfAny(factionTemplateEntry);
                if (repRank != ReputationRank.None)
                    return repRank;

                if (target.IsUnit() && !target.ToUnit().HasUnitFlag2(UnitFlags2.IgnoreReputation))
                {
                    var factionEntry = CliDB.FactionStorage.LookupByKey(factionTemplateEntry.Faction);
                    if (factionEntry != null)
                    {
                        if (factionEntry.CanHaveReputation())
                        {
                            // CvP case - check reputation, don't allow state higher than neutral when at war
                            ReputationRank repRank1 = targetPlayerOwner.GetReputationMgr().GetRank(factionEntry);
                            if (targetPlayerOwner.GetReputationMgr().IsAtWar(factionEntry))
                                repRank1 = (ReputationRank)Math.Min((int)ReputationRank.Neutral, (int)repRank1);
                            return repRank1;
                        }
                    }
                }
            }

            // common faction based check
            if (factionTemplateEntry.IsHostileTo(targetFactionTemplateEntry))
                return ReputationRank.Hostile;
            if (factionTemplateEntry.IsFriendlyTo(targetFactionTemplateEntry))
                return ReputationRank.Friendly;
            if (targetFactionTemplateEntry.IsFriendlyTo(factionTemplateEntry))
                return ReputationRank.Friendly;
            if ((factionTemplateEntry.Flags & (ushort)FactionTemplateFlags.HostileByDefault) != 0)
                return ReputationRank.Hostile;
            // neutral by default
            return ReputationRank.Neutral;
        }

        public bool IsHostileTo(WorldObject target)
        {
            return GetReactionTo(target) <= ReputationRank.Hostile;
        }

        public bool IsFriendlyTo(WorldObject target)
        {
            return GetReactionTo(target) >= ReputationRank.Friendly;
        }

        public bool IsHostileToPlayers()
        {
            var my_faction = GetFactionTemplateEntry();
            if (my_faction.Faction == 0)
                return false;

            var raw_faction = CliDB.FactionStorage.LookupByKey(my_faction.Faction);
            if (raw_faction != null && raw_faction.ReputationIndex >= 0)
                return false;

            return my_faction.IsHostileToPlayers();
        }

        public bool IsNeutralToAll()
        {
            var my_faction = GetFactionTemplateEntry();
            if (my_faction.Faction == 0)
                return true;

            var raw_faction = CliDB.FactionStorage.LookupByKey(my_faction.Faction);
            if (raw_faction != null && raw_faction.ReputationIndex >= 0)
                return false;

            return my_faction.IsNeutralToAll();
        }

        public void CastSpell(WorldObject target, uint spellId, bool triggered = false)
        {
            CastSpellExtraArgs args = new(triggered);
            CastSpell(target, spellId, args);
        }

        public void CastSpell(SpellCastTargets targets, uint spellId, CastSpellExtraArgs args)
        {
            SpellInfo info = Global.SpellMgr.GetSpellInfo(spellId, args.CastDifficulty != Difficulty.None ? args.CastDifficulty : GetMap().GetDifficultyID());
            if (info == null)
            {
                Log.outError(LogFilter.Unit, $"CastSpell: unknown spell {spellId} by caster {GetGUID()}");
                return;
            }

            Spell spell = new Spell(this, info, args.TriggerFlags, args.OriginalCaster);
            foreach (var pair in args.SpellValueOverrides)
                spell.SetSpellValue(pair.Key, pair.Value);

            spell.m_CastItem = args.CastItem;
            spell.Prepare(targets, args.TriggeringAura);
        }

        public void CastSpell(WorldObject target, uint spellId, CastSpellExtraArgs args)
        {
            SpellCastTargets targets = new();
            if (target)
            {
                Unit unitTarget = target.ToUnit();
                if (unitTarget != null)
                    targets.SetUnitTarget(unitTarget);
                else
                {
                    GameObject goTarget = target.ToGameObject();
                    if (goTarget != null)
                        targets.SetGOTarget(goTarget);
                    else
                    {
                        Log.outError(LogFilter.Unit, $"CastSpell: Invalid target {target.GetGUID()} passed to spell cast by {GetGUID()}");
                        return;
                    }
                }
            }
            CastSpell(targets, spellId, args);
        }

        public void CastSpell(Position dest, uint spellId, CastSpellExtraArgs args)
        {
            SpellCastTargets targets = new();
            targets.SetDst(dest);
            CastSpell(targets, spellId, args);
        }

        // function based on function Unit::CanAttack from 13850 client
        public bool IsValidAttackTarget(WorldObject target, SpellInfo bySpell = null)
        {
            Cypher.Assert(target != null);

            // some positive spells can be casted at hostile target
            bool isPositiveSpell = bySpell != null && bySpell.IsPositive();

            // can't attack self (spells can, attribute check)
            if (bySpell == null && this == target)
                return false;

            // can't attack unattackable units
            Unit unitTarget = target.ToUnit();
            if (unitTarget != null && unitTarget.HasUnitState(UnitState.Unattackable))
                return false;

            // can't attack GMs
            if (target.IsPlayer() && target.ToPlayer().IsGameMaster())
                return false;

            Unit unit = ToUnit();
            // visibility checks (only units)
            if (unit != null)
            {
                // can't attack invisible
                if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetInvisible))
                {
                    if (!unit.CanSeeOrDetect(target, bySpell != null && bySpell.IsAffectingArea()))
                        return false;
                }
            }

            // can't attack dead
            if ((bySpell == null || !bySpell.IsAllowingDeadTarget()) && unitTarget != null && !unitTarget.IsAlive())
                return false;

            // can't attack untargetable
            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable)) && unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NotSelectable))
                return false;

            Player playerAttacker = ToPlayer();
            if (playerAttacker != null)
            {
                if (playerAttacker.HasPlayerFlag(PlayerFlags.Uber))
                    return false;
            }

            // check flags
            if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.TaxiFlight | UnitFlags.NotAttackable1 | UnitFlags.Unk16))
                return false;

            // ignore immunity flags when assisting
            if (bySpell == null || (isPositiveSpell && !bySpell.HasAttribute(SpellAttr6.AssistIgnoreImmuneFlag)))
            {
                if (unit != null && !unit.HasUnitFlag(UnitFlags.PvpAttackable) && unitTarget != null && unitTarget.IsImmuneToNPC())
                    return false;

                if (unitTarget != null && !unitTarget.HasUnitFlag(UnitFlags.PvpAttackable) && unit && unit.IsImmuneToNPC())
                    return false;

                if (bySpell == null || !bySpell.HasAttribute(SpellAttr8.AttackIgnoreImmuneToPCFlag))
                {
                    if (unit != null && unit.HasUnitFlag(UnitFlags.PvpAttackable) && unitTarget && unitTarget.IsImmuneToPC())
                        return false;

                    if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PvpAttackable) && unit && unit.IsImmuneToPC())
                        return false;
                }
            }

            // CvC case - can attack each other only when one of them is hostile
            if (unit && !unit.HasUnitFlag(UnitFlags.PvpAttackable) && unitTarget != null && !unitTarget.HasUnitFlag(UnitFlags.PvpAttackable))
                return IsHostileTo(target) || target.IsHostileTo(this);

            // PvP, PvC, CvP case
            // can't attack friendly targets
            if (IsFriendlyTo(target) || target.IsFriendlyTo(this))
                return false;

            Player playerAffectingAttacker = unit != null && unit.HasUnitFlag(UnitFlags.PvpAttackable) ? GetAffectingPlayer() : null;
            Player playerAffectingTarget = unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PvpAttackable) ? target.GetAffectingPlayer() : null;

            // Not all neutral creatures can be attacked (even some unfriendly faction does not react aggresive to you, like Sporaggar)
            if ((playerAffectingAttacker && !playerAffectingTarget) || (!playerAffectingAttacker && playerAffectingTarget))
            {
                Player player = playerAffectingAttacker ? playerAffectingAttacker : playerAffectingTarget;
                Unit creature = playerAffectingAttacker ? unitTarget : unit;
                if (creature != null)
                {
                    if (creature.IsContestedGuard() && player.HasPlayerFlag(PlayerFlags.ContestedPVP))
                        return true;

                    var factionTemplate = creature.GetFactionTemplateEntry();
                    if (factionTemplate != null)
                    {
                        if (player.GetReputationMgr().GetForcedRankIfAny(factionTemplate) == ReputationRank.None)
                        {
                            var factionEntry = CliDB.FactionStorage.LookupByKey(factionTemplate.Faction);
                            if (factionEntry != null)
                            {
                                var repState = player.GetReputationMgr().GetState(factionEntry);
                                if (repState != null)
                                    if (!repState.Flags.HasFlag(ReputationFlags.AtWar))
                                        return false;
                            }
                        }

                    }
                }
            }

            Creature creatureAttacker = ToCreature();
            if (creatureAttacker && creatureAttacker.GetCreatureTemplate().TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit))
                return false;

            if (playerAffectingAttacker && playerAffectingTarget)
                if (playerAffectingAttacker.duel != null && playerAffectingAttacker.duel.opponent == playerAffectingTarget && playerAffectingAttacker.duel.startTime != 0)
                    return true;

            // PvP case - can't attack when attacker or target are in sanctuary
            // however, 13850 client doesn't allow to attack when one of the unit's has sanctuary flag and is pvp
            if (unitTarget && unitTarget.HasUnitFlag(UnitFlags.PvpAttackable) && unit && unit.HasUnitFlag(UnitFlags.PvpAttackable) && (unitTarget.IsInSanctuary() || unit.IsInSanctuary()))
                return false;

            // additional checks - only PvP case
            if (playerAffectingAttacker && playerAffectingTarget)
            {
                if (unitTarget.IsPvP())
                    return true;

                if (unit.IsFFAPvP() && unitTarget.IsFFAPvP())
                    return true;

                return unit.HasPvpFlag(UnitPVPStateFlags.Unk1) ||
                    unitTarget.HasPvpFlag(UnitPVPStateFlags.Unk1);
            }

            return true;
        }

        // function based on function Unit::CanAssist from 13850 client
        public bool IsValidAssistTarget(WorldObject target, SpellInfo bySpell = null, bool spellCheck = true)
        {
            Cypher.Assert(target);

            // some negative spells can be casted at friendly target
            bool isNegativeSpell = bySpell != null && !bySpell.IsPositive();

            // can assist to self
            if (this == target)
                return true;

            // can't assist unattackable units
            Unit unitTarget = target.ToUnit();
            if (unitTarget && unitTarget.HasUnitState(UnitState.Unattackable))
                return false;

            // can't assist GMs
            if (target.IsPlayer() && target.ToPlayer().IsGameMaster())
                return false;

            // can't assist own vehicle or passenger
            Unit unit = ToUnit();
            if (unit && unitTarget && unit.GetVehicle())
            {
                if (unit.IsOnVehicle(unitTarget))
                    return false;

                if (unit.GetVehicleBase().IsOnVehicle(unitTarget))
                    return false;
            }

            // can't assist invisible
            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetInvisible)) && !CanSeeOrDetect(target, bySpell != null && bySpell.IsAffectingArea()))
                return false;

            // can't assist dead
            if ((bySpell == null || !bySpell.IsAllowingDeadTarget()) && unitTarget && !unitTarget.IsAlive())
                return false;

            // can't assist untargetable
            if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable)) && unitTarget && unitTarget.HasUnitFlag(UnitFlags.NotSelectable))
                return false;

            // check flags for negative spells
            if (isNegativeSpell && unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.TaxiFlight | UnitFlags.NotAttackable1 | UnitFlags.Unk16))
                return false;

            if (isNegativeSpell || bySpell == null || !bySpell.HasAttribute(SpellAttr6.AssistIgnoreImmuneFlag))
            {
                if (unit != null && unit.HasUnitFlag(UnitFlags.PvpAttackable))
                {
                    if (bySpell == null || !bySpell.HasAttribute(SpellAttr8.AttackIgnoreImmuneToPCFlag))
                        if (unitTarget != null && unitTarget.IsImmuneToPC())
                            return false;
                }
                else
                {
                    if (unitTarget != null && unitTarget.IsImmuneToNPC())
                        return false;
                }
            }

            // can't assist non-friendly targets
            if (GetReactionTo(target) < ReputationRank.Neutral && target.GetReactionTo(this) < ReputationRank.Neutral && (!ToCreature() || !ToCreature().GetCreatureTemplate().TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit)))
                return false;

            // PvP case
            if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PvpAttackable))
            {
                Player targetPlayerOwner = target.GetAffectingPlayer();
                if (unit != null && unit.HasUnitFlag(UnitFlags.PvpAttackable))
                {
                    Player selfPlayerOwner = GetAffectingPlayer();
                    if (selfPlayerOwner != null && targetPlayerOwner != null)
                    {
                        // can't assist player which is dueling someone
                        if (selfPlayerOwner != targetPlayerOwner && targetPlayerOwner.duel != null)
                            return false;
                    }
                    // can't assist player in ffa_pvp zone from outside
                    if (unitTarget.IsFFAPvP() && unit && !unit.IsFFAPvP())
                        return false;

                    // can't assist player out of sanctuary from sanctuary if has pvp enabled
                    if (unitTarget.IsPvP())
                        if (unit != null && unit.IsInSanctuary() && !unitTarget.IsInSanctuary())
                            return false;
                }
            }
            // PvC case - player can assist creature only if has specific type flags
            // !target.HasFlag(UNIT_FIELD_FLAGS, UnitFlags.PvpAttackable) &&
            else if (unit != null && unit.HasUnitFlag(UnitFlags.PvpAttackable))
            {
                if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.AssistIgnoreImmuneFlag))
                {
                    if (unitTarget != null && !unitTarget.IsPvP())
                    {
                        Creature creatureTarget = target.ToCreature();
                        if (creatureTarget != null)
                            return (creatureTarget.GetCreatureTemplate().TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit) || creatureTarget.GetCreatureTemplate().TypeFlags.HasFlag(CreatureTypeFlags.CanAssist));
                    }
                }
            }

            return true;
        }

        public Unit GetMagicHitRedirectTarget(Unit victim, SpellInfo spellInfo)
        {
            // Patch 1.2 notes: Spell Reflection no longer reflects abilities
            if (spellInfo.HasAttribute(SpellAttr0.Ability) || spellInfo.HasAttribute(SpellAttr1.CantBeRedirected) || spellInfo.HasAttribute(SpellAttr0.UnaffectedByInvulnerability))
                return victim;

            var magnetAuras = victim.GetAuraEffectsByType(AuraType.SpellMagnet);
            foreach (AuraEffect aurEff in magnetAuras)
            {
                Unit magnet = aurEff.GetBase().GetCaster();
                if (magnet != null)
                {
                    if (spellInfo.CheckExplicitTarget(this, magnet) == SpellCastResult.SpellCastOk && IsValidAttackTarget(magnet, spellInfo))
                    {
                        /// @todo handle this charge drop by proc in cast phase on explicit target
                        if (spellInfo.HasHitDelay())
                        {
                            // Set up missile speed based delay
                            float hitDelay = spellInfo.LaunchDelay;
                            if (spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                                hitDelay += spellInfo.Speed;
                            else if (spellInfo.Speed > 0.0f)
                                hitDelay += Math.Max(victim.GetDistance(this), 5.0f) / spellInfo.Speed;

                            uint delay = (uint)Math.Floor(hitDelay * 1000.0f);
                            // Schedule charge drop
                            aurEff.GetBase().DropChargeDelayed(delay, AuraRemoveMode.Expire);
                        }
                        else
                            aurEff.GetBase().DropCharge(AuraRemoveMode.Expire);

                        return magnet;
                    }
                }
            }
            return victim;
        }

        public virtual uint GetCastSpellXSpellVisualId(SpellInfo spellInfo)
        {
            return spellInfo.GetSpellXSpellVisualId(this);
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
            List<Unit> playerList = new();
            var checker = new AnyPlayerInObjectRangeCheck(this, maxSearchRange);
            var searcher = new PlayerListSearcher(this, playerList, checker);

            Cell.VisitWorldObjects(this, searcher, maxSearchRange);
            return playerList;
        }

        public bool IsInPhase(WorldObject obj)
        {
            return GetPhaseShift().CanSee(obj.GetPhaseShift());
        }

        public static bool InSamePhase(WorldObject a, WorldObject b)
        {
            return a != null && b != null && a.IsInPhase(b);
        }

        public virtual float GetCombatReach() { return 0.0f; } // overridden (only) in Unit
        public PhaseShift GetPhaseShift() { return _phaseShift; }
        public void SetPhaseShift(PhaseShift phaseShift) { _phaseShift = new PhaseShift(phaseShift); }
        public PhaseShift GetSuppressedPhaseShift() { return _suppressedPhaseShift; }
        public void SetSuppressedPhaseShift(PhaseShift phaseShift) { _suppressedPhaseShift = new PhaseShift(phaseShift); }
        public int GetDBPhase() { return _dbPhase; }

        // if negative it is used as PhaseGroupId
        public void SetDBPhase(int p) { _dbPhase = p; }

        public void PlayDistanceSound(uint soundId, Player target = null)
        {
            PlaySpeakerBoxSound playSpeakerBoxSound = new(GetGUID(), soundId);
            if (target != null)
                target.SendPacket(playSpeakerBoxSound);
            else
                SendMessageToSet(playSpeakerBoxSound, true);
        }

        public void PlayDirectSound(uint soundId, Player target = null, uint broadcastTextId = 0)
        {
            PlaySound sound = new(GetGUID(), soundId, broadcastTextId);
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

            List<Unit> targets = new();
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

        // Watcher
        public bool IsPrivateObject() { return !_privateObjectOwner.IsEmpty(); }
        public ObjectGuid GetPrivateObjectOwner() { return _privateObjectOwner; }
        public void SetPrivateObjectOwner(ObjectGuid owner) { _privateObjectOwner = owner; }

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
        public void SetIsNewObject(bool enable) { _isNewObject = enable; }

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
        NotifyFlags GetNotifyFlags() { return m_notifyflags; }
        public void ResetAllNotifies() { m_notifyflags = 0; }

        public bool IsActiveObject() { return m_isActive; }
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

        public virtual float GetCollisionHeight() { return 0.0f; }
        public float GetMidsectionHeight() { return GetCollisionHeight() / 2.0f; }

        public virtual bool IsNeverVisibleFor(WorldObject seer) { return !IsInWorld; }
        public virtual bool IsAlwaysVisibleFor(WorldObject seer) { return false; }
        public virtual bool IsInvisibleDueToDespawn() { return false; }
        public virtual bool IsAlwaysDetectableFor(WorldObject seer) { return false; }

        public virtual bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate) { return true; }

        public virtual ObjectGuid GetOwnerGUID() { return default; }
        public virtual ObjectGuid GetCharmerOrOwnerGUID() { return GetOwnerGUID(); }

        public virtual uint GetFaction() { return 0; }
        public virtual void SetFaction(uint faction) { }

        //Position

        public float GetDistanceZ(WorldObject obj)
        {
            float dz = Math.Abs(GetPositionZ() - obj.GetPositionZ());
            float sizefactor = GetCombatReach() + obj.GetCombatReach();
            float dist = dz - sizefactor;
            return (dist > 0 ? dist : 0);
        }

        public virtual bool _IsWithinDist(WorldObject obj, float dist2compare, bool is3D, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            float sizefactor = 0;
            sizefactor += incOwnRadius ? GetCombatReach() : 0.0f;
            sizefactor += incTargetRadius ? obj.GetCombatReach() : 0.0f;
            float maxdist = dist2compare + sizefactor;

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
            float d = GetExactDist(obj.GetPosition()) - GetCombatReach() - obj.GetCombatReach();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance(Position pos)
        {
            float d = GetExactDist(pos) - GetCombatReach();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance(float x, float y, float z)
        {
            float d = GetExactDist(x, y, z) - GetCombatReach();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance2d(WorldObject obj)
        {
            float d = GetExactDist2d(obj.GetPosition()) - GetCombatReach() - obj.GetCombatReach();
            return d > 0.0f ? d : 0.0f;
        }

        public float GetDistance2d(float x, float y)
        {
            float d = GetExactDist2d(x, y) - GetCombatReach();
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
                oz += GetCollisionHeight();
                float x, y, z;
                if (IsTypeId(TypeId.Player))
                {
                    GetPosition(out x, out y, out z);
                    z += GetCollisionHeight();
                }
                else
                    GetHitSpherePointFor(new Position(ox, oy, oz), out x, out y, out z);

                return GetMap().IsInLineOfSight(GetPhaseShift(), x, y, z, ox, oy, oz, checks, ignoreFlags);
            }

            return true;
        }

        public bool IsWithinLOSInMap(WorldObject obj, LineOfSightChecks checks = LineOfSightChecks.All, ModelIgnoreFlags ignoreFlags = ModelIgnoreFlags.Nothing)
        {
            if (!IsInMap(obj))
                return false;

            float ox, oy, oz;
            if (obj.IsTypeId(TypeId.Player))
            {
                obj.GetPosition(out ox, out oy, out oz);
                oz += GetCollisionHeight();
            }
            else
                obj.GetHitSpherePointFor(new(GetPositionX(), GetPositionY(), GetPositionZ() + GetCollisionHeight()), out ox, out oy, out oz);

            float x, y, z;
            if (IsPlayer())
            {
                GetPosition(out x, out y, out z);
                z += GetCollisionHeight();
            }
            else
                GetHitSpherePointFor(new(obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ() + obj.GetCollisionHeight()), out x, out y, out z);

            return GetMap().IsInLineOfSight(GetPhaseShift(), x, y, z, ox, oy, oz, checks, ignoreFlags);
        }

        Position GetHitSpherePointFor(Position dest)
        {
            Vector3 vThis = new(GetPositionX(), GetPositionY(), GetPositionZ() + GetCollisionHeight());
            Vector3 vObj = new(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ());
            Vector3 contactPoint = vThis + (vObj - vThis).directionOrZero() * Math.Min(dest.GetExactDist(GetPosition()), GetCombatReach());

            return new Position(contactPoint.X, contactPoint.Y, contactPoint.Z, GetAbsoluteAngle(contactPoint.X, contactPoint.Y));
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

            float sizefactor = GetCombatReach() + obj.GetCombatReach();

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

        public bool IsInBetween(WorldObject obj1, WorldObject obj2, float size = 0)
        {
            return obj1 && obj2 && IsInBetween(obj1.GetPosition(), obj2.GetPosition(), size);
        }
        bool IsInBetween(Position pos1, Position pos2, float size)
        {
            float dist = GetExactDist2d(pos1);

            // not using sqrt() for performance
            if ((dist * dist) >= pos1.GetExactDist2dSq(pos2))
                return false;

            if (size == 0)
                size = GetCombatReach() / 2;

            float angle = pos1.GetAbsoluteAngle(pos2);

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

        public Position GetRandomPoint(Position srcPos, float distance)
        {
            float x, y, z;
            GetRandomPoint(srcPos, distance, out x, out y, out z);
            return new Position(x, y, z, GetOrientation());
        }

        public void UpdateGroundPositionZ(float x, float y, ref float z)
        {
            float newZ = GetMapHeight(x, y, z);
            if (newZ > MapConst.InvalidHeight)
                z = newZ + (IsUnit() ? ToUnit().GetHoverOffset() : 0.0f);
        }

        public void UpdateAllowedPositionZ(float x, float y, ref float z)
        {
            // TODO: Allow transports to be part of dynamic vmap tree
            if (GetTransport())
                return;

            Unit unit = ToUnit();
            if (unit != null)
            {
                if (!unit.CanFly())
                {
                    bool canSwim = unit.CanSwim();
                    float groundZ = z;
                    float max_z;
                    if (canSwim)
                        max_z = GetMapWaterOrGroundLevel(x, y, z, ref groundZ);
                    else
                        max_z = groundZ = GetMapHeight(x, y, z);

                    if (max_z > MapConst.InvalidHeight)
                    {
                        // hovering units cannot go below their hover height
                        float hoverOffset = unit.GetHoverOffset();
                        max_z += hoverOffset;
                        groundZ += hoverOffset;

                        if (z > max_z)
                            z = max_z;
                        else if (z < groundZ)
                            z = groundZ;
                    }
                }
                else
                {
                    float ground_z = GetMapHeight(x, y, z) + unit.GetHoverOffset();
                    if (z < ground_z)
                        z = ground_z;
                }
            }
            else
            {
                float ground_z = GetMapHeight(x, y, z);
                if (ground_z > MapConst.InvalidHeight)
                    z = ground_z;
            }
        }

        public void GetNearPoint2D(WorldObject searcher, out float x, out float y, float distance2d, float absAngle)
        {
            float effectiveReach = GetCombatReach();

            if (searcher)
            {
                effectiveReach += searcher.GetCombatReach();

                if (this != searcher)
                {
                    float myHover = 0.0f;
                    float searcherHover = 0.0f;

                    Unit unit = ToUnit();
                    if (unit != null)
                        myHover = unit.GetHoverOffset();

                    Unit searchUnit = searcher.ToUnit();
                    if (searchUnit != null)
                        searcherHover = searchUnit.GetHoverOffset();

                    float hoverDelta = myHover - searcherHover;
                    if (hoverDelta != 0.0f)
                        effectiveReach = MathF.Sqrt(Math.Max(effectiveReach * effectiveReach - hoverDelta * hoverDelta, 0.0f));
                }
            }

            x = GetPositionX() + (effectiveReach + distance2d) * MathF.Cos(absAngle);
            y = GetPositionY() + (effectiveReach + distance2d) * MathF.Sin(absAngle);

            GridDefines.NormalizeMapCoord(ref x);
            GridDefines.NormalizeMapCoord(ref y);
        }

        public void GetNearPoint(WorldObject searcher, out float x, out float y, out float z, float distance2d, float absAngle)
        {
            GetNearPoint2D(searcher, out x, out y, distance2d, absAngle);
            z = GetPositionZ();
            (searcher ?? this).UpdateAllowedPositionZ(x, y, ref z);

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
                GetNearPoint2D(searcher, out x, out y, distance2d, absAngle + angle);
                z = GetPositionZ();
                (searcher ?? this).UpdateAllowedPositionZ(x, y, ref z);
                if (IsWithinLOS(x, y, z))
                    return;
            }

            // still not in LoS, give up and return first position found
            x = first_x;
            y = first_y;
            z = first_z;
        }

        public void GetClosePoint(out float x, out float y, out float z, float size, float distance2d = 0, float relAngle = 0)
        {
            // angle calculated from current orientation
            GetNearPoint(null, out x, out y, out z, distance2d + size, GetOrientation() + relAngle);
        }

        public Position GetNearPosition(float dist, float angle)
        {
            var pos = GetPosition();
            MovePosition(pos, dist, angle);
            return pos;
        }

        public Position GetFirstCollisionPosition(float dist, float angle)
        {
            var pos = new Position(GetPosition());
            MovePositionToFirstCollision(pos, dist, angle);
            return pos;
        }

        public Position GetRandomNearPosition(float radius)
        {
            var pos = GetPosition();
            MovePosition(pos, radius * (float)RandomHelper.NextDouble(), (float)RandomHelper.NextDouble() * MathFunctions.PI * 2);
            return pos;
        }

        public void GetContactPoint(WorldObject obj, out float x, out float y, out float z, float distance2d = 0.5f)
        {
            // angle to face `obj` to `this` using distance includes size of `obj`
            GetNearPoint(obj, out x, out y, out z, distance2d, GetAbsoluteAngle(obj));
        }

        public void MovePosition(Position pos, float dist, float angle)
        {
            angle += GetOrientation();
            float destx = pos.posX + dist * (float)Math.Cos(angle);
            float desty = pos.posY + dist * (float)Math.Sin(angle);

            // Prevent invalid coordinates here, position is unchanged
            if (!GridDefines.IsValidMapCoord(destx, desty, pos.posZ))
            {
                Log.outError(LogFilter.Server, "WorldObject.MovePosition invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);
                return;
            }

            float ground = GetMapHeight(destx, desty, MapConst.MaxHeight);
            float floor = GetMapHeight(destx, desty, pos.posZ);
            float destz = Math.Abs(ground - pos.posZ) <= Math.Abs(floor - pos.posZ) ? ground : floor;

            float step = dist / 10.0f;

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

        public void MovePositionToFirstCollision(Position pos, float dist, float angle)
        {
            angle += GetOrientation();
            float destx = pos.posX + dist * (float)Math.Cos(angle);
            float desty = pos.posY + dist * (float)Math.Sin(angle);
            float destz = pos.posZ;

            // Prevent invalid coordinates here, position is unchanged
            if (!GridDefines.IsValidMapCoord(destx, desty))
            {
                Log.outError(LogFilter.Server, "WorldObject.MovePositionToFirstCollision invalid coordinates X: {0} and Y: {1} were passed!", destx, desty);
                return;
            }

            UpdateAllowedPositionZ(destx, desty, ref destz);
            bool col = Global.VMapMgr.GetObjectHitPos(PhasingHandler.GetTerrainMapId(GetPhaseShift(), GetMap(), pos.posX, pos.posY), pos.posX, pos.posY, pos.posZ, destx, desty, destz, out destx, out desty, out destz, -0.5f);

            // collision occured
            if (col)
            {
                // move back a bit
                destx -= SharedConst.ContactDistance * (float)Math.Cos(angle);
                desty -= SharedConst.ContactDistance * (float)Math.Sin(angle);
                dist = (float)Math.Sqrt((pos.posX - destx) * (pos.posX - destx) + (pos.posY - desty) * (pos.posY - desty));
            }

            // check dynamic collision
            col = GetMap().GetObjectHitPos(GetPhaseShift(), pos.posX, pos.posY, pos.posZ, destx, desty, destz, out destx, out desty, out destz, -0.5f);

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
                    UpdateAllowedPositionZ(destx, desty, ref destz);
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
            UpdateAllowedPositionZ(destx, desty, ref pos.posZ);
            pos.SetOrientation(GetOrientation());
        }

        public float GetFloorZ()
        {
            if (!IsInWorld)
                return m_staticFloorZ;
            return Math.Max(m_staticFloorZ, GetMap().GetGameObjectFloor(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZ() + GetCollisionHeight()));
        }

        float GetMapWaterOrGroundLevel(float x, float y, float z, ref float ground)
        {
            Unit unit = ToUnit();
            if (unit != null)
                return GetMap().GetWaterOrGroundLevel(GetPhaseShift(), x, y, z, ref ground, !unit.HasAuraType(AuraType.WaterWalk), GetCollisionHeight());

            return z;
        }

        public float GetMapHeight(float x, float y, float z, bool vmap = true, float distanceToSearch = MapConst.DefaultHeightSearch)
        {
            if (z != MapConst.MaxHeight)
                z += GetCollisionHeight();

            return GetMap().GetHeight(GetPhaseShift(), x, y, z, vmap, distanceToSearch);
        }

        public void SetLocationInstanceId(uint _instanceId) { instanceId = _instanceId; }

        #region Fields
        public TypeMask ObjectTypeMask { get; set; }
        protected TypeId ObjectTypeId { get; set; }
        protected CreateObjectBits m_updateFlag;
        ObjectGuid m_guid;
        bool _isNewObject;

        public UpdateFieldHolder m_values;
        public ObjectFieldData m_objectData;

        public uint LastUsedScriptID;

        bool m_objectUpdated;

        uint m_zoneId;
        uint m_areaId;
        float m_staticFloorZ;
        bool m_outdoors;

        // Event handler
        public EventSystem m_Events = new();

        public MovementInfo m_movementInfo;
        string _name;
        protected bool m_isActive;
        bool m_isFarVisible;
        Optional<float> m_visibilityDistanceOverride;
        bool m_isWorldObject;
        public ZoneScript m_zoneScript;

        Transport m_transport;
        Map _currMap;
        uint instanceId;
        PhaseShift _phaseShift = new();
        PhaseShift _suppressedPhaseShift = new();                   // contains phases for current area but not applied due to conditions
        int _dbPhase;
        public bool IsInWorld { get; set; }

        NotifyFlags m_notifyflags;

        ObjectGuid _privateObjectOwner;

        public FlaggedArray<StealthType> m_stealth = new(2);
        public FlaggedArray<StealthType> m_stealthDetect = new(2);

        public FlaggedArray<InvisibilityType> m_invisibility = new((int)InvisibilityType.Max);
        public FlaggedArray<InvisibilityType> m_invisibilityDetect = new((int)InvisibilityType.Max);

        public FlaggedArray<ServerSideVisibilityType> m_serverSideVisibility = new(2);
        public FlaggedArray<ServerSideVisibilityType> m_serverSideVisibilityDetect = new(2);
        #endregion

        public static implicit operator bool(WorldObject obj)
        {
            return obj != null;
        }
    }

    public class MovementInfo
    {
        public ObjectGuid Guid { get; set; }
        MovementFlag flags;
        MovementFlag2 flags2;
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
        public int Unused910;

        public void Read(WorldPacket data)
        {
            ID = data.ReadPackedGuid();
            Origin = data.ReadVector3();
            Direction = data.ReadVector3();
            TransportID = data.ReadUInt32();
            Magnitude = data.ReadFloat();
            Type = data.ReadBits<byte>(2);
            bool has910 = data.HasBit();
            if (has910)
                Unused910 = data.ReadInt32();
        }

        public void Write(WorldPacket data)
        {
            MovementExtensions.WriteMovementForceWithDirection(this, data);
        }
    }

    public class MovementForces
    {
        List<MovementForce> _forces = new();
        float _modMagnitude = 1.0f;

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

        MovementForce FindMovementForce(ObjectGuid id)
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

    class CombatLogSender : IDoWork<Player>
    {
        CombatLogServerPacket i_message;

        public CombatLogSender(CombatLogServerPacket msg)
        {
            i_message = msg;
        }

        public void Invoke(Player player)
        {
            i_message.Clear();
            i_message.SetAdvancedCombatLogging(player.IsAdvancedCombatLoggingEnabled());

            player.SendPacket(i_message);
        }
    }
}