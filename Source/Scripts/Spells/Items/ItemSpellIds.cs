// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Spells;

namespace Scripts.Spells.Items
{
    internal struct ItemSpellIds
    {
        //Aegisofpreservation
        public const uint AegisHeal = 23781;

        //ZezzaksShard
        public const uint EyeOfGrillok = 38495;

        // LowerCityPrayerbook
        public const uint BlessingOfLowerCityDruid = 37878;
        public const uint BlessingOfLowerCityPaladin = 37879;
        public const uint BlessingOfLowerCityPriest = 37880;
        public const uint BlessingOfLowerCityShaman = 37881;

        //Alchemiststone
        public const uint AlchemistStoneExtraHeal = 21399;
        public const uint AlchemistStoneExtraMana = 21400;

        //Angercapacitor
        public const uint MoteOfAnger = 71432;
        public const uint ManifestAngerMainHand = 71433;
        public const uint ManifestAngerOffHand = 71434;

        //Auraofmadness
        public const uint Sociopath = 39511;     // Sociopath: +35 Strength(Paladin; Rogue; Druid; Warrior)
        public const uint Delusional = 40997;    // Delusional: +70 Attack Power(Rogue; Hunter; Paladin; Warrior; Druid)
        public const uint Kleptomania = 40998;   // Kleptomania: +35 Agility(Warrior; Rogue; Paladin; Hunter; Druid)
        public const uint Megalomania = 40999;   // Megalomania: +41 Damage / Healing(Druid; Shaman; Priest; Warlock; Mage; Paladin)
        public const uint Paranoia = 41002;      // Paranoia: +35 Spell / Melee / Ranged Crit Strike Rating(All Classes)
        public const uint Manic = 41005;         // Manic: +35 Haste(Spell; Melee And Ranged) (All Classes)
        public const uint Narcissism = 41009;    // Narcissism: +35 Intellect(Druid; Shaman; Priest; Warlock; Mage; Paladin; Hunter)
        public const uint MartyrComplex = 41011; // Martyr Complex: +35 Stamina(All Classes)
        public const uint Dementia = 41404;      // Dementia: Every 5 Seconds Either Gives You +5/-5%  Damage/Healing. (Druid; Shaman; Priest; Warlock; Mage; Paladin)
        public const uint DementiaPos = 41406;
        public const uint DementiaNeg = 41409;

        // BrittleArmor
        public const uint BrittleArmor = 24575;

        //Blessingofancientkings
        public const uint ProtectionOfAncientKings = 64413;

        //Deadlyprecision
        public const uint DeadlyPrecision = 71564;

        //Deathbringerswill
        public const uint StrengthOfTheTaunka = 71484;     // +600 Strength
        public const uint AgilityOfTheVrykul = 71485;      // +600 Agility
        public const uint PowerOfTheTaunka = 71486;        // +1200 Attack Power
        public const uint AimOfTheIronDwarves = 71491;     // +600 Critical
        public const uint SpeedOfTheVrykul = 71492;        // +600 Haste
        public const uint AgilityOfTheVrykulHero = 71556;  // +700 Agility
        public const uint PowerOfTheTaunkaHero = 71558;    // +1400 Attack Power
        public const uint AimOfTheIronDwarvesHero = 71559; // +700 Critical
        public const uint SpeedOfTheVrykulHero = 71560;    // +700 Haste
        public const uint StrengthOfTheTaunkaHero = 71561; // +700 Strength

        //GoblinBombDispenser
        public const uint SummonGoblinBomb = 13258;
        public const uint MalfunctionExplosion = 13261;

        //GoblinWeatherMachine
        public const uint PersonalizedWeather1 = 46740;
        public const uint PersonalizedWeather2 = 46739;
        public const uint PersonalizedWeather3 = 46738;
        public const uint PersonalizedWeather4 = 46736;

        //Defibrillate
        public const uint GoblinJumperCablesFail = 8338;
        public const uint GoblinJumperCablesXlFail = 23055;

        //Desperatedefense
        public const uint DesperateRage = 33898;

        //Deviatefishspells
        public const uint Sleepy = 8064;
        public const uint Invigorate = 8065;
        public const uint Shrink = 8066;
        public const uint PartyTime = 8067;
        public const uint HealthySpirit = 8068;
        public const uint Rejuvenation = 8070;

        //Discerningeyebeastmisc
        public const uint DiscerningEyeBeast = 59914;

        //Fateruneofunsurpassedvigor
        public const uint UnsurpassedVigor = 25733;

        //Flaskofthenorthspells
        public const uint FlaskOfTheNorthSp = 67016;
        public const uint FlaskOfTheNorthAp = 67017;
        public const uint FlaskOfTheNorthStr = 67018;

        //Frozenshadoweave
        public const uint Shadowmend = 39373;

        //Gnomishdeathray
        public const uint GnomishDeathRaySelf = 13493;
        public const uint GnomishDeathRayTarget = 13279;

        //Heartpierce
        public const uint InvigorationMana = 71881;
        public const uint InvigorationEnergy = 71882;
        public const uint InvigorationRage = 71883;
        public const uint InvigorationRp = 71884;
        public const uint InvigorationRpHero = 71885;
        public const uint InvigorationRageHero = 71886;
        public const uint InvigorationEnergyHero = 71887;
        public const uint InvigorationManaHero = 71888;

        //HourglassSand
        public const uint BroodAfflictionBronze = 23170;

        //Makeawish
        public const uint MrPinchysBlessing = 33053;
        public const uint SummonMightyMrPinchy = 33057;
        public const uint SummonFuriousMrPinchy = 33059;
        public const uint TinyMagicalCrawdad = 33062;
        public const uint MrPinchysGift = 33064;

        //Markofconquest
        public const uint MarkOfConquestEnergize = 39599;

        // MercurialShield
        public const uint MercurialShield = 26464;

        //MingoFortune
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

        //Necrotictouch
        public const uint ItemNecroticTouchProc = 71879;

        //Netomaticspells
        public const uint NetOMaticTriggered1 = 16566;
        public const uint NetOMaticTriggered2 = 13119;
        public const uint NetOMaticTriggered3 = 13099;

        //Noggenfoggerelixirspells
        public const uint NoggenfoggerElixirTriggered1 = 16595;
        public const uint NoggenfoggerElixirTriggered2 = 16593;
        public const uint NoggenfoggerElixirTriggered3 = 16591;

        //Persistentshieldmisc
        public const uint PersistentShieldTriggered = 26470;

        //Pethealing
        public const uint HealthLink = 37382;

        //PowerCircle
        public const uint LimitlessPower = 45044;

        //Savorydeviatedelight
        public const uint FlipOutMale = 8219;
        public const uint FlipOutFemale = 8220;
        public const uint YaaarrrrMale = 8221;
        public const uint YaaarrrrFemale = 8222;

        //Scrollofrecall
        public const uint ScrollOfRecallI = 48129;
        public const uint ScrollOfRecallII = 60320;
        public const uint ScrollOfRecallIII = 60321;
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

        //Shadowsfate
        public const uint SoulFeast = 71203;

        //Shadowmourne
        public const uint ShadowmourneChaosBaneDamage = 71904;
        public const uint ShadowmourneSoulFragment = 71905;
        public const uint ShadowmourneVisualLow = 72521;
        public const uint ShadowmourneVisualHigh = 72523;
        public const uint ShadowmourneChaosBaneBuff = 73422;

        //Sixdemonbagspells
        public const uint Frostbolt = 11538;
        public const uint Polymorph = 14621;
        public const uint SummonFelhoundMinion = 14642;
        public const uint Fireball = 15662;
        public const uint ChainLightning = 21179;
        public const uint EnvelopingWinds = 25189;

        //Swifthandjusticemisc
        public const uint SwiftHandOfJusticeHeal = 59913;

        //Underbellyelixirspells
        public const uint UnderbellyElixirTriggered1 = 59645;
        public const uint UnderbellyElixirTriggered2 = 59831;
        public const uint UnderbellyElixirTriggered3 = 59843;

        //Wormholegeneratorpandariaspell
        public const uint Wormholepandariaisleofreckoning = 126756;
        public const uint Wormholepandariakunlaiunderwater = 126757;
        public const uint Wormholepandariasravess = 126758;
        public const uint Wormholepandariarikkitunvillage = 126759;
        public const uint Wormholepandariazanvesstree = 126760;
        public const uint Wormholepandariaanglerswharf = 126761;
        public const uint Wormholepandariacranestatue = 126762;
        public const uint Wormholepandariaemperorsomen = 126763;
        public const uint Wormholepandariawhitepetallake = 126764;

        //Airriflespells
        public const uint AirRifleHoldVisual = 65582;
        public const uint AirRifleShoot = 67532;
        public const uint AirRifleShootSelf = 65577;

        //Genericdata
        public const uint ArcaniteDragonling = 19804;
        public const uint BattleChicken = 13166;
        public const uint MechanicalDragonling = 4073;
        public const uint MithrilMechanicalDragonling = 12749;

        //Vanquishedclutchesspells
        public const uint Crusher = 64982;
        public const uint Constrictor = 64983;
        public const uint Corruptor = 64984;

        //Magiceater
        public const uint WildMagic = 58891;
        public const uint WellFed1 = 57288;
        public const uint WellFed2 = 57139;
        public const uint WellFed3 = 57111;
        public const uint WellFed4 = 57286;
        public const uint WellFed5 = 57291;

        //Purifyhelboarmeat
        public const uint SummonPurifiedHelboarMeat = 29277;
        public const uint SummonToxicHelboarMeat = 29278;

        //Nighinvulnerability
        public const uint NighInvulnerability = 30456;
        public const uint CompleteVulnerability = 30457;

        //Poultryzer
        public const uint PoultryizerSuccess = 30501;
        public const uint PoultryizerBackfire = 30504;

        //Socretharsstone
        public const uint SocretharToSeat = 35743;
        public const uint SocretharFromSeat = 35744;

        //Demonbroiledsurprise
        public const uint CreateDemonBroiledSurprise = 43753;

        //Completeraptorcapture
        public const uint RaptorCaptureCredit = 42337;

        //Impaleleviroth
        public const uint LevirothSelfImpale = 49882;

        //LifegivingGem
        public const uint GiftOfLife1 = 23782;
        public const uint GiftOfLife2 = 23783;

        //Nitroboots
        public const uint NitroBoostsSuccess = 54861;
        public const uint NitroBoostsBackfire = 46014;
        public const uint NitroBoostsParachute = 54649;

        //Teachlanguage
        public const uint LearnGnomishBinary = 50242;
        public const uint LearnGoblinBinary = 50246;

        //Rocketboots
        public const uint RocketBootsProc = 30452;

        //Pygmyoil
        public const uint PygmyOilPygmyAura = 53806;
        public const uint PygmyOilSmallerAura = 53805;

        //Chickencover
        public const uint ChickenNet = 51959;
        public const uint CaptureChickenEscape = 51037;

        //Greatmotherssoulcather
        public const uint ForceCastSummonGnomeSoul = 46486;

        //Shardofthescale
        public const uint PurifiedCauterizingHeal = 69733;
        public const uint PurifiedSearingFlames = 69729;
        public const uint ShinyCauterizingHeal = 69734;
        public const uint ShinySearingFlames = 69730;

        //Soulpreserver
        public const uint SoulPreserverDruid = 60512;
        public const uint SoulPreserverPaladin = 60513;
        public const uint SoulPreserverPriest = 60514;
        public const uint SoulPreserverShaman = 60515;

        //ExaltedSunwellNeck
        public const uint LightsWrath = 45479; // Light'S Wrath If Exalted By Aldor
        public const uint ArcaneBolt = 45429;  // Arcane Bolt If Exalted By Scryers

        public const uint LightsStrength = 45480; // Light'S Strength If Exalted By Aldor
        public const uint ArcaneStrike = 45428;   // Arcane Strike If Exalted By Scryers

        public const uint LightsWard = 45432;    // Light'S Ward If Exalted By Aldor
        public const uint ArcaneInsight = 45431; // Arcane Insight If Exalted By Scryers

        public const uint LightsSalvation = 45478; // Light'S Salvation If Exalted By Aldor
        public const uint ArcaneSurge = 45430;     // Arcane Surge If Exalted By Scryers

        //Deathchoicespells
        public const uint DeathChoiceNormalAura = 67702;
        public const uint DeathChoiceNormalAgility = 67703;
        public const uint DeathChoiceNormalStrength = 67708;
        public const uint DeathChoiceHeroicAura = 67771;
        public const uint DeathChoiceHeroicAgility = 67772;
        public const uint DeathChoiceHeroicStrength = 67773;

        //Trinketstackspells
        public const uint LightningCapacitorAura = 37657; // Lightning Capacitor
        public const uint LightningCapacitorStack = 37658;
        public const uint LightningCapacitorTrigger = 37661;
        public const uint ThunderCapacitorAura = 54841; // Thunder Capacitor
        public const uint ThunderCapacitorStack = 54842;
        public const uint ThunderCapacitorTrigger = 54843;
        public const uint Toc25CasterTrinketNormalAura = 67712; // Item - Coliseum 25 Normal Caster Trinket
        public const uint Toc25CasterTrinketNormalStack = 67713;
        public const uint Toc25CasterTrinketNormalTrigger = 67714;
        public const uint Toc25CasterTrinketHeroicAura = 67758; // Item - Coliseum 25 Heroic Caster Trinket
        public const uint Toc25CasterTrinketHeroicStack = 67759;
        public const uint Toc25CasterTrinketHeroicTrigger = 67760;

        //Darkmooncardspells
        public const uint DarkmoonCardStrenght = 60229;
        public const uint DarkmoonCardAgility = 60233;
        public const uint DarkmoonCardIntellect = 60234;
        public const uint DarkmoonCardVersatility = 60235;

        //Manadrainspells
        public const uint ManaDrainEnergize = 29471;
        public const uint ManaDrainLeech = 27526;

        //Tauntflag
        public const uint TauntFlag = 51657;

        //MirrensDrinkingHat
        public const uint LochModanLager = 29827;
        public const uint StouthammerLite = 29828;
        public const uint AeriePeakPaleAle = 29829;

        //MindControlCap
        public const uint GnomishMindControlCap = 13181;
        public const uint Dullard = 67809;

        //UniversalRemote
        public const uint ControlMachine = 8345;
        public const uint MobilityMalfunction = 8346;
        public const uint TargetLock = 8347;

        //Zandalariancharms
        public const uint UnstablePowerAuraStack = 24659;
        public const uint RestlessStrengthAuraStack = 24662;

        // AuraprocRemovespells        
        public const uint TalismanOfAscendance = 28200;
        public const uint JomGabbar = 29602;
        public const uint BattleTrance = 45040;
        public const uint WorldQuellerFocus = 90900;
        public const uint BrutalKinship1 = 144671;
        public const uint BrutalKinship2 = 145738;

        // Eggnog
        public const uint EggNogReindeer = 21936;
        public const uint EggNogSnowman = 21980;
    }

    // 23074 Arcanite Dragonling
    // 23133 Gnomish Battle Chicken
    // 23076 Mechanical Dragonling
    // 23075 Mithril Mechanical Dragonling

    // 37877 - Blessing of Faith

    // Item - 13503: Alchemist's Stone
    // Item - 35748: Guardian's Alchemist Stone
    // Item - 35749: Sorcerer's Alchemist Stone
    // Item - 35750: Redeemer's Alchemist Stone
    // Item - 35751: Assassin's Alchemist Stone
    // Item - 44322: Mercurial Alchemist Stone
    // Item - 44323: Indestructible Alchemist's Stone
    // Item - 44324: Mighty Alchemist's Stone

    // Item - 50351: Tiny Abomination in a Jar
    // 71406 - Anger Capacitor
    // Item - 50706: Tiny Abomination in a Jar (Heroic)
    // 71545 - Anger Capacitor

    // Item - 31859: Darkmoon Card: Madness

    // Item - 50362: Deathbringer's Will
    // 71519 - Item - Icecrown 25 Normal Melee Trinket

    // Item - 50363: Deathbringer's Will
    // 71562 - Item - Icecrown 25 Heroic Melee Trinket

    // 8342  - Defibrillate (Goblin Jumper Cables) have 33% chance on success
    // 22999 - Defibrillate (Goblin Jumper Cables XL) have 50% chance on success
    // 54732 - Defibrillate (Gnomish Army Knife) have 67% chance on success

    // http://www.wowhead.com/Item=6522 Deviate Fish

    // http://www.wowhead.com/Item=47499 Flask of the North

    // 39372 - Frozen Shadoweave

    // http://www.wowhead.com/Item=10645 Gnomish Death Ray

    // Item - 49982: Heartpierce
    // 71880 - Item - Icecrown 25 Normal Dagger Proc

    // Item - 50641: Heartpierce (Heroic)
    // 71892 - Item - Icecrown 25 Heroic Dagger Proc

    // http://www.wowhead.com/Item=27388 Mr. Pinchy

    // Item - 27920: Mark of Conquest
    // Item - 27921: Mark of Conquest

    // http://www.wowhead.com/Item=32686 Mingo's Fortune Giblets

    // http://www.wowhead.com/Item=10720 Gnomish Net-o-Matic Projector

    // http://www.wowhead.com/Item=8529 Noggenfogger Elixir

    // 37381 - Pet Healing
    // Hunter T5 2P Bonus

    // http://www.wowhead.com/Item=6657 Savory Deviate Delight

    // 48129 - Scroll of Recall
    // 60320 - Scroll of Recall II

    // http://www.wowhead.com/Item=7734 Six Demon Bag

    // http://www.wowhead.com/Item=44012 Underbelly Elixir

    // Item - 49310: Purified Shard of the Scale
    // 69755 - Purified Shard of the Scale - Equip Effect

    // Item - 49488: Shiny Shard of the Scale
    // 69739 - Shiny Shard of the Scale - Equip Effect

    // Item - 19950: Zandalarian Hero Charm
    // 24658 - Unstable Power
    // Item - 19949: Zandalarian Hero Medallion
    // 24661 - Restless Strength

    // 118089 - Azure Water Strider
    // 127271 - Crimson Water Strider
    // 127272 - Orange Water Strider
    // 127274 - Jade Water Strider

    // 144671 - Brutal Kinship
}