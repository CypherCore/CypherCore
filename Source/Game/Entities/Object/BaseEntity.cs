// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public class BaseEntity : WorldLocation, IDisposable
    {
        public ObjectGuid _guid;

        public TypeId ObjectTypeId = TypeId.Max;
        public CreateObjectBits m_updateFlag;
        public EntityFragmentsHolder EntityFragments;

        public UpdateFieldHolder m_values;

        bool _objectUpdated;

        bool _isNewObject;
        bool _isDestroyedObject;

        public BaseEntity()
        {
            m_values = new UpdateFieldHolder(this);
        }

        public virtual void Dispose()
        {
            if (IsInWorld)
            {
                Log.outFatal(LogFilter.Misc, $"BaseEntity::~BaseEntity {GetGUID()} deleted but still in world!!");
                Cypher.Assert(false);
            }

            if (_objectUpdated)
            {
                Log.outFatal(LogFilter.Misc, $"BaseEntity::~BaseEntity {GetGUID()} deleted but still in update list!!");
                Cypher.Assert(false);
            }
        }

        public virtual void AddToWorld()
        {
            if (IsInWorld)
                return;

            IsInWorld = true;

            // synchronize values mirror with values array (changes will send in updatecreate opcode any way
            Cypher.Assert(!_objectUpdated);
            ClearUpdateMask(true);
        }

        public virtual void RemoveFromWorld()
        {
            if (!IsInWorld)
                return;

            IsInWorld = false;

            // if we remove from world then sending changes not required
            ClearUpdateMask(true);
        }

        public virtual void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
        {
            if (target == null)
                return;

            UpdateType updateType = _isNewObject ? UpdateType.CreateObject2 : UpdateType.CreateObject;
            TypeId tempObjectType = ObjectTypeId;
            CreateObjectBits flags = m_updateFlag;

            if (target == this)
            {
                flags.ThisIsYou = true;
                flags.ActivePlayer = true;
                tempObjectType = TypeId.ActivePlayer;
            }

            if (IsWorldObject())
            {
                WorldObject worldObject = (this as WorldObject);
                if (worldObject.GetSmoothPhasing()?.GetInfoForSeer(target.GetGUID()) != null)
                    flags.SmoothPhasing = true;
            }

            WorldPacket buffer = new();
            buffer.WriteUInt8((byte)updateType);
            buffer.WritePackedGuid(GetGUID());
            buffer.WriteUInt8((byte)tempObjectType);

            BuildMovementUpdate(buffer, flags, target);

            UpdateFieldFlag fieldFlags = GetUpdateFieldFlagsFor(target);

            WorldPacket tempBuffer = new();
            tempBuffer.WriteUInt8((byte)fieldFlags);
            BuildEntityFragments(tempBuffer, EntityFragments.GetIds());

            for (int i = 0; i < EntityFragments.UpdateableCount; ++i)
            {
                EntityFragment fragmentId = EntityFragments.Updateable.Ids[i];
                if (EntityFragmentsHolder.IsIndirectFragment(fragmentId))
                    tempBuffer.WriteUInt8(1);  // IndirectFragmentActive

                EntityFragments.Updateable.SerializeCreate[i](this, tempBuffer, fieldFlags, target);
            }

            buffer.WriteUInt32(tempBuffer.GetSize());
            buffer.WriteBytes(tempBuffer);

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

            UpdateFieldFlag fieldFlags = GetUpdateFieldFlagsFor(target);

            WorldPacket tempBuffer = new();
            tempBuffer.WriteUInt8((byte)(fieldFlags.HasFlag(UpdateFieldFlag.Owner) ? 1 : 0));
            tempBuffer.WriteUInt8((byte)(EntityFragments.IdsChanged ? 1 : 0));
            if (EntityFragments.IdsChanged)
            {
                tempBuffer.WriteUInt8((byte)EntityFragmentSerializationType.Full);
                BuildEntityFragments(tempBuffer, EntityFragments.GetIds());
            }
            tempBuffer.WriteUInt8(EntityFragments.ContentsChangedMask);

            for (int i = 0; i < EntityFragments.UpdateableCount; ++i)
            {
                if ((EntityFragments.ContentsChangedMask & EntityFragments.Updateable.Masks[i]) == 0)
                    continue;

                EntityFragments.Updateable.SerializeUpdate[i](this, tempBuffer, fieldFlags, target);
            }

            buffer.WriteUInt32(tempBuffer.GetSize());
            buffer.WriteBytes(tempBuffer);

            data.AddUpdateBlock(buffer);
        }

        void BuildEntityFragments(WorldPacket data, Span<EntityFragment> fragments)
        {
            foreach (var frag in fragments)
                data.WriteUInt8((byte)frag);

            data.WriteUInt8((byte)EntityFragment.End);
        }

        public void BuildDestroyUpdateBlock(UpdateData data)
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

        public void SendOutOfRangeForPlayer(Player target)
        {
            Cypher.Assert(target != null);

            UpdateData updateData = new(target.GetMapId());
            BuildOutOfRangeUpdateBlock(updateData);
            updateData.BuildPacket(out UpdateObject packet);
            target.SendPacket(packet);
        }

        public unsafe void BuildMovementUpdate(WorldPacket data, CreateObjectBits flags, Player target)
        {
            List<uint> PauseTimes = null;
            if (IsGameObject())
                PauseTimes = (this as GameObject).GetPauseTimes();

            data.WriteBit(flags.HasEntityPosition);
            data.WriteBit(flags.NoBirthAnim);
            data.WriteBit(flags.EnablePortals);
            data.WriteBit(flags.PlayHoverAnim);
            data.WriteBit(flags.ThisIsYou);
            data.WriteBit(flags.MovementUpdate);
            data.WriteBit(flags.MovementTransport);
            data.WriteBit(flags.Stationary);
            data.WriteBit(flags.CombatVictim);
            data.WriteBit(flags.ServerTime);
            data.WriteBit(flags.Vehicle);
            data.WriteBit(flags.AnimKit);
            data.WriteBit(flags.Rotation);
            data.WriteBit(flags.GameObject);
            data.WriteBit(flags.SmoothPhasing);
            data.WriteBit(flags.SceneObject);
            data.WriteBit(flags.ActivePlayer);
            data.WriteBit(flags.Conversation);
            data.WriteBit(flags.Room);
            data.WriteBit(flags.Decor);
            data.WriteBit(flags.MeshObject);
            data.FlushBits();

            if (flags.MovementUpdate)
            {
                Unit unit = this as Unit;
                bool HasFallDirection = unit.HasUnitMovementFlag(MovementFlag.Falling);
                bool HasFall = HasFallDirection || unit.m_movementInfo.jump.fallTime != 0;
                bool HasSpline = unit.IsSplineEnabled();
                bool HasInertia = unit.m_movementInfo.inertia.HasValue;
                bool HasAdvFlying = unit.m_movementInfo.advFlying.HasValue;
                bool HasDriveStatus = unit.m_movementInfo.driveStatus.HasValue;
                bool HasStandingOnGameObjectGUID = unit.m_movementInfo.standingOnGameObjectGUID.HasValue;

                data.WritePackedGuid(GetGUID());                                         // MoverGUID

                data.WriteUInt32((uint)unit.GetUnitMovementFlags());
                data.WriteUInt32((uint)unit.GetUnitMovementFlags2());
                data.WriteUInt32((uint)unit.GetExtraUnitMovementFlags2());

                data.WriteUInt32(unit.m_movementInfo.Time);                     // MoveTime
                data.WriteFloat(unit.GetPositionX());
                data.WriteFloat(unit.GetPositionY());
                data.WriteFloat(unit.GetPositionZ());
                data.WriteFloat(unit.GetOrientation());

                data.WriteFloat(unit.m_movementInfo.Pitch);                     // Pitch
                data.WriteFloat(unit.m_movementInfo.stepUpStartElevation);           // StepUpStartElevation

                data.WriteUInt32(0);                                             // RemoveForcesIDs.size()
                data.WriteUInt32(0);                                             // MoveIndex

                //for (public uint i = 0; i < RemoveForcesIDs.Count; ++i)
                //    *data << ObjectGuid(RemoveForcesIDs);

                data.WriteBit(HasStandingOnGameObjectGUID);                    // HasStandingOnGameObjectGUID
                data.WriteBit(!unit.m_movementInfo.transport.guid.IsEmpty());  // HasTransport
                data.WriteBit(HasFall);                                        // HasFall
                data.WriteBit(HasSpline);                                      // HasSpline - marks that the unit uses spline movement
                data.WriteBit(false);                                          // HeightChangeFailed
                data.WriteBit(false);                                          // RemoteTimeValid
                data.WriteBit(HasInertia);                                     // HasInertia
                data.WriteBit(HasAdvFlying);                                   // HasAdvFlying
                data.WriteBit(HasDriveStatus);                                 // HasDriveStatus
                data.FlushBits();

                if (!unit.m_movementInfo.transport.guid.IsEmpty())
                    MovementExtensions.WriteTransportInfo(data, unit.m_movementInfo.transport);

                if (HasStandingOnGameObjectGUID)
                    data.WritePackedGuid(unit.m_movementInfo.standingOnGameObjectGUID.Value);

                if (HasInertia)
                {
                    data.WriteInt32(unit.m_movementInfo.inertia.Value.id);
                    data.WriteXYZ(unit.m_movementInfo.inertia.Value.force);
                    data.WriteUInt32(unit.m_movementInfo.inertia.Value.lifetime);
                }

                if (HasAdvFlying)
                {
                    data.WriteFloat(unit.m_movementInfo.advFlying.Value.forwardVelocity);
                    data.WriteFloat(unit.m_movementInfo.advFlying.Value.upVelocity);
                }

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

                if (HasDriveStatus)
                {
                    data.WriteFloat(unit.m_movementInfo.driveStatus.Value.speed);
                    data.WriteFloat(unit.m_movementInfo.driveStatus.Value.movementAngle);
                    data.WriteBit(unit.m_movementInfo.driveStatus.Value.accelerating);
                    data.WriteBit(unit.m_movementInfo.driveStatus.Value.drifting);
                    data.FlushBits();
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

                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.AirFriction));
                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.MaxVel));
                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.LiftCoefficient));
                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.DoubleJumpVelMod));
                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.GlideStartMinHeight));
                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.AddImpulseMaxSpeed));
                data.WriteFloat(unit.GetAdvFlyingSpeedMin(AdvFlyingRateTypeRange.BankingRate));
                data.WriteFloat(unit.GetAdvFlyingSpeedMax(AdvFlyingRateTypeRange.BankingRate));
                data.WriteFloat(unit.GetAdvFlyingSpeedMin(AdvFlyingRateTypeRange.PitchingRateDown));
                data.WriteFloat(unit.GetAdvFlyingSpeedMax(AdvFlyingRateTypeRange.PitchingRateDown));
                data.WriteFloat(unit.GetAdvFlyingSpeedMin(AdvFlyingRateTypeRange.PitchingRateUp));
                data.WriteFloat(unit.GetAdvFlyingSpeedMax(AdvFlyingRateTypeRange.PitchingRateUp));
                data.WriteFloat(unit.GetAdvFlyingSpeedMin(AdvFlyingRateTypeRange.TurnVelocityThreshold));
                data.WriteFloat(unit.GetAdvFlyingSpeedMax(AdvFlyingRateTypeRange.TurnVelocityThreshold));
                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.SurfaceFriction));
                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.OverMaxDeceleration));
                data.WriteFloat(unit.GetAdvFlyingSpeed(AdvFlyingRateTypeSingle.LaunchSpeedCoefficient));

                data.WriteBit(HasSpline);
                data.FlushBits();

                if (movementForces != null)
                    foreach (MovementForce force in movementForces.GetForces())
                        MovementExtensions.WriteMovementForceWithDirection(force, data, unit);

                // HasMovementSpline - marks that spline data is present in packet
                if (HasSpline)
                    MovementExtensions.WriteCreateObjectSplineDataBlock(unit.MoveSpline, data);
            }

            data.WriteInt32(PauseTimes != null ? PauseTimes.Count : 0);

            if (flags.Stationary)
            {
                WorldObject self = this as WorldObject;
                data.WriteXYZO(self.GetStationaryPosition());
            }

            if (flags.CombatVictim)
            {
                Unit unit = this as Unit;
                data.WritePackedGuid(unit.GetVictim().GetGUID());                          // CombatVictim
            }

            if (flags.ServerTime)
                data.WriteUInt32(GameTime.GetGameTimeMS());

            if (flags.Vehicle)
            {
                Unit unit = this as Unit;
                data.WriteUInt32(unit.GetVehicleKit().GetVehicleInfo().Id); // RecID
                data.WriteFloat(unit.GetOrientation());                         // InitialRawFacing
            }

            if (flags.AnimKit)
            {
                WorldObject self = this as WorldObject;
                data.WriteUInt16(self.GetAIAnimKitId());                        // AiID
                data.WriteUInt16(self.GetMovementAnimKitId());                  // MovementID
                data.WriteUInt16(self.GetMeleeAnimKitId());                     // MeleeID
            }

            if (flags.Rotation)
            {
                GameObject gameObject = this as GameObject;
                data.WriteInt64(gameObject.GetPackedLocalRotation());                 // Rotation
            }

            //if (flags.Room)
            //    *data << ObjectGuid(HouseGUID);

            //if (flags.Decor)
            //    *data << ObjectGuid(RoomGUID);

            //if (flags.MeshObject)
            //{
            //    *data << ObjectGuid(AttachParentGUID);
            //    *data << TaggedPosition<Position::XYZ>(PositionLocalSpace);
            //    *data << QuaternionData(RotationLocalSpace);
            //    *data << float(ScaleLocalSpace);
            //    *data << uint8(AttachmentFlags);
            //}

            if (PauseTimes != null && !PauseTimes.Empty())
                foreach (var stopFrame in PauseTimes)
                    data.WriteUInt32(stopFrame);

            if (flags.MovementTransport)
            {
                WorldObject self = this as WorldObject;
                MovementExtensions.WriteTransportInfo(data, self.m_movementInfo.transport);
            }

            if (flags.GameObject)
            {
                GameObject gameObject = this as GameObject;
                Transport transport = gameObject.ToTransport();

                bool bit8 = false;

                data.WriteUInt32(gameObject.GetWorldEffectID());

                data.WriteBit(bit8);
                data.WriteBit(transport != null);
                data.WriteBit(gameObject.GetPathProgressForClient().HasValue);
                data.FlushBits();
                if (transport != null)
                {
                    uint period = transport.GetTransportPeriod();

                    data.WriteUInt32((uint)((((long)transport.GetTimer() - (long)GameTime.GetGameTimeMS()) % period) + period) % period);  // TimeOffset
                    data.WriteUInt32(transport.GetNextStopTimestamp().GetValueOrDefault(0));
                    data.WriteBit(transport.GetNextStopTimestamp().HasValue);
                    data.WriteBit(transport.IsStopped());
                    data.WriteBit(false);
                    data.FlushBits();
                }

                if (bit8)
                    data.WriteUInt32(0);

                if (gameObject.GetPathProgressForClient().HasValue)
                    data.WriteFloat(gameObject.GetPathProgressForClient().Value);
            }

            if (flags.SmoothPhasing)
            {
                SmoothPhasingInfo smoothPhasingInfo = (this as WorldObject).GetSmoothPhasing().GetInfoForSeer(target.GetGUID());
                Cypher.Assert(smoothPhasingInfo != null);

                data.WriteBit(smoothPhasingInfo.ReplaceActive);
                data.WriteBit(smoothPhasingInfo.StopAnimKits);
                data.WriteBit(smoothPhasingInfo.ReplaceObject.HasValue);
                data.FlushBits();
                if (smoothPhasingInfo.ReplaceObject.HasValue)
                    data.WritePackedGuid(smoothPhasingInfo.ReplaceObject.Value);
            }

            if (flags.SceneObject)
            {
                data.WriteBit(false); // HasLocalScriptData
                data.WriteBit(false); // HasPetBattleFullUpdate
                data.FlushBits();

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
                //            data.WriteInt32(Players[i].TrapAbilityID);
                //            data.WriteInt32(Players[i].TrapStatus);
                //            *data << uint16(Players[i].RoundTimeSecs);
                //            data.WriteInt8(Players[i].FrontPet);
                //            *data << uint8(Players[i].InputFlags);

                //            data.WriteBits(Players[i].Pets.size(), 2);
                //            data.FlushBits();
                //            for (std::size_t j = 0; j < Players[i].Pets.size(); ++j)
                //            {
                //                *data << ObjectGuid(Players[i].Pets[j].BattlePetGUID);
                //                data.WriteInt32(Players[i].Pets[j].SpeciesID);
                //                data.WriteInt32(Players[i].Pets[j].CreatureID);
                //                data.WriteInt32(Players[i].Pets[j].DisplayID);
                //                *data << int16(Players[i].Pets[j].Level);
                //                *data << int16(Players[i].Pets[j].Xp);
                //                data.WriteInt32(Players[i].Pets[j].CurHealth);
                //                data.WriteInt32(Players[i].Pets[j].MaxHealth);
                //                data.WriteInt32(Players[i].Pets[j].Power);
                //                data.WriteInt32(Players[i].Pets[j].Speed);
                //                data.WriteInt32(Players[i].Pets[j].NpcTeamMemberID);
                //                *data << uint8(Players[i].Pets[j].BreedQuality);
                //                *data << uint16(Players[i].Pets[j].StatusFlags);
                //                data.WriteInt8(Players[i].Pets[j].Slot);

                //                *data << uint(Players[i].Pets[j].Abilities.size());
                //                *data << uint(Players[i].Pets[j].Auras.size());
                //                *data << uint(Players[i].Pets[j].States.size());
                //                for (std::size_t k = 0; k < Players[i].Pets[j].Abilities.size(); ++k)
                //                {
                //                    data.WriteInt32(Players[i].Pets[j].Abilities[k].AbilityID);
                //                    *data << int16(Players[i].Pets[j].Abilities[k].CooldownRemaining);
                //                    *data << int16(Players[i].Pets[j].Abilities[k].LockdownRemaining);
                //                    data.WriteInt8(Players[i].Pets[j].Abilities[k].AbilityIndex);
                //                    *data << uint8(Players[i].Pets[j].Abilities[k].Pboid);
                //                }

                //                for (std::size_t k = 0; k < Players[i].Pets[j].Auras.size(); ++k)
                //                {
                //                    data.WriteInt32(Players[i].Pets[j].Auras[k].AbilityID);
                //                    *data << uint(Players[i].Pets[j].Auras[k].InstanceID);
                //                    data.WriteInt32(Players[i].Pets[j].Auras[k].RoundsRemaining);
                //                    data.WriteInt32(Players[i].Pets[j].Auras[k].CurrentRound);
                //                    *data << uint8(Players[i].Pets[j].Auras[k].CasterPBOID);
                //                }

                //                for (std::size_t k = 0; k < Players[i].Pets[j].States.size(); ++k)
                //                {
                //                    *data << uint(Players[i].Pets[j].States[k].StateID);
                //                    data.WriteInt32(Players[i].Pets[j].States[k].StateValue);
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
                //                data.WriteInt32(Enviros[j].Auras[j].AbilityID);
                //                *data << uint(Enviros[j].Auras[j].InstanceID);
                //                data.WriteInt32(Enviros[j].Auras[j].RoundsRemaining);
                //                data.WriteInt32(Enviros[j].Auras[j].CurrentRound);
                //                *data << uint8(Enviros[j].Auras[j].CasterPBOID);
                //            }

                //            for (std::size_t j = 0; j < Enviros[j].States.size(); ++j)
                //            {
                //                *data << uint(Enviros[i].States[j].StateID);
                //                data.WriteInt32(Enviros[i].States[j].StateValue);
                //            }
                //        }

                //        *data << uint16(WaitingForFrontPetsMaxSecs);
                //        *data << uint16(PvpMaxRoundTime);
                //        data.WriteInt32(CurRound);
                //        *data << uint(NpcCreatureID);
                //        *data << uint(NpcDisplayID);
                //        data.WriteInt8(CurPetBattleState);
                //        *data << uint8(ForfeitPenalty);
                //        *data << ObjectGuid(InitialWildPetGUID);
                //        data.WriteBit(IsPVP);
                //        data.WriteBit(CanAwardXP);
                //        data.FlushBits();
                //    }
            }

            if (flags.ActivePlayer)
            {
                Player player = this as Player;

                bool HasSceneInstanceIDs = !player.GetSceneMgr().GetSceneTemplateByInstanceMap().Empty();
                bool HasRuneState = player.GetPowerIndex(PowerType.Runes) != (int)PowerType.Max;

                data.WriteBit(HasSceneInstanceIDs);
                data.WriteBit(HasRuneState);
                data.FlushBits();

                if (HasSceneInstanceIDs)
                {
                    data.WriteInt32(player.GetSceneMgr().GetSceneTemplateByInstanceMap().Count);
                    foreach (var (sceneInstanceId, _) in player.GetSceneMgr().GetSceneTemplateByInstanceMap())
                        data.WriteUInt32(sceneInstanceId);
                }

                if (HasRuneState)
                {
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
                Conversation self = this as Conversation;
                if (data.WriteBit(self.GetTextureKitId() != 0))
                    data.WriteUInt32(self.GetTextureKitId());
                data.FlushBits();
            }
        }

        public virtual UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
        {
            return UpdateFieldFlag.None;
        }

        public virtual bool AddToObjectUpdate() { return false; }

        public virtual void RemoveFromObjectUpdate() { }

        public void AddToObjectUpdateIfNeeded()
        {
            if (IsInWorld && !_objectUpdated)
                _objectUpdated = AddToObjectUpdate();
        }

        public virtual void ClearUpdateMask(bool remove)
        {
            EntityFragments.IdsChanged = false;

            if (_objectUpdated)
            {
                if (remove)
                    RemoveFromObjectUpdate();

                _objectUpdated = false;
            }
        }

        public void BuildUpdateChangesMask()
        {
            for (int i = 0; i < EntityFragments.UpdateableCount; ++i)
            {
                if (EntityFragments.Updateable.IsChanged[i](this))
                    EntityFragments.ContentsChangedMask |= EntityFragments.Updateable.Masks[i];
                else
                    EntityFragments.ContentsChangedMask &= (byte)~EntityFragments.Updateable.Masks[i];
            }
        }

        public void BuildFieldsUpdate(Player player, Dictionary<Player, UpdateData> data_map)
        {
            if (!data_map.ContainsKey(player))
                data_map.Add(player, new UpdateData(player.GetMapId()));

            BuildValuesUpdateBlockForPlayer(data_map[player], player);
        }

        public override string GetDebugInfo()
        {
            return $"{GetGUID()}";
        }

        public bool IsInWorld { get; private set; }

        public static ObjectGuid GetGUID(BaseEntity o) { return o != null ? o.GetGUID() : ObjectGuid.Empty; }
        public ObjectGuid GetGUID() { return _guid; }

        public TypeId GetTypeId() { return ObjectTypeId; }
        public bool IsTypeId(TypeId typeId) { return GetTypeId() == typeId; }
        public bool IsTypeMask(TypeMask mask) { return (ObjectTypeMask[(int)ObjectTypeId] & mask) != 0; }

        public bool IsWorldObject() { return IsTypeMask(TypeMask.WorldObject); }
        public bool IsItem() { return IsTypeMask(TypeMask.Item); }
        public bool IsUnit() { return IsTypeMask(TypeMask.Unit); }
        public bool IsCreature() { return GetTypeId() == TypeId.Unit; }
        public bool IsPlayer() { return GetTypeId() == TypeId.Player; }
        public bool IsGameObject() { return GetTypeId() == TypeId.GameObject; }
        public bool IsDynObject() { return GetTypeId() == TypeId.DynamicObject; }
        public bool IsCorpse() { return GetTypeId() == TypeId.Corpse; }
        public bool IsAreaTrigger() { return GetTypeId() == TypeId.AreaTrigger; }
        public bool IsSceneObject() { return GetTypeId() == TypeId.SceneObject; }
        public bool IsConversation() { return GetTypeId() == TypeId.Conversation; }
        public bool IsMeshObject() { return GetTypeId() == TypeId.MeshObject; }

        public void SetIsNewObject(bool enable) { _isNewObject = enable; }
        public bool IsDestroyedObject() { return _isDestroyedObject; }
        public void SetDestroyedObject(bool destroyed) { _isDestroyedObject = destroyed; }

        public virtual void BuildUpdate(Dictionary<Player, UpdateData> data_map) { }

        public void ForceUpdateFieldChange()
        {
            AddToObjectUpdateIfNeeded();
        }

        public void _Create(ObjectGuid guid) { _guid = guid; }

        public void SetUpdateFieldValue<T>(IUpdateField<T> updateField, T newValue)
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

        public void SetUpdateFieldValue(ref string value, string newValue)
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

        public void SetUpdateFieldFlagValue<T>(DynamicUpdateFieldSetter<T> updateField, T flag) where T : new()
        {
            //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
            SetUpdateFieldValue(updateField, (T)(updateField.GetValue() | (dynamic)flag));
        }

        public void SetUpdateFieldFlagValue<T>(ref T value, T flag) where T : new()
        {
            //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
            SetUpdateFieldValue(ref value, (T)(value | (dynamic)flag));
        }

        public void RemoveUpdateFieldFlagValue<T>(IUpdateField<T> updateField, T flag)
        {
            //static_assert(std::is_integral < T >::value, "SetUpdateFieldFlagValue must be used with integral types");
            SetUpdateFieldValue(updateField, (T)(updateField.GetValue() & ~(dynamic)flag));
        }

        public void RemoveUpdateFieldFlagValue<T>(DynamicUpdateFieldSetter<T> updateField, T flag) where T : new()
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

        public void RemoveMapUpdateFieldValue<K, V>(MapUpdateField<K, V> setter, K key) where V : new()
        {
            AddToObjectUpdateIfNeeded();
            setter.MarkKeyForRemoval(key);
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

        public void DoWithSuppressingObjectUpdates(Action action)
        {
            bool wasUpdatedBeforeAction = _objectUpdated;
            action();
            if (_objectUpdated && !wasUpdatedBeforeAction)
            {
                RemoveFromObjectUpdate();
                _objectUpdated = false;
            }
        }

        public TypeMask[] ObjectTypeMask =
        {
            TypeMask.Object,
            TypeMask.Object | TypeMask.Item,
            TypeMask.Object | TypeMask.Item | TypeMask.Container,
            TypeMask.Object | TypeMask.Item | TypeMask.AzeriteEmpoweredItem,
            TypeMask.Object | TypeMask.Item | TypeMask.AzeriteItem,
            TypeMask.Object | TypeMask.Unit,
            TypeMask.Object | TypeMask.Unit | TypeMask.Player,
            TypeMask.Object | TypeMask.Unit | TypeMask.Player | TypeMask.ActivePlayer,
            TypeMask.Object | TypeMask.GameObject,
            TypeMask.Object | TypeMask.DynamicObject,
            TypeMask.Object | TypeMask.Corpse,
            TypeMask.Object | TypeMask.AreaTrigger,
            TypeMask.Object | TypeMask.SceneObject,
            TypeMask.Object | TypeMask.Conversation,
            TypeMask.Object  | TypeMask.MeshObject,
            TypeMask.Object  | TypeMask.AIGroup,
            TypeMask.Object  | TypeMask.Scenario,
            TypeMask.Object  | TypeMask.LootObject,
            0
        };
    }

    public struct CreateObjectBits
    {
        public bool HasEntityPosition;
        public bool NoBirthAnim;
        public bool EnablePortals;
        public bool PlayHoverAnim;
        public bool ThisIsYou;
        public bool MovementUpdate;
        public bool MovementTransport;
        public bool Stationary;
        public bool CombatVictim;
        public bool ServerTime;
        public bool Vehicle;
        public bool AnimKit;
        public bool Rotation;
        public bool GameObject;
        public bool SmoothPhasing;
        public bool SceneObject;
        public bool ActivePlayer;
        public bool Conversation;
        public bool Room;
        public bool Decor;
        public bool MeshObject;

        public void Clear()
        {
            HasEntityPosition = false;
            NoBirthAnim = false;
            EnablePortals = false;
            PlayHoverAnim = false;
            ThisIsYou = false;
            MovementUpdate = false;
            MovementTransport = false;
            Stationary = false;
            CombatVictim = false;
            ServerTime = false;
            Vehicle = false;
            AnimKit = false;
            Rotation = false;
            GameObject = false;
            SmoothPhasing = false;
            SceneObject = false;
            ActivePlayer = false;
            Conversation = false;
            Room = false;
            Decor = false;
            MeshObject = false;
        }
    }

    public class UpdateFieldHolder
    {
        UpdateMask _changesMask = new((int)TypeId.Max);
        BaseEntity _owner;

        public UpdateFieldHolder(BaseEntity owner)
        {
            _owner = owner;
        }

        public HasChangesMask ModifyValue(HasChangesMask updateData)
        {
            if ((EntityFragment)updateData._blockBit == EntityFragment.CGObject)
                _changesMask.Set(updateData.Bit);
            return updateData;
        }

        public void ClearChangesMask(HasChangesMask updateData)
        {
            if (updateData == null)
                return;

            if ((EntityFragment)updateData._blockBit == EntityFragment.CGObject)
                _changesMask.Reset(updateData.Bit);

            updateData.ClearChangesMask();
        }

        public void ClearChangesMask<U>(HasChangesMask updateData, ref UpdateField<U> updateField) where U : new()
        {
            if ((EntityFragment)updateData._blockBit == EntityFragment.CGObject)
                _changesMask.Reset(updateData.Bit);

            if (typeof(IHasChangesMask).IsAssignableFrom(typeof(U)))
                ((IHasChangesMask)updateField._value).ClearChangesMask();
        }

        public uint GetChangedObjectTypeMask()
        {
            return _changesMask.GetBlock(0);
        }

        public bool HasChanged(TypeId index)
        {
            return _changesMask[(int)index];
        }
    }


}
