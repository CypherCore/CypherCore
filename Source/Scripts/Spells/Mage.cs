// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using Game.AI;

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
        public const uint BlizzardDamage = 190357;
        public const uint BlizzardSlow = 12486;
        public const uint Cauterized = 87024;
        public const uint CauterizeDot = 87023;
        public const uint Chilled = 205708;
        public const uint CometStormDamage = 153596;
        public const uint CometStormVisual = 228601;
        public const uint ConeOfCold = 120;
        public const uint ConeOfColdSlow = 212792;
        public const uint ConjureRefreshment = 116136;
        public const uint ConjureRefreshmentTable = 167145;
        public const uint DradonhawkForm = 32818;
        public const uint EverwarmSocks = 320913;
        public const uint FingersOfFrost = 44544;
        public const uint FireBlast = 108853;
        public const uint Firestarter = 205026;
        public const uint FrostNova = 122;
        public const uint GiraffeForm = 32816;
        public const uint IceBarrier = 11426;
        public const uint IceBlock = 45438;
        public const uint Ignite = 12654;
        public const uint IncantersFlow = 116267;
        public const uint LivingBombExplosion = 44461;
        public const uint LivingBombPeriodic = 217694;
        public const uint ManaSurge = 37445;
        public const uint MasterOfTime = 342249;
        public const uint RayOfFrostBonus = 208141;
        public const uint RayOfFrostFingersOfFrost = 269748;
        public const uint Reverberate = 281482;
        public const uint RingOfFrostDummy = 91264;
        public const uint RingOfFrostFreeze = 82691;
        public const uint RingOfFrostSummon = 113724;
        public const uint SerpentForm = 32817;
        public const uint SheepForm = 32820;
        public const uint SquirrelForm = 32813;
        public const uint TemporalDisplacement = 80354;
        public const uint WorgenForm = 32819;
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
            return ValidateSpellInfo(SpellIds.ArcaneBarrageR3, SpellIds.ArcaneBarrageEnergize) && spellInfo.GetEffects().Count > 1;
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

            if (spellInfo.GetEffects().Count <= 1)
                return false;

            return spellInfo.GetEffect(1).IsEffect(SpellEffectName.SchoolDamage);
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

    // 190356 - Blizzard
    [Script] // 4658 - AreaTrigger Create Properties
    class areatrigger_mage_blizzard : AreaTriggerAI
    {
        TimeSpan _tickTimer;

        public areatrigger_mage_blizzard(AreaTrigger areatrigger) : base(areatrigger)
        {
            _tickTimer = TimeSpan.FromMilliseconds(1000);
        }

        public override void OnUpdate(uint diff)
        {
            _tickTimer -= TimeSpan.FromMilliseconds(diff);

            while (_tickTimer <= TimeSpan.Zero)
            {
                Unit caster = at.GetCaster();
                if (caster != null)
                    caster.CastSpell(at.GetPosition(), SpellIds.BlizzardDamage, new CastSpellExtraArgs());

                _tickTimer += TimeSpan.FromMilliseconds(1000);
            }
        }
    }

    [Script] // 190357 - Blizzard (Damage)
    class spell_mage_blizzard_damage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlizzardSlow);
        }

        void HandleSlow(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.BlizzardSlow, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleSlow, 0, SpellEffectName.SchoolDamage));
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
            return spellInfo.GetEffects().Count > 2 && ValidateSpellInfo(SpellIds.CauterizeDot, SpellIds.Cauterized, spellInfo.GetEffect(2).TriggerSpell);
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
            GetTarget().CastSpell(GetTarget(), GetEffectInfo(2).TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
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
        static uint[] SpellsToReset =
        {
            SpellIds.ConeOfCold,
            SpellIds.IceBarrier,
            SpellIds.IceBlock
        };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellsToReset) && ValidateSpellInfo(SpellIds.FrostNova);
        }

        void HandleDummy(uint effIndex)
        {
            foreach (uint spellId in SpellsToReset)
                GetCaster().GetSpellHistory().ResetCooldown(spellId, true);

            GetCaster().GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(SpellIds.FrostNova, GetCastDifficulty()).ChargeCategoryId);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect));
        }
    }

    class CometStormEvent : BasicEvent
    {
        Unit _caster;
        ObjectGuid _originalCastId;
        Position _dest;
        byte _count;

        public CometStormEvent(Unit caster, ObjectGuid originalCastId, Position dest)
        {
            _caster = caster;
            _originalCastId = originalCastId;
            _dest = dest;
        }

        public override bool Execute(ulong time, uint diff)
        {
            Position destPosition = new(_dest.GetPositionX() + RandomHelper.FRand(-3.0f, 3.0f), _dest.GetPositionY() + RandomHelper.FRand(-3.0f, 3.0f), _dest.GetPositionZ());
            _caster.CastSpell(destPosition, SpellIds.CometStormVisual, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetOriginalCastId(_originalCastId));
            ++_count;

            if (_count >= 7)
                return true;

            _caster.m_Events.AddEvent(this, TimeSpan.FromMilliseconds(time) + RandomHelper.RandTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(275)));
            return false;
        }
    }

    [Script] // 153595 - Comet Storm (launch)
    class spell_mage_comet_storm : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CometStormVisual);
        }

        void EffectHit(uint effIndex)
        {
            GetCaster().m_Events.AddEventAtOffset(new CometStormEvent(GetCaster(), GetSpell().m_castId, GetHitDest()), RandomHelper.RandTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(275)));
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(EffectHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 228601 - Comet Storm (damage)
    class spell_mage_comet_storm_damage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CometStormDamage);
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            GetCaster().CastSpell(GetHitDest(), SpellIds.CometStormDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetOriginalCastId(GetSpell().m_originalCastId));
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
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

    // 133 - Fireball
    [Script] // 11366 - Pyroblast
    class spell_mage_firestarter : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Firestarter);
        }

        void CalcCritChance(Unit victim, ref float critChance)
        {
            AuraEffect aurEff = GetCaster().GetAuraEffect(SpellIds.Firestarter, 0);
            if (aurEff != null)
                if (victim.GetHealthPct() >= aurEff.GetAmount())
                    critChance = 100.0f;
        }

        public override void Register()
        {
            OnCalcCritChance.Add(new OnCalcCritChanceHandler(CalcCritChance));
        }
    }

    [Script] // 321712 - Pyroblast
    class spell_mage_firestarter_dots : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Firestarter);
        }

        void CalcCritChance(AuraEffect aurEff, Unit victim, ref float critChance)
        {
            AuraEffect aurEff0 = GetCaster().GetAuraEffect(SpellIds.Firestarter, 0);
            if (aurEff0 != null)
                if (victim.GetHealthPct() >= aurEff0.GetAmount())
                    critChance = 100.0f;
        }

        public override void Register()
        {
            DoEffectCalcCritChance.Add(new EffectCalcCritChanceHandler(CalcCritChance, SpellConst.EffectAll, AuraType.PeriodicDamage));
        }
    }

    // 205029 - Flame On
    class spell_mage_flame_on : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FireBlast)
                && CliDB.SpellCategoryStorage.HasRecord(Global.SpellMgr.GetSpellInfo(SpellIds.FireBlast, Difficulty.None).ChargeCategoryId)
                && spellInfo.GetEffects().Count > 2;
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            amount = -(int)MathFunctions.GetPctOf(GetEffectInfo(2).CalcValue() * Time.InMilliseconds, CliDB.SpellCategoryStorage.LookupByKey(Global.SpellMgr.GetSpellInfo(SpellIds.FireBlast, Difficulty.None).ChargeCategoryId).ChargeRecoveryTime);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ChargeRecoveryMultiplier));
        }
    }

    [Script] // 116 - Frostbolt
    class spell_mage_frostbolt : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.Chilled);
        }

        void HandleChilled()
        {
            Unit target = GetHitUnit();
            if (target != null)
                GetCaster().CastSpell(target, SpellIds.Chilled, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleChilled));
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

    [Script] // 45438 - Ice Block
    class spell_mage_ice_block : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EverwarmSocks);
        }

        void PreventStunWithEverwarmSocks(ref WorldObject target)
        {
            if (GetCaster().HasAura(SpellIds.EverwarmSocks))
                target = null;
        }

        void PreventEverwarmSocks(ref WorldObject target)
        {
            if (!GetCaster().HasAura(SpellIds.EverwarmSocks))
                target = null;
        }

        public override void Register()
        {
            OnObjectTargetSelect.Add(new ObjectTargetSelectHandler(PreventStunWithEverwarmSocks, 0, Targets.UnitCaster));
            OnObjectTargetSelect.Add(new ObjectTargetSelectHandler(PreventEverwarmSocks, 5, Targets.UnitCaster));
            OnObjectTargetSelect.Add(new ObjectTargetSelectHandler(PreventEverwarmSocks, 6, Targets.UnitCaster));
        }
    }

    [Script] // Ice Lance - 30455
    class spell_mage_ice_lance : SpellScript
    {
        List<ObjectGuid> _orderedTargets = new();

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
                if (!thermalVoid.GetSpellInfo().GetEffects().Empty())
                {
                    Aura icyVeins = caster.GetAura(SpellIds.IcyVeins);
                    if (icyVeins != null)
                        icyVeins.SetDuration(icyVeins.GetDuration() + thermalVoid.GetSpellInfo().GetEffect(0).CalcValue(caster) * Time.InMilliseconds);
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

    [Script] // 1463 - Incanter's Flow
    class spell_mage_incanters_flow : AuraScript
    {
        sbyte modifier = 1;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IncantersFlow);
        }

        void HandlePeriodicTick(AuraEffect aurEff)
        {
            // Incanter's flow should not cycle out of combat
            if (!GetTarget().IsInCombat())
                return;

            Aura aura = GetTarget().GetAura(SpellIds.IncantersFlow);
            if (aura != null)
            {
                uint stacks = aura.GetStackAmount();

                // Force always to values between 1 and 5
                if ((modifier == -1 && stacks == 1) || (modifier == 1 && stacks == 5))
                {
                    modifier *= -1;
                    return;
                }

                aura.ModStackAmount(modifier);
            }
            else
                GetTarget().CastSpell(GetTarget(), SpellIds.IncantersFlow, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodicTick, 0, AuraType.PeriodicDummy));
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
                caster.CastSpell(GetTarget(), SpellIds.LivingBombExplosion, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount()));
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

    [Script] // 205021 - Ray of Frost
    class spell_mage_ray_of_frost : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RayOfFrostFingersOfFrost);
        }

        void HandleOnHit()
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(caster, SpellIds.RayOfFrostFingersOfFrost, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleOnHit));
        }
    }

    [Script]
    class spell_mage_ray_of_frost_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RayOfFrostBonus, SpellIds.RayOfFrostFingersOfFrost);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                if (aurEff.GetTickNumber() > 1) // First tick should deal base damage
                    caster.CastSpell(caster, SpellIds.RayOfFrostBonus, true);
            }
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.RemoveAurasDueToSpell(SpellIds.RayOfFrostFingersOfFrost);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDamage));
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
        }
    }
    
    [Script] // 136511 - Ring of Frost
    class spell_mage_ring_of_frost : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RingOfFrostSummon, SpellIds.RingOfFrostFreeze) && !Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon, Difficulty.None).GetEffects().Empty();
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            TempSummon ringOfFrost = GetRingOfFrostMinion();
            if (ringOfFrost)
                GetTarget().CastSpell(ringOfFrost.GetPosition(), SpellIds.RingOfFrostFreeze, new CastSpellExtraArgs(true));
        }

        void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            List<TempSummon> minions = new();
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
            return ValidateSpellInfo(SpellIds.RingOfFrostSummon, SpellIds.RingOfFrostFreeze) && !Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon, Difficulty.None).GetEffects().Empty();
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

    [Script] // 157980 - Supernova
    class spell_mage_supernova : SpellScript
    {
        void HandleDamage(uint effIndex)
        {
            if (GetExplTargetUnit() == GetHitUnit())
            {
                int damage = GetHitDamage();
                MathFunctions.AddPct(ref damage, GetEffectInfo(0).CalcValue());
                SetHitDamage(damage);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage));
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
