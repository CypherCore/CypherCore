// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.AI
{
    public struct PlayerSpells
	{
		/* Generic */
		public const uint AutoShot = 75;
		public const uint Shoot = 3018;
		public const uint Throw = 2764;
		public const uint Wand = 5019;

		/* Warrior - Generic */
		public const uint BattleStance = 2457;
		public const uint BerserkerStance = 2458;
		public const uint DefensiveStance = 71;
		public const uint Charge = 11578;
		public const uint Intercept = 20252;
		public const uint EnragedRegen = 55694;
		public const uint IntimidatingShout = 5246;
		public const uint Pummel = 6552;
		public const uint ShieldBash = 72;
		public const uint Bloodrage = 2687;

		/* Warrior - Arms */
		public const uint SweepingStrikes = 12328;
		public const uint MortalStrike = 12294;
		public const uint Bladestorm = 46924;
		public const uint Rend = 47465;
		public const uint Retaliation = 20230;
		public const uint ShatteringThrow = 64382;
		public const uint ThunderClap = 47502;

		/* Warrior - Fury */
		public const uint DeathWish = 12292;
		public const uint Bloodthirst = 23881;
		public const uint PassiveTitansGrip = 46917;
		public const uint DemoShout = 47437;
		public const uint Execute = 47471;
		public const uint HeroicFury = 60970;
		public const uint Recklessness = 1719;
		public const uint PiercingHowl = 12323;

		/* Warrior - Protection */
		public const uint Vigilance = 50720;
		public const uint Devastate = 20243;
		public const uint Shockwave = 46968;
		public const uint ConcussionBlow = 12809;
		public const uint Disarm = 676;
		public const uint LastStand = 12975;
		public const uint ShieldBlock = 2565;
		public const uint ShieldSlam = 47488;
		public const uint ShieldWall = 871;
		public const uint Reflection = 23920;

		/* Paladin - Generic */
		public const uint PalAuraMastery = 31821;
		public const uint LayOnHands = 48788;
		public const uint BlessingOfMight = 48932;
		public const uint AvengingWrath = 31884;
		public const uint DivineProtection = 498;
		public const uint DivineShield = 642;
		public const uint HammerOfJustice = 10308;
		public const uint HandOfFreedom = 1044;
		public const uint HandOfProtection = 10278;
		public const uint HandOfSacrifice = 6940;

		/* Paladin - Holy*/
		public const uint PassiveIllumination = 20215;
		public const uint HolyShock = 20473;
		public const uint BeaconOfLight = 53563;
		public const uint Consecration = 48819;
		public const uint FlashOfLight = 48785;
		public const uint HolyLight = 48782;
		public const uint DivineFavor = 20216;
		public const uint DivineIllumination = 31842;

		/* Paladin - Protection */
		public const uint BlessOfSanc = 20911;
		public const uint HolyShield = 20925;
		public const uint AvengersShield = 48827;
		public const uint DivineSacrifice = 64205;
		public const uint HammerOfRighteous = 53595;
		public const uint RighteousFury = 25780;
		public const uint ShieldOfRighteous = 61411;

		/* Paladin - Retribution */
		public const uint SealOfCommand = 20375;
		public const uint CrusaderStrike = 35395;
		public const uint DivineStorm = 53385;
		public const uint Judgement = 20271;
		public const uint HammerOfWrath = 48806;

		/* Hunter - Generic */
		public const uint Deterrence = 19263;
		public const uint ExplosiveTrap = 49067;
		public const uint FreezingArrow = 60192;
		public const uint RapidFire = 3045;
		public const uint KillShot = 61006;
		public const uint MultiShot = 49048;
		public const uint ViperSting = 3034;

		/* Hunter - Beast Mastery */
		public const uint BestialWrath = 19574;
		public const uint PassiveBeastWithin = 34692;
		public const uint PassiveBeastMastery = 53270;

		/* Hunter - Marksmanship */
		public const uint AimedShot = 19434;
		public const uint PassiveTrueshotAura = 19506;
		public const uint ChimeraShot = 53209;
		public const uint ArcaneShot = 49045;
		public const uint SteadyShot = 49052;
		public const uint Readiness = 23989;
		public const uint SilencingShot = 34490;

		/* Hunter - Survival */
		public const uint PassiveLockAndLoad = 56344;
		public const uint WyvernSting = 19386;
		public const uint ExplosiveShot = 53301;
		public const uint BlackArrow = 3674;

		/* Rogue - Generic */
		public const uint Dismantle = 51722;
		public const uint Evasion = 26669;
		public const uint Kick = 1766;
		public const uint Vanish = 26889;
		public const uint Blind = 2094;
		public const uint CloakOfShadows = 31224;

		/* Rogue - Assassination */
		public const uint ColdBlood = 14177;
		public const uint Mutilate = 1329;
		public const uint HungerForBlood = 51662;
		public const uint Envenom = 57993;

		/* Rogue - Combat */
		public const uint SinisterStrike = 48637;
		public const uint BladeFlurry = 13877;
		public const uint AdrenalineRush = 13750;
		public const uint KillingSpree = 51690;
		public const uint Eviscerate = 48668;

		/* Rogue - Sublety */
		public const uint Hemorrhage = 16511;
		public const uint Premeditation = 14183;
		public const uint ShadowDance = 51713;
		public const uint Preparation = 14185;
		public const uint Shadowstep = 36554;

		/* Priest - Generic */
		public const uint FearWard = 6346;
		public const uint PowerWordFort = 48161;
		public const uint DivineSpirit = 48073;
		public const uint ShadowProtection = 48169;
		public const uint DivineHymn = 64843;
		public const uint HymnOfHope = 64901;
		public const uint ShadowWordDeath = 48158;
		public const uint PsychicScream = 10890;

		/* Priest - Discipline */
		public const uint PassiveSoulWarding = 63574;
		public const uint PowerInfusion = 10060;
		public const uint Penance = 47540;
		public const uint PainSuppression = 33206;
		public const uint InnerFocus = 14751;
		public const uint PowerWordShield = 48066;

		/* Priest - Holy */
		public const uint PassiveSpiritRedemption = 20711;
		public const uint DesperatePrayer = 19236;
		public const uint GuardianSpirit = 47788;
		public const uint FlashHeal = 48071;
		public const uint Renew = 48068;

		/* Priest - Shadow */
		public const uint VampiricEmbrace = 15286;
		public const uint Shadowform = 15473;
		public const uint VampiricTouch = 34914;
		public const uint MindFlay = 15407;
		public const uint MindBlast = 48127;
		public const uint ShadowWordPain = 48125;
		public const uint DevouringPlague = 48300;
		public const uint Dispersion = 47585;

		/* Death Knight - Generic */
		public const uint DeathGrip = 49576;
		public const uint Strangulate = 47476;
		public const uint EmpowerRuneWeap = 47568;
		public const uint IcebornFortitude = 48792;
		public const uint AntiMagicShell = 48707;
		public const uint DeathCoilDk = 49895;
		public const uint MindFreeze = 47528;
		public const uint IcyTouch = 49909;
		public const uint AuraFrostFever = 55095;
		public const uint PlagueStrike = 49921;
		public const uint AuraBloodPlague = 55078;
		public const uint Pestilence = 50842;

		/* Death Knight - Blood */
		public const uint RuneTap = 48982;
		public const uint Hysteria = 49016;
		public const uint HeartStrike = 55050;
		public const uint DeathStrike = 49924;
		public const uint BloodStrike = 49930;
		public const uint MarkOfBlood = 49005;
		public const uint VampiricBlood = 55233;

		/* Death Knight - Frost */
		public const uint PassiveIcyTalons = 50887;
		public const uint FrostStrike = 49143;
		public const uint HowlingBlast = 49184;
		public const uint UnbreakableArmor = 51271;
		public const uint Obliterate = 51425;
		public const uint Deathchill = 49796;

		/* Death Knight - Unholy */
		public const uint PassiveUnholyBlight = 49194;
		public const uint PassiveMasterOfGhoul = 52143;
		public const uint ScourgeStrike = 55090;
		public const uint DeathAndDecay = 49938;
		public const uint AntiMagicZone = 51052;
		public const uint SummonGargoyle = 49206;

		/* Shaman - Generic */
		public const uint Heroism = 32182;
		public const uint Bloodlust = 2825;
		public const uint GroundingTotem = 8177;

		/* Shaman - Elemental*/
		public const uint PassiveElementalFocus = 16164;
		public const uint TotemOfWrath = 30706;
		public const uint Thunderstorm = 51490;
		public const uint LightningBolt = 49238;
		public const uint EarthShock = 49231;
		public const uint FlameShock = 49233;
		public const uint LavaBurst = 60043;
		public const uint ChainLightning = 49271;
		public const uint ElementalMastery = 16166;

		/* Shaman - Enhancement */
		public const uint PassiveSpiritWeapons = 16268;
		public const uint LavaLash = 60103;
		public const uint FeralSpirit = 51533;
		public const uint AuraMaelstromWeapon = 53817;
		public const uint Stormstrike = 17364;
		public const uint ShamanisticRage = 30823;

		/* Shaman - Restoration*/
		public const uint ShaNatureSwift = 591;
		public const uint ManaTideTotem = 590;
		public const uint EarthShield = 49284;
		public const uint Riptide = 61295;
		public const uint HealingWave = 49273;
		public const uint LesserHealWave = 49276;
		public const uint TidalForce = 55198;

		/* Mage - Generic */
		public const uint DampenMagic = 43015;
		public const uint Evocation = 12051;
		public const uint ManaShield = 43020;
		public const uint MirrorImage = 55342;
		public const uint Spellsteal = 30449;
		public const uint Counterspell = 2139;
		public const uint IceBlock = 45438;

		/* Mage - Arcane */
		public const uint FocusMagic = 54646;
		public const uint ArcanePower = 12042;
		public const uint ArcaneBarrage = 44425;
		public const uint ArcaneBlast = 42897;
		public const uint AuraArcaneBlast = 36032;
		public const uint ArcaneMissiles = 42846;
		public const uint PresenceOfMind = 12043;

		/* Mage - Fire */
		public const uint Pyroblast = 11366;
		public const uint Combustion = 11129;
		public const uint LivingBomb = 44457;
		public const uint Fireball = 42833;
		public const uint FireBlast = 42873;
		public const uint DragonsBreath = 31661;
		public const uint BlastWave = 11113;

		/* Mage - Frost */
		public const uint IcyVeins = 12472;
		public const uint IceBarrier = 11426;
		public const uint DeepFreeze = 44572;
		public const uint FrostNova = 42917;
		public const uint Frostbolt = 42842;
		public const uint ColdSnap = 11958;
		public const uint IceLance = 42914;

		/* Warlock - Generic */
		public const uint Fear = 6215;
		public const uint HowlOfTerror = 17928;
		public const uint Corruption = 47813;
		public const uint DeathCoilW = 47860;
		public const uint ShadowBolt = 47809;
		public const uint Incinerate = 47838;
		public const uint Immolate = 47811;
		public const uint SeedOfCorruption = 47836;

		/* Warlock - Affliction */
		public const uint PassiveSiphonLife = 63108;
		public const uint UnstableAffliction = 30108;
		public const uint Haunt = 48181;
		public const uint CurseOfAgony = 47864;
		public const uint DrainSoul = 47855;

		/* Warlock - Demonology */
		public const uint SoulLink = 19028;
		public const uint DemonicEmpowerment = 47193;
		public const uint Metamorphosis = 59672;
		public const uint ImmolationAura = 50589;
		public const uint DemonCharge = 54785;
		public const uint AuraDecimation = 63167;
		public const uint AuraMoltenCore = 71165;
		public const uint SoulFire = 47825;

		/* Warlock - Destruction */
		public const uint Shadowburn = 17877;
		public const uint Conflagrate = 17962;
		public const uint ChaosBolt = 50796;
		public const uint Shadowfury = 47847;

		/* Druid - Generic */
		public const uint Barkskin = 22812;
		public const uint Innervate = 29166;

		/* Druid - Balance */
		public const uint InsectSwarm = 5570;
		public const uint MoonkinForm = 24858;
		public const uint Starfall = 48505;
		public const uint Typhoon = 61384;
		public const uint AuraEclipseLunar = 48518;
		public const uint Moonfire = 48463;
		public const uint Starfire = 48465;
		public const uint Wrath = 48461;

		/* Druid - Feral */
		public const uint CatForm = 768;
		public const uint SurvivalInstincts = 61336;
		public const uint Mangle = 33917;
		public const uint Berserk = 50334;
		public const uint MangleCat = 48566;
		public const uint FeralChargeCat = 49376;
		public const uint Rake = 48574;
		public const uint Rip = 49800;
		public const uint SavageRoar = 52610;
		public const uint TigerFury = 50213;
		public const uint Claw = 48570;
		public const uint Dash = 33357;
		public const uint Maim = 49802;

		/* Druid - Restoration */
		public const uint Swiftmend = 18562;
		public const uint TreeOfLife = 33891;
		public const uint WildGrowth = 48438;
		public const uint NatureSwiftness = 17116;
		public const uint Tranquility = 48447;
		public const uint Nourish = 50464;
		public const uint HealingTouch = 48378;
		public const uint Rejuvenation = 48441;
		public const uint Regrowth = 48443;
		public const uint Lifebloom = 48451;
	}
}