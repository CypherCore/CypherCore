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
using System;

namespace Framework.Constants
{
    public enum VehicleSeatFlags : uint
    {
        HasLowerAnimForEnter = 0x01,
        HasLowerAnimForRide = 0x02,
        DisableGravity = 0x04, // Passenger will not be affected by gravity
        ShouldUseVehSeatExitAnimOnVoluntaryExit = 0x08,
        Unk5 = 0x10,
        Unk6 = 0x20,
        Unk7 = 0x40,
        Unk8 = 0x80,
        Unk9 = 0x100,
        HidePassenger = 0x200, // Passenger Is Hidden
        AllowTurning = 0x400, // Needed For CgcameraSyncfreelookfacing
        CanControl = 0x800, // LuaUnitinvehiclecontrolseat
        CanCastMountSpell = 0x1000, // Can Cast Spells With SpellAuraMounted From Seat (Possibly 4.X Only, 0 Seats On 3.3.5a)
        Uncontrolled = 0x2000, // Can Override !& CanEnterOrExit
        CanAttack = 0x4000, // Can Attack, Cast Spells And Use Items From Vehicle
        ShouldUseVehSeatExitAnimOnForcedExit = 0x8000,
        Unk17 = 0x10000,
        Unk18 = 0x20000, // Needs Research And Support (28 Vehicles): Allow Entering Vehicles While Keeping Specific Permanent(?) Auras That Impose Visuals (States Like Beeing Under Freeze/Stun Mechanic, Emote State Animations).
        HasVehExitAnimVoluntaryExit = 0x40000,
        HasVehExitAnimForcedExit = 0x80000,
        PassengerNotSelectable = 0x100000,
        Unk22 = 0x200000,
        RecHasVehicleEnterAnim = 0x400000,
        IsUsingVehicleControls = 0x800000, // LuaIsusingvehiclecontrols
        EnableVehicleZoom = 0x1000000,
        CanEnterOrExit = 0x2000000, // LuaCanexitvehicle - Can Enter And Exit At Free Will
        CanSwitch = 0x4000000, // LuaCanswitchvehicleseats
        HasStartWaritingForVehTransitionAnimEnter = 0x8000000,
        HasStartWaritingForVehTransitionAnimExit = 0x10000000,
        CanCast = 0x20000000, // LuaUnithasvehicleui
        Unk2 = 0x40000000, // Checked In Conjunction With 0x800 In Castspell2
        AllowsInteraction = 0x80000000
    }

    public enum VehicleSeatFlagsB : uint
    {
        None = 0x00,
        UsableForced = 0x02,
        TargetsInRaidUi = 0x08,           // Lua_Unittargetsvehicleinraidui
        Ejectable = 0x20,           // Ejectable
        UsableForced2 = 0x40,
        UsableForced3 = 0x100,
        KeepPet = 0x20000,
        UsableForced4 = 0x02000000,
        CanSwitch = 0x4000000,
        VehiclePlayerframeUi = 0x80000000            // Lua_Unithasvehicleplayerframeui - Actually Checked For Flagsb &~ 0x80000000
    }

    public enum SpellClickCastFlags
    {
        CasterClicker = 0x01,
        TargetClicker = 0x02,
        OrigCasterOwner = 0x04
    }


    public enum SummonSlot
    {
        Pet = 0,
        Totem = 1,
        MiniPet = 5,
        Quest = 6,
    }

    public enum BaseModType
    {
        FlatMod,
        PCTmod,
        End
    }

    //To all Immune system, if target has immunes,
    //some spell that related to ImmuneToDispel or ImmuneToSchool or ImmuneToDamage type can't cast to it,
    //some spell_effects that related to ImmuneToEffect<effect>(only this effect in the spell) can't cast to it,
    //some aura(related to Mechanics or ImmuneToState<aura>) can't apply to it.
    public enum SpellImmunity
    {
        Effect = 0,                     // enum SpellEffects
        State = 1,                     // enum AuraType
        School = 2,                     // enum SpellSchoolMask
        Damage = 3,                     // enum SpellSchoolMask
        Dispel = 4,                     // enum DispelType
        Mechanic = 5,                     // enum Mechanics
        Id = 6,
        Max = 7
    }


    public enum BaseModGroup
    {
        CritPercentage,
        RangedCritPercentage,
        OffhandCritPercentage,
        ShieldBlockValue,
        End
    }

    public enum DamageEffectType
    {
        Direct = 0,                            // used for normal weapon damage (not for class abilities or spells)
        SpellDirect = 1,                            // spell/class abilities damage
        DOT = 2,
        Heal = 3,
        NoDamage = 4,                            // used also in case when damage applied to health but not applied to spell channelInterruptFlags/etc
        Self = 5
    }
    public enum WeaponDamageRange
    {
        MinDamage,
        MaxDamage
    }
    public enum UnitMods
    {
        StatStrength, // STAT_STRENGTH..UNIT_MOD_STAT_INTELLECT must be in existed order, it's accessed by index values of Stats enum.
        StatAgility,
        StatStamina,
        StatIntellect,
        Health,
        Mana, // UNIT_MOD_MANA..UNIT_MOD_PAIN must be listed in existing order, it is accessed by index values of Powers enum.
        Rage,
        Focus,
        Energy,
        ComboPoints,
        Runes,
        RunicPower,
        SoulShards,
        LunarPower,
        HolyPower,
        Alternate,
        Maelstrom,
        Chi,
        Insanity,
        BurningEmbers,
        DemonicFury,
        ArcaneCharges,
        Fury,
        Pain,
        Armor, // ARMOR..RESISTANCE_ARCANE must be in existed order, it's accessed by index values of SpellSchools enum.
        ResistanceHoly,
        ResistanceFire,
        ResistanceNature,
        ResistanceFrost,
        ResistanceShadow,
        ResistanceArcane,
        AttackPower,
        AttackPowerRanged,
        DamageMainHand,
        DamageOffHand,
        DamageRanged,
        End,
        // synonyms
        StatStart = StatStrength,
        StatEnd = StatIntellect + 1,
        ResistanceStart = Armor,
        ResistanceEnd = ResistanceArcane + 1,
        PowerStart = Mana,
        PowerEnd = Pain + 1
    }
    public enum UnitModifierType
    {
        BaseValue = 0,
        BasePCTExcludeCreate = 1, // percent modifier affecting all stat values from auras and gear but not player base for level
        BasePCT = 2,
        TotalValue = 3,
        TotalPCT = 4,
        End = 5
    }
    public enum VictimState
    {
        Intact = 0, // set when attacker misses
        Hit = 1, // victim got clear/blocked hit
        Dodge = 2,
        Parry = 3,
        Imterrupt = 4,
        Blocks = 5, // unused? not set when blocked, even on full block
        Evades = 6,
        Immune = 7,
        Deflects = 8
    }

    [Flags]
    public enum HitInfo
    {
        NormalSwing = 0x0,
        Unk1 = 0x01,               // req correct packet structure
        AffectsVictim = 0x02,
        OffHand = 0x04,
        Unk2 = 0x08,
        Miss = 0x10,
        FullAbsorb = 0x20,
        PartialAbsorb = 0x40,
        FullResist = 0x80,
        PartialResist = 0x100,
        CriticalHit = 0x200,               // critical hit
        Unk10 = 0x400,
        Unk11 = 0x800,
        Unk12 = 0x1000,
        Block = 0x2000,               // blocked damage
        Unk14 = 0x4000,               // set only if meleespellid is present//  no world text when victim is hit for 0 dmg(HideWorldTextForNoDamage?)
        Unk15 = 0x8000,               // player victim?// something related to blod sprut visual (BloodSpurtInBack?)
        Glancing = 0x10000,
        Crushing = 0x20000,
        NoAnimation = 0x40000,
        Unk19 = 0x80000,
        Unk20 = 0x100000,
        SwingNoHitSound = 0x200000,               // unused?
        Unk22 = 0x00400000,
        RageGain = 0x800000,
        FakeDamage = 0x1000000                // enables damage animation even if no damage done, set only if no damage
    }

    public enum ReactStates
    {
        Passive = 0,
        Defensive = 1,
        Aggressive = 2,
        Assist = 3
    }

    /// <summary>
    /// UnitStandStateType
    /// </summary>
    public enum UnitStandStateType
    {
        Stand = 0,
        Sit = 1,
        SitChair = 2,
        Sleep = 3,
        SitLowChair = 4,
        SitMediumChair = 5,
        SitHighChair = 6,
        Dead = 7,
        Kneel = 8,
        Submerged = 9
    }

    public struct UnitBytes0Offsets
    {
        public const byte Race = 0;
        public const byte Class = 1;
        public const byte PlayerClass = 2;
        public const byte Gender = 3;
    }

    public struct UnitBytes1Offsets
    {
        public const byte StandState = 0;
        public const byte PetTalents = 1;    // unused
        public const byte VisFlag = 2;
        public const byte AnimTier = 3;
    }

    public enum UnitStandFlags
    {
        Unk1 = 0x01,
        Creep = 0x02,
        Untrackable = 0x04,
        Unk4 = 0x08,
        Unk5 = 0x10,
        All = 0xFF
    }

    public enum UnitBytes1Flags
    {
        AlwaysStand = 0x01,
        Hover = 0x02,
        Unk3 = 0x04,
        All = 0xFF
    }

    public struct UnitBytes2Offsets
    {
        public const byte SheathState = 0;
        public const byte PvpFlag = 1;
        public const byte PetFlags = 2;
        public const byte ShapeshiftForm = 3;
    }

    public enum UnitBytes2Flags
    {
        PvP = 0x01,
        Unk1 = 0x02,
        FFAPvp = 0x04,
        Sanctuary = 0x08,
        Unk4 = 0x10,
        Unk5 = 0x20,
        Unk6 = 0x40,
        Unk7 = 0x80,
    }

    public enum UnitPetFlags
    { 
        CanBeRenamed = 0x01,
        CanBeAbandoned = 0x02
    }

    /// <summary>
    /// UnitFields.Bytes2, 3
    /// </summary>
    public enum ShapeShiftForm
    {
        None = 0,
        CatForm = 1,
        TreeOfLife = 2,
        TravelForm = 3,
        AquaticForm = 4,
        BearForm = 5,
        Ambient = 6,
        Ghoul = 7,
        DireBearForm = 8,
        CraneStance = 9,
        TharonjaSkeleton = 10,
        DarkmoonTestOfStrength = 11,
        BlbPlayer = 12,
        ShadowDance = 13,
        CreatureBear = 14,
        CreatureCat = 15,
        GhostWolf = 16,
        BattleStance = 17,
        DefensiveStance = 18,
        BerserkerStance = 19,
        SerpentStance = 20,
        Zombie = 21,
        Metamorphosis = 22,
        OxStance = 23,
        TigerStance = 24,
        Undead = 25,
        Frenzy = 26,
        FlightFormEpic = 27,
        Shadowform = 28,
        FlightForm = 29,
        Stealth = 30,
        MoonkinForm = 31,
        SpiritOfRedemption = 32,
        GladiatorStance = 33
    }

    public enum ReactiveType
    {
        Defense = 0,
        HunterParry = 1,
        OverPower = 2,
        Max = 3
    }

    public enum SpellValueMod
    {
        BasePoint0,
        BasePoint1,
        BasePoint2,
        BasePoint3,
        BasePoint4,
        BasePoint5,
        BasePoint6,
        BasePoint7,
        BasePoint8,
        BasePoint9,
        BasePoint10,
        BasePoint11,
        BasePoint12,
        BasePoint13,
        BasePoint14,
        BasePoint15,
        BasePoint16,
        BasePoint17,
        BasePoint18,
        BasePoint19,
        BasePoint20,
        BasePoint21,
        BasePoint22,
        BasePoint23,
        BasePoint24,
        BasePoint25,
        BasePoint26,
        BasePoint27,
        BasePoint28,
        BasePoint29,
        BasePoint30,
        BasePoint31,
        End,
        RadiusMod,
        MaxTargets,
        AuraStack
    }

    public enum CombatRating
    {
        Amplify = 0,
        DefenseSkill = 1,
        Dodge = 2,
        Parry = 3,
        Block = 4,
        HitMelee = 5,
        HitRanged = 6,
        HitSpell = 7,
        CritMelee = 8,
        CritRanged = 9,
        CritSpell = 10,
        Multistrike = 11,
        Readiness = 12,
        Speed = 13,
        ResilienceCritTaken = 14,
        ResiliencePlayerDamage = 15,
        Lifesteal = 16,
        HasteMelee = 17,
        HasteRanged = 18,
        HasteSpell = 19,
        Avoidance = 20,
        Studiness = 21,
        Unused7 = 22,
        Expertise = 23,
        ArmorPenetration = 24,
        Mastery = 25,
        PvpPower = 26,
        Cleave = 27,
        VersatilityDamageDone = 28,
        VersatilityHealingDone = 29,
        VersatilityDamageTaken = 30,
        Unused12 = 31,
        Max = 32
    }

    public enum DeathState
    {
        Alive = 0,
        JustDied = 1,
        Corpse = 2,
        Dead = 3,
        JustRespawned = 4
    }

    [Flags]
    public enum UnitState : uint
    {
        Died = 0x01,                     // Player Has Fake Death Aura
        MeleeAttacking = 0x02,                     // Player Is Melee Attacking Someone
        //Melee_Attack_By = 0x04,                     // Player Is Melee Attack By Someone
        Stunned = 0x08,
        Roaming = 0x10,
        Chase = 0x20,
        //Searching       = 0x40,
        Fleeing = 0x80,
        InFlight = 0x100,                     // Player Is In Flight Mode
        Follow = 0x200,
        Root = 0x400,
        Confused = 0x800,
        Distracted = 0x1000,
        Isolated = 0x2000,                     // Area Auras Do Not Affect Other Players
        AttackPlayer = 0x4000,
        Casting = 0x8000,
        Possessed = 0x10000,
        Charging = 0x20000,
        Jumping = 0x40000,
        //Onvehicle = 0x80000,
        Move = 0x100000,
        Rotating = 0x200000,
        Evade = 0x400000,
        RoamingMove = 0x800000,
        ConfusedMove = 0x1000000,
        FleeingMove = 0x2000000,
        ChaseMove = 0x4000000,
        FollowMove = 0x8000000,
        IgnorePathfinding = 0x10000000,
        AllStateSupported = Died | MeleeAttacking | Stunned | Roaming | Chase
                            | Fleeing | InFlight | Follow | Root | Confused
                            | Distracted | Isolated | AttackPlayer | Casting
                            | Possessed | Charging | Jumping | Move | Rotating
                            | Evade | RoamingMove | ConfusedMove | FleeingMove
                            | ChaseMove | FollowMove | IgnorePathfinding,
        Unattackable = InFlight,
        // For Real Move Using Movegen Check And Stop (Except Unstoppable Flight)
        Moving = RoamingMove | ConfusedMove | FleeingMove | ChaseMove | FollowMove,
        Controlled = (Confused | Stunned | Fleeing),
        LostControl = (Controlled | Jumping | Charging),
        Sightless = (LostControl | Evade),
        CannotAutoattack = (LostControl | Casting),
        CannotTurn = (LostControl | Rotating),
        // Stay By Different Reasons
        NotMove = Root | Stunned | Died | Distracted,
        AllState = 0xffffffff                      //(Stopped | Moving | In_Combat | In_Flight)
    }

    public enum UnitMoveType
    {
        Walk = 0,
        Run = 1,
        RunBack = 2,
        Swim = 3,
        SwimBack = 4,
        TurnRate = 5,
        Flight = 6,
        FlightBack = 7,
        PitchRate = 8,
        Max = 9
    }
    public enum WeaponAttackType
    {
        BaseAttack = 0,
        OffAttack = 1,
        RangedAttack = 2,
        Max
    }

    public enum UnitTypeMask
    {
        None = 0x0,
        Summon = 0x01,
        Minion = 0x02,
        Guardian = 0x04,
        Totem = 0x08,
        Pet = 0x10,
        Vehicle = 0x20,
        Puppet = 0x40,
        HunterPet = 0x80,
        ControlableGuardian = 0x100,
        Accessory = 0x200
    }

    public enum CurrentSpellTypes
    {
        Melee = 0,
        Generic = 1,
        Channeled = 2,
        AutoRepeat = 3,
        Max = 4
    }
    public enum SheathState
    {
        Unarmed = 0,                            // non prepared weapon
        Melee = 1,                              // prepared melee weapon
        Ranged = 2,                             // prepared ranged weapon
        Max = 3
    }
    public enum MeleeHitOutcome
    {
        Evade,
        Miss,
        Dodge,
        Block,
        Parry,
        Glancing,
        Crit,
        Crushing,
        Normal
    }

    public enum UnitDynFlags
    {
        None                    = 0x00,
        HideModel               = 0x02,     // Object model is not shown with this flag
        Lootable                = 0x04,
        TrackUnit               = 0x08,
        Tapped                  = 0x10,     // Lua_UnitIsTapped
        SpecialInfo             = 0x20,
        Dead                    = 0x40,
        ReferAFriend            = 0x80
    }
}
