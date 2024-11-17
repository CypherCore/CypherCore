// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Movement
{
    public class MoveSplineInit
    {
        public MoveSplineInit(Unit m)
        {
            unit = m;
            args.splineId = MotionMaster.SplineId;

            // Elevators also use MOVEMENTFLAG_ONTRANSPORT but we do not keep track of their position changes
            args.TransformForTransport = !unit.GetTransGUID().IsEmpty();
            // mix existing state into new
            args.flags.SetUnsetFlag(MoveSplineFlagEnum.CanSwim, unit.CanSwim());
            args.walk = unit.HasUnitMovementFlag(MovementFlag.Walking);
            args.flags.SetUnsetFlag(MoveSplineFlagEnum.Flying, unit.HasUnitMovementFlag(MovementFlag.CanFly | MovementFlag.DisableGravity));
            args.flags.SetUnsetFlag(MoveSplineFlagEnum.FastSteering, true);
            args.flags.SetUnsetFlag(MoveSplineFlagEnum.Steering, unit.HasNpcFlag2(NPCFlags2.Steering) || !unit.IsInCombat());
        }

        UnitMoveType SelectSpeedType(MovementFlag moveFlags)
        {
            if (moveFlags.HasAnyFlag(MovementFlag.Flying))
            {
                if (moveFlags.HasAnyFlag(MovementFlag.Backward))
                    return UnitMoveType.FlightBack;
                else
                    return UnitMoveType.Flight;
            }
            else if (moveFlags.HasAnyFlag(MovementFlag.Swimming))
            {
                if (moveFlags.HasAnyFlag(MovementFlag.Backward))
                    return UnitMoveType.SwimBack;
                else
                    return UnitMoveType.Swim;
            }
            else if (moveFlags.HasAnyFlag(MovementFlag.Walking))
            {
                return UnitMoveType.Walk;
            }
            else if (moveFlags.HasAnyFlag(MovementFlag.Backward))
                return UnitMoveType.RunBack;

            // Flying creatures use MOVEMENTFLAG_CAN_FLY or MOVEMENTFLAG_DISABLE_GRAVITY
            // Run speed is their default flight speed.
            return UnitMoveType.Run;
        }

        public int Launch()
        {
            MoveSpline move_spline = unit.MoveSpline;

            bool transport = !unit.GetTransGUID().IsEmpty();
            Vector4 real_position = new();
            // there is a big chance that current position is unknown if current state is not finalized, need compute it
            // this also allows calculate spline position and update map position in much greater intervals
            // Don't compute for transport movement if the unit is in a motion between two transports
            if (!move_spline.Finalized() && move_spline.onTransport == transport)
                real_position = move_spline.ComputePosition();
            else
            {
                Position pos;
                if (!transport)
                    pos = unit;
                else
                    pos = unit.m_movementInfo.transport.pos;

                real_position.X = pos.GetPositionX();
                real_position.Y = pos.GetPositionY();
                real_position.Z = pos.GetPositionZ();
                real_position.W = unit.GetOrientation();
            }

            // should i do the things that user should do? - no.
            if (args.path.Count == 0)
                return 0;

            // correct first vertex
            args.path[0] = new Vector3(real_position.X, real_position.Y, real_position.Z);
            args.initialOrientation = real_position.W;
            args.flags.SetUnsetFlag(MoveSplineFlagEnum.EnterCycle, args.flags.HasFlag(MoveSplineFlagEnum.Cyclic));
            move_spline.onTransport = transport;

            MovementFlag moveFlags = unit.m_movementInfo.GetMovementFlags();
            if (!args.flags.HasFlag(MoveSplineFlagEnum.Backward))
                moveFlags = (moveFlags & ~MovementFlag.Backward) | MovementFlag.Forward;
            else
                moveFlags = (moveFlags & ~MovementFlag.Forward) | MovementFlag.Backward;

            if (Convert.ToBoolean(moveFlags & MovementFlag.Root))
                moveFlags &= ~MovementFlag.MaskMoving;

            if (!args.HasVelocity)
            {
                // If spline is initialized with SetWalk method it only means we need to select
                // walk move speed for it but not add walk flag to unit
                var moveFlagsForSpeed = moveFlags;
                if (args.walk)
                    moveFlagsForSpeed |= MovementFlag.Walking;
                else
                    moveFlagsForSpeed &= ~MovementFlag.Walking;

                args.velocity = unit.GetSpeed(SelectSpeedType(moveFlagsForSpeed));
                Creature creature = unit.ToCreature();
                if (creature != null)
                    if (creature.HasSearchedAssistance())
                        args.velocity *= 0.66f;
            }

            // limit the speed in the same way the client does
            float speedLimit()
            {
                if (args.flags.HasFlag(MoveSplineFlagEnum.UnlimitedSpeed))
                    return float.MaxValue;

                if (args.flags.HasFlag(MoveSplineFlagEnum.Falling) || args.flags.HasFlag(MoveSplineFlagEnum.Catmullrom) || args.flags.HasFlag(MoveSplineFlagEnum.Flying) || args.flags.HasFlag(MoveSplineFlagEnum.Parabolic))
                    return 50.0f;

                return Math.Max(28.0f, unit.GetSpeed(UnitMoveType.Run) * 4.0f);
            };

            args.velocity = Math.Min(args.velocity, speedLimit());

            if (!args.Validate(unit))
                return 0;

            unit.m_movementInfo.SetMovementFlags(moveFlags);
            move_spline.Initialize(args);

            MonsterMove packet = new();
            packet.MoverGUID = unit.GetGUID();
            packet.Pos = new Vector3(real_position.X, real_position.Y, real_position.Z);
            packet.InitializeSplineData(move_spline);
            if (transport)
            {
                packet.SplineData.Move.TransportGUID = unit.GetTransGUID();
                packet.SplineData.Move.VehicleSeat = unit.GetTransSeat();
            }
            unit.SendMessageToSet(packet, true);

            return move_spline.Duration();
        }

        public void Stop()
        {
            MoveSpline move_spline = unit.MoveSpline;

            // No need to stop if we are not moving
            if (move_spline.Finalized())
                return;

            bool transport = !unit.GetTransGUID().IsEmpty();
            Vector4 loc = new();
            if (move_spline.onTransport == transport)
                loc = move_spline.ComputePosition();
            else
            {
                Position pos;
                if (!transport)
                    pos = unit;
                else
                    pos = unit.m_movementInfo.transport.pos;

                loc.X = pos.GetPositionX();
                loc.Y = pos.GetPositionY();
                loc.Z = pos.GetPositionZ();
                loc.W = unit.GetOrientation();
            }

            args.flags.Flags = MoveSplineFlagEnum.Done;
            unit.m_movementInfo.RemoveMovementFlag(MovementFlag.Forward);
            move_spline.onTransport = transport;
            move_spline.Initialize(args);

            MonsterMove packet = new();
            packet.MoverGUID = unit.GetGUID();
            packet.Pos = new Vector3(loc.X, loc.Y, loc.Z);
            packet.SplineData.StopSplineStyle = 2;
            packet.SplineData.Id = move_spline.GetId();

            if (transport)
            {
                packet.SplineData.Move.TransportGUID = unit.GetTransGUID();
                packet.SplineData.Move.VehicleSeat = unit.GetTransSeat();
            }

            unit.SendMessageToSet(packet, true);
        }

        public void SetFacing(Vector3 spot)
        {
            TransportPathTransform transform = new(unit, args.TransformForTransport);
            Vector3 finalSpot = transform.Calc(spot);
            args.facing.f = new Vector3(finalSpot.X, finalSpot.Y, finalSpot.Z);
            args.facing.type = MonsterMoveType.FacingSpot;
        }

        public void SetFacing(float x, float y, float z)
        {
            SetFacing(new Vector3(x, y, z));
        }

        public void SetFacing(Unit target)
        {
            args.facing.angle = unit.GetAbsoluteAngle(target);
            args.facing.target = target.GetGUID();
            args.facing.type = MonsterMoveType.FacingTarget;
        }

        public void SetFacing(float angle)
        {
            if (args.TransformForTransport)
            {
                Unit vehicle = unit.GetVehicleBase();
                if (vehicle != null)
                    angle -= vehicle.GetOrientation();
                else
                {
                    ITransport transport = unit.GetTransport();
                    if (transport != null)
                        angle -= transport.GetTransportOrientation();
                }
            }

            args.facing.angle = MathFunctions.wrap(angle, 0.0f, MathFunctions.TwoPi);
            args.facing.type = MonsterMoveType.FacingAngle;
        }

        public void MoveTo(Vector3 dest, bool generatePath = true, bool forceDestination = false)
        {
            if (generatePath)
            {
                PathGenerator path = new(unit);
                bool result = path.CalculatePath(dest.X, dest.Y, dest.Z, forceDestination);
                if (result && !Convert.ToBoolean(path.GetPathType() & PathType.NoPath))
                {
                    MovebyPath(path.GetPath());
                    return;
                }
            }

            args.path_Idx_offset = 0;
            args.path.Add(default);
            TransportPathTransform transform = new(unit, args.TransformForTransport);
            args.path.Add(transform.Calc(dest));
        }

        public void SetFall()
        {
            args.flags.EnableFalling();
            args.flags.SetUnsetFlag(MoveSplineFlagEnum.FallingSlow, unit.HasUnitMovementFlag(MovementFlag.FallingSlow));
        }

        public void SetFirstPointId(int pointId) { args.path_Idx_offset = pointId; }

        public void SetFly() { args.flags.EnableFlying(); }

        public void SetWalk(bool enable) { args.walk = enable; }

        public void SetSmooth() { args.flags.EnableCatmullRom(); }

        public void SetUncompressed() { args.flags.SetUnsetFlag(MoveSplineFlagEnum.UncompressedPath); }

        public void SetCyclic() { args.flags.SetUnsetFlag(MoveSplineFlagEnum.Cyclic); }

        public void SetVelocity(float vel) { args.velocity = vel; args.HasVelocity = true; }

        public void SetBackward() { args.flags.SetUnsetFlag(MoveSplineFlagEnum.Backward); }

        public void SetTransportEnter() { args.flags.EnableTransportEnter(); }

        public void SetTransportExit() { args.flags.EnableTransportExit(); }

        public void SetOrientationFixed(bool enable) { args.flags.SetUnsetFlag(MoveSplineFlagEnum.OrientationFixed, enable); }

        public void SetJumpOrientationFixed(bool enable) { args.flags.SetUnsetFlag(MoveSplineFlagEnum.JumpOrientationFixed, enable); }

        public void SetSteering() { args.flags.EnableSteering(); }

        public void SetUnlimitedSpeed() { args.flags.SetUnsetFlag(MoveSplineFlagEnum.UnlimitedSpeed, true); }

        public void MovebyPath(Vector3[] controls, int path_offset = 0)
        {
            args.path_Idx_offset = path_offset;
            TransportPathTransform transform = new(unit, args.TransformForTransport);
            for (var i = 0; i < controls.Length; i++)
                args.path.Add(transform.Calc(controls[i]));

        }

        public void MoveTo(float x, float y, float z, bool generatePath = true, bool forceDest = false)
        {
            MoveTo(new Vector3(x, y, z), generatePath, forceDest);
        }

        public void SetParabolic(float amplitude, float time_shift)
        {
            args.effect_start_time_percent = time_shift;
            args.parabolic_amplitude = amplitude;
            args.vertical_acceleration = 0.0f;
            args.flags.EnableParabolic();
        }

        public void SetParabolicVerticalAcceleration(float vertical_acceleration, float time_shift)
        {
            args.effect_start_time_percent = time_shift;
            args.parabolic_amplitude = 0.0f;
            args.vertical_acceleration = vertical_acceleration;
            args.flags.EnableParabolic();
        }

        public void SetAnimation(AnimTier anim, uint tierTransitionId = 0, TimeSpan transitionStartTime = default)
        {
            args.effect_start_time_percent = 0.0f;
            args.effect_start_time = transitionStartTime;
            args.animTier = new();
            args.animTier.TierTransitionId = tierTransitionId;
            args.animTier.AnimTier = (byte)anim;
            if (tierTransitionId == 0)
                args.flags.EnableAnimation();
        }

        public void DisableTransportPathTransformations() { args.TransformForTransport = false; }

        public void SetSpellEffectExtraData(SpellEffectExtraData spellEffectExtraData)
        {
            args.spellEffectExtra = spellEffectExtraData;
        }

        public List<Vector3> Path() { return args.path; }

        public MoveSplineInitArgs args = new();
        Unit unit;
    }

    // Transforms coordinates from global to transport offsets
    public class TransportPathTransform
    {
        public TransportPathTransform(Unit owner, bool transformForTransport)
        {
            _owner = owner;
            _transformForTransport = transformForTransport;
        }

        public Vector3 Calc(Vector3 input)
        {
            float x = input.X;
            float y = input.Y;
            float z = input.Z;
            if (_transformForTransport)
            {
                ITransport transport = _owner.GetDirectTransport();
                if (transport != null)
                {
                    float unused = 0.0f; // need reference
                    transport.CalculatePassengerOffset(ref x, ref y, ref z, ref unused);
                }
            }
            return new Vector3(x, y, z);
        }

        Unit _owner;
        bool _transformForTransport;
    }
}
