// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

using static Global;

namespace Scripts.Shadowlands.Torghast
{
    [Script] // 300771 - Blade of the Lifetaker
    class spell_torghast_blade_of_the_lifetaker : AuraScript
    {
        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            PreventDefaultAction();

            procInfo.GetActor().CastSpell(procInfo.GetProcTarget(), aurEff.GetSpellEffectInfo().TriggerSpell, new CastSpellExtraArgs(aurEff)
                .AddSpellMod(SpellValueMod.BasePoint0, (int)GetTarget().CountPctFromMaxHealth(aurEff.GetAmount()))
                .SetTriggeringSpell(procInfo.GetProcSpell()));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 300796 - Touch of the Unseen
    class spell_torghast_touch_of_the_unseen : AuraScript
    {
        static uint SPELL_DOOR_OF_SHADOWS = 300728;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SPELL_DOOR_OF_SHADOWS);
        }

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().Id == SPELL_DOOR_OF_SHADOWS;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            PreventDefaultAction();

            procInfo.GetActor().CastSpell(procInfo.GetProcTarget(), aurEff.GetSpellEffectInfo().TriggerSpell, new CastSpellExtraArgs(aurEff)
                .AddSpellMod(SpellValueMod.BasePoint0, (int)GetTarget().CountPctFromMaxHealth(aurEff.GetAmount()))
                .SetTriggeringSpell(procInfo.GetProcSpell()));
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 305060 - Yel'Shir's Powerglove
    class spell_torghast_yelshirs_powerglove : SpellScript
    {
        void HandleEffect(uint effIndex)
        {
            SpellInfo triggeringSpell = GetTriggeringSpell();
            if (triggeringSpell != null)
            {
                Aura triggerAura = GetCaster().GetAura(triggeringSpell.Id);
                if (triggerAura != null)
                    SetEffectValue(GetEffectValue() * triggerAura.GetStackAmount());
            }
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 321706 - Dimensional Blade
    class spell_torghast_dimensional_blade : SpellScript
    {
        static uint SPELL_MAGE_BLINK = 1953;
        static uint SPELL_MAGE_SHIMMER = 212653;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SPELL_MAGE_BLINK, SPELL_MAGE_SHIMMER);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (!targets.Empty())
            {
                GetCaster().GetSpellHistory().RestoreCharge(SpellMgr.GetSpellInfo(SPELL_MAGE_BLINK, Difficulty.None).ChargeCategoryId);
                GetCaster().GetSpellHistory().RestoreCharge(SpellMgr.GetSpellInfo(SPELL_MAGE_SHIMMER, Difficulty.None).ChargeCategoryId);
            }

            // filter targets by entry here and not with conditions table because we need to know if any enemy was hit for charge restoration, not just mawrats
            targets.RemoveAll(target =>
            {
                switch (target.GetEntry())
                {
                    case 151353: // Mawrat
                    case 179458: // Protective Mawrat
                    case 154030: // Oddly Large Mawrat
                    case 169871: // Hungry Mawrat
                        return false;
                    default:
                        break;
                }
                return true;
            });
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
        }
    }
}
