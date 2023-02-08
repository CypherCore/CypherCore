// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 192090 - Thrash (Aura) - SPELL_DRUID_THRASH_BEAR_AURA
    internal class spell_dru_thrash_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.BloodFrenzyAura, DruidSpellIds.BloodFrenzyRageGain);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
        }

        private void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (caster != null)
                if (caster.HasAura(DruidSpellIds.BloodFrenzyAura))
                    caster.CastSpell(caster, DruidSpellIds.BloodFrenzyRageGain, true);
        }
    }
}