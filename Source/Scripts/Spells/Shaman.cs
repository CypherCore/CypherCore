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

using static Global;

namespace Scripts.Spells.Shaman
{

    struct SpellIds
    {
        public const uint AftershockEnergize = 210712;
        public const uint AncestralGuidance = 108281;
        public const uint AncestralGuidanceHeal = 114911;
        public const uint AscendanceElemental = 114050;
        public const uint AscendanceEnhancement = 114051;
        public const uint AscendanceRestoration = 114052;
        public const uint ChainLightning = 188443;
        public const uint ChainLightningEnergize = 195897;
        public const uint ChainLightningOverload = 45297;
        public const uint ChainLightningOverloadEnergize = 218558;
        public const uint ChainedHeal = 70809;
        public const uint CrashLightningCleave = 187878;
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
        public const uint ElementalBlastEnergize = 344645;
        public const uint ElementalBlastHaste = 173183;
        public const uint ElementalBlastMastery = 173184;
        public const uint ElementalBlastOverload = 120588;
        public const uint ElementalMastery = 16166;
        public const uint EnergySurge = 40465;
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
        public const uint RestorativeMists = 114083;
        public const uint RestorativeMistsInitial = 294020;
        public const uint Riptide = 61295;
        public const uint SpiritWolfTalent = 260878;
        public const uint SpiritWolfPeriodic = 260882;
        public const uint SpiritWolfAura = 260881;
        public const uint Stormkeeper = 191634;
        public const uint Stormstrike = 17364;
        public const uint T292PElementalDamageBuff = 394651;
        public const uint TidalWaves = 53390;
        public const uint TotemicPowerArmor = 28827;
        public const uint TotemicPowerAttackPower = 28826;
        public const uint TotemicPowerMp5 = 28824;
        public const uint TotemicPowerSpellPower = 28825;
        public const uint UndulationProc = 216251;
        public const uint UnlimitedPowerBuff = 272737;
        public const uint VolcanicSurge = 408572;
        public const uint WindfuryAttack = 25504;
        public const uint WindfuryEnchantment = 334302;
        public const uint WindRush = 192082;

        public const uint LabelShamanWindfuryTotem = 1038;
    }

    [Script] // 273221 - Aftershock
    class spell_sha_aftershock : AuraScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.AftershockEnergize);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();
            if (procSpell != null)
            {
                var cost = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);
                if (cost.HasValue)
                    return cost.Value > 0 && RandomHelper.randChance(aurEff.GetAmount());
            }

            return false;
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            Spell procSpell = eventInfo.GetProcSpell();
            int? energize = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);

            eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.AftershockEnergize, new CastSpellExtraArgs(energize.HasValue)
                .AddSpellMod(SpellValueMod.BasePoint0, energize.Value));
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

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetHealInfo() != null && eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().Id == SpellIds.AncestralGuidanceHeal)
                return false;

            if (eventInfo.GetHealInfo() == null && eventInfo.GetDamageInfo() == null)
                return false;

            return true;
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            int bp0 = MathFunctions.CalculatePct((int)(eventInfo.GetDamageInfo() != null ? eventInfo.GetDamageInfo().GetDamage() : eventInfo.GetHealInfo().GetHeal()), aurEff.GetAmount());
            if (bp0 != 0)
            {
                CastSpellExtraArgs args = new(aurEff);
                args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
                eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.AncestralGuidanceHeal, args);
            }
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
            OnObjectAreaTargetSelect.Add(new(ResizeTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    [Script] // 114052 - Ascendance (Restoration)
    class spell_sha_ascendance_restoration : AuraScript
    {
        uint _healToDistribute;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RestorativeMists);
        }

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetHealInfo() != null && procInfo.GetHealInfo().GetOriginalHeal() != 0 && procInfo.GetSpellInfo().Id != SpellIds.RestorativeMistsInitial;
        }

        void OnProcHeal(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            _healToDistribute += procInfo.GetHealInfo().GetOriginalHeal();
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            if (_healToDistribute == 0)
                return;

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)_healToDistribute);
            GetTarget().CastSpell(null, SpellIds.RestorativeMists, args);
            _healToDistribute = 0;
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(OnProcHeal, 1, AuraType.PeriodicDummy));
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 1, AuraType.PeriodicDummy));
        }
    }

    [Script] // 188443 - Chain Lightning
    class spell_sha_chain_lightning : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChainLightningEnergize, SpellIds.MaelstromController)
            && ValidateSpellEffect((SpellIds.MaelstromController, 4));
        }

        void HandleScript(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 4);
            if (energizeAmount != null)
                GetCaster().CastSpell(GetCaster(), SpellIds.ChainLightningEnergize, new CastSpellExtraArgs(energizeAmount)
                    .AddSpellMod(SpellValueMod.BasePoint0, (int)(energizeAmount.GetAmount() * GetUnitTargetCountForEffect(0))));
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
            return ValidateSpellInfo(SpellIds.ChainLightningOverloadEnergize, SpellIds.MaelstromController)
                && ValidateSpellEffect((SpellIds.MaelstromController, 5));
        }

        void HandleScript(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 5);
            if (energizeAmount != null)
                GetCaster().CastSpell(GetCaster(), SpellIds.ChainLightningOverloadEnergize, new CastSpellExtraArgs(energizeAmount)
                    .AddSpellMod(SpellValueMod.BasePoint0, (int)(energizeAmount.GetAmount() * GetUnitTargetCountForEffect(0))));
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new(HandleScript, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // 187874 - Crash Lightning
    class spell_sha_crash_lightning : SpellScript
    {
        int _targetsHit;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CrashLightningCleave, SpellIds.GatheringStorms, SpellIds.GatheringStormsBuff);
        }

        void CountTargets(List<WorldObject> targets)
        {
            _targetsHit = targets.Count;
        }

        void TriggerCleaveBuff()
        {
            if (_targetsHit >= 2)
                GetCaster().CastSpell(GetCaster(), SpellIds.CrashLightningCleave, true);

            AuraEffect gatheringStorms = GetCaster().GetAuraEffect(SpellIds.GatheringStorms, 0);
            if (gatheringStorms != null)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.BasePoint0, gatheringStorms.GetAmount() * _targetsHit);
                GetCaster().CastSpell(GetCaster(), SpellIds.GatheringStormsBuff, args);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(CountTargets, 0, Targets.UnitConeCasterToDestEnemy));
            AfterCast.Add(new(TriggerCleaveBuff));
        }
    }

    [Script] // 378270 - Deeply Rooted Elements
    class spell_sha_deeply_rooted_elements : AuraScript
    {
        uint requiredSpellId;
        uint ascendanceSpellId;
        int _procAttempts;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LavaBurst, SpellIds.Stormstrike, SpellIds.Riptide, SpellIds.AscendanceElemental, SpellIds.AscendanceEnhancement, SpellIds.AscendanceRestoration)
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

            if (procInfo.GetSpellInfo().Id != requiredSpellId)
                return false;

            return RandomHelper.randChance(_procAttempts++ - 2);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            _procAttempts = 0;

            Unit target = eventInfo.GetActor();

            int duration = GetEffect(0).GetAmount();
            Aura ascendanceAura = target.GetAura(ascendanceSpellId);
            if (ascendanceAura != null)
                duration += ascendanceAura.GetDuration();

            target.CastSpell(target, ascendanceSpellId,
                new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress)
                .SetTriggeringAura(aurEff)
                .SetTriggeringSpell(eventInfo.GetProcSpell())
                .AddSpellMod(SpellValueMod.Duration, duration));
        }

        public override void Register()
        {
            if (GetAura() == null || GetUnitOwner().ToPlayer().GetPrimarySpecialization() == ChrSpecialization.ShamanElemental)
            {
                requiredSpellId = SpellIds.LavaBurst;
                ascendanceSpellId = SpellIds.AscendanceElemental;
                DoCheckEffectProc.Add(new(CheckProc, 1, AuraType.Dummy));
                OnEffectProc.Add(new(HandleProc, 1, AuraType.Dummy));
            }

            if (GetAura() == null || GetUnitOwner().ToPlayer().GetPrimarySpecialization() == ChrSpecialization.ShamanEnhancement)
            {
                requiredSpellId = SpellIds.Stormstrike;
                ascendanceSpellId = SpellIds.AscendanceEnhancement;
                DoCheckEffectProc.Add(new(CheckProc, 2, AuraType.Dummy));
                OnEffectProc.Add(new(HandleProc, 2, AuraType.Dummy));
            }

            if (GetAura() == null || GetUnitOwner().ToPlayer().GetPrimarySpecialization() == ChrSpecialization.ShamanRestoration)
            {
                requiredSpellId = SpellIds.Riptide;
                ascendanceSpellId = SpellIds.AscendanceRestoration;
                DoCheckEffectProc.Add(new(CheckProc, 3, AuraType.Dummy));
                OnEffectProc.Add(new(HandleProc, 3, AuraType.Dummy));
            }
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
            var cooldown = TimeSpan.FromMilliseconds(GetSpellInfo().RecoveryTime) + TimeSpan.FromSeconds(GetEffectInfo(1).CalcValue() * _healedTargets);
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

            GetTarget().CastSpell(GetTarget(), SpellIds.EarthShieldHeal, new CastSpellExtraArgs(aurEff)
                .SetOriginalCaster(GetCasterGUID()));
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
            return ValidateSpellEffect((SpellIds.T292PElementalDamageBuff, 0));
        }

        void AddScriptedDamageMods()
        {
            AuraEffect t29 = GetCaster().GetAuraEffect(SpellIds.T292PElementalDamageBuff, 0);
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
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EarthenRagePeriodic, SpellIds.EarthenRageDamage);
        }

        bool CheckProc(ProcEventInfo procInfo)
        {
            return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().Id != SpellIds.EarthenRageDamage;
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            _procTargetGuid = eventInfo.GetProcTarget().GetGUID();
            eventInfo.GetActor().CastSpell(eventInfo.GetActor(), SpellIds.EarthenRagePeriodic, true);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
        }

        ObjectGuid _procTargetGuid;

        public ObjectGuid GetProcTargetGuid()
        {
            return _procTargetGuid;
        }
    }

    [Script] // 170377 - Earthen Rage (Proc Aura)
    class spell_sha_earthen_rage_proc_AuraScript : AuraScript
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
                    Unit procTarget = ObjAccessor.GetUnit(GetCaster(), script.GetProcTargetGuid());
                    if (procTarget != null)
                        GetTarget().CastSpell(procTarget, SpellIds.EarthenRageDamage, true);
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
    class areatrigger_sha_earthquake : AreaTriggerAI
    {
        TimeSpan _refreshTimer;
        TimeSpan _period;
        HashSet<ObjectGuid> _stunnedUnits = new();
        float _damageMultiplier;

        public areatrigger_sha_earthquake(AreaTrigger areatrigger) : base(areatrigger)
        {
            _refreshTimer = TimeSpan.FromSeconds(0);
            _period = TimeSpan.FromSeconds(1);
            _damageMultiplier = 1.0f;
        }

        public override void OnCreate(Spell creatingSpell)
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                AuraEffect earthquake = caster.GetAuraEffect(SpellIds.Earthquake, 1);
                if (earthquake != null)
                    _period = TimeSpan.FromMilliseconds(earthquake.GetPeriod());
            }

            if (creatingSpell != null)
            {
                float damageMultiplier = (float)creatingSpell.m_customArg;
                if (damageMultiplier != 0)
                    _damageMultiplier = damageMultiplier;
            }
        }

        public override void OnUpdate(uint diff)
        {
            _refreshTimer -= TimeSpan.FromMilliseconds(diff);
            while (_refreshTimer <= TimeSpan.FromSeconds(0))
            {
                Unit caster = at.GetCaster();
                if (caster != null)
                    caster.CastSpell(at.GetPosition(), SpellIds.EarthquakeTick, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                        .SetOriginalCaster(at.GetGUID())
                        .AddSpellMod(SpellValueMod.BasePoint0, (int)(caster.SpellBaseDamageBonusDone(SpellSchoolMask.Nature) * 0.213f * _damageMultiplier)));

                _refreshTimer += _period;
            }
        }

        // Each target can only be stunned once by each earthquake - keep track of who we already stunned
        public bool AddStunnedTarget(ObjectGuid guid)
        {
            return _stunnedUnits.Add(guid);
        }
    }

    [Script] // 61882 - Earthquake
    class spell_sha_earthquake : SpellScript
    {
        (uint, uint)[] DamageBuffs =
        {
            (SpellIds.EchoesOfGreatSunderingLegendary, 1),
            (SpellIds.EchoesOfGreatSunderingTalent, 0),
            (SpellIds.T292PElementalDamageBuff, 0)
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
                    var areaTrigger = areaTriggers.Find(at => at.GetGUID() == GetSpell().GetOriginalCasterGUID());
                    if (areaTrigger != null)
                    {
                        areatrigger_sha_earthquake eq = areaTrigger.GetAI<areatrigger_sha_earthquake>();
                        if (eq != null)
                            if (eq.AddStunnedTarget(target.GetGUID()))
                                GetCaster().CastSpell(target, SpellIds.EarthquakeKnockingDown, true);
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
        uint[] BuffSpells = { SpellIds.ElementalBlastCrit, SpellIds.ElementalBlastHaste, SpellIds.ElementalBlastMastery };

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ElementalBlastCrit, SpellIds.ElementalBlastHaste, SpellIds.ElementalBlastMastery, SpellIds.ElementalBlastEnergize, SpellIds.MaelstromController)
                && ValidateSpellEffect((SpellIds.MaelstromController, 10), (SpellIds.T292PElementalDamageBuff, 0));
        }

        void HandleEnergize(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, GetSpellInfo().Id == SpellIds.ElementalBlast ? 9 : 10u);
            if (energizeAmount != null)
                GetCaster().CastSpell(GetCaster(), SpellIds.ElementalBlastEnergize, new CastSpellExtraArgs(energizeAmount)
                    .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.GetAmount()));
        }

        void TriggerBuff()
        {
            Unit caster = GetCaster();
            uint spellId = BuffSpells.SelectRandomElementByWeight(buffSpellId => !caster.HasAura(buffSpellId) ? 1.0f : 0.0f);

            GetCaster().CastSpell(GetCaster(), spellId, TriggerCastFlags.FullMask);
        }

        void AddScriptedDamageMods()
        {
            AuraEffect t29 = GetCaster().GetAuraEffect(SpellIds.T292PElementalDamageBuff, 0);
            if (t29 != null)
            {
                SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), 100 + t29.GetAmount()));
                t29.GetBase().Remove();
            }
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new(HandleEnergize, 0, SpellEffectName.SchoolDamage));
            AfterCast.Add(new(TriggerBuff));
            OnHit.Add(new(AddScriptedDamageMods));
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

            player.CastSpell(targetItem, SpellIds.FlametongueWeaponEnchant, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleEffectHitTarget, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 319778  - Flametongue - SpellIds.FlametongueWeaponAura
    class spell_sha_flametongue_weapon_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FlametongueAttack);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit attacker = eventInfo.GetActor();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, Math.Max(1, (int)(attacker.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.0264f)));
            attacker.CastSpell(eventInfo.GetActionTarget(), SpellIds.FlametongueAttack, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 73920 - Healing Rain (Aura)
    class spell_sha_healing_rain_AuraScript : AuraScript
    {
        ObjectGuid _visualDummy;
        Position _dest;

        public void SetVisualDummy(TempSummon summon)
        {
            _visualDummy = summon.GetGUID();
            _dest = summon.GetPosition();
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            GetTarget().CastSpell(_dest, SpellIds.HealingRainHeal, aurEff);
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
                    var duration = TimeSpan.FromMilliseconds(GetSpellInfo().CalcDuration(GetOriginalCaster()));
                    TempSummon summon = GetCaster().GetMap().SummonCreature(NpcHealingRainInvisibleStalker, dest, null, duration, GetOriginalCaster());
                    if (summon == null)
                        return;

                    summon.CastSpell(summon, SpellIds.HealingRainVisual, true);

                    spell_sha_healing_rain_AuraScript script = aura.GetScript<spell_sha_healing_rain_AuraScript>();
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
                caster.CastSpell(caster, SpellIds.FrostShockEnergize, TriggerCastFlags.IgnoreCastInProgress);
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
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.ItemLightningShield, aurEff);
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
            GetTarget().CastSpell(GetTarget(), SpellIds.ItemLightningShieldDamage, aurEff);
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

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcSpell() != null;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            int? manaCost = eventInfo.GetProcSpell().GetPowerTypeCostAmount(PowerType.Mana);
            if (manaCost.HasValue)
            {
                int mana = MathFunctions.CalculatePct(manaCost.Value, 35);
                if (mana > 0)
                {
                    CastSpellExtraArgs args = new(aurEff);
                    args.AddSpellMod(SpellValueMod.BasePoint0, mana);
                    GetTarget().CastSpell(GetTarget(), SpellIds.ItemManaSurge, args);
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
                return;

            if (RandomHelper.randChance(chance))
                eventInfo.GetActor().CastSpell(null, spellId, true);
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
                target.GetSpellHistory().ModifyCooldown(SpellIds.ElementalMastery, TimeSpan.FromMilliseconds(-aurEff.GetAmount()));
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
                    caster.CastSpell(GetHitUnit(), SpellIds.PathOfFlamesSpread, GetSpell());
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
            GetTarget().CastSpell(GetTarget(), SpellIds.LavaSurge, true);
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
            GetCaster().GetSpellHistory().RestoreCharge(SpellMgr.GetSpellInfo(SpellIds.LavaBurst, GetCastDifficulty()).ChargeCategoryId);
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
            return ValidateSpellInfo(SpellIds.LightningBoltEnergize, SpellIds.MaelstromController)
            && ValidateSpellEffect((SpellIds.MaelstromController, 0));
        }

        void HandleScript(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 0);
            if (energizeAmount != null)
                GetCaster().CastSpell(GetCaster(), SpellIds.LightningBoltEnergize, new CastSpellExtraArgs(energizeAmount)
                    .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.GetAmount()));
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
            return ValidateSpellInfo(SpellIds.LightningBoltOverloadEnergize, SpellIds.MaelstromController)
            && ValidateSpellEffect((SpellIds.MaelstromController, 1));
        }

        void HandleScript(uint effIndex)
        {
            AuraEffect energizeAmount = GetCaster().GetAuraEffect(SpellIds.MaelstromController, 1);
            if (energizeAmount != null)
                GetCaster().CastSpell(GetCaster(), SpellIds.LightningBoltOverloadEnergize, new CastSpellExtraArgs(energizeAmount)
                    .AddSpellMod(SpellValueMod.BasePoint0, energizeAmount.GetAmount()));
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
                GetCaster().CastSpell(hitUnit, SpellIds.LiquidMagmaHit, true);
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

    [Script] // 168534 - Mastery: Elemental Overload (passive)
    class spell_sha_mastery_elemental_overload : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LightningBolt, SpellIds.LightningBoltOverload, SpellIds.ElementalBlast, SpellIds.ElementalBlastOverload, SpellIds.Icefury, SpellIds.IcefuryOverload,
            SpellIds.LavaBurst, SpellIds.LavaBurstOverload, SpellIds.ChainLightning, SpellIds.ChainLightningOverload, SpellIds.LavaBeam, SpellIds.LavaBeamOverload, SpellIds.Stormkeeper);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo == null || eventInfo.GetProcSpell() == null)
                return false;

            if (GetTriggeredSpellId(spellInfo.Id) == 0)
                return false;

            float chance = aurEff.GetAmount();   // Mastery % amount

            if (spellInfo.Id == SpellIds.ChainLightning)
                chance /= 3.0f;

            Aura stormkeeper = eventInfo.GetActor().GetAura(SpellIds.Stormkeeper);
            if (stormkeeper != null)
                if (eventInfo.GetProcSpell().m_appliedMods.Contains(stormkeeper))
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
            }, TimeSpan.FromMilliseconds(400));
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.Dummy));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }

        uint GetTriggeredSpellId(uint triggeringSpellId)
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

    [Script] // 30884 - Nature's Guardian
    class spell_sha_natures_guardian : AuraScript
    {
        bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetActionTarget().HealthBelowPct(aurEff.GetAmount());
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
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
            targets.RandomResize(target => target.GetTypeId() == TypeId.Unit && !target.ToUnit().HasAura(SpellIds.FlameShock, GetCaster().GetGUID()), 1);
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

    // 114083 - Restorative Mists
    [Script] // 294020 - Restorative Mists
    class spell_sha_restorative_mists : SpellScript
    {
        int _targetCount;

        void FilterTargets(List<WorldObject> targets)
        {
            _targetCount = targets.Count;
        }

        void HandleHeal(uint effIndex)
        {
            if (_targetCount != 0)
                SetHitHeal(GetHitHeal() / _targetCount);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaAlly));
            OnEffectHitTarget.Add(new(HandleHeal, 0, SpellEffectName.Heal));
        }
    }

    // 2645 - Ghost Wolf
    [Script] // 260878 - Spirit Wolf
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
                target.CastSpell(target, SpellIds.SpiritWolfPeriodic, aurEff);
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
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, -aurEff.GetAmount());
            args.AddSpellMod(SpellValueMod.BasePoint1, aurEff.GetAmount());

            GetTarget().CastSpell(GetTarget(), SpellIds.TidalWaves, args);
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
            return ValidateSpellInfo(SpellIds.TotemicPowerArmor, SpellIds.TotemicPowerAttackPower, SpellIds.TotemicPowerSpellPower, SpellIds.TotemicPowerMp5);
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

            caster.CastSpell(target, spellId, aurEff);
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

            // Need Remove self if Lightning Shield not active
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

            SpellInfo spellInfo = SpellMgr.GetSpellInfo(SpellIds.Electrified, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());

            Cypher.Assert(spellInfo.GetMaxTicks() > 0);
            amount /= (int)spellInfo.GetMaxTicks();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.Electrified, args);
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

            SpellInfo spellInfo = SpellMgr.GetSpellInfo(SpellIds.LavaBurstBonusDamage, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());

            Cypher.Assert(spellInfo.GetMaxTicks() > 0);
            amount /= (int)spellInfo.GetMaxTicks();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.LavaBurstBonusDamage, args);
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

            SpellInfo spellInfo = SpellMgr.GetSpellInfo(SpellIds.ChainedHeal, GetCastDifficulty());
            int amount = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());

            Cypher.Assert(spellInfo.GetMaxTicks() > 0);
            amount /= (int)spellInfo.GetMaxTicks();

            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, amount);
            caster.CastSpell(target, SpellIds.ChainedHeal, args);
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

        void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            Unit caster = procInfo.GetActor();
            Aura aura = caster.GetAura(SpellIds.UnlimitedPowerBuff);
            if (aura != null)
                aura.SetStackAmount((byte)(aura.GetStackAmount() + 1));
            else
                caster.CastSpell(caster, SpellIds.UnlimitedPowerBuff, procInfo.GetProcSpell());
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
                GetTarget().CastSpell(GetTarget(), SpellIds.UndulationProc, true);
                _castCounter = 0;
            }
        }

        public override void Register()
        {
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
                GetCaster().CastSpell(mainHand, SpellIds.WindfuryEnchantment, GetSpell());
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
            return ValidateSpellInfo(SpellIds.WindfuryAttack);
        }

        void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            for (uint i = 0; i < 2; ++i)
                eventInfo.GetActor().CastSpell(eventInfo.GetProcTarget(), SpellIds.WindfuryAttack, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleEffectProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 378269 - Windspeaker's Lava Resurgence
    class spell_sha_windspeakers_lava_resurgence : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VolcanicSurge);
        }

        void PreventLavaSurge(uint effIndex)
        {
            if (GetCaster().HasAura(SpellIds.VolcanicSurge))
                PreventHitDefaultEffect(effIndex);
        }

        void PreventVolcanicSurge(uint effIndex)
        {
            if (!GetCaster().HasAura(SpellIds.VolcanicSurge))
                PreventHitDefaultEffect(effIndex);
        }

        public override void Register()
        {
            OnEffectLaunch.Add(new(PreventLavaSurge, 1, SpellEffectName.TriggerSpell));
            OnEffectLaunch.Add(new(PreventVolcanicSurge, 2, SpellEffectName.TriggerSpell));
        }
    }

    // 192078 - Wind Rush Totem (Spell)
    [Script] //  12676 - AreaTriggerId
    class areatrigger_sha_wind_rush_totem : AreaTriggerAI
    {
        uint RefreshTime = 4500;

        uint _refreshTimer;

        public areatrigger_sha_wind_rush_totem(AreaTrigger areatrigger) : base(areatrigger)
        {
            _refreshTimer = RefreshTime;
        }

        public override void OnUpdate(uint diff)
        {
            _refreshTimer -= diff;
            if (_refreshTimer <= 0)
            {
                Unit caster = at.GetCaster();
                if (caster != null)
                {
                    foreach (ObjectGuid guid in at.GetInsideUnits())
                    {
                        Unit unit = ObjAccessor.GetUnit(caster, guid);
                        if (unit != null)
                        {
                            if (!caster.IsFriendlyTo(unit))
                                continue;

                            caster.CastSpell(unit, SpellIds.WindRush, true);
                        }
                    }
                }
                _refreshTimer += RefreshTime;
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