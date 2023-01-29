// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk
{
    internal struct SpellIds
    {
        public const uint CracklingJadeLightningChannel = 117952;
        public const uint CracklingJadeLightningChiProc = 123333;
        public const uint CracklingJadeLightningKnockback = 117962;
        public const uint CracklingJadeLightningKnockbackCd = 117953;
        public const uint ProvokeSingleTarget = 116189;
        public const uint ProvokeAoe = 118635;
        public const uint NoFeatherFall = 79636;
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

    [Script] // 117952 - Crackling Jade Lightning
    internal class spell_monk_crackling_jade_lightning : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StanceOfTheSpiritedCrane, SpellIds.CracklingJadeLightningChiProc);
        }

        private void OnTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (caster)
                if (caster.HasAura(SpellIds.StanceOfTheSpiritedCrane))
                    caster.CastSpell(caster, SpellIds.CracklingJadeLightningChiProc, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDamage));
        }
    }

    [Script] // 117959 - Crackling Jade Lightning
    internal class spell_monk_crackling_jade_lightning_knockback_proc_aura : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CracklingJadeLightningKnockback, SpellIds.CracklingJadeLightningKnockbackCd);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (GetTarget().HasAura(SpellIds.CracklingJadeLightningKnockbackCd))
                return false;

            if (eventInfo.GetActor().HasAura(SpellIds.CracklingJadeLightningChannel, GetTarget().GetGUID()))
                return false;

            Spell currentChanneledSpell = GetTarget().GetCurrentSpell(CurrentSpellTypes.Channeled);

            if (!currentChanneledSpell ||
                currentChanneledSpell.GetSpellInfo().Id != SpellIds.CracklingJadeLightningChannel)
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
            GetTarget().CastSpell(eventInfo.GetActor(), SpellIds.CracklingJadeLightningKnockback, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            GetTarget().CastSpell(GetTarget(), SpellIds.CracklingJadeLightningKnockbackCd, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }
    }

    [Script] // 115546 - Provoke
    internal class spell_monk_provoke : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        private const uint BlackOxStatusEntry = 61146;

        public override bool Validate(SpellInfo spellInfo)
        {
            if (!spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask)) // ensure GetExplTargetUnit() will return something meaningful during CheckCast
                return false;

            return ValidateSpellInfo(SpellIds.ProvokeSingleTarget, SpellIds.ProvokeAoe);
        }

        public SpellCastResult CheckCast()
        {
            if (GetExplTargetUnit().GetEntry() != BlackOxStatusEntry)
            {
                SpellInfo singleTarget = Global.SpellMgr.GetSpellInfo(SpellIds.ProvokeSingleTarget, GetCastDifficulty());
                SpellCastResult singleTargetExplicitResult = singleTarget.CheckExplicitTarget(GetCaster(), GetExplTargetUnit());

                if (singleTargetExplicitResult != SpellCastResult.SpellCastOk)
                    return singleTargetExplicitResult;
            }
            else if (GetExplTargetUnit().GetOwnerGUID() != GetCaster().GetGUID())
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
            PreventHitDefaultEffect(effIndex);

            if (GetHitUnit().GetEntry() != BlackOxStatusEntry)
                GetCaster().CastSpell(GetHitUnit(), SpellIds.ProvokeSingleTarget, true);
            else
                GetCaster().CastSpell(GetHitUnit(), SpellIds.ProvokeAoe, true);
        }
    }

    [Script] // 109132 - Roll
    internal class spell_monk_roll : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RollBackward, SpellIds.RollForward, SpellIds.NoFeatherFall);
        }

        public SpellCastResult CheckCast()
        {
            if (GetCaster().HasUnitState(UnitState.Root))
                return SpellCastResult.Rooted;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            GetCaster()
                .CastSpell(GetCaster(),
                           GetCaster().HasUnitMovementFlag(MovementFlag.Backward) ? SpellIds.RollBackward : SpellIds.RollForward,
                           new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));

            GetCaster().CastSpell(GetCaster(), SpellIds.NoFeatherFall, true);
        }
    }

    // 107427 - Roll
    [Script] // 109131 - Roll (backward)
    internal class spell_monk_roll_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        private void CalcMovementAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount += 100;
        }

        private void CalcImmunityAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount -= 100;
        }

        private void ChangeRunBackSpeed(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().SetSpeed(UnitMoveType.RunBack, GetTarget().GetSpeed(UnitMoveType.Run));
        }

        private void RestoreRunBackSpeed(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().UpdateSpeed(UnitMoveType.RunBack);
        }

        public override void Register()
        {
            // Values need manual correction
            Effects.Add(new EffectCalcAmountHandler(CalcMovementAmount, 0, AuraType.ModSpeedNoControl));
            Effects.Add(new EffectCalcAmountHandler(CalcMovementAmount, 2, AuraType.ModMinimumSpeed));
            Effects.Add(new EffectCalcAmountHandler(CalcImmunityAmount, 5, AuraType.MechanicImmunity));
            Effects.Add(new EffectCalcAmountHandler(CalcImmunityAmount, 6, AuraType.MechanicImmunity));

            // This is a special aura that sets backward run speed equal to forward speed
            Effects.Add(new EffectApplyHandler(ChangeRunBackSpeed, 4, AuraType.UseNormalMovementSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectApplyHandler(RestoreRunBackSpeed, 4, AuraType.UseNormalMovementSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }
    }

    [Script] // 115069 - Stagger
    internal class spell_monk_stagger : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StaggerLight, SpellIds.StaggerModerate, SpellIds.StaggerHeavy);
        }

        private void AbsorbNormal(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            Absorb(dmgInfo, 1.0f);
        }

        private void AbsorbMagic(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            AuraEffect effect = GetEffect(4);

            if (effect == null)
                return;

            Absorb(dmgInfo, effect.GetAmount() / 100.0f);
        }

        private void Absorb(DamageInfo dmgInfo, float multiplier)
        {
            // Prevent default Action (which would remove the aura)
            PreventDefaultAction();

            // make sure Damage doesn't come from stagger Damage spell SPELL_MONK_STAGGER_DAMAGE_AURA
            SpellInfo dmgSpellInfo = dmgInfo.GetSpellInfo();

            if (dmgSpellInfo != null)
                if (dmgSpellInfo.Id == SpellIds.StaggerDamageAura)
                    return;

            AuraEffect effect = GetEffect(0);

            if (effect == null)
                return;

            Unit target = GetTarget();
            float agility = target.GetStat(Stats.Agility);
            float baseAmount = MathFunctions.CalculatePct(agility, effect.GetAmount());
            float K = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, target.GetLevel(), -2, 0, target.GetClass());

            float newAmount = (baseAmount / (baseAmount + K));
            newAmount *= multiplier;

            // Absorb X percentage of the Damage
            float absorbAmount = dmgInfo.GetDamage() * newAmount;

            if (absorbAmount > 0)
            {
                dmgInfo.AbsorbDamage((uint)absorbAmount);

                // Cast stagger and make it tick on each tick
                AddAndRefreshStagger(absorbAmount);
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectAbsorbHandler(AbsorbNormal, 1, false, AuraScriptHookType.EffectAbsorb));
            Effects.Add(new EffectAbsorbHandler(AbsorbMagic, 2, false, AuraScriptHookType.EffectAbsorb));
        }

        private void AddAndRefreshStagger(float amount)
        {
            Unit target = GetTarget();
            Aura auraStagger = FindExistingStaggerEffect(target);

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
                    effStaggerRemaining.ChangeAmount((int)newAmount, false, true /* reapply */);
                }
                else
                {
                    // amount changed the stagger Type so we need to change the stagger amount (e.g. from medium to light)
                    GetTarget().RemoveAura(auraStagger);
                    AddNewStagger(target, spellId, newAmount);
                }
            }
            else
            {
                AddNewStagger(target, GetStaggerSpellId(target, amount), amount);
            }
        }

        private uint GetStaggerSpellId(Unit unit, float amount)
        {
            const float StaggerHeavy = 0.6f;
            const float StaggerModerate = 0.3f;

            float staggerPct = amount / unit.GetMaxHealth();

            return (staggerPct >= StaggerHeavy) ? SpellIds.StaggerHeavy :
                   (staggerPct >= StaggerModerate) ? SpellIds.StaggerModerate :
                                                     SpellIds.StaggerLight;
        }

        private void AddNewStagger(Unit unit, uint staggerSpellId, float staggerAmount)
        {
            // We only set the total stagger amount. The amount per tick will be set by the stagger spell script
            unit.CastSpell(unit, staggerSpellId, new CastSpellExtraArgs(SpellValueMod.BasePoint1, (int)staggerAmount).SetTriggerFlags(TriggerCastFlags.FullMask));
        }

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

    [Script] // 124255 - Stagger - SPELL_MONK_STAGGER_DAMAGE_AURA
    internal class spell_monk_stagger_damage_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StaggerLight, SpellIds.StaggerModerate, SpellIds.StaggerHeavy);
        }

        private void OnPeriodicDamage(AuraEffect aurEff)
        {
            // Update our light/medium/heavy stagger with the correct stagger amount left
            Aura auraStagger = spell_monk_stagger.FindExistingStaggerEffect(GetTarget());

            if (auraStagger != null)
            {
                AuraEffect auraEff = auraStagger.GetEffect(1);

                if (auraEff != null)
                {
                    float total = auraEff.GetAmount();
                    float tickDamage = aurEff.GetAmount();
                    auraEff.ChangeAmount((int)(total - tickDamage));
                }
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(OnPeriodicDamage, 0, AuraType.PeriodicDamage));
        }
    }

    [Script] // 124273, 124274, 124275 - Light/Moderate/Heavy Stagger - SPELL_MONK_STAGGER_LIGHT / SPELL_MONK_STAGGER_MODERATE / SPELL_MONK_STAGGER_HEAVY
    internal class spell_monk_stagger_debuff_aura : AuraScript, IHasAuraEffects
    {
        private float _period;
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StaggerDamageAura) && !Global.SpellMgr.GetSpellInfo(SpellIds.StaggerDamageAura, Difficulty.None).GetEffects().Empty();
        }

        public override bool Load()
        {
            _period = (float)Global.SpellMgr.GetSpellInfo(SpellIds.StaggerDamageAura, GetCastDifficulty()).GetEffect(0).ApplyAuraPeriod;

            return true;
        }

        private void OnReapply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Calculate Damage per tick
            float total = aurEff.GetAmount();
            float perTick = total * _period / (float)GetDuration(); // should be same as GetMaxDuration() TODO: verify

            // Set amount on effect for tooltip
            AuraEffect effInfo = GetAura().GetEffect(0);

            effInfo?.ChangeAmount((int)perTick);

            // Set amount on Damage aura (or cast it if needed)
            CastOrChangeTickDamage(perTick);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (mode != AuraEffectHandleModes.Real)
                return;

            // Remove Damage aura
            GetTarget().RemoveAura(SpellIds.StaggerDamageAura);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnReapply, 1, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void CastOrChangeTickDamage(float tickDamage)
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

                eff?.ChangeAmount((int)tickDamage);
            }
        }
    }
}