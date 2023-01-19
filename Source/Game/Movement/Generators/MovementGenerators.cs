// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using System;

namespace Game.Movement
{
    public abstract class MovementGenerator : IEquatable<MovementGenerator>
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

        public bool Equals(MovementGenerator other)
        {
            if (Mode == other.Mode && Priority == other.Priority)
                return true;

            return false;
        }

        public int GetHashCode(MovementGenerator obj)
        {
            return obj.Mode.GetHashCode() ^ obj.Priority.GetHashCode();
        }

        public virtual string GetDebugInfo()
        {
            return $"Mode: {Mode} Priority: {Priority} Flags: {Flags} BaseUniteState: {BaseUnitState}";
        }
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
