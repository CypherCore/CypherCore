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


namespace Scripts.Northrend.Nexus.Oculus
{
    /*
    struct DataTypes
    {
        // Encounter States/Boss GUIDs
        public const uint Drakos = 0;
        public const uint Varos = 1;
        public const uint Urom = 2;
        public const uint Eregos = 3;
        // GPS System
        public const uint Constructs = 4;
    }

    class instance_oculus : InstanceMapScript
    {
        public instance_oculus() : base(nameof(instance_oculus), 578) { }

        class instance_oculus_InstanceMapScript : InstanceScript
        {
            public instance_oculus_InstanceMapScript(Map map) : base(map)
            {
                SetHeaders("OC");
                SetBossNumber(DataTypes.Eregos + 1);
                LoadDoorData(doorData);

                CentrifugueConstructCounter = 0;
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case NPC_DRAKOS:
                        DrakosGUID = creature.GetGUID();
                        break;
                    case NPC_VAROS:
                        VarosGUID = creature.GetGUID();
                        if (GetBossState(DATA_DRAKOS) == EncounterState.Done)
                            creature.SetPhaseMask(1, true);
                        break;
                    case NPC_UROM:
                        UromGUID = creature.GetGUID();
                        if (GetBossState(DATA_VAROS) == EncounterState.Done)
                            creature.SetPhaseMask(1, true);
                        break;
                    case NPC_EREGOS:
                        EregosGUID = creature.GetGUID();
                        if (GetBossState(DATA_UROM) == EncounterState.Done)
                            creature.SetPhaseMask(1, true);
                        break;
                    case NPC_CENTRIFUGE_CONSTRUCT:
                        if (creature.IsAlive())
                            DoUpdateWorldState(WORLD_STATE_CENTRIFUGE_CONSTRUCT_AMOUNT, ++CentrifugueConstructCounter);
                        break;
                    case NPC_BELGARISTRASZ:
                        BelgaristraszGUID = creature.GetGUID();
                        if (GetBossState(DATA_DRAKOS) == EncounterState.Done)
                        {
                            creature.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                            creature.Relocate(BelgaristraszMove);
                        }
                        break;
                    case NPC_ETERNOS:
                        EternosGUID = creature.GetGUID();
                        if (GetBossState(DATA_DRAKOS) == EncounterState.Done)
                        {
                            creature.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                            creature.Relocate(EternosMove);
                        }
                        break;
                    case NPC_VERDISA:
                        VerdisaGUID = creature.GetGUID();
                        if (GetBossState(DATA_DRAKOS) == EncounterState.Done)
                        {
                            creature.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                            creature.Relocate(VerdisaMove);
                        }
                        break;
                    case NPC_GREATER_WHELP:
                        if (GetBossState(DATA_UROM) == EncounterState.Done)
                        {
                            creature.SetPhaseMask(1, true);
                            GreaterWhelpList.Add(creature.GetGUID());
                        }
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GO_DRAGON_CAGE_DOOR:
                        AddDoor(go, true);
                        break;
                    case GO_EREGOS_CACHE_N:
                    case GO_EREGOS_CACHE_H:
                        EregosCacheGUID = go.GetGUID();
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectRemove(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GO_DRAGON_CAGE_DOOR:
                        AddDoor(go, false);
                        break;
                    default:
                        break;
                }
            }

            public override void OnUnitDeath(Unit unit)
            {
                Creature creature = unit.ToCreature();
                if (!creature)
                    return;

                if (creature.GetEntry() == NPC_CENTRIFUGE_CONSTRUCT)
                {
                    DoUpdateWorldState(WORLD_STATE_CENTRIFUGE_CONSTRUCT_AMOUNT, --CentrifugueConstructCounter);

                    if (CentrifugueConstructCounter == 0)
                    {
                        Creature varos = instance.GetCreature(VarosGUID);
                        if (varos)
                            varos.RemoveAllAuras();
                    }
                }
            }

            public override void FillInitialWorldStates(InitWorldStates packet)
            {
                if (GetBossState(DATA_DRAKOS) == EncounterState.Done && GetBossState(DATA_VAROS) != EncounterState.Done)
                {
                    packet.Worldstates.Add(WORLD_STATE_CENTRIFUGE_CONSTRUCT_SHOW, 1);
                    packet.Worldstates.Add(WORLD_STATE_CENTRIFUGE_CONSTRUCT_AMOUNT, CentrifugueConstructCounter);
                }
                else
                {
                    packet.Worldstates.Add(WORLD_STATE_CENTRIFUGE_CONSTRUCT_SHOW, 0);
                    packet.Worldstates.Add(WORLD_STATE_CENTRIFUGE_CONSTRUCT_AMOUNT, 0);
                }
            }

            public override void ProcessEvent(WorldObject Unit, uint eventId)
            {
                if (eventId != EVENT_CALL_DRAGON)
                    return;

                Creature varos = instance.GetCreature(VarosGUID);
                if (varos)
                {
                    Creature drake = varos.SummonCreature(NPC_AZURE_RING_GUARDIAN, varos.GetPositionX(), varos.GetPositionY(), varos.GetPositionZ() + 40);
                    if (drake)
                        drake.GetAI().DoAction(ACTION_CALL_DRAGON_EVENT);
                }
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case DATA_DRAKOS:
                        if (state == EncounterState.Done)
                        {
                            DoUpdateWorldState(WORLD_STATE_CENTRIFUGE_CONSTRUCT_SHOW, 1);
                            DoUpdateWorldState(WORLD_STATE_CENTRIFUGE_CONSTRUCT_AMOUNT, CentrifugueConstructCounter);
                            FreeDragons();
                            Creature varos = instance.GetCreature(VarosGUID);
                            if (varos)
                                varos.SetPhaseMask(1, true);
                            events.ScheduleEvent(EVENT_VAROS_INTRO, 15000);
                        }
                        break;
                    case DATA_VAROS:
                        if (state == EncounterState.Done)
                        {
                            DoUpdateWorldState(WORLD_STATE_CENTRIFUGE_CONSTRUCT_SHOW, 0);
                            Creature urom = instance.GetCreature(UromGUID);
                            if (urom)
                                urom.SetPhaseMask(1, true);
                        }
                        break;
                    case DATA_UROM:
                        if (state == EncounterState.Done)
                        {
                            Creature eregos = instance.GetCreature(EregosGUID);
                            if (eregos)
                            {
                                eregos.SetPhaseMask(1, true);
                                GreaterWhelps();
                                events.ScheduleEvent(EVENT_EREGOS_INTRO, 5000);
                            }
                        }
                        break;
                    case DATA_EREGOS:
                        if (state == EncounterState.Done)
                        {
                            GameObject cache = instance.GetGameObject(EregosCacheGUID);
                            if (cache)
                            {
                                cache.SetRespawnTime((int)cache.GetRespawnDelay());
                                cache.RemoveFlag(GAMEOBJECT_FLAGS, GO_FLAG_NOT_SELECTABLE);
                            }
                        }
                        break;
                }

                return true;
            }

            public override uint GetData(uint type)
            {
                if (type == DATA_CONSTRUCTS)
                {
                    if (CentrifugueConstructCounter == 0)
                        return KILL_NO_CONSTRUCT;
                    else if (CentrifugueConstructCounter == 1)
                        return KILL_ONE_CONSTRUCT;
                    else if (CentrifugueConstructCounter > 1)
                        return KILL_MORE_CONSTRUCT;
                }

                return KILL_NO_CONSTRUCT;
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case DATA_DRAKOS:
                        return DrakosGUID;
                    case DATA_VAROS:
                        return VarosGUID;
                    case DATA_UROM:
                        return UromGUID;
                    case DATA_EREGOS:
                        return EregosGUID;
                    default:
                        break;
                }

                return ObjectGuid.Empty;
            }

            void FreeDragons()
            {
                Creature belgaristrasz = instance.GetCreature(BelgaristraszGUID);
                if (belgaristrasz)
                {
                    belgaristrasz.SetWalk(true);
                    belgaristrasz.GetMotionMaster().MovePoint(POINT_MOVE_OUT, BelgaristraszMove);
                }

                Creature eternos = instance.GetCreature(EternosGUID);
                if (eternos)
                {
                    eternos.SetWalk(true);
                    eternos.GetMotionMaster().MovePoint(POINT_MOVE_OUT, EternosMove);
                }

                Creature verdisa = instance.GetCreature(VerdisaGUID);
                if (verdisa)
                {
                    verdisa.SetWalk(true);
                    verdisa.GetMotionMaster().MovePoint(POINT_MOVE_OUT, VerdisaMove);
                }
            }

            public override void Update(uint diff)
            {
                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case EVENT_VAROS_INTRO:
                            Creature varos = instance.GetCreature(VarosGUID);
                            if (varos)
                                varos.GetAI().Talk(SAY_VAROS_INTRO_TEXT);
                            break;
                        case EVENT_EREGOS_INTRO:
                            Creature eregos = instance.GetCreature(EregosGUID);
                            if (eregos)
                                eregos.GetAI().Talk(SAY_EREGOS_INTRO_TEXT);
                            break;
                        default:
                            break;
                    }
                });
            }

            void GreaterWhelps()
            {
                foreach (ObjectGuid guid in GreaterWhelpList)
                {
                    Creature gwhelp = instance.GetCreature(guid);
                    if (gwhelp)
                        gwhelp.SetPhaseMask(1, true);
                }
            }

            ObjectGuid DrakosGUID;
            ObjectGuid VarosGUID;
            ObjectGuid UromGUID;
            ObjectGuid EregosGUID;

            ObjectGuid BelgaristraszGUID;
            ObjectGuid EternosGUID;
            ObjectGuid VerdisaGUID;

            byte CentrifugueConstructCounter;

            ObjectGuid EregosCacheGUID;

            List<ObjectGuid> GreaterWhelpList = new List<ObjectGuid>();

            EventMap events = new EventMap();
        }

        */
}
