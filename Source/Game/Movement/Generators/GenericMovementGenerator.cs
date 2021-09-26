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
using System;

namespace Game.Movement
{
    class GenericMovementGenerator : IMovementGenerator
    {
        MoveSplineInit _splineInit;
        MovementGeneratorType _type;
        uint _pointId;
        TimeTrackerSmall _duration;

        uint _arrivalSpellId;
        ObjectGuid _arrivalSpellTargetGuid;

        public GenericMovementGenerator(MoveSplineInit splineInit, MovementGeneratorType type, uint id, uint arrivalSpellId = 0, ObjectGuid arrivalSpellTargetGuid = default)
        {
            _splineInit = splineInit;
            _type = type;
            _pointId = id;
            _duration = new();
            _arrivalSpellId = arrivalSpellId;
            _arrivalSpellTargetGuid = arrivalSpellTargetGuid;
        }

        public void Initialize(Unit owner)
        {
            _duration.Reset(_splineInit.Launch());
        }

        public bool Update(Unit owner, uint diff)
        {
            _duration.Update((int)diff);
            if (_duration.Passed())
                return false;

            return !owner.MoveSpline.Finalized();
        }

        public void Finalize(Unit owner)
        {
            MovementInform(owner);
        }

        void MovementInform(Unit owner)
        {
            if (_arrivalSpellId != 0)
                owner.CastSpell(Global.ObjAccessor.GetUnit(owner, _arrivalSpellTargetGuid), _arrivalSpellId, true);

            Creature creature = owner.ToCreature();
            if (creature != null)
                if (creature.GetAI() != null)
                    creature.GetAI().MovementInform(_type, _pointId);
        }

        public void Reset(Unit owner) { }

        public MovementGeneratorType GetMovementGeneratorType() { return _type; }
    }
}
