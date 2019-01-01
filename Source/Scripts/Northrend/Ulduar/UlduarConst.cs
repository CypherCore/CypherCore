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

namespace Scripts.Northrend.Ulduar
{
    struct BossIds
    {
        public const uint MaxEncounter = 17;

        public const uint Leviathan = 0;
        public const uint Ignis = 1;
        public const uint Razorscale = 2;
        public const uint Xt002 = 3;
        public const uint AssemblyOfIron = 4;
        public const uint Kologarn = 5;
        public const uint Auriaya = 6;
        public const uint Mimiron = 7;
        public const uint Hodir = 8;
        public const uint Thorim = 9;
        public const uint Freya = 10;
        public const uint Brightleaf = 11;
        public const uint Ironbranch = 12;
        public const uint Stonebark = 13;
        public const uint Vezax = 14;
        public const uint YoggSaron = 15;
        public const uint Algalon = 16;
    }

    struct InstanceCreatureIds
    {
        // General
        public const uint Leviathan = 33113;
        public const uint SalvagedDemolisher = 33109;
        public const uint SalvagedSiegeEngine = 33060;
        public const uint SalvagedChopper = 33062;
        public const uint Ignis = 33118;
        public const uint Razorscale = 33186;
        public const uint RazorscaleController = 33233;
        public const uint SteelforgedDeffender = 33236;
        public const uint ExpeditionCommander = 33210;
        public const uint Xt002 = 33293;
        public const uint XtToyPile = 33337;
        public const uint Steelbreaker = 32867;
        public const uint Molgeim = 32927;
        public const uint Brundir = 32857;
        public const uint Kologarn = 32930;
        public const uint FocusedEyebeam = 33632;
        public const uint FocusedEyebeamRight = 33802;
        public const uint LeftArm = 32933;
        public const uint RightArm = 32934;
        public const uint Rubble = 33768;
        public const uint Auriaya = 33515;
        public const uint Mimiron = 33350;
        public const uint Hodir = 32845;
        public const uint Thorim = 32865;
        public const uint Freya = 32906;
        public const uint Vezax = 33271;
        public const uint YoggSaron = 33288;
        public const uint Algalon = 32871;

        //XT002
        public const uint XS013Scrapbot = 33343;

        // Mimiron
        public const uint LeviathanMkII = 33432;
        public const uint Vx001 = 33651;
        public const uint AerialCommandUnit = 33670;
        public const uint AssaultBot = 34057;
        public const uint BombBot = 33836;
        public const uint JunkBot = 33855;
        public const uint EmergencyFireBot = 34147;
        public const uint FrostBomb = 34149;
        public const uint BurstTarget = 34211;
        public const uint Flame = 34363;
        public const uint FlameSpread = 34121;
        public const uint DBTarget = 33576;
        public const uint RocketMimironVisual = 34050;
        public const uint WorldTriggerMimiron = 21252;
        public const uint Computer = 34143;

        // Freya'S Keepers
        public const uint Ironbranch = 32913;
        public const uint Brightleaf = 32915;
        public const uint Stonebark = 32914;

        // Hodir'S Helper Npcs
        public const uint TorGreycloud = 32941;
        public const uint KarGreycloud = 33333;
        public const uint EiviNightfeather = 33325;
        public const uint EllieNightfeather = 32901;
        public const uint SpiritwalkerTara = 33332;
        public const uint SpiritwalkerYona = 32950;
        public const uint ElementalistMahfuun = 33328;
        public const uint ElementalistAvuun = 32900;
        public const uint AmiraBlazeweaver = 33331;
        public const uint VeeshaBlazeweaver = 32946;
        public const uint MissyFlamecuffs = 32893;
        public const uint SissyFlamecuffs = 33327;
        public const uint BattlePriestEliza = 32948;
        public const uint BattlePriestGina = 33330;
        public const uint FieldMedicPenny = 32897;
        public const uint FieldMedicJessi = 33326;

        // Freya'S Trash Npcs
        public const uint CorruptedServitor = 33354;
        public const uint MisguidedNymph = 33355;
        public const uint GuardianLasher = 33430;
        public const uint ForestSwarmer = 33431;
        public const uint MangroveEnt = 33525;
        public const uint IronrootLasher = 33526;
        public const uint NaturesBlade = 33527;
        public const uint GuardianOfLife = 33528;

        // Freya Achievement Trigger
        public const uint FreyaAchieveTrigger = 33406;

        // Yogg-Saron
        public const uint Sara = 33134;
        public const uint GuardianOfYoggSaron = 33136;
        public const uint HodirObservationRing = 33213;
        public const uint FreyaObservationRing = 33241;
        public const uint ThorimObservationRing = 33242;
        public const uint MimironObservationRing = 33244;
        public const uint VoiceOfYoggSaron = 33280;
        public const uint OminousCloud = 33292;
        public const uint FreyaYs = 33410;
        public const uint HodirYs = 33411;
        public const uint MimironYs = 33412;
        public const uint ThorimYs = 33413;
        public const uint SuitOfArmor = 33433;
        public const uint KingLlane = 33437;
        public const uint TheLichKing = 33441;
        public const uint ImmolatedChampion = 33442;
        public const uint Ysera = 33495;
        public const uint Neltharion = 33523;
        public const uint Malygos = 33535;
        public const uint DeathRay = 33881;
        public const uint DeathOrb = 33882;
        public const uint BrainOfYoggSaron = 33890;
        public const uint InfluenceTentacle = 33943;
        public const uint TurnedChampion = 33962;
        public const uint CrusherTentacle = 33966;
        public const uint ConstrictorTentacle = 33983;
        public const uint CorruptorTentacle = 33985;
        public const uint ImmortalGuardian = 33988;
        public const uint SanityWell = 33991;
        public const uint DescendIntoMadness = 34072;
        public const uint MarkedImmortalGuardian = 36064;

        // Algalon The Observer
        public const uint BrannBronzbeardAlg = 34064;
        public const uint Azeroth = 34246;
        public const uint LivingConstellation = 33052;
        public const uint AlgalonStalker = 33086;
        public const uint CollapsingStar = 32955;
        public const uint BlackHole = 32953;
        public const uint WormHole = 34099;
        public const uint AlgalonVoidZoneVisualStalker = 34100;
        public const uint AlgalonStalkerAsteroidTarget01 = 33104;
        public const uint AlgalonStalkerAsteroidTarget02 = 33105;
        public const uint UnleashedDarkMatter = 34097;
    }

    struct InstanceGameObjectIds
    {
        // Leviathan
        public const uint LeviathanDoor = 194905;
        public const uint LeviathanGate = 194630;

        // Razorscale
        public const uint MoleMachine = 194316;
        public const uint RazorHarpoon1 = 194542;
        public const uint RazorHarpoon2 = 194541;
        public const uint RazorHarpoon3 = 194543;
        public const uint RazorHarpoon4 = 194519;
        public const uint RazorBrokenHarpoon = 194565;

        // Xt-002
        public const uint Xt002Door = 194631;

        // Assembly Of Iron
        public const uint IronCouncilDoor = 194554;
        public const uint ArchivumDoor = 194556;

        // Kologarn
        public const uint KologarnChestHero = 195047;
        public const uint KologarnChest = 195046;
        public const uint KologarnBridge = 194232;
        public const uint KologarnDoor = 194553;

        // Hodir
        public const uint HodirEntrance = 194442;
        public const uint HodirDoor = 194634;
        public const uint HodirIceDoor = 194441;
        public const uint HodirRareCacheOfWinter = 194200;
        public const uint HodirRareCacheOfWinterHero = 194201;
        public const uint HodirChestHero = 194308;
        public const uint HodirChest = 194307;

        // Thorim
        public const uint ThorimChestHero = 194315;
        public const uint ThorimChest = 194314;

        // Mimiron
        public const uint MimironTram = 194675;
        public const uint MimironElevator = 194749;
        public const uint MimironButton = 194739;
        public const uint MimironDoor1 = 194774;
        public const uint MimironDoor2 = 194775;
        public const uint MimironDoor3 = 194776;
        public const uint CacheOfInnovation = 194789;
        public const uint CacheOfInnovationFirefighter = 194957;
        public const uint CacheOfInnovationHero = 194956;
        public const uint CacheOfInnovationFirefighterHero = 194958;

        // Vezax
        public const uint VezaxDoor = 194750;

        // Yogg-Saron
        public const uint YoggSaronDoor = 194773;
        public const uint BrainRoomDoor1 = 194635;
        public const uint BrainRoomDoor2 = 194636;
        public const uint BrainRoomDoor3 = 194637;

        // Algalon The Observer
        public const uint CelestialPlanetariumAccess10 = 194628;
        public const uint CelestialPlanetariumAccess25 = 194752;
        public const uint DoodadUlSigildoor01 = 194767;
        public const uint DoodadUlSigildoor02 = 194911;
        public const uint DoodadUlSigildoor03 = 194910;
        public const uint DoodadUlUniversefloor01 = 194715;
        public const uint DoodadUlUniversefloor02 = 194716;
        public const uint DoodadUlUniverseglobe01 = 194148;
        public const uint DoodadUlUlduarTrapdoor03 = 194253;
        public const uint GiftOfTheObserver10 = 194821;
        public const uint GiftOfTheObserver25 = 194822;
    }

    struct InstanceEventIds
    {
        public const int TowerOfStormDestroyed = 21031;
        public const int TowerOfFrostDestroyed = 21032;
        public const int TowerOfFlamesDestroyed = 21033;
        public const int TowerOfLifeDestroyed = 21030;
        public const int ActivateSanityWell = 21432;
        public const int HodirsProtectiveGazeProc = 21437;
    }

    struct LeviathanActions
    {
        public const int TowerOfStormDestroyed = 1;
        public const int TowerOfFrostDestroyed = 2;
        public const int TowerOfFlamesDestroyed = 3;
        public const int TowerOfLifeDestroyed = 4;
        public const int MoveToCenterPosition = 10;
    }

    struct InstanceCriteriaIds
    {
        public const uint ConSpeedAtory = 21597;
        public const uint Lumberjacked = 21686;
        public const uint Disarmed = 21687;
        public const uint WaitsDreamingStormwind25 = 10321;
        public const uint WaitsDreamingChamber25 = 10322;
        public const uint WaitsDreamingIcecrown25 = 10323;
        public const uint WaitsDreamingStormwind10 = 10324;
        public const uint WaitsDreamingChamber10 = 10325;
        public const uint WaitsDreamingIcecrown10 = 10326;
        public const uint DriveMeCrazy10 = 10185;
        public const uint DriveMeCrazy25 = 10296;
        public const uint ThreeLightsInTheDarkness10 = 10410;
        public const uint ThreeLightsInTheDarkness25 = 10414;
        public const uint TwoLightsInTheDarkness10 = 10388;
        public const uint TwoLightsInTheDarkness25 = 10415;
        public const uint OneLightInTheDarkness10 = 10409;
        public const uint OneLightInTheDarkness25 = 10416;
        public const uint AloneInTheDarkness10 = 10412;
        public const uint AloneInTheDarkness25 = 10417;
        public const uint HeraldOfTitans = 10678;

        // Champion Of Ulduar
        public const uint ChampionLeviathan10 = 10042;
        public const uint ChampionIgnis10 = 10342;
        public const uint ChampionRazorscale10 = 10340;
        public const uint ChampionXt002_10 = 10341;
        public const uint ChampionIronCouncil10 = 10598;
        public const uint ChampionKologarn10 = 10348;
        public const uint ChampionAuriaya10 = 10351;
        public const uint ChampionHodir10 = 10439;
        public const uint ChampionThorim10 = 10403;
        public const uint ChampionFreya10 = 10582;
        public const uint ChampionMimiron10 = 10347;
        public const uint ChampionVezax10 = 10349;
        public const uint ChampionYoggSaron10 = 10350;
        // Conqueror Of Ulduar
        public const uint ChampionLeviathan25 = 10352;
        public const uint ChampionIgnis25 = 10355;
        public const uint ChampionRazorscale25 = 10353;
        public const uint ChampionXt002_25 = 10354;
        public const uint ChampionIronCouncil25 = 10599;
        public const uint ChampionKologarn25 = 10357;
        public const uint ChampionAuriaya25 = 10363;
        public const uint ChampionHodir25 = 10719;
        public const uint ChampionThorim25 = 10404;
        public const uint ChampionFreya25 = 10583;
        public const uint ChampionMimiron25 = 10361;
        public const uint ChampionVezax25 = 10362;
        public const uint ChampionYoggSaron25 = 10364;
    }

    struct InstanceData
    {
        // Colossus (Leviathan)
        public const uint Colossus = 20;

        // Razorscale
        public const uint ExpeditionCommander = 21;
        public const uint RazorscaleControl = 22;

        // Xt-002
        public const uint ToyPile0 = 23;
        public const uint ToyPile1 = 24;
        public const uint ToyPile2 = 25;
        public const uint ToyPile3 = 26;

        // Assembly Of Iron
        public const uint Steelbreaker = 27;
        public const uint Molgeim = 28;
        public const uint Brundir = 29;

        // Hodir
        public const uint HodirRareCache = 30;

        // Mimiron
        public const uint LeviathanMKII = 31;
        public const uint VX001 = 32;
        public const uint AerialCommandUnit = 33;
        public const uint Computer = 34;
        public const uint MimironWorldTrigger = 35;
        public const uint MimironElevator = 36;
        public const uint MimironTram = 37;
        public const uint MimironButton = 38;

        // Yogg-Saron
        public const uint VoiceOfYoggSaron = 39;
        public const uint Sara = 40;
        public const uint BrainOfYoggSaron = 41;
        public const uint FreyaYs = 42;
        public const uint HodirYs = 43;
        public const uint ThorimYs = 44;
        public const uint MimironYs = 45;
        public const uint Illusion = 46;
        public const uint DriveMeCrazy = 47;
        public const uint KeepersCount = 48;

        // Algalon The Observer
        public const uint AlgalonSummonState = 49;
        public const uint Sigildoor01 = 50;
        public const uint Sigildoor02 = 51;
        public const uint Sigildoor03 = 52;
        public const uint UniverseFloor01 = 53;
        public const uint UniverseFloor02 = 54;
        public const uint UniverseGlobe = 55;
        public const uint AlgalonTrapdoor = 56;
        public const uint BrannBronzebeardAlg = 57;

        // Misc
        public const uint BrannBronzebeardIntro = 58;
        public const uint LoreKeeperOfNorgannon = 59;
        public const uint Dellorah = 60;
        public const uint BronzebeardRadio = 61;
    }

    struct InstanceWorldStates
    {
        public const uint AlgalonDespawnTimer = 4131;
        public const uint AlgalonTimerEnabled = 4132;
    }

    struct InstanceAchievementData
    {
        // Fl Achievement Boolean
        public const uint DataUnbroken = 29052906; // 2905, 2906 Are Achievement Ids,
        public const uint MaxHeraldArmorItemlevel = 226;
        public const uint MaxHeraldWeaponItemlevel = 232;
    }

    struct InstanceEvents
    {
        public const uint EventDespawnAlgalon = 1;
        public const uint EventUpdateAlgalonTimer = 2;
        public const uint ActionInitAlgalon = 6;
    }

    struct YoggSaronIllusions
    {
        public const uint ChamberIllusion = 0;
        public const uint IcecrownIllusion = 1;
        public const uint StormwindIllusion = 2;
    }
}
