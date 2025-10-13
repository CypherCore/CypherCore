// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Shaman;

struct SpellIds
{
    public const uint AftershockEnergize = 210712;
    public const uint AncestralGuidance = 108281;
    public const uint AncestralGuidanceHeal = 114911;
    public const uint ArcticSnowstormAreatrigger = 462767;
    public const uint ArcticSnowstormSlow = 462765;
    public const uint AscendanceElemental = 114050;
    public const uint AscendanceEnhancement = 114051;
    public const uint AscendanceRestoration = 114052;
    public const uint ChainLightning = 188443;
    public const uint ChainLightningEnergize = 195897;
    public const uint ChainLightningOverload = 45297;
    public const uint ChainLightningOverloadEnergize = 218558;
    public const uint ChainedHeal = 70809;
    public const uint ConvergingStorms = 384363;
    public const uint CrashLightning = 187874;
    public const uint CrashLightningCleave = 187878;
    public const uint CrashLightningDamageBuff = 333964;
    public const uint DelugeAura = 200075;
    public const uint DelugeTalent = 200076;
    public const uint DoomWindsDamage = 469270;
    public const uint DoomWindsLegendaryCooldown = 335904;
    public const uint Earthquake = 61882;
    public const uint EarthquakeKnockingDown = 77505;
    public const uint EarthquakeTick = 77478;
    public const uint EarthShieldHeal = 204290;
    public const uint EarthenRagePassive = 170374;
    public const uint EarthenRagePeriodic = 170377;
    public const uint EarthenRageDamage = 170379;
    public const uint EchoesOfGreatSunderingLegendary = 336217;
    public const uint EchoesOfGreatSunderingTalent = 384088;
    public const uint Electrified = 64930;
    public const uint ElementalBlast = 117014;
    public const uint ElementalBlastCrit = 118522;
    public const uint ElementalBlastHaste = 173183;
    public const uint ElementalBlastMastery = 173184;
    public const uint ElementalBlastOverload = 120588;
    public const uint ElementalMastery = 16166;
    public const uint ElementalWeaponsBuff = 408390;
    public const uint EnergySurge = 40465;
    public const uint EnhancedElements = 77223;
    public const uint FireNovaDamage = 333977;
    public const uint FireNovaEnabler = 466622;
    public const uint FlameShock = 188389;
    public const uint FlametongueAttack = 10444;
    public const uint FlametongueWeaponEnchant = 334294;
    public const uint FlametongueWeaponAura = 319778;
    public const uint ForcefulWindsProc = 262652;
    public const uint ForcefulWindsTalent = 262647;
    public const uint FrostShock = 196840;
    public const uint FrostShockEnergize = 289439;
    public const uint GatheringStorms = 198299;
    public const uint GatheringStormsBuff = 198300;
    public const uint GhostWolf = 2645;
    public const uint HailstormBuff = 334196;
    public const uint HailstormTalent = 334195;
    public const uint HealingRainVisual = 147490;
    public const uint HealingRain = 73920;
    public const uint HealingRainHeal = 73921;
    public const uint IceStrikeOverrideAura = 466469;
    public const uint IceStrikeProc = 466467;
    public const uint Icefury = 210714;
    public const uint IcefuryOverload = 219271;
    public const uint IgneousPotential = 279830;
    public const uint ItemLightningShield = 23552;
    public const uint ItemLightningShieldDamage = 27635;
    public const uint ItemManaSurge = 23571;
    public const uint LavaBurst = 51505;
    public const uint LavaBurstBonusDamage = 71824;
    public const uint LavaBurstOverload = 77451;
    public const uint LavaLash = 60103;
    public const uint LavaSurge = 77762;
    public const uint LightningBolt = 188196;
    public const uint LightningBoltEnergize = 214815;
    public const uint LightningBoltOverload = 45284;
    public const uint LightningBoltOverloadEnergize = 214816;
    public const uint LiquidMagmaHit = 192231;
    public const uint MaelstromController = 343725;
    public const uint MaelstromWeaponModAura = 187881;
    public const uint MaelstromWeaponVisibleAura = 344179;
    public const uint MaelstromWeaponOverlay = 187890;
    public const uint MaelstromWeaponOverlayHeals = 412692;
    public const uint MasteryElementalOverload = 168534;
    public const uint MoltenAssault = 334033;
    public const uint MoltenThunderProc = 469346;
    public const uint MoltenThunderTalent = 469344;
    public const uint NaturesGuardianCooldown = 445698;
    public const uint OverflowingMaelstromAura = 384669;
    public const uint OverflowingMaelstromTalent = 384149;
    public const uint PathOfFlamesSpread = 210621;
    public const uint PathOfFlamesTalent = 201909;
    public const uint PowerSurge = 40466;
    public const uint PrimordialWaveDamage = 375984;
    public const uint RestorativeMists = 114083;
    public const uint RestorativeMistsInitial = 294020;
    public const uint Riptide = 61295;
    public const uint SpiritWolfTalent = 260878;
    public const uint SpiritWolfPeriodic = 260882;
    public const uint SpiritWolfAura = 260881;
    public const uint StormblastDamage = 390287;
    public const uint StormblastProc = 470466;
    public const uint StormblastTalent = 319930;
    public const uint Stormflurry = 344357;
    public const uint StormflurryArtifact = 198367;
    public const uint Stormkeeper = 191634;
    public const uint Stormstrike = 17364;
    public const uint StormstrikeDamageMainHand = 32175;
    public const uint StormstrikeDamageOffHand = 32176;
    public const uint StormsurgeProc = 201846;
    public const uint StormweaverPvpTalent = 410673;
    public const uint StormweaverPvpTalentBuff = 410681;
    public const uint T29_2PElementalDamageBuff = 394651;
    public const uint ThorimsInvocation = 384444;
    public const uint TidalWaves = 53390;
    public const uint TotemicPowerArmor = 28827;
    public const uint TotemicPowerAttackPower = 28826;
    public const uint TotemicPowerMP5 = 28824;
    public const uint TotemicPowerSpellPower = 28825;
    public const uint UndulationProc = 216251;
    public const uint UnlimitedPowerBuff = 272737;
    public const uint UnrelentingStormsReduction = 470491;
    public const uint UnrelentingStormsTalent = 470490;
    public const uint UnrulyWinds = 390288;
    public const uint VoltaicBlazeDamage = 470057;
    public const uint VoltaicBlazeOverride = 470058;
    public const uint WindfuryAttack = 25504;
    public const uint WindfuryAura = 319773;
    public const uint WindfuryEnchantment = 334302;
    public const uint WindfuryVisual1 = 466440;
    public const uint WindfuryVisual2 = 466442;
    public const uint WindfuryVisual3 = 466443;
    public const uint WindRush = 192082;
    public const uint WindstrikeDamageMainHand = 115357;
    public const uint WindstrikeDamageOffHand = 115360;


    public const uint LabelShamanWindfuryTotem = 1038;
}

class spell_sha_maelstrom_weapon_base
{
    public static bool Validate()
    {
        return SpellScriptBase.ValidateSpellInfo
            (SpellIds.MaelstromWeaponVisibleAura,
            SpellIds.MaelstromWeaponOverlay,
            SpellIds.MaelstromWeaponOverlayHeals,
            SpellIds.OverflowingMaelstromTalent,
            SpellIds.OverflowingMaelstromAura,
            SpellIds.StormweaverPvpTalentBuff,
            SpellIds.IceStrikeProc,
            SpellIds.HailstormBuff,
            SpellIds.HailstormTalent)
            && SpellScriptBase.ValidateSpellEffect((SpellIds.MaelstromWeaponModAura, 1), (SpellIds.StormweaverPvpTalent, 0));
    }

    public static void GenerateMaelstromWeapon(Unit shaman, int stacks)
    {
        CastSpellExtraArgs args = new();
        args.SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        while (--stacks >= 0)
        {
            uint totalStacks = shaman.GetAuraCount(SpellIds.MaelstromWeaponVisibleAura);
            if (totalStacks >= 4)
            {
                // cast action bar overlays
                if (!shaman.HasAura(SpellIds.StormweaverPvpTalent))
                    shaman.CastSpell(shaman, SpellIds.MaelstromWeaponOverlayHeals, args);

                shaman.CastSpell(shaman, SpellIds.MaelstromWeaponOverlay, args);
            }

            shaman.CastSpell(shaman, SpellIds.MaelstromWeaponModAura, args);
            shaman.CastSpell(shaman, SpellIds.MaelstromWeaponVisibleAura, args);
            if (totalStacks >= 5 && shaman.HasAura(SpellIds.OverflowingMaelstromTalent))
                shaman.CastSpell(shaman, SpellIds.OverflowingMaelstromAura, args);
        }
    }

    public static void ConsumeMaelstromWeapon(Unit shaman, Aura maelstromWeaponVisibleAura, int stacks, Spell consumingSpell = null)
    {
        AuraEffect stormweaver = shaman.GetAuraEffect(SpellIds.StormweaverPvpTalent, 0);
        if (stormweaver != null)
        {
            shaman.RemoveAurasDueToSpell(SpellIds.StormweaverPvpTalentBuff);    // remove to ensure new buff has exactly "consumedStacks" stacks
            Aura maelstromSpellMod = shaman.GetAura(SpellIds.MaelstromWeaponModAura);
            if (maelstromSpellMod != null)
            {
                shaman.CastSpell(shaman, SpellIds.StormweaverPvpTalentBuff, new CastSpellExtraArgs()
                {
                    TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                    TriggeringSpell = consumingSpell,
                    SpellValueOverrides =
                    {
                        new(SpellValueMod.BasePoint0, maelstromSpellMod.GetEffect(0).GetAmount()),
                        // this is indeed very silly but it is how it behaves on official servers
                        // it ignores how many stacks were actually consumed and calculates benefit from max stacks (Thorim's Invocation can consume less stacks than entire aura)
                        new(SpellValueMod.BasePoint1, MathFunctions.CalculatePct(maelstromSpellMod.GetEffect(1).GetAmount(), stormweaver.GetAmount())),
                        new(SpellValueMod.AuraStack, Math.Min(stacks, maelstromWeaponVisibleAura.GetStackAmount()))
                    }
                });
            }
        }

        Aura iceStrike = shaman.GetAura(SpellIds.IceStrikeProc);
        if (iceStrike != null)
        {
            spell_sha_ice_strike_proc script = iceStrike.GetScript<spell_sha_ice_strike_proc>();
            if (script != null)
                script.AttemptProc();
        }

        if (shaman.HasAura(SpellIds.HailstormTalent))
            shaman.CastSpell(shaman, SpellIds.HailstormBuff, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = consumingSpell,
                SpellValueOverrides = { new(SpellValueMod.AuraStack, Math.Min(stacks, maelstromWeaponVisibleAura.GetStackAmount())) }
            });

        if (maelstromWeaponVisibleAura.ModStackAmount(-stacks))
            return;

        // not removed
        byte newStacks = maelstromWeaponVisibleAura.GetStackAmount();

        Aura overflowingMaelstrom = shaman.GetAura(SpellIds.OverflowingMaelstromAura);
        if (overflowingMaelstrom != null)
        {
            if (newStacks > 5)
                overflowingMaelstrom.SetStackAmount((byte)(newStacks - 5));
            else
                overflowingMaelstrom.Remove();
        }

        Aura maelstromSpellMod1 = shaman.GetAura(SpellIds.MaelstromWeaponModAura);
        if (maelstromSpellMod1 != null)
        {
            if (newStacks > 0)
                maelstromSpellMod1.SetStackAmount(Math.Min(newStacks, (byte)5));
            else
                maelstromSpellMod1.Remove();
        }

        if (newStacks < 5)
        {
            shaman.RemoveAurasDueToSpell(SpellIds.MaelstromWeaponOverlay);
            shaman.RemoveAurasDueToSpell(SpellIds.MaelstromWeaponOverlayHeals);
        }
    }
}

class WindfuryProcEvent : BasicEvent
{
    Unit _shaman;
    CastSpellTargetArg _target;
    int _index;
    int _endIndex;

    class WindfuryProcEventInfo
    {
        public TimeSpan Delay;
        public uint VisualSpellId;
    }

    static WindfuryProcEventInfo[] Sequence =
    {
        new WindfuryProcEventInfo() { Delay = TimeSpan.FromSeconds(500), VisualSpellId = SpellIds.WindfuryVisual1 },
        new WindfuryProcEventInfo() { Delay = TimeSpan.FromSeconds(150), VisualSpellId = SpellIds.WindfuryVisual2 },
        new WindfuryProcEventInfo() { Delay = TimeSpan.FromSeconds(250), VisualSpellId = SpellIds.WindfuryVisual3 },
    };

    public WindfuryProcEvent(Unit shaman, Unit target, int attacks)
    {
        _shaman = shaman;
        _target = target;
        _index = 0;
        _endIndex = attacks;
    }

    public override bool Execute(ulong time, uint diff)
    {
        if (_target.Targets == null)
            return true;

        _target.Targets.Update(_shaman);
        if (_target.Targets.GetUnitTarget() == null)
            return true;

        CastSpellExtraArgs args = new();
        args.TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError;
        args.TriggeringAura = _shaman.GetAuraEffect(SpellIds.WindfuryAura, 0); // prevent proc from itself

        _shaman.CastSpell(_shaman, Sequence[_index].VisualSpellId, args);
        _shaman.CastSpell(_target, SpellIds.WindfuryAttack, args);

        if (++_index == _endIndex)
            return true;

        _shaman.m_Events.AddEvent(this, TimeSpan.FromSeconds(time) + Sequence[_index].Delay);
        return false;
    }

    public static void Trigger(Unit shaman, Unit target)
    {
        // Not a separate script because of ordering requirements for Forceful Winds
        if (shaman.HasAuraEffect(SpellIds.ForcefulWindsTalent, 0))
        {
            Aura forcefulWinds = shaman.GetAura(SpellIds.ForcefulWindsProc);
            if (forcefulWinds != null)
            {
                // gaining a stack should not refresh duration
                uint maxStack = forcefulWinds.CalcMaxStackAmount();
                if (forcefulWinds.GetStackAmount() < maxStack)
                    forcefulWinds.SetStackAmount((byte)(forcefulWinds.GetStackAmount() + 1));
            }
            else
            {
                shaman.CastSpell(shaman, SpellIds.ForcefulWindsProc, new CastSpellExtraArgs()
                {
                    TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError
                });
            }
        }

        int attacks = 2;
        AuraEffect unrulyWinds = shaman.GetAuraEffect(SpellIds.UnrulyWinds, 0); RandomHelper.randChance(unrulyWinds.GetAmount());
        if (unrulyWinds != null)
            ++attacks;

        shaman.m_Events.AddEventAtOffset(new WindfuryProcEvent(shaman, target, attacks), Sequence.First().Delay);
    }
}

[Script] // 273221 - Aftershock
class spell_sha_aftershock : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AftershockEnergize);
    }

    static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell != null)
        {
            var cost = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);
            if (cost.HasValue)
                return cost > 0 && RandomHelper.randChance(aurEff.GetAmount());
        }

        return false;
    }

    static void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Spell procSpell = eventInfo.GetProcSpell();
        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.AftershockEnergize, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = procSpell,
            TriggeringAura = aurEff,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom).Value) }
        });
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 108281 - Ancestral Guidance
class spell_sha_ancestral_guidance : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AncestralGuidanceHeal);
    }

    static bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().Id == SpellIds.AncestralGuidanceHeal)
            return false;

        if (eventInfo.GetHealInfo() == null && eventInfo.GetDamageInfo() == null)
            return false;

        return true;
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        int bp0 = MathFunctions.CalculatePct((int)(eventInfo.GetDamageInfo() != null ? eventInfo.GetDamageInfo().GetDamage() : eventInfo.GetHealInfo().GetHeal()), aurEff.GetAmount());
        if (bp0 == 0)
            return;

        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.AncestralGuidanceHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, bp0) }
        });
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 114911 - Ancestral Guidance Heal
class spell_sha_ancestral_guidance_heal : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AncestralGuidance);
    }

    void ResizeTargets(List<WorldObject> targets)
    {
        SelectRandomInjuredTargets(targets, 3, true);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(ResizeTargets, 0, Targets.UnitCasterAreaRaid));
    }
}

[Script] // 462764 - Arctic Snowstorm
class spell_sha_arctic_snowstorm : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ArcticSnowstormAreatrigger);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActionTarget(), SpellIds.ArcticSnowstormAreatrigger,
           new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 198299 - Gathering Storms
class spell_sha_artifact_gathering_storms : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GatheringStorms, SpellIds.GatheringStormsBuff);
    }

    public override bool Load()
    {
        return GetCaster().HasAuraEffect(SpellIds.GatheringStorms, 0);
    }

    void TriggerBuff(uint effIndex)
    {
        AuraEffect gatheringStorms = GetCaster().GetAuraEffect(SpellIds.GatheringStorms, 0);
        if (gatheringStorms == null)
            return;

        GetCaster().CastSpell(GetCaster(), SpellIds.GatheringStormsBuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, (int)(gatheringStorms.GetAmount() * GetUnitTargetCountForEffect(effIndex))) }
        });
    }

    public override void Register()
    {
        OnEffectHit.Add(new(TriggerBuff, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 114052 - Ascendance (Restoration)
class spell_sha_ascendance_restoration : AuraScript
{
    int _healToDistribute;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RestorativeMists);
    }

    static bool CheckProc(ProcEventInfo procInfo)
    {
        return procInfo.GetHealInfo() != null && procInfo.GetHealInfo().GetOriginalHeal() != 0 && procInfo.GetSpellInfo().Id != SpellIds.RestorativeMistsInitial;
    }

    void OnProcHeal(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        _healToDistribute += MathFunctions.CalculatePct((int)(procInfo.GetHealInfo().GetOriginalHeal()), aurEff.GetAmount());
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        if (_healToDistribute == 0)
            return;

        GetTarget().CastSpell(null, SpellIds.RestorativeMists, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, _healToDistribute) }
        });
        _healToDistribute = 0;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(OnProcHeal, 8, AuraType.Dummy));
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 6, AuraType.PeriodicDummy));
    }
}

[Script] // 390370 - Ashen Catalyst
class spell_sha_ashen_catalyst : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LavaLash);
    }

    void ReduceLavaLashCooldown(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        GetTarget().GetSpellHistory().ModifyCooldown(SpellIds.LavaLash, -aurEff.GetAmount() * TimeSpan.FromSeconds(100));
    }

    public override void Register()
    {
        OnEffectProc.Add(new(ReduceLavaLashCooldown, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 188443 - Chain Lightning
class spell_sha_chain_lightning_crash_lightning : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CrashLightning, SpellIds.CrashLightningDamageBuff);
    }

    public override bool Load()
    {
        return GetCaster().HasSpell(SpellIds.CrashLightning);
    }

    void HandleCooldownReduction(uint effIndex)
    {
        GetCaster().GetSpellHistory().ModifyCooldown(SpellIds.CrashLightning, TimeSpan.FromSeconds(-GetEffectValue()) * GetUnitTargetCountForEffect(0));
    }

    void HandleDamageBuff(uint effIndex)
    {
        long targetsHit = GetUnitTargetCountForEffect(effIndex);
        if (targetsHit > 1)
            GetCaster().CastSpell(GetCaster(), SpellIds.CrashLightningDamageBuff, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                SpellValueOverrides = { new(SpellValueMod.AuraStack, (int)targetsHit) }
            });
    }

    public override void Register()
    {
        OnEffectLaunch.Add(new(HandleCooldownReduction, 2, SpellEffectName.Dummy));
        OnEffectLaunch.Add(new(HandleDamageBuff, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 188443 - Chain Lightning
class spell_sha_chain_lightning_energize : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ChainLightningEnergize)
            && ValidateSpellEffect((SpellIds.MaelstromController, 4));
    }

    public override bool Load()
    {
        return GetCaster().HasAuraEffect(SpellIds.MaelstromController, 4);
    }

    void HandleScript(uint effIndex)
    {
        AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 4);
        if (energizeAmount != null)
            GetCaster().CastSpell(GetCaster(), SpellIds.ChainLightningEnergize, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = energizeAmount,
                SpellValueOverrides = { new(SpellValueMod.BasePoint0, (int)(energizeAmount.GetAmount() * GetUnitTargetCountForEffect(0))) }
            });
    }

    public override void Register()
    {
        OnEffectLaunch.Add(new(HandleScript, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 45297 - Chain Lightning Overload
class spell_sha_chain_lightning_overload : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ChainLightningOverloadEnergize)
            && ValidateSpellEffect((SpellIds.MaelstromController, 5));
    }

    public override bool Load()
    {
        return GetCaster().HasAuraEffect(SpellIds.MaelstromController, 5);
    }

    void HandleScript(uint effIndex)
    {
        AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 5);
        if (energizeAmount != null)
            GetCaster().CastSpell(GetCaster(), SpellIds.ChainLightningOverloadEnergize, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = energizeAmount,
                SpellValueOverrides = { new(SpellValueMod.BasePoint0, (int)(energizeAmount.GetAmount() * GetUnitTargetCountForEffect(0))) }
            });
    }

    public override void Register()
    {
        OnEffectLaunch.Add(new(HandleScript, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 384363 - Converging Storms
class spell_sha_converging_storms : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ConvergingStorms, SpellIds.GatheringStormsBuff);
    }

    public override bool Load()
    {
        return GetCaster().HasAuraEffect(SpellIds.ConvergingStorms, 0);
    }

    void TriggerBuff(uint effIndex)
    {
        AuraEffect convergingStorms = GetCaster().GetAuraEffect(SpellIds.ConvergingStorms, 0);
        if (convergingStorms == null)
            return;

        GetCaster().CastSpell(GetCaster(), SpellIds.GatheringStormsBuff, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            SpellValueOverrides = { new(SpellValueMod.AuraStack, (int)Math.Min(GetUnitTargetCountForEffect(effIndex), 6)) }
        });
    }

    public override void Register()
    {
        OnEffectHit.Add(new(TriggerBuff, 0, SpellEffectName.SchoolDamage));
    }
}

[Script("spell_sha_stormsurge_proc")]
[Script("spell_sha_converging_storms_buff")] // 198300 - Converging Storms
class spell_sha_delayed_stormstrike_mod_charge_drop_proc : AuraScript
{
    void DropAura(ProcEventInfo eventInfo)
    {
        GetAura().DropChargeDelayed(1);
    }

    public override void Register()
    {
        AfterProc.Add(new(DropAura));
    }
}

[Script] // 187874 - Crash Lightning
class spell_sha_crash_lightning : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CrashLightningCleave);
    }

    void TriggerCleaveBuff(uint effIndex)
    {
        if (GetUnitTargetCountForEffect(effIndex) >= 2)
            GetCaster().CastSpell(GetCaster(), SpellIds.CrashLightningCleave, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
    }

    public override void Register()
    {
        OnEffectHit.Add(new(TriggerCleaveBuff, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 200076 - Deluge (attached to 77472 - Healing Wave, 8004 - Healing Surge and 1064 - Chain Heal
class spell_sha_deluge : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Riptide, SpellIds.DelugeAura)
            && ValidateSpellEffect((SpellIds.DelugeTalent, 0));
    }

    void CalculateHealingBonus(SpellEffectInfo spellEffectInfo, Unit victim, ref int healing, ref int flatMod, ref float pctMod)
    {
        AuraEffect deluge = GetCaster().GetAuraEffect(SpellIds.DelugeTalent, 0);
        if (deluge != null)
            if (victim.GetAura(SpellIds.Riptide, GetCaster().GetGUID()) != null || victim.GetAura(SpellIds.DelugeAura, GetCaster().GetGUID()) != null)
                MathFunctions.AddPct(ref pctMod, deluge.GetAmount());
    }

    public override void Register()
    {
        CalcHealing.Add(new(CalculateHealingBonus));
    }
}

[Script] // 200075 - Deluge (attached to 73920 - Healing Rain)
class spell_sha_deluge_healing_rain : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DelugeTalent, SpellIds.DelugeAura);
    }

    public override bool Load()
    {
        return GetUnitOwner().HasAura(SpellIds.DelugeTalent);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        GetCaster().CastSpell(GetHealingRainPosition(GetAura()), SpellIds.DelugeAura, TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
    }

    Position GetHealingRainPosition(Aura healingRain)
    {
        spell_sha_healing_rain_aura script = healingRain.GetScript<spell_sha_healing_rain_aura>();
        if (script != null)
            return script.GetPosition();

        return healingRain.GetUnitOwner().GetPosition();
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 1, AuraType.PeriodicDummy));
    }
}

[Script] // 378270 - Deeply Rooted Elements
class spell_sha_deeply_rooted_elements : AuraScript
{
    int _procAttempts;
    uint _triggeringSpellId;
    uint _triggeredSpellId;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LavaBurst, SpellIds.Stormstrike, SpellIds.Riptide,
                SpellIds.AscendanceElemental, SpellIds.AscendanceEnhancement, SpellIds.AscendanceRestoration)
            && ValidateSpellEffect((spellInfo.Id, 0))
            && spellInfo.GetEffect(0).IsAura();
    }

    public override bool Load()
    {
        return GetUnitOwner().IsPlayer();
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        if (procInfo.GetSpellInfo() == null)
            return false;

        if (procInfo.GetSpellInfo().Id != _triggeringSpellId)
            return false;

        return RandomHelper.randChance(_procAttempts++ - 2);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        _procAttempts = 0;

        Unit target = eventInfo.GetActor();

        int duration = GetEffect(0).GetAmount();
        Aura ascendanceAura = target.GetAura(_triggeredSpellId);
        if (ascendanceAura != null)
            duration += ascendanceAura.GetDuration();

        target.CastSpell(target, _triggeredSpellId, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress,
            TriggeringSpell = eventInfo.GetProcSpell(),
            TriggeringAura = aurEff,
            SpellValueOverrides = { new(SpellValueMod.Duration, duration) }
        });
    }

    public override void Register()
    {
        ChrSpecialization specialization = ChrSpecialization.None;
        Aura aura = GetAura();
        if (aura != null) // aura doesn't exist at startup validation
        {
            Player owner = aura.GetOwner()?.ToPlayer();
            if (owner != null)
                specialization = owner.GetPrimarySpecialization();
        }

        if (specialization == ChrSpecialization.None || specialization == ChrSpecialization.ShamanElemental)
        {
            DoCheckEffectProc.Add(new(CheckProc, 1, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
            _triggeringSpellId = SpellIds.LavaBurst;
            _triggeredSpellId = SpellIds.AscendanceElemental;
        }

        if (specialization == ChrSpecialization.None || specialization == ChrSpecialization.ShamanEnhancement)
        {
            DoCheckEffectProc.Add(new(CheckProc, 2, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 2, AuraType.Dummy));
            _triggeringSpellId = SpellIds.Stormstrike;
            _triggeredSpellId = SpellIds.AscendanceEnhancement;
        }

        if (specialization == ChrSpecialization.None || specialization == ChrSpecialization.ShamanRestoration)
        {
            DoCheckEffectProc.Add(new(CheckProc, 3, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 3, AuraType.Dummy));
            _triggeringSpellId = SpellIds.Riptide;
            _triggeredSpellId = SpellIds.AscendanceRestoration;
        }
    }
}

[Script] // 466772 - Doom Winds
class spell_sha_doom_winds : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DoomWindsDamage);
    }

    void PeriodicTick(AuraEffect aurEff)
    {
        GetTarget().CastSpell(GetTarget().GetPosition(), SpellIds.DoomWindsDamage, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 2, AuraType.PeriodicDummy));
    }
}

[Script] // 335902 - Doom Winds
class spell_sha_doom_winds_legendary : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DoomWindsLegendaryCooldown);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        if (GetTarget().HasAura(SpellIds.DoomWindsLegendaryCooldown))
            return false;

        SpellInfo spellInfo = procInfo.GetSpellInfo();
        if (spellInfo == null)
            return false;

        return spellInfo.HasLabel(SpellIds.LabelShamanWindfuryTotem);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 207778 - Downpour
class spell_sha_downpour : SpellScript
{
    int _healedTargets;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    void FilterTargets(List<WorldObject> targets)
    {
        SelectRandomInjuredTargets(targets, 6, true);
    }

    void CountEffectivelyHealedTarget()
    {
        // Cooldown increased for each target effectively healed
        if (GetHitHeal() != 0)
            ++_healedTargets;
    }

    void HandleCooldown()
    {
        TimeSpan cooldown = TimeSpan.FromSeconds(GetSpellInfo().RecoveryTime) + TimeSpan.FromSeconds(GetEffectInfo(1).CalcValue() * _healedTargets);
        GetCaster().GetSpellHistory().StartCooldown(GetSpellInfo(), 0, GetSpell(), false, cooldown);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
        AfterHit.Add(new(CountEffectivelyHealedTarget));
        AfterCast.Add(new(HandleCooldown));
    }
}

[Script] // 204288 - Earth Shield
class spell_sha_earth_shield : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EarthShieldHeal);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.GetDamageInfo() == null || !HasEffect(1) || eventInfo.GetDamageInfo().GetDamage() < GetTarget().CountPctFromMaxHealth(GetEffect(1).GetAmount()))
            return false;
        return true;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        GetTarget().CastSpell(GetTarget(), SpellIds.EarthShieldHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff,
            OriginalCaster = GetCasterGUID()
        });
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
    }
}

[Script] // 8042 - Earth Shock
class spell_sha_earth_shock : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((SpellIds.T29_2PElementalDamageBuff, 0));
    }

    void AddScriptedDamageMods()
    {
        AuraEffect t29 = GetCaster().GetAuraEffect(SpellIds.T29_2PElementalDamageBuff, 0);
        if (t29 != null)
        {
            SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), 100 + t29.GetAmount()));
            t29.GetBase().Remove();
        }
    }

    public override void Register()
    {
        OnHit.Add(new(AddScriptedDamageMods));
    }
}

[Script] // 170374 - Earthen Rage (Passive)
class spell_sha_earthen_rage_passive : AuraScript
{
    ObjectGuid _procTargetGuid;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EarthenRagePeriodic, SpellIds.EarthenRageDamage);
    }

    static bool CheckProc(ProcEventInfo procInfo)
    {
        return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().Id != SpellIds.EarthenRageDamage;
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        _procTargetGuid = eventInfo.GetProcTarget().GetGUID();
        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.EarthenRagePeriodic, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask
        });
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }

    public ObjectGuid GetProcTarGetGUID() { return _procTargetGuid; }
}

[Script] // 170377 - Earthen Rage (Proc Aura)
class spell_sha_earthen_rage_proc_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EarthenRagePassive, SpellIds.EarthenRageDamage);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        PreventDefaultAction();
        Aura aura = GetCaster().GetAura(SpellIds.EarthenRagePassive);
        if (aura != null)
        {
            spell_sha_earthen_rage_passive script = aura.GetScript<spell_sha_earthen_rage_passive>();
            if (script != null)
            {
                Unit procTarget = Global.ObjAccessor.GetUnit(GetCaster(), script.GetProcTarGetGUID());
                if (procTarget != null)
                {
                    GetTarget().CastSpell(procTarget, SpellIds.EarthenRageDamage, new CastSpellExtraArgs()
                    {
                        TriggerFlags = TriggerCastFlags.FullMask
                    });
                }
            }
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }
}

// 61882 - Earthquake
[Script] //  8382 - AreaTriggerId
class areatrigger_sha_earthquake(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    TimeSpan _refreshTimer = TimeSpan.Zero;
    TimeSpan _period = TimeSpan.FromSeconds(1);
    List<ObjectGuid> _stunnedUnits = new();
    float _damageMultiplier = 1.0f;

    public override void OnCreate(Spell creatingSpell)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            AuraEffect earthquake = caster.GetAuraEffect(SpellIds.Earthquake, 1);
            if (earthquake != null)
                _period = TimeSpan.FromSeconds(earthquake.GetPeriod());
        }

        if (creatingSpell != null)
        {
            if (creatingSpell.m_customArg is float damageMultiplier)
                _damageMultiplier = damageMultiplier;
        }
    }

    public override void OnUpdate(uint diff)
    {
        _refreshTimer -= TimeSpan.FromSeconds(diff);
        while (_refreshTimer <= TimeSpan.Zero)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
                caster.CastSpell(at.GetPosition(), SpellIds.EarthquakeTick, new CastSpellExtraArgs()
                {
                    TriggerFlags = TriggerCastFlags.FullMask,
                    OriginalCaster = at.GetGUID(),
                    SpellValueOverrides = { new(SpellValueMod.BasePoint0, (int)(caster.SpellBaseDamageBonusDone(SpellSchoolMask.Nature) * 0.213f * _damageMultiplier)) }
                });

            _refreshTimer += _period;
        }
    }

    // Each target can only be stunned once by each earthquake - keep track of who we already stunned
    public bool AddStunnedTarget(ObjectGuid guid)
    {
        if (_stunnedUnits.Contains(guid))
            return false;

        _stunnedUnits.Add(guid);
        return true;
    }
}

[Script] // 61882 - Earthquake
class spell_sha_earthquake : SpellScript
{
    static (uint, uint)[] DamageBuffs =
    {
        (SpellIds.EchoesOfGreatSunderingLegendary, 1),
        (SpellIds.EchoesOfGreatSunderingTalent, 0),
        (SpellIds.T29_2PElementalDamageBuff, 0)
    };

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect(DamageBuffs);
    }

    void SnapshotDamageMultiplier(uint effIndex)
    {
        float damageMultiplier = 1.0f;
        foreach (var (spellId, effect) in DamageBuffs)
        {
            AuraEffect buff = GetCaster().GetAuraEffect(spellId, effect);
            if (buff != null)
            {
                MathFunctions.AddPct(ref damageMultiplier, buff.GetAmount());
                buff.GetBase().Remove();
            }
        }

        if (damageMultiplier != 1.0f)
            GetSpell().m_customArg = damageMultiplier;
    }

    public override void Register()
    {
        OnEffectLaunch.Add(new(SnapshotDamageMultiplier, 2, SpellEffectName.CreateAreaTrigger));
    }
}

[Script] // 77478 - Earthquake tick
class spell_sha_earthquake_tick : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EarthquakeKnockingDown)
            && ValidateSpellEffect((spellInfo.Id, 1));
    }

    void HandleOnHit()
    {
        Unit target = GetHitUnit();
        if (target != null)
        {
            if (RandomHelper.randChance(GetEffectInfo(1).CalcValue()))
            {
                List<AreaTrigger> areaTriggers = GetCaster().GetAreaTriggers(SpellIds.Earthquake);
                var itr = areaTriggers.Find(at => at.GetGUID() == GetSpell().GetOriginalCasterGUID());
                if (itr != null)
                {
                    areatrigger_sha_earthquake eq = itr.GetAI<areatrigger_sha_earthquake>();
                    if (eq != null)
                        if (eq.AddStunnedTarget(target.GetGUID()))
                            GetCaster().CastSpell(target, SpellIds.EarthquakeKnockingDown, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
                }
            }
        }
    }

    public override void Register()
    {
        OnHit.Add(new(HandleOnHit));
    }
}

// 117014 - Elemental Blast
[Script] // 120588 - Elemental Blast Overload
class spell_sha_elemental_blast : SpellScript
{
    static uint[] BuffSpells = [SpellIds.ElementalBlastCrit, SpellIds.ElementalBlastHaste, SpellIds.ElementalBlastMastery];

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ElementalBlastCrit, SpellIds.ElementalBlastHaste, SpellIds.ElementalBlastMastery)
            && ValidateSpellEffect((SpellIds.T29_2PElementalDamageBuff, 0));
    }

    void TriggerBuff()
    {
        Unit caster = GetCaster();
        double total = 0.0;
        for (var i = 0; i < BuffSpells.Length; ++i)
            total += !caster.HasAura(BuffSpells[i]) ? 1.0f : 0.0f;

        uint spellId()
        {
            if (total > 0.0)
                return BuffSpells.SelectRandomElementByWeight(buffSpell => !caster.HasAura(buffSpell) ? 1.0f : 0.0f);

            // refresh random one if we have them all
            return BuffSpells.SelectRandom();
        }

        GetCaster().CastSpell(GetCaster(), spellId(), new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
    }

    void AddScriptedDamageMods()
    {
        AuraEffect t29 = GetCaster().GetAuraEffect(SpellIds.T29_2PElementalDamageBuff, 0);
        if (t29 != null)
        {
            SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), 100 + t29.GetAmount()));
            t29.GetBase().Remove();
        }
    }

    public override void Register()
    {
        AfterCast.Add(new(TriggerBuff));
        OnHit.Add(new(AddScriptedDamageMods));
    }
}

[Script] // 384355 - Elemental Weapons
class spell_sha_elemental_weapons : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ElementalWeaponsBuff);
    }

    public override bool Load()
    {
        return GetUnitOwner().IsPlayer();
    }

    void CheckEnchantments()
    {
        Player owner = GetUnitOwner().ToPlayer();
        int enchatmentCount = 0;
        if (owner.HasAura(SpellIds.FlametongueWeaponAura))
            ++enchatmentCount;
        if (owner.HasAura(SpellIds.WindfuryAura))
            ++enchatmentCount;

        int valuePerStack = GetEffect(0).GetAmount();
        Aura buff = owner.GetAura(SpellIds.ElementalWeaponsBuff);
        if (buff != null)
        {
            if (enchatmentCount != 0)
                foreach (AuraEffect aurEff in buff.GetAuraEffects())
                    aurEff.ChangeAmount(valuePerStack * enchatmentCount / 10);
            else
                buff.Remove();
        }
        else if (enchatmentCount != 0)
            owner.CastSpell(owner, SpellIds.ElementalWeaponsBuff, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                SpellValueOverrides =
                {
                    new( SpellValueMod.BasePoint0, valuePerStack * enchatmentCount / 10 ),
                    new( SpellValueMod.BasePoint1, valuePerStack * enchatmentCount / 10 )
                }
            });
    }

    void RemoveAllBuffs(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetUnitOwner().RemoveAurasDueToSpell(SpellIds.ElementalWeaponsBuff);
    }

    public override void Register()
    {
        OnHeartbeat.Add(new(CheckEnchantments));
        AfterEffectRemove.Add(new(RemoveAllBuffs, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

class FireNovaTargetCheck : ICheck<Unit>
{
    public float MaxSearchRange = 40.0f;
    public Unit Shaman;

    public bool Invoke(Unit candidate)
    {
        return candidate.IsWithinDist3d(Shaman, MaxSearchRange) && candidate.HasAura(SpellIds.FlameShock, Shaman.GetGUID());
    }
}

[Script] // 333974 - Fire Nova
class spell_sha_fire_nova : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FireNovaDamage);
    }

    void TriggerDamage(uint effIndex)
    {
        Unit shaman = GetCaster();
        List<Unit> targets = new();
        FireNovaTargetCheck check = new() { Shaman = shaman };
        UnitListSearcher searcher = new(shaman, targets, check);
        Cell.VisitAllObjects(shaman, searcher, check.MaxSearchRange);

        CastSpellExtraArgs args = new();
        args.SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        args.SetTriggeringSpell(GetSpell());

        foreach (Unit target in targets)
            shaman.CastSpell(target, SpellIds.FireNovaDamage, args);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(TriggerDamage, 0, SpellEffectName.Dummy));
    }
}

[Script] // 466620 - Flame Shock
class spell_sha_flame_shock_fire_nova_enabler : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlameShock, SpellIds.FireNovaEnabler);
    }

    void CheckFlameShocks(AuraEffect aurEff)
    {
        Unit shaman = GetTarget();
        FireNovaTargetCheck check = new() { Shaman = shaman };
        UnitSearcher searcher = new(shaman, check);
        Cell.VisitAllObjects(shaman, searcher, check.MaxSearchRange);
        if (searcher.GetResult() != null)
            shaman.CastSpell(shaman, SpellIds.FireNovaEnabler, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = aurEff
            });
        else
            shaman.RemoveAurasDueToSpell(SpellIds.FireNovaEnabler);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(CheckFlameShocks, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 318038 - Flametongue Weapon
class spell_sha_flametongue_weapon : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlametongueWeaponEnchant);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleEffectHitTarget(uint effIndex)
    {
        Player player = GetCaster().ToPlayer();
        byte slot = EquipmentSlot.MainHand;
        if (player.GetPrimarySpecialization() == ChrSpecialization.ShamanEnhancement)
            slot = EquipmentSlot.OffHand;

        Item targetItem = player.GetItemByPos(InventorySlots.Bag0, slot);
        if (targetItem == null || !targetItem.GetTemplate().IsWeapon())
            return;

        player.CastSpell(targetItem, SpellIds.FlametongueWeaponEnchant, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
    }
}

[Script] // 319778  - Flametongue - SpellIds.FlametongueWeaponAura
class spell_sha_flametongue_weapon_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlametongueAttack);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        eventInfo.GetActor().CastSpell(eventInfo.GetActionTarget(), SpellIds.FlametongueAttack, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 334196 - Hailstorm
class spell_sha_hailstorm : AuraScript
{
    void CalcCleaveMod(AuraEffect aurEff, ref SpellModifier spellMod)
    {
        if (spellMod == null)
        {
            SpellModifierByClassMask mod = new SpellModifierByClassMask(GetAura());
            mod.op = SpellModOp.ChainTargets;
            mod.type = SpellModType.Flat;
            mod.spellId = GetId();
            mod.mask = new FlagArray128(0x80000000, 0x00000000, 0x00000000, 0x00000000);

            spellMod = mod;
        }

        AuraEffect hailstormPassive = GetUnitOwner().GetAuraEffect(SpellIds.HailstormTalent, 0);
        if (hailstormPassive != null)
        {
            int targetCap = hailstormPassive.GetAmount() / aurEff.GetBaseAmount();
            ((SpellModifierByClassMask)spellMod).value = Math.Min(targetCap, GetStackAmount()) + 1;
        }
    }

    public override void Register()
    {
        DoEffectCalcSpellMod.Add(new(CalcCleaveMod, 1, AuraType.Dummy));
    }
}

[Script] // 73920 - Healing Rain (Aura)
class spell_sha_healing_rain_aura : AuraScript
{
    ObjectGuid _visualDummy;
    Position _dest;

    public void SetVisualDummy(TempSummon summon)
    {
        _visualDummy = summon.GetGUID();
        _dest = summon.GetPosition();
    }

    public Position GetPosition() { return _dest; }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        GetTarget().CastSpell(_dest, SpellIds.HealingRainHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff
        });
    }

    void HandleEffecRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Creature summon = ObjectAccessor.GetCreature(GetTarget(), _visualDummy);
        if (summon != null)
            summon.DespawnOrUnsummon();
    }

    public override void Register()
    {
        OnEffectRemove.Add(new(HandleEffecRemoved, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 1, AuraType.PeriodicDummy));
    }
}

[Script] // 73920 - Healing Rain
class spell_sha_healing_rain : SpellScript
{
    const uint NpcHealingRainInvisibleStalker = 73400;

    void InitializeVisualStalker()
    {
        Aura aura = GetHitAura();
        if (aura != null)
        {
            WorldLocation dest = GetExplTargetDest();
            if (dest != null)
            {
                TimeSpan duration = TimeSpan.FromSeconds(GetSpellInfo().CalcDuration(GetOriginalCaster()));
                TempSummon summon = GetCaster().GetMap().SummonCreature(NpcHealingRainInvisibleStalker, dest, null, duration, GetOriginalCaster());
                if (summon == null)
                    return;

                summon.CastSpell(summon, SpellIds.HealingRainVisual, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });

                spell_sha_healing_rain_aura script = aura.GetScript<spell_sha_healing_rain_aura>();
                if (script != null)
                    script.SetVisualDummy(summon);
            }
        }
    }

    public override void Register()
    {
        AfterHit.Add(new(InitializeVisualStalker));
    }
}

[Script] // 73921 - Healing Rain
class spell_sha_healing_rain_target_limit : SpellScript
{
    void SelectTargets(List<WorldObject> targets)
    {
        SelectRandomInjuredTargets(targets, 6, true);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(SelectTargets, 0, Targets.UnitDestAreaAlly));
    }
}

[Script] // 52042 - Healing Stream Totem
class spell_sha_healing_stream_totem_heal : SpellScript
{
    void SelectTargets(List<WorldObject> targets)
    {
        SelectRandomInjuredTargets(targets, 1, true);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(SelectTargets, 0, Targets.UnitDestAreaAlly));
    }
}

[Script] // 201900 - Hot Hand
class spell_sha_hot_hand : AuraScript
{
    static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetActor().HasAura(SpellIds.FlametongueWeaponAura);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
    }
}

[Script] // 342240 - Ice Strike
class spell_sha_ice_strike : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return spell_sha_maelstrom_weapon_base.Validate();
    }

    void EnergizeMaelstrom(uint effIndex)
    {
        spell_sha_maelstrom_weapon_base.GenerateMaelstromWeapon(GetCaster(), GetEffectValue());
    }

    public override void Register()
    {
        OnEffectHit.Add(new(EnergizeMaelstrom, 3, SpellEffectName.Dummy));
    }
}

[Script] // 466467 - Ice Strike
class spell_sha_ice_strike_proc : AuraScript
{
    int _attemptCount;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.IceStrikeOverrideAura);
    }

    public override void Register() { }

    public void AttemptProc()
    {
        if (!RandomHelper.randChance(++_attemptCount * 7))
            return;

        _attemptCount = 0;
        Unit shaman = GetUnitOwner();
        shaman.CastSpell(shaman, SpellIds.IceStrikeOverrideAura, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.DontReportCastError
        });
    }
}

[Script] // 210714 - Icefury
class spell_sha_icefury : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FrostShockEnergize);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(caster, SpellIds.FrostShockEnergize, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.IgnoreCastInProgress });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 1, AuraType.AddPctModifier));
    }
}

[Script] // 23551 - Lightning Shield T2 Bonus
class spell_sha_item_lightning_shield : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ItemLightningShield);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.ItemLightningShield, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 23552 - Lightning Shield T2 Bonus
class spell_sha_item_lightning_shield_trigger : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ItemLightningShieldDamage);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), SpellIds.ItemLightningShieldDamage, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 23572 - Mana Surge
class spell_sha_item_mana_surge : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ItemManaSurge);
    }

    static bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcSpell() != null;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var manaCost = eventInfo.GetProcSpell().GetPowerTypeCostAmount(PowerType.Mana);
        if (manaCost.HasValue)
        {
            int mana = MathFunctions.CalculatePct(manaCost.Value, 35);
            if (mana > 0)
            {
                GetTarget().CastSpell(GetTarget(), SpellIds.ItemManaSurge, new CastSpellExtraArgs()
                {
                    TriggerFlags = TriggerCastFlags.FullMask,
                    TriggeringAura = aurEff,
                    SpellValueOverrides = { new(SpellValueMod.BasePoint0, mana) }
                });
            }
        }
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 40463 - Shaman Tier 6 Trinket
class spell_sha_item_t6_trinket : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EnergySurge, SpellIds.PowerSurge);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        SpellInfo spellInfo = eventInfo.GetSpellInfo();
        if (spellInfo == null)
            return;

        uint spellId;
        int chance;

        // Lesser Healing Wave
        if ((spellInfo.SpellFamilyFlags[0] & 0x00000080) != 0)
        {
            spellId = SpellIds.EnergySurge;
            chance = 10;
        }
        // Lightning Bolt
        else if ((spellInfo.SpellFamilyFlags[0] & 0x00000001) != 0)
        {
            spellId = SpellIds.EnergySurge;
            chance = 15;
        }
        // Stormstrike
        else if ((spellInfo.SpellFamilyFlags[1] & 0x00000010) != 0)
        {
            spellId = SpellIds.PowerSurge;
            chance = 50;
        }
        else
            return;

        if (RandomHelper.randChance(chance))
            eventInfo.GetActor().CastSpell(null, spellId, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 70811 - Item - Shaman T10 Elemental 2P Bonus
class spell_sha_item_t10_elemental_2p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ElementalMastery);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Player target = GetTarget().ToPlayer();
        if (target != null)
            target.GetSpellHistory().ModifyCooldown(SpellIds.ElementalMastery, TimeSpan.FromSeconds(-aurEff.GetAmount()));
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 189063 - Lightning Vortex (proc 185881 Item - Shaman T18 Elemental 4P Bonus)
class spell_sha_item_t18_elemental_4p_bonus : AuraScript
{
    void DiminishHaste(AuraEffect aurEff)
    {
        PreventDefaultAction();
        AuraEffect hasteBuff = GetEffect(0);
        if (hasteBuff != null)
            hasteBuff.ChangeAmount(hasteBuff.GetAmount() - aurEff.GetAmount());
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(DiminishHaste, 1, AuraType.PeriodicDummy));
    }
}

[Script] // 51505 - Lava burst
class spell_sha_lava_burst : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PathOfFlamesTalent, SpellIds.PathOfFlamesSpread, SpellIds.LavaSurge);
    }

    void HandleScript(uint effIndex)
    {
        Unit caster = GetCaster();
        if (caster != null)
            if (caster.HasAura(SpellIds.PathOfFlamesTalent))
                caster.CastSpell(GetHitUnit(), SpellIds.PathOfFlamesSpread, new CastSpellExtraArgs()
                {
                    TriggerFlags = TriggerCastFlags.FullMask,
                    TriggeringSpell = GetSpell()
                });
    }

    void EnsureLavaSurgeCanBeImmediatelyConsumed()
    {
        Unit caster = GetCaster();

        Aura lavaSurge = caster.GetAura(SpellIds.LavaSurge);
        if (lavaSurge != null)
        {
            if (!GetSpell().m_appliedMods.Contains(lavaSurge))
            {
                uint chargeCategoryId = GetSpellInfo().ChargeCategoryId;

                // Ensure we have at least 1 usable charge after cast to allow next cast immediately
                if (!caster.GetSpellHistory().HasCharge(chargeCategoryId))
                    caster.GetSpellHistory().RestoreCharge(chargeCategoryId);
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.TriggerMissile));
        AfterCast.Add(new(EnsureLavaSurgeCanBeImmediatelyConsumed));
    }
}

// 285452 - Lava Burst damage
[Script] // 285466 - Lava Burst Overload damage
class spell_sha_lava_crit_chance : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlameShock);
    }

    void CalcCritChance(Unit victim, ref float chance)
    {
        Unit caster = GetCaster();

        if (caster == null || victim == null)
            return;

        if (victim.HasAura(SpellIds.FlameShock, caster.GetGUID()))
            if (victim.GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance) > -100)
                chance = 100.0f;
    }

    public override void Register()
    {
        OnCalcCritChance.Add(new(CalcCritChance));
    }
}

[Script] // 60103 - Lava Lash
class spell_sha_lava_lash : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1))
            && ValidateSpellInfo(SpellIds.FlametongueWeaponAura);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void AddBonusFlametongueDamage(SpellEffectInfo effectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        Player caster = GetCaster().ToPlayer();
        ObjectGuid offhandItemGuid = caster.GetWeaponForAttack(GetSpellInfo().GetAttackType()).GetGUID();
        if (GetCaster().HasAura(SpellIds.FlametongueWeaponAura, ObjectGuid.Empty, offhandItemGuid))
            MathFunctions.AddPct(ref pctMod, GetSpell().CalculateDamage(GetEffectInfo(1), victim));
    }

    public override void Register()
    {
        CalcDamage.Add(new(AddBonusFlametongueDamage));
    }
}

[Script] // 77756 - Lava Surge
class spell_sha_lava_surge : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LavaSurge, SpellIds.IgneousPotential);
    }

    bool CheckProcChance(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        int procChance = aurEff.GetAmount();
        AuraEffect igneousPotential = GetTarget().GetAuraEffect(SpellIds.IgneousPotential, 0);
        if (igneousPotential != null)
            procChance += igneousPotential.GetAmount();

        return RandomHelper.randChance(procChance);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), SpellIds.LavaSurge, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProcChance, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 77762 - Lava Surge
class spell_sha_lava_surge_proc : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LavaBurst);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void ResetCooldown()
    {
        GetCaster().GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(SpellIds.LavaBurst, GetCastDifficulty()).ChargeCategoryId);
    }

    public override void Register()
    {
        AfterHit.Add(new(ResetCooldown));
    }
}

[Script] // 188196 - Lightning Bolt
class spell_sha_lightning_bolt : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LightningBoltEnergize)
            && ValidateSpellEffect((SpellIds.MaelstromController, 0));
    }

    void HandleScript(uint effIndex)
    {
        AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 0);
        if (energizeAmount != null)
            GetCaster().CastSpell(GetCaster(), SpellIds.LightningBoltEnergize, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = energizeAmount,
                SpellValueOverrides = { new(SpellValueMod.BasePoint0, energizeAmount.GetAmount()) }
            });
    }

    public override void Register()
    {
        OnEffectLaunch.Add(new(HandleScript, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 45284 - Lightning Bolt Overload
class spell_sha_lightning_bolt_overload : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LightningBoltOverloadEnergize)
            && ValidateSpellEffect((SpellIds.MaelstromController, 1));
    }

    void HandleScript(uint effIndex)
    {
        AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 1);
        if (energizeAmount != null)
            GetCaster().CastSpell(GetCaster(), SpellIds.LightningBoltOverloadEnergize, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringAura = energizeAmount,
                SpellValueOverrides = { new(SpellValueMod.BasePoint0, energizeAmount.GetAmount()) }
            });
    }

    public override void Register()
    {
        OnEffectLaunch.Add(new(HandleScript, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 192223 - Liquid Magma Totem (erupting hit spell)
class spell_sha_liquid_magma_totem : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LiquidMagmaHit);
    }

    void HandleEffectHitTarget(uint effIndex)
    {
        Unit hitUnit = GetHitUnit();
        if (hitUnit != null)
            GetCaster().CastSpell(hitUnit, SpellIds.LiquidMagmaHit, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
    }

    void HandleTargetSelect(List<WorldObject> targets)
    {
        // choose one random target from targets
        if (targets.Count > 1)
        {
            WorldObject selected = targets.SelectRandom();
            targets.Clear();
            targets.Add(selected);
        }
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(HandleTargetSelect, 0, Targets.UnitDestAreaEnemy));
        OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
    }
}

[Script] // 187880 - Maelstrom Weapon
class spell_sha_maelstrom_weapon : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return spell_sha_maelstrom_weapon_base.Validate();
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procEvent)
    {
        spell_sha_maelstrom_weapon_base.GenerateMaelstromWeapon(GetTarget(), 1);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 344179 - Maelstrom Weapon
class spell_sha_maelstrom_weapon_proc : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return spell_sha_maelstrom_weapon_base.Validate();
    }

    bool CheckProc(ProcEventInfo procEvent)
    {
        Spell procSpell = procEvent.GetProcSpell();
        if (procSpell == null)
            return false;

        Aura maelstromSpellMod = GetTarget().GetAura(SpellIds.MaelstromWeaponModAura);
        if (maelstromSpellMod == null)
            return false;

        return procSpell.m_appliedMods.Contains(maelstromSpellMod);
    }

    void RemoveMaelstromAuras(ProcEventInfo procEvent)
    {
        Unit shaman = GetTarget();
        int stacksToConsume = 5;
        if (shaman.HasAura(SpellIds.OverflowingMaelstromTalent))
            stacksToConsume = 10;

        spell_sha_maelstrom_weapon_base.ConsumeMaelstromWeapon(shaman, GetAura(), stacksToConsume, procEvent.GetProcSpell());
    }

    void ExpireMaelstromAuras(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit shaman = GetTarget();
        AuraRemoveMode removeMode = GetTargetApplication().GetRemoveMode();
        shaman.RemoveAurasDueToSpell(SpellIds.OverflowingMaelstromAura, ObjectGuid.Empty, 0, removeMode);
        shaman.RemoveAurasDueToSpell(SpellIds.MaelstromWeaponModAura, ObjectGuid.Empty, 0, removeMode);
        shaman.RemoveAurasDueToSpell(SpellIds.MaelstromWeaponOverlay, ObjectGuid.Empty, 0, removeMode);
        shaman.RemoveAurasDueToSpell(SpellIds.MaelstromWeaponOverlayHeals, ObjectGuid.Empty, 0, removeMode);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnProc.Add(new(RemoveMaelstromAuras));
        AfterEffectRemove.Add(new(ExpireMaelstromAuras, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 168534 - Mastery: Elemental Overload (passive)
class spell_sha_mastery_elemental_overload : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo
        (
            SpellIds.LightningBolt,
            SpellIds.LightningBoltOverload,
            SpellIds.ElementalBlast,
            SpellIds.ElementalBlastOverload,
            SpellIds.Icefury,
            SpellIds.IcefuryOverload,
            SpellIds.LavaBurst,
            SpellIds.LavaBurstOverload,
            SpellIds.ChainLightning,
            SpellIds.ChainLightningOverload,
            SpellIds.Stormkeeper
       );
    }

    static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        SpellInfo spellInfo = eventInfo.GetSpellInfo();
        if (spellInfo == null || eventInfo.GetProcSpell() == null)
            return false;

        if (GetTriggeredSpellId(spellInfo.Id) == null)
            return false;

        float chance = aurEff.GetAmount();   // Mastery % amount

        if (spellInfo.Id == SpellIds.ChainLightning)
            chance /= 3.0f;

        Aura stormkeeper = eventInfo.GetActor().GetAura(SpellIds.Stormkeeper);
        if (stormkeeper != null && eventInfo.GetProcSpell().m_appliedMods.Contains(stormkeeper))
            chance = 100.0f;

        return RandomHelper.randChance(chance);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        PreventDefaultAction();

        Unit caster = procInfo.GetActor();

        var targets = new CastSpellTargetArg(procInfo.GetProcTarget());
        var overloadSpellId = GetTriggeredSpellId(procInfo.GetSpellInfo().Id);
        var originalCastId = procInfo.GetProcSpell().m_castId;
        caster.m_Events.AddEventAtOffset(() =>
        {
            if (targets.Targets == null)
                return;

            targets.Targets.Update(caster);

            CastSpellExtraArgs args = new();
            args.OriginalCastId = originalCastId;
            caster.CastSpell(targets, overloadSpellId, args);
        }, TimeSpan.FromSeconds(400));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }

    static uint GetTriggeredSpellId(uint triggeringSpellId)
    {
        switch (triggeringSpellId)
        {
            case SpellIds.LightningBolt: return SpellIds.LightningBoltOverload;
            case SpellIds.ElementalBlast: return SpellIds.ElementalBlastOverload;
            case SpellIds.Icefury: return SpellIds.IcefuryOverload;
            case SpellIds.LavaBurst: return SpellIds.LavaBurstOverload;
            case SpellIds.ChainLightning: return SpellIds.ChainLightningOverload;
            default:
                break;
        }
        return 0;
    }
}

// 45284 - Lightning Bolt Overload
// 45297 - Chain Lightning Overload
// 114738 - Lava Beam Overload
// 120588 - Elemental Blast Overload
// 219271 - Icefury Overload
[Script] // 285466 - Lava Burst Overload
class spell_sha_mastery_elemental_overload_proc : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MasteryElementalOverload);
    }

    void ApplyDamageModifier(uint effIndex)
    {
        AuraEffect elementalOverload = GetCaster().GetAuraEffect(SpellIds.MasteryElementalOverload, 1);
        if (elementalOverload != null)
            SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), elementalOverload.GetAmount()));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(ApplyDamageModifier, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 334033 - Molten Assault (60103 - Lava Lash)
class spell_sha_molten_assault : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlameShock);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.MoltenAssault);
    }

    void TriggerFlameShocks(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit lavaLashTarget = GetHitUnit();
        if (!lavaLashTarget.HasAura(SpellIds.FlameShock, caster.GetGUID()))
            return;

        float range = 10.0f;
        List<WorldObject> targets = new();
        WorldObjectSpellAreaTargetCheck check = new(range, lavaLashTarget, caster, caster, Global.SpellMgr.GetSpellInfo(SpellIds.FlameShock, Difficulty.None),
         SpellTargetCheckTypes.Enemy, null, SpellTargetObjectTypes.Unit, WorldObjectSpellAreaTargetSearchReason.Area);
        WorldObjectListSearcher searcher = new(caster, targets, check, GridMapTypeMask.Creature | GridMapTypeMask.Player);
        Cell.VisitAllObjects(lavaLashTarget, searcher, range + SharedConst.ExtraCellSearchRadius);

        var predicate = new UnitAuraCheck(true, SpellIds.FlameShock, GetCaster().GetGUID()).Invoke;
        targets.PartitionInPlace(predicate);

        var withoutFlameShockIndex = targets.FindIndex(0, x => !predicate(x));
        if (withoutFlameShockIndex != -1)
            targets.RandomShuffle(withoutFlameShockIndex, targets.Count - withoutFlameShockIndex);

        CastSpellExtraArgs args = new();
        args.SetTriggerFlags(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnorePowerCost | TriggerCastFlags.IgnoreCastInProgress);
        args.SetTriggeringSpell(GetSpell());

        // targets that already have flame shock are first in the list (and need to refresh it)
        for (var i = 0; i < Math.Min(targets.Count, GetEffectValue() + 1); ++i)
            caster.CastSpell(targets[i], SpellIds.FlameShock, args);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(TriggerFlameShocks, 2, SpellEffectName.Dummy));
    }
}

[Script] // 469344 Molten Thunder
class spell_sha_molten_thunder : AuraScript
{
    public int ProcCount = 0;

    public override void Register() { }
}

[Script]
class spell_sha_molten_thunder_sundering : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MoltenThunderTalent, SpellIds.MoltenThunderProc);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.MoltenThunderTalent);
    }

    void RemoveIncapacitateEffect(List<WorldObject> targets)
    {
        targets.Clear();
    }

    void RollReset()
    {
        Unit shaman = GetCaster();
        Aura talent = shaman.GetAura(SpellIds.MoltenThunderTalent);
        if (talent == null)
            return;

        AuraEffect chanceBaseEffect = talent.GetEffect(1);
        AuraEffect chancePerTargetEffect = talent.GetEffect(2);
        AuraEffect targetLimitEffect = talent.GetEffect(3);
        if (chanceBaseEffect == null || chancePerTargetEffect == null || targetLimitEffect == null)
            return;

        spell_sha_molten_thunder counterScript = talent.GetScript<spell_sha_molten_thunder>();
        if (counterScript == null)
            return;

        int procChance = chanceBaseEffect.GetAmount();
        procChance += (int)Math.Min(targetLimitEffect.GetAmount(), GetUnitTargetCountForEffect(0)) * chancePerTargetEffect.GetAmount();
        procChance >>= counterScript.ProcCount; // Each consecutive reset reduces these chances by half
        if (RandomHelper.randChance(procChance))
        {
            shaman.CastSpell(shaman, SpellIds.MoltenThunderProc, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                TriggeringSpell = GetSpell()
            });
            shaman.GetSpellHistory().ResetCooldown(GetSpellInfo().Id, true);
            ++counterScript.ProcCount;
        }
        else
            counterScript.ProcCount = 0;
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(RemoveIncapacitateEffect, 3, Targets.UnitRectCasterEnemy));
        AfterCast.Add(new(RollReset));
    }
}

[Script] // 30884 - Nature's Guardian
class spell_sha_natures_guardian : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NaturesGuardianCooldown);
    }

    static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetActionTarget().HealthBelowPct(aurEff.GetAmount())
            && !eventInfo.GetActionTarget().HasAura(SpellIds.NaturesGuardianCooldown);
    }

    static void StartCooldown(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit shaman = eventInfo.GetActionTarget();
        shaman.CastSpell(shaman, SpellIds.NaturesGuardianCooldown, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(StartCooldown, 0, AuraType.Dummy));
    }
}

[Script] // 210621 - Path of Flames Spread
class spell_sha_path_of_flames_spread : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlameShock);
    }

    void FilterTargets(List<WorldObject> targets)
    {
        targets.Remove(GetExplTargetUnit());
        targets.RandomResize(new UnitAuraCheck(false, SpellIds.FlameShock, GetCaster().GetGUID()).Invoke, 1);
    }

    void HandleScript(uint effIndex)
    {
        Unit mainTarget = GetExplTargetUnit();
        if (mainTarget != null)
        {
            Aura flameShock = mainTarget.GetAura(SpellIds.FlameShock, GetCaster().GetGUID());
            if (flameShock != null)
            {
                Aura newAura = GetCaster().AddAura(SpellIds.FlameShock, GetHitUnit());
                if (newAura != null)
                {
                    newAura.SetDuration(flameShock.GetDuration());
                    newAura.SetMaxDuration(flameShock.GetDuration());
                }
            }
        }
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitDestAreaEnemy));
        OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.Dummy));
    }
}

[Script] // 375982 - Primordial Wave
class spell_sha_primordial_wave : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlameShock, SpellIds.PrimordialWaveDamage);
    }

    void TriggerDamage(uint effIndex)
    {
        Unit shaman = GetCaster();
        List<Unit> targets = new();
        FireNovaTargetCheck check = new() { MaxSearchRange = GetSpell().GetMinMaxRange(false).maxRange, Shaman = shaman };
        UnitListSearcher searcher = new(shaman, targets, check);
        Cell.VisitAllObjects(shaman, searcher, check.MaxSearchRange);

        CastSpellExtraArgs args = new();
        args.SetTriggerFlags(TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError);
        args.SetTriggeringSpell(GetSpell());

        foreach (Unit target in targets)
            shaman.CastSpell(target, SpellIds.PrimordialWaveDamage, args);
    }

    void PreventLavaSurge(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
    }

    void EnergizeMaelstrom(uint effIndex)
    {
        spell_sha_maelstrom_weapon_base.GenerateMaelstromWeapon(GetCaster(), GetEffectValue());
    }

    public override void Register()
    {
        ChrSpecialization specialization = ChrSpecialization.None;
        Spell spell = GetSpell();
        if (spell != null) // spell doesn't exist at startup validation
        {
            Player caster = spell.GetCaster()?.ToPlayer();
            if (caster != null)
                specialization = caster.GetPrimarySpecialization();
        }

        OnEffectHitTarget.Add(new(TriggerDamage, 0, SpellEffectName.Dummy));

        if (specialization != ChrSpecialization.ShamanElemental)
            OnEffectLaunch.Add(new(PreventLavaSurge, 5, SpellEffectName.TriggerSpell));

        if (specialization == ChrSpecialization.None || specialization == ChrSpecialization.ShamanEnhancement)
            OnEffectHitTarget.Add(new(EnergizeMaelstrom, 4, SpellEffectName.Dummy));
    }
}

// 114083 - Restorative Mists
[Script] // 294020 - Restorative Mists
class spell_sha_restorative_mists : SpellScript
{
    void HandleHeal(uint effIndex)
    {
        SetHitHeal((int)(GetHitHeal() / GetUnitTargetCountForEffect(effIndex)));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHeal, 0, SpellEffectName.Heal));
    }
}

// 2645 - Ghost Wolf
// 260878 - Spirit Wolf
class spell_sha_spirit_wolf : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GhostWolf, SpellIds.SpiritWolfTalent, SpellIds.SpiritWolfPeriodic, SpellIds.SpiritWolfAura);
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        if (target.HasAura(SpellIds.SpiritWolfTalent) && target.HasAura(SpellIds.GhostWolf))
            target.CastSpell(target, SpellIds.SpiritWolfPeriodic, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.FullMask,
                TriggeringAura = aurEff
            });
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.SpiritWolfPeriodic);
        GetTarget().RemoveAurasDueToSpell(SpellIds.SpiritWolfAura);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnApply, 0, AuraType.Any, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Any, AuraEffectHandleModes.Real));
    }
}

[Script] // 319930 - Stormblast
class spell_sha_stormblast : AuraScript
{
    public ObjectGuid AllowedOriginalCastId;

    public override void Register() { }
}

[Script] // 470466 - Stormblast (Stormstrike and Winstrike damaging spells)
class spell_sha_stormblast_damage : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((SpellIds.StormblastTalent, 0), (SpellIds.EnhancedElements, 0));
    }

    public override bool Load()
    {
        Aura stormblast = GetCaster().GetAura(SpellIds.StormblastTalent);
        if (stormblast != null)
        {
            spell_sha_stormblast script = stormblast.GetScript<spell_sha_stormblast>();
            if (script != null)
                return script.AllowedOriginalCastId == GetSpell().m_originalCastId;
        }

        return false;
    }

    void TriggerDamage()
    {
        AuraEffect stormblast = GetCaster().GetAuraEffect(SpellIds.StormblastTalent, 0);
        if (stormblast != null)
        {
            int damage = MathFunctions.CalculatePct(GetHitDamage(), stormblast.GetAmount());

            // Not part of SpellFamilyFlags for mastery effect but known to be affected by it
            AuraEffect mastery = GetCaster().GetAuraEffect(SpellIds.EnhancedElements, 0);
            if (mastery != null)
                MathFunctions.AddPct(ref damage, mastery.GetAmount());

            GetCaster().CastSpell(GetHitUnit(), SpellIds.StormblastDamage, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                SpellValueOverrides = { new(SpellValueMod.BasePoint0, damage) }
            });
        }
    }

    public override void Register()
    {
        AfterHit.Add(new(TriggerDamage));
    }
}

[Script] // 470466 - Stormblast (17364 - Stormstrike, 115356 - Windstrike)
class spell_sha_stormblast_proc : SpellScript
{
    public override bool Load()
    {
        Unit caster = GetCaster();
        return caster.HasAura(SpellIds.StormblastTalent)
            && caster.HasAura(SpellIds.StormblastProc);
    }

    // Store allowed CastId in passive aura because damaging spells are delayed (and delayed further if Stormflurry is triggered)
    void SaveCastId()
    {
        Unit caster = GetCaster();
        Aura stormblast = caster.GetAura(SpellIds.StormblastTalent);
        if (stormblast != null)
        {
            spell_sha_stormblast script = stormblast.GetScript<spell_sha_stormblast>();
            if (script != null)
                script.AllowedOriginalCastId = GetSpell().m_castId;
        }

        caster.RemoveAuraFromStack(SpellIds.StormblastProc);
    }

    public override void Register()
    {
        OnCast.Add(new(SaveCastId));
    }
}

class StormflurryEvent : BasicEvent
{
    Unit _caster;
    CastSpellTargetArg _target;
    ObjectGuid _originalCastId;
    int _damagePercent;
    uint _mainHandDamageSpellId;
    uint _offHandDamageSpellId;
    int _procChance;

    public StormflurryEvent(Unit caster, Unit target, ObjectGuid originalCastId, int damagePercent, uint mainHandDamageSpellId, uint offHandDamageSpellId, int procChance)
    {
        _caster = caster;
        _target = target;
        _originalCastId = originalCastId;
        _damagePercent = damagePercent;
        _mainHandDamageSpellId = mainHandDamageSpellId;
        _offHandDamageSpellId = offHandDamageSpellId;
        _procChance = procChance;
    }

    public override bool Execute(ulong time, uint diff)
    {
        if (_target.Targets == null)
            return true;

        _target.Targets.Update(_caster);

        CastSpellExtraArgs args = new();
        args.TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError;
        args.OriginalCastId = _originalCastId;
        args.CustomArg = new Data() { DamagePercent = _damagePercent };

        _caster.CastSpell(_target, _mainHandDamageSpellId, args);
        _caster.CastSpell(_target, _offHandDamageSpellId, args);

        if (!RandomHelper.randChance(_procChance))
            return true;

        _caster.m_Events.AddEvent(this, TimeSpan.FromSeconds(time) + TimeSpan.FromSeconds(200));
        return false;
    }

    public class Data
    {
        public int DamagePercent;
    }
}

// 198367 Stormflurry
// 344357 Stormflurry
[Script("spell_sha_artifact_stormflurry_stormstrike", SpellIds.StormflurryArtifact, SpellIds.StormstrikeDamageMainHand, SpellIds.StormstrikeDamageOffHand)]
[Script("spell_sha_artifact_stormflurry_windstrike", SpellIds.StormflurryArtifact, SpellIds.WindstrikeDamageMainHand, SpellIds.WindstrikeDamageOffHand)]
[Script("spell_sha_stormflurry_stormstrike", SpellIds.Stormflurry, SpellIds.StormstrikeDamageMainHand, SpellIds.StormstrikeDamageOffHand)]
[Script("spell_sha_stormflurry_windstrike", SpellIds.Stormflurry, SpellIds.WindstrikeDamageMainHand, SpellIds.WindstrikeDamageOffHand)]
class spell_sha_stormflurry : SpellScript
{
    uint _stormflurrySpellId;
    uint _mainHandDamageSpellId;
    uint _offHandDamageSpellId;

    public spell_sha_stormflurry(uint stormflurrySpellId, uint mainHandDamageSpellId, uint offHandDamageSpellId)
    {
        _stormflurrySpellId = stormflurrySpellId;
        _mainHandDamageSpellId = mainHandDamageSpellId;
        _offHandDamageSpellId = offHandDamageSpellId;
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(_stormflurrySpellId, _mainHandDamageSpellId, _offHandDamageSpellId)
            && ValidateSpellEffect((spellInfo.Id, 1))
            && spellInfo.GetEffect(0).IsEffect(SpellEffectName.TriggerSpell)
            && spellInfo.GetEffect(1).IsEffect(SpellEffectName.TriggerSpell);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(_stormflurrySpellId);
    }

    void HandleProc(uint effIndex)
    {
        Unit caster = GetCaster();
        Aura stormflurry = caster.GetAura(_stormflurrySpellId);
        if (stormflurry == null)
            return;

        AuraEffect chanceEffect = stormflurry.GetEffect(0);
        AuraEffect damageEffect = stormflurry.GetEffect(1);
        if (chanceEffect == null || damageEffect == null)
            return;

        int procChance = chanceEffect.GetAmount();
        if (!RandomHelper.randChance(procChance))
            return;

        caster.m_Events.AddEventAtOffset(new StormflurryEvent(caster, GetHitUnit(), GetSpell().m_castId, damageEffect.GetAmount(),
            _mainHandDamageSpellId, _offHandDamageSpellId, procChance), TimeSpan.FromSeconds(200));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleProc, 1, SpellEffectName.TriggerSpell));
    }
}

// 32175 - Stormstrike
[Script] // 32176 - Stormstrike Off-Hand
class spell_sha_stormflurry_damage : SpellScript
{
    public override bool Load()
    {
        return GetSpell().m_customArg.GetType() == typeof(StormflurryEvent.Data);
    }

    void ApplyModifier(SpellEffectInfo effectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        MathFunctions.ApplyPct(ref pctMod, ((StormflurryEvent.Data)GetSpell().m_customArg).DamagePercent);
    }

    public override void Register()
    {
        CalcDamage.Add(new(ApplyModifier));
    }
}

[Script] // 201845 - Stormsurge
class spell_sha_stormsurge : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.StormsurgeProc);
    }

    static void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.StormsurgeProc, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 187881 - Maelstrom Weapon
class spell_sha_stormweaver : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.StormweaverPvpTalent);
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.StormweaverPvpTalent);
    }

    void PreventAffectingHealingSpells(ref WorldObject target)
    {
        target = null;
    }

    public override void Register()
    {
        OnObjectTargetSelect.Add(new(PreventAffectingHealingSpells, 2, Targets.UnitCaster));
        OnObjectTargetSelect.Add(new(PreventAffectingHealingSpells, 4, Targets.UnitCaster));
    }
}

[Script] // 384359 - Swirling Maelstrom
class spell_sha_swirling_maelstrom : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1))
            && spell_sha_maelstrom_weapon_base.Validate();
    }

    bool CheckHailstormProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Shaman, new FlagArray128(0x80000000, 0x0, 0x0, 0x0))) // Frost Shock
        {
            Aura hailstorm = eventInfo.GetActor().GetAura(SpellIds.HailstormBuff);
            if (hailstorm == null || hailstorm.GetStackAmount() < GetEffect(1).GetAmount())
                return false;

            if (!eventInfo.GetProcSpell().m_appliedMods.Contains(hailstorm))
                return false;
        }

        return true;
    }

    void EnergizeMaelstrom(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        spell_sha_maelstrom_weapon_base.GenerateMaelstromWeapon(GetTarget(), aurEff.GetAmount());
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckHailstormProc));
        OnEffectProc.Add(new(EnergizeMaelstrom, 0, AuraType.Dummy));
    }
}

[Script] // 384444 - Thorim's Invocation
class spell_sha_thorims_invocation : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LightningBolt, SpellIds.ChainLightning);
    }

    public override void Register()
    {
    }

    public
        uint SpellIdToTrigger = SpellIds.LightningBolt;
}

// 188196 - Lightning Bolt
// 188443 - Chain Lightning
[Script] // 452201 - Tempest
class spell_sha_thorims_invocation_primer : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ThorimsInvocation, SpellIds.LightningBolt, SpellIds.ChainLightning)
            && ValidateSpellEffect((spellInfo.Id, 0))
            && spellInfo.GetEffect(0).IsEffect(SpellEffectName.SchoolDamage);
    }

    void UpdateThorimsInvocationSpell()
    {
        Aura thorimsInvocation = GetCaster().GetAura(SpellIds.ThorimsInvocation);
        if (thorimsInvocation != null)
        {
            spell_sha_thorims_invocation spellIdHolder = thorimsInvocation.GetScript<spell_sha_thorims_invocation>();
            if (spellIdHolder != null)
                spellIdHolder.SpellIdToTrigger = GetUnitTargetCountForEffect(0) > 1 ? SpellIds.ChainLightning : SpellIds.LightningBolt;
        }
    }

    public override void Register()
    {
        AfterCast.Add(new(UpdateThorimsInvocationSpell));
    }
}

[Script] // 115357 - Windstrike (Mh)
class spell_sha_thorims_invocation_trigger : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((SpellIds.ThorimsInvocation, 0));
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.ThorimsInvocation);
    }

    void TriggerLightningSpell(uint effIndex)
    {
        Unit caster = GetCaster();

        AuraEffect thorimsInvocation = caster.GetAuraEffect(SpellIds.ThorimsInvocation, 0);
        if (thorimsInvocation == null)
            return;

        spell_sha_thorims_invocation spellIdHolder = thorimsInvocation.GetBase().GetScript<spell_sha_thorims_invocation>();
        if (spellIdHolder == null)
            return;

        var (spellInfo, triggerFlags) = caster.GetCastSpellInfo(Global.SpellMgr.GetSpellInfo(spellIdHolder.SpellIdToTrigger, GetCastDifficulty()));

        // Remove Overflowing Maelstrom spellmod early to make next cast behave as if it consumed only 5 or less maelstrom stacks
        // this works because consuming "up to 5 stacks" will always cause Maelstrom Weapon stacks to drop to 5 or lower
        // which means Overflowing Maelstrom needs removing anyway
        caster.RemoveAurasDueToSpell(SpellIds.OverflowingMaelstromAura);

        caster.CastSpell(GetHitUnit(), spellInfo.Id, new CastSpellExtraArgs()
        {
            TriggerFlags = triggerFlags | TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.IgnoreCastTime | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });

        // Manually remove stacks - Maelstrom Weapon aura cannot proc from procs and free Lightning Bolt/Chain Lightning procs from Arc Discharge (455096) shoulnd't consume it
        Aura maelstromWeaponVisibleAura = caster.GetAura(SpellIds.MaelstromWeaponVisibleAura);
        if (maelstromWeaponVisibleAura != null)
            spell_sha_maelstrom_weapon_base.ConsumeMaelstromWeapon(caster, maelstromWeaponVisibleAura, thorimsInvocation.GetAmount());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(TriggerLightningSpell, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 51564 - Tidal Waves
class spell_sha_tidal_waves : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TidalWaves);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        GetTarget().CastSpell(GetTarget(), SpellIds.TidalWaves, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff,
            SpellValueOverrides =
            {
                new( SpellValueMod.BasePoint0, -aurEff.GetAmount() ),
                new(SpellValueMod.BasePoint1, aurEff.GetAmount() )
            }
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

[Script] // 28823 - Totemic Power
class spell_sha_t3_6p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TotemicPowerArmor, SpellIds.TotemicPowerAttackPower, SpellIds.TotemicPowerSpellPower, SpellIds.TotemicPowerMP5);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId;
        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        switch (target.GetClass())
        {
            case Class.Paladin:
            case Class.Priest:
            case Class.Shaman:
            case Class.Druid:
                spellId = SpellIds.TotemicPowerMP5;
                break;
            case Class.Mage:
            case Class.Warlock:
                spellId = SpellIds.TotemicPowerSpellPower;
                break;
            case Class.Hunter:
            case Class.Rogue:
                spellId = SpellIds.TotemicPowerAttackPower;
                break;
            case Class.Warrior:
                spellId = SpellIds.TotemicPowerArmor;
                break;
            default:
                return;
        }

        caster.CastSpell(target, spellId, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 28820 - Lightning Shield
class spell_sha_t3_8p_bonus : AuraScript
{
    void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();

        // Need remove self if Lightning Shield not active
        if (GetTarget().GetAuraEffect(AuraType.ProcTriggerSpell, SpellFamilyNames.Shaman, new FlagArray128(0x400), GetCaster().GetGUID()) == null)
            Remove();
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // 64928 - Item - Shaman T8 Elemental 4P Bonus
class spell_sha_t8_elemental_4p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Electrified);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo == null || damageInfo.GetDamage() == 0)
            return;

        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.Electrified, GetCastDifficulty());
        int amount = MathFunctions.CalculatePct((int)(damageInfo.GetDamage()), aurEff.GetAmount());

        Cypher.Assert(spellInfo.GetMaxTicks() > 0);
        amount /= (int)spellInfo.GetMaxTicks();

        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        caster.CastSpell(target, SpellIds.Electrified, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, amount) }
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 67228 - Item - Shaman T9 Elemental 4P Bonus (Lava Burst)
class spell_sha_t9_elemental_4p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LavaBurstBonusDamage);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo == null || damageInfo.GetDamage() == 0)
            return;

        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.LavaBurstBonusDamage, GetCastDifficulty());
        int amount = MathFunctions.CalculatePct((int)(damageInfo.GetDamage()), aurEff.GetAmount());

        Cypher.Assert(spellInfo.GetMaxTicks() > 0);
        amount /= (int)spellInfo.GetMaxTicks();

        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        caster.CastSpell(target, SpellIds.LavaBurstBonusDamage, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, amount) }
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 70817 - Item - Shaman T10 Elemental 4P Bonus
class spell_sha_t10_elemental_4p_bonus : AuraScript
{
    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        // try to find spell Flame Shock on the target
        AuraEffect flameShock = target.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Shaman, new FlagArray128(0x10000000), caster.GetGUID());
        if (flameShock == null)
            return;

        Aura flameShockAura = flameShock.GetBase();

        int maxDuration = flameShockAura.GetMaxDuration();
        int newDuration = flameShockAura.GetDuration() + aurEff.GetAmount() * Time.InMilliseconds;

        flameShockAura.SetDuration(newDuration);
        // is it blizzlike to change max duration for Fs?
        if (newDuration > maxDuration)
            flameShockAura.SetMaxDuration(newDuration);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 70808 - Item - Shaman T10 Restoration 4P Bonus
class spell_sha_t10_restoration_4p_bonus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ChainedHeal);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        HealInfo healInfo = eventInfo.GetHealInfo();
        if (healInfo == null || healInfo.GetHeal() == 0)
            return;

        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.ChainedHeal, GetCastDifficulty());
        int amount = MathFunctions.CalculatePct((int)(healInfo.GetHeal()), aurEff.GetAmount());

        Cypher.Assert(spellInfo.GetMaxTicks() > 0);
        amount /= (int)spellInfo.GetMaxTicks();

        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        caster.CastSpell(target, SpellIds.ChainedHeal, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.FullMask,
            TriggeringAura = aurEff,
            SpellValueOverrides = { new(SpellValueMod.BasePoint0, amount) }
        });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 260895 - Unlimited Power
class spell_sha_unlimited_power : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.UnlimitedPowerBuff);
    }

    static void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        Unit caster = procInfo.GetActor();
        Aura aura = caster.GetAura(SpellIds.UnlimitedPowerBuff);
        if (aura != null)
            aura.SetStackAmount((byte)(aura.GetStackAmount() + 1));
        else
            caster.CastSpell(caster, SpellIds.UnlimitedPowerBuff, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.FullMask,
                TriggeringSpell = procInfo.GetProcSpell()
            });
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 200071 - Undulation
class spell_sha_undulation_passive : AuraScript
{
    byte _castCounter = 1; // first proc happens after two casts, then one every 3 casts

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.UndulationProc);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        if (++_castCounter == 3)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.UndulationProc, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
            _castCounter = 0;
        }
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 470490 - Unrelenting Storms
class spell_sha_unrelenting_storms : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.UnrelentingStormsReduction)
            && ValidateSpellEffect((SpellIds.UnrelentingStormsTalent, 1));
    }

    public override bool Load()
    {
        return GetCaster().HasAura(SpellIds.UnrelentingStormsTalent);
    }

    void Trigger(uint effIndex)
    {
        Unit shaman = GetCaster();
        Aura unrelentingStorms = shaman.GetAura(SpellIds.UnrelentingStormsTalent);
        if (unrelentingStorms == null)
            return;

        long targetLimit = 0;
        AuraEffect limitEffect = unrelentingStorms.GetEffect(0);
        if (limitEffect != null)
            targetLimit = limitEffect.GetAmount();

        if (GetUnitTargetCountForEffect(effIndex) > targetLimit)
            return;

        TimeSpan cooldown = TimeSpan.FromSeconds(0);
        AuraEffect reductionPctEffect = unrelentingStorms.GetEffect(1);
        if (reductionPctEffect != null)
        {
            SpellHistory.GetCooldownDurations(GetSpellInfo(), 0, ref cooldown);

            shaman.CastSpell(shaman, SpellIds.UnrelentingStormsReduction, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
                SpellValueOverrides = { new(SpellValueMod.BasePoint0, -(int)(MathFunctions.CalculatePct((int)cooldown.TotalMilliseconds, reductionPctEffect.GetAmount()))) }
            });
        }

        if (shaman.HasAura(SpellIds.WindfuryAura))
            WindfuryProcEvent.Trigger(shaman, GetHitUnit());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(Trigger, 0, SpellEffectName.SchoolDamage));
    }
}

[Script] // 470057 - Voltaic Blaze
class spell_sha_voltaic_blaze : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return spell_sha_maelstrom_weapon_base.Validate();
    }

    void ApplyFlameShock(uint effIndex)
    {
        Unit caster = GetCaster();
        var targets = new CastSpellTargetArg(GetHitUnit());
        caster.m_Events.AddEventAtOffset(() =>
        {
            if (targets.Targets == null)
                return;

            targets.Targets.Update(caster);

            caster.CastSpell(targets, SpellIds.FlameShock, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
        }, TimeSpan.FromSeconds(500));
    }

    void EnergizeMaelstrom(uint effIndex)
    {
        spell_sha_maelstrom_weapon_base.GenerateMaelstromWeapon(GetCaster(), GetEffectValue());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(ApplyFlameShock, 0, SpellEffectName.SchoolDamage));
        OnEffectHitTarget.Add(new(EnergizeMaelstrom, 1, SpellEffectName.Dummy));
    }
}

[Script] // 470058 - Voltaic Blaze
class spell_sha_voltaic_blaze_aura : AuraScript
{
    static bool CheckProc(ProcEventInfo eventInfo)
    {
        // 470057 - Voltaic Blaze does not have any unique SpellFamilyFlags, check by id
        return eventInfo.GetSpellInfo().Id == SpellIds.VoltaicBlazeDamage;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

[Script] // 470053 - Voltaic Blaze
class spell_sha_voltaic_blaze_talent : AuraScript
{
    static bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return RandomHelper.randChance(aurEff.GetAmount());
    }

    static void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.VoltaicBlazeOverride);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 33757 - Windfury Weapon
class spell_sha_windfury_weapon : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.WindfuryEnchantment);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleEffect(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        Item mainHand = GetCaster().ToPlayer().GetWeaponForAttack(WeaponAttackType.BaseAttack, false);
        if (mainHand != null)
        {
            GetCaster().CastSpell(mainHand, SpellIds.WindfuryEnchantment, new CastSpellExtraArgs()
            {
                TriggerFlags = TriggerCastFlags.FullMask,
                TriggeringSpell = GetSpell()
            });
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleEffect, 0, SpellEffectName.Dummy));
    }
}

[Script] // 319773 - Windfury Weapon (proc)
class spell_sha_windfury_weapon_proc : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.WindfuryAttack, SpellIds.WindfuryVisual1, SpellIds.WindfuryVisual2, SpellIds.WindfuryVisual3, SpellIds.UnrulyWinds, SpellIds.ForcefulWindsTalent, SpellIds.ForcefulWindsProc);
    }

    void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        WindfuryProcEvent.Trigger(eventInfo.GetActor(), eventInfo.GetActionTarget());
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
    }
}

// 462767 - Arctic Snowstorm
[Script] // 36797 - AreatriggerId
class areatrigger_sha_arctic_snowstorm(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
        {
            if (unit.GetAura(SpellIds.FrostShock, caster.GetGUID()) != null)
                return;

            if (caster.IsValidAttackTarget(unit))
                caster.CastSpell(unit, SpellIds.ArcticSnowstormSlow, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError });
        }
    }

    public override void OnUnitExit(Unit unit)
    {
        unit.RemoveAurasDueToSpell(SpellIds.ArcticSnowstormSlow, at.GetCasterGUID());
    }
}

// 192078 - Wind Rush Totem (Spell)
[Script] // 12676 - AreaTriggerId
class areatrigger_sha_wind_rush_totem(AreaTrigger areaTrigger) : AreaTriggerAI(areaTrigger)
{
    static int RefreshTime = 4500;

    int _refreshTimer = RefreshTime;

    public override void OnUpdate(uint diff)
    {
        _refreshTimer -= (int)diff;
        if (_refreshTimer <= 0)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                foreach (ObjectGuid guid in at.GetInsideUnits())
                {
                    Unit unit = Global.ObjAccessor.GetUnit(caster, guid);
                    if (unit != null)
                        CastSpeedBuff(caster, unit);
                }
            }

            _refreshTimer += RefreshTime;
        }
    }

    public override void OnUnitEnter(Unit unit)
    {
        Unit caster = at.GetCaster();
        if (caster != null)
            CastSpeedBuff(caster, unit);
    }

    static void CastSpeedBuff(Unit caster, Unit unit)
    {
        if (!caster.IsValidAssistTarget(unit))
            return;

        caster.CastSpell(unit, SpellIds.WindRush, new CastSpellExtraArgs() { TriggerFlags = TriggerCastFlags.FullMask });
    }
}