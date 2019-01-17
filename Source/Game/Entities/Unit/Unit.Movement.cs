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
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Maps;
using Game.Movement;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using Game.Spells;

namespace Game.Entities
{
    public partial class Unit
    {
        public bool IsLevitating()
        {
            return m_movementInfo.HasMovementFlag(MovementFlag.DisableGravity);
        }
        public bool IsWalking()
        {
            return m_movementInfo.HasMovementFlag(MovementFlag.Walking);
        }
        bool IsHovering() { return m_movementInfo.HasMovementFlag(MovementFlag.Hover); }
        public bool isStopped()
        {
            return !(HasUnitState(UnitState.Moving));
        }
        public bool isMoving() { return m_movementInfo.HasMovementFlag(MovementFlag.MaskMoving); }
        public bool isTurning() { return m_movementInfo.HasMovementFlag(MovementFlag.MaskTurning); }
        public virtual bool CanFly() { return false; }
        public bool IsFlying() { return m_movementInfo.HasMovementFlag(MovementFlag.Flying | MovementFlag.DisableGravity); }
        public bool IsFalling()
        {
            return m_movementInfo.HasMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar) || moveSpline.isFalling();
        }
        public virtual bool CanSwim()
        {
            // Mirror client behavior, if this method returns false then client will not use swimming animation and for players will apply gravity as if there was no water
            if (HasFlag(UnitFields.Flags, UnitFlags.CannotSwim))
                return false;
            if (HasFlag(UnitFields.Flags, UnitFlags.PvpAttackable)) // is player
                return true;
            if (HasFlag(UnitFields.Flags2, 0x1000000))
                return false;
            return HasFlag(UnitFields.Flags, UnitFlags.PetInCombat | UnitFlags.Rename | UnitFlags.Unk15);
        }
        public virtual bool IsInWater()
        {
            return GetMap().IsInWater(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZ());
        }
        public virtual bool IsUnderWater()
        {
            return GetMap().IsUnderWater(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZ());
        }

        void propagateSpeedChange() { GetMotionMaster().propagateSpeedChange(); }

        public float GetSpeed(UnitMoveType mtype)
        {
            return m_speed_rate[(int)mtype] * (IsControlledByPlayer() ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]);
        }

        public void SetSpeed(UnitMoveType mtype, float newValue)
        {
            SetSpeedRate(mtype, newValue / (IsControlledByPlayer() ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]));
        }

        public void SetSpeedRate(UnitMoveType mtype, float rate)
        {
            rate = Math.Max(rate, 0.01f);

            if (m_speed_rate[(int)mtype] == rate)
                return;

            m_speed_rate[(int)mtype] = rate;

            propagateSpeedChange();

            // Spline packets are for creatures and move_update are for players
            ServerOpcodes[,] moveTypeToOpcode = new ServerOpcodes[(int)UnitMoveType.Max, 3]
            {
                { ServerOpcodes.MoveSplineSetWalkSpeed,         ServerOpcodes.MoveSetWalkSpeed,         ServerOpcodes.MoveUpdateWalkSpeed       },
                { ServerOpcodes.MoveSplineSetRunSpeed,          ServerOpcodes.MoveSetRunSpeed,          ServerOpcodes.MoveUpdateRunSpeed        },
                { ServerOpcodes.MoveSplineSetRunBackSpeed,      ServerOpcodes.MoveSetRunBackSpeed,      ServerOpcodes.MoveUpdateRunBackSpeed    },
                { ServerOpcodes.MoveSplineSetSwimSpeed,         ServerOpcodes.MoveSetSwimSpeed,         ServerOpcodes.MoveUpdateSwimSpeed       },
                { ServerOpcodes.MoveSplineSetSwimBackSpeed,     ServerOpcodes.MoveSetSwimBackSpeed,     ServerOpcodes.MoveUpdateSwimBackSpeed   },
                { ServerOpcodes.MoveSplineSetTurnRate,          ServerOpcodes.MoveSetTurnRate,          ServerOpcodes.MoveUpdateTurnRate        },
                { ServerOpcodes.MoveSplineSetFlightSpeed,       ServerOpcodes.MoveSetFlightSpeed,       ServerOpcodes.MoveUpdateFlightSpeed     },
                { ServerOpcodes.MoveSplineSetFlightBackSpeed,   ServerOpcodes.MoveSetFlightBackSpeed,   ServerOpcodes.MoveUpdateFlightBackSpeed },
                { ServerOpcodes.MoveSplineSetPitchRate,         ServerOpcodes.MoveSetPitchRate,         ServerOpcodes.MoveUpdatePitchRate       },
            };

            if (IsTypeId(TypeId.Player))
            {
                // register forced speed changes for WorldSession.HandleForceSpeedChangeAck
                // and do it only for real sent packets and use run for run/mounted as client expected
                ++ToPlayer().m_forced_speed_changes[(int)mtype];

                if (!IsInCombat())
                {
                    Pet pet = ToPlayer().GetPet();
                    if (pet)
                        pet.SetSpeedRate(mtype, m_speed_rate[(int)mtype]);
                }
            }

            Player playerMover = GetPlayerBeingMoved(); // unit controlled by a player.
            if (playerMover)
            {
                // Send notification to self
                MoveSetSpeed selfpacket = new MoveSetSpeed(moveTypeToOpcode[(int)mtype, 1]);
                selfpacket.MoverGUID = GetGUID();
                selfpacket.SequenceIndex = m_movementCounter++;
                selfpacket.Speed = GetSpeed(mtype);
                playerMover.SendPacket(selfpacket);

                // Send notification to other players
                MoveUpdateSpeed packet = new MoveUpdateSpeed(moveTypeToOpcode[(int)mtype, 2]);
                packet.Status = m_movementInfo;
                packet.Speed = GetSpeed(mtype);
                playerMover.SendMessageToSet(packet, false);
            }
            else
            {
                MoveSplineSetSpeed packet = new MoveSplineSetSpeed(moveTypeToOpcode[(int)mtype, 0]);
                packet.MoverGUID = GetGUID();
                packet.Speed = GetSpeed(mtype);
                SendMessageToSet(packet, true);
            }
        }

        public float GetSpeedRate(UnitMoveType mtype) { return m_speed_rate[(int)mtype]; }

        public void StopMoving()
        {
            ClearUnitState(UnitState.Moving);

            // not need send any packets if not in world or not moving
            if (!IsInWorld || moveSpline.Finalized())
                return;

            MoveSplineInit init = new MoveSplineInit(this);
            init.Stop();
        }

        public void SetInFront(WorldObject target)
        {
            if (!HasUnitState(UnitState.CannotTurn))
                Orientation = GetAngle(target.GetPosition());
        }

        public void SetFacingTo(float ori, bool force = false)
        {
            if (!force && !IsStopped())
                return;

            MoveSplineInit init = new MoveSplineInit(this);
            init.MoveTo(GetPositionX(), GetPositionY(), GetPositionZMinusOffset(), false);
            init.SetFacing(ori);
            init.Launch();
        }

        public void SetFacingToObject(WorldObject obj, bool force = false)
        {
            // do not face when already moving
            if (!force && !IsStopped())
                return;

            // @todo figure out under what conditions creature will move towards object instead of facing it where it currently is.
            SetFacingTo(GetAngle(obj));
        }

        public void MonsterMoveWithSpeed(float x, float y, float z, float speed, bool generatePath = false, bool forceDestination = false)
        {
            MoveSplineInit init = new MoveSplineInit(this);
            init.MoveTo(x, y, z, generatePath, forceDestination);
            init.SetVelocity(speed);
            init.Launch();
        }

        public void KnockbackFrom(float x, float y, float speedXY, float speedZ, SpellEffectExtraData spellEffectExtraData = null)
        {
            Player player = ToPlayer();
            if (!player)
            {
                Unit charmer = GetCharmer();
                if (charmer)
                {
                    player = charmer.ToPlayer();
                    if (player && player.m_unitMovedByMe != this)
                        player = null;
                }
            }

            if (!player)
                GetMotionMaster().MoveKnockbackFrom(x, y, speedXY, speedZ, spellEffectExtraData);
            else
            {
                float vcos, vsin;
                GetSinCos(x, y, out vsin, out vcos);
                SendMoveKnockBack(player, speedXY, -speedZ, vcos, vsin);
            }
        }

        void SendMoveKnockBack(Player player, float speedXY, float speedZ, float vcos, float vsin)
        {
            MoveKnockBack moveKnockBack = new MoveKnockBack();
            moveKnockBack.MoverGUID = GetGUID();
            moveKnockBack.SequenceIndex = m_movementCounter++;
            moveKnockBack.Speeds.HorzSpeed = speedXY;
            moveKnockBack.Speeds.VertSpeed = speedZ;
            moveKnockBack.Direction = new Vector2(vcos, vsin);
            player.SendPacket(moveKnockBack);
        }

        bool SetCollision(bool disable)
        {
            if (disable == HasUnitMovementFlag(MovementFlag.DisableCollision))
                return false;

            if (disable)
                AddUnitMovementFlag(MovementFlag.DisableCollision);
            else
                RemoveUnitMovementFlag(MovementFlag.DisableCollision);

            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(disable ? ServerOpcodes.MoveSplineEnableCollision : ServerOpcodes.MoveEnableCollision);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }
            else
            {
                MoveSplineSetFlag packet = new MoveSplineSetFlag(disable ? ServerOpcodes.MoveSplineDisableCollision : ServerOpcodes.MoveDisableCollision);
                packet.MoverGUID = GetGUID();
                SendMessageToSet(packet, true);
            }

            return true;
        }

        public bool SetCanTransitionBetweenSwimAndFly(bool enable)
        {
            if (!IsTypeId(TypeId.Player))
                return false;

            if (enable == HasUnitMovementFlag2(MovementFlag2.CanSwimToFlyTrans))
                return false;

            if (enable)
                AddUnitMovementFlag2(MovementFlag2.CanSwimToFlyTrans);
            else
                RemoveUnitMovementFlag2(MovementFlag2.CanSwimToFlyTrans);

            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(enable ? ServerOpcodes.MoveEnableTransitionBetweenSwimAndFly : ServerOpcodes.MoveDisableTransitionBetweenSwimAndFly);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }

            return true;
        }

        public bool SetCanTurnWhileFalling(bool enable)
        {
            if (enable == HasUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling))
                return false;

            if (enable)
                AddUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling);
            else
                RemoveUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling);

            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(enable ? ServerOpcodes.MoveSetCanTurnWhileFalling : ServerOpcodes.MoveUnsetCanTurnWhileFalling);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }

            return true;
        }

        public bool SetCanDoubleJump(bool enable)
        {
            if (enable == HasUnitMovementFlag2(MovementFlag2.CanDoubleJump))
                return false;

            if (enable)
                AddUnitMovementFlag2(MovementFlag2.CanDoubleJump);
            else
                RemoveUnitMovementFlag2(MovementFlag2.CanDoubleJump);

            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(enable ? ServerOpcodes.MoveEnableDoubleJump : ServerOpcodes.MoveDisableDoubleJump);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }

            return true;
        }

        public void JumpTo(float speedXY, float speedZ, bool forward)
        {
            float angle = forward ? 0 : MathFunctions.PI;
            if (IsTypeId(TypeId.Unit))
                GetMotionMaster().MoveJumpTo(angle, speedXY, speedZ);
            else
            {
                float vcos = (float)Math.Cos(angle + GetOrientation());
                float vsin = (float)Math.Sin(angle + GetOrientation());
                SendMoveKnockBack(ToPlayer(), speedXY, -speedZ, vcos, vsin);
            }
        }

        public void JumpTo(WorldObject obj, float speedZ, bool withOrientation = false)
        {
            float x, y, z;
            obj.GetContactPoint(this, out x, out y, out z);
            float speedXY = GetExactDist2d(x, y) * 10.0f / speedZ;
            GetMotionMaster().MoveJump(x, y, z, GetAngle(obj), speedXY, speedZ, EventId.Jump, withOrientation);
        }

        public void UpdateSpeed(UnitMoveType mtype)
        {
            int main_speed_mod = 0;
            float stack_bonus = 1.0f;
            float non_stack_bonus = 1.0f;

            switch (mtype)
            {
                // Only apply debuffs
                case UnitMoveType.FlightBack:
                case UnitMoveType.RunBack:
                case UnitMoveType.SwimBack:
                    break;
                case UnitMoveType.Walk:
                    return;
                case UnitMoveType.Run:
                    {
                        if (IsMounted()) // Use on mount auras
                        {
                            main_speed_mod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseMountedSpeed);
                            stack_bonus = GetTotalAuraMultiplier(AuraType.ModMountedSpeedAlways);
                            non_stack_bonus += GetMaxPositiveAuraModifier(AuraType.ModMountedSpeedNotStack) / 100.0f;
                        }
                        else
                        {
                            main_speed_mod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseSpeed);
                            stack_bonus = GetTotalAuraMultiplier(AuraType.ModSpeedAlways);
                            non_stack_bonus += GetMaxPositiveAuraModifier(AuraType.ModSpeedNotStack) / 100.0f;
                        }
                        break;
                    }
                case UnitMoveType.Swim:
                    {
                        main_speed_mod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseSwimSpeed);
                        break;
                    }
                case UnitMoveType.Flight:
                    {
                        if (IsTypeId(TypeId.Unit) && IsControlledByPlayer()) // not sure if good for pet
                        {
                            main_speed_mod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseVehicleFlightSpeed);
                            stack_bonus = GetTotalAuraMultiplier(AuraType.ModVehicleSpeedAlways);

                            // for some spells this mod is applied on vehicle owner
                            int owner_speed_mod = 0;

                            Unit owner = GetCharmer();
                            if (owner != null)
                                owner_speed_mod = owner.GetMaxPositiveAuraModifier(AuraType.ModIncreaseVehicleFlightSpeed);

                            main_speed_mod = Math.Max(main_speed_mod, owner_speed_mod);
                        }
                        else if (IsMounted())
                        {
                            main_speed_mod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseMountedFlightSpeed);
                            stack_bonus = GetTotalAuraMultiplier(AuraType.ModMountedFlightSpeedAlways);
                        }
                        else             // Use not mount (shapeshift for example) auras (should stack)
                            main_speed_mod = GetTotalAuraModifier(AuraType.ModIncreaseFlightSpeed) + GetTotalAuraModifier(AuraType.ModIncreaseVehicleFlightSpeed);

                        non_stack_bonus += GetMaxPositiveAuraModifier(AuraType.ModFlightSpeedNotStack) / 100.0f;

                        // Update speed for vehicle if available
                        if (IsTypeId(TypeId.Player) && GetVehicle() != null)
                            GetVehicleBase().UpdateSpeed(UnitMoveType.Flight);
                        break;
                    }
                default:
                    Log.outError(LogFilter.Unit, "Unit.UpdateSpeed: Unsupported move type ({0})", mtype);
                    return;
            }

            // now we ready for speed calculation
            float speed = Math.Max(non_stack_bonus, stack_bonus);
            if (main_speed_mod != 0)
                MathFunctions.AddPct(ref speed, main_speed_mod);

            switch (mtype)
            {
                case UnitMoveType.Run:
                case UnitMoveType.Swim:
                case UnitMoveType.Flight:
                    {
                        // Set creature speed rate
                        if (IsTypeId(TypeId.Unit))
                        {
                            Unit pOwner = GetCharmerOrOwner();
                            if ((IsPet() || IsGuardian()) && !IsInCombat() && pOwner != null) // Must check for owner or crash on "Tame Beast"
                            {
                                // For every yard over 5, increase speed by 0.01
                                //  to help prevent pet from lagging behind and despawning
                                float dist = GetDistance(pOwner);
                                float base_rate = 1.00f; // base speed is 100% of owner speed

                                if (dist < 5)
                                    dist = 5;

                                float mult = base_rate + ((dist - 5) * 0.01f);

                                speed *= pOwner.GetSpeedRate(mtype) * mult; // pets derive speed from owner when not in combat
                            }
                            else
                                speed *= ToCreature().GetCreatureTemplate().SpeedRun;    // at this point, MOVE_WALK is never reached
                        }

                        // Normalize speed by 191 aura SPELL_AURA_USE_NORMAL_MOVEMENT_SPEED if need
                        // @todo possible affect only on MOVE_RUN
                        int normalization = GetMaxPositiveAuraModifier(AuraType.UseNormalMovementSpeed);
                        if (normalization != 0)
                        {
                            Creature creature = ToCreature();
                            if (creature)
                            {
                                uint immuneMask = creature.GetCreatureTemplate().MechanicImmuneMask;
                                if (Convert.ToBoolean(immuneMask & (1 << ((int)Mechanics.Snare - 1))) || Convert.ToBoolean(immuneMask & (1 << ((int)Mechanics.Daze - 1))))
                                    break;
                            }

                            // Use speed from aura
                            float max_speed = normalization / (IsControlledByPlayer() ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]);
                            if (speed > max_speed)
                                speed = max_speed;
                        }

                        if (mtype == UnitMoveType.Run)
                        {
                            // force minimum speed rate @ aura 437 SPELL_AURA_MOD_MINIMUM_SPEED_RATE
                            int minSpeedMod1 = GetMaxPositiveAuraModifier(AuraType.ModMinimumSpeedRate);
                            if (minSpeedMod1 != 0)
                            {
                                float minSpeed = minSpeedMod1 / (IsControlledByPlayer() ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]);
                                if (speed < minSpeed)
                                    speed = minSpeed;
                            }
                        }

                        break;
                    }
                default:
                    break;
            }

            // for creature case, we check explicit if mob searched for assistance
            if (IsTypeId(TypeId.Unit))
            {
                if (ToCreature().HasSearchedAssistance())
                    speed *= 0.66f;                                 // best guessed value, so this will be 33% reduction. Based off initial speed, mob can then "run", "walk fast" or "walk".
            }

            // Apply strongest slow aura mod to speed
            int slow = GetMaxNegativeAuraModifier(AuraType.ModDecreaseSpeed);
            if (slow != 0)
                MathFunctions.AddPct(ref speed, slow);

            float minSpeedMod = GetMaxPositiveAuraModifier(AuraType.ModMinimumSpeed);
            if (minSpeedMod != 0)
            {
                float min_speed = minSpeedMod / 100.0f;
                if (speed < min_speed)
                    speed = min_speed;
            }

            SetSpeedRate(mtype, speed);
        }

        public virtual bool UpdatePosition(Position obj, bool teleport = false)
        {
            return UpdatePosition(obj.posX, obj.posY, obj.posZ, obj.Orientation, teleport);
        }

        public virtual bool UpdatePosition(float x, float y, float z, float orientation, bool teleport = false)
        {
            if (!GridDefines.IsValidMapCoord(x, y, z, orientation))
            {
                Log.outError(LogFilter.Unit, "Unit.UpdatePosition({0}, {1}, {2}) .. bad coordinates!", x, y, z);
                return false;
            }

            // Check if angular distance changed
            bool turn = MathFunctions.fuzzyGt((float)Math.PI - Math.Abs(Math.Abs(GetOrientation() - orientation) - (float)Math.PI), 0.0f);
            // G3D::fuzzyEq won't help here, in some cases magnitudes differ by a little more than G3D::eps, but should be considered equal
            bool relocated = (teleport ||
                Math.Abs(GetPositionX() - x) > 0.001f ||
                Math.Abs(GetPositionY() - y) > 0.001f ||
                Math.Abs(GetPositionZ() - z) > 0.001f);

            if (turn)
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Turning);

            if (relocated)
            {
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Move);

                // move and update visible state if need
                if (IsTypeId(TypeId.Player))
                    GetMap().PlayerRelocation(ToPlayer(), x, y, z, orientation);
                else
                    GetMap().CreatureRelocation(ToCreature(), x, y, z, orientation);
            }
            else if (turn)
                UpdateOrientation(orientation);

            // code block for underwater state update
            UpdateUnderwaterState(GetMap(), x, y, z);

            return (relocated || turn);
        }

        void UpdateOrientation(float orientation)
        {
            Orientation = orientation;
            if (IsVehicle())
                GetVehicleKit().RelocatePassengers();
        }

        //! Only server-side height update, does not broadcast to client
        void UpdateHeight(float newZ)
        {
            Relocate(GetPositionX(), GetPositionY(), newZ);
            if (IsVehicle())
                GetVehicleKit().RelocatePassengers();
        }

        public bool IsWithinBoundaryRadius(Unit obj)
        {
            if (!obj || !IsInMap(obj) || !IsInPhase(obj))
                return false;

            float objBoundaryRadius = Math.Max(obj.GetBoundaryRadius(), SharedConst.MinMeleeReach);

            return IsInDist(obj, objBoundaryRadius);
        }

        public void GetRandomContactPoint(Unit obj, out float x, out float y, out float z, float distance2dMin, float distance2dMax)
        {
            float combat_reach = GetCombatReach();
            if (combat_reach < 0.1f)
                combat_reach = SharedConst.DefaultCombatReach;

            int attacker_number = getAttackers().Count;
            if (attacker_number > 0)
                --attacker_number;
            GetNearPoint(obj, out x, out y, out z, obj.GetCombatReach(), distance2dMin + (distance2dMax - distance2dMin) * (float)RandomHelper.NextDouble()
                , GetAngle(obj.GetPosition()) + (attacker_number != 0 ? MathFunctions.PiOver2 - MathFunctions.PI * (float)RandomHelper.NextDouble() * (float)attacker_number / combat_reach * 0.3f : 0.0f));
        }

        public bool SetDisableGravity(bool disable)
        {
            if (disable == IsLevitating())
                return false;

            if (disable)
            {
                AddUnitMovementFlag(MovementFlag.DisableGravity);
                RemoveUnitMovementFlag(MovementFlag.Swimming | MovementFlag.SplineElevation);
            }
            else
                RemoveUnitMovementFlag(MovementFlag.DisableGravity);


            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(disable ? ServerOpcodes.MoveDisableGravity : ServerOpcodes.MoveEnableGravity);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }
            else
            {
                MoveSplineSetFlag packet = new MoveSplineSetFlag(disable ? ServerOpcodes.MoveSplineDisableGravity : ServerOpcodes.MoveSplineEnableGravity);
                packet.MoverGUID = GetGUID();
                SendMessageToSet(packet, true);
            }

            return true;
        }

        public MountCapabilityRecord GetMountCapability(uint mountType)
        {
            if (mountType == 0)
                return null;

            var capabilities = Global.DB2Mgr.GetMountCapabilities(mountType);
            if (capabilities == null)
                return null;

            uint areaId = GetAreaId();
            uint ridingSkill = 5000;
            AreaMountFlags mountFlags = 0;
            bool isSubmerged = false;
            bool isInWater = false;

            if (IsTypeId(TypeId.Player))
                ridingSkill = ToPlayer().GetSkillValue(SkillType.Riding);

            if (HasAuraType(AuraType.MountRestrictions))
            {
                foreach (AuraEffect auraEffect in GetAuraEffectsByType(AuraType.MountRestrictions))
                    mountFlags |= (AreaMountFlags)auraEffect.GetMiscValue();
            }
            else
            {
                AreaTableRecord areaTable = CliDB.AreaTableStorage.LookupByKey(areaId);
                if (areaTable != null)
                    mountFlags = (AreaMountFlags)areaTable.MountFlags;
            }

            LiquidData liquid;
            ZLiquidStatus liquidStatus = GetMap().getLiquidStatus(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZ(), MapConst.MapAllLiquidTypes, out liquid);
            isSubmerged = liquidStatus.HasAnyFlag(ZLiquidStatus.UnderWater) || HasUnitMovementFlag(MovementFlag.Swimming);
            isInWater = liquidStatus.HasAnyFlag(ZLiquidStatus.InWater | ZLiquidStatus.UnderWater);

            foreach (var mountTypeXCapability in capabilities)
            {
                MountCapabilityRecord mountCapability = CliDB.MountCapabilityStorage.LookupByKey(mountTypeXCapability.MountCapabilityID);
                if (mountCapability == null)
                    continue;

                if (ridingSkill < mountCapability.ReqRidingSkill)
                    continue;

                if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.IgnoreRestrictions))
                {
                    if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Ground) && !mountFlags.HasAnyFlag(AreaMountFlags.GroundAllowed))
                        continue;
                    if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Flying) && !mountFlags.HasAnyFlag(AreaMountFlags.FlyingAllowed))
                        continue;
                    if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Float) && !mountFlags.HasAnyFlag(AreaMountFlags.FloatAllowed))
                        continue;
                    if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Underwater) && !mountFlags.HasAnyFlag(AreaMountFlags.UnderwaterAllowed))
                        continue;
                }

                if (!isSubmerged)
                {
                    if (!isInWater)
                    {
                        // player is completely out of water
                        if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Ground))
                            continue;
                    }
                    else if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Underwater))
                        continue;
                }
                else if (isInWater)
                {
                    if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Underwater))
                        continue;
                }
                else if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Float))
                    continue;

                if (mountCapability.ReqMapID != -1 &&
                    GetMapId() != mountCapability.ReqMapID &&
                    GetMap().GetEntry().CosmeticParentMapID != mountCapability.ReqMapID &&
                    GetMap().GetEntry().ParentMapID != mountCapability.ReqMapID)
                    continue;

                if (mountCapability.ReqAreaID != 0 && !Global.DB2Mgr.IsInArea(areaId, mountCapability.ReqAreaID))
                    continue;

                if (mountCapability.ReqSpellAuraID != 0 && !HasAura(mountCapability.ReqSpellAuraID))
                    continue;

                if (mountCapability.ReqSpellKnownID != 0 && !HasSpell(mountCapability.ReqSpellKnownID))
                    continue;

                return mountCapability;
            }

            return null;
        }

        public virtual void UpdateUnderwaterState(Map m, float x, float y, float z)
        {
            if (IsFlying() || (!IsPet() && !IsVehicle()))
                return;

            LiquidData liquid_status;
            ZLiquidStatus res = m.getLiquidStatus(GetPhaseShift(), x, y, z, MapConst.MapAllLiquidTypes, out liquid_status);
            if (res == 0)
            {
                if (_lastLiquid != null && _lastLiquid.SpellID != 0)
                    RemoveAurasDueToSpell(_lastLiquid.SpellID);

                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NotUnderwater);
                _lastLiquid = null;
                return;
            }
            uint liqEntry = liquid_status.entry;
            if (liqEntry != 0)
            {
                LiquidTypeRecord liquid = CliDB.LiquidTypeStorage.LookupByKey(liqEntry);
                if (_lastLiquid != null && _lastLiquid.SpellID != 0 && _lastLiquid.Id != liqEntry)
                    RemoveAurasDueToSpell(_lastLiquid.SpellID);

                if (liquid != null && liquid.SpellID != 0)
                {
                    if (res.HasAnyFlag(ZLiquidStatus.UnderWater | ZLiquidStatus.InWater))
                    {
                        if (!HasAura(liquid.SpellID))
                            CastSpell(this, liquid.SpellID, true);
                    }
                    else
                        RemoveAurasDueToSpell(liquid.SpellID);
                }

                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NotAbovewater);
                _lastLiquid = liquid;
            }
            else if (_lastLiquid != null && _lastLiquid.SpellID != 0)
            {
                RemoveAurasDueToSpell(_lastLiquid.SpellID);
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NotUnderwater);
                _lastLiquid = null;
            }
        }

        public bool SetWalk(bool enable)
        {
            if (enable == IsWalking())
                return false;

            if (enable)
                AddUnitMovementFlag(MovementFlag.Walking);
            else
                RemoveUnitMovementFlag(MovementFlag.Walking);

            MoveSplineSetFlag packet = new MoveSplineSetFlag(enable ? ServerOpcodes.MoveSplineSetWalkMode : ServerOpcodes.MoveSplineSetRunMode);
            packet.MoverGUID = GetGUID();
            SendMessageToSet(packet, true);
            return true;
        }

        public bool SetFall(bool enable)
        {
            if (enable == HasUnitMovementFlag(MovementFlag.Falling))
                return false;

            if (enable)
            {
                AddUnitMovementFlag(MovementFlag.Falling);
                m_movementInfo.SetFallTime(0);
            }
            else
                RemoveUnitMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar);

            return true;
        }

        public bool SetSwim(bool enable)
        {
            if (enable == HasUnitMovementFlag(MovementFlag.Swimming))
                return false;

            if (enable)
                AddUnitMovementFlag(MovementFlag.Swimming);
            else
                RemoveUnitMovementFlag(MovementFlag.Swimming);

            MoveSplineSetFlag packet = new MoveSplineSetFlag(enable ? ServerOpcodes.MoveSplineStartSwim : ServerOpcodes.MoveSplineStopSwim);
            packet.MoverGUID = GetGUID();
            SendMessageToSet(packet, true);

            return true;
        }

        public bool SetCanFly(bool enable)
        {
            if (enable == HasUnitMovementFlag(MovementFlag.CanFly))
                return false;

            if (enable)
            {
                AddUnitMovementFlag(MovementFlag.CanFly);
                RemoveUnitMovementFlag(MovementFlag.Swimming | MovementFlag.SplineElevation);
            }
            else
                RemoveUnitMovementFlag(MovementFlag.CanFly | MovementFlag.MaskMovingFly);

            if (!enable && IsTypeId(TypeId.Player))
                ToPlayer().SetFallInformation(0, GetPositionZ());

            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(enable ? ServerOpcodes.MoveSetCanFly : ServerOpcodes.MoveUnsetCanFly);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }
            else
            {
                MoveSplineSetFlag packet = new MoveSplineSetFlag(enable ? ServerOpcodes.MoveSplineSetFlying : ServerOpcodes.MoveSplineUnsetFlying);
                packet.MoverGUID = GetGUID();
                SendMessageToSet(packet, true);
            }

            return true;
        }

        public bool SetWaterWalking(bool enable)
        {
            if (enable == HasUnitMovementFlag(MovementFlag.WaterWalk))
                return false;

            if (enable)
                AddUnitMovementFlag(MovementFlag.WaterWalk);
            else
                RemoveUnitMovementFlag(MovementFlag.WaterWalk);


            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(enable ? ServerOpcodes.MoveSetWaterWalk : ServerOpcodes.MoveSetLandWalk);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }
            else
            {
                MoveSplineSetFlag packet = new MoveSplineSetFlag(enable ? ServerOpcodes.MoveSplineSetWaterWalk : ServerOpcodes.MoveSplineSetLandWalk);
                packet.MoverGUID = GetGUID();
                SendMessageToSet(packet, true);
            }
            return true;
        }

        public bool SetFeatherFall(bool enable)
        {
            if (enable == HasUnitMovementFlag(MovementFlag.FallingSlow))
                return false;

            if (enable)
                AddUnitMovementFlag(MovementFlag.FallingSlow);
            else
                RemoveUnitMovementFlag(MovementFlag.FallingSlow);


            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(enable ? ServerOpcodes.MoveSetFeatherFall : ServerOpcodes.MoveSetNormalFall);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }
            else
            {
                MoveSplineSetFlag packet = new MoveSplineSetFlag(enable ? ServerOpcodes.MoveSplineSetFeatherFall : ServerOpcodes.MoveSplineSetNormalFall);
                packet.MoverGUID = GetGUID();
                SendMessageToSet(packet, true);
            }
            return true;
        }

        public bool SetHover(bool enable)
        {
            if (enable == HasUnitMovementFlag(MovementFlag.Hover))
                return false;

            float hoverHeight = GetFloatValue(UnitFields.HoverHeight);

            if (enable)
            {
                //! No need to check height on ascent
                AddUnitMovementFlag(MovementFlag.Hover);
                if (hoverHeight != 0)
                    UpdateHeight(GetPositionZ() + hoverHeight);
            }
            else
            {
                RemoveUnitMovementFlag(MovementFlag.Hover);
                if (hoverHeight != 0)
                {
                    float newZ = GetPositionZ() - hoverHeight;
                    UpdateAllowedPositionZ(GetPositionX(), GetPositionY(), ref newZ);
                    UpdateHeight(newZ);
                }
            }

            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(enable ? ServerOpcodes.MoveSetHovering : ServerOpcodes.MoveUnsetHovering);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }
            else
            {
                MoveSplineSetFlag packet = new MoveSplineSetFlag(enable ? ServerOpcodes.MoveSplineSetHover : ServerOpcodes.MoveSplineUnsetHover);
                packet.MoverGUID = GetGUID();
                SendMessageToSet(packet, true);
            }
            return true;
        }

        public bool IsWithinCombatRange(Unit obj, float dist2compare)
        {
            if (!obj || !IsInMap(obj) || !IsInPhase(obj))
                return false;

            float dx = GetPositionX() - obj.GetPositionX();
            float dy = GetPositionY() - obj.GetPositionY();
            float dz = GetPositionZ() - obj.GetPositionZ();
            float distsq = dx * dx + dy * dy + dz * dz;

            float sizefactor = GetCombatReach() + obj.GetCombatReach();
            float maxdist = dist2compare + sizefactor;

            return distsq < maxdist * maxdist;
        }

        public bool isInFrontInMap(Unit target, float distance, float arc = MathFunctions.PI)
        {
            return IsWithinDistInMap(target, distance) && HasInArc(arc, target);
        }

        public bool isInBackInMap(Unit target, float distance, float arc = MathFunctions.PI)
        {
            return IsWithinDistInMap(target, distance) && !HasInArc(MathFunctions.TwoPi - arc, target);
        }
        public bool isInAccessiblePlaceFor(Creature c)
        {
            if (IsInWater())
                return c.CanSwim();
            else
                return c.CanWalk() || c.CanFly();
        }

        public void NearTeleportTo(float x, float y, float z, float orientation, bool casting = false) { NearTeleportTo(new Position(x, y, z, orientation), casting); }
        public void NearTeleportTo(Position pos, bool casting = false)
        {
            DisableSpline();
            if (IsTypeId(TypeId.Player))
            {
                WorldLocation target = new WorldLocation(GetMapId(), pos);
                ToPlayer().TeleportTo(target, (TeleportToOptions.NotLeaveTransport | TeleportToOptions.NotLeaveCombat | TeleportToOptions.NotUnSummonPet | (casting ? TeleportToOptions.Spell : 0)));
            }                
            else
            {
                SendTeleportPacket(pos);
                UpdatePosition(pos, true);
                UpdateObjectVisibility();
            }
        }

        public void SetControlled(bool apply, UnitState state)
        {
            if (apply)
            {
                if (HasUnitState(state))
                    return;

                AddUnitState(state);
                switch (state)
                {
                    case UnitState.Stunned:
                        SetStunned(true);
                        CastStop();
                        break;
                    case UnitState.Root:
                        if (!HasUnitState(UnitState.Stunned))
                            SetRooted(true);
                        break;
                    case UnitState.Confused:
                        if (!HasUnitState(UnitState.Stunned))
                        {
                            ClearUnitState(UnitState.MeleeAttacking);
                            SendMeleeAttackStop();
                            // SendAutoRepeatCancel ?
                            SetConfused(true);
                            CastStop();
                        }
                        break;
                    case UnitState.Fleeing:
                        if (!HasUnitState(UnitState.Stunned | UnitState.Confused))
                        {
                            ClearUnitState(UnitState.MeleeAttacking);
                            SendMeleeAttackStop();
                            // SendAutoRepeatCancel ?
                            SetFeared(true);
                            CastStop();
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (state)
                {
                    case UnitState.Stunned:
                        if (HasAuraType(AuraType.ModStun))
                            return;

                        SetStunned(false);
                        break;
                    case UnitState.Root:
                        if (HasAuraType(AuraType.ModRoot) || HasAuraType(AuraType.ModRoot2) || GetVehicle() != null)
                            return;

                        if (!HasUnitState(UnitState.Stunned))
                            SetRooted(false);
                        break;
                    case UnitState.Confused:
                        if (HasAuraType(AuraType.ModConfuse))
                            return;

                        SetConfused(false);
                        break;
                    case UnitState.Fleeing:
                        if (HasAuraType(AuraType.ModFear))
                            return;

                        SetFeared(false);
                        break;
                    default:
                        return;
                }

                ClearUnitState(state);

                // Unit States might have been already cleared but auras still present. I need to check with HasAuraType
                if (HasAuraType(AuraType.ModStun))
                    SetStunned(true);
                else
                {
                    if (HasAuraType(AuraType.ModRoot) || HasAuraType(AuraType.ModRoot2))
                        SetRooted(true);

                    if (HasAuraType(AuraType.ModConfuse))
                        SetConfused(true);
                    else if (HasAuraType(AuraType.ModFear))
                        SetFeared(true);
                }
            }
        }

        void SetStunned(bool apply)
        {
            if (apply)
            {
                SetTarget(ObjectGuid.Empty);
                SetFlag(UnitFields.Flags, UnitFlags.Stunned);

                StopMoving();

                if (IsTypeId(TypeId.Player))
                    SetStandState(UnitStandStateType.Stand);

                SetRooted(true);

                CastStop();
            }
            else
            {
                if (IsAlive() && GetVictim() != null)
                    SetTarget(GetVictim().GetGUID());

                // don't remove UNIT_FLAG_STUNNED for pet when owner is mounted (disabled pet's interface)
                Unit owner = GetCharmerOrOwner();
                if (owner == null || !owner.IsTypeId(TypeId.Player) || !owner.ToPlayer().IsMounted())
                    RemoveFlag(UnitFields.Flags, UnitFlags.Stunned);

                if (!HasUnitState(UnitState.Root))         // prevent moving if it also has root effect
                    SetRooted(false);
            }
        }

        public void SetRooted(bool apply, bool packetOnly = false)
        {
            if (!packetOnly)
            {
                if (apply)
                {
                    // MOVEMENTFLAG_ROOT cannot be used in conjunction with MOVEMENTFLAG_MASK_MOVING (tested 3.3.5a)
                    // this will freeze clients. That's why we remove MOVEMENTFLAG_MASK_MOVING before
                    // setting MOVEMENTFLAG_ROOT
                    RemoveUnitMovementFlag(MovementFlag.MaskMoving);
                    AddUnitMovementFlag(MovementFlag.Root);
                    StopMoving();
                }
                else
                    RemoveUnitMovementFlag(MovementFlag.Root);
            }

            Player playerMover = GetPlayerBeingMoved();// unit controlled by a player.
            if (playerMover)
            {
                MoveSetFlag packet = new MoveSetFlag(apply ? ServerOpcodes.MoveRoot : ServerOpcodes.MoveUnroot);
                packet.MoverGUID = GetGUID();
                packet.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(packet);

                MoveUpdate moveUpdate = new MoveUpdate();
                moveUpdate.Status = m_movementInfo;
                SendMessageToSet(moveUpdate, playerMover);
            }
            else
            {
                MoveSplineSetFlag packet = new MoveSplineSetFlag(apply ? ServerOpcodes.MoveSplineRoot : ServerOpcodes.MoveSplineUnroot);
                packet.MoverGUID = GetGUID();
                SendMessageToSet(packet, true);
            }
        }

        void SetFeared(bool apply)
        {
            if (apply)
            {
                SetTarget(ObjectGuid.Empty);

                Unit caster = null;
                var fearAuras = GetAuraEffectsByType(AuraType.ModFear);
                if (!fearAuras.Empty())
                    caster = Global.ObjAccessor.GetUnit(this, fearAuras[0].GetCasterGUID());
                if (caster == null)
                    caster = getAttackerForHelper();
                GetMotionMaster().MoveFleeing(caster, (uint)(fearAuras.Empty() ? WorldConfig.GetIntValue(WorldCfg.CreatureFamilyFleeDelay) : 0)); // caster == NULL processed in MoveFleeing
            }
            else
            {
                if (IsAlive())
                {
                    if (GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Fleeing)
                        GetMotionMaster().MovementExpired();
                    if (GetVictim() != null)
                        SetTarget(GetVictim().GetGUID());
                }
            }

            Player player = ToPlayer();
            if (player)
                if (player.HasUnitState(UnitState.Possessed))
                    player.SetClientControl(this, !apply);
        }

        void SetConfused(bool apply)
        {
            if (apply)
            {
                SetTarget(ObjectGuid.Empty);
                GetMotionMaster().MoveConfused();
            }
            else
            {
                if (IsAlive())
                {
                    if (GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Confused)
                        GetMotionMaster().MovementExpired();
                    if (GetVictim() != null)
                        SetTarget(GetVictim().GetGUID());
                }
            }

            Player player = ToPlayer();
            if (player)
                if (player.HasUnitState(UnitState.Possessed))
                    player.SetClientControl(this, !apply);
        }

        public bool CanFreeMove()
        {
            return !HasUnitState(UnitState.Confused | UnitState.Fleeing | UnitState.InFlight |
                 UnitState.Root | UnitState.Stunned | UnitState.Distracted) && GetOwnerGUID().IsEmpty();
        }

        public void Mount(uint mount, uint VehicleId = 0, uint creatureEntry = 0)
        {
            if (mount != 0)
                SetUInt32Value(UnitFields.MountDisplayId, mount);

            SetFlag(UnitFields.Flags, UnitFlags.Mount);

            Player player = ToPlayer();
            if (player != null)
            {
                // mount as a vehicle
                if (VehicleId != 0)
                {
                    if (CreateVehicleKit(VehicleId, creatureEntry))
                    {
                        player.SendOnCancelExpectedVehicleRideAura();

                        // mounts can also have accessories
                        GetVehicleKit().InstallAllAccessories(false);
                    }
                }

                // unsummon pet
                Pet pet = player.GetPet();
                if (pet != null)
                {
                    Battleground bg = ToPlayer().GetBattleground();
                    // don't unsummon pet in arena but SetFlag UNIT_FLAG_STUNNED to disable pet's interface
                    if (bg && bg.isArena())
                        pet.SetFlag(UnitFields.Flags, UnitFlags.Stunned);
                    else
                        player.UnsummonPetTemporaryIfAny();
                }

                // if we have charmed npc, stun him also (everywhere)
                Unit charm = player.GetCharm();
                if (charm)
                    if (charm.GetTypeId() == TypeId.Unit)
                        charm.SetFlag(UnitFields.Flags, UnitFlags.Stunned);

                player.SendMovementSetCollisionHeight(player.GetCollisionHeight(true));
            }

            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Mount);
        }

        public void Dismount()
        {
            if (!IsMounted())
                return;

            SetUInt32Value(UnitFields.MountDisplayId, 0);
            RemoveFlag(UnitFields.Flags, UnitFlags.Mount);

            Player thisPlayer = ToPlayer();
            if (thisPlayer != null)
                thisPlayer.SendMovementSetCollisionHeight(thisPlayer.GetCollisionHeight(false));

            Dismount data = new Dismount();
            data.Guid = GetGUID();
            SendMessageToSet(data, true);

            // dismount as a vehicle
            if (IsTypeId(TypeId.Player) && GetVehicleKit() != null)
            {
                // Remove vehicle from player
                RemoveVehicleKit();
            }

            RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NotMounted);

            // only resummon old pet if the player is already added to a map
            // this prevents adding a pet to a not created map which would otherwise cause a crash
            // (it could probably happen when logging in after a previous crash)
            Player player = ToPlayer();
            if (player != null)
            {
                Pet pPet = player.GetPet();
                if (pPet != null)
                {
                    if (pPet.HasFlag(UnitFields.Flags, UnitFlags.Stunned) && !pPet.HasUnitState(UnitState.Stunned))
                        pPet.RemoveFlag(UnitFields.Flags, UnitFlags.Stunned);
                }
                else
                    player.ResummonPetTemporaryUnSummonedIfAny();

                // if we have charmed npc, remove stun also
                Unit charm = player.GetCharm();
                if (charm)
                    if (charm.GetTypeId() == TypeId.Unit && charm.HasFlag(UnitFields.Flags, UnitFlags.Stunned) && !charm.HasUnitState(UnitState.Stunned))
                        charm.RemoveFlag(UnitFields.Flags, UnitFlags.Stunned);
            }
        }

        public bool CreateVehicleKit(uint id, uint creatureEntry, bool loading = false)
        {
            VehicleRecord vehInfo = CliDB.VehicleStorage.LookupByKey(id);
            if (vehInfo == null)
                return false;

            m_vehicleKit = new Vehicle(this, vehInfo, creatureEntry);
            m_updateFlag.Vehicle = true;
            m_unitTypeMask |= UnitTypeMask.Vehicle;

            if (!loading)
                SendSetVehicleRecId(id);

            return true;
        }

        public void RemoveVehicleKit(bool onRemoveFromWorld = false)
        {
            if (m_vehicleKit == null)
                return;

            if (!onRemoveFromWorld)
                SendSetVehicleRecId(0);

            m_vehicleKit.Uninstall();

            m_vehicleKit = null;

            m_updateFlag.Vehicle = false;
            m_unitTypeMask &= ~UnitTypeMask.Vehicle;
            RemoveFlag64(UnitFields.NpcFlags, NPCFlags.SpellClick | NPCFlags.PlayerVehicle);
        }

        void SendSetVehicleRecId(uint vehicleId)
        {
            Player player = ToPlayer();
            if (player)
            {
                MoveSetVehicleRecID moveSetVehicleRec = new MoveSetVehicleRecID();
                moveSetVehicleRec.MoverGUID = GetGUID();
                moveSetVehicleRec.SequenceIndex = m_movementCounter++;
                moveSetVehicleRec.VehicleRecID = vehicleId;
                player.SendPacket(moveSetVehicleRec);
            }

            SetVehicleRecID setVehicleRec = new SetVehicleRecID();
            setVehicleRec.VehicleGUID = GetGUID();
            setVehicleRec.VehicleRecID = vehicleId;
            SendMessageToSet(setVehicleRec, true);
        }

        void SendSetPlayHoverAnim(bool enable)
        {
            SetPlayHoverAnim data = new SetPlayHoverAnim();
            data.UnitGUID = GetGUID();
            data.PlayHoverAnim = enable;

            SendMessageToSet(data, true);
        }

        public float GetPositionZMinusOffset()
        {
            float offset = 0.0f;
            if (HasUnitMovementFlag(MovementFlag.Hover))
                offset = GetFloatValue(UnitFields.HoverHeight);

            return GetPositionZ() - offset;
        }

        public Unit GetUnitBeingMoved()
        {
            Player player = ToPlayer();
            if (player)
                return player.m_unitMovedByMe;

            return null;
        }

        Player GetPlayerBeingMoved()
        {
            Unit mover = GetUnitBeingMoved();
            if (mover)
                return mover.ToPlayer();

            return null;
        }

        Player GetPlayerMovingMe() { return m_playerMovingMe; }

        public void AddUnitMovementFlag(MovementFlag f)
        {
            m_movementInfo.AddMovementFlag(f);
        }
        public void RemoveUnitMovementFlag(MovementFlag f)
        {
            m_movementInfo.RemoveMovementFlag(f);
        }
        public bool HasUnitMovementFlag(MovementFlag f)
        {
            return m_movementInfo.HasMovementFlag(f);
        }
        public MovementFlag GetUnitMovementFlags()
        {
            return m_movementInfo.GetMovementFlags();
        }
        public void SetUnitMovementFlags(MovementFlag f)
        {
            m_movementInfo.SetMovementFlags(f);
        }

        public void AddUnitMovementFlag2(MovementFlag2 f)
        {
            m_movementInfo.AddMovementFlag2(f);
        }
        void RemoveUnitMovementFlag2(MovementFlag2 f)
        {
            m_movementInfo.RemoveMovementFlag2(f);
        }
        public bool HasUnitMovementFlag2(MovementFlag2 f)
        {
            return m_movementInfo.HasMovementFlag2(f);
        }
        public MovementFlag2 GetUnitMovementFlags2()
        {
            return m_movementInfo.GetMovementFlags2();
        }
        public void SetUnitMovementFlags2(MovementFlag2 f)
        {
            m_movementInfo.SetMovementFlags2(f);
        }

        //Spline
        public bool IsSplineEnabled()
        {
            return moveSpline.Initialized() && !moveSpline.Finalized();
        }
        void UpdateSplineMovement(uint diff)
        {
            int positionUpdateDelay = 400;

            if (moveSpline.Finalized())
                return;

            moveSpline.updateState((int)diff);
            bool arrived = moveSpline.Finalized();

            if (arrived)
                DisableSpline();

            movesplineTimer.Update((int)diff);
            if (movesplineTimer.Passed() || arrived)
            {
                movesplineTimer.Reset(positionUpdateDelay);
                Vector4 loc = moveSpline.ComputePosition();
                float x = loc.X;
                float y = loc.Y;
                float z = loc.Z;
                float o = loc.W;

                if (moveSpline.onTransport)
                {
                    m_movementInfo.transport.pos.Relocate(x, y, z, o);

                    ITransport transport = GetDirectTransport();
                    if (transport != null)
                        transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                }
                if (HasUnitState(UnitState.CannotTurn))
                    o = GetOrientation();

                UpdatePosition(x, y, z, o);
            }
        }
        public void DisableSpline()
        {
            m_movementInfo.RemoveMovementFlag(MovementFlag.Forward);
            moveSpline.Interrupt();
        }

        //Transport
        public override ObjectGuid GetTransGUID()
        {
            if (GetVehicle() != null)
                return GetVehicleBase().GetGUID();
            if (GetTransport() != null)
                return GetTransport().GetGUID();

            return ObjectGuid.Empty;
        }

        //Teleport
        public void SendTeleportPacket(Position pos)
        {
            // SMSG_MOVE_UPDATE_TELEPORT is sent to nearby players to signal the teleport
            // SMSG_MOVE_TELEPORT is sent to self in order to trigger CMSG_MOVE_TELEPORT_ACK and update the position server side

            MoveUpdateTeleport moveUpdateTeleport = new MoveUpdateTeleport();
            moveUpdateTeleport.Status = m_movementInfo;
            Unit broadcastSource = this;

            Player playerMover = GetPlayerBeingMoved();
            if (playerMover)
            {
                float x, y, z, o;
                pos.GetPosition(out x, out y, out z, out o);

                ITransport transportBase = GetDirectTransport();
                if (transportBase != null)
                    transportBase.CalculatePassengerOffset(ref x, ref y, ref z, ref o);

                MoveTeleport moveTeleport = new MoveTeleport();
                moveTeleport.MoverGUID = GetGUID();
                moveTeleport.Pos = new Position(x, y, z, o);
                if (GetTransGUID() != ObjectGuid.Empty)
                    moveTeleport.TransportGUID.Set(GetTransGUID());
                moveTeleport.Facing = o;
                moveTeleport.SequenceIndex = m_movementCounter++;
                playerMover.SendPacket(moveTeleport);

                broadcastSource = playerMover;
            }
            else
            {
                // This is the only packet sent for creatures which contains MovementInfo structure
                // we do not update m_movementInfo for creatures so it needs to be done manually here
                moveUpdateTeleport.Status.Guid = GetGUID();
                moveUpdateTeleport.Status.Pos.Relocate(pos);
                moveUpdateTeleport.Status.Time = Time.GetMSTime();
            }

            // Broadcast the packet to everyone except self.
            broadcastSource.SendMessageToSet(moveUpdateTeleport, false);
        }
    }
}
