// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;
using Game.Entities;

namespace Game.Spells
{
    public class WorldObjectSpellNearbyTargetCheck : WorldObjectSpellTargetCheck
    {
        private readonly Position _position;
        private float _range;

        public WorldObjectSpellNearbyTargetCheck(float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(caster, caster, spellInfo, selectionType, condList, objectType)
        {
            _range = range;
            _position = caster.GetPosition();
        }

        public override bool Invoke(WorldObject target)
        {
            float dist = target.GetDistance(_position);

            if (dist < _range &&
                base.Invoke(target))
            {
                _range = dist;

                return true;
            }

            return false;
        }
    }
}