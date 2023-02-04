// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
    // 5246 - Intimidating Shout
    [Script]
    internal class spell_warr_intimidating_shout : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitSrcAreaEnemy));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitSrcAreaEnemy));
        }

        private void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetExplTargetWorldObject());
        }
    }
}