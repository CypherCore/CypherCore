// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using static Global;

namespace Scripts.Spells.Azerite
{
    struct GenericQuestUpdateEntryIds
    {
        // http://www.wowhead.com/quest=55 Morbent Fel
        public const uint Morbent = 1200;
        public const uint WeakenedMorbent = 24782;

        // http://www.wowhead.com/quest=10255 Testing the Antidote
        public const uint Helboar = 16880;
        public const uint Dreadtusk = 16992;

        // http://www.wowhead.com/quest=11515 Blood for Blood
        public const uint FelbloodInitiate = 24918;
        public const uint EmaciatedFelblood = 24955;
    }

    [Script("spell_q55_sacred_cleansing", SpellEffectName.Dummy, 1u, GenericQuestUpdateEntryIds.Morbent, GenericQuestUpdateEntryIds.WeakenedMorbent, true)] // 8913 - Sacred Cleansing
    [Script("spell_q10255_administer_antidote", SpellEffectName.Dummy, 0u, GenericQuestUpdateEntryIds.Helboar, GenericQuestUpdateEntryIds.Dreadtusk, true)] // 34665 - Administer Antidote
    [Script("spell_q11515_fel_siphon_dummy", SpellEffectName.Dummy, 0u, GenericQuestUpdateEntryIds.FelbloodInitiate, GenericQuestUpdateEntryIds.EmaciatedFelblood, true)] // 44936 - Quest - Fel Siphon Dummy
    class spell_generic_quest_update_entry : SpellScript
    {
        SpellEffectName _spellEffect;
        byte _effIndex;
        uint _originalEntry;
        uint _newEntry;
        bool _shouldAttack;

        public spell_generic_quest_update_entry(SpellEffectName spellEffect, uint effIndex, uint originalEntry, uint newEntry, bool shouldAttack)
        {
            _spellEffect = spellEffect;
            _effIndex = (byte)effIndex;
            _originalEntry = originalEntry;
            _newEntry = newEntry;
            _shouldAttack = shouldAttack;
        }

        void HandleDummy(uint effIndex)
        {
            Creature creatureTarget = GetHitCreature();
            if (creatureTarget != null)
            {
                if (creatureTarget.IsPet() && creatureTarget.GetEntry() == _originalEntry)
                {
                    creatureTarget.UpdateEntry(_newEntry);
                    if (_shouldAttack)
                        creatureTarget.EngageWithTarget(GetCaster());
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, _effIndex, _spellEffect));
        }
    }

    [Script] // 9712 - Thaumaturgy Channel
    class spell_q2203_thaumaturgy_channel : AuraScript
    {
        const uint SpellThaumaturgyChannel = 21029;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellThaumaturgyChannel);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(caster, SpellThaumaturgyChannel, false);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 19512 - Apply Salve
    class spell_q6124_6129_apply_salve : SpellScript
    {
        const uint NpcSicklyGazelle = 12296;
        const uint NpcCuredGazelle = 12297;
        const uint NpcSicklyDeer = 12298;
        const uint NpcCuredDeer = 12299;

        TimeSpan Quest6124_6129_DESPAWN_TIME = TimeSpan.FromSeconds(30);

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (GetCastItem() != null)
            {
                Creature creatureTarget = GetHitCreature();
                if (creatureTarget != null)
                {
                    uint newEntry = 0;
                    switch (caster.GetTeam())
                    {
                        case Team.Horde:
                            if (creatureTarget.GetEntry() == NpcSicklyGazelle)
                                newEntry = NpcCuredGazelle;
                            break;
                        case Team.Alliance:
                            if (creatureTarget.GetEntry() == NpcSicklyDeer)
                                newEntry = NpcCuredDeer;
                            break;
                    }
                    if (newEntry != 0)
                    {
                        creatureTarget.UpdateEntry(newEntry);
                        creatureTarget.DespawnOrUnsummon(Quest6124_6129_DESPAWN_TIME);
                        caster.KilledMonsterCredit(newEntry);
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 43874 - Scourge Mur'gul Camp: Force Shield Arcane Purple x3
    class spell_q11396_11399_force_shield_arcane_purple_x3 : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetImmuneToPC(true);
            target.AddUnitState(UnitState.Root);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().SetImmuneToPC(false);
        }

        public override void Register()
        {
            OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 50133 - Scourging Crystal Controller
    class spell_q11396_11399_scourging_crystal_controller : SpellScript
    {
        const uint SpellForceShieldArcanePurpleX3 = 43874;
        const uint SpellScourgingCrystalController = 43878;

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellForceShieldArcanePurpleX3, SpellScourgingCrystalController);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
                if (target.GetTypeId() == TypeId.Unit && target.HasAura(SpellForceShieldArcanePurpleX3))
                    // Make sure nobody else is channeling the same target
                    if (!target.HasAura(SpellScourgingCrystalController))
                        GetCaster().CastSpell(target, SpellScourgingCrystalController, GetCastItem());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 43882 - Scourging Crystal Controller Dummy
    class spell_q11396_11399_scourging_crystal_controller_dummy : SpellScript
    {
        const uint SpellForceShieldArcanePurpleX3 = 43874;

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellForceShieldArcanePurpleX3);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
                if (target.GetTypeId() == TypeId.Unit)
                    target.RemoveAurasDueToSpell(SpellForceShieldArcanePurpleX3);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 46023 - The Ultrasonic Screwdriver
    class spell_q11730_ultrasonic_screwdriver : SpellScript
    {
        const uint SpellSummonScavengebot004A8 = 46063;
        const uint SpellSummonSentrybot57K = 46068;
        const uint SpellSummonDefendotank66D = 46058;
        const uint SpellSummonScavengebot005B6 = 46066;
        const uint SpellSummon55DCollectatron = 46034;
        const uint SpellRobotKillCredit = 46027;
        const uint NpcScavengebot004A8 = 25752;
        const uint NpcSentrybot57K = 25753;
        const uint NpcDefendotank66D = 25758;
        const uint NpcScavengebot005B6 = 25792;
        const uint Npc55DCollectatron = 25793;

        public override bool Load()
        {
            return GetCaster().IsPlayer() && GetCastItem() != null;
        }

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellSummonScavengebot004A8,
                SpellSummonSentrybot57K,
                SpellSummonDefendotank66D,
                SpellSummonScavengebot005B6,
                SpellSummon55DCollectatron,
                SpellRobotKillCredit
           );
        }

        void HandleDummy(uint effIndex)
        {
            Item castItem = GetCastItem();
            Unit caster = GetCaster();
            Creature target = GetHitCreature();
            if (target != null)
            {
                uint spellId = target.GetEntry() switch
                {
                    NpcScavengebot004A8 => SpellSummonScavengebot004A8,
                    NpcSentrybot57K => SpellSummonSentrybot57K,
                    NpcDefendotank66D => SpellSummonDefendotank66D,
                    NpcScavengebot005B6 => SpellSummonScavengebot005B6,
                    Npc55DCollectatron => SpellSummon55DCollectatron,
                    _ => 0
                };

                if (spellId == 0)
                    return;

                caster.CastSpell(caster, spellId, castItem);
                caster.CastSpell(caster, SpellRobotKillCredit, true);
                target.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 49587 - Seeds of Nature's Wrath
    class spell_q12459_seeds_of_natures_wrath : SpellScript
    {
        const uint NpcReanimatedFrostwyrm = 26841;
        const uint NpcWeakReanimatedFrostwyrm = 27821;

        const uint NpcTurgid = 27808;
        const uint NpcWeakTurgid = 27809;

        const uint NpcDeathgaze = 27122;
        const uint NpcWeakDeathgaze = 27807;

        void HandleDummy(uint effIndex)
        {
            Creature creatureTarget = GetHitCreature();
            if (creatureTarget != null)
            {
                uint uiNewEntry = creatureTarget.GetEntry() switch
                {
                    NpcReanimatedFrostwyrm => NpcWeakReanimatedFrostwyrm,
                    NpcTurgid => NpcWeakTurgid,
                    NpcDeathgaze => NpcWeakDeathgaze,
                    _ => 0
                };

                if (uiNewEntry != 0)
                    creatureTarget.UpdateEntry(uiNewEntry);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 51840 - Despawn Fruit Tosser
    class spell_q12634_despawn_fruit_tosser : SpellScript
    {
        const uint SpellBananasFallToGround = 51836;
        const uint SpellOrangeFallsToGround = 51837;
        const uint SpellPapayaFallsToGround = 51839;
        const uint SpellSummonAdventurousDwarf = 52070;

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellBananasFallToGround,
                SpellOrangeFallsToGround,
                SpellPapayaFallsToGround,
                SpellSummonAdventurousDwarf
           );
        }

        void HandleDummy(uint effIndex)
        {
            uint spellId = SpellBananasFallToGround;
            switch (RandomHelper.URand(0, 3))
            {
                case 1:
                    spellId = SpellOrangeFallsToGround;
                    break;
                case 2:
                    spellId = SpellPapayaFallsToGround;
                    break;
            }
            // sometimes, if you're lucky, you get a dwarf
            if (RandomHelper.randChance(5))
                spellId = SpellSummonAdventurousDwarf;
            GetCaster().CastSpell(GetCaster(), spellId, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 54798 - Flaming Arrow Triggered Effect
    class spell_q12851_going_bearback : AuraScript
    {
        const uint NpcFrostgiant = 29351;
        const uint NpcFrostworg = 29358;
        const uint SpellFrostgiantCredit = 58184;
        const uint SpellFrostworgCredit = 58183;
        const uint SpellImmolation = 54690;
        const uint SpellAblaze = 54683;

        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Unit target = GetTarget();
                // Already in fire
                if (target.HasAura(SpellAblaze))
                    return;

                Player player = caster.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (player != null)
                {
                    switch (target.GetEntry())
                    {
                        case NpcFrostworg:
                            target.CastSpell(player, SpellFrostworgCredit, true);
                            target.CastSpell(target, SpellImmolation, true);
                            target.CastSpell(target, SpellAblaze, true);
                            break;
                        case NpcFrostgiant:
                            target.CastSpell(player, SpellFrostgiantCredit, true);
                            target.CastSpell(target, SpellImmolation, true);
                            target.CastSpell(target, SpellAblaze, true);
                            break;
                    }
                }
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleEffectApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script] // 52090 - Ahunae's Knife
    class spell_q12659_ahunaes_knife : SpellScript
    {
        const uint NpcScalpsKcBunny = 28622;

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            Creature target = GetHitCreature();
            if (target != null)
            {
                target.DespawnOrUnsummon();
                caster.KilledMonsterCredit(NpcScalpsKcBunny);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 54190 - Lifeblood Dummy
    class spell_q12805_lifeblood_dummy : SpellScript
    {
        const uint NpcShardKillCredit = 29303;

        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        void HandleScript(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            Creature target = GetHitCreature();
            if (target != null)
            {
                caster.KilledMonsterCredit(NpcShardKillCredit);
                target.CastSpell(target, (uint)GetEffectValue(), true);
                target.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 4338 - Plant Alliance Battle Standard
    [Script] // 59643 - Plant Horde Battle Standard
    class spell_q13280_13283_plant_battle_standard : SpellScript
    {
        const uint NpcKingOfTheMountaintKc = 31766;
        const uint SpellPlantHordeBattleStandard = 59643;
        const uint SpellHordeBattleStandardState = 59642;
        const uint SpellAllianceBattleStandardState = 4339;

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            uint triggeredSpellID = SpellAllianceBattleStandardState;

            caster.HandleEmoteCommand(Emote.OneshotRoar);
            if (caster.IsVehicle())
            {
                Unit player = caster.GetVehicleKit().GetPassenger(0);
                if (player != null)
                    player.ToPlayer().KilledMonsterCredit(NpcKingOfTheMountaintKc);
            }

            if (GetSpellInfo().Id == SpellPlantHordeBattleStandard)
                triggeredSpellID = SpellHordeBattleStandardState;

            target.RemoveAllAuras();
            target.CastSpell(target, triggeredSpellID, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 4336 - Jump Jets
    class spell_q13280_13283_jump_jets : SpellScript
    {
        const uint SpellJumpRocketBlast = 4340;

        void HandleCast()
        {
            Unit caster = GetCaster();
            if (caster.IsVehicle())
            {
                Unit rocketBunny = caster.GetVehicleKit().GetPassenger(1);
                if (rocketBunny != null)
                    rocketBunny.CastSpell(rocketBunny, SpellJumpRocketBlast, true);
            }
        }

        public override void Register()
        {
            OnCast.Add(new(HandleCast));
        }
    }

    [Script] // 50546 - The Focus on the Beach: Ley Line Focus Control Ring Effect
    class spell_q12066_bunny_kill_credit : SpellScript
    {
        const uint SpellBunnyCreditBeam = 47390;

        void HandleDummy(uint effIndex)
        {
            Creature target = GetHitCreature();
            if (target != null)
                target.CastSpell(GetCaster(), SpellBunnyCreditBeam, false);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 49213 - Defending Wyrmrest Temple: Character Script Cast From Gossip
    class spell_q12372_cast_from_gossip_trigger : SpellScript
    {
        const uint SpellSummonWyrmrestDefender = 49207;

        void HandleScript(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellSummonWyrmrestDefender, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 49370 - Wyrmrest Defender: Destabilize Azure Dragonshrine Effect
    class spell_q12372_destabilize_azure_dragonshrine_dummy : SpellScript
    {
        const uint NpcWyrmrestTempleCredit = 27698;

        void HandleDummy(uint effIndex)
        {
            if (GetHitCreature() != null)
            {
                Unit caster = GetOriginalCaster();
                if (caster != null)
                {
                    Vehicle vehicle = caster.GetVehicleKit();
                    if (vehicle != null)
                    {
                        Unit passenger = vehicle.GetPassenger(0);
                        if (passenger != null)
                        {
                            Player player = passenger.ToPlayer();
                            if (player != null)
                                player.KilledMonsterCredit(NpcWyrmrestTempleCredit);
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 40113 - Knockdown Fel Cannon: The Aggro Check Aura
    class spell_q11010_q11102_q11023_aggro_check_AuraScript : AuraScript
    {
        const uint SpellAggroCheck = 40112;

        void HandleTriggerSpell(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (target != null)
                // On trigger proccing
                target.CastSpell(target, SpellAggroCheck);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleTriggerSpell, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 40112 - Knockdown Fel Cannon: The Aggro Check
    class spell_q11010_q11102_q11023_aggro_check : SpellScript
    {
        const uint SpellFlakCannonTrigger = 40110;

        void HandleDummy(uint effIndex)
        {
            Player playerTarget = GetHitPlayer();
            if (playerTarget != null)
                // Check if found player target is on fly mount or using flying form
                if (playerTarget.HasAuraType(AuraType.Fly) || playerTarget.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                    playerTarget.CastSpell(playerTarget, SpellFlakCannonTrigger, TriggerCastFlags.IgnoreCasterMountedOrOnVehicle);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 40119 - Knockdown Fel Cannon: The Aggro Burst
    class spell_q11010_q11102_q11023_aggro_burst : AuraScript
    {
        const uint SpellChooseLoc = 40056;

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (target != null)
                // On each tick cast Choose Loc to trigger summon
                target.CastSpell(target, SpellChooseLoc);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 40056 - Knockdown Fel Cannon: Choose Loc
    class spell_q11010_q11102_q11023_choose_loc : SpellScript
    {
        const uint NpcFelCannon2 = 23082;

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            // Check for player that is in 65 y range
            List<Unit> playerList = new();
            AnyPlayerInObjectRangeCheck checker = new(caster, 65.0f);
            PlayerListSearcher searcher = new(caster, playerList, checker);
            Cell.VisitWorldObjects(caster, searcher, 65.0f);

            foreach (var player in playerList)
                // Check if found player target is on fly mount or using flying form
                if (player.HasAuraType(AuraType.Fly) || player.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                    // Summom Fel Cannon (bunny version) at found player
                    caster.SummonCreature(NpcFelCannon2, player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 39844 - Skyguard Blasting Charge
    [Script] // 40160 - Throw Bomb
    class spell_q11010_q11102_q11023_q11008_check_fly_mount : SpellScript
    {
        SpellCastResult CheckRequirement()
        {
            Unit caster = GetCaster();
            // This spell will be cast only if caster has one of these auras
            if (!(caster.HasAuraType(AuraType.Fly) || caster.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed)))
                return SpellCastResult.CantDoThatRightNow;
            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new(CheckRequirement));
        }
    }

    [Script] // 50894 - Zul'Drak Rat
    class spell_q12527_zuldrak_rat : SpellScript
    {
        const uint SpellSummonGorgedLurkingBasilisk = 50928;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellSummonGorgedLurkingBasilisk);
        }

        void HandleScriptEffect(uint effIndex)
        {
            if (GetHitAura() != null && GetHitAura().GetStackAmount() >= GetSpellInfo().StackAmount)
            {
                GetHitUnit().CastSpell(null, SpellSummonGorgedLurkingBasilisk, true);
                Creature basilisk = GetHitUnit().ToCreature();
                if (basilisk != null)
                    basilisk.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 55368 - Summon Stefan
    class spell_q12661_q12669_q12676_q12677_q12713_summon_stefan : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            // Adjust effect summon position
            Position offset = new(0.0f, 0.0f, 20.0f, 0.0f);
            dest.RelocateOffset(offset);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new(SetDest, 0, Targets.DestCasterBack));
        }
    }

    [Script] // 53350 - Quenching Mist
    class spell_q12730_quenching_mist : AuraScript
    {
        const uint SpellFlickeringFlames = 53504;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellFlickeringFlames);
        }

        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            GetTarget().RemoveAurasDueToSpell(SpellFlickeringFlames);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicHeal));
        }
    }

    [Script] // 59318 - Grab Fake Soldier
    class spell_q13291_q13292_q13239_q13261_frostbrood_skytalon_grab_decoy : SpellScript
    {
        const uint NpcSkytalon = 31583;
        const uint NpcDecoy = 31578;
        const uint SpellRide = 59319;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellRide);
        }

        void HandleDummy(uint effIndex)
        {
            if (GetHitCreature() == null)
                return;
            // To Do: Being triggered is hack, but in checkcast it doesn't pass aurastate requirements.
            // Beside that the decoy won't keep it's freeze animation state when enter.
            GetHitCreature().CastSpell(GetCaster(), SpellRide, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 59303 - Summon Frost Wyrm
    class spell_q13291_q13292_q13239_q13261_armored_decoy_summon_skytalon : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            // Adjust effect summon position
            Position offset = new(0.0f, 0.0f, 20.0f, 0.0f);
            dest.RelocateOffset(offset);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new(SetDest, 0, Targets.DestCasterBack));
        }
    }

    [Script] // 12601 - Second Chances: Summon Landgren's Soul Moveto Target Bunny
    class spell_q12847_summon_soul_moveto_bunny : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            // Adjust effect summon position
            Position offset = new(0.0f, 0.0f, 2.5f, 0.0f);
            dest.RelocateOffset(offset);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new(SetDest, 0, Targets.DestCaster));
        }
    }

    // 57385 - Argent Cannon
    [Script] // 57412 - Reckoning Bomb
    class spell_q13086_cannons_target : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 0))
            && ValidateSpellInfo((uint)(spellInfo.GetEffect(0).CalcValue()));
        }

        void HandleEffectDummy(uint effIndex)
        {
            WorldLocation pos = GetExplTargetDest();
            if (pos != null)
                GetCaster().CastSpell(pos.GetPosition(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    struct BurstAtTheSeamsIds
    {
        public const uint AreaTheBrokenFront = 4507;
        public const uint AreaMordRetharTheDeathGate = 4508;

        public const uint NpcDrakkariChieftaink = 29099;
        public const uint NpcIcyGhoul = 31142;
        public const uint NpcViciousGeist = 31147;
        public const uint NpcRisenAllianceSoldiers = 31205;
        public const uint NpcRenimatedAbomination = 31692;

        public const uint QuestFuelForTheFire = 12690;

        public const uint SpellBloatedAbominationFeignDeath = 52593;
        public const uint SpellBurstAtTheSeamsBone = 52516;
        public const uint SpellExplodeAbominationMeat = 52520;
        public const uint SpellExplodeAbominationBloodyMeat = 52523;
        public const uint SpellTrollExplosion = 52565;
        public const uint SpellExplodeTrollMeat = 52578;
        public const uint SpellExplodeTrollBloodyMeat = 52580;

        public const uint SpellBurstAtTheSeams59576 = 59576; //script/knockback, That's Abominable
        public const uint SpellBurstAtTheSeams59579 = 59579; //dummy
        public const uint SpellBurstAtTheSeams52510 = 52510; //script/knockback, Fuel for the Fire
        public const uint SpellBurstAtTheSeams52508 = 52508; //damage 20000
        public const uint SpellBurstAtTheSeams59580 = 59580; //damage 50000

        public const uint SpellAssignGhoulKillCreditToMaster = 59590;
        public const uint SpellAssignGeistKillCreditToMaster = 60041;
        public const uint SpellAssignSkeletonKillCreditToMaster = 60039;

        public const uint SpellDrakkariSkullcrusherCredit = 52590;
        public const uint SpellSummonDrakkariChieftain = 52616;
        public const uint SpellDrakkariChieftainkKillCredit = 52620;
    }

    [Script] // 59576 - Burst at the Seams
    class spell_q13264_q13276_q13288_q13289_burst_at_the_seams_59576 : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(BurstAtTheSeamsIds.SpellBurstAtTheSeams59576, BurstAtTheSeamsIds.SpellBloatedAbominationFeignDeath, BurstAtTheSeamsIds.SpellBurstAtTheSeams59579,
                BurstAtTheSeamsIds.SpellBurstAtTheSeamsBone, BurstAtTheSeamsIds.SpellExplodeAbominationMeat, BurstAtTheSeamsIds.SpellExplodeAbominationBloodyMeat);
        }

        void HandleScript(uint effIndex)
        {
            Creature creature = GetCaster().ToCreature();
            if (creature != null)
            {
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellBloatedAbominationFeignDeath, true);
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellBurstAtTheSeams59579, true);
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellBurstAtTheSeamsBone, true);
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellBurstAtTheSeamsBone, true);
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellBurstAtTheSeamsBone, true);
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellExplodeAbominationMeat, true);
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellExplodeAbominationBloodyMeat, true);
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellExplodeAbominationBloodyMeat, true);
                creature.CastSpell(creature, BurstAtTheSeamsIds.SpellExplodeAbominationBloodyMeat, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 59579 - Burst at the Seams
    class spell_q13264_q13276_q13288_q13289_burst_at_the_seams_59579 : AuraScript
    {
        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.CastSpell(target, BurstAtTheSeamsIds.SpellTrollExplosion, true);
            target.CastSpell(target, BurstAtTheSeamsIds.SpellExplodeAbominationMeat, true);
            target.CastSpell(target, BurstAtTheSeamsIds.SpellExplodeTrollMeat, true);
            target.CastSpell(target, BurstAtTheSeamsIds.SpellExplodeTrollMeat, true);
            target.CastSpell(target, BurstAtTheSeamsIds.SpellExplodeTrollBloodyMeat, true);
            target.CastSpell(target, BurstAtTheSeamsIds.SpellBurstAtTheSeamsBone, true);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            Unit caster = GetCaster();
            if (caster != null)
            {
                switch (target.GetEntry())
                {
                    case BurstAtTheSeamsIds.NpcIcyGhoul:
                        target.CastSpell(caster, BurstAtTheSeamsIds.SpellAssignGhoulKillCreditToMaster, true);
                        break;
                    case BurstAtTheSeamsIds.NpcViciousGeist:
                        target.CastSpell(caster, BurstAtTheSeamsIds.SpellAssignGeistKillCreditToMaster, true);
                        break;
                    case BurstAtTheSeamsIds.NpcRisenAllianceSoldiers:
                        target.CastSpell(caster, BurstAtTheSeamsIds.SpellAssignSkeletonKillCreditToMaster, true);
                        break;
                }
            }
            target.CastSpell(target, BurstAtTheSeamsIds.SpellBurstAtTheSeams59580, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 52593 - Bloated Abomination Feign Death
    class spell_q13264_q13276_q13288_q13289_bloated_abom_feign_death : AuraScript
    {
        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            Creature creature = target.ToCreature();
            if (creature != null)
                creature.DespawnOrUnsummon();
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 76245 - Area Restrict Abom
    class spell_q13264_q13276_q13288_q13289_area_restrict_abom : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Creature creature = GetHitCreature();
            if (creature != null)
            {
                uint area = creature.GetAreaId();
                if (area != BurstAtTheSeamsIds.AreaTheBrokenFront && area != BurstAtTheSeamsIds.AreaMordRetharTheDeathGate)
                    creature.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 59590 - Assign Ghoul Kill Credit to Master
    // 60039 - Assign Skeleton Kill Credit to Master
    [Script] // 60041 - Assign Geist Kill Credit to Master
    class spell_q13264_q13276_q13288_q13289_assign_credit_to_master : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                Unit owner = target.GetOwner();
                if (owner != null)
                {
                    owner.CastSpell(owner, (uint)GetEffectValue(), true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 52510 - Burst at the Seams
    class spell_q12690_burst_at_the_seams_52510 : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(BurstAtTheSeamsIds.SpellBurstAtTheSeams52510, BurstAtTheSeamsIds.SpellBurstAtTheSeams52508, BurstAtTheSeamsIds.SpellBurstAtTheSeams59580,
                BurstAtTheSeamsIds.SpellBurstAtTheSeamsBone, BurstAtTheSeamsIds.SpellExplodeAbominationMeat, BurstAtTheSeamsIds.SpellExplodeAbominationBloodyMeat);
        }

        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Unit;
        }

        void HandleKnockBack(uint effIndex)
        {
            Unit creature = GetHitCreature();
            if (creature != null)
            {
                Unit charmer = GetCaster().GetCharmerOrOwner();
                if (charmer != null)
                {
                    Player player = charmer.ToPlayer();
                    if (player != null)
                    {
                        if (player.GetQuestStatus(BurstAtTheSeamsIds.QuestFuelForTheFire) == QuestStatus.Incomplete)
                        {
                            creature.CastSpell(creature, BurstAtTheSeamsIds.SpellBurstAtTheSeamsBone, true);
                            creature.CastSpell(creature, BurstAtTheSeamsIds.SpellExplodeAbominationMeat, true);
                            creature.CastSpell(creature, BurstAtTheSeamsIds.SpellExplodeAbominationBloodyMeat, true);
                            creature.CastSpell(creature, BurstAtTheSeamsIds.SpellBurstAtTheSeams52508, true);
                            creature.CastSpell(creature, BurstAtTheSeamsIds.SpellBurstAtTheSeams59580, true);

                            player.CastSpell(player, BurstAtTheSeamsIds.SpellDrakkariSkullcrusherCredit, true);
                            ushort count = player.GetReqKillOrCastCurrentCount(BurstAtTheSeamsIds.QuestFuelForTheFire, (int)BurstAtTheSeamsIds.NpcDrakkariChieftaink);
                            if ((count % 20) == 0)
                                player.CastSpell(player, BurstAtTheSeamsIds.SpellSummonDrakkariChieftain, true);
                        }
                    }
                }
            }
        }

        void HandleScript(uint effIndex)
        {
            GetCaster().ToCreature().DespawnOrUnsummon(TimeSpan.FromSeconds(2));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleKnockBack, 1, SpellEffectName.KnockBack));
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 48682 - Escape from Silverbrook - Periodic Dummy
    class spell_q12308_escape_from_silverbrook : SpellScript
    {
        const uint SpellSummonWorgen = 48681;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellSummonWorgen);
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellSummonWorgen, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 48681 - Summon Silverbrook Worgen
    class spell_q12308_escape_from_silverbrook_summon_worgen : SpellScript
    {
        void ModDest(ref SpellDestination dest)
        {
            float dist = GetEffectInfo(0).CalcRadius(GetCaster());
            float angle = RandomHelper.FRand(0.75f, 1.25f) * (float)(MathF.PI);

            Position pos = GetCaster().GetNearPosition(dist, angle);
            dest.Relocate(pos);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new(ModDest, 0, Targets.DestCasterSummon));
        }
    }

    [Script] // 51858 - Siphon of Acherus
    class spell_q12641_death_comes_from_on_high : SpellScript
    {
        const uint SpellForgeCredit = 51974;
        const uint SpellTownHallCredit = 51977;
        const uint SpellScarletHoldCredit = 51980;
        const uint SpellChapelCredit = 51982;

        const uint NpcNewAvalonForge = 28525;
        const uint NpcNewAvalonTownHall = 28543;
        const uint NpcScarletHold = 28542;
        const uint NpcChapelOfTheCrimsonFlame = 28544;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellForgeCredit, SpellTownHallCredit, SpellScarletHoldCredit, SpellChapelCredit);
        }

        void HandleDummy(uint effIndex)
        {
            uint spellId = GetHitCreature().GetEntry() switch
            {
                NpcNewAvalonForge => SpellForgeCredit,
                NpcNewAvalonTownHall => SpellTownHallCredit,
                NpcScarletHold => SpellScarletHoldCredit,
                NpcChapelOfTheCrimsonFlame => SpellChapelCredit,
                _ => 0
            };

            GetCaster().CastSpell(null, spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 52694 - Recall Eye of Acherus
    class spell_q12641_recall_eye_of_acherus : SpellScript
    {
        const uint TheEyeOfAcherus = 51852;

        void HandleDummy(uint effIndex)
        {
            Player player = GetCaster().GetCharmerOrOwner().ToPlayer();
            if (player != null)
            {
                player.StopCastingCharm();
                player.StopCastingBindSight();
                player.RemoveAura(TheEyeOfAcherus);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 51769 - Emblazon Runeblade
    class spell_q12619_emblazon_runeblade : AuraScript
    {
        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(caster, aurEff.GetSpellEffectInfo().TriggerSpell, aurEff);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 51770 - Emblazon Runeblade
    class spell_q12619_emblazon_runeblade_effect : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), (uint)GetEffectValue(), false);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 55516 - Gymer's Grab
    class spell_q12919_gymers_grab : SpellScript
    {
        const uint SpellRideGymer = 43671;
        const uint SpellGrabbed = 55424;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellRideGymer);
        }

        void HandleScript(uint effIndex)
        {
            if (GetHitCreature() == null)
                return;

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, 2);
            GetHitCreature().CastSpell(GetCaster(), SpellRideGymer, args);
            GetHitCreature().CastSpell(GetHitCreature(), SpellGrabbed, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 55421 - Gymer's Throw
    class spell_q12919_gymers_throw : SpellScript
    {
        const uint SpellVargulExplosion = 55569;

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.IsVehicle())
            {
                Unit passenger = caster.GetVehicleKit().GetPassenger(1);
                if (passenger != null)
                {
                    passenger.ExitVehicle();
                    caster.CastSpell(passenger, SpellVargulExplosion, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 61752 - Illidan Kill Credit Master
    class spell_q13400_illidan_kill_master : SpellScript
    {
        const uint SpellIllidanKillCredit = 61748;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIllidanKillCredit);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.IsVehicle())
            {
                Unit passenger = caster.GetVehicleKit().GetPassenger(0);
                if (passenger != null)
                    passenger.CastSpell(passenger, SpellIllidanKillCredit, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 66744 - Make Player Destroy Totems
    class spell_q14100_q14111_make_player_destroy_totems : SpellScript
    {
        const uint SpellTotemOfTheEarthenRing = 66747;
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellTotemOfTheEarthenRing);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Player player = GetHitPlayer();
            if (player != null)
                player.CastSpell(player, SpellTotemOfTheEarthenRing, TriggerCastFlags.FullMask); // ignore reagent Cost, consumed by quest
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 39238 - Fuping
    class spell_q10929_fuping : AuraScript
    {
        const uint SpellSummonSandGnome = 39240;
        const uint SpellSummonBoneSlicer = 39241;

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellSummonSandGnome, SpellSummonBoneSlicer);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(caster, RandomHelper.URand(SpellSummonSandGnome, SpellSummonBoneSlicer), true);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 93072 - Get Our Boys Back Dummy
    class spell_q28813_get_our_boys_back_dummy : SpellScript
    {
        const uint SpellRenewedLife = 93097;
        const uint NpcInjuredStormwindInfantry = 50047;
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellRenewedLife);
        }

        void HandleDummyEffect()
        {
            Unit caster = GetCaster();

            Creature injuredStormwindInfantry = caster.FindNearestCreature(NpcInjuredStormwindInfantry, 5.0f, true);
            if (injuredStormwindInfantry != null)
            {
                injuredStormwindInfantry.SetCreatorGUID(caster.GetGUID());
                injuredStormwindInfantry.CastSpell(injuredStormwindInfantry, SpellRenewedLife, true);
            }
        }

        public override void Register()
        {
            OnCast.Add(new(HandleDummyEffect));
        }
    }

    [Script] // 53034 - Set Health Random
    class spell_q28813_set_health_random : SpellScript
    {
        void HandleDummyEffect()
        {
            Unit caster = GetCaster();
            caster.SetHealth(caster.CountPctFromMaxHealth(RandomHelper.IRand(3, 5) * 10));
        }

        public override void Register()
        {
            OnCast.Add(new(HandleDummyEffect));
        }
    }

    [Script] // 49285 - Hand Over Reins
    class spell_q12414_hand_over_reins : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Creature caster = GetCaster().ToCreature();
            GetHitUnit().ExitVehicle();

            if (caster != null)
                caster.DespawnOrUnsummon();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    // 13790 13793 13811 13814 - Among the Chapions
    [Script] // 13665 13745 13750 13756 13761 13767 13772 13777 13782  13787 - The Grand Melee
    class spell_q13665_q13790_bested_trigger : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit().GetCharmerOrOwnerOrSelf();
            target.CastSpell(target, (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    struct ApplyHeatAndStirIds
    {
        public const uint SpellSpurtsAndSmoke = 38594;
        public const uint SpellFailedMix1 = 43376;
        public const uint SpellFailedMix2 = 43378;
        public const uint SpellFailedMix3 = 43970;
        public const uint SpellSuccessfulMix = 43377;

        public const uint CreatureGenericTriggerLab = 24042;

        public const uint Talk0 = 0;
        public const uint Talk1 = 1;
    }

    [Script] // 43972 - Mixing Blood
    class spell_q11306_mixing_blood : SpellScript
    {
        void HandleEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Creature trigger = caster.FindNearestCreature(ApplyHeatAndStirIds.CreatureGenericTriggerLab, 100.0f);
                if (trigger != null)
                    trigger.GetAI().DoCastSelf(ApplyHeatAndStirIds.SpellSpurtsAndSmoke);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleEffect, 1, SpellEffectName.SendEvent));
        }
    }

    [Script] // 43375 - Mixing Vrykul Blood
    class spell_q11306_mixing_vrykul_blood : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                uint chance = RandomHelper.URand(0, 99);
                uint spellId = 0;

                // 90% chance of getting one out of three failure effects
                if (chance < 30)
                    spellId = ApplyHeatAndStirIds.SpellFailedMix1;
                else if (chance < 60)
                    spellId = ApplyHeatAndStirIds.SpellFailedMix2;
                else if (chance < 90)
                    spellId = ApplyHeatAndStirIds.SpellFailedMix3;
                else // 10% chance of successful cast
                    spellId = ApplyHeatAndStirIds.SpellSuccessfulMix;

                caster.CastSpell(caster, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 1, SpellEffectName.Dummy));
        }
    }

    [Script] // 43376 - Failed Mix
    class spell_q11306_failed_mix_43376 : SpellScript
    {
        void HandleEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Creature trigger = caster.FindNearestCreature(ApplyHeatAndStirIds.CreatureGenericTriggerLab, 100.0f);
                if (trigger != null)
                    trigger.GetAI().Talk(ApplyHeatAndStirIds.Talk0, caster);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleEffect, 1, SpellEffectName.SendEvent));
        }
    }

    [Script] // 43378 - Failed Mix
    class spell_q11306_failed_mix_43378 : SpellScript
    {
        void HandleEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                Creature trigger = caster.FindNearestCreature(ApplyHeatAndStirIds.CreatureGenericTriggerLab, 100.0f);
                if (trigger != null)
                    trigger.GetAI().Talk(ApplyHeatAndStirIds.Talk1, caster);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleEffect, 2, SpellEffectName.SendEvent));
        }
    }

    [Script] // 46444 - Weakness to Lightning: Cast on Master Script Effect
    class spell_q11896_weakness_to_lightning_46444 : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                Unit owner = target.GetOwner();
                if (owner != null)
                {
                    target.CastSpell(owner, (uint)GetEffectValue(), true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    struct TamingTheBeastIds
    {
        public const uint IceClawBear = 19548;
        public const uint LargeCragBoar = 19674;
        public const uint SnowLeopard = 19687;
        public const uint AdultPlainstrider = 19688;
        public const uint PrairieStalker = 19689;
        public const uint Swoop = 19692;
        public const uint WebwoodLurker = 19693;
        public const uint DireMottledBoar = 19694;
        public const uint SurfCrawler = 19696;
        public const uint ArmoredScorpid = 19697;
        public const uint NightsaberStalker = 19699;
        public const uint StrigidScreecher = 19700;
        public const uint BarbedCrawler = 30646;
        public const uint GreaterTimberstrider = 30653;
        public const uint Nightstalker = 30654;
        public const uint CrazedDragonhawk = 30099;
        public const uint ElderSpringpaw = 30102;
        public const uint Mistbat = 30105;
        public const uint IceClawBear1 = 19597;
        public const uint LargeCragBoar1 = 19677;
        public const uint SnowLeopard1 = 19676;
        public const uint AdultPlainstrider1 = 19678;
        public const uint PrairieStalker1 = 19679;
        public const uint Swoop1 = 19680;
        public const uint WebwoodLurker1 = 19684;
        public const uint DireMottledBoar1 = 19681;
        public const uint SurfCrawler1 = 19682;
        public const uint ArmoredScorpid1 = 19683;
        public const uint NightsaberStalker1 = 19685;
        public const uint StrigidScreecher1 = 19686;
        public const uint BarbedCrawler1 = 30647;
        public const uint GreaterTimberstrider1 = 30648;
        public const uint Nightstalker1 = 30652;
        public const uint CrazedDragonhawk1 = 30100;
        public const uint ElderSpringpaw1 = 30103;
        public const uint Mistbat1 = 30104;
    }

    [Script]
    class spell_quest_taming_the_beast : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(TamingTheBeastIds.IceClawBear1,
                TamingTheBeastIds.LargeCragBoar1,
                TamingTheBeastIds.SnowLeopard1,
                TamingTheBeastIds.AdultPlainstrider1,
                TamingTheBeastIds.PrairieStalker1,
                TamingTheBeastIds.Swoop1,
                TamingTheBeastIds.WebwoodLurker1,
                TamingTheBeastIds.DireMottledBoar1,
                TamingTheBeastIds.SurfCrawler1,
                TamingTheBeastIds.ArmoredScorpid1,
                TamingTheBeastIds.NightsaberStalker1,
                TamingTheBeastIds.StrigidScreecher1,
                TamingTheBeastIds.BarbedCrawler1,
                TamingTheBeastIds.GreaterTimberstrider1,
                TamingTheBeastIds.Nightstalker1,
                TamingTheBeastIds.CrazedDragonhawk1,
                TamingTheBeastIds.ElderSpringpaw1,
                TamingTheBeastIds.Mistbat1
           );
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetCaster() == null || !GetCaster().IsAlive() || !GetTarget().IsAlive())
                return;

            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            uint finalSpellId = GetId() switch
            {
                TamingTheBeastIds.IceClawBear => TamingTheBeastIds.IceClawBear1,
                TamingTheBeastIds.LargeCragBoar => TamingTheBeastIds.LargeCragBoar1,
                TamingTheBeastIds.SnowLeopard => TamingTheBeastIds.SnowLeopard1,
                TamingTheBeastIds.AdultPlainstrider => TamingTheBeastIds.AdultPlainstrider1,
                TamingTheBeastIds.PrairieStalker => TamingTheBeastIds.PrairieStalker1,
                TamingTheBeastIds.Swoop => TamingTheBeastIds.Swoop1,
                TamingTheBeastIds.WebwoodLurker => TamingTheBeastIds.WebwoodLurker1,
                TamingTheBeastIds.DireMottledBoar => TamingTheBeastIds.DireMottledBoar1,
                TamingTheBeastIds.SurfCrawler => TamingTheBeastIds.SurfCrawler1,
                TamingTheBeastIds.ArmoredScorpid => TamingTheBeastIds.ArmoredScorpid1,
                TamingTheBeastIds.NightsaberStalker => TamingTheBeastIds.NightsaberStalker1,
                TamingTheBeastIds.StrigidScreecher => TamingTheBeastIds.StrigidScreecher1,
                TamingTheBeastIds.BarbedCrawler => TamingTheBeastIds.BarbedCrawler1,
                TamingTheBeastIds.GreaterTimberstrider => TamingTheBeastIds.GreaterTimberstrider1,
                TamingTheBeastIds.Nightstalker => TamingTheBeastIds.Nightstalker1,
                TamingTheBeastIds.CrazedDragonhawk => TamingTheBeastIds.CrazedDragonhawk1,
                TamingTheBeastIds.ElderSpringpaw => TamingTheBeastIds.ElderSpringpaw1,
                TamingTheBeastIds.Mistbat => TamingTheBeastIds.Mistbat1,
                _ => 0
            };

            if (finalSpellId != 0)
                GetCaster().CastSpell(GetTarget(), finalSpellId, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 53099, 57896, 58418, 58420, 59064, 59065, 59439, 60900, 60940
    class spell_quest_portal_with_condition : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 1))
            && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue())
            && ObjectMgr.GetQuestTemplate((uint)spellInfo.GetEffect(1).CalcValue()) != null;
        }

        void HandleScriptEffect(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target == null)
                return;

            uint spellId = (uint)GetEffectInfo().CalcValue();
            uint questId = (uint)GetEffectInfo(1).CalcValue();

            // This probably should be a way to throw error in SpellCastResult
            if (target.IsActiveQuest(questId))
                target.CastSpell(target, spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    struct TributeSpellIds
    {
        public const uint GromsTrollTribute = 24101;
        public const uint GromsTaurenTribute = 24102;
        public const uint GromsUndeadTribute = 24103;
        public const uint GromsOrcTribute = 24104;
        public const uint GromsBloodelfTribute = 69530;
        public const uint UthersHumanTribute = 24105;
        public const uint UthersGnomeTribute = 24106;
        public const uint UthersDwarfTribute = 24107;
        public const uint UthersNightelfTribute = 24108;
        public const uint UthersDraeneiTribute = 69533;
    }

    // 24194 - Uther's Tribute
    [Script] // 24195 - Grom's Tribute
    class spell_quest_uther_grom_tribute : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(TributeSpellIds.GromsTrollTribute, TributeSpellIds.UthersHumanTribute,
                TributeSpellIds.GromsTaurenTribute, TributeSpellIds.UthersGnomeTribute,
                TributeSpellIds.GromsUndeadTribute, TributeSpellIds.UthersDwarfTribute,
                TributeSpellIds.GromsOrcTribute, TributeSpellIds.UthersNightelfTribute,
                TributeSpellIds.GromsBloodelfTribute, TributeSpellIds.UthersDraeneiTribute);
        }

        void HandleScript(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (caster == null)
                return;

            uint spell = caster.GetRace() switch
            {
                Race.Troll => TributeSpellIds.GromsTrollTribute,
                Race.Tauren => TributeSpellIds.GromsTaurenTribute,
                Race.Undead => TributeSpellIds.GromsUndeadTribute,
                Race.Orc => TributeSpellIds.GromsOrcTribute,
                Race.BloodElf => TributeSpellIds.GromsBloodelfTribute,
                Race.Human => TributeSpellIds.UthersHumanTribute,
                Race.Gnome => TributeSpellIds.UthersGnomeTribute,
                Race.Dwarf => TributeSpellIds.UthersDwarfTribute,
                Race.NightElf => TributeSpellIds.UthersNightelfTribute,
                Race.Draenei => TributeSpellIds.UthersDraeneiTribute,
                _ => 0,
            };

            if (spell != 0)
                caster.CastSpell(caster, spell);
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 68682 Call Attack Mastiffs
    class spell_q14386_call_attack_mastiffs : SpellScript
    {
        const uint NpcAttackMastiff = 36405;

        void HandleEffect(uint eff)
        {
            Unit caster = GetCaster();
            caster.SummonCreature(NpcAttackMastiff, -1944.573f, 2657.402f, 0.994939f, 1.691919f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
            caster.SummonCreature(NpcAttackMastiff, -2005.65f, 2663.526f, -2.086935f, 0.5942355f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
            caster.SummonCreature(NpcAttackMastiff, -1996.506f, 2651.347f, -1.011707f, 0.8185352f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
            caster.SummonCreature(NpcAttackMastiff, -1972.352f, 2640.07f, 1.080288f, 1.217854f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
            caster.SummonCreature(NpcAttackMastiff, -1949.322f, 2642.76f, 1.242482f, 1.58074f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
            caster.SummonCreature(NpcAttackMastiff, -1993.94f, 2672.535f, -2.322549f, 0.5766209f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
            caster.SummonCreature(NpcAttackMastiff, -1982.724f, 2662.8f, -1.773986f, 0.8628055f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
            caster.SummonCreature(NpcAttackMastiff, -1973.301f, 2655.475f, -0.7831049f, 1.098415f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
            caster.SummonCreature(NpcAttackMastiff, -1956.509f, 2650.655f, 1.350571f, 1.441473f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        }

        public override void Register()
        {
            OnEffectHit.Add(new(HandleEffect, 1, SpellEffectName.SendEvent));
        }
    }
}