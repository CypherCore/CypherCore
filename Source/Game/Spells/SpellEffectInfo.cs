// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;

namespace Game.Spells
{
    public class SpellEffectInfo
    {

        private readonly SpellInfo _spellInfo;
        public uint EffectIndex { get; set; }

        public SpellEffectName Effect { get; set; }
        public AuraType ApplyAuraName { get; set; }
        public uint ApplyAuraPeriod { get; set; }
        public int BasePoints { get; set; }
        public float RealPointsPerLevel { get; set; }
        public float PointsPerResource { get; set; }
        public float Amplitude { get; set; }
        public float ChainAmplitude { get; set; }
        public float BonusCoefficient { get; set; }
        public int MiscValue { get; set; }
        public int MiscValueB { get; set; }
        public Mechanics Mechanic { get; set; }
        public float PositionFacing { get; set; }
        public SpellImplicitTargetInfo TargetA { get; set; } = new();
        public SpellImplicitTargetInfo TargetB { get; set; } = new();
        public SpellRadiusRecord RadiusEntry { get; set; }
        public SpellRadiusRecord MaxRadiusEntry { get; set; }
        public int ChainTargets { get; set; }
        public uint ItemType { get; set; }
        public uint TriggerSpell { get; set; }
        public FlagArray128 SpellClassMask { get; set; }
        public float BonusCoefficientFromAP { get; set; }
        public List<Condition> ImplicitTargetConditions { get; set; }
        public SpellEffectAttributes EffectAttributes { get; set; }
        public ScalingInfo Scaling;

        private readonly ImmunityInfo _immunityInfo;

        public class StaticData
        {
            public SpellEffectImplicitTargetTypes ImplicitTargetType; // defines what Target can be added to effect Target list if there's no valid Target Type provided for effect
            public SpellTargetObjectTypes UsedTargetObjectType;       // defines valid Target object Type for spell effect

            public StaticData(SpellEffectImplicitTargetTypes implicittarget, SpellTargetObjectTypes usedtarget)
            {
                ImplicitTargetType = implicittarget;
                UsedTargetObjectType = usedtarget;
            }
        }

        public struct ScalingInfo
        {
            public int Class { get; set; }
            public float Coefficient { get; set; }
            public float Variance { get; set; }
            public float ResourceCoefficient { get; set; }
        }

        private static readonly StaticData[] _data = new StaticData[(int)SpellEffectName.TotalSpellEffects]
                                                     {
                                                         // implicit Target Type           used Target object Type
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 0
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 1 SPELL_EFFECT_INSTAKILL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 2 SPELL_EFFECT_SCHOOL_DAMAGE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 3 SPELL_EFFECT_DUMMY
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 4 SPELL_EFFECT_PORTAL_TELEPORT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 5 SPELL_EFFECT_5
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 6 SPELL_EFFECT_APPLY_AURA
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 7 SPELL_EFFECT_ENVIRONMENTAL_DAMAGE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 8 SPELL_EFFECT_POWER_DRAIN
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 9 SPELL_EFFECT_HEALTH_LEECH
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 10 SPELL_EFFECT_HEAL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 11 SPELL_EFFECT_BIND
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 12 SPELL_EFFECT_PORTAL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 13 SPELL_EFFECT_TELEPORT_TO_RETURN_POINT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 14 SPELL_EFFECT_INCREASE_CURRENCY_CAP
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 15 SPELL_EFFECT_TELEPORT_WITH_SPELL_VISUAL_KIT_LOADING_SCREEN
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 16 SPELL_EFFECT_QUEST_COMPLETE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 17 SPELL_EFFECT_WEAPON_DAMAGE_NOSCHOOL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.CorpseAlly),  // 18 SPELL_EFFECT_RESURRECT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 19 SPELL_EFFECT_ADD_EXTRA_ATTACKS
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 20 SPELL_EFFECT_DODGE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 21 SPELL_EFFECT_EVADE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 22 SPELL_EFFECT_PARRY
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 23 SPELL_EFFECT_BLOCK
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 24 SPELL_EFFECT_CREATE_ITEM
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 25 SPELL_EFFECT_WEAPON
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 26 SPELL_EFFECT_DEFENSE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 27 SPELL_EFFECT_PERSISTENT_AREA_AURA
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 28 SPELL_EFFECT_SUMMON
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 29 SPELL_EFFECT_LEAP
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 30 SPELL_EFFECT_ENERGIZE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 31 SPELL_EFFECT_WEAPON_PERCENT_DAMAGE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 32 SPELL_EFFECT_TRIGGER_MISSILE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.GobjItem),    // 33 SPELL_EFFECT_OPEN_LOCK
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 34 SPELL_EFFECT_SUMMON_CHANGE_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 35 SPELL_EFFECT_APPLY_AREA_AURA_PARTY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 36 SPELL_EFFECT_LEARN_SPELL
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 37 SPELL_EFFECT_SPELL_DEFENSE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 38 SPELL_EFFECT_DISPEL
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 39 SPELL_EFFECT_LANGUAGE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 40 SPELL_EFFECT_DUAL_WIELD
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 41 SPELL_EFFECT_JUMP
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 42 SPELL_EFFECT_JUMP_DEST
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 43 SPELL_EFFECT_TELEPORT_UNITS_FACE_CASTER
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 44 SPELL_EFFECT_SKILL_STEP
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 45 SPELL_EFFECT_ADD_HONOR
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 46 SPELL_EFFECT_SPAWN
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 47 SPELL_EFFECT_TRADE_SKILL
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 48 SPELL_EFFECT_STEALTH
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 49 SPELL_EFFECT_DETECT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 50 SPELL_EFFECT_TRANS_DOOR
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 51 SPELL_EFFECT_FORCE_CRITICAL_HIT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 52 SPELL_EFFECT_SET_MAX_BATTLE_PET_COUNT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 53 SPELL_EFFECT_ENCHANT_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 54 SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 55 SPELL_EFFECT_TAMECREATURE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 56 SPELL_EFFECT_SUMMON_PET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 57 SPELL_EFFECT_LEARN_PET_SPELL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 58 SPELL_EFFECT_WEAPON_DAMAGE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 59 SPELL_EFFECT_CREATE_RANDOM_ITEM
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 60 SPELL_EFFECT_PROFICIENCY
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 61 SPELL_EFFECT_SEND_EVENT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 62 SPELL_EFFECT_POWER_BURN
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 63 SPELL_EFFECT_THREAT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 64 SPELL_EFFECT_TRIGGER_SPELL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 65 SPELL_EFFECT_APPLY_AREA_AURA_RAID
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 66 SPELL_EFFECT_RECHARGE_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 67 SPELL_EFFECT_HEAL_MAX_HEALTH
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 68 SPELL_EFFECT_INTERRUPT_CAST
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 69 SPELL_EFFECT_DISTRACT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 70 SPELL_EFFECT_COMPLETE_AND_REWARD_WORLD_QUEST
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 71 SPELL_EFFECT_PICKPOCKET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 72 SPELL_EFFECT_ADD_FARSIGHT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 73 SPELL_EFFECT_UNTRAIN_TALENTS
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 74 SPELL_EFFECT_APPLY_GLYPH
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 75 SPELL_EFFECT_HEAL_MECHANICAL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 76 SPELL_EFFECT_SUMMON_OBJECT_WILD
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 77 SPELL_EFFECT_SCRIPT_EFFECT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 78 SPELL_EFFECT_ATTACK
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 79 SPELL_EFFECT_SANCTUARY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 80 SPELL_EFFECT_MODIFY_FOLLOWER_ITEM_LEVEL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 81 SPELL_EFFECT_PUSH_ABILITY_TO_ACTION_BAR
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 82 SPELL_EFFECT_BIND_SIGHT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 83 SPELL_EFFECT_DUEL
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 84 SPELL_EFFECT_STUCK
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 85 SPELL_EFFECT_SUMMON_PLAYER
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Gobj),        // 86 SPELL_EFFECT_ACTIVATE_OBJECT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Gobj),        // 87 SPELL_EFFECT_GAMEOBJECT_DAMAGE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Gobj),        // 88 SPELL_EFFECT_GAMEOBJECT_REPAIR
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Gobj),        // 89 SPELL_EFFECT_GAMEOBJECT_SET_DESTRUCTION_STATE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 90 SPELL_EFFECT_KILL_CREDIT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 91 SPELL_EFFECT_THREAT_ALL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 92 SPELL_EFFECT_ENCHANT_HELD_ITEM
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 93 SPELL_EFFECT_FORCE_DESELECT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 94 SPELL_EFFECT_SELF_RESURRECT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 95 SPELL_EFFECT_SKINNING
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 96 SPELL_EFFECT_CHARGE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 97 SPELL_EFFECT_CAST_BUTTON
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 98 SPELL_EFFECT_KNOCK_BACK
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 99 SPELL_EFFECT_DISENCHANT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 100 SPELL_EFFECT_INEBRIATE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 101 SPELL_EFFECT_FEED_PET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 102 SPELL_EFFECT_DISMISS_PET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 103 SPELL_EFFECT_REPUTATION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 104 SPELL_EFFECT_SUMMON_OBJECT_SLOT1
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 105 SPELL_EFFECT_SURVEY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 106 SPELL_EFFECT_CHANGE_RAID_MARKER
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 107 SPELL_EFFECT_SHOW_CORPSE_LOOT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 108 SPELL_EFFECT_DISPEL_MECHANIC
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 109 SPELL_EFFECT_RESURRECT_PET
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 110 SPELL_EFFECT_DESTROY_ALL_TOTEMS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 111 SPELL_EFFECT_DURABILITY_DAMAGE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 112 SPELL_EFFECT_112
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 113 SPELL_EFFECT_CANCEL_CONVERSATION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 114 SPELL_EFFECT_ATTACK_ME
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 115 SPELL_EFFECT_DURABILITY_DAMAGE_PCT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.CorpseEnemy), // 116 SPELL_EFFECT_SKIN_PLAYER_CORPSE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 117 SPELL_EFFECT_SPIRIT_HEAL
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 118 SPELL_EFFECT_SKILL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 119 SPELL_EFFECT_APPLY_AREA_AURA_PET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 120 SPELL_EFFECT_TELEPORT_GRAVEYARD
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 121 SPELL_EFFECT_NORMALIZED_WEAPON_DMG
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 122 SPELL_EFFECT_122
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 123 SPELL_EFFECT_SEND_TAXI
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 124 SPELL_EFFECT_PULL_TOWARDS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 125 SPELL_EFFECT_MODIFY_THREAT_PERCENT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 126 SPELL_EFFECT_STEAL_BENEFICIAL_BUFF
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 127 SPELL_EFFECT_PROSPECTING
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 128 SPELL_EFFECT_APPLY_AREA_AURA_FRIEND
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 129 SPELL_EFFECT_APPLY_AREA_AURA_ENEMY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 130 SPELL_EFFECT_REDIRECT_THREAT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 131 SPELL_EFFECT_PLAY_SOUND
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 132 SPELL_EFFECT_PLAY_MUSIC
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 133 SPELL_EFFECT_UNLEARN_SPECIALIZATION
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 134 SPELL_EFFECT_KILL_CREDIT2
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 135 SPELL_EFFECT_CALL_PET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 136 SPELL_EFFECT_HEAL_PCT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 137 SPELL_EFFECT_ENERGIZE_PCT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 138 SPELL_EFFECT_LEAP_BACK
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 139 SPELL_EFFECT_CLEAR_QUEST
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 140 SPELL_EFFECT_FORCE_CAST
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 141 SPELL_EFFECT_FORCE_CAST_WITH_VALUE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 142 SPELL_EFFECT_TRIGGER_SPELL_WITH_VALUE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 143 SPELL_EFFECT_APPLY_AREA_AURA_OWNER
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 144 SPELL_EFFECT_KNOCK_BACK_DEST
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 145 SPELL_EFFECT_PULL_TOWARDS_DEST
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 146 SPELL_EFFECT_RESTORE_GARRISON_TROOP_VITALITY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 147 SPELL_EFFECT_QUEST_FAIL
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 148 SPELL_EFFECT_TRIGGER_MISSILE_SPELL_WITH_VALUE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 149 SPELL_EFFECT_CHARGE_DEST
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 150 SPELL_EFFECT_QUEST_START
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 151 SPELL_EFFECT_TRIGGER_SPELL_2
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 152 SPELL_EFFECT_SUMMON_RAF_FRIEND
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 153 SPELL_EFFECT_CREATE_TAMED_PET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 154 SPELL_EFFECT_DISCOVER_TAXI
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Unit),            // 155 SPELL_EFFECT_TITAN_GRIP
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 156 SPELL_EFFECT_ENCHANT_ITEM_PRISMATIC
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 157 SPELL_EFFECT_CREATE_LOOT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 158 SPELL_EFFECT_MILLING
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 159 SPELL_EFFECT_ALLOW_RENAME_PET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 160 SPELL_EFFECT_160
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 161 SPELL_EFFECT_TALENT_SPEC_COUNT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 162 SPELL_EFFECT_TALENT_SPEC_SELECT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 163 SPELL_EFFECT_OBLITERATE_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 164 SPELL_EFFECT_REMOVE_AURA
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 165 SPELL_EFFECT_DAMAGE_FROM_MAX_HEALTH_PCT
                                                         new(SpellEffectImplicitTargetTypes.Caster, SpellTargetObjectTypes.Unit),          // 166 SPELL_EFFECT_GIVE_CURRENCY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 167 SPELL_EFFECT_UPDATE_PLAYER_PHASE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 168 SPELL_EFFECT_ALLOW_CONTROL_PET
                                                         new(SpellEffectImplicitTargetTypes.Caster, SpellTargetObjectTypes.Unit),          // 169 SPELL_EFFECT_DESTROY_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 170 SPELL_EFFECT_UPDATE_ZONE_AURAS_AND_PHASES
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 171 SPELL_EFFECT_SUMMON_PERSONAL_GAMEOBJECT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.CorpseAlly),  // 172 SPELL_EFFECT_RESURRECT_WITH_AURA
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 173 SPELL_EFFECT_UNLOCK_GUILD_VAULT_TAB
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 174 SPELL_EFFECT_APPLY_AURA_ON_PET
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 175 SPELL_EFFECT_175
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 176 SPELL_EFFECT_SANCTUARY_2
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 177 SPELL_EFFECT_DESPAWN_PERSISTENT_AREA_AURA
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 178 SPELL_EFFECT_178
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 179 SPELL_EFFECT_CREATE_AREATRIGGER
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 180 SPELL_EFFECT_UPDATE_AREATRIGGER
                                                         new(SpellEffectImplicitTargetTypes.Caster, SpellTargetObjectTypes.Unit),          // 181 SPELL_EFFECT_REMOVE_TALENT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 182 SPELL_EFFECT_DESPAWN_AREATRIGGER
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 183 SPELL_EFFECT_183
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 184 SPELL_EFFECT_REPUTATION_2
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 185 SPELL_EFFECT_185
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 186 SPELL_EFFECT_186
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 187 SPELL_EFFECT_RANDOMIZE_ARCHAEOLOGY_DIGSITES
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest),        // 188 SPELL_EFFECT_SUMMON_STABLED_PET_AS_GUARDIAN
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 189 SPELL_EFFECT_LOOT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 190 SPELL_EFFECT_CHANGE_PARTY_MEMBERS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 191 SPELL_EFFECT_TELEPORT_TO_DIGSITE
                                                         new(SpellEffectImplicitTargetTypes.Caster, SpellTargetObjectTypes.Unit),          // 192 SPELL_EFFECT_UNCAGE_BATTLEPET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 193 SPELL_EFFECT_START_PET_BATTLE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 194 SPELL_EFFECT_194
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 195 SPELL_EFFECT_PLAY_SCENE_SCRIPT_PACKAGE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 196 SPELL_EFFECT_CREATE_SCENE_OBJECT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 197 SPELL_EFFECT_CREATE_PERSONAL_SCENE_OBJECT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 198 SPELL_EFFECT_PLAY_SCENE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 199 SPELL_EFFECT_DESPAWN_SUMMON
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 200 SPELL_EFFECT_HEAL_BATTLEPET_PCT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 201 SPELL_EFFECT_ENABLE_BATTLE_PETS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 202 SPELL_EFFECT_APPLY_AREA_AURA_SUMMONS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 203 SPELL_EFFECT_REMOVE_AURA_2
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 204 SPELL_EFFECT_CHANGE_BATTLEPET_QUALITY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 205 SPELL_EFFECT_LAUNCH_QUEST_CHOICE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 206 SPELL_EFFECT_ALTER_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 207 SPELL_EFFECT_LAUNCH_QUEST_TASK
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 208 SPELL_EFFECT_SET_REPUTATION
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 209 SPELL_EFFECT_209
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 210 SPELL_EFFECT_LEARN_GARRISON_BUILDING
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 211 SPELL_EFFECT_LEARN_GARRISON_SPECIALIZATION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 212 SPELL_EFFECT_REMOVE_AURA_BY_SPELL_LABEL
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 213 SPELL_EFFECT_JUMP_DEST_2
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 214 SPELL_EFFECT_CREATE_GARRISON
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 215 SPELL_EFFECT_UPGRADE_CHARACTER_SPELLS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 216 SPELL_EFFECT_CREATE_SHIPMENT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 217 SPELL_EFFECT_UPGRADE_GARRISON
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 218 SPELL_EFFECT_218
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 219 SPELL_EFFECT_CREATE_CONVERSATION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 220 SPELL_EFFECT_ADD_GARRISON_FOLLOWER
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 221 SPELL_EFFECT_ADD_GARRISON_MISSION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 222 SPELL_EFFECT_CREATE_HEIRLOOM_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 223 SPELL_EFFECT_CHANGE_ITEM_BONUSES
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 224 SPELL_EFFECT_ACTIVATE_GARRISON_BUILDING
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 225 SPELL_EFFECT_GRANT_BATTLEPET_LEVEL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 226 SPELL_EFFECT_TRIGGER_ACTION_SET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 227 SPELL_EFFECT_TELEPORT_TO_LFG_DUNGEON
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 228 SPELL_EFFECT_228
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 229 SPELL_EFFECT_SET_FOLLOWER_QUALITY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 230 SPELL_EFFECT_230
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 231 SPELL_EFFECT_INCREASE_FOLLOWER_EXPERIENCE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 232 SPELL_EFFECT_REMOVE_PHASE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 233 SPELL_EFFECT_RANDOMIZE_FOLLOWER_ABILITIES
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 234 SPELL_EFFECT_234
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 235 SPELL_EFFECT_235
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 236 SPELL_EFFECT_GIVE_EXPERIENCE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 237 SPELL_EFFECT_GIVE_RESTED_EXPERIENCE_BONUS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 238 SPELL_EFFECT_INCREASE_SKILL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 239 SPELL_EFFECT_END_GARRISON_BUILDING_CONSTRUCTION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 240 SPELL_EFFECT_GIVE_ARTIFACT_POWER
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 241 SPELL_EFFECT_241
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 242 SPELL_EFFECT_GIVE_ARTIFACT_POWER_NO_BONUS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 243 SPELL_EFFECT_APPLY_ENCHANT_ILLUSION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 244 SPELL_EFFECT_LEARN_FOLLOWER_ABILITY
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 245 SPELL_EFFECT_UPGRADE_HEIRLOOM
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 246 SPELL_EFFECT_FINISH_GARRISON_MISSION
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 247 SPELL_EFFECT_ADD_GARRISON_MISSION_SET
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 248 SPELL_EFFECT_FINISH_SHIPMENT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 249 SPELL_EFFECT_FORCE_EQUIP_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 250 SPELL_EFFECT_TAKE_SCREENSHOT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 251 SPELL_EFFECT_SET_GARRISON_CACHE_SIZE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 252 SPELL_EFFECT_TELEPORT_UNITS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 253 SPELL_EFFECT_GIVE_HONOR
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.Dest),            // 254 SPELL_EFFECT_JUMP_CHARGE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 255 SPELL_EFFECT_LEARN_TRANSMOG_SET
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 256 SPELL_EFFECT_256
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 257 SPELL_EFFECT_257
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 258 SPELL_EFFECT_MODIFY_KEYSTONE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 259 SPELL_EFFECT_RESPEC_AZERITE_EMPOWERED_ITEM
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 260 SPELL_EFFECT_SUMMON_STABLED_PET
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 261 SPELL_EFFECT_SCRAP_ITEM
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 262 SPELL_EFFECT_262
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 263 SPELL_EFFECT_REPAIR_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 264 SPELL_EFFECT_REMOVE_GEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 265 SPELL_EFFECT_LEARN_AZERITE_ESSENCE_POWER
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 266 SPELL_EFFECT_SET_ITEM_BONUS_LIST_GROUP_ENTRY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 267 SPELL_EFFECT_CREATE_PRIVATE_CONVERSATION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 268 SPELL_EFFECT_APPLY_MOUNT_EQUIPMENT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 269 SPELL_EFFECT_INCREASE_ITEM_BONUS_LIST_GROUP_STEP
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 270 SPELL_EFFECT_270
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 271 SPELL_EFFECT_APPLY_AREA_AURA_PARTY_NONRANDOM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 272 SPELL_EFFECT_SET_COVENANT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 273 SPELL_EFFECT_CRAFT_RUNEFORGE_LEGENDARY
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 274 SPELL_EFFECT_274
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 275 SPELL_EFFECT_275
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 276 SPELL_EFFECT_LEARN_TRANSMOG_ILLUSION
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 277 SPELL_EFFECT_SET_CHROMIE_TIME
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 278 SPELL_EFFECT_278
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 279 SPELL_EFFECT_LEARN_GARR_TALENT
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 280 SPELL_EFFECT_280
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 281 SPELL_EFFECT_LEARN_SOULBIND_CONDUIT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 282 SPELL_EFFECT_CONVERT_ITEMS_TO_CURRENCY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 283 SPELL_EFFECT_COMPLETE_CAMPAIGN
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 284 SPELL_EFFECT_SEND_CHAT_MESSAGE
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 285 SPELL_EFFECT_MODIFY_KEYSTONE_2
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 286 SPELL_EFFECT_GRANT_BATTLEPET_EXPERIENCE
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 287 SPELL_EFFECT_SET_GARRISON_FOLLOWER_LEVEL
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 288 SPELL_EFFECT_CRAFT_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 289 SPELL_EFFECT_MODIFY_AURA_STACKS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 290 SPELL_EFFECT_MODIFY_COOLDOWN
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 291 SPELL_EFFECT_MODIFY_COOLDOWNS
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 292 SPELL_EFFECT_MODIFY_COOLDOWNS_BY_CATEGORY
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 293 SPELL_EFFECT_MODIFY_CHARGES
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 294 SPELL_EFFECT_CRAFT_LOOT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 295 SPELL_EFFECT_SALVAGE_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 296 SPELL_EFFECT_CRAFT_SALVAGE_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 297 SPELL_EFFECT_RECRAFT_ITEM
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 298 SPELL_EFFECT_CANCEL_ALL_PRIVATE_CONVERSATIONS
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 299 SPELL_EFFECT_299
                                                         new(SpellEffectImplicitTargetTypes.None, SpellTargetObjectTypes.None),            // 300 SPELL_EFFECT_300
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item),        // 301 SPELL_EFFECT_CRAFT_ENCHANT
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.None),        // 302 SPELL_EFFECT_GATHERING
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit),        // 303 SPELL_EFFECT_CREATE_TRAIT_TREE_CONFIG
                                                         new(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit)         // 304 SPELL_EFFECT_CHANGE_ACTIVE_COMBAT_TRAIT_CONFIG
                                                     };

        public SpellEffectInfo(SpellInfo spellInfo, SpellEffectRecord effect = null)
        {
            _spellInfo = spellInfo;

            if (effect != null)
            {
                EffectIndex = (uint)effect.EffectIndex;
                Effect = (SpellEffectName)effect.Effect;
                ApplyAuraName = (AuraType)effect.EffectAura;
                ApplyAuraPeriod = effect.EffectAuraPeriod;
                BasePoints = (int)effect.EffectBasePoints;
                RealPointsPerLevel = effect.EffectRealPointsPerLevel;
                PointsPerResource = effect.EffectPointsPerResource;
                Amplitude = effect.EffectAmplitude;
                ChainAmplitude = effect.EffectChainAmplitude;
                BonusCoefficient = effect.EffectBonusCoefficient;
                MiscValue = effect.EffectMiscValue[0];
                MiscValueB = effect.EffectMiscValue[1];
                Mechanic = (Mechanics)effect.EffectMechanic;
                PositionFacing = effect.EffectPosFacing;
                TargetA = new SpellImplicitTargetInfo((Targets)effect.ImplicitTarget[0]);
                TargetB = new SpellImplicitTargetInfo((Targets)effect.ImplicitTarget[1]);
                RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(effect.EffectRadiusIndex[0]);
                MaxRadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(effect.EffectRadiusIndex[1]);
                ChainTargets = effect.EffectChainTargets;
                ItemType = effect.EffectItemType;
                TriggerSpell = effect.EffectTriggerSpell;
                SpellClassMask = effect.EffectSpellClassMask;
                BonusCoefficientFromAP = effect.BonusCoefficientFromAP;
                Scaling.Class = effect.ScalingClass;
                Scaling.Coefficient = effect.Coefficient;
                Scaling.Variance = effect.Variance;
                Scaling.ResourceCoefficient = effect.ResourceCoefficient;
                EffectAttributes = effect.EffectAttributes;
            }

            ImplicitTargetConditions = null;

            _immunityInfo = new ImmunityInfo();
        }

        public bool IsEffect()
        {
            return Effect != 0;
        }

        public bool IsEffect(SpellEffectName effectName)
        {
            return Effect == effectName;
        }

        public bool IsAura()
        {
            return (IsUnitOwnedAuraEffect() || Effect == SpellEffectName.PersistentAreaAura) && ApplyAuraName != 0;
        }

        public bool IsAura(AuraType aura)
        {
            return IsAura() && ApplyAuraName == aura;
        }

        public bool IsTargetingArea()
        {
            return TargetA.IsArea() || TargetB.IsArea();
        }

        public bool IsAreaAuraEffect()
        {
            if (Effect == SpellEffectName.ApplyAreaAuraParty ||
                Effect == SpellEffectName.ApplyAreaAuraRaid ||
                Effect == SpellEffectName.ApplyAreaAuraFriend ||
                Effect == SpellEffectName.ApplyAreaAuraEnemy ||
                Effect == SpellEffectName.ApplyAreaAuraPet ||
                Effect == SpellEffectName.ApplyAreaAuraOwner ||
                Effect == SpellEffectName.ApplyAreaAuraSummons ||
                Effect == SpellEffectName.ApplyAreaAuraPartyNonrandom)
                return true;

            return false;
        }

        public bool IsUnitOwnedAuraEffect()
        {
            return IsAreaAuraEffect() || Effect == SpellEffectName.ApplyAura || Effect == SpellEffectName.ApplyAuraOnPet;
        }

        public int CalcValue(WorldObject caster = null, int? bp = null, Unit target = null, uint castItemId = 0, int itemLevel = -1)
        {
            return CalcValue(out _, caster, bp, target, castItemId, itemLevel);
        }

        public int CalcValue(out float variance, WorldObject caster = null, int? bp = null, Unit target = null, uint castItemId = 0, int itemLevel = -1)
        {
            variance = 0.0f;
            double basePointsPerLevel = RealPointsPerLevel;
            // TODO: this needs to be a float, not rounded
            int basePoints = CalcBaseValue(caster, target, castItemId, itemLevel);
            double value = bp.HasValue ? bp.Value : basePoints;
            double comboDamage = PointsPerResource;

            Unit casterUnit = null;

            if (caster != null)
                casterUnit = caster.ToUnit();

            if (Scaling.Variance != 0)
            {
                float delta = Math.Abs(Scaling.Variance * 0.5f);
                double valueVariance = RandomHelper.FRand(-delta, delta);
                value += (double)basePoints * valueVariance;
                variance = (float)valueVariance;
            }

            // base amount modification based on spell lvl vs caster lvl
            if (Scaling.Coefficient != 0.0f)
            {
                if (Scaling.ResourceCoefficient != 0)
                    comboDamage = Scaling.ResourceCoefficient * value;
            }
            else if (GetScalingExpectedStat() == ExpectedStatType.None)
            {
                if (casterUnit != null &&
                    basePointsPerLevel != 0.0f)
                {
                    int level = (int)casterUnit.GetLevel();

                    if (level > (int)_spellInfo.MaxLevel &&
                        _spellInfo.MaxLevel > 0)
                        level = (int)_spellInfo.MaxLevel;

                    // if base level is greater than spell level, reduce by base level (eg. pilgrims foods)
                    level -= (int)Math.Max(_spellInfo.BaseLevel, _spellInfo.SpellLevel);

                    if (level < 0)
                        level = 0;

                    value += level * basePointsPerLevel;
                }
            }

            // random Damage
            if (casterUnit != null)
            {
                // bonus amount from combo points
                if (comboDamage != 0)
                {
                    uint comboPoints = casterUnit.GetComboPoints();

                    if (comboPoints != 0)
                        value += comboDamage * comboPoints;
                }

                if (caster != null)
                    value = caster.ApplyEffectModifiers(_spellInfo, EffectIndex, value);
            }

            return (int)Math.Round(value);
        }

        public int CalcBaseValue(WorldObject caster, Unit target, uint itemId, int itemLevel)
        {
            if (Scaling.Coefficient != 0.0f)
            {
                uint level = _spellInfo.SpellLevel;

                if (target &&
                    _spellInfo.IsPositiveEffect(EffectIndex) &&
                    (Effect == SpellEffectName.ApplyAura))
                    level = target.GetLevel();
                else if (caster != null &&
                         caster.IsUnit())
                    level = caster.ToUnit().GetLevel();

                if (_spellInfo.BaseLevel != 0 &&
                    !_spellInfo.HasAttribute(SpellAttr11.ScalesWithItemLevel) &&
                    _spellInfo.HasAttribute(SpellAttr10.UseSpellBaseLevelForScaling))
                    level = _spellInfo.BaseLevel;

                if (_spellInfo.Scaling.MinScalingLevel != 0 &&
                    _spellInfo.Scaling.MinScalingLevel > level)
                    level = _spellInfo.Scaling.MinScalingLevel;

                if (_spellInfo.Scaling.MaxScalingLevel != 0 &&
                    _spellInfo.Scaling.MaxScalingLevel < level)
                    level = _spellInfo.Scaling.MaxScalingLevel;

                float tempValue = 0.0f;

                if (level > 0)
                {
                    if (Scaling.Class == 0)
                        return 0;

                    uint effectiveItemLevel = itemLevel != -1 ? (uint)itemLevel : 1u;

                    if (_spellInfo.Scaling.ScalesFromItemLevel != 0 ||
                        _spellInfo.HasAttribute(SpellAttr11.ScalesWithItemLevel))
                    {
                        if (_spellInfo.Scaling.ScalesFromItemLevel != 0)
                            effectiveItemLevel = _spellInfo.Scaling.ScalesFromItemLevel;

                        if (Scaling.Class == -8 ||
                            Scaling.Class == -9)
                        {
                            RandPropPointsRecord randPropPoints = CliDB.RandPropPointsStorage.LookupByKey(effectiveItemLevel);

                            if (randPropPoints == null)
                                randPropPoints = CliDB.RandPropPointsStorage.LookupByKey(CliDB.RandPropPointsStorage.GetNumRows() - 1);

                            tempValue = Scaling.Class == -8 ? randPropPoints.DamageReplaceStatF : randPropPoints.DamageSecondaryF;
                        }
                        else
                        {
                            tempValue = ItemEnchantmentManager.GetRandomPropertyPoints(effectiveItemLevel, ItemQuality.Rare, InventoryType.Chest, 0);
                        }
                    }
                    else
                    {
                        tempValue = CliDB.GetSpellScalingColumnForClass(CliDB.SpellScalingGameTable.GetRow(level), Scaling.Class);
                    }

                    if (Scaling.Class == -7)
                    {
                        GtGenericMultByILvlRecord ratingMult = CliDB.CombatRatingsMultByILvlGameTable.GetRow(effectiveItemLevel);

                        if (ratingMult != null)
                        {
                            ItemSparseRecord itemSparse = CliDB.ItemSparseStorage.LookupByKey(itemId);

                            if (itemSparse != null)
                                tempValue *= CliDB.GetIlvlStatMultiplier(ratingMult, itemSparse.inventoryType);
                        }
                    }

                    if (Scaling.Class == -6)
                    {
                        GtGenericMultByILvlRecord staminaMult = CliDB.StaminaMultByILvlGameTable.GetRow(effectiveItemLevel);

                        if (staminaMult != null)
                        {
                            ItemSparseRecord itemSparse = CliDB.ItemSparseStorage.LookupByKey(itemId);

                            if (itemSparse != null)
                                tempValue *= CliDB.GetIlvlStatMultiplier(staminaMult, itemSparse.inventoryType);
                        }
                    }
                }

                tempValue *= Scaling.Coefficient;

                if (tempValue > 0.0f &&
                    tempValue < 1.0f)
                    tempValue = 1.0f;

                return (int)Math.Round(tempValue);
            }
            else
            {
                float tempValue = BasePoints;
                ExpectedStatType stat = GetScalingExpectedStat();

                if (stat != ExpectedStatType.None)
                {
                    if (_spellInfo.HasAttribute(SpellAttr0.ScalesWithCreatureLevel))
                        stat = ExpectedStatType.CreatureAutoAttackDps;

                    // TODO - add expansion and content tuning Id args?
                    uint contentTuningId = _spellInfo.ContentTuningId; // content tuning should be passed as arg, the one stored in SpellInfo is fallback
                    int expansion = -2;
                    ContentTuningRecord contentTuning = CliDB.ContentTuningStorage.LookupByKey(contentTuningId);

                    if (contentTuning != null)
                        expansion = contentTuning.ExpansionID;

                    uint level = caster != null && caster.IsUnit() ? caster.ToUnit().GetLevel() : 1;
                    tempValue = Global.DB2Mgr.EvaluateExpectedStat(stat, level, expansion, 0, Class.None) * BasePoints / 100.0f;
                }

                return (int)Math.Round(tempValue);
            }
        }

        public float CalcValueMultiplier(WorldObject caster, Spell spell = null)
        {
            float multiplier = Amplitude;
            Player modOwner = (caster?.GetSpellModOwner());

            modOwner?.ApplySpellMod(_spellInfo, SpellModOp.Amplitude, ref multiplier, spell);

            return multiplier;
        }

        public float CalcDamageMultiplier(WorldObject caster, Spell spell = null)
        {
            float multiplierPercent = ChainAmplitude * 100.0f;
            Player modOwner = (caster?.GetSpellModOwner());

            modOwner?.ApplySpellMod(_spellInfo, SpellModOp.ChainAmplitude, ref multiplierPercent, spell);

            return multiplierPercent / 100.0f;
        }

        public bool HasRadius()
        {
            return RadiusEntry != null;
        }

        public bool HasMaxRadius()
        {
            return MaxRadiusEntry != null;
        }

        public float CalcRadius(WorldObject caster = null, Spell spell = null)
        {
            SpellRadiusRecord entry = RadiusEntry;

            if (!HasRadius() &&
                HasMaxRadius())
                entry = MaxRadiusEntry;

            if (entry == null)
                return 0.0f;

            float radius = entry.RadiusMin;

            // Client uses max if min is 0
            if (radius == 0.0f)
                radius = entry.RadiusMax;

            if (caster != null)
            {
                Unit casterUnit = caster.ToUnit();

                if (casterUnit != null)
                    radius += entry.RadiusPerLevel * casterUnit.GetLevel();

                radius = Math.Min(radius, entry.RadiusMax);
                Player modOwner = caster.GetSpellModOwner();

                modOwner?.ApplySpellMod(_spellInfo, SpellModOp.Radius, ref radius, spell);
            }

            return radius;
        }

        public SpellCastTargetFlags GetProvidedTargetMask()
        {
            return SpellInfo.GetTargetFlagMask(TargetA.GetObjectType()) | SpellInfo.GetTargetFlagMask(TargetB.GetObjectType());
        }

        public SpellCastTargetFlags GetMissingTargetMask(bool srcSet = false, bool dstSet = false, SpellCastTargetFlags mask = 0)
        {
            var effImplicitTargetMask = SpellInfo.GetTargetFlagMask(GetUsedTargetObjectType());
            SpellCastTargetFlags providedTargetMask = GetProvidedTargetMask() | mask;

            // remove all Flags covered by effect Target mask
            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.UnitMask))
                effImplicitTargetMask &= ~SpellCastTargetFlags.UnitMask;

            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.CorpseMask))
                effImplicitTargetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask);

            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.GameobjectItem))
                effImplicitTargetMask &= ~(SpellCastTargetFlags.GameobjectItem | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.Item);

            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.Gameobject))
                effImplicitTargetMask &= ~(SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.GameobjectItem);

            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.Item))
                effImplicitTargetMask &= ~(SpellCastTargetFlags.Item | SpellCastTargetFlags.GameobjectItem);

            if (dstSet || Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.DestLocation))
                effImplicitTargetMask &= ~SpellCastTargetFlags.DestLocation;

            if (srcSet || Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.SourceLocation))
                effImplicitTargetMask &= ~SpellCastTargetFlags.SourceLocation;

            return effImplicitTargetMask;
        }

        public SpellEffectImplicitTargetTypes GetImplicitTargetType()
        {
            return _data[(int)Effect].ImplicitTargetType;
        }

        public SpellTargetObjectTypes GetUsedTargetObjectType()
        {
            return _data[(int)Effect].UsedTargetObjectType;
        }

        public ImmunityInfo GetImmunityInfo()
        {
            return _immunityInfo;
        }

        private ExpectedStatType GetScalingExpectedStat()
        {
            switch (Effect)
            {
                case SpellEffectName.SchoolDamage:
                case SpellEffectName.EnvironmentalDamage:
                case SpellEffectName.HealthLeech:
                case SpellEffectName.WeaponDamageNoSchool:
                case SpellEffectName.WeaponDamage:
                    return ExpectedStatType.CreatureSpellDamage;
                case SpellEffectName.Heal:
                case SpellEffectName.HealMechanical:
                    return ExpectedStatType.PlayerHealth;
                case SpellEffectName.Energize:
                case SpellEffectName.PowerBurn:
                    if (MiscValue == (int)PowerType.Mana)
                        return ExpectedStatType.PlayerMana;

                    return ExpectedStatType.None;
                case SpellEffectName.PowerDrain:
                    return ExpectedStatType.PlayerMana;
                case SpellEffectName.ApplyAura:
                case SpellEffectName.PersistentAreaAura:
                case SpellEffectName.ApplyAreaAuraParty:
                case SpellEffectName.ApplyAreaAuraRaid:
                case SpellEffectName.ApplyAreaAuraPet:
                case SpellEffectName.ApplyAreaAuraFriend:
                case SpellEffectName.ApplyAreaAuraEnemy:
                case SpellEffectName.ApplyAreaAuraOwner:
                case SpellEffectName.ApplyAuraOnPet:
                case SpellEffectName.ApplyAreaAuraSummons:
                case SpellEffectName.ApplyAreaAuraPartyNonrandom:
                    switch (ApplyAuraName)
                    {
                        case AuraType.PeriodicDamage:
                        case AuraType.ModDamageDone:
                        case AuraType.DamageShield:
                        case AuraType.ProcTriggerDamage:
                        case AuraType.PeriodicLeech:
                        case AuraType.ModDamageDoneCreature:
                        case AuraType.PeriodicHealthFunnel:
                        case AuraType.ModMeleeAttackPowerVersus:
                        case AuraType.ModRangedAttackPowerVersus:
                        case AuraType.ModFlatSpellDamageVersus:
                            return ExpectedStatType.CreatureSpellDamage;
                        case AuraType.PeriodicHeal:
                        case AuraType.ModDamageTaken:
                        case AuraType.ModIncreaseHealth:
                        case AuraType.SchoolAbsorb:
                        case AuraType.ModRegen:
                        case AuraType.ManaShield:
                        case AuraType.ModHealing:
                        case AuraType.ModHealingDone:
                        case AuraType.ModHealthRegenInCombat:
                        case AuraType.ModMaxHealth:
                        case AuraType.ModIncreaseHealth2:
                        case AuraType.SchoolHealAbsorb:
                            return ExpectedStatType.PlayerHealth;
                        case AuraType.PeriodicManaLeech:
                            return ExpectedStatType.PlayerMana;
                        case AuraType.ModStat:
                        case AuraType.ModAttackPower:
                        case AuraType.ModRangedAttackPower:
                            return ExpectedStatType.PlayerPrimaryStat;
                        case AuraType.ModRating:
                            return ExpectedStatType.PlayerSecondaryStat;
                        case AuraType.ModResistance:
                        case AuraType.ModBaseResistance:
                        case AuraType.ModTargetResistance:
                        case AuraType.ModBonusArmor:
                            return ExpectedStatType.ArmorConstant;
                        case AuraType.PeriodicEnergize:
                        case AuraType.ModIncreaseEnergy:
                        case AuraType.ModPowerCostSchool:
                        case AuraType.ModPowerRegen:
                        case AuraType.PowerBurn:
                        case AuraType.ModMaxPower:
                            if (MiscValue == (int)PowerType.Mana)
                                return ExpectedStatType.PlayerMana;

                            return ExpectedStatType.None;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }

            return ExpectedStatType.None;
        }
    }
}