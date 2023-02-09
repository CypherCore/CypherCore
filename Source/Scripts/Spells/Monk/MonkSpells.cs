// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol.Account.V1;
using Game.Spells;

namespace Scripts.Spells.Monk
{
    internal struct MonkSpells
    {
        public const uint CracklingJadeLightningChannel = 117952;
        public const uint CracklingJadeLightningChiProc = 123333;
        public const uint CracklingJadeLightningKnockback = 117962;
        public const uint CracklingJadeLightningKnockbackCd = 117953;
        public const uint ProvokeSingleTarget = 116189;
        public const uint ProvokeAoe = 118635;
        public const uint NoFeatherFall = 79636;
        public const uint RollBackward = 109131;
        public const uint RollForward = 107427;
        public const uint SoothingMist = 115175;
        public const uint StanceOfTheSpiritedCrane = 154436;
        public const uint StaggerDamageAura = 124255;
        public const uint StaggerHeavy = 124273;
        public const uint StaggerLight = 124275;
        public const uint StaggerModerate = 124274;
        public const uint SurgingMistHeal = 116995;
        public const uint ITEM_MONK_T14_TANK_4P = 123159;
        public const uint MONK_NPC_BLACK_OX_STATUE = 61146;
        public const uint MONK_NPC_JADE_SERPENT_STATUE = 60849;
        public const uint SPELL_MONK_BLACKOUT_KICK_DOT = 128531;
        public const uint SPELL_MONK_BLACKOUT_KICK_HEAL = 128591;
        public const uint SPELL_MONK_BLACKOUT_STRIKE = 205523;
        public const uint SPELL_MONK_BLACKOUT_KICK = 100784;
        public const uint SPELL_MONK_BLACKOUT_KICK_PROC = 116768;
        public const uint SPELL_MONK_BLACKOUT_KICK_TRIGGERED = 228649;
        public const uint SPELL_MONK_BLACK_OX_BREW = 115399;
        public const uint SPELL_MONK_BREATH_OF_FIRE = 115181;
        public const uint SPELL_MONK_BREATH_OF_FIRE_CONFUSED = 123393;
        public const uint SPELL_MONK_BREATH_OF_FIRE_DOT = 123725;
        public const uint SPELL_MONK_CHI_BURST_DAMAGE = 148135;
        public const uint SPELL_MONK_CHI_BURST_HEAL = 130654;
        public const uint SPELL_MONK_CHI_TORPEDO_DAMAGE = 117993;
        public const uint SPELL_MONK_CHI_TORPEDO_HEAL = 124040;
        public const uint SPELL_MONK_CHI_WAVE_DAMAGE = 132467;
        public const uint SPELL_MONK_CHI_WAVE_HEAL = 132463;
        public const uint SPELL_MONK_CHI_WAVE_HEALING_BOLT = 132464;
        public const uint SPELL_MONK_CHI_WAVE_TALENT_AURA = 115098;
        public const uint SPELL_MONK_CRACKLING_JADE_LIGHTNING_CHANNEL = 117952;
        public const uint SPELL_MONK_CRACKLING_JADE_LIGHTNING_CHI_PROC = 123333;
        public const uint SPELL_MONK_CRACKLING_JADE_LIGHTNING_KNOCKBACK = 117962;
        public const uint SPELL_MONK_CRACKLING_JADE_LIGHTNING_KNOCKBACK_CD = 117953;
        public const uint SPELL_MONK_CRACKLING_JADE_SHOCK_BUMP = 117962;
        public const uint SPELL_MONK_CREATE_CHI_SPHERE = 121286;
        public const uint SPELL_MONK_DISABLE = 116095;
        public const uint SPELL_MONK_DISABLE_ROOT = 116706;
        public const uint SPELL_MONK_DIZZYING_HAZE = 116330;
        public const uint SPELL_MONK_ELUSIVE_BRAWLER = 195630;
        public const uint SPELL_MONK_ELUSIVE_BREW = 115308;
        public const uint SPELL_MONK_ELUSIVE_BREW_STACKS = 128939;
        public const uint SPELL_MONK_EMINENCE_HEAL = 126890;
        public const uint SPELL_MONK_ENHANCED_ROLL = 157361;
        public const uint SPELL_MONK_ENVELOPING_MIST = 124682;
        public const uint SPELL_MONK_ENVELOPING_MIST_HEAL = 132120;
        public const uint SPELL_MONK_ESSENCE_FONT_HEAL = 191840;
        public const uint SPELL_MONK_FISTS_OF_FURY = 113656;
        public const uint SPELL_MONK_FISTS_OF_FURY_DAMAGE = 117418;
        public const uint SPELL_MONK_FLYING_SERPENT_KICK = 101545;
        public const uint SPELL_MONK_FLYING_SERPENT_KICK_AOE = 123586;
        public const uint SPELL_MONK_FLYING_SERPENT_KICK_NEW = 115057;
        public const uint SPELL_MONK_FORTIFYING_BREW = 120954;
        public const uint SPELL_MONK_GIFT_OF_THE_OX_AURA = 124502;
        public const uint SPELL_MONK_GIFT_OF_THE_OX_AT_RIGHT = 124503;
        public const uint SPELL_MONK_GIFT_OF_THE_OX_AT_LEFT = 124506;
        public const uint SPELL_MONK_GLYPH_OF_BLACKOUT_KICK = 132005;
        public const uint SPELL_MONK_GLYPH_OF_RENEWING_MIST = 123334;
        public const uint SPELL_MONK_GLYPH_OF_ZEN_FLIGHT = 125893;
        public const uint SPELL_MONK_GRAPPLE_WEAPON_DPS_UPGRADE = 123231;
        public const uint SPELL_MONK_GRAPPLE_WEAPON_HEAL_UPGRADE = 123234;
        public const uint SPELL_MONK_GRAPPLE_WEAPON_TANK_UPGRADE = 123232;
        public const uint SPELL_MONK_GUARD = 115295;
        public const uint SPELL_MONK_HEALING_ELIXIRS_AURA = 122280;
        public const uint SPELL_MONK_HEALING_ELIXIRS_RESTORE_HEALTH = 122281;
        public const uint SPELL_MONK_HEAVY_STAGGER = 124273;
        public const uint SPELL_MONK_ITEM_2_S12_MISTWEAVER = 131561;
        public const uint SPELL_MONK_ITEM_4_S12_MISTWEAVER = 124487;
        public const uint SPELL_MONK_ITEM_PVP_GLOVES_BONUS = 124489;
        public const uint SPELL_MONK_JADE_LIGHTNING_ENERGIZE = 123333;
        public const uint SPELL_MONK_KEG_SMASH_AURA = 121253;
        public const uint SPELL_MONK_KEG_SMASH_ENERGIZE = 127796;
        public const uint SPELL_MONK_KEG_SMASH_VISUAL = 123662;
        public const uint SPELL_MONK_LEGACY_OF_THE_EMPEROR = 117667;
        public const uint SPELL_MONK_LIFECYCLES_ENVELOPING_MIST = 197919;
        public const uint SPELL_MONK_LIFECYCLES_VIVIFY = 197916;
        public const uint SPELL_MONK_LIGHT_STAGGER = 124275;
        public const uint SPELL_MONK_MANA_TEA_REGEN = 115294;
        public const uint SPELL_MONK_MANA_TEA_STACKS = 115867;
        public const uint SPELL_MONK_MEDITATE_VISUAL = 124416;
        public const uint SPELL_MONK_MODERATE_STAGGER = 124274;
        public const uint SPELL_MONK_MORTAL_WOUNDS = 115804;
        public const uint SPELL_MONK_PATH_OF_BLOSSOM_AREATRIGGER = 122035;
        public const uint SPELL_MONK_PLUS_ONE_MANA_TEA = 123760;
        public const uint SPELL_MONK_POWER_STRIKES_TALENT = 121817;
        public const uint SPELL_MONK_POWER_STRIKES_AURA = 129914;
        public const uint SPELL_MONK_PROVOKE = 118635;
        public const uint SPELL_MONK_PROVOKE_AOE = 118635;
        public const uint SPELL_MONK_PROVOKE_SINGLE_TARGET = 116189;
        public const uint SPELL_MONK_PURIFYING_BREW = 119582;
        public const uint SPELL_MONK_COUNTERACT_MAGIC = 202428;
        public const uint SPELL_MONK_RENEWING_MIST = 115151;
        public const uint SPELL_MONK_RENEWING_MIST_HOT = 119611;
        public const uint SPELL_MONK_RENEWING_MIST_JUMP = 119607;
        public const uint SPELL_MONK_VISUAL_RENEWING_MIST = 24599;
        public const uint SPELL_MONK_RING_OF_PEACE_DISARM = 137461;
        public const uint SPELL_MONK_RING_OF_PEACE_SILENCE = 137460;
        public const uint SPELL_MONK_RISING_SUN_KICK = 107428;
        public const uint SPELL_MONK_RISING_THUNDER = 210804;
        public const uint SPELL_MONK_ROLL = 109132;
        public const uint SPELL_MONK_ROLL_ANIMATION = 111396;
        public const uint SPELL_MONK_ROLL_BACKWARD = 109131;
        public const uint SPELL_MONK_ROLL_TRIGGER = 107427;
        public const uint SPELL_MONK_SHUFFLE = 115307;
        public const uint SPELL_MONK_SONG_OF_CHIJI = 198909;
        public const uint SPELL_MONK_SOOTHING_MIST = 115175;
        public const uint SPELL_MONK_SOOTHING_MIST_AURA = 193884;
        public const uint SPELL_MONK_SOOTHING_MIST_ENERGIZE = 116335;
        public const uint SPELL_MONK_SOOTHING_MIST_VISUAL = 125955;
        public const uint SPELL_MONK_SPEAR_HAND_STRIKE_SILENCE = 116709;
        public const uint SPELL_MONK_SPINNING_FIRE_BLOSSOM_MISSILE = 118852;
        public const uint SPELL_MONK_SPINNING_FIRE_BLOSSOM_ROOT = 123407;
        public const uint SPELL_MONK_SPIRIT_OF_THE_CRANE_AURA = 210802;
        public const uint SPELL_MONK_SPIRIT_OF_THE_CRANE_MANA = 210803;
        public const uint SPELL_MONK_STAGGER = 124255;
        public const uint SPELL_MONK_STANCE_OF_THE_SPIRITED_CRANE = 154436;
        public const uint SPELL_MONK_SURGING_MIST_HEAL = 116995;
        public const uint SPELL_MONK_TEACHINGS_OF_THE_MONASTERY = 202090;
        public const uint SPELL_MONK_TEACHINGS_OF_THE_MONASTERY_PASSIVE = 116645;
        public const uint SPELL_MONK_TIGER_PALM = 100780;
        public const uint SPELL_MONK_THUNDER_FOCUS_TEA = 116680;
        public const uint SPELL_MONK_TIGEREYE_BREW = 116740;
        public const uint SPELL_MONK_TIGEREYE_BREW_STACKS = 125195;
        public const uint SPELL_MONK_TRANSCENDENCE_CLONE_TARGET = 119051;
        public const uint SPELL_MONK_TRANSCENDENCE_VISUAL = 119053;
        public const uint SPELL_MONK_TOUCH_OF_DEATH = 115080;
        public const uint SPELL_MONK_TOUCH_OF_DEATH_DAMAGE = 229980;
        public const uint SPELL_MONK_TOUCH_OF_DEATH_AMPLIFIER = 271232;
        public const uint SPELL_MONK_TOUCH_OF_KARMA = 122470;
        public const uint SPELL_MONK_TOUCH_OF_KARMA_BUFF = 125174;
        public const uint SPELL_MONK_TOUCH_OF_KARMA_DAMAGE = 124280;
        public const uint SPELL_MONK_UPLIFT_ALLOWING_CAST = 123757;
        public const uint SPELL_MONK_VIVIFY = 116670;
        public const uint SPELL_MONK_WAY_OF_THE_CRANE = 216113;
        public const uint SPELL_MONK_WAY_OF_THE_CRANE_HEAL = 216161;
        public const uint SPELL_MONK_WEAKENED_BLOWS = 115798;
        public const uint SPELL_MONK_WHIRLING_DRAGON_PUNCH = 152175;
        public const uint SPELL_MONK_WHIRLING_DRAGON_PUNCH_CASTER_AURA = 196742;
        public const uint SPELL_MONK_WHIRLING_DRAGON_PUNCH_DAMAGE = 158221;
        public const uint SPELL_MONK_WINDWALKER_AURA = 166646;
        public const uint SPELL_MONK_WINDWALKING = 157411;
        public const uint SPELL_MONK_XUEN_AURA = 123999;
        public const uint SPELL_MONK_ZEN_FLIGHT = 125883;
        public const uint SPELL_MONK_ZEN_FOCUS = 124488;
        public const uint SPELL_MONK_ZEN_PILGRIMAGE = 126892;
        public const uint SPELL_MONK_ZEN_PILGRIMAGE_RETURN = 126895;
        public const uint SPELL_MONK_ZEN_PILGREIMAGE_RETURN_AURA = 126896;
        public const uint SPELL_MONK_ZEN_PULSE_DAMAGE = 124081;
        public const uint SPELL_MONK_ZEN_PULSE_HEAL = 198487;
        public const uint SPELL_SERPENT_STATUE_SOOTHING_MIST = 248887;
        public const uint SPELL_MONK_RING_OF_PEACE_KNOCKBACK = 142895;
        public const uint SPELL_MONK_MYSTIC_TOUCH = 8647;
        public const uint SPELL_MONK_MYSTIC_TOUCH_TARGET_DEBUFF = 113746;
        public const uint SPELL_MONK_ESSENCE_FONT = 191837;
        public const uint SPELL_MONK_ESSENCE_FONT_PERIODIC_HEAL = 191840;
        public const uint SPELL_RISING_MIST = 274909;
        public const uint SPELL_RISING_MIST_HEAL = 274912;
        public const uint SPELL_LIFECYCLES = 197915;
        public const uint SPELL_SPIRIT_OF_THE_CRANE = 210802;
        public const uint SPELL_FOCUSED_THUNDER_TALENT = 197895;
        public const uint SPELL_GOOD_KARMA_TALENT = 195295;
        public const uint SPELL_GOOD_KARMA_TALENT_HEAL = 285594;
        public const uint SPELL_MONK_POWER_STRIKE_ENERGIZE = 121283;
    }


    // Roll trigger - 107427

    // Fists of Fury (stun effect) - 120086

    // Expel Harm - 115072

    // Chi Wave (healing bolt) - 132464

    // 132464 - Chi Wave (heal missile)

    // 132467 - Chi Wave (damage missile)

    // Called by Thunder Focus Tea - 116680
    // Item S12 4P - Mistweaver - 124487

    // Zen Flight - 125883

    // Enveloping Mist - 124682

    // Renewing Mist - 119611
    // En attente

    // Called by : Fortifying Brew - 115203, Chi Brew - 115399, Elusive Brew - 115308, Tigereye Brew - 116740
    // Purifying Brew - 119582, Mana Tea - 115294, Thunder Focus Tea - 116680 and Energizing Brew - 115288
    // Healing Elixirs - 122280
    // En attente

    // Zen Pulse - 124081

    // Chi Burst - 123986
    // En attente

    // Energizing Brew - 115288

    // Spear Hand Strike - 116705
    // En attente

    // Tigereye Brew - 116740
    // En attente

    // Tiger's Lust - 116841
    // En attente

    // Flying Serpent Kick - 115057

    // Chi Torpedo - 115008 or Chi Torpedo (3 charges) - 121828
    // En attente

    // Purifying Brew - 119582

    // Keg Smash - 121253

    // Elusive Brew - 115308
    // En attente

    // Soothing Mist - 115175

    // Disable - 116095

    // Disable - 116095

    // Zen Pilgrimage - 126892, Zen Pilgrimage - 194011

    // Zen Pilgrimage : Return - 126895

    // Blackout Kick - 100784
    // En attente

    // Paralysis - 115078
    // En attente

    // Touch of Death - 115080

    // 271232 - Touch of Death Amplifier - Triggers: 271233 on ToD Cast via proc

    // 271233 - Amplifier (Applied with ToD)

    // Fortifying brew - 115203

    // 115181 - Breath of Fire

    // 117962 - crackling jade lightning knockback

    // 121817 - Power Strike

    // 129914 - Power Strike Proc

    // 122470 - Touch of Karma


    // 125174 - Touch of Karma Buff

    // 101643

    // 210802 - Spirit of the Crane (Passive)

    // 101643

    // 119996 - Transcendence: Transfer

    // 115098 - Chi Wave

    // 132466 - Chi Wave (target selector)


    // Rising Thunder - 210804

    // 116645 - Teachings of the monastery (Passive)

    // 202090 - Teachings of the monastery (Buff)

    //Rising Sun Kick - 107428

    //191840 - Essence Font (Heal)


    //115313 - Summon Jade Serpent Statue


    // 115151 - Renewing Mist

    // 119611 - Renewing Mist (HoT)

    // 119607 - Renewing Mist Jump


    // Windwalking - 157411
    // AreaTriggerID - 2763

    // Spell 124503
    // AT ID : 3282

    //124502 - Gift of the Ox

    // 117906 - Mastery : Elusive Brawler

    // 195630 - Elusive Brawler

    // Dampen Harm - 122278

    //123986, 5300

    // Chi Burst heal - 130654

    //5484

    //137639

    // 69791 - 69792

    // 63508

    //115399

    //122280

    //202162 - Guard

    // Whirling Dragon Punch - 152175

    // Whirling Dragon Punch - 152175

    // 100780

    // 116670 - Vivify

    //60849

    //3983

    //8647

    //191837 - Essence Font

    // 210802 Spirit of the Crane
    // 228649 Blackout Kick triggered by Teachings of the Monastery

    // 116680 Thunder Focus Tea
    // 197895 Focused Thunder

    // 222029 - 205414 - Strike of the Windlord

    // 107427 - Roll
}