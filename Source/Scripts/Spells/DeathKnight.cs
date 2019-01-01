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
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.DeathKnight
{
    struct SpellIds
    {
        public const uint ArmyFleshBeastTransform = 127533;
        public const uint ArmyGeistTransform = 127534;
        public const uint ArmyNorthrendSkeletonTransform = 127528;
        public const uint ArmySkeletonTransform = 127527;
        public const uint ArmySpikedGhoulTransform = 127525;
        public const uint ArmySuperZombieTransform = 127526;
        public const uint Blood = 137008;
        public const uint BloodPlague = 55078;
        public const uint BloodShieldAbsorb = 77535;
        public const uint BloodShieldMastery = 77513;
        public const uint CorpseExplosionTriggered = 43999;
        public const uint DeathAndDecayDamage = 52212;
        public const uint DeathCoilDamage = 47632;
        public const uint DeathGripDummy = 243912;
        public const uint DeathGripJump = 49575;
        public const uint DeathGripTaunt = 51399;
        public const uint DeathStrikeHeal = 45470;
        public const uint DeathStrikeOffhand = 66188;
        public const uint FesteringWound = 194310;
        public const uint Frost = 137006;
        public const uint FrostFever = 55095;
        public const uint GlyphOfFoulMenagerie = 58642;
        public const uint GlyphOfTheGeist = 58640;
        public const uint GlyphOfTheSkeleton = 146652;
        public const uint MarkOfBloodHeal = 206945;
        public const uint NecrosisEffect = 216974;
        public const uint RaiseDeadSummon = 52150;
        public const uint RecentlyUsedDeathStrike = 180612;
        public const uint RunicPowerEnergize = 49088;
        public const uint RunicReturn = 61258;
        public const uint SludgeBelcher = 207313;
        public const uint SludgeBelcherSummon = 212027;
        public const uint TighteningGrasp = 206970;
        public const uint TighteningGraspSlow = 143375;
        public const uint Unholy = 137007;
        public const uint UnholyVigor = 196263;
        public const uint VolatileShielding = 207188;
        public const uint VolatileShieldingDamage = 207194;

        public static uint[] ArmyTransforms =
        {
            ArmyFleshBeastTransform,
            ArmyGeistTransform,
            ArmyNorthrendSkeletonTransform,
            ArmySkeletonTransform,
            ArmySpikedGhoulTransform,
            ArmySuperZombieTransform
        };
    }

    struct CreatureIds
    {
        public const uint DancingRuneWeapon = 27893;
    }

    [Script] // 70656 - Advantage (T10 4P Melee Bonus)
    class spell_dk_advantage_t10_4p : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit caster = eventInfo.GetActor();
            if (caster)
            {
                Player player = caster.ToPlayer();
                if (!player || caster.GetClass() != Class.Deathknight)
                    return false;

                for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                    if (player.GetRuneCooldown(i) == 0)
                        return false;

                return true;
            }

            return false;
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
        }
    }

    [Script] // 48707 - Anti-Magic Shell
    class spell_dk_anti_magic_shell : AuraScript
    {
        public spell_dk_anti_magic_shell()
        {
            absorbPct = 0;
            maxHealth = 0;
            absorbedAmount = 0;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RunicPowerEnergize, SpellIds.VolatileShielding);
        }

        public override bool Load()
        {
            absorbPct = GetSpellInfo().GetEffect(1).CalcValue(GetCaster());
            maxHealth = (int)GetCaster().GetMaxHealth();
            absorbedAmount = 0;
            return true;
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount = MathFunctions.CalculatePct(maxHealth, absorbPct);
        }

        void Trigger(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            absorbedAmount += absorbAmount;

            if (!GetTarget().HasAura(SpellIds.VolatileShielding))
            {
                int bp = (int)(2 * absorbAmount * 100 / maxHealth);
                GetTarget().CastCustomSpell(SpellIds.RunicPowerEnergize, SpellValueMod.BasePoint0, bp, GetTarget(), true, null, aurEff);
            }
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            AuraEffect volatileShielding = GetTarget().GetAuraEffect(SpellIds.VolatileShielding, 1);
            if (volatileShielding != null)
            {
                int damage = (int)MathFunctions.CalculatePct(absorbedAmount, volatileShielding.GetAmount());
                GetTarget().CastCustomSpell(SpellIds.VolatileShieldingDamage, SpellValueMod.BasePoint0, damage, null, TriggerCastFlags.FullMask, null, volatileShielding);
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            AfterEffectAbsorb.Add(new EffectAbsorbHandler(Trigger, 0));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real));
        }

        int absorbPct;
        int maxHealth;
        uint absorbedAmount;
    }

    [Script] // 127517 - Army Transform    // 6.x, does this belong here or in spell_generic? where do we cast this? sniffs say this is only cast when caster has glyph of foul menagerie.
    class spell_dk_army_transform : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfFoulMenagerie);
        }

        public override bool Load()
        {
            return GetCaster().IsGuardian();
        }

        SpellCastResult CheckCast()
        {
            Unit owner = GetCaster().GetOwner();
            if (owner)
                if (owner.HasAura(SpellIds.GlyphOfFoulMenagerie))
                    return SpellCastResult.SpellCastOk;

            return SpellCastResult.SpellUnavailable;
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.ArmyTransforms.SelectRandom(), true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 50842 - Blood Boil
    class spell_dk_blood_boil : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodPlague);
        }

        void HandleEffect()
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.BloodPlague, true);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleEffect));
        }
    }

    [Script] // 49028 - Dancing Rune Weapon 7.1.5
    class spell_dk_dancing_rune_weapon : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            if (Global.ObjectMgr.GetCreatureTemplate(CreatureIds.DancingRuneWeapon) == null)
                return false;
            return true;
        }

        // This is a port of the old switch hack in Unit.cpp, it's not correct
        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (!caster)
                return;

            Unit drw = null;
            foreach (Unit controlled in caster.m_Controlled)
            {
                if (controlled.GetEntry() == CreatureIds.DancingRuneWeapon)
                {
                    drw = controlled;
                    break;
                }
            }

            if (!drw || !drw.GetVictim())
                return;

            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null)
                return;

            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int amount = (int)(damageInfo.GetDamage() / 2);
            SpellNonMeleeDamage log = new SpellNonMeleeDamage(drw, drw.GetVictim(), spellInfo.Id, spellInfo.GetSpellXSpellVisualId(drw), spellInfo.GetSchoolMask());
            log.damage = (uint)amount;
            drw.DealDamage(drw.GetVictim(), (uint)amount, null, DamageEffectType.Direct, spellInfo.GetSchoolMask(), spellInfo, true);
            drw.SendSpellNonMeleeDamageLog(log);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // 43265 - Death and Decay
    class spell_dk_death_and_decay : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TighteningGrasp, SpellIds.TighteningGraspSlow);
        }

        void HandleDummy()
        {
            if (GetCaster().HasAura(SpellIds.TighteningGrasp))
            {
                Position pos = GetExplTargetDest();
                if (pos != null)
                    GetCaster().CastSpell(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), SpellIds.TighteningGraspSlow, true);
            }
        }

        public override void Register()
        {
            OnCast.Add(new CastHandler(HandleDummy));
        }
    }

    [Script]
    class spell_dk_death_and_decay_AuraScript: AuraScript
    {
        void HandleDummyTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster)
                caster.CastSpell(GetTarget(), SpellIds.DeathAndDecayDamage, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 2, AuraType.PeriodicDummy));
        }
    }

    [Script] // 47541 - Death Coil
    class spell_dk_death_coil : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.DeathCoilDamage, SpellIds.Unholy, SpellIds.UnholyVigor);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.CastSpell(GetHitUnit(), SpellIds.DeathCoilDamage, true);

            AuraEffect unholyAura = caster.GetAuraEffect(SpellIds.Unholy, 6);
            if (unholyAura != null) // can be any effect, just here to send SPELL_FAILED_DONT_REPORT on failure
                caster.CastSpell(caster, SpellIds.UnholyVigor, true, null, unholyAura);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 52751 - Death Gate
    class spell_dk_death_gate : SpellScript
    {
        SpellCastResult CheckClass()
        {
            if (GetCaster().GetClass() != Class.Deathknight)
            {
                SetCustomCastResultMessage(SpellCustomErrors.MustBeDeathKnight);
                return SpellCastResult.CustomError;
            }

            return SpellCastResult.SpellCastOk;
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Unit target = GetHitUnit();
            if (target)
                target.CastSpell(target, (uint)GetEffectValue(), false);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckClass));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] //49576 - Death Grip Initial
    class spell_dk_death_grip_initial : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DeathGripDummy, SpellIds.DeathGripJump, SpellIds.Blood, SpellIds.DeathGripTaunt);
        }

        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            // Death Grip should not be castable while jumping/falling
            if (caster.HasUnitState(UnitState.Jumping) || caster.HasUnitMovementFlag(MovementFlag.Falling))
                return SpellCastResult.Moving;

            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.DeathGripDummy, true);
            GetHitUnit().CastSpell(GetCaster(), SpellIds.DeathGripJump, true);
            if (GetCaster().HasAura(SpellIds.Blood))
                GetCaster().CastSpell(GetHitUnit(), SpellIds.DeathGripTaunt, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 48743 - Death Pact
    class spell_dk_death_pact : AuraScript
    {
        void HandleCalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            if (caster)
                amount = (int)caster.CountPctFromMaxHealth(amount);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(HandleCalcAmount, 1, AuraType.SchoolHealAbsorb));
        }
    }

    [Script] // 49998 - Death Strike
    class spell_dk_death_strike : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DeathStrikeHeal, SpellIds.BloodShieldMastery, SpellIds.BloodShieldAbsorb,
                SpellIds.RecentlyUsedDeathStrike, SpellIds.Frost, SpellIds.DeathStrikeOffhand);
        }

        void HandleHeal(uint effIndex)
        {
            Unit caster = GetCaster();
            //TODO: heal = std::min(10% health, 20% of all damage taken in last 5 seconds)
            int heal = (int)MathFunctions.CalculatePct(caster.GetMaxHealth(), GetSpellInfo().GetEffect(4).CalcValue());
            caster.CastCustomSpell(SpellIds.DeathStrikeHeal, SpellValueMod.BasePoint0, heal, caster, true);

            AuraEffect aurEff = caster.GetAuraEffect(SpellIds.BloodShieldMastery, 0);
            if (aurEff != null)
                caster.CastCustomSpell(SpellIds.BloodShieldAbsorb, SpellValueMod.BasePoint0, MathFunctions.CalculatePct(heal, aurEff.GetAmount()), caster);

            if (caster.HasAura(SpellIds.Frost))
                caster.CastSpell(GetHitUnit(), SpellIds.DeathStrikeOffhand, true);
        }

        void TriggerRecentlyUsedDeathStrike()
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.RecentlyUsedDeathStrike, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleHeal, 1, SpellEffectName.WeaponPercentDamage));
            AfterCast.Add(new CastHandler(TriggerRecentlyUsedDeathStrike));
        }
    }

    [Script] // 85948 - Festering Strike
    class spell_dk_festering_strike : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FesteringWound);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastCustomSpell(SpellIds.FesteringWound, SpellValueMod.AuraStack, GetEffectValue(), GetHitUnit(), TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 2, SpellEffectName.Dummy));
        }
    }

    [Script] // 47496 - Explode, Ghoul spell for Corpse Explosion
    class spell_dk_ghoul_explode : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CorpseExplosionTriggered);
        }

        void HandleDamage(uint effIndex)
        {
            SetHitDamage((int)GetCaster().CountPctFromMaxHealth(GetEffectInfo(2).CalcValue(GetCaster())));
        }

        void Suicide(uint effIndex)
        {
            Unit unitTarget = GetHitUnit();
            if (unitTarget)
            {
                // Corpse Explosion (Suicide)
                unitTarget.CastSpell(unitTarget, SpellIds.CorpseExplosionTriggered, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage));
            OnEffectHitTarget.Add(new EffectHandler(Suicide, 1, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 206940 - Mark of Blood
    class spell_dk_mark_of_blood : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MarkOfBloodHeal);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster)
                caster.CastSpell(eventInfo.GetProcTarget(), SpellIds.MarkOfBloodHeal, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 207346 - Necrosis
    class spell_dk_necrosis : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.NecrosisEffect);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.NecrosisEffect, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 121916 - Glyph of the Geist (Unholy)    // 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast on raise dead.
    class spell_dk_pet_geist_transform : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfTheGeist);
        }

        public override bool Load()
        {
            return GetCaster().IsPet();
        }

        SpellCastResult CheckCast()
        {
            Unit owner = GetCaster().GetOwner();
            if (owner)
                if (owner.HasAura(SpellIds.GlyphOfTheGeist))
                    return SpellCastResult.SpellCastOk;

            return SpellCastResult.SpellUnavailable;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    }

    [Script] // 147157 Glyph of the Skeleton (Unholy)    // 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast on raise dead.
    class spell_dk_pet_skeleton_transform : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfTheSkeleton);
        }

        SpellCastResult CheckCast()
        {
            Unit owner = GetCaster().GetOwner();
            if (owner)
                if (owner.HasAura(SpellIds.GlyphOfTheSkeleton))
                    return SpellCastResult.SpellCastOk;

            return SpellCastResult.SpellUnavailable;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    }

    [Script] // 61257 - Runic Power Back on Snare/Root 7.1.5
    class spell_dk_pvp_4p_bonus : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RunicReturn);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null)
                return false;

            return (spellInfo.GetAllEffectsMechanicMask() & ((1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Snare))) != 0;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActionTarget().CastSpell((Unit)null, SpellIds.RunicReturn, true);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 46584 - Raise Dead
    class spell_dk_raise_dead : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RaiseDeadSummon, SpellIds.SludgeBelcher, SpellIds.SludgeBelcherSummon);
        }

        void HandleDummy(uint effIndex)
        {
            uint spellId = SpellIds.RaiseDeadSummon;
            if (GetCaster().HasAura(SpellIds.SludgeBelcher))
                spellId = SpellIds.SludgeBelcherSummon;

            GetCaster().CastSpell((Unit)null, spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 115994 - Unholy Blight
    class spell_dk_unholy_blight : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FrostFever, SpellIds.BloodPlague);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.FrostFever, true);
            GetCaster().CastSpell(GetHitUnit(), SpellIds.BloodPlague, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 55233 - Vampiric Blood
    class spell_dk_vampiric_blood : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount = (int)GetUnitOwner().CountPctFromMaxHealth(amount);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseHealth2));
        }
    }
}
