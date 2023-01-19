// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.Spell;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Generic
{
    struct SpellIds
    {
        // Adaptivewarding
        public const uint GenAdaptiveWardingFire = 28765;
        public const uint GenAdaptiveWardingNature = 28768;
        public const uint GenAdaptiveWardingFrost = 28766;
        public const uint GenAdaptiveWardingShadow = 28769;
        public const uint GenAdaptiveWardingArcane = 28770;

        // Animalbloodpoolspell
        public const uint AnimalBlood = 46221;
        public const uint SpawnBloodPool = 63471;

        // Serviceuniform
        public const uint ServiceUniform = 71450;

        // Genericbandage
        public const uint RecentlyBandaged = 11196;

        // Bloodreserve
        public const uint BloodReserveAura = 64568;
        public const uint BloodReserveHeal = 64569;

        // Bonked
        public const uint Bonked = 62991;
        public const uint FormSwordDefeat = 62994;
        public const uint Onguard = 62972;

        // Breakshieldspells
        public const uint BreakShieldDamage2k = 62626;
        public const uint BreakShieldDamage10k = 64590;
        public const uint BreakShieldTriggerFactionMounts = 62575; // Also On Toc5 Mounts
        public const uint BreakShieldTriggerCampaingWarhorse = 64595;
        public const uint BreakShieldTriggerUnk = 66480;

        // Cannibalizespells
        public const uint CannibalizeTriggered = 20578;

        // Chaosblast
        public const uint ChaosBlast = 37675;

        // Clone
        public const uint NightmareFigmentMirrorImage = 57528;

        // Cloneweaponspells        
        public const uint WeaponAura = 41054;
        public const uint Weapon2Aura = 63418;
        public const uint Weapon3Aura = 69893;

        public const uint OffhandAura = 45205;
        public const uint Offhand2Aura = 69896;

        public const uint RangedAura = 57594;

        // Createlancespells
        public const uint CreateLanceAlliance = 63914;
        public const uint CreateLanceHorde = 63919;

        // Dalarandisguisespells        
        public const uint SunreaverTrigger = 69672;
        public const uint SunreaverFemale = 70973;
        public const uint SunreaverMale = 70974;

        public const uint SilverCovenantTrigger = 69673;
        public const uint SilverCovenantFemale = 70971;
        public const uint SilverCovenantMale = 70972;

        // Defendvisuals
        public const uint VisualShield1 = 63130;
        public const uint VisualShield2 = 63131;
        public const uint VisualShield3 = 63132;

        // Divinestormspell
        public const uint DivineStorm = 53385;

        // Elunecandle
        public const uint OmenHead = 26622;
        public const uint OmenChest = 26624;
        public const uint OmenHandR = 26625;
        public const uint OmenHandL = 26649;
        public const uint Normal = 26636;

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

        //FuriousRage
        public const uint Exhaustion = 35492;

        // Fishingspells
        public const uint FishingNoFishingPole = 131476;
        public const uint FishingWithPole = 131490;

        // Transporterbackfires
        public const uint TransporterMalfunctionPolymorph = 23444;
        public const uint TransporterEviltwin = 23445;
        public const uint TransporterMalfunctionMiss = 36902;

        // Gnomishtransporter
        public const uint TransporterSuccess = 23441;
        public const uint TransporterFailure = 23446;

        // Interrupt
        public const uint GenThrowInterrupt = 32747;

        // Genericlifebloomspells        
        public const uint HexlordMalacrass = 43422;
        public const uint TurragePaw = 52552;
        public const uint CenarionScout = 53692;
        public const uint TwistedVisage = 57763;
        public const uint FactionChampionsDru = 66094;

        // Chargespells        
        public const uint Damage8k5 = 62874;
        public const uint Damage20k = 68498;
        public const uint Damage45k = 64591;

        public const uint ChargingEffect8k5 = 63661;
        public const uint Charging20k1 = 68284;
        public const uint Charging20k2 = 68501;
        public const uint ChargingEffect45k1 = 62563;
        public const uint ChargingEffect45k2 = 66481;

        public const uint TriggerFactionMounts = 62960;
        public const uint TriggerTrialChampion = 68282;

        public const uint MissEffect = 62977;

        // MossCoveredFeet
        public const uint FallDown = 6869;

        // Netherbloom
        public const uint NetherBloomPollen1 = 28703;

        // Nightmarevine
        public const uint NightmarePollen = 28721;

        // Obsidianarmorspells        
        public const uint Holy = 27536;
        public const uint Fire = 27533;
        public const uint Nature = 27538;
        public const uint Frost = 27534;
        public const uint Shadow = 27535;
        public const uint Arcane = 27540;

        // Tournamentpennantspells
        public const uint StormwindAspirant = 62595;
        public const uint StormwindValiant = 62596;
        public const uint StormwindChampion = 62594;
        public const uint GnomereganAspirant = 63394;
        public const uint GnomereganValiant = 63395;
        public const uint GnomereganChampion = 63396;
        public const uint SenjinAspirant = 63397;
        public const uint SenjinValiant = 63398;
        public const uint SenjinChampion = 63399;
        public const uint SilvermoonAspirant = 63401;
        public const uint SilvermoonValiant = 63402;
        public const uint SilvermoonChampion = 63403;
        public const uint DarnassusAspirant = 63404;
        public const uint DarnassusValiant = 63405;
        public const uint DarnassusChampion = 63406;
        public const uint ExodarAspirant = 63421;
        public const uint ExodarValiant = 63422;
        public const uint ExodarChampion = 63423;
        public const uint IronforgeAspirant = 63425;
        public const uint IronforgeValiant = 63426;
        public const uint IronforgeChampion = 63427;
        public const uint UndercityAspirant = 63428;
        public const uint UndercityValiant = 63429;
        public const uint UndercityChampion = 63430;
        public const uint OrgrimmarAspirant = 63431;
        public const uint OrgrimmarValiant = 63432;
        public const uint OrgrimmarChampion = 63433;
        public const uint ThunderbluffAspirant = 63434;
        public const uint ThunderbluffValiant = 63435;
        public const uint ThunderbluffChampion = 63436;
        public const uint ArgentcrusadeAspirant = 63606;
        public const uint ArgentcrusadeValiant = 63500;
        public const uint ArgentcrusadeChampion = 63501;
        public const uint EbonbladeAspirant = 63607;
        public const uint EbonbladeValiant = 63608;
        public const uint EbonbladeChampion = 63609;

        // Orcdisguisespells
        public const uint OrcDisguiseTrigger = 45759;
        public const uint OrcDisguiseMale = 45760;
        public const uint OrcDisguiseFemale = 45762;

        // Paralytic Poison
        public const uint Paralysis = 35202;

        // Parachutespells
        public const uint Parachute = 45472;
        public const uint ParachuteBuff = 44795;

        // ProfessionResearch
        public const uint NorthrendInscriptionResearch = 61177;

        // Trinketspells
        public const uint PvpTrinketAlliance = 97403;
        public const uint PvpTrinketHorde = 97404;

        // Replenishment
        public const uint Replenishment = 57669;
        public const uint InfiniteReplenishment = 61782;

        // Runningwild
        public const uint AlteredForm = 97709;

        // Seaforiumspells
        public const uint PlantChargesCreditAchievement = 60937;

        // Summonelemental
        public const uint SummonFireElemental = 8985;
        public const uint SummonEarthElemental = 19704;

        // Tournamentmountsspells
        public const uint LanceEquipped = 62853;

        // Mountedduelspells
        public const uint OnTournamentMount = 63034;
        public const uint MountedDuel = 62875;

        // Teleporting
        public const uint TeleportSpireDown = 59316;
        public const uint TeleportSpireUp = 59314;

        // Pvptrinkettriggeredspells
        public const uint WillOfTheForsakenCooldownTrigger = 72752;
        public const uint WillOfTheForsakenCooldownTriggerWotf = 72757;

        // Friendorfowl
        public const uint TurkeyVengeance = 25285;

        // Vampirictouch
        public const uint VampiricTouchHeal = 52724;

        // Vehiclescaling
        public const uint GearScaling = 66668;

        // Whispergulchyoggsaronwhisper
        public const uint YoggSaronWhisperDummy = 29072;

        // Gmfreeze
        public const uint GmFreeze = 9454;

        // Landmineknockbackachievement        
        public const uint LandmineKnockbackAchievement = 57064;

        // Ponyspells
        public const uint AchievementPonyup = 3736;
        public const uint MountPony = 29736;

        // CorruptinPlagueEntrys
        public const uint CorruptingPlague = 40350;

        // StasisFieldEntrys
        public const uint StasisField = 40307;

        // SiegeTankControl
        public const uint SiegeTankControl = 47963;

        // CannonBlast
        public const uint CannonBlast = 42578;
        public const uint CannonBlastDamage = 42576;

        // FreezingCircleMisc
        public const uint FreezingCirclePitOfSaronNormal = 69574;
        public const uint FreezingCirclePitOfSaronHeroic = 70276;
        public const uint FreezingCircle = 34787;
        public const uint FreezingCircleScenario = 141383;

        // Kazrogalhellfiremark
        public const uint MarkOfKazrogalHellfire = 189512;
        public const uint MarkOfKazrogalDamageHellfire = 189515;

        // AuraprocRemovespells
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
    }

    struct CreatureIds
    {
        // EluneCandle
        public const uint Omen = 15467;

        // EtherealPet
        public const uint EtherealSoulTrader = 27914;

        // TournamentMounts
        public const uint StormwindSteed = 33217;
        public const uint IronforgeRam = 33316;
        public const uint GnomereganMechanostrider = 33317;
        public const uint ExodarElekk = 33318;
        public const uint DarnassianNightsaber = 33319;
        public const uint OrgrimmarWolf = 33320;
        public const uint DarkSpearRaptor = 33321;
        public const uint ThunderBluffKodo = 33322;
        public const uint SilvermoonHawkstrider = 33323;
        public const uint ForsakenWarhorse = 33324;
        public const uint ArgentWarhorse = 33782;
        public const uint ArgentSteedAspirant = 33845;
        public const uint ArgentHawkstriderAspirant = 33844;

        // PetSummoned
        public const uint Doomguard = 11859;
        public const uint Infernal = 89;
        public const uint Imp = 416;

        // VendorBarkTrigger
        public const uint AmphitheaterVendor = 30098;

        //CorruptinPlagueEntrys
        public const uint ApexisFlayer = 22175;
        public const uint ShardHideBoar = 22180;
        public const uint AetherRay = 22181;

        // StasisFieldEntrys
        public const uint DaggertailLizard = 22255;

        // DefenderOfAzerothData
        public const uint Nazgrim = 161706;
        public const uint Trollbane = 161707;
        public const uint Whitemane = 161708;
        public const uint Morgaine = 161709;
    }

    struct ModelIds
    {
        //ServiceUniform
        public const uint GoblinMale = 31002;
        public const uint GoblinFemale = 31003;
    }

    struct TextIds
    {
        //EtherealPet
        public const uint SayStealEssence = 1;
        public const uint SayCreateToken = 2;

        //VendorBarkTrigger
        public const uint SayAmphitheaterVendor = 0;
    }

    struct EmoteIds
    {
        //FuriousRage
        public const uint FuriousRage = 19415;
        public const uint Exhausted = 18368;
    }

    struct AchievementIds
    {
        //TournamentAchievements
        public const uint ChampionStormwind = 2781;
        public const uint ChampionDarnassus = 2777;
        public const uint ChampionIronforge = 2780;
        public const uint ChampionGnomeregan = 2779;
        public const uint ChampionTheExodar = 2778;
        public const uint ChampionOrgrimmar = 2783;
        public const uint ChampionSenJin = 2784;
        public const uint ChampionThunderBluff = 2786;
        public const uint ChampionUndercity = 2787;
        public const uint ChampionSilvermoon = 2785;
        public const uint ArgentValor = 2758;
        public const uint ChampionAlliance = 2782;
        public const uint ChampionHorde = 2788;
    }

    struct QuestIds
    {
        //TournamentQuests
        public const uint ValiantOfStormwind = 13593;
        public const uint A_ValiantOfStormwind = 13684;
        public const uint ValiantOfDarnassus = 13706;
        public const uint A_ValiantOfDarnassus = 13689;
        public const uint ValiantOfIronforge = 13703;
        public const uint A_ValiantOfIronforge = 13685;
        public const uint ValiantOfGnomeregan = 13704;
        public const uint A_ValiantOfGnomeregan = 13688;
        public const uint ValiantOfTheExodar = 13705;
        public const uint A_ValiantOfTheExodar = 13690;
        public const uint ValiantOfOrgrimmar = 13707;
        public const uint A_ValiantOfOrgrimmar = 13691;
        public const uint ValiantOfSenJin = 13708;
        public const uint A_ValiantOfSenJin = 13693;
        public const uint ValiantOfThunderBluff = 13709;
        public const uint A_ValiantOfThunderBluff = 13694;
        public const uint ValiantOfUndercity = 13710;
        public const uint A_ValiantOfUndercity = 13695;
        public const uint ValiantOfSilvermoon = 13711;
        public const uint A_ValiantOfSilvermoon = 13696;

        //DefenderOfAzerothData
        public const uint DefenderOfAzerothAlliance = 58902;
        public const uint DefenderOfAzerothHorde = 58903;
    }

    struct Misc
    {
        // FungalDecay
        public const int AuraDuration = 12600; // found in sniffs, there is no duration entry we can possibly use

        // FreezingCircleMisc
        public const uint MapIdBloodInTheSnowScenario = 1130;

        // Teleporting
        public const uint AreaVioletCitadelSpire = 4637;
    }

    [Script]
    class spell_gen_absorb0_hitlimit1 : AuraScript
    {
        int limit;

        public override bool Load()
        {
            // Max absorb stored in 1 dummy effect
            limit = GetSpellInfo().GetEffect(1).CalcValue();
            return true;
        }

        void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            absorbAmount = (uint)Math.Min(limit, absorbAmount);
        }

        public override void Register()
        {
            OnEffectAbsorb.Add(new EffectAbsorbHandler(Absorb, 0));
        }
    }

    [Script] // 28764 - Adaptive Warding (Frostfire Regalia Set)
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

            uint spellId;
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
            GetTarget().CastSpell(GetTarget(), spellId, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script]
    class spell_gen_allow_cast_from_item_only : SpellScript, ICheckCastHander
    {
        public SpellCastResult CheckCast()
        {
            if (!GetCastItem())
                return SpellCastResult.CantDoThatRightNow;
            return SpellCastResult.SpellCastOk;
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
            if (owner)
                owner.CastSpell(owner, SpellIds.SpawnBloodPool, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 63471 -Spawn Blood Pool
    class spell_spawn_blood_pool : SpellScript
    {
        void SetDest(ref SpellDestination dest)
        {
            Unit caster = GetCaster();
            Position summonPos = caster.GetPosition();
            LiquidData liquidStatus = new();
            if (caster.GetMap().GetLiquidStatus(caster.GetPhaseShift(), caster.GetPositionX(), caster.GetPositionY(), caster.GetPositionZ(), LiquidHeaderTypeFlags.AllLiquids, liquidStatus, caster.GetCollisionHeight()) != ZLiquidStatus.NoWater)
                summonPos.posZ = liquidStatus.level;
            dest.Relocate(summonPos);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
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
            return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            if (spellInfo.GetEffects().Empty() || !spellInfo.GetEffect(0).IsAura(AuraType.ModPowerRegen))
            {
                Log.outError(LogFilter.Spells, "Aura {GetId()} structure has been changed - first aura is no longer SPELL_AURA_MOD_POWER_REGEN");
                return false;
            }

            return true;
        }

        void CalcPeriodic(AuraEffect aurEff, ref bool isPeriodic, ref int amplitude)
        {
            // Get SPELL_AURA_MOD_POWER_REGEN aura from spell
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
            DoEffectCalcPeriodic.Add(new EffectCalcPeriodicHandler(CalcPeriodic, 1, AuraType.PeriodicDummy));
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalcAmount, 1, AuraType.PeriodicDummy));
            OnEffectUpdatePeriodic.Add(new EffectUpdatePeriodicHandler(UpdatePeriodic, 1, AuraType.PeriodicDummy));
        }
    }

    [Script] // 28313 - Aura of Fear
    class spell_gen_aura_of_fear : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return !spellInfo.GetEffects().Empty() && ValidateSpellInfo(spellInfo.GetEffect(0).TriggerSpell);
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_gen_av_drekthar_presence : AuraScript
    {
        bool CheckAreaTarget(Unit target)
        {
            return (target.GetEntry()) switch
            {
                // alliance
                // Dun Baldar North Marshal
                14762 or 14763 or 14764 or 14765 or 11948 or 14772 or 14776 or 14773 or 14777 or 11946 => true,
                _ => false,
            };
        }

        public override void Register()
        {
            DoCheckAreaTarget.Add(new CheckAreaTargetHandler(CheckAreaTarget));
        }
    }

    [Script]
    class spell_gen_bandage : SpellScript, ICheckCastHander
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RecentlyBandaged);
        }

        public SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();
            if (target)
            {
                if (target.HasAura(SpellIds.RecentlyBandaged))
                    return SpellCastResult.TargetAurastate;
            }
            return SpellCastResult.SpellCastOk;
        }

        void HandleScript()
        {
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.RecentlyBandaged, true);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleScript));
        }
    }

    [Script] // 193970 - Mercenary Shapeshift
    class spell_gen_battleground_mercenary_shapeshift : AuraScript
    {
        //using OtherFactionRacePriorityList = std::array<Races, 3>;

        static Dictionary<Race, Race[]> RaceInfo = new()
        {
            { Race.Human, new[] { Race.Undead, Race.BloodElf } },
            { Race.Orc, new[] { Race.Dwarf } },
            { Race.Dwarf, new[] { Race.Orc, Race.Undead, Race.Tauren } },
            { Race.NightElf, new[] { Race.Troll, Race.BloodElf } },
            { Race.Undead, new[] { Race.Human } },
            { Race.Tauren, new[] { Race.Draenei, Race.NightElf } },
            { Race.Gnome, new[] { Race.Goblin, Race.BloodElf } },
            { Race.Troll, new[] { Race.NightElf, Race.Human, Race.Draenei } },
            { Race.Goblin, new[] { Race.Gnome, Race.Dwarf } },
            { Race.BloodElf, new[] { Race.Human, Race.NightElf } },
            { Race.Draenei, new[] { Race.Tauren, Race.Orc } },
            { Race.Worgen, new[] { Race.Troll } },
            { Race.PandarenNeutral, new[] { Race.PandarenNeutral } },
            { Race.PandarenAlliance, new[] { Race.PandarenHorde, Race.PandarenNeutral } },
            { Race.PandarenHorde, new[] { Race.PandarenAlliance, Race.PandarenNeutral } },
            { Race.Nightborne, new[] { Race.NightElf, Race.Human } },
            { Race.HighmountainTauren, new[] { Race.Draenei, Race.NightElf } },
            { Race.VoidElf, new[] { Race.Troll, Race.BloodElf } },
            { Race.LightforgedDraenei, new[] { Race.Tauren, Race.Orc } },
            { Race.ZandalariTroll, new[] { Race.KulTiran, Race.Human } },
            { Race.KulTiran, new[] { Race.ZandalariTroll } },
            { Race.DarkIronDwarf, new[] { Race.MagharOrc, Race.Orc } },
            { Race.Vulpera, new[] { Race.MechaGnome, Race.DarkIronDwarf /*Guessed, For Shamans*/ } },
            { Race.MagharOrc, new[] { Race.DarkIronDwarf } },
            { Race.MechaGnome, new[] { Race.Vulpera } },
        };

        static Dictionary<Race, uint[]> RaceDisplayIds = new()
        {
            { Race.Human, new uint[] { 55239, 55238 } },
            { Race.Orc, new uint[] { 55257, 55256 } },
            { Race.Dwarf, new uint[] { 55241, 55240 } },
            { Race.NightElf, new uint[] { 55243, 55242 } },
            { Race.Undead, new uint[] { 55259, 55258 } },
            { Race.Tauren, new uint[] { 55261, 55260 } },
            { Race.Gnome, new uint[] { 55245, 55244 } },
            { Race.Troll, new uint[] { 55263, 55262 } },
            { Race.Goblin, new uint[] { 55267, 57244 } },
            { Race.BloodElf, new uint[] { 55265, 55264 } },
            { Race.Draenei, new uint[] { 55247, 55246 } },
            { Race.Worgen, new uint[] { 55255, 55254 } },
            { Race.PandarenNeutral, new uint[] { 55253, 55252 } }, // Not Verified, Might Be Swapped With Race.PandarenHorde
            { Race.PandarenAlliance, new uint[] { 55249, 55248 } },
            { Race.PandarenHorde, new uint[] { 55251, 55250 } },
            { Race.Nightborne, new uint[] { 82375, 82376 } },
            { Race.HighmountainTauren, new uint[] { 82377, 82378 } },
            { Race.VoidElf, new uint[] { 82371, 82372 } },
            { Race.LightforgedDraenei, new uint[] { 82373, 82374 } },
            { Race.ZandalariTroll, new uint[] { 88417, 88416 } },
            { Race.KulTiran, new uint[] { 88414, 88413 } },
            { Race.DarkIronDwarf, new uint[] { 88409, 88408 } },
            { Race.Vulpera, new uint[] { 94999, 95001 } },
            { Race.MagharOrc, new uint[] { 88420, 88410 } },
            { Race.MechaGnome, new uint[] { 94998, 95000 } },
        };

        static List<uint> RacialSkills = new();

        static Race GetReplacementRace(Race nativeRace, Class playerClass)
        {
            var otherRaces = RaceInfo.LookupByKey(nativeRace);
            if (otherRaces != null)
                foreach (Race race in otherRaces)
                    if (Global.ObjectMgr.GetPlayerInfo(race, playerClass) != null)
                        return race;

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
            foreach (var (race, otherRaces) in RaceInfo)
            {
                if (!CliDB.ChrRacesStorage.ContainsKey(race))
                    return false;

                foreach (Race otherRace in otherRaces)
                    if (!CliDB.ChrRacesStorage.ContainsKey(otherRace))
                        return false;
            }

            foreach (var (race, displayIds) in RaceDisplayIds)
            {
                if (!CliDB.ChrRacesStorage.ContainsKey(race))
                    return false;

                foreach (uint displayId in displayIds)
                    if (CliDB.CreatureDisplayInfoStorage.ContainsKey(displayId))
                        return false;
            }

            RacialSkills.Clear();
            foreach (SkillLineRecord skillLine in CliDB.SkillLineStorage.Values)
                if (skillLine.GetFlags().HasFlag(SkillLineFlags.RacialForThePurposeOfTemporaryRaceChange))
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

            if (mode.HasFlag(AuraEffectHandleModes.Real))
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
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.SendForClientMask));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real));
        }
    }

    [Script] // Blood Reserve - 64568
    class spell_gen_blood_reserve : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodReserveHeal);
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
            caster.CastSpell(caster, SpellIds.BloodReserveHeal, args);
            caster.RemoveAura(SpellIds.BloodReserveAura);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script]
    class spell_gen_bonked : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target)
            {
                Aura aura = GetHitAura();
                if (!(aura != null && aura.GetStackAmount() == 3))
                    return;

                target.CastSpell(target, SpellIds.FormSwordDefeat, true);
                target.RemoveAurasDueToSpell(SpellIds.Bonked);

                aura = target.GetAura(SpellIds.Onguard);
                if (aura != null)
                {
                    Item item = target.GetItemByGuid(aura.GetCastItemGUID());
                    if (item)
                        target.DestroyItemCount(item.GetEntry(), 1, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect));
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
                                spellId = SpellIds.BreakShieldDamage10k;
                                break;
                            case SpellIds.BreakShieldTriggerFactionMounts:
                                spellId = SpellIds.BreakShieldDamage2k;
                                break;
                            default:
                                return;
                        }
                        Unit rider = GetCaster().GetCharmer();
                        if (rider)
                            rider.CastSpell(target, spellId, false);
                        else
                            GetCaster().CastSpell(target, spellId, false);
                        break;
                    }
                case 1: // On damaging spells, for removing a defend layer
                    {
                        var auras = target.GetAppliedAuras();
                        foreach (var pair in auras)
                        {
                            Aura aura = pair.Value.GetBase();
                            if (aura != null)
                            {
                                if (aura.GetId() == 62552 || aura.GetId() == 62719 || aura.GetId() == 64100 || aura.GetId() == 66482)
                                {
                                    aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                    // Remove dummys from rider (Necessary for updating visual shields)
                                    Unit rider = target.GetCharmer();
                                    if (rider)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 48750 - Burning Depths Necrolyte Image
    class spell_gen_burning_depths_necrolyte_image : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 2 && ValidateSpellInfo((uint)spellInfo.GetEffect(2).CalcValue());
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
                caster.CastSpell(GetTarget(), (uint)GetEffectInfo(2).CalcValue());
        }

        void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell((uint)GetEffectInfo(2).CalcValue(), GetCasterGUID());
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_cannibalize : SpellScript, ICheckCastHander
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CannibalizeTriggered);
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            float max_range = GetSpellInfo().GetMaxRange(false);
            // search for nearby enemy corpse in range
            var check = new AnyDeadUnitSpellTargetInRangeCheck<Unit>(caster, max_range, GetSpellInfo(), SpellTargetCheckTypes.Enemy, SpellTargetObjectTypes.CorpseEnemy);
            var searcher = new UnitSearcher(caster, check);
            Cell.VisitWorldObjects(caster, searcher, max_range);
            if (!searcher.GetTarget())
                Cell.VisitGridObjects(caster, searcher, max_range);
            if (!searcher.GetTarget())
                return SpellCastResult.NoEdibleCorpses;
            return SpellCastResult.SpellCastOk;
        }

        void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.CannibalizeTriggered, false);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectUpdatePeriodic.Add(new EffectUpdatePeriodicHandler(UpdatePeriodic, 1, AuraType.PeriodicDummy));
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
            if (target)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.BasePoint0, basepoints0);
                caster.CastSpell(target, SpellIds.ChaosBlast, args);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.Dummy));
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.Dummy));
            }
            else
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.ScriptEffect));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_clone_weapon_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WeaponAura, SpellIds.Weapon2Aura, SpellIds.Weapon3Aura, SpellIds.OffhandAura, SpellIds.Offhand2Aura, SpellIds.RangedAura);
        }

        public override bool Load()
        {
            prevItem = 0;
            return true;
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            Unit target = GetTarget();
            if (!caster)
                return;

            switch (GetSpellInfo().Id)
            {
                case SpellIds.WeaponAura:
                case SpellIds.Weapon2Aura:
                case SpellIds.Weapon3Aura:
                    {
                        prevItem = target.GetVirtualItemId(0);

                        Player player = caster.ToPlayer();
                        if (player)
                        {
                            Item mainItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                            if (mainItem)
                                target.SetVirtualItem(0, mainItem.GetEntry());
                        }
                        else
                            target.SetVirtualItem(0, caster.GetVirtualItemId(0));
                        break;
                    }
                case SpellIds.OffhandAura:
                case SpellIds.Offhand2Aura:
                    {
                        prevItem = target.GetVirtualItemId(1);

                        Player player = caster.ToPlayer();
                        if (player)
                        {
                            Item offItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                            if (offItem)
                                target.SetVirtualItem(1, offItem.GetEntry());
                        }
                        else
                            target.SetVirtualItem(1, caster.GetVirtualItemId(1));
                        break;
                    }
                case SpellIds.RangedAura:
                    {
                        prevItem = target.GetVirtualItemId(2);

                        Player player = caster.ToPlayer();
                        if (player)
                        {
                            Item rangedItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                            if (rangedItem)
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
                case SpellIds.WeaponAura:
                case SpellIds.Weapon2Aura:
                case SpellIds.Weapon3Aura:
                    target.SetVirtualItem(0, prevItem);
                    break;
                case SpellIds.OffhandAura:
                case SpellIds.Offhand2Aura:
                    target.SetVirtualItem(1, prevItem);
                    break;
                case SpellIds.RangedAura:
                    target.SetVirtualItem(2, prevItem);
                    break;
                default:
                    break;
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
        }

        uint prevItem;
    }

    [Script("spell_gen_default_count_pct_from_max_hp", 0)]
    [Script("spell_gen_50pct_count_pct_from_max_hp", 50)]
    class spell_gen_count_pct_from_max_hp : SpellScript
    {
        int _damagePct;

        public spell_gen_count_pct_from_max_hp(int damagePct)
        {
            _damagePct = damagePct;
        }

        void RecalculateDamage()
        {
            if (_damagePct == 0)
                _damagePct = GetHitDamage();

            SetHitDamage((int)GetHitUnit().CountPctFromMaxHealth(_damagePct));
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(RecalculateDamage));
        }
    }

    // 28865 - Consumption
    [Script] // 64208 - Consumption
    class spell_gen_consumption : SpellScript
    {
        void HandleDamageCalc(uint effIndex)
        {
            Creature caster = GetCaster().ToCreature();
            if (caster == null)
                return;

            int damage = 0;
            SpellInfo createdBySpell = Global.SpellMgr.GetSpellInfo(caster.m_unitData.CreatedBySpell, GetCastDifficulty());
            if (createdBySpell != null)
                damage = createdBySpell.GetEffect(2).CalcValue();

            if (damage != 0)
                SetEffectValue(damage);
        }

        public override void Register()
        {
            OnEffectLaunchTarget.Add(new EffectHandler(HandleDamageCalc, 0, SpellEffectName.SchoolDamage));
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
            if (target)
            {
                if (target.GetTeam() == Team.Alliance)
                    GetCaster().CastSpell(target, SpellIds.CreateLanceAlliance, true);
                else
                    GetCaster().CastSpell(target, SpellIds.CreateLanceHorde, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script("spell_gen_sunreaver_disguise")]
    [Script("spell_gen_silver_covenant_disguise")]
    class spell_gen_dalaran_disguise : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.Id switch
            {
                SpellIds.SunreaverTrigger => ValidateSpellInfo(SpellIds.SunreaverFemale, SpellIds.SunreaverMale),
                SpellIds.SilverCovenantTrigger => ValidateSpellInfo(SpellIds.SilverCovenantFemale, SpellIds.SilverCovenantMale),
                _ => false,
            };
        }

        void HandleScript(uint effIndex)
        {
            Player player = GetHitPlayer();
            if (player)
            {
                Gender gender = player.GetNativeGender();

                uint spellId = GetSpellInfo().Id;
                switch (spellId)
                {
                    case SpellIds.SunreaverTrigger:
                        spellId = gender == Gender.Female ? SpellIds.SunreaverFemale : SpellIds.SunreaverMale;
                        break;
                    case SpellIds.SilverCovenantTrigger:
                        spellId = gender == Gender.Female ? SpellIds.SilverCovenantFemale : SpellIds.SilverCovenantMale;
                        break;
                    default:
                        break;
                }

                GetCaster().CastSpell(player, spellId, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 32065 - Fungal Decay
    class spell_gen_decay_over_time_fungal_decay_SpellScript : SpellScript
    {
        void ModAuraStack()
        {
            Aura aur = GetHitAura();
            if (aur != null)
                aur.SetStackAmount((byte)GetSpellInfo().StackAmount);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(ModAuraStack));
        }
    }

    [Script] // 32065 - Fungal Decay
    class spell_gen_decay_over_time_fungal_decay_AuraScript : AuraScript
    {
        void ModDuration(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // only on actual reapply, not on stack decay
            if (GetDuration() == GetMaxDuration())
            {
                SetMaxDuration(Misc.AuraDuration);
                SetDuration(Misc.AuraDuration);
            }
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() == GetSpellInfo();
        }

        void Decay(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            ModStackAmount(-1);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnProc.Add(new AuraProcHandler(Decay));
            OnEffectApply.Add(new EffectApplyHandler(ModDuration, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }

    [Script] // 36659 - Tail Sting
    class spell_gen_decay_over_time_tail_sting_SpellScript : SpellScript
    {
        void ModAuraStack()
        {
            Aura aur = GetHitAura();
            if (aur != null)
                aur.SetStackAmount((byte)GetSpellInfo().StackAmount);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(ModAuraStack));
        }
    }

    [Script] // 36659 - Tail Sting
    class spell_gen_decay_over_time_tail_sting_AuraScript : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() == GetSpellInfo();
        }

        void Decay(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            ModStackAmount(-1);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnProc.Add(new AuraProcHandler(Decay));
        }
    }

    class spell_gen_despawn_aura : AuraScript
    {
        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().ToCreature()?.DespawnOrUnsummon();
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, SpellConst.EffectFirstFound, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_despawn_self : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Unit);
        }

        void HandleDummy(uint effIndex)
        {
            if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) || GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
                GetCaster().ToCreature().DespawnOrUnsummon();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, SpellConst.EffectAll, SpellEffectName.Any));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDespawn, SpellConst.EffectAll, SpellEffectName.Any));
        }
    }

    [Script] // 70769 Divine Storm!
    class spell_gen_divine_storm_cd_reset : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_ds_flush_knockback : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            // Here the target is the water spout and determines the position where the player is knocked from
            Unit target = GetHitUnit();
            if (target)
            {
                Player player = GetCaster().ToPlayer();
                if (player)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_dungeon_credit : SpellScript
    {
        public override bool Load()
        {
            _handled = false;
            return GetCaster().IsTypeId(TypeId.Unit);
        }

        void CreditEncounter()
        {
            // This hook is executed for every target, make sure we only credit instance once
            if (_handled)
                return;

            _handled = true;
            Unit caster = GetCaster();
            InstanceScript instance = caster.GetInstanceScript();
            if (instance != null)
                instance.UpdateEncounterStateForSpellCast(GetSpellInfo().Id, caster);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(CreditEncounter));
        }

        bool _handled;
    }

    // 50051 - Ethereal Pet Aura
    [Script]
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

            List<TempSummon> minionList = new();
            GetUnitOwner().GetAllMinionsByEntry(minionList, CreatureIds.EtherealSoulTrader);
            foreach (Creature minion in minionList)
            {
                if (minion.IsAIEnabled())
                {
                    minion.GetAI().Talk(TextIds.SayStealEssence);
                    minion.CastSpell(eventInfo.GetProcTarget(), SpellIds.StealEssenceVisual);
                }
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    // 50052 - Ethereal Pet onSummon
    [Script]
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 50055 - Ethereal Pet Aura Remove
    [Script]
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 50101 - Ethereal Pet OnKill Steal Essence
    [Script]
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
                    soulTrader.GetAI().Talk(TextIds.SayCreateToken);
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
            return ValidateSpellInfo(SpellIds.FeastFood, SpellIds.FeastDrink, SpellIds.BountifulFeastDrink, SpellIds.BountifulFeastFood, SpellIds.GreatFeastRefreshment,
                SpellIds.FishFeastRefreshment, SpellIds.GiganticFeastRefreshment, SpellIds.SmallFeastRefreshment, SpellIds.BountifulFeastRefreshment);
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    /*
     * There are only 3 possible flags Feign Death auras can apply: UNIT_DYNFLAG_DEAD, UnitFlags2.FeignDeath
     * and UNIT_FLAG_PREVENT_EMOTES_FROM_CHAT_TEXT. Some auras can apply only 2 flags
     * 
     * spell_gen_feign_death_all_flags applies all 3 flags
     * spell_gen_feign_death_no_dyn_flag applies no UNIT_DYNFLAG_DEAD (does not make the creature appear dead)
     * spell_gen_feign_death_no_prevent_emotes applies no UNIT_FLAG_PREVENT_EMOTES_FROM_CHAT_TEXT
     * 
     * REACT_PASSIVE should be handled directly in scripts since not all creatures should be passive. Otherwise
     * creature will be not able to aggro or execute MoveInLineOfSight events. Removing may cause more issues
     * than already exists
     */
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
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.InitializeReactState();
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.InitializeReactState();
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
            if (creature != null)
                creature.SetReactState(ReactStates.Passive);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);

            Creature creature = target.ToCreature();
            if (creature != null)
                creature.InitializeReactState();
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 35491 - Furious Rage
    class spell_gen_furious_rage : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhaustion) &&
                CliDB.BroadcastTextStorage.HasRecord(EmoteIds.FuriousRage) &&
                CliDB.BroadcastTextStorage.HasRecord(EmoteIds.Exhausted);
        }

        void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.TextEmote(EmoteIds.FuriousRage, target, false);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit target = GetTarget();
            target.TextEmote(EmoteIds.Exhausted, target, false);
            target.CastSpell(target, SpellIds.Exhaustion, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(AfterApply, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real));
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            uint spellId;
            Item mainHand = GetCaster().ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
            if (!mainHand || mainHand.GetTemplate().GetClass() != ItemClass.Weapon || (ItemSubClassWeapon)mainHand.GetTemplate().GetSubClass() != ItemSubClassWeapon.FishingPole)
                spellId = SpellIds.FishingNoFishingPole;
            else
                spellId = SpellIds.FishingWithPole;

            GetCaster().CastSpell(GetCaster(), spellId, false);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_gadgetzan_transporter_backfire : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TransporterMalfunctionPolymorph, SpellIds.TransporterEviltwin, SpellIds.TransporterMalfunctionMiss);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            int r = RandomHelper.IRand(0, 119);
            if (r < 20)                           // Transporter Malfunction - 1/6 polymorph
                caster.CastSpell(caster, SpellIds.TransporterMalfunctionPolymorph, true);
            else if (r < 100)                     // Evil Twin               - 4/6 evil twin
                caster.CastSpell(caster, SpellIds.TransporterEviltwin, true);
            else                                    // Transporter Malfunction - 1/6 miss the target
                caster.CastSpell(caster, SpellIds.TransporterMalfunctionMiss, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_gift_of_naaru : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 1;
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (!GetCaster() || aurEff.GetTotalTicks() == 0)
                return;

            float healPct = GetEffectInfo(1).CalcValue() / 100.0f;
            float heal = healPct * GetCaster().GetMaxHealth();
            int healTick = (int)Math.Floor(heal / aurEff.GetTotalTicks());
            amount += healTick;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    /* 9204 - Hate to Zero(Melee)
     * 20538 - Hate to Zero(AoE)
     * 26569 - Hate to Zero(AoE)
     * 26637 - Hate to Zero(AoE, Unique)
     * 37326 - Hate to Zero(AoE)
     * 40410 - Hate to Zero(Should be added, AoE)
     * 40467 - Hate to Zero(Should be added, AoE)
     * 41582 - Hate to Zero(Should be added, Melee) */
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_interrupt : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GenThrowInterrupt);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.GenThrowInterrupt, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script("spell_hexlord_lifebloom", SpellIds.HexlordMalacrass)]
    [Script("spell_tur_ragepaw_lifebloom", SpellIds.TurragePaw)]
    [Script("spell_cenarion_scout_lifebloom", SpellIds.CenarionScout)]
    [Script("spell_twisted_visage_lifebloom", SpellIds.TwistedVisage)]
    [Script("spell_faction_champion_dru_lifebloom", SpellIds.FactionChampionsDru)]
    class spell_gen_lifebloom : AuraScript
    {
        readonly uint _spellId;

        public spell_gen_lifebloom(uint spellId)
        {
            _spellId = spellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_spellId);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Final heal only on duration end
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire && GetTargetApplication().GetRemoveMode() != AuraRemoveMode.EnemySpell)
                return;

            // final heal
            GetTarget().CastSpell(GetTarget(), _spellId, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
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
                            case SpellIds.TriggerTrialChampion:
                                spellId = SpellIds.Charging20k1;
                                break;
                            case SpellIds.TriggerFactionMounts:
                                spellId = SpellIds.ChargingEffect8k5;
                                break;
                            default:
                                return;
                        }

                        // If target isn't a training dummy there's a chance of failing the charge
                        if (!target.IsCharmedOwnedByPlayerOrPlayer() && RandomHelper.randChance(12.5f))
                            spellId = SpellIds.MissEffect;

                        Unit vehicle = GetCaster().GetVehicleBase();
                        if (vehicle)
                            vehicle.CastSpell(target, spellId, false);
                        else
                            GetCaster().CastSpell(target, spellId, false);
                        break;
                    }
                case 1: // On damaging spells, for removing a defend layer
                case 2:
                    {
                        var auras = target.GetAppliedAuras();
                        foreach (var pair in auras)
                        {
                            Aura aura = pair.Value.GetBase();
                            if (aura != null)
                            {
                                if (aura.GetId() == 62552 || aura.GetId() == 62719 || aura.GetId() == 64100 || aura.GetId() == 66482)
                                {
                                    aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                    // Remove dummys from rider (Necessary for updating visual shields)
                                    Unit rider = target.GetCharmer();
                                    if (rider)
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
            }
        }

        void HandleChargeEffect(uint effIndex)
        {
            uint spellId;

            switch (GetSpellInfo().Id)
            {
                case SpellIds.ChargingEffect8k5:
                    spellId = SpellIds.Damage8k5;
                    break;
                case SpellIds.Charging20k1:
                case SpellIds.Charging20k2:
                    spellId = SpellIds.Damage20k;
                    break;
                case SpellIds.ChargingEffect45k1:
                case SpellIds.ChargingEffect45k2:
                    spellId = SpellIds.Damage45k;
                    break;
                default:
                    return;
            }
            Unit rider = GetCaster().GetCharmer();
            if (rider)
                rider.CastSpell(GetHitUnit(), spellId, false);
            else
                GetCaster().CastSpell(GetHitUnit(), spellId, false);
        }

        public override void Register()
        {
            SpellInfo spell = Global.SpellMgr.GetSpellInfo(m_scriptSpellId, Difficulty.None);

            if (spell.HasEffect(SpellEffectName.ScriptEffect))
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect));

            if (spell.GetEffect(0).IsEffect(SpellEffectName.Charge))
                OnEffectHitTarget.Add(new EffectHandler(HandleChargeEffect, 0, SpellEffectName.Charge));
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
            eventInfo.GetActionTarget().CastSpell((Unit)null, SpellIds.FallDown, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 28702 - Netherbloom
    class spell_gen_netherbloom : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            for (byte i = 0; i < 5; ++i)
                if (!ValidateSpellInfo(SpellIds.NetherBloomPollen1 + i))
                    return false;

            return true;
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Unit target = GetHitUnit();
            if (target)
            {
                // 25% chance of casting a random buff
                if (RandomHelper.randChance(75))
                    return;

                // triggered spells are 28703 to 28707
                // Note: some sources say, that there was the possibility of
                //       receiving a debuff. However, this seems to be Removed by a patch.

                // don't overwrite an existing aura
                for (byte i = 0; i < 5; ++i)
                    if (target.HasAura(SpellIds.NetherBloomPollen1 + i))
                        return;

                target.CastSpell(target, SpellIds.NetherBloomPollen1 + RandomHelper.URand(0, 4), true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            if (target)
            {
                // 25% chance of casting Nightmare Pollen
                if (RandomHelper.randChance(25))
                    target.CastSpell(target, SpellIds.NightmarePollen, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 27539 - Obsidian Armor
    class spell_gen_obsidian_armor : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Holy, SpellIds.Fire, SpellIds.Nature, SpellIds.Frost, SpellIds.Shadow, SpellIds.Arcane);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo() == null)
                return false;

            if (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()) == SpellSchools.Normal)
                return false;

            return true;
        }

        void OnProcEffect(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            uint spellId;
            switch (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()))
            {
                case SpellSchools.Holy:
                    spellId = SpellIds.Holy;
                    break;
                case SpellSchools.Fire:
                    spellId = SpellIds.Fire;
                    break;
                case SpellSchools.Nature:
                    spellId = SpellIds.Nature;
                    break;
                case SpellSchools.Frost:
                    spellId = SpellIds.Frost;
                    break;
                case SpellSchools.Shadow:
                    spellId = SpellIds.Shadow;
                    break;
                case SpellSchools.Arcane:
                    spellId = SpellIds.Arcane;
                    break;
                default:
                    return;
            }
            GetTarget().CastSpell(GetTarget(), spellId, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(OnProcEffect, 0, AuraType.Dummy));
        }
    }

    [Script]
    class spell_gen_oracle_wolvar_reputation : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 1;
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleDummy(uint effIndex)
        {
            Player player = GetCaster().ToPlayer();
            uint factionId = (uint)GetEffectInfo().CalcValue();
            int repChange = GetEffectInfo(1).CalcValue();

            FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(factionId);

            if (factionEntry == null)
                return;

            // Set rep to baserep + basepoints (expecting spillover for oposite faction . become hated)
            // Not when player already has equal or higher rep with this faction
            if (player.GetReputationMgr().GetBaseReputation(factionEntry) < repChange)
                player.GetReputationMgr().SetReputation(factionEntry, repChange);

            // EFFECT_INDEX_2 most likely update at war state, we already handle this in SetReputation
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            if (target)
            {
                Gender gender = target.GetNativeGender();
                if (gender == Gender.Male)
                    caster.CastSpell(target, SpellIds.OrcDisguiseMale, true);
                else
                    caster.CastSpell(target, SpellIds.OrcDisguiseFemale, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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

            GetTarget().CastSpell((Unit)null, SpellIds.Paralysis, new CastSpellExtraArgs(aurEff));
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(HandleStun, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
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
            OnEffectApply.Add(new EffectApplyHandler(HandleEffectApply, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
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
            OnProc.Add(new AuraProcHandler(HandleChargeDrop));
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
            if (target)
            {
                if (target.IsFalling())
                {
                    target.RemoveAurasDueToSpell(SpellIds.Parachute);
                    target.CastSpell(target, SpellIds.ParachuteBuff, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_gen_pet_summoned : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleScript(uint effIndex)
        {
            Player player = GetCaster().ToPlayer();
            if (player.GetLastPetNumber() != 0)
            {
                PetType newPetType = (player.GetClass() == Class.Hunter) ? PetType.Hunter : PetType.Summon;
                Pet newPet = new(player, newPetType);
                if (newPet.LoadPetFromDB(player, 0, player.GetLastPetNumber(), true))
                {
                    // revive the pet if it is dead
                    if (newPet.GetDeathState() != DeathState.Alive && newPet.GetDeathState() != DeathState.JustRespawned)
                        newPet.SetDeathState(DeathState.JustRespawned);

                    newPet.SetFullHealth();
                    newPet.SetFullPower(newPet.GetPowerType());

                    var summonScript = GetSpell().GetSpellScripts<IOnSummon>();

                    foreach (IOnSummon summon in summonScript)
                        summon.HandleSummon(newPet);

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
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            OnEffectHit.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_profession_research : SpellScript, ICheckCastHander
    {
        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public SpellCastResult CheckCast()
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
                    uint _discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);
                    if (_discoveredSpellId != 0)
                        caster.LearnSpell(_discoveredSpellId, false);
                }
            }

            uint discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);
            if (discoveredSpellId != 0)
                caster.LearnSpell(discoveredSpellId, false);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_pvp_trinket : SpellScript, IAfterCast
    {
        public void AfterCast()
        {
            Player caster = GetCaster().ToPlayer();

            switch (caster.GetEffectiveTeam())
            {
                case Team.Alliance:
                    caster.CastSpell(caster, SpellIds.PvpTrinketAlliance, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
                    break;
                case Team.Horde:
                    caster.CastSpell(caster, SpellIds.PvpTrinketHorde, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
                    break;
            }
        }
    }

    [Script]
    class spell_gen_remove_flight_auras : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
            {
                target.RemoveAurasByType(AuraType.Fly);
                target.RemoveAurasByType(AuraType.ModIncreaseMountedFlightSpeed);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_remove_impairing_auras : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            GetHitUnit().RemoveMovementImpairingAuras(true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
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
            return spellInfo.GetEffects().Count > 1;
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 2, AuraType.PeriodicDamagePercent));
        }
    }

    [Script]
    class spell_gen_replenishment : SpellScript
    {
        void RemoveInvalidTargets(List<WorldObject> targets)
        {
            // In arenas Replenishment may only affect the caster
            Player caster = GetCaster().ToPlayer();
            if (caster)
            {
                if (caster.InArena())
                {
                    targets.Clear();
                    targets.Add(caster);
                    return;
                }
            }

            targets.RemoveAll(obj =>
            {
                var target = obj.ToUnit();
                if (target)
                    return target.GetPowerType() != PowerType.Mana;

                return true;
            });

            byte maxTargets = 10;

            if (targets.Count > maxTargets)
            {
                targets.Sort(new PowerPctOrderPred(PowerType.Mana));
                targets.Resize(maxTargets);
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 255, Targets.UnitCasterAreaRaid));
        }
    }

    [Script]
    class spell_gen_replenishment_AuraScript : AuraScript
    {
        public override bool Load()
        {
            return GetUnitOwner().GetPower(PowerType.Mana) != 0;
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
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicEnergize));
        }
    }

    [Script]
    class spell_gen_running_wild : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AlteredForm);
        }

        public override bool Load()
        {
            // Definitely not a good thing, but currently the only way to do something at cast start
            // Should be replaced as soon as possible with a new hook: BeforeCastStart
            GetCaster().CastSpell(GetCaster(), SpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            return false;
        }

        public override void Register()
        {
        }
    }

    [Script]
    class spell_gen_running_wild_AuraScript : AuraScript
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
            MountCapabilityRecord mountCapability = CliDB.MountCapabilityStorage.LookupByKey(aurEff.GetAmount());
            if (mountCapability != null)
                target.CastSpell(target, mountCapability.ModSpellAuraID, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleMount, 1, AuraType.Mounted, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_two_forms : SpellScript, ICheckCastHander
    {
        public SpellCastResult CheckCast()
        {
            if (GetCaster().IsInCombat())
            {
                SetCustomCastResultMessage(SpellCustomErrors.CantTransform);
                return SpellCastResult.CustomError;
            }

            // Player cannot transform to human form if he is forced to be worgen for some reason (Darkflight)
            if (GetCaster().GetAuraEffectsByType(AuraType.WorgenAlteredForm).Count > 1)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleTransform, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_darkflight : SpellScript, IAfterCast
    {
        public void AfterCast()
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
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
            // OriginalCaster is always available in Spell.prepare
            return GetGObjCaster().GetOwnerGUID().IsPlayer();
        }

        void AchievementCredit(uint effIndex)
        {
            // but in effect handling OriginalCaster can become null
            Unit owner = GetGObjCaster().GetOwner();
            if (owner != null)
            {
                GameObject go = GetHitGObj();
                if (go)
                    if (go.GetGoInfo().type == GameObjectTypes.DestructibleBuilding)
                        owner.CastSpell(null, SpellIds.PlantChargesCreditAchievement, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(AchievementCredit, 1, SpellEffectName.GameObjectDamage));
        }
    }

    [Script]
    class spell_gen_spectator_cheer_trigger : SpellScript
    {
        readonly static Emote[] EmoteArray = { Emote.OneshotCheer, Emote.OneshotExclamation, Emote.OneshotApplaud };

        void HandleDummy(uint effIndex)
        {
            GetCaster().HandleEmoteCommand(EmoteArray.SelectRandom());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_spirit_healer_res : SpellScript
    {
        public override bool Load()
        {
            return GetOriginalCaster() && GetOriginalCaster().IsTypeId(TypeId.Player);
        }

        void HandleDummy(uint effIndex)
        {
            Player originalCaster = GetOriginalCaster().ToPlayer();
            Unit target = GetHitUnit();
            if (target)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_summon_tournament_mount : SpellScript, ICheckCastHander
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.LanceEquipped);
        }

        public SpellCastResult CheckCast()
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect));
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
            if (rider)
            {
                Player playerTarget = GetHitPlayer();
                if (playerTarget)
                {
                    if (playerTarget.HasAura(SpellIds.OnTournamentMount) && playerTarget.GetVehicleBase())
                        rider.CastSpell(playerTarget, SpellIds.MountedDuel, true);
                    return;
                }

                Unit unitTarget = GetHitUnit();
                if (unitTarget)
                {
                    if (unitTarget.GetCharmer() && unitTarget.GetCharmer().IsTypeId(TypeId.Player) && unitTarget.GetCharmer().HasAura(SpellIds.OnTournamentMount))
                        rider.CastSpell(unitTarget.GetCharmer(), SpellIds.MountedDuel, true);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_tournament_pennant : AuraScript
    {
        public override bool Load()
        {
            return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
        }

        void HandleApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();
            if (caster)
                if (!caster.GetVehicleBase())
                    caster.RemoveAurasDueToSpell(GetId());
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(HandleApplyEffect, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
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
            if (target.ToPlayer().GetAreaId() == Misc.AreaVioletCitadelSpire)
                target.CastSpell(target, SpellIds.TeleportSpireDown, true);
            // teleport atop
            else
                target.CastSpell(target, SpellIds.TeleportSpireUp, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_trigger_exclude_caster_aura_spell : SpellScript, IAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(spellInfo.ExcludeCasterAuraSpell);
        }

        public void AfterCast()
        {
            // Blizz seems to just apply aura without bothering to cast
            GetCaster().AddAura(GetSpellInfo().ExcludeCasterAuraSpell, GetCaster());
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
            if (target)
                // Blizz seems to just apply aura without bothering to cast
                GetCaster().AddAura(GetSpellInfo().ExcludeTargetAuraSpell, target);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleTrigger));
        }
    }

    [Script("spell_pvp_trinket_shared_cd", SpellIds.WillOfTheForsakenCooldownTrigger)]
    [Script("spell_wotf_shared_cd", SpellIds.WillOfTheForsakenCooldownTriggerWotf)]
    class spell_pvp_trinket_wotf_shared_cd : SpellScript, IAfterCast
    {
        readonly uint _triggered;

        public spell_pvp_trinket_wotf_shared_cd(uint triggered)
        {
            _triggered = triggered;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_triggered);
        }

        public void AfterCast()
        {
            /*
             * @workaround: PendingCast flag normally means 'triggered' spell, however
             * if the spell is cast triggered, the core won't send SMSG_SPELL_GO packet
             * so client never registers the cooldown (see Spell::IsNeedSendToClient)
             *
             * ServerToClient: SMSG_SPELL_GO (0x0132) Length: 42 ConnIdx: 0 Time: 07/19/2010 02:32:35.000 Number: 362675
             * Caster GUID: Full: Player
             * Caster Unit GUID: Full: Player
             * Cast Count: 0
             * Spell ID: 72752 (72752)
             * Cast Flags: PendingCast, Unknown3, Unknown7 (265)
             * Time: 3901468825
             * Hit Count: 1
             * [0] Hit GUID: Player
             * Miss Count: 0
             * Target Flags: Unit (2)
             * Target GUID: 0x0
            */

            // Spell flags need further research, until then just cast not triggered
            GetCaster().CastSpell((Unit)null, _triggered, false);
        }
    }

    [Script]
    class spell_gen_turkey_marker : AuraScript
    {
        readonly List<uint> _applyTimes = new();

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // store stack apply times, so we can pop them while they expire
            _applyTimes.Add(GameTime.GetGameTimeMS());
            Unit target = GetTarget();

            // on stack 15 cast the achievement crediting spell
            if (GetStackAmount() >= 15)
                target.CastSpell(target, SpellIds.TurkeyVengeance, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            int removeCount = 0;

            // pop expired times off of the stack
            while (!_applyTimes.Empty() && _applyTimes.FirstOrDefault() + GetMaxDuration() < GameTime.GetGameTimeMS())
            {
                _applyTimes.RemoveAt(0);
                removeCount++;
            }

            if (removeCount != 0)
                ModStackAmount(-removeCount, AuraRemoveMode.Expire);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_gen_upper_deck_create_foam_sword : SpellScript
    {
        //                       green  pink   blue   red    yellow
        readonly static uint[] itemId = { 45061, 45176, 45177, 45178, 45179 };

        void HandleScript(uint effIndex)
        {
            Player player = GetHitPlayer();
            if (player)
            {
                // player can only have one of these items
                for (byte i = 0; i < 5; ++i)
                {
                    if (player.HasItemCount(itemId[i], 1, true))
                        return;
                }

                CreateItem(itemId[RandomHelper.URand(0, 4)], ItemContext.None);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage() / 2);
            caster.CastSpell(caster, SpellIds.VampiricTouchHeal, args);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script]
    class spell_gen_vehicle_scaling : AuraScript
    {
        public override bool Load()
        {
            return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit caster = GetCaster();
            float factor;
            ushort baseItemLevel;

            // @todo Reserach coeffs for different vehicles
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
                return;                     // @todo Research possibility of scaling down

            amount = (int)((avgILvl - baseItemLevel) * factor);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModHealingPct));
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModDamagePercentDone));
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 2, AuraType.ModIncreaseHealthPercent));
        }
    }

    [Script]
    class spell_gen_vendor_bark_trigger : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Creature vendor = GetCaster().ToCreature();
            if (vendor)
                if (vendor.GetEntry() == CreatureIds.AmphitheaterVendor)
                    vendor.GetAI().Talk(TextIds.SayAmphitheaterVendor);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_wg_water : SpellScript, ICheckCastHander
    {
        public SpellCastResult CheckCast()
        {
            if (!GetSpellInfo().CheckTargetCreatureType(GetCaster()))
                return SpellCastResult.DontReport;
            return SpellCastResult.SpellCastOk;
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
            GetTarget().CastSpell((Unit)null, SpellIds.YoggSaronWhisperDummy, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
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
            OnEffectHit.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // BasePoints of spells is ID of npc_text used to group texts, it's not implemented so texts are grouped the old way
    // 50037 - Mystery of the Infinite: Future You's Whisper to Controller - Random
    // 50287 - Azure Dragon: On Death Force Cast Wyrmrest Defender to Whisper to Controller - Random
    // 60709 - MOTI, Redux: Past You's Whisper to Controller - Random
    [Script("spell_future_you_whisper_to_controller_random", 2u)]
    [Script("spell_wyrmrest_defender_whisper_to_controller_random", 1u)]
    [Script("spell_past_you_whisper_to_controller_random", 2u)]
    class spell_gen_whisper_to_controller_random : SpellScript
    {
        uint _text;

        public spell_gen_whisper_to_controller_random(uint text)
        {
            _text = text;
        }

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
                        targetSummon.GetAI().Talk(_text, player);
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_eject_all_passengers : SpellScript
    {
        void RemoveVehicleAuras()
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();
            if (vehicle)
                vehicle.RemoveAllPassengers();
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(RemoveVehicleAuras));
        }
    }

    [Script]
    class spell_gen_eject_passenger : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            if (spellInfo.GetEffects().Empty())
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
                if (passenger)
                    passenger.ExitVehicle();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(EjectPassenger, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script("spell_gen_eject_passenger_1", 0)]
    [Script("spell_gen_eject_passenger_3", 2)]
    class spell_gen_eject_passenger_with_seatId : SpellScript
    {
        sbyte _seatId;

        public spell_gen_eject_passenger_with_seatId(sbyte seatId)
        {
            _seatId = seatId;
        }

        void EjectPassenger(uint effIndex)
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();
            if (vehicle != null)
            {
                Unit passenger = vehicle.GetPassenger(_seatId);
                if (passenger != null)
                    passenger.ExitVehicle();
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(EjectPassenger, 0, SpellEffectName.ScriptEffect));
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
            if (player)
            {
                // stop combat + make player unattackable + duel stop + stop some spells
                player.SetFaction(35);
                player.CombatStop();
                if (player.IsNonMeleeSpellCast(true))
                    player.InterruptNonMeleeSpells(true);
                player.SetUnitFlag(UnitFlags.NonAttackable);

                // if player class = hunter || warlock Remove pet if alive
                if ((player.GetClass() == Class.Hunter) || (player.GetClass() == Class.Warlock))
                {
                    Pet pet = player.GetPet();
                    if (pet)
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
            if (player)
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
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_gen_stand : SpellScript
    {
        void HandleScript(uint eff)
        {
            Creature target = GetHitCreature();
            if (!target)
                return;

            target.SetStandState(UnitStandStateType.Stand);
            target.HandleEmoteCommand(Emote.StateNone);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    enum RequiredMixologySpells
    {
        Mixology = 53042,
        // Flasks
        FlaskOfTheFrostWyrm = 53755,
        FlaskOfStoneblood = 53758,
        FlaskOfEndlessRage = 53760,
        FlaskOfPureMojo = 54212,
        LesserFlaskOfResistance = 62380,
        LesserFlaskOfToughness = 53752,
        FlaskOfBlindingLight = 28521,
        FlaskOfChromaticWonder = 42735,
        FlaskOfFortification = 28518,
        FlaskOfMightyRestoration = 28519,
        FlaskOfPureDeath = 28540,
        FlaskOfRelentlessAssault = 28520,
        FlaskOfChromaticResistance = 17629,
        FlaskOfDistilledWisdom = 17627,
        FlaskOfSupremePower = 17628,
        FlaskOfTheTitans = 17626,
        // Elixirs
        ElixirOfMightyAgility = 28497,
        ElixirOfAccuracy = 60340,
        ElixirOfDeadlyStrikes = 60341,
        ElixirOfMightyDefense = 60343,
        ElixirOfExpertise = 60344,
        ElixirOfArmorPiercing = 60345,
        ElixirOfLightningSpeed = 60346,
        ElixirOfMightyFortitude = 53751,
        ElixirOfMightyMageblood = 53764,
        ElixirOfMightyStrength = 53748,
        ElixirOfMightyToughts = 60347,
        ElixirOfProtection = 53763,
        ElixirOfSpirit = 53747,
        GurusElixir = 53749,
        ShadowpowerElixir = 33721,
        WrathElixir = 53746,
        ElixirOfEmpowerment = 28514,
        ElixirOfMajorMageblood = 28509,
        ElixirOfMajorShadowPower = 28503,
        ElixirOfMajorDefense = 28502,
        FelStrengthElixir = 38954,
        ElixirOfIronskin = 39628,
        ElixirOfMajorAgility = 54494,
        ElixirOfDraenicWisdom = 39627,
        ElixirOfMajorFirepower = 28501,
        ElixirOfMajorFrostPower = 28493,
        EarthenElixir = 39626,
        ElixirOfMastery = 33726,
        ElixirOfHealingPower = 28491,
        ElixirOfMajorFortitude = 39625,
        ElixirOfMajorStrength = 28490,
        AdeptsElixir = 54452,
        OnslaughtElixir = 33720,
        MightyTrollsBloodElixir = 24361,
        GreaterArcaneElixir = 17539,
        ElixirOfTheMongoose = 17538,
        ElixirOfBruteForce = 17537,
        ElixirOfSages = 17535,
        ElixirOfSuperiorDefense = 11348,
        ElixirOfDemonslaying = 11406,
        ElixirOfGreaterFirepower = 26276,
        ElixirOfShadowPower = 11474,
        MagebloodElixir = 24363,
        ElixirOfGiants = 11405,
        ElixirOfGreaterAgility = 11334,
        ArcaneElixir = 11390,
        ElixirOfGreaterIntellect = 11396,
        ElixirOfGreaterDefense = 11349,
        ElixirOfFrostPower = 21920,
        ElixirOfAgility = 11328,
        MajorTrollsBlloodElixir = 3223,
        ElixirOfFortitude = 3593,
        ElixirOfOgresStrength = 3164,
        ElixirOfFirepower = 7844,
        ElixirOfLesserAgility = 3160,
        ElixirOfDefense = 3220,
        StrongTrollsBloodElixir = 3222,
        ElixirOfMinorAccuracy = 63729,
        ElixirOfWisdom = 3166,
        ElixirOfGianthGrowth = 8212,
        ElixirOfMinorAgility = 2374,
        ElixirOfMinorFortitude = 2378,
        WeakTrollsBloodElixir = 3219,
        ElixirOfLionsStrength = 2367,
        ElixirOfMinorDefense = 673
    }

    [Script]
    class spell_gen_mixology_bonus : AuraScript
    {
        int bonus;

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)RequiredMixologySpells.Mixology) && !spellInfo.GetEffects().Empty();
        }

        public override bool Load()
        {
            return GetCaster() && GetCaster().GetTypeId() == TypeId.Player;
        }

        void SetBonusValueForEffect(uint effIndex, int value, AuraEffect aurEff)
        {
            if (aurEff.GetEffIndex() == effIndex)
                bonus = value;
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster().HasAura((uint)RequiredMixologySpells.Mixology) && GetCaster().HasSpell(GetEffectInfo(0).TriggerSpell))
            {
                switch ((RequiredMixologySpells)GetId())
                {
                    case RequiredMixologySpells.WeakTrollsBloodElixir:
                    case RequiredMixologySpells.MagebloodElixir:
                        bonus = amount;
                        break;
                    case RequiredMixologySpells.ElixirOfFrostPower:
                    case RequiredMixologySpells.LesserFlaskOfToughness:
                    case RequiredMixologySpells.LesserFlaskOfResistance:
                        bonus = MathFunctions.CalculatePct(amount, 80);
                        break;
                    case RequiredMixologySpells.ElixirOfMinorDefense:
                    case RequiredMixologySpells.ElixirOfLionsStrength:
                    case RequiredMixologySpells.ElixirOfMinorAgility:
                    case RequiredMixologySpells.MajorTrollsBlloodElixir:
                    case RequiredMixologySpells.ElixirOfShadowPower:
                    case RequiredMixologySpells.ElixirOfBruteForce:
                    case RequiredMixologySpells.MightyTrollsBloodElixir:
                    case RequiredMixologySpells.ElixirOfGreaterFirepower:
                    case RequiredMixologySpells.OnslaughtElixir:
                    case RequiredMixologySpells.EarthenElixir:
                    case RequiredMixologySpells.ElixirOfMajorAgility:
                    case RequiredMixologySpells.FlaskOfTheTitans:
                    case RequiredMixologySpells.FlaskOfRelentlessAssault:
                    case RequiredMixologySpells.FlaskOfStoneblood:
                    case RequiredMixologySpells.ElixirOfMinorAccuracy:
                        bonus = MathFunctions.CalculatePct(amount, 50);
                        break;
                    case RequiredMixologySpells.ElixirOfProtection:
                        bonus = 280;
                        break;
                    case RequiredMixologySpells.ElixirOfMajorDefense:
                        bonus = 200;
                        break;
                    case RequiredMixologySpells.ElixirOfGreaterDefense:
                    case RequiredMixologySpells.ElixirOfSuperiorDefense:
                        bonus = 140;
                        break;
                    case RequiredMixologySpells.ElixirOfFortitude:
                        bonus = 100;
                        break;
                    case RequiredMixologySpells.FlaskOfEndlessRage:
                        bonus = 82;
                        break;
                    case RequiredMixologySpells.ElixirOfDefense:
                        bonus = 70;
                        break;
                    case RequiredMixologySpells.ElixirOfDemonslaying:
                        bonus = 50;
                        break;
                    case RequiredMixologySpells.FlaskOfTheFrostWyrm:
                        bonus = 47;
                        break;
                    case RequiredMixologySpells.WrathElixir:
                        bonus = 32;
                        break;
                    case RequiredMixologySpells.ElixirOfMajorFrostPower:
                    case RequiredMixologySpells.ElixirOfMajorFirepower:
                    case RequiredMixologySpells.ElixirOfMajorShadowPower:
                        bonus = 29;
                        break;
                    case RequiredMixologySpells.ElixirOfMightyToughts:
                        bonus = 27;
                        break;
                    case RequiredMixologySpells.FlaskOfSupremePower:
                    case RequiredMixologySpells.FlaskOfBlindingLight:
                    case RequiredMixologySpells.FlaskOfPureDeath:
                    case RequiredMixologySpells.ShadowpowerElixir:
                        bonus = 23;
                        break;
                    case RequiredMixologySpells.ElixirOfMightyAgility:
                    case RequiredMixologySpells.FlaskOfDistilledWisdom:
                    case RequiredMixologySpells.ElixirOfSpirit:
                    case RequiredMixologySpells.ElixirOfMightyStrength:
                    case RequiredMixologySpells.FlaskOfPureMojo:
                    case RequiredMixologySpells.ElixirOfAccuracy:
                    case RequiredMixologySpells.ElixirOfDeadlyStrikes:
                    case RequiredMixologySpells.ElixirOfMightyDefense:
                    case RequiredMixologySpells.ElixirOfExpertise:
                    case RequiredMixologySpells.ElixirOfArmorPiercing:
                    case RequiredMixologySpells.ElixirOfLightningSpeed:
                        bonus = 20;
                        break;
                    case RequiredMixologySpells.FlaskOfChromaticResistance:
                        bonus = 17;
                        break;
                    case RequiredMixologySpells.ElixirOfMinorFortitude:
                    case RequiredMixologySpells.ElixirOfMajorStrength:
                        bonus = 15;
                        break;
                    case RequiredMixologySpells.FlaskOfMightyRestoration:
                        bonus = 13;
                        break;
                    case RequiredMixologySpells.ArcaneElixir:
                        bonus = 12;
                        break;
                    case RequiredMixologySpells.ElixirOfGreaterAgility:
                    case RequiredMixologySpells.ElixirOfGiants:
                        bonus = 11;
                        break;
                    case RequiredMixologySpells.ElixirOfAgility:
                    case RequiredMixologySpells.ElixirOfGreaterIntellect:
                    case RequiredMixologySpells.ElixirOfSages:
                    case RequiredMixologySpells.ElixirOfIronskin:
                    case RequiredMixologySpells.ElixirOfMightyMageblood:
                        bonus = 10;
                        break;
                    case RequiredMixologySpells.ElixirOfHealingPower:
                        bonus = 9;
                        break;
                    case RequiredMixologySpells.ElixirOfDraenicWisdom:
                    case RequiredMixologySpells.GurusElixir:
                        bonus = 8;
                        break;
                    case RequiredMixologySpells.ElixirOfFirepower:
                    case RequiredMixologySpells.ElixirOfMajorMageblood:
                    case RequiredMixologySpells.ElixirOfMastery:
                        bonus = 6;
                        break;
                    case RequiredMixologySpells.ElixirOfLesserAgility:
                    case RequiredMixologySpells.ElixirOfOgresStrength:
                    case RequiredMixologySpells.ElixirOfWisdom:
                    case RequiredMixologySpells.ElixirOfTheMongoose:
                        bonus = 5;
                        break;
                    case RequiredMixologySpells.StrongTrollsBloodElixir:
                    case RequiredMixologySpells.FlaskOfChromaticWonder:
                        bonus = 4;
                        break;
                    case RequiredMixologySpells.ElixirOfEmpowerment:
                        bonus = -10;
                        break;
                    case RequiredMixologySpells.AdeptsElixir:
                        SetBonusValueForEffect(0, 13, aurEff);
                        SetBonusValueForEffect(1, 13, aurEff);
                        SetBonusValueForEffect(2, 8, aurEff);
                        break;
                    case RequiredMixologySpells.ElixirOfMightyFortitude:
                        SetBonusValueForEffect(0, 160, aurEff);
                        break;
                    case RequiredMixologySpells.ElixirOfMajorFortitude:
                        SetBonusValueForEffect(0, 116, aurEff);
                        SetBonusValueForEffect(1, 6, aurEff);
                        break;
                    case RequiredMixologySpells.FelStrengthElixir:
                        SetBonusValueForEffect(0, 40, aurEff);
                        SetBonusValueForEffect(1, 40, aurEff);
                        break;
                    case RequiredMixologySpells.FlaskOfFortification:
                        SetBonusValueForEffect(0, 210, aurEff);
                        SetBonusValueForEffect(1, 5, aurEff);
                        break;
                    case RequiredMixologySpells.GreaterArcaneElixir:
                        SetBonusValueForEffect(0, 19, aurEff);
                        SetBonusValueForEffect(1, 19, aurEff);
                        SetBonusValueForEffect(2, 5, aurEff);
                        break;
                    case RequiredMixologySpells.ElixirOfGianthGrowth:
                        SetBonusValueForEffect(0, 5, aurEff);
                        break;
                    default:
                        Log.outError(LogFilter.Spells, "SpellId {0} couldn't be processed in spell_gen_mixology_bonus", GetId());
                        break;
                }
                amount += bonus;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, SpellConst.EffectAll, AuraType.Any));
        }
    }

    [Script]
    class spell_gen_landmine_knockback_achievement : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();
            if (target)
            {
                Aura aura = GetHitAura();
                if (aura == null || aura.GetStackAmount() < 10)
                    return;

                target.CastSpell(target, SpellIds.LandmineKnockbackAchievement, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 34098 - ClearAllDebuffs
    class spell_gen_clear_debuffs : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_gen_pony_mount_check : AuraScript
    {
        void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (!caster)
                return;

            Player owner = caster.GetOwner().ToPlayer();
            if (!owner || !owner.HasAchieved(SpellIds.AchievementPonyup))
                return;

            if (owner.IsMounted())
            {
                caster.Mount(SpellIds.MountPony);
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    // 40350 - Corrupting Plague
    class CorruptingPlagueSearcher : ICheck<Unit>
    {
        public CorruptingPlagueSearcher(Unit obj, float distance)
        {
            _unit = obj;
            _distance = distance;
        }

        public bool Invoke(Unit u)
        {
            if (_unit.GetDistance2d(u) < _distance &&
                (u.GetEntry() == CreatureIds.ApexisFlayer || u.GetEntry() == CreatureIds.ShardHideBoar || u.GetEntry() == CreatureIds.AetherRay) &&
                !u.HasAura(SpellIds.CorruptingPlague))
                return true;

            return false;
        }

        Unit _unit;
        float _distance;
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
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
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
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
                spellId = map.GetId() == Misc.MapIdBloodInTheSnowScenario ? SpellIds.FreezingCircleScenario : SpellIds.FreezingCircle;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());
            if (spellInfo != null)
                if (!spellInfo.GetEffects().Empty())
                    SetHitDamage(spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage));
        }
    }

    [Script] // Used for some spells cast by vehicles or charmed creatures that do not send a cooldown event on their own
    class spell_gen_charmed_unit_spell_cooldown : SpellScript, IOnCast
    {
        public void OnCast()
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 37751 - Submerged
    class spell_gen_submerged : SpellScript
    {
        void HandleScript(uint eff)
        {
            Creature target = GetHitCreature();
            if (target != null)
                target.SetStandState(UnitStandStateType.Submerged);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 169869 - Transformation Sickness
    class spell_gen_decimatus_transformation_sickness : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                target.SetHealth(target.CountPctFromMaxHealth(25));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_gen_mark_of_kazrogal_hellfire : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(target =>
            {
                Unit unit = target.ToUnit();
                if (unit)
                    return unit.GetPowerType() != PowerType.Mana;
                return false;
            });
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        }
    }

    [Script]
    class spell_gen_mark_of_kazrogal_hellfire_AuraScript : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.MarkOfKazrogalDamageHellfire);
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();

            if (target.GetPower(PowerType.Mana) == 0)
            {
                target.CastSpell(target, SpellIds.MarkOfKazrogalDamageHellfire, new CastSpellExtraArgs(aurEff));
                // Remove aura
                SetDuration(0);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PowerBurn));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 99947 - Face Rage
    class spell_gen_face_rage : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FaceRage) && spellInfo.GetEffects().Count > 2;
        }

        void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(GetEffectInfo(2).TriggerSpell);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 187213 - Impatient Mind
    class spell_gen_impatient_mind : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.ImpatientMind);
        }

        void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(effect.GetSpellEffectInfo().TriggerSpell);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real));
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
                case TeamId.Alliance:
                    amount = Global.WorldStateMgr.GetValue(WorldStates.WarModeAllianceBuffValue, target.GetMap());
                    break;
                case TeamId.Horde:
                    amount = Global.WorldStateMgr.GetValue(WorldStates.WarModeHordeBuffValue, target.GetMap());
                    break;
            }
        }

        public override void Register()
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(m_scriptSpellId, Difficulty.None);

            if (spellInfo.HasAura(AuraType.ModXpPct))
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpPct));

            if (spellInfo.HasAura(AuraType.ModXpQuestPct))
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpQuestPct));

            if (spellInfo.HasAura(AuraType.ModCurrencyGainFromSource))
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModCurrencyGainFromSource));

            if (spellInfo.HasAura(AuraType.ModMoneyGain))
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModMoneyGain));

            if (spellInfo.HasAura(AuraType.ModAnimaGain))
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModAnimaGain));

            if (spellInfo.HasAura(AuraType.Dummy))
                DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.Dummy));
        }
    }

    [Script]
    class spell_defender_of_azeroth_death_gate_selector : SpellScript
    {
        (WorldLocation, uint) StormwindInnLoc = (new WorldLocation(0, -8868.1f, 675.82f, 97.9f, 5.164778709411621093f), 5148);
        (WorldLocation, uint) OrgrimmarInnLoc = (new WorldLocation(1, 1573.18f, -4441.62f, 16.06f, 1.818284034729003906f), 8618);

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.DeathGateTeleportStormwind, SpellIds.DeathGateTeleportOrgrimmar);
        }

        void HandleDummy(uint effIndex)
        {
            Player player = GetHitUnit().ToPlayer();
            if (player == null)
                return;

            if (player.GetQuestStatus(QuestIds.DefenderOfAzerothAlliance) == QuestStatus.None && player.GetQuestStatus(QuestIds.DefenderOfAzerothHorde) == QuestStatus.None)
                return;

            (WorldLocation Loc, uint AreaId) bindLoc = player.GetTeam() == Team.Alliance ? StormwindInnLoc : OrgrimmarInnLoc;
            player.SetHomebind(bindLoc.Loc, bindLoc.AreaId);
            player.SendBindPointUpdate();
            player.SendPlayerBound(player.GetGUID(), bindLoc.AreaId);

            player.CastSpell(player, player.GetTeam() == Team.Alliance ? SpellIds.DeathGateTeleportStormwind : SpellIds.DeathGateTeleportOrgrimmar);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_defender_of_azeroth_speak_with_mograine : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            if (!GetCaster())
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

            // @TODO: spawntracking - show death gate for casting player
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]// 118301 - Summon Battle Pet
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
                uint duration = (uint)GetSpellInfo().CalcDuration(caster);
                Position pos = GetHitDest().GetPosition();

                Creature summon = caster.GetMap().SummonCreature(creatureId, pos, properties, duration, caster, GetSpellInfo().Id);
                if (summon != null)
                    summon.SetImmuneToAll(true);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Summon));
        }
    }

    [Script] // 132334 - Trainer Heal Cooldown (SERVERSIDE)
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
                TimeSpan expectedCooldown = TimeSpan.FromMilliseconds(GetAura().GetMaxDuration());
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
            OnEffectApply.Add(new EffectApplyHandler(UpdateReviveBattlePetCooldown, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }
    }

    [Script] // 274738 - Ancestral Call (Mag'har Orc Racial)
    class spell_gen_ancestral_call : SpellScript, IOnCast
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.RictusOfTheLaughingSkull, SpellIds.ZealOfTheBurningBlade, SpellIds.FerocityOfTheFrostwolf, SpellIds.MightOfTheBlackrock);
        }

        static uint[] AncestralCallBuffs = { SpellIds.RictusOfTheLaughingSkull, SpellIds.ZealOfTheBurningBlade, SpellIds.FerocityOfTheFrostwolf, SpellIds.MightOfTheBlackrock };

        public void OnCast()
        {
            Unit caster = GetCaster();
            uint spellId = AncestralCallBuffs.SelectRandom();

            caster.CastSpell(caster, spellId, true);
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
                vehicle.GetPassenger(i)?.ExitVehicle();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 83781 - Reverse Cast Ride Vehicle
    class spell_gen_reverse_cast_target_to_caster_triggered : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            GetHitUnit().CastSpell(GetCaster(), (uint)GetSpellInfo().GetEffect(effIndex).CalcValue(), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // Note: this spell unsummons any creature owned by the caster. Set appropriate target conditions on the DB.
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
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 40307 - Stasis Field
    class StasisFieldSearcher : ICheck<Unit>
    {
        Unit _unit;
        float _distance;

        public StasisFieldSearcher(Unit obj, float distance)
        {
            _unit = obj;
            _distance = distance;
        }

        public bool Invoke(Unit u)
        {
            if (_unit.GetDistance2d(u) < _distance &&
                (u.GetEntry() == CreatureIds.ApexisFlayer || u.GetEntry() == CreatureIds.ShardHideBoar || u.GetEntry() == CreatureIds.AetherRay || u.GetEntry() == CreatureIds.DaggertailLizard) &&
                !u.HasAura(SpellIds.StasisField))
                return true;

            return false;
        }
    }
}