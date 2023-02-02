// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum ConnectionType
    {
        Realm = 0,
        Instance = 1,
        Max
    }

    public enum ConnectToSerial
    {
        None = 0,
        Realm = 14,
        WorldAttempt1 = 17,
        WorldAttempt2 = 35,
        WorldAttempt3 = 53,
        WorldAttempt4 = 71,
        WorldAttempt5 = 89
    }

    public enum LoginFailureReason
    {
        Failed = 0,
        NoWorld = 1,
        DuplicateCharacter = 2,
        NoInstances = 3,
        Disabled = 4,
        NoCharacter = 5,
        LockedForTransfer = 6,
        LockedByBilling = 7,
        LockedByMobileAH = 8,
        TemporaryGMLock = 9,
        LockedByCharacterUpgrade = 10,
        LockedByRevokedCharacterUpgrade = 11,
        LockedByRevokedVASTransaction = 17,
        LockedByRestriction = 19,
        LockedForRealmPlaytype = 23
    }
}
