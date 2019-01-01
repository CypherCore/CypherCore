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
    public class PointMovementGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public PointMovementGenerator(ulong _id, float _x, float _y, float _z, bool _generatePath, float _speed = 0.0f, Unit faceTarget = null, SpellEffectExtraData spellEffectExtraData = null)
        {
            id = _id;
            i_x = _x;
            i_y = _y;
            i_z = _z;
            speed = _speed;
            i_faceTarget = faceTarget;
            i_spellEffectExtra = spellEffectExtraData;
            m_generatePath = _generatePath;
            i_recalculateSpeed = false;
        }

        public override void DoInitialize(T owner)
        {
            if (!owner.IsStopped())
                owner.StopMoving();

            owner.AddUnitState(UnitState.Roaming | UnitState.RoamingMove);

            if (id == EventId.ChargePrepath)
                return;

            MoveSplineInit init = new MoveSplineInit(owner);
            init.MoveTo(i_x, i_y, i_z, m_generatePath);
            if (speed > 0.0f)
                init.SetVelocity(speed);

            if (i_faceTarget)
                init.SetFacing(i_faceTarget);

            if (i_spellEffectExtra != null)
                init.SetSpellEffectExtraData(i_spellEffectExtra);

            init.Launch();

            // Call for creature group update
            Creature creature = owner.ToCreature();
            if (creature != null)
            if (creature.GetFormation() != null && creature.GetFormation().getLeader() == creature)
                creature.GetFormation().LeaderMoveTo(i_x, i_y, i_z);
        }

        public override bool DoUpdate(T owner, uint time_diff)
        {
            if (owner == null)
                return false;

            if (owner.HasUnitState(UnitState.Root | UnitState.Stunned))
            {
                owner.ClearUnitState(UnitState.RoamingMove);
                return true;
            }

            owner.AddUnitState(UnitState.RoamingMove);

            if (id != EventId.ChargePrepath && i_recalculateSpeed && !owner.moveSpline.Finalized())
            {
                i_recalculateSpeed = false;
                MoveSplineInit init = new MoveSplineInit(owner);
                init.MoveTo(i_x, i_y, i_z, m_generatePath);
                if (speed > 0.0f) // Default value for point motion type is 0.0, if 0.0 spline will use GetSpeed on unit
                    init.SetVelocity(speed);
                init.Launch();

                // Call for creature group update
                Creature creature = owner.ToCreature();
                if (creature != null)
                    if (creature.GetFormation() != null && creature.GetFormation().getLeader() == creature)
                        creature.GetFormation().LeaderMoveTo(i_x, i_y, i_z);
            }

            return !owner.moveSpline.Finalized();
        }

        public override void DoFinalize(T owner)
        {
            if (!owner.HasUnitState(UnitState.Charging))
                owner.ClearUnitState(UnitState.Roaming | UnitState.RoamingMove);

            if (owner.moveSpline.Finalized())
                MovementInform(owner);
        }

        public override void DoReset(T owner)
        {
            if (!owner.IsStopped())
                owner.StopMoving();

            owner.AddUnitState(UnitState.Roaming | UnitState.RoamingMove);
        }

        public void MovementInform(T unit)
        {
            if (!unit.IsTypeId(TypeId.Unit))
                return;

            if (unit.ToCreature().GetAI() != null)
                unit.ToCreature().GetAI().MovementInform(MovementGeneratorType.Point, (uint)id);
        }

        public override void unitSpeedChanged()
        {
            i_recalculateSpeed = true;
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Point;
        }

        ulong id;
        float i_x, i_y, i_z;
        float speed;
        Unit i_faceTarget;
        SpellEffectExtraData i_spellEffectExtra;
        bool m_generatePath;
        bool i_recalculateSpeed;
    }

    public class AssistanceMovementGenerator : PointMovementGenerator<Creature>
    {
        public AssistanceMovementGenerator(float _x, float _y, float _z) : base(0, _x, _y, _z, true) { }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Assistance; }

        public override void Finalize(Unit owner)
        {
            owner.ToCreature().SetNoCallAssistance(false);
            owner.ToCreature().CallAssistance();
            if (owner.IsAlive())
                owner.GetMotionMaster().MoveSeekAssistanceDistract(WorldConfig.GetUIntValue(WorldCfg.CreatureFamilyAssistanceDelay));
        }
    }

    // Does almost nothing - just doesn't allows previous movegen interrupt current effect.
    public class EffectMovementGenerator : IMovementGenerator
    {
        public EffectMovementGenerator(uint Id, uint arrivalSpellId = 0, ObjectGuid arrivalSpellTargetGuid = default(ObjectGuid))
        {
            _Id = Id;
            _arrivalSpellId = arrivalSpellId;
            _arrivalSpellTargetGuid = arrivalSpellTargetGuid;
        }

        public override void Finalize(Unit unit)
        {
            if (_arrivalSpellId != 0)
                unit.CastSpell(Global.ObjAccessor.GetUnit(unit, _arrivalSpellTargetGuid), _arrivalSpellId, true);

            if (!unit.IsTypeId(TypeId.Unit))
                return;

            // Need restore previous movement since we have no proper states system
            if (unit.IsAlive() && !unit.HasUnitState(UnitState.Confused | UnitState.Fleeing))
            {
                Unit victim = unit.GetVictim();
                if (victim != null)
                    unit.GetMotionMaster().MoveChase(victim);
                else
                    unit.GetMotionMaster().Initialize();
            }

            if (unit.ToCreature().GetAI() != null)
                unit.ToCreature().GetAI().MovementInform(MovementGeneratorType.Effect, _Id);
        }

        public override bool Update(Unit owner, uint time_diff)
        {
            return !owner.moveSpline.Finalized();
        }

        public override void Initialize(Unit owner) { }

        public override void Reset(Unit owner) { }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Effect; }

        uint _Id;
        uint _arrivalSpellId;
        ObjectGuid _arrivalSpellTargetGuid;
    }
}
