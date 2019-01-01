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

        //Misc
        public const uint HunterInsanity = 95809;
        public const uint ShamanExhaustion = 57723;
        public const uint ShamanSated = 57724;
        public const uint PetNetherwindsFatigued = 160455;
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
            AuraEffect effect1 = GetEffect(1);
            if (effect1 == null || !GetTargetApplication().HasEffect(1) ||
                dmgInfo.GetDamage() < GetTarget().GetHealth() ||
                dmgInfo.GetDamage() > GetTarget().GetMaxHealth() * 2 ||
                GetTarget().HasAura(SpellIds.Cauterized))
            {
                PreventDefaultAction();
                return;
            }

            GetTarget().SetHealth(GetTarget().CountPctFromMaxHealth(effect1.GetAmount()));
            GetTarget().CastSpell(GetTarget(), GetAura().GetSpellEffectInfo(2).TriggerSpell, TriggerCastFlags.FullMask);
            GetTarget().CastSpell(GetTarget(), SpellIds.CauterizeDot, TriggerCastFlags.FullMask);
            GetTarget().CastSpell(GetTarget(), SpellIds.Cauterized, TriggerCastFlags.FullMask);
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

    // 11119 - Ignite
    [Script]
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

            SpellInfo igniteDot = Global.SpellMgr.GetSpellInfo(SpellIds.Ignite);
            int pct = aurEff.GetAmount();

            int amount = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), pct) / igniteDot.GetMaxTicks(Difficulty.None));
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
            GetCaster().CastCustomSpell(SpellIds.LivingBombPeriodic, SpellValueMod.BasePoint2, 1, GetHitUnit(), TriggerCastFlags.FullMask);
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
                GetCaster().CastCustomSpell(SpellIds.LivingBombPeriodic, SpellValueMod.BasePoint2, 0, GetHitUnit(), TriggerCastFlags.FullMask);
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
                caster.CastCustomSpell(SpellIds.LivingBombExplosion, SpellValueMod.BasePoint0, aurEff.GetAmount(), GetTarget(), TriggerCastFlags.FullMask);
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
                GetTarget().CastSpell(ringOfFrost.GetPositionX(), ringOfFrost.GetPositionY(), ringOfFrost.GetPositionZ(), SpellIds.RingOfFrostFreeze, true);
        }

        void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            List<TempSummon> minions = new List<TempSummon>();
            GetTarget().GetAllMinionsByEntry(minions, (uint)Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon).GetEffect(0).MiscValue);

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
            float outRadius = Global.SpellMgr.GetSpellInfo(SpellIds.RingOfFrostSummon).GetEffect(0).CalcRadius();
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
