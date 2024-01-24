// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire
{
    struct DataTypes
    {
        public const uint HighlordOmokk = 0;
        public const uint ShadowHunterVoshgajin = 1;
        public const uint WarmasterVoone = 2;
        public const uint MotherSmolderweb = 3;
        public const uint UrokDoomhowl = 4;
        public const uint QuartermasterZigris = 5;
        public const uint GizrulTheSlavener = 6;
        public const uint Halycon = 7;
        public const uint OverlordWyrmthalak = 8;
        public const uint PyrogaurdEmberseer = 9;
        public const uint WarchiefRendBlackhand = 10;
        public const uint Gyth = 11;
        public const uint TheBeast = 12;
        public const uint GeneralDrakkisath = 13;
        public const uint LordValthalak = 14;
        // Extra
        public const uint DragonspireRoom = 15;
        public const uint HallRune1 = 16;
        public const uint HallRune2 = 17;
        public const uint HallRune3 = 18;
        public const uint HallRune4 = 19;
        public const uint HallRune5 = 20;
        public const uint HallRune6 = 21;
        public const uint HallRune7 = 22;
        public const uint ScarshieldInfiltrator = 23;
        public const uint BlackhandIncarcerator = 24;
    }

    struct CreaturesIds
    {
        public const uint HighlordOmokk = 9196;
        public const uint ShadowHunterVoshgajin = 9236;
        public const uint WarmasterVoone = 9237;
        public const uint MotherSmolderweb = 10596;
        public const uint UrokDoomhowl = 10584;
        public const uint QuartermasterZigris = 9736;
        public const uint GizrulTheSlavener = 10268;
        public const uint Halycon = 10220;
        public const uint OverlordWyrmthalak = 9568;
        public const uint PyrogaurdEmberseer = 9816;
        public const uint WarchiefRendBlackhand = 10429;
        public const uint Gyth = 10339;
        public const uint TheBeast = 10430;
        public const uint GeneralDrakkisath = 10363;
        public const uint BlackhandDreadweaver = 9817;
        public const uint BlackhandSummoner = 9818;
        public const uint BlackhandVeteran = 9819;
        public const uint BlackhandIncarcerator = 10316;
        public const uint LordVictorNefarius = 10162;
        public const uint ScarshieldInfiltrator = 10299;
    }

    struct GameObjectsIds
    {
        public const uint WhelpSpawner = 175622; // trap spawned by public const uint  id 175124
                                                 // Doors
        public const uint EmberseerIn = 175244; // First door to Pyroguard Emberseer
        public const uint Doors = 175705; // Second door to Pyroguard Emberseer
        public const uint EmberseerOut = 175153; // Door after Pyroguard Emberseer event
        public const uint GythEntryDoor = 164726;
        public const uint GythCombatDoor = 175185;
        public const uint GythExitDoor = 175186;
        public const uint DrakkisathDoor1 = 175946;
        public const uint DrakkisathDoor2 = 175947;
        // Runes in drapublic const uint nspire hall
        public const uint HallRune1 = 175197;
        public const uint HallRune2 = 175199;
        public const uint HallRune3 = 175195;
        public const uint HallRune4 = 175200;
        public const uint HallRune5 = 175198;
        public const uint HallRune6 = 175196;
        public const uint HallRune7 = 175194;
        // Runes in emberseers room
        public const uint EmberseerRune1 = 175266;
        public const uint EmberseerRune2 = 175267;
        public const uint EmberseerRune3 = 175268;
        public const uint EmberseerRune4 = 175269;
        public const uint EmberseerRune5 = 175270;
        public const uint EmberseerRune6 = 175271;
        public const uint EmberseerRune7 = 175272;
        // For Gyth event
        public const uint DrPortcullis = 175185;
        public const uint PortcullisActive = 164726;
        public const uint PortcullisTobossrooms = 175186;
    }

    struct BRSMiscConst
    {
        public const uint SpellSummonRookeryWhelp = 15745;
        public const uint EventUrokDoomhowl = 4845;
        public const uint EventPyroguardEmberseer = 4884;
        public const uint Areatrigger = 1;
        public const uint AreatriggerDragonspireHall = 2046;
        public const uint AreatriggerBlackrockStadium = 2026;

        public const uint EncounterCount = 23;

        //uint const DragonspireRunes[7] = { GoHallRune1, GoHallRune2, GoHallRune3, GoHallRune4, GoHallRune5, GoHallRune6, GoHallRune7 }

        public static uint[] DragonspireMobs = { CreaturesIds.BlackhandDreadweaver, CreaturesIds.BlackhandSummoner, CreaturesIds.BlackhandVeteran };

        public static DoorData[] doorData =
        {
            new DoorData(GameObjectsIds.Doors, DataTypes.PyrogaurdEmberseer, DoorType.Passage),
            new DoorData(GameObjectsIds.EmberseerOut, DataTypes.PyrogaurdEmberseer, DoorType.Passage),
            new DoorData(GameObjectsIds.DrakkisathDoor1, DataTypes.GeneralDrakkisath, DoorType.Passage),
            new DoorData(GameObjectsIds.DrakkisathDoor2, DataTypes.GeneralDrakkisath, DoorType.Passage),
            new DoorData(GameObjectsIds.PortcullisActive, DataTypes.WarchiefRendBlackhand, DoorType.Passage),
            new DoorData(GameObjectsIds.PortcullisTobossrooms, DataTypes.WarchiefRendBlackhand, DoorType.Passage),
        };
    }

    struct EventIds
    {
        public const uint DargonspireRoomStore = 1;
        public const uint DargonspireRoomCheck = 2;
        public const uint UrokDoomhowlSpawns1 = 3;
        public const uint UrokDoomhowlSpawns2 = 4;
        public const uint UrokDoomhowlSpawns3 = 5;
        public const uint UrokDoomhowlSpawns4 = 6;
        public const uint UrokDoomhowlSpawns5 = 7;
        public const uint UrokDoomhowlSpawnIn = 8;
    }

    [Script]
    class instance_blackrock_spire : InstanceMapScript
    {
        public instance_blackrock_spire() : base(nameof(instance_blackrock_spire), 229) { }

        class instance_blackrock_spireMapScript : InstanceScript
        {
            public instance_blackrock_spireMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("BRSv1");
                SetBossNumber(BRSMiscConst.EncounterCount);
                LoadDoorData(BRSMiscConst.doorData);

                for (byte i = 0; i < 7; ++i)
                    runecreaturelist[i] = new();
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case CreaturesIds.HighlordOmokk:
                        HighlordOmokk = creature.GetGUID();
                        break;
                    case CreaturesIds.ShadowHunterVoshgajin:
                        ShadowHunterVoshgajin = creature.GetGUID();
                        break;
                    case CreaturesIds.WarmasterVoone:
                        WarMasterVoone = creature.GetGUID();
                        break;
                    case CreaturesIds.MotherSmolderweb:
                        MotherSmolderweb = creature.GetGUID();
                        break;
                    case CreaturesIds.UrokDoomhowl:
                        UrokDoomhowl = creature.GetGUID();
                        break;
                    case CreaturesIds.QuartermasterZigris:
                        QuartermasterZigris = creature.GetGUID();
                        break;
                    case CreaturesIds.GizrulTheSlavener:
                        GizrultheSlavener = creature.GetGUID();
                        break;
                    case CreaturesIds.Halycon:
                        Halycon = creature.GetGUID();
                        break;
                    case CreaturesIds.OverlordWyrmthalak:
                        OverlordWyrmthalak = creature.GetGUID();
                        break;
                    case CreaturesIds.PyrogaurdEmberseer:
                        PyroguardEmberseer = creature.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            creature.DespawnOrUnsummon(TimeSpan.FromSeconds(0), TimeSpan.FromDays(7));
                        break;
                    case CreaturesIds.WarchiefRendBlackhand:
                        WarchiefRendBlackhand = creature.GetGUID();
                        if (GetBossState(DataTypes.Gyth) == EncounterState.Done)
                            creature.DespawnOrUnsummon(TimeSpan.FromSeconds(0), TimeSpan.FromDays(7));
                        break;
                    case CreaturesIds.Gyth:
                        Gyth = creature.GetGUID();
                        break;
                    case CreaturesIds.TheBeast:
                        TheBeast = creature.GetGUID();
                        break;
                    case CreaturesIds.GeneralDrakkisath:
                        GeneralDrakkisath = creature.GetGUID();
                        break;
                    case CreaturesIds.LordVictorNefarius:
                        LordVictorNefarius = creature.GetGUID();
                        if (GetBossState(DataTypes.Gyth) == EncounterState.Done)
                            creature.DespawnOrUnsummon(TimeSpan.FromSeconds(0), TimeSpan.FromDays(7));
                        break;
                    case CreaturesIds.ScarshieldInfiltrator:
                        ScarshieldInfiltrator = creature.GetGUID();
                        break;
                    case CreaturesIds.BlackhandIncarcerator:
                        _incarceratorList.Add(creature.GetGUID());
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                base.OnGameObjectCreate(go);

                switch (go.GetEntry())
                {
                    case GameObjectsIds.WhelpSpawner:
                        go.CastSpell(null, BRSMiscConst.SpellSummonRookeryWhelp);
                        break;
                    case GameObjectsIds.EmberseerIn:
                        go_emberseerin = go.GetGUID();
                        if (GetBossState(DataTypes.DragonspireRoom) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, true, go);
                        break;
                    case GameObjectsIds.Doors:
                        go_doors = go.GetGUID();
                        if (GetBossState(DataTypes.DragonspireRoom) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, true, go);
                        break;
                    case GameObjectsIds.EmberseerOut:
                        go_emberseerout = go.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, true, go);
                        break;
                    case GameObjectsIds.HallRune1:
                        go_roomrunes[0] = go.GetGUID();
                        if (GetBossState(DataTypes.HallRune1) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.HallRune2:
                        go_roomrunes[1] = go.GetGUID();
                        if (GetBossState(DataTypes.HallRune2) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.HallRune3:
                        go_roomrunes[2] = go.GetGUID();
                        if (GetBossState(DataTypes.HallRune3) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.HallRune4:
                        go_roomrunes[3] = go.GetGUID();
                        if (GetBossState(DataTypes.HallRune4) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.HallRune5:
                        go_roomrunes[4] = go.GetGUID();
                        if (GetBossState(DataTypes.HallRune5) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.HallRune6:
                        go_roomrunes[5] = go.GetGUID();
                        if (GetBossState(DataTypes.HallRune6) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.HallRune7:
                        go_roomrunes[6] = go.GetGUID();
                        if (GetBossState(DataTypes.HallRune7) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.EmberseerRune1:
                        go_emberseerrunes[0] = go.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.EmberseerRune2:
                        go_emberseerrunes[1] = go.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.EmberseerRune3:
                        go_emberseerrunes[2] = go.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.EmberseerRune4:
                        go_emberseerrunes[3] = go.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.EmberseerRune5:
                        go_emberseerrunes[4] = go.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.EmberseerRune6:
                        go_emberseerrunes[5] = go.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.EmberseerRune7:
                        go_emberseerrunes[6] = go.GetGUID();
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, false, go);
                        break;
                    case GameObjectsIds.PortcullisActive:
                        go_portcullis_active = go.GetGUID();
                        if (GetBossState(DataTypes.Gyth) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, true, go);
                        break;
                    case GameObjectsIds.PortcullisTobossrooms:
                        go_portcullis_tobossrooms = go.GetGUID();
                        if (GetBossState(DataTypes.Gyth) == EncounterState.Done)
                            HandleGameObject(ObjectGuid.Empty, true, go);
                        break;
                    default:
                        break;
                }
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case DataTypes.HighlordOmokk:
                    case DataTypes.ShadowHunterVoshgajin:
                    case DataTypes.WarmasterVoone:
                    case DataTypes.MotherSmolderweb:
                    case DataTypes.UrokDoomhowl:
                    case DataTypes.QuartermasterZigris:
                    case DataTypes.GizrulTheSlavener:
                    case DataTypes.Halycon:
                    case DataTypes.OverlordWyrmthalak:
                    case DataTypes.PyrogaurdEmberseer:
                    case DataTypes.WarchiefRendBlackhand:
                    case DataTypes.Gyth:
                    case DataTypes.TheBeast:
                    case DataTypes.GeneralDrakkisath:
                    case DataTypes.DragonspireRoom:
                        break;
                    default:
                        break;
                }

                return true;
            }

            public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker)
            {
                switch (eventId)
                {
                    case BRSMiscConst.EventPyroguardEmberseer:
                        if (GetBossState(DataTypes.PyrogaurdEmberseer) == EncounterState.NotStarted)
                        {
                            Creature Emberseer = instance.GetCreature(PyroguardEmberseer);
                            if (Emberseer != null)
                                Emberseer.GetAI().SetData(1, 1);
                        }
                        break;
                    case BRSMiscConst.EventUrokDoomhowl:
                        if (GetBossState(CreaturesIds.UrokDoomhowl) == EncounterState.NotStarted)
                        {

                        }
                        break;
                    default:
                        break;
                }
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case BRSMiscConst.Areatrigger:
                        if (data == BRSMiscConst.AreatriggerDragonspireHall)
                        {
                            if (GetBossState(DataTypes.DragonspireRoom) != EncounterState.Done)
                                _events.ScheduleEvent(EventIds.DargonspireRoomStore, TimeSpan.FromSeconds(1));
                        }
                        break;
                    case DataTypes.BlackhandIncarcerator:
                        foreach (var itr in _incarceratorList)
                        {
                            Creature creature = instance.GetCreature(itr);
                            if (creature != null)
                                creature.Respawn();
                        }
                        break;
                    default:
                        break;
                }
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case DataTypes.HighlordOmokk:
                        return HighlordOmokk;
                    case DataTypes.ShadowHunterVoshgajin:
                        return ShadowHunterVoshgajin;
                    case DataTypes.WarmasterVoone:
                        return WarMasterVoone;
                    case DataTypes.MotherSmolderweb:
                        return MotherSmolderweb;
                    case DataTypes.UrokDoomhowl:
                        return UrokDoomhowl;
                    case DataTypes.QuartermasterZigris:
                        return QuartermasterZigris;
                    case DataTypes.GizrulTheSlavener:
                        return GizrultheSlavener;
                    case DataTypes.Halycon:
                        return Halycon;
                    case DataTypes.OverlordWyrmthalak:
                        return OverlordWyrmthalak;
                    case DataTypes.PyrogaurdEmberseer:
                        return PyroguardEmberseer;
                    case DataTypes.WarchiefRendBlackhand:
                        return WarchiefRendBlackhand;
                    case DataTypes.Gyth:
                        return Gyth;
                    case DataTypes.TheBeast:
                        return TheBeast;
                    case DataTypes.GeneralDrakkisath:
                        return GeneralDrakkisath;
                    case DataTypes.ScarshieldInfiltrator:
                        return ScarshieldInfiltrator;
                    case GameObjectsIds.EmberseerIn:
                        return go_emberseerin;
                    case GameObjectsIds.Doors:
                        return go_doors;
                    case GameObjectsIds.EmberseerOut:
                        return go_emberseerout;
                    case GameObjectsIds.HallRune1:
                        return go_roomrunes[0];
                    case GameObjectsIds.HallRune2:
                        return go_roomrunes[1];
                    case GameObjectsIds.HallRune3:
                        return go_roomrunes[2];
                    case GameObjectsIds.HallRune4:
                        return go_roomrunes[3];
                    case GameObjectsIds.HallRune5:
                        return go_roomrunes[4];
                    case GameObjectsIds.HallRune6:
                        return go_roomrunes[5];
                    case GameObjectsIds.HallRune7:
                        return go_roomrunes[6];
                    case GameObjectsIds.EmberseerRune1:
                        return go_emberseerrunes[0];
                    case GameObjectsIds.EmberseerRune2:
                        return go_emberseerrunes[1];
                    case GameObjectsIds.EmberseerRune3:
                        return go_emberseerrunes[2];
                    case GameObjectsIds.EmberseerRune4:
                        return go_emberseerrunes[3];
                    case GameObjectsIds.EmberseerRune5:
                        return go_emberseerrunes[4];
                    case GameObjectsIds.EmberseerRune6:
                        return go_emberseerrunes[5];
                    case GameObjectsIds.EmberseerRune7:
                        return go_emberseerrunes[6];
                    case GameObjectsIds.PortcullisActive:
                        return go_portcullis_active;
                    case GameObjectsIds.PortcullisTobossrooms:
                        return go_portcullis_tobossrooms;
                    default:
                        break;
                }
                return ObjectGuid.Empty;
            }

            public override void Update(uint diff)
            {
                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case EventIds.DargonspireRoomStore:
                            Dragonspireroomstore();
                            _events.ScheduleEvent(EventIds.DargonspireRoomCheck, TimeSpan.FromSeconds(3));
                            break;
                        case EventIds.DargonspireRoomCheck:
                            Dragonspireroomcheck();
                            if (GetBossState(DataTypes.DragonspireRoom) != EncounterState.Done)
                                _events.ScheduleEvent(EventIds.DargonspireRoomCheck, TimeSpan.FromSeconds(3));
                            break;
                        default:
                            break;
                    }
                });
            }

            void Dragonspireroomstore()
            {
                for (byte i = 0; i < 7; ++i)
                {
                    // Refresh the creature list
                    runecreaturelist[i].Clear();

                    GameObject rune = instance.GetGameObject(go_roomrunes[i]);
                    if (rune != null)
                    {
                        for (byte j = 0; j < 3; ++j)
                        {
                            List<Creature> creatureList = rune.GetCreatureListWithEntryInGrid(BRSMiscConst.DragonspireMobs[j], 15.0f);
                            foreach (var creature in creatureList)
                                if (creature != null)
                                    runecreaturelist[i].Add(creature.GetGUID());
                        }
                    }
                }
            }

            void Dragonspireroomcheck()
            {
                Creature mob = null;
                GameObject rune = null;

                for (byte i = 0; i < 7; ++i)
                {
                    bool _mobAlive = false;
                    rune = instance.GetGameObject(go_roomrunes[i]);
                    if (rune == null)
                        continue;

                    if (rune.GetGoState() == GameObjectState.Active)
                    {
                        foreach (ObjectGuid guid in runecreaturelist[i])
                        {
                            mob = instance.GetCreature(guid);
                            if (mob != null && mob.IsAlive())
                                _mobAlive = true;
                        }
                    }

                    if (!_mobAlive && rune.GetGoState() == GameObjectState.Active)
                    {
                        HandleGameObject(ObjectGuid.Empty, false, rune);

                        switch (rune.GetEntry())
                        {
                            case GameObjectsIds.HallRune1:
                                SetBossState(DataTypes.HallRune1, EncounterState.Done);
                                break;
                            case GameObjectsIds.HallRune2:
                                SetBossState(DataTypes.HallRune2, EncounterState.Done);
                                break;
                            case GameObjectsIds.HallRune3:
                                SetBossState(DataTypes.HallRune3, EncounterState.Done);
                                break;
                            case GameObjectsIds.HallRune4:
                                SetBossState(DataTypes.HallRune4, EncounterState.Done);
                                break;
                            case GameObjectsIds.HallRune5:
                                SetBossState(DataTypes.HallRune5, EncounterState.Done);
                                break;
                            case GameObjectsIds.HallRune6:
                                SetBossState(DataTypes.HallRune6, EncounterState.Done);
                                break;
                            case GameObjectsIds.HallRune7:
                                SetBossState(DataTypes.HallRune7, EncounterState.Done);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (GetBossState(DataTypes.HallRune1) == EncounterState.Done && GetBossState(DataTypes.HallRune2) == EncounterState.Done && GetBossState(DataTypes.HallRune3) == EncounterState.Done &&
                    GetBossState(DataTypes.HallRune4) == EncounterState.Done && GetBossState(DataTypes.HallRune5) == EncounterState.Done && GetBossState(DataTypes.HallRune6) == EncounterState.Done &&
                    GetBossState(DataTypes.HallRune7) == EncounterState.Done)
                {
                    SetBossState(DataTypes.DragonspireRoom, EncounterState.Done);
                    GameObject door1 = instance.GetGameObject(go_emberseerin);
                    if (door1 != null)
                        HandleGameObject(ObjectGuid.Empty, true, door1);
                    GameObject door2 = instance.GetGameObject(go_doors);
                    if (door2 != null)
                        HandleGameObject(ObjectGuid.Empty, true, door2);
                }
            }

            ObjectGuid HighlordOmokk;
            ObjectGuid ShadowHunterVoshgajin;
            ObjectGuid WarMasterVoone;
            ObjectGuid MotherSmolderweb;
            ObjectGuid UrokDoomhowl;
            ObjectGuid QuartermasterZigris;
            ObjectGuid GizrultheSlavener;
            ObjectGuid Halycon;
            ObjectGuid OverlordWyrmthalak;
            ObjectGuid PyroguardEmberseer;
            ObjectGuid WarchiefRendBlackhand;
            ObjectGuid Gyth;
            ObjectGuid LordVictorNefarius;
            ObjectGuid TheBeast;
            ObjectGuid GeneralDrakkisath;
            ObjectGuid ScarshieldInfiltrator;
            ObjectGuid go_emberseerin;
            ObjectGuid go_doors;
            ObjectGuid go_emberseerout;
            ObjectGuid go_blackrockaltar;
            ObjectGuid[] go_roomrunes = new ObjectGuid[7];
            ObjectGuid[] go_emberseerrunes = new ObjectGuid[7];
            List<ObjectGuid>[] runecreaturelist = new List<ObjectGuid>[7];
            ObjectGuid go_portcullis_active;
            ObjectGuid go_portcullis_tobossrooms;
            List<ObjectGuid> _incarceratorList = new();
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_blackrock_spireMapScript(map);
        }
    }

    [Script]
    class at_dragonspire_hall : AreaTriggerScript
    {
        public at_dragonspire_hall() : base("at_dragonspire_hall") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            if (player != null && player.IsAlive())
            {
                InstanceScript instance = player.GetInstanceScript();
                if (instance != null)
                {
                    instance.SetData(BRSMiscConst.Areatrigger, BRSMiscConst.AreatriggerDragonspireHall);
                    return true;
                }
            }

            return false;
        }
    }

    [Script]
    class at_blackrock_stadium : AreaTriggerScript
    {
        public at_blackrock_stadium() : base("at_blackrock_stadium") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            if (player != null && player.IsAlive())
            {
                InstanceScript instance = player.GetInstanceScript();
                if (instance == null)
                    return false;

                Creature rend = player.FindNearestCreature(CreaturesIds.WarchiefRendBlackhand, 50.0f);
                if (rend != null)
                {
                    rend.GetAI().SetData(BRSMiscConst.Areatrigger, BRSMiscConst.AreatriggerBlackrockStadium);
                    return true;
                }
            }

            return false;
        }
    }

    [Script]
    class at_nearby_scarshield_infiltrator : AreaTriggerScript
    {
        public at_nearby_scarshield_infiltrator() : base("at_nearby_scarshield_infiltrator") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord at)
        {
            if (player.IsAlive())
            {
                InstanceScript instance = player.GetInstanceScript();
                if (instance != null)
                {
                    Creature infiltrator = ObjectAccessor.GetCreature(player, instance.GetGuidData(DataTypes.ScarshieldInfiltrator));
                    if (infiltrator != null)
                    {
                        if (player.GetLevel() >= 57)
                            infiltrator.GetAI().SetData(1, 1);
                        else if (infiltrator.GetEntry() == CreaturesIds.ScarshieldInfiltrator)
                            infiltrator.GetAI().Talk(0, player);

                        return true;
                    }
                }
            }

            return false;
        }
    }
}

