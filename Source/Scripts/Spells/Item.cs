/*
 * This file is part of the TrinityCore Project. See Authors file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the Gnu General Public License as published by the
 * Free Software Foundation; either version 2 of the License, or (at your
 * option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but Without
 * Any Warranty; without even the implied warranty of Merchantability or
 * Fitness For A Particular Purpose. See the Gnu General Public License for
 * more details.
 *
 * You should have received a copy of the Gnu General Public License along
 * with this program. If not, see <http://www.gnu.org/licenses/>.
 */

/*
 * Scripts for spells with SpellfamilyGeneric spells used by items.
 * Ordered alphabetically using scriptname.
 * Scriptnames of files in this file should be prefixed with "spell_item_".
 */


using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Miscellaneous;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Items;

struct SpellIds
{
    // GenericData
    public const uint ArcaniteDragonling = 19804;
    public const uint BattleChicken = 13166;
    public const uint MechanicalDragonling = 4073;
    public const uint MithrilMechanicalDragonling = 12749;

    // AegisOfPreservation
    public const uint AegisHeal = 23781;

    // ZezzaksShard
    public const uint EyeOfGrillok = 38495;

    // LowerCityPrayerbook
    public const uint BlessingOfLowerCityDruid = 37878;
    public const uint BlessingOfLowerCityPaladin = 37879;
    public const uint BlessingOfLowerCityPriest = 37880;
    public const uint BlessingOfLowerCityShaman = 37881;

    // AlchemistStone
    public const uint AlchemistStoneExtraHeal = 21399;
    public const uint AlchemistStoneExtraMana = 21400;

    // AngerCapacitor
    public const uint MoteOfAnger = 71432;
    public const uint ManifestAngerMainHand = 71433;
    public const uint ManifestAngerOffHand = 71434;

    // AuraOfMadness
    public const uint Sociopath = 39511; // Sociopath: +35 strength(Paladin; Rogue; Druid; Warrior)
    public const uint Delusional = 40997; // Delusional: +70 attack power(Rogue; Hunter; Paladin; Warrior; Druid)
    public const uint Kleptomania = 40998; // Kleptomania: +35 agility(Warrior; Rogue; Paladin; Hunter; Druid)
    public const uint Megalomania = 40999; // Megalomania: +41 damage / healing(Druid; Shaman; Priest; Warlock; Mage; Paladin)
    public const uint Paranoia = 41002; // Paranoia: +35 spell / melee / ranged crit strike rating(All classes)
    public const uint Manic = 41005; // Manic: +35 haste(spell; melee and ranged) (All classes)
    public const uint Narcissism = 41009; // Narcissism: +35 intellect(Druid; Shaman; Priest; Warlock; Mage; Paladin; Hunter)
    public const uint MartyrComplex = 41011; // Martyr Complex: +35 stamina(All classes)
    public const uint Dementia = 41404; // Dementia: Every 5 seconds either gives you +5/-5%  damage/healing. (Druid; Shaman; Priest; Warlock; Mage; Paladin)
    public const uint DementiaPos = 41406;
    public const uint DementiaNeg = 41409;

    // BrittleArmor
    public const uint BrittleArmor = 24575;

    // BlessingOfAncientKings
    public const uint ProtectionOfAncientKings = 64413;

    // DeadlyPrecision
    public const uint DeadlyPrecision = 71564;

    // DeathbringersWill
    public const uint StrengthOfTheTaunka = 71484; // +600 Strength
    public const uint AgilityOfTheVrykul = 71485; // +600 Agility
    public const uint PowerOfTheTaunka = 71486; // +1200 Attack Power
    public const uint AimOfTheIronDwarves = 71491; // +600 Critical
    public const uint SpeedOfTheVrykul = 71492; // +600 Haste

    public const uint AgilityOfTheVrykulHero = 71556; // +700 Agility
    public const uint PowerOfTheTaunkaHero = 71558; // +1400 Attack Power
    public const uint AimOfTheIronDwarvesHero = 71559; // +700 Critical
    public const uint SpeedOfTheVrykulHero = 71560; // +700 Haste
    public const uint StrengthOfTheTaunkaHero = 71561;  // +700 Strength

    // GoblinBombDispenser
    public const uint SummonGoblinBomb = 13258;
    public const uint MalfunctionExplosion = 13261;

    // GoblinWeatherMachine
    public const uint PersonalizedWeather1 = 46740;
    public const uint PersonalizedWeather2 = 46739;
    public const uint PersonalizedWeather3 = 46738;
    public const uint PersonalizedWeather4 = 46736;

    // Defibrillate
    public const uint GoblinJumperCablesFail = 8338;
    public const uint GoblinJumperCablesXlFail = 23055;

    // DesperateDefense
    public const uint DesperateRage = 33898;

    // DeviateFishSpells
    public const uint Sleepy = 8064;
    public const uint Invigorate = 8065;
    public const uint Shrink = 8066;
    public const uint PartyTime = 8067;
    public const uint HealthySpirit = 8068;
    public const uint Rejuvenation = 8070;

    // DiscerningEyeBeastMisc
    public const uint DiscerningEyeBeast = 59914;

    // FateRuneOfUnsurpassedVigor
    public const uint UnsurpassedVigor = 25733;

    // FlaskOfTheNorthSpells
    public const uint FlaskOfTheNorthSp = 67016;
    public const uint FlaskOfTheNorthAp = 67017;
    public const uint FlaskOfTheNorthStr = 67018;

    // FrozenShadoweave
    public const uint Shadowmend = 39373;

    // GnomishDeathRay
    public const uint GnomishDeathRaySelf = 13493;
    public const uint GnomishDeathRayTarget = 13279;

    // HarmPreventionBelt
    public const uint ForcefieldCollapse = 13235;

    // Heartpierce
    public const uint InvigorationMana = 71881;
    public const uint InvigorationEnergy = 71882;
    public const uint InvigorationRage = 71883;
    public const uint InvigorationRp = 71884;

    public const uint InvigorationRpHero = 71885;
    public const uint InvigorationRageHero = 71886;
    public const uint InvigorationEnergyHero = 71887;
    public const uint InvigorationManaHero = 71888;

    // HourglassSand
    public const uint BroodAfflictionBronze = 23170;

    // MakeAWish
    public const uint MrPinchysBlessing = 33053;
    public const uint SummonMightyMrPinchy = 33057;
    public const uint SummonFuriousMrPinchy = 33059;
    public const uint TinyMagicalCrawdad = 33062;
    public const uint MrPinchysGift = 33064;

    // MarkOfConquest
    public const uint MarkOfConquestEnergize = 39599;

    // MercurialShield
    public const uint MercurialShield = 26464;

    // MingoFortune
    public const uint CreateFortune1 = 40804;
    public const uint CreateFortune2 = 40805;
    public const uint CreateFortune3 = 40806;
    public const uint CreateFortune4 = 40807;
    public const uint CreateFortune5 = 40808;
    public const uint CreateFortune6 = 40809;
    public const uint CreateFortune7 = 40908;
    public const uint CreateFortune8 = 40910;
    public const uint CreateFortune9 = 40911;
    public const uint CreateFortune10 = 40912;
    public const uint CreateFortune11 = 40913;
    public const uint CreateFortune12 = 40914;
    public const uint CreateFortune13 = 40915;
    public const uint CreateFortune14 = 40916;
    public const uint CreateFortune15 = 40918;
    public const uint CreateFortune16 = 40919;
    public const uint CreateFortune17 = 40920;
    public const uint CreateFortune18 = 40921;
    public const uint CreateFortune19 = 40922;
    public const uint CreateFortune20 = 40923;

    // NecroticTouch
    public const uint ItemNecroticTouchProc = 71879;

    // NetOMaticSpells
    public const uint NetOMaticTRIGGERED1 = 16566;
    public const uint NetOMaticTRIGGERED2 = 13119;
    public const uint NetOMaticTRIGGERED3 = 13099;

    // NoggenfoggerElixirSpells
    public const uint NoggenfoggerElixirTRIGGERED1 = 16595;
    public const uint NoggenfoggerElixirTRIGGERED2 = 16593;
    public const uint NoggenfoggerElixirTRIGGERED3 = 16591;

    // PersistentShieldMisc
    public const uint PersistentShieldTriggered = 26470;

    // PetHealing
    public const uint HealthLink = 37382;

    // PowerCircle
    public const uint LimitlessPower = 45044;

    // SavoryDeviateDelight
    public const uint FlipOutMale = 8219;
    public const uint FlipOutFemale = 8220;
    public const uint YaaarrrrMale = 8221;
    public const uint YaaarrrrFemale = 8222;

    // ScrollOfRecall
    public const uint ScrollOfRecallI = 48129;
    public const uint ScrollOfRecallIi = 60320;
    public const uint ScrollOfRecallIii = 60321;
    public const uint Lost = 60444;
    public const uint ScrollOfRecallFailAlliance1 = 60323;
    public const uint ScrollOfRecallFailHorde1 = 60328;

    // TransporterSpells
    public const uint EvilTwin = 23445;
    public const uint TransporterMalfunctionFire = 23449;
    public const uint TransporterMalfunctionSmaller = 36893;
    public const uint TransporterMalfunctionBigger = 36895;
    public const uint TransporterMalfunctionChicken = 36940;
    public const uint TransformHorde = 36897;
    public const uint TransformAlliance = 36899;
    public const uint SoulSplitEvil = 36900;
    public const uint SoulSplitGood = 36901;

    // ShadowsFate
    public const uint SoulFeast = 71203;

    // Shadowmourne
    public const uint ShadowmourneChaosBaneDamage = 71904;
    public const uint ShadowmourneSoulFragment = 71905;
    public const uint ShadowmourneVisualLow = 72521;
    public const uint ShadowmourneVisualHigh = 72523;
    public const uint ShadowmourneChaosBaneBuff = 73422;

    // SixDemonBagSpells
    public const uint Frostbolt = 11538;
    public const uint Polymorph = 14621;
    public const uint SummonFelhoundMinion = 14642;
    public const uint Fireball = 15662;
    public const uint ChainLightning = 21179;
    public const uint EnvelopingWinds = 25189;

    // SwiftHandJusticeMisc
    public const uint SwiftHandOfJusticeHeal = 59913;

    // UnderbellyElixirSpells
    public const uint UnderbellyElixirTriggered1 = 59645;
    public const uint UnderbellyElixirTriggered2 = 59831;
    public const uint UnderbellyElixirTriggered3 = 59843;

    // WormholeGeneratorPandariaSpell
    public const uint WormholePandariaIsleOfReckoning = 126756;
    public const uint WormholePandariaKunlaiUnderwater = 126757;
    public const uint WormholePandariaSraVess = 126758;
    public const uint WormholePandariaRikkitunVillage = 126759;
    public const uint WormholePandariaZanvessTree = 126760;
    public const uint WormholePandariaAnglersWharf = 126761;
    public const uint WormholePandariaCraneStatue = 126762;
    public const uint WormholePandariaEmperorsOmen = 126763;
    public const uint WormholePandariaWhitepetalLake = 126764;

    // AirRifleSpells
    public const uint AirRifleHoldVisual = 65582;
    public const uint AirRifleShoot = 67532;
    public const uint AirRifleShootSelf = 65577;

    // VanquishedClutchesSpells
    public const uint Crusher = 64982;
    public const uint Constrictor = 64983;
    public const uint Corruptor = 64984;

    // MagicEater
    public const uint WildMagic = 58891;
    public const uint WellFed1 = 57288;
    public const uint WellFed2 = 57139;
    public const uint WellFed3 = 57111;
    public const uint WellFed4 = 57286;
    public const uint WellFed5 = 57291;

    // PurifyHelboarMeat
    public const uint SummonPurifiedHelboarMeat = 29277;
    public const uint SummonToxicHelboarMeat = 29278;

    // NighInvulnerability
    public const uint NighInvulnerability = 30456;
    public const uint CompleteVulnerability = 30457;

    // Poultryzer
    public const uint PoultryizerSuccess = 30501;
    public const uint PoultryizerBackfire = 30504;

    // SocretharsStone
    public const uint SocretharToSeat = 35743;
    public const uint SocretharFromSeat = 35744;

    // DemonBroiledSurprise
    public const uint CreateDemonBroiledSurprise = 43753;

    // CompleteRaptorCapture
    public const uint RaptorCaptureCredit = 42337;

    // ImpaleLeviroth
    public const uint LevirothSelfImpale = 49882;

    // LifegivingGem
    public const uint GiftOfLife1 = 23782;
    public const uint GiftOfLife2 = 23783;

    // NitroBoosts
    public const uint NitroBoostsSuccess = 54861;
    public const uint NitroBoostsBackfire = 54621;
    public const uint NitroBoostsParachute = 54649;

    // RocketBoots
    public const uint RocketBootsProc = 30452;

    // PygmyOil
    public const uint PygmyOilPygmyAura = 53806;
    public const uint PygmyOilSmallerAura = 53805;

    // ChickenCover
    public const uint ChickenNet = 51959;
    public const uint CaptureChickenEscape = 51037;

    // GreatmothersSoulcather
    public const uint ForceCastSummonGnomeSoul = 46486;

    // ShardOfTheScale
    public const uint PurifiedCauterizingHeal = 69733;
    public const uint PurifiedSearingFlames = 69729;

    public const uint ShinyCauterizingHeal = 69734;
    public const uint ShinySearingFlames = 69730;

    // SoulPreserver
    public const uint SoulPreserverDruid = 60512;
    public const uint SoulPreserverPaladin = 60513;
    public const uint SoulPreserverPriest = 60514;
    public const uint SoulPreserverShaman = 60515;

    // ExaltedSunwellNeck
    public const uint LightsWrath = 45479; // Light's Wrath if Exalted by Aldor
    public const uint ArcaneBolt = 45429; // Arcane Bolt if Exalted by Scryers

    public const uint LightsStrength = 45480; // Light's Strength if Exalted by Aldor
    public const uint ArcaneStrike = 45428; // Arcane Strike if Exalted by Scryers

    public const uint LightsWard = 45432; // Light's Ward if Exalted by Aldor
    public const uint ArcaneInsight = 45431; // Arcane Insight if Exalted by Scryers

    public const uint LightsSalvation = 45478; // Light's Salvation if Exalted by Aldor
    public const uint ArcaneSurge = 45430; // Arcane Surge if Exalted by Scryers

    // DeathChoiceSpells
    public const uint DeathChoiceNormalAura = 67702;
    public const uint DeathChoiceNormalAgility = 67703;
    public const uint DeathChoiceNormalStrength = 67708;
    public const uint DeathChoiceHeroicAura = 67771;
    public const uint DeathChoiceHeroicAgility = 67772;
    public const uint DeathChoiceHeroicStrength = 67773;

    // TrinketStackSpells
    public const uint LightningCapacitorAura = 37657;  // Lightning Capacitor
    public const uint LightningCapacitorStack = 37658;
    public const uint LightningCapacitorTrigger = 37661;
    public const uint ThunderCapacitorAura = 54841;  // Thunder Capacitor
    public const uint ThunderCapacitorStack = 54842;
    public const uint ThunderCapacitorTrigger = 54843;
    public const uint TOC25_CasterTrinketNormalAura = 67712;  // Item - Coliseum 25 Normal Caster Trinket
    public const uint TOC25CasterTrinketNormalStack = 67713;
    public const uint TOC25CasterTrinketNormalTrigger = 67714;
    public const uint TOC25_CasterTrinketHeroicAura = 67758;  // Item - Coliseum 25 Heroic Caster Trinket
    public const uint TOC25CasterTrinketHeroicStack = 67759;
    public const uint TOC25CasterTrinketHeroicTrigger = 67760;

    // DarkmoonCardSpells
    public const uint DarkmoonCardStrength = 60229;
    public const uint DarkmoonCardAgility = 60233;
    public const uint DarkmoonCardIntellect = 60234;
    public const uint DarkmoonCardVersatility = 60235;

    // ManaDrainSpells
    public const uint ManaDrainEnergize = 29471;
    public const uint ManaDrainLeech = 27526;

    // TauntFlag
    public const uint TauntFlag = 51657;

    // MirrensDrinkingHat
    public const uint LochModanLager = 29827;
    public const uint StouthammerLite = 29828;
    public const uint AeriePeakPaleAle = 29829;

    // MindControlCap
    public const uint GnomishMindControlCap = 13181;
    public const uint Dullard = 67809;

    // UniversalRemote
    public const uint ControlMachine = 8345;
    public const uint MobilityMalfunction = 8346;
    public const uint TargetLock = 8347;

    // ZandalarianCharms
    public const uint UnstablePowerAuraStack = 24659;
    public const uint RestlessStrengthAuraStack = 24662;

    // AuraProcRemoveSpells
    public const uint TalismanOfAscendance = 28200;
    public const uint JomGabbar = 29602;
    public const uint BattleTrance = 45040;
    public const uint WorldQuellerFocus = 90900;
    public const uint BrutalKinship1 = 144671;
    public const uint BrutalKinship2 = 145738;

    // Eggnog
    public const uint EggNogReindeer = 21936;
    public const uint EggNogSnowman = 21980;

    // SephuzsSecret
    public const uint SephuzsSecretCooldown = 226262;

    // AmalgamsSeventhSpine
    public const uint FragileEchoesMonk = 225281;
    public const uint FragileEchoesShaman = 225292;
    public const uint FragileEchoesPriestDiscipline = 225294;
    public const uint FragileEchoesPaladin = 225297;
    public const uint FragileEchoesDruid = 225298;
    public const uint FragileEchoesPriestHoly = 225366;
    public const uint FragileEchoesEvoker = 429020;
    public const uint FragileEchoEnergize = 215270;

    // HighfathersMachination
    public const uint HighfathersTimekeepingHeal = 253288;

    // SeepingScourgewing
    public const uint ShadowStrikeAoeCheck = 255861;
    public const uint IsolatedStrike = 255609;

    // ShiverVenomSpell
    public const uint ShiverVenom = 301624;
    public const uint ShiveringBolt = 303559;
    public const uint VenomousLance = 303562;

    // MettleSpell
    public const uint MettleCooldown = 410532;
}

struct MiscConst
{
    // AuraOfMadness
    public const uint SayMadness = 21954;

    //Roll Dice
    public const uint TextDecahedralDwarvenDice = 26147;

    // DireBrew
    public const uint ModelClassClothMale = 25229;
    public const uint ModelClassClothFemale = 25233;
    public const uint ModelClassLeatherMale = 25230;
    public const uint ModelClassLeatherFemale = 25234;
    public const uint ModelClassMailMale = 25231;
    public const uint ModelClassMailFemale = 25235;
    public const uint ModelClassPlateMale = 25232;
    public const uint ModelClassPlateFemale = 25236;

    // Feast
    public const uint TextGreatFeast = 31843;
    public const uint TextFishFeast = 31844;
    public const uint TextGiganticFeast = 31846;
    public const uint TextSmallFeast = 31845;
    public const uint TextBountifulFeast = 35153;

    // ShadowsFate
    public const uint NpcSindragosa = 36853;

    // Roll 'dem Bones
    public const uint TextWornTrollDice = 26152;

    // GiftOfTheHarvester
    public const uint NpcGhoul = 28845;
    public const uint MaxGhouls = 5;

    // Sinkholes
    public const uint NpcSouthSinkhole = 25664;
    public const uint NpcNortheastSinkhole = 25665;
    public const uint NpcNorthwestSinkhole = 25666;

    // AshbringerSounds
    public const uint SoundAshbringer1 = 8906;                             // "I was pure once"
    public const uint SoundAshbringer2 = 8907;                             // "Fought for righteousness"
    public const uint SoundAshbringer3 = 8908;                             // "I was once called Ashbringer"
    public const uint SoundAshbringer4 = 8920;                             // "Betrayed by my order"
    public const uint SoundAshbringer5 = 8921;                             // "Destroyed by Kel'Thuzad"
    public const uint SoundAshbringer6 = 8922;                             // "Made to serve"
    public const uint SoundAshbringer7 = 8923;                             // "My son watched me die"
    public const uint SoundAshbringer8 = 8924;                             // "Crusades fed his rage"
    public const uint SoundAshbringer9 = 8925;                             // "Truth is unknown to him"
    public const uint SoundAshbringer10 = 8926;                             // "Scarlet Crusade  is pure no longer"
    public const uint SoundAshbringer11 = 8927;                             // "Balnazzar's crusade corrupted my son"
    public const uint SoundAshbringer12 = 8928;                             // "Kill them all!"

    // DemonBroiledSurprise
    public const uint QuestSuperHotStew = 11379;
    public const uint NpcAbyssalFlamebringer = 19973;


    // ImpaleLeviroth
    public const uint NpcLeviroth = 26452;

    // ChickenCover
    public const uint QuestChickenParty = 12702;
    public const uint QuestFlownTheCoop = 12532;

    // ExaltedSunwellNeck
    public const uint FactionAldor = 932;
    public const uint FactionScryers = 934;

    // TauntFlag
    public const uint EmotePlantsFlag = 28008;


    // MindControlCap
    public const uint RollChanceDullard = 32;
    public const uint RollChanceNoBackfire = 95;
}

[Script("spell_item_arcanite_dragonling", SpellIds.ArcaniteDragonling)]        // 23074 Arcanite Dragonling
[Script("spell_item_gnomish_battle_chicken", SpellIds.BattleChicken)]    // 23133 Gnomish Battle Chicken
[Script("spell_item_mechanical_dragonling", SpellIds.MechanicalDragonling)]    // 23076 Mechanical Dragonling
[Script("spell_item_mithril_mechanical_dragonling", SpellIds.MithrilMechanicalDragonling)]    // 23075 Mithril Mechanical Dragonling
class spell_item_trigger_spell(uint triggeredSpellId) : SpellScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(triggeredSpellId);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster(); Item item = GetCastItem();
        if (item != null)
            caster.CastSpell(caster, triggeredSpellId, item);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 23780 - Aegis of Preservation
class spell_item_aegis_of_preservation : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AegisHeal);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), SpellIds.AegisHeal, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 1, AuraType.ProcTriggerSpell));
    }
}


[Script] // 38554 - Absorb Eye of Grillok (31463: Zezzak's Shard)
class spell_item_absorb_eye_of_grillok : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EyeOfGrillok);
    }

    void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();

        if (GetCaster() == null || GetTarget().GetTypeId() != TypeId.Unit)
            return;

        GetCaster().CastSpell(GetCaster(), SpellIds.EyeOfGrillok, aurEff);
        GetTarget().ToCreature().DespawnOrUnsummon();
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }
}

// 37877 - Blessing of Faith
class spell_item_blessing_of_faith : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BlessingOfLowerCityDruid, SpellIds.BlessingOfLowerCityPaladin, SpellIds.BlessingOfLowerCityPriest, SpellIds.BlessingOfLowerCityShaman);
    }

    void HandleDummy(uint effIndex)
    {
        Unit unitTarget = GetHitUnit();
        if (unitTarget != null)
        {
            uint spellId = 0;
            switch (unitTarget.GetClass())
            {
                case Class.Druid:
                    spellId = SpellIds.BlessingOfLowerCityDruid;
                    break;
                case Class.Paladin:
                    spellId = SpellIds.BlessingOfLowerCityPaladin;
                    break;
                case Class.Priest:
                    spellId = SpellIds.BlessingOfLowerCityPriest;
                    break;
                case Class.Shaman:
                    spellId = SpellIds.BlessingOfLowerCityShaman;
                    break;
                default:
                    return; // ignore for non-healing classes
            }

            Unit caster = GetCaster();
            caster.CastSpell(caster, spellId, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// Item - 13503: Alchemist's Stone
// Item - 35748: Guardian's Alchemist Stone
// Item - 35749: Sorcerer's Alchemist Stone
// Item - 35750: Redeemer's Alchemist Stone
// Item - 35751: Assassin's Alchemist Stone
// Item - 44322: Mercurial Alchemist Stone
// Item - 44323: Indestructible Alchemist's Stone
// Item - 44324: Mighty Alchemist's Stone

[Script] // 17619 - Alchemist Stone
class spell_item_alchemist_stone : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AlchemistStoneExtraHeal, SpellIds.AlchemistStoneExtraMana);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetDamageInfo().GetSpellInfo().SpellFamilyName == SpellFamilyNames.Potion;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId = 0;
        int amount = (int)(eventInfo.GetDamageInfo().GetDamage() * 0.4f);

        if (eventInfo.GetDamageInfo().GetSpellInfo().HasEffect(SpellEffectName.Heal))
            spellId = SpellIds.AlchemistStoneExtraHeal;
        else if (eventInfo.GetDamageInfo().GetSpellInfo().HasEffect(SpellEffectName.Energize))
            spellId = SpellIds.AlchemistStoneExtraMana;

        if (spellId == 0)
            return;

        Unit caster = eventInfo.GetActionTarget();
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.CastSpell(null, spellId, args);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

// Item - 50351: Tiny Abomination in a Jar
// 71406 - Anger Capacitor

// Item - 50706: Tiny Abomination in a Jar (Heroic)
// 71545 - Anger Capacitor
[Script("spell_item_tiny_abomination_in_a_jar", 8)]
[Script("spell_item_tiny_abomination_in_a_jar_hero", 7)]
class spell_item_anger_capacitor(uint stacks) : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MoteOfAnger, SpellIds.ManifestAngerMainHand, SpellIds.ManifestAngerOffHand);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        caster.CastSpell(null, SpellIds.MoteOfAnger, true);
        Aura motes = caster.GetAura(SpellIds.MoteOfAnger);
        if (motes == null || motes.GetStackAmount() < stacks)
            return;

        caster.RemoveAurasDueToSpell(SpellIds.MoteOfAnger);
        uint spellId = SpellIds.ManifestAngerMainHand;
        Player player = caster.ToPlayer();
        if (player != null && player.GetWeaponForAttack(WeaponAttackType.OffAttack, true) != null && RandomHelper.randChance(50))
            spellId = SpellIds.ManifestAngerOffHand;

        caster.CastSpell(target, spellId, aurEff);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.MoteOfAnger);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 26400 - Arcane Shroud
class spell_item_arcane_shroud : AuraScript
{
    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        int diff = (int)GetUnitOwner().GetLevel() - 60;
        if (diff > 0)
            amount += 2 * diff;
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModThreat));
    }
}

// Item - 31859: Darkmoon Card: Madness
[Script] // 39446 - Aura of Madness
class spell_item_aura_of_madness : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic,
            SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia) && CliDB.BroadcastTextStorage.ContainsKey(MiscConst.SayMadness);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        List<uint>[] triggeredSpells =
        [
            //ClassNone
            [],
            //ClassWarrior
            [ SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.MartyrComplex ],
            //ClassPaladin
            [SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia ],
            //ClassHunter
            [SpellIds.Delusional, SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia ],
            //ClassRogue
            [SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.MartyrComplex ],
            //ClassPriest
            [SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia ],
            //ClassDeathKnight
            [SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.MartyrComplex ],
            //ClassShaman
            [SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia ],
            //ClassMage
            [SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia ],
            //ClassWarlock
            [SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia ],
            //ClassUnk
            [],
            //ClassDruid
            [SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia ]
        ];

        PreventDefaultAction();
        Unit caster = eventInfo.GetActor();
        uint spellId = triggeredSpells[(int)caster.GetClass()].SelectRandom();
        caster.CastSpell(caster, spellId, aurEff);

        if (RandomHelper.randChance(10))
            caster.Say(MiscConst.SayMadness);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 41404 - Dementia
class spell_item_dementia : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DementiaPos, SpellIds.DementiaNeg);
    }

    void HandlePeriodicDummy(AuraEffect aurEff)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), RandomHelper.RAND(SpellIds.DementiaPos, SpellIds.DementiaNeg), aurEff);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandlePeriodicDummy, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 24590 - Brittle Armor
class spell_item_brittle_armor : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BrittleArmor);
    }

    void HandleScript(uint effIndex)
    {
        GetHitUnit().RemoveAuraFromStack(SpellIds.BrittleArmor);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 64411 - Blessing of Ancient Kings (Val'anyr, Hammer of Ancient Kings)
class spell_item_blessing_of_ancient_kings : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ProtectionOfAncientKings);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcTarget() != null;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        HealInfo healInfo = eventInfo.GetHealInfo();
        if (healInfo == null || healInfo.GetHeal() == 0)
            return;

        int absorb = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), 15.0f);
        AuraEffect protEff = eventInfo.GetProcTarget().GetAuraEffect(SpellIds.ProtectionOfAncientKings, 0, eventInfo.GetActor().GetGUID());
        if (protEff != null)
        {
            // The shield can grow to a maximum size of 20,000 damage absorbtion
            protEff.SetAmount(Math.Min(protEff.GetAmount() + absorb, 20000));

            // Refresh and return to prevent replacing the aura
            protEff.GetBase().RefreshDuration();
        }
        else
        {
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, absorb);
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.ProtectionOfAncientKings, args);
        }
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 64415 Val'anyr Hammer of Ancient Kings - Equip Effect
class spell_item_valanyr_hammer_of_ancient_kings : AuraScript
{
    bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetHealInfo() != null && eventInfo.GetHealInfo().GetEffectiveHeal() > 0;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

[Script] // 71564 - Deadly Precision
class spell_item_deadly_precision : AuraScript
{
    void HandleStackDrop(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().RemoveAuraFromStack(GetId(), GetTarget().GetGUID());
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleStackDrop, 0, AuraType.ModRating));
    }
}

[Script] // 71563 - Deadly Precision Dummy
class spell_item_deadly_precision_dummy : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DeadlyPrecision);
    }

    void HandleDummy(uint effIndex)
    {
        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.DeadlyPrecision, GetCastDifficulty());
        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.AuraStack, (int)spellInfo.StackAmount);
        GetCaster().CastSpell(GetCaster(), spellInfo.Id, args);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.ApplyAura));
    }
}

// Item - 50362: Deathbringer's Will
// 71519 - Item - Icecrown 25 Normal Melee Trinket

// Item - 50363: Deathbringer's Will
// 71562 - Item - Icecrown 25 Heroic Melee Trinket
[Script("spell_item_deathbringers_will_normal", SpellIds.StrengthOfTheTaunka, SpellIds.AgilityOfTheVrykul, SpellIds.PowerOfTheTaunka, SpellIds.AimOfTheIronDwarves, SpellIds.SpeedOfTheVrykul)]
[Script("spell_item_deathbringers_will_heroic", SpellIds.StrengthOfTheTaunkaHero, SpellIds.AgilityOfTheVrykulHero, SpellIds.PowerOfTheTaunkaHero, SpellIds.AimOfTheIronDwarvesHero, SpellIds.SpeedOfTheVrykulHero)]
class spell_item_deathbringers_will(uint StrengthSpellId, uint AgilitySpellId, uint APSpellId, uint CriticalSpellId, uint HasteSpellId) : AuraScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(StrengthSpellId, AgilitySpellId, APSpellId, CriticalSpellId, HasteSpellId);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        List<uint>[] triggeredSpells =
        [
            //ClassNone
            [],
            //ClassWarrior
            [StrengthSpellId, CriticalSpellId, HasteSpellId ],
                    //ClassPaladin
            [StrengthSpellId, CriticalSpellId, HasteSpellId ],
                    //ClassHunter
            [AgilitySpellId, CriticalSpellId, APSpellId ],
            //ClassRogue
            [AgilitySpellId, HasteSpellId, APSpellId ],
            //ClassPriest
            [],
            //ClassDeathKnight
            [StrengthSpellId, CriticalSpellId, HasteSpellId ],
            //ClassShaman
            [AgilitySpellId, HasteSpellId, APSpellId ],
            //ClassMage
            [],
            //ClassWarlock
            [],
            //ClassUnk
            [],
            //ClassDruid
            [StrengthSpellId, AgilitySpellId, HasteSpellId]
        ];

        PreventDefaultAction();
        Unit caster = eventInfo.GetActor();
        List<uint> randomSpells = triggeredSpells[(int)caster.GetClass()];
        if (randomSpells.Empty())
            return;

        uint spellId = randomSpells.SelectRandom();
        caster.CastSpell(caster, spellId, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 47770 - Roll Dice
class spell_item_decahedral_dwarven_dice : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        if (!CliDB.BroadcastTextStorage.ContainsKey(MiscConst.TextDecahedralDwarvenDice))
            return false;
        return true;
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleScript(uint effIndex)
    {
        GetCaster().TextEmote(MiscConst.TextDecahedralDwarvenDice, GetHitUnit());

        uint minimum = 1;
        uint maximum = 100;

        GetCaster().ToPlayer().DoRandomRoll(minimum, maximum);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 23134 - Goblin Bomb
class spell_item_goblin_bomb_dispenser : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SummonGoblinBomb, SpellIds.MalfunctionExplosion);
    }

    void HandleDummy(uint effIndex)
    {
        Item item = GetCastItem();
        if (item != null)
            GetCaster().CastSpell(GetCaster(), RandomHelper.randChance(95) ? SpellIds.SummonGoblinBomb : SpellIds.MalfunctionExplosion, item);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 46203 - Goblin Weather Machine
class spell_item_goblin_weather_machine : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Unit target = GetHitUnit();

        uint spellId = RandomHelper.RAND(SpellIds.PersonalizedWeather1, SpellIds.PersonalizedWeather2, SpellIds.PersonalizedWeather3, SpellIds.PersonalizedWeather4);
        target.CastSpell(target, spellId, GetSpell());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

// 8342  - Defibrillate (Goblin Jumper Cables) have 33% chance on success
// 22999 - Defibrillate (Goblin Jumper Cables Xl) have 50% chance on success
// 54732 - Defibrillate (Gnomish Army Knife) have 67% chance on success
[Script("spell_item_goblin_jumper_cables", 67, SpellIds.GoblinJumperCablesFail)]
[Script("spell_item_goblin_jumper_cables_xl", 50, SpellIds.GoblinJumperCablesXlFail)]
[Script("spell_item_gnomish_army_knife", 33, 0)]
class spell_item_defibrillate(byte chance, uint failSpell) : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return failSpell == 0 || ValidateSpellInfo(failSpell);
    }

    void HandleScript(uint effIndex)
    {
        if (RandomHelper.randChance(chance))
        {
            PreventHitDefaultEffect(effIndex);
            if (failSpell != 0)
                GetCaster().CastSpell(GetCaster(), failSpell, GetCastItem());
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.Resurrect));
    }
}

[Script] // 33896 - Desperate Defense
class spell_item_desperate_defense : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DesperateRage);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), SpellIds.DesperateRage, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 2, AuraType.ProcTriggerSpell));
    }
}

// http://www.wowhead.com/item=6522 Deviate Fish
[Script] // 8063 Deviate Fish
class spell_item_deviate_fish : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Sleepy, SpellIds.Invigorate, SpellIds.Shrink, SpellIds.PartyTime, SpellIds.HealthySpirit, SpellIds.Rejuvenation);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        uint spellId = RandomHelper.RAND(SpellIds.Sleepy, SpellIds.Invigorate, SpellIds.Shrink, SpellIds.PartyTime, SpellIds.HealthySpirit, SpellIds.Rejuvenation);
        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_party_time : AuraScript
{
    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Player player = GetOwner().ToPlayer();
        if (player == null)
            return;

        player.m_Events.AddEventAtOffset(new PartyTimeEmoteEvent(player), RandomHelper.RAND(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15)));
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }

    class PartyTimeEmoteEvent(Player player) : BasicEvent
    {
        public override bool Execute(ulong time, uint diff)
        {
            if (!player.HasAura(SpellIds.PartyTime))
                return true;

            if (player.IsMoving())
                player.HandleEmoteCommand(RandomHelper.RAND(Emote.OneshotApplaud, Emote.OneshotLaugh, Emote.OneshotCheer, Emote.OneshotChicken));
            else
                player.HandleEmoteCommand(RandomHelper.RAND(Emote.OneshotApplaud, Emote.OneshotDancespecial, Emote.OneshotLaugh, Emote.OneshotCheer, Emote.OneshotChicken));

            player.m_Events.AddEventAtOffset(this, RandomHelper.RAND(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15)));

            return false; // do not delete re-added event in EventProcessor::Update
        }
    }
}

[Script] // 51010 - Dire Brew
class spell_item_dire_brew : AuraScript
{
    void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();

        uint model = 0;
        Gender gender = target.GetGender();
        var chrClass = CliDB.ChrClassesStorage.LookupByKey(target.GetClass());
        if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Plate)) != 0)
            model = gender == Gender.Male ? MiscConst.ModelClassPlateMale : MiscConst.ModelClassPlateFemale;
        else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Mail)) != 0)
            model = gender == Gender.Male ? MiscConst.ModelClassMailMale : MiscConst.ModelClassMailFemale;
        else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Leather)) != 0)
            model = gender == Gender.Male ? MiscConst.ModelClassLeatherMale : MiscConst.ModelClassLeatherFemale;
        else if ((chrClass.ArmorTypeMask & (1 << (int)ItemSubClassArmor.Cloth)) != 0)
            model = gender == Gender.Male ? MiscConst.ModelClassClothMale : MiscConst.ModelClassClothFemale;

        if (model != 0)
            target.SetDisplayId(model);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(AfterApply, 0, AuraType.Transform, AuraEffectHandleModes.Real));
    }
}

[Script] // 59915 - Discerning Eye of the Beast Dummy
class spell_item_discerning_eye_beast_dummy : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DiscerningEyeBeast);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        eventInfo.GetActor().CastSpell(null, SpellIds.DiscerningEyeBeast, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 71610, 71641 - Echoes of Light (Althor's Abacus)
class spell_item_echoes_of_light : SpellScript
{
    void FilterTargets(List<WorldObject> targets)
    {
        if (targets.Count < 2)
            return;

        targets.Sort(new HealthPctOrderPred());

        WorldObject target = targets.First();
        targets.Clear();
        targets.Add(target);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
    }
}

[Script] // 30427 - Extract Gas (23821: Zapthrottle Mote Extractor)
class spell_item_extract_gas : AuraScript
{
    void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();

        // move loot to player inventory and despawn target
        if (GetCaster() != null && GetCaster().IsPlayer() && GetTarget().IsUnit() &&
            GetTarget().ToCreature().GetCreatureTemplate().CreatureType == CreatureType.GasCloud)
        {
            Player player = GetCaster().ToPlayer();
            Creature creature = GetTarget().ToCreature();
            CreatureDifficulty creatureDifficulty = creature.GetCreatureDifficulty();
            // missing lootid has been reported on startup - just return
            if (creatureDifficulty.SkinLootID == 0)
                return;

            player.AutoStoreLoot(creatureDifficulty.SkinLootID, LootStorage.Skinning, ItemContext.None, true);
            creature.DespawnOrUnsummon();
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // 7434 - Fate Rune of Unsurpassed Vigor
class spell_item_fate_rune_of_unsurpassed_vigor : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.UnsurpassedVigor);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), SpellIds.UnsurpassedVigor, true);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

/* 57301 - Great Feast
   57426 - Fish Feast
   58465 - Gigantic Feast
   58474 - Small Feast
   66476 - Bountiful Feast */
[Script("spell_item_great_feast", MiscConst.TextGreatFeast)]
[Script("spell_item_fish_feast", MiscConst.TextFishFeast)]
[Script("spell_item_gigantic_feast", MiscConst.TextGiganticFeast)]
[Script("spell_item_small_feast", MiscConst.TextSmallFeast)]
[Script("spell_item_bountiful_feast", MiscConst.TextBountifulFeast)]
class spell_item_feast(uint text) : SpellScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return CliDB.BroadcastTextStorage.ContainsKey(text);
    }

    void HandleScript(uint effIndex)
    {
        Unit caster = GetCaster();
        caster.TextEmote(text, caster, false);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

// http://www.wowhead.com/item=47499 Flask of the North
[Script] // 67019 Flask of the North
class spell_item_flask_of_the_north : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlaskOfTheNorthSp, SpellIds.FlaskOfTheNorthAp, SpellIds.FlaskOfTheNorthStr);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        List<uint> possibleSpells = new();
        switch (caster.GetClass())
        {
            case Class.Warlock:
            case Class.Mage:
            case Class.Priest:
                possibleSpells.Add(SpellIds.FlaskOfTheNorthSp);
                break;
            case Class.DeathKnight:
            case Class.Warrior:
                possibleSpells.Add(SpellIds.FlaskOfTheNorthStr);
                break;
            case Class.Rogue:
            case Class.Hunter:
                possibleSpells.Add(SpellIds.FlaskOfTheNorthAp);
                break;
            case Class.Druid:
            case Class.Paladin:
                possibleSpells.Add(SpellIds.FlaskOfTheNorthSp);
                possibleSpells.Add(SpellIds.FlaskOfTheNorthStr);
                break;
            case Class.Shaman:
                possibleSpells.Add(SpellIds.FlaskOfTheNorthSp);
                possibleSpells.Add(SpellIds.FlaskOfTheNorthAp);
                break;
        }

        if (possibleSpells.Empty())
        {
            Log.outWarn(LogFilter.Spells, $"Missing spells for class {caster.GetClass()} in script spell_item_flask_of_the_north");
            return;
        }

        caster.CastSpell(caster, possibleSpells.SelectRandom(), true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// 39372 - Frozen Shadoweave
[Script] // Frozen Shadoweave set 3p bonus
class spell_item_frozen_shadoweave : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Shadowmend);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo == null || damageInfo.GetDamage() == 0)
            return;

        Unit caster = eventInfo.GetActor();
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount()));
        caster.CastSpell(null, SpellIds.Shadowmend, args);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

// http://www.wowhead.com/item=10645 Gnomish Death Ray
[Script] // 13280 Gnomish Death Ray
class spell_item_gnomish_death_ray : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GnomishDeathRaySelf, SpellIds.GnomishDeathRayTarget);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit target = GetHitUnit();
        if (target != null)
        {
            if (RandomHelper.URand(0, 99) < 15)
                caster.CastSpell(caster, SpellIds.GnomishDeathRaySelf, true);    // failure
            else
                caster.CastSpell(target, SpellIds.GnomishDeathRayTarget, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// Item 10721: Gnomish Harm Prevention Belt
[Script] // 13234 - Harm Prevention Belt
class spell_item_harm_prevention_belt : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ForcefieldCollapse);
    }

    void HandleProc(ProcEventInfo eventInfo)
    {
        GetTarget().CastSpell(null, SpellIds.ForcefieldCollapse, true);
    }

    public override void Register()
    {
        OnProc.Add(new(HandleProc));
    }
}

// Item - 49982: Heartpierce
// 71880 - Item - Icecrown 25 Normal Dagger Proc

// Item - 50641: Heartpierce (Heroic)
// 71892 - Item - Icecrown 25 Heroic Dagger Proc
[Script("spell_item_heartpierce", SpellIds.InvigorationEnergy, SpellIds.InvigorationMana, SpellIds.InvigorationRage, SpellIds.InvigorationRp)]
[Script("spell_item_heartpierce_hero", SpellIds.InvigorationEnergyHero, SpellIds.InvigorationManaHero, SpellIds.InvigorationRageHero, SpellIds.InvigorationRpHero)]
class spell_item_heartpierce(uint Energy, uint Mana, uint Rage, uint RunicPower) : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(Energy, Mana, Rage, RunicPower);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Unit caster = eventInfo.GetActor();

        uint spellId;
        switch (caster.GetPowerType())
        {
            case PowerType.Mana:
                spellId = Mana;
                break;
            case PowerType.Energy:
                spellId = Energy;
                break;
            case PowerType.Rage:
                spellId = Rage;
                break;
            // Death Knights can't use daggers, but oh well
            case PowerType.RunicPower:
                spellId = RunicPower;
                break;
            default:
                return;
        }

        caster.CastSpell(null, spellId, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 23645 - Hourglass Sand
class spell_item_hourglass_sand : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BroodAfflictionBronze);
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().RemoveAurasDueToSpell(SpellIds.BroodAfflictionBronze);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 40971 - Bonus Healing (Crystal Spire of Karabor)
class spell_item_crystal_spire_of_karabor : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 0));
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        int pct = GetSpellInfo().GetEffect(0).CalcValue();
        HealInfo healInfo = eventInfo.GetHealInfo();
        if (healInfo != null)
        {
            Unit healTarget = healInfo.GetTarget();
            if (healTarget != null && healTarget.GetHealth() - healInfo.GetEffectiveHeal() <= healTarget.CountPctFromMaxHealth(pct))
                return true;
        }

        return false;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

// http://www.wowhead.com/item=27388 Mr. Pinchy
[Script] // 33060 Make a Wish
class spell_item_make_a_wish : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MrPinchysBlessing, SpellIds.SummonMightyMrPinchy, SpellIds.SummonFuriousMrPinchy, SpellIds.TinyMagicalCrawdad, SpellIds.MrPinchysGift);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        uint spellId = SpellIds.MrPinchysGift;
        switch (RandomHelper.URand(1, 5))
        {
            case 1:
                spellId = SpellIds.MrPinchysBlessing;
                break;
            case 2:
                spellId = SpellIds.SummonMightyMrPinchy;
                break;
            case 3:
                spellId = SpellIds.SummonFuriousMrPinchy;
                break;
            case 4:
                spellId = SpellIds.TinyMagicalCrawdad;
                break;
        }
        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// Item - 27920: Mark of Conquest
// Item - 27921: Mark of Conquest
[Script] // 33510 - Health Restore
class spell_item_mark_of_conquest : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MarkOfConquestEnergize);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        if (eventInfo.GetTypeMask() & new ProcFlagsInit(ProcFlags.DealRangedAttack | ProcFlags.DealRangedAbility))
        {
            // in that case, do not cast heal spell
            PreventDefaultAction();
            // but mana instead
            eventInfo.GetActor().CastSpell(null, SpellIds.MarkOfConquestEnergize, aurEff);
        }
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 26465 - Mercurial Shield
class spell_item_mercurial_shield : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MercurialShield);
    }

    void HandleScript(uint effIndex)
    {
        GetHitUnit().RemoveAuraFromStack(SpellIds.MercurialShield);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

// http://www.wowhead.com/item=32686 Mingo's Fortune Giblets
[Script] // 40802 Mingo's Fortune Generator
class spell_item_mingos_fortune_generator : SpellScript
{
    uint[] CreateFortuneSpells =
    [
        SpellIds.CreateFortune1, SpellIds.CreateFortune2, SpellIds.CreateFortune3, SpellIds.CreateFortune4, SpellIds.CreateFortune5,
        SpellIds.CreateFortune6, SpellIds.CreateFortune7, SpellIds.CreateFortune8, SpellIds.CreateFortune9, SpellIds.CreateFortune10,
        SpellIds.CreateFortune11, SpellIds.CreateFortune12, SpellIds.CreateFortune13, SpellIds.CreateFortune14, SpellIds.CreateFortune15,
        SpellIds.CreateFortune16, SpellIds.CreateFortune17, SpellIds.CreateFortune18, SpellIds.CreateFortune19, SpellIds.CreateFortune20
    ];

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(CreateFortuneSpells);
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), CreateFortuneSpells.SelectRandom(), true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 71875, 71877 - Item - Black Bruise: Necrotic Touch Proc
class spell_item_necrotic_touch : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ItemNecroticTouchProc);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcTarget() != null && eventInfo.GetProcTarget().IsAlive();
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo == null || damageInfo.GetDamage() == 0)
            return;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount()));
        GetTarget().CastSpell(null, SpellIds.ItemNecroticTouchProc, args);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

// http://www.wowhead.com/item=10720 Gnomish Net-o-Matic Projector
[Script] // 13120 Net-o-Matic
class spell_item_net_o_matic : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NetOMaticTRIGGERED1, SpellIds.NetOMaticTRIGGERED2, SpellIds.NetOMaticTRIGGERED3);
    }

    void HandleDummy(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (target != null)
        {
            uint spellId = SpellIds.NetOMaticTRIGGERED3;
            uint roll = RandomHelper.URand(0, 99);
            if (roll < 2)                            // 2% for 30 sec self root (off-like chance unknown)
                spellId = SpellIds.NetOMaticTRIGGERED1;
            else if (roll < 4)                       // 2% for 20 sec root, charge to target (off-like chance unknown)
                spellId = SpellIds.NetOMaticTRIGGERED2;

            GetCaster().CastSpell(target, spellId, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// http://www.wowhead.com/item=8529 Noggenfogger Elixir
[Script] // 16589 Noggenfogger Elixir
class spell_item_noggenfogger_elixir : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NoggenfoggerElixirTRIGGERED1, SpellIds.NoggenfoggerElixirTRIGGERED2, SpellIds.NoggenfoggerElixirTRIGGERED3);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        uint spellId = SpellIds.NoggenfoggerElixirTRIGGERED3;
        switch (RandomHelper.URand(1, 3))
        {
            case 1:
                spellId = SpellIds.NoggenfoggerElixirTRIGGERED1;
                break;
            case 2:
                spellId = SpellIds.NoggenfoggerElixirTRIGGERED2;
                break;
        }

        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 29601 - Enlightenment (Pendant of the Violet Eye)
class spell_item_pendant_of_the_violet_eye : AuraScript
{
    bool CheckProc(ProcEventInfo eventInfo)
    {
        Spell spell = eventInfo.GetProcSpell();
        if (spell != null && spell.GetPowerTypeCostAmount(PowerType.Mana) > 0)
            return true;

        return false;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

[Script] // 26467 - Persistent Shield
class spell_item_persistent_shield : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PersistentShieldTriggered);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.GetHealInfo() != null && eventInfo.GetHealInfo().GetHeal() != 0;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();
        int bp0 = (int)MathFunctions.CalculatePct(eventInfo.GetHealInfo().GetHeal(), 15);

        // Scarab Brooch does not replace stronger shields
        AuraEffect shield = target.GetAuraEffect(SpellIds.PersistentShieldTriggered, 0, caster.GetGUID());
        if (shield != null && shield.GetAmount() > bp0)
            return;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
        caster.CastSpell(target, SpellIds.PersistentShieldTriggered, args);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

// 37381 - Pet Healing
// Hunter T5 2P Bonus
[Script] // Warlock T5 2P Bonus
class spell_item_pet_healing : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HealthLink);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo == null || damageInfo.GetDamage() == 0)
            return;

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount()));
        eventInfo.GetActor().CastSpell(null, SpellIds.HealthLink, args);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 17512 - Piccolo of the Flaming Fire
class spell_item_piccolo_of_the_flaming_fire : SpellScript
{
    void HandleScript(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        Player target = GetHitPlayer();
        if (target != null)
            target.HandleEmoteCommand(Emote.StateDance);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 45043 - Power Circle (Shifting Naaru Sliver)
class spell_item_power_circle : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LimitlessPower);
    }

    bool CheckCaster(Unit target)
    {
        return target.GetGUID() == GetCasterGUID();
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().CastSpell(null, SpellIds.LimitlessPower, true);
        Aura buff = GetTarget().GetAura(SpellIds.LimitlessPower);
        if (buff != null)
            buff.SetDuration(GetDuration());
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.LimitlessPower);
    }

    public override void Register()
    {
        DoCheckAreaTarget.Add(new(CheckCaster));

        AfterEffectApply.Add(new(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

// http://www.wowhead.com/item=6657 Savory Deviate Delight
[Script] // 8213 Savory Deviate Delight
class spell_item_savory_deviate_delight : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FlipOutMale, SpellIds.FlipOutFemale, SpellIds.YaaarrrrMale, SpellIds.YaaarrrrFemale);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        uint spellId = 0;
        switch (RandomHelper.URand(1, 2))
        {
            // Flip Out - ninja
            case 1: spellId = (caster.GetNativeGender() == Gender.Male ? SpellIds.FlipOutMale : SpellIds.FlipOutFemale); break;
            // Yaaarrrr - pirate
            case 2: spellId = (caster.GetNativeGender() == Gender.Male ? SpellIds.YaaarrrrMale : SpellIds.YaaarrrrFemale); break;
        }
        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// 48129 - Scroll of Recall
// 60320 - Scroll of Recall Ii
[Script] // 60321 - Scroll of Recall Iii
class spell_item_scroll_of_recall : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleScript(uint effIndex)
    {
        Unit caster = GetCaster();
        byte maxSafeLevel = 0;
        switch (GetSpellInfo().Id)
        {
            case SpellIds.ScrollOfRecallI:  // Scroll of Recall
                maxSafeLevel = 40;
                break;
            case SpellIds.ScrollOfRecallIi:  // Scroll of Recall Ii
                maxSafeLevel = 70;
                break;
            case SpellIds.ScrollOfRecallIii:  // Scroll of Recal Iii
                maxSafeLevel = 80;
                break;
            default:
                break;
        }

        if (caster.GetLevel() > maxSafeLevel)
        {
            caster.CastSpell(caster, SpellIds.Lost, true);

            // Alliance from 60323 to 60330 - Horde from 60328 to 60335
            uint spellId = SpellIds.ScrollOfRecallFailAlliance1;
            if (GetCaster().ToPlayer().GetTeam() == Team.Horde)
                spellId = SpellIds.ScrollOfRecallFailHorde1;

            GetCaster().CastSpell(GetCaster(), spellId + RandomHelper.URand(0, 7), true);

            PreventHitDefaultEffect(effIndex);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.TeleportUnits));
    }
}

[Script] // 23442 - Dimensional Ripper - Everlook
class spell_item_dimensional_ripper_everlook : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TransporterMalfunctionFire, SpellIds.EvilTwin);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleScript(uint effIndex)
    {
        int r = RandomHelper.IRand(0, 119);
        if (r <= 70)                               // 7/12 success
            return;

        Unit caster = GetCaster();

        if (r < 100)                              // 4/12 evil twin
            caster.CastSpell(caster, SpellIds.EvilTwin, true);
        else                                      // 1/12 fire
            caster.CastSpell(caster, SpellIds.TransporterMalfunctionFire, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.TeleportUnits));
    }
}

[Script] // 36941 - Ultrasafe Transporter: Toshley's Station
class spell_item_ultrasafe_transporter : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TransporterMalfunctionSmaller, SpellIds.TransporterMalfunctionBigger, SpellIds.SoulSplitEvil, SpellIds.SoulSplitGood, SpellIds.TransformHorde, SpellIds.TransformAlliance, SpellIds.TransporterMalfunctionChicken, SpellIds.EvilTwin);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleScript(uint effIndex)
    {
        if (!RandomHelper.randChance(50)) // 50% success
            return;

        Unit caster = GetCaster();

        uint spellId = 0;
        switch (RandomHelper.URand(0, 6))
        {
            case 0:
                spellId = SpellIds.TransporterMalfunctionSmaller;
                break;
            case 1:
                spellId = SpellIds.TransporterMalfunctionBigger;
                break;
            case 2:
                spellId = SpellIds.SoulSplitEvil;
                break;
            case 3:
                spellId = SpellIds.SoulSplitGood;
                break;
            case 4:
                if (caster.ToPlayer().GetTeam() == Team.Alliance)
                    spellId = SpellIds.TransformHorde;
                else
                    spellId = SpellIds.TransformAlliance;
                break;
            case 5:
                spellId = SpellIds.TransporterMalfunctionChicken;
                break;
            case 6:
                spellId = SpellIds.EvilTwin;
                break;
            default:
                break;
        }

        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.TeleportUnits));
    }
}

[Script] // 36890 - Dimensional Ripper - Area 52
class spell_item_dimensional_ripper_area52 : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TransporterMalfunctionBigger, SpellIds.SoulSplitEvil, SpellIds.SoulSplitGood, SpellIds.TransformHorde, SpellIds.TransformAlliance);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleScript(uint effIndex)
    {
        if (!RandomHelper.randChance(50)) // 50% success
            return;

        Unit caster = GetCaster();

        uint spellId = 0;
        switch (RandomHelper.URand(0, 3))
        {
            case 0:
                spellId = SpellIds.TransporterMalfunctionBigger;
                break;
            case 1:
                spellId = SpellIds.SoulSplitEvil;
                break;
            case 2:
                spellId = SpellIds.SoulSplitGood;
                break;
            case 3:
                if (caster.ToPlayer().GetTeam() == Team.Alliance)
                    spellId = SpellIds.TransformHorde;
                else
                    spellId = SpellIds.TransformAlliance;
                break;
            default:
                break;
        }

        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.TeleportUnits));
    }
}

[Script] // 71169 - Shadow's Fate (Shadowmourne questline)
class spell_item_unsated_craving : AuraScript
{
    bool CheckProc(ProcEventInfo procInfo)
    {
        Unit caster = procInfo.GetActor();
        if (caster == null || caster.GetTypeId() != TypeId.Player)
            return false;

        Unit target = procInfo.GetActionTarget();
        if (target == null || target.GetTypeId() != TypeId.Unit || target.IsCritter() || (target.GetEntry() != MiscConst.NpcSindragosa && target.IsSummon()))
            return false;

        return true;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

[Script]
class spell_item_shadows_fate : AuraScript
{
    void HandleProc(ProcEventInfo procInfo)
    {
        PreventDefaultAction();

        Unit caster = procInfo.GetActor();
        Unit target = GetCaster();
        if (caster == null || target == null)
            return;

        caster.CastSpell(target, SpellIds.SoulFeast, TriggerCastFlags.FullMask);
    }

    public override void Register()
    {
        OnProc.Add(new(HandleProc));
    }
}

[Script] // 71903 - Item - Shadowmourne Legendary
class spell_item_shadowmourne : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShadowmourneChaosBaneDamage, SpellIds.ShadowmourneSoulFragment, SpellIds.ShadowmourneChaosBaneBuff);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        if (GetTarget().HasAura(SpellIds.ShadowmourneChaosBaneBuff)) // cant collect shards while under effect of Chaos Bane buff
            return false;
        return eventInfo.GetProcTarget() != null && eventInfo.GetProcTarget().IsAlive();
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(GetTarget(), SpellIds.ShadowmourneSoulFragment, aurEff);

        // this can't be handled in AuraScript of SoulFragments because we need to know victim
        Aura soulFragments = GetTarget().GetAura(SpellIds.ShadowmourneSoulFragment);
        if (soulFragments != null)
        {
            if (soulFragments.GetStackAmount() >= 10)
            {
                GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.ShadowmourneChaosBaneDamage, aurEff);
                soulFragments.Remove();
            }
        }
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.ShadowmourneSoulFragment);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 71905 - Soul Fragment
class spell_item_shadowmourne_soul_fragment : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShadowmourneVisualLow, SpellIds.ShadowmourneVisualHigh, SpellIds.ShadowmourneChaosBaneBuff);
    }

    void OnStackChange(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        switch (GetStackAmount())
        {
            case 1:
                target.CastSpell(target, SpellIds.ShadowmourneVisualLow, true);
                break;
            case 6:
                target.RemoveAurasDueToSpell(SpellIds.ShadowmourneVisualLow);
                target.CastSpell(target, SpellIds.ShadowmourneVisualHigh, true);
                break;
            case 10:
                target.RemoveAurasDueToSpell(SpellIds.ShadowmourneVisualHigh);
                target.CastSpell(target, SpellIds.ShadowmourneChaosBaneBuff, true);
                break;
            default:
                break;
        }
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.RemoveAurasDueToSpell(SpellIds.ShadowmourneVisualLow);
        target.RemoveAurasDueToSpell(SpellIds.ShadowmourneVisualHigh);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnStackChange, 0, AuraType.ModStat, AuraEffectHandleModes.Real | AuraEffectHandleModes.Reapply));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.ModStat, AuraEffectHandleModes.Real));
    }
}

// http://www.wowhead.com/item=7734 Six Demon Bag
[Script] // 14537 Six Demon Bag
class spell_item_six_demon_bag : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Frostbolt, SpellIds.Polymorph, SpellIds.SummonFelhoundMinion, SpellIds.Fireball, SpellIds.ChainLightning, SpellIds.EnvelopingWinds);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit target = GetHitUnit();
        if (target != null)
        {
            uint spellId = 0;
            uint rand = RandomHelper.URand(0, 99);
            if (rand < 25)                      // Fireball (25% chance)
                spellId = SpellIds.Fireball;
            else if (rand < 50)                 // Frostball (25% chance)
                spellId = SpellIds.Frostbolt;
            else if (rand < 70)                 // Chain Lighting (20% chance)
                spellId = SpellIds.ChainLightning;
            else if (rand < 80)                 // Polymorph (10% chance)
            {
                spellId = SpellIds.Polymorph;
                if (RandomHelper.URand(0, 100) <= 30)        // 30% chance to self-cast
                    target = caster;
            }
            else if (rand < 95)                 // Enveloping Winds (15% chance)
                spellId = SpellIds.EnvelopingWinds;
            else                                // Summon Felhund minion (5% chance)
            {
                spellId = SpellIds.SummonFelhoundMinion;
                target = caster;
            }

            caster.CastSpell(target, spellId, GetCastItem());
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 59906 - Swift Hand of Justice Dummy
class spell_item_swift_hand_justice_dummy : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SwiftHandOfJusticeHeal);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActor();
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)caster.CountPctFromMaxHealth(aurEff.GetAmount()));
        caster.CastSpell(null, SpellIds.SwiftHandOfJusticeHeal, args);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 28862 - The Eye of Diminution
class spell_item_the_eye_of_diminution : AuraScript
{
    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        int diff = (int)GetUnitOwner().GetLevel() - 60;
        if (diff > 0)
            amount += diff;
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModThreat));
    }
}

// http://www.wowhead.com/item=44012 Underbelly Elixir
[Script] // 59640 Underbelly Elixir
class spell_item_underbelly_elixir : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.UnderbellyElixirTriggered1, SpellIds.UnderbellyElixirTriggered2, SpellIds.UnderbellyElixirTriggered3);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        uint spellId = SpellIds.UnderbellyElixirTriggered3;
        switch (RandomHelper.URand(1, 3))
        {
            case 1:
                spellId = SpellIds.UnderbellyElixirTriggered1;
                break;
            case 2:
                spellId = SpellIds.UnderbellyElixirTriggered2;
                break;
        }
        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 126755 - Wormhole: Pandaria
class spell_item_wormhole_pandaria : SpellScript
{
    uint[] WormholeTargetLocations =
    {
        SpellIds.WormholePandariaIsleOfReckoning,
        SpellIds.WormholePandariaKunlaiUnderwater,
        SpellIds.WormholePandariaSraVess,
        SpellIds.WormholePandariaRikkitunVillage,
        SpellIds.WormholePandariaZanvessTree,
        SpellIds.WormholePandariaAnglersWharf,
        SpellIds.WormholePandariaCraneStatue,
        SpellIds.WormholePandariaEmperorsOmen,
        SpellIds.WormholePandariaWhitepetalLake
    };

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(WormholeTargetLocations);
    }

    void HandleTeleport(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        uint spellId = WormholeTargetLocations.SelectRandom();
        GetCaster().CastSpell(GetHitUnit(), spellId, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleTeleport, 0, SpellEffectName.Dummy));
    }
}

[Script] // 47776 - Roll 'dem Bones
class spell_item_worn_troll_dice : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        if (!CliDB.BroadcastTextStorage.ContainsKey(MiscConst.TextWornTrollDice))
            return false;
        return true;
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleScript(uint effIndex)
    {
        GetCaster().TextEmote(MiscConst.TextWornTrollDice, GetHitUnit());

        uint minimum = 1;
        uint maximum = 6;

        // roll twice
        GetCaster().ToPlayer().DoRandomRoll(minimum, maximum);
        GetCaster().ToPlayer().DoRandomRoll(minimum, maximum);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_item_red_rider_air_rifle : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AirRifleHoldVisual, SpellIds.AirRifleShoot, SpellIds.AirRifleShootSelf);
    }

    void HandleScript(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        Unit caster = GetCaster();
        Unit target = GetHitUnit();
        if (target != null)
        {
            caster.CastSpell(caster, SpellIds.AirRifleHoldVisual, true);
            // needed because this spell shares Gcd with its triggered spells (which must not be cast with triggered flag)
            Player player = caster.ToPlayer();
            if (player != null)
                player.GetSpellHistory().CancelGlobalCooldown(GetSpellInfo());
            if (RandomHelper.URand(0, 4) != 0)
                caster.CastSpell(target, SpellIds.AirRifleShoot, false);
            else
                caster.CastSpell(caster, SpellIds.AirRifleShootSelf, false);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_item_book_of_glyph_mastery : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    SpellCastResult CheckRequirement()
    {
        if (SkillDiscovery.HasDiscoveredAllSpells(GetSpellInfo().Id, GetCaster().ToPlayer()))
        {
            SetCustomCastResultMessage(SpellCustomErrors.LearnedEverything);
            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }

    void HandleScript(uint effIndex)
    {
        Player caster = GetCaster().ToPlayer();
        uint spellId = GetSpellInfo().Id;

        // learn random explicit discovery recipe (if any)
        uint discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);
        if (discoveredSpellId != 0)
            caster.LearnSpell(discoveredSpellId, false);
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckRequirement));
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_item_gift_of_the_harvester : SpellScript
{
    SpellCastResult CheckRequirement()
    {
        List<TempSummon> ghouls = GetCaster().GetAllMinionsByEntry(MiscConst.NpcGhoul);
        if (ghouls.Count >= MiscConst.MaxGhouls)
        {
            SetCustomCastResultMessage(SpellCustomErrors.TooManyGhouls);
            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckRequirement));
    }
}

[Script]
class spell_item_map_of_the_geyser_fields : SpellScript
{
    SpellCastResult CheckSinkholes()
    {
        Unit caster = GetCaster();
        if (caster.FindNearestCreature(MiscConst.NpcSouthSinkhole, 30.0f, true) != null ||
            caster.FindNearestCreature(MiscConst.NpcNortheastSinkhole, 30.0f, true) != null ||
            caster.FindNearestCreature(MiscConst.NpcNorthwestSinkhole, 30.0f, true) != null)
            return SpellCastResult.SpellCastOk;

        SetCustomCastResultMessage(SpellCustomErrors.MustBeCloseToSinkhole);
        return SpellCastResult.CustomError;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckSinkholes));
    }
}

[Script]
class spell_item_vanquished_clutches : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Crusher, SpellIds.Constrictor, SpellIds.Corruptor);
    }

    void HandleDummy(uint effIndex)
    {
        uint spellId = RandomHelper.RAND(SpellIds.Crusher, SpellIds.Constrictor, SpellIds.Corruptor);
        Unit caster = GetCaster();
        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_ashbringer : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void OnDummyEffect(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        Player player = GetCaster().ToPlayer();
        uint sound_id = RandomHelper.RAND(MiscConst.SoundAshbringer1, MiscConst.SoundAshbringer2, MiscConst.SoundAshbringer3, MiscConst.SoundAshbringer4, MiscConst.SoundAshbringer5, MiscConst.SoundAshbringer6,
                       MiscConst.SoundAshbringer7, MiscConst.SoundAshbringer8, MiscConst.SoundAshbringer9, MiscConst.SoundAshbringer10, MiscConst.SoundAshbringer11, MiscConst.SoundAshbringer12);

        // Ashbringers effect (spellId 28441) retriggers every 5 seconds, with a chance of making it say one of the above 12 sounds
        if (RandomHelper.URand(0, 60) < 1)
            player.PlayDirectSound(sound_id, player);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(OnDummyEffect, 0, SpellEffectName.Dummy));
    }
}

[Script] // 58886 - Food
class spell_magic_eater_food : AuraScript
{
    void HandleTriggerSpell(AuraEffect aurEff)
    {
        PreventDefaultAction();
        Unit target = GetTarget();
        switch (RandomHelper.URand(0, 5))
        {
            case 0:
                target.CastSpell(target, SpellIds.WildMagic, true);
                break;
            case 1:
                target.CastSpell(target, SpellIds.WellFed1, true);
                break;
            case 2:
                target.CastSpell(target, SpellIds.WellFed2, true);
                break;
            case 3:
                target.CastSpell(target, SpellIds.WellFed3, true);
                break;
            case 4:
                target.CastSpell(target, SpellIds.WellFed4, true);
                break;
            case 5:
                target.CastSpell(target, SpellIds.WellFed5, true);
                break;
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleTriggerSpell, 1, AuraType.PeriodicTriggerSpell));
    }
}

[Script]
class spell_item_purify_helboar_meat : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SummonPurifiedHelboarMeat, SpellIds.SummonToxicHelboarMeat);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        caster.CastSpell(caster, RandomHelper.randChance(50) ? SpellIds.SummonPurifiedHelboarMeat : SpellIds.SummonToxicHelboarMeat, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_nigh_invulnerability : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NighInvulnerability, SpellIds.CompleteVulnerability);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        Item castItem = GetCastItem();
        if (castItem != null)
        {
            if (RandomHelper.randChance(86))                  // Nigh-Invulnerability   - success
                caster.CastSpell(caster, SpellIds.NighInvulnerability, castItem);
            else                                    // Complete Vulnerability - backfire in 14% casts
                caster.CastSpell(caster, SpellIds.CompleteVulnerability, castItem);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_poultryizer : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PoultryizerSuccess, SpellIds.PoultryizerBackfire);
    }

    void HandleDummy(uint effIndex)
    {
        if (GetCastItem() != null && GetHitUnit() != null)
            GetCaster().CastSpell(GetHitUnit(), RandomHelper.randChance(80) ? SpellIds.PoultryizerSuccess : SpellIds.PoultryizerBackfire, GetCastItem());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_socrethars_stone : SpellScript
{
    public override bool Load()
    {
        return (GetCaster().GetAreaId() == 3900 || GetCaster().GetAreaId() == 3742);
    }
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SocretharToSeat, SpellIds.SocretharFromSeat);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        switch (caster.GetAreaId())
        {
            case 3900:
                caster.CastSpell(caster, SpellIds.SocretharToSeat, true);
                break;
            case 3742:
                caster.CastSpell(caster, SpellIds.SocretharFromSeat, true);
                break;
            default:
                return;
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_demon_broiled_surprise : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CreateDemonBroiledSurprise) &&
            Global.ObjectMgr.GetCreatureTemplate(MiscConst.NpcAbyssalFlamebringer) != null &&
            Global.ObjectMgr.GetQuestTemplate(MiscConst.QuestSuperHotStew) != null;
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleDummy(uint effIndex)
    {
        Unit player = GetCaster();
        player.CastSpell(player, SpellIds.CreateDemonBroiledSurprise, false);
    }

    SpellCastResult CheckRequirement()
    {
        Player player = GetCaster().ToPlayer();
        if (player.GetQuestStatus(MiscConst.QuestSuperHotStew) != QuestStatus.Incomplete)
            return SpellCastResult.CantDoThatRightNow;

        Creature creature = player.FindNearestCreature(MiscConst.NpcAbyssalFlamebringer, 10, false);
        if (creature != null && creature.IsDead())
            return SpellCastResult.SpellCastOk;
        return SpellCastResult.NotHere;
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 1, SpellEffectName.Dummy));
        OnCheckCast.Add(new(CheckRequirement));
    }
}

[Script]
class spell_item_complete_raptor_capture : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RaptorCaptureCredit);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        if (GetHitCreature() != null)
        {
            GetHitCreature().DespawnOrUnsummon();

            //cast spell Raptor Capture Credit
            caster.CastSpell(caster, SpellIds.RaptorCaptureCredit, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_impale_leviroth : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        if (Global.ObjectMgr.GetCreatureTemplate(MiscConst.NpcLeviroth) == null)
            return false;
        return true;
    }

    void HandleDummy(uint effIndex)
    {
        Creature target = GetHitCreature();
        if (target != null)
            if (target.GetEntry() == MiscConst.NpcLeviroth && !target.HealthBelowPct(95))
            {
                target.CastSpell(target, SpellIds.LevirothSelfImpale, true);
                target.ResetPlayerDamageReq();
            }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 23725 - Gift of Life
class spell_item_lifegiving_gem : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GiftOfLife1, SpellIds.GiftOfLife2);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        caster.CastSpell(caster, SpellIds.GiftOfLife1, true);
        caster.CastSpell(caster, SpellIds.GiftOfLife2, true);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_nitro_boosts : SpellScript
{
    public override bool Load()
    {
        if (GetCastItem() == null)
            return false;
        return true;
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NitroBoostsSuccess, SpellIds.NitroBoostsBackfire);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        bool success = true;
        if (!caster.GetMap().IsDungeon())
            success = RandomHelper.randChance(95); // nitro boosts can only fail in flying-enabled locations on 3.3.5
        caster.CastSpell(caster, success ? SpellIds.NitroBoostsSuccess : SpellIds.NitroBoostsBackfire, GetCastItem());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_nitro_boosts_backfire : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NitroBoostsParachute);
    }

    void HandleApply(AuraEffect effect, AuraEffectHandleModes mode)
    {
        lastZ = GetTarget().GetPositionZ();
    }

    void HandlePeriodicDummy(AuraEffect effect)
    {
        PreventDefaultAction();
        float curZ = GetTarget().GetPositionZ();
        if (curZ < lastZ)
        {
            if (RandomHelper.randChance(80)) // we don't have enough sniffs to verify this, guesstimate
                GetTarget().CastSpell(GetTarget(), SpellIds.NitroBoostsParachute, effect);
            GetAura().Remove();
        }
        else
            lastZ = curZ;
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleApply, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        OnEffectPeriodic.Add(new(HandlePeriodicDummy, 1, AuraType.PeriodicTriggerSpell));
    }

    float lastZ = MapConst.InvalidHeight;
}

[Script]
class spell_item_rocket_boots : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RocketBootsProc);
    }

    void HandleDummy(uint effIndex)
    {
        Player caster = GetCaster().ToPlayer();
        caster.GetSpellHistory().ResetCooldown(SpellIds.RocketBootsProc);
        caster.CastSpell(caster, SpellIds.RocketBootsProc, true);
    }

    SpellCastResult CheckCast()
    {
        if (GetCaster().IsInWater())
            return SpellCastResult.OnlyAbovewater;
        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 67489 - Runic Healing Injector
class spell_item_runic_healing_injector : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleHeal(uint effIndex)
    {
        Player caster = GetCaster().ToPlayer();
        if (caster != null && caster.HasSkill(SkillType.Engineering))
            SetHitHeal((int)(GetHitHeal() * 1.25f));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHeal, 0, SpellEffectName.Heal));
    }
}

[Script]
class spell_item_pygmy_oil : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PygmyOilPygmyAura, SpellIds.PygmyOilSmallerAura);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        Aura aura = caster.GetAura(SpellIds.PygmyOilPygmyAura);
        if (aura != null)
            aura.RefreshDuration();
        else
        {
            aura = caster.GetAura(SpellIds.PygmyOilSmallerAura);
            if (aura == null || aura.GetStackAmount() < 5 || !RandomHelper.randChance(50))
                caster.CastSpell(caster, SpellIds.PygmyOilSmallerAura, true);
            else
            {
                aura.Remove();
                caster.CastSpell(caster, SpellIds.PygmyOilPygmyAura, true);
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_unusual_compass : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        caster.SetFacingTo(RandomHelper.FRand(0.0f, 2.0f * MathF.PI));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_chicken_cover : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ChickenNet, SpellIds.CaptureChickenEscape) &&
            Global.ObjectMgr.GetQuestTemplate(MiscConst.QuestChickenParty) != null &&
            Global.ObjectMgr.GetQuestTemplate(MiscConst.QuestFlownTheCoop) != null;
    }

    void HandleDummy(uint effIndex)
    {
        Player caster = GetCaster().ToPlayer();
        Unit target = GetHitUnit();
        if (target != null)
        {
            if (!target.HasAura(SpellIds.ChickenNet) && (caster.GetQuestStatus(MiscConst.QuestChickenParty) == QuestStatus.Incomplete || caster.GetQuestStatus(MiscConst.QuestFlownTheCoop) == QuestStatus.Incomplete))
            {
                caster.CastSpell(caster, SpellIds.CaptureChickenEscape, true);
                target.KillSelf();
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_muisek_vessel : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        Creature target = GetHitCreature();
        if (target != null)
            if (target.IsDead())
                target.DespawnOrUnsummon();
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_item_greatmothers_soulcatcher : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        if (GetHitUnit() != null)
            GetCaster().CastSpell(GetCaster(), SpellIds.ForceCastSummonGnomeSoul);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// Item - 49310: Purified Shard of the Scale
// 69755 - Purified Shard of the Scale - Equip Effect

// Item - 49488: Shiny Shard of the Scale
// 69739 - Shiny Shard of the Scale - Equip Effect
[Script("spell_item_purified_shard_of_the_scale", SpellIds.PurifiedCauterizingHeal, SpellIds.PurifiedSearingFlames)]
[Script("spell_item_shiny_shard_of_the_scale", SpellIds.ShinyCauterizingHeal, SpellIds.ShinySearingFlames)]
class spell_item_shard_of_the_scale(uint HealProcSpellId, uint DamageProcSpellId) : AuraScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(HealProcSpellId, DamageProcSpellId);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetProcTarget();

        if (eventInfo.GetTypeMask() & new ProcFlagsInit(ProcFlags.DealHelpfulSpell))
            caster.CastSpell(target, HealProcSpellId, aurEff);

        if (eventInfo.GetTypeMask() & new ProcFlagsInit(ProcFlags.DealHarmfulSpell))
            caster.CastSpell(target, DamageProcSpellId, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script]
class spell_item_soul_preserver : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SoulPreserverDruid, SpellIds.SoulPreserverPaladin, SpellIds.SoulPreserverPriest, SpellIds.SoulPreserverShaman);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActor();

        switch (caster.GetClass())
        {
            case Class.Druid:
                caster.CastSpell(caster, SpellIds.SoulPreserverDruid, aurEff);
                break;
            case Class.Paladin:
                caster.CastSpell(caster, SpellIds.SoulPreserverPaladin, aurEff);
                break;
            case Class.Priest:
                caster.CastSpell(caster, SpellIds.SoulPreserverPriest, aurEff);
                break;
            case Class.Shaman:
                caster.CastSpell(caster, SpellIds.SoulPreserverShaman, aurEff);
                break;
            default:
                break;
        }
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

// Item - 34678: Shattered Sun Pendant of Acumen
// 45481 - Sunwell Exalted Caster Neck

// Item - 34679: Shattered Sun Pendant of Might
// 45482 - Sunwell Exalted Melee Neck

// Item - 34680: Shattered Sun Pendant of Resolve
// 45483 - Sunwell Exalted Tank Neck

// Item - 34677: Shattered Sun Pendant of Restoration
// 45484 Sunwell Exalted Healer Neck
[Script("spell_item_sunwell_exalted_caster_neck", SpellIds.LightsWrath, SpellIds.ArcaneBolt)]
[Script("spell_item_sunwell_exalted_melee_neck", SpellIds.LightsStrength, SpellIds.ArcaneStrike)]
[Script("spell_item_sunwell_exalted_tank_neck", SpellIds.LightsWard, SpellIds.ArcaneInsight)]
[Script("spell_item_sunwell_exalted_healer_neck", SpellIds.LightsSalvation, SpellIds.ArcaneSurge)]
class spell_item_sunwell_neck(uint Aldors, uint Scryers) : AuraScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(Aldors, Scryers) &&
            CliDB.FactionStorage.ContainsKey(MiscConst.FactionAldor) &&
            CliDB.FactionStorage.ContainsKey(MiscConst.FactionScryers);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.GetActor().GetTypeId() != TypeId.Player)
            return false;
        return true;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Player player = eventInfo.GetActor().ToPlayer();
        Unit target = eventInfo.GetProcTarget();

        // Aggression checks are in the spell system... just cast and forget
        if (player.GetReputationRank(MiscConst.FactionAldor) == ReputationRank.Exalted)
            player.CastSpell(target, Aldors, aurEff);

        if (player.GetReputationRank(MiscConst.FactionScryers) == ReputationRank.Exalted)
            player.CastSpell(target, Scryers, aurEff);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script]
class spell_item_toy_train_set_pulse : SpellScript
{
    void HandleDummy(uint index)
    {
        Player target = GetHitUnit().ToPlayer();
        if (target != null)
        {
            target.HandleEmoteCommand(Emote.OneshotTrain);
            var soundEntry = Global.DB2Mgr.GetTextSoundEmoteFor((uint)TextEmotes.Train, target.GetRace(), target.GetNativeGender(), target.GetClass());
            if (soundEntry != null)
                target.PlayDistanceSound(soundEntry.SoundId);
        }
    }

    void HandleTargets(List<WorldObject> targetList)
    {
        targetList.RemoveAll(obj => obj.GetTypeId() != TypeId.Player);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.ScriptEffect));
        OnObjectAreaTargetSelect.Add(new(HandleTargets, SpellConst.EffectAll, Targets.UnitSrcAreaAlly));
    }
}

[Script]
class spell_item_death_choice : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DeathChoiceNormalStrength, SpellIds.DeathChoiceNormalAgility, SpellIds.DeathChoiceHeroicStrength, SpellIds.DeathChoiceHeroicAgility);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActor();
        float str = caster.GetStat(Stats.Strength);
        float agi = caster.GetStat(Stats.Agility);

        switch (aurEff.GetId())
        {
            case SpellIds.DeathChoiceNormalAura:
            {
                if (str > agi)
                    caster.CastSpell(caster, SpellIds.DeathChoiceNormalStrength, aurEff);
                else
                    caster.CastSpell(caster, SpellIds.DeathChoiceNormalAgility, aurEff);
                break;
            }
            case SpellIds.DeathChoiceHeroicAura:
            {
                if (str > agi)
                    caster.CastSpell(caster, SpellIds.DeathChoiceHeroicStrength, aurEff);
                else
                    caster.CastSpell(caster, SpellIds.DeathChoiceHeroicAgility, aurEff);
                break;
            }
            default:
                break;
        }
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script("spell_item_lightning_capacitor", SpellIds.LightningCapacitorStack, SpellIds.LightningCapacitorTrigger)]
[Script("spell_item_thunder_capacitor", SpellIds.ThunderCapacitorStack, SpellIds.ThunderCapacitorTrigger)]
[Script("spell_item_toc25_normal_caster_trinket", SpellIds.TOC25CasterTrinketNormalStack, SpellIds.TOC25CasterTrinketNormalTrigger)]
[Script("spell_item_toc25_heroic_caster_trinket", SpellIds.TOC25CasterTrinketHeroicStack, SpellIds.TOC25CasterTrinketHeroicTrigger)]
class spell_item_trinket_stack(uint stackSpell, uint triggerSpell) : AuraScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(stackSpell, triggerSpell);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActor();

        caster.CastSpell(caster, stackSpell, aurEff); // cast the stack

        Aura dummy = caster.GetAura(stackSpell); // retrieve aura

        //dont do anything if it's not the right amount of stacks;
        if (dummy == null || dummy.GetStackAmount() < aurEff.GetAmount())
            return;

        // if right amount, remove the aura and cast real trigger
        caster.RemoveAurasDueToSpell(stackSpell);
        Unit target = eventInfo.GetActionTarget();
        if (target != null)
            caster.CastSpell(target, triggerSpell, aurEff);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(stackSpell);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real));
    }
}

[Script] // 57345 - Darkmoon Card: Greatness
class spell_item_darkmoon_card_greatness : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DarkmoonCardStrength, SpellIds.DarkmoonCardAgility, SpellIds.DarkmoonCardIntellect, SpellIds.DarkmoonCardVersatility);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActor();
        float str = caster.GetStat(Stats.Strength);
        float agi = caster.GetStat(Stats.Agility);
        float intl = caster.GetStat(Stats.Intellect);
        float vers = 0.0f; // caster.GetStat(StatVersatility);
        float stat = 0.0f;

        uint spellTrigger = SpellIds.DarkmoonCardStrength;

        if (str > stat)
        {
            spellTrigger = SpellIds.DarkmoonCardStrength;
            stat = str;
        }

        if (agi > stat)
        {
            spellTrigger = SpellIds.DarkmoonCardAgility;
            stat = agi;
        }

        if (intl > stat)
        {
            spellTrigger = SpellIds.DarkmoonCardIntellect;
            stat = intl;
        }

        if (vers > stat)
        {
            spellTrigger = SpellIds.DarkmoonCardVersatility;
            stat = vers;
        }

        caster.CastSpell(caster, spellTrigger, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 27522,40336 - Mana Drain
class spell_item_mana_drain : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ManaDrainEnergize, SpellIds.ManaDrainLeech);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActor();
        Unit target = eventInfo.GetActionTarget();

        if (caster.IsAlive())
            caster.CastSpell(caster, SpellIds.ManaDrainEnergize, aurEff);

        if (target != null && target.IsAlive())
            caster.CastSpell(target, SpellIds.ManaDrainLeech, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 51640 - Taunt Flag Targeting
class spell_item_taunt_flag_targeting : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TauntFlag) &&
            CliDB.BroadcastTextStorage.ContainsKey(MiscConst.EmotePlantsFlag);
    }

    void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(obj => obj.GetTypeId() != TypeId.Player && obj.GetTypeId() != TypeId.Corpse);

        if (targets.Empty())
        {
            FinishCast(SpellCastResult.NoValidTargets);
            return;
        }

        targets.RandomResize(1);
    }

    void HandleDummy(uint effIndex)
    {
        // we *really* want the unit implementation here
        // it sends a packet like seen on sniff
        GetCaster().TextEmote(MiscConst.EmotePlantsFlag, GetHitUnit(), false);

        GetCaster().CastSpell(GetHitUnit(), SpellIds.TauntFlag, true);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.CorpseSrcAreaEnemy));
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 29830 - Mirren's Drinking Hat
class spell_item_mirrens_drinking_hat : SpellScript
{
    void HandleScriptEffect(uint effIndex)
    {
        uint spellId = 0;
        switch (RandomHelper.URand(1, 6))
        {
            case 1:
            case 2:
            case 3:
                spellId = SpellIds.LochModanLager; break;
            case 4:
            case 5:
                spellId = SpellIds.StouthammerLite; break;
            case 6:
                spellId = SpellIds.AeriePeakPaleAle; break;
            default:
                return;
        }

        Unit caster = GetCaster();
        caster.CastSpell(caster, spellId, GetSpell());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 13180 - Gnomish Mind Control Cap
class spell_item_mind_control_cap : SpellScript
{
    public override bool Load()
    {
        if (GetCastItem() == null)
            return false;
        return true;
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GnomishMindControlCap, SpellIds.Dullard);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        Unit target = GetHitUnit();
        if (target != null)
        {
            if (RandomHelper.randChance(MiscConst.RollChanceNoBackfire))
                caster.CastSpell(target, RandomHelper.randChance(MiscConst.RollChanceDullard) ? SpellIds.Dullard : SpellIds.GnomishMindControlCap, GetCastItem());
            else
                target.CastSpell(caster, SpellIds.GnomishMindControlCap, true); // backfire - 5% chance
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 8344 - Universal Remote (Gnomish Universal Remote)
class spell_item_universal_remote : SpellScript
{
    public override bool Load()
    {
        if (GetCastItem() == null)
            return false;
        return true;
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ControlMachine, SpellIds.MobilityMalfunction, SpellIds.TargetLock);
    }

    void HandleDummy(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (target != null)
        {
            uint chance = RandomHelper.URand(0, 99);
            if (chance < 15)
                GetCaster().CastSpell(target, SpellIds.TargetLock, GetCastItem());
            else if (chance < 25)
                GetCaster().CastSpell(target, SpellIds.MobilityMalfunction, GetCastItem());
            else
                GetCaster().CastSpell(target, SpellIds.ControlMachine, GetCastItem());
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// Item - 19950: Zandalarian Hero Charm
// 24658 - Unstable Power

// Item - 19949: Zandalarian Hero Medallion
// 24661 - Restless Strength
[Script("spell_item_unstable_power", SpellIds.UnstablePowerAuraStack)]
[Script("spell_item_restless_strength", SpellIds.RestlessStrengthAuraStack)]
class spell_item_zandalarian_charm_AuraScript(uint spellId) : AuraScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(spellId);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        SpellInfo spellInfo = eventInfo.GetSpellInfo();
        if (spellInfo != null && spellInfo.Id != m_scriptSpellId)
            return true;

        return false;
    }

    void HandleStackDrop(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().RemoveAuraFromStack(spellId);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleStackDrop, 0, AuraType.Dummy));
    }
}

[Script]
class spell_item_artifical_stamina : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    public override bool Load()
    {
        return GetOwner().IsPlayer();
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Item artifact = GetOwner().ToPlayer().GetItemByGuid(GetAura().GetCastItemGUID());
        if (artifact != null)
            amount = (int)(GetEffectInfo(1).BasePoints * artifact.GetTotalPurchasedArtifactPowers() / 100);
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModTotalStatPercentage));
    }
}

[Script]
class spell_item_artifical_damage : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    public override bool Load()
    {
        return GetOwner().IsPlayer();
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Item artifact = GetOwner().ToPlayer().GetItemByGuid(GetAura().GetCastItemGUID());
        if (artifact != null)
            amount = (int)(GetSpellInfo().GetEffect(1).BasePoints * artifact.GetTotalPurchasedArtifactPowers() / 100);
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModDamagePercentDone));
    }
}

[Script] // 28200 - Ascendance
class spell_item_talisman_of_ascendance : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TalismanOfAscendance);
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

[Script] // 29602 - Jom Gabbar
class spell_item_jom_gabbar : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.JomGabbar);
    }

    void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(effect.GetSpellEffectInfo().TriggerSpell);
    }

    public override void Register()
    {
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
    }
}

[Script] // 45040 - Battle Trance
class spell_item_battle_trance : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BattleTrance);
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

[Script] // 90900 - World-Queller Focus
class spell_item_world_queller_focus : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.WorldQuellerFocus);
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

// 118089 - Azure Water Strider
// 127271 - Crimson Water Strider
// 127272 - Orange Water Strider
// 127274 - Jade Water Strider
[Script] // 127278 - Golden Water Strider
class spell_item_water_strider : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(GetSpellInfo().GetEffect(1).TriggerSpell);
    }

    public override void Register()
    {
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.Mounted, AuraEffectHandleModes.Real));
    }
}

// 144671 - Brutal Kinship
[Script] // 145738 - Brutal Kinship
class spell_item_brutal_kinship : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.BrutalKinship1, SpellIds.BrutalKinship2);
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

[Script] // 45051 - Mad Alchemist's Potion (34440)
class spell_item_mad_alchemists_potion : SpellScript
{
    void SecondaryEffect()
    {
        List<uint> availableElixirs =
        [
            // Battle Elixirs
            33720, // Onslaught Elixir (28102)
            54452, // Adept's Elixir (28103)
            33726, // Elixir of Mastery (28104)
            28490, // Elixir of Major Strength (22824)
            28491, // Elixir of Healing Power (22825)
            28493, // Elixir of Major Frost Power (22827)
            54494, // Elixir of Major Agility (22831)
            28501, // Elixir of Major Firepower (22833)
            28503,// Elixir of Major Shadow Power (22835)
            38954, // Fel Strength Elixir (31679)
            // Guardian Elixirs
            39625, // Elixir of Major Fortitude (32062)
            39626, // Earthen Elixir (32063)
            39627, // Elixir of Draenic Wisdom (32067)
            39628, // Elixir of Ironskin (32068)
            28502, // Elixir of Major Defense (22834)
            28514, // Elixir of Empowerment (22848)
            // Other
            28489, // Elixir of Camouflage (22823)
            28496  // Elixir of the Searching Eye (22830)
        ];

        Unit target = GetCaster();

        if (target.GetPowerType() == PowerType.Mana)
            availableElixirs.Add(28509); // Elixir of Major Mageblood (22840)

        uint chosenElixir = availableElixirs.SelectRandom();

        bool useElixir = true;

        SpellGroup chosenSpellGroup = SpellGroup.None;
        if (Global.SpellMgr.IsSpellMemberOfSpellGroup(chosenElixir, SpellGroup.ElixirBattle))
            chosenSpellGroup = SpellGroup.ElixirBattle;
        if (Global.SpellMgr.IsSpellMemberOfSpellGroup(chosenElixir, SpellGroup.ElixirGuardian))
            chosenSpellGroup = SpellGroup.ElixirGuardian;
        // If another spell of the same group is already active the elixir should not be cast
        if (chosenSpellGroup != SpellGroup.None)
        {
            var auraMap = target.GetAppliedAuras();
            foreach (var (_, app) in auraMap)
            {
                uint spellId = app.GetBase().GetId();
                if (Global.SpellMgr.IsSpellMemberOfSpellGroup(spellId, chosenSpellGroup) && spellId != chosenElixir)
                {
                    useElixir = false;
                    break;
                }
            }
        }

        if (useElixir)
            target.CastSpell(target, chosenElixir, GetCastItem());
    }

    public override void Register()
    {
        AfterCast.Add(new(SecondaryEffect));
    }
}

[Script] // 53750 - Crazy Alchemist's Potion (40077)
class spell_item_crazy_alchemists_potion : SpellScript
{
    void SecondaryEffect()
    {
        List<uint> availableElixirs =
        [
            43185, // Runic Healing Potion (33447)
            53750, // Crazy Alchemist's Potion (40077)
            53761, // Powerful Rejuvenation Potion (40087)
            53762, // Indestructible Potion (40093)
            53908, // Potion of Speed (40211)
            53909, // Potion of Wild Magic (40212)
            53910, // Mighty Arcane Protection Potion (40213)
            53911, // Mighty Fire Protection Potion (40214)
            53913, // Mighty Frost Protection Potion (40215)
            53914, // Mighty Nature Protection Potion (40216)
            53915  // Mighty Shadow Protection Potion (40217)
        ];

        Unit target = GetCaster();

        if (!target.IsInCombat())
            availableElixirs.Add(53753); // Potion of Nightmares (40081)
        if (target.GetPowerType() == PowerType.Mana)
            availableElixirs.Add(43186); // Runic Mana Potion(33448)

        uint chosenElixir = availableElixirs.SelectRandom();

        target.CastSpell(target, chosenElixir, GetCastItem());
    }

    public override void Register()
    {
        AfterCast.Add(new(SecondaryEffect));
    }
}

[Script] // 21149 - Egg Nog
class spell_item_eggnog : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EggNogReindeer, SpellIds.EggNogSnowman);
    }

    void HandleScript(uint effIndex)
    {
        if (RandomHelper.randChance(40))
            GetCaster().CastSpell(GetHitUnit(), RandomHelper.randChance(50) ? SpellIds.EggNogReindeer : SpellIds.EggNogSnowman, GetCastItem());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 2, SpellEffectName.Inebriate));
    }
}

// 208051 - Sephuz's Secret
// 234867 - Sephuz's Secret
[Script] // 236763 - Sephuz's Secret
class spell_item_sephuzs_secret : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SephuzsSecretCooldown);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        if (GetUnitOwner().HasAura(SpellIds.SephuzsSecretCooldown))
            return false;

        if (eventInfo.GetHitMask().HasAnyFlag(ProcFlagsHit.Interrupt | ProcFlagsHit.Dispel))
            return true;

        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell == null)
            return false;

        bool isCrowdControl = procSpell.GetSpellInfo().HasAura(AuraType.ModConfuse)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModFear)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModStun)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModPacify)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModRoot)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModSilence)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModPacifySilence)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModRoot2);

        if (!isCrowdControl)
            return false;

        return true;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        PreventDefaultAction();

        GetUnitOwner().CastSpell(GetUnitOwner(), SpellIds.SephuzsSecretCooldown, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        GetUnitOwner().CastSpell(procInfo.GetProcTarget(), aurEff.GetSpellEffectInfo().TriggerSpell, new CastSpellExtraArgs(aurEff).SetTriggeringSpell(procInfo.GetProcSpell()));
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 215266 - Fragile Echoes
class spell_item_amalgams_seventh_spine : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FragileEchoesMonk, SpellIds.FragileEchoesShaman, SpellIds.FragileEchoesPriestDiscipline, SpellIds.FragileEchoesPaladin, SpellIds.FragileEchoesDruid, SpellIds.FragileEchoesPriestHoly, SpellIds.FragileEchoesEvoker);
    }

    void UpdateSpecAura(bool apply)
    {
        Player target = GetUnitOwner().ToPlayer();
        if (target == null)
            return;

        void updateAuraIfInCorrectSpec(ChrSpecialization spec, uint aura)
        {
            if (!apply || target.GetPrimarySpecialization() != spec)
                target.RemoveAurasDueToSpell(aura);
            else if (!target.HasAura(aura))
                target.CastSpell(target, aura, GetEffect(0));
        }

        switch (target.GetClass())
        {
            case Class.Monk:
                updateAuraIfInCorrectSpec(ChrSpecialization.MonkMistweaver, SpellIds.FragileEchoesMonk);
                break;
            case Class.Shaman:
                updateAuraIfInCorrectSpec(ChrSpecialization.ShamanRestoration, SpellIds.FragileEchoesShaman);
                break;
            case Class.Priest:
                updateAuraIfInCorrectSpec(ChrSpecialization.PriestDiscipline, SpellIds.FragileEchoesPriestDiscipline);
                updateAuraIfInCorrectSpec(ChrSpecialization.PriestHoly, SpellIds.FragileEchoesPriestHoly);
                break;
            case Class.Paladin:
                updateAuraIfInCorrectSpec(ChrSpecialization.PaladinHoly, SpellIds.FragileEchoesPaladin);
                break;
            case Class.Druid:
                updateAuraIfInCorrectSpec(ChrSpecialization.DruidRestoration, SpellIds.FragileEchoesDruid);
                break;
            case Class.Evoker:
                updateAuraIfInCorrectSpec(ChrSpecialization.EvokerPreservation, SpellIds.FragileEchoesEvoker);
                break;
            default:
                break;
        }
    }

    void HandleHeartbeat()
    {
        UpdateSpecAura(true);
    }

    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        UpdateSpecAura(false);
    }

    public override void Register()
    {
        OnHeartbeat.Add(new(HandleHeartbeat));
        AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 215267 - Fragile Echo
class spell_item_amalgams_seventh_spine_mana_restore : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FragileEchoEnergize);
    }

    void TriggerManaRestoration(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        Unit caster = GetCaster();
        if (caster == null)
            return;

        AuraEffect trinketEffect = caster.GetAuraEffect(aurEff.GetSpellEffectInfo().TriggerSpell, 0);
        if (trinketEffect != null)
            caster.CastSpell(caster, SpellIds.FragileEchoEnergize, new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, trinketEffect.GetAmount()));
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(TriggerManaRestoration, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 228445 - March of the Legion
class spell_item_set_march_of_the_legion : AuraScript
{
    bool IsDemon(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcTarget() != null && eventInfo.GetProcTarget().GetCreatureType() == CreatureType.Demon;
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(IsDemon, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 234113 - Arrogance (used by item 142171 - Seal of Darkshire Nobility)
class spell_item_seal_of_darkshire_nobility : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1))
            && ValidateSpellInfo(spellInfo.GetEffect(1).TriggerSpell);
    }

    bool CheckCooldownAura(ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcTarget() != null && !eventInfo.GetProcTarget().HasAura(GetEffectInfo(1).TriggerSpell, GetTarget().GetGUID());
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckCooldownAura));
    }
}

[Script] // 247625 - March of the Legion
class spell_item_lightblood_elixir : AuraScript
{
    bool IsDemon(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetProcTarget() != null && eventInfo.GetProcTarget().GetCreatureType() == CreatureType.Demon;
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(IsDemon, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 253287 - Highfather's Timekeeping
class spell_item_highfathers_machination : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.HighfathersTimekeepingHeal);
    }

    bool CheckHealth(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetDamageInfo() != null && GetTarget().HealthBelowPctDamaged(aurEff.GetAmount(), eventInfo.GetDamageInfo().GetDamage());
    }

    void Heal(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        PreventDefaultAction();
        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(GetTarget(), SpellIds.HighfathersTimekeepingHeal, aurEff);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckHealth, 0, AuraType.Dummy));
        OnEffectProc.Add(new(Heal, 0, AuraType.Dummy));
    }
}

[Script] // 253323 - Shadow Strike
class spell_item_seeping_scourgewing : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShadowStrikeAoeCheck);
    }

    void TriggerIsolatedStrikeCheck(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.ShadowStrikeAoeCheck,
            new CastSpellExtraArgs(aurEff).SetTriggeringSpell(eventInfo.GetProcSpell()));
    }

    public override void Register()
    {
        AfterEffectProc.Add(new(TriggerIsolatedStrikeCheck, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 255861 - Shadow Strike
class spell_item_seeping_scourgewing_aoe_check : SpellScript
{
    void TriggerAdditionalDamage()
    {
        if (GetUnitTargetCountForEffect(0) > 1)
            return;

        CastSpellExtraArgs args = new();
        args.TriggerFlags = TriggerCastFlags.FullMask;
        args.OriginalCastId = GetSpell().m_originalCastId;
        if (GetSpell().m_castItemLevel >= 0)
            args.OriginalCastItemLevel = GetSpell().m_castItemLevel;

        GetCaster().CastSpell(GetHitUnit(), SpellIds.IsolatedStrike, args);
    }

    public override void Register()
    {
        AfterHit.Add(new(TriggerAdditionalDamage));
    }
}

[Script] // 295175 - Spiteful Binding
class spell_item_grips_of_forsaken_sanity : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    bool CheckHealth(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetActor().GetHealthPct() >= (float)GetEffectInfo(1).CalcValue();
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckHealth, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 302385 - Resurrect Health
class spell_item_zanjir_scaleguard_greatcloak : AuraScript
{
    bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().HasEffect(SpellEffectName.Resurrect);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script("spell_item_shiver_venom_crossbow", SpellIds.ShiveringBolt)] // 303358 Venomous Bolt
[Script("spell_item_shiver_venom_lance", SpellIds.VenomousLance)] // 303361 Shivering Lance
class spell_item_shiver_venom_weapon_proc(uint additionalProcSpellId) : AuraScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShiverVenom, additionalProcSpellId);
    }

    void HandleAdditionalProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        if (procInfo.GetProcTarget().HasAura(SpellIds.ShiverVenom))
            procInfo.GetActor().CastSpell(procInfo.GetProcTarget(), additionalProcSpellId, new CastSpellExtraArgs(aurEff)
                .AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount())
                .SetTriggeringSpell(procInfo.GetProcSpell()));
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleAdditionalProc, 1, AuraType.Dummy));
    }
}

[Script] // 302774 - Arcane Tempest
class spell_item_phial_of_the_arcane_tempest_damage : SpellScript
{
    void ModifyStacks()
    {
        if (GetUnitTargetCountForEffect(0) != 1 || GetTriggeringSpell() == null)
            return;

        AuraEffect aurEff = GetCaster().GetAuraEffect(GetTriggeringSpell().Id, 0);
        if (aurEff != null)
        {
            aurEff.GetBase().ModStackAmount(1, AuraRemoveMode.None, false);
            aurEff.CalculatePeriodic(GetCaster(), false);
        }
    }

    public override void Register()
    {
        AfterCast.Add(new(ModifyStacks));
    }
}

[Script] // 302769 - Arcane Tempest
class spell_item_phial_of_the_arcane_tempest_periodic : AuraScript
{
    void CalculatePeriod(AuraEffect aurEff, ref bool isPeriodic, ref int period)
    {
        period -= (GetStackAmount() - 1) * 300;
    }

    public override void Register()
    {
        DoEffectCalcPeriodic.Add(new(CalculatePeriod, 0, AuraType.PeriodicTriggerSpell));
    }
}

// 410530 - Mettle
[Script] // 410964 - Mettle
class spell_item_infurious_crafted_gear_mettle : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MettleCooldown);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        if (GetTarget().HasAura(SpellIds.MettleCooldown))
            return false;

        if (eventInfo.GetHitMask().HasAnyFlag(ProcFlagsHit.Interrupt | ProcFlagsHit.Dispel))
            return true;

        Spell procSpell = eventInfo.GetProcSpell();
        if (procSpell == null)
            return false;

        bool isCrowdControl = procSpell.GetSpellInfo().HasAura(AuraType.ModConfuse)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModFear)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModStun)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModPacify)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModRoot)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModSilence)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModPacifySilence)
            || procSpell.GetSpellInfo().HasAura(AuraType.ModRoot2);

        if (!isCrowdControl)
            return false;

        return eventInfo.GetActionTarget().HasAura(aura => aura.GetCastId() == procSpell.m_castId);
    }

    void TriggerCooldown(ProcEventInfo eventInfo)
    {
        GetTarget().CastSpell(GetTarget(), SpellIds.MettleCooldown, true);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        AfterProc.Add(new(TriggerCooldown));
    }
}