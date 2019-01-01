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


namespace Scripts.EasternKingdoms.Deadmines
{
    /*class instance_deadmines : InstanceMapScript
    {
        public instance_deadmines() : base("instance_deadmines", 36)
        {
        }

        class instance_deadmines_InstanceMapScript : InstanceScript
        {
            public instance_deadmines_InstanceMapScript(Map map) : base(map)
            {
                SetHeaders(DataHeader);

                State = CANNON_NOT_USED;
                CannonBlast_Timer = 0;
                PiratesDelay_Timer = 0;
            }

            public override void Initialize()
            {

            }

            public override void Update(uint diff)
            {
                if (IronCladDoorGUID.IsEmpty() || DefiasCannonGUID.IsEmpty() || DoorLeverGUID.IsEmpty())
                    return;

                GameObject pIronCladDoor = instance.GetGameObject(IronCladDoorGUID);
                if (!pIronCladDoor)
                    return;

                switch (State)
                {
                    case CANNON_GUNPOWDER_USED:
                        CannonBlast_Timer = DATA_CANNON_BLAST_TIMER;
                        // it's a hack - Mr. Smite should do that but his too far away
                        //pIronCladDoor.SetName("Mr. Smite");
                        //pIronCladDoor.MonsterYell(SAY_MR_SMITE_ALARM1, LANG_UNIVERSAL, NULL);
                        pIronCladDoor.PlayDirectSound(SOUND_MR_SMITE_ALARM1);
                        State = CANNON_BLAST_INITIATED;
                        break;
                    case CANNON_BLAST_INITIATED:
                        PiratesDelay_Timer = DATA_PIRATES_DELAY_TIMER;
                        if (CannonBlast_Timer <= diff)
                        {
                            SummonCreatures();
                            GameObject pDefiasCannon = instance.GetGameObject(DefiasCannonGUID);
                            if (pDefiasCannon)
                            {
                                pDefiasCannon.SetGoState(GO_STATE_ACTIVE);
                                pDefiasCannon.PlayDirectSound(SOUND_CANNONFIRE);
                            }
                            pIronCladDoor.SetGoState(GO_STATE_ACTIVE_ALTERNATIVE);
                            pIronCladDoor.PlayDirectSound(SOUND_DESTROYDOOR);
                            GameObject pDoorLever = instance.GetGameObject(DoorLeverGUID);
                            if (pDoorLever)
                                pDoorLever.SetUInt32Value(GAMEOBJECT_FLAGS, 4);
                            //pIronCladDoor.MonsterYell(SAY_MR_SMITE_ALARM2, LANG_UNIVERSAL, NULL);
                            pIronCladDoor.PlayDirectSound(SOUND_MR_SMITE_ALARM2);
                            State = PIRATES_ATTACK;
                        }
                        else CannonBlast_Timer -= diff;
                        break;
                    case PIRATES_ATTACK:
                        if (PiratesDelay_Timer <= diff)
                        {
                            MoveCreaturesInside();
                            State = EVENT_DONE;
                        }
                        else PiratesDelay_Timer -= diff;
                        break;
                }
            }

            void SummonCreatures()
            {
                GameObject pIronCladDoor = instance.GetGameObject(IronCladDoorGUID);
                if (pIronCladDoor)
                {
                    Creature DefiasPirate1 = pIronCladDoor.SummonCreature(657, pIronCladDoor.GetPositionX() - 2, pIronCladDoor.GetPositionY() - 7, pIronCladDoor.GetPositionZ(), 0, TEMPSUMMON_CORPSE_TIMED_DESPAWN, 3000);
                    Creature DefiasPirate2 = pIronCladDoor.SummonCreature(657, pIronCladDoor.GetPositionX() + 3, pIronCladDoor.GetPositionY() - 6, pIronCladDoor.GetPositionZ(), 0, TEMPSUMMON_CORPSE_TIMED_DESPAWN, 3000);
                    Creature DefiasCompanion = pIronCladDoor.SummonCreature(3450, pIronCladDoor.GetPositionX() + 2, pIronCladDoor.GetPositionY() - 6, pIronCladDoor.GetPositionZ(), 0, TEMPSUMMON_CORPSE_TIMED_DESPAWN, 3000);

                    DefiasPirate1GUID = DefiasPirate1.GetGUID();
                    DefiasPirate2GUID = DefiasPirate2.GetGUID();
                    DefiasCompanionGUID = DefiasCompanion.GetGUID();
                }
            }

            void MoveCreaturesInside()
            {
                if (DefiasPirate1GUID.IsEmpty() || DefiasPirate2GUID.IsEmpty() || DefiasCompanionGUID.IsEmpty())
                    return;

                Creature pDefiasPirate1 = instance.GetCreature(DefiasPirate1GUID);
                Creature pDefiasPirate2 = instance.GetCreature(DefiasPirate2GUID);
                Creature pDefiasCompanion = instance.GetCreature(DefiasCompanionGUID);
                if (!pDefiasPirate1 || !pDefiasPirate2 || !pDefiasCompanion)
                    return;

                MoveCreatureInside(pDefiasPirate1);
                MoveCreatureInside(pDefiasPirate2);
                MoveCreatureInside(pDefiasCompanion);
            }

            void MoveCreatureInside(Creature creature)
            {
                creature.SetWalk(false);
                creature.GetMotionMaster().MovePoint(0, -102.7f, -655.9f, creature.GetPositionZ());
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GO_FACTORY_DOOR: FactoryDoorGUID = go.GetGUID(); break;
                    case GO_IRONCLAD_DOOR: IronCladDoorGUID = go.GetGUID(); break;
                    case GO_DEFIAS_CANNON: DefiasCannonGUID = go.GetGUID(); break;
                    case GO_DOOR_LEVER: DoorLeverGUID = go.GetGUID(); break;
                    case GO_MR_SMITE_CHEST: uiSmiteChestGUID = go.GetGUID(); break;
                }
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case EVENT_STATE:
                        if (!DefiasCannonGUID.IsEmpty() && !IronCladDoorGUID.IsEmpty())
                            State = data;
                        break;
                    case EVENT_RHAHKZOR:
                        if (data == DONE)
                        {
                            GameObject go = instance.GetGameObject(FactoryDoorGUID);
                            if (go)
                                go.SetGoState(GO_STATE_ACTIVE);
                        }
                        break;
                }
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case EVENT_STATE:
                        return State;
                }

                return 0;
            }

            public override ObjectGuid GetGuidData(uint data)
            {
                switch (data)
                {
                    case DATA_SMITE_CHEST:
                        return uiSmiteChestGUID;
                }

                return ObjectGuid::Empty;
            }

            ObjectGuid FactoryDoorGUID;
            ObjectGuid IronCladDoorGUID;
            ObjectGuid DefiasCannonGUID;
            ObjectGuid DoorLeverGUID;
            ObjectGuid DefiasPirate1GUID;
            ObjectGuid DefiasPirate2GUID;
            ObjectGuid DefiasCompanionGUID;

            uint State;
            uint CannonBlast_Timer;
            uint PiratesDelay_Timer;
            ObjectGuid uiSmiteChestGUID;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_deadmines_InstanceMapScript(map);
        }
    }*/
}
