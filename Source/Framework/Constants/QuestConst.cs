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
        HaveCurrency = 16,   // requires the player to have X currency when turning in but does not consume it
        ObtainCurrency = 17    // requires the player to gain X currency after starting the quest but not required to keep it until the end (does not consume)
    }

    public enum QuestObjectiveFlags
    {
        TrackedOnMinimap = 0x01, // Client Displays Large Yellow Blob On Minimap For Creature/Gameobject
        Sequenced = 0x02, // Client Will Not See The Objective Displayed Until All Previous Objectives Are Completed
        Optional = 0x04, // Not Required To Complete The Quest
        Hidden = 0x08, // Never Displayed In Quest Log
        HideItemGains = 0x10, // Skip Showing Item Objective Progress
        ProgressCountsItemsInInventory = 0x20, // Item Objective Progress Counts Items In Inventory Instead Of Reading It From Updatefields
        PartOfProgressBar = 0x40, // Hidden Objective Used To Calculate Progress Bar Percent (Quests Are Limited To A Single Progress Bar Objective)
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
        PVP = 41,
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
        SideQuest = 104
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
        Pickpocketing = 405
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
        Success = 0,
        Invalid = 1,
        Accepted = 2,
        Declined = 3,
        Busy = 4,
        Dead = 5,
        LogFull = 6,
        OnQuest = 7,
        AlreadyDone = 8,
        NotDaily = 9,
        TimerExpired = 10,
        NotInParty = 11,
        DifferentServerDaily = 12,
        NotAllowed = 13
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

    public enum QuestGiverStatus
    {
        None = 0x000,
        Unk = 0x001,
        Unavailable = 0x002,
        LowLevelAvailable = 0x004,
        LowLevelRewardRep = 0x008,
        LowLevelAvailableRep = 0x010,
        Incomplete = 0x020,
        RewardRep = 0x040,
        AvailableRep = 0x080,
        Available = 0x100,
        Reward2 = 0x200,         // No Yellow Dot On Minimap
        Reward = 0x400,          // Yellow Dot On Minimap

        // Custom value meaning that script call did not return any valid quest status
        ScriptedNoStatus = 0x1000
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
        TBC = 0x80,                     // Not Used Currently: Available If Tbc Expansion Enabled Only
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

    // last checked in 19802
    [Flags]
    public enum QuestFlagsEx
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
        SuppressFarewellAudioAfterQuestAccept = 0x0400000,
        RewardsBypassWeeklyCapsAndSeasonTotal = 0x0800000,
        ClearProgressOfCriteriaTreeObjectivesOnAccept = 0x1000000
    }

    public enum QuestFlagsEx2
    {
        NoWarModeBonus = 0x2
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
        Cast = 0x20, // Set by 32 in SpecialFlags in DB if the quest requires RequiredOrNpcGo killcredit but NOT kill (a spell cast)
        // Room For More Custom Flags

        DbAllowed = Repeatable | ExplorationOrEvent | AutoAccept | DfQuest | Monthly | Cast,

        Deliver = 0x080,   // Internal Flag Computed Only
        Speakto = 0x100,   // Internal Flag Computed Only
        Kill = 0x200,   // Internal Flag Computed Only
        Timed = 0x400,   // Internal Flag Computed Only
        PlayerKill = 0x800    // Internal Flag Computed Only
    }

    public enum QuestSaveType
    {
        Default = 0,
        Delete,
        ForceDelete
    }
}
