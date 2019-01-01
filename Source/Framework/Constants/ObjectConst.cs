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
        Conversation = 13
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
        Sceneobject = 0x1000,
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
        ClientConnection = 48
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
        TimedDespawnOOC = 4,             // despawns after a specified time after the creature is out of combat
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

    public enum SummonType
    {
        None = 0,
        Pet = 1,
        Guardian = 2,
        Minion = 3,
        Totem = 4,
        Minipet = 5,
        Guardian2 = 6,
        Wild2 = 7,
        Wild3 = 8,    // Related to phases and DK prequest line (3.3.5a)
        Vehicle = 9,
        Vehicle2 = 10,   // Oculus and Argent Tournament vehicles (3.3.5a)
        LightWell = 11,
        Jeeves = 12,
        Unk13 = 13
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
        Unk7 = 7,
        Unk8 = 8,
        Unk9 = 9,
        Unk10 = 10,
        Unk11 = 11,
        Unk12 = 12,
        Unk13 = 13,
        Unk14 = 14,
        Unk15 = 15,
        Unk16 = 16,
        Unk17 = 17,
        Unk18 = 18,
        Unk19 = 19,
        Unk20 = 20,
        Unk21 = 21,
        Unk22 = 22,
        Unk23 = 23,
        Unk24 = 24,
        Unk25 = 25,
        Unk26 = 26,
        Unk27 = 27,
        Unk28 = 28,
        Unk29 = 29,
        Unk30 = 30,
        Unk31 = 31,
        Unk32 = 32,
        Unk33 = 33,
        Unk34 = 34,
        Unk35 = 35,
        Unk36 = 36,
        Unk37 = 37,

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
