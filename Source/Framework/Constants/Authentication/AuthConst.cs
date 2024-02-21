﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
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

        CharDeleteInProgress = 63,
        CharDeleteSuccess = 64,
        CharDeleteFailed = 65,
        CharDeleteFailedCharacterServicePending = 66,
        CharDeleteFailedGuildLeader = 67,
        CharDeleteFailedArenaCaptain = 68,
        CharDeleteFailedHasHeirloomOrMail = 69,
        CharDeleteFailedDeprecated1 = 70,
        CharDeleteFailedHasWowToken = 71,
        CharDeleteFailedDeprecated2 = 72,
        CharDeleteFailedCommunityOwner = 73,

        CharLoginInProgress = 74,
        CharLoginSuccess = 75,
        CharLoginNoWorld = 76,
        CharLoginDuplicateCharacter = 77,
        CharLoginNoInstances = 78,
        CharLoginFailed = 79,
        CharLoginDisabled = 80,
        CharLoginNoCharacter = 81,
        CharLoginLockedForTransfer = 82,
        CharLoginLockedByBilling = 83,
        CharLoginLockedByMobileAh = 84,
        CharLoginTemporaryGmLock = 85,
        CharLoginLockedByCharacterUpgrade = 86,
        CharLoginLockedByRevokedCharacterUpgrade = 87,
        CharLoginLockedByRevokedVasTransaction = 88,
        CharLoginLockedByRestriction = 89,
        CharLoginLockedForRealmPlaytype = 90,

        CharNameSuccess = 91,
        CharNameFailure = 92,
        CharNameNoName = 93,
        CharNameTooShort = 94,
        CharNameTooLong = 95,
        CharNameInvalidCharacter = 96,
        CharNameMixedLanguages = 97,
        CharNameProfane = 98,
        CharNameReserved = 99,
        CharNameInvalidApostrophe = 100,
        CharNameMultipleApostrophes = 101,
        CharNameThreeConsecutive = 102,
        CharNameInvalidSpace = 103,
        CharNameConsecutiveSpaces = 104,
        CharNameRussianConsecutiveSilentCharacters = 105,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 106,
        CharNameDeclensionDoesntMatchBaseName = 107,
        CharNameSpacesDisallowed = 108,
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
