﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones
{
    class BgArathiBasin : Battleground
    {
        public BgArathiBasin(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            m_IsInformedNearVictory = false;
            BgObjects = new ObjectGuid[ABObjectTypes.Max];
            BgCreatures = new ObjectGuid[ABBattlegroundNodes.AllCount + 5];//+5 for aura triggers

            for (byte i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
            {
                m_Nodes[i] = 0;
                m_prevNodes[i] = 0;
                m_NodeTimers[i] = 0;
                m_BannerTimers[i].timer = 0;
                m_BannerTimers[i].type = 0;
                m_BannerTimers[i].teamIndex = 0;
            }

            for (byte i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                m_lastTick[i] = 0;
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
                int[] team_points = { 0, 0 };

                for (byte node = 0; node < ABBattlegroundNodes.DynamicNodesCount; ++node)
                {
                    // 3 sec delay to spawn new banner instead previous despawned one
                    if (m_BannerTimers[node].timer != 0)
                    {
                        if (m_BannerTimers[node].timer > diff)
                            m_BannerTimers[node].timer -= diff;
                        else
                        {
                            m_BannerTimers[node].timer = 0;
                            _CreateBanner(node, (ABNodeStatus)m_BannerTimers[node].type, m_BannerTimers[node].teamIndex, false);
                        }
                    }

                    // 1-minute to occupy a node from contested state
                    if (m_NodeTimers[node] != 0)
                    {
                        if (m_NodeTimers[node] > diff)
                            m_NodeTimers[node] -= diff;
                        else
                        {
                            m_NodeTimers[node] = 0;
                            // Change from contested to occupied !
                            int teamIndex = (int)m_Nodes[node] - 1;
                            m_prevNodes[node] = m_Nodes[node];
                            m_Nodes[node] += 2;
                            // burn current contested banner
                            _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                            // create new occupied banner
                            _CreateBanner(node, ABNodeStatus.Occupied, teamIndex, true);
                            _SendNodeUpdate(node);
                            _NodeOccupied(node, (teamIndex == BatttleGroundTeamId.Alliance) ? Team.Alliance : Team.Horde);
                            // Message to chatlog

                            if (teamIndex == 0)
                            {
                                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceTaken, ChatMsg.BgSystemAlliance);
                                PlaySoundToAll(SoundCapturedAlliance);
                            }
                            else
                            {
                                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeTaken, ChatMsg.BgSystemHorde);
                                PlaySoundToAll(SoundCapturedHorde);
                            }
                        }
                    }

                    for (int team = 0; team < SharedConst.PvpTeamsCount; ++team)
                        if (m_Nodes[node] == team + ABNodeStatus.Occupied)
                            ++team_points[team];
                }

                // Accumulate points
                for (int team = 0; team < SharedConst.PvpTeamsCount; ++team)
                {
                    int points = team_points[team];
                    if (points == 0)
                        continue;

                    m_lastTick[team] += diff;

                    if (m_lastTick[team] > TickIntervals[points])
                    {
                        m_lastTick[team] -= TickIntervals[points];
                        m_TeamScores[team] += TickPoints[points];
                        m_HonorScoreTics[team] += TickPoints[points];
                        m_ReputationScoreTics[team] += TickPoints[points];

                        if (m_ReputationScoreTics[team] >= m_ReputationTics)
                        {
                            if (team == BatttleGroundTeamId.Alliance)
                                RewardReputationToTeam(509, 10, Team.Alliance);
                            else
                                RewardReputationToTeam(510, 10, Team.Horde);

                            m_ReputationScoreTics[team] -= m_ReputationTics;
                        }

                        if (m_HonorScoreTics[team] >= m_HonorTics)
                        {
                            RewardHonorToTeam(GetBonusHonorFromKill(1), (team == BatttleGroundTeamId.Alliance) ? Team.Alliance : Team.Horde);
                            m_HonorScoreTics[team] -= m_HonorTics;
                        }

                        if (!m_IsInformedNearVictory && m_TeamScores[team] > WarningNearVictoryScore)
                        {
                            if (team == BatttleGroundTeamId.Alliance)
                            {
                                SendBroadcastText(ABBattlegroundBroadcastTexts.AllianceNearVictory, ChatMsg.BgSystemNeutral);
                                PlaySoundToAll(SoundNearVictoryAlliance);
                            }
                            else
                            {
                                SendBroadcastText(ABBattlegroundBroadcastTexts.HordeNearVictory, ChatMsg.BgSystemNeutral);
                                PlaySoundToAll(SoundNearVictoryHorde);
                            }
                            m_IsInformedNearVictory = true;
                        }

                        if (m_TeamScores[team] > MaxTeamScore)
                            m_TeamScores[team] = MaxTeamScore;

                        if (team == BatttleGroundTeamId.Alliance)
                            UpdateWorldState(ABWorldStates.ResourcesAlly, (int)m_TeamScores[team]);
                        else
                            UpdateWorldState(ABWorldStates.ResourcesHorde, (int)m_TeamScores[team]);
                        // update achievement flags
                        // we increased m_TeamScores[team] so we just need to check if it is 500 more than other teams resources
                        int otherTeam = (team + 1) % SharedConst.PvpTeamsCount;
                        if (m_TeamScores[team] > m_TeamScores[otherTeam] + 500)
                        {
                            if (team == BatttleGroundTeamId.Alliance)
                                UpdateWorldState(ABWorldStates.Had500DisadvantageHorde, 1);
                            else
                                UpdateWorldState(ABWorldStates.Had500DisadvantageAlliance, 1);
                        }
                    }
                }

                // Test win condition
                if (m_TeamScores[BatttleGroundTeamId.Alliance] >= MaxTeamScore)
                    EndBattleground(Team.Alliance);
                else if (m_TeamScores[BatttleGroundTeamId.Horde] >= MaxTeamScore)
                    EndBattleground(Team.Horde);
            }
        }

        public override void StartingEventCloseDoors()
        {
            // despawn banners, auras and buffs
            for (int obj = ABObjectTypes.BannerNeutral; obj < ABBattlegroundNodes.DynamicNodesCount * 8; ++obj)
                SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);
            for (int i = 0; i < ABBattlegroundNodes.DynamicNodesCount * 3; ++i)
                SpawnBGObject(ABObjectTypes.SpeedbuffStables + i, BattlegroundConst.RespawnOneDay);

            // Starting doors
            DoorClose(ABObjectTypes.GateA);
            DoorClose(ABObjectTypes.GateH);
            SpawnBGObject(ABObjectTypes.GateA, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(ABObjectTypes.GateH, BattlegroundConst.RespawnImmediately);

            // Starting base spirit guides
            _NodeOccupied(ABBattlegroundNodes.SpiritAliance, Team.Alliance);
            _NodeOccupied(ABBattlegroundNodes.SpiritHorde, Team.Horde);
        }

        public override void StartingEventOpenDoors()
        {
            // spawn neutral banners
            for (int banner = ABObjectTypes.BannerNeutral, i = 0; i < 5; banner += 8, ++i)
                SpawnBGObject(banner, BattlegroundConst.RespawnImmediately);
            for (int i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
            {
                //randomly select buff to spawn
                int buff = RandomHelper.IRand(0, 2);
                SpawnBGObject(ABObjectTypes.SpeedbuffStables + buff + i * 3, BattlegroundConst.RespawnImmediately);
            }
            DoorOpen(ABObjectTypes.GateA);
            DoorOpen(ABObjectTypes.GateH);

            // Achievement: Let's Get This Done
            TriggerGameEvent(EventStartBattle);
        }

        void _CreateBanner(byte node, ABNodeStatus type, int teamIndex, bool delay)
        {
            // Just put it into the queue
            if (delay)
            {
                m_BannerTimers[node].timer = 2000;
                m_BannerTimers[node].type = (byte)type;
                m_BannerTimers[node].teamIndex = (byte)teamIndex;
                return;
            }

            int obj = node * 8 + (byte)type + teamIndex;

            SpawnBGObject(obj, BattlegroundConst.RespawnImmediately);

            // handle aura with banner
            if (type == 0)
                return;
            obj = node * 8 + ((type == ABNodeStatus.Occupied) ? (5 + teamIndex) : 7);
            SpawnBGObject(obj, BattlegroundConst.RespawnImmediately);
        }

        void _DelBanner(byte node, ABNodeStatus type, byte teamIndex)
        {
            int obj = node * 8 + (byte)type + teamIndex;
            SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);

            // handle aura with banner
            if (type == 0)
                return;
            obj = node * 8 + ((type == ABNodeStatus.Occupied) ? (5 + teamIndex) : 7);
            SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);
        }

        void _SendNodeUpdate(byte node)
        {
            // Send node owner state update to refresh map icons on client
            int[] idPlusArray = { 0, 2, 3, 0, 1 };
            int[] statePlusArray = { 0, 2, 0, 2, 0 };

            if (m_prevNodes[node] != 0)
                UpdateWorldState(NodeStates[node] + idPlusArray[(int)m_prevNodes[node]], 0);
            else
                UpdateWorldState(NodeIcons[node], 0);

            UpdateWorldState(NodeStates[node] + idPlusArray[(byte)m_Nodes[node]], 1);

            switch (node)
            {
                case ABBattlegroundNodes.NodeStables:
                    UpdateWorldState(ABWorldStates.StablesIconNew, (int)m_Nodes[node] + statePlusArray[(int)m_Nodes[node]]);
                    UpdateWorldState(ABWorldStates.StablesHordeControlState, m_Nodes[node] == ABNodeStatus.HordeOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.HordeContested ? 1 : 0));
                    UpdateWorldState(ABWorldStates.StablesAllianceControlState, m_Nodes[node] == ABNodeStatus.AllyOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.AllyContested ? 1 : 0));
                    break;
                case ABBattlegroundNodes.NodeBlacksmith:
                    UpdateWorldState(ABWorldStates.BlacksmithIconNew, (int)m_Nodes[node] + statePlusArray[(int)m_Nodes[node]]);
                    UpdateWorldState(ABWorldStates.BlacksmithHordeControlState, m_Nodes[node] == ABNodeStatus.HordeOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.HordeContested ? 1 : 0));
                    UpdateWorldState(ABWorldStates.BlacksmithAllianceControlState, m_Nodes[node] == ABNodeStatus.AllyOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.AllyContested ? 1 : 0));
                    break;
                case ABBattlegroundNodes.NodeFarm:
                    UpdateWorldState(ABWorldStates.FarmIconNew, (int)m_Nodes[node] + statePlusArray[(int)m_Nodes[node]]);
                    UpdateWorldState(ABWorldStates.FarmHordeControlState, m_Nodes[node] == ABNodeStatus.HordeOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.HordeContested ? 1 : 0));
                    UpdateWorldState(ABWorldStates.FarmAllianceControlState, m_Nodes[node] == ABNodeStatus.AllyOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.AllyContested ? 1 : 0));
                    break;
                case ABBattlegroundNodes.NodeLumberMill:
                    UpdateWorldState(ABWorldStates.LumberMillIconNew, (int)m_Nodes[node] + statePlusArray[(int)m_Nodes[node]]);
                    UpdateWorldState(ABWorldStates.LumberMillHordeControlState, m_Nodes[node] == ABNodeStatus.HordeOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.HordeContested ? 1 : 0));
                    UpdateWorldState(ABWorldStates.LumberMillAllianceControlState, m_Nodes[node] == ABNodeStatus.AllyOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.AllyContested ? 1 : 0));
                    break;
                case ABBattlegroundNodes.NodeGoldMine:
                    UpdateWorldState(ABWorldStates.GoldMineIconNew, (int)m_Nodes[node] + statePlusArray[(int)m_Nodes[node]]);
                    UpdateWorldState(ABWorldStates.GoldMineHordeControlState, m_Nodes[node] == ABNodeStatus.HordeOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.HordeContested ? 1 : 0));
                    UpdateWorldState(ABWorldStates.GoldMineAllianceControlState, m_Nodes[node] == ABNodeStatus.AllyOccupied ? 2 : (m_Nodes[node] == ABNodeStatus.AllyContested ? 1 : 0));
                    break;
                default:
                    break;
            }

            // How many bases each team owns
            byte ally = 0, horde = 0;
            for (byte i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
                if (m_Nodes[i] == ABNodeStatus.AllyOccupied)
                    ++ally;
                else if (m_Nodes[i] == ABNodeStatus.HordeOccupied)
                    ++horde;

            UpdateWorldState(ABWorldStates.OccupiedBasesAlly, ally);
            UpdateWorldState(ABWorldStates.OccupiedBasesHorde, horde);
        }

        void _NodeOccupied(byte node, Team team)
        {
            if (!AddSpiritGuide(node, SpiritGuidePos[node], GetTeamIndexByTeamId(team)))
                Log.outError(LogFilter.Battleground, "Failed to spawn spirit guide! point: {0}, team: {1}, ", node, team);

            if (node >= ABBattlegroundNodes.DynamicNodesCount)//only dynamic nodes, no start points
                return;

            byte capturedNodes = 0;
            for (byte i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
                if (m_Nodes[i] == ABNodeStatus.Occupied + GetTeamIndexByTeamId(team) && m_NodeTimers[i] == 0)
                    ++capturedNodes;

            if (capturedNodes >= 5)
                CastSpellOnTeam(BattlegroundConst.AbQuestReward5Bases, team);
            if (capturedNodes >= 4)
                CastSpellOnTeam(BattlegroundConst.AbQuestReward4Bases, team);

            Creature trigger = !BgCreatures[node + 7].IsEmpty() ? GetBGCreature(node + 7u) : null; // 0-6 spirit guides
            if (trigger == null)
                trigger = AddCreature(SharedConst.WorldTrigger, node + 7u, NodePositions[node], GetTeamIndexByTeamId(team));

            //add bonus honor aura trigger creature when node is accupied
            //cast bonus aura (+50% honor in 25yards)
            //aura should only apply to players who have accupied the node, set correct faction for trigger
            if (trigger != null)
            {
                trigger.SetFaction(team == Team.Alliance ? 84u : 83u);
                trigger.CastSpell(trigger, BattlegroundConst.SpellHonorableDefender25y, false);
            }
        }

        void _NodeDeOccupied(byte node)
        {
            //only dynamic nodes, no start points
            if (node >= ABBattlegroundNodes.DynamicNodesCount)
                return;

            //remove bonus honor aura trigger creature when node is lost
            DelCreature(node + 7u);//null checks are in DelCreature! 0-6 spirit guides

            DelCreature(node);

            // buff object isn't despawned
        }

        //Invoked if a player used a banner as a gameobject
        public override void EventPlayerClickedOnFlag(Player source, GameObject target_obj)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            byte node = ABBattlegroundNodes.NodeStables;
            GameObject obj = GetBgMap().GetGameObject(BgObjects[node * 8 + 7]);
            while ((node < ABBattlegroundNodes.DynamicNodesCount) && ((obj == null) || (!source.IsWithinDistInMap(obj, 10))))
            {
                ++node;
                obj = GetBgMap().GetGameObject(BgObjects[node * 8 + ABObjectTypes.AuraContested]);
            }

            if (node == ABBattlegroundNodes.DynamicNodesCount)
            {
                // this means our player isn't close to any of banners - maybe cheater ??
                return;
            }

            int teamIndex = GetTeamIndexByTeamId(GetPlayerTeam(source.GetGUID()));

            // Check if player really could use this banner, not cheated
            if (!(m_Nodes[node] == 0 || teamIndex == (int)m_Nodes[node] % 2))
                return;

            source.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
            uint sound;
            // If node is neutral, change to contested
            if (m_Nodes[node] == ABNodeStatus.Neutral)
            {
                UpdatePvpStat(source, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                m_prevNodes[node] = m_Nodes[node];
                m_Nodes[node] = (ABNodeStatus)(teamIndex + 1);
                // burn current neutral banner
                _DelBanner(node, ABNodeStatus.Neutral, 0);
                // create new contested banner
                _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                _SendNodeUpdate(node);
                m_NodeTimers[node] = FlagCapturingTime;

                // FIXME: team and node names not localized
                if (teamIndex == BatttleGroundTeamId.Alliance)
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceClaims, ChatMsg.BgSystemAlliance, source);
                else
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeClaims, ChatMsg.BgSystemHorde, source);

                sound = SoundClaimed;
            }
            // If node is contested
            else if ((m_Nodes[node] == ABNodeStatus.AllyContested) || (m_Nodes[node] == ABNodeStatus.HordeContested))
            {
                // If last state is NOT occupied, change node to enemy-contested
                if (m_prevNodes[node] < ABNodeStatus.Occupied)
                {
                    UpdatePvpStat(source, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                    m_prevNodes[node] = m_Nodes[node];
                    m_Nodes[node] = (ABNodeStatus.Contested + teamIndex);
                    // burn current contested banner
                    _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                    // create new contested banner
                    _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                    _SendNodeUpdate(node);
                    m_NodeTimers[node] = FlagCapturingTime;

                    if (teamIndex == BatttleGroundTeamId.Alliance)
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceAssaulted, ChatMsg.BgSystemAlliance, source);
                    else
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeAssaulted, ChatMsg.BgSystemHorde, source);
                }
                // If contested, change back to occupied
                else
                {
                    UpdatePvpStat(source, (uint)ArathiBasinPvpStats.BasesDefended, 1);
                    m_prevNodes[node] = m_Nodes[node];
                    m_Nodes[node] = (ABNodeStatus.Occupied + teamIndex);
                    // burn current contested banner
                    _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                    // create new occupied banner
                    _CreateBanner(node, ABNodeStatus.Occupied, (byte)teamIndex, true);
                    _SendNodeUpdate(node);
                    m_NodeTimers[node] = 0;
                    _NodeOccupied(node, (teamIndex == BatttleGroundTeamId.Alliance) ? Team.Alliance : Team.Horde);

                    if (teamIndex == BatttleGroundTeamId.Alliance)
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceDefended, ChatMsg.BgSystemAlliance, source);
                    else
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeDefended, ChatMsg.BgSystemHorde, source);
                }
                sound = (teamIndex == BatttleGroundTeamId.Alliance) ? SoundAssaultedAlliance : SoundAssaultedHorde;
            }
            // If node is occupied, change to enemy-contested
            else
            {
                UpdatePvpStat(source, (uint)ArathiBasinPvpStats.BasesAssaulted, 1);
                m_prevNodes[node] = m_Nodes[node];
                m_Nodes[node] = (ABNodeStatus.Contested + teamIndex);
                // burn current occupied banner
                _DelBanner(node, ABNodeStatus.Occupied, (byte)teamIndex);
                // create new contested banner
                _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                _SendNodeUpdate(node);
                _NodeDeOccupied(node);
                m_NodeTimers[node] = FlagCapturingTime;

                if (teamIndex == BatttleGroundTeamId.Alliance)
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceAssaulted, ChatMsg.BgSystemAlliance, source);
                else
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeAssaulted, ChatMsg.BgSystemHorde, source);

                sound = (teamIndex == BatttleGroundTeamId.Alliance) ? SoundAssaultedAlliance : SoundAssaultedHorde;
            }

            // If node is occupied again, send "X has taken the Y" msg.
            if (m_Nodes[node] >= ABNodeStatus.Occupied)
            {
                // FIXME: team and node names not localized
                if (teamIndex == BatttleGroundTeamId.Alliance)
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceTaken, ChatMsg.BgSystemAlliance);
                else
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeTaken, ChatMsg.BgSystemHorde);
            }
            PlaySoundToAll(sound);
        }

        public override Team GetPrematureWinner()
        {
            // How many bases each team owns
            byte ally = 0, horde = 0;
            for (byte i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
                if (m_Nodes[i] == ABNodeStatus.AllyOccupied)
                    ++ally;
                else if (m_Nodes[i] == ABNodeStatus.HordeOccupied)
                    ++horde;

            if (ally > horde)
                return Team.Alliance;
            else if (horde > ally)
                return Team.Horde;

            // If the values are equal, fall back to the original result (based on number of players on each team)
            return base.GetPrematureWinner();
        }

        public override bool SetupBattleground()
        {
            bool result = true;
            for (int i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
            {
                result &= AddObject(ABObjectTypes.BannerNeutral + 8 * i, (uint)(NodeObjectId.Banner0 + i), NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.BannerContA + 8 * i, ABObjectIds.BannerContA, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.BannerContH + 8 * i, ABObjectIds.BannerContH, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.BannerAlly + 8 * i, ABObjectIds.BannerA, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.BannerHorde + 8 * i, ABObjectIds.BannerH, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.AuraAlly + 8 * i, ABObjectIds.AuraA, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.AuraHorde + 8 * i, ABObjectIds.AuraH, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.AuraContested + 8 * i, ABObjectIds.AuraC, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                if (!result)
                {
                    Log.outError(LogFilter.Sql, "BatteGroundAB: Failed to spawn some object Battleground not created!");
                    return false;
                }
            }

            result &= AddObject(ABObjectTypes.GateA, ABObjectIds.GateA, DoorPositions[0][0], DoorPositions[0][1], DoorPositions[0][2], DoorPositions[0][3], DoorPositions[0][4], DoorPositions[0][5], DoorPositions[0][6], DoorPositions[0][7], BattlegroundConst.RespawnImmediately);
            result &= AddObject(ABObjectTypes.GateH, ABObjectIds.GateH, DoorPositions[1][0], DoorPositions[1][1], DoorPositions[1][2], DoorPositions[1][3], DoorPositions[1][4], DoorPositions[1][5], DoorPositions[1][6], DoorPositions[1][7], BattlegroundConst.RespawnImmediately);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "BatteGroundAB: Failed to spawn door object Battleground not created!");
                return false;
            }

            //buffs
            for (int i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
            {
                result &= AddObject(ABObjectTypes.SpeedbuffStables + 3 * i, Buff_Entries[0], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.SpeedbuffStables + 3 * i + 1, Buff_Entries[1], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.SpeedbuffStables + 3 * i + 2, Buff_Entries[2], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);
                if (!result)
                {
                    Log.outError(LogFilter.Sql, "BatteGroundAB: Failed to spawn buff object!");
                    return false;
                }
            }

            UpdateWorldState(ABWorldStates.ResourcesMax, MaxTeamScore);
            UpdateWorldState(ABWorldStates.ResourcesWarning, WarningNearVictoryScore);

            return true;
        }

        public override void Reset()
        {
            //call parent's class reset
            base.Reset();

            for (var i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                m_TeamScores[i] = 0;
                m_lastTick[i] = 0;
                m_HonorScoreTics[i] = 0;
                m_ReputationScoreTics[i] = 0;
            }

            m_IsInformedNearVictory = false;
            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
            m_HonorTics = (isBGWeekend) ? ABBGWeekendHonorTicks : NotABBGWeekendHonorTicks;
            m_ReputationTics = (isBGWeekend) ? ABBGWeekendReputationTicks : NotABBGWeekendReputationTicks;

            for (byte i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
            {
                m_Nodes[i] = 0;
                m_prevNodes[i] = 0;
                m_NodeTimers[i] = 0;
                m_BannerTimers[i].timer = 0;
            }
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
            int teamIndex = GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID()));

            // Is there any occupied node for this team?
            List<byte> nodes = new();
            for (byte i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
                if (m_Nodes[i] == ABNodeStatus.Occupied + teamIndex)
                    nodes.Add(i);

            WorldSafeLocsEntry good_entry = null;
            // If so, select the closest node to place ghost on
            if (!nodes.Empty())
            {
                float plr_x = player.GetPositionX();
                float plr_y = player.GetPositionY();

                float mindist = 999999.0f;
                for (byte i = 0; i < nodes.Count; ++i)
                {
                    WorldSafeLocsEntry entry = Global.ObjectMgr.GetWorldSafeLoc(GraveyardIds[nodes[i]]);
                    if (entry == null)
                        continue;
                    float dist = (entry.Loc.GetPositionX() - plr_x) * (entry.Loc.GetPositionX() - plr_x) + (entry.Loc.GetPositionY() - plr_y) * (entry.Loc.GetPositionY() - plr_y);
                    if (mindist > dist)
                    {
                        mindist = dist;
                        good_entry = entry;
                    }
                }
                nodes.Clear();
            }
            // If not, place ghost on starting location
            if (good_entry == null)
                good_entry = Global.ObjectMgr.GetWorldSafeLoc(GraveyardIds[teamIndex + 5]);

            return good_entry;
        }

        public override WorldSafeLocsEntry GetExploitTeleportLocation(Team team)
        {
            return Global.ObjectMgr.GetWorldSafeLoc(team == Team.Alliance ? ExploitTeleportLocationAlliance : ExploitTeleportLocationHorde);
        }

        /// <summary>
        /// Nodes info:
        ///    0: neutral
        ///    1: ally contested
        ///    2: horde contested
        ///    3: ally occupied
        ///    4: horde occupied
        /// </summary>
        ABNodeStatus[] m_Nodes = new ABNodeStatus[ABBattlegroundNodes.DynamicNodesCount];
        ABNodeStatus[] m_prevNodes = new ABNodeStatus[ABBattlegroundNodes.DynamicNodesCount];
        BannerTimer[] m_BannerTimers = new BannerTimer[ABBattlegroundNodes.DynamicNodesCount];
        uint[] m_NodeTimers = new uint[ABBattlegroundNodes.DynamicNodesCount];
        uint[] m_lastTick = new uint[SharedConst.PvpTeamsCount];
        uint[] m_HonorScoreTics = new uint[SharedConst.PvpTeamsCount];
        uint[] m_ReputationScoreTics = new uint[SharedConst.PvpTeamsCount];
        bool m_IsInformedNearVictory;
        uint m_HonorTics;
        uint m_ReputationTics;

        //Const
        public const uint NotABBGWeekendHonorTicks = 260;
        public const uint ABBGWeekendHonorTicks = 160;
        public const uint NotABBGWeekendReputationTicks = 160;
        public const uint ABBGWeekendReputationTicks = 120;

        public const int EventStartBattle = 9158;// Achievement: Let's Get This Done

        public const int SoundClaimed = 8192;
        public const int SoundCapturedAlliance = 8173;
        public const int SoundCapturedHorde = 8213;
        public const uint SoundAssaultedAlliance = 8212;
        public const uint SoundAssaultedHorde = 8174;
        public const int SoundNearVictoryAlliance = 8456;
        public const int SoundNearVictoryHorde = 8457;

        public const int FlagCapturingTime = 60000;

        public const int WarningNearVictoryScore = 1400;
        public const int MaxTeamScore = 1500;

        public const uint ExploitTeleportLocationAlliance = 3705;
        public const uint ExploitTeleportLocationHorde = 3706;

        public static Position[] NodePositions =
        {
            new Position(1166.785f, 1200.132f, -56.70859f, 0.9075713f),         // stables
            new Position(977.0156f, 1046.616f, -44.80923f, -2.600541f),         // blacksmith
            new Position(806.1821f, 874.2723f, -55.99371f, -2.303835f),         // farm
            new Position(856.1419f, 1148.902f, 11.18469f, -2.303835f),          // lumber mill
            new Position(1146.923f, 848.1782f, -110.917f, -0.7330382f)          // gold mine
        };

        // x, y, z, o, rot0, rot1, rot2, rot3
        public static float[][] DoorPositions =
        {
            new float[] {1284.597f, 1281.167f, -15.97792f, 0.7068594f, 0.012957f, -0.060288f, 0.344959f, 0.93659f },
            new float[] {708.0903f, 708.4479f, -17.8342f, -2.391099f, 0.050291f, 0.015127f, 0.929217f, -0.365784f}
        };

        // Tick intervals and given points: case 0, 1, 2, 3, 4, 5 captured nodes
        public static uint[] TickIntervals = { 0, 12000, 9000, 6000, 3000, 1000 };
        public static uint[] TickPoints = { 0, 10, 10, 10, 10, 30 };

        // WorldSafeLocs ids for 5 nodes, and for ally, and horde starting location
        public static uint[] GraveyardIds = { 895, 894, 893, 897, 896, 898, 899 };

        // x, y, z, o
        public static float[][] BuffPositions =
        {
            new float[] {1185.566f, 1184.629f, -56.36329f, 2.303831f },         // stables
            new float[] {990.1131f, 1008.73f, -42.60328f, 0.8203033f },         // blacksmith
            new float[] {818.0089f, 842.3543f, -56.54062f, 3.176533f },         // farm
            new float[] {808.8463f, 1185.417f,  11.92161f, 5.619962f },         // lumber mill
            new float[] {1147.091f, 816.8362f, -98.39896f, 6.056293f }          // gold mine
        };

        public static Position[] SpiritGuidePos =
        {
            new Position(1200.03f, 1171.09f, -56.47f, 5.15f),                   // stables
            new Position(1017.43f, 960.61f, -42.95f, 4.88f),                    // blacksmith
            new Position(833.00f, 793.00f, -57.25f, 5.27f),                     // farm
            new Position(775.17f, 1206.40f, 15.79f, 1.90f),                     // lumber mill
            new Position(1207.48f, 787.00f, -83.36f, 5.51f),                    // gold mine
            new Position(1354.05f, 1275.48f, -11.30f, 4.77f),                   // alliance starting base
            new Position(714.61f, 646.15f, -10.87f, 4.34f)                      // horde starting base
        };

        public static int[] NodeStates = { 1767, 1782, 1772, 1792, 1787 };

        public static int[] NodeIcons = { 1842, 1846, 1845, 1844, 1843 };
    }

    struct BannerTimer
    {
        public uint timer;
        public byte type;
        public byte teamIndex;
    }

    #region Consts
    struct ABWorldStates
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

    // Note: code uses that these IDs follow each other
    struct NodeObjectId
    {
        public const uint Banner0 = 180087;       // Stables Banner
        public const uint Banner1 = 180088;       // Blacksmith Banner
        public const uint Banner2 = 180089;       // Farm Banner
        public const uint Banner3 = 180090;       // Lumber Mill Banner
        public const uint Banner4 = 180091;        // Gold Mine Banner
    }

    struct ABObjectTypes
    {
        // for all 5 node points 8*5=40 objects
        public const int BannerNeutral = 0;
        public const int BannerContA = 1;
        public const int BannerContH = 2;
        public const int BannerAlly = 3;
        public const int BannerHorde = 4;
        public const int AuraAlly = 5;
        public const int AuraHorde = 6;
        public const int AuraContested = 7;
        //Gates
        public const int GateA = 40;
        public const int GateH = 41;
        //Buffs
        public const int SpeedbuffStables = 42;
        public const int RegenbuffStables = 43;
        public const int BerserkbuffStables = 44;
        public const int SpeedbuffBlacksmith = 45;
        public const int RegenbuffBlacksmith = 46;
        public const int BerserkbuffBlacksmith = 47;
        public const int SpeedbuffFarm = 48;
        public const int RegenbuffFarm = 49;
        public const int BerserkbuffFarm = 50;
        public const int SpeedbuffLumberMill = 51;
        public const int RegenbuffLumberMill = 52;
        public const int BerserkbuffLumberMill = 53;
        public const int SpeedbuffGoldMine = 54;
        public const int RegenbuffGoldMine = 55;
        public const int BerserkbuffGoldMine = 56;
        public const int Max = 57;
    }

    // Object id templates from DB
    struct ABObjectIds
    {
        public const uint BannerA = 180058;
        public const uint BannerContA = 180059;
        public const uint BannerH = 180060;
        public const uint BannerContH = 180061;

        public const uint AuraA = 180100;
        public const uint AuraH = 180101;
        public const uint AuraC = 180102;

        public const uint GateA = 180255;
        public const uint GateH = 180256;
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

    struct ABBattlegroundBroadcastTexts
    {
        public const uint AllianceNearVictory = 10598;
        public const uint HordeNearVictory = 10599;

        public static ABNodeInfo[] ABNodes =
        {
            new ABNodeInfo(ABBattlegroundNodes.NodeStables,    10199, 10200, 10203, 10204, 10201, 10202, 10286, 10287),
            new ABNodeInfo(ABBattlegroundNodes.NodeBlacksmith, 10211, 10212, 10213, 10214, 10215, 10216, 10290, 10291),
            new ABNodeInfo(ABBattlegroundNodes.NodeFarm,       10217, 10218, 10219, 10220, 10221, 10222, 10288, 10289),
            new ABNodeInfo(ABBattlegroundNodes.NodeLumberMill, 10224, 10225, 10226, 10227, 10228, 10229, 10284, 10285),
            new ABNodeInfo(ABBattlegroundNodes.NodeGoldMine,   10230, 10231, 10232, 10233, 10234, 10235, 10282, 10283)
        };
    }

    struct ABNodeInfo
    {
        public ABNodeInfo(uint nodeId, uint textAllianceAssaulted, uint textHordeAssaulted, uint textAllianceTaken, uint textHordeTaken, uint textAllianceDefended, uint textHordeDefended, uint textAllianceClaims, uint textHordeClaims)
        {
            NodeId = nodeId;
            TextAllianceAssaulted = textAllianceAssaulted;
            TextHordeAssaulted = textHordeAssaulted;
            TextAllianceTaken = textAllianceTaken;
            TextHordeTaken = textHordeTaken;
            TextAllianceDefended = textAllianceDefended;
            TextHordeDefended = textHordeDefended;
            TextAllianceClaims = textAllianceClaims;
            TextHordeClaims = textHordeClaims;
        }

        public uint NodeId;
        public uint TextAllianceAssaulted;
        public uint TextHordeAssaulted;
        public uint TextAllianceTaken;
        public uint TextHordeTaken;
        public uint TextAllianceDefended;
        public uint TextHordeDefended;
        public uint TextAllianceClaims;
        public uint TextHordeClaims;
    }

    enum ABNodeStatus
    {
        Neutral = 0,
        Contested = 1,
        AllyContested = 1,
        HordeContested = 2,
        Occupied = 3,
        AllyOccupied = 3,
        HordeOccupied = 4
    }

    enum ABObjectives
    {
        AssaultBase = 122,
        DefendBase = 123
    }

    enum ArathiBasinPvpStats
    {
        BasesAssaulted = 926,
        BasesDefended = 927,
    }
    #endregion
}
