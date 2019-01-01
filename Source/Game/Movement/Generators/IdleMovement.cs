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

namespace Game.Movement
{
    public class IdleMovementGenerator : IMovementGenerator
    {
        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Idle;
        }

        public bool isActive { get; set; }

        public override void Reset(Unit owner)
        {
            if (!owner.isStopped())
                owner.StopMoving();
        }
        public override void Initialize(Unit owner)
        {
            Reset(owner);
        }
        public override void Finalize(Unit owner)
        {
        }
        public override bool Update(Unit owner, uint time_diff)
        {
            return true;
        }
    }

    public class RotateMovementGenerator : IMovementGenerator
    {
        public RotateMovementGenerator(uint time, RotateDirection direction)
        {
            m_duration = time;
            m_maxDuration = time;
            m_direction = direction;
        }

        public override void Finalize(Unit owner)
        {
            owner.ClearUnitState(UnitState.Rotating);
            if (owner.IsTypeId(TypeId.Unit))
                owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Rotate, 0);
        }

        public override void Initialize(Unit owner)
        {
            if (!owner.IsStopped())
                owner.StopMoving();

            if (owner.GetVictim())
                owner.SetInFront(owner.GetVictim());

            owner.AddUnitState(UnitState.Rotating);
            owner.AttackStop();
        }

        public override bool Update(Unit owner, uint time_diff)
        {
            float angle = owner.GetOrientation();
            angle += time_diff * MathFunctions.TwoPi / m_maxDuration * (m_direction == RotateDirection.Left ? 1.0f : -1.0f);
            angle = MathFunctions.wrap(angle, 0.0f, MathFunctions.TwoPi);

            owner.SetOrientation(angle);   // UpdateSplinePosition does not set orientation with UNIT_STATE_ROTATING
            owner.SetFacingTo(angle);      // Send spline movement to clients

            if (m_duration > time_diff)
                m_duration -= time_diff;
            else
                return false;

            return true;
        }

        public override void Reset(Unit owner) { }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Rotate; }

        uint m_duration, m_maxDuration;
        RotateDirection m_direction;
    }

    public class DistractMovementGenerator : IMovementGenerator
    {
        public DistractMovementGenerator(uint timer)
        {
            m_timer = timer;
        }

        public override void Finalize(Unit owner)
        {
            owner.ClearUnitState(UnitState.Distracted);

            // If this is a creature, then return orientation to original position (for idle movement creatures)
            if (owner.IsTypeId(TypeId.Unit))
            {
                float angle = owner.ToCreature().GetHomePosition().GetOrientation();
                owner.SetFacingTo(angle);
            }
        }

        public override void Initialize(Unit owner)
        {
            // Distracted creatures stand up if not standing
            if (!owner.IsStandState())
                owner.SetStandState(UnitStandStateType.Stand);

            owner.AddUnitState(UnitState.Distracted);
        }

        public override bool Update(Unit owner, uint time_diff)
        {
            if (time_diff > m_timer)
                return false;

            m_timer -= time_diff;
            return true;
        }

        public override void Reset(Unit owner) { }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Distract; }

        uint m_timer;
    }

    public class AssistanceDistractMovementGenerator : DistractMovementGenerator
    {
        public AssistanceDistractMovementGenerator(uint timer) : base(timer) { }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.AssistanceDistract; }
        
        public override void Finalize(Unit owner)
        {
            owner.ClearUnitState(UnitState.Distracted);
            owner.ToCreature().SetReactState(ReactStates.Aggressive);
        }
    }
}
