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
using Game.Entities;
using Game.Network.Packets;
using System;

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
            args.flags.SetUnsetFlag(SplineFlag.CanSwim, unit.CanSwim());
            args.walk = unit.HasUnitMovementFlag(MovementFlag.Walking);
            args.flags.SetUnsetFlag(SplineFlag.Flying, unit.HasUnitMovementFlag(MovementFlag.CanFly | MovementFlag.DisableGravity));
            args.flags.SetUnsetFlag(SplineFlag.SmoothGroundPath, true); // enabled by default, CatmullRom mode or client config "pathSmoothing" will disable this
            args.flags.SetUnsetFlag(SplineFlag.Steering, unit.HasFlag64(UnitFields.NpcFlags, NPCFlags.Steering));
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
            MoveSpline move_spline = unit.moveSpline;

            bool transport = !unit.GetTransGUID().IsEmpty();
            Vector4 real_position = new Vector4();            
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
            if (args.path.Length == 0)
                return 0;

            // correct first vertex
            args.path[0] = new Vector3(real_position.X, real_position.Y, real_position.Z);
            args.initialOrientation = real_position.W;
            move_spline.onTransport = !unit.GetTransGUID().IsEmpty();

            MovementFlag moveFlags = unit.m_movementInfo.GetMovementFlags();
            if (!args.flags.hasFlag(SplineFlag.Backward))
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
            }

            if (!args.Validate(unit))
                return 0;

            unit.m_movementInfo.SetMovementFlags(moveFlags);
            move_spline.Initialize(args);

            MonsterMove packet = new MonsterMove();
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
            MoveSpline move_spline = unit.moveSpline;

            // No need to stop if we are not moving
            if (move_spline.Finalized())
                return;

            bool transport = !unit.GetTransGUID().IsEmpty();
            Vector4 loc = new Vector4();
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

            args.flags.Flags = SplineFlag.Done;
            unit.m_movementInfo.RemoveMovementFlag(MovementFlag.Forward);
            move_spline.onTransport = transport;
            move_spline.Initialize(args);

            MonsterMove packet = new MonsterMove();
            packet.MoverGUID = unit.GetGUID();
            packet.Pos = new Vector3(loc.X, loc.Y, loc.Z);
            packet.SplineData.StopDistanceTolerance = 2;
            packet.SplineData.ID = move_spline.GetId();

            if (transport)
            {
                packet.SplineData.Move.TransportGUID = unit.GetTransGUID();
                packet.SplineData.Move.VehicleSeat = unit.GetTransSeat();
            }

            unit.SendMessageToSet(packet, true);
        }

        public void SetFacing(Unit target)
        {
            args.facing.angle = unit.GetAngle(target);
            args.facing.target = target.GetGUID();
            args.facing.type = MonsterMoveType.FacingTarget;
        }

        public void SetFacing(float angle)
        {
            if (args.TransformForTransport)
            {
                Unit vehicle = unit.GetVehicleBase();
                Transport transport = unit.GetTransport();
                if (vehicle != null)
                    angle -= vehicle.Orientation;
                else if (transport != null)
                    angle -= transport.Orientation;
            }

            args.facing.angle = MathFunctions.wrap(angle, 0.0f, MathFunctions.TwoPi);
            args.facing.type = MonsterMoveType.FacingAngle;
        }

        public void MoveTo(Vector3 dest, bool generatePath = true, bool forceDestination = false)
        {
            if (generatePath)
            {
                PathGenerator path = new PathGenerator(unit);
                bool result = path.CalculatePath(dest.X, dest.Y, dest.Z, forceDestination);
                if (result && !Convert.ToBoolean(path.GetPathType() & PathType.NoPath))
                {
                    MovebyPath(path.GetPath());
                    return;
                }
            }

            args.path_Idx_offset = 0;
            args.path = new Vector3[2];
            TransportPathTransform transform = new TransportPathTransform(unit, args.TransformForTransport);
            args.path[1] = transform.Calc(dest);
        }

        public void SetFall()
        {
            args.flags.EnableFalling();
            args.flags.SetUnsetFlag(SplineFlag.FallingSlow, unit.HasUnitMovementFlag(MovementFlag.FallingSlow));
        }

        public void SetFirstPointId(int pointId) { args.path_Idx_offset = pointId; }

        public void SetFly() { args.flags.EnableFlying(); }

        public void SetWalk(bool enable) { args.walk = enable; }

        public void SetSmooth() { args.flags.EnableCatmullRom(); }

        public void SetUncompressed() { args.flags.SetUnsetFlag(SplineFlag.UncompressedPath); }

        public void SetCyclic() { args.flags.SetUnsetFlag(SplineFlag.Cyclic); }

        public void SetVelocity(float vel) { args.velocity = vel; args.HasVelocity = true; }

        void SetBackward() { args.flags.SetUnsetFlag(SplineFlag.Backward); }

        public void SetTransportEnter() { args.flags.EnableTransportEnter(); }

        public void SetTransportExit() { args.flags.EnableTransportExit(); }

        public void SetOrientationFixed(bool enable) { args.flags.SetUnsetFlag(SplineFlag.OrientationFixed, enable); }

        public void MovebyPath(Vector3[] controls, int path_offset = 0)
        {
            args.path_Idx_offset = path_offset;
            args.path = new Vector3[controls.Length];
            TransportPathTransform transform = new TransportPathTransform(unit, args.TransformForTransport);
            for (var i = 0; i < controls.Length; i++)
                args.path[i] = transform.Calc(controls[i]);

        }

        public void MoveTo(float x, float y, float z, bool generatePath = true, bool forceDest = false)
        {
            MoveTo(new Vector3(x, y, z), generatePath, forceDest);
        }

        public void SetParabolic(float amplitude, float time_shift)
        {
            args.time_perc = time_shift;
            args.parabolic_amplitude = amplitude;
            args.flags.EnableParabolic();
        }

        public void SetAnimation(AnimType anim)
        {
            args.time_perc = 0.0f;
            args.flags.EnableAnimation((byte)anim);
        }

        public void SetFacing(Vector3 spot)
        {
            TransportPathTransform transform = new TransportPathTransform(unit, args.TransformForTransport);
            Vector3 finalSpot = transform.Calc(spot);
            args.facing.f = new Vector3(finalSpot.X, finalSpot.Y, finalSpot.Z);
            args.facing.type = MonsterMoveType.FacingSpot;
        }

        public void DisableTransportPathTransformations() { args.TransformForTransport = false; }

        public void SetSpellEffectExtraData(SpellEffectExtraData spellEffectExtraData)
        {
            args.spellEffectExtra.Set(spellEffectExtraData);
        }

        public Vector3[] Path() { return args.path; }

        public MoveSplineInitArgs args = new MoveSplineInitArgs();
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
