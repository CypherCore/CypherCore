// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;
using Game.Entities;

namespace Game.Spells
{
    public class WorldObjectSpellAreaTargetCheck : WorldObjectSpellTargetCheck
    {
        private readonly Position _position;
        private readonly float _range;

        public WorldObjectSpellAreaTargetCheck(float range, Position position, WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(caster, referer, spellInfo, selectionType, condList, objectType)
        {
            _range = range;
            _position = position;
        }

        public override bool Invoke(WorldObject target)
        {
            if (target.ToGameObject())
            {
                // isInRange including the dimension of the GO
                bool isInRange = target.ToGameObject().IsInRange(_position.GetPositionX(), _position.GetPositionY(), _position.GetPositionZ(), _range);

                if (!isInRange)
                    return false;
            }
            else
            {
                bool isInsideCylinder = target.IsWithinDist2d(_position, _range) && Math.Abs(target.GetPositionZ() - _position.GetPositionZ()) <= _range;

                if (!isInsideCylinder)
                    return false;
            }

            return base.Invoke(target);
        }
    }
}