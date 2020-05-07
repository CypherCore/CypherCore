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
    public class FleeingGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public const float MIN_QUIET_DISTANCE = 28.0f;
        public const float MAX_QUIET_DISTANCE = 43.0f;

        public FleeingGenerator(ObjectGuid fright)
        {
            _fleeTargetGUID = fright;
            _timer = new TimeTracker();
        }

        public override void DoInitialize(T owner)
        {
            if (owner == null)
                return;

            owner.AddUnitFlag(UnitFlags.Fleeing);
            owner.AddUnitState(UnitState.Fleeing);
            SetTargetLocation(owner);
        }

        public override void DoReset(T owner)
        {
            DoInitialize(owner);
        }

        public override bool DoUpdate(T owner, uint diff)
        {
            if (owner == null || !owner.IsAlive())
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
                SetTargetLocation(owner);

            return true;
        }

        public override void DoFinalize(T owner)
        {
            if (owner.IsTypeId(TypeId.Player))
            {
                owner.RemoveUnitFlag(UnitFlags.Fleeing);
                owner.ClearUnitState(UnitState.Fleeing);
                owner.StopMoving();
            }
            else
            {
                owner.RemoveUnitFlag(UnitFlags.Fleeing);
                owner.ClearUnitState(UnitState.Fleeing | UnitState.FleeingMove);
                if (owner.GetVictim() != null)
                    owner.SetTarget(owner.GetVictim().GetGUID());
            }
        }

        void SetTargetLocation(T owner)
        {
            if (owner == null)
                return;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                _interrupt = true;
                owner.StopMoving();
                return;
            }

            owner.AddUnitState(UnitState.FleeingMove);

            Position destination = owner.GetPosition();
            GetPoint(owner, ref destination);

            // Add LOS check for target point
            Position currentPosition = owner.GetPosition();
            bool isInLOS = Global.VMapMgr.IsInLineOfSight(PhasingHandler.GetTerrainMapId(owner.GetPhaseShift(), owner.GetMap(), currentPosition.posX, currentPosition.posY), currentPosition.posX, currentPosition.posY, currentPosition.posZ + 2.0f, destination.GetPositionX(), destination.GetPositionY(), destination.GetPositionZ() + 2.0f, ModelIgnoreFlags.Nothing);
            if (!isInLOS)
            {
                _timer.Reset(200);
                return;
            }

            if (_path == null)
                _path = new PathGenerator(owner);

            _path.SetPathLengthLimit(30.0f);
            bool result = _path.CalculatePath(destination.GetPositionX(), destination.GetPositionY(), destination.GetPositionZ());
            if (!result || _path.GetPathType().HasAnyFlag(PathType.NoPath))
            {
                _timer.Reset(100);
                return;
            }

            MoveSplineInit init = new MoveSplineInit(owner);
            init.MovebyPath(_path.GetPath());
            init.SetWalk(false);
            int traveltime = init.Launch();
            _timer.Reset(traveltime + RandomHelper.URand(800, 1500));
        }

        void GetPoint(T owner, ref Position position)
        {
            float casterDistance, casterAngle;
            Unit fleeTarget = Global.ObjAccessor.GetUnit(owner, _fleeTargetGUID);
            if (fleeTarget != null)
            {
                casterDistance = fleeTarget.GetDistance(owner);
                if (casterDistance > 0.2f)
                    casterAngle = fleeTarget.GetAngle(owner);
                else
                    casterAngle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
            }
            else
            {
                casterDistance = 0.0f;
                casterAngle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
            }

            float distance, angle;
            if (casterDistance < MIN_QUIET_DISTANCE)
            {
                distance = RandomHelper.FRand(0.4f, 1.3f) * (MIN_QUIET_DISTANCE - casterDistance);
                angle = casterAngle + RandomHelper.FRand(-MathF.PI / 8.0f, MathF.PI / 8.0f);
            }
            else if (casterDistance > MAX_QUIET_DISTANCE)
            {
                distance = RandomHelper.FRand(0.4f, 1.0f) * (MAX_QUIET_DISTANCE - MIN_QUIET_DISTANCE);
                angle = -casterAngle + RandomHelper.FRand(-MathF.PI / 4.0f, MathF.PI / 4.0f);
            }
            else    // we are inside quiet range
            {
                distance = RandomHelper.FRand(0.6f, 1.2f) * (MAX_QUIET_DISTANCE - MIN_QUIET_DISTANCE);
                angle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
            }

            owner.MovePositionToFirstCollision(ref position, distance, angle);
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Fleeing;
        }

        PathGenerator _path;
        ObjectGuid _fleeTargetGUID;
        TimeTracker _timer;
        bool _interrupt;
    }

    public class TimedFleeingGenerator : FleeingGenerator<Creature>
    {
        public TimedFleeingGenerator(ObjectGuid fright, uint time) : base(fright)
        {
            _totalFleeTime = new TimeTracker(time);
        }

        public override void Finalize(Unit owner)
        {
            owner.RemoveUnitFlag(UnitFlags.Fleeing);
            owner.ClearUnitState(UnitState.Fleeing | UnitState.FleeingMove);
            Unit victim = owner.GetVictim();
            if (victim != null)
            {
                if (owner.IsAlive())
                {
                    owner.AttackStop();
                    owner.ToCreature().GetAI().AttackStart(victim);
                }
            }
        }

        public override bool Update(Unit owner, uint diff)
        {
            if (!owner.IsAlive())
                return false;

            if (owner.HasUnitState(UnitState.Root | UnitState.Stunned))
            {
                owner.ClearUnitState(UnitState.FleeingMove);
                return true;
            }

            _totalFleeTime.Update(diff);
            if (_totalFleeTime.Passed())
                return false;

            // This calls grant-parent Update method hiden by FleeingMovementGenerator.Update(Creature &, uint32) version
            // This is done instead of casting Unit& to Creature& and call parent method, then we can use Unit directly
            return base.Update(owner, diff);
        }

        TimeTracker _totalFleeTime;
    }
}
