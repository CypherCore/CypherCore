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
    public enum GameObjectTypes : byte
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
        ArtifactForge = 47,
        UILink = 48,
        KeystoneReceptacle = 49,
        GatheringNode = 50,
        ChallengeModeReward = 51,
        Multi = 52,
        SiegeableMulti = 53,
        SiegeableMo = 54,
        PvpReward = 55,
        Max = 56
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
        NoInteract = 0x10,
        Sparkle = 0x20,
        Stopped = 0x40
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
        InteractDistanceUsesTemplateModel = 0x80000, // client checks interaction distance from model sent in SMSG_QUERY_GAMEOBJECT_RESPONSE instead of GAMEOBJECT_DISPLAYID
        MapObject = 0x00100000                    // pre-7.0 model loading used to be controlled by file extension (wmo vs m2)
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
}
