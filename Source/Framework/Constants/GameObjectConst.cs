// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        CraftingTable = 61,
        PerksProgramChest = 62,

        Max
    }

    public enum GameObjectState
    {
        Active = 0,
        Ready = 1,
        Destroyed = 2,
        TransportActive = 24,
        TransportStopped = 25,
        Max = 3
    }

    public enum GameObjectDynamicLowFlags : ushort
    {
        HideModel = 0x02,
        Activate = 0x04,
        Animate = 0x08,
        Depleted = 0x10,
        Sparkle = 0x20,
        Stopped = 0x40,
        NoInterract = 0x80,
        InvertedMovement = 0x100,
        InteractCond = 0x200,               // Cannot interact (requires GO_DYNFLAG_LO_ACTIVATE to enable interaction clientside)
        Highlight = 0x4000, // Allows object highlight when GO_DYNFLAG_LO_ACTIVATE are set, not only when player is on quest determined by Data fields
        StateTransitionAnimDone = 0x8000,      // don't play state transition anim on entering visibility
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

    public enum GameObjectActions
    {
        // Name from client executable          // Comments
        None = 0,    // -NONE-
        AnimateCustom0 = 1,    // Animate Custom0
        AnimateCustom1 = 2,    // Animate Custom1
        AnimateCustom2 = 3,    // Animate Custom2
        AnimateCustom3 = 4,    // Animate Custom3
        Disturb = 5,    // Disturb                              // Triggers trap
        Unlock = 6,    // Unlock                               // Resets GO_FLAG_LOCKED
        Lock = 7,    // Lock                                 // Sets GO_FLAG_LOCKED
        Open = 8,    // Open                                 // Sets GO_STATE_ACTIVE
        OpenAndUnlock = 9,    // Open + Unlock                        // Sets GO_STATE_ACTIVE and resets GO_FLAG_LOCKED
        Close = 10,   // Close                                // Sets GO_STATE_READY
        ToggleOpen = 11,   // Toggle Open
        Destroy = 12,   // Destroy                              // Sets GO_STATE_DESTROYED
        Rebuild = 13,   // Rebuild                              // Resets from GO_STATE_DESTROYED
        Creation = 14,   // Creation
        Despawn = 15,   // Despawn
        MakeInert = 16,   // Make Inert                           // Disables interactions
        MakeActive = 17,   // Make Active                          // Enables interactions
        CloseAndLock = 18,   // Close + Lock                         // Sets GO_STATE_READY and sets GO_FLAG_LOCKED
        UseArtKit0 = 19,   // Use ArtKit0                          // 46904: 121
        UseArtKit1 = 20,   // Use ArtKit1                          // 36639: 81, 46903: 122
        UseArtKit2 = 21,   // Use ArtKit2
        UseArtKit3 = 22,   // Use ArtKit3
        SetTapList = 23,   // Set Tap List
        GoTo1stFloor = 24,   // Go to 1st floor
        GoTo2ndFloor = 25,   // Go to 2nd floor
        GoTo3rdFloor = 26,   // Go to 3rd floor
        GoTo4thFloor = 27,   // Go to 4th floor
        GoTo5thFloor = 28,   // Go to 5th floor
        GoTo6thFloor = 29,   // Go to 6th floor
        GoTo7thFloor = 30,   // Go to 7th floor
        GoTo8thFloor = 31,   // Go to 8th floor
        GoTo9thFloor = 32,   // Go to 9th floor
        GoTo10thFloor = 33,   // Go to 10th floor
        UseArtKit4 = 34,   // Use ArtKit4
        PlayAnimKit = 35,   // Play Anim Kit "{AnimKit}"
        OpenAndPlayAnimKit = 36,   // Open + Play Anim Kit "{AnimKit}"
        CloseAndPlayAnimKit = 37,   // Close + Play Anim Kit "{AnimKit}"
        PlayOneShotAnimKit = 38,   // Play One-shot Anim Kit "{AnimKit}"
        StopAnimKit = 39,   // Stop Anim Kit
        OpenAndStopAnimKit = 40,   // Open + Stop Anim Kit
        CloseAndStopAnimKit = 41,   // Close + Stop Anim Kit
        PlaySpellVisual = 42,   // Play Spell Visual "{SpellVisual}"
        StopSpellVisual = 43,   // Stop Spell Visual
        SetTappedToChallengePlayers = 44,   // Set Tapped to Challenge Players
        Max
    }

    public enum TransportMovementState
    {
        Moving,
        WaitingOnPauseWaypoint
    }

    // enum for GAMEOBJECT_TYPE_NEW_FLAG
    // values taken from world state
    public enum FlagState
    {
        InBase = 1,
        Taken,
        Dropped,
        Respawning
    }
}
