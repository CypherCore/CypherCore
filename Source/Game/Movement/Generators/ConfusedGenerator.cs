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
using System;

namespace Game.Movement
{
    public class ConfusedGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public ConfusedGenerator()
        {
            _timer = new TimeTracker();
        }

        public override void DoInitialize(T owner)
        {
            if (!owner || !owner.IsAlive())
                return;

            owner.AddUnitState(UnitState.Confused);
            owner.AddUnitFlag(UnitFlags.Confused);
            owner.StopMoving();

            _timer.Reset(0);
            _reference = owner.GetPosition();
        }

        public override void DoReset(T owner)
        {
            DoInitialize(owner);
        }

        public override bool DoUpdate(T owner, uint diff)
        {
            if (!owner || !owner.IsAlive())
                return false;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                _interrupt = true;
                owner.StopMoving();
                return true;
            }
            else
                _interrupt = false;

            // waiting for next move
            _timer.Update(diff);
            if (!_interrupt && _timer.Passed() && owner.MoveSpline.Finalized())
            {
                // start moving
                owner.AddUnitState(UnitState.ConfusedMove);

                Position destination = _reference;
                float distance = (float)(4.0f * RandomHelper.FRand(0.0f, 1.0f) - 2.0f);
                float angle = RandomHelper.FRand(0.0f, 1.0f) * MathF.PI * 2.0f;
                owner.MovePositionToFirstCollision(ref destination, distance, angle);

                if (_path == null)
                    _path = new PathGenerator(owner);

                _path.SetPathLengthLimit(30.0f);
                bool result = _path.CalculatePath(destination.GetPositionX(), destination.GetPositionY(), destination.GetPositionZ());
                if (!result || _path.GetPathType().HasAnyFlag(PathType.NoPath))
                {
                    _timer.Reset(100);
                    return true;
                }

                MoveSplineInit init = new MoveSplineInit(owner);
                init.MovebyPath(_path.GetPath());
                init.SetWalk(true);
                int traveltime = init.Launch();
                _timer.Reset(traveltime + RandomHelper.URand(800, 1500));
            }

            return true;
        }

        public override void DoFinalize(T owner)
        {
            if (owner.IsTypeId(TypeId.Player))
            {
                owner.RemoveUnitFlag(UnitFlags.Confused);
                owner.ClearUnitState(UnitState.Confused);
                owner.StopMoving();
            }
            else if (owner.IsTypeId(TypeId.Unit))
            {
                owner.RemoveUnitFlag(UnitFlags.Confused);
                owner.ClearUnitState(UnitState.Confused | UnitState.ConfusedMove);
                if (owner.GetVictim())
                    owner.SetTarget(owner.GetVictim().GetGUID());
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Confused;
        }

        PathGenerator _path;
        TimeTracker _timer;
        Position _reference;
        bool _interrupt;
    }
}
