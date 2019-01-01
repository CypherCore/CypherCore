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
 */﻿

namespace Framework.Constants
{
    public enum RollType
    {
        Pass = 0,
        Need = 1,
        Greed = 2,
        Disenchant = 3,
        NotEmitedYet = 4,
        NotValid = 5,

        MaxTypes = 4,
    }

    public enum RollMask
    {
        Pass = 0x01,
        Need = 0x02,
        Greed = 0x04,
        Disenchant = 0x08,

        AllNoDisenchant = 0x07,
        AllMask = 0x0f
    }

    public enum LootMethod
    {
        FreeForAll = 0,
        MasterLoot = 2,
        GroupLoot = 3,
        PersonalLoot = 5
    }

    public enum LootModes
    {
        Default = 0x1,
        HardMode1 = 0x2,
        HardMode2 = 0x4,
        HardMode3 = 0x8,
        HardMode4 = 0x10,
        JunkFish = 0x8000
    }

    public enum PermissionTypes
    {
        All = 0,
        Group = 1,
        Master = 2,
        Restricted = 3,
        Owner = 5,
        None = 6
    }

    public enum LootType
    {
        None = 0,
        Corpse = 1,
        Pickpocketing = 2,
        Fishing = 3,
        Disenchanting = 4,
        // Ignored Always By Client
        Skinning = 6,
        Prospecting = 7,
        Milling = 8,

        Fishinghole = 20,                       // Unsupported By Client, Sending Fishing Instead
        Insignia = 21,                        // Unsupported By Client, Sending Corpse Instead
        FishingJunk = 22 // unsupported by client, sending LOOT_FISHING instead
    }

    public enum LootError
    {
        DidntKill = 0,    // You don't have permission to loot that corpse.
        TooFar = 4,    // You are too far away to loot that corpse.
        BadFacing = 5,    // You must be facing the corpse to loot it.
        Locked = 6,    // Someone is already looting that corpse.
        NotStanding = 8,    // You need to be standing up to loot something!
        Stunned = 9,    // You can't loot anything while stunned!
        PlayerNotFound = 10,   // Player not found
        PlayTimeExceeded = 11,   // Maximum play time exceeded
        MasterInvFull = 12,   // That player's inventory is full
        MasterUniqueItem = 13,   // Player has too many of that item already
        MasterOther = 14,   // Can't assign item to that player
        AlreadPickPocketed = 15,   // Your target has already had its pockets picked
        NotWhileShapeShifted = 16,    // You can't do that while shapeshifted.
        NoLoot = 17    // There is no loot.
    }

    // type of Loot Item in Loot View
    public enum LootSlotType
    {
        AllowLoot = 0,                     // Player Can Loot The Item.
        RollOngoing = 1,                   // Roll Is Ongoing. Player Cannot Loot.
        Locked = 2,                        // Item Is Shown In Red. Player Cannot Loot.
        Master = 3,                        // Item Can Only Be Distributed By Group Loot Master.
        Owner = 4                         // Ignore Binding Confirmation And Etc, For Single Player Looting
    }
}
