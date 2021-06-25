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
using Framework.Dynamic;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Mage
{
    struct SpellIds
    {
        public const uint AlterTimeAura = 110909;
        public const uint AlterTimeVisual = 347402;
        public const uint ArcaneAlterTimeAura = 342246;
        public const uint ArcaneBarrageEnergize = 321529;
        public const uint ArcaneBarrageR3 = 321526;
        public const uint ArcaneCharge = 36032;
        public const uint ArcaneMage = 137021;
        public const uint BlazingBarrierTrigger = 235314;
        public const uint Blink = 1953;
        public const uint Cauterized = 87024;
        public const uint CauterizeDot = 87023;
        public const uint ConeOfCold = 120;
        public const uint ConeOfColdSlow = 212792;
        public const uint ConjureRefreshment = 116136;
        public const uint ConjureRefreshmentTable = 167145;
        public const uint DradonhawkForm = 32818;
        public const uint FingersOfFrost = 44544;
        public const uint FrostNova = 122;
        public const uint GiraffeForm = 32816;
        public const uint IceBarrier = 11426;
        public const uint IceBlock = 45438;
        public const uint Ignite = 12654;
        public const uint LivingBombExplosion = 44461;
        public const uint LivingBombPeriodic = 217694;
        public const uint ManaSurge = 37445;
        public const uint MasterOfTime = 342249;
        public const uint Reverberate = 281482;
        public const uint RingOfFrostDummy = 91264;
        public const uint RingOfFrostFreeze = 82691;
        public const uint RingOfFrostSummon = 113724;
        public const uint SerpentForm = 32817;
        public const uint SheepForm = 32820;
        public const uint SquirrelForm = 32813;
        public const uint TemporalDisplacement = 80354;
        public const uint WorgenForm = 32819;
        public const uint Chilled = 205708;
        public const uint IceLanceTrigger = 228598;
        public const uint ThermalVoid = 155149;
        public const uint IcyVeins = 12472;
        public const uint ChainReactionDummy = 278309;
        public const uint ChainReaction = 278310;
        public const uint TouchOfTheMagiExplode = 210833;

        //Misc
        public const uint HunterInsanity = 95809;
        public const uint ShamanExhaustion = 57723;
        public const uint ShamanSated = 57724;
        public const uint PetNetherwindsFatigued = 160455;
    }

    // 110909 - Alter Time Aura
    [Script] // 342246 - Alter Time Aura
    class spell_mage_alter_time_aura : AuraScript
    {
        ulong _health;
        Position _pos;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AlterTimeVisual, SpellIds.MasterOfTime, SpellIds.Blink);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit unit = GetTarget();
            _health = unit.GetHealth();
            _pos = new(unit.GetPosition());
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit unit = GetTarget();
            if (unit.GetDistance(_pos) <= 100.0f && GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
            {
                unit.SetHealth(_health);
                unit.NearTeleportTo(_pos);

                if (unit.HasAura(SpellIds.MasterOfTime))
                {
                    SpellInfo blink = Global.SpellMgr.GetSpellInfo(SpellIds.Blink, Difficulty.None);
                    unit.GetSpellHistory().ResetCharges(blink.ChargeCategoryId);
                }
                unit.CastSpell(unit, SpellIds.AlterTimeVisual);
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.OverrideActionbarSpells, AuraEffectHandleModes.Real));
        }
    }

    // 127140 - Alter Time Active
    [Script] // 342247 - Alter Time Active
    class spell_mage_alter_time_active : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AlterTimeAura, SpellIds.ArcaneAlterTimeAura);
        }

        void RemoveAlterTimeAura(uint effIndex)
        {
            Unit unit = GetCaster();
            unit.RemoveAura(SpellIds.AlterTimeAura, ObjectGuid.Empty, 0, AuraRemoveMode.Expire);
            unit.RemoveAura(SpellIds.ArcaneAlterTimeAura, ObjectGuid.Empty, 0, AuraRemoveMode.Expire);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(RemoveAlterTimeAura, 0, SpellEffectName.Dummy));
        }
    }
    
    [Script] // 44425 - Arcane Barrage
    class spell_mage_arcane_barrage : SpellScript
    {
        ObjectGuid _primaryTarget;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ArcaneBarrageR3, SpellIds.ArcaneBarrageEnergize) && spellInfo.GetEffect(1) != null;
        }

        void ConsumeArcaneCharges()
        {
            Unit caster = GetCaster();

            // Consume all arcane charges
            int arcaneCharges = -caster.ModifyPower(PowerType.ArcaneCharges, -caster.GetMaxPower(PowerType.ArcaneCharges), false);
            if (arcaneCharges != 0)
            {
                AuraEffect auraEffect = caster.GetAuraEffect(SpellIds.ArcaneBarrageR3, 0, caster.GetGUID());
                if (auraEffect != null)
                    caster.CastSpell(caster, SpellIds.ArcaneBarrageEnergize, new CastSpellExtraArgs(SpellValueMod.BasePoint0, arcaneCharges * auraEffect.GetAmount() / 100));
            }
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            if (GetHitUnit().GetGUID() != _primaryTarget)
                SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), GetEffectInfo(1).CalcValue(GetCaster())));
        }

        void MarkPrimaryTarget(uint effIndex)
        {
            _primaryTarget = GetHitUnit().GetGUID();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.SchoolDamage));
            OnEffectLaunchTarget.Add(new EffectHandler(MarkPrimaryTarget, 1, SpellEffectName.Dummy));
            AfterCast.Add(new CastHandler(ConsumeArcaneCharges));
        }
    }

    [Script] // 195302 - Arcane Charge
    class spell_mage_arcane_charge_clear : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ArcaneCharge);
        }

        void RemoveArcaneCharge(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell(SpellIds.ArcaneCharge);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(RemoveArcaneCharge, 0, SpellEffectName.Dummy));
        }
    }
    
    [Script] // 1449 - Arcane Explosion
    class spell_mage_arcane_explosion : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            if (!ValidateSpellInfo(SpellIds.ArcaneMage, SpellIds.Reverberate))
                return false;

            SpellEffectInfo damageEffect = spellInfo.GetEffect(1);
            return damageEffect != null && damageEffect.IsEffect(SpellEffectName.SchoolDamage);
        }

        void CheckRequiredAuraForBaselineEnergize(uint effIndex)
        {
            if (GetUnitTargetCountForEffect(1) == 0 || !GetCaster().HasAura(SpellIds.ArcaneMage))
                PreventHitDefaultEffect(effIndex);
        }

        void HandleReverberate(uint effIndex)
        {
            bool procTriggered = false;
            
            Unit caster = GetCaster();
            AuraEffect triggerChance = caster.GetAuraEffect(SpellIds.Reverberate, 0);
            if (triggerChance != null)
            {
                AuraEffect requiredTargets = caster.GetAuraEffect(SpellIds.Reverberate, 1);
                if (requiredTargets != null)
                    procTriggered = GetUnitTargetCountForEffect(1) >= requiredTargets.GetAmount() && RandomHelper.randChance(triggerChance.GetAmount());
            }

            if (!procTriggered)
                PreventHitDefaultEffect(effIndex);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(CheckRequiredAuraForBaselineEnergize, 0, SpellEffectName.Energize));
            OnEffectHitTarget.Add(new EffectHandler(HandleReverberate, 2, SpellEffectName.Energize));
        }
    }
    
    [Script] // 235313 - Blazing Barrier
    class spell_mage_blazing_barrier : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlazingBarrierTrigger);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            Unit caster = GetCaster();
            if (caster)
                amount = (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetDamageInfo().GetVictim();
            Unit target = eventInfo.GetDamageInfo().GetAttacker();

            if (caster && target)
                caster.CastSpell(target, SpellIds.BlazingBarrierTrigger, true);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 198063 - Burning Determination
    class spell_mage_burning_determination : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo != null)
                if (spellInfo.GetAllEffectsMechanicMask().HasAnyFlag(((1u << (int)Mechanics.Interrupt) | (1 << (int)Mechanics.Silence))))
                    return true;

            return false;
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
        }
    }

    [Script] // 86949 - Cauterize
    class spell_mage_cauterize : SpellScript
    {
        void SuppressSpeedBuff(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new EffectHandler(SuppressSpeedBuff, 2, SpellEffectName.TriggerSpell));
        }
    }

    [Script]
    class spell_mage_cauterize_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffect(2) != null && ValidateSpellInfo(SpellIds.CauterizeDot, SpellIds.Cauterized, spellInfo.GetEffect(2).TriggerSpell);
        }

        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            AuraEffect effectInfo = GetEffect(1);
            if (effectInfo == null || !GetTargetApplication().HasEffect(1) ||
                dmgInfo.GetDamage() < GetTarget().GetHealth() ||
                dmgInfo.GetDamage() > GetTarget().GetMaxHealth() * 2 ||
                GetTarget().HasAura(SpellIds.Cauterized))
            {
                PreventDefaultAction();
                return;
            }

            GetTarget().SetHealth(GetTarget().CountPctFromMaxHealth(effectInfo.GetAmount()));
            GetTarget().CastSpell(GetTarget(), GetSpellInfo().GetEffect(2).TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            GetTarget().CastSpell(GetTarget(), SpellIds.CauterizeDot, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            GetTarget().CastSpell(GetTarget(), SpellIds.Cauterized, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new EffectAbsorbHandler(HandleAbsorb, 0));
        }
    }

    [Script] // 235219 - Cold Snap
    class spell_mage_cold_snap : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConeOfCold, SpellIds.FrostNova, SpellIds.IceBarrier, SpellIds.IceBlock);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().GetSpellHistory().ResetCooldowns(p =>
            {
                switch (p.Key)
                {
                    case SpellIds.ConeOfCold:
                    case SpellIds.FrostNova:
                    case SpellIds.IceBarrier:
                    case SpellIds.IceBlock:
                        return true;
                    default:
                        break;
                }
                return false;
            }, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 120 - Cone of Cold
    class spell_mage_cone_of_cold : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConeOfColdSlow);
        }

        void HandleSlow(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ConeOfColdSlow, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleSlow, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 190336 - Conjure Refreshment
    class spell_mage_conjure_refreshment : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConjureRefreshment, SpellIds.ConjureRefreshmentTable);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (caster)
            {
                Group group = caster.GetGroup();
                if (group)
                    caster.CastSpell(caster, SpellIds.ConjureRefreshmentTable, true);
                else
                    caster.CastSpell(caster, SpellIds.ConjureRefreshment, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 112965 - Fingers of Frost
    class spell_mage_fingers_of_frost_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FingersOfFrost);
        }

        bool CheckFrostboltProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Mage, new FlagArray128(0, 0x2000000, 0, 0))
                && RandomHelper.randChance(aurEff.GetAmount());
        }

        bool CheckFrozenOrbProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Mage, new FlagArray128(0, 0, 0x80, 0))
                && RandomHelper.randChance(aurEff.GetAmount());
        }

        void Trigger(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            eventInfo.GetActor().CastSpell(GetTarget(), SpellIds.FingersOfFrost, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new CheckEffectProcHandler(CheckFrostboltProc, 0, AuraType.Dummy));
            DoCheckEffectProc.Add(new CheckEffectProcHandler(CheckFrozenOrbProc, 1, AuraType.Dummy));
            AfterEffectProc.Add(new EffectProcHandler(Trigger, 0, AuraType.Dummy));
            AfterEffectProc.Add(new EffectProcHandler(Trigger, 1, AuraType.Dummy));
        }
    }
    
    [Script] // 11426 - Ice Barrier
    class spell_mage_ice_barrier : AuraScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.Chilled);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            Unit caster = GetCaster();
            if (caster)
                amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 10.0f);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit caster = eventInfo.GetDamageInfo().GetVictim();
            Unit target = eventInfo.GetDamageInfo().GetAttacker();

            if (caster && target)
                caster.CastSpell(target, SpellIds.Chilled, true);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.SchoolAbsorb));
        }
    }

    [Script] // Ice Lance - 30455
    class spell_mage_ice_lance : SpellScript
    {
        List<ObjectGuid> _orderedTargets = new List<ObjectGuid>();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IceLanceTrigger, SpellIds.ThermalVoid, SpellIds.IcyVeins, SpellIds.ChainReactionDummy, SpellIds.ChainReaction, SpellIds.FingersOfFrost);
        }

        void IndexTarget(uint effIndex)
        {
            _orderedTargets.Add(GetHitUnit().GetGUID());
        }

        void HandleOnHit(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            int index = _orderedTargets.IndexOf(target.GetGUID());

            if (index == 0 // only primary target triggers these benefits
                && target.HasAuraState(AuraStateType.Frozen, GetSpellInfo(), caster))
            {
                // Thermal Void
                Aura thermalVoid = caster.GetAura(SpellIds.ThermalVoid);
                if (thermalVoid != null)
                {
                    SpellEffectInfo thermalVoidEffect = thermalVoid.GetSpellInfo().GetEffect(0);
                    if (thermalVoidEffect != null)
                    {
                        Aura icyVeins = caster.GetAura(SpellIds.IcyVeins);
                        if (icyVeins != null)
                            icyVeins.SetDuration(icyVeins.GetDuration() + thermalVoidEffect.CalcValue(caster) * Time.InMilliseconds);
                    }
                }

                // Chain Reaction
                if (caster.HasAura(SpellIds.ChainReactionDummy))
                    caster.CastSpell(caster, SpellIds.ChainReaction, true);
            }

            // put target index for chain value multiplier into EFFECT_1 base points, otherwise triggered spell doesn't know which damage multiplier to apply
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint1, index);
            caster.CastSpell(target, SpellIds.IceLanceTrigger, args);
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new EffectHandler(IndexTarget, 0, SpellEffectName.ScriptEffect));
            OnEffectHitTarget.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 228598 - Ice Lance
    class spell_mage_ice_lance_damage : SpellScript
    {
        void ApplyDamageMultiplier(uint effIndex)
        {
            SpellValue spellValue = GetSpellValue();
            if ((spellValue.CustomBasePointsMask & (1 << 1)) != 0)
            {
                int originalDamage = GetHitDamage();
                float targetIndex = (float)spellValue.EffectBasePoints[1];
                float multiplier = MathF.Pow(GetEffectInfo().CalcDamageMultiplier(GetCaster(), GetSpell()), targetIndex);
                SetHitDamage((int)(originalDamage * multiplier));
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(ApplyDamageMultiplier, 0, SpellEffectName.SchoolDamage));
        }
    }
    
    [Script] // 11119 - Ignite
    class spell_mage_ignite : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Ignite);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            SpellInfo igniteDot = Global.SpellMgr.GetSpellInfo(SpellIds.Ignite, GetCastDifficulty());
            int pct = aurEff.GetAmount();

            int amount = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), pct) / igniteDot.GetMaxTicks());
            amount += (int)eventInfo.GetProcTarget().GetRemainingPeriodicAmount(eventInfo.GetActor().GetGUID(), SpellIds.Ignite, AuraType.PeriodicDamage);

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.Ignite, args);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 37447 - Improved Mana Gems
    [Script] // 61062 - Improved Mana Gems
    class spell_mage_imp_mana_gems : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ManaSurge);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActor().CastSpell((Unit)null, SpellIds.ManaSurge, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // 44457 - Living Bomb
    class spell_mage_living_bomb : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LivingBombPeriodic);
        }

        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetCaster().CastSpell(GetHitUnit(), SpellIds.LivingBombPeriodic, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint2, 1));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 44461 - Living Bomb
    class spell_mage_living_bomb_explosion : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.NeedsExplicitUnitTarget() && ValidateSpellInfo(SpellIds.LivingBombPeriodic);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.Remove(GetExplTargetWorldObject());
        }

        void HandleSpread(uint effIndex)
        {
            if (GetSpellValue().EffectBasePoints[0] > 0)
                GetCaster().CastSpell(GetHitUnit(), SpellIds.LivingBombPeriodic, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint2, 0));
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleSpread, 1, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 217694 - Living Bomb
    class spell_mage_living_bomb_periodic : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LivingBombExplosion);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit caster = GetCaster();
            if (caster)
                caster.CastSpell(GetTarget(), SpellIds.LivingBombExplosion, new CastSpellExtraArgs (TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount()));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 2, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // @todo move out of here and rename - not a mage spell
    [Script] // 32826 - Polymorph (Visual)
    class spell_mage_polymorph_visual : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PolymorhForms);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetCaster().FindNearestCreature(NPC_AUROSALIA, 30.0f);
            if (target)
                if (target.IsTypeId(TypeId.Unit))
                    target.CastSpell(target, PolymorhForms[RandomHelper.IRand(0, 5)], true);
        }

        public override void Register()
        {
            // add dummy effect spell handler to Polymorph visual
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }

        const uint NPC_AUROSALIA = 18744;
        uint[] PolymorhForms =
        {
                SpellIds.SquirrelForm,
                SpellIds.GiraffeForm,
                SpellIds.SerpentForm,
                SpellIds.DradonhawkForm,
                SpellIds.WorgenForm,
                SpellIds.SheepForm
        };
    }

    [Script] // 235450 - Prismatic Barrier
    class spell_mage_prismatic_barrier : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            Unit caster = GetCaster();
            if (caster)
                amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        }
    }

    [Script] // 136511 - Ring of Frost
    class spell_mage_ring_of_frost : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RingOfFrostSummon, SpellIds.RingOfFrostFreeze);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            TempSummon ringOfFrost = GetRingOfFrostMinion();
            if (ringOfFrost)
                GetTarget().CastSpell(ringOfFrost.GetPosition(), SpellIds.RingOfFrostFreeze, new CastSpellExtraArgs(true));
        }

        void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            List<TempSummon> minions = new List<TempSummon>();
            GetTarget().GetAllMinionsByEntry(minions, (uint)Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon, GetCastDifficulty()).GetEffect(0).MiscValue);

            // Get the last summoned RoF, save it and despawn older ones
            foreach (var summon in minions)
            {
                TempSummon ringOfFrost = GetRingOfFrostMinion();
                if (ringOfFrost)
                {
                    if (summon.GetTimer() > ringOfFrost.GetTimer())
                    {
                        ringOfFrost.DespawnOrUnsummon();
                        _ringOfFrostGUID = summon.GetGUID();
                    }
                    else
                        summon.DespawnOrUnsummon();
                }
                else
                    _ringOfFrostGUID = summon.GetGUID();
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.ProcTriggerSpell));
            OnEffectApply.Add(new EffectApplyHandler(Apply, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.RealOrReapplyMask));
        }

        TempSummon GetRingOfFrostMinion()
        {
            Creature creature = ObjectAccessor.GetCreature(GetOwner(), _ringOfFrostGUID);
            if (creature)
                return creature.ToTempSummon();
            return null;
        }

        ObjectGuid _ringOfFrostGUID;
    }

    [Script] // 82691 - Ring of Frost (freeze efect)
    class spell_mage_ring_of_frost_freeze : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RingOfFrostSummon, SpellIds.RingOfFrostFreeze);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            WorldLocation dest = GetExplTargetDest();
            float outRadius = Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon, GetCastDifficulty()).GetEffect(0).CalcRadius();
            float inRadius = 6.5f;

            targets.RemoveAll(target =>
            {
                Unit unit = target.ToUnit();
                if (!unit)
                    return true;

                return unit.HasAura(SpellIds.RingOfFrostDummy) || unit.HasAura(SpellIds.RingOfFrostFreeze) || unit.GetExactDist(dest) > outRadius || unit.GetExactDist(dest) < inRadius;
            });
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        }
    }

    [Script]
    class spell_mage_ring_of_frost_freeze_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RingOfFrostDummy);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                if (GetCaster())
                    GetCaster().CastSpell(GetTarget(), SpellIds.RingOfFrostDummy, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 80353 - Time Warp
    class spell_mage_time_warp : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TemporalDisplacement, SpellIds.HunterInsanity, SpellIds.ShamanExhaustion, SpellIds.ShamanSated, SpellIds.PetNetherwindsFatigued);
        }

        void RemoveInvalidTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.TemporalDisplacement));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HunterInsanity));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.ShamanExhaustion));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.ShamanSated));
        }

        void ApplyDebuff()
        {
            Unit target = GetHitUnit();
            if (target)
                target.CastSpell(target, SpellIds.TemporalDisplacement, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
            AfterHit.Add(new HitHandler(ApplyDebuff));
        }
    }

    [Script] // 210824 - Touch of the Magi (Aura)
    class spell_mage_touch_of_the_magi_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TouchOfTheMagiExplode);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                if (damageInfo.GetAttacker() == GetCaster() && damageInfo.GetVictim() == GetTarget())
                {
                    uint extra = MathFunctions.CalculatePct(damageInfo.GetDamage(), 25);
                    if (extra > 0)
                        aurEff.ChangeAmount(aurEff.GetAmount() + (int)extra);
                }
            }
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            int amount = aurEff.GetAmount();
            if (amount == 0 || GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(GetTarget(), SpellIds.TouchOfTheMagiExplode, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, amount));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }
    
    [Script] //228597 - Frostbolt   84721  - Frozen Orb   190357 - Blizzard
    class spell_mage_trigger_chilled : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.Chilled);
        }

        void HandleChilled()
        {
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.Chilled, true);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleChilled));
        }
    }

    [Script] // 33395 Water Elemental's Freeze
    class spell_mage_water_elemental_freeze : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FingersOfFrost);
        }

        void HandleImprovedFreeze()
        {
            Unit owner = GetCaster().GetOwner();
            if (!owner)
                return;

            owner.CastSpell(owner, SpellIds.FingersOfFrost, true);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleImprovedFreeze));
        }
    }
}
