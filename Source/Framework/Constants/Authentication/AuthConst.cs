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
        CharCreateCharacterInCommunity = 50,
        CharCreateNewPlayer = 51,
        CharCreateNameReservationFull = 52,
        CharCreateDracthyrDuplicate = 53,
        CharCreateDracthyrLevelRequirement = 54,
        CharCreateDeathknightDuplicate = 55,
        CharCreateDeathknightLevelRequirement = 56,
        CharCreateClassTrialNewcomer = 57,
        CharCreateClassTrialThrottleHour = 58,
        CharCreateClassTrialThrottleDay = 59,
        CharCreateClassTrialThrottleWeek = 60,
        CharCreateClassTrialThrottleAccount = 61,
        CharCreateFactionBalance = 62,
        CharCreateTimerunning = 63,

        CharDeleteInProgress = 64,
        CharDeleteSuccess = 65,
        CharDeleteFailed = 66,
        CharDeleteFailedCharacterServicePending = 67,
        CharDeleteFailedGuildLeader = 68,
        CharDeleteFailedArenaCaptain = 69,
        CharDeleteFailedHasHeirloomOrMail = 70,
        CharDeleteFailedDeprecated1 = 71,
        CharDeleteFailedHasWowToken = 72,
        CharDeleteFailedDeprecated2 = 73,
        CharDeleteFailedCommunityOwner = 74,

        CharLoginInProgress = 75,
        CharLoginSuccess = 76,
        CharLoginNoWorld = 77,
        CharLoginDuplicateCharacter = 78,
        CharLoginNoInstances = 79,
        CharLoginFailed = 80,
        CharLoginDisabled = 81,
        CharLoginNoCharacter = 82,
        CharLoginLockedForTransfer = 83,
        CharLoginLockedByBilling = 84,
        CharLoginLockedByMobileAh = 85,
        CharLoginTemporaryGmLock = 86,
        CharLoginLockedByCharacterUpgrade = 87,
        CharLoginLockedByRevokedCharacterUpgrade = 88,
        CharLoginLockedByRevokedVasTransaction = 89,
        CharLoginLockedByRestriction = 90,
        CharLoginLockedForRealmPlaytype = 91,

        CharNameSuccess = 92,
        CharNameFailure = 93,
        CharNameNoName = 94,
        CharNameTooShort = 95,
        CharNameTooLong = 96,
        CharNameInvalidCharacter = 97,
        CharNameMixedLanguages = 98,
        CharNameProfane = 99,
        CharNameReserved = 100,
        CharNameInvalidApostrophe = 101,
        CharNameMultipleApostrophes = 102,
        CharNameThreeConsecutive = 103,
        CharNameInvalidSpace = 104,
        CharNameConsecutiveSpaces = 105,
        CharNameRussianConsecutiveSilentCharacters = 106,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 107,
        CharNameDeclensionDoesntMatchBaseName = 108,
        CharNameSpacesDisallowed = 109,
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
