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
 * Scripts for spells with SpellfamilyGeneric which cannot be included in Ai script file
 * of creature using it or can't be bound to any player class.
 * Ordered alphabetically using scriptname.
 * Scriptnames of files in this file should be prefixed with "spell_gen_"
 */

using Framework.Constants;
using Framework.Dynamic;
using Game;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Miscellaneous;
using Game.Movement;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Generic;

struct SpellIds
{
    // AdaptiveWarding
    public const uint GenAdaptiveWardingFire = 28765;
    public const uint GenAdaptiveWardingNature = 28768;
    public const uint GenAdaptiveWardingFrost = 28766;
    public const uint GenAdaptiveWardingShadow = 28769;
    public const uint GenAdaptiveWardingArcane = 28770;

    // AnimalBloodPoolSpell
    public const uint AnimalBlood = 46221;
    public const uint SpawnBloodPool = 63471;

    // GenericBandage
    public const uint RecentlyBandaged = 11196;

    // BloodReserve
    public const uint GenBloodReserveAura = 64568;
    public const uint GenBloodReserveHeal = 64569;

    // Bonked
    public const uint Bonked = 62991;
    public const uint FoamSwordDefeat = 62994;
    public const uint OnGuard = 62972;

    // BreakShieldSpells
    public const uint BreakShieldDamage2K = 62626;
    public const uint BreakShieldDamage10K = 64590;
    public const uint BreakShieldTriggerFactionMounts = 62575; // Also on ToC5 mounts
    public const uint BreakShieldTriggerCampaingWarhorse = 64595;
    public const uint BreakShieldTriggerUnk = 66480;

    // CannibalizeSpells
    public const uint CannibalizeTriggered = 20578;

    // ChaosBlast
    public const uint ChaosBlast = 37675;

    // Clone
    public const uint NightmareFigmentMirrorImage = 57528;

    // CloneWeaponSpells
    public const uint CopyWeaponAura = 41054;
    public const uint CopyWeapon2Aura = 63418;
    public const uint CopyWeapon3Aura = 69893;
    public const uint CopyOffhandAura = 45205;
    public const uint CopyOffhand2Aura = 69896;
    public const uint CopyRangedAura = 57594;

    // CreateLanceSpells
    public const uint CreateLanceAlliance = 63914;
    public const uint CreateLanceHorde = 63919;

    // DalaranDisguiseSpells
    public const uint SunreaverDisguiseTrigger = 69672;
    public const uint SunreaverDisguiseFemale = 70973;
    public const uint SunreaverDisguiseMale = 70974;

    public const uint SilverCovenantDisguiseTrigger = 69673;
    public const uint SilverCovenantDisguiseFemale = 70971;
    public const uint SilverCovenantDisguiseMale = 70972;

    // DefendVisuals
    public const uint VisualShield1 = 63130;
    public const uint VisualShield2 = 63131;
    public const uint VisualShield3 = 63132;

    // DivineStormSpell
    public const uint DivineStorm = 53385;

    // EtherealPet
    public const uint ProcTriggerOnKillAura = 50051;
    public const uint EtherealPetAura = 50055;
    public const uint CreateToken = 50063;
    public const uint StealEssenceVisual = 50101;

    // Feast
    public const uint GreatFeast = 57337;
    public const uint FishFeast = 57397;
    public const uint GiganticFeast = 58466;
    public const uint SmallFeast = 58475;
    public const uint BountifulFeast = 66477;
    public const uint FeastFood = 45548;
    public const uint FeastDrink = 57073;
    public const uint BountifulFeastDrink = 66041;
    public const uint BountifulFeastFood = 66478;
    public const uint GreatFeastRefreshment = 57338;
    public const uint FishFeastRefreshment = 57398;
    public const uint GiganticFeastRefreshment = 58467;
    public const uint SmallFeastRefreshment = 58477;
    public const uint BountifulFeastRefreshment = 66622;

    // FuriousRage
    public const uint Exhaustion = 35492;

    // FishingSpells
    public const uint FishingNoFishingPole = 131476;
    public const uint FishingWithPole = 131490;

    // TransporterBackfires
    public const uint TransporterMalfunctionPolymorph = 23444;
    public const uint TransporterEvilTwin = 23445;
    public const uint TransporterMalfunctionMiss = 36902;

    // GnomishTransporter
    public const uint TransporterSuccess = 23441;
    public const uint TransporterFailure = 23446;

    // Interrupt
    public const uint GenThrowInterrupt = 32747;

    // GenericLifebloom
    public const uint HexlordMalacrassLifebloomFinalHeal = 43422;
    public const uint TurRagepawLifebloomFinalHeal = 52552;
    public const uint CenarionScoutLifebloomFinalHeal = 53692;
    public const uint TwistedVisageLifebloomFinalHeal = 57763;
    public const uint FactionChampionsDruLifebloomFinalHeal = 66094;

    // ChargeSpells
    public const uint ChargeDamage8K5 = 62874;
    public const uint ChargeDamage20K = 68498;
    public const uint ChargeDamage45K = 64591;
    public const uint ChargeChargingEffect8K5 = 63661;
    public const uint ChargeCharging20K1 = 68284;
    public const uint ChargeCharging20K2 = 68501;
    public const uint ChargeChargingEffect45K1 = 62563;
    public const uint ChargeChargingEffect45K2 = 66481;
    public const uint ChargeTriggerFactionMounts = 62960;
    public const uint ChargeTriggerTrialChampion = 68282;
    public const uint ChargeMissEffect = 62977;

    // MossCoveredFeet
    public const uint FallDown = 6869;

    // Netherbloom : uint
    public const uint NetherbloomPollen1 = 28703;

    // NightmareVine
    public const uint NightmarePollen = 28721;

    // ObsidianArmor
    public const uint GenObsidianArmorHoly = 27536;
    public const uint GenObsidianArmorFire = 27533;
    public const uint GenObsidianArmorNature = 27538;
    public const uint GenObsidianArmorFrost = 27534;
    public const uint GenObsidianArmorShadow = 27535;
    public const uint GenObsidianArmorArcane = 27540;

    // OrcDisguiseSpells
    public const uint OrcDisguiseTrigger = 45759;
    public const uint OrcDisguiseMale = 45760;
    public const uint OrcDisguiseFemale = 45762;

    // ParalyticPoison
    public const uint Paralysis = 35202;

    // ParachuteSpells
    public const uint Parachute = 45472;
    public const uint ParachuteBuff = 44795;

    // ProfessionResearch
    public const uint NorthrendInscriptionResearch = 61177;

    // TrinketSpells
    public const uint PvpTrinketAlliance = 97403;
    public const uint PvpTrinketHorde = 97404;

    // Replenishment
    public const uint Replenishment = 57669;
    public const uint InfiniteReplenishment = 61782;

    // RunningWildMountIds
    public const uint AlteredForm = 97709;

    // SeaforiumSpells
    public const uint PlantChargesCreditAchievement = 60937;

    // TournamentMountsSpells
    public const uint LanceEquipped = 62853;

    // MountedDuelSpells
    public const uint OnTournamentMount = 63034;
    public const uint MountedDuel = 62875;

    // Teleporting
    public const uint TeleportSpireDown = 59316;
    public const uint TeleportSpireUp = 59314;

    // PvPTrinketTriggeredSpells
    public const uint WillOfTheForsakenCooldownTrigger = 72752;
    public const uint WillOfTheForsakenCooldownTriggerWotf = 72757;

    // FriendOrFowl
    public const uint TurkeyVengeance = 25285;

    // VampiricTouch
    public const uint VampiricTouchHeal = 52724;

    // VehicleScaling
    public const uint GearScaling = 66668;

    // WhisperGulchYoggSaronWhisper
    public const uint YoggSaronWhisperDummy = 29072;

    // GMFreeze
    public const uint GmFreeze = 9454;

    // RequiredMixologySpells
    public const uint Mixology = 53042;
    // Flasks
    public const uint FlaskOfTheFrostWyrm = 53755;
    public const uint FlaskOfStoneblood = 53758;
    public const uint FlaskOfEndlessRage = 53760;
    public const uint FlaskOfPureMojo = 54212;
    public const uint LesserFlaskOfResistance = 62380;
    public const uint LesserFlaskOfToughness = 53752;
    public const uint FlaskOfBlindingLight = 28521;
    public const uint FlaskOfChromaticWonder = 42735;
    public const uint FlaskOfFortification = 28518;
    public const uint FlaskOfMightyRestoration = 28519;
    public const uint FlaskOfPureDeath = 28540;
    public const uint FlaskOfRelentlessAssault = 28520;
    public const uint FlaskOfChromaticResistance = 17629;
    public const uint FlaskOfDistilledWisdom = 17627;
    public const uint FlaskOfSupremePower = 17628;
    public const uint FlaskOfTheTitans = 17626;
    // Elixirs
    public const uint ElixirOfMightyAgility = 28497;
    public const uint ElixirOfAccuracy = 60340;
    public const uint ElixirOfDeadlyStrikes = 60341;
    public const uint ElixirOfMightyDefense = 60343;
    public const uint ElixirOfExpertise = 60344;
    public const uint ElixirOfArmorPiercing = 60345;
    public const uint ElixirOfLightningSpeed = 60346;
    public const uint ElixirOfMightyFortitude = 53751;
    public const uint ElixirOfMightyMageblood = 53764;
    public const uint ElixirOfMightyStrength = 53748;
    public const uint ElixirOfMightyToughts = 60347;
    public const uint ElixirOfProtection = 53763;
    public const uint ElixirOfSpirit = 53747;
    public const uint GurusElixir = 53749;
    public const uint ShadowpowerElixir = 33721;
    public const uint WrathElixir = 53746;
    public const uint ElixirOfEmpowerment = 28514;
    public const uint ElixirOfMajorMageblood = 28509;
    public const uint ElixirOfMajorShadowPower = 28503;
    public const uint ElixirOfMajorDefense = 28502;
    public const uint FelStrengthElixir = 38954;
    public const uint ElixirOfIronskin = 39628;
    public const uint ElixirOfMajorAgility = 54494;
    public const uint ElixirOfDraenicWisdom = 39627;
    public const uint ElixirOfMajorFirepower = 28501;
    public const uint ElixirOfMajorFrostPower = 28493;
    public const uint EarthenElixir = 39626;
    public const uint ElixirOfMastery = 33726;
    public const uint ElixirOfHealingPower = 28491;
    public const uint ElixirOfMajorFortitude = 39625;
    public const uint ElixirOfMajorStrength = 28490;
    public const uint AdeptsElixir = 54452;
    public const uint OnslaughtElixir = 33720;
    public const uint MightyTrollsBloodElixir = 24361;
    public const uint GreaterArcaneElixir = 17539;
    public const uint ElixirOfTheMongoose = 17538;
    public const uint ElixirOfBruteForce = 17537;
    public const uint ElixirOfSages = 17535;
    public const uint ElixirOfSuperiorDefense = 11348;
    public const uint ElixirOfDemonslaying = 11406;
    public const uint ElixirOfGreaterFirepower = 26276;
    public const uint ElixirOfShadowPower = 11474;
    public const uint MagebloodElixir = 24363;
    public const uint ElixirOfGiants = 11405;
    public const uint ElixirOfGreaterAgility = 11334;
    public const uint ArcaneElixir = 11390;
    public const uint ElixirOfGreaterIntellect = 11396;
    public const uint ElixirOfGreaterDefense = 11349;
    public const uint ElixirOfFrostPower = 21920;
    public const uint ElixirOfAgility = 11328;
    public const uint MajorTrollsBlloodElixir = 3223;
    public const uint ElixirOfFortitude = 3593;
    public const uint ElixirOfOgresStrength = 3164;
    public const uint ElixirOfFirepower = 7844;
    public const uint ElixirOfLesserAgility = 3160;
    public const uint ElixirOfDefense = 3220;
    public const uint StrongTrollsBloodElixir = 3222;
    public const uint ElixirOfMinorAccuracy = 63729;
    public const uint ElixirOfWisdom = 3166;
    public const uint ElixirOfGianthGrowth = 8212;
    public const uint ElixirOfMinorAgility = 2374;
    public const uint ElixirOfMinorFortitude = 2378;
    public const uint WeakTrollsBloodElixir = 3219;
    public const uint ElixirOfLionsStrength = 2367;
    public const uint ElixirOfMinorDefense = 673;

    // LandmineKnockbackAchievement
    public const uint LandmineKnockbackAchievement = 57064;

    // CorruptinPlagueEntrys
    public const uint CorruptingPlague = 40350;

    // StasisFieldEntrys
    public const uint StasisField = 40307;

    // SiegeTankControl
    public const uint SiegeTankControl = 47963;

    // FreezingCircleMisc
    public const uint FreezingCirclePitOfSaronNormal = 69574;
    public const uint FreezingCirclePitOfSaronHeroic = 70276;
    public const uint FreezingCircle = 34787;
    public const uint FreezingCircleScenario = 141383;

    // CannonBlast
    public const uint CannonBlast = 42578;
    public const uint CannonBlastDamage = 42576;

    // KazrogalHellfireMark
    public const uint MarkOfKazrogalHellfire = 189512;
    public const uint MarkOfKazrogalDamageHellfire = 189515;

    // AuraProcRemoveSpells
    public const uint FaceRage = 99947;
    public const uint ImpatientMind = 187213;

    // DefenderOfAzerothData
    public const uint DeathGateTeleportStormwind = 316999;
    public const uint DeathGateTeleportOrgrimmar = 317000;

    // AncestralCallSpells
    public const uint RictusOfTheLaughingSkull = 274739;
    public const uint ZealOfTheBurningBlade = 274740;
    public const uint FerocityOfTheFrostwolf = 274741;
    public const uint MightOfTheBlackrock = 274742;

    // SkinningLearningSpell
    public const uint ClassicSkinning = 265856;
    public const uint OutlandSkinning = 265858;
    public const uint NorthrendSkinning = 265860;
    public const uint CataclysmSkinning = 265862;
    public const uint PandariaSkinning = 265864;
    public const uint DraenorSkinning = 265866;
    public const uint LegionSkinning = 265868;
    public const uint KulTiranSkinning = 265870;
    public const uint ZandalariSkinning = 265872;
    public const uint ShadowlandsSkinning = 308570;
    public const uint DragonIslesSkinning = 366263;

    // BloodlustExhaustionSpell
    public const uint ShamanSated = 57724; // Bloodlust
    public const uint ShamanExhaustion = 57723; // Heroism; Drums
    public const uint MageTemporalDisplacement = 80354;
    public const uint HunterFatigued = 264689;
    public const uint EvokerExhaustion = 390435;

    // MajorHealingCooldownSpell
    public const uint DruidTranquility = 740;
    public const uint DruidTranquilityHeal = 157982;
    public const uint PriestDivineHymn = 64843;
    public const uint PriestDivineHymnHeal = 64844;
    public const uint PriestLuminousBarrier = 271466;
    public const uint ShamanHealingTideTotem = 108280;
    public const uint ShamanHealingTideTotemHeal = 114942;
    public const uint MonkRevival = 115310;
    public const uint EvokerRewind = 363534;

    // SpatialRiftSpells
    public const uint SpatialRiftTeleport = 257034;
    public const uint SpatialRiftAreatrigger = 256948;
}

struct CreatureIds
{
    // EtherealPet
    public const uint EtherealSoulTrader = 27914;

    // PetSummoned
    public const uint Doomguard = 11859;
    public const uint Infernal = 89;
    public const uint Imp = 416;

    // VendorBarkTrigger
    public const uint AmphitheaterVendor = 30098;

    // CorruptinPlagueEntrys
    public const uint ApexisFlayer = 22175;
    public const uint ShardHideBoar = 22180;
    public const uint AetherRay = 22181;

    // StasisFieldEntrys
    public const uint DaggertailLizard = 22255;

    // DefenderOfAzerothData
    public const uint Nazgrim = 161706;
    public const uint Trollbane = 161707;
    public const uint Whitemane = 161708;
    public const uint Mograine = 161709;
}

struct MiscConst
{
    // EtherealPet
    public const uint SayStealEssence = 1;
    public const uint SayCreateToken = 2;


    // FuriousRage
    public const uint EmoteFuriousRage = 19415;
    public const uint EmoteExhausted = 18368;


    // Teleporting
    public const uint AreaVioletCitadelSpire = 4637;

    // FoamSword
    public const uint ItemFoamSwordGreen = 45061;
    public const uint ItemFoamSwordPink = 45176;
    public const uint ItemFoamSwordBlue = 45177;
    public const uint ItemFoamSwordRed = 45178;
    public const uint ItemFoamSwordYellow = 45179;

    // VendorBarkTrigger
    public const uint SayAmphitheaterVendor = 0;

    // WhisperToControllerTexts
    public const uint WhisperFutureYou = 2;
    public const uint WhisperDefender = 1;
    public const uint WhisperPastYou = 2;

    // PonySpells
    public const uint AchievPonyUp = 3736;
    public const uint MountPony = 29736;

    // FreezingCircleMisc
    public const uint MapIdBloodInTheSnowScenario = 1130;

    // DefenderOfAzerothData
    public const uint QuestDefenderOfAzerothAlliance = 58902;
    public const uint QuestDefenderOfAzerothHorde = 58903;

    public static float GetBonusMultiplier(Unit unit, uint spellId)
    {
        // Note: if caster is not in a raid setting, is in PvP or while in arena combat with 5 or less allied players.
        if (!unit.GetMap().IsRaid() || !unit.GetMap().IsBattleground())
        {
            uint bonusSpellId = 0;
            uint effIndex = 0;
            switch (spellId)
            {
                case SpellIds.DruidTranquilityHeal:
                    bonusSpellId = SpellIds.DruidTranquility;
                    effIndex = 2;
                    break;
                case SpellIds.PriestDivineHymnHeal:
                    bonusSpellId = SpellIds.PriestDivineHymn;
                    effIndex = 1;
                    break;
                case SpellIds.PriestLuminousBarrier:
                    bonusSpellId = spellId;
                    effIndex = 1;
                    break;
                case SpellIds.ShamanHealingTideTotemHeal:
                    bonusSpellId = SpellIds.ShamanHealingTideTotem;
                    effIndex = 2;
                    break;
                case SpellIds.MonkRevival:
                    bonusSpellId = spellId;
                    effIndex = 4;
                    break;
                case SpellIds.EvokerRewind:
                    bonusSpellId = spellId;
                    effIndex = 3;
                    break;
                default:
                    return 0.0f;
            }

            AuraEffect healingIncreaseEffect = unit.GetAuraEffect(bonusSpellId, effIndex);
            if (healingIncreaseEffect != null)
                return healingIncreaseEffect.GetAmount();

            return Global.SpellMgr.GetSpellInfo(bonusSpellId, Difficulty.None).GetEffect(effIndex).CalcValue(unit);
        }

        return 0.0f;
    }
}

[Script]
class spell_gen_absorb0_hitlimit1 : AuraScript
{
    uint limit = 0;

    public override bool Load()
    {
        // Max absorb stored in 1 dummy effect
        limit = (uint)GetSpellInfo().GetEffect(1).CalcValue();
        return true;
    }

    void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
    {
        absorbAmount = Math.Min(limit, absorbAmount);
    }

    public override void Register()
    {
        OnEffectAbsorb.Add(new(Absorb, 0));
    }
}

[Script]
class spell_gen_adaptive_warding : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GenAdaptiveWardingFire, SpellIds.GenAdaptiveWardingNature, SpellIds.GenAdaptiveWardingFrost, SpellIds.GenAdaptiveWardingShadow, SpellIds.GenAdaptiveWardingArcane);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.GetSpellInfo() == null)
            return false;

        // find Mage Armor
        if (GetTarget().GetAuraEffect(AuraType.ModManaRegenInterrupt, SpellFamilyNames.Mage, new FlagArray128(0x10000000, 0x0, 0x0)) == null)
            return false;

        switch (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()))
        {
            case SpellSchools.Normal:
            case SpellSchools.Holy:
                return false;
            default:
                break;
        }
        return true;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId = 0;
        switch (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()))
        {
            case SpellSchools.Fire:
                spellId = SpellIds.GenAdaptiveWardingFire;
                break;
            case SpellSchools.Nature:
                spellId = SpellIds.GenAdaptiveWardingNature;
                break;
            case SpellSchools.Frost:
                spellId = SpellIds.GenAdaptiveWardingFrost;
                break;
            case SpellSchools.Shadow:
                spellId = SpellIds.GenAdaptiveWardingShadow;
                break;
            case SpellSchools.Arcane:
                spellId = SpellIds.GenAdaptiveWardingArcane;
                break;
            default:
                return;
        }
        GetTarget().CastSpell(GetTarget(), spellId, aurEff);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script]
class spell_gen_allow_cast_from_item_only : SpellScript
{
    SpellCastResult CheckRequirement()
    {
        if (GetCastItem() == null)
            return SpellCastResult.CantDoThatRightNow;
        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckRequirement));
    }
}

[Script] // 46221 - Animal Blood
class spell_gen_animal_blood : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SpawnBloodPool);
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Remove all auras with spell id 46221, except the one currently being applied
        Aura aur;
        while ((aur = GetUnitOwner().GetOwnedAura(SpellIds.AnimalBlood, ObjectGuid.Empty, ObjectGuid.Empty, 0, GetAura())) != null)
            GetUnitOwner().RemoveOwnedAura(aur);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit owner = GetUnitOwner();
        if (owner != null)
            owner.CastSpell(owner, SpellIds.SpawnBloodPool, true);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
    }
}

[Script] // 63471 - Spawn Blood Pool
class spell_spawn_blood_pool : SpellScript
{
    void SetDest(ref SpellDestination dest)
    {
        Unit caster = GetCaster();
        Position summonPos = caster.GetPosition();
        LiquidData liquidStatus;
        if (caster.GetMap().GetLiquidStatus(caster.GetPhaseShift(), caster.GetPositionX(), caster.GetPositionY(), caster.GetPositionZ(), out liquidStatus, null, caster.GetCollisionHeight()) != ZLiquidStatus.NoWater)
            summonPos.posZ = liquidStatus.level;
        dest.Relocate(summonPos);
    }

    public override void Register()
    {
        OnDestinationTargetSelect.Add(new(SetDest, 0, Targets.DestCaster));
    }
}

// 430 Drink
// 431 Drink
// 432 Drink
// 1133 Drink
// 1135 Drink
// 1137 Drink
// 10250 Drink
// 22734 Drink
// 27089 Drink
// 34291 Drink
// 43182 Drink
// 43183 Drink
// 46755 Drink
// 49472 Drink Coffee
// 57073 Drink
// 61830 Drink
[Script] // 72623 Drink
class spell_gen_arena_drink : AuraScript
{
    public override bool Load()
    {
        return GetCaster() != null && GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        if (!ValidateSpellEffect((spellInfo.Id, 0)) || !spellInfo.GetEffect(0).IsAura(AuraType.ModPowerRegen))
        {
            Log.outError(LogFilter.Spells, $"Aura {GetId()} structure has been changed - first aura is no longer SpellAuraModPowerRegen");
            return false;
        }

        return true;
    }

    void CalcPeriodic(AuraEffect aurEff, ref bool isPeriodic, ref int amplitude)
    {
        // Get SpellAuraModPowerRegen aura from spell
        AuraEffect regen = GetAura().GetEffect(0);
        if (regen == null)
            return;

        // default case - not in arena
        if (!GetCaster().ToPlayer().InArena())
            isPeriodic = false;
    }

    void CalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        AuraEffect regen = GetAura().GetEffect(0);
        if (regen == null)
            return;

        // default case - not in arena
        if (!GetCaster().ToPlayer().InArena())
            regen.ChangeAmount(amount);
    }

    void UpdatePeriodic(AuraEffect aurEff)
    {
        AuraEffect regen = GetAura().GetEffect(0);
        if (regen == null)
            return;

        // **********************************************
        // This feature used only in arenas
        // **********************************************
        // Here need increase mana regen per tick (6 second rule)
        // on 0 tick -   0  (handled in 2 second)
        // on 1 tick - 166% (handled in 4 second)
        // on 2 tick - 133% (handled in 6 second)

        // Apply bonus for 1 - 4 tick
        switch (aurEff.GetTickNumber())
        {
            case 1:   // 0%
                regen.ChangeAmount(0);
                break;
            case 2:   // 166%
                regen.ChangeAmount(aurEff.GetAmount() * 5 / 3);
                break;
            case 3:   // 133%
                regen.ChangeAmount(aurEff.GetAmount() * 4 / 3);
                break;
            default:  // 100% - normal regen
                regen.ChangeAmount(aurEff.GetAmount());
                // No need to update after 4th tick
                aurEff.SetPeriodic(false);
                break;
        }
    }

    public override void Register()
    {
        DoEffectCalcPeriodic.Add(new(CalcPeriodic, 1, AuraType.PeriodicDummy));
        DoEffectCalcAmount.Add(new(CalcAmount, 1, AuraType.PeriodicDummy));
        OnEffectUpdatePeriodic.Add(new(UpdatePeriodic, 1, AuraType.PeriodicDummy));
    }
}

[Script] // 28313 - Aura of Fear
class spell_gen_aura_of_fear : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 0)) && ValidateSpellInfo(spellInfo.GetEffect(0).TriggerSpell);
    }

    void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();
        if (!RandomHelper.randChance(GetSpellInfo().ProcChance))
            return;

        GetTarget().CastSpell(null, aurEff.GetSpellEffectInfo().TriggerSpell, true);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script]
class spell_gen_av_drekthar_presence : AuraScript
{
    bool CheckAreaTarget(Unit target)
    {
        switch (target.GetEntry())
        {
            // alliance
            case 14762: // Dun Baldar North Marshal
            case 14763: // Dun Baldar South Marshal
            case 14764: // Icewing Marshal
            case 14765: // Stonehearth Marshal
            case 11948: // Vandar Stormspike
            // horde
            case 14772: // East Frostwolf Warmaster
            case 14776: // Tower Point Warmaster
            case 14773: // Iceblood Warmaster
            case 14777: // West Frostwolf Warmaster
            case 11946: // Drek'thar
                return true;
            default:
                return false;
        }
    }

    public override void Register()
    {
        DoCheckAreaTarget.Add(new(CheckAreaTarget));
    }
}

[Script]
class spell_gen_bandage : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RecentlyBandaged);
    }

    SpellCastResult CheckCast()
    {
        Unit target = GetExplTargetUnit();
        if (target != null)
        {
            if (target.HasAura(SpellIds.RecentlyBandaged))
                return SpellCastResult.TargetAurastate;
        }
        return SpellCastResult.SpellCastOk;
    }

    void HandleScript()
    {
        Unit target = GetHitUnit();
        if (target != null)
            GetCaster().CastSpell(target, SpellIds.RecentlyBandaged, true);
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
        AfterHit.Add(new(HandleScript));
    }
}

[Script] // 193970 - Mercenary Shapeshift
class spell_gen_battleground_mercenary_shapeshift : AuraScript
{
    static Dictionary<Race, uint[]> RaceDisplayIds = new()
    {
        [Race.Human] = [55239, 55238],
        [Race.Orc] = [55257, 55256],
        [Race.Dwarf] = [55241, 55240],
        [Race.NightElf] = [55243, 55242],
        [Race.Undead] = [55259, 55258],
        [Race.Tauren] = [55261, 55260],
        [Race.Gnome] = [55245, 55244],
        [Race.Troll] = [55263, 55262],
        [Race.Goblin] = [55267, 57244],
        [Race.BloodElf] = [55265, 55264],
        [Race.Draenei] = [55247, 55246],
        [Race.Worgen] = [55255, 55254],
        [Race.PandarenNeutral] = [55253, 55252], // not verified, might be swapped with RacePandarenHorde
        [Race.PandarenAlliance] = [55249, 55248],
        [Race.PandarenHorde] = [55251, 55250],
        [Race.Nightborne] = [82375, 82376],
        [Race.HighmountainTauren] = [82377, 82378],
        [Race.VoidElf] = [82371, 82372],
        [Race.LightforgedDraenei] = [82373, 82374],
        [Race.ZandalariTroll] = [88417, 88416],
        [Race.KulTiran] = [88414, 88413],
        [Race.DarkIronDwarf] = [88409, 88408],
        [Race.Vulpera] = [94999, 95001],
        [Race.MagharOrc] = [88420, 88410],
        [Race.MechaGnome] = [94998, 95000]
    };

    List<uint> RacialSkills = new();

    static Race GetReplacementRace(Race nativeRace, Class playerClass)
    {
        var charBaseInfo = Global.DB2Mgr.GetCharBaseInfo(nativeRace, playerClass);
        if (charBaseInfo != null)
            if (Global.ObjectMgr.GetPlayerInfo((Race)charBaseInfo.OtherFactionRaceID, playerClass) != null)
                return (Race)charBaseInfo.OtherFactionRaceID;

        return Race.None;
    }

    static uint GetDisplayIdForRace(Race race, Gender gender)
    {
        var displayIds = RaceDisplayIds.LookupByKey(race);
        if (displayIds != null)
            return displayIds[(int)gender];

        return 0;
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        foreach (var (race, displayIds) in RaceDisplayIds)
        {
            if (!CliDB.ChrRacesStorage.ContainsKey((uint)race))
                return false;

            foreach (uint displayId in displayIds)
                if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(displayId))
                    return false;
        }

        RacialSkills.Clear();
        foreach (var (_, skillLine) in CliDB.SkillLineStorage)
            if (skillLine.HasFlag(SkillLineFlags.RacialForThePurposeOfTemporaryRaceChange))
                RacialSkills.Add(skillLine.Id);

        return true;
    }

    void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit owner = GetUnitOwner();
        Race otherFactionRace = GetReplacementRace(owner.GetRace(), owner.GetClass());
        if (otherFactionRace == Race.None)
            return;

        uint displayId = GetDisplayIdForRace(otherFactionRace, owner.GetNativeGender());
        if (displayId != 0)
            owner.SetDisplayId(displayId);

        if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
            UpdateRacials(owner.GetRace(), otherFactionRace);
    }

    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit owner = GetUnitOwner();
        Race otherFactionRace = GetReplacementRace(owner.GetRace(), owner.GetClass());
        if (otherFactionRace == Race.None)
            return;

        UpdateRacials(otherFactionRace, owner.GetRace());
    }

    void UpdateRacials(Race oldRace, Race newRace)
    {
        Player player = GetUnitOwner().ToPlayer();
        if (player == null)
            return;

        foreach (uint racialSkillId in RacialSkills)
        {
            if (Global.DB2Mgr.GetSkillRaceClassInfo(racialSkillId, oldRace, player.GetClass()) != null)
            {
                var skillLineAbilities = Global.DB2Mgr.GetSkillLineAbilitiesBySkill(racialSkillId);
                if (skillLineAbilities != null)
                    foreach (var ability in skillLineAbilities)
                        player.RemoveSpell(ability.Spell, false, false);
            }

            if (Global.DB2Mgr.GetSkillRaceClassInfo(racialSkillId, newRace, player.GetClass()) != null)
                player.LearnSkillRewardedSpells(racialSkillId, player.GetMaxSkillValueForLevel(), newRace);
        }
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.SendForClientMask));
        AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real));
    }
}

[Script] // Blood Reserve - 64568
class spell_gen_blood_reserve : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GenBloodReserveHeal);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        Unit caster = eventInfo.GetActionTarget();
        if (caster != null)
            if (caster.HealthBelowPct(35))
                return true;

        return false;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Unit caster = eventInfo.GetActionTarget();
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
        caster.CastSpell(caster, SpellIds.GenBloodReserveHeal, args);
        caster.RemoveAura(SpellIds.GenBloodReserveAura);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script]
class spell_gen_bonked : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Player target = GetHitPlayer();
        if (target != null)
        {
            Aura aura = GetHitAura();
            if (!(aura != null && aura.GetStackAmount() == 3))
                return;

            target.CastSpell(target, SpellIds.FoamSwordDefeat, true);
            target.RemoveAurasDueToSpell(SpellIds.Bonked);

            Aura auraOnGuard = target.GetAura(SpellIds.OnGuard);
            if (auraOnGuard != null)
            {
                Item item = target.GetItemByGuid(auraOnGuard.GetCastItemGUID());
                if (item != null)
                    target.DestroyItemCount(item.GetEntry(), 1, true);
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
    }
}

[Script("spell_gen_break_shield")]
[Script("spell_gen_tournament_counterattack")]
class spell_gen_break_shield : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(62552, 62719, 64100, 66482);
    }

    void HandleScriptEffect(uint effIndex)
    {
        Unit target = GetHitUnit();

        switch (effIndex)
        {
            case 0: // On spells wich trigger the damaging spell (and also the visual)
            {
                uint spellId;

                switch (GetSpellInfo().Id)
                {
                    case SpellIds.BreakShieldTriggerUnk:
                    case SpellIds.BreakShieldTriggerCampaingWarhorse:
                        spellId = SpellIds.BreakShieldDamage10K;
                        break;
                    case SpellIds.BreakShieldTriggerFactionMounts:
                        spellId = SpellIds.BreakShieldDamage2K;
                        break;
                    default:
                        return;
                }

                Unit rider = GetCaster().GetCharmer();
                if (rider != null)
                    rider.CastSpell(target, spellId, false);
                else
                    GetCaster().CastSpell(target, spellId, false);
                break;
            }
            case 1: // On damaging spells, for removing a defend layer
            {
                var auras = target.GetAppliedAuras();
                foreach (var itr in auras)
                {
                    Aura aura = itr.Value.GetBase();
                    if (aura != null)
                    {
                        if (aura.GetId() == 62552 || aura.GetId() == 62719 || aura.GetId() == 64100 || aura.GetId() == 66482)
                        {
                            aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                            // Remove dummys from rider (Necessary for updating visual shields)
                            Unit rider = target.GetCharmer();
                            if (rider != null)
                            {
                                Aura defend = rider.GetAura(aura.GetId());
                                if (defend != null)
                                    defend.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                            }
                            break;
                        }
                    }
                }
                break;
            }
            default:
                break;
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect));
    }
}

[Script] // 48750 - Burning Depths Necrolyte Image
class spell_gen_burning_depths_necrolyte_image : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 2))
            && ValidateSpellInfo((uint)spellInfo.GetEffect(2).CalcValue());
    }

    void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit caster = GetCaster();
        if (caster != null)
            caster.CastSpell(GetTarget(), (uint)GetEffectInfo(2).CalcValue());
    }

    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell((uint)GetEffectInfo(2).CalcValue(), GetCasterGUID());
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real));
    }
}

[Script]
class spell_gen_cannibalize : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CannibalizeTriggered);
    }

    SpellCastResult CheckIfCorpseNear()
    {
        Unit caster = GetCaster();
        float max_range = GetSpellInfo().GetMaxRange(false);
        // search for nearby enemy corpse in range
        AnyDeadUnitSpellTargetInRangeCheck<WorldObject> check = new(caster, max_range, GetSpellInfo(), SpellTargetCheckTypes.Enemy, SpellTargetObjectTypes.CorpseEnemy);
        WorldObjectSearcher searcher = new(caster, check);
        Cell.VisitWorldObjects(caster, searcher, max_range);
        if (searcher.GetResult() == null)
            Cell.VisitGridObjects(caster, searcher, max_range);
        if (searcher.HasResult())
            return SpellCastResult.NoEdibleCorpses;
        return SpellCastResult.SpellCastOk;
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.CannibalizeTriggered, false);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        OnCheckCast.Add(new(CheckIfCorpseNear));
    }
}

[Script] // 66020 Chains of Ice
class spell_gen_chains_of_ice : AuraScript
{
    void UpdatePeriodic(AuraEffect aurEff)
    {
        // Get 0 effect aura
        AuraEffect slow = GetAura().GetEffect(0);
        if (slow == null)
            return;

        int newAmount = Math.Min(slow.GetAmount() + aurEff.GetAmount(), 0);
        slow.ChangeAmount(newAmount);
    }

    public override void Register()
    {
        OnEffectUpdatePeriodic.Add(new(UpdatePeriodic, 1, AuraType.PeriodicDummy));
    }
}

[Script]
class spell_gen_chaos_blast : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ChaosBlast);
    }

    void HandleDummy(uint effIndex)
    {
        int basepoints0 = 100;
        Unit caster = GetCaster();
        Unit target = GetHitUnit();
        if (target != null)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, basepoints0);
            caster.CastSpell(target, SpellIds.ChaosBlast, args);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 28471 - ClearAll
class spell_clear_all : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Unit caster = GetCaster();
        caster.RemoveAllAurasOnDeath();
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_clone : SpellScript
{
    void HandleScriptEffect(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
    }

    public override void Register()
    {
        if (m_scriptSpellId == SpellIds.NightmareFigmentMirrorImage)
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.Dummy));
            OnEffectHitTarget.Add(new(HandleScriptEffect, 2, SpellEffectName.Dummy));
        }
        else
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
            OnEffectHitTarget.Add(new(HandleScriptEffect, 2, SpellEffectName.ScriptEffect));
        }
    }
}

[Script]
class spell_gen_clone_weapon : SpellScript
{
    void HandleScriptEffect(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_clone_weapon_aura : AuraScript
{
    uint prevItem = 0;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CopyWeaponAura, SpellIds.CopyWeapon2Aura, SpellIds.CopyWeapon3Aura, SpellIds.CopyOffhandAura, SpellIds.CopyOffhand2Aura, SpellIds.CopyRangedAura);
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit caster = GetCaster();
        Unit target = GetTarget();
        if (caster == null)
            return;

        switch (GetSpellInfo().Id)
        {
            case SpellIds.CopyWeaponAura:
            case SpellIds.CopyWeapon2Aura:
            case SpellIds.CopyWeapon3Aura:
            {
                prevItem = target.GetVirtualItemId(0);

                Player player = caster.ToPlayer();
                if (player != null)
                {
                    Item mainItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                    if (mainItem != null)
                        target.SetVirtualItem(0, mainItem.GetEntry());
                }
                else
                    target.SetVirtualItem(0, caster.GetVirtualItemId(0));
                break;
            }
            case SpellIds.CopyOffhandAura:
            case SpellIds.CopyOffhand2Aura:
            {
                prevItem = target.GetVirtualItemId(1);

                Player player = caster.ToPlayer();
                if (player != null)
                {
                    Item offItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                    if (offItem != null)
                        target.SetVirtualItem(1, offItem.GetEntry());
                }
                else
                    target.SetVirtualItem(1, caster.GetVirtualItemId(1));
                break;
            }
            case SpellIds.CopyRangedAura:
            {
                prevItem = target.GetVirtualItemId(2);

                Player player = caster.ToPlayer();
                if (player != null)
                {
                    Item rangedItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                    if (rangedItem != null)
                        target.SetVirtualItem(2, rangedItem.GetEntry());
                }
                else
                    target.SetVirtualItem(2, caster.GetVirtualItemId(2));
                break;
            }
            default:
                break;
        }
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();

        switch (GetSpellInfo().Id)
        {
            case SpellIds.CopyWeaponAura:
            case SpellIds.CopyWeapon2Aura:
            case SpellIds.CopyWeapon3Aura:
                target.SetVirtualItem(0, prevItem);
                break;
            case SpellIds.CopyOffhandAura:
            case SpellIds.CopyOffhand2Aura:
                target.SetVirtualItem(1, prevItem);
                break;
            case SpellIds.CopyRangedAura:
                target.SetVirtualItem(2, prevItem);
                break;
            default:
                break;
        }
    }

    public override void Register()
    {
        OnEffectApply.Add(new(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
    }
}

[Script("spell_gen_default_count_pct_from_max_hp", 0)]
[Script("spell_gen_50pct_count_pct_from_max_hp", 50)]
class spell_gen_count_pct_from_max_hp(int damagePct) : SpellScript()
{
    void RecalculateDamage()
    {
        if (damagePct == 0)
            damagePct = GetHitDamage();

        SetHitDamage((int)GetHitUnit().CountPctFromMaxHealth(damagePct));
    }

    public override void Register()
    {
        OnHit.Add(new(RecalculateDamage));
    }
}

// 28865 - Consumption
[Script] // 64208 - Consumption
class spell_gen_consumption : SpellScript
{
    void CalculateDamage(SpellEffectInfo spellEffectInfo, Unit victim, ref int damage, ref int flatMod, ref float pctMod)
    {
        SpellInfo createdBySpell = Global.SpellMgr.GetSpellInfo(GetCaster().m_unitData.CreatedBySpell, GetCastDifficulty());
        if (createdBySpell != null)
            damage = createdBySpell.GetEffect(1).CalcValue();
    }

    public override void Register()
    {
        CalcDamage.Add(new(CalculateDamage));
    }
}

[Script] // 63845 - Create Lance
class spell_gen_create_lance : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CreateLanceAlliance, SpellIds.CreateLanceHorde);
    }

    void HandleScript(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        Player target = GetHitPlayer();
        if (target != null)
        {
            if (target.GetTeam() == Team.Alliance)
                GetCaster().CastSpell(target, SpellIds.CreateLanceAlliance, true);
            else if (target.GetTeam() == Team.Horde)
                GetCaster().CastSpell(target, SpellIds.CreateLanceHorde, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script("spell_gen_sunreaver_disguise")]
[Script("spell_gen_silver_covenant_disguise")]
class spell_gen_dalaran_disguise : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        switch (spellInfo.Id)
        {
            case SpellIds.SunreaverDisguiseTrigger:
                return ValidateSpellInfo(SpellIds.SunreaverDisguiseFemale, SpellIds.SunreaverDisguiseMale);
            case SpellIds.SilverCovenantDisguiseTrigger:
                return ValidateSpellInfo(SpellIds.SilverCovenantDisguiseFemale, SpellIds.SilverCovenantDisguiseMale);
            default:
                break;
        }

        return false;
    }

    void HandleScript(uint effIndex)
    {
        Player player = GetHitPlayer();
        if (player != null)
        {
            Gender gender = player.GetNativeGender();

            uint spellId = GetSpellInfo().Id;
            switch (spellId)
            {
                case SpellIds.SunreaverDisguiseTrigger:
                    spellId = gender == Gender.Female ? SpellIds.SunreaverDisguiseFemale : SpellIds.SunreaverDisguiseMale;
                    break;
                case SpellIds.SilverCovenantDisguiseTrigger:
                    spellId = gender == Gender.Female ? SpellIds.SilverCovenantDisguiseFemale : SpellIds.SilverCovenantDisguiseMale;
                    break;
                default:
                    break;
            }

            GetCaster().CastSpell(player, spellId, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_decay_over_time_spell : SpellScript
{
    void ModAuraStack()
    {
        Aura aur = GetHitAura();
        if (aur != null)
            aur.SetStackAmount((byte)GetSpellInfo().StackAmount);
    }

    public override void Register()
    {
        AfterHit.Add(new(ModAuraStack));
    }
}

[Script] // 32065 - Fungal Decay
class spell_gen_decay_over_time_fungal_decay : AuraScript
{
    // found in sniffs, there is no duration entry we can possibly use
    const int AuraDuration = 12600;

    bool CheckProc(ProcEventInfo eventInfo)
    {
        return (eventInfo.GetSpellInfo() == GetSpellInfo());
    }

    void Decay(ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        ModStackAmount(-1);
    }

    void ModDuration(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // only on actual reapply, not on stack decay
        if (GetDuration() == GetMaxDuration())
        {
            SetMaxDuration(AuraDuration);
            SetDuration(AuraDuration);
        }
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnProc.Add(new(Decay));
        OnEffectApply.Add(new(ModDuration, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.RealOrReapplyMask));
    }
}

[Script] // 36659 - Tail Sting
class spell_gen_decay_over_time_tail_sting : AuraScript
{
    bool CheckProc(ProcEventInfo eventInfo)
    {
        return (eventInfo.GetSpellInfo() == GetSpellInfo());
    }

    void Decay(ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        ModStackAmount(-1);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnProc.Add(new(Decay));
    }
}

[Script]
class spell_gen_defend : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.VisualShield1, SpellIds.VisualShield2, SpellIds.VisualShield3);
    }

    void RefreshVisualShields(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetCaster() != null)
        {
            Unit target = GetTarget();

            for (byte i = 0; i < GetSpellInfo().StackAmount; ++i)
                target.RemoveAurasDueToSpell(SpellIds.VisualShield1 + i);

            target.CastSpell(target, SpellIds.VisualShield1 + GetAura().GetStackAmount() - 1, aurEff);
        }
        else
            GetTarget().RemoveAurasDueToSpell(GetId());
    }

    void RemoveVisualShields(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        for (byte i = 0; i < GetSpellInfo().StackAmount; ++i)
            GetTarget().RemoveAurasDueToSpell(SpellIds.VisualShield1 + i);
    }

    void RemoveDummyFromDriver(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            TempSummon vehicle = caster.ToTempSummon();
            if (vehicle != null)
            {
                Unit rider = vehicle.GetSummonerUnit();
                if (rider != null)
                    rider.RemoveAurasDueToSpell(GetId());
            }
        }
    }

    public override void Register()
    {
        /*
        SpellInfo  spell = Global.SpellMgr.AssertSpellInfo(m_scriptSpellId, Difficulty.None);

        // 6.x effects removed

        // Defend spells cast by NPCs (add visuals)
        if (spell.GetEffect(0).ApplyAuraName == SpellAuraModDamagePercentTaken)
        {
            AfterEffectApply.Add(new (RefreshVisualShields, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.RealOrReapplyMask));
            OnEffectRemove.Add(new (RemoveVisualShields, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.ChangeAmountMask));
        }

        // Remove Defend spell from player when he dismounts
        if (spell.GetEffect(2).ApplyAuraName == SpellAuraModDamagePercentTaken)
            OnEffectRemove.Add(new (RemoveDummyFromDriver, 2, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));

        // Defend spells cast by players (add/remove visuals)
        if (spell.GetEffect(1).ApplyAuraName == SpellAuraDummy)
        {
            AfterEffectApply.Add(new (RefreshVisualShields, 1, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
            OnEffectRemove.Add(new (RemoveVisualShields, 1, AuraType.Dummy, AuraEffectHandleModes.ChangeAmountMask));
        }
        */
    }
}

[Script]
class spell_gen_despawn_aura : AuraScript
{
    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Creature target = GetTarget().ToCreature();
        if (target != null)
            target.DespawnOrUnsummon();
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, SpellConst.EffectFirstFound, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] /// @todo: migrate spells to spell_gen_despawn_target, then remove this
class spell_gen_despawn_self : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsUnit();
    }

    void HandleDummy(uint effIndex)
    {
        if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) || GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
            GetCaster().ToCreature().DespawnOrUnsummon();
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, SpellConst.EffectAll, SpellEffectName.Any));
    }
}

[Script]
class spell_gen_despawn_target : SpellScript
{
    void HandleDespawn(uint effIndex)
    {
        if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) || GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
        {
            Creature target = GetHitCreature();
            if (target != null)
                target.DespawnOrUnsummon();
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDespawn, SpellConst.EffectAll, SpellEffectName.Any));
    }
}

[Script] // 70769 Divine Storm!
class spell_gen_divine_storm_cd_reset : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DivineStorm);
    }

    void HandleScript(uint effIndex)
    {
        GetCaster().GetSpellHistory().ResetCooldown(SpellIds.DivineStorm, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_gen_ds_flush_knockback : SpellScript
{
    void HandleScript(uint effIndex)
    {
        // Here the target is the water spout and determines the position where the player is knocked from
        Unit target = GetHitUnit();
        if (target != null)
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                float horizontalSpeed = 20.0f + (40.0f - GetCaster().GetDistance(target));
                float verticalSpeed = 8.0f;
                // This method relies on the Dalaran Sewer map disposition and Water Spout position
                // What we do is knock the player from a position exactly behind him and at the end of the pipe
                player.KnockbackFrom(target.GetPosition(), horizontalSpeed, verticalSpeed);
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.Dummy));
    }
}

[Script] // 50051 - Ethereal Pet Aura
class spell_ethereal_pet_aura : AuraScript
{
    bool CheckProc(ProcEventInfo eventInfo)
    {
        uint levelDiff = (uint)Math.Abs(GetTarget().GetLevel() - eventInfo.GetProcTarget().GetLevel());
        return levelDiff <= 9;
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        List<TempSummon> minionList = GetUnitOwner().GetAllMinionsByEntry(CreatureIds.EtherealSoulTrader);
        foreach (Creature minion in minionList)
        {
            if (minion.IsAIEnabled())
            {
                minion.GetAI().Talk(MiscConst.SayStealEssence);
                minion.CastSpell(eventInfo.GetProcTarget(), SpellIds.StealEssenceVisual);
            }
        }
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(HandleProc, 0, AuraType.ProcTriggerSpell));
    }
}

[Script] // 50052 - Ethereal Pet onSummon
class spell_ethereal_pet_onsummon : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ProcTriggerOnKillAura);
    }

    void HandleScriptEffect(uint effIndex)
    {
        Unit target = GetHitUnit();
        target.CastSpell(target, SpellIds.ProcTriggerOnKillAura, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 50055 - Ethereal Pet Aura Remove
class spell_ethereal_pet_aura_remove : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.EtherealPetAura);
    }

    void HandleScriptEffect(uint effIndex)
    {
        GetHitUnit().RemoveAurasDueToSpell(SpellIds.EtherealPetAura);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 50101 - Ethereal Pet OnKill Steal Essence
class spell_steal_essence_visual : AuraScript
{
    void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            caster.CastSpell(caster, SpellIds.CreateToken, true);
            Creature soulTrader = caster.ToCreature();
            if (soulTrader != null)
                soulTrader.GetAI().Talk(MiscConst.SayCreateToken);
        }
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

/* 57337 - Great Feast
   57397 - Fish Feast
   58466 - Gigantic Feast
   58475 - Small Feast
   66477 - Bountiful Feast */
[Script]
class spell_gen_feast : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FeastFood, SpellIds.FeastDrink, SpellIds.BountifulFeastDrink, SpellIds.BountifulFeastFood, SpellIds.GreatFeastRefreshment, SpellIds.FishFeastRefreshment, SpellIds.GiganticFeastRefreshment, SpellIds.SmallFeastRefreshment, SpellIds.BountifulFeastRefreshment);
    }

    void HandleScript(uint effIndex)
    {
        Unit target = GetHitUnit();

        switch (GetSpellInfo().Id)
        {
            case SpellIds.GreatFeast:
                target.CastSpell(target, SpellIds.FeastFood);
                target.CastSpell(target, SpellIds.FeastDrink);
                target.CastSpell(target, SpellIds.GreatFeastRefreshment);
                break;
            case SpellIds.FishFeast:
                target.CastSpell(target, SpellIds.FeastFood);
                target.CastSpell(target, SpellIds.FeastDrink);
                target.CastSpell(target, SpellIds.FishFeastRefreshment);
                break;
            case SpellIds.GiganticFeast:
                target.CastSpell(target, SpellIds.FeastFood);
                target.CastSpell(target, SpellIds.FeastDrink);
                target.CastSpell(target, SpellIds.GiganticFeastRefreshment);
                break;
            case SpellIds.SmallFeast:
                target.CastSpell(target, SpellIds.FeastFood);
                target.CastSpell(target, SpellIds.FeastDrink);
                target.CastSpell(target, SpellIds.SmallFeastRefreshment);
                break;
            case SpellIds.BountifulFeast:
                target.CastSpell(target, SpellIds.BountifulFeastRefreshment);
                target.CastSpell(target, SpellIds.BountifulFeastDrink);
                target.CastSpell(target, SpellIds.BountifulFeastFood);
                break;
            default:
                break;
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_feign_death_all_flags : AuraScript
{
    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.SetUnitFlag3(UnitFlags3.FakeDead);
        target.SetUnitFlag2(UnitFlags2.FeignDeath);
        target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.SetReactState(ReactStates.Passive);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.RemoveUnitFlag3(UnitFlags3.FakeDead);
        target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
        target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.InitializeReactState();
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script]
class spell_gen_feign_death_all_flags_uninteractible : AuraScript
{
    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.SetUnitFlag3(UnitFlags3.FakeDead);
        target.SetUnitFlag2(UnitFlags2.FeignDeath);
        target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
        target.SetImmuneToAll(true);
        target.SetUninteractible(true);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.SetReactState(ReactStates.Passive);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.RemoveUnitFlag3(UnitFlags3.FakeDead);
        target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
        target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
        target.SetImmuneToAll(false);
        target.SetUninteractible(false);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.InitializeReactState();
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 96733 - Permanent Feign Death (Stun)
class spell_gen_feign_death_all_flags_no_uninteractible : AuraScript
{
    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.SetUnitFlag3(UnitFlags3.FakeDead);
        target.SetUnitFlag2(UnitFlags2.FeignDeath);
        target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
        target.SetImmuneToAll(true);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.SetReactState(ReactStates.Passive);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.RemoveUnitFlag3(UnitFlags3.FakeDead);
        target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
        target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
        target.SetImmuneToAll(false);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.InitializeReactState();
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

// 35357 - Spawn Feign Death
[Script] // 51329 - Feign Death
class spell_gen_feign_death_no_dyn_flag : AuraScript
{
    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.SetUnitFlag2(UnitFlags2.FeignDeath);
        target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.SetReactState(ReactStates.Passive);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
        target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.InitializeReactState();
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 58951 - Permanent Feign Death
class spell_gen_feign_death_no_prevent_emotes : AuraScript
{
    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.SetUnitFlag3(UnitFlags3.FakeDead);
        target.SetUnitFlag2(UnitFlags2.FeignDeath);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.SetReactState(ReactStates.Passive);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.RemoveUnitFlag3(UnitFlags3.FakeDead);
        target.RemoveUnitFlag2(UnitFlags2.FeignDeath);

        Creature creature = target.ToCreature();
        if (target != null)
            creature.InitializeReactState();
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 35491 - Furious Rage
class spell_gen_furious_rage : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Exhaustion) &&
            CliDB.BroadcastTextStorage.HasRecord(MiscConst.EmoteFuriousRage) &&
            CliDB.BroadcastTextStorage.HasRecord(MiscConst.EmoteExhausted);
    }

    void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.TextEmote(MiscConst.EmoteFuriousRage, target, false);
    }

    void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        Unit target = GetTarget();
        target.TextEmote(MiscConst.EmoteExhausted, target, false);
        target.CastSpell(target, SpellIds.Exhaustion, true);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(AfterApply, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real));
    }
}

[Script] // 46642 - 5,000 Gold
class spell_gen_5000_gold : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Player target = GetHitPlayer();
        if (target != null)
            target.ModifyMoney(5000 * MoneyConstants.Gold);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 131474 - Fishing
class spell_gen_fishing : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FishingNoFishingPole, SpellIds.FishingWithPole);
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleDummy(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        uint spellId;
        Item mainHand = GetCaster().ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
        if (mainHand == null || mainHand.GetTemplate().GetClass() != ItemClass.Weapon || (ItemSubClassWeapon)mainHand.GetTemplate().GetSubClass() != ItemSubClassWeapon.FishingPole)
            spellId = SpellIds.FishingNoFishingPole;
        else
            spellId = SpellIds.FishingWithPole;

        GetCaster().CastSpell(GetCaster(), spellId, false);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_gen_gadgetzan_transporter_backfire : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TransporterMalfunctionPolymorph, SpellIds.TransporterEvilTwin, SpellIds.TransporterMalfunctionMiss);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();
        int r = RandomHelper.IRand(0, 119);
        if (r < 20)                           // Transporter Malfunction - 1/6 polymorph
            caster.CastSpell(caster, SpellIds.TransporterMalfunctionPolymorph, true);
        else if (r < 100)                     // Evil Twin               - 4/6 evil twin
            caster.CastSpell(caster, SpellIds.TransporterEvilTwin, true);
        else                                    // Transporter Malfunction - 1/6 miss the target
            caster.CastSpell(caster, SpellIds.TransporterMalfunctionMiss, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// 28880 - Warrior
// 59542 - Paladin
// 59543 - Hunter
// 59544 - Priest
// 59545 - Death Knight
// 59547 - Shaman
// 59548 - Mage
[Script] // 121093 - Monk
class spell_gen_gift_of_naaru : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        if (GetCaster() == null || aurEff.GetTotalTicks() == 0)
            return;

        float healPct = GetEffectInfo(1).CalcValue() / 100.0f;
        float heal = healPct * GetCaster().GetMaxHealth();
        int healTick = (int)Math.Floor(heal / aurEff.GetTotalTicks());
        amount += healTick;
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.PeriodicHeal));
    }
}

[Script]
class spell_gen_gnomish_transporter : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.TransporterSuccess, SpellIds.TransporterFailure);
    }

    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetCaster(), RandomHelper.randChance(50) ? SpellIds.TransporterSuccess : SpellIds.TransporterFailure, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 69641 - Gryphon/Wyvern Pet - Mounting Check Aura
class spell_gen_gryphon_wyvern_mount_check : AuraScript
{
    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Unit target = GetTarget();
        Unit owner = target.GetOwner();

        if (owner == null)
            return;

        if (owner.IsMounted())
            target.SetDisableGravity(true);
        else
            target.SetDisableGravity(false);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }
}

/* 9204 - Hate to Zero (Melee)
  20538 - Hate to Zero (AoE)
  26569 - Hate to Zero (AoE)
  26637 - Hate to Zero (AoE, Unique)
  37326 - Hate to Zero (AoE)
  40410 - Hate to Zero (Should be added, AoE)
  40467 - Hate to Zero (Should be added, AoE)
  41582 - Hate to Zero (Should be added, Melee) */
[Script]
class spell_gen_hate_to_zero : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        if (GetCaster().CanHaveThreatList())
            GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -100);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// This spell is used by both player and creature, but currently works only if used by player
[Script] // 63984 - Hate to Zero
class spell_gen_hate_to_zero_caster_target : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (target != null)
            if (target.CanHaveThreatList())
                target.GetThreatManager().ModifyThreatByPercent(GetCaster(), -100);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 19707 - Hate to 50%
class spell_gen_hate_to_50 : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        if (GetCaster().CanHaveThreatList())
            GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -50);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 26886 - Hate to 75%
class spell_gen_hate_to_75 : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        if (GetCaster().CanHaveThreatList())
            GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -25);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

// 32748 - Deadly Throw Interrupt
[Script] // 44835 - Maim Interrupt
class spell_gen_interrupt : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GenThrowInterrupt);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.GenThrowInterrupt, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script("spell_pal_blessing_of_kings")]
[Script("spell_pal_blessing_of_might")]
[Script("spell_dru_mark_of_the_wild")]
[Script("spell_pri_power_word_fortitude")]
[Script("spell_pri_shadow_protection")]
class spell_gen_increase_stats_buff : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        if (GetHitUnit().IsInRaidWith(GetCaster()))
            GetCaster().CastSpell(GetCaster(), (uint)GetEffectValue() + 1, true); // raid buff
        else
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true); // single-target buff
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script("spell_hexlord_lifebloom", SpellIds.HexlordMalacrassLifebloomFinalHeal)]
[Script("spell_tur_ragepaw_lifebloom", SpellIds.TurRagepawLifebloomFinalHeal)]
[Script("spell_cenarion_scout_lifebloom", SpellIds.CenarionScoutLifebloomFinalHeal)]
[Script("spell_twisted_visage_lifebloom", SpellIds.TwistedVisageLifebloomFinalHeal)]
[Script("spell_faction_champion_dru_lifebloom", SpellIds.FactionChampionsDruLifebloomFinalHeal)]
class spell_gen_lifebloom(uint spellId) : AuraScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(spellId);
    }

    void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // final heal only on duration end or dispel
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire && GetTargetApplication().GetRemoveMode() != AuraRemoveMode.EnemySpell)
            return;

        // final heal
        GetTarget().CastSpell(GetTarget(), spellId, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
    }
}

[Script]
class spell_gen_mounted_charge : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(62552, 62719, 64100, 66482);
    }

    void HandleScriptEffect(uint effIndex)
    {
        Unit target = GetHitUnit();

        switch (effIndex)
        {
            case 0: // On spells wich trigger the damaging spell (and also the visual)
            {
                uint spellId;

                switch (GetSpellInfo().Id)
                {
                    case SpellIds.ChargeTriggerTrialChampion:
                        spellId = SpellIds.ChargeCharging20K1;
                        break;
                    case SpellIds.ChargeTriggerFactionMounts:
                        spellId = SpellIds.ChargeChargingEffect8K5;
                        break;
                    default:
                        return;
                }

                // If target isn't a training dummy there's a chance of failing the charge
                if (!target.IsCharmedOwnedByPlayerOrPlayer() && RandomHelper.randChance(12.5f))
                    spellId = SpellIds.ChargeMissEffect;

                Unit vehicle = GetCaster().GetVehicleBase();
                if (vehicle != null)
                    vehicle.CastSpell(target, spellId, false);
                else
                    GetCaster().CastSpell(target, spellId, false);
                break;
            }
            case 1: // On damaging spells, for removing a defend layer
            case 2:
            {
                var auras = target.GetAppliedAuras();
                foreach (var itr in auras)
                {
                    Aura aura = itr.Value.GetBase();
                    if (aura != null)
                    {
                        if (aura.GetId() == 62552 || aura.GetId() == 62719 || aura.GetId() == 64100 || aura.GetId() == 66482)
                        {
                            aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                            // Remove dummys from rider (Necessary for updating visual shields)
                            Unit rider = target.GetCharmer();
                            if (rider != null)
                            {
                                Aura defend = rider.GetAura(aura.GetId());
                                if (defend != null)
                                    defend.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                            }
                            break;
                        }
                    }
                }
                break;
            }
            default:
                break;
        }
    }

    void HandleChargeEffect(uint effIndex)
    {
        uint spellId;

        switch (GetSpellInfo().Id)
        {
            case SpellIds.ChargeChargingEffect8K5:
                spellId = SpellIds.ChargeDamage8K5;
                break;
            case SpellIds.ChargeCharging20K1:
            case SpellIds.ChargeCharging20K2:
                spellId = SpellIds.ChargeDamage20K;
                break;
            case SpellIds.ChargeChargingEffect45K1:
            case SpellIds.ChargeChargingEffect45K2:
                spellId = SpellIds.ChargeDamage45K;
                break;
            default:
                return;
        }

        Unit rider = GetCaster().GetCharmer();
        if (rider != null)
            rider.CastSpell(GetHitUnit(), spellId, false);
        else
            GetCaster().CastSpell(GetHitUnit(), spellId, false);
    }

    public override void Register()
    {
        SpellInfo spell = Global.SpellMgr.GetSpellInfo(m_scriptSpellId, Difficulty.None);

        if (spell.HasEffect(SpellEffectName.ScriptEffect))
            OnEffectHitTarget.Add(new(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect));

        if (spell.GetEffect(0).IsEffect(SpellEffectName.Charge))
            OnEffectHitTarget.Add(new(HandleChargeEffect, 0, SpellEffectName.Charge));
    }
}

// 6870 Moss Covered Feet
[Script] // 31399 Moss Covered Feet
class spell_gen_moss_covered_feet : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FallDown);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        eventInfo.GetActionTarget().CastSpell(null, SpellIds.FallDown, aurEff);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script] // 28702 - Netherbloom
class spell_gen_netherbloom : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        for (byte i = 0; i < 5; ++i)
            if (!ValidateSpellInfo(SpellIds.NetherbloomPollen1 + i))
                return false;

        return true;
    }

    void HandleScript(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        Unit target = GetHitUnit();
        if (target != null)
        {
            // 25% chance of casting a random buff
            if (RandomHelper.randChance(75))
                return;

            // triggered spells are 28703 to 28707
            // Note: some sources say, that there was the possibility of
            //       receiving a debuff. However, this seems to be removed by a patch.

            // don't overwrite an existing aura
            for (byte i = 0; i < 5; ++i)
                if (target.HasAura(SpellIds.NetherbloomPollen1 + i))
                    return;

            target.CastSpell(target, SpellIds.NetherbloomPollen1 + RandomHelper.URand(0, 4), true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 28720 - Nightmare Vine
class spell_gen_nightmare_vine : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.NightmarePollen);
    }

    void HandleScript(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        Unit target = GetHitUnit();
        if (target != null)
        {
            // 25% chance of casting Nightmare Pollen
            if (RandomHelper.randChance(25))
                target.CastSpell(target, SpellIds.NightmarePollen, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 27746 -  Nitrous Boost
class spell_gen_nitrous_boost : AuraScript
{
    void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();

        if (GetCaster() != null && GetTarget().GetPower(PowerType.Mana) >= 10)
            GetTarget().ModifyPower(PowerType.Mana, -10);
        else
            Remove();
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
    }
}

[Script] // 27539 - Obsidian Armor
class spell_gen_obsidian_armor : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GenObsidianArmorHoly, SpellIds.GenObsidianArmorFire, SpellIds.GenObsidianArmorNature, SpellIds.GenObsidianArmorFrost, SpellIds.GenObsidianArmorShadow, SpellIds.GenObsidianArmorArcane);
    }

    bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.GetSpellInfo() == null)
            return false;

        if (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()) == SpellSchools.Normal)
            return false;

        return true;
    }

    void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId = 0;
        switch (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()))
        {
            case SpellSchools.Holy:
                spellId = SpellIds.GenObsidianArmorHoly;
                break;
            case SpellSchools.Fire:
                spellId = SpellIds.GenObsidianArmorFire;
                break;
            case SpellSchools.Nature:
                spellId = SpellIds.GenObsidianArmorNature;
                break;
            case SpellSchools.Frost:
                spellId = SpellIds.GenObsidianArmorFrost;
                break;
            case SpellSchools.Shadow:
                spellId = SpellIds.GenObsidianArmorShadow;
                break;
            case SpellSchools.Arcane:
                spellId = SpellIds.GenObsidianArmorArcane;
                break;
            default:
                return;
        }
        GetTarget().CastSpell(GetTarget(), spellId, aurEff);
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
        OnEffectProc.Add(new(OnProc, 0, AuraType.Dummy));
    }
}

[Script]
class spell_gen_oracle_wolvar_reputation : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleDummy(uint effIndex)
    {
        Player player = GetCaster().ToPlayer();
        uint factionId = (uint)GetEffectInfo().CalcValue();
        int repChange = GetEffectInfo(1).CalcValue();

        var factionEntry = CliDB.FactionStorage.LookupByKey(factionId);
        if (factionEntry == null)
            return;

        // Set rep to baserep + basepoints (expecting spillover for oposite faction . become hated)
        // Not when player already has equal or higher rep with this faction
        if (player.GetReputationMgr().GetReputation(factionEntry) < repChange)
            player.GetReputationMgr().SetReputation(factionEntry, repChange);

        // EffectIndex2 most likely update at war state, we already handle this in SetReputation
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_gen_orc_disguise : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.OrcDisguiseTrigger, SpellIds.OrcDisguiseMale, SpellIds.OrcDisguiseFemale);
    }

    void HandleScript(uint effIndex)
    {
        Unit caster = GetCaster();
        Player target = GetHitPlayer();
        if (target != null)
        {
            var gender = target.GetNativeGender();
            if (gender == 0)
                caster.CastSpell(target, SpellIds.OrcDisguiseMale, true);
            else
                caster.CastSpell(target, SpellIds.OrcDisguiseFemale, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 35201 - Paralytic Poison
class spell_gen_paralytic_poison : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Paralysis);
    }

    void HandleStun(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        GetTarget().CastSpell(null, SpellIds.Paralysis, aurEff);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(HandleStun, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
    }
}

[Script]
class spell_gen_prevent_emotes : AuraScript
{
    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleEffectApply, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real));
        OnEffectRemove.Add(new(OnRemove, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real));
    }
}

[Script]
class spell_gen_player_say : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return CliDB.BroadcastTextStorage.HasRecord((uint)spellInfo.GetEffect(0).CalcValue());
    }

    void HandleScript(uint effIndex)
    {
        // Note: target here is always player; caster here is gameobject, creature or player (self cast)
        Unit target = GetHitUnit();
        if (target != null)
            target.Say((uint)GetEffectValue(), target);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script("spell_item_soul_harvesters_charm")]
[Script("spell_item_commendation_of_kaelthas")]
[Script("spell_item_corpse_tongue_coin")]
[Script("spell_item_corpse_tongue_coin_heroic")]
[Script("spell_item_petrified_twilight_scale")]
[Script("spell_item_petrified_twilight_scale_heroic")]
class spell_gen_proc_below_pct_damaged : AuraScript
{
    bool CheckProc(ProcEventInfo eventInfo)
    {
        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo == null || damageInfo.GetDamage() == 0)
            return false;

        int pct = GetSpellInfo().GetEffect(0).CalcValue();

        if (eventInfo.GetActionTarget().HealthBelowPctDamaged(pct, damageInfo.GetDamage()))
            return true;

        return false;
    }

    public override void Register()
    {
        DoCheckProc.Add(new(CheckProc));
    }
}

[Script]
class spell_gen_proc_charge_drop_only : AuraScript
{
    void HandleChargeDrop(ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
    }

    public override void Register()
    {
        OnProc.Add(new(HandleChargeDrop));
    }
}

[Script] // 45472 Parachute
class spell_gen_parachute : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Parachute, SpellIds.ParachuteBuff);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Player target = GetTarget().ToPlayer();
        if (target != null && target.IsFalling())
        {
            target.RemoveAurasDueToSpell(SpellIds.Parachute);
            target.CastSpell(target, SpellIds.ParachuteBuff, true);
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }
}

[Script]
class spell_gen_pet_summoned : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    void HandleScript(uint effIndex)
    {
        Player player = GetCaster().ToPlayer();
        if (player.GetLastPetNumber() != 0)
        {
            PetType newPetType = (player.GetClass() == Class.Hunter) ? PetType.Hunter : PetType.Summon;
            Pet newPet = new Pet(player, newPetType);
            if (newPet.LoadPetFromDB(player, 0, player.GetLastPetNumber(), true))
            {
                // revive the pet if it is dead
                if (newPet.GetDeathState() != DeathState.Alive && newPet.GetDeathState() != DeathState.JustRespawned)
                    newPet.SetDeathState(DeathState.JustRespawned);

                newPet.SetFullHealth();
                newPet.SetFullPower(newPet.GetPowerType());

                switch (newPet.GetEntry())
                {
                    case CreatureIds.Doomguard:
                    case CreatureIds.Infernal:
                        newPet.SetEntry(CreatureIds.Imp);
                        break;
                    default:
                        break;
                }
            }
            else
                newPet.Dispose();
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 36553 - PetWait
class spell_gen_pet_wait : SpellScript
{
    void HandleScript(uint effIndex)
    {
        GetCaster().GetMotionMaster().Clear();
        GetCaster().GetMotionMaster().MoveIdle();
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_profession_research : SpellScript
{
    public override bool Load()
    {
        return GetCaster().IsPlayer();
    }

    SpellCastResult CheckRequirement()
    {
        Player player = GetCaster().ToPlayer();

        if (SkillDiscovery.HasDiscoveredAllSpells(GetSpellInfo().Id, player))
        {
            SetCustomCastResultMessage(SpellCustomErrors.NothingToDiscover);
            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }

    void HandleScript(uint effIndex)
    {
        Player caster = GetCaster().ToPlayer();
        uint spellId = GetSpellInfo().Id;

        // Learn random explicit discovery recipe (if any)
        // Players will now learn 3 recipes the very first time they perform Northrend Inscription Research (3.3.0 patch notes)
        if (spellId == SpellIds.NorthrendInscriptionResearch && !SkillDiscovery.HasDiscoveredAnySpell(spellId, caster))
        {
            for (int i = 0; i < 2; ++i)
            {
                uint discoveredSpellId1 = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);
                if (discoveredSpellId1 != 0)
                    caster.LearnSpell(discoveredSpellId1, false);
            }
        }

        uint discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);
        if (discoveredSpellId != 0)
            caster.LearnSpell(discoveredSpellId, false);
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckRequirement));
        OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_pvp_trinket : SpellScript
{
    void TriggerAnimation()
    {
        Player caster = GetCaster().ToPlayer();

        switch (caster.GetEffectiveTeam())
        {
            case Team.Alliance:
                caster.CastSpell(caster, SpellIds.PvpTrinketAlliance, TriggerCastFlags.FullMask);
                break;
            case Team.Horde:
                caster.CastSpell(caster, SpellIds.PvpTrinketHorde, TriggerCastFlags.FullMask);
                break;
            default:
                break;
        }
    }

    public override void Register()
    {
        AfterCast.Add(new(TriggerAnimation));
    }
}

[Script]
class spell_gen_remove_flight_auras : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (target != null)
        {
            target.RemoveAurasByType(AuraType.Fly);
            target.RemoveAurasByType(AuraType.ModIncreaseMountedFlightSpeed);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
    }
}

[Script] // 20589 - Escape artist
class spell_gen_remove_impairing_auras : SpellScript
{
    void HandleScriptEffect(uint effIndex)
    {
        GetHitUnit().RemoveMovementImpairingAuras(true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

// 23493 - Restoration
[Script] // 24379 - Restoration
class spell_gen_restoration : AuraScript
{
    void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();

        Unit target = GetTarget();
        if (target == null)
            return;

        uint heal = (uint)target.CountPctFromMaxHealth(10);
        HealInfo healInfo = new(target, target, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
        target.HealBySpell(healInfo);

        /// @todo: should proc other auras?
        int mana = target.GetMaxPower(PowerType.Mana);
        if (mana != 0)
        {
            mana /= 10;
            target.EnergizeBySpell(target, GetSpellInfo(), mana, PowerType.Mana);
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }
}

// 38772 Grievous Wound
// 43937 Grievous Wound
// 62331 Impale
[Script] // 62418 Impale
class spell_gen_remove_on_health_pct : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 1));
    }

    void PeriodicTick(AuraEffect aurEff)
    {
        // they apply damage so no need to check for ticks here

        if (GetTarget().HealthAbovePct(GetEffectInfo(1).CalcValue()))
        {
            Remove(AuraRemoveMode.EnemySpell);
            PreventDefaultAction();
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicDamage));
    }
}

// 31956 Grievous Wound
// 38801 Grievous Wound
// 43093 Grievous Throw
// 58517 Grievous Wound
[Script] // 59262 Grievous Wound
class spell_gen_remove_on_full_health : AuraScript
{
    void PeriodicTick(AuraEffect aurEff)
    {
        // if it has only periodic effect, allow 1 tick
        bool onlyEffect = GetSpellInfo().GetEffects().Count == 1;
        if (onlyEffect && aurEff.GetTickNumber() <= 1)
            return;

        if (GetTarget().IsFullHealth())
        {
            Remove(AuraRemoveMode.EnemySpell);
            PreventDefaultAction();
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 0, AuraType.PeriodicDamage));
    }
}

// 70292 - Glacial Strike
[Script] // 71316 - Glacial Strike
class spell_gen_remove_on_full_health_pct : AuraScript
{
    void PeriodicTick(AuraEffect aurEff)
    {
        // they apply damage so no need to check for ticks here

        if (GetTarget().IsFullHealth())
        {
            Remove(AuraRemoveMode.EnemySpell);
            PreventDefaultAction();
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(PeriodicTick, 2, AuraType.PeriodicDamagePercent));
    }
}

class ReplenishmentCheck
{
    public bool Invoke(WorldObject obj)
    {
        Unit target = obj.ToUnit();
        if (target != null)
            return target.GetPowerType() != PowerType.Mana;

        return true;
    }
}

[Script]
class spell_gen_replenishment : SpellScript
{
    void RemoveInvalidTargets(List<WorldObject> targets)
    {
        // In arenas Replenishment may only affect the caster
        Player caster = GetCaster().ToPlayer();
        if (caster != null)
        {
            if (caster.InArena())
            {
                targets.Clear();
                targets.Add(caster);
                return;
            }
        }

        targets.RemoveAll(new ReplenishmentCheck().Invoke);

        byte maxTargets = 10;

        if (targets.Count > maxTargets)
        {
            targets.Sort(new PowerPctOrderPred(PowerType.Mana));
            targets.Resize(maxTargets);
        }
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
    }
}

[Script]
class spell_gen_replenishment_aura : AuraScript
{
    public override bool Load()
    {
        return GetUnitOwner().GetPowerType() == PowerType.Mana;
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        switch (GetSpellInfo().Id)
        {
            case SpellIds.Replenishment:
                amount = (int)(GetUnitOwner().GetMaxPower(PowerType.Mana) * 0.002f);
                break;
            case SpellIds.InfiniteReplenishment:
                amount = (int)(GetUnitOwner().GetMaxPower(PowerType.Mana) * 0.0025f);
                break;
            default:
                break;
        }
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.PeriodicEnergize));
    }
}

[Script]
class spell_gen_running_wild : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.AlteredForm);
    }

    public override void OnPrecast()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.AlteredForm, TriggerCastFlags.FullMask);
    }

    public override void Register()
    {
    }
}

[Script]
class spell_gen_running_wild_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(SharedConst.DisplayIdHiddenMount))
            return false;
        return true;
    }

    void HandleMount(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        PreventDefaultAction();

        target.Mount(SharedConst.DisplayIdHiddenMount, 0, 0);

        // cast speed aura
        var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(aurEff.GetAmount());
        if (mountCapability != null)
            target.CastSpell(target, mountCapability.ModSpellAuraID, TriggerCastFlags.FullMask);
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleMount, 1, AuraType.Mounted, AuraEffectHandleModes.Real));
    }
}

[Script]
class spell_gen_two_forms : SpellScript
{
    SpellCastResult CheckCast()
    {
        if (GetCaster().IsInCombat())
        {
            SetCustomCastResultMessage(SpellCustomErrors.CantTransform);
            return SpellCastResult.CustomError;
        }

        // Player cannot transform to human form if he is forced to be worgen for some reason (Darkflight)
        var alteredFormAuras = GetCaster().GetAuraEffectsByType(AuraType.WorgenAlteredForm);
        if (alteredFormAuras.Count > 1)
        {
            SetCustomCastResultMessage(SpellCustomErrors.CantTransform);
            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }

    void HandleTransform(uint effIndex)
    {
        Unit target = GetHitUnit();
        PreventHitDefaultEffect(effIndex);
        if (target.HasAuraType(AuraType.WorgenAlteredForm))
            target.RemoveAurasByType(AuraType.WorgenAlteredForm);
        else    // Basepoints 1 for this aura control whether to trigger transform transition animation or not.
            target.CastSpell(target, SpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 1));
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
        OnEffectHitTarget.Add(new(HandleTransform, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_gen_darkflight : SpellScript
{
    void TriggerTransform()
    {
        GetCaster().CastSpell(GetCaster(), SpellIds.AlteredForm, TriggerCastFlags.FullMask);
    }

    public override void Register()
    {
        AfterCast.Add(new(TriggerTransform));
    }
}

[Script]
class spell_gen_seaforium_blast : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.PlantChargesCreditAchievement);
    }

    public override bool Load()
    {
        return GetGObjCaster().GetOwnerGUID().IsPlayer();
    }

    void AchievementCredit(uint effIndex)
    {
        Unit owner = GetGObjCaster().GetOwner();
        if (owner != null)
        {
            GameObject go = GetHitGObj();
            if (go != null && go.GetGoInfo().type == GameObjectTypes.DestructibleBuilding)
                owner.CastSpell(null, SpellIds.PlantChargesCreditAchievement, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(AchievementCredit, 1, SpellEffectName.GameObjectDamage));
    }
}

[Script]
class spell_gen_spectator_cheer_trigger : SpellScript
{
    static Emote[] EmoteArray = [Emote.OneshotCheer, Emote.OneshotExclamation, Emote.OneshotApplaud];

    void HandleDummy(uint effIndex)
    {
        if (RandomHelper.randChance(40))
            GetCaster().HandleEmoteCommand(EmoteArray.SelectRandom());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_gen_spirit_healer_res : SpellScript
{
    public override bool Load()
    {
        return GetOriginalCaster() != null && GetOriginalCaster().IsPlayer();
    }

    void HandleDummy(uint effIndex)
    {
        Player originalCaster = GetOriginalCaster().ToPlayer();
        Unit target = GetHitUnit();
        if (target != null)
        {
            NPCInteractionOpenResult spiritHealerConfirm = new();
            spiritHealerConfirm.Npc = target.GetGUID();
            spiritHealerConfirm.InteractionType = PlayerInteractionType.SpiritHealer;
            spiritHealerConfirm.Success = true;
            originalCaster.SendPacket(spiritHealerConfirm);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_gen_summon_tournament_mount : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.LanceEquipped);
    }

    SpellCastResult CheckIfLanceEquiped()
    {
        if (GetCaster().IsInDisallowedMountForm())
            GetCaster().RemoveAurasByType(AuraType.ModShapeshift);

        if (!GetCaster().HasAura(SpellIds.LanceEquipped))
        {
            SetCustomCastResultMessage(SpellCustomErrors.MustHaveLanceEquipped);
            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckIfLanceEquiped));
    }
}

[Script] // 41213, 43416, 69222, 73076 - Throw Shield
class spell_gen_throw_shield : SpellScript
{
    void HandleScriptEffect(uint effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_tournament_duel : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.OnTournamentMount, SpellIds.MountedDuel);
    }

    void HandleScriptEffect(uint effIndex)
    {
        Unit rider = GetCaster().GetCharmer();
        if (rider != null)
        {
            Player playerTarget = GetHitPlayer();
            if (playerTarget != null && playerTarget.HasAura(SpellIds.OnTournamentMount) && playerTarget.GetVehicleBase() != null)
                rider.CastSpell(playerTarget, SpellIds.MountedDuel, true);
            else
            {
                Unit unitTarget = GetHitUnit();
                if (unitTarget != null && unitTarget.GetCharmer() != null && unitTarget.GetCharmer().IsPlayer() && unitTarget.GetCharmer().HasAura(SpellIds.OnTournamentMount))
                    rider.CastSpell(unitTarget.GetCharmer(), SpellIds.MountedDuel, true);
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_tournament_pennant : AuraScript
{
    public override bool Load()
    {
        return GetCaster() != null && GetCaster().IsPlayer();
    }

    void HandleApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit caster = GetCaster();
        if (caster != null && caster.GetVehicleBase() == null)
            caster.RemoveAurasDueToSpell(GetId());
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleApplyEffect, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
    }
}

[Script]
class spell_gen_teleporting : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (!target.IsPlayer())
            return;

        // return from top
        if (target.ToPlayer().GetAreaId() == MiscConst.AreaVioletCitadelSpire)
            target.CastSpell(target, SpellIds.TeleportSpireDown, true);
        // teleport atop
        else
            target.CastSpell(target, SpellIds.TeleportSpireUp, true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_trigger_exclude_caster_aura_spell : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(spellInfo.ExcludeCasterAuraSpell);
    }

    void HandleTrigger()
    {
        // Blizz seems to just apply aura without bothering to cast
        GetCaster().AddAura(GetSpellInfo().ExcludeCasterAuraSpell, GetCaster());
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleTrigger));
    }
}

[Script]
class spell_gen_trigger_exclude_target_aura_spell : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(spellInfo.ExcludeTargetAuraSpell);
    }

    void HandleTrigger()
    {
        Unit target = GetHitUnit();
        if (target != null)
            // Blizz seems to just apply aura without bothering to cast
            GetCaster().AddAura(GetSpellInfo().ExcludeTargetAuraSpell, target);
    }

    public override void Register()
    {
        AfterHit.Add(new(HandleTrigger));
    }
}

[Script("spell_pvp_trinket_shared_cd", SpellIds.WillOfTheForsakenCooldownTrigger)]
[Script("spell_wotf_shared_cd", SpellIds.WillOfTheForsakenCooldownTriggerWotf)]
class spell_pvp_trinket_wotf_shared_cd(uint triggeredSpellId) : SpellScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(triggeredSpellId);
    }

    void HandleScript()
    {
        /*
            * @workaround: PendingCast flag normally means 'triggered' spell, however
            * if the spell is cast triggered, the core won't send SmsgSpellGo packet
            * so client never registers the cooldown (see Spell::IsNeedSendToClient)
            *
            * ServerToClient: SmsgSpellGo (0x0132) Length: 42 ConnIdx: 0 Time: 07/19/2010 02:32:35.000 Number: 362675
            * Caster Guid: Full: Player
            * Caster Unit Guid: Full: Player
            * Cast Count: 0
            * Spell Id: 72752 (72752)
            * Cast Flags: PendingCast, Unknown3, Unknown7 (265)
            * Time: 3901468825
            * Hit Count: 1
            * [0] Hit Guid: Player
            * Miss Count: 0
            * Target Flags: Unit (2)
            * Target Guid: 0x0
        */

        // Spell flags need further research, until then just cast not triggered
        GetCaster().CastSpell(null, triggeredSpellId, false);
    }

    public override void Register()
    {
        AfterCast.Add(new(HandleScript));
    }
}

[Script]
class spell_gen_turkey_marker : AuraScript
{
    List<uint> _applyTimes = new();

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // store stack apply times, so we can pop them while they expire
        _applyTimes.Add(GameTime.GetGameTimeMS());
        Unit target = GetTarget();

        // on stack 15 cast the achievement crediting spell
        if (GetStackAmount() >= 15)
            target.CastSpell(target, SpellIds.TurkeyVengeance, new CastSpellExtraArgs(aurEff)
                .SetOriginalCaster(GetCasterGUID()));
    }

    void OnPeriodic(AuraEffect aurEff)
    {
        int removeCount = 0;

        // pop expired times off of the stack
        while (!_applyTimes.Empty() && _applyTimes.First() + GetMaxDuration() < GameTime.GetGameTimeMS())
        {
            _applyTimes.RemoveAt(0);
            removeCount++;
        }

        if (removeCount != 0)
            ModStackAmount(-removeCount, AuraRemoveMode.Expire);
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
        OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicDummy));
    }
}

[Script]
class spell_gen_upper_deck_create_foam_sword : SpellScript
{
    static uint[] itemId = [MiscConst.ItemFoamSwordGreen, MiscConst.ItemFoamSwordPink, MiscConst.ItemFoamSwordBlue, MiscConst.ItemFoamSwordRed, MiscConst.ItemFoamSwordYellow];

    void HandleScript(uint effIndex)
    {
        Player player = GetHitPlayer();
        if (player != null)
        {
            // player can only have one of these items
            for (byte i = 0; i < 5; ++i)
            {
                if (player.HasItemCount(itemId[i], 1, true))
                    return;
            }

            CreateItem(itemId[RandomHelper.IRand(0, 4)], ItemContext.None);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

// 52723 - Vampiric Touch
[Script] // 60501 - Vampiric Touch
class spell_gen_vampiric_touch : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.VampiricTouchHeal);
    }

    void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        DamageInfo damageInfo = eventInfo.GetDamageInfo();
        if (damageInfo == null || damageInfo.GetDamage() == 0)
            return;

        Unit caster = eventInfo.GetActor();
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)(damageInfo.GetDamage() / 2));
        caster.CastSpell(caster, SpellIds.VampiricTouchHeal, args);
    }

    public override void Register()
    {
        OnEffectProc.Add(new(HandleProc, 0, AuraType.Dummy));
    }
}

[Script]
class spell_gen_vehicle_scaling : AuraScript
{
    public override bool Load()
    {
        return GetCaster() != null && GetCaster().IsPlayer();
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Unit caster = GetCaster();
        float factor;
        ushort baseItemLevel;

        /// @todo Reserach coeffs for different vehicles
        switch (GetId())
        {
            case SpellIds.GearScaling:
                factor = 1.0f;
                baseItemLevel = 205;
                break;
            default:
                factor = 1.0f;
                baseItemLevel = 170;
                break;
        }

        float avgILvl = caster.ToPlayer().GetAverageItemLevel();
        if (avgILvl < baseItemLevel)
            return;                     /// @todo Research possibility of scaling down

        amount = (ushort)((avgILvl - baseItemLevel) * factor);
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModHealingPct));
        DoEffectCalcAmount.Add(new(CalculateAmount, 1, AuraType.ModDamagePercentDone));
        DoEffectCalcAmount.Add(new(CalculateAmount, 2, AuraType.ModIncreaseHealthPercent));
    }
}

[Script]
class spell_gen_vendor_bark_trigger : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        Creature vendor = GetCaster().ToCreature();
        if (vendor != null && vendor.GetEntry() == CreatureIds.AmphitheaterVendor)
            vendor.GetAI().Talk(MiscConst.SayAmphitheaterVendor);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class spell_gen_wg_water : SpellScript
{
    SpellCastResult CheckCast()
    {
        if (!GetSpellInfo().CheckTargetCreatureType(GetCaster()))
            return SpellCastResult.DontReport;
        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        OnCheckCast.Add(new(CheckCast));
    }
}

[Script]
class spell_gen_whisper_gulch_yogg_saron_whisper : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.YoggSaronWhisperDummy);
    }

    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        PreventDefaultAction();
        GetTarget().CastSpell(null, SpellIds.YoggSaronWhisperDummy, true);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }
}

[Script]
class spell_gen_whisper_to_controller : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return CliDB.BroadcastTextStorage.HasRecord((uint)spellInfo.GetEffect(0).CalcValue());
    }

    void HandleScript(uint effIndex)
    {
        TempSummon casterSummon = GetCaster().ToTempSummon();
        if (casterSummon != null)
        {
            Player target = casterSummon.GetSummonerUnit().ToPlayer();
            if (target != null)
                casterSummon.Whisper((uint)GetEffectValue(), target, false);
        }
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

// BasePoints of spells is Id of npc_text used to group texts, it's not implemented so texts are grouped the old way
// 50037 - Mystery of the Infinite: Future You's Whisper to Controller - Random
// 50287 - Azure Dragon: On Death Force Cast Wyrmrest Defender to Whisper to Controller - Random
// 60709 - Moti, Redux: Past You's Whisper to Controller - Random
[Script("spell_future_you_whisper_to_controller_random", MiscConst.WhisperFutureYou)]
[Script("spell_wyrmrest_defender_whisper_to_controller_random", MiscConst.WhisperDefender)]
[Script("spell_past_you_whisper_to_controller_random", MiscConst.WhisperPastYou)]
class spell_gen_whisper_to_controller_random(uint text) : SpellScript
{
    void HandleScript(uint effIndex)
    {
        // Same for all spells
        if (!RandomHelper.randChance(20))
            return;

        Creature target = GetHitCreature();
        if (target != null)
        {
            TempSummon targetSummon = target.ToTempSummon();
            if (targetSummon != null)
            {
                Player player = targetSummon.GetSummonerUnit().ToPlayer();
                if (player != null)
                    targetSummon.GetAI().Talk(text, player);
            }
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_eject_all_passengers : SpellScript
{
    void RemoveVehicleAuras()
    {
        Vehicle vehicle = GetHitUnit().GetVehicleKit();
        if (vehicle != null)
            vehicle.RemoveAllPassengers();
    }

    public override void Register()
    {
        AfterHit.Add(new(RemoveVehicleAuras));
    }
}

[Script]
class spell_gen_eject_passenger : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        if (!ValidateSpellEffect((spellInfo.Id, 0)))
            return false;
        if (spellInfo.GetEffect(0).CalcValue() < 1)
            return false;
        return true;
    }

    void EjectPassenger(uint effIndex)
    {
        Vehicle vehicle = GetHitUnit().GetVehicleKit();
        if (vehicle != null)
        {
            Unit passenger = vehicle.GetPassenger((sbyte)(GetEffectValue() - 1));
            if (passenger != null)
                passenger.ExitVehicle();
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(EjectPassenger, 0, SpellEffectName.ScriptEffect));
    }
}

[Script("spell_gen_eject_passenger_1", 0)]
[Script("spell_gen_eject_passenger_3", 2)]
class spell_gen_eject_passenger_with_seatId(sbyte seatId) : SpellScript()
{
    void EjectPassenger(uint effIndex)
    {
        Vehicle vehicle = GetHitUnit().GetVehicleKit();
        if (vehicle != null)
        {
            Unit passenger = vehicle.GetPassenger(seatId);
            if (passenger != null)
                passenger.ExitVehicle();
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(EjectPassenger, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_gm_freeze : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.GmFreeze);
    }

    void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Do what was done before to the target in HandleFreezeCommand
        Player player = GetTarget().ToPlayer();
        if (player != null)
        {
            // stop combat + make player unattackable + duel stop + stop some spells
            player.SetFaction(FactionTemplates.Friendly);
            player.CombatStop();
            if (player.IsNonMeleeSpellCast(true))
                player.InterruptNonMeleeSpells(true);
            player.SetUnitFlag(UnitFlags.NonAttackable);

            // if player class = hunter || warlock remove pet if alive
            if ((player.GetClass() == Class.Hunter) || (player.GetClass() == Class.Warlock))
            {
                Pet pet = player.GetPet();
                if (pet != null)
                {
                    pet.SavePetToDB(PetSaveMode.AsCurrent);
                    // not let dismiss dead pet
                    if (pet.IsAlive())
                        player.RemovePet(pet, PetSaveMode.NotInSlot);
                }
            }
        }
    }

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Do what was done before to the target in HandleUnfreezeCommand
        Player player = GetTarget().ToPlayer();
        if (player != null)
        {
            // Reset player faction + allow combat + allow duels
            player.SetFactionForRace(player.GetRace());
            player.RemoveUnitFlag(UnitFlags.NonAttackable);
            // save player
            player.SaveToDB();
        }
    }

    public override void Register()
    {
        OnEffectApply.Add(new(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
    }
}

[Script]
class spell_gen_stand : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Creature target = GetHitCreature();
        if (target == null)
            return;

        target.SetStandState(UnitStandStateType.Stand);
        target.HandleEmoteCommand(Emote.StateNone);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_mixology_bonus : AuraScript
{
    int bonus = 0;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.Mixology) && ValidateSpellEffect((spellInfo.Id, 0));
    }

    public override bool Load()
    {
        return GetCaster() != null && GetCaster().IsPlayer();
    }

    void SetBonusValueForEffect(uint effIndex, int value, AuraEffect aurEff)
    {
        if (aurEff.GetEffIndex() == effIndex)
            bonus = value;
    }

    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        if (GetCaster().HasAura(SpellIds.Mixology) && GetCaster().HasSpell(GetEffectInfo(0).TriggerSpell))
        {
            switch (GetId())
            {
                case SpellIds.WeakTrollsBloodElixir:
                case SpellIds.MagebloodElixir:
                    bonus = amount;
                    break;
                case SpellIds.ElixirOfFrostPower:
                case SpellIds.LesserFlaskOfToughness:
                case SpellIds.LesserFlaskOfResistance:
                    bonus = MathFunctions.CalculatePct(amount, 80);
                    break;
                case SpellIds.ElixirOfMinorDefense:
                case SpellIds.ElixirOfLionsStrength:
                case SpellIds.ElixirOfMinorAgility:
                case SpellIds.MajorTrollsBlloodElixir:
                case SpellIds.ElixirOfShadowPower:
                case SpellIds.ElixirOfBruteForce:
                case SpellIds.MightyTrollsBloodElixir:
                case SpellIds.ElixirOfGreaterFirepower:
                case SpellIds.OnslaughtElixir:
                case SpellIds.EarthenElixir:
                case SpellIds.ElixirOfMajorAgility:
                case SpellIds.FlaskOfTheTitans:
                case SpellIds.FlaskOfRelentlessAssault:
                case SpellIds.FlaskOfStoneblood:
                case SpellIds.ElixirOfMinorAccuracy:
                    bonus = MathFunctions.CalculatePct(amount, 50);
                    break;
                case SpellIds.ElixirOfProtection:
                    bonus = 280;
                    break;
                case SpellIds.ElixirOfMajorDefense:
                    bonus = 200;
                    break;
                case SpellIds.ElixirOfGreaterDefense:
                case SpellIds.ElixirOfSuperiorDefense:
                    bonus = 140;
                    break;
                case SpellIds.ElixirOfFortitude:
                    bonus = 100;
                    break;
                case SpellIds.FlaskOfEndlessRage:
                    bonus = 82;
                    break;
                case SpellIds.ElixirOfDefense:
                    bonus = 70;
                    break;
                case SpellIds.ElixirOfDemonslaying:
                    bonus = 50;
                    break;
                case SpellIds.FlaskOfTheFrostWyrm:
                    bonus = 47;
                    break;
                case SpellIds.WrathElixir:
                    bonus = 32;
                    break;
                case SpellIds.ElixirOfMajorFrostPower:
                case SpellIds.ElixirOfMajorFirepower:
                case SpellIds.ElixirOfMajorShadowPower:
                    bonus = 29;
                    break;
                case SpellIds.ElixirOfMightyToughts:
                    bonus = 27;
                    break;
                case SpellIds.FlaskOfSupremePower:
                case SpellIds.FlaskOfBlindingLight:
                case SpellIds.FlaskOfPureDeath:
                case SpellIds.ShadowpowerElixir:
                    bonus = 23;
                    break;
                case SpellIds.ElixirOfMightyAgility:
                case SpellIds.FlaskOfDistilledWisdom:
                case SpellIds.ElixirOfSpirit:
                case SpellIds.ElixirOfMightyStrength:
                case SpellIds.FlaskOfPureMojo:
                case SpellIds.ElixirOfAccuracy:
                case SpellIds.ElixirOfDeadlyStrikes:
                case SpellIds.ElixirOfMightyDefense:
                case SpellIds.ElixirOfExpertise:
                case SpellIds.ElixirOfArmorPiercing:
                case SpellIds.ElixirOfLightningSpeed:
                    bonus = 20;
                    break;
                case SpellIds.FlaskOfChromaticResistance:
                    bonus = 17;
                    break;
                case SpellIds.ElixirOfMinorFortitude:
                case SpellIds.ElixirOfMajorStrength:
                    bonus = 15;
                    break;
                case SpellIds.FlaskOfMightyRestoration:
                    bonus = 13;
                    break;
                case SpellIds.ArcaneElixir:
                    bonus = 12;
                    break;
                case SpellIds.ElixirOfGreaterAgility:
                case SpellIds.ElixirOfGiants:
                    bonus = 11;
                    break;
                case SpellIds.ElixirOfAgility:
                case SpellIds.ElixirOfGreaterIntellect:
                case SpellIds.ElixirOfSages:
                case SpellIds.ElixirOfIronskin:
                case SpellIds.ElixirOfMightyMageblood:
                    bonus = 10;
                    break;
                case SpellIds.ElixirOfHealingPower:
                    bonus = 9;
                    break;
                case SpellIds.ElixirOfDraenicWisdom:
                case SpellIds.GurusElixir:
                    bonus = 8;
                    break;
                case SpellIds.ElixirOfFirepower:
                case SpellIds.ElixirOfMajorMageblood:
                case SpellIds.ElixirOfMastery:
                    bonus = 6;
                    break;
                case SpellIds.ElixirOfLesserAgility:
                case SpellIds.ElixirOfOgresStrength:
                case SpellIds.ElixirOfWisdom:
                case SpellIds.ElixirOfTheMongoose:
                    bonus = 5;
                    break;
                case SpellIds.StrongTrollsBloodElixir:
                case SpellIds.FlaskOfChromaticWonder:
                    bonus = 4;
                    break;
                case SpellIds.ElixirOfEmpowerment:
                    bonus = -10;
                    break;
                case SpellIds.AdeptsElixir:
                    SetBonusValueForEffect(0, 13, aurEff);
                    SetBonusValueForEffect(1, 13, aurEff);
                    SetBonusValueForEffect(2, 8, aurEff);
                    break;
                case SpellIds.ElixirOfMightyFortitude:
                    SetBonusValueForEffect(0, 160, aurEff);
                    break;
                case SpellIds.ElixirOfMajorFortitude:
                    SetBonusValueForEffect(0, 116, aurEff);
                    SetBonusValueForEffect(1, 6, aurEff);
                    break;
                case SpellIds.FelStrengthElixir:
                    SetBonusValueForEffect(0, 40, aurEff);
                    SetBonusValueForEffect(1, 40, aurEff);
                    break;
                case SpellIds.FlaskOfFortification:
                    SetBonusValueForEffect(0, 210, aurEff);
                    SetBonusValueForEffect(1, 5, aurEff);
                    break;
                case SpellIds.GreaterArcaneElixir:
                    SetBonusValueForEffect(0, 19, aurEff);
                    SetBonusValueForEffect(1, 19, aurEff);
                    SetBonusValueForEffect(2, 5, aurEff);
                    break;
                case SpellIds.ElixirOfGianthGrowth:
                    SetBonusValueForEffect(0, 5, aurEff);
                    break;
                default:
                    Log.outError(LogFilter.Spells, $"SpellId {GetId()} couldn't be processed in spell_gen_mixology_bonus");
                    break;
            }
            amount += bonus;
        }
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, SpellConst.EffectAll, AuraType.Any));
    }
}

[Script]
class spell_gen_landmine_knockback_achievement : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Player target = GetHitPlayer();
        if (target != null)
        {
            Aura aura = GetHitAura();
            if (aura == null || aura.GetStackAmount() < 10)
                return;

            target.CastSpell(target, SpellIds.LandmineKnockbackAchievement, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 34098 - ClearAllDebuffs
class spell_gen_clear_debuffs : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (target != null)
        {
            target.RemoveOwnedAuras(aura =>
            {
                SpellInfo spellInfo = aura.GetSpellInfo();
                return !spellInfo.IsPositive() && !spellInfo.IsPassive();
            });
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script]
class spell_gen_pony_mount_check : AuraScript
{
    void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Unit caster = GetCaster();
        if (caster == null)
            return;

        Player owner = caster.GetOwner().ToPlayer();
        if (owner == null || !owner.HasAchieved(MiscConst.AchievPonyUp))
            return;

        if (owner.IsMounted())
        {
            caster.Mount(MiscConst.MountPony);
            caster.SetSpeedRate(UnitMoveType.Run, owner.GetSpeedRate(UnitMoveType.Run));
        }
        else if (caster.IsMounted())
        {
            caster.Dismount();
            caster.SetSpeedRate(UnitMoveType.Run, owner.GetSpeedRate(UnitMoveType.Run));
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }
}

// 40350 - Corrupting Plague
class CorruptingPlagueSearcher(Unit obj, float distance) : ICheck<Unit>
{
    public bool Invoke(Unit u)
    {
        if (obj.GetDistance2d(u) < distance &&
            (u.GetEntry() == CreatureIds.ApexisFlayer || u.GetEntry() == CreatureIds.ShardHideBoar || u.GetEntry() == CreatureIds.AetherRay) &&
            !u.HasAura(SpellIds.CorruptingPlague))
            return true;

        return false;
    }
}

[Script] // 40349 - Corrupting Plague
class spell_corrupting_plague_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CorruptingPlague);
    }

    void OnPeriodic(AuraEffect aurEff)
    {
        Unit owner = GetTarget();

        List<Creature> targets = new();
        CorruptingPlagueSearcher creature_check = new(owner, 15.0f);
        CreatureListSearcher creature_searcher = new(owner, targets, creature_check);
        Cell.VisitGridObjects(owner, creature_searcher, 15.0f);

        if (!targets.Empty())
            return;

        PreventDefaultAction();
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
    }
}

// 40307 - Stasis Field
class StasisFieldSearcher(Unit obj, float distance) : ICheck<Unit>
{
    public bool Invoke(Unit u)
    {
        if (obj.GetDistance2d(u) < distance &&
            (u.GetEntry() == CreatureIds.ApexisFlayer || u.GetEntry() == CreatureIds.ShardHideBoar || u.GetEntry() == CreatureIds.AetherRay || u.GetEntry() == CreatureIds.DaggertailLizard) &&
            !u.HasAura(SpellIds.StasisField))
            return true;

        return false;
    }
}

[Script] // 40306 - Stasis Field
class spell_stasis_field_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.StasisField);
    }

    void OnPeriodic(AuraEffect aurEff)
    {
        Unit owner = GetTarget();

        List<Creature> targets = new();
        StasisFieldSearcher creature_check = new(owner, 15.0f);
        CreatureListSearcher creature_searcher = new(owner, targets, creature_check);
        Cell.VisitGridObjects(owner, creature_searcher, 15.0f);

        if (!targets.Empty())
            return;

        PreventDefaultAction();
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
    }
}

[Script]
class spell_gen_vehicle_control_link : AuraScript
{
    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(SpellIds.SiegeTankControl); //aurEff.GetAmount()
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 34779 - Freezing Circle
class spell_freezing_circle : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FreezingCirclePitOfSaronNormal, SpellIds.FreezingCirclePitOfSaronHeroic, SpellIds.FreezingCircle, SpellIds.FreezingCircleScenario);
    }

    void HandleDamage(uint effIndex)
    {
        Unit caster = GetCaster();
        uint spellId = 0;
        Map map = caster.GetMap();

        if (map.IsDungeon())
            spellId = map.IsHeroic() ? SpellIds.FreezingCirclePitOfSaronHeroic : SpellIds.FreezingCirclePitOfSaronNormal;
        else
            spellId = map.GetId() == MiscConst.MapIdBloodInTheSnowScenario ? SpellIds.FreezingCircleScenario : SpellIds.FreezingCircle;

        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());
        if (spellInfo != null && !spellInfo.GetEffects().Empty())
            SetHitDamage(spellInfo.GetEffect(0).CalcValue());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDamage, 1, SpellEffectName.SchoolDamage));
    }
}

[Script] // Used for some spells cast by vehicles or charmed creatures that do not send a cooldown event on their own
class spell_gen_charmed_unit_spell_cooldown : SpellScript
{
    void HandleCast()
    {
        Unit caster = GetCaster();
        Player owner = caster.GetCharmerOrOwnerPlayerOrPlayerItself();
        if (owner != null)
        {
            SpellCooldownPkt spellCooldown = new();
            spellCooldown.Caster = owner.GetGUID();
            spellCooldown.Flags = SpellCooldownFlags.None;
            spellCooldown.SpellCooldowns.Add(new SpellCooldownStruct(GetSpellInfo().Id, GetSpellInfo().RecoveryTime));
            owner.SendPacket(spellCooldown);
        }
    }

    public override void Register()
    {
        OnCast.Add(new(HandleCast));
    }
}

[Script]
class spell_gen_cannon_blast : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.CannonBlast);
    }

    void HandleScript(uint effIndex)
    {
        int bp = GetEffectValue();
        Unit target = GetHitUnit();
        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.BasePoint0, bp);
        target.CastSpell(target, SpellIds.CannonBlastDamage, args);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 37751 - Submerged
class spell_gen_submerged : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Creature target = GetHitCreature();
        if (target != null)
            target.SetStandState(UnitStandStateType.Submerged);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 169869 - Transformation Sickness
class spell_gen_decimatus_transformation_sickness : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Unit target = GetHitUnit();
        if (target != null)
            target.SetHealth(target.CountPctFromMaxHealth(25));
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
    }
}

[Script] // 189491 - Summon Towering Infernal.
class spell_gen_anetheron_summon_towering_infernal : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

class MarkTargetHellfireFilter
{
    public bool Invoke(WorldObject target)
    {
        Unit unit = target.ToUnit();
        if (unit != null)
            return unit.GetPowerType() != PowerType.Mana;
        return false;
    }
}

[Script]
class spell_gen_mark_of_kazrogal_hellfire : SpellScript
{
    void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(new MarkTargetHellfireFilter().Invoke);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
    }
}

[Script]
class spell_gen_mark_of_kazrogal_hellfire_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.MarkOfKazrogalDamageHellfire);
    }

    void OnPeriodic(AuraEffect aurEff)
    {
        Unit target = GetTarget();

        if (target.GetPower(PowerType.Mana) == 0)
        {
            target.CastSpell(target, SpellIds.MarkOfKazrogalDamageHellfire, aurEff);
            // Remove aura
            SetDuration(0);
        }
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PowerBurn));
    }
}

[Script]
class spell_gen_azgalor_rain_of_fire_hellfire_citadel : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 99947 - Face Rage
class spell_gen_face_rage : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.FaceRage)
            && ValidateSpellEffect((spellInfo.Id, 2));
    }

    void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
    {
        GetTarget().RemoveAurasDueToSpell(GetEffectInfo(2).TriggerSpell);
    }

    public override void Register()
    {
        OnEffectRemove.Add(new(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
    }
}

[Script] // 187213 - Impatient Mind
class spell_gen_impatient_mind : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ImpatientMind);
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

[Script] // 209352 - Boost 2.0 [Paladin+Priest] - Watch for Shield
class spell_gen_boost_2_0_paladin_priest_watch_for_shield : AuraScript
{
    static uint SpellPowerWordShield = 17;
    static uint SpellDivineShield = 642;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellPowerWordShield, SpellDivineShield);
    }

    bool CheckProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        SpellInfo spellInfo = procInfo.GetSpellInfo();
        return spellInfo != null && (spellInfo.Id == SpellPowerWordShield || spellInfo.Id == SpellDivineShield);
    }

    public override void Register()
    {
        DoCheckEffectProc.Add(new(CheckProc, 0, AuraType.ProcTriggerSpell));
    }
}

// 269083 - Enlisted
[Script] // 282559 - Enlisted
class spell_gen_war_mode_enlisted : AuraScript
{
    void CalcWarModeBonus(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Player target = GetUnitOwner().ToPlayer();
        if (target == null)
            return;

        switch (target.GetTeamId())
        {
            case BattleGroundTeamId.Alliance:
                amount = Global.WorldStateMgr.GetValue(WorldStates.WarModeAllianceBuffValue, target.GetMap());
                break;
            case BattleGroundTeamId.Horde:
                amount = Global.WorldStateMgr.GetValue(WorldStates.WarModeHordeBuffValue, target.GetMap());
                break;
            default:
                break;
        }
    }

    public override void Register()
    {
        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(m_scriptSpellId, Difficulty.None);

        if (spellInfo.HasAura(AuraType.ModXpPct))
            DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpPct));

        if (spellInfo.HasAura(AuraType.ModXpQuestPct))
            DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpQuestPct));

        if (spellInfo.HasAura(AuraType.ModCurrencyGainFromSource))
            DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModCurrencyGainFromSource));

        if (spellInfo.HasAura(AuraType.ModMoneyGain))
            DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModMoneyGain));

        if (spellInfo.HasAura(AuraType.ModAnimaGain))
            DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModAnimaGain));

        if (spellInfo.HasAura(AuraType.Dummy))
            DoEffectCalcAmount.Add(new(CalcWarModeBonus, SpellConst.EffectAll, AuraType.Dummy));
    }
}

[Script]
class spell_defender_of_azeroth_death_gate_selector : SpellScript
{
    BindLocation StormwindInnLoc = new(0, -8868.1f, 675.82f, 97.9f, 5.164778709411621093f, 5148);
    BindLocation OrgrimmarInnLoc = new(1, 1573.18f, -4441.62f, 16.06f, 1.818284034729003906f, 8618);

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.DeathGateTeleportStormwind, SpellIds.DeathGateTeleportOrgrimmar);
    }

    void HandleDummy(uint effIndex)
    {
        Player player = GetHitUnit().ToPlayer();
        if (player == null)
            return;

        if (player.GetQuestStatus(MiscConst.QuestDefenderOfAzerothAlliance) == QuestStatus.None && player.GetQuestStatus(MiscConst.QuestDefenderOfAzerothHorde) == QuestStatus.None)
            return;

        BindLocation bindLoc = player.GetTeam() == Team.Alliance ? StormwindInnLoc : OrgrimmarInnLoc;
        player.SetHomebind(bindLoc.Loc, bindLoc.AreaId);
        player.SendBindPointUpdate();
        player.SendPlayerBound(player.GetGUID(), bindLoc.AreaId);

        player.CastSpell(player, player.GetTeam() == Team.Alliance ? SpellIds.DeathGateTeleportStormwind : SpellIds.DeathGateTeleportOrgrimmar);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }

    struct BindLocation
    {
        public WorldLocation Loc;
        public uint AreaId;

        public BindLocation(uint mapId, float x, float y, float z, float orientation, uint areaId)
        {
            Loc = new(mapId, x, y, z, orientation);
            AreaId = areaId;
        }
    }
}

[Script]
class spell_defender_of_azeroth_speak_with_mograine : SpellScript
{
    void HandleDummy(uint effIndex)
    {
        if (GetCaster() == null)
            return;

        Player player = GetCaster().ToPlayer();
        if (player == null)
            return;
        Creature nazgrim = GetHitUnit().FindNearestCreature(CreatureIds.Nazgrim, 10.0f);
        if (nazgrim != null)
            nazgrim.HandleEmoteCommand(Emote.OneshotPoint, player);
        Creature trollbane = GetHitUnit().FindNearestCreature(CreatureIds.Trollbane, 10.0f);
        if (trollbane != null)
            trollbane.HandleEmoteCommand(Emote.OneshotPoint, player);
        Creature whitemane = GetHitUnit().FindNearestCreature(CreatureIds.Whitemane, 10.0f);
        if (whitemane != null)
            whitemane.HandleEmoteCommand(Emote.OneshotPoint, player);

        // @Todo: spawntracking - show death gate for casting player
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script] // 118301 - Summon Battle Pet
class spell_summon_battle_pet : SpellScript
{
    void HandleSummon(uint effIndex)
    {
        uint creatureId = (uint)GetSpellValue().EffectBasePoints[effIndex];
        if (Global.ObjectMgr.GetCreatureTemplate(creatureId) != null)
        {
            PreventHitDefaultEffect(effIndex);

            Unit caster = GetCaster();
            var properties = CliDB.SummonPropertiesStorage.LookupByKey((uint)GetEffectInfo().MiscValueB);
            TimeSpan duration = TimeSpan.FromSeconds(GetSpellInfo().CalcDuration(caster));
            Position pos = GetHitDest().GetPosition();

            Creature summon = caster.GetMap().SummonCreature(creatureId, pos, properties, duration, caster, GetSpellInfo().Id);
            if (summon != null)
                summon.SetImmuneToAll(true);
        }
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleSummon, 0, SpellEffectName.Summon));
    }
}

[Script] // 132334 - Trainer Heal Cooldown (Serverside)
class spell_gen_trainer_heal_cooldown : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SharedConst.SpellReviveBattlePets);
    }

    public override bool Load()
    {
        return GetUnitOwner().IsPlayer();
    }

    void UpdateReviveBattlePetCooldown(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Player target = GetUnitOwner().ToPlayer();
        SpellInfo reviveBattlePetSpellInfo = Global.SpellMgr.GetSpellInfo(SharedConst.SpellReviveBattlePets, Difficulty.None);

        if (target.GetSession().GetBattlePetMgr().IsBattlePetSystemEnabled())
        {
            TimeSpan expectedCooldown = TimeSpan.FromSeconds(GetAura().GetMaxDuration());
            TimeSpan remainingCooldown = target.GetSpellHistory().GetRemainingCategoryCooldown(reviveBattlePetSpellInfo);
            if (remainingCooldown > TimeSpan.Zero)
            {
                if (remainingCooldown < expectedCooldown)
                    target.GetSpellHistory().ModifyCooldown(reviveBattlePetSpellInfo, expectedCooldown - remainingCooldown);
            }
            else
            {
                target.GetSpellHistory().StartCooldown(reviveBattlePetSpellInfo, 0, null, false, expectedCooldown);
            }
        }
    }

    public override void Register()
    {
        OnEffectApply.Add(new(UpdateReviveBattlePetCooldown, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 45313 - Anchor Here
class spell_gen_anchor_here : SpellScript
{
    void HandleScript(uint effIndex)
    {
        Creature creature = GetHitCreature();
        if (creature != null)
            creature.SetHomePosition(creature.GetPositionX(), creature.GetPositionY(), creature.GetPositionZ(), creature.GetOrientation());
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 147066 - (Serverside/Non-DB2) Generic - Mount Check Aura
class spell_gen_mount_check_aura : AuraScript
{
    void OnPeriodic(AuraEffect aurEff)
    {
        Unit target = GetTarget();
        uint mountDisplayId = 0;

        TempSummon tempSummon = target.ToTempSummon();
        if (tempSummon == null)
            return;

        Player summoner = tempSummon.GetSummoner()?.ToPlayer();
        if (summoner == null)
            return;

        if (summoner.IsMounted() && (!summoner.IsInCombat() || summoner.IsFlying()))
        {
            CreatureSummonedData summonedData = Global.ObjectMgr.GetCreatureSummonedData(tempSummon.GetEntry());
            if (summonedData != null)
            {
                if (summoner.IsFlying() && summonedData.FlyingMountDisplayID.HasValue)
                    mountDisplayId = summonedData.FlyingMountDisplayID.Value;
                else if (summonedData.GroundMountDisplayID.HasValue)
                    mountDisplayId = summonedData.GroundMountDisplayID.Value;
            }
        }

        if (mountDisplayId != target.GetMountDisplayId())
            target.SetMountDisplayId(mountDisplayId);
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 274738 - Ancestral Call (Mag'har Orc Racial)
class spell_gen_ancestral_call : SpellScript
{
    uint[] AncestralCallBuffs = [SpellIds.RictusOfTheLaughingSkull, SpellIds.ZealOfTheBurningBlade, SpellIds.FerocityOfTheFrostwolf, SpellIds.MightOfTheBlackrock];

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.RictusOfTheLaughingSkull, SpellIds.ZealOfTheBurningBlade, SpellIds.FerocityOfTheFrostwolf, SpellIds.MightOfTheBlackrock);
    }

    void HandleOnCast()
    {
        Unit caster = GetCaster();
        uint spellId = AncestralCallBuffs.SelectRandom();

        caster.CastSpell(caster, spellId, true);
    }

    public override void Register()
    {
        OnCast.Add(new(HandleOnCast));
    }
}

[Script] // 83477 - Eject Passengers 3-8
class spell_gen_eject_passengers_3_8 : SpellScript
{
    void HandleScriptEffect(uint effIndex)
    {
        Vehicle vehicle = GetHitUnit().GetVehicleKit();
        if (vehicle == null)
            return;

        for (sbyte i = 2; i < 8; i++)
        {
            Unit passenger = vehicle.GetPassenger(i);
            if (passenger != null)
                passenger.ExitVehicle();
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

// 83781 - Reverse Cast Ride Vehicle
// 85299 - Reverse Cast Ride Seat 1
[Script] // 258344 - Reverse Cast Ride Vehicle
class spell_gen_reverse_cast_target_to_caster_triggered : SpellScript
{
    void HandleScript(uint effIndex)
    {
        GetHitUnit().CastSpell(GetCaster(), (uint)GetSpellInfo().GetEffect(effIndex).CalcValue(), true);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScript, 0, SpellEffectName.ScriptEffect));
    }
}

// Note: this spell unsummons any creature owned by the caster. Set appropriate target conditions on the Db.
// 84065 - Despawn All Summons
// 83935 - Despawn All Summons
[Script] // 160938 - Despawn All Summons (Garrison Intro Only)
class spell_gen_despawn_all_summons_owned_by_caster : SpellScript
{
    void HandleScriptEffect(uint effIndex)
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            Creature target = GetHitCreature();

            if (target.GetOwner() == caster)
                target.DespawnOrUnsummon();
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 8613 - Skinning
class spell_gen_skinning : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.OutlandSkinning, SpellIds.NorthrendSkinning, SpellIds.CataclysmSkinning, SpellIds.PandariaSkinning, SpellIds.DraenorSkinning, SpellIds.KulTiranSkinning, SpellIds.ZandalariSkinning,
            SpellIds.ShadowlandsSkinning, SpellIds.DragonIslesSkinning);
    }

    void HandleSkinningEffect(uint effIndex)
    {
        Player player = GetCaster().ToPlayer();
        if (player == null)
            return;

        var contentTuning = CliDB.ContentTuningStorage.LookupByKey(GetHitUnit().GetContentTuning());
        if (contentTuning == null)
            return;

        SkillType skinningSkill = (SkillType)player.GetProfessionSkillForExp(SkillType.Skinning, contentTuning.ExpansionID);
        if (skinningSkill == 0)
            return;

        // Autolearning missing skinning skill (Dragonflight)
        uint getSkinningLearningSpellBySkill = skinningSkill switch
        {
            SkillType.OutlandSkinning => SpellIds.OutlandSkinning,
            SkillType.NorthrendSkinning => SpellIds.NorthrendSkinning,
            SkillType.CataclysmSkinning => SpellIds.CataclysmSkinning,
            SkillType.PandariaSkinning => SpellIds.PandariaSkinning,
            SkillType.DraenorSkinning => SpellIds.DraenorSkinning,
            SkillType.KulTiranSkinning => player.GetTeam() == Team.Alliance ? SpellIds.KulTiranSkinning : (player.GetTeam() == Team.Horde ? SpellIds.ZandalariSkinning : 0),
            SkillType.ShadowlandsSkinning => SpellIds.ShadowlandsSkinning,
            SkillType.DragonIslesSkinning => SpellIds.DragonIslesSkinning,
            _ => 0,
        };

        if (!player.HasSkill(skinningSkill))
        {
            if (getSkinningLearningSpellBySkill != 0)
                player.CastSpell(null, getSkinningLearningSpellBySkill, true);
        }
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleSkinningEffect, 0, SpellEffectName.Skinning));
    }
}

// 2825 - Bloodlust
// 32182 - Heroism
// 80353 - Time Warp
// 264667 - Primal Rage
// 390386 - Fury of the Aspects
// 146555 - Drums of Rage
// 178207 - Drums of Fury
// 230935 - Drums of the Mountain
// 256740 - Drums of the Maelstrom
// 309658 - Drums of Deathly Ferocity
// 381301 - Feral Hide Drums
[Script("spell_sha_bloodlust", SpellIds.ShamanSated)]
[Script("spell_sha_heroism", SpellIds.ShamanExhaustion)]
[Script("spell_mage_time_warp", SpellIds.MageTemporalDisplacement)]
[Script("spell_hun_primal_rage", SpellIds.HunterFatigued)]
[Script("spell_evo_fury_of_the_aspects", SpellIds.EvokerExhaustion)]
[Script("spell_item_bloodlust_drums", SpellIds.ShamanExhaustion)]
class spell_gen_bloodlust(uint exhaustionSpellId) : SpellScript()
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.ShamanSated, SpellIds.ShamanExhaustion, SpellIds.MageTemporalDisplacement, SpellIds.HunterFatigued, SpellIds.EvokerExhaustion);
    }

    void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(target =>
            {
                Unit unit = target.ToUnit();
                if (unit == null)
                    return true;

                return unit.HasAura(SpellIds.ShamanSated)
                    || unit.HasAura(SpellIds.ShamanExhaustion)
                    || unit.HasAura(SpellIds.MageTemporalDisplacement)
                    || unit.HasAura(SpellIds.HunterFatigued)
                    || unit.HasAura(SpellIds.EvokerExhaustion);
            });
    }

    void HandleHit(uint effIndex)
    {
        Unit target = GetHitUnit();
        target.CastSpell(target, exhaustionSpellId, true);
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitCasterAreaRaid));
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 1, Targets.UnitCasterAreaRaid));
        OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.ApplyAura));
    }
}

// AoE resurrections by spirit guides
[Script] // 22012 - Spirit Heal
class spell_gen_spirit_heal_aoe : SpellScript
{
    void FilterTargets(List<WorldObject> targets)
    {
        Unit caster = GetCaster();
        targets.RemoveAll(target =>
        {
            Player playerTarget = target.ToPlayer();
            if (playerTarget != null)
                return !playerTarget.CanAcceptAreaSpiritHealFrom(caster);

            return true;
        });
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitDestAreaAlly));
    }
}

// Personal resurrections in battlegrounds
[Script] // 156758 - Spirit Heal
class spell_gen_spirit_heal_personal : AuraScript
{
    uint SpellSpiritHealEffect = 156763;

    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        Player targetPlayer = GetTarget().ToPlayer();
        if (targetPlayer == null)
            return;

        Unit caster = GetCaster();
        if (caster == null)
            return;

        if (targetPlayer.CanAcceptAreaSpiritHealFrom(caster))
            caster.CastSpell(targetPlayer, SpellSpiritHealEffect);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 22011 - Spirit Heal Channel
class spell_gen_spirit_heal_channel : AuraScript
{
    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
            return;

        Unit target = GetTarget();
        target.m_Events.AddEventAtOffset(new RecastSpiritHealChannelEvent(target), TimeSpan.FromSeconds(1));
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
    }

    class RecastSpiritHealChannelEvent(Unit caster) : BasicEvent()
    {
        public override bool Execute(ulong e_time, uint p_time)
        {
            if (caster.GetChannelSpellId() == 0)
                caster.CastSpell(null, BattlegroundConst.SpellSpiritHealChannelAoE, false);

            return true;
        }
    }
}

[Script] // 2584 - Waiting to Resurrect
class spell_gen_waiting_to_resurrect : AuraScript
{
    void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Player targetPlayer = GetTarget().ToPlayer();
        if (targetPlayer == null)
            return;

        targetPlayer.SetAreaSpiritHealer(null);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

// 157982 - Tranquility (Heal)
// 64844 - Divine Hymn (Heal)
// 114942 - Healing Tide (Heal)
[Script] // 115310 - Revival (Heal)
class spell_gen_major_healing_cooldown_modifier : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((SpellIds.DruidTranquility, 2), (SpellIds.PriestDivineHymn, 1), (SpellIds.ShamanHealingTideTotem, 2), (SpellIds.MonkRevival, 4));
    }

    void CalculateHealingBonus(SpellEffectInfo spellEffectInfo, Unit victim, ref int healing, ref int flatMod, ref float pctMod)
    {
        MathFunctions.AddPct(ref pctMod, MiscConst.GetBonusMultiplier(GetCaster(), GetSpellInfo().Id));
    }

    public override void Register()
    {
        CalcHealing.Add(new(CalculateHealingBonus));
    }
}

// 157982 - Tranquility (Heal)
// 271466 - Luminous Barrier (Absorb)
[Script] // 363534 - Rewind (Heal)
class spell_gen_major_healing_cooldown_modifier_aura : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((SpellIds.DruidTranquility, 2), (SpellIds.PriestLuminousBarrier, 1), (SpellIds.EvokerRewind, 3));
    }

    void CalculateHealingBonus(AuraEffect aurEff, Unit victim, ref int damageOrHealing, ref int flatMod, ref float pctMod)
    {
        Unit caster = GetCaster();
        if (caster != null)
            MathFunctions.AddPct(ref pctMod, MiscConst.GetBonusMultiplier(caster, GetSpellInfo().Id));
    }

    public override void Register()
    {
        DoEffectCalcDamageAndHealing.Add(new(CalculateHealingBonus, SpellConst.EffectAll, AuraType.Any));
    }
}

[Script] // 50230 - Random Aggro (Taunt)
class spell_gen_random_aggro_taunt : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellEffect((spellInfo.Id, 0)) && ValidateSpellInfo((uint)spellInfo.GetEffect(0).BasePoints);
    }

    void SelectRandomTarget(List<WorldObject> targets)
    {
        if (targets.Empty())
            return;

        targets.RandomResize(1);
    }

    void HandleTauntEffect(uint effIndex)
    {
        GetHitUnit().CastSpell(GetCaster(), (uint)GetSpellInfo().GetEffect(effIndex).BasePoints, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(SelectRandomTarget, 0, Targets.UnitSrcAreaEnemy));
        OnEffectHitTarget.Add(new(HandleTauntEffect, 0, SpellEffectName.ScriptEffect));
    }
}

// 24931 - 100 Health
// 24959 - 500 Health
// 28838 - 1 Health
// 43645 - 1 Health
// 73342 - 1 Health
// 86562 - 1 Health
[Script("spell_gen_set_health_1", 1)]
[Script("spell_gen_set_health_100", 100)]
[Script("spell_gen_set_health_500", 500)]
class spell_gen_set_health(ulong health) : SpellScript
{
    void HandleHit(uint effIndex)
    {
        if (GetHitUnit().IsAlive() && health > 0)
            GetHitUnit().SetHealth(health);
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.ScriptEffect));
    }
}

[Script] // 128648 - Defending Cart Aura
class spell_bg_defending_cart_aura : SpellScript
{
    void FilterTargets(List<WorldObject> targets)
    {
        if (targets.Empty())
            return;

        GameObject controlZone = GetControlZone();
        if (controlZone != null)
        {
            targets.RemoveAll(obj =>
            {
                Player player = obj.ToPlayer();
                if (player != null)
                    return SharedConst.GetTeamIdForTeam(player.GetBGTeam()) != controlZone.GetControllingTeam();

                return true;
            });
        }
    }

    GameObject GetControlZone()
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            var auraEffects = caster.GetAuraEffectsByType(AuraType.ActAsControlZone);
            foreach (AuraEffect auraEffect in auraEffects)
            {
                GameObject gameobject = caster.GetGameObject(auraEffect.GetSpellInfo().Id);
                if (gameobject != null)
                    return gameobject;
            }
        }

        return null;
    }

    public override void Register()
    {
        OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaAlly));
    }
}

[Script] // 128648 - Defending Cart Aura
class spell_bg_defending_cart_aura_AuraScript : AuraScript
{
    void OnPeriodic(AuraEffect aurEff)
    {
        Unit caster = GetCaster();
        if (caster == null)
            return;

        GameObject controlZone = GetControlZone();
        if (controlZone != null && !controlZone.GetInsidePlayers().Contains(GetTarget().GetGUID()))
            GetTarget().RemoveAurasDueToSpell(GetSpellInfo().Id, caster.GetGUID());
    }

    GameObject GetControlZone()
    {
        Unit caster = GetCaster();
        if (caster != null)
        {
            var auraEffects = caster.GetAuraEffectsByType(AuraType.ActAsControlZone);
            foreach (AuraEffect auraEffect in auraEffects)
            {
                GameObject gameobject = caster.GetGameObject(auraEffect.GetSpellInfo().Id);
                if (gameobject != null)
                    return gameobject;
            }
        }

        return null;
    }

    public override void Register()
    {
        OnEffectPeriodic.Add(new(OnPeriodic, 0, AuraType.PeriodicDummy));
    }
}

[Script] // 296837 - Comfortable Rider's Barding
class spell_gen_comfortable_riders_barding : AuraScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(1604);
    }

    void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().ApplySpellImmune(GetId(), SpellImmunity.Id, 1604, true);
    }

    void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        GetTarget().ApplySpellImmune(GetId(), SpellImmunity.Id, 1604, false);
    }

    public override void Register()
    {
        OnEffectApply.Add(new(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        OnEffectRemove.Add(new(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 297091 - Parachute
class spell_gen_saddlechute : AuraScript
{
    static uint SpellParachute = 297092;

    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellParachute);
    }

    void TriggerParachute(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Unit target = GetTarget();
        if (target.IsFlying() || target.IsFalling())
            target.CastSpell(target, SpellParachute, TriggerCastFlags.DontReportCastError);
    }

    public override void Register()
    {
        AfterEffectRemove.Add(new(TriggerParachute, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}

[Script] // 257040 - Spatial Rift
class spell_gen_spatial_rift : SpellScript
{
    public override bool Validate(SpellInfo spellInfo)
    {
        return ValidateSpellInfo(SpellIds.SpatialRiftTeleport, SpellIds.SpatialRiftAreatrigger);
    }

    void HandleDummy(uint effIndex)
    {
        Unit caster = GetCaster();

        AreaTrigger at = caster.GetAreaTrigger(SpellIds.SpatialRiftAreatrigger);
        if (at == null)
            return;

        caster.CastSpell(at.GetPosition(), SpellIds.SpatialRiftTeleport, new CastSpellExtraArgs()
        {
            TriggerFlags = TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.DontReportCastError,
            TriggeringSpell = GetSpell()
        });

        at.SetDuration(0);
    }

    public override void Register()
    {
        OnEffectHit.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
    }
}

[Script]
class at_gen_spatial_rift(AreaTrigger areatrigger) : AreaTriggerAI(areatrigger)
{
    public override void OnInitialize()
    {
        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(at.GetSpellId(), Difficulty.None);
        if (spellInfo == null)
            return;

        Position destPos = at.GetPosition();
        at.MovePositionToFirstCollision(destPos, spellInfo.GetMaxRange(), 0.0f);

        PathGenerator path = new(at);
        path.CalculatePath(destPos.GetPositionX(), destPos.GetPositionY(), destPos.GetPositionZ(), true);

        at.InitSplines(path.GetPath());
    }
}

[Script]
class spell_gen_force_phase_update : AuraScript
{
    void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        PhasingHandler.OnConditionChange(GetTarget());
    }

    void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        PhasingHandler.OnConditionChange(GetTarget());
    }

    public override void Register()
    {
        AfterEffectApply.Add(new(AfterApply, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real));
        AfterEffectRemove.Add(new(AfterRemove, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real));
    }
}

[Script("spell_gen_no_npc_damage_below_override_70", 70.0f)]
class spell_gen_no_npc_damage_below_override(float healthPct) : AuraScript
{
    void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        amount = -1;
    }

    void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
    {
        if (dmgInfo.GetAttacker() == null || !dmgInfo.GetAttacker().IsCreature())
        {
            PreventDefaultAction();
            return;
        }

        if (GetTarget().GetHealthPct() <= healthPct)
            absorbAmount = dmgInfo.GetDamage();
        else
            PreventDefaultAction();
    }

    public override void Register()
    {
        DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.SchoolAbsorb));
        OnEffectAbsorb.Add(new(HandleAbsorb, 0));
    }
}

[Script] // 92678 - Abandon Vehicle
class spell_gen_abandon_vehicle : SpellScript
{
    void HandleHitTarget(uint effIndex)
    {
        GetHitUnit().ExitVehicle();
    }

    public override void Register()
    {
        OnEffectHitTarget.Add(new(HandleHitTarget, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect));
    }
}
