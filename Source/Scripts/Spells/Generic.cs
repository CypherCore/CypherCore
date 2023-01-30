// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Scripts.Spells.Generic
{
    internal struct SpellIds
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

    internal struct CreatureIds
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

    internal struct ModelIds
    {
        //ServiceUniform
        public const uint GoblinMale = 31002;
        public const uint GoblinFemale = 31003;
    }

    internal struct TextIds
    {
        //EtherealPet
        public const uint SayStealEssence = 1;
        public const uint SayCreateToken = 2;

        //VendorBarkTrigger
        public const uint SayAmphitheaterVendor = 0;
    }

    internal struct EmoteIds
    {
        //FuriousRage
        public const uint FuriousRage = 19415;
        public const uint Exhausted = 18368;
    }

    internal struct AchievementIds
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

    internal struct QuestIds
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

    internal struct Misc
    {
        // FungalDecay
        public const int AuraDuration = 12600; // found in sniffs, there is no duration entry we can possibly use

        // FreezingCircleMisc
        public const uint MapIdBloodInTheSnowScenario = 1130;

        // Teleporting
        public const uint AreaVioletCitadelSpire = 4637;
    }

    [Script]
    internal class spell_gen_absorb0_hitlimit1 : AuraScript, IHasAuraEffects
    {
        private int limit;
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Load()
        {
            // Max Absorb stored in 1 dummy effect
            limit = GetSpellInfo().GetEffect(1).CalcValue();

            return true;
        }

        public override void Register()
        {
            Effects.Add(new EffectAbsorbHandler(Absorb, 0, false, AuraScriptHookType.EffectAbsorb));
        }

        private void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            absorbAmount = (uint)Math.Min(limit, absorbAmount);
        }
    }

    [Script] // 28764 - Adaptive Warding (Frostfire Regalia Set)
    internal class spell_gen_adaptive_warding : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GenAdaptiveWardingFire, SpellIds.GenAdaptiveWardingNature, SpellIds.GenAdaptiveWardingFrost, SpellIds.GenAdaptiveWardingShadow, SpellIds.GenAdaptiveWardingArcane);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
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

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
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
    }

    [Script]
    internal class spell_gen_allow_cast_from_item_only : SpellScript, ICheckCastHander
    {
        public SpellCastResult CheckCast()
        {
            if (!GetCastItem())
                return SpellCastResult.CantDoThatRightNow;

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script] // 46221 - Animal Blood
    internal class spell_gen_animal_blood : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SpawnBloodPool);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Remove all Auras with spell Id 46221, except the one currently being applied
            Aura aur;

            while ((aur = GetUnitOwner().GetOwnedAura(SpellIds.AnimalBlood, ObjectGuid.Empty, ObjectGuid.Empty, 0, GetAura())) != null)
                GetUnitOwner().RemoveOwnedAura(aur);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit owner = GetUnitOwner();

            if (owner)
                owner.CastSpell(owner, SpellIds.SpawnBloodPool, true);
        }
    }

    [Script] // 63471 -Spawn Blood Pool
    internal class spell_spawn_blood_pool : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
        }

        private void SetDest(ref SpellDestination dest)
        {
            Unit caster = GetCaster();
            Position summonPos = caster.GetPosition();
            LiquidData liquidStatus = new();

            if (caster.GetMap().GetLiquidStatus(caster.GetPhaseShift(), caster.GetPositionX(), caster.GetPositionY(), caster.GetPositionZ(), LiquidHeaderTypeFlags.AllLiquids, liquidStatus, caster.GetCollisionHeight()) != ZLiquidStatus.NoWater)
                summonPos.Z = liquidStatus.Level;

            dest.Relocate(summonPos);
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
    internal class spell_gen_arena_drink : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Load()
        {
            return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            if (spellInfo.GetEffects().Empty() ||
                !spellInfo.GetEffect(0).IsAura(AuraType.ModPowerRegen))
            {
                Log.outError(LogFilter.Spells, "Aura {GetId()} structure has been changed - first aura is no longer SPELL_AURA_MOD_POWER_REGEN");

                return false;
            }

            return true;
        }

        public override void Register()
        {
            Effects.Add(new EffectCalcPeriodicHandler(CalcPeriodic, 1, AuraType.PeriodicDummy));
            Effects.Add(new EffectCalcAmountHandler(CalcAmount, 1, AuraType.PeriodicDummy));
            Effects.Add(new EffectUpdatePeriodicHandler(UpdatePeriodic, 1, AuraType.PeriodicDummy));
        }

        private void CalcPeriodic(AuraEffect aurEff, ref bool isPeriodic, ref int amplitude)
        {
            // Get SPELL_AURA_MOD_POWER_REGEN aura from spell
            AuraEffect regen = GetAura().GetEffect(0);

            if (regen == null)
                return;

            // default case - not in arena
            if (!GetCaster().ToPlayer().InArena())
                isPeriodic = false;
        }

        private void CalcAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            AuraEffect regen = GetAura().GetEffect(0);

            if (regen == null)
                return;

            // default case - not in arena
            if (!GetCaster().ToPlayer().InArena())
                regen.ChangeAmount(amount);
        }

        private void UpdatePeriodic(AuraEffect aurEff)
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
                case 1: // 0%
                    regen.ChangeAmount(0);

                    break;
                case 2: // 166%
                    regen.ChangeAmount(aurEff.GetAmount() * 5 / 3);

                    break;
                case 3: // 133%
                    regen.ChangeAmount(aurEff.GetAmount() * 4 / 3);

                    break;
                default: // 100% - normal regen
                    regen.ChangeAmount(aurEff.GetAmount());
                    // No need to update after 4th tick
                    aurEff.SetPeriodic(false);

                    break;
            }
        }
    }

    [Script] // 28313 - Aura of Fear
    internal class spell_gen_aura_of_fear : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return !spellInfo.GetEffects().Empty() && ValidateSpellInfo(spellInfo.GetEffect(0).TriggerSpell);
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }

        private void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();

            if (!RandomHelper.randChance(GetSpellInfo().ProcChance))
                return;

            GetTarget().CastSpell(null, aurEff.GetSpellEffectInfo().TriggerSpell, true);
        }
    }

    [Script]
    internal class spell_gen_av_drekthar_presence : AuraScript, IAuraCheckAreaTarget
    {
        public bool CheckAreaTarget(Unit target)
        {
            return (target.GetEntry()) switch
            {
                // alliance
                // Dun Baldar North Marshal
                14762 or 14763 or 14764 or 14765 or 11948 or 14772 or 14776 or 14773 or 14777 or 11946 => true,
                _ => false
            };
        }
    }

    [Script]
    internal class spell_gen_bandage : SpellScript, ICheckCastHander, IAfterHit
    {
        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target)
                GetCaster().CastSpell(target, SpellIds.RecentlyBandaged, true);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.RecentlyBandaged);
        }

        public SpellCastResult CheckCast()
        {
            Unit target = GetExplTargetUnit();

            if (target)
                if (target.HasAura(SpellIds.RecentlyBandaged))
                    return SpellCastResult.TargetAurastate;

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script] // 193970 - Mercenary Shapeshift
    internal class spell_gen_battleground_mercenary_shapeshift : AuraScript, IHasAuraEffects
    {
        //using OtherFactionRacePriorityList = std::array<Races, 3>;

        private static readonly Dictionary<Race, Race[]> RaceInfo = new()
                                                                    {
                                                                        {
                                                                            Race.Human, new[]
                                                                                        {
                                                                                            Race.Undead, Race.BloodElf
                                                                                        }
                                                                        },
                                                                        {
                                                                            Race.Orc, new[]
                                                                                      {
                                                                                          Race.Dwarf
                                                                                      }
                                                                        },
                                                                        {
                                                                            Race.Dwarf, new[]
                                                                                        {
                                                                                            Race.Orc, Race.Undead, Race.Tauren
                                                                                        }
                                                                        },
                                                                        {
                                                                            Race.NightElf, new[]
                                                                                           {
                                                                                               Race.Troll, Race.BloodElf
                                                                                           }
                                                                        },
                                                                        {
                                                                            Race.Undead, new[]
                                                                                         {
                                                                                             Race.Human
                                                                                         }
                                                                        },
                                                                        {
                                                                            Race.Tauren, new[]
                                                                                         {
                                                                                             Race.Draenei, Race.NightElf
                                                                                         }
                                                                        },
                                                                        {
                                                                            Race.Gnome, new[]
                                                                                        {
                                                                                            Race.Goblin, Race.BloodElf
                                                                                        }
                                                                        },
                                                                        {
                                                                            Race.Troll, new[]
                                                                                        {
                                                                                            Race.NightElf, Race.Human, Race.Draenei
                                                                                        }
                                                                        },
                                                                        {
                                                                            Race.Goblin, new[]
                                                                                         {
                                                                                             Race.Gnome, Race.Dwarf
                                                                                         }
                                                                        },
                                                                        {
                                                                            Race.BloodElf, new[]
                                                                                           {
                                                                                               Race.Human, Race.NightElf
                                                                                           }
                                                                        },
                                                                        {
                                                                            Race.Draenei, new[]
                                                                                          {
                                                                                              Race.Tauren, Race.Orc
                                                                                          }
                                                                        },
                                                                        {
                                                                            Race.Worgen, new[]
                                                                                         {
                                                                                             Race.Troll
                                                                                         }
                                                                        },
                                                                        {
                                                                            Race.PandarenNeutral, new[]
                                                                                                  {
                                                                                                      Race.PandarenNeutral
                                                                                                  }
                                                                        },
                                                                        {
                                                                            Race.PandarenAlliance, new[]
                                                                                                   {
                                                                                                       Race.PandarenHorde, Race.PandarenNeutral
                                                                                                   }
                                                                        },
                                                                        {
                                                                            Race.PandarenHorde, new[]
                                                                                                {
                                                                                                    Race.PandarenAlliance, Race.PandarenNeutral
                                                                                                }
                                                                        },
                                                                        {
                                                                            Race.Nightborne, new[]
                                                                                             {
                                                                                                 Race.NightElf, Race.Human
                                                                                             }
                                                                        },
                                                                        {
                                                                            Race.HighmountainTauren, new[]
                                                                                                     {
                                                                                                         Race.Draenei, Race.NightElf
                                                                                                     }
                                                                        },
                                                                        {
                                                                            Race.VoidElf, new[]
                                                                                          {
                                                                                              Race.Troll, Race.BloodElf
                                                                                          }
                                                                        },
                                                                        {
                                                                            Race.LightforgedDraenei, new[]
                                                                                                     {
                                                                                                         Race.Tauren, Race.Orc
                                                                                                     }
                                                                        },
                                                                        {
                                                                            Race.ZandalariTroll, new[]
                                                                                                 {
                                                                                                     Race.KulTiran, Race.Human
                                                                                                 }
                                                                        },
                                                                        {
                                                                            Race.KulTiran, new[]
                                                                                           {
                                                                                               Race.ZandalariTroll
                                                                                           }
                                                                        },
                                                                        {
                                                                            Race.DarkIronDwarf, new[]
                                                                                                {
                                                                                                    Race.MagharOrc, Race.Orc
                                                                                                }
                                                                        },
                                                                        {
                                                                            Race.Vulpera, new[]
                                                                                          {
                                                                                              Race.MechaGnome, Race.DarkIronDwarf /*Guessed, For Shamans*/
                                                                                          }
                                                                        },
                                                                        {
                                                                            Race.MagharOrc, new[]
                                                                                            {
                                                                                                Race.DarkIronDwarf
                                                                                            }
                                                                        },
                                                                        {
                                                                            Race.MechaGnome, new[]
                                                                                             {
                                                                                                 Race.Vulpera
                                                                                             }
                                                                        }
                                                                    };

        private static readonly Dictionary<Race, uint[]> RaceDisplayIds = new()
                                                                          {
                                                                              {
                                                                                  Race.Human, new uint[]
                                                                                              {
                                                                                                  55239, 55238
                                                                                              }
                                                                              },
                                                                              {
                                                                                  Race.Orc, new uint[]
                                                                                            {
                                                                                                55257, 55256
                                                                                            }
                                                                              },
                                                                              {
                                                                                  Race.Dwarf, new uint[]
                                                                                              {
                                                                                                  55241, 55240
                                                                                              }
                                                                              },
                                                                              {
                                                                                  Race.NightElf, new uint[]
                                                                                                 {
                                                                                                     55243, 55242
                                                                                                 }
                                                                              },
                                                                              {
                                                                                  Race.Undead, new uint[]
                                                                                               {
                                                                                                   55259, 55258
                                                                                               }
                                                                              },
                                                                              {
                                                                                  Race.Tauren, new uint[]
                                                                                               {
                                                                                                   55261, 55260
                                                                                               }
                                                                              },
                                                                              {
                                                                                  Race.Gnome, new uint[]
                                                                                              {
                                                                                                  55245, 55244
                                                                                              }
                                                                              },
                                                                              {
                                                                                  Race.Troll, new uint[]
                                                                                              {
                                                                                                  55263, 55262
                                                                                              }
                                                                              },
                                                                              {
                                                                                  Race.Goblin, new uint[]
                                                                                               {
                                                                                                   55267, 57244
                                                                                               }
                                                                              },
                                                                              {
                                                                                  Race.BloodElf, new uint[]
                                                                                                 {
                                                                                                     55265, 55264
                                                                                                 }
                                                                              },
                                                                              {
                                                                                  Race.Draenei, new uint[]
                                                                                                {
                                                                                                    55247, 55246
                                                                                                }
                                                                              },
                                                                              {
                                                                                  Race.Worgen, new uint[]
                                                                                               {
                                                                                                   55255, 55254
                                                                                               }
                                                                              },
                                                                              {
                                                                                  Race.PandarenNeutral, new uint[]
                                                                                                        {
                                                                                                            55253, 55252
                                                                                                        }
                                                                              }, // Not Verified, Might Be Swapped With Race.PandarenHorde
                                                                              {
                                                                                  Race.PandarenAlliance, new uint[]
                                                                                                         {
                                                                                                             55249, 55248
                                                                                                         }
                                                                              },
                                                                              {
                                                                                  Race.PandarenHorde, new uint[]
                                                                                                      {
                                                                                                          55251, 55250
                                                                                                      }
                                                                              },
                                                                              {
                                                                                  Race.Nightborne, new uint[]
                                                                                                   {
                                                                                                       82375, 82376
                                                                                                   }
                                                                              },
                                                                              {
                                                                                  Race.HighmountainTauren, new uint[]
                                                                                                           {
                                                                                                               82377, 82378
                                                                                                           }
                                                                              },
                                                                              {
                                                                                  Race.VoidElf, new uint[]
                                                                                                {
                                                                                                    82371, 82372
                                                                                                }
                                                                              },
                                                                              {
                                                                                  Race.LightforgedDraenei, new uint[]
                                                                                                           {
                                                                                                               82373, 82374
                                                                                                           }
                                                                              },
                                                                              {
                                                                                  Race.ZandalariTroll, new uint[]
                                                                                                       {
                                                                                                           88417, 88416
                                                                                                       }
                                                                              },
                                                                              {
                                                                                  Race.KulTiran, new uint[]
                                                                                                 {
                                                                                                     88414, 88413
                                                                                                 }
                                                                              },
                                                                              {
                                                                                  Race.DarkIronDwarf, new uint[]
                                                                                                      {
                                                                                                          88409, 88408
                                                                                                      }
                                                                              },
                                                                              {
                                                                                  Race.Vulpera, new uint[]
                                                                                                {
                                                                                                    94999, 95001
                                                                                                }
                                                                              },
                                                                              {
                                                                                  Race.MagharOrc, new uint[]
                                                                                                  {
                                                                                                      88420, 88410
                                                                                                  }
                                                                              },
                                                                              {
                                                                                  Race.MechaGnome, new uint[]
                                                                                                   {
                                                                                                       94998, 95000
                                                                                                   }
                                                                              }
                                                                          };

        private static readonly List<uint> RacialSkills = new();
        public List<IAuraEffectHandler> Effects { get; } = new();

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

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.SendForClientMask, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private static Race GetReplacementRace(Race nativeRace, Class playerClass)
        {
            var otherRaces = RaceInfo.LookupByKey(nativeRace);

            if (otherRaces != null)
                foreach (Race race in otherRaces)
                    if (Global.ObjectMgr.GetPlayerInfo(race, playerClass) != null)
                        return race;

            return Race.None;
        }

        private static uint GetDisplayIdForRace(Race race, Gender gender)
        {
            var displayIds = RaceDisplayIds.LookupByKey(race);

            if (displayIds != null)
                return displayIds[(int)gender];

            return 0;
        }

        private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
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

        private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit owner = GetUnitOwner();
            Race otherFactionRace = GetReplacementRace(owner.GetRace(), owner.GetClass());

            if (otherFactionRace == Race.None)
                return;

            UpdateRacials(otherFactionRace, owner.GetRace());
        }

        private void UpdateRacials(Race oldRace, Race newRace)
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
    }

    [Script] // Blood Reserve - 64568
    internal class spell_gen_blood_reserve : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.BloodReserveHeal);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            Unit caster = eventInfo.GetActionTarget();

            if (caster != null)
                if (caster.HealthBelowPct(35))
                    return true;

            return false;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit caster = eventInfo.GetActionTarget();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
            caster.CastSpell(caster, SpellIds.BloodReserveHeal, args);
            caster.RemoveAura(SpellIds.BloodReserveAura);
        }
    }

    [Script]
    internal class spell_gen_bonked : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
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
    }

    [Script("spell_gen_break_shield")]
    [Script("spell_gen_tournament_counterattack")]
    internal class spell_gen_break_shield : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(62552, 62719, 64100, 66482);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
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
                                if (aura.GetId() == 62552 ||
                                    aura.GetId() == 62719 ||
                                    aura.GetId() == 64100 ||
                                    aura.GetId() == 66482)
                                {
                                    aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                    // Remove dummys from rider (Necessary for updating visual shields)
                                    Unit rider = target.GetCharmer();

                                    if (rider)
                                    {
                                        Aura defend = rider.GetAura(aura.GetId());

                                        defend?.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                default:
                    break;
            }
        }
    }

    [Script] // 48750 - Burning Depths Necrolyte Image
    internal class spell_gen_burning_depths_necrolyte_image : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 2 && ValidateSpellInfo((uint)spellInfo.GetEffect(2).CalcValue());
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            if (caster)
                caster.CastSpell(GetTarget(), (uint)GetEffectInfo(2).CalcValue());
        }

        private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell((uint)GetEffectInfo(2).CalcValue(), GetCasterGUID());
        }
    }

    [Script]
    internal class spell_gen_cannibalize : SpellScript, ICheckCastHander, IHasSpellEffects
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

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.CannibalizeTriggered, false);
        }
    }

    [Script] // 66020 Chains of Ice
    internal class spell_gen_chains_of_ice : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectUpdatePeriodicHandler(UpdatePeriodic, 1, AuraType.PeriodicDummy));
        }

        private void UpdatePeriodic(AuraEffect aurEff)
        {
            // Get 0 effect aura
            AuraEffect slow = GetAura().GetEffect(0);

            if (slow == null)
                return;

            int newAmount = Math.Min(slow.GetAmount() + aurEff.GetAmount(), 0);
            slow.ChangeAmount(newAmount);
        }
    }

    [Script]
    internal class spell_gen_chaos_blast : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ChaosBlast);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
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
    }

    [Script] // 28471 - ClearAll
    internal class spell_clear_all : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.RemoveAllAurasOnDeath();
        }
    }

    [Script]
    internal class spell_gen_clone : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            if (ScriptSpellId == SpellIds.NightmareFigmentMirrorImage)
            {
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            }
            else
            {
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
            }
        }

        private void HandleScriptEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
        }
    }

    [Script]
    internal class spell_gen_clone_weapon : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
        }
    }

    [Script]
    internal class spell_gen_clone_weapon_AuraScript : AuraScript, IHasAuraEffects
    {
        private uint prevItem;
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WeaponAura, SpellIds.Weapon2Aura, SpellIds.Weapon3Aura, SpellIds.OffhandAura, SpellIds.Offhand2Aura, SpellIds.RangedAura);
        }

        public override bool Load()
        {
            prevItem = 0;

            return true;
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
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
                        {
                            target.SetVirtualItem(0, caster.GetVirtualItemId(0));
                        }

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
                        {
                            target.SetVirtualItem(1, caster.GetVirtualItemId(1));
                        }

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
                        {
                            target.SetVirtualItem(2, caster.GetVirtualItemId(2));
                        }

                        break;
                    }
                default:
                    break;
            }
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
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
    }

    [Script("spell_gen_default_count_pct_from_max_hp", 0)]
    [Script("spell_gen_50pct_count_pct_from_max_hp", 50)]
    internal class spell_gen_count_pct_from_max_hp : SpellScript, IOnHit
    {
        private int _damagePct;

        public spell_gen_count_pct_from_max_hp(int damagePct)
        {
            _damagePct = damagePct;
        }

        public void OnHit()
        {
            if (_damagePct == 0)
                _damagePct = GetHitDamage();

            SetHitDamage((int)GetHitUnit().CountPctFromMaxHealth(_damagePct));
        }
    }

    // 28865 - Consumption
    [Script] // 64208 - Consumption
    internal class spell_gen_consumption : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamageCalc, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
        }

        private void HandleDamageCalc(uint effIndex)
        {
            Creature caster = GetCaster().ToCreature();

            if (caster == null)
                return;

            int damage = 0;
            SpellInfo createdBySpell = Global.SpellMgr.GetSpellInfo(caster.UnitData.CreatedBySpell, GetCastDifficulty());

            if (createdBySpell != null)
                damage = createdBySpell.GetEffect(2).CalcValue();

            if (damage != 0)
                SetEffectValue(damage);
        }
    }

    [Script] // 63845 - Create Lance
    internal class spell_gen_create_lance : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CreateLanceAlliance, SpellIds.CreateLanceHorde);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
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
    }

    [Script("spell_gen_sunreaver_disguise")]
    [Script("spell_gen_silver_covenant_disguise")]
    internal class spell_gen_dalaran_disguise : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.Id switch
            {
                SpellIds.SunreaverTrigger => ValidateSpellInfo(SpellIds.SunreaverFemale, SpellIds.SunreaverMale),
                SpellIds.SilverCovenantTrigger => ValidateSpellInfo(SpellIds.SilverCovenantFemale, SpellIds.SilverCovenantMale),
                _ => false
            };
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
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
    }

    [Script] // 32065 - Fungal Decay
    internal class spell_gen_decay_over_time_fungal_decay_SpellScript : SpellScript, IAfterHit
    {
        public void AfterHit()
        {
            Aura aur = GetHitAura();

            aur?.SetStackAmount((byte)GetSpellInfo().StackAmount);
        }
    }

    [Script] // 32065 - Fungal Decay
    internal class spell_gen_decay_over_time_fungal_decay_AuraScript : AuraScript, IAuraCheckProc, IAuraOnProc, IHasAuraEffects
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() == GetSpellInfo();
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(ModDuration, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
        }

        public void OnProc(ProcEventInfo info)
        {
            PreventDefaultAction();
            ModStackAmount(-1);
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void ModDuration(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // only on actual reapply, not on stack decay
            if (GetDuration() == GetMaxDuration())
            {
                SetMaxDuration(Misc.AuraDuration);
                SetDuration(Misc.AuraDuration);
            }
        }
    }

    [Script] // 36659 - Tail Sting
    internal class spell_gen_decay_over_time_tail_sting_SpellScript : SpellScript, IAfterHit
    {
        public void AfterHit()
        {
            Aura aur = GetHitAura();

            aur?.SetStackAmount((byte)GetSpellInfo().StackAmount);
        }
    }

    [Script] // 36659 - Tail Sting
    internal class spell_gen_decay_over_time_tail_sting_AuraScript : AuraScript, IAuraOnProc, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() == GetSpellInfo();
        }

        public void OnProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            ModStackAmount(-1);
        }
    }

    internal class spell_gen_despawn_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnRemove, SpellConst.EffectFirstFound, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().ToCreature()?.DespawnOrUnsummon();
        }
    }

    [Script]
    internal class spell_gen_despawn_self : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Unit);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, SpellConst.EffectAll, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) ||
                GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
                GetCaster().ToCreature().DespawnOrUnsummon();
        }
    }

    [Script]
    internal class spell_gen_despawn_target : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDespawn, SpellConst.EffectAll, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDespawn(uint effIndex)
        {
            if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) ||
                GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
            {
                Creature target = GetHitCreature();

                target?.DespawnOrUnsummon();
            }
        }
    }

    [Script] // 70769 Divine Storm!
    internal class spell_gen_divine_storm_cd_reset : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DivineStorm);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            GetCaster().GetSpellHistory().ResetCooldown(SpellIds.DivineStorm, true);
        }
    }

    [Script]
    internal class spell_gen_ds_flush_knockback : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            // Here the Target is the water spout and determines the position where the player is knocked from
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
    }

    [Script]
    internal class spell_gen_dungeon_credit : SpellScript, IAfterHit
    {
        private bool _handled;

        public override bool Load()
        {
            _handled = false;

            return GetCaster().IsTypeId(TypeId.Unit);
        }

        public void AfterHit()
        {
            // This hook is executed for every Target, make sure we only credit instance once
            if (_handled)
                return;

            _handled = true;
            Unit caster = GetCaster();
            InstanceScript instance = caster.GetInstanceScript();

            instance?.UpdateEncounterStateForSpellCast(GetSpellInfo().Id, caster);
        }
    }

    // 50051 - Ethereal Pet Aura
    [Script]
    internal class spell_ethereal_pet_aura : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            uint levelDiff = (uint)Math.Abs(GetTarget().GetLevel() - eventInfo.GetProcTarget().GetLevel());

            return levelDiff <= 9;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            List<TempSummon> minionList = new();
            GetUnitOwner().GetAllMinionsByEntry(minionList, CreatureIds.EtherealSoulTrader);

            foreach (Creature minion in minionList)
                if (minion.IsAIEnabled())
                {
                    minion.GetAI().Talk(TextIds.SayStealEssence);
                    minion.CastSpell(eventInfo.GetProcTarget(), SpellIds.StealEssenceVisual);
                }
        }
    }

    // 50052 - Ethereal Pet onSummon
    [Script]
    internal class spell_ethereal_pet_onsummon : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ProcTriggerOnKillAura);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            Unit target = GetHitUnit();
            target.CastSpell(target, SpellIds.ProcTriggerOnKillAura, true);
        }
    }

    // 50055 - Ethereal Pet Aura Remove
    [Script]
    internal class spell_ethereal_pet_aura_remove : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.EtherealPetAura);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell(SpellIds.EtherealPetAura);
        }
    }

    // 50101 - Ethereal Pet OnKill Steal Essence
    [Script]
    internal class spell_steal_essence_visual : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            if (caster != null)
            {
                caster.CastSpell(caster, SpellIds.CreateToken, true);
                Creature soulTrader = caster.ToCreature();

                soulTrader?.GetAI().Talk(TextIds.SayCreateToken);
            }
        }
    }

    /* 57337 - Great Feast
   57397 - Fish Feast
   58466 - Gigantic Feast
   58475 - Small Feast
   66477 - Bountiful Feast */
    [Script]
    internal class spell_gen_feast : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FeastFood,
                                     SpellIds.FeastDrink,
                                     SpellIds.BountifulFeastDrink,
                                     SpellIds.BountifulFeastFood,
                                     SpellIds.GreatFeastRefreshment,
                                     SpellIds.FishFeastRefreshment,
                                     SpellIds.GiganticFeastRefreshment,
                                     SpellIds.SmallFeastRefreshment,
                                     SpellIds.BountifulFeastRefreshment);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
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
    }

    /*
	 * There are only 3 possible flags Feign Death Auras can apply: UNIT_DYNFLAG_DEAD, UnitFlags2.FeignDeath
	 * and UNIT_FLAG_PREVENT_EMOTES_FROM_CHAT_TEXT. Some Auras can apply only 2 flags
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
    internal class spell_gen_feign_death_all_flags : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();

            creature?.SetReactState(ReactStates.Passive);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();

            creature?.InitializeReactState();
        }
    }

    // 35357 - Spawn Feign Death
    [Script] // 51329 - Feign Death
    internal class spell_gen_feign_death_no_dyn_flag : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag2(UnitFlags2.FeignDeath);
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();

            creature?.SetReactState(ReactStates.Passive);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

            Creature creature = target.ToCreature();

            creature?.InitializeReactState();
        }
    }

    [Script] // 58951 - Permanent Feign Death
    internal class spell_gen_feign_death_no_prevent_emotes : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);

            Creature creature = target.ToCreature();

            creature?.SetReactState(ReactStates.Passive);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);

            Creature creature = target.ToCreature();

            creature?.InitializeReactState();
        }
    }

    [Script] // 35491 - Furious Rage
    internal class spell_gen_furious_rage : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Exhaustion) &&
                   CliDB.BroadcastTextStorage.HasRecord(EmoteIds.FuriousRage) &&
                   CliDB.BroadcastTextStorage.HasRecord(EmoteIds.Exhausted);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(AfterApply, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.TextEmote(EmoteIds.FuriousRage, target, false);
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit target = GetTarget();
            target.TextEmote(EmoteIds.Exhausted, target, false);
            target.CastSpell(target, SpellIds.Exhaustion, true);
        }
    }

    [Script] // 46642 - 5,000 Gold
    internal class spell_gen_5000_gold : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();

            target?.ModifyMoney(5000 * MoneyConstants.Gold);
        }
    }

    [Script] // 131474 - Fishing
    internal class spell_gen_fishing : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FishingNoFishingPole, SpellIds.FishingWithPole);
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            uint spellId;
            Item mainHand = GetCaster().ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

            if (!mainHand ||
                mainHand.GetTemplate().GetClass() != ItemClass.Weapon ||
                (ItemSubClassWeapon)mainHand.GetTemplate().GetSubClass() != ItemSubClassWeapon.FishingPole)
                spellId = SpellIds.FishingNoFishingPole;
            else
                spellId = SpellIds.FishingWithPole;

            GetCaster().CastSpell(GetCaster(), spellId, false);
        }
    }

    [Script]
    internal class spell_gen_gadgetzan_transporter_backfire : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TransporterMalfunctionPolymorph, SpellIds.TransporterEviltwin, SpellIds.TransporterMalfunctionMiss);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            int r = RandomHelper.IRand(0, 119);

            if (r < 20) // Transporter Malfunction - 1/6 polymorph
                caster.CastSpell(caster, SpellIds.TransporterMalfunctionPolymorph, true);
            else if (r < 100) // Evil Twin               - 4/6 evil twin
                caster.CastSpell(caster, SpellIds.TransporterEviltwin, true);
            else // Transporter Malfunction - 1/6 miss the Target
                caster.CastSpell(caster, SpellIds.TransporterMalfunctionMiss, true);
        }
    }

    [Script]
    internal class spell_gen_gift_of_naaru : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 1;
        }

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (!GetCaster() ||
                aurEff.GetTotalTicks() == 0)
                return;

            float healPct = GetEffectInfo(1).CalcValue() / 100.0f;
            float heal = healPct * GetCaster().GetMaxHealth();
            int healTick = (int)Math.Floor(heal / aurEff.GetTotalTicks());
            amount += healTick;
        }
    }

    [Script]
    internal class spell_gen_gnomish_transporter : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.TransporterSuccess, SpellIds.TransporterFailure);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), RandomHelper.randChance(50) ? SpellIds.TransporterSuccess : SpellIds.TransporterFailure, true);
        }
    }

    [Script] // 69641 - Gryphon/Wyvern Pet - Mounting Check Aura
    internal class spell_gen_gryphon_wyvern_mount_check : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
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
    internal class spell_gen_hate_to_zero : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            if (GetCaster().CanHaveThreatList())
                GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -100);
        }
    }

    // This spell is used by both player and creature, but currently works only if used by player
    [Script] // 63984 - Hate to Zero
    internal class spell_gen_hate_to_zero_caster_target : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();

            if (target != null)
                if (target.CanHaveThreatList())
                    target.GetThreatManager().ModifyThreatByPercent(GetCaster(), -100);
        }
    }

    [Script] // 19707 - Hate to 50%
    internal class spell_gen_hate_to_50 : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            if (GetCaster().CanHaveThreatList())
                GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -50);
        }
    }

    [Script] // 26886 - Hate to 75%
    internal class spell_gen_hate_to_75 : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            if (GetCaster().CanHaveThreatList())
                GetCaster().GetThreatManager().ModifyThreatByPercent(GetHitUnit(), -25);
        }
    }

    [Script]
    internal class spell_gen_interrupt : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GenThrowInterrupt);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.GenThrowInterrupt, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script("spell_pal_blessing_of_kings")]
    [Script("spell_pal_blessing_of_might")]
    [Script("spell_dru_mark_of_the_wild")]
    [Script("spell_pri_power_word_fortitude")]
    [Script("spell_pri_shadow_protection")]
    internal class spell_gen_increase_stats_buff : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            if (GetHitUnit().IsInRaidWith(GetCaster()))
                GetCaster().CastSpell(GetCaster(), (uint)GetEffectValue() + 1, true); // raid buff
            else
                GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true); // single-Target buff
        }
    }

    [Script("spell_hexlord_lifebloom", SpellIds.HexlordMalacrass)]
    [Script("spell_tur_ragepaw_lifebloom", SpellIds.TurragePaw)]
    [Script("spell_cenarion_scout_lifebloom", SpellIds.CenarionScout)]
    [Script("spell_twisted_visage_lifebloom", SpellIds.TwistedVisage)]
    [Script("spell_faction_champion_dru_lifebloom", SpellIds.FactionChampionsDru)]
    internal class spell_gen_lifebloom : AuraScript, IHasAuraEffects
    {
        private readonly uint _spellId;

        public spell_gen_lifebloom(uint spellId)
        {
            _spellId = spellId;
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_spellId);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Final heal only on duration end
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire &&
                GetTargetApplication().GetRemoveMode() != AuraRemoveMode.EnemySpell)
                return;

            // final heal
            GetTarget().CastSpell(GetTarget(), _spellId, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
        }
    }

    [Script]
    internal class spell_gen_mounted_charge : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(62552, 62719, 64100, 66482);
        }

        public override void Register()
        {
            SpellInfo spell = Global.SpellMgr.GetSpellInfo(ScriptSpellId, Difficulty.None);

            if (spell.HasEffect(SpellEffectName.ScriptEffect))
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));

            if (spell.GetEffect(0).IsEffect(SpellEffectName.Charge))
                SpellEffects.Add(new EffectHandler(HandleChargeEffect, 0, SpellEffectName.Charge, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
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

                        // If Target isn't a training dummy there's a chance of failing the charge
                        if (!target.IsCharmedOwnedByPlayerOrPlayer() &&
                            RandomHelper.randChance(12.5f))
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
                                if (aura.GetId() == 62552 ||
                                    aura.GetId() == 62719 ||
                                    aura.GetId() == 64100 ||
                                    aura.GetId() == 66482)
                                {
                                    aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                    // Remove dummys from rider (Necessary for updating visual shields)
                                    Unit rider = target.GetCharmer();

                                    if (rider)
                                    {
                                        Aura defend = rider.GetAura(aura.GetId());

                                        defend?.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                                    }

                                    break;
                                }
                        }

                        break;
                    }
            }
        }

        private void HandleChargeEffect(uint effIndex)
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
    }

    // 6870 Moss Covered Feet
    [Script] // 31399 Moss Covered Feet
    internal class spell_gen_moss_covered_feet : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FallDown);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            eventInfo.GetActionTarget().CastSpell((Unit)null, SpellIds.FallDown, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script] // 28702 - Netherbloom
    internal class spell_gen_netherbloom : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            for (byte i = 0; i < 5; ++i)
                if (!ValidateSpellInfo(SpellIds.NetherBloomPollen1 + i))
                    return false;

            return true;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
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
    }

    [Script] // 28720 - Nightmare Vine
    internal class spell_gen_nightmare_vine : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.NightmarePollen);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Unit target = GetHitUnit();

            if (target)
                // 25% chance of casting Nightmare Pollen
                if (RandomHelper.randChance(25))
                    target.CastSpell(target, SpellIds.NightmarePollen, true);
        }
    }

    [Script] // 27746 -  Nitrous Boost
    internal class spell_gen_nitrous_boost : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
        }

        private void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();

            if (GetCaster() != null &&
                GetTarget().GetPower(PowerType.Mana) >= 10)
                GetTarget().ModifyPower(PowerType.Mana, -10);
            else
                Remove();
        }
    }

    [Script] // 27539 - Obsidian Armor
    internal class spell_gen_obsidian_armor : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Holy, SpellIds.Fire, SpellIds.Nature, SpellIds.Frost, SpellIds.Shadow, SpellIds.Arcane);
        }

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetSpellInfo() == null)
                return false;

            if (SharedConst.GetFirstSchoolInMask(eventInfo.GetSchoolMask()) == SpellSchools.Normal)
                return false;

            return true;
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(OnProcEffect, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        public List<IAuraEffectHandler> Effects { get; } = new();

        private void OnProcEffect(AuraEffect aurEff, ProcEventInfo eventInfo)
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
    }

    [Script]
    internal class spell_gen_oracle_wolvar_reputation : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 1;
        }

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void HandleDummy(uint effIndex)
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

            // EFFECT_INDEX_2 most likely update at war State, we already handle this in SetReputation
        }
    }

    [Script]
    internal class spell_gen_orc_disguise : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.OrcDisguiseTrigger, SpellIds.OrcDisguiseMale, SpellIds.OrcDisguiseFemale);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
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
    }

    [Script] // 35201 - Paralytic Poison
    internal class spell_gen_paralytic_poison : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Paralysis);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleStun, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void HandleStun(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            GetTarget().CastSpell((Unit)null, SpellIds.Paralysis, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script]
    internal class spell_gen_prevent_emotes : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleEffectApply, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            Effects.Add(new EffectApplyHandler(OnRemove, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
        }
    }

    [Script]
    internal class spell_gen_player_say : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.BroadcastTextStorage.HasRecord((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            // Note: Target here is always player; caster here is gameobject, creature or player (self cast)
            Unit target = GetHitUnit();

            target?.Say((uint)GetEffectValue(), target);
        }
    }

    [Script("spell_item_soul_harvesters_charm")]
    [Script("spell_item_commendation_of_kaelthas")]
    [Script("spell_item_corpse_tongue_coin")]
    [Script("spell_item_corpse_tongue_coin_heroic")]
    [Script("spell_item_petrified_twilight_scale")]
    [Script("spell_item_petrified_twilight_scale_heroic")]
    internal class spell_gen_proc_below_pct_damaged : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo eventInfo)
        {
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return false;

            int pct = GetSpellInfo().GetEffect(0).CalcValue();

            if (eventInfo.GetActionTarget().HealthBelowPctDamaged(pct, damageInfo.GetDamage()))
                return true;

            return false;
        }
    }

    [Script]
    internal class spell_gen_proc_charge_drop_only : AuraScript, IAuraOnProc
    {
        public void OnProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
        }
    }

    [Script] // 45472 Parachute
    internal class spell_gen_parachute : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Parachute, SpellIds.ParachuteBuff);
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Player target = GetTarget().ToPlayer();

            if (target)
                if (target.IsFalling())
                {
                    target.RemoveAurasDueToSpell(SpellIds.Parachute);
                    target.CastSpell(target, SpellIds.ParachuteBuff, true);
                }
        }
    }

    [Script]
    internal class spell_gen_pet_summoned : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Load()
        {
            return GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Player player = GetCaster().ToPlayer();

            if (player.GetLastPetNumber() != 0)
            {
                PetType newPetType = (player.GetClass() == Class.Hunter) ? PetType.Hunter : PetType.Summon;
                Pet newPet = new(player, newPetType);

                if (newPet.LoadPetFromDB(player, 0, player.GetLastPetNumber(), true))
                {
                    // revive the pet if it is dead
                    if (newPet.GetDeathState() != DeathState.Alive &&
                        newPet.GetDeathState() != DeathState.JustRespawned)
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
    }

    [Script] // 36553 - PetWait
    internal class spell_gen_pet_wait : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            GetCaster().GetMotionMaster().Clear();
            GetCaster().GetMotionMaster().MoveIdle();
        }
    }

    [Script]
    internal class spell_gen_profession_research : SpellScript, ICheckCastHander, IHasSpellEffects
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

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleScript(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            uint spellId = GetSpellInfo().Id;

            // Learn random explicit discovery recipe (if any)
            // Players will now learn 3 recipes the very first Time they perform Northrend Inscription Research (3.3.0 patch notes)
            if (spellId == SpellIds.NorthrendInscriptionResearch &&
                !SkillDiscovery.HasDiscoveredAnySpell(spellId, caster))
                for (int i = 0; i < 2; ++i)
                {
                    uint _discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);

                    if (_discoveredSpellId != 0)
                        caster.LearnSpell(_discoveredSpellId, false);
                }

            uint discoveredSpellId = SkillDiscovery.GetExplicitDiscoverySpell(spellId, caster);

            if (discoveredSpellId != 0)
                caster.LearnSpell(discoveredSpellId, false);
        }
    }

    [Script]
    internal class spell_gen_pvp_trinket : SpellScript, IAfterCast
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
    internal class spell_gen_remove_flight_auras : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();

            if (target)
            {
                target.RemoveAurasByType(AuraType.Fly);
                target.RemoveAurasByType(AuraType.ModIncreaseMountedFlightSpeed);
            }
        }
    }

    [Script]
    internal class spell_gen_remove_impairing_auras : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            GetHitUnit().RemoveMovementImpairingAuras(true);
        }
    }

    // 23493 - Restoration
    [Script] // 24379 - Restoration
    internal class spell_gen_restoration : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }

        private void PeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();

            Unit target = GetTarget();

            if (target == null)
                return;

            uint heal = (uint)target.CountPctFromMaxHealth(10);
            HealInfo healInfo = new(target, target, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            target.HealBySpell(healInfo);

            /// @todo: should proc other Auras?
            int mana = target.GetMaxPower(PowerType.Mana);

            if (mana != 0)
            {
                mana /= 10;
                target.EnergizeBySpell(target, GetSpellInfo(), mana, PowerType.Mana);
            }
        }
    }

    // 38772 Grievous Wound
    // 43937 Grievous Wound
    // 62331 Impale
    [Script] // 62418 Impale
    internal class spell_gen_remove_on_health_pct : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffects().Count > 1;
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
        }

        private void PeriodicTick(AuraEffect aurEff)
        {
            // they apply Damage so no need to check for ticks here

            if (GetTarget().HealthAbovePct(GetEffectInfo(1).CalcValue()))
            {
                Remove(AuraRemoveMode.EnemySpell);
                PreventDefaultAction();
            }
        }
    }

    // 31956 Grievous Wound
    // 38801 Grievous Wound
    // 43093 Grievous Throw
    // 58517 Grievous Wound
    [Script] // 59262 Grievous Wound
    internal class spell_gen_remove_on_full_health : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
        }

        private void PeriodicTick(AuraEffect aurEff)
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
    }

    // 70292 - Glacial Strike
    [Script] // 71316 - Glacial Strike
    internal class spell_gen_remove_on_full_health_pct : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(PeriodicTick, 2, AuraType.PeriodicDamagePercent));
        }

        private void PeriodicTick(AuraEffect aurEff)
        {
            // they apply Damage so no need to check for ticks here

            if (GetTarget().IsFullHealth())
            {
                Remove(AuraRemoveMode.EnemySpell);
                PreventDefaultAction();
            }
        }
    }

    [Script]
    internal class spell_gen_replenishment : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 255, Targets.UnitCasterAreaRaid));
        }

        private void RemoveInvalidTargets(List<WorldObject> targets)
        {
            // In arenas Replenishment may only affect the caster
            Player caster = GetCaster().ToPlayer();

            if (caster)
                if (caster.InArena())
                {
                    targets.Clear();
                    targets.Add(caster);

                    return;
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
    }

    [Script]
    internal class spell_gen_replenishment_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Load()
        {
            return GetUnitOwner().GetPower(PowerType.Mana) != 0;
        }

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicEnergize));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
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
    }

    [Script]
    internal class spell_gen_running_wild : SpellScript
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
    internal class spell_gen_running_wild_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(SharedConst.DisplayIdHiddenMount))
                return false;

            return true;
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleMount, 1, AuraType.Mounted, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        }

        private void HandleMount(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            PreventDefaultAction();

            target.Mount(SharedConst.DisplayIdHiddenMount, 0, 0);

            // cast speed aura
            MountCapabilityRecord mountCapability = CliDB.MountCapabilityStorage.LookupByKey(aurEff.GetAmount());

            if (mountCapability != null)
                target.CastSpell(target, mountCapability.ModSpellAuraID, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }
    }

    [Script]
    internal class spell_gen_two_forms : SpellScript, ICheckCastHander, IHasSpellEffects
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

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleTransform, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleTransform(uint effIndex)
        {
            Unit target = GetHitUnit();
            PreventHitDefaultEffect(effIndex);

            if (target.HasAuraType(AuraType.WorgenAlteredForm))
                target.RemoveAurasByType(AuraType.WorgenAlteredForm);
            else // Basepoints 1 for this aura control whether to trigger transform transition animation or not.
                target.CastSpell(target, SpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 1));
        }
    }

    [Script]
    internal class spell_gen_darkflight : SpellScript, IAfterCast
    {
        public void AfterCast()
        {
            GetCaster().CastSpell(GetCaster(), SpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }
    }

    [Script]
    internal class spell_gen_seaforium_blast : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.PlantChargesCreditAchievement);
        }

        public override bool Load()
        {
            // OriginalCaster is always available in Spell.prepare
            return GetGObjCaster().GetOwnerGUID().IsPlayer();
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(AchievementCredit, 1, SpellEffectName.GameObjectDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void AchievementCredit(uint effIndex)
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
    }

    [Script]
    internal class spell_gen_spectator_cheer_trigger : SpellScript, IHasSpellEffects
    {
        private static readonly Emote[] EmoteArray =
        {
            Emote.OneshotCheer, Emote.OneshotExclamation, Emote.OneshotApplaud
        };

        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().HandleEmoteCommand(EmoteArray.SelectRandom());
        }
    }

    [Script]
    internal class spell_gen_spirit_healer_res : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Load()
        {
            return GetOriginalCaster() && GetOriginalCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
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
    }

    [Script]
    internal class spell_gen_summon_tournament_mount : SpellScript, ICheckCastHander
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
    internal class spell_gen_throw_shield : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }
    }

    [Script]
    internal class spell_gen_tournament_duel : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.OnTournamentMount, SpellIds.MountedDuel);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            Unit rider = GetCaster().GetCharmer();

            if (rider)
            {
                Player playerTarget = GetHitPlayer();

                if (playerTarget)
                {
                    if (playerTarget.HasAura(SpellIds.OnTournamentMount) &&
                        playerTarget.GetVehicleBase())
                        rider.CastSpell(playerTarget, SpellIds.MountedDuel, true);

                    return;
                }

                Unit unitTarget = GetHitUnit();

                if (unitTarget)
                    if (unitTarget.GetCharmer() &&
                        unitTarget.GetCharmer().IsTypeId(TypeId.Player) &&
                        unitTarget.GetCharmer().HasAura(SpellIds.OnTournamentMount))
                        rider.CastSpell(unitTarget.GetCharmer(), SpellIds.MountedDuel, true);
            }
        }
    }

    [Script]
    internal class spell_gen_tournament_pennant : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Load()
        {
            return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleApplyEffect, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
        }

        private void HandleApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit caster = GetCaster();

            if (caster)
                if (!caster.GetVehicleBase())
                    caster.RemoveAurasDueToSpell(GetId());
        }
    }

    [Script]
    internal class spell_gen_teleporting : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
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
    }

    [Script]
    internal class spell_gen_trigger_exclude_caster_aura_spell : SpellScript, IAfterCast
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
    internal class spell_gen_trigger_exclude_target_aura_spell : SpellScript, IAfterHit
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(spellInfo.ExcludeTargetAuraSpell);
        }

        public void AfterHit()
        {
            Unit target = GetHitUnit();

            if (target)
                // Blizz seems to just apply aura without bothering to cast
                GetCaster().AddAura(GetSpellInfo().ExcludeTargetAuraSpell, target);
        }
    }

    [Script("spell_pvp_trinket_shared_cd", SpellIds.WillOfTheForsakenCooldownTrigger)]
    [Script("spell_wotf_shared_cd", SpellIds.WillOfTheForsakenCooldownTriggerWotf)]
    internal class spell_pvp_trinket_wotf_shared_cd : SpellScript, IAfterCast
    {
        private readonly uint _triggered;

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
    internal class spell_gen_turkey_marker : AuraScript, IHasAuraEffects
    {
        private readonly List<uint> _applyTimes = new();
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
            Effects.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // store stack apply times, so we can pop them while they expire
            _applyTimes.Add(GameTime.GetGameTimeMS());
            Unit target = GetTarget();

            // on stack 15 cast the Achievement crediting spell
            if (GetStackAmount() >= 15)
                target.CastSpell(target, SpellIds.TurkeyVengeance, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
        }

        private void OnPeriodic(AuraEffect aurEff)
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
    }

    [Script]
    internal class spell_gen_upper_deck_create_foam_sword : SpellScript, IHasSpellEffects
    {
        //                       green  pink   blue   red    yellow
        private static readonly uint[] itemId =
        {
            45061, 45176, 45177, 45178, 45179
        };

        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Player player = GetHitPlayer();

            if (player)
            {
                // player can only have one of these items
                for (byte i = 0; i < 5; ++i)
                    if (player.HasItemCount(itemId[i], 1, true))
                        return;

                CreateItem(itemId[RandomHelper.URand(0, 4)], ItemContext.None);
            }
        }
    }

    // 52723 - Vampiric Touch
    [Script] // 60501 - Vampiric Touch
    internal class spell_gen_vampiric_touch : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.VampiricTouchHeal);
        }

        public override void Register()
        {
            Effects.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();

            if (damageInfo == null ||
                damageInfo.GetDamage() == 0)
                return;

            Unit caster = eventInfo.GetActor();
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage() / 2);
            caster.CastSpell(caster, SpellIds.VampiricTouchHeal, args);
        }
    }

    [Script]
    internal class spell_gen_vehicle_scaling : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Load()
        {
            return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
        }

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModHealingPct));
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModDamagePercentDone));
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, 2, AuraType.ModIncreaseHealthPercent));
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
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
                return; // @todo Research possibility of scaling down

            amount = (int)((avgILvl - baseItemLevel) * factor);
        }
    }

    [Script]
    internal class spell_gen_vendor_bark_trigger : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Creature vendor = GetCaster().ToCreature();

            if (vendor)
                if (vendor.GetEntry() == CreatureIds.AmphitheaterVendor)
                    vendor.GetAI().Talk(TextIds.SayAmphitheaterVendor);
        }
    }

    [Script]
    internal class spell_gen_wg_water : SpellScript, ICheckCastHander
    {
        public SpellCastResult CheckCast()
        {
            if (!GetSpellInfo().CheckTargetCreatureType(GetCaster()))
                return SpellCastResult.DontReport;

            return SpellCastResult.SpellCastOk;
        }
    }

    [Script]
    internal class spell_gen_whisper_gulch_yogg_saron_whisper : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.YoggSaronWhisperDummy);
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            PreventDefaultAction();
            GetTarget().CastSpell((Unit)null, SpellIds.YoggSaronWhisperDummy, true);
        }
    }

    [Script]
    internal class spell_gen_whisper_to_controller : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.BroadcastTextStorage.HasRecord((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
        }

        private void HandleScript(uint effIndex)
        {
            TempSummon casterSummon = GetCaster().ToTempSummon();

            if (casterSummon != null)
            {
                Player target = casterSummon.GetSummonerUnit().ToPlayer();

                if (target != null)
                    casterSummon.Whisper((uint)GetEffectValue(), target, false);
            }
        }
    }

    // BasePoints of spells is ID of npc_text used to group texts, it's not implemented so texts are grouped the old way
    // 50037 - Mystery of the Infinite: Future You's Whisper to Controller - Random
    // 50287 - Azure Dragon: On Death Force Cast Wyrmrest Defender to Whisper to Controller - Random
    // 60709 - MOTI, Redux: Past You's Whisper to Controller - Random
    [Script("spell_future_you_whisper_to_controller_random", 2u)]
    [Script("spell_wyrmrest_defender_whisper_to_controller_random", 1u)]
    [Script("spell_past_you_whisper_to_controller_random", 2u)]
    internal class spell_gen_whisper_to_controller_random : SpellScript, IHasSpellEffects
    {
        private readonly uint _text;

        public spell_gen_whisper_to_controller_random(uint text)
        {
            _text = text;
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
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
    }

    [Script]
    internal class spell_gen_eject_all_passengers : SpellScript, IAfterHit
    {
        public void AfterHit()
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();

            if (vehicle)
                vehicle.RemoveAllPassengers();
        }
    }

    [Script]
    internal class spell_gen_eject_passenger : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            if (spellInfo.GetEffects().Empty())
                return false;

            if (spellInfo.GetEffect(0).CalcValue() < 1)
                return false;

            return true;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(EjectPassenger, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void EjectPassenger(uint effIndex)
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();

            if (vehicle != null)
            {
                Unit passenger = vehicle.GetPassenger((sbyte)(GetEffectValue() - 1));

                if (passenger)
                    passenger.ExitVehicle();
            }
        }
    }

    [Script("spell_gen_eject_passenger_1", 0)]
    [Script("spell_gen_eject_passenger_3", 2)]
    internal class spell_gen_eject_passenger_with_seatId : SpellScript, IHasSpellEffects
    {
        private readonly sbyte _seatId;

        public spell_gen_eject_passenger_with_seatId(sbyte seatId)
        {
            _seatId = seatId;
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(EjectPassenger, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void EjectPassenger(uint effIndex)
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();

            if (vehicle != null)
            {
                Unit passenger = vehicle.GetPassenger(_seatId);

                passenger?.ExitVehicle();
            }
        }
    }

    [Script]
    internal class spell_gen_gm_freeze : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.GmFreeze);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Do what was done before to the Target in HandleFreezeCommand
            Player player = GetTarget().ToPlayer();

            if (player)
            {
                // stop combat + make player unattackable + Duel stop + stop some spells
                player.SetFaction(35);
                player.CombatStop();

                if (player.IsNonMeleeSpellCast(true))
                    player.InterruptNonMeleeSpells(true);

                player.SetUnitFlag(UnitFlags.NonAttackable);

                // if player class = hunter || warlock Remove pet if alive
                if ((player.GetClass() == Class.Hunter) ||
                    (player.GetClass() == Class.Warlock))
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

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // Do what was done before to the Target in HandleUnfreezeCommand
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
    }

    [Script]
    internal class spell_gen_stand : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint eff)
        {
            Creature target = GetHitCreature();

            if (!target)
                return;

            target.SetStandState(UnitStandStateType.Stand);
            target.HandleEmoteCommand(Emote.StateNone);
        }
    }

    internal enum RequiredMixologySpells
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
    internal class spell_gen_mixology_bonus : AuraScript, IHasAuraEffects
    {
        private int bonus;
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)RequiredMixologySpells.Mixology) && !spellInfo.GetEffects().Empty();
        }

        public override bool Load()
        {
            return GetCaster() && GetCaster().GetTypeId() == TypeId.Player;
        }

        public override void Register()
        {
            Effects.Add(new EffectCalcAmountHandler(CalculateAmount, SpellConst.EffectAll, AuraType.Any));
        }

        private void SetBonusValueForEffect(uint effIndex, int value, AuraEffect aurEff)
        {
            if (aurEff.GetEffIndex() == effIndex)
                bonus = value;
        }

        private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster().HasAura((uint)RequiredMixologySpells.Mixology) &&
                GetCaster().HasSpell(GetEffectInfo(0).TriggerSpell))
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
    }

    [Script]
    internal class spell_gen_landmine_knockback_achievement : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Player target = GetHitPlayer();

            if (target)
            {
                Aura aura = GetHitAura();

                if (aura == null ||
                    aura.GetStackAmount() < 10)
                    return;

                target.CastSpell(target, SpellIds.LandmineKnockbackAchievement, true);
            }
        }
    }

    [Script] // 34098 - ClearAllDebuffs
    internal class spell_gen_clear_debuffs : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();

            if (target)
                target.RemoveOwnedAuras(aura =>
                                        {
                                            SpellInfo spellInfo = aura.GetSpellInfo();

                                            return !spellInfo.IsPositive() && !spellInfo.IsPassive();
                                        });
        }
    }

    [Script]
    internal class spell_gen_pony_mount_check : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void HandleEffectPeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (!caster)
                return;

            Player owner = caster.GetOwner().ToPlayer();

            if (!owner ||
                !owner.HasAchieved(SpellIds.AchievementPonyup))
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
    }

    // 40350 - Corrupting Plague
    internal class CorruptingPlagueSearcher : ICheck<Unit>
    {
        private readonly float _distance;

        private readonly Unit _unit;

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
    }

    [Script] // 40349 - Corrupting Plague
    internal class spell_corrupting_plague_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CorruptingPlague);
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }

        private void OnPeriodic(AuraEffect aurEff)
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
    }

    [Script] // 40306 - Stasis Field
    internal class spell_stasis_field_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.StasisField);
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
        }

        private void OnPeriodic(AuraEffect aurEff)
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
    }

    [Script]
    internal class spell_gen_vehicle_control_link : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(SpellIds.SiegeTankControl); //aurEff.GetAmount()
        }
    }

    [Script] // 34779 - Freezing Circle
    internal class spell_freezing_circle : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FreezingCirclePitOfSaronNormal, SpellIds.FreezingCirclePitOfSaronHeroic, SpellIds.FreezingCircle, SpellIds.FreezingCircleScenario);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDamage(uint effIndex)
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
    }

    [Script] // Used for some spells cast by vehicles or charmed creatures that do not send a cooldown event on their own
    internal class spell_gen_charmed_unit_spell_cooldown : SpellScript, IOnCast
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
    internal class spell_gen_cannon_blast : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.CannonBlast);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            int bp = GetEffectValue();
            Unit target = GetHitUnit();
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, bp);
            target.CastSpell(target, SpellIds.CannonBlastDamage, args);
        }
    }

    [Script] // 37751 - Submerged
    internal class spell_gen_submerged : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint eff)
        {
            Creature target = GetHitCreature();

            target?.SetStandState(UnitStandStateType.Submerged);
        }
    }

    [Script] // 169869 - Transformation Sickness
    internal class spell_gen_decimatus_transformation_sickness : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();

            if (target)
                target.SetHealth(target.CountPctFromMaxHealth(25));
        }
    }

    [Script] // 189491 - Summon Towering Infernal.
    internal class spell_gen_anetheron_summon_towering_infernal : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }
    }

    [Script]
    internal class spell_gen_mark_of_kazrogal_hellfire : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(target =>
                              {
                                  Unit unit = target.ToUnit();

                                  if (unit)
                                      return unit.GetPowerType() != PowerType.Mana;

                                  return false;
                              });
        }
    }

    [Script]
    internal class spell_gen_mark_of_kazrogal_hellfire_AuraScript : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.MarkOfKazrogalDamageHellfire);
        }

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PowerBurn));
        }

        private void OnPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();

            if (target.GetPower(PowerType.Mana) == 0)
            {
                target.CastSpell(target, SpellIds.MarkOfKazrogalDamageHellfire, new CastSpellExtraArgs(aurEff));
                // Remove aura
                SetDuration(0);
            }
        }
    }

    [Script]
    internal class spell_gen_azgalor_rain_of_fire_hellfire_citadel : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
        }
    }

    [Script] // 99947 - Face Rage
    internal class spell_gen_face_rage : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FaceRage) && spellInfo.GetEffects().Count > 2;
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(GetEffectInfo(2).TriggerSpell);
        }
    }

    [Script] // 187213 - Impatient Mind
    internal class spell_gen_impatient_mind : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.ImpatientMind);
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(effect.GetSpellEffectInfo().TriggerSpell);
        }
    }

    // 269083 - Enlisted
    [Script] // 282559 - Enlisted
    internal class spell_gen_war_mode_enlisted : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ScriptSpellId, Difficulty.None);

            if (spellInfo.HasAura(AuraType.ModXpPct))
                Effects.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpPct));

            if (spellInfo.HasAura(AuraType.ModXpQuestPct))
                Effects.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModXpQuestPct));

            if (spellInfo.HasAura(AuraType.ModCurrencyGainFromSource))
                Effects.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModCurrencyGainFromSource));

            if (spellInfo.HasAura(AuraType.ModMoneyGain))
                Effects.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModMoneyGain));

            if (spellInfo.HasAura(AuraType.ModAnimaGain))
                Effects.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.ModAnimaGain));

            if (spellInfo.HasAura(AuraType.Dummy))
                Effects.Add(new EffectCalcAmountHandler(CalcWarModeBonus, SpellConst.EffectAll, AuraType.Dummy));
        }

        private void CalcWarModeBonus(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
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
    }

    [Script]
    internal class spell_defender_of_azeroth_death_gate_selector : SpellScript, IHasSpellEffects
    {
        private (WorldLocation, uint) OrgrimmarInnLoc = (new WorldLocation(1, 1573.18f, -4441.62f, 16.06f, 1.818284034729003906f), 8618);
        private (WorldLocation, uint) StormwindInnLoc = (new WorldLocation(0, -8868.1f, 675.82f, 97.9f, 5.164778709411621093f), 5148);
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.DeathGateTeleportStormwind, SpellIds.DeathGateTeleportOrgrimmar);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Player player = GetHitUnit().ToPlayer();

            if (player == null)
                return;

            if (player.GetQuestStatus(QuestIds.DefenderOfAzerothAlliance) == QuestStatus.None &&
                player.GetQuestStatus(QuestIds.DefenderOfAzerothHorde) == QuestStatus.None)
                return;

            (WorldLocation Loc, uint AreaId) bindLoc = player.GetTeam() == Team.Alliance ? StormwindInnLoc : OrgrimmarInnLoc;
            player.SetHomebind(bindLoc.Loc, bindLoc.AreaId);
            player.SendBindPointUpdate();
            player.SendPlayerBound(player.GetGUID(), bindLoc.AreaId);

            player.CastSpell(player, player.GetTeam() == Team.Alliance ? SpellIds.DeathGateTeleportStormwind : SpellIds.DeathGateTeleportOrgrimmar);
        }
    }

    [Script]
    internal class spell_defender_of_azeroth_speak_with_mograine : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            if (!GetCaster())
                return;

            Player player = GetCaster().ToPlayer();

            if (player == null)
                return;

            Creature nazgrim = GetHitUnit().FindNearestCreature(CreatureIds.Nazgrim, 10.0f);

            nazgrim?.HandleEmoteCommand(Emote.OneshotPoint, player);

            Creature trollbane = GetHitUnit().FindNearestCreature(CreatureIds.Trollbane, 10.0f);

            trollbane?.HandleEmoteCommand(Emote.OneshotPoint, player);

            Creature whitemane = GetHitUnit().FindNearestCreature(CreatureIds.Whitemane, 10.0f);

            whitemane?.HandleEmoteCommand(Emote.OneshotPoint, player);

            // @TODO: spawntracking - show death gate for casting player
        }
    }

    [Script] // 118301 - Summon Battle Pet
    internal class spell_summon_battle_pet : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Summon, SpellScriptHookType.EffectHit));
        }

        private void HandleSummon(uint effIndex)
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

                summon?.SetImmuneToAll(true);
            }
        }
    }

    [Script] // 132334 - Trainer Heal Cooldown (SERVERSIDE)
    internal class spell_gen_trainer_heal_cooldown : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SharedConst.SpellReviveBattlePets);
        }

        public override bool Load()
        {
            return GetUnitOwner().IsPlayer();
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(UpdateReviveBattlePetCooldown, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        }

        private void UpdateReviveBattlePetCooldown(AuraEffect aurEff, AuraEffectHandleModes mode)
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
    }

    [Script] // 45313 - Anchor Here
    internal class spell_gen_anchor_here : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            Creature creature = GetHitCreature();

            creature?.SetHomePosition(creature.GetPositionX(), creature.GetPositionY(), creature.GetPositionZ(), creature.GetOrientation());
        }
    }

    [Script] // 147066 - (Serverside/Non-DB2) Generic - Mount Check Aura
    internal class spell_gen_mount_check_aura : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
        }

        private void OnPeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();
            uint mountDisplayId = 0;

            TempSummon tempSummon = target.ToTempSummon();

            if (tempSummon == null)
                return;

            Player summoner = tempSummon.GetSummoner()?.ToPlayer();

            if (summoner == null)
                return;

            if (summoner.IsMounted() &&
                (!summoner.IsInCombat() || summoner.IsFlying()))
            {
                CreatureSummonedData summonedData = Global.ObjectMgr.GetCreatureSummonedData(tempSummon.GetEntry());

                if (summonedData != null)
                {
                    if (summoner.IsFlying() &&
                        summonedData.FlyingMountDisplayID.HasValue)
                        mountDisplayId = summonedData.FlyingMountDisplayID.Value;
                    else if (summonedData.GroundMountDisplayID.HasValue)
                        mountDisplayId = summonedData.GroundMountDisplayID.Value;
                }
            }

            if (mountDisplayId != target.GetMountDisplayId())
                target.SetMountDisplayId(mountDisplayId);
        }
    }

    [Script] // 274738 - Ancestral Call (Mag'har Orc Racial)
    internal class spell_gen_ancestral_call : SpellScript, IOnCast
    {
        private static readonly uint[] AncestralCallBuffs =
        {
            SpellIds.RictusOfTheLaughingSkull, SpellIds.ZealOfTheBurningBlade, SpellIds.FerocityOfTheFrostwolf, SpellIds.MightOfTheBlackrock
        };

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.RictusOfTheLaughingSkull, SpellIds.ZealOfTheBurningBlade, SpellIds.FerocityOfTheFrostwolf, SpellIds.MightOfTheBlackrock);
        }

        public void OnCast()
        {
            Unit caster = GetCaster();
            uint spellId = AncestralCallBuffs.SelectRandom();

            caster.CastSpell(caster, spellId, true);
        }
    }

    [Script] // 83477 - Eject Passengers 3-8
    internal class spell_gen_eject_passengers_3_8 : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            Vehicle vehicle = GetHitUnit().GetVehicleKit();

            if (vehicle == null)
                return;

            for (sbyte i = 2; i < 8; i++)
                vehicle.GetPassenger(i)?.ExitVehicle();
        }
    }

    [Script] // 83781 - Reverse Cast Ride Vehicle
    internal class spell_gen_reverse_cast_target_to_caster_triggered : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScript(uint effIndex)
        {
            GetHitUnit().CastSpell(GetCaster(), (uint)GetSpellInfo().GetEffect(effIndex).CalcValue(), true);
        }
    }

    // Note: this spell unsummons any creature owned by the caster. Set appropriate Target conditions on the DB.
    // 84065 - Despawn All Summons
    // 83935 - Despawn All Summons
    [Script] // 160938 - Despawn All Summons (Garrison Intro Only)
    internal class spell_gen_despawn_all_summons_owned_by_caster : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleScriptEffect(uint effIndex)
        {
            Unit caster = GetCaster();

            if (caster != null)
            {
                Creature target = GetHitCreature();

                if (target.GetOwner() == caster)
                    target.DespawnOrUnsummon();
            }
        }
    }

    // 40307 - Stasis Field
    internal class StasisFieldSearcher : ICheck<Unit>
    {
        private readonly float _distance;
        private readonly Unit _unit;

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