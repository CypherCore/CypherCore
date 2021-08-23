/*
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

        Max
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

        Max
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
        EquipItemInSlot = 49,
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
        LootAnyItem = 90,
        ObtainAnyItem = 91,
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
        OwnItemModifiedAppearance = 192,
        HonorLevelReached = 194,
        PrestigeReached = 195,
        ActivelyReachLevel = 196,
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
        ExpansionLevel = 212,
        ReachAccountHonorLevel = 213,
        HeartOfAzerothArtifactPowerEarned = 214,
        HeartOfAzerothLevelReached = 215,
        MythicKeystoneCompleted = 216, // NYI
        // 217 - 0 Criterias
        CompleteQuestAccumulate = 218,
        BoughtItemFromVendor = 219,
        SoldItemToVendor = 220,
        // 221 - 0 Criterias
        // 222 - 0 Criterias
        // 223 - 0 Criterias
        // 224 - 0 Criterias
        TravelledToArea = 225,
        // 226 - 0 Criterias
        // 227 - 0 Criterias
        ApplyConduit = 228,
        ConvertItemsToCurrency = 229,
        TotalTypes = 232
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
