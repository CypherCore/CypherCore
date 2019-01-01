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
using Game.Movement;

namespace Game.AI
{
    public class HomeMovementGenerator<T> : MovementGeneratorMedium<T> where T : Creature
    {
        public override void DoInitialize(T owner)
        {
            SetTargetLocation(owner);
        }

        public override void DoFinalize(T owner)
        {
            if (arrived)
            {
                owner.ClearUnitState(UnitState.Evade);
                owner.SetWalk(true);
                owner.LoadCreaturesAddon();
                owner.GetAI().JustReachedHome();
                owner.SetSpawnHealth();
            }
        }

        public override void DoReset(T owner) { }

        public override bool DoUpdate(T owner, uint time_diff)
        {
            arrived = skipToHome || owner.moveSpline.Finalized();
            return !arrived;
        }

        void SetTargetLocation(T owner)
        {
            if (owner.HasUnitState(UnitState.Root | UnitState.Stunned | UnitState.Distracted))
            { // if we are ROOT/STUNNED/DISTRACTED even after aura clear, finalize on next update - otherwise we would get stuck in evade
                skipToHome = true;
                return;
            }

            MoveSplineInit init = new MoveSplineInit(owner);
            float x, y, z, o;
            // at apply we can select more nice return points base at current movegen
            if (owner.GetMotionMaster().empty() || !owner.GetMotionMaster().top().GetResetPosition(owner, out x, out y, out z))
            {
                owner.GetHomePosition(out x, out y, out z, out o);
                init.SetFacing(o);
            }
            init.MoveTo(x, y, z);
            init.SetWalk(false);
            init.Launch();

            skipToHome = false;
            arrived = false;

            owner.ClearUnitState(UnitState.AllState & ~(UnitState.Evade | UnitState.IgnorePathfinding));
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Home;
        }

        bool arrived;
        bool skipToHome;
    }
}
