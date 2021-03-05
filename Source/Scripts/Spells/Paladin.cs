/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.DataStorage;

namespace Scripts.Spells.Paladin
{
    internal struct SpellIds
    {
        public const uint AvengersShield = 31935;
        public const uint AvengingWrath = 31884;
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
        public const uint GuardianOfAcientKings = 86659;
        public const uint HammerOfJustice = 853;
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
        public const uint JudementGainHolyPower = 220637;
        public const uint JudgementProtRetR3 = 315867;
        public const uint RighteousDefenseTaunt = 31790;
        public const uint RighteousVerdictAura = 267611;
        public const uint SealOfRighteousness = 25742;
        public const uint TemplarVerdictDamage = 224266;
        public const uint ZealAura = 269571;
    }

    internal struct SpellVisualKits
    {
        public const uint DivineStorm = 73892;
    }

    // 37877 - Blessing of Faith
    [Script]
    internal class spell_pal_blessing_of_faith : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfLowerCityDruid, SpellIds.BlessingOfLowerCityPaladin, SpellIds.BlessingOfLowerCityPriest, SpellIds.BlessingOfLowerCityShaman);
        }

        private void HandleDummy(uint effIndex)
        {
            var unitTarget = GetHitUnit();
            if (unitTarget)
            {
                uint spell_id;
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
                var caster = GetCaster();
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
    internal class spell_pal_blessing_of_protection : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Forbearance) //, SpellIds._PALADIN_IMMUNE_SHIELD_MARKER) // uncomment when we have serverside only spells
                && spellInfo.ExcludeTargetAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        private SpellCastResult CheckForbearance()
        {
            var target = GetExplTargetUnit();
            if (!target || target.HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }

        private void TriggerForbearance()
        {
            var target = GetHitUnit();
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
    internal class spell_pal_blinding_light : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlindingLightEffect);
        }

        private void HandleDummy(uint effIndex)
        {
            var target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.BlindingLightEffect, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script] // 196926 - Crusader Might
    internal class spell_pal_crusader_might : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyShockR1);
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.HolyShockR1, aurEff.GetAmount());
        }

        public override void Register()
        {
            OnEffectProc .Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 642 - Divine Shield
    internal class spell_pal_divine_shield : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FinalStand, SpellIds.FinalStandEffect, SpellIds.Forbearance) //, SpellIds._PALADIN_IMMUNE_SHIELD_MARKER // uncomment when we have serverside only spells
                    && spellInfo.ExcludeCasterAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        private SpellCastResult CheckForbearance()
        {
            if (GetCaster().HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }

        private void HandleFinalStand()
        {
            if (GetCaster().HasAura(SpellIds.FinalStand))
                GetCaster().CastSpell((Unit)null, SpellIds.FinalStandEffect, true);
        }

        private void TriggerForbearance()
        {
            var caster = GetCaster();
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
    internal class spell_pal_divine_steed : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineSteedHuman, SpellIds.DivineSteedDraenei, SpellIds.DivineSteedBloodelf, SpellIds.DivineSteedTauren);
        }

        private void HandleOnCast()
        {
            var caster = GetCaster();

            var spellId = SpellIds.DivineSteedHuman;
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
    internal class spell_pal_divine_storm : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.SpellVisualKitStorage.HasRecord(SpellVisualKits.DivineStorm);
        }

        private void HandleOnCast()
        {
            GetCaster().SendPlaySpellVisualKit(SpellVisualKits.DivineStorm, 0, 0);
        }

        public override void Register()
        {
            OnCast.Add(new CastHandler(HandleOnCast));
        }
    }

    [Script] // 234299 - Fist of Justice
    internal class spell_pal_fist_of_justice : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HammerOfJustice);
        }

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            var procSpell = eventInfo.GetProcSpell();
            if (procSpell != null)
                return procSpell.HasPowerTypeCost(PowerType.HolyPower);

            return false;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            var value = aurEff.GetAmount() / 10;

            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.HammerOfJustice, -value);
        }

        public override void Register()
        {
            DoCheckEffectProc .Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
            OnEffectProc .Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }


    [Script] // -85043 - Grand Crusader
    internal class spell_pal_grand_crusader : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AvengersShield);
        }

        private bool CheckProc(ProcEventInfo eventInfo)
        {
            return GetTarget().IsTypeId(TypeId.Player);
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
    internal class spell_pal_glyph_of_holy_light : SpellScript
    {
        private void FilterTargets(List<WorldObject> targets)
        {
            var maxTargets = GetSpellInfo().MaxAffectedTargets;

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
    internal class spell_pal_hand_of_sacrifice : AuraScript
    {
        private int remainingAmount;

        public override bool Load()
        {
            var caster = GetCaster();
            if (caster)
            {
                remainingAmount = (int)caster.GetMaxHealth();
                return true;
            }
            return false;
        }

        private void Split(AuraEffect aurEff, DamageInfo dmgInfo, uint splitAmount)
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

    [Script] // 327193 - Moment of Glory
    internal class spell_pal_moment_of_glory : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AvengersShield);
        }

        private void HandleOnHit()
        {
            GetCaster().GetSpellHistory().ResetCooldown(SpellIds.AvengersShield);
        }

        public override void Register()
        {
            OnHit .Add(new HitHandler(HandleOnHit));
        }
    }

    [Script] // 20271/275779 - Judgement Ret/Prot
    internal class spell_pal_judgement : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.JudgementProtRetR3, SpellIds.JudementGainHolyPower);
        }

        private void HandleOnHit()
        {
            var caster = GetCaster();
            if (caster.HasSpell(SpellIds.JudgementProtRetR3))
                caster.CastSpell(caster, SpellIds.JudementGainHolyPower, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnHit .Add(new HitHandler(HandleOnHit));
        }
    }

    // 20473 - Holy Shock
    [Script]
    internal class spell_pal_holy_shock : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            var firstRankSpellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.HolyShockR1, Difficulty.None);
            if (firstRankSpellInfo == null)
                return false;

            // can't use other spell than holy shock due to spell_ranks dependency
            if (!spellInfo.IsRankOf(firstRankSpellInfo))
                return false;

            var rank = spellInfo.GetRank();
            if (Global.SpellMgr.GetSpellWithRank(SpellIds.HolyShockR1Damage, rank, true) == 0 || Global.SpellMgr.GetSpellWithRank(SpellIds.HolyShockR1Healing, rank, true) == 0)
                return false;

            return true;
        }

        private void HandleDummy(uint effIndex)
        {
            var caster = GetCaster();
            var unitTarget = GetHitUnit();
            if (unitTarget)
            {
                var rank = GetSpellInfo().GetRank();
                if (caster.IsFriendlyTo(unitTarget))
                    caster.CastSpell(unitTarget, Global.SpellMgr.GetSpellWithRank(SpellIds.HolyShockR1Healing, rank), true);
                else
                    caster.CastSpell(unitTarget, Global.SpellMgr.GetSpellWithRank(SpellIds.HolyShockR1Damage, rank), true);
            }
        }

        private SpellCastResult CheckCast()
        {
            var caster = GetCaster();
            var target = GetExplTargetUnit();
            if (target)
            {
                if (!caster.IsFriendlyTo(target))
                {
                    if (!caster.IsValidAttackTarget(target))
                        return SpellCastResult.BadTargets;

                    if (!caster.IsInFront(target))
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
    internal class spell_pal_item_healing_discount : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemHealingTrance);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
    internal class spell_pal_item_t6_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EnduringLight, SpellIds.EnduringJudgement);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            var spellInfo = eventInfo.GetSpellInfo();
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

    // 633 - Lay on Hands
    [Script]
    internal class spell_pal_lay_on_hands : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Forbearance)//, SpellIds.ImmuneShieldMarker);
                && spellInfo.ExcludeTargetAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        private SpellCastResult CheckForbearance()
        {
            var target = GetExplTargetUnit();
            if (!target || target.HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }

        private void TriggerForbearance()
        {
            var target = GetHitUnit();
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
    internal class spell_pal_light_s_beacon : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BeaconOfLight, SpellIds.BeaconOfLightHeal);
        }

        private bool CheckProc(ProcEventInfo eventInfo)
        {
            if (!eventInfo.GetActionTarget())
                return false;
            if (eventInfo.GetActionTarget().HasAura(SpellIds.BeaconOfLight, eventInfo.GetActor().GetGUID()))
                return false;
            return true;
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            var healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            var heal = MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());

            var auras = GetCaster().GetSingleCastAuras();
            foreach (var eff in auras)
            {
                if (eff.GetId() == SpellIds.BeaconOfLight)
                {
                    var applications = eff.GetApplicationList();
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

    [Script] // 204074 - Righteous Protector
    internal class spell_pal_righteous_protector : AuraScript
    {
        private SpellPowerCost _baseHolyPowerCost;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AvengingWrath, SpellIds.GuardianOfAcientKings);
        }

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            var procSpell = eventInfo.GetSpellInfo();
            if (procSpell != null)
                _baseHolyPowerCost = procSpell.CalcPowerCost(PowerType.HolyPower, false, eventInfo.GetActor(), eventInfo.GetSchoolMask());
            else
                _baseHolyPowerCost = null;

            return _baseHolyPowerCost != null;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            var value = aurEff.GetAmount() * 100 * _baseHolyPowerCost.Amount;

            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.AvengingWrath, -value);
            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.GuardianOfAcientKings, -value);
        }

        public override void Register()
        {
            DoCheckEffectProc .Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
            OnEffectProc .Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 267610 - Righteous Verdict
    internal class spell_pal_righteous_verdict : AuraScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.RighteousVerdictAura);
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            procInfo.GetActor().CastSpell(procInfo.GetActor(), SpellIds.RighteousVerdictAura, true);
        }

        public override void Register()
        {
            OnEffectProc .Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 85804 - Selfless Healer
    internal class spell_pal_selfless_healer : AuraScript
    {
        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            var procSpell = eventInfo.GetProcSpell();
            if (procSpell != null)
                return procSpell.HasPowerTypeCost(PowerType.HolyPower);

            return false;
        }

        public override void Register()
        {
            DoCheckEffectProc .Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 85256 - Templar's Verdict
    internal class spell_pal_templar_s_verdict : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.TemplarVerdictDamage);
        }

        private void HandleHitTarget(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.TemplarVerdictDamage, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 28789 - Holy Power
    internal class spell_pal_t3_6p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyPowerArmor, SpellIds.HolyPowerAttackPower, SpellIds.HolyPowerSpellPower, SpellIds.HolyPowerMp5);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            uint spellId;
            var caster = eventInfo.GetActor();
            var target = eventInfo.GetProcTarget();

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
    internal class spell_pal_t8_2p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyMending);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            var healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return;

            var caster = eventInfo.GetActor();
            var target = eventInfo.GetProcTarget();

            var spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.HolyMending, GetCastDifficulty());
            var amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();
            // Add remaining ticks to damage done
            amount += (int)target.GetRemainingPeriodicAmount(caster.GetGUID(), SpellIds.HolyMending, AuraType.PeriodicHeal);

            caster.CastCustomSpell(SpellIds.HolyMending, SpellValueMod.BasePoint0, amount, target, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 269569 - Zeal
    internal class spell_pal_zeal : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ZealAura);
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            var target = GetTarget();
            target.CastCustomSpell(SpellIds.ZealAura, SpellValueMod.AuraStack, aurEff.GetAmount(), target, true);

            PreventDefaultAction();
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell));
        }
    }
}
