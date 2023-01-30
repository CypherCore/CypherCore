// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Maps.Checks
{
    public class AnyAoETargetUnitInObjectRangeCheck : ICheck<Unit>
    {
        private readonly SpellInfo _spellInfo;
        private readonly Unit _funit;
        private readonly bool _incOwnRadius;
        private readonly bool _incTargetRadius;

        private readonly WorldObject _obj;
        private readonly float _range;

        public AnyAoETargetUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, SpellInfo spellInfo = null, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            _obj = obj;
            _funit = funit;
            _spellInfo = spellInfo;
            _range = range;
            _incOwnRadius = incOwnRadius;
            _incTargetRadius = incTargetRadius;
        }

        public bool Invoke(Unit u)
        {
            // Check contains checks for: live, uninteractible, non-attackable Flags, flight check and GM check, ignore totems
            if (u.IsTypeId(TypeId.Unit) &&
                u.IsTotem())
                return false;

            if (_spellInfo != null)
            {
                if (!u.IsPlayer())
                {
                    if (_spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                        return false;

                    if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) &&
                        u.IsControlledByPlayer())
                        return false;
                }
                else if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                {
                    return false;
                }
            }

            if (!_funit.IsValidAttackTarget(u, _spellInfo))
                return false;

            float searchRadius = _range;

            if (_incOwnRadius)
                searchRadius += _obj.GetCombatReach();

            if (_incTargetRadius)
                searchRadius += u.GetCombatReach();

            return u.IsInMap(_obj) && u.InSamePhase(_obj) && u.IsWithinDoubleVerticalCylinder(_obj, searchRadius, searchRadius);
        }
    }
}