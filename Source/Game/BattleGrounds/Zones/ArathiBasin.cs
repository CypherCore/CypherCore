// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones.ArathisBasin
{
    class BgArathiBasin : Battleground
    {
        TimeTracker _pointsTimer;
        uint[] m_HonorScoreTics = new uint[SharedConst.PvpTeamsCount];
        uint[] m_ReputationScoreTics = new uint[SharedConst.PvpTeamsCount];
        bool m_IsInformedNearVictory;
        uint m_HonorTics;
        uint m_ReputationTics;

        List<ObjectGuid> _gameobjectsToRemoveOnMatchStart = new();
        List<ObjectGuid> _creaturesToRemoveOnMatchStart = new();
        List<ObjectGuid> _doors = new();
        List<ObjectGuid> _capturePoints = new();

        public BgArathiBasin(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            m_IsInformedNearVictory = false;
            _pointsTimer = new TimeTracker(MiscConst.TickInterval);

            for (byte i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                m_HonorScoreTics[i] = 0;
                m_ReputationScoreTics[i] = 0;
            }

            m_HonorTics = 0;
            m_ReputationTics = 0;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                // Accumulate points
                _pointsTimer.Update(diff);
                if (_pointsTimer.Passed())
                {
                    _pointsTimer.Reset(MiscConst.TickInterval);

                    _CalculateTeamNodes(out var ally, out var horde);
                    int[] points = [ally, horde];

                    for (int team = 0; team < SharedConst.PvpTeamsCount; ++team)
                    {
                        if (points[team] == 0)
                            continue;

                        m_TeamScores[team] += MiscConst.TickPoints[points[team]];
                        m_HonorScoreTics[team] += MiscConst.TickPoints[points[team]];
                        m_ReputationScoreTics[team] += MiscConst.TickPoints[points[team]];

                        if (m_ReputationScoreTics[team] >= m_ReputationTics)
                        {
                            if (team == BattleGroundTeamId.Alliance)
                                RewardReputationToTeam(509, 10, Team.Alliance);
                            else
                                RewardReputationToTeam(510, 10, Team.Horde);

                            m_ReputationScoreTics[team] -= m_ReputationTics;
                        }

                        if (m_HonorScoreTics[team] >= m_HonorTics)
                        {
                            RewardHonorToTeam(GetBonusHonorFromKill(1), (team == BattleGroundTeamId.Alliance) ? Team.Alliance : Team.Horde);
                            m_HonorScoreTics[team] -= m_HonorTics;
                        }

                        if (!m_IsInformedNearVictory && m_TeamScores[team] > MiscConst.WarningNearVictoryScore)
                        {
                            if (team == BattleGroundTeamId.Alliance)
                            {
                                SendBroadcastText((uint)ABBattlegroundBroadcastTexts.AllianceNearVictory, ChatMsg.BgSystemNeutral);
                                PlaySoundToAll((uint)SoundIds.NearVictoryAlliance);
                            }
                            else
                            {
                                SendBroadcastText((uint)ABBattlegroundBroadcastTexts.HordeNearVictory, ChatMsg.BgSystemNeutral);
                                PlaySoundToAll((uint)SoundIds.NearVictoryHorde);
                            }
                            m_IsInformedNearVictory = true;
                        }

                        if (m_TeamScores[team] > MiscConst.MaxTeamScore)
                            m_TeamScores[team] = MiscConst.MaxTeamScore;

                        if (team == BattleGroundTeamId.Alliance)
                            UpdateWorldState(WorldStateIds.ResourcesAlly, (int)m_TeamScores[team]);
                        else
                            UpdateWorldState(WorldStateIds.ResourcesHorde, (int)m_TeamScores[team]);
                        // update achievement flags
                        // we increased m_TeamScores[team] so we just need to check if it is 500 more than other teams resources
                        int otherTeam = (team + 1) % SharedConst.PvpTeamsCount;
                        if (m_TeamScores[team] > m_TeamScores[otherTeam] + 500)
                        {
                            if (team == BattleGroundTeamId.Alliance)
                                UpdateWorldState(WorldStateIds.Had500DisadvantageHorde, 1);
                            else
                                UpdateWorldState(WorldStateIds.Had500DisadvantageAlliance, 1);
                        }
                    }

                    UpdateWorldState(WorldStateIds.OccupiedBasesAlly, ally);
                    UpdateWorldState(WorldStateIds.OccupiedBasesHorde, horde);
                }

                // Test win condition
                if (m_TeamScores[BattleGroundTeamId.Alliance] >= MiscConst.MaxTeamScore)
                    EndBattleground(Team.Alliance);
                else if (m_TeamScores[BattleGroundTeamId.Horde] >= MiscConst.MaxTeamScore)
                    EndBattleground(Team.Horde);
            }
        }

        public override void StartingEventOpenDoors()
        {
            // Achievement: Let's Get This Done
            TriggerGameEvent((uint)ABEventIds.StartBattle);
        }

        void _CalculateTeamNodes(out int alliance, out int horde)
        {
            alliance = 0;
            horde = 0;

            BattlegroundMap map = FindBgMap();
            if (map != null)
            {
                foreach (ObjectGuid guid in _capturePoints)
                {
                    GameObject capturePoint = map.GetGameObject(guid);
                    if (capturePoint != null)
                    {
                        int wsValue = map.GetWorldStateValue((int)capturePoint.GetGoInfo().CapturePoint.worldState1);
                        switch ((BattlegroundCapturePointState)wsValue)
                        {
                            case BattlegroundCapturePointState.AllianceCaptured:
                                ++alliance;
                                break;
                            case BattlegroundCapturePointState.HordeCaptured:
                                ++horde;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        public override Team GetPrematureWinner()
        {
            // How many bases each team owns
            _CalculateTeamNodes(out var ally, out var horde);

            if (ally > horde)
                return Team.Alliance;
            else if (horde > ally)
                return Team.Horde;

            // If the values are equal, fall back to the original result (based on number of players on each team)
            return base.GetPrematureWinner();
        }

        public override void ProcessEvent(WorldObject source, uint eventId, WorldObject invoker)
        {
            Player player = invoker.ToPlayer();

            switch ((ABEventIds)eventId)
            {
                case ABEventIds.StartBattle:
                {
                    foreach (ObjectGuid guid in _creaturesToRemoveOnMatchStart)
                    {
                        Creature creature = GetBgMap().GetCreature(guid);
                        if (creature != null)
                            creature.DespawnOrUnsummon();
                    }

                    foreach (ObjectGuid guid in _gameobjectsToRemoveOnMatchStart)
                    {
                        GameObject gameObject = GetBgMap().GetGameObject(guid);
                        if (gameObject != null)
                            gameObject.DespawnOrUnsummon();
                    }

                    foreach (ObjectGuid guid in _doors)
                    {
                        GameObject gameObject = GetBgMap().GetGameObject(guid);
                        if (gameObject != null)
                        {
                            gameObject.UseDoorOrButton();
                            gameObject.DespawnOrUnsummon(TimeSpan.FromSeconds(3));
                        }
                    }
                    break;
                }
                case ABEventIds.ContestedBlacksmithAlliance:
                    UpdateWorldState(WorldStateIds.BlacksmithAllianceControlState, 1);
                    UpdateWorldState(WorldStateIds.BlacksmithHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedBlacksmithAlliance:
                    UpdateWorldState(WorldStateIds.BlacksmithAllianceControlState, 2);
                    UpdateWorldState(WorldStateIds.BlacksmithHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureBlacksmithAlliance:
                    UpdateWorldState(WorldStateIds.BlacksmithAllianceControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case ABEventIds.ContestedBlacksmithHorde:
                    UpdateWorldState(WorldStateIds.BlacksmithAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.BlacksmithHordeControlState, 1);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedBlacksmithHorde:
                    UpdateWorldState(WorldStateIds.BlacksmithAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.BlacksmithHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureBlacksmithHorde:
                    UpdateWorldState(WorldStateIds.BlacksmithHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                case ABEventIds.ContestedFarmAlliance:
                    UpdateWorldState(WorldStateIds.FarmAllianceControlState, 1);
                    UpdateWorldState(WorldStateIds.FarmHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedFarmAlliance:
                    UpdateWorldState(WorldStateIds.FarmAllianceControlState, 2);
                    UpdateWorldState(WorldStateIds.FarmHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureFarmAlliance:
                    UpdateWorldState(WorldStateIds.FarmAllianceControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case ABEventIds.ContestedFarmHorde:
                    UpdateWorldState(WorldStateIds.FarmAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.FarmHordeControlState, 1);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedFarmHorde:
                    UpdateWorldState(WorldStateIds.FarmAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.FarmHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureFarmHorde:
                    UpdateWorldState(WorldStateIds.FarmHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                case ABEventIds.ContestedGoldMineAlliance:
                    UpdateWorldState(WorldStateIds.GoldMineAllianceControlState, 1);
                    UpdateWorldState(WorldStateIds.GoldMineHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedGoldMineAlliance:
                    UpdateWorldState(WorldStateIds.GoldMineAllianceControlState, 2);
                    UpdateWorldState(WorldStateIds.GoldMineHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureGoldMineAlliance:
                    UpdateWorldState(WorldStateIds.GoldMineAllianceControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case ABEventIds.ContestedGoldMineHorde:
                    UpdateWorldState(WorldStateIds.GoldMineAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.GoldMineHordeControlState, 1);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedGoldMineHorde:
                    UpdateWorldState(WorldStateIds.GoldMineAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.GoldMineHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureGoldMineHorde:
                    UpdateWorldState(WorldStateIds.GoldMineHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                case ABEventIds.ContestedLumberMillAlliance:
                    UpdateWorldState(WorldStateIds.LumberMillAllianceControlState, 1);
                    UpdateWorldState(WorldStateIds.LumberMillHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedLumberMillAlliance:
                    UpdateWorldState(WorldStateIds.LumberMillAllianceControlState, 2);
                    UpdateWorldState(WorldStateIds.LumberMillHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureLumberMillAlliance:
                    UpdateWorldState(WorldStateIds.LumberMillAllianceControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case ABEventIds.ContestedLumberMillHorde:
                    UpdateWorldState(WorldStateIds.LumberMillAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.LumberMillHordeControlState, 1);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedLumberMillHorde:
                    UpdateWorldState(WorldStateIds.LumberMillAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.LumberMillHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureLumberMillHorde:
                    UpdateWorldState(WorldStateIds.LumberMillHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                case ABEventIds.ContestedStablesAlliance:
                    UpdateWorldState(WorldStateIds.StablesAllianceControlState, 1);
                    UpdateWorldState(WorldStateIds.StablesHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedStablesAlliance:
                    UpdateWorldState(WorldStateIds.StablesAllianceControlState, 2);
                    UpdateWorldState(WorldStateIds.StablesHordeControlState, 0);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureStablesAlliance:
                    UpdateWorldState(WorldStateIds.StablesAllianceControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case ABEventIds.ContestedStablesHorde:
                    UpdateWorldState(WorldStateIds.StablesAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.StablesHordeControlState, 1);
                    PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    break;
                case ABEventIds.DefendedStablesHorde:
                    UpdateWorldState(WorldStateIds.StablesAllianceControlState, 0);
                    UpdateWorldState(WorldStateIds.StablesHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        UpdatePvpStat(player, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    break;
                case ABEventIds.CaptureStablesHorde:
                    UpdateWorldState(WorldStateIds.StablesHordeControlState, 2);
                    PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                default:
                    Log.outWarn(LogFilter.Battleground, $"BattlegroundAB::ProcessEvent: Unhandled event {eventId}.");
                    break;
            }
        }

        public override void OnCreatureCreate(Creature creature)
        {
            switch ((CreatureIds)creature.GetEntry())
            {
                case CreatureIds.TheBlackBride:
                case CreatureIds.RadulfLeder:
                    _creaturesToRemoveOnMatchStart.Add(creature.GetGUID());
                    break;
                default:
                    break;
            }
        }

        public override void OnGameObjectCreate(GameObject gameObject)
        {
            if (gameObject.GetGoInfo().type == GameObjectTypes.CapturePoint)
                _capturePoints.Add(gameObject.GetGUID());

            switch ((GameobjectIds)gameObject.GetEntry())
            {
                case GameobjectIds.GhostGate:
                    _gameobjectsToRemoveOnMatchStart.Add(gameObject.GetGUID());
                    break;
                case GameobjectIds.AllianceDoor:
                case GameobjectIds.HordeDoor:
                    _doors.Add(gameObject.GetGUID());
                    break;
                default:
                    break;
            }
        }

        public override bool SetupBattleground()
        {
            UpdateWorldState(WorldStateIds.ResourcesMax, MiscConst.MaxTeamScore);
            UpdateWorldState(WorldStateIds.ResourcesWarning, MiscConst.WarningNearVictoryScore);

            return true;
        }

        public override void Reset()
        {
            //call parent's class reset
            base.Reset();

            for (var i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                m_TeamScores[i] = 0;
                m_HonorScoreTics[i] = 0;
                m_ReputationScoreTics[i] = 0;
            }

            _pointsTimer.Reset(MiscConst.TickInterval);

            m_IsInformedNearVictory = false;
            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
            m_HonorTics = isBGWeekend ? MiscConst.ABBGWeekendHonorTicks : MiscConst.NotABBGWeekendHonorTicks;
            m_ReputationTics = isBGWeekend ? MiscConst.ABBGWeekendReputationTicks : MiscConst.NotABBGWeekendReputationTicks;

            _creaturesToRemoveOnMatchStart.Clear();
            _gameobjectsToRemoveOnMatchStart.Clear();
            _doors.Clear();
            _capturePoints.Clear();
        }

        public override void EndBattleground(Team winner)
        {
            // Win reward
            if (winner == Team.Alliance)
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Alliance);
            if (winner == Team.Horde)
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Horde);
            // Complete map_end rewards (even if no team wins)
            RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Horde);
            RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Alliance);

            base.EndBattleground(winner);
        }

        public override WorldSafeLocsEntry GetClosestGraveyard(Player player)
        {
            return Global.ObjectMgr.GetClosestGraveyard(player.GetWorldLocation(), player.GetTeam(), player);
        }

        public override WorldSafeLocsEntry GetExploitTeleportLocation(Team team)
        {
            return Global.ObjectMgr.GetWorldSafeLoc(team == Team.Alliance ? MiscConst.ExploitTeleportLocationAlliance : MiscConst.ExploitTeleportLocationHorde);
        }
    }

    #region Constants
    struct MiscConst
    {
        public const uint NotABBGWeekendHonorTicks = 260;
        public const uint ABBGWeekendHonorTicks = 160;
        public const uint NotABBGWeekendReputationTicks = 160;
        public const uint ABBGWeekendReputationTicks = 120;

        public const int WarningNearVictoryScore = 1400;
        public const int MaxTeamScore = 1500;

        public const uint ExploitTeleportLocationAlliance = 3705;
        public const uint ExploitTeleportLocationHorde = 3706;

        // Tick intervals and given points: case 0, 1, 2, 3, 4, 5 captured nodes
        public static TimeSpan TickInterval = TimeSpan.FromSeconds(2);
        public static uint[] TickPoints = { 0, 10, 10, 10, 10, 30 };
    }

    enum ABEventIds
    {
        StartBattle = 9158, // Achievement: Let'S Get This Done

        ContestedStablesHorde = 28523,
        CaptureStablesHorde = 28527,
        DefendedStablesHorde = 28525,
        ContestedStablesAlliance = 28522,
        CaptureStablesAlliance = 28526,
        DefendedStablesAlliance = 28524,

        ContestedBlacksmithHorde = 8876,
        CaptureBlacksmithHorde = 8773,
        DefendedBlacksmithHorde = 8770,
        ContestedBlacksmithAlliance = 8874,
        CaptureBlacksmithAlliance = 8769,
        DefendedBlacksmithAlliance = 8774,

        ContestedFarmHorde = 39398,
        CaptureFarmHorde = 39399,
        DefendedFarmHorde = 39400,
        ContestedFarmAlliance = 39401,
        CaptureFarmAlliance = 39402,
        DefendedFarmAlliance = 39403,

        ContestedGoldMineHorde = 39404,
        CaptureGoldMineHorde = 39405,
        DefendedGoldMineHorde = 39406,
        ContestedGoldMineAlliance = 39407,
        CaptureGoldMineAlliance = 39408,
        DefendedGoldMineAlliance = 39409,

        ContestedLumberMillHorde = 39387,
        CaptureLumberMillHorde = 39388,
        DefendedLumberMillHorde = 39389,
        ContestedLumberMillAlliance = 39390,
        CaptureLumberMillAlliance = 39391,
        DefendedLumberMillAlliance = 39392
    }

    struct WorldStateIds
    {
        public const int OccupiedBasesHorde = 1778;
        public const int OccupiedBasesAlly = 1779;
        public const int ResourcesAlly = 1776;
        public const int ResourcesHorde = 1777;
        public const int ResourcesMax = 1780;
        public const int ResourcesWarning = 1955;

        public const int StableIcon = 1842;             // Stable Map Icon (None)
        public const int StableStateAlience = 1767;             // Stable Map State (Alience)
        public const int StableStateHorde = 1768;             // Stable Map State (Horde)
        public const int StableStateConAli = 1769;             // Stable Map State (Con Alience)
        public const int StableStateConHor = 1770;             // Stable Map State (Con Horde)
        public const int FarmIcon = 1845;             // Farm Map Icon (None)
        public const int FarmStateAlience = 1772;             // Farm State (Alience)
        public const int FarmStateHorde = 1773;             // Farm State (Horde)
        public const int FarmStateConAli = 1774;             // Farm State (Con Alience)
        public const int FarmStateConHor = 1775;             // Farm State (Con Horde)
        public const int BlacksmithIcon = 1846;             // Blacksmith Map Icon (None)
        public const int BlacksmithStateAlience = 1782;             // Blacksmith Map State (Alience)
        public const int BlacksmithStateHorde = 1783;             // Blacksmith Map State (Horde)
        public const int BlacksmithStateConAli = 1784;             // Blacksmith Map State (Con Alience)
        public const int BlacksmithStateConHor = 1785;             // Blacksmith Map State (Con Horde)
        public const int LumbermillIcon = 1844;             // Lumber Mill Map Icon (None)
        public const int LumbermillStateAlience = 1792;             // Lumber Mill Map State (Alience)
        public const int LumbermillStateHorde = 1793;             // Lumber Mill Map State (Horde)
        public const int LumbermillStateConAli = 1794;             // Lumber Mill Map State (Con Alience)
        public const int LumbermillStateConHor = 1795;             // Lumber Mill Map State (Con Horde)
        public const int GoldmineIcon = 1843;             // Gold Mine Map Icon (None)
        public const int GoldmineStateAlience = 1787;             // Gold Mine Map State (Alience)
        public const int GoldmineStateHorde = 1788;             // Gold Mine Map State (Horde)
        public const int GoldmineStateConAli = 1789;             // Gold Mine Map State (Con Alience
        public const int GoldmineStateConHor = 1790;             // Gold Mine Map State (Con Horde)

        public const int Had500DisadvantageAlliance = 3644;
        public const int Had500DisadvantageHorde = 3645;

        public const int FarmIconNew = 8808;             // Farm Map Icon
        public const int LumberMillIconNew = 8805;             // Lumber Mill Map Icon
        public const int BlacksmithIconNew = 8799;             // Blacksmith Map Icon
        public const int GoldMineIconNew = 8809;             // Gold Mine Map Icon
        public const int StablesIconNew = 5834;             // Stable Map Icon

        public const int FarmHordeControlState = 17328;
        public const int FarmAllianceControlState = 17325;
        public const int LumberMillHordeControlState = 17330;
        public const int LumberMillAllianceControlState = 17326;
        public const int BlacksmithHordeControlState = 17327;
        public const int BlacksmithAllianceControlState = 17324;
        public const int GoldMineHordeControlState = 17329;
        public const int GoldMineAllianceControlState = 17323;
        public const int StablesHordeControlState = 17331;
        public const int StablesAllianceControlState = 17322;
    }

    // Object id templates from DB
    enum GameobjectIds
    {
        CapturePointStables = 227420,
        CapturePointBlacksmith = 227522,
        CapturePointFarm = 227536,
        CapturePointGoldMine = 227538,
        CapturePointLumberMill = 227544,

        GhostGate = 180322,
        AllianceDoor = 322273,
        HordeDoor = 322274
    }

    enum CreatureIds
    {
        TheBlackBride = 150501,
        RadulfLeder = 150505
    }

    struct ABBattlegroundNodes
    {
        public const int NodeStables = 0;
        public const int NodeBlacksmith = 1;
        public const int NodeFarm = 2;
        public const int NodeLumberMill = 3;
        public const int NodeGoldMine = 4;

        public const int DynamicNodesCount = 5;                        // Dynamic Nodes That Can Be Captured

        public const int SpiritAliance = 5;
        public const int SpiritHorde = 6;

        public const int AllCount = 7;                         // All Nodes (Dynamic And Static)
    }

    enum ABBattlegroundBroadcastTexts
    {
        AllianceNearVictory = 10598,
        HordeNearVictory = 10599
    }

    enum SoundIds
    {
        NodeClaimed = 8192,
        NodeCapturedAlliance = 8173,
        NodeCapturedHorde = 8213,
        NodeAssaultedAlliance = 8212,
        NodeAssaultedHorde = 8174,
        NearVictoryAlliance = 8456,
        NearVictoryHorde = 8457
    }

    enum ArathiBasinPvpStats
    {
        BasesAssaulted = 926,
        BasesDefended = 927,
    }
    #endregion
}
