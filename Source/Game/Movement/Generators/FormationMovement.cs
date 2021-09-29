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
using Game.Entities;

namespace Game.Movement
{
    public class FormationMovementGenerator : MovementGeneratorMedium<Creature>
    {
        public FormationMovementGenerator(uint id, Position destination, WaypointMoveType moveType, bool run, bool orientation)
        {
            _movementId = id;
            _destination = destination;
            _moveType = moveType;
            _run = run;
            _orientation = orientation;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
        }

        public override void DoInitialize(Creature owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                return;
            }

            owner.AddUnitState(UnitState.RoamingMove);

            MoveSplineInit init = new(owner);
            init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ());
            if (_orientation)
                init.SetFacing(_destination.GetOrientation());

            switch (_moveType)
            {
                case WaypointMoveType.Land:
                    init.SetAnimation(AnimType.ToGround);
                    break;
                case WaypointMoveType.Takeoff:
                    init.SetAnimation(AnimType.ToFly);
                    break;
                case WaypointMoveType.Run:
                    init.SetWalk(false);
                    break;
                case WaypointMoveType.Walk:
                    init.SetWalk(true);
                    break;
            }

            if (_run)
                init.SetWalk(false);

            init.Launch();
        }

        public override void DoReset(Creature owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

            owner.StopMoving();
            DoInitialize(owner);
        }

        public override bool DoUpdate(Creature owner, uint diff)
        {
            if (!owner)
                return false;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                return true;
            }

            if ((HasFlag(MovementGeneratorFlags.Interrupted) && owner.MoveSpline.Finalized()) || (HasFlag(MovementGeneratorFlags.SpeedUpdatePending) && !owner.MoveSpline.Finalized()))
            {
                RemoveFlag(MovementGeneratorFlags.Interrupted | MovementGeneratorFlags.SpeedUpdatePending);

                owner.AddUnitState(UnitState.RoamingMove);

                MoveSplineInit init = new(owner);
                init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ());
                if (_orientation)
                    init.SetFacing(_destination.GetOrientation());

                switch (_moveType)
                {
                    case WaypointMoveType.Land:
                        init.SetAnimation(AnimType.ToGround);
                        break;
                    case WaypointMoveType.Takeoff:
                        init.SetAnimation(AnimType.ToFly);
                        break;
                    case WaypointMoveType.Run:
                        init.SetWalk(false);
                        break;
                    case WaypointMoveType.Walk:
                        init.SetWalk(true);
                        break;
                }

                if (_run)
                    init.SetWalk(false);
                init.Launch();
            }

            if (owner.MoveSpline.Finalized())
            {
                RemoveFlag(MovementGeneratorFlags.Transitory);
                AddFlag(MovementGeneratorFlags.InformEnabled);
                return false;
            }
            return true;
        }

        public override void DoDeactivate(Creature owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.RoamingMove);
        }

        public override void DoFinalize(Creature owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
                owner.ClearUnitState(UnitState.RoamingMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
                MovementInform(owner);
        }

        public override void UnitSpeedChanged()
        {
            AddFlag(MovementGeneratorFlags.SpeedUpdatePending);
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Formation;
        }

        void MovementInform(Creature owner)
        {
            if (owner.GetAI() != null)
                owner.GetAI().MovementInform(MovementGeneratorType.Formation, _movementId);
        }

        uint _movementId;
        Position _destination;
        WaypointMoveType _moveType;
        bool _run;
        bool _orientation;
    }
}
