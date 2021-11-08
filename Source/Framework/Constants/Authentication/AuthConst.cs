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

        CharDeleteInProgress = 51,
        CharDeleteSuccess = 52,
        CharDeleteFailed = 53,
        CharDeleteFailedLockedForTransfer = 54,
        CharDeleteFailedGuildLeader = 55,
        CharDeleteFailedArenaCaptain = 56,
        CharDeleteFailedHasHeirloomOrMail = 57,
        CharDeleteFailedUpgradeInProgress = 58,
        CharDeleteFailedHasWowToken = 59,
        CharDeleteFailedVasTransactionInProgress = 60,
        CharDeleteFailedCommunityOwner = 61,

        CharLoginInProgress = 62,
        CharLoginSuccess = 63,
        CharLoginNoWorld = 64,
        CharLoginDuplicateCharacter = 65,
        CharLoginNoInstances = 66,
        CharLoginFailed = 67,
        CharLoginDisabled = 68,
        CharLoginNoCharacter = 69,
        CharLoginLockedForTransfer = 70,
        CharLoginLockedByBilling = 71,
        CharLoginLockedByMobileAh = 72,
        CharLoginTemporaryGmLock = 73,
        CharLoginLockedByCharacterUpgrade = 74,
        CharLoginLockedByRevokedCharacterUpgrade = 75,
        CharLoginLockedByRevokedVasTransaction = 76,
        CharLoginLockedByRestriction = 77,

        CharLoginLockedForRealmPlaytype = 78,

        CharNameSuccess = 79,
        CharNameFailure = 80,
        CharNameNoName = 81,
        CharNameTooShort = 82,
        CharNameTooLong = 83,
        CharNameInvalidCharacter = 84,
        CharNameMixedLanguages = 85,
        CharNameProfane = 86,
        CharNameReserved = 87,
        CharNameInvalidApostrophe = 88,
        CharNameMultipleApostrophes = 89,
        CharNameThreeConsecutive = 90,
        CharNameInvalidSpace = 91,
        CharNameConsecutiveSpaces = 92,
        CharNameRussianConsecutiveSilentCharacters = 93,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 94,
        CharNameDeclensionDoesntMatchBaseName = 95,
        CharNameSpacesDisallowed = 96,
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
