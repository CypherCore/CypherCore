/*
 * Copyright (C) 2012-2016 CypherCore <http://github.com/CypherCore>
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

using Game.Entities;
using Framework.Constants;
using System.Collections.Generic;
using Game.Network.Packets;
using Game.DataStorage;

namespace Game.BattleGrounds.Zones
{
    class BgEyeofStorm : Battleground
    {
        public BgEyeofStorm()
        {
            m_BuffChange = true;
            BgObjects = new ObjectGuid[ABObjectTypes.Max];
            BgCreatures= new ObjectGuid[EotSCreaturesTypes.Max];
            m_Points_Trigger[EotSPoints.FelReaver] = EotSPointsTrigger.FelReaverBuff;
            m_Points_Trigger[EotSPoints.BloodElf] = EotSPointsTrigger.BloodElfBuff;
            m_Points_Trigger[EotSPoints.DraeneiRuins] = EotSPointsTrigger.DraeneiRuinsBuff;
            m_Points_Trigger[EotSPoints.MageTower] = EotSPointsTrigger.MageTowerBuff;
            m_HonorScoreTics[TeamId.Alliance] = 0;
            m_HonorScoreTics[TeamId.Horde] = 0;
            m_TeamPointsCount[TeamId.Alliance] = 0;
            m_TeamPointsCount[TeamId.Horde] = 0;
            m_FlagKeeper.Clear();
            m_DroppedFlagGUID.Clear();
            m_FlagCapturedBgObjectType = 0;
            m_FlagState = EotSFlagState.OnBase;
            m_FlagsTimer = 0;
            m_TowerCapCheckTimer = 0;
            m_PointAddingTimer = 0;
            m_HonorTics = 0;

            for (byte i = 0; i < EotSPoints.PointsMax; ++i)
            {
                m_PointOwnedByTeam[i] = Team.Other;
                m_PointState[i] = EotSPointState.Uncontrolled;
                m_PointBarStatus[i] = EotSProgressBarConsts.ProgressBarStateMiddle;
            }

            for (byte i = 0; i < EotSPoints.PointsMax + 1; ++i)
                m_PlayersNearPoint[i] = new List<ObjectGuid>();

            for (byte i = 0; i < 2 * EotSPoints.PointsMax; ++i)
                m_CurrentPointPlayersCount[i] = 0;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                m_PointAddingTimer -= (int)diff;
                if (m_PointAddingTimer <= 0)
                {
                    m_PointAddingTimer = EotSMisc.FPointsTickTime;
                    if (m_TeamPointsCount[TeamId.Alliance] > 0)
                        AddPoints(Team.Alliance, EotSMisc.TickPoints[m_TeamPointsCount[TeamId.Alliance] - 1]);
                    if (m_TeamPointsCount[TeamId.Horde] > 0)
                        AddPoints(Team.Horde, EotSMisc.TickPoints[m_TeamPointsCount[TeamId.Horde] - 1]);
                }

                if (m_FlagState == EotSFlagState.WaitRespawn || m_FlagState == EotSFlagState.OnGround)
                {
                    m_FlagsTimer -= (int)diff;

                    if (m_FlagsTimer < 0)
                    {
                        m_FlagsTimer = 0;
                        if (m_FlagState == EotSFlagState.WaitRespawn)
                            RespawnFlag(true);
                        else
                            RespawnFlagAfterDrop();
                    }
                }

                m_TowerCapCheckTimer -= (int)diff;
                if (m_TowerCapCheckTimer <= 0)
                {
                    //check if player joined point
                    /*I used this order of calls, because although we will check if one player is in gameobject's distance 2 times
                      but we can count of players on current point in CheckSomeoneLeftPoint
                    */
                    CheckSomeoneJoinedPo();
                    //check if player left point
                    CheckSomeoneLeftPo();
                    UpdatePointStatuses();
                    m_TowerCapCheckTimer = EotSMisc.FPointsTickTime;
                }
            }
        }

        public override void GetPlayerPositionData(List<BattlegroundPlayerPosition> positions)
        {
            Player player = Global.ObjAccessor.GetPlayer(GetBgMap(), m_FlagKeeper);
            if (player)
            {
                BattlegroundPlayerPosition position = new BattlegroundPlayerPosition();
                position.Guid = player.GetGUID();
                position.Pos.X = player.GetPositionX();
                position.Pos.Y = player.GetPositionY();
                position.IconID = player.GetTeam() == Team.Alliance ? BattlegroundConst.PlayerPositionIconAllianceFlag : BattlegroundConst.PlayerPositionIconHordeFlag;
                position.ArenaSlot = BattlegroundConst.PlayerPositionArenaSlotNone;
                positions.Add(position);
            }
        }

        public override void StartingEventCloseDoors()
        {
            SpawnBGObject(EotSObjectTypes.DoorA, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(EotSObjectTypes.DoorH, BattlegroundConst.RespawnImmediately);

            for (int i = EotSObjectTypes.ABannerFelReaverCenter; i < EotSObjectTypes.Max; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
        }

        public override void StartingEventOpenDoors()
        {
            SpawnBGObject(EotSObjectTypes.DoorA, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(EotSObjectTypes.DoorH, BattlegroundConst.RespawnOneDay);

            for (int i = EotSObjectTypes.NBannerFelReaverLeft; i <= EotSObjectTypes.FlagNetherstorm; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

            for (int i = 0; i < EotSPoints.PointsMax; ++i)
            {
                //randomly spawn buff
                byte buff = (byte)RandomHelper.URand(0, 2);
                SpawnBGObject(EotSObjectTypes.SpeedbuffFelReaver + buff + i * 3, BattlegroundConst.RespawnImmediately);
            }

            // Achievement: Flurry
            StartCriteriaTimer(CriteriaTimedTypes.Event, EotSMisc.EventStartBattle);
        }

        void AddPoints(Team Team, uint Points)
        {
            int team_index = GetTeamIndexByTeamId(Team);
            m_TeamScores[team_index] += Points;
            m_HonorScoreTics[team_index] += Points;
            if (m_HonorScoreTics[team_index] >= m_HonorTics)
            {
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team);
                m_HonorScoreTics[team_index] -= m_HonorTics;
            }
            UpdateTeamScore(team_index);
        }

        void CheckSomeoneJoinedPo()
        {
            GameObject obj = null;
            for (byte i = 0; i < EotSPoints.PointsMax; ++i)
            {
                obj = GetBgMap().GetGameObject(BgObjects[EotSObjectTypes.TowerCapFelReaver + i]);
                if (obj)
                {
                    byte j = 0;
                    while (j < m_PlayersNearPoint[EotSPoints.PointsMax].Count)
                    {
                        Player player = Global.ObjAccessor.FindPlayer(m_PlayersNearPoint[EotSPoints.PointsMax][j]);
                        if (!player)
                        {
                            Log.outError(LogFilter.Battleground, "BattlegroundEY:CheckSomeoneJoinedPoint: Player ({0}) could not be found!", m_PlayersNearPoint[EotSPoints.PointsMax][j].ToString());
                            ++j;
                            continue;
                        }
                        if (player.CanCaptureTowerPoint() && player.IsWithinDistInMap(obj, (float)EotSProgressBarConsts.PointRadius))
                        {
                            //player joined point!
                            //show progress bar
                            player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarPercentGrey, (uint)EotSProgressBarConsts.ProgressBarPercentGrey);
                            player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarStatus, (uint)m_PointBarStatus[i]);
                            player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarShow, (uint)EotSProgressBarConsts.ProgressBarShow);
                            //add player to point
                            m_PlayersNearPoint[i].Add(m_PlayersNearPoint[EotSPoints.PointsMax][j]);
                            //remove player from "free space"
                            m_PlayersNearPoint[EotSPoints.PointsMax].RemoveAt(j);
                        }
                        else
                            ++j;
                    }
                }
            }
        }

        void CheckSomeoneLeftPo()
        {
            //reset current point counts
            for (byte i = 0; i < 2 * EotSPoints.PointsMax; ++i)
                m_CurrentPointPlayersCount[i] = 0;
            GameObject obj = null;
            for (byte i = 0; i < EotSPoints.PointsMax; ++i)
            {
                obj = GetBgMap().GetGameObject(BgObjects[EotSObjectTypes.TowerCapFelReaver + i]);
                if (obj)
                {
                    byte j = 0;
                    while (j < m_PlayersNearPoint[i].Count)
                    {
                        Player player = Global.ObjAccessor.FindPlayer(m_PlayersNearPoint[i][j]);
                        if (!player)
                        {
                            Log.outError(LogFilter.Battleground, "BattlegroundEY:CheckSomeoneLeftPoint Player ({0}) could not be found!", m_PlayersNearPoint[i][j].ToString());
                            //move non-existing players to "free space" - this will cause many errors showing in log, but it is a very important bug
                            m_PlayersNearPoint[EotSPoints.PointsMax].Add(m_PlayersNearPoint[i][j]);
                            m_PlayersNearPoint[i].RemoveAt(j);
                            continue;
                        }
                        if (!player.CanCaptureTowerPoint() || !player.IsWithinDistInMap(obj, (float)EotSProgressBarConsts.PointRadius))
                        //move player out of point (add him to players that are out of points
                        {
                            m_PlayersNearPoint[EotSPoints.PointsMax].Add(m_PlayersNearPoint[i][j]);
                            m_PlayersNearPoint[i].RemoveAt(j);
                            player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarShow, (uint)EotSProgressBarConsts.ProgressBarDontShow);
                        }
                        else
                        {
                            //player is neat flag, so update count:
                            m_CurrentPointPlayersCount[2 * i + GetTeamIndexByTeamId(player.GetTeam())]++;
                            ++j;
                        }
                    }
                }
            }
        }

        void UpdatePointStatuses()
        {
            for (byte point = 0; point < EotSPoints.PointsMax; ++point)
            {
                if (m_PlayersNearPoint[point].Empty())
                    continue;
                //count new point bar status:
                m_PointBarStatus[point] += (m_CurrentPointPlayersCount[2 * point] - m_CurrentPointPlayersCount[2 * point + 1] < (int)EotSProgressBarConsts.PointMaxCapturersCount) ? m_CurrentPointPlayersCount[2 * point] - m_CurrentPointPlayersCount[2 * point + 1] : (int)EotSProgressBarConsts.PointMaxCapturersCount;

                if (m_PointBarStatus[point] > EotSProgressBarConsts.ProgressBarAliControlled)
                    //point is fully alliance's
                    m_PointBarStatus[point] = EotSProgressBarConsts.ProgressBarAliControlled;
                if (m_PointBarStatus[point] < EotSProgressBarConsts.ProgressBarHordeControlled)
                    //point is fully horde's
                    m_PointBarStatus[point] = EotSProgressBarConsts.ProgressBarHordeControlled;

                uint pointOwnerTeamId = 0;
                //find which team should own this point
                if (m_PointBarStatus[point] <= EotSProgressBarConsts.ProgressBarNeutralLow)
                    pointOwnerTeamId = (uint)Team.Horde;
                else if (m_PointBarStatus[point] >= EotSProgressBarConsts.ProgressBarNeutralHigh)
                    pointOwnerTeamId = (uint)Team.Alliance;
                else
                    pointOwnerTeamId = (uint)Team.Other;

                for (byte i = 0; i < m_PlayersNearPoint[point].Count; ++i)
                {
                    Player player = Global.ObjAccessor.FindPlayer(m_PlayersNearPoint[point][i]);
                    if (player)
                    {
                        player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarStatus, (uint)m_PointBarStatus[point]);
                        //if point owner changed we must evoke event!
                        if (pointOwnerTeamId != (uint)m_PointOwnedByTeam[point])
                        {
                            //point was uncontrolled and player is from team which captured point
                            if (m_PointState[point] == EotSPointState.Uncontrolled && (uint)player.GetTeam() == pointOwnerTeamId)
                                EventTeamCapturedPoint(player, point);

                            //point was under control and player isn't from team which controlled it
                            if (m_PointState[point] == EotSPointState.UnderControl && player.GetTeam() != m_PointOwnedByTeam[point])
                                EventTeamLostPoint(player, point);
                        }

                        // @workaround The original AreaTrigger is covered by a bigger one and not triggered on client side.
                        if (point == EotSPoints.FelReaver && m_PointOwnedByTeam[point] == player.GetTeam())
                            if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                                if (player.GetDistance(2044.0f, 1729.729f, 1190.03f) < 3.0f)
                                    EventPlayerCapturedFlag(player, EotSObjectTypes.FlagFelReaver);
                    }
                }
            }
        }

        void UpdateTeamScore(int team)
        {
            uint score = GetTeamScore(team);
            if (score >= EotSScoreIds.MaxTeamScore)
            {
                score = EotSScoreIds.MaxTeamScore;
                if (team == TeamId.Alliance)
                    EndBattleground(Team.Alliance);
                else
                    EndBattleground(Team.Horde);
            }

            if (team == TeamId.Alliance)
                UpdateWorldState(EotSWorldStateIds.AllianceResources, score);
            else
                UpdateWorldState(EotSWorldStateIds.HordeResources, score);
        }

        public override void EndBattleground(Team winner)
        {
            // Win reward
            if (winner == Team.Alliance)
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Alliance);
            if (winner == Team.Horde)
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Horde);
            // Complete map reward
            RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Alliance);
            RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Horde);

            base.EndBattleground(winner);
        }

        void UpdatePointsCount(Team team)
        {
            if (team == Team.Alliance)
                UpdateWorldState(EotSWorldStateIds.AllianceBase, m_TeamPointsCount[TeamId.Alliance]);
            else
                UpdateWorldState(EotSWorldStateIds.HordeBase, m_TeamPointsCount[TeamId.Horde]);
        }

        void UpdatePointsIcons(Team team, int Point)
        {
            //we MUST firstly send 0, after that we can send 1!!!
            if (m_PointState[Point] == EotSPointState.UnderControl)
            {
                UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateControlIndex, 0);
                if (team == Team.Alliance)
                    UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateAllianceControlledIndex, 1);
                else
                    UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateHordeControlledIndex, 1);
            }
            else
            {
                if (team == Team.Alliance)
                    UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateAllianceControlledIndex, 0);
                else
                    UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateHordeControlledIndex, 0);
                UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateControlIndex, 1);
            }
        }

        public override void AddPlayer(Player player)
        {
            base.AddPlayer(player);
            PlayerScores[player.GetGUID()] = new BgEyeOfStormScore(player.GetGUID(), player.GetBGTeam());

            m_PlayersNearPoint[EotSPoints.PointsMax].Add(player.GetGUID());
        }

        public override void RemovePlayer(Player player, ObjectGuid guid, Team team)
        {
            // sometimes flag aura not removed :(
            for (int j = EotSPoints.PointsMax; j >= 0; --j)
            {
                for (int i = 0; i < m_PlayersNearPoint[j].Count; ++i)
                    if (m_PlayersNearPoint[j][i] == guid)
                        m_PlayersNearPoint[j].RemoveAt(i);
            }
            if (IsFlagPickedup())
            {
                if (m_FlagKeeper == guid)
                {
                    if (player)
                        EventPlayerDroppedFlag(player);
                    else
                    {
                        SetFlagPicker(ObjectGuid.Empty);
                        RespawnFlag(true);
                    }
                }
            }
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            if (!player.IsAlive())                                  //hack code, must be removed later
                return;

            switch (trigger)
            {
                case 4530: // Horde Start
                case 4531: // Alliance Start
                    if (GetStatus() == BattlegroundStatus.WaitJoin && !entered)
                        TeleportPlayerToExploitLocation(player);
                    break;
                case EotSPointsTrigger.BloodElfPoint:
                    if (m_PointState[EotSPoints.BloodElf] == EotSPointState.UnderControl && m_PointOwnedByTeam[EotSPoints.BloodElf] == player.GetTeam())
                        if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, EotSObjectTypes.FlagBloodElf);
                    break;
                case EotSPointsTrigger.FelReaverPoint:
                    if (m_PointState[EotSPoints.FelReaver] == EotSPointState.UnderControl && m_PointOwnedByTeam[EotSPoints.FelReaver] == player.GetTeam())
                        if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, EotSObjectTypes.FlagFelReaver);
                    break;
                case EotSPointsTrigger.MageTowerPoint:
                    if (m_PointState[EotSPoints.MageTower] == EotSPointState.UnderControl && m_PointOwnedByTeam[EotSPoints.MageTower] == player.GetTeam())
                        if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, EotSObjectTypes.FlagMageTower);
                    break;
                case EotSPointsTrigger.DraeneiRuinsPoint:
                    if (m_PointState[EotSPoints.DraeneiRuins] == EotSPointState.UnderControl && m_PointOwnedByTeam[EotSPoints.DraeneiRuins] == player.GetTeam())
                        if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, EotSObjectTypes.FlagDraeneiRuins);
                    break;
                case 4512:
                case 4515:
                case 4517:
                case 4519:
                case 4568:
                case 4569:
                case 4570:
                case 4571:
                case 5866:
                    break;
                default:
                    base.HandleAreaTrigger(player, trigger, entered);
                    break;
            }
        }

        public override bool SetupBattleground()
        {
            // doors
            if (!AddObject(EotSObjectTypes.DoorA, EotSObjectIds.ADoor, 2527.6f, 1596.91f, 1262.13f, -3.12414f, -0.173642f, -0.001515f, 0.98477f, -0.008594f, BattlegroundConst.RespawnImmediately)
                || !AddObject(EotSObjectTypes.DoorH, EotSObjectIds.HDoor, 1803.21f, 1539.49f, 1261.09f, 3.14159f, 0.173648f, 0, 0.984808f, 0, BattlegroundConst.RespawnImmediately)
                // banners (alliance)
                || !AddObject(EotSObjectTypes.ABannerFelReaverCenter, EotSObjectIds.ABanner, 2057.46f, 1735.07f, 1187.91f, -0.925024f, 0, 0, 0.446198f, -0.894934f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerFelReaverLeft, EotSObjectIds.ABanner, 2032.25f, 1729.53f, 1190.33f, 1.8675f, 0, 0, 0.803857f, 0.594823f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerFelReaverRight, EotSObjectIds.ABanner, 2092.35f, 1775.46f, 1187.08f, -0.401426f, 0, 0, 0.199368f, -0.979925f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerBloodElfCenter, EotSObjectIds.ABanner, 2047.19f, 1349.19f, 1189.0f, -1.62316f, 0, 0, 0.725374f, -0.688354f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerBloodElfLeft, EotSObjectIds.ABanner, 2074.32f, 1385.78f, 1194.72f, 0.488692f, 0, 0, 0.241922f, 0.970296f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerBloodElfRight, EotSObjectIds.ABanner, 2025.13f, 1386.12f, 1192.74f, 2.3911f, 0, 0, 0.930418f, 0.366501f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerDraeneiRuinsCenter, EotSObjectIds.ABanner, 2276.8f, 1400.41f, 1196.33f, 2.44346f, 0, 0, 0.939693f, 0.34202f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerDraeneiRuinsLeft, EotSObjectIds.ABanner, 2305.78f, 1404.56f, 1199.38f, 1.74533f, 0, 0, 0.766044f, 0.642788f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerDraeneiRuinsRight, EotSObjectIds.ABanner, 2245.4f, 1366.41f, 1195.28f, 2.21657f, 0, 0, 0.894934f, 0.446198f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerMageTowerCenter, EotSObjectIds.ABanner, 2270.84f, 1784.08f, 1186.76f, 2.42601f, 0, 0, 0.936672f, 0.350207f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerMageTowerLeft, EotSObjectIds.ABanner, 2269.13f, 1737.7f, 1186.66f, 0.994838f, 0, 0, 0.477159f, 0.878817f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.ABannerMageTowerRight, EotSObjectIds.ABanner, 2300.86f, 1741.25f, 1187.7f, -0.785398f, 0, 0, 0.382683f, -0.92388f, BattlegroundConst.RespawnOneDay)
                // banners (horde)
                || !AddObject(EotSObjectTypes.HBannerFelReaverCenter, EotSObjectIds.HBanner, 2057.46f, 1735.07f, 1187.91f, -0.925024f, 0, 0, 0.446198f, -0.894934f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerFelReaverLeft, EotSObjectIds.HBanner, 2032.25f, 1729.53f, 1190.33f, 1.8675f, 0, 0, 0.803857f, 0.594823f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerFelReaverRight, EotSObjectIds.HBanner, 2092.35f, 1775.46f, 1187.08f, -0.401426f, 0, 0, 0.199368f, -0.979925f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerBloodElfCenter, EotSObjectIds.HBanner, 2047.19f, 1349.19f, 1189.0f, -1.62316f, 0, 0, 0.725374f, -0.688354f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerBloodElfLeft, EotSObjectIds.HBanner, 2074.32f, 1385.78f, 1194.72f, 0.488692f, 0, 0, 0.241922f, 0.970296f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerBloodElfRight, EotSObjectIds.HBanner, 2025.13f, 1386.12f, 1192.74f, 2.3911f, 0, 0, 0.930418f, 0.366501f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerDraeneiRuinsCenter, EotSObjectIds.HBanner, 2276.8f, 1400.41f, 1196.33f, 2.44346f, 0, 0, 0.939693f, 0.34202f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerDraeneiRuinsLeft, EotSObjectIds.HBanner, 2305.78f, 1404.56f, 1199.38f, 1.74533f, 0, 0, 0.766044f, 0.642788f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerDraeneiRuinsRight, EotSObjectIds.HBanner, 2245.4f, 1366.41f, 1195.28f, 2.21657f, 0, 0, 0.894934f, 0.446198f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerMageTowerCenter, EotSObjectIds.HBanner, 2270.84f, 1784.08f, 1186.76f, 2.42601f, 0, 0, 0.936672f, 0.350207f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerMageTowerLeft, EotSObjectIds.HBanner, 2269.13f, 1737.7f, 1186.66f, 0.994838f, 0, 0, 0.477159f, 0.878817f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.HBannerMageTowerRight, EotSObjectIds.HBanner, 2300.86f, 1741.25f, 1187.7f, -0.785398f, 0, 0, 0.382683f, -0.92388f, BattlegroundConst.RespawnOneDay)
                // banners (natural)
                || !AddObject(EotSObjectTypes.NBannerFelReaverCenter, EotSObjectIds.NBanner, 2057.46f, 1735.07f, 1187.91f, -0.925024f, 0, 0, 0.446198f, -0.894934f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerFelReaverLeft, EotSObjectIds.NBanner, 2032.25f, 1729.53f, 1190.33f, 1.8675f, 0, 0, 0.803857f, 0.594823f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerFelReaverRight, EotSObjectIds.NBanner, 2092.35f, 1775.46f, 1187.08f, -0.401426f, 0, 0, 0.199368f, -0.979925f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerBloodElfCenter, EotSObjectIds.NBanner, 2047.19f, 1349.19f, 1189.0f, -1.62316f, 0, 0, 0.725374f, -0.688354f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerBloodElfLeft, EotSObjectIds.NBanner, 2074.32f, 1385.78f, 1194.72f, 0.488692f, 0, 0, 0.241922f, 0.970296f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerBloodElfRight, EotSObjectIds.NBanner, 2025.13f, 1386.12f, 1192.74f, 2.3911f, 0, 0, 0.930418f, 0.366501f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerDraeneiRuinsCenter, EotSObjectIds.NBanner, 2276.8f, 1400.41f, 1196.33f, 2.44346f, 0, 0, 0.939693f, 0.34202f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerDraeneiRuinsLeft, EotSObjectIds.NBanner, 2305.78f, 1404.56f, 1199.38f, 1.74533f, 0, 0, 0.766044f, 0.642788f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerDraeneiRuinsRight, EotSObjectIds.NBanner, 2245.4f, 1366.41f, 1195.28f, 2.21657f, 0, 0, 0.894934f, 0.446198f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerMageTowerCenter, EotSObjectIds.NBanner, 2270.84f, 1784.08f, 1186.76f, 2.42601f, 0, 0, 0.936672f, 0.350207f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerMageTowerLeft, EotSObjectIds.NBanner, 2269.13f, 1737.7f, 1186.66f, 0.994838f, 0, 0, 0.477159f, 0.878817f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.NBannerMageTowerRight, EotSObjectIds.NBanner, 2300.86f, 1741.25f, 1187.7f, -0.785398f, 0, 0, 0.382683f, -0.92388f, BattlegroundConst.RespawnOneDay)
                // flags
                || !AddObject(EotSObjectTypes.FlagNetherstorm, EotSObjectIds.Flag2, 2174.782227f, 1569.054688f, 1160.361938f, -1.448624f, 0, 0, 0.662620f, -0.748956f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.FlagFelReaver, EotSObjectIds.Flag1, 2044.28f, 1729.68f, 1189.96f, -0.017453f, 0, 0, 0.008727f, -0.999962f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.FlagBloodElf, EotSObjectIds.Flag1, 2048.83f, 1393.65f, 1194.49f, 0.20944f, 0, 0, 0.104528f, 0.994522f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.FlagDraeneiRuins, EotSObjectIds.Flag1, 2286.56f, 1402.36f, 1197.11f, 3.72381f, 0, 0, 0.957926f, -0.287016f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.FlagMageTower, EotSObjectIds.Flag1, 2284.48f, 1731.23f, 1189.99f, 2.89725f, 0, 0, 0.992546f, 0.121869f, BattlegroundConst.RespawnOneDay)
                // tower cap
                || !AddObject(EotSObjectTypes.TowerCapFelReaver, EotSObjectIds.FrTowerCap, 2024.600708f, 1742.819580f, 1195.157715f, 2.443461f, 0, 0, 0.939693f, 0.342020f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.TowerCapBloodElf, EotSObjectIds.BeTowerCap, 2050.493164f, 1372.235962f, 1194.563477f, 1.710423f, 0, 0, 0.754710f, 0.656059f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.TowerCapDraeneiRuins, EotSObjectIds.DrTowerCap, 2301.010498f, 1386.931641f, 1197.183472f, 1.570796f, 0, 0, 0.707107f, 0.707107f, BattlegroundConst.RespawnOneDay)
                || !AddObject(EotSObjectTypes.TowerCapMageTower, EotSObjectIds.HuTowerCap, 2282.121582f, 1760.006958f, 1189.707153f, 1.919862f, 0, 0, 0.819152f, 0.573576f, BattlegroundConst.RespawnOneDay)
        )
            {
                Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn some objects. The battleground was not created.");
                return false;
            }

            //buffs
            for (int i = 0; i < EotSPoints.PointsMax; ++i)
            {
                AreaTriggerRecord at = CliDB.AreaTriggerStorage.LookupByKey(m_Points_Trigger[i]);
                if (at == null)
                {
                    Log.outError(LogFilter.Battleground, "BattlegroundEY: Unknown trigger: {0}", m_Points_Trigger[i]);
                    continue;
                }
                if (!AddObject(EotSObjectTypes.SpeedbuffFelReaver + i * 3, Buff_Entries[0], at.Pos.X, at.Pos.Y, at.Pos.Z, 0.907571f, 0, 0, 0.438371f, 0.898794f, BattlegroundConst.RespawnOneDay)
                    || !AddObject(EotSObjectTypes.SpeedbuffFelReaver + i * 3 + 1, Buff_Entries[1], at.Pos.X, at.Pos.Y, at.Pos.Z, 0.907571f, 0, 0, 0.438371f, 0.898794f, BattlegroundConst.RespawnOneDay)
                    || !AddObject(EotSObjectTypes.SpeedbuffFelReaver + i * 3 + 2, Buff_Entries[2], at.Pos.X, at.Pos.Y, at.Pos.Z, 0.907571f, 0, 0, 0.438371f, 0.898794f, BattlegroundConst.RespawnOneDay)
        )
                    Log.outError(LogFilter.Battleground, "BattlegroundEY: Could not spawn Speedbuff Fel Reaver.");
            }

            WorldSafeLocsRecord sg = CliDB.WorldSafeLocsStorage.LookupByKey(EotSGaveyardIds.MainAlliance);
            if (sg == null || !AddSpiritGuide(EotSCreaturesTypes.SpiritMainAlliance, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.124139f, TeamId.Alliance))
            {
                Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");
                return false;
            }

            sg = CliDB.WorldSafeLocsStorage.LookupByKey(EotSGaveyardIds.MainHorde);
            if (sg == null || !AddSpiritGuide(EotSCreaturesTypes.SpiritMainHorde, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.193953f, TeamId.Horde))
            {
                Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");
                return false;
            }

            return true;
        }

        public override void Reset()
        {
            //call parent's class reset
            base.Reset();

            m_TeamScores[TeamId.Alliance] = 0;
            m_TeamScores[TeamId.Horde] = 0;
            m_TeamPointsCount[TeamId.Alliance] = 0;
            m_TeamPointsCount[TeamId.Horde] = 0;
            m_HonorScoreTics[TeamId.Alliance] = 0;
            m_HonorScoreTics[TeamId.Horde] = 0;
            m_FlagState = EotSFlagState.OnBase;
            m_FlagCapturedBgObjectType = 0;
            m_FlagKeeper.Clear();
            m_DroppedFlagGUID.Clear();
            m_PointAddingTimer = 0;
            m_TowerCapCheckTimer = 0;
            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
            m_HonorTics = (isBGWeekend) ? EotSMisc.EYWeekendHonorTicks : EotSMisc.NotEYWeekendHonorTicks;

            for (byte i = 0; i < EotSPoints.PointsMax; ++i)
            {
                m_PointOwnedByTeam[i] = Team.Other;
                m_PointState[i] = EotSPointState.Uncontrolled;
                m_PointBarStatus[i] = EotSProgressBarConsts.ProgressBarStateMiddle;
                m_PlayersNearPoint[i].Clear();
            }
            m_PlayersNearPoint[EotSPoints.PlayersOutOfPoints].Clear();
        }

        void RespawnFlag(bool send_message)
        {
            if (m_FlagCapturedBgObjectType > 0)
                SpawnBGObject((int)m_FlagCapturedBgObjectType, BattlegroundConst.RespawnOneDay);

            m_FlagCapturedBgObjectType = 0;
            m_FlagState = EotSFlagState.OnBase;
            SpawnBGObject(EotSObjectTypes.FlagNetherstorm, BattlegroundConst.RespawnImmediately);

            if (send_message)
            {
                SendBroadcastText(EotSBroadcastTexts.FlagReset, ChatMsg.BgSystemNeutral);
                PlaySoundToAll(EotSSoundIds.FlagReset);             // flags respawned sound...
            }

            UpdateWorldState(EotSWorldStateIds.NetherstormFlag, 1);
        }

        void RespawnFlagAfterDrop()
        {
            RespawnFlag(true);

            GameObject obj = GetBgMap().GetGameObject(GetDroppedFlagGUID());
            if (obj)
                obj.Delete();
            else
                Log.outError(LogFilter.Battleground, "BattlegroundEY: Unknown dropped flag ({0}).", GetDroppedFlagGUID().ToString());

            SetDroppedFlagGUID(ObjectGuid.Empty);
        }

        public override void HandleKillPlayer(Player player, Player killer)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            base.HandleKillPlayer(player, killer);
            EventPlayerDroppedFlag(player);
        }

        public override void EventPlayerDroppedFlag(Player player)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
            {
                // if not running, do not cast things at the dropper player, neither send unnecessary messages
                // just take off the aura
                if (IsFlagPickedup() && GetFlagPickerGUID() == player.GetGUID())
                {
                    SetFlagPicker(ObjectGuid.Empty);
                    player.RemoveAurasDueToSpell(EotSMisc.SpellNetherstormFlag);
                }
                return;
            }

            if (!IsFlagPickedup())
                return;

            if (GetFlagPickerGUID() != player.GetGUID())
                return;

            SetFlagPicker(ObjectGuid.Empty);
            player.RemoveAurasDueToSpell(EotSMisc.SpellNetherstormFlag);
            m_FlagState = EotSFlagState.OnGround;
            m_FlagsTimer = EotSMisc.FlagRespawnTime;
            player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedFlag, true);
            player.CastSpell(player, EotSMisc.SpellPlayerDroppedFlag, true);
            //this does not work correctly :((it should remove flag carrier name)
            UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateHorde, (uint)EotSFlagState.WaitRespawn);
            UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateAlliance, (uint)EotSFlagState.WaitRespawn);

            if (player.GetTeam() == Team.Alliance)
                SendBroadcastText(EotSBroadcastTexts.FlagDropped, ChatMsg.BgSystemAlliance, null);
            else
                SendBroadcastText(EotSBroadcastTexts.FlagDropped, ChatMsg.BgSystemHorde, null);
        }

        public override void EventPlayerClickedOnFlag(Player player, GameObject target_obj)
        {
            if (GetStatus() != BattlegroundStatus.InProgress || IsFlagPickedup() || !player.IsWithinDistInMap(target_obj, 10))
                return;

            if (player.GetTeam() == Team.Alliance)
            {
                UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateAlliance, (uint)EotSFlagState.OnPlayer);
                PlaySoundToAll(EotSSoundIds.FlagPickedUpAlliance);
            }
            else
            {
                UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateHorde, (uint)EotSFlagState.OnPlayer);
                PlaySoundToAll(EotSSoundIds.FlagPickedUpHorde);
            }

            if (m_FlagState == EotSFlagState.OnBase)
                UpdateWorldState(EotSWorldStateIds.NetherstormFlag, 0);
            m_FlagState = EotSFlagState.OnPlayer;

            SpawnBGObject(EotSObjectTypes.FlagNetherstorm, BattlegroundConst.RespawnOneDay);
            SetFlagPicker(player.GetGUID());
            //get flag aura on player
            player.CastSpell(player, EotSMisc.SpellNetherstormFlag, true);
            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterPvpCombat);

            if (player.GetTeam() == Team.Alliance)
                SendBroadcastText(EotSBroadcastTexts.TakenFlag, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(EotSBroadcastTexts.TakenFlag, ChatMsg.BgSystemHorde, player);
        }

        void EventTeamLostPoint(Player player, int Point)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            //Natural point
            Team Team = m_PointOwnedByTeam[Point];

            if (Team == 0)
                return;

            if (Team == Team.Alliance)
            {
                m_TeamPointsCount[TeamId.Alliance]--;
                SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeAlliance, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeAlliance + 1, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeAlliance + 2, BattlegroundConst.RespawnOneDay);
            }
            else
            {
                m_TeamPointsCount[TeamId.Horde]--;
                SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeHorde, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeHorde + 1, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeHorde + 2, BattlegroundConst.RespawnOneDay);
            }

            SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].SpawnNeutralObjectType, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].SpawnNeutralObjectType + 1, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].SpawnNeutralObjectType + 2, BattlegroundConst.RespawnImmediately);

            //buff isn't despawned

            m_PointOwnedByTeam[Point] = Team.Other;
            m_PointState[Point] = EotSPointState.NoOwner;

            if (Team == Team.Alliance)
                SendBroadcastText(EotSMisc.m_LosingPointTypes[Point].MessageIdAlliance, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(EotSMisc.m_LosingPointTypes[Point].MessageIdHorde, ChatMsg.BgSystemHorde, player);

            UpdatePointsIcons(Team, Point);
            UpdatePointsCount(Team);

            //remove bonus honor aura trigger creature when node is lost
            if (Point < EotSPoints.PointsMax)
                DelCreature(Point + 6);//null checks are in DelCreature! 0-5 spirit guides
        }

        void EventTeamCapturedPoint(Player player, int Point)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            Team Team = player.GetTeam();

            SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].DespawnNeutralObjectType, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].DespawnNeutralObjectType + 1, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].DespawnNeutralObjectType + 2, BattlegroundConst.RespawnOneDay);

            if (Team == Team.Alliance)
            {
                m_TeamPointsCount[TeamId.Alliance]++;
                SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeAlliance, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeAlliance + 1, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeAlliance + 2, BattlegroundConst.RespawnImmediately);
            }
            else
            {
                m_TeamPointsCount[TeamId.Horde]++;
                SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeHorde, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeHorde + 1, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeHorde + 2, BattlegroundConst.RespawnImmediately);
            }

            //buff isn't respawned

            m_PointOwnedByTeam[Point] = Team;
            m_PointState[Point] = EotSPointState.UnderControl;

            if (Team == Team.Alliance)
                SendBroadcastText(EotSMisc.m_CapturingPointTypes[Point].MessageIdAlliance, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(EotSMisc.m_CapturingPointTypes[Point].MessageIdHorde, ChatMsg.BgSystemHorde, player);

            if (!BgCreatures[Point].IsEmpty())
                DelCreature(Point);

            WorldSafeLocsRecord sg = CliDB.WorldSafeLocsStorage.LookupByKey(EotSMisc.m_CapturingPointTypes[Point].GraveYardId);
            if (sg == null || !AddSpiritGuide(Point, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.124139f, GetTeamIndexByTeamId(Team)))
                Log.outError(LogFilter.Battleground, "BatteGroundEY: Failed to spawn spirit guide. point: {0}, team: {1}, graveyard_id: {2}",
                    Point, Team, EotSMisc.m_CapturingPointTypes[Point].GraveYardId);

            //    SpawnBGCreature(Point, RESPAWN_IMMEDIATELY);

            UpdatePointsIcons(Team, Point);
            UpdatePointsCount(Team);

            if (Point >= EotSPoints.PointsMax)
                return;

            Creature trigger = GetBGCreature(Point + 6);//0-5 spirit guides
            if (!trigger)
                trigger = AddCreature(SharedConst.WorldTrigger, Point + 6, EotSMisc.TriggerPositions[Point], GetTeamIndexByTeamId(Team));

            //add bonus honor aura trigger creature when node is accupied
            //cast bonus aura (+50% honor in 25yards)
            //aura should only apply to players who have accupied the node, set correct faction for trigger
            if (trigger)
            {
                trigger.SetFaction(Team == Team.Alliance ? 84u : 83);
                trigger.CastSpell(trigger, BattlegroundConst.SpellHonorableDefender25y, false);
            }
        }

        void EventPlayerCapturedFlag(Player player, uint BgObjectType)
        {
            if (GetStatus() != BattlegroundStatus.InProgress || GetFlagPickerGUID() != player.GetGUID())
                return;

            SetFlagPicker(ObjectGuid.Empty);
            m_FlagState = EotSFlagState.WaitRespawn;
            player.RemoveAurasDueToSpell(EotSMisc.SpellNetherstormFlag);

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterPvpCombat);

            if (player.GetTeam() == Team.Alliance)
            {
                SendBroadcastText(EotSBroadcastTexts.AllianceCapturedFlag, ChatMsg.BgSystemAlliance, player);
                PlaySoundToAll(EotSSoundIds.FlagCapturedAlliance);
            }
            else
            {
                SendBroadcastText(EotSBroadcastTexts.HordeCapturedFlag, ChatMsg.BgSystemHorde, player);
                PlaySoundToAll(EotSSoundIds.FlagCapturedHorde);
            }

            SpawnBGObject((int)BgObjectType, BattlegroundConst.RespawnImmediately);

            m_FlagsTimer = EotSMisc.FlagRespawnTime;
            m_FlagCapturedBgObjectType = BgObjectType;

            int team_id = player.GetTeam() == Team.Alliance ? TeamId.Alliance : TeamId.Horde;
            if (m_TeamPointsCount[team_id] > 0)
                AddPoints(player.GetTeam(), EotSMisc.FlagPoints[m_TeamPointsCount[team_id] - 1]);

            UpdatePlayerScore(player, ScoreType.FlagCaptures, 1);
        }

        public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
        {
            if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
                return false;

            switch (type)
            {
                case ScoreType.FlagCaptures:
                    player.UpdateCriteria(CriteriaTypes.BgObjectiveCapture, EotSMisc.ObjectiveCaptureFlag);
                    break;
                default:
                    break;
            }
            return true;
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(EotSWorldStateIds.HordeBase, (int)m_TeamPointsCount[TeamId.Horde]);
            packet.AddState(EotSWorldStateIds.AllianceBase, (int)m_TeamPointsCount[TeamId.Alliance]);
            packet.AddState(0xAB6, 0x0);
            packet.AddState(0xAB5, 0x0);
            packet.AddState(0xAB4, 0x0);
            packet.AddState(0xAB3, 0x0);
            packet.AddState(0xAB2, 0x0);
            packet.AddState(0xAB1, 0x0);
            packet.AddState(0xAB0, 0x0);
            packet.AddState(0xAAF, 0x0);

            packet.AddState((EotSWorldStateIds.DraeneiRuinsHordeControl), (m_PointOwnedByTeam[EotSPoints.DraeneiRuins] == Team.Horde && m_PointState[EotSPoints.DraeneiRuins] == EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.DraeneiRuinsAllianceControl), (m_PointOwnedByTeam[EotSPoints.DraeneiRuins] == Team.Alliance && m_PointState[EotSPoints.DraeneiRuins] == EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.DraeneiRuinsUncontrol), (m_PointState[EotSPoints.DraeneiRuins] != EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.MageTowerAllianceControl), (m_PointOwnedByTeam[EotSPoints.MageTower] == Team.Alliance && m_PointState[EotSPoints.MageTower] == EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.MageTowerHordeControl), (m_PointOwnedByTeam[EotSPoints.MageTower] == Team.Horde && m_PointState[EotSPoints.MageTower] == EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.MageTowerUncontrol), (m_PointState[EotSPoints.MageTower] != EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.FelReaverHordeControl), (m_PointOwnedByTeam[EotSPoints.FelReaver] == Team.Horde && m_PointState[EotSPoints.FelReaver] == EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.FelReaverAllianceControl), (m_PointOwnedByTeam[EotSPoints.FelReaver] == Team.Alliance && m_PointState[EotSPoints.FelReaver] == EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.FelReaverUncontrol), (m_PointState[EotSPoints.FelReaver] != EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.BloodElfHordeControl), (m_PointOwnedByTeam[EotSPoints.BloodElf] == Team.Horde && m_PointState[EotSPoints.BloodElf] == EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.BloodElfAllianceControl), (m_PointOwnedByTeam[EotSPoints.BloodElf] == Team.Alliance && m_PointState[EotSPoints.BloodElf] == EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.BloodElfUncontrol), (m_PointState[EotSPoints.BloodElf] != EotSPointState.UnderControl));
            packet.AddState((EotSWorldStateIds.NetherstormFlag), (m_FlagState == EotSFlagState.OnBase));

            packet.AddState(0xAD2, 0x1);
            packet.AddState(0xAD1, 0x1);

            packet.AddState(0xABE, (int)GetTeamScore(TeamId.Horde));
            packet.AddState(0xABD, (int)GetTeamScore(TeamId.Alliance));

            packet.AddState(0xA05, 0x8E);
            packet.AddState(0xAA0, 0x0);
            packet.AddState(0xA9F, 0x0);
            packet.AddState(0xA9E, 0x0);
            packet.AddState(0xC0D, 0x17B);
        }

        public override WorldSafeLocsRecord GetClosestGraveYard(Player player)
        {
            uint g_id = 0;

            switch (player.GetTeam())
            {
                case Team.Alliance:
                    g_id = EotSGaveyardIds.MainAlliance;
                    break;
                case Team.Horde:
                    g_id = EotSGaveyardIds.MainHorde;
                    break;
                default: return null;
            }

            WorldSafeLocsRecord entry = null;
            WorldSafeLocsRecord nearestEntry = null;
            entry = CliDB.WorldSafeLocsStorage.LookupByKey(g_id);
            nearestEntry = entry;

            if (entry == null)
            {
                Log.outError(LogFilter.Battleground, "BattlegroundEY: The main team graveyard could not be found. The graveyard system will not be operational!");
                return null;
            }

            float plr_x = player.GetPositionX();
            float plr_y = player.GetPositionY();
            float plr_z = player.GetPositionZ();

            float distance = (entry.Loc.X - plr_x) * (entry.Loc.X - plr_x) + (entry.Loc.Y - plr_y) * (entry.Loc.Y - plr_y) + (entry.Loc.Z - plr_z) * (entry.Loc.Z - plr_z);
            float nearestDistance = distance;

            for (byte i = 0; i < EotSPoints.PointsMax; ++i)
            {
                if (m_PointOwnedByTeam[i] == player.GetTeam() && m_PointState[i] == EotSPointState.UnderControl)
                {
                    entry = CliDB.WorldSafeLocsStorage.LookupByKey(EotSMisc.m_CapturingPointTypes[i].GraveYardId);
                    if (entry == null)
                        Log.outError(LogFilter.Battleground, "BattlegroundEY: Graveyard {0} could not be found.", EotSMisc.m_CapturingPointTypes[i].GraveYardId);
                    else
                    {
                        distance = (entry.Loc.X - plr_x) * (entry.Loc.X - plr_x) + (entry.Loc.Y - plr_y) * (entry.Loc.Y - plr_y) + (entry.Loc.Z - plr_z) * (entry.Loc.Z - plr_z);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestEntry = entry;
                        }
                    }
                }
            }

            return nearestEntry;
        }

        public override WorldSafeLocsRecord GetExploitTeleportLocation(Team team)
        {
            return CliDB.WorldSafeLocsStorage.LookupByKey(team == Team.Alliance ? EotSMisc.ExploitTeleportLocationAlliance : EotSMisc.ExploitTeleportLocationHorde);
        }

        public override bool IsAllNodesControlledByTeam(Team team)
        {
            uint count = 0;
            for (int i = 0; i < EotSPoints.PointsMax; ++i)
                if (m_PointOwnedByTeam[i] == team && m_PointState[i] == EotSPointState.UnderControl)
                    ++count;

            return count == EotSPoints.PointsMax;
        }

        public override Team GetPrematureWinner()
        {
            if (GetTeamScore(TeamId.Alliance) > GetTeamScore(TeamId.Horde))
                return Team.Alliance;
            else if (GetTeamScore(TeamId.Horde) > GetTeamScore(TeamId.Alliance))
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        public override ObjectGuid GetFlagPickerGUID(int team = -1) { return m_FlagKeeper; }
        void SetFlagPicker(ObjectGuid guid) { m_FlagKeeper = guid; }
        bool IsFlagPickedup() { return !m_FlagKeeper.IsEmpty(); }
        byte GetFlagState() { return (byte)m_FlagState; }

        public override void SetDroppedFlagGUID(ObjectGuid guid, int TeamID = -1) { m_DroppedFlagGUID = guid; }
        ObjectGuid GetDroppedFlagGUID() { return m_DroppedFlagGUID; }

        void RemovePo(Team TeamID, uint Points = 1) { m_TeamScores[GetTeamIndexByTeamId(TeamID)] -= Points; }
        void SetTeamPo(Team TeamID, uint Points = 0) { m_TeamScores[GetTeamIndexByTeamId(TeamID)] = Points; }

        uint[] m_HonorScoreTics = new uint[2];
        uint[] m_TeamPointsCount = new uint[2];

        uint[] m_Points_Trigger = new uint[EotSPoints.PointsMax];

        ObjectGuid m_FlagKeeper;                                // keepers guid
        ObjectGuid m_DroppedFlagGUID;
        uint m_FlagCapturedBgObjectType;                  // type that should be despawned when flag is captured
        EotSFlagState m_FlagState;                                  // for checking flag state
        int m_FlagsTimer;
        int m_TowerCapCheckTimer;

        Team[] m_PointOwnedByTeam = new Team[EotSPoints.PointsMax];
        EotSPointState[] m_PointState = new EotSPointState[EotSPoints.PointsMax];
        EotSProgressBarConsts[] m_PointBarStatus = new EotSProgressBarConsts[EotSPoints.PointsMax];
        List<ObjectGuid>[] m_PlayersNearPoint = new List<ObjectGuid>[EotSPoints.PointsMax + 1];
        byte[] m_CurrentPointPlayersCount = new byte[2 * EotSPoints.PointsMax];

        int m_PointAddingTimer;
        uint m_HonorTics;
    }

    class BgEyeOfStormScore : BattlegroundScore
    {
        public BgEyeOfStormScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team) { }

        public override void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.FlagCaptures:   // Flags captured
                    FlagCaptures += value;
                    break;
                default:
                    base.UpdateScore(type, value);
                    break;
            }
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPLogData.PVPMatchPlayerStatistics playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            playerData.Stats.Add(new PVPLogData.PVPMatchPlayerPVPStat((int)EotSMisc.ObjectiveCaptureFlag, FlagCaptures));
        }

        public override uint GetAttr1() { return FlagCaptures; }

        uint FlagCaptures;
    }

    struct BattlegroundEYPointIconsStruct
    {
        public BattlegroundEYPointIconsStruct(uint _WorldStateControlIndex, uint _WorldStateAllianceControlledIndex, uint _WorldStateHordeControlledIndex)
        {
            WorldStateControlIndex = _WorldStateControlIndex;
            WorldStateAllianceControlledIndex = _WorldStateAllianceControlledIndex;
            WorldStateHordeControlledIndex = _WorldStateHordeControlledIndex;
        }

        public uint WorldStateControlIndex;
        public uint WorldStateAllianceControlledIndex;
        public uint WorldStateHordeControlledIndex;
    }

    struct BattlegroundEYLosingPointStruct
    {
        public BattlegroundEYLosingPointStruct(int _SpawnNeutralObjectType, int _DespawnObjectTypeAlliance, uint _MessageIdAlliance, int _DespawnObjectTypeHorde, uint _MessageIdHorde)
        {
            SpawnNeutralObjectType = _SpawnNeutralObjectType;
            DespawnObjectTypeAlliance = _DespawnObjectTypeAlliance;
            MessageIdAlliance = _MessageIdAlliance;
            DespawnObjectTypeHorde = _DespawnObjectTypeHorde;
            MessageIdHorde = _MessageIdHorde;
        }

        public int SpawnNeutralObjectType;
        public int DespawnObjectTypeAlliance;
        public uint MessageIdAlliance;
        public int DespawnObjectTypeHorde;
        public uint MessageIdHorde;
    }

    struct BattlegroundEYCapturingPointStruct
    {
        public BattlegroundEYCapturingPointStruct(int _DespawnNeutralObjectType, int _SpawnObjectTypeAlliance, uint _MessageIdAlliance, int _SpawnObjectTypeHorde, uint _MessageIdHorde, uint _GraveYardId)
        {
            DespawnNeutralObjectType = _DespawnNeutralObjectType;
            SpawnObjectTypeAlliance = _SpawnObjectTypeAlliance;
            MessageIdAlliance = _MessageIdAlliance;
            SpawnObjectTypeHorde = _SpawnObjectTypeHorde;
            MessageIdHorde = _MessageIdHorde;
            GraveYardId = _GraveYardId;
        }

        public int DespawnNeutralObjectType;
        public int SpawnObjectTypeAlliance;
        public uint MessageIdAlliance;
        public int SpawnObjectTypeHorde;
        public uint MessageIdHorde;
        public uint GraveYardId;
    }

    struct EotSMisc
    {
        public const uint EventStartBattle = 13180; // Achievement: Flurry
        public const int FlagRespawnTime = (8 * Time.InMilliseconds);
        public const int FPointsTickTime = (2 * Time.InMilliseconds);

        public const uint NotEYWeekendHonorTicks = 260;
        public const uint EYWeekendHonorTicks = 160;

        public const uint ObjectiveCaptureFlag = 183;

        public const uint SpellNetherstormFlag = 34976;
        public const uint SpellPlayerDroppedFlag = 34991;

        public const uint ExploitTeleportLocationAlliance = 3773;
        public const uint ExploitTeleportLocationHorde = 3772;

        public static Position[] TriggerPositions =
        {
            new Position(2044.28f, 1729.68f, 1189.96f, 0.017453f),  // FEL_REAVER center
            new Position(2048.83f, 1393.65f, 1194.49f, 0.20944f),   // BLOOD_ELF center
            new Position(2286.56f, 1402.36f, 1197.11f, 3.72381f),   // DRAENEI_RUINS center
            new Position(2284.48f, 1731.23f, 1189.99f, 2.89725f)    // MAGE_TOWER center
        };

        public static byte[] TickPoints = { 1, 2, 5, 10 };
        public static uint[] FlagPoints = { 75, 85, 100, 500 };

        public static BattlegroundEYPointIconsStruct[] m_PointsIconStruct =
        {
            new BattlegroundEYPointIconsStruct(EotSWorldStateIds.FelReaverUncontrol, EotSWorldStateIds.FelReaverAllianceControl, EotSWorldStateIds.FelReaverHordeControl),
            new BattlegroundEYPointIconsStruct(EotSWorldStateIds.BloodElfUncontrol, EotSWorldStateIds.BloodElfAllianceControl, EotSWorldStateIds.BloodElfHordeControl),
            new BattlegroundEYPointIconsStruct(EotSWorldStateIds.DraeneiRuinsUncontrol, EotSWorldStateIds.DraeneiRuinsAllianceControl, EotSWorldStateIds.DraeneiRuinsHordeControl),
            new BattlegroundEYPointIconsStruct(EotSWorldStateIds.MageTowerUncontrol, EotSWorldStateIds.MageTowerAllianceControl, EotSWorldStateIds.MageTowerHordeControl)
        };
        public static BattlegroundEYLosingPointStruct[] m_LosingPointTypes =
        {
            new BattlegroundEYLosingPointStruct(EotSObjectTypes.NBannerFelReaverCenter, EotSObjectTypes.ABannerFelReaverCenter, EotSBroadcastTexts.AllianceLostFelReaverRuins, EotSObjectTypes.HBannerFelReaverCenter, EotSBroadcastTexts.HordeLostFelReaverRuins),
            new BattlegroundEYLosingPointStruct(EotSObjectTypes.NBannerBloodElfCenter, EotSObjectTypes.ABannerBloodElfCenter, EotSBroadcastTexts.AllianceLostBloodElfTower, EotSObjectTypes.HBannerBloodElfCenter, EotSBroadcastTexts.HordeLostBloodElfTower),
            new BattlegroundEYLosingPointStruct(EotSObjectTypes.NBannerDraeneiRuinsCenter, EotSObjectTypes.ABannerDraeneiRuinsCenter, EotSBroadcastTexts.AllianceLostDraeneiRuins, EotSObjectTypes.HBannerDraeneiRuinsCenter, EotSBroadcastTexts.HordeLostDraeneiRuins),
            new BattlegroundEYLosingPointStruct(EotSObjectTypes.NBannerMageTowerCenter, EotSObjectTypes.ABannerMageTowerCenter, EotSBroadcastTexts.AllianceLostMageTower, EotSObjectTypes.HBannerMageTowerCenter, EotSBroadcastTexts.HordeLostMageTower)
        };
        public static BattlegroundEYCapturingPointStruct[] m_CapturingPointTypes =
        {
            new BattlegroundEYCapturingPointStruct(EotSObjectTypes.NBannerFelReaverCenter, EotSObjectTypes.ABannerFelReaverCenter, EotSBroadcastTexts.AllianceTakenFelReaverRuins, EotSObjectTypes.HBannerFelReaverCenter, EotSBroadcastTexts.HordeTakenFelReaverRuins, EotSGaveyardIds.FelReaver),
            new BattlegroundEYCapturingPointStruct(EotSObjectTypes.NBannerBloodElfCenter, EotSObjectTypes.ABannerBloodElfCenter, EotSBroadcastTexts.AllianceTakenBloodElfTower, EotSObjectTypes.HBannerBloodElfCenter, EotSBroadcastTexts.HordeTakenBloodElfTower, EotSGaveyardIds.BloodElf),
            new BattlegroundEYCapturingPointStruct(EotSObjectTypes.NBannerDraeneiRuinsCenter, EotSObjectTypes.ABannerDraeneiRuinsCenter, EotSBroadcastTexts.AllianceTakenDraeneiRuins, EotSObjectTypes.HBannerDraeneiRuinsCenter, EotSBroadcastTexts.HordeTakenDraeneiRuins, EotSGaveyardIds.DraeneiRuins),
            new BattlegroundEYCapturingPointStruct(EotSObjectTypes.NBannerMageTowerCenter, EotSObjectTypes.ABannerMageTowerCenter, EotSBroadcastTexts.AllianceTakenMageTower, EotSObjectTypes.HBannerMageTowerCenter, EotSBroadcastTexts.HordeTakenMageTower, EotSGaveyardIds.MageTower)
        };
    }

    struct EotSBroadcastTexts
    {
        public const uint AllianceTakenFelReaverRuins = 17828;
        public const uint HordeTakenFelReaverRuins = 17829;
        public const uint AllianceLostFelReaverRuins = 17835;
        public const uint HordeLostFelReaverRuins = 17836;

        public const uint AllianceTakenBloodElfTower = 17819;
        public const uint HordeTakenBloodElfTower = 17823;
        public const uint AllianceLostBloodElfTower = 17831;
        public const uint HordeLostBloodElfTower = 17832;

        public const uint AllianceTakenDraeneiRuins = 17826;
        public const uint HordeTakenDraeneiRuins = 17827;
        public const uint AllianceLostDraeneiRuins = 17833;
        public const uint HordeLostDraeneiRuins = 17834;

        public const uint AllianceTakenMageTower = 17824;
        public const uint HordeTakenMageTower = 17825;
        public const uint AllianceLostMageTower = 17837;
        public const uint HordeLostMageTower = 17838;

        public const uint TakenFlag = 18359;
        public const uint FlagDropped = 18361;
        public const uint FlagReset = 18364;
        public const uint AllianceCapturedFlag = 18375;
        public const uint HordeCapturedFlag = 18384;
    }

    struct EotSWorldStateIds
    {
        public const uint AllianceResources = 2749;
        public const uint HordeResources = 2750;
        public const uint AllianceBase = 2752;
        public const uint HordeBase = 2753;
        public const uint DraeneiRuinsHordeControl = 2733;
        public const uint DraeneiRuinsAllianceControl = 2732;
        public const uint DraeneiRuinsUncontrol = 2731;
        public const uint MageTowerAllianceControl = 2730;
        public const uint MageTowerHordeControl = 2729;
        public const uint MageTowerUncontrol = 2728;
        public const uint FelReaverHordeControl = 2727;
        public const uint FelReaverAllianceControl = 2726;
        public const uint FelReaverUncontrol = 2725;
        public const uint BloodElfHordeControl = 2724;
        public const uint BloodElfAllianceControl = 2723;
        public const uint BloodElfUncontrol = 2722;
        public const uint ProgressBarPercentGrey = 2720;                 //100 = Empty (Only Grey); 0 = Blue|Red (No Grey)
        public const uint ProgressBarStatus = 2719;                 //50 Init!; 48 ... Hordak Bere .. 33 .. 0 = Full 100% Hordacky; 100 = Full Alliance
        public const uint ProgressBarShow = 2718;                 //1 Init; 0 Druhy Send - Bez Messagu; 1 = Controlled Aliance
        public const uint NetherstormFlag = 2757;
        //Set To 2 When Flag Is Picked Up; And To 1 If It Is Dropped
        public const uint NetherstormFlagStateAlliance = 2769;
        public const uint NetherstormFlagStateHorde = 2770;
    }

    enum EotSProgressBarConsts
    {
        PointMaxCapturersCount = 5,
        PointRadius = 70,
        ProgressBarDontShow = 0,
        ProgressBarShow = 1,
        ProgressBarPercentGrey = 40,
        ProgressBarStateMiddle = 50,
        ProgressBarHordeControlled = 0,
        ProgressBarNeutralLow = 30,
        ProgressBarNeutralHigh = 70,
        ProgressBarAliControlled = 100
    }

    struct EotSSoundIds
    {
        //strange ids, but sure about them
        public const uint FlagPickedUpAlliance = 8212;
        public const uint FlagCapturedHorde = 8213;
        public const uint FlagPickedUpHorde = 8174;
        public const uint FlagCapturedAlliance = 8173;
        public const uint FlagReset = 8192;
    }

    struct EotSObjectIds
    {
        public const uint ADoor = 184719;           //Alliance Door
        public const uint HDoor = 184720;          //Horde Door
        public const uint Flag1 = 184493;           //Netherstorm Flag (Generic)
        public const uint Flag2 = 184141;           //Netherstorm Flag (Flagstand)
        public const uint Flag3 = 184142;           //Netherstorm Flag (Flagdrop)
        public const uint ABanner = 184381;           //Visual Banner (Alliance)
        public const uint HBanner = 184380;           //Visual Banner (Horde)
        public const uint NBanner = 184382;           //Visual Banner (Neutral)
        public const uint BeTowerCap = 184080;           //Be Tower Cap Pt
        public const uint FrTowerCap = 184081;           //Fel Reaver Cap Pt
        public const uint HuTowerCap = 184082;           //Human Tower Cap Pt
        public const uint DrTowerCap = 184083;            //Draenei Tower Cap Pt
    }

    struct EotSPointsTrigger
    {
        public const uint BloodElfPoint = 4476;
        public const uint FelReaverPoint = 4514;
        public const uint MageTowerPoint = 4516;
        public const uint DraeneiRuinsPoint = 4518;
        public const uint BloodElfBuff = 4568;
        public const uint FelReaverBuff = 4569;
        public const uint MageTowerBuff = 4570;
        public const uint DraeneiRuinsBuff = 4571;
    }

    struct EotSGaveyardIds
    {
        public const int MainAlliance = 1103;
        public const uint MainHorde = 1104;
        public const uint FelReaver = 1105;
        public const uint BloodElf = 1106;
        public const uint DraeneiRuins = 1107;
        public const uint MageTower = 1108;
    }

    struct EotSPoints
    {
        public const int FelReaver = 0;
        public const int BloodElf = 1;
        public const int DraeneiRuins = 2;
        public const int MageTower = 3;

        public const int PlayersOutOfPoints = 4;
        public const int PointsMax = 4;
    }

    struct EotSCreaturesTypes
    {
        public const uint SpiritFelReaver = 0;
        public const uint SpiritBloodElf = 1;
        public const uint SpiritDraeneiRuins = 2;
        public const uint SpiritMageTower = 3;
        public const int SpiritMainAlliance = 4;
        public const int SpiritMainHorde = 5;

        public const uint TriggerFelReaver = 6;
        public const uint TriggerBloodElf = 7;
        public const uint TriggerDraeneiRuins = 8;
        public const uint TriggerMageTower = 9;

       public const uint Max = 10;
    }

    struct EotSObjectTypes
    {
        public const int DoorA = 0;
        public const int DoorH = 1;
        public const int ABannerFelReaverCenter = 2;
        public const int ABannerFelReaverLeft = 3;
        public const int ABannerFelReaverRight = 4;
        public const int ABannerBloodElfCenter = 5;
        public const int ABannerBloodElfLeft = 6;
        public const int ABannerBloodElfRight = 7;
        public const int ABannerDraeneiRuinsCenter = 8;
        public const int ABannerDraeneiRuinsLeft = 9;
        public const int ABannerDraeneiRuinsRight = 10;
        public const int ABannerMageTowerCenter = 11;
        public const int ABannerMageTowerLeft = 12;
        public const int ABannerMageTowerRight = 13;
        public const int HBannerFelReaverCenter = 14;
        public const int HBannerFelReaverLeft = 15;
        public const int HBannerFelReaverRight = 16;
        public const int HBannerBloodElfCenter = 17;
        public const int HBannerBloodElfLeft = 18;
        public const int HBannerBloodElfRight = 19;
        public const int HBannerDraeneiRuinsCenter = 20;
        public const int HBannerDraeneiRuinsLeft = 21;
        public const int HBannerDraeneiRuinsRight = 22;
        public const int HBannerMageTowerCenter = 23;
        public const int HBannerMageTowerLeft = 24;
        public const int HBannerMageTowerRight = 25;
        public const int NBannerFelReaverCenter = 26;
        public const int NBannerFelReaverLeft = 27;
        public const int NBannerFelReaverRight = 28;
        public const int NBannerBloodElfCenter = 29;
        public const int NBannerBloodElfLeft = 30;
        public const int NBannerBloodElfRight = 31;
        public const int NBannerDraeneiRuinsCenter = 32;
        public const int NBannerDraeneiRuinsLeft = 33;
        public const int NBannerDraeneiRuinsRight = 34;
        public const int NBannerMageTowerCenter = 35;
        public const int NBannerMageTowerLeft = 36;
        public const int NBannerMageTowerRight = 37;
        public const int TowerCapFelReaver = 38;
        public const int TowerCapBloodElf = 39;
        public const int TowerCapDraeneiRuins = 40;
        public const int TowerCapMageTower = 41;
        public const int FlagNetherstorm = 42;
        public const int FlagFelReaver = 43;
        public const int FlagBloodElf = 44;
        public const int FlagDraeneiRuins = 45;
        public const int FlagMageTower = 46;
        //Buffs
        public const int SpeedbuffFelReaver = 47;
        public const int RegenbuffFelReaver = 48;
        public const int BerserkbuffFelReaver = 49;
        public const int SpeedbuffBloodElf = 50;
        public const int RegenbuffBloodElf = 51;
        public const int BerserkbuffBloodElf = 52;
        public const int SpeedbuffDraeneiRuins = 53;
        public const int RegenbuffDraeneiRuins = 54;
        public const int BerserkbuffDraeneiRuins = 55;
        public const int SpeedbuffMageTower = 56;
        public const int RegenbuffMageTower = 57;
        public const int BerserkbuffMageTower = 58;
        public const int Max = 59;
    }

    struct EotSScoreIds
    {
        public const uint WarningNearVictoryScore = 1400;
        public const uint MaxTeamScore = 1600;
    }

    enum EotSFlagState
    {
        OnBase = 0,
        WaitRespawn = 1,
        OnPlayer = 2,
        OnGround = 3
    }

    enum EotSPointState
    {
        NoOwner = 0,
        Uncontrolled = 0,
        UnderControl = 3
    }
}
