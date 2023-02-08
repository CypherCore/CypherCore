// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Dynamic;
using Game.Scripting.BaseScripts;
using Game.Spells;

namespace Scripts.Spells.Hunter
{
    internal struct HunterSpells
    {
        public const uint AMurderOfCrowsDamage = 131900;
        public const uint AMurderOfCrowsVisual1 = 131637;
        public const uint AMurderOfCrowsVisual2 = 131951;
        public const uint AMurderOfCrowsVisual3 = 131952;
        public const uint AspectCheetahSlow = 186258;
        public const uint Exhilaration = 109304;
        public const uint ExhilarationPet = 128594;
        public const uint ExhilarationR2 = 231546;
        public const uint ExplosiveShotDamage = 212680;
        public const uint Lonewolf = 155228;
        public const uint MastersCallTriggered = 62305;
        public const uint Misdirection = 34477;
        public const uint MisdirectionProc = 35079;
        public const uint PetLastStandTriggered = 53479;
        public const uint PetHeartOfThePhoenixTriggered = 54114;
        public const uint PetHeartOfThePhoenixDebuff = 55711;
        public const uint PosthasteIncreaseSpeed = 118922;
        public const uint PosthasteTalent = 109215;
        public const uint SteadyShotFocus = 77443;
        public const uint T94PGreatness = 68130;
        public const uint DraeneiGiftOfTheNaaru = 59543;
        public const uint RoarOfSacrificeTriggered = 67481;
        public const uint SPELL_HUNTER_A_MURDER_OF_CROWS_1 = 131894;
        public const uint SPELL_HUNTER_A_MURDER_OF_CROWS_2 = 206505;
        public const uint SPELL_HUNTER_A_MURDER_OF_CROWS_DAMAGE = 131900;
        public const uint SPELL_HUNTER_ANIMAL_INSTINCTS = 204315;
        public const uint SPELL_HUNTER_ANIMAL_INSTINCTS_CHEETAH = 204324;
        public const uint SPELL_HUNTER_ANIMAL_INSTINCTS_MONGOOSE = 204333;
        public const uint SPELL_HUNTER_ANIMAL_INSTINCTS_RAPTOR = 204321;
        public const uint SPELL_HUNTER_ARCANE_SHOT = 185358;
        public const uint SPELL_HUNTER_ASPECT_OF_THE_CHEETAH_2 = 186258;
        public const uint SPELL_HUNTER_ASPECT_OF_THE_EAGLE = 186289;
        public const uint SPELL_HUNTER_AUTO_SHOT = 75;
        public const uint SPELL_HUNTER_BARRAGE = 120360;
        public const uint SPELL_HUNTER_BASIC_ATTACK_COST_MODIFIER = 62762;
        public const uint SPELL_HUNTER_BEAST_CLEAVE_AURA = 115939;
        public const uint SPELL_HUNTER_BEAST_CLEAVE_DAMAGE = 118459;
        public const uint SPELL_HUNTER_BEAST_CLEAVE_PROC = 118455;
        public const uint SPELL_HUNTER_BESTIAL_WRATH = 19574;
        public const uint SPELL_HUNTER_BLACK_ARROW = 194599;
        public const uint SPELL_HUNTER_BLINK_STRIKES = 130392;
        public const uint SPELL_HUNTER_BLINK_STRIKES_TELEPORT = 130393;
        public const uint SPELL_HUNTER_CAMOUFLAGE = 199483;
        public const uint SPELL_HUNTER_COBRA_SHOT = 193455;
        public const uint SPELL_HUNTER_DIRE_BEAST_GENERIC = 120679;
        public const uint SPELL_HUNTER_DIRE_FRENZY = 217200;
        public const uint SPELL_HUNTER_DIRE_FRENZY_DAMAGE = 217207;
        public const uint SPELL_BARBED_SHOT = 217200;
        public const uint SPELL_BARBED_SHOT_PLAYERAURA = 246152;
        public const uint SPELL_BARBED_SHOT_PETAURA = 272790;
        public const uint SPELL_HUNTER_DISENGAGE = 781;
        public const uint SPELL_HUNTER_EXHILARATION = 109304;
        public const uint SPELL_HUNTER_EXHILARATION_PET = 128594;
        public const uint SPELL_HUNTER_EXHILARATION_PET_AURA = 231546;
        public const uint SPELL_HUNTER_EXPLOSIVE_SHOT = 212431;
        public const uint SPELL_HUNTER_EXPLOSIVE_SHOT_DAMAGE = 212680;
        public const uint SPELL_HUNTER_EXPLOSIVE_SHOT_DETONATE = 212679;
        public const uint SPELL_HUNTER_FLANKING_STRIKE = 202800;
        public const uint SPELL_HUNTER_FLANKING_STRIKE_PROC = 204740;
        public const uint SPELL_HUNTER_FLANKING_STRIKE_PROC_UP = 206933;
        public const uint SPELL_HUNTER_FLARE_EFFECT = 28822;
        public const uint SPELL_HUNTER_FRENZY_STACKS = 19615;
        public const uint SPELL_HUNTER_HARPOON = 190925;
        public const uint SPELL_HUNTER_HARPOON_ROOT = 190927;
        public const uint SPELL_HUNTER_HUNTERS_MARK = 185987;
        public const uint SPELL_HUNTER_HUNTERS_MARK_AURA = 185365;
        public const uint SPELL_HUNTER_HUNTERS_MARK_AURA_2 = 185743;
        public const uint SPELL_HUNTER_IMPROVED_MEND_PET = 24406;
        public const uint SPELL_HUNTER_INTIMIDATION_STUN = 24394;
        public const uint SPELL_HUNTER_INVIGORATION_TRIGGERED = 53398;
        public const uint SPELL_HUNTER_KILL_COMMAND = 34026;
        public const uint SPELL_HUNTER_KILL_COMMAND_CHARGE = 118171;
        public const uint SPELL_HUNTER_KILL_COMMAND_TRIGGER = 83381;
        public const uint SPELL_HUNTER_LACERATE = 185855;
        public const uint SPELL_HUNTER_LONE_WOLF = 155228;
        public const uint SPELL_HUNTER_MARKED_SHOT = 185901;
        public const uint SPELL_HUNTER_MARKED_SHOT_DAMAGE = 212621;
        public const uint SPELL_HUNTER_MARKING_TARGETS = 223138;
        public const uint SPELL_HUNTER_MASTERS_CALL_TRIGGERED = 62305;
        public const uint SPELL_HUNTER_MISDIRECTION = 34477;
        public const uint SPELL_HUNTER_MISDIRECTION_PROC = 35079;
        public const uint SPELL_HUNTER_MONGOOSE_BITE = 190928;
        public const uint SPELL_HUNTER_MONGOOSE_FURY = 190931;
        public const uint SPELL_HUNTER_MULTISHOT = 2643;
        public const uint SPELL_HUNTER_PET_HEART_OF_THE_PHOENIX = 55709;
        public const uint SPELL_HUNTER_PET_HEART_OF_THE_PHOENIX_DEBUFF = 55711;
        public const uint SPELL_HUNTER_PET_HEART_OF_THE_PHOENIX_TRIGGERED = 54114;
        public const uint SPELL_HUNTER_PET_LAST_STAND_TRIGGERED = 53479;
        public const uint SPELL_HUNTER_POSTHAST = 109215;
        public const uint SPELL_HUNTER_POSTHAST_SPEED = 118922;
        public const uint SPELL_HUNTER_RANGERS_NET_INCREASE_SPEED = 206755;
        public const uint SPELL_HUNTER_RAPTOR_STRIKE = 186270;
        public const uint SPELL_HUNTER_SENTINEL = 206817;
        public const uint SPELL_HUNTER_SERPENT_STING = 87935;
        public const uint SPELL_HUNTER_SERPENT_STING_DAMAGE = 118253;
        public const uint SPELL_HUNTER_SPIKED_COLLAR = 53184;
        public const uint SPELL_HUNTER_STEADY_FOCUS = 193533;
        public const uint SPELL_HUNTER_STEADY_FOCUS_PROC = 193534;
        public const uint SPELL_HUNTER_STICKY_BOMB_PROC = 191244;
        public const uint SPELL_HUNTER_THOWING_AXES_DAMAGE = 200167;
        public const uint SPELL_HUNTER_TRAILBLAZER = 199921;
        public const uint SPELL_HUNTER_TRAILBLAZER_BUFF = 231390;
        public const uint SPELL_HUNTER_VULNERABLE = 187131;
        public const uint SPELL_HUNTER_WILD_CALL_AURA = 185791;
        public const uint SPELL_HUNTER_GENERIC_ENERGIZE_FOCUS = 91954;
        public const uint SPELL_HUNTER_PET_CARRION_FEEDER_TRIGGERED = 54045;
        public const uint SPELL_HUNTER_READINESS = 23989;
        public const uint SPELL_DRAENEI_GIFT_OF_THE_NAARU = 59543;
        public const uint SPELL_HUNTER_FIRE = 82926;
        public const uint SPELL_ROAR_OF_SACRIFICE_TRIGGERED = 67481;
        public const uint SPELL_HUNTER_SNIPER_TRAINING_R1 = 53302;
        public const uint SPELL_HUNTER_SNIPER_TRAINING_BUFF_R1 = 64418;
        public const uint SPELL_HUNTER_STEADY_SHOT_FOCUS = 77443;
        public const uint SPELL_HUNTER_LOCK_AND_LOAD = 56453;
        public const uint SPELL_HUNTER_GLAIVE_TOSS_AURA = 117050;
        public const uint SPELL_HUNTER_GLAIVE_TOSS_DAMAGE_AND_SNARE_LEFT = 120761;
        public const uint SPELL_HUNTER_GLAIVE_TOSS_DAMAGE_AND_SNARE_RIGHT = 121414;
        public const uint SPELL_HUNTER_GLAIVE_TOSS_RIGHT = 120755;
        public const uint SPELL_HUNTER_GLAIVE_TOSS_LEFT = 120756;
        public const uint SPELL_HUNTER_GLYPH_OF_STAMPEDE = 57902;
        public const uint SPELL_HUNTER_STAMPEDE_DAMAGE_REDUCTION = 130201;
        public const uint SPELL_HUNTER_BOND_SPIRIT_HEAL = 149254;
        public const uint SPELL_HUNTER_FOCUSING_SHOT = 152245;
        public const uint SPELL_HUNTER_FOCUSING_SHOT_AIM = 163485;
        public const uint SPELL_HUNTER_STEADY_FOCUS_TRIGGERED = 177668;
        public const uint SPELL_HUNTER_STEADY_SHOT = 56641;
        public const uint SPELL_HUNTER_BLINK_STRIKE = 130392;
        public const uint SPELL_HUNTER_BLINK_STRIKE_TELEPORT = 130393;
        public const uint SPELL_HUNTER_AURA_SHOOTING = 224729;
        public const uint SPELL_HUNTER_AIMED_SHOT = 19434;
        public const uint SPELL_HUNTER_GLYPH_OF_MISDIRECTION = 56829;
        public const uint SPELL_HUNTER_SCORCHING_WILDFIRE = 259496;
        public const uint SPELL_HUNTER_RAPID_FIRE = 257044;
        public const uint SPELL_HUNTER_RAPID_FIRE_MISSILE = 257045;
        public const uint SPELL_HUNTER_LETHAL_SHOTS = 260393;
        public const uint SPELL_HUNTER_CALLING_THE_SHOTS = 260404;
        public const uint SPELL_HUNTER_TRUESHOT = 288613;

    }


    // Harpoon - 190925

    // Snake Hunter - 201078

    // Way of the Mok'nathal - 201082

    // Mongoose Bite - 190928

    // 90355 - Ancient Hysteria

    // 53209 - chimera Shot

    // Barrage - 120361

    // Beast Cleave - 115939
    // Called by Multi-Shot - 2643

    // Beast Cleave Proc - 118455

    // Dire Beast - 120679

    // Kill Command - 34026


    // Kill Command (Damage) - 83381

    // 198670 Piercing Shot -

    // Arcane Shot - 185358

    // Feign Death - 5384

    // Intimidation - 19577

    // Killer Cobra - 199532


    // Disengage - 781


    // Farstrider - 199523

    // Throwing Axes - 200163

    // Ranger's Net - 200108

    // Sticky Bomb - 191241

    // Camouflage - 199483

    // Wild Call - 185789

    // Lock and Load - 194595

    // Sentinel - 206817
    // AreaTriggerID - 9769

    // Mortal Wounds - 201075

    // Raptor Strike - 186270

    // Carve - 187708

    // True Aim - 199527

    // Explosive Shot - 212679

    // 194386

    // 199921

    // Thunderstomp (Pet) - 63900


    // Basic Attack + Blink Strikes
    // Called by Smack - 49966, Bite - 17253, Claw - 16827

    // Cobra Spit (Pet) - 206685

    // 883 - Call Pet 1
    // 83242 - Call Pet 2
    // 83243 - Call Pet 3
    // 83244 - Call Pet 4
    // 83245 - Call Pet 5

    // Flare - 1543
    // AreaTriggerID - 510

    // Exposive Trap - 191433
    // AreaTriggerID - 1613

    // Freezing Trap - 187650
    // AreaTriggerID - 4424

    // Tar Trap (not activated) - 187698
    // AreaTriggerID - 4435

    // Tar Trap (activated) - 187700
    // AreaTriggerID - 4436

    // Binding Shot - 109248
    // AreaTriggerID - 1524

    // Caltrops - 194277 (194278)
    // AreaTriggerID - 5084


    // 194599 Black Arrow -
    // Reset Cooldown on creature kill

    // Glaive Toss (damage) - 120761 and 121414

    // Glaive Toss (Missile data) - 120755 and 120756


    // 155228 - Lone Wolf


    // 204147 - Windburst

    // 186265 - Aspect of the turtle

    // 19434 - Aimed shot

    // 214579 - Sidewinders

    // 186387 Bursting Shot --

    //19574

    //257044

    //217200 - Barbed Shot - 9.2.7

    // 53478 - Last Stand Pet

    // 53271 - Masters Call

    // 34477 - Misdirection

    // 35079 - Misdirection (Proc)

    // 55709 - Pet Heart of the Phoenix

    // 37506 - Scatter Shot

    // 56641 - Steady Shot

    // 1515 - Tame Beast
}