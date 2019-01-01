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
using Framework.GameMath;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

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
        public const uint ColossusSmashEffect = 208086;
        public const uint Execute = 20647;
        public const uint GlyphOfTheBlazingTrail = 123779;
        public const uint GlyphOfHeroicLeap = 159708;
        public const uint GlyphOfHeroicLeapBuff = 133278;
        public const uint HeroicLeapJump = 178368;
        public const uint ImpendingVictory = 202168;
        public const uint ImpendingVictoryHeal = 202166;
        public const uint ImprovedHeroicLeap = 157449;
        public const uint JuggernautCritBonusBuff = 65156;
        public const uint JuggernautCritBonusTalent = 64976;
        public const uint LastStandTriggered = 12976;
        public const uint MortalStrike = 12294;
        public const uint MortalWounds = 213667;
        public const uint RallyingCry = 97463;
        public const uint Rend = 94009;
        public const uint RetaliationDamage = 22858;
        public const uint SecoundWindProcRank1 = 29834;
        public const uint SecoundWindProcRank2 = 29838;
        public const uint SecoundWindTriggerRank1 = 29841;
        public const uint SecoundWindTriggerRank2 = 29842;
        public const uint ShieldSlam = 23922;
        public const uint Shockwave = 46968;
        public const uint ShockwaveStun = 132168;
        public const uint Slam = 50782;
        public const uint Stoicism = 70845;
        public const uint StormBoltStun = 132169;
        public const uint SweepingStrikesExtraAttack1 = 12723;
        public const uint SweepingStrikesExtraAttack2 = 26654;
        public const uint Taunt = 355;
        public const uint TraumaEffect = 215537;
        public const uint UnrelentingAssaultRank1 = 46859;
        public const uint UnrelentingAssaultRank2 = 46860;
        public const uint UnrelentingAssaultTrigger1 = 64849;
        public const uint UnrelentingAssaultTrigger2 = 64850;
        public const uint Vengeance = 76691;
        public const uint Victorious = 32216;
        public const uint VictoriousRushHeal = 118779;
    }

    struct Misc
    {
        public const uint SpellVisualBlazingCharge = 26423;
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
            OnEffectHit.Add(new EffectHandler(HandleDummy, 3, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
                for (uint i = 0; i < 5; ++i)
                {
                    int timeOffset = (int)(6 * i * aurEff.GetPeriod() / 25);
                    Vector4 loc = GetTarget().moveSpline.ComputePosition(timeOffset);
                    GetTarget().SendPlaySpellVisual(new Vector3(loc.X, loc.Y, loc.Z), 0.0f, Misc.SpellVisualBlazingCharge, 0, 0, 1.0f, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(DropFireVisual, 0, AuraType.PeriodicTriggerSpell));
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
            caster.CastCustomSpell(SpellIds.ChargePauseRageDecay, SpellValueMod.BasePoint0, 0, caster, true);
            caster.CastSpell(target, SpellIds.ChargeRootEffect, true);
            caster.CastSpell(target, SpellIds.ChargeSlowEffect, true);
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new EffectHandler(HandleCharge, 0, SpellEffectName.Charge));
        }
    }

    [Script] // 167105 - Colossus Smash 7.1.5
    class spell_warr_colossus_smash_SpellScript : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ColossusSmashEffect);
        }

        void HandleOnHit()
        {
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.ColossusSmashEffect, true);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleOnHit));
        }
    }

    // Updated 4.3.4
    [Script]
    class spell_warr_concussion_blow : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            SetHitDamage((int)MathFunctions.CalculatePct(GetCaster().GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), GetEffectValue()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 2, SpellEffectName.Dummy));
        }
    }

    // Updated 4.3.4
    [Script] // 5308 - Execute
    class spell_warr_execute : SpellScript
    {
        void HandleEffect(uint effIndex)
        {
            /*
            Unit caster = GetCaster();
            if (GetHitUnit())
            {
                SpellInfo spellInfo = GetSpellInfo();
                int rageUsed = Math.Min(200 - spellInfo.CalcPowerCost(caster, spellInfo.SchoolMask), caster.GetPower(PowerType.Rage));
                int newRage = Math.Max(0, caster.GetPower(PowerType.Rage) - rageUsed);

                // Sudden Death rage save
                AuraEffect aurEff = caster.GetAuraEffect(AuraType.ProcTriggerSpell, SpellFamilyNames.Generic, 1989, 0); // Icon SuddenDeath
                if (aurEff != null)
                {
                    int ragesave = aurEff.GetSpellInfo().GetEffect(0).CalcValue() * 10;
                    newRage = Math.Max(newRage, ragesave);
                }

                caster.SetPower(PowerType.Rage, newRage);

                // Formula taken from the DBC: "${10+$AP*0.437*$m1/100}"
                int baseDamage = (int)(10 + caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.437f * GetEffectValue() / 100.0f);
                // Formula taken from the DBC: "${$ap*0.874*$m1/100-1} = 20 rage"
                int moreDamage = (int)(rageUsed * (caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.874f * GetEffectValue() / 100.0f - 1) / 200);
                SetHitDamage(baseDamage + moreDamage);
            }
            */
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // Heroic leap - 6544
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

                    PathGenerator generatedPath = new PathGenerator(GetCaster());
                    generatedPath.SetPathLengthLimit(range);

                    bool result = generatedPath.CalculatePath(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), false, true);
                    if (generatedPath.GetPathType().HasAnyFlag(PathType.Short))
                        return SpellCastResult.OutOfRange;
                    else if (!result || generatedPath.GetPathType().HasAnyFlag(PathType.NoPath))
                    {
                        result = generatedPath.CalculatePath(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), false, false);
                        if (generatedPath.GetPathType().HasAnyFlag(PathType.Short))
                            return SpellCastResult.OutOfRange;
                        else if (!result || generatedPath.GetPathType().HasAnyFlag(PathType.NoPath))
                            return SpellCastResult.NoPath;
                    }
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
                GetCaster().CastSpell(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), SpellIds.HeroicLeapJump, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckElevation));
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // Heroic Leap (triggered by Heroic Leap (6544)) - 178368
    class spell_warr_heroic_leap_jump : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfHeroicLeap,
                SpellIds.GlyphOfHeroicLeapBuff,
                SpellIds.ImprovedHeroicLeap,
                SpellIds.Taunt);
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
            OnEffectHit.Add(new EffectHandler(AfterJump, 1, SpellEffectName.JumpDest));
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
            AfterCast.Add(new CastHandler(HandleAfterCast));
        }
    }

    // 5246 - Intimidating Shout
    [Script]
    class spell_warr_intimidating_shout : SpellScript
    {
        void FilterTargets(List<WorldObject> unitList)
        {
            unitList.Remove(GetExplTargetWorldObject());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitSrcAreaEnemy));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitSrcAreaEnemy));
        }
    }

    // 70844 - Item - Warrior T10 Protection 4P Bonus
    [Script] // 7.1.5
    class spell_warr_item_t10_prot_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Stoicism);
        }

        void HandleProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit target = eventInfo.GetActionTarget();
            int bp0 = (int)MathFunctions.CalculatePct(target.GetMaxHealth(), GetSpellInfo().GetEffect(1).CalcValue());
            target.CastCustomSpell(SpellIds.Stoicism, SpellValueMod.BasePoint0, bp0, (Unit)null, true);
        }

        public override void Register()
        {
            OnProc.Add(new AuraProcHandler(HandleProc));
        }
    }

    // -84583 Lambs to the Slaughter
    [Script]
    class spell_warr_lambs_to_the_slaughter : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MortalStrike, SpellIds.Rend);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Aura aur = eventInfo.GetProcTarget().GetAura(SpellIds.Rend, GetTarget().GetGUID());
            if (aur != null)
                aur.SetDuration(aur.GetSpellInfo().GetMaxDuration(), true);

        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    // Updated 4.3.4
    // 12975 - Last Stand
    [Script]
    class spell_warr_last_stand : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LastStandTriggered);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            int healthModSpellBasePoints0 = (int)(caster.CountPctFromMaxHealth(GetEffectValue()));
            caster.CastCustomSpell(caster, SpellIds.LastStandTriggered, healthModSpellBasePoints0, 0, 0, true, null);
        }

        public override void Register()
        {
            // add dummy effect spell handler to Last Stand
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 12294 - Mortal Strike 7.1.5
    class spell_warr_mortal_strike_SpellScript : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MortalWounds);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.MortalWounds, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 7384 - Overpower
    class spell_warr_overpower : SpellScript
    {
        void HandleEffect(uint effIndex)
        {
            uint spellId = 0;
            if (GetCaster().HasAura(SpellIds.UnrelentingAssaultRank1))
                spellId = SpellIds.UnrelentingAssaultTrigger1;
            else if (GetCaster().HasAura(SpellIds.UnrelentingAssaultRank2))
                spellId = SpellIds.UnrelentingAssaultTrigger2;

            if (spellId == 0)
                return;

            Player target = GetHitPlayer();
            if (target)
                if (target.IsNonMeleeSpellCast(false, false, true)) // UNIT_STATE_CASTING should not be used here, it's present during a tick for instant casts
                    target.CastSpell(target, spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.Any));
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
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleScript(uint effIndex)
        {
            int basePoints0 = (int)(GetHitUnit().CountPctFromMaxHealth(GetEffectValue()));

            GetCaster().CastCustomSpell(GetHitUnit(), SpellIds.RallyingCry, basePoints0, 0, 0, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    // 94009 - Rend
    [Script]
    class spell_warr_rend : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                canBeRecalculated = false;

                // $0.25 * (($MWB + $mwb) / 2 + $AP / 14 * $MWS) bonus per tick
                float ap = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
                int mws = (int)caster.GetBaseAttackTime(WeaponAttackType.BaseAttack);
                float mwbMin = caster.GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage);
                float mwbMax = caster.GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage);
                float mwb = ((mwbMin + mwbMax) / 2 + ap * mws / 14000) * 0.25f;
                amount += (int)(caster.ApplyEffectModifiers(GetSpellInfo(), aurEff.GetEffIndex(), mwb));
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDamage));
        }
    }

    // 20230 - Retaliation
    [Script]
    class spell_warr_retaliation : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RetaliationDamage);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            // check attack comes not from behind and warrior is not stunned
            return GetTarget().isInFront(eventInfo.GetProcTarget(), MathFunctions.PI) && !GetTarget().HasUnitState(UnitState.Stunned);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.RetaliationDamage, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    // 64380, 65941 - Shattering Throw
    [Script]
    class spell_warr_shattering_throw : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            // remove shields, will still display immune to damage part
            Unit target = GetHitUnit();
            if (target)
                target.RemoveAurasWithMechanic(1 << (int)Mechanics.ImmuneShield, AuraRemoveMode.EnemySpell);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // Updated 4.3.4
    [Script]
    class spell_warr_slam : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Slam);
        }

        void HandleDummy(uint effIndex)
        {
            if (GetHitUnit())
                GetCaster().CastCustomSpell(SpellIds.Slam, SpellValueMod.BasePoint0, GetEffectValue(), GetHitUnit(), TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_warr_second_wind_proc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SecoundWindProcRank1, SpellIds.SecoundWindProcRank2, SpellIds.SecoundWindTriggerRank1, SpellIds.SecoundWindTriggerRank2);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetProcTarget() == GetTarget())
                return false;
            if (eventInfo.GetDamageInfo().GetSpellInfo() == null ||
                (eventInfo.GetDamageInfo().GetSpellInfo().GetAllEffectsMechanicMask() & ((1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Stun))) == 0)
                return false;
            return true;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            uint spellId = 0;

            if (GetSpellInfo().Id == SpellIds.SecoundWindProcRank1)
                spellId = SpellIds.SecoundWindTriggerRank1;
            else if (GetSpellInfo().Id == SpellIds.SecoundWindProcRank2)
                spellId = SpellIds.SecoundWindTriggerRank2;
            if (spellId == 0)
                return;

            GetTarget().CastSpell(GetTarget(), spellId, true, null, aurEff);

        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script]
    class spell_warr_second_wind_trigger : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount = (int)(GetUnitOwner().CountPctFromMaxHealth(amount));
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.PeriodicHeal));
        }
    }

    [Script] // 46968 - Shockwave
    class spell_warr_shockwave : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            if (!ValidateSpellInfo(SpellIds.Shockwave, SpellIds.ShockwaveStun))
                return false;

            return spellInfo.GetEffect(0) != null && spellInfo.GetEffect(3) != null;
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleStun(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.ShockwaveStun, true);
            ++_targetCount;
        }

        // Cooldown reduced by 20 sec if it strikes at least 3 targets.
        void HandleAfterCast()
        {
            if (_targetCount >= (uint)GetSpellInfo().GetEffect(0).CalcValue())
                GetCaster().ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, -(GetSpellInfo().GetEffect(3).CalcValue() * Time.InMilliseconds));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleStun, 0, SpellEffectName.Dummy));
            AfterCast.Add(new CastHandler(HandleAfterCast));
        }

        uint _targetCount;
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
            OnEffectHitTarget.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy));
        }
    }

    // 52437 - Sudden Death
    [Script]
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
            if (player)
                player.GetSpellHistory().ResetCooldown(SpellIds.ColossusSmash, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real)); // correct?
        }
    }

    // 12328, 18765, 35429 - Sweeping Strikes
    [Script]
    class spell_warr_sweeping_strikes : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SweepingStrikesExtraAttack1, SpellIds.SweepingStrikesExtraAttack2);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            _procTarget = eventInfo.GetActor().SelectNearbyTarget(eventInfo.GetProcTarget());
            return _procTarget;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo != null)
            {
                SpellInfo spellInfo = damageInfo.GetSpellInfo();
                if (spellInfo != null && (spellInfo.Id == SpellIds.BladestormPeriodicWhirlwind || (spellInfo.Id == SpellIds.Execute && !_procTarget.HasAuraState(AuraStateType.HealthLess20Percent))))
                {
                    // If triggered by Execute (while target is not under 20% hp) or Bladestorm deals normalized weapon damage
                    GetTarget().CastSpell(_procTarget, SpellIds.SweepingStrikesExtraAttack2, true, null, aurEff);
                }
                else
                {
                    int damage = (int)damageInfo.GetDamage();
                    GetTarget().CastCustomSpell(SpellIds.SweepingStrikesExtraAttack1, SpellValueMod.BasePoint0, damage, _procTarget, true, null, aurEff);
                }
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        Unit _procTarget;
    }

    // -46951 - Sword and Board
    [Script]
    class spell_warr_sword_and_board : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShieldSlam);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // Remove cooldown on Shield Slam
            Player player = GetTarget().ToPlayer();
            if (player)
                player.GetSpellHistory().ResetCooldown(SpellIds.ShieldSlam, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
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
            //Get the Remaining Damage from the aura (if exist)
            int remainingDamage = (int)target.GetRemainingPeriodicAmount(target.GetGUID(), SpellIds.TraumaEffect, AuraType.PeriodicDamage);
            //Get 25% of damage from the spell casted (Slam & Whirlwind) plus Remaining Damage from Aura
            int damage = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()) / Global.SpellMgr.GetSpellInfo(SpellIds.TraumaEffect).GetMaxTicks(Difficulty.None)) + remainingDamage;
            GetCaster().CastCustomSpell(SpellIds.TraumaEffect, SpellValueMod.BasePoint0, damage, target, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
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
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
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
            if (procInfo.GetActor().GetTypeId() == TypeId.Player && procInfo.GetActor().GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.WarriorFury)
                PreventDefaultAction();

            procInfo.GetActor().GetSpellHistory().ResetCooldown(SpellIds.ImpendingVictory, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleOnProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 34428 - Victory Rush
    class spell_warr_victory_rush : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Victorious, SpellIds.VictoriousRushHeal);
        }

        void HandleHeal()
        {
            Unit caster = GetCaster();

            caster.CastSpell(caster, SpellIds.VictoriousRushHeal, true);
            caster.RemoveAurasDueToSpell(SpellIds.Victorious);
        }

        public override void Register()
        {
            AfterCast.Add(new CastHandler(HandleHeal));
        }
    }

    // 50720 - Vigilance
    [Script]
    class spell_warr_vigilance : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Vengeance);
        }

        public override bool Load()
        {
            //_procTarget = null;
            return true;
        }

        /*bool CheckProc(ProcEventInfo eventInfo)
        {
            _procTarget = GetCaster();
            return _procTarget && eventInfo.GetDamageInfo() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            int damage = (int)(MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetSpellInfo().GetEffect(1).CalcValue()));

            GetTarget().CastSpell(_procTarget, SpellIds.VigilanceProc, true, null, aurEff);
            _procTarget.CastCustomSpell(_procTarget, SpellIds.Vengeance, damage, damage, damage, true, null, aurEff);
        }*/

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                if (caster.HasAura(SpellIds.Vengeance))
                    caster.RemoveAurasDueToSpell(SpellIds.Vengeance);
            }
        }

        public override void Register()
        {
            //DoCheckProc.Add(new CheckProcHandler(CheckProc));
            //OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
            OnEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real));
        }

        //Unit _procTarget;
    }

    // 50725 Vigilance
    [Script]
    class spell_warr_vigilance_trigger : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            // Remove Taunt cooldown
            Player target = GetHitPlayer();
            if (target)
                target.GetSpellHistory().ResetCooldown(SpellIds.Taunt, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }
}
