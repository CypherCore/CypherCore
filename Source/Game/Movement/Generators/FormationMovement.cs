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
        public FormationMovementGenerator(uint id, Position destination, uint moveType, bool run, bool orientation)
        {
            _movementId = id;
            _destination = destination;
            _moveType = moveType;
            _run = run;
            _orientation = orientation;
        }

        public override void DoInitialize(Creature owner)
        {
            owner.AddUnitState(UnitState.Roaming);

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                _interrupt = true;
                owner.StopMoving();
                return;
            }

            owner.AddUnitState(UnitState.RoamingMove);

            MoveSplineInit init = new MoveSplineInit(owner);
            init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ());
            if (_orientation)
                init.SetFacing(_destination.GetOrientation());

            switch (_moveType)
            {
                case 2: // WAYPOINT_MOVE_TYPE_LAND
                    init.SetAnimation(AnimType.ToGround);
                    break;
                case 3: // WAYPOINT_MOVE_TYPE_TAKEOFF
                    init.SetAnimation(AnimType.ToFly);
                    break;
                case 1: // WAYPOINT_MOVE_TYPE_RUN
                    init.SetWalk(false);
                    break;
                case 0: // WAYPOINT_MOVE_TYPE_WALK
                    init.SetWalk(true);
                    break;
            }

            if (_run)
                init.SetWalk(false);

            init.Launch();
        }

        public override void DoReset(Creature owner)
        {
            owner.StopMoving();
            DoInitialize(owner);
        }

        public override bool DoUpdate(Creature owner, uint diff)
        {
            if (!owner)
                return false;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                _interrupt = true;
                owner.StopMoving();
                return true;
            }

            if ((_interrupt && owner.MoveSpline.Finalized()) || (_recalculateSpeed && !owner.MoveSpline.Finalized()))
            {
                _recalculateSpeed = false;
                _interrupt = false;

                owner.AddUnitState(UnitState.RoamingMove);

                MoveSplineInit init = new MoveSplineInit(owner);
                init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ());
                if (_orientation)
                    init.SetFacing(_destination.GetOrientation());

                switch (_moveType)
                {
                    case 2: // WAYPOINT_MOVE_TYPE_LAND
                        init.SetAnimation(AnimType.ToGround);
                        break;
                    case 3: // WAYPOINT_MOVE_TYPE_TAKEOFF
                        init.SetAnimation(AnimType.ToFly);
                        break;
                    case 1: // WAYPOINT_MOVE_TYPE_RUN
                        init.SetWalk(false);
                        break;
                    case 0: // WAYPOINT_MOVE_TYPE_WALK
                        init.SetWalk(true);
                        break;
                }

                if (_run)
                    init.SetWalk(false);
                init.Launch();
            }

            return !owner.MoveSpline.Finalized();
        }

        public override void DoFinalize(Creature owner)
        {
            owner.ClearUnitState(UnitState.Roaming | UnitState.RoamingMove);

            if (owner.MoveSpline.Finalized())
                MovementInform(owner);
        }

        public override void UnitSpeedChanged()
        {
            _recalculateSpeed = true;
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
        uint _moveType;
        bool _run;
        bool _orientation;
        bool _recalculateSpeed;
        bool _interrupt;
    }
}
