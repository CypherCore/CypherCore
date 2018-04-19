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

        CharCreateAlliedRaceAchievement = 50,
        CharCreateLevelRequirementDemonHunter = 51,

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

        CharNameSuccess = 78,
        CharNameFailure = 79,
        CharNameNoName = 80,
        CharNameTooShort = 81,
        CharNameTooLong = 82,
        CharNameInvalidCharacter = 83,
        CharNameMixedLanguages = 84,
        CharNameProfane = 85,
        CharNameReserved = 86,
        CharNameInvalidApostrophe = 87,
        CharNameMultipleApostrophes = 88,
        CharNameThreeConsecutive = 89,
        CharNameInvalidSpace = 90,
        CharNameConsecutiveSpaces = 91,
        CharNameRussianConsecutiveSilentCharacters = 92,
        CharNameRussianSilentCharacterAtBeginningOrEnd = 93,
        CharNameDeclensionDoesntMatchBaseName = 94
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
