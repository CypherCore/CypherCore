// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Scripts.Spells.DeathKnight
{
    internal struct SpellIds
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
        public const uint DeathStrikeEnabler = 89832; //Server Side
        public const uint DeathStrikeHeal = 45470;
        public const uint DeathStrikeOffhand = 66188;
        public const uint FesteringWound = 194310;
        public const uint Frost = 137006;
        public const uint FrostFever = 55095;
        public const uint FrostScythe = 207230;
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
            ArmyFleshBeastTransform, ArmyGeistTransform, ArmyNorthrendSkeletonTransform, ArmySkeletonTransform, ArmySpikedGhoulTransform, ArmySuperZombieTransform
        };
    }

    internal struct CreatureIds
    {
        public const uint DancingRuneWeapon = 27893;
    }

    [Script] // 70656 - Advantage (T10 4P Melee Bonus)
    internal class spell_dk_advantage_t10_4p : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit caster = eventInfo.GetActor();

            if (caster)
            {
                Player player = caster.ToPlayer();

                if (!player ||
                    caster.GetClass() != Class.Deathknight)
                    return false;

                for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                    if (player.GetRuneCooldown(i) == 0)
                        return false;

                return true;
            }

            return false;
        }
    }

    [Script] // 48707 - Anti-Magic Shell
    internal class spell_dk_anti_magic_shell : AuraScript, IHasAuraEffects
    {
        private uint absorbedAmount;
        private int absorbPct;
        private ulong maxHealth;

        public spell_dk_anti_magic_shell()
        {
            absorbPct = 0;
            maxHealth = 0;
            absorbedAmount = 0;
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RunicPowerEnergize, SpellIds.VolatileShielding) && spellInfo.GetEffects().Count > 1;
        }

        public override bool Load()
        {
            absorbPct = GetEffectInfo(1).CalcValue(GetCaster());
            maxHealth = GetCaster().GetMaxHealth();
            absorbedAmount = 0;

            return true;
        }

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
            Effects.Add(new EffectAbsorbHandler(Trigger, 0, false, AuraScriptHookType.EffectAfterAbsorb));
            Effects.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount = (int)MathFunctions.CalculatePct(maxHealth, absorbPct);
        }

        private void Trigger(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            absorbedAmount += absorbAmount;

            if (!GetTarget().HasAura(SpellIds.VolatileShielding))
            {
                CastSpellExtraArgs args = new(aurEff);
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(absorbAmount, 2 * absorbAmount * 100 / maxHealth));
                GetTarget().CastSpell(GetTarget(), SpellIds.RunicPowerEnergize, args);
            }
        }

        private void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            AuraEffect volatileShielding = GetTarget().GetAuraEffect(SpellIds.VolatileShielding, 1);

            if (volatileShielding != null)
            {
                CastSpellExtraArgs args = new(volatileShielding);
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(absorbedAmount, volatileShielding.GetAmount()));
                GetTarget().CastSpell((Unit)null, SpellIds.VolatileShieldingDamage, args);
            }
        }
    }

    [Script] // 127517 - Army Transform
    internal class spell_dk_army_transform : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfFoulMenagerie);
        }

        public override bool Load()
        {
            return GetCaster().IsGuardian();
        }

        public SpellCastResult CheckCast()
        {
            Unit owner = GetCaster().GetOwner();

            if (owner)
                if (owner.HasAura(SpellIds.GlyphOfFoulMenagerie))
                    return SpellCastResult.SpellCastOk;

            return SpellCastResult.SpellUnavailable;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.ArmyTransforms.SelectRandom(), true);
        }
    }

    [Script] // 50842 - Blood Boil
    internal class spell_dk_blood_boil : SpellScript, IOnHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodPlague);
        }

        public void OnHit()
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.BloodPlague, true);
        }
    }

    [Script] // 49028 - Dancing Rune Weapon
    internal class spell_dk_dancing_rune_weapon : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            if (Global.ObjectMgr.GetCreatureTemplate(CreatureIds.DancingRuneWeapon) == null)
                return false;

            return true;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        // This is a port of the old switch hack in Unit.cpp, it's not correct
        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();

            if (!caster)
                return;

            Unit drw = null;

            foreach (Unit controlled in caster.Controlled)
                if (controlled.GetEntry() == CreatureIds.DancingRuneWeapon)
                {
                    drw = controlled;

                    break;
                }

            if (!drw ||
                !drw.GetVictim())
                return;

            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo == null)
                return;

            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return;

            int amount = (int)damageInfo.GetDamage() / 2;
            SpellNonMeleeDamage log = new(drw, drw.GetVictim(), spellInfo, new SpellCastVisual(spellInfo.GetSpellXSpellVisualId(drw), 0), spellInfo.GetSchoolMask());
            log.Damage = (uint)amount;
            Unit.DealDamage(drw, drw.GetVictim(), (uint)amount, null, DamageEffectType.SpellDirect, spellInfo.GetSchoolMask(), spellInfo, true);
            drw.SendSpellNonMeleeDamageLog(log);
        }
    }

    [Script] // 43265 - Death and Decay
    internal class spell_dk_death_and_decay_SpellScript : SpellScript, IOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TighteningGrasp, SpellIds.TighteningGraspSlow);
        }

        public void OnCast()
        {
            if (GetCaster().HasAura(SpellIds.TighteningGrasp))
            {
                WorldLocation pos = GetExplTargetDest();

                if (pos != null)
                    GetCaster().CastSpell(pos, SpellIds.TighteningGraspSlow, new CastSpellExtraArgs(true));
            }
        }
    }

    [Script] // 43265 - Death and Decay
    internal class spell_dk_death_and_decay_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(HandleDummyTick, 2, AuraType.PeriodicDummy));
        }

        private void HandleDummyTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (caster)
                caster.CastSpell(GetTarget(), SpellIds.DeathAndDecayDamage, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 47541 - Death Coil
    internal class spell_dk_death_coil : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.DeathCoilDamage, SpellIds.Unholy, SpellIds.UnholyVigor);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.CastSpell(GetHitUnit(), SpellIds.DeathCoilDamage, true);
            AuraEffect unholyAura = caster.GetAuraEffect(SpellIds.Unholy, 6);

            if (unholyAura != null) // can be any effect, just here to send SpellFailedDontReport on failure
                caster.CastSpell(caster, SpellIds.UnholyVigor, new CastSpellExtraArgs(unholyAura));
        }
    }

    [Script] // 52751 - Death Gate
    internal class spell_dk_death_gate : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        public SpellCastResult CheckCast()
        {
            if (GetCaster().GetClass() != Class.Deathknight)
            {
                SetCustomCastResultMessage(SpellCustomErrors.MustBeDeathKnight);

                return SpellCastResult.CustomError;
            }

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Unit target = GetHitUnit();

            if (target)
                target.CastSpell(target, (uint)GetEffectValue(), false);
        }
    }

    [Script] // 49576 - Death Grip Initial
    internal class spell_dk_death_grip_initial : SpellScript, ICheckCastHander, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DeathGripDummy, SpellIds.DeathGripJump, SpellIds.Blood, SpellIds.DeathGripTaunt);
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();

            // Death Grip should not be castable while jumping/falling
            if (caster.HasUnitState(UnitState.Jumping) ||
                caster.HasUnitMovementFlag(MovementFlag.Falling))
                return SpellCastResult.Moving;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.DeathGripDummy, true);
            GetHitUnit().CastSpell(GetCaster(), SpellIds.DeathGripJump, true);

            if (GetCaster().HasAura(SpellIds.Blood))
                GetCaster().CastSpell(GetHitUnit(), SpellIds.DeathGripTaunt, true);
        }
    }

    [Script] // 48743 - Death Pact
    internal class spell_dk_death_pact : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(HandleCalcAmount, 1, AuraType.SchoolHealAbsorb));
        }

        private void HandleCalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();

            if (caster)
                amount = (int)caster.CountPctFromMaxHealth(amount);
        }
    }

    [Script] // 49998 - Death Strike
    internal class spell_dk_death_strike : SpellScript, IAfterCast, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DeathStrikeEnabler, SpellIds.DeathStrikeHeal, SpellIds.BloodShieldMastery, SpellIds.BloodShieldAbsorb, SpellIds.RecentlyUsedDeathStrike, SpellIds.Frost, SpellIds.DeathStrikeOffhand) && spellInfo.GetEffects().Count > 2;
        }

        public void AfterCast()
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.RecentlyUsedDeathStrike, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();

            AuraEffect enabler = caster.GetAuraEffect(SpellIds.DeathStrikeEnabler, 0, GetCaster().GetGUID());

            if (enabler != null)
            {
                // Heals you for 25% of all Damage taken in the last 5 sec,
                int heal = MathFunctions.CalculatePct(enabler.CalculateAmount(GetCaster()), GetEffectInfo(1).CalcValue(GetCaster()));
                // minimum 7.0% of maximum health.
                int pctOfMaxHealth = MathFunctions.CalculatePct(GetEffectInfo(2).CalcValue(GetCaster()), caster.GetMaxHealth());
                heal = Math.Max(heal, pctOfMaxHealth);

                caster.CastSpell(caster, SpellIds.DeathStrikeHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, heal));

                AuraEffect aurEff = caster.GetAuraEffect(SpellIds.BloodShieldMastery, 0);

                if (aurEff != null)
                    caster.CastSpell(caster, SpellIds.BloodShieldAbsorb, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(heal, aurEff.GetAmount())));

                if (caster.HasAura(SpellIds.Frost))
                    caster.CastSpell(GetHitUnit(), SpellIds.DeathStrikeOffhand, true);
            }
        }
    }

    [Script] // 89832 - Death Strike Enabler - SPELL_DK_DEATH_STRIKE_ENABLER
    internal class spell_dk_death_strike_enabler : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        // Amount of seconds we calculate Damage over
        private uint[] _damagePerSecond = new uint[5];

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetDamageInfo() != null;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
            Effects.Add(new EffectCalcAmountHandler(HandleCalcAmount, 0, AuraType.PeriodicDummy));
            Effects.Add(new EffectUpdatePeriodicHandler(Update, 0, AuraType.PeriodicDummy));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void Update(AuraEffect aurEff)
        {
            // Move backwards all datas by one from [23][0][0][0][0] -> [0][23][0][0][0]
            _damagePerSecond = Enumerable.Range(1, _damagePerSecond.Length).Select(i => _damagePerSecond[i % _damagePerSecond.Length]).ToArray();
            _damagePerSecond[0] = 0;
        }

        private void HandleCalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            canBeRecalculated = true;
            amount = Enumerable.Range(1, _damagePerSecond.Length).Sum();
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            _damagePerSecond[0] += eventInfo.GetDamageInfo().GetDamage();
        }
    }

    [Script] // 85948 - Festering Strike
    internal class spell_dk_festering_strike : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FesteringWound);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.FesteringWound, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, GetEffectValue()));
        }
    }

    [Script] // 47496 - Explode, Ghoul spell for Corpse Explosion
    internal class spell_dk_ghoul_explode : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CorpseExplosionTriggered) && spellInfo.GetEffects().Count > 2;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(Suicide, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDamage(uint effIndex)
        {
            SetHitDamage((int)GetCaster().CountPctFromMaxHealth(GetEffectInfo(2).CalcValue(GetCaster())));
        }

        private void Suicide(uint effIndex)
        {
            Unit unitTarget = GetHitUnit();

            if (unitTarget)
                // Corpse Explosion (Suicide)
                unitTarget.CastSpell(unitTarget, SpellIds.CorpseExplosionTriggered, true);
        }
    }

    [Script] // 69961 - Glyph of Scourge Strike
    internal class spell_dk_glyph_of_scourge_strike_script : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            var mPeriodic = target.GetAuraEffectsByType(AuraType.PeriodicDamage);

            foreach (var aurEff in mPeriodic)
            {
                SpellInfo spellInfo = aurEff.GetSpellInfo();

                // search our Blood Plague and Frost Fever on Target
                if (spellInfo.SpellFamilyName == SpellFamilyNames.Deathknight &&
                    spellInfo.SpellFamilyFlags[2].HasAnyFlag(0x2u) &&
                    aurEff.GetCasterGUID() == caster.GetGUID())
                {
                    int countMin = aurEff.GetBase().GetMaxDuration();
                    int countMax = spellInfo.GetMaxDuration();

                    // this Glyph
                    countMax += 9000;

                    if (countMin < countMax)
                    {
                        aurEff.GetBase().SetDuration(aurEff.GetBase().GetDuration() + 3000);
                        aurEff.GetBase().SetMaxDuration(countMin + 3000);
                    }
                }
            }
        }
    }

    [Script] // 49184 - Howling Blast
    class spell_dk_howling_blast : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FrostFever);
        }

        void HandleFrostFever(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.FrostFever);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleFrostFever, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
    
    [Script] // 206940 - Mark of Blood
    internal class spell_dk_mark_of_blood : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MarkOfBloodHeal);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();

            if (caster)
                caster.CastSpell(eventInfo.GetProcTarget(), SpellIds.MarkOfBloodHeal, true);
        }
    }

    [Script] // 207346 - Necrosis
    internal class spell_dk_necrosis : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.NecrosisEffect);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.NecrosisEffect, true);
        }
    }

    [Script] // 121916 - Glyph of the Geist (Unholy)
    internal class spell_dk_pet_geist_transform : SpellScript, ICheckCastHander
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfTheGeist);
        }

        public override bool Load()
        {
            return GetCaster().IsPet();
        }

        public SpellCastResult CheckCast()
        {
            Unit owner = GetCaster().GetOwner();

            if (owner)
                if (owner.HasAura(SpellIds.GlyphOfTheGeist))
                    return SpellCastResult.SpellCastOk;

            return SpellCastResult.SpellUnavailable;
        }
    }

    [Script] // 147157 Glyph of the Skeleton (Unholy)
    internal class spell_dk_pet_skeleton_transform : SpellScript, ICheckCastHander
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlyphOfTheSkeleton);
        }

        public SpellCastResult CheckCast()
        {
            Unit owner = GetCaster().GetOwner();

            if (owner)
                if (owner.HasAura(SpellIds.GlyphOfTheSkeleton))
                    return SpellCastResult.SpellCastOk;

            return SpellCastResult.SpellUnavailable;
        }
    }

    [Script] // 61257 - Runic Power Back on Snare/Root
    internal class spell_dk_pvp_4p_bonus : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RunicReturn);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo == null)
                return false;

            return (spellInfo.GetAllEffectsMechanicMask() & ((1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Snare))) != 0;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActionTarget().CastSpell((Unit)null, SpellIds.RunicReturn, true);
        }
    }

    [Script] // 46584 - Raise Dead
    internal class spell_dk_raise_dead : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RaiseDeadSummon, SpellIds.SludgeBelcher, SpellIds.SludgeBelcherSummon);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            uint spellId = SpellIds.RaiseDeadSummon;

            if (GetCaster().HasAura(SpellIds.SludgeBelcher))
                spellId = SpellIds.SludgeBelcherSummon;

            GetCaster().CastSpell((Unit)null, spellId, true);
        }
    }

    [Script] // 59057 - Rime
    internal class spell_dk_rime : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 1 && ValidateSpellInfo(SpellIds.FrostScythe);
        }

        public override void Register()
        {
            Effects.Add(new CheckEffectProcHandler(CheckProc, 0, AuraType.ProcTriggerSpell));
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            float chance = (float)GetSpellInfo().GetEffect(1).CalcValue(GetTarget());

            if (eventInfo.GetSpellInfo().Id == SpellIds.FrostScythe)
                chance /= 2.0f;

            return RandomHelper.randChance(chance);
        }
    }

    [Script] // 55233 - Vampiric Blood
    internal class spell_dk_vampiric_blood : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseHealth2));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            amount = (int)GetUnitOwner().CountPctFromMaxHealth(amount);
        }
    }
}