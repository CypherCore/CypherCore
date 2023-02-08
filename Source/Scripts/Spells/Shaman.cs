// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Maps;

using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
    internal struct SpellIds
    {
        public const uint AftershockEnergize = 210712;
        public const uint AncestralGuidance = 108281;
        public const uint AncestralGuidanceHeal = 114911;
        public const uint ChainLightning = 188443;
        public const uint ChainLightningEnergize = 195897;
        public const uint ChainLightningOverload = 45297;
        public const uint ChainLightningOverloadEnergize = 218558;
        public const uint ChainedHeal = 70809;
        public const uint CrashLightningCleave = 187878;
        public const uint Earthquake = 61882;
        public const uint EarthquakeKnockingDown = 77505;
        public const uint EarthquakeTick = 77478;
        public const uint EarthShieldHeal = 204290;
        public const uint EarthenRagePassive = 170374;
        public const uint EarthenRagePeriodic = 170377;
        public const uint EarthenRageDamage = 170379;
        public const uint Electrified = 64930;
        public const uint ElementalBlast = 117014;
        public const uint ElementalBlastCrit = 118522;
        public const uint ElementalBlastEnergize = 344645;
        public const uint ElementalBlastHaste = 173183;
        public const uint ElementalBlastMastery = 173184;
        public const uint ElementalBlastOverload = 120588;
        public const uint ElementalMastery = 16166;
        public const uint EnergySurge = 40465;
        public const uint Exhaustion = 57723;
        public const uint FlameShock = 188389;
        public const uint FlametongueAttack = 10444;
        public const uint FlametongueWeaponEnchant = 334294;
        public const uint FlametongueWeaponAura = 319778;
        public const uint FrostShockEnergize = 289439;
        public const uint GatheringStorms = 198299;
        public const uint GatheringStormsBuff = 198300;
        public const uint GhostWolf = 2645;
        public const uint HealingRainVisual = 147490;
        public const uint HealingRainHeal = 73921;
        public const uint Icefury = 210714;
        public const uint IcefuryOverload = 219271;
        public const uint IgneousPotential = 279830;
        public const uint ItemLightningShield = 23552;
        public const uint ItemLightningShieldDamage = 27635;
        public const uint ItemManaSurge = 23571;
        public const uint LavaBeam = 114074;
        public const uint LavaBeamOverload = 114738;
        public const uint LavaBurst = 51505;
        public const uint LavaBurstBonusDamage = 71824;
        public const uint LavaBurstOverload = 77451;
        public const uint LavaBurstRank2 = 231721;
        public const uint LavaSurge = 77762;
        public const uint LightningBolt = 188196;
        public const uint LightningBoltEnergize = 214815;
        public const uint LightningBoltOverload = 45284;
        public const uint LightningBoltOverloadEnergize = 214816;
        public const uint LiquidMagmaHit = 192231;
        public const uint MaelstromController = 343725;
        public const uint MasteryElementalOverload = 168534;
        public const uint PathOfFlamesSpread = 210621;
        public const uint PathOfFlamesTalent = 201909;
        public const uint PowerSurge = 40466;
        public const uint Sated = 57724;
        public const uint SpiritWolfTalent = 260878;
        public const uint SpiritWolfPeriodic = 260882;
        public const uint SpiritWolfAura = 260881;
        public const uint Stormkeeper = 191634;
        public const uint TidalWaves = 53390;
        public const uint TotemicPowerMp5 = 28824;
        public const uint TotemicPowerSpellPower = 28825;
        public const uint TotemicPowerAttackPower = 28826;
        public const uint TotemicPowerArmor = 28827;
        public const uint UndulationProc = 216251;
        public const uint UnlimitedPowerBuff = 272737;
        public const uint WindfuryAttack = 25504;
        public const uint WindfuryEnchantment = 334302;
        public const uint WindRush = 192082;

        //Misc
        public const uint HunterInsanity = 95809;
        public const uint MageTemporalDisplacement = 80354;
        public const uint PetNetherwindsFatigued = 160455;
    }

    internal struct CreatureIds
    {
        public const uint HealingRainInvisibleStalker = 73400;
    }

    [Script] // 273221 - Aftershock
    internal class spell_sha_aftershock : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.AftershockEnergize);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();

            if (procSpell != null)
            {
                int? cost = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);

                if (cost.HasValue)
                    return cost > 0 && RandomHelper.randChance(aurEff.GetAmount());
            }

            return false;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();
            int? energize = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);

            eventInfo.GetActor()
                     .CastSpell(eventInfo.GetActor(),
                                SpellIds.AftershockEnergize,
                                new CastSpellExtraArgs(energize != 0)
                                    .AddSpellMod(SpellValueMod.BasePoint0, energize.Value));
        }
    }

    [Script] // 108281 - Ancestral Guidance
    internal class spell_sha_ancestral_guidance : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AncestralGuidanceHeal);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetHealInfo().GetSpellInfo().Id == SpellIds.AncestralGuidanceHeal)
                return false;

            return true;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            int bp0 = MathFunctions.CalculatePct((int)eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());

            if (bp0 != 0)
            {
                CastSpellExtraArgs args = new(aurEff);
                args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
                eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.AncestralGuidanceHeal, args);
            }
        }
    }

    [Script] // 114911 - Ancestral Guidance Heal
    internal class spell_sha_ancestral_guidance_heal : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AncestralGuidance);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(ResizeTargets, 0, Targets.UnitDestAreaAlly));
        }

        private void ResizeTargets(List<WorldObject> targets)
        {
            SelectRandomInjuredTargets(targets, 3, true);
        }
    }

    [Script] // 2825 - Bloodlust
    internal class spell_sha_bloodlust : SpellScript, ISpellAfterHit, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Sated, SpellIds.HunterInsanity, SpellIds.MageTemporalDisplacement, SpellIds.PetNetherwindsFatigued);
        }

        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target)
                target.CastSpell(target, SpellIds.Sated, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 0, Targets.UnitCasterAreaRaid));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 1, Targets.UnitCasterAreaRaid));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void RemoveInvalidTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.Sated));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HunterInsanity));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.MageTemporalDisplacement));
        }
    }

    [Script] // 188443 - Chain Lightning
    internal class spell_sha_chain_lightning : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChainLightningEnergize, SpellIds.MaelstromController) && Global.SpellMgr.GetSpellInfo(SpellIds.MaelstromController, Difficulty.None).GetEffects().Count > 4;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
        }

        private void HandleScript(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 4);

            if (energizeAmount != null)
                GetCaster()
                    .CastSpell(GetCaster(),
                               SpellIds.ChainLightningEnergize,
                               new CastSpellExtraArgs(energizeAmount)
                                   .AddSpellMod(SpellValueMod.BasePoint0, (int)(energizeAmount.GetAmount() * GetUnitTargetCountForEffect(0))));
        }
    }

    [Script] // 45297 - Chain Lightning Overload
    internal class spell_sha_chain_lightning_overload : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChainLightningOverloadEnergize, SpellIds.MaelstromController) && Global.SpellMgr.GetSpellInfo(SpellIds.MaelstromController, Difficulty.None).GetEffects().Count > 5;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
        }

        private void HandleScript(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 5);

            if (energizeAmount != null)
                GetCaster()
                    .CastSpell(GetCaster(),
                               SpellIds.ChainLightningOverloadEnergize,
                               new CastSpellExtraArgs(energizeAmount)
                                   .AddSpellMod(SpellValueMod.BasePoint0, (int)(energizeAmount.GetAmount() * GetUnitTargetCountForEffect(0))));
        }
    }

    [Script] // 187874 - Crash Lightning
    internal class spell_sha_crash_lightning : SpellScript, ISpellAfterCast, IHasSpellEffects
    {
        private int _targetsHit;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CrashLightningCleave);
        }

        public void AfterCast()
        {
            if (_targetsHit >= 2)
                GetCaster().CastSpell(GetCaster(), SpellIds.CrashLightningCleave, true);

            AuraEffect gatheringStorms = GetCaster().GetAuraEffect(SpellIds.GatheringStorms, 0);

            if (gatheringStorms != null)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)(gatheringStorms.GetAmount() * _targetsHit));
                GetCaster().CastSpell(GetCaster(), SpellIds.GatheringStormsBuff, args);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitConeCasterToDestEnemy));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void CountTargets(List<WorldObject> targets)
        {
            _targetsHit = targets.Count;
        }
    }

    [Script] // 207778 - Downpour
    internal class spell_sha_downpour : SpellScript, ISpellAfterCast, ISpellAfterHit, IHasSpellEffects
    {
        private int _healedTargets = 0;

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 1;
        }

        public void AfterCast()
        {
            var cooldown = TimeSpan.FromMilliseconds(GetSpellInfo().RecoveryTime) + TimeSpan.FromSeconds(GetEffectInfo(1).CalcValue() * _healedTargets);
            GetCaster().GetSpellHistory().StartCooldown(GetSpellInfo(), 0, GetSpell(), false, cooldown);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }

        public void AfterHit()
        {
            // Cooldown increased for each Target effectively healed
            if (GetHitHeal() != 0)
                ++_healedTargets;
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void FilterTargets(List<WorldObject> targets)
        {
            SelectRandomInjuredTargets(targets, 6, true);
        }
    }

    [Script] // 204288 - Earth Shield
    internal class spell_sha_earth_shield : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EarthShieldHeal);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetDamageInfo() == null ||
                !HasEffect(1) ||
                eventInfo.GetDamageInfo().GetDamage() < GetTarget().CountPctFromMaxHealth(GetEffect(1).GetAmount()))
                return false;

            return true;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            GetTarget().CastSpell(GetTarget(), SpellIds.EarthShieldHeal, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
        }
    }

    [Script] // 170374 - Earthen Rage (Passive)
    public class spell_sha_earthen_rage_passive : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        private ObjectGuid _procTargetGuid;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EarthenRagePeriodic, SpellIds.EarthenRageDamage);
        }

        public bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().Id != SpellIds.EarthenRageDamage;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public ObjectGuid GetProcTargetGuid()
        {
            return _procTargetGuid;
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            _procTargetGuid = eventInfo.GetProcTarget().GetGUID();
            eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.EarthenRagePeriodic, true);
        }
    }

    [Script] // 170377 - Earthen Rage (Proc Aura)
    internal class spell_sha_earthen_rage_proc_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EarthenRagePassive, SpellIds.EarthenRageDamage);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            Aura aura = GetCaster().GetAura(SpellIds.EarthenRagePassive);

            if (aura != null)
            {
                var earthen_rage_script = aura.GetScript<spell_sha_earthen_rage_passive>();

                if (earthen_rage_script != null)
                {
                    Unit procTarget = Global.ObjAccessor.GetUnit(GetCaster(), earthen_rage_script.GetProcTargetGuid());

                    if (procTarget)
                        GetTarget().CastSpell(procTarget, SpellIds.EarthenRageDamage, true);
                }
            }
        }
    }

    // 61882 - Earthquake
    [Script] //  8382 - AreaTriggerId
    internal class areatrigger_sha_earthquake : AreaTriggerAI
    {
        private readonly HashSet<ObjectGuid> _stunnedUnits = new();
        private TimeSpan _period;
        private TimeSpan _refreshTimer;

        public areatrigger_sha_earthquake(AreaTrigger areatrigger) : base(areatrigger)
        {
            _refreshTimer = TimeSpan.Zero;
            _period = TimeSpan.FromSeconds(1);
        }

        public override void OnCreate()
        {
            Unit caster = at.GetCaster();

            if (caster != null)
            {
                AuraEffect earthquake = caster.GetAuraEffect(SpellIds.Earthquake, 1);

                if (earthquake != null)
                    _period = TimeSpan.FromMilliseconds(earthquake.GetPeriod());
            }
        }

        public override void OnUpdate(uint diff)
        {
            _refreshTimer -= TimeSpan.FromMilliseconds(diff);

            while (_refreshTimer <= TimeSpan.Zero)
            {
                Unit caster = at.GetCaster();

                caster?.CastSpell(at.GetPosition(),
                                     SpellIds.EarthquakeTick,
                                     new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                         .SetOriginalCaster(at.GetGUID()));

                _refreshTimer += _period;
            }
        }

        // Each Target can only be stunned once by each earthquake - keep track of who we already stunned
        public bool AddStunnedTarget(ObjectGuid guid)
        {
            return _stunnedUnits.Add(guid);
        }
    }

    [Script] // 77478 - Earthquake tick
    internal class spell_sha_earthquake_tick : SpellScript, ISpellOnHit, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EarthquakeKnockingDown) && spellInfo.GetEffects().Count > 1;
        }

        public void OnHit()
        {
            Unit target = GetHitUnit();

            if (target != null)
                if (RandomHelper.randChance(GetEffectInfo(1).CalcValue()))
                {
                    var areaTriggers = GetCaster().GetAreaTriggers(SpellIds.Earthquake);
                    var foundAreaTrigger = areaTriggers.Find(at => at.GetGUID() == GetSpell().GetOriginalCasterGUID());

                    if (foundAreaTrigger != null)
                    {
                        areatrigger_sha_earthquake eq = foundAreaTrigger.GetAI<areatrigger_sha_earthquake>();

                        if (eq != null)
                            if (eq.AddStunnedTarget(target.GetGUID()))
                                GetCaster().CastSpell(target, SpellIds.EarthquakeKnockingDown, true);
                    }
                }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamageCalc, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
        }

        private void HandleDamageCalc(uint effIndex)
        {
            SetEffectValue((int)(GetCaster().SpellBaseDamageBonusDone(SpellSchoolMask.Nature) * 0.391f));
        }
    }

    // 117014 - Elemental Blast
    [Script] // 120588 - Elemental Blast Overload
    internal class spell_sha_elemental_blast : SpellScript, ISpellAfterCast, IHasSpellEffects
    {
        private readonly uint[] BuffSpells =
        {
            SpellIds.ElementalBlastCrit, SpellIds.ElementalBlastHaste, SpellIds.ElementalBlastMastery
        };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ElementalBlastCrit, SpellIds.ElementalBlastHaste, SpellIds.ElementalBlastMastery, SpellIds.MaelstromController) && Global.SpellMgr.GetSpellInfo(SpellIds.MaelstromController, Difficulty.None).GetEffects().Count > 10;
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();
            uint spellId = BuffSpells.SelectRandomElementByWeight(buffSpellId => { return !caster.HasAura(buffSpellId) ? 1.0f : 0.0f; });

            GetCaster().CastSpell(GetCaster(), spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEnergize, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleEnergize(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, GetSpellInfo().Id == SpellIds.ElementalBlast ? 9 : 10u);

            if (energizeAmount != null)
                GetCaster()
                    .CastSpell(GetCaster(),
                               SpellIds.ElementalBlastEnergize,
                               new CastSpellExtraArgs(energizeAmount)
                                   .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.GetAmount()));
        }
    }

    [Script] // 318038 - Flametongue Weapon
    internal class spell_sha_flametongue_weapon : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlametongueWeaponEnchant);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleEffectHitTarget(uint index)
        {
            Player player = GetCaster().ToPlayer();
            byte slot = EquipmentSlot.MainHand;

            if (player.GetPrimarySpecialization() == TalentSpecialization.ShamanEnhancement)
                slot = EquipmentSlot.OffHand;

            Item targetItem = player.GetItemByPos(InventorySlots.Bag0, slot);

            if (targetItem == null ||
                !targetItem.GetTemplate().IsWeapon())
                return;

            GetCaster().CastSpell(targetItem, SpellIds.FlametongueWeaponEnchant, true);
        }
    }

    [Script] // 319778 - Flametongue
    internal class spell_sha_flametongue_weapon_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlametongueAttack);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit attacker = eventInfo.GetActor();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, Math.Max(1, (int)(attacker.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.0264f)));
            attacker.CastSpell(eventInfo.GetActionTarget(), SpellIds.FlametongueAttack, args);
        }
    }

    [Script] // 73920 - Healing Rain
    internal class spell_sha_healing_rain : SpellScript, ISpellAfterHit
    {
        public void AfterHit()
        {
            Aura aura = GetHitAura();

            if (aura != null)
            {
                WorldLocation dest = GetExplTargetDest();

                if (dest != null)
                {
                    int duration = GetSpellInfo().CalcDuration(GetOriginalCaster());
                    TempSummon summon = GetCaster().GetMap().SummonCreature(CreatureIds.HealingRainInvisibleStalker, dest, null, (uint)duration, GetOriginalCaster());

                    if (summon == null)
                        return;

                    summon.CastSpell(summon, SpellIds.HealingRainVisual, true);

                    var script = aura.GetScript<spell_sha_healing_rain_AuraScript>();

                    script?.SetVisualDummy(summon);
                }
            }
        }
    }

    [Script] // 73920 - Healing Rain (Aura)
    internal class spell_sha_healing_rain_AuraScript : AuraScript, IHasAuraEffects
    {
        private ObjectGuid _visualDummy;
        private float _x;
        private float _y;
        private float _z;
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public void SetVisualDummy(TempSummon summon)
        {
            _visualDummy = summon.GetGUID();
            summon.GetPosition(out _x, out _y, out _z);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(HandleEffecRemoved, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDummy));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            GetTarget().CastSpell(new Position(_x, _y, _z), SpellIds.HealingRainHeal, new CastSpellExtraArgs(aurEff));
        }

        private void HandleEffecRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature summon = ObjectAccessor.GetCreature(GetTarget(), _visualDummy);

            summon?.DespawnOrUnsummon();
        }
    }

    [Script] // 73921 - Healing Rain
    internal class spell_sha_healing_rain_target_limit : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectTargets, 0, Targets.UnitDestAreaAlly));
        }

        private void SelectTargets(List<WorldObject> targets)
        {
            SelectRandomInjuredTargets(targets, 6, true);
        }
    }

    [Script] // 52042 - Healing Stream Totem
    internal class spell_sha_healing_stream_totem_heal : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectTargets, 0, Targets.UnitDestAreaAlly));
        }

        private void SelectTargets(List<WorldObject> targets)
        {
            SelectRandomInjuredTargets(targets, 1, true);
        }
    }

    [Script] // 32182 - Heroism
    internal class spell_sha_heroism : SpellScript, ISpellAfterHit, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhaustion, SpellIds.HunterInsanity, SpellIds.MageTemporalDisplacement, SpellIds.PetNetherwindsFatigued);
        }

        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target)
                target.CastSpell(target, SpellIds.Exhaustion, true);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 0, Targets.UnitCasterAreaRaid));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 1, Targets.UnitCasterAreaRaid));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void RemoveInvalidTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.Exhaustion));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.HunterInsanity));
            targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, SpellIds.MageTemporalDisplacement));
        }
    }

    [Script] // 210714 - Icefury
    internal class spell_sha_icefury : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FrostShockEnergize);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Unit caster = GetCaster();

            caster?.CastSpell(caster, SpellIds.FrostShockEnergize, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }
    }

    [Script] // 23551 - Lightning Shield T2 Bonus
    internal class spell_sha_item_lightning_shield : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemLightningShield);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.ItemLightningShield, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 23552 - Lightning Shield T2 Bonus
    internal class spell_sha_item_lightning_shield_trigger : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemLightningShieldDamage);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemLightningShieldDamage, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 23572 - Mana Surge
    internal class spell_sha_item_mana_surge : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ItemManaSurge);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcSpell() != null;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            var costs = eventInfo.GetProcSpell().GetPowerCost();
            var m = costs.Find(cost => cost.Power == PowerType.Mana);

            if (m != null)
            {
                int mana = MathFunctions.CalculatePct(m.Amount, 35);

                if (mana > 0)
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, mana);
                    GetTarget().CastSpell(GetTarget(), SpellIds.ItemManaSurge, args);
                }
            }
        }
    }

    [Script] // 40463 - Shaman Tier 6 Trinket
    internal class spell_sha_item_t6_trinket : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EnergySurge, SpellIds.PowerSurge);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo == null)
                return;

            uint spellId;
            int chance;

            // Lesser Healing Wave
            if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000080u))
            {
                spellId = SpellIds.EnergySurge;
                chance = 10;
            }
            // Lightning Bolt
            else if (spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000001u))
            {
                spellId = SpellIds.EnergySurge;
                chance = 15;
            }
            // Stormstrike
            else if (spellInfo.SpellFamilyFlags[1].HasAnyFlag(0x00000010u))
            {
                spellId = SpellIds.PowerSurge;
                chance = 50;
            }
            else
            {
                return;
            }

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell((Unit)null, spellId, true);
        }
    }

    [Script] // 70811 - Item - Shaman T10 Elemental 2P Bonus
    internal class spell_sha_item_t10_elemental_2p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ElementalMastery);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Player target = GetTarget().ToPlayer();

            if (target)
                target.GetSpellHistory().ModifyCooldown(SpellIds.ElementalMastery, TimeSpan.FromMilliseconds(-aurEff.GetAmount()));
        }
    }

    [Script] // 189063 - Lightning Vortex (proc 185881 Item - Shaman T18 Elemental 4P Bonus)
    internal class spell_sha_item_t18_elemental_4p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(DiminishHaste, 1, AuraType.PeriodicDummy));
        }

        private void DiminishHaste(AuraEffect aurEff)
        {
            PreventDefaultAction();
            AuraEffect hasteBuff = GetEffect(0);

            hasteBuff?.ChangeAmount(hasteBuff.GetAmount() - aurEff.GetAmount());
        }
    }

    [Script] // 51505 - Lava burst
    internal class spell_sha_lava_burst : SpellScript, ISpellAfterCast, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PathOfFlamesTalent, SpellIds.PathOfFlamesSpread, SpellIds.LavaSurge);
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();

            Aura lavaSurge = caster.GetAura(SpellIds.LavaSurge);

            if (lavaSurge != null)
                if (!GetSpell().m_appliedMods.Contains(lavaSurge))
                {
                    uint chargeCategoryId = GetSpellInfo().ChargeCategoryId;

                    // Ensure we have at least 1 usable charge after cast to allow next cast immediately
                    if (!caster.GetSpellHistory().HasCharge(chargeCategoryId))
                        caster.GetSpellHistory().RestoreCharge(chargeCategoryId);
                }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();

            if (caster)
                if (caster.HasAura(SpellIds.PathOfFlamesTalent))
                    caster.CastSpell(GetHitUnit(), SpellIds.PathOfFlamesSpread, new CastSpellExtraArgs(GetSpell()));
        }
    }

    // 285452 - Lava Burst Damage
    [Script] // 285466 - Lava Burst Overload Damage
    internal class spell_sha_lava_crit_chance : SpellScript, ISpellCalcCritChance
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LavaBurstRank2, SpellIds.FlameShock);
        }

        public void CalcCritChance(Unit victim, ref float critChance)
        {
            Unit caster = GetCaster();

            if (caster == null ||
                victim == null)
                return;

            if (caster.HasAura(SpellIds.LavaBurstRank2) &&
                victim.HasAura(SpellIds.FlameShock, caster.GetGUID()))
                if (victim.GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance) > -100)
                    critChance = 100.0f;
        }
    }

    [Script] // 77756 - Lava Surge
    internal class spell_sha_lava_surge : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LavaSurge, SpellIds.IgneousPotential);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProcChance, 0, AuraType.Dummy));
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckProcChance(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            int procChance = aurEff.GetAmount();
            AuraEffect igneousPotential = GetTarget().GetAuraEffect(SpellIds.IgneousPotential, 0);

            if (igneousPotential != null)
                procChance += igneousPotential.GetAmount();

            return RandomHelper.randChance(procChance);
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.LavaSurge, true);
        }
    }

    [Script] // 77762 - Lava Surge
    internal class spell_sha_lava_surge_proc : SpellScript, ISpellAfterHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LavaBurst);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public void AfterHit()
        {
            GetCaster().GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(SpellIds.LavaBurst, GetCastDifficulty()).ChargeCategoryId);
        }
    }

    [Script] // 188196 - Lightning Bolt
    internal class spell_sha_lightning_bolt : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LightningBoltEnergize, SpellIds.MaelstromController) && Global.SpellMgr.GetSpellInfo(SpellIds.MaelstromController, Difficulty.None).GetEffects().Count > 0;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
        }

        private void HandleScript(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 0);

            if (energizeAmount != null)
                GetCaster()
                    .CastSpell(GetCaster(),
                               SpellIds.LightningBoltEnergize,
                               new CastSpellExtraArgs(energizeAmount)
                                   .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.GetAmount()));
        }
    }

    [Script] // 45284 - Lightning Bolt Overload
    internal class spell_sha_lightning_bolt_overload : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LightningBoltOverloadEnergize, SpellIds.MaelstromController) && Global.SpellMgr.GetSpellInfo(SpellIds.MaelstromController, Difficulty.None).GetEffects().Count > 1;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.Launch));
        }

        private void HandleScript(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 1);

            if (energizeAmount != null)
                GetCaster()
                    .CastSpell(GetCaster(),
                               SpellIds.LightningBoltOverloadEnergize,
                               new CastSpellExtraArgs(energizeAmount)
                                   .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.GetAmount()));
        }
    }

    [Script] // 192223 - Liquid Magma Totem (erupting hit spell)
    internal class spell_sha_liquid_magma_totem : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LiquidMagmaHit);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(HandleTargetSelect, 0, Targets.UnitDestAreaEnemy));
            SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleEffectHitTarget(uint effIndex)
        {
            Unit hitUnit = GetHitUnit();

            if (hitUnit != null)
                GetCaster().CastSpell(hitUnit, SpellIds.LiquidMagmaHit, true);
        }

        private void HandleTargetSelect(List<WorldObject> targets)
        {
            // choose one random Target from targets
            if (targets.Count > 1)
            {
                WorldObject selected = targets.SelectRandom();
                targets.Clear();
                targets.Add(selected);
            }
        }
    }

    [Script] // 168534 - Mastery: Elemental Overload (passive)
    internal class spell_sha_mastery_elemental_overload : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LightningBolt,
                                     SpellIds.LightningBoltOverload,
                                     SpellIds.ElementalBlast,
                                     SpellIds.ElementalBlastOverload,
                                     SpellIds.Icefury,
                                     SpellIds.IcefuryOverload,
                                     SpellIds.LavaBurst,
                                     SpellIds.LavaBurstOverload,
                                     SpellIds.ChainLightning,
                                     SpellIds.ChainLightningOverload,
                                     SpellIds.LavaBeam,
                                     SpellIds.LavaBeamOverload,
                                     SpellIds.Stormkeeper);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            if (spellInfo == null ||
                !eventInfo.GetProcSpell())
                return false;

            if (GetTriggeredSpellId(spellInfo.Id) == 0)
                return false;

            float chance = aurEff.GetAmount(); // Mastery % amount

            if (spellInfo.Id == SpellIds.ChainLightning)
                chance /= 3.0f;

            Aura stormkeeper = eventInfo.GetActor().GetAura(SpellIds.Stormkeeper);

            if (stormkeeper != null)
                if (eventInfo.GetProcSpell().m_appliedMods.Contains(stormkeeper))
                    chance = 100.0f;

            return RandomHelper.randChance(chance);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
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
                                            },
                                            TimeSpan.FromMilliseconds(400));
        }

        private uint GetTriggeredSpellId(uint triggeringSpellId)
        {
            switch (triggeringSpellId)
            {
                case SpellIds.LightningBolt:
                    return SpellIds.LightningBoltOverload;
                case SpellIds.ElementalBlast:
                    return SpellIds.ElementalBlastOverload;
                case SpellIds.Icefury:
                    return SpellIds.IcefuryOverload;
                case SpellIds.LavaBurst:
                    return SpellIds.LavaBurstOverload;
                case SpellIds.ChainLightning:
                    return SpellIds.ChainLightningOverload;
                case SpellIds.LavaBeam:
                    return SpellIds.LavaBeamOverload;
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
    internal class spell_sha_mastery_elemental_overload_proc : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MasteryElementalOverload);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(ApplyDamageModifier, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void ApplyDamageModifier(uint effIndex)
        {
            AuraEffect elementalOverload = GetCaster().GetAuraEffect(SpellIds.MasteryElementalOverload, 1);

            if (elementalOverload != null)
                SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), elementalOverload.GetAmount()));
        }
    }

    [Script] // 30884 - Nature's Guardian
    internal class spell_sha_natures_guardian : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.ProcTriggerSpell));
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetActionTarget().HealthBelowPct(aurEff.GetAmount());
        }
    }

    [Script] // 210621 - Path of Flames Spread
    internal class spell_sha_path_of_flames_spread : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlameShock);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
            SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            targets.Remove(GetExplTargetUnit());
            targets.RandomResize(target => target.IsTypeId(TypeId.Unit) && !target.ToUnit().HasAura(SpellIds.FlameShock, GetCaster().GetGUID()), 1);
        }

        private void HandleScript(uint effIndex)
        {
            Unit mainTarget = GetExplTargetUnit();

            if (mainTarget)
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
    }

    // 2645 - Ghost Wolf
    [Script] // 260878 - Spirit Wolf
    internal class spell_sha_spirit_wolf : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GhostWolf, SpellIds.SpiritWolfTalent, SpellIds.SpiritWolfPeriodic, SpellIds.SpiritWolfAura);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();

            if (target.HasAura(SpellIds.SpiritWolfTalent) &&
                target.HasAura(SpellIds.GhostWolf))
                target.CastSpell(target, SpellIds.SpiritWolfPeriodic, new CastSpellExtraArgs(aurEff));
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SpiritWolfPeriodic);
            GetTarget().RemoveAurasDueToSpell(SpellIds.SpiritWolfAura);
        }
    }

    [Script] // 51562 - Tidal Waves
    internal class spell_sha_tidal_waves : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TidalWaves);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, -aurEff.GetAmount());
            args.AddSpellMod(SpellValueMod.BasePoint1, aurEff.GetAmount());

            GetTarget().CastSpell(GetTarget(), SpellIds.TidalWaves, args);
        }
    }

    [Script] // 28823 - Totemic Power
    internal class spell_sha_t3_6p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TotemicPowerArmor, SpellIds.TotemicPowerAttackPower, SpellIds.TotemicPowerSpellPower, SpellIds.TotemicPowerMp5);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
                    spellId = SpellIds.TotemicPowerMp5;

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

            caster.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 28820 - Lightning Shield
    internal class spell_sha_t3_8p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
        }

        private void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();

            // Need remove self if Lightning Shield not active
            if (GetTarget().GetAuraEffect(AuraType.ProcTriggerSpell, SpellFamilyNames.Shaman, new FlagArray128(0x400), GetCaster().GetGUID()) == null)
                Remove();
        }
    }

    [Script] // 64928 - Item - Shaman T8 Elemental 4P Bonus
    internal class spell_sha_t8_elemental_4p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Electrified);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.Electrified, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.Electrified, args);
        }
    }

    [Script] // 67228 - Item - Shaman T9 Elemental 4P Bonus (Lava Burst)
    internal class spell_sha_t9_elemental_4p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LavaBurstBonusDamage);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.LavaBurstBonusDamage, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.LavaBurstBonusDamage, args);
        }
    }

    [Script] // 70817 - Item - Shaman T10 Elemental 4P Bonus
    internal class spell_sha_t10_elemental_4p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            // try to find spell Flame Shock on the Target
            AuraEffect flameShock = target.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Shaman, new FlagArray128(0x10000000), caster.GetGUID());

            if (flameShock == null)
                return;

            Aura flameShockAura = flameShock.GetBase();

            int maxDuration = flameShockAura.GetMaxDuration();
            int newDuration = flameShockAura.GetDuration() + aurEff.GetAmount() * Time.InMilliseconds;

            flameShockAura.SetDuration(newDuration);

            // is it blizzlike to change max duration for FS?
            if (newDuration > maxDuration)
                flameShockAura.SetMaxDuration(newDuration);
        }
    }

    [Script] // 70808 - Item - Shaman T10 Restoration 4P Bonus
    internal class spell_sha_t10_restoration_4p_bonus : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChainedHeal);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            HealInfo healInfo = eventInfo.GetHealInfo();

            if (healInfo == null ||
                healInfo.GetHeal() == 0)
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.ChainedHeal, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
            amount /= (int)spellInfo.GetMaxTicks();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.ChainedHeal, args);
        }
    }

    [Script] // 260895 - Unlimited Power
    internal class spell_sha_unlimited_power : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.UnlimitedPowerBuff);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit caster = procInfo.GetActor();
            Aura aura = caster.GetAura(SpellIds.UnlimitedPowerBuff);

            if (aura != null)
                aura.SetStackAmount((byte)(aura.GetStackAmount() + 1));
            else
                caster.CastSpell(caster, SpellIds.UnlimitedPowerBuff, procInfo.GetProcSpell());
        }
    }

    [Script] // 200071 - Undulation
    internal class spell_sha_undulation_passive : AuraScript, IHasAuraEffects
    {
        private byte _castCounter = 1; // first proc happens after two casts, then one every 3 casts
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.UndulationProc);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            if (++_castCounter == 3)
            {
                GetTarget().CastSpell(GetTarget(), SpellIds.UndulationProc, true);
                _castCounter = 0;
            }
        }
    }

    [Script] // 33757 - Windfury Weapon
    internal class spell_sha_windfury_weapon : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WindfuryEnchantment);
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Item mainHand = GetCaster().ToPlayer().GetWeaponForAttack(WeaponAttackType.BaseAttack, false);

            if (mainHand != null)
                GetCaster().CastSpell(mainHand, SpellIds.WindfuryEnchantment, GetSpell());
        }
    }

    [Script] // 319773 - Windfury Weapon (proc)
    internal class spell_sha_windfury_weapon_proc : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WindfuryAttack);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            for (uint i = 0; i < 2; ++i)
                eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.WindfuryAttack, new CastSpellExtraArgs(aurEff));
        }
    }

    // 192078 - Wind Rush Totem (Spell)
    [Script] //  12676 - AreaTriggerId
    internal class areatrigger_sha_wind_rush_totem : AreaTriggerAI
    {
        private static readonly int REFRESH_TIME = 4500;

        private int _refreshTimer;

        public areatrigger_sha_wind_rush_totem(AreaTrigger areatrigger) : base(areatrigger)
        {
            _refreshTimer = REFRESH_TIME;
        }

        public override void OnUpdate(uint diff)
        {
            _refreshTimer -= (int)diff;

            if (_refreshTimer <= 0)
            {
                Unit caster = at.GetCaster();

                if (caster != null)
                    foreach (ObjectGuid guid in at.GetInsideUnits())
                    {
                        Unit unit = Global.ObjAccessor.GetUnit(caster, guid);

                        if (unit != null)
                        {
                            if (!caster.IsFriendlyTo(unit))
                                continue;

                            caster.CastSpell(unit, SpellIds.WindRush, true);
                        }
                    }

                _refreshTimer += REFRESH_TIME;
            }
        }

        public override void OnUnitEnter(Unit unit)
        {
            Unit caster = at.GetCaster();

            if (caster != null)
            {
                if (!caster.IsFriendlyTo(unit))
                    return;

                caster.CastSpell(unit, SpellIds.WindRush, true);
            }
        }
    }
}