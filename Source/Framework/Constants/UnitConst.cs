// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        PassengerMirrorsAnims = 0x10000,           // Passenger forced to repeat all vehicle animations
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
        Any = -1,
        Pet = 0,
        Totem = 1,
        Totem2 = 2,
        Totem3 = 3,
        Totem4 = 4,
        MiniPet = 5,
        Quest = 6,

        Max
    }

    public enum BaseModType
    {
        FlatMod,
        PctMod,
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
        Other = 7,  //enum SpellOtherImmunity
        Max
    }


    public enum BaseModGroup
    {
        CritPercentage,
        RangedCritPercentage,
        OffhandCritPercentage,
        ShieldBlockValue,
        End
    }

    public enum AdvFlyingRateTypeSingle
    {
        AirFriction = 0,
        MaxVel = 1,
        LiftCoefficient = 2,
        DoubleJumpVelMod = 3,
        GlideStartMinHeight = 4,
        AddImpulseMaxSpeed = 5,
        SurfaceFriction = 14,
        OverMaxDeceleration = 15,
        LaunchSpeedCoefficient = 16,
        Max = 17
    }

    public enum AdvFlyingRateTypeRange
    {
        BankingRate = 6,
        PitchingRateDown = 8,
        PitchingRateUp = 10,
        TurnVelocityThreshold = 12
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
        Essence,
        RuneBlood,
        RuneFrost,
        RuneUnholy,
        AlternateQuest,
        AlternateEncounter,
        AlternateMount,
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
        PowerEnd = AlternateMount + 1
    }

    public enum UnitModifierFlatType
    {
        Base = 0,
        BasePCTExcludeCreate = 1, // percent modifier affecting all stat values from auras and gear but not player base for level
        Total = 2,
        End = 3
    }

    public enum UnitModifierPctType
    {
        Base = 0,
        Total = 1,
        End = 2
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
    public enum UnitStandStateType : byte
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
        Submerged = 9,

        Max
    }

    public enum UnitVisFlags
    {
        Invisible = 0x01,
        Stealthed = 0x02,
        Untrackable = 0x04,
        Unk4 = 0x08,
        Unk5 = 0x10,
        All = 0xFF
    }

    public enum AnimTier
    {
        Ground = 0, // plays ground tier animations
        Swim = 1, // falls back to ground tier animations, not handled by the client, should never appear in sniffs, will prevent tier change animations from playing correctly if used
        Hover = 2, // plays flying tier animations or falls back to ground tier animations, automatically enables hover clientside when entering visibility with this value
        Fly = 3, // plays flying tier animations
        Submerged = 4,

        Max
    }

    public enum UnitPVPStateFlags
    {
        None = 0x00,
        PvP = 0x01,
        Unk1 = 0x02,
        FFAPvp = 0x04,
        Sanctuary = 0x08,
        Unk4 = 0x10,
        Unk5 = 0x20,
        Unk6 = 0x40,
        Unk7 = 0x80,
    }

    public enum UnitPetFlags : byte
    {
        None = 0x00,
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
        GladiatorStance = 33,
        Metamorphosis2 = 34,
        MoonkinRestoration = 35,
        TreantForm = 36,
        SpiritOwlForm = 37,
        SpiritOwl2 = 38,
        WispForm = 39,
        Wisp2 = 40,
        Soulshape = 41,
        ForgeborneReveries = 42
    }

    public enum ReactiveType
    {
        Defense = 0,
        Defense2 = 1,
        Max = 2
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
        BasePointEnd,

        MaxTargets = BasePointEnd,
        AuraStack,
        Duration,
        ParentSpellTargetCount,
        ParentSpellTargetIndex,
        IntEnd,
    }

    public enum SpellValueModFloat
    {
        RadiusMod = SpellValueMod.IntEnd,
        CritChance,
        DurationPct,
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
        Corruption = 11,
        CorruptionResistance = 12,
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
        Died = 0x01, // Player Has Fake Death Aura
        MeleeAttacking = 0x02, // Player Is Melee Attacking Someone
        Charmed = 0x04, // having any kind of charm aura on self
        Stunned = 0x08,
        Roaming = 0x10,
        Chase = 0x20,
        Focusing = 0x40,
        Fleeing = 0x80,
        InFlight = 0x100, // Player Is In Flight Mode
        Follow = 0x200,
        Root = 0x400,
        Confused = 0x800,
        Distracted = 0x1000,
        Isolated_Deprecated = 0x2000, // REUSE
        AttackPlayer = 0x4000,
        Casting = 0x8000,
        Possessed = 0x10000, // being possessed by another unit
        Charging = 0x20000,
        Jumping = 0x40000,
        FollowFormation = 0x80000,
        Move = 0x100000,
        Rotating = 0x200000,
        Evade = 0x400000,
        RoamingMove = 0x800000,
        ConfusedMove = 0x1000000,
        FleeingMove = 0x2000000,
        ChaseMove = 0x4000000,
        FollowMove = 0x8000000,
        IgnorePathfinding = 0x10000000,
        FollowFormationMove = 0x20000000,

        AllStateSupported = Died | MeleeAttacking | Charmed | Stunned | Roaming | Chase
                            | Focusing | Fleeing | InFlight | Follow | Root | Confused
                            | Distracted | AttackPlayer | Casting
                            | Possessed | Charging | Jumping | Move | Rotating
                            | Evade | RoamingMove | ConfusedMove | FleeingMove
                            | ChaseMove | FollowMove | IgnorePathfinding | FollowFormationMove,

        Unattackable = InFlight,
        Moving = RoamingMove | ConfusedMove | FleeingMove | ChaseMove | FollowMove | FollowFormationMove,
        Controlled = Confused | Stunned | Fleeing,
        LostControl = Controlled | Possessed | Jumping | Charging,
        CannotAutoattack = Controlled | Charging,
        Sightless = LostControl | Evade,
        CannotTurn = LostControl | Rotating | Focusing,
        NotMove = Root | Stunned | Died | Distracted,

        AllErasable = AllStateSupported & ~IgnorePathfinding,
        AllState = 0xffffffff
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
        CanSkin                 = 0x40,     // previously UNIT_DYNFLAG_DEAD
        ReferAFriend            = 0x80
    }
}
