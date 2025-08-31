// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Miscellaneous;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using static Global;

namespace Scripts.Spells.Generic
{
    [Script]
    class spell_gen_absorb0_hitlimit1 : AuraScript
    {
        uint limit;

        public override bool Load()
        {
            // Max absorb stored in 1 dummy effect
            limit = (uint)GetSpellInfo().GetEffect(1).CalcValue();
            return true;
        }

        void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            absorbAmount = Math.Min(limit, absorbAmount);
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new(Absorb, 0));
        }
    }

    [Script] // 28764 - Adaptive Warding (Frostfire Regalia Set)
    class spell_gen_adaptive_warding : AuraScript
    {
        const uint SpellGenAdaptiveWardingFire = 28765;
        const uint SpellGenAdaptiveWardingNature = 28768;
        const uint SpellGenAdaptiveWardingFrost = 28766;
        const uint SpellGenAdaptiveWardingShadow = 28769;
        const uint SpellGenAdaptiveWardingArcane = 28770;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellGenAdaptiveWardingFire, SpellGenAdaptiveWardingNature, SpellGenAdaptiveWardingFrost, SpellGenAdaptiveWardingShadow, SpellGenAdaptiveWardingArcane);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo() == null)
                return false;

            // find Mage Armor
            if (GetTarget().GetAuraEffect(AuraType.ModManaRegenInterrupt, SpellFamilyNames.Mage, new FlagArray128(0x10000000, 0x0, 0x0)) == null)
                return false;

            return SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()) switch
            {
                SpellSchools.Normal or SpellSchools.Holy => false,
                _ => true
            };
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            uint spellId = SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()) switch
            {
                SpellSchools.Fire => SpellGenAdaptiveWardingFire,
                SpellSchools.Nature => SpellGenAdaptiveWardingNature,
                SpellSchools.Frost => SpellGenAdaptiveWardingFrost,
                SpellSchools.Shadow => SpellGenAdaptiveWardingShadow,
                SpellSchools.Arcane => SpellGenAdaptiveWardingArcane,
                _ => 0
            };

            if (spellId != 0)
                GetTarget().CastSpell(GetTarget(), spellId, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script]
    class spell_gen_allow_cast_from_item_only : SpellScript
    {
        SpellCastResult CheckRequirement()
        {
            if (GetCastItem() == null)
                return SpellCastResult.CantDoThatRightNow;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckRequirement));
        }
    }

    [Script] // 46221 - Animal Blood
    class spell_gen_animal_blood : AuraScript
    {
        const uint SpellAnimalBlood = 46221;
        const uint SpellSpawnBloodPool = 63471;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellSpawnBloodPool);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Remove all auras with spell id 46221, except the one currently being applied
            Aura aur;
            while ((aur = GetUnitOwner().GetOwnedAura(SpellAnimalBlood, ObjectGuid.Empty, ObjectGuid.Empty, 0, GetAura())) != null)
                GetUnitOwner().RemoveOwnedAura(aur);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit owner = GetUnitOwner();
            if (owner != null)
                owner.CastSpell(owner, SpellSpawnBloodPool, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(OnApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 63471 - Spawn Blood Pool
    class spell_spawn_blood_pool : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            Unit caster = GetCaster();
            Position summonPos = caster.GetPosition();
            LiquidData liquidStatus;
            if (caster.GetMap().GetLiquidStatus(caster.GetPhaseShift(), caster.GetPositionX(), caster.GetPositionY(), caster.GetPositionZ(), out liquidStatus, null, caster.GetCollisionHeight()) != ZLiquidStatus.NoWater)
                summonPos.posZ = liquidStatus.level;
            dest.Relocate(summonPos);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new(SetDest, 0, Targets.DestCaster));
        }
    }

    // 430 Drink
    // 431 Drink
    // 432 Drink
    // 1133 Drink
    // 1135 Drink
    // 1137 Drink
    // 10250 Drink
    // 22734 Drink
    // 27089 Drink
    // 34291 Drink
    // 43182 Drink
    // 43183 Drink
    // 46755 Drink
    // 49472 Drink Coffee
    // 57073 Drink
    // 61830 Drink
    [Script] // 72623 Drink
    class spell_gen_arena_drink : AuraScript
    {
        public override bool Load()
        {
            return GetCaster() != null && GetCaster().IsPlayer();
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            if (!ValidateSpellEffect((spellInfo.Id, 0)) || !spellInfo.GetEffect(0).IsAura(AuraType.ModPowerRegen))
            {
                Log.outError(LogFilter.Spells, $"Aura {GetId()} structure has been changed - first aura is no longer AuraType.ModPowerRegen");
                return false;
            }

            return true;
        }

        void CalcPeriodic(AuraEffect aurEff, ref bool isPeriodic, ref int amplitude)
        {
            // Get AuraType.ModPowerRegen aura from spell
            AuraEffect regen = GetAura().GetEffect(0);
            if (regen == null)
                return;

            // default case - not in arena
            if (!GetCaster().ToPlayer().InArena())
                isPeriodic = false;
        }

        void CalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            AuraEffect regen = GetAura().GetEffect(0);
            if (regen == null)
                return;

            // default case - not in arena
            if (!GetCaster().ToPlayer().InArena())
                regen.ChangeAmount(amount);
        }

        void UpdatePeriodic(AuraEffect aurEff)
        {
            AuraEffect regen = GetAura().GetEffect(0);
            if (regen == null)
                return;

            // **********************************************
            // This feature used only in arenas
            // **********************************************
            // Here need increase mana regen per tick (6 second rule)
            // on 0 tick -   0  (handled in 2 second)
            // on 1 tick - 166% (handled in 4 second)
            // on 2 tick - 133% (handled in 6 second)

            // Apply bonus for 1 - 4 tick
            switch (aurEff.GetTickNumber())
            {
                case 1:   // 0%
                    regen.ChangeAmount(0);
                    break;
                case 2:   // 166%
                    regen.ChangeAmount(aurEff.GetAmount() * 5 / 3);
                    break;
                case 3:   // 133%
                    regen.ChangeAmount(aurEff.GetAmount() * 4 / 3);
                    break;
                default:  // 100% - normal regen
                    regen.ChangeAmount(aurEff.GetAmount());
                    // No need to update after 4th tick
                    aurEff.SetPeriodic(false);
                    break;
            }
        }

        public override void Register()
        {
            DoEffectCalcPeriodic.Add(new(CalcPeriodic, 1, AuraType.PeriodicDummy));
            DoEffectCalcAmount.Add(new(CalcAmount, 1, AuraType.PeriodicDummy));
            OnEffectUpdatePeriodic.Add(new(UpdatePeriodic, 1, AuraType.PeriodicDummy));
        }
    }

    [Script] // 28313 - Aura of Fear
    class spell_gen_aura_of_fear : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 0)) && ValidateSpellInfo(spellInfo.GetEffect(0).TriggerSpell);
        }

        void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();
            if (!RandomHelper.randChance(GetSpellInfo().ProcChance))
                return;

            GetTarget().CastSpell(null, aurEff.GetSpellEffectInfo().TriggerSpell, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_gen_av_drekthar_presence : AuraScript
    {
        bool CheckAreaTarget(Unit target)
        {
            switch (target.GetEntry())
            {
                // alliance
                case 14762: // Dun Baldar North Marshal
                case 14763: // Dun Baldar South Marshal
                case 14764: // Icewing Marshal
                case 14765: // Stonehearth Marshal
                case 11948: // Vandar Stormspike
                            // horde
                case 14772: // East Frostwolf Warmaster
                case 14776: // Tower Point Warmaster
                case 14773: // Iceblood Warmaster
                case 14777: // West Frostwolf Warmaster
                case 11946: // Drek'thar
                    return true;
                default:
                    return false;
            }
        }

        public override void Register()
        {
            DoCheckAreaTarget.Add(new(CheckAreaTarget));
        }
    }

    [Script]
    class spell_gen_bandage : SpellScript
    {
        const uint SpellRecentlyBandaged = 11196;
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellRecentlyBandaged);
        }

        SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();
            if (target != null)
            {
                if (target.HasAura(SpellRecentlyBandaged))
                    return SpellCastResult.TargetAurastate;
            }
            return SpellCastResult.SpellCastOk;
        }

        void HandleScript()
        {
            Unit target = GetHitUnit();
            if (target != null)
                GetCaster().CastSpell(target, SpellRecentlyBandaged, true);
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
            AfterHit.Add(new(HandleScript));
        }
    }

    [Script] // 193970 - Mercenary Shapeshift
    class spell_gen_battleground_mercenary_shapeshift : AuraScript
    {
        List<uint> RacialSkills = new();

        Dictionary<Race, uint[]> RaceDisplayIds = new()
        {
            {  Race.Human , new uint[] { 55239, 55238 } },
            {  Race.Orc , new uint[] { 55257, 55256 } },
            {  Race.Dwarf , new uint[] { 55241, 55240 } },
            {  Race.NightElf , new uint[] { 55243, 55242 } },
            {  Race.Undead , new uint[] { 55259, 55258 } },
            {  Race.Tauren , new uint[] { 55261, 55260 } },
            {  Race.Gnome , new uint[] { 55245, 55244 } },
            {  Race.Troll , new uint[] { 55263, 55262 } },
            { Race.Goblin , new uint[] { 55267, 57244 } },
            {  Race.BloodElf , new uint[] { 55265, 55264 } },
            { Race.Draenei , new uint[] { 55247, 55246 } },
            {  Race.Worgen , new uint[] { 55255, 55254 } },
            {  Race.PandarenNeutral , new uint[] { 55253, 55252 } }, // not verified, might be swapped with RacePandarenHorde
            {  Race.PandarenAlliance , new uint[] { 55249, 55248 } },
            {  Race.PandarenHorde , new uint[] { 55251, 55250 } },
            {  Race.Nightborne , new uint[] { 82375, 82376 } },
            {  Race.HighmountainTauren , new uint[] { 82377, 82378 } },
            {  Race.VoidElf , new uint[] { 82371, 82372 } },
            {  Race.LightforgedDraenei , new uint[] { 82373, 82374 } },
            {  Race.ZandalariTroll , new uint[] { 88417, 88416 } },
            {  Race.KulTiran , new uint[] { 88414, 88413 } },
            {  Race.DarkIronDwarf , new uint[] { 88409, 88408 } },
            {  Race.Vulpera , new uint[] { 94999, 95001 } },
            {  Race.MagharOrc , new uint[] { 88420, 88410 } },
            {  Race.MechaGnome , new uint[] { 94998, 95000 } },
        };

        Race GetReplacementRace(Race nativeRace, Class playerClass)
        {
            CharBaseInfoRecord charBaseInfo = DB2Mgr.GetCharBaseInfo(nativeRace, playerClass);
            if (charBaseInfo != null && ObjectMgr.GetPlayerInfo((Race)charBaseInfo.OtherFactionRaceID, playerClass) != null)
                return (Race)charBaseInfo.OtherFactionRaceID;

            return Race.None;
        }

        uint GetDisplayIdForRace(Race race, Gender gender)
        {
            var displayIds = RaceDisplayIds.LookupByKey(race);
            if (!displayIds.Empty())
                return displayIds[(int)gender];

            return 0;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            foreach (var (race, displayIds) in RaceDisplayIds)
            {
                if (!CliDB.ChrRacesStorage.ContainsKey(race))
                    return false;

                foreach (uint displayId in displayIds)
                    if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(displayId))
                        return false;
            }

            RacialSkills.Clear();
            foreach (var skillLine in CliDB.SkillLineStorage.Values)
                if (skillLine.HasFlag(SkillLineFlags.RacialForThePurposeOfTemporaryRaceChange))
                    RacialSkills.Add(skillLine.Id);

            return true;
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit owner = GetUnitOwner();
            Race otherFactionRace = GetReplacementRace(owner.GetRace(), owner.GetClass());
            if (otherFactionRace == Race.None)
                return;

            uint displayId = GetDisplayIdForRace(otherFactionRace, owner.GetNativeGender());
            if (displayId != 0)
                owner.SetDisplayId(displayId);

            if (mode.HasFlag(AuraEffectHandleModes.Real))
                UpdateRacials(owner.GetRace(), otherFactionRace);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit owner = GetUnitOwner();
            Race otherFactionRace = GetReplacementRace(owner.GetRace(), owner.GetClass());
            if (otherFactionRace == Race.None)
                return;

            UpdateRacials(otherFactionRace, owner.GetRace());
        }

        void UpdateRacials(Race oldRace, Race newRace)
        {
            Player player = GetUnitOwner().ToPlayer();
            if (player == null)
                return;

            foreach (uint racialSkillId in RacialSkills)
            {
                if (DB2Mgr.GetSkillRaceClassInfo(racialSkillId, oldRace, player.GetClass()) != null)
                {
                    var skillLineAbilities = DB2Mgr.GetSkillLineAbilitiesBySkill(racialSkillId);
                    if (skillLineAbilities != null)
                        foreach (var ability in skillLineAbilities)
                            player.RemoveSpell(ability.Spell, false, false);
                }

                if (DB2Mgr.GetSkillRaceClassInfo(racialSkillId, newRace, player.GetClass()) != null)
                    player.LearnSkillRewardedSpells(racialSkillId, player.GetMaxSkillValueForLevel(), newRace);
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.SendForClientMask));
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real));
        }
    }

    [Script] // Blood Reserve - 64568
    class spell_gen_blood_reserve : AuraScript
    {
        const uint SpellGenBloodReserveAura = 64568;
        const uint SpellGenBloodReserveHeal = 64569;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellGenBloodReserveHeal);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit caster = eventInfo.GetActionTarget();
            if (caster != null)
                if (caster.HealthBelowPct(35))
                    return true;

            return false;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit caster = eventInfo.GetActionTarget();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
            caster.CastSpell(caster, SpellGenBloodReserveHeal, args);
            caster.RemoveAura(SpellGenBloodReserveAura);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script]
    class spell_gen_bonked : SpellScript
    {
        const uint SpellBonked = 62991;
        const uint SpellFoamSwordDefeat = 62994;
        const uint SpellOnGuard = 62972;

        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target != null)
            {
                Aura aura = GetHitAura();
                if (!(aura != null && aura.GetStackAmount() == 3))
                    return;

                target.CastSpell(target, SpellFoamSwordDefeat, true);
                target.RemoveAurasDueToSpell(SpellBonked);

                Aura auraOnGuard = target.GetAura(SpellOnGuard);
                if (auraOnGuard != null)
                {
                    Item item = target.GetItemByGuid(auraOnGuard.GetCastItemGUID());
                    if (item != null)
                        target.DestroyItemCount(item.GetEntry(), 1, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script("spell_gen_break_shield")]
    [Script("spell_gen_tournament_counterattack")]
    class spell_gen_break_shield : SpellScript
    {
        const uint SpellBreakShieldDamage2K = 62626;
        const uint SpellBreakShieldDamage10K = 64590;

        const uint SpellBreakShieldTriggerFactionMounts = 62575; // Also on ToC5 mounts
        const uint SpellBreakShieldTriggerCampaingWarhorse = 64595;
        const uint SpellBreakShieldTriggerUnk = 66480;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(62552, 62719, 64100, 66482);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Unit target = GetHitUnit();

            switch (effIndex)
            {
                case 0: // On spells wich trigger the damaging spell (and also the visual)
                {
                    uint spellId;

                    switch (GetSpellInfo().Id)
                    {
                        case SpellBreakShieldTriggerUnk:
                        case SpellBreakShieldTriggerCampaingWarhorse:
                            spellId = SpellBreakShieldDamage10K;
                            break;
                        case SpellBreakShieldTriggerFactionMounts:
                            spellId = SpellBreakShieldDamage2K;
                            break;
                        default:
                            return;
                    }

                    Unit rider = GetCaster().GetCharmer();
                    if (rider != null)
                        rider.CastSpell(target, spellId, false);
                    else
                        GetCaster().CastSpell(target, spellId, false);
                    break;
                }
                case 1: // On damaging spells, for removing a defend layer
                {
                    var auras = target.GetAppliedAuras();
                    foreach (var pair in auras)
                    {
                        Aura aura = pair.Value.GetBase();
                        if (aura != null)
                        {
                            if (aura.GetId() == 62552 || aura.GetId() == 62719 || aura.GetId() == 64100 || aura.GetId() == 66482)
                            {
                                aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                // Remove dummys from rider (Necessary for updating visual shields)
                                Unit rider = target.GetCharmer();
                                if (rider != null)
                                {
                                    Aura defend = rider.GetAura(aura.GetId());
                                    if (defend != null)
                                        defend.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                }
                                break;
                            }
                        }
                    }
                    break;
                }
                default:
                    break;
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 48750 - Burning Depths Necrolyte Image
    class spell_gen_burning_depths_necrolyte_image : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 2)) && ValidateSpellInfo((uint)(spellInfo.GetEffect(2).CalcValue()));
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(GetTarget(), (uint)GetEffectInfo(2).CalcValue());
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell((uint)GetEffectInfo(2).CalcValue(), GetCasterGUID());
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_cannibalize : SpellScript
    {
        const uint SpellCannibalizeTriggered = 20578;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellCannibalizeTriggered);
        }

        SpellCastResult CheckIfCorpseNear()
        {
            Unit caster = GetCaster();
            float max_range = GetSpellInfo().GetMaxRange(false);
            // search for nearby enemy corpse in range
            AnyDeadUnitSpellTargetInRangeCheck<WorldObject> check = new(caster, max_range, GetSpellInfo(), SpellTargetCheckTypes.Enemy, SpellTargetObjectTypes.CorpseEnemy);
            WorldObjectSearcher searcher = new(caster, check);
            Cell.VisitWorldObjects(caster, searcher, max_range);
            if (searcher.GetResult() == null)
                Cell.VisitGridObjects(caster, searcher, max_range);
            if (searcher.GetResult() == null)
                return SpellCastResult.NoEdibleCorpses;
            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellCannibalizeTriggered, false);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
            OnCheckCast.Add(new(CheckIfCorpseNear));
        }
    }

    [Script] // 66020 Chains of Ice
    class spell_gen_chains_of_ice : AuraScript
    {
        void UpdatePeriodic(AuraEffect aurEff)
        {
            // Get 0 effect aura
            AuraEffect slow = GetAura().GetEffect(0);
            if (slow == null)
                return;

            int newAmount = Math.Min(slow.GetAmount() + aurEff.GetAmount(), 0);
            slow.ChangeAmount(newAmount);
        }

        public override void Register()
        {
            OnEffectUpdatePeriodic.Add(new(UpdatePeriodic, 1, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_gen_chaos_blast : SpellScript
    {
        const uint SpellChaosBlast = 37675;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellChaosBlast);
        }

        void HandleDummy(uint effIndex)
        {
            int basepoints0 = 100;
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target != null)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.BasePoint0, basepoints0);
                caster.CastSpell(target, SpellChaosBlast, args);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 28471 - ClearAll
    class spell_clear_all : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.RemoveAllAurasOnDeath();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_clone : SpellScript
    {
        const uint SpellNightmareFigmentMirrorImage = 57528;

        void HandleScriptEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            if (m_scriptSpellId == SpellNightmareFigmentMirrorImage)
            {
                OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.Dummy));
                OnEffectHitTarget.Add(new(HandleScriptEffect, 2, SpellEffectName.Dummy));
            }
            else
            {
                OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
                OnEffectHitTarget.Add(new(HandleScriptEffect, 2, SpellEffectName.ScriptEffect));
            }
        }
    }

    [Script]
    class spell_gen_clone_weapon : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_clone_weapon_AuraScript : AuraScript
    {
        const uint SpellCopyWeaponAura = 41054;
        const uint SpellCopyWeapon2Aura = 63418;
        const uint SpellCopyWeapon3Aura = 69893;

        const uint SpellCopyOffhandAura = 45205;
        const uint SpellCopyOffhand2Aura = 69896;

        const uint SpellCopyRangedAura = 57594;

        uint prevItem;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellCopyWeaponAura, SpellCopyWeapon2Aura, SpellCopyWeapon3Aura, SpellCopyOffhandAura, SpellCopyOffhand2Aura, SpellCopyRangedAura);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            Unit target = GetTarget();
            if (caster == null)
                return;

            switch (GetSpellInfo().Id)
            {
                case SpellCopyWeaponAura:
                case SpellCopyWeapon2Aura:
                case SpellCopyWeapon3Aura:
                {
                    prevItem = target.GetVirtualItemId(0);

                    Player player = caster.ToPlayer();
                    if (player != null)
                    {
                        Item mainItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                        if (mainItem != null)
                            target.SetVirtualItem(0, mainItem.GetEntry());
                    }
                    else
                        target.SetVirtualItem(0, caster.GetVirtualItemId(0));
                    break;
                }
                case SpellCopyOffhandAura:
                case SpellCopyOffhand2Aura:
                {
                    prevItem = target.GetVirtualItemId(1);

                    Player player = caster.ToPlayer();
                    if (player != null)
                    {
                        Item offItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                        if (offItem != null)
                            target.SetVirtualItem(1, offItem.GetEntry());
                    }
                    else
                        target.SetVirtualItem(1, caster.GetVirtualItemId(1));
                    break;
                }
                case SpellCopyRangedAura:
                {
                    prevItem = target.GetVirtualItemId(2);

                    Player player = caster.ToPlayer();
                    if (player != null)
                    {
                        Item rangedItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                        if (rangedItem != null)
                            target.SetVirtualItem(2, rangedItem.GetEntry());
                    }
                    else
                        target.SetVirtualItem(2, caster.GetVirtualItemId(2));
                    break;
                }
                default:
                    break;
            }
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();

            switch (GetSpellInfo().Id)
            {
                case SpellCopyWeaponAura:
                case SpellCopyWeapon2Aura:
                case SpellCopyWeapon3Aura:
                    target.SetVirtualItem(0, prevItem);
                    break;
                case SpellCopyOffhandAura:
                case SpellCopyOffhand2Aura:
                    target.SetVirtualItem(1, prevItem);
                    break;
                case SpellCopyRangedAura:
                    target.SetVirtualItem(2, prevItem);
                    break;
                default:
                    break;
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script("spell_gen_default_count_pct_from_max_hp", 0)]
    [Script("spell_gen_50pct_count_pct_from_max_hp", 50)]
    class spell_gen_count_pct_from_max_hp : SpellScript
    {
        int _damagePct;

        public spell_gen_count_pct_from_max_hp(int damagePct)
        {
            _damagePct = damagePct;
        }

        void RecalculateDamage()
        {
            if (_damagePct == 0)
                _damagePct = GetHitDamage();

            SetHitDamage((int)GetHitUnit().CountPctFromMaxHealth(_damagePct));
        }

        public override void Register()
        {
            OnHit.Add(new(RecalculateDamage));
        }
    }

    // 28865 - Consumption
    [Script] // 64208 - Consumption
    class spell_gen_consumption : SpellScript
    {
        void CalculateDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
        {
            SpellInfo createdBySpell = SpellMgr.GetSpellInfo(GetCaster().m_unitData.CreatedBySpell, GetCastDifficulty());
            if (createdBySpell != null)
                damage = createdBySpell.GetEffect(1).CalcValue();
        }

        public override void Register()
        {
            CalcDamage.Add(new(CalculateDamage));
        }
    }

    [Script] // 63845 - Create Lance
    class spell_gen_create_lance : SpellScript
    {
        const uint SpellCreateLanceAlliance = 63914;
        const uint SpellCreateLanceHorde = 63919;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellCreateLanceAlliance, SpellCreateLanceHorde);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Player target = GetHitPlayer();
            if (target != null)
            {
                if (target.GetTeam() == Team.Alliance)
                    GetCaster().CastSpell(target, SpellCreateLanceAlliance, true);
                else if (target.GetTeam() == Team.Horde)
                    GetCaster().CastSpell(target, SpellCreateLanceHorde, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script("spell_gen_sunreaver_disguise")]
    [Script("spell_gen_silver_covenant_disguise")]
    class spell_gen_dalaran_disguise : SpellScript
    {
        const uint SpellSunreaverDisguiseTrigger = 69672;
        const uint SpellSunreaverDisguiseFemale = 70973;
        const uint SpellSunreaverDisguiseMale = 70974;

        const uint SpellSilverCovenantDisguiseTrigger = 69673;
        const uint SpellSilverCovenantDisguiseFemale = 70971;
        const uint SpellSilverCovenantDisguiseMale = 70972;

        public override bool Validate(SpellInfo spellInfo)
        {
            switch (spellInfo.Id)
            {
                case SpellSunreaverDisguiseTrigger:
                    return ValidateSpellInfo(SpellSunreaverDisguiseFemale, SpellSunreaverDisguiseMale);
                case SpellSilverCovenantDisguiseTrigger:
                    return ValidateSpellInfo(SpellSilverCovenantDisguiseFemale, SpellSilverCovenantDisguiseMale);
                default:
                    break;
            }

            return false;
        }

        void HandleScript(uint effIndex)
        {
            Player player = GetHitPlayer();
            if (player != null)
            {
                Gender gender = player.GetNativeGender();

                uint spellId = GetSpellInfo().Id;

                switch (spellId)
                {
                    case SpellSunreaverDisguiseTrigger:
                        spellId = gender == Gender.Female ? SpellSunreaverDisguiseFemale : SpellSunreaverDisguiseMale;
                        break;
                    case SpellSilverCovenantDisguiseTrigger:
                        spellId = gender == Gender.Female ? SpellSilverCovenantDisguiseFemale : SpellSilverCovenantDisguiseMale;
                        break;
                    default:
                        break;
                }

                GetCaster().CastSpell(player, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_decay_over_time_spell : SpellScript
    {
        void ModAuraStack()
        {
            Aura aur = GetHitAura();
            if (aur != null)
                aur.SetStackAmount((byte)GetSpellInfo().StackAmount);
        }

        public override void Register()
        {
            AfterHit.Add(new(ModAuraStack));
        }
    }

    [Script] // 32065 - Fungal Decay
    class spell_gen_decay_over_time_fungal_decay : AuraScript
    {
        // found in sniffs, there is no duration entry we can possibly use
        const int AuraDuration = 12600;

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() == GetSpellInfo();
        }

        void Decay(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            ModStackAmount(-1);
        }

        void ModDuration(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // only on actual reapply, not on stack decay
            if (GetDuration() == GetMaxDuration())
            {
                SetMaxDuration(AuraDuration);
                SetDuration(AuraDuration);
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnProc.Add(new(Decay));
            OnEffectApply.Add(new(ModDuration, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    // 36659 - Tail Sting
    [Script] // 36659 - Tail Sting
    class spell_gen_decay_over_time_tail_sting : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() == GetSpellInfo();
        }

        void Decay(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            ModStackAmount(-1);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnProc.Add(new(Decay));
        }
    }

    [Script]
    class spell_gen_defend : AuraScript
    {
        const uint SpellVisualShield1 = 63130;
        const uint SpellVisualShield2 = 63131;
        const uint SpellVisualShield3 = 63132;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellVisualShield1, SpellVisualShield2, SpellVisualShield3);
        }

        void RefreshVisualShields(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetCaster() != null)
            {
                Unit target = GetTarget();

                for (byte i = 0; i < GetSpellInfo().StackAmount; ++i)
                    target.RemoveAurasDueToSpell(SpellVisualShield1 + i);

                target.CastSpell(target, SpellVisualShield1 + GetAura().GetStackAmount() - 1, aurEff);
            }
            else
                GetTarget().RemoveAurasDueToSpell(GetId());
        }

        void RemoveVisualShields(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            for (byte i = 0; i < GetSpellInfo().StackAmount; ++i)
                GetTarget().RemoveAurasDueToSpell(SpellVisualShield1 + i);
        }

        void RemoveDummyFromDriver(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                TempSummon vehicle = caster.ToTempSummon();
                if (vehicle != null)
                {
                    Unit rider = vehicle.GetSummonerUnit();
                    if (rider != null)
                        rider.RemoveAurasDueToSpell(GetId());
                }
            }
        }

        public override void Register()
        {
            /*SpellInfo spell = SpellMgr.GetSpellInfo(m_scriptSpellId, Difficulty.None);

            // 6.x effects Removed

            // Defend spells cast by NPCs (add visuals)
            if (spell.GetEffect(0).ApplyAuraName == AuraType.ModDamagePercentTaken)
            {
                AfterEffectApply.Add(new(RefreshVisualShields, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.RealOrReapplyMask));
                OnEffectRemove.Add(new(RemoveVisualShields, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.ChangeAmountMask));
            }

            // Remove Defend spell from player when he dismounts
            if (spell.GetEffect(2).ApplyAuraName == AuraType.ModDamagePercentTaken)
                OnEffectRemove.Add(new(RemoveDummyFromDriver, 2, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));

            // Defend spells cast by players (add/Remove visuals)
            if (spell.GetEffect(1).ApplyAuraName == AuraType.Dummy)
            {
                AfterEffectApply.Add(new(RefreshVisualShields, 1, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
                OnEffectRemove.Add(new(RemoveVisualShields, 1, AuraType.Dummy, AuraEffectHandleModes.ChangeAmountMask));
            }*/
        }
    }

    [Script]
    class spell_gen_despawn_AuraScript : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature target = GetTarget().ToCreature();
            if (target != null)
                target.DespawnOrUnsummon();
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, SpellConst.EffectFirstFound, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] /// @todo: migrate spells to spell_gen_despawn_target, then Remove this
    class spell_gen_despawn_self : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Unit;
        }

        void HandleDummy(uint effIndex)
        {
            if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) || GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
                GetCaster().ToCreature().DespawnOrUnsummon();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, SpellConst.EffectAll, SpellEffectName.Any));
        }
    }

    [Script]
    class spell_gen_despawn_target : SpellScript
    {
        void HandleDespawn(uint effIndex)
        {
            if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) || GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
            {
                Creature target = GetHitCreature();
                if (target != null)
                    target.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDespawn, SpellConst.EffectAll, SpellEffectName.Any));
        }
    }

    [Script] // 70769 Divine Storm!
    class spell_gen_divine_storm_cd_reset : SpellScript
    {
        const uint SpellDivineStorm = 53385;

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellDivineStorm);
        }

        void HandleScript(uint effIndex)
        {
            GetCaster().GetSpellHistory().ResetCooldown(SpellDivineStorm, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_ds_flush_knockback : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            // Here the target is the water spout and determines the position where the player is knocked from
            Unit target = GetHitUnit();
            if (target != null)
            {
                Player player = GetCaster().ToPlayer();
                if (player != null)
                {
                    float horizontalSpeed = 20.0f + (40.0f - GetCaster().GetDistance(target));
                    float verticalSpeed = 8.0f;
                    // This method relies on the Dalaran Sewer map disposition and Water Spout position
                    // What we do is knock the player from a position exactly behind him and at the end of the pipe
                    player.KnockbackFrom(target.GetPosition(), horizontalSpeed, verticalSpeed);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 50051 - Ethereal Pet Aura
    class spell_ethereal_pet_AuraScript : AuraScript
    {
        const uint NpcEtherealSoulTrader = 27914;

        const uint SayStealEssence = 1;

        const uint SpellStealEssenceVisual = 50101;

        bool CheckProc(ProcEventInfo eventInfo)
        {
            uint levelDiff = (uint)Math.Abs(GetTarget().GetLevel() - eventInfo.GetProcTarget().GetLevel());
            return levelDiff <= 9;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            List<TempSummon> minionList = new();
            GetUnitOwner().GetAllMinionsByEntry(minionList, NpcEtherealSoulTrader);
            foreach (Creature minion in minionList)
            {
                if (minion.IsAIEnabled())
                {
                    minion.GetAI().Talk(SayStealEssence);
                    minion.CastSpell(eventInfo.GetProcTarget(), SpellStealEssenceVisual);
                }
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 50052 - Ethereal Pet onSummon
    class spell_ethereal_pet_onsummon : SpellScript
    {
        const uint SpellProcTriggerOnKillAura = 50051;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellProcTriggerOnKillAura);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Unit target = GetHitUnit();
            target.CastSpell(target, SpellProcTriggerOnKillAura, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 50055 - Ethereal Pet Aura Remove
    class spell_ethereal_pet_aura_Remove : SpellScript
    {
        const uint SpellEtherealPetAura = 50055;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellEtherealPetAura);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell(SpellEtherealPetAura);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 50101 - Ethereal Pet OnKill Steal Essence
    class spell_steal_essence_visual : AuraScript
    {
        const uint SpellCreateToken = 50063;
        const uint SayCreateToken = 2;

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.CastSpell(caster, SpellCreateToken, true);
                Creature soulTrader = caster.ToCreature();
                if (soulTrader != null)
                    soulTrader.GetAI().Talk(SayCreateToken);
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    struct FeastSpellIds
    {
        public const uint GreatFeast = 57337;
        public const uint FishFeast = 57397;
        public const uint GiganticFeast = 58466;
        public const uint SmallFeast = 58475;
        public const uint BountifulFeast = 66477;

        public const uint FeastFood = 45548;
        public const uint FeastDrink = 57073;
        public const uint BountifulFeastDrink = 66041;
        public const uint BountifulFeastFood = 66478;

        public const uint GreatFeastRefreshment = 57338;
        public const uint FishFeastRefreshment = 57398;
        public const uint GiganticFeastRefreshment = 58467;
        public const uint SmallFeastRefreshment = 58477;
        public const uint BountifulFeastRefreshment = 66622;
    }

    //57397 - Fish Feast
    //58466 - Gigantic Feast
    //58475 - Small Feast
    [Script] //66477 - Bountiful Feast
    class spell_gen_feast : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(FeastSpellIds.FeastFood, FeastSpellIds.FeastDrink, FeastSpellIds.BountifulFeastDrink, FeastSpellIds.BountifulFeastFood, FeastSpellIds.GreatFeastRefreshment, FeastSpellIds.FishFeastRefreshment,
                FeastSpellIds.GiganticFeastRefreshment, FeastSpellIds.SmallFeastRefreshment, FeastSpellIds.BountifulFeastRefreshment);
        }

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();

            switch (GetSpellInfo().Id)
            {
                case FeastSpellIds.GreatFeast:
                    target.CastSpell(target, FeastSpellIds.FeastFood);
                    target.CastSpell(target, FeastSpellIds.FeastDrink);
                    target.CastSpell(target, FeastSpellIds.GreatFeastRefreshment);
                    break;
                case FeastSpellIds.FishFeast:
                    target.CastSpell(target, FeastSpellIds.FeastFood);
                    target.CastSpell(target, FeastSpellIds.FeastDrink);
                    target.CastSpell(target, FeastSpellIds.FishFeastRefreshment);
                    break;
                case FeastSpellIds.GiganticFeast:
                    target.CastSpell(target, FeastSpellIds.FeastFood);
                    target.CastSpell(target, FeastSpellIds.FeastDrink);
                    target.CastSpell(target, FeastSpellIds.GiganticFeastRefreshment);
                    break;
                case FeastSpellIds.SmallFeast:
                    target.CastSpell(target, FeastSpellIds.FeastFood);
                    target.CastSpell(target, FeastSpellIds.FeastDrink);
                    target.CastSpell(target, FeastSpellIds.SmallFeastRefreshment);
                    break;
                case FeastSpellIds.BountifulFeast:
                    target.CastSpell(target, FeastSpellIds.BountifulFeastRefreshment);
                    target.CastSpell(target, FeastSpellIds.BountifulFeastDrink);
                    target.CastSpell(target, FeastSpellIds.BountifulFeastFood);
                    break;
                default:
                    break;
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_feign_death_all_flags : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.InitializeReactState();
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_feign_death_all_flags_uninteractible : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
            target.SetImmuneToAll(true);
            target.SetUninteractible(true);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
            target.SetImmuneToAll(false);
            target.SetUninteractible(false);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.InitializeReactState();
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 96733 - Permanent Feign Death (Stun)
    class spell_gen_feign_death_all_flags_no_uninteractible : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
            target.SetImmuneToAll(true);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
            target.SetImmuneToAll(false);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.InitializeReactState();
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 35357 - Spawn Feign Death
    [Script] // 51329 - Feign Death
    class spell_gen_feign_death_no_dyn_flag : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag2(UnitFlags2.FeignDeath);
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.InitializeReactState();
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 58951 - Permanent Feign Death
    class spell_gen_feign_death_no_prevent_emotes : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.InitializeReactState();
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 35491 - Furious Rage
    class spell_gen_furious_rage : AuraScript
    {
        const uint EmoteFuriousRage = 19415;
        const uint EmoteExhausted = 18368;
        const uint SpellExhaustion = 35492;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellExhaustion) &&
                CliDB.BroadcastTextStorage.HasRecord(EmoteFuriousRage) &&
               CliDB.BroadcastTextStorage.HasRecord(EmoteExhausted);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.TextEmote(EmoteFuriousRage, target, false);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit target = GetTarget();
            target.TextEmote(EmoteExhausted, target, false);
            target.CastSpell(target, SpellExhaustion, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(AfterApply, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 46642 - 5,000 Gold
    class spell_gen_5000_gold : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target != null)
                target.ModifyMoney(5000 * MoneyConstants.Gold);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 131474 - Fishing
    class spell_gen_fishing : SpellScript
    {
        const uint SpellFishingNoFishingPole = 131476;
        const uint SpellFishingWithPole = 131490;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellFishingNoFishingPole, SpellFishingWithPole);
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            uint spellId;
            Item mainHand = GetCaster().ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
            if (mainHand == null || mainHand.GetTemplate().GetClass() != ItemClass.Weapon || mainHand.GetTemplate().GetSubClass() != (uint)ItemSubClassWeapon.FishingPole)
                spellId = SpellFishingNoFishingPole;
            else
                spellId = SpellFishingWithPole;

            GetCaster().CastSpell(GetCaster(), spellId, false);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_gadgetzan_transporter_backfire : SpellScript
    {
        const uint SpellTransporterMalfunctionPolymorph = 23444;
        const uint SpellTransporterEvilTwin = 23445;
        const uint SpellTransporterMalfunctionMiss = 36902;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellTransporterMalfunctionPolymorph, SpellTransporterEvilTwin, SpellTransporterMalfunctionMiss);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            int r = RandomHelper.IRand(0, 119);
            if (r < 20)                           // Transporter Malfunction - 1/6 polymorph
                caster.CastSpell(caster, SpellTransporterMalfunctionPolymorph, true);
            else if (r < 100)                     // Evil Twin               - 4/6 evil twin
                caster.CastSpell(caster, SpellTransporterEvilTwin, true);
            else                                    // Transporter Malfunction - 1/6 miss the target
                caster.CastSpell(caster, SpellTransporterMalfunctionMiss, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 28880 - Warrior
    // 59542 - Paladin
    // 59543 - Hunter
    // 59544 - Priest
    // 59545 - Death Knight
    // 59547 - Shaman
    // 59548 - Mage
    [Script] // 121093 - Monk
    class spell_gen_gift_of_naaru : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1));
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || aurEff.GetTotalTicks() == 0)
                return;

            float healPct = GetEffectInfo(1).CalcValue() / 100.0f;
            float heal = healPct * GetCaster().GetMaxHealth();
            int healTick = (int)Math.Floor(heal / aurEff.GetTotalTicks());
            amount += healTick;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.PeriodicHeal));
        }
    }

    [Script]
    class spell_gen_gnomish_transporter : SpellScript
    {
        const uint SpellTransporterSuccess = 23441;
        const uint SpellTransporterFailure = 23446;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellTransporterSuccess, SpellTransporterFailure);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), RandomHelper.randChance(50) ? SpellTransporterSuccess : SpellTransporterFailure, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 69641 - Gryphon/Wyvern Pet - Mounting Check Aura
    class spell_gen_gryphon_wyvern_mount_check : AuraScript
    {
        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            Unit owner = target.GetOwner();

            if (owner == null)
                return;

            if (owner.IsMounted())
                target.SetDisableGravity(true);
            else
                target.SetDisableGravity(false);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    //20538 - Hate to Zero(AoE)
    //26569 - Hate to Zero(AoE)
    //26637 - Hate to Zero(AoE, Unique)
    //37326 - Hate to Zero(AoE)
    //40410 - Hate to Zero(Should be added, AoE)
    //40467 - Hate to Zero(Should be added, AoE)
    [Script] //41582 - Hate to Zero(Should be added, Melee)
    class spell_gen_hate_to_zero : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            if (GetCaster().CanHaveThreatList())
                GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -100);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // This spell is used by both player and creature, but currently works only if used by player
    [Script] // 63984 - Hate to Zero
    class spell_gen_hate_to_zero_caster_target : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
                if (target.CanHaveThreatList())
                    target.GetThreatManager().ModifyThreatByPercent(GetCaster(), -100);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 19707 - Hate to 50%
    class spell_gen_hate_to_50 : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            if (GetCaster().CanHaveThreatList())
                GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -50);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 26886 - Hate to 75%
    class spell_gen_hate_to_75 : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            if (GetCaster().CanHaveThreatList())
                GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -25);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 32748 - Deadly Throw Interrupt
    [Script] // 44835 - Maim Interrupt
    class spell_gen_interrupt : AuraScript
    {
        const uint SpellGenThrowInterrupt = 32747;
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellGenThrowInterrupt);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellGenThrowInterrupt, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script("spell_pal_blessing_of_kings")]
    [Script("spell_pal_blessing_of_might")]
    [Script("spell_dru_mark_of_the_wild")]
    [Script("spell_pri_power_word_fortitude")]
    [Script("spell_pri_shadow_protection")]
    class spell_gen_increase_stats_buff : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            if (GetHitUnit().IsInRaidWith(GetCaster()))
                GetCaster().CastSpell(GetCaster(), (uint)(GetEffectValue() + 1), true); // raid buff
            else
                GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true); // single-target buff
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script("spell_hexlord_lifebloom", SpellHexlordMalacrassLifebloomFinalHeal)]
    [Script("spell_tur_ragepaw_lifebloom", SpellTurRagepawLifebloomFinalHeal)]
    [Script("spell_cenarion_scout_lifebloom", SpellCenarionScoutLifebloomFinalHeal)]
    [Script("spell_twisted_visage_lifebloom", SpellTwistedVisageLifebloomFinalHeal)]
    [Script("spell_faction_champion_dru_lifebloom", SpellFactionChapionsDruLifebloomFinalHeal)]
    class spell_gen_lifebloom : AuraScript
    {
        const uint SpellHexlordMalacrassLifebloomFinalHeal = 43422;
        const uint SpellTurRagepawLifebloomFinalHeal = 52552;
        const uint SpellCenarionScoutLifebloomFinalHeal = 53692;
        const uint SpellTwistedVisageLifebloomFinalHeal = 57763;
        const uint SpellFactionChapionsDruLifebloomFinalHeal = 66094;

        uint _spellId;

        public spell_gen_lifebloom(uint spellId)
        {
            _spellId = spellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_spellId);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // final heal only on duration end or dispel
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire && GetTargetApplication().GetRemoveMode() != AuraRemoveMode.EnemySpell)
                return;

            // final heal
            GetTarget().CastSpell(GetTarget(), _spellId, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_mounted_charge : SpellScript
    {
        const uint SpellChargeDamage8K5 = 62874;
        const uint SpellChargeDamage20K = 68498;
        const uint SpellChargeDamage45K = 64591;

        const uint SpellChargeChargingEffect8K5 = 63661;
        const uint SpellChargeCharging20K1 = 68284;
        const uint SpellChargeCharging20K2 = 68501;
        const uint SpellChargeCharging45K1 = 62563;
        const uint SpellChargeCharging45K2 = 66481;

        const uint SpellChargeTriggerFactionMounts = 62960;
        const uint SpellChargeTriggerTrialChapion = 68282;

        const uint SpellChargeMissEffect = 62977;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(62552, 62719, 64100, 66482);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Unit target = GetHitUnit();

            switch (effIndex)
            {
                case 0: // On spells wich trigger the damaging spell (and also the visual)
                {
                    uint spellId;

                    switch (GetSpellInfo().Id)
                    {
                        case SpellChargeTriggerTrialChapion:
                            spellId = SpellChargeCharging20K1;
                            break;
                        case SpellChargeTriggerFactionMounts:
                            spellId = SpellChargeChargingEffect8K5;
                            break;
                        default:
                            return;
                    }

                    // If target isn't a training dummy there's a chance of failing the charge
                    if (!target.IsCharmedOwnedByPlayerOrPlayer() && RandomHelper.randChance(12.5f))
                        spellId = SpellChargeMissEffect;

                    Unit vehicle = GetCaster().GetVehicleBase();
                    if (vehicle != null)
                        vehicle.CastSpell(target, spellId, false);
                    else
                        GetCaster().CastSpell(target, spellId, false);
                    break;
                }
                case 1: // On damaging spells, for removing a defend layer
                case 2:
                {
                    var auras = target.GetAppliedAuras();
                    foreach (var pair in auras)
                    {
                        Aura aura = pair.Value.GetBase();
                        if (aura != null)
                        {
                            if (aura.GetId() == 62552 || aura.GetId() == 62719 || aura.GetId() == 64100 || aura.GetId() == 66482)
                            {
                                aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                // Remove dummys from rider (Necessary for updating visual shields)
                                Unit rider = target.GetCharmer();
                                if (rider != null)
                                {
                                    Aura defend = rider.GetAura(aura.GetId());
                                    if (defend != null)
                                        defend.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                }
                                break;
                            }
                        }
                    }
                    break;
                }
                default:
                    break;
            }
        }

        void HandleChargeEffect(uint effIndex)
        {
            uint spellId;

            switch (GetSpellInfo().Id)
            {
                case SpellChargeChargingEffect8K5:
                    spellId = SpellChargeDamage8K5;
                    break;
                case SpellChargeCharging20K1:
                case SpellChargeCharging20K2:
                    spellId = SpellChargeDamage20K;
                    break;
                case SpellChargeCharging45K1:
                case SpellChargeCharging45K2:
                    spellId = SpellChargeDamage45K;
                    break;
                default:
                    return;
            }

            Unit rider = GetCaster().GetCharmer();
            if (rider != null)
                rider.CastSpell(GetHitUnit(), spellId, false);
            else
                GetCaster().CastSpell(GetHitUnit(), spellId, false);
        }

        public override void Register()
        {
            SpellInfo spell = SpellMgr.GetSpellInfo(m_scriptSpellId, Difficulty.None);

            if (spell.HasEffect(SpellEffectName.ScriptEffect))
                OnEffectHitTarget.Add(new(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect));

            if (spell.GetEffect(0).IsEffect(SpellEffectName.Charge))
                OnEffectHitTarget.Add(new(HandleChargeEffect, 0, SpellEffectName.Charge));
        }
    }

    // 6870 Moss Covered Feet
    [Script] // 31399 Moss Covered Feet
    class spell_gen_moss_covered_feet : AuraScript
    {
        const uint SpellFallDown = 6869;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellFallDown);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActionTarget().CastSpell(null, SpellFallDown, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 28702 - Netherbloom
    class spell_gen_netherbloom : SpellScript
    {
        const uint SpellNetherbloomPollen1 = 28703;

        public override bool Validate(SpellInfo spellInfo)
        {
            for (byte i = 0; i < 5; ++i)
                if (!ValidateSpellInfo(SpellNetherbloomPollen1 + i))
                    return false;

            return true;
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Unit target = GetHitUnit();
            if (target != null)
            {
                // 25% chance of casting a random buff
                if (RandomHelper.randChance(75))
                    return;

                // triggered spells are 28703 to 28707
                // Note: some sources say, that there was the possibility of
                //       receiving a debuff. However, this seems to be Removed by a patch.

                // don't overwrite an existing aura
                for (byte i = 0; i < 5; ++i)
                    if (target.HasAura(SpellNetherbloomPollen1 + i))
                        return;

                target.CastSpell(target, SpellNetherbloomPollen1 + RandomHelper.URand(0, 4), true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 28720 - Nightmare Vine
    class spell_gen_nightmare_vine : SpellScript
    {
        const uint SpellNightmarePollen = 28721;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellNightmarePollen);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Unit target = GetHitUnit();
            if (target != null)
            {
                // 25% chance of casting Nightmare Pollen
                if (RandomHelper.randChance(25))
                    target.CastSpell(target, SpellNightmarePollen, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 27746 -  Nitrous Boost
    class spell_gen_nitrous_boost : AuraScript
    {
        void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();

            if (GetCaster() != null && GetTarget().GetPower(PowerType.Mana) >= 10)
                GetTarget().ModifyPower(PowerType.Mana, -10);
            else
                Remove();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 27539 - Obsidian Armor
    class spell_gen_obsidian_armor : AuraScript
    {
        const uint SpellGenObsidianArmorHoly = 27536;
        const uint SpellGenObsidianArmorFire = 27533;
        const uint SpellGenObsidianArmorNature = 27538;
        const uint SpellGenObsidianArmorFrost = 27534;
        const uint SpellGenObsidianArmorShadow = 27535;
        const uint SpellGenObsidianArmorArcane = 27540;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellGenObsidianArmorHoly, SpellGenObsidianArmorFire, SpellGenObsidianArmorNature, SpellGenObsidianArmorFrost, SpellGenObsidianArmorShadow, SpellGenObsidianArmorArcane);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo() == null)
                return false;

            if (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()) == SpellSchools.Normal)
                return false;

            return true;
        }

        void OnProcEffect(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            uint spellId;
            switch (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()))
            {
                case SpellSchools.Holy:
                    spellId = SpellGenObsidianArmorHoly;
                    break;
                case SpellSchools.Fire:
                    spellId = SpellGenObsidianArmorFire;
                    break;
                case SpellSchools.Nature:
                    spellId = SpellGenObsidianArmorNature;
                    break;
                case SpellSchools.Frost:
                    spellId = SpellGenObsidianArmorFrost;
                    break;
                case SpellSchools.Shadow:
                    spellId = SpellGenObsidianArmorShadow;
                    break;
                case SpellSchools.Arcane:
                    spellId = SpellGenObsidianArmorArcane;
                    break;
                default:
                    return;
            }
            GetTarget().CastSpell(GetTarget(), spellId, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
            OnEffectProc.Add(new(OnProcEffect, 0, AuraType.Dummy));
        }
    }

    [Script]
    class spell_gen_oracle_wolvar_reputation : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1));
        }

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleDummy(uint effIndex)
        {
            Player player = GetCaster().ToPlayer();
            uint factionId = (uint)GetEffectInfo().CalcValue();
            int repChange = GetEffectInfo(1).CalcValue();

            var factionEntry = CliDB.FactionStorage.LookupByKey(factionId);
            if (factionEntry == null)
                return;

            // Set rep to baserep + basepoints (expecting spillover for oposite faction . become hated)
            // Not when player already has equal or higher rep with this faction
            if (player.GetReputationMgr().GetReputation(factionEntry) < repChange)
                player.GetReputationMgr().SetReputation(factionEntry, repChange);

            // EffectIndex2 most likely update at war state, we already handle this in SetReputation
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_orc_disguise : SpellScript
    {
        const uint SpellOrcDisguiseTrigger = 45759;
        const uint SpellOrcDisguiseMale = 45760;
        const uint SpellOrcDisguiseFemale = 45762;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellOrcDisguiseTrigger, SpellOrcDisguiseMale, SpellOrcDisguiseFemale);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            Player target = GetHitPlayer();
            if (target != null)
            {
                if (target.GetNativeGender() == 0)
                    caster.CastSpell(target, SpellOrcDisguiseMale, true);
                else
                    caster.CastSpell(target, SpellOrcDisguiseFemale, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 35201 - Paralytic Poison
    class spell_gen_paralytic_poison : AuraScript
    {
        const uint SpellParalysis = 35202;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellParalysis);
        }

        void HandleStun(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            GetTarget().CastSpell(null, SpellParalysis, aurEff);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(HandleStun, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_prevent_emotes : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleEffectApply, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_player_say : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.BroadcastTextStorage.HasRecord((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void HandleScript(uint effIndex)
        {
            // Note: target here is always player; caster here is gameobj, creature or player (self cast)
            Unit target = GetHitUnit();
            if (target != null)
                target.Say((uint)GetEffectValue(), target);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script("spell_item_soul_harvesters_charm")]
    [Script("spell_item_commendation_of_kaelthas")]
    [Script("spell_item_corpse_tongue_coin")]
    [Script("spell_item_corpse_tongue_coin_heroic")]
    [Script("spell_item_petrified_twilight_scale")]
    [Script("spell_item_petrified_twilight_scale_heroic")]
    class spell_gen_proc_below_pct_damaged : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return false;

            int pct = GetSpellInfo().GetEffect(0).CalcValue();

            if (eventInfo.GetActionTarget().HealthBelowPctDamaged(pct, damageInfo.GetDamage()))
                return true;

            return false;
        }

        public override void Register()
        {
            DoCheckProc.Add(new(CheckProc));
        }
    }

    [Script]
    class spell_gen_proc_charge_drop_only : AuraScript
    {
        void HandleChargeDrop(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
        }

        public override void Register()
        {
            OnProc.Add(new(HandleChargeDrop));
        }
    }

    [Script] // 45472 Parachute
    class spell_gen_parachute : AuraScript
    {
        const uint SpellParachute = 45472;
        const uint SpellParachuteBuff = 44795;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellParachute, SpellParachuteBuff);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Player target = GetTarget().ToPlayer();
            if (target != null)
                if (target.IsFalling())
                {
                    target.RemoveAurasDueToSpell(SpellParachute);
                    target.CastSpell(target, SpellParachuteBuff, true);
                }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_gen_pet_summoned : SpellScript
    {
        const uint NpcDoomguard = 11859;
        const uint NpcInfernal = 89;
        const uint NpcImp = 416;

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleScript(uint effIndex)
        {
            Player player = GetCaster().ToPlayer();
            if (player.GetLastPetNumber() != 0)
            {
                PetType newPetType = (player.GetClass() == Class.Hunter) ? PetType.Hunter : PetType.Summon;
                Pet newPet = new Pet(player, newPetType);
                if (newPet.LoadPetFromDB(player, 0, player.GetLastPetNumber(), true))
                {
                    // revive the pet if it is dead
                    if (newPet.GetDeathState() != DeathState.Alive && newPet.GetDeathState() != DeathState.JustRespawned)
                        newPet.SetDeathState(DeathState.JustRespawned);

                    newPet.SetFullHealth();
                    newPet.SetFullPower(newPet.GetPowerType());

                    switch (newPet.GetEntry())
                    {
                        case NpcDoomguard:
                        case NpcInfernal:
                            newPet.SetEntry(NpcImp);
                            break;
                        default:
                            break;
                    }
                }
                else
                    newPet.Dispose();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 36553 - PetWait
    class spell_gen_pet_wait : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            GetCaster().GetMotionMaster().Clear();
            GetCaster().GetMotionMaster().MoveIdle();
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_profession_research : SpellScript
    {
        const uint SpellNorthrendInscriptionResearch = 61177;

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        SpellCastResult CheckRequirement()
        {
            Player player = GetCaster().ToPlayer();

            if (SkillDiscovery.HasDiscoveredAllSpells(GetSpellInfo().Id, player))
            {
                SetCustomCastResultMessage(SpellCustomErrors.NothingToDiscover);
                return SpellCastResult.CustomError;
            }

            return SpellCastResult.SpellCastOk;
        }

        void HandleScript(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            uint spellId = GetSpellInfo().Id;

            // Learn random explicit discovery recipe (if any)
            // Players will now learn 3 recipes the very first time they perform Northrend Inscription Research (3.3.0 patch notes)
            if (spellId == SpellNorthrendInscriptionResearch && !SkillDiscovery.HasDiscoveredAnySpell(spellId, caster))
            {
                for (int i = 0; i < 2; ++i)
                {
                    uint discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);
                    if (discoveredSpellId != 0)
                        caster.LearnSpell(discoveredSpellId, false);
                }
            }

            uint discoveredSpellId1 = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);
            if (discoveredSpellId1 != 0)
                caster.LearnSpell(discoveredSpellId1, false);
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckRequirement));
            OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_pvp_trinket : SpellScript
    {
        const uint SpellPvpTrinketAlliance = 97403;
        const uint SpellPvpTrinketHorde = 97404;

        void TriggerAnimation()
        {
            Player caster = GetCaster().ToPlayer();

            switch (caster.GetEffectiveTeam())
            {
                case Team.Alliance:
                    caster.CastSpell(caster, SpellPvpTrinketAlliance, TriggerCastFlags.FullMask);
                    break;
                case Team.Horde:
                    caster.CastSpell(caster, SpellPvpTrinketHorde, TriggerCastFlags.FullMask);
                    break;
                default:
                    break;
            }
        }

        public override void Register()
        {
            AfterCast.Add(new(TriggerAnimation));
        }
    }

    [Script]
    class spell_gen_Remove_flight_auras : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                target.RemoveAurasByType(AuraType.Fly);
                target.RemoveAurasByType(AuraType.ModIncreaseMountedFlightSpeed);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 20589 - Escape artist
    class spell_gen_Remove_impairing_auras : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            GetHitUnit().RemoveMovementImpairingAuras(true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 23493 - Restoration
    [Script] // 24379 - Restoration
    class spell_gen_restoration : AuraScript
    {
        void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();

            Unit target = GetTarget();
            if (target == null)
                return;

            uint heal = (uint)target.CountPctFromMaxHealth(10);
            HealInfo healInfo = new(target, target, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            target.HealBySpell(healInfo);

            /// @todo: should proc other auras?
            int mana = target.GetMaxPower(PowerType.Mana);
            if (mana != 0)
            {
                mana /= 10;
                target.EnergizeBySpell(target, GetSpellInfo(), mana, PowerType.Mana);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    // 38772 Grievous Wound
    // 43937 Grievous Wound
    // 62331 Impale
    [Script] // 62418 Impale
    class spell_gen_Remove_on_health_pct : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1));
        }

        void PeriodicTick(AuraEffect aurEff)
        {
            // they apply damage so no need to check for ticks here

            if (GetTarget().HealthAbovePct(GetEffectInfo(1).CalcValue()))
            {
                Remove(AuraRemoveMode.EnemySpell);
                PreventDefaultAction();
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicDamage));
        }
    }

    // 31956 Grievous Wound
    // 38801 Grievous Wound
    // 43093 Grievous Throw
    // 58517 Grievous Wound
    [Script] // 59262 Grievous Wound
    class spell_gen_Remove_on_full_health : AuraScript
    {
        void PeriodicTick(AuraEffect aurEff)
        {
            // if it has only periodic effect, allow 1 tick
            bool onlyEffect = GetSpellInfo().GetEffects().Count == 1;
            if (onlyEffect && aurEff.GetTickNumber() <= 1)
                return;

            if (GetTarget().IsFullHealth())
            {
                Remove(AuraRemoveMode.EnemySpell);
                PreventDefaultAction();
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicDamage));
        }
    }

    // 70292 - Glacial Strike
    [Script] // 71316 - Glacial Strike
    class spell_gen_Remove_on_full_health_pct : AuraScript
    {
        void PeriodicTick(AuraEffect aurEff)
        {
            // they apply damage so no need to check for ticks here

            if (GetTarget().IsFullHealth())
            {
                Remove(AuraRemoveMode.EnemySpell);
                PreventDefaultAction();
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(PeriodicTick, 2, AuraType.PeriodicDamagePercent));
        }
    }

    class ReplenishmentCheck : ICheck<WorldObject>
    {
        public bool Invoke(WorldObject obj)
        {
            Unit target = obj.ToUnit();
            if (target != null)
                return target.GetPowerType() != PowerType.Mana;

            return true;
        }
    }

    [Script]
    class spell_gen_replenishment : SpellScript
    {
        void RemoveInvalidTargets(List<WorldObject> targets)
        {
            // In arenas Replenishment may only affect the caster
            Player caster = GetCaster().ToPlayer();
            if (caster != null)
            {
                if (caster.InArena())
                {
                    targets.Clear();
                    targets.Add(caster);
                    return;
                }
            }

            targets.RemoveAll(new ReplenishmentCheck());

            byte maxTargets = 10;

            if (targets.Count > maxTargets)
            {
                targets.Sort(new PowerPctOrderPred(PowerType.Mana));
                targets.Resize(maxTargets);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
        }
    }

    [Script]
    class spell_gen_replenishment_AuraScript : AuraScript
    {
        const uint SpellReplenishment = 57669;
        const uint SpellInfiniteReplenishment = 61782;

        public override bool Load()
        {
            return GetUnitOwner().GetPowerType() == PowerType.Mana;
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            switch (GetSpellInfo().Id)
            {
                case SpellReplenishment:
                    amount = (int)(GetUnitOwner().GetMaxPower(PowerType.Mana) * 0.002f);
                    break;
                case SpellInfiniteReplenishment:
                    amount = (int)(GetUnitOwner().GetMaxPower(PowerType.Mana) * 0.0025f);
                    break;
                default:
                    break;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.PeriodicEnergize));
        }
    }

    struct RunningWildMountIds
    {
        public const uint AlteredForm = 97709;
    }

    [Script]
    class spell_gen_running_wild : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(RunningWildMountIds.AlteredForm);
        }

        public override void OnPrecast()
        {
            GetCaster().CastSpell(GetCaster(), RunningWildMountIds.AlteredForm, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
        }
    }

    [Script]
    class spell_gen_running_wild_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(SharedConst.DisplayIdHiddenMount))
                return false;
            return true;
        }

        void HandleMount(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            PreventDefaultAction();

            target.Mount(SharedConst.DisplayIdHiddenMount, 0, 0);

            // cast speed aura
            var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(aurEff.GetAmount());
            if (mountCapability != null)
                target.CastSpell(target, mountCapability.ModSpellAuraID, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleMount, 1, AuraType.Mounted, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_two_forms : SpellScript
    {
        SpellCastResult CheckCast()
        {
            if (GetCaster().IsInCombat())
            {
                SetCustomCastResultMessage(SpellCustomErrors.CantTransform);
                return SpellCastResult.CustomError;
            }

            // Player cannot transform to human form if he is forced to be worgen for some reason (Darkflight)
            var alteredFormAuras = GetCaster().GetAuraEffectsByType(AuraType.WorgenAlteredForm);
            if (alteredFormAuras.Count > 1)
            {
                SetCustomCastResultMessage(SpellCustomErrors.CantTransform);
                return SpellCastResult.CustomError;
            }

            return SpellCastResult.SpellCastOk;
        }

        void HandleTransform(uint effIndex)
        {
            Unit target = GetHitUnit();
            PreventHitDefaultEffect(effIndex);
            if (target.HasAuraType(AuraType.WorgenAlteredForm))
                target.RemoveAurasByType(AuraType.WorgenAlteredForm);
            else    // Basepoints 1 for this aura control whether to trigger transform transition animation or not.
                target.CastSpell(target, RunningWildMountIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 1));
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
            OnEffectHitTarget.Add(new(HandleTransform, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_darkflight : SpellScript
    {
        void TriggerTransform()
        {
            GetCaster().CastSpell(GetCaster(), RunningWildMountIds.AlteredForm, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            AfterCast.Add(new(TriggerTransform));
        }
    }

    [Script]
    class spell_gen_seaforium_blast : SpellScript
    {
        const uint SpellPlantChargesCreditAchievement = 60937;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellPlantChargesCreditAchievement);
        }

        public override bool Load()
        {
            return GetGObjCaster().GetOwnerGUID().IsPlayer();
        }

        void AchievementCredit(uint effIndex)
        {
            Unit owner = GetGObjCaster().GetOwner();
            if (owner != null)
            {
                GameObject go = GetHitGObj();
                if (go != null)
                    if (go.GetGoInfo().type == GameObjectTypes.DestructibleBuilding)
                        owner.CastSpell(null, SpellPlantChargesCreditAchievement, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(AchievementCredit, 1, SpellEffectName.GameObjectDamage));
        }
    }

    [Script]
    class spell_gen_spectator_cheer_trigger : SpellScript
    {
        Emote[] EmoteArray = { Emote.OneshotCheer, Emote.OneshotExclamation, Emote.OneshotApplaud };

        void HandleDummy(uint effIndex)
        {
            if (RandomHelper.randChance(40))
                GetCaster().HandleEmoteCommand(EmoteArray.SelectRandom());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_spirit_healer_res : SpellScript
    {
        public override bool Load()
        {
            return GetOriginalCaster() != null && GetOriginalCaster().IsPlayer();
        }

        void HandleDummy(uint effIndex)
        {
            Player originalCaster = GetOriginalCaster().ToPlayer();
            Unit target = GetHitUnit();
            if (target != null)
            {
                NPCInteractionOpenResult spiritHealerConfirm = new();
                spiritHealerConfirm.Npc = target.GetGUID();
                spiritHealerConfirm.InteractionType = PlayerInteractionType.SpiritHealer;
                spiritHealerConfirm.Success = true;
                originalCaster.SendPacket(spiritHealerConfirm);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_summon_tournament_mount : SpellScript
    {
        const uint SpellLanceEquipped = 62853;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellLanceEquipped);
        }

        SpellCastResult CheckIfLanceEquiped()
        {
            if (GetCaster().IsInDisallowedMountForm())
                GetCaster().RemoveAurasByType(AuraType.ModShapeshift);

            if (!GetCaster().HasAura(SpellLanceEquipped))
            {
                SetCustomCastResultMessage(SpellCustomErrors.MustHaveLanceEquipped);
                return SpellCastResult.CustomError;
            }

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckIfLanceEquiped));
        }
    }

    [Script] // 41213, 43416, 69222, 73076 - Throw Shield
    class spell_gen_throw_shield : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_tournament_duel : SpellScript
    {
        const uint SpellOnTournamentMount = 63034;
        const uint SpellMountedDuel = 62875;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellOnTournamentMount, SpellMountedDuel);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Unit rider = GetCaster().GetCharmer();
            if (rider != null)
            {
                Player playerTarget = GetHitPlayer();
                if (playerTarget != null)
                {
                    if (playerTarget.HasAura(SpellOnTournamentMount) && playerTarget.GetVehicleBase() != null)
                        rider.CastSpell(playerTarget, SpellMountedDuel, true);
                }
                else
                {
                    Unit unitTarget = GetHitUnit();
                    if (unitTarget != null)
                    {
                        if (unitTarget.GetCharmer() != null && unitTarget.GetCharmer().IsPlayer() && unitTarget.GetCharmer().HasAura(SpellOnTournamentMount))
                            rider.CastSpell(unitTarget.GetCharmer(), SpellMountedDuel, true);
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_tournament_pennant : AuraScript
    {
        public override bool Load()
        {
            return GetCaster() != null && GetCaster().IsPlayer();
        }

        void HandleApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
                if (caster.GetVehicleBase() == null)
                    caster.RemoveAurasDueToSpell(GetId());
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleApplyEffect, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script]
    class spell_gen_teleporting : SpellScript
    {
        const uint AreaVioletCitadelSpire = 4637;

        const uint SpellTeleportSpireDown = 59316;
        const uint SpellTeleportSpireUp = 59314;

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (!target.IsPlayer())
                return;

            // return from top
            if (target.ToPlayer().GetAreaId() == AreaVioletCitadelSpire)
                target.CastSpell(target, SpellTeleportSpireDown, true);
            // teleport atop
            else
                target.CastSpell(target, SpellTeleportSpireUp, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_trigger_exclude_caster_aura_spell : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(spellInfo.ExcludeCasterAuraSpell);
        }

        void HandleTrigger()
        {
            // Blizz seems to just apply aura without bothering to cast
            GetCaster().AddAura(GetSpellInfo().ExcludeCasterAuraSpell, GetCaster());
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleTrigger));
        }
    }

    [Script]
    class spell_gen_trigger_exclude_target_aura_spell : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(spellInfo.ExcludeTargetAuraSpell);
        }

        void HandleTrigger()
        {
            Unit target = GetHitUnit();
            if (target != null)
                // Blizz seems to just apply aura without bothering to cast
                GetCaster().AddAura(GetSpellInfo().ExcludeTargetAuraSpell, target);
        }

        public override void Register()
        {
            AfterHit.Add(new(HandleTrigger));
        }
    }

    [Script("spell_pvp_trinket_shared_cd", SpellWillOfTheForsakenCooldownTrigger)]
    [Script("spell_wotf_shared_cd", SpellWillOfTheForsakenCooldownTriggerWotf)]
    class spell_pvp_trinket_wotf_shared_cd : SpellScript
    {
        const uint SpellWillOfTheForsakenCooldownTrigger = 72752;
        const uint SpellWillOfTheForsakenCooldownTriggerWotf = 72757;

        uint _triggeredSpellId;

        public spell_pvp_trinket_wotf_shared_cd(uint triggeredSpellId)
        {
            _triggeredSpellId = triggeredSpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_triggeredSpellId);
        }

        void HandleScript()
        {
            // Spell flags need further research, until then just cast not triggered
            GetCaster().CastSpell(null, _triggeredSpellId, false);
        }

        public override void Register()
        {
            AfterCast.Add(new(HandleScript));
        }
    }

    [Script]
    class spell_gen_turkey_marker : AuraScript
    {
        const uint SpellTurkeyVengeance = 25285;

        List<uint> _applyTimes = new();

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // store stack apply times, so we can pop them while they expire
            _applyTimes.Add(GameTime.GetGameTimeMS());
            Unit target = GetTarget();

            // on stack 15 cast the achievement crediting spell
            if (GetStackAmount() >= 15)
                target.CastSpell(target, SpellTurkeyVengeance, new CastSpellExtraArgs(aurEff)
                    .SetOriginalCaster(GetCasterGUID()));
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            int RemoveCount = 0;

            // pop expired times off of the stack
            while (!_applyTimes.Empty() && _applyTimes.First() + GetMaxDuration() < GameTime.GetGameTimeMS())
            {
                _applyTimes.RemoveAt(0);
                RemoveCount++;
            }

            if (RemoveCount != 0)
                ModStackAmount(-RemoveCount, AuraRemoveMode.Expire);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_gen_upper_deck_create_foam_sword : SpellScript
    {
        uint[] itemId = { 45061, 45176, 45177, 45178, 45179 };

        void HandleScript(uint effIndex)
        {
            Player player = GetHitPlayer();
            if (player != null)
            {

                // player can only have one of these items
                for (byte i = 0; i < 5; ++i)
                {
                    if (player.HasItemCount(itemId[i], 1, true))
                        return;
                }

                CreateItem(itemId[RandomHelper.IRand(0, 4)], ItemContext.None);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 52723 - VaMathF.PIric Touch
    [Script] // 60501 - VaMathF.PIric Touch
    class spell_gen_vapiric_touch : AuraScript
    {
        const uint SpellVapiricTouchHeal = 52724;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellVapiricTouchHeal);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            Unit caster = eventInfo.GetActor();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)(damageInfo.GetDamage() / 2));
            caster.CastSpell(caster, SpellVapiricTouchHeal, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script]
    class spell_gen_vehicle_scaling : AuraScript
    {
        const uint SpellGearScaling = 66668;

        public override bool Load()
        {
            return GetCaster() != null && GetCaster().IsPlayer();
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            float factor;
            ushort baseItemLevel;

            /// @todo Reserach coeffs for different vehicles
            switch (GetId())
            {
                case SpellGearScaling:
                    factor = 1.0f;
                    baseItemLevel = 205;
                    break;
                default:
                    factor = 1.0f;
                    baseItemLevel = 170;
                    break;
            }

            float avgILvl = caster.ToPlayer().GetAverageItemLevel();
            if (avgILvl < baseItemLevel)
                return;                     /// @todo Research possibility of scaling down

            amount = (ushort)((avgILvl - baseItemLevel) * factor);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModHealingPct));
            DoEffectCalcAmount.Add(new(CalculateAmount, 1, AuraType.ModDamagePercentDone));
            DoEffectCalcAmount.Add(new(CalculateAmount, 2, AuraType.ModIncreaseHealthPercent));
        }
    }

    [Script]
    class spell_gen_vendor_bark_trigger : SpellScript
    {
        const uint NpcAmphitheaterVendor = 30098;
        const uint SayAmphitheaterVendor = 0;
        void HandleDummy(uint effIndex)
        {
            Creature vendor = GetCaster().ToCreature();
            if (vendor != null)
                if (vendor.GetEntry() == NpcAmphitheaterVendor)
                    vendor.GetAI().Talk(SayAmphitheaterVendor);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_wg_water : SpellScript
    {
        SpellCastResult CheckCast()
        {
            if (!GetSpellInfo().CheckTargetCreatureType(GetCaster()))
                return SpellCastResult.DontReport;
            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckCast));
        }
    }

    [Script]
    class spell_gen_whisper_gulch_yogg_saron_whisper : AuraScript
    {
        const uint SpellYoggSaronWhisperDummy = 29072;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellYoggSaronWhisperDummy);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(null, SpellYoggSaronWhisperDummy, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_gen_whisper_to_controller : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.BroadcastTextStorage.HasRecord((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void HandleScript(uint effIndex)
        {
            TempSummon casterSummon = GetCaster().ToTempSummon();
            if (casterSummon != null)
            {
                Player target = casterSummon.GetSummonerUnit().ToPlayer();
                if (target != null)
                    casterSummon.Whisper((uint)GetEffectValue(), target, false);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // BasePoints of spells is Id of npc_text used to group texts, it's not implemented so texts are grouped the old way
    // 50037 - Mystery of the Infinite: Future You's Whisper to Controller - Random
    // 50287 - Azure Dragon: On Death Force Cast Wyrmrest Defender to Whisper to Controller - Random
    // 60709 - Moti, Redux: Past You's Whisper to Controller - Random
    [Script("spell_future_you_whisper_to_controller_random", WhisperFutureYou)]
    [Script("spell_wyrmrest_defender_whisper_to_controller_random", WhisperDefender)]
    [Script("spell_past_you_whisper_to_controller_random", WhisperPastYou)]
    class spell_gen_whisper_to_controller_random : SpellScript
    {
        const uint WhisperFutureYou = 2;
        const uint WhisperDefender = 1;
        const uint WhisperPastYou = 2;

        uint _text;

        public spell_gen_whisper_to_controller_random(uint text)
        {
            _text = text;
        }

        void HandleScript(uint effIndex)
        {
            // Same for all spells
            if (!RandomHelper.randChance(20))
                return;

            Creature target = GetHitCreature();
            if (target != null)
            {
                TempSummon targetSummon = target.ToTempSummon();
                if (targetSummon != null)
                {
                    Player player = targetSummon.GetSummonerUnit().ToPlayer();
                    if (player != null)
                        targetSummon.GetAI().Talk(_text, player);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    class spell_gen_eject_all_passengers : SpellScript
    {
        void RemoveVehicleAuras()
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();
            if (vehicle != null)
                vehicle.RemoveAllPassengers();
        }

        public override void Register()
        {
            AfterHit.Add(new(RemoveVehicleAuras));
        }
    }

    [Script]
    class spell_gen_eject_passenger : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            if (!ValidateSpellEffect((spellInfo.Id, 0)))
                return false;
            if (spellInfo.GetEffect(0).CalcValue() < 1)
                return false;
            return true;
        }

        void EjectPassenger(uint effIndex)
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();
            if (vehicle != null)
            {
                Unit passenger = vehicle.GetPassenger((sbyte)(GetEffectValue() - 1));
                if (passenger != null)
                    passenger.ExitVehicle();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(EjectPassenger, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script("spell_gen_eject_passenger_1", 0)]
    [Script("spell_gen_eject_passenger_3", 2)]
    class spell_gen_eject_passenger_with_seatId : SpellScript
    {
        sbyte _seatId;

        public spell_gen_eject_passenger_with_seatId(int seatId)
        {
            _seatId = (sbyte)seatId;
        }

        void EjectPassenger(uint effIndex)
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();
            if (vehicle != null)
            {
                Unit passenger = vehicle.GetPassenger(_seatId);
                if (passenger != null)
                    passenger.ExitVehicle();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(EjectPassenger, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_gm_freeze : AuraScript
    {
        const uint SpellGmFreeze = 9454;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellGmFreeze);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Do what was done before to the target in HandleFreezeCommand
            Player player = GetTarget().ToPlayer();
            if (player != null)
            {
                // stop combat + make player unattackable + duel stop + stop some spells
                player.SetFaction((uint)FactionTemplates.Friendly);
                player.CombatStop();
                if (player.IsNonMeleeSpellCast(true))
                    player.InterruptNonMeleeSpells(true);
                player.SetUnitFlag(UnitFlags.NonAttackable);

                // if player class = hunter || warlock Remove pet if alive
                if ((player.GetClass() == Class.Hunter) || (player.GetClass() == Class.Warlock))
                {
                    Pet pet = player.GetPet();
                    if (pet != null)
                    {
                        pet.SavePetToDB(PetSaveMode.AsCurrent);
                        // not let dismiss dead pet
                        if (pet.IsAlive())
                            player.RemovePet(pet, PetSaveMode.NotInSlot);
                    }
                }
            }
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Do what was done before to the target in HandleUnfreezeCommand
            Player player = GetTarget().ToPlayer();
            if (player != null)
            {
                // Reset player faction + allow combat + allow duels
                player.SetFactionForRace(player.GetRace());
                player.RemoveUnitFlag(UnitFlags.NonAttackable);
                // save player
                player.SaveToDB();
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_stand : SpellScript
    {
        void HandleScript(uint eff)
        {
            Creature target = GetHitCreature();
            if (target == null)
                return;

            target.SetStandState(UnitStandStateType.Stand);
            target.HandleEmoteCommand(Emote.StateNone);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    struct MixologySpellIds
    {
        public const uint Mixology = 53042;
        // Flasks
        public const uint FlaskOfTheFrostWyrm = 53755;
        public const uint FlaskOfStoneblood = 53758;
        public const uint FlaskOfEndlessRage = 53760;
        public const uint FlaskOfPureMojo = 54212;
        public const uint LesserFlaskOfResistance = 62380;
        public const uint LesserFlaskOfToughness = 53752;
        public const uint FlaskOfBlindingLight = 28521;
        public const uint FlaskOfChromaticWonder = 42735;
        public const uint FlaskOfFortification = 28518;
        public const uint FlaskOfMightyRestoration = 28519;
        public const uint FlaskOfPureDeath = 28540;
        public const uint FlaskOfRelentlessAssault = 28520;
        public const uint FlaskOfChromaticResistance = 17629;
        public const uint FlaskOfDistilledWisdom = 17627;
        public const uint FlaskOfSupremePower = 17628;
        public const uint FlaskOfTheTitans = 17626;
        // Elixirs
        public const uint ElixirOfMightyAgility = 28497;
        public const uint ElixirOfAccuracy = 60340;
        public const uint ElixirOfDeadlyStrikes = 60341;
        public const uint ElixirOfMightyDefense = 60343;
        public const uint ElixirOfExpertise = 60344;
        public const uint ElixirOfArmorPiercing = 60345;
        public const uint ElixirOfLightningSpeed = 60346;
        public const uint ElixirOfMightyFortitude = 53751;
        public const uint ElixirOfMightyMageblood = 53764;
        public const uint ElixirOfMightyStrength = 53748;
        public const uint ElixirOfMightyToughts = 60347;
        public const uint ElixirOfProtection = 53763;
        public const uint ElixirOfSpirit = 53747;
        public const uint GurusElixir = 53749;
        public const uint ShadowpowerElixir = 33721;
        public const uint WrathElixir = 53746;
        public const uint ElixirOfEmpowerment = 28514;
        public const uint ElixirOfMajorMageblood = 28509;
        public const uint ElixirOfMajorShadowPower = 28503;
        public const uint ElixirOfMajorDefense = 28502;
        public const uint FelStrengthElixir = 38954;
        public const uint ElixirOfIronskin = 39628;
        public const uint ElixirOfMajorAgility = 54494;
        public const uint ElixirOfDraenicWisdom = 39627;
        public const uint ElixirOfMajorFirepower = 28501;
        public const uint ElixirOfMajorFrostPower = 28493;
        public const uint EarthenElixir = 39626;
        public const uint ElixirOfMastery = 33726;
        public const uint ElixirOfHealingPower = 28491;
        public const uint ElixirOfMajorFortitude = 39625;
        public const uint ElixirOfMajorStrength = 28490;
        public const uint AdeptsElixir = 54452;
        public const uint OnslaughtElixir = 33720;
        public const uint MightyTrollsBloodElixir = 24361;
        public const uint GreaterArcaneElixir = 17539;
        public const uint ElixirOfTheMongoose = 17538;
        public const uint ElixirOfBruteForce = 17537;
        public const uint ElixirOfSages = 17535;
        public const uint ElixirOfSuperiorDefense = 11348;
        public const uint ElixirOfDemonslaying = 11406;
        public const uint ElixirOfGreaterFirepower = 26276;
        public const uint ElixirOfShadowPower = 11474;
        public const uint MagebloodElixir = 24363;
        public const uint ElixirOfGiants = 11405;
        public const uint ElixirOfGreaterAgility = 11334;
        public const uint ArcaneElixir = 11390;
        public const uint ElixirOfGreaterIntellect = 11396;
        public const uint ElixirOfGreaterDefense = 11349;
        public const uint ElixirOfFrostPower = 21920;
        public const uint ElixirOfAgility = 11328;
        public const uint MajorTrollsBlloodElixir = 3223;
        public const uint ElixirOfFortitude = 3593;
        public const uint ElixirOfOgresStrength = 3164;
        public const uint ElixirOfFirepower = 7844;
        public const uint ElixirOfLesserAgility = 3160;
        public const uint ElixirOfDefense = 3220;
        public const uint StrongTrollsBloodElixir = 3222;
        public const uint ElixirOfMinorAccuracy = 63729;
        public const uint ElixirOfWisdom = 3166;
        public const uint ElixirOfGianthGrowth = 8212;
        public const uint ElixirOfMinorAgility = 2374;
        public const uint ElixirOfMinorFortitude = 2378;
        public const uint WeakTrollsBloodElixir = 3219;
        public const uint ElixirOfLionsStrength = 2367;
        public const uint ElixirOfMinorDefense = 673;
    }

    [Script]
    class spell_gen_mixology_bonus : AuraScript
    {
        int bonus = 0;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(MixologySpellIds.Mixology) && ValidateSpellEffect((spellInfo.Id, 0));
        }

        public override bool Load()
        {
            return GetCaster() != null && GetCaster().IsPlayer();
        }

        void SetBonusValueForEffect(uint effIndex, int value, AuraEffect aurEff)
        {
            if (aurEff.GetEffIndex() == effIndex)
                bonus = value;
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster().HasAura(MixologySpellIds.Mixology) && GetCaster().HasSpell(GetEffectInfo(0).TriggerSpell))
            {
                switch (GetId())
                {
                    case MixologySpellIds.WeakTrollsBloodElixir:
                    case MixologySpellIds.MagebloodElixir:
                        bonus = amount;
                        break;
                    case MixologySpellIds.ElixirOfFrostPower:
                    case MixologySpellIds.LesserFlaskOfToughness:
                    case MixologySpellIds.LesserFlaskOfResistance:
                        bonus = MathFunctions.CalculatePct(amount, 80);
                        break;
                    case MixologySpellIds.ElixirOfMinorDefense:
                    case MixologySpellIds.ElixirOfLionsStrength:
                    case MixologySpellIds.ElixirOfMinorAgility:
                    case MixologySpellIds.MajorTrollsBlloodElixir:
                    case MixologySpellIds.ElixirOfShadowPower:
                    case MixologySpellIds.ElixirOfBruteForce:
                    case MixologySpellIds.MightyTrollsBloodElixir:
                    case MixologySpellIds.ElixirOfGreaterFirepower:
                    case MixologySpellIds.OnslaughtElixir:
                    case MixologySpellIds.EarthenElixir:
                    case MixologySpellIds.ElixirOfMajorAgility:
                    case MixologySpellIds.FlaskOfTheTitans:
                    case MixologySpellIds.FlaskOfRelentlessAssault:
                    case MixologySpellIds.FlaskOfStoneblood:
                    case MixologySpellIds.ElixirOfMinorAccuracy:
                        bonus = MathFunctions.CalculatePct(amount, 50);
                        break;
                    case MixologySpellIds.ElixirOfProtection:
                        bonus = 280;
                        break;
                    case MixologySpellIds.ElixirOfMajorDefense:
                        bonus = 200;
                        break;
                    case MixologySpellIds.ElixirOfGreaterDefense:
                    case MixologySpellIds.ElixirOfSuperiorDefense:
                        bonus = 140;
                        break;
                    case MixologySpellIds.ElixirOfFortitude:
                        bonus = 100;
                        break;
                    case MixologySpellIds.FlaskOfEndlessRage:
                        bonus = 82;
                        break;
                    case MixologySpellIds.ElixirOfDefense:
                        bonus = 70;
                        break;
                    case MixologySpellIds.ElixirOfDemonslaying:
                        bonus = 50;
                        break;
                    case MixologySpellIds.FlaskOfTheFrostWyrm:
                        bonus = 47;
                        break;
                    case MixologySpellIds.WrathElixir:
                        bonus = 32;
                        break;
                    case MixologySpellIds.ElixirOfMajorFrostPower:
                    case MixologySpellIds.ElixirOfMajorFirepower:
                    case MixologySpellIds.ElixirOfMajorShadowPower:
                        bonus = 29;
                        break;
                    case MixologySpellIds.ElixirOfMightyToughts:
                        bonus = 27;
                        break;
                    case MixologySpellIds.FlaskOfSupremePower:
                    case MixologySpellIds.FlaskOfBlindingLight:
                    case MixologySpellIds.FlaskOfPureDeath:
                    case MixologySpellIds.ShadowpowerElixir:
                        bonus = 23;
                        break;
                    case MixologySpellIds.ElixirOfMightyAgility:
                    case MixologySpellIds.FlaskOfDistilledWisdom:
                    case MixologySpellIds.ElixirOfSpirit:
                    case MixologySpellIds.ElixirOfMightyStrength:
                    case MixologySpellIds.FlaskOfPureMojo:
                    case MixologySpellIds.ElixirOfAccuracy:
                    case MixologySpellIds.ElixirOfDeadlyStrikes:
                    case MixologySpellIds.ElixirOfMightyDefense:
                    case MixologySpellIds.ElixirOfExpertise:
                    case MixologySpellIds.ElixirOfArmorPiercing:
                    case MixologySpellIds.ElixirOfLightningSpeed:
                        bonus = 20;
                        break;
                    case MixologySpellIds.FlaskOfChromaticResistance:
                        bonus = 17;
                        break;
                    case MixologySpellIds.ElixirOfMinorFortitude:
                    case MixologySpellIds.ElixirOfMajorStrength:
                        bonus = 15;
                        break;
                    case MixologySpellIds.FlaskOfMightyRestoration:
                        bonus = 13;
                        break;
                    case MixologySpellIds.ArcaneElixir:
                        bonus = 12;
                        break;
                    case MixologySpellIds.ElixirOfGreaterAgility:
                    case MixologySpellIds.ElixirOfGiants:
                        bonus = 11;
                        break;
                    case MixologySpellIds.ElixirOfAgility:
                    case MixologySpellIds.ElixirOfGreaterIntellect:
                    case MixologySpellIds.ElixirOfSages:
                    case MixologySpellIds.ElixirOfIronskin:
                    case MixologySpellIds.ElixirOfMightyMageblood:
                        bonus = 10;
                        break;
                    case MixologySpellIds.ElixirOfHealingPower:
                        bonus = 9;
                        break;
                    case MixologySpellIds.ElixirOfDraenicWisdom:
                    case MixologySpellIds.GurusElixir:
                        bonus = 8;
                        break;
                    case MixologySpellIds.ElixirOfFirepower:
                    case MixologySpellIds.ElixirOfMajorMageblood:
                    case MixologySpellIds.ElixirOfMastery:
                        bonus = 6;
                        break;
                    case MixologySpellIds.ElixirOfLesserAgility:
                    case MixologySpellIds.ElixirOfOgresStrength:
                    case MixologySpellIds.ElixirOfWisdom:
                    case MixologySpellIds.ElixirOfTheMongoose:
                        bonus = 5;
                        break;
                    case MixologySpellIds.StrongTrollsBloodElixir:
                    case MixologySpellIds.FlaskOfChromaticWonder:
                        bonus = 4;
                        break;
                    case MixologySpellIds.ElixirOfEmpowerment:
                        bonus = -10;
                        break;
                    case MixologySpellIds.AdeptsElixir:
                        SetBonusValueForEffect(0, 13, aurEff);
                        SetBonusValueForEffect(1, 13, aurEff);
                        SetBonusValueForEffect(2, 8, aurEff);
                        break;
                    case MixologySpellIds.ElixirOfMightyFortitude:
                        SetBonusValueForEffect(0, 160, aurEff);
                        break;
                    case MixologySpellIds.ElixirOfMajorFortitude:
                        SetBonusValueForEffect(0, 116, aurEff);
                        SetBonusValueForEffect(1, 6, aurEff);
                        break;
                    case MixologySpellIds.FelStrengthElixir:
                        SetBonusValueForEffect(0, 40, aurEff);
                        SetBonusValueForEffect(1, 40, aurEff);
                        break;
                    case MixologySpellIds.FlaskOfFortification:
                        SetBonusValueForEffect(0, 210, aurEff);
                        SetBonusValueForEffect(1, 5, aurEff);
                        break;
                    case MixologySpellIds.GreaterArcaneElixir:
                        SetBonusValueForEffect(0, 19, aurEff);
                        SetBonusValueForEffect(1, 19, aurEff);
                        SetBonusValueForEffect(2, 5, aurEff);
                        break;
                    case MixologySpellIds.ElixirOfGianthGrowth:
                        SetBonusValueForEffect(0, 5, aurEff);
                        break;
                    default:
                        Log.outError(LogFilter.Spells, $"SpellId {GetId()} couldn't be processed in spell_gen_mixology_bonus");
                        break;
                }
                amount += bonus;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, SpellConst.EffectAll, AuraType.Any));
        }
    }

    [Script]
    class spell_gen_landmine_knockback_achievement : SpellScript
    {
        const uint SpellLandmineKnockbackAchievement = 57064;

        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target != null)
            {
                Aura aura = GetHitAura();
                if (aura == null || aura.GetStackAmount() < 10)
                    return;

                target.CastSpell(target, SpellLandmineKnockbackAchievement, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 34098 - ClearAllDebuffs
    class spell_gen_clear_debuffs : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                target.RemoveOwnedAuras(aura =>
                {
                    SpellInfo spellInfo = aura.GetSpellInfo();
                    return spellInfo.IsPositive() && spellInfo.IsPassive();
                });

            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_pony_mount_check : AuraScript
    {
        const uint AchievPonyUp = 3736;
        const uint MountPony = 29736;

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster == null)
                return;
            Player owner = caster.GetOwner().ToPlayer();
            if (owner == null || !owner.HasAchieved(AchievPonyUp))
                return;

            if (owner.IsMounted())
            {
                caster.Mount(MountPony);
                caster.SetSpeedRate(UnitMoveType.Run, owner.GetSpeedRate(UnitMoveType.Run));
            }
            else if (caster.IsMounted())
            {
                caster.Dismount();
                caster.SetSpeedRate(UnitMoveType.Run, owner.GetSpeedRate(UnitMoveType.Run));
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    struct CorruptinPlagueIds
    {
        public const uint NpcApexisFlayer = 22175;
        public const uint NpcShardHideBoar = 22180;
        public const uint NpcAetherRay = 22181;
        public const uint SpellCorruptingPlague = 40350;
    }

    // 40350 - Corrupting Plague
    class CorruptingPlagueSearcher : ICheck<Unit>
    {
        Unit _unit;
        float _distance;

        public CorruptingPlagueSearcher(Unit obj, float distance)
        {
            _unit = obj;
            _distance = distance;
        }

        public bool Invoke(Unit u)
        {
            if (_unit.GetDistance2d(u) < _distance &&
                (u.GetEntry() == CorruptinPlagueIds.NpcApexisFlayer || u.GetEntry() == CorruptinPlagueIds.NpcShardHideBoar || u.GetEntry() == CorruptinPlagueIds.NpcAetherRay) &&
                !u.HasAura(CorruptinPlagueIds.SpellCorruptingPlague))
                return true;

            return false;
        }
    }

    [Script] // 40349 - Corrupting Plague
    class spell_corrupting_plague_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(CorruptinPlagueIds.SpellCorruptingPlague);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            Unit owner = GetTarget();

            List<Creature> targets = new();
            CorruptingPlagueSearcher creature_check = new(owner, 15.0f);
            CreatureListSearcher creature_searcher = new(owner, targets, creature_check);
            Cell.VisitGridObjects(owner, creature_searcher, 15.0f);

            if (!targets.Empty())
                return;

            PreventDefaultAction();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    struct StasisFieldIds
    {
        public const uint NpcDaggertailLizard = 22255;
        public const uint SpellStasisField = 40307;
    }

    // 40307 - Stasis Field
    class StasisFieldSearcher : ICheck<Unit>
    {
        Unit _unit;
        float _distance;

        public StasisFieldSearcher(Unit obj, float distance)
        {
            _unit = obj;
            _distance = distance;
        }

        public bool Invoke(Unit u)
        {
            if (_unit.GetDistance2d(u) < _distance &&
                (u.GetEntry() == CorruptinPlagueIds.NpcApexisFlayer || u.GetEntry() == CorruptinPlagueIds.NpcShardHideBoar || u.GetEntry() == CorruptinPlagueIds.NpcAetherRay || u.GetEntry() == StasisFieldIds.NpcDaggertailLizard) &&
                !u.HasAura(StasisFieldIds.SpellStasisField))
                return true;

            return false;
        }
    }

    [Script] // 40306 - Stasis Field
    class spell_stasis_field_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(StasisFieldIds.SpellStasisField);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            Unit owner = GetTarget();

            List<Creature> targets = new();
            StasisFieldSearcher creature_check = new(owner, 15.0f);
            CreatureListSearcher creature_searcher = new(owner, targets, creature_check);
            Cell.VisitGridObjects(owner, creature_searcher, 15.0f);

            if (!targets.Empty())
                return;

            PreventDefaultAction();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_gen_vehicle_control_link : AuraScript
    {
        const uint SpellSiegeTankControl = 47963;

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellSiegeTankControl); //aurEff.GetAmount()
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 34779 - Freezing Circle
    class spell_freezing_circle : SpellScript
    {
        const uint SpellFreezingCirclePitOfSaronNormal = 69574;
        const uint SpellFreezingCirclePitOfSaronHeroic = 70276;
        const uint SpellFreezingCircle = 34787;
        const uint SpellFreezingCircleScenario = 141383;
        const uint MapIdBloodInTheSnowScenario = 1130;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellFreezingCirclePitOfSaronNormal,
                    SpellFreezingCirclePitOfSaronHeroic,
                    SpellFreezingCircle,
                    SpellFreezingCircleScenario
               );
        }

        void HandleDamage(uint effIndex)
        {
            Unit caster = GetCaster();
            uint spellId;
            Map map = caster.GetMap();

            if (map.IsDungeon())
                spellId = map.IsHeroic() ? SpellFreezingCirclePitOfSaronHeroic : SpellFreezingCirclePitOfSaronNormal;
            else
                spellId = map.GetId() == MapIdBloodInTheSnowScenario ? SpellFreezingCircleScenario : SpellFreezingCircle;

            SpellInfo spellInfo = SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());
            if (spellInfo != null && spellInfo.GetEffects().Empty())
                SetHitDamage(spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDamage, 1, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // Used for some spells cast by vehicles or charmed creatures that do not send a cooldown event on their own
    class spell_gen_charmed_unit_spell_cooldown : SpellScript
    {
        void HandleCast()
        {
            Unit caster = GetCaster();
            Player owner = caster.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (owner != null)
            {
                SpellCooldownPkt spellCooldown = new();
                spellCooldown.Caster = owner.GetGUID();
                spellCooldown.Flags = SpellCooldownFlags.None;
                spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(GetSpellInfo().Id, GetSpellInfo().RecoveryTime));
                owner.SendPacket(spellCooldown);
            }
        }

        public override void Register()
        {
            OnCast.Add(new(HandleCast));
        }
    }

    [Script]
    class spell_gen_cannon_blast : SpellScript
    {
        const uint SpellCannonBlast = 42578;
        const uint SpellCannonBlastDamage = 42576;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellCannonBlast);
        }
        void HandleScript(uint effIndex)
        {
            int bp = GetEffectValue();
            Unit target = GetHitUnit();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, bp);
            target.CastSpell(target, SpellCannonBlastDamage, args);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 37751 - Submerged
    class spell_gen_submerged : SpellScript
    {
        void HandleScript(uint eff)
        {
            Creature target = GetHitCreature();
            if (target != null)
                target.SetStandState(UnitStandStateType.Submerged);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 169869 - Transformation Sickness
    class spell_gen_decimatus_transformation_sickness : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
                target.SetHealth(target.CountPctFromMaxHealth(25));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 189491 - Summon Towering Infernal.
    class spell_gen_anetheron_summon_towering_infernal : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_mark_of_kazrogal_hellfire : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(target =>
            {
                Unit unit = target.ToUnit();
                if (unit != null)
                    return unit.GetPowerType() != PowerType.Mana;
                return false;
            });
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        }
    }

    [Script]
    class spell_gen_mark_of_kazrogal_hellfire_AuraScript : AuraScript
    {
        const uint SpellMarkOfKazrogalHellfire = 189512;
        const uint SpellMarkOfKazrogalDamageHellfire = 189515;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellMarkOfKazrogalDamageHellfire);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();

            if (target.GetPower(PowerType.Mana) == 0)
            {
                target.CastSpell(target, SpellMarkOfKazrogalDamageHellfire, aurEff);
                // Remove aura
                SetDuration(0);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PowerBurn));
        }
    }

    [Script]
    class spell_gen_azgalor_rain_of_fire_hellfire_citadel : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 99947 - Face Rage
    class spell_gen_face_rage : AuraScript
    {
        const uint SpellFaceRage = 99947;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellFaceRage)
                && ValidateSpellEffect((spellInfo.Id, 2));
        }

        void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(GetEffectInfo(2).TriggerSpell);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 187213 - Impatient Mind
    class spell_gen_impatient_mind : AuraScript
    {
        const uint SpellImpatientMind = 187213;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellImpatientMind);
        }

        void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(effect.GetSpellEffectInfo().TriggerSpell);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 209352 - Boost 2.0 [Paladin+Priest] - Watch for Shield
    class spell_gen_boost_2_0_paladin_priest_watch_for_shield : AuraScript
    {
        uint SpellPowerWordShield = 17;
        uint SpellDivineShield = 642;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellPowerWordShield, SpellDivineShield);
        }

        bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
        {
            SpellInfo spellInfo = procInfo.GetSpellInfo();
            return spellInfo != null && (spellInfo.Id == SpellPowerWordShield || spellInfo.Id == SpellDivineShield);
        }

        public override void Register()
        {
            DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    // 269083 - Enlisted
    [Script] // 282559 - Enlisted
    class spell_gen_war_mode_enlisted : AuraScript
    {
        void CalcWarModeBonus(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player target = GetUnitOwner().ToPlayer();
            if (target == null)
                return;

            switch (target.GetTeamId())
            {
                case BattleGroundTeamId.Alliance:
                    amount = WorldStateMgr.GetValue(WorldStates.WarModeAllianceBuffValue, target.GetMap());
                    break;
                case BattleGroundTeamId.Horde:
                    amount = WorldStateMgr.GetValue(WorldStates.WarModeHordeBuffValue, target.GetMap());
                    break;
                default:
                    break;
            }
        }

        public override void Register()
        {
            SpellInfo spellInfo = SpellMgr.GetSpellInfo(m_scriptSpellId, Difficulty.None);

            if (spellInfo.HasAura(AuraType.ModXpPct))
                DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpPct));

            if (spellInfo.HasAura(AuraType.ModXpQuestPct))
                DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpQuestPct));

            if (spellInfo.HasAura(AuraType.ModCurrencyGainFromSource))
                DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModCurrencyGainFromSource));

            if (spellInfo.HasAura(AuraType.ModMoneyGain))
                DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModMoneyGain));

            if (spellInfo.HasAura(AuraType.ModAnimaGain))
                DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModAnimaGain));

            if (spellInfo.HasAura(AuraType.Dummy))
                DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.Dummy));
        }
    }

    [Script]
    class spell_defender_of_azeroth_death_gate_selector : SpellScript
    {
        const uint SpellDeathGateTeleportStormwind = 316999;
        const uint SpellDeathGateTeleportOrgrimmar = 317000;

        const uint QuestDefenderOfAzerothAlliance = 58902;
        const uint QuestDefenderOfAzerothHorde = 58903;

        (WorldLocation, uint) StormwindInnLoc = (new(0, -8868.1f, 675.82f, 97.9f, 5.164778709411621093f), 5148);
        (WorldLocation, uint) OrgrimmarInnLoc = (new(1, 1573.18f, -4441.62f, 16.06f, 1.818284034729003906f), 8618);

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellDeathGateTeleportStormwind, SpellDeathGateTeleportOrgrimmar);
        }

        void HandleDummy(uint effIndex)
        {
            Player player = GetHitUnit().ToPlayer();
            if (player == null)
                return;

            if (player.GetQuestStatus(QuestDefenderOfAzerothAlliance) == QuestStatus.None && player.GetQuestStatus(QuestDefenderOfAzerothHorde) == QuestStatus.None)
                return;

            var (loc, areaId) = player.GetTeam() == Team.Alliance ? StormwindInnLoc : OrgrimmarInnLoc;
            player.SetHomebind(loc, areaId);
            player.SendBindPointUpdate();
            player.SendPlayerBound(player.GetGUID(), areaId);

            player.CastSpell(player, player.GetTeam() == Team.Alliance ? SpellDeathGateTeleportStormwind : SpellDeathGateTeleportOrgrimmar);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_defender_of_azeroth_speak_with_mograine : SpellScript
    {
        const uint NpcNazgrim = 161706;
        const uint NpcTrollbane = 161707;
        const uint NpcWhitemane = 161708;

        void HandleDummy(uint effIndex)
        {
            if (GetCaster() == null)
                return;

            Player player = GetCaster().ToPlayer();
            if (player == null)
                return;

            Creature nazgrim = GetHitUnit().FindNearestCreature(NpcNazgrim, 10.0f);
            if (nazgrim != null)
                nazgrim.HandleEmoteCommand(Emote.OneshotPoint, player);
            Creature trollbane = GetHitUnit().FindNearestCreature(NpcTrollbane, 10.0f);
            if (trollbane != null)
                trollbane.HandleEmoteCommand(Emote.OneshotPoint, player);
            Creature whitemane = GetHitUnit().FindNearestCreature(NpcWhitemane, 10.0f);
            if (whitemane != null)
                whitemane.HandleEmoteCommand(Emote.OneshotPoint, player);

            // @Todo: spawntracking - show death gate for casting player
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 118301 - Summon Battle Pet
    class spell_summon_battle_pet : SpellScript
    {
        void HandleSummon(uint effIndex)
        {
            uint creatureId = (uint)GetSpellValue().EffectBasePoints[effIndex];
            if (ObjectMgr.GetCreatureTemplate(creatureId) != null)
            {
                PreventHitDefaultEffect(effIndex);

                Unit caster = GetCaster();
                var properties = CliDB.SummonPropertiesStorage.LookupByKey((uint)GetEffectInfo().MiscValueB);
                TimeSpan duration = TimeSpan.FromMilliseconds(GetSpellInfo().CalcDuration(caster));
                Position pos = GetHitDest().GetPosition();

                Creature summon = caster.GetMap().SummonCreature(creatureId, pos, properties, duration, caster, GetSpellInfo().Id);
                if (summon != null)
                    summon.SetImmuneToAll(true);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleSummon, 0, SpellEffectName.Summon));
        }
    }

    [Script] // 132334 - Trainer Heal Cooldown (Serverside)
    class spell_gen_trainer_heal_cooldown : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SharedConst.SpellReviveBattlePets);
        }

        public override bool Load()
        {
            return GetUnitOwner().IsPlayer();
        }

        void UpdateReviveBattlePetCooldown(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player target = GetUnitOwner().ToPlayer();
            SpellInfo reviveBattlePetSpellInfo = SpellMgr.GetSpellInfo(SharedConst.SpellReviveBattlePets, Difficulty.None);

            if (target.GetSession().GetBattlePetMgr().IsBattlePetSystemEnabled())
            {
                TimeSpan expectedCooldown = TimeSpan.FromMilliseconds(GetAura().GetMaxDuration());
                var remainingCooldown = target.GetSpellHistory().GetRemainingCategoryCooldown(reviveBattlePetSpellInfo);
                if (remainingCooldown > TimeSpan.Zero)
                {
                    if (remainingCooldown < expectedCooldown)
                        target.GetSpellHistory().ModifyCooldown(reviveBattlePetSpellInfo, expectedCooldown - remainingCooldown);
                }
                else
                {
                    target.GetSpellHistory().StartCooldown(reviveBattlePetSpellInfo, 0, null, false, expectedCooldown);
                }
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new(UpdateReviveBattlePetCooldown, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 45313 - Anchor Here
    class spell_gen_anchor_here : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Creature creature = GetHitCreature();
            if (creature != null)
                creature.SetHomePosition(creature.GetPositionX(), creature.GetPositionY(), creature.GetPositionZ(), creature.GetOrientation());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 147066 - (Serverside/Non-Db2) Generic - Mount Check Aura
    class spell_gen_mount_check_AuraScript : AuraScript
    {
        void OnPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            uint mountDisplayId = 0;

            TempSummon tempSummon = target.ToTempSummon();
            if (tempSummon == null)
                return;

            Player summoner = tempSummon.GetSummoner()?.ToPlayer();
            if (summoner == null)
                return;

            if (summoner.IsMounted() && (!summoner.IsInCombat() || summoner.IsFlying()))
            {
                CreatureSummonedData summonedData = ObjectMgr.GetCreatureSummonedData(tempSummon.GetEntry());
                if (summonedData != null)
                {
                    if (summoner.IsFlying() && summonedData.FlyingMountDisplayID.HasValue)
                        mountDisplayId = summonedData.FlyingMountDisplayID.Value;
                    else if (summonedData.GroundMountDisplayID.HasValue)
                        mountDisplayId = summonedData.GroundMountDisplayID.Value;
                }
            }

            if (mountDisplayId != target.GetMountDisplayId())
                target.SetMountDisplayId(mountDisplayId);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 274738 - Ancestral Call (Mag'har Orc Racial)
    class spell_gen_ancestral_call : SpellScript
    {
        const uint SpellRictusOfTheLaughingSkull = 274739;
        const uint SpellZealOfTheBurningBlade = 274740;
        const uint SpellFerocityOfTheFrostwolf = 274741;
        const uint SpellMightOfTheBlackrock = 274742;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellRictusOfTheLaughingSkull, SpellZealOfTheBurningBlade, SpellFerocityOfTheFrostwolf, SpellMightOfTheBlackrock);
        }

        uint[] AncestralCallBuffs = { SpellRictusOfTheLaughingSkull, SpellZealOfTheBurningBlade, SpellFerocityOfTheFrostwolf, SpellMightOfTheBlackrock };

        void HandleOnCast()
        {
            Unit caster = GetCaster();
            uint spellId = AncestralCallBuffs.SelectRandom();

            caster.CastSpell(caster, spellId, true);
        }

        public override void Register()
        {
            OnCast.Add(new(HandleOnCast));
        }
    }

    [Script] // 83477 - Eject Passengers 3-8
    class spell_gen_eject_passengers_3_8 : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();
            if (vehicle == null)
                return;

            for (sbyte i = 2; i < 8; i++)
                vehicle.GetPassenger(i)?.ExitVehicle();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 83781 - Reverse Cast Ride Vehicle
    class spell_gen_reverse_cast_target_to_caster_triggered : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            GetHitUnit().CastSpell(GetCaster(), (uint)GetSpellInfo().GetEffect(effIndex).CalcValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // Note: this spell unsummons any creature owned by the caster. Set appropriate target conditions on the Db.
    // 84065 - Despawn All Summons
    // 83935 - Despawn All Summons
    [Script] // 160938 - Despawn All Summons (Garrison Intro Only)
    class spell_gen_despawn_all_summons_owned_by_caster : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Creature target = GetHitCreature();

                if (target.GetOwner() == caster)
                    target.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 8613 - Skinning
    class spell_gen_skinning : SpellScript
    {
        const uint SpellClassicSkinning = 265856;
        const uint SpellOutlandSkinning = 265858;
        const uint SpellNorthrendSkinning = 265860;
        const uint SpellCataclysmSkinning = 265862;
        const uint SpellPandariaSkinning = 265864;
        const uint SpellDraenorSkinning = 265866;
        const uint SpellLegionSkinning = 265868;
        const uint SpellKulTiranSkinning = 265870;
        const uint SpellZandalariSkinning = 265872;
        const uint SpellShadowlandsSkinning = 308570;
        const uint SpellDragonIslesSkinning = 366263;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellOutlandSkinning, SpellNorthrendSkinning, SpellCataclysmSkinning, SpellPandariaSkinning, SpellDraenorSkinning, SpellKulTiranSkinning, SpellZandalariSkinning, SpellShadowlandsSkinning, SpellDragonIslesSkinning);
        }

        void HandleSkinningEffect(uint effIndex)
        {
            Player player = GetCaster().ToPlayer();
            if (player == null)
                return;

            var contentTuning = CliDB.ContentTuningStorage.LookupByKey(GetHitUnit().GetContentTuning());
            if (contentTuning == null)
                return;

            uint skinningSkill = player.GetProfessionSkillForExp(SkillType.Skinning, contentTuning.ExpansionID);
            if (skinningSkill == 0)
                return;

            // Autolearning missing skinning skill (Dragonflight)
            uint getSkinningLearningSpellBySkill()
            {
                switch ((SkillType)skinningSkill)
                {
                    case SkillType.OutlandSkinning: return SpellOutlandSkinning;
                    case SkillType.NorthrendSkinning: return SpellNorthrendSkinning;
                    case SkillType.CataclysmSkinning: return SpellCataclysmSkinning;
                    case SkillType.PandariaSkinning: return SpellPandariaSkinning;
                    case SkillType.DraenorSkinning: return SpellDraenorSkinning;
                    case SkillType.KulTiranSkinning: return player.GetTeam() == Team.Alliance ? SpellKulTiranSkinning : (player.GetTeam() == Team.Horde ? SpellZandalariSkinning : 0);
                    case SkillType.ShadowlandsSkinning: return SpellShadowlandsSkinning;
                    case SkillType.DragonIslesSkinning: return SpellDragonIslesSkinning;
                    case SkillType.ClassicSkinning:      // Trainer only
                    case SkillType.LegionSkinning:       // Quest only
                    default: break;
                }

                return 0;
            }

            if (!player.HasSkill(skinningSkill))
            {
                uint spellId = getSkinningLearningSpellBySkill();
                if (spellId != 0)
                    player.CastSpell(null, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleSkinningEffect, 0, SpellEffectName.Skinning));
        }
    }

    // 2825 - Bloodlust
    // 32182 - Heroism
    // 80353 - Time Warp
    // 264667 - Primal Rage
    // 390386 - Fury of the Aspects
    // 146555 - Drums of Rage
    // 178207 - Drums of Fury
    // 230935 - Drums of the Mountain
    // 256740 - Drums of the Maelstrom
    // 309658 - Drums of Deathly Ferocity
    // 381301 - Feral Hide Drums
    [Script("spell_sha_bloodlust", SpellShamanSated)]
    [Script("spell_sha_heroism", SpellShamanExhaustion)]
    [Script("spell_mage_time_warp", SpellMageTemporalDisplacement)]
    [Script("spell_hun_primal_rage", SpellHunterFatigued)]
    [Script("spell_evo_fury_of_the_aspects", SpellEvokerExhaustion)]
    [Script("spell_item_bloodlust_drums", SpellShamanExhaustion)]
    class spell_gen_bloodlust : SpellScript
    {
        const uint SpellShamanSated = 57724; // Bloodlust
        const uint SpellShamanExhaustion = 57723; // Heroism, Drums
        const uint SpellMageTemporalDisplacement = 80354;
        const uint SpellHunterFatigued = 264689;
        const uint SpellEvokerExhaustion = 390435;

        uint _exhaustionSpellId;

        public spell_gen_bloodlust(uint exhaustionSpellId)
        {
            _exhaustionSpellId = exhaustionSpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellShamanSated, SpellShamanExhaustion, SpellMageTemporalDisplacement, SpellHunterFatigued, SpellEvokerExhaustion);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(target =>
            {
                Unit unit = target.ToUnit();
                if (unit == null)
                    return true;

                return unit.HasAura(SpellShamanSated)
                    || unit.HasAura(SpellShamanExhaustion)
                    || unit.HasAura(SpellMageTemporalDisplacement)
                    || unit.HasAura(SpellHunterFatigued)
                    || unit.HasAura(SpellEvokerExhaustion);
            });
        }

        void HandleHit(uint effIndex)
        {
            Unit target = GetHitUnit();
            target.CastSpell(target, _exhaustionSpellId, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitCasterAreaRaid));
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitCasterAreaRaid));
            OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.ApplyAura));
        }
    }

    // AoE resurrections by spirit guides
    [Script] // 22012 - Spirit Heal
    class spell_gen_spirit_heal_aoe : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            targets.RemoveAll(target =>
            {
                Player playerTarget = target.ToPlayer();
                if (playerTarget != null)
                    return !playerTarget.CanAcceptAreaSpiritHealFrom(caster);

                return true;
            });
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
        }
    }

    // Personal resurrections in battlegrounds
    [Script] // 156758 - Spirit Heal
    class spell_gen_spirit_heal_personal : AuraScript
    {
        uint SpellSpiritHealEffect = 156763;

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Player targetPlayer = GetTarget().ToPlayer();
            if (targetPlayer == null)
                return;

            Unit caster = GetCaster();
            if (caster == null)
                return;

            if (targetPlayer.CanAcceptAreaSpiritHealFrom(caster))
                caster.CastSpell(targetPlayer, SpellSpiritHealEffect);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    class RecastSpiritHealChannelEvent : BasicEvent
    {
        Unit _caster;

        public RecastSpiritHealChannelEvent(Unit caster)
        {
            _caster = caster;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            if (_caster.GetChannelSpellId() == 0)
                _caster.CastSpell(null, BattlegroundConst.SpellSpiritHealChannelAoE, false);

            return true;
        }
    }

    [Script] // 22011 - Spirit Heal Channel
    class spell_gen_spirit_heal_channel : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit target = GetTarget();
            target.m_Events.AddEventAtOffset(new RecastSpiritHealChannelEvent(target), TimeSpan.FromSeconds(1));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 2584 - Waiting to Resurrect
    class spell_gen_waiting_to_resurrect : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player targetPlayer = GetTarget().ToPlayer();
            if (targetPlayer == null)
                return;

            targetPlayer.SetAreaSpiritHealer(null);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    struct MajorPlayerHealingCooldownHelpers
    {
        public const uint SpellDruidTranquility = 740;
        public const uint SpellDruidTranquilityHeal = 157982;
        public const uint SpellPriestDivineHymn = 64843;
        public const uint SpellPriestDivineHymnHeal = 64844;
        public const uint SpellPriestLuminousBarrier = 271466;
        public const uint SpellShamanHealingTideTotem = 108280;
        public const uint SpellShamanHealingTideTotemHeal = 114942;
        public const uint SpellMonkRevival = 115310;
        public const uint SpellEvokerRewind = 363534;

        public static float GetBonusMultiplier(Unit unit, uint spellId)
        {
            // Note: if caster is not in a raid setting, is in PvP or while in arena combat with 5 or less allied players.
            if (!unit.GetMap().IsRaid() || !unit.GetMap().IsBattleground())
            {
                uint bonusSpellId;
                uint effIndex;
                switch (spellId)
                {
                    case SpellDruidTranquilityHeal:
                        bonusSpellId = SpellDruidTranquility;
                        effIndex = 2;
                        break;
                    case SpellPriestDivineHymnHeal:
                        bonusSpellId = SpellPriestDivineHymn;
                        effIndex = 1;
                        break;
                    case SpellPriestLuminousBarrier:
                        bonusSpellId = spellId;
                        effIndex = 1;
                        break;
                    case SpellShamanHealingTideTotemHeal:
                        bonusSpellId = SpellShamanHealingTideTotem;
                        effIndex = 2;
                        break;
                    case SpellMonkRevival:
                        bonusSpellId = spellId;
                        effIndex = 4;
                        break;
                    case SpellEvokerRewind:
                        bonusSpellId = spellId;
                        effIndex = 3;
                        break;
                    default:
                        return 0.0f;
                }

                AuraEffect healingIncreaseEffect = unit.GetAuraEffect(bonusSpellId, effIndex);
                if (healingIncreaseEffect != null)
                    return healingIncreaseEffect.GetAmount();

                return SpellMgr.GetSpellInfo(bonusSpellId, Difficulty.None).GetEffect(effIndex).CalcValue(unit);
            }

            return 0.0f;
        }
    }

    // 157982 - Tranquility (Heal)
    // 64844 - Divine Hymn (Heal)
    // 114942 - Healing Tide (Heal)
    [Script] // 115310 - Revival (Heal)
    class spell_gen_major_healing_cooldown_modifier : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect
            (
                (MajorPlayerHealingCooldownHelpers.SpellDruidTranquility, 2),
                (MajorPlayerHealingCooldownHelpers.SpellPriestDivineHymn, 1),
                (MajorPlayerHealingCooldownHelpers.SpellShamanHealingTideTotem, 2),
                (MajorPlayerHealingCooldownHelpers.SpellMonkRevival, 4)
           );
        }

        void CalculateHealingBonus(SpellEffectInfo spellEffectInfo, Unit victim, ref int healing, ref int flatMod, ref float pctMod)
        {
            MathFunctions.AddPct(ref pctMod, MajorPlayerHealingCooldownHelpers.GetBonusMultiplier(GetCaster(), GetSpellInfo().Id));
        }

        public override void Register()
        {
            CalcHealing.Add(new(CalculateHealingBonus));
        }
    }

    // 157982 - Tranquility (Heal)
    // 271466 - Luminous Barrier (Absorb)
    [Script] // 363534 - Rewind (Heal)
    class spell_gen_major_healing_cooldown_modifier_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect
            (
                (MajorPlayerHealingCooldownHelpers.SpellDruidTranquility, 2),
                (MajorPlayerHealingCooldownHelpers.SpellPriestLuminousBarrier, 1),
                (MajorPlayerHealingCooldownHelpers.SpellEvokerRewind, 3)
           );
        }

        void CalculateHealingBonus(AuraEffect aurEff, Unit victim, ref int damageOrHealing, ref int flatMod, ref float pctMod)
        {
            Unit caster = GetCaster();
            if (caster != null)
                MathFunctions.AddPct(ref pctMod, MajorPlayerHealingCooldownHelpers.GetBonusMultiplier(caster, GetSpellInfo().Id));
        }

        public override void Register()
        {
            DoEffectCalcDamageAndHealing.Add(new(CalculateHealingBonus, SpellConst.EffectAll, AuraType.Any));
        }
    }

    // 24931 - 100 Health
    // 24959 - 500 Health
    // 28838 - 1 Health
    // 43645 - 1 Health
    // 73342 - 1 Health
    [Script] // 86562 - 1 Health
    class spell_gen_set_health : SpellScript
    {
        ulong _health;

        public spell_gen_set_health(ulong health)
        {
            _health = health;
        }

        void HandleHit(uint effIndex)
        {
            if (GetHitUnit().IsAlive() && _health > 0)
                GetHitUnit().SetHealth(_health);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 296837 - Comfortable Rider's Barding
    class spell_gen_comfortable_riders_barding : AuraScript
    {
        static uint SPELL_DAZED = 1604;

        bool _apply;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SPELL_DAZED);
        }

        void HandleEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().ApplySpellImmune(GetId(), SpellImmunity.Id, SPELL_DAZED, _apply);
        }

        public override void Register()
        {
            _apply = true;
            OnEffectApply.Add(new EffectApplyHandler(HandleEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            _apply = false;
            OnEffectRemove.Add(new EffectApplyHandler(HandleEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 297091 - Parachute
    class spell_gen_saddlechute : AuraScript
    {
        static uint SPELL_PARACHUTE = 297092;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SPELL_PARACHUTE);
        }

        void TriggerParachute(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            if (target.IsFlying() || target.IsFalling())
                target.CastSpell(target, SPELL_PARACHUTE, new CastSpellExtraArgs(TriggerCastFlags.DontReportCastError));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(TriggerParachute, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }
}