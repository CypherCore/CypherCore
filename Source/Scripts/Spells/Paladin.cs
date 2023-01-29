// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Paladin
{
    internal struct SpellIds
    {
        public const uint ArtOfWarTriggered = 231843;
        public const uint AshenHallow = 316958;
        public const uint AshenHallowDamage = 317221;
        public const uint AshenHallowHeal = 317223;
        public const uint AshenHallowAllowHammer = 330382;
        public const uint AvengersShield = 31935;
        public const uint AvengingWrath = 31884;
        public const uint BeaconOfLight = 53563;
        public const uint BeaconOfLightHeal = 53652;
        public const uint BladeOfJustice = 184575;
        public const uint BlindingLightEffect = 105421;
        public const uint ConcentractionAura = 19746;
        public const uint ConsecratedGroundPassive = 204054;
        public const uint ConsecratedGroundSlow = 204242;
        public const uint Consecration = 26573;
        public const uint ConsecrationDamage = 81297;
        public const uint ConsecrationProtectionAura = 188370;
        public const uint DivinePurposeTriggerred = 223819;
        public const uint DivineSteedHuman = 221883;
        public const uint DivineSteedDwarf = 276111;
        public const uint DivineSteedDraenei = 221887;
        public const uint DivineSteedDarkIronDwarf = 276112;
        public const uint DivineSteedBloodelf = 221886;
        public const uint DivineSteedTauren = 221885;
        public const uint DivineSteedZandalariTroll = 294133;
        public const uint DivineStormDamage = 224239;
        public const uint EnduringLight = 40471;
        public const uint EnduringJudgement = 40472;
        public const uint EyeForAnEyeTriggered = 205202;
        public const uint FinalStand = 204077;
        public const uint FinalStandEffect = 204079;
        public const uint Forbearance = 25771;
        public const uint GuardianOfAcientKings = 86659;
        public const uint HammerOfJustice = 853;
        public const uint HammerOfTheRighteousAoe = 88263;
        public const uint HandOfSacrifice = 6940;
        public const uint HolyMending = 64891;
        public const uint HolyPowerArmor = 28790;
        public const uint HolyPowerAttackPower = 28791;
        public const uint HolyPowerSpellPower = 28793;
        public const uint HolyPowerMp5 = 28795;
        public const uint HolyPrismAreaBeamVisual = 121551;
        public const uint HolyPrismTargetAlly = 114871;
        public const uint HolyPrismTargetEnemy = 114852;
        public const uint HolyPrismTargetBeamVisual = 114862;
        public const uint HolyShock = 20473;
        public const uint HolyShockDamage = 25912;
        public const uint HolyShockHealing = 25914;
        public const uint HolyLight = 82326;
        public const uint InfusionOfLightEnergize = 356717;
        public const uint ImmuneShieldMarker = 61988;
        public const uint ItemHealingTrance = 37706;
        public const uint JudgmentGainHolyPower = 220637;
        public const uint JudgmentHolyR3 = 231644;
        public const uint JudgmentHolyR3Debuff = 214222;
        public const uint JudgmentProtRetR3 = 315867;
        public const uint LightHammerCosmetic = 122257;
        public const uint LightHammerDamage = 114919;
        public const uint LightHammerHealing = 119952;
        public const uint LightHammerPeriodic = 114918;
        public const uint RighteousDefenseTaunt = 31790;
        public const uint RighteousVerdictAura = 267611;
        public const uint SealOfRighteousness = 25742;
        public const uint TemplarVerdictDamage = 224266;
        public const uint ZealAura = 269571;
    }

    internal struct SpellVisualKit
    {
        public const uint DivineStorm = 73892;
    }

    internal struct SpellVisual
    {
        public const uint HolyShockDamage = 83731;
        public const uint HolyShockDamageCrit = 83881;
        public const uint HolyShockHeal = 83732;
        public const uint HolyShockHealCrit = 83880;
    }

    [Script] // 267344 - Art of War
    internal class spell_pal_art_of_war : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ArtOfWarTriggered, SpellIds.BladeOfJustice);
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().GetSpellHistory().ResetCooldown(SpellIds.BladeOfJustice, true);
            GetTarget().CastSpell(GetTarget(), SpellIds.ArtOfWarTriggered, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 19042 - Ashen Hallow
    internal class areatrigger_pal_ashen_hallow : AreaTriggerAI
    {
        private TimeSpan _period;
        private TimeSpan _refreshTimer;

        public areatrigger_pal_ashen_hallow(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

        private void RefreshPeriod()
        {
            Unit caster = at.GetCaster();

            if (caster != null)
            {
                AuraEffect ashen = caster.GetAuraEffect(SpellIds.AshenHallow, 1);

                if (ashen != null)
                    _period = TimeSpan.FromMilliseconds(ashen.GetPeriod());
            }
        }

        public override void OnCreate()
        {
            RefreshPeriod();
            _refreshTimer = _period;
        }

        public override void OnUpdate(uint diff)
        {
            _refreshTimer -= TimeSpan.FromMilliseconds(diff);

            while (_refreshTimer <= TimeSpan.Zero)
            {
                Unit caster = at.GetCaster();

                if (caster != null)
                {
                    caster.CastSpell(at.GetPosition(), SpellIds.AshenHallowHeal, new CastSpellExtraArgs());
                    caster.CastSpell(at.GetPosition(), SpellIds.AshenHallowDamage, new CastSpellExtraArgs());
                }

                RefreshPeriod();

                _refreshTimer += _period;
            }
        }

        public override void OnUnitEnter(Unit unit)
        {
            if (unit.GetGUID() == at.GetCasterGuid())
                unit.CastSpell(unit, SpellIds.AshenHallowAllowHammer, true);
        }

        public override void OnUnitExit(Unit unit)
        {
            if (unit.GetGUID() == at.GetCasterGuid())
                unit.RemoveAura(SpellIds.AshenHallowAllowHammer);
        }
    }

    [Script] // 248033 - Awakening
    internal class spell_pal_awakening : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AvengingWrath) && spellInfo.GetEffects().Count >= 1;
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            TimeSpan extraDuration = TimeSpan.Zero;
            AuraEffect durationEffect = GetEffect(1);

            if (durationEffect != null)
                extraDuration = TimeSpan.FromSeconds(durationEffect.GetAmount());

            Aura avengingWrath = GetTarget().GetAura(SpellIds.AvengingWrath);

            if (avengingWrath != null)
            {
                avengingWrath.SetDuration((int)(avengingWrath.GetDuration() + extraDuration.TotalMilliseconds));
                avengingWrath.SetMaxDuration((int)(avengingWrath.GetMaxDuration() + extraDuration.TotalMilliseconds));
            }
            else
            {
                GetTarget()
                    .CastSpell(GetTarget(),
                               SpellIds.AvengingWrath,
                               new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.IgnoreSpellAndCategoryCD)
                                   .SetTriggeringSpell(eventInfo.GetProcSpell())
                                   .AddSpellMod(SpellValueMod.Duration, (int)extraDuration.TotalMilliseconds));
            }
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    // 1022 - Blessing of Protection
    [Script] // 204018 - Blessing of Spellwarding
    internal class spell_pal_blessing_of_protection : SpellScript, ICheckCastHander, IAfterHit
    {
        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target)
            {
                GetCaster().CastSpell(target, SpellIds.Forbearance, true);
                GetCaster().CastSpell(target, SpellIds.ImmuneShieldMarker, true);
            }
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Forbearance) //, SpellIds._PALADIN_IMMUNE_SHIELD_MARKER) // uncomment when we have serverside only spells
                   &&
                   spellInfo.ExcludeTargetAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        public SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();

            if (!target ||
                target.HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script] // 115750 - Blinding Light
    internal class spell_pal_blinding_light : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlindingLightEffect);
        }

        private void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();

            if (target)
                GetCaster().CastSpell(target, SpellIds.BlindingLightEffect, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 26573 - Consecration
    internal class spell_pal_consecration : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConsecrationDamage, SpellIds.ConsecrationProtectionAura, SpellIds.ConsecratedGroundPassive, SpellIds.ConsecratedGroundSlow);
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            AreaTrigger at = GetTarget().GetAreaTrigger(SpellIds.Consecration);

            if (at != null)
                GetTarget().CastSpell(at.GetPosition(), SpellIds.ConsecrationDamage, new CastSpellExtraArgs());
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    // 26573 - Consecration
    [Script] //  9228 - AreaTriggerId
    internal class areatrigger_pal_consecration : AreaTriggerAI
    {
        public areatrigger_pal_consecration(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();

            if (caster != null)
            {
                // 243597 is also being cast as protection, but CreateObject is not sent, either serverside areatrigger for this aura or unused - also no visual is seen
                if (unit == caster &&
                    caster.IsPlayer() &&
                    caster.ToPlayer().GetPrimarySpecialization() == (uint)TalentSpecialization.PaladinProtection)
                    caster.CastSpell(caster, SpellIds.ConsecrationProtectionAura);

                if (caster.IsValidAttackTarget(unit))
                    if (caster.HasAura(SpellIds.ConsecratedGroundPassive))
                        caster.CastSpell(unit, SpellIds.ConsecratedGroundSlow);
            }
        }

        public override void OnUnitExit(Unit unit)
        {
            if (at.GetCasterGuid() == unit.GetGUID())
                unit.RemoveAurasDueToSpell(SpellIds.ConsecrationProtectionAura, at.GetCasterGuid());

            unit.RemoveAurasDueToSpell(SpellIds.ConsecratedGroundSlow, at.GetCasterGuid());
        }
    }

    [Script] // 196926 - Crusader Might
    internal class spell_pal_crusader_might : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyShock);
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.HolyShock, TimeSpan.FromSeconds(aurEff.GetAmount()));
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 223817 - Divine Purpose
    internal class spell_pal_divine_purpose : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivinePurposeTriggerred);
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();

            if (!procSpell)
                return false;

            if (!procSpell.HasPowerTypeCost(PowerType.HolyPower))
                return false;

            return RandomHelper.randChance(aurEff.GetAmount());
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            eventInfo.GetActor()
                     .CastSpell(eventInfo.GetActor(),
                                SpellIds.DivinePurposeTriggerred,
                                new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(eventInfo.GetProcSpell()));
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 642 - Divine Shield
    internal class spell_pal_divine_shield : SpellScript, ICheckCastHander, IAfterCast
    {
        public void AfterCast()
        {
            Unit caster = GetCaster();

            if (caster.HasAura(SpellIds.FinalStand))
                caster.CastSpell((Unit)null, SpellIds.FinalStandEffect, true);


            caster.CastSpell(caster, SpellIds.Forbearance, true);
            caster.CastSpell(caster, SpellIds.ImmuneShieldMarker, true);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FinalStand, SpellIds.FinalStandEffect, SpellIds.Forbearance) //, SpellIds._PALADIN_IMMUNE_SHIELD_MARKER // uncomment when we have serverside only spells
                   &&
                   spellInfo.ExcludeCasterAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        public SpellCastResult CheckCast()
        {
            if (GetCaster().HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script] // 190784 - Divine Steed
    internal class spell_pal_divine_steed : SpellScript, IOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineSteedHuman, SpellIds.DivineSteedDwarf, SpellIds.DivineSteedDraenei, SpellIds.DivineSteedDarkIronDwarf, SpellIds.DivineSteedBloodelf, SpellIds.DivineSteedTauren, SpellIds.DivineSteedZandalariTroll);
        }

        public void OnCast()
        {
            Unit caster = GetCaster();

            uint spellId = SpellIds.DivineSteedHuman;

            switch (caster.GetRace())
            {
                case Race.Human:
                    spellId = SpellIds.DivineSteedHuman;

                    break;
                case Race.Dwarf:
                    spellId = SpellIds.DivineSteedDwarf;

                    break;
                case Race.Draenei:
                case Race.LightforgedDraenei:
                    spellId = SpellIds.DivineSteedDraenei;

                    break;
                case Race.DarkIronDwarf:
                    spellId = SpellIds.DivineSteedDarkIronDwarf;

                    break;
                case Race.BloodElf:
                    spellId = SpellIds.DivineSteedBloodelf;

                    break;
                case Race.Tauren:
                    spellId = SpellIds.DivineSteedTauren;

                    break;
                case Race.ZandalariTroll:
                    spellId = SpellIds.DivineSteedZandalariTroll;

                    break;
                default:
                    break;
            }

            caster.CastSpell(caster, spellId, true);
        }
    }

    [Script] // 224239 - Divine Storm
    internal class spell_pal_divine_storm : SpellScript, IOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.SpellVisualKitStorage.HasRecord(SpellVisualKit.DivineStorm);
        }

        public void OnCast()
        {
            GetCaster().SendPlaySpellVisualKit(SpellVisualKit.DivineStorm, 0, 0);
        }
    }

    [Script] // 205191 - Eye for an Eye
    internal class spell_pal_eye_for_an_eye : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EyeForAnEyeTriggered);
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell(eventInfo.GetActor(), SpellIds.EyeForAnEyeTriggered, true);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 234299 - Fist of Justice
    internal class spell_pal_fist_of_justice : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HammerOfJustice);
        }

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();

            if (procSpell != null)
                return procSpell.HasPowerTypeCost(PowerType.HolyPower);

            return false;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            int value = aurEff.GetAmount() / 10;

            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.HammerOfJustice, TimeSpan.FromSeconds(-value));
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
            Effects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // -85043 - Grand Crusader
    internal class spell_pal_grand_crusader : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AvengersShield);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return GetTarget().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().GetSpellHistory().ResetCooldown(SpellIds.AvengersShield, true);
        }
    }

    [Script] // 54968 - Glyph of Holy Light
    internal class spell_pal_glyph_of_holy_light : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        private void FilterTargets(List<WorldObject> targets)
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
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 53595 - Hammer of the Righteous
    internal class spell_pal_hammer_of_the_righteous : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConsecrationProtectionAura, SpellIds.HammerOfTheRighteousAoe);
        }

        private void HandleAoEHit(uint effIndex)
        {
            if (GetCaster().HasAura(SpellIds.ConsecrationProtectionAura))
                GetCaster().CastSpell(GetHitUnit(), SpellIds.HammerOfTheRighteousAoe);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleAoEHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 6940 - Hand of Sacrifice
    internal class spell_pal_hand_of_sacrifice : AuraScript, IHasAuraEffects
    {
        private int remainingAmount;
        public List<IAuraEffectHandler> Effects { get; } = new();

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

        private void Split(AuraEffect aurEff, DamageInfo dmgInfo, uint splitAmount)
        {
            remainingAmount -= (int)splitAmount;

            if (remainingAmount <= 0)
                GetTarget().RemoveAura(SpellIds.HandOfSacrifice);
        }

        public override void Register()
        {
            Effects.Add(new EffectSplitHandler(Split, 0));
        }
    }

    [Script] // 54149 - Infusion of Light
    internal class spell_pal_infusion_of_light : AuraScript, IHasAuraEffects
    {
        private static readonly FlagArray128 HolyLightSpellClassMask = new(0, 0, 0x400);
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.InfusionOfLightEnergize);
        }

        private bool CheckFlashOfLightProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcSpell() && eventInfo.GetProcSpell()._appliedMods.Contains(GetAura());
        }

        private bool CheckHolyLightProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Paladin, HolyLightSpellClassMask);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            eventInfo.GetActor()
                     .CastSpell(eventInfo.GetActor(),
                                SpellIds.InfusionOfLightEnergize,
                                new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(eventInfo.GetProcSpell()));
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckFlashOfLightProc, 0, AuraType.AddPctModifier));
            Effects.Add(new CheckEffectProcHandler(CheckFlashOfLightProc, 2, AuraType.AddFlatModifier));

            Effects.Add(new CheckEffectProcHandler(CheckHolyLightProc, 1, AuraType.Dummy));
            Effects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 327193 - Moment of Glory
    internal class spell_pal_moment_of_glory : SpellScript, IOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AvengersShield);
        }

        public void OnHit()
        {
            GetCaster().GetSpellHistory().ResetCooldown(SpellIds.AvengersShield);
        }
    }

    [Script] // 20271/275779/275773 - Judgement (Retribution/Protection/Holy)
    internal class spell_pal_judgment : SpellScript, IOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.JudgmentProtRetR3, SpellIds.JudgmentGainHolyPower, SpellIds.JudgmentHolyR3, SpellIds.JudgmentHolyR3Debuff);
        }

        public void OnHit()
        {
            Unit caster = GetCaster();

            if (caster.HasSpell(SpellIds.JudgmentProtRetR3))
                caster.CastSpell(caster, SpellIds.JudgmentGainHolyPower, GetSpell());

            if (caster.HasSpell(SpellIds.JudgmentHolyR3))
                caster.CastSpell(GetHitUnit(), SpellIds.JudgmentHolyR3Debuff, GetSpell());
        }
    }

    [Script] // 114165 - Holy Prism
    internal class spell_pal_holy_prism : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyPrismTargetAlly, SpellIds.HolyPrismTargetEnemy, SpellIds.HolyPrismTargetBeamVisual);
        }

        private void HandleDummy(uint effIndex)
        {
            if (GetCaster().IsFriendlyTo(GetHitUnit()))
                GetCaster().CastSpell(GetHitUnit(), SpellIds.HolyPrismTargetAlly, true);
            else
                GetCaster().CastSpell(GetHitUnit(), SpellIds.HolyPrismTargetEnemy, true);

            GetCaster().CastSpell(GetHitUnit(), SpellIds.HolyPrismTargetBeamVisual, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

    // 114852 - Holy Prism (Damage)
    [Script] // 114871 - Holy Prism (Heal)
    internal class spell_pal_holy_prism_selector : SpellScript, IHasSpellEffects
    {
        private List<WorldObject> _sharedTargets = new();
        private ObjectGuid _targetGUID;
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyPrismTargetAlly, SpellIds.HolyPrismTargetBeamVisual);
        }

        private void SaveTargetGuid(uint effIndex)
        {
            _targetGUID = GetHitUnit().GetGUID();
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            byte maxTargets = 5;

            if (targets.Count > maxTargets)
            {
                if (GetSpellInfo().Id == SpellIds.HolyPrismTargetAlly)
                {
                    targets.Sort(new HealthPctOrderPred());
                    targets.Resize(maxTargets);
                }
                else
                {
                    targets.RandomResize(maxTargets);
                }
            }

            _sharedTargets = targets;
        }

        private void ShareTargets(List<WorldObject> targets)
        {
            targets.Clear();
            targets.AddRange(_sharedTargets);
        }

        private void HandleScript(uint effIndex)
        {
            Unit initialTarget = Global.ObjAccessor.GetUnit(GetCaster(), _targetGUID);

            initialTarget?.CastSpell(GetHitUnit(), SpellIds.HolyPrismTargetBeamVisual, true);
        }

        public override void Register()
        {
            if (ScriptSpellId == SpellIds.HolyPrismTargetEnemy)
                SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaAlly));
            else if (ScriptSpellId == SpellIds.HolyPrismTargetAlly)
                SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));

            SpellEffects.Add(new ObjectAreaTargetSelectHandler(ShareTargets, 2, Targets.UnitDestAreaEntry));

            SpellEffects.Add(new EffectHandler(SaveTargetGuid, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleScript, 2, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 20473 - Holy Shock
    internal class spell_pal_holy_shock : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyShock, SpellIds.HolyShockHealing, SpellIds.HolyShockDamage);
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();

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
            {
                return SpellCastResult.BadTargets;
            }

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit unitTarget = GetHitUnit();

            if (unitTarget != null)
            {
                if (caster.IsFriendlyTo(unitTarget))
                    caster.CastSpell(unitTarget, SpellIds.HolyShockHealing, new CastSpellExtraArgs(GetSpell()));
                else
                    caster.CastSpell(unitTarget, SpellIds.HolyShockDamage, new CastSpellExtraArgs(GetSpell()));
            }
        }
    }

    [Script] // 25912 - Holy Shock
    internal class spell_pal_holy_shock_damage_visual : SpellScript, IAfterHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.SpellVisualStorage.HasRecord(SpellVisual.HolyShockDamage) && CliDB.SpellVisualStorage.HasRecord(SpellVisual.HolyShockDamageCrit);
        }

        public void AfterHit()
        {
            GetCaster().SendPlaySpellVisual(GetHitUnit(), IsHitCrit() ? SpellVisual.HolyShockDamageCrit : SpellVisual.HolyShockDamage, 0, 0, 0.0f, false);
        }
    }

    [Script] // 25914 - Holy Shock
    internal class spell_pal_holy_shock_heal_visual : SpellScript, IAfterHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.SpellVisualStorage.HasRecord(SpellVisual.HolyShockHeal) && CliDB.SpellVisualStorage.HasRecord(SpellVisual.HolyShockHealCrit);
        }

        public void AfterHit()
        {
            GetCaster().SendPlaySpellVisual(GetHitUnit(), IsHitCrit() ? SpellVisual.HolyShockHealCrit : SpellVisual.HolyShockHeal, 0, 0, 0.0f, false);
        }
    }

    [Script] // 37705 - Healing Discount
    internal class spell_pal_item_healing_discount : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemHealingTrance);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemHealingTrance, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 40470 - Paladin Tier 6 Trinket
    internal class spell_pal_item_t6_trinket : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EnduringLight, SpellIds.EnduringJudgement);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
            {
                return;
            }

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), spellId, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 633 - Lay on Hands
    internal class spell_pal_lay_on_hands : SpellScript, ICheckCastHander, IAfterHit
    {
        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target)
            {
                GetCaster().CastSpell(target, SpellIds.Forbearance, true);
                GetCaster().CastSpell(target, SpellIds.ImmuneShieldMarker, true);
            }
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Forbearance) //, SpellIds.ImmuneShieldMarker);
                   &&
                   spellInfo.ExcludeTargetAuraSpell == SpellIds.ImmuneShieldMarker;
        }

        public SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();

            if (!target ||
                target.HasAura(SpellIds.Forbearance))
                return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script] // 53651 - Beacon of Light
    internal class spell_pal_light_s_beacon : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BeaconOfLight, SpellIds.BeaconOfLightHeal);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (!eventInfo.GetActionTarget())
                return false;

            if (eventInfo.GetActionTarget().HasAura(SpellIds.BeaconOfLight, eventInfo.GetActor().GetGUID()))
                return false;

            return true;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();

            if (healInfo == null ||
                healInfo.GetHeal() == 0)
                return;

            uint heal = MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());

            var auras = GetCaster().GetSingleCastAuras();

            foreach (var eff in auras)
                if (eff.GetId() == SpellIds.BeaconOfLight)
                {
                    List<AuraApplication> applications = eff.GetApplicationList();

                    if (!applications.Empty())
                    {
                        CastSpellExtraArgs args = new(aurEff);
                        args.AddSpellMod(SpellValueMod.BasePoint0, (int)heal);
                        eventInfo.GetActor().CastSpell(applications[0].GetTarget(), SpellIds.BeaconOfLightHeal, args);
                    }

                    return;
                }
        }
    }

    [Script] // 122773 - Light's Hammer
    internal class spell_pal_light_hammer_init_summon : SpellScript, IAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LightHammerCosmetic, SpellIds.LightHammerPeriodic);
        }

        public void AfterCast()
        {
            foreach (var summonedObject in GetSpell().GetExecuteLogEffect(SpellEffectName.Summon).GenericVictimTargets)
            {
                Unit hammer = Global.ObjAccessor.GetUnit(GetCaster(), summonedObject.Victim);

                if (hammer != null)
                {
                    hammer.CastSpell(hammer,
                                     SpellIds.LightHammerCosmetic,
                                     new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));

                    hammer.CastSpell(hammer,
                                     SpellIds.LightHammerPeriodic,
                                     new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));
                }
            }
        }
    }

    [Script] // 114918 - Light's Hammer (Periodic)
    internal class spell_pal_light_hammer_periodic : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LightHammerHealing, SpellIds.LightHammerDamage);
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit lightHammer = GetTarget();
            Unit originalCaster = lightHammer.GetOwner();

            if (originalCaster != null)
            {
                originalCaster.CastSpell(lightHammer.GetPosition(), SpellIds.LightHammerDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
                originalCaster.CastSpell(lightHammer.GetPosition(), SpellIds.LightHammerHealing, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 204074 - Righteous Protector
    internal class spell_pal_righteous_protector : AuraScript, IHasAuraEffects
    {
        private SpellPowerCost _baseHolyPowerCost;
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AvengingWrath, SpellIds.GuardianOfAcientKings);
        }

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            SpellInfo procSpell = eventInfo.GetSpellInfo();

            if (procSpell != null)
                _baseHolyPowerCost = procSpell.CalcPowerCost(PowerType.HolyPower, false, eventInfo.GetActor(), eventInfo.GetSchoolMask());
            else
                _baseHolyPowerCost = null;

            return _baseHolyPowerCost != null;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            int value = aurEff.GetAmount() * 100 * _baseHolyPowerCost.Amount;

            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.AvengingWrath, TimeSpan.FromMilliseconds(-value));
            GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.GuardianOfAcientKings, TimeSpan.FromMilliseconds(-value));
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
            Effects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 267610 - Righteous Verdict
    internal class spell_pal_righteous_verdict : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

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
            Effects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 85804 - Selfless Healer
    internal class spell_pal_selfless_healer : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();

            if (procSpell != null)
                return procSpell.HasPowerTypeCost(PowerType.HolyPower);

            return false;
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckEffectProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 85256 - Templar's Verdict
    internal class spell_pal_templar_s_verdict : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

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
            SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

    [Script] // 28789 - Holy Power
    internal class spell_pal_t3_6p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyPowerArmor, SpellIds.HolyPowerAttackPower, SpellIds.HolyPowerSpellPower, SpellIds.HolyPowerMp5);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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

            caster.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 64890 - Item - Paladin T8 Holy 2P Bonus
    internal class spell_pal_t8_2p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HolyMending);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();

            if (healInfo == null ||
                healInfo.GetHeal() == 0)
                return;

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.HolyMending, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.HolyMending, args);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }

    [Script] // 269569 - Zeal
    internal class spell_pal_zeal : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ZealAura);
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.ZealAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, aurEff.GetAmount()));

            PreventDefaultAction();
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }
    }
}