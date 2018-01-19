/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
        CharCreateLevelRequirement = 36,
        CharCreateUniqueClassLimit = 37,
        CharCreateCharacterInGuild = 38,
        CharCreateRestrictedRaceclass = 39,
        CharCreateCharacterChooseRace = 40,
        CharCreateCharacterArenaLeader = 41,
        CharCreateCharacterDeleteMail = 42,
        CharCreateCharacterSwapFaction = 43,
        CharCreateCharacterRaceOnly = 44,
        CharCreateCharacterGoldLimit = 45,
        CharCreateForceLogin = 46,
        CharCreateTrial = 47,
        CharCreateTimeout = 48,
        CharCreateThrottle = 49,

        CharDeleteInProgress = 50,
        CharDeleteSuccess = 51,
        CharDeleteFailed = 52,
        CharDeleteFailedLockedForTransfer = 53,
        CharDeleteFailedGuildLeader = 54,
        CharDeleteFailedArenaCaptain = 55,
        CharDeleteFailedHasHeirloomOrMail = 56,
        CharDeleteFailedUpgradeInProgress = 57,
        CharDeleteFailedHasWowToken = 58,
        CharDeleteFailedVasTransactionInProgress = 59,

        CharLoginInProgress = 60,
        CharLoginSuccess = 61,
        CharLoginNoWorld = 62,
        CharLoginDuplicateCharacter = 63,
        CharLoginNoInstances = 64,
        CharLoginFailed = 65,
        CharLoginDisabled = 66,
        CharLoginNoCharacter = 67,
        CharLoginLockedForTransfer = 68,
        CharLoginLockedByBilling = 69,
        CharLoginLockedByMobileAh = 70,
        CharLoginTemporaryGmLock = 71,
        CharLoginLockedByCharacterUpgrade = 72,
        CharLoginLockedByRevokedCharacterUpgrade = 73,
        CharLoginLockedByRevokedVasTransaction = 74,

        CharNameSuccess = 75,
        CharNameFailure = 76,
        CharNameNoName = 77,
        CharNameTooShort = 78,
        CharNameTooLong = 79,
        CharNameInvalidCharacter = 80,
        CharNameMixedLanguages = 81,
        CharNameProfane = 82,
        CharNameReserved = 83,
        CharNameInvalidApostrophe = 84,
        CharNameMultipleApostrophes = 85,
        CharNameThreeConsecutive = 86,
        CharNameInvalidSpace = 87,
        CharNameConsecutiveSpaces = 88,
        CharNameRussianConsecutiveSilentCharacters = 89,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 90,
        CharNameDeclensionDoesntMatchBaseName = 91
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
