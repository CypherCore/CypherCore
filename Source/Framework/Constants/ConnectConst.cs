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
        LockedByRevokedCharacterUpgrade = 11
    }
}
