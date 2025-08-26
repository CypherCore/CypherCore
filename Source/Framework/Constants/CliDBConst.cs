// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    public enum DB2ColumnCompression : uint
    {
        None,
        Immediate,
        Common,
        Pallet,
        PalletArray,
        SignedImmediate
    }

    [Flags]
    public enum HeaderFlags : short
    {
        None = 0x0,
        Sparse = 0x1,
        SecondaryKey = 0x2,
        Index = 0x4,
        Unknown1 = 0x8,
        BitPacked = 0x10,
    }

    [Flags]
    public enum SkillLineFlags
    {
        AlwaysShownInUI = 0x01,
        NeverShownInUI = 0x02,
        FirstTierIsSelfTaught = 0x04,
        GrantedIncrementallyByCharacterUpgrade = 0x08,
        AutomaticRank = 0x0010,
        InheritParentRankWhenLearned = 0x20,
        ShowsInSpellTooltip = 0x40,
        AppearsInMiscTabOfSpellbook = 0x80,
        // unused                                       = 0x0100,
        IgnoreCategoryMods = 0x200,
        DisplaysAsProficiency = 0x400,
        PetsOnly = 0x0800,
        UniqueBitfield = 0x1000,
        RacialForThePurposeOfPaidRaceOrFactionChange = 0x2000,
        ProgressiveSkillUp = 0x4000,
        RacialForThePurposeOfTemporaryRaceChange = 0x8000,
    }

    public enum SkillLineAbilityAcquireMethod : int
    {
        Learned = 0,
        AutomaticSkillRank = 1, // Spell state will update depending on skill value
        AutomaticCharLevel = 2, // Spell will be learned/removed together with entire skill
        NeverLearned = 3,
        LearnedOrAutomaticCharLevel = 4,
    }

    [Flags]
    public enum SkillLineAbilityFlags
    {
        CanFallbackToLearnedOnSkillLearn = 0x80, // The skill is rewarded from a quest if player started on exile's reach
    }

    public enum Anim
    {
        Stand = 0,
        Death = 1,
        Spell = 2,
        Stop = 3,
        Walk = 4,
        Run = 5,
        Dead = 6,
        Rise = 7,
        StandWound = 8,
        CombatWound = 9,
        CombatCritical = 10,
        ShuffleLeft = 11,
        ShuffleRight = 12,
        WalkBackwards = 13,
        Stun = 14,
        HandsClosed = 15,
        AttackUnarmed = 16,
        Attack1h = 17,
        Attack2h = 18,
        Attack2hl = 19,
        ParryUnarmed = 20,
        Parry1h = 21,
        Parry2h = 22,
        Parry2hl = 23,
        ShieldBlock = 24,
        ReadyUnarmed = 25,
        Ready1h = 26,
        Ready2h = 27,
        Ready2hl = 28,
        ReadyBow = 29,
        Dodge = 30,
        SpellPrecast = 31,
        SpellCast = 32,
        SpellCastArea = 33,
        NpcWelcome = 34,
        NpcGoodbye = 35,
        Block = 36,
        JumpStart = 37,
        Jump = 38,
        JumpEnd = 39,
        Fall = 40,
        SwimIdle = 41,
        Swim = 42,
        SwimLeft = 43,
        SwimRight = 44,
        SwimBackwards = 45,
        AttackBow = 46,
        FireBow = 47,
        ReadyRifle = 48,
        AttackRifle = 49,
        Loot = 50,
        ReadySpellDirected = 51,
        ReadySpellOmni = 52,
        SpellCastDirected = 53,
        SpellCastOmni = 54,
        BattleRoar = 55,
        ReadyAbility = 56,
        Special1h = 57,
        Special2h = 58,
        ShieldBash = 59,
        EmoteTalk = 60,
        EmoteEat = 61,
        EmoteWork = 62,
        EmoteUseStanding = 63,
        EmoteTalkExclamation = 64,
        EmoteTalkQuestion = 65,
        EmoteBow = 66,
        EmoteWave = 67,
        EmoteCheer = 68,
        EmoteDance = 69,
        EmoteLaugh = 70,
        EmoteSleep = 71,
        EmoteSitGround = 72,
        EmoteRude = 73,
        EmoteRoar = 74,
        EmoteKneel = 75,
        EmoteKiss = 76,
        EmoteCry = 77,
        EmoteChicken = 78,
        EmoteBeg = 79,
        EmoteApplaud = 80,
        EmoteShout = 81,
        EmoteFlex = 82,
        EmoteShy = 83,
        EmotePoint = 84,
        Attack1hPierce = 85,
        Attack2hLoosePierce = 86,
        AttackOff = 87,
        AttackOffPierce = 88,
        Sheathe = 89,
        HipSheathe = 90,
        Mount = 91,
        RunRight = 92,
        RunLeft = 93,
        MountSpecial = 94,
        Kick = 95,
        SitGroundDown = 96,
        SitGround = 97,
        SitGroundUp = 98,
        SleepDown = 99,
        Sleep = 100,
        SleepUp = 101,
        SitChairLow = 102,
        SitChairMed = 103,
        SitChairHigh = 104,
        LoadBow = 105,
        LoadRifle = 106,
        AttackThrown = 107,
        ReadyThrown = 108,
        HoldBow = 109,
        HoldRifle = 110,
        HoldThrown = 111,
        LoadThrown = 112,
        EmoteSalute = 113,
        KneelStart = 114,
        KneelLoop = 115,
        KneelEnd = 116,
        AttackUnarmedOff = 117,
        SpecialUnarmed = 118,
        StealthWalk = 119,
        StealthStand = 120,
        Knockdown = 121,
        EatingLoop = 122,
        UseStandingLoop = 123,
        ChannelCastDirected = 124,
        ChannelCastOmni = 125,
        Whirlwind = 126,
        Birth = 127,
        UseStandingStart = 128,
        UseStandingEnd = 129,
        CreatureSpecial = 130,
        Drown = 131,
        Drowned = 132,
        FishingCast = 133,
        FishingLoop = 134,
        Fly = 135,
        EmoteWorkNoSheathe = 136,
        EmoteStunNoSheathe = 137,
        EmoteUseStandingNoSheathe = 138,
        SpellSleepDown = 139,
        SpellKneelStart = 140,
        SpellKneelLoop = 141,
        SpellKneelEnd = 142,
        Sprint = 143,
        InFlight = 144,
        Spawn = 145,
        Close = 146,
        Closed = 147,
        Open = 148,
        Opened = 149,
        Destroy = 150,
        Destroyed = 151,
        Rebuild = 152,
        Custom0 = 153,
        Custom1 = 154,
        Custom2 = 155,
        Custom3 = 156,
        Despawn = 157,
        Hold = 158,
        Decay = 159,
        BowPull = 160,
        BowRelease = 161,
        ShipStart = 162,
        ShipMoving = 163,
        ShipStop = 164,
        GroupArrow = 165,
        Arrow = 166,
        CorpseArrow = 167,
        GuideArrow = 168,
        Sway = 169,
        DruidCatPounce = 170,
        DruidCatRip = 171,
        DruidCatRake = 172,
        DruidCatRavage = 173,
        DruidCatClaw = 174,
        DruidCatCower = 175,
        DruidBearSwipe = 176,
        DruidBearBite = 177,
        DruidBearMaul = 178,
        DruidBearBash = 179,
        DragonTail = 180,
        DragonStomp = 181,
        DragonSpit = 182,
        DragonSpitHover = 183,
        DragonSpitFly = 184,
        EmoteYes = 185,
        EmoteNo = 186,
        JumpLandRun = 187,
        LootHold = 188,
        LootUp = 189,
        StandHigh = 190,
        Impact = 191,
        Liftoff = 192,
        Hover = 193,
        SuccubusEntice = 194,
        EmoteTrain = 195,
        EmoteDead = 196,
        EmoteDanceOnce = 197,
        Deflect = 198,
        EmoteEatNoSheathe = 199,
        Land = 200,
        Submerge = 201,
        Submerged = 202,
        Cannibalize = 203,
        ArrowBirth = 204,
        GroupArrowBirth = 205,
        CorpseArrowBirth = 206,
        GuideArrowBirth = 207,
        EmoteTalkNoSheathe = 208,
        EmotePointNoSheathe = 209,
        EmoteSaluteNoSheathe = 210,
        EmoteDanceSpecial = 211,
        Mutilate = 212,
        CustomSpell01 = 213,
        CustomSpell02 = 214,
        CustomSpell03 = 215,
        CustomSpell04 = 216,
        CustomSpell05 = 217,
        CustomSpell06 = 218,
        CustomSpell07 = 219,
        CustomSpell08 = 220,
        CustomSpell09 = 221,
        CustomSpell10 = 222,
        StealthRun = 223,
        Emerge = 224,
        Cower = 225,
        Grab = 226,
        GrabClosed = 227,
        GrabThrown = 228,
        FlyStand = 229,
        FlyDeath = 230,
        FlySpell = 231,
        FlyStop = 232,
        FlyWalk = 233,
        FlyRun = 234,
        FlyDead = 235,
        FlyRise = 236,
        FlyStandWound = 237,
        FlyCombatWound = 238,
        FlyCombatCritical = 239,
        FlyShuffleLeft = 240,
        FlyShuffleRight = 241,
        FlyWalkBackwards = 242,
        FlyStun = 243,
        FlyHandsClosed = 244,
        FlyAttackUnarmed = 245,
        FlyAttack1h = 246,
        FlyAttack2h = 247,
        FlyAttack2hl = 248,
        FlyParryUnarmed = 249,
        FlyParry1h = 250,
        FlyParry2h = 251,
        FlyParry2hl = 252,
        FlyShieldBlock = 253,
        FlyReadyUnarmed = 254,
        FlyReady1h = 255,
        FlyReady2h = 256,
        FlyReady2hl = 257,
        FlyReadyBow = 258,
        FlyDodge = 259,
        FlySpellPrecast = 260,
        FlySpellCast = 261,
        FlySpellCastArea = 262,
        FlyNpcWelcome = 263,
        FlyNpcGoodbye = 264,
        FlyBlock = 265,
        FlyJumpStart = 266,
        FlyJump = 267,
        FlyJumpEnd = 268,
        FlyFall = 269,
        FlySwimIdle = 270,
        FlySwim = 271,
        FlySwimLeft = 272,
        FlySwimRight = 273,
        FlySwimBackwards = 274,
        FlyAttackBow = 275,
        FlyFireBow = 276,
        FlyReadyRifle = 277,
        FlyAttackRifle = 278,
        FlyLoot = 279,
        FlyReadySpellDirected = 280,
        FlyReadySpellOmni = 281,
        FlySpellCastDirected = 282,
        FlySpellCastOmni = 283,
        FlySpellBattleRoar = 284,
        FlyReadyAbility = 285,
        FlySpecial1h = 286,
        FlySpecial2h = 287,
        FlyShieldBash = 288,
        FlyEmoteTalk = 289,
        FlyEmoteEat = 290,
        FlyEmoteWork = 291,
        FlyUseStanding = 292,
        FlyEmoteTalkExclamation = 293,
        FlyEmoteTalkQuestion = 294,
        FlyEmoteBow = 295,
        FlyEmoteWave = 296,
        FlyEmoteCheer = 297,
        FlyEmoteDance = 298,
        FlyEmoteLaugh = 299,
        FlyEmoteSleep = 300,
        FlyEmoteSitGround = 301,
        FlyEmoteRude = 302,
        FlyEmoteRoar = 303,
        FlyEmoteKneel = 304,
        FlyEmoteKiss = 305,
        FlyEmoteCry = 306,
        FlyEmoteChicken = 307,
        FlyEmoteBeg = 308,
        FlyEmoteApplaud = 309,
        FlyEmoteShout = 310,
        FlyEmoteFlex = 311,
        FlyEmoteShy = 312,
        FlyEmotePoint = 313,
        FlyAttack1hPierce = 314,
        FlyAttack2hLoosePierce = 315,
        FlyAttackOff = 316,
        FlyAttackOffPierce = 317,
        FlySheath = 318,
        FlyHipSheath = 319,
        FlyMount = 320,
        FlyRunRight = 321,
        FlyRunLeft = 322,
        FlyMountSpecial = 323,
        FlyKick = 324,
        FlySitGroundDown = 325,
        FlySitGround = 326,
        FlySitGroundUp = 327,
        FlySleepDown = 328,
        FlySleep = 329,
        FlySleepUp = 330,
        FlySitChairLow = 331,
        FlySitChairMed = 332,
        FlySitChairHigh = 333,
        FlyLoadBow = 334,
        FlyLoadRifle = 335,
        FlyAttackThrown = 336,
        FlyReadyThrown = 337,
        FlyHoldBow = 338,
        FlyHoldRifle = 339,
        FlyHoldThrown = 340,
        FlyLoadThrown = 341,
        FlyEmoteSalute = 342,
        FlyKneelStart = 343,
        FlyKneelLoop = 344,
        FlyKneelEnd = 345,
        FlyAttackUnarmedOff = 346,
        FlySpecialUnarmed = 347,
        FlyStealthWalk = 348,
        FlyStealthStand = 349,
        FlyKnockdown = 350,
        FlyEatingLoop = 351,
        FlyUseStandingLoop = 352,
        FlyChannelCastDirected = 353,
        FlyChannelCastOmni = 354,
        FlyWhirlwind = 355,
        FlyBirth = 356,
        FlyUseStandingStart = 357,
        FlyUseStandingEnd = 358,
        FlyCreatureSpecial = 359,
        FlyDrown = 360,
        FlyDrowned = 361,
        FlyFishingCast = 362,
        FlyFishingLoop = 363,
        FlyFly = 364,
        FlyEmoteWorkNoSheathe = 365,
        FlyEmoteStunNoSheathe = 366,
        FlyEmoteUseStandingNoSheathe = 367,
        FlySpellSleepDown = 368,
        FlySpellKneelStart = 369,
        FlySpellKneelLoop = 370,
        FlySpellKneelEnd = 371,
        FlySprint = 372,
        FlyInFlight = 373,
        FlySpawn = 374,
        FlyClose = 375,
        FlyClosed = 376,
        FlyOpen = 377,
        FlyOpened = 378,
        FlyDestroy = 379,
        FlyDestroyed = 380,
        FlyRebuild = 381,
        FlyCustom0 = 382,
        FlyCustom1 = 383,
        FlyCustom2 = 384,
        FlyCustom3 = 385,
        FlyDespawn = 386,
        FlyHold = 387,
        FlyDecay = 388,
        FlyBowPull = 389,
        FlyBowRelease = 390,
        FlyShipStart = 391,
        FlyShipMoving = 392,
        FlyShipStop = 393,
        FlyGroupArrow = 394,
        FlyArrow = 395,
        FlyCorpseArrow = 396,
        FlyGuideArrow = 397,
        FlySway = 398,
        FlyDruidCatPounce = 399,
        FlyDruidCatRip = 400,
        FlyDruidCatRake = 401,
        FlyDruidCatRavage = 402,
        FlyDruidCatClaw = 403,
        FlyDruidCatCower = 404,
        FlyDruidBearSwipe = 405,
        FlyDruidBearBite = 406,
        FlyDruidBearMaul = 407,
        FlyDruidBearBash = 408,
        FlyDragonTail = 409,
        FlyDragonStomp = 410,
        FlyDragonSpit = 411,
        FlyDragonSpitHover = 412,
        FlyDragonSpitFly = 413,
        FlyEmoteYes = 414,
        FlyEmoteNo = 415,
        FlyJumpLandRun = 416,
        FlyLootHold = 417,
        FlyLootUp = 418,
        FlyStandHigh = 419,
        FlyImpact = 420,
        FlyLiftoff = 421,
        FlyHover = 422,
        FlySuccubusEntice = 423,
        FlyEmoteTrain = 424,
        FlyEmoteDead = 425,
        FlyEmoteDanceOnce = 426,
        FlyDeflect = 427,
        FlyEmoteEatNoSheathe = 428,
        FlyLand = 429,
        FlySubmerge = 430,
        FlySubmerged = 431,
        FlyCannibalize = 432,
        FlyArrowBirth = 433,
        FlyGroupArrowBirth = 434,
        FlyCorpseArrowBirth = 435,
        FlyGuideArrowBirth = 436,
        FlyEmoteTalkNoSheathe = 437,
        FlyEmotePointNoSheathe = 438,
        FlyEmoteSaluteNoSheathe = 439,
        FlyEmoteDanceSpecial = 440,
        FlyMutilate = 441,
        FlyCustomSpell01 = 442,
        FlyCustomSpell02 = 443,
        FlyCustomSpell03 = 444,
        FlyCustomSpell04 = 445,
        FlyCustomSpell05 = 446,
        FlyCustomSpell06 = 447,
        FlyCustomSpell07 = 448,
        FlyCustomSpell08 = 449,
        FlyCustomSpell09 = 450,
        FlyCustomSpell10 = 451,
        FlyStealthRun = 452,
        FlyEmerge = 453,
        FlyCower = 454,
        FlyGrab = 455,
        FlyGrabClosed = 456,
        FlyGrabThrown = 457,
        ToFly = 458,
        ToHover = 459,
        ToGround = 460,
        FlyToFly = 461,
        FlyToHover = 462,
        FlyToGround = 463,
        Settle = 464,
        FlySettle = 465,
        DeathStart = 466,
        DeathLoop = 467,
        DeathEnd = 468,
        FlyDeathStart = 469,
        FlyDeathLoop = 470,
        FlyDeathEnd = 471,
        DeathEndHold = 472,
        FlyDeathEndHold = 473,
        Strangulate = 474,
        FlyStrangulate = 475,
        ReadyJoust = 476,
        LoadJoust = 477,
        HoldJoust = 478,
        FlyReadyJoust = 479,
        FlyLoadJoust = 480,
        FlyHoldJoust = 481,
        AttackJoust = 482,
        FlyAttackJoust = 483,
        ReclinedMount = 484,
        FlyReclinedMount = 485,
        ToAltered = 486,
        FromAltered = 487,
        FlyToAltered = 488,
        FlyFromAltered = 489,
        InStocks = 490,
        FlyInStocks = 491,
        VehicleGrab = 492,
        VehicleThrow = 493,
        FlyVehicleGrab = 494,
        FlyVehicleThrow = 495,
        ToAlteredPostSwap = 496,
        FromAlteredPostSwap = 497,
        FlyToAlteredPostSwap = 498,
        FlyFromAlteredPostSwap = 499,
        ReclinedMountPassenger = 500,
        FlyReclinedMountPassenger = 501,
        Carry2h = 502,
        Carried2h = 503,
        FlyCarry2h = 504,
        FlyCarried2h = 505,
        EmoteSniff = 506,
        EmoteFlySniff = 507,
        AttackFist1h = 508,
        FlyAttackFist1h = 509,
        AttackFist1hOff = 510,
        FlyAttackFist1hOff = 511,
        ParryFist1h = 512,
        FlyParryFist1h = 513,
        ReadyFist1h = 514,
        FlyReadyFist1h = 515,
        SpecialFist1h = 516,
        FlySpecialFist1h = 517,
        EmoteReadStart = 518,
        FlyEmoteReadStart = 519,
        EmoteReadLoop = 520,
        FlyEmoteReadLoop = 521,
        EmoteReadEnd = 522,
        FlyEmoteReadEnd = 523,
        SwimRun = 524,
        FlySwimRun = 525,
        SwimWalk = 526,
        FlySwimWalk = 527,
        SwimWalkBackwards = 528,
        FlySwimWalkBackwards = 529,
        SwimSprint = 530,
        FlySwimSprint = 531,
        MountSwimIdle = 532,
        FlyMountSwimIdle = 533,
        MountSwimBackwards = 534,
        FlyMountSwimBackwards = 535,
        MountSwimLeft = 536,
        FlyMountSwimLeft = 537,
        MountSwimRight = 538,
        FlyMountSwimRight = 539,
        MountSwimRun = 540,
        FlyMountSwimRun = 541,
        MountSwimSprint = 542,
        FlyMountSwimSprint = 543,
        MountSwimWalk = 544,
        FlyMountSwimWalk = 545,
        MountSwimWalkBackwards = 546,
        FlyMountSwimWalkBackwards = 547,
        MountFlightIdle = 548,
        FlyMountFlightIdle = 549,
        MountFlightBackwards = 550,
        FlyMountFlightBackwards = 551,
        MountFlightLeft = 552,
        FlyMountFlightLeft = 553,
        MountFlightRight = 554,
        FlyMountFlightRight = 555,
        MountFlightRun = 556,
        FlyMountFlightRun = 557,
        MountFlightSprint = 558,
        FlyMountFlightSprint = 559,
        MountFlightWalk = 560,
        FlyMountFlightWalk = 561,
        MountFlightWalkBackwards = 562,
        FlyMountFlightWalkBackwards = 563,
        MountFlightStart = 564,
        FlyMountFlightStart = 565,
        MountSwimStart = 566,
        FlyMountSwimStart = 567,
        MountSwimLand = 568,
        FlyMountSwimLand = 569,
        MountSwimLandRun = 570,
        FlyMountSwimLandRun = 571,
        MountFlightLand = 572,
        FlyMountFlightLand = 573,
        MountFlightLandRun = 574,
        FlyMountFlightLandRun = 575,
        ReadyBlowDart = 576,
        FlyReadyBlowDart = 577,
        LoadBlowDart = 578,
        FlyLoadBlowDart = 579,
        HoldBlowDart = 580,
        FlyHoldBlowDart = 581,
        AttackBlowDart = 582,
        FlyAttackBlowDart = 583,
        CarriageMount = 584,
        FlyCarriageMount = 585,
        CarriagePassengerMount = 586,
        FlyCarriagePassengerMount = 587,
        CarriageMountAttack = 588,
        FlyCarriageMountAttack = 589,
        BartenderStand = 590,
        FlyBartenderStand = 591,
        BartenderWalk = 592,
        FlyBartenderWalk = 593,
        BartenderRun = 594,
        FlyBartenderRun = 595,
        BartenderShuffleLeft = 596,
        FlyBartenderShuffleLeft = 597,
        BartenderShuffleRight = 598,
        FlyBartenderShuffleRight = 599,
        BartenderEmoteTalk = 600,
        FlyBartenderEmoteTalk = 601,
        BartenderEmotePoint = 602,
        FlyBartenderEmotePoint = 603,
        BarmaidStand = 604,
        FlyBarmaidStand = 605,
        BarmaidWalk = 606,
        FlyBarmaidWalk = 607,
        BarmaidRun = 608,
        FlyBarmaidRun = 609,
        BarmaidShuffleLeft = 610,
        FlyBarmaidShuffleLeft = 611,
        BarmaidShuffleRight = 612,
        FlyBarmaidShuffleRight = 613,
        BarmaidEmoteTalk = 614,
        FlyBarmaidEmoteTalk = 615,
        BarmaidEmotePoint = 616,
        FlyBarmaidEmotePoint = 617,
        MountSelfIdle = 618,
        FlyMountSelfIdle = 619,
        MountSelfWalk = 620,
        FlyMountSelfWalk = 621,
        MountSelfRun = 622,
        FlyMountSelfRun = 623,
        MountSelfSprint = 624,
        FlyMountSelfSprint = 625,
        MountSelfRunLeft = 626,
        FlyMountSelfRunLeft = 627,
        MountSelfRunRight = 628,
        FlyMountSelfRunRight = 629,
        MountSelfShuffleLeft = 630,
        FlyMountSelfShuffleLeft = 631,
        MountSelfShuffleRight = 632,
        FlyMountSelfShuffleRight = 633,
        MountSelfWalkBackwards = 634,
        FlyMountSelfWalkBackwards = 635,
        MountSelfSpecial = 636,
        FlyMountSelfSpecial = 637,
        MountSelfJump = 638,
        FlyMountSelfJump = 639,
        MountSelfJumpStart = 640,
        FlyMountSelfJumpStart = 641,
        MountSelfJumpEnd = 642,
        FlyMountSelfJumpEnd = 643,
        MountSelfJumpLandRun = 644,
        FlyMountSelfJumpLandRun = 645,
        MountSelfStart = 646,
        FlyMountSelfStart = 647,
        MountSelfFall = 648,
        FlyMountSelfFall = 649,
        Stormstrike = 650,
        FlyStormstrike = 651,
        ReadyJoustNoSheathe = 652,
        FlyReadyJoustNoSheathe = 653,
        Slam = 654,
        FlySlam = 655,
        DeathStrike = 656,
        FlyDeathStrike = 657,
        SwimAttackUnarmed = 658,
        FlySwimAttackUnarmed = 659,
        SpinningKick = 660,
        FlySpinningKick = 661,
        RoundHouseKick = 662,
        FlyRoundHouseKick = 663,
        RollStart = 664,
        FlyRollStart = 665,
        Roll = 666,
        FlyRoll = 667,
        RollEnd = 668,
        FlyRollEnd = 669,
        PalmStrike = 670,
        FlyPalmStrike = 671,
        MonkOffenseAttackUnarmed = 672,
        FlyMonkOffenseAttackUnarmed = 673,
        MonkOffenseAttackUnarmedOff = 674,
        FlyMonkOffenseAttackUnarmedOff = 675,
        MonkOffenseParryUnarmed = 676,
        FlyMonkOffenseParryUnarmed = 677,
        MonkOffenseReadyUnarmed = 678,
        FlyMonkOffenseReadyUnarmed = 679,
        MonkOffenseSpecialUnarmed = 680,
        FlyMonkOffenseSpecialUnarmed = 681,
        MonkDefenseAttackUnarmed = 682,
        FlyMonkDefenseAttackUnarmed = 683,
        MonkDefenseAttackUnarmedOff = 684,
        FlyMonkDefenseAttackUnarmedOff = 685,
        MonkDefenseParryUnarmed = 686,
        FlyMonkDefenseParryUnarmed = 687,
        MonkDefenseReadyUnarmed = 688,
        FlyMonkDefenseReadyUnarmed = 689,
        MonkDefenseSpecialUnarmed = 690,
        FlyMonkDefenseSpecialUnarmed = 691,
        MonkHealAttackUnarmed = 692,
        FlyMonkHealAttackUnarmed = 693,
        MonkHealAttackUnarmedOff = 694,
        FlyMonkHealAttackUnarmedOff = 695,
        MonkHealParryUnarmed = 696,
        FlyMonkHealParryUnarmed = 697,
        MonkHealReadyUnarmed = 698,
        FlyMonkHealReadyUnarmed = 699,
        MonkHealSpecialUnarmed = 700,
        FlyMonkHealSpecialUnarmed = 701,
        FlyingKick = 702,
        FlyFlyingKick = 703,
        FlyingKickStart = 704,
        FlyFlyingKickStart = 705,
        FlyingKickEnd = 706,
        FlyFlyingKickEnd = 707,
        CraneStart = 708,
        FlyCraneStart = 709,
        CraneLoop = 710,
        FlyCraneLoop = 711,
        CraneEnd = 712,
        FlyCraneEnd = 713,
        Despawned = 714,
        FlyDespawned = 715,
        ThousandFists = 716,
        FlyThousandFists = 717,
        MonkHealReadySpellDirected = 718,
        FlyMonkHealReadySpellDirected = 719,
        MonkHealReadySpellOmni = 720,
        FlyMonkHealReadySpellOmni = 721,
        MonkHealSpellCastDirected = 722,
        FlyMonkHealSpellCastDirected = 723,
        MonkHealSpellCastOmni = 724,
        FlyMonkHealSpellCastOmni = 725,
        MonkHealChannelCastDirected = 726,
        FlyMonkHealChannelCastDirected = 727,
        MonkHealChannelCastOmni = 728,
        FlyMonkHealChannelCastOmni = 729,
        Torpedo = 730,
        FlyTorpedo = 731,
        Meditate = 732,
        FlyMeditate = 733,
        BreathOfFire = 734,
        FlyBreathOfFire = 735,
        RisingSunKick = 736,
        FlyRisingSunKick = 737,
        GroundKick = 738,
        FlyGroundKick = 739,
        KickBack = 740,
        FlyKickBack = 741,
        PetBattleStand = 742,
        FlyPetBattleStand = 743,
        PetBattleDeath = 744,
        FlyPetBattleDeath = 745,
        PetBattleRun = 746,
        FlyPetBattleRun = 747,
        PetBattleWound = 748,
        FlyPetBattleWound = 749,
        PetBattleAttack = 750,
        FlyPetBattleAttack = 751,
        PetBattleReadySpell = 752,
        FlyPetBattleReadySpell = 753,
        PetBattleSpellCast = 754,
        FlyPetBattleSpellCast = 755,
        PetBattleCustom0 = 756,
        FlyPetBattleCustom0 = 757,
        PetBattleCustom1 = 758,
        FlyPetBattleCustom1 = 759,
        PetBattleCustom2 = 760,
        FlyPetBattleCustom2 = 761,
        PetBattleCustom3 = 762,
        FlyPetBattleCustom3 = 763,
        PetBattleVictory = 764,
        FlyPetBattleVictory = 765,
        PetBattleLoss = 766,
        FlyPetBattleLoss = 767,
        PetBattleStun = 768,
        FlyPetBattleStun = 769,
        PetBattleDead = 770,
        FlyPetBattleDead = 771,
        PetBattleFreeze = 772,
        FlyPetBattleFreeze = 773,
        MonkOffenseAttackWeapon = 774,
        FlyMonkOffenseAttackWeapon = 775,
        BarTendEmoteWave = 776,
        FlyBarTendEmoteWave = 777,
        BarServerEmoteTalk = 778,
        FlyBarServerEmoteTalk = 779,
        BarServerEmoteWave = 780,
        FlyBarServerEmoteWave = 781,
        BarServerPourDrinks = 782,
        FlyBarServerPourDrinks = 783,
        BarServerPickup = 784,
        FlyBarServerPickup = 785,
        BarServerPutDown = 786,
        FlyBarServerPutDown = 787,
        BarSweepStand = 788,
        FlyBarSweepStand = 789,
        BarPatronSit = 790,
        FlyBarPatronSit = 791,
        BarPatronSitEmoteTalk = 792,
        FlyBarPatronSitEmoteTalk = 793,
        BarPatronStand = 794,
        FlyBarPatronStand = 795,
        BarPatronStandEmoteTalk = 796,
        FlyBarPatronStandEmoteTalk = 797,
        BarPatronStandEmotePoint = 798,
        FlyBarPatronStandEmotePoint = 799,
        CarrionSwarm = 800,
        FlyCarrionSwarm = 801,
        WheelLoop = 802,
        FlyWheelLoop = 803,
        StandCharacterCreate = 804,
        FlyStandCharacterCreate = 805,
        MountChopper = 806,
        FlyMountChopper = 807,
        FacePose = 808,
        FlyFacePose = 809,
        WarriorColossusSmash = 810,
        FlyWarriorColossusSmash = 811,
        WarriorMortalStrike = 812,
        FlyWarriorMortalStrike = 813,
        WarriorWhirlwind = 814,
        FlyWarriorWhirlwind = 815,
        WarriorCharge = 816,
        FlyWarriorCharge = 817,
        WarriorChargeStart = 818,
        FlyWarriorChargeStart = 819,
        WarriorChargeEnd = 820,
        FlyWarriorChargeEnd = 821
    }

    [Flags]
    public enum AreaFlags : uint
    {
        EmitBreathParticles = 0x01,
        BreathParticlesOverrideParent = 0x02,
        OnMapDungeon = 0x04,
        AllowTradeChannel = 0x08,
        EnemiesPvPFlagged = 0x10,
        AllowResting = 0x20,
        AllowDueling = 0x40,
        FreeForAllPvP = 0x80,
        LinkedChat = 0x100, // Set in cities
        LinkedChatSpecialArea = 0x200,
        ForceThisAreaWhenOnDynamicTransport = 0x400,
        NoPvP = 0x800,
        NoGhostOnRelease = 0x1000,
        SubZoneAmbientMultiplier = 0x2000,
        EnableFlightBoundsOnMap = 0x4000,
        PVPPOI = 0x8000,
        NoChatChannels = 0x10000,
        AreaNotInUse = 0x20000,
        Contested = 0x40000,
        NoPlayerSummoning = 0x80000,
        NoDuelingIfTournamentRealm = 0x100000,
        PlayersCallGuards = 0x200000,
        HordeResting = 0x400000,
        AllianceResting = 0x800000,
        CombatZone = 0x1000000,
        ForceIndoors = 0x2000000,
        ForceOutdoors = 0x4000000,
        AllowHearthAndRessurectFromArea = 0x08000000,
        NoLocalDefenseChannel = 0x10000000,
        OnlyEvaluateGhostBindOnce = 0x20000000,
        IsSubzone = 0x40000000,
        DontEvaluateGraveyardFromClient = 0x80000000
    }

    [Flags]
    public enum AreaFlags2
    {
        ForceMicroDungeonArtMap = 0x01, // Ask Programmer
        UseSubzonePlayerLoot = 0x02,
        AllowPetBattleDuelingEvenIfNoDuelingAllowed = 0x04,
        UseMapTransferLocsForCemeteries = 0x08,
        IsGarrison = 0x10,
        UseSubzoneForChatChannel = 0x20,
        DontRealmCoalesceChatChannel = 0x40,
        NotExplorable = 0x80, // Don't assign area bit
        DontUseParentMapForCemeteries = 0x100,
        DontShowSanctuaryText = 0x200,
        CrossFactionZoneChat = 0x400,
        ForceNoResting = 0x800,
        AllowWarModeToggle = 0x1000
    }

    [Flags]
    public enum AreaMountFlags
    {
        None = 0x0,
        AllowGroundMounts = 0x1,
        AllowFlyingMounts = 0x2,
        AllowSurfaceSwimmingMounts = 0x4,
        AllowUnderwaterSwimmingMounts = 0x8,
        ClientEnforcesMount = 0x10
    }

    [Flags]
    public enum AreaTriggerActionSetFlag
    {
        None = 0x00,
        OnlyTriggeredByCaster = 0x01,
        ResurrectIfConditionFails = 0x02, // NYI
        Obsolete = 0x04,
        AllowWhileGhost = 0x08,
        AllowWhileDead = 0x10,
        UnifyAllInstances = 0x20, // NYI
        SuppressConditionError = 0x40, // NYI
        NotTriggeredbyCaster = 0x80,
        CreatorsPartyOnly = 0x100,
        DontRunOnLeaveWhenExpiring = 0x200, // NYI
        CanAffectUninteractible = 0x400,
        DontDespawnWithCreator = 0x800, // NYI
        CanAffectBeastmaster = 0x1000, // Can affect GMs
        RequiresLineOfSight = 0x2000 // NYI
    }

    public enum AreaTriggerShapeType
    {
        Sphere = 0,
        Box = 1,
        Unk = 2,
        Polygon = 3,
        Cylinder = 4,
        Disk = 5,
        BoundedPlane = 6,
        Max
    }

    public enum ArtifactCategory
    {
        Primary = 1,
        Fishing = 2
    }

    public enum ArtifactPowerFlag : byte
    {
        Gold = 0x01,
        NoLinkRequired = 0x02,
        Final = 0x04,
        ScalesWithNumPowers = 0x08,
        DontCountFirstBonusRank = 0x10,
        MaxRankWithTier = 0x20,

        First = NoLinkRequired | DontCountFirstBonusRank,
    }

    public enum AzeriteItemMilestoneType
    {
        MajorEssence = 0,
        MinorEssence = 1,
        BonusStamina = 2
    }

    public enum AzeriteTierUnlockSetFlags
    {
        Default = 0x01
    }

    public enum BattlegroundBracketId                                  // bracketId for level ranges
    {
        First = 0,
        Last = 12,
        Max
    }

    public enum ChrCustomizationOptionFlag
    {
        Disabled = 0x04
    }

    public enum ChrCustomizationReqFlag
    {
        HasRequirements = 0x01
    }

    public enum CharSectionType
    {
        SkinLowRes = 0,
        FaceLowRes = 1,
        FacialHairLowRes = 2,
        HairLowRes = 3,
        UnderwearLowRes = 4,
        Skin = 5,
        Face = 6,
        FacialHair = 7,
        Hair = 8,
        Underwear = 9,
        CustomDisplay1LowRes = 10,
        CustomDisplay1 = 11,
        CustomDisplay2LowRes = 12,
        CustomDisplay2 = 13,
        CustomDisplay3LowRes = 14,
        CustomDisplay3 = 15,

        Max
    }

    [Flags]
    public enum CurrencyTypesFlags : uint
    {
        Tradable = 0x00000001, // NYI
        AppearsInLootWindow = 0x00000002, // NYI
        ComputedWeeklyMaximum = 0x00000004, // NYI
        _100_Scaler = 0x00000008,
        NoLowLevelDrop = 0x00000010, // NYI
        IgnoreMaxQtyOnLoad = 0x00000020,
        LogOnWorldChange = 0x00000040, // NYI
        TrackQuantity = 0x00000080,
        ResetTrackedQuantity = 0x00000100, // NYI
        UpdateVersionIgnoreMax = 0x00000200,
        SuppressChatMessageOnVersionChange = 0x00000400,
        SingleDropInLoot = 0x00000800, // NYI
        HasWeeklyCatchup = 0x00001000, // NYI
        DoNotCompressChat = 0x00002000, // NYI
        DoNotLogAcquisitionToBi = 0x00004000, // NYI
        NoRaidDrop = 0x00008000, // NYI
        NotPersistent = 0x00010000, // NYI
        Deprecated = 0x00020000, // NYI
        DynamicMaximum = 0x00040000,
        SuppressChatMessages = 0x00080000,
        DoNotToast = 0x00100000, // NYI
        DestroyExtraOnLoot = 0x00200000, // NYI
        DontShowTotalInTooltip = 0x00400000, // NYI
        DontCoalesceInLootWindow = 0x00800000, // NYI
        AccountWide = 0x01000000, // NYI
        AllowOverflowMailer = 0x02000000, // NYI
        HideAsReward = 0x04000000, // NYI
        HasWarmodeBonus = 0x08000000, // NYI
        IsAllianceOnly = 0x10000000,
        IsHordeOnly = 0x20000000,
        LimitWarmodeBonusOncePerTooltip = 0x40000000, // NYI
        DeprecatedCurrencyFlag = 0x80000000  // this flag itself is deprecated, not currency that has it
    }

    [Flags]
    public enum CurrencyTypesFlagsB
    {
        UseTotalEarnedForEarned = 0x01,
        ShowQuestXPGainInTooltip = 0x02, // NYI
        NoNotificationMailOnOfflineProgress = 0x04, // NYI
        BattlenetVirtualCurrency = 0x08,  // NYI
        FutureCurrencyFlag = 0x10, // NYI
        DontDisplayIfZero = 0x20, // NYI
        ScaleMaxQuantityBySeasonWeeks = 0x40, // NYI
        ScaleMaxQuantityByWeeksSinceStart = 0x80, // NYI
        ForceMaxQuantityOnConversion = 0x100, // NYI
    }

    public enum BattlemasterType
    {
        Battleground = 0,
        Arena = 1,
    }

    [Flags]
    public enum BattlemasterListFlags : int
    {
        Disabled = 0x01,
        SkipRoleCheck = 0x02,
        Unk4 = 0x04,
        CanInitWarGame = 0x08,
        CanSpecificQueue = 0x10,
        Brawl = 0x20,
        Factional = 0x40
    }

    [Flags]
    public enum CfgCategoriesCharsets
    {
        Any = 0x00,
        Latin1 = 0x01,
        English = 0x02,
        Russian = 0x04,
        Korean = 0x08,
        Chinese = 0x10
    }

    [Flags]
    public enum CfgCategoriesFlags
    {
        None = 0x0,
        Tournament = 0x1
    }

    [Flags]
    public enum ChatChannelFlags
    {
        None = 0x00,
        AutoJoin = 0x01,
        ZoneBased = 0x02,
        ReadOnly = 0x04,
        AllowItemLinks = 0x08,
        OnlyInCities = 0x10,
        LinkedChannel = 0x20,
        ZoneAttackAlerts = 0x10000,
        GuildRecruitment = 0x20000,
        LookingForGroup = 0x40000,
        GlobalForTournament = 0x80000,
        DisableRaidIcons = 0x100000,
        Regional = 0x200000
    }

    public enum ChatChannelRuleset
    {
        None = 0,
        Mentor = 1,
        Disabled = 2,
        ChromieTimeCataclysm = 3,
        ChromieTimeBuringCrusade = 4,
        ChromieTimeWrath = 5,
        ChromieTimeMists = 6,
        ChromieTimeWoD = 7,
        ChromieTimeLegion = 8,
    }

    [Flags]
    public enum ChrRacesFlag
    {
        NPCOnly = 0x01,
        DoNotComponentFeet = 0x02,
        CanMount = 0x04,
        HasBald = 0x08,
        BindToStartingArea = 0x10,
        AlternateForm = 0x20,
        CanMountSelf = 0x40,
        ForceToHDModelIfAvailable = 0x80,
        ExaltedWithAllVendors = 0x100,
        NotSelectable = 0x200,
        ReputationBonus = 0x400,
        UseLoincloth = 0x800,
        RestBonus = 0x1000,
        NoStartKits = 0x2000,
        NoStartingWeapon = 0x4000,
        DontRedeemAccountLicenses = 0x8000,
        SkinVariationIsHairColor = 0x10000,
        UsePandarenRingForComponentingTexture = 0x20000,
        IgnoreForAssetManifestComponentInfoParsing = 0x40000,
        IsAlliedRace = 0x80000,
        VoidVendorDiscount = 0x100000,
        DAMMComponentNoMaleGeneration = 0x200000,
        DAMMComponentNoFemaleGeneration = 0x400000,
        NoAssociatedFactionReputationInRaceChange = 0x800000,
        InternalOnly = 0x100000,
    }

    public enum ChrSpecializationFlag
    {
        Caster = 0x01,
        Ranged = 0x02,
        Melee = 0x04,
        DualWieldTwoHanded = 0x10,     // Used For Cunitdisplay::Setsheatheinvertedfordualwield
        PetOverrideSpec = 0x20,
        Recommended = 0x40,
    }

    public enum ChrSpecializationRole
    {
        Tank = 0,
        Healer = 1,
        Dps = 2
    }

    public enum ChrSpecialization
    {
        None = 0,
        MageArcane = 62,
        MageFire = 63,
        MageFrost = 64,
        PaladinHoly = 65,
        PaladinProtection = 66,
        PaladinRetribution = 70,
        WarriorArms = 71,
        WarriorFury = 72,
        WarriorProtection = 73,
        DruidBalance = 102,
        DruidFeral = 103,
        DruidGuardian = 104,
        DruidRestoration = 105,
        DeathKnightBlood = 250,
        DeathKnightFrost = 251,
        DeathKnightUnholy = 252,
        HunterBeastMastery = 253,
        HunterMarksmanship = 254,
        HunterSurvival = 255,
        PriestDiscipline = 256,
        PriestHoly = 257,
        PriestShadow = 258,
        RogueAssassination = 259,
        RogueOutlaw = 260,
        RogueSubtely = 261,
        ShamanElemental = 262,
        ShamanEnhancement = 263,
        ShamanRestoration = 264,
        WarlockAffliction = 265,
        WarlockDemonology = 266,
        WarlockDestruction = 267,
        MonkBrewmaster = 268,
        MonkWindwalker = 269,
        MonkMistweaver = 270,
        DemonHunterHavoc = 577,
        DemonHunterVengeance = 581,
        EvokerDevastation = 1467,
        EvokerPreservation = 1468,
        EvokerAugmentation = 1473
    }

    public enum ContentTuningCalcType
    {
        None = 0,
        MinLevel = 1,
        MaxLevel = 2,
        PrevExpansionMaxLevel = 3
    }

    [Flags]
    public enum ContentTuningFlag
    {
        DisabledForItem = 0x04,
        Horde = 0x08,
        Alliance = 0x10
    }

    public enum Curves
    {
        ArtifactRelicItemLevelBonus = 1718,
        AzeriteEmpoweredItemRespecCost = 6785
    }

    public enum Emote
    {
        OneshotNone = 0,
        OneshotTalk = 1,
        OneshotBow = 2,
        OneshotWave = 3,
        OneshotCheer = 4,
        OneshotExclamation = 5,
        OneshotQuestion = 6,
        OneshotEat = 7,
        StateDance = 10,
        OneshotLaugh = 11,
        StateSleep = 12,
        StateSit = 13,
        OneshotRude = 14,
        OneshotRoar = 15,
        OneshotKneel = 16,
        OneshotKiss = 17,
        OneshotCry = 18,
        OneshotChicken = 19,
        OneshotBeg = 20,
        OneshotApplaud = 21,
        OneshotShout = 22,
        OneshotFlex = 23,
        OneshotShy = 24,
        OneshotPoint = 25,
        StateStand = 26,
        StateReadyUnarmed = 27,
        StateWorkSheathed = 28,
        StatePoint = 29,
        StateNone = 30,
        OneshotWound = 33,
        OneshotWoundCritical = 34,
        OneshotAttackUnarmed = 35,
        OneshotAttack1h = 36,
        OneshotAttack2htight = 37,
        OneshotAttack2hLoose = 38,
        OneshotParryUnarmed = 39,
        OneshotParryShield = 43,
        OneshotReadyUnarmed = 44,
        OneshotReady1h = 45,
        OneshotReadyBow = 48,
        OneshotSpellPrecast = 50,
        OneshotSpellCast = 51,
        OneshotBattleRoar = 53,
        OneshotSpecialattack1h = 54,
        OneshotKick = 60,
        OneshotAttackThrown = 61,
        StateStun = 64,
        StateDead = 65,
        OneshotSalute = 66,
        StateKneel = 68,
        StateUseStanding = 69,
        OneshotWaveNoSheathe = 70,
        OneshotCheerNoSheathe = 71,
        OneshotEatNoSheathe = 92,
        StateStunNoSheathe = 93,
        OneshotDance = 94,
        OneshotSaluteNoSheath = 113,
        StateUseStandingNoSheathe = 133,
        OneshotLaughNoSheathe = 153,
        StateWork = 173,
        StateSpellPrecast = 193,
        OneshotReadyRifle = 213,
        StateReadyRifle = 214,
        StateWorkMining = 233,
        StateWorkChopwood = 234,
        StateApplaud = 253,
        OneshotLiftoff = 254,
        OneshotYes = 273,
        OneshotNo = 274,
        OneshotTrain = 275,
        OneshotLand = 293,
        StateAtEase = 313,
        StateReady1h = 333,
        StateSpellKneelStart = 353,
        StateSubmerged = 373,
        OneshotSubmerge = 374,
        StateReady2h = 375,
        StateReadyBow = 376,
        OneshotMountSpecial = 377,
        StateTalk = 378,
        StateFishing = 379,
        OneshotFishing = 380,
        OneshotLoot = 381,
        StateWhirlwind = 382,
        StateDrowned = 383,
        StateHoldBow = 384,
        StateHoldRifle = 385,
        StateHoldThrown = 386,
        OneshotDrown = 387,
        OneshotStomp = 388,
        OneshotAttackOff = 389,
        OneshotAttackOffPierce = 390,
        StateRoar = 391,
        StateLaugh = 392,
        OneshotCreatureSpecial = 393,
        OneshotJumplandrun = 394,
        OneshotJumpend = 395,
        OneshotTalkNoSheathe = 396,
        OneshotPointNoSheathe = 397,
        StateCannibalize = 398,
        OneshotJumpstart = 399,
        StateDancespecial = 400,
        OneshotDancespecial = 401,
        OneshotCustomSpell01 = 402,
        OneshotCustomSpell02 = 403,
        OneshotCustomSpell03 = 404,
        OneshotCustomSpell04 = 405,
        OneshotCustomSpell05 = 406,
        OneshotCustomSpell06 = 407,
        OneshotCustomSpell07 = 408,
        OneshotCustomSpell08 = 409,
        OneshotCustomSpell09 = 410,
        OneshotCustomSpell10 = 411,
        StateExclaim = 412,
        StateDanceCustom = 413,
        StateSitChairMed = 415,
        StateCustomSpell01 = 416,
        StateCustomSpell02 = 417,
        StateEat = 418,
        StateCustomSpell04 = 419,
        StateCustomSpell03 = 420,
        StateCustomSpell05 = 421,
        StateSpelleffectHold = 422,
        StateEatNoSheathe = 423,
        StateMount = 424,
        StateReady2hl = 425,
        StateSitChairHigh = 426,
        StateFall = 427,
        StateLoot = 428,
        StateSubmergedNew = 429,
        OneshotCower = 430,
        StateCower = 431,
        OneshotUseStanding = 432,
        StateStealthStand = 433,
        OneshotOmnicastGhoul = 434,
        OneshotAttackBow = 435,
        OneshotAttackRifle = 436,
        StateSwimIdle = 437,
        StateAttackUnarmed = 438,
        OneshotSpellCastWSound = 439,
        OneshotDodge = 440,
        OneshotParry1h = 441,
        OneshotParry2h = 442,
        OneshotParry2hl = 443,
        StateFlyfall = 444,
        OneshotFlydeath = 445,
        StateFlyFall = 446,
        OneshotFlySitGroundDown = 447,
        OneshotFlySitGroundUp = 448,
        OneshotEmerge = 449,
        OneshotDragonSpit = 450,
        StateSpecialUnarmed = 451,
        OneshotFlygrab = 452,
        StateFlygrabclosed = 453,
        OneshotFlygrabthrown = 454,
        StateFlySitGround = 455,
        StateWalkBackwards = 456,
        OneshotFlytalk = 457,
        OneshotFlyattack1h = 458,
        StateCustomSpell08 = 459,
        OneshotFlyDragonSpit = 460,
        StateSitChairLow = 461,
        OneshotStun = 462,
        OneshotSpellCastOmni = 463,
        StateReadyThrown = 465,
        OneshotWorkChopwood = 466,
        OneshotWorkMining = 467,
        StateSpellChannelOmni = 468,
        StateSpellChannelDirected = 469,
        StandStateNone = 470,
        StateReadyjoust = 471,
        StateStrangulate = 472,
        StateStrangulate2 = 473,
        StateReadySpellOmni = 474,
        StateHoldJoust = 475,
        OneshotCryJaina = 476,
        OneshotSpecialUnarmed = 477,
        StateDanceNosheathe = 478,
        OneshotSniff = 479,
        OneshotDragonstomp = 480,
        OneshotKnockdown = 482,
        StateRead = 483,
        OneshotFlyemotetalk = 485,
        StateReadAllowmovement = 492,
        StateCustomSpell06 = 498,
        StateCustomSpell07 = 499,
        StateCustomSpell082 = 500,
        StateCustomSpell09 = 501,
        StateCustomSpell10 = 502,
        StateReady1hAllowMovement = 505,
        StateReady2hAllowMovement = 506,
        OneshotMonkoffenseAttackunarmed = 507,
        OneshotMonkoffenseSpecialunarmed = 508,
        OneshotMonkoffenseParryunarmed = 509,
        StateMonkoffenseReadyunarmed = 510,
        OneshotPalmstrike = 511,
        StateCrane = 512,
        OneshotOpen = 517,
        StateReadChristmas = 518,
        OneshotFlyattack2hl = 526,
        OneshotFlyattackthrown = 527,
        StateFlyreadyspelldirected = 528,
        StateFlyReady1h = 531,
        StateMeditate = 533,
        StateFlyReady2hl = 534,
        OneshotToground = 535,
        OneshotTofly = 536,
        StateAttackthrown = 537,
        StateSpellChannelDirectedNosound = 538,
        OneshotWork = 539,
        StateReadyunarmedNosound = 540,
        OneshotMonkoffenseAttackunarmedoff = 543,
        ReclinedMountPassenger = 546,
        OneshotQuestion_2 = 547,
        OneshotSpellChannelDirectedNosound = 549,
        StateKneel_2 = 550,
        OneshotFlyattackunarmed = 551,
        OneshotFlycombatwound = 552,
        OneshotMountselfspecial = 553,
        OneshotAttackunarmedNosound = 554,
        StateWoundcriticalDoesntWork = 555,
        OneshotAttack1hNoSound = 556,
        StateMountSelfIdle = 557,
        OneshotWalk = 558,
        StateOpened = 559,
        StateCustomspell03 = 564,
        OneshotBreathoffire = 565,
        StateAttack1h = 567,
        StateWorkChopwood2 = 568,
        StateUsestandingLoop = 569,
        StateUsestanding = 572,
        OneshotSheath = 573,
        OneshotLaughNoSound = 574,
        ReclinedMount = 575,
        OneshotAttack1h2 = 577,
        StateCryNosound = 578,
        OneshotCryNosound = 579,
        OneshotCombatcritical = 584,
        StateTrain = 585,
        StateWorkChopwoodLumberAxe = 586,
        OneshotSpecialattack2h = 587,
        StateReadAndTalk = 588,
        OneshotStandVar1 = 589,
        RexxarStranglesGoblin = 590,
        OneshotStandVar2 = 591,
        OneshotDeath = 592,
        StateTalkonce = 595,
        StateAttack2h = 596,
        StateSitGround = 598,
        StateWorkChopwood3 = 599,
        StateCustomspell01 = 601,
        OneshotCombatwound = 602,
        OneshotTalkExclamation = 603,
        OneshotQuestion2 = 604,
        StateCry = 605,
        StateUsestandingLoop2 = 606,
        StateWorkSmith = 613,
        StateWorkChopwood4 = 614,
        StateCustomspell02 = 615,
        StateReadAndSit = 616,
        StateParryUnarmed = 619,
        StateBlockShield = 620,
        StateSitGround2 = 621,
        OneshotMountspecial = 628,
        OneshotSettle = 636,
        StateAttackUnarmedStill = 638,
        StateReadBookAndTalk = 641,
        OneshotSlam = 642,
        OneshotGrabthrown = 643,
        OneshotReadyspelldirectedNosound = 644,
        StateReadyspellomniWithSound = 645,
        OneshotTalkBarserver = 646,
        OneshotWaveBarserver = 647,
        StateWorkMining2 = 648,
        StateReady2hlAllowMovement = 654,
        StateUsestandingNosheatheStill = 655,
        OneshotWorkStill = 657,
        StateHoldThrownInterrupts = 658,
        OneshotCannibalize = 659,
        OneshotNoNotSwimming = 661,
        StateReadyglv = 663,
        OneshotCombatabilityglv01 = 664,
        OneshotCombatabilityglvoff01 = 665,
        OneshotCombatabilityglvbig02 = 666,
        OneshotParryglv = 667,
        StateWorkMining3 = 668,
        OneshotTalkNosheathe = 669,
        OneshotStandVar3 = 671,
        StateKneel2 = 672,
        OneshotCustom0 = 673,
        OneshotCustom1 = 674,
        OneshotCustom2 = 675,
        OneshotCustom3 = 676,
        StateFlyReadyUnarmed = 677,
        OneshotCheerForthealliance = 679,
        OneshotCheerForthehorde = 680,
        OneshotStandVar4 = 690,
        OneshotFlyemoteexclamation = 691,
        StateEmoteeat = 700,
        StateMonkhealChannelomni = 705,
        StateMonkdefenseReadyunarmed = 706,
        OneshotStand = 707,
        StateWapourhold = 709,
        StateReadyblowdart = 710,
        StateWorkChopmeat = 711,
        StateMonk2hlidle = 712,
        StateWaperch = 713,
        StateWaguardstand01 = 714,
        StateReadAndSitChairMed = 715,
        StateWaguardstand02 = 716,
        StateWaguardstand03 = 717,
        StateWaguardstand04 = 718,
        StateWachant02 = 719,
        StateWalean01 = 720,
        StateDrunkwalk = 721,
        StateWascrubbing = 722,
        StateWachant01 = 723,
        StateWachant03 = 724,
        StateWasummon01 = 725,
        StateWatrance01 = 726,
        StateCustomspell05 = 727,
        StateWahammerloop = 728,
        StateWabound01 = 729,
        StateWabound02 = 730,
        StateWasackhold = 731,
        StateWasit01 = 732,
        StateWarowingstandleft = 733,
        StateWarowingstandright = 734,
        StateLootBiteSound = 735,
        OneshotWasummon01 = 736,
        OneshotStandVar22 = 737,
        OneshotFalconeerStart = 738,
        StateFalconeerLoop = 739,
        OneshotFalconeerEnd = 740,
        StateWaperchNointeract = 741,
        OneshotWastanddrink = 742,
        StateWalean02 = 743,
        OneshotReadEnd = 744,
        StateWaguardstand04AllowMovement = 745,
        StateReadycrossbow = 747,
        OneshotWastanddrinkNosheath = 748,
        StateWahang01 = 749,
        StateWabeggarstand = 750,
        StateWadrunkstand = 751,
        OneshotWacriertalk = 753,
        StateHoldCrossbow = 754,
        StateWasit02 = 757,
        StateWacrankstand = 761,
        OneshotReadStart = 762,
        OneshotReadLoop = 763,
        OneshotWadrunkdrink = 765,
        StateSitChairMedEat = 766,
        StateKneelCopy = 867,
        StateWorkChopmeatNosheathe = 868,
        OneshotBarpatronPoint = 870,
        StateStandNosound = 871,
        StateMountFlightIdleNosound = 872,
        StateUsestandingLoop3 = 873,
        OneshotVehiclegrab = 874,
        StateUsestandingLoop4 = 875,
        StateBarpatronStand = 876,
        OneshotWabeggarpoint = 877,
        StateWacrierstand01 = 878,
        OneshotWabeggarbeg = 879,
        StateWaboatwheelstand = 880,
        StateWasit03 = 882,
        StateBarsweepStand = 883,
        StateWaguardstand05 = 884,
        StateWaguardstand06 = 885,
        StateBartendstand = 886,
        StateWahammerloop2 = 887,
        // StandStateStateReadythrown          = 890,  //< Doesn'T Work
        StateWorkMiningNoCombat = 893,
        OneshotCaststrong = 894,
        StateCustomspell07 = 895,
        StateWalk = 897,
        OneshotClose = 898,
        StateWacratehold = 900,
        StateFlycustomspell02 = 901,
        OneshotSleep = 902,
        StateStandSetemotestate = 903,
        OneshotWawalktalk = 904,
        OneshotTakeOffFinish = 905,
        OneshotAttack2h = 906,
        StateWaBarrelHold = 908,
        StateWaBarrelWalk = 909,
        StateCustomspell04 = 910,
        StateFlywaperch01 = 912,
        OneshotPalspellcast1hup = 916,
        OneshotReadyspellomni = 917,
        OneshotSpellcastDirected = 961,
        StateFlycustomspell07 = 977,
        StateFlychannelcastomni = 978,
        StateClosed = 979,
        StateCustomspell10 = 980,
        StateWawheelbarrowstand = 981,
        StateCustomspell06 = 982,
        StateCustom1 = 983,
        StateWasit04 = 986,
        OneshotBarsweepStand = 987,
        TorghastTalkingHeadMawCastSound = 989,
        TorghastTalkingHeadMawCastSound2 = 990,
        OneshotStandVar0 = 991,
        OneshotFlycustomspell01 = 992,
        OneshotSpelleffectDecay = 993,
        StateCreatureSpecial = 994,
        OneshotWareact01 = 1001,
        OneshotFlycustomspell04 = 1004,
        OneshotTalkSubdued = 1005,
        StateEmotetalk = 1006,
        StateWainteraction = 1007,
        OneshotTakeOffStart = 1009,
        OneshotBattleroarNoSound = 1010,
        StateWaweaponsharpen = 1011,
        OneshotRollstart = 1012,
        OneshotRollend = 1013,
        OneshotWareact02 = 1014,
        OneshotWathreaten = 1015,
        Artoffloop = 1016,
        StateReadyspellomniNosheath = 1017,
    }

    public enum GlyphSlotType
    {
        Major = 0,
        Minor = 1,
        Prime = 2
    }

    public enum ItemSetFlags
    {
        LegacyInactive = 0x01,
    }

    public enum ItemSpecStat : byte
    {
        Intellect = 0,
        Agility = 1,
        Strength = 2,
        Spirit = 3,
        Hit = 4,
        Dodge = 5,
        Parry = 6,
        OneHandedAxe = 7,
        TwoHandedAxe = 8,
        OneHandedSword = 9,
        TwoHandedSword = 10,
        OneHandedMace = 11,
        TwoHandedMace = 12,
        Dagger = 13,
        FistWeapon = 14,
        Gun = 15,
        Bow = 16,
        Crossbow = 17,
        Staff = 18,
        Polearm = 19,
        Thrown = 20,
        Wand = 21,
        Shield = 22,
        Relic = 23,
        Crit = 24,
        Haste = 25,
        BonusArmor = 26,
        Cloak = 27,
        Warglaives = 28,
        RelicIron = 29,
        RelicBlood = 30,
        RelicShadow = 31,
        RelicFel = 32,
        RelicArcane = 33,
        RelicFrost = 34,
        RelicFire = 35,
        RelicWater = 36,
        RelicLife = 37,
        RelicWind = 38,
        RelicHoly = 39,

        None = 40
    }

    public enum LockKeyType
    {
        None = 0,
        Item = 1,
        Skill = 2,
        Spell = 3
    }

    public enum LockType
    {
        Lockpicking = 1,
        Herbalism = 2,
        Mining = 3,
        DisarmTrap = 4,
        Open = 5,
        Treasure = 6,
        CalcifiedElvenGems = 7,
        Close = 8,
        ArmTrap = 9,
        QuickOpen = 10,
        QuickClose = 11,
        OpenTinkering = 12,
        OpenKneeling = 13,
        OpenAttacking = 14,
        Gahzridian = 15,
        Blasting = 16,
        PvpOpen = 17,
        PvpClose = 18,
        Fishing = 19,
        Inscription = 20,
        OpenFromVehicle = 21,
        Archaeology = 22,
        PvpOpenFast = 23,
        LumberMill = 28,
        Skinning = 29,
        AncientMana = 30,
        Warboard = 31,
        ClassicHerbalism = 32,
        OutlandHerbalism = 33,
        NorthrendHerbalism = 34,
        CataclysmHerbalism = 35,
        PandariaHerbalism = 36,
        DraenorHerbalism = 37,
        LegionHerbalism = 38,
        KulTiranHerbalism = 39,
        ClassicMining = 40,
        OutlandMining = 41,
        NorthrendMining = 42,
        CataclysmMining = 43,
        PandariaMining = 44,
        DraenorMining = 45,
        LegionMining = 46,
        KulTiranMining = 47,
        LegionSkinning = 48,
        OpenItem = 149,
        Foraging = 150,
        JellyDeposit = 152,
        ShadowlandsHerbalism = 153,
        ShadowlandsMining = 155,
        CovenantNightFae = 157,
        CovenantVenthyr = 158,
        CovenantKyrian = 159,
        CovenantNecrolord = 160,
        Engineering = 161,
        DragonIslesHerbalism = 162,
        Mining2 = 163,
        ElusiveHerbalism = 166,
        ElusiveMining = 167,
        Enchanting = 169,
        DragonIslesTreasure = 170,
        DragonIslesAlchemy25 = 172,
        DragonIslesBlacksmithing25 = 173,
        DragonIslesEnchanting25 = 174,
        DragonIslesEngineering25 = 175,
        DragonIslesHerbalism25 = 176,
        DragonIslesInscription25 = 177,
        DragonIslesJewelcrafting25 = 178,
        DragonIslesLeatherworking25 = 179,
        DragonIslesMining25 = 180,
        DragonIslesSkinning25 = 181,
        DragonIslesTailoring25 = 182,
        OpenKneelingPlant = 186,
        DragonIslesMining = 188
    }

    public enum MapTypes : byte
    {
        Common = 0,
        Instance = 1,
        Raid = 2,
        Battleground = 3,
        Arena = 4,
        Scenario = 5
    }

    public enum MapDifficultyFlags : int
    {
        LimitToPlayersFromOneRealm = 0x01,
        UseLootBasedLockInsteadOfInstanceLock = 0x02, // Lock to single encounters
        LockedToSoloOwner = 0x04,
        ResumeDungeonProgressBasedOnLockout = 0x08, // Mythic dungeons with this flag zone into leaders instance instead of always using a fresh one (Return to Karazhan, Operation: Mechagon)
        DisableLockExtension = 0x10,
    }

    public enum MapDifficultyResetInterval : byte
    {
        Anytime = 0,
        Daily = 1,
        Weekly = 2
    }

    public enum ModifierTreeOperator
    {
        SingleTrue = 2,
        SingleFalse = 3,
        All = 4,
        Some = 8
    }

    public enum MountCapabilityFlags : int
    {
        Ground = 0x1,
        Flying = 0x2,
        Float = 0x4,
        Underwater = 0x8,
        IgnoreRestrictions = 0x20
    }

    public enum MountFlags : int
    {
        ServerOnly = 0x01,
        IsSelfMount = 0x02,
        ExcludeFromJournalIfFactionDoesntMatch = 0x04,
        AllowMountedCombat = 0x08,
        SummonRandomFavorWhileUnderwater = 0x10,
        SummonRandomFavorWhileAtWaterSurface = 0x20,
        ExcludeFromJournalIfNotLearned = 0x40,
        SummonRandomDoNotFavorWhenGrounded = 0x80,
        ShowInSpellbook = 0x100,
        AddToActionBarOnLearn = 0x200,
        NotForUseAsATaxi = 0x400,
        MountEquipmentEffectsSuppressed = 0x800,
        DisablePlayerMountPreview = 0x1000,
    }

    public enum PathPropertyIndex
    {
        UseNewLiquidGenerateCode = 0,
        AnimaCableId = 1,
        AnimaPlayerCondition = 2,
        AnimaStartTaper = 3,
        AnimaEndTaper = 4,
        VolumeHeight = 5,
        AiPathGraphMaxStartDist = 6,
        AiPathGraphMinTotalDist = 7,
        AiPathGraphAreaControl = 8,
        AiPathGraphAreaId = 9,
        AiPathGraphWidth = 10,
        AiPathDefaultFollowStyle = 11,
        AiPathConstrainSteering = 12,
        Phase = 13,
        SteepSlopeDegrees = 14
    }

    public enum PhaseEntryFlags : int
    {
        ReadOnly = 0x1,
        InternalPhase = 0x2,
        Normal = 0x8,
        Cosmetic = 0x010,
        Personal = 0x020,
        Expensive = 0x040,
        EventsAreObservable = 0x080,
        UsesPreloadConditions = 0x100,
        UnshareablePersonal = 0x200,
        ObjectsAreVisible = 0x400,
    }

    // PhaseUseFlags fields in different db2s
    [Flags]
    public enum PhaseUseFlagsValues : byte
    {
        None = 0x0,
        AlwaysVisible = 0x1,
        Inverse = 0x2,

        All = AlwaysVisible | Inverse
    }

    public enum PlayerConditionLfgStatus
    {
        InLFGDungeon = 1,
        InLFGRandomDungeon = 2,
        InLFGFirstRandomDungeon = 3,
        PartialClear = 4,
        StrangerCount = 5,
        VoteKickCount = 6,
        BootCount = 7,
        GearDiff = 8
    }

    public enum PlayerDataElementType
    {
        Int64 = 0,
        Float = 1
    }

    public enum PlayerInteractionType
    {
        None = 0,
        TradePartner = 1,
        Item = 2,
        Gossip = 3,
        QuestGiver = 4,
        Merchant = 5,
        TaxiNode = 6,
        Trainer = 7,
        Banker = 8,
        AlliedRaceDetailsGiver = 9,
        GuildBanker = 10,
        Registrar = 11,
        Vendor = 12,
        PetitionVendor = 13,
        GuildTabardVendor = 14,
        TalentMaster = 15,
        SpecializationMaster = 16,
        MailInfo = 17,
        SpiritHealer = 18,
        AreaSpiritHealer = 19,
        Binder = 20,
        Auctioneer = 21,
        StableMaster = 22,
        BattleMaster = 23,
        Transmogrifier = 24,
        LFGDungeon = 25,
        VoidStorageBanker = 26,
        BlackMarketAuctioneer = 27,
        AdventureMap = 28,
        WorldMap = 29,
        GarrArchitect = 30,
        GarrTradeskill = 31,
        GarrMission = 32,
        ShipmentCrafter = 33,
        GarrRecruitment = 34,
        GarrTalent = 35,
        Trophy = 36,
        PlayerChoice = 37,
        ArtifactForge = 38,
        ObliterumForge = 39,
        ScrappingMachine = 40,
        ContributionCollector = 41,
        AzeriteRespec = 42,
        IslandQueue = 43,
        ItemInteraction = 44,
        ChromieTime = 45,
        CovenantPreview = 46,
        AnimaDiversion = 47,
        LegendaryCrafting = 48,
        WeeklyRewards = 49,
        Soulbind = 50,
        CovenantSanctum = 51,
        NewPlayerGuide = 52,
        ItemUpgrade = 53,
        AdventureJournal = 54,
        Renown = 55,
        AzeriteForge = 56,
        PerksProgramVendor = 57,
        ProfessionsCraftingOrder = 58,
        Professions = 59,
        ProfessionsCustomerOrder = 60,
        TraitSystem = 61,
        BarbersChoice = 62,
        JailersTowerBuffs = 63,
        MajorFactionRenown = 64,
        PersonalTabardVendor = 65,
        ForgeMaster = 66,
        CharacterBanker = 67,
        AccountBanker = 68,
        ProfessionRespec = 69,
        PlaceholderType71 = 70,
        PlaceholderType72 = 71,
        PlaceholderType73 = 72,
        PlaceholderType74 = 73,
        PlaceholderType75 = 74,
        PlaceholderType76 = 75,
        GuildRename = 76,
        PlaceholderType77 = 77,
    }

    [Flags]
    public enum PowerTypeFlags
    {
        StopRegenWhileCasting = 0x01,
        UseRegenInterrupt = 0x02,
        FillFractionalPowerOnEnergize = 0x08,
        NoClientPrediction = 0x10,
        UnitsUseDefaultPowerOnInit = 0x20,
        NotSetToDefaultOnResurrect = 0x40,
        IsUsedByNPCs = 0x80,
        ContinueRegenWhileFatigued = 0x200,
        RegenAffectedByHaste = 0x400,
        SetToMaxOnLevelUp = 0x1000,
        SetToMaxOnInitialLogIn = 0x2000,
        AllowCostModsForPlayers = 0x4000
    }

    public enum PrestigeLevelInfoFlags : byte
    {
        Disabled = 0x01                      // Prestige levels with this flag won't be included to calculate max prestigelevel.
    }

    public enum QuestPackageFilter : byte
    {
        LootSpecialization = 0,    // Players can select this quest reward if it matches their selected loot specialization
        Class = 1,    // Players can select this quest reward if it matches their class
        Unmatched = 2,    // Players can select this quest reward if no class/loot_spec rewards are available
        Everyone = 3     // Players can always select this quest reward
    }

    public enum ScenarioStepFlags : byte
    {
        BonusObjective = 0x1,
        HeroicOnly = 0x2
    }

    public enum SkillRaceClassInfoFlags : ushort
    {
        NoSkillupMessage = 0x2,
        AlwaysMaxValue = 0x10,
        Unlearnable = 0x20,     // Skill can be unlearned
        IncludeInSort = 0x80,     // Spells belonging to a skill with this flag will additionally compare skill ids when sorting spellbook in client
        NotTrainable = 0x100,
        MonoValue = 0x400     // Skill always has value 1
    }

    public enum SpellCategoryFlags : int
    {
        CooldownScalesWithWeaponSpeed = 0x01, // unused
        CooldownStartsOnEvent = 0x04,
        CooldownExpiresAtDailyReset = 0x08,
        IgnoreForModTimeRate = 0x40
    }

    [Flags]
    public enum SpellEffectAttributes
    {
        None = 0,
        NoImmunity = 0x01,  // not cancelled by immunities
        PositionIsFacingRelative = 0x02, /*NYI*/
        JumpChargeUnitMeleeRange = 0x04, /*NYI*/
        JumpChargeUnitStrictPathCheck = 0x08, /*NYI*/
        ExcludeOwnParty = 0x10, /*NYI*/
        AlwaysAoeLineOfSight = 0x20,
        SuppressPointsStacking = 0x40,
        ChainFromInitialTarget = 0x80,
        UncontrolledNoBackwards = 0x100, /*NYI*/
        AuraPointsStack = 0x000200, // refreshing periodic auras with this attribute will add remaining damage to new aura
        NoCopyDamageInterruptsOrProcs = 0x400, /*NYI*/
        AddTargetCombatReachToAOE = 0x800, /*NYI*/
        IsHarmful = 0x1000,
        ForceScaleToOverrideCameraMinHeight = 0x2000, /*NYI*/
        PlayersOnly = 0x004000,
        ComputePointsOnlyAtCastTime = 0x8000, /*NYI*/
        EnforceLineOfSightToChainTargets = 0x10000,
        AreaEffectsUseTargetRadius = 0x20000, /*NYI*/
        TeleportWithVehicle = 0x40000, /*NYI*/
        ScalePointsByChallengeModeDamageScaler = 0x80000, /*NYI*/
        DontFailSpellOnTargetingFailure = 0x100000, /*NYI*/
        IgnoreDuringCooldownTimeRateCalculation = 0x800000, /*NYI*/
    }

    public enum SpellProcsPerMinuteModType : byte
    {
        Haste = 1,
        Crit = 2,
        Class = 3,
        Spec = 4,
        Race = 5,
        ItemLevel = 6,
        Battleground = 7
    }

    public enum SpellShapeshiftFormFlags
    {
        Stance = 0x01,
        NotToggleable = 0x02,   // player cannot cancel the aura giving this shapeshift
        PersistOnDeath = 0x04,
        CanInteractNPC = 0x08,   // if the form does not have SHAPESHIFT_FORM_IS_NOT_A_SHAPESHIFT then this flag must be present to allow NPC interaction
        DontUseWeapon = 0x10,

        CanUseEquippedItems = 0x40,   // if the form does not have SHAPESHIFT_FORM_IS_NOT_A_SHAPESHIFT then this flag allows equipping items without ITEM_FLAG_USABLE_WHEN_SHAPESHIFTED
        CanUseItems = 0x80,   // if the form does not have SHAPESHIFT_FORM_IS_NOT_A_SHAPESHIFT then this flag allows using items without ITEM_FLAG_USABLE_WHEN_SHAPESHIFTED
        DontAutoUnshift = 0x100,   // clientside
        ConsideredDead = 0x200,
        CanOnlyCastShapeshiftSpells = 0x400,   // prevents using spells that don't have any shapeshift requirement
        StanceCancelsAtFlightmaster = 0x800,
        NoEmoteSounds = 0x1000,
        NoTriggerTeleport = 0x2000,
        CannotChangeEquippedItems = 0x4000,

        CannotUseGameObjects = 0x10000
    }

    public enum SummonPropertiesFlags : uint
    {
        None = 0x00,
        AttackSummoner = 0x01, // NYI
        HelpWhenSummonedInCombat = 0x02, // NYI
        UseLevelOffset = 0x04, // NYI
        DespawnOnSummonerDeath = 0x08, // NYI
        OnlyVisibleToSummoner = 0x10,
        CannotDismissPet = 0x20, // NYI
        UseDemonTimeout = 0x40,
        UnlimitedSummons = 0x80, // NYI
        UseCreatureLevel = 0x100,
        JoinSummonerSpawnGroup = 0x200, // NYI
        DoNotToggle = 0x400, // NYI
        DespawnWhenExpired = 0x800, // NYI
        UseSummonerFaction = 0x1000,
        DoNotFollowMountedSummoner = 0x2000, // NYI
        SavePetAutocast = 0x4000, // NYI
        IgnoreSummonerPhase = 0x8000, // Wild Only
        OnlyVisibleToSummonerGroup = 0x10000,
        DespawnOnSummonerLogout = 0x20000, // NYI
        CastRideVehicleSpellOnSummoner = 0x40000, // NYI
        GuardianActsLikePet = 0x80000, // NYI
        DontSnapSessileToGround = 0x100000, // NYI
        SummonFromBattlePetJournal = 0x200000,
        UnitClutter = 0x400000, // NYI
        DefaultNameColor = 0x800000, // NYI
        UseOwnInvisibilityDetection = 0x1000000, // NYI. Ignore Owner's Invisibility Detection
        DespawnWhenReplaced = 0x2000000, // NYI. Totem Slots Only
        DespawnWhenTeleportingOutOfRange = 0x4000000, // NYI
        SummonedAtGroupFormationPosition = 0x8000000, // NYI
        DontDespawnOnSummonerDeath = 0x10000000, // NYI
        UseTitleAsCreatureName = 0x20000000, // NYI
        AttackableBySummoner = 0x40000000, // NYI
        DontDismissWhenEncounterIsAborted = 0x80000000  // NYI
    }

    [Flags]
    public enum TaxiNodeFlags : int
    {
        ShowOnAllianceMap = 0x01,
        ShowOnHordeMap = 0x02,
        ShowOnMapBorder = 0x04,
        ShowIfClientPassesCondition = 0x08,
        UsePlayerFavoriteMount = 0x10,
        EndPointOnly = 0x20,
        IgnoreForFindNearest = 0x40,
        DoNotShowInWorldMapUI = 0x80,
        ShowNpcMinimapAtlasIfClientPassesCondition = 0x100,
        MapLayerTransition = 0x200,
        NotAccountWide = 0x400
    }

    [Flags]
    public enum TaxiPathNodeFlags : int
    {
        Teleport = 0x1,
        Stop = 0x2
    }

    public enum TextEmotes
    {
        Agree = 1,
        Amaze = 2,
        Angry = 3,
        Apologize = 4,
        Applaud = 5,
        Bashful = 6,
        Beckon = 7,
        Beg = 8,
        Bite = 9,
        Bleed = 10,
        Blink = 11,
        Blush = 12,
        Bonk = 13,
        Bored = 14,
        Bounce = 15,
        Brb = 16,
        Bow = 17,
        Burp = 18,
        Bye = 19,
        Cackle = 20,
        Cheer = 21,
        Chicken = 22,
        Chuckle = 23,
        Clap = 24,
        Confused = 25,
        Congratulate = 26,
        Cough = 27,
        Cower = 28,
        Crack = 29,
        Cringe = 30,
        Cry = 31,
        Curious = 32,
        Curtsey = 33,
        Dance = 34,
        Drink = 35,
        Drool = 36,
        Eat = 37,
        Eye = 38,
        Fart = 39,
        Fidget = 40,
        Flex = 41,
        Frown = 42,
        Gasp = 43,
        Gaze = 44,
        Giggle = 45,
        Glare = 46,
        Gloat = 47,
        Greet = 48,
        Grin = 49,
        Groan = 50,
        Grovel = 51,
        Guffaw = 52,
        Hail = 53,
        Happy = 54,
        Hello = 55,
        Hug = 56,
        Hungry = 57,
        Kiss = 58,
        Kneel = 59,
        Laugh = 60,
        Laydown = 61,
        Message = 62,
        Moan = 63,
        Moon = 64,
        Mourn = 65,
        No = 66,
        Nod = 67,
        Nosepick = 68,
        Panic = 69,
        Peer = 70,
        Plead = 71,
        Point = 72,
        Poke = 73,
        Pray = 74,
        Roar = 75,
        Rofl = 76,
        Rude = 77,
        Salute = 78,
        Scratch = 79,
        Sexy = 80,
        Shake = 81,
        Shout = 82,
        Shrug = 83,
        Shy = 84,
        Sigh = 85,
        Sit = 86,
        Sleep = 87,
        Snarl = 88,
        Spit = 89,
        Stare = 90,
        Surprised = 91,
        Surrender = 92,
        Talk = 93,
        Talkex = 94,
        Talkq = 95,
        Tap = 96,
        Thank = 97,
        Threaten = 98,
        Tired = 99,
        Victory = 100,
        Wave = 101,
        Welcome = 102,
        Whine = 103,
        Whistle = 104,
        Work = 105,
        Yawn = 106,
        Boggle = 107,
        Calm = 108,
        Cold = 109,
        Comfort = 110,
        Cuddle = 111,
        Duck = 112,
        Insult = 113,
        Introduce = 114,
        Jk = 115,
        Lick = 116,
        Listen = 117,
        Lost = 118,
        Mock = 119,
        Ponder = 120,
        Pounce = 121,
        Praise = 122,
        Purr = 123,
        Puzzle = 124,
        Raise = 125,
        Ready = 126,
        Shimmy = 127,
        Shiver = 128,
        Shoo = 129,
        Slap = 130,
        Smirk = 131,
        Sniff = 132,
        Snub = 133,
        Soothe = 134,
        Stink = 135,
        Taunt = 136,
        Tease = 137,
        Thirsty = 138,
        Veto = 139,
        Snicker = 140,
        Stand = 141,
        Tickle = 142,
        Violin = 143,
        Smile = 163,
        Rasp = 183,
        Pity = 203,
        Growl = 204,
        Bark = 205,
        Scared = 223,
        Flop = 224,
        Love = 225,
        Moo = 226,
        Commend = 243,
        Train = 264,
        Helpme = 303,
        Incoming = 304,
        Charge = 305,
        Flee = 306,
        Attackmytarget = 307,
        Oom = 323,
        Follow = 324,
        Wait = 325,
        Healme = 326,
        Openfire = 327,
        Flirt = 328,
        Joke = 329,
        Golfclap = 343,
        Wink = 363,
        Pat = 364,
        Serious = 365,
        MountSpecial = 366,
        Goodluck = 367,
        Blame = 368,
        Blank = 369,
        Brandish = 370,
        Breath = 371,
        Disagree = 372,
        Doubt = 373,
        Embarrass = 374,
        Encourage = 375,
        Enemy = 376,
        Eyebrow = 377,
        Toast = 378,
        Fail = 379,
        Highfive = 380,
        Absent = 381,
        Arm = 382,
        Awe = 383,
        Backpack = 384,
        Badfeeling = 385,
        Challenge = 386,
        Chug = 387,
        Ding = 389,
        Facepalm = 390,
        Faint = 391,
        Go = 392,
        Going = 393,
        Glower = 394,
        Headache = 395,
        Hiccup = 396,
        Hiss = 398,
        Holdhand = 399,
        Hurry = 401,
        Idea = 402,
        Jealous = 403,
        Luck = 404,
        Map = 405,
        Mercy = 406,
        Mutter = 407,
        Nervous = 408,
        Offer = 409,
        Pet = 410,
        Pinch = 411,
        Proud = 413,
        Promise = 414,
        Pulse = 415,
        Punch = 416,
        Pout = 417,
        Regret = 418,
        Revenge = 420,
        Rolleyes = 421,
        Ruffle = 422,
        Sad = 423,
        Scoff = 424,
        Scold = 425,
        Scowl = 426,
        Search = 427,
        Shakefist = 428,
        Shifty = 429,
        Shudder = 430,
        Signal = 431,
        Silence = 432,
        Sing = 433,
        Smack = 434,
        Sneak = 435,
        Sneeze = 436,
        Snort = 437,
        Squeal = 438,
        Stopattack = 439,
        Suspicious = 440,
        Think = 441,
        Truce = 442,
        Twiddle = 443,
        Warn = 444,
        Snap = 445,
        Charm = 446,
        Coverears = 447,
        Crossarms = 448,
        Look = 449,
        Object = 450,
        Sweat = 451,
        Yw = 453,
        Read = 456,
        Boot = 506,
        ForTheAlliance = 507,
        ForTheHorde = 508,
        Whoa = 517,
        Oops = 518,
        Alliance = 519,
        Horde = 520,
        Meow = 521,
        Boop = 522,
        Wince = 623,
        Huzzah = 624,
        Impressed = 625,
        Magnificent = 626,
    }

    public enum ExpectedStatType : byte
    {
        CreatureHealth = 0,
        PlayerHealth = 1,
        CreatureAutoAttackDps = 2,
        CreatureArmor = 3,
        PlayerMana = 4,
        PlayerPrimaryStat = 5,
        PlayerSecondaryStat = 6,
        ArmorConstant = 7,
        None = 8,
        CreatureSpellDamage = 9
    }

    [Flags]
    public enum TraitCombatConfigFlags
    {
        None = 0x0,
        ActiveForSpec = 0x1,
        StarterBuild = 0x2,
        SharedActionBars = 0x4
    }

    [Flags]
    public enum TraitCondFlags
    {
        None = 0x0,
        IsGate = 0x1,
        IsAlwaysMet = 0x2,
        IsSufficient = 0x4,
    }

    public enum TraitConditionType
    {
        Available = 0,
        Visible = 1,
        Granted = 2,
        Increased = 3,
        DisplayError = 4
    }

    public enum TraitConfigType
    {
        Invalid = 0,
        Combat = 1,
        Profession = 2,
        Generic = 3
    }

    public enum TraitCurrencyType
    {
        Gold = 0,
        CurrencyTypesBased = 1,
        TraitSourced = 2
    }

    public enum TraitEdgeType
    {
        VisualOnly = 0,
        DeprecatedRankConnection = 1,
        SufficientForAvailability = 2,
        RequiredForAvailability = 3,
        MutuallyExclusive = 4,
        DeprecatedSelectionOption = 5
    }

    public enum TraitNodeEntryType
    {
        SpendHex = 0,
        SpendSquare = 1,
        SpendCircle = 2,
        SpendSmallCircle = 3,
        DeprecatedSelect = 4,
        DragAndDrop = 5,
        SpendDiamond = 6,
        ProfPath = 7,
        ProfPerk = 8,
        ProfPathUnlock = 9
    }

    [Flags]
    public enum TraitNodeGroupFlag
    {
        None = 0x0,
        AvailableByDefault = 0x1
    }

    public enum TraitNodeType
    {
        Single = 0,
        Tiered = 1,
        Selection = 2,
        SubTreeSelection = 3
    }

    public enum TraitPointsOperationType
    {
        None = -1,
        Set = 0,
        Multiply = 1
    }

    [Flags]
    public enum TraitTreeFlag
    {
        None = 0x0,
        CannotRefund = 0x1,
        HideSingleRankNumbers = 0x2
    }

    public enum UiMapFlag
    {
        None = 0x00,
        NoHighlight = 0x01,
        ShowOverlays = 0x02,
        ShowTaxiNodes = 0x04,
        GarrisonMap = 0x08,
        FallbackToParentMap = 0x10,
        NoHighlightTexture = 0x20,
        ShowTaskObjectives = 0x40,
        NoWorldPositions = 0x80,
        HideArchaeologyDigs = 0x100,
        Deprecated = 0x200,
        HideIcons = 0x400,
        HideVignettes = 0x800,
        ForceAllOverlayExplored = 0x1000,
        FlightMapShowZoomOut = 0x2000,
        FlightMapAutoZoom = 0x4000,
        ForceOnNavbar = 0x8000
    }

    public enum UiMapSystem : sbyte
    {
        World = 0,
        Taxi = 1,
        Adventure = 2,
        SystemMinimap = 3,
        Max
    }

    public enum UiMapType : byte
    {
        Cosmic = 0,
        World = 1,
        Continent = 2,
        Zone = 3,
        Dungeon = 4,
        Micro = 5,
        Orphan = 6
    }

    [Flags]
    public enum UnitConditionFlags
    {
        LogicOr = 0x1
    }

    public enum UnitConditionOp
    {
        EqualTo = 1,
        NotEqualTo = 2,
        LessThan = 3,
        LessThanOrEqualTo = 4,
        GreaterThan = 5,
        GreaterThanOrEqualTo = 6
    }

    public enum UnitConditionVariable
    {
        None = 0,  // - NONE -
        Race = 1,  // Race {$Is/Is Not} "{ChrRaces}"
        Class = 2,  // Class {$Is/Is Not} "{ChrClasses}"
        Level = 3,  // Level {$Relative Op} "{#Level}"
        IsSelf = 4,  // Is self? {$Yes/No}{=1}
        IsMyPet = 5,  // Is my pet? {$Yes/No}{=1}
        IsMaster = 6,  // Is master? {$Yes/No}{=1}
        IsTarget = 7,  // Is target? {$Yes/No}{=1}
        CanAssist = 8,  // Can assist? {$Yes/No}{=1}
        CanAttack = 9,  // Can attack? {$Yes/No}{=1}
        HasPet = 10, // Has pet? {$Yes/No}{=1}
        HasWeapon = 11, // Has weapon? {$Yes/No}{=1}
        HealthPct = 12, // Health {$Relative Op} {#Health %}%
        ManaPct = 13, // Mana {$Relative Op} {#Mana %}%
        RagePct = 14, // Rage {$Relative Op} {#Rage %}%
        EnergyPct = 15, // Energy {$Relative Op} {#Energy %}%
        ComboPoints = 16, // Combo Points {$Relative Op} {#Points}
        HasHelpfulAuraSpell = 17, // Has helpful aura spell? {$Yes/No} "{Spell}"
        HasHelpfulAuraDispelType = 18, // Has helpful aura dispel type? {$Yes/No} "{SpellDispelType}"
        HasHelpfulAuraMechanic = 19, // Has helpful aura mechanic? {$Yes/No} "{SpellMechanic}"
        HasHarmfulAuraSpell = 20, // Has harmful aura spell? {$Yes/No} "{Spell}"
        HasHarmfulAuraDispelType = 21, // Has harmful aura dispel type? {$Yes/No} "{SpellDispelType}"
        HasHarmfulAuraMechanic = 22, // Has harmful aura mechanic? {$Yes/No} "{SpellMechanic}"
        HasHarmfulAuraSchool = 23, // Has harmful aura school? {$Yes/No} "{Resistances}"
        DamagePhysicalPct = 24, // NYI Damage (Physical) {$Relative Op} {#Physical Damage %}%
        DamageHolyPct = 25, // NYI Damage (Holy) {$Relative Op} {#Holy Damage %}%
        DamageFirePct = 26, // NYI Damage (Fire) {$Relative Op} {#Fire Damage %}%
        DamageNaturePct = 27, // NYI Damage (Nature) {$Relative Op} {#Nature Damage %}%
        DamageFrostPct = 28, // NYI Damage (Frost) {$Relative Op} {#Frost Damage %}%
        DamageShadowPct = 29, // NYI Damage (Shadow) {$Relative Op} {#Shadow Damage %}%
        DamageArcanePct = 30, // NYI Damage (Arcane) {$Relative Op} {#Arcane Damage %}%
        InCombat = 31, // In combat? {$Yes/No}{=1}
        IsMoving = 32, // Is moving? {$Yes/No}{=1}
        IsCasting = 33, // Is casting? {$Yes/No}{=1}
        IsCastingSpell = 34, // Is casting spell? {$Yes/No}{=1}
        IsChanneling = 35, // Is channeling? {$Yes/No}{=1}
        IsChannelingSpell = 36, // Is channeling spell? {$Yes/No}{=1}
        NumberOfMeleeAttackers = 37, // Number of melee attackers {$Relative Op} {#Attackers}
        IsAttackingMe = 38, // Is attacking me? {$Yes/No}{=1}
        Range = 39, // Range {$Relative Op} {#Yards}
        InMeleeRange = 40, // In melee range? {$Yes/No}{=1}
        PursuitTime = 41, // NYI Pursuit time {$Relative Op} {#Seconds}
        HasHarmfulAuraCanceledByDamage = 42, // Has harmful aura canceled by damage? {$Yes/No}{=1}
        HasHarmfulAuraWithPeriodicDamage = 43, // Has harmful aura with periodic damage? {$Yes/No}{=1}
        NumberOfEnemies = 44, // Number of enemies {$Relative Op} {#Enemies}
        NumberOfFriends = 45, // NYI Number of friends {$Relative Op} {#Friends}
        ThreatPhysicalPct = 46, // NYI Threat (Physical) {$Relative Op} {#Physical Threat %}%
        ThreatHolyPct = 47, // NYI Threat (Holy) {$Relative Op} {#Holy Threat %}%
        ThreatFirePct = 48, // NYI Threat (Fire) {$Relative Op} {#Fire Threat %}%
        ThreatNaturePct = 49, // NYI Threat (Nature) {$Relative Op} {#Nature Threat %}%
        ThreatFrostPct = 50, // NYI Threat (Frost) {$Relative Op} {#Frost Threat %}%
        ThreatShadowPct = 51, // NYI Threat (Shadow) {$Relative Op} {#Shadow Threat %}%
        ThreatArcanePct = 52, // NYI Threat (Arcane) {$Relative Op} {#Arcane Threat %}%
        IsInterruptible = 53, // NYI Is interruptible? {$Yes/No}{=1}
        NumberOfAttackers = 54, // Number of attackers {$Relative Op} {#Attackers}
        NumberOfRangedAttackers = 55, // Number of ranged attackers {$Relative Op} {#Ranged Attackers}
        CreatureType = 56, // Creature type {$Is/Is Not} "{CreatureType}"
        IsMeleeAttacking = 57, // Is melee-attacking? {$Yes/No}{=1}
        IsRangedAttacking = 58, // Is ranged-attacking? {$Yes/No}{=1}
        Health = 59, // Health {$Relative Op} {#HP} HP
        SpellKnown = 60, // Spell known? {$Yes/No} "{Spell}"
        HasHarmfulAuraEffect = 61, // Has harmful aura effect? {$Yes/No} "{#Spell Aura}"
        IsImmuneToAreaOfEffect = 62, // NYI Is immune to area-of-effect? {$Yes/No}{=1}
        IsPlayer = 63, // Is player? {$Yes/No}{=1}
        DamageMagicPct = 64, // NYI Damage (Magic) {$Relative Op} {#Magic Damage %}%
        DamageTotalPct = 65, // NYI Damage (Total) {$Relative Op} {#Damage %}%
        ThreatMagicPct = 66, // NYI Threat (Magic) {$Relative Op} {#Magic Threat %}%
        ThreatTotalPct = 67, // NYI Threat (Total) {$Relative Op} {#Threat %}%
        HasCritter = 68, // Has critter? {$Yes/No}{=1}
        HasTotemInSlot1 = 69, // Has totem in slot 1? {$Yes/No}{=1}
        HasTotemInSlot2 = 70, // Has totem in slot 2? {$Yes/No}{=1}
        HasTotemInSlot3 = 71, // Has totem in slot 3? {$Yes/No}{=1}
        HasTotemInSlot4 = 72, // Has totem in slot 4? {$Yes/No}{=1}
        HasTotemInSlot5 = 73, // NYI Has totem in slot 5? {$Yes/No}{=1}
        Creature = 74, // Creature {$Is/Is Not} "{Creature}"
        StringID = 75, // NYI String ID {$Is/Is Not} "{StringID}"
        HasAura = 76, // Has aura? {$Yes/No} {Spell}
        IsEnemy = 77, // Is enemy? {$Yes/No}{=1}
        IsSpecMelee = 78, // Is spec - melee? {$Yes/No}{=1}
        IsSpecTank = 79, // Is spec - tank? {$Yes/No}{=1}
        IsSpecRanged = 80, // Is spec - ranged? {$Yes/No}{=1}
        IsSpecHealer = 81, // Is spec - healer? {$Yes/No}{=1}
        IsPlayerControlledNPC = 82, // Is player controlled NPC? {$Yes/No}{=1}
        IsDying = 83, // Is dying? {$Yes/No}{=1}
        PathFailCount = 84, // NYI Path fail count {$Relative Op} {#Path Fail Count}
        IsMounted = 85, // Is mounted? {$Yes/No}{=1}
        Label = 86, // NYI Label {$Is/Is Not} "{Label}"
        IsMySummon = 87, //
        IsSummoner = 88, //
        IsMyTarget = 89, //
        Sex = 90, // Sex {$Is/Is Not} "{UnitSex}"
        LevelWithinContentTuning = 91, // Level is within {$Is/Is Not} {ContentTuning}

        IsFlying = 93, // Is flying? {$Yes/No}{=1}
        IsHovering = 94, // Is hovering? {$Yes/No}{=1}
        HasHelpfulAuraEffect = 95, // Has helpful aura effect? {$Yes/No} "{#Spell Aura}"
        HasHelpfulAuraSchool = 96, // Has helpful aura school? {$Yes/No} "{Resistances}"
    }

    public enum WorldStateExpressionValueType
    {
        Constant = 1,
        WorldState = 2,
        Function = 3
    }

    public enum WorldStateExpressionLogic
    {
        None = 0,
        And = 1,
        Or = 2,
        Xor = 3,
    }

    public enum WorldStateExpressionComparisonType
    {
        None = 0,
        Equal = 1,
        NotEqual = 2,
        Less = 3,
        LessOrEqual = 4,
        Greater = 5,
        GreaterOrEqual = 6,
    }

    public enum WorldStateExpressionOperatorType
    {
        None = 0,
        Sum = 1,
        Substraction = 2,
        Multiplication = 3,
        Division = 4,
        Remainder = 5,
    }

    public enum WorldStateExpressionFunctions
    {
        None = 0,
        Random,
        Month,
        Day,
        TimeOfDay,
        Region,
        ClockHour,
        OldDifficultyId,
        HolidayStart,
        HolidayLeft,
        HolidayActive,
        TimerCurrentTime,
        WeekNumber,
        Unk13,
        Unk14,
        DifficultyId,
        WarModeActive,
        Unk17,
        Unk18,
        Unk19,
        Unk20,
        Unk21,
        WorldStateExpression,
        KeystoneAffix,
        Unk24,
        Unk25,
        Unk26,
        Unk27,
        KeystoneLevel,
        Unk29,
        Unk30,
        Unk31,
        Unk32,
        MersenneRandom,
        Unk34,
        Unk35,
        Unk36,
        UiWidgetData,
        TimeEventPassed,

        Max,
    }

    public enum CorruptionEffectsFlag
    {
        None = 0,
        Disabled = 0x1
    }

    [Flags]
    public enum CreatureModelDataFlags
    {
        NoFootprintParticles = 0x01,
        NoBreathParticles = 0x02,
        IsPlayerModel = 0x04,
        NoAttachedWeapons = 0x10,
        NoFootprintTrailTextures = 0x20,
        DisableHighlight = 0x40,
        CanMountWhileTransformedAsThis = 0x80,
        DisableScaleInterpolation = 0x100,
        ForceProjectedTex = 0x200,
        CanJumpInPlaceAsMount = 0x400,
        AICannotUseWalkBackwardsAnim = 0x800,
        IgnoreSpineLowForSplitBody = 0x1000,
        IgnoreHeadForSplitBody = 0x2000,
        IgnoreSpineLowForSplitBodyWhenFlying = 0x4000,
        IgnoreHeadForSplitBodyWhenFlying = 0x8000,
        UseWheelAnimationOnUnitWheelBones = 0x10000,
        IsHDModel = 0x20000,
        SuppressEmittersOnLowSettings = 0x40000
    }

    public enum FriendshipReputationFlags : int
    {
        NoFXOnReactionChange = 0x01,
        NoLogTextOnRepGain = 0x02,
        NoLogTextOnReactionChange = 0x04,
        ShowRepGainandReactionChangeForHiddenFaction = 0x08,
        NoRepGainModifiers = 0x10
    }

    public enum GameRule
    {
        NoDebuffLimit = 1,
        CharNameReservationEnabled = 2,
        MaxCharReservationsPerRealm = 3,
        MaxAccountCharReservationsPerContentset = 4,
        EtaRealmLaunchTime = 5,
        TrivialGroupXPPercent = 7,
        CharReservationsPerRealmReopenThreshold = 8,
        DisablePct = 9,
        HardcoreRuleset = 10,
        ReplaceAbsentGmSeconds = 11,
        ReplaceGmRankLastOnlineSeconds = 12,
        GameMode = 13,
        CharacterlessLogin = 14,
        NoMultiboxing = 15,
        VanillaNpcKnockback = 16,
        Runecarving = 17,
        TalentRespecCostMin = 18,
        TalentRespecCostMax = 19,
        TalentRespecCostStep = 20,
        VanillaRageGenerationModifier = 21,
        SelfFoundAllowed = 22,
        DisableHonorDecay = 23,
        MaxLootDropLevel = 25,
        MicrobarScale = 26,
        MaxUnitNameDistance = 27,
        MaxNameplateDistance = 28,
        UserAddonsDisabled = 29,
        UserScriptsDisabled = 30,
        NonPlayerNameplateScale = 31,
        ForcedPartyFrameScale = 32,
        CustomActionbarOverlayHeightOffset = 33,
        ForcedChatLanguage = 34,
        LandingPageFactionID = 35,
        CollectionsPanelDisabled = 36,
        CharacterPanelDisabled = 37,
        SpellbookPanelDisabled = 38,
        TalentsPanelDisabled = 39,
        AchievementsPanelDisabled = 40,
        CommunitiesPanelDisabled = 41,
        EncounterJournalDisabled = 42,
        FinderPanelDisabled = 43,
        StoreDisabled = 44,
        HelpPanelDisabled = 45,
        GuildsDisabled = 46,
        QuestLogMicrobuttonDisabled = 47,
        MapPlunderstormCircle = 48,
        AfterDeathSpectatingUI = 49,
        FrontEndChat = 50,
        UniversalNameplateOcclusion = 51,
        FastAreaTriggerTick = 52,
        AllPlayersAreFastMovers = 53,
        IgnoreChrclassDisabledFlag = 54,
        CharacterCreateUseFixedBackgroundModel = 55,
        ForceAlteredFormsOn = 56,
        PlayerNameplateDifficultyIcon = 57,
        PlayerNameplateAlternateHealthColor = 58,
        AlwaysAllowAlliedRaces = 59,
        ActionbarIconIntroDisabled = 60,
        ReleaseSpiritGhostDisabled = 61,
        DeleteItemConfirmationDisabled = 62,
        ChatLinkLevelToastsDisabled = 63,
        BagsUIDisabled = 64,
        PetBattlesDisabled = 65,
        PerksProgramActivityTrackingDisabled = 66,
        MaximizeWorldMapDisabled = 67,
        WorldMapTrackingOptionsDisabled = 68,
        WorldMapTrackingPinDisabled = 69,
        WorldMapHelpPlateDisabled = 70,
        QuestLogPanelDisabled = 71,
        QuestLogSuperTrackingDisabled = 72,
        TutorialFrameDisabled = 73,
        IngameMailNotificationDisabled = 74,
        IngameCalendarDisabled = 75,
        IngameTrackingDisabled = 76,
        IngameWhoListDisabled = 77,
        RaceAlteredFormsDisabled = 78,
        IngameFriendsListDisabled = 79,
        MacrosDisabled = 80,
        CompactRaidFrameManagerDisabled = 81,
        EditModeDisabled = 82,
        InstanceDifficultyBannerDisabled = 83,
        FullCharacterCreateDisabled = 84,
        TargetFrameBuffsDisabled = 85,
        UnitFramePvPContextualDisabled = 86,
        BlockWhileSheathedAllowed = 88,
        VanillaAccountMailInstant = 91,
        ClearMailOnRealmTransfer = 92,
        PremadeGroupFinderStyle = 93,
        PlunderstormAreaSelection = 94,
        GroupFinderCapabilities = 98,
        WorldMapLegendDisabled = 99,
        WorldMapFrameStrata = 100,
        MerchantFilterDisabled = 101,
        SummoningStones = 108,
        TransmogEnabled = 109,
        MailGameRule = 132,
        LootMethodStyle = 157,
    }

    public enum GlobalCurve
    {
        CritDiminishing = 0,
        MasteryDiminishing = 1,
        HasteDiminishing = 2,
        SpeedDiminishing = 3,
        AvoidanceDiminishing = 4,
        VersatilityDoneDiminishing = 5,
        LifestealDiminishing = 6,
        DodgeDiminishing = 7,
        BlockDiminishing = 8,
        ParryDiminishing = 9,

        VersatilityTakenDiminishing = 11,

        ContentTuningPvpItemLevelHealthScaling = 13,
        ContentTuningPvpLevelDamageScaling = 14,
        ContentTuningPvpItemLevelDamageScaling = 15,

        ArmorItemLevelDiminishing = 18,

        ChallengeModeHealth = 21,
        ChallengeModeDamage = 22,
        MythicPlusEndOfRunGearSequenceLevel = 23,

        SpellAreaEffectWarningRadius = 26,  // ground spell effect warning circle radius (based on spell radius)
    }

    public enum BattlePetSpeciesFlags : int
    {
        NoRename = 0x01,
        WellKnown = 0x02,
        NotAccountWide = 0x04,
        Capturable = 0x08,
        NotTradable = 0x10,
        HideFromJournal = 0x20,
        LegacyAccountUnique = 0x40,
        CantBattle = 0x80,
        HordeOnly = 0x100,
        AllianceOnly = 0x200,
        Boss = 0x400,
        RandomDisplay = 0x800,
        NoLicenseRequired = 0x1000,
        AddsAllowedWithBoss = 0x2000,
        HideUntilLearned = 0x4000,
        MatchPlayerHighPetLevel = 0x8000,
        NoWildPetAddsAllowed = 0x10000,
    }

    public enum SpellVisualEffectNameType
    {
        Model = 0,
        Item = 1,
        Creature = 2,
        UnitItemMainHand = 3,
        UnitItemOffHand = 4,
        UnitItemRanged = 5,
        UnitAmmoBasic = 6,
        UnitAmmoPreferred = 7,
        UnitItemMainHandIgnoreDisarmed = 8,
        UnitItemOffHandIgnoreDisarmed = 9,
        UnitItemRangedIgnoreDisarmed = 10
    }

    [Flags]
    public enum TransmogIllusionFlags
    {
        HideUntilCollected = 0x1,
        PlayerConditionGrantsOnLogin = 0x2,
    }
}
