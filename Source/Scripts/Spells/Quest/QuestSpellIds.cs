// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.


using Game.Spells;

namespace Scripts.Spells.Quest
{
    internal struct QuestSpellIds
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
}