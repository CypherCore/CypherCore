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

        CharDeleteInProgress = 52,
        CharDeleteSuccess = 53,
        CharDeleteFailed = 54,
        CharDeleteFailedLockedForTransfer = 55,
        CharDeleteFailedGuildLeader = 56,
        CharDeleteFailedArenaCaptain = 57,
        CharDeleteFailedHasHeirloomOrMail = 58,
        CharDeleteFailedUpgradeInProgress = 59,
        CharDeleteFailedHasWowToken = 60,
        CharDeleteFailedVasTransactionInProgress = 61,
        CharDeleteFailedCommunityOwner = 62,

        CharLoginInProgress = 63,
        CharLoginSuccess = 64,
        CharLoginNoWorld = 65,
        CharLoginDuplicateCharacter = 66,
        CharLoginNoInstances = 67,
        CharLoginFailed = 68,
        CharLoginDisabled = 69,
        CharLoginNoCharacter = 70,
        CharLoginLockedForTransfer = 71,
        CharLoginLockedByBilling = 72,
        CharLoginLockedByMobileAh = 73,
        CharLoginTemporaryGmLock = 74,
        CharLoginLockedByCharacterUpgrade = 75,
        CharLoginLockedByRevokedCharacterUpgrade = 76,
        CharLoginLockedByRevokedVasTransaction = 77,
        CharLoginLockedByRestriction = 78,
        CharLoginLockedForRealmPlaytype = 79,

        CharNameSuccess = 80,
        CharNameFailure = 81,
        CharNameNoName = 82,
        CharNameTooShort = 83,
        CharNameTooLong = 84,
        CharNameInvalidCharacter = 85,
        CharNameMixedLanguages = 86,
        CharNameProfane = 87,
        CharNameReserved = 88,
        CharNameInvalidApostrophe = 89,
        CharNameMultipleApostrophes = 90,
        CharNameThreeConsecutive = 91,
        CharNameInvalidSpace = 92,
        CharNameConsecutiveSpaces = 93,
        CharNameRussianConsecutiveSilentCharacters = 94,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 95,
        CharNameDeclensionDoesntMatchBaseName = 96,
        CharNameSpacesDisallowed = 97,
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
