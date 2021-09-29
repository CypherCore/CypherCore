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
    public abstract class MovementGenerator
    {
        public MovementGeneratorMode Mode;
        public MovementGeneratorPriority Priority;
        public MovementGeneratorFlags Flags;
        public UnitState BaseUnitState;

        // on top first update
        public virtual void Initialize(Unit owner) { }

        // on top reassign
        public virtual void Reset(Unit owner) { }

        // on top on MotionMaster::Update
        public abstract bool Update(Unit owner, uint diff);

        // on current top if another movement replaces
        public virtual void Deactivate(Unit owner) { }

        // on movement delete
        public virtual void Finalize(Unit owner, bool active, bool movementInform) { }

        public abstract MovementGeneratorType GetMovementGeneratorType();

        public virtual void UnitSpeedChanged() { }

        // timer in ms
        public virtual void Pause(uint timer = 0) { }

        // timer in ms
        public virtual void Resume(uint overrideTimer = 0) { }

        // used by Evade code for select point to evade with expected restart default movement
        public virtual bool GetResetPosition(Unit u, out float x, out float y, out float z)
        {
            x = y = z = 0.0f;
            return false;
        }

        public void AddFlag(MovementGeneratorFlags flag) { Flags |= flag; }
        public bool HasFlag(MovementGeneratorFlags flag) { return (Flags & flag) != 0; }
        public void RemoveFlag(MovementGeneratorFlags flag) { Flags &= ~flag; }
    }

    public abstract class MovementGeneratorMedium<T> : MovementGenerator where T : Unit
    {
        public override void Initialize(Unit owner)
        {
            DoInitialize((T)owner);
            IsActive = true;
        }

        public override void Reset(Unit owner)
        {
            DoReset((T)owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            return DoUpdate((T)owner, diff);
        }

        public override void Deactivate(Unit owner)
        {
            DoDeactivate((T)owner);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            DoFinalize((T)owner, active, movementInform);
        }

        public bool IsActive { get; set; }

        public abstract void DoInitialize(T owner);
        public abstract void DoFinalize(T owner, bool active, bool movementInform);
        public abstract void DoReset(T owner);
        public abstract bool DoUpdate(T owner, uint diff);
        public abstract void DoDeactivate(T owner);

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Max; }
    }
}
