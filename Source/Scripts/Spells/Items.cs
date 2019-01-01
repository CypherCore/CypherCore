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
using Framework.GameMath;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Spells.Items
{
    struct SpellIds
    {
        //Aegisofpreservation
        public const uint AegisHeal = 23781;

        //Alchemiststone
        public const uint AlchemistStoneExtraHeal = 21399;
        public const uint AlchemistStoneExtraMana = 21400;

        //Angercapacitor
        public const uint MoteOfAnger = 71432;
        public const uint ManifestAngerMainHand = 71433;
        public const uint ManifestAngerOffHand = 71434;

        //Auraofmadness
        public const uint Sociopath = 39511; // Sociopath: +35 Strength(Paladin; Rogue; Druid; Warrior)
        public const uint Delusional = 40997; // Delusional: +70 Attack Power(Rogue; Hunter; Paladin; Warrior; Druid)
        public const uint Kleptomania = 40998; // Kleptomania: +35 Agility(Warrior; Rogue; Paladin; Hunter; Druid)
        public const uint Megalomania = 40999; // Megalomania: +41 Damage / Healing(Druid; Shaman; Priest; Warlock; Mage; Paladin)
        public const uint Paranoia = 41002; // Paranoia: +35 Spell / Melee / Ranged Crit Strike Rating(All Classes)
        public const uint Manic = 41005; // Manic: +35 Haste(Spell; Melee And Ranged) (All Classes)
        public const uint Narcissism = 41009; // Narcissism: +35 Intellect(Druid; Shaman; Priest; Warlock; Mage; Paladin; Hunter)
        public const uint MartyrComplex = 41011; // Martyr Complex: +35 Stamina(All Classes)
        public const uint Dementia = 41404; // Dementia: Every 5 Seconds Either Gives You +5/-5%  Damage/Healing. (Druid; Shaman; Priest; Warlock; Mage; Paladin)
        public const uint DementiaPos = 41406;
        public const uint DementiaNeg = 41409;

        //Blessingofancientkings
        public const uint ProtectionOfAncientKings = 64413;

        //Deadlyprecision
        public const uint DeadlyPrecision = 71564;

        //Deathbringerswill
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

        //Makeawish
        public const uint MrPinchysBlessing = 33053;
        public const uint SummonMightyMrPinchy = 33057;
        public const uint SummonFuriousMrPinchy = 33059;
        public const uint TinyMagicalCrawdad = 33062;
        public const uint MrPinchysGift = 33064;

        //Markofconquest
        public const uint MarkOfConquestEnergize = 39599;

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

        public static uint[] WormholeTargetLocations =
        {
            Wormholepandariaisleofreckoning,
            Wormholepandariakunlaiunderwater,
            Wormholepandariasravess,
            Wormholepandariarikkitunvillage,
            Wormholepandariazanvesstree,
            Wormholepandariaanglerswharf,
            Wormholepandariacranestatue,
            Wormholepandariaemperorsomen,
            Wormholepandariawhitepetallake
        };

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

        //Reindeertransformation
        public const uint FlyingReindeer310 = 44827;
        public const uint FlyingReindeer280 = 44825;
        public const uint FlyingReindeer60 = 44824;
        public const uint Reindeer100 = 25859;
        public const uint Reindeer60 = 25858;

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

        //Brewfestmounttransformation
        public const uint MountRam100 = 43900;
        public const uint MountRam60 = 43899;
        public const uint MountKodo100 = 49379;
        public const uint MountKodo60 = 49378;
        public const uint BrewfestMountTransform = 49357;
        public const uint BrewfestMountTransformReverse = 52845;

        //Nitroboots
        public const uint NitroBootsSuccess = 54861;
        public const uint NitroBootsBackfire = 46014;

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
        public const uint ArcaneBolt = 45429; // Arcane Bolt If Exalted By Scryers

        public const uint LightsStrength = 45480; // Light'S Strength If Exalted By Aldor
        public const uint ArcaneStrike = 45428; // Arcane Strike If Exalted By Scryers

        public const uint LightsWard = 45432; // Light'S Ward If Exalted By Aldor
        public const uint ArcaneInsight = 45431; // Arcane Insight If Exalted By Scryers

        public const uint LightsSalvation = 45478; // Light'S Salvation If Exalted By Aldor
        public const uint ArcaneSurge = 45430; // Arcane Surge If Exalted By Scryers

        //Deathchoicespells
        public const uint DeathChoiceNormalAura = 67702;
        public const uint DeathChoiceNormalAgility = 67703;
        public const uint DeathChoiceNormalStrength = 67708;
        public const uint DeathChoiceHeroicAura = 67771;
        public const uint DeathChoiceHeroicAgility = 67772;
        public const uint DeathChoiceHeroicStrength = 67773;

        //Trinketstackspells
        public const uint LightningCapacitorAura = 37657;  // Lightning Capacitor
        public const uint LightningCapacitorStack = 37658;
        public const uint LightningCapacitorTrigger = 37661;
        public const uint ThunderCapacitorAura = 54841;  // Thunder Capacitor
        public const uint ThunderCapacitorStack = 54842;
        public const uint ThunderCapacitorTrigger = 54843;
        public const uint Toc25CasterTrinketNormalAura = 67712;  // Item - Coliseum 25 Normal Caster Trinket
        public const uint Toc25CasterTrinketNormalStack = 67713;
        public const uint Toc25CasterTrinketNormalTrigger = 67714;
        public const uint Toc25CasterTrinketHeroicAura = 67758;  // Item - Coliseum 25 Heroic Caster Trinket
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

        // Auraprocremovespells        
        public const uint TalismanOfAscendance = 28200;
        public const uint JomGabbar = 29602;
        public const uint BattleTrance = 45040;
        public const uint WorldQuellerFocus = 90900;
        public const uint AzureWaterStrider = 118089;
        public const uint CrimsonWaterStrider = 127271;
        public const uint OrangeWaterStrider = 127272;
        public const uint JadeWaterStrider = 127274;
        public const uint GoldenWaterStrider = 127278;
        public const uint BrutalKinship1 = 144671;
        public const uint BrutalKinship2 = 145738;
    }

    struct TextIds
    {
        //Auraofmadness
        public const uint SayMadness = 21954;

        //Roll Dice
        public const uint DecahedralDwarvenDice = 26147;

        //Roll 'dem Bones
        public const uint WornTrollDice = 26152;

        //TauntFlag
        public const uint EmotePlantsFlag = 28008;
    }

    struct FactionIds
    {
        //ExaltedSunwellNeck
        public const uint Aldor = 932;
        public const uint Scryers = 934;
    }

    struct ObjectIds
    {
        //Crystalprison
        public const uint ImprisonedDoomguard = 179644;
    }

    struct CreatureIds
    {
        //Shadowsfate
        public const uint Sindragosa = 36853;

        //Giftoftheharvester
        public const uint Ghoul = 28845;
        public const uint MaxGhouls = 5;

        //Sinkholes
        public const uint SouthSinkhole = 25664;
        public const uint NortheastSinkhole = 25665;
        public const uint NorthwestSinkhole = 25666;

        //Demonbroiledsurprise
        public const uint AbyssalFlamebringer = 19973;

        //Impaleleviroth
        public const uint Leviroth = 26452;
    }

    struct ItemIds
    {
        //Createheartcandy
        public const uint HeartCandy1 = 21818;
        public const uint HeartCandy2 = 21817;
        public const uint HeartCandy3 = 21821;
        public const uint HeartCandy4 = 21819;
        public const uint HeartCandy5 = 21816;
        public const uint HeartCandy6 = 21823;
        public const uint HeartCandy7 = 21822;
        public const uint HeartCandy8 = 21820;
    }

    struct QuestIds
    {
        //Demonbroiledsurprise
        public const uint SuperHotStew = 11379;

        //Chickencover
        public const uint ChickenParty = 12702;
        public const uint FlownTheCoop = 12532;
    }

    struct SoundIds
    {
        //Ashbringersounds
        public const uint Ashbringer1 = 8906;                             // "I Was Pure Once"
        public const uint Ashbringer2 = 8907;                             // "Fought For Righteousness"
        public const uint Ashbringer3 = 8908;                             // "I Was Once Called Ashbringer"
        public const uint Ashbringer4 = 8920;                             // "Betrayed By My Order"
        public const uint Ashbringer5 = 8921;                             // "Destroyed By Kel'Thuzad"
        public const uint Ashbringer6 = 8922;                             // "Made To Serve"
        public const uint Ashbringer7 = 8923;                             // "My Son Watched Me Die"
        public const uint Ashbringer8 = 8924;                             // "Crusades Fed His Rage"
        public const uint Ashbringer9 = 8925;                             // "Truth Is Unknown To Him"
        public const uint Ashbringer10 = 8926;                             // "Scarlet Crusade  Is Pure No Longer"
        public const uint Ashbringer11 = 8927;                             // "Balnazzar'S Crusade Corrupted My Son"
        public const uint Ashbringer12 = 8928;                             // "Kill Them All!"
    }

    // 23074 Arcanite Dragonling
    // 23133 Gnomish Battle Chicken
    // 23076 Mechanical Dragonling
    // 23075 Mithril Mechanical Dragonling
    [Script("spell_item_arcanite_dragonling", SpellIds.ArcaniteDragonling)]
    [Script("spell_item_gnomish_battle_chicken", SpellIds.BattleChicken)]
    [Script("spell_item_mechanical_dragonling", SpellIds.MechanicalDragonling)]
    [Script("spell_item_mithril_mechanical_dragonling", SpellIds.MithrilMechanicalDragonling)]
    class spell_item_trigger_spell : SpellScript
    {
        public spell_item_trigger_spell(uint triggeredSpellId)
        {
            _triggeredSpellId = triggeredSpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_triggeredSpellId);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Item item = GetCastItem();
            if (item)
                caster.CastSpell(caster, _triggeredSpellId, true, item);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }

        uint _triggeredSpellId;
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
            GetTarget().CastSpell(GetTarget(), SpellIds.AegisHeal, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 1, AuraType.PeriodicTriggerSpell));
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

            if (eventInfo.GetDamageInfo().GetSpellInfo().HasEffect(Difficulty.None, SpellEffectName.Heal))
                spellId = SpellIds.AlchemistStoneExtraHeal;
            else if (eventInfo.GetDamageInfo().GetSpellInfo().HasEffect(Difficulty.None, SpellEffectName.Energize))
                spellId = SpellIds.AlchemistStoneExtraMana;

            if (spellId == 0)
                return;

            GetTarget().CastCustomSpell(spellId, SpellValueMod.BasePoint0, amount, GetTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // Item - 50351: Tiny Abomination in a Jar
    // 71406 - Anger Capacitor

    // Item - 50706: Tiny Abomination in a Jar (Heroic)
    // 71545 - Anger Capacitor
    [Script("spell_item_tiny_abomination_in_a_jar", 8)]
    [Script("spell_item_tiny_abomination_in_a_jar_hero", 7)]
    class spell_item_anger_capacitor : AuraScript
    {
        public spell_item_anger_capacitor(int stackAmount)
        {
            _stackAmount = stackAmount;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MoteOfAnger, SpellIds.ManifestAngerMainHand, SpellIds.ManifestAngerOffHand);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            caster.CastSpell((Unit)null, SpellIds.MoteOfAnger, true);
            Aura motes = caster.GetAura(SpellIds.MoteOfAnger);
            if (motes == null || motes.GetStackAmount() < _stackAmount)
                return;

            caster.RemoveAurasDueToSpell(SpellIds.MoteOfAnger);
            uint spellId = SpellIds.ManifestAngerMainHand;
            Player player = caster.ToPlayer();
            if (player)
                if (player.GetWeaponForAttack(WeaponAttackType.OffAttack, true) && RandomHelper.URand(0, 1) != 0)
                    spellId = SpellIds.ManifestAngerOffHand;

            caster.CastSpell(target, spellId, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        int _stackAmount;
    }

    [Script] // 26400 - Arcane Shroud
    class spell_item_arcane_shroud : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            int diff = (int)GetUnitOwner().getLevel() - 60;
            if (diff > 0)
                amount += 2 * diff;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModThreat));
        }
    }

    // Item - 31859: Darkmoon Card: Madness
    [Script] // 39446 - Aura of Madness
    class spell_item_aura_of_madness : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.BroadcastTextStorage.ContainsKey(TextIds.SayMadness) && ValidateSpellInfo(SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania,
                SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            uint[][] triggeredSpells =
            {
                    //CLASS_NONE
                    new uint[] { },
                    //CLASS_WARRIOR
                    new uint[] { SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.MartyrComplex },
                    //CLASS_PALADIN
                    new uint[]{ SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia },
                    //CLASS_HUNTER
                    new uint[]{ SpellIds.Delusional, SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia },
                    //CLASS_ROGUE
                    new uint[]{ SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.MartyrComplex },
                    //CLASS_PRIEST
                    new uint[]{ SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia },
                    //CLASS_DEATH_KNIGHT
                    new uint[]{ SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.MartyrComplex },
                    //CLASS_SHAMAN
                    new uint[] { SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia },
                    //CLASS_MAGE
                    new uint[]{ SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia },
                    //CLASS_WARLOCK
                    new uint[]{ SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia },
                    //CLASS_UNK
                    new uint[]{ },
                    //CLASS_DRUID
                    new uint[]{ SpellIds.Sociopath, SpellIds.Delusional, SpellIds.Kleptomania, SpellIds.Megalomania, SpellIds.Paranoia, SpellIds.Manic, SpellIds.Narcissism, SpellIds.MartyrComplex, SpellIds.Dementia }
                };

            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();
            uint spellId = triggeredSpells[(int)caster.GetClass()].SelectRandom();
            caster.CastSpell(caster, spellId, true, null, aurEff);

            if (RandomHelper.randChance(10))
                caster.Say(TextIds.SayMadness);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
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
            GetTarget().CastSpell(GetTarget(), RandomHelper.RAND(SpellIds.DementiaPos, SpellIds.DementiaNeg), true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodicDummy, 0, AuraType.PeriodicDummy));
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
                GetTarget().CastCustomSpell(SpellIds.ProtectionOfAncientKings, SpellValueMod.BasePoint0, absorb, eventInfo.GetProcTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
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
            OnEffectProc.Add(new EffectProcHandler(HandleStackDrop, 0, AuraType.ModRating));
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
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.DeadlyPrecision);
            GetCaster().CastCustomSpell(spellInfo.Id, SpellValueMod.AuraStack, (int)spellInfo.StackAmount, GetCaster(), true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura));
        }
    }

    // Item - 50362: Deathbringer's Will
    // 71519 - Item - Icecrown 25 Normal Melee Trinket

    // Item - 50363: Deathbringer's Will
    // 71562 - Item - Icecrown 25 Heroic Melee Trinket
    [Script("spell_item_deathbringers_will_normal", SpellIds.StrengthOfTheTaunka, SpellIds.AgilityOfTheVrykul, SpellIds.PowerOfTheTaunka, SpellIds.AimOfTheIronDwarves, SpellIds.SpeedOfTheVrykul)]
    [Script("spell_item_deathbringers_will_heroic", SpellIds.StrengthOfTheTaunkaHero, SpellIds.AgilityOfTheVrykulHero, SpellIds.PowerOfTheTaunkaHero, SpellIds.AimOfTheIronDwarvesHero, SpellIds.SpeedOfTheVrykulHero)]
    class spell_item_deathbringers_will : AuraScript
    {
        public spell_item_deathbringers_will(uint strengthSpellId, uint agilitySpellId, uint apSpellId, uint criticalSpellId, uint hasteSpellId)
        {
            _strengthSpellId = strengthSpellId;
            _agilitySpellId = agilitySpellId;
            _apSpellId = apSpellId;
            _criticalSpellId = criticalSpellId;
            _hasteSpellId = hasteSpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_strengthSpellId, _agilitySpellId, _apSpellId, _criticalSpellId, _hasteSpellId);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            uint[][] triggeredSpells =
            {
                    //CLASS_NONE
                    new uint[] { },
                    //CLASS_WARRIOR
                    new uint[] { _strengthSpellId, _criticalSpellId, _hasteSpellId },
                    //CLASS_PALADIN
                    new uint[] { _strengthSpellId, _criticalSpellId, _hasteSpellId },
                    //CLASS_HUNTER
                    new uint[] { _agilitySpellId, _criticalSpellId, _apSpellId },
                    //CLASS_ROGUE
                    new uint[] { _agilitySpellId, _hasteSpellId, _apSpellId },
                    //CLASS_PRIEST
                    new uint[] { },
                    //CLASS_DEATH_KNIGHT
                    new uint[] { _strengthSpellId, _criticalSpellId, _hasteSpellId },
                    //CLASS_SHAMAN
                    new uint[] { _agilitySpellId, _hasteSpellId, _apSpellId },
                    //CLASS_MAGE
                    new uint[] { },
                    //CLASS_WARLOCK
                    new uint[] { },
                    //CLASS_UNK
                    new uint[] { },
                    //CLASS_DRUID
                    new uint[] { _strengthSpellId, _agilitySpellId, _hasteSpellId }
                };

            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();
            var randomSpells = triggeredSpells[(int)caster.GetClass()];
            if (randomSpells.Empty())
                return;

            uint spellId = randomSpells.SelectRandom();
            caster.CastSpell(caster, spellId, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        uint _strengthSpellId;
        uint _agilitySpellId;
        uint _apSpellId;
        uint _criticalSpellId;
        uint _hasteSpellId;
    }

    [Script] // 47770 - Roll Dice
    class spell_item_decahedral_dwarven_dice : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            if (!CliDB.BroadcastTextStorage.ContainsKey(TextIds.DecahedralDwarvenDice))
                return false;
            return true;
        }

        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        void HandleScript(uint effIndex)
        {
            GetCaster().TextEmote(TextIds.DecahedralDwarvenDice, GetHitUnit());

            uint minimum = 1;
            uint maximum = 100;

            GetCaster().ToPlayer().DoRandomRoll(minimum, maximum);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 8342  - Defibrillate (Goblin Jumper Cables) have 33% chance on success
    // 22999 - Defibrillate (Goblin Jumper Cables XL) have 50% chance on success
    // 54732 - Defibrillate (Gnomish Army Knife) have 67% chance on success
    [Script("spell_item_goblin_jumper_cables", 33u, SpellIds.GoblinJumperCablesFail)]
    [Script("spell_item_goblin_jumper_cables_xl", 50u, SpellIds.GoblinJumperCablesXlFail)]
    [Script("spell_item_gnomish_army_knife", 67u, 0u)]
    class spell_item_defibrillate : SpellScript
    {
        public spell_item_defibrillate(uint chance, uint failSpell)
        {
            _chance = chance;
            _failSpell = failSpell;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            if (_failSpell != 0 && !ValidateSpellInfo(_failSpell))
                return false;
            return true;
        }

        void HandleScript(uint effIndex)
        {
            if (RandomHelper.randChance(_chance))
            {
                PreventHitDefaultEffect(effIndex);
                if (_failSpell != 0)
                    GetCaster().CastSpell(GetCaster(), _failSpell, true, GetCastItem());
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Resurrect));
        }

        uint _chance;
        uint _failSpell;
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
            GetTarget().CastSpell(GetTarget(), SpellIds.DesperateRage, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 2, AuraType.PeriodicTriggerSpell));
        }
    }

    // http://www.wowhead.com/item=6522 Deviate Fish
    [Script] // 8063 Deviate Fish
    class spell_item_deviate_fish : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.Sleepy, SpellIds.Invigorate, SpellIds.Shrink, SpellIds.PartyTime, SpellIds.HealthySpirit);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            uint spellId = RandomHelper.URand(SpellIds.Sleepy, SpellIds.HealthySpirit);
            caster.CastSpell(caster, spellId, true, null);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            eventInfo.GetActor().CastSpell((Unit)null, SpellIds.DiscerningEyeBeast, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
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

            WorldObject target = targets.FirstOrDefault();
            targets.Clear();
            targets.Add(target);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
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
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
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
            List<uint> possibleSpells = new List<uint>();
            switch (caster.GetClass())
            {
                case Class.Warlock:
                case Class.Mage:
                case Class.Priest:
                    possibleSpells.Add(SpellIds.FlaskOfTheNorthSp);
                    break;
                case Class.Deathknight:
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
                Log.outWarn(LogFilter.Spells, "Missing spells for class {0} in script spell_item_flask_of_the_north", caster.GetClass());
                return;
            }

            caster.CastSpell(caster, possibleSpells.SelectRandom(), true);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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

            int amount = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            Unit caster = eventInfo.GetActor();
            caster.CastCustomSpell(SpellIds.Shadowmend, SpellValueMod.BasePoint0, amount, null, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
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
            if (target)
            {
                if (RandomHelper.URand(0, 99) < 15)
                    caster.CastSpell(caster, SpellIds.GnomishDeathRaySelf, true);    // failure
                else
                    caster.CastSpell(target, SpellIds.GnomishDeathRayTarget, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // Item - 49982: Heartpierce
    // 71880 - Item - Icecrown 25 Normal Dagger Proc

    // Item - 50641: Heartpierce (Heroic)
    // 71892 - Item - Icecrown 25 Heroic Dagger Proc
    [Script("spell_item_heartpierce", SpellIds.InvigorationEnergy, SpellIds.InvigorationMana, SpellIds.InvigorationRage, SpellIds.InvigorationRp)]
    [Script("spell_item_heartpierce_hero", SpellIds.InvigorationEnergyHero, SpellIds.InvigorationManaHero, SpellIds.InvigorationRageHero, SpellIds.InvigorationRpHero)]
    class spell_item_heartpierce : AuraScript
    {
        public spell_item_heartpierce(uint energySpellId, uint manaSpellId, uint rageSpellId, uint rpSpellId)
        {
            _energySpellId = energySpellId;
            _manaSpellId = manaSpellId;
            _rageSpellId = rageSpellId;
            _rpSpellId = rpSpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_energySpellId, _manaSpellId, _rageSpellId, _rpSpellId);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();

            uint spellId;
            switch (caster.GetPowerType())
            {
                case PowerType.Mana:
                    spellId = _manaSpellId;
                    break;
                case PowerType.Energy:
                    spellId = _energySpellId;
                    break;
                case PowerType.Rage:
                    spellId = _rageSpellId;
                    break;
                // Death Knights can't use daggers, but oh well
                case PowerType.RunicPower:
                    spellId = _rpSpellId;
                    break;
                default:
                    return;
            }

            caster.CastSpell((Unit)null, spellId, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        uint _energySpellId;
        uint _manaSpellId;
        uint _rageSpellId;
        uint _rpSpellId;
    }

    [Script] // 40971 - Bonus Healing (Crystal Spire of Karabor)
    class spell_item_crystal_spire_of_karabor : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffect(0) != null;
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            int pct = GetSpellInfo().GetEffect(0).CalcValue();
            HealInfo healInfo = eventInfo.GetHealInfo();
            if (healInfo != null)
            {
                Unit healTarget = healInfo.GetTarget();
                if (healTarget)
                    if (healTarget.GetHealth() - healInfo.GetEffectiveHeal() <= healTarget.CountPctFromMaxHealth(pct))
                        return true;
            }

            return false;
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
        }
    }

    // http://www.wowhead.com/item=27388 Mr. Pinchy
    [Script] // 33060 Make a Wish
    class spell_item_make_a_wish : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
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
            caster.CastSpell(caster, spellId, true, null);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            if (eventInfo.GetTypeMask().HasAnyFlag(ProcFlags.DoneRangedAutoAttack | ProcFlags.DoneSpellRangedDmgClass))
            {
                // in that case, do not cast heal spell
                PreventDefaultAction();
                // but mana instead
                eventInfo.GetActor().CastSpell((Unit)null, SpellIds.MarkOfConquestEnergize, true, null, aurEff);
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    // http://www.wowhead.com/item=32686 Mingo's Fortune Giblets
    [Script] // 40802 Mingo's Fortune Generator
    class spell_item_mingos_fortune_generator : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            // Selecting one from Bloodstained Fortune item
            uint newitemid;
            switch (RandomHelper.URand(1, 20))
            {
                case 1: newitemid = 32688; break;
                case 2: newitemid = 32689; break;
                case 3: newitemid = 32690; break;
                case 4: newitemid = 32691; break;
                case 5: newitemid = 32692; break;
                case 6: newitemid = 32693; break;
                case 7: newitemid = 32700; break;
                case 8: newitemid = 32701; break;
                case 9: newitemid = 32702; break;
                case 10: newitemid = 32703; break;
                case 11: newitemid = 32704; break;
                case 12: newitemid = 32705; break;
                case 13: newitemid = 32706; break;
                case 14: newitemid = 32707; break;
                case 15: newitemid = 32708; break;
                case 16: newitemid = 32709; break;
                case 17: newitemid = 32710; break;
                case 18: newitemid = 32711; break;
                case 19: newitemid = 32712; break;
                case 20: newitemid = 32713; break;
                default:
                    return;
            }

            CreateItem(effIndex, newitemid);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            return eventInfo.GetProcTarget() && eventInfo.GetProcTarget().IsAlive();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int bp = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            GetTarget().CastCustomSpell(SpellIds.ItemNecroticTouchProc, SpellValueMod.BasePoint0, bp, eventInfo.GetProcTarget(), true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    // http://www.wowhead.com/item=10720 Gnomish Net-o-Matic Projector
    [Script] // 13120 Net-o-Matic
    class spell_item_net_o_matic : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.NetOMaticTriggered1, SpellIds.NetOMaticTriggered2, SpellIds.NetOMaticTriggered3);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
            {
                uint spellId = SpellIds.NetOMaticTriggered3;
                uint roll = RandomHelper.URand(0, 99);
                if (roll < 2)                            // 2% for 30 sec self root (off-like chance unknown)
                    spellId = SpellIds.NetOMaticTriggered1;
                else if (roll < 4)                       // 2% for 20 sec root, charge to target (off-like chance unknown)
                    spellId = SpellIds.NetOMaticTriggered2;

                GetCaster().CastSpell(target, spellId, true, null);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // http://www.wowhead.com/item=8529 Noggenfogger Elixir
    [Script] // 16589 Noggenfogger Elixir
    class spell_item_noggenfogger_elixir : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.NoggenfoggerElixirTriggered1, SpellIds.NoggenfoggerElixirTriggered2, SpellIds.NoggenfoggerElixirTriggered3);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            uint spellId = SpellIds.NoggenfoggerElixirTriggered3;
            switch (RandomHelper.URand(1, 3))
            {
                case 1:
                    spellId = SpellIds.NoggenfoggerElixirTriggered1;
                    break;
                case 2:
                    spellId = SpellIds.NoggenfoggerElixirTriggered2;
                    break;
            }

            caster.CastSpell(caster, spellId, true, null);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 29601 - Enlightenment (Pendant of the Violet Eye)
    class spell_item_pendant_of_the_violet_eye : AuraScript
    {
        bool CheckProc(ProcEventInfo eventInfo)
        {
            Spell spell = eventInfo.GetProcSpell();
            if (spell != null)
            {
                var costs = spell.GetPowerCost();
                var m = costs.FirstOrDefault(cost => cost.Power == PowerType.Mana && cost.Amount > 0);
                if (m != null)
                    return true;
            }

            return false;
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
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
            if (shield != null)
                if (shield.GetAmount() > bp0)
                    return;

            caster.CastCustomSpell(SpellIds.PersistentShieldTriggered, SpellValueMod.BasePoint0, bp0, target, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell));
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

            int bp = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
            Unit caster = eventInfo.GetActor();
            caster.CastCustomSpell(SpellIds.HealthLink, SpellValueMod.BasePoint0, bp, null, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 17512 - Piccolo of the Flaming Fire
    class spell_item_piccolo_of_the_flaming_fire : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Player target = GetHitPlayer();
            if (target)
                target.HandleEmoteCommand(Emote.StateDance);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // http://www.wowhead.com/item=6657 Savory Deviate Delight
    [Script] // 8213 Savory Deviate Delight
    class spell_item_savory_deviate_delight : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
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
                case 1: spellId = (caster.GetGender() == Gender.Male ? SpellIds.FlipOutMale : SpellIds.FlipOutFemale); break;
                // Yaaarrrr - pirate
                case 2: spellId = (caster.GetGender() == Gender.Male ? SpellIds.YaaarrrrMale : SpellIds.YaaarrrrFemale); break;
            }
            caster.CastSpell(caster, spellId, true, null);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // 48129 - Scroll of Recall
    // 60320 - Scroll of Recall II
    [Script] // 60321 - Scroll of Recall III
    class spell_item_scroll_of_recall : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
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
                case SpellIds.ScrollOfRecallII:  // Scroll of Recall II
                    maxSafeLevel = 70;
                    break;
                case SpellIds.ScrollOfRecallIII:  // Scroll of Recal III
                    maxSafeLevel = 80;
                    break;
                default:
                    break;
            }

            if (caster.getLevel() > maxSafeLevel)
            {
                caster.CastSpell(caster, SpellIds.Lost, true);

                // ALLIANCE from 60323 to 60330 - HORDE from 60328 to 60335
                uint spellId = SpellIds.ScrollOfRecallFailAlliance1;
                if (GetCaster().ToPlayer().GetTeam() == Team.Horde)
                    spellId = SpellIds.ScrollOfRecallFailHorde1;

                GetCaster().CastSpell(GetCaster(), spellId + RandomHelper.URand(0, 7), true);

                PreventHitDefaultEffect(effIndex);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.TeleportUnits));
        }
    }

    [Script] // 71169 - Shadow's Fate (Shadowmourne questline)
    class spell_item_unsated_craving : AuraScript
    {
        bool CheckProc(ProcEventInfo procInfo)
        {
            Unit caster = procInfo.GetActor();
            if (!caster || caster.GetTypeId() != TypeId.Player)
                return false;

            Unit target = procInfo.GetActionTarget();
            if (!target || target.GetTypeId() != TypeId.Unit || target.IsCritter() || (target.GetEntry() != CreatureIds.Sindragosa && target.IsSummon()))
                return false;

            return true;
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
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
            if (!caster || !target)
                return;

            caster.CastSpell(target, SpellIds.SoulFeast, TriggerCastFlags.FullMask);
        }

        public override void Register()
        {
            OnProc.Add(new AuraProcHandler(HandleProc));
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
            return eventInfo.GetProcTarget() && eventInfo.GetProcTarget().IsAlive();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().CastSpell(GetTarget(), SpellIds.ShadowmourneSoulFragment, true, null, aurEff);

            // this can't be handled in AuraScript of SoulFragments because we need to know victim
            Aura soulFragments = GetTarget().GetAura(SpellIds.ShadowmourneSoulFragment);
            if (soulFragments != null)
            {
                if (soulFragments.GetStackAmount() >= 10)
                {
                    GetTarget().CastSpell(eventInfo.GetProcTarget(), SpellIds.ShadowmourneChaosBaneDamage, true, null, aurEff);
                    soulFragments.Remove();
                }
            }
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
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
            AfterEffectApply.Add(new EffectApplyHandler(OnStackChange, 0, AuraType.ModStat, AuraEffectHandleModes.Real | AuraEffectHandleModes.Reapply));
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ModStat, AuraEffectHandleModes.Real));
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
            if (target)
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

                caster.CastSpell(target, spellId, true, GetCastItem());
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
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
            int amount = (int)caster.CountPctFromMaxHealth(aurEff.GetAmount());
            caster.CastCustomSpell(SpellIds.SwiftHandOfJusticeHeal, SpellValueMod.BasePoint0, amount, null, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script] // 28862 - The Eye of Diminution
    class spell_item_the_eye_of_diminution : AuraScript
    {
        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            int diff = (int)GetUnitOwner().getLevel() - 60;
            if (diff > 0)
                amount += diff;
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModThreat));
        }
    }

    // http://www.wowhead.com/item=44012 Underbelly Elixir
    [Script] // 59640 Underbelly Elixir
    class spell_item_underbelly_elixir : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
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
            caster.CastSpell(caster, spellId, true, null);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 126755 - Wormhole: Pandaria
    class spell_item_wormhole_pandaria : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.WormholeTargetLocations);
        }

        void HandleTeleport(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            uint spellId = SpellIds.WormholeTargetLocations.SelectRandom();
            GetCaster().CastSpell(GetHitUnit(), spellId, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleTeleport, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 47776 - Roll 'dem Bones
    class spell_item_worn_troll_dice : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            if (!CliDB.BroadcastTextStorage.ContainsKey(TextIds.WornTrollDice))
                return false;
            return true;
        }

        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        void HandleScript(uint effIndex)
        {
            GetCaster().TextEmote(TextIds.WornTrollDice, GetHitUnit());

            uint minimum = 1;
            uint maximum = 6;

            // roll twice
            GetCaster().ToPlayer().DoRandomRoll(minimum, maximum);
            GetCaster().ToPlayer().DoRandomRoll(minimum, maximum);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_item_red_rider_air_rifle : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.AirRifleHoldVisual, SpellIds.AirRifleShoot, SpellIds.AirRifleShootSelf);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target)
            {
                caster.CastSpell(caster, SpellIds.AirRifleHoldVisual, true);
                // needed because this spell shares GCD with its triggered spells (which must not be cast with triggered flag)
                Player player = caster.ToPlayer();
                if (player)
                    player.GetSpellHistory().CancelGlobalCooldown(GetSpellInfo());
                if (RandomHelper.URand(0, 4) != 0)
                    caster.CastSpell(target, SpellIds.AirRifleShoot, false);
                else
                    caster.CastSpell(caster, SpellIds.AirRifleShootSelf, false);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_item_create_heart_candy : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Player target = GetHitPlayer();
            if (target)
            {
                uint[] items = { ItemIds.HeartCandy1, ItemIds.HeartCandy2, ItemIds.HeartCandy3, ItemIds.HeartCandy4, ItemIds.HeartCandy5, ItemIds.HeartCandy6, ItemIds.HeartCandy7, ItemIds.HeartCandy8 };
                target.AddItem(items[RandomHelper.IRand(0, 7)], 1);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_item_book_of_glyph_mastery : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
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
            OnCheckCast.Add(new CheckCastHandler(CheckRequirement));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_item_gift_of_the_harvester : SpellScript
    {
        SpellCastResult CheckRequirement()
        {
            List<TempSummon> ghouls = new List<TempSummon>();
            GetCaster().GetAllMinionsByEntry(ghouls, CreatureIds.Ghoul);
            if (ghouls.Count >= CreatureIds.MaxGhouls)
            {
                SetCustomCastResultMessage(SpellCustomErrors.TooManyGhouls);
                return SpellCastResult.CustomError;
            }

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckRequirement));
        }
    }

    [Script]
    class spell_item_map_of_the_geyser_fields : SpellScript
    {
        SpellCastResult CheckSinkholes()
        {
            Unit caster = GetCaster();
            if (caster.FindNearestCreature(CreatureIds.SouthSinkhole, 30.0f, true) ||
                caster.FindNearestCreature(CreatureIds.NortheastSinkhole, 30.0f, true) ||
                caster.FindNearestCreature(CreatureIds.NorthwestSinkhole, 30.0f, true))
                return SpellCastResult.SpellCastOk;

            SetCustomCastResultMessage(SpellCustomErrors.MustBeCloseToSinkhole);
            return SpellCastResult.CustomError;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckSinkholes));
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
            OnEffectHit.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_ashbringer : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        void OnDummyEffect(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);

            Player player = GetCaster().ToPlayer();
            uint sound_id = RandomHelper.RAND(SoundIds.Ashbringer1, SoundIds.Ashbringer2, SoundIds.Ashbringer3, SoundIds.Ashbringer4, SoundIds.Ashbringer5, SoundIds.Ashbringer6,
                            SoundIds.Ashbringer7, SoundIds.Ashbringer8, SoundIds.Ashbringer9, SoundIds.Ashbringer10, SoundIds.Ashbringer11, SoundIds.Ashbringer12);

            // Ashbringers effect (spellID 28441) retriggers every 5 seconds, with a chance of making it say one of the above 12 sounds
            if (RandomHelper.URand(0, 60) < 1)
                player.PlayDirectSound(sound_id, player);
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(OnDummyEffect, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
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
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleTriggerSpell, 1, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_item_shimmering_vessel : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Creature target = GetHitCreature();
            if (target)
                target.setDeathState(DeathState.JustRespawned);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_purify_helboar_meat : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SummonPurifiedHelboarMeat, SpellIds.SummonToxicHelboarMeat);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.CastSpell(caster, RandomHelper.randChance(50) ? SpellIds.SummonPurifiedHelboarMeat : SpellIds.SummonToxicHelboarMeat, true, null);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_crystal_prison_dummy_dnd : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            if (Global.ObjectMgr.GetGameObjectTemplate(ObjectIds.ImprisonedDoomguard) == null)
                return false;
            return true;
        }

        void HandleDummy(uint effIndex)
        {
            Creature target = GetHitCreature();
            if (target)
            {
                if (target.IsDead() && !target.IsPet())
                {
                    GetCaster().SummonGameObject(ObjectIds.ImprisonedDoomguard, target, Quaternion.fromEulerAnglesZYX(target.GetOrientation(), 0.0f, 0.0f), (uint)(target.GetRespawnTime() - Time.UnixTime));
                    target.DespawnOrUnsummon();
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_reindeer_transformation : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.FlyingReindeer310, SpellIds.FlyingReindeer280, SpellIds.FlyingReindeer60, SpellIds.Reindeer100, SpellIds.Reindeer60);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster.HasAuraType(AuraType.Mounted))
            {
                float flyspeed = caster.GetSpeedRate(UnitMoveType.Flight);
                float speed = caster.GetSpeedRate(UnitMoveType.Run);

                caster.RemoveAurasByType(AuraType.Mounted);
                //5 different spells used depending on mounted speed and if mount can fly or not

                if (flyspeed >= 4.1f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellIds.FlyingReindeer310, true); //310% flying Reindeer
                else if (flyspeed >= 3.8f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellIds.FlyingReindeer280, true); //280% flying Reindeer
                else if (flyspeed >= 1.6f)
                    // Flying Reindeer
                    caster.CastSpell(caster, SpellIds.FlyingReindeer60, true); //60% flying Reindeer
                else if (speed >= 2.0f)
                    // Reindeer
                    caster.CastSpell(caster, SpellIds.Reindeer100, true); //100% ground Reindeer
                else
                    // Reindeer
                    caster.CastSpell(caster, SpellIds.Reindeer60, true); //60% ground Reindeer
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_nigh_invulnerability : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.NighInvulnerability, SpellIds.CompleteVulnerability);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Item castItem = GetCastItem();
            if (castItem)
            {
                if (RandomHelper.randChance(86))                  // Nigh-Invulnerability   - success
                    caster.CastSpell(caster, SpellIds.NighInvulnerability, true, castItem);
                else                                    // Complete Vulnerability - backfire in 14% casts
                    caster.CastSpell(caster, SpellIds.CompleteVulnerability, true, castItem);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_poultryizer : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.PoultryizerSuccess, SpellIds.PoultryizerBackfire);
        }

        void HandleDummy(uint effIndex)
        {
            if (GetCastItem() && GetHitUnit())
                GetCaster().CastSpell(GetHitUnit(), RandomHelper.randChance(80) ? SpellIds.PoultryizerSuccess : SpellIds.PoultryizerBackfire, true, GetCastItem());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_socrethars_stone : SpellScript
    {
        public override bool Load()
        {
            return (GetCaster().GetAreaId() == 3900 || GetCaster().GetAreaId() == 3742);
        }

        public override bool Validate(SpellInfo spell)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_demon_broiled_surprise : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return Global.ObjectMgr.GetCreatureTemplate(CreatureIds.AbyssalFlamebringer) != null
                && Global.ObjectMgr.GetQuestTemplate(QuestIds.SuperHotStew) != null
                && ValidateSpellInfo(SpellIds.CreateDemonBroiledSurprise);
        }

        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        void HandleDummy(uint effIndex)
        {
            Unit player = GetCaster();
            player.CastSpell(player, SpellIds.CreateDemonBroiledSurprise, false);
        }

        SpellCastResult CheckRequirement()
        {
            Player player = GetCaster().ToPlayer();
            if (player.GetQuestStatus(QuestIds.SuperHotStew) != QuestStatus.Incomplete)
                return SpellCastResult.CantDoThatRightNow;

            Creature creature = player.FindNearestCreature(CreatureIds.AbyssalFlamebringer, 10, false);
            if (creature)
                if (creature.IsDead())
                    return SpellCastResult.SpellCastOk;
            return SpellCastResult.NotHere;
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy));
            OnCheckCast.Add(new CheckCastHandler(CheckRequirement));
        }
    }

    [Script]
    class spell_item_complete_raptor_capture : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.RaptorCaptureCredit);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            if (GetHitCreature())
            {
                GetHitCreature().DespawnOrUnsummon();

                //cast spell Raptor Capture Credit
                caster.CastSpell(caster, SpellIds.RaptorCaptureCredit, true, null);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_impale_leviroth : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            if (Global.ObjectMgr.GetCreatureTemplate(CreatureIds.Leviroth) == null)
                return false;
            return true;
        }

        void HandleDummy(uint effIndex)
        {
            Creature target = GetHitCreature();
            if (target)
                if (target.GetEntry() == CreatureIds.Leviroth && !target.HealthBelowPct(95))
                {
                    target.CastSpell(target, SpellIds.LevirothSelfImpale, true);
                    target.ResetPlayerDamageReq();
                }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_brewfest_mount_transformation : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.MountRam100, SpellIds.MountRam60, SpellIds.MountKodo100, SpellIds.MountKodo60);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (caster.HasAuraType(AuraType.Mounted))
            {
                caster.RemoveAurasByType(AuraType.Mounted);
                uint spell_id;

                switch (GetSpellInfo().Id)
                {
                    case SpellIds.BrewfestMountTransform:
                        if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
                            spell_id = caster.GetTeam() == Team.Alliance ? SpellIds.MountRam100 : SpellIds.MountKodo100;
                        else
                            spell_id = caster.GetTeam() == Team.Alliance ? SpellIds.MountRam60 : SpellIds.MountKodo60;
                        break;
                    case SpellIds.BrewfestMountTransformReverse:
                        if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
                            spell_id = caster.GetTeam() == Team.Horde ? SpellIds.MountRam100 : SpellIds.MountKodo100;
                        else
                            spell_id = caster.GetTeam() == Team.Horde ? SpellIds.MountRam60 : SpellIds.MountKodo60;
                        break;
                    default:
                        return;
                }
                caster.CastSpell(caster, spell_id, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_nitro_boots : SpellScript
    {
        public override bool Load()
        {
            if (!GetCastItem())
                return false;
            return true;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.NitroBootsSuccess, SpellIds.NitroBootsBackfire);
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            bool success = caster.GetMap().IsDungeon() || RandomHelper.randChance(95);
            caster.CastSpell(caster, success ? SpellIds.NitroBootsSuccess : SpellIds.NitroBootsBackfire, true, GetCastItem());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_teach_language : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LearnGnomishBinary, SpellIds.LearnGoblinBinary);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();

            if (RandomHelper.randChance(34))
                caster.CastSpell(caster, caster.GetTeam() == Team.Alliance ? SpellIds.LearnGnomishBinary : SpellIds.LearnGoblinBinary, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_rocket_boots : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.RocketBootsProc);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();

            Battleground bg = caster.GetBattleground();
            if (bg)
                bg.EventPlayerDroppedFlag(caster);

            caster.GetSpellHistory().ResetCooldown(SpellIds.RocketBootsProc);
            caster.CastSpell(caster, SpellIds.RocketBootsProc, true, null);
        }

        SpellCastResult CheckCast()
        {
            if (GetCaster().IsInWater())
                return SpellCastResult.OnlyAbovewater;
            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_pygmy_oil : SpellScript
    {
        public override bool Validate(SpellInfo spell)
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
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_unusual_compass : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            caster.SetFacingTo(RandomHelper.FRand(0.0f, 2.0f * (float)Math.PI));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_chicken_cover : SpellScript
    {
        public override bool Load()
        {
            return GetCaster().GetTypeId() == TypeId.Player;
        }

        public override bool Validate(SpellInfo spell)
        {
            return Global.ObjectMgr.GetQuestTemplate(QuestIds.ChickenParty) != null
                && Global.ObjectMgr.GetQuestTemplate(QuestIds.FlownTheCoop) != null
                && ValidateSpellInfo(SpellIds.ChickenNet, SpellIds.CaptureChickenEscape);
        }

        void HandleDummy(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            Unit target = GetHitUnit();
            if (target)
            {
                if (!target.HasAura(SpellIds.ChickenNet) && (caster.GetQuestStatus(QuestIds.ChickenParty) == QuestStatus.Incomplete || caster.GetQuestStatus(QuestIds.FlownTheCoop) == QuestStatus.Incomplete))
                {
                    caster.CastSpell(caster, SpellIds.CaptureChickenEscape, true);
                    target.KillSelf();
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_muisek_vessel : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            Creature target = GetHitCreature();
            if (target)
                if (target.IsDead())
                    target.DespawnOrUnsummon();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_item_greatmothers_soulcatcher : SpellScript
    {
        void HandleDummy(uint effIndex)
        {
            if (GetHitUnit())
                GetCaster().CastSpell(GetCaster(), SpellIds.ForceCastSummonGnomeSoul);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // Item - 49310: Purified Shard of the Scale
    // 69755 - Purified Shard of the Scale - Equip Effect

    // Item - 49488: Shiny Shard of the Scale
    // 69739 - Shiny Shard of the Scale - Equip Effect
    [Script("spell_item_purified_shard_of_the_scale", SpellIds.PurifiedCauterizingHeal, SpellIds.PurifiedSearingFlames)]
    [Script("spell_item_shiny_shard_of_the_scale", SpellIds.ShinyCauterizingHeal, SpellIds.ShinySearingFlames)]
    class spell_item_shard_of_the_scale : AuraScript
    {
        public spell_item_shard_of_the_scale(uint healProcSpellId, uint damageProcSpellId)
        {
            _healProcSpellId = healProcSpellId;
            _damageProcSpellId = damageProcSpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_healProcSpellId, _damageProcSpellId);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            Unit caster = eventInfo.GetActor();
            Unit target = eventInfo.GetProcTarget();

            if (eventInfo.GetTypeMask().HasAnyFlag(ProcFlags.DoneSpellMagicDmgClassPos))
                caster.CastSpell(target, _healProcSpellId, true, null, aurEff);

            if (eventInfo.GetTypeMask().HasAnyFlag(ProcFlags.DoneSpellMagicDmgClassNeg))
                caster.CastSpell(target, _damageProcSpellId, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        uint _healProcSpellId;
        uint _damageProcSpellId;
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
                    caster.CastSpell(caster, SpellIds.SoulPreserverDruid, true, null, aurEff);
                    break;
                case Class.Paladin:
                    caster.CastSpell(caster, SpellIds.SoulPreserverPaladin, true, null, aurEff);
                    break;
                case Class.Priest:
                    caster.CastSpell(caster, SpellIds.SoulPreserverPriest, true, null, aurEff);
                    break;
                case Class.Shaman:
                    caster.CastSpell(caster, SpellIds.SoulPreserverShaman, true, null, aurEff);
                    break;
                default:
                    break;
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script("spell_item_sunwell_exalted_caster_neck", SpellIds.LightsWrath, SpellIds.ArcaneBolt)]
    [Script("spell_item_sunwell_exalted_melee_neck", SpellIds.LightsStrength, SpellIds.ArcaneStrike)]
    [Script("spell_item_sunwell_exalted_tank_neck", SpellIds.LightsWard, SpellIds.ArcaneInsight)]
    [Script("spell_item_sunwell_exalted_healer_neck", SpellIds.LightsSalvation, SpellIds.ArcaneSurge)]
    class spell_item_sunwell_neck : AuraScript
    {
        public spell_item_sunwell_neck(uint aldorSpellId, uint scryersSpellId)
        {
            _aldorSpellId = aldorSpellId;
            _scryersSpellId = scryersSpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.FactionStorage.ContainsKey(FactionIds.Aldor) && CliDB.FactionStorage.ContainsKey(FactionIds.Scryers)
                && ValidateSpellInfo(_aldorSpellId, _scryersSpellId);
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
            if (player.GetReputationRank(FactionIds.Aldor) == ReputationRank.Exalted)
                player.CastSpell(target, _aldorSpellId, true, null, aurEff);

            if (player.GetReputationRank(FactionIds.Scryers) == ReputationRank.Exalted)
                player.CastSpell(target, _scryersSpellId, true, null, aurEff);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }

        uint _aldorSpellId;
        uint _scryersSpellId;
    }

    [Script]
    class spell_item_toy_train_set_pulse : SpellScript
    {
        void HandleDummy(uint index)
        {
            Player target = GetHitUnit().ToPlayer();
            if (target)
            {
                target.HandleEmoteCommand(Emote.OneshotTrain);
                EmotesTextSoundRecord soundEntry = Global.DB2Mgr.GetTextSoundEmoteFor((uint)TextEmotes.Train, target.GetRace(), target.GetGender(), target.GetClass());
                if (soundEntry != null)
                    target.PlayDistanceSound(soundEntry.SoundId);
            }
        }

        void HandleTargets(List<WorldObject> targetList)
        {
            targetList.RemoveAll(obj => !obj.IsTypeId(TypeId.Player));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(HandleTargets, SpellConst.EffectAll, Targets.UnitSrcAreaAlly));
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
                            caster.CastSpell(caster, SpellIds.DeathChoiceNormalStrength, true, null, aurEff);
                        else
                            caster.CastSpell(caster, SpellIds.DeathChoiceNormalAgility, true, null, aurEff);
                        break;
                    }
                case SpellIds.DeathChoiceHeroicAura:
                    {
                        if (str > agi)
                            caster.CastSpell(caster, SpellIds.DeathChoiceHeroicStrength, true, null, aurEff);
                        else
                            caster.CastSpell(caster, SpellIds.DeathChoiceHeroicAgility, true, null, aurEff);
                        break;
                    }
                default:
                    break;
            }
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script("spell_item_lightning_capacitor", SpellIds.LightningCapacitorStack, SpellIds.LightningCapacitorTrigger)]
    [Script("spell_item_thunder_capacitor", SpellIds.ThunderCapacitorStack, SpellIds.ThunderCapacitorTrigger)]
    [Script("spell_item_toc25_normal_caster_trinket", SpellIds.Toc25CasterTrinketNormalStack, SpellIds.Toc25CasterTrinketNormalTrigger)]
    [Script("spell_item_toc25_heroic_caster_trinket", SpellIds.Toc25CasterTrinketHeroicStack, SpellIds.Toc25CasterTrinketHeroicTrigger)]
    class spell_item_trinket_stack : AuraScript
    {
        public spell_item_trinket_stack(uint stackSpell, uint triggerSpell)
        {
            _stackSpell = stackSpell;
            _triggerSpell = triggerSpell;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_stackSpell, _triggerSpell);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit caster = eventInfo.GetActor();

            caster.CastSpell(caster, _stackSpell, true, null, aurEff); // cast the stack

            Aura dummy = caster.GetAura(_stackSpell); // retrieve aura

            //dont do anything if it's not the right amount of stacks;
            if (dummy == null || dummy.GetStackAmount() < aurEff.GetAmount())
                return;

            // if right amount, remove the aura and cast real trigger
            caster.RemoveAurasDueToSpell(_stackSpell);
            Unit target = eventInfo.GetActionTarget();
            if (target)
                caster.CastSpell(target, _triggerSpell, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell));
        }

        uint _stackSpell;
        uint _triggerSpell;
    }

    [Script] // 57345 - Darkmoon Card: Greatness
    class spell_item_darkmoon_card_greatness : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DarkmoonCardStrenght, SpellIds.DarkmoonCardAgility, SpellIds.DarkmoonCardIntellect, SpellIds.DarkmoonCardVersatility);
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit caster = eventInfo.GetActor();
            float str = caster.GetStat(Stats.Strength);
            float agi = caster.GetStat(Stats.Agility);
            float intl = caster.GetStat(Stats.Intellect);
            float vers = 0.0f; // caster.GetStat(STAT_VERSATILITY);
            float stat = 0.0f;

            uint spellTrigger = SpellIds.DarkmoonCardStrenght;

            if (str > stat)
            {
                spellTrigger = SpellIds.DarkmoonCardStrenght;
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

            caster.CastSpell(caster, spellTrigger, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 27522, 40336 - Mana Drain
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
                caster.CastSpell(caster, SpellIds.ManaDrainEnergize, true, null, aurEff);

            if (target && target.IsAlive())
                caster.CastSpell(target, SpellIds.ManaDrainLeech, true, null, aurEff);
        }

        public override void Register()
        {
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script] // 51640 - Taunt Flag Targeting
    class spell_item_taunt_flag_targeting : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.BroadcastTextStorage.ContainsKey(TextIds.EmotePlantsFlag) && ValidateSpellInfo(SpellIds.TauntFlag);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj => !obj.IsTypeId(TypeId.Player) && !obj.IsTypeId(TypeId.Corpse));

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
            GetCaster().TextEmote(TextIds.EmotePlantsFlag, GetHitUnit(), false);

            GetCaster().CastSpell(GetHitUnit(), SpellIds.TauntFlag, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.CorpseSrcAreaEnemy));
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 13180 - Gnomish Mind Control Cap
    class spell_item_mind_control_cap_SpellScript : SpellScript
    {
        public override bool Load()
        {
            if (!GetCastItem())
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
            if (target)
            {
                if (RandomHelper.randChance(95))
                    caster.CastSpell(target, RandomHelper.randChance(32) ? SpellIds.Dullard : SpellIds.GnomishMindControlCap, true, GetCastItem());
                else
                    target.CastSpell(caster, SpellIds.GnomishMindControlCap, true); // backfire - 5% chance
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 8344 - Universal Remote (Gnomish Universal Remote)
    class spell_item_universal_remote_SpellScript : SpellScript
    {
        public override bool Load()
        {
            if (!GetCastItem())
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
            if (target)
            {
                uint chance = RandomHelper.URand(0, 99);
                if (chance < 15)
                    GetCaster().CastSpell(target, SpellIds.TargetLock, true, GetCastItem());
                else if (chance < 25)
                    GetCaster().CastSpell(target, SpellIds.MobilityMalfunction, true, GetCastItem());
                else
                    GetCaster().CastSpell(target, SpellIds.ControlMachine, true, GetCastItem());
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    // Item - 19950: Zandalarian Hero Charm
    // 24658 - Unstable Power
    // Item - 19949: Zandalarian Hero Medallion
    // 24661 - Restless Strength
    [Script("spell_item_unstable_power", SpellIds.UnstablePowerAuraStack)]
    [Script("spell_item_restless_strength", SpellIds.RestlessStrengthAuraStack)]
    class spell_item_zandalarian_charm : AuraScript
    {
        public spell_item_zandalarian_charm(uint SpellId)
        {
            _spellId = SpellId;
        }

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(_spellId);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo != null)
                if (spellInfo.Id != m_scriptSpellId)
                    return true;

            return false;
        }

        void HandleStackDrop(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            GetTarget().RemoveAuraFromStack(_spellId);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleStackDrop, 0, AuraType.Dummy));
        }

        uint _spellId;
    }

    [Script]
    class spell_item_artifical_stamina : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffect(1) != null;
        }

        public override bool Load()
        {
            return GetOwner().IsTypeId(TypeId.Player);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Item artifact = GetOwner().ToPlayer().GetItemByGuid(GetAura().GetCastItemGUID());
            if (artifact)
                amount = (int)(GetSpellInfo().GetEffect(1).BasePoints * artifact.GetTotalPurchasedArtifactPowers() / 100);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModTotalStatPercentage));
        }
    }

    [Script]
    class spell_item_artifical_damage : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return spellInfo.GetEffect(1) != null;
        }

        public override bool Load()
        {
            return GetOwner().IsTypeId(TypeId.Player);
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Item artifact = GetOwner().ToPlayer().GetItemByGuid(GetAura().GetCastItemGUID());
            if (artifact)
                amount = (int)(GetSpellInfo().GetEffect(1).BasePoints * artifact.GetTotalPurchasedArtifactPowers() / 100);
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModDamagePercentDone));
        }
    }

    [Script] // 28200 - Ascendance
    class spell_item_talisman_of_ascendance : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.TalismanOfAscendance);
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

    [Script] // 29602 - Jom Gabbar
    class spell_item_jom_gabbar : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.JomGabbar);
        }

        void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(effect.GetSpellEffectInfo().TriggerSpell);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script] // 45040 - Battle Trance
    class spell_item_battle_trance : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.BattleTrance);
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

    [Script] // 90900 - World-Queller Focus
    class spell_item_world_queller_focus : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.WorldQuellerFocus);
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

    // 118089 - Azure Water Strider
    // 127271 - Crimson Water Strider
    // 127272 - Orange Water Strider
    // 127274 - Jade Water Strider
    [Script] // 127278 - Golden Water Strider
    class spell_item_water_strider : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.AzureWaterStrider, SpellIds.CrimsonWaterStrider, SpellIds.OrangeWaterStrider, SpellIds.JadeWaterStrider, SpellIds.GoldenWaterStrider);
        }

        void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(GetSpellInfo().GetEffect(1).TriggerSpell);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Mounted, AuraEffectHandleModes.Real));
        }
    }

    // 144671 - Brutal Kinship
    [Script] // 145738 - Brutal Kinship
    class spell_item_brutal_kinship : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.BrutalKinship1, SpellIds.BrutalKinship2);
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
}
