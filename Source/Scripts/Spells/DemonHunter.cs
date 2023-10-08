// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

using static Global;

namespace Scripts.Spells.DemonHunter
{
    struct SpellIds
    {
        public const uint AbyssalStrike = 207550;
        public const uint Annihilation = 201427;
        public const uint AnnihilationMh = 227518;
        public const uint AnnihilationOh = 201428;
        public const uint AwakenTheDemonWithinCd = 207128;
        public const uint Blur = 212800;
        public const uint BlurTrigger = 198589;
        public const uint BurningAlive = 207739;
        public const uint BurningAliveTargetSelector = 207760;
        public const uint ChaosNova = 179057;
        public const uint ChaosStrike = 162794;
        public const uint ChaosStrikeEnergize = 193840;
        public const uint ChaosStrikeMh = 222031;
        public const uint ChaosStrikeOh = 199547;
        public const uint ConsumeSoulHavoc = 228542;
        public const uint ConsumeSoulHavocDemon = 228556;
        public const uint ConsumeSoulHavocShattered = 228540;
        public const uint ConsumeSoulHeal = 203794;
        public const uint ConsumeSoulVengeance = 208014;
        public const uint ConsumeSoulVengeanceDemon = 210050;
        public const uint ConsumeSoulVengeanceShattered = 210047;
        public const uint DarknessAbsorb = 209426;
        public const uint DemonBladesDmg = 203796;
        public const uint DemonSpikes = 203819;
        public const uint DemonSpikesTrigger = 203720;
        public const uint Demonic = 213410;
        public const uint DemonicOrigins = 235893;
        public const uint DemonicOriginsBuff = 235894;
        public const uint DemonicTrampleDmg = 208645;
        public const uint DemonicTrampleStun = 213491;
        public const uint DemonsBite = 162243;
        public const uint EyeBeam = 198013;
        public const uint EyeBeamDmg = 198030;
        public const uint EyeOfLeotherasDmg = 206650;
        public const uint FeastOfSouls = 207697;
        public const uint FeastOfSoulsPeriodicHeal = 207693;
        public const uint FeedTheDemon = 218612;
        public const uint FelBarrage = 211053;
        public const uint FelBarrageDmg = 211052;
        public const uint FelBarrageProc = 222703;
        public const uint FelDevastation = 212084;
        public const uint FelDevastationDmg = 212105;
        public const uint FelDevastationHeal = 212106;
        public const uint FelRush = 195072;
        public const uint FelRushDmg = 192611;
        public const uint FelRushGround = 197922;
        public const uint FelRushWaterAir = 197923;
        public const uint Felblade = 232893;
        public const uint FelbladeCharge = 213241;
        public const uint FelbladeDmg = 213243;
        public const uint FelbladeProc = 203557;
        public const uint FelbladeProcVisual = 204497;
        public const uint FelbladeProc1 = 236167;
        public const uint FieryBrand = 204021;
        public const uint FieryBrandDmgReductionDebuff = 207744;
        public const uint FieryBrandDot = 207771;
        public const uint FirstBlood = 206416;
        public const uint FlameCrash = 227322;
        public const uint Frailty = 224509;
        public const uint Glide = 131347;
        public const uint GlideDuration = 197154;
        public const uint GlideKnockback = 196353;
        public const uint HavocMastery = 185164;
        public const uint IllidansGrasp = 205630;
        public const uint IllidansGraspDamage = 208618;
        public const uint IllidansGraspJumpDest = 208175;
        public const uint InfernalStrikeCast = 189110;
        public const uint InfernalStrikeImpactDamage = 189112;
        public const uint InfernalStrikeJump = 189111;
        public const uint JaggedSpikes = 205627;
        public const uint JaggedSpikesDmg = 208790;
        public const uint JaggedSpikesProc = 208796;
        public const uint ManaRiftDmgPowerBurn = 235904;
        public const uint Metamorphosis = 191428;
        public const uint MetamorphosisDummy = 191427;
        public const uint MetamorphosisImpactDamage = 200166;
        public const uint MetamorphosisReset = 320645;
        public const uint MetamorphosisTransform = 162264;
        public const uint MetamorphosisVengeanceTransform = 187827;
        public const uint Momentum = 208628;
        public const uint NemesisAberrations = 208607;
        public const uint NemesisBeasts = 208608;
        public const uint NemesisCritters = 208609;
        public const uint NemesisDemons = 208608;
        public const uint NemesisDragonkin = 208610;
        public const uint NemesisElementals = 208611;
        public const uint NemesisGiants = 208612;
        public const uint NemesisHumanoids = 208605;
        public const uint NemesisMechanicals = 208613;
        public const uint NemesisUndead = 208614;
        public const uint RainFromAbove = 206803;
        public const uint RainOfChaos = 205628;
        public const uint RainOfChaosImpact = 232538;
        public const uint RazorSpikes = 210003;
        public const uint Sever = 235964;
        public const uint ShatterSoul = 209980;
        public const uint ShatterSoul1 = 209981;
        public const uint ShatterSoul2 = 210038;
        public const uint ShatteredSoul = 226258;
        public const uint ShatteredSoulLesserSoulFragment1 = 228533;
        public const uint ShatteredSoulLesserSoulFragment2 = 237867;
        public const uint Shear = 203782;
        public const uint SigilOfChainsAreaSelector = 204834;
        public const uint SigilOfChainsGrip = 208674;
        public const uint SigilOfChainsJump = 208674;
        public const uint SigilOfChainsSlow = 204843;
        public const uint SigilOfChainsSnare = 204843;
        public const uint SigilOfChainsTargetSelect = 204834;
        public const uint SigilOfChainsVisual = 208673;
        public const uint SigilOfFlameAoe = 204598;
        public const uint SigilOfFlameDamage = 204598;
        public const uint SigilOfFlameFlameCrash = 228973;
        public const uint SigilOfMisery = 207685;
        public const uint SigilOfMiseryAoe = 207685;
        public const uint SigilOfSilence = 204490;
        public const uint SigilOfSilenceAoe = 204490;
        public const uint SoulBarrier = 227225;
        public const uint SoulCleave = 228477;
        public const uint SoulCleaveDmg = 228478;
        public const uint SoulFragmentCounter = 203981;
        public const uint SoulFurnaceDamageBuff = 391172;
        public const uint SoulRending = 204909;
        public const uint SpiritBombDamage = 218677;
        public const uint SpiritBombHeal = 227255;
        public const uint SpiritBombVisual = 218678;
        public const uint ThrowGlaive = 185123;
        public const uint UncontainedFel = 209261;
        public const uint VengefulRetreat = 198813;
        public const uint VengefulRetreatTrigger = 198793;
    }

    [Script] // 197125 - Chaos Strike
    class spell_dh_chaos_strike : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChaosStrikeEnergize);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
            args.SetTriggeringAura(aurEff);
            GetTarget().CastSpell(GetTarget(), SpellIds.ChaosStrikeEnergize, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 206416 - First Blood
    class spell_dh_first_blood : AuraScript
    {
        ObjectGuid _firstTargetGUID;

        public ObjectGuid GetFirstTarget() { return _firstTargetGUID; }

        public void SetFirstTarget(ObjectGuid targetGuid) { _firstTargetGUID = targetGuid; }

        public override void Register() { }
    }

    // 188499 - Blade Dance
    [Script] // 210152 - Death Sweep
    class spell_dh_blade_dance : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FirstBlood);
        }

        void DecideFirstTarget(List<WorldObject> targetList)
        {
            if (targetList.Empty())
                return;

            Aura aura = GetCaster().GetAura(SpellIds.FirstBlood);
            if (aura == null)
                return;

            ObjectGuid firstTargetGUID = ObjectGuid.Empty;
            ObjectGuid selectedTarget = GetCaster().GetTarget();

            // Prefer the selected target if he is one of the enemies
            if (targetList.Count > 1 && !selectedTarget.IsEmpty())
            {
                var obj = targetList.Find(obj => obj.GetGUID() == selectedTarget);
                if (obj != null)
                    firstTargetGUID = obj.GetGUID();
            }

            if (firstTargetGUID.IsEmpty())
                firstTargetGUID = targetList[0].GetGUID();

            spell_dh_first_blood script = aura.GetScript<spell_dh_first_blood>();
            if (script != null)
                script.SetFirstTarget(firstTargetGUID);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(DecideFirstTarget, 0, Targets.UnitSrcAreaEnemy));
        }
    }

    // 199552 - Blade Dance
    // 200685 - Blade Dance
    // 210153 - Death Sweep
    [Script] // 210155 - Death Sweep
    class spell_dh_blade_dance_damage : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FirstBlood);
        }

        void HandleHitTarget()
        {
            int damage = GetHitDamage();

            AuraEffect aurEff = GetCaster().GetAuraEffect(SpellIds.FirstBlood, 0);
            if (aurEff != null)
            {
                spell_dh_first_blood script = aurEff.GetBase().GetScript<spell_dh_first_blood>();
                if (script != null)
                    if (GetHitUnit().GetGUID() == script.GetFirstTarget())
                        MathFunctions.AddPct(ref damage, aurEff.GetAmount());
            }

            SetHitDamage(damage);
        }

        public override void Register()
        {
            OnHit.Add(new(HandleHitTarget));
        }
    }

    [Script("areatrigger_dh_sigil_of_silence", SpellIds.SigilOfSilenceAoe)]
    [Script("areatrigger_dh_sigil_of_misery", SpellIds.SigilOfMiseryAoe)]
    [Script("areatrigger_dh_sigil_of_flame", SpellIds.SigilOfFlameAoe)]
    class areatrigger_dh_generic_sigil : AreaTriggerAI
    {
        uint _trigger;

        public areatrigger_dh_generic_sigil(AreaTrigger at, uint trigger) : base(at)
        {
            _trigger = trigger;
        }

        public override void OnRemove()
        {
            Unit caster = at.GetCaster();
            if (caster != null)
                caster.CastSpell(at.GetPosition(), _trigger);
        }
    }

    [Script] // 208673 - Sigil of Chains
    class spell_dh_sigil_of_chains : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SigilOfChainsSlow, SpellIds.SigilOfChainsGrip);
        }

        void HandleEffectHitTarget(uint effIndex)
        {
            WorldLocation loc = GetExplTargetDest();
            if (loc != null)
            {
                GetCaster().CastSpell(GetHitUnit(), SpellIds.SigilOfChainsSlow, true);
                GetHitUnit().CastSpell(loc.GetPosition(), SpellIds.SigilOfChainsGrip, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 202138 - Sigil of Chains
    class areatrigger_dh_sigil_of_chains : AreaTriggerAI
    {
        public areatrigger_dh_sigil_of_chains(AreaTrigger at) : base(at) { }

        public override void OnRemove()
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                caster.CastSpell(at.GetPosition(), SpellIds.SigilOfChainsVisual);
                caster.CastSpell(at.GetPosition(), SpellIds.SigilOfChainsTargetSelect);
            }
        }
    }

    [Script] // 131347 - Glide
    class spell_dh_glide : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlideKnockback, SpellIds.GlideDuration, SpellIds.VengefulRetreatTrigger, SpellIds.FelRush);
        }

        SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (caster.IsMounted() || caster.GetVehicleBase() != null)
                return SpellCastResult.DontReport;

            if (!caster.IsFalling())
                return SpellCastResult.NotOnGround;

            return SpellCastResult.SpellCastOk;
        }

        void HandleCast()
        {
            Player caster = GetCaster().ToPlayer();
            if (caster == null)
                return;

            caster.CastSpell(caster, SpellIds.GlideKnockback, true);
            caster.CastSpell(caster, SpellIds.GlideDuration, true);

            caster.GetSpellHistory().StartCooldown(SpellMgr.GetSpellInfo(SpellIds.VengefulRetreatTrigger, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
            caster.GetSpellHistory().StartCooldown(SpellMgr.GetSpellInfo(SpellIds.FelRush, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
            BeforeCast.Add(new(HandleCast));
        }
    }

    [Script] // 131347 - Glide
    class spell_dh_glide_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GlideDuration);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.GlideDuration);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.FeatherFall, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 197154 - Glide
    class spell_dh_glide_timer : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Glide);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAura(SpellIds.Glide);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 391166 - Soul Furnace
    class spell_dh_soul_furnace : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SoulFurnaceDamageBuff);
        }

        void CalculateSpellMod(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetStackAmount() == GetAura().CalcMaxStackAmount())
            {
                GetTarget().CastSpell(GetTarget(), SpellIds.SoulFurnaceDamageBuff, true);
                Remove();
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(CalculateSpellMod, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script] // 339424 - Soul Furnace
    class spell_dh_soul_furnace_conduit : AuraScript
    {
        void CalculateSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
        {
            if (aurEff.GetAmount() == 10)
            {
                if (spellMod == null)
                {
                    spellMod = new SpellModifierByClassMask(GetAura());
                    spellMod.op = SpellModOp.HealingAndDamage;
                    spellMod.type = SpellModType.Pct;
                    spellMod.spellId = GetId();
                    (spellMod as SpellModifierByClassMask).mask = new FlagArray128(0x80000000);
                    (spellMod as SpellModifierByClassMask).value = GetEffect(1).GetAmount() + 1;
                }
            }
        }

        public override void Register()
        {
            DoEffectCalcSpellMod.Add(new(CalculateSpellMod, 0, AuraType.Dummy));
        }
    }
}