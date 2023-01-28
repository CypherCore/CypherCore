// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Maps;
using Game.Movement;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities
{
	public partial class Unit
	{
		public bool IsGravityDisabled()
		{
			return MovementInfo.HasMovementFlag(MovementFlag.DisableGravity);
		}

		public bool IsWalking()
		{
			return MovementInfo.HasMovementFlag(MovementFlag.Walking);
		}

		public bool IsHovering()
		{
			return MovementInfo.HasMovementFlag(MovementFlag.Hover);
		}

		public bool IsStopped()
		{
			return !HasUnitState(UnitState.Moving);
		}

		public bool IsMoving()
		{
			return MovementInfo.HasMovementFlag(MovementFlag.MaskMoving);
		}

		public bool IsTurning()
		{
			return MovementInfo.HasMovementFlag(MovementFlag.MaskTurning);
		}

		public virtual bool CanFly()
		{
			return false;
		}

		public bool IsFlying()
		{
			return MovementInfo.HasMovementFlag(MovementFlag.Flying | MovementFlag.DisableGravity);
		}

		public bool IsFalling()
		{
			return MovementInfo.HasMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar) || MoveSpline.IsFalling();
		}

		public virtual bool CanEnterWater()
		{
			return false;
		}

		public virtual bool CanSwim()
		{
			// Mirror client behavior, if this method returns false then client will not use swimming animation and for players will apply gravity as if there was no water
			if (HasUnitFlag(UnitFlags.CantSwim))
				return false;

			if (HasUnitFlag(UnitFlags.PlayerControlled)) // is player
				return true;

			if (HasUnitFlag2((UnitFlags2)0x1000000))
				return false;

			if (HasUnitFlag(UnitFlags.PetInCombat))
				return true;

			return HasUnitFlag(UnitFlags.Rename | UnitFlags.CanSwim);
		}

		public bool IsInWater()
		{
			return GetLiquidStatus().HasAnyFlag(ZLiquidStatus.InWater | ZLiquidStatus.UnderWater);
		}

		public bool IsUnderWater()
		{
			return GetLiquidStatus().HasFlag(ZLiquidStatus.UnderWater);
		}

		private void PropagateSpeedChange()
		{
			GetMotionMaster().PropagateSpeedChange();
		}

		public float GetSpeed(UnitMoveType mtype)
		{
			return SpeedRate[(int)mtype] * (IsControlledByPlayer() ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]);
		}

		public void SetSpeed(UnitMoveType mtype, float newValue)
		{
			SetSpeedRate(mtype, newValue / (IsControlledByPlayer() ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]));
		}

		public void SetSpeedRate(UnitMoveType mtype, float rate)
		{
			rate = Math.Max(rate, 0.01f);

			if (SpeedRate[(int)mtype] == rate)
				return;

			SpeedRate[(int)mtype] = rate;

			PropagateSpeedChange();

			// Spline packets are for creatures and move_update are for players
			ServerOpcodes[,] moveTypeToOpcode = new ServerOpcodes[(int)UnitMoveType.Max, 3]
			                                    {
				                                    {
					                                    ServerOpcodes.MoveSplineSetWalkSpeed, ServerOpcodes.MoveSetWalkSpeed, ServerOpcodes.MoveUpdateWalkSpeed
				                                    },
				                                    {
					                                    ServerOpcodes.MoveSplineSetRunSpeed, ServerOpcodes.MoveSetRunSpeed, ServerOpcodes.MoveUpdateRunSpeed
				                                    },
				                                    {
					                                    ServerOpcodes.MoveSplineSetRunBackSpeed, ServerOpcodes.MoveSetRunBackSpeed, ServerOpcodes.MoveUpdateRunBackSpeed
				                                    },
				                                    {
					                                    ServerOpcodes.MoveSplineSetSwimSpeed, ServerOpcodes.MoveSetSwimSpeed, ServerOpcodes.MoveUpdateSwimSpeed
				                                    },
				                                    {
					                                    ServerOpcodes.MoveSplineSetSwimBackSpeed, ServerOpcodes.MoveSetSwimBackSpeed, ServerOpcodes.MoveUpdateSwimBackSpeed
				                                    },
				                                    {
					                                    ServerOpcodes.MoveSplineSetTurnRate, ServerOpcodes.MoveSetTurnRate, ServerOpcodes.MoveUpdateTurnRate
				                                    },
				                                    {
					                                    ServerOpcodes.MoveSplineSetFlightSpeed, ServerOpcodes.MoveSetFlightSpeed, ServerOpcodes.MoveUpdateFlightSpeed
				                                    },
				                                    {
					                                    ServerOpcodes.MoveSplineSetFlightBackSpeed, ServerOpcodes.MoveSetFlightBackSpeed, ServerOpcodes.MoveUpdateFlightBackSpeed
				                                    },
				                                    {
					                                    ServerOpcodes.MoveSplineSetPitchRate, ServerOpcodes.MoveSetPitchRate, ServerOpcodes.MoveUpdatePitchRate
				                                    }
			                                    };

			if (IsTypeId(TypeId.Player))
			{
				// register forced speed changes for WorldSession.HandleForceSpeedChangeAck
				// and do it only for real sent packets and use run for run/mounted as client expected
				++ToPlayer().ForcedSpeedChanges[(int)mtype];

				if (!IsInCombat())
				{
					Pet pet = ToPlayer().GetPet();

					if (pet)
						pet.SetSpeedRate(mtype, SpeedRate[(int)mtype]);
				}
			}

			Player playerMover = GetUnitBeingMoved()?.ToPlayer(); // unit controlled by a player.

			if (playerMover)
			{
				// Send notification to self
				MoveSetSpeed selfpacket = new(moveTypeToOpcode[(int)mtype, 1]);
				selfpacket.MoverGUID     = GetGUID();
				selfpacket.SequenceIndex = MovementCounter++;
				selfpacket.Speed         = GetSpeed(mtype);
				playerMover.SendPacket(selfpacket);

				// Send notification to other players
				MoveUpdateSpeed packet = new(moveTypeToOpcode[(int)mtype, 2]);
				packet.Status = MovementInfo;
				packet.Speed  = GetSpeed(mtype);
				playerMover.SendMessageToSet(packet, false);
			}
			else
			{
				MoveSplineSetSpeed packet = new(moveTypeToOpcode[(int)mtype, 0]);
				packet.MoverGUID = GetGUID();
				packet.Speed     = GetSpeed(mtype);
				SendMessageToSet(packet, true);
			}
		}

		public float GetSpeedRate(UnitMoveType mtype)
		{
			return SpeedRate[(int)mtype];
		}

		public virtual MovementGeneratorType GetDefaultMovementType()
		{
			return MovementGeneratorType.Idle;
		}

		public void StopMoving()
		{
			ClearUnitState(UnitState.Moving);

			// not need send any packets if not in world or not moving
			if (!IsInWorld ||
			    MoveSpline.Finalized())
				return;

			// Update position now since Stop does not start a new movement that can be updated later
			if (MoveSpline.HasStarted())
				UpdateSplinePosition();

			MoveSplineInit init = new(this);
			init.Stop();
		}

		public void PauseMovement(uint timer = 0, MovementSlot slot = 0, bool forced = true)
		{
			if (MotionMaster.IsInvalidMovementSlot(slot))
				return;

			MovementGenerator movementGenerator = GetMotionMaster().GetCurrentMovementGenerator(slot);

			if (movementGenerator != null)
				movementGenerator.Pause(timer);

			if (forced && GetMotionMaster().GetCurrentSlot() == slot)
				StopMoving();
		}

		public void ResumeMovement(uint timer = 0, MovementSlot slot = 0)
		{
			if (MotionMaster.IsInvalidMovementSlot(slot))
				return;

			MovementGenerator movementGenerator = GetMotionMaster().GetCurrentMovementGenerator(slot);

			if (movementGenerator != null)
				movementGenerator.Resume(timer);
		}

		public void SetInFront(WorldObject target)
		{
			if (!HasUnitState(UnitState.CannotTurn))
				Orientation = GetAbsoluteAngle(target.GetPosition());
		}

		public void SetFacingTo(float ori, bool force = true)
		{
			// do not face when already moving
			if (!force &&
			    (!IsStopped() || !MoveSpline.Finalized()))
				return;

			MoveSplineInit init = new(this);
			init.MoveTo(GetPositionX(), GetPositionY(), GetPositionZ(), false);

			if (GetTransport() != null)
				init.DisableTransportPathTransformations(); // It makes no sense to Target global orientation

			init.SetFacing(ori);

			//GetMotionMaster().LaunchMoveSpline(init, EventId.Face, MovementGeneratorPriority.Highest);
			init.Launch();
		}

		public void SetFacingToObject(WorldObject obj, bool force = true)
		{
			// do not face when already moving
			if (!force &&
			    (!IsStopped() || !MoveSpline.Finalized()))
				return;

			// @todo figure out under what conditions creature will move towards object instead of facing it where it currently is.
			MoveSplineInit init = new(this);
			init.MoveTo(GetPositionX(), GetPositionY(), GetPositionZ(), false);
			init.SetFacing(GetAbsoluteAngle(obj)); // when on Transport, GetAbsoluteAngle will still return global coordinates (and angle) that needs transforming

			//GetMotionMaster().LaunchMoveSpline(init, EventId.Face, MovementGeneratorPriority.Highest);
			init.Launch();
		}

		public void MonsterMoveWithSpeed(float x, float y, float z, float speed, bool generatePath = false, bool forceDestination = false)
		{
			var initializer = (MoveSplineInit init) =>
			                  {
				                  init.MoveTo(x, y, z, generatePath, forceDestination);
				                  init.SetVelocity(speed);
			                  };

			GetMotionMaster().LaunchMoveSpline(initializer, 0, MovementGeneratorPriority.Normal, MovementGeneratorType.Point);
		}

		public void KnockbackFrom(Position origin, float speedXY, float speedZ, SpellEffectExtraData spellEffectExtraData = null)
		{
			Player player = ToPlayer();

			if (!player)
			{
				Unit charmer = GetCharmer();

				if (charmer)
				{
					player = charmer.ToPlayer();

					if (player && player.GetUnitBeingMoved() != this)
						player = null;
				}
			}

			if (!player)
			{
				GetMotionMaster().MoveKnockbackFrom(origin, speedXY, speedZ, spellEffectExtraData);
			}
			else
			{
				float o = GetPosition() == origin ? GetOrientation() + MathF.PI : origin.GetRelativeAngle(this);

				if (speedXY < 0)
				{
					speedXY = -speedXY;
					o       = o - MathF.PI;
				}

				float vcos = MathF.Cos(o);
				float vsin = MathF.Sin(o);
				SendMoveKnockBack(player, speedXY, -speedZ, vcos, vsin);
			}
		}

		private void SendMoveKnockBack(Player player, float speedXY, float speedZ, float vcos, float vsin)
		{
			MoveKnockBack moveKnockBack = new();
			moveKnockBack.MoverGUID        = GetGUID();
			moveKnockBack.SequenceIndex    = MovementCounter++;
			moveKnockBack.Speeds.HorzSpeed = speedXY;
			moveKnockBack.Speeds.VertSpeed = speedZ;
			moveKnockBack.Direction        = new Vector2(vcos, vsin);
			player.SendPacket(moveKnockBack);
		}

		private bool SetCollision(bool disable)
		{
			if (disable == HasUnitMovementFlag(MovementFlag.DisableCollision))
				return false;

			if (disable)
				AddUnitMovementFlag(MovementFlag.DisableCollision);
			else
				RemoveUnitMovementFlag(MovementFlag.DisableCollision);

			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(disable ? ServerOpcodes.MoveSplineEnableCollision : ServerOpcodes.MoveEnableCollision);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}
			else
			{
				MoveSplineSetFlag packet = new(disable ? ServerOpcodes.MoveSplineDisableCollision : ServerOpcodes.MoveDisableCollision);
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

			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(enable ? ServerOpcodes.MoveEnableTransitionBetweenSwimAndFly : ServerOpcodes.MoveDisableTransitionBetweenSwimAndFly);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}

			return true;
		}

		public bool SetCanTurnWhileFalling(bool enable)
		{
			// Temporarily disabled for short lived Auras that unapply before client had Time to ACK applying
			//if (enable == HasUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling))
			//return false;

			if (enable)
				AddUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling);
			else
				RemoveUnitMovementFlag2(MovementFlag2.CanTurnWhileFalling);

			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetCanTurnWhileFalling : ServerOpcodes.MoveUnsetCanTurnWhileFalling);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
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

			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(enable ? ServerOpcodes.MoveEnableDoubleJump : ServerOpcodes.MoveDisableDoubleJump);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}

			return true;
		}

		public bool SetDisableInertia(bool disable)
		{
			if (disable == HasExtraUnitMovementFlag2(MovementFlags3.DisableInertia))
				return false;

			if (disable)
				AddExtraUnitMovementFlag2(MovementFlags3.DisableInertia);
			else
				RemoveExtraUnitMovementFlag2(MovementFlags3.DisableInertia);

			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover != null)
			{
				MoveSetFlag packet = new(disable ? ServerOpcodes.MoveDisableInertia : ServerOpcodes.MoveEnableInertia);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}

			return true;
		}

		public void JumpTo(float speedXY, float speedZ, float angle, Position dest = null)
		{
			if (dest != null)
				angle += GetRelativeAngle(dest);

			if (IsTypeId(TypeId.Unit))
			{
				GetMotionMaster().MoveJumpTo(angle, speedXY, speedZ);
			}
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
			GetMotionMaster().MoveJump(x, y, z, GetAbsoluteAngle(obj), speedXY, speedZ, EventId.Jump, withOrientation);
		}

		public void UpdateSpeed(UnitMoveType mtype)
		{
			int   main_speed_mod  = 0;
			float stack_bonus     = 1.0f;
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
					if (IsMounted()) // Use on Mount Auras
					{
						main_speed_mod  =  GetMaxPositiveAuraModifier(AuraType.ModIncreaseMountedSpeed);
						stack_bonus     =  GetTotalAuraMultiplier(AuraType.ModMountedSpeedAlways);
						non_stack_bonus += GetMaxPositiveAuraModifier(AuraType.ModMountedSpeedNotStack) / 100.0f;
					}
					else
					{
						main_speed_mod  =  GetMaxPositiveAuraModifier(AuraType.ModIncreaseSpeed);
						stack_bonus     =  GetTotalAuraMultiplier(AuraType.ModSpeedAlways);
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
					if (IsTypeId(TypeId.Unit) &&
					    IsControlledByPlayer()) // not sure if good for pet
					{
						main_speed_mod = GetMaxPositiveAuraModifier(AuraType.ModIncreaseVehicleFlightSpeed);
						stack_bonus    = GetTotalAuraMultiplier(AuraType.ModVehicleSpeedAlways);

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
						stack_bonus    = GetTotalAuraMultiplier(AuraType.ModMountedFlightSpeedAlways);
					}
					else // Use not Mount (shapeshift for example) Auras (should stack)
					{
						main_speed_mod = GetTotalAuraModifier(AuraType.ModIncreaseFlightSpeed) + GetTotalAuraModifier(AuraType.ModIncreaseVehicleFlightSpeed);
					}

					non_stack_bonus += GetMaxPositiveAuraModifier(AuraType.ModFlightSpeedNotStack) / 100.0f;

					// Update speed for vehicle if available
					if (IsTypeId(TypeId.Player) &&
					    GetVehicle() != null)
						GetVehicleBase().UpdateSpeed(UnitMoveType.Flight);

					break;
				}
				default:
					Log.outError(LogFilter.Unit, "Unit.UpdateSpeed: Unsupported move Type ({0})", mtype);

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
						speed *= ToCreature().GetCreatureTemplate().SpeedRun; // at this point, MOVE_WALK is never reached

					// Normalize speed by 191 aura SPELL_AURA_USE_NORMAL_MOVEMENT_SPEED if need
					// @todo possible affect only on MOVE_RUN
					int normalization = GetMaxPositiveAuraModifier(AuraType.UseNormalMovementSpeed);

					if (normalization != 0)
					{
						Creature creature1 = ToCreature();

						if (creature1)
						{
							ulong immuneMask = creature1.GetCreatureTemplate().MechanicImmuneMask;

							if (Convert.ToBoolean(immuneMask & (1 << ((int)Mechanics.Snare - 1))) ||
							    Convert.ToBoolean(immuneMask & (1 << ((int)Mechanics.Daze - 1))))
								break;
						}

						// Use speed from aura
						float max_speed = normalization / (IsControlledByPlayer() ? SharedConst.playerBaseMoveSpeed[(int)mtype] : SharedConst.baseMoveSpeed[(int)mtype]);

						if (speed > max_speed)
							speed = max_speed;
					}

					if (mtype == UnitMoveType.Run)
					{
						// Force minimum speed rate @ aura 437 SPELL_AURA_MOD_MINIMUM_SPEED_RATE
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

			Creature creature = ToCreature();

			if (creature != null)
				if (creature.HasUnitTypeMask(UnitTypeMask.Minion) &&
				    !creature.IsInCombat())
					if (GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Follow)
					{
						Unit followed = (GetMotionMaster().GetCurrentMovementGenerator() as FollowMovementGenerator).GetTarget();

						if (followed != null &&
						    followed.GetGUID() == GetOwnerGUID() &&
						    !followed.IsInCombat())
						{
							float ownerSpeed = followed.GetSpeedRate(mtype);

							if (speed < ownerSpeed ||
							    creature.IsWithinDist3d(followed, 10.0f))
								speed = ownerSpeed;

							speed *= Math.Min(Math.Max(1.0f, 0.75f + (GetDistance(followed) - SharedConst.PetFollowDist) * 0.05f), 1.3f);
						}
					}

			// Apply strongest slow aura mod to speed
			int slow = GetMaxNegativeAuraModifier(AuraType.ModDecreaseSpeed);

			if (slow != 0)
				MathFunctions.AddPct(ref speed, slow);

			float minSpeedMod = GetMaxPositiveAuraModifier(AuraType.ModMinimumSpeed);

			if (minSpeedMod != 0)
			{
				float baseMinSpeed = 1.0f;

				if (!GetOwnerGUID().IsPlayer() &&
				    !IsHunterPet() &&
				    GetTypeId() == TypeId.Unit)
					baseMinSpeed = ToCreature().GetCreatureTemplate().SpeedRun;

				float min_speed = MathFunctions.CalculatePct(baseMinSpeed, minSpeedMod);

				if (speed < min_speed)
					speed = min_speed;
			}

			SetSpeedRate(mtype, speed);
		}

		public virtual bool UpdatePosition(Position obj, bool teleport = false)
		{
			return UpdatePosition(obj.X, obj.Y, obj.Z, obj.Orientation, teleport);
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

			if (relocated)
			{
				// move and update visible State if need
				if (IsTypeId(TypeId.Player))
					GetMap().PlayerRelocation(ToPlayer(), x, y, z, orientation);
				else
					GetMap().CreatureRelocation(ToCreature(), x, y, z, orientation);
			}
			else if (turn)
			{
				UpdateOrientation(orientation);
			}

			_positionUpdateInfo.Relocated = relocated;
			_positionUpdateInfo.Turned    = turn;

			bool isInWater = IsInWater();

			if (!IsFalling() ||
			    isInWater ||
			    IsFlying())
				RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.Ground);

			if (isInWater)
				RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.Swimming);

			return (relocated || turn);
		}

		private void UpdateOrientation(float orientation)
		{
			SetOrientation(orientation);

			if (IsVehicle())
				GetVehicleKit().RelocatePassengers();
		}

		//! Only server-side height update, does not broadcast to client
		private void UpdateHeight(float newZ)
		{
			Relocate(GetPositionX(), GetPositionY(), newZ);

			if (IsVehicle())
				GetVehicleKit().RelocatePassengers();
		}

		public float GetHoverOffset()
		{
			return HasUnitMovementFlag(MovementFlag.Hover) ? UnitData.HoverHeight : 0.0f;
		}

		public bool IsWithinBoundaryRadius(Unit obj)
		{
			if (!obj ||
			    !IsInMap(obj) ||
			    !InSamePhase(obj))
				return false;

			float objBoundaryRadius = Math.Max(obj.GetBoundingRadius(), SharedConst.MinMeleeReach);

			return IsInDist(obj, objBoundaryRadius);
		}

		public bool SetDisableGravity(bool disable, bool updateAnimTier = true)
		{
			if (disable == IsGravityDisabled())
				return false;

			if (disable)
			{
				AddUnitMovementFlag(MovementFlag.DisableGravity);
				RemoveUnitMovementFlag(MovementFlag.Swimming | MovementFlag.SplineElevation);
			}
			else
			{
				RemoveUnitMovementFlag(MovementFlag.DisableGravity);
			}


			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(disable ? ServerOpcodes.MoveDisableGravity : ServerOpcodes.MoveEnableGravity);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}
			else
			{
				MoveSplineSetFlag packet = new(disable ? ServerOpcodes.MoveSplineDisableGravity : ServerOpcodes.MoveSplineEnableGravity);
				packet.MoverGUID = GetGUID();
				SendMessageToSet(packet, true);
			}

			if (IsCreature() &&
			    updateAnimTier &&
			    IsAlive() &&
			    !HasUnitState(UnitState.Root) &&
			    !ToCreature().GetMovementTemplate().IsRooted())
			{
				if (IsGravityDisabled())
					SetAnimTier(AnimTier.Fly);
				else if (IsHovering())
					SetAnimTier(AnimTier.Hover);
				else
					SetAnimTier(AnimTier.Ground);
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

			uint           areaId      = GetAreaId();
			uint           ridingSkill = 5000;
			AreaMountFlags mountFlags  = 0;
			bool           isSubmerged;
			bool           isInWater;

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

			ZLiquidStatus liquidStatus = GetMap().GetLiquidStatus(GetPhaseShift(), GetPositionX(), GetPositionY(), GetPositionZ(), LiquidHeaderTypeFlags.AllLiquids, null);
			isSubmerged = liquidStatus.HasAnyFlag(ZLiquidStatus.UnderWater) || HasUnitMovementFlag(MovementFlag.Swimming);
			isInWater   = liquidStatus.HasAnyFlag(ZLiquidStatus.InWater | ZLiquidStatus.UnderWater);

			foreach (var mountTypeXCapability in capabilities)
			{
				MountCapabilityRecord mountCapability = CliDB.MountCapabilityStorage.LookupByKey(mountTypeXCapability.MountCapabilityID);

				if (mountCapability == null)
					continue;

				if (ridingSkill < mountCapability.ReqRidingSkill)
					continue;

				if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.IgnoreRestrictions))
				{
					if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Ground) &&
					    !mountFlags.HasAnyFlag(AreaMountFlags.GroundAllowed))
						continue;

					if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Flying) &&
					    !mountFlags.HasAnyFlag(AreaMountFlags.FlyingAllowed))
						continue;

					if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Float) &&
					    !mountFlags.HasAnyFlag(AreaMountFlags.FloatAllowed))
						continue;

					if (mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Underwater) &&
					    !mountFlags.HasAnyFlag(AreaMountFlags.UnderwaterAllowed))
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
					// player is on water surface
					else if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Float))
					{
						continue;
					}
				}
				else if (isInWater)
				{
					if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Underwater))
						continue;
				}
				else if (!mountCapability.Flags.HasAnyFlag(MountCapabilityFlags.Float))
				{
					continue;
				}

				if (mountCapability.ReqMapID != -1 &&
				    GetMapId() != mountCapability.ReqMapID &&
				    GetMap().GetEntry().CosmeticParentMapID != mountCapability.ReqMapID &&
				    GetMap().GetEntry().ParentMapID != mountCapability.ReqMapID)
					continue;

				if (mountCapability.ReqAreaID != 0 &&
				    !Global.DB2Mgr.IsInArea(areaId, mountCapability.ReqAreaID))
					continue;

				if (mountCapability.ReqSpellAuraID != 0 &&
				    !HasAura(mountCapability.ReqSpellAuraID))
					continue;

				if (mountCapability.ReqSpellKnownID != 0 &&
				    !HasSpell(mountCapability.ReqSpellKnownID))
					continue;

				Player thisPlayer = ToPlayer();

				if (thisPlayer != null)
				{
					PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mountCapability.PlayerConditionID);

					if (playerCondition != null)
						if (!ConditionManager.IsPlayerMeetingCondition(thisPlayer, playerCondition))
							continue;
				}

				return mountCapability;
			}

			return null;
		}

		public void UpdateMountCapability()
		{
			var mounts = GetAuraEffectsByType(AuraType.Mounted);

			foreach (AuraEffect aurEff in mounts.ToArray())
			{
				aurEff.RecalculateAmount();

				if (aurEff.GetAmount() == 0)
				{
					aurEff.GetBase().Remove();
				}
				else
				{
					var capability = CliDB.MountCapabilityStorage.LookupByKey(aurEff.GetAmount());

					if (capability != null) // aura may get removed by interrupt flag, reapply
						if (!HasAura(capability.ModSpellAuraID))
							CastSpell(this, capability.ModSpellAuraID, new CastSpellExtraArgs(aurEff));
				}
			}
		}

		public override void ProcessPositionDataChanged(PositionFullTerrainStatus data)
		{
			ZLiquidStatus oldLiquidStatus = GetLiquidStatus();
			base.ProcessPositionDataChanged(data);
			ProcessTerrainStatusUpdate(oldLiquidStatus, data.LiquidInfo);
		}

		public virtual void ProcessTerrainStatusUpdate(ZLiquidStatus oldLiquidStatus, LiquidData newLiquidData)
		{
			if (!IsControlledByPlayer())
				return;

			// remove appropriate Auras if we are swimming/not swimming respectively
			if (IsInWater())
				RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.UnderWater);
			else
				RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.AboveWater);

			// liquid aura handling
			LiquidTypeRecord curLiquid = null;

			if (IsInWater() &&
			    newLiquidData != null)
				curLiquid = CliDB.LiquidTypeStorage.LookupByKey(newLiquidData.entry);

			if (curLiquid != _lastLiquid)
			{
				if (_lastLiquid != null &&
				    _lastLiquid.SpellID != 0)
					RemoveAurasDueToSpell(_lastLiquid.SpellID);

				Player player = GetCharmerOrOwnerPlayerOrPlayerItself();

				// Set _lastLiquid before casting liquid spell to avoid infinite loops
				_lastLiquid = curLiquid;

				if (curLiquid != null &&
				    curLiquid.SpellID != 0 &&
				    (!player || !player.IsGameMaster()))
					CastSpell(this, curLiquid.SpellID, true);
			}

			// Mount capability depends on liquid State change
			if (oldLiquidStatus != GetLiquidStatus())
				UpdateMountCapability();
		}

		public bool SetWalk(bool enable)
		{
			if (enable == IsWalking())
				return false;

			if (enable)
				AddUnitMovementFlag(MovementFlag.Walking);
			else
				RemoveUnitMovementFlag(MovementFlag.Walking);

			MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetWalkMode : ServerOpcodes.MoveSplineSetRunMode);
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
				MovementInfo.SetFallTime(0);
			}
			else
			{
				RemoveUnitMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar);
			}

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

			MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineStartSwim : ServerOpcodes.MoveSplineStopSwim);
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
			{
				RemoveUnitMovementFlag(MovementFlag.CanFly | MovementFlag.MaskMovingFly);
			}

			if (!enable &&
			    IsTypeId(TypeId.Player))
				ToPlayer().SetFallInformation(0, GetPositionZ());

			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetCanFly : ServerOpcodes.MoveUnsetCanFly);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}
			else
			{
				MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetFlying : ServerOpcodes.MoveSplineUnsetFlying);
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


			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetWaterWalk : ServerOpcodes.MoveSetLandWalk);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}
			else
			{
				MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetWaterWalk : ServerOpcodes.MoveSplineSetLandWalk);
				packet.MoverGUID = GetGUID();
				SendMessageToSet(packet, true);
			}

			return true;
		}

		public bool SetFeatherFall(bool enable)
		{
			// Temporarily disabled for short lived Auras that unapply before client had Time to ACK applying
			//if (enable == HasUnitMovementFlag(MovementFlag.FallingSlow))
			//return false;

			if (enable)
				AddUnitMovementFlag(MovementFlag.FallingSlow);
			else
				RemoveUnitMovementFlag(MovementFlag.FallingSlow);


			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetFeatherFall : ServerOpcodes.MoveSetNormalFall);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}
			else
			{
				MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetFeatherFall : ServerOpcodes.MoveSplineSetNormalFall);
				packet.MoverGUID = GetGUID();
				SendMessageToSet(packet, true);
			}

			return true;
		}

		public bool SetHover(bool enable, bool updateAnimTier = true)
		{
			if (enable == HasUnitMovementFlag(MovementFlag.Hover))
				return false;

			float hoverHeight = UnitData.HoverHeight;

			if (enable)
			{
				//! No need to check height on ascent
				AddUnitMovementFlag(MovementFlag.Hover);

				if (hoverHeight != 0 &&
				    GetPositionZ() - GetFloorZ() < hoverHeight)
					UpdateHeight(GetPositionZ() + hoverHeight);
			}
			else
			{
				RemoveUnitMovementFlag(MovementFlag.Hover);

				//! Dying creatures will MoveFall from setDeathState
				if (hoverHeight != 0 &&
				    (!IsDying() || !IsUnit()))
				{
					float newZ = Math.Max(GetFloorZ(), GetPositionZ() - hoverHeight);
					UpdateAllowedPositionZ(GetPositionX(), GetPositionY(), ref newZ);
					UpdateHeight(newZ);
				}
			}

			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				MoveSetFlag packet = new(enable ? ServerOpcodes.MoveSetHovering : ServerOpcodes.MoveUnsetHovering);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}
			else
			{
				MoveSplineSetFlag packet = new(enable ? ServerOpcodes.MoveSplineSetHover : ServerOpcodes.MoveSplineUnsetHover);
				packet.MoverGUID = GetGUID();
				SendMessageToSet(packet, true);
			}

			if (IsCreature() &&
			    updateAnimTier &&
			    IsAlive() &&
			    !HasUnitState(UnitState.Root) &&
			    !ToCreature().GetMovementTemplate().IsRooted())
			{
				if (IsGravityDisabled())
					SetAnimTier(AnimTier.Fly);
				else if (IsHovering())
					SetAnimTier(AnimTier.Hover);
				else
					SetAnimTier(AnimTier.Ground);
			}

			return true;
		}

		public bool IsWithinCombatRange(Unit obj, float dist2compare)
		{
			if (!obj ||
			    !IsInMap(obj) ||
			    !InSamePhase(obj))
				return false;

			float dx     = GetPositionX() - obj.GetPositionX();
			float dy     = GetPositionY() - obj.GetPositionY();
			float dz     = GetPositionZ() - obj.GetPositionZ();
			float distsq = dx * dx + dy * dy + dz * dz;

			float sizefactor = GetCombatReach() + obj.GetCombatReach();
			float maxdist    = dist2compare + sizefactor;

			return distsq < maxdist * maxdist;
		}

		public bool IsInFrontInMap(Unit target, float distance, float arc = MathFunctions.PI)
		{
			return IsWithinDistInMap(target, distance) && HasInArc(arc, target);
		}

		public bool IsInBackInMap(Unit target, float distance, float arc = MathFunctions.PI)
		{
			return IsWithinDistInMap(target, distance) && !HasInArc(MathFunctions.TwoPi - arc, target);
		}

		public bool IsInAccessiblePlaceFor(Creature c)
		{
			if (IsInWater())
				return c.CanEnterWater();
			else
				return c.CanWalk() || c.CanFly();
		}

		public void NearTeleportTo(float x, float y, float z, float orientation, bool casting = false)
		{
			NearTeleportTo(new Position(x, y, z, orientation), casting);
		}

		public void NearTeleportTo(Position pos, bool casting = false)
		{
			DisableSpline();

			if (IsTypeId(TypeId.Player))
			{
				WorldLocation target = new(GetMapId(), pos);
				ToPlayer().TeleportTo(target, (TeleportToOptions.NotLeaveTransport | TeleportToOptions.NotLeaveCombat | TeleportToOptions.NotUnSummonPet | (casting ? TeleportToOptions.Spell : 0)));
			}
			else
			{
				SendTeleportPacket(pos);
				UpdatePosition(pos, true);
				UpdateObjectVisibility();
			}
		}

		public void SetMovedUnit(Unit target)
		{
			UnitMovedByMe.PlayerMovingMe = null;
			UnitMovedByMe                 = target;
			UnitMovedByMe.PlayerMovingMe = ToPlayer();

			MoveSetActiveMover packet = new();
			packet.MoverGUID = target.GetGUID();
			ToPlayer().SendPacket(packet);
		}

		public void SetControlled(bool apply, UnitState state)
		{
			if (apply)
			{
				if (HasUnitState(state))
					return;

				if (state.HasFlag(UnitState.Controlled))
					CastStop();

				AddUnitState(state);

				switch (state)
				{
					case UnitState.Stunned:
						SetStunned(true);

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
						}

						break;
					case UnitState.Fleeing:
						if (!HasUnitState(UnitState.Stunned | UnitState.Confused))
						{
							ClearUnitState(UnitState.MeleeAttacking);
							SendMeleeAttackStop();
							// SendAutoRepeatCancel ?
							SetFeared(true);
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
						if (HasAuraType(AuraType.ModStun) ||
						    HasAuraType(AuraType.ModStunDisableGravity))
							return;

						ClearUnitState(state);
						SetStunned(false);

						break;
					case UnitState.Root:
						if (HasAuraType(AuraType.ModRoot) ||
						    HasAuraType(AuraType.ModRoot2) ||
						    HasAuraType(AuraType.ModRootDisableGravity) ||
						    GetVehicle() != null ||
						    (IsCreature() && ToCreature().GetMovementTemplate().IsRooted()))
							return;

						ClearUnitState(state);

						if (!HasUnitState(UnitState.Stunned))
							SetRooted(false);

						break;
					case UnitState.Confused:
						if (HasAuraType(AuraType.ModConfuse))
							return;

						ClearUnitState(state);
						SetConfused(false);

						break;
					case UnitState.Fleeing:
						if (HasAuraType(AuraType.ModFear))
							return;

						ClearUnitState(state);
						SetFeared(false);

						break;
					default:
						return;
				}

				ApplyControlStatesIfNeeded();
			}
		}

		private void ApplyControlStatesIfNeeded()
		{
			// Unit States might have been already cleared but Auras still present. I need to check with HasAuraType
			if (HasUnitState(UnitState.Stunned) ||
			    HasAuraType(AuraType.ModStun) ||
			    HasAuraType(AuraType.ModStunDisableGravity))
				SetStunned(true);

			if (HasUnitState(UnitState.Root) ||
			    HasAuraType(AuraType.ModRoot) ||
			    HasAuraType(AuraType.ModRoot2) ||
			    HasAuraType(AuraType.ModRootDisableGravity))
				SetRooted(true);

			if (HasUnitState(UnitState.Confused) ||
			    HasAuraType(AuraType.ModConfuse))
				SetConfused(true);

			if (HasUnitState(UnitState.Fleeing) ||
			    HasAuraType(AuraType.ModFear))
				SetFeared(true);
		}

		private void SetStunned(bool apply)
		{
			if (apply)
			{
				SetTarget(ObjectGuid.Empty);
				SetUnitFlag(UnitFlags.Stunned);

				StopMoving();

				if (IsTypeId(TypeId.Player))
					SetStandState(UnitStandStateType.Stand);

				SetRooted(true);

				CastStop();
			}
			else
			{
				if (IsAlive() &&
				    GetVictim() != null)
					SetTarget(GetVictim().GetGUID());

				// don't remove UNIT_FLAG_STUNNED for pet when owner is mounted (disabled pet's interface)
				Unit owner = GetCharmerOrOwner();

				if (owner == null ||
				    !owner.IsTypeId(TypeId.Player) ||
				    !owner.ToPlayer().IsMounted())
					RemoveUnitFlag(UnitFlags.Stunned);

				if (!HasUnitState(UnitState.Root)) // prevent moving if it also has root effect
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
				{
					RemoveUnitMovementFlag(MovementFlag.Root);
				}
			}

			Player playerMover = GetUnitBeingMoved()?.ToPlayer(); // unit controlled by a player.

			if (playerMover)
			{
				MoveSetFlag packet = new(apply ? ServerOpcodes.MoveRoot : ServerOpcodes.MoveUnroot);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, playerMover);
			}
			else
			{
				MoveSplineSetFlag packet = new(apply ? ServerOpcodes.MoveSplineRoot : ServerOpcodes.MoveSplineUnroot);
				packet.MoverGUID = GetGUID();
				SendMessageToSet(packet, true);
			}
		}

		private void SetFeared(bool apply)
		{
			if (apply)
			{
				SetTarget(ObjectGuid.Empty);

				Unit caster    = null;
				var  fearAuras = GetAuraEffectsByType(AuraType.ModFear);

				if (!fearAuras.Empty())
					caster = Global.ObjAccessor.GetUnit(this, fearAuras[0].GetCasterGUID());

				if (caster == null)
					caster = GetAttackerForHelper();

				GetMotionMaster().MoveFleeing(caster, (uint)(fearAuras.Empty() ? WorldConfig.GetIntValue(WorldCfg.CreatureFamilyFleeDelay) : 0)); // caster == NULL processed in MoveFleeing
			}
			else
			{
				if (IsAlive())
				{
					GetMotionMaster().Remove(MovementGeneratorType.Fleeing);

					if (GetVictim() != null)
						SetTarget(GetVictim().GetGUID());

					if (!IsPlayer() &&
					    !IsInCombat())
						GetMotionMaster().MoveTargetedHome();
				}
			}

			// block / allow control to real player in control (eg charmer)
			if (IsPlayer())
				if (PlayerMovingMe)
					PlayerMovingMe.SetClientControl(this, !apply);
		}

		private void SetConfused(bool apply)
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
					GetMotionMaster().Remove(MovementGeneratorType.Confused);

					if (GetVictim() != null)
						SetTarget(GetVictim().GetGUID());
				}
			}

			// block / allow control to real player in control (eg charmer)
			if (IsPlayer())
				if (PlayerMovingMe)
					PlayerMovingMe.SetClientControl(this, !apply);
		}

		public bool CanFreeMove()
		{
			return !HasUnitState(UnitState.Confused |
			                     UnitState.Fleeing |
			                     UnitState.InFlight |
			                     UnitState.Root |
			                     UnitState.Stunned |
			                     UnitState.Distracted) &&
			       GetOwnerGUID().IsEmpty();
		}

		public void Mount(uint mount, uint VehicleId = 0, uint creatureEntry = 0)
		{
			RemoveAurasByType(AuraType.CosmeticMounted);

			if (mount != 0)
				SetMountDisplayId(mount);

			SetUnitFlag(UnitFlags.Mount);

			Player player = ToPlayer();

			if (player != null)
			{
				// Mount as a vehicle
				if (VehicleId != 0)
					if (CreateVehicleKit(VehicleId, creatureEntry))
					{
						player.SendOnCancelExpectedVehicleRideAura();

						// mounts can also have accessories
						GetVehicleKit().InstallAllAccessories(false);
					}

				// unsummon pet
				Pet pet = player.GetPet();

				if (pet != null)
				{
					Battleground bg = ToPlayer().GetBattleground();

					// don't unsummon pet in arena but SetFlag UNIT_FLAG_STUNNED to disable pet's interface
					if (bg && bg.IsArena())
						pet.SetUnitFlag(UnitFlags.Stunned);
					else
						player.UnsummonPetTemporaryIfAny();
				}

				// if we have charmed npc, stun him also (everywhere)
				Unit charm = player.GetCharmed();

				if (charm)
					if (charm.GetTypeId() == TypeId.Unit)
						charm.SetUnitFlag(UnitFlags.Stunned);

				player.SendMovementSetCollisionHeight(player.GetCollisionHeight(), UpdateCollisionHeightReason.Mount);
			}

			RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Mount);
		}

		public void Dismount()
		{
			if (!IsMounted())
				return;

			SetMountDisplayId(0);
			RemoveUnitFlag(UnitFlags.Mount);

			Player thisPlayer = ToPlayer();

			if (thisPlayer != null)
				thisPlayer.SendMovementSetCollisionHeight(thisPlayer.GetCollisionHeight(), UpdateCollisionHeightReason.Mount);

			// dismount as a vehicle
			if (IsTypeId(TypeId.Player) &&
			    GetVehicleKit() != null)
				// Remove vehicle from player
				RemoveVehicleKit();

			RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Dismount);

			// only resummon old pet if the player is already added to a map
			// this prevents adding a pet to a not created map which would otherwise cause a crash
			// (it could probably happen when logging in after a previous crash)
			Player player = ToPlayer();

			if (player != null)
			{
				Pet pPet = player.GetPet();

				if (pPet != null)
				{
					if (pPet.HasUnitFlag(UnitFlags.Stunned) &&
					    !pPet.HasUnitState(UnitState.Stunned))
						pPet.RemoveUnitFlag(UnitFlags.Stunned);
				}
				else
				{
					player.ResummonPetTemporaryUnSummonedIfAny();
				}

				// if we have charmed npc, remove stun also
				Unit charm = player.GetCharmed();

				if (charm)
					if (charm.GetTypeId() == TypeId.Unit &&
					    charm.HasUnitFlag(UnitFlags.Stunned) &&
					    !charm.HasUnitState(UnitState.Stunned))
						charm.RemoveUnitFlag(UnitFlags.Stunned);
			}
		}

		public bool CreateVehicleKit(uint id, uint creatureEntry, bool loading = false)
		{
			VehicleRecord vehInfo = CliDB.VehicleStorage.LookupByKey(id);

			if (vehInfo == null)
				return false;

			VehicleKit          =  new Vehicle(this, vehInfo, creatureEntry);
			UpdateFlag.Vehicle =  true;
			UnitTypeMask        |= UnitTypeMask.Vehicle;

			if (!loading)
				SendSetVehicleRecId(id);

			return true;
		}

		public void RemoveVehicleKit(bool onRemoveFromWorld = false)
		{
			if (VehicleKit == null)
				return;

			if (!onRemoveFromWorld)
				SendSetVehicleRecId(0);

			VehicleKit.Uninstall();

			VehicleKit = null;

			UpdateFlag.Vehicle =  false;
			UnitTypeMask        &= ~UnitTypeMask.Vehicle;
			RemoveNpcFlag(NPCFlags.SpellClick | NPCFlags.PlayerVehicle);
		}

		private void SendSetVehicleRecId(uint vehicleId)
		{
			Player player = ToPlayer();

			if (player)
			{
				MoveSetVehicleRecID moveSetVehicleRec = new();
				moveSetVehicleRec.MoverGUID     = GetGUID();
				moveSetVehicleRec.SequenceIndex = MovementCounter++;
				moveSetVehicleRec.VehicleRecID  = vehicleId;
				player.SendPacket(moveSetVehicleRec);
			}

			SetVehicleRecID setVehicleRec = new();
			setVehicleRec.VehicleGUID  = GetGUID();
			setVehicleRec.VehicleRecID = vehicleId;
			SendMessageToSet(setVehicleRec, true);
		}

		public MovementForces GetMovementForces()
		{
			return _movementForces;
		}

		private void ApplyMovementForce(ObjectGuid id, Vector3 origin, float magnitude, MovementForceType type, Vector3 direction, ObjectGuid transportGuid = default)
		{
			if (_movementForces == null)
				_movementForces = new MovementForces();

			MovementForce force = new();
			force.ID        = id;
			force.Origin    = origin;
			force.Direction = direction;

			if (transportGuid.IsMOTransport())
				force.TransportID = (uint)transportGuid.GetCounter();

			force.Magnitude = magnitude;
			force.Type      = type;

			if (_movementForces.Add(force))
			{
				Player movingPlayer = GetPlayerMovingMe();

				if (movingPlayer != null)
				{
					MoveApplyMovementForce applyMovementForce = new();
					applyMovementForce.MoverGUID     = GetGUID();
					applyMovementForce.SequenceIndex = (int)MovementCounter++;
					applyMovementForce.Force         = force;
					movingPlayer.SendPacket(applyMovementForce);
				}
				else
				{
					MoveUpdateApplyMovementForce updateApplyMovementForce = new();
					updateApplyMovementForce.Status = MovementInfo;
					updateApplyMovementForce.Force  = force;
					SendMessageToSet(updateApplyMovementForce, true);
				}
			}
		}

		private void RemoveMovementForce(ObjectGuid id)
		{
			if (_movementForces == null)
				return;

			if (_movementForces.Remove(id))
			{
				Player movingPlayer = GetPlayerMovingMe();

				if (movingPlayer != null)
				{
					MoveRemoveMovementForce moveRemoveMovementForce = new();
					moveRemoveMovementForce.MoverGUID     = GetGUID();
					moveRemoveMovementForce.SequenceIndex = (int)MovementCounter++;
					moveRemoveMovementForce.ID            = id;
					movingPlayer.SendPacket(moveRemoveMovementForce);
				}
				else
				{
					MoveUpdateRemoveMovementForce updateRemoveMovementForce = new();
					updateRemoveMovementForce.Status      = MovementInfo;
					updateRemoveMovementForce.TriggerGUID = id;
					SendMessageToSet(updateRemoveMovementForce, true);
				}
			}

			if (_movementForces.IsEmpty())
				_movementForces = new MovementForces();
		}

		public bool SetIgnoreMovementForces(bool ignore)
		{
			if (ignore == HasUnitMovementFlag2(MovementFlag2.IgnoreMovementForces))
				return false;

			if (ignore)
				AddUnitMovementFlag2(MovementFlag2.IgnoreMovementForces);
			else
				RemoveUnitMovementFlag2(MovementFlag2.IgnoreMovementForces);

			ServerOpcodes[] ignoreMovementForcesOpcodeTable =
			{
				ServerOpcodes.MoveUnsetIgnoreMovementForces, ServerOpcodes.MoveSetIgnoreMovementForces
			};

			Player movingPlayer = GetPlayerMovingMe();

			if (movingPlayer != null)
			{
				MoveSetFlag packet = new(ignoreMovementForcesOpcodeTable[ignore ? 1 : 0]);
				packet.MoverGUID     = GetGUID();
				packet.SequenceIndex = MovementCounter++;
				movingPlayer.SendPacket(packet);

				MoveUpdate moveUpdate = new();
				moveUpdate.Status = MovementInfo;
				SendMessageToSet(moveUpdate, movingPlayer);
			}

			return true;
		}

		public void UpdateMovementForcesModMagnitude()
		{
			float modMagnitude = GetTotalAuraMultiplier(AuraType.ModMovementForceMagnitude);

			Player movingPlayer = GetPlayerMovingMe();

			if (movingPlayer != null)
			{
				MoveSetSpeed setModMovementForceMagnitude = new(ServerOpcodes.MoveSetModMovementForceMagnitude);
				setModMovementForceMagnitude.MoverGUID     = GetGUID();
				setModMovementForceMagnitude.SequenceIndex = MovementCounter++;
				setModMovementForceMagnitude.Speed         = modMagnitude;
				movingPlayer.SendPacket(setModMovementForceMagnitude);
				++movingPlayer.MovementForceModMagnitudeChanges;
			}
			else
			{
				MoveUpdateSpeed updateModMovementForceMagnitude = new(ServerOpcodes.MoveUpdateModMovementForceMagnitude);
				updateModMovementForceMagnitude.Status = MovementInfo;
				updateModMovementForceMagnitude.Speed  = modMagnitude;
				SendMessageToSet(updateModMovementForceMagnitude, true);
			}

			if (modMagnitude != 1.0f &&
			    _movementForces == null)
				_movementForces = new MovementForces();

			if (_movementForces != null)
			{
				_movementForces.SetModMagnitude(modMagnitude);

				if (_movementForces.IsEmpty())
					_movementForces = new MovementForces();
			}
		}

		public bool IsPlayingHoverAnim()
		{
			return _playHoverAnim;
		}

		private void SetPlayHoverAnim(bool enable)
		{
			_playHoverAnim = enable;

			SetPlayHoverAnim data = new();
			data.UnitGUID      = GetGUID();
			data.PlayHoverAnim = enable;

			SendMessageToSet(data, true);
		}

		public Unit GetUnitBeingMoved()
		{
			return UnitMovedByMe;
		}

		private Player GetPlayerBeingMoved()
		{
			Unit mover = GetUnitBeingMoved();

			if (mover)
				return mover.ToPlayer();

			return null;
		}

		public Player GetPlayerMovingMe()
		{
			return PlayerMovingMe;
		}

		public void AddUnitMovementFlag(MovementFlag f)
		{
			MovementInfo.AddMovementFlag(f);
		}

		public void RemoveUnitMovementFlag(MovementFlag f)
		{
			MovementInfo.RemoveMovementFlag(f);
		}

		public bool HasUnitMovementFlag(MovementFlag f)
		{
			return MovementInfo.HasMovementFlag(f);
		}

		public MovementFlag GetUnitMovementFlags()
		{
			return MovementInfo.GetMovementFlags();
		}

		public void SetUnitMovementFlags(MovementFlag f)
		{
			MovementInfo.SetMovementFlags(f);
		}

		public void AddUnitMovementFlag2(MovementFlag2 f)
		{
			MovementInfo.AddMovementFlag2(f);
		}

		private void RemoveUnitMovementFlag2(MovementFlag2 f)
		{
			MovementInfo.RemoveMovementFlag2(f);
		}

		public bool HasUnitMovementFlag2(MovementFlag2 f)
		{
			return MovementInfo.HasMovementFlag2(f);
		}

		public MovementFlag2 GetUnitMovementFlags2()
		{
			return MovementInfo.GetMovementFlags2();
		}

		public void SetUnitMovementFlags2(MovementFlag2 f)
		{
			MovementInfo.SetMovementFlags2(f);
		}

		public void AddExtraUnitMovementFlag2(MovementFlags3 f)
		{
			MovementInfo.AddExtraMovementFlag2(f);
		}

		public void RemoveExtraUnitMovementFlag2(MovementFlags3 f)
		{
			MovementInfo.RemoveExtraMovementFlag2(f);
		}

		public bool HasExtraUnitMovementFlag2(MovementFlags3 f)
		{
			return MovementInfo.HasExtraMovementFlag2(f);
		}

		public MovementFlags3 GetExtraUnitMovementFlags2()
		{
			return MovementInfo.GetExtraMovementFlags2();
		}

		public void SetExtraUnitMovementFlags2(MovementFlags3 f)
		{
			MovementInfo.SetExtraMovementFlags2(f);
		}

		//Spline
		public bool IsSplineEnabled()
		{
			return MoveSpline.Initialized() && !MoveSpline.Finalized();
		}

		private void UpdateSplineMovement(uint diff)
		{
			if (MoveSpline.Finalized())
				return;

			MoveSpline.UpdateState((int)diff);
			bool arrived = MoveSpline.Finalized();

			if (MoveSpline.IsCyclic())
			{
				_splineSyncTimer.Update(diff);

				if (_splineSyncTimer.Passed())
				{
					_splineSyncTimer.Reset(5000); // Retail value, do not change

					FlightSplineSync flightSplineSync = new();
					flightSplineSync.Guid       = GetGUID();
					flightSplineSync.SplineDist = MoveSpline.TimePassed() / MoveSpline.Duration();
					SendMessageToSet(flightSplineSync, true);
				}
			}

			if (arrived)
			{
				DisableSpline();

				AnimTier? animTier = MoveSpline.GetAnimation();

				if (animTier.HasValue)
					SetAnimTier(animTier.Value);
			}

			UpdateSplinePosition();
		}

		private void UpdateSplinePosition()
		{
			Vector4 loc = MoveSpline.ComputePosition();

			if (MoveSpline.onTransport)
			{
				Position pos = MovementInfo.Transport.Pos;
				pos.X = loc.X;
				pos.Y = loc.Y;
				pos.Z = loc.Z;
				pos.SetOrientation(loc.W);

				ITransport transport = GetDirectTransport();

				if (transport != null)
					transport.CalculatePassengerPosition(ref loc.X, ref loc.Y, ref loc.Z, ref loc.W);
				else
					return;
			}

			if (HasUnitState(UnitState.CannotTurn))
				loc.W = GetOrientation();

			UpdatePosition(loc.X, loc.Y, loc.Z, loc.W);
		}

		private void InterruptMovementBasedAuras()
		{
			// TODO: Check if orientation Transport offset changed instead of only global orientation
			if (_positionUpdateInfo.Turned)
				RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Turning);

			if (_positionUpdateInfo.Relocated &&
			    !GetVehicle())
				RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Moving);
		}

		public void DisableSpline()
		{
			MovementInfo.RemoveMovementFlag(MovementFlag.Forward);
			MoveSpline.Interrupt();
		}

		//Transport
		public override ObjectGuid GetTransGUID()
		{
			if (GetVehicle() != null)
				return GetVehicleBase().GetGUID();

			if (GetTransport() != null)
				return GetTransport().GetTransportGUID();

			return ObjectGuid.Empty;
		}

		//Teleport
		public void SendTeleportPacket(Position pos)
		{
			// SMSG_MOVE_UPDATE_TELEPORT is sent to nearby players to signal the teleport
			// SMSG_MOVE_TELEPORT is sent to self in order to trigger CMSG_MOVE_TELEPORT_ACK and update the position server side

			MoveUpdateTeleport moveUpdateTeleport = new();
			moveUpdateTeleport.Status = MovementInfo;

			if (_movementForces != null)
				moveUpdateTeleport.MovementForces = _movementForces.GetForces();

			Unit broadcastSource = this;

			// should this really be the unit _being_ moved? not the unit doing the moving?
			Player playerMover = GetUnitBeingMoved()?.ToPlayer();

			if (playerMover)
			{
				float x, y, z, o;
				pos.GetPosition(out x, out y, out z, out o);

				ITransport transportBase = GetDirectTransport();

				if (transportBase != null)
					transportBase.CalculatePassengerOffset(ref x, ref y, ref z, ref o);

				MoveTeleport moveTeleport = new();
				moveTeleport.MoverGUID = GetGUID();
				moveTeleport.Pos       = new Position(x, y, z, o);

				if (GetTransGUID() != ObjectGuid.Empty)
					moveTeleport.TransportGUID = GetTransGUID();

				moveTeleport.Facing        = o;
				moveTeleport.SequenceIndex = MovementCounter++;
				playerMover.SendPacket(moveTeleport);

				broadcastSource = playerMover;
			}
			else
			{
				// This is the only packet sent for creatures which contains MovementInfo structure
				// we do not update MovementInfo for creatures so it needs to be done manually here
				moveUpdateTeleport.Status.Guid = GetGUID();
				moveUpdateTeleport.Status.Pos.Relocate(pos);
				moveUpdateTeleport.Status.Time = Time.GetMSTime();
				var transportBase = GetDirectTransport();

				if (transportBase != null)
				{
					pos.GetPosition(out float tx, out float ty, out float tz, out float to);
					transportBase.CalculatePassengerOffset(ref tx, ref ty, ref tz, ref to);
					moveUpdateTeleport.Status.Transport.Pos.Relocate(tx, ty, tz, to);
				}
			}

			// Broadcast the packet to everyone except self.
			broadcastSource.SendMessageToSet(moveUpdateTeleport, false);
		}
	}
}