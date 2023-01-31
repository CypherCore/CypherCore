// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(6358)] // 6358 - Seduction (Special Ability)
    internal class spell_warl_seduction : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GLYPH_OF_SUCCUBUS, SpellIds.PRIEST_SHADOW_WORD_DEATH);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            if (target)
                if (caster.GetOwner() &&
                    caster.GetOwner().HasAura(SpellIds.GLYPH_OF_SUCCUBUS))
                {
                    target.RemoveAurasByType(AuraType.PeriodicDamage, ObjectGuid.Empty, target.GetAura(SpellIds.PRIEST_SHADOW_WORD_DEATH)); // SW:D shall not be Removed.
                    target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
                    target.RemoveAurasByType(AuraType.PeriodicLeech);
                }
        }
    }
}