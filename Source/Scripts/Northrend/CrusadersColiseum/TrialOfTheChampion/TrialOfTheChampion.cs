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
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System.Collections.Generic;


namespace Scripts.Northrend.CrusadersColiseum.TrialOfTheChampion
{
    struct TrialOfTheChampionConst
    {
        public const uint SAY_INTRO_1 = 0;
        public const uint SAY_INTRO_2 = 1;
        public const uint SAY_INTRO_3 = 2;
        public const uint SAY_AGGRO = 3;
        public const uint SAY_PHASE_2 = 4;
        public const uint SAY_PHASE_3 = 5;
        public const uint SAY_KILL_PLAYER = 6;
        public const uint SAY_DEATH = 7;

        public const string GOSSIP_START_EVENT1 = "I'm ready to start challenge.";
        public const string GOSSIP_START_EVENT2 = "I'm ready for the next challenge.";
    }

    enum Data
    {
        BOSS_GRAND_CHAMPIONS,
        BOSS_ARGENT_CHALLENGE_E,
        BOSS_ARGENT_CHALLENGE_P,
        BOSS_BLACK_KNIGHT,
        DATA_MOVEMENT_DONE,
        DATA_LESSER_CHAMPIONS_DEFEATED,
        DATA_START,
        DATA_IN_POSITION,
        DATA_ARGENT_SOLDIER_DEFEATED
    };

    enum Data64
    {
        DATA_ANNOUNCER,
        DATA_MAIN_GATE,

        DATA_GRAND_CHAMPION_VEHICLE_1,
        DATA_GRAND_CHAMPION_VEHICLE_2,
        DATA_GRAND_CHAMPION_VEHICLE_3,

        DATA_GRAND_CHAMPION_1,
        DATA_GRAND_CHAMPION_2,
        DATA_GRAND_CHAMPION_3
    };

    struct CreatureIds
    {
        // Horde Champions
        public const uint MOKRA = 35572;
        public const uint ERESSEA = 35569;
        public const uint RUNOK = 35571;
        public const uint ZULTORE = 35570;
        public const uint VISCERI = 35617;

        // Alliance Champions
        public const uint JACOB = 34705;
        public const uint AMBROSE = 34702;
        public const uint COLOSOS = 34701;
        public const uint JAELYNE = 34657;
        public const uint LANA = 34703;

        public const uint EADRIC = 35119;
        public const uint PALETRESS = 34928;

        public const uint ARGENT_LIGHWIELDER = 35309;
        public const uint ARGENT_MONK = 35305;
        public const uint PRIESTESS = 35307;

        public const uint BLACK_KNIGHT = 35451;

        public const uint RISEN_JAEREN = 35545;
        public const uint RISEN_ARELAS = 35564;

        public const uint JAEREN = 35004;
        public const uint ARELAS = 35005;
    }

    struct GameObjectIds
    {
        public const uint MAIN_GATE = 195647;

        public const uint CHAMPIONS_LOOT = 195709;
        public const uint CHAMPIONS_LOOT_H = 195710;

        public const uint EADRIC_LOOT = 195374;
        public const uint EADRIC_LOOT_H = 195375;

        public const uint PALETRESS_LOOT = 195323;
        public const uint PALETRESS_LOOT_H = 195324;
    }

    struct VehicleIds
    {
        //Grand Champions Alliance Vehicles
        public const uint MARSHAL_JACOB_ALERIUS_MOUNT = 35637;
        public const uint AMBROSE_BOLTSPARK_MOUNT = 35633;
        public const uint COLOSOS_MOUNT = 35768;
        public const uint EVENSONG_MOUNT = 34658;
        public const uint LANA_STOUTHAMMER_MOUNT = 35636;
        //Faction Champions (ALLIANCE)
        public const uint DARNASSIA_NIGHTSABER = 33319;
        public const uint EXODAR_ELEKK = 33318;
        public const uint STORMWIND_STEED = 33217;
        public const uint GNOMEREGAN_MECHANOSTRIDER = 33317;
        public const uint IRONFORGE_RAM = 33316;
        //Grand Champions Horde Vehicles
        public const uint MOKRA_SKILLCRUSHER_MOUNT = 35638;
        public const uint ERESSEA_DAWNSINGER_MOUNT = 35635;
        public const uint RUNOK_WILDMANE_MOUNT = 35640;
        public const uint ZUL_TORE_MOUNT = 35641;
        public const uint DEATHSTALKER_VESCERI_MOUNT = 35634;
        //Faction Champions (HORDE)
        public const uint FORSAKE_WARHORSE = 33324;
        public const uint THUNDER_BLUFF_KODO = 33322;
        public const uint ORGRIMMAR_WOLF = 33320;
        public const uint SILVERMOON_HAWKSTRIDER = 33323;
        public const uint DARKSPEAR_RAPTOR = 33321;

        public const uint ARGENT_WARHORSE = 35644;
        public const uint ARGENT_BATTLEWORG = 36558;

        public const uint BLACK_KNIGHT = 35491;
    }

    [Script]
    class npc_announcer_toc5 : CreatureScript
    {
        public npc_announcer_toc5() : base("npc_announcer_toc5") { }

        class npc_announcer_toc5AI : ScriptedAI
        {
            public npc_announcer_toc5AI(Creature creature) : base(creature)
            {
                instance = creature.GetInstanceScript();

                uiSummonTimes = 0;
                uiLesserChampions = 0;

                uiFirstBoss = 0;
                uiSecondBoss = 0;
                uiThirdBoss = 0;

                uiArgentChampion = 0;

                uiPhase = 0;
                uiTimer = 0;

                me.SetReactState(ReactStates.Passive);
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                me.SetFlag(UnitFields.NpcFlags, NPCFlags.Gossip);

                SetGrandChampionsForEncounter();
                SetArgentChampion();
            }

            void NextStep(uint uiTimerStep, bool bNextStep = true, byte uiPhaseStep = 0)
            {
                uiTimer = uiTimerStep;
                if (bNextStep)
                    ++uiPhase;
                else
                    uiPhase = uiPhaseStep;
            }

            public override void SetData(uint uiType, uint uiData)
            {
                switch (uiType)
                {
                    case (uint)Data.DATA_START:
                        DoSummonGrandChampion(uiFirstBoss);
                        NextStep(10000, false, 1);
                        break;
                    case (uint)Data.DATA_IN_POSITION: //movement EncounterState.Done.
                        me.GetMotionMaster().MovePoint(1, 735.81f, 661.92f, 412.39f);
                        GameObject go = ObjectAccessor.GetGameObject(me, instance.GetGuidData((uint)Data64.DATA_MAIN_GATE));
                        if (go)
                            instance.HandleGameObject(go.GetGUID(), false);
                        NextStep(10000, false, 3);
                        break;
                    case (uint)Data.DATA_LESSER_CHAMPIONS_DEFEATED:
                        {
                            ++uiLesserChampions;
                            List<ObjectGuid> TempList = new List<ObjectGuid>();
                            if (uiLesserChampions == 3 || uiLesserChampions == 6)
                            {
                                switch (uiLesserChampions)
                                {
                                    case 3:
                                        TempList = Champion2List;
                                        break;
                                    case 6:
                                        TempList = Champion3List;
                                        break;
                                }

                                foreach (var guid in TempList)
                                {
                                    Creature summon = ObjectAccessor.GetCreature(me, guid);
                                    if (summon)
                                        AggroAllPlayers(summon);
                                }
                            }
                            else if (uiLesserChampions == 9)
                                StartGrandChampionsAttack();

                            break;
                        }
                }
            }

            void StartGrandChampionsAttack()
            {
                Creature pGrandChampion1 = ObjectAccessor.GetCreature(me, uiVehicle1GUID);
                Creature pGrandChampion2 = ObjectAccessor.GetCreature(me, uiVehicle2GUID);
                Creature pGrandChampion3 = ObjectAccessor.GetCreature(me, uiVehicle3GUID);

                if (pGrandChampion1 && pGrandChampion2 && pGrandChampion3)
                {
                    AggroAllPlayers(pGrandChampion1);
                    AggroAllPlayers(pGrandChampion2);
                    AggroAllPlayers(pGrandChampion3);
                }
            }

            public override void MovementInform(MovementGeneratorType uiType, uint uiPointId)
            {
                if (uiType != MovementGeneratorType.Point)
                    return;

                if (uiPointId == 1)
                    me.SetFacingTo(4.714f);
            }

            void DoSummonGrandChampion(uint uiBoss)
            {
                ++uiSummonTimes;
                uint VEHICLE_TO_SUMMON1 = 0;
                uint VEHICLE_TO_SUMMON2 = 0;
                switch (uiBoss)
                {
                    case 0:
                        VEHICLE_TO_SUMMON1 = VehicleIds.MOKRA_SKILLCRUSHER_MOUNT;
                        VEHICLE_TO_SUMMON2 = VehicleIds.ORGRIMMAR_WOLF;
                        break;
                    case 1:
                        VEHICLE_TO_SUMMON1 = VehicleIds.ERESSEA_DAWNSINGER_MOUNT;
                        VEHICLE_TO_SUMMON2 = VehicleIds.SILVERMOON_HAWKSTRIDER;
                        break;
                    case 2:
                        VEHICLE_TO_SUMMON1 = VehicleIds.RUNOK_WILDMANE_MOUNT;
                        VEHICLE_TO_SUMMON2 = VehicleIds.THUNDER_BLUFF_KODO;
                        break;
                    case 3:
                        VEHICLE_TO_SUMMON1 = VehicleIds.ZUL_TORE_MOUNT;
                        VEHICLE_TO_SUMMON2 = VehicleIds.DARKSPEAR_RAPTOR;
                        break;
                    case 4:
                        VEHICLE_TO_SUMMON1 = VehicleIds.DEATHSTALKER_VESCERI_MOUNT;
                        VEHICLE_TO_SUMMON2 = VehicleIds.FORSAKE_WARHORSE;
                        break;
                    default:
                        return;
                }

                Creature pBoss = me.SummonCreature(VEHICLE_TO_SUMMON1, SpawnPosition);
                if (pBoss)
                {
                    ObjectGuid uiGrandChampionBoss = ObjectGuid.Empty;
                    Vehicle pVehicle = pBoss.GetVehicleKit();
                    if (pVehicle)
                    {
                        Unit unit = pVehicle.GetPassenger(0);
                        if (unit)
                            uiGrandChampionBoss = unit.GetGUID();
                    }

                    switch (uiSummonTimes)
                    {
                        case 1:
                            uiVehicle1GUID = pBoss.GetGUID();
                            instance.SetGuidData((uint)Data64.DATA_GRAND_CHAMPION_VEHICLE_1, uiVehicle1GUID);
                            instance.SetGuidData((uint)Data64.DATA_GRAND_CHAMPION_1, uiGrandChampionBoss);
                            break;
                        case 2:

                            uiVehicle2GUID = pBoss.GetGUID();
                            instance.SetGuidData((uint)Data64.DATA_GRAND_CHAMPION_VEHICLE_2, uiVehicle2GUID);
                            instance.SetGuidData((uint)Data64.DATA_GRAND_CHAMPION_2, uiGrandChampionBoss);
                            break;
                        case 3:
                            uiVehicle3GUID = pBoss.GetGUID();
                            instance.SetGuidData((uint)Data64.DATA_GRAND_CHAMPION_VEHICLE_3, uiVehicle3GUID);
                            instance.SetGuidData((uint)Data64.DATA_GRAND_CHAMPION_3, uiGrandChampionBoss);
                            break;

                        default:
                            return;
                    }
                    pBoss.GetAI().SetData(uiSummonTimes, 0);

                    for (byte i = 0; i < 3; ++i)
                    {
                        Creature pAdd = me.SummonCreature(VEHICLE_TO_SUMMON2, SpawnPosition, TempSummonType.CorpseDespawn);
                        if (pAdd)
                        {
                            switch (uiSummonTimes)
                            {
                                case 1:
                                    Champion1List.Add(pAdd.GetGUID());
                                    break;
                                case 2:
                                    Champion2List.Add(pAdd.GetGUID());
                                    break;
                                case 3:
                                    Champion3List.Add(pAdd.GetGUID());
                                    break;
                            }

                            switch (i)
                            {
                                case 0:
                                    pAdd.GetMotionMaster().MoveFollow(pBoss, 2.0f, MathFunctions.PI);
                                    break;
                                case 1:
                                    pAdd.GetMotionMaster().MoveFollow(pBoss, 2.0f, MathFunctions.PI / 2);
                                    break;
                                case 2:
                                    pAdd.GetMotionMaster().MoveFollow(pBoss, 2.0f, MathFunctions.PI / 2 + MathFunctions.PI);
                                    break;
                            }
                        }

                    }
                }
            }

            void DoStartArgentChampionEncounter()
            {
                me.GetMotionMaster().MovePoint(1, 735.81f, 661.92f, 412.39f);

                if (me.SummonCreature(uiArgentChampion, SpawnPosition))
                {
                    for (byte i = 0; i < 3; ++i)
                    {
                        Creature lightwielderTrash = me.SummonCreature(CreatureIds.ARGENT_LIGHWIELDER, SpawnPosition);
                        if (lightwielderTrash)
                            lightwielderTrash.GetAI().SetData(i, 0);

                        Creature monkTrash = me.SummonCreature(CreatureIds.ARGENT_MONK, SpawnPosition);
                        if (monkTrash)
                            monkTrash.GetAI().SetData(i, 0);

                        Creature priestessTrash = me.SummonCreature(CreatureIds.PRIESTESS, SpawnPosition);
                        if (priestessTrash)
                            priestessTrash.GetAI().SetData(i, 0);
                    }
                }
            }

            void SetGrandChampionsForEncounter()
            {
                uiFirstBoss = RandomHelper.URand(0, 4);

                while (uiSecondBoss == uiFirstBoss || uiThirdBoss == uiFirstBoss || uiThirdBoss == uiSecondBoss)
                {
                    uiSecondBoss = RandomHelper.URand(0, 4);
                    uiThirdBoss = RandomHelper.URand(0, 4);
                }
            }

            void SetArgentChampion()
            {
                byte uiTempBoss = (byte)RandomHelper.URand(0, 1);

                switch (uiTempBoss)
                {
                    case 0:
                        uiArgentChampion = CreatureIds.EADRIC;
                        break;
                    case 1:
                        uiArgentChampion = CreatureIds.PALETRESS;
                        break;
                }
            }

            public void StartEncounter()
            {
                me.RemoveFlag(UnitFields.NpcFlags, NPCFlags.Gossip);

                if (instance.GetData((uint)Data.BOSS_BLACK_KNIGHT) == (uint)EncounterState.NotStarted)
                {
                    if (instance.GetData((uint)Data.BOSS_ARGENT_CHALLENGE_E) == (uint)EncounterState.NotStarted && instance.GetData((uint)Data.BOSS_ARGENT_CHALLENGE_P) == (uint)EncounterState.NotStarted)
                    {
                        if (instance.GetData((uint)Data.BOSS_GRAND_CHAMPIONS) == (uint)EncounterState.NotStarted)
                            SetData((uint)Data.DATA_START, 0);

                        if (instance.GetData((uint)Data.BOSS_GRAND_CHAMPIONS) == (uint)EncounterState.Done)
                            DoStartArgentChampionEncounter();
                    }

                    if ((instance.GetData((uint)Data.BOSS_GRAND_CHAMPIONS) == (uint)EncounterState.Done &&
                        instance.GetData((uint)Data.BOSS_ARGENT_CHALLENGE_E) == (uint)EncounterState.Done) ||
                        instance.GetData((uint)Data.BOSS_ARGENT_CHALLENGE_P) == (uint)EncounterState.Done)
                        me.SummonCreature(VehicleIds.BLACK_KNIGHT, 769.834f, 651.915f, 447.035f, 0);
                }
            }

            void AggroAllPlayers(Creature temp)
            {
                var PlList = me.GetMap().GetPlayers();

                if (PlList.Empty())
                    return;

                foreach (var player in PlList)
                {
                    if (player.IsGameMaster())
                        continue;

                    if (player.IsAlive())
                    {
                        temp.SetHomePosition(me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetOrientation());
                        temp.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                        temp.SetReactState(ReactStates.Aggressive);
                        temp.SetInCombatWith(player);
                        player.SetInCombatWith(temp);
                        temp.AddThreat(player, 0.0f);
                    }
                }
            }

            public override void UpdateAI(uint diff)
            {
                base.UpdateAI(diff);

                if (uiTimer <= diff)
                {
                    switch (uiPhase)
                    {
                        case 1:
                            DoSummonGrandChampion(uiSecondBoss);
                            NextStep(10000, true);
                            break;
                        case 2:
                            DoSummonGrandChampion(uiThirdBoss);
                            NextStep(0, false);
                            break;
                        case 3:
                            if (!Champion1List.Empty())
                            {
                                foreach (var guid in Champion1List)
                                {
                                    Creature summon = ObjectAccessor.GetCreature(me, guid);
                                    if (summon)
                                        AggroAllPlayers(summon);
                                }
                                NextStep(0, false);
                            }
                            break;
                    }
                }
                else
                    uiTimer -= diff;

                if (!UpdateVictim())
                    return;
            }

            public override void JustSummoned(Creature summon)
            {
                if (instance.GetData((uint)Data.BOSS_GRAND_CHAMPIONS) == (uint)EncounterState.NotStarted)
                {
                    summon.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                    summon.SetReactState(ReactStates.Passive);
                }
            }

            public override void SummonedCreatureDespawn(Creature summon)
            {
                switch (summon.GetEntry())
                {
                    case VehicleIds.DARNASSIA_NIGHTSABER:
                    case VehicleIds.EXODAR_ELEKK:
                    case VehicleIds.STORMWIND_STEED:
                    case VehicleIds.GNOMEREGAN_MECHANOSTRIDER:
                    case VehicleIds.IRONFORGE_RAM:
                    case VehicleIds.FORSAKE_WARHORSE:
                    case VehicleIds.THUNDER_BLUFF_KODO:
                    case VehicleIds.ORGRIMMAR_WOLF:
                    case VehicleIds.SILVERMOON_HAWKSTRIDER:
                    case VehicleIds.DARKSPEAR_RAPTOR:
                        SetData((uint)Data.DATA_LESSER_CHAMPIONS_DEFEATED, 0);
                        break;
                }
            }

            InstanceScript instance;

            byte uiSummonTimes;
            byte uiLesserChampions;

            uint uiArgentChampion;

            uint uiFirstBoss;
            uint uiSecondBoss;
            uint uiThirdBoss;

            uint uiPhase;
            uint uiTimer;

            ObjectGuid uiVehicle1GUID;
            ObjectGuid uiVehicle2GUID;
            ObjectGuid uiVehicle3GUID;

            List<ObjectGuid> Champion1List = new List<ObjectGuid>();
            List<ObjectGuid> Champion2List = new List<ObjectGuid>();
            List<ObjectGuid> Champion3List = new List<ObjectGuid>();

            Position SpawnPosition = new Position(746.261f, 657.401f, 411.681f, 4.65f);
        }

        public override bool OnGossipHello(Player player, Creature creature)
        {
            InstanceScript instance = creature.GetInstanceScript();

            if (instance != null &&
                ((instance.GetData((uint)Data.BOSS_GRAND_CHAMPIONS) == (uint)EncounterState.Done &&
                instance.GetData((uint)Data.BOSS_BLACK_KNIGHT) == (uint)EncounterState.Done &&
                instance.GetData((uint)Data.BOSS_ARGENT_CHALLENGE_E) == (uint)EncounterState.Done) ||
                instance.GetData((uint)Data.BOSS_ARGENT_CHALLENGE_P) == (uint)EncounterState.Done))
                return false;

            if (instance != null &&
                instance.GetData((uint)Data.BOSS_GRAND_CHAMPIONS) == (uint)EncounterState.NotStarted &&
                instance.GetData((uint)Data.BOSS_ARGENT_CHALLENGE_E) == (uint)EncounterState.NotStarted &&
                instance.GetData((uint)Data.BOSS_ARGENT_CHALLENGE_P) == (uint)EncounterState.NotStarted &&
                instance.GetData((uint)Data.BOSS_BLACK_KNIGHT) == (uint)EncounterState.NotStarted)
                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, TrialOfTheChampionConst.GOSSIP_START_EVENT1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
            else if (instance != null)
                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, TrialOfTheChampionConst.GOSSIP_START_EVENT2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

            player.SEND_GOSSIP_MENU(player.GetGossipTextId(creature), creature.GetGUID());

            return true;
        }

        public override bool OnGossipSelect(Player player, Creature creature, uint sender, uint action)
        {
            player.PlayerTalkClass.ClearMenus();
            if (action == eTradeskill.GossipActionInfoDef + 1)
            {
                player.CLOSE_GOSSIP_MENU();
                ((npc_announcer_toc5AI)creature.GetAI()).StartEncounter();
            }

            return true;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<npc_announcer_toc5AI>(creature);
        }
    }
}
