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

        CharCreateCharacterInCommunity = 52,
        CharDeleteInProgress = 53,
        CharDeleteSuccess = 54,
        CharDeleteFailed = 55,
        CharDeleteFailedLockedForTransfer = 56,
        CharDeleteFailedGuildLeader = 57,
        CharDeleteFailedArenaCaptain = 58,
        CharDeleteFailedHasHeirloomOrMail = 59,
        CharDeleteFailedUpgradeInProgress = 60,
        CharDeleteFailedHasWowToken = 61,
        CharDeleteFailedVasTransactionInProgress = 62,
        CharDeleteFailedCommunityOwner = 63,
        CharLoginInProgress = 64,
        CharLoginSuccess = 65,
        CharLoginNoWorld = 66,
        CharLoginDuplicateCharacter = 67,
        CharLoginNoInstances = 68,
        CharLoginFailed = 69,
        CharLoginDisabled = 70,
        CharLoginNoCharacter = 71,
        CharLoginLockedForTransfer = 72,
        CharLoginLockedByBilling = 73,
        CharLoginLockedByMobileAh = 74,
        CharLoginTemporaryGmLock = 75,
        CharLoginLockedByCharacterUpgrade = 76,
        CharLoginLockedByRevokedCharacterUpgrade = 77,
        CharLoginLockedByRevokedVasTransaction = 78,
        CharLoginLockedByRestriction = 79,
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
        CharNameDeclensionDoesntMatchBaseName = 96
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
