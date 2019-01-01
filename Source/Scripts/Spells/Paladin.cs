/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Paladin
{
    struct SpellIds
    {
        public const uint AvengersShield = 31935;
        public const uint AuraMasteryImmune = 64364;
        public const uint BeaconOfLight = 53563;
        public const uint BeaconOfLightHeal = 53652;
        public const uint BlessingOfLowerCityDruid = 37878;
        public const uint BlessingOfLowerCityPaladin = 37879;
        public const uint BlessingOfLowerCityPriest = 37880;
        public const uint BlessingOfLowerCityShaman = 37881;
        public const uint BlindingLightEffect = 105421;
        public const uint ConcentractionAura = 19746;
        public const uint DivinePurposeProc = 90174;
        public const uint DivineSteedHuman = 221883;
        public const uint DivineSteedDraenei = 221887;
        public const uint DivineSteedBloodelf = 221886;
        public const uint DivineSteedTauren = 221885;
        public const uint DivineStormDamage = 224239;
        public const uint EnduringLight = 40471;
        public const uint EnduringJudgement = 40472;
        public const uint EyeForAnEyeRank1 = 9799;
        public const uint EyeForAnEyeDamage = 25997;
        public const uint FinalStand = 204077;
        public const uint FinalStandEffect = 204079;
        public const uint Forbearance = 25771;
        public const uint HandOfSacrifice = 6940;
        public const uint HolyMending = 64891;
        public const uint HolyPowerArmor = 28790;
        public const uint HolyPowerAttackPower = 28791;
        public const uint HolyPowerSpellPower = 28793;
        public const uint HolyPowerMp5 = 28795;
        public const uint HolyShockR1 = 20473;
        public const uint HolyShockR1Damage = 25912;
        public const uint HolyShockR1Healing = 25914;
        public const uint ImmuneShieldMarker = 61988;
        public const uint ItemHealingTrance = 37706;
        public const uint JudgementDamage = 54158;
        public const uint RighteousDefenseTaunt = 31790;
        public const uint SealOfCommand = 105361;
        public const uint SealOfRighteousness = 25742;
    }

    // 31821 - Aura Mastery
    [Script]
    class spell_pal_aura_mastery : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AuraMasteryImmune);
        }

        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.AuraMasteryImmune, true);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveOwnedAura(SpellIds.AuraMasteryImmune, GetCasterGUID());
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
        }
    }

    // 64364 - Aura Mastery Immune
    [Script]
    class spell_pal_aura_mastery_immune : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConcentractionAura);
        }

        bool CheckAreaTarget(Unit target)
        {
            return target.HasAura(SpellIds.ConcentractionAura, GetCasterGUID());
        }

        public override void Register()
        {
            DoCheckAreaTarget.Add(new CheckAreaTargetHandler(CheckAreaTarget));
        }
    }

    // 37877 - Blessing of Faith
    [Script]
    class spell_pal_blessing_of_faith : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfLowerCityDruid, SpellIds.BlessingOfLowerCityPaladin, SpellIds.BlessingOfLowerCityPriest, SpellIds.BlessingOfLowerCityShaman);
        }

        void HandleDummy(uint effIndex)
        {
            Unit unitTarget = GetHitUnit();
            if (unitTarget)
            {
                uint spell_id = 0;
                switch (unitTarget.GetClass())
                {
                    case Class.Druid:
                        spell_id = SpellIds.BlessingOfLowerCityDruid;
                        break;
                    case Class.Paladin:
                        spell_id = SpellIds.BlessingOfLowerCityPaladin;
                        break;
                    case Class.Priest:
                        spell_id = SpellIds.BlessingOfLowerCityPriest;
                        break;
                    case Class.Shaman:
                        spell_id = SpellIds.BlessingOfLowerCityShaman;
                        break;
                    default:
                        return; // ignore for non-healing classes
                }
                Unit caster = GetCaster();
                caster.CastSpell(caster, spell_id, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 1022 - Blessing of Protection
    [Script] // 204018 - Blessing of Spellwarding
    class spell_pal_blessing_of_protection : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Forbearance) //, SPELL_PALADIN_IMMUNE_SHIELD_MARKER) // uncomment when we have serverside only spells
                && spellInfo.ExcludeTargetAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        SpellCastResult CheckForbearance()
        {
            Unit target = GetExplTargetUnit();
            if (!target || target.HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }

        void TriggerForbearance()
        {
            Unit target = GetHitUnit();
            if (target)
            {
                GetCaster().CastSpell(target, SpellIds.Forbearance, true);
                GetCaster().CastSpell(target, SpellIds.ImmuneShieldMarker, true);
            }
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckForbearance));
            AfterHit.Add(new HitHandler(TriggerForbearance));
        }
    }

    [Script] // 115750 - Blinding Light
    class spell_pal_blinding_light : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlindingLightEffect);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.BlindingLightEffect, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script] // 642 - Divine Shield
    class spell_pal_divine_shield : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FinalStand, SpellIds.FinalStandEffect, SpellIds.Forbearance) //, SPELL_PALADIN_IMMUNE_SHIELD_MARKER // uncomment when we have serverside only spells
                    && spellInfo.ExcludeCasterAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        SpellCastResult CheckForbearance()
        {
            if (GetCaster().HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }

        void HandleFinalStand()
        {
            if (GetCaster().HasAura(SpellIds.FinalStand))
                GetCaster().CastSpell((Unit)null, SpellIds.FinalStandEffect, true);
        }

        void TriggerForbearance()
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.Forbearance, true);
            caster.CastSpell(caster, SpellIds.ImmuneShieldMarker, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckForbearance));
            AfterCast.Add(new CastHandler(HandleFinalStand));
            AfterCast.Add(new CastHandler(TriggerForbearance));
        }
    }

    [Script] // 190784 - Divine Steed
    class spell_pal_divine_steed : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineSteedHuman, SpellIds.DivineSteedDraenei, SpellIds.DivineSteedBloodelf, SpellIds.DivineSteedTauren);
        }

        void HandleOnCast()
        {
            Unit caster = GetCaster();

            uint spellId = SpellIds.DivineSteedHuman;
            switch (caster.GetRace())
            {
                case Race.Draenei:
                    spellId = SpellIds.DivineSteedDraenei;
                    break;
                case Race.BloodElf:
                    spellId = SpellIds.DivineSteedBloodelf;
                    break;
                case Race.Tauren:
                    spellId = SpellIds.DivineSteedTauren;
                    break;
                default:
                    break;
            }

            caster.CastSpell(caster, spellId, true);
        }

        public override void Register()
        {
            OnCast.Add(new CastHandler(HandleOnCast));
        }
    }

    // 224239 - Divine Storm
    [Script]
    class spell_pal_divine_storm : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineStormDamage);
        }

        void HandleOnCast()
        {
            Unit caster = GetCaster();
            caster.SendPlaySpellVisualKit(73892, 0, 0);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (!target)
                return;

            caster.CastSpell(target, SpellIds.DivineStormDamage, true);
        }

        public override void Register()
        {
            OnCast.Add(new CastHandler(HandleOnCast));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 33695 - Exorcism and Holy Wrath Damage
    [Script]
    class spell_pal_exorcism_and_holy_wrath_damage : AuraScript
    {
        void HandleEffectCalcSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
        {
            if (spellMod == null)
            {
                spellMod = new SpellModifier(aurEff.GetBase());
                spellMod.op = SpellModOp.Damage;
                spellMod.type = SpellModType.Flat;
                spellMod.spellId = GetId();
                spellMod.mask[1] = 0x200002;
            }

            spellMod.value = aurEff.GetAmount();
        }

        public override void Register()
        {
            DoEffectCalcSpellMod.Add(new EffectCalcSpellModHandler(HandleEffectCalcSpellMod, 0, AuraType.Dummy));
        }
    }

    // -9799 - Eye for an Eye
    [Script]
    class spell_pal_eye_for_an_eye : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EyeForAnEyeDamage);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int damage = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            GetTarget().CastCustomSpell(SpellIds.EyeForAnEyeDamage, SpellValueMod.BasePoint0, damage, eventInfo.GetProcTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, m_scriptSpellId == SpellIds.EyeForAnEyeRank1 ? AuraType.Dummy : AuraType.ProcTriggerSpell));
        }
    }

    // -75806 - Grand Crusader
    [Script]
    class spell_pal_grand_crusader : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AvengersShield);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return GetTarget().IsTypeId(TypeId.Player);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().GetSpellHistory().ResetCooldown(SpellIds.AvengersShield, true);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    // 54968 - Glyph of Holy Light
    [Script]
    class spell_pal_glyph_of_holy_light : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            uint maxTargets = GetSpellInfo().MaxAffectedTargets;

            if (targets.Count > maxTargets)
            {
                targets.Sort(new HealthPctOrderPred());
                targets.Resize(maxTargets);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    // 6940 - Hand of Sacrifice
    [Script]
    class spell_pal_hand_of_sacrifice : AuraScript
    {
        int remainingAmount;

        public override bool Load()
        {
            Unit caster = GetCaster();
            if (caster)
            {
                remainingAmount = (int)caster.GetMaxHealth();
                return true;
            }
            return false;
        }

        void Split(AuraEffect aurEff, DamageInfo dmgInfo, uint splitAmount)
        {
            remainingAmount -= (int)splitAmount;

            if (remainingAmount <= 0)
            {
                GetTarget().RemoveAura(SpellIds.HandOfSacrifice);
            }
        }

        public override void Register()
        {
            OnEffectSplit.Add(new EffectSplitHandler(Split, 0));
        }
    }

    // 20473 - Holy Shock
    [Script]
    class spell_pal_holy_shock : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            SpellInfo firstRankSpellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.HolyShockR1);
            if (firstRankSpellInfo == null)
                return false;

            // can't use other spell than holy shock due to spell_ranks dependency
            if (!spellInfo.IsRankOf(firstRankSpellInfo))
                return false;

            byte rank = spellInfo.GetRank();
            if (Global.SpellMgr.GetSpellWithRank(SpellIds.HolyShockR1Damage, rank, true) == 0 || Global.SpellMgr.GetSpellWithRank(SpellIds.HolyShockR1Healing, rank, true) == 0)
                return false;

            return true;
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit unitTarget = GetHitUnit();
            if (unitTarget)
            {
                byte rank = GetSpellInfo().GetRank();
                if (caster.IsFriendlyTo(unitTarget))
                    caster.CastSpell(unitTarget, Global.SpellMgr.GetSpellWithRank(SpellIds.HolyShockR1Healing, rank), true);
                else
                    caster.CastSpell(unitTarget, Global.SpellMgr.GetSpellWithRank(SpellIds.HolyShockR1Damage, rank), true);
            }
        }

        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();
            if (target)
            {
                if (!caster.IsFriendlyTo(target))
                {
                    if (!caster.IsValidAttackTarget(target))
                        return SpellCastResult.BadTargets;

                    if (!caster.isInFront(target))
                        return SpellCastResult.NotInfront;
                }
            }
            else
                return SpellCastResult.BadTargets;
            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 37705 - Healing Discount
    [Script]
    class spell_pal_item_healing_discount : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemHealingTrance);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemHealingTrance, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 40470 - Paladin Tier 6 Trinket
    class spell_pal_item_t6_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EnduringLight, SpellIds.EnduringJudgement);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null)
                return;

            uint spellId;
            int chance;

            // Holy Light & Flash of Light
            if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0xC0000000))
            {
                spellId = SpellIds.EnduringLight;
                chance = 15;
            }
            // Judgements
            else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00800000u))
            {
                spellId = SpellIds.EnduringJudgement;
                chance = 50;
            }
            else
                return;

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), spellId, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 20271 - Judgement
    // Updated 4.3.4
    [Script]
    class spell_pal_judgement : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.JudgementDamage);
        }

        void HandleScriptEffect(uint effIndex)
        {
            uint spellId = SpellIds.JudgementDamage;

            // some seals have SPELL_AURA_DUMMY in EFFECT_2
            var auras = GetCaster().GetAuraEffectsByType(AuraType.Dummy);
            foreach (var eff in auras)
            {
                if (eff.GetSpellInfo().GetSpellSpecific() == SpellSpecificType.Seal && eff.GetEffIndex() == 2)
                {
                    if (Global.SpellMgr.HasSpellInfo((uint)eff.GetAmount()))
                    {
                        spellId = (uint)eff.GetAmount();
                        break;
                    }
                }
            }

            GetCaster().CastSpell(GetHitUnit(), spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.Dummy));
        }
    }

    // 633 - Lay on Hands
    [Script]
    class spell_pal_lay_on_hands : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Forbearance)//, SpellIds.ImmuneShieldMarker);
                && spellInfo.ExcludeTargetAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        SpellCastResult CheckForbearance()
        {
            Unit target = GetExplTargetUnit();
            if (!target || target.HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }

        void TriggerForbearance()
        {
            Unit target = GetHitUnit();
            if (target)
            {
                GetCaster().CastSpell(target, SpellIds.Forbearance, true);
                GetCaster().CastSpell(target, SpellIds.ImmuneShieldMarker, true);
            }
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckForbearance));
            AfterHit.Add(new HitHandler(TriggerForbearance));
        }
    }

    // 53651 - Beacon of Light
    [Script]
    class spell_pal_light_s_beacon : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BeaconOfLight, SpellIds.BeaconOfLightHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (!eventInfo.GetActionTarget())
                return false;
            if (eventInfo.GetActionTarget().HasAura(SpellIds.BeaconOfLight, eventInfo.GetActor().GetGUID()))
                return false;
            return true;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            uint heal = MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());

            var auras = GetCaster().GetSingleCastAuras();
            foreach (var eff in auras)
            {
                if (eff.GetId() == SpellIds.BeaconOfLight)
                {
                    List<AuraApplication> applications = eff.GetApplicationList();
                    if (!applications.Empty())
                        eventInfo.GetActor().CastCustomSpell(SpellIds.BeaconOfLightHeal, SpellValueMod.BasePoint0, (int)heal, applications.First().GetTarget(), true);
                    return;
                }
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 31789 - Righteous Defense
    [Script]
    class spell_pal_righteous_defense : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RighteousDefenseTaunt);
        }

        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (!caster.IsTypeId(TypeId.Player))
                return SpellCastResult.DontReport;

            Unit target = GetExplTargetUnit();
            if (target)
            {
                if (!target.IsFriendlyTo(caster) || target.getAttackers().Empty())
                    return SpellCastResult.BadTargets;
            }
            else
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        void HandleTriggerSpellLaunch(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
        }

        void HandleTriggerSpellHit(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.RighteousDefenseTaunt, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            //! WORKAROUND
            //! target select will be executed in hitphase of effect 0
            //! so we must handle trigger spell also in hit phase (default execution in launch phase)
            //! see issue #3718
            OnEffectLaunchTarget.Add(new EffectHandler(HandleTriggerSpellLaunch, 1, SpellEffectName.TriggerSpell));
            OnEffectHitTarget.Add(new EffectHandler(HandleTriggerSpellHit, 1, SpellEffectName.TriggerSpell));
        }
    }

    // 85285 - Sacred Shield
    [Script]
    class spell_pal_sacred_shield : SpellScript
    {
        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (!caster.IsTypeId(TypeId.Player))
                return SpellCastResult.DontReport;

            if (!caster.HealthBelowPct(30))
                return SpellCastResult.CantDoThatRightNow;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    };

    // 85256 - Templar's Verdict
    // Updated 4.3.4
    [Script]
    class spell_pal_templar_s_verdict : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.DivinePurposeProc);
        }

        public override bool Load()
        {
            if (!GetCaster().IsTypeId(TypeId.Player))
                return false;

            if (GetCaster().ToPlayer().GetClass() != Class.Paladin)
                return false;

            return true;
        }

        void ChangeDamage(uint effIndex)
        {
            Unit caster = GetCaster();
            float damage = GetHitDamage();

            if (caster.HasAura(SpellIds.DivinePurposeProc))
                damage *= 7.5f;  // 7.5*30% = 225%
            else
            {
                switch (caster.GetPower(PowerType.HolyPower))
                {
                    case 0: // 1 Holy Power
                            //damage = damage;
                        break;
                    case 1: // 2 Holy Power
                        damage *= 3;    // 3*30 = 90%
                        break;
                    case 2: // 3 Holy Power
                        damage *= 7.5f;  // 7.5*30% = 225%
                        break;
                }
            }

            SetHitDamage((int)damage);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(ChangeDamage, 0, SpellEffectName.WeaponPercentDamage));
        }
    }

    // 20154, 21084 - Seal of Righteousness - melee proc dummy (addition ${$MWS*(0.022*$AP+0.044*$SPH)} damage)
    [Script]
    class spell_pal_seal_of_righteousness : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SealOfCommand);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            float ap = GetTarget().GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
            int holy = GetTarget().SpellBaseDamageBonusDone(SpellSchoolMask.Holy);
            holy += eventInfo.GetProcTarget().SpellBaseDamageBonusTaken(SpellSchoolMask.Holy);
            int bp = (int)((ap * 0.022f + 0.044f * holy) * GetTarget().GetBaseAttackTime(WeaponAttackType.BaseAttack) / 1000);
            GetTarget().CastCustomSpell(SpellIds.SealOfCommand, SpellValueMod.BasePoint0, bp, eventInfo.GetProcTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 28789 - Holy Power
    class spell_pal_t3_6p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyPowerArmor, SpellIds.HolyPowerAttackPower, SpellIds.HolyPowerSpellPower, SpellIds.HolyPowerMp5);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            uint spellId;
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            switch (target.GetClass())
            {
                case Class.Paladin:
                case Class.Priest:
                case Class.Shaman:
                case Class.Druid:
                    spellId = SpellIds.HolyPowerMp5;
                    break;
                case Class.Mage:
                case Class.Warlock:
                    spellId = SpellIds.HolyPowerSpellPower;
                    break;
                case Class.Hunter:
                case Class.Rogue:
                    spellId = SpellIds.HolyPowerAttackPower;
                    break;
                case Class.Warrior:
                    spellId = SpellIds.HolyPowerArmor;
                    break;
                default:
                    return;
            }

            caster.CastSpell(target, spellId, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 64890 Item - Paladin T8 Holy 2P Bonus
    class spell_pal_t8_2p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyMending);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.HolyMending);
            int amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks(Difficulty.None);
            // Add remaining ticks to damage done
            amount += (int)target.GetRemainingPeriodicAmount(caster.GetGUID(), SpellIds.HolyMending, AuraType.PeriodicHeal);

            caster.CastCustomSpell(SpellIds.HolyMending, SpellValueMod.BasePoint0, amount, target, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }
}
