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
using Framework.Dynamic;
using Game.Entities;
using Framework.GameMath;

namespace Game.Movement
{
    public interface IMovementGenerator
    {
        public void Finalize(Unit owner);

        public void Initialize(Unit owner);

        public void Reset(Unit owner);

        public bool Update(Unit owner, uint time_diff);

        public MovementGeneratorType GetMovementGeneratorType();

        public void UnitSpeedChanged() { }

        public void Pause(uint timer = 0) { }

        public void Resume(uint overrideTimer = 0) { }

        // used by Evade code for select point to evade with expected restart default movement
        public bool GetResetPosition(Unit u, out float x, out float y, out float z)
        {
            x = y = z = 0.0f;
            return false;
        }
    }

    public abstract class MovementGeneratorMedium<T> : IMovementGenerator where T : Unit
    {
        public virtual void Initialize(Unit owner)
        {
            DoInitialize((T)owner);
            IsActive = true;
        }

        public virtual void Finalize(Unit owner)
        {
            DoFinalize((T)owner);
        }

        public virtual void Reset(Unit owner)
        {
            DoReset((T)owner);
        }

        public virtual bool Update(Unit owner, uint diff)
        {
            return DoUpdate((T)owner, diff);
        }

        public bool IsActive { get; set; }

        public abstract void DoInitialize(T owner);
        public abstract void DoFinalize(T owner);
        public abstract void DoReset(T owner);
        public abstract bool DoUpdate(T owner, uint diff);

        public virtual MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Max; }
    }
}
