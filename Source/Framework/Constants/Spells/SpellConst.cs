﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
    public struct SpellConst
    {
        public const int MaxEffects = 32;
        public const uint MaxEffectMask = 0xFFFFFFFF;
        public const int MaxReagents = 8;
        public const int MaxTotems = 2;
        public const int MaxShapeshift = 8;

        public const int MaxAuras = 255;

        public const int EffectFirstFound = 254;
        public const int EffectAll = 255;

        public const float TrajectoryMissileSize = 3.0f;

        public const int MaxPowersPerSpell = 4;

        public const uint VisualKitFood = 406;
        public const uint VisualKitDrink = 438;
    }


    // only used in code
    public enum SpellCategories
    {
        HealthManaPotions = 4,
        DevourMagic = 12,
        Judgement = 1210,               // Judgement (seal trigger)
        Food = 11,
        Drink = 59
    }

    public enum SpellLinkedType
    {
        Cast = 0,            // +: cast; -: remove
        Hit = 1 * 200000,
        Aura = 2 * 200000,   // +: aura; -: immune
        Remove = 0
    }

    //Spell targets used by SelectSpell
    public enum SelectTargetType
    {
        DontCare = 0,                             //All target types allowed

        Self,                                     //Only Self casting

        SingleEnemy,                             //Only Single Enemy
        AoeEnemy,                                //Only AoE Enemy
        AnyEnemy,                                //AoE or Single Enemy

        SingleFriend,                            //Only Single Friend
        AoeFriend,                               //Only AoE Friend
        AnyFriend                                //AoE or Single Friend
    }

    //Spell Effects used by SelectSpell
    public enum SelectEffect
    {
        DontCare = 0,                             //All spell effects allowed
        Damage,                                   //Spell does damage
        Healing,                                  //Spell does healing
        Aura                                      //Spell applies an aura
    }

    public enum SpellCastSource
    {
        Player = 2,
        Normal = 3,
        Item = 4,
        Passive = 7,
        Pet = 9,
        Aura = 13,
        Spell = 16,
    }

    public enum SpellRangeFlag : byte
    {
        Default = 0,
        Melee = 1,     //melee
        Ranged = 2      //hunter range and ranged weapon
    }

    [Flags]
    public enum SpellInterruptFlags
    {
        None = 0,
        Movement = 0x01,
        DamagePushbackPlayerOnly = 0x02,
        Stun = 0x04, // useless, even spells without it get interrupted
        Combat = 0x08,
        DamageCancelsPlayerOnly = 0x10,
        MeleeCombat = 0x20, // NYI
        Immunity = 0x40, // NYI
        DamageAbsorb = 0x80,
        ZeroDamageCancels = 0x100,
        DamagePushback = 0x200,
        DamageCancels = 0x400
    }

    [Flags]
    public enum SpellAuraInterruptFlags : uint
    {
        None = 0,
        HostileActionReceived = 0x01,
        Damage = 0x02,
        Action = 0x04,
        Moving = 0x08,
        Turning = 0x10,
        Anim = 0x20,
        Dismount = 0x40,
        UnderWater = 0x80, // TODO: disallow casting when swimming (SPELL_FAILED_ONLY_ABOVEWATER)
        AboveWater = 0x100, // TODO: disallow casting when not swimming (SPELL_FAILED_ONLY_UNDERWATER)
        Sheathing = 0x200,
        Interacting = 0x400, // TODO: more than gossip, replace all the feign death removals by aura type
        Looting = 0x800,
        Attacking = 0x1000,
        ItemUse = 0x2000,
        DamageChannelDuration = 0x4000,
        Shapeshifting = 0x8000,
        ActionDelayed = 0x10000,
        Mount = 0x20000,
        Standing = 0x40000,
        LeaveWorld = 0x80000,
        StealthOrInvis = 0x100000,
        InvulnerabilityBuff = 0x200000,
        EnterWorld = 0x400000,
        PvPActive = 0x800000,
        NonPeriodicDamage = 0x1000000,
        LandingOrFlight = 0x2000000,
        Release = 0x4000000,
        DamageCancelsScript = 0x8000000, // NYI dedicated aura script hook
        EnteringCombat = 0x10000000,
        Login = 0x20000000,
        Summon = 0x40000000,
        LeavingCombat = 0x80000000,

        NotVictim = (HostileActionReceived | Damage | NonPeriodicDamage)
    }

    [Flags]
    public enum SpellAuraInterruptFlags2
    {
        None = 0,
        Falling = 0x01, // NYI
        Swimming = 0x02,
        NotMoving = 0x04, // NYI
        Ground = 0x08,
        Transform = 0x10, // NYI
        Jump = 0x20,
        ChangeSpec = 0x40, // NYI
        AbandonVehicle = 0x80, // NYI
        StartOfEncounter = 0x100, // NYI
        EndOfEncounter = 0x200, // NYI
        Disconnect = 0x400, // NYI
        EnteringInstance = 0x800, // NYI
        DuelEnd = 0x1000, // NYI
        LeaveArenaOrBattleground = 0x2000, // NYI
        ChangeTalent = 0x4000, // NYI
        ChangeGlyph = 0x8000, // NYI
        SeamlessTransfer = 0x10000, // NYI
        WarModeLeave = 0x20000, // NYI
        TouchingGround = 0x40000, // NYI
        ChromieTime = 0x80000, // NYI
        SplineFlightOrFreeFlight = 0x100000, // NYI
        ProcOrPeriodicAttacking = 0x200000  // NYI
    }

    // Enum with EffectRadiusIndex and their actual radius
    public enum EffectRadiusIndex
    {
        Yards2 = 7,
        Yards5 = 8,
        Yards20 = 9,
        Yards30 = 10,
        Yards45 = 11,
        Yards100 = 12,
        Yards10 = 13,
        Yards8 = 14,
        Yards3 = 15,
        Yards1 = 16,
        Yards13 = 17,
        Yards15 = 18,
        Yards18 = 19,
        Yards25 = 20,
        Yards35 = 21,
        Yards200 = 22,
        Yards40 = 23,
        Yards65 = 24,
        Yards70 = 25,
        Yards4 = 26,
        Yards50 = 27,
        Yards50000 = 28,
        Yards6 = 29,
        Yards500 = 30,
        Yards80 = 31,
        Yards12 = 32,
        Yards99 = 33,
        Yards55 = 35,
        Yards0 = 36,
        Yards7 = 37,
        Yards21 = 38,
        Yards34 = 39,
        Yards9 = 40,
        Yards150 = 41,
        Yards11 = 42,
        Yards16 = 43,
        Yards0_5 = 44,   // 0.5 Yards
        Yards10_2 = 45,
        Yards5_2 = 46,
        Yards15_2 = 47,
        Yards60 = 48,
        Yards90 = 49,
        Yards15_3 = 50,
        Yards60_2 = 51,
        Yards5_3 = 52,
        Yards60_3 = 53,
        Yards50000_2 = 54,
        Yards130 = 55,
        Yards38 = 56,
        Yards45_2 = 57,
        Yards32 = 59,
        Yards44 = 60,
        Yards14 = 61,
        Yards47 = 62,
        Yards23 = 63,
        Yards3_5 = 64,   // 3.5 Yards
        Yards80_2 = 65
    }


    public enum SpellEffectImplicitTargetTypes
    {
        None = 0,
        Explicit,
        Caster
    }

    // Spell dispel type
    public enum DispelType
    {
        None = 0,
        Magic = 1,
        Curse = 2,
        Disease = 3,
        Poison = 4,
        Stealth = 5,
        Invisibility = 6,
        ALL = 7,
        SpeNPCOnly = 8,
        Enrage = 9,
        ZGTicket = 10,
        OldUnused = 11,

        AllMask = ((1 << Magic) | (1 << Curse) | (1 << Disease) | (1 << Poison))
    }

    // Spell clasification
    public enum SpellSpecificType
    {
        Normal = 0,
        Seal = 1,
        Aura = 3,
        Sting = 4,
        Curse = 5,
        Aspect = 6,
        Tracker = 7,
        WarlockArmor = 8,
        MageArmor = 9,
        ElementalShield = 10,
        MagePolymorph = 11,
        Judgement = 13,
        WarlockCorruption = 17,
        Food = 19,
        Drink = 20,
        FoodAndDrink = 21,
        Presence = 22,
        Charm = 23,
        Scroll = 24,
        MageArcaneBrillance = 25,
        WarriorEnrage = 26,
        PriestDivineSpirit = 27,
        Hand = 28,
        Phase = 29,
        Bane = 30
    }

    // Spell mechanics
    public enum Mechanics
    {
        None = 0,
        Charm = 1,
        Disoriented = 2,
        Disarm = 3,
        Distract = 4,
        Fear = 5,
        Grip = 6,
        Root = 7,
        SlowAttack = 8,
        Silence = 9,
        Sleep = 10,
        Snare = 11,
        Stun = 12,
        Freeze = 13,
        Knockout = 14,
        Bleed = 15,
        Bandage = 16,
        Polymorph = 17,
        Banish = 18,
        Shield = 19,
        Shackle = 20,
        Mount = 21,
        Infected = 22,
        Turn = 23,
        Horror = 24,
        Invulnerability = 25,
        Interrupt = 26,
        Daze = 27,
        Discovery = 28,
        ImmuneShield = 29,                         // Divine (Blessing) Shield/Protection And Ice Block
        Sapped = 30,
        Enraged = 31,
        Wounded = 32,
        Max = 33,

        ImmuneToMovementImpairmentAndLossControlMask = ((1 << Charm) | (1 << Disoriented) |
            (1 << Fear) | (1 << Root) | (1 << Sleep) | (1 << Snare) | (1 << Stun) |
            (1 << Freeze) | (1 << Silence) | (1 << Disarm) | (1 << Knockout) |
            (1 << Polymorph) | (1 << Banish) | (1 << Shackle) |
            (1 << Turn) | (1 << Horror) | (1 << Daze) | (1 << Sapped))
    }

    public enum SpellModOp
    {
        HealingAndDamage = 0,
        Duration = 1,
        Hate = 2,
        PointsIndex0 = 3,
        ProcCharges = 4,
        Range = 5,
        Radius = 6,
        CritChance = 7,
        Points = 8,
        ResistPushback = 9,
        ChangeCastTime = 10,
        Cooldown = 11,
        PointsIndex1 = 12,
        TargetResistance = 13,
        PowerCost0 = 14, // Used when SpellPowerEntry::PowerIndex == 0
        CritDamageAndHealing = 15,
        HitChance = 16,
        ChainTargets = 17,
        ProcChance = 18,
        Period = 19,
        ChainAmplitude = 20,
        StartCooldown = 21,
        PeriodicHealingAndDamage = 22,
        PointsIndex2 = 23,
        BonusCoefficient = 24,
        TriggerDamage = 25, // NYI
        ProcFrequency = 26,
        Amplitude = 27,
        DispelResistance = 28,
        CrowdDamage = 29, // NYI
        PowerCostOnMiss = 30,
        Doses = 31,
        PointsIndex3 = 32,
        PointsIndex4 = 33,
        PowerCost1 = 34, // Used when SpellPowerEntry::PowerIndex == 1
        ChainJumpDistance = 35,
        AreaTriggerMaxSummons = 36, // NYI
        MaxAuraStacks = 37,
        ProcCooldown = 38,
        PowerCost2 = 39, // Used when SpellPowerEntry::PowerIndex == 2

        Max = 40
    }

    public enum SpellModType
    {
        Flat = 0,                            // SPELL_AURA_ADD_FLAT_MODIFIER
        Pct = 1,                             // SPELL_AURA_ADD_PCT_MODIFIER
        LabelFlat = 2,                            // SPELL_AURA_ADD_FLAT_MODIFIER_BY_SPELL_LABEL
        LabelPct = 3,                            // SPELL_AURA_ADD_PCT_MODIFIER_BY_SPELL_LABEL
        End
    }

    public enum SpellGroup
    {
        None = 0,
        ElixirBattle = 1,
        ElixirGuardian = 2,
        ElixirUnstable = 3,
        ElixirShattrath = 4,
        CoreRangeMax = 5
    }
    public enum SpellGroupStackRule
    {
        Default,
        Exclusive,
        ExclusiveFromSameCaster,
        ExclusiveSameEffect,
        ExclusiveHighest,
        Max
    }

    public enum PlayerSpellState
    {
        Unchanged = 0,
        Changed = 1,
        New = 2,
        Removed = 3,
        Temporary = 4
    }

    public enum SpellState
    {
        None = 0,
        Preparing = 1,
        Casting = 2,
        Finished = 3,
        Idle = 4,
        Delayed = 5
    }

    public enum SpellSchools
    {
        Normal = 0,
        Holy = 1,
        Fire = 2,
        Nature = 3,
        Frost = 4,
        Shadow = 5,
        Arcane = 6,
        Max = 7
    }

    public enum SpellCastResult
    {
        Success = 0,
        AffectingCombat = 1,
        AlreadyAtFullHealth = 2,
        AlreadyAtFullMana = 3,
        AlreadyAtFullPower = 4,
        AlreadyBeingTamed = 5,
        AlreadyHaveCharm = 6,
        AlreadyHaveSummon = 7,
        AlreadyHavePet = 8,
        AlreadyOpen = 9,
        AuraBounced = 10,
        AutotrackInterrupted = 11,
        BadImplicitTargets = 12,
        BadTargets = 13,
        PvpTargetWhileUnflagged = 14,
        CantBeCharmed = 15,
        CantBeDisenchanted = 16,
        CantBeDisenchantedSkill = 17,
        CantBeEnchanted = 18,
        CantBeMilled = 19,
        CantBeProspected = 20,
        CantCastOnTapped = 21,
        CantDuelWhileInvisible = 22,
        CantDuelWhileStealthed = 23,
        CantStealth = 24,
        CantUntalent = 25,
        CasterAurastate = 26,
        CasterDead = 27,
        Charmed = 28,
        ChestInUse = 29,
        Confused = 30,
        DisabledByPowerScaling = 31,
        DontReport = 32,
        EquippedItem = 33,
        EquippedItemClass = 34,
        EquippedItemClassMainhand = 35,
        EquippedItemClassOffhand = 36,
        Error = 37,
        Falling = 38,
        Fizzle = 39,
        Fleeing = 40,
        FoodLowlevel = 41,
        GarrisonNotOwned = 42,
        GarrisonOwned = 43,
        GarrisonMaxLevel = 44,
        GarrisonNotUpgradeable = 45,
        GarrisonFollowerOnMission = 46,
        GarrisonFollowerInBuilding = 47,
        GarrisonFollowerMaxLevel = 48,
        GarrisonFollowerMinItemLevel = 49,
        GarrisonFollowerMaxItemLevel = 50,
        GarrisonFollowerMaxQuality = 51,
        GarrisonFollowerNotMaxLevel = 52,
        GarrisonFollowerHasAbility = 53,
        GarrisonFollowerHasSingleMissionAbility = 54,
        GarrisonFollowerRequiresEpic = 55,
        GarrisonMissionNotInProgress = 56,
        GarrisonMissionComplete = 57,
        GarrisonNoMissionsAvailable = 58,
        Highlevel = 59,
        HungerSatiated = 60,
        Immune = 61,
        IncorrectArea = 62,
        Interrupted = 63,
        InterruptedCombat = 64,
        ItemAlreadyEnchanted = 65,
        ItemGone = 66,
        ItemNotFound = 67,
        ItemNotReady = 68,
        LevelRequirement = 69,
        LineOfSight = 70,
        Lowlevel = 71,
        LowCastlevel = 72,
        MainhandEmpty = 73,
        Moving = 74,
        NeedAmmo = 75,
        NeedAmmoPouch = 76,
        NeedExoticAmmo = 77,
        NeedMoreItems = 78,
        NoPath = 79,
        NotBehind = 80,
        NotFishable = 81,
        NotFlying = 82,
        NotHere = 83,
        NotInfront = 84,
        NotInControl = 85,
        NotKnown = 86,
        NotMounted = 87,
        NotOnTaxi = 88,
        NotOnTransport = 89,
        NotReady = 90,
        NotShapeshift = 91,
        NotStanding = 92,
        NotTradeable = 93,
        NotTrading = 94,
        NotUnsheathed = 95,
        NotWhileGhost = 96,
        NotWhileLooting = 97,
        NoAmmo = 98,
        NoChargesRemain = 99,
        NoComboPoints = 100,
        NoDueling = 101,
        NoEndurance = 102,
        NoFish = 103,
        NoItemsWhileShapeshifted = 104,
        NoMountsAllowed = 105,
        NoPet = 106,
        NoPower = 107,
        NothingToDispel = 108,
        NothingToSteal = 109,
        OnlyAbovewater = 110,
        OnlyIndoors = 111,
        OnlyMounted = 112,
        OnlyOutdoors = 113,
        OnlyShapeshift = 114,
        OnlyStealthed = 115,
        OnlyUnderwater = 116,
        OutOfRange = 117,
        Pacified = 118,
        Possessed = 119,
        Reagents = 120,
        RequiresArea = 121,
        RequiresSpellFocus = 122,
        Rooted = 123,
        Silenced = 124,
        SpellInProgress = 125,
        SpellLearned = 126,
        SpellUnavailable = 127,
        Stunned = 128,
        TargetsDead = 129,
        TargetAffectingCombat = 130,
        TargetAurastate = 131,
        TargetDueling = 132,
        TargetEnemy = 133,
        TargetEnraged = 134,
        TargetFriendly = 135,
        TargetInCombat = 136,
        TargetInPetBattle = 137,
        TargetIsPlayer = 138,
        TargetIsPlayerControlled = 139,
        TargetNotDead = 140,
        TargetNotInParty = 141,
        TargetNotLooted = 142,
        TargetNotPlayer = 143,
        TargetNoPockets = 144,
        TargetNoWeapons = 145,
        TargetNoRangedWeapons = 146,
        TargetUnskinnable = 147,
        ThirstSatiated = 148,
        TooClose = 149,
        TooManyOfItem = 150,
        TotemCategory = 151,
        Totems = 152,
        TryAgain = 153,
        UnitNotBehind = 154,
        UnitNotInfront = 155,
        VisionObscured = 156,
        WrongPetFood = 157,
        NotWhileFatigued = 158,
        TargetNotInInstance = 159,
        NotWhileTrading = 160,
        TargetNotInRaid = 161,
        TargetFreeforall = 162,
        NoEdibleCorpses = 163,
        OnlyBattlegrounds = 164,
        TargetNotGhost = 165,
        TransformUnusable = 166,
        WrongWeather = 167,
        DamageImmune = 168,
        PreventedByMechanic = 169,
        PlayTime = 170,
        Reputation = 171,
        MinSkill = 172,
        NotInRatedBattleground = 173,
        NotOnShapeshift = 174,
        NotOnStealthed = 175,
        NotOnDamageImmune = 176,
        NotOnMounted = 177,
        TooShallow = 178,
        TargetNotInSanctuary = 179,
        TargetIsTrivial = 180,
        BmOrInvisgod = 181,
        GroundMountNotAllowed = 182,
        FloatingMountNotAllowed = 183,
        UnderwaterMountNotAllowed = 184,
        FlyingMountNotAllowed = 185,
        ApprenticeRidingRequirement = 186,
        JourneymanRidingRequirement = 187,
        ExpertRidingRequirement = 188,
        ArtisanRidingRequirement = 189,
        MasterRidingRequirement = 190,
        ColdRidingRequirement = 191,
        FlightMasterRidingRequirement = 192,
        CsRidingRequirement = 193,
        PandaRidingRequirement = 194,
        DraenorRidingRequirement = 195,
        BrokenIslesRidingRequirement = 196,
        MountNoFloatHere = 197,
        MountNoUnderwaterHere = 198,
        MountAboveWaterHere = 199,
        MountCollectedOnOtherChar = 200,
        NotIdle = 201,
        NotInactive = 202,
        PartialPlaytime = 203,
        NoPlaytime = 204,
        NotInBattleground = 205,
        NotInRaidInstance = 206,
        OnlyInArena = 207,
        TargetLockedToRaidInstance = 208,
        OnUseEnchant = 209,
        NotOnGround = 210,
        CustomError = 211,
        CantDoThatRightNow = 212,
        TooManySockets = 213,
        InvalidGlyph = 214,
        UniqueGlyph = 215,
        GlyphSocketLocked = 216,
        GlyphExclusiveCategory = 217,
        GlyphInvalidSpec = 218,
        GlyphNoSpec = 219,
        NoActiveGlyphs = 220,
        NoValidTargets = 221,
        ItemAtMaxCharges = 222,
        NotInBarbershop = 223,
        FishingTooLow = 224,
        ItemEnchantTradeWindow = 225,
        SummonPending = 226,
        MaxSockets = 227,
        PetCanRename = 228,
        TargetCannotBeResurrected = 229,
        TargetHasResurrectPending = 230,
        NoActions = 231,
        CurrencyWeightMismatch = 232,
        WeightNotEnough = 233,
        WeightTooMuch = 234,
        NoVacantSeat = 235,
        NoLiquid = 236,
        OnlyNotSwimming = 237,
        ByNotMoving = 238,
        InCombatResLimitReached = 239,
        NotInArena = 240,
        TargetNotGrounded = 241,
        ExceededWeeklyUsage = 242,
        NotInLfgDungeon = 243,
        BadTargetFilter = 244,
        NotEnoughTargets = 245,
        NoSpec = 246,
        CantAddBattlePet = 247,
        CantUpgradeBattlePet = 248,
        WrongBattlePetType = 249,
        NoDungeonEncounter = 250,
        NoTeleportFromDungeon = 251,
        MaxLevelTooLow = 252,
        CantReplaceItemBonus = 253,
        GrantPetLevelFail = 254,
        SkillLineNotKnown = 255,
        BlueprintKnown = 256,
        FollowerKnown = 257,
        CantOverrideEnchantVisual = 258,
        ItemNotAWeapon = 259,
        SameEnchantVisual = 260,
        ToyUseLimitReached = 261,
        ToyAlreadyKnown = 262,
        ShipmentsFull = 263,
        NoShipmentsForContainer = 264,
        NoBuildingForShipment = 265,
        NotEnoughShipmentsForContainer = 266,
        HasMission = 267,
        BuildingActivateNotReady = 268,
        NotSoulbound = 269,
        RidingVehicle = 270,
        VeteranTrialAboveSkillRankMax = 271,
        NotWhileMercenary = 272,
        SpecDisabled = 273,
        CantBeObliterated = 274,
        CantBeScrapped = 275,
        FollowerClassSpecCap = 276,
        TransportNotReady = 277,
        TransmogSetAlreadyKnown = 278,
        DisabledByAuraLabel = 279,
        DisabledByMaxUsableLevel = 280,
        SpellAlreadyKnown = 281,
        MustKnowSupercedingSpell = 282,
        YouCannotUseThatInPvpInstance = 283,
        NoArtifactEquipped = 284,
        WrongArtifactEquipped = 285,
        TargetIsUntargetableByAnyone = 286,
        SpellEffectFailed = 287,
        NeedAllPartyMembers = 288,
        ArtifactAtFullPower = 289,
        ApItemFromPreviousTier = 290,
        AreaTriggerCreation = 291,
        AzeriteEmpoweredOnly = 292,
        AzeriteEmpoweredNoChoicesToUndo = 293,
        WrongFaction = 294,
        NotEnoughCurrency = 295,
        BattleForAzerothRidingRequirement = 296,
        MountEquipmentError = 297,
        NotWhileLevelLinked = 298,
        LevelLinkedLowLevel = 299,
        SummonMapCondition = 300,
        SetCovenantError = 301,
        RuneforgeLegendaryUpgrade = 302,
        SetChromieTimeError = 303,
        IneligibleWeaponAppearance = 304,
        PlayerCondition = 305,
        NotWhileChromieTimed = 306,
        OptionalReagents = 307,
        SpectatorOrCommentator = 308,
        SoulbindConduitLearnFailedInvalidCovenant = 309,
        ShadowlandsRidingRequirement = 310,
        NotInMageTower = 311,
        GarrisonFollowerAtMinLevel = 312,
        Unknown = 313,

        // Ok Cast Value - Here In Case A Future Version Removes Success And We Need To Use A Custom Value (Not Sent To Client Either Way)
        SpellCastOk = Success
    }

    public enum SpellCustomErrors
    {
        None = 0,
        CustomMsg = 1,  // Something Bad Happened, And We Want To Display A Custom Message!
        AlexBrokeQuest = 2,  // Alex Broke Your Quest! Thank Him Later!
        NeedHelplessVillager = 3,  // This Spell May Only Be Used On Helpless Wintergarde Villagers That Have Not Been Rescued.
        NeedWarsongDisguise = 4,  // Requires That You Be Wearing The Warsong Orc Disguise.
        RequiresPlagueWagon = 5,  // You Must Be Closer To A Plague Wagon In Order To Drop Off Your 7th Legion Siege Engineer.
        CantTargetFriendlyNonparty = 6,  // You Cannot Target Friendly Units Outside Your Party.
        NeedChillNymph = 7,  // You Must Target A Weakened Chill Nymph.
        MustBeInEnkilah = 8,  // The Imbued Scourge Shroud Will Only Work When Equipped In The Temple City Of En'Kilah.
        RequiresCorpseDust = 9,  // Requires Corpse Dust
        CantSummonGargoyle = 10,  // You Cannot Summon Another Gargoyle Yet.
        NeedCorpseDustIfNoTarget = 11,  // Requires Corpse Dust If The Target Is Not Dead And Humanoid.
        MustBeAtShatterhorn = 12,  // Can Only Be Placed Near Shatterhorn
        MustTargetProtoDrakeEgg = 13,  // You Must First Select A Proto-Drake Egg.
        MustBeCloseToTree = 14,  // You Must Be Close To A Marked Tree.
        MustTargetTurkey = 15,  // You Must Target A Fjord Turkey.
        MustTargetHawk = 16,  // You Must Target A Fjord Hawk.
        TooFarFromBouy = 17,  // You Are Too Far From The Bouy.
        MustBeCloseToOilSlick = 18,  // Must Be Used Near An Oil Slick.
        MustBeCloseToBouy = 19,  // You Must Be Closer To The Buoy!
        WyrmrestVanquisher = 20,  // You May Only Call For The Aid Of A Wyrmrest Vanquisher In Wyrmrest Temple, The Dragon Wastes, Galakrond'S Rest Or The Wicked Coil.
        MustTargetIceHeartJormungar = 21,  // That Can Only Be Used On A Ice Heart Jormungar Spawn.
        MustBeCloseToSinkhole = 22,  // You Must Be Closer To A Sinkhole To Use Your Map.
        RequiresHaroldLane = 23,  // You May Only Call Down A Stampede On Harold Lane.
        RequiresGammothMagnataur = 24,  // You May Only Use The Pouch Of Crushed Bloodspore On Gammothra Or Other Magnataur In The Bloodspore Plains And Gammoth.
        MustBeInResurrectionChamber = 25,  // Requires The Magmawyrm Resurrection Chamber In The Back Of The Maw Of Neltharion.
        CantCallWintergardeHere = 26,  // You May Only Call Down A Wintergarde Gryphon In Wintergarde Keep Or The Carrion Fields.
        MustTargetWilhelm = 27,  // What Are You Doing? Only Aim That Thing At Wilhelm!
        NotEnoughHealth = 28,  // Not Enough Health!
        NoNearbyCorpses = 29,  // There Are No Nearby Corpses To Use.
        TooManyGhouls = 30,  // You'Ve Created Enough Ghouls. Return To Gothik The Harvester At Death'S Breach.
        GoFurtherFromSunderedShard = 31,  // Your Companion Does Not Want To Come Here.  Go Further From The Sundered Shard.
        MustBeInCatForm = 32,  // Must Be In Cat Form
        MustBeDeathKnight = 33,  // Only Death Knights May Enter Ebon Hold.
        MustBeInBearForm = 34,  // Must Be In Bear Form
        MustBeNearHelplessVillager = 35,  // You Must Be Within Range Of A Helpless Wintergarde Villager.
        CantTargetElementalMechanical = 36,  // You Cannot Target An Elemental Or Mechanical Corpse.
        MustHaveUsedDalaranCrystal = 37,  // This Teleport Crystal Cannot Be Used Until The Teleport Crystal In Dalaran Has Been Used At Least Once.
        YouAlreadyHoldSomething = 38,  // You Are Already Holding Something In Your Hand. You Must Throw The Creature In Your Hand Before Picking Up Another.
        YouDontHoldAnything = 39,  // You Don'T Have Anything To Throw! Find A Vargul And Use Gymer Grab To Pick One Up!
        MustBeCloseToValduran = 40,  // Bouldercrag'S War Horn Can Only Be Used Within 10 Yards Of Valduran The Stormborn.
        NoPassenger = 41,  // You Are Not Carrying A Passenger. There Is Nobody To Drop Off.
        CantBuildMoreVehicles = 42,  // You Cannot Build Any More Siege Vehicles.
        AlreadyCarryingCrusader = 43,  // You Are Already Carrying A Captured Argent Crusader. You Must Return To The Argent Vanguard Infirmary And Drop Off Your Passenger Before You May Pick Up Another.
        CantDoWhileRooted = 44,  // You Can'T Do That While Rooted.
        RequiresNearbyTarget = 45,  // Requires A Nearby Target.
        NothingToDiscover = 46,  // Nothing Left To Discover.
        NotEnoughTargets = 47,  // No Targets Close Enough To Bluff.
        ConstructTooFar = 48,  // Your Iron Rune Construct Is Out Of Range.
        RequiresGrandMasterEngineer = 49,  // Requires Engineering (350)
        CantUseThatMount = 50,  // You Can'T Use That Mount.
        NooneToEject = 51,  // There Is Nobody To Eject!
        TargetMustBeBound = 52,  // The Target Must Be Bound To You.
        TargetMustBeUndead = 53,  // Target Must Be Undead.
        TargetTooFar = 54,  // You Have No Target Or Your Target Is Too Far Away.
        MissingDarkMatter = 55,  // Missing Reagents: Dark Matter
        CantUseThatItem = 56,  // You Can'T Use That Item
        CantDoWhileCycyloned = 57,  // You Can'T Do That While Cycloned
        TargetHasScroll = 58,  // Target Is Already Affected By A Similar Effect
        PoisonTooStrong = 59,  // That Anti-Venom Is Not Strong Enough To Dispel That Poison
        MustHaveLanceEquipped = 60,  // You Must Have A Lance Equipped.
        MustBeCloseToMaiden = 61,  // You Must Be Near The Maiden Of Winter'S Breath Lake.
        LearnedEverything = 62,  // You Have Learned Everything From That Book
        PetIsDead = 63,  // Your Pet Is Dead
        NoValidTargets = 64,  // There Are No Valid Targets Within Range.
        GmOnly = 65,  // Only Gms May Use That. Your Account Has Been Reported For Investigation.
        RequiresLevel58 = 66,  // You Must Reach Level 58 To Use This Portal.
        AtHonorCap = 67,  // You Already Have The Maximum Amount Of Honor.
        HaveHotRod = 68,  // You Already Have A Hot Rod.
        PartygoerMoreBubbly = 69,  // This Partygoer Wants Some More Bubbly.
        PartygoerNeedBucket = 70,  // This Partygoer Needs A Bucket!
        PartygoerWantToDance = 71,  // This Partygoer Wants To Dance With You.
        PartygoerWantFireworks = 72,  // This Partygoer Wants To See Some Fireworks.
        PartygoerWantAppetizer = 73,  // This Partygoer Wants Some More Hors D'Oeuvres.
        GoblinBatteryDepleted = 74,  // The Goblin All-In-1-Der Belt'S Battery Is Depleted.
        MustHaveDemonicCircle = 75,  // You Must Have A Demonic Circle Active.
        AtMaxRage = 76,  // You Already Have Maximum Rage
        Requires350Engineering = 77,  // Requires Engineering (350)
        SoulBelongsToLichKing = 78,  // Your Soul Belongs To The Lich King
        AttendantHasPony = 79,  // Your Attendant Already Has An Argent Pony
        GoblinStartingMission = 80,  // First, Overload The Defective Generator, Activate The Leaky Stove, And Drop A Cigar On The Flammable Bed.
        GasbotAlreadySent = 81,  // You'Ve Already Sent In The Gasbot And Destroyed Headquarters!
        GoblinIsPartiedOut = 82,  // This Goblin Is All Partied Out!
        MustHaveFireTotem = 83,  // You Must Have A Magma, Flametongue, Or Fire Elemental Totem Active.
        CantTargetVampires = 84,  // You May Not Bite Other Vampires.
        PetAlreadyAtYourLevel = 85,  // Your Pet Is Already At Your Level.
        MissingItemRequiremens = 86,  // You Do Not Meet The Level Requirements For This Item.
        TooManyAbominations = 87,  // There Are Too Many Mutated Abominations.
        AllPotionsUsed = 88,  // The Potions Have All Been Depleted By Professor Putricide.
        DefeatedEnoughAlready = 89,  // You Have Already Defeated Enough Of Them.
        RequiresLevel65 = 90,  // Requires Level 65
        DestroyedKtcOilPlatform = 91,  // You Have Already Destroyed The Ktc Oil Platform.
        LaunchedEnoughCages = 92,  // You Have Already Launched Enough Cages.
        RequiresBoosterRockets = 93,  // Requires Single-Stage Booster Rockets. Return To Hobart Grapplehammer To Get More.
        EnoughWildCluckers = 94,  // You Have Already Captured Enough Wild Cluckers.
        RequiresControlFireworks = 95,  // Requires Remote Control Fireworks. Return To Hobart Grapplehammer To Get More.
        MaxNumberOfRecruits = 96,  // You Already Have The Max Number Of Recruits.
        MaxNumberOfVolunteers = 97,  // You Already Have The Max Number Of Volunteers.
        FrostmourneRenderedResurrect = 98,  // Frostmourne Has Rendered You Unable To Resurrect.
        CantMountWithShapeshift = 99,  // You Can'T Mount While Affected By That Shapeshift.
        FawnsAlreadyFollowing = 100, // Three Fawns Are Already Following You!
        AlreadyHaveRiverBoat = 101, // You Already Have A River Boat.
        NoActiveEnchantment = 102, // You Have No Active Enchantment To Unleash.
        EnoughHighbourneSouls = 103, // You Have Bound Enough Highborne Souls. Return To Arcanist Valdurian.
        Atleast40ydFromOilDrilling = 104, // You Must Be At Least 40 Yards Away From All Other Oil Drilling Rigs.
        AboveEnslavedPearlMiner = 106, // You Must Be Above The Enslaved Pearl Miner.
        MustTargetCorpseSpecial1 = 107, // You Must Target The Corpse Of A Seabrush Terrapin, Scourgut Remora, Or Spinescale Hammerhead.
        SlaghammerAlreadyPrisoner = 108, // Ambassador Slaghammer Is Already Your Prisoner.
        RequireAttunedLocation1 = 109, // Requires A Location That Is Attuned With The Naz'Jar Battlemaiden.
        NeedToFreeDrakeFirst = 110, // Free The Drake From The Net First!
        DragonmawAlliesAlreadyFollow = 111, // You Already Have Three Dragonmaw Allies Following You.
        RequireOpposableThumbs = 112, // Requires Opposable Thumbs.
        NotEnoughHealth2 = 113, // Not Enough Health
        EnoughForsakenTroopers = 114, // You Already Have Enough Forsaken Troopers.
        CannotJumpToBoulder = 115, // You Cannot Jump To Another Boulder Yet.
        SkillTooHigh = 116, // Skill Too High.
        Already6SurvivorsRescued = 117, // You Have Already Rescued 6 Survivors.
        MustFaceShipsFromBalloon = 118, // You Need To Be Facing The Ships From The Rescue Balloon.
        CannotSuperviseMoreCultists = 119, // You Cannot Supervise More Than 5 Arrested Cultists At A Time.
        RequiresLevel85 = 120, // You Must Reach Level 85 To Use This Portal.
        MustBeBelow35Health = 121, // Your Target Must Be Below 35% Health.
        MustSelectSpecialization = 122, // You Must Select A Specialization First.
        TooWiseAndPowerful = 123, // You Are Too Wise And Powerful To Gain Any Benefit From That Item.
        TooCloseArgentLightwell = 124, // You Are Within 10 Yards Of Another Argent Lightwell.
        NotWhileShapeshifted = 125, // You Can'T Do That While Shapeshifted.
        ManaGemInBank = 126, // You Already Have A Mana Gem In Your Bank.
        FlameShockNotActive = 127, // You Must Have At Least One Flame Shock Active.
        CantTransform = 128, // You Cannot Transform Right Now
        PetMustBeAttacking = 129, // Your Pet Must Be Attacking A Target.
        GnomishEngineering = 130, // Requires Gnomish Engineering
        GoblinEngineering = 131, // Requires Goblin Engineering
        NoTarget = 132, // You Have No Target.
        PetOutOfRange = 133, // Your Pet Is Out Of Range Of The Target.
        HoldingFlag = 134, // You Can'T Do That While Holding The Flag.
        TargetHoldingFlag = 135, // You Can'T Do That To Targets Holding The Flag.
        PortalNotOpen = 136, // The Portal Is Not Yet Open.  Continue Helping The Druids At The Sanctuary Of Malorne.
        AggraAirTotem = 137, // You Need To Be Closer To Aggra'S Air Totem, In The West.
        AggraWaterTotem = 138, // You Need To Be Closer To Aggra'S Water Totem, In The North.
        AggraEarthTotem = 139, // You Need To Be Closer To Aggra'S Earth Totem, In The East.
        AggraFireTotem = 140, // You Need To Be Closer To Aggra'S Fire Totem, Near Thrall.
        FacingWrongWay = 141, // You Are Facing The Wrong Way.
        TooCloseToMakeshiftDynamite = 142, // You Are Within 10 Yards Of Another Makeshift Dynamite.
        NotNearSapphireSunkenShip = 143, // You Must Be Near The Sunken Ship At Sapphire'S End In The Jade Forest.
        DemonsHealthFull = 144, // That Demon'S Health Is Already Full.
        OnyxSerpentNotOverhead = 145, // Wait Until The Onyx Serpent Is Directly Overhead.
        ObjectiveAlreadyComplete = 146, // Your Objective Is Already Complete.
        PushSadPandaTowardsTown = 147, // You Can Only Push Sad Panda Towards Sad Panda Town!
        TargetHasStartdust2 = 148, // Target Is Already Affected By Stardust No. 2.
        ElementiumGemClusters = 149, // You Cannot Deconstruct Elementium Gem Clusters While Collecting Them!
        YouDontHaveEnoughHealth = 150, // You Don'T Have Enough Health.
        YouCannotUseTheGatewayYet = 151, // You Cannot Use The Gateway Yet.
        ChooseSpecForAscendance = 152, // You Must Choose A Specialization To Use Ascendance.
        InsufficientBloodCharges = 153, // You Have Insufficient Blood Charges.
        NoFullyDepletedRunes = 154, // No Fully Depleted Runes.
        NoMoreCharges = 155, // No More Charges.
        StatueIsOutOfRangeOfTarget = 156, // Statue Is Out Of Range Of The Target.
        YouDontHaveAStatueSummoned = 157, // You Don'T Have A Statue Summoned.
        YouHaveNoSpiritActive = 158, // You Have No Spirit Active.
        BothDisesasesMustBeOnTarget = 159, // Both Frost Fever And Blood Plague Must Be Present On The Target.
        CantDoThatWithOrbOfPower = 160, // You Can'T Do That While Holding An Orb Of Power.
        CantDoThatWhileJumpingOrFalling = 161, // You Can'T Do That While Jumping Or Falling.
        MustBeTransformedByPolyformicAcid = 162, // You Must Be Transformed By Polyformic Acid.
        NotEnoughAcidToStoreTransformation = 163, // There Isn'T Enough Acid Left To Store This Transformation.
        MustHaveFlightMastersLicense = 164, // You Must Obtain A Flight Master'S License Before Using This Spell.
        AlreadySampledSapFromFeeder = 165, // You Have Already Sampled Sap From This Feeder.
        MustBeNewrMantidFeeder = 166, // Requires You To Be Near A Mantid Feeder In The Heart Of Fear.
        TargetMustBeInDirectlyFront = 167, // Target Must Be Directly In Front Of You.
        CantDoThatWhileMythicKeystoneIsActive = 168, // You Can'T Do That While A Mythic Keystone Is Active.
        WrongClassForMount = 169, // You Are Not The Correct Class For That Mount.
        NothingLeftToDiscover = 170, // Nothing Left To Discover.
        NoExplosivesAvailable = 171, // There Are No Explosives Available.
        YouMustBeFlaggedForPvp = 172, // You Must Be Flagged For Pvp.
        RequiresBattleRations = 173, // Requires Battle Rations Or Meaty Haunch
        RequiresBrittleRoot = 174, // Requires Brittle Root
        RequiresLaborersTool = 175, // Requires Laborer'S Tool
        RequiresUnexplodedCannonball = 176, // Requires Unexploded Cannonball
        RequiresMisplacedKeg = 177, // Requires Misplaced Keg
        RequiresLiquidFire = 178, // Requires Liquid Fire, Jungle Hops, Or Spirit-Kissed Water
        RequiresKrasariIron = 179, // Requires Krasari Iron
        RequiresSpiritKissedWater = 180, // Requires Spirit-Kissed Water
        RequiresSnakeOil = 181, // Requires Snake Oil
        ScenarioIsInProgress = 182, // You Can'T Do That While A Scenario Is In Progress.
        RequiresDarkmoonFaireOpen = 183, // Requires The Darkmoon Faire To Be Open.
        AlreadyAtValorCap = 184, // Already At Valor Cap
        AlreadyCommendedByThisFaction = 185, // Already Commended By This Faction
        OutOfCoins = 186, // Out Of Coins! Pickpocket Humanoids To Get More.
        OnlyOneElementalSpirit = 187, // Only One Elemental Spirit On A Target At A Time.
        DontKnowHowToTameDirehorns = 188, // You Do Not Know How To Tame Direhorns.
        MustBeNearBloodiedCourtGate = 189, // You Must Be Near The Bloodied Court Gate.
        YouAreNotElectrified = 190, // You Are Not Electrified.
        ThereIsNothingToBeFetched = 191, // There Is Nothing To Be Fetched.
        RequiresTheThunderForge = 192, // Requires The Thunder Forge.
        CannotUseTheDiceAgainYet = 193, // You Cannot Use The Dice Again Yet.
        AlreadyMemberOfBrawlersGuild = 194, // You Are Already A Member Of The Brawler'S Guild.
        CantChangeSpecInCelestialChallenge = 195, // You May Not Change Talent Specializations During A Celestial Challenge.
        SpecDoesMatchChallenge = 196, // Your Talent Specialization Does Not Match The Selected Challenge.
        YouDontHaveEnoughCurrency = 197, // You Don'T Have Enough Currency To Do That.
        TargetCannotBenefitFromSpell = 198, // Target Cannot Benefit From That Spell
        YouCanOnlyHaveOneHealingRain = 199, // You Can Only Have One Healing Rain Active At A Time.
        TheDoorIsLocked = 200, // The Door Is Locked.
        YouNeedToSelectWaitingCustomer = 201, // You Need To Select A Customer Who Is Waiting In Line First.
        CantChangeSpecDuringTrial = 202, // You May Not Change Specialization While A Trial Is In Progress.
        CustomerNeedToGetInLine = 203, // You Must Wait For Customers To Get In Line Before You Can Select Them To Be Seated.
        MustBeCloserToGazloweObjective = 204, // Must Be Closer To One Of Gazlowe'S Objectives To Deploy!
        MustBeCloserToThaelinObjective = 205, // Must Be Closer To One Of Thaelin'S Objectives To Deploy!
        YourPackOfVolenIsFull = 206, // Your Pack Of Volen Is Already Full!
        Requires600MiningOrBlacksmithing = 207, // Requires 600 Mining Or Blacksmithing
        ArkoniteProtectorNotInRange = 208, // The Arkonite Protector Is Not In Range.
        TargetCannotHaveBothBeacons = 209, // You Are Unable To Have Both Beacon Of Light And Beacon Of Faith On The Same Target.
        CanOnlyUseOnAfkPlayer = 210, // Can Only Be Used On Afk Players.
        NoLootableCorpsesInRange = 211, // No Lootable Corpse In Range
        ChimaeronTooCalmToTame = 212, // Chimaeron Is Too Calm To Tame Right Now.
        CanOnlyCarryOneTypeOfMunitions = 213, // You May Only Carry One Type Of Blackrock Munitions.
        OutOfBlackrockMunitions = 214, // You Have Run Out Of Blackrock Munitions.
        CarryingMaxAmountOfMunitions = 215, // You Are Carrying The Maximum Amount Of Blackrock Munitions.
        TargetIsTooFarAway = 216, // Target Is Too Far Away.
        CannotUseDuringBossEncounter = 217, // Cannot Use During A Boss Encounter.
        MustHaveMeleeWeaponInBothHands = 218, // Must Have A Melee Weapon Equipped In Both Hands
        YourWeaponHasOverheated = 219, // Your Weapon Has Overheated.
        MustBePartyLeaderToQueue = 220, // You Must Be A Party Leader To Queue Your Group.
        NotEnoughFuel = 221, // Not Enough Fuel
        YouAreAlreadyDisguised = 222, // You Are Already Disguised!
        YouNeedToBeInShredder = 223, // You Need To Be In A Shredder To Chop This Up!
        FoodCannotEatFood = 224, // Food Cannot Eat Food
        MysteriousForcePreventsOpeningChest = 225, // A Mysterious Force Prevents You From Opening The Chest.
        CantDoThatWhileHoldingEmpoweredOre = 226, // You Can'T Do That While Holding Empowered Ore.
        NotEnoughAmmunition = 227, // Not Enough Ammunition!
        YouNeedBeatfaceTheGladiator = 228, // You Need Beatface The Sparring Arena Gladiator To Break This!
        YouCanOnlyHaveOneWaygate = 229, // You Can Only Have One Waygate Open. Disable An Activated Waygate First.
        YouCanOnlyHaveTwoWaygates = 230, // You Can Only Have Two Waygates Open. Disable An Activated Waygate First.
        YouCanOnlyHaveThreeWaygates = 231, // You Can Only Have Three Waygates Open. Disable An Activated Waygate First.
        RequiresMageTower = 232, // Requires Mage Tower
        RequiresSpiritLodge = 233, // Requires Spirit Lodge
        FrostWyrmAlreadyActive = 234, // A Frost Wyrm Is Already Active.
        NotEnoughRunicPower = 235, // Not Enough Runic Power
        YouAreThePartyLeader = 236, // You Are The Party Leader.
        YulonIsAlreadyActive = 237, // Yu'Lon Is Already Active.
        AStampedeIsAlreadyActive = 238, // A Stampede Is Already Active.
        YouAreAlreadyWellFed = 239, // You Are Already Well Fed.
        CantDoThatUnderSuppressiveFire = 240, // You Cannot Do That While Under Suppressive Fire.
        YouAlreadyHaveMurlocSlop = 241, // You Already Have A Piece Of Murloc Slop.
        YouDontHaveArtifactFragments = 242, // You Don'T Have Any Artifact Fragments.
        YouArentInAParty = 243, // You Aren'T In A Party.
        Requires20Ammunition = 244, // Requires 30 Ammunition!
        Requires30Ammunition = 245, // Requires 20 Ammunition!
        YouAlreadyHaveMaxOutcastFollowers = 246, // You Already Have The Maximum Amount Of Outcasts Following You.
        NotInWorldPvpZone = 247, // Not In World Pvp Zone.
        AlreadyAtResourceCap = 248, // Already At Resource Cap
        ApexisSentinelRequiresEnergy = 249, // This Apexis Sentinel Requires Energy From A Nearby Apexis Pylon To Be Powered Up.
        YouMustHave3OrFewerPlayer = 250, // You Must Have 3 Or Fewer Players.
        YouAlreadyReadTreasureMap = 251, // You Have Already Read That Treasure Map.
        MayOnlyUseWhileGarrisonUnderAttack = 252, // You May Only Use This Item While Your Garrison Is Under Attack.
        RequiresActiveMushrooms = 253, // This Spell Requires Active Mushrooms For You To Detonate.
        RequiresFasterTimeWithRacer = 254, // Requires A Faster Time With The Basic Racer
        RequiresInfernoShotAmmo = 255, // Requires Inferno Shot Ammo!
        YouCannotDoThatRightNow = 256, // You Cannot Do That Right Now.
        ATrapIsAlreadyPlacedThere = 257, // A Trap Is Already Placed There.
        YouAreAlreadyOnThatQuest = 258, // You Are Already On That Quest.
        RequiresFelforgedCudgel = 259, // Requires A Felforged Cudgel!
        CantTakeWhileBeingDamaged = 260, // Can'T Take While Being Damaged!
        YouAreBoundToDraenor = 261, // You Are Bound To Draenor By Archimonde'S Magic.
        AlreayHaveMaxNumberOfShips = 262, // You Already Have The Maximum Number Of Ships Your Shipyard Can Support.
        MustBeAtShipyard = 263, // You Must Be At Your Shipyard.
        RequiresLevel3MageTower = 264, // Requires A Level 3 Mage Tower.
        RequiresLevel3SpiritLodge = 265, // Requires A Level 3 Spirit Lodge.
        YouDoNotLikeFelEggsAndHam = 266, // You Do Not Like Fel Eggs And Ham.
        AlreadyEnteredInThisAgreement = 267, // You Have Already Entered In To This Trade Agreement.
        CannotStealThatWhileGuardsAreOnDuty = 268, // You Cannot Steal That While Guards Are On Duty.
        YouAlreadyUsedVantusRune = 269, // You Have Already Used A Vantus Rune This Week.
        ThatItemCannotBeObliterated = 270, // That Item Cannot Be Obliterated.
        NoSkinnableCorpseInRange = 271, // No Skinnable Corpse In Range
        MustBeMercenaryToUseTrinket = 272, // You Must Be A Mercenary To Use This Trinket.
        YouMustBeInCombat = 273, // You Must Be In Combat.
        NoEnemiesNearTarget = 274, // No Enemies Near Target.
        RequiresLeyspineMissile = 275, // Requires A Leyspine Missile
        RequiresBothCurrentsConnected = 276, // Requires Both Currents Connected.
        CantDoThatInDemonForm = 277, // Can'T Do That While In Demon Form (Yet)
        YouDontKnowHowToTameMechs = 278, // You Do Not Know How To Tame Or Obtain Lore About Mechs.
        CannotCharmAnyMoreWithered = 279, // You Cannot Charm Any More Withered.
        RequiresActiveHealingRain = 280, // Requires An Active Healing Rain.
        AlreadyCollectedAppearances = 281, // You'Ve Already Collected These Appearances
        CannotResurrectSurrenderedToMadness = 282, // Cannot Resurrect Someone Who Has Surrendered To Madness
        YouMustBeInCatForm = 283, // You Must Be In Cat Form.
        YouCannotReleaseSpiritYet = 284, // You Cannot Release Spirit Yet.
        NoFishingNodesNearby = 285, // No Fishing Nodes Nearby.
        YouAreNotInCorrectSpec = 286, // You Are Not The Correct Specialization.
        UlthaleshHasNoPowerWithoutSouls = 287, // Ulthalesh Has No Power Without Souls.
        CannotCastThatWithVoodooTotem = 288, // You Cannot Cast That While Talented Into Voodoo Totem.
        AlreadyCollectedThisAppearance = 289, // You'Ve Already Collected This Appearance.
        YourPetMaximumIsAlreadyHigh = 290, // Your Total Pet Maximum Is Already This High.
        YouDontHaveEnoughWithered = 291, // You Do Not Have Enough Withered To Do That.
        RequiresNearbySoulFragment = 292, // Requires A Nearby Soul Fragment.
        RequiresAtLeast10Withered = 293, // Requires At Least 10 Living Withered
        RequiresAtLeast14Withered = 294, // Requires At Least 14 Living Withered
        RequiresAtLeast18Withered = 295, // Requires At Least 18 Living Withered
        Requires2WitheredManaRagers = 296, // Requires 2 Withered Mana-Ragers
        Requires1WitheredBerserke = 297, // Requires 1 Withered Berserker
        Requires2WitheredBerserker = 298, // Requires 2 Withered Berserkers
        TargetHealthIsTooLow = 299, // Target'S Health Is Too Low
        CannotShapeshiftWhileRidingStormtalon = 300, // You Cannot Shapeshift While Riding Stormtalon
        CannotChangeSpecInCombatTraining = 301, // You Can Not Change Specializations While In Combat Training.
        UnknownPhenomenonPreventsLeylineConnection = 302, // Unknown Phenomenon Is Preventing A Connection To The Leyline.
        TheNightmareObscuresYourVision = 303, // The Nightmare Obscures Your Vision.
        YouAreInWrongClassSpec = 304, // You Are In The Wrong Class Specialization.
        ThereAreNoValidCorpsesNearby = 305, // There Are No Valid Corpses Nearby.
        CantCastThatRightNow = 306, // Can'T Cast That Right Now.
        NotEnoughAncientMan = 307, // Not Enough Ancient Mana.
        RequiresSongScroll = 308, // Requires A Song Scroll To Function.
        MustHaveArtifactEquipped = 309, // You Must Have An Artifact Weapon Equipped.
        RequiresCatForm = 310, // Requires Cat Form.
        RequiresBearForm = 311, // Requires Bear Form.
        RequiresConjuredFood = 312, // Requires Either A Conjured Mana Pudding Or Conjured Mana Fritter.
        RequiresArtifactWeapon = 313, // Requires An Artifact Weapon.
        YouCantCastThatHere = 314, // You Can'T Cast That Here
        CantDoThatOnClassTrial = 315, // You Cannot Do That While On A Class Trial.
        RitualOfDoomOncePerDay = 316, // You Can Only Benefit From The Ritual Of Doom Once Per Day.
        CannotRitualOfDoomWhileSummoningSiters = 317, // You Cannot Perform The Ritual Of Doom While Attempting To Summon The Sisters.
        LearnedAllThatYouCanAboutYourArtifact = 318, // You Have Learned All That You Can About Your Artifact.
        CantCallPetWithLoneWolf = 319, // You Cannot Use Call Pet While Lone Wolf Is Active.
        TargetCannotAlreadyHaveOrbOfPower = 320, // Target cannot already have a Orb of Power.
        YouMustBeInAnInnToStrumThatGuitar = 321, // You must be in an inn to strum that guitar.
        YouCannotReachTheLatch = 322, // You cannot reach the latch.
        RequiresABrimmingKeystone = 323, // Requires A Brimming Keystone.
        YouMustBeWieldingTheUnderlightAngler = 324, // You Must Be Wielding The Underlight Angler.
        YourTargetMustBeShackled = 325, // Your Target Must Be Shackled.
        YouAlreadyPossesAllOfTheKnowledgeContainedInThosePages = 326, // You Already Possess All Of The Knowledge Contained In These Pages.
        YouCantRiskGettingTheGrummelsWet = 327, // You Can'T Risk Getting The Grummels Wet!
        YouCannotChangeSpecializationRightNow = 328, // You Cannot Change Specializations Right Now.
        YouveReachedTheMaximumNumberOfArtifactResearchNotesAvailable = 329, // You'Ve Reached The Maximum Number Of Artifact Research Notes Available.
        YouDontHaveEnoughNethershards = 330, // You Don'T Have Enough Nethershards.
        TheSentinaxIsNotPatrollingThisArea = 331, // The Sentinax Is Not Patrolling This Area.
        TheSentinaxCannotOpenAnotherPortalRightNow = 332, // The Sentinax Cannot Open Another Portal Right Now.
        YouCannotGainAdditionalReputationWithThisItem = 333, // You Cannot Gain Additional Reputation With This Item.
        CantDoThatWhileGhostWolfForm = 334, // Can'T Do That While In Ghost Wolf Form.
        YourSuppliesAreFrozen = 335, // Your Supplies Are Frozen.
        YouDoNotKnowHowToTameFeathermanes = 336, // You Do Not Know How To Tame Feathermanes.
        YouMustReachArtifactKnowledgeLevel25 = 337, // You Must Reach Artifact Knowledge Level 25 To Use The Tome.
        RequiresANetherPortalDisruptor = 338, // Requires A Nether Portal Disruptor.
        YouAreNotTheCorrectRankToUseThisItem = 339, // You Are Not The Correct Rank To Use This Item.
        MustBeStandingNearInjuredChromieInMountHyjal = 340, // Must Be Standing Near The Injured Chromie In Mount Hyjal.
        TheresNothingFurtherYouCanLearn = 341, // There'S Nothing Further You Can Learn.
        RemoveCannonsHeavyIronPlatingFirst = 342, // You Should Remove The Cannon'S Heavy Iron Plating First.
        RemoveCannonsElectrokineticDefenseGridFirst = 343, // You Should Remove The Cannon'S Electrokinetic Defense Grid First.
        RequiresTheArmoryKeyAndDendriteClusters = 344, // You Are Missing Pieces Of The Armory Key Or Do Not Have Enough Dendrite Clusters.
        ThisItemRequiresBasicObliterumToUpgrade = 345, // This Item Requires Basic Obliterum To Upgrade.
        ThisItemRequiresPrimalObliterumToUpgrade = 346, // This Item Requires Primal Obliterum To Upgrade.
        ThisItemRequiresFlightMastersWhistle = 347, // This Item Requires A Flight Master'S Whistle.
        RequiresMorrisonsMasterKey = 348, // Requires Morrison'S Master Key.
        RequiresPowerThatEchoesThatOfTheAugari = 349, // Will Only Open To One Wielding The Power That Echoes That Of The Augari.
        ThatPlayerHasAPendingTotemicRevival = 350, // That Player Has A Pending Totemic Revival.
        YouHaveNoFireMinesDeployed = 351, // You Have No Fire Mines Deployed.
        MustBeAffectedBySpiritPowder = 352, // You Must Be Affected By The Spirit Powder To Take The Phylactery.
        YouAreBlockedByAStructureAboveYou = 353, // You Are Blocked By A Structure Above You.
        Requires100ImpMeat = 354, // Requires 100 Imp Meat.
        YouHaveNotObtainedAnyBackgroundFilters = 355, // You Have Not Obtained Any Background Filters.
        NothingInterestingPostedHereRightNow = 356, // There Is Nothing Interesting Posted Here Right Now.
        ParagonReputationRequiresHigherLevel = 357, // Paragon Reputation Is Not Available Until A Higher Level.
        UunaIsMissing = 358, // Uuna Is Missing.
        OnlyOtherHivemindMembersMayJoin = 359, // Only Other Members Of Their Hivemind May Join With Them.
        NoValidFlaskPresent = 360, // No Valid Flask Present.
        NoWildImpsToSacrifice = 361, // There Are No Wild Imps To Sacrifice.
        YouAreCarryingTooMuchIron = 362, // You Are Carrying Too Much Iron
        YouHaveNoIronToCollect = 363, // You Have No Iron To Collect
        YouHaveNoWildImps = 364, // You Have No Available Wild Imps.
        NeedsRepairs = 365, // Needs Repairs.
        YouAreCarryingTooMuchWood = 366, // You'Re Carrying Too Much Wood.
        YouAreAlreadyCarryingRepairParts = 367, // You'Re Already Carrying Repair Parts.
        YouHaveNotUnlockedFlightWhistleForZone = 368, // You Have Not Unlocked The Flight Whistle For This Zone.
        ThereAreNoUnlockedFlightPointsNearby = 369, // There Are No Unlocked Flight Points Nearby To Take You To.
        YouMustHaveAFelguard = 370, // You Must Have A Felguard.
        TargetHasNoFesteringWounds = 371, // The Target Has No Festering Wounds.
        YouDontHaveDeadlyOrWoundPoisonActive = 372, // You Do Not Have Deadly Poison Or Wound Poison Active.
        CannotReadSoldierDogTagWithoutHeadlampOn = 373, // You Cannot Read The Soldier'S Dog Tag Without Your Headlamp On.
        YouHaveNoWoodToCollect = 374, // You Have No Wood To Collect.
        YouAreNotWearingAShirt = 375, // You Are Not Wearing A Shirt!
        TargetMustBeDead = 376, // Target Must Be Dead.
        YourTargetIsAlreadyEmbiggified = 377, // Your Target Is Already Embiggified.
        YouMustTargetASinisterGladiatorItem = 378, // You Must Target A Sinister Gladiator'S Item To Upgrade.
        ThisItemLevelIsTooHighForThisUpgrade = 379, // This Item'S Level Is Too High For This Upgrade.
        TheBallistaCannotBeUsedWhileOnFire = 380, // The Ballista Cannot Be Used While On Fire.
        YouMustTargetADreadGladiatorItem = 381, // You Must Target A Dread Gladiator'S Item To Upgrade.
        YouDoKnotKnowHowToTameBloodBeasts = 382, // You Do Not Know How To Tame Blood Beasts.
        CanOnlyBeUsedInTheEvening = 385, // Can Only Be Used In The Evening.
        RequiresPakuToBeYourChosenLoa = 386, // Requires Pa'Ku To Be Your Chosen Loa.
        RequiresVigorEngaged = 387, // Requires V.I.G.O.R. Engaged.
        YourTargetIsNotHungry = 388, // Your Target Is Not Hungry.
        YouCanOnlyHaveOnTreasureMapMission = 389, // You Can Only Have One Treasure Map Mission At A Time.
        YouAlreadyHaveASilasSphereOfTransmutation = 390, // You Already Have A Silas' Sphere Of Transmuation.
        YouDoNotHaveTheMalletOfThunderousSkins = 391, // You Do Not Have The Mallet Of Thunderous Skins.
        YouMustHaveAnOpenStableSlot = 393, // You Must Have An Open Stable Slot.
        DoesNotWorkOnCritters = 394, // Does Not Work On Critters.
        CanOnlyBeUsedOnHati = 395, // Can Only Be Used On Hati.
        YouAlreadyHaveIwensEnchantingRod = 396, // You Already Have An Iwen'S Enchanting Rod.
        YouAlreadyHaveMalletOfThunderousSkins = 397, // You Already Have A Mallet Of Thunderous Skins.
        CanOnlyBeUsedOnInertTideWatchersOrVoodooMasks = 398, // Can Only Be Used On Inert Tide Watchers Or Inert Voodoo Masks.
        YouMustBeAtShrineToMakeOfferingToLoa = 399, // You Must Be At A Shrine To Make An Offering To A Loa.
        RequiresEmeraldEmpowerment = 400, // Requires Emerald Empowerment.
        YouMustBeAnHonoredTauren = 401, // You Must Be An Honored Tauren.
        RequiresChitterspineMeat = 402, // Requires Chitterspine Meat.
        RequiresHeartForge = 403, // Requires Heart Forge.
        NotAuthorizedToAccessChargingStation = 405, // You Are Not Authorized To Access This Charging Station. Speak To Flux.
        RequiresMardivasArcaneCoffer = 406, // Requires Mardivas'S Arcane Coffer
        RequiresHeartOfAzerothAtopHeartForge = 407, // Requires Heart Of Azeroth Placed Atop Heart Forge.
        RequiresBrinestonePickaxe = 408, // Requires A Brinestone Pickaxe.
        YouAlreadyCollectedDataOnThisTarget = 409, // You Have Already Collected Data On This Target.
        YouAlreadyHaveThisEssenceForCurrentSpec = 410, // You Already Have This Essence For Your Current Loot Specialization
        YouCannotSummonAnotherPetWhileRidingHati = 411, // You Cannot Summon Another Pet While Riding Hati.
        YouHaveAlreadyCollectedThisAzerothMini = 422, // You Have Already Collected This Azeroth Mini
        YourTargetIsAlreadyAffectedByTeaTime = 412, // Your Target Is Already Affected By Tea Time!
        YouMustCompleteQuestTheHeartForgeToInfuseEssence = 413, // You Must Complete The Quest "The Heart Forge" To Infuse An Essence
        ThisTargetDoesNotHaveYourRazorCoral = 414, // This Target Does Not Have Your Razor Coral.
        YouDoNotHaveEnoughOfThatItem = 415, // You Do Not Have Enough Of That Item.
        YourTargetIsNotWearingUnboundCursedLoversRing = 417, // Your Target Is Not Wearing An Unbound Cursed Lover'S Ring
        YourCursedLoversRingIsAlredyBound = 418, // Your Cursed Lover'S Ring Is Already Bound
        YouMustTargetANotoriusGladiatorItem = 421, // You Must Target A Notorious Gladiator'S Item To Upgrade.
        YouCantCarryMorePickaxesChumSeeds = 423, // You Can'T Carry Any More Brinestone Pickaxes, Chum, Or Germinating Seeds.
        RequiresHolidayFeastOfWinterWeil = 424, // Requires Holiday: Feast Of Winter Veil
        RequiresAshjrakamasShroudOfResolve = 425, // Requires Ashjra'Kamas, Shroud Of Resolve.
        RequiresWarMode = 426, // Requires War Mode.
        OnlyOneOfThisMaskMayBeWorn = 427, // Only One Of This Mask May Be Worn.
        YouCannotAscendWhileTheTarragrueIsNearby = 428, // You Cannot Ascend While The Tarragrue Is Nearby.
        TargetDoesNotHaveAValidAzeriteEssence = 429, // Target Does Not Have A Valid Azerite Essence.
        YourMindIsStillRecoveringFromRecentVision = 430, // Your Mind Is Still Recovering From A Recent Vision.
        RequiresVesselOfHorrificVisions = 431, // Requires Vessel Of Horrific Visions.
        RequiresAllPartyMembersToBeWearingAshjrakamasShroudOfResolve = 432, // Requires All Party Members To Be Wearing Ashjra'Kamas, Shroud Of Resolve.
        RequiresAllPartyMembersToPossessAVesselOfHorrificVisions = 434, // Requires All Party Members To Possess A Vessel Of Horrific Visions.
        YouAlreadyHaveTheHighestRankedEssenceAvailableFromThisSource = 435, // You Already Have The Highest Ranked Essence Available From This Source.
        RequiresDarkmoonGameToken = 436, // Requires Darkmoon Game Token.
        YouAreNotTheRightProfession = 437, // You Are Not The Right Profession.
        YouAlreadyKnowHowToCraftAVoidFocus = 438, // You Already Know How To Craft A Void Focus.
        YouAlreadyKnowTheRecipesInThisBook = 439, // You Already Know The Recipes In This Book.
        YouMustTargetACorruptedGladiatorsItem = 440, // You Must Target A Corrupted Gladiator'S Item To Upgrade.
        RequiresTheFixItStick = 441, // Requires The Fix-It-Stick.
        ThatItemCannotReceiveAdditionalSockets = 442, // That Item Cannot Receive Additional Sockets.
        YouAlreadyHaveAContractedVeteranTroop = 443, // You Already Have A Contracted Veteran Troop.
        YouAreCurrentlyAtYourTroopCapacity = 444, // You Are Currently At Your Troop Capacity.
        YouDontHaveEnoughAnima = 445, // You Don'T Have Enough Anima
        TargetAlreadyHoldingVoidTouchedSkull = 446, // That Player Is Already Holding A Void-Touched Skull.
        TargetsInventoryIsFull = 447, // Target'S Inventory Is Full.
        TargetsMindIsProtectedByNeuralSilencer = 448, // Your Target'S Mind Is Protected By A Neural Silencer.
        AllTargetsMindsAreProtectedByNeuralSilencers = 449, // All Of Your Targets' Minds Are Protected By Neural Silencers.
        YouMustFindAMorePowerfulCoreToProgressYourCloakRanksFurther = 450, // You Must Find A More Powerful Core To Progress Your Cloak Ranks Further.
        YouCannotUseThisItemInWarMode = 451, // You Cannot Use This Item In War Mode.
        YouCannotMakeYourCampHere = 452, // You Cannot Make Your Camp Here.
        RequiresTitanicBeacon = 453, // Requires Titanic Beacon
        ThatObjectIsLocked = 454, // That Object Is Locked.
        InvalidCombination = 455, // Invalid Combination.
        NoNearbyEnemyPlayersAreCorrupted = 456, // No Nearby Enemy Players Are Corrupted.
        ThatSpellIsAlreadyActive = 457, // That Spell Is Already Active
        YouCannotUseThisWhenTheTarragrueHasBeenAlerted = 458, // You Cannot Use This When The Tarragrue Has Been Alerted.
        ThatGuestAlreadyHasTea = 459, // That Guest Already Has Tea.
        RequiresShadowlandsSkinning = 460, // Requires Shadowlands Skinning.
        RequiresHuntersMarkOnATarget = 461, // Requires Hunter'S Mark On A Target.
        HoundmasterLokseyIsBusy = 462, // Houndmaster Loksey Is Busy.
        RequiresCoilOfRope = 463, // Requires Coil Of Rope.
        MustBeInARestArea = 464, // Must Be In A Rest Area.
        TargetIsLinkedToSomebodyElse = 465, // Target Is Linked To Somebody Else.
        YouDontHaveASummonedGhoul = 466, // You Don'T Have A Summoned Ghoul.
        OneOfYourPartyMembersIsAnIneligibleClass = 467, // One Of Your Party Members Is An Ineligible Class.
        YouMustSelectASoulbindBossAndTierFirst = 468, // You Must Select A Soulbind, Boss, And Tier First.
        ThatGuestDoesntWantThis = 469, // That Guest Doesn'T Want This.
        YouMustDefeatTheEmpoweredGuardToAscend = 470, // You Must Defeat The Empowered Guard To Ascend.
        RequiresSoulsteelForge = 471, // Requires Soulsteel Forge.
        RequiresProofOfPurity = 472, // Requires Proof Of Purity
        RequiresProofOfHumility = 473, // Requires Proof Of Humility
        RequiresProofOfCourage = 474, // Requires Proof Of Courage
        RequiresProofOfWisdom = 475, // Requires Proof Of Wisdom
        RequiresProofOfLoyalty = 476, // Requires Proof Of Loyalty
        RequiresArcaneSpecilization = 477, // Requires Arcane Specilization.
        PleaseGatherYourPartyBeforeQueuing = 478, // Please Gather Your Party Before Queuing.
        YouDoNotKnowHowToTameGargon = 479, // You Do Not Know How To Tame Gargon.
        RequiresDeadSpriggan = 480, // Requires Dead Spriggan
        YouAlreadyUsedAProfessionJournalThisWeek = 481, // You Have Already Used A Profession Journal This Week.
        RequiresMordretharTheDeathGate = 482, // Requires Mord'Rethar: The Death Gate.
        RequiresPlaguefallen = 483, // Requires Plaguefallen
        YouCannotFitThroughThere = 484, // You Cannot Fit Through There.
        ABindingRitualPreventsThisFromOpening = 485, // A Binding Ritual Prevents This From Opening.
        ThatCharmIsAlreadyApplied = 486, // That Charm Is Already Applied.
        ThatSigilIsAlreadyApplied = 487, // That Sigil Is Already Applied.
        AtLeastOneGuestMustRsvpBeforeYouOpenCourt = 488, // At Least One Guest Must Rsvp Before You Open Court.
        ThereIsNoTimeLimitToIncrease = 489, // There Is No Time Limit To Increase.
        YourHeartOfAzerothIsCurrentlyDisabled = 490, // Your Heart Of Azeroth Is Currently Disabled.
        EssenceYouAreTryingToActivateIsInvalid = 491, // The Essence You Are Trying To Activate Is Invalid.
        RequiresMedallionOfService = 492, // Requires Medallion Of Service
        AllPlayersMustHaveQuestTorghastTowerOfTheDamned = 493, // All Players Must Have Quest - Torghast: Tower Of The Damned.
        RequiresShadowlandsEngineering = 494, // Requires Shadowlands Engineering
        YouAreNotInDeepEnoughWater = 495, // You Are Not In Deep Enough Water.
        RequiresFreshWatersOfArdenwealdOrBastion = 496, // Requires The Fresh Waters Of Ardenweald Or Bastion
        Requires30InfusedRubies = 497, // Requires 30 Infused Rubies
        TheCurseOfTeramaniksLegacyIsKeepingYourMountsFromHeedingYourCall = 498, // The Curse Of Teramanik'S Legacy Is Keeping Your Mounts From Heeding Your Call.
        YouDoNotKnowHowToTameCloudSerpents = 499, // You Do Not Know How To Tame Cloud Serpents.
        YouDoNotKnowHowToTameUndeadCreatures = 500, // You Do Not Know How To Tame Undead Creatures.
        RequiresTheForgeOfBonds = 501, // Requires The Forge Of Bonds
        RequiresGatamatos = 502, // Requires Gatamatos
        MustBeChannellingMindSear = 503, // Must Be Channelling Mind Sear.
        YouDontHaveAnyPeriodicEffectsActive = 504, // You Don'T Have Any Periodic Effects Active.
        YouAreNotBestFriendsWithAnyEmberCourtGuests = 505, // You Are Not Best Friends With Any Ember Court Guests.
        YouMustObtainVenomousSolvents = 506, // You Must Obtain Venomous Solvents.
        YouMustObtainDreadPollen = 507, // You Must Obtain Dread Pollen.
        APartyMemberDoesNotHaveThatLayerUnlocked = 508, // A Party Member Does Not Have That Layer Unlocked
        InventoryIsFull = 509, // Inventory Is Full.
        YouHaveNoAnimaToDeposit = 510, // You Have No Anima To Deposit
        YourMountIgnoresYourCallWithinTheMaw = 511, // Your Mount Ignores Your Call Within The Maw.
        YourButlerIsAlreadyPresentSomewhereInTheEmberCourt = 512, // Your Butler Is Already Present Somewhere In The Ember Court.
        YouHaveAlreadyBuiltThisConstruct = 513, // You Have Already Built This Construct
        RequiresInnerAltarOfDomination = 514, // Requires Inner Altar Of Domination
        PartyMemberDoesNotMeetRequirementsToQueue = 515, // A Party Member Does Not Meet The Requirements To Queue
        NoConstructCurrentlyActive = 516, // No Construct Currently Active
        CompleteTheQuestLineWelcomeToZandalar = 517, // Complete The Quest Line "Welcome To Zandalar" To Use This Spell.
        CompleteTheQuestLineANationDivided = 518, // Complete The Quest Line "A Nation Divided" To Use This Spell.
        CannotBeUsedOnCommonQualityItems = 519, // Cannot Be Used On Common Quality Items.
        YouMustBePledgedToTheVenthyr = 520, // You Must Be Pledged To The Venthyr.
        YouMustBePledgedToTheNightFae = 521, // You Must Be Pledged To The Night Fae.
        YouMustBePledgedToTheKyrian = 522, // You Must Be Pledged To The Kyrian.
        YouMustBePledgedToTheNecrolords = 523, // You Must Be Pledged To The Necrolords.
        YouMustBeInTheShadowlands = 524, // You Must Be In The Shadowlands.
        RequiresSanctumReservoir = 525, // Requires Sanctum Reservoir.
        ThisWildseedOfRegrowthIsStillIncubating = 526, // This Wildseed Of Regrowth Is Still Incubating.
        ThisWildseedOfRegrowthIsStillGrowing = 527, // This Wildseed Of Regrowth Is Still Growing.
        YouMustBePartyLeaderToStartThisEscort = 528, // You Must Be The Party Leader To Start This Escort.
        YouHaveFullyUpgradedAllOfYourConduits = 529, // You Have Fully Upgraded All Of Your Conduits.
        YouHaveAlreadyAddedThatConduit = 530, // You Have Already Added That Conduit To The Forge Of Bonds.
        TargetMustBeWeakened = 531, // Target Must Be Weakened.
        YouCannotAddThatConduitToForgeOfBonds = 532, // You Cannot Add That Conduit To The Forge Of Bonds.
        YouCannotSoulshapeDuringLichborne = 533, // You Cannot Soulshape During Lichborne.
        YouCantDoThatWhileCarryingAnAnimacone = 534, // You Can'T Do That While Carrying An Animacone.
        NecessaryConstructNotPresent = 535, // Necessary Construct Not Present
        ThatGuestIsAlreadyCoveredInGelatin = 536, // That Guest Is Already Covered In Gelatin.
        YouNeedToWaitToUseThisItem = 537, // You Need To Wait To Use This Item.
        ThatAllyAlreadyHasTea = 538, // That Ally Already Has Tea.
        TargetDoesNotNeedTea = 539, // The Target Does Not Need Tea.
        TheyDontDeserveYourFancyTea = 540, // They Don'T Deserve Your Fancy Tea!
        YourGormPrefersToEatDeadThings = 541, // Your Gorm Prefers To Eat Dead Things.
        YourGormHasAlreadyEatenThatCorpse = 542, // Your Gorm Has Already Eaten That Corpse.
        YouNeedAGormlingFromNiyaToDoThat = 543, // You Need A Gormling From Niya To Do That.
        TargetIsAlreadyShrunken = 544, // Target Is Already Shrunken.
        TargetIsAlreadyEnlarged = 545, // Target Is Already Enlarged.
        LadyMoonberrysWandIsForEnemiesAndMawrats = 546, // Lady Moonberry'S Wand Is Meant For Enemies And Mawrats.
        LadyMoonberrysWandIsForEnemies = 547, // Lady Moonberry'S Wand Is Meant For Enemies.
        TargetIsAlreadyTransformedIntoASnail = 548, // Target Is Already Transformed Into A Snail.
        YourTargetMustBeBelow50PctHealth = 549, // Your Target Must Be Below 50% Health.
        LadyMoonberrysWandIsDrainedOfPower = 550, // Lady Moonberry'S Wand Is Drained Of Power.
        ThisBookHoldsNoRecipesForYourProfession = 551, // This Book Holds No Recipes For Your Profession.
        YouHaveNoKorthianRelicsInYourInventory = 552, // You Have No Korthian Relics In Your Inventory.
        YouMustBeInTheRiftToInteractWithThis = 553, // You Must Be In The Rift To Interact With This.
        CannotSummonWhileInTheRift = 554, // Cannot Summon While In The Rift.
        YouNeedAnActiveElementalShield = 600, // You Need An Active Elemental Shield.
        SpeakToArchivistToTurnInRelicFragments = 601, // Speak To The Archivist To Turn In Relic Fragments.
        RequiresResearchReportsAncientShrines = 602, // Requires Knowledge From Research Reports: Ancient Shrines
        YourStewardIsNotPresent = 603, // Your Steward Companion Is Not Present.
        YourStewardHasAlreadyBeenTransformed = 604, // Your Steward Companion Has Already Been Transformed.
        RequiresKorthianRelics = 605, // Requires Korthian Relics
        RequiresFullEmpoweredBar = 606, // Requires Full Empowered Bar
        RequiresYouToBeRidingAYak = 607, // Requires You To Be Riding A Yak.
        JaithysIsNotACannibal = 609, // Jaithys Is Many Things, But Jaithys Is Not A Cannibal.
        ThatItemIsPunyUnworthyFindAnother = 610, // That Item Is Puny. Unworthy. Find Another.
        JaithysHungersOnlyForWeapons = 611, // Jaithys Hungers Only For Weapons. Only Weapons Will Do.
        ReturnToReliquaryOfRememberanceToSubmitKorthianRelics = 612, // Return To The Reliquary Of Rememberance To Submit Korthian Relics To Archivist Roh-Suir.
        ShardsOfDominationCanBeRemovedBySoulfireChisel = 615, // Shards Of Domination Can Only Be Removed By The Soulfire Chisel.
        YouHaveLearnedEverythingFromThatMap = 616, // You Have Learned Everything From That Map.
        YouMustTargetACritterToHelpItAtoneForItsSins = 617, // You Must Target A Critter To Help It Atone For Its Sins.
        YouMustBeAMemberOfTheKyrianCovenantOrHaveReachedRenown80WithTheKyrian = 619, // You Must Be A Member Of The Kyrian Covenant Or Have Reached Renown 80 With The Kyrian.
        YouMustBeAMemberOfTheNecrolordCovenantOrHaveReachedRenown80WithTheNecrolords = 620, // You Must Be A Member Of The Necrolord Covenant Or Have Reached Renown 80 With The Necrolords.
        YouMustBeAMemberOfTheNightFaeCovenantOrHaveReachedRenown80WithTheNightFae = 621, // You Must Be A Member Of The Night Fae Covenant Or Have Reached Renown 80 With The Night Fae.
        YouMustBeAMemberOfTheVenthyrCovenantOrHaveReachedRenown80WithTheVenthyr = 622, // You Must Be A Member Of The Venthyr Covenant Or Have Reached Renown 80 With The Venthyr.
        YouAlreadyHaveThatMuchRenownWithYourActiveCovenant = 627, // You Already Have That Much Renown With Your Active Covenant.
        CannotExceedTheMaximumForThatCurrency = 628, // Cannot Exceed The Maximum For That Currency.
    }

    public enum SpellMissInfo
    {
        None = 0,
        Miss = 1,
        Resist = 2,
        Dodge = 3,
        Parry = 4,
        Block = 5,
        Evade = 6,
        Immune = 7,
        Immune2 = 8, // One Of These 2 Is MissTempimmune
        Deflect = 9,
        Absorb = 10,
        Reflect = 11
    }

    public enum SpellHitType
    {
        CritDebu = 0x1,
        Crit = 0x2,
        HitDebug = 0x4,
        Split = 0x8,
        VictimIsAttacker = 0x10,
        AttackTableDebug = 0x20,
        Unk = 0x40,
        NoAttacker = 0x80 // does the same as SPELL_ATTR4_COMBAT_LOG_NO_CASTER
    }

    public enum SpellDmgClass
    {
        None = 0,
        Magic = 1,
        Melee = 2,
        Ranged = 3
    }

    public enum SpellPreventionType
    {
        Silence = 1,
        Pacify = 2,
        NoActions = 4
    }

    [Flags]
    public enum SpellCastTargetFlags
    {
        None = 0x0,
        Unused1 = 0x01,               // Not Used
        Unit = 0x02,               // Pguid
        UnitRaid = 0x04,               // Not Sent, Used To Validate Target (If Raid Member)
        UnitParty = 0x08,               // Not Sent, Used To Validate Target (If Party Member)
        Item = 0x10,               // Pguid
        SourceLocation = 0x20,               // Pguid, 3 Float
        DestLocation = 0x40,               // Pguid, 3 Float
        UnitEnemy = 0x80,               // Not Sent, Used To Validate Target (If Enemy)
        UnitAlly = 0x100,               // Not Sent, Used To Validate Target (If Ally)
        CorpseEnemy = 0x200,               // Pguid
        UnitDead = 0x400,               // Not Sent, Used To Validate Target (If Dead Creature)
        Gameobject = 0x800,               // Pguid, Used With TargetGameobjectTarget
        TradeItem = 0x1000,               // Pguid
        String = 0x2000,               // String
        GameobjectItem = 0x4000,               // Not Sent, Used With TargetGameobjectItemTarget
        CorpseAlly = 0x8000,               // Pguid
        UnitMinipet = 0x10000,               // Pguid, Used To Validate Target (If Non Combat Pet)
        GlyphSlot = 0x20000,               // Used In Glyph Spells
        DestTarget = 0x40000,               // Sometimes Appears With DestTarget Spells (May Appear Or Not For A Given Spell)
        ExtraTargets = 0x80000,               // Uint32 Counter, Loop { Vec3 - Screen Position (?), Guid }, Not Used So Far
        UnitPassenger = 0x100000,               // Guessed, Used To Validate Target (If Vehicle Passenger)\
        Unk400000 = 0x400000,
        Unk1000000 = 0X01000000,
        Unk4000000 = 0X04000000,
        Unk10000000 = 0X10000000,
        Unk40000000 = 0X40000000,

        UnitMask = Unit | UnitRaid | UnitParty | UnitEnemy | UnitAlly | UnitDead | UnitMinipet | UnitPassenger,
        GameobjectMask = Gameobject | GameobjectItem,
        CorpseMask = CorpseAlly | CorpseEnemy,
        ItemMask = TradeItem | Item | GameobjectItem
    }

    public enum SpellFamilyNames
    {
        Generic = 0,
        Events = 1,                            // Events, Holidays
        // 2 - Unused
        Mage = 3,
        Warrior = 4,
        Warlock = 5,
        Priest = 6,
        Druid = 7,
        Rogue = 8,
        Hunter = 9,
        Paladin = 10,
        Shaman = 11,
        Unk12 = 12,                           // 2 Spells (Silence Resistance)
        Potion = 13,
        // 14 - Unused
        Deathknight = 15,
        // 16 - Unused
        Pet = 17,
        Totems = 50,
        Monk = 53,
        WarlockPet = 57,
        Unk66 = 66,
        Unk71 = 71,
        Unk78 = 78,
        Unk91 = 91,
        Unk100 = 100,
        DemonHunter = 107
    }

    [Flags]
    public enum TriggerCastFlags : uint
    {
        None = 0x0,   //! Not Triggered
        IgnoreGCD = 0x01,   //! Will Ignore Gcd
        IgnoreSpellAndCategoryCD = 0x02,   //! Will Ignore Spell And Category Cooldowns
        IgnorePowerAndReagentCost = 0x04,   //! Will Ignore Power And Reagent Cost
        IgnoreCastItem = 0x08,   //! Will Not Take Away Cast Item Or Update Related Achievement Criteria
        IgnoreAuraScaling = 0x10,   //! Will Ignore Aura Scaling
        IgnoreCastInProgress = 0x20,   //! Will Not Check If A Current Cast Is In Progress
        IgnoreComboPoints = 0x40,   //! Will Ignore Combo Point Requirement
        CastDirectly = 0x80,   //! In Spell.Prepare, Will Be Cast Directly Without Setting Containers For Executed Spell
        IgnoreAuraInterruptFlags = 0x100,   //! Will Ignore Interruptible Aura'S At Cast
        IgnoreSetFacing = 0x200,   //! Will Not Adjust Facing To Target (If Any)
        IgnoreShapeshift = 0x400,   //! Will Ignore Shapeshift Checks
        IgnoreCasterAurastate = 0x800,   //! Will Ignore Caster Aura States Including Combat Requirements And Death State
        DisallowProcEvents = 0x1000,   //! Disallows proc events from triggered spell (default)
        IgnoreCasterMountedOrOnVehicle = 0x2000,   //! Will Ignore Mounted/On Vehicle Restrictions
        // reuse                                        = 0x4000,
        // reuse                                        = 0x8000,
        IgnoreCasterAuras = 0x10000,   //! Will Ignore Caster Aura Restrictions Or Requirements
        DontResetPeriodicTimer = 0x20000,   //! Will allow periodic aura timers to keep ticking (instead of resetting)
        DontReportCastError = 0x40000,   //! Will Return SpellFailedDontReport In Checkcast Functions
        FullMask = 0x0007FFFF, //! Used when doing CastSpell with triggered == true

        // debug flags (used with .cast triggered commands)
        IgnoreEquippedItemRequirement = 0x80000, //! Will ignore equipped item requirements
        IgnoreTargetCheck = 0x100000, //! Will ignore most target checks (mostly DBC target checks)
        FullDebugMask = 0xFFFFFFFF
    }

    public enum SpellSchoolMask
    {
        None = 0x0,                       // Not Exist
        Normal = (1 << SpellSchools.Normal), // Physical (Armor)
        Holy = (1 << SpellSchools.Holy),
        Fire = (1 << SpellSchools.Fire),
        Nature = (1 << SpellSchools.Nature),
        Frost = (1 << SpellSchools.Frost),
        Shadow = (1 << SpellSchools.Shadow),
        Arcane = (1 << SpellSchools.Arcane),

        // 124, Not Include Normal And Holy Damage
        Spell = (Fire | Nature | Frost | Shadow | Arcane),
        // 126
        Magic = (Holy | Spell),

        // 127
        All = (Normal | Magic),
    }

    [Flags]
    public enum SpellCastFlags : uint
    {
        None = 0x0,
        Pending = 0x01,              // Aoe Combat Log?
        HasTrajectory = 0x02,
        Unk3 = 0x04,
        Unk4 = 0x08,              // Ignore Aoe Visual
        Unk5 = 0x10,
        Projectile = 0x20,
        Unk7 = 0x40,
        Unk8 = 0x80,
        Unk9 = 0x100,
        Unk10 = 0x200,
        Unk11 = 0x400,
        PowerLeftSelf = 0x800,
        Unk13 = 0x1000,
        Unk14 = 0x2000,
        Unk15 = 0x4000,
        Unk16 = 0x8000,
        Unk17 = 0x10000,
        AdjustMissile = 0x20000,
        NoGCD = 0x40000,
        VisualChain = 0x80000,
        Unk21 = 0x100000,
        RuneList = 0x200000,
        Unk23 = 0x400000,
        Unk24 = 0x800000,
        Unk25 = 0x1000000,
        Unk26 = 0x2000000,
        Immunity = 0x4000000,
        Unk28 = 0x8000000,
        Unk29 = 0x10000000,
        Unk30 = 0x20000000,
        HealPrediction = 0x40000000,
        Unk32 = 0x80000000
    }

    [System.Flags]
    public enum SpellCastFlagsEx
    {
        None = 0x0,
        Unknown1 = 0x01,
        Unknown2 = 0x02,
        Unknown3 = 0x04,
        Unknown4 = 0x08,
        Unknown5 = 0x10,
        Unknown6 = 0x20,
        Unknown7 = 0x40,
        Unknown8 = 0x80,
        Unknown9 = 0x100,
        Unknown10 = 0x200,
        Unknown11 = 0x400,
        Unknown12 = 0x800,
        Unknown13 = 0x1000,
        Unknown14 = 0x2000,
        Unknown15 = 0x4000,
        UseToySpell = 0x8000, // Starts Cooldown On Toy
        Unknown17 = 0x10000,
        Unknown18 = 0x20000,
        Unknown19 = 0x40000,
        Unknown20 = 0x80000
    }

    #region Spell Attributes
    public enum SpellAttr0 : uint
    {
        Unk0 = 0x01, //  0
        ReqAmmo = 0x02, //  1 On Next Ranged
        OnNextSwing = 0x04, //  2
        IsReplenishment = 0x08, //  3 Not Set In 3.0.3
        Ability = 0x10, //  4 Client Puts 'Ability' Instead Of 'Spell' In Game Strings For These Spells
        Tradespell = 0x20, //  5 Trade Spells (Recipes), Will Be Added By Client To A Sublist Of Profession Spell
        Passive = 0x40, //  6 Passive Spell
        HiddenClientside = 0x80, //  7 Spells With This Attribute Are Not Visible In Spellbook Or Aura Bar
        HideInCombatLog = 0x100, //  8 This Attribite Controls Whether Spell Appears In Combat Logs
        TargetMainhandItem = 0x200, //  9 Client Automatically Selects Item From Mainhand Slot As A Cast Target
        OnNextSwing2 = 0x400, // 10
        Unk11 = 0x800, // 11
        DaytimeOnly = 0x1000, // 12 Only Useable At Daytime, Not Set In 2.4.2
        NightOnly = 0x2000, // 13 Only Useable At Night, Not Set In 2.4.2
        IndoorsOnly = 0x4000, // 14 Only Useable Indoors, Not Set In 2.4.2
        OutdoorsOnly = 0x8000, // 15 Only Useable Outdoors.
        NotShapeshift = 0x10000, // 16 Not While Shapeshifted
        OnlyStealthed = 0x20000, // 17 Must Be In Stealth
        DontAffectSheathState = 0x40000, // 18 Client Won'T Hide Unit Weapons In Sheath On Cast/Channel
        LevelDamageCalculation = 0x80000, // 19 Spelldamage Depends On Caster Level
        StopAttackTarget = 0x100000, // 20 Stop Attack After Use This Spell (And Not Begin Attack If Use)
        ImpossibleDodgeParryBlock = 0x200000, // 21 Cannot Be Dodged/Parried/Blocked
        CastTrackTarget = 0x400000, // 22 Client Automatically Forces Player To Face Target When Casting
        CastableWhileDead = 0x800000, // 23 Castable While Dead?
        CastableWhileMounted = 0x1000000, // 24 Castable While Mounted
        DisabledWhileActive = 0x2000000, // 25 Activate And Start Cooldown After Aura Fade Or Remove Summoned Creature Or Go
        Negative1 = 0x4000000, // 26 Many Negative Spells Have This Attr
        CastableWhileSitting = 0x8000000, // 27 Castable While Sitting
        CantUsedInCombat = 0x10000000, // 28 Cannot Be Used In Combat
        UnaffectedByInvulnerability = 0x20000000, // 29 Unaffected By Invulnerability (Hmm Possible Not...)
        HeartResistCheck = 0x40000000, // 30
        CantCancel = 0x80000000,  // 31 Positive Aura Can'T Be Canceled
    }
    public enum SpellAttr1 : uint
    {
        DismissPet = 0x01, //  0 For Spells Without This Flag Client Doesn'T Allow To Summon Pet If Caster Has A Pet
        DrainAllPower = 0x02, //  1 Use All Power (Only Paladin Lay Of Hands And Bunyanize)
        Channeled1 = 0x04, //  2 Clientside Checked? Cancelable?
        CantBeRedirected = 0x08, //  3
        Unk4 = 0x10, //  4 Stealth And Whirlwind
        NotBreakStealth = 0x20, //  5 Not Break Stealth
        Channeled2 = 0x40, //  6
        CantBeReflected = 0x80, //  7
        CantTargetInCombat = 0x100, //  8 Can Target Only Out Of Combat Units
        MeleeCombatStart = 0x200, //  9 Player Starts Melee Combat After This Spell Is Cast
        NoThreat = 0x400, // 10 No Generates Threat On Cast 100% (Old NoInitialAggro)
        DontRefreshDurationOnRecast = 0x800, // 11 aura will not refresh its duration when recast
        IsPickpocket = 0x1000, // 12 Pickpocket
        Farsight = 0x2000, // 13 Client Removes Farsight On Aura Loss
        ChannelTrackTarget = 0x4000, // 14 Client Automatically Forces Player To Face Target When Channeling
        DispelAurasOnImmunity = 0x8000, // 15 Remove Auras On Immunity
        UnaffectedBySchoolImmune = 0x10000, // 16 On Immuniy
        UnautocastableByPet = 0x20000, // 17
        Unk18 = 0x40000, // 18 Stun, Polymorph, Daze, Hex
        CantTargetSelf = 0x80000, // 19
        ReqComboPoints1 = 0x100000, // 20 Req Combo Points On Target
        Unk21 = 0x200000, // 21
        ReqComboPoints2 = 0x400000, // 22 Req Combo Points On Target
        Unk23 = 0x800000, // 23
        IsFishing = 0x1000000, // 24 Only Fishing Spells
        Unk25 = 0x2000000, // 25
        Unk26 = 0x4000000, // 26 Works Correctly With [Target=Focus] And [Target=Mouseover] Macros?
        Unk27 = 0x8000000, // 27 Melee Spell?
        DontDisplayInAuraBar = 0x10000000, // 28 Client Doesn'T Display These Spells In Aura Bar
        ChannelDisplaySpellName = 0x20000000, // 29 Spell Name Is Displayed In Cast Bar Instead Of 'Channeling' Text
        EnableAtDodge = 0x40000000, // 30 Overpower
        Unk31 = 0x80000000  // 31
    }
    public enum SpellAttr2 : uint
    {
        CanTargetDead = 0x01, //  0 Can Target Dead Unit Or Corpse
        Unk1 = 0x02, //  1 Vanish, Shadowform, Ghost Wolf And Other
        CanTargetNotInLos = 0x04, //  2 26368 4.0.1 Dbc Change
        Unk3 = 0x08, //  3
        DisplayInStanceBar = 0x10, //  4 Client Displays Icon In Stance Bar When Learned, Even If Not Shapeshift
        AutorepeatFlag = 0x20, //  5
        CantTargetTapped = 0x40, //  6 Target Must Be Tapped By Caster
        Unk7 = 0x80, //  7
        Unk8 = 0x100, //  8 Not Set In 3.0.3
        Unk9 = 0x200, //  9
        Unk10 = 0x400, // 10 Related To Tame
        HealthFunnel = 0x800, // 11
        Unk12 = 0x1000, // 12 Cleave, Heart Strike, Maul, Sunder Armor, Swipe
        PreserveEnchantInArena = 0x2000, // 13 Items Enchanted By Spells With This Flag Preserve The Enchant To Arenas
        Unk14 = 0x4000, // 14
        Unk15 = 0x8000, // 15 Not Set In 3.0.3
        TameBeast = 0x10000, // 16
        NotResetAutoActions = 0x20000, // 17 Don'T Reset Timers For Melee Autoattacks (Swings) Or Ranged Autoattacks (Autoshoots)
        ReqDeadPet = 0x40000, // 18 Only Revive Pet And Heart Of The Pheonix
        NotNeedShapeshift = 0x80000, // 19 Does Not Necessarly Need Shapeshift
        Unk20 = 0x100000, // 20
        DamageReducedShield = 0x200000, // 21 For Ice Blocks, Pala Immunity Buffs, Priest Absorb Shields, But Used Also For Other Spells . Not Sure!
        Unk22 = 0x400000, // 22 Ambush, Backstab, Cheap Shot, Death Grip, Garrote, Judgements, Mutilate, Pounce, Ravage, Shiv, Shred
        IsArcaneConcentration = 0x800000, // 23 Only Mage Arcane Concentration Have This Flag
        Unk24 = 0x1000000, // 24
        Unk25 = 0x2000000, // 25
        UnaffectedByAuraSchoolImmune = 0x4000000, // 26 Unaffected By School Immunity
        Unk27 = 0x8000000, // 27
        IgnoreActionAuraInterruptFlags = 0x10000000, // 28 doesnt break auras with SpellAuraInterruptFlags::Action and SpellAuraInterruptFlags::ActionDelayed
        CantCrit = 0x20000000, // 29 Spell Can'T Crit
        TriggeredCanTriggerProc = 0x40000000, // 30 Spell Can Trigger Even If Triggered
        FoodBuff = 0x80000000  // 31 Food Or Drink Buff (Like Well Fed)
    }
    public enum SpellAttr3 : uint
    {
        Unk0 = 0x01, //  0
        NoProcEquipRequirement = 0x02, //  1 Ignores subclass mask check when checking proc
        Unk2 = 0x04, //  2
        BlockableSpell = 0x08, //  3 Only Dmg Class Melee In 3.1.3
        IgnoreResurrectionTimer = 0x10, //  4 You Don'T Have To Wait To Be Resurrected With These Spells
        Unk5 = 0x20, //  5
        Unk6 = 0x40, //  6
        StackForDiffCasters = 0x80, //  7 Separate Stack For Every Caster
        OnlyTargetPlayers = 0x100, //  8 Can Only Target Players
        TriggeredCanTriggerProc2 = 0x200, //  9 Triggered From Effect?
        MainHand = 0x400, // 10 Main Hand Weapon Required
        Battleground = 0x800, // 11 Can Casted Only On Battleground
        OnlyTargetGhosts = 0x1000, // 12
        DontDisplayChannelBar = 0x2000, // 13 Clientside attribute - will not display channeling bar
        IsHonorlessTarget = 0x4000, // 14 "Honorless Target" Only This Spells Have This Flag
        Unk15 = 0x8000, // 15 Auto Shoot, Shoot, Throw,  - This Is Autoshot Flag
        CantTriggerProc = 0x10000, // 16 Confirmed With Many Patchnotes
        NoInitialAggro = 0x20000, // 17 Soothe Animal, 39758, Mind Soothe
        IgnoreHitResult = 0x40000, // 18 Spell Should Always Hit Its Target
        DisableProc = 0x80000, // 19 During Aura Proc No Spells Can Trigger (20178, 20375)
        DeathPersistent = 0x100000, // 20 Death Persistent Spells
        Unk21 = 0x200000, // 21 Unused
        ReqWand = 0x400000, // 22 Req Wand
        Unk23 = 0x800000, // 23
        ReqOffhand = 0x1000000, // 24 Req Offhand Weapon
        TreatAsPeriodic = 0x2000000, // 25 Makes the spell appear as periodic in client combat logs - used by spells that trigger another spell on each tick
        CanProcWithTriggered = 0x4000000, // 26 Auras With This Attribute Can Proc From Triggered Spell Casts With TriggeredCanTriggerProc2 (67736 + 52999)
        DrainSoul = 0x8000000, // 27 Only Drain Soul Has This Flag
        Unk28 = 0x10000000, // 28
        NoDoneBonus = 0x20000000, // 29 Ignore Caster Spellpower And Done Damage Mods?  Client Doesn'T Apply Spellmods For Those Spells
        DontDisplayRange = 0x40000000, // 30 Client Doesn'T Display Range In Tooltip For Those Spells
        Unk31 = 0x80000000  // 31
    }
    public enum SpellAttr4 : uint
    {
        IgnoreResistances = 0x01, //  0 Spells With This Attribute Will Completely Ignore The Target'S Resistance (These Spells Can'T Be Resisted)
        ProcOnlyOnCaster = 0x02, //  1 Proc Only On Effects With TargetUnitCaster?
        AuraExpiresOffline = 0x04, //  2 duration is removed from aura while player is logged out
        Unk3 = 0x08, //  3
        Unk4 = 0x10, //  4 This Will No Longer Cause Guards To Attack On Use??
        Unk5 = 0x20, //  5
        NotStealable = 0x40, //  6 Although Such Auras Might Be Dispellable, They Cannot Be Stolen
        CanCastWhileCasting = 0x80, //  7 Can be cast while another cast is in progress - see CanCastWhileCasting(SpellRec const*,CGUnit_C *,int &)
        FixedDamage = 0x100, //  8 Ignores Taken Percent Damage Mods?
        TriggerActivate = 0x200, //  9 Initially Disabled / Trigger Activate From Event (Execute, Riposte, Deep Freeze End Other)
        SpellVsExtendCost = 0x400, // 10 Rogue Shiv Have This Flag
        Unk11 = 0x800, // 11
        Unk12 = 0x1000, // 12
        CombatLogNoCaster = 0x2000, // 13 No caster object is sent to client combat log
        DamageDoesntBreakAuras = 0x4000, // 14 Doesn'T Break Auras By Damage From These Spells
        HiddenInSpellbook = 0x8000, // 15
        NotUsableInArenaOrRatedBg = 0x10000, // 16 Cannot Be Used In Both Arenas Or Rated Battlegrounds
        UsableInArena = 0x20000, // 17
        AreaTargetChain = 0x40000, // 18 (Nyi)Hits Area Targets One After Another Instead Of All At Once
        Unk19 = 0x80000, // 19 Proc Dalayed, After Damage Or Don'T Proc On Absorb?
        NotCheckSelfcastPower = 0x100000, // 20 Supersedes Message "More Powerful Spell Applied" For Self Casts.
        DontRemoveInArena = 0x200000, // 21 Pally Aura, Dk Presence, Dudu Form, Warrior Stance, Shadowform, Hunter Track
        Unk22 = 0x400000, // 22 Seal Of Command (42058,57770) And Gymer'S Smash 55426
        SuppressWeaponProcs = 0x800000, // 23 spells with this flag should not trigger item spells / enchants (mostly in conjunction with SPELL_ATTR0_STOP_ATTACK_TARGET)
        Unk24 = 0x1000000, // 24 Some Shoot Spell
        IsPetScaling = 0x2000000, // 25 Pet Scaling Auras
        CastOnlyInOutland = 0x4000000, // 26 Can Only Be Used In Outland.
        Unk27 = 0x8000000, // 27
        Unk28 = 0x10000000, // 28 Aimed Shot
        Unk29 = 0x20000000, // 29
        Unk30 = 0x40000000, // 30
        Unk31 = 0x80000000  // 31 Polymorph (Chicken) 228 And Sonic Boom (38052,38488)
    }
    public enum SpellAttr5 : uint
    {
        CanChannelWhenMoving = 0x01, //  0 available casting channel spell when moving
        NoReagentWhilePrep = 0x02, //  1 Not Need Reagents If UnitFlagPreparation
        RemoveEnteringArena = 0x04, //  2 remove this aura on arena enter
        UsableWhileStunned = 0x08, //  3 Usable While Stunned
        Unk4 = 0x10, //  4
        SingleTargetSpell = 0x20, //  5 Only One Target Can Be Apply At A Time
        Unk6 = 0x40, //  6
        Unk7 = 0x80, //  7
        CantTargetPlayerControlled = 0x100, //  8 cannot target player controlled units but can target players
        StartPeriodicAtApply = 0x200, //  9 Begin Periodic Tick At Aura Apply
        HideDuration = 0x400, // 10 Do Not Send Duration To Client
        AllowTargetOfTargetAsTarget = 0x800, // 11 (Nyi) Uses Target'S Target As Target If Original Target Not Valid (Intervene For Example)
        Unk12 = 0x1000, // 12 Cleave Related?
        HasteAffectDuration = 0x2000, // 13 Haste Effects Decrease Duration Of This
        NotUsableWhileCharmed = 0x4000, // 14 Charmed units cannot cast this spell
        Unk15 = 0x8000, // 15 Inflits On Multiple Targets?
        Unk16 = 0x10000, // 16
        UsableWhileFeared = 0x20000, // 17 Usable While Feared
        UsableWhileConfused = 0x40000, // 18 Usable While Confused
        DontTurnDuringCast = 0x80000, // 19 Blocks Caster'S Turning When Casting (Client Does Not Automatically Turn Caster'S Model To Face UnitFieldTarget)
        Unk20 = 0x100000, // 20
        Unk21 = 0x200000, // 21
        Unk22 = 0x400000, // 22
        Unk23 = 0x800000, // 23
        Unk24 = 0x1000000, // 24
        Unk25 = 0x2000000, // 25
        AlwaysAoeLineOfSight = 0x4000000, // 26 Aoe Related - Boulder, Cannon, Corpse Explosion, Fire Nova, Flames, Frost Bomb, Living Bomb, Seed Of Corruption, Starfall, Thunder Clap, Volley
        DontShowAuraIfSelfCast = 0x8000000, // 27
        DontShowAuraIfNotSelfCast = 0x10000000, // 28
        Unk29 = 0x20000000, // 29
        Unk30 = 0x40000000, // 30
        Unk31 = 0x80000000  // 31 Forces All Nearby Enemies To Focus Attacks Caster
    }
    public enum SpellAttr6 : uint
    {
        DontDisplayCooldown = 0x01, //  0 Client Doesn'T Display Cooldown In Tooltip For These Spells
        OnlyInArena = 0x02, //  1 Only Usable In Arena
        IgnoreCasterAuras = 0x04, //  2
        AssistIgnoreImmuneFlag = 0x08, //  3
        Unk4 = 0x10, //  4
        DoNotConsumeResources = 0x20, //  5 dont consume proc charges
        UseSpellCastEvent = 0x40, //  6
        Unk7 = 0x80, //  7
        CantTargetCrowdControlled = 0x100, //  8
        Unk9 = 0x200, //  9
        CanTargetPossessedFriends = 0x400, // 10 Nyi!
        NotInRaidInstance = 0x800, // 11 Not Usable In Raid Instance
        CastableWhileOnVehicle = 0x1000, // 12 Castable While Caster Is On Vehicle
        CanTargetInvisible = 0x2000, // 13 Ignore Visibility Requirement For Spell Target (Phases, Invisibility, Etc.)
        Unk14 = 0x4000, // 14
        Unk15 = 0x8000, // 15 Only 54368, 67892
        Unk16 = 0x10000, // 16
        Unk17 = 0x20000, // 17 Mount Spell
        CastByCharmer = 0x40000, // 18 Client Won'T Allow To Cast These Spells When Unit Is Not Possessed && Charmer Of Caster Will Be Original Caster
        Unk19 = 0x80000, // 19 Only 47488, 50782
        OnlyVisibleToCaster = 0x100000, // 20 Only 58371, 62218
        ClientUiTargetEffects = 0x200000, // 21 It'S Only Client-Side Attribute
        Unk22 = 0x400000, // 22 Only 72054
        Unk23 = 0x800000, // 23
        CanTargetUntargetable = 0x1000000, // 24
        NotResetSwingIfInstant = 0x2000000, // 25 Exorcism, Flash Of Light
        Unk26 = 0x4000000, // 26 Related To Player Castable Positive Buff
        IgnoreHealingModifiers = 0x8000000, // 27 some custom rules - complicated
        Unk28 = 0x10000000, // 28 Death Grip
        IgnoreCasterDamageModifiers = 0x20000000, // 29 ignores done percent damage mods? some custom rules - complicated
        Unk30 = 0x40000000, // 30
        IgnoreCategoryCooldownMods = 0x80000000  // 31 Some Special Cooldown Calc? Only 2894
    }
    public enum SpellAttr7 : uint
    {
        Unk0 = 0x01, //  0 Shaman'S New Spells (Call Of The ...), Feign Death.
        IgnoreDurationMods = 0x02, //  1 Duration is not affected by duration modifiers
        ReactivateAtResurrect = 0x04, //  2 Paladin'S Auras And 65607 Only.
        IsCheatSpell = 0x08, //  3 Cannot Cast If Caster Doesn'T Have Unitflag2 & UnitFlag2AllowCheatSpells
        Unk4 = 0x10, //  4 Only 47883 (Soulstone Resurrection) And Test Spell.
        SummonTotem = 0x20, //  5 Only Shaman Player Totems.
        NoPushbackOnDamage = 0x40, //  6 Does not cause spell pushback on damage
        Unk7 = 0x80, //  7 66218 (Launch) Spell.
        HordeOnly = 0x100, //  8 Teleports, Mounts And Other Spells.
        AllianceOnly = 0x200, //  9 Teleports, Mounts And Other Spells.
        DispelCharges = 0x400, // 10 Dispel And Spellsteal Individual Charges Instead Of Whole Aura.
        InterruptOnlyNonplayer = 0x800, // 11 Only Non-Player Casts Interrupt, Though Feral Charge - Bear Has It.
        SilenceOnlyNonplayer = 0x1000, // 12 Not Set In 3.2.2a.
        CanAlwaysBeInterrupted = 0x2000, // 13 Can always be interrupted, even if caster is immune
        Unk14 = 0x4000, // 14 Only 52150 (Raise Dead - Pet) Spell.
        Unk15 = 0x8000, // 15 Exorcism. Usable On Players? 100% Crit Chance On Undead And Demons?
        HiddenInSpellbookWhenLearned = 0x10000, // 16 After learning these spells become hidden in spellbook (but are visible when not learned for low level characters)
        Unk17 = 0x20000, // 17 Only 27965 (Suicide) Spell.
        HasChargeEffect = 0x40000, // 18 Only Spells That Have Charge Among Effects.
        ZoneTeleport = 0x80000, // 19 Teleports To Specific Zones.
        Unk20 = 0x100000, // 20 Blink, Divine Shield, Ice Block
        Unk21 = 0x200000, // 21 Not Set
        Unk22 = 0x400000, // 22
        Unk23 = 0x800000, // 23 Motivate, Mutilate, Shattering Throw
        Unk24 = 0x1000000, // 24 Motivate, Mutilate, Perform Speech, Shattering Throw
        Unk25 = 0x2000000, // 25
        Unk26 = 0x4000000, // 26
        Unk27 = 0x8000000, // 27 Not Set
        ConsolidatedRaidBuff = 0x10000000, // 28 Related To Player Positive Buff
        Unk29 = 0x20000000, // 29 Only 69028, 71237
        Unk30 = 0x40000000, // 30 Burning Determination, Divine Sacrifice, Earth Shield, Prayer Of Mending
        ClientIndicator = 0x80000000  // 31 Only 70769
    }
    public enum SpellAttr8 : uint
    {
        CantMiss = 0x01, // 0
        Unk1 = 0x02, // 1
        Unk2 = 0x04, // 2
        Unk3 = 0x08, // 3
        Unk4 = 0x10, // 4
        Unk5 = 0x20, // 5
        Unk6 = 0x40, // 6
        Unk7 = 0x80, // 7
        AffectPartyAndRaid = 0x100, // 8
        DontResetPeriodicTimer = 0x200, // 9 Periodic Auras With This Flag Keep Old Periodic Timer When Refreshing At Close To One Tick Remaining (Kind Of Anti Dot Clipping)
        NameChangedDuringTransofrm = 0x400, // 10
        Unk11 = 0x800, // 11
        AuraSendAmount = 0x1000, // 12 Aura Must Have Flag AflagAnyEffectAmountSent To Send Amount
        Unk13 = 0x2000, // 13
        Unk14 = 0x4000, // 14
        WaterMount = 0x8000, // 15
        Unk16 = 0x10000, // 16
        Unk17 = 0x20000, // 17
        RememberSpells = 0x40000, // 18
        UseComboPointsOnAnyTarget = 0x80000, // 19
        ArmorSpecialization = 0x100000, // 20
        Unk21 = 0x200000, // 21
        Unk22 = 0x400000, // 22
        BattleResurrection = 0x800000, // 23
        HealingSpell = 0x1000000, // 24
        Unk25 = 0x2000000, // 25
        RaidMarker = 0x4000000, // 26 Probably Spell No Need Learn To Cast
        Unk27 = 0x8000000, // 27
        NotInBgOrArena = 0x10000000, // 28
        MasterySpecialization = 0x20000000, // 29
        Unk30 = 0x40000000, // 30
        AttackIgnoreImmuneToPCFlag = 0x80000000  // 31
    }
    public enum SpellAttr9 : uint
    {
        Unk0 = 0x01, // 0
        Unk1 = 0x02, // 1
        RestrictedFlightArea = 0x04, // 2
        Unk3 = 0x08, // 3
        SpecialDelayCalculation = 0x10, // 4
        SummonPlayerTotem = 0x20, // 5
        Unk6 = 0x40, // 6
        Unk7 = 0x80, // 7
        AimedShot = 0x100, // 8
        NotUsableInArena = 0x200, // 9 Cannot Be Used In Arenas
        Unk10 = 0x400, // 10
        Unk11 = 0x800, // 11
        Unk12 = 0x1000, // 12
        Slam = 0x2000, // 13
        UsableInRatedBattlegrounds = 0x4000, // 14 Can Be Used In Rated Battlegrounds
        Unk15 = 0x8000, // 15
        Unk16 = 0x10000, // 16
        Unk17 = 0x20000, // 17
        Unk18 = 0x40000, // 18
        Unk19 = 0x80000, // 19
        Unk20 = 0x100000, // 20
        Unk21 = 0x200000, // 21
        Unk22 = 0x400000, // 22
        Unk23 = 0x800000, // 23
        Unk24 = 0x1000000, // 24
        Unk25 = 0x2000000, // 25
        Unk26 = 0x4000000, // 26
        Unk27 = 0x8000000, // 27
        Unk28 = 0x10000000, // 28
        Unk29 = 0x20000000, // 29
        Unk30 = 0x40000000, // 30
        Unk31 = 0x80000000  // 31
    }
    public enum SpellAttr10 : uint
    {
        Unk0 = 0x01, // 0
        Unk1 = 0x02, // 1
        UsesRangedSlotCosmeticOnly = 0x04, // 2
        Unk3 = 0x08, // 3
        WaterSpout = 0x10, // 4
        Unk5 = 0x20, // 5
        Unk6 = 0x40, // 6
        TeleportPlayer = 0x80, // 7
        Unk8 = 0x100, // 8
        Unk9 = 0x200, // 9
        Unk10 = 0x400, // 10
        HerbGatheringMining = 0x800, // 11
        UseSpellBaseLevelForScaling = 0x1000, // 12
        ResetCooldownOnEncounterEnd = 0x2000, // 13
        RollingPeriodic = 0x4000, // 14
        Unk15 = 0x8000, // 15
        Unk16 = 0x10000, // 16
        CanDodgeParryWhileCasting = 0x20000, // 17
        Unk18 = 0x40000, // 18
        Unk19 = 0x80000, // 19
        Unk20 = 0x100000, // 20
        Unk21 = 0x200000, // 21
        Unk22 = 0x400000, // 22
        Unk23 = 0x800000, // 23
        Unk24 = 0x1000000, // 24
        Unk25 = 0x2000000, // 25
        Unk26 = 0x4000000, // 26
        Unk27 = 0x8000000, // 27
        Unk28 = 0x10000000, // 28
        MountIsNotAccountWide = 0x20000000, // 29 This mount is stored per-character
        Unk30 = 0x40000000, // 30
        Unk31 = 0x80000000  // 31
    }
    public enum SpellAttr11 : uint
    {
        Unk0 = 0x01, //  0
        Unk1 = 0x02, //  1
        ScalesWithItemLevel = 0x04, //  2
        Unk3 = 0x08, //  3
        Unk4 = 0x10, //  4
        AbsorbEnvironmentalDamage = 0x20, //  5
        Unk6 = 0x40, //  6
        RankIgnoresCasterLevel = 0x80, //  7 Spell_C_GetSpellRank returns SpellLevels.MaxLevel * 5 instead of std::min(SpellLevels.MaxLevel, caster.Level) * 5
        Unk8 = 0x100, //  8
        Unk9 = 0x200, //  9
        Unk10 = 0x400, // 10
        NotUsableInInstances = 0x800, // 11
        Unk12 = 0x1000, // 12
        Unk13 = 0x2000, // 13
        Unk14 = 0x4000, // 14
        Unk15 = 0x8000, // 15
        NotUsableInChallengeMode = 0x10000, // 16
        Unk17 = 0x20000, // 17
        Unk18 = 0x40000, // 18
        Unk19 = 0x80000, // 19
        Unk20 = 0x100000, // 20
        Unk21 = 0x200000, // 21
        Unk22 = 0x400000, // 22
        Unk23 = 0x800000, // 23
        Unk24 = 0x1000000, // 24
        Unk25 = 0x2000000, // 25
        Unk26 = 0x4000000, // 26
        Unk27 = 0x8000000, // 27
        Unk28 = 0x10000000, // 28
        Unk29 = 0x20000000, // 29
        Unk30 = 0x40000000, // 30
        Unk31 = 0x80000000  // 31
    }
    public enum SpellAttr12 : uint
    {
        Unk0 = 0x01, //  0
        Unk1 = 0x02, //  1
        Unk2 = 0x04, //  2
        Unk3 = 0x08, //  3
        Unk4 = 0x10, //  4
        Unk5 = 0x20, //  5
        Unk6 = 0x40, //  6
        Unk7 = 0x80, //  7
        Unk8 = 0x100, //  8
        IgnoreCastingDisabled = 0x200, //  9
        Unk10 = 0x400, // 10
        Unk11 = 0x800, // 11
        Unk12 = 0x1000, // 12
        Unk13 = 0x2000, // 13
        Unk14 = 0x4000, // 14
        Unk15 = 0x8000, // 15
        Unk16 = 0x10000, // 16
        Unk17 = 0x20000, // 17
        Unk18 = 0x40000, // 18
        Unk19 = 0x80000, // 19
        Unk20 = 0x100000, // 20
        Unk21 = 0x200000, // 21
        Unk22 = 0x400000, // 22
        StartCooldownOnCastStart = 0x800000, // 23
        IsGarrisonBuff = 0x1000000, // 24
        Unk25 = 0x2000000, // 25
        Unk26 = 0x4000000, // 26
        IsReadinessSpell = 0x8000000, // 27
        Unk28 = 0x10000000, // 28
        Unk29 = 0x20000000, // 29
        Unk30 = 0x40000000, // 30
        Unk31 = 0x80000000  // 31
    }
    public enum SpellAttr13 : uint
    {
        Unk0 = 0x01, //  0
        Unk1 = 0x02, //  1
        PassiveIsUpgrade = 0x04, //  2 Displays "Upgrade" in spell tooltip instead of "Passive"
        Unk3 = 0x08, //  3
        Unk4 = 0x10, //  4
        Unk5 = 0x20, //  5
        Unk6 = 0x40, //  6
        Unk7 = 0x80, //  7
        Unk8 = 0x100, //  8
        Unk9 = 0x200, //  9
        Unk10 = 0x400, // 10
        Unk11 = 0x800, // 11
        Unk12 = 0x1000, // 12
        Unk13 = 0x2000, // 13
        Unk14 = 0x4000, // 14
        Unk15 = 0x8000, // 15
        Unk16 = 0x10000, // 16
        Unk17 = 0x20000, // 17
        ActivatesRequiredShapeshift = 0x40000, // 18
        Unk19 = 0x80000, // 19
        Unk20 = 0x100000, // 20
        Unk21 = 0x200000, // 21
        Unk22 = 0x400000, // 22
        Unk23 = 0x800000, // 23
        Unk24 = 0x01000000, // 24
        Unk25 = 0x02000000, // 25
        Unk26 = 0x04000000, // 26
        Unk27 = 0x08000000, // 27
        Unk28 = 0x10000000, // 28
        Unk29 = 0x20000000, // 29
        Unk30 = 0x40000000, // 30
        Unk31 = 0x80000000  // 31
    }
    public enum SpellAttr14 : uint
    {
        Unk0 = 0x01, //  0
        ReagentCostConsumesCharges = 0x02, //  1 Consumes item charges for reagent costs instead of whole items
        Unk2 = 0x04, //  2
        HidePassiveFromTooltip = 0x08, //  3 Don't show "Passive" or "Upgrade" in tooltip
        Unk4 = 0x10, //  4
        Unk5 = 0x20, //  5
        Unk6 = 0x40, //  6
        Unk7 = 0x80, //  7
        Unk8 = 0x100, //  8
        Unk9 = 0x200, //  9
        Unk10 = 0x400, // 10
        Unk11 = 0x800, // 11
        Unk12 = 0x1000, // 12
        Unk13 = 0x2000, // 13
        Unk14 = 0x4000, // 14
        Unk15 = 0x8000, // 15
        Unk16 = 0x10000, // 16
        Unk17 = 0x20000, // 17
        Unk18 = 0x40000, // 18
        Unk19 = 0x80000, // 19
        Unk20 = 0x100000, // 20
        Unk21 = 0x200000, // 21
        Unk22 = 0x400000, // 22
        Unk23 = 0x800000, // 23
        Unk24 = 0x1000000, // 24
        Unk25 = 0x2000000, // 25
        Unk26 = 0x4000000, // 26
        Unk27 = 0x8000000, // 27
        Unk28 = 0x10000000, // 28
        Unk29 = 0x20000000, // 29
        Unk30 = 0x40000000, // 30
        Unk31 = 0x80000000  // 31
    }
    public enum SpellCustomAttributes
    {
        EnchantProc = 0x01,
        ConeBack = 0x02,
        ConeLine = 0x04,
        ShareDamage = 0x08,
        NoInitialThreat = 0x10,
        AuraCC = 0x20,
        DontBreakStealth = 0x40,
        CanCrit = 0x80,
        DirectDamage = 0x100,
        Charge = 0x200,
        PickPocket = 0x400,
        DeprecatedRollingPeriodic = 0x800, // DO NOT REUSE
        DeprecatedNegativeEff0 = 0x1000, // DO NOT REUSE
        DeprecatedNegativeEff1 = 0x2000, // DO NOT REUSE
        DeprecatedNegativeEff2 = 0x4000, // DO NOT REUSE
        IgnoreArmor = 0x8000,
        ReqTargetFacingCaster = 0x10000,
        ReqCasterBehindTarget = 0x20000,
        AllowInflightTarget = 0x40000,
        NeedsAmmoData = 0x80000,
        BinarySpell = 0x100000,
        SchoolmaskNormalWithMagic = 0x200000,
        DeprecatedLiquidAura = 0x400000,
        IsTalent = 0x800000,
        AuraCannotBeSaved = 0x1000000
    }
    #endregion

    //Effects
    public enum SpellEffectName
    {
        Any = -1,
        None = 0,
        Instakill = 1,
        SchoolDamage = 2,
        Dummy = 3,
        PortalTeleport = 4, // Unused (4.3.4)
        Effect5 = 5,
        ApplyAura = 6,
        EnvironmentalDamage = 7,
        PowerDrain = 8,
        HealthLeech = 9,
        Heal = 10,
        Bind = 11,
        Portal = 12,
        TeleportToReturnPoint = 13, // MiscValueA = spellid of the aura holding destination
        IncreseCurrencyCap = 14,
        TeleportWithSpellVisualKitLoadingScreen = 15, // MiscValueA = delay, MiscValueB = SpellVisualKitId
        QuestComplete = 16,
        WeaponDamageNoSchool = 17,
        Resurrect = 18,
        AddExtraAttacks = 19,
        Dodge = 20,
        Evade = 21,
        Parry = 22,
        Block = 23,
        CreateItem = 24,
        Weapon = 25,
        Defense = 26,
        PersistentAreaAura = 27,
        Summon = 28,
        Leap = 29,
        Energize = 30,
        WeaponPercentDamage = 31,
        TriggerMissile = 32,
        OpenLock = 33,
        SummonChangeItem = 34,
        ApplyAreaAuraParty = 35,
        LearnSpell = 36,
        SpellDefense = 37,
        Dispel = 38,
        Language = 39,
        DualWield = 40,
        Jump = 41,
        JumpDest = 42,
        TeleportUnitsFaceCaster = 43,
        SkillStep = 44,
        PlayMovie = 45,
        Spawn = 46,
        TradeSkill = 47,
        Stealth = 48,
        Detect = 49,
        TransDoor = 50,
        ForceCriticalHit = 51, // Unused (4.3.4)
        SetMaxBattlePetCount = 52,
        EnchantItem = 53,
        EnchantItemTemporary = 54,
        Tamecreature = 55,
        SummonPet = 56,
        LearnPetSpell = 57,
        WeaponDamage = 58,
        CreateRandomItem = 59,
        Proficiency = 60,
        SendEvent = 61,
        PowerBurn = 62,
        Threat = 63,
        TriggerSpell = 64,
        ApplyAreaAuraRaid = 65,
        RechargeItem = 66,
        HealMaxHealth = 67,
        InterruptCast = 68,
        Distract = 69,
        CompleteAndRewardWorldQuest = 70,
        Pickpocket = 71,
        AddFarsight = 72,
        UntrainTalents = 73,
        ApplyGlyph = 74,
        HealMechanical = 75,
        SummonObjectWild = 76,
        ScriptEffect = 77,
        Attack = 78,
        Sanctuary = 79,
        ModifyFollowerItemLevel = 80,
        PushAbilityToActionBar = 81,
        BindSight = 82,
        Duel = 83,
        Stuck = 84,
        SummonPlayer = 85,
        ActivateObject = 86,
        GameObjectDamage = 87,
        GameobjectRepair = 88,
        GameobjectSetDestructionState = 89,
        KillCredit = 90,
        ThreatAll = 91,
        EnchantHeldItem = 92,
        ForceDeselect = 93,
        SelfResurrect = 94,
        Skinning = 95,
        Charge = 96,
        CastButton = 97,
        KnockBack = 98,
        Disenchant = 99,
        Inebriate = 100,
        FeedPet = 101,
        DismissPet = 102,
        Reputation = 103,
        SummonObjectSlot1 = 104,
        Survey = 105,
        ChangeRaidMarker = 106,
        ShowCorpseLoot = 107,
        DispelMechanic = 108,
        ResurrectPet = 109,
        DestroyAllTotems = 110,
        DurabilityDamage = 111,
        Effect112 = 112,
        CancelConversation = 113,
        AttackMe = 114,
        DurabilityDamagePct = 115,
        SkinPlayerCorpse = 116,
        SpiritHeal = 117,
        Skill = 118,
        ApplyAreaAuraPet = 119,
        TeleportGraveyard = 120,
        NormalizedWeaponDmg = 121,
        Effect122 = 122,
        SendTaxi = 123,
        PullTowards = 124,
        ModifyThreatPercent = 125,
        StealBeneficialBuff = 126,
        Prospecting = 127,
        ApplyAreaAuraFriend = 128,
        ApplyAreaAuraEnemy = 129,
        RedirectThreat = 130,
        PlaySound = 131,
        PlayMusic = 132,
        UnlearnSpecialization = 133,
        KillCredit2 = 134,
        CallPet = 135,
        HealPct = 136,
        EnergizePct = 137,
        LeapBack = 138,
        ClearQuest = 139,
        ForceCast = 140,
        ForceCastWithValue = 141,
        TriggerSpellWithValue = 142,
        ApplyAreaAuraOwner = 143,
        KnockBackDest = 144,
        PullTowardsDest = 145,
        RestoreGarrisonTroopVitality = 146,
        QuestFail = 147,
        TriggerMissileSpellWithValue = 148,
        ChargeDest = 149,
        QuestStart = 150,
        TriggerSpell2 = 151,
        SummonRafFriend = 152,
        CreateTamedPet = 153,
        DiscoverTaxi = 154,
        TitanGrip = 155,
        EnchantItemPrismatic = 156,
        CreateLoot = 157,
        Milling = 158,
        AllowRenamePet = 159,
        ForceCast2 = 160,
        TalentSpecCount = 161,
        TalentSpecSelect = 162,
        ObliterateItem = 163,
        RemoveAura = 164,
        DamageFromMaxHealthPCT = 165,
        GiveCurrency = 166,
        UpdatePlayerPhase = 167,
        AllowControlPet = 168,
        DestroyItem = 169,
        UpdateZoneAurasPhases = 170,
        SummonPersonalGameobject = 171, // Summons Gameobject
        ResurrectWithAura = 172, // Aoe Ressurection
        UnlockGuildVaultTab = 173, // Guild Tab Unlocked (Guild Perk)
        ApplyAuraOnPet = 174,
        Effect175 = 175,
        Sanctuary2 = 176,
        DespawnPersistentAreaAura = 177,
        Effect178 = 178,
        CreateAreaTrigger = 179,
        UpdateAreatrigger = 180,
        RemoveTalent = 181,
        DespawnAreatrigger = 182,
        Unk183 = 183,
        Reputation2 = 184,
        Unk185 = 185,
        Unk186 = 186,
        RandomizeArchaeologyDigsites = 187,
        SummonStabledPetAsGuardian = 188,
        Loot = 189, // NYI, lootid in MiscValue ?
        ChangePartyMembers = 190, // MiscValueA = 1 is join, MiscValueA = 0 is leave - used by NPCs
        TeleportToDigsite = 191,
        UncageBattlepet = 192,
        StartPetBattle = 193,
        Unk194 = 194,
        PlaySceneScriptPackage = 195,
        CreateSceneObject = 196, // MiscValueA = SceneId, goes into guid as entry, SceneScriptPackageId goes into ObjectData::Entry
        CreatePersonalSceneObject = 197, // MiscValueA = SceneId, goes into guid as entry, SceneScriptPackageId goes into ObjectData::Entry
        PlayScene = 198, // NYI
        DespawnSummon = 199, // MiscValueA is some kind of unknown internal id
        HealBattlepetPct = 200, // NYI
        EnableBattlePets = 201, // NYI
        ApplyAreaAuraSummons = 202,
        RemoveAura2 = 203,
        ChangeBattlepetQuality = 204,
        LaunchQuestChoice = 205,
        AlterItem = 206,
        Unk207 = 207,
        SetReputation = 208,
        Unk209 = 209,
        LearnGarrisonBuilding = 210,
        LearnGarrisonSpecialization = 211,
        RemoveAuraBySApellLabel = 212,
        JumpDest2 = 213,
        CreateGarrison = 214,
        UpgradeCharacterSpells = 215,
        CreateShipment = 216,
        UpgradeGarrison = 217,
        Unk218 = 218,
        CreateConversation = 219,
        AddGarrisonFollower = 220,
        AddGarrisonMission = 221,
        CreateHeirloomItem = 222,
        ChangeItemBonuses = 223,
        ActivateGarrisonBuilding = 224,
        GrantBattlepetLevel = 225,
        TriggerActionSet = 226,
        TeleportToLfgDungeon = 227,
        Unk228 = 228,
        SetFollowerQuality = 229,
        Unk230 = 230,
        IncreaseFollowerExperience = 231,
        RemovePhase = 232,
        RandomizeFollowerAbilities = 233,
        Unk234 = 234,
        Unk235 = 235,
        GiveExperience = 236,
        GiveRestedEcperienceBonus = 237,
        IncreaseSkill = 238,
        EndGarrisonBuildingConstruction = 239,
        GiveArtifactPower = 240,
        Unk241 = 241,
        GiveArtifactPowerNoBonus = 242, // Unaffected by Artifact Knowledge
        ApplyEnchantIllusion = 243,
        LearnFollowerAbility = 244,
        UpgradeHeirloom = 245,
        FinishGarrisonMission = 246,
        AddGarrisonMissionSet = 247,
        FinishShipment = 248,
        ForceEquipItem = 249,
        TakeScreenshot = 250, // Serverside marker for selfie screenshot - achievement check
        SetGarrisonCacheSize = 251,
        TeleportUnits = 252,
        GiveHonor = 253,
        JumpCharge = 254,
        LearnTransmogSet = 255,
        Unk256 = 256,
        Unk257 = 257,
        ModifyKeystone = 258,
        RespecAzeriteEmpoweredItem = 259,
        SummonStabledPet = 260,
        ScrapItem = 261,
        Unk262 = 262,
        RepairItem = 263,
        RemoveGem = 264,
        LearnAzeriteEssencePower = 265,
        SetItemBonusListGroupEntry = 266, // Sets item bonuses to specific ItemBonusListGroupEntry id
        CreatePrivateConversation = 267,
        ApplyMountEquipment = 268,
        IncreaseItemBonusListGroupStep = 269, // Advances ItemBonusListGroup bonuses to next rank
        Unk270 = 270,
        ApplyAreaAuraPartyNonrandom = 271,
        SetCovenant = 272,
        CraftRuneforgeLegendary = 273,
        Unk274 = 274,
        Unk275 = 275,
        LearnTransmogIllusion = 276,
        SetChromieTime = 277,
        Unk278 = 278,
        LearnGarrTalent = 279,
        Unk280 = 280,
        LearnSoulbindConduit = 281,
        ConvertItemsToCurrency = 282,
        CompleteCampaign = 283, // Flags all quests as completed that are assigned to campaign (MiscValueA)
        SendChatMessage = 284, // MiscValue[0] = BroadcastTextID, MiscValue[1] = ChatType
        ModifyKeystone2 = 285,
        GrantBattlepetExperience = 286,
        SetGarrisonFollowerLevel = 287,
        TotalSpellEffects
    }

    public enum SpellEffectHandle
    {
        Launch,
        LaunchTarget,
        Hit,
        HitTarget
    }

    [Flags]
    public enum ProcFlags
    {
        None = 0x0,

        Killed = 0x01,    // 00 Killed by agressor - not sure about this flag
        Kill = 0x02,    // 01 Kill target (in most cases need XP/Honor reward)

        DoneMeleeAutoAttack = 0x04,    // 02 Done melee auto attack
        TakenMeleeAutoAttack = 0x08,    // 03 Taken melee auto attack

        DoneSpellMeleeDmgClass = 0x10,    // 04 Done attack by Spell that has dmg class melee
        TakenSpellMeleeDmgClass = 0x20,    // 05 Taken attack by Spell that has dmg class melee

        DoneRangedAutoAttack = 0x40,    // 06 Done ranged auto attack
        TakenRangedAutoAttack = 0x80,    // 07 Taken ranged auto attack

        DoneSpellRangedDmgClass = 0x100,    // 08 Done attack by Spell that has dmg class ranged
        TakenSpellRangedDmgClass = 0x200,    // 09 Taken attack by Spell that has dmg class ranged

        DoneSpellNoneDmgClassPos = 0x400,    // 10 Done positive spell that has dmg class none
        TakenSpellNoneDmgClassPos = 0x800,    // 11 Taken positive spell that has dmg class none

        DoneSpellNoneDmgClassNeg = 0x1000,    // 12 Done negative spell that has dmg class none
        TakenSpellNoneDmgClassNeg = 0x2000,    // 13 Taken negative spell that has dmg class none

        DoneSpellMagicDmgClassPos = 0x4000,    // 14 Done positive spell that has dmg class magic
        TakenSpellMagicDmgClassPos = 0x8000,    // 15 Taken positive spell that has dmg class magic

        DoneSpellMagicDmgClassNeg = 0x10000,    // 16 Done negative spell that has dmg class magic
        TakenSpellMagicDmgClassNeg = 0x20000,    // 17 Taken negative spell that has dmg class magic

        DonePeriodic = 0x40000,    // 18 Successful do periodic (damage / healing)
        TakenPeriodic = 0x80000,    // 19 Taken spell periodic (damage / healing)

        TakenDamage = 0x100000,    // 20 Taken any damage
        DoneTrapActivation = 0x200000,    // 21 On trap activation (possibly needs name change to ONGAMEOBJECTCAST or USE)

        DoneMainHandAttack = 0x400000,    // 22 Done main-hand melee attacks (spell and autoattack)
        DoneOffHandAttack = 0x800000,    // 23 Done off-hand melee attacks (spell and autoattack)

        Death = 0x1000000,    // 24 Died in any way
        Jump = 0x02000000,    // 25 Jumped

        EnterCombat = 0x08000000,    // 27 Entered combat
        EncounterStart = 0x10000000,    // 28 Encounter started

        // flag masks
        AutoAttackMask = DoneMeleeAutoAttack | TakenMeleeAutoAttack | DoneRangedAutoAttack | TakenRangedAutoAttack,

        MeleeMask = DoneMeleeAutoAttack | TakenMeleeAutoAttack | DoneSpellMeleeDmgClass | TakenSpellMeleeDmgClass
            | DoneMainHandAttack | DoneOffHandAttack,

        RangedMask = DoneRangedAutoAttack | TakenRangedAutoAttack | DoneSpellRangedDmgClass | TakenSpellRangedDmgClass,

        SpellMask = DoneSpellMeleeDmgClass | TakenSpellMeleeDmgClass |  DoneRangedAutoAttack | TakenRangedAutoAttack
            | DoneSpellRangedDmgClass | TakenSpellRangedDmgClass | DoneSpellNoneDmgClassPos | TakenSpellNoneDmgClassPos
            | DoneSpellNoneDmgClassNeg | TakenSpellNoneDmgClassNeg | DoneSpellMagicDmgClassPos | TakenSpellMagicDmgClassPos
            | DoneSpellMagicDmgClassNeg | TakenSpellMagicDmgClassNeg | DonePeriodic | TakenPeriodic | DoneTrapActivation,

        DoneHitMask = DoneMeleeAutoAttack | DoneRangedAutoAttack | DoneSpellMeleeDmgClass | DoneSpellRangedDmgClass
            | DoneSpellNoneDmgClassPos | DoneSpellNoneDmgClassNeg | DoneSpellMagicDmgClassPos | DoneSpellMagicDmgClassNeg
            | DonePeriodic | DoneTrapActivation | DoneMainHandAttack | DoneOffHandAttack,

        TakenHitMask = TakenMeleeAutoAttack | TakenRangedAutoAttack | TakenSpellMeleeDmgClass | TakenSpellRangedDmgClass
            | TakenSpellNoneDmgClassPos | TakenSpellNoneDmgClassNeg | TakenSpellMagicDmgClassPos | TakenSpellMagicDmgClassNeg
            | TakenPeriodic | TakenDamage,

        ReqSpellPhaseMask = SpellMask & DoneHitMask,

        MeleeBasedTriggerMask = (DoneMeleeAutoAttack | TakenMeleeAutoAttack | DoneSpellMeleeDmgClass | TakenSpellMeleeDmgClass |
                                  DoneRangedAutoAttack | TakenRangedAutoAttack | DoneSpellRangedDmgClass | TakenSpellRangedDmgClass)
    }
    public enum ProcFlagsSpellPhase
    {
        None = 0x0,
        Cast = 0x1,
        Hit = 0x2,
        Finish = 0x4,
        MaskAll = Cast | Hit | Finish
    }

    [Flags]
    public enum ProcFlagsHit
    {
        None = 0x0, // No Value - Proc_Hit_Normal | Proc_Hit_Critical For Taken Proc Type, Proc_Hit_Normal | Proc_Hit_Critical | Proc_Hit_Absorb For Done
        Normal = 0x1, // Non-Critical Hits
        Critical = 0x2,
        Miss = 0x4,
        FullResist = 0x8,
        Dodge = 0x10,
        Parry = 0x20,
        Block = 0x40, // Partial Or Full Block
        Evade = 0x80,
        Immune = 0x100,
        Deflect = 0x200,
        Absorb = 0x400, // Partial Or Full Absorb
        Reflect = 0x800,
        Interrupt = 0x1000,
        FullBlock = 0x2000,
        MaskAll = 0x0003FFF
    }

    [Flags]
    public enum ProcAttributes
    {
        ReqExpOrHonor = 0x01, // requires proc target to give exp or honor for aura proc
        TriggeredCanProc = 0x02, // aura can proc even with triggered spells
        ReqPowerCost = 0x04, // requires triggering spell to have a power cost for aura proc
        ReqSpellmod = 0x08,  // requires triggering spell to be affected by proccing aura to drop charges
        UseStacksForCharges = 0x10, // consuming proc drops a stack from proccing aura instead of charge
        ReduceProc60 = 0x80  // aura should have a reduced chance to proc if level of proc Actor > 60
    }

    // Spell aura states
    public enum AuraStateType
    {   // (C) used in caster aura state     (T) used in target aura state
        // (c) used in caster aura state-not (t) used in target aura state-not
        None = 0,            // C   |
        Defensive = 1,            // Cctt|
        Wounded20Percent = 2,            // Cct |
        Unbalanced = 3,            // Cct | Nyi
        Frozen = 4,            //  C T|
        Marked = 5,            // C  T| Nyi
        Wounded25Percent = 6,            //   T |
        Defensive2 = 7,            // Cc  | Nyi
        Banished = 8,            //  C  | Nyi
        Dazed = 9,            //    T|
        Victorious = 10,           // C   |
        Rampage = 11,           //     | Nyi
        FaerieFire = 12,           //  C T|
        Wounded35Percent = 13,           // Cct |
        RaidEncounter2 = 14,           //  Ct |
        DruidPeriodicHeal = 15,           //   T |
        RoguePoisoned = 16,           //     |
        Enraged = 17,           // C   |
        Bleed = 18,           //   T |
        Vulnerable = 19,           //     | Nyi
        ArenaPreparation = 20,           //  C  |
        WoundHealth20_80 = 21,           //   T |
        RaidEncounter = 22,           // Cctt|
        Healthy75Percent = 23,           // C   |
        WoundHealth35_80 = 24,            //   T |

        PerCasterAuraStateMask = (1 << (RaidEncounter2 - 1)) | (1 << (RoguePoisoned - 1))
    }

    // target enum name consist of:
    // [OBJECTTYPE][REFERENCETYPE(skipped for caster)][SELECTIONTYPE(skipped for default)][additional specifiers(friendly, BACKLEFT, etc.]
    public enum Targets
    {
        UnitCaster = 1,
        UnitNearbyEnemy = 2,
        UnitNearbyParty = 3,
        UnitNearbyAlly = 4,
        UnitPet = 5,
        UnitTargetEnemy = 6,
        UnitSrcAreaEntry = 7,
        UnitDestAreaEntry = 8,
        DestHome = 9,
        UnitSrcAreaUnk11 = 11,
        UnitSrcAreaEnemy = 15,
        UnitDestAreaEnemy = 16,
        DestDb = 17,
        DestCaster = 18,
        UnitCasterAreaParty = 20,
        UnitTargetAlly = 21,
        SrcCaster = 22,
        GameobjectTarget = 23,
        UnitConeEnemy24 = 24,
        UnitTargetAny = 25,
        GameobjectItemTarget = 26,
        UnitMaster = 27,
        DestDynobjEnemy = 28,
        DestDynobjAlly = 29,
        UnitSrcAreaAlly = 30,
        UnitDestAreaAlly = 31,
        DestCasterSummon = 32, // Front Left, Doesn'T Use Radius
        UnitSrcAreaParty = 33,
        UnitDestAreaParty = 34,
        UnitTargetParty = 35,
        DestCasterUnk36 = 36,
        UnitLastTargetAreaParty = 37,
        UnitNearbyEntry = 38,
        DestCasterFishing = 39,
        GameobjectNearbyEntry = 40,
        DestCasterFrontRight = 41,
        DestCasterBackRight = 42,
        DestCasterBackLeft = 43,
        DestCasterFrontLeft = 44,
        UnitChainhealAlly = 45,
        DestNearbyEntry = 46,
        DestCasterFront = 47,
        DestCasterBack = 48,
        DestCasterRight = 49,
        DestCasterLeft = 50,
        GameobjectSrcArea = 51,
        GameobjectDestArea = 52,
        DestTargetEnemy = 53,
        UnitConeEnemy54 = 54,
        DestCasterFrontLeap = 55, // For A Leap Spell
        UnitCasterAreaRaid = 56,
        UnitRaid = 57,
        UnitNearbyRaid = 58,
        UnitConeAlly = 59,
        UnitConeEntry = 60,
        UnitAreaRaidClass = 61,
        Unk62 = 62,
        DestAny = 63,
        DestFront = 64,
        DestBack = 65,
        DestRight = 66,
        DestLeft = 67,
        DestFrontRight = 68,
        DestBackRight = 69,
        DestBackLeft = 70,
        DestFrontLeft = 71,
        DestCasterRandom = 72,
        DestCasterRadius = 73,
        DestRandom = 74,
        DestRadius = 75,
        DestChannelTarget = 76,
        UnitChannelTarget = 77,
        DestDestFront = 78,
        DestDestBack = 79,
        DestDestRight = 80,
        DestDestLeft = 81,
        DestDestFrontRight = 82,
        DestDestBackRight = 83,
        DestDestBackLeft = 84,
        DestDestFrontLeft = 85,
        DestDestRandom = 86,
        DestDest = 87,
        DestDynobjNone = 88,
        DestTraj = 89,
        UnitMinipet = 90,
        DestDestRadius = 91,
        UnitSummoner = 92,
        CorpseSrcAreaEnemy = 93, // Nyi
        UnitVehicle = 94,
        UnitPassenger = 95,
        UnitPassenger0 = 96,
        UnitPassenger1 = 97,
        UnitPassenger2 = 98,
        UnitPassenger3 = 99,
        UnitPassenger4 = 100,
        UnitPassenger5 = 101,
        UnitPassenger6 = 102,
        UnitPassenger7 = 103,
        UnitConeEnemy104 = 104,
        UnitUnk105 = 105, // 1 Spell
        DestChannelCaster = 106,
        UnkDestAreaUnk107 = 107, // Not Enough Info - Only Generic Spells Avalible
        GameobjectCone_108 = 108,
        GameobjectCone_109 = 109,
        UnitConeEntry110 = 110,
        Unk111 = 111,
        Unk112 = 112,
        Unk113 = 113,
        Unk114 = 114,
        Unk115 = 115,
        Unk116 = 116,
        Unk117 = 117,
        UnitTargetAllyOrRaid = 118, // If target is in your party or raid, all party and raid members will be affected
        CorpseSrcAreaRaid = 119,
        UnitCasterAndSummons = 120,
        Unk121 = 121,
        UnitAreaThreatList = 122, // any unit on threat list
        UnitAreaTapList = 123,
        Unk124 = 124,
        DestCasterGround = 125,
        Unk126 = 126,
        Unk127 = 127,
        Unk128 = 128,
        ConeEntry129 = 129,
        Unk130 = 130,
        DestSummoner = 131,
        DestTargetAlly = 132,
        UnitLineCasterToDestAlly = 133,
        UnitLineCasterToDestEnemy = 134,
        UnitLineCasterToDest = 135,
        Unk136 = 136,
        Unk137 = 137,
        Unk138 = 138,
        Unk139 = 139,
        Unk140 = 140,
        Unk141 = 141,
        Unk142 = 142,
        Unk143 = 143,
        Unk144 = 144,
        Unk145 = 145,
        Unk146 = 146,
        Unk147 = 147,
        Unk148 = 148,
        Unk149 = 149,
        UnitOwnCritter = 150, // own battle pet from UNIT_FIELD_CRITTER
        Unk151 = 151,
        TotalSpellTargets
    }
    public enum SpellTargetSelectionCategories
    {
        Nyi,
        Default,
        Channel,
        Nearby,
        Cone,
        Area,
        Traj,
        Line
    }

    public enum SpellTargetReferenceTypes
    {
        None,
        Caster,
        Target,
        Last,
        Src,
        Dest
    }

    public enum SpellTargetObjectTypes
    {
        None = 0,
        Src,
        Dest,
        Unit,
        UnitAndDest,
        Gobj,
        GobjItem,
        Item,
        Corpse,
        // Only For Effect Target Type
        CorpseEnemy,
        CorpseAlly
    }

    public enum SpellTargetCheckTypes
    {
        Default,
        Entry,
        Enemy,
        Ally,
        Party,
        Raid,
        RaidClass,
        Passenger,
        Summoned,
        Threat,
        Tap
    }

    public enum SpellTargetDirectionTypes
    {
        None,
        Front,
        Back,
        Right,
        Left,
        FrontRight,
        BackRight,
        BackLeft,
        FrontLeft,
        Random,
        Entry
    }

    public enum ProcFlagsSpellType
    {
        None = 0x0,
        Damage = 0x1, // damage type of spell
        Heal = 0x2, // heal type of spell
        NoDmgHeal = 0x4, // other spells
        MaskAll = Damage | Heal | NoDmgHeal
    }

    public enum SpellCooldownFlags
    {
        None = 0x0,
        IncludeGCD = 0x1,  // Starts GCD in addition to normal cooldown specified in the packet
        IncludeEventCooldowns = 0x2,   // Starts GCD for spells that should start their cooldown on events, requires SPELL_COOLDOWN_FLAG_INCLUDE_GCD set
        LossOfControlUi = 0x4,  // Shows interrupt cooldown in loss of control ui
        OnHold = 0x8   // Forces cooldown to behave as if SpellInfo::IsCooldownStartedOnEvent was true
    }

    public enum SpellAreaFlag
    {
        AutoCast = 0x1, // if has autocast, spell is applied on enter
        AutoRemove = 0x2, // if has autoremove, spell is remove automatically inside zone/area (always removed on leaving area or zone)
        IgnoreAutocastOnQuestStatusChange = 0x4, // if this flag is set then spell will not be applied automatically on quest status change
    }

    public enum EnchantProcAttributes
    {
        WhiteHit = 0x01, // enchant shall only proc off white hits (not abilities)
        Limit60 = 0x02  // enchant effects shall be reduced past lvl 60
    }

    public enum MountResult
    {
        InvalidMountee = 0,
        TooFarAway = 1,
        AlreadyMounted = 2,
        NotMountable = 3,
        NotYourPet = 4,
        Other = 5,
        Looting = 6,
        RaceCantMount = 7,
        Shapeshifted = 8,
        ForcedDismount = 9,
        Ok = 10 // never sent
    }
}
