﻿/*
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
using Game.Groups;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Mage
{
    internal struct SpellIds
    {
        public const uint BlazingBarrierTrigger = 235314;
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
        public const uint TouchOfTheMagiAura = 210824;
        public const uint TouchOfTheMagiExplode = 210833;

        //Misc
        public const uint HunterInsanity = 95809;
        public const uint ShamanExhaustion = 57723;
        public const uint ShamanSated = 57724;
        public const uint PetNetherwindsFatigued = 160455;
    }

    [Script] // 235313 - Blazing Barrier
    internal class spell_mage_blazing_barrier : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BlazingBarrierTrigger);
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            var caster = GetCaster();
            if (caster)
                amount = (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            var caster = eventInfo.GetDamageInfo().GetVictim();
            var target = eventInfo.GetDamageInfo().GetAttacker();

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
    internal class spell_mage_burning_determination : AuraScript
    {
        private bool CheckProc(ProcEventInfo eventInfo)
        {
            var spellInfo = eventInfo.GetSpellInfo();
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
    internal class spell_mage_cauterize : SpellScript
    {
        private void SuppressSpeedBuff(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new EffectHandler(SuppressSpeedBuff, 2, SpellEffectName.TriggerSpell));
        }
    }

    [Script]
    internal class spell_mage_cauterize_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffect(2) != null && ValidateSpellInfo(SpellIds.CauterizeDot, SpellIds.Cauterized, spellInfo.GetEffect(2).TriggerSpell);
        }

        private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            var effectInfo = GetEffect(1);
            if (effectInfo == null || !GetTargetApplication().HasEffect(1) ||
                dmgInfo.GetDamage() < GetTarget().GetHealth() ||
                dmgInfo.GetDamage() > GetTarget().GetMaxHealth() * 2 ||
                GetTarget().HasAura(SpellIds.Cauterized))
            {
                PreventDefaultAction();
                return;
            }

            GetTarget().SetHealth(GetTarget().CountPctFromMaxHealth(effectInfo.GetAmount()));
            GetTarget().CastSpell(GetTarget(), GetSpellInfo().GetEffect(2).TriggerSpell, TriggerCastFlags.FullMask);
            GetTarget().CastSpell(GetTarget(), SpellIds.CauterizeDot, TriggerCastFlags.FullMask);
            GetTarget().CastSpell(GetTarget(), SpellIds.Cauterized, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new EffectAbsorbHandler(HandleAbsorb, 0));
        }
    }

    [Script] // 235219 - Cold Snap
    internal class spell_mage_cold_snap : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConeOfCold, SpellIds.FrostNova, SpellIds.IceBarrier, SpellIds.IceBlock);
        }

        private void HandleDummy(uint effIndex)
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
    internal class spell_mage_cone_of_cold : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConeOfColdSlow);
        }

        private void HandleSlow(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ConeOfColdSlow, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleSlow, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 190336 - Conjure Refreshment
    internal class spell_mage_conjure_refreshment : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ConjureRefreshment, SpellIds.ConjureRefreshmentTable);
        }

        private void HandleDummy(uint effIndex)
        {
            var caster = GetCaster().ToPlayer();
            if (caster)
            {
                var group = caster.GetGroup();
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

    [Script] // 44544 - Fingers of Frost
    internal class spell_mage_fingers_of_frost : AuraScript
    {
        private void SuppressWarning(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            PreventDefaultAction();
        }

        private void DropFingersOfFrost(ProcEventInfo eventInfo)
        {
            GetAura().ModStackAmount(-1);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(SuppressWarning, 1, AuraType.Dummy));
            AfterProc.Add(new AuraProcHandler(DropFingersOfFrost));
        }
    }
    
    [Script] // 11426 - Ice Barrier
    internal class spell_mage_ice_barrier : AuraScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.Chilled);
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            var caster = GetCaster();
            if (caster)
                amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 10.0f);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            var caster = eventInfo.GetDamageInfo().GetVictim();
            var target = eventInfo.GetDamageInfo().GetAttacker();

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
    internal class spell_mage_ice_lance : SpellScript
    {
        private List<ObjectGuid> _orderedTargets = new List<ObjectGuid>();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.IceLanceTrigger, SpellIds.ThermalVoid, SpellIds.IcyVeins, SpellIds.ChainReactionDummy, SpellIds.ChainReaction, SpellIds.FingersOfFrost);
        }

        private void IndexTarget(uint effIndex)
        {
            _orderedTargets.Add(GetHitUnit().GetGUID());
        }

        private void HandleOnHit(uint effIndex)
        {
            var caster = GetCaster();
            var target = GetHitUnit();

            var index = _orderedTargets.IndexOf(target.GetGUID());

            if (index == 0 // only primary target triggers these benefits
                && target.HasAuraState(AuraStateType.Frozen, GetSpellInfo(), caster))
            {
                // Thermal Void
                var thermalVoid = caster.GetAura(SpellIds.ThermalVoid);
                if (thermalVoid != null)
                {
                    var thermalVoidEffect = thermalVoid.GetSpellInfo().GetEffect(0);
                    if (thermalVoidEffect != null)
                    {
                        var icyVeins = caster.GetAura(SpellIds.IcyVeins);
                        if (icyVeins != null)
                            icyVeins.SetDuration(icyVeins.GetDuration() + thermalVoidEffect.CalcValue(caster) * Time.InMilliseconds);
                    }
                }

                // Chain Reaction
                if (caster.HasAura(SpellIds.ChainReactionDummy))
                    caster.CastSpell(caster, SpellIds.ChainReaction, true);
            }

            // put target index for chain value multiplier into EFFECT_1 base points, otherwise triggered spell doesn't know which damage multiplier to apply
            caster.CastCustomSpell(SpellIds.IceLanceTrigger, SpellValueMod.BasePoint1, index, target, true);
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new EffectHandler(IndexTarget, 0, SpellEffectName.ScriptEffect));
            OnEffectHitTarget.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 228598 - Ice Lance
    internal class spell_mage_ice_lance_damage : SpellScript
    {
        private void ApplyDamageMultiplier(uint effIndex)
        {
            var spellValue = GetSpellValue();
            if ((spellValue.CustomBasePointsMask & (1 << 1)) != 0)
            {
                var originalDamage = GetHitDamage();
                var targetIndex = (float)spellValue.EffectBasePoints[1];
                var multiplier = MathF.Pow(GetEffectInfo().CalcDamageMultiplier(GetCaster(), GetSpell()), targetIndex);
                SetHitDamage((int)(originalDamage * multiplier));
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(ApplyDamageMultiplier, 0, SpellEffectName.SchoolDamage));
        }
    }
    
    [Script] // 11119 - Ignite
    internal class spell_mage_ignite : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Ignite);
        }

        private bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcTarget();
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            var igniteDot = Global.SpellMgr.GetSpellInfo(SpellIds.Ignite, GetCastDifficulty());
            var pct = aurEff.GetAmount();

            var amount = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), pct) / igniteDot.GetMaxTicks());
            amount += (int)eventInfo.GetProcTarget().GetRemainingPeriodicAmount(eventInfo.GetActor().GetGUID(), SpellIds.Ignite, AuraType.PeriodicDamage);
            GetTarget().CastCustomSpell(SpellIds.Ignite, SpellValueMod.BasePoint0, amount, eventInfo.GetProcTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // 37447 - Improved Mana Gems
    [Script] // 61062 - Improved Mana Gems
    internal class spell_mage_imp_mana_gems : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ManaSurge);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
    internal class spell_mage_living_bomb : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LivingBombPeriodic);
        }

        private void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetCaster().CastCustomSpell(SpellIds.LivingBombPeriodic, SpellValueMod.BasePoint2, 1, GetHitUnit(), TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 44461 - Living Bomb
    internal class spell_mage_living_bomb_explosion : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.NeedsExplicitUnitTarget() && ValidateSpellInfo(SpellIds.LivingBombPeriodic);
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            targets.Remove(GetExplTargetWorldObject());
        }

        private void HandleSpread(uint effIndex)
        {
            if (GetSpellValue().EffectBasePoints[0] > 0)
                GetCaster().CastCustomSpell(SpellIds.LivingBombPeriodic, SpellValueMod.BasePoint2, 0, GetHitUnit(), TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleSpread, 1, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 217694 - Living Bomb
    internal class spell_mage_living_bomb_periodic : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LivingBombExplosion);
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            var caster = GetCaster();
            if (caster)
                caster.CastCustomSpell(SpellIds.LivingBombExplosion, SpellValueMod.BasePoint0, aurEff.GetAmount(), GetTarget(), TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 2, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // @todo move out of here and rename - not a mage spell
    [Script] // 32826 - Polymorph (Visual)
    internal class spell_mage_polymorph_visual : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PolymorhForms);
        }

        private void HandleDummy(uint effIndex)
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

        private const uint NPC_AUROSALIA = 18744;

        private uint[] PolymorhForms =
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
    internal class spell_mage_prismatic_barrier : AuraScript
    {
        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            var caster = GetCaster();
            if (caster)
                amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        }
    }

    [Script] // 136511 - Ring of Frost
    internal class spell_mage_ring_of_frost : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RingOfFrostSummon, SpellIds.RingOfFrostFreeze);
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            var ringOfFrost = GetRingOfFrostMinion();
            if (ringOfFrost)
                GetTarget().CastSpell(ringOfFrost.GetPositionX(), ringOfFrost.GetPositionY(), ringOfFrost.GetPositionZ(), SpellIds.RingOfFrostFreeze, true);
        }

        private void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            var minions = new List<TempSummon>();
            GetTarget().GetAllMinionsByEntry(minions, (uint)Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon, GetCastDifficulty()).GetEffect(0).MiscValue);

            // Get the last summoned RoF, save it and despawn older ones
            foreach (var summon in minions)
            {
                var ringOfFrost = GetRingOfFrostMinion();
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

        private TempSummon GetRingOfFrostMinion()
        {
            var creature = ObjectAccessor.GetCreature(GetOwner(), _ringOfFrostGUID);
            if (creature)
                return creature.ToTempSummon();
            return null;
        }

        private ObjectGuid _ringOfFrostGUID;
    }

    [Script] // 82691 - Ring of Frost (freeze efect)
    internal class spell_mage_ring_of_frost_freeze : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RingOfFrostSummon, SpellIds.RingOfFrostFreeze);
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            var dest = GetExplTargetDest();
            var outRadius = Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon, GetCastDifficulty()).GetEffect(0).CalcRadius();
            var inRadius = 6.5f;

            targets.RemoveAll(target =>
            {
                var unit = target.ToUnit();
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
    internal class spell_mage_ring_of_frost_freeze_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RingOfFrostDummy);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
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
    internal class spell_mage_time_warp : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TemporalDisplacement, SpellIds.HunterInsanity, SpellIds.ShamanExhaustion, SpellIds.ShamanSated, SpellIds.PetNetherwindsFatigued);
        }

        private void RemoveInvalidTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.TemporalDisplacement));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HunterInsanity));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.ShamanExhaustion));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.ShamanSated));
        }

        private void ApplyDebuff()
        {
            var target = GetHitUnit();
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
    internal class spell_mage_touch_of_the_magi_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TouchOfTheMagiExplode);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            var damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                if (damageInfo.GetAttacker() == GetCaster() && damageInfo.GetVictim() == GetTarget())
                {
                    var extra = MathFunctions.CalculatePct(damageInfo.GetDamage(), 25);
                    if (extra > 0)
                        aurEff.ChangeAmount(aurEff.GetAmount() + (int)extra);
                }
            }
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            var amount = aurEff.GetAmount();
            if (amount == 0 || GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            var caster = GetCaster();
            if (caster != null)
                caster.CastCustomSpell(SpellIds.TouchOfTheMagiExplode, SpellValueMod.BasePoint0, amount, GetTarget(), TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }
    
    [Script] //228597 - Frostbolt   84721  - Frozen Orb   190357 - Blizzard
    internal class spell_mage_trigger_chilled : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.Chilled);
        }

        private void HandleChilled()
        {
            var target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.Chilled, true);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleChilled));
        }
    }

    [Script] // 33395 Water Elemental's Freeze
    internal class spell_mage_water_elemental_freeze : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FingersOfFrost);
        }

        private void HandleImprovedFreeze()
        {
            var owner = GetCaster().GetOwner();
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
