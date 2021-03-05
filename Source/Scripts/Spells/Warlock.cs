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
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Warlock
{
    struct SpellIds
    {
        public const uint CreateHealthstone = 23517;
        public const uint DemonicCircleAllowCast = 62388;
        public const uint DemonicCircleSummon = 48018;
        public const uint DemonicCircleTeleport = 48020;
        public const uint DevourMagicHeal = 19658;
        public const uint GlyphOfDemonTraining = 56249;
        public const uint GlyphOfSoulSwap = 56226;
        public const uint GlyphOfSuccubus = 56250;
        public const uint ImprovedHealthFunnelBuffR1 = 60955;
        public const uint ImprovedHealthFunnelBuffR2 = 60956;
        public const uint ImprovedHealthFunnelR1 = 18703;
        public const uint ImprovedHealthFunnelR2 = 18704;
        public const uint RainOfFire = 5740;
        public const uint RainOfFireDamage = 42223;
        public const uint SeedOfCorruptionDamage = 27285;
        public const uint SeedOfCorruptionGeneric = 32865;
        public const uint Soulshatter = 32835;
        public const uint SoulSwapCdMarker = 94229;
        public const uint SoulSwapOverride = 86211;
        public const uint SoulSwapModCost = 92794;
        public const uint SoulSwapDotMarker = 92795;
        public const uint UnstableAffliction = 30108;
        public const uint UnstableAfflictionDispel = 31117;
        public const uint Shadowflame = 37378;
        public const uint Flameshadow = 37379;

        public const uint GenReplenishment = 57669;
        public const uint PriestShadowWordDeath = 32409;
    }

    [Script] // 710 - Banish
    class spell_warl_banish : SpellScript
    {
        void HandleBanish(SpellMissInfo missInfo)
        {
            if (missInfo != SpellMissInfo.Immune)
                return;

            var target = GetHitUnit();
            if (target)
            {
                // Casting Banish on a banished target will Remove applied aura
                var banishAura = target.GetAura(GetSpellInfo().Id, GetCaster().GetGUID());
                if (banishAura != null)
                    banishAura.Remove();
            }
        }

        public override void Register()
        {
            BeforeHit.Add(new BeforeHitHandler(HandleBanish));
        }
    }

    [Script] // 77220 - Mastery: Chaotic Energies
    class spell_warl_chaotic_energies : AuraScript
    {
        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            var auraEffect = GetEffect(1);
            if (auraEffect == null || !GetTargetApplication().HasEffect(1))
            {
                PreventDefaultAction();
                return;
            }

            // You take ${$s2/3}% reduced damage
            var damageReductionPct = (float)auraEffect.GetAmount() / 3;
            // plus a random amount of up to ${$s2/3}% additional reduced damage
            damageReductionPct += RandomHelper.FRand(0.0f, damageReductionPct);

            absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), damageReductionPct);
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new EffectAbsorbHandler(HandleAbsorb, 2));
        }
    }

    [Script] // 6201 - Create Healthstone
    class spell_warl_create_healthstone : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CreateHealthstone);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.CreateHealthstone, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 48018 - Demonic Circle: Summon
    class spell_warl_demonic_circle_summon : AuraScript
    {
        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // If effect is Removed by expire Remove the summoned demonic circle too.
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Reapply))
                GetTarget().RemoveGameObject(GetId(), true);

            GetTarget().RemoveAura(SpellIds.DemonicCircleAllowCast);
        }

        void HandleDummyTick(AuraEffect aurEff)
        {
            var circle = GetTarget().GetGameObject(GetId());
            if (circle)
            {
                // Here we check if player is in demonic circle teleport range, if so add
                // WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST; allowing him to cast the WARLOCK_DEMONIC_CIRCLE_TELEPORT.
                // If not in range Remove the WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST.

                var spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.DemonicCircleTeleport, GetCastDifficulty());

                if (GetTarget().IsWithinDist(circle, spellInfo.GetMaxRange(true)))
                {
                    if (!GetTarget().HasAura(SpellIds.DemonicCircleAllowCast))
                        GetTarget().CastSpell(GetTarget(), SpellIds.DemonicCircleAllowCast, true);
                }
                else
                    GetTarget().RemoveAura(SpellIds.DemonicCircleAllowCast);
            }
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 48020 - Demonic Circle: Teleport
    class spell_warl_demonic_circle_teleport : AuraScript
    {
        void HandleTeleport(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            var player = GetTarget().ToPlayer();
            if (player)
            {
                var circle = player.GetGameObject(SpellIds.DemonicCircleSummon);
                if (circle)
                {
                    player.NearTeleportTo(circle.GetPositionX(), circle.GetPositionY(), circle.GetPositionZ(), circle.GetOrientation());
                    player.RemoveMovementImpairingAuras(false);
                }
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleTeleport, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 67518, 19505 - Devour Magic
    class spell_warl_devour_magic : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfDemonTraining, SpellIds.DevourMagicHeal);
        }

        void OnSuccessfulDispel(uint effIndex)
        {
            var effect = GetSpellInfo().GetEffect(1);
            if (effect != null)
            {
                var caster = GetCaster();
                var heal_amount = effect.CalcValue(caster);

                caster.CastCustomSpell(caster, SpellIds.DevourMagicHeal, heal_amount, 0, 0, true);

                // Glyph of Felhunter
                var owner = caster.GetOwner();
                if (owner)
                    if (owner.GetAura(SpellIds.GlyphOfDemonTraining) != null)
                        owner.CastCustomSpell(owner, SpellIds.DevourMagicHeal, heal_amount, 0, 0, true);
            }
        }

        public override void Register()
        {
            OnEffectSuccessfulDispel.Add(new EffectHandler(OnSuccessfulDispel, 0, SpellEffectName.Dispel));
        }
    }

    [Script] // 48181 - Haunt
    class spell_warl_haunt : SpellScript
    {
        void HandleAfterHit()
        {
            var aura = GetHitAura();
            if (aura != null)
            {
                var aurEff = aura.GetEffect(1);
                if (aurEff != null)
                    aurEff.SetAmount(MathFunctions.CalculatePct(aurEff.GetAmount(), GetHitDamage()));
            }
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleAfterHit));
        }
    }

    [Script] // 755 - Health Funnel
    class spell_warl_health_funnel : AuraScript
    {
        void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            var caster = GetCaster();
            if (!caster)
                return;

            var target = GetTarget();
            if (caster.HasAura(SpellIds.ImprovedHealthFunnelR2))
                target.CastSpell(target, SpellIds.ImprovedHealthFunnelBuffR2, true);
            else if (caster.HasAura(SpellIds.ImprovedHealthFunnelR1))
                target.CastSpell(target, SpellIds.ImprovedHealthFunnelBuffR1, true);
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            var target = GetTarget();
            target.RemoveAurasDueToSpell(SpellIds.ImprovedHealthFunnelBuffR1);
            target.RemoveAurasDueToSpell(SpellIds.ImprovedHealthFunnelBuffR2);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            var caster = GetCaster();
            if (!caster)
                return;
            //! HACK for self damage, is not blizz :/
            var damage = (uint)caster.CountPctFromMaxHealth(aurEff.GetBaseAmount());

            var modOwner = caster.GetSpellModOwner();
            if (modOwner)
                modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.Cost, ref damage);

            var damageInfo = new SpellNonMeleeDamage(caster, caster, GetSpellInfo(), GetAura().GetSpellVisual(), GetSpellInfo().SchoolMask, GetAura().GetCastGUID());
            damageInfo.periodicLog = true;
            damageInfo.damage = damage;
            caster.DealSpellDamage(damageInfo, false);
            caster.SendSpellNonMeleeDamageLog(damageInfo);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(ApplyEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(RemoveEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.ObsModHealth));
        }
    }

    [Script] // 6262 - Healthstone
    class spell_warl_healthstone_heal : SpellScript
    {
        void HandleOnHit()
        {
            var heal = (int)MathFunctions.CalculatePct(GetCaster().GetCreateHealth(), GetHitHeal());
            SetHitHeal(heal);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleOnHit));
        }
    }

    [Script] // 6358 - Seduction (Special Ability)
    class spell_warl_seduction : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfSuccubus, SpellIds.PriestShadowWordDeath);
        }

        void HandleScriptEffect(uint effIndex)
        {
            var caster = GetCaster();
            var target = GetHitUnit();
            if (target)
            {
                if (caster.GetOwner() && caster.GetOwner().HasAura(SpellIds.GlyphOfSuccubus))
                {
                    target.RemoveAurasByType(AuraType.PeriodicDamage, ObjectGuid.Empty, target.GetAura(SpellIds.PriestShadowWordDeath)); // SW:D shall not be Removed.
                    target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
                    target.RemoveAurasByType(AuraType.PeriodicLeech);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script] // 27285 - Seed of Corruption
    class spell_warl_seed_of_corruption : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            if (GetExplTargetUnit())
                targets.Remove(GetExplTargetUnit());
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        }
    }

    [Script] // 27243 - Seed of Corruption
    class spell_warl_seed_of_corruption_dummy : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SeedOfCorruptionDamage);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            var damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            var amount = (int)(aurEff.GetAmount() - damageInfo.GetDamage());
            if (amount > 0)
            {
                aurEff.SetAmount(amount);
                if (!GetTarget().HealthBelowPctDamaged(1, damageInfo.GetDamage()))
                    return;
            }

            Remove();

            var caster = GetCaster();
            if (!caster)
                return;

            caster.CastSpell(eventInfo.GetActionTarget(), SpellIds.SeedOfCorruptionDamage, true);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    // 32863 - Seed of Corruption
    // 36123 - Seed of Corruption
    // 38252 - Seed of Corruption
    // 39367 - Seed of Corruption
    // 44141 - Seed of Corruption
    // 70388 - Seed of Corruption
    [Script] // Monster spells, triggered only on amount drop (not on death)
    class spell_warl_seed_of_corruption_generic : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SeedOfCorruptionGeneric);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            var damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            var amount = aurEff.GetAmount() - (int)damageInfo.GetDamage();
            if (amount > 0)
            {
                aurEff.SetAmount(amount);
                return;
            }

            Remove();

            var caster = GetCaster();
            if (!caster)
                return;

            caster.CastSpell(eventInfo.GetActionTarget(), SpellIds.SeedOfCorruptionGeneric, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // 86121 - Soul Swap
    class spell_warl_soul_swap : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfSoulSwap, SpellIds.SoulSwapCdMarker, SpellIds.SoulSwapOverride);
        }

        void HandleHit(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.SoulSwapOverride, true);
            GetHitUnit().CastSpell(GetCaster(), SpellIds.SoulSwapDotMarker, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 86211 - Soul Swap - Also acts as a dot container
    public class spell_warl_soul_swap_override : AuraScript
    {
        //! Forced to, pure virtual functions must have a body when linking
        public override void Register() { }

        public void AddDot(uint id) { _dotList.Add(id); }
        public List<uint> GetDotList() { return _dotList; }
        public Unit GetOriginalSwapSource() { return _swapCaster; }
        public void SetOriginalSwapSource(Unit victim) { _swapCaster = victim; }
        List<uint> _dotList = new List<uint>();
        Unit _swapCaster;
    }

    [Script] //! Soul Swap Copy Spells - 92795 - Simply copies spell IDs.
    class spell_warl_soul_swap_dot_marker : SpellScript
    {
        void HandleHit(uint effIndex)
        {
            var swapVictim = GetCaster();
            var warlock = GetHitUnit();
            if (!warlock || !swapVictim)
                return;

            var appliedAuras = swapVictim.GetAppliedAuras();
            spell_warl_soul_swap_override swapSpellScript = null;
            var swapOverrideAura = warlock.GetAura(SpellIds.SoulSwapOverride);
            if (swapOverrideAura != null)
                swapSpellScript = swapOverrideAura.GetScript<spell_warl_soul_swap_override>("spell_warl_soul_swap_override");

            if (swapSpellScript == null)
                return;

            var classMask = GetEffectInfo().SpellClassMask;

            foreach (var itr in appliedAuras)
            {
                var spellProto = itr.Value.GetBase().GetSpellInfo();
                if (itr.Value.GetBase().GetCaster() == warlock)
                    if (spellProto.SpellFamilyName == SpellFamilyNames.Warlock && (spellProto.SpellFamilyFlags & classMask))
                        swapSpellScript.AddDot(itr.Key);
            }

            swapSpellScript.SetOriginalSwapSource(swapVictim);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 86213 - Soul Swap Exhale
    class spell_warl_soul_swap_exhale : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SoulSwapModCost, SpellIds.SoulSwapOverride);
        }

        SpellCastResult CheckCast()
        {
            var currentTarget = GetExplTargetUnit();
            Unit swapTarget = null;
            var swapOverride = GetCaster().GetAura(SpellIds.SoulSwapOverride);
            if (swapOverride != null)
            {
                var swapScript = swapOverride.GetScript<spell_warl_soul_swap_override>("spell_warl_soul_swap_override");
                if (swapScript != null)
                    swapTarget = swapScript.GetOriginalSwapSource();
            }

            // Soul Swap Exhale can't be cast on the same target than Soul Swap
            if (swapTarget && currentTarget && swapTarget == currentTarget)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        void onEffectHit(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.SoulSwapModCost, true);
            var hasGlyph = GetCaster().HasAura(SpellIds.GlyphOfSoulSwap);

            var dotList = new List<uint>();
            Unit swapSource = null;
            var swapOverride = GetCaster().GetAura(SpellIds.SoulSwapOverride);
            if (swapOverride != null)
            {
                var swapScript = swapOverride.GetScript<spell_warl_soul_swap_override>("spell_warl_soul_swap_override");
                if (swapScript == null)
                    return;
                dotList = swapScript.GetDotList();
                swapSource = swapScript.GetOriginalSwapSource();
            }

            if (dotList.Empty())
                return;

            foreach (var itr in dotList)
            {
                GetCaster().AddAura(itr, GetHitUnit());
                if (!hasGlyph && swapSource)
                    swapSource.RemoveAurasDueToSpell(itr);
            }

            // Remove Soul Swap Exhale buff
            GetCaster().RemoveAurasDueToSpell(SpellIds.SoulSwapOverride);

            if (hasGlyph) // Add a cooldown on Soul Swap if caster has the glyph
                GetCaster().CastSpell(GetCaster(), SpellIds.SoulSwapCdMarker, false);
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            OnEffectHitTarget.Add(new EffectHandler(onEffectHit, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 29858 - Soulshatter
    class spell_warl_soulshatter : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Soulshatter);
        }

        void HandleDummy(uint effIndex)
        {
            var caster = GetCaster();
            var target = GetHitUnit();
            if (target)
                if (target.CanHaveThreatList() && target.GetThreatManager().GetThreat(caster) > 0.0f)
                    caster.CastSpell(target, SpellIds.Soulshatter, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script("spell_warl_t4_2p_bonus_shadow", SpellIds.Flameshadow)]// 37377 - Shadowflame
    [Script("spell_warl_t4_2p_bonus_fire", SpellIds.Shadowflame)]// 39437 - Shadowflame Hellfire and RoF
    class spell_warl_t4_2p_bonus : AuraScript
    {
        public spell_warl_t4_2p_bonus(uint triggerSpell)
        {
            _triggerSpell = triggerSpell;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_triggerSpell);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            var caster = eventInfo.GetActor();
            caster.CastSpell(caster, _triggerSpell, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        uint _triggerSpell;
    }

    [Script] // 30108, 34438, 34439, 35183 - Unstable Affliction
    class spell_warl_unstable_affliction : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.UnstableAfflictionDispel);
        }

        void HandleDispel(DispelInfo dispelInfo)
        {
            var caster = GetCaster();
            if (caster)
            {
                var aurEff = GetEffect(1);
                if (aurEff != null)
                {
                    var damage = aurEff.GetAmount() * 9;
                    // backfire damage and silence
                    caster.CastCustomSpell(dispelInfo.GetDispeller(), SpellIds.UnstableAfflictionDispel, damage, 0, 0, true, null, aurEff);
                }
            }
        }

        public override void Register()
        {
            AfterDispel.Add(new AuraDispelHandler(HandleDispel));
        }
    }

    [Script] // 5740 - Rain of Fire Updated 7.1.5
    class spell_warl_rain_of_fire : AuraScript
    {
        void HandleDummyTick(AuraEffect aurEff)
        {
            var rainOfFireAreaTriggers = GetTarget().GetAreaTriggers(SpellIds.RainOfFire);
            var targetsInRainOfFire = new List<ObjectGuid>();

            foreach (var rainOfFireAreaTrigger in rainOfFireAreaTriggers)
            {
                var insideTargets = rainOfFireAreaTrigger.GetInsideUnits();
                targetsInRainOfFire.AddRange(insideTargets);
            }

            foreach (var insideTargetGuid in targetsInRainOfFire)
            {
                var insideTarget = Global.ObjAccessor.GetUnit(GetTarget(), insideTargetGuid);
                if (insideTarget)
                    if (!GetTarget().IsFriendlyTo(insideTarget))
                        GetTarget().CastSpell(insideTarget, SpellIds.RainOfFireDamage, true);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 3, AuraType.PeriodicDummy));
        }
    }
}
