// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;
using Game.Entities;

namespace Game.Spells
{
    public class WorldObjectSpellLineTargetCheck : WorldObjectSpellAreaTargetCheck
    {
        private readonly float _lineWidth;
        private readonly Position _position;

        public WorldObjectSpellLineTargetCheck(Position srcPosition, Position dstPosition, float lineWidth, float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(range, caster, caster, caster, spellInfo, selectionType, condList, objectType)
        {
            _position = srcPosition;
            _lineWidth = lineWidth;

            if (dstPosition != null &&
                srcPosition != dstPosition)
                _position.SetOrientation(srcPosition.GetAbsoluteAngle(dstPosition));
        }

        public override bool Invoke(WorldObject target)
        {
            if (!_position.HasInLine(target, target.GetCombatReach(), _lineWidth))
                return false;

            return base.Invoke(target);
        }
    }
}