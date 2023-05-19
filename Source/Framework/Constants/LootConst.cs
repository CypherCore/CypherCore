// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.﻿

namespace Framework.Constants
{
    public enum RollVote
    {
        Pass = 0,
        Need = 1,
        Greed = 2,
        Disenchant = 3,
        NotEmitedYet = 4,
        NotValid = 5
    }

    public enum RollMask
    {
        Pass = 0x01,
        Need = 0x02,
        Greed = 0x04,
        Disenchant = 0x08,
        Transmog = 0x10,

        AllNoDisenchant = 0x07,
        AllMask = 0x0f
    }

    public enum LootMethod
    {
        FreeForAll = 0,
        RoundRobin = 1,
        MasterLoot = 2,
        GroupLoot = 3,
        NeedBeforeGreed = 4,
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

    public enum LootType
    {
        None = 0,
        Corpse = 1,
        Pickpocketing = 2,
        Fishing = 3,
        Disenchanting = 4,
        Item = 5,
        Skinning = 6,
        GatheringNode = 8,
        Chest = 9,
        CorpsePersonal = 14,

        Fishinghole = 20,                       // Unsupported By Client, Sending Fishing Instead
        Insignia = 21,                        // Unsupported By Client, Sending Corpse Instead
        FishingJunk = 22, // unsupported by client, sending LOOT_FISHING instead
        Prospecting = 23,
        Milling = 24
    }

    public enum LootItemType
    {
        Item = 0,
        Currency = 1
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

    public enum LootRollIneligibilityReason
    {
        None = 0,
        UnusableByClass = 1, // Your class may not roll need on this item.
        MaxUniqueItemCount = 2, // You already have the maximum amount of this item.
        CannotBeDisenchanted = 3, // This item may not be disenchanted.
        EnchantingSkillTooLow = 4, // You do not have an Enchanter of skill %d in your group.
        NeedDisabled = 5, // Need rolls are disabled for this item.
        OwnBetterItem = 6  // You already have a powerful version of this item.
    }
}
