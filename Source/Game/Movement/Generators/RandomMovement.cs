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
using Game.Maps;
using System;

namespace Game.Movement
{
    public class RandomMovementGenerator : MovementGeneratorMedium<Creature>
    {
        public RandomMovementGenerator(float spawn_dist = 0.0f)
        {
            _timer = new TimeTracker();
            _wanderDistance = spawn_dist;
        }

        public override void DoInitialize(Creature owner)
        {
            if (owner == null || !owner.IsAlive())
                return;

            owner.AddUnitState(UnitState.Roaming);
            _reference = owner.GetPosition();
            owner.StopMoving();

            if (_wanderDistance == 0)
                _wanderDistance = owner.GetRespawnRadius();

            _timer.Reset(0);
        }

        public override void DoReset(Creature owner)
        {
            DoInitialize(owner);
        }

        public override bool DoUpdate(Creature owner, uint diff)
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

            _timer.Update(diff);
            if (!_interrupt && _timer.Passed() && owner.MoveSpline.Finalized())
                SetRandomLocation(owner);

            return true;
        }

        public override void DoFinalize(Creature owner)
        {
            owner.ClearUnitState(UnitState.Roaming);
            owner.StopMoving();
            owner.SetWalk(false);
        }

        void SetRandomLocation(Creature owner)
        {
            if (owner == null)
                return;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                _interrupt = true;
                owner.StopMoving();
                return;
            }

            owner.AddUnitState(UnitState.RoamingMove);

            Position position = _reference;
            float distance = RandomHelper.FRand(0.0f, 1.0f) * _wanderDistance;
            float angle = RandomHelper.FRand(0.0f, 1.0f) * MathF.PI * 2.0f;
            owner.MovePositionToFirstCollision(ref position, distance, angle);

            uint resetTimer = RandomHelper.randChance(50) ? RandomHelper.URand(5000, 10000) : RandomHelper.URand(1000, 2000);

            if (_path == null)
                _path = new PathGenerator(owner);

            _path.SetPathLengthLimit(30.0f);
            bool result = _path.CalculatePath(position.GetPositionX(), position.GetPositionY(), position.GetPositionZ());
            if (!result || _path.GetPathType().HasAnyFlag(PathType.NoPath))
            {
                _timer.Reset(100);
                return;
            }

            MoveSplineInit init = new MoveSplineInit(owner);
            init.MovebyPath(_path.GetPath());
            init.SetWalk(true);
            int traveltime = init.Launch();
            _timer.Reset(traveltime + resetTimer);

            // Call for creature group update
            owner.SignalFormationMovement(position);
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Random;
        }

        PathGenerator _path;
        TimeTracker _timer;
        Position _reference;
        float _wanderDistance;
        bool _interrupt;
    }
}
