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
        public const uint BaneOfDoomEffect = 18662;
        public const uint CreateHealthstone = 23517;
        public const uint DemonicCircleAllowCast = 62388;
        public const uint DemonicCircleSummon = 48018;
        public const uint DemonicCircleTeleport = 48020;
        public const uint DemonicEmpowermentFelguard = 54508;
        public const uint DemonicEmpowermentFelhunter = 54509;
        public const uint DemonicEmpowermentImp = 54444;
        public const uint DemonicEmpowermentSuccubus = 54435;
        public const uint DemonicEmpowermentVoidwalker = 54443;
        public const uint DemonSoulImp = 79459;
        public const uint DemonSoulFelhunter = 79460;
        public const uint DemonSoulFelguard = 79452;
        public const uint DemonSoulSuccubus = 79453;
        public const uint DemonSoulVoidwalker = 79454;
        public const uint DevourMagicHeal = 19658;
        public const uint FelSynergyHeal = 54181;
        public const uint GlyphOfDemonTraining = 56249;
        public const uint GlyphOfShadowflame = 63311;
        public const uint GlyphOfSoulSwap = 56226;
        public const uint GlyphOfSuccubus = 56250;
        public const uint HauntHeal = 48210;
        public const uint Immolate = 348;
        public const uint ImprovedHealthFunnelBuffR1 = 60955;
        public const uint ImprovedHealthFunnelBuffR2 = 60956;
        public const uint ImprovedHealthFunnelR1 = 18703;
        public const uint ImprovedHealthFunnelR2 = 18704;
        public const uint ImprovedSoulFirePct = 85383;
        public const uint ImprovedSoulFireState = 85385;
        public const uint NetherWard = 91711;
        public const uint NetherTalent = 91713;
        public const uint RainOfFire = 5740;
        public const uint RainOfFireDamage = 42223;
        public const uint SeedOfCorruptionDamage = 27285;
        public const uint SeedOfCorruptionGeneric = 32865;
        public const uint ShadowTrance = 17941;
        public const uint ShadowWard = 6229;
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

            Unit target = GetHitUnit();
            if (target)
            {
                // Casting Banish on a banished target will remove applied aura
                Aura banishAura = target.GetAura(GetSpellInfo().Id, GetCaster().GetGUID());
                if (banishAura != null)
                    banishAura.Remove();
            }
        }

        public override void Register()
        {
            BeforeHit.Add(new BeforeHitHandler(HandleBanish));
        }
    }

    [Script] // 17962 - Conflagrate - Updated to 4.3.4
    class spell_warl_conflagrate : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Immolate);
        }

        // 6.x dmg formula in tooltip
        // void HandleHit(uint effIndex)
        // {
        //     if (AuraEffect aurEff = GetHitUnit().GetAuraEffect(SPELL_WARLOCK_IMMOLATE, 2, GetCaster().GetGUID()))
        //         SetHitDamage(CalculatePct(aurEff.GetAmount(), HasSpellInfo().Effects[1].CalcValue(GetCaster())));
        // }

        public override void Register()
        {
            //OnEffectHitTarget.Add(new EffectHandler(spell_warl_conflagrate_SpellScript::HandleHit, 0, SPELL_EFFECT_SCHOOL_DAMAGE);
        }
    }

    [Script] // 77220 - Mastery: Chaotic Energies
    class spell_warl_chaotic_energies : AuraScript
    {
        void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            AuraEffect effect1 = GetEffect(1);
            if (effect1 == null || !GetTargetApplication().HasEffect(1))
            {
                PreventDefaultAction();
                return;
            }

            // You take ${$s2/3}% reduced damage
            float damageReductionPct = (float)effect1.GetAmount() / 3;
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

    [Script] // 603 - Bane of Doom
    class spell_warl_bane_of_doom : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BaneOfDoomEffect);
        }

        public override bool Load()
        {
            return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (!GetCaster())
                return;

            AuraRemoveMode removeMode = GetTargetApplication().GetRemoveMode();
            if (removeMode != AuraRemoveMode.Death || !IsExpired())
                return;

            if (GetCaster().ToPlayer().isHonorOrXPTarget(GetTarget()))
                GetCaster().CastSpell(GetTarget(), SpellIds.BaneOfDoomEffect, true, null, aurEff);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 48018 - Demonic Circle: Summon
    class spell_warl_demonic_circle_summon : AuraScript
    {
        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // If effect is removed by expire remove the summoned demonic circle too.
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Reapply))
                GetTarget().RemoveGameObject(GetId(), true);

            GetTarget().RemoveAura(SpellIds.DemonicCircleAllowCast);
        }

        void HandleDummyTick(AuraEffect aurEff)
        {
            GameObject circle = GetTarget().GetGameObject(GetId());
            if (circle)
            {
                // Here we check if player is in demonic circle teleport range, if so add
                // WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST; allowing him to cast the WARLOCK_DEMONIC_CIRCLE_TELEPORT.
                // If not in range remove the WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST.

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.DemonicCircleTeleport);

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
            Player player = GetTarget().ToPlayer();
            if (player)
            {
                GameObject circle = player.GetGameObject(SpellIds.DemonicCircleSummon);
                if (circle)
                {
                    player.NearTeleportTo(circle.GetPositionX(), circle.GetPositionY(), circle.GetPositionZ(), circle.GetOrientation());
                    player.RemoveMovementImpairingAuras();
                }
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleTeleport, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 77801 - Demon Soul - Updated to 4.3.4
    class spell_warl_demon_soul : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DemonSoulImp, SpellIds.DemonSoulFelhunter, SpellIds.DemonSoulFelguard, SpellIds.DemonSoulSuccubus, SpellIds.DemonSoulVoidwalker);
        }

        void OnHitTarget(uint effIndex)
        {
            Unit caster = GetCaster();
            Creature targetCreature = GetHitCreature();
            if (targetCreature)
            {
                if (targetCreature.IsPet())
                {
                    CreatureTemplate ci = targetCreature.GetCreatureTemplate();
                    switch (ci.Family)
                    {
                        case CreatureFamily.Succubus:
                            caster.CastSpell(caster, SpellIds.DemonSoulSuccubus);
                            break;
                        case CreatureFamily.Voidwalker:
                            caster.CastSpell(caster, SpellIds.DemonSoulVoidwalker);
                            break;
                        case CreatureFamily.Felguard:
                            caster.CastSpell(caster, SpellIds.DemonSoulFelguard);
                            break;
                        case CreatureFamily.Felhunter:
                            caster.CastSpell(caster, SpellIds.DemonSoulFelhunter);
                            break;
                        case CreatureFamily.Imp:
                            caster.CastSpell(caster, SpellIds.DemonSoulImp);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(OnHitTarget, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 47193 - Demonic Empowerment
    class spell_warl_demonic_empowerment : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DemonicEmpowermentSuccubus, SpellIds.DemonicEmpowermentVoidwalker, SpellIds.DemonicEmpowermentFelguard, SpellIds.DemonicEmpowermentFelhunter, SpellIds.DemonicEmpowermentImp);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Creature targetCreature = GetHitCreature();
            if (targetCreature)
            {
                if (targetCreature.IsPet())
                {
                    CreatureTemplate ci = targetCreature.GetCreatureTemplate();
                    switch (ci.Family)
                    {
                        case CreatureFamily.Succubus:
                            targetCreature.CastSpell(targetCreature, SpellIds.DemonicEmpowermentSuccubus, true);
                            break;
                        case CreatureFamily.Voidwalker:
                            {
                                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.DemonicEmpowermentVoidwalker);
                                int hp = (int)targetCreature.CountPctFromMaxHealth(GetCaster().CalculateSpellDamage(targetCreature, spellInfo, 0));
                                targetCreature.CastCustomSpell(targetCreature, SpellIds.DemonicEmpowermentVoidwalker, hp, 0, 0, true);
                                break;
                            }
                        case CreatureFamily.Felguard:
                            targetCreature.CastSpell(targetCreature, SpellIds.DemonicEmpowermentFelguard, true);
                            break;
                        case CreatureFamily.Felhunter:
                            targetCreature.CastSpell(targetCreature, SpellIds.DemonicEmpowermentFelhunter, true);
                            break;
                        case CreatureFamily.Imp:
                            targetCreature.CastSpell(targetCreature, SpellIds.DemonicEmpowermentImp, true);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
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
            SpellEffectInfo effect = GetSpellInfo().GetEffect(1);
            if (effect != null)
            {
                Unit caster = GetCaster();
                int heal_amount = effect.CalcValue(caster);

                caster.CastCustomSpell(caster, SpellIds.DevourMagicHeal, heal_amount, 0, 0, true);

                // Glyph of Felhunter
                Unit owner = caster.GetOwner();
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

    [Script] // 47422 - Everlasting Affliction
    class spell_warl_everlasting_affliction : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target)
            {
                // Refresh corruption on target
                AuraEffect aurEff = target.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Warlock, new FlagArray128(0x2, 0, 0), caster.GetGUID());
                if (aurEff != null)
                {
                    uint damage = (uint)Math.Max(aurEff.GetAmount(), 0);
                    Global.ScriptMgr.ModifyPeriodicDamageAurasTick(target, caster, ref damage);
                    aurEff.SetDamage((int)(caster.SpellDamageBonusDone(target, aurEff.GetSpellInfo(), damage, DamageEffectType.DOT, GetEffectInfo(effIndex)) * aurEff.GetDonePct()));
                    aurEff.CalculatePeriodic(caster, false, false);
                    aurEff.GetBase().RefreshDuration(true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // -47230 - Fel Synergy
    class spell_warl_fel_synergy : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FelSynergyHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return false;

            return GetTarget().GetGuardianPet();
        }

        void onProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            int heal = (int)MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());
            GetTarget().CastCustomSpell(SpellIds.FelSynergyHeal, SpellValueMod.BasePoint0, heal, (Unit)null, true, null, aurEff); // TARGET_UNIT_PET
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(onProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 63310 - Glyph of Shadowflame
    class spell_warl_glyph_of_shadowflame : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfShadowflame);
        }

        void onProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.GlyphOfShadowflame, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(onProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 48181 - Haunt
    class spell_warl_haunt : SpellScript
    {
        void HandleAfterHit()
        {
            Aura aura = GetHitAura();
            if (aura != null)
            {
                AuraEffect aurEff = aura.GetEffect(1);
                if (aurEff != null)
                    aurEff.SetAmount(MathFunctions.CalculatePct(aurEff.GetAmount(), GetHitDamage()));
            }
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleAfterHit));
        }
    }

    [Script]
    class spell_warl_haunt_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.HauntHeal);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                int amount = aurEff.GetAmount();
                GetTarget().CastCustomSpell(caster, SpellIds.HauntHeal, amount, 0, 0, true, null, aurEff, GetCasterGUID());
            }
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(HandleRemove, 1, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script] // 755 - Health Funnel
    class spell_warl_health_funnel : AuraScript
    {
        void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (!caster)
                return;

            Unit target = GetTarget();
            if (caster.HasAura(SpellIds.ImprovedHealthFunnelR2))
                target.CastSpell(target, SpellIds.ImprovedHealthFunnelBuffR2, true);
            else if (caster.HasAura(SpellIds.ImprovedHealthFunnelR1))
                target.CastSpell(target, SpellIds.ImprovedHealthFunnelBuffR1, true);
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveAurasDueToSpell(SpellIds.ImprovedHealthFunnelBuffR1);
            target.RemoveAurasDueToSpell(SpellIds.ImprovedHealthFunnelBuffR2);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (!caster)
                return;
            //! HACK for self damage, is not blizz :/
            uint damage = (uint)caster.CountPctFromMaxHealth(aurEff.GetBaseAmount());

            Player modOwner = caster.GetSpellModOwner();
            if (modOwner)
                modOwner.ApplySpellMod(GetId(), SpellModOp.Cost, ref damage);

            SpellNonMeleeDamage damageInfo = new SpellNonMeleeDamage(caster, caster, GetSpellInfo().Id, GetAura().GetSpellXSpellVisualId(), GetSpellInfo().SchoolMask, GetAura().GetCastGUID());
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
            int heal = (int)MathFunctions.CalculatePct(GetCaster().GetCreateHealth(), GetHitHeal());
            SetHitHeal(heal);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleOnHit));
        }
    }

    [Script] // -18119 - Improved Soul Fire
    class spell_warl_improved_soul_fire : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ImprovedSoulFirePct, SpellIds.ImprovedSoulFireState);
        }

        void onProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastCustomSpell(SpellIds.ImprovedSoulFirePct, SpellValueMod.BasePoint0, aurEff.GetAmount(), GetTarget(), true, null, aurEff);
            GetTarget().CastSpell(GetTarget(), SpellIds.ImprovedSoulFireState, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(onProc, 0, AuraType.Dummy));
        }
    }

    // 687 - Demon Armor
    [Script] // 28176 - Fel Armor
    class spell_warl_nether_ward_overrride : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.NetherTalent, SpellIds.NetherWard, SpellIds.ShadowWard);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetUnitOwner().HasAura(SpellIds.NetherTalent))
                amount = (int)SpellIds.NetherWard;
            else
                amount = (int)SpellIds.ShadowWard;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 2, AuraType.OverrideActionbarSpells));
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
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target)
            {
                if (caster.GetOwner() && caster.GetOwner().HasAura(SpellIds.GlyphOfSuccubus))
                {
                    target.RemoveAurasByType(AuraType.PeriodicDamage, ObjectGuid.Empty, target.GetAura(SpellIds.PriestShadowWordDeath)); // SW:D shall not be removed.
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
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int amount = (int)(aurEff.GetAmount() - damageInfo.GetDamage());
            if (amount > 0)
            {
                aurEff.SetAmount(amount);
                if (!GetTarget().HealthBelowPctDamaged(1, damageInfo.GetDamage()))
                    return;
            }

            Remove();

            Unit caster = GetCaster();
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
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int amount = aurEff.GetAmount() - (int)damageInfo.GetDamage();
            if (amount > 0)
            {
                aurEff.SetAmount(amount);
                return;
            }

            Remove();

            Unit caster = GetCaster();
            if (!caster)
                return;

            caster.CastSpell(eventInfo.GetActionTarget(), SpellIds.SeedOfCorruptionGeneric, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy));
        }
    }

    [Script] // -7235 - Shadow Ward
    class spell_warl_shadow_ward : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = false;
            Unit caster = GetCaster();
            if (caster)
            {
                // +80.68% from sp bonus
                float bonus = 0.8068f;

                bonus *= caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask());

                amount += (int)bonus;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        }
    }

    [Script] // -30293 - Soul Leech
    class spell_warl_soul_leech : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GenReplenishment);
        }

        void onProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().CastSpell((Unit)null, SpellIds.GenReplenishment, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(onProc, 0, AuraType.ProcTriggerSpellWithValue));
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
            Unit swapVictim = GetCaster();
            Unit warlock = GetHitUnit();
            if (!warlock || !swapVictim)
                return;

            var appliedAuras = swapVictim.GetAppliedAuras();
            spell_warl_soul_swap_override swapSpellScript = null;
            Aura swapOverrideAura = warlock.GetAura(SpellIds.SoulSwapOverride);
            if (swapOverrideAura != null)
                swapSpellScript = swapOverrideAura.GetScript<spell_warl_soul_swap_override>("spell_warl_soul_swap_override");

            if (swapSpellScript == null)
                return;

            FlagArray128 classMask = GetEffectInfo().SpellClassMask;

            foreach (var itr in appliedAuras)
            {
                SpellInfo spellProto = itr.Value.GetBase().GetSpellInfo();
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
            Unit currentTarget = GetExplTargetUnit();
            Unit swapTarget = null;
            Aura swapOverride = GetCaster().GetAura(SpellIds.SoulSwapOverride);
            if (swapOverride != null)
            {
                spell_warl_soul_swap_override swapScript = swapOverride.GetScript<spell_warl_soul_swap_override>("spell_warl_soul_swap_override");
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
            bool hasGlyph = GetCaster().HasAura(SpellIds.GlyphOfSoulSwap);

            List<uint> dotList = new List<uint>();
            Unit swapSource = null;
            Aura swapOverride = GetCaster().GetAura(SpellIds.SoulSwapOverride);
            if (swapOverride != null)
            {
                spell_warl_soul_swap_override swapScript = swapOverride.GetScript<spell_warl_soul_swap_override>("spell_warl_soul_swap_override");
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
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target)
                if (target.CanHaveThreatList() && target.GetThreatManager().getThreat(caster) > 0.0f)
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
            Unit caster = eventInfo.GetActor();
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
            Unit caster = GetCaster();
            if (caster)
            {
                AuraEffect aurEff = GetEffect(1);
                if (aurEff != null)
                {
                    int damage = aurEff.GetAmount() * 9;
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
            List<AreaTrigger> rainOfFireAreaTriggers = GetTarget().GetAreaTriggers(SpellIds.RainOfFire);
            List<ObjectGuid> targetsInRainOfFire = new List<ObjectGuid>();

            foreach (AreaTrigger rainOfFireAreaTrigger in rainOfFireAreaTriggers)
            {
                var insideTargets = rainOfFireAreaTrigger.GetInsideUnits();
                targetsInRainOfFire.AddRange(insideTargets);
            }

            foreach (ObjectGuid insideTargetGuid in targetsInRainOfFire)
            {
                Unit insideTarget = Global.ObjAccessor.GetUnit(GetTarget(), insideTargetGuid);
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
