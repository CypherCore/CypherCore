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

        CharDeleteInProgress = 65,
        CharDeleteSuccess = 66,
        CharDeleteFailed = 67,
        CharDeleteFailedCharacterServicePending = 68,
        CharDeleteFailedGuildLeader = 69,
        CharDeleteFailedArenaCaptain = 70,
        CharDeleteFailedHasHeirloomOrMail = 71,
        CharDeleteFailedDeprecated1 = 72,
        CharDeleteFailedHasWowToken = 73,
        CharDeleteFailedDeprecated2 = 74,
        CharDeleteFailedCommunityOwner = 75,
        CharDeleteFailedNeighborhoodOwner = 76,
        CharDeleteFailedHouseOwner = 77,

        CharLoginInProgress = 78,
        CharLoginSuccess = 79,
        CharLoginNoWorld = 80,
        CharLoginDuplicateCharacter = 81,
        CharLoginNoInstances = 82,
        CharLoginFailed = 83,
        CharLoginDisabled = 84,
        CharLoginNoCharacter = 85,
        CharLoginLockedForTransfer = 86,
        CharLoginLockedByBilling = 87,
        CharLoginLockedByMobileAh = 88,
        CharLoginTemporaryGmLock = 89,
        CharLoginLockedByCharacterUpgrade = 90,
        CharLoginLockedByRevokedCharacterUpgrade = 91,
        CharLoginLockedByRevokedVasTransaction = 92,
        CharLoginLockedByRestriction = 93,
        CharLoginLockedForRealmPlaytype = 94,

        CharNameSuccess = 95,
        CharNameFailure = 96,
        CharNameNoName = 97,
        CharNameTooShort = 98,
        CharNameTooLong = 99,
        CharNameInvalidCharacter = 100,
        CharNameMixedLanguages = 101,
        CharNameProfane = 102,
        CharNameReserved = 103,
        CharNameInvalidApostrophe = 104,
        CharNameMultipleApostrophes = 105,
        CharNameThreeConsecutive = 106,
        CharNameInvalidSpace = 107,
        CharNameConsecutiveSpaces = 108,
        CharNameRussianConsecutiveSilentCharacters = 109,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 110,
        CharNameDeclensionDoesntMatchBaseName = 111,
        CharNameSpacesDisallowed = 112,
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
