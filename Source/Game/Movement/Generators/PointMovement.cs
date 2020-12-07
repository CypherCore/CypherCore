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
using Game.Entities;

namespace Game.Movement
{
    public class PointMovementGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public PointMovementGenerator(uint id, float x, float y, float z, bool generatePath, float speed = 0.0f, Unit faceTarget = null, SpellEffectExtraData spellEffectExtraData = null)
        {
            _movementId = id;
            _destination = new Position(x, y, z);
            _speed = speed;
            _faceTarget = faceTarget;
            _spellEffectExtra = spellEffectExtraData;
            _generatePath = generatePath;
            _recalculateSpeed = false;
        }

        public override void DoInitialize(T owner)
        {
            if (_movementId == EventId.ChargePrepath)
            {
                owner.AddUnitState(UnitState.Roaming | UnitState.RoamingMove);
                return;
            }

            owner.AddUnitState(UnitState.Roaming);

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                _interrupt = true;
                owner.StopMoving();
                return;
            }

            owner.AddUnitState(UnitState.RoamingMove);

            MoveSplineInit init = new MoveSplineInit(owner);
            init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ(), _generatePath);
            if (_speed > 0.0f)
                init.SetVelocity(_speed);

            if (_faceTarget)
                init.SetFacing(_faceTarget);

            if (_spellEffectExtra != null)
                init.SetSpellEffectExtraData(_spellEffectExtra);

            init.Launch();

            // Call for creature group update
            Creature creature = owner.ToCreature();
            if (creature != null)
                creature.SignalFormationMovement(_destination, _movementId);
        }

        public override void DoReset(T owner)
        {
            owner.StopMoving();
            DoInitialize(owner);
        }

        public override bool DoUpdate(T owner, uint diff)
        {
            if (owner == null)
                return false;

            if (_movementId == EventId.ChargePrepath)
                return !owner.MoveSpline.Finalized();

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                _interrupt = true;
                owner.StopMoving();
                return true;
            }

            if ((_interrupt && owner.MoveSpline.Finalized()) || (_recalculateSpeed && !owner.MoveSpline.Finalized()))
            {
                _recalculateSpeed = false;
                _interrupt = false;

                owner.AddUnitState(UnitState.RoamingMove);

                MoveSplineInit init = new MoveSplineInit(owner);
                init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ(), _generatePath);
                if (_speed > 0.0f) // Default value for point motion type is 0.0, if 0.0 spline will use GetSpeed on unit
                    init.SetVelocity(_speed);
                init.Launch();

                // Call for creature group update
                Creature creature = owner.ToCreature();
                if (creature != null)
                    creature.SignalFormationMovement(_destination, _movementId);
            }

            return !owner.MoveSpline.Finalized();
        }

        public override void DoFinalize(T owner)
        {
            owner.ClearUnitState(UnitState.Roaming | UnitState.RoamingMove);

            if (owner.MoveSpline.Finalized())
                MovementInform(owner);
        }

        public void MovementInform(T owner)
        {
            if (owner.IsTypeId(TypeId.Unit))
            {
                if (owner.ToCreature().GetAI() != null)
                    owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Point, _movementId);
            }
        }

        public override void UnitSpeedChanged()
        {
            _recalculateSpeed = true;
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Point;
        }

        uint _movementId;
        Position _destination;
        float _speed;
        Unit _faceTarget;
        SpellEffectExtraData _spellEffectExtra;
        bool _generatePath;
        bool _recalculateSpeed;
        bool _interrupt;
    }

    public class AssistanceMovementGenerator : PointMovementGenerator<Creature>
    {
        public AssistanceMovementGenerator(float x, float y, float z) : base(0, x, y, z, true) { }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Assistance; }

        public override void Finalize(Unit owner)
        {
            owner.ClearUnitState(UnitState.Roaming);
            owner.StopMoving();
            owner.ToCreature().SetNoCallAssistance(false);
            owner.ToCreature().CallAssistance();
            if (owner.IsAlive())
                owner.GetMotionMaster().MoveSeekAssistanceDistract(WorldConfig.GetUIntValue(WorldCfg.CreatureFamilyAssistanceDelay));
        }
    }

    public class EffectMovementGenerator : IMovementGenerator
    {
        public EffectMovementGenerator(uint Id, uint arrivalSpellId = 0, ObjectGuid arrivalSpellTargetGuid = default)
        {
            _pointId = Id;
            _arrivalSpellId = arrivalSpellId;
            _arrivalSpellTargetGuid = arrivalSpellTargetGuid;
        }

        public override void Initialize(Unit owner) { }

        public override void Reset(Unit owner) { }

        public override bool Update(Unit owner, uint diff)
        {
            return !owner.MoveSpline.Finalized();
        }

        public override void Finalize(Unit owner)
        {
            MovementInform(owner);
        }

        public void MovementInform(Unit owner)
        {
            if (_arrivalSpellId != 0)
                owner.CastSpell(Global.ObjAccessor.GetUnit(owner, _arrivalSpellTargetGuid), _arrivalSpellId, true); 
            
            if (owner.ToCreature() && owner.ToCreature().GetAI() != null)
                owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Effect, _pointId);
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Effect; }

        uint _pointId;
        uint _arrivalSpellId;
        ObjectGuid _arrivalSpellTargetGuid;
    }
}
