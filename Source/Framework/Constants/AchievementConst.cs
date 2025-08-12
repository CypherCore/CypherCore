// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    public enum AchievementFaction : sbyte
    {
        Horde = 0,
        Alliance = 1,
        Any = -1,
    }

    public enum CriteriaTreeFlags
    {
        ProgressBar = 0x0001, // Progress Bar
        DoNotDisplay = 0x0002, // Do Not Display
        IsDate = 0x0004, // Is a Date
        IsMoney = 0x0008, // Is Money
        ToastOnComplete = 0x0010, // Toast on Complete
        UseObjectsDescription = 0x0020, // Use Object's Description
        ShowFactionSpecificChild = 0x0040, // Show faction specific child
        DisplayAllChildren = 0x0080, // Display all children
        AwardBonusRep = 0x0100, // Award Bonus Rep (Hack!!)
        AllianceOnly = 0x0200, // Treat this criteria or block as Alliance
        HordeOnly = 0x0400, // Treat this criteria or block as Horde
        DisplayAsFraction = 0x0800, // Display as Fraction
        IsForQuest = 0x1000  // Is For Quest
    }

    public enum CriteriaTreeOperator
    {
        Complete = 0, // Complete
        NotComplete = 1, // Not Complete
        CompleteAll = 4, // Complete All
        Sum = 5, // Sum Of Criteria Is
        Highest = 6, // Highest Criteria Is
        StartedAtLeast = 7, // Started At Least
        CompleteAtLeast = 8, // Complete At Least
        ProgressBar = 9  // Progress Bar
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

    public enum ModifierTreeType
    {
        None = 0,   // No modifier
        PlayerInebriationLevelEqualOrGreaterThan = 1,   // Player inebriation level is {#Drunkenness} or more
        PlayerMeetsCondition = 2,   // Player meets condition "{PlayerCondition}"
        MinimumItemLevel = 3,   // Minimum item level is {#Item Level}
        TargetCreatureId = 4,   // Target is NPC "{Creature}"
        TargetIsPlayer = 5,   // Target is player
        TargetIsDead = 6,   // Target is dead
        TargetIsOppositeFaction = 7,   // Target is opposite faction
        PlayerHasAura = 8,   // Player has aura "{Spell}"
        PlayerHasAuraEffect = 9,   // Player has aura effect "{SpellAuraNames.EnumID}"
        TargetHasAura = 10,  // Target has aura "{Spell}"
        TargetHasAuraEffect = 11,  // Target has aura effect "{SpellAuraNames.EnumID}"
        TargetHasAuraState = 12,  // Target has aura state "{$Aura State}"
        PlayerHasAuraState = 13,  // Player has aura state "{$Aura State}"
        ItemQualityIsAtLeast = 14,  // Item quality is at least {$Item Quality}
        ItemQualityIsExactly = 15,  // Item quality is exactly {$Item Quality}
        PlayerIsAlive = 16,  // Player is alive
        PlayerIsInArea = 17,  // Player is in area "{AreaTable}"
        TargetIsInArea = 18,  // Target is in area "{AreaTable}"
        ItemId = 19,  // Item is "{Item}"
        LegacyDungeonDifficulty = 20,  // Legacy dungeon difficulty is "{$Dungeon Difficulty}"
        PlayerToTargetLevelDeltaGreaterThan = 21,  // Exceeds the target's level by {#Level Delta} levels
        TargetToPlayerLevelDeltaGreaterThan = 22,  // Target exceeds your level by {#Level Delta} levels
        PlayerLevelEqualTargetLevel = 23,  // You and the target are equal level
        PlayerInArenaWithTeamSize = 24,  // Player is in an arena with team size {#Team Size}
        PlayerRace = 25,  // Player race is "{ChrRaces}"
        PlayerClass = 26,  // Player class is "{ChrClasses}"
        TargetRace = 27,  // Target race is "{ChrRaces}"
        TargetClass = 28,  // Target class is "{ChrClasses}"
        LessThanTappers = 29,  // Less than {#Tappers} tappers
        CreatureType = 30,  // Creature is type "{CreatureType}"
        CreatureFamily = 31,  // Creature is family "{CreatureFamily}"
        PlayerMap = 32,  // Player is on map "{Map}"
        ClientVersionEqualOrLessThan = 33,  // Milestone is at or before "{WowStaticSchemas}"
        BattlePetTeamLevel = 34,  // All three winning battle pets are at or above level {#Battle Pet Level}
        PlayerIsNotInParty = 35,  // Player is not in a party
        PlayerIsInParty = 36,  // Player is in a party
        HasPersonalRatingEqualOrGreaterThan = 37,  // Has a Personal Rating of at least {#Personal Rating}
        HasTitle = 38,  // Has title "{CharTitles.Mask_ID}"
        PlayerLevelEqual = 39,  // Player is exactly level {#Level}
        TargetLevelEqual = 40,  // Target is exactly level {#Level}
        PlayerIsInZone = 41,  // Player is in top-level area "{AreaTable}"
        TargetIsInZone = 42,  // Target is in top-level area "{AreaTable}"
        PlayerHealthBelowPercent = 43,  // Player health below {#Percent}%
        PlayerHealthAbovePercent = 44,  // Player health above {#Percent}%
        PlayerHealthEqualsPercent = 45,  // Player health equals {#Percent}%
        TargetHealthBelowPercent = 46,  // Target health below {#Percent}%
        TargetHealthAbovePercent = 47,  // Target health above {#Percent}%
        TargetHealthEqualsPercent = 48,  // Target health equals {#Percent}%
        PlayerHealthBelowValue = 49,  // Player health below {#Hit Points} HP
        PlayerHealthAboveValue = 50,  // Player health above {#Hit Points} HP
        PlayerHealthEqualsValue = 51,  // Player health equals {#Hit Points} HP
        TargetHealthBelowValue = 52,  // Target health below {#Hit Points} HP
        TargetHealthAboveValue = 53,  // Target health above {#Hit Points} HP
        TargetHealthEqualsValue = 54,  // Target health equals {#Hit Points} HP
        TargetIsPlayerAndMeetsCondition = 55,  // Target is a player with condition "{PlayerCondition}"
        PlayerHasMoreThanAchievementPoints = 56,  // Player has over {#Achievement Pts} achievement points
        PlayerInLfgDungeon = 57,  // Player is in a LFG dungeon
        PlayerInRandomLfgDungeon = 58,  // Player is in a random LFG dungeon
        PlayerInFirstRandomLfgDungeon = 59,  // Player is in a first random LFG dungeon
        PlayerInRankedArenaMatch = 60,  // Player is in a ranked arena match
        PlayerInGuildParty = 61,  /*NYI*/ // Player is in a guild party
        PlayerGuildReputationEqualOrGreaterThan = 62,  // Player has guild reputation of {#Guild Reputation} or more
        PlayerInRatedBattleground = 63,  // Player is in rated battleground
        PlayerBattlegroundRatingEqualOrGreaterThan = 64,  // Player has a battleground rating of {#Battleground Rating} or more
        ResearchProjectRarity = 65,  /*NYI*/ // Research project rarity is "{$Project Rarity}"
        ResearchProjectBranch = 66,  /*NYI*/ // Research project is in branch "{ResearchBranch}"
        WorldStateExpression = 67,  // World state expression "{WorldStateExpression}" is true
        DungeonDifficulty = 68,  // Dungeon difficulty is "{Difficulty}"
        PlayerLevelEqualOrGreaterThan = 69,  // Player level is {#Level} or more
        TargetLevelEqualOrGreaterThan = 70,  // Target level is {#Level} or more
        PlayerLevelEqualOrLessThan = 71,  // Player level is {#Level} or less
        TargetLevelEqualOrLessThan = 72,  // Target level is {#Level} or less
        ModifierTree = 73,  // Modifier tree "{ModifierTree}" is also true
        PlayerScenario = 74,  // Player is on scenario "{Scenario}"
        TillersReputationGreaterThan = 75,  // Reputation with Tillers is above {#Reputation}
        BattlePetAchievementPointsEqualOrGreaterThan = 76,  // Battle pet achievement points are at least {#Achievement Pts}
        UniqueBattlePetsEqualOrGreaterThan = 77,  // (Account) At least {#Pets Known} unique pets known
        BattlePetType = 78,  // Battlepet is of type "{$Battle Pet Types}"
        BattlePetHealthPercentLessThan = 79,  /*NYI*/ // (Account) Battlepet's health is below {#Health Percent} percent
        GuildGroupMemberCountEqualOrGreaterThan = 80,  // Be in a group with at least {#Members} guild members
        BattlePetOpponentCreatureId = 81,  /*NYI*/ // Battle pet opponent is "{Creature}"
        PlayerScenarioStep = 82,  // Player is on scenario step number {#Step Number}
        ChallengeModeMedal = 83,  // Challenge mode medal earned is "{#Challenge Mode Medal(OBSOLETE)}" (OBSOLETE)
        PlayerOnQuest = 84,  // Player is currently on the quest "{QuestV2}"
        ExaltedWithFaction = 85,  // Reach exalted with "{Faction}"
        EarnedAchievementOnAccount = 86,  // Earned achievement "{Achievement}" on this account
        EarnedAchievementOnPlayer = 87,  // Earned achievement "{Achievement}" on this player
        OrderOfTheCloudSerpentReputationGreaterThan = 88,  // Reputation with Order of the Cloud Serpent is above {#Reputation}
        BattlePetQuality = 89,  /*NYI*/ // Battle pet is of quality "{BattlePetBreedQuality}"
        BattlePetFightWasPVP = 90,  /*NYI*/ // Battle pet fight was PVP
        BattlePetSpecies = 91,  // Battle pet is species type "{BattlePetSpecies}"
        ServerExpansionEqualOrGreaterThan = 92,  // Server expansion level is "{$Expansion Level}" or higher
        PlayerHasBattlePetJournalLock = 93,  // Player has battle pet journal lock
        FriendshipRepReactionIsMet = 94,  // Friendship rep reaction "{FriendshipRepReaction}" is met
        ReputationWithFactionIsEqualOrGreaterThan = 95,  // Reputation with "{Faction}" is {#Reputation} or more
        ItemClassAndSubclass = 96,  // Item is class "{ItemClass.ClassID}", subclass "{^ItemSubclass.SubclassID:ItemSubclass.ClassID = ?}"
        PlayerGender = 97,  // Player's gender is "{$Gender}"
        PlayerNativeGender = 98,  // Player's native gender is "{$Gender}"
        PlayerSkillEqualOrGreaterThan = 99,  // Player skill "{SkillLine}" is level {#Skill Level} or higher
        PlayerLanguageSkillEqualOrGreaterThan = 100, // Player language "{Languages}" is level {#Language Level} or higher
        PlayerIsInNormalPhase = 101, // Player is in normal phase
        PlayerIsInPhase = 102, // Player is in phase "{Phase}"
        PlayerIsInPhaseGroup = 103, // Player is in phase group "{PhaseGroup}"
        PlayerKnowsSpell = 104, // Player knows spell "{Spell}"
        PlayerHasItemQuantity = 105, // Player is carrying item "{Item}", quantity {#Quantity}
        PlayerExpansionLevelEqualOrGreaterThan = 106, // Player expansion level is "{$Expansion Level}" or higher
        PlayerHasAuraWithLabel = 107, // Player has aura with label {Label}
        PlayersRealmWorldState = 108, // Player's realm state "{WorldState}" equals {#Value}
        TimeBetween = 109, // Time is between "{/Begin Date}" and "{/End Date}"
        PlayerHasCompletedQuest = 110, // Player has previously completed quest "{QuestV2}"
        PlayerIsReadyToTurnInQuest = 111, // Player is ready to turn in quest "{QuestV2}"
        PlayerHasCompletedQuestObjective = 112, // Player has completed Quest Objective "{QuestObjective}"
        PlayerHasExploredArea = 113, // Player has explored area "{AreaTable}"
        PlayerHasItemQuantityIncludingBank = 114, // Player or bank has item "{Item}", quantity {#Quantity}
        Weather = 115, // Weather is "{Weather}"
        PlayerFaction = 116, // Player faction is {$Player Faction}
        LfgStatusEqual = 117, // Looking-for-group status "{$LFG Status}" equals {#Value}
        LFgStatusEqualOrGreaterThan = 118, // Looking-for-group status "{$LFG Status}" is {#Value} or more
        PlayerHasCurrencyEqualOrGreaterThan = 119, // Player has currency "{CurrencyTypes}" in amount {#Amount} or more
        TargetThreatListSizeLessThan = 120, // Player Killed creature with less than "{#Targets}" threat list targets
        PlayerHasTrackedCurrencyEqualOrGreaterThan = 121, // Player has currency "{CurrencyTypes}" tracked (per season) in amount {#Amount} or more
        PlayerMapInstanceType = 122, // Player is on a map of type "{@INSTANCE_TYPE}"
        PlayerInTimeWalkerInstance = 123, // Player was in a Time Walker instance
        PvpSeasonIsActive = 124, // PVP season is active
        PvpSeason = 125, // Current PVP season is {#Season}
        GarrisonTierEqualOrGreaterThan = 126, // Garrison is tier {#Tier} or higher for garrison type "{GarrType}"
        GarrisonFollowersWithLevelEqualOrGreaterThan = 127, // At least {#Followers} followers of at least level {#Level} for follower type "{GarrFollowerType}"
        GarrisonFollowersWithQualityEqualOrGreaterThan = 128, // At least {#Followers} followers at least quality "{@GARR_FOLLOWER_QUALITY}" for follower type "{GarrFollowerType}"
        GarrisonFollowerWithAbilityAtLevelEqualOrGreaterThan = 129, // Follower of at least level {#Level} has ability {GarrAbility} for follower type "{GarrFollowerType}"
        GarrisonFollowerWithTraitAtLevelEqualOrGreaterThan = 130, // Follower of at least level {#Level} has trait {GarrAbility} for follower type "{GarrFollowerType}"
        GarrisonFollowerWithAbilityAssignedToBuilding = 131, // Follower with ability "{GarrAbility}" is assigned to building type "{@GARRISON_BUILDING_TYPE}" for garrison type "{GarrType}"
        GarrisonFollowerWithTraitAssignedToBuilding = 132, // Follower with trait "{GarrAbility}" is assigned to building type "{@GARRISON_BUILDING_TYPE}" for garrison type "{GarrType}"
        GarrisonFollowerWithLevelAssignedToBuilding = 133, // Follower at least level {#Level} is assigned to building type "{@GARRISON_BUILDING_TYPE}" for garrison type "GarrType}"
        GarrisonBuildingWithLevelEqualOrGreaterThan = 134, // Building "{@GARRISON_BUILDING_TYPE}" is at least level {#Level} for garrison type "{GarrType}"
        HasBlueprintForGarrisonBuilding = 135, // Has blueprint for garrison building "{GarrBuilding}" of type "{GarrType}"
        HasGarrisonBuildingSpecialization = 136, // Has garrison building specialization "{GarrSpecialization}"
        AllGarrisonPlotsAreFull = 137, // All garrison type "{GarrType}" plots are full
        PlayerIsInOwnGarrison = 138, // Player is in their own garrison
        GarrisonShipmentOfTypeIsPending = 139, /*NYI*/ // Shipment of type "{CharShipmentContainer}" is pending
        GarrisonBuildingIsUnderConstruction = 140, // Garrison building "{GarrBuilding}" is under construction
        GarrisonMissionHasBeenCompleted = 141, /*NYI*/ // Garrison mission "{GarrMission}" has been completed
        GarrisonBuildingLevelEqual = 142, // Building {@GARRISON_BUILDING_TYPE} is exactly level {#Level} for garrison type "{GarrType}"
        GarrisonFollowerHasAbility = 143, // This follower has ability "{GarrAbility}" for garrison type "{GarrType}"
        GarrisonFollowerHasTrait = 144, // This follower has trait "{GarrAbility}" for garrison type "{GarrType}"
        GarrisonFollowerQualityEqual = 145, // This Garrison Follower is {@GARR_FOLLOWER_QUALITY} quality
        GarrisonFollowerLevelEqual = 146, // This Garrison Follower is level {#Level}
        GarrisonMissionIsRare = 147, /*NYI*/ // This Garrison Mission is Rare
        GarrisonMissionIsElite = 148, /*NYI*/ // This Garrison Mission is Elite
        CurrentGarrisonBuildingLevelEqual = 149, // This Garrison Building is level {#Level} - building type passed as argument
        GarrisonPlotInstanceHasBuildingThatIsReadyToActivate = 150, // Garrison plot instance "{GarrPlotInstance}" has building that is ready to activate
        BattlePetTeamWithSpeciesEqualOrGreaterThan = 151, // Battlepet: with at least {#Amount} "{BattlePetSpecies}"
        BattlePetTeamWithTypeEqualOrGreaterThan = 152, // Battlepet: with at least {#Amount} pets of type "{$Battle Pet Types}"
        PetBattleLastAbility = 153, /*NYI*/ // Battlepet: last ability was "{BattlePetAbility}"
        PetBattleLastAbilityType = 154, /*NYI*/ // Battlepet: last ability was of type "{$Battle Pet Types}"
        BattlePetTeamWithAliveEqualOrGreaterThan = 155, // Battlepet: with at least {#Alive} alive
        HasGarrisonBuildingActiveSpecialization = 156, // Has Garrison building active specialization "{GarrSpecialization}"
        HasGarrisonFollower = 157, // Has Garrison follower "{GarrFollower}"
        PlayerQuestObjectiveProgressEqual = 158, // Player's progress on Quest Objective "{QuestObjective}" is equal to {#Value}
        PlayerQuestObjectiveProgressEqualOrGreaterThan = 159, // Player's progress on Quest Objective "{QuestObjective}" is at least {#Value}
        IsPTRRealm = 160, // This is a PTR Realm
        IsBetaRealm = 161, // This is a Beta Realm
        IsQARealm = 162, // This is a QA Realm
        GarrisonShipmentContainerIsFull = 163, /*NYI*/ // Shipment Container "{CharShipmentContainer}" is full
        PlayerCountIsValidToStartGarrisonInvasion = 164, // Player count is valid to start garrison invasion
        InstancePlayerCountEqualOrLessThan = 165, // Instance has at most {#Players} players
        AllGarrisonPlotsFilledWithBuildingsWithLevelEqualOrGreater = 166, // All plots are full and at least level {#Level} for garrison type "{GarrType}"
        GarrisonMissionType = 167, /*NYI*/ // This mission is type "{GarrMissionType}"
        GarrisonFollowerItemLevelEqualOrGreaterThan = 168, // This follower is at least item level {#Level}
        GarrisonFollowerCountWithItemLevelEqualOrGreaterThan = 169, // At least {#Followers} followers are at least item level {#Level} for follower type "{GarrFollowerType}"
        GarrisonTierEqual = 170, // Garrison is exactly tier {#Tier} for garrison type "{GarrType}"
        InstancePlayerCountEqual = 171, // Instance has exactly {#Players} players
        CurrencyId = 172, // The currency is type "{CurrencyTypes}"
        SelectionIsPlayerCorpse = 173, // Target is player corpse
        PlayerCanAcceptQuest = 174, // Player is currently eligible for quest "{QuestV2}"
        GarrisonFollowerCountWithLevelEqualOrGreaterThan = 175, // At least {#Followers} followers exactly level {#Level} for follower type "{GarrFollowerType}"
        GarrisonFollowerIsInBuilding = 176, // Garrison follower "{GarrFollower}" is in building "{GarrBuilding}"
        GarrisonMissionCountLessThan = 177, /*NYI*/ // Player has less than {#Available} available and {#In-Progress} in-progress missions of garrison type "{GarrType}"
        GarrisonPlotInstanceCountEqualOrGreaterThan = 178, // Player has at least {#Amount} instances of plot "{GarrPlot}" available
        CurrencySource = 179, /*NYI*/ // Currency source is {$Currency Source}
        PlayerIsInNotOwnGarrison = 180, // Player is in another garrison (not their own)
        HasActiveGarrisonFollower = 181, // Has active Garrison follower "{GarrFollower}"
        PlayerDailyRandomValueMod_X_Equals = 182, /*NYI*/ // Player daily random value mod {#Mod Value} equals {#Equals Value}
        PlayerHasMount = 183, // Player has Mount "{Mount}"
        GarrisonFollowerCountWithInactiveWithItemLevelEqualOrGreaterThan = 184, // At least {#Followers} followers (including inactive) are at least item level {#Level} for follower type "{GarrFollowerType}"
        GarrisonFollowerIsOnAMission = 185, // Garrison follower "{GarrFollower}" is on a mission
        GarrisonMissionCountInSetLessThan = 186, /*NYI*/ // Player has less than {#Missions} available and in-progress missions of set "{GarrMissionSet}" in garrison type "{GarrType}"
        GarrisonFollowerType = 187, // This Garrison Follower is of type "{GarrFollowerType}"
        PlayerUsedBoostLessThanHoursAgoRealTime = 188, /*NYI*/ // Player has boosted and boost occurred < {#Hours} hours ago (real time)
        PlayerUsedBoostLessThanHoursAgoGameTime = 189, /*NYI*/ // Player has boosted and boost occurred < {#Hours} hours ago (in-game time)
        PlayerIsMercenary = 190, // Player is currently Mercenary
        PlayerEffectiveRace = 191, /*NYI*/ // Player effective race is "{ChrRaces}"
        TargetEffectiveRace = 192, /*NYI*/ // Target effective race is "{ChrRaces}"
        HonorLevelEqualOrGreaterThan = 193, // Honor level >= {#Level}
        PrestigeLevelEqualOrGreaterThan = 194, // Prestige level >= {#Level}
        GarrisonMissionIsReadyToCollect = 195, /*NYI*/ // Garrison mission "{GarrMission}" is ready to collect
        PlayerIsInstanceOwner = 196, /*NYI*/ // Player is the instance owner (requires 'Lock Instance Owner' LFGDungeon flag)
        PlayerHasHeirloom = 197, // Player has heirloom "{Item}"
        TeamPoints = 198, /*NYI*/ // Team has {#Points} Points
        PlayerHasToy = 199, // Player has toy "{Item}"
        PlayerHasTransmog = 200, // Player has transmog "{ItemModifiedAppearance}"
        GarrisonTalentSelected = 201, /*NYI*/ // Garrison has talent "{GarrTalent}" selected
        GarrisonTalentResearched = 202, /*NYI*/ // Garrison has talent "{GarrTalent}" researched
        PlayerHasRestriction = 203, // Player has restriction of type "{@CHARACTER_RESTRICTION_TYPE}"
        PlayerCreatedCharacterLessThanHoursAgoRealTime = 204, /*NYI*/ // Player has created their character < {#Hours} hours ago (real time)
        PlayerCreatedCharacterLessThanHoursAgoGameTime = 205, // Player has created their character < {#Hours} hours ago (in-game time)
        QuestHasQuestInfoId = 206, // Quest has Quest Info "{QuestInfo}"
        GarrisonTalentResearchInProgress = 207, /*NYI*/ // Garrison is researching talent "{GarrTalent}"
        PlayerEquippedArtifactAppearanceSet = 208, // Player has equipped Artifact Appearance Set "{ArtifactAppearanceSet}"
        PlayerHasCurrencyEqual = 209, // Player has currency "{CurrencyTypes}" in amount {#Amount} exactly
        MinimumAverageItemHighWaterMarkForSpec = 210, /*NYI*/ // Minimum average item high water mark is {#Item High Water Mark} for "{$Item History Spec Match}")
        PlayerScenarioType = 211, // Player in scenario of type "{$Scenario Type}"
        PlayersAuthExpansionLevelEqualOrGreaterThan = 212, // Player's auth expansion level is "{$Expansion Level}" or higher
        PlayerLastWeek2v2Rating = 213, /*NYI*/ // Player achieved at least a rating of {#Rating} in 2v2 last week player played
        PlayerLastWeek3v3Rating = 214, /*NYI*/ // Player achieved at least a rating of {#Rating} in 3v3 last week player played
        PlayerLastWeekRBGRating = 215, /*NYI*/ // Player achieved at least a rating of {#Rating} in RBG last week player played
        GroupMemberCountFromConnectedRealmEqualOrGreaterThan = 216, // At least {#Num Players} members of the group are from your connected realms
        ArtifactTraitUnlockedCountEqualOrGreaterThan = 217, // At least {#Num Traits} traits have been unlocked in artifact "{Item}"
        ParagonReputationLevelEqualOrGreaterThan = 218, // Paragon level >= "{#Level}"
        GarrisonShipmentIsReady = 219, /*NYI*/ // Shipment in container type "{CharShipmentContainer}" ready
        PlayerIsInPvpBrawl = 220, // Player is in PvP Brawl
        ParagonReputationLevelWithFactionEqualOrGreaterThan = 221, // Paragon level >= "{#Level}" with faction "{Faction}"
        PlayerHasItemWithBonusListFromTreeAndQuality = 222, // Player has an item with bonus list from tree "{ItemBonusTree}" and of quality "{$Item Quality}"
        PlayerHasEmptyInventorySlotCountEqualOrGreaterThan = 223, // Player has at least "{#Number of empty slots}" empty inventory slots
        PlayerHasItemInHistoryOfProgressiveEvent = 224, /*NYI*/ // Player has item "{Item}" in the item history of progressive event "{ProgressiveEvent}"
        PlayerHasArtifactPowerRankCountPurchasedEqualOrGreaterThan = 225, // Player has at least {#Purchased Ranks} ranks of {ArtifactPower} on equipped artifact
        PlayerHasBoosted = 226, // Player has boosted
        PlayerHasRaceChanged = 227, // Player has race changed
        PlayerHasBeenGrantedLevelsFromRaF = 228, // Player has been granted levels from Recruit a Friend
        IsTournamentRealm = 229, // Is Tournament Realm
        PlayerCanAccessAlliedRaces = 230, // Player can access allied races
        GroupMemberCountWithAchievementEqualOrLessThan = 231, // No More Than {#Group Members} With Achievement {Achievement} In Group (true if no group)
        PlayerMainhandWeaponType = 232, // Player has main hand weapon of type "{$Weapon Type}"
        PlayerOffhandWeaponType = 233, // Player has off-hand weapon of type "{$Weapon Type}"
        PlayerPvpTier = 234, // Player is in PvP tier {PvpTier}
        PlayerAzeriteLevelEqualOrGreaterThan = 235, // Players' Azerite Item is at or above level "{#Azerite Level}"
        PlayerIsOnQuestInQuestline = 236, // Player is on quest in questline "{QuestLine}"
        PlayerIsQnQuestLinkedToScheduledWorldStateGroup = 237, // Player is on quest associated with current progressive unlock group "{ScheduledWorldStateGroup}"
        PlayerIsInRaidGroup = 238, // Player is in raid group
        PlayerPvpTierInBracketEqualOrGreaterThan = 239, // Player is at or above "{@PVP_TIER_ENUM}" for "{@PVP_BRACKET}"
        PlayerCanAcceptQuestInQuestline = 240, // Player is eligible for quest in questline "{Questline}"
        PlayerHasCompletedQuestline = 241, // Player has completed questline "{Questline}"
        PlayerHasCompletedQuestlineQuestCount = 242, // Player has completed "{#Quests}" quests in questline "{Questline}"
        PlayerHasCompletedPercentageOfQuestline = 243, // Player has completed "{#Percentage}" % of quests in questline "{Questline}"
        PlayerHasWarModeEnabled = 244, // Player has WarMode Enabled (regardless of shard state)
        PlayerIsOnWarModeShard = 245, // Player is on a WarMode Shard
        PlayerIsAllowedToToggleWarModeInArea = 246, // Player is allowed to toggle WarMode in area
        MythicPlusKeystoneLevelEqualOrGreaterThan = 247, /*NYI*/ // Mythic Plus Keystone Level Atleast {#Level}
        MythicPlusCompletedInTime = 248, /*NYI*/ // Mythic Plus Completed In Time
        MythicPlusMapChallengeMode = 249, /*NYI*/ // Mythic Plus Map Challenge Mode {MapChallengeMode}
        MythicPlusDisplaySeason = 250, /*NYI*/ // Mythic Plus Display Season {#Season}
        MythicPlusMilestoneSeason = 251, /*NYI*/ // Mythic Plus Milestone Season {#Season}
        PlayerVisibleRace = 252, // Player visible race is "{ChrRaces}"
        TargetVisibleRace = 253, // Target visible race is "{ChrRaces}"
        FriendshipRepReactionEqual = 254, // Friendship rep reaction is exactly "{FriendshipRepReaction}"
        PlayerAuraStackCountEqual = 255, // Player has exactly {#Stacks} stacks of aura "{Spell}"
        TargetAuraStackCountEqual = 256, // Target has exactly {#Stacks} stacks of aura "{Spell}"
        PlayerAuraStackCountEqualOrGreaterThan = 257, // Player has at least {#Stacks} stacks of aura "{Spell}"
        TargetAuraStackCountEqualOrGreaterThan = 258, // Target has at least {#Stacks} stacks of aura "{Spell}"
        PlayerHasAzeriteEssenceRankLessThan = 259, // Player has Azerite Essence {AzeriteEssence} at less than rank {#rank}
        PlayerHasAzeriteEssenceRankEqual = 260, // Player has Azerite Essence {AzeriteEssence} at rank {#rank}
        PlayerHasAzeriteEssenceRankGreaterThan = 261, // Player has Azerite Essence {AzeriteEssence} at greater than rank {#rank}
        PlayerHasAuraWithEffectIndex = 262, // Player has Aura {Spell} with Effect Index {#index} active
        PlayerLootSpecializationMatchesRole = 263, // Player loot specialization matches role {@LFG_ROLE}
        PlayerIsAtMaxExpansionLevel = 264, // Player is at max expansion level
        TransmogSource = 265, // Transmog Source is "{@TRANSMOG_SOURCE}"
        PlayerHasAzeriteEssenceInSlotAtRankLessThan = 266, // Player has Azerite Essence in slot {@AZERITE_ESSENCE_SLOT} at less than rank {#rank}
        PlayerHasAzeriteEssenceInSlotAtRankGreaterThan = 267, // Player has Azerite Essence in slot {@AZERITE_ESSENCE_SLOT} at greater than rank {#rank}
        PlayerLevelWithinContentTuning = 268, // Player has level within Content Tuning {ContentTuning}
        TargetLevelWithinContentTuning = 269, // Target has level within Content Tuning {ContentTuning}
        PlayerIsScenarioInitiator = 270, /*NYI*/ // Player is Scenario Initiator
        PlayerHasCompletedQuestOrIsOnQuest = 271, // Player is currently on or previously completed quest "{QuestV2}"
        PlayerLevelWithinOrAboveContentTuning = 272, // Player has level within or above Content Tuning {ContentTuning}
        TargetLevelWithinOrAboveContentTuning = 273, // Target has level within or above Content Tuning {ContentTuning}
        PlayerLevelWithinOrAboveLevelRange = 274, /*NYI*/ // Player has level within or above Level Range {LevelRange}
        TargetLevelWithinOrAboveLevelRange = 275, /*NYI*/ // Target has level within or above Level Range {LevelRange}
        MaxJailersTowerLevelEqualOrGreaterThan = 276, // Max Jailers Tower Level Atleast {#Level}
        GroupedWithRaFRecruit = 277, // Grouped With Recruit
        GroupedWithRaFRecruiter = 278, // Grouped with Recruiter
        PlayerSpecialization = 279, // Specialization is "{ChrSpecialization}"
        PlayerMapOrCosmeticChildMap = 280, // Player is on map or cosmetic child map "{Map}"
        PlayerCanAccessShadowlandsPrepurchaseContent = 281, // Player can access Shadowlands (9.0) prepurchase content
        PlayerHasEntitlement = 282, /*NYI*/ // Player has entitlement "{BattlePayDeliverable}"
        PlayerIsInPartySyncGroup = 283, /*NYI*/ // Player is in party sync group
        QuestHasPartySyncRewards = 284, /*NYI*/ // Quest is eligible for party sync rewards
        HonorGainSource = 285, /*NYI*/ // Player gained honor from source {@SPECIAL_MISC_HONOR_GAIN_SOURCE}
        JailersTowerActiveFloorIndexEqualOrGreaterThan = 286, /*NYI*/ // Active Floor Index Atleast {#Level}
        JailersTowerActiveFloorDifficultyEqualOrGreaterThan = 287, /*NYI*/ // Active Floor Difficulty Atleast {#Level}
        PlayerCovenant = 288, // Player is member of covenant "{Covenant}"
        HasTimeEventPassed = 289, // Has time event "{TimeEvent}" passed
        GarrisonHasPermanentTalent = 290, /*NYI*/ // Garrison has permanent talent "{GarrTalent}"
        HasActiveSoulbind = 291, // Has Active Soulbind "{Soulbind}"
        HasMemorizedSpell = 292, /*NYI*/ // Has memorized spell "{Spell}"
        PlayerHasAPACSubscriptionReward_2020 = 293, // Player has APAC Subscription Reward 2020
        PlayerHasTBCCDEWarpStalker_Mount = 294, // Player has TBCC:DE Warp Stalker Mount
        PlayerHasTBCCDEDarkPortal_Toy = 295, // Player has TBCC:DE Dark Portal Toy
        PlayerHasTBCCDEPathOfIllidan_Toy = 296, // Player has TBCC:DE Path of Illidan Toy
        PlayerHasImpInABallToySubscriptionReward = 297, // Player has Imp in a Ball Toy Subscription Reward
        PlayerIsInAreaGroup = 298, // Player is in area group "{AreaGroup}"
        TargetIsInAreaGroup = 299, // Target is in area group "{AreaGroup}"
        PlayerIsInChromieTime = 300, // Player has selected Chromie Time ID "{UiChromieTimeExpansionInfo}"
        PlayerIsInAnyChromieTime = 301, // Player has selected ANY Chromie Time ID
        ItemIsAzeriteArmor = 302, // Item is Azerite Armor
        PlayerHasRuneforgePower = 303, // Player Has Runeforge Power "{RuneforgeLegendaryAbility}"
        PlayerInChromieTimeForScaling = 304, // Player is Chromie Time for Scaling
        IsRaFRecruit = 305, // Is RAF recruit
        AllPlayersInGroupHaveAchievement = 306, // All Players In Group Have Achievement "{Achievement}"
        PlayerHasSoulbindConduitRankEqualOrGreaterThan = 307, /*NYI*/ // Player has Conduit "{SoulbindConduit}" at Rank {#Rank} or Higher
        PlayerSpellShapeshiftFormCreatureDisplayInfoSelection = 308, // Player has chosen {CreatureDisplayInfo} for shapeshift form {SpellShapeshiftForm}
        PlayerSoulbindConduitCountAtRankEqualOrGreaterThan = 309, /*NYI*/ // Player has at least {#Level} Conduits at Rank {#Rank} or higher.
        PlayerIsRestrictedAccount = 310, // Player is a Restricted Account
        PlayerIsFlying = 311, // Player is flying
        PlayerScenarioIsLastStep = 312, // Player is on the last step of a Scenario
        PlayerHasWeeklyRewardsAvailable = 313, // Player has weekly rewards available
        TargetCovenant = 314, // Target is member of covenant "{Covenant}"
        PlayerHasTBCCollectorsEdition = 315, // Player has TBC Collector's Edition
        PlayerHasWrathCollectorsEdition = 316, // Player has Wrath Collector's Edition
        GarrisonTalentResearchedAndAtRankEqualOrGreaterThan = 317, /*NYI*/ // Garrison has talent "{GarrTalent}" researched and active at or above {#Rank}
        CurrencySpentOnGarrisonTalentResearchEqualOrGreaterThan = 318, /*NYI*/ // Currency {CurrencyTypes} Spent on Garrison Talent Research in Tree {GarrTalentTree} is greater than or equal to {#Quantity}
        RenownCatchupActive = 319, /*NYI*/ // Renown Catchup Active
        RapidRenownCatchupActive = 320, /*NYI*/ // Rapid Renown Catchup Active
        PlayerMythicPlusRatingEqualOrGreaterThan = 321, /*NYI*/ // Player has Mythic+ Rating of at least "{#DungeonScore}"
        PlayerMythicPlusRunCountInCurrentExpansionEqualOrGreaterThan = 322, /*NYI*/ // Player has completed at least "{#MythicKeystoneRuns}" Mythic+ runs in current expansion
        PlayerHasCustomizationChoice = 323, // (Mainline) Player has Customization Choice "{ChrCustomizationChoice}"
        PlayerBestWeeklyWinPvpTier = 324, // (Mainline) Player has best weekly win in PVP tier {PvpTier}
        PlayerBestWeeklyWinPvpTierInBracketEqualOrGreaterThan = 325, // (Mainline) Player has best weekly win at or above "{@PVP_TIER_ENUM}" for "{@PVP_BRACKET}"
        PlayerHasVanillaCollectorsEdition = 326, // Player has Vanilla Collector's Edition
        PlayerHasItemWithKeystoneLevelModifierEqualOrGreaterThan = 327,
        PlayerMythicPlusRatingInDisplaySeasonEqualOrGreaterThan = 329, /*NYI*/ // Player has Mythic+ Rating of at least "{#DungeonScore}" in {DisplaySeason}
        PlayerMythicPlusLadderRatingInDisplaySeasonEqualOrGreaterThan = 333, /*NYI*/ // Player has Mythic+ Ladder Rating of at least "{#DungeonScore}" in {DisplaySeason}
        MythicPlusRatingIsInTop01Percent = 334, /*NYI*/ // top 0.1% rating
        PlayerAuraWithLabelStackCountEqualOrGreaterThan = 335, // Player has at least {#Stacks} stacks of aura "{Label}"
        PlayerAuraWithLabelStackCountEqual = 336, // Target has exactly {#Stacks} stacks of aura with label "{Label}"
        PlayerAuraWithLabelStackCountEqualOrLessThan = 337, // Player has at most {#Stacks} stacks of aura "{Label}"
        PlayerIsInCrossFactionGroup = 338, // Player is in a cross faction group
        PlayerHasTraitNodeEntryInActiveConfig = 340, // Player has {TraitNodeEntry} node in currently active config
        PlayerHasTraitNodeEntryInActiveConfigRankGreaterOrEqualThan = 341, // Player has at least {#Rank} for {TraitNodeEntry} node in currently active config
        PlayerHasPurchasedCombatTraitRanks = 342, /*NYI*/ // Player has purchased at least {#Count} talent points in active combat config
        PlayerHasPurchasedTraitRanksInTraitTree = 343, /*NYI*/ // Player has purchased at least {#Count} ranks in {#TraitTree}
        PlayerDaysSinceLogout = 344,
        CraftingOrderSkillLineAbility = 347, /*NYI*/
        CraftingOrderProfession = 348, /*NYI*/ // ProfessionEnum

        PlayerHasPerksProgramPendingReward = 350,
        PlayerCanUseItem = 351, // Player can use item {#Item}
        PlayerSummonedBattlePetSpecies = 352,
        PlayerSummonedBattlePetIsMaxLevel = 353,

        PlayerHasAtLeastProfPathRanks = 355, // Player has purchased or granted at least {#Count} ranks in {SkillLine} config
        PlayerHasAtLeastMissingProfPathRanks = 356, /*NYI*/ // Player is missing least {#Count} ranks in {SkillLine} config

        PlayerHasItemTransmogrifiedToItemModifiedAppearance = 358, // Player has item with {ItemModifiedAppearance} transmog
        ItemHasBonusList = 359, /*NYI*/ // Item has {ItemBonusList} (used by ItemCondition)
        ItemHasBonusListFromGroup = 360, /*NYI*/ // Item has a bonus list from {ItemBonusListGroup} (used by ItemCondition)
        ItemHasContext = 361, /*NYI*/ // Item has {ItemContext}
        ItemHasItemLevelBetween = 362, /*NYI*/ // Item has item level between {#Min} and {#Max}
        ItemHasContentTuningID = 363, /*NYI*/ // Item has {ContentTuning} (modifier 28)
        ItemHasInventoryType = 364, /*NYI*/ // Item has inventory type
        ItemWasCraftedWithReagentInSlot = 365, /*NYI*/ // Item was crafted with reagent item {Item} in slot {ModifiedCraftingReagentSlot}
        PlayerHasCompletedDungeonEncounterInDifficulty = 366, // Player has completed {DungeonEncounter} on {Difficulty}
        PlayerCurrencyIsRelOpFromMax = 367, /*NYI*/ // Player {CurrencyTypes} is {RelOp} {#Amount} from currency limit
        ItemHasModifiedCraftingReagentSlot = 368, /*NYI*/ // Item has {ModifiedCraftingReagentSlot}
        PlayerIsBetweenQuests = 369, // Player has previously completed quest or is on "{QuestV2}" but not "{QuestV2}" (SecondaryAsset)
        PlayerIsOnQuestWithLabel = 370, /*NYI*/ // Player is on quest with {QuestLabel}
        PlayerScenarioStepID = 371, // Player is on scenario step number {ScenarioStep}
        PlayerHasCompletedQuestWithLabel = 372, /*NYI*/ // Player has previously completed quest with {QuestLabel}
        LegacyLootIsEnabled = 373, /*NYI*/
        PlayerZPositionBelow = 374,
        PlayerWeaponHighWatermarkAboveOrEqual = 375, /*NYI*/
        PlayerHeadHighWatermarkAboveOrEqual = 376, /*NYI*/
        PlayerHasDisplayedCurrencyLessThan = 377, /*NYI*/ // Player has {CurrencyTypes} less than {#Amount} (value visible in ui is taken into account, not raw value)
        PlayerDataFlagAccountIsSet = 378, /*NYI*/ // Player {PlayerDataFlagAccount} is set
        PlayerDataFlagCharacterIsSet = 379, /*NYI*/ // Player {PlayerDataFlagCharacter} is set
        PlayerIsOnMapWithExpansion = 380, // Player is on map that has {ExpansionID}

        PlayerHasCompletedQuestOnAccount = 382, /*NYI*/ // Player has previously completed quest "{QuestV2}" on account
        PlayerHasCompletedQuestlineOnAccount = 383, /*NYI*/ // Player has completed questline "{Questline}" on account
        PlayerHasCompletedQuestlineQuestCountOnAccount = 384, /*NYI*/ // Player has completed "{#Quests}" quests in questline "{Questline}" on account
        PlayerHasActiveTraitSubTree = 385, // Player has active trait config with {TraitSubTree}

        PlayerIsInSoloRBG = 387, /*NYI*/ // Player is in solo RBG (BG Blitz)
        PlayerHasCompletedCampaign = 388, /*NYI*/ // Player has completed campaign "{Campaign}"
        TargetCreatureClassificationEqual = 389, // Creature classification is {CreatureClassification}
        PlayerDataElementCharacterEqual = 390, /*NYI*/ // Player {PlayerDataElementCharacter} is greater than {#Amount}
        PlayerDataElementAccountEqual = 391, /*NYI*/ // Player {PlayerDataElementAccount} is greater than {#Amount}
        PlayerHasCompletedQuestOrIsReadyToTurnIn = 392, // Player has previously completed quest "{QuestV2}" or is ready to turn it in
        PlayerTitle = 393, // Player is currently using "{ChrTitles}" title

        PlayerIsInGuild = 404, // Player is in a guild
    }

    public enum CriteriaFailEvent : byte
    {
        None = 0,
        Death = 1,    // Death
        Hours24WithoutCompletingDailyQuest = 2,    // 24 hours without completing a daily quest
        LeaveBattleground = 3,    // Leave a battleground
        LoseRankedArenaMatchWithTeamSize = 4,    // Lose a ranked arena match with team size {#Team Size}
        LoseAura = 5,    // Lose aura "{Spell}"
        GainAura = 6,    // Gain aura "{Spell}"
        GainAuraEffect = 7,    // Gain aura effect "{SpellAuraNames.EnumID}"
        CastSpell = 8,    // Cast spell "{Spell}"
        BeSpellTarget = 9,    // Have spell "{Spell}" cast on you
        ModifyPartyStatus = 10,   // Modify your party status
        LosePetBattle = 11,   // Lose a pet battle
        BattlePetDies = 12,   // Battle pet dies
        DailyQuestsCleared = 13,   // Daily quests cleared
        SendEvent = 14,   // Send event "{GameEvents}" (player-sent/instance only)

        Count
    }

    public enum CriteriaStartEvent : byte
    {
        None = 0, // - NONE -
        ReachLevel = 1, // Reach level {#Level}
        CompleteDailyQuest = 2, // Complete daily quest "{QuestV2}"
        StartBattleground = 3, // Start battleground "{Map}"
        WinRankedArenaMatchWithTeamSize = 4, // Win a ranked arena match with team size {#Team Size}
        GainAura = 5, // Gain aura "{Spell}"
        GainAuraEffect = 6, // Gain aura effect "{SpellAuraNames.EnumID}"
        CastSpell = 7, // Cast spell "{Spell}"
        BeSpellTarget = 8, // Have spell "{Spell}" cast on you
        AcceptQuest = 9, // Accept quest "{QuestV2}"
        KillNPC = 10, // Kill NPC "{Creature}"
        KillPlayer = 11, // Kill player
        UseItem = 12, // Use item "{Item}"
        SendEvent = 13, // Send event "{GameEvents}" (player-sent/instance only)
        BeginScenarioStep = 14, // Begin scenario step "{#Step}" (for use with "Player on Scenario" modifier only)

        Count
    }

    public enum CriteriaFlags
    {
        FailAchievement = 0x01, // Fail Achievement
        ResetOnStart = 0x02, // Reset on Start
        ServerOnly = 0x04, // Server Only
        AlwaysSaveToDB = 0x08, // Always Save to DB (Use with Caution)
        AllowCriteriaDecrement = 0x10, // Allow criteria to be decremented
        IsForQuest = 0x20  // Is For Quest
    }

    public enum CriteriaType : short
    {
        KillCreature = 0,   // Kill NPC "{Creature}"
        WinBattleground = 1,   // Win battleground "{Map}"
        CompleteResearchProject = 2,   /*NYI*/ // Complete research project "{ResearchProject}"
        CompleteAnyResearchProject = 3,   /*NYI*/ // Complete any research project
        FindResearchObject = 4,   /*NYI*/ // Find research object "{GameObjects}"
        ReachLevel = 5,   // Reach level
        ExhaustAnyResearchSite = 6,   /*NYI*/ // Exhaust any research site
        SkillRaised = 7,   // Skill "{SkillLine}" raised
        EarnAchievement = 8,   // Earn achievement "{Achievement}"
        CompleteQuestsCount = 9,   // Count of complete quests (quest count)
        CompleteAnyDailyQuestPerDay = 10,  // Complete any daily quest (per day)
        CompleteQuestsInZone = 11,  // Complete quests in "{AreaTable}"
        CurrencyGained = 12,  // Currency "{CurrencyTypes}" gained
        DamageDealt = 13,  // Damage dealt
        CompleteDailyQuest = 14,  // Complete daily quest
        ParticipateInBattleground = 15,  // Participate in battleground "{Map}"
        DieOnMap = 16,  // Die on map "{Map}"
        DieAnywhere = 17,  // Die anywhere
        DieInInstance = 18,  // Die in an instance which handles at most {#Max Players} players
        RunInstance = 19,  /*NYI*/ // Run an instance which handles at most {#Max Players} players
        KilledByCreature = 20,  // Get killed by "{Creature}"
        CompleteInternalCriteria = 21,  /*NYI*/ // Designer Value{`Uses Record ID}
        CompleteAnyChallengeMode = 22,  /*NYI*/ // Complete any challenge mode
        KilledByPlayer = 23,  // Die to a player
        MaxDistFallenWithoutDying = 24,  // Maximum distance fallen without dying
        EarnChallengeModeMedal = 25,  /*NYI*/ // Earn a challenge mode medal of "{#Challenge Mode Medal (OBSOLETE)}" (OBSOLETE)
        DieFromEnviromentalDamage = 26,  // Die to "{$Env Damage}" environmental damage
        CompleteQuest = 27,  // Complete quest "{QuestV2}"
        BeSpellTarget = 28,  // Have the spell "{Spell}" cast on you
        CastSpell = 29,  // Cast the spell "{Spell}"
        TrackedWorldStateUIModified = 30,  // Tracked WorldStateUI value "{WorldStateUI}" is modified
        PVPKillInArea = 31,  // Kill someone in PVP in "{AreaTable}"
        WinArena = 32,  // Win arena "{Map}"
        ParticipateInArena = 33,  // Participate in arena "{Map}"
        LearnOrKnowSpell = 34,  // Learn or Know spell "{Spell}"
        EarnHonorableKill = 35,  // Earn an honorable kill
        AcquireItem = 36,  // Acquire item "{Item}"
        WinAnyRankedArena = 37,  // Win a ranked arena match (any arena)
        EarnTeamArenaRating = 38,  /*NYI*/ // Earn a team arena rating of {#Arena Rating}
        EarnPersonalArenaRating = 39,  // Earn a personal arena rating of {#Arena Rating}
        AchieveSkillStep = 40,  // Achieve a skill step in "{SkillLine}"
        UseItem = 41,  // Use item "{Item}"
        LootItem = 42,  // Loot "{Item}" via corpse, pickpocket, fishing, disenchanting, etc.
        RevealWorldMapOverlay = 43,  // Reveal world map overlay "{WorldMapOverlay}"
        EarnTitle = 44,  /*NYI*/ // Deprecated PVP Titles
        BankSlotsPurchased = 45,  // Bank slots purchased
        ReputationGained = 46,  // Reputation gained with faction "{Faction}"
        TotalExaltedFactions = 47,  // Total exalted factions
        GotHaircut = 48,  // Got a haircut
        EquipItemInSlot = 49,  // Equip item in slot "{$Equip Slot}"
        RollNeed = 50,  // Roll need and get {#Need Roll}
        RollGreed = 51,  // Roll greed and get {#Greed Roll}
        DeliverKillingBlowToClass = 52,  // Deliver a killing blow to a {ChrClasses}
        DeliverKillingBlowToRace = 53,  // Deliver a killing blow to a {ChrRaces}
        DoEmote = 54,  // Do a "{EmotesText}" emote
        HealingDone = 55,  // Healing done
        DeliveredKillingBlow = 56,  // Delivered a killing blow
        EquipItem = 57,  // Equip item "{Item}"
        CompleteQuestsInSort = 58,  /*NYI*/ // Complete quests in "{QuestSort}"
        MoneyEarnedFromSales = 59,  // Sell items to vendors
        MoneySpentOnRespecs = 60,  // Money spent on respecs
        TotalRespecs = 61,  // Total respecs
        MoneyEarnedFromQuesting = 62,  // Money earned from questing
        MoneySpentOnTaxis = 63,  // Money spent on taxis
        KilledAllUnitsInSpawnRegion = 64,  /*NYI*/ // Killed all units in spawn region "{SpawnRegion}"
        MoneySpentAtBarberShop = 65,  // Money spent at the barber shop
        MoneySpentOnPostage = 66,  // Money spent on postage
        MoneyLootedFromCreatures = 67,  // Money looted from creatures
        UseGameobject = 68,  // Use Game Object "{GameObjects}"
        GainAura = 69,  // Gain aura "{Spell}"
        KillPlayer = 70,  // Kill a player (no honor check)
        CompleteChallengeMode = 71,  /*NYI*/ // Complete a challenge mode on map "{Map}"
        CatchFishInFishingHole = 72,  // Catch fish in the "{GameObjects}" fishing hole
        PlayerTriggerGameEvent = 73,  // Player will Trigger game event "{GameEvents}"
        Login = 74,  // Login (USE SPARINGLY!)
        LearnSpellFromSkillLine = 75,  // Learn spell from the "{SkillLine}" skill line
        WinDuel = 76,  // Win a duel
        LoseDuel = 77,  // Lose a duel
        KillAnyCreature = 78,  // Kill any NPC
        CreatedItemsByCastingSpellWithLimit = 79,  /*NYI*/ // Created items by casting a spell (limit 1 per create...)
        MoneyEarnedFromAuctions = 80,  // Money earned from auctions
        BattlePetAchievementPointsEarned = 81,  /*NYI*/ // Battle pet achievement points earned
        ItemsPostedAtAuction = 82,  // Number of items posted at auction
        HighestAuctionBid = 83,  // Highest auction bid
        AuctionsWon = 84,  // Auctions won
        HighestAuctionSale = 85,  // Highest coin value of item sold
        MostMoneyOwned = 86,  // Most money owned
        TotalReveredFactions = 87,  // Total revered factions
        TotalHonoredFactions = 88,  // Total honored factions
        TotalFactionsEncountered = 89,  // Total factions encountered
        LootAnyItem = 90,  // Loot any item
        ObtainAnyItem = 91,  // Obtain any item
        AnyoneTriggerGameEventScenario = 92,  // Anyone will Trigger game event "{GameEvents}" (Scenario Only)
        RollAnyNeed = 93,  // Roll any number on need
        RollAnyGreed = 94,  // Roll any number on greed
        ReleasedSpirit = 95,  /*NYI*/ // Released Spirit
        AccountKnownPet = 96,  /*NYI*/ // Account knows pet "{Creature}" (Backtracked)
        DefeatDungeonEncounterWhileElegibleForLoot = 97, // Defeat Encounter "{DungeonEncounter}" While Eligible For Loot
                                                                  // UNUSED 18{}                                 = 98,  // Unused
                                                                  // UNUSED 19{}                                 = 99,  // Unused
                                                                  // UNUSED 20{}                                 = 100, // Unused
        HighestDamageDone = 101, // Highest damage done in 1 single ability
        HighestDamageTaken = 102, // Most damage taken in 1 single hit
        TotalDamageTaken = 103, // Total damage taken
        HighestHealCast = 104, // Largest heal cast
        TotalHealReceived = 105, // Total healing received
        HighestHealReceived = 106, // Largest heal received
        AbandonAnyQuest = 107, // Abandon any quest
        BuyTaxi = 108, // Buy a taxi
        GetLootByType = 109, // Get loot via "{$Loot Acquisition}"
        LandTargetedSpellOnTarget = 110, // Land targeted spell "{Spell}" on a target
                                         // UNUSED 21{}                                 = 111, // Unused
        LearnTradeskillSkillLine = 112, // Learn tradeskill skill line "{SkillLine}"
        HonorableKills = 113, // Honorable kills (number in interface, won't update except for login)
        AcceptSummon = 114, // Accept a summon
        EarnAchievementPoints = 115, // Earn achievement points
        RollDisenchant = 116, /*NYI*/ // Roll disenchant and get {#Disenchant Roll}
        RollAnyDisenchant = 117, /*NYI*/ // Roll any number on disenchant
        CompletedLFGDungeon = 118, // Completed an LFG dungeon
        CompletedLFGDungeonWithStrangers = 119, // Completed an LFG dungeon with strangers
        KickInitiatorInLFGDungeon = 120, /*NYI*/ // Kicked in an LFG dungeon (initiator)
        KickVoterInLFGDungeon = 121, /*NYI*/ // Kicked in an LFG dungeon (voter)
        KickTargetInLFGDungeon = 122, /*NYI*/ // Kicked in an LFG dungeon (target)
        AbandonedLFGDungeon = 123, /*NYI*/ // Abandoned an LFG dungeon
        MoneySpentOnGuildRepair = 124, /*NYI*/ // Guild repair amount spent
        GuildAttainedLevel = 125, /*NYI*/ // Guild attained level
        CreatedItemsByCastingSpell = 126, /*NYI*/ // Created items by casting a spell
        FishInAnyPool = 127, /*NYI*/ // Fish in any pool
        GuildBankTabsPurchased = 128, /*NYI*/ // Guild bank tabs purchased
        EarnGuildAchievementPoints = 129, /*NYI*/ // Earn guild achievement points
        WinAnyBattleground = 130, /*NYI*/ // Win any battleground
        ParticipateInAnyBattleground = 131, /*NYI*/ // Participate in any battleground
        EarnBattlegroundRating = 132, /*NYI*/ // Earn a battleground rating
        GuildTabardCreated = 133, /*NYI*/ // Guild tabard created
        CompleteQuestsCountForGuild = 134, /*NYI*/ // Count of complete quests for guild (Quest count)
        HonorableKillsForGuild = 135, /*NYI*/ // Honorable kills for Guild
        KillAnyCreatureForGuild = 136, /*NYI*/ // Kill any NPC for Guild
        GroupedTankLeftEarlyInLFGDungeon = 137, /*NYI*/ // Grouped tank left early in an LFG dungeon
        CompleteGuildChallenge = 138, /*NYI*/ // Complete a "{$Guild Challenge}" guild challenge
        CompleteAnyGuildChallenge = 139, /*NYI*/ // Complete any guild challenge
        MarkedAFKInBattleground = 140, /*NYI*/ // Marked AFK in a battleground
        RemovedAFKInBattleground = 141, /*NYI*/ // Removed for being AFK in a battleground
        StartAnyBattleground = 142, /*NYI*/ // Start any battleground (AFK tracking)
        CompleteAnyBattleground = 143, /*NYI*/ // Complete any battleground (AFK tracking)
        MarkedSomeoneAFKInBattleground = 144, /*NYI*/ // Marked someone for being AFK in a battleground
        CompletedLFRDungeon = 145, /*NYI*/ // Completed an LFR dungeon
        AbandonedLFRDungeon = 146, /*NYI*/ // Abandoned an LFR dungeon
        KickInitiatorInLFRDungeon = 147, /*NYI*/ // Kicked in an LFR dungeon (initiator)
        KickVoterInLFRDungeon = 148, /*NYI*/ // Kicked in an LFR dungeon (voter)
        KickTargetInLFRDungeon = 149, /*NYI*/ // Kicked in an LFR dungeon (target)
        GroupedTankLeftEarlyInLFRDungeon = 150, /*NYI*/ // Grouped tank left early in an LFR dungeon
        CompleteAnyScenario = 151, // Complete a Scenario
        CompleteScenario = 152, // Complete scenario "{Scenario}"
        EnterAreaTriggerWithActionSet = 153, // Enter area trigger "{AreaTriggerActionSet}"
        LeaveAreaTriggerWithActionSet = 154, // Leave area trigger "{AreaTriggerActionSet}"
        LearnedNewPet = 155, // (Account Only) Learned a new pet
        UniquePetsOwned = 156, // (Account Only) Unique pets owned
        AccountObtainPetThroughBattle = 157, /*NYI*/ // (Account Only) Obtain a pet through battle
        WinPetBattle = 158, /*NYI*/ // Win a pet battle
        LosePetBattle = 159, /*NYI*/ // Lose a pet battle
        BattlePetReachLevel = 160, // (Account Only) Battle pet has reached level {#Level}
        PlayerObtainPetThroughBattle = 161, /*NYI*/ // (Player) Obtain a pet through battle
        ActivelyEarnPetLevel = 162, // (Player) Actively earn level {#Level} with a pet by a player
        EnterArea = 163, // Enter Map Area "{AreaTable}"
        LeaveArea = 164, // Leave Map Area "{AreaTable}"
        DefeatDungeonEncounter = 165, // Defeat Encounter "{DungeonEncounter}"
        PlaceAnyGarrisonBuilding = 166, /*NYI*/ // Garrison Building: Place any
        PlaceGarrisonBuilding = 167, // Garrison Building: Place "{GarrBuilding}"
        ActivateAnyGarrisonBuilding = 168, // Garrison Building: Activate any
        ActivateGarrisonBuilding = 169, /*NYI*/ // Garrison Building: Activate "{GarrBuilding}"
        UpgradeGarrison = 170, /*NYI*/ // Garrison: Upgrade Garrison to Tier "{#Tier:2,3}"
        StartAnyGarrisonMissionWithFollowerType = 171, /*NYI*/ // Garrison Mission: Start any with FollowerType "{GarrFollowerType}"
        StartGarrisonMission = 172, /*NYI*/ // Garrison Mission: Start "{GarrMission}"
        SucceedAnyGarrisonMissionWithFollowerType = 173, /*NYI*/ // Garrison Mission: Succeed any with FollowerType "{GarrFollowerType}"
        SucceedGarrisonMission = 174, /*NYI*/ // Garrison Mission: Succeed "{GarrMission}"
        RecruitAnyGarrisonFollower = 175, /*NYI*/ // Garrison Follower: Recruit any
        RecruitGarrisonFollower = 176, // Garrison Follower: Recruit "{GarrFollower}"
        AcquireGarrison = 177, /*NYI*/ // Garrison: Acquire a Garrison
        LearnAnyGarrisonBlueprint = 178, /*NYI*/ // Garrison Blueprint: Learn any
        LearnGarrisonBlueprint = 179, /*NYI*/ // Garrison Blueprint: Learn "{GarrBuilding}"
        LearnAnyGarrisonSpecialization = 180, /*NYI*/ // Garrison Specialization: Learn any
        LearnGarrisonSpecialization = 181, /*NYI*/ // Garrison Specialization: Learn "{GarrSpecialization}"
        CollectGarrisonShipment = 182, /*NYI*/ // Garrison Shipment of type "{CharShipmentContainer}" collected
        ItemLevelChangedForGarrisonFollower = 183, /*NYI*/ // Garrison Follower: Item Level Changed
        LevelChangedForGarrisonFollower = 184, /*NYI*/ // Garrison Follower: Level Changed
        LearnToy = 185, /*NYI*/ // Learn Toy "{Item}"
        LearnAnyToy = 186, /*NYI*/ // Learn Any Toy
        QualityUpgradedForGarrisonFollower = 187, /*NYI*/ // Garrison Follower: Quality Upgraded
        LearnHeirloom = 188, // Learn Heirloom "{Item}"
        LearnAnyHeirloom = 189, // Learn Any Heirloom
        EarnArtifactXP = 190, // Earn Artifact XP
        AnyArtifactPowerRankPurchased = 191, // Artifact Power Ranks Purchased
        LearnTransmog = 192, /*NYI*/ // Learn Transmog "{ItemModifiedAppearance}"
        LearnAnyTransmog = 193, // Learn Any Transmog
        HonorLevelIncrease = 194, // (Player) honor level increase
        PrestigeLevelIncrease = 195, /*NYI*/ // (Player) prestige level increase
        ActivelyReachLevel = 196, // Actively level to level {#Level}
        CompleteResearchAnyGarrisonTalent = 197, /*NYI*/ // Garrison Talent: Complete Research Any
        CompleteResearchGarrisonTalent = 198, /*NYI*/ // Garrison Talent: Complete Research "{GarrTalent}"
        LearnAnyTransmogInSlot = 199, // Learn Any Transmog in Slot "{$Equip Slot}"
        RecruitAnyGarrisonTroop = 200, /*NYI*/ // Recruit any Garrison Troop
        StartResearchAnyGarrisonTalent = 201, /*NYI*/ // Garrison Talent: Start Research Any
        StartResearchGarrisonTalent = 202, /*NYI*/ // Garrison Talent: Start Research "{GarrTalent}"
        CompleteAnyWorldQuest = 203, /*NYI*/ // Complete Any Quest
        EarnLicense = 204, /*NYI*/ // Earn License "{BattlePayDeliverable}" (does NOT work for box level)
        CollectTransmogSetFromGroup = 205, // (Account Only) Collect a Transmog Set from Group "{TransmogSetGroup}"
        ParagonLevelIncreaseWithFaction = 206, /*NYI*/ // (Player) paragon level increase with faction "{Faction}"
        PlayerHasEarnedHonor = 207, /*NYI*/ // Player has earned honor
        KillCreatureScenario = 208, /*NYI*/ // Kill NPC "{Creature}" (scenario criteria only, do not use for player)
        ArtifactPowerRankPurchased = 209, /*NYI*/ // Artifact Power Rank of "{ArtifactPower}" Purchased
        ChooseAnyRelicTalent = 210, /*NYI*/ // Choose any Relic Talent
        ChooseRelicTalent = 211, /*NYI*/ // Choose Relic Talent "{ArtifactPower}"
        EarnExpansionLevel = 212, /*NYI*/ // Earn Expansion Level "{$Expansion Level}"
        AccountHonorLevelReached = 213, /*NYI*/ // (Account Only) honor level {#Level} reached
        EarnArtifactXPForAzeriteItem = 214, // Earn Artifact experience for Azerite Item
        AzeriteLevelReached = 215, // Azerite Level {#Azerite Level} reached
        MythicPlusCompleted = 216, /*NYI*/ // Mythic Plus Completed
        ScenarioGroupCompleted = 217, /*NYI*/ // Scenario Group Completed
        CompleteAnyReplayQuest = 218, // Complete Any Replay Quest
        BuyItemsFromVendors = 219, // Buy items from vendors
        SellItemsToVendors = 220, // Sell items to vendors
        ReachMaxLevel = 221, // Reach Max Level
        MemorizeSpell = 222, /*NYI*/ // Memorize Spell "{Spell}"
        LearnTransmogIllusion = 223, /*NYI*/ // Learn Transmog Illusion
        LearnAnyTransmogIllusion = 224, /*NYI*/ // Learn Any Transmog Illusion
        EnterTopLevelArea = 225, // Enter Top Level Map Area "{AreaTable}"
        LeaveTopLevelArea = 226, // Leave Top Level Map Area "{AreaTable}"
        SocketGarrisonTalent = 227, /*NYI*/ // Socket Garrison Talent {GarrTalent}
        SocketAnySoulbindConduit = 228, /*NYI*/ // Socket Any Soulbind Conduit
        ObtainAnyItemWithCurrencyValue = 229, /*NYI*/ // Obtain Any Item With Currency Value "{CurrencyTypes}"
        MythicPlusRatingAttained = 230, /*NYI*/ // (Player) Mythic+ Rating "{#DungeonScore}" attained
        SpentTalentPoint = 231, /*NYI*/ // (Player) spent talent point
        MythicPlusDisplaySeasonEnded = 234, /*NYI*/ // {DisplaySeason}
        WinRatedSoloShuffleRound = 239, /*NYI*/
        ParticipateInRatedSoloShuffleRound = 240, /*NYI*/
        ReputationAmountGained = 243, /*NYI*/ // Gain reputation amount with {FactionID}; accumulate, not highest
        FulfillAnyCraftingOrder = 245, /*NYI*/
        FulfillCraftingOrderType = 246, /*NYI*/ // {CraftingOrderType}

        PerksProgramMonthComplete = 249, /*NYI*/
        CompleteTrackingQuest = 250, /*NYI*/

        GainLevels = 253, // Gain levels

        CompleteQuestsCountOnAccount = 257, /*NYI*/

        WarbandBankTabPurchased = 260, /*NYI*/
        ReachRenownLevel = 261,
        LearnTaxiNode = 262,
        Count = 264
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

        Max
    }

    public enum ProgressType
    {
        Set,
        Accumulate,
        Highest
    }
}
