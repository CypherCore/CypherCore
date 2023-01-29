// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Maps.Checks
{
    public class AnyDeadUnitSpellTargetInRangeCheck<T> : AnyDeadUnitObjectInRangeCheck<T> where T : WorldObject
    {
        private readonly WorldObjectSpellTargetCheck _check;

        public AnyDeadUnitSpellTargetInRangeCheck(WorldObject searchObj, float range, SpellInfo spellInfo, SpellTargetCheckTypes check, SpellTargetObjectTypes objectType) : base(searchObj, range)
        {
            _check = new WorldObjectSpellTargetCheck(searchObj, searchObj, spellInfo, check, null, objectType);
        }

        public override bool Invoke(T obj)
        {
            return base.Invoke(obj) && _check.Invoke(obj);
        }
    }
}