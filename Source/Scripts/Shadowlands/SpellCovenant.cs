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
        uint SpellSulfuricEmissionCooldownAura = 347684;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellSulfuricEmissionCooldownAura);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            if (!procInfo.GetProcTarget().HealthBelowPct(aurEff.GetAmount()))
                return false;

            if (procInfo.GetProcTarget().HasAura(SpellSulfuricEmissionCooldownAura))
                return false;

            return true;
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 332753 - Superior Tactics
    class spell_soulbind_superior_tactics : AuraScript
    {
        uint SpellSuperiorTacticsCooldownAura = 332926;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellSuperiorTacticsCooldownAura);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            if (GetTarget().HasAura(SpellSuperiorTacticsCooldownAura))
                return false;

            // only dispels from friendly targets count
            if ((procInfo.GetHitMask() & ProcFlagsHit.Dispel) != 0 && !(procInfo.GetTypeMask() & new ProcFlagsInit(ProcFlags.DealHelpfulAbility | ProcFlags.DealHelpfulSpell | ProcFlags.DealHelpfulPeriodic)))
                return false;

            return true;
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }
}