// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class NearestAttackableNoTotemUnitInObjectRangeCheck : ICheck<Unit>
    {
        private readonly WorldObject _obj;
        private float _range;

        public NearestAttackableNoTotemUnitInObjectRangeCheck(WorldObject obj, float range)
        {
            _obj = obj;
            _range = range;
        }

        public bool Invoke(Unit u)
        {
            if (!u.IsAlive())
                return false;

            if (u.GetCreatureType() == CreatureType.NonCombatPet)
                return false;

            if (u.IsTypeId(TypeId.Unit) &&
                u.IsTotem())
                return false;

            if (!u.IsTargetableForAttack(false))
                return false;

            if (!_obj.IsWithinDist(u, _range) ||
                _obj.IsValidAttackTarget(u))
                return false;

            _range = _obj.GetDistance(u);

            return true;
        }
    }
}