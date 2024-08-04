// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.ArthiBasin
{
    enum PvpStats
    {
        BasesAssaulted = 926,
        BasesDefended = 927,
    }

    enum EventIds
    {
        StartBattle = 9158, // Achievement: Let's Get This Done

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

    enum BroadcastTexts
    {
        AllianceNearVictory = 10598,
        HordeNearVictory = 10599,
    }

    enum CreatureIds
    {
        TheBlackBride = 150501,
        RadulfLeder = 150505
    }

    enum GameObjectIds
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

    enum WorldStateIds
    {
        OccupiedBasesHorde = 1778,
        OccupiedBasesAlly = 1779,
        ResourcesAlly = 1776,
        ResourcesHorde = 1777,
        ResourcesMax = 1780,
        ResourcesWarning = 1955,

        StableIcon = 1842,             // Stable Map Icon (None)
        StableStateAlience = 1767,             // Stable Map State (Alience)
        StableStateHorde = 1768,             // Stable Map State (Horde)
        StableStateConAli = 1769,             // Stable Map State (Con Alience)
        StableStateConHor = 1770,             // Stable Map State (Con Horde)
        FarmIcon = 1845,             // Farm Map Icon (None)
        FarmStateAlience = 1772,             // Farm State (Alience)
        FarmStateHorde = 1773,             // Farm State (Horde)
        FarmStateConAli = 1774,             // Farm State (Con Alience)
        FarmStateConHor = 1775,             // Farm State (Con Horde)
        BlacksmithIcon = 1846,             // Blacksmith Map Icon (None)
        BlacksmithStateAlience = 1782,             // Blacksmith Map State (Alience)
        BlacksmithStateHorde = 1783,             // Blacksmith Map State (Horde)
        BlacksmithStateConAli = 1784,             // Blacksmith Map State (Con Alience)
        BlacksmithStateConHor = 1785,             // Blacksmith Map State (Con Horde)
        LumbermillIcon = 1844,             // Lumber Mill Map Icon (None)
        LumbermillStateAlience = 1792,             // Lumber Mill Map State (Alience)
        LumbermillStateHorde = 1793,             // Lumber Mill Map State (Horde)
        LumbermillStateConAli = 1794,             // Lumber Mill Map State (Con Alience)
        LumbermillStateConHor = 1795,             // Lumber Mill Map State (Con Horde)
        GoldmineIcon = 1843,             // Gold Mine Map Icon (None)
        GoldmineStateAlience = 1787,             // Gold Mine Map State (Alience)
        GoldmineStateHorde = 1788,             // Gold Mine Map State (Horde)
        GoldmineStateConAli = 1789,             // Gold Mine Map State (Con Alience
        GoldmineStateConHor = 1790,             // Gold Mine Map State (Con Horde)

        Had500DisadvantageAlliance = 3644,
        Had500DisadvantageHorde = 3645,

        FarmIconNew = 8808,             // Farm Map Icon
        LumberMillIconNew = 8805,             // Lumber Mill Map Icon
        BlacksmithIconNew = 8799,             // Blacksmith Map Icon
        GoldMineIconNew = 8809,             // Gold Mine Map Icon
        StablesIconNew = 5834,             // Stable Map Icon

        FarmHordeControlState = 17328,
        FarmAllianceControlState = 17325,
        LumberMillHordeControlState = 17330,
        LumberMillAllianceControlState = 17326,
        BlacksmithHordeControlState = 17327,
        BlacksmithAllianceControlState = 17324,
        GoldMineHordeControlState = 17329,
        GoldMineAllianceControlState = 17323,
        StablesHordeControlState = 17331,
        StablesAllianceControlState = 17322,
    }

    struct Misc
    {
        public const int WarningNearVictoryScore = 1200;
        public const int MaxTeamScore = 1500;

        // Tick intervals and given points: case 0, 1, 2, 3, 4, 5 captured nodes
        public static uint TickInterval = 2000;
        public static uint[] TickPoints = { 0, 2, 3, 4, 7, 60 };
        public static uint NormalHonorTicks = 160;
        public static uint WeekendHonorTicks = 260;
        public static uint NormalReputationTicks = 120;
        public static uint WeekendReputationTicks = 160;
    }

    [Script(nameof(battleground_arathi_basin), 2107)]
    class battleground_arathi_basin : BattlegroundScript
    {
        uint _lastTick;
        uint[] _honorScoreTics = new uint[SharedConst.PvpTeamsCount];
        uint[] _reputationScoreTics = new uint[SharedConst.PvpTeamsCount];
        bool _isInformedNearVictory;
        uint _honorTics;
        uint _reputationTics;

        List<ObjectGuid> _gameobjectsToRemoveOnMatchStart = new();
        List<ObjectGuid> _creaturesToRemoveOnMatchStart = new();
        List<ObjectGuid> _doors = new();
        List<ObjectGuid> _capturePoints = new();

        public battleground_arathi_basin(BattlegroundMap map) : base(map)
        {
            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(battleground.GetTypeID());

            _honorTics = isBGWeekend ? Misc.WeekendHonorTicks : Misc.NormalHonorTicks;
            _reputationTics = isBGWeekend ? Misc.WeekendReputationTicks : Misc.NormalReputationTicks;
            _honorScoreTics = [0, 0];
            _reputationScoreTics = [0, 0];
        }

        public override void OnInit()
        {
            base.OnInit();

            UpdateWorldState((int)WorldStateIds.ResourcesMax, Misc.MaxTeamScore);
            UpdateWorldState((int)WorldStateIds.ResourcesWarning, Misc.WarningNearVictoryScore);
        }

        public override void OnUpdate(uint diff)
        {
            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                return;

            // Accumulate points
            _lastTick += diff;
            if (_lastTick > Misc.TickInterval)
            {
                _lastTick -= Misc.TickInterval;

                _CalculateTeamNodes(out byte ally, out byte horde);
                byte[] points = { ally, horde };

                for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                {
                    if (points[team] == 0)
                        continue;

                    battleground.AddPoint(team == BattleGroundTeamId.Horde ? Team.Horde : Team.Alliance, Misc.TickPoints[points[team]]);
                    _honorScoreTics[team] += Misc.TickPoints[points[team]];
                    _reputationScoreTics[team] += Misc.TickPoints[points[team]];

                    if (_reputationScoreTics[team] >= _reputationTics)
                    {
                        if (team == BattleGroundTeamId.Alliance)
                            battleground.RewardReputationToTeam(509, 10, Team.Alliance);
                        else
                            battleground.RewardReputationToTeam(510, 10, Team.Horde);
                        _reputationScoreTics[team] -= _reputationTics;
                    }

                    if (_honorScoreTics[team] >= _honorTics)
                    {
                        battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), (team == BattleGroundTeamId.Alliance) ? Team.Alliance : Team.Horde);
                        _honorScoreTics[team] -= _honorTics;
                    }

                    uint teamScore = battleground.GetTeamScore(team);
                    if (!_isInformedNearVictory && teamScore > Misc.WarningNearVictoryScore)
                    {
                        if (team == BattleGroundTeamId.Alliance)
                        {
                            battleground.SendBroadcastText((uint)BroadcastTexts.AllianceNearVictory, ChatMsg.BgSystemNeutral);
                            battleground.PlaySoundToAll((uint)SoundIds.NearVictoryAlliance);
                        }
                        else
                        {
                            battleground.SendBroadcastText((uint)BroadcastTexts.HordeNearVictory, ChatMsg.BgSystemNeutral);
                            battleground.PlaySoundToAll((uint)SoundIds.NearVictoryHorde);
                        }
                        _isInformedNearVictory = true;
                    }

                    if (teamScore > Misc.MaxTeamScore)
                        battleground.SetTeamPoint(team == BattleGroundTeamId.Horde ? Team.Horde : Team.Alliance, Misc.MaxTeamScore);

                    if (team == BattleGroundTeamId.Alliance)
                        UpdateWorldState((int)WorldStateIds.ResourcesAlly, (int)teamScore);
                    else
                        UpdateWorldState((int)WorldStateIds.ResourcesHorde, (int)teamScore);

                    // update achievement flags
                    // we increased m_TeamScores[team] so we just need to check if it is 500 more than other teams resources
                    int otherTeam = (team + 1) % SharedConst.PvpTeamsCount;
                    if (teamScore > battleground.GetTeamScore(otherTeam) + 500)
                    {
                        if (team == BattleGroundTeamId.Alliance)
                            UpdateWorldState((int)WorldStateIds.Had500DisadvantageHorde, 1);
                        else
                            UpdateWorldState((int)WorldStateIds.Had500DisadvantageAlliance, 1);
                    }
                }

                UpdateWorldState((int)WorldStateIds.OccupiedBasesAlly, ally);
                UpdateWorldState((int)WorldStateIds.OccupiedBasesHorde, horde);
            }

            // Test win condition
            if (battleground.GetTeamScore(BattleGroundTeamId.Alliance) >= Misc.MaxTeamScore && battleground.GetTeamScore(BattleGroundTeamId.Horde) >= Misc.MaxTeamScore)
                battleground.EndBattleground(Team.Other); // draw
            else if (battleground.GetTeamScore(BattleGroundTeamId.Alliance) >= Misc.MaxTeamScore)
                battleground.EndBattleground(Team.Alliance);
            else if (battleground.GetTeamScore(BattleGroundTeamId.Horde) >= Misc.MaxTeamScore)
                battleground.EndBattleground(Team.Horde);
        }

        public override void OnStart()
        {
            // Achievement: Let's Get This Done
            TriggerGameEvent((uint)EventIds.StartBattle);
        }

        void _CalculateTeamNodes(out byte alliance, out byte horde)
        {
            alliance = 0;
            horde = 0;

            foreach (ObjectGuid guid in _capturePoints)
            {
                GameObject capturePoint = battlegroundMap.GetGameObject(guid);
                if (capturePoint != null)
                {
                    int wsValue = battlegroundMap.GetWorldStateValue((int)capturePoint.GetGoInfo().CapturePoint.worldState1);
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

        public override Team GetPrematureWinner()
        {
            // How many bases each team owns
            _CalculateTeamNodes(out byte ally, out byte horde);

            if (ally > horde)
                return Team.Alliance;
            if (horde > ally)
                return Team.Horde;

            // If the values are equal, fall back to the original result (based on number of players on each team)
            return base.GetPrematureWinner();
        }

        public override void ProcessEvent(WorldObject source, uint eventId, WorldObject invoker)
        {
            Player player = invoker.ToPlayer();

            switch ((EventIds)eventId)
            {
                case EventIds.StartBattle:
                {
                    foreach (ObjectGuid guid in _creaturesToRemoveOnMatchStart)
                    {
                        Creature creature = battlegroundMap.GetCreature(guid);
                        if (creature != null)
                            creature.DespawnOrUnsummon();
                    }

                    foreach (ObjectGuid guid in _gameobjectsToRemoveOnMatchStart)
                    {
                        GameObject gameObject = battlegroundMap.GetGameObject(guid);
                        if (gameObject != null)
                            gameObject.DespawnOrUnsummon();
                    }

                    foreach (ObjectGuid guid in _doors)
                    {
                        GameObject gameObject = battlegroundMap.GetGameObject(guid);
                        if (gameObject != null)
                        {
                            gameObject.UseDoorOrButton();
                            gameObject.DespawnOrUnsummon(TimeSpan.FromSeconds(3));
                        }
                    }
                    break;
                }
                case EventIds.ContestedBlacksmithAlliance:
                    UpdateWorldState((int)WorldStateIds.BlacksmithAllianceControlState, 1);
                    UpdateWorldState((int)WorldStateIds.BlacksmithHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedBlacksmithAlliance:
                    UpdateWorldState((int)WorldStateIds.BlacksmithAllianceControlState, 2);
                    UpdateWorldState((int)WorldStateIds.BlacksmithHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureBlacksmithAlliance:
                    UpdateWorldState((int)WorldStateIds.BlacksmithAllianceControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case EventIds.ContestedBlacksmithHorde:
                    UpdateWorldState((int)WorldStateIds.BlacksmithAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.BlacksmithHordeControlState, 1);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedBlacksmithHorde:
                    UpdateWorldState((int)WorldStateIds.BlacksmithAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.BlacksmithHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureBlacksmithHorde:
                    UpdateWorldState((int)WorldStateIds.BlacksmithHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                case EventIds.ContestedFarmAlliance:
                    UpdateWorldState((int)WorldStateIds.FarmAllianceControlState, 1);
                    UpdateWorldState((int)WorldStateIds.FarmHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedFarmAlliance:
                    UpdateWorldState((int)WorldStateIds.FarmAllianceControlState, 2);
                    UpdateWorldState((int)WorldStateIds.FarmHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureFarmAlliance:
                    UpdateWorldState((int)WorldStateIds.FarmAllianceControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case EventIds.ContestedFarmHorde:
                    UpdateWorldState((int)WorldStateIds.FarmAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.FarmHordeControlState, 1);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedFarmHorde:
                    UpdateWorldState((int)WorldStateIds.FarmAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.FarmHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureFarmHorde:
                    UpdateWorldState((int)WorldStateIds.FarmHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                case EventIds.ContestedGoldMineAlliance:
                    UpdateWorldState((int)WorldStateIds.GoldMineAllianceControlState, 1);
                    UpdateWorldState((int)WorldStateIds.GoldMineHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedGoldMineAlliance:
                    UpdateWorldState((int)WorldStateIds.GoldMineAllianceControlState, 2);
                    UpdateWorldState((int)WorldStateIds.GoldMineHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureGoldMineAlliance:
                    UpdateWorldState((int)WorldStateIds.GoldMineAllianceControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case EventIds.ContestedGoldMineHorde:
                    UpdateWorldState((int)WorldStateIds.GoldMineAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.GoldMineHordeControlState, 1);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedGoldMineHorde:
                    UpdateWorldState((int)WorldStateIds.GoldMineAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.GoldMineHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureGoldMineHorde:
                    UpdateWorldState((int)WorldStateIds.GoldMineHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                case EventIds.ContestedLumberMillAlliance:
                    UpdateWorldState((int)WorldStateIds.LumberMillAllianceControlState, 1);
                    UpdateWorldState((int)WorldStateIds.LumberMillHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedLumberMillAlliance:
                    UpdateWorldState((int)WorldStateIds.LumberMillAllianceControlState, 2);
                    UpdateWorldState((int)WorldStateIds.LumberMillHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureLumberMillAlliance:
                    UpdateWorldState((int)WorldStateIds.LumberMillAllianceControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case EventIds.ContestedLumberMillHorde:
                    UpdateWorldState((int)WorldStateIds.LumberMillAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.LumberMillHordeControlState, 1);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedLumberMillHorde:
                    UpdateWorldState((int)WorldStateIds.LumberMillAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.LumberMillHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureLumberMillHorde:
                    UpdateWorldState((int)WorldStateIds.LumberMillHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    break;
                case EventIds.ContestedStablesAlliance:
                    UpdateWorldState((int)WorldStateIds.StablesAllianceControlState, 1);
                    UpdateWorldState((int)WorldStateIds.StablesHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedStablesAlliance:
                    UpdateWorldState((int)WorldStateIds.StablesAllianceControlState, 2);
                    UpdateWorldState((int)WorldStateIds.StablesHordeControlState, 0);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureStablesAlliance:
                    UpdateWorldState((int)WorldStateIds.StablesAllianceControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedAlliance);
                    break;
                case EventIds.ContestedStablesHorde:
                    UpdateWorldState((int)WorldStateIds.StablesAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.StablesHordeControlState, 1);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeAssaultedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);
                    break;
                case EventIds.DefendedStablesHorde:
                    UpdateWorldState((int)WorldStateIds.StablesAllianceControlState, 0);
                    UpdateWorldState((int)WorldStateIds.StablesHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
                    if (player != null)
                        battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);
                    break;
                case EventIds.CaptureStablesHorde:
                    UpdateWorldState((int)WorldStateIds.StablesHordeControlState, 2);
                    battleground.PlaySoundToAll((uint)SoundIds.NodeCapturedHorde);
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

            switch ((GameObjectIds)gameObject.GetEntry())
            {
                case GameObjectIds.GhostGate:
                    _gameobjectsToRemoveOnMatchStart.Add(gameObject.GetGUID());
                    break;
                case GameObjectIds.AllianceDoor:
                case GameObjectIds.HordeDoor:
                    _doors.Add(gameObject.GetGUID());
                    break;
                default:
                    break;
            }
        }

        public override void OnEnd(Team winner)
        {
            base.OnEnd(winner);

            // Win reward
            if (winner == Team.Alliance)
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Alliance);
            if (winner == Team.Horde)
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Horde);
            // Complete map_end rewards (even if no team wins)
            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Horde);
            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Alliance);
        }
    }
}