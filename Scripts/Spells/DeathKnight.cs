/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using System;
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
        public const uint BloodPlague = 55078;
        public const uint BloodPresence = 48263;
        public const uint BloodShieldMastery = 77513;
        public const uint BloodShieldAbsorb = 77535;
        public const uint ChainsOfIce = 45524;
        public const uint CorpseExplosionTriggered = 43999;
        public const uint DeathAndDecayDamage = 52212;
        public const uint DeathAndDecaySlow = 143375;
        public const uint DeathCoilBarrier = 115635;
        public const uint DeathCoilDamage = 47632;
        public const uint DeathCoilHeal = 47633;
        public const uint DeathGrip = 49560;
        public const uint DeathStrikeHeal = 45470;
        public const uint EnhancedDeathCoil = 157343;
        public const uint FrostFever = 55095;
        public const uint GhoulExplode = 47496;
        public const uint GlyphOfAbsorbMagic = 159415;
        public const uint GlyphOfAntiMagicShell = 58623;
        public const uint GlyphOfArmyOfTheDead = 58669;
        public const uint GlyphOfDeathCoil = 63333;
        public const uint GlyphOfDeathAndDecay = 58629;
        public const uint GlyphOfFoulMenagerie = 58642;
        public const uint GlyphOfRegenerativeMagic = 146648;
        public const uint GlyphOfRunicPowerTriggered = 159430;
        public const uint GlyphOfSwiftDeath = 146645;
        public const uint GlyphOfTheGeist = 58640;
        public const uint GlyphOfTheSkeleton = 146652;
        public const uint ImprovedBloodPresence = 50371;
        public const uint ImprovedSoulReaper = 157342;
        public const uint MarkOfBloodHeal = 206945;
        public const uint NecrosisEffect = 216974;
        public const uint RunicPowerEnergize = 49088;
        public const uint RunicReturn = 61258;
        public const uint ScourgeStrikeTriggered = 70890;
        public const uint ShadowOfDeath = 164047;
        public const uint SoulReaperDamage = 114867;
        public const uint SoulReaperHaste = 114868;
        public const uint T15Dps4pBonus = 138347;
        public const uint UnholyPresence = 48265;
        public const uint WillOfTheNecropolis = 157335;

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
    class spell_dk_advantage_t10_4p : SpellScriptLoader
    {
        public spell_dk_advantage_t10_4p() : base("spell_dk_advantage_t10_4p") { }

        class spell_dk_advantage_t10_4p_AuraScript : AuraScript
        {
            bool CheckProc(ProcEventInfo eventInfo)
            {
                Unit caster = eventInfo.GetActor();
                if (caster)
                {
                    if (!caster.IsTypeId(TypeId.Player) || caster.GetClass() != Class.Deathknight)
                        return false;

                    for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
                        if (caster.ToPlayer().GetRuneCooldown(i) == 0)
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

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_advantage_t10_4p_AuraScript();
        }
    }

    [Script] // 48707 - Anti-Magic Shell
    class spell_dk_anti_magic_shell : SpellScriptLoader
    {
        public spell_dk_anti_magic_shell() : base("spell_dk_anti_magic_shell") { }

        class spell_dk_anti_magic_shell_AuraScript : AuraScript
        {
            public spell_dk_anti_magic_shell_AuraScript()
            {
                absorbPct = 0;
                maxHealth = 0;
                absorbedAmount = 0;
            }

            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.RunicPowerEnergize, SpellIds.GlyphOfAbsorbMagic, SpellIds.GlyphOfRegenerativeMagic);
            }

            public override bool Load()
            {
                absorbPct = GetSpellInfo().GetEffect(0).CalcValue(GetCaster());
                maxHealth = (int)GetCaster().GetMaxHealth();
                absorbedAmount = 0;
                return true;
            }

            void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
            {
                amount = maxHealth;

                /// todo, check if AMS has basepoints for 2. in that case, this function should be rewritten.
                if (!GetUnitOwner().HasAura(SpellIds.GlyphOfAbsorbMagic))
                    amount /= 2;
            }

            void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
            {
                // we may only absorb a certain percentage of incoming damage.
                absorbAmount = (uint)(dmgInfo.GetDamage() * absorbPct / 100);
            }

            void Trigger(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
            {
                absorbedAmount += absorbAmount;

                if (!GetTarget().HasAura(SpellIds.GlyphOfAbsorbMagic))
                {
                    // Patch 6.0.2 (October 14, 2014): Anti-Magic Shell now restores 2 Runic Power per 1% of max health absorbed.
                    int bp = (int)(2 * absorbAmount * 100 / maxHealth);
                    GetTarget().CastCustomSpell(SpellIds.RunicPowerEnergize, SpellValueMod.BasePoint0, bp, GetTarget(), true, null, aurEff);
                }
            }

            void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Player player = GetTarget().ToPlayer();
                if (player)
                {
                    AuraEffect glyph = player.GetAuraEffect(SpellIds.GlyphOfRegenerativeMagic, 0);
                    if (glyph != null) // reduce cooldown of AMS if player has glyph
                    {
                        // Cannot reduce cooldown by more than 50%
                        int val = Math.Min(glyph.GetAmount(), (int)absorbedAmount * 100 / maxHealth);
                        player.GetSpellHistory().ModifyCooldown(GetId(), -(int)(player.GetSpellHistory().GetRemainingCooldown(GetSpellInfo()) * val / 100));
                    }
                }
            }

            public override void Register()
            {
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
                OnEffectAbsorb.Add(new EffectAbsorbHandler(Absorb, 0));
                AfterEffectAbsorb.Add(new EffectAbsorbHandler(Trigger, 0));
                AfterEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real));
            }

            int absorbPct;
            int maxHealth;
            uint absorbedAmount;
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_anti_magic_shell_AuraScript();
        }
    }

    [Script] // 43264 - Periodic Taunt     // 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast army of the dead ghouls.
    class spell_dk_army_periodic_taunt : SpellScriptLoader
    {
        public spell_dk_army_periodic_taunt() : base("spell_dk_army_periodic_taunt") { }

        class spell_dk_army_periodic_taunt_SpellScript : SpellScript
        {
            public override bool Load()
            {
                return GetCaster().IsGuardian();
            }

            SpellCastResult CheckCast()
            {
                Unit owner = GetCaster().GetOwner();
                if (owner)
                    if (!owner.HasAura(SpellIds.GlyphOfArmyOfTheDead))
                        return SpellCastResult.SpellCastOk;

                return SpellCastResult.SpellUnavailable;
            }

            public override void Register()
            {
                OnCheckCast.Add(new CheckCastHandler(CheckCast));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_army_periodic_taunt_SpellScript();
        }
    }

    [Script] // 127517 - Army Transform    // 6.x, does this belong here or in spell_generic? where do we cast this? sniffs say this is only cast when caster has glyph of foul menagerie.
    class spell_dk_army_transform : SpellScriptLoader
    {
        public spell_dk_army_transform() : base("spell_dk_army_transform") { }

        class spell_dk_army_transform_SpellScript : SpellScript
        {
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
                GetCaster().CastSpell(GetCaster(), SpellIds.ArmyTransforms[RandomHelper.URand(0, 5)], true);
            }

            public override void Register()
            {
                OnCheckCast.Add(new CheckCastHandler(CheckCast));
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_army_transform_SpellScript();
        }
    }

    [Script] // 50842 - Blood Boil
    class spell_dk_blood_boil : SpellScriptLoader
    {
        public spell_dk_blood_boil() : base("spell_dk_blood_boil") { }

        class spell_dk_blood_boil_SpellScript : SpellScript
        {
            public spell_dk_blood_boil_SpellScript()
            {
                bpDuration = 0;
                ffDuration = 0;
            }

            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.BloodPlague, SpellIds.FrostFever);
            }

            void FilterTargets(List<WorldObject> targets)
            {
                if (targets.Empty())
                    return;

                Unit caster = GetCaster();

                foreach (WorldObject target in targets)
                {
                    if (bpDuration != 0 && ffDuration != 0)
                        break;

                    Unit unit = target.ToUnit();
                    if (unit)
                    {
                        Aura bp = unit.GetAura(SpellIds.BloodPlague, caster.GetGUID());
                        if (bp != null)
                            bpDuration = bp.GetDuration();
                        Aura ff = unit.GetAura(SpellIds.FrostFever, caster.GetGUID());
                        if (ff != null)
                            ffDuration = ff.GetDuration();
                    }
                }
            }

            void HandleEffect(uint effIndex)
            {
                Unit caster = GetCaster();
                Unit target = GetHitUnit();

                if (ffDuration != 0)
                    caster.CastSpell(target, SpellIds.FrostFever, true);
                if (bpDuration != 0)
                    caster.CastSpell(target, SpellIds.BloodPlague, true);

                Aura bp = target.GetAura(SpellIds.BloodPlague, caster.GetGUID());
                if (bp != null)
                {
                    bp.SetDuration(bpDuration);
                    bp.SetMaxDuration(bpDuration);
                }

                Aura ff = target.GetAura(SpellIds.FrostFever, caster.GetGUID());
                if (ff != null)
                {
                    ff.SetDuration(ffDuration);
                    ff.SetMaxDuration(ffDuration);
                }
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
                OnEffectHitTarget.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.SchoolDamage));
            }

            int bpDuration;
            int ffDuration;
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_blood_boil_SpellScript();
        }
    }

    [Script] // 49028 - Dancing Rune Weapon 7.1.5
    class spell_dk_dancing_rune_weapon : SpellScriptLoader
    {
        public spell_dk_dancing_rune_weapon() : base("spell_dk_dancing_rune_weapon") { }

        class spell_dk_dancing_rune_weapon_AuraScript : AuraScript
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

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_dancing_rune_weapon_AuraScript();
        }
    }

    [Script] // 43265 - Death and Decay
    class spell_dk_death_and_decay : SpellScriptLoader
    {
        public spell_dk_death_and_decay() : base("spell_dk_death_and_decay") { }

        class spell_dk_death_and_decay_SpellScript : SpellScript
        {
            void HandleDummy(uint effIndex)
            {
                if (GetCaster().HasAura(SpellIds.GlyphOfDeathAndDecay))
                {
                    Position pos = GetExplTargetDest();
                    if (pos != null)
                        GetCaster().CastSpell(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), SpellIds.DeathAndDecaySlow, true);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_death_and_decay_SpellScript();
        }

        class spell_dk_death_and_decay_AuraScript : AuraScript
        {
            void HandleDummyTick(AuraEffect aurEff)
            {
                Unit caster = GetCaster();
                if (caster)
                    caster.CastCustomSpell(SpellIds.DeathAndDecayDamage, SpellValueMod.BasePoint0, aurEff.GetAmount(), GetTarget(), true, null, aurEff);
            }

            public override void Register()
            {
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleDummyTick, 2, AuraType.PeriodicDummy));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_death_and_decay_AuraScript();
        }
    }

    [Script] // 47541 - Death Coil
    class spell_dk_death_coil : SpellScriptLoader
    {
        public spell_dk_death_coil() : base("spell_dk_death_coil") { }

        class spell_dk_death_coil_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(SpellIds.DeathCoilDamage, SpellIds.DeathCoilHeal, SpellIds.GlyphOfDeathCoil);
            }

            void HandleDummy(uint effIndex)
            {
                Unit caster = GetCaster();
                Unit target = GetHitUnit();
                if (target)
                {
                    if (caster.IsFriendlyTo(target))
                    {
                        if (target.GetCreatureType() == CreatureType.Undead) // Any undead ally, including caster if he has lichborne.
                        {
                            caster.CastSpell(target, SpellIds.DeathCoilHeal, true);
                        }
                        else if (target != caster) // Any non undead ally except caster and only if caster has glyph of death coil.
                        {
                            SpellInfo DCD = Global.SpellMgr.GetSpellInfo(SpellIds.DeathCoilDamage);
                            SpellEffectInfo eff = DCD.GetEffect(0);
                            int bp = (int)caster.SpellDamageBonusDone(target, DCD, (uint)eff.CalcValue(caster), DamageEffectType.SpellDirect, eff);

                            caster.CastCustomSpell(target, SpellIds.DeathCoilBarrier, bp, 0, 0, true);
                        }
                    }
                    else // Any enemy target.
                    {
                        caster.CastSpell(target, SpellIds.DeathCoilDamage, true);
                    }
                }

                if (caster.HasAura(SpellIds.EnhancedDeathCoil))
                    caster.CastSpell(caster, SpellIds.ShadowOfDeath, true);
            }

            SpellCastResult CheckCast()
            {
                Unit caster = GetCaster();
                Unit target = GetExplTargetUnit();
                if (target)
                {
                    if (!caster.IsFriendlyTo(target) && !caster.isInFront(target))
                        return SpellCastResult.UnitNotInfront;

                    if (target.IsFriendlyTo(caster) && target.GetCreatureType() != CreatureType.Undead && !caster.HasAura(SpellIds.GlyphOfDeathCoil))
                        return SpellCastResult.BadTargets;
                }
                else
                    return SpellCastResult.BadTargets;

                return SpellCastResult.SpellCastOk;
            }

            public override void Register()
            {
                OnCheckCast.Add(new CheckCastHandler(CheckCast));
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_death_coil_SpellScript();
        }
    }

    [Script] // 52751 - Death Gate
    class spell_dk_death_gate : SpellScriptLoader
    {
        public spell_dk_death_gate() : base("spell_dk_death_gate") { }

        class spell_dk_death_gate_SpellScript : SpellScript
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

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_death_gate_SpellScript();
        }
    }

    [Script] // 49560 - Death Grip
    class spell_dk_death_grip : SpellScriptLoader
    {
        public spell_dk_death_grip() : base("spell_dk_death_grip") { }

        class spell_dk_death_grip_SpellScript : SpellScript
        {
            void HandleDummy(uint effIndex)
            {
                int damage = GetEffectValue();
                Position pos = GetExplTargetDest();
                Unit target = GetHitUnit();
                if (target)
                {
                    if (!target.HasAuraType(AuraType.DeflectSpells)) // Deterrence
                        target.CastSpell(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), (uint)damage, true);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }

        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_death_grip_SpellScript();
        }
    }

    [Script] // 48743 - Death Pact
    class spell_dk_death_pact : SpellScriptLoader
    {
        public spell_dk_death_pact() : base("spell_dk_death_pact") { }

        class spell_dk_death_pact_AuraScript : AuraScript
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

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_death_pact_AuraScript();
        }
    }

    [Script] // 49998 - Death Strike
    class spell_dk_death_strike : SpellScriptLoader
    {
        public spell_dk_death_strike() : base("spell_dk_death_strike") { }

        class spell_dk_death_strike_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.DeathStrikeHeal, SpellIds.BloodShieldMastery, SpellIds.BloodShieldAbsorb);
            }

            void HandleHeal(uint effIndex)
            {
                Unit caster = GetCaster();
                int heal = (int)caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 4; /// todo, add versatality bonus as it will probably not apply to the heal due to its damageclass SPELL_DAMAGE_CLASS_NONE.
                caster.CastCustomSpell(SpellIds.DeathStrikeHeal, SpellValueMod.BasePoint0, heal, caster, true);

                if (!caster.HasAura(SpellIds.BloodPresence) || !caster.HasAura(SpellIds.ImprovedBloodPresence))
                    return;

                /// todo, if SPELL_AURA_MOD_ABSORB_PERCENTAGE will not apply to SPELL_DAMAGE_CLASS_NONE, resolve must be applied here.
                AuraEffect aurEff = caster.GetAuraEffect(SpellIds.BloodShieldMastery, 0);
                if (aurEff != null)
                    caster.CastCustomSpell(SpellIds.BloodShieldAbsorb, SpellValueMod.BasePoint0, MathFunctions.CalculatePct(heal, aurEff.GetAmount()), caster);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleHeal, 1, SpellEffectName.WeaponPercentDamage));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_death_strike_SpellScript();
        }
    }

    [Script] // 85948 - Festering Strike
    class spell_dk_festering_strike : SpellScriptLoader
    {
        public spell_dk_festering_strike() : base("spell_dk_festering_strike") { }

        class spell_dk_festering_strike_SpellScript : SpellScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.FrostFever, SpellIds.BloodPlague, SpellIds.ChainsOfIce);
            }

            void HandleScriptEffect(uint effIndex)
            {
                int extraDuration = GetEffectValue();
                Unit target = GetHitUnit();
                ObjectGuid casterGUID = GetCaster().GetGUID();

                Aura ff = target.GetAura(SpellIds.FrostFever, casterGUID);
                if (ff != null)
                {
                    int newDuration = Math.Min(ff.GetDuration() + extraDuration, 2 * Time.Minute * Time.InMilliseconds); // caps at 2min.
                    ff.SetDuration(newDuration);
                    ff.SetMaxDuration(newDuration);
                }

                Aura bp = target.GetAura(SpellIds.BloodPlague, casterGUID);
                if (bp != null)
                {
                    int newDuration = Math.Min(bp.GetDuration() + extraDuration, 2 * Time.Minute * Time.InMilliseconds); // caps at 2min.
                    bp.SetDuration(newDuration);
                    bp.SetMaxDuration(newDuration);
                }

                Aura coi = target.GetAura(SpellIds.ChainsOfIce, casterGUID);
                if (coi != null)
                {
                    int newDuration = Math.Min(coi.GetDuration() + extraDuration, 20 * Time.InMilliseconds); // is 20sec cap? couldnt manage to get runes up to pass 20.
                    coi.SetDuration(newDuration);
                    coi.SetMaxDuration(newDuration);
                }
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.ScriptEffect));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_festering_strike_SpellScript();
        }
    }

    [Script] // 47496 - Explode, Ghoul spell for Corpse Explosion
    class spell_dk_ghoul_explode : SpellScriptLoader
    {
        public spell_dk_ghoul_explode() : base("spell_dk_ghoul_explode") { }

        class spell_dk_ghoul_explode_SpellScript : SpellScript
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

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_ghoul_explode_SpellScript();
        }
    }

    [Script] // 58677 - Glyph of Death's Embrace
    class spell_dk_glyph_of_deaths_embrace : SpellScriptLoader
    {
        public spell_dk_glyph_of_deaths_embrace() : base("spell_dk_glyph_of_deaths_embrace") { }

        class spell_dk_glyph_of_deaths_embrace_AuraScript : AuraScript
        {
            bool CheckProc(ProcEventInfo eventInfo)
            {
                Unit actionTarget = eventInfo.GetActionTarget();
                return actionTarget && actionTarget.GetCreatureType() == CreatureType.Undead && actionTarget.GetOwner();
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_glyph_of_deaths_embrace_AuraScript();
        }
    }

    [Script] // 159429 - Glyph of Runic Power
    class spell_dk_glyph_of_runic_power : SpellScriptLoader
    {
        public spell_dk_glyph_of_runic_power() : base("spell_dk_glyph_of_runic_power") { }

        class spell_dk_glyph_of_runic_power_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.GlyphOfRunicPowerTriggered);
            }

            public override bool Load()
            {
                return GetUnitOwner().GetClass() == Class.Deathknight;
            }

            bool CheckProc(ProcEventInfo eventInfo)
            {
                return eventInfo.GetSpellInfo() != null
                    && Convert.ToBoolean(eventInfo.GetSpellInfo().GetAllEffectsMechanicMask() & (1 << (int)Mechanics.Snare | 1 << (int)Mechanics.Root | 1 << (int)Mechanics.Freeze));
            }

            void HandleProc(ProcEventInfo eventInfo)
            {
                Unit target = eventInfo.GetProcTarget();
                if (target)
                    target.CastSpell(target, SpellIds.GlyphOfRunicPowerTriggered, true);
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
                OnProc.Add(new AuraProcHandler(HandleProc));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_glyph_of_runic_power_AuraScript();
        }
    }

    [Script] // 48792 - Icebound Fortitude
    class spell_dk_icebound_fortitude : SpellScriptLoader
    {
        public spell_dk_icebound_fortitude() : base("spell_dk_icebound_fortitude") { }

        class spell_dk_icebound_fortitude_AuraScript : AuraScript
        {
            void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
            {
                if (GetUnitOwner().HasAura(SpellIds.ImprovedBloodPresence))
                    amount += 30; /// todo, figure out how tooltip is updated
            }

            public override void Register()
            {
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 2, AuraType.ModDamagePercentTaken));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_icebound_fortitude_AuraScript();
        }
    }

    [Script] // 206940 - Mark of Blood 7.1.5
    class spell_dk_mark_of_blood : SpellScriptLoader
    {
        public spell_dk_mark_of_blood() : base("spell_dk_mark_of_blood") { }

        class spell_dk_mark_of_blood_AuraScript : AuraScript
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

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_mark_of_blood_AuraScript();
        }
    }

    [Script] // 207346 - Necrosis 7.1.5
    class spell_dk_necrosis : SpellScriptLoader
    {
        public spell_dk_necrosis() : base("spell_dk_necrosis") { }

        class spell_dk_necrosis_AuraScript : AuraScript
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

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_necrosis_AuraScript();
        }
    }

    [Script] // 121916 - Glyph of the Geist (Unholy)    // 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast on raise dead.
    class spell_dk_pet_geist_transform : SpellScriptLoader
    {
        public spell_dk_pet_geist_transform() : base("spell_dk_pet_geist_transform") { }

        class spell_dk_pet_geist_transform_SpellScript : SpellScript
        {
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

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_pet_geist_transform_SpellScript();
        }
    }

    [Script] // 147157 Glyph of the Skeleton (Unholy)    // 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast on raise dead.
    class spell_dk_pet_skeleton_transform : SpellScriptLoader
    {
        public spell_dk_pet_skeleton_transform() : base("spell_dk_pet_skeleton_transform") { }

        class spell_dk_pet_skeleton_transform_SpellScript : SpellScript
        {
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

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_pet_skeleton_transform_SpellScript();
        }
    }

    [Script] // 61257 - Runic Power Back on Snare/Root 7.1.5
    class spell_dk_pvp_4p_bonus : SpellScriptLoader
    {
        public spell_dk_pvp_4p_bonus() : base("spell_dk_pvp_4p_bonus") { }

        class spell_dk_pvp_4p_bonus_AuraScript : AuraScript
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

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_pvp_4p_bonus_AuraScript();
        }
    }

    [Script] // 46584 - Raise Dead
    class spell_dk_raise_dead : SpellScriptLoader
    {
        public spell_dk_raise_dead() : base("spell_dk_raise_dead") { }

        class spell_dk_raise_dead_SpellScript : SpellScript
        {
            public spell_dk_raise_dead_SpellScript() { }

            public override bool Validate(SpellInfo spellInfo)
            {
                return spellInfo.GetEffect(0) != null && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
            }

            public override bool Load()
            {
                return GetCaster().IsTypeId(TypeId.Player);
            }

            void HandleDummy(uint effIndex)
            {
                GetCaster().CastSpell((Unit)null, (uint)GetEffectValue(), true);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_raise_dead_SpellScript();
        }
    }

    [Script] // 114866 - Soul Reaper, 130735 - Soul Reaper, 130736 - Soul Reaper
    class spell_dk_soul_reaper : SpellScriptLoader
    {
        public spell_dk_soul_reaper() : base("spell_dk_soul_reaper") { }

        class spell_dk_soul_reaper_AuraScript : AuraScript
        {
            void HandlePeriodicDummy(AuraEffect aurEff)
            {
                Unit caster = GetCaster();
                Unit target = GetUnitOwner();

                if (!caster || !target)
                    return;

                float pct = target.GetHealthPct();

                if (pct < 35.0f || (pct < 45.0f && (caster.HasAura(SpellIds.ImprovedSoulReaper) || caster.HasAura(SpellIds.T15Dps4pBonus))))
                    caster.CastSpell(target, SpellIds.SoulReaperDamage, true, null, aurEff);
            }

            void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.ByDeath)
                    return;

                Unit caster = GetCaster();
                if (caster)
                    caster.CastSpell(caster, SpellIds.SoulReaperHaste, true);
            }

            public override void Register()
            {
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodicDummy, 1, AuraType.PeriodicDummy));
                AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_soul_reaper_AuraScript();
        }
    }

    [Script] // 115994 - Unholy Blight
    class spell_dk_unholy_blight : SpellScriptLoader
    {
        public spell_dk_unholy_blight() : base("spell_dk_unholy_blight") { }

        class spell_dk_unholy_blight_SpellScript : SpellScript
        {
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

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_unholy_blight_SpellScript();
        }
    }

    [Script] // 55233 - Vampiric Blood
    class spell_dk_vampiric_blood : SpellScriptLoader
    {
        public spell_dk_vampiric_blood() : base("spell_dk_vampiric_blood") { }

        class spell_dk_vampiric_blood_AuraScript : AuraScript
        {
            void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
            {
                amount = (int)GetUnitOwner().CountPctFromMaxHealth(amount);
            }

            public override void Register()
            {
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseHealth));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_vampiric_blood_AuraScript();
        }
    }

    [Script] // 81164 - Will of the Necropolis
    class spell_dk_will_of_the_necropolis : SpellScriptLoader
    {
        public spell_dk_will_of_the_necropolis() : base("spell_dk_will_of_the_necropolis") { }

        class spell_dk_will_of_the_necropolis_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return spellInfo.GetEffect(0) != null && ValidateSpellInfo(SpellIds.WillOfTheNecropolis);
            }

            bool CheckProc(ProcEventInfo eventInfo)
            {
                Unit target = GetTarget();

                if (target.HasAura(SpellIds.WillOfTheNecropolis))
                    return false;

                return target.HealthBelowPctDamaged(30, eventInfo.GetDamageInfo().GetDamage());
            }

            void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                GetTarget().CastSpell(GetTarget(), SpellIds.WillOfTheNecropolis, true, null, aurEff);
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
                OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_dk_will_of_the_necropolis_AuraScript();
        }
    }

    [Script] // 49576 - Death Grip Initial
    class spell_dk_death_grip_initial : SpellScriptLoader
    {
        public spell_dk_death_grip_initial() : base("spell_dk_death_grip_initial") { }

        class spell_dk_death_grip_initial_SpellScript : SpellScript
        {
            SpellCastResult CheckCast()
            {
                Unit caster = GetCaster();
                // Death Grip should not be castable while jumping/falling
                if (caster.HasUnitState(UnitState.Jumping) || caster.HasUnitMovementFlag(MovementFlag.Falling))
                    return SpellCastResult.Moving;

                // Patch 3.3.3 (2010-03-23): Minimum range has been changed to 8 yards in PvP.
                Unit target = GetExplTargetUnit();
                if (target && target.IsTypeId(TypeId.Player))
                    if (caster.GetDistance(target) < 8.0f)
                        return SpellCastResult.TooClose;

                return SpellCastResult.SpellCastOk;
            }

            void HandleDummy(uint effIndex)
            {
                GetCaster().CastSpell(GetHitUnit(), SpellIds.DeathGrip, true);
            }

            public override void Register()
            {
                OnCheckCast.Add(new CheckCastHandler(CheckCast));
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_dk_death_grip_initial_SpellScript();
        }
    }
}
