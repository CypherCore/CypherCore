// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum ResponseCodes
    {
        Success = 0,
        Failure = 1,
        Cancelled = 2,
        Disconnected = 3,
        FailedToConnect = 4,
        Connected = 5,
        VersionMismatch = 6,

        CstatusConnecting = 7,
        CstatusNegotiatingSecurity = 8,
        CstatusNegotiationComplete = 9,
        CstatusNegotiationFailed = 10,
        CstatusAuthenticating = 11,

        RealmListInProgress = 12,
        RealmListSuccess = 13,
        RealmListFailed = 14,
        RealmListInvalid = 15,
        RealmListRealmNotFound = 16,

        AccountCreateInProgress = 17,
        AccountCreateSuccess = 18,
        AccountCreateFailed = 19,

        CharListRetrieving = 20,
        CharListRetrieved = 21,
        CharListFailed = 22,

        CharCreateInProgress = 23,
        CharCreateSuccess = 24,
        CharCreateError = 25,
        CharCreateFailed = 26,
        CharCreateNameInUse = 27,
        CharCreateDisabled = 28,
        CharCreatePvpTeamsViolation = 29,
        CharCreateServerLimit = 30,
        CharCreateAccountLimit = 31,
        CharCreateServerQueue = 32,
        CharCreateOnlyExisting = 33,
        CharCreateExpansion = 34,
        CharCreateExpansionClass = 35,
        CharCreateCharacterInGuild = 36,
        CharCreateRestrictedRaceclass = 37,
        CharCreateCharacterChooseRace = 38,
        CharCreateCharacterArenaLeader = 39,
        CharCreateCharacterArenaTeam = 40,
        CharCreateCharacterDeleteMail = 41,
        CharCreateCharacterSwapFaction = 42,
        CharCreateCharacterRaceOnly = 43,
        CharCreateCharacterGoldLimit = 44,
        CharCreateForceLogin = 45,
        CharCreateTrial = 46,
        CharCreateTimeout = 47,
        CharCreateThrottle = 48,
        CharCreateAlliedRaceAchievement = 49,
        CharCreateRaceclassAchievement = 50,
        CharCreateCharacterInCommunity = 51,
        CharCreateNewPlayer = 52,
        CharCreateNameReservationFull = 53,
        CharCreateDracthyrDuplicate = 54,
        CharCreateDracthyrLevelRequirement = 55,
        CharCreateDeathknightDuplicate = 56,
        CharCreateDeathknightLevelRequirement = 57,
        CharCreateClassTrialNewcomer = 58,
        CharCreateClassTrialThrottleHour = 59,
        CharCreateClassTrialThrottleDay = 60,
        CharCreateClassTrialThrottleWeek = 61,
        CharCreateClassTrialThrottleAccount = 62,
        CharCreateFactionBalance = 63,
        CharCreateTimerunning = 64,
        CharCreateNeedEntitlement = 65,

        CharDeleteInProgress = 66,
        CharDeleteSuccess = 67,
        CharDeleteFailed = 68,
        CharDeleteFailedCharacterServicePending = 69,
        CharDeleteFailedGuildLeader = 70,
        CharDeleteFailedArenaCaptain = 71,
        CharDeleteFailedHasHeirloomOrMail = 72,
        CharDeleteFailedDeprecated1 = 73,
        CharDeleteFailedHasWowToken = 74,
        CharDeleteFailedDeprecated2 = 75,
        CharDeleteFailedCommunityOwner = 76,
        CharDeleteFailedNeighborhoodOwner = 77,
        CharDeleteFailedHouseOwner = 78,

        CharLoginInProgress = 79,
        CharLoginSuccess = 80,
        CharLoginNoWorld = 81,
        CharLoginDuplicateCharacter = 82,
        CharLoginNoInstances = 83,
        CharLoginFailed = 84,
        CharLoginDisabled = 85,
        CharLoginNoCharacter = 86,
        CharLoginLockedForTransfer = 87,
        CharLoginLockedByBilling = 88,
        CharLoginLockedByMobileAh = 89,
        CharLoginTemporaryGmLock = 90,
        CharLoginLockedByCharacterUpgrade = 91,
        CharLoginLockedByRevokedCharacterUpgrade = 92,
        CharLoginLockedByRevokedVasTransaction = 93,
        CharLoginLockedByRestriction = 94,
        CharLoginLockedForRealmPlaytype = 95,

        CharNameSuccess = 96,
        CharNameFailure = 97,
        CharNameNoName = 98,
        CharNameTooShort = 99,
        CharNameTooLong = 100,
        CharNameInvalidCharacter = 101,
        CharNameMixedLanguages = 102,
        CharNameProfane = 103,
        CharNameReserved = 104,
        CharNameInvalidApostrophe = 105,
        CharNameMultipleApostrophes = 106,
        CharNameThreeConsecutive = 107,
        CharNameInvalidSpace = 108,
        CharNameConsecutiveSpaces = 109,
        CharNameRussianConsecutiveSilentCharacters = 110,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 111,
        CharNameDeclensionDoesntMatchBaseName = 112,
        CharNameSpacesDisallowed = 113,
    }

    public enum CharacterUndeleteResult
    {
        Ok = 0,
        Cooldown = 1,
        CharCreate = 2,
        Disabled = 3,
        NameTakenByThisAccount = 4,
        Unknown = 5
    }

    public enum SrpVersion
    {
        v1 = 1,
        v2 = 2
    }

    public enum SrpHashFunction
    {
        Sha256 = 0,
        Sha512 = 1
    }
}
