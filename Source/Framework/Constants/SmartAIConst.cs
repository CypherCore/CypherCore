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
    public enum SmartScriptType
    {
        Creature = 0,
        GameObject = 1,
        AreaTrigger = 2,
        Event = 3,
        Gossip = 4,
        Quest = 5,
        Spell = 6,
        Transport = 7,
        Instance = 8,
        TimedActionlist = 9,
        Scene = 10, // done
        Max = 11
    }

    public struct SmartScriptTypeMaskId
    {
        public const uint Creature = 1;
        public const uint Gameobject = 2;
        public const uint Areatrigger = 4;
        public const uint Event = 8;
        public const uint Gossip = 16;
        public const uint Quest = 32;
        public const uint Spell = 64;
        public const uint Transport = 128;
        public const uint Instance = 256;
        public const uint TimedActionlist = 512;
        public const uint Scene = 1024;
    }

    public enum SmartPhase
    {
        ALWAYS = 0,
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3,
        Phase4 = 4,
        Phase5 = 5,
        Phase6 = 6,
        Max = 7,
        All = (Phase1 | Phase2  | Phase3  | Phase4  | Phase5  | Phase6),

        Count = 6
    }

    public enum SmartEventFlags
    {
        NotRepeatable = 0x01,           //Event can not repeat
        Difficulty0 = 0x02,             //Event only occurs in instance difficulty 0
        Difficulty1 = 0x04,             //Event only occurs in instance difficulty 1
        Difficulty2 = 0x08,             //Event only occurs in instance difficulty 2
        Difficulty3 = 0x10,             //Event only occurs in instance difficulty 3
        Reserved5 = 0x20,
        Reserved6 = 0x40,
        DebugOnly = 0x80,               //Event only occurs in debug build
        DontReset = 0x100,              //Event will not reset in SmartScript.OnReset()
        WhileCharmed = 0x200,           //Event occurs even if AI owner is charmed

        DifficultyAll = (Difficulty0 | Difficulty1 | Difficulty2 | Difficulty3),
        All = (NotRepeatable | DifficultyAll | Reserved5 | Reserved6 | DebugOnly | DontReset | WhileCharmed)
    }

    public enum SmartRespawnCondition
    {
        None = 0,
        Map = 1,
        Area = 2,
        End = 3
    }

    public enum SmartAITemplate
    {
        Basic = 0, //nothing is preset
        Caster = 1, //spellid, repeatMin, repeatMax, range, manaPCT +JOIN: target_param1 as castFlag
        Turret = 2, //spellid, repeatMin, repeatMax +JOIN: target_param1 as castFlag
        Passive = 3,
        CagedGOPart = 4, //creatureID, give credit at point end?,
        CagedNPCPart = 5, //gameObjectID, despawntime, run?, dist, TextGroupID
        End = 6
    }

    public enum SmartCastFlags
    {
        InterruptPrevious = 0x01,                     //Interrupt any spell casting
        Triggered = 0x02,                     //Triggered (this makes spell cost zero mana and have no cast time)
        //CAST_FORCE_CAST             = 0x04,                     //Forces cast even if creature is out of mana or out of range
        //CAST_NO_MELEE_IF_OOM        = 0x08,                     //Prevents creature from entering melee if out of mana or out of range
        //CAST_FORCE_TARGET_SELF      = 0x10,                     //Forces the target to cast this spell on itself
        AuraNotPresent = 0x20,                      //Only casts the spell if the target does not have an aura from the spell
        CombatMove = 0x40                      //Prevents combat movement if cast successful. Allows movement on range, OOM, LOS
    }

    public enum SmartEvents
    {
        UpdateIc = 0,       // Initialmin, Initialmax, Repeatmin, Repeatmax
        UpdateOoc = 1,       // Initialmin, Initialmax, Repeatmin, Repeatmax
        HealtPct = 2,       // Hpmin%, Hpmax%,  Repeatmin, Repeatmax
        ManaPct = 3,       // Manamin%, Manamax%, Repeatmin, Repeatmax
        Aggro = 4,       // None
        Kill = 5,       // Cooldownmin0, Cooldownmax1, Playeronly2, Else Creature Entry3
        Death = 6,       // None
        Evade = 7,       // None
        SpellHit = 8,       // Spellid, School, Cooldownmin, Cooldownmax
        Range = 9,       // Mindist, Maxdist, Repeatmin, Repeatmax
        OocLos = 10,      // Nohostile, Maxrnage, Cooldownmin, Cooldownmax
        Respawn = 11,      // Type, Mapid, Zoneid
        TargetHealthPct = 12,      // Hpmin%, Hpmax%, Repeatmin, Repeatmax
        VictimCasting = 13,      // Repeatmin, Repeatmax
        FriendlyHealth = 14,      // Hpdeficit, Radius, Repeatmin, Repeatmax
        FriendlyIsCc = 15,      // Radius, Repeatmin, Repeatmax
        FriendlyMissingBuff = 16,      // Spellid, Radius, Repeatmin, Repeatmax
        SummonedUnit = 17,      // Creatureid(0 All), Cooldownmin, Cooldownmax
        TargetManaPct = 18,      // Manamin%, Manamax%, Repeatmin, Repeatmax
        AcceptedQuest = 19,      // Questid(0any)
        RewardQuest = 20,      // Questid(0any)
        ReachedHome = 21,      // None
        ReceiveEmote = 22,      // Emoteid, Cooldownmin, Cooldownmax, Condition, Val1, Val2, Val3
        HasAura = 23,      // Param1 = Spellid, Param2 = Stack Amount, Param3/4 Repeatmin, Repeatmax
        TargetBuffed = 24,      // Param1 = Spellid, Param2 = Stack Amount, Param3/4 Repeatmin, Repeatmax
        Reset = 25,      // Called After Combat, When The Creature Respawn And Spawn.
        IcLos = 26,      // Nohostile, Maxrnage, Cooldownmin, Cooldownmax
        PassengerBoarded = 27,      // Cooldownmin, Cooldownmax
        PassengerRemoved = 28,      // Cooldownmin, Cooldownmax
        Charmed = 29,      // onRemove (0 - on apply, 1 - on remove)
        CharmedTarget = 30,      // None
        SpellhitTarget = 31,      // Spellid, School, Cooldownmin, Cooldownmax
        Damaged = 32,      // Mindmg, Maxdmg, Cooldownmin, Cooldownmax
        DamagedTarget = 33,      // Mindmg, Maxdmg, Cooldownmin, Cooldownmax
        Movementinform = 34,      // Movementtype(Any), Pointid
        SummonDespawned = 35,      // Entry, Cooldownmin, Cooldownmax
        CorpseRemoved = 36,      // None
        AiInit = 37,      // None
        DataSet = 38,      // Id, Value, Cooldownmin, Cooldownmax
        WaypointStart = 39,      // Pointid(0any), Pathid(0any)
        WaypointReached = 40,      // Pointid(0any), Pathid(0any)
        TransportAddplayer = 41,      // None
        TransportAddcreature = 42,      // Entry (0 Any)
        TransportRemovePlayer = 43,      // None
        TransportRelocate = 44,      // Pointid
        InstancePlayerEnter = 45,      // Team (0 Any), Cooldownmin, Cooldownmax
        AreatriggerOntrigger = 46,      // Triggerid(0 Any)
        QuestAccepted = 47,      // None
        QuestObjCompletion = 48,      // None
        QuestCompletion = 49,      // None
        QuestRewarded = 50,      // None
        QuestFail = 51,      // None
        TextOver = 52,      // Groupid From CreatureText,  Creature Entry Who Talks (0 Any)
        ReceiveHeal = 53,      // Minheal, Maxheal, Cooldownmin, Cooldownmax
        JustSummoned = 54,      // None
        WaypointPaused = 55,      // Pointid(0any), Pathid(0any)
        WaypointResumed = 56,      // Pointid(0any), Pathid(0any)
        WaypointStopped = 57,      // Pointid(0any), Pathid(0any)
        WaypointEnded = 58,      // Pointid(0any), Pathid(0any)
        TimedEventTriggered = 59,      // Id
        Update = 60,      // Initialmin, Initialmax, Repeatmin, Repeatmax
        Link = 61,      // Internal Usage, No Params, Used To Link Together Multiple Events, Does Not Use Any Extra Resources To Iterate Event Lists Needlessly
        GossipSelect = 62,      // Menuid, Actionid
        JustCreated = 63,      // None
        GossipHello = 64,      // None
        FollowCompleted = 65,      // None
        DummyEffect = 66,      // Spellid, Effectindex
        IsBehindTarget = 67,      // Cooldownmin, Cooldownmax
        GameEventStart = 68,      // GameEvent.Entry
        GameEventEnd = 69,      // GameEvent.Entry
        GoStateChanged = 70,      // Go State
        GoEventInform = 71,      // Eventid
        ActionDone = 72,      // Eventid (Shareddefines.Eventid)
        OnSpellclick = 73,      // Clicker (Unit)
        FriendlyHealthPCT = 74,// minHpPct, maxHpPct, repeatMin, repeatMax
        DistanceCreature = 75,      // guid, entry, distance, repeat
        DistanceGameobject = 76,      // guid, entry, distance, repeat
        CounterSet = 77,      // id, value, cooldownMin, cooldownMax
        SceneStart = 78,      // none
        SceneTrigger = 79,      // param_string : triggerName
        SceneCancel = 80,      // none
        SceneComplete = 81,      // none

        //New
        SpellEffectHit = 82,
        SpellEffectHitTarget = 83,

        End = 84
    }

    public enum SmartActions
    {
        None = 0,      // No Action
        Talk = 1,      // Groupid From CreatureText, Duration To Wait Before TextOver Event Is Triggered, useTalkTarget (0/1) - use target as talk target
        SetFaction = 2,      // Factionid (Or 0 For Default)
        MorphToEntryOrModel = 3,      // CreatureTemplate Entry(Param1) Or Modelid (Param2) (Or 0 For Both To Demorph)
        Sound = 4,      // Soundid, Textrange
        PlayEmote = 5,      // Emoteid
        FailQuest = 6,      // Questid
        OfferQuest = 7,      // Questid, directAdd
        SetReactState = 8,      // State
        ActivateGobject = 9,      //
        RandomEmote = 10,     // Emoteid1, Emoteid2, Emoteid3...
        Cast = 11,     // Spellid, Castflags, TriggeredFlags
        SummonCreature = 12,     // Creatureid, Summontype, Duration In Ms, Storageid, Attackinvoker,
        ThreatSinglePct = 13,     // Threat%
        ThreatAllPct = 14,     // Threat%
        CallAreaexploredoreventhappens = 15,     // Questid
        SetIngamePhaseGroup = 16,     // phaseGroupId, apply
        SetEmoteState = 17,     // Emoteid
        SetUnitFlag = 18,     // Flags (May Be More Than One Field Or'D Together), Target
        RemoveUnitFlag = 19,     // Flags (May Be More Than One Field Or'D Together), Target
        AutoAttack = 20,     // Allowattackstate (0 = Stop Attack, Anything Else Means Continue Attacking)
        AllowCombatMovement = 21,     // Allowcombatmovement (0 = Stop Combat Based Movement, Anything Else Continue Attacking)
        SetEventPhase = 22,     // Phase
        IncEventPhase = 23,     // Value (May Be Negative To Decrement Phase, Should Not Be 0)
        Evade = 24,     // No Params
        FleeForAssist = 25,     // With Emote
        CallGroupeventhappens = 26,     // Questid
        CombatStop = 27,     //
        Removeaurasfromspell = 28,     // Spellid, 0 Removes All Auras
        Follow = 29,     // Distance (0 = Default), Angle (0 = Default), Endcreatureentry, Credit, Credittype (0monsterkill, 1event)
        RandomPhase = 30,     // Phaseid1, Phaseid2, Phaseid3...
        RandomPhaseRange = 31,     // Phasemin, Phasemax
        ResetGobject = 32,     //
        CallKilledmonster = 33,     // Creatureid,
        SetInstData = 34,     // Field, Data, Type (0 = SetData, 1 = SetBossState)
        SetInstData64 = 35,     // Field,
        UpdateTemplate = 36,     // Entry
        Die = 37,     // No Params
        SetInCombatWithZone = 38,     // No Params
        CallForHelp = 39,     // Radius, With Emote
        SetSheath = 40,     // Sheath (0-Unarmed, 1-Melee, 2-Ranged)
        ForceDespawn = 41,     // Timer
        SetInvincibilityHpLevel = 42,     // Minhpvalue(+Pct, -Flat)
        MountToEntryOrModel = 43,     // CreatureTemplate Entry(Param1) Or Modelid (Param2) (Or 0 For Both To Dismount)
        SetIngamePhaseId = 44,     // Id
        SetData = 45,     // Field, Data (Only Creature Todo)
        //46 Unused
        SetVisibility = 47,     // On/Off
        SetActive = 48,     // No Params
        AttackStart = 49,     //
        SummonGo = 50,     // Gameobjectid, Despawntime In Ms,
        KillUnit = 51,     //
        ActivateTaxi = 52,     // Taxiid
        WpStart = 53,     // Run/Walk, Pathid, Canrepeat, Quest, Despawntime, Reactstate
        WpPause = 54,     // Time
        WpStop = 55,     // Despawntime, Quest, Fail?
        AddItem = 56,     // Itemid, Count
        RemoveItem = 57,     // Itemid, Count
        InstallAiTemplate = 58,     // Aitemplateid
        SetRun = 59,     // 0/1
        SetFly = 60,     // 0/1
        SetSwim = 61,     // 0/1
        Teleport = 62,     // Mapid,
        SetCounter = 63,   // id, value, reset (0/1)
        StoreTargetList = 64,     // Varid,
        WpResume = 65,     // None
        SetOrientation = 66,     //
        CreateTimedEvent = 67,     // Id, Initialmin, Initialmax, Repeatmin(Only If It Repeats), Repeatmax(Only If It Repeats), Chance
        Playmovie = 68,     // Entry
        MoveToPos = 69,     // PointId, transport, disablePathfinding, ContactDistance
        RespawnTarget = 70,     //
        Equip = 71,     // Entry, Slotmask Slot1, Slot2, Slot3   , Only Slots With Mask Set Will Be Sent To Client, Bits Are 1, 2, 4, Leaving Mask 0 Is Defaulted To Mask 7 (Send All), Slots1-3 Are Only Used If No Entry Is Set
        CloseGossip = 72,     // None
        TriggerTimedEvent = 73,     // Id(>1)
        RemoveTimedEvent = 74,     // Id(>1)
        AddAura = 75,     // Spellid,  Targets
        OverrideScriptBaseObject = 76,     // Warning: Can Crash Core, Do Not Use If You Dont Know What You Are Doing
        ResetScriptBaseObject = 77,     // None
        CallScriptReset = 78,     // None
        SetRangedMovement = 79,     // Distance, Angle
        CallTimedActionlist = 80,     // Id (Overwrites Already Running Actionlist), Stop After Combat?(0/1), Timer Update Type(0-Ooc, 1-Ic, 2-Always)
        SetNpcFlag = 81,     // Flags
        AddNpcFlag = 82,     // Flags
        RemoveNpcFlag = 83,     // Flags
        SimpleTalk = 84,     // Groupid, Can Be Used To Make Players Say Groupid, TextOver Event Is Not Triggered, Whisper Can Not Be Used (Target Units Will Say The Text)
        InvokerCast = 85,     // Spellid, Castflags,   If Avaliable, Last Used Invoker Will Cast Spellid With Castflags On Targets
        CrossCast = 86,     // Spellid, Castflags, Castertargettype, Castertarget Param1, Castertarget Param2, Castertarget Param3, ( + The Origonal Target Fields As Destination Target),   Castertargets Will Cast Spellid On All Targets (Use With Caution If Targeting Multiple * Multiple Units)
        CallRandomTimedActionlist = 87,     // Script9 Ids 1-9
        CallRandomRangeTimedActionlist = 88,     // Script9 Id Min, Max
        RandomMove = 89,     // Maxdist
        SetUnitFieldBytes1 = 90,     // Bytes, Target
        RemoveUnitFieldBytes1 = 91,     // Bytes, Target
        InterruptSpell = 92,
        SendGoCustomAnim = 93,     // Anim Id
        SetDynamicFlag = 94,     // Flags
        AddDynamicFlag = 95,     // Flags
        RemoveDynamicFlag = 96,     // Flags
        JumpToPos = 97,     // Speedxy, Speedz, Targetx, Targety, Targetz
        SendGossipMenu = 98,     // Menuid, optionIndex
        GoSetLootState = 99,     // State
        SendTargetToTarget = 100,    // Id
        SetHomePos = 101,    // None
        SetHealthRegen = 102,    // 0/1
        SetRoot = 103,    // Off/On
        SetGoFlag = 104,    // Flags
        AddGoFlag = 105,    // Flags
        RemoveGoFlag = 106,    // Flags
        SummonCreatureGroup = 107,    // Group, Attackinvoker
        SetPower = 108,    // PowerType, newPower
        AddPower = 109,    // PowerType, newPower
        RemovePower = 110,    // PowerType, newPower
        GameEventStop = 111,    // GameEventId
        GameEventStart = 112,    // GameEventId
        // Not used 
        MoveOffset = 114,
        RandomSound = 115,    // SoundId1, SoundId2, SoundId3, SoundId4, SoundId5, onlySelf
        SetCorpseDelay = 116,    // timer
        DisableEvade = 117,    // 0/1 (1 = disabled, 0 = enabled)
        GoSetGoState = 118,
        // 119 - 127 : 3.3.5 reserved
        PlayAnimkit = 128,
        ScenePlay = 129,    // sceneId
        SceneCancel = 130,    // sceneId

        End = 131
    }

    public enum SmartTargets
    {
        None = 0,    // None, Defaulting To Invoket
        Self = 1,    // Self Cast
        Victim = 2,    // Our Current Target (Ie: Highest Aggro)
        HostileSecondAggro = 3,    // Second Highest Aggro
        HostileLastAggro = 4,    // Dead Last On Aggro
        HostileRandom = 5,    // Just Any Random Target On Our Threat List
        HostileRandomNotTop = 6,    // Any Random Target Except Top Threat
        ActionInvoker = 7,    // Unit Who Caused This Event To Occur
        Position = 8,    // Use Xyz From Event Params
        CreatureRange = 9,    // Creatureentry(0any), Mindist, Maxdist
        CreatureGuid = 10,   // Guid, Entry
        CreatureDistance = 11,   // Creatureentry(0any), Maxdist
        Stored = 12,   // Id, Uses Pre-Stored Target(List)
        GameobjectRange = 13,   // Entry(0any), Min, Max
        GameobjectGuid = 14,   // Guid, Entry
        GameobjectDistance = 15,   // Entry(0any), Maxdist
        InvokerParty = 16,   // Invoker'S Party Members
        PlayerRange = 17,   // Min, Max
        PlayerDistance = 18,   // Maxdist
        ClosestCreature = 19,   // Creatureentry(0any), Maxdist, Dead?
        ClosestGameobject = 20,   // Entry(0any), Maxdist
        ClosestPlayer = 21,   // Maxdist
        ActionInvokerVehicle = 22,   // Unit'S Vehicle Who Caused This Event To Occur
        OwnerOrSummoner = 23,   // Unit'S Owner Or Summoner
        ThreatList = 24,   // All Units On Creature'S Threat List
        ClosestEnemy = 25,   // maxDist, playerOnly
        ClosestFriendly = 26,   // maxDist, playerOnly
        LootRecipients = 27,   // all players that have tagged this creature (for kill credit)
        Farthest = 28,   // maxDist, playerOnly, isInLos
        VehicleAccessory = 29,   // seat number (vehicle can target it's own accessory)
        SpellTarget = 30,

        End = 31
    }
}
