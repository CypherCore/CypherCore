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
using Game.Entities;
using System;

namespace Game.Movement
{
    public class ConfusedGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public ConfusedGenerator()
        {
            i_nextMoveTime = new TimeTracker();
        }

        public override void DoInitialize(T owner)
        {
            owner.AddUnitState(UnitState.Confused);
            owner.SetFlag(UnitFields.Flags, UnitFlags.Confused);
            owner.GetPosition(out i_x, out i_y, out i_z);

            if (!owner.IsAlive() || owner.IsStopped())
                return;

            owner.StopMoving();
            owner.AddUnitState(UnitState.ConfusedMove);
        }

        public override void DoFinalize(T owner)
        {
            if (owner.IsTypeId(TypeId.Player))
            {
                owner.RemoveFlag(UnitFields.Flags, UnitFlags.Confused);
                owner.ClearUnitState(UnitState.Confused | UnitState.ConfusedMove);
                owner.StopMoving();
            }
            else if (owner.IsTypeId(TypeId.Unit))
            {
                owner.RemoveFlag(UnitFields.Flags, UnitFlags.Confused);
                owner.ClearUnitState(UnitState.Confused | UnitState.ConfusedMove);
                if (owner.GetVictim())
                    owner.SetTarget(owner.GetVictim().GetGUID());
            }
        }

        public override void DoReset(T owner)
        {
            i_nextMoveTime.Reset(0);

            if (!owner.IsAlive() || owner.IsStopped())
                return;

            owner.StopMoving();
            owner.AddUnitState(UnitState.Confused | UnitState.ConfusedMove);
        }

        public override bool DoUpdate(T owner, uint time_diff)
        {
            if (owner.HasUnitState(UnitState.Root | UnitState.Stunned | UnitState.Distracted))
                return true;

            if (i_nextMoveTime.Passed())
            {
                // currently moving, update location
                owner.AddUnitState(UnitState.ConfusedMove);

                if (owner.moveSpline.Finalized())
                    i_nextMoveTime.Reset(RandomHelper.IRand(800, 1500));
            }
            else
            {
                // waiting for next move
                i_nextMoveTime.Update(time_diff);
                if (i_nextMoveTime.Passed())
                {
                    // start moving
                    owner.AddUnitState(UnitState.ConfusedMove);

                    float dest = (float)(4.0f * RandomHelper.NextDouble() - 2.0f);

                    Position pos = new Position(i_x, i_y, i_z);
                    owner.MovePositionToFirstCollision(ref pos, dest, 0.0f);

                    PathGenerator path = new PathGenerator(owner);
                    path.SetPathLengthLimit(30.0f);
                    bool result = path.CalculatePath(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ());
                    if (!result || path.GetPathType().HasAnyFlag(PathType.NoPath))
                    {
                        i_nextMoveTime.Reset(100);
                        return true;
                    }

                    MoveSplineInit init = new MoveSplineInit(owner);
                    init.MovebyPath(path.GetPath());
                    init.SetWalk(true);
                    init.Launch();
                }
            }

            return true;
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Confused;
        }

        TimeTracker i_nextMoveTime;
        float i_x, i_y, i_z;
    }
}
