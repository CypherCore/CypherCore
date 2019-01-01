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
using Framework.Dynamic;
using Game.Entities;

namespace Game.Movement
{
    public abstract class IMovementGenerator
    {
        public abstract void Finalize(Unit owner);

        public abstract void Initialize(Unit owner);

        public abstract void Reset(Unit owner);

        public abstract bool Update(Unit owner, uint time_diff);

        public abstract MovementGeneratorType GetMovementGeneratorType();

        public virtual void unitSpeedChanged() { }
        
        // used by Evade code for select point to evade with expected restart default movement
        public virtual bool GetResetPosition(Unit u, out float x, out float y, out float z)
        {
            x = y = z = 0.0f; 
            return false;
        }
    }

    public abstract class MovementGeneratorMedium<T> : IMovementGenerator where T : Unit
    {
        public override void Initialize(Unit owner)
        {
            DoInitialize((T)owner);
            isActive = true;
        }
        public override void Finalize(Unit owner)
        {
            DoFinalize((T)owner);
        }
        public override void Reset(Unit owner)
        {
            DoReset((T)owner);
        }
        public override bool Update(Unit owner, uint time_diff)
        {
            return DoUpdate((T)owner, time_diff);
        }

        public bool isActive { get; set; }

        public abstract void DoInitialize(T owner);
        public abstract void DoFinalize(T owner);
        public abstract void DoReset(T owner);
        public abstract bool DoUpdate(T owner, uint time_diff);
    }

    public class FollowerReference : Reference<Unit, TargetedMovementGeneratorBase>
    {
        public override void targetObjectBuildLink()
        {
            getTarget().addFollower(this);
        }

        public override void targetObjectDestroyLink()
        {
            getTarget().removeFollower(this);
        }

        public override void sourceObjectDestroyLink()
        {
            GetSource().stopFollowing();
        }
    }
}
