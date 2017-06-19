using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Network;
using Game.Entities;
using Framework.Constants;
using Game.Scripting;
using Game.Network.Packets;

namespace Game.PvP.Nagrand
{
    struct Misc
    {
        // kill credit for pks
        public const uint CreditMarker = 24867;

        public const uint MaxGuards = 15;

        public const uint BuffZone = 3518;

        public const uint HalaaGraveyard = 993;

        public const uint HalaaGraveyardZone = 3518; // need to add zone id, not area id

        public const uint RespawnTime = 3600000; // one hour to capture after defeating all guards

        public const uint GuardCheckTime = 500; // every half second

        public const uint FlightNodesNum = 4;

        public static uint[] FlightPathStartNodes = { 103, 105, 107, 109 };
        public static uint[] FlightPathEndNodes = { 104, 106, 108, 110 };

        // spawned when the alliance is attacking, horde is in control
        public static go_type[] HordeControlGOs =
        {
            new go_type(182267, 530, -1815.8f, 8036.51f, -26.2354f, -2.89725f, 0.0f, 0.0f, 0.992546f, -0.121869f), //ALLY_ROOST_SOUTH
            new go_type(182280, 530, -1507.95f, 8132.1f, -19.5585f, -1.3439f, 0.0f, 0.0f, 0.622515f, -0.782608f), //ALLY_ROOST_WEST
            new go_type(182281, 530, -1384.52f, 7779.33f, -11.1663f, -0.575959f, 0.0f, 0.0f, 0.284015f, -0.95882f), //ALLY_ROOST_NORTH
            new go_type(182282, 530, -1650.11f, 7732.56f, -15.4505f, -2.80998f, 0.0f, 0.0f, 0.986286f, -0.165048f), //ALLY_ROOST_EAST

            new go_type(182222, 530, -1825.4022f, 8039.2602f, -26.08f, -2.89725f, 0.0f, 0.0f, 0.992546f, -0.121869f), //HORDE_BOMB_WAGON_SOUTH
            new go_type(182272, 530, -1515.37f, 8136.91f, -20.42f, -1.3439f, 0.0f, 0.0f, 0.622515f, -0.782608f), //HORDE_BOMB_WAGON_WEST
            new go_type(182273, 530, -1377.95f, 7773.44f, -10.31f, -0.575959f, 0.0f, 0.0f, 0.284015f, -0.95882f), //HORDE_BOMB_WAGON_NORTH
            new go_type(182274, 530, -1659.87f, 7733.15f, -15.75f, -2.80998f, 0.0f, 0.0f, 0.986286f, -0.165048f), //HORDE_BOMB_WAGON_EAST

            new go_type(182266, 530, -1815.8f, 8036.51f, -26.2354f, -2.89725f, 0.0f, 0.0f, 0.992546f, -0.121869f), //DESTROYED_ALLY_ROOST_SOUTH
            new go_type(182275, 530, -1507.95f, 8132.1f, -19.5585f, -1.3439f, 0.0f, 0.0f, 0.622515f, -0.782608f), //DESTROYED_ALLY_ROOST_WEST
            new go_type(182276, 530, -1384.52f, 7779.33f, -11.1663f, -0.575959f, 0.0f, 0.0f, 0.284015f, -0.95882f), //DESTROYED_ALLY_ROOST_NORTH
            new go_type(182277, 530, -1650.11f, 7732.56f, -15.4505f, -2.80998f, 0.0f, 0.0f, 0.986286f, -0.165048f)  //DESTROYED_ALLY_ROOST_EAST
        };

        // spawned when the horde is attacking, alliance is in control
        public static go_type[] AllianceControlGOs =
        {
            new go_type(182301, 530, -1815.8f, 8036.51f, -26.2354f, -2.89725f, 0.0f, 0.0f, 0.992546f, -0.121869f), //HORDE_ROOST_SOUTH
            new go_type(182302, 530, -1507.95f, 8132.1f, -19.5585f, -1.3439f, 0.0f, 0.0f, 0.622515f, -0.782608f), //HORDE_ROOST_WEST
            new go_type(182303, 530, -1384.52f, 7779.33f, -11.1663f, -0.575959f, 0.0f, 0.0f, 0.284015f, -0.95882f), //HORDE_ROOST_NORTH
            new go_type(182304, 530, -1650.11f, 7732.56f, -15.4505f, -2.80998f, 0.0f, 0.0f, 0.986286f, -0.165048f), //HORDE_ROOST_EAST

            new go_type(182305, 530, -1825.4022f, 8039.2602f, -26.08f, -2.89725f, 0.0f, 0.0f, 0.992546f, -0.121869f), //ALLY_BOMB_WAGON_SOUTH
            new go_type(182306, 530, -1515.37f, 8136.91f, -20.42f, -1.3439f, 0.0f, 0.0f, 0.622515f, -0.782608f), //ALLY_BOMB_WAGON_WEST
            new go_type(182307, 530, -1377.95f, 7773.44f, -10.31f, -0.575959f, 0.0f, 0.0f, 0.284015f, -0.95882f), //ALLY_BOMB_WAGON_NORTH
            new go_type(182308, 530, -1659.87f, 7733.15f, -15.75f, -2.80998f, 0.0f, 0.0f, 0.986286f, -0.165048f), //ALLY_BOMB_WAGON_EAST

            new go_type(182297, 530, -1815.8f, 8036.51f, -26.2354f, -2.89725f, 0.0f, 0.0f, 0.992546f, -0.121869f), //DESTROYED_HORDE_ROOST_SOUTH
            new go_type(182298, 530, -1507.95f, 8132.1f, -19.5585f, -1.3439f, 0.0f, 0.0f, 0.622515f, -0.782608f), //DESTROYED_HORDE_ROOST_WEST
            new go_type(182299, 530, -1384.52f, 7779.33f, -11.1663f, -0.575959f, 0.0f, 0.0f, 0.284015f, -0.95882f), //DESTROYED_HORDE_ROOST_NORTH
            new go_type(182300, 530, -1650.11f, 7732.56f, -15.4505f, -2.80998f, 0.0f, 0.0f, 0.986286f, -0.165048f)  //DESTROYED_HORDE_ROOST_EAST
        };

        public static creature_type[] HordeControlNPCs =
        {
            new creature_type(18816, 530, -1523.92f, 7951.76f, -17.6942f, 3.51172f),
            new creature_type(18821, 530, -1527.75f, 7952.46f, -17.6948f, 3.99317f),
            new creature_type(21474, 530, -1520.14f, 7927.11f, -20.2527f, 3.39389f),
            new creature_type(21484, 530, -1524.84f, 7930.34f, -20.182f, 3.6405f),
            new creature_type(21483, 530, -1570.01f, 7993.8f, -22.4505f, 5.02655f),
            new creature_type(18192, 530, -1654.06f, 8000.46f, -26.59f, 3.37f),
            new creature_type(18192, 530, -1487.18f, 7899.1f, -19.53f, 0.954f),
            new creature_type(18192, 530, -1480.88f, 7908.79f, -19.19f, 4.485f),
            new creature_type(18192, 530, -1540.56f, 7995.44f, -20.45f, 0.947f),
            new creature_type(18192, 530, -1546.95f, 8000.85f, -20.72f, 6.035f),
            new creature_type(18192, 530, -1595.31f, 7860.53f, -21.51f, 3.747f),
            new creature_type(18192, 530, -1642.31f, 7995.59f, -25.8f, 3.317f),
            new creature_type(18192, 530, -1545.46f, 7995.35f, -20.63f, 1.094f),
            new creature_type(18192, 530, -1487.58f, 7907.99f, -19.27f, 5.567f),
            new creature_type(18192, 530, -1651.54f, 7988.56f, -26.5289f, 2.98451f),
            new creature_type(18192, 530, -1602.46f, 7866.43f, -22.1177f, 4.74729f),
            new creature_type(18192, 530, -1591.22f, 7875.29f, -22.3536f, 4.34587f),
            new creature_type(18192, 530, -1550.6f, 7944.45f, -21.63f, 3.559f),
            new creature_type(18192, 530, -1545.57f, 7935.83f, -21.13f, 3.448f),
            new creature_type(18192, 530, -1550.86f, 7937.56f, -21.7f, 3.801f)
        };

        public static creature_type[] AllianceControlNPCs =
        {
            new creature_type(18817, 530, -1591.18f, 8020.39f, -22.2042f, 4.59022f),
            new creature_type(18822, 530, -1588.0f, 8019.0f, -22.2042f, 4.06662f),
            new creature_type(21485, 530, -1521.93f, 7927.37f, -20.2299f, 3.24631f),
            new creature_type(21487, 530, -1540.33f, 7971.95f, -20.7186f, 3.07178f),
            new creature_type(21488, 530, -1570.01f, 7993.8f, -22.4505f, 5.02655f),
            new creature_type(18256, 530, -1654.06f, 8000.46f, -26.59f, 3.37f),
            new creature_type(18256, 530, -1487.18f, 7899.1f, -19.53f, 0.954f),
            new creature_type(18256, 530, -1480.88f, 7908.79f, -19.19f, 4.485f),
            new creature_type(18256, 530, -1540.56f, 7995.44f, -20.45f, 0.947f),
            new creature_type(18256, 530, -1546.95f, 8000.85f, -20.72f, 6.035f),
            new creature_type(18256, 530, -1595.31f, 7860.53f, -21.51f, 3.747f),
            new creature_type(18256, 530, -1642.31f, 7995.59f, -25.8f, 3.317f),
            new creature_type(18256, 530, -1545.46f, 7995.35f, -20.63f, 1.094f),
            new creature_type(18256, 530, -1487.58f, 7907.99f, -19.27f, 5.567f),
            new creature_type(18256, 530, -1651.54f, 7988.56f, -26.5289f, 2.98451f),
            new creature_type(18256, 530, -1602.46f, 7866.43f, -22.1177f, 4.74729f),
            new creature_type(18256, 530, -1591.22f, 7875.29f, -22.3536f, 4.34587f),
            new creature_type(18256, 530, -1603.75f, 8000.36f, -24.18f, 4.516f),
            new creature_type(18256, 530, -1585.73f, 7994.68f, -23.29f, 4.439f),
            new creature_type(18256, 530, -1595.5f, 7991.27f, -23.53f, 4.738f)
        };
    }

    class OutdoorPvPNA : OutdoorPvP
    {
        public OutdoorPvPNA()
        {
            m_TypeId = OutdoorPvPTypes.Nagrand;
            m_obj = null;
        }

        public override void HandleKillImpl(Player player, Unit killed)
        {
            if (killed.GetTypeId() == TypeId.Player && player.GetTeam() != killed.ToPlayer().GetTeam())
            {
                player.KilledMonsterCredit(NA_CREDIT_MARKER); // 0 guid, btw it isn't even used in killedmonster function :S
                if (player.GetTeam() == Team.Alliance)
                    player.CastSpell(player, NA_KILL_TOKEN_ALLIANCE, true);
                else
                    player.CastSpell(player, NA_KILL_TOKEN_HORDE, true);
            }
        }

        public override bool SetupOutdoorPvP()
        {
            //    m_TypeId = OUTDOOR_PVP_NA; _MUST_ be set in ctor, because of spawns cleanup
            // add the zones affected by the pvp buff
            SetMapFromZone(NA_BUFF_ZONE);
            RegisterZone(NA_BUFF_ZONE);

            // halaa
            m_obj = new OPvPCapturePointNA(this);

            AddCapturePoint(m_obj);
            return true;
        }

        public override void HandlePlayerEnterZone(Player player, uint zone)
        {
            // add buffs
            if (player.GetTeam() == m_obj.GetControllingFaction())
                player.CastSpell(player, NA_CAPTURE_BUFF, true);
            base.HandlePlayerEnterZone(player, zone);
        }

        public override void HandlePlayerLeaveZone(Player player, uint zone)
        {
            // remove buffs
            player.RemoveAurasDueToSpell(NA_CAPTURE_BUFF);
            base.HandlePlayerLeaveZone(player, zone);
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            m_obj.FillInitialWorldStates(packet);
        }

        public override void SendRemoveWorldStates(Player player)
        {
            player.SendUpdateWorldState(NA_UI_HORDE_GUARDS_SHOW, 0);
            player.SendUpdateWorldState(NA_UI_ALLIANCE_GUARDS_SHOW, 0);
            player.SendUpdateWorldState(NA_UI_GUARDS_MAX, 0);
            player.SendUpdateWorldState(NA_UI_GUARDS_LEFT, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_NORTH_NEU_H, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_NORTH_NEU_A, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_NORTH_H, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_NORTH_A, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_SOUTH_NEU_H, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_SOUTH_NEU_A, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_SOUTH_H, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_SOUTH_A, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_WEST_NEU_H, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_WEST_NEU_A, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_WEST_H, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_WEST_A, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_EAST_NEU_H, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_EAST_NEU_A, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_EAST_H, 0);
            player.SendUpdateWorldState(NA_MAP_WYVERN_EAST_A, 0);
            player.SendUpdateWorldState(NA_MAP_HALAA_NEUTRAL, 0);
            player.SendUpdateWorldState(NA_MAP_HALAA_NEU_A, 0);
            player.SendUpdateWorldState(NA_MAP_HALAA_NEU_H, 0);
            player.SendUpdateWorldState(NA_MAP_HALAA_HORDE, 0);
            player.SendUpdateWorldState(NA_MAP_HALAA_ALLIANCE, 0);
        }

        public override bool Update(uint diff)
        {
            return m_obj.Update(diff);
        }

        OPvPCapturePointNA m_obj;
    }

    class OPvPCapturePointNA : OPvPCapturePoint
    {
        public OPvPCapturePointNA(OutdoorPvP pvp) : base(pvp)
        {
            m_capturable = true;
            m_HalaaState = HALAA_N;
            m_RespawnTimer = Misc.RespawnTime;
            m_GuardCheckTimer = Misc.GuardCheckTime;
            SetCapturePointData(182210, 530, -1572.57f, 7945.3f, -22.475f, 2.05949f, 0.0f, 0.0f, 0.857167f, 0.515038f);
        }

        uint GetAliveGuardsCount()
        {
            uint cnt = 0;
            foreach (var pair in m_Creatures)
            {
                switch (pair.Key)
                {
                    case NA_NPC_GUARD_01:
                    case NA_NPC_GUARD_02:
                    case NA_NPC_GUARD_03:
                    case NA_NPC_GUARD_04:
                    case NA_NPC_GUARD_05:
                    case NA_NPC_GUARD_06:
                    case NA_NPC_GUARD_07:
                    case NA_NPC_GUARD_08:
                    case NA_NPC_GUARD_09:
                    case NA_NPC_GUARD_10:
                    case NA_NPC_GUARD_11:
                    case NA_NPC_GUARD_12:
                    case NA_NPC_GUARD_13:
                    case NA_NPC_GUARD_14:
                    case NA_NPC_GUARD_15:
                        {
                            var bounds = m_PvP.GetMap().GetCreatureBySpawnIdStore().LookupByKey(pair.Value);
                            foreach (var creature in bounds)
                                if (creature.IsAlive())
                                    ++cnt;
                            break;
                        }
                    default:
                        break;
                }
            }
            return cnt;
        }

        public Team GetControllingFaction()
        {
            return m_ControllingFaction;
        }

        void SpawnNPCsForTeam(Team team)
        {
            creature_type[] creatures = null;
            if (team == Team.Alliance)
                creatures = Misc.AllianceControlNPCs;
            else if (team == Team.Horde)
                creatures = Misc.HordeControlNPCs;
            else
                return;
            for (int i = 0; i < NA_CONTROL_NPC_NUM; ++i)
                AddCreature(i, creatures[i].entry, creatures[i].map, creatures[i].x, creatures[i].y, creatures[i].z, creatures[i].o, OutdoorPvP.GetTeamIdByTeam(team), 1000000);
        }

        void DeSpawnNPCs()
        {
            for (uint i = 0; i < NA_CONTROL_NPC_NUM; ++i)
                DelCreature(i);
        }

        void SpawnGOsForTeam(Team team)
        {
            go_type[] gos = null;
            if (team == Team.Alliance)
                gos = Misc.AllianceControlGOs;
            else if (team == Team.Horde)
                gos = Misc.HordeControlGOs;
            else
                return;
            for (uint i = 0; i < NA_CONTROL_GO_NUM; ++i)
            {
                if (i == NA_ROOST_S ||
                    i == NA_ROOST_W ||
                    i == NA_ROOST_N ||
                    i == NA_ROOST_E ||
                    i == NA_BOMB_WAGON_S ||
                    i == NA_BOMB_WAGON_W ||
                    i == NA_BOMB_WAGON_N ||
                    i == NA_BOMB_WAGON_E)
                    continue;   // roosts and bomb wagons are spawned when someone uses the matching destroyed roost
                AddObject(i, gos[i].entry, gos[i].map, gos[i].x, gos[i].y, gos[i].z, gos[i].o, gos[i].rot0, gos[i].rot1, gos[i].rot2, gos[i].rot3);
            }
        }

        void DeSpawnGOs()
        {
            for (uint i = 0; i < NA_CONTROL_GO_NUM; ++i)
            {
                DelObject(i);
            }
        }

        void FactionTakeOver(Team team)
        {
            if (m_ControllingFaction != 0)
                Global.ObjectMgr.RemoveGraveYardLink(NA_HALAA_GRAVEYARD, NA_HALAA_GRAVEYARD_ZONE, m_ControllingFaction, false);

            m_ControllingFaction = team;
            if (m_ControllingFaction != 0)
                Global.ObjectMgr.AddGraveYardLink(NA_HALAA_GRAVEYARD, NA_HALAA_GRAVEYARD_ZONE, m_ControllingFaction, false);
            DeSpawnGOs();
            DeSpawnNPCs();
            SpawnGOsForTeam(team);
            SpawnNPCsForTeam(team);
            m_GuardsAlive = Misc.MaxGuards;
            m_capturable = false;
            this.UpdateHalaaWorldState();
            if (team == Team.Alliance)
            {
                m_WyvernStateSouth = WYVERN_NEU_HORDE;
                m_WyvernStateNorth = WYVERN_NEU_HORDE;
                m_WyvernStateEast = WYVERN_NEU_HORDE;
                m_WyvernStateWest = WYVERN_NEU_HORDE;
                m_PvP.TeamApplyBuff(TeamId.Alliance, NA_CAPTURE_BUFF);
                m_PvP.SendUpdateWorldState(NA_UI_HORDE_GUARDS_SHOW, 0);
                m_PvP.SendUpdateWorldState(NA_UI_ALLIANCE_GUARDS_SHOW, 1);
                m_PvP.SendUpdateWorldState(NA_UI_GUARDS_LEFT, m_GuardsAlive);
                m_PvP.SendDefenseMessage(NA_HALAA_GRAVEYARD_ZONE, TEXT_HALAA_TAKEN_ALLIANCE);
            }
            else
            {
                m_WyvernStateSouth = WYVERN_NEU_ALLIANCE;
                m_WyvernStateNorth = WYVERN_NEU_ALLIANCE;
                m_WyvernStateEast = WYVERN_NEU_ALLIANCE;
                m_WyvernStateWest = WYVERN_NEU_ALLIANCE;
                m_PvP.TeamApplyBuff(TeamId.Horde, NA_CAPTURE_BUFF);
                m_PvP.SendUpdateWorldState(NA_UI_HORDE_GUARDS_SHOW, 1);
                m_PvP.SendUpdateWorldState(NA_UI_ALLIANCE_GUARDS_SHOW, 0);
                m_PvP.SendUpdateWorldState(NA_UI_GUARDS_LEFT, m_GuardsAlive);
                m_PvP.SendDefenseMessage(NA_HALAA_GRAVEYARD_ZONE, TEXT_HALAA_TAKEN_HORDE);
            }
            UpdateWyvernRoostWorldState(NA_ROOST_S);
            UpdateWyvernRoostWorldState(NA_ROOST_N);
            UpdateWyvernRoostWorldState(NA_ROOST_W);
            UpdateWyvernRoostWorldState(NA_ROOST_E);
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            if (m_ControllingFaction == Team.Alliance)
            {
                packet.AddState(NA_UI_HORDE_GUARDS_SHOW, 0);
                packet.AddState(NA_UI_ALLIANCE_GUARDS_SHOW, 1);
            }
            else if (m_ControllingFaction == Team.Horde)
            {
                packet.AddState(NA_UI_HORDE_GUARDS_SHOW, 1);
                packet.AddState(NA_UI_ALLIANCE_GUARDS_SHOW, 1);
            }
            else
            {
                packet.AddState(NA_UI_HORDE_GUARDS_SHOW, 0);
                packet.AddState(NA_UI_ALLIANCE_GUARDS_SHOW, 0);
            }

            packet.AddState(NA_UI_GUARDS_MAX, Misc.MaxGuards);
            packet.AddState(NA_UI_GUARDS_LEFT, m_GuardsAlive);

            packet.AddState(NA_MAP_WYVERN_NORTH_NEU_A, (m_WyvernStateNorth & WYVERN_NEU_HORDE) != 0);
            packet.AddState(NA_MAP_WYVERN_NORTH_NEU_A, (m_WyvernStateNorth & WYVERN_NEU_ALLIANCE) != 0);
            packet.AddState(NA_MAP_WYVERN_NORTH_H, (m_WyvernStateNorth & WYVERN_HORDE) != 0);
            packet.AddState(NA_MAP_WYVERN_NORTH_A, (m_WyvernStateNorth & WYVERN_ALLIANCE) != 0);

            packet.AddState(NA_MAP_WYVERN_SOUTH_NEU_H, (m_WyvernStateSouth & WYVERN_NEU_HORDE) != 0);
            packet.AddState(NA_MAP_WYVERN_SOUTH_NEU_A, (m_WyvernStateSouth & WYVERN_NEU_ALLIANCE) != 0);
            packet.AddState(NA_MAP_WYVERN_SOUTH_H, (m_WyvernStateSouth & WYVERN_HORDE) != 0);
            packet.AddState(NA_MAP_WYVERN_SOUTH_A, (m_WyvernStateSouth & WYVERN_ALLIANCE) != 0);

            packet.AddState(NA_MAP_WYVERN_WEST_NEU_H, (m_WyvernStateWest & WYVERN_NEU_HORDE) != 0);
            packet.AddState(NA_MAP_WYVERN_WEST_NEU_A, (m_WyvernStateWest & WYVERN_NEU_ALLIANCE) != 0);
            packet.AddState(NA_MAP_WYVERN_WEST_H, (m_WyvernStateWest & WYVERN_HORDE) != 0);
            packet.AddState(NA_MAP_WYVERN_WEST_A, (m_WyvernStateWest & WYVERN_ALLIANCE) != 0);

            packet.AddState(NA_MAP_WYVERN_EAST_NEU_H, (m_WyvernStateEast & WYVERN_NEU_HORDE) != 0);
            packet.AddState(NA_MAP_WYVERN_EAST_NEU_A, (m_WyvernStateEast & WYVERN_NEU_ALLIANCE) != 0);
            packet.AddState(NA_MAP_WYVERN_EAST_H, (m_WyvernStateEast & WYVERN_HORDE) != 0);
            packet.AddState(NA_MAP_WYVERN_EAST_A, (m_WyvernStateEast & WYVERN_ALLIANCE) != 0);

            packet.AddState(NA_MAP_HALAA_NEUTRAL, (m_HalaaState & HALAA_N) != 0);
            packet.AddState(NA_MAP_HALAA_NEU_A, (m_HalaaState & HALAA_N_A) != 0);
            packet.AddState(NA_MAP_HALAA_NEU_H, (m_HalaaState & HALAA_N_H) != 0);
            packet.AddState(NA_MAP_HALAA_HORDE, (m_HalaaState & HALAA_H) != 0);
            packet.AddState(NA_MAP_HALAA_ALLIANCE, (m_HalaaState & HALAA_A) != 0);
        }

        public override bool HandleCustomSpell(Player player, uint spellId, GameObject go)
        {
            List<uint> nodes = new List<uint>();

            bool retval = false;
            switch (spellId)
            {
                case NA_SPELL_FLY_NORTH:
                    nodes[0] = Misc.FlightPathStartNodes[NA_ROOST_N];
                    nodes[1] = Misc.FlightPathEndNodes[NA_ROOST_N];
                    player.ActivateTaxiPathTo(nodes);
                    player.SetFlag(PlayerFields.Flags, PlayerFlags.InPVP);
                    player.UpdatePvP(true, true);
                    retval = true;
                    break;
                case NA_SPELL_FLY_SOUTH:
                    nodes[0] = Misc.FlightPathStartNodes[NA_ROOST_S];
                    nodes[1] = Misc.FlightPathEndNodes[NA_ROOST_S];
                    player.ActivateTaxiPathTo(nodes);
                    player.SetFlag(PlayerFields.Flags, PlayerFlags.InPVP);
                    player.UpdatePvP(true, true);
                    retval = true;
                    break;
                case NA_SPELL_FLY_WEST:
                    nodes[0] = Misc.FlightPathStartNodes[NA_ROOST_W];
                    nodes[1] = Misc.FlightPathEndNodes[NA_ROOST_W];
                    player.ActivateTaxiPathTo(nodes);
                    player.SetFlag(PlayerFields.Flags, PlayerFlags.InPVP);
                    player.UpdatePvP(true, true);
                    retval = true;
                    break;
                case NA_SPELL_FLY_EAST:
                    nodes[0] = Misc.FlightPathStartNodes[NA_ROOST_E];
                    nodes[1] = Misc.FlightPathEndNodes[NA_ROOST_E];
                    player.ActivateTaxiPathTo(nodes);
                    player.SetFlag(PlayerFields.Flags, PlayerFlags.InPVP);
                    player.UpdatePvP(true, true);
                    retval = true;
                    break;
                default:
                    break;
            }

            if (retval)
            {
                //Adding items
                uint noSpaceForCount = 0;

                // check space and find places
                List<ItemPosCount> dest = new List<ItemPosCount>();

                uint count = 10;
                uint itemid = 24538;
                // bomb id count
                InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemid, count, out noSpaceForCount);
                if (msg != InventoryResult.Ok)                               // convert to possible store amount
                    count -= noSpaceForCount;

                if (count == 0 || dest.Empty())                         // can't add any
                {
                    return true;
                }

                Item item = player.StoreNewItem(dest, itemid, true);
                if (count > 0 && item)
                {
                    player.SendNewItem(item, count, true, false);
                }

                return true;
            }
            return false;
        }

        public override int HandleOpenGo(Player player, GameObject go)
        {
            int retval = base.HandleOpenGo(player, go);
            if (retval >= 0)
            {
                go_type[] gos = null;
                if (m_ControllingFaction == Team.Alliance)
                    gos = Misc.AllianceControlGOs;
                else if (m_ControllingFaction == Team.Horde)
                    gos = Misc.HordeControlGOs;
                else
                    return -1;

                int del = -1;
                int del2 = -1;
                int add = -1;
                int add2 = -1;

                switch (retval)
                {
                    case NA_DESTROYED_ROOST_S:
                        del = NA_DESTROYED_ROOST_S;
                        add = NA_ROOST_S;
                        add2 = NA_BOMB_WAGON_S;
                        if (m_ControllingFaction == Team.Horde)
                            m_WyvernStateSouth = WYVERN_ALLIANCE;
                        else
                            m_WyvernStateSouth = WYVERN_HORDE;
                        UpdateWyvernRoostWorldState(NA_ROOST_S);
                        break;
                    case NA_DESTROYED_ROOST_N:
                        del = NA_DESTROYED_ROOST_N;
                        add = NA_ROOST_N;
                        add2 = NA_BOMB_WAGON_N;
                        if (m_ControllingFaction == Team.Horde)
                            m_WyvernStateNorth = WYVERN_ALLIANCE;
                        else
                            m_WyvernStateNorth = WYVERN_HORDE;
                        UpdateWyvernRoostWorldState(NA_ROOST_N);
                        break;
                    case NA_DESTROYED_ROOST_W:
                        del = NA_DESTROYED_ROOST_W;
                        add = NA_ROOST_W;
                        add2 = NA_BOMB_WAGON_W;
                        if (m_ControllingFaction == Team.Horde)
                            m_WyvernStateWest = WYVERN_ALLIANCE;
                        else
                            m_WyvernStateWest = WYVERN_HORDE;
                        UpdateWyvernRoostWorldState(NA_ROOST_W);
                        break;
                    case NA_DESTROYED_ROOST_E:
                        del = NA_DESTROYED_ROOST_E;
                        add = NA_ROOST_E;
                        add2 = NA_BOMB_WAGON_E;
                        if (m_ControllingFaction == Team.Horde)
                            m_WyvernStateEast = WYVERN_ALLIANCE;
                        else
                            m_WyvernStateEast = WYVERN_HORDE;
                        UpdateWyvernRoostWorldState(NA_ROOST_E);
                        break;
                    case NA_BOMB_WAGON_S:
                        del = NA_BOMB_WAGON_S;
                        del2 = NA_ROOST_S;
                        add = NA_DESTROYED_ROOST_S;
                        if (m_ControllingFaction == Team.Horde)
                            m_WyvernStateSouth = WYVERN_NEU_ALLIANCE;
                        else
                            m_WyvernStateSouth = WYVERN_NEU_HORDE;
                        UpdateWyvernRoostWorldState(NA_ROOST_S);
                        break;
                    case NA_BOMB_WAGON_N:
                        del = NA_BOMB_WAGON_N;
                        del2 = NA_ROOST_N;
                        add = NA_DESTROYED_ROOST_N;
                        if (m_ControllingFaction == Team.Horde)
                            m_WyvernStateNorth = WYVERN_NEU_ALLIANCE;
                        else
                            m_WyvernStateNorth = WYVERN_NEU_HORDE;
                        UpdateWyvernRoostWorldState(NA_ROOST_N);
                        break;
                    case NA_BOMB_WAGON_W:
                        del = NA_BOMB_WAGON_W;
                        del2 = NA_ROOST_W;
                        add = NA_DESTROYED_ROOST_W;
                        if (m_ControllingFaction == Team.Horde)
                            m_WyvernStateWest = WYVERN_NEU_ALLIANCE;
                        else
                            m_WyvernStateWest = WYVERN_NEU_HORDE;
                        UpdateWyvernRoostWorldState(NA_ROOST_W);
                        break;
                    case NA_BOMB_WAGON_E:
                        del = NA_BOMB_WAGON_E;
                        del2 = NA_ROOST_E;
                        add = NA_DESTROYED_ROOST_E;
                        if (m_ControllingFaction == Team.Horde)
                            m_WyvernStateEast = WYVERN_NEU_ALLIANCE;
                        else
                            m_WyvernStateEast = WYVERN_NEU_HORDE;
                        UpdateWyvernRoostWorldState(NA_ROOST_E);
                        break;
                    default:
                        return -1;
                        break;
                }

                if (del > -1)
                    DelObject((uint)del);

                if (del2 > -1)
                    DelObject((uint)del2);

                if (add > -1)
                    AddObject((uint)add, gos[add].entry, gos[add].map, gos[add].x, gos[add].y, gos[add].z, gos[add].o, gos[add].rot0, gos[add].rot1, gos[add].rot2, gos[add].rot3);

                if (add2 > -1)
                    AddObject((uint)add2, gos[add2].entry, gos[add2].map, gos[add2].x, gos[add2].y, gos[add2].z, gos[add2].o, gos[add2].rot0, gos[add2].rot1, gos[add2].rot2, gos[add2].rot3);

                return retval;
            }
            return -1;
        }

        public override bool Update(uint diff)
        {
            // let the controlling faction advance in phase
            bool capturable = false;
            if (m_ControllingFaction == Team.Alliance && m_activePlayers[0].Count > m_activePlayers[1].Count)
                capturable = true;
            else if (m_ControllingFaction == Team.Horde && m_activePlayers[0].Count < m_activePlayers[1].Count)
                capturable = true;

            if (m_GuardCheckTimer < diff)
            {
                m_GuardCheckTimer = Misc.GuardCheckTime;
                uint cnt = GetAliveGuardsCount();
                if (cnt != m_GuardsAlive)
                {
                    m_GuardsAlive = cnt;
                    if (m_GuardsAlive == 0)
                        m_capturable = true;
                    // update the guard count for the players in zone
                    m_PvP.SendUpdateWorldState(NA_UI_GUARDS_LEFT, m_GuardsAlive);
                }
            }
            else m_GuardCheckTimer -= diff;

            if (m_capturable || capturable)
            {
                if (m_RespawnTimer < diff)
                {
                    // if the guards have been killed, then the challenger has one hour to take over halaa.
                    // in case they fail to do it, the guards are respawned, and they have to start again.
                    if (m_ControllingFaction != 0)
                        FactionTakeOver(m_ControllingFaction);
                    m_RespawnTimer = NA_RESPAWN_TIME;
                }
                else m_RespawnTimer -= diff;

                return base.Update(diff);
            }
            return false;
        }

        public override void ChangeState()
        {
            uint artkit = 21;
            switch (m_State)
            {
                case OBJECTIVESTATE_NEUTRAL:
                    m_HalaaState = HALAA_N;
                    break;
                case OBJECTIVESTATE_ALLIANCE:
                    m_HalaaState = HALAA_A;
                    FactionTakeOver(Team.Alliance);
                    artkit = 2;
                    break;
                case OBJECTIVESTATE_HORDE:
                    m_HalaaState = HALAA_H;
                    FactionTakeOver(Team.Horde);
                    artkit = 1;
                    break;
                case OBJECTIVESTATE_NEUTRAL_ALLIANCE_CHALLENGE:
                    m_HalaaState = HALAA_N_A;
                    break;
                case OBJECTIVESTATE_NEUTRAL_HORDE_CHALLENGE:
                    m_HalaaState = HALAA_N_H;
                    break;
                case OBJECTIVESTATE_ALLIANCE_HORDE_CHALLENGE:
                    m_HalaaState = HALAA_N_A;
                    artkit = 2;
                    break;
                case OBJECTIVESTATE_HORDE_ALLIANCE_CHALLENGE:
                    m_HalaaState = HALAA_N_H;
                    artkit = 1;
                    break;
            }

            var bounds = Global.MapMgr.FindMap(530, 0).GetGameObjectBySpawnIdStore().LookupByKey(m_capturePointSpawnId);
            foreach (var itr in bounds)
                itr.SetGoArtKit((byte)artkit);

            UpdateHalaaWorldState();
        }

        void UpdateHalaaWorldState()
        {
            m_PvP.SendUpdateWorldState(NA_MAP_HALAA_NEUTRAL, (m_HalaaState & HALAA_N) != 0);
            m_PvP.SendUpdateWorldState(NA_MAP_HALAA_NEU_A, (m_HalaaState & HALAA_N_A) != 0);
            m_PvP.SendUpdateWorldState(NA_MAP_HALAA_NEU_H, (m_HalaaState & HALAA_N_H) != 0);
            m_PvP.SendUpdateWorldState(NA_MAP_HALAA_HORDE, (m_HalaaState & HALAA_H) != 0);
            m_PvP.SendUpdateWorldState(NA_MAP_HALAA_ALLIANCE, (m_HalaaState & HALAA_A) != 0);
        }

        void UpdateWyvernRoostWorldState(uint roost)
        {
            switch (roost)
            {
                case NA_ROOST_S:
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_SOUTH_NEU_H, (m_WyvernStateSouth & WYVERN_NEU_HORDE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_SOUTH_NEU_A, (m_WyvernStateSouth & WYVERN_NEU_ALLIANCE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_SOUTH_H, (m_WyvernStateSouth & WYVERN_HORDE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_SOUTH_A, (m_WyvernStateSouth & WYVERN_ALLIANCE) != 0);
                    break;
                case NA_ROOST_N:
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_NORTH_NEU_H, (m_WyvernStateNorth & WYVERN_NEU_HORDE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_NORTH_NEU_A, (m_WyvernStateNorth & WYVERN_NEU_ALLIANCE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_NORTH_H, (m_WyvernStateNorth & WYVERN_HORDE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_NORTH_A, (m_WyvernStateNorth & WYVERN_ALLIANCE) != 0);
                    break;
                case NA_ROOST_W:
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_WEST_NEU_H, (m_WyvernStateWest & WYVERN_NEU_HORDE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_WEST_NEU_A, (m_WyvernStateWest & WYVERN_NEU_ALLIANCE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_WEST_H, (m_WyvernStateWest & WYVERN_HORDE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_WEST_A, (m_WyvernStateWest & WYVERN_ALLIANCE) != 0);
                    break;
                case NA_ROOST_E:
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_EAST_NEU_H, (m_WyvernStateEast & WYVERN_NEU_HORDE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_EAST_NEU_A, (m_WyvernStateEast & WYVERN_NEU_ALLIANCE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_EAST_H, (m_WyvernStateEast & WYVERN_HORDE) != 0);
                    m_PvP.SendUpdateWorldState(NA_MAP_WYVERN_EAST_A, (m_WyvernStateEast & WYVERN_ALLIANCE) != 0);
                    break;
            }
        }

        bool m_capturable;

        uint m_GuardsAlive;

        Team m_ControllingFaction;

        uint m_WyvernStateNorth;
        uint m_WyvernStateSouth;
        uint m_WyvernStateEast;
        uint m_WyvernStateWest;

        uint m_HalaaState;

        uint m_RespawnTimer;

        uint m_GuardCheckTimer;
    }

    class OutdoorPvP_nagrand : OutdoorPvPScript
    {
        public OutdoorPvP_nagrand() : base("outdoorpvp_na") { }

        public override OutdoorPvP GetOutdoorPvP()
        {
            return new OutdoorPvPNA();
        }
    }
}
