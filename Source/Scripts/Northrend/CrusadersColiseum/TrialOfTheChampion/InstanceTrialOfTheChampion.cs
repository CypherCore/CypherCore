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

using Framework.Constants;
using Framework.GameMath;
using Framework.IO;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.AI;

namespace Scripts.Northrend.CrusadersColiseum.TrialOfTheChampion
{
    [Script]
    class instance_trial_of_the_champion : InstanceMapScript
    {
        public instance_trial_of_the_champion() : base("instance_trial_of_the_champion", 650) { }

        class instance_trial_of_the_champion_InstanceMapScript : InstanceScript
        {
            public instance_trial_of_the_champion_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("TC");
                uiMovementDone = 0;
                uiGrandChampionsDeaths = 0;
                uiArgentSoldierDeaths = 0;

                //bDone = false;
            }

            public override bool IsEncounterInProgress()
            {
                for (byte i = 0; i < 4; ++i)
                {
                    if (m_auiEncounter[i] == EncounterState.InProgress)
                        return true;
                }

                return false;
            }

            public override void OnCreatureCreate(Creature creature)
            {
                var players = instance.GetPlayers();
                Team TeamInInstance = 0;

                if (!players.Empty())
                {
                    Player player = players.First();
                    if (player)
                        TeamInInstance = player.GetTeam();
                }

                switch (creature.GetEntry())
                {
                    // Champions
                    case VehicleIds.MOKRA_SKILLCRUSHER_MOUNT:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(VehicleIds.MARSHAL_JACOB_ALERIUS_MOUNT);
                        break;
                    case VehicleIds.ERESSEA_DAWNSINGER_MOUNT:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(VehicleIds.AMBROSE_BOLTSPARK_MOUNT);
                        break;
                    case VehicleIds.RUNOK_WILDMANE_MOUNT:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(VehicleIds.COLOSOS_MOUNT);
                        break;
                    case VehicleIds.ZUL_TORE_MOUNT:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(VehicleIds.EVENSONG_MOUNT);
                        break;
                    case VehicleIds.DEATHSTALKER_VESCERI_MOUNT:
                        if (TeamInInstance == Team.Horde)
                            creature.UpdateEntry(VehicleIds.LANA_STOUTHAMMER_MOUNT);
                        break;
                    // Coliseum Announcer || Just NPC_JAEREN must be spawned.
                    case CreatureIds.JAEREN:
                        uiAnnouncerGUID = creature.GetGUID();
                        if (TeamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.ARELAS);
                        break;
                    case VehicleIds.ARGENT_WARHORSE:
                    case VehicleIds.ARGENT_BATTLEWORG:
                        VehicleList.Add(creature.GetGUID());
                        break;
                    case CreatureIds.EADRIC:
                    case CreatureIds.PALETRESS:
                        uiArgentChampionGUID = creature.GetGUID();
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.MAIN_GATE:
                        uiMainGateGUID = go.GetGUID();
                        break;
                    case GameObjectIds.CHAMPIONS_LOOT:
                    case GameObjectIds.CHAMPIONS_LOOT_H:
                        uiChampionLootGUID = go.GetGUID();
                        break;
                }
            }

            public override void SetData(uint uiType, uint uiData)
            {
                switch (uiType)
                {
                    case (uint)Data.DATA_MOVEMENT_DONE:
                        uiMovementDone = (ushort)uiData;
                        if (uiMovementDone == 3)
                        {
                            Creature pAnnouncer = instance.GetCreature(uiAnnouncerGUID);
                            if (pAnnouncer)
                                pAnnouncer.GetAI().SetData((uint)Data.DATA_IN_POSITION, 0);
                        }
                        break;
                    case (uint)Data.BOSS_GRAND_CHAMPIONS:
                        m_auiEncounter[0] = (EncounterState)uiData;
                        if (uiData == (uint)EncounterState.InProgress)
                        {
                            foreach (var guid in VehicleList)
                            {
                                Creature summon = instance.GetCreature(guid);
                                if (summon)
                                    summon.RemoveFromWorld();
                            }
                        }
                        else if (uiData == (uint)EncounterState.Done)
                        {
                            ++uiGrandChampionsDeaths;
                            if (uiGrandChampionsDeaths == 3)
                            {
                                Creature pAnnouncer = instance.GetCreature(uiAnnouncerGUID);
                                if (pAnnouncer)
                                {
                                    pAnnouncer.GetMotionMaster().MovePoint(0, 748.309f, 619.487f, 411.171f);
                                    pAnnouncer.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                                    pAnnouncer.SummonGameObject(instance.IsHeroic() ? GameObjectIds.CHAMPIONS_LOOT_H : GameObjectIds.CHAMPIONS_LOOT, 746.59f, 618.49f, 411.09f, 1.42f, Quaternion.fromEulerAnglesZYX(1.42f, 0.0f, 0.0f), 90000);
                                }
                            }
                        }
                        break;
                    case (uint)Data.DATA_ARGENT_SOLDIER_DEFEATED:
                        uiArgentSoldierDeaths = (byte)uiData;
                        if (uiArgentSoldierDeaths == 9)
                        {
                            Creature pBoss = instance.GetCreature(uiArgentChampionGUID);
                            if (pBoss)
                            {
                                pBoss.GetMotionMaster().MovePoint(0, 746.88f, 618.74f, 411.06f);
                                pBoss.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                                pBoss.SetReactState(ReactStates.Aggressive);
                            }
                        }
                        break;
                    case (uint)Data.BOSS_ARGENT_CHALLENGE_E:
                        {
                            m_auiEncounter[1] = (EncounterState)uiData;
                            Creature pAnnouncer = instance.GetCreature(uiAnnouncerGUID);
                            if (pAnnouncer)
                            {
                                pAnnouncer.GetMotionMaster().MovePoint(0, 748.309f, 619.487f, 411.171f);
                                pAnnouncer.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                                pAnnouncer.SummonGameObject(instance.IsHeroic() ? GameObjectIds.EADRIC_LOOT_H : GameObjectIds.EADRIC_LOOT, 746.59f, 618.49f, 411.09f, 1.42f, Quaternion.fromEulerAnglesZYX(1.42f, 0.0f, 0.0f), 90000);
                            }
                        }
                        break;
                    case (uint)Data.BOSS_ARGENT_CHALLENGE_P:
                        {
                            m_auiEncounter[2] = (EncounterState)uiData;
                            Creature pAnnouncer = instance.GetCreature(uiAnnouncerGUID);
                            if (pAnnouncer)
                            {
                                pAnnouncer.GetMotionMaster().MovePoint(0, 748.309f, 619.487f, 411.171f);
                                pAnnouncer.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);
                                pAnnouncer.SummonGameObject(instance.IsHeroic() ? GameObjectIds.PALETRESS_LOOT_H : GameObjectIds.PALETRESS_LOOT, 746.59f, 618.49f, 411.09f, 1.42f, Quaternion.fromEulerAnglesZYX(1.42f, 0.0f, 0.0f), 90000);
                            }
                        }
                        break;
                }

                if (uiData == (uint)EncounterState.Done)
                    SaveToDB();
            }

            public override uint GetData(uint uiData)
            {
                switch (uiData)
                {
                    case (uint)Data.BOSS_GRAND_CHAMPIONS:
                        return (uint)m_auiEncounter[0];
                    case (uint)Data.BOSS_ARGENT_CHALLENGE_E:
                        return (uint)m_auiEncounter[1];
                    case (uint)Data.BOSS_ARGENT_CHALLENGE_P:
                        return (uint)m_auiEncounter[2];
                    case (uint)Data.BOSS_BLACK_KNIGHT:
                        return (uint)m_auiEncounter[3];

                    case (uint)Data.DATA_MOVEMENT_DONE:
                        return uiMovementDone;
                    case (uint)Data.DATA_ARGENT_SOLDIER_DEFEATED:
                        return uiArgentSoldierDeaths;
                }

                return 0;
            }

            public override ObjectGuid GetGuidData(uint uiData)
            {
                switch (uiData)
                {
                    case (uint)Data64.DATA_ANNOUNCER:
                        return uiAnnouncerGUID;
                    case (uint)Data64.DATA_MAIN_GATE:
                        return uiMainGateGUID;

                    case (uint)Data64.DATA_GRAND_CHAMPION_1:
                        return uiGrandChampion1GUID;
                    case (uint)Data64.DATA_GRAND_CHAMPION_2:
                        return uiGrandChampion2GUID;
                    case (uint)Data64.DATA_GRAND_CHAMPION_3:
                        return uiGrandChampion3GUID;
                }

                return ObjectGuid.Empty;
            }

            public override void SetGuidData(uint uiType, ObjectGuid uiData)
            {
                switch (uiType)
                {
                    case (uint)Data64.DATA_GRAND_CHAMPION_1:
                        uiGrandChampion1GUID = uiData;
                        break;
                    case (uint)Data64.DATA_GRAND_CHAMPION_2:
                        uiGrandChampion2GUID = uiData;
                        break;
                    case (uint)Data64.DATA_GRAND_CHAMPION_3:
                        uiGrandChampion3GUID = uiData;
                        break;
                }
            }

            public override string GetSaveData()
            {
                OUT_SAVE_INST_DATA();

                string str_data =
                    $"T C {m_auiEncounter[0]} {m_auiEncounter[1]} {m_auiEncounter[2]} {m_auiEncounter[3]} {uiGrandChampionsDeaths} {uiMovementDone}";

                OUT_SAVE_INST_DATA_COMPLETE();
                return str_data;
            }

            public override void Load(string str)
            {
                if (str.IsEmpty())
                {
                    OUT_LOAD_INST_DATA_FAIL();
                    return;
                }

                OUT_LOAD_INST_DATA(str);

                StringArguments loadStream = new StringArguments(str);
                string dataHead = loadStream.NextString();

                if (dataHead[0] == 'T' && dataHead[1] == 'C')
                {
                    for (byte i = 0; i < 4; ++i)
                    {
                        m_auiEncounter[i] = (EncounterState)loadStream.NextUInt32();
                        if (m_auiEncounter[i] == EncounterState.InProgress)
                            m_auiEncounter[i] = EncounterState.NotStarted;

                    }

                    uiGrandChampionsDeaths = loadStream.NextUInt16();
                    uiMovementDone = loadStream.NextUInt16();
                }
                else
                    OUT_LOAD_INST_DATA_FAIL();

                OUT_LOAD_INST_DATA_COMPLETE();
            }

            EncounterState[] m_auiEncounter = new EncounterState[4];

            ushort uiMovementDone;
            ushort uiGrandChampionsDeaths;
            byte uiArgentSoldierDeaths;

            ObjectGuid uiAnnouncerGUID;
            ObjectGuid uiMainGateGUID;
            //ObjectGuid uiGrandChampionVehicle1GUID;
            //ObjectGuid uiGrandChampionVehicle2GUID;
            //ObjectGuid uiGrandChampionVehicle3GUID;
            ObjectGuid uiGrandChampion1GUID;
            ObjectGuid uiGrandChampion2GUID;
            ObjectGuid uiGrandChampion3GUID;
            ObjectGuid uiChampionLootGUID;
            ObjectGuid uiArgentChampionGUID;

            List<ObjectGuid> VehicleList = new List<ObjectGuid>();

            //bool bDone;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_trial_of_the_champion_InstanceMapScript(map);
        }
    }
}
