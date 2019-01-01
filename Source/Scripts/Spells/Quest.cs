/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

        //Quest5206
        public const uint CreateResonatingSkull = 17269;
        public const uint CreateBoneDust = 17270;

        //Quest11396-11399
        public const uint ForceShieldArcanePurpleX3 = 43874;
        public const uint ScourgingCrystalController = 43878;

        //Quest11587
        public const uint SummonArcanePrisonerMale = 45446;    // Summon Arcane Prisoner - Male
        public const uint SummonArcanePrisonerFemale = 45448;    // Summon Arcane Prisoner - Female
        public const uint ArcanePrisonerKillCredit = 45456;     // Arcane Prisoner Kill Credit

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

        //Whoarethey
        public const uint MaleDisguise = 38080;
        public const uint FemaleDisguise = 38081;
        public const uint GenericDisguise = 32756;

        //Symboloflife
        public const uint PermanentFeignDeath = 29266;

        //Stoppingthespread
        public const uint Flames = 39199;

        //Chumthewatersummons
        public const uint SummonAngryKvaldir = 66737;
        public const uint SummonNorthSeaMako = 66738;
        public const uint SummonNorthSeaThresher = 66739;
        public const uint SummonNorthSeaBlueShark = 66740;

        //Redsnapperverytasty
        public const uint CastNet = 29866;
        public const uint FishedUpMurloc = 29869;

        //Pounddrumspells
        public const uint SummonDeepJormungar = 66510;
        public const uint StormforgedMoleMachine = 66492;

        //Leavenothingtochance
        public const uint UpperMineShaftCredit = 48744;
        public const uint LowerMineShaftCredit = 48745;

        //Focusonthebeach
        public const uint BunnyCreditBeam = 47390;

        //Acleansingsong
        public const uint SummonSpiritAtah = 52954;
        public const uint SummonSpiritHakhalan = 52958;
        public const uint SummonSpiritKoosu = 52959;

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

        //Burstattheseams
        public const uint BurstAtTheSeams = 52510; // Burst At The Seams
        public const uint BurstAtTheSeamsDmg = 52508; // Damage Spell
        public const uint BurstAtTheSeamsDmg2 = 59580; // Abomination Self Damage Spell
        public const uint BurstAtTheSeamsBone = 52516; // Burst At The Seams:Bone
        public const uint BurstAtTheSeamsMeat = 52520; // Explode Abomination:Meat
        public const uint BurstAtTheSeamsBmeat = 52523; // Explode Abomination:Bloody Meat
        public const uint DrakkariSkullcrusherCredit = 52590; // Credit For Drakkari Skullcrusher
        public const uint SummonDrakkariChieftain = 52616; // Summon Drakkari Chieftain
        public const uint DrakkariChieftainkKillCredit = 52620; // Drakkari Chieftain Kill Credit

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

        //Stoppingthespread
        public const uint VillagerKillCredit = 18240;

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
        public const uint DespawnTime = 30000;

        //HodirsHelm
        public const byte Say1 = 1;
        public const byte Say2 = 2;

        //RedSnapperVeryTasty
        public const uint ItemIdRedSnapper = 23614;

        //Acleansingsong
        public const uint AreaIdBittertidelake = 4385;
        public const uint AreaIdRiversheart = 4290;
        public const uint AreaIdWintergraspriver = 4388;

        //Quest12372
        public const uint WhisperOnHitByForceWhisper = 1;

        //BurstAtTheSeams
        public const uint QuestIdBurstAtTheSeams = 12690;
    }

    // http://www.wowhead.com/quest=55 Morbent Fel
    // 8913 Sacred Cleansing
    // http://www.wowhead.com/quest=10255 Testing the Antidote
    // 34665 Administer Antidote
    // http://www.wowhead.com/quest=11515 Blood for Blood
    // 44936 Quest - Fel Siphon Dummy
    [Script("spell_q55_sacred_cleansing", SpellEffectName.Dummy, 1u, CreatureIds.Morbent, CreatureIds.WeakenedMorbent, true)]
    [Script("spell_q10255_administer_antidote", SpellEffectName.Dummy, 0u, CreatureIds.Helboar, CreatureIds.Dreadtusk, true)]
    [Script("spell_q11515_fel_siphon_dummy", SpellEffectName.Dummy, 0u, CreatureIds.FelbloodInitiate, CreatureIds.EmaciatedFelblood, true)]
    class spell_generic_quest_update_entry : SpellScript
    {
        public spell_generic_quest_update_entry(SpellEffectName spellEffect, uint effIndex, uint originalEntry, uint newEntry, bool shouldAttack) : this(spellEffect, effIndex, originalEntry, newEntry, shouldAttack, 0) { }
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
                    if (_shouldAttack && creatureTarget.IsAIEnabled)
                        creatureTarget.GetAI().AttackStart(GetCaster());

                    if (_despawnTime != 0)
                        creatureTarget.DespawnOrUnsummon(_despawnTime);
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

    // 9712 - Thaumaturgy Channel
    [Script]
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

    // http://www.wowhead.com/quest=5206 Marauders of Darrowshire
    // 17271 Test Fetid Skull
    [Script]
    class spell_q5206_test_fetid_skull : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.CreateResonatingSkull, SpellIds.CreateBoneDust);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            uint spellId = RandomHelper.randChance(50) ? SpellIds.CreateResonatingSkull : SpellIds.CreateBoneDust;
            caster.CastSpell(caster, spellId, true, null);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=6124 Curing the Sick (A)
    // http://www.wowhead.com/quest=6129 Curing the Sick (H)
    // 19512 Apply Salve
    [Script]
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

    // 43874 Scourge Mur'gul Camp: Force Shield Arcane Purple x3
    [Script]
    class spell_q11396_11399_force_shield_arcane_purple_x3 : AuraScript
    {
        void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
            target.AddUnitState(UnitState.Root);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    // 50133 Scourging Crystal Controller
    [Script]
    class spell_q11396_11399_scourging_crystal_controller : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.ForceShieldArcanePurpleX3, SpellIds.ScourgingCrystalController);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetExplTargetUnit();
            if (target)
                if (target.IsTypeId(TypeId.Unit) && target.HasAura(SpellIds.ForceShieldArcanePurpleX3))
                    // Make sure nobody else is channeling the same target
                    if (!target.HasAura(SpellIds.ScourgingCrystalController))
                        GetCaster().CastSpell(target, SpellIds.ScourgingCrystalController, true, GetCastItem());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 43882 Scourging Crystal Controller Dummy
    [Script]
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

    // http://www.wowhead.com/quest=11587 Prison Break
    // 45449 Arcane Prisoner Rescue
    [Script]
    class spell_q11587_arcane_prisoner_rescue : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.SummonArcanePrisonerMale, SpellIds.SummonArcanePrisonerFemale, SpellIds.ArcanePrisonerKillCredit);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();

            Unit unitTarget = GetHitUnit();
            if (unitTarget)
            {
                uint spellId = SpellIds.SummonArcanePrisonerMale;
                if (Convert.ToBoolean(RandomHelper.Rand32() % 2))
                    spellId = SpellIds.SummonArcanePrisonerFemale;
                caster.CastSpell(caster, spellId, true);
                unitTarget.CastSpell(caster, SpellIds.ArcanePrisonerKillCredit, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=11730 Master and Servant
    // 46023 The Ultrasonic Screwdriver
    [Script]
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
                uint spellId = 0;
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
                caster.CastSpell(caster, spellId, true, castItem);
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
    // 49587 Seeds of Nature's Wrath
    [Script]
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
    // 51840 Despawn Fruit Tosser
    [Script]
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
            GetCaster().CastSpell(GetCaster(), spellId, true, null);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=12683 Burning to Help
    // 52308 Take Sputum Sample
    [Script]
    class spell_q12683_take_sputum_sample : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            uint reqAuraId = (uint)GetSpellInfo().GetEffect(1).CalcValue();

            Unit caster = GetCaster();
            if (caster.HasAuraEffect(reqAuraId, 0))
            {
                uint spellId = (uint)GetSpellInfo().GetEffect(0).CalcValue();
                caster.CastSpell(caster, spellId, true, null);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=12851 Going Bearback
    // 54798 FLAMING Arrow Triggered Effect
    [Script]
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

    // http://www.wowhead.com/quest=12937 Relief for the Fallen
    // 55804 Healing Finished
    [Script]
    class spell_q12937_relief_for_the_fallen : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.TriggerAidOfTheEarthen);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();

            Creature target = GetHitCreature();
            if (target)
            {
                caster.CastSpell(caster, SpellIds.TriggerAidOfTheEarthen, true, null);
                caster.KilledMonsterCredit(CreatureIds.FallenEarthenDefender);
                target.DespawnOrUnsummon();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_q10041_q10040_who_are_they : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.MaleDisguise, SpellIds.FemaleDisguise, SpellIds.GenericDisguise);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Player target = GetHitPlayer();
            if (target)
            {
                target.CastSpell(target, target.GetGender() == Gender.Male ? SpellIds.MaleDisguise : SpellIds.FemaleDisguise, true);
                target.CastSpell(target, SpellIds.GenericDisguise, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 8593 Symbol of life dummy
    [Script]
    class spell_symbol_of_life_dummy : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Creature target = GetHitCreature();
            if (target)
            {
                if (target.HasAura(SpellIds.PermanentFeignDeath))
                {
                    target.RemoveAurasDueToSpell(SpellIds.PermanentFeignDeath);
                    target.SetUInt32Value(ObjectFields.DynamicFlags, 0);
                    target.SetUInt32Value(UnitFields.Flags2, 0);
                    target.SetHealth(target.GetMaxHealth() / 2);
                    target.SetPower(PowerType.Mana, (int)(target.GetMaxPower(PowerType.Mana) * 0.75f));
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/quest=12659 Scalps!
    // 52090 Ahunae's Knife
    [Script]
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

    [Script]
    class spell_q9874_liquid_fire : SpellScript
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
                if (target && !target.HasAura(SpellIds.Flames))
                {
                    caster.KilledMonsterCredit(CreatureIds.VillagerKillCredit);
                    target.CastSpell(target, SpellIds.Flames, true);
                    target.DespawnOrUnsummon(60000);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
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
                target.DespawnOrUnsummon(2000);
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
 59643 Plant Horde Battle Standard
 4338 Plant Alliance Battle Standard
 */
    [Script]
    class spell_q13280_13283_plant_battle_standard : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.IsVehicle())
            {
                Unit player = caster.GetVehicleKit().GetPassenger(0);
                if (player)
                    player.ToPlayer().KilledMonsterCredit(CreatureIds.KingOfTheMountaintKc);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_q14112_14145_chum_the_water : SpellScript
    {
        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(SpellIds.SummonAngryKvaldir, SpellIds.SummonNorthSeaMako, SpellIds.SummonNorthSeaThresher, SpellIds.SummonNorthSeaBlueShark);
        }

        void HandleScriptEffect(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, RandomHelper.RAND(SpellIds.SummonAngryKvaldir, SpellIds.SummonNorthSeaMako, SpellIds.SummonNorthSeaThresher, SpellIds.SummonNorthSeaBlueShark));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // http://old01.wowhead.com/quest=9452 - Red Snapper - Very Tasty!
    [Script]
    class spell_q9452_cast_net : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (RandomHelper.randChance(66))
                caster.AddItem(Misc.ItemIdRedSnapper, 1);
            else
                caster.CastSpell(caster, SpellIds.FishedUpMurloc, true);
        }

        void HandleActiveObject(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitGObj().SetRespawnTime(RandomHelper.randChance(50) ? 2 * Time.Minute : 3 * Time.Minute);
            GetHitGObj().Use(GetCaster());
            GetHitGObj().SetLootState(LootState.JustDeactivated);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
            OnEffectHitTarget.Add(new EffectHandler(HandleActiveObject, 1, SpellEffectName.ActivateObject));
        }
    }

    [Script]
    class spell_q14076_14092_pound_drum : SpellScript
    {
        void HandleSummon()
        {
            Unit caster = GetCaster();

            if (RandomHelper.randChance(80))
                caster.CastSpell(caster, SpellIds.SummonDeepJormungar, true);
            else
                caster.CastSpell(caster, SpellIds.StormforgedMoleMachine, true);
        }

        void HandleActiveObject(uint effIndex)
        {
            GetHitGObj().SetLootState(LootState.JustDeactivated);
        }

        public override void Register()
        {
            OnCast.Add(new CastHandler(HandleSummon));
            OnEffectHitTarget.Add(new EffectHandler(HandleActiveObject, 0, SpellEffectName.ActivateObject));
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

    [Script]
    class spell_q12987_read_pronouncement : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // player must cast kill credit and do emote text, according to sniff
            Player target = GetTarget().ToPlayer();
            if (target)
            {
                Creature trigger = target.FindNearestCreature(CreatureIds.IceSpikeBunny, 25.0f);
                if (trigger)
                {
                    Global.CreatureTextMgr.SendChat(trigger, Misc.Say1, target, ChatMsg.Addon, Language.Addon, Game.CreatureTextRange.Normal, 0, Team.Other, false, target);
                    target.KilledMonsterCredit(CreatureIds.Killcredit);
                    Global.CreatureTextMgr.SendChat(trigger, Misc.Say2, target, ChatMsg.Addon, Language.Addon, Game.CreatureTextRange.Normal, 0, Team.Other, false, target);
                }
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.None, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_q12277_wintergarde_mine_explosion : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Creature unitTarget = GetHitCreature();
            if (unitTarget)
            {
                Unit caster = GetCaster();
                if (caster)
                {
                    if (caster.IsTypeId(TypeId.Unit))
                    {
                        Unit owner = caster.GetOwner();
                        if (owner)
                        {
                            switch (unitTarget.GetEntry())
                            {
                                case CreatureIds.UpperMineShaft:
                                    caster.CastSpell(owner, SpellIds.UpperMineShaftCredit, true);
                                    break;
                                case CreatureIds.LowerMineShaft:
                                    caster.CastSpell(owner, SpellIds.LowerMineShaftCredit, true);
                                    break;
                                default:
                                    break;
                            }
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

    [Script]
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

    [Script]
    class spell_q12735_song_of_cleansing : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            switch (caster.GetAreaId())
            {
                case Misc.AreaIdBittertidelake:
                    caster.CastSpell(caster, SpellIds.SummonSpiritAtah);
                    break;
                case Misc.AreaIdRiversheart:
                    caster.CastSpell(caster, SpellIds.SummonSpiritHakhalan);
                    break;
                case Misc.AreaIdWintergraspriver:
                    caster.CastSpell(caster, SpellIds.SummonSpiritKoosu);
                    break;
                default:
                    break;
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
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
    // 49370 - Wyrmrest Defender: Destabilize Azure Dragonshrine Effect
    [Script]
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

    // ID - 50287 Azure Dragon: On Death Force Cast Wyrmrest Defender to Whisper to Controller - Random (cast from Azure Dragons and Azure Drakes on death)
    [Script]
    class spell_q12372_azure_on_death_force_whisper : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Creature defender = GetHitCreature();
            if (defender)
                defender.GetAI().Talk(Misc.WhisperOnHitByForceWhisper, defender.GetCharmerOrOwner());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 40113 Knockdown Fel Cannon: The Aggro Check Aura
    [Script]
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

    // 40112 Knockdown Fel Cannon: The Aggro Check
    [Script]
    class spell_q11010_q11102_q11023_aggro_check : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Player playerTarget = GetHitPlayer();
            if (playerTarget)
                // Check if found player target is on fly mount or using flying form
                if (playerTarget.HasAuraType(AuraType.Fly) || playerTarget.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                    playerTarget.CastSpell(playerTarget, SpellIds.FlakCannonTrigger, TriggerCastFlags.IgnoreCasterMountedOrOnVehicle);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 40119 Knockdown Fel Cannon: The Aggro Burst
    [Script]
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

    // 40056 Knockdown Fel Cannon: Choose Loc
    [Script]
    class spell_q11010_q11102_q11023_choose_loc : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            // Check for player that is in 65 y range
            List<Player> playerList = new List<Player>();
            AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(caster, 65.0f);
            PlayerListSearcher searcher = new PlayerListSearcher(caster, playerList, checker);
            Cell.VisitWorldObjects(caster, searcher, 65.0f);
            foreach (var player in playerList)
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
    // 40160 - Throw Bomb
    [Script]
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

    [Script]
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

    // 55368 - Summon Stefan
    [Script]
    class spell_q12661_q12669_q12676_q12677_q12713_summon_stefan : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            // Adjust effect summon position
            Position offset = new Position(0.0f, 0.0f, 20.0f, 0.0f);
            dest.RelocateOffset(offset);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCasterBack));
        }
    }

    [Script]
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

    // 13291 - Borrowed Technology/13292 - The Solution Solution /Daily//13239 - Volatility/13261 - Volatiliy /Daily//
    [Script]
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

    // 59303 - Summon Frost Wyrm
    [Script]
    class spell_q13291_q13292_q13239_q13261_armored_decoy_summon_skytalon : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            // Adjust effect summon position
            Position offset = new Position(0.0f, 0.0f, 20.0f, 0.0f);
            dest.RelocateOffset(offset);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCasterBack));
        }
    }

    // 12601 - Second Chances: Summon Landgren's Soul Moveto Target Bunny
    [Script]
    class spell_q12847_summon_soul_moveto_bunny : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            // Adjust effect summon position
            Position offset = new Position(0.0f, 0.0f, 2.5f, 0.0f);
            dest.RelocateOffset(offset);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
        }
    }

    [Script]
    class spell_q13011_bear_flank_master : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BearFlankMaster, SpellIds.CreateBearFlank);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Unit);
        }

        void HandleScript(uint effIndex)
        {
            Player player = GetHitPlayer();
            if (player)
            {
                if (RandomHelper.randChance(50))
                {
                    Creature creature = GetCaster().ToCreature();
                    player.CastSpell(creature, SpellIds.BearFlankFail);
                    creature.GetAI().Talk(0, player);
                }
                else
                    player.CastSpell(player, SpellIds.CreateBearFlank);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_q13086_cannons_target : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void HandleEffectDummy(uint effIndex)
        {
            WorldLocation pos = GetExplTargetDest();
            if (pos != null)
                GetCaster().CastSpell(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), (uint)GetEffectValue(), true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_q12690_burst_at_the_seams : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BurstAtTheSeams, SpellIds.BurstAtTheSeamsDmg, SpellIds.BurstAtTheSeamsDmg2,
                SpellIds.BurstAtTheSeamsBone, SpellIds.BurstAtTheSeamsMeat, SpellIds.BurstAtTheSeamsBmeat);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Unit);
        }

        void HandleKnockBack(uint effIndex)
        {
            Unit creature = GetHitCreature();
            if (creature)
            {
                Unit charmer = GetCaster().GetCharmerOrOwner();
                if (charmer)
                {
                    Player player = charmer.ToPlayer();
                    if (player)
                    {
                        if (player.GetQuestStatus(Misc.QuestIdBurstAtTheSeams) == QuestStatus.Incomplete)
                        {
                            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsBone, true);
                            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsMeat, true);
                            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsBmeat, true);
                            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsDmg, true);
                            creature.CastSpell(creature, SpellIds.BurstAtTheSeamsDmg2, true);

                            player.CastSpell(player, SpellIds.DrakkariSkullcrusherCredit, true);
                            ushort count = player.GetReqKillOrCastCurrentCount(Misc.QuestIdBurstAtTheSeams, (int)CreatureIds.DrakkariChieftaink);
                            if ((count % 20) == 0)
                                player.CastSpell(player, SpellIds.SummonDrakkariChieftain, true);
                        }
                    }
                }
            }
        }

        void HandleScript(uint effIndex)
        {
            GetCaster().ToCreature().DespawnOrUnsummon(2 * Time.InMilliseconds);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleKnockBack, 1, SpellEffectName.KnockBack));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 48682 - Escape from Silverbrook - Periodic Dummy
    [Script]
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

    // 48681 - Summon Silverbrook Worgen
    [Script]
    class spell_q12308_escape_from_silverbrook_summon_worgen : SpellScript
    {
        void ModDest(ref SpellDestination dest)
        {
            float dist = GetSpellInfo().GetEffect(0).CalcRadius(GetCaster());
            float angle = RandomHelper.FRand(0.75f, 1.25f) * MathFunctions.PI;

            Position pos = GetCaster().GetNearPosition(dist, angle);
            dest.Relocate(pos);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(ModDest, 0, Targets.DestCasterSummon));
        }
    }

    // 51858 - Siphon of Acherus
    [Script]
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

            GetCaster().CastSpell((Unit)null, spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 52694 - Recall Eye of Acherus
    [Script]
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

    // 51769 - Emblazon Runeblade
    [Script]
    class spell_q12619_emblazon_runeblade : AuraScript
    {
        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster)
                caster.CastSpell(caster, GetSpellInfo().GetEffect(aurEff.GetEffIndex()).TriggerSpell, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    // 51770 - Emblazon Runeblade
    [Script]
    class spell_q12619_emblazon_runeblade_effect : SpellScript
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

    [Script]
    class spell_q12919_gymers_grab : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.RideGymer);
        }

        void HandleScript(uint effIndex)
        {
            sbyte seatId = 2;
            if (!GetHitCreature())
                return;
            GetHitCreature().CastCustomSpell(SpellIds.RideGymer, SpellValueMod.BasePoint0, seatId, GetCaster(), true);
            GetHitCreature().CastSpell(GetHitCreature(), SpellIds.Grabbed, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
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

    [Script]
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

    // 66744 - Make Player Destroy Totems
    [Script]
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
                player.CastSpell(player, SpellIds.TotemOfTheEarthenRing, TriggerCastFlags.FullMask); // ignore reagent cost, consumed by quest
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 39238 - Fumping
    [Script]
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

    // 93072 - Get Our Boys Back Dummy
    [Script]
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

    [Script]
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

    [Script]
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
