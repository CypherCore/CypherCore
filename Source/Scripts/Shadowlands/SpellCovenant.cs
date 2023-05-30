// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Shadowlands
{
    [Script] // 323916 - Sulfuric Emission
    class spell_soulbind_sulfuric_emission : AuraScript
    {
        static uint SPELL_SULFURIC_EMISSION_COOLDOWN_AURA = 347684;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SPELL_SULFURIC_EMISSION_COOLDOWN_AURA);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            if (!procInfo.GetProcTarget().HealthBelowPct(aurEff.GetAmount()))
                return false;

            if (procInfo.GetProcTarget().HasAura(SPELL_SULFURIC_EMISSION_COOLDOWN_AURA))
                return false;

            return true;
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new CheckEffectProcHandler(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }
}
