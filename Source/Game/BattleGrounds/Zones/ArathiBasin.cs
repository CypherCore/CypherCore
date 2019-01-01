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
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones
{
    class BgArathiBasin : Battleground
    {
        public BgArathiBasin()
        {
            m_IsInformedNearVictory = false;
            m_BuffChange = true;
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

            for (byte i = 0; i < SharedConst.BGTeamsCount; ++i)
            {
                m_lastTick[i] = 0;
                m_HonorScoreTics[i] = 0;
                m_ReputationScoreTics[i] = 0;
                m_TeamScores500Disadvantage[i] = false;
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
                            _NodeOccupied(node, (teamIndex == TeamId.Alliance) ? Team.Alliance : Team.Horde);
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

                    for (int team = 0; team < SharedConst.BGTeamsCount; ++team)
                        if (m_Nodes[node] == team + ABNodeStatus.Occupied)
                            ++team_points[team];
                }

                // Accumulate points
                for (int team = 0; team < SharedConst.BGTeamsCount; ++team)
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
                            if (team == TeamId.Alliance)
                                RewardReputationToTeam(509, 10, Team.Alliance);
                            else
                                RewardReputationToTeam(510, 10, Team.Horde);

                            m_ReputationScoreTics[team] -= m_ReputationTics;
                        }

                        if (m_HonorScoreTics[team] >= m_HonorTics)
                        {
                            RewardHonorToTeam(GetBonusHonorFromKill(1), (team == TeamId.Alliance) ? Team.Alliance : Team.Horde);
                            m_HonorScoreTics[team] -= m_HonorTics;
                        }

                        if (!m_IsInformedNearVictory && m_TeamScores[team] > WarningNearVictoryScore)
                        {
                            if (team == TeamId.Alliance)
                                SendBroadcastText(ABBattlegroundBroadcastTexts.AllianceNearVictory, ChatMsg.BgSystemNeutral);
                            else
                                SendBroadcastText(ABBattlegroundBroadcastTexts.HordeNearVictory, ChatMsg.BgSystemNeutral);
                            PlaySoundToAll(SoundNearVictory);
                            m_IsInformedNearVictory = true;
                        }

                        if (m_TeamScores[team] > MaxTeamScore)
                            m_TeamScores[team] = MaxTeamScore;

                        if (team == TeamId.Alliance)
                            UpdateWorldState(ABWorldStates.ResourcesAlly, m_TeamScores[team]);
                        else if (team == TeamId.Horde)
                            UpdateWorldState(ABWorldStates.ResourcesHorde, m_TeamScores[team]);
                        // update achievement flags
                        // we increased m_TeamScores[team] so we just need to check if it is 500 more than other teams resources
                        int otherTeam = (team + 1) % SharedConst.BGTeamsCount;
                        if (m_TeamScores[team] > m_TeamScores[otherTeam] + 500)
                            m_TeamScores500Disadvantage[otherTeam] = true;
                    }
                }

                // Test win condition
                if (m_TeamScores[TeamId.Alliance] >= MaxTeamScore)
                    EndBattleground(Team.Alliance);
                else if (m_TeamScores[TeamId.Horde] >= MaxTeamScore)
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
            StartCriteriaTimer(CriteriaTimedTypes.Event, EventStartBattle);
        }

        public override void AddPlayer(Player player)
        {
            base.AddPlayer(player);
            PlayerScores[player.GetGUID()] = new BattlegroundABScore(player.GetGUID(), player.GetBGTeam());
        }

        public override void RemovePlayer(Player Player, ObjectGuid guid, Team team)
        {
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            switch (trigger)
            {
                case 6635: // Horde Start
                case 6634: // Alliance Start
                    if (GetStatus() == BattlegroundStatus.WaitJoin && !entered)
                        TeleportPlayerToExploitLocation(player);
                    break;
                case 3948:                                          // Arathi Basin Alliance Exit.
                    if (player.GetTeam() != Team.Alliance)
                        player.GetSession().SendNotification("Only The Alliance can use that portal");
                    else
                        player.LeaveBattleground();
                    break;
                case 3949:                                          // Arathi Basin Horde Exit.
                    if (player.GetTeam() != Team.Horde)
                        player.GetSession().SendNotification("Only The Horde can use that portal");
                    else
                        player.LeaveBattleground();
                    break;
                case 3866:                                          // Stables
                case 3869:                                          // Gold Mine
                case 3867:                                          // Farm
                case 3868:                                          // Lumber Mill
                case 3870:                                          // Black Smith
                case 4020:                                          // Unk1
                case 4021:                                          // Unk2
                case 4674:                                          // Unk3
                                                                    //break;
                default:
                    base.HandleAreaTrigger(player, trigger, entered);
                    break;
            }
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

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            byte[] plusArray = { 0, 2, 3, 0, 1 };

            // Node icons
            for (byte node = 0; node < ABBattlegroundNodes.DynamicNodesCount; ++node)
                packet.AddState(NodeIcons[node], (m_Nodes[node] == 0));

            // Node occupied states
            for (byte node = 0; node < ABBattlegroundNodes.DynamicNodesCount; ++node)
                for (byte i = 1; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
                    packet.AddState(NodeStates[node] + plusArray[i], ((int)m_Nodes[node] == i));

            // How many bases each team owns
            byte ally = 0, horde = 0;
            for (byte node = 0; node < ABBattlegroundNodes.DynamicNodesCount; ++node)
                if (m_Nodes[node] == ABNodeStatus.AllyOccupied)
                    ++ally;
                else if (m_Nodes[node] == ABNodeStatus.HordeOccupied)
                    ++horde;

            packet.AddState(ABWorldStates.OccupiedBasesAlly, ally);
            packet.AddState(ABWorldStates.OccupiedBasesHorde, horde);

            // Team scores
            packet.AddState(ABWorldStates.ResourcesMax, MaxTeamScore);
            packet.AddState(ABWorldStates.ResourcesWarning, WarningNearVictoryScore);
            packet.AddState(ABWorldStates.ResourcesAlly, (int)m_TeamScores[TeamId.Alliance]);
            packet.AddState(ABWorldStates.ResourcesHorde, (int)m_TeamScores[TeamId.Horde]);

            // other unknown
            packet.AddState(0x745, 0x2);
        }

        void _SendNodeUpdate(byte node)
        {
            // Send node owner state update to refresh map icons on client
            byte[] plusArray = { 0, 2, 3, 0, 1 };

            if (m_prevNodes[node] != 0)
                UpdateWorldState(NodeStates[node] + plusArray[(int)m_prevNodes[node]], 0);
            else
                UpdateWorldState(NodeIcons[node], 0);

            UpdateWorldState(NodeStates[node] + plusArray[(byte)m_Nodes[node]], 1);

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

            Creature trigger = !BgCreatures[node + 7].IsEmpty() ? GetBGCreature(node + 7) : null; // 0-6 spirit guides
            if (!trigger)
                trigger = AddCreature(SharedConst.WorldTrigger, node + 7, NodePositions[node], GetTeamIndexByTeamId(team));

            //add bonus honor aura trigger creature when node is accupied
            //cast bonus aura (+50% honor in 25yards)
            //aura should only apply to players who have accupied the node, set correct faction for trigger
            if (trigger)
            {
                trigger.SetFaction(team == Team.Alliance ? 84u : 83u);
                trigger.CastSpell(trigger, BattlegroundConst.SpellHonorableDefender25y, false);
            }
        }

        void _NodeDeOccupied(byte node)
        {
            if (node >= ABBattlegroundNodes.DynamicNodesCount)
                return;

            //remove bonus honor aura trigger creature when node is lost
            if (node < ABBattlegroundNodes.DynamicNodesCount)//only dynamic nodes, no start points
                DelCreature(node + 7);//null checks are in DelCreature! 0-6 spirit guides

            RelocateDeadPlayers(BgCreatures[node]);

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
            while ((node < ABBattlegroundNodes.DynamicNodesCount) && ((!obj) || (!source.IsWithinDistInMap(obj, 10))))
            {
                ++node;
                obj = GetBgMap().GetGameObject(BgObjects[node * 8 + ABObjectTypes.AuraContested]);
            }

            if (node == ABBattlegroundNodes.DynamicNodesCount)
            {
                // this means our player isn't close to any of banners - maybe cheater ??
                return;
            }

            int teamIndex = GetTeamIndexByTeamId(source.GetTeam());

            // Check if player really could use this banner, not cheated
            if (!(m_Nodes[node] == 0 || teamIndex == (int)m_Nodes[node] % 2))
                return;

            source.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnterPvpCombat);
            uint sound = 0;
            // If node is neutral, change to contested
            if (m_Nodes[node] == ABNodeStatus.Neutral)
            {
                UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
                m_prevNodes[node] = m_Nodes[node];
                m_Nodes[node] = (ABNodeStatus)(teamIndex + 1);
                // burn current neutral banner
                _DelBanner(node, ABNodeStatus.Neutral, 0);
                // create new contested banner
                _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                _SendNodeUpdate(node);
                m_NodeTimers[node] = FlagCapturingTime;

                // FIXME: team and node names not localized
                if (teamIndex == TeamId.Alliance)
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
                    UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
                    m_prevNodes[node] = m_Nodes[node];
                    m_Nodes[node] = (ABNodeStatus.Contested + teamIndex);
                    // burn current contested banner
                    _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                    // create new contested banner
                    _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                    _SendNodeUpdate(node);
                    m_NodeTimers[node] = FlagCapturingTime;

                    if (teamIndex == TeamId.Alliance)
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceAssaulted, ChatMsg.BgSystemAlliance, source);
                    else
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeAssaulted, ChatMsg.BgSystemHorde, source);
                }
                // If contested, change back to occupied
                else
                {
                    UpdatePlayerScore(source, ScoreType.BasesDefended, 1);
                    m_prevNodes[node] = m_Nodes[node];
                    m_Nodes[node] = (ABNodeStatus.Occupied + teamIndex);
                    // burn current contested banner
                    _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                    // create new occupied banner
                    _CreateBanner(node, ABNodeStatus.Occupied, (byte)teamIndex, true);
                    _SendNodeUpdate(node);
                    m_NodeTimers[node] = 0;
                    _NodeOccupied(node, (teamIndex == TeamId.Alliance) ? Team.Alliance : Team.Horde);

                    if (teamIndex == TeamId.Alliance)
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceDefended, ChatMsg.BgSystemAlliance, source);
                    else
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeDefended, ChatMsg.BgSystemHorde, source);
                }
                sound = (teamIndex == TeamId.Alliance) ? SoundAssaultedAlliance : SoundAssaultedHorde;
            }
            // If node is occupied, change to enemy-contested
            else
            {
                UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
                m_prevNodes[node] = m_Nodes[node];
                m_Nodes[node] = (ABNodeStatus.Contested + teamIndex);
                // burn current occupied banner
                _DelBanner(node, ABNodeStatus.Occupied, (byte)teamIndex);
                // create new contested banner
                _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                _SendNodeUpdate(node);
                _NodeDeOccupied(node);
                m_NodeTimers[node] = FlagCapturingTime;

                if (teamIndex == TeamId.Alliance)
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceAssaulted, ChatMsg.BgSystemAlliance, source);
                else
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeAssaulted, ChatMsg.BgSystemHorde, source);

                sound = (teamIndex == TeamId.Alliance) ? SoundAssaultedAlliance : SoundAssaultedHorde;
            }

            // If node is occupied again, send "X has taken the Y" msg.
            if (m_Nodes[node] >= ABNodeStatus.Occupied)
            {
                // FIXME: team and node names not localized
                if (teamIndex == TeamId.Alliance)
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

            return true;
        }

        public override void Reset()
        {
            //call parent's class reset
            base.Reset();

            for (var i = 0; i < SharedConst.BGTeamsCount; ++i)
            {
                m_TeamScores[i] = 0;
                m_lastTick[i] = 0;
                m_HonorScoreTics[i] = 0;
                m_ReputationScoreTics[i] = 0;
                m_TeamScores500Disadvantage[i] = false;
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

        public override WorldSafeLocsRecord GetClosestGraveYard(Player player)
        {
            int teamIndex = GetTeamIndexByTeamId(player.GetTeam());

            // Is there any occupied node for this team?
            List<byte> nodes = new List<byte>();
            for (byte i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
                if (m_Nodes[i] == ABNodeStatus.Occupied + teamIndex)
                    nodes.Add(i);

            WorldSafeLocsRecord good_entry = null;
            // If so, select the closest node to place ghost on
            if (!nodes.Empty())
            {
                float plr_x = player.GetPositionX();
                float plr_y = player.GetPositionY();

                float mindist = 999999.0f;
                for (byte i = 0; i < nodes.Count; ++i)
                {
                    WorldSafeLocsRecord entry = CliDB.WorldSafeLocsStorage.LookupByKey(GraveyardIds[nodes[i]]);
                    if (entry == null)
                        continue;
                    float dist = (entry.Loc.X - plr_x) * (entry.Loc.X - plr_x) + (entry.Loc.Y - plr_y) * (entry.Loc.Y - plr_y);
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
                good_entry = CliDB.WorldSafeLocsStorage.LookupByKey(GraveyardIds[teamIndex + 5]);

            return good_entry;
        }

        public override WorldSafeLocsRecord GetExploitTeleportLocation(Team team)
        {
            return CliDB.WorldSafeLocsStorage.LookupByKey(team == Team.Alliance ? ExploitTeleportLocationAlliance : ExploitTeleportLocationHorde);
        }

        public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
        {
            if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
                return false;

            switch (type)
            {
                case ScoreType.BasesAssaulted:
                    player.UpdateCriteria(CriteriaTypes.BgObjectiveCapture, (uint)ABObjectives.AssaultBase);
                    break;
                case ScoreType.BasesDefended:
                    player.UpdateCriteria(CriteriaTypes.BgObjectiveCapture, (uint)ABObjectives.DefendBase);
                    break;
                default:
                    break;
            }
            return true;
        }

        public override bool IsAllNodesControlledByTeam(Team team)
        {
            uint count = 0;
            for (int i = 0; i < ABBattlegroundNodes.DynamicNodesCount; ++i)
                if ((team == Team.Alliance && m_Nodes[i] == ABNodeStatus.AllyOccupied) ||
                    (team == Team.Horde && m_Nodes[i] == ABNodeStatus.HordeOccupied))
                    ++count;

            return count == ABBattlegroundNodes.DynamicNodesCount;
        }

        public override bool CheckAchievementCriteriaMeet(uint criteriaId, Player player, Unit target, uint miscvalue)
        {
            switch ((BattlegroundCriteriaId)criteriaId)
            {
                case BattlegroundCriteriaId.ResilientVictory:
                    return m_TeamScores500Disadvantage[GetTeamIndexByTeamId(player.GetTeam())];
            }

            return base.CheckAchievementCriteriaMeet(criteriaId, player, target, miscvalue);
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
        uint[] m_lastTick = new uint[SharedConst.BGTeamsCount];
        uint[] m_HonorScoreTics = new uint[SharedConst.BGTeamsCount];
        uint[] m_ReputationScoreTics = new uint[SharedConst.BGTeamsCount];
        bool m_IsInformedNearVictory;
        uint m_HonorTics;
        uint m_ReputationTics;
        // need for achievements
        bool[] m_TeamScores500Disadvantage = new bool[SharedConst.BGTeamsCount];

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
        public const int SoundNearVictory = 8456;

        public const int FlagCapturingTime = 60000;

        public const int WarningNearVictoryScore = 1400;
        public const int MaxTeamScore = 1600;

        public const int ExploitTeleportLocationAlliance = 3705;
        public const int ExploitTeleportLocationHorde = 3706;

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
            new float[] {1185.71f, 1185.24f, -56.36f, 2.56f },                   // stables
            new float[] {990.75f, 1008.18f, -42.60f, 2.43f },                    // blacksmith
            new float[] {817.66f, 843.34f, -56.54f, 3.01f },                     // farm
            new float[] {807.46f, 1189.16f, 11.92f, 5.44f },                     // lumber mill
            new float[] {1146.62f, 816.94f, -98.49f, 6.14f }                     // gold mine
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

        public static uint[] NodeStates = { 1767, 1782, 1772, 1792, 1787 };

        public static uint[] NodeIcons = { 1842, 1846, 1845, 1844, 1843 };
    }

    class BattlegroundABScore : BattlegroundScore
    {
        public BattlegroundABScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team)
        {
            BasesAssaulted = 0;
            BasesDefended = 0;
        }

        public override void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.BasesAssaulted:
                    BasesAssaulted += value;
                    break;
                case ScoreType.BasesDefended:
                    BasesDefended += value;
                    break;
                default:
                    base.UpdateScore(type, value);
                    break;
            }
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPLogData.PlayerData playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            playerData.Stats.Add(BasesAssaulted);
            playerData.Stats.Add(BasesDefended);
        }

        public override uint GetAttr1() { return BasesAssaulted; }
        public override uint GetAttr2() { return BasesDefended; }

        uint BasesAssaulted;
        uint BasesDefended;
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
        public const uint OccupiedBasesHorde = 1778;
        public const uint OccupiedBasesAlly = 1779;
        public const uint ResourcesAlly = 1776;
        public const uint ResourcesHorde = 1777;
        public const uint ResourcesMax = 1780;
        public const uint ResourcesWarning = 1955;
        /*
            public const uint StableIcon                = 1842;             //Stable Map Icon (None)
            public const uint StableStateAlience       = 1767;             //Stable Map State (Alience)
            public const uint StableStateHorde         = 1768;             //Stable Map State (Horde)
            public const uint StableStateConAli       = 1769;             //Stable Map State (Con Alience)
            public const uint StableStateConHor       = 1770;             //Stable Map State (Con Horde)
            public const uint FarmIcon                  = 1845;             //Farm Map Icon (None)
            public const uint FarmStateAlience         = 1772;             //Farm State (Alience)
            public const uint FarmStateHorde           = 1773;             //Farm State (Horde)
            public const uint FarmStateConAli         = 1774;             //Farm State (Con Alience)
            public const uint FarmStateConHor         = 1775;             //Farm State (Con Horde)

            public const uint BlacksmithIcon            = 1846;             //Blacksmith Map Icon (None)
            public const uint BlacksmithStateAlience   = 1782;             //Blacksmith Map State (Alience)
            public const uint BlacksmithStateHorde     = 1783;             //Blacksmith Map State (Horde)
            public const uint BlacksmithStateConAli   = 1784;             //Blacksmith Map State (Con Alience)
            public const uint BlacksmithStateConHor   = 1785;             //Blacksmith Map State (Con Horde)
            public const uint LumbermillIcon            = 1844;             //Lumber Mill Map Icon (None)
            public const uint LumbermillStateAlience   = 1792;             //Lumber Mill Map State (Alience)
            public const uint LumbermillStateHorde     = 1793;             //Lumber Mill Map State (Horde)
            public const uint LumbermillStateConAli   = 1794;             //Lumber Mill Map State (Con Alience)
            public const uint LumbermillStateConHor   = 1795;             //Lumber Mill Map State (Con Horde)
            public const uint GoldmineIcon              = 1843;             //Gold Mine Map Icon (None)
            public const uint GoldmineStateAlience     = 1787;             //Gold Mine Map State (Alience)
            public const uint GoldmineStateHorde       = 1788;             //Gold Mine Map State (Horde)
            public const uint GoldmineStateConAli     = 1789;             //Gold Mine Map State (Con Alience
            public const uint GoldmineStateConHor     = 1790;             //Gold Mine Map State (Con Horde)
        */
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
    #endregion
}
