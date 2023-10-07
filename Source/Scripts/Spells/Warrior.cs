// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Global;

namespace Scripts.Spells.Warrior
{
    struct SpellIds
    {
        public const uint BladestormPeriodicWhirlwind = 50622;
        public const uint BloodthirstHeal = 117313;
        public const uint Charge = 34846;
        public const uint ChargeEffect = 218104;
        public const uint ChargeEffectBlazingTrail = 198337;
        public const uint ChargePauseRageDecay = 109128;
        public const uint ChargeRootEffect = 105771;
        public const uint ChargeSlowEffect = 236027;
        public const uint ColossusSmash = 167105;
        public const uint ColossusSmashAura = 208086;
        public const uint CriticalThinkingEnergize = 392776;
        public const uint Execute = 20647;
        public const uint FueledByViolenceHeal = 383104;
        public const uint GlyphOfTheBlazingTrail = 123779;
        public const uint GlyphOfHeroicLeap = 159708;
        public const uint GlyphOfHeroicLeapBuff = 133278;
        public const uint HeroicLeapJump = 178368;
        public const uint IgnorePain = 190456;
        public const uint InForTheKill = 248621;
        public const uint InForTheKillHaste = 248622;
        public const uint ImpendingVictory = 202168;
        public const uint ImpendingVictoryHeal = 202166;
        public const uint ImprovedHeroicLeap = 157449;
        public const uint MortalStrike = 12294;
        public const uint MortalWounds = 213667;
        public const uint RallyingCry = 97463;
        public const uint ShieldBlockAura = 132404;
        public const uint ShieldChargeEffect = 385953;
        public const uint ShieldSlam = 23922;
        public const uint ShieldSlamMarker = 224324;
        public const uint Shockwave = 46968;
        public const uint ShockwaveStun = 132168;
        public const uint Stoicism = 70845;
        public const uint StormBoltStun = 132169;
        public const uint Strategist = 384041;
        public const uint SweepingStrikesExtraAttack1 = 12723;
        public const uint SweepingStrikesExtraAttack2 = 26654;
        public const uint Taunt = 355;
        public const uint TraumaEffect = 215537;
        public const uint Victorious = 32216;
        public const uint VictoryRushHeal = 118779;

        public const uint VisualBlazingCharge = 26423;
    }

    [Script] // 23881 - Bloodthirst
    class spell_warr_bloodthirst : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodthirstHeal);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.BloodthirstHeal, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 3, SpellEffectName.Dummy));
        }
    }

    [Script] // 384036 - Brutal Vitality
    class spell_warr_brutal_vitality : AuraScript
    {
        uint _damageAmount;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IgnorePain);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            _damageAmount += MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
        }

        void HandleDummyTick(AuraEffect aurEff)
        {
            if (_damageAmount == 0)
                return;

            AuraEffect ignorePainAura = GetTarget().GetAuraEffect(SpellIds.IgnorePain, 0);
            if (ignorePainAura != null)
                ignorePainAura.ChangeAmount((int)(ignorePainAura.GetAmount() + _damageAmount));

            _damageAmount = 0;
        }

        public override void Register()
        {
            AfterEffectProc.Add(new(HandleProc, 0, AuraType.PeriodicDummy));
            OnEffectPeriodic.Add(new(HandleDummyTick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 100 - Charge
    class spell_warr_charge : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChargeEffect, SpellIds.ChargeEffectBlazingTrail);
        }

        void HandleDummy(uint effIndex)
        {
            uint spellId = SpellIds.ChargeEffect;
            if (GetCaster().HasAura(SpellIds.GlyphOfTheBlazingTrail))
                spellId = SpellIds.ChargeEffectBlazingTrail;

            GetCaster().CastSpell(GetHitUnit(), spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 126661 - Warrior Charge Drop Fire Periodic
    class spell_warr_charge_drop_fire_periodic : AuraScript
    {
        void DropFireVisual(AuraEffect aurEff)
        {
            PreventDefaultAction();
            if (GetTarget().IsSplineEnabled())
            {
                for (int i = 0; i < 5; ++i)
                {
                    int timeOffset = (int)(6 * i * aurEff.GetPeriod() / 25);
                    Vector4 loc = GetTarget().MoveSpline.ComputePosition(timeOffset);
                    GetTarget().SendPlaySpellVisual(new Position(loc.X, loc.Y, loc.Z), SpellIds.VisualBlazingCharge, 0, 0, 1.0f, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(DropFireVisual, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    // 198337 - Charge Effect (dropping Blazing Trail)
    [Script] // 218104 - Charge Effect
    class spell_warr_charge_effect : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChargePauseRageDecay, SpellIds.ChargeRootEffect, SpellIds.ChargeSlowEffect);
        }

        void HandleCharge(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            caster.CastSpell(caster, SpellIds.ChargePauseRageDecay, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 0));
            caster.CastSpell(target, SpellIds.ChargeRootEffect, true);
            caster.CastSpell(target, SpellIds.ChargeSlowEffect, true);
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new(HandleCharge, 0, SpellEffectName.Charge));
        }
    }

    // 167105 - Colossus Smash
    [Script] // 262161 - Warbreaker
    class spell_warr_colossus_smash : SpellScript
    {
        bool _bonusHaste;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ColossusSmashAura, SpellIds.InForTheKill, SpellIds.InForTheKillHaste)
            && ValidateSpellEffect((SpellIds.InForTheKill, 2));
        }

        void HandleHit()
        {
            Unit target = GetHitUnit();
            Unit caster = GetCaster();

            GetCaster().CastSpell(GetHitUnit(), SpellIds.ColossusSmashAura, true);

            if (caster.HasAura(SpellIds.InForTheKill))
            {
                SpellInfo spellInfo = SpellMgr.GetSpellInfo(SpellIds.InForTheKill, Difficulty.None);
                if (spellInfo != null)
                {
                    if (target.HealthBelowPct(spellInfo.GetEffect(2).CalcValue(caster)))
                        _bonusHaste = true;
                }
            }
        }

        void HandleAfterCast()
        {
            Unit caster = GetCaster();
            SpellInfo spellInfo = SpellMgr.GetSpellInfo(SpellIds.InForTheKill, Difficulty.None);
            if (spellInfo == null)
                return;

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, spellInfo.GetEffect(0).CalcValue(caster));
            if (_bonusHaste)
                args.AddSpellMod(SpellValueMod.BasePoint0, spellInfo.GetEffect(1).CalcValue(caster));
            caster.CastSpell(caster, SpellIds.InForTheKillHaste, args);
        }

        public override void Register()
        {
            OnHit.Add(new(HandleHit));
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 389306 - Critical Thinking
    class spell_warr_critical_thinking : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CriticalThinkingEnergize);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            int? rageCost = eventInfo.GetProcSpell().GetPowerTypeCostAmount(PowerType.Rage);
            if (rageCost.HasValue)
                GetTarget().CastSpell(null, SpellIds.CriticalThinkingEnergize, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                    .AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(rageCost.Value, aurEff.GetAmount())));
        }

        public override void Register()
        {
            AfterEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // 236279 - Devastator
    class spell_warr_devastator : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1)) && ValidateSpellInfo(SpellIds.ShieldSlam, SpellIds.ShieldSlamMarker);
        }

        void OnProcSpell(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            if (GetTarget().GetSpellHistory().HasCooldown(SpellIds.ShieldSlam))
            {
                if (RandomHelper.randChance(GetEffectInfo(1).CalcValue()))
                {
                    GetTarget().GetSpellHistory().ResetCooldown(SpellIds.ShieldSlam, true);
                    GetTarget().CastSpell(GetTarget(), SpellIds.ShieldSlamMarker, TriggerCastFlags.IgnoreCastInProgress);
                }
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new(OnProcSpell, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 383103  - Fueled by Violence
    class spell_warr_fueled_by_violence : AuraScript
    {
        uint _nextHealAmount = 0;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FueledByViolenceHeal);
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            _nextHealAmount += MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), GetEffectInfo(0).CalcValue(GetTarget()));
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            if (_nextHealAmount == 0)
                return;

            Unit target = GetTarget();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)_nextHealAmount);

            target.CastSpell(target, SpellIds.FueledByViolenceHeal, args);
            _nextHealAmount = 0;
        }

        public override void Register()
        {
            OnProc.Add(new(HandleProc));
            OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 6544 - Heroic leap
    class spell_warr_heroic_leap : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HeroicLeapJump);
        }

        SpellCastResult CheckElevation()
        {
            WorldLocation dest = GetExplTargetDest();
            if (dest != null)
            {
                if (GetCaster().HasUnitMovementFlag(MovementFlag.Root))
                    return SpellCastResult.Rooted;

                if (GetCaster().GetMap().Instanceable())
                {
                    float range = GetSpellInfo().GetMaxRange(true, GetCaster()) * 1.5f;

                    PathGenerator generatedPath = new(GetCaster());
                    generatedPath.SetPathLengthLimit(range);

                    bool result = generatedPath.CalculatePath(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), false);
                    if (generatedPath.GetPathType().HasFlag(PathType.Short))
                        return SpellCastResult.OutOfRange;
                    else if (!result || generatedPath.GetPathType().HasFlag(PathType.NoPath))
                        return SpellCastResult.NoPath;
                }
                else if (dest.GetPositionZ() > GetCaster().GetPositionZ() + 4.0f)
                    return SpellCastResult.NoPath;

                return SpellCastResult.SpellCastOk;
            }

            return SpellCastResult.NoValidTargets;
        }

        void HandleDummy(uint effIndex)
        {
            WorldLocation dest = GetHitDest();
            if (dest != null)
                GetCaster().CastSpell(dest, SpellIds.HeroicLeapJump, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckElevation));
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // Heroic Leap (triggered by Heroic Leap (6544)) - 178368
    class spell_warr_heroic_leap_jump : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfHeroicLeap, SpellIds.GlyphOfHeroicLeapBuff, SpellIds.ImprovedHeroicLeap, SpellIds.Taunt);
        }

        void AfterJump(uint effIndex)
        {
            if (GetCaster().HasAura(SpellIds.GlyphOfHeroicLeap))
                GetCaster().CastSpell(GetCaster(), SpellIds.GlyphOfHeroicLeapBuff, true);
            if (GetCaster().HasAura(SpellIds.ImprovedHeroicLeap))
                GetCaster().GetSpellHistory().ResetCooldown(SpellIds.Taunt, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(AfterJump, 1, SpellEffectName.JumpDest));
        }
    }

    [Script] // 202168 - Impending Victory
    class spell_warr_impending_victory : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImpendingVictoryHeal);
        }

        void HandleAfterCast()
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.ImpendingVictoryHeal, true);
            caster.RemoveAurasDueToSpell(SpellIds.Victorious);
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 5246 - Intimidating Shout
    class spell_warr_intimidating_shout : SpellScript
    {
        void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetExplTargetWorldObject());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitSrcAreaEnemy));
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 2, Targets.UnitSrcAreaEnemy));
        }
    }

    [Script] // 70844 - Item - Warrior T10 Protection 4P Bonus
    class spell_warr_item_t10_prot_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Stoicism)
            && ValidateSpellEffect((spellInfo.Id, 1));
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit target = eventInfo.GetActionTarget();
            int bp0 = (int)MathFunctions.CalculatePct(target.GetMaxHealth(), GetEffectInfo(1).CalcValue());
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
            target.CastSpell(null, SpellIds.Stoicism, args);
        }

        public override void Register()
        {
            OnProc.Add(new(HandleProc));
        }
    }

    [Script] // 12294 - Mortal Strike 7.1.5
    class spell_warr_mortal_strike : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MortalWounds);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
                GetCaster().CastSpell(target, SpellIds.MortalWounds, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 97462 - Rallying Cry
    class spell_warr_rallying_cry : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RallyingCry);
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleScript(uint effIndex)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)GetHitUnit().CountPctFromMaxHealth(GetEffectValue()));

            GetCaster().CastSpell(GetHitUnit(), SpellIds.RallyingCry, args);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 2565 - Shield Block
    class spell_warr_shield_block : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShieldBlockAura);
        }

        void HandleHitTarget(uint effIndex)
        {
            GetCaster().CastSpell(null, SpellIds.ShieldBlockAura, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHitTarget, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 385952 - Shield Charge
    class spell_warr_shield_charge : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShieldChargeEffect);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ShieldChargeEffect, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 46968 - Shockwave
    class spell_warr_shockwave : SpellScript
    {
        uint _targetCount;

        public override bool Validate(SpellInfo spellInfo)
        {
            return !ValidateSpellInfo(SpellIds.Shockwave, SpellIds.ShockwaveStun)
            && ValidateSpellEffect((spellInfo.Id, 3));
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleStun(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ShockwaveStun, true);
            ++_targetCount;
        }

        // Cooldown reduced by 20 sec if it strikes at least 3 targets.
        void HandleAfterCast()
        {
            if (_targetCount >= (uint)GetEffectInfo(0).CalcValue())
                GetCaster().ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(-GetEffectInfo(3).CalcValue()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleStun, 0, SpellEffectName.Dummy));
            AfterCast.Add(new(HandleAfterCast));
        }
    }

    [Script] // 107570 - Storm Bolt
    class spell_warr_storm_bolt : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StormBoltStun);
        }

        void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.StormBoltStun, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleOnHit, 1, SpellEffectName.Dummy));
        }
    }

    [Script] // 384041 - Strategist
    class spell_warr_strategist : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShieldSlam, SpellIds.ShieldSlamMarker)
                && ValidateSpellEffect((SpellIds.Strategist, 0));
        }

        static bool CheckProc(AuraEffect aurEff, ProcEventInfo procEvent)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        void HandleCooldown(AuraEffect aurEff, ProcEventInfo procEvent)
        {
            Unit caster = GetTarget();
            caster.GetSpellHistory().ResetCooldown(SpellIds.ShieldSlam, true);
            caster.CastSpell(caster, SpellIds.ShieldSlamMarker, TriggerCastFlags.IgnoreCastInProgress);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleCooldown, 0, AuraType.Dummy));
        }
    }

    [Script] // 52437 - Sudden Death
    class spell_warr_sudden_death : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ColossusSmash);
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Remove cooldown on Colossus Smash
            Player player = GetTarget().ToPlayer();
            if (player != null)
                player.GetSpellHistory().ResetCooldown(SpellIds.ColossusSmash, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real)); // correct?
        }
    }

    [Script] // 12328, 18765, 35429 - Sweeping Strikes
    class spell_warr_sweeping_strikes : AuraScript
    {
        Unit _procTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SweepingStrikesExtraAttack1, SpellIds.SweepingStrikesExtraAttack2);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            _procTarget = eventInfo.GetActor().SelectNearbyTarget(eventInfo.GetProcTarget());
            return _procTarget != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                SpellInfo spellInfo = damageInfo.GetSpellInfo();
                if (spellInfo != null && (spellInfo.Id == SpellIds.BladestormPeriodicWhirlwind || (spellInfo.Id == SpellIds.Execute && !_procTarget.HasAuraState(AuraStateType.Wounded20Percent))))
                {
                    // If triggered by Execute (while target is not under 20% hp) or Bladestorm deals normalized weapon damage
                    GetTarget().CastSpell(_procTarget, SpellIds.SweepingStrikesExtraAttack2, aurEff);
                }
                else
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage());
                    GetTarget().CastSpell(_procTarget, SpellIds.SweepingStrikesExtraAttack1, args);
                }
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 215538 - Trauma
    class spell_warr_trauma : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TraumaEffect);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit target = eventInfo.GetActionTarget();
            //Get 25% of damage from the spell casted (Slam & Whirlwind) plus Remaining Damage from Aura
            int damage = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()) / SpellMgr.GetSpellInfo(SpellIds.TraumaEffect, GetCastDifficulty()).GetMaxTicks());
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, damage);
            GetCaster().CastSpell(target, SpellIds.TraumaEffect, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 28845 - Cheat Death
    class spell_warr_t3_prot_8p_bonus : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetActionTarget().HealthBelowPct(20))
                return true;

            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null && damageInfo.GetDamage() != 0)
                if (GetTarget().HealthBelowPctDamaged(20, damageInfo.GetDamage()))
                    return true;

            return false;
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script] // 32215 - Victorious State
    class spell_warr_victorious_state : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImpendingVictory);
        }

        void HandleOnProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            if (procInfo.GetActor().IsPlayer() && procInfo.GetActor().ToPlayer().GetPrimarySpecialization() == ChrSpecialization.WarriorFury)
                PreventDefaultAction();

            procInfo.GetActor().GetSpellHistory().ResetCooldown(SpellIds.ImpendingVictory, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleOnProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 34428 - Victory Rush
    class spell_warr_victory_rush : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Victorious, SpellIds.VictoryRushHeal);
        }

        void HandleHeal()
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, SpellIds.VictoryRushHeal, true);
            caster.RemoveAurasDueToSpell(SpellIds.Victorious);
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleHeal));
        }
    }
}