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

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.BattleGrounds.Zones.EyeOfTheStorm
{
    internal class BgEyeofStorm : Battleground
    {
        private readonly byte[] _currentPointPlayersCount = new byte[2 * EotSPoints.POINTS_MAX];
        private ObjectGuid _droppedFlagGUID;
        private uint _flagCapturedBgObjectType; // Type that should be despawned when flag is captured

        private ObjectGuid _flagKeeper;   // keepers Guid
        private EotSFlagState _flagState; // for checking flag State
        private int _flagsTimer;

        private readonly uint[] _honorScoreTics = new uint[2];
        private uint _honorTics;
        private readonly BattlegroundPointCaptureStatus[] _lastPointCaptureStatus = new BattlegroundPointCaptureStatus[EotSPoints.POINTS_MAX];
        private readonly List<ObjectGuid>[] _playersNearPoint = new List<ObjectGuid>[EotSPoints.POINTS_MAX + 1];

        private int _pointAddingTimer;
        private readonly EotSProgressBarConsts[] _pointBarStatus = new EotSProgressBarConsts[EotSPoints.POINTS_MAX];

        private readonly Team[] _pointOwnedByTeam = new Team[EotSPoints.POINTS_MAX];

        private readonly uint[] _points_Trigger = new uint[EotSPoints.POINTS_MAX];
        private readonly EotSPointState[] _pointState = new EotSPointState[EotSPoints.POINTS_MAX];
        private readonly uint[] _teamPointsCount = new uint[2];
        private int _towerCapCheckTimer;

        public BgEyeofStorm(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            _BuffChange = true;
            BgObjects = new ObjectGuid[EotSObjectTypes.MAX];
            BgCreatures = new ObjectGuid[EotSCreaturesTypes.MAX];
            _points_Trigger[EotSPoints.FEL_REAVER] = EotSPointsTrigger.FEL_REAVER_BUFF;
            _points_Trigger[EotSPoints.BLOOD_ELF] = EotSPointsTrigger.BLOOD_ELF_BUFF;
            _points_Trigger[EotSPoints.DRAENEI_RUINS] = EotSPointsTrigger.DRAENEI_RUINS_BUFF;
            _points_Trigger[EotSPoints.MAGE_TOWER] = EotSPointsTrigger.MAGE_TOWER_BUFF;
            _honorScoreTics[TeamId.Alliance] = 0;
            _honorScoreTics[TeamId.Horde] = 0;
            _teamPointsCount[TeamId.Alliance] = 0;
            _teamPointsCount[TeamId.Horde] = 0;
            _flagKeeper.Clear();
            _droppedFlagGUID.Clear();
            _flagCapturedBgObjectType = 0;
            _flagState = EotSFlagState.OnBase;
            _flagsTimer = 0;
            _towerCapCheckTimer = 0;
            _pointAddingTimer = 0;
            _honorTics = 0;

            for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
            {
                _pointOwnedByTeam[i] = Team.Other;
                _pointState[i] = EotSPointState.Uncontrolled;
                _pointBarStatus[i] = EotSProgressBarConsts.ProgressBarStateMiddle;
                _lastPointCaptureStatus[i] = BattlegroundPointCaptureStatus.Neutral;
            }

            for (byte i = 0; i < EotSPoints.POINTS_MAX + 1; ++i)
                _playersNearPoint[i] = new List<ObjectGuid>();

            for (byte i = 0; i < 2 * EotSPoints.POINTS_MAX; ++i)
                _currentPointPlayersCount[i] = 0;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                _pointAddingTimer -= (int)diff;

                if (_pointAddingTimer <= 0)
                {
                    _pointAddingTimer = EotSMisc.F_POINTS_TICK_TIME;

                    if (_teamPointsCount[TeamId.Alliance] > 0)
                        AddPoints(Team.Alliance, EotSMisc.TickPoints[_teamPointsCount[TeamId.Alliance] - 1]);

                    if (_teamPointsCount[TeamId.Horde] > 0)
                        AddPoints(Team.Horde, EotSMisc.TickPoints[_teamPointsCount[TeamId.Horde] - 1]);
                }

                if (_flagState == EotSFlagState.WaitRespawn ||
                    _flagState == EotSFlagState.OnGround)
                {
                    _flagsTimer -= (int)diff;

                    if (_flagsTimer < 0)
                    {
                        _flagsTimer = 0;

                        if (_flagState == EotSFlagState.WaitRespawn)
                            RespawnFlag(true);
                        else
                            RespawnFlagAfterDrop();
                    }
                }

                _towerCapCheckTimer -= (int)diff;

                if (_towerCapCheckTimer <= 0)
                {
                    //check if player joined point
                    /*I used this order of calls, because although we will check if one player is in gameobject's distance 2 times
					  but we can Count of players on current point in CheckSomeoneLeftPoint
					*/
                    CheckSomeoneJoinedPoint();
                    //check if player left point
                    CheckSomeoneLeftPo();
                    UpdatePointStatuses();
                    _towerCapCheckTimer = EotSMisc.F_POINTS_TICK_TIME;
                }
            }
        }

        public override void StartingEventCloseDoors()
        {
            SpawnBGObject(EotSObjectTypes.DOOR_A, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(EotSObjectTypes.DOOR_H, BattlegroundConst.RespawnImmediately);

            for (int i = EotSObjectTypes.A_BANNER_FEL_REAVER_CENTER; i < EotSObjectTypes.MAX; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
        }

        public override void StartingEventOpenDoors()
        {
            SpawnBGObject(EotSObjectTypes.DOOR_A, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(EotSObjectTypes.DOOR_H, BattlegroundConst.RespawnOneDay);

            for (int i = EotSObjectTypes.N_BANNER_FEL_REAVER_LEFT; i <= EotSObjectTypes.FLAG_NETHERSTORM; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

            for (int i = 0; i < EotSPoints.POINTS_MAX; ++i)
            {
                //randomly spawn buff
                byte buff = (byte)RandomHelper.URand(0, 2);
                SpawnBGObject(EotSObjectTypes.SPEEDBUFF_FEL_REAVER + buff + i * 3, BattlegroundConst.RespawnImmediately);
            }

            // Achievement: Flurry
            TriggerGameEvent(EotSMisc.EVENT_START_BATTLE);
        }

        private void AddPoints(Team Team, uint Points)
        {
            int team_index = GetTeamIndexByTeamId(Team);
            _TeamScores[team_index] += Points;
            _honorScoreTics[team_index] += Points;

            if (_honorScoreTics[team_index] >= _honorTics)
            {
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team);
                _honorScoreTics[team_index] -= _honorTics;
            }

            UpdateTeamScore(team_index);
        }

        private BattlegroundPointCaptureStatus GetPointCaptureStatus(uint point)
        {
            if (_pointBarStatus[point] >= EotSProgressBarConsts.ProgressBarAliControlled)
                return BattlegroundPointCaptureStatus.AllianceControlled;

            if (_pointBarStatus[point] <= EotSProgressBarConsts.ProgressBarHordeControlled)
                return BattlegroundPointCaptureStatus.HordeControlled;

            if (_currentPointPlayersCount[2 * point] == _currentPointPlayersCount[2 * point + 1])
                return BattlegroundPointCaptureStatus.Neutral;

            return _currentPointPlayersCount[2 * point] > _currentPointPlayersCount[2 * point + 1]
                       ? BattlegroundPointCaptureStatus.AllianceCapturing
                       : BattlegroundPointCaptureStatus.HordeCapturing;
        }

        private void CheckSomeoneJoinedPoint()
        {
            GameObject obj;

            for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
            {
                obj = GetBgMap().GetGameObject(BgObjects[EotSObjectTypes.TOWER_CAP_FEL_REAVER + i]);

                if (obj)
                {
                    byte j = 0;

                    while (j < _playersNearPoint[EotSPoints.POINTS_MAX].Count)
                    {
                        Player player = Global.ObjAccessor.FindPlayer(_playersNearPoint[EotSPoints.POINTS_MAX][j]);

                        if (!player)
                        {
                            Log.outError(LogFilter.Battleground, "BattlegroundEY:CheckSomeoneJoinedPoint: Player ({0}) could not be found!", _playersNearPoint[EotSPoints.POINTS_MAX][j].ToString());
                            ++j;

                            continue;
                        }

                        if (player.CanCaptureTowerPoint() &&
                            player.IsWithinDistInMap(obj, (float)EotSProgressBarConsts.PointRadius))
                        {
                            //player joined point!
                            //show progress bar
                            player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_PERCENT_GREY, (uint)EotSProgressBarConsts.ProgressBarPercentGrey);
                            player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_STATUS, (uint)_pointBarStatus[i]);
                            player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_SHOW, (uint)EotSProgressBarConsts.ProgressBarShow);
                            //add player to point
                            _playersNearPoint[i].Add(_playersNearPoint[EotSPoints.POINTS_MAX][j]);
                            //remove player from "free space"
                            _playersNearPoint[EotSPoints.POINTS_MAX].RemoveAt(j);
                        }
                        else
                        {
                            ++j;
                        }
                    }
                }
            }
        }

        private void CheckSomeoneLeftPo()
        {
            //reset current point counts
            for (byte i = 0; i < 2 * EotSPoints.POINTS_MAX; ++i)
                _currentPointPlayersCount[i] = 0;

            GameObject obj;

            for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
            {
                obj = GetBgMap().GetGameObject(BgObjects[EotSObjectTypes.TOWER_CAP_FEL_REAVER + i]);

                if (obj)
                {
                    byte j = 0;

                    while (j < _playersNearPoint[i].Count)
                    {
                        Player player = Global.ObjAccessor.FindPlayer(_playersNearPoint[i][j]);

                        if (!player)
                        {
                            Log.outError(LogFilter.Battleground, "BattlegroundEY:CheckSomeoneLeftPoint Player ({0}) could not be found!", _playersNearPoint[i][j].ToString());
                            //move non-existing players to "free space" - this will cause many errors showing in log, but it is a very important bug
                            _playersNearPoint[EotSPoints.POINTS_MAX].Add(_playersNearPoint[i][j]);
                            _playersNearPoint[i].RemoveAt(j);

                            continue;
                        }

                        if (!player.CanCaptureTowerPoint() ||
                            !player.IsWithinDistInMap(obj, (float)EotSProgressBarConsts.PointRadius))
                        //move player out of point (add him to players that are out of points
                        {
                            _playersNearPoint[EotSPoints.POINTS_MAX].Add(_playersNearPoint[i][j]);
                            _playersNearPoint[i].RemoveAt(j);
                            player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_SHOW, (uint)EotSProgressBarConsts.ProgressBarDontShow);
                        }
                        else
                        {
                            //player is neat flag, so update Count:
                            _currentPointPlayersCount[2 * i + GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID()))]++;
                            ++j;
                        }
                    }
                }
            }
        }

        private void UpdatePointStatuses()
        {
            for (byte point = 0; point < EotSPoints.POINTS_MAX; ++point)
            {
                if (!_playersNearPoint[point].Empty())
                {
                    //Count new point bar status:
                    int pointDelta = _currentPointPlayersCount[2 * point] - _currentPointPlayersCount[2 * point + 1];
                    MathFunctions.RoundToInterval(ref pointDelta, -(int)EotSProgressBarConsts.PointMaxCapturersCount, EotSProgressBarConsts.PointMaxCapturersCount);
                    _pointBarStatus[point] += pointDelta;

                    if (_pointBarStatus[point] > EotSProgressBarConsts.ProgressBarAliControlled)
                        //point is fully alliance's
                        _pointBarStatus[point] = EotSProgressBarConsts.ProgressBarAliControlled;

                    if (_pointBarStatus[point] < EotSProgressBarConsts.ProgressBarHordeControlled)
                        //point is fully horde's
                        _pointBarStatus[point] = EotSProgressBarConsts.ProgressBarHordeControlled;

                    uint pointOwnerTeamId;

                    //find which team should own this point
                    if (_pointBarStatus[point] <= EotSProgressBarConsts.ProgressBarNeutralLow)
                        pointOwnerTeamId = (uint)Team.Horde;
                    else if (_pointBarStatus[point] >= EotSProgressBarConsts.ProgressBarNeutralHigh)
                        pointOwnerTeamId = (uint)Team.Alliance;
                    else
                        pointOwnerTeamId = (uint)EotSPointState.NoOwner;

                    for (byte i = 0; i < _playersNearPoint[point].Count; ++i)
                    {
                        Player player = Global.ObjAccessor.FindPlayer(_playersNearPoint[point][i]);

                        if (player)
                        {
                            player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_STATUS, (uint)_pointBarStatus[point]);
                            Team team = GetPlayerTeam(player.GetGUID());

                            //if point owner changed we must evoke event!
                            if (pointOwnerTeamId != (uint)_pointOwnedByTeam[point])
                            {
                                //point was uncontrolled and player is from team which captured point
                                if (_pointState[point] == EotSPointState.Uncontrolled &&
                                    (uint)team == pointOwnerTeamId)
                                    EventTeamCapturedPoint(player, point);

                                //point was under control and player isn't from team which controlled it
                                if (_pointState[point] == EotSPointState.UnderControl &&
                                    team != _pointOwnedByTeam[point])
                                    EventTeamLostPoint(player, point);
                            }

                            // @workaround The original AreaTrigger is covered by a bigger one and not triggered on client side.
                            if (point == EotSPoints.FEL_REAVER &&
                                _pointOwnedByTeam[point] == team)
                                if (_flagState != 0 &&
                                    GetFlagPickerGUID() == player.GetGUID())
                                    if (player.GetDistance(2044.0f, 1729.729f, 1190.03f) < 3.0f)
                                        EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_FEL_REAVER);
                        }
                    }
                }

                BattlegroundPointCaptureStatus captureStatus = GetPointCaptureStatus(point);

                if (_lastPointCaptureStatus[point] != captureStatus)
                {
                    UpdateWorldState(EotSMisc.PointsIconStruct[point].WorldStateAllianceStatusBarIcon, captureStatus == BattlegroundPointCaptureStatus.AllianceControlled ? 2 : captureStatus == BattlegroundPointCaptureStatus.AllianceCapturing ? 1 : 0);
                    UpdateWorldState(EotSMisc.PointsIconStruct[point].WorldStateHordeStatusBarIcon, captureStatus == BattlegroundPointCaptureStatus.HordeControlled ? 2 : captureStatus == BattlegroundPointCaptureStatus.HordeCapturing ? 1 : 0);
                    _lastPointCaptureStatus[point] = captureStatus;
                }
            }
        }

        private void UpdateTeamScore(int team)
        {
            uint score = GetTeamScore(team);

            if (score >= EotSScoreIds.MAX_TEAM_SCORE)
            {
                score = EotSScoreIds.MAX_TEAM_SCORE;

                if (team == TeamId.Alliance)
                    EndBattleground(Team.Alliance);
                else
                    EndBattleground(Team.Horde);
            }

            if (team == TeamId.Alliance)
                UpdateWorldState(EotSWorldStateIds.ALLIANCE_RESOURCES, (int)score);
            else
                UpdateWorldState(EotSWorldStateIds.HORDE_RESOURCES, (int)score);
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

        private void UpdatePointsCount(Team team)
        {
            if (team == Team.Alliance)
                UpdateWorldState(EotSWorldStateIds.ALLIANCE_BASE, (int)_teamPointsCount[TeamId.Alliance]);
            else
                UpdateWorldState(EotSWorldStateIds.HORDE_BASE, (int)_teamPointsCount[TeamId.Horde]);
        }

        private void UpdatePointsIcons(Team team, int Point)
        {
            //we MUST firstly send 0, after that we can send 1!!!
            if (_pointState[Point] == EotSPointState.UnderControl)
            {
                UpdateWorldState(EotSMisc.PointsIconStruct[Point].WorldStateControlIndex, 0);

                if (team == Team.Alliance)
                    UpdateWorldState(EotSMisc.PointsIconStruct[Point].WorldStateAllianceControlledIndex, 1);
                else
                    UpdateWorldState(EotSMisc.PointsIconStruct[Point].WorldStateHordeControlledIndex, 1);
            }
            else
            {
                if (team == Team.Alliance)
                    UpdateWorldState(EotSMisc.PointsIconStruct[Point].WorldStateAllianceControlledIndex, 0);
                else
                    UpdateWorldState(EotSMisc.PointsIconStruct[Point].WorldStateHordeControlledIndex, 0);

                UpdateWorldState(EotSMisc.PointsIconStruct[Point].WorldStateControlIndex, 1);
            }
        }

        public override void AddPlayer(Player player)
        {
            bool isInBattleground = IsPlayerInBattleground(player.GetGUID());
            base.AddPlayer(player);

            if (!isInBattleground)
                PlayerScores[player.GetGUID()] = new BgEyeOfStormScore(player.GetGUID(), player.GetBGTeam());

            _playersNearPoint[EotSPoints.POINTS_MAX].Add(player.GetGUID());
        }

        public override void RemovePlayer(Player player, ObjectGuid guid, Team team)
        {
            // sometimes flag aura not removed :(
            for (int j = EotSPoints.POINTS_MAX; j >= 0; --j)
            {
                for (int i = 0; i < _playersNearPoint[j].Count; ++i)
                    if (_playersNearPoint[j][i] == guid)
                        _playersNearPoint[j].RemoveAt(i);
            }

            if (IsFlagPickedup())
                if (_flagKeeper == guid)
                {
                    if (player)
                    {
                        EventPlayerDroppedFlag(player);
                    }
                    else
                    {
                        SetFlagPicker(ObjectGuid.Empty);
                        RespawnFlag(true);
                    }
                }
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            if (!player.IsAlive()) //hack code, must be removed later
                return;

            switch (trigger)
            {
                case 4530: // Horde Start
                case 4531: // Alliance Start
                    if (GetStatus() == BattlegroundStatus.WaitJoin &&
                        !entered)
                        TeleportPlayerToExploitLocation(player);

                    break;
                case EotSPointsTrigger.BLOOD_ELF_POINT:
                    if (_pointState[EotSPoints.BLOOD_ELF] == EotSPointState.UnderControl &&
                        _pointOwnedByTeam[EotSPoints.BLOOD_ELF] == GetPlayerTeam(player.GetGUID()))
                        if (_flagState != 0 &&
                            GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_BLOOD_ELF);

                    break;
                case EotSPointsTrigger.FEL_REAVER_POINT:
                    if (_pointState[EotSPoints.FEL_REAVER] == EotSPointState.UnderControl &&
                        _pointOwnedByTeam[EotSPoints.FEL_REAVER] == GetPlayerTeam(player.GetGUID()))
                        if (_flagState != 0 &&
                            GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_FEL_REAVER);

                    break;
                case EotSPointsTrigger.MAGE_TOWER_POINT:
                    if (_pointState[EotSPoints.MAGE_TOWER] == EotSPointState.UnderControl &&
                        _pointOwnedByTeam[EotSPoints.MAGE_TOWER] == GetPlayerTeam(player.GetGUID()))
                        if (_flagState != 0 &&
                            GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_MAGE_TOWER);

                    break;
                case EotSPointsTrigger.DRAENEI_RUINS_POINT:
                    if (_pointState[EotSPoints.DRAENEI_RUINS] == EotSPointState.UnderControl &&
                        _pointOwnedByTeam[EotSPoints.DRAENEI_RUINS] == GetPlayerTeam(player.GetGUID()))
                        if (_flagState != 0 &&
                            GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_DRAENEI_RUINS);

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
            if (!AddObject(EotSObjectTypes.DOOR_A, EotSObjectIds.A_DOOR_EY_ENTRY, 2527.59716796875f, 1596.90625f, 1238.4544677734375f, 3.159139871597290039f, 0.173641681671142578f, 0.001514434814453125f, -0.98476982116699218f, 0.008638577535748481f, BattlegroundConst.RespawnImmediately) ||
                !AddObject(EotSObjectTypes.DOOR_H, EotSObjectIds.H_DOOR_EY_ENTRY, 1803.2066650390625f, 1539.486083984375f, 1238.4544677734375f, 3.13898324966430664f, 0.173647880554199218f, 0.0f, 0.984807014465332031f, 0.001244877814315259f, BattlegroundConst.RespawnImmediately)
                // banners (alliance)
                ||
                !AddObject(EotSObjectTypes.A_BANNER_FEL_REAVER_CENTER, EotSObjectIds.A_BANNER_EY_ENTRY, 2057.47265625f, 1735.109130859375f, 1188.065673828125f, 5.305802345275878906f, 0.0f, 0.0f, -0.46947097778320312f, 0.882947921752929687f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_FEL_REAVER_LEFT, EotSObjectIds.A_BANNER_EY_ENTRY, 2032.248291015625f, 1729.546875f, 1191.2296142578125f, 1.797688722610473632f, 0.0f, 0.0f, 0.7826080322265625f, 0.622514784336090087f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_FEL_REAVER_RIGHT, EotSObjectIds.A_BANNER_EY_ENTRY, 2092.338623046875f, 1775.4739990234375f, 1187.504150390625f, 5.811946868896484375f, 0.0f, 0.0f, -0.2334451675415039f, 0.972369968891143798f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_BLOOD_ELF_CENTER, EotSObjectIds.A_BANNER_EY_ENTRY, 2047.1910400390625f, 1349.1927490234375f, 1189.0032958984375f, 4.660029888153076171f, 0.0f, 0.0f, -0.72537422180175781f, 0.688354730606079101f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_BLOOD_ELF_LEFT, EotSObjectIds.A_BANNER_EY_ENTRY, 2074.319580078125f, 1385.779541015625f, 1194.7203369140625f, 0.488691210746765136f, 0.0f, 0.0f, 0.241921424865722656f, 0.970295846462249755f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_BLOOD_ELF_RIGHT, EotSObjectIds.A_BANNER_EY_ENTRY, 2025.125f, 1386.123291015625f, 1192.7354736328125f, 2.391098499298095703f, 0.0f, 0.0f, 0.930417060852050781f, 0.366502493619918823f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_DRAENEI_RUINS_CENTER, EotSObjectIds.A_BANNER_EY_ENTRY, 2276.796875f, 1400.407958984375f, 1196.333740234375f, 2.44346022605895996f, 0.0f, 0.0f, 0.939692497253417968f, 0.34202045202255249f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_DRAENEI_RUINS_LEFT, EotSObjectIds.A_BANNER_EY_ENTRY, 2305.776123046875f, 1404.5572509765625f, 1199.384765625f, 1.745326757431030273f, 0.0f, 0.0f, 0.766043663024902343f, 0.642788589000701904f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_DRAENEI_RUINS_RIGHT, EotSObjectIds.A_BANNER_EY_ENTRY, 2245.395751953125f, 1366.4132080078125f, 1195.27880859375f, 2.216565132141113281f, 0.0f, 0.0f, 0.894933700561523437f, 0.44619917869567871f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_MAGE_TOWER_CENTER, EotSObjectIds.A_BANNER_EY_ENTRY, 2270.8359375f, 1784.080322265625f, 1186.757080078125f, 2.426007747650146484f, 0.0f, 0.0f, 0.936672210693359375f, 0.350207358598709106f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_MAGE_TOWER_LEFT, EotSObjectIds.A_BANNER_EY_ENTRY, 2269.126708984375f, 1737.703125f, 1186.8145751953125f, 0.994837164878845214f, 0.0f, 0.0f, 0.477158546447753906f, 0.878817260265350341f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.A_BANNER_MAGE_TOWER_RIGHT, EotSObjectIds.A_BANNER_EY_ENTRY, 2300.85595703125f, 1741.24658203125f, 1187.793212890625f, 5.497788906097412109f, 0.0f, 0.0f, -0.38268280029296875f, 0.923879802227020263f, BattlegroundConst.RespawnOneDay)
                // banners (horde)
                ||
                !AddObject(EotSObjectTypes.H_BANNER_FEL_REAVER_CENTER, EotSObjectIds.H_BANNER_EY_ENTRY, 2057.45654296875f, 1735.07470703125f, 1187.9063720703125f, 5.35816192626953125f, 0.0f, 0.0f, -0.446197509765625f, 0.894934535026550292f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_FEL_REAVER_LEFT, EotSObjectIds.H_BANNER_EY_ENTRY, 2032.251708984375f, 1729.532958984375f, 1190.3251953125f, 1.867502212524414062f, 0.0f, 0.0f, 0.803856849670410156f, 0.594822824001312255f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_FEL_REAVER_RIGHT, EotSObjectIds.H_BANNER_EY_ENTRY, 2092.354248046875f, 1775.4583740234375f, 1187.079345703125f, 5.881760597229003906f, 0.0f, 0.0f, -0.19936752319335937f, 0.979924798011779785f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_BLOOD_ELF_CENTER, EotSObjectIds.H_BANNER_EY_ENTRY, 2047.1978759765625f, 1349.1875f, 1188.5650634765625f, 4.625123500823974609f, 0.0f, 0.0f, -0.73727703094482421f, 0.67559051513671875f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_BLOOD_ELF_LEFT, EotSObjectIds.H_BANNER_EY_ENTRY, 2074.3056640625f, 1385.7725830078125f, 1194.4686279296875f, 0.471238493919372558f, 0.0f, 0.0f, 0.233445167541503906f, 0.972369968891143798f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_BLOOD_ELF_RIGHT, EotSObjectIds.H_BANNER_EY_ENTRY, 2025.09375f, 1386.12158203125f, 1192.6536865234375f, 2.373644113540649414f, 0.0f, 0.0f, 0.927183151245117187f, 0.37460830807685852f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_DRAENEI_RUINS_CENTER, EotSObjectIds.H_BANNER_EY_ENTRY, 2276.798583984375f, 1400.4410400390625f, 1196.2200927734375f, 2.495818138122558593f, 0.0f, 0.0f, 0.948323249816894531f, 0.317305892705917358f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_DRAENEI_RUINS_LEFT, EotSObjectIds.H_BANNER_EY_ENTRY, 2305.763916015625f, 1404.5972900390625f, 1199.3333740234375f, 1.640606880187988281f, 0.0f, 0.0f, 0.731352806091308593f, 0.6819993257522583f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_DRAENEI_RUINS_RIGHT, EotSObjectIds.H_BANNER_EY_ENTRY, 2245.382080078125f, 1366.454833984375f, 1195.1815185546875f, 2.373644113540649414f, 0.0f, 0.0f, 0.927183151245117187f, 0.37460830807685852f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_MAGE_TOWER_CENTER, EotSObjectIds.H_BANNER_EY_ENTRY, 2270.869873046875f, 1784.0989990234375f, 1186.4384765625f, 2.356194972991943359f, 0.0f, 0.0f, 0.923879623413085937f, 0.382683247327804565f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_MAGE_TOWER_LEFT, EotSObjectIds.H_BANNER_EY_ENTRY, 2268.59716796875f, 1737.0191650390625f, 1186.75390625f, 0.942476630210876464f, 0.0f, 0.0f, 0.453989982604980468f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.H_BANNER_MAGE_TOWER_RIGHT, EotSObjectIds.H_BANNER_EY_ENTRY, 2301.01904296875f, 1741.4930419921875f, 1187.48974609375f, 5.375615119934082031f, 0.0f, 0.0f, -0.4383707046508789f, 0.898794233798980712f, BattlegroundConst.RespawnOneDay)
                // banners (natural)
                ||
                !AddObject(EotSObjectTypes.N_BANNER_FEL_REAVER_CENTER, EotSObjectIds.N_BANNER_EY_ENTRY, 2057.4931640625f, 1735.111083984375f, 1187.675537109375f, 5.340708732604980468f, 0.0f, 0.0f, -0.45398998260498046f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_FEL_REAVER_LEFT, EotSObjectIds.N_BANNER_EY_ENTRY, 2032.2569580078125f, 1729.5572509765625f, 1191.0802001953125f, 1.797688722610473632f, 0.0f, 0.0f, 0.7826080322265625f, 0.622514784336090087f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_FEL_REAVER_RIGHT, EotSObjectIds.N_BANNER_EY_ENTRY, 2092.395751953125f, 1775.451416015625f, 1186.965576171875f, 5.89921426773071289f, 0.0f, 0.0f, -0.19080829620361328f, 0.981627285480499267f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_BLOOD_ELF_CENTER, EotSObjectIds.N_BANNER_EY_ENTRY, 2047.1875f, 1349.1944580078125f, 1188.5731201171875f, 4.642575740814208984f, 0.0f, 0.0f, -0.731353759765625f, 0.681998312473297119f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_BLOOD_ELF_LEFT, EotSObjectIds.N_BANNER_EY_ENTRY, 2074.3212890625f, 1385.76220703125f, 1194.362060546875f, 0.488691210746765136f, 0.0f, 0.0f, 0.241921424865722656f, 0.970295846462249755f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_BLOOD_ELF_RIGHT, EotSObjectIds.N_BANNER_EY_ENTRY, 2025.13720703125f, 1386.1336669921875f, 1192.5482177734375f, 2.391098499298095703f, 0.0f, 0.0f, 0.930417060852050781f, 0.366502493619918823f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_DRAENEI_RUINS_CENTER, EotSObjectIds.N_BANNER_EY_ENTRY, 2276.833251953125f, 1400.4375f, 1196.146728515625f, 2.478367090225219726f, 0.0f, 0.0f, 0.94551849365234375f, 0.325568377971649169f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_DRAENEI_RUINS_LEFT, EotSObjectIds.N_BANNER_EY_ENTRY, 2305.77783203125f, 1404.5364990234375f, 1199.246337890625f, 1.570795774459838867f, 0.0f, 0.0f, 0.707106590270996093f, 0.707106947898864746f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_DRAENEI_RUINS_RIGHT, EotSObjectIds.N_BANNER_EY_ENTRY, 2245.40966796875f, 1366.4410400390625f, 1195.1107177734375f, 2.356194972991943359f, 0.0f, 0.0f, 0.923879623413085937f, 0.382683247327804565f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_MAGE_TOWER_CENTER, EotSObjectIds.N_BANNER_EY_ENTRY, 2270.84033203125f, 1784.1197509765625f, 1186.1473388671875f, 2.303830623626708984f, 0.0f, 0.0f, 0.913544654846191406f, 0.406738430261611938f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_MAGE_TOWER_LEFT, EotSObjectIds.N_BANNER_EY_ENTRY, 2268.46533203125f, 1736.8385009765625f, 1186.742919921875f, 0.942476630210876464f, 0.0f, 0.0f, 0.453989982604980468f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.N_BANNER_MAGE_TOWER_RIGHT, EotSObjectIds.N_BANNER_EY_ENTRY, 2300.9931640625f, 1741.5504150390625f, 1187.10693359375f, 5.375615119934082031f, 0.0f, 0.0f, -0.4383707046508789f, 0.898794233798980712f, BattlegroundConst.RespawnOneDay)
                // Flags
                ||
                !AddObject(EotSObjectTypes.FLAG_NETHERSTORM, EotSObjectIds.FLAG_2_EY_ENTRY, 2174.444580078125f, 1569.421875f, 1159.852783203125f, 4.625123500823974609f, 0.0f, 0.0f, -0.73727703094482421f, 0.67559051513671875f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.FLAG_FEL_REAVER, EotSObjectIds.FLAG_1_EY_ENTRY, 2044.28f, 1729.68f, 1189.96f, -0.017453f, 0, 0, 0.008727f, -0.999962f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.FLAG_BLOOD_ELF, EotSObjectIds.FLAG_1_EY_ENTRY, 2048.83f, 1393.65f, 1194.49f, 0.20944f, 0, 0, 0.104528f, 0.994522f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.FLAG_DRAENEI_RUINS, EotSObjectIds.FLAG_1_EY_ENTRY, 2286.56f, 1402.36f, 1197.11f, 3.72381f, 0, 0, 0.957926f, -0.287016f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.FLAG_MAGE_TOWER, EotSObjectIds.FLAG_1_EY_ENTRY, 2284.48f, 1731.23f, 1189.99f, 2.89725f, 0, 0, 0.992546f, 0.121869f, BattlegroundConst.RespawnOneDay)
                // tower cap
                ||
                !AddObject(EotSObjectTypes.TOWER_CAP_FEL_REAVER, EotSObjectIds.FR_TOWER_CAP_EY_ENTRY, 2024.600708f, 1742.819580f, 1195.157715f, 2.443461f, 0, 0, 0.939693f, 0.342020f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.TOWER_CAP_BLOOD_ELF, EotSObjectIds.BE_TOWER_CAP_EY_ENTRY, 2050.493164f, 1372.235962f, 1194.563477f, 1.710423f, 0, 0, 0.754710f, 0.656059f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.TOWER_CAP_DRAENEI_RUINS, EotSObjectIds.DR_TOWER_CAP_EY_ENTRY, 2301.010498f, 1386.931641f, 1197.183472f, 1.570796f, 0, 0, 0.707107f, 0.707107f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.TOWER_CAP_MAGE_TOWER, EotSObjectIds.HU_TOWER_CAP_EY_ENTRY, 2282.121582f, 1760.006958f, 1189.707153f, 1.919862f, 0, 0, 0.819152f, 0.573576f, BattlegroundConst.RespawnOneDay)
                // buffs
                ||
                !AddObject(EotSObjectTypes.SPEEDBUFF_FEL_REAVER, EotSObjectIds.SPEED_BUFF_FEL_REAVER_EY_ENTRY, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.REGENBUFF_FEL_REAVER, EotSObjectIds.RESTORATION_BUFF_FEL_REAVER_EY_ENTRY, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.BERSERKBUFF_FEL_REAVER, EotSObjectIds.BERSERK_BUFF_FEL_REAVER_EY_ENTRY, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.SPEEDBUFF_BLOOD_ELF, EotSObjectIds.SPEED_BUFF_BLOOD_ELF_EY_ENTRY, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.REGENBUFF_BLOOD_ELF, EotSObjectIds.RESTORATION_BUFF_BLOOD_ELF_EY_ENTRY, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.BERSERKBUFF_BLOOD_ELF, EotSObjectIds.BERSERK_BUFF_BLOOD_ELF_EY_ENTRY, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.SPEEDBUFF_DRAENEI_RUINS, EotSObjectIds.SPEED_BUFF_DRAENEI_RUINS_EY_ENTRY, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.REGENBUFF_DRAENEI_RUINS, EotSObjectIds.RESTORATION_BUFF_DRAENEI_RUINS_EY_ENTRY, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.BERSERKBUFF_DRAENEI_RUINS, EotSObjectIds.BERSERK_BUFF_DRAENEI_RUINS_EY_ENTRY, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.SPEEDBUFF_MAGE_TOWER, EotSObjectIds.SPEED_BUFF_MAGE_TOWER_EY_ENTRY, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.REGENBUFF_MAGE_TOWER, EotSObjectIds.RESTORATION_BUFF_MAGE_TOWER_EY_ENTRY, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay) ||
                !AddObject(EotSObjectTypes.BERSERKBUFF_MAGE_TOWER, EotSObjectIds.BERSERK_BUFF_MAGE_TOWER_EY_ENTRY, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay)
               )
            {
                Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn some objects. The battleground was not created.");

                return false;
            }

            WorldSafeLocsEntry sg = Global.ObjectMgr.GetWorldSafeLoc(EotSGaveyardIds.MAIN_ALLIANCE);

            if (sg == null ||
                !AddSpiritGuide(EotSCreaturesTypes.SPIRIT_MAIN_ALLIANCE, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), 3.124139f, TeamId.Alliance))
            {
                Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");

                return false;
            }

            sg = Global.ObjectMgr.GetWorldSafeLoc(EotSGaveyardIds.MAIN_HORDE);

            if (sg == null ||
                !AddSpiritGuide(EotSCreaturesTypes.SPIRIT_MAIN_HORDE, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), 3.193953f, TeamId.Horde))
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

            _TeamScores[TeamId.Alliance] = 0;
            _TeamScores[TeamId.Horde] = 0;
            _teamPointsCount[TeamId.Alliance] = 0;
            _teamPointsCount[TeamId.Horde] = 0;
            _honorScoreTics[TeamId.Alliance] = 0;
            _honorScoreTics[TeamId.Horde] = 0;
            _flagState = EotSFlagState.OnBase;
            _flagCapturedBgObjectType = 0;
            _flagKeeper.Clear();
            _droppedFlagGUID.Clear();
            _pointAddingTimer = 0;
            _towerCapCheckTimer = 0;
            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
            _honorTics = isBGWeekend ? EotSMisc.EY_WEEKEND_HONOR_TICKS : EotSMisc.NOT_EY_WEEKEND_HONOR_TICKS;

            for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
            {
                _pointOwnedByTeam[i] = Team.Other;
                _pointState[i] = EotSPointState.Uncontrolled;
                _pointBarStatus[i] = EotSProgressBarConsts.ProgressBarStateMiddle;
                _playersNearPoint[i].Clear();
            }

            _playersNearPoint[EotSPoints.PLAYERS_OUT_OF_POINTS].Clear();
        }

        private void RespawnFlag(bool send_message)
        {
            if (_flagCapturedBgObjectType > 0)
                SpawnBGObject((int)_flagCapturedBgObjectType, BattlegroundConst.RespawnOneDay);

            _flagCapturedBgObjectType = 0;
            _flagState = EotSFlagState.OnBase;
            SpawnBGObject(EotSObjectTypes.FLAG_NETHERSTORM, BattlegroundConst.RespawnImmediately);

            if (send_message)
            {
                SendBroadcastText(EotSBroadcastTexts.FLAG_RESET, ChatMsg.BgSystemNeutral);
                PlaySoundToAll(EotSSoundIds.FLAG_RESET); // Flags respawned sound...
            }

            UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG, 1);
        }

        private void RespawnFlagAfterDrop()
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
                if (IsFlagPickedup() &&
                    GetFlagPickerGUID() == player.GetGUID())
                {
                    SetFlagPicker(ObjectGuid.Empty);
                    player.RemoveAurasDueToSpell(EotSMisc.SPELL_NETHERSTORM_FLAG);
                }

                return;
            }

            if (!IsFlagPickedup())
                return;

            if (GetFlagPickerGUID() != player.GetGUID())
                return;

            SetFlagPicker(ObjectGuid.Empty);
            player.RemoveAurasDueToSpell(EotSMisc.SPELL_NETHERSTORM_FLAG);
            _flagState = EotSFlagState.OnGround;
            _flagsTimer = EotSMisc.FLAG_RESPAWN_TIME;
            player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedFlag, true);
            player.CastSpell(player, EotSMisc.SPELL_PLAYER_DROPPED_FLAG, true);
            //this does not work correctly :((it should remove flag carrier Name)
            UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_HORDE, (int)EotSFlagState.WaitRespawn);
            UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_ALLIANCE, (int)EotSFlagState.WaitRespawn);

            if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
                SendBroadcastText(EotSBroadcastTexts.FLAG_DROPPED, ChatMsg.BgSystemAlliance, null);
            else
                SendBroadcastText(EotSBroadcastTexts.FLAG_DROPPED, ChatMsg.BgSystemHorde, null);
        }

        public override void EventPlayerClickedOnFlag(Player player, GameObject target_obj)
        {
            if (GetStatus() != BattlegroundStatus.InProgress ||
                IsFlagPickedup() ||
                !player.IsWithinDistInMap(target_obj, 10))
                return;

            if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
            {
                UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_ALLIANCE, (int)EotSFlagState.OnPlayer);
                PlaySoundToAll(EotSSoundIds.FLAG_PICKED_UP_ALLIANCE);
            }
            else
            {
                UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_HORDE, (int)EotSFlagState.OnPlayer);
                PlaySoundToAll(EotSSoundIds.FLAG_PICKED_UP_HORDE);
            }

            if (_flagState == EotSFlagState.OnBase)
                UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG, 0);

            _flagState = EotSFlagState.OnPlayer;

            SpawnBGObject(EotSObjectTypes.FLAG_NETHERSTORM, BattlegroundConst.RespawnOneDay);
            SetFlagPicker(player.GetGUID());
            //get flag aura on player
            player.CastSpell(player, EotSMisc.SPELL_NETHERSTORM_FLAG, true);
            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

            if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
                SendBroadcastText(EotSBroadcastTexts.TAKEN_FLAG, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(EotSBroadcastTexts.TAKEN_FLAG, ChatMsg.BgSystemHorde, player);
        }

        private void EventTeamLostPoint(Player player, int Point)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            //Natural point
            Team Team = _pointOwnedByTeam[Point];

            if (Team == 0)
                return;

            if (Team == Team.Alliance)
            {
                _teamPointsCount[TeamId.Alliance]--;
                SpawnBGObject(EotSMisc.LosingPointTypes[Point].DespawnObjectTypeAlliance, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(EotSMisc.LosingPointTypes[Point].DespawnObjectTypeAlliance + 1, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(EotSMisc.LosingPointTypes[Point].DespawnObjectTypeAlliance + 2, BattlegroundConst.RespawnOneDay);
            }
            else
            {
                _teamPointsCount[TeamId.Horde]--;
                SpawnBGObject(EotSMisc.LosingPointTypes[Point].DespawnObjectTypeHorde, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(EotSMisc.LosingPointTypes[Point].DespawnObjectTypeHorde + 1, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(EotSMisc.LosingPointTypes[Point].DespawnObjectTypeHorde + 2, BattlegroundConst.RespawnOneDay);
            }

            SpawnBGObject(EotSMisc.LosingPointTypes[Point].SpawnNeutralObjectType, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(EotSMisc.LosingPointTypes[Point].SpawnNeutralObjectType + 1, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(EotSMisc.LosingPointTypes[Point].SpawnNeutralObjectType + 2, BattlegroundConst.RespawnImmediately);

            //buff isn't despawned

            _pointOwnedByTeam[Point] = Team.Other;
            _pointState[Point] = EotSPointState.NoOwner;

            if (Team == Team.Alliance)
                SendBroadcastText(EotSMisc.LosingPointTypes[Point].MessageIdAlliance, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(EotSMisc.LosingPointTypes[Point].MessageIdHorde, ChatMsg.BgSystemHorde, player);

            UpdatePointsIcons(Team, Point);
            UpdatePointsCount(Team);

            //remove bonus honor aura trigger creature when node is lost
            if (Point < EotSPoints.POINTS_MAX)
                DelCreature(Point + 6); //null checks are in DelCreature! 0-5 spirit guides
        }

        private void EventTeamCapturedPoint(Player player, int Point)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            Team Team = GetPlayerTeam(player.GetGUID());

            SpawnBGObject(EotSMisc.CapturingPointTypes[Point].DespawnNeutralObjectType, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(EotSMisc.CapturingPointTypes[Point].DespawnNeutralObjectType + 1, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(EotSMisc.CapturingPointTypes[Point].DespawnNeutralObjectType + 2, BattlegroundConst.RespawnOneDay);

            if (Team == Team.Alliance)
            {
                _teamPointsCount[TeamId.Alliance]++;
                SpawnBGObject(EotSMisc.CapturingPointTypes[Point].SpawnObjectTypeAlliance, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(EotSMisc.CapturingPointTypes[Point].SpawnObjectTypeAlliance + 1, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(EotSMisc.CapturingPointTypes[Point].SpawnObjectTypeAlliance + 2, BattlegroundConst.RespawnImmediately);
            }
            else
            {
                _teamPointsCount[TeamId.Horde]++;
                SpawnBGObject(EotSMisc.CapturingPointTypes[Point].SpawnObjectTypeHorde, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(EotSMisc.CapturingPointTypes[Point].SpawnObjectTypeHorde + 1, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(EotSMisc.CapturingPointTypes[Point].SpawnObjectTypeHorde + 2, BattlegroundConst.RespawnImmediately);
            }

            //buff isn't respawned

            _pointOwnedByTeam[Point] = Team;
            _pointState[Point] = EotSPointState.UnderControl;

            if (Team == Team.Alliance)
                SendBroadcastText(EotSMisc.CapturingPointTypes[Point].MessageIdAlliance, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(EotSMisc.CapturingPointTypes[Point].MessageIdHorde, ChatMsg.BgSystemHorde, player);

            if (!BgCreatures[Point].IsEmpty())
                DelCreature(Point);

            WorldSafeLocsEntry sg = Global.ObjectMgr.GetWorldSafeLoc(EotSMisc.CapturingPointTypes[Point].GraveYardId);

            if (sg == null ||
                !AddSpiritGuide(Point, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), 3.124139f, GetTeamIndexByTeamId(Team)))
                Log.outError(LogFilter.Battleground,
                             "BatteGroundEY: Failed to spawn spirit guide. point: {0}, team: {1}, graveyard_id: {2}",
                             Point,
                             Team,
                             EotSMisc.CapturingPointTypes[Point].GraveYardId);

            //    SpawnBGCreature(Point, RESPAWN_IMMEDIATELY);

            UpdatePointsIcons(Team, Point);
            UpdatePointsCount(Team);

            if (Point >= EotSPoints.POINTS_MAX)
                return;

            Creature trigger = GetBGCreature(Point + 6); //0-5 spirit guides

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

        private void EventPlayerCapturedFlag(Player player, uint BgObjectType)
        {
            if (GetStatus() != BattlegroundStatus.InProgress ||
                GetFlagPickerGUID() != player.GetGUID())
                return;

            SetFlagPicker(ObjectGuid.Empty);
            _flagState = EotSFlagState.WaitRespawn;
            player.RemoveAurasDueToSpell(EotSMisc.SPELL_NETHERSTORM_FLAG);

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

            Team team = GetPlayerTeam(player.GetGUID());

            if (team == Team.Alliance)
            {
                SendBroadcastText(EotSBroadcastTexts.ALLIANCE_CAPTURED_FLAG, ChatMsg.BgSystemAlliance, player);
                PlaySoundToAll(EotSSoundIds.FLAG_CAPTURED_ALLIANCE);
            }
            else
            {
                SendBroadcastText(EotSBroadcastTexts.HORDE_CAPTURED_FLAG, ChatMsg.BgSystemHorde, player);
                PlaySoundToAll(EotSSoundIds.FLAG_CAPTURED_HORDE);
            }

            SpawnBGObject((int)BgObjectType, BattlegroundConst.RespawnImmediately);

            _flagsTimer = EotSMisc.FLAG_RESPAWN_TIME;
            _flagCapturedBgObjectType = BgObjectType;

            int team_id = GetTeamIndexByTeamId(team);

            if (_teamPointsCount[team_id] > 0)
                AddPoints(team, EotSMisc.FlagPoints[_teamPointsCount[team_id] - 1]);

            UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_HORDE, (int)EotSFlagState.OnBase);
            UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_ALLIANCE, (int)EotSFlagState.OnBase);

            UpdatePlayerScore(player, ScoreType.FlagCaptures, 1);
        }

        public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
        {
            if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
                return false;

            switch (type)
            {
                case ScoreType.FlagCaptures:
                    player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, EotSMisc.OBJECTIVE_CAPTURE_FLAG);

                    break;
                default:
                    break;
            }

            return true;
        }

        public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
        {
            uint g_id;
            Team team = GetPlayerTeam(player.GetGUID());

            switch (team)
            {
                case Team.Alliance:
                    g_id = EotSGaveyardIds.MAIN_ALLIANCE;

                    break;
                case Team.Horde:
                    g_id = EotSGaveyardIds.MAIN_HORDE;

                    break;
                default: return null;
            }

            WorldSafeLocsEntry entry = Global.ObjectMgr.GetWorldSafeLoc(g_id);
            WorldSafeLocsEntry nearestEntry = entry;

            if (entry == null)
            {
                Log.outError(LogFilter.Battleground, "BattlegroundEY: The main team graveyard could not be found. The graveyard system will not be operational!");

                return null;
            }

            float plr_x = player.GetPositionX();
            float plr_y = player.GetPositionY();
            float plr_z = player.GetPositionZ();

            float distance = (entry.Loc.GetPositionX() - plr_x) * (entry.Loc.GetPositionX() - plr_x) + (entry.Loc.GetPositionY() - plr_y) * (entry.Loc.GetPositionY() - plr_y) + (entry.Loc.GetPositionZ() - plr_z) * (entry.Loc.GetPositionZ() - plr_z);
            float nearestDistance = distance;

            for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
                if (_pointOwnedByTeam[i] == team &&
                    _pointState[i] == EotSPointState.UnderControl)
                {
                    entry = Global.ObjectMgr.GetWorldSafeLoc(EotSMisc.CapturingPointTypes[i].GraveYardId);

                    if (entry == null)
                    {
                        Log.outError(LogFilter.Battleground, "BattlegroundEY: Graveyard {0} could not be found.", EotSMisc.CapturingPointTypes[i].GraveYardId);
                    }
                    else
                    {
                        distance = (entry.Loc.GetPositionX() - plr_x) * (entry.Loc.GetPositionX() - plr_x) + (entry.Loc.GetPositionY() - plr_y) * (entry.Loc.GetPositionY() - plr_y) + (entry.Loc.GetPositionZ() - plr_z) * (entry.Loc.GetPositionZ() - plr_z);

                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestEntry = entry;
                        }
                    }
                }

            return nearestEntry;
        }

        public override WorldSafeLocsEntry GetExploitTeleportLocation(Team team)
        {
            return Global.ObjectMgr.GetWorldSafeLoc(team == Team.Alliance ? EotSMisc.EXPLOIT_TELEPORT_LOCATION_ALLIANCE : EotSMisc.EXPLOIT_TELEPORT_LOCATION_HORDE);
        }

        public override Team GetPrematureWinner()
        {
            if (GetTeamScore(TeamId.Alliance) > GetTeamScore(TeamId.Horde))
                return Team.Alliance;
            else if (GetTeamScore(TeamId.Horde) > GetTeamScore(TeamId.Alliance))
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        public override ObjectGuid GetFlagPickerGUID(int team = -1)
        {
            return _flagKeeper;
        }

        private void SetFlagPicker(ObjectGuid guid)
        {
            _flagKeeper = guid;
        }

        private bool IsFlagPickedup()
        {
            return !_flagKeeper.IsEmpty();
        }

        public override void SetDroppedFlagGUID(ObjectGuid guid, int TeamID = -1)
        {
            _droppedFlagGUID = guid;
        }

        private ObjectGuid GetDroppedFlagGUID()
        {
            return _droppedFlagGUID;
        }
    }
}