// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Movement
{
    public class MoveSplineInit
    {
        public MoveSplineInitArgs Args = new();
        private readonly Unit _unit;

        public MoveSplineInit(Unit m)
        {
            _unit = m;
            Args.SplineId = MotionMaster.SplineId;

            // Elevators also use MOVEMENTFLAG_ONTRANSPORT but we do not keep track of their position changes
            Args.TransformForTransport = !_unit.GetTransGUID().IsEmpty();
            // mix existing State into new
            Args.Flags.SetUnsetFlag(SplineFlag.CanSwim, _unit.CanSwim());
            Args.Walk = _unit.HasUnitMovementFlag(MovementFlag.Walking);
            Args.Flags.SetUnsetFlag(SplineFlag.Flying, _unit.HasUnitMovementFlag(MovementFlag.CanFly | MovementFlag.DisableGravity));
            Args.Flags.SetUnsetFlag(SplineFlag.SmoothGroundPath, true); // enabled by default, CatmullRom mode or client config "pathSmoothing" will disable this
            Args.Flags.SetUnsetFlag(SplineFlag.Steering, _unit.HasNpcFlag2(NPCFlags2.Steering));
        }

        public int Launch()
        {
            MoveSpline move_spline = _unit.MoveSpline;

            bool transport = !_unit.GetTransGUID().IsEmpty();
            Vector4 real_position = new();

            // there is a big chance that current position is unknown if current State is not finalized, need compute it
            // this also allows calculate spline position and update map position in much greater intervals
            // Don't compute for Transport movement if the unit is in a motion between two transports
            if (!move_spline.Finalized() &&
                move_spline.onTransport == transport)
            {
                real_position = move_spline.ComputePosition();
            }
            else
            {
                Position pos;

                if (!transport)
                    pos = _unit;
                else
                    pos = _unit.MovementInfo.Transport.Pos;

                real_position.X = pos.GetPositionX();
                real_position.Y = pos.GetPositionY();
                real_position.Z = pos.GetPositionZ();
                real_position.W = _unit.GetOrientation();
            }

            // should i do the things that user should do? - no.
            if (Args.Path.Count == 0)
                return 0;

            // correct first vertex
            Args.Path[0] = new Vector3(real_position.X, real_position.Y, real_position.Z);
            Args.InitialOrientation = real_position.W;
            Args.Flags.SetUnsetFlag(SplineFlag.EnterCycle, Args.Flags.HasFlag(SplineFlag.Cyclic));
            move_spline.onTransport = transport;

            MovementFlag moveFlags = _unit.MovementInfo.GetMovementFlags();

            if (!Args.Flags.HasFlag(SplineFlag.Backward))
                moveFlags = (moveFlags & ~MovementFlag.Backward) | MovementFlag.Forward;
            else
                moveFlags = (moveFlags & ~MovementFlag.Forward) | MovementFlag.Backward;

            if (Convert.ToBoolean(moveFlags & MovementFlag.Root))
                moveFlags &= ~MovementFlag.MaskMoving;

            if (!Args.HasVelocity)
            {
                // If spline is initialized with SetWalk method it only means we need to select
                // walk move speed for it but not add walk flag to unit
                var moveFlagsForSpeed = moveFlags;

                if (Args.Walk)
                    moveFlagsForSpeed |= MovementFlag.Walking;
                else
                    moveFlagsForSpeed &= ~MovementFlag.Walking;

                Args.Velocity = _unit.GetSpeed(SelectSpeedType(moveFlagsForSpeed));
                Creature creature = _unit.ToCreature();

                if (creature != null)
                    if (creature.HasSearchedAssistance())
                        Args.Velocity *= 0.66f;
            }

            // limit the speed in the same way the client does
            float speedLimit()
            {
                if (Args.Flags.HasFlag(SplineFlag.UnlimitedSpeed))
                    return float.MaxValue;

                if (Args.Flags.HasFlag(SplineFlag.Falling) ||
                    Args.Flags.HasFlag(SplineFlag.Catmullrom) ||
                    Args.Flags.HasFlag(SplineFlag.Flying) ||
                    Args.Flags.HasFlag(SplineFlag.Parabolic))
                    return 50.0f;

                return Math.Max(28.0f, _unit.GetSpeed(UnitMoveType.Run) * 4.0f);
            }

            ;

            Args.Velocity = Math.Min(Args.Velocity, speedLimit());

            if (!Args.Validate(_unit))
                return 0;

            _unit.MovementInfo.SetMovementFlags(moveFlags);
            move_spline.Initialize(Args);

            MonsterMove packet = new();
            packet.MoverGUID = _unit.GetGUID();
            packet.Pos = new Vector3(real_position.X, real_position.Y, real_position.Z);
            packet.InitializeSplineData(move_spline);

            if (transport)
            {
                packet.SplineData.Move.TransportGUID = _unit.GetTransGUID();
                packet.SplineData.Move.VehicleSeat = _unit.GetTransSeat();
            }

            _unit.SendMessageToSet(packet, true);

            return move_spline.Duration();
        }

        public void Stop()
        {
            MoveSpline move_spline = _unit.MoveSpline;

            // No need to stop if we are not moving
            if (move_spline.Finalized())
                return;

            bool transport = !_unit.GetTransGUID().IsEmpty();
            Vector4 loc = new();

            if (move_spline.onTransport == transport)
            {
                loc = move_spline.ComputePosition();
            }
            else
            {
                Position pos;

                if (!transport)
                    pos = _unit;
                else
                    pos = _unit.MovementInfo.Transport.Pos;

                loc.X = pos.GetPositionX();
                loc.Y = pos.GetPositionY();
                loc.Z = pos.GetPositionZ();
                loc.W = _unit.GetOrientation();
            }

            Args.Flags.Flags = SplineFlag.Done;
            _unit.MovementInfo.RemoveMovementFlag(MovementFlag.Forward);
            move_spline.onTransport = transport;
            move_spline.Initialize(Args);

            MonsterMove packet = new();
            packet.MoverGUID = _unit.GetGUID();
            packet.Pos = new Vector3(loc.X, loc.Y, loc.Z);
            packet.SplineData.StopDistanceTolerance = 2;
            packet.SplineData.Id = move_spline.GetId();

            if (transport)
            {
                packet.SplineData.Move.TransportGUID = _unit.GetTransGUID();
                packet.SplineData.Move.VehicleSeat = _unit.GetTransSeat();
            }

            _unit.SendMessageToSet(packet, true);
        }

        public void SetFacing(Unit target)
        {
            Args.Facing.Angle = _unit.GetAbsoluteAngle(target);
            Args.Facing.Target = target.GetGUID();
            Args.Facing.type = MonsterMoveType.FacingTarget;
        }

        public void SetFacing(float angle)
        {
            if (Args.TransformForTransport)
            {
                Unit vehicle = _unit.GetVehicleBase();

                if (vehicle != null)
                {
                    angle -= vehicle.GetOrientation();
                }
                else
                {
                    ITransport transport = _unit.GetTransport();

                    if (transport != null)
                        angle -= transport.GetTransportOrientation();
                }
            }

            Args.Facing.Angle = MathFunctions.wrap(angle, 0.0f, MathFunctions.TwoPi);
            Args.Facing.type = MonsterMoveType.FacingAngle;
        }

        public void MoveTo(Vector3 dest, bool generatePath = true, bool forceDestination = false)
        {
            if (generatePath)
            {
                PathGenerator path = new(_unit);
                bool result = path.CalculatePath(dest.X, dest.Y, dest.Z, forceDestination);

                if (result && !Convert.ToBoolean(path.GetPathType() & PathType.NoPath))
                {
                    MovebyPath(path.GetPath());

                    return;
                }
            }

            Args.PathIdxOffset = 0;
            Args.Path.Add(default);
            TransportPathTransform transform = new(_unit, Args.TransformForTransport);
            Args.Path.Add(transform.Calc(dest));
        }

        public void SetFall()
        {
            Args.Flags.EnableFalling();
            Args.Flags.SetUnsetFlag(SplineFlag.FallingSlow, _unit.HasUnitMovementFlag(MovementFlag.FallingSlow));
        }

        public void SetFirstPointId(int pointId)
        {
            Args.PathIdxOffset = pointId;
        }

        public void SetFly()
        {
            Args.Flags.EnableFlying();
        }

        public void SetWalk(bool enable)
        {
            Args.Walk = enable;
        }

        public void SetSmooth()
        {
            Args.Flags.EnableCatmullRom();
        }

        public void SetUncompressed()
        {
            Args.Flags.SetUnsetFlag(SplineFlag.UncompressedPath);
        }

        public void SetCyclic()
        {
            Args.Flags.SetUnsetFlag(SplineFlag.Cyclic);
        }

        public void SetVelocity(float vel)
        {
            Args.Velocity = vel;
            Args.HasVelocity = true;
        }

        public void SetTransportEnter()
        {
            Args.Flags.EnableTransportEnter();
        }

        public void SetTransportExit()
        {
            Args.Flags.EnableTransportExit();
        }

        public void SetOrientationFixed(bool enable)
        {
            Args.Flags.SetUnsetFlag(SplineFlag.OrientationFixed, enable);
        }

        public void SetUnlimitedSpeed()
        {
            Args.Flags.SetUnsetFlag(SplineFlag.UnlimitedSpeed, true);
        }

        public void MovebyPath(Vector3[] controls, int path_offset = 0)
        {
            Args.PathIdxOffset = path_offset;
            TransportPathTransform transform = new(_unit, Args.TransformForTransport);

            for (var i = 0; i < controls.Length; i++)
                Args.Path.Add(transform.Calc(controls[i]));
        }

        public void MoveTo(float x, float y, float z, bool generatePath = true, bool forceDest = false)
        {
            MoveTo(new Vector3(x, y, z), generatePath, forceDest);
        }

        public void SetParabolic(float amplitude, float time_shift)
        {
            Args.TimePerc = time_shift;
            Args.ParabolicAmplitude = amplitude;
            Args.VerticalAcceleration = 0.0f;
            Args.Flags.EnableParabolic();
        }

        public void SetParabolicVerticalAcceleration(float vertical_acceleration, float time_shift)
        {
            Args.TimePerc = time_shift;
            Args.ParabolicAmplitude = 0.0f;
            Args.VerticalAcceleration = vertical_acceleration;
            Args.Flags.EnableParabolic();
        }

        public void SetAnimation(AnimTier anim)
        {
            Args.TimePerc = 0.0f;
            Args.AnimTier = new AnimTierTransition();
            Args.AnimTier.AnimTier = (byte)anim;
            Args.Flags.EnableAnimation();
        }

        public void SetFacing(Vector3 spot)
        {
            TransportPathTransform transform = new(_unit, Args.TransformForTransport);
            Vector3 finalSpot = transform.Calc(spot);
            Args.Facing.F = new Vector3(finalSpot.X, finalSpot.Y, finalSpot.Z);
            Args.Facing.type = MonsterMoveType.FacingSpot;
        }

        public void DisableTransportPathTransformations()
        {
            Args.TransformForTransport = false;
        }

        public void SetSpellEffectExtraData(SpellEffectExtraData spellEffectExtraData)
        {
            Args.SpellEffectExtra = spellEffectExtraData;
        }

        public List<Vector3> Path()
        {
            return Args.Path;
        }

        private UnitMoveType SelectSpeedType(MovementFlag moveFlags)
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
            {
                return UnitMoveType.RunBack;
            }

            // Flying creatures use MOVEMENTFLAG_CAN_FLY or MOVEMENTFLAG_DISABLE_GRAVITY
            // Run speed is their default flight speed.
            return UnitMoveType.Run;
        }

        private void SetBackward()
        {
            Args.Flags.SetUnsetFlag(SplineFlag.Backward);
        }
    }
}