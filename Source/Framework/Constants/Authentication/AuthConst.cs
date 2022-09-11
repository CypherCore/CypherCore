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
        CharCreateCharacterDeleteMail = 40,
        CharCreateCharacterSwapFaction = 41,
        CharCreateCharacterRaceOnly = 42,
        CharCreateCharacterGoldLimit = 43,
        CharCreateForceLogin = 44,
        CharCreateTrial = 45,
        CharCreateTimeout = 46,
        CharCreateThrottle = 47,
        CharCreateAlliedRaceAchievement = 48,
        CharCreateCharacterInCommunity = 49,
        CharCreateNewPlayer = 50,
        CharCreateNameReservationFull = 51,

        CharCreateClassTrialNewcomer = 52,
        CharCreateClassTrialThrottleHour = 53,
        CharCreateClassTrialThrottleDay = 54,
        CharCreateClassTrialThrottleWeek = 55,
        CharCreateClassTrialThrottleAccount = 56,

        CharDeleteInProgress = 57,
        CharDeleteSuccess = 58,
        CharDeleteFailed = 59,
        CharDeleteFailedLockedForTransfer = 60,
        CharDeleteFailedGuildLeader = 61,
        CharDeleteFailedArenaCaptain = 62,
        CharDeleteFailedHasHeirloomOrMail = 63,
        CharDeleteFailedUpgradeInProgress = 64,
        CharDeleteFailedHasWowToken = 65,
        CharDeleteFailedVasTransactionInProgress = 66,
        CharDeleteFailedCommunityOwner = 67,

        CharLoginInProgress = 68,
        CharLoginSuccess = 69,
        CharLoginNoWorld = 70,
        CharLoginDuplicateCharacter = 71,
        CharLoginNoInstances = 72,
        CharLoginFailed = 73,
        CharLoginDisabled = 74,
        CharLoginNoCharacter = 75,
        CharLoginLockedForTransfer = 76,
        CharLoginLockedByBilling = 77,
        CharLoginLockedByMobileAh = 78,
        CharLoginTemporaryGmLock = 79,
        CharLoginLockedByCharacterUpgrade = 80,
        CharLoginLockedByRevokedCharacterUpgrade = 81,
        CharLoginLockedByRevokedVasTransaction = 82,
        CharLoginLockedByRestriction = 83,
        CharLoginLockedForRealmPlaytype = 84,

        CharNameSuccess = 85,
        CharNameFailure = 86,
        CharNameNoName = 87,
        CharNameTooShort = 88,
        CharNameTooLong = 89,
        CharNameInvalidCharacter = 90,
        CharNameMixedLanguages = 91,
        CharNameProfane = 92,
        CharNameReserved = 93,
        CharNameInvalidApostrophe = 94,
        CharNameMultipleApostrophes = 95,
        CharNameThreeConsecutive = 96,
        CharNameInvalidSpace = 97,
        CharNameConsecutiveSpaces = 98,
        CharNameRussianConsecutiveSilentCharacters = 99,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 100,
        CharNameDeclensionDoesntMatchBaseName = 101,
        CharNameSpacesDisallowed = 102,
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
}
