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
 */﻿

namespace Framework.Constants
{
    public enum GameObjectTypes : int
    {
        Door = 0,
        Button = 1,
        QuestGiver = 2,
        Chest = 3,
        Binder = 4,
        Generic = 5,
        Trap = 6,
        Chair = 7,
        SpellFocus = 8,
        Text = 9,
        Goober = 10,
        Transport = 11,
        AreaDamage = 12,
        Camera = 13,
        MapObject = 14,
        MapObjTransport = 15,
        DuelArbiter = 16,
        FishingNode = 17,
        Ritual = 18,
        Mailbox = 19,
        DoNotUse = 20,
        GuardPost = 21,
        SpellCaster = 22,
        MeetingStone = 23,
        FlagStand = 24,
        FishingHole = 25,
        FlagDrop = 26,
        MiniGame = 27,
        DoNotUse2 = 28,
        ControlZone = 29,
        AuraGenerator = 30,
        DungeonDifficulty = 31,
        BarberChair = 32,
        DestructibleBuilding = 33,
        GuildBank = 34,
        TrapDoor = 35,
        NewFlag = 36,
        NewFlagDrop = 37,
        GarrisonBuilding = 38,
        GarrisonPlot = 39,
        ClientCreature = 40,
        ClientItem = 41,
        CapturePoint = 42,
        PhaseableMo = 43,
        GarrisonMonument = 44,
        GarrisonShipment = 45,
        GarrisonMonumentPlaque = 46,
        ItemForge = 47,
        UILink = 48,
        KeystoneReceptacle = 49,
        GatheringNode = 50,
        ChallengeModeReward = 51,
        Multi = 52,
        SiegeableMulti = 53,
        SiegeableMo = 54,
        PvpReward = 55,
        PlayerChoiceChest = 56,
        LegendaryForge = 57,
        GarrTalentTree = 58,
        WeeklyRewardChest = 59,
        ClientModel = 60,
        Max = 61
    }

    public enum GameObjectState
    {
        Active = 0,
        Ready = 1,
        ActiveAlternative = 2,
        TransportActive = 24,
        TransportStopped = 25,
        Max = 3
    }

    public enum GameObjectDynamicLowFlags
    {
        HideModel = 0x02,
        Activate = 0x04,
        Animate = 0x08,
        Depleted = 0x10,
        Sparkle = 0x20,
        Stopped = 0x40,
        NoInterract = 0x80,
        InvertedMovement = 0x100,
        Highlight = 0x200
    }

    public enum GameObjectFlags
    {
        InUse = 0x01,                   // Disables Interaction While Animated
        Locked = 0x02,                  // Require Key, Spell, Event, Etc To Be Opened. Makes "Locked" Appear In Tooltip
        InteractCond = 0x04,            // cannot interact (condition to interact - requires GO_DYNFLAG_LO_ACTIVATE to enable interaction clientside)
        Transport = 0x08,               // Any Kind Of Transport? Object Can Transport (Elevator, Boat, Car)
        NotSelectable = 0x10,           // Not Selectable Even In Gm Mode
        NoDespawn = 0x20,               // Never Despawn, Typically For Doors, They Just Change State
        AiObstacle = 0x40,              // makes the client register the object in something called AIObstacleMgr, unknown what it does
        FreezeAnimation = 0x80,
        Damaged = 0x200,
        Destroyed = 0x400,

        IgnoreCurrentStateForUseSpell = 0x4000, // Allows casting use spell without checking current state (opening open gameobjects, unlocking unlocked gameobjects and closing closed gameobjects)
        InteractDistanceIgnoresModel = 0x8000, // Client completely ignores model bounds for interaction distance check
        IgnoreCurrentStateForUseSpellExceptUnlocked = 0x40000, // Allows casting use spell without checking current state except unlocking unlocked gamobjets (opening open gameobjects and closing closed gameobjects)
        InteractDistanceUsesTemplateModel = 0x80000, // client checks interaction distance from model sent in SMSG_QUERY_GAMEOBJECT_RESPONSE instead of GAMEOBJECT_DISPLAYID
        MapObject = 0x100000, // pre-7.0 model loading used to be controlled by file extension (wmo vs m2)
        InMultiUse = 0x200000, // GO_FLAG_IN_USE equivalent for objects usable by multiple players
        LowPrioritySelection = 0x4000000, // client will give lower cursor priority to this object when multiple objects overlap
    }

    public enum LootState
    {
        NotReady = 0,
        Ready,                                               // can be ready but despawned, and then not possible activate until spawn
        Activated,
        JustDeactivated
    }

    public enum GameObjectDestructibleState
    {
        Intact = 0,
        Damaged = 1,
        Destroyed = 2,
        Rebuilding = 3
    }

    public enum GameObjectSummonType
    {
        TimedOrCorpseDespawn = 0,    // despawns after a specified time OR when the summoner dies
        TimedDespawn = 1     // despawns after a specified time
    }
}
