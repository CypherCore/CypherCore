// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    public struct PaladinSpells
    {
        public const uint AvengersShield = 31935;
        public const uint AvengingWrath = 31884;
        public const uint BeaconOfLight = 53563;
        public const uint BeaconOfLightHeal = 53652;
        public const uint BLESSING_OF_LOWER_CITY_DRUID = 37878;
        public const uint BLESSING_OF_LOWER_CITY_PALADIN = 37879;
        public const uint BLESSING_OF_LOWER_CITY_PRIEST = 37880;
        public const uint BLESSING_OF_LOWER_CITY_SHAMAN = 37881;
        public const uint ConsecratedGroundSlow = 204242;
        public const uint BLINDING_LIGHT_EFFECT = 105421;
        public const uint ConsecrationAura = 19746;
        public const uint ConsecrationProtectionAura = 188370;
        public const uint DIVINE_PURPOSE_PROC = 90174;
        public const uint DivineSteedHuman = 221883;
        public const uint DivineSteedDwarf = 276111;
        public const uint CONSECRATION_DAMAGE = 81297;
        public const uint DivineSteedDraenei = 221887;
        public const uint DivineSteedDarkIronDwarf = 276112;
        public const uint DivineSteedBloodelf = 221886;
        public const uint DivineSteedTauren = 221885;
        public const uint DivineSteedZandalariTroll = 294133;
        public const uint DIVINE_STORM_DAMAGE = 224239;
        public const uint EnduringLight = 40471;
        public const uint EnduringJudgement = 40472;
        public const uint EYE_FOR_AN_EYE_RANK_1 = 9799;
        public const uint EYE_FOR_AN_EYE_DAMAGE = 25997;
        public const uint EyeForAnEyeTriggered = 205202;
        public const uint FinalStand = 204077;
        public const uint FinalStandEffect = 204079;
        public const uint Forbearance = 25771;
        public const uint GuardianOfAcientKings = 86659;
        public const uint HammerOfJustice = 853;
        public const uint HandOfSacrifice = 6940;
        public const uint HolyMending = 64891;
        public const uint HolyPowerArmor = 28790;
        public const uint HolyPowerAttackPower = 28791;
        public const uint ConsecratedGroundPassive = 204054;
        public const uint HolyPowerSpellPower = 28793;
        public const uint HolyPowerMp5 = 28795;
        public const uint HOLY_SHOCK_R1 = 20473;
        public const uint HOLY_SHOCK_R1_DAMAGE = 25912;
        public const uint HOLY_SHOCK_R1_HEALING = 25914;
        public const uint ImmuneShieldMarker = 61988;
        public const uint ItemHealingTrance = 37706;
        public const uint JudgmentGainHolyPower = 220637;
        public const uint JudgmentProtRetR3 = 315867;
        public const uint JudgmentHolyR3 = 231644;
        public const uint JudgmentHolyR3Debuff = 214222;
        public const uint RIGHTEOUS_DEFENSE_TAUNT = 31790;
        public const uint RighteousVerdictAura = 267611;
        public const uint SEAL_OF_RIGHTEOUSNESS = 25742;
        public const uint TEMPLAR_VERDICT_DAMAGE = 224266;
        public const uint ZealAura = 269571;
        public const uint DARKEST_BEFORE_THE_DAWN = 210378;
        public const uint DARKEST_BEFORE_THE_DAWN_BUFF = 210391;
        public const uint FIST_OF_JUSTICE = 198054;
        public const uint FIST_OF_JUSTICE_RETRI = 234299;
        public const uint JUDGMENT_OF_LIGHT = 183778;
        public const uint JUDGMENT_OF_LIGHT_TARGET_DEBUFF = 196941;
        public const uint AWAKENING = 248033;
        public const uint HAND_OF_HINDRANCE = 183218;
        public const uint LAW_AND_ORDER = 204934;
        public const uint WAKE_OF_ASHES = 255937;
        public const uint WAKE_OF_ASHES_STUN = 255941;
        public const uint LIGHT_OF_DAWN = 85222;
        public const uint LIGHT_OF_DAWN_TRIGGER = 185984;
        public const uint ARCING_LIGHT_HEAL = 119952;
        public const uint HolyPrismTargetAlly = 114871;
        public const uint BEACON_OF_LIGHT_PROC_AURA = 53651;
        public const uint BEACON_OF_FAITH_PROC_AURA = 177173;
        public const uint BEACON_OF_FAITH = 156910;
        public const uint BEACON_OF_VIRTUE = 200025;
        public const uint InfusionOfLightAura = 54149;
        public const uint InfusionOfLightEnergize = 356717;
        public const uint HolyShockDamage = 25912;
        public const uint HolyShockHealing = 25914;
        public const uint BLADE_OF_JUSTICE = 184575;
        public const uint SHIELD_OF_VENGEANCE_DAMAGE = 184689;
        public const uint CRUSADER_STRIKE = 35395;
        public const uint ARDENT_DEFENDER = 31850;
        public const uint ARDENT_DEFENDER_HEAL = 66235;
        public const uint AURA_OF_SACRIFICE = 183416;
        public const uint AURA_OF_SACRIFICE_DAMAGE = 210380;
        public const uint LightHammerCosmetic = 122257;
        public const uint AURA_OF_SACRIFICE_ALLY = 210372;
        public const uint JUDGMENT_OF_LIGHT_HEAL = 183811;
        public const uint LightHammerPeriodic = 114918;
        public const uint LightHammerDamage = 114919;
        public const uint LightHammerHealing = 119952;
        public const uint LIGHT_OF_THE_MARTYR = 183998;
        public const uint LIGHT_OF_THE_MARTYR_DAMAGE = 196917;
        public const uint LIGHT_OF_THE_PROTECTOR = 184092;
        public const uint FERVENT_MARTYR_BUFF = 223316;
        public const uint GREATER_BLESSING_OF_KINGS = 203538;
        public const uint SERAPHIM = 152262;
        public const uint SHIELD_OF_THE_RIGHTEOUS = 53600;
        public const uint SHIELD_OF_THE_RIGHTEOUS_PROC = 132403;
        public const uint DivinePurposeTriggerred = 223819;
        public const uint HAND_OF_THE_PROTECTOR = 213652;
        public const uint CONSECRATION = 26573;
        public const uint CRUSADERS_MIGHT = 196926;
        public const uint DIVINE_INTERVENTION_HEAL = 184250;
        public const uint DIVINE_PURPOSE_HOLY = 197646;
        public const uint DIVINE_PURPOSE_HOLY_AURA_1 = 216411;
        public const uint DIVINE_PURPOSE_HOLY_AURA_2 = 216413;
        public const uint DIVINE_PURPOSE_RET = 223817;
        public const uint DIVINE_SHIELD = 642;
        public const uint DIVINE_STEED_SPEED = 220509;
        public const uint DIVINE_STORM = 53385;
        public const uint ARCING_LIGHT_DAMAGE = 114919;
        public const uint AURA_OF_SACRIFICE_HEAL = 210383;
        public const uint BLINDING_LIGHT_CONFUSE = 105421;
        public const uint CONSECRATION_HEAL = 204241;
        public const uint CRUSADERS_JUDGMENT = 204023;
        public const uint FINAL_STAND_TAUNT = 204079;
        public const uint FIRST_AVENGER = 203776;
        public const uint GRAND_CRUSADER_PROC = 85416;
        public const uint HAMMER_OF_RIGHTEOUS = 53595;
        public const uint HammerOfTheRighteousAoe = 88263;
        public const uint HOLY_LIGHT = 82326;
        public const uint HolyPrismTargetBeamVisual = 114862;
        public const uint HOLY_PRISM_DAMAGE_VISUAL_2 = 114870;
        public const uint HolyPrismTargetEnemy = 114852;
        public const uint HOLY_PRISM_HEAL_VISUAL = 121551;
        public const uint HOLY_PRISM_HEAL_VISUAL_2 = 121552;
        public const uint HolyShock = 20473;
        public const uint JUDGMENT = 20271;
        public const uint JUDGMENT_RETRI_DEBUFF = 197277;
        public const uint JUSTICARS_VENGEANCE = 215661;
        public const uint RETRIBUTION_AURA_DAMAGE = 204011;
        public const uint RIGHTEOUS_PROTECTOR = 204074;
        public const uint RIGHTEOUS_VERDICT = 267610;
        public const uint RIGHTEOUS_VERDICT_PROC = 267611;
        public const uint TEMPLARS_VERDICT = 85256;
        public const uint TemplarVerdictDamage = 224266;
        public const uint THE_FIRES_OF_JUSTICE = 209785;
        public const uint WORD_OF_GLORY = 210191;
        public const uint WORD_OF_GLORY_HEAL = 214894;
        public const uint BLESSED_HAMMER = 204019;
        public const uint EXORCISM_DF = 383185;
        public const uint ArtOfWarTriggered = 231843;
        public const uint AshenHallow = 316958;
        public const uint AshenHallowDamage = 317221;
        public const uint AshenHallowHeal = 317223;
        public const uint AshenHallowAllowHammer = 330382;
    }

    public struct PaladinNPCs
    {
        public const uint NPC_PALADIN_LIGHTS_HAMMER = 59738;
    }

    public struct PaladinSpellVisualKit
    {
        public const uint DivineStorm = 73892;
    }

    internal struct SpellVisual
    {
        public const uint HolyShockDamage = 83731;
        public const uint HolyShockDamageCrit = 83881;
        public const uint HolyShockHeal = 83732;
        public const uint HolyShockHealCrit = 83880;
    }
}
