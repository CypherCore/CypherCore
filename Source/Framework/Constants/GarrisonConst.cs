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

namespace Framework.Constants
{
    public struct GarrisonFactionIndex
    {
        public const uint Horde = 0;
        public const uint Alliance = 1;
    }

    public enum GarrisonBuildingFlags : byte
    {
        NeedsPlan = 0x1
    }

    enum GarrisonFollowerFlags
    {
        Unique = 0x1
    }

    public enum GarrisonFollowerType
    {
        Garrison = 1,
        Shipyard = 2
    }

    public enum GarrisonAbilityFlags : ushort
    {
        Trait = 0x01,
        CannotRoll = 0x02,
        HordeOnly = 0x04,
        AllianceOnly = 0x08,
        CannotRemove = 0x10,
        Exclusive = 0x20,
        SingleMissionDuration = 0x40,
        ActiveOnlyOnZoneSupport = 0x80,
        ApplyToFirstMission = 0x100,
        IsSpecialization = 0x200,
        IsEmptySlot = 0x400
    }

    public enum GarrisonError
    {
        Success = 0,
        NoGarrison = 1,
        GarrisonExists = 2,
        GarrisonSameTypeExists = 3,
        InvalidGarrison = 4,
        InvalidGarrisonLevel = 5,
        GarrisonLevelUnchanged = 6,
        NotInGarrison = 7,
        NoBuilding = 8,
        BuildingExists = 9,
        InvalidPlotInstanceId = 10,
        InvalidBuildingId = 11,
        InvalidUpgradeLevel = 12,
        UpgradeLevelExceedsGarrisonLevel = 13,
        PlotsNotFull = 14,
        InvalidSiteId = 15,
        InvalidPlotBuilding = 16,
        InvalidFaction = 17,
        InvalidSpecialization = 18,
        SpecializationExists = 19,
        SpecializationOnCooldown = 20,
        BlueprintExists = 21,
        RequiresBlueprint = 22,
        InvalidDoodadSetId = 23,
        BuildingTypeExists = 24,
        BuildingNotActive = 25,
        ConstructionComplete = 26,
        FollowerExists = 27,
        InvalidFollower = 28,
        FollowerAlreadyOnMission = 29,
        FollowerInBuilding = 30,
        FollowerInvalidForBuilding = 31,
        InvalidFollowerLevel = 32,
        MissionExists = 33,
        InvalidMission = 34,
        InvalidMissionTime = 35,
        InvalidMissionRewardIndex = 36,
        MissionNotOffered = 37,
        AlreadyOnMission = 38,
        MissionSizeInvalid = 39,
        FollowerSoftCapExceeded = 40,
        NotOnMission = 41,
        AlreadyCompletedMission = 42,
        MissionNotComplete = 43,
        MissionRewardsPending = 44,
        MissionExpired = 45,
        NotEnoughCurrency = 46,
        NotEnoughGold = 47,
        BuildingMissing = 48,
        NoArchitect = 49,
        ArchitectNotAvailable = 50,
        NoMissionNpc = 51,
        MissionNpcNotAvailable = 52,
        InternalError = 53,
        InvalidStaticTableValue = 54,
        InvalidItemLevel = 55,
        InvalidAvailableRecruit = 56,
        FollowerAlreadyRecruited = 57,
        RecruitmentGenerationInProgress = 58,
        RecruitmentOnCooldown = 59,
        RecruitBlockedByGeneration = 60,
        RecruitmentNpcNotAvailable = 61,
        InvalidFollowerQuality = 62,
        ProxyNotOk = 63,
        RecallPortalUsedLessThan24HoursAgo = 64,
        OnRemoveBuildingSpellFailed = 65,
        OperationNotSupported = 66,
        FollowerFatigued = 67,
        UpgradeConditionFailed = 68,
        FollowerInactive = 69,
        FollowerActive = 70,
        FollowerActivationUnavailable = 71,
        FollowerTypeMismatch = 72,
        InvalidGarrisonType = 73,
        MissionStartConditionFailed = 74,
        InvalidFollowerAbility = 75,
        InvalidMissionBonusAbility = 76,
        HigherBuildingTypeExists = 77,
        AtFollowerHardCap = 78,
        FollowerCannotGainXp = 79,
        NoOp = 80,
        AtClassSpecCap = 81,
        MissionRequires100ToStart = 82,
        MissionMissingRequiredFollower = 83,
        InvalidTalent = 84,
        AlreadyResearchingTalent = 85,
        FailedCondition = 86,
        InvalidTier = 87,
        InvalidClass = 88
    }

    public enum GarrisonFollowerStatus
    {
        Favorite = 0x01,
        Exhausted = 0x02,
        Inactive = 0x04,
        Troop = 0x08,
        NoXpGain = 0x10
    }

    public enum GarrisonType
    {
        Garrison = 2,
        ClassOrder = 3
    }
}
