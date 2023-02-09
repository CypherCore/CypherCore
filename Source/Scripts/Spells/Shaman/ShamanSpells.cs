using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    internal struct ShamanSpells
    {
        public const uint SPELL_PET_NETHERWINDS_FATIGUED = 160455;
        public const uint SPELL_SHAMAN_ANCESTRAL_AWAKENING = 52759;
        public const uint SPELL_SHAMAN_ANCESTRAL_AWAKENING_PROC = 52752;
        public const uint SPELL_SHAMAN_ANCESTRAL_GUIDANCE = 108281;
        public const uint SPELL_SHAMAN_ANCESTRAL_GUIDANCE_HEAL = 114911;
        public const uint SPELL_SHAMAN_ASCENDANCE = 114049;
        public const uint SPELL_SHAMAN_ASCENDANCE_ELEMENTAL = 114050;
        public const uint SPELL_SHAMAN_ASCENDANCE_ENHANCED = 114051;
        public const uint SPELL_SHAMAN_ASCENDANCE_RESTORATION = 114052;
        public const uint SPELL_SHAMAN_AT_EARTHEN_SHIELD_TOTEM = 198839;
        public const uint SPELL_SHAMAN_BIND_SIGHT = 6277;
        public const uint SPELL_SHAMAN_CONDUCTIVITY_HEAL = 118800;
        public const uint SPELL_SHAMAN_CONDUCTIVITY_TALENT = 108282;
        public const uint SPELL_SHAMAN_CRASH_LIGHTNING_PROC = 195592;
        public const uint SPELL_SHAMAN_CRASH_LIGTHNING = 187874;
        public const uint SPELL_SHAMAN_CRASH_LIGTHNING_AURA = 187878;
        public const uint SPELL_SHAMAN_CRASHING_STORM_DUMMY = 192246;
        public const uint SPELL_SHAMAN_CARESS_OF_THE_TIDEMOTHER = 207354;
        public const uint SPELL_SHAMAN_CARESS_OF_THE_TIDEMOTHER_AURA = 209950;
        public const uint SPELL_SHAMAN_DOOM_WINDS = 204945;
        public const uint SPELL_SHAMAN_EARTHBIND_FOR_EARTHGRAB_TOTEM = 116947;
        public const uint SPELL_SHAMAN_EARTHEN_RAGE_DAMAGE = 170379;
        public const uint SPELL_SHAMAN_EARTHEN_RAGE_PASSIVE = 170374;
        public const uint SPELL_SHAMAN_EARTHEN_RAGE_PERIODIC = 170377;
        public const uint SPELL_SHAMAN_EARTHGRAB_IMMUNITY = 116946;
        public const uint SPELL_SHAMAN_EARTHQUAKE = 61882;
        public const uint SPELL_SHAMAN_EARTHQUAKE_KNOCKING_DOWN = 77505;
        public const uint SPELL_SHAMAN_EARTHQUAKE_TICK = 77478;
        public const uint SPELL_SHAMAN_EARTH_ELEMENTAL_AGGRO = 235429;
        public const uint SPELL_SHAMAN_EARTH_ELEMENTAL_DUMMY = 198103;
        public const uint SPELL_SHAMAN_EARTH_ELEMENTAL_SUMMON = 188616;
        public const uint SPELL_SHAMAN_EARTH_SHIELD_HEAL = 379;
        public const uint SPELL_SHAMAN_EARTH_SHOCK = 8042;
        public const uint SPELL_SHAMAN_ECHO_OF_THE_ELEMENTS = 108283;
        public const uint SPELL_SHAMAN_ELEMENTAL_BLAST = 117014;
        public const uint SPELL_SHAMAN_ELEMENTAL_BLAST_CRIT = 118522;
        public const uint SPELL_SHAMAN_ELEMENTAL_BLAST_HASTE = 173183;
        public const uint SPELL_SHAMAN_ELEMENTAL_BLAST_MASTERY = 173184;
        public const uint SPELL_SHAMAN_ELEMENTAL_MASTERY = 16166;
        public const uint SPELL_SHAMAN_EXHAUSTION = 57723;
        public const uint SPELL_SHAMAN_FERAL_LUNGE = 196884;
        public const uint SPELL_SHAMAN_FERAL_LUNGE_DAMAGE = 215802;
        public const uint SPELL_SHAMAN_FERAL_SPIRIT = 51533;
        public const uint SPELL_SHAMAN_FERAL_SPIRIT_SUMMON = 228562;
        public const uint SPELL_SHAMAN_FERAL_SPIRIT_ENERGIZE = 190185;
        public const uint SPELL_SHAMAN_FERAL_SPIRIT_ENERGIZE_DUMMY = 231723;
        public const uint SPELL_SHAMAN_FIRE_ELEMENTAL_DUMMY = 198067;
        public const uint SPELL_SHAMAN_FIRE_ELEMENTAL_SUMMON = 188592;
        public const uint SPELL_SHAMAN_FIRE_NOVA = 1535;
        public const uint SPELL_SHAMAN_FIRE_NOVA_TRIGGERED = 131786;
        public const uint SPELL_SHAMAN_FLAMETONGUE_ATTACK = 10444;
        public const uint SPELL_SHAMAN_FLAMETONGUE_WEAPON_PASSIVE = 10400;
        public const uint SPELL_SHAMAN_FLAMETONGUE_WEAPON_ENCHANT = 334294;
        public const uint SPELL_SHAMAN_FLAMETONGUE_WEAPON_AURA = 319778;
        public const uint SPELL_SHAMAN_FLAME_SHOCK = 8050;
        public const uint SPELL_SHAMAN_FLAME_SHOCK_ELEM = 188389;
        public const uint SPELL_SHAMAN_FLAME_SHOCK_MAELSTROM = 188389;
        public const uint SPELL_SHAMAN_FROST_SHOCK_FREEZE = 63685;
        public const uint SPELL_SHAMAN_FROZEN_POWER = 63374;
        public const uint SPELL_SHAMAN_FULMINATION = 88766;
        public const uint SPELL_SHAMAN_FULMINATION_INFO = 95774;
        public const uint SPELL_SHAMAN_FULMINATION_TRIGGERED = 88767;
        public const uint SPELL_SHAMAN_FURY_OF_AIR = 197211;
        public const uint SPELL_SHAMAN_FURY_OF_AIR_EFFECT = 197385;
        public const uint SPELL_SHAMAN_GHOST_WOLF = 2645;
        public const uint SPELL_SHAMAN_GLYPH_OF_HEALING_STREAM = 119523;
        public const uint SPELL_SHAMAN_GLYPH_OF_HEALING_STREAM_TOTEM = 55456;
        public const uint SPELL_SHAMAN_GLYPH_OF_HEALING_STREAM_TOTEM_TRIGGERED = 119523;
        public const uint SPELL_SHAMAN_GLYPH_OF_HEALING_WAVE = 55533;
        public const uint SPELL_SHAMAN_GLYPH_OF_LAKESTRIDER = 55448;
        public const uint SPELL_SHAMAN_GLYPH_OF_LAVA_LASH = 55444;
        public const uint SPELL_SHAMAN_GLYPH_OF_SHAMANISTIC_RAGE = 63280;
        public const uint SPELL_SHAMAN_GLYPH_OF_THUNDERSTORM = 62132;
        public const uint SPELL_SHAMAN_HEALING_RAIN = 73920;
        public const uint SPELL_SHAMAN_HEALING_RAIN_TICK = 73921;
        public const uint SPELL_SHAMAN_HEALING_STREAM = 52042;
        public const uint SPELL_SHAMAN_HEALING_STREAM_DUMMY = 98856;
        public const uint SPELL_SHAMAN_HIGH_TIDE = 157154;
        public const uint SPELL_SHAMAN_HOT_HAND = 215785;
        public const uint SPELL_SHAMAN_IMPROVED_LIGHTNING_SHIELD = 157774;
        public const uint SPELL_SHAMAN_ITEM_LIGHTNING_SHIELD = 23552;
        public const uint SPELL_SHAMAN_ITEM_LIGHTNING_SHIELD_DAMAGE = 27635;
        public const uint SPELL_SHAMAN_ITEM_MANA_SURGE = 23571;
        public const uint SPELL_SHAMAN_ITEM_T14_4P = 123124;
        public const uint SPELL_SHAMAN_ITEM_T18_ELEMENTAL_2P_BONUS = 185880;
        public const uint SPELL_SHAMAN_ITEM_T18_ELEMENTAL_4P_BONUS = 185881;
        public const uint SPELL_SHAMAN_ITEM_T18_GATHERING_VORTEX = 189078;
        public const uint SPELL_SHAMAN_ITEM_T18_LIGHTNING_VORTEX = 189063;
        public const uint SPELL_SHAMAN_LAVA_BURST = 51505;
        public const uint SPELL_SHAMAN_LAVA_LASH = 60103;
        public const uint SPELL_SHAMAN_LAVA_LASH_SPREAD_FLAME_SHOCK = 105792;
        public const uint SPELL_SHAMAN_LAVA_SURGE = 77756;
        public const uint SPELL_SHAMAN_LAVA_SURGE_CAST_TIME = 77762;
        public const uint SPELL_SHAMAN_LIGHTNING_BOLT_ELEM = 188196;
        public const uint SPELL_SHAMAN_LIGHTNING_BOLT_ELEM_POWER = 214815;
        public const uint SPELL_SHAMAN_LIGHTNING_SHIELD = 324;
        public const uint SPELL_SHAMAN_LIGHTNING_SHIELD_AURA = 324;
        public const uint SPELL_SHAMAN_LIGHTNING_SHIELD_ORB_DAMAGE = 26364;
        public const uint SPELL_SHAMAN_LIGHTNING_SHIELD_TRIGGER = 26364;
        public const uint SPELL_SHAMAN_LIQUID_MAGMA_DAMAGE = 192231;
        public const uint SPELL_SHAMAN_MAELSTROM_WEAPON = 187880;
        public const uint SPELL_SHAMAN_MAELSTROM_WEAPON_POWER = 187890;
        public const uint SPELL_SHAMAN_MAIL_SPECIALISATION_INT = 86100;
        public const uint SPELL_SHAMAN_MAIL_SPECIALIZATION_AGI = 86099;
        public const uint SPELL_SHAMAN_MANA_TIDE = 16191;
        public const uint SPELL_SHAMAN_NATURE_GUARDIAN = 31616;
        public const uint SPELL_SHAMAN_OVERCHARGE = 210727;
        public const uint SPELL_SHAMAN_PATH_OF_FLAMES_SPREAD = 210621;
        public const uint SPELL_SHAMAN_PATH_OF_FLAMES_TALENT = 201909;
        public const uint SPELL_SHAMAN_RAINFALL = 215864;
        public const uint SPELL_SHAMAN_RAINFALL_HEAL = 215871;
        public const uint SPELL_SHAMAN_RESTORATIVE_MISTS = 114083;
        public const uint SPELL_SHAMAN_RIPTIDE = 61295;
        public const uint SPELL_SHAMAN_ROLLING_THUNDER_AURA = 88764;
        public const uint SPELL_SHAMAN_ROLLING_THUNDER_ENERGIZE = 88765;
        public const uint SPELL_SHAMAN_RUSHING_STREAMS = 147074;
        public const uint SPELL_SHAMAN_SATED = 57724;
        public const uint SPELL_SHAMAN_SEARING_FLAMES_DAMAGE_DONE = 77661;
        public const uint SPELL_SHAMAN_SOLAR_BEAM = 113286;
        public const uint SPELL_SHAMAN_SOLAR_BEAM_SILENCE = 113288;
        public const uint SPELL_SHAMAN_STONE_BULWARK_ABSORB = 114893;
        public const uint SPELL_SHAMAN_STORMBRINGER = 201845;
        public const uint SPELL_SHAMAN_STORMBRINGER_PROC = 201846;
        public const uint SPELL_SHAMAN_STORMLASH = 195255;
        public const uint SPELL_SHAMAN_STORMLASH_BUFF = 195222;
        public const uint SPELL_SHAMAN_STORMLASH_DAMAGE = 213307;
        public const uint SPELL_SHAMAN_STORMSTRIKE = 17364;
        public const uint SPELL_SHAMAN_STORMSTRIKE_MAIN = 32175;
        public const uint SPELL_SHAMAN_TIDAL_WAVES = 53390;
        public const uint SPELL_SHAMAN_TOTEM_HEALING_STREAM_HEAL = 52042;
        public const uint SPELL_SHAMAN_UNDULATION_PROC = 216251;
        public const uint SPELL_SHAMAN_UNLEASHED_FURY_EARTHLIVING = 118473;
        public const uint SPELL_SHAMAN_UNLEASHED_FURY_FLAMETONGUE = 118470;
        public const uint SPELL_SHAMAN_UNLEASHED_FURY_FROSTBRAND = 118474;
        public const uint SPELL_SHAMAN_UNLEASHED_FURY_ROCKBITER = 118475;
        public const uint SPELL_SHAMAN_UNLEASHED_FURY_TALENT = 117012;
        public const uint SPELL_SHAMAN_UNLEASHED_FURY_WINDFURY = 118472;
        public const uint SPELL_SHAMAN_UNLEASH_ELEMENTS = 73680;
        public const uint SPELL_SHAMAN_WATER_WALKING = 546;
        public const uint SPELL_SHAMAN_WELLSPRING_MISSILE = 198117;
        public const uint SPELL_SHAMAN_WINDFURY_ATTACK = 25504;
        public const uint SPELL_SHAMAN_WINDFURY_ATTACK_MAIN_HAND = 25504;
        public const uint SPELL_SHAMAN_WINDFURY_ATTACK_OFF_HAND = 33750;
        public const uint SPELL_SHAMAN_WINDFURY_WEAPON_PASSIVE = 33757;
        public const uint SPELL_SHAMAN_WIND_RUSH_TOTEM = 192077;
        public const uint SPELL_SHAMAN_FORCEFUL_WINDS = 262647;
        public const uint SPELL_SHAMAN_FORCEFUL_WINDS_MOD_DAMAGE_DONE = 262652;
        public const uint SPELL_SHAMAN_CRASHING_LIGHTNING_MOD_CL = 242286;
        public const uint SPELL_SHAMAN_CRASHING_LIGHTNING_DAMAGE = 195592;
        public const uint SPELL_SHAMAN_MASTERY_ELEMENTAL_OVERLOAD = 168534;
        public const uint SPELL_SHAMAN_CHAIN_LIGHTNING = 188443;
        public const uint SPELL_FROSTBRAND = 196834;
        public const uint SPELL_FROSTBRAND_SLOW = 147732;
        public const uint SPELL_HAILSTORM_TALENT = 210853;
        public const uint SPELL_HAILSTORM_TALENT_PROC = 210854;
        public const uint SPELL_FLAMETONGUE = 193796;
        public const uint SPELL_FLAMETONGUE_AURA = 194084;
        public const uint SPELL_SEARING_ASSAULT_TALENT = 192087;
        public const uint SPELL_SEARING_ASSULAT_TALENT_PROC = 268429;
        public const uint SPELL_GATHERING_STORMS_AURA = 198300;
        public const uint SPELL_CRASHING_STORM_TALENT_DAMAGE = 210801;
        public const uint SPELL_CRASHING_STORM_TALENT_AT = 210797
    }

    internal struct Resurgence
    {
        public const uint SPELL_WATER_SHIELD = 52127;
        public const uint SPELL_RESURGENCE = 16196;
        public const uint SPELL_RESURGENCE_PROC = 101033;

        /* Spells that can cause the proc  */
        public const uint SPELL_HEALING_WAVE = 331;
        public const uint SPELL_GREATER_HEALING_WAVE = 77472;
        public const uint SPELL_RIPTIDE = 61295;
        public const uint SPELL_HEALING_SURGE = 8004;
        public const uint SPELL_UNLEASH_LIFE = 73685;
        public const uint SPELL_CHAIN_HEAL = 1064;
    }

    internal struct TotemSpells
    {
        public const uint SPELL_TOTEM_WIND_RUSH_EFFECT = 192082;
        public const uint SPELL_TOTEM_VOODOO_AT = 196935;
        public const uint SPELL_TOTEM_VOODOO_EFFECT = 196942;
        public const uint SPELL_TOTEM_VOODOO_COOLDOWN = 202318;
        public const uint SPELL_TOTEM_LIGHTNING_SURGE_EFFECT = 118905;
        public const uint SPELL_TOTEM_RESONANCE_EFFECT = 202192;
        public const uint SPELL_TOTEM_LIQUID_MAGMA_EFFECT = 192226;
        public const uint SPELL_TOTEM_EARTH_GRAB_ROOT_EFFECT = 64695;
        public const uint SPELL_TOTEM_EARTH_GRAB_SLOW_EFFECT = 116947;
        public const uint SPELL_TOTEM_HEALING_TIDE_EFFECT = 114942;
        public const uint SPELL_TOTEM_TAIL_WIND_EFFECT = 210659;
        public const uint SPELL_TOTEM_EMBER_EFFECT = 210658;
        public const uint SPELL_TOTEM_STORM_EFFECT = 210652;
        public const uint SPELL_TOTEM_CLOUDBURST_EFFECT = 157504;
        public const uint SPELL_TOTEM_CLOUDBURST = 157503;
        public const uint SPELL_TOTEM_ANCESTRAL_PROTECTION_AT = 207495;
        public const uint SPELL_TOTEM_TOTEMIC_REVIVAL = 207553;
        public const uint SPELL_TOTEM_SKYFURY_EFFECT = 208963;
        public const uint SPELL_TOTEM_GROUDING_TOTEM_EFFECT = 8178
    }

    // Spells used by the Earthgrab Totem of the Shaman
    internal struct EarthgrabTotem
    {
        // Applied on the totem, ticks every two seconds and roots all targets within 10 yards (64695)
        public const uint SPELL_EARTHGRAB = 116943;
        public const uint SPELL_EARTHGRAB_PERIODIC = 64695;

        // When Earthgrab already hit a target, this target cannot be rooted a second time, and is instead
        // slowed with this one. (Same as the one used by Earthbind Totem)
        public const uint SPELL_EARTHBIND = 3600;
    }


    internal struct ShamanTotems
    {
        /* Entries */
        public const uint NPC_TOTEM_MAGMA = 5929;
        public const uint NPC_TOTEM_HEALING_STREAM = 3527;
        public const uint NPC_TOTEM_HEALING_TIDE = 59764;

        /* Spells */
        public const uint SPELL_MAGMA_TOTEM = 8188; // Ticks every two seconds, inflicting damages to all the creatures in a 8 yards radius
        public const uint SPELL_HEALING_STREAM = 5672; // Ticks every two seconds, targeting the group member with lowest hp in a 40 yards radius
        public const uint SPELL_HEALING_TIDE = 114941; // Ticks every two seconds, targeting 5 / 12 group / raid members with lowest hp in a 40 yards radius
    }

    internal struct ShamanSpellIcons
    {
        public const uint SHAMAN_ICON_ID_SOOTHING_RAIN = 2011;
        public const uint SHAMAN_ICON_ID_SHAMAN_LAVA_FLOW = 3087
    }

    internal struct MiscSpells
    {
        public const uint SPELL_HUNTER_INSANITY = 95809;
        public const uint SPELL_MAGE_TEMPORAL_DISPLACEMENT = 80354
    }

    internal struct AncestralAwakeningProc
    {
        public const uint SPELL_ANCESTRAL_AWAKENING_PROC = 52752
    }

    internal struct ShamanNpcs
    {
        public const uint NPC_RAINFALL = 73400;
        public const uint NPC_HEALING_RAIN = 73400
    }
}
