// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Quest
{
    struct SpellIds
    {
        //Thaumaturgychannel        
        public const uint ThaumaturgyChannel = 21029;

        //Quest11396-11399
        public const uint ForceShieldArcanePurpleX3 = 43874;
        public const uint ScourgingCrystalController = 43878;

        //Quest11730
        public const uint SummonScavengebot004a8 = 46063;
        public const uint SummonSentrybot57k = 46068;
        public const uint SummonDefendotank66d = 46058;
        public const uint SummonScavengebot005b6 = 46066;
        public const uint Summon55dCollectatron = 46034;
        public const uint RobotKillCredit = 46027;

        //Quest12634
        public const uint BananasFallToGround = 51836;
        public const uint OrangeFallsToGround = 51837;
        public const uint PapayaFallsToGround = 51839;
        public const uint SummonAdventurousDwarf = 52070;

        //Quest12851
        public const uint FrostgiantCredit = 58184;
        public const uint FrostworgCredit = 58183;
        public const uint Immolation = 54690;
        public const uint Ablaze = 54683;

        //Quest12937
        public const uint TriggerAidOfTheEarthen = 55809;

        //Symboloflife
        public const uint PermanentFeignDeath = 29266;

        //BattleStandard
        public const uint PlantHordeBattleStandard = 59643;
        public const uint HordeBattleStandardState = 59642;
        public const uint AllianceBattleStandardState = 4339;
        public const uint JumpRocketBlast = 4340;

        //BreakfastOfChampions
        public const uint SummonDeepJormungar = 66510;
        public const uint StormforgedMoleMachine = 66492;

        //Leavenothingtochance
        public const uint UpperMineShaftCredit = 48744;
        public const uint LowerMineShaftCredit = 48745;

        //Focusonthebeach
        public const uint BunnyCreditBeam = 47390;

        //Defendingwyrmresttemple
        public const uint SummonWyrmrestDefender = 49207;

        //Quest11010 11102 11023
        public const uint FlakCannonTrigger = 40110;
        public const uint ChooseLoc = 40056;
        public const uint AggroCheck = 40112;

        //Spellzuldrakrat
        public const uint SummonGorgedLurkingBasilisk = 50928;

        //Quenchingmist
        public const uint FlickeringFlames = 53504;

        //Quest13291 13292 13239 13261
        public const uint Ride = 59319;

        //Bearflankmaster
        public const uint BearFlankMaster = 56565;
        public const uint CreateBearFlank = 56566;
        public const uint BearFlankFail = 56569;

        //BurstAtTheSeams
        public const uint BloatedAbominationFeignDeath = 52593;
        public const uint BurstAtTheSeamsBone = 52516;
        public const uint ExplodeAbominationMeat = 52520;
        public const uint ExplodeAbominationBloodyMeat = 52523;
        public const uint TrollExplosion = 52565;
        public const uint ExplodeTrollMeat = 52578;
        public const uint ExplodeTrollBloodyMeat = 52580;

        public const uint BurstAtTheSeams59576 = 59576; //Script/Knockback; That'S Abominable
        public const uint BurstAtTheSeams59579 = 59579; //Dummy
        public const uint BurstAtTheSeams52510 = 52510; //Script/Knockback; Fuel For The Fire
        public const uint BurstAtTheSeams52508 = 52508; //Damage 20000
        public const uint BurstAtTheSeams59580 = 59580; //Damage 50000

        public const uint AssignGhoulKillCreditToMaster = 59590;
        public const uint AssignGeistKillCreditToMaster = 60041;
        public const uint AssignSkeletonKillCreditToMaster = 60039;

        public const uint DrakkariSkullcrusherCredit = 52590;
        public const uint SummonDrakkariChieftain = 52616;
        public const uint DrakkariChieftainkKillCredit = 52620;

        // Tamingthebeast
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

        //TributeSpells
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

        //Escapefromsilverbrook
        public const uint SummonWorgen = 48681;

        //Deathcomesfromonhigh
        public const uint ForgeCredit = 51974;
        public const uint TownHallCredit = 51977;
        public const uint ScarletHoldCredit = 51980;
        public const uint ChapelCredit = 51982;

        //RecallEyeOfAcherus
        public const uint TheEyeOfAcherus = 51852;

        //QuestTheStormKing
        public const uint RideGymer = 43671;
        public const uint Grabbed = 55424;

        //QuestTheStormKingThrow
        public const uint VargulExplosion = 55569;

        //QuestTheHunterAndThePrince
        public const uint IllidanKillCredit = 61748;

        //Relicoftheearthenring
        public const uint TotemOfTheEarthenRing = 66747;

        //Fumping
        public const uint SummonSandGnome = 39240;
        public const uint SummonBoneSlicer = 39241;

        //Fearnoevil
        public const uint RenewedLife = 93097;
    }

    struct CreatureIds
    {
        //Quest55
        public const uint Morbent = 1200;
        public const uint WeakenedMorbent = 24782;

        //Quests6124 6129
        public const uint SicklyGazelle = 12296;
        public const uint CuredGazelle = 12297;
        public const uint SicklyDeer = 12298;
        public const uint CuredDeer = 12299;

        //Quest10255        
        public const uint Helboar = 16880;
        public const uint Dreadtusk = 16992;

        //Quest11515
        public const uint FelbloodInitiate = 24918;
        public const uint EmaciatedFelblood = 24955;

        //Quest11730
        public const uint Scavengebot004a8 = 25752;
        public const uint Sentrybot57k = 25753;
        public const uint Defendotank66d = 25758;
        public const uint Scavengebot005b6 = 25792;
        public const uint Npc55dCollectatron = 25793;

        //Quest12459        
        public const uint ReanimatedFrostwyrm = 26841;
        public const uint WeakReanimatedFrostwyrm = 27821;
        public const uint Turgid = 27808;
        public const uint WeakTurgid = 27809;
        public const uint Deathgaze = 27122;
        public const uint WeakDeathgaze = 27807;

        //Quest12851
        public const uint Frostgiant = 29351;
        public const uint Frostworg = 29358;

        //Quest12937
        public const uint FallenEarthenDefender = 30035;

        //Quest12659
        public const uint ScalpsKcBunny = 28622;

        //Salvaginglifesstength
        public const uint ShardKillCredit = 29303;

        //Battlestandard
        public const uint KingOfTheMountaintKc = 31766;

        //Hodirshelm
        public const uint Killcredit = 30210; // Hodir'S Helm Kc Bunny
        public const uint IceSpikeBunny = 30215;

        //Leavenothingtochance
        public const uint UpperMineShaft = 27436;
        public const uint LowerMineShaft = 27437;

        //Quest12372
        public const uint WyrmrestTempleCredit = 27698;

        //Quest11010 11102 11023        
        public const uint FelCannon2 = 23082;

        //Quest13291 13292 13239 13261
        public const uint Skytalon = 31583;
        public const uint Decoy = 31578;

        //Burstattheseams
        public const uint DrakkariChieftaink = 29099;
        public const uint IcyGhoul = 31142;
        public const uint ViciousGeist = 31147;
        public const uint RisenAllianceSoldiers = 31205;
        public const uint RenimatedAbomination = 31692;

        //Deathcomesfromonhigh
        public const uint NewAvalonForge = 28525;
        public const uint NewAvalonTownHall = 28543;
        public const uint ScarletHold = 28542;
        public const uint ChapelOfTheCrimsonFlame = 28544;

        //Fearnoevil
        public const uint InjuredStormwindInfantry = 50047;
    }

    struct Misc
    {
        //Quests6124 6129
        public static TimeSpan DespawnTime = TimeSpan.FromSeconds(30);

        //HodirsHelm
        public const byte Say1 = 1;
        public const byte Say2 = 2;

        //Acleansingsong
        public const uint AreaIdBittertidelake = 4385;
        public const uint AreaIdRiversheart = 4290;
        public const uint AreaIdWintergraspriver = 4388;

        //Quest12372
        public const uint WhisperOnHitByForceWhisper = 1;

        //BurstAtTheSeams
        public const uint AreaTheBrokenFront = 4507;
        public const uint AreaMordRetharTheDeathGate = 4508;
        public const uint QuestFuelForTheFire = 12690;
    }

    // http://www.wowhead.com/quest=55 Morbent Fel
    // 8913 Sacred Cleansing
    // http://www.wowhead.com/quest=10255 Testing the Antidote
    // 34665 Administer Antidote
    // http://www.wowhead.com/quest=11515 Blood for Blood
    // 44936 Quest - Fel Siphon Dummy
    [Script("spell_q55_sacred_cleansing", SpellEffectName.Dummy, 1u, CreatureIds.Morbent, CreatureIds.WeakenedMorbent, true, 0)]
    [Script("spell_q10255_administer_antidote", SpellEffectName.Dummy, 0u, CreatureIds.Helboar, CreatureIds.Dreadtusk, true, 0)]
    [Script("spell_q11515_fel_siphon_dummy", SpellEffectName.Dummy, 0u, CreatureIds.FelbloodInitiate, CreatureIds.EmaciatedFelblood, true, 0)]
    class spell_generic_quest_update_entry : SpellScript
    {
        public spell_generic_quest_update_entry(SpellEffectName spellEffect, uint effIndex, uint originalEntry, uint newEntry, bool shouldAttack, uint despawnTime)
        {
            _spellEffect = spellEffect;
            _effIndex = (byte)effIndex;
            _originalEntry = originalEntry;
            _newEntry = newEntry;
            _shouldAttack = shouldAttack;
            _despawnTime = despawnTime;
        }

        void HandleDummy(uint effIndex)
        {
            Creature creatureTarget = GetHitCreature();
            if (creatureTarget)
            {
                if (!creatureTarget.IsPet() && creatureTarget.GetEntry() == _originalEntry)
                {
                    creatureTarget.UpdateEntry(_newEntry);
                    if (_shouldAttack)
                        creatureTarget.EngageWithTarget(GetCaster());

                    if (_despawnTime != 0)
                        creatureTarget.DespawnOrUnsummon(TimeSpan.FromMilliseconds(_despawnTime));
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, _effIndex, _spellEffect));
        }

        SpellEffectName _spellEffect;
        byte _effIndex;
        uint _originalEntry;
        uint _newEntry;
        bool _shouldAttack;
        uint _despawnTime;
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
            if (caster)
                caster.CastSpell(caster, SpellIds.ThaumaturgyChannel, false);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    // http://www.wowhead.com/quest=6124 Curing the Sick (A)
    // http://www.wowhead.com/quest=6129 Curing the Sick (H)
    [Script] // 19512 Apply Salve
    class spell_q6124_6129_apply_salve : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (GetCastItem())
            {
                Creature creatureTarget = GetHitCreature();
                if (creatureTarget)
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
                    }
                    if (newEntry != 0)
                    {
                        creatureTarget.UpdateEntry(newEntry);
                        creatureTarget.DespawnOrUnsummon(Misc.DespawnTime);
                        caster.KilledMonsterCredit(newEntry);
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 50133 - Scourging Crystal Controller
    class spell_q11396_11399_scourging_crystal_controller : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.ForceShieldArcanePurpleX3, SpellIds.ScourgingCrystalController);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                if (target.IsTypeId(TypeId.Unit) && target.HasAura(SpellIds.ForceShieldArcanePurpleX3))
                    // Make sure nobody else is channeling the same target
                    if (!target.HasAura(SpellIds.ScourgingCrystalController))
                        GetCaster().CastSpell(target, SpellIds.ScourgingCrystalController, new CastSpellExtraArgs(GetCastItem()));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 43882 - Scourging Crystal Controller Dummy
    class spell_q11396_11399_scourging_crystal_controller_dummy : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.ForceShieldArcanePurpleX3);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                if (target.IsTypeId(TypeId.Unit))
                    target.RemoveAurasDueToSpell(SpellIds.ForceShieldArcanePurpleX3);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=11730 Master and Servant
    [Script] // 46023 The Ultrasonic Screwdriver
    class spell_q11730_ultrasonic_screwdriver : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player) && GetCastItem();
        }

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.SummonScavengebot004a8, SpellIds.SummonSentrybot57k, SpellIds.SummonDefendotank66d,
                SpellIds.SummonScavengebot005b6, SpellIds.Summon55dCollectatron, SpellIds.RobotKillCredit);
        }

        void HandleDummy(uint effIndex)
        {
            Item castItem = GetCastItem();
            Unit caster = GetCaster();

            Creature target = GetHitCreature();
            if (target)
            {
                uint spellId;
                switch (target.GetEntry())
                {
                    case CreatureIds.Scavengebot004a8:
                        spellId = SpellIds.SummonScavengebot004a8;
                        break;
                    case CreatureIds.Sentrybot57k:
                        spellId = SpellIds.SummonSentrybot57k;
                        break;
                    case CreatureIds.Defendotank66d:
                        spellId = SpellIds.SummonDefendotank66d;
                        break;
                    case CreatureIds.Scavengebot005b6:
                        spellId = SpellIds.SummonScavengebot005b6;
                        break;
                    case CreatureIds.Npc55dCollectatron:
                        spellId = SpellIds.Summon55dCollectatron;
                        break;
                    default:
                        return;
                }
                caster.CastSpell(caster, spellId, new CastSpellExtraArgs(castItem));
                caster.CastSpell(caster, SpellIds.RobotKillCredit, true);
                target.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=12459 That Which Creates Can Also Destroy
    [Script] // 49587 Seeds of Nature's Wrath
    class spell_q12459_seeds_of_natures_wrath : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Creature creatureTarget = GetHitCreature();
            if (creatureTarget)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=12634 Some Make Lemonade, Some Make Liquor
    [Script] // 51840 Despawn Fruit Tosser
    class spell_q12634_despawn_fruit_tosser : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.BananasFallToGround, SpellIds.OrangeFallsToGround, SpellIds.PapayaFallsToGround, SpellIds.SummonAdventurousDwarf);
        }

        void HandleDummy(uint effIndex)
        {
            uint spellId = SpellIds.BananasFallToGround;
            switch (RandomHelper.URand(0, 3))
            {
                case 1:
                    spellId = SpellIds.OrangeFallsToGround;
                    break;
                case 2:
                    spellId = SpellIds.PapayaFallsToGround;
                    break;
            }
            // sometimes, if you're lucky, you get a dwarf
            if (RandomHelper.randChance(5))
                spellId = SpellIds.SummonAdventurousDwarf;
            GetCaster().CastSpell(GetCaster(), spellId, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=12851 Going Bearback
    [Script] // 54798 FLAMING Arrow Triggered Effect
    class spell_q12851_going_bearback : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                Unit target = GetTarget();
                // Already in fire
                if (target.HasAura(SpellIds.Ablaze))
                    return;
                Player player = caster.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (player)
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
            AfterEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    // http://www.wowhead.com/quest=12659 Scalps!
    [Script] // 52090 Ahunae's Knife
    class spell_q12659_ahunaes_knife : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();

            Creature target = GetHitCreature();
            if (target)
            {
                target.DespawnOrUnsummon();
                caster.KilledMonsterCredit(CreatureIds.ScalpsKcBunny);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 54190 - Lifeblood Dummy
    class spell_q12805_lifeblood_dummy : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleScript(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();

            Creature target = GetHitCreature();
            if (target)
            {
                caster.KilledMonsterCredit(CreatureIds.ShardKillCredit);
                target.CastSpell(target, (uint)GetEffectValue(), true);
                target.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    /*
     http://www.wowhead.com/quest=13283 King of the Mountain
     http://www.wowhead.com/quest=13280 King of the Mountain
    */
    // 59643 Plant Horde Battle Standard
    [Script] // 4338 Plant Alliance Battle Standard
    class spell_q13280_13283_plant_battle_standard : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            uint triggeredSpellID = SpellIds.AllianceBattleStandardState;

            caster.HandleEmoteCommand(Emote.OneshotRoar);
            if (caster.IsVehicle())
            {
                Unit player = caster.GetVehicleKit().GetPassenger(0);
                if (player)
                    player.ToPlayer().KilledMonsterCredit(CreatureIds.KingOfTheMountaintKc);
            }

            if (GetSpellInfo().Id == SpellIds.PlantHordeBattleStandard)
                triggeredSpellID = SpellIds.HordeBattleStandardState;

            target.RemoveAllAuras();
            target.CastSpell(target, triggeredSpellID, true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnCast.Add(new CastHandler(HandleCast));
        }
    }

    [Script]
    class spell_q12279_cast_net : SpellScript
    {
        void HandleActiveObject(uint effIndex)
        {
            GetHitGObj().SetLootState(LootState.JustDeactivated);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleActiveObject, 1, SpellEffectName.ActivateObject));
        }
    }

    [Script] // 50546 - The Focus on the Beach: Ley Line Focus Control Ring Effect
    class spell_q12066_bunny_kill_credit : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Creature target = GetHitCreature();
            if (target)
                target.CastSpell(GetCaster(), SpellIds.BunnyCreditBeam, false);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // http://www.wowhead.com/quest=12372 Defending Wyrmrest Temple
    [Script] // 49370 - Wyrmrest Defender: Destabilize Azure Dragonshrine Effect
    class spell_q12372_destabilize_azure_dragonshrine_dummy : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            if (GetHitCreature())
            {
                Unit caster = GetOriginalCaster();
                if (caster)
                {
                    Vehicle vehicle = caster.GetVehicleKit();
                    if (vehicle)
                    {
                        Unit passenger = vehicle.GetPassenger(0);
                        if (passenger)
                        {
                            Player player = passenger.ToPlayer();
                            if (player)
                                player.KilledMonsterCredit(CreatureIds.WyrmrestTempleCredit);
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 40113 Knockdown Fel Cannon: The Aggro Check Aura
    class spell_q11010_q11102_q11023_aggro_check_aura : AuraScript
    {
        void HandleTriggerSpell(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (target)
                // On trigger proccing
                target.CastSpell(target, SpellIds.AggroCheck);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleTriggerSpell, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 40112 Knockdown Fel Cannon: The Aggro Check
    class spell_q11010_q11102_q11023_aggro_check : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Player playerTarget = GetHitPlayer();
            if (playerTarget)
                // Check if found player target is on fly mount or using flying form
                if (playerTarget.HasAuraType(AuraType.Fly) || playerTarget.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                    playerTarget.CastSpell(playerTarget, SpellIds.FlakCannonTrigger, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }
    
    [Script] // 40119 Knockdown Fel Cannon: The Aggro Burst
    class spell_q11010_q11102_q11023_aggro_burst : AuraScript
    {
        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            if (target)
                // On each tick cast Choose Loc to trigger summon
                target.CastSpell(target, SpellIds.ChooseLoc);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }
    
    [Script] // 40056 Knockdown Fel Cannon: Choose Loc
    class spell_q11010_q11102_q11023_choose_loc : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            // Check for player that is in 65 y range
            List<Unit> playerList = new();
            AnyPlayerInObjectRangeCheck checker = new(caster, 65.0f);
            PlayerListSearcher searcher = new(caster, playerList, checker);
            Cell.VisitWorldObjects(caster, searcher, 65.0f);
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
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnCheckCast.Add(new CheckCastHandler(CheckRequirement));
        }
    }
    
    [Script] // 50894 - Zul'Drak Rat
    class spell_q12527_zuldrak_rat : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SummonGorgedLurkingBasilisk);
        }

        void HandleScriptEffect(uint effIndex)
        {
            if (GetHitAura() != null && GetHitAura().GetStackAmount() >= GetSpellInfo().StackAmount)
            {
                GetHitUnit().CastSpell((Unit)null, SpellIds.SummonGorgedLurkingBasilisk, true);
                Creature basilisk = GetHitUnit().ToCreature();
                if (basilisk)
                    basilisk.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
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
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCasterBack));
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicHeal));
        }
    }

    [Script] // 59318 - Grab Fake Soldier
    class spell_q13291_q13292_q13239_q13261_frostbrood_skytalon_grab_decoy : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.Ride);
        }

        void HandleDummy(uint effIndex)
        {
            if (!GetHitCreature())
                return;
            // TO DO: Being triggered is hack, but in checkcast it doesn't pass aurastate requirements.
            // Beside that the decoy won't keep it's freeze animation state when enter.
            GetHitCreature().CastSpell(GetCaster(), SpellIds.Ride, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCasterBack));
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
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
        }
    }

    // 57385 - Argent Cannon    
    [Script] // 57412 - Reckoning Bomb
    class spell_q13086_cannons_target : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return !spellInfo.GetEffects().Empty() && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void HandleEffectDummy(uint effIndex)
        {
            WorldLocation pos = GetExplTargetDest();
            if (pos != null)
                GetCaster().CastSpell(pos.GetPosition(), (uint)GetEffectValue(), new CastSpellExtraArgs(true));
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
                if (area != Misc.AreaTheBrokenFront && area != Misc.AreaMordRetharTheDeathGate)
                    creature.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 52510 - Burst at the Seams
    class spell_q12690_burst_at_the_seams_52510 : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BurstAtTheSeams52510, SpellIds.BurstAtTheSeams52508, SpellIds.BurstAtTheSeams59580, 
                SpellIds.BurstAtTheSeamsBone, SpellIds.ExplodeAbominationMeat, SpellIds.ExplodeAbominationBloodyMeat);
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
                        if (player.GetQuestStatus(Misc.QuestFuelForTheFire) == QuestStatus.Incomplete)
                        {
                            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsBone, true);
                            creature.CastSpell(creature, SpellIds.ExplodeAbominationMeat, true);
                            creature.CastSpell(creature, SpellIds.ExplodeAbominationBloodyMeat, true);
                            creature.CastSpell(creature, SpellIds.BurstAtTheSeams52508, true);
                            creature.CastSpell(creature, SpellIds.BurstAtTheSeams59580, true);

                            player.CastSpell(player, SpellIds.DrakkariSkullcrusherCredit, true);
                            ushort count = player.GetReqKillOrCastCurrentCount(Misc.QuestFuelForTheFire, (int)CreatureIds.DrakkariChieftaink);
                            if ((count % 20) == 0)
                                player.CastSpell(player, SpellIds.SummonDrakkariChieftain, true);
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
            OnEffectHitTarget.Add(new EffectHandler(HandleKnockBack, 1, SpellEffectName.KnockBack));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_quest_taming_the_beast : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TameIceClawBear, SpellIds.TameLargeCragBoar, SpellIds.TameSnowLeopard, SpellIds.TameAdultPlainstrider, SpellIds.TamePrairieStalker, SpellIds.TameSwoop,
                SpellIds.TameWebwoodLurker, SpellIds.TameDireMottledBoar, SpellIds.TameSurfCrawler, SpellIds.TameArmoredScorpid, SpellIds.TameNightsaberStalker, SpellIds.TameStrigidScreecher,
                SpellIds.TameBarbedCrawler, SpellIds.TameGreaterTimberstrider, SpellIds.TameNightstalker, SpellIds.TameCrazedDragonhawk, SpellIds.TameElderSpringpaw, SpellIds.TameMistbat);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (!GetCaster() || !GetCaster().IsAlive() || !GetTarget().IsAlive())
                return;

            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            uint finalSpellId = GetId() switch
            {
                SpellIds.TameIceClawBear => SpellIds.TameIceClawBear1,
                SpellIds.TameLargeCragBoar => SpellIds.TameLargeCragBoar1,
                SpellIds.TameSnowLeopard => SpellIds.TameSnowLeopard1,
                SpellIds.TameAdultPlainstrider => SpellIds.TameAdultPlainstrider1,
                SpellIds.TamePrairieStalker => SpellIds.TamePrairieStalker1,
                SpellIds.TameSwoop => SpellIds.TameSwoop1,
                SpellIds.TameWebwoodLurker => SpellIds.TameWebwoodLurker1,
                SpellIds.TameDireMottledBoar => SpellIds.TameDireMottledBoar1,
                SpellIds.TameSurfCrawler => SpellIds.TameSurfCrawler1,
                SpellIds.TameArmoredScorpid => SpellIds.TameArmoredScorpid1,
                SpellIds.TameNightsaberStalker => SpellIds.TameNightsaberStalker1,
                SpellIds.TameStrigidScreecher => SpellIds.TameStrigidScreecher1,
                SpellIds.TameBarbedCrawler => SpellIds.TameBarbedCrawler1,
                SpellIds.TameGreaterTimberstrider => SpellIds.TameGreaterTimberstrider1,
                SpellIds.TameNightstalker => SpellIds.TameNightstalker1,
                SpellIds.TameCrazedDragonhawk => SpellIds.TameCrazedDragonhawk1,
                SpellIds.TameElderSpringpaw => SpellIds.TameElderSpringpaw1,
                SpellIds.TameMistbat => SpellIds.TameMistbat1,
                _ => 0
            };

            if (finalSpellId != 0)
                GetCaster().CastSpell(GetTarget(), finalSpellId, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 53099, 57896, 58418, 58420, 59064, 59065, 59439, 60900, 60940
    class spell_quest_portal_with_condition : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 1
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 24194 - Uther's Tribute
    [Script] // 24195 - Grom's Tribute
    class spell_quest_uther_grom_tribute : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GromsTrollTribute, SpellIds.GromsTaurenTribute, SpellIds.GromsUndeadTribute, SpellIds.GromsOrcTribute, SpellIds.GromsBloodelfTribute,
                  SpellIds.UthersHumanTribute, SpellIds.UthersGnomeTribute, SpellIds.UthersDwarfTribute, SpellIds.UthersNightelfTribute, SpellIds.UthersDraeneiTribute);
        }

        void HandleScript(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (!caster)
                return;

            uint spell = caster.GetRace() switch
            {
                Race.Troll => SpellIds.GromsTrollTribute,
                Race.Tauren => SpellIds.GromsTaurenTribute,
                Race.Undead => SpellIds.GromsUndeadTribute,
                Race.Orc => SpellIds.GromsOrcTribute,
                Race.BloodElf => SpellIds.GromsBloodelfTribute,
                Race.Human => SpellIds.UthersHumanTribute,
                Race.Gnome => SpellIds.UthersGnomeTribute,
                Race.Dwarf => SpellIds.UthersDwarfTribute,
                Race.NightElf => SpellIds.UthersNightelfTribute,
                Race.Draenei => SpellIds.UthersDraeneiTribute,
                _ => 0
            };

            if (spell != 0)
                caster.CastSpell(caster, spell);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }
    
    [Script] // 48681 - Summon Silverbrook Worgen
    class spell_q12308_escape_from_silverbrook_summon_worgen : SpellScript
    {
        void ModDest(ref SpellDestination dest)
        {
            float dist = GetEffectInfo(0).CalcRadius(GetCaster());
            float angle = RandomHelper.FRand(0.75f, 1.25f) * MathFunctions.PI;

            Position pos = GetCaster().GetNearPosition(dist, angle);
            dest.Relocate(pos);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(ModDest, 0, Targets.DestCasterSummon));
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
            uint spellId;

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

            GetCaster().CastSpell((Unit)null, spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }
    
    [Script] // 52694 - Recall Eye of Acherus
    class spell_q12641_recall_eye_of_acherus : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Player player = GetCaster().GetCharmerOrOwner().ToPlayer();
            if (player)
            {
                player.StopCastingCharm();
                player.StopCastingBindSight();
                player.RemoveAura(SpellIds.TheEyeOfAcherus);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect));
        }
    }
    
    [Script] // 51769 - Emblazon Runeblade
    class spell_q12619_emblazon_runeblade_AuraScript : AuraScript
    {
        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster)
                caster.CastSpell(caster, aurEff.GetSpellEffectInfo().TriggerSpell, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }
    
    [Script] // 51770 - Emblazon Runeblade
    class spell_q12619_emblazon_runeblade : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), (uint)GetEffectValue(), false);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 55516 - Gymer's Grab
    class spell_q12919_gymers_grab : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.RideGymer);
        }

        void HandleScript(uint effIndex)
        {
            if (!GetHitCreature())
                return;

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, 2);
            GetHitCreature().CastSpell(GetCaster(), SpellIds.RideGymer, args);
            GetHitCreature().CastSpell(GetHitCreature(), SpellIds.Grabbed, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
                if (passenger)
                {
                    passenger.ExitVehicle();
                    caster.CastSpell(passenger, SpellIds.VargulExplosion, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
                if (passenger)
                    passenger.CastSpell(passenger, SpellIds.IllidanKillCredit, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            if (player)
                player.CastSpell(player, SpellIds.TotemOfTheEarthenRing, new CastSpellExtraArgs(TriggerCastFlags.FullMask)); // ignore reagent cost, consumed by quest
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }
   
    [Script] // 39238 - Fumping
    class spell_q10929_fumping : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SummonSandGnome, SpellIds.SummonBoneSlicer);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit caster = GetCaster();
            if (caster)
                caster.CastSpell(caster, RandomHelper.URand(SpellIds.SummonSandGnome, SpellIds.SummonBoneSlicer), true);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
            if (injuredStormwindInfantry)
            {
                injuredStormwindInfantry.SetCreatorGUID(caster.GetGUID());
                injuredStormwindInfantry.CastSpell(injuredStormwindInfantry, SpellIds.RenewedLife, true);
            }
        }

        public override void Register()
        {
            OnCast.Add(new CastHandler(HandleDummyEffect));
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
            OnCast.Add(new CastHandler(HandleDummyEffect));
        }
    }

    [Script] // 49285 - Hand Over Reins
    class spell_q12414_hand_over_reins : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Creature caster = GetCaster().ToCreature();
            GetHitUnit().ExitVehicle();

            if (caster)
                caster.DespawnOrUnsummon();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }
}
