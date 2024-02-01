// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones.EyeofStorm
{
    class BgEyeofStorm : Battleground
    {
        public BgEyeofStorm(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            BgObjects = new ObjectGuid[ObjectTypes.Max];
            BgCreatures = new ObjectGuid[CreaturesTypes.Max];
            m_Points_Trigger[Points.FelReaver] = PointsTrigger.FelReaverBuff;
            m_Points_Trigger[Points.BloodElf] = PointsTrigger.BloodElfBuff;
            m_Points_Trigger[Points.DraeneiRuins] = PointsTrigger.DraeneiRuinsBuff;
            m_Points_Trigger[Points.MageTower] = PointsTrigger.MageTowerBuff;
            m_HonorScoreTics[TeamId.Alliance] = 0;
            m_HonorScoreTics[TeamId.Horde] = 0;
            m_TeamPointsCount[TeamId.Alliance] = 0;
            m_TeamPointsCount[TeamId.Horde] = 0;
            m_FlagKeeper.Clear();
            m_DroppedFlagGUID.Clear();
            m_FlagCapturedBgObjectType = 0;
            m_FlagState = FlagState.OnBase;
            m_FlagsTimer = 0;
            m_TowerCapCheckTimer = 0;
            m_PointAddingTimer = 0;
            m_HonorTics = 0;

            for (byte i = 0; i < Points.PointsMax; ++i)
            {
                m_PointOwnedByTeam[i] = Team.Other;
                m_PointState[i] = PointState.Uncontrolled;
                m_PointBarStatus[i] = ProgressBarConsts.ProgressBarStateMiddle;
                m_LastPointCaptureStatus[i] = BattlegroundPointCaptureStatus.Neutral;
            }

            for (byte i = 0; i < Points.PointsMax + 1; ++i)
                m_PlayersNearPoint[i] = new List<ObjectGuid>();

            for (byte i = 0; i < 2 * Points.PointsMax; ++i)
                m_CurrentPointPlayersCount[i] = 0;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                m_PointAddingTimer -= (int)diff;
                if (m_PointAddingTimer <= 0)
                {
                    m_PointAddingTimer = Misc.FPointsTickTime;
                    if (m_TeamPointsCount[TeamId.Alliance] > 0)
                        AddPoints(Team.Alliance, Misc.TickPoints[m_TeamPointsCount[TeamId.Alliance] - 1]);
                    if (m_TeamPointsCount[TeamId.Horde] > 0)
                        AddPoints(Team.Horde, Misc.TickPoints[m_TeamPointsCount[TeamId.Horde] - 1]);
                }

                if (m_FlagState == FlagState.WaitRespawn || m_FlagState == FlagState.OnGround)
                {
                    m_FlagsTimer -= (int)diff;

                    if (m_FlagsTimer < 0)
                    {
                        m_FlagsTimer = 0;
                        if (m_FlagState == FlagState.WaitRespawn)
                            RespawnFlag(true);
                        else
                            RespawnFlagAfterDrop();
                    }
                }
            }
        }

        public override void StartingEventCloseDoors()
        {
            SpawnBGObject(ObjectTypes.DoorA, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(ObjectTypes.DoorH, BattlegroundConst.RespawnImmediately);

            for (int i = ObjectTypes.ABannerFelReaverCenter; i < ObjectTypes.Max; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
        }

        public override void StartingEventOpenDoors()
        {
            SpawnBGObject(ObjectTypes.DoorA, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(ObjectTypes.DoorH, BattlegroundConst.RespawnOneDay);

            for (int i = ObjectTypes.NBannerFelReaverLeft; i <= ObjectTypes.FlagNetherstorm; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

            for (int i = 0; i < Points.PointsMax; ++i)
            {
                //randomly spawn buff
                byte buff = (byte)RandomHelper.URand(0, 2);
                SpawnBGObject(ObjectTypes.SpeedbuffFelReaver + buff + i * 3, BattlegroundConst.RespawnImmediately);
            }

            // Achievement: Flurry
            TriggerGameEvent(Misc.EventStartBattle);
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

        BattlegroundPointCaptureStatus GetPointCaptureStatus(uint point)
        {
            if (m_PointBarStatus[point] >= ProgressBarConsts.ProgressBarAliControlled)
                return BattlegroundPointCaptureStatus.AllianceControlled;

            if (m_PointBarStatus[point] <= ProgressBarConsts.ProgressBarHordeControlled)
                return BattlegroundPointCaptureStatus.HordeControlled;

            if (m_CurrentPointPlayersCount[2 * point] == m_CurrentPointPlayersCount[2 * point + 1])
                return BattlegroundPointCaptureStatus.Neutral;

            return m_CurrentPointPlayersCount[2 * point] > m_CurrentPointPlayersCount[2 * point + 1]
                ? BattlegroundPointCaptureStatus.AllianceCapturing
                : BattlegroundPointCaptureStatus.HordeCapturing;
        }

        void UpdateTeamScore(int team)
        {
            uint score = GetTeamScore(team);
            if (score >= ScoreIds.MaxTeamScore)
            {
                score = ScoreIds.MaxTeamScore;
                if (team == TeamId.Alliance)
                    EndBattleground(Team.Alliance);
                else
                    EndBattleground(Team.Horde);
            }

            if (team == TeamId.Alliance)
                UpdateWorldState(WorldStateIds.AllianceResources, (int)score);
            else
                UpdateWorldState(WorldStateIds.HordeResources, (int)score);
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
                UpdateWorldState(WorldStateIds.AllianceBase, (int)m_TeamPointsCount[TeamId.Alliance]);
            else
                UpdateWorldState(WorldStateIds.HordeBase, (int)m_TeamPointsCount[TeamId.Horde]);
        }

        void UpdatePointsIcons(Team team, uint Point)
        {
            //we MUST firstly send 0, after that we can send 1!!!
            if (m_PointState[Point] == PointState.UnderControl)
            {
                UpdateWorldState(Misc.m_PointsIconStruct[Point].WorldStateControlIndex, 0);
                if (team == Team.Alliance)
                    UpdateWorldState(Misc.m_PointsIconStruct[Point].WorldStateAllianceControlledIndex, 1);
                else
                    UpdateWorldState(Misc.m_PointsIconStruct[Point].WorldStateHordeControlledIndex, 1);
            }
            else
            {
                if (team == Team.Alliance)
                    UpdateWorldState(Misc.m_PointsIconStruct[Point].WorldStateAllianceControlledIndex, 0);
                else
                    UpdateWorldState(Misc.m_PointsIconStruct[Point].WorldStateHordeControlledIndex, 0);
                UpdateWorldState(Misc.m_PointsIconStruct[Point].WorldStateControlIndex, 1);
            }
        }

        public override void AddPlayer(Player player, BattlegroundQueueTypeId queueId)
        {
            bool isInBattleground = IsPlayerInBattleground(player.GetGUID());
            base.AddPlayer(player, queueId);
            if (!isInBattleground)
                PlayerScores[player.GetGUID()] = new BgEyeOfStormScore(player.GetGUID(), player.GetBGTeam());

            m_PlayersNearPoint[Points.PointsMax].Add(player.GetGUID());
        }

        public override void RemovePlayer(Player player, ObjectGuid guid, Team team)
        {
            // sometimes flag aura not removed :(
            for (int j = Points.PointsMax; j >= 0; --j)
            {
                for (int i = 0; i < m_PlayersNearPoint[j].Count; ++i)
                    if (m_PlayersNearPoint[j][i] == guid)
                        m_PlayersNearPoint[j].RemoveAt(i);
            }
            if (IsFlagPickedup())
            {
                if (m_FlagKeeper == guid)
                {
                    if (player != null)
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
                case PointsTrigger.BloodElfPoint:
                    if (m_PointState[Points.BloodElf] == PointState.UnderControl && m_PointOwnedByTeam[Points.BloodElf] == GetPlayerTeam(player.GetGUID()))
                        if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, ObjectTypes.FlagBloodElf);
                    break;
                case PointsTrigger.FelReaverPoint:
                    if (m_PointState[Points.FelReaver] == PointState.UnderControl && m_PointOwnedByTeam[Points.FelReaver] == GetPlayerTeam(player.GetGUID()))
                        if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, ObjectTypes.FlagFelReaver);
                    break;
                case PointsTrigger.MageTowerPoint:
                    if (m_PointState[Points.MageTower] == PointState.UnderControl && m_PointOwnedByTeam[Points.MageTower] == GetPlayerTeam(player.GetGUID()))
                        if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, ObjectTypes.FlagMageTower);
                    break;
                case PointsTrigger.DraeneiRuinsPoint:
                    if (m_PointState[Points.DraeneiRuins] == PointState.UnderControl && m_PointOwnedByTeam[Points.DraeneiRuins] == GetPlayerTeam(player.GetGUID()))
                        if (m_FlagState != 0 && GetFlagPickerGUID() == player.GetGUID())
                            EventPlayerCapturedFlag(player, ObjectTypes.FlagDraeneiRuins);
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
            if (!AddObject(ObjectTypes.DoorA, ObjectIds.ADoorEyEntry, 2527.59716796875f, 1596.90625f, 1238.4544677734375f, 3.159139871597290039f, 0.173641681671142578f, 0.001514434814453125f, -0.98476982116699218f, 0.008638577535748481f, BattlegroundConst.RespawnImmediately)
                || !AddObject(ObjectTypes.DoorH, ObjectIds.HDoorEyEntry, 1803.2066650390625f, 1539.486083984375f, 1238.4544677734375f, 3.13898324966430664f, 0.173647880554199218f, 0.0f, 0.984807014465332031f, 0.001244877814315259f, BattlegroundConst.RespawnImmediately)
                // banners (alliance)
                || !AddObject(ObjectTypes.ABannerFelReaverCenter, ObjectIds.ABannerEyEntry, 2057.47265625f, 1735.109130859375f, 1188.065673828125f, 5.305802345275878906f, 0.0f, 0.0f, -0.46947097778320312f, 0.882947921752929687f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerFelReaverLeft, ObjectIds.ABannerEyEntry, 2032.248291015625f, 1729.546875f, 1191.2296142578125f, 1.797688722610473632f, 0.0f, 0.0f, 0.7826080322265625f, 0.622514784336090087f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerFelReaverRight, ObjectIds.ABannerEyEntry, 2092.338623046875f, 1775.4739990234375f, 1187.504150390625f, 5.811946868896484375f, 0.0f, 0.0f, -0.2334451675415039f, 0.972369968891143798f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerBloodElfCenter, ObjectIds.ABannerEyEntry, 2047.1910400390625f, 1349.1927490234375f, 1189.0032958984375f, 4.660029888153076171f, 0.0f, 0.0f, -0.72537422180175781f, 0.688354730606079101f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerBloodElfLeft, ObjectIds.ABannerEyEntry, 2074.319580078125f, 1385.779541015625f, 1194.7203369140625f, 0.488691210746765136f, 0.0f, 0.0f, 0.241921424865722656f, 0.970295846462249755f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerBloodElfRight, ObjectIds.ABannerEyEntry, 2025.125f, 1386.123291015625f, 1192.7354736328125f, 2.391098499298095703f, 0.0f, 0.0f, 0.930417060852050781f, 0.366502493619918823f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerDraeneiRuinsCenter, ObjectIds.ABannerEyEntry, 2276.796875f, 1400.407958984375f, 1196.333740234375f, 2.44346022605895996f, 0.0f, 0.0f, 0.939692497253417968f, 0.34202045202255249f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerDraeneiRuinsLeft, ObjectIds.ABannerEyEntry, 2305.776123046875f, 1404.5572509765625f, 1199.384765625f, 1.745326757431030273f, 0.0f, 0.0f, 0.766043663024902343f, 0.642788589000701904f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerDraeneiRuinsRight, ObjectIds.ABannerEyEntry, 2245.395751953125f, 1366.4132080078125f, 1195.27880859375f, 2.216565132141113281f, 0.0f, 0.0f, 0.894933700561523437f, 0.44619917869567871f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerMageTowerCenter, ObjectIds.ABannerEyEntry, 2270.8359375f, 1784.080322265625f, 1186.757080078125f, 2.426007747650146484f, 0.0f, 0.0f, 0.936672210693359375f, 0.350207358598709106f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerMageTowerLeft, ObjectIds.ABannerEyEntry, 2269.126708984375f, 1737.703125f, 1186.8145751953125f, 0.994837164878845214f, 0.0f, 0.0f, 0.477158546447753906f, 0.878817260265350341f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.ABannerMageTowerRight, ObjectIds.ABannerEyEntry, 2300.85595703125f, 1741.24658203125f, 1187.793212890625f, 5.497788906097412109f, 0.0f, 0.0f, -0.38268280029296875f, 0.923879802227020263f, BattlegroundConst.RespawnOneDay)
                // banners (horde)
                || !AddObject(ObjectTypes.HBannerFelReaverCenter, ObjectIds.HBannerEyEntry, 2057.45654296875f, 1735.07470703125f, 1187.9063720703125f, 5.35816192626953125f, 0.0f, 0.0f, -0.446197509765625f, 0.894934535026550292f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerFelReaverLeft, ObjectIds.HBannerEyEntry, 2032.251708984375f, 1729.532958984375f, 1190.3251953125f, 1.867502212524414062f, 0.0f, 0.0f, 0.803856849670410156f, 0.594822824001312255f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerFelReaverRight, ObjectIds.HBannerEyEntry, 2092.354248046875f, 1775.4583740234375f, 1187.079345703125f, 5.881760597229003906f, 0.0f, 0.0f, -0.19936752319335937f, 0.979924798011779785f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerBloodElfCenter, ObjectIds.HBannerEyEntry, 2047.1978759765625f, 1349.1875f, 1188.5650634765625f, 4.625123500823974609f, 0.0f, 0.0f, -0.73727703094482421f, 0.67559051513671875f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerBloodElfLeft, ObjectIds.HBannerEyEntry, 2074.3056640625f, 1385.7725830078125f, 1194.4686279296875f, 0.471238493919372558f, 0.0f, 0.0f, 0.233445167541503906f, 0.972369968891143798f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerBloodElfRight, ObjectIds.HBannerEyEntry, 2025.09375f, 1386.12158203125f, 1192.6536865234375f, 2.373644113540649414f, 0.0f, 0.0f, 0.927183151245117187f, 0.37460830807685852f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerDraeneiRuinsCenter, ObjectIds.HBannerEyEntry, 2276.798583984375f, 1400.4410400390625f, 1196.2200927734375f, 2.495818138122558593f, 0.0f, 0.0f, 0.948323249816894531f, 0.317305892705917358f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerDraeneiRuinsLeft, ObjectIds.HBannerEyEntry, 2305.763916015625f, 1404.5972900390625f, 1199.3333740234375f, 1.640606880187988281f, 0.0f, 0.0f, 0.731352806091308593f, 0.6819993257522583f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerDraeneiRuinsRight, ObjectIds.HBannerEyEntry, 2245.382080078125f, 1366.454833984375f, 1195.1815185546875f, 2.373644113540649414f, 0.0f, 0.0f, 0.927183151245117187f, 0.37460830807685852f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerMageTowerCenter, ObjectIds.HBannerEyEntry, 2270.869873046875f, 1784.0989990234375f, 1186.4384765625f, 2.356194972991943359f, 0.0f, 0.0f, 0.923879623413085937f, 0.382683247327804565f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerMageTowerLeft, ObjectIds.HBannerEyEntry, 2268.59716796875f, 1737.0191650390625f, 1186.75390625f, 0.942476630210876464f, 0.0f, 0.0f, 0.453989982604980468f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.HBannerMageTowerRight, ObjectIds.HBannerEyEntry, 2301.01904296875f, 1741.4930419921875f, 1187.48974609375f, 5.375615119934082031f, 0.0f, 0.0f, -0.4383707046508789f, 0.898794233798980712f, BattlegroundConst.RespawnOneDay)
                // banners (natural)
                || !AddObject(ObjectTypes.NBannerFelReaverCenter, ObjectIds.NBannerEyEntry, 2057.4931640625f, 1735.111083984375f, 1187.675537109375f, 5.340708732604980468f, 0.0f, 0.0f, -0.45398998260498046f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerFelReaverLeft, ObjectIds.NBannerEyEntry, 2032.2569580078125f, 1729.5572509765625f, 1191.0802001953125f, 1.797688722610473632f, 0.0f, 0.0f, 0.7826080322265625f, 0.622514784336090087f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerFelReaverRight, ObjectIds.NBannerEyEntry, 2092.395751953125f, 1775.451416015625f, 1186.965576171875f, 5.89921426773071289f, 0.0f, 0.0f, -0.19080829620361328f, 0.981627285480499267f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerBloodElfCenter, ObjectIds.NBannerEyEntry, 2047.1875f, 1349.1944580078125f, 1188.5731201171875f, 4.642575740814208984f, 0.0f, 0.0f, -0.731353759765625f, 0.681998312473297119f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerBloodElfLeft, ObjectIds.NBannerEyEntry, 2074.3212890625f, 1385.76220703125f, 1194.362060546875f, 0.488691210746765136f, 0.0f, 0.0f, 0.241921424865722656f, 0.970295846462249755f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerBloodElfRight, ObjectIds.NBannerEyEntry, 2025.13720703125f, 1386.1336669921875f, 1192.5482177734375f, 2.391098499298095703f, 0.0f, 0.0f, 0.930417060852050781f, 0.366502493619918823f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerDraeneiRuinsCenter, ObjectIds.NBannerEyEntry, 2276.833251953125f, 1400.4375f, 1196.146728515625f, 2.478367090225219726f, 0.0f, 0.0f, 0.94551849365234375f, 0.325568377971649169f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerDraeneiRuinsLeft, ObjectIds.NBannerEyEntry, 2305.77783203125f, 1404.5364990234375f, 1199.246337890625f, 1.570795774459838867f, 0.0f, 0.0f, 0.707106590270996093f, 0.707106947898864746f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerDraeneiRuinsRight, ObjectIds.NBannerEyEntry, 2245.40966796875f, 1366.4410400390625f, 1195.1107177734375f, 2.356194972991943359f, 0.0f, 0.0f, 0.923879623413085937f, 0.382683247327804565f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerMageTowerCenter, ObjectIds.NBannerEyEntry, 2270.84033203125f, 1784.1197509765625f, 1186.1473388671875f, 2.303830623626708984f, 0.0f, 0.0f, 0.913544654846191406f, 0.406738430261611938f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerMageTowerLeft, ObjectIds.NBannerEyEntry, 2268.46533203125f, 1736.8385009765625f, 1186.742919921875f, 0.942476630210876464f, 0.0f, 0.0f, 0.453989982604980468f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.NBannerMageTowerRight, ObjectIds.NBannerEyEntry, 2300.9931640625f, 1741.5504150390625f, 1187.10693359375f, 5.375615119934082031f, 0.0f, 0.0f, -0.4383707046508789f, 0.898794233798980712f, BattlegroundConst.RespawnOneDay)
                // flags
                || !AddObject(ObjectTypes.FlagNetherstorm, ObjectIds.Flag2EyEntry, 2174.444580078125f, 1569.421875f, 1159.852783203125f, 4.625123500823974609f, 0.0f, 0.0f, -0.73727703094482421f, 0.67559051513671875f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.FlagFelReaver, ObjectIds.Flag1EyEntry, 2044.28f, 1729.68f, 1189.96f, -0.017453f, 0, 0, 0.008727f, -0.999962f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.FlagBloodElf, ObjectIds.Flag1EyEntry, 2048.83f, 1393.65f, 1194.49f, 0.20944f, 0, 0, 0.104528f, 0.994522f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.FlagDraeneiRuins, ObjectIds.Flag1EyEntry, 2286.56f, 1402.36f, 1197.11f, 3.72381f, 0, 0, 0.957926f, -0.287016f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.FlagMageTower, ObjectIds.Flag1EyEntry, 2284.48f, 1731.23f, 1189.99f, 2.89725f, 0, 0, 0.992546f, 0.121869f, BattlegroundConst.RespawnOneDay)
                // tower cap
                || !AddObject(ObjectTypes.TowerCapFelReaver, ObjectIds.FrTowerCapEyEntry, 2024.600708f, 1742.819580f, 1195.157715f, 2.443461f, 0, 0, 0.939693f, 0.342020f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.TowerCapBloodElf, ObjectIds.BeTowerCapEyEntry, 2050.493164f, 1372.235962f, 1194.563477f, 1.710423f, 0, 0, 0.754710f, 0.656059f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.TowerCapDraeneiRuins, ObjectIds.DrTowerCapEyEntry, 2301.010498f, 1386.931641f, 1197.183472f, 1.570796f, 0, 0, 0.707107f, 0.707107f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.TowerCapMageTower, ObjectIds.HuTowerCapEyEntry, 2282.121582f, 1760.006958f, 1189.707153f, 1.919862f, 0, 0, 0.819152f, 0.573576f, BattlegroundConst.RespawnOneDay)
                // buffs
                || !AddObject(ObjectTypes.SpeedbuffFelReaver, ObjectIds.SpeedBuffFelReaverEyEntry, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.RegenbuffFelReaver, ObjectIds.RestorationBuffFelReaverEyEntry, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.BerserkbuffFelReaver, ObjectIds.BerserkBuffFelReaverEyEntry, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.SpeedbuffBloodElf, ObjectIds.SpeedBuffBloodElfEyEntry, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.RegenbuffBloodElf, ObjectIds.RestorationBuffBloodElfEyEntry, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.BerserkbuffBloodElf, ObjectIds.BerserkBuffBloodElfEyEntry, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.SpeedbuffDraeneiRuins, ObjectIds.SpeedBuffDraeneiRuinsEyEntry, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.RegenbuffDraeneiRuins, ObjectIds.RestorationBuffDraeneiRuinsEyEntry, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.BerserkbuffDraeneiRuins, ObjectIds.BerserkBuffDraeneiRuinsEyEntry, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.SpeedbuffMageTower, ObjectIds.SpeedBuffMageTowerEyEntry, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.RegenbuffMageTower, ObjectIds.RestorationBuffMageTowerEyEntry, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay)
                || !AddObject(ObjectTypes.BerserkbuffMageTower, ObjectIds.BerserkBuffMageTowerEyEntry, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay)
                )
            {
                Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn some objects. The battleground was not created.");
                return false;
            }

            WorldSafeLocsEntry sg = Global.ObjectMgr.GetWorldSafeLoc(GaveyardIds.MainAlliance);
            if (sg == null || !AddSpiritGuide(CreaturesTypes.SpiritMainAlliance, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), 3.124139f, TeamId.Alliance))
            {
                Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");
                return false;
            }

            sg = Global.ObjectMgr.GetWorldSafeLoc(GaveyardIds.MainHorde);
            if (sg == null || !AddSpiritGuide(CreaturesTypes.SpiritMainHorde, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), 3.193953f, TeamId.Horde))
            {
                Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");
                return false;
            }

            ControlZoneHandlers[ObjectIds.FrTowerCapEyEntry] = new BgEyeOfStormControlZoneHandler(this, Points.FelReaver);
            ControlZoneHandlers[ObjectIds.BeTowerCapEyEntry] = new BgEyeOfStormControlZoneHandler(this, Points.BloodElf);
            ControlZoneHandlers[ObjectIds.DrTowerCapEyEntry] = new BgEyeOfStormControlZoneHandler(this, Points.DraeneiRuins);
            ControlZoneHandlers[ObjectIds.HuTowerCapEyEntry] = new BgEyeOfStormControlZoneHandler(this, Points.MageTower);

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
            m_FlagState = FlagState.OnBase;
            m_FlagCapturedBgObjectType = 0;
            m_FlagKeeper.Clear();
            m_DroppedFlagGUID.Clear();
            m_PointAddingTimer = 0;
            m_TowerCapCheckTimer = 0;
            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
            m_HonorTics = (isBGWeekend) ? Misc.EYWeekendHonorTicks : Misc.NotEYWeekendHonorTicks;

            for (byte i = 0; i < Points.PointsMax; ++i)
            {
                m_PointOwnedByTeam[i] = Team.Other;
                m_PointState[i] = PointState.Uncontrolled;
                m_PointBarStatus[i] = ProgressBarConsts.ProgressBarStateMiddle;
                m_PlayersNearPoint[i].Clear();
            }
            m_PlayersNearPoint[Points.PlayersOutOfPoints].Clear();
        }

        void RespawnFlag(bool send_message)
        {
            if (m_FlagCapturedBgObjectType > 0)
                SpawnBGObject((int)m_FlagCapturedBgObjectType, BattlegroundConst.RespawnOneDay);

            m_FlagCapturedBgObjectType = 0;
            m_FlagState = FlagState.OnBase;
            SpawnBGObject(ObjectTypes.FlagNetherstorm, BattlegroundConst.RespawnImmediately);

            if (send_message)
            {
                SendBroadcastText(BroadcastTextIds.FlagReset, ChatMsg.BgSystemNeutral);
                PlaySoundToAll(SoundIds.FlagReset);             // flags respawned sound...
            }

            UpdateWorldState(WorldStateIds.NetherstormFlag, 1);
        }

        void RespawnFlagAfterDrop()
        {
            RespawnFlag(true);

            GameObject obj = GetBgMap().GetGameObject(GetDroppedFlagGUID());
            if (obj != null)
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
                    player.RemoveAurasDueToSpell(Misc.SpellNetherstormFlag);
                }
                return;
            }

            if (!IsFlagPickedup())
                return;

            if (GetFlagPickerGUID() != player.GetGUID())
                return;

            SetFlagPicker(ObjectGuid.Empty);
            player.RemoveAurasDueToSpell(Misc.SpellNetherstormFlag);
            m_FlagState = FlagState.OnGround;
            m_FlagsTimer = Misc.FlagRespawnTime;
            player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedFlag, true);
            player.CastSpell(player, Misc.SpellPlayerDroppedFlag, true);
            //this does not work correctly :((it should remove flag carrier name)
            UpdateWorldState(WorldStateIds.NetherstormFlagStateHorde, (int)FlagState.WaitRespawn);
            UpdateWorldState(WorldStateIds.NetherstormFlagStateAlliance, (int)FlagState.WaitRespawn);

            if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
                SendBroadcastText(BroadcastTextIds.FlagDropped, ChatMsg.BgSystemAlliance, null);
            else
                SendBroadcastText(BroadcastTextIds.FlagDropped, ChatMsg.BgSystemHorde, null);
        }

        public override void EventPlayerClickedOnFlag(Player player, GameObject target_obj)
        {
            if (GetStatus() != BattlegroundStatus.InProgress || IsFlagPickedup() || !player.IsWithinDistInMap(target_obj, 10))
                return;

            if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
            {
                UpdateWorldState(WorldStateIds.NetherstormFlagStateAlliance, (int)FlagState.OnPlayer);
                PlaySoundToAll(SoundIds.FlagPickedUpAlliance);
            }
            else
            {
                UpdateWorldState(WorldStateIds.NetherstormFlagStateHorde, (int)FlagState.OnPlayer);
                PlaySoundToAll(SoundIds.FlagPickedUpHorde);
            }

            if (m_FlagState == FlagState.OnBase)
                UpdateWorldState(WorldStateIds.NetherstormFlag, 0);
            m_FlagState = FlagState.OnPlayer;

            SpawnBGObject(ObjectTypes.FlagNetherstorm, BattlegroundConst.RespawnOneDay);
            SetFlagPicker(player.GetGUID());
            //get flag aura on player
            player.CastSpell(player, Misc.SpellNetherstormFlag, true);
            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

            if (GetPlayerTeam(player.GetGUID()) == Team.Alliance)
                SendBroadcastText(BroadcastTextIds.TakenFlag, ChatMsg.BgSystemAlliance, player);
            else
                SendBroadcastText(BroadcastTextIds.TakenFlag, ChatMsg.BgSystemHorde, player);
        }

        public void EventTeamLostPoint(Team team, uint point, WorldObject controlZone)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            if (team == 0)
                return;

            if (team == Team.Alliance)
            {
                m_TeamPointsCount[TeamId.Alliance]--;
                SpawnBGObject(Misc.m_LosingPointTypes[point].DespawnObjectTypeAlliance, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(Misc.m_LosingPointTypes[point].DespawnObjectTypeAlliance + 1, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(Misc.m_LosingPointTypes[point].DespawnObjectTypeAlliance + 2, BattlegroundConst.RespawnOneDay);
            }
            else
            {
                m_TeamPointsCount[TeamId.Horde]--;
                SpawnBGObject(Misc.m_LosingPointTypes[point].DespawnObjectTypeHorde, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(Misc.m_LosingPointTypes[point].DespawnObjectTypeHorde + 1, BattlegroundConst.RespawnOneDay);
                SpawnBGObject(Misc.m_LosingPointTypes[point].DespawnObjectTypeHorde + 2, BattlegroundConst.RespawnOneDay);
            }

            SpawnBGObject(Misc.m_LosingPointTypes[point].SpawnNeutralObjectType, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(Misc.m_LosingPointTypes[point].SpawnNeutralObjectType + 1, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(Misc.m_LosingPointTypes[point].SpawnNeutralObjectType + 2, BattlegroundConst.RespawnImmediately);

            //buff isn't despawned

            m_PointOwnedByTeam[point] = Team.Other;
            m_PointState[point] = PointState.NoOwner;

            if (team == Team.Alliance)
                SendBroadcastText(Misc.m_LosingPointTypes[point].MessageIdAlliance, ChatMsg.BgSystemAlliance, controlZone);
            else
                SendBroadcastText(Misc.m_LosingPointTypes[point].MessageIdHorde, ChatMsg.BgSystemHorde, controlZone);

            UpdatePointsIcons(team, point);
            UpdatePointsCount(team);

            //remove bonus honor aura trigger creature when node is lost
            if (point < Points.PointsMax)
                DelCreature(point + 6);//null checks are in DelCreature! 0-5 spirit guides
        }

        public void EventTeamCapturedPoint(Team team, uint point, WorldObject controlZone)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            SpawnBGObject(Misc.m_CapturingPointTypes[point].DespawnNeutralObjectType, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(Misc.m_CapturingPointTypes[point].DespawnNeutralObjectType + 1, BattlegroundConst.RespawnOneDay);
            SpawnBGObject(Misc.m_CapturingPointTypes[point].DespawnNeutralObjectType + 2, BattlegroundConst.RespawnOneDay);

            if (team == Team.Alliance)
            {
                m_TeamPointsCount[TeamId.Alliance]++;
                SpawnBGObject(Misc.m_CapturingPointTypes[point].SpawnObjectTypeAlliance, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(Misc.m_CapturingPointTypes[point].SpawnObjectTypeAlliance + 1, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(Misc.m_CapturingPointTypes[point].SpawnObjectTypeAlliance + 2, BattlegroundConst.RespawnImmediately);
            }
            else
            {
                m_TeamPointsCount[TeamId.Horde]++;
                SpawnBGObject(Misc.m_CapturingPointTypes[point].SpawnObjectTypeHorde, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(Misc.m_CapturingPointTypes[point].SpawnObjectTypeHorde + 1, BattlegroundConst.RespawnImmediately);
                SpawnBGObject(Misc.m_CapturingPointTypes[point].SpawnObjectTypeHorde + 2, BattlegroundConst.RespawnImmediately);
            }

            //buff isn't respawned

            m_PointOwnedByTeam[point] = team;
            m_PointState[point] = PointState.UnderControl;

            if (team == Team.Alliance)
                SendBroadcastText(Misc.m_CapturingPointTypes[point].MessageIdAlliance, ChatMsg.BgSystemAlliance, controlZone);
            else
                SendBroadcastText(Misc.m_CapturingPointTypes[point].MessageIdHorde, ChatMsg.BgSystemHorde, controlZone);

            if (!BgCreatures[point].IsEmpty())
                DelCreature(point);

            WorldSafeLocsEntry sg = Global.ObjectMgr.GetWorldSafeLoc(Misc.m_CapturingPointTypes[point].GraveyardId);
            if (sg == null || !AddSpiritGuide(point, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), 3.124139f, GetTeamIndexByTeamId(team)))
                Log.outError(LogFilter.Battleground, "BatteGroundEY: Failed to spawn spirit guide. point: {0}, team: {1}, graveyard_id: {2}",
                    point, team, Misc.m_CapturingPointTypes[point].GraveyardId);

            //    SpawnBGCreature(Point, RESPAWN_IMMEDIATELY);

            UpdatePointsIcons(team, point);
            UpdatePointsCount(team);

            if (point >= Points.PointsMax)
                return;

            Creature trigger = GetBGCreature(point + 6);//0-5 spirit guides
            if (trigger == null)
                trigger = AddCreature(SharedConst.WorldTrigger, point + 6, Misc.TriggerPositions[point], GetTeamIndexByTeamId(team));

            //add bonus honor aura trigger creature when node is accupied
            //cast bonus aura (+50% honor in 25yards)
            //aura should only apply to players who have accupied the node, set correct faction for trigger
            if (trigger != null)
            {
                trigger.SetFaction(team == Team.Alliance ? 84u : 83);
                trigger.CastSpell(trigger, BattlegroundConst.SpellHonorableDefender25y, false);
            }
        }

        void EventPlayerCapturedFlag(Player player, uint BgObjectType)
        {
            if (GetStatus() != BattlegroundStatus.InProgress || GetFlagPickerGUID() != player.GetGUID())
                return;

            SetFlagPicker(ObjectGuid.Empty);
            m_FlagState = FlagState.WaitRespawn;
            player.RemoveAurasDueToSpell(Misc.SpellNetherstormFlag);

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

            Team team = GetPlayerTeam(player.GetGUID());
            if (team == Team.Alliance)
            {
                SendBroadcastText(BroadcastTextIds.AllianceCapturedFlag, ChatMsg.BgSystemAlliance, player);
                PlaySoundToAll(SoundIds.FlagCapturedAlliance);
            }
            else
            {
                SendBroadcastText(BroadcastTextIds.HordeCapturedFlag, ChatMsg.BgSystemHorde, player);
                PlaySoundToAll(SoundIds.FlagCapturedHorde);
            }

            SpawnBGObject((int)BgObjectType, BattlegroundConst.RespawnImmediately);

            m_FlagsTimer = Misc.FlagRespawnTime;
            m_FlagCapturedBgObjectType = BgObjectType;

            int team_id = GetTeamIndexByTeamId(team);
            if (m_TeamPointsCount[team_id] > 0)
                AddPoints(team, Misc.FlagPoints[m_TeamPointsCount[team_id] - 1]);

            UpdateWorldState(WorldStateIds.NetherstormFlagStateHorde, (int)FlagState.OnBase);
            UpdateWorldState(WorldStateIds.NetherstormFlagStateAlliance, (int)FlagState.OnBase);

            UpdatePlayerScore(player, ScoreType.FlagCaptures, 1);
        }

        public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
        {
            if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
                return false;

            switch (type)
            {
                case ScoreType.FlagCaptures:
                    player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, Misc.ObjectiveCaptureFlag);
                    break;
                default:
                    break;
            }
            return true;
        }

        public override WorldSafeLocsEntry GetClosestGraveyard(Player player)
        {
            uint g_id;
            Team team = GetPlayerTeam(player.GetGUID());
            switch (team)
            {
                case Team.Alliance:
                    g_id = GaveyardIds.MainAlliance;
                    break;
                case Team.Horde:
                    g_id = GaveyardIds.MainHorde;
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

            for (byte i = 0; i < Points.PointsMax; ++i)
            {
                if (m_PointOwnedByTeam[i] == team && m_PointState[i] == PointState.UnderControl)
                {
                    entry = Global.ObjectMgr.GetWorldSafeLoc(Misc.m_CapturingPointTypes[i].GraveyardId);
                    if (entry == null)
                        Log.outError(LogFilter.Battleground, "BattlegroundEY: Graveyard {0} could not be found.", Misc.m_CapturingPointTypes[i].GraveyardId);
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
            }

            return nearestEntry;
        }

        public override WorldSafeLocsEntry GetExploitTeleportLocation(Team team)
        {
            return Global.ObjectMgr.GetWorldSafeLoc(team == Team.Alliance ? Misc.ExploitTeleportLocationAlliance : Misc.ExploitTeleportLocationHorde);
        }

        public override Team GetPrematureWinner()
        {
            if (GetTeamScore(TeamId.Alliance) > GetTeamScore(TeamId.Horde))
                return Team.Alliance;
            else if (GetTeamScore(TeamId.Horde) > GetTeamScore(TeamId.Alliance))
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        public override void ProcessEvent(WorldObject target, uint eventId, WorldObject invoker)
        {
            base.ProcessEvent(target, eventId, invoker);

            if (invoker != null)
            {
                GameObject gameobject = invoker.ToGameObject();
                if (gameobject != null)
                {
                    if (gameobject.GetGoType() == GameObjectTypes.ControlZone)
                    {
                        if (!ControlZoneHandlers.TryGetValue(gameobject.GetEntry(), out BgEyeOfStormControlZoneHandler handler))
                            return;

                        var controlzone = gameobject.GetGoInfo().ControlZone;
                        if (eventId == controlzone.CaptureEventAlliance)
                            handler.HandleCaptureEventAlliance(gameobject);
                        else if (eventId == controlzone.CaptureEventHorde)
                            handler.HandleCaptureEventHorde(gameobject);
                        else if (eventId == controlzone.ContestedEventAlliance)
                            handler.HandleContestedEventAlliance(gameobject);
                        else if (eventId == controlzone.ContestedEventHorde)
                            handler.HandleContestedEventHorde(gameobject);
                        else if (eventId == controlzone.NeutralEventAlliance)
                            handler.HandleNeutralEventAlliance(gameobject);
                        else if (eventId == controlzone.NeutralEventHorde)
                            handler.HandleNeutralEventHorde(gameobject);
                        else if (eventId == controlzone.ProgressEventAlliance)
                            handler.HandleProgressEventAlliance(gameobject);
                        else if (eventId == controlzone.ProgressEventHorde)
                            handler.HandleProgressEventHorde(gameobject);
                    }
                }
            }
        }

        public override ObjectGuid GetFlagPickerGUID(int team = -1) { return m_FlagKeeper; }
        void SetFlagPicker(ObjectGuid guid) { m_FlagKeeper = guid; }
        bool IsFlagPickedup() { return !m_FlagKeeper.IsEmpty(); }

        public override void SetDroppedFlagGUID(ObjectGuid guid, int TeamID = -1) { m_DroppedFlagGUID = guid; }
        ObjectGuid GetDroppedFlagGUID() { return m_DroppedFlagGUID; }

        uint[] m_HonorScoreTics = new uint[2];
        uint[] m_TeamPointsCount = new uint[2];

        uint[] m_Points_Trigger = new uint[Points.PointsMax];

        ObjectGuid m_FlagKeeper;                                // keepers guid
        ObjectGuid m_DroppedFlagGUID;
        uint m_FlagCapturedBgObjectType;                  // type that should be despawned when flag is captured
        FlagState m_FlagState;                                  // for checking flag state
        int m_FlagsTimer;
        int m_TowerCapCheckTimer;

        Team[] m_PointOwnedByTeam = new Team[Points.PointsMax];
        PointState[] m_PointState = new PointState[Points.PointsMax];
        ProgressBarConsts[] m_PointBarStatus = new ProgressBarConsts[Points.PointsMax];
        BattlegroundPointCaptureStatus[] m_LastPointCaptureStatus = new BattlegroundPointCaptureStatus[Points.PointsMax];
        List<ObjectGuid>[] m_PlayersNearPoint = new List<ObjectGuid>[Points.PointsMax + 1];
        byte[] m_CurrentPointPlayersCount = new byte[2 * Points.PointsMax];

        int m_PointAddingTimer;
        uint m_HonorTics;

        Dictionary<uint, BgEyeOfStormControlZoneHandler> ControlZoneHandlers = new();
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

        public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)Misc.ObjectiveCaptureFlag, FlagCaptures));
        }

        public override uint GetAttr1() { return FlagCaptures; }

        uint FlagCaptures;
    }

    struct BgEyeOfStormPointIconsStruct
    {
        public BgEyeOfStormPointIconsStruct(uint worldStateControlIndex, uint worldStateAllianceControlledIndex, uint worldStateHordeControlledIndex, uint worldStateAllianceStatusBarIcon, uint worldStateHordeStatusBarIcon)
        {
            WorldStateControlIndex = worldStateControlIndex;
            WorldStateAllianceControlledIndex = worldStateAllianceControlledIndex;
            WorldStateHordeControlledIndex = worldStateHordeControlledIndex;
            WorldStateAllianceStatusBarIcon = worldStateAllianceStatusBarIcon;
            WorldStateHordeStatusBarIcon = worldStateHordeStatusBarIcon;
        }

        public uint WorldStateControlIndex;
        public uint WorldStateAllianceControlledIndex;
        public uint WorldStateHordeControlledIndex;
        public uint WorldStateAllianceStatusBarIcon;
        public uint WorldStateHordeStatusBarIcon;
    }

    struct BgEyeOfStormLosingPointStruct
    {
        public BgEyeOfStormLosingPointStruct(int _SpawnNeutralObjectType, int _DespawnObjectTypeAlliance, uint _MessageIdAlliance, int _DespawnObjectTypeHorde, uint _MessageIdHorde)
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

    struct BgEyeOfStormCapturingPointStruct
    {
        public BgEyeOfStormCapturingPointStruct(int _DespawnNeutralObjectType, int _SpawnObjectTypeAlliance, uint _MessageIdAlliance, int _SpawnObjectTypeHorde, uint _MessageIdHorde, uint _GraveyardId)
        {
            DespawnNeutralObjectType = _DespawnNeutralObjectType;
            SpawnObjectTypeAlliance = _SpawnObjectTypeAlliance;
            MessageIdAlliance = _MessageIdAlliance;
            SpawnObjectTypeHorde = _SpawnObjectTypeHorde;
            MessageIdHorde = _MessageIdHorde;
            GraveyardId = _GraveyardId;
        }

        public int DespawnNeutralObjectType;
        public int SpawnObjectTypeAlliance;
        public uint MessageIdAlliance;
        public int SpawnObjectTypeHorde;
        public uint MessageIdHorde;
        public uint GraveyardId;
    }

    class BgEyeOfStormControlZoneHandler : ControlZoneHandler
    {
        BgEyeofStorm _battleground;
        uint _point;

        public BgEyeOfStormControlZoneHandler(BgEyeofStorm bg, uint point)
        {
            _battleground = bg;
            _point = point;
        }

        public override void HandleProgressEventHorde(GameObject controlZone)
        {
            _battleground.EventTeamCapturedPoint(Team.Horde, _point, controlZone);
        }

        public override void HandleProgressEventAlliance(GameObject controlZone)
        {
            _battleground.EventTeamCapturedPoint(Team.Alliance, _point, controlZone);
        }

        public override void HandleNeutralEventHorde(GameObject controlZone)
        {
            _battleground.EventTeamLostPoint(Team.Horde, _point, controlZone);
        }

        public override void HandleNeutralEventAlliance(GameObject controlZone)
        {
            _battleground.EventTeamLostPoint(Team.Alliance, _point, controlZone);
        }
    }

    struct Misc
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

        public static BgEyeOfStormPointIconsStruct[] m_PointsIconStruct =
        {
            new BgEyeOfStormPointIconsStruct(WorldStateIds.FelReaverUncontrol, WorldStateIds.FelReaverAllianceControl, WorldStateIds.FelReaverHordeControl, WorldStateIds.FelReaverAllianceControlState, WorldStateIds.FelReaverHordeControlState),
            new BgEyeOfStormPointIconsStruct(WorldStateIds.BloodElfUncontrol, WorldStateIds.BloodElfAllianceControl, WorldStateIds.BloodElfHordeControl, WorldStateIds.BloodElfAllianceControlState, WorldStateIds.BloodElfHordeControlState),
            new BgEyeOfStormPointIconsStruct(WorldStateIds.DraeneiRuinsUncontrol, WorldStateIds.DraeneiRuinsAllianceControl, WorldStateIds.DraeneiRuinsHordeControl, WorldStateIds.DraeneiRuinsAllianceControlState, WorldStateIds.DraeneiRuinsHordeControlState),
            new BgEyeOfStormPointIconsStruct(WorldStateIds.MageTowerUncontrol, WorldStateIds.MageTowerAllianceControl, WorldStateIds.MageTowerHordeControl, WorldStateIds.MageTowerAllianceControlState, WorldStateIds.MageTowerHordeControlState)
        };
        public static BgEyeOfStormLosingPointStruct[] m_LosingPointTypes =
        {
            new BgEyeOfStormLosingPointStruct(ObjectTypes.NBannerFelReaverCenter, ObjectTypes.ABannerFelReaverCenter, BroadcastTextIds.AllianceLostFelReaverRuins, ObjectTypes.HBannerFelReaverCenter, BroadcastTextIds.HordeLostFelReaverRuins),
            new BgEyeOfStormLosingPointStruct(ObjectTypes.NBannerBloodElfCenter, ObjectTypes.ABannerBloodElfCenter, BroadcastTextIds.AllianceLostBloodElfTower, ObjectTypes.HBannerBloodElfCenter, BroadcastTextIds.HordeLostBloodElfTower),
            new BgEyeOfStormLosingPointStruct(ObjectTypes.NBannerDraeneiRuinsCenter, ObjectTypes.ABannerDraeneiRuinsCenter, BroadcastTextIds.AllianceLostDraeneiRuins, ObjectTypes.HBannerDraeneiRuinsCenter, BroadcastTextIds.HordeLostDraeneiRuins),
            new BgEyeOfStormLosingPointStruct(ObjectTypes.NBannerMageTowerCenter, ObjectTypes.ABannerMageTowerCenter, BroadcastTextIds.AllianceLostMageTower, ObjectTypes.HBannerMageTowerCenter, BroadcastTextIds.HordeLostMageTower)
        };
        public static BgEyeOfStormCapturingPointStruct[] m_CapturingPointTypes =
        {
            new BgEyeOfStormCapturingPointStruct(ObjectTypes.NBannerFelReaverCenter, ObjectTypes.ABannerFelReaverCenter, BroadcastTextIds.AllianceTakenFelReaverRuins, ObjectTypes.HBannerFelReaverCenter, BroadcastTextIds.HordeTakenFelReaverRuins, GaveyardIds.FelReaver),
            new BgEyeOfStormCapturingPointStruct(ObjectTypes.NBannerBloodElfCenter, ObjectTypes.ABannerBloodElfCenter, BroadcastTextIds.AllianceTakenBloodElfTower, ObjectTypes.HBannerBloodElfCenter, BroadcastTextIds.HordeTakenBloodElfTower, GaveyardIds.BloodElf),
            new BgEyeOfStormCapturingPointStruct(ObjectTypes.NBannerDraeneiRuinsCenter, ObjectTypes.ABannerDraeneiRuinsCenter, BroadcastTextIds.AllianceTakenDraeneiRuins, ObjectTypes.HBannerDraeneiRuinsCenter, BroadcastTextIds.HordeTakenDraeneiRuins, GaveyardIds.DraeneiRuins),
            new BgEyeOfStormCapturingPointStruct(ObjectTypes.NBannerMageTowerCenter, ObjectTypes.ABannerMageTowerCenter, BroadcastTextIds.AllianceTakenMageTower, ObjectTypes.HBannerMageTowerCenter, BroadcastTextIds.HordeTakenMageTower, GaveyardIds.MageTower)
        };
    }

    struct BroadcastTextIds
    {
        public const uint AllianceTakenFelReaverRuins = 17828;
        public const uint HordeTakenFelReaverRuins = 17829;
        public const uint AllianceLostFelReaverRuins = 17835;
        public const uint HordeLostFelReaverRuins = 17836;

        public const uint AllianceTakenBloodElfTower = 17819;
        public const uint HordeTakenBloodElfTower = 17823;
        public const uint AllianceLostBloodElfTower = 17831;
        public const uint HordeLostBloodElfTower = 17832;

        public const uint AllianceTakenDraeneiRuins = 17827;
        public const uint HordeTakenDraeneiRuins = 17826;
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

    struct WorldStateIds
    {
        public const uint AllianceResources = 1776;
        public const uint HordeResources = 1777;
        public const uint MaxResources = 1780;
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
        public const uint NetherstormFlag = 8863;
        //Set To 2 When Flag Is Picked Up; And To 1 If It Is Dropped
        public const uint NetherstormFlagStateAlliance = 9808;
        public const uint NetherstormFlagStateHorde = 9809;

        public const uint DraeneiRuinsHordeControlState = 17362;
        public const uint DraeneiRuinsAllianceControlState = 17366;
        public const uint MageTowerHordeControlState = 17361;
        public const uint MageTowerAllianceControlState = 17368;
        public const uint FelReaverHordeControlState = 17364;
        public const uint FelReaverAllianceControlState = 17367;
        public const uint BloodElfHordeControlState = 17363;
        public const uint BloodElfAllianceControlState = 17365;
    }

    enum ProgressBarConsts
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

    struct SoundIds
    {
        //strange ids, but sure about them
        public const uint FlagPickedUpAlliance = 8212;
        public const uint FlagCapturedHorde = 8213;
        public const uint FlagPickedUpHorde = 8174;
        public const uint FlagCapturedAlliance = 8173;
        public const uint FlagReset = 8192;
    }

    struct ObjectIds
    {
        public const uint ADoorEyEntry = 184719;           //Alliance Door
        public const uint HDoorEyEntry = 184720;           //Horde Door
        public const uint Flag1EyEntry = 184493;           //Netherstorm Flag (Generic)
        public const uint Flag2EyEntry = 208977;           //Netherstorm Flag (Flagstand)
        public const uint ABannerEyEntry = 184381;           //Visual Banner (Alliance)
        public const uint HBannerEyEntry = 184380;           //Visual Banner (Horde)
        public const uint NBannerEyEntry = 184382;           //Visual Banner (Neutral)
        public const uint BeTowerCapEyEntry = 184080;           //Be Tower Cap Pt
        public const uint FrTowerCapEyEntry = 184081;           //Fel Reaver Cap Pt
        public const uint HuTowerCapEyEntry = 184082;           //Human Tower Cap Pt
        public const uint DrTowerCapEyEntry = 184083;           //Draenei Tower Cap Pt
        public const uint SpeedBuffFelReaverEyEntry = 184970;
        public const uint RestorationBuffFelReaverEyEntry = 184971;
        public const uint BerserkBuffFelReaverEyEntry = 184972;
        public const uint SpeedBuffBloodElfEyEntry = 184964;
        public const uint RestorationBuffBloodElfEyEntry = 184965;
        public const uint BerserkBuffBloodElfEyEntry = 184966;
        public const uint SpeedBuffDraeneiRuinsEyEntry = 184976;
        public const uint RestorationBuffDraeneiRuinsEyEntry = 184977;
        public const uint BerserkBuffDraeneiRuinsEyEntry = 184978;
        public const uint SpeedBuffMageTowerEyEntry = 184973;
        public const uint RestorationBuffMageTowerEyEntry = 184974;
        public const uint BerserkBuffMageTowerEyEntry = 184975;
    }

    struct PointsTrigger
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

    struct GaveyardIds
    {
        public const int MainAlliance = 1103;
        public const uint MainHorde = 1104;
        public const uint FelReaver = 1105;
        public const uint BloodElf = 1106;
        public const uint DraeneiRuins = 1107;
        public const uint MageTower = 1108;
    }

    struct Points
    {
        public const int FelReaver = 0;
        public const int BloodElf = 1;
        public const int DraeneiRuins = 2;
        public const int MageTower = 3;

        public const int PlayersOutOfPoints = 4;
        public const int PointsMax = 4;
    }

    struct CreaturesTypes
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

    struct ObjectTypes
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

    struct ScoreIds
    {
        public const uint WarningNearVictoryScore = 1400;
        public const uint MaxTeamScore = 1500;
    }

    enum FlagState
    {
        OnBase = 0,
        WaitRespawn = 1,
        OnPlayer = 2,
        OnGround = 3
    }

    enum PointState
    {
        NoOwner = 0,
        Uncontrolled = 0,
        UnderControl = 3
    }
}
