// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.BattleGrounds.Zones.ArathiBasin
{
    internal class BgArathiBasin : Battleground
    {
        //Const
        public const uint NOT_ABBG_WEEKEND_HONOR_TICKS = 260;
        public const uint ABBG_WEEKEND_HONOR_TICKS = 160;
        public const uint NOT_ABBG_WEEKEND_REPUTATION_TICKS = 160;
        public const uint ABBG_WEEKEND_REPUTATION_TICKS = 120;

        public const int EVENT_START_BATTLE = 9158; // Achievement: Let's Get This Done

        public const int SOUND_CLAIMED = 8192;
        public const int SOUND_CAPTURED_ALLIANCE = 8173;
        public const int SOUND_CAPTURED_HORDE = 8213;
        public const uint SOUND_ASSAULTED_ALLIANCE = 8212;
        public const uint SOUND_ASSAULTED_HORDE = 8174;
        public const int SOUND_NEAR_VICTORY_ALLIANCE = 8456;
        public const int SOUND_NEAR_VICTORY_HORDE = 8457;

        public const int FLAG_CAPTURING_TIME = 60000;

        public const int WARNING_NEAR_VICTORY_SCORE = 1400;
        public const int MAX_TEAM_SCORE = 1500;

        public const uint EXPLOIT_TELEPORT_LOCATION_ALLIANCE = 3705;
        public const uint EXPLOIT_TELEPORT_LOCATION_HORDE = 3706;

        public static Position[] NodePositions =
        {
            new(1166.785f, 1200.132f, -56.70859f, 0.9075713f), // stables
			new(977.0156f, 1046.616f, -44.80923f, -2.600541f), // blacksmith
			new(806.1821f, 874.2723f, -55.99371f, -2.303835f), // farm
			new(856.1419f, 1148.902f, 11.18469f, -2.303835f),  // lumber mill
			new(1146.923f, 848.1782f, -110.917f, -0.7330382f)  // gold mine
		};

        // x, y, z, o, rot0, rot1, rot2, rot3
        public static float[][] DoorPositions =
        {
            new float[]
            {
                1284.597f, 1281.167f, -15.97792f, 0.7068594f, 0.012957f, -0.060288f, 0.344959f, 0.93659f
            },
            new float[]
            {
                708.0903f, 708.4479f, -17.8342f, -2.391099f, 0.050291f, 0.015127f, 0.929217f, -0.365784f
            }
        };

        // Tick intervals and given points: case 0, 1, 2, 3, 4, 5 captured nodes
        public static uint[] TickIntervals =
        {
            0, 12000, 9000, 6000, 3000, 1000
        };

        public static uint[] TickPoints =
        {
            0, 10, 10, 10, 10, 30
        };

        // WorldSafeLocs ids for 5 nodes, and for ally, and horde starting location
        public static uint[] GraveyardIds =
        {
            895, 894, 893, 897, 896, 898, 899
        };

        // x, y, z, o
        public static float[][] BuffPositions =
        {
            new float[]
            {
                1185.566f, 1184.629f, -56.36329f, 2.303831f
            }, // stables
			new float[]
            {
                990.1131f, 1008.73f, -42.60328f, 0.8203033f
            }, // blacksmith
			new float[]
            {
                818.0089f, 842.3543f, -56.54062f, 3.176533f
            }, // farm
			new float[]
            {
                808.8463f, 1185.417f, 11.92161f, 5.619962f
            }, // lumber mill
			new float[]
            {
                1147.091f, 816.8362f, -98.39896f, 6.056293f
            } // gold mine
		};

        public static Position[] SpiritGuidePos =
        {
            new(1200.03f, 1171.09f, -56.47f, 5.15f), // stables
			new(1017.43f, 960.61f, -42.95f, 4.88f),  // blacksmith
			new(833.00f, 793.00f, -57.25f, 5.27f),   // farm
			new(775.17f, 1206.40f, 15.79f, 1.90f),   // lumber mill
			new(1207.48f, 787.00f, -83.36f, 5.51f),  // gold mine
			new(1354.05f, 1275.48f, -11.30f, 4.77f), // alliance starting base
			new(714.61f, 646.15f, -10.87f, 4.34f)    // horde starting base
		};

        public static int[] NodeStates =
        {
            1767, 1782, 1772, 1792, 1787
        };

        public static int[] NodeIcons =
        {
            1842, 1846, 1845, 1844, 1843
        };

        private readonly BannerTimer[] _bannerTimers = new BannerTimer[ABBattlegroundNodes.DYNAMIC_NODES_COUNT];
        private readonly uint[] _honorScoreTics = new uint[SharedConst.PvpTeamsCount];
        private uint _honorTics;
        private bool _isInformedNearVictory;
        private readonly uint[] _lastTick = new uint[SharedConst.PvpTeamsCount];

        /// <summary>
        ///  Nodes info:
        ///  0: neutral
        ///  1: ally contested
        ///  2: horde contested
        ///  3: ally occupied
        ///  4: horde occupied
        /// </summary>
        private ABNodeStatus[] _nodes = new ABNodeStatus[ABBattlegroundNodes.DYNAMIC_NODES_COUNT];

        private readonly uint[] _nodeTimers = new uint[ABBattlegroundNodes.DYNAMIC_NODES_COUNT];
        private readonly ABNodeStatus[] _prevNodes = new ABNodeStatus[ABBattlegroundNodes.DYNAMIC_NODES_COUNT];
        private readonly uint[] _reputationScoreTics = new uint[SharedConst.PvpTeamsCount];
        private uint _reputationTics;

        public BgArathiBasin(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            _isInformedNearVictory = false;
            _BuffChange = true;
            BgObjects = new ObjectGuid[ABObjectTypes.MAX];
            BgCreatures = new ObjectGuid[ABBattlegroundNodes.ALL_COUNT + 5]; //+5 for aura triggers

            for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            {
                _nodes[i] = 0;
                _prevNodes[i] = 0;
                _nodeTimers[i] = 0;
                _bannerTimers[i].Timer = 0;
                _bannerTimers[i].Type = 0;
                _bannerTimers[i].TeamIndex = 0;
            }

            for (byte i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                _lastTick[i] = 0;
                _honorScoreTics[i] = 0;
                _reputationScoreTics[i] = 0;
            }

            _honorTics = 0;
            _reputationTics = 0;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() == BattlegroundStatus.InProgress)
            {
                int[] team_points =
                {
                    0, 0
                };

                for (byte node = 0; node < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++node)
                {
                    // 3 sec delay to spawn new banner instead previous despawned one
                    if (_bannerTimers[node].Timer != 0)
                    {
                        if (_bannerTimers[node].Timer > diff)
                        {
                            _bannerTimers[node].Timer -= diff;
                        }
                        else
                        {
                            _bannerTimers[node].Timer = 0;
                            _CreateBanner(node, (ABNodeStatus)_bannerTimers[node].Type, _bannerTimers[node].TeamIndex, false);
                        }
                    }

                    // 1-minute to occupy a node from contested State
                    if (_nodeTimers[node] != 0)
                    {
                        if (_nodeTimers[node] > diff)
                        {
                            _nodeTimers[node] -= diff;
                        }
                        else
                        {
                            _nodeTimers[node] = 0;
                            // Change from contested to occupied !
                            int teamIndex = (int)_nodes[node] - 1;
                            _prevNodes[node] = _nodes[node];
                            _nodes[node] += 2;
                            // burn current contested banner
                            _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                            // create new occupied banner
                            _CreateBanner(node, ABNodeStatus.Occupied, teamIndex, true);
                            _SendNodeUpdate(node);
                            _NodeOccupied(node, teamIndex == TeamId.Alliance ? Team.Alliance : Team.Horde);
                            // Message to chatlog

                            if (teamIndex == 0)
                            {
                                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceTaken, ChatMsg.BgSystemAlliance);
                                PlaySoundToAll(SOUND_CAPTURED_ALLIANCE);
                            }
                            else
                            {
                                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeTaken, ChatMsg.BgSystemHorde);
                                PlaySoundToAll(SOUND_CAPTURED_HORDE);
                            }
                        }
                    }

                    for (int team = 0; team < SharedConst.PvpTeamsCount; ++team)
                        if (_nodes[node] == team + ABNodeStatus.Occupied)
                            ++team_points[team];
                }

                // Accumulate points
                for (int team = 0; team < SharedConst.PvpTeamsCount; ++team)
                {
                    int points = team_points[team];

                    if (points == 0)
                        continue;

                    _lastTick[team] += diff;

                    if (_lastTick[team] > TickIntervals[points])
                    {
                        _lastTick[team] -= TickIntervals[points];
                        _TeamScores[team] += TickPoints[points];
                        _honorScoreTics[team] += TickPoints[points];
                        _reputationScoreTics[team] += TickPoints[points];

                        if (_reputationScoreTics[team] >= _reputationTics)
                        {
                            if (team == TeamId.Alliance)
                                RewardReputationToTeam(509, 10, Team.Alliance);
                            else
                                RewardReputationToTeam(510, 10, Team.Horde);

                            _reputationScoreTics[team] -= _reputationTics;
                        }

                        if (_honorScoreTics[team] >= _honorTics)
                        {
                            RewardHonorToTeam(GetBonusHonorFromKill(1), team == TeamId.Alliance ? Team.Alliance : Team.Horde);
                            _honorScoreTics[team] -= _honorTics;
                        }

                        if (!_isInformedNearVictory &&
                            _TeamScores[team] > WARNING_NEAR_VICTORY_SCORE)
                        {
                            if (team == TeamId.Alliance)
                            {
                                SendBroadcastText(ABBattlegroundBroadcastTexts.ALLIANCE_NEAR_VICTORY, ChatMsg.BgSystemNeutral);
                                PlaySoundToAll(SOUND_NEAR_VICTORY_ALLIANCE);
                            }
                            else
                            {
                                SendBroadcastText(ABBattlegroundBroadcastTexts.HORDE_NEAR_VICTORY, ChatMsg.BgSystemNeutral);
                                PlaySoundToAll(SOUND_NEAR_VICTORY_HORDE);
                            }

                            _isInformedNearVictory = true;
                        }

                        if (_TeamScores[team] > MAX_TEAM_SCORE)
                            _TeamScores[team] = MAX_TEAM_SCORE;

                        if (team == TeamId.Alliance)
                            UpdateWorldState(ABWorldStates.RESOURCES_ALLY, (int)_TeamScores[team]);
                        else
                            UpdateWorldState(ABWorldStates.RESOURCES_HORDE, (int)_TeamScores[team]);

                        // update Achievement Flags
                        // we increased _TeamScores[team] so we just need to check if it is 500 more than other teams resources
                        int otherTeam = (team + 1) % SharedConst.PvpTeamsCount;

                        if (_TeamScores[team] > _TeamScores[otherTeam] + 500)
                        {
                            if (team == TeamId.Alliance)
                                UpdateWorldState(ABWorldStates.HAD_500_DISADVANTAGE_HORDE, 1);
                            else
                                UpdateWorldState(ABWorldStates.HAD_500_DISADVANTAGE_ALLIANCE, 1);
                        }
                    }
                }

                // Test win condition
                if (_TeamScores[TeamId.Alliance] >= MAX_TEAM_SCORE)
                    EndBattleground(Team.Alliance);
                else if (_TeamScores[TeamId.Horde] >= MAX_TEAM_SCORE)
                    EndBattleground(Team.Horde);
            }
        }

        public override void StartingEventCloseDoors()
        {
            // despawn banners, Auras and buffs
            for (int obj = ABObjectTypes.BANNER_NEUTRAL; obj < ABBattlegroundNodes.DYNAMIC_NODES_COUNT * 8; ++obj)
                SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);

            for (int i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT * 3; ++i)
                SpawnBGObject(ABObjectTypes.SPEEDBUFF_STABLES + i, BattlegroundConst.RespawnOneDay);

            // Starting doors
            DoorClose(ABObjectTypes.GATE_A);
            DoorClose(ABObjectTypes.GATE_H);
            SpawnBGObject(ABObjectTypes.GATE_A, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(ABObjectTypes.GATE_H, BattlegroundConst.RespawnImmediately);

            // Starting base spirit guides
            _NodeOccupied(ABBattlegroundNodes.SPIRIT_ALIANCE, Team.Alliance);
            _NodeOccupied(ABBattlegroundNodes.SPIRIT_HORDE, Team.Horde);
        }

        public override void StartingEventOpenDoors()
        {
            // spawn neutral banners
            for (int banner = ABObjectTypes.BANNER_NEUTRAL, i = 0; i < 5; banner += 8, ++i)
                SpawnBGObject(banner, BattlegroundConst.RespawnImmediately);

            for (int i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            {
                //randomly select buff to spawn
                int buff = RandomHelper.IRand(0, 2);
                SpawnBGObject(ABObjectTypes.SPEEDBUFF_STABLES + buff + i * 3, BattlegroundConst.RespawnImmediately);
            }

            DoorOpen(ABObjectTypes.GATE_A);
            DoorOpen(ABObjectTypes.GATE_H);

            // Achievement: Let's Get This Done
            TriggerGameEvent(EVENT_START_BATTLE);
        }

        public override void AddPlayer(Player player)
        {
            bool isInBattleground = IsPlayerInBattleground(player.GetGUID());
            base.AddPlayer(player);

            if (!isInBattleground)
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
                    if (GetStatus() == BattlegroundStatus.WaitJoin &&
                        !entered)
                        TeleportPlayerToExploitLocation(player);

                    break;
                case 3948: // Arathi Basin Alliance Exit.
                case 3949: // Arathi Basin Horde Exit.
                case 3866: // Stables
                case 3869: // Gold Mine
                case 3867: // Farm
                case 3868: // Lumber Mill
                case 3870: // Black Smith
                case 4020: // Unk1
                case 4021: // Unk2
                case 4674: // Unk3
                default:
                    base.HandleAreaTrigger(player, trigger, entered);

                    break;
            }
        }

        private void _CreateBanner(byte node, ABNodeStatus type, int teamIndex, bool delay)
        {
            // Just put it into the queue
            if (delay)
            {
                _bannerTimers[node].Timer = 2000;
                _bannerTimers[node].Type = (byte)type;
                _bannerTimers[node].TeamIndex = (byte)teamIndex;

                return;
            }

            int obj = node * 8 + (byte)type + teamIndex;

            SpawnBGObject(obj, BattlegroundConst.RespawnImmediately);

            // handle aura with banner
            if (type == 0)
                return;

            obj = node * 8 + (type == ABNodeStatus.Occupied ? 5 + teamIndex : 7);
            SpawnBGObject(obj, BattlegroundConst.RespawnImmediately);
        }

        private void _DelBanner(byte node, ABNodeStatus type, byte teamIndex)
        {
            int obj = node * 8 + (byte)type + teamIndex;
            SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);

            // handle aura with banner
            if (type == 0)
                return;

            obj = node * 8 + (type == ABNodeStatus.Occupied ? 5 + teamIndex : 7);
            SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);
        }

        private void _SendNodeUpdate(byte node)
        {
            // Send node owner State update to refresh map icons on client
            int[] idPlusArray =
            {
                0, 2, 3, 0, 1
            };

            int[] statePlusArray =
            {
                0, 2, 0, 2, 0
            };

            if (_prevNodes[node] != 0)
                UpdateWorldState(NodeStates[node] + idPlusArray[(int)_prevNodes[node]], 0);
            else
                UpdateWorldState(NodeIcons[node], 0);

            UpdateWorldState(NodeStates[node] + idPlusArray[(byte)_nodes[node]], 1);

            switch (node)
            {
                case ABBattlegroundNodes.NODE_STABLES:
                    UpdateWorldState(ABWorldStates.STABLES_ICON_NEW, (int)_nodes[node] + statePlusArray[(int)_nodes[node]]);
                    UpdateWorldState(ABWorldStates.STABLES_HORDE_CONTROL_STATE, _nodes[node] == ABNodeStatus.HordeOccupied ? 2 : _nodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                    UpdateWorldState(ABWorldStates.STABLES_ALLIANCE_CONTROL_STATE, _nodes[node] == ABNodeStatus.AllyOccupied ? 2 : _nodes[node] == ABNodeStatus.AllyContested ? 1 : 0);

                    break;
                case ABBattlegroundNodes.NODE_BLACKSMITH:
                    UpdateWorldState(ABWorldStates.BLACKSMITH_ICON_NEW, (int)_nodes[node] + statePlusArray[(int)_nodes[node]]);
                    UpdateWorldState(ABWorldStates.BLACKSMITH_HORDE_CONTROL_STATE, _nodes[node] == ABNodeStatus.HordeOccupied ? 2 : _nodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                    UpdateWorldState(ABWorldStates.BLACKSMITH_ALLIANCE_CONTROL_STATE, _nodes[node] == ABNodeStatus.AllyOccupied ? 2 : _nodes[node] == ABNodeStatus.AllyContested ? 1 : 0);

                    break;
                case ABBattlegroundNodes.NODE_FARM:
                    UpdateWorldState(ABWorldStates.FARM_ICON_NEW, (int)_nodes[node] + statePlusArray[(int)_nodes[node]]);
                    UpdateWorldState(ABWorldStates.FARM_HORDE_CONTROL_STATE, _nodes[node] == ABNodeStatus.HordeOccupied ? 2 : _nodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                    UpdateWorldState(ABWorldStates.FARM_ALLIANCE_CONTROL_STATE, _nodes[node] == ABNodeStatus.AllyOccupied ? 2 : _nodes[node] == ABNodeStatus.AllyContested ? 1 : 0);

                    break;
                case ABBattlegroundNodes.NODE_LUMBER_MILL:
                    UpdateWorldState(ABWorldStates.LUMBER_MILL_ICON_NEW, (int)_nodes[node] + statePlusArray[(int)_nodes[node]]);
                    UpdateWorldState(ABWorldStates.LUMBER_MILL_HORDE_CONTROL_STATE, _nodes[node] == ABNodeStatus.HordeOccupied ? 2 : _nodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                    UpdateWorldState(ABWorldStates.LUMBER_MILL_ALLIANCE_CONTROL_STATE, _nodes[node] == ABNodeStatus.AllyOccupied ? 2 : _nodes[node] == ABNodeStatus.AllyContested ? 1 : 0);

                    break;
                case ABBattlegroundNodes.NODE_GOLD_MINE:
                    UpdateWorldState(ABWorldStates.GOLD_MINE_ICON_NEW, (int)_nodes[node] + statePlusArray[(int)_nodes[node]]);
                    UpdateWorldState(ABWorldStates.GOLD_MINE_HORDE_CONTROL_STATE, _nodes[node] == ABNodeStatus.HordeOccupied ? 2 : _nodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                    UpdateWorldState(ABWorldStates.GOLD_MINE_ALLIANCE_CONTROL_STATE, _nodes[node] == ABNodeStatus.AllyOccupied ? 2 : _nodes[node] == ABNodeStatus.AllyContested ? 1 : 0);

                    break;
                default:
                    break;
            }

            // How many bases each team owns
            byte ally = 0, horde = 0;

            for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
                if (_nodes[i] == ABNodeStatus.AllyOccupied)
                    ++ally;
                else if (_nodes[i] == ABNodeStatus.HordeOccupied)
                    ++horde;

            UpdateWorldState(ABWorldStates.OCCUPIED_BASES_ALLY, ally);
            UpdateWorldState(ABWorldStates.OCCUPIED_BASES_HORDE, horde);
        }

        private void _NodeOccupied(byte node, Team team)
        {
            if (!AddSpiritGuide(node, SpiritGuidePos[node], GetTeamIndexByTeamId(team)))
                Log.outError(LogFilter.Battleground, "Failed to spawn spirit guide! point: {0}, team: {1}, ", node, team);

            if (node >= ABBattlegroundNodes.DYNAMIC_NODES_COUNT) //only dynamic nodes, no start points
                return;

            byte capturedNodes = 0;

            for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
                if (_nodes[i] == ABNodeStatus.Occupied + GetTeamIndexByTeamId(team) &&
                    _nodeTimers[i] == 0)
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

        private void _NodeDeOccupied(byte node)
        {
            //only dynamic nodes, no start points
            if (node >= ABBattlegroundNodes.DYNAMIC_NODES_COUNT)
                return;

            //remove bonus honor aura trigger creature when node is lost
            DelCreature(node + 7); //null checks are in DelCreature! 0-6 spirit guides

            RelocateDeadPlayers(BgCreatures[node]);

            DelCreature(node);

            // buff object isn't despawned
        }

        //Invoked if a player used a banner as a gameobject
        public override void EventPlayerClickedOnFlag(Player source, GameObject target_obj)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            byte node = ABBattlegroundNodes.NODE_STABLES;
            GameObject obj = GetBgMap().GetGameObject(BgObjects[node * 8 + 7]);

            while (node < ABBattlegroundNodes.DYNAMIC_NODES_COUNT && (!obj || !source.IsWithinDistInMap(obj, 10)))
            {
                ++node;
                obj = GetBgMap().GetGameObject(BgObjects[node * 8 + ABObjectTypes.AURA_CONTESTED]);
            }

            if (node == ABBattlegroundNodes.DYNAMIC_NODES_COUNT)
                // this means our player isn't close to any of banners - maybe cheater ??
                return;

            int teamIndex = GetTeamIndexByTeamId(GetPlayerTeam(source.GetGUID()));

            // Check if player really could use this banner, not cheated
            if (!(_nodes[node] == 0 || teamIndex == (int)_nodes[node] % 2))
                return;

            source.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
            uint sound;

            // If node is neutral, change to contested
            if (_nodes[node] == ABNodeStatus.Neutral)
            {
                UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
                _prevNodes[node] = _nodes[node];
                _nodes[node] = (ABNodeStatus)(teamIndex + 1);
                // burn current neutral banner
                _DelBanner(node, ABNodeStatus.Neutral, 0);
                // create new contested banner
                _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                _SendNodeUpdate(node);
                _nodeTimers[node] = FLAG_CAPTURING_TIME;

                // FIXME: team and node names not localized
                if (teamIndex == TeamId.Alliance)
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceClaims, ChatMsg.BgSystemAlliance, source);
                else
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeClaims, ChatMsg.BgSystemHorde, source);

                sound = SOUND_CLAIMED;
            }
            // If node is contested
            else if (_nodes[node] == ABNodeStatus.AllyContested ||
                     _nodes[node] == ABNodeStatus.HordeContested)
            {
                // If last State is NOT occupied, change node to enemy-contested
                if (_prevNodes[node] < ABNodeStatus.Occupied)
                {
                    UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
                    _prevNodes[node] = _nodes[node];
                    _nodes[node] = ABNodeStatus.Contested + teamIndex;
                    // burn current contested banner
                    _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                    // create new contested banner
                    _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                    _SendNodeUpdate(node);
                    _nodeTimers[node] = FLAG_CAPTURING_TIME;

                    if (teamIndex == TeamId.Alliance)
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceAssaulted, ChatMsg.BgSystemAlliance, source);
                    else
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeAssaulted, ChatMsg.BgSystemHorde, source);
                }
                // If contested, change back to occupied
                else
                {
                    UpdatePlayerScore(source, ScoreType.BasesDefended, 1);
                    _prevNodes[node] = _nodes[node];
                    _nodes[node] = ABNodeStatus.Occupied + teamIndex;
                    // burn current contested banner
                    _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                    // create new occupied banner
                    _CreateBanner(node, ABNodeStatus.Occupied, (byte)teamIndex, true);
                    _SendNodeUpdate(node);
                    _nodeTimers[node] = 0;
                    _NodeOccupied(node, teamIndex == TeamId.Alliance ? Team.Alliance : Team.Horde);

                    if (teamIndex == TeamId.Alliance)
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceDefended, ChatMsg.BgSystemAlliance, source);
                    else
                        SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeDefended, ChatMsg.BgSystemHorde, source);
                }

                sound = teamIndex == TeamId.Alliance ? SOUND_ASSAULTED_ALLIANCE : SOUND_ASSAULTED_HORDE;
            }
            // If node is occupied, change to enemy-contested
            else
            {
                UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
                _prevNodes[node] = _nodes[node];
                _nodes[node] = ABNodeStatus.Contested + teamIndex;
                // burn current occupied banner
                _DelBanner(node, ABNodeStatus.Occupied, (byte)teamIndex);
                // create new contested banner
                _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                _SendNodeUpdate(node);
                _NodeDeOccupied(node);
                _nodeTimers[node] = FLAG_CAPTURING_TIME;

                if (teamIndex == TeamId.Alliance)
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceAssaulted, ChatMsg.BgSystemAlliance, source);
                else
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeAssaulted, ChatMsg.BgSystemHorde, source);

                sound = teamIndex == TeamId.Alliance ? SOUND_ASSAULTED_ALLIANCE : SOUND_ASSAULTED_HORDE;
            }

            // If node is occupied again, send "X has taken the Y" msg.
            if (_nodes[node] >= ABNodeStatus.Occupied)
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

            for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
                if (_nodes[i] == ABNodeStatus.AllyOccupied)
                    ++ally;
                else if (_nodes[i] == ABNodeStatus.HordeOccupied)
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

            for (int i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            {
                result &= AddObject(ABObjectTypes.BANNER_NEUTRAL + 8 * i, (uint)(NodeObjectId.BANNER_0 + i), NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.BANNER_CONT_A + 8 * i, ABObjectIds.BANNER_CONT_A, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.BANNER_CONT_H + 8 * i, ABObjectIds.BANNER_CONT_H, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.BANNER_ALLY + 8 * i, ABObjectIds.BANNER_A, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.BANNER_HORDE + 8 * i, ABObjectIds.BANNER_H, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.AURA_ALLY + 8 * i, ABObjectIds.AURA_A, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.AURA_HORDE + 8 * i, ABObjectIds.AURA_H, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.AURA_CONTESTED + 8 * i, ABObjectIds.AURA_C, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].GetOrientation() / 2), (float)Math.Cos(NodePositions[i].GetOrientation() / 2), BattlegroundConst.RespawnOneDay);

                if (!result)
                {
                    Log.outError(LogFilter.Sql, "BatteGroundAB: Failed to spawn some object Battleground not created!");

                    return false;
                }
            }

            result &= AddObject(ABObjectTypes.GATE_A, ABObjectIds.GATE_A, DoorPositions[0][0], DoorPositions[0][1], DoorPositions[0][2], DoorPositions[0][3], DoorPositions[0][4], DoorPositions[0][5], DoorPositions[0][6], DoorPositions[0][7], BattlegroundConst.RespawnImmediately);
            result &= AddObject(ABObjectTypes.GATE_H, ABObjectIds.GATE_H, DoorPositions[1][0], DoorPositions[1][1], DoorPositions[1][2], DoorPositions[1][3], DoorPositions[1][4], DoorPositions[1][5], DoorPositions[1][6], DoorPositions[1][7], BattlegroundConst.RespawnImmediately);

            if (!result)
            {
                Log.outError(LogFilter.Sql, "BatteGroundAB: Failed to spawn door object Battleground not created!");

                return false;
            }

            //buffs
            for (int i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            {
                result &= AddObject(ABObjectTypes.SPEEDBUFF_STABLES + 3 * i, Buff_Entries[0], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.SPEEDBUFF_STABLES + 3 * i + 1, Buff_Entries[1], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);
                result &= AddObject(ABObjectTypes.SPEEDBUFF_STABLES + 3 * i + 2, Buff_Entries[2], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);

                if (!result)
                {
                    Log.outError(LogFilter.Sql, "BatteGroundAB: Failed to spawn buff object!");

                    return false;
                }
            }

            UpdateWorldState(ABWorldStates.RESOURCES_MAX, MAX_TEAM_SCORE);
            UpdateWorldState(ABWorldStates.RESOURCES_WARNING, WARNING_NEAR_VICTORY_SCORE);

            return true;
        }

        public override void Reset()
        {
            //call parent's class reset
            base.Reset();

            for (var i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                _TeamScores[i] = 0;
                _lastTick[i] = 0;
                _honorScoreTics[i] = 0;
                _reputationScoreTics[i] = 0;
            }

            _isInformedNearVictory = false;
            bool isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
            _honorTics = isBGWeekend ? ABBG_WEEKEND_HONOR_TICKS : NOT_ABBG_WEEKEND_HONOR_TICKS;
            _reputationTics = isBGWeekend ? ABBG_WEEKEND_REPUTATION_TICKS : NOT_ABBG_WEEKEND_REPUTATION_TICKS;

            for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            {
                _nodes[i] = 0;
                _prevNodes[i] = 0;
                _nodeTimers[i] = 0;
                _bannerTimers[i].Timer = 0;
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

        public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
        {
            int teamIndex = GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID()));

            // Is there any occupied node for this team?
            List<byte> nodes = new();

            for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
                if (_nodes[i] == ABNodeStatus.Occupied + teamIndex)
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
            return Global.ObjectMgr.GetWorldSafeLoc(team == Team.Alliance ? EXPLOIT_TELEPORT_LOCATION_ALLIANCE : EXPLOIT_TELEPORT_LOCATION_HORDE);
        }

        public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
        {
            if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
                return false;

            switch (type)
            {
                case ScoreType.BasesAssaulted:
                    player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)ABObjectives.AssaultBase);

                    break;
                case ScoreType.BasesDefended:
                    player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)ABObjectives.DefendBase);

                    break;
                default:
                    break;
            }

            return true;
        }
    }

}