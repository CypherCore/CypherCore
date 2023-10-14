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
    [Script] // 297721 - Subjugator's Manacles
    class spell_torghast_subjugators_manacles : AuraScript
    {
        List<ObjectGuid> _triggeredTargets = new();

        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            if (_triggeredTargets.Contains(procInfo.GetProcTarget().GetGUID()))
                return false;

            _triggeredTargets.Add(procInfo.GetProcTarget().GetGUID());
            return true;
        }

        void ResetMarkedTargets(bool isNowInCombat)
        {
            if (!isNowInCombat)
                _triggeredTargets.Clear();
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
            OnEnterLeaveCombat.Add(new(ResetMarkedTargets));
        }
    }

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
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 300796 - Touch of the Unseen
    class spell_torghast_touch_of_the_unseen : AuraScript
    {
        uint SpellDoorOfShadows = 300728;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellDoorOfShadows);
        }

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().Id == SpellDoorOfShadows;
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
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 305060 - Yel'Shir's Powerglove
    class spell_torghast_yelshirs_powerglove : SpellScript
    {
        void CalculateDamage(Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            SpellInfo triggeringSpell = GetTriggeringSpell();
            if (triggeringSpell != null)
            {
                Aura triggerAura = GetCaster().GetAura(triggeringSpell.Id);
                if (triggerAura != null)
                    pctMod *= triggerAura.GetStackAmount();
            }
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateDamage));
        }
    }

    [Script] // 321706 - Dimensional Blade
    class spell_torghast_dimensional_blade : SpellScript
    {
        uint SpellMageBlink = 1953;
        uint SpellMageShimmer = 212653;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellMageBlink, SpellMageShimmer);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (!targets.Empty())
            {
                GetCaster().GetSpellHistory().RestoreCharge(SpellMgr.GetSpellInfo(SpellMageBlink, Difficulty.None).ChargeCategoryId);
                GetCaster().GetSpellHistory().RestoreCharge(SpellMgr.GetSpellInfo(SpellMageShimmer, Difficulty.None).ChargeCategoryId);
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
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaEnemy));
        }
    }

    [Script] // 341324 - Uncontrolled Darkness
    class spell_torghast_uncontrolled_darkness : AuraScript
    {
        public int KillCounter;

        public override void Register()
        {
            // just a value holder, no hooks
        }
    }

    [Script] // 343174 - Uncontrolled Darkness
    class spell_torghast_uncontrolled_darkness_proc : AuraScript
    {
        uint SpellUncontrolledDarkness = 341324;
        uint SpellUncontrolledDarknessBuff = 341375;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellUncontrolledDarkness, 1))
            && ValidateSpellInfo(SpellUncontrolledDarknessBuff);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit caster = GetCaster();
            if (caster == null)
                return;

            Aura uncontrolledDarkness = caster.GetAura(SpellUncontrolledDarkness, caster.GetGUID());
            if (uncontrolledDarkness == null)
                return;

            var script = uncontrolledDarkness.GetScript<spell_torghast_uncontrolled_darkness>();
            if (script == null)
                return;

            if (caster.HasAura(SpellUncontrolledDarknessBuff))
            {
                if (++script.KillCounter >= uncontrolledDarkness.GetSpellInfo().GetEffect(1).CalcValue())
                {
                    caster.RemoveAura(SpellUncontrolledDarknessBuff);
                    script.KillCounter = 0;
                }
            }
            else
            {
                if (++script.KillCounter >= uncontrolledDarkness.GetSpellInfo().GetEffect(0).CalcValue())
                {
                    caster.CastSpell(caster, SpellUncontrolledDarknessBuff, true);
                    script.KillCounter = 0;
                }
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 342632 - Malevolent Stitching
    class spell_torghast_fleshcraft_shield_proc : AuraScript
    {
        uint SpellLabelFleshcraftBuff = 1103;

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().HasLabel(SpellLabelFleshcraftBuff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 342779 - Crystallized Dreams
    class spell_torghast_soulshape_proc : AuraScript
    {
        uint SpellLabelSoulshape = 1100;

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().HasLabel(SpellLabelSoulshape);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    // 342793 - Murmuring Shawl
    [Script] // 342799 - Gnarled Key
    class spell_torghast_door_of_shadows_proc : AuraScript
    {
        uint SpellLabelDoorOfShadows = 726;

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().HasLabel(SpellLabelDoorOfShadows);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 348908 - Ethereal Wildseed
    class spell_torghast_flicker_proc : AuraScript
    {
        uint SpellLabelFlicker = 1105;

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().HasLabel(SpellLabelFlicker);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 354569 - Potent Potion
    class spell_torghast_potent_potion_proc : AuraScript
    {
        uint SpellLabelRejuvenatingSiphonedEssence = 1290;

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().HasLabel(SpellLabelRejuvenatingSiphonedEssence);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 354706 - Spiritual Rejuvenation Potion
    class spell_torghast_potent_potion_calc : SpellScript
    {
        uint SpellLabelSpiritualRejuvenationPotion = 354568;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellLabelSpiritualRejuvenationPotion, 1));
        }

        void SetValue(uint effIndex)
        {
            SetEffectValue(SpellMgr.GetSpellInfo(SpellLabelSpiritualRejuvenationPotion, GetCastDifficulty()).GetEffect(effIndex)
                .CalcValue(GetCaster(), null, GetHitUnit()));
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new(SetValue, 0, SpellEffectName.Heal));
            OnEffectHitTarget.Add(new(SetValue, 1, SpellEffectName.Energize));
        }
    }

    [Script] // 373761 - Poisonous Spores
    class spell_torghast_poisonous_spores : AuraScript
    {
        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            PreventDefaultAction();

            Spell procSpell = procInfo.GetProcSpell();
            procInfo.GetActor().CastSpell(procSpell.m_targets.GetDst(), aurEff.GetSpellEffectInfo().TriggerSpell,
                new CastSpellExtraArgs(aurEff).SetTriggeringSpell(procSpell));
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }
}