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
    public class FleeingGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public const float MIN_QUIET_DISTANCE = 28.0f;
        public const float MAX_QUIET_DISTANCE = 43.0f;

        public FleeingGenerator(ObjectGuid fright)
        {
            i_frightGUID = fright;
            i_nextCheckTime = new TimeTracker();
        }

        void _setTargetLocation(T owner)
        {
            if (owner == null)
                return;

            if (owner.HasUnitState(UnitState.Root | UnitState.Stunned))
                return;

            if (owner.IsMovementPreventedByCasting())
            {
                owner.CastStop();
                return;
            }

            owner.AddUnitState(UnitState.FleeingMove);

            float x, y, z;
            _getPoint(owner, out x, out y, out z);

            Position mypos = owner.GetPosition();
            bool isInLOS = Global.VMapMgr.isInLineOfSight(PhasingHandler.GetTerrainMapId(owner.GetPhaseShift(), owner.GetMap(), mypos.posX, mypos.posY), mypos.posX, mypos.posY, mypos.posZ + 2.0f, x, y, z + 2.0f, ModelIgnoreFlags.Nothing);

            if (!isInLOS)
            {
                i_nextCheckTime.Reset(200);
                return;
            }

            PathGenerator path = new PathGenerator(owner);
            path.SetPathLengthLimit(30.0f);
            bool result = path.CalculatePath(x, y, z);
            if (!result || path.GetPathType().HasAnyFlag(PathType.NoPath))
            {
                i_nextCheckTime.Reset(100);
                return;
            }

            MoveSplineInit init = new MoveSplineInit(owner);
            init.MovebyPath(path.GetPath());
            init.SetWalk(false);
            int traveltime = init.Launch();
            i_nextCheckTime.Reset(traveltime + RandomHelper.URand(800, 1500));
        }

        void _getPoint(T owner, out float x, out float y, out float z)
        {
            float dist_from_caster, angle_to_caster;
            Unit fright = Global.ObjAccessor.GetUnit(owner, i_frightGUID);
            if (fright != null)
            {
                dist_from_caster = fright.GetDistance(owner);
                if (dist_from_caster > 0.2f)
                    angle_to_caster = fright.GetAngle(owner);
                else
                    angle_to_caster = RandomHelper.FRand(0, 2 * MathFunctions.PI);
            }
            else
            {
                dist_from_caster = 0.0f;
                angle_to_caster = RandomHelper.FRand(0, 2 * MathFunctions.PI);
            }

            float dist, angle;
            if (dist_from_caster < MIN_QUIET_DISTANCE)
            {
                dist = RandomHelper.FRand(0.4f, 1.3f) * (MIN_QUIET_DISTANCE - dist_from_caster);
                angle = angle_to_caster + RandomHelper.FRand(-MathFunctions.PI / 8, MathFunctions.PI / 8);
            }
            else if (dist_from_caster > MAX_QUIET_DISTANCE)
            {
                dist = RandomHelper.FRand(0.4f, 1.0f) * (MAX_QUIET_DISTANCE - MIN_QUIET_DISTANCE);
                angle = -angle_to_caster + RandomHelper.FRand(-MathFunctions.PI / 4, MathFunctions.PI / 4);
            }
            else    // we are inside quiet range
            {
                dist = RandomHelper.FRand(0.6f, 1.2f) * (MAX_QUIET_DISTANCE - MIN_QUIET_DISTANCE);
                angle = RandomHelper.FRand(0, 2 * MathFunctions.PI);
            }

            Position pos = owner.GetFirstCollisionPosition(dist, angle);
            x = pos.posX;
            y = pos.posY;
            z = pos.posZ;
        }

        public override void DoInitialize(T owner)
        {
            if (owner == null)
                return;

            owner.SetFlag(UnitFields.Flags, UnitFlags.Fleeing);
            owner.AddUnitState(UnitState.Fleeing | UnitState.FleeingMove);
            _setTargetLocation(owner);
        }

        public override void DoFinalize(T owner)
        {
            if (owner.IsTypeId(TypeId.Player))
            {
                owner.RemoveFlag(UnitFields.Flags, UnitFlags.Fleeing);
                owner.ClearUnitState(UnitState.Fleeing | UnitState.FleeingMove);
                owner.StopMoving();
            }
            else
            {
                owner.RemoveFlag(UnitFields.Flags, UnitFlags.Fleeing);
                owner.ClearUnitState(UnitState.Fleeing | UnitState.FleeingMove);
                if (owner.GetVictim() != null)
                    owner.SetTarget(owner.GetVictim().GetGUID());
            }
        }

        public override void DoReset(T owner)
        {
            DoInitialize(owner);
        }

        public override bool DoUpdate(T owner, uint time_diff)
        {
            if (owner == null || !owner.IsAlive())
                return false;

            if (owner.HasUnitState(UnitState.Root | UnitState.Stunned))
            {
                owner.ClearUnitState(UnitState.FleeingMove);
                return true;
            }

            i_nextCheckTime.Update(time_diff);
            if (i_nextCheckTime.Passed() && owner.moveSpline.Finalized())
                _setTargetLocation(owner);

            return true;
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Fleeing;
        }

        ObjectGuid i_frightGUID;
        TimeTracker i_nextCheckTime;
    }

    public class TimedFleeingGenerator : FleeingGenerator<Creature>
    {
        public TimedFleeingGenerator(ObjectGuid fright, uint time) : base(fright)
        {
            i_totalFleeTime = new TimeTracker(time);
        }

        public override void Finalize(Unit owner)
        {
            owner.RemoveFlag(UnitFields.Flags, UnitFlags.Fleeing);
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

        public override bool Update(Unit owner, uint time_diff)
        {
            if (!owner.IsAlive())
                return false;

            if (owner.HasUnitState(UnitState.Root | UnitState.Stunned))
            {
                owner.ClearUnitState(UnitState.FleeingMove);
                return true;
            }

            i_totalFleeTime.Update(time_diff);
            if (i_totalFleeTime.Passed())
                return false;

            // This calls grant-parent Update method hiden by FleeingMovementGenerator.Update(Creature &, uint32) version
            // This is done instead of casting Unit& to Creature& and call parent method, then we can use Unit directly
            return base.Update(owner, time_diff);
        }

        TimeTracker i_totalFleeTime;
    }
}
