// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using static Global;

namespace Scripts.Spells.Druid
{
    struct SpellIds
    {
        public const uint Abundance = 207383;
        public const uint AbundanceEffect = 207640;
        public const uint BalanceT10Bonus = 70718;
        public const uint BalanceT10BonusProc = 70721;
        public const uint BearForm = 5487;
        public const uint BlessingOfCenarius = 40452;
        public const uint BlessingOfElune = 40446;
        public const uint BlessingOfRemulos = 40445;
        public const uint BlessingOfTheClaw = 28750;
        public const uint BloodFrenzyAura = 203962;
        public const uint BloodFrenzyRageGain = 203961;
        public const uint BramblesDamageAura = 213709;
        public const uint BramblesPassive = 203953;
        public const uint BramblesReflect = 203958;
        public const uint BristlingFurGainRage = 204031;
        public const uint CatForm = 768;
        public const uint Cultivation = 200390;
        public const uint CultivationHeal = 200389;
        public const uint CuriousBramblepatch = 330670;
        public const uint EarthwardenAura = 203975;
        public const uint EclipseDummy = 79577;
        public const uint EclipseLunarAura = 48518;
        public const uint EclipseLunarSpellCnt = 326055;
        public const uint EclipseOoc = 329910;
        public const uint EclipseSolarAura = 48517;
        public const uint EclipseSolarSpellCnt = 326053;
        public const uint EfflorescenceAura = 81262;
        public const uint EfflorescenceHeal = 81269;
        public const uint EmbraceOfTheDreamEffect = 392146;
        public const uint EmbraceOfTheDreamHeal = 392147;
        public const uint EntanglingRoots = 339;
        public const uint Exhilarate = 28742;
        public const uint FormAquaticPassive = 276012;
        public const uint FormAquatic = 1066;
        public const uint FormFlight = 33943;
        public const uint FormStag = 165961;
        public const uint FormSwiftFlight = 40120;
        public const uint FormsTrinketBear = 37340;
        public const uint FormsTrinketCat = 37341;
        public const uint FormsTrinketMoonkin = 37343;
        public const uint FormsTrinketNone = 37344;
        public const uint FormsTrinketTree = 37342;
        public const uint GalacticGuardianAura = 213708;
        public const uint Germination = 155675;
        public const uint GlyphOfStars = 114301;
        public const uint GlyphOfStarsVisual = 114302;
        public const uint GoreProc = 93622;
        public const uint Growl = 6795;
        public const uint IdolOfFeralShadows = 34241;
        public const uint IdolOfWorship = 60774;
        public const uint Incarnation = 117679;
        public const uint IncarnationKingOfTheJungle = 102543;
        public const uint IncarnationTreeOfLife = 33891;
        public const uint InnerPeace = 197073;
        public const uint Innervate = 29166;
        public const uint InnervateRank2 = 326228;
        public const uint Infusion = 37238;
        public const uint Languish = 71023;
        public const uint LifebloomFinalHeal = 33778;
        public const uint LunarInspirationOverride = 155627;
        public const uint Mangle = 33917;
        public const uint MassEntanglement = 102359;
        public const uint MoonfireDamage = 164812;
        public const uint PowerOfTheArchdruid = 392302;
        public const uint Prowl = 5215;
        public const uint Regrowth = 8936;
        public const uint Rejuvenation = 774;
        public const uint RejuvenationGermination = 155777;
        public const uint RejuvenationT10Proc = 70691;
        public const uint RestorationT102PBonus = 70658;
        public const uint SavageRoar = 62071;
        public const uint ShootingStars = 202342;
        public const uint ShootingStarsDamage = 202497;
        public const uint SkullBashCharge = 221514;
        public const uint SkullBashInterrupt = 93985;
        public const uint SpringBlossoms = 207385;
        public const uint SpringBlossomsHeal = 207386;
        public const uint SunfireDamage = 164815;
        public const uint SurvivalInstincts = 50322;
        public const uint TravelForm = 783;
        public const uint TreeOfLife = 33891;
        public const uint ThrashBear = 77758;
        public const uint ThrashBearAura = 192090;
        public const uint ThrashCat = 106830;
        public const uint YserasGiftHealParty = 145110;
        public const uint YserasGiftHealSelf = 145109;
    }

    // 774 - Rejuvenation
    [Script] // 155777 - Rejuvenation (Germination)
    class spell_dru_abundance : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Abundance, SpellIds.AbundanceEffect);
        }

        void HandleOnApplyOrReapply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster == null || !caster.HasAura(SpellIds.Abundance))
                return;

            // Note: caster only casts Abundance when first applied on the target, otherwise that given stack is refreshed.
            if (mode.HasFlag(AuraEffectHandleModes.Real))
                caster.CastSpell(caster, SpellIds.AbundanceEffect, new CastSpellExtraArgs().SetTriggeringAura(aurEff));
            else
            {
                Aura abundanceAura = caster.GetAura(SpellIds.AbundanceEffect);
                if (abundanceAura != null)
                    abundanceAura.RefreshDuration();
            }
        }

        void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster == null)
                return;

            Aura abundanceEffect = caster.GetAura(SpellIds.AbundanceEffect);
            if (abundanceEffect != null)
                abundanceEffect.ModStackAmount(-1);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleOnApplyOrReapply, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.RealOrReapplyMask));
            AfterEffectRemove.Add(new(HandleOnRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 22812 - Barkskin
    class spell_dru_barkskin : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BramblesPassive);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (target.HasAura(SpellIds.BramblesPassive))
                target.CastSpell(target, SpellIds.BramblesDamageAura, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 2, AuraType.PeriodicDummy));
        }
    }

    [Script] // 50334 - Berserk
    class spell_dru_berserk : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm, SpellIds.Mangle, SpellIds.ThrashBear, SpellIds.Growl);
        }

        void HandleOnCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }

        void ResetCooldowns()
        {
            GetCaster().GetSpellHistory().ResetCooldown(SpellIds.Mangle);
            GetCaster().GetSpellHistory().ResetCooldown(SpellIds.ThrashBear);
            GetCaster().GetSpellHistory().ResetCooldown(SpellIds.Growl);
        }

        public override void Register()
        {
            BeforeCast.Add(new(HandleOnCast));
            AfterCast.Add(new(ResetCooldowns));
        }
    }

    [Script] // 203953 - Brambles - SpellBramblesPassive
    class spell_dru_brambles : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BramblesReflect, SpellIds.BramblesDamageAura);
        }

        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            // Prevent Removal
            PreventDefaultAction();
        }

        void HandleAfterAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            // reflect back damage to the attacker
            Unit target = GetTarget();
            Unit attacker = dmgInfo.GetAttacker();
            if (attacker != null)
                target.CastSpell(attacker, SpellIds.BramblesReflect, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new(HandleAbsorb, 0));
            AfterEffectAbsorb.Add(new(HandleAfterAbsorb, 0));
        }
    }

    [Script] // 155835 - Bristling Fur
    class spell_dru_bristling_fur : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BristlingFurGainRage);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // BristlingFurRage = 100 * Damage / MaxHealth.
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                Unit target = GetTarget();
                uint rage = (uint)(target.GetMaxPower(PowerType.Rage) * (float)damageInfo.GetDamage() / (float)target.GetMaxHealth());
                if (rage > 0)
                    target.CastSpell(target, SpellIds.BristlingFurGainRage, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)rage));
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 768 - CatForm - SpellCatForm
    class spell_dru_cat_form : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Prowl);
        }

        void HandleAfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveOwnedAura(SpellIds.Prowl);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(HandleAfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
        }
    }

    // 774 - Rejuvenation
    [Script] // 155777 - Rejuventation (Germination)
    class spell_dru_cultivation : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CultivationHeal) && ValidateSpellEffect((SpellIds.Cultivation, 0));
        }

        void HandleOnTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster == null)
                return;

            Unit target = GetTarget();
            AuraEffect cultivationEffect = caster.GetAuraEffect(SpellIds.Cultivation, 0);
            if (cultivationEffect != null)
                if (target.HealthBelowPct(cultivationEffect.GetAmount()))
                    caster.CastSpell(target, SpellIds.CultivationHeal, new CastSpellExtraArgs().SetTriggeringAura(aurEff));
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleOnTick, 0, AuraType.PeriodicHeal));
        }
    }

    [Script] // 1850 - Dash
    class spell_dru_dash : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            // do not set speed if not in cat form
            if (GetUnitOwner().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                amount = 0;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModIncreaseSpeed));
        }
    }

    [Script] // 203974 - Earthwarden
    class spell_dru_earthwarden : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ThrashCat, SpellIds.ThrashBear, SpellIds.EarthwardenAura);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.EarthwardenAura, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    class spell_dru_eclipse_common
    {
        public static void SetSpellCount(Unit unitOwner, uint spellId, uint amount)
        {
            Aura aura = unitOwner.GetAura(spellId);
            if (aura == null)
                unitOwner.CastSpell(unitOwner, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, (int)amount));
            else
                aura.SetStackAmount((byte)amount);
        }
    }

    [Script] // 48517 Eclipse (Solar) + 48518 Eclipse (Lunar)
    class spell_dru_eclipse_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseDummy);
        }

        void HandleRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            AuraEffect auraEffDummy = GetTarget().GetAuraEffect(SpellIds.EclipseDummy, 0);
            if (auraEffDummy == null)
                return;

            uint spellId = GetSpellInfo().Id == SpellIds.EclipseSolarAura ? SpellIds.EclipseLunarSpellCnt : SpellIds.EclipseSolarSpellCnt;
            spell_dru_eclipse_common.SetSpellCount(GetTarget(), spellId, (uint)auraEffDummy.GetAmount());
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(HandleRemoved, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 79577 - Eclipse - SpellEclipseDummy
    class spell_dru_eclipse_dummy : AuraScript
    {
        class InitializeEclipseCountersEvent : BasicEvent
        {
            Unit _owner;
            uint _count;

            public InitializeEclipseCountersEvent(Unit owner, uint count)
            {
                _owner = owner;
                _count = count;
            }

            public override bool Execute(ulong e_time, uint p_time)
            {
                spell_dru_eclipse_common.SetSpellCount(_owner, SpellIds.EclipseSolarSpellCnt, _count);
                spell_dru_eclipse_common.SetSpellCount(_owner, SpellIds.EclipseLunarSpellCnt, _count);
                return true;
            }
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarAura, SpellIds.EclipseLunarAura);
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo != null)
            {
                if (spellInfo.SpellFamilyFlags & new FlagArray128(0x4, 0x0, 0x0, 0x0)) // Starfire
                    OnSpellCast(SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarAura);
                else if (spellInfo.SpellFamilyFlags & new FlagArray128(0x1, 0x0, 0x0, 0x0)) // Wrath
                    OnSpellCast(SpellIds.EclipseLunarSpellCnt, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarAura);
            }
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // counters are applied with a delay
            GetTarget().m_Events.AddEventAtOffset(new InitializeEclipseCountersEvent(GetTarget(), (uint)aurEff.GetAmount()), TimeSpan.FromSeconds(1));
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.EclipseSolarSpellCnt);
            GetTarget().RemoveAura(SpellIds.EclipseLunarSpellCnt);
        }

        void OnOwnerOutOfCombat(bool isNowInCombat)
        {
            if (!isNowInCombat)
                GetTarget().CastSpell(GetTarget(), SpellIds.EclipseOoc, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnProc.Add(new(HandleProc));
            OnEnterLeaveCombat.Add(new(OnOwnerOutOfCombat));
        }

        void OnSpellCast(uint cntSpellId, uint otherCntSpellId, uint eclipseAuraSpellId)
        {
            Unit target = GetTarget();
            Aura aura = target.GetAura(cntSpellId);
            if (aura != null)
            {
                uint remaining = aura.GetStackAmount();
                if (remaining == 0)
                    return;

                if (remaining > 1)
                    aura.SetStackAmount((byte)(remaining - 1));
                else
                {
                    // cast eclipse
                    target.CastSpell(target, eclipseAuraSpellId, TriggerCastFlags.FullMask);

                    // Remove stacks from other one as well
                    // reset remaining power on other spellId
                    target.RemoveAura(cntSpellId);
                    target.RemoveAura(otherCntSpellId);
                }
            }
        }
    }

    [Script] // 329910 - Eclipse out of combat - SpellEclipseOoc
    class spell_dru_eclipse_ooc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EclipseDummy, SpellIds.EclipseSolarSpellCnt, SpellIds.EclipseLunarSpellCnt);
        }

        void Tick(AuraEffect aurEff)
        {
            Unit owner = GetTarget();
            AuraEffect auraEffDummy = owner.GetAuraEffect(SpellIds.EclipseDummy, 0);
            if (auraEffDummy == null)
                return;

            if (!owner.IsInCombat() && (!owner.HasAura(SpellIds.EclipseSolarSpellCnt) || !owner.HasAura(SpellIds.EclipseLunarSpellCnt)))
            {
                // Restore 2 stacks to each spell when out of combat
                spell_dru_eclipse_common.SetSpellCount(owner, SpellIds.EclipseSolarSpellCnt, (uint)auraEffDummy.GetAmount());
                spell_dru_eclipse_common.SetSpellCount(owner, SpellIds.EclipseLunarSpellCnt, (uint)auraEffDummy.GetAmount());
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(Tick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 145205 - Efflorescence
    class spell_dru_efflorescence : SpellScript
    {
        void RemoveOldAreaTrigger(uint effIndex)
        {
            // if caster has any Efflorescence areatrigger, we Remove it.
            GetCaster().RemoveAreaTrigger(GetSpellInfo().Id);
        }

        void InitSummon()
        {
            foreach (var summonedObject in GetSpell().GetExecuteLogEffect(SpellEffectName.Summon).GenericVictimTargets)
            {
                Unit summon = ObjectAccessor.GetCreature(GetCaster(), summonedObject.Victim);
                if (summon != null)
                    summon.CastSpell(summon, SpellIds.EfflorescenceAura,
                        new CastSpellExtraArgs().SetTriggeringSpell(GetSpell()));
            }
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new(RemoveOldAreaTrigger, 2, SpellEffectName.CreateAreaTrigger));
            AfterCast.Add(new(InitSummon));
        }
    }

    [Script] // 81262 - Efflorescence (Dummy)
    class spell_dru_efflorescence_dummy : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EfflorescenceHeal);
        }

        void HandlePeriodicDummy(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            Unit summoner = target.GetOwner();
            if (summoner == null)
                return;

            summoner.CastSpell(target, SpellIds.EfflorescenceHeal, TriggerCastFlags.DontReportCastError);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodicDummy, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 81269 - Efflorescence (Heal)
    class spell_dru_efflorescence_heal : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            // Efflorescence became a smart heal which prioritizes players and their pets in their group before any unit outside their group.
            SelectRandomInjuredTargets(targets, 3, true, GetCaster());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 392124 - Embrace of the Dream
    class spell_dru_embrace_of_the_dream : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EmbraceOfTheDreamEffect)
                && ValidateSpellEffect((spellInfo.Id, 2));
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(GetEffectInfo(2).CalcValue(GetCaster()));
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.EmbraceOfTheDreamEffect,
                new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
                .SetTriggeringAura(aurEff)
                .SetTriggeringSpell(eventInfo.GetProcSpell()));
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 392146 - Embrace of the Dream (Selector)
    class spell_dru_embrace_of_the_dream_effect : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EmbraceOfTheDreamHeal, SpellIds.Regrowth, SpellIds.Rejuvenation, SpellIds.RejuvenationGermination);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(target => target.ToUnit()?.GetAuraEffect(AuraType.PeriodicHeal, SpellFamilyNames.Druid, new FlagArray128(0x50, 0, 0, 0), GetCaster().GetGUID()) == null);
        }

        void HandleEffect(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.EmbraceOfTheDreamHeal,
                new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
                .SetTriggeringSpell(GetSpell()));
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
            OnEffectHitTarget.Add(new(HandleEffect, 0, SpellEffectName.Dummy));
        }
    }

    // 339 - Entangling Roots
    [Script] // 102359 - Mass Entanglement
    class spell_dru_entangling_roots : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CuriousBramblepatch);
        }

        void HandleCuriousBramblepatch(ref WorldObject target)
        {
            if (!GetCaster().HasAura(SpellIds.CuriousBramblepatch))
                target = null;
        }

        void HandleCuriousBramblepatchAOE(List<WorldObject> targets)
        {
            if (!GetCaster().HasAura(SpellIds.CuriousBramblepatch))
                targets.Clear();
        }

        public override void Register()
        {
            OnObjectTargetSelect.Add(new(HandleCuriousBramblepatch, 1, Targets.UnitTargetEnemy));
            if (m_scriptSpellId == SpellIds.MassEntanglement)
                OnObjectAreaTargetSelect.Add(new(HandleCuriousBramblepatchAOE, 1, Targets.UnitDestAreaEnemy));
        }
    }

    [Script]
    class spell_dru_entangling_roots_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EntanglingRoots, SpellIds.MassEntanglement);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo != null)
            {
                // dont subtract dmg caused by roots from dmg required to break root
                if (spellInfo.Id == SpellIds.EntanglingRoots || spellInfo.Id == SpellIds.MassEntanglement)
                    return false;
            }
            return true;
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 22568 - Ferocious Bite
    class spell_dru_ferocious_bite : SpellScript
    {
        float _damageMultiplier;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.IncarnationKingOfTheJungle, 1));
        }

        void HandleHitTargetBurn(uint effIndex)
        {
            int newValue = (int)((float)(GetEffectValue()) * _damageMultiplier);
            SetEffectValue(newValue);
        }

        void HandleHitTargetDmg(uint effIndex)
        {
            int newValue = (int)((float)(GetHitDamage()) * (1.0f + _damageMultiplier));
            SetHitDamage(newValue);
        }

        void HandleLaunchTarget(uint effIndex)
        {
            Unit caster = GetCaster();

            int maxExtraConsumedPower = GetEffectValue();

            AuraEffect auraEffect = caster.GetAuraEffect(SpellIds.IncarnationKingOfTheJungle, 1);
            if (auraEffect != null)
            {
                float multiplier = 1.0f + (float)(auraEffect.GetAmount()) / 100.0f;
                maxExtraConsumedPower = (int)((float)(maxExtraConsumedPower) * multiplier);
                SetEffectValue(maxExtraConsumedPower);
            }

            _damageMultiplier = Math.Min(caster.GetPower(PowerType.Energy), maxExtraConsumedPower) / maxExtraConsumedPower;
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new(HandleLaunchTarget, 1, SpellEffectName.PowerBurn));
            OnEffectHitTarget.Add(new(HandleHitTargetBurn, 1, SpellEffectName.PowerBurn));
            OnEffectHitTarget.Add(new(HandleHitTargetDmg, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 37336 - Druid Forms Trinket
    class spell_dru_forms_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormsTrinketBear, SpellIds.FormsTrinketCat, SpellIds.FormsTrinketMoonkin, SpellIds.FormsTrinketNone, SpellIds.FormsTrinketTree);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActor();

            return target.GetShapeshiftForm() switch
            {
                ShapeShiftForm.BearForm or ShapeShiftForm.DireBearForm or ShapeShiftForm.CatForm or ShapeShiftForm.MoonkinForm or ShapeShiftForm.None or ShapeShiftForm.TreeOfLife => true,
                _ => false
            };
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit target = eventInfo.GetActor();

            uint triggerspell = target.GetShapeshiftForm() switch
            {
                ShapeShiftForm.BearForm or ShapeShiftForm.DireBearForm => SpellIds.FormsTrinketBear,
                ShapeShiftForm.CatForm => SpellIds.FormsTrinketCat,
                ShapeShiftForm.MoonkinForm => SpellIds.FormsTrinketMoonkin,
                ShapeShiftForm.None => SpellIds.FormsTrinketNone,
                ShapeShiftForm.TreeOfLife => SpellIds.FormsTrinketTree,
                _ => 0
            };

            if (triggerspell != 0)
                target.CastSpell(target, triggerspell, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 203964 - Galactic Guardian
    class spell_dru_galactic_guardian : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GalacticGuardianAura);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                Unit target = GetTarget();

                // free automatic moonfire on target
                target.CastSpell(damageInfo.GetVictim(), SpellIds.MoonfireDamage, true);

                // Cast aura
                target.CastSpell(damageInfo.GetVictim(), SpellIds.GalacticGuardianAura, true);
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 774 - Rejuvenation
    class spell_dru_germination : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Rejuvenation, SpellIds.Germination, SpellIds.RejuvenationGermination);
        }

        void PickRejuvenationVariant(ref WorldObject target)
        {
            Unit caster = GetCaster();

            // Germination talent.
            if (caster.HasAura(SpellIds.Germination))
            {
                Unit unitTarget = target.ToUnit();
                Aura rejuvenationAura = unitTarget.GetAura(SpellIds.Rejuvenation, caster.GetGUID());
                Aura germinationAura = unitTarget.GetAura(SpellIds.RejuvenationGermination, caster.GetGUID());

                // if target doesn't have Rejuventation, cast passes through.
                if (rejuvenationAura == null)
                    return;

                // if target has Rejuvenation, but not Germination, or Germination has lower remaining duration than Rejuvenation, then cast Germination
                if (germinationAura != null && germinationAura.GetDuration() >= rejuvenationAura.GetDuration())
                    return;

                caster.CastSpell(target, SpellIds.RejuvenationGermination,
                    new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError)
                    .SetTriggeringSpell(GetSpell()));

                // prevent aura refresh (but cast must still happen to consume mana)
                target = null;
            }
        }

        public override void Register()
        {
            OnObjectTargetSelect.Add(new(PickRejuvenationVariant, 0, Targets.UnitTargetAlly));
        }
    }

    [Script] // 24858 - Moonkin Form
    class spell_dru_glyph_of_stars : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfStars, SpellIds.GlyphOfStarsVisual);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            if (target.HasAura(SpellIds.GlyphOfStars))
                target.CastSpell(target, SpellIds.GlyphOfStarsVisual, true);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.GlyphOfStarsVisual);
        }

        public override void Register()
        {
            OnEffectApply.Add(new(OnApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 210706 - Gore
    class spell_dru_gore : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GoreProc, SpellIds.Mangle);
        }

        bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit owner = GetTarget();
            owner.CastSpell(owner, SpellIds.GoreProc);
            owner.GetSpellHistory().ResetCooldown(SpellIds.Mangle, true);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckEffectProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 99 - Incapacitating Roar
    class spell_dru_incapacitating_roar : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm);
        }

        void HandleOnCast()
        {
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new(HandleOnCast));
        }
    }

    [Script] // 29166 - Innervate
    class spell_dru_innervate : SpellScript
    {
        SpellCastResult CheckCast()
        {
            Player target = GetExplTargetUnit()?.ToPlayer();
            if (target == null)
                return SpellCastResult.BadTargets;

            var spec = target.GetPrimarySpecializationEntry();
            if (spec == null || spec.GetRole() != ChrSpecializationRole.Healer)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        void HandleRank2()
        {
            Unit caster = GetCaster();
            if (caster != GetHitUnit())
            {
                AuraEffect innervateR2 = caster.GetAuraEffect(SpellIds.InnervateRank2, 0);
                if (innervateR2 != null)
                    caster.CastSpell(caster, SpellIds.Innervate,
                        new CastSpellExtraArgs(TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress)
                        .SetTriggeringSpell(GetSpell())
                        .AddSpellMod(SpellValueMod.BasePoint0, -innervateR2.GetAmount()));
            }

        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
            OnHit.Add(new(HandleRank2));
        }
    }

    [Script] // 117679 - Incarnation (Passive)
    class spell_dru_incarnation : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IncarnationTreeOfLife);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.IncarnationTreeOfLife);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.IgnoreSpellCooldown, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 33891 - Incarnation: Tree of Life (Talent, Shapeshift)
    class spell_dru_incarnation_tree_of_life : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Incarnation);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (!GetTarget().HasAura(SpellIds.Incarnation))
                GetTarget().CastSpell(GetTarget(), SpellIds.Incarnation, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(AfterApply, 2, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 740 - Tranquility
    class spell_dru_inner_peace : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.InnerPeace)
            && ValidateSpellEffect((spellInfo.Id, 4))
            && spellInfo.GetEffect(3).IsAura(AuraType.MechanicImmunityMask)
            && spellInfo.GetEffect(4).IsAura(AuraType.ModDamagePercentTaken);
        }

        void PreventEffect(ref WorldObject target)
        {
            // Note: Inner Peace talent.
            if (!GetCaster().HasAura(SpellIds.InnerPeace))
                target = null;
        }

        public override void Register()
        {
            OnObjectTargetSelect.Add(new(PreventEffect, 3, Targets.UnitCaster));
            OnObjectTargetSelect.Add(new(PreventEffect, 4, Targets.UnitCaster));
        }
    }

    [Script] // 40442 - Druid Tier 6 Trinket
    class spell_dru_item_t6_trinket : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfRemulos, SpellIds.BlessingOfElune, SpellIds.BlessingOfCenarius);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null)
                return;

            uint spellId;
            int chance;

            // Starfire
            if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000004u))
            {
                spellId = SpellIds.BlessingOfRemulos;
                chance = 25;
            }
            // Rejuvenation
            else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000010u))
            {
                spellId = SpellIds.BlessingOfElune;
                chance = 25;
            }
            // Mangle (Bear) and Mangle (Cat)
            else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000440u))
            {
                spellId = SpellIds.BlessingOfCenarius;
                chance = 40;
            }
            else
                return;

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell(null, spellId, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 33763 - Lifebloom
    class spell_dru_lifebloom : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LifebloomFinalHeal);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Final heal only on duration end
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire || GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell)
                GetCaster().CastSpell(GetUnitOwner(), SpellIds.LifebloomFinalHeal, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 155580 - Lunar Inspiration
    class spell_dru_lunar_inspiration : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LunarInspirationOverride);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.LunarInspirationOverride, true);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.LunarInspirationOverride);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 392315 - Luxuriant Soil
    class spell_dru_luxuriant_soil : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Rejuvenation);
        }

        static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit rejuvCaster = GetTarget();

            // let's use the ProcSpell's max. range.
            float spellRange = eventInfo.GetSpellInfo().GetMaxRange();

            List<Unit> targetList = new();
            WorldObjectSpellAreaTargetCheck check = new(spellRange, rejuvCaster, rejuvCaster, rejuvCaster, eventInfo.GetSpellInfo(), SpellTargetCheckTypes.Ally, null, SpellTargetObjectTypes.Unit);
            UnitListSearcher searcher = new(rejuvCaster, targetList, check);
            Cell.VisitAllObjects(rejuvCaster, searcher, spellRange);

            if (targetList.Empty())
                return;

            rejuvCaster.CastSpell(targetList.SelectRandom(), SpellIds.Rejuvenation, TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnorePowerAndReagentCost | TriggerCastFlags.IgnoreCastInProgress);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] //  8921 - Moonfire
    class spell_dru_moonfire : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MoonfireDamage);
        }

        void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.MoonfireDamage, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleOnHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 16864 - Omen of Clarity
    class spell_dru_omen_of_clarity : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BalanceT10Bonus, SpellIds.BalanceT10BonusProc);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = GetTarget();
            if (target.HasAura(SpellIds.BalanceT10Bonus))
                target.CastSpell(null, SpellIds.BalanceT10BonusProc, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 392303 - Power of the Archdruid
    class spell_dru_power_of_the_archdruid : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.PowerOfTheArchdruid, 0));
        }

        static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetActor().HasAuraEffect(SpellIds.PowerOfTheArchdruid, 0);
        }

        static void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit druid = eventInfo.GetActor();
            Unit procTarget = eventInfo.GetActionTarget();

            // range is 0's BasePoints.
            float spellRange = aurEff.GetAmount();

            List<Unit> targetList = new();
            WorldObjectSpellAreaTargetCheck checker = new(spellRange, procTarget, druid, druid, eventInfo.GetSpellInfo(), SpellTargetCheckTypes.Ally, null, SpellTargetObjectTypes.Unit);
            UnitListSearcher searcher = new(procTarget, targetList, checker);
            Cell.VisitAllObjects(procTarget, searcher, spellRange);
            targetList.Remove(procTarget);

            if (targetList.Empty())
                return;

            AuraEffect powerOfTheArchdruidEffect = druid.GetAuraEffect(SpellIds.PowerOfTheArchdruid, 0);

            // max. targets is SpellPowerOfTheArchdruid's 0 BasePoints.
            int maxTargets = powerOfTheArchdruidEffect.GetAmount();

            targetList.RandomResize((uint)maxTargets);

            foreach (Unit chosenTarget in targetList)
                druid.CastSpell(chosenTarget, eventInfo.GetProcSpell().GetSpellInfo().Id, aurEff);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 5215 - Prowl
    class spell_dru_prowl : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CatForm);
        }

        void HandleOnCast()
        {
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.CatForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new(HandleOnCast));
        }
    }

    [Script] // 1079 - Rip
    class spell_dru_rip : AuraScript
    {
        public override bool Load()
        {
            Unit caster = GetCaster();
            return caster != null && caster.IsPlayer();
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;

            Unit caster = GetCaster();
            if (caster != null)
            {
                // 0.01 * $Ap * cp
                int cp = caster.GetPower(PowerType.ComboPoints);

                // Idol of Feral Shadows. Can't be handled as SpellMod due its dependency from CPs
                AuraEffect auraEffIdolOfFeralShadows = caster.GetAuraEffect(SpellIds.IdolOfFeralShadows, 0);
                if (auraEffIdolOfFeralShadows != null)
                    amount += cp * auraEffIdolOfFeralShadows.GetAmount();
                // Idol of Worship. Can't be handled as SpellMod due its dependency from CPs
                else
                {
                    AuraEffect auraEffIdolOfWorship = caster.GetAuraEffect(SpellIds.IdolOfWorship, 0);
                    if (auraEffIdolOfWorship != null)
                        amount += cp * auraEffIdolOfWorship.GetAmount();
                }

                amount += (int)MathFunctions.CalculatePct(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), cp);
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.PeriodicDamage));
        }
    }

    [Script] // 52610 - Savage Roar
    class spell_dru_savage_roar : SpellScript
    {
        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (caster.GetShapeshiftForm() != ShapeShiftForm.CatForm)
                return SpellCastResult.OnlyShapeshift;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
        }
    }

    [Script]
    class spell_dru_savage_roar_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SavageRoar);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.SavageRoar, new CastSpellExtraArgs(aurEff)
                .SetOriginalCaster(GetCasterGUID()));
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SavageRoar);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(AfterApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(AfterRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 164815 - Sunfire
    [Script] // 164812 - Moonfire
    class spell_dru_shooting_stars : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShootingStars, SpellIds.ShootingStarsDamage);
        }

        void OnTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                AuraEffect shootingStars = caster.GetAuraEffect(SpellIds.ShootingStars, 0);
                if (shootingStars != null)
                    if (RandomHelper.randChance(shootingStars.GetAmount()))
                        caster.CastSpell(GetTarget(), SpellIds.ShootingStarsDamage, true);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnTick, 1, AuraType.PeriodicDamage));
        }
    }

    [Script] // 106839 - Skull Bash
    class spell_dru_skull_bash : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SkullBashCharge, SpellIds.SkullBashInterrupt);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SkullBashCharge, true);
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SkullBashInterrupt, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 81269 - Efflorescence (Heal)
    class spell_dru_spring_blossoms : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SpringBlossoms, SpellIds.SpringBlossomsHeal);
        }

        void HandleOnHit(uint effIndex)
        {
            if (GetCaster().HasAura(SpellIds.SpringBlossoms))
                GetCaster().CastSpell(GetHitUnit(), SpellIds.SpringBlossomsHeal, TriggerCastFlags.DontReportCastError);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleOnHit, 0, SpellEffectName.Heal));
        }
    }

    [Script] // 106898 - Stampeding Roar
    class spell_dru_stampeding_roar : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearForm);
        }

        void HandleOnCast()
        {
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.BearForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new(HandleOnCast));
        }
    }

    [Script] // 50286 - Starfall (Dummy)
    class spell_dru_starfall_dummy : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RandomResize(2);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            // Shapeshifting into an animal form or mounting cancels the effect
            if (caster.GetCreatureType() == CreatureType.Beast || caster.IsMounted())
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

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaEnemy));
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 340694 - Sudden Ambush
    [Script] // 384667 - Sudden Ambush
    class spell_dru_sudden_ambush : AuraScript
    {
        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Spell procSpell = procInfo.GetProcSpell();
            if (procSpell == null)
                return false;

            int? comboPoints = procSpell.GetPowerTypeCostAmount(PowerType.ComboPoints);
            if (!comboPoints.HasValue)
                return false;

            return RandomHelper.randChance(comboPoints.Value * aurEff.GetAmount());
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] //  93402 - Sunfire
    class spell_dru_sunfire : SpellScript
    {
        void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SunfireDamage, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleOnHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 61336 - Survival Instincts
    class spell_dru_survival_instincts : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SurvivalInstincts);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.SurvivalInstincts, true);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SurvivalInstincts);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 40121 - Swift Flight Form (Passive)
    class spell_dru_swift_flight_passive : AuraScript
    {
        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player caster = GetCaster().ToPlayer();
            if (caster != null)
                if (caster.GetSkillValue(SkillType.Riding) >= 375)
                    amount = 310;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 1, AuraType.ModIncreaseVehicleFlightSpeed));
        }
    }

    [Script] // 28744 - Regrowth
    class spell_dru_t3_6p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlessingOfTheClaw);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.BlessingOfTheClaw, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.OverrideClassScripts));
        }
    }

    [Script] // 28719 - Healing Touch
    class spell_dru_t3_8p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhilarate);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Spell spell = eventInfo.GetProcSpell();
            if (spell == null)
                return;

            Unit caster = eventInfo.GetActor();
            List<SpellPowerCost> costs = spell.GetPowerCost();
            var m = costs.Find(cost => cost.Power == PowerType.Mana);
            if (m == null)
                return;

            int amount = MathFunctions.CalculatePct(m.Amount, aurEff.GetAmount());
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(null, SpellIds.Exhilarate, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 37288 - Mana Restore
    [Script] // 37295 - Mana Restore
    class spell_dru_t4_2p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Infusion);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell(null, SpellIds.Infusion, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 70723 - Item - Druid T10 Balance 4P Bonus
    class spell_dru_t10_balance_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Languish);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            SpellInfo spellInfo = SpellMgr.GetSpellInfo(SpellIds.Languish, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());

            Cypher.Assert(spellInfo.GetMaxTicks() > 0);
            amount /= (int)spellInfo.GetMaxTicks();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.Languish, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 70691 - Item T10 Restoration 4P Bonus
    class spell_dru_t10_restoration_4p_bonus : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (GetCaster().ToPlayer().GetGroup() == null)
            {
                targets.Clear();
                targets.Add(GetCaster());
            }
            else
            {
                targets.Remove(GetExplTargetUnit());
                List<Unit> tempTargets = new();
                foreach (var obj in targets)
                    if (obj.IsPlayer() && GetCaster().IsInRaidWith(obj.ToUnit()))
                        tempTargets.Add(obj.ToUnit());

                if (tempTargets.Empty())
                {
                    targets.Clear();
                    FinishCast(SpellCastResult.DontReport);
                    return;
                }

                Unit target = tempTargets.SelectRandom();
                targets.Clear();
                targets.Add(target);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 70664 - Druid T10 Restoration 4P Bonus (Rejuvenation)
    class spell_dru_t10_restoration_4p_bonus_dummy : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RejuvenationT10Proc);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null || spellInfo.Id == SpellIds.RejuvenationT10Proc)
                return false;

            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo == null || healInfo.GetHeal() == 0)
                return false;

            Player caster = eventInfo.GetActor().ToPlayer();
            if (caster == null)
                return false;

            return caster.GetGroup() != null || caster != eventInfo.GetProcTarget();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)eventInfo.GetHealInfo().GetHeal());
            eventInfo.GetActor().CastSpell(null, SpellIds.RejuvenationT10Proc, args);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 77758 - Thrash
    class spell_dru_thrash : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ThrashBearAura);
        }

        void HandleOnHitTarget(uint effIndex)
        {
            Unit hitUnit = GetHitUnit();
            if (hitUnit != null)
                GetCaster().CastSpell(hitUnit, SpellIds.ThrashBearAura, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 192090 - Thrash (Aura) - SpellThrashBearAura
    class spell_dru_thrash_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodFrenzyAura, SpellIds.BloodFrenzyRageGain);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
                if (caster.HasAura(SpellIds.BloodFrenzyAura))
                    caster.CastSpell(caster, SpellIds.BloodFrenzyRageGain, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicDamage));
        }
    }

    // 1066 - Aquatic Form
    // 33943 - Flight Form
    // 40120 - Swift Flight Form
    [Script] // 165961 - Stag Form
    class spell_dru_travel_form : AuraScript
    {
        uint triggeredSpellId;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormStag, SpellIds.FormAquaticPassive, SpellIds.FormAquatic, SpellIds.FormFlight, SpellIds.FormSwiftFlight);
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // If it stays 0, it Removes Travel Form dummy in AfterRemove.
            triggeredSpellId = 0;

            // We should only handle aura interrupts.
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Interrupt)
                return;

            // Check what form is appropriate
            triggeredSpellId = GetFormSpellId(GetTarget().ToPlayer(), GetCastDifficulty(), true);

            // If chosen form is current aura, just don't Remove it.
            if (triggeredSpellId == m_scriptSpellId)
                PreventDefaultAction();
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (triggeredSpellId == m_scriptSpellId)
                return;

            Player player = GetTarget().ToPlayer();

            if (triggeredSpellId != 0) // Apply new form
                player.CastSpell(player, triggeredSpellId, aurEff);
            else // If not set, simply Remove Travel Form dummy
                player.RemoveAura(SpellIds.TravelForm);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
        }

        public static uint GetFormSpellId(Player player, Difficulty difficulty, bool requiresOutdoor)
        {
            // Check what form is appropriate
            if (player.HasSpell(SpellIds.FormAquaticPassive) && player.IsInWater()) // Aquatic form
                return SpellIds.FormAquatic;

            if (!player.IsInCombat() && player.GetSkillValue(SkillType.Riding) >= 225 && CheckLocationForForm(player, difficulty, requiresOutdoor, SpellIds.FormFlight) == SpellCastResult.SpellCastOk) // Flight form
                return player.GetSkillValue(SkillType.Riding) >= 300 ? SpellIds.FormSwiftFlight : SpellIds.FormFlight;

            if (!player.IsInWater() && CheckLocationForForm(player, difficulty, requiresOutdoor, SpellIds.FormStag) == SpellCastResult.SpellCastOk) // Stag form
                return SpellIds.FormStag;

            return 0;
        }

        static SpellCastResult CheckLocationForForm(Player targetPlayer, Difficulty difficulty, bool requireOutdoors, uint spellId)
        {
            SpellInfo spellInfo = SpellMgr.GetSpellInfo(spellId, difficulty);

            if (requireOutdoors && !targetPlayer.IsOutdoors())
                return SpellCastResult.OnlyOutdoors;

            return spellInfo.CheckLocation(targetPlayer.GetMapId(), targetPlayer.GetZoneId(), targetPlayer.GetAreaId(), targetPlayer);
        }
    }

    [Script] // 783 - Travel Form (dummy)
    class spell_dru_travel_form_dummy : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormAquaticPassive, SpellIds.FormAquatic, SpellIds.FormStag);
        }

        SpellCastResult CheckCast()
        {
            Player player = GetCaster().ToPlayer();
            if (player == null)
                return SpellCastResult.CustomError;

            uint spellId = (player.HasSpell(SpellIds.FormAquaticPassive) && player.IsInWater()) ? SpellIds.FormAquatic : SpellIds.FormStag;

            SpellInfo spellInfo = SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());
            return spellInfo.CheckLocation(player.GetMapId(), player.GetZoneId(), player.GetAreaId(), player);
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
        }
    }

    [Script]
    class spell_dru_travel_form_dummy_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FormStag, SpellIds.FormAquatic, SpellIds.FormFlight, SpellIds.FormSwiftFlight);
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();

            // Outdoor check already passed - Travel Form (dummy) has SpellAttr0OutdoorsOnly attribute.
            uint triggeredSpellId = spell_dru_travel_form.GetFormSpellId(player, GetCastDifficulty(), false);

            player.CastSpell(player, triggeredSpellId, aurEff);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // No need to check Remove mode, it's safe for auras to Remove each other in AfterRemove hook.
            GetTarget().RemoveAura(SpellIds.FormStag);
            GetTarget().RemoveAura(SpellIds.FormAquatic);
            GetTarget().RemoveAura(SpellIds.FormFlight);
            GetTarget().RemoveAura(SpellIds.FormSwiftFlight);
        }

        public override void Register()
        {
            OnEffectApply.Add(new(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 252216 - Tiger Dash
    class spell_dru_tiger_dash : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CatForm);
        }

        void HandleOnCast()
        {
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.CatForm)
                GetCaster().CastSpell(GetCaster(), SpellIds.CatForm, true);
        }

        public override void Register()
        {
            BeforeCast.Add(new(HandleOnCast));
        }
    }

    [Script] // 252216 - Tiger Dash (Aura)
    class spell_dru_tiger_dash_AuraScript : AuraScript
    {
        void HandlePeriodic(AuraEffect aurEff)
        {
            AuraEffect effRunSpeed = GetEffect(0);
            if (effRunSpeed != null)
            {
                int reduction = aurEff.GetAmount();
                effRunSpeed.ChangeAmount(effRunSpeed.GetAmount() - reduction);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 1, AuraType.PeriodicDummy));
        }
    }

    [Script] // 48438 - Wild Growth
    class spell_dru_wild_growth : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1), (SpellIds.TreeOfLife, 2));
        }

        void FilterTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            int maxTargets = GetEffectInfo(1).CalcValue(caster);

            AuraEffect treeOfLife = caster.GetAuraEffect(SpellIds.TreeOfLife, 2);
            if (treeOfLife != null)
                maxTargets += treeOfLife.GetAmount();

            // Note: Wild Growth became a smart heal which prioritizes players and their pets in their group before any unit outside their group.
            SelectRandomInjuredTargets(targets, (uint)maxTargets, true, caster);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script]
    class spell_dru_wild_growth_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RestorationT102PBonus);
        }

        void HandleTickUpdate(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster == null)
                return;

            // calculate from base damage, not from aurEff.GetAmount() (already modified)
            float damage = caster.CalculateSpellDamage(GetUnitOwner(), aurEff.GetSpellEffectInfo());

            // Wild Growth = first tick gains a 6% bonus, reduced by 2% each tick
            float reduction = 2.0f;
            AuraEffect bonus = caster.GetAuraEffect(SpellIds.RestorationT102PBonus, 0);
            if (bonus != null)
                reduction -= MathFunctions.CalculatePct(reduction, bonus.GetAmount());
            reduction *= (aurEff.GetTickNumber() - 1);

            MathFunctions.AddPct(ref damage, 6.0f - reduction);
            aurEff.SetAmount((int)damage);
        }

        public override void Register()
        {
            OnEffectUpdatePeriodic.Add(new(HandleTickUpdate, 0, AuraType.PeriodicHeal));
        }
    }

    [Script] // 145108 - Ysera's Gift
    class spell_dru_yseras_gift : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.YserasGiftHealSelf, SpellIds.YserasGiftHealParty);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            int healAmount = (int)GetTarget().CountPctFromMaxHealth(aurEff.GetAmount());

            if (!GetTarget().IsFullHealth())
                GetTarget().CastSpell(GetTarget(), SpellIds.YserasGiftHealSelf, new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, healAmount));
            else
                GetTarget().CastSpell(GetTarget(), SpellIds.YserasGiftHealParty, new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, healAmount));
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 145110 - Ysera's Gift (heal)
    class spell_dru_yseras_gift_group_heal : SpellScript
    {
        void SelectTargets(List<WorldObject> targets)
        {
            SelectRandomInjuredTargets(targets, 1, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(SelectTargets, 0, Targets.UnitCasterAreaRaid));
        }
    }
}