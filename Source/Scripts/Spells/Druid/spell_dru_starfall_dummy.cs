// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 50286 - Starfall (Dummy)
    internal class spell_dru_starfall_dummy : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            targets.Resize(2);
        }

        private void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();

            // Shapeshifting into an animal form or mounting cancels the effect
            if (caster.GetCreatureType() == CreatureType.Beast ||
                caster.IsMounted())
            {
                SpellInfo spellInfo = GetTriggeringSpell();

                if (spellInfo != null)
                    caster.RemoveAurasDueToSpell(spellInfo.Id);

                return;
            }

            // Any effect which causes you to lose control of your character will supress the starfall effect.
            if (caster.HasUnitState(UnitState.Controlled))
                return;

            caster.CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }
    }
}