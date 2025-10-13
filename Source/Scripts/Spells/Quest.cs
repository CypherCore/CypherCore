// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the Gnu General Public License. See License file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Quest;

struct SpellIds
{
    // ThaumaturgyChannel
    public const uint ThaumaturgyChannel = 21029;

    // Quest11396_11399Data
    public const uint ForceShieldArcanePurpleX3 = 43874;
    public const uint ScourgingCrystalController = 43878;

    // Quest11730Data
    public const uint SummonScavengebot004A8 = 46063;
    public const uint SummonSentrybot57K = 46068;
    public const uint SummonDefendotank66D = 46058;
    public const uint SummonScavengebot005B6 = 46066;
    public const uint Summon55DCollectatron = 46034;
    public const uint RobotKillCredit = 46027;

    // Quest12634Data
    public const uint BananasFallToGround = 51836;
    public const uint OrangeFallsToGround = 51837;
    public const uint PapayaFallsToGround = 51839;
    public const uint SummonAdventurousDwarf = 52070;

    // Quest12851Data
    public const uint FrostgiantCredit = 58184;
    public const uint FrostworgCredit = 58183;
    public const uint Immolation = 54690;
    public const uint Ablaze = 54683;

    // BattleStandard
    public const uint PlantHordeBattleStandard = 59643;
    public const uint HordeBattleStandardState = 59642;
    public const uint AllianceBattleStandardState = 4339;
    public const uint JumpRocketBlast = 4340;

    // FocusOnTheBeach
    public const uint BunnyCreditBeam = 47390;

    // DefendingWyrmrestTemple
    public const uint SummonWyrmrestDefender = 49207;

    // Quest11010_11102_11023Data
    public const uint FlakCannonTrigger = 40110;
    public const uint ChooseLoc = 40056;
    public const uint AggroCheck = 40112;

    // SpellZuldrakRat
    public const uint SummonGorgedLurkingBasilisk = 50928;

    // QuenchingMist
    public const uint FlickeringFlames = 53504;

    // Quest13291_13292_13239_13261Data
    public const uint Ride = 59319;

    // BurstAtTheSeams
    public const uint BloatedAbominationFeignDeath = 52593;
    public const uint BurstAtTheSeamsBone = 52516;
    public const uint ExplodeAbominationMeat = 52520;
    public const uint ExplodeAbominationBloodyMeat = 52523;
    public const uint TrollExplosion = 52565;
    public const uint ExplodeTrollMeat = 52578;
    public const uint ExplodeTrollBloodyMeat = 52580;
    public const uint BurstAtTheSeams59576 = 59576; //script/knockback; That's Abominable
    public const uint BurstAtTheSeams59579 = 59579; //dummy
    public const uint BurstAtTheSeams52510 = 52510; //script/knockback; Fuel for the Fire
    public const uint BurstAtTheSeams52508 = 52508; //damage 20000
    public const uint BurstAtTheSeams59580 = 59580; //damage 50000
    public const uint AssignGhoulKillCreditToMaster = 59590;
    public const uint AssignGeistKillCreditToMaster = 60041;
    public const uint AssignSkeletonKillCreditToMaster = 60039;
    public const uint DrakkariSkullcrusherCredit = 52590;
    public const uint SummonDrakkariChieftain = 52616;
    public const uint DrakkariChieftainkKillCredit = 52620;

    // EscapeFromSilverbrook
    public const uint SummonWorgen = 48681;

    // DeathComesFromOnHigh
    public const uint ForgeCredit = 51974;
    public const uint TownHallCredit = 51977;
    public const uint ScarletHoldCredit = 51980;
    public const uint ChapelCredit = 51982;

    // QuestTheStormKing
    public const uint RideGymer = 43671;
    public const uint Grabbed = 55424;

    // QuestTheStormKingThrow
    public const uint VargulExplosion = 55569;

    // QuestTheHunterAndThePrince
    public const uint IllidanKillCredit = 61748;

    // RelicOfTheEarthenRing
    public const uint TotemOfTheEarthenRing = 66747;

    // Fumping
    public const uint SummonSandGnome = 39240;
    public const uint SummonBoneSlicer = 39241;

    // FearNoEvil
    public const uint RenewedLife = 93097;

    // ApplyHeatAndStir
    public const uint SpurtsAndSmoke = 38594;
    public const uint ErrorMix1 = 43376;
    public const uint ErrorMix2 = 43378;
    public const uint ErrorMix3 = 43970;
    public const uint SuccessfulMix = 43377;

    // TamingTheBeast
    public const uint TameIceClawBear = 19548;
    public const uint TameLargeCragBoar = 19674;
    public const uint TameSnowLeopard = 19687;
    public const uint TameAdultPlainstrider = 19688;
    public const uint TamePrairieStalker = 19689;
    public const uint TameSwoop = 19692;
    public const uint TameWebwoodLurker = 19693;
    public const uint TameDireMottledBoar = 19694;
    public const uint TameSurfCrawler = 19696;
    public const uint TameArmoredScorpid = 19697;
    public const uint TameNightsaberStalker = 19699;
    public const uint TameStrigidScreecher = 19700;
    public const uint TameBarbedCrawler = 30646;
    public const uint TameGreaterTimberstrider = 30653;
    public const uint TameNightstalker = 30654;
    public const uint TameCrazedDragonhawk = 30099;
    public const uint TameElderSpringpaw = 30102;
    public const uint TameMistbat = 30105;
    public const uint TameIceClawBear1 = 19597;
    public const uint TameLargeCragBoar1 = 19677;
    public const uint TameSnowLeopard1 = 19676;
    public const uint TameAdultPlainstrider1 = 19678;
    public const uint TamePrairieStalker1 = 19679;
    public const uint TameSwoop1 = 19680;
    public const uint TameWebwoodLurker1 = 19684;
    public const uint TameDireMottledBoar1 = 19681;
    public const uint TameSurfCrawler1 = 19682;
    public const uint TameArmoredScorpid1 = 19683;
    public const uint TameNightsaberStalker1 = 19685;
    public const uint TameStrigidScreecher1 = 19686;
    public const uint TameBarbedCrawler1 = 30647;
    public const uint TameGreaterTimberstrider1 = 30648;
    public const uint TameNightstalker1 = 30652;
    public const uint TameCrazedDragonhawk1 = 30100;
    public const uint TameElderSpringpaw1 = 30103;
    public const uint TameMistbat1 = 30104;

    // TributeSpells
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

struct CreatureIds
{
    // Quest55Data
    public const uint Morbent = 1200;
    public const uint WeakenedMorbent = 24782;

    // Quests6124_6129Data
    public const uint SicklyGazelle = 12296;
    public const uint CuredGazelle = 12297;
    public const uint SicklyDeer = 12298;
    public const uint CuredDeer = 12299;

    // Quest10255Data
    public const uint Helboar = 16880;
    public const uint Dreadtusk = 16992;

    // Quest11515Data
    public const uint FelbloodInitiate = 24918;
    public const uint EmaciatedFelblood = 24955;

    // Quest11730Data
    public const uint Scavengebot004A8 = 25752;
    public const uint Sentrybot57K = 25753;
    public const uint Defendotank66D = 25758;
    public const uint Scavengebot005B6 = 25792;
    public const uint Collectatron55D = 25793;

    // Quest12459Data
    public const uint ReanimatedFrostwyrm = 26841;
    public const uint WeakReanimatedFrostwyrm = 27821;
    public const uint Turgid = 27808;
    public const uint WeakTurgid = 27809;
    public const uint Deathgaze = 27122;
    public const uint WeakDeathgaze = 27807;

    // Quest12851Data
    public const uint Frostgiant = 29351;
    public const uint Frostworg = 29358;

    // Quest12659Data
    public const uint ScalpsKcBunny = 28622;

    // SalvagingLifesStength
    public const uint ShardKillCredit = 29303;

    // BattleStandard
    public const uint KingOfTheMountaintKc = 31766;

    // Quest12372Data
    public const uint WyrmrestTempleCredit = 27698;

    // Quest11010_11102_11023Data
    public const uint FelCannon2 = 23082;

    // Quest13291_13292_13239_13261Data
    public const uint Skytalon = 31583;
    public const uint Decoy = 31578;

    // BurstAtTheSeams
    public const uint DrakkariChieftaink = 29099;
    public const uint IcyGhoul = 31142;
    public const uint ViciousGeist = 31147;
    public const uint RisenAllianceSoldiers = 31205;
    public const uint RenimatedAbomination = 31692;

    // DeathComesFromOnHigh
    public const uint NewAvalonForge = 28525;
    public const uint NewAvalonTownHall = 28543;
    public const uint ScarletHold = 28542;
    public const uint ChapelOfTheCrimsonFlame = 28544;

    // FearNoEvil
    public const uint InjuredStormwindInfantry = 50047;

    // CallAttackMastiffs
    public const uint AttackMastiff = 36405;
}

struct MiscConst
{
    // BurstAtTheSeams
    public const uint AreaTheBrokenFront = 4507;
    public const uint AreaMordRetharTheDeathGate = 4508;
    public const uint QuestFuelForTheFire = 12690;

    // Recall_Eye_of_Acherus
    public const uint TheEyeOfAcherus = 51852;

    // ApplyHeatAndStir
    public const uint CreatureGenericTriggerLab = 24042;
    public const uint Talk0 = 0;
    public const uint Talk1 = 1;
}

[Script("spell_q55_sacred_cleansing", SpellEffectName.Dummy, 1, CreatureIds.Morbent, CreatureIds.WeakenedMorbent, true)] // 8913 - Sacred Cleansing
[Script("spell_q10255_administer_antidote", SpellEffectName.Dummy, 0, CreatureIds.Helboar, CreatureIds.Dreadtusk, true)] // 34665 - Administer Antidote
[Script("spell_q11515_fel_siphon_dummy", SpellEffectName.Dummy, 0, CreatureIds.FelbloodInitiate, CreatureIds.EmaciatedFelblood, true)] // 44936 - Quest - Fel Siphon Dummy
class spell_generic_quest_update_entry : SpellScript
{
    SpellEffectName _spellEffect;
    uint _effIndex;
    uint _originalEntry;
    uint _newEntry;
    bool _shouldAttack;
    TimeSpan _despawnTime;

    public spell_generic_quest_update_entry(SpellEffectName spellEffect, uint effIndex, uint originalEntry, uint newEntry, bool shouldAttack, TimeSpan despawnTime)
    {
        _spellEffect = spellEffect;
        _effIndex = effIndex;
        _originalEntry = originalEntry;
        _newEntry = newEntry;
        _shouldAttack = shouldAttack;
        _despawnTime = despawnTime;
    }

    public spell_generic_quest_update_entry(SpellEffectName spellEffect, uint effIndex, uint originalEntry, uint newEntry, bool shouldAttack)
    {
        _spellEffect = spellEffect;
        _effIndex = effIndex;
        _originalEntry = originalEntry;
        _newEntry = newEntry;
        _shouldAttack = shouldAttack;
        _despawnTime = TimeSpan.Zero;
    }

    void HandleDummy(uint effIndex)
    {
        Creature creatureTarget = GetHitCreature();
        if (creatureTarget != null && !creatureTarget.IsPet() && creatureTarget.GetEntry() == _originalEntry)
        {
            creatureTarget.UpdateEntry(_newEntry);
            if (_shouldAttack)
                creatureTarget.EngageWithTarget(GetCaster());

            if (_despawnTime != TimeSpan.FromSeconds(0))
                creatureTarget.DespawnOrUnsummon(_despawnTime);
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ThaumaturgyChannel);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        PreventDefaultAction();
        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(caster, SpellIds.ThaumaturgyChannel, false);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // 19512 - Apply Salve
class spell_q6124_6129_apply_salve : SpellScript
{
    TimeSpan despawnTime = TimeSpan.FromSeconds(30);

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
                        if (creatureTarget.GetEntry() == CreatureIds.SicklyGazelle)
                            newEntry = CreatureIds.CuredGazelle;
                        break;
                    case Team.Alliance:
                        if (creatureTarget.GetEntry() == CreatureIds.SicklyDeer)
                            newEntry = CreatureIds.CuredDeer;
                        break;
                    default:
                        break;
                }
                if (newEntry != 0)
                {
                    creatureTarget.UpdateEntry(newEntry);
                    creatureTarget.DespawnOrUnsummon(despawnTime);
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ForceShieldArcanePurpleX3, SpellIds.ScourgingCrystalController);
    }

    void HandleDummy(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (target != null)
            if (target.IsUnit() && target.HasAura(SpellIds.ForceShieldArcanePurpleX3))
                // Make sure nobody else is channeling the same target
                if (!target.HasAura(SpellIds.ScourgingCrystalController))
                    GetCaster().CastSpell(target, SpellIds.ScourgingCrystalController, GetCastItem());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 43882 - Scourging Crystal Controller Dummy
class spell_q11396_11399_scourging_crystal_controller_dummy : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ForceShieldArcanePurpleX3);
    }

    void HandleDummy(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (target != null)
            if (target.IsUnit())
                target.RemoveAurasDueToSpell(SpellIds.ForceShieldArcanePurpleX3);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 46023 - The Ultrasonic Screwdriver
class spell_q11730_ultrasonic_screwdriver : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer() && GetCastItem() != null;
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SummonScavengebot004A8, SpellIds.SummonSentrybot57K, SpellIds.SummonDefendotank66D, SpellIds.SummonScavengebot005B6, SpellIds.Summon55DCollectatron, SpellIds.RobotKillCredit);
    }

    void HandleDummy(uint effIndex)
    {
        Item castItem = GetCastItem();
        Unit caster = GetCaster();
        Creature target = GetHitCreature();
        if (target != null)
        {
            uint spellId = 0;
            switch (target.GetEntry())
            {
                case CreatureIds.Scavengebot004A8: spellId = SpellIds.SummonScavengebot004A8; break;
                case CreatureIds.Sentrybot57K: spellId = SpellIds.SummonSentrybot57K; break;
                case CreatureIds.Defendotank66D: spellId = SpellIds.SummonDefendotank66D; break;
                case CreatureIds.Scavengebot005B6: spellId = SpellIds.SummonScavengebot005B6; break;
                case CreatureIds.Collectatron55D: spellId = SpellIds.Summon55DCollectatron; break;
                default:
                    return;
            }
            caster.CastSpell(caster, spellId, castItem);
            caster.CastSpell(caster, SpellIds.RobotKillCredit, true);
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
    void HandleDummy(uint effIndex)
    {
        Creature creatureTarget = GetHitCreature();
        if (creatureTarget != null)
        {
            uint uiNewEntry = 0;
            switch (creatureTarget.GetEntry())
            {
                case CreatureIds.ReanimatedFrostwyrm:
                    uiNewEntry = CreatureIds.WeakReanimatedFrostwyrm;
                    break;
                case CreatureIds.Turgid:
                    uiNewEntry = CreatureIds.WeakTurgid;
                    break;
                case CreatureIds.Deathgaze:
                    uiNewEntry = CreatureIds.WeakDeathgaze;
                    break;
            }
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BananasFallToGround, SpellIds.OrangeFallsToGround, SpellIds.PapayaFallsToGround, SpellIds.SummonAdventurousDwarf);
    }

    void HandleDummy(uint effIndex)
    {
        uint spellId = SpellIds.BananasFallToGround;
        switch (RandomHelper.URand(0, 3))
        {
            case 1: spellId = SpellIds.OrangeFallsToGround; break;
            case 2: spellId = SpellIds.PapayaFallsToGround; break;
        }
        // sometimes, if you're lucky, you get a dwarf
        if (RandomHelper.randChance(5))
            spellId = SpellIds.SummonAdventurousDwarf;
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
    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            Unit target = GetTarget();
            // Already in fire
            if (target.HasAura(SpellIds.Ablaze))
                return;

            Player player = caster.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (player != null)
            {
                switch (target.GetEntry())
                {
                    case CreatureIds.Frostworg:
                        target.CastSpell(player, SpellIds.FrostworgCredit, true);
                        target.CastSpell(target, SpellIds.Immolation, true);
                        target.CastSpell(target, SpellIds.Ablaze, true);
                        break;
                    case CreatureIds.Frostgiant:
                        target.CastSpell(player, SpellIds.FrostgiantCredit, true);
                        target.CastSpell(target, SpellIds.Immolation, true);
                        target.CastSpell(target, SpellIds.Ablaze, true);
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
            caster.KilledMonsterCredit(CreatureIds.ScalpsKcBunny);
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
            caster.KilledMonsterCredit(CreatureIds.ShardKillCredit);
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
    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit target = GetHitUnit();
        uint triggeredSpellId = SpellIds.AllianceBattleStandardState;

        caster.HandleEmoteCommand(Emote.OneshotRoar);
        if (caster.IsVehicle())
        {
            Unit player = caster.GetVehicleKit().GetPassenger(0);
            if (player != null)
                player.ToPlayer().KilledMonsterCredit(CreatureIds.KingOfTheMountaintKc);
        }

        if (GetSpellInfo().Id == SpellIds.PlantHordeBattleStandard)
            triggeredSpellId = SpellIds.HordeBattleStandardState;

        target.RemoveAllAuras();
        target.CastSpell(target, triggeredSpellId, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 4336 - Jump Jets
class spell_q13280_13283_jump_jets : SpellScript
{
    void HandleCast()
    {
        Unit caster = GetCaster();
        if (caster.IsVehicle())
        {
            Unit rocketBunny = caster.GetVehicleKit().GetPassenger(1);
            if (rocketBunny != null)
                rocketBunny.CastSpell(rocketBunny, SpellIds.JumpRocketBlast, true);
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
    void HandleDummy(uint effIndex)
    {
        Creature target = GetHitCreature();
        if (target != null)
            target.CastSpell(GetCaster(), SpellIds.BunnyCreditBeam, false);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 49213 - Defending Wyrmrest Temple: Character Script Cast From Gossip
class spell_q12372_cast_from_gossip_trigger : SpellScript
{
    void HandleScript(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.SummonWyrmrestDefender, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 49370 - Wyrmrest Defender: Destabilize Azure Dragonshrine Effect
class spell_q12372_destabilize_azure_dragonshrine_dummy : SpellScript
{
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
                            player.KilledMonsterCredit(CreatureIds.WyrmrestTempleCredit);
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
class spell_q11010_q11102_q11023_aggro_check_aura : AuraScript
{
    void HandleTriggerSpell(AuraEffect aurEff)
    {
        Unit target = GetTarget();
        if (target != null)
            // On trigger proccing
            target.CastSpell(target, SpellIds.AggroCheck);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleTriggerSpell, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // 40112 - Knockdown Fel Cannon: The Aggro Check
class spell_q11010_q11102_q11023_aggro_check : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        Player playerTarget = GetHitPlayer();
        // Check if found player target is on fly mount or using flying form
        if (playerTarget != null && playerTarget.HasAuraType(AuraType.Fly) || playerTarget.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
            playerTarget.CastSpell(playerTarget, SpellIds.FlakCannonTrigger, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 40119 - Knockdown Fel Cannon: The Aggro Burst
class spell_q11010_q11102_q11023_aggro_burst : AuraScript
{
    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Unit target = GetTarget();
        if (target != null)
            // On each tick cast Choose Loc to trigger summon
            target.CastSpell(target, SpellIds.ChooseLoc);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 40056 - Knockdown Fel Cannon: Choose Loc
class spell_q11010_q11102_q11023_choose_loc : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        // Check for player that is in 65 y range
        List<Player> playerList = caster.GetPlayerListInGrid(65.0f);
        foreach (Player player in playerList)
        {
            // Check if found player target is on fly mount or using flying form
            if (player.HasAuraType(AuraType.Fly) || player.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                // Summom Fel Cannon (bunny version) at found player
                caster.SummonCreature(CreatureIds.FelCannon2, player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());
        }
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SummonGorgedLurkingBasilisk);
    }

    void HandleScriptEffect(uint effIndex)
    {
        if (GetHitAura() != null && GetHitAura().GetStackAmount() >= GetSpellInfo().StackAmount)
        {
            GetHitUnit().CastSpell(null, SpellIds.SummonGorgedLurkingBasilisk, true);
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlickeringFlames);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.FlickeringFlames);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicHeal));
    }
}

[Script] // 59318 - Grab Fake Soldier
class spell_q13291_q13292_q13239_q13261_frostbrood_skytalon_grab_decoy : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Ride);
    }

    void HandleDummy(uint effIndex)
    {
        if (GetHitCreature() == null)
            return;
        // To Do: Being triggered is hack, but in checkcast it doesn't pass aurastate requirements.
        // Beside that the decoy won't keep it's freeze animation state when enter.
        GetHitCreature().CastSpell(GetCaster(), SpellIds.Ride, true);
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
            && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
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

[Script] // 59576 - Burst at the Seams
class spell_q13264_q13276_q13288_q13289_burst_at_the_seams_59576 : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BurstAtTheSeams59576, SpellIds.BloatedAbominationFeignDeath, SpellIds.BurstAtTheSeams59579, SpellIds.BurstAtTheSeamsBone, SpellIds.ExplodeAbominationMeat, SpellIds.ExplodeAbominationBloodyMeat);
    }

    void HandleScript(uint effIndex)
    {
        Creature creature = GetCaster().ToCreature();
        if (creature != null)
        {
            creature.CastSpell(creature, SpellIds.BloatedAbominationFeignDeath, true);
            creature.CastSpell(creature, SpellIds.BurstAtTheSeams59579, true);
            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsBone, true);
            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsBone, true);
            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsBone, true);
            creature.CastSpell(creature, SpellIds.ExplodeAbominationMeat, true);
            creature.CastSpell(creature, SpellIds.ExplodeAbominationBloodyMeat, true);
            creature.CastSpell(creature, SpellIds.ExplodeAbominationBloodyMeat, true);
            creature.CastSpell(creature, SpellIds.ExplodeAbominationBloodyMeat, true);
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
        target.CastSpell(target, SpellIds.TrollExplosion, true);
        target.CastSpell(target, SpellIds.ExplodeAbominationMeat, true);
        target.CastSpell(target, SpellIds.ExplodeTrollMeat, true);
        target.CastSpell(target, SpellIds.ExplodeTrollMeat, true);
        target.CastSpell(target, SpellIds.ExplodeTrollBloodyMeat, true);
        target.CastSpell(target, SpellIds.BurstAtTheSeamsBone, true);
    }

    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        Unit caster = GetCaster();
        if (caster != null)
        {
            switch (target.GetEntry())
            {
                case CreatureIds.IcyGhoul:
                    target.CastSpell(caster, SpellIds.AssignGhoulKillCreditToMaster, true);
                    break;
                case CreatureIds.ViciousGeist:
                    target.CastSpell(caster, SpellIds.AssignGeistKillCreditToMaster, true);
                    break;
                case CreatureIds.RisenAllianceSoldiers:
                    target.CastSpell(caster, SpellIds.AssignSkeletonKillCreditToMaster, true);
                    break;
            }
        }
        target.CastSpell(target, SpellIds.BurstAtTheSeams59580, true);
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
        if (target != null)
            creature.SetReactState(ReactStates.Passive);
    }

    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        Creature creature = target.ToCreature();
        if (target != null)
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
            if (area != MiscConst.AreaTheBrokenFront && area != MiscConst.AreaMordRetharTheDeathGate)
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
                owner.CastSpell(owner, (uint)GetEffectValue(), true);
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
        return ValidateSpellInfo(SpellIds.BurstAtTheSeams52510, SpellIds.BurstAtTheSeams52508, SpellIds.BurstAtTheSeams59580, SpellIds.BurstAtTheSeamsBone, SpellIds.ExplodeAbominationMeat, SpellIds.ExplodeAbominationBloodyMeat);
    }

    public override bool Load()
    {
        return GetCaster().IsUnit();
    }

    void HandleKnockBack(uint effIndex)
    {
        Unit creature = GetCaster();
        if (creature != null)
        {
            Unit charmer = GetCaster().GetCharmerOrOwner();
            if (charmer != null)
            {
                Player player = charmer.ToPlayer();
                if (player != null && player.GetQuestStatus(MiscConst.QuestFuelForTheFire) == QuestStatus.Incomplete)
                {
                    creature.CastSpell(creature, SpellIds.BurstAtTheSeamsBone, true);
                    creature.CastSpell(creature, SpellIds.ExplodeAbominationMeat, true);
                    creature.CastSpell(creature, SpellIds.ExplodeAbominationBloodyMeat, true);
                    creature.CastSpell(creature, SpellIds.BurstAtTheSeams52508, true);
                    creature.CastSpell(creature, SpellIds.BurstAtTheSeams59580, true);

                    player.CastSpell(player, SpellIds.DrakkariSkullcrusherCredit, true);
                    ushort count = player.GetReqKillOrCastCurrentCount(MiscConst.QuestFuelForTheFire, (int)CreatureIds.DrakkariChieftaink);
                    if ((count % 20) == 0)
                        player.CastSpell(player, SpellIds.SummonDrakkariChieftain, true);
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SummonWorgen);
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.SummonWorgen, true);
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
        float angle = RandomHelper.FRand(0.75f, 1.25f) * MathF.PI;

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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ForgeCredit, SpellIds.TownHallCredit, SpellIds.ScarletHoldCredit, SpellIds.ChapelCredit);
    }

    void HandleDummy(uint effIndex)
    {
        uint spellId = 0;

        switch (GetHitCreature().GetEntry())
        {
            case CreatureIds.NewAvalonForge:
                spellId = SpellIds.ForgeCredit;
                break;
            case CreatureIds.NewAvalonTownHall:
                spellId = SpellIds.TownHallCredit;
                break;
            case CreatureIds.ScarletHold:
                spellId = SpellIds.ScarletHoldCredit;
                break;
            case CreatureIds.ChapelOfTheCrimsonFlame:
                spellId = SpellIds.ChapelCredit;
                break;
            default:
                return;
        }

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
    void HandleDummy(uint effIndex)
    {
        Player player = GetCaster().GetCharmerOrOwner().ToPlayer();
        if (player != null)
        {
            player.StopCastingCharm();
            player.StopCastingBindSight();
            player.RemoveAura(MiscConst.TheEyeOfAcherus);
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RideGymer);
    }

    void HandleScript(uint effIndex)
    {
        if (GetHitCreature() == null)
            return;

        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.BasePoint0, 2);
        GetHitCreature().CastSpell(GetCaster(), SpellIds.RideGymer, args);
        GetHitCreature().CastSpell(GetHitCreature(), SpellIds.Grabbed, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 55421 - Gymer's Throw
class spell_q12919_gymers_throw : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Unit caster = GetCaster();
        if (caster.IsVehicle())
        {
            Unit passenger = caster.GetVehicleKit().GetPassenger(1);
            if (passenger != null)
            {
                passenger.ExitVehicle();
                caster.CastSpell(passenger, SpellIds.VargulExplosion, true);
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.IllidanKillCredit);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        if (caster.IsVehicle())
        {
            Unit passenger = caster.GetVehicleKit().GetPassenger(0);
            if (passenger != null)
                passenger.CastSpell(passenger, SpellIds.IllidanKillCredit, true);
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
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TotemOfTheEarthenRing);
    }

    void HandleScriptEffect(uint effIndex)
    {
        Player player = GetHitPlayer();
        if (player != null)
            player.CastSpell(player, SpellIds.TotemOfTheEarthenRing, new CastSpellExtraArgs(TriggerCastFlags.FullMask)); // ignore reagent cost, consumed by quest
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 39238 - Fumping
class spell_q10929_fumping : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SummonSandGnome, SpellIds.SummonBoneSlicer);
    }

    void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(caster, RandomHelper.URand(SpellIds.SummonSandGnome, SpellIds.SummonBoneSlicer), true);
    }

    public override void Register()
    {
        OnEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 93072 - Get Our Boys Back Dummy
class spell_q28813_get_our_boys_back_dummy : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RenewedLife);
    }

    void HandleDummyEffect()
    {
        Unit caster = GetCaster();
        Creature injuredStormwindInfantry = caster.FindNearestCreature(CreatureIds.InjuredStormwindInfantry, 5.0f, true);
        if (injuredStormwindInfantry != null)
        {
            injuredStormwindInfantry.SetCreatorGUID(caster.GetGUID());
            injuredStormwindInfantry.CastSpell(injuredStormwindInfantry, SpellIds.RenewedLife, true);
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

// 13790 13793 13811 13814 - Among the Champions
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

[Script] // 43972 - Mixing Blood
class spell_q11306_mixing_blood : SpellScript
{
    void HandleEffect(uint effIndex)
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            Creature trigger = caster.FindNearestCreature(MiscConst.CreatureGenericTriggerLab, 100.0f);
            if (trigger != null)
                trigger.GetAI().DoCastSelf(SpellIds.SpurtsAndSmoke);
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
                spellId = SpellIds.ErrorMix1;
            else if (chance < 60)
                spellId = SpellIds.ErrorMix2;
            else if (chance < 90)
                spellId = SpellIds.ErrorMix3;
            else // 10% chance of successful cast
                spellId = SpellIds.SuccessfulMix;

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
            Creature trigger = caster.FindNearestCreature(MiscConst.CreatureGenericTriggerLab, 100.0f);
            if (trigger != null)
                trigger.GetAI().Talk(MiscConst.Talk0, caster);
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
            Creature trigger = caster.FindNearestCreature(MiscConst.CreatureGenericTriggerLab, 100.0f);
            if (trigger != null)
                trigger.GetAI().Talk(MiscConst.Talk1, caster);
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
            Unit owner = GetCaster();
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

[Script]
class spell_quest_taming_the_beast : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TameIceClawBear1, SpellIds.TameLargeCragBoar1, SpellIds.TameSnowLeopard1, SpellIds.TameAdultPlainstrider1, SpellIds.TamePrairieStalker1, SpellIds.TameSwoop1,
            SpellIds.TameWebwoodLurker1, SpellIds.TameDireMottledBoar1, SpellIds.TameSurfCrawler1, SpellIds.TameArmoredScorpid1, SpellIds.TameNightsaberStalker1, SpellIds.TameStrigidScreecher1,
            SpellIds.TameBarbedCrawler1, SpellIds.TameGreaterTimberstrider1, SpellIds.TameNightstalker1, SpellIds.TameCrazedDragonhawk1, SpellIds.TameElderSpringpaw1, SpellIds.TameMistbat1);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetCaster() == null || !GetCaster().IsAlive() || !GetTarget().IsAlive())
            return;

        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        uint finalSpellId = 0;
        switch (GetId())
        {
            case SpellIds.TameIceClawBear: finalSpellId = SpellIds.TameIceClawBear1; break;
            case SpellIds.TameLargeCragBoar: finalSpellId = SpellIds.TameLargeCragBoar1; break;
            case SpellIds.TameSnowLeopard: finalSpellId = SpellIds.TameSnowLeopard1; break;
            case SpellIds.TameAdultPlainstrider: finalSpellId = SpellIds.TameAdultPlainstrider1; break;
            case SpellIds.TamePrairieStalker: finalSpellId = SpellIds.TamePrairieStalker1; break;
            case SpellIds.TameSwoop: finalSpellId = SpellIds.TameSwoop1; break;
            case SpellIds.TameWebwoodLurker: finalSpellId = SpellIds.TameWebwoodLurker1; break;
            case SpellIds.TameDireMottledBoar: finalSpellId = SpellIds.TameDireMottledBoar1; break;
            case SpellIds.TameSurfCrawler: finalSpellId = SpellIds.TameSurfCrawler1; break;
            case SpellIds.TameArmoredScorpid: finalSpellId = SpellIds.TameArmoredScorpid1; break;
            case SpellIds.TameNightsaberStalker: finalSpellId = SpellIds.TameNightsaberStalker1; break;
            case SpellIds.TameStrigidScreecher: finalSpellId = SpellIds.TameStrigidScreecher1; break;
            case SpellIds.TameBarbedCrawler: finalSpellId = SpellIds.TameBarbedCrawler1; break;
            case SpellIds.TameGreaterTimberstrider: finalSpellId = SpellIds.TameGreaterTimberstrider1; break;
            case SpellIds.TameNightstalker: finalSpellId = SpellIds.TameNightstalker1; break;
            case SpellIds.TameCrazedDragonhawk: finalSpellId = SpellIds.TameCrazedDragonhawk1; break;
            case SpellIds.TameElderSpringpaw: finalSpellId = SpellIds.TameElderSpringpaw1; break;
            case SpellIds.TameMistbat: finalSpellId = SpellIds.TameMistbat1; break;
        }

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
            && Global.ObjectMgr.GetQuestTemplate((uint)spellInfo.GetEffect(1).CalcValue()) != null;
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

// 24194 - Uther's Tribute
[Script] // 24195 - Grom's Tribute
class spell_quest_uther_grom_tribute : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GromsTrollTribute, SpellIds.UthersHumanTribute, SpellIds.GromsTaurenTribute, SpellIds.UthersGnomeTribute, SpellIds.GromsUndeadTribute, SpellIds.UthersDwarfTribute,
            SpellIds.GromsOrcTribute, SpellIds.UthersNightelfTribute, SpellIds.GromsBloodelfTribute, SpellIds.UthersDraeneiTribute);
    }

    void HandleScript(uint effIndex)
    {
        Player caster = GetCaster().ToPlayer();
        if (caster == null)
            return;

        uint spell = 0;
        switch (caster.GetRace())
        {
            case Race.Troll:
                spell = SpellIds.GromsTrollTribute;
                break;
            case Race.Tauren:
                spell = SpellIds.GromsTaurenTribute;
                break;
            case Race.Undead:
                spell = SpellIds.GromsUndeadTribute;
                break;
            case Race.Orc:
                spell = SpellIds.GromsOrcTribute;
                break;
            case Race.BloodElf:
                spell = SpellIds.GromsBloodelfTribute;
                break;
            case Race.Human:
                spell = SpellIds.UthersHumanTribute;
                break;
            case Race.Gnome:
                spell = SpellIds.UthersGnomeTribute;
                break;
            case Race.Dwarf:
                spell = SpellIds.UthersDwarfTribute;
                break;
            case Race.NightElf:
                spell = SpellIds.UthersNightelfTribute;
                break;
            case Race.Draenei:
                spell = SpellIds.UthersDraeneiTribute;
                break;
            default: break;
        }

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
    void HandleEffect(uint effIndex)
    {
        Unit caster = GetCaster();
        caster.SummonCreature(CreatureIds.AttackMastiff, -1944.573f, 2657.402f, 0.994939f, 1.691919f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        caster.SummonCreature(CreatureIds.AttackMastiff, -2005.65f, 2663.526f, -2.086935f, 0.5942355f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        caster.SummonCreature(CreatureIds.AttackMastiff, -1996.506f, 2651.347f, -1.011707f, 0.8185352f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        caster.SummonCreature(CreatureIds.AttackMastiff, -1972.352f, 2640.07f, 1.080288f, 1.217854f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        caster.SummonCreature(CreatureIds.AttackMastiff, -1949.322f, 2642.76f, 1.242482f, 1.58074f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        caster.SummonCreature(CreatureIds.AttackMastiff, -1993.94f, 2672.535f, -2.322549f, 0.5766209f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        caster.SummonCreature(CreatureIds.AttackMastiff, -1982.724f, 2662.8f, -1.773986f, 0.8628055f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        caster.SummonCreature(CreatureIds.AttackMastiff, -1973.301f, 2655.475f, -0.7831049f, 1.098415f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
        caster.SummonCreature(CreatureIds.AttackMastiff, -1956.509f, 2650.655f, 1.350571f, 1.441473f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(1));
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleEffect, 1, SpellEffectName.SendEvent));
    }
}
