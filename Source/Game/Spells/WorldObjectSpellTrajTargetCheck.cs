// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;
using Game.Entities;

namespace Game.Spells
{
    public class WorldObjectSpellTrajTargetCheck : WorldObjectSpellTargetCheck
    {
        private readonly Position _position;
        private readonly float _range;

        public WorldObjectSpellTrajTargetCheck(float range, Position position, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
            : base(caster, caster, spellInfo, selectionType, condList, objectType)
        {
            _range = range;
            _position = position;
        }

        public override bool Invoke(WorldObject target)
        {
            // return all targets on missile trajectory (0 - size of a missile)
            if (!Caster.HasInLine(target, target.GetCombatReach(), SpellConst.TrajectoryMissileSize))
                return false;

            if (target.GetExactDist2d(_position) > _range)
                return false;

            return base.Invoke(target);
        }
    }
}