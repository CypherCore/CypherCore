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

namespace Scripts.Northrend.IcecrownCitadel
{
    struct IccConst
    {
        public const uint WeeklyNPCs = 9;
    }

    struct Bosses
    {
        public const uint LordMarrowgar = 0;
        public const uint LadyDeathwhisper = 1;
        public const uint GunshipBattle = 2;
        public const uint DeathbringerSaurfang = 3;
        public const uint Festergut = 4;
        public const uint Rotface = 5;
        public const uint ProfessorPutricide = 6;
        public const uint BloodPrinceCouncil = 7;
        public const uint BloodQueenLanaThel = 8;
        public const uint SisterSvalna = 9;
        public const uint ValithriaDreamwalker = 10;
        public const uint Sindragosa = 11;
        public const uint TheLichKing = 12;

        public const uint MaxEncounters = 13;
    }

    struct Texts
    {
        // Highlord Tirion Fordring (At Light'S Hammer)
        public const uint SayTirionIntro1 = 0;
        public const uint SayTirionIntro2 = 1;
        public const uint SayTirionIntro3 = 2;
        public const uint SayTirionIntro4 = 3;
        public const uint SayTirionIntroH5 = 4;
        public const uint SayTirionIntroA5 = 5;

        // The Lich King (At Light'S Hammer)
        public const uint SayLkIntro1 = 0;
        public const uint SayLkIntro2 = 1;
        public const uint SayLkIntro3 = 2;
        public const uint SayLkIntro4 = 3;
        public const uint SayLkIntro5 = 4;

        // Highlord Bolvar Fordragon (At Light'S Hammer)
        public const uint SayBolvarIntro1 = 0;

        // High Overlord Saurfang (At Light'S Hammer)
        public const uint SaySaurfangIntro1 = 15;
        public const uint SaySaurfangIntro2 = 16;
        public const uint SaySaurfangIntro3 = 17;
        public const uint SaySaurfangIntro4 = 18;

        // Muradin Bronzebeard (At Light'S Hammer)
        public const uint SayMuradinIntro1 = 13;
        public const uint SayMuradinIntro2 = 14;
        public const uint SayMuradinIntro3 = 15;

        // Deathbound Ward
        public const uint SayTrapActivate = 0;

        // Rotting Frost Giant
        public const uint EmoteDeathPlagueWarning = 0;

        // Sister Svalna
        public const uint SaySvalnaKillCaptain = 1; // Happens When She Kills A Captain
        public const uint SaySvalnaKill = 4;
        public const uint SaySvalnaCaptainDeath = 5; // Happens When A Captain Resurrected By Her Dies
        public const uint SaySvalnaDeath = 6;
        public const uint EmoteSvalnaImpale = 7;
        public const uint EmoteSvalnaBrokenShield = 8;

        public const uint SayCrokIntro1 = 0; // Ready Your Arms; My Argent Brothers. The Vrykul Will Protect The Frost Queen With Their Lives.
        public const uint SayArnathIntro2 = 5; // Even Dying Here Beats Spending Another Day Collecting Reagents For That Madman; Finklestein.
        public const uint SayCrokIntro3 = 1; // Enough Idle Banter! Our Champions Have Arrived - Support Them As We Push Our Way Through The Hall!
        public const uint SaySvalnaEventStart = 0; // You May Have Once Fought Beside Me; Crok; But Now You Are Nothing More Than A Traitor. Come; Your Second Death Approaches!
        public const uint SayCrokCombatWp0 = 2; // Draw Them Back To Us; And We'Ll Assist You.
        public const uint SayCrokCombatWp1 = 3; // Quickly; Push On!
        public const uint SayCrokFinalWp = 4; // Her Reinforcements Will Arrive Shortly; We Must Bring Her Down Quickly!
        public const uint SaySvalnaResurrectCaptains = 2; // Foolish Crok. You Brought My Reinforcements With You. Arise; Argent Champions; And Serve The Lich King In Death!
        public const uint SayCrokCombatSvalna = 5; // I'Ll Draw Her Attacks. Return Our Brothers To Their Graves; Then Help Me Bring Her Down!
        public const uint SaySvalnaAggro = 3; // Come; Scourgebane. I'Ll Show The Master Which Of Us Is Truly Worthy Of The Title Of "Champion"!
        public const uint SayCaptainDeath = 0;
        public const uint SayCaptainResurrected = 1;
        public const uint SayCaptainKill = 2;
        public const uint SayCaptainSecondDeath = 3;
        public const uint SayCaptainSurviveTalk = 4;
        public const uint SayCrokWeakeningGauntlet = 6;
        public const uint SayCrokWeakeningSvalna = 7;
        public const uint SayCrokDeath = 8;
    }

    struct InstanceSpells
    {
        // Rotting Frost Giant
        public const uint DeathPlague = 72879;
        public const uint DeathPlagueAura = 72865;
        public const uint RecentlyInfected = 72884;
        public const uint DeathPlagueKill = 72867;
        public const uint Stomp = 64652;
        public const uint ArcticBreath = 72848;

        // Frost Freeze Trap
        public const uint ColdflameJets = 70460;

        // Alchemist Adrianna
        public const uint HarvestBlightSpecimen = 72155;
        public const uint HarvestBlightSpecimen25 = 72162;

        // Crok Scourgebane
        public const uint IceboundArmor = 70714;
        public const uint ScourgeStrike = 71488;
        public const uint DeathStrike = 71489;

        // Sister Svalna
        public const uint CaressOfDeath = 70078;
        public const uint ImpalingSpearKill = 70196;
        public const uint ReviveChampion = 70053;
        public const uint Undeath = 70089;
        public const uint ImpalingSpear = 71443;
        public const uint AetherShield = 71463;
        public const uint HurlSpear = 71466;

        // Captain Arnath
        public const uint DominateMind = 14515;
        public const uint FlashHealNormal = 71595;
        public const uint PowerWordShieldNormal = 71548;
        public const uint SmiteNormal = 71546;
        public const uint FlashHealUndead = 71782;
        public const uint PowerWordShieldUndead = 71780;
        public const uint SmiteUndead = 71778;
        public static uint SpellFlashHeal(bool isUndead) { return isUndead ? FlashHealUndead : FlashHealNormal; }
        public static uint SpellPowerWordShield(bool isUndead) { return isUndead ? PowerWordShieldUndead : PowerWordShieldNormal; }
        public static uint SpellSmite(bool isUndead) { return isUndead ? SmiteUndead : SmiteNormal; }

        // Captain Brandon
        public const uint CrusaderStrike = 71549;
        public const uint DivineShield = 71550;
        public const uint JudgementOfCommand = 71551;
        public const uint HammerOfBetrayal = 71784;

        // Captain Grondel
        public const uint Charge = 71553;
        public const uint MortalStrike = 71552;
        public const uint SunderArmor = 71554;
        public const uint Conflagration = 71785;

        // Captain Rupert
        public const uint FelIronBombNormal = 71592;
        public const uint MachineGunNormal = 71594;
        public const uint RocketLaunchNormal = 71590;
        public const uint FelIronBombUndead = 71787;
        public const uint MachineGunUndead = 71788;
        public const uint RocketLaunchUndead = 71786;
        public static uint SpellFelIronBomb(bool isUndead) { return isUndead ? FelIronBombUndead : FelIronBombNormal; }
        public static uint SpellMachineGun(bool isUndead) { return isUndead ? MachineGunUndead : MachineGunNormal; }
        public static uint SpellRocketLaunch(bool isUndead) { return isUndead ? RocketLaunchUndead : RocketLaunchNormal; }

        // Invisible Stalker (Float; Uninteractible; Largeaoi)
        public const uint SoulMissile = 72585;

        public const uint Berserk = 26662;
        public const uint Berserk2 = 47008;

        // Deathbound Ward
        public const uint StoneForm = 70733;

        // Residue Rendezvous
        public const uint OrangeBlightResidue = 72144;
        public const uint GreenBlightResidue = 72145;

        // The Lich King
        public const uint ArthasTeleporterCeremony = 72915;
        public const uint FrostmourneTeleportVisual = 73078;

        // Shadowmourne questline
        public const uint UnsatedCraving = 71168;
        public const uint ShadowsFate = 71169;
    }

    struct EventTypes
    {
        // Highlord Tirion Fordring (At Light'S Hammer)
        // The Lich King (At Light'S Hammer)
        // Highlord Bolvar Fordragon (At Light'S Hammer)
        // High Overlord Saurfang (At Light'S Hammer)
        // Muradin Bronzebeard (At Light'S Hammer)
        public const uint TirionIntro2 = 1;
        public const uint TirionIntro3 = 2;
        public const uint TirionIntro4 = 3;
        public const uint TirionIntro5 = 4;
        public const uint LkIntro1 = 5;
        public const uint TirionIntro6 = 6;
        public const uint LkIntro2 = 7;
        public const uint LkIntro3 = 8;
        public const uint LkIntro4 = 9;
        public const uint BolvarIntro1 = 10;
        public const uint LkIntro5 = 11;
        public const uint SaurfangIntro1 = 12;
        public const uint TirionIntroH7 = 13;
        public const uint SaurfangIntro2 = 14;
        public const uint SaurfangIntro3 = 15;
        public const uint SaurfangIntro4 = 16;
        public const uint SaurfangRun = 17;
        public const uint MuradinIntro1 = 18;
        public const uint MuradinIntro2 = 19;
        public const uint MuradinIntro3 = 20;
        public const uint TirionIntroA7 = 21;
        public const uint MuradinIntro4 = 22;
        public const uint MuradinIntro5 = 23;
        public const uint MuradinRun = 24;

        // Rotting Frost Giant
        public const uint DeathPlague = 25;
        public const uint Stomp = 26;
        public const uint ArcticBreath = 27;

        // Frost Freeze Trap
        public const uint ActivateTrap = 28;

        // Crok Scourgebane
        public const uint ScourgeStrike = 29;
        public const uint DeathStrike = 30;
        public const uint HealthCheck = 31;
        public const uint CrokIntro3 = 32;
        public const uint StartPathing = 33;

        // Sister Svalna
        public const uint ArnathIntro2 = 34;
        public const uint SvalnaStart = 35;
        public const uint SvalnaResurrect = 36;
        public const uint SvalnaCombat = 37;
        public const uint ImpalingSpear = 38;
        public const uint AetherShield = 39;

        // Captain Arnath
        public const uint ArnathFlashHeal = 40;
        public const uint ArnathPwShield = 41;
        public const uint ArnathSmite = 42;
        public const uint ArnathDominateMind = 43;

        // Captain Brandon
        public const uint BrandonCrusaderStrike = 44;
        public const uint BrandonDivineShield = 45;
        public const uint BrandonJudgementOfCommand = 46;
        public const uint BrandonHammerOfBetrayal = 47;

        // Captain Grondel
        public const uint GrondelChargeCheck = 48;
        public const uint GrondelMortalStrike = 49;
        public const uint GrondelSunderArmor = 50;
        public const uint GrondelConflagration = 51;

        // Captain Rupert
        public const uint RupertFelIronBomb = 52;
        public const uint RupertMachineGun = 53;
        public const uint RupertRocketLaunch = 54;

        // Invisible Stalker (Float; Uninteractible; Largeaoi)
        public const uint SoulMissile = 55;
    }

    struct DataTypes
    {
        // Additional Data
        public const uint SaurfangEventNpc = 13;
        public const uint BonedAchievement = 14;
        public const uint OozeDanceAchievement = 15;
        public const uint PutricideTable = 16;
        public const uint NauseaAchievement = 17;
        public const uint OrbWhispererAchievement = 18;
        public const uint PrinceKelesethGuid = 19;
        public const uint PrinceTaldaramGuid = 20;
        public const uint PrinceValanarGuid = 21;
        public const uint BloodPrincesControl = 22;
        public const uint SindragosaFrostwyrms = 23;
        public const uint Spinestalker = 24;
        public const uint Rimefang = 25;
        public const uint ColdflameJets = 26;
        public const uint TeamInInstance = 27;
        public const uint BloodQuickeningState = 28;
        public const uint HeroicAttempts = 29;
        public const uint CrokScourgebane = 30;
        public const uint CaptainArnath = 31;
        public const uint CaptainBrandon = 32;
        public const uint CaptainGrondel = 33;
        public const uint CaptainRupert = 34;
        public const uint ValithriaTrigger = 35;
        public const uint ValithriaLichKing = 36;
        public const uint HighlordTirionFordring = 37;
        public const uint ArthasPlatform = 38;
        public const uint TerenasMenethil = 39;
        public const uint EnemyGunship = 40;
        public const uint UpperSpireTeleAct = 41;
    }

    struct WorldStates
    {
        public const uint ShowTimer = 4903;
        public const uint ExecutionTime = 4904;
        public const uint ShowAttempts = 4940;
        public const uint AttemptsRemaining = 4941;
        public const uint AttemptsMax = 4942;
    }

    struct CreatureIds
    {
        // At Light'S Hammer
        public const uint HighlordTirionFordringLh = 37119;
        public const uint TheLichKingLh = 37181;
        public const uint HighlordBolvarFordragonLh = 37183;
        public const uint KorKronGeneral = 37189;
        public const uint AllianceCommander = 37190;
        public const uint Tortunok = 37992;    // Druid Armor H
        public const uint AlanaMoonstrike = 37999;    // Druid Armor A
        public const uint GerardoTheSuave = 37993;    // Hunter Armor H
        public const uint TalanMoonstrike = 37998;    // Hunter Armor A
        public const uint UvlusBanefire = 38284;    // Mage Armor H
        public const uint MalfusGrimfrost = 38283;    // Mage Armor A
        public const uint IkfirusTheVile = 37991;    // Rogue Armor H
        public const uint Yili = 37997;    // Rogue Armor A
        public const uint VolGuk = 38841;    // Shaman Armor H
        public const uint Jedebia = 38840;    // Shaman Armor A
        public const uint HaraggTheUnseen = 38181;    // Warlock Armor H
        public const uint NibyTheAlmighty = 38182;    // Warlock Armor N
        public const uint GarroshHellscream = 39372;
        public const uint KingVarianWrynn = 39371;
        public const uint DeathboundWard = 37007;
        public const uint LadyJainaProudmooreQuest = 38606;
        public const uint MuradinBronzaBeardQuest = 38607;
        public const uint UtherTheLightBringerQuest = 38608;
        public const uint LadySylvanasWindrunnerQuest = 38609;

        // Weekly Quests
        public const uint InfiltratorMinchar = 38471;
        public const uint KorKronLieutenant = 38491;
        public const uint SkybreakerLieutenant = 38492;
        public const uint RottingFrostGiant10 = 38490;
        public const uint RottingFrostGiant25 = 38494;
        public const uint AlchemistAdrianna = 38501;
        public const uint AlrinTheAgile = 38551;
        public const uint InfiltratorMincharBq = 38558;
        public const uint MincharBeamStalker = 38557;
        public const uint ValithriaDreamwalkerQuest = 38589;

        // Lord Marrowgar
        public const uint LordMarrowgar = 36612;
        public const uint Coldflame = 36672;
        public const uint BoneSpike = 36619;

        // Lady Deathwhisper
        public const uint LadyDeathwhisper = 36855;
        public const uint CultFanatic = 37890;
        public const uint DeformedFanatic = 38135;
        public const uint ReanimatedFanatic = 38009;
        public const uint CultAdherent = 37949;
        public const uint EmpoweredAdherent = 38136;
        public const uint ReanimatedAdherent = 38010;
        public const uint VengefulShade = 38222;

        // Icecrown Gunship Battle
        public const uint MartyrStalkerIGBSaurfang = 38569;
        public const uint AllianceGunshipCannon = 36838;
        public const uint HordeGunshipCannon = 36839;
        public const uint SkybreakerDeckhand = 36970;
        public const uint OrgrimsHammerCrew = 36971;
        public const uint IGBHighOverlordSaurfang = 36939;
        public const uint IGBMuradinBrozebeard = 36948;
        public const uint TheSkybreaker = 37540;
        public const uint OrgrimsHammer = 37215;
        public const uint GunshipHull = 37547;
        public const uint TeleportPortal = 37227;
        public const uint TeleportExit = 37488;
        public const uint SkybreakerSorcerer = 37116;
        public const uint SkybreakerRifleman = 36969;
        public const uint SkybreakerMortarSoldier = 36978;
        public const uint SkybreakerMarine = 36950;
        public const uint SkybreakerSergeant = 36961;
        public const uint KorKronBattleMage = 37117;
        public const uint KorKronAxeThrower = 36968;
        public const uint KorKronRocketeer = 36982;
        public const uint KorKronReaver = 36957;
        public const uint KorKronSergeant = 36960;
        public const uint ZafodBoombox = 37184;
        public const uint HighCaptainJustinBartlett = 37182;
        public const uint SkyReaverKormBlackscar = 37833;

        // Deathbringer Saurfang
        public const uint DeathbringerSaurfang = 37813;
        public const uint BloodBeast = 38508;
        public const uint SeJainaProudmoore = 37188;    // Se Means Saurfang Event
        public const uint SeMuradinBronzebeard = 37200;
        public const uint SeKingVarianWrynn = 37879;
        public const uint SeHighOverlordSaurfang = 37187;
        public const uint SeKorKronReaver = 37920;
        public const uint SeSkybreakerMarine = 37830;
        public const uint FrostFreezeTrap = 37744;

        // Festergut
        public const uint Festergut = 36626;
        public const uint GasDummy = 36659;
        public const uint MalleableOozeStalker = 38556;

        // Rotface
        public const uint Rotface = 36627;
        public const uint OozeSprayStalker = 37986;
        public const uint PuddleStalker = 37013;
        public const uint UnstableExplosionStalker = 38107;
        public const uint VileGasStalker = 38548;

        // Professor Putricide
        public const uint ProfessorPutricide = 36678;
        public const uint AbominationWingMadScientistStalker = 37824;
        public const uint GrowingOozePuddle = 37690;
        public const uint GasCloud = 37562;
        public const uint VolatileOoze = 37697;
        public const uint ChokingGasBomb = 38159;
        public const uint TearGasTargetStalker = 38317;
        public const uint MutatedAbomination10 = 37672;
        public const uint MutatedAbomination25 = 38285;

        // Blood Prince Council
        public const uint PrinceKeleseth = 37972;
        public const uint PrinceTaldaram = 37973;
        public const uint PrinceValanar = 37970;
        public const uint BloodOrbController = 38008;
        public const uint FloatingTrigger = 30298;
        public const uint DarkNucleus = 38369;
        public const uint BallOfFlame = 38332;
        public const uint BallOfInfernoFlame = 38451;
        public const uint KineticBombTarget = 38458;
        public const uint KineticBomb = 38454;
        public const uint ShockVortex = 38422;

        // Blood-Queen Lana'Thel
        public const uint BloodQueenLanaThel = 37955;

        // Frostwing Halls Gauntlet Event
        public const uint CrokScourgebane = 37129;
        public const uint CaptainArnath = 37122;
        public const uint CaptainBrandon = 37123;
        public const uint CaptainGrondel = 37124;
        public const uint CaptainRupert = 37125;
        public const uint CaptainArnathUndead = 37491;
        public const uint CaptainBrandonUndead = 37493;
        public const uint CaptainGrondelUndead = 37494;
        public const uint CaptainRupertUndead = 37495;
        public const uint YmirjarBattleMaiden = 37132;
        public const uint YmirjarDeathbringer = 38125;
        public const uint YmirjarFrostbinder = 37127;
        public const uint YmirjarHuntress = 37134;
        public const uint YmirjarWarlord = 37133;
        public const uint SisterSvalna = 37126;
        public const uint ImpalingSpear = 38248;

        // Valithria Dreamwalker
        public const uint ValithriaDreamwalker = 36789;
        public const uint GreenDragonCombatTrigger = 38752;
        public const uint RisenArchmage = 37868;
        public const uint BlazingSkeleton = 36791;
        public const uint Suppresser = 37863;
        public const uint BlisteringZombie = 37934;
        public const uint GluttonousAbomination = 37886;
        public const uint ManaVoid = 38068;
        public const uint ColumnOfFrost = 37918;
        public const uint RotWorm = 37907;
        public const uint TheLichKingValithria = 16980;
        public const uint DreamPortalPreEffect = 38186;
        public const uint NightmarePortalPreEffect = 38429;
        public const uint DreamPortal = 37945;
        public const uint NightmarePortal = 38430;

        // Sindragosa
        public const uint Sindragosa = 36853;
        public const uint Spinestalker = 37534;
        public const uint Rimefang = 37533;
        public const uint FrostwardenHandler = 37531;
        public const uint FrostwingWhelp = 37532;
        public const uint IcyBlast = 38223;
        public const uint FrostBomb = 37186;
        public const uint IceTomb = 36980;

        // The Lich King
        public const uint TheLichKing = 36597;
        public const uint HighlordTirionFordringLk = 38995;
        public const uint TerenasMenethilFrostmourne = 36823;
        public const uint SpiritWarden = 36824;
        public const uint TerenasMenethilFrostmourneH = 39217;
        public const uint ShamblingHorror = 37698;
        public const uint DrudgeGhoul = 37695;
        public const uint IceSphere = 36633;
        public const uint RagingSpirit = 36701;
        public const uint Defile = 38757;
        public const uint ValkyrShadowguard = 36609;
        public const uint VileSpirit = 37799;
        public const uint WickedSpirit = 39190;
        public const uint StrangulateVehicle = 36598;
        public const uint WorldTrigger = 22515;
        public const uint WorldTriggerInfiniteAoi = 36171;
        public const uint SpiritBomb = 39189;
        public const uint FrostmourneTrigger = 38584;

        // Generic
        public const uint InvisibleStalker = 30298;
    }

    struct GameObjectIds
    {
        // ICC Teleporters
        public const uint TransporterLichKing = 202223;
        public const uint TransporterUpperSpire = 202235;
        public const uint TransporterLightsHammer = 202242;
        public const uint TransporterRampart = 202243;
        public const uint TransporterDeathBringer = 202244;
        public const uint TransporterOratory = 202245;
        public const uint TransporterSindragosa = 202246;

        // Lower Spire Trash
        public const uint SpiritAlarm1 = 201814;
        public const uint SpiritAlarm2 = 201815;
        public const uint SpiritAlarm3 = 201816;
        public const uint SpiritAlarm4 = 201817;

        // Lord Marrogar
        public const uint DoodadIcecrownIcewall02 = 201910;
        public const uint LordMarrowgarIcewall = 201911;
        public const uint LordMarrowgarSEntrance = 201857;

        // Lady Deathwhisper
        public const uint OratoryOfTheDamnedEntrance = 201563;
        public const uint LadyDeathwhisperElevator = 202220;

        // Icecrown Gunship Battle - Horde raid
        public const uint OrgrimsHammer_H = 201812;
        public const uint TheSkybreaker_H = 201811;
        public const uint GunshipArmory_H_10N = 202178;
        public const uint GunshipArmory_H_25N = 202180;
        public const uint GunshipArmory_H_10H = 202177;
        public const uint GunshipArmory_H_25H = 202179;

        // Icecrown Gunship Battle - Alliance raid
        public const uint OrgrimsHammer_A = 201581;
        public const uint TheSkybreaker_A = 201580;
        public const uint GunshipArmory_A_10N = 201873;
        public const uint GunshipArmory_A_25N = 201874;
        public const uint GunshipArmory_A_10H = 201872;
        public const uint GunshipArmory_A_25H = 201875;

        // Deathbringer Saurfang
        public const uint SaurfangSDoor = 201825;
        public const uint DeathbringerSCache10n = 202239;
        public const uint DeathbringerSCache25n = 202240;
        public const uint DeathbringerSCache10h = 202238;
        public const uint DeathbringerSCache25h = 202241;

        // Professor Putricide
        public const uint OrangePlagueMonsterEntrance = 201371;
        public const uint GreenPlagueMonsterEntrance = 201370;
        public const uint ScientistAirlockDoorCollision = 201612;
        public const uint ScientistAirlockDoorOrange = 201613;
        public const uint ScientistAirlockDoorGreen = 201614;
        public const uint DoodadIcecrownOrangetubes02 = 201617;
        public const uint DoodadIcecrownGreentubes02 = 201618;
        public const uint ScientistEntrance = 201372;
        public const uint DrinkMe = 201584;
        public const uint PlagueSigil = 202182;

        // Blood Prince Council
        public const uint CrimsonHallDoor = 201376;
        public const uint BloodElfCouncilDoor = 201378;
        public const uint BloodElfCouncilDoorRight = 201377;

        // Blood-Queen Lana'Thel
        public const uint DoodadIcecrownBloodprinceDoor01 = 201746;
        public const uint DoodadIcecrownGrate01 = 201755;
        public const uint BloodwingSigil = 202183;

        // Valithria Dreamwalker
        public const uint GreenDragonBossEntrance = 201375;
        public const uint GreenDragonBossExit = 201374;
        public const uint DoodadIcecrownRoostportcullis01 = 201380;
        public const uint DoodadIcecrownRoostportcullis02 = 201381;
        public const uint DoodadIcecrownRoostportcullis03 = 201382;
        public const uint DoodadIcecrownRoostportcullis04 = 201383;
        public const uint CacheOfTheDreamwalker10n = 201959;
        public const uint CacheOfTheDreamwalker25n = 202339;
        public const uint CacheOfTheDreamwalker10h = 202338;
        public const uint CacheOfTheDreamwalker25h = 202340;

        // Sindragosa
        public const uint SindragosaEntranceDoor = 201373;
        public const uint SindragosaShortcutEntranceDoor = 201369;
        public const uint SindragosaShortcutExitDoor = 201379;
        public const uint IceWall = 202396;
        public const uint IceBlock = 201722;
        public const uint SigilOfTheFrostwing = 202181;

        // The Lich King
        public const uint ArthasPlatform = 202161;
        public const uint ArthasPrecipice = 202078;
        public const uint DoodadIcecrownThronefrostywind01 = 202188;
        public const uint DoodadIcecrownThronefrostyedge01 = 202189;
        public const uint DoodadIceshardStanding02 = 202141;
        public const uint DoodadIceshardStanding01 = 202142;
        public const uint DoodadIceshardStanding03 = 202143;
        public const uint DoodadIceshardStanding04 = 202144;
        public const uint DoodadIcecrownSnowedgewarning01 = 202190;
        public const uint FrozenLavaman = 202436;
        public const uint LavamanPillarsChained = 202437;
        public const uint LavamanPillarsUnchained = 202438;
    }

    struct AchievementCriteriaIds
    {
        // Lord Marrowgar
        public const uint Boned10n = 12775;
        public const uint Boned25n = 12962;
        public const uint Boned10h = 13393;
        public const uint Boned25h = 13394;

        // Rotface
        public const uint DancesWithOozes10 = 12984;
        public const uint DancesWithOozes25 = 12966;
        public const uint DancesWithOozes10H = 12985;
        public const uint DancesWithOozes25H = 12983;

        // Professor Putricide
        public const uint Nausea10 = 12987;
        public const uint Nausea25 = 12968;
        public const uint Nausea10H = 12988;
        public const uint Nausea25H = 12981;

        // Blood Prince Council
        public const uint OrbWhisperer10 = 13033;
        public const uint OrbWhisperer25 = 12969;
        public const uint OrbWhisperer10H = 13034;
        public const uint OrbWhisperer25H = 13032;

        // Blood-Queen Lana'Thel
        public const uint KillLanaThel10m = 13340;
        public const uint KillLanaThel25m = 13360;
        public const uint OnceBittenTwiceShy10 = 12780;
        public const uint OnceBittenTwiceShy25 = 13012;
        public const uint OnceBittenTwiceShy10V = 13011;
        public const uint OnceBittenTwiceShy25V = 13013;
    }

    struct WeeklyQuestIds
    {
        public const uint Deprogramming10 = 24869;
        public const uint Deprogramming25 = 24875;
        public const uint SecuringTheRamparts10 = 24870;
        public const uint SecuringTheRamparts25 = 24877;
        public const uint ResidueRendezvous10 = 24873;
        public const uint ResidueRendezvous25 = 24878;
        public const uint BloodQuickening10 = 24874;
        public const uint BloodQuickening25 = 24879;
        public const uint RespiteForATornmentedSoul10 = 24872;
        public const uint RespiteForATornmentedSoul25 = 24880;
    }

    struct Actions
    {
        // Icecrown Gunship Battle
        public const int EnemyGunshipTalk = -369390;
        public const int ExitShip = -369391;

        // Festergut
        public const int FestergutCombat = -366260;
        public const int FestergutGas = -366261;
        public const int FestergutDeath = -366262;

        // Rotface
        public const int RotfaceCombat = -366270;
        public const int RotfaceOoze = -366271;
        public const int RotfaceDeath = -366272;
        public const int ChangePhase = -366780;

        // Blood-Queen Lana'Thel
        public const int KillMinchar = -379550;

        // Frostwing Halls Gauntlet Event
        public const int VrykulDeath = 37129;

        // Sindragosa
        public const int StartFrostwyrm = -368530;
        public const int TriggerAsphyxiation = -368531;

        // The Lich King
        public const int RestoreLight = -72262;
        public const int FrostmourneIntro = -36823;

        // Sister Svalna
        public const int KillCaptain = 1;
        public const int StartGauntlet = 2;
        public const int ResurrectCaptains = 3;
        public const int CaptainDies = 4;
        public const int ResetEvent = 5;
    }

    struct TeleporterSpells
    {
        public const uint LIGHT_S_HAMMER_TELEPORT = 70781;
        public const uint ORATORY_OF_THE_DAMNED_TELEPORT = 70856;
        public const uint RAMPART_OF_SKULLS_TELEPORT = 70857;
        public const uint DEATHBRINGER_S_RISE_TELEPORT = 7085;
        public const uint UPPER_SPIRE_TELEPORT = 70859;
        public const uint FROZEN_THRONE_TELEPORT = 70860;
        public const uint SINDRAGOSA_S_LAIR_TELEPORT = 70861;
    }

    struct AreaIds
    {
        public const uint IcecrownCitadel = 4812;
        public const uint TheFrozenThrone = 4859;
    }
}
