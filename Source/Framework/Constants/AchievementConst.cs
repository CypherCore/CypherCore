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
    public enum AchievementFaction : sbyte
    {
        Horde = 0,
        Alliance = 1,
        Any = -1,
    }

    public enum CriteriaTreeFlags : ushort
    {
        ProgressBar = 0x0001,
        ProgressIsDate = 0x0004,
        ShowCurrencyIcon = 0x0008,
        AllianceOnly = 0x0200,
        HordeOnly = 0x0400,
        ShowRequiredCount = 0x0800
    }

    public enum CriteriaTreeOperator
    {
        Single = 0,
        SinglerNotCompleted = 1,
        All = 4,
        SumChildren = 5,
        MaxChild = 6,
        CountDirectChildren = 7,
        Any = 8,
        SumChildrenWeight = 9
    }

    [Flags]
    public enum AchievementFlags
    {
        Counter = 0x01,
        Hidden = 0x02,
        PlayNoVisual = 0x04,
        Summ = 0x08,
        MaxUsed = 0x10,
        ReqCount = 0x20,
        Average = 0x40,
        Bar = 0x80,
        RealmFirstReach = 0x100,
        RealmFirstKill = 0x200,
        Unk3 = 0x400,
        HideIncomplete = 0x800,
        ShowInGuildNews = 0x1000,
        ShowInGuildHeader = 0x2000,
        Guild = 0x4000,
        ShowGuildMembers = 0x8000,
        ShowCriteriaMembers = 0x10000,
        Account = 0x20000,
        Unk5 = 0x00040000,
        HideZeroCounter = 0x00080000,
        TrackingFlag = 0x00100000
    }

    [Flags]
    public enum CriteriaFlagsCu
    {
        Player = 0x1,
        Account = 0x2,
        Guild = 0x4,
        Scenario = 0x8,
        QuestObjective = 0x10
    }

    public enum CriteriaCondition
    {
        None = 0,
        NoDeath = 1,
        Unk2 = 2,
        BgMap = 3,
        NoLose = 4,
        Unk5 = 5,
        Unk8 = 8,
        NoSpellHit = 9,
        NotInGroup = 10,
        Unk13 = 13
    }

    public enum CriteriaAdditionalCondition
    {
        SourceDrunkValue = 1,
        Unk2 = 2,
        ItemLevel = 3,
        TargetCreatureEntry = 4,
        TargetMustBePlayer = 5,
        TargetMustBeDead = 6,
        TargetMustBeEnemy = 7,
        SourceHasAura = 8,
        TargetHasAura = 10,
        TargetHasAuraType = 11,
        ItemQualityMin = 14,
        ItemQualityEquals = 15,
        Unk16 = 16,
        SourceAreaOrZone = 17,
        TargetAreaOrZone = 18,
        MapDifficultyOld = 20,
        TargetCreatureYieldsXp = 21,
        ArenaType = 24,
        SourceRace = 25,
        SourceClass = 26,
        TargetRace = 27,
        TargetClass = 28,
        MaxGroupMembers = 29,
        TargetCreatureType = 30,
        SourceMap = 32,
        ItemClass = 33,
        ItemSubclass = 34,
        CompleteQuestNotInGroup = 35,
        MinPersonalRating = 37,
        TitleBitIndex = 38,
        SourceLevel = 39,
        TargetLevel = 40,
        TargetZone = 41,
        TargetHealthPercentBelow = 46,
        Unk55 = 55,
        MinAchievementPoints = 56,
        RequiresLfgGroup = 58,
        Unk60 = 60,
        RequiresGuildGroup = 61,
        GuildReputation = 62,
        RatedBattleground = 63,
        RatedBattlegroundRating = 64,
        ProjectRarity = 65,
        ProjectRace = 66,
        WorldState = 67, // Nyi
        MapDifficulty = 68, // Nyi
        PlayerLevel = 69, // Nyi
        TargetPlayerLevel = 70, // Nyi
        //PlayerLevelOnAccount       = 71, // Not Verified
        //Unk73       = 73, // References Another Modifier Tree Id
        ScenarioId = 74, // Nyi
        BattlePetFamily = 78, // Nyi
        BattlePetHealthPct = 79, // Nyi
        //Unk80                         = 80 // Something To Do With World Bosses
        BattlePetEntry = 81, // Nyi
        //BattlePetEntryId           = 82, // Some Sort Of Data Id?
        ChallengeModeMedal = 83, // NYI
        //CRITERIA_ADDITIONAL_CONDITION_UNK84                         = 84, // Quest id
        //CRITERIA_ADDITIONAL_CONDITION_UNK86                         = 86, // Some external event id
        //CRITERIA_ADDITIONAL_CONDITION_UNK87                         = 87, // Achievement id
        BattlePetSpecies = 91,
        Expansion = 92,
        GarrisonFollowerEntry = 144,
        GarrisonFollowerQuality = 145,
        GarrisonFollowerLevel = 146,
        GarrisonRareMission = 147, // NYI
        GarrisonBuildingLevel = 149, // NYI
        GarrisonMissionType = 167, // NYI
        PLayerItemLevel = 169, // NYI
        GarrisonFollowILvl = 184,
        HonorLevel = 193,
        PrestigeLevel = 194
    }

    public enum CriteriaFlags
    {
        ShowProgressBar = 0x01,
        Hidden = 0x02,
        FailAchievement = 0x04,
        ResetOnStart = 0x08,
        IsDate = 0x10,
        MoneyCounter = 0x20
    }

    public enum CriteriaTimedTypes : byte
    {
        Event = 1,    // Timer Is Started By Internal Event With Id In Timerstartevent
        Quest = 2,    // Timer Is Started By Accepting Quest With Entry In Timerstartevent
        SpellCaster = 5,    // Timer Is Started By Casting A Spell With Entry In Timerstartevent
        SpellTarget = 6,    // Timer Is Started By Being Target Of Spell With Entry In Timerstartevent
        Creature = 7,    // Timer Is Started By Killing Creature With Entry In Timerstartevent
        Item = 9,    // Timer Is Started By Using Item With Entry In Timerstartevent
        Unk = 10,   // Unknown
        Unk2 = 13,   // Unknown
        ScenarioStage = 14,   // Timer is started by changing stages in a scenario

        Max
    }

    public enum CriteriaTypes : byte
    {
        KillCreature = 0,
        WinBg = 1,
        // 2 - unused (Legion - 23420)
        CompleteArchaeologyProjects = 3, // Struct { Uint32 Itemcount; }
        SurveyGameobject = 4,
        ReachLevel = 5,
        ClearDigsite = 6,
        ReachSkillLevel = 7,
        CompleteAchievement = 8,
        CompleteQuestCount = 9,
        CompleteDailyQuestDaily = 10, // You Have To Complete A Daily Quest X Times In A Row
        CompleteQuestsInZone = 11,
        Currency = 12,
        DamageDone = 13,
        CompleteDailyQuest = 14,
        CompleteBattleground = 15,
        DeathAtMap = 16,
        Death = 17,
        DeathInDungeon = 18,
        CompleteRaid = 19,
        KilledByCreature = 20,
        ManualCompleteCriteria = 21,
        CompleteChallengeModeGuild = 22,
        KilledByPlayer = 23,
        FallWithoutDying = 24,
        // 25 - unused (Legion - 23420)
        DeathsFrom = 26,
        CompleteQuest = 27,
        BeSpellTarget = 28,
        CastSpell = 29,
        BgObjectiveCapture = 30,
        HonorableKillAtArea = 31,
        WinArena = 32,
        PlayArena = 33,
        LearnSpell = 34,
        HonorableKill = 35,
        OwnItem = 36,
        WinRatedArena = 37,
        HighestTeamRating = 38,
        HighestPersonalRating = 39,
        LearnSkillLevel = 40,
        UseItem = 41,
        LootItem = 42,
        ExploreArea = 43,
        OwnRank = 44,
        BuyBankSlot = 45,
        GainReputation = 46,
        GainExaltedReputation = 47,
        VisitBarberShop = 48,
        EquipEpicItem = 49,
        RollNeedOnLoot = 50, /// Todo Itemlevel Is Mentioned In Text But Not Present In Dbc
        RollGreedOnLoot = 51,
        HkClass = 52,
        HkRace = 53,
        DoEmote = 54,
        HealingDone = 55,
        GetKillingBlows = 56, /// Todo In Some Cases Map Not Present, And In Some Cases Need Do Without Die
        EquipItem = 57,
        // 58 - unused (Legion - 23420)
        MoneyFromVendors = 59,
        GoldSpentForTalents = 60,
        NumberOfTalentResets = 61,
        MoneyFromQuestReward = 62,
        GoldSpentForTravelling = 63,
        DefeatCreatureGroup = 64,
        GoldSpentAtBarber = 65,
        GoldSpentForMail = 66,
        LootMoney = 67,
        UseGameobject = 68,
        BeSpellTarget2 = 69,
        SpecialPvpKill = 70,
        CompleteChallengeMode = 71,
        FishInGameobject = 72,
        SendEvent = 73,
        OnLogin = 74,
        LearnSkilllineSpells = 75,
        WinDuel = 76,
        LoseDuel = 77,
        KillCreatureType = 78,
        CookRecipesGuild = 79,
        GoldEarnedByAuctions = 80,
        EarnPetBattleAchievementPoints = 81,
        CreateAuction = 82,
        HighestAuctionBid = 83,
        WonAuctions = 84,
        HighestAuctionSold = 85,
        HighestGoldValueOwned = 86,
        GainReveredReputation = 87,
        GainHonoredReputation = 88,
        KnownFactions = 89,
        LootEpicItem = 90,
        ReceiveEpicItem = 91,
        SendEventScenario = 92,
        RollNeed = 93,
        RollGreed = 94,
        ReleaseSpirit = 95,
        OwnPet = 96,
        GarrisonCompleteDungeonEncounter = 97,
        // 98 - unused (Legion - 23420)
        // 99 - unused (Legion - 23420)
        // 100 - unused (Legion - 23420)
        HighestHitDealt = 101,
        HighestHitReceived = 102,
        TotalDamageReceived = 103,
        HighestHealCasted = 104,
        TotalHealingReceived = 105,
        HighestHealingReceived = 106,
        QuestAbandoned = 107,
        FlightPathsTaken = 108,
        LootType = 109,
        CastSpell2 = 110, /// Todo Target Entry Is Missing
        // 111 - unused (Legion - 23420)
        LearnSkillLine = 112,
        EarnHonorableKill = 113,
        AcceptedSummonings = 114,
        EarnAchievementPoints = 115,
        // 116 - unused (Legion - 23420)
        // 117 - unused (Legion - 23420)
        CompleteLfgDungeon = 118,
        UseLfdToGroupWithPlayers = 119,
        LfgVoteKicksInitiatedByPlayer = 120,
        LfgVoteKicksNotInitByPlayer = 121,
        BeKickedFromLfg = 122,
        LfgLeaves = 123,
        SpentGoldGuildRepairs = 124,
        ReachGuildLevel = 125,
        CraftItemsGuild = 126,
        CatchFromPool = 127,
        BuyGuildBankSlots = 128,
        EarnGuildAchievementPoints = 129,
        WinRatedBattleground = 130,
        // 131 - unused (Legion - 23420)
        ReachBgRating = 132,
        BuyGuildTabard = 133,
        CompleteQuestsGuild = 134,
        HonorableKillsGuild = 135,
        KillCreatureTypeGuild = 136,
        CountOfLfgQueueBoostsByTank = 137,
        CompleteGuildChallengeType = 138, //Struct { Flag Flag; Uint32 Count; } 1: Guild Dungeon, 2:Guild Challenge, 3:Guild Battlefield
        CompleteGuildChallenge = 139,  //Struct { Uint32 Count; } Guild Challenge
        // 140 - 1 criteria (16883), unused (Legion - 23420)
        // 141 - 1 criteria (16884), unused (Legion - 23420)
        // 142 - 1 criteria (16881), unused (Legion - 23420)
        // 143 - 1 criteria (16882), unused (Legion - 23420)
        // 144 - 1 criteria (17386), unused (Legion - 23420)
        LfrDungeonsCompleted = 145,
        LfrLeaves = 146,
        LfrVoteKicksInitiatedByPlayer = 147,
        LfrVoteKicksNotInitByPlayer = 148,
        BeKickedFromLfr = 149,
        CountOfLfrQueueBoostsByTank = 150,
        CompleteScenarioCount = 151,
        CompleteScenario = 152,
        ReachAreatriggerWithActionset = 153,
        // 154 - unused (Legion - 23420)
        OwnBattlePet = 155,
        OwnBattlePetCount = 156,
        CaptureBattlePet = 157,
        WinPetBattle = 158,
        // 159 - 2 criterias (22312,22314), unused (Legion - 23420)
        LevelBattlePet = 160,
        CaptureBattlePetCredit = 161, // Triggers A Quest Credit
        LevelBattlePetCredit = 162, // Triggers A Quest Credit
        EnterArea = 163, // Triggers A Quest Credit
        LeaveArea = 164, // Triggers A Quest Credit
        CompleteDungeonEncounter = 165,
        // 166 - unused (Legion - 23420)
        PlaceGarrisonBuilding = 167,
        UpgradeGarrisonBuilding = 168,
        ConstructGarrisonBuilding = 169,
        UpgradeGarrison = 170,
        StartGarrisonMission = 171,
        StartOrderHallMission = 172,
        CompleteGarrisonMissionCount = 173,
        CompleteGarrisonMission = 174,
        RecruitGarrisonFollowerCount = 175,
        RecruitGarrisonFollower = 176,
        // 177 - 0 criterias (Legion - 23420)
        LearnGarrisonBlueprintCount = 178,
        // 179 - 0 criterias (Legion - 23420)
        // 180 - 0 criterias (Legion - 23420)
        // 181 - 0 criterias (Legion - 23420)
        CompleteGarrisonShipment = 182,
        RaiseGarrisonFollowerItemLevel = 183,
        RaiseGarrisonFollowerLevel = 184,
        OwnToy = 185,
        OwnToyCount = 186,
        RecruitGarrisonFollowerWithQuality = 187,
        // 188 - 0 criterias (Legion - 23420)
        OwnHeirlooms = 189,
        ArtifactPowerEarned = 190,
        ArtifactTraitsUnlocked = 191,
        HonorLevelReached = 194,
        PrestigeReached = 195,
        // 196 - CRITERIA_TYPE_REACH_LEVEL_2 or something
        // 197 - Order Hall Advancement related
        OrderHallTalentLearned = 198,
        AppearanceUnlockedBySlot = 199,
        OrderHallRecruitTroop = 200,
        // 201 - 0 criterias (Legion - 23420)
        // 202 - 0 criterias (Legion - 23420)
        CompleteWorldQuest = 203,
        // 204 - Special criteria type to award players for some external events? Comes with what looks like an identifier, so guessing it's not unique.
        TransmogSetUnlocked = 205,
        GainParagonReputation = 206,
        EarnHonorXp = 207,
        RelicTalentUnlocked = 211,
        ReachAccountHonorLevel = 213,
        HeartOfAzerothArtifactPowerEarned = 214,
        HeartOfAzerothLevelReached = 215,
        TotalTypes
    }

    public enum CriteriaDataType
    {
        None = 0,
        TCreature = 1,
        TPlayerClassRace = 2,
        TPlayerLessHealth = 3,
        SAura = 5,
        TAura = 7,
        Value = 8,
        TLevel = 9,
        TGender = 10,
        Script = 11,
        // Reuse
        MapPlayerCount = 13,
        TTeam = 14,
        SDrunk = 15,
        Holiday = 16,
        BgLossTeamScore = 17,
        InstanceScript = 18,
        SEquippedItem = 19,
        MapId = 20,
        SPlayerClassRace = 21,
        // Reuse
        SKnownTitle = 23,
        GameEvent = 24,
        SItemQuality = 25,

        Max = 25
    }

    public enum ProgressType
    {
        Set,
        Accumulate,
        Highest
    }
}
