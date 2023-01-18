// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    public enum QuestObjectiveType
    {
        Monster = 0,
        Item = 1,
        GameObject = 2,
        TalkTo = 3,
        Currency = 4,
        LearnSpell = 5,
        MinReputation = 6,
        MaxReputation = 7,
        Money = 8,
        PlayerKills = 9,
        AreaTrigger = 10,
        WinPetBattleAgainstNpc = 11,
        DefeatBattlePet = 12,
        WinPvpPetBattles = 13,
        CriteriaTree = 14,
        ProgressBar = 15,
        HaveCurrency = 16,      // requires the player to have X currency when turning in but does not consume it
        ObtainCurrency = 17,    // requires the player to gain X currency after starting the quest but not required to keep it until the end (does not consume)
        IncreaseReputation = 18,// requires the player to gain X reputation with a faction
        AreaTriggerEnter = 19,
        AreaTriggerExit = 20,
        Max
    }

    public enum QuestObjectiveFlags
    {
        TrackedOnMinimap = 0x01, // Client Displays Large Yellow Blob On Minimap For Creature/Gameobject
        Sequenced = 0x02, // Client Will Not See The Objective Displayed Until All Previous Objectives Are Completed
        Optional = 0x04, // Not Required To Complete The Quest
        Hidden = 0x08, // Never Displayed In Quest Log
        HideCreditMsg = 0x10, // Skip Showing Item Objective Progress
        PreserveQuestItems = 0x20,
        PartOfProgressBar = 0x40, // Hidden Objective Used To Calculate Progress Bar Percent (Quests Are Limited To A Single Progress Bar Objective)
        KillPlayersSameFaction = 0x80
    }

    public struct QuestSlotOffsets
    {
        public const int Id = 0;
        public const int State = 1;
        public const int Counts = 2;
        public const int Time = 14;
        public const int Max = 16;
    }

    public enum QuestSlotStateMask
    {
        None = 0x0000,
        Complete = 0x0001,
        Fail = 0x0002
    }

    public enum QuestType
    {
        AutoComplete= 0,
        Disabled = 1,
        Normal = 2,
        Task = 3,
        Max = 4
    }

    public enum QuestInfos
    {
        Group = 1,
        Class = 21,
        Pvp = 41,
        Raid = 62,
        Dungeon = 81,
        WorldEvent = 82,
        Legendary = 83,
        Escort = 84,
        Heroic = 85,
        Raid10 = 88,
        Raid25 = 89,
        Scenario = 98,
        Account = 102,
        SideQuest = 104,
        Artifact = 107,
        WorldQuest = 109,
        EpicWorldQuest = 110,
        EliteWorldQuest = 111,
        EpicEliteWorldQuest = 112,
        PvpWorldQuest = 113,
        FirstAidWorldQuest = 114,
        BattlePetWorldQuest = 115,
        BlacksmithingWorldQuest = 116,
        LeatherworkingWorldQuest = 117,
        AlchemyWorldQuest = 118,
        HerbalismWorldQuest = 119,
        MiningWorldQuest = 120,
        TailoringWorldQuest = 121,
        EngineeringWorldQuest = 122,
        EnchantingWorldQuest = 123,
        SkinningWorldQuest = 124,
        JewelcraftingWorldQuest = 125,
        InscriptionWorldQuest = 126,
        EmissaryQuest = 128,
        ArcheologyWorldQuest = 129,
        FishingWorldQuest = 130,
        CookingWorldQuest = 131,
        RareWorldQuest = 135,
        RareEliteWorldQuest = 136,
        DungeonWorldQuest = 137,
        LegionInvasionWorldQuest = 139,
        RatedReward = 140,
        RaidWorldQuest = 141,
        LegionInvasionEliteWorldQuest = 142,
        LegionfallContribution = 143,
        LegionfallWorldQuest = 144,
        LegionfallDungeonWorldQuest = 145,
        LegionInvasionWorldQuestWrapper = 146,
        WarfrontBarrens = 147,
        Pickpocketing = 148,
        MagniWorldQuestAzerite = 151,
        TortollanWorldQuest = 152,
        WarfrontContribution = 153,
        IslandQuest = 254,
        WarMode = 255,
        PvpConquest = 256,
        FactionAssaultWorldQuest = 259,
        FactionAssaultEliteWorldQuest = 260,
        IslandWeeklyQuest = 261,
        PublicQuest = 263,
        ThreatObjective = 264,
        HiddenQuest = 265,
        CombatAllyQuest = 266,
        Professions = 267,
        ThreatWrapper = 268,
        ThreatEmissaryQuest = 270,
        CallingQuest = 271,
        VenthyrPartyQuest = 272,
        MawSoulSpawnTracker = 273
    }

    public enum QuestSort
    {
        Epic = 1,
        HallowsEnd = 21,
        Seasonal = 22,
        Cataclysm = 23,
        Herbalism = 24,
        Battlegrounds = 25,
        DayOfTheDead = 41,
        Warlock = 61,
        Warrior = 81,
        Shaman = 82,
        Fishing = 101,
        Blacksmithing = 121,
        Paladin = 141,
        Mage = 161,
        Rogue = 162,
        Alchemy = 181,
        Leatherworking = 182,
        Engineering = 201,
        TreasureMap = 221,
        Tournament = 241,
        Hunter = 261,
        Priest = 262,
        Druid = 263,
        Tailoring = 264,
        Special = 284,
        Cooking = 304,
        FirstAid = 324,
        Legendary = 344,
        DarkmoonFaire = 364,
        AhnQirajWar = 365,
        LunarFestival = 366,
        Reputation = 367,
        Invasion = 368,
        Midsummer = 369,
        Brewfest = 370,
        Inscription = 371,
        DeathKnight = 372,
        Jewelcrafting = 373,
        Noblegarden = 374,
        PilgrimsBounty = 375,
        LoveIsInTheAir = 376,
        Archaeology = 377,
        ChildrensWeek = 378,
        FirelandsInvasion = 379,
        TheZandalari = 380,
        ElementalBonds = 381,
        PandarenBrewmaster = 391,
        Scenario = 392,
        BattlePets = 394,
        Monk = 395,
        Landfall = 396,
        PandarenCampaign = 397,
        Riding = 398,
        BrawlersGuild = 399,
        ProvingGrounds = 400,
        GarrisonCampaign = 401,
        AssaultOnTheDarkPortal = 402,
        GarrisonSupport = 403,
        Logging = 404,
        Pickpocketing = 405,
        Artifact = 406,
        DemonHunter = 407,
        Mining = 408,
        WeekendEvent = 409,
        Enchanting = 410,
        Skinning = 411,
        WorldQuest = 412,
        DeathKnightCampaign = 413,
        DemonHunterCampaign = 416,
        DruidCampaign = 417,
        HunterCampaign = 418,
        MonkCampaign = 419,
        MageCampaign = 420,
        PriestCampaign = 421,
        PaladinCampaign = 422,
        ShamanCampaign = 423,
        RogueCampaign = 424,
        WarlockCampaign = 425,
        WarriorCampaign = 426,
        OrderHall = 427,
        LegionfallCampaign = 428,
        TheHuntForIllidanStormrage = 429,
        PiratesDay = 430,
        ArgusExpedition = 431,
        Warfronts = 432,
        MoonkinFestival = 433,
        TheKingsPath = 434,
        TheDeathsOfChromie = 435,
        RocketChicken = 436,
        LightforgedDraenei = 437,
        HighmountainTauren = 438,
        VoidElf = 439,
        Nightborne = 440,
        Dungeon = 441,
        Raid = 442,
        AlliedRaces = 444,
        TheWarchiefsAgenda = 445,
        AdventureJourney = 446,
        AllianceWarCampaign = 447,
        HordeWarCampaign = 448,
        DarkIronDwarf = 449,
        MagharOrc = 450,
        TheShadowHunter = 451,
        IslandExpeditions = 453,
        WorldPvp = 555,
        ThePrideOfKulTiras = 556,
        RatedPvp = 557,
        ZandalariTroll = 559,
        Heritage = 560,
        Questfall = 561,
        TyrandesVengeance = 562,
        TheFateOfSaurfang = 563,
        FreeTshirtDay = 564,
        CrucibleOfStorms = 565,
        KulTiran = 566,
        Assault = 567,
        HeartOfAzeroth = 569,
        Professions = 571,
        NazjatarFollowers = 573,
        Sinfall = 574,
        KorraksRevenge = 575,
        CovenantSanctum = 576,
        ReferAFriend = 579,
        VisionsOfNzoth = 580,
        Vulpera = 582,
        Mechagnome = 583,
        BlackEmpireCampaign = 584,
        EmberCourt = 586,
        ThroughTheShatteredSky = 587,
        DeathRising = 588,
        KyrianCallings = 589,
        NightFaeCallings = 590,
        NecrolordCallings = 591,
        VenthyrCallings = 592,
        AbominableStitching = 593,
        TimewalkingCampaign = 594,
        PathOfAscension = 595,
        LegendaryCrafting = 596,
        Campaign91 = 600,
        CyphersOfTheFirstOnes = 601,
        ZerethMortisCampaign = 602,
        TheArchivistsCodex = 603,
        CovenantAssaults = 604,
        ProtoformSynthesis = 606,
        Ch6SymbolTracking = 607,
    }

    public enum QuestFailedReasons
    {
        None = 0,
        FailedLowLevel = 1,        // "You Are Not High Enough Level For That Quest.""
        FailedWrongRace = 6,        // "That Quest Is Not Available To Your Race."
        AlreadyDone = 7,        // "You Have Completed That Daily Quest Today."
        OnlyOneTimed = 12,       // "You Can Only Be On One Timed Quest At A Time"
        AlreadyOn1 = 13,       // "You Are Already On That Quest"
        FailedExpansion = 16,       // "This Quest Requires An Expansion Enabled Account."
        AlreadyOn2 = 18,       // "You Are Already On That Quest"
        FailedMissingItems = 21,       // "You Don'T Have The Required Items With You.  Check Storage."
        FailedNotEnoughMoney = 23,       // "You Don'T Have Enough Money For That Quest"
        FailedCais = 24,       // "You Cannot Complete Quests Once You Have Reached Tired Time"
        AlreadyDoneDaily = 26,       // "You Have Completed That Daily Quest Today."
        FailedSpell = 28,       // "You Haven'T Learned The Required Spell."
        HasInProgress = 30        // "Progress Bar Objective Not Completed"
    }

    public enum QuestPushReason
    {
        Success = 0,    // "Sharing quest with %s..."
        Invalid = 1,    // "%s is not eligible for that quest"
        InvalidToRecipient = 2,    // "%s's attempt to share quest "%s" failed. You are not eligible for that quest."
        Accepted = 3,    // "%s has accepted your quest"
        Declined = 4,    // "%s has declined your quest"
        Busy = 5,    // "%s is busy"
        Dead = 6,    // "%s is dead."
        DeadToRecipient = 7,    // "%s's attempt to share quest "%s" failed. You are dead."
        LogFull = 8,    // "%s's quest log is full"
        LogFullToRecipient = 9,    // "%s's attempt to share quest "%s" failed. Your quest log is full."
        OnQuest = 10,   // "%s is already on that quest"
        OnQuestToRecipient = 11,   // "%s's attempt to share quest "%s" failed. You are already on that quest."
        AlreadyDone = 12,   // "%s has completed that quest"
        AlreadyDoneToRecipient = 13,   // "%s's attempt to share quest "%s" failed. You have completed that quest."
        NotDaily = 14,   // "That quest cannot be shared today"
        TimerExpired = 15,   // "Quest sharing timer has expired"
        NotInParty = 16,   // "You are not in a party"
        DifferentServerDaily = 17,   // "%s is not eligible for that quest today"
        DifferentServerDailyToRecipient = 18,   // "%s's attempt to share quest "%s" failed. You are not eligible for that quest today."
        NotAllowed = 19,   // "That quest cannot be shared"
        Prerequisite = 20,   // "%s hasn't completed all of the prerequisite quests required for that quest."
        PrerequisiteToRecipient = 21,   // "%s's attempt to share quest "%s" failed. You must complete all of the prerequisite quests first."
        LowLevel = 22,   // "%s is too low level for that quest."
        LowLevelToRecipient = 23,   // "%s's attempt to share quest "%s" failed. You are too low level for that quest."
        HighLevel = 24,   // "%s is too high level for that quest."
        HighLevelToRecipient = 25,   // "%s's attempt to share quest "%s" failed. You are too high level for that quest."
        Class = 26,   // "%s is the wrong class for that quest."
        ClassToRecipient = 27,   // "%s's attempt to share quest "%s" failed. You are the wrong class for that quest."
        Race = 28,   // "%s is the wrong race for that quest."
        RaceToRecipient = 29,   // "%s's attempt to share quest "%s" failed. You are the wrong race for that quest."
        LowFaction = 30,   // "%s's reputation is too low for that quest."
        LowFactionToRecipient = 31,   // "%s's attempt to share quest "%s" failed. Your reputation is too low for that quest."
        Expansion = 32,   // "%s doesn't own the required expansion for that quest."
        ExpansionToRecipient = 33,   // "%s's attempt to share quest "%s" failed. You do not own the required expansion for that quest."
        NotGarrisonOwner = 34,   // "%s must own a garrison to accept that quest."
        NotGarrisonOwnerToRecipient = 35,   // "%s's attempt to share quest "%s" failed. You must own a garrison to accept that quest."
        WrongCovenant = 36,   // "%s is in the wrong covenant for that quest."
        WrongCovenantToRecipient = 37,   // "%s's attempt to share quest "%s" failed. You are in the wrong covenant for that quest."
        NewPlayerExperience = 38,   // "%s must complete Exile's Reach to accept that quest."
        NewPlayerExperienceToRecipient = 39,   // "%s's attempt to share quest "%s" failed. You must complete Exile's Reach to accept that quest."
        WrongFaction = 40,   // "%s is the wrong faction for that quest."
        WrongFactionToRecipient = 41    // "%s's attempt to share quest "%s" failed. You are the wrong faction for that quest."
    }

    public enum QuestTradeSkill
    {
        None = 0,
        Alchemy = 1,
        Blacksmithing = 2,
        Cooking = 3,
        Enchanting = 4,
        Engineering = 5,
        Firstaid = 6,
        Herbalism = 7,
        Leatherworking = 8,
        Poisons = 9,
        Tailoring = 10,
        Mining = 11,
        Fishing = 12,
        Skinning = 13,
        Jewelcrafting = 14
    }

    public enum QuestStatus
    {
        None = 0,
        Complete = 1,
        //Unavailable    = 2,
        Incomplete = 3,
        //Available      = 4,
        Failed = 5,
        Rewarded = 6,        // Not Used In Db
        Max
    }

    public enum QuestGiverStatus : uint
    {
        None = 0x00,
        Future = 0x02,
        Trivial = 0x04,
        TrivialRepeatableTurnin = 0x08,
        TrivialDailyQuest = 0x10,
        Reward = 0x20,
        JourneyReward = 0x40,
        CovenantCallingReward = 0x80,
        RepeatableTurnin = 0x100,
        DailyQuest = 0x200,
        Quest = 0x400,
        RewardCompleteNoPOI = 0x800,
        RewardCompletePOI = 0x1000,
        LegendaryQuest = 0x2000,
        LegendaryRewardCompleteNoPOI = 0x4000,
        LegendaryRewardCompletePOI = 0x8000,
        JourneyQuest = 0x10000,
        JourneyRewardCompleteNoPOI = 0x20000,
        JourneyRewardCompletePOI = 0x40000,
        CovenantCallingQuest = 0x80000,
        CovenantCallingRewardCompleteNoPOI = 0x100000,
        CovenantCallingRewardCompletePOI = 0x200000,
    }

    [Flags]
    public enum QuestFlags : uint
    {
        None = 0x00,
        StayAlive = 0x01,               // Not Used Currently
        PartyAccept = 0x02,             // Not Used Currently. If Player In Party, All Players That Can Accept This Quest Will Receive Confirmation Box To Accept Quest Cmsg_Quest_Confirm_Accept/Smsg_Quest_Confirm_Accept
        Exploration = 0x04,             // Not Used Currently
        Sharable = 0x08,                // Can Be Shared: Player.Cansharequest()
        HasCondition = 0x10,            // Not Used Currently
        HideRewardPoi = 0x20,           // Not Used Currently: Unsure Of Content
        Raid = 0x40,                    // Can be completed while in raid
        WarModeRewardsOptIn = 0x80,     // Not Used Currently
        NoMoneyFromXp = 0x100,          // Not Used Currently: Experience Is Not Converted To Gold At Max Level
        HiddenRewards = 0x200,          // Items And Money Rewarded Only Sent In Smsg_Questgiver_Offer_Reward (Not In Smsg_Questgiver_Quest_Details Or In Client Quest Log(Smsg_Quest_Query_Response))
        Tracking = 0x400,               // These Quests Are Automatically Rewarded On Quest Complete And They Will Never Appear In Quest Log Client Side.
        DeprecateReputation = 0x800,    // Not Used Currently
        Daily = 0x1000,                 // Used To Know Quest Is Daily One
        Pvp = 0x2000,                   // Having This Quest In Log Forces Pvp Flag
        Unavailable = 0x4000,           // Used On Quests That Are Not Generically Available
        Weekly = 0x8000,
        AutoComplete = 0x10000,         // Quests with this flag player submit automatically by special button in player gui
        DisplayItemInTracker = 0x20000, // Displays Usable Item In Quest Tracker
        ObjText = 0x40000,              // Use Objective Text As Complete Text
        AutoAccept = 0x80000,           // The client recognizes this flag as auto-accept.
        PlayerCastOnAccept = 0x100000,
        PlayerCastOnComplete = 0x200000,
        UpdatePhaseShift = 0x400000,
        SorWhitelist = 0x800000,
        LaunchGossipComplete = 0x1000000,
        RemoveExtraGetItems = 0x2000000,
        HideUntilDiscovered = 0x4000000,
        PortraitInQuestLog = 0x8000000,
        ShowItemWhenCompleted = 0x10000000,
        LaunchGossipAccept = 0x20000000,
        ItemsGlowWhenDone = 0x40000000,
        FailOnLogout = 0x80000000
    }

    [Flags]
    public enum QuestFlagsEx : uint
    {
        None = 0x00,
        KeepAdditionalItems = 0x01,
        SuppressGossipComplete = 0x02,
        SuppressGossipAccept = 0x04,
        DisallowPlayerAsQuestgiver = 0x08,
        DisplayClassChoiceRewards = 0x10,
        DisplaySpecChoiceRewards = 0x20,
        RemoveFromLogOnPeriodicReset = 0x40,
        AccountLevelQuest = 0x80,
        LegendaryQuest = 0x100,
        NoGuildXp = 0x200,
        ResetCacheOnAccept = 0x400,
        NoAbandonOnceAnyObjectiveComplete = 0x800,
        RecastAcceptSpellOnLogin = 0x1000,
        UpdateZoneAuras = 0x2000,
        NoCreditForProxy = 0x4000,
        DisplayAsDailyQuest = 0x8000,
        PartOfQuestLine = 0x10000,
        QuestForInternalBuildsOnly = 0x20000,
        SuppressSpellLearnTextLine = 0x40000,
        DisplayHeaderAsObjectiveForTasks = 0x80000,
        GarrisonNonOwnersAllowed = 0x100000,
        RemoveQuestOnWeeklyReset = 0x200000,
        SuppressFarewellAudioAfterQuestAccept = 0x400000,
        RewardsBypassWeeklyCapsAndSeasonTotal = 0x800000,
        IsWorldQuest = 0x1000000,
        NotIgnorable = 0x2000000,
        AutoPush = 0x4000000,
        NoSpellCompleteEffects = 0x8000000,
        DoNotToastHonorReward = 0x10000000,
        KeepRepeatableQuestOnFactionChange = 0x20000000,
        KeepProgressOnFactionChange = 0x40000000,
        PushTeamQuestUsingMapController = 0x80000000
    }

    public enum QuestFlagsEx2
    {
        ResetOnGameMilestone = 0x01,
        NoWarModeBonus = 0x02,
        AwardHighestProfession = 0x04,
        NotReplayable = 0x08,
        NoReplayRewards = 0x10,
        DisableWaypointPathing = 0x20,
        ResetOnMythicPlusSeason = 0x40,
        ResetOnPvpSeason = 0x80,
        EnableOverrideSortOrder = 0x100,
        ForceStartingLocOnZoneMap = 0x200,
        BonusLootNever = 0x400,
        BonusLootAlways = 0x800,
        HideTaskOnMainMap = 0x1000,
        HideTaskInTracker = 0x2000,
        SkipDisabledCheck = 0x4000,
        EnforceMaximumQuestLevel = 0x8000
    }

    public enum QuestSpecialFlags
    {
        None = 0x00,
        // Flags For Set Specialflags In Db If Required But Used Only At Server
        Repeatable = 0x001,
        ExplorationOrEvent = 0x002, // If Required Area Explore, Spell Spell_Effect_Quest_Complete Casting, Table `*_Script` Command Script_Command_Quest_Explored Use, Set From Script)
        AutoAccept = 0x004, // Quest Is To Be Auto-Accepted.
        DfQuest = 0x008, // Quest Is Used By Dungeon Finder.
        Monthly = 0x010, // Quest Is Reset At The Begining Of The Month
        // Room For More Custom Flags

        DbAllowed = Repeatable | ExplorationOrEvent | AutoAccept | DfQuest | Monthly,

        SequencedObjectives = 0x20 // Internal flag computed only
    }

    public enum QuestSaveType
    {
        Default = 0,
        Delete,
        ForceDelete
    }

    public enum QuestTagType
    {
        Tag,
        Profession,
        Normal,
        Pvp,
        PetBattle,
        Bounty,
        Dungeon,
        Invasion,
        Raid,
        Contribution,
        RatedRreward,
        InvasionWrapper,
        FactionAssault,
        Islands,
        Threat,
        CovenantCalling
    }
}
