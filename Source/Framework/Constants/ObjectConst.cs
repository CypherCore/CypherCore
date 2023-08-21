// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum TypeId
    {
        Object = 0,
        Item = 1,
        Container = 2,
        AzeriteEmpoweredItem = 3,
        AzeriteItem = 4,
        Unit = 5,
        Player = 6,
        ActivePlayer = 7,
        GameObject = 8,
        DynamicObject = 9,
        Corpse = 10,
        AreaTrigger = 11,
        SceneObject = 12,
        Conversation = 13,
        Max = 14
    }

    public enum TypeMask
    {
        Object = 0x01,
        Item = 0x02,
        Container = 0x04,
        AzeriteEmpoweredItem = 0x08,
        AzeriteItem = 0x10,
        Unit = 0x20,
        Player = 0x40,
        ActivePlayer = 0x80,
        GameObject = 0x100,
        DynamicObject = 0x200,
        Corpse = 0x400,
        AreaTrigger = 0x800,
        SceneObject = 0x1000,
        Conversation = 0x2000,
        Seer = Player | Unit | DynamicObject
    }

    public enum HighGuid
    {
        Null = 0,
        Uniq = 1,
        Player = 2,
        Item = 3,
        WorldTransaction = 4,
        StaticDoor = 5,   //NYI
        Transport = 6,
        Conversation = 7,
        Creature = 8,
        Vehicle = 9,
        Pet = 10,
        GameObject = 11,
        DynamicObject = 12,
        AreaTrigger = 13,
        Corpse = 14,
        LootObject = 15,
        SceneObject = 16,
        Scenario = 17,
        AIGroup = 18,
        DynamicDoor = 19,
        ClientActor = 20,  //NYI
        Vignette = 21,
        CallForHelp = 22,
        AIResource = 23,
        AILock = 24,
        AILockTicket = 25,
        ChatChannel = 26,
        Party = 27,
        Guild = 28,
        WowAccount = 29,
        BNetAccount = 30,
        GMTask = 31,
        MobileSession = 32,  //NYI
        RaidGroup = 33,
        Spell = 34,
        Mail = 35,
        WebObj = 36,  //NYI
        LFGObject = 37,  //NYI
        LFGList = 38,  //NYI
        UserRouter = 39,
        PVPQueueGroup = 40,
        UserClient = 41,
        PetBattle = 42,  //NYI
        UniqUserClient = 43,
        BattlePet = 44,
        CommerceObj = 45,
        ClientSession = 46,
        Cast = 47,
        ClientConnection = 48,
        ClubFinder = 49,
        ToolsClient = 50,
        WorldLayer = 51,
        ArenaTeam = 52,
        LMMParty = 53,
        LMMLobby = 54,

        Count
    }

    public enum NotifyFlags
    {
        None = 0x00,
        AIRelocation = 0x01,
        VisibilityChanged = 0x02,
        All = 0xFF
    }

    public enum TempSummonType
    {
        TimedOrDeadDespawn = 1,             // despawns after a specified time OR when the creature disappears
        TimedOrCorpseDespawn = 2,             // despawns after a specified time OR when the creature dies
        TimedDespawn = 3,             // despawns after a specified time
        TimedDespawnOutOfCombat = 4,             // despawns after a specified time after the creature is out of combat
        CorpseDespawn = 5,             // despawns instantly after death
        CorpseTimedDespawn = 6,             // despawns after a specified time after death
        DeadDespawn = 7,             // despawns when the creature disappears
        ManualDespawn = 8              // despawns when UnSummon() is called
    }

    public enum SummonCategory
    {
        Wild = 0,
        Ally = 1,
        Pet = 2,
        Puppet = 3,
        Vehicle = 4,
        Unk = 5  // as of patch 3.3.5a only Bone Spike in Icecrown Citadel
        // uses this category
    }

    public enum SummonTitle
    {
        None = 0,
        Pet = 1,
        Guardian = 2,
        Minion = 3,
        Totem = 4,
        Companion = 5,
        Runeblade = 6,
        Construct = 7,
        Opponent = 8,    // Related to phases and DK prequest line (3.3.5a)
        Vehicle = 9,
        Mount = 10,   // Oculus and Argent Tournament vehicles (3.3.5a)
        LightWell = 11,
        Butler = 12,
        Aka = 13,
        Gateway = 14,
        Hatred = 15,
        Statue = 16,
        Spirit = 17,
        WarBanner = 18,
        Heartwarmer = 19,
        HiredBy = 20,
        PurchasedBy = 21,
        Pride = 22,
        TwistedImage = 23,
        NoodleCart = 24,
        InnerDemon = 25,
        Bodyguard = 26,
        Name = 27,
        Squire = 28,
        Champion = 29,
        TheBetrayer = 30,
        EruptingReflection = 31,
        HopelessReflection = 32,
        MalignantReflection = 33,
        WailingReflection = 34,
        Assistant = 35,
        Enforcer = 36,
        Recruit = 37,
        Admirer = 38,
        EvilTwin = 39,
        Greed = 40,
        LostMind = 41,
        ServantOfNZoth = 44
    }

    public enum SummonerType
    {
        Creature = 0,
        GameObject = 1,
        Map = 2
    }

    public enum GhostVisibilityType
    {
        Alive = 0x1,
        Ghost = 0x2
    }

    public enum StealthType
    {
        General = 0,
        Trap = 1,

        Max = 2
    }

    public enum InvisibilityType
    {
        General = 0,
        Unk1 = 1,
        Unk2 = 2,
        Trap = 3,
        Unk4 = 4,
        Unk5 = 5,
        Drunk = 6,
        QuestZoneSpecific1 = 7,
        QuestZoneSpecific2 = 8,
        QuestZoneSpecific3 = 9,
        Unk10 = 10,
        Unk11 = 11,
        QuestZoneSpecific4 = 12,
        QuestZoneSpecific5 = 13,
        QuestZoneSpecific6 = 14,
        QuestZoneSpecific7 = 15,
        QuestZoneSpecific8 = 16,
        QuestZoneSpecific9 = 17,
        QuestZoneSpecific10 = 18,
        QuestZoneSpecific11 = 19,
        QuestZoneSpecific12 = 20,
        QuestZoneSpecific13 = 21,
        QuestZoneSpecific14 = 22,
        QuestZoneSpecific15 = 23,
        QuestZoneSpecific16 = 24,
        QuestZoneSpecific17 = 25,
        QuestZoneSpecific18 = 26,
        QuestZoneSpecific19 = 27,
        QuestZoneSpecific20 = 28,
        QuestZoneSpecific21 = 29,
        QuestZoneSpecific22 = 30,
        QuestZoneSpecific23 = 31,
        QuestZoneSpecific24 = 32,
        QuestZoneSpecific25 = 33,
        QuestZoneSpecific26 = 34,
        QuestZoneSpecific27 = 35,
        QuestZoneSpecific28 = 36,
        QuestZoneSpecific29 = 37,

        Max = 38
    }

    public enum ServerSideVisibilityType
    {
        GM = 0,
        Ghost = 1,
    }

    public enum SessionFlags
    {
        None = 0x00,
        FromRedirect = 0x01,
        HasRedirected = 0x02
    }
}
