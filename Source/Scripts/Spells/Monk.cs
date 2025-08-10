// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using static Global;

namespace Scripts.Spells.Monk
{
    struct SpellIds
    {
        public const uint CalmingCoalescence = 388220;
        public const uint CracklingJadeLightningChannel = 117952;
        public const uint CracklingJadeLightningChiProc = 123333;
        public const uint CracklingJadeLightningKnockback = 117962;
        public const uint CracklingJadeLightningKnockbackCd = 117953;
        public const uint PowerStrikeProc = 129914;
        public const uint PowerStrikeEnergize = 121283;
        public const uint ProvokeSingleTarget = 116189;
        public const uint ProvokeAoe = 118635;
        public const uint NoFeatherFall = 79636;
        public const uint OpenPalmStrikesTalent = 392970;
        public const uint RollBackward = 109131;
        public const uint RollForward = 107427;
        public const uint SoothingMist = 115175;
        public const uint StanceOfTheSpiritedCrane = 154436;
        public const uint StaggerDamageAura = 124255;
        public const uint StaggerHeavy = 124273;
        public const uint StaggerLight = 124275;
        public const uint StaggerModerate = 124274;
        public const uint SurgingMistHeal = 116995;
    }

    struct MonkUtility
    {
        // Utility for stagger scripts
        public static Aura FindExistingStaggerEffect(Unit unit)
        {
            Aura auraLight = unit.GetAura(SpellIds.StaggerLight);
            if (auraLight != null)
                return auraLight;

            Aura auraModerate = unit.GetAura(SpellIds.StaggerModerate);
            if (auraModerate != null)
                return auraModerate;

            Aura auraHeavy = unit.GetAura(SpellIds.StaggerHeavy);
            if (auraHeavy != null)
                return auraHeavy;

            return null;
        }
    }

    [Script] // 117952 - Crackling Jade Lightning
    class spell_monk_crackling_jade_lightning : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StanceOfTheSpiritedCrane, SpellIds.CracklingJadeLightningChiProc);
        }

        void OnTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
                if (caster.HasAura(SpellIds.StanceOfTheSpiritedCrane))
                    caster.CastSpell(caster, SpellIds.CracklingJadeLightningChiProc, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnTick, 0, AuraType.PeriodicDamage));
        }
    }

    [Script] // 117959 - Crackling Jade Lightning
    class spell_monk_crackling_jade_lightning_knockback_proc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CracklingJadeLightningKnockback,
                SpellIds.CracklingJadeLightningKnockbackCd
           );
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (GetTarget().HasAura(SpellIds.CracklingJadeLightningKnockbackCd))
                return false;

            if (eventInfo.GetActor().HasAura(SpellIds.CracklingJadeLightningChannel, GetTarget().GetGUID()))
                return false;

            Spell currentChanneledSpell = GetTarget().GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (currentChanneledSpell == null || currentChanneledSpell.GetSpellInfo().Id != SpellIds.CracklingJadeLightningChannel)
                return false;

            return true;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell(eventInfo.GetActor(), SpellIds.CracklingJadeLightningKnockback, TriggerCastFlags.FullMask);
            GetTarget().CastSpell(GetTarget(), SpellIds.CracklingJadeLightningKnockbackCd, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 116849 - Life Cocoon
    class spell_monk_life_cocoon : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CalmingCoalescence);
        }

        void CalculateAbsorb(uint effIndex)
        {
            int absorb = (int)GetCaster().CountPctFromMaxHealth(GetEffectValue());
            Player player = GetCaster().ToPlayer();
            if (player != null)
                MathFunctions.AddPct(ref absorb, player.GetRatingBonusValue(CombatRating.VersatilityHealingDone));

            AuraEffect calmingCoalescence = GetCaster().GetAuraEffect(SpellIds.CalmingCoalescence, 0, GetCaster().GetGUID());
            if (calmingCoalescence != null)
            {
                MathFunctions.AddPct(ref absorb, calmingCoalescence.GetAmount());
                calmingCoalescence.GetBase().Remove();
            }

            GetSpell().SetSpellValue(new((int)SpellValueMod.BasePoint0, absorb));
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new(CalculateAbsorb, 2, SpellEffectName.Dummy));
        }
    }

    [Script] // 392972 - Open Palm Strikes
    class spell_monk_open_palm_strikes : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((SpellIds.OpenPalmStrikesTalent, 1));
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            AuraEffect talent = GetTarget().GetAuraEffect(SpellIds.OpenPalmStrikesTalent, 1);
            return talent != null && RandomHelper.randChance(talent.GetAmount());
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 121817 - Power Strike
    class spell_monk_power_strike_periodic : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerStrikeProc);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.PowerStrikeProc, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 129914 - Power Strike Proc
    class spell_monk_power_strike_proc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PowerStrikeEnergize);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.PowerStrikeEnergize, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 115546 - Provoke
    class spell_monk_provoke : SpellScript
    {
        const uint BlackOxStatusEntry = 61146;

        public override bool Validate(SpellInfo spellInfo)
        {
            if (!spellInfo.GetExplicitTargetMask().HasFlag(SpellCastTargetFlags.UnitMask)) // ensure GetExplTargetUnit() will return something meaningful during CheckCast
                return false;
            return ValidateSpellInfo(SpellIds.ProvokeSingleTarget,
                SpellIds.ProvokeAoe
           );
        }

        SpellCastResult CheckExplicitTarget()
        {
            if (GetExplTargetUnit().GetEntry() != BlackOxStatusEntry)
            {
                SpellInfo singleTarget = SpellMgr.GetSpellInfo(SpellIds.ProvokeSingleTarget, GetCastDifficulty());
                SpellCastResult singleTargetExplicitResult = singleTarget.CheckExplicitTarget(GetCaster(), GetExplTargetUnit());
                if (singleTargetExplicitResult != SpellCastResult.SpellCastOk)
                    return singleTargetExplicitResult;
            }
            else if (GetExplTargetUnit().GetOwnerGUID() != GetCaster().GetGUID())
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            if (GetHitUnit().GetEntry() != BlackOxStatusEntry)
                GetCaster().CastSpell(GetHitUnit(), SpellIds.ProvokeSingleTarget, true);
            else
                GetCaster().CastSpell(GetHitUnit(), SpellIds.ProvokeAoe, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckExplicitTarget));
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 109132 - Roll
    class spell_monk_roll : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RollBackward, SpellIds.RollForward, SpellIds.NoFeatherFall);
        }

        SpellCastResult CheckCast()
        {
            if (GetCaster().HasUnitState(UnitState.Root))
                return SpellCastResult.Rooted;
            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), GetCaster().HasUnitMovementFlag(MovementFlag.Backward) ? SpellIds.RollBackward : SpellIds.RollForward,
                TriggerCastFlags.IgnoreCastInProgress);
            GetCaster().CastSpell(GetCaster(), SpellIds.NoFeatherFall, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 107427 - Roll
    [Script] // 109131 - Roll (backward)
    class spell_monk_roll_AuraScript : AuraScript
    {
        void CalcMovementAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount += 100;
        }

        void CalcImmunityAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount -= 100;
        }

        void ChangeRunBackSpeed(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().SetSpeed(UnitMoveType.RunBack, GetTarget().GetSpeed(UnitMoveType.Run));
        }

        void RestoreRunBackSpeed(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().UpdateSpeed(UnitMoveType.RunBack);
        }

        public override void Register()
        {
            // Values need manual correction
            DoEffectCalcAmount.Add(new(CalcMovementAmount, 0, AuraType.ModSpeedNoControl));
            DoEffectCalcAmount.Add(new(CalcMovementAmount, 2, AuraType.ModMinimumSpeed));
            DoEffectCalcAmount.Add(new(CalcImmunityAmount, 5, AuraType.MechanicImmunity));
            DoEffectCalcAmount.Add(new(CalcImmunityAmount, 6, AuraType.MechanicImmunity));

            // This is a special aura that sets backward run speed equal to forward speed
            AfterEffectApply.Add(new(ChangeRunBackSpeed, 4, AuraType.UseNormalMovementSpeed, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(RestoreRunBackSpeed, 4, AuraType.UseNormalMovementSpeed, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 115069 - Stagger
    class spell_monk_stagger : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StaggerLight, SpellIds.StaggerModerate, SpellIds.StaggerHeavy);
        }

        void AbsorbNormal(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Absorb(dmgInfo, 1.0f);
        }

        void AbsorbMagic(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            AuraEffect effect = GetEffect(4);
            if (effect == null)
                return;

            Absorb(dmgInfo, (float)(effect.GetAmount()) / 100.0f);
        }

        void Absorb(DamageInfo dmgInfo, float multiplier)
        {
            // Prevent default action (which would Remove the aura)
            PreventDefaultAction();

            // make sure damage doesn't come from stagger damage spell SpellIds.StaggerDamageAura
            SpellInfo dmgSpellInfo = dmgInfo.GetSpellInfo();
            if (dmgSpellInfo != null && dmgSpellInfo.Id == SpellIds.StaggerDamageAura)
                return;

            AuraEffect effect = GetEffect(0);
            if (effect == null)
                return;

            Unit target = GetTarget();
            float agility = target.GetStat(Stats.Agility);
            float baseAmount = MathFunctions.CalculatePct(agility, (float)(effect.GetAmount()));
            float K = DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, target.GetLevel(), -2, 0, target.GetClass(), 0);

            float newAmount = (baseAmount / (baseAmount + K));
            newAmount *= multiplier;

            // Absorb X percentage of the damage
            float absorbAmount = (float)(dmgInfo.GetDamage()) * newAmount;
            if (absorbAmount > 0)
            {
                dmgInfo.AbsorbDamage((uint)absorbAmount);

                // Cast stagger and make it tick on each tick
                AddAndRefreshStagger(absorbAmount);
            }
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new(AbsorbNormal, 1));
            OnEffectAbsorb.Add(new(AbsorbMagic, 2));
        }

        void AddAndRefreshStagger(float amount)
        {
            Unit target = GetTarget();
            Aura auraStagger = MonkUtility.FindExistingStaggerEffect(target);
            if (auraStagger != null)
            {
                AuraEffect effStaggerRemaining = auraStagger.GetEffect(1);
                if (effStaggerRemaining == null)
                    return;

                float newAmount = effStaggerRemaining.GetAmount() + amount;
                uint spellId = GetStaggerSpellId(target, newAmount);
                if (spellId == effStaggerRemaining.GetSpellInfo().Id)
                {
                    auraStagger.RefreshDuration();
                    effStaggerRemaining.ChangeAmount((int)newAmount, false, true);
                }
                else
                {
                    // amount changed the stagger type so we need to change the stagger amount (e.g. from medium to light)
                    GetTarget().RemoveAura(auraStagger);
                    AddNewStagger(target, spellId, newAmount);
                }
            }
            else
                AddNewStagger(target, GetStaggerSpellId(target, amount), amount);
        }

        uint GetStaggerSpellId(Unit unit, float amount)
        {
            float StaggerHeavy = 0.6f;
            float StaggerModerate = 0.3f;

            float staggerPct = amount / (float)(unit.GetMaxHealth());
            return (staggerPct >= StaggerHeavy) ? SpellIds.StaggerHeavy :
                (staggerPct >= StaggerModerate) ? SpellIds.StaggerModerate :
                SpellIds.StaggerLight;
        }

        void AddNewStagger(Unit unit, uint staggerSpellId, float staggerAmount)
        {
            // We only set the total stagger amount. The amount per tick will be set by the stagger spell script
            unit.CastSpell(unit, staggerSpellId, new CastSpellExtraArgs(SpellValueMod.BasePoint1, (int)staggerAmount).SetTriggerFlags(TriggerCastFlags.FullMask));
        }
    }

    [Script] // 124255 - Stagger - SpellIds.StaggerDamageAura
    class spell_monk_stagger_damage : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StaggerLight, SpellIds.StaggerModerate, SpellIds.StaggerHeavy);
        }

        void OnPeriodicDamage(AuraEffect aurEff)
        {
            // Update our light/medium/heavy stagger with the correct stagger amount left
            Aura auraStagger = MonkUtility.FindExistingStaggerEffect(GetTarget());
            if (auraStagger != null)
            {
                AuraEffect auraEff = auraStagger.GetEffect(1);
                if (auraEff != null)
                {
                    float total = (float)(auraEff.GetAmount());
                    float tickDamage = (float)(aurEff.GetAmount());
                    auraEff.ChangeAmount((int)(total - tickDamage));
                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnPeriodicDamage, 0, AuraType.PeriodicDamage));
        }
    }

    [Script] // 124273, 124274, 124275 - Light/Moderate/Heavy Stagger - SpellIds.StaggerLight / SpellIds.StaggerModerate / SpellIds.StaggerHeavy
    class spell_monk_stagger_debuff : AuraScript
    {
        float _period;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StaggerDamageAura) && ValidateSpellEffect((SpellIds.StaggerDamageAura, 0));
        }

        public override bool Load()
        {
            _period = (float)(SpellMgr.GetSpellInfo(SpellIds.StaggerDamageAura, GetCastDifficulty()).GetEffect(0).ApplyAuraPeriod);
            return true;
        }

        void OnReapply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Calculate damage per tick
            float total = (float)(aurEff.GetAmount());
            float perTick = total * _period / (float)(GetDuration()); // should be same as GetMaxDuration() Todo: verify

            // Set amount on effect for tooltip
            AuraEffect effInfo = GetAura().GetEffect(0);
            if (effInfo != null)
                effInfo.ChangeAmount((int)perTick);

            // Set amount on damage aura (or cast it if needed)
            CastOrChangeTickDamage(perTick);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (mode != AuraEffectHandleModes.Real)
                return;

            // Remove damage aura
            GetTarget().RemoveAura(SpellIds.StaggerDamageAura);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(OnReapply, 1, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
            AfterEffectRemove.Add(new(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        }

        void CastOrChangeTickDamage(float tickDamage)
        {
            Unit unit = GetTarget();
            Aura auraDamage = unit.GetAura(SpellIds.StaggerDamageAura);
            if (auraDamage == null)
            {
                unit.CastSpell(unit, SpellIds.StaggerDamageAura, true);
                auraDamage = unit.GetAura(SpellIds.StaggerDamageAura);
            }

            if (auraDamage != null)
            {
                AuraEffect eff = auraDamage.GetEffect(0);
                if (eff != null)
                    eff.ChangeAmount((int)tickDamage);
            }
        }
    }
}
