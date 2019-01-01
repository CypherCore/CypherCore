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
using Game.Maps;
using System;

namespace Game.Movement
{
    public class RandomMovementGenerator : MovementGeneratorMedium<Creature>
    {
        public RandomMovementGenerator(float spawn_dist = 0.0f)
        {
            i_nextMoveTime = new TimeTrackerSmall();
            wander_distance = spawn_dist;
        }

        TimeTrackerSmall i_nextMoveTime;
        float wander_distance;

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Random;
        }

        public override void DoInitialize(Creature creature)
        {
            if (!creature.IsAlive())
                return;

            if (wander_distance == 0)
                wander_distance = creature.GetRespawnRadius();

            if (wander_distance == 0)//Temp fix
                wander_distance = 50.0f;

            creature.AddUnitState(UnitState.Roaming | UnitState.RoamingMove);
            _setRandomLocation(creature);

        }
        public override void DoFinalize(Creature creature)
        {
            creature.ClearUnitState(UnitState.Roaming | UnitState.RoamingMove);
            creature.SetWalk(false);
        }
        public override void DoReset(Creature creature)
        {
            DoInitialize(creature);
        }

        public override bool DoUpdate(Creature creature, uint diff)
        {
            if (!creature || !creature.IsAlive())
                return false;

            if (creature.HasUnitState(UnitState.Root | UnitState.Stunned | UnitState.Distracted))
            {
                i_nextMoveTime.Reset(0);  // Expire the timer
                creature.ClearUnitState(UnitState.RoamingMove);
                return true;
            }

            if (creature.moveSpline.Finalized())
            {
                i_nextMoveTime.Update((int)diff);
                if (i_nextMoveTime.Passed())
                    _setRandomLocation(creature);
            }
            return true;
        }

        void _setRandomLocation(Creature creature)
        {
            if (creature.IsMovementPreventedByCasting())
            {
                creature.CastStop();
                return;
            }

            float respX, respY, respZ, respO, destZ;
            creature.GetHomePosition(out respX, out respY, out respZ, out respO);
            Map map = creature.GetMap();

            bool is_air_ok = creature.CanFly();

            float angle = (float)(RandomHelper.NextDouble() * MathFunctions.TwoPi);
            float range = (float)(RandomHelper.NextDouble() * wander_distance);
            float distanceX = (float)(range * Math.Cos(angle));
            float distanceY = (float)(range * Math.Sin(angle));

            float destX = respX + distanceX;
            float destY = respY + distanceY;

            // prevent invalid coordinates generation
            GridDefines.NormalizeMapCoord(ref destX);
            GridDefines.NormalizeMapCoord(ref destY);

            float travelDistZ = range;   // sin^2+cos^2=1, so travelDistZ=range^2; no need for sqrt below

            if (is_air_ok)                                          // 3D system above ground and above water (flying mode)
            {
                // Limit height change
                float distanceZ = (float)(RandomHelper.NextDouble() * travelDistZ / 2.0f);
                destZ = respZ + distanceZ;
                float levelZ = map.GetWaterOrGroundLevel(creature.GetPhaseShift(), destX, destY, destZ - 2.5f);

                // Problem here, we must fly above the ground and water, not under. Let's try on next tick
                if (levelZ >= destZ)
                    return;
            }
            else                                                    // 2D only
            {
                // 10.0 is the max that vmap high can check (MAX_CAN_FALL_DISTANCE)
                travelDistZ = travelDistZ >= 10.0f ? 10.0f : travelDistZ;

                // The fastest way to get an accurate result 90% of the time.
                // Better result can be obtained like 99% accuracy with a ray light, but the cost is too high and the code is too long.
                destZ = map.GetHeight(creature.GetPhaseShift(), destX, destY, respZ + travelDistZ - 2.0f, false);

                if (Math.Abs(destZ - respZ) > travelDistZ)              // Map check
                {
                    // Vmap Horizontal or above
                    destZ = map.GetHeight(creature.GetPhaseShift(), destX, destY, respZ - 2.0f, true);

                    if (Math.Abs(destZ - respZ) > travelDistZ)
                    {
                        // Vmap Higher
                        destZ = map.GetHeight(creature.GetPhaseShift(), destX, destY, respZ + travelDistZ - 2.0f, true);

                        // let's forget this bad coords where a z cannot be find and retry at next tick
                        if (Math.Abs(destZ - respZ) > travelDistZ)
                            return;
                    }
                }
            }

            if (is_air_ok)
                i_nextMoveTime.Reset(0);
            else
            {
                if (RandomHelper.randChance(50))
                    i_nextMoveTime.Reset(RandomHelper.IRand(5000, 10000));
                else
                    i_nextMoveTime.Reset(RandomHelper.IRand(50, 400));
            }

            creature.AddUnitState(UnitState.RoamingMove);

            MoveSplineInit init = new MoveSplineInit(creature);
            init.MoveTo(destX, destY, destZ);
            init.SetWalk(true);
            init.Launch();

            //Call for creature group update
            if (creature.GetFormation() != null && creature.GetFormation().getLeader() == creature)
                creature.GetFormation().LeaderMoveTo(destX, destY, destZ);
        }

        public virtual bool GetResetPosition(Creature creature, float x, float y, float z) { return false; }
    }
}
