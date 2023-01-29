// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattleGrounds.Zones.StrandofAncients
{
    public class BgStrandOfAncients : Battleground
    {
        /// Id of Attacker team
        private int _attackers;

        private readonly Dictionary<uint /*Id*/, uint /*timer*/> _demoliserRespawnList = new();

        // Max Time of round
        private uint _endRoundTimer;

        // Status of each gate (Destroy/Damage/Intact)
        private readonly SAGateState[] _gateStatus = new SAGateState[SAMiscConst.Gates.Length];

        // Team witch conntrol each graveyard
        private readonly int[] _graveyardStatus = new int[SAGraveyards.MAX];

        // for know if second round has been init
        private bool _initSecondRound;

        // Score of each round
        private readonly SARoundScore[] _roundScores = new SARoundScore[2];

        // For know if boats has start moving or not yet
        private bool _shipsStarted;

        // for know if warning about second round start has been sent
        private bool _signaledRoundTwo;

        // for know if warning about second round start has been sent
        private bool _signaledRoundTwoHalfMin;

        // Statu of battle (Start or not, and what round)
        private SAStatus _status;

        // used for know we are in timer phase or not (used for worldstate update)
        private bool _timerEnabled;

        // Totale elapsed Time of current round
        private uint _totalTime;

        // 5secs before starting the 1min countdown for second round
        private uint _updateWaitTimer;

        public BgStrandOfAncients(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            StartMessageIds[BattlegroundConst.EventIdFourth] = 0;

            BgObjects = new ObjectGuid[SAObjectTypes.MAX_OBJ];
            BgCreatures = new ObjectGuid[SACreatureTypes.MAX + SAGraveyards.MAX];
            _timerEnabled = false;
            _updateWaitTimer = 0;
            _signaledRoundTwo = false;
            _signaledRoundTwoHalfMin = false;
            _initSecondRound = false;
            _attackers = TeamId.Alliance;
            _totalTime = 0;
            _endRoundTimer = 0;
            _shipsStarted = false;
            _status = SAStatus.NotStarted;

            for (byte i = 0; i < _gateStatus.Length; ++i)
                _gateStatus[i] = SAGateState.HordeGateOk;

            for (byte i = 0; i < 2; i++)
            {
                _roundScores[i].Winner = TeamId.Alliance;
                _roundScores[i].Time = 0;
            }
        }

        public override void Reset()
        {
            _totalTime = 0;
            _attackers = RandomHelper.URand(0, 1) != 0 ? TeamId.Alliance : TeamId.Horde;

            for (byte i = 0; i <= 5; i++)
                _gateStatus[i] = SAGateState.HordeGateOk;

            _shipsStarted = false;
            _status = SAStatus.Warmup;
        }

        public override bool SetupBattleground()
        {
            return ResetObjs();
        }

        private bool ResetObjs()
        {
            foreach (var pair in GetPlayers())
            {
                Player player = Global.ObjAccessor.FindPlayer(pair.Key);

                if (player)
                    SendTransportsRemove(player);
            }

            uint atF = SAMiscConst.Factions[_attackers];
            uint defF = SAMiscConst.Factions[_attackers != 0 ? TeamId.Alliance : TeamId.Horde];

            for (byte i = 0; i < SAObjectTypes.MAX_OBJ; i++)
                DelObject(i);

            for (byte i = 0; i < SACreatureTypes.MAX; i++)
                DelCreature(i);

            for (byte i = SACreatureTypes.MAX; i < SACreatureTypes.MAX + SAGraveyards.MAX; i++)
                DelCreature(i);

            for (byte i = 0; i < _gateStatus.Length; ++i)
                _gateStatus[i] = _attackers == TeamId.Horde ? SAGateState.AllianceGateOk : SAGateState.HordeGateOk;

            if (!AddCreature(SAMiscConst.NpcEntries[SACreatureTypes.KANRETHAD], SACreatureTypes.KANRETHAD, SAMiscConst.NpcSpawnlocs[SACreatureTypes.KANRETHAD]))
            {
                Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn Kanrethad, aborted. Entry: {SAMiscConst.NpcEntries[SACreatureTypes.KANRETHAD]}");

                return false;
            }

            for (byte i = 0; i <= SAObjectTypes.PORTAL_DEFFENDER_RED; i++)
                if (!AddObject(i, SAMiscConst.ObjEntries[i], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
                {
                    Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn BG_SA_PORTAL_DEFFENDER_RED, Entry: {SAMiscConst.ObjEntries[i]}");

                    continue;
                }

            for (int i = SAObjectTypes.BOAT_ONE; i <= SAObjectTypes.BOAT_TWO; i++)
            {
                uint boatid = 0;

                switch (i)
                {
                    case SAObjectTypes.BOAT_ONE:
                        boatid = _attackers != 0 ? SAGameObjectIds.BOAT_ONE_H : SAGameObjectIds.BOAT_ONE_A;

                        break;
                    case SAObjectTypes.BOAT_TWO:
                        boatid = _attackers != 0 ? SAGameObjectIds.BOAT_TWO_H : SAGameObjectIds.BOAT_TWO_A;

                        break;
                    default:
                        break;
                }

                if (!AddObject(i,
                               boatid,
                               SAMiscConst.ObjSpawnlocs[i].GetPositionX(),
                               SAMiscConst.ObjSpawnlocs[i].GetPositionY(),
                               SAMiscConst.ObjSpawnlocs[i].GetPositionZ() + (_attackers != 0 ? -3.750f : 0),
                               SAMiscConst.ObjSpawnlocs[i].GetOrientation(),
                               0,
                               0,
                               0,
                               0,
                               BattlegroundConst.RespawnOneDay))
                {
                    Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn one of the BG_SA_BOAT, Entry: {boatid}");

                    continue;
                }
            }

            for (byte i = SAObjectTypes.SIGIL_1; i <= SAObjectTypes.LEFT_FLAGPOLE; i++)
                if (!AddObject(i, SAMiscConst.ObjEntries[i], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
                {
                    Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn Sigil, Entry: {SAMiscConst.ObjEntries[i]}");

                    continue;
                }

            // MAD props for Kiper for discovering those values - 4 hours of his work.
            GetBGObject(SAObjectTypes.BOAT_ONE).SetParentRotation(new Quaternion(0.0f, 0.0f, 1.0f, 0.0002f));
            GetBGObject(SAObjectTypes.BOAT_TWO).SetParentRotation(new Quaternion(0.0f, 0.0f, 1.0f, 0.00001f));
            SpawnBGObject(SAObjectTypes.BOAT_ONE, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(SAObjectTypes.BOAT_TWO, BattlegroundConst.RespawnImmediately);

            //Cannons and demolishers - NPCs are spawned
            //By capturing GYs.
            for (byte i = 0; i < SACreatureTypes.DEMOLISHER_5; i++)
                if (!AddCreature(SAMiscConst.NpcEntries[i], i, SAMiscConst.NpcSpawnlocs[i], _attackers == TeamId.Alliance ? TeamId.Horde : TeamId.Alliance, 600))
                {
                    Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn Cannon or demolisher, Entry: {SAMiscConst.NpcEntries[i]}, Attackers: {(_attackers == TeamId.Alliance ? "Horde(1)" : "Alliance(0)")}");

                    continue;
                }

            OverrideGunFaction();
            DemolisherStartState(true);

            for (byte i = 0; i <= SAObjectTypes.PORTAL_DEFFENDER_RED; i++)
            {
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
                GetBGObject(i).SetFaction(defF);
            }

            GetBGObject(SAObjectTypes.TITAN_RELIC).SetFaction(atF);
            GetBGObject(SAObjectTypes.TITAN_RELIC).Refresh();

            _totalTime = 0;
            _shipsStarted = false;

            //Graveyards
            for (byte i = 0; i < SAGraveyards.MAX; i++)
            {
                WorldSafeLocsEntry sg = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);

                if (sg == null)
                {
                    Log.outError(LogFilter.Battleground, $"SOTA: Can't find GY entry {SAMiscConst.GYEntries[i]}");

                    return false;
                }

                if (i == SAGraveyards.BEACH_GY)
                {
                    _graveyardStatus[i] = _attackers;
                    AddSpiritGuide(i + SACreatureTypes.MAX, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), SAMiscConst.GYOrientation[i], _attackers);
                }
                else
                {
                    _graveyardStatus[i] = _attackers == TeamId.Horde ? TeamId.Alliance : TeamId.Horde;

                    if (!AddSpiritGuide(i + SACreatureTypes.MAX, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), SAMiscConst.GYOrientation[i], _attackers == TeamId.Horde ? TeamId.Alliance : TeamId.Horde))
                        Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn GY: {i}");
                }
            }

            //GY capture points
            for (byte i = SAObjectTypes.CENTRAL_FLAG; i <= SAObjectTypes.LEFT_FLAG; i++)
            {
                if (!AddObject(i, SAMiscConst.ObjEntries[i] - (_attackers == TeamId.Alliance ? 1u : 0), SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
                {
                    Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn Central Flag Entry: {SAMiscConst.ObjEntries[i] - (_attackers == TeamId.Alliance ? 1 : 0)}");

                    continue;
                }

                GetBGObject(i).SetFaction(atF);
            }

            UpdateObjectInteractionFlags();

            for (byte i = SAObjectTypes.BOMB; i < SAObjectTypes.MAX_OBJ; i++)
            {
                if (!AddObject(i, SAMiscConst.ObjEntries[SAObjectTypes.BOMB], SAMiscConst.ObjSpawnlocs[i], 0, 0, 0, 0, BattlegroundConst.RespawnOneDay))
                {
                    Log.outError(LogFilter.Battleground, $"SOTA: couldn't spawn SA Bomb Entry: {SAMiscConst.ObjEntries[SAObjectTypes.BOMB] + i}");

                    continue;
                }

                GetBGObject(i).SetFaction(atF);
            }

            //Player may enter BEFORE we set up BG - lets update his worldstates anyway...
            UpdateWorldState(SAWorldStateIds.RIGHT_GY_HORDE, _graveyardStatus[SAGraveyards.RIGHT_CAPTURABLE_GY] == TeamId.Horde ? 1 : 0);
            UpdateWorldState(SAWorldStateIds.LEFT_GY_HORDE, _graveyardStatus[SAGraveyards.LEFT_CAPTURABLE_GY] == TeamId.Horde ? 1 : 0);
            UpdateWorldState(SAWorldStateIds.CENTER_GY_HORDE, _graveyardStatus[SAGraveyards.CENTRAL_CAPTURABLE_GY] == TeamId.Horde ? 1 : 0);

            UpdateWorldState(SAWorldStateIds.RIGHT_GY_ALLIANCE, _graveyardStatus[SAGraveyards.RIGHT_CAPTURABLE_GY] == TeamId.Alliance ? 1 : 0);
            UpdateWorldState(SAWorldStateIds.LEFT_GY_ALLIANCE, _graveyardStatus[SAGraveyards.LEFT_CAPTURABLE_GY] == TeamId.Alliance ? 1 : 0);
            UpdateWorldState(SAWorldStateIds.CENTER_GY_ALLIANCE, _graveyardStatus[SAGraveyards.CENTRAL_CAPTURABLE_GY] == TeamId.Alliance ? 1 : 0);

            if (_attackers == TeamId.Alliance)
            {
                UpdateWorldState(SAWorldStateIds.ALLY_ATTACKS, 1);
                UpdateWorldState(SAWorldStateIds.HORDE_ATTACKS, 0);

                UpdateWorldState(SAWorldStateIds.RIGHT_ATT_TOKEN_ALL, 1);
                UpdateWorldState(SAWorldStateIds.LEFT_ATT_TOKEN_ALL, 1);
                UpdateWorldState(SAWorldStateIds.RIGHT_ATT_TOKEN_HRD, 0);
                UpdateWorldState(SAWorldStateIds.LEFT_ATT_TOKEN_HRD, 0);

                UpdateWorldState(SAWorldStateIds.HORDE_DEFENCE_TOKEN, 1);
                UpdateWorldState(SAWorldStateIds.ALLIANCE_DEFENCE_TOKEN, 0);
            }
            else
            {
                UpdateWorldState(SAWorldStateIds.HORDE_ATTACKS, 1);
                UpdateWorldState(SAWorldStateIds.ALLY_ATTACKS, 0);

                UpdateWorldState(SAWorldStateIds.RIGHT_ATT_TOKEN_ALL, 0);
                UpdateWorldState(SAWorldStateIds.LEFT_ATT_TOKEN_ALL, 0);
                UpdateWorldState(SAWorldStateIds.RIGHT_ATT_TOKEN_HRD, 1);
                UpdateWorldState(SAWorldStateIds.LEFT_ATT_TOKEN_HRD, 1);

                UpdateWorldState(SAWorldStateIds.HORDE_DEFENCE_TOKEN, 0);
                UpdateWorldState(SAWorldStateIds.ALLIANCE_DEFENCE_TOKEN, 1);
            }

            UpdateWorldState(SAWorldStateIds.ATTACKER_TEAM, _attackers);
            UpdateWorldState(SAWorldStateIds.PURPLE_GATE, 1);
            UpdateWorldState(SAWorldStateIds.RED_GATE, 1);
            UpdateWorldState(SAWorldStateIds.BLUE_GATE, 1);
            UpdateWorldState(SAWorldStateIds.GREEN_GATE, 1);
            UpdateWorldState(SAWorldStateIds.YELLOW_GATE, 1);
            UpdateWorldState(SAWorldStateIds.ANCIENT_GATE, 1);

            for (int i = SAObjectTypes.BOAT_ONE; i <= SAObjectTypes.BOAT_TWO; i++)
                foreach (var pair in GetPlayers())
                {
                    Player player = Global.ObjAccessor.FindPlayer(pair.Key);

                    if (player)
                        SendTransportInit(player);
                }

            // set status manually so preparation is cast correctly in 2nd round too
            SetStatus(BattlegroundStatus.WaitJoin);

            TeleportPlayers();

            return true;
        }

        private void StartShips()
        {
            if (_shipsStarted)
                return;

            GetBGObject(SAObjectTypes.BOAT_ONE).SetGoState(GameObjectState.TransportStopped);
            GetBGObject(SAObjectTypes.BOAT_TWO).SetGoState(GameObjectState.TransportStopped);

            for (int i = SAObjectTypes.BOAT_ONE; i <= SAObjectTypes.BOAT_TWO; i++)
                foreach (var pair in GetPlayers())
                {
                    Player p = Global.ObjAccessor.FindPlayer(pair.Key);

                    if (p)
                    {
                        UpdateData data = new(p.GetMapId());
                        GetBGObject(i).BuildValuesUpdateBlockForPlayer(data, p);

                        UpdateObject pkt;
                        data.BuildPacket(out pkt);
                        p.SendPacket(pkt);
                    }
                }

            _shipsStarted = true;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (_initSecondRound)
            {
                if (_updateWaitTimer < diff)
                {
                    if (!_signaledRoundTwo)
                    {
                        _signaledRoundTwo = true;
                        _initSecondRound = false;
                        SendBroadcastText(SABroadcastTexts.ROUND_TWO_START_ONE_MINUTE, ChatMsg.BgSystemNeutral);
                    }
                }
                else
                {
                    _updateWaitTimer -= diff;

                    return;
                }
            }

            _totalTime += diff;

            if (_status == SAStatus.Warmup)
            {
                _endRoundTimer = SATimers.ROUND_LENGTH;
                UpdateWorldState(SAWorldStateIds.TIMER, (int)(GameTime.GetGameTime() + _endRoundTimer));

                if (_totalTime >= SATimers.WARMUP_LENGTH)
                {
                    Creature c = GetBGCreature(SACreatureTypes.KANRETHAD);

                    if (c)
                        SendChatMessage(c, SATextIds.ROUND_STARTED);

                    _totalTime = 0;
                    ToggleTimer();
                    DemolisherStartState(false);
                    _status = SAStatus.RoundOne;
                    TriggerGameEvent(_attackers == TeamId.Alliance ? 23748 : 21702u);
                }

                if (_totalTime >= SATimers.BOAT_START)
                    StartShips();

                return;
            }
            else if (_status == SAStatus.SecondWarmup)
            {
                if (_roundScores[0].Time < SATimers.ROUND_LENGTH)
                    _endRoundTimer = _roundScores[0].Time;
                else
                    _endRoundTimer = SATimers.ROUND_LENGTH;

                UpdateWorldState(SAWorldStateIds.TIMER, (int)(GameTime.GetGameTime() + _endRoundTimer));

                if (_totalTime >= 60000)
                {
                    Creature c = GetBGCreature(SACreatureTypes.KANRETHAD);

                    if (c)
                        SendChatMessage(c, SATextIds.ROUND_STARTED);

                    _totalTime = 0;
                    ToggleTimer();
                    DemolisherStartState(false);
                    _status = SAStatus.RoundTwo;
                    TriggerGameEvent(_attackers == TeamId.Alliance ? 23748 : 21702u);
                    // status was set to STATUS_WAIT_JOIN manually for Preparation, set it back now
                    SetStatus(BattlegroundStatus.InProgress);

                    foreach (var pair in GetPlayers())
                    {
                        Player p = Global.ObjAccessor.FindPlayer(pair.Key);

                        if (p)
                            p.RemoveAurasDueToSpell(BattlegroundConst.SpellPreparation);
                    }
                }

                if (_totalTime >= 30000)
                    if (!_signaledRoundTwoHalfMin)
                    {
                        _signaledRoundTwoHalfMin = true;
                        SendBroadcastText(SABroadcastTexts.ROUND_TWO_START_HALF_MINUTE, ChatMsg.BgSystemNeutral);
                    }

                StartShips();

                return;
            }
            else if (GetStatus() == BattlegroundStatus.InProgress)
            {
                if (_status == SAStatus.RoundOne)
                {
                    if (_totalTime >= SATimers.ROUND_LENGTH)
                    {
                        CastSpellOnTeam(SASpellIds.END_OF_ROUND, Team.Alliance);
                        CastSpellOnTeam(SASpellIds.END_OF_ROUND, Team.Horde);
                        _roundScores[0].Winner = (uint)_attackers;
                        _roundScores[0].Time = SATimers.ROUND_LENGTH;
                        _totalTime = 0;
                        _status = SAStatus.SecondWarmup;
                        _attackers = _attackers == TeamId.Alliance ? TeamId.Horde : TeamId.Alliance;
                        _updateWaitTimer = 5000;
                        _signaledRoundTwo = false;
                        _signaledRoundTwoHalfMin = false;
                        _initSecondRound = true;
                        ToggleTimer();
                        ResetObjs();
                        GetBgMap().UpdateAreaDependentAuras();

                        return;
                    }
                }
                else if (_status == SAStatus.RoundTwo)
                {
                    if (_totalTime >= _endRoundTimer)
                    {
                        CastSpellOnTeam(SASpellIds.END_OF_ROUND, Team.Alliance);
                        CastSpellOnTeam(SASpellIds.END_OF_ROUND, Team.Horde);
                        _roundScores[1].Time = SATimers.ROUND_LENGTH;
                        _roundScores[1].Winner = (uint)(_attackers == TeamId.Alliance ? TeamId.Horde : TeamId.Alliance);

                        if (_roundScores[0].Time == _roundScores[1].Time)
                            EndBattleground(0);
                        else if (_roundScores[0].Time < _roundScores[1].Time)
                            EndBattleground(_roundScores[0].Winner == TeamId.Alliance ? Team.Alliance : Team.Horde);
                        else
                            EndBattleground(_roundScores[1].Winner == TeamId.Alliance ? Team.Alliance : Team.Horde);

                        return;
                    }
                }

                if (_status == SAStatus.RoundOne ||
                    _status == SAStatus.RoundTwo)
                    UpdateDemolisherSpawns();
            }
        }

        public override void AddPlayer(Player player)
        {
            bool isInBattleground = IsPlayerInBattleground(player.GetGUID());
            base.AddPlayer(player);

            if (!isInBattleground)
                PlayerScores[player.GetGUID()] = new BattlegroundSAScore(player.GetGUID(), player.GetBGTeam());

            SendTransportInit(player);

            if (!isInBattleground)
                TeleportToEntrancePosition(player);
        }

        public override void RemovePlayer(Player player, ObjectGuid guid, Team team)
        {
        }

        public override void HandleAreaTrigger(Player source, uint trigger, bool entered)
        {
            // this is wrong way to implement these things. On official it done by gameobject spell cast.
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;
        }

        private void TeleportPlayers()
        {
            foreach (var pair in GetPlayers())
            {
                Player player = Global.ObjAccessor.FindPlayer(pair.Key);

                if (player)
                {
                    // should remove spirit of redemption
                    if (player.HasAuraType(AuraType.SpiritOfRedemption))
                        player.RemoveAurasByType(AuraType.ModShapeshift);

                    if (!player.IsAlive())
                    {
                        player.ResurrectPlayer(1.0f);
                        player.SpawnCorpseBones();
                    }

                    player.ResetAllPowers();
                    player.CombatStopWithPets(true);

                    player.CastSpell(player, BattlegroundConst.SpellPreparation, true);

                    TeleportToEntrancePosition(player);
                }
            }
        }

        private void TeleportToEntrancePosition(Player player)
        {
            if (GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID())) == _attackers)
            {
                if (!_shipsStarted)
                {
                    // player.AddUnitMovementFlag(MOVEMENTFLAG_ONTRANSPORT);

                    if (RandomHelper.URand(0, 1) != 0)
                        player.TeleportTo(607, 2682.936f, -830.368f, 15.0f, 2.895f, 0);
                    else
                        player.TeleportTo(607, 2577.003f, 980.261f, 15.0f, 0.807f, 0);
                }
                else
                {
                    player.TeleportTo(607, 1600.381f, -106.263f, 8.8745f, 3.78f, 0);
                }
            }
            else
            {
                player.TeleportTo(607, 1209.7f, -65.16f, 70.1f, 0.0f, 0);
            }
        }

        public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker = null)
        {
            GameObject go = obj.ToGameObject();

            if (go)
                switch (go.GetGoType())
                {
                    case GameObjectTypes.Goober:
                        if (invoker)
                            if (eventId == (uint)SAEventIds.BG_SA_EVENT_TITAN_RELIC_ACTIVATED)
                                TitanRelicActivated(invoker.ToPlayer());

                        break;
                    case GameObjectTypes.DestructibleBuilding:
                        {
                            SAGateInfo gate = GetGate(obj.GetEntry());

                            if (gate != null)
                            {
                                uint gateId = gate.GateId;

                                // damaged
                                if (eventId == go.GetGoInfo().DestructibleBuilding.DamagedEvent)
                                {
                                    _gateStatus[gateId] = _attackers == TeamId.Horde ? SAGateState.AllianceGateDamaged : SAGateState.HordeGateDamaged;

                                    Creature c = obj.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                                    if (c)
                                        SendChatMessage(c, (byte)gate.DamagedText, invoker);

                                    PlaySoundToAll(_attackers == TeamId.Alliance ? SASoundIds.WALL_ATTACKED_ALLIANCE : SASoundIds.WALL_ATTACKED_HORDE);
                                }
                                // destroyed
                                else if (eventId == go.GetGoInfo().DestructibleBuilding.DestroyedEvent)
                                {
                                    _gateStatus[gate.GateId] = _attackers == TeamId.Horde ? SAGateState.AllianceGateDestroyed : SAGateState.HordeGateDestroyed;

                                    if (gateId < 5)
                                        DelObject((int)gateId + 14);

                                    Creature c = obj.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                                    if (c)
                                        SendChatMessage(c, (byte)gate.DestroyedText, invoker);

                                    PlaySoundToAll(_attackers == TeamId.Alliance ? SASoundIds.WALL_DESTROYED_ALLIANCE : SASoundIds.WALL_DESTROYED_HORDE);

                                    bool rewardHonor = true;

                                    switch (gateId)
                                    {
                                        case SAObjectTypes.GREEN_GATE:
                                            if (IsGateDestroyed(SAObjectTypes.BLUE_GATE))
                                                rewardHonor = false;

                                            break;
                                        case SAObjectTypes.BLUE_GATE:
                                            if (IsGateDestroyed(SAObjectTypes.GREEN_GATE))
                                                rewardHonor = false;

                                            break;
                                        case SAObjectTypes.RED_GATE:
                                            if (IsGateDestroyed(SAObjectTypes.PURPLE_GATE))
                                                rewardHonor = false;

                                            break;
                                        case SAObjectTypes.PURPLE_GATE:
                                            if (IsGateDestroyed(SAObjectTypes.RED_GATE))
                                                rewardHonor = false;

                                            break;
                                        default:
                                            break;
                                    }

                                    if (invoker)
                                    {
                                        Unit unit = invoker.ToUnit();

                                        if (unit)
                                        {
                                            Player player = unit.GetCharmerOrOwnerPlayerOrPlayerItself();

                                            if (player)
                                            {
                                                UpdatePlayerScore(player, ScoreType.DestroyedWall, 1);

                                                if (rewardHonor)
                                                    UpdatePlayerScore(player, ScoreType.BonusHonor, GetBonusHonorFromKill(1));
                                            }
                                        }
                                    }

                                    UpdateObjectInteractionFlags();
                                }
                                else
                                {
                                    break;
                                }

                                UpdateWorldState(gate.WorldState, (int)_gateStatus[gateId]);
                            }

                            break;
                        }
                    default:
                        break;
                }
        }

        public override void HandleKillUnit(Creature creature, Player killer)
        {
            if (creature.GetEntry() == SACreatureIds.DEMOLISHER)
            {
                UpdatePlayerScore(killer, ScoreType.DestroyedDemolisher, 1);
                uint worldStateId = _attackers == TeamId.Horde ? SAWorldStateIds.DESTROYED_HORDE_VEHICLES : SAWorldStateIds.DESTROYED_ALLIANCE_VEHICLES;
                int currentDestroyedVehicles = Global.WorldStateMgr.GetValue((int)worldStateId, GetBgMap());
                UpdateWorldState(worldStateId, currentDestroyedVehicles + 1);
            }
        }

        /*
		  You may ask what the fuck does it do?
		  Prevents owner overwriting guns faction with own.
		 */
        private void OverrideGunFaction()
        {
            if (BgCreatures[0].IsEmpty())
                return;

            for (byte i = SACreatureTypes.GUN_1; i <= SACreatureTypes.GUN_10; i++)
            {
                Creature gun = GetBGCreature(i);

                if (gun)
                    gun.SetFaction(SAMiscConst.Factions[_attackers != 0 ? TeamId.Alliance : TeamId.Horde]);
            }

            for (byte i = SACreatureTypes.DEMOLISHER_1; i <= SACreatureTypes.DEMOLISHER_4; i++)
            {
                Creature dem = GetBGCreature(i);

                if (dem)
                    dem.SetFaction(SAMiscConst.Factions[_attackers]);
            }
        }

        private void DemolisherStartState(bool start)
        {
            if (BgCreatures[0].IsEmpty())
                return;

            // set Flags only for the demolishers on the beach, factory ones dont need it
            for (byte i = SACreatureTypes.DEMOLISHER_1; i <= SACreatureTypes.DEMOLISHER_4; i++)
            {
                Creature dem = GetBGCreature(i);

                if (dem)
                {
                    if (start)
                        dem.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
                    else
                        dem.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
                }
            }
        }

        public override void DestroyGate(Player player, GameObject go)
        {
        }

        public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
        {
            uint safeloc;

            int teamId = GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID()));

            if (teamId == _attackers)
                safeloc = SAMiscConst.GYEntries[SAGraveyards.BEACH_GY];
            else
                safeloc = SAMiscConst.GYEntries[SAGraveyards.DEFENDER_LAST_GY];

            WorldSafeLocsEntry closest = Global.ObjectMgr.GetWorldSafeLoc(safeloc);
            float nearest = player.GetExactDistSq(closest.Loc);

            for (byte i = SAGraveyards.RIGHT_CAPTURABLE_GY; i < SAGraveyards.MAX; i++)
            {
                if (_graveyardStatus[i] != teamId)
                    continue;

                WorldSafeLocsEntry ret = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);
                float dist = player.GetExactDistSq(ret.Loc);

                if (dist < nearest)
                {
                    closest = ret;
                    nearest = dist;
                }
            }

            return closest;
        }

        private bool CanInteractWithObject(uint objectId)
        {
            switch (objectId)
            {
                case SAObjectTypes.TITAN_RELIC:
                    if (!IsGateDestroyed(SAObjectTypes.ANCIENT_GATE) ||
                        !IsGateDestroyed(SAObjectTypes.YELLOW_GATE))
                        return false;

                    goto case SAObjectTypes.CENTRAL_FLAG;
                case SAObjectTypes.CENTRAL_FLAG:
                    if (!IsGateDestroyed(SAObjectTypes.RED_GATE) &&
                        !IsGateDestroyed(SAObjectTypes.PURPLE_GATE))
                        return false;

                    goto case SAObjectTypes.LEFT_FLAG;
                case SAObjectTypes.LEFT_FLAG:
                case SAObjectTypes.RIGHT_FLAG:
                    if (!IsGateDestroyed(SAObjectTypes.GREEN_GATE) &&
                        !IsGateDestroyed(SAObjectTypes.BLUE_GATE))
                        return false;

                    break;
                default:
                    //ABORT();
                    break;
            }

            return true;
        }

        private void UpdateObjectInteractionFlags(uint objectId)
        {
            GameObject go = GetBGObject((int)objectId);

            if (go)
            {
                if (CanInteractWithObject(objectId))
                    go.RemoveFlag(GameObjectFlags.NotSelectable);
                else
                    go.SetFlag(GameObjectFlags.NotSelectable);
            }
        }

        private void UpdateObjectInteractionFlags()
        {
            for (byte i = SAObjectTypes.CENTRAL_FLAG; i <= SAObjectTypes.LEFT_FLAG; ++i)
                UpdateObjectInteractionFlags(i);

            UpdateObjectInteractionFlags(SAObjectTypes.TITAN_RELIC);
        }

        public override void EventPlayerClickedOnFlag(Player source, GameObject go)
        {
            switch (go.GetEntry())
            {
                case 191307:
                case 191308:
                    if (CanInteractWithObject(SAObjectTypes.LEFT_FLAG))
                        CaptureGraveyard(SAGraveyards.LEFT_CAPTURABLE_GY, source);

                    break;
                case 191305:
                case 191306:
                    if (CanInteractWithObject(SAObjectTypes.RIGHT_FLAG))
                        CaptureGraveyard(SAGraveyards.RIGHT_CAPTURABLE_GY, source);

                    break;
                case 191310:
                case 191309:
                    if (CanInteractWithObject(SAObjectTypes.CENTRAL_FLAG))
                        CaptureGraveyard(SAGraveyards.CENTRAL_CAPTURABLE_GY, source);

                    break;
                default:
                    return;
            }
        }

        private void CaptureGraveyard(int i, Player source)
        {
            if (_graveyardStatus[i] == _attackers)
                return;

            DelCreature(SACreatureTypes.MAX + i);
            int teamId = GetTeamIndexByTeamId(GetPlayerTeam(source.GetGUID()));
            _graveyardStatus[i] = teamId;
            WorldSafeLocsEntry sg = Global.ObjectMgr.GetWorldSafeLoc(SAMiscConst.GYEntries[i]);

            if (sg == null)
            {
                Log.outError(LogFilter.Battleground, $"CaptureGraveyard: non-existant GY entry: {SAMiscConst.GYEntries[i]}");

                return;
            }

            AddSpiritGuide(i + SACreatureTypes.MAX, sg.Loc.GetPositionX(), sg.Loc.GetPositionY(), sg.Loc.GetPositionZ(), SAMiscConst.GYOrientation[i], _graveyardStatus[i]);

            uint npc;
            int flag;

            switch (i)
            {
                case SAGraveyards.LEFT_CAPTURABLE_GY:
                    {
                        flag = SAObjectTypes.LEFT_FLAG;
                        DelObject(flag);

                        AddObject(flag,
                                  SAMiscConst.ObjEntries[flag] - (teamId == TeamId.Alliance ? 0 : 1u),
                                  SAMiscConst.ObjSpawnlocs[flag],
                                  0,
                                  0,
                                  0,
                                  0,
                                  BattlegroundConst.RespawnOneDay);

                        npc = SACreatureTypes.RIGSPARK;
                        Creature rigspark = AddCreature(SAMiscConst.NpcEntries[npc], (int)npc, SAMiscConst.NpcSpawnlocs[npc], _attackers);

                        if (rigspark)
                            rigspark.GetAI().Talk(SATextIds.SPARKLIGHT_RIGSPARK_SPAWN);

                        for (byte j = SACreatureTypes.DEMOLISHER_7; j <= SACreatureTypes.DEMOLISHER_8; j++)
                        {
                            AddCreature(SAMiscConst.NpcEntries[j], j, SAMiscConst.NpcSpawnlocs[j], _attackers == TeamId.Alliance ? TeamId.Horde : TeamId.Alliance, 600);
                            Creature dem = GetBGCreature(j);

                            if (dem)
                                dem.SetFaction(SAMiscConst.Factions[_attackers]);
                        }

                        UpdateWorldState(SAWorldStateIds.LEFT_GY_ALLIANCE, _graveyardStatus[i] == TeamId.Alliance ? 1 : 0);
                        UpdateWorldState(SAWorldStateIds.LEFT_GY_HORDE, _graveyardStatus[i] == TeamId.Horde ? 1 : 0);

                        Creature c = source.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                        if (c)
                            SendChatMessage(c, teamId == TeamId.Alliance ? SATextIds.WEST_GRAVEYARD_CAPTURED_A : SATextIds.WEST_GRAVEYARD_CAPTURED_H, source);
                    }

                    break;
                case SAGraveyards.RIGHT_CAPTURABLE_GY:
                    {
                        flag = SAObjectTypes.RIGHT_FLAG;
                        DelObject(flag);

                        AddObject(flag,
                                  SAMiscConst.ObjEntries[flag] - (teamId == TeamId.Alliance ? 0 : 1u),
                                  SAMiscConst.ObjSpawnlocs[flag],
                                  0,
                                  0,
                                  0,
                                  0,
                                  BattlegroundConst.RespawnOneDay);

                        npc = SACreatureTypes.SPARKLIGHT;
                        Creature sparklight = AddCreature(SAMiscConst.NpcEntries[npc], (int)npc, SAMiscConst.NpcSpawnlocs[npc], _attackers);

                        if (sparklight)
                            sparklight.GetAI().Talk(SATextIds.SPARKLIGHT_RIGSPARK_SPAWN);

                        for (byte j = SACreatureTypes.DEMOLISHER_5; j <= SACreatureTypes.DEMOLISHER_6; j++)
                        {
                            AddCreature(SAMiscConst.NpcEntries[j], j, SAMiscConst.NpcSpawnlocs[j], _attackers == TeamId.Alliance ? TeamId.Horde : TeamId.Alliance, 600);

                            Creature dem = GetBGCreature(j);

                            if (dem)
                                dem.SetFaction(SAMiscConst.Factions[_attackers]);
                        }

                        UpdateWorldState(SAWorldStateIds.RIGHT_GY_ALLIANCE, _graveyardStatus[i] == TeamId.Alliance ? 1 : 0);
                        UpdateWorldState(SAWorldStateIds.RIGHT_GY_HORDE, _graveyardStatus[i] == TeamId.Horde ? 1 : 0);

                        Creature c = source.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                        if (c)
                            SendChatMessage(c, teamId == TeamId.Alliance ? SATextIds.EAST_GRAVEYARD_CAPTURED_A : SATextIds.EAST_GRAVEYARD_CAPTURED_H, source);
                    }

                    break;
                case SAGraveyards.CENTRAL_CAPTURABLE_GY:
                    {
                        flag = SAObjectTypes.CENTRAL_FLAG;
                        DelObject(flag);

                        AddObject(flag,
                                  SAMiscConst.ObjEntries[flag] - (teamId == TeamId.Alliance ? 0 : 1u),
                                  SAMiscConst.ObjSpawnlocs[flag],
                                  0,
                                  0,
                                  0,
                                  0,
                                  BattlegroundConst.RespawnOneDay);

                        UpdateWorldState(SAWorldStateIds.CENTER_GY_ALLIANCE, _graveyardStatus[i] == TeamId.Alliance ? 1 : 0);
                        UpdateWorldState(SAWorldStateIds.CENTER_GY_HORDE, _graveyardStatus[i] == TeamId.Horde ? 1 : 0);

                        Creature c = source.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);

                        if (c)
                            SendChatMessage(c, teamId == TeamId.Alliance ? SATextIds.SOUTH_GRAVEYARD_CAPTURED_A : SATextIds.SOUTH_GRAVEYARD_CAPTURED_H, source);
                    }

                    break;
                default:
                    //ABORT();
                    break;
            }
        }

        private void TitanRelicActivated(Player clicker)
        {
            if (!clicker)
                return;

            if (CanInteractWithObject(SAObjectTypes.TITAN_RELIC))
            {
                int clickerTeamId = GetTeamIndexByTeamId(GetPlayerTeam(clicker.GetGUID()));

                if (clickerTeamId == _attackers)
                {
                    if (clickerTeamId == TeamId.Alliance)
                        SendBroadcastText(SABroadcastTexts.ALLIANCE_CAPTURED_TITAN_PORTAL, ChatMsg.BgSystemNeutral);
                    else
                        SendBroadcastText(SABroadcastTexts.HORDE_CAPTURED_TITAN_PORTAL, ChatMsg.BgSystemNeutral);

                    if (_status == SAStatus.RoundOne)
                    {
                        _roundScores[0].Winner = (uint)_attackers;
                        _roundScores[0].Time = _totalTime;

                        // Achievement Storm the Beach (1310)
                        foreach (var pair in GetPlayers())
                        {
                            Player player = Global.ObjAccessor.FindPlayer(pair.Key);

                            if (player)
                                if (GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID())) == _attackers)
                                    player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                        }

                        _attackers = _attackers == TeamId.Alliance ? TeamId.Horde : TeamId.Alliance;
                        _status = SAStatus.SecondWarmup;
                        _totalTime = 0;
                        ToggleTimer();

                        Creature c = GetBGCreature(SACreatureTypes.KANRETHAD);

                        if (c)
                            SendChatMessage(c, SATextIds.ROUND_1_FINISHED);

                        _updateWaitTimer = 5000;
                        _signaledRoundTwo = false;
                        _signaledRoundTwoHalfMin = false;
                        _initSecondRound = true;
                        ResetObjs();
                        GetBgMap().UpdateAreaDependentAuras();
                        CastSpellOnTeam(SASpellIds.END_OF_ROUND, Team.Alliance);
                        CastSpellOnTeam(SASpellIds.END_OF_ROUND, Team.Horde);
                    }
                    else if (_status == SAStatus.RoundTwo)
                    {
                        _roundScores[1].Winner = (uint)_attackers;
                        _roundScores[1].Time = _totalTime;
                        ToggleTimer();

                        // Achievement Storm the Beach (1310)
                        foreach (var pair in GetPlayers())
                        {
                            Player player = Global.ObjAccessor.FindPlayer(pair.Key);

                            if (player)
                                if (GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID())) == _attackers &&
                                    _roundScores[1].Winner == _attackers)
                                    player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                        }

                        if (_roundScores[0].Time == _roundScores[1].Time)
                            EndBattleground(0);
                        else if (_roundScores[0].Time < _roundScores[1].Time)
                            EndBattleground(_roundScores[0].Winner == TeamId.Alliance ? Team.Alliance : Team.Horde);
                        else
                            EndBattleground(_roundScores[1].Winner == TeamId.Alliance ? Team.Alliance : Team.Horde);
                    }
                }
            }
        }

        private void ToggleTimer()
        {
            _timerEnabled = !_timerEnabled;
            UpdateWorldState(SAWorldStateIds.ENABLE_TIMER, _timerEnabled ? 1 : 0);
        }

        public override void EndBattleground(Team winner)
        {
            // honor reward for winning
            if (winner == Team.Alliance)
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Alliance);
            else if (winner == Team.Horde)
                RewardHonorToTeam(GetBonusHonorFromKill(1), Team.Horde);

            // complete map_end rewards (even if no team wins)
            RewardHonorToTeam(GetBonusHonorFromKill(2), Team.Alliance);
            RewardHonorToTeam(GetBonusHonorFromKill(2), Team.Horde);

            base.EndBattleground(winner);
        }

        private void UpdateDemolisherSpawns()
        {
            for (byte i = SACreatureTypes.DEMOLISHER_1; i <= SACreatureTypes.DEMOLISHER_8; i++)
                if (!BgCreatures[i].IsEmpty())
                {
                    Creature Demolisher = GetBGCreature(i);

                    if (Demolisher)
                        if (Demolisher.IsDead())
                        {
                            // Demolisher is not in list
                            if (!_demoliserRespawnList.ContainsKey(i))
                            {
                                _demoliserRespawnList[i] = GameTime.GetGameTimeMS() + 30000;
                            }
                            else
                            {
                                if (_demoliserRespawnList[i] < GameTime.GetGameTimeMS())
                                {
                                    Demolisher.Relocate(SAMiscConst.NpcSpawnlocs[i]);
                                    Demolisher.Respawn();
                                    _demoliserRespawnList.Remove(i);
                                }
                            }
                        }
                }
        }

        private void SendTransportInit(Player player)
        {
            if (!BgObjects[SAObjectTypes.BOAT_ONE].IsEmpty() ||
                !BgObjects[SAObjectTypes.BOAT_TWO].IsEmpty())
            {
                UpdateData transData = new(player.GetMapId());

                if (!BgObjects[SAObjectTypes.BOAT_ONE].IsEmpty())
                    GetBGObject(SAObjectTypes.BOAT_ONE).BuildCreateUpdateBlockForPlayer(transData, player);

                if (!BgObjects[SAObjectTypes.BOAT_TWO].IsEmpty())
                    GetBGObject(SAObjectTypes.BOAT_TWO).BuildCreateUpdateBlockForPlayer(transData, player);

                UpdateObject packet;
                transData.BuildPacket(out packet);
                player.SendPacket(packet);
            }
        }

        private void SendTransportsRemove(Player player)
        {
            if (!BgObjects[SAObjectTypes.BOAT_ONE].IsEmpty() ||
                !BgObjects[SAObjectTypes.BOAT_TWO].IsEmpty())
            {
                UpdateData transData = new(player.GetMapId());

                if (!BgObjects[SAObjectTypes.BOAT_ONE].IsEmpty())
                    GetBGObject(SAObjectTypes.BOAT_ONE).BuildOutOfRangeUpdateBlock(transData);

                if (!BgObjects[SAObjectTypes.BOAT_TWO].IsEmpty())
                    GetBGObject(SAObjectTypes.BOAT_TWO).BuildOutOfRangeUpdateBlock(transData);

                UpdateObject packet;
                transData.BuildPacket(out packet);
                player.SendPacket(packet);
            }
        }

        private bool IsGateDestroyed(uint gateId)
        {
            Cypher.Assert(gateId < SAMiscConst.Gates.Length);

            return _gateStatus[gateId] == SAGateState.AllianceGateDestroyed || _gateStatus[gateId] == SAGateState.HordeGateDestroyed;
        }

        public override bool IsSpellAllowed(uint spellId, Player player)
        {
            switch (spellId)
            {
                case SASpellIds.ALLIANCE_CONTROL_PHASE_SHIFT:
                    return _attackers == TeamId.Horde;
                case SASpellIds.HORDE_CONTROL_PHASE_SHIFT:
                    return _attackers == TeamId.Alliance;
                case BattlegroundConst.SpellPreparation:
                    return _status == SAStatus.Warmup || _status == SAStatus.SecondWarmup;
                default:
                    break;
            }

            return true;
        }

        public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
        {
            if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
                return false;

            switch (type)
            {
                case ScoreType.DestroyedDemolisher:
                    player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)SAObjectives.DemolishersDestroyed);

                    break;
                case ScoreType.DestroyedWall:
                    player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)SAObjectives.GatesDestroyed);

                    break;
                default:
                    break;
            }

            return true;
        }

        private SAGateInfo GetGate(uint entry)
        {
            foreach (var gate in SAMiscConst.Gates)
                if (gate.GameObjectId == entry)
                    return gate;

            return null;
        }
    }

}