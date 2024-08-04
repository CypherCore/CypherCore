// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.BattleGrounds;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.StrandOfTheAncients
{
    enum Status
    {
        NotStarted = 0,
        Warmup,
        RoundOne,
        SecondWarmup,
        RoundTwo,
        BonusRound
    }

    enum GateState
    {
        // Alliance Is Defender
        AllianceGateOk = 1,
        AllianceGateDamaged = 2,
        AllianceGateDestroyed = 3,
        // Horde Is Defender
        HordeGateOk = 4,
        HordeGateDamaged = 5,
        HordeGateDestroyed = 6,
    }

    enum GameObjectIds
    {
        SeaforiumBombH = 194086, // Used By Horde Players
        SeaforiumBombA = 190753, // Used By Alliance Players
        SeaforiumChargeH = 257572,
        SeaforiumChargeA = 257565,

        GraveyardWestH = 191307,
        GraveyardWestA = 191308,

        GraveyardEastH = 191305,
        GraveyardEastA = 191306,

        GraveyardCentralH = 191309,
        GraveyardCentralA = 191310,

        CollisionDoor = 194162,
        TitanRelicA = 194083,
        TitanRelicH = 194082,

        GateOfTheGreenEmerald = 190722,
        GateOfThePurpleAmethyst = 190723,
        GateOfTheBlueSapphire = 190724,
        GateOfTheRedSun = 190726,
        GateOfTheYellowMoon = 190727,
        ChamberOfAncientRelics = 192549,

        BoatOneA = 208000,
        BoatTwoH = 208001,
        BoatOneH = 193184,
        BoatTwoA = 193185
    }

    enum SoundIds
    {
        GraveyardTakenHorde = 8174,
        GraveyardTakenAlliance = 8212,
        DefeatHorde = 15905,
        VictoryHorde = 15906,
        VictoryAlliance = 15907,
        DefeatAlliance = 15908,
        WallDestroyedAlliance = 15909,
        WallDestroyedHorde = 15910,
        WallAttackedHorde = 15911,
        WallAttackedAlliance = 15912
    }

    enum TextIds
    {
        // Kanrethad
        RoundStarted = 1,
        Round1Finished = 2,

        // Rigger Sparklight / Gorgril Rigspark
        SparklightRigsparkSpawn = 1,

        // World Trigger
        BlueGateUnderAttack = 1,
        GreenGateUnderAttack = 2,
        RedGateUnderAttack = 3,
        PurpleGateUnderAttack = 4,
        YellowGateUnderAttack = 5,
        YellowGateDestroyed = 6,
        PurpleGateDestroyed = 7,
        RedGateDestroyed = 8,
        GreenGateDestroyed = 9,
        BlueGateDestroyed = 10,
        EastGraveyardCapturedA = 11,
        WestGraveyardCapturedA = 12,
        SouthGraveyardCapturedA = 13,
        EastGraveyardCapturedH = 14,
        WestGraveyardCapturedH = 15,
        SouthGraveyardCapturedH = 16,
        AncientGateUnderAttack = 17,
        AncientGateDestroyed = 18
    }

    enum WorldStateIds
    {
        Timer = 3557,
        AllyAttacks = 4352,
        HordeAttacks = 4353,
        PurpleGate = 3614,
        RedGate = 3617,
        BlueGate = 3620,
        GreenGate = 3623,
        YellowGate = 3638,
        AncientGate = 3849,

        LeftGyAlliance = 3635,
        RightGyAlliance = 3636,
        CenterGyAlliance = 3637,

        RightAttTokenAll = 3627,
        LeftAttTokenAll = 3626,
        LeftAttTokenHrd = 3629,
        RightAttTokenHrd = 3628,
        HordeDefenceToken = 3631,
        AllianceDefenceToken = 3630,

        RightGyHorde = 3632,
        LeftGyHorde = 3633,
        CenterGyHorde = 3634,

        BonusTimer = 3571,

        EnableTimer = 3564,
        AttackerTeam = 3690,
        DestroyedAllianceVehicles = 3955,
        DestroyedHordeVehicles = 3956,
    }

    enum StrandOfTheAncientsGraveyard
    {
        West,
        East,
        Central
    }

    enum BroadcastTextIds
    {
        AllianceCapturedTitanPortal = 28944,
        HordeCapturedTitanPortal = 28945,

        RoundTwoStartOneMinute = 29448,
        RoundTwoStartHalfMinute = 29449
    }

    enum DefenseLine
    {
        First,
        Second,
        Third,
        Last
    }

    enum PvpStats
    {
        GatesDestroyed = 231,
        DemolishersDestroyed = 232
    }

    enum EventIds
    {
        HordeAssaultStarted = 21702,
        AllianceAssaultStarted = 23748,
        TitanRelicActivated = 20572
    }

    enum CreatureIds
    {
        Kanrethad = 29,
        Demolisher = 28781,
        AntipersonnelCannon = 27894,
        RiggerSparklight = 29260,
        GorgrilRigspark = 29262,
        WorldTrigger = 22515,
    }

    enum SpellIds
    {
        TeleportDefender = 52364,
        TeleportAttackers = 60178,
        EndOfRound = 52459,
        RemoveSeaforium = 59077,
        AllianceControlPhaseShift = 60027,
        HordeControlPhaseShift = 60028,
        CarryingSeaforiumCharge = 52415
    }

    struct Misc
    {
        public const uint DataAttackers = 1;
        public const uint DataStatus = 2;

        public const uint SpawnDefenders = 1399;

        public const uint BoatStartTime = 60 * Time.InMilliseconds;
        public const uint WarmupTime = 120 * Time.InMilliseconds;
        public const uint RoundTime = 600 * Time.InMilliseconds;

        public static uint[] Factions = { 1732, 1735, };

        public static GateInfo[] Gates =
        {
            new GateInfo(GameObjectIds.GateOfTheGreenEmerald, WorldStateIds.GreenGate, TextIds.GreenGateUnderAttack, TextIds.GreenGateDestroyed, DefenseLine.First),
            new GateInfo(GameObjectIds.GateOfTheYellowMoon, WorldStateIds.YellowGate, TextIds.YellowGateUnderAttack, TextIds.YellowGateDestroyed, DefenseLine.Third),
            new GateInfo(GameObjectIds.GateOfTheBlueSapphire, WorldStateIds.BlueGate, TextIds.BlueGateUnderAttack, TextIds.BlueGateDestroyed, DefenseLine.First),
            new GateInfo(GameObjectIds.GateOfTheRedSun, WorldStateIds.RedGate, TextIds.RedGateUnderAttack, TextIds.RedGateDestroyed, DefenseLine.Second),
            new GateInfo(GameObjectIds.GateOfThePurpleAmethyst, WorldStateIds.PurpleGate, TextIds.PurpleGateUnderAttack, TextIds.PurpleGateDestroyed, DefenseLine.Second),
            new GateInfo(GameObjectIds.ChamberOfAncientRelics, WorldStateIds.AncientGate, TextIds.AncientGateUnderAttack, TextIds.AncientGateDestroyed, DefenseLine.Last)
        };

        public static Position[] SpawnPositionOnTransport =
        {
            new Position(0.0f, 5.0f, 9.6f, 3.14f),
            new Position(-6.0f, -3.0f, 8.6f, 0.0f)
        };
    }

    class GateInfo
    {
        public uint GameObjectId;
        public int WorldState;
        public byte DamagedText;
        public byte DestroyedText;
        public DefenseLine LineOfDefense;

        public GateInfo(GameObjectIds gameObjectId, WorldStateIds worldState, TextIds damagedText, TextIds destroyedText, DefenseLine lineOfDefense)
        {
            GameObjectId = (uint)gameObjectId;
            WorldState = (int)worldState;
            DamagedText = (byte)damagedText;
            DestroyedText = (byte)destroyedText;
            LineOfDefense = lineOfDefense;
        }
    }

    class RoundScore
    {
        public int Winner;
        public uint Time;
    }

    [Script(nameof(battleground_strand_of_the_ancients), 607)]
    class battleground_strand_of_the_ancients : BattlegroundScript
    {
        /// Id of attacker team
        int _attackers;

        /// Totale elapsed time of current round
        uint _totalTime;
        /// Max time of round
        uint _endRoundTimer;
        /// For know if boats has start moving or not yet
        bool _shipsStarted;
        /// Statu of battle (Start or not, and what round)
        Status _status;
        /// Score of each round
        RoundScore[] _roundScores = new RoundScore[2];
        /// used for know we are in timer phase or not (used for worldstate update)
        bool _timerEnabled;
        /// 5secs before starting the 1min countdown for second round
        uint _updateWaitTimer;
        /// for know if warning about second round start has been sent
        bool _signaledRoundTwo;
        /// for know if warning about second round start has been sent
        bool _signaledRoundTwoHalfMin;
        /// for know if second round has been init
        bool _initSecondRound;

        // [team][boat_idx]
        ObjectGuid[][] _boatGUIDs = new ObjectGuid[SharedConst.PvpTeamsCount][];
        List<ObjectGuid>[] _staticBombGUIDs = new List<ObjectGuid>[SharedConst.PvpTeamsCount]; // bombs ready to be picked up
        List<ObjectGuid> _dynamicBombGUIDs = new(); // bombs thrown by players, ready to explode/disarm

        ObjectGuid _graveyardWest;
        ObjectGuid _graveyardEast;
        ObjectGuid _graveyardCentral;
        List<ObjectGuid> _gateGUIDs = new();
        ObjectGuid _collisionDoorGUID;
        ObjectGuid _kanrethadGUID;
        ObjectGuid _titanRelicGUID;

        public battleground_strand_of_the_ancients(BattlegroundMap map) : base(map)
        {
            _timerEnabled = false;
            _updateWaitTimer = 0;
            _signaledRoundTwo = false;
            _signaledRoundTwoHalfMin = false;
            _initSecondRound = false;
            _attackers = RandomHelper.IRand(BattleGroundTeamId.Alliance, BattleGroundTeamId.Horde);
            _totalTime = 0;
            _endRoundTimer = 0;
            _shipsStarted = false;
            _status = Status.NotStarted;

            foreach (RoundScore roundScore in _roundScores)
            {
                roundScore.Winner = BattleGroundTeamId.Alliance;
                roundScore.Time = 0;
            }

            for (var i = 0; i < SharedConst.PvpTeamsCount; i++)
            {
                _boatGUIDs[i] = new ObjectGuid[2];
                _staticBombGUIDs[i] = new();
            }
        }

        public override void OnInit()
        {
            base.OnInit();
            _status = Status.Warmup;
            ResetObjs();
        }

        public override void OnUpdate(uint diff)
        {
            base.OnUpdate(diff);
            if (_initSecondRound)
            {
                if (_updateWaitTimer < diff)
                {
                    if (!_signaledRoundTwo)
                    {
                        _signaledRoundTwo = true;
                        _initSecondRound = false;
                        battleground.SendBroadcastText((uint)BroadcastTextIds.RoundTwoStartOneMinute, ChatMsg.BgSystemNeutral);
                    }
                }
                else
                {
                    _updateWaitTimer -= diff;
                    return;
                }
            }
            _totalTime += diff;

            if (_status == Status.Warmup)
            {
                _endRoundTimer = Misc.RoundTime;
                UpdateWorldState((int)WorldStateIds.Timer, (int)(GameTime.GetGameTime() + (_endRoundTimer / 1000)));
                if (_totalTime >= Misc.WarmupTime)
                {
                    Creature c = FindKanrethad();
                    if (c != null)
                        battleground.SendChatMessage(c, (byte)TextIds.RoundStarted);

                    _totalTime = 0;
                    ToggleTimer();
                    _status = Status.RoundOne;
                    TriggerGameEvent((uint)((_attackers == BattleGroundTeamId.Alliance) ? EventIds.AllianceAssaultStarted : EventIds.HordeAssaultStarted));
                }
                if (_totalTime >= Misc.BoatStartTime)
                    StartShips();
                return;
            }
            else if (_status == Status.SecondWarmup)
            {
                if (_roundScores[0].Time < Misc.RoundTime)
                    _endRoundTimer = _roundScores[0].Time;
                else
                    _endRoundTimer = Misc.RoundTime;

                UpdateWorldState((int)WorldStateIds.Timer, (int)(GameTime.GetGameTime() + (_endRoundTimer / 1000)));
                if (_totalTime >= 60000)
                {
                    Creature c = FindKanrethad();
                    if (c != null)
                        battleground.SendChatMessage(c, (byte)TextIds.RoundStarted);

                    _totalTime = 0;
                    ToggleTimer();
                    _status = Status.RoundTwo;
                    TriggerGameEvent((uint)(_attackers == BattleGroundTeamId.Alliance ? EventIds.AllianceAssaultStarted : EventIds.HordeAssaultStarted));
                    // status was set to STATUS_WAIT_JOIN manually for Preparation, set it back now
                    battleground.SetStatus(BattlegroundStatus.InProgress);
                    foreach (var (playerGuid, _) in battleground.GetPlayers())
                    {
                        Player p = Global.ObjAccessor.FindPlayer(playerGuid);
                        if (p != null)
                            p.RemoveAurasDueToSpell(BattlegroundConst.SpellPreparation);
                    }
                }
                if (_totalTime >= 30000)
                {
                    if (!_signaledRoundTwoHalfMin)
                    {
                        _signaledRoundTwoHalfMin = true;
                        battleground.SendBroadcastText((uint)BroadcastTextIds.RoundTwoStartHalfMinute, ChatMsg.BgSystemNeutral);
                    }
                }
                StartShips();
                return;
            }
            else if (battleground.GetStatus() == BattlegroundStatus.InProgress)
            {
                if (_status == Status.RoundOne)
                {
                    if (_totalTime >= Misc.RoundTime)
                    {
                        EndRound();
                        return;
                    }
                }
                else if (_status == Status.RoundTwo)
                {
                    if (_totalTime >= _endRoundTimer)
                    {
                        battleground.CastSpellOnTeam((uint)SpellIds.EndOfRound, Team.Alliance);
                        battleground.CastSpellOnTeam((uint)SpellIds.EndOfRound, Team.Horde);
                        _roundScores[1].Time = Misc.RoundTime;
                        _roundScores[1].Winner = (_attackers == BattleGroundTeamId.Alliance) ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance;
                        if (_roundScores[0].Time == _roundScores[1].Time)
                            battleground.EndBattleground(Team.Other);
                        else if (_roundScores[0].Time < _roundScores[1].Time)
                            battleground.EndBattleground(_roundScores[0].Winner == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                        else
                            battleground.EndBattleground(_roundScores[1].Winner == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                        return;
                    }
                }
            }
        }

        void Reset()
        {
            _totalTime = 0;
            _shipsStarted = false;
            _status = Status.Warmup;
        }

        void ResetObjs()
        {
            foreach (ObjectGuid bombGuid in _dynamicBombGUIDs)
            {
                GameObject bomb = battlegroundMap.GetGameObject(bombGuid);
                if (bomb != null)
                    bomb.Delete();
            }

            _dynamicBombGUIDs.Clear();

            int defenders = _attackers == BattleGroundTeamId.Alliance ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance;

            _totalTime = 0;
            _shipsStarted = false;

            UpdateWorldState((int)WorldStateIds.AllyAttacks, _attackers == BattleGroundTeamId.Alliance);
            UpdateWorldState((int)WorldStateIds.HordeAttacks, _attackers == BattleGroundTeamId.Horde);

            UpdateWorldState((int)WorldStateIds.RightAttTokenAll, _attackers == BattleGroundTeamId.Alliance);
            UpdateWorldState((int)WorldStateIds.LeftAttTokenAll, _attackers == BattleGroundTeamId.Alliance);

            UpdateWorldState((int)WorldStateIds.RightAttTokenHrd, _attackers == BattleGroundTeamId.Horde);
            UpdateWorldState((int)WorldStateIds.LeftAttTokenHrd, _attackers == BattleGroundTeamId.Horde);

            UpdateWorldState((int)WorldStateIds.HordeDefenceToken, defenders == BattleGroundTeamId.Horde);
            UpdateWorldState((int)WorldStateIds.AllianceDefenceToken, defenders == BattleGroundTeamId.Alliance);

            CaptureGraveyard(StrandOfTheAncientsGraveyard.Central, defenders);
            CaptureGraveyard(StrandOfTheAncientsGraveyard.West, defenders);
            CaptureGraveyard(StrandOfTheAncientsGraveyard.East, defenders);

            UpdateWorldState((int)WorldStateIds.AttackerTeam, _attackers);

            foreach (ObjectGuid guid in _gateGUIDs)
            {
                GameObject gate = battlegroundMap.GetGameObject(guid);
                if (gate != null)
                    gate.SetDestructibleState(GameObjectDestructibleState.Intact, null, true);
            }

            GateState state = _attackers == BattleGroundTeamId.Alliance ? GateState.HordeGateOk : GateState.AllianceGateOk;
            UpdateWorldState((int)WorldStateIds.PurpleGate, (int)state);
            UpdateWorldState((int)WorldStateIds.RedGate, (int)state);
            UpdateWorldState((int)WorldStateIds.BlueGate, (int)state);
            UpdateWorldState((int)WorldStateIds.GreenGate, (int)state);
            UpdateWorldState((int)WorldStateIds.YellowGate, (int)state);
            UpdateWorldState((int)WorldStateIds.AncientGate, (int)state);

            battlegroundMap.UpdateSpawnGroupConditions();

            GameObject door = battlegroundMap.GetGameObject(_collisionDoorGUID);
            if (door != null)
                door.ResetDoorOrButton();

            battleground.SetStatus(BattlegroundStatus.WaitJoin);
            battlegroundMap.DoOnPlayers(SendTransportInit);

            TeleportPlayers();
        }

        void StartShips()
        {
            if (_shipsStarted)
                return;

            foreach (ObjectGuid guid in _boatGUIDs[_attackers])
            {
                GameObject boat = battlegroundMap.GetGameObject(guid);
                if (boat != null)
                {
                    boat.SetGoState(GameObjectState.TransportStopped);

                    // make sure every player knows the transport exists & is moving
                    foreach (var (playerGuid, _) in battleground.GetPlayers())
                    {
                        Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                        if (player != null)
                            boat.SendUpdateToPlayer(player);
                    }
                }
            }

            _shipsStarted = true;
        }

        public override void OnPlayerJoined(Player player, bool inBattleground)
        {
            base.OnPlayerJoined(player, inBattleground);
            SendTransportInit(player);
            if (!inBattleground)
                TeleportToEntrancePosition(player);
        }

        void TeleportPlayers()
        {
            foreach (var (playerGuid, _) in battleground.GetPlayers())
            {
                Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                if (player != null)
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

        // This function teleports player to entrance position, but it's not implemented correctly
        // It is also used on round swap; which should be handled by the following spells: 52365, 52528, 53464, 53465 (Split Teleport (FACTION) (Boat X))
        // This spell however cannot work with current system because of grid limitations.
        // On battleground start, this function should work fine, except that it is called to late and we need a NearTeleport to solve this.
        void TeleportToEntrancePosition(Player player)
        {
            if (Battleground.GetTeamIndexByTeamId(battleground.GetPlayerTeam(player.GetGUID())) == _attackers)
            {
                ObjectGuid boatGUID = _boatGUIDs[_attackers][RandomHelper.URand(0, 1)];

                GameObject boat = battlegroundMap.GetGameObject(boatGUID);
                if (boat != null)
                {
                    ITransport transport = boat.ToTransportBase();
                    if (transport != null)
                    {
                        player.Relocate(Misc.SpawnPositionOnTransport[_attackers]);
                        transport.AddPassenger(player);
                        player.m_movementInfo.transport.pos.Relocate(Misc.SpawnPositionOnTransport[_attackers]);
                        Misc.SpawnPositionOnTransport[_attackers].GetPosition(out float x, out float y, out float z, out float o);
                        transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                        player.Relocate(x, y, z, o);

                        if (player.IsInWorld)
                            player.NearTeleportTo(x, y, z, o);
                    }
                }
            }
            else
            {
                WorldSafeLocsEntry defenderSpawn = Global.ObjectMgr.GetWorldSafeLoc(Misc.SpawnDefenders);
                if (defenderSpawn != null)
                {
                    if (player.IsInWorld)
                        player.TeleportTo(defenderSpawn.Loc);
                    else
                        player.WorldRelocate(defenderSpawn.Loc);
                }
            }
        }

        public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker)
        {
            base.ProcessEvent(obj, eventId, invoker);

            switch ((EventIds)eventId)
            {
                case EventIds.AllianceAssaultStarted:
                    foreach (ObjectGuid bombGuid in _staticBombGUIDs[BattleGroundTeamId.Alliance])
                    {
                        GameObject bomb = battlegroundMap.GetGameObject(bombGuid);
                        if (bomb != null)
                            bomb.RemoveFlag(GameObjectFlags.NotSelectable);
                    }
                    break;
                case EventIds.HordeAssaultStarted:
                    foreach (ObjectGuid bombGuid in _staticBombGUIDs[BattleGroundTeamId.Horde])
                    {
                        GameObject bomb = battlegroundMap.GetGameObject(bombGuid);
                        if (bomb != null)
                            bomb.RemoveFlag(GameObjectFlags.NotSelectable);
                    }
                    break;
                default:
                    break;
            }
            GameObject go = obj?.ToGameObject();
            if (go != null)
            {
                switch (go.GetGoType())
                {
                    case GameObjectTypes.Goober:
                        if (invoker != null)
                            if (eventId == (uint)EventIds.TitanRelicActivated)
                                TitanRelicActivated(invoker.ToPlayer());
                        break;
                    case GameObjectTypes.DestructibleBuilding:
                    {
                        GateInfo gate = GetGate(obj.GetEntry());
                        if (gate != null)
                        {
                            uint gateId = gate.GameObjectId;

                            // damaged
                            if (eventId == go.GetGoInfo().DestructibleBuilding.DamagedEvent)
                            {
                                GateState gateState = _attackers == BattleGroundTeamId.Horde ? GateState.AllianceGateDamaged : GateState.HordeGateDamaged;
                                Creature c = obj.FindNearestCreature((uint)CreatureIds.WorldTrigger, 500.0f);
                                if (c != null)
                                    battleground.SendChatMessage(c, gate.DamagedText, invoker);

                                battleground.PlaySoundToAll((uint)(_attackers == BattleGroundTeamId.Alliance ? SoundIds.WallAttackedAlliance : SoundIds.WallAttackedHorde));

                                UpdateWorldState(gate.WorldState, (int)gateState);
                            }
                            // destroyed
                            else if (eventId == go.GetGoInfo().DestructibleBuilding.DestroyedEvent)
                            {
                                GateState gateState = _attackers == BattleGroundTeamId.Horde ? GateState.AllianceGateDestroyed : GateState.HordeGateDestroyed;
                                Creature c = obj.FindNearestCreature((uint)CreatureIds.WorldTrigger, 500.0f);
                                if (c != null)
                                    battleground.SendChatMessage(c, gate.DestroyedText, invoker);

                                battleground.PlaySoundToAll((uint)(_attackers == BattleGroundTeamId.Alliance ? SoundIds.WallDestroyedAlliance : SoundIds.WallDestroyedHorde));

                                // check if other gate from same line of defense is already destroyed for honor reward
                                bool rewardHonor = true;
                                switch ((GameObjectIds)gateId)
                                {
                                    case GameObjectIds.GateOfTheGreenEmerald:
                                        if (IsGateDestroyed(GetGate((uint)GameObjectIds.GateOfTheBlueSapphire)))
                                            rewardHonor = false;
                                        break;
                                    case GameObjectIds.GateOfTheBlueSapphire:
                                        if (IsGateDestroyed(GetGate((uint)GameObjectIds.GateOfTheGreenEmerald)))
                                            rewardHonor = false;
                                        break;
                                    case GameObjectIds.GateOfTheRedSun:
                                        if (IsGateDestroyed(GetGate((uint)GameObjectIds.GateOfThePurpleAmethyst)))
                                            rewardHonor = false;
                                        break;
                                    case GameObjectIds.GateOfThePurpleAmethyst:
                                        if (IsGateDestroyed(GetGate((uint)GameObjectIds.GateOfTheRedSun)))
                                            rewardHonor = false;
                                        break;
                                    default:
                                        break;
                                }

                                if (invoker != null)
                                {
                                    Unit unit = invoker.ToUnit();
                                    if (unit != null)
                                    {
                                        Player player = unit.GetCharmerOrOwnerPlayerOrPlayerItself();
                                        if (player != null)
                                        {
                                            battleground.UpdatePvpStat(player, (uint)PvpStats.GatesDestroyed, 1);
                                            if (rewardHonor)
                                                battleground.UpdatePlayerScore(player, ScoreType.BonusHonor, battleground.GetBonusHonorFromKill(1));
                                        }
                                    }
                                }

                                if (rewardHonor)
                                    MakeObjectsInteractable(gate.LineOfDefense);

                                UpdateWorldState(gate.WorldState, (int)gateState);
                                battlegroundMap.UpdateSpawnGroupConditions();
                            }
                        }
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        public override void OnUnitKilled(Creature victim, Unit killer)
        {
            if (victim.GetEntry() == (uint)CreatureIds.Demolisher)
            {
                Player killerPlayer = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (killerPlayer != null)
                    battleground.UpdatePvpStat(killerPlayer, (byte)PvpStats.DemolishersDestroyed, 1);
                int worldStateId = _attackers == BattleGroundTeamId.Horde ? (int)WorldStateIds.DestroyedHordeVehicles : (int)WorldStateIds.DestroyedAllianceVehicles;
                int currentDestroyedVehicles = Global.WorldStateMgr.GetValue(worldStateId, battlegroundMap);
                UpdateWorldState(worldStateId, currentDestroyedVehicles + 1);
            }
        }

        void CaptureGraveyard(StrandOfTheAncientsGraveyard graveyard, int teamId)
        {
            switch (graveyard)
            {
                case StrandOfTheAncientsGraveyard.West:
                    UpdateWorldState((int)WorldStateIds.LeftGyAlliance, teamId == BattleGroundTeamId.Alliance);
                    UpdateWorldState((int)WorldStateIds.LeftGyHorde, teamId == BattleGroundTeamId.Horde);
                    break;
                case StrandOfTheAncientsGraveyard.East:
                    UpdateWorldState((int)WorldStateIds.RightGyAlliance, teamId == BattleGroundTeamId.Alliance);
                    UpdateWorldState((int)WorldStateIds.RightGyHorde, teamId == BattleGroundTeamId.Horde);
                    break;
                case StrandOfTheAncientsGraveyard.Central:
                    UpdateWorldState((int)WorldStateIds.CenterGyAlliance, teamId == BattleGroundTeamId.Alliance);
                    UpdateWorldState((int)WorldStateIds.CenterGyHorde, teamId == BattleGroundTeamId.Horde);

                    CaptureGraveyard(StrandOfTheAncientsGraveyard.East, teamId);
                    CaptureGraveyard(StrandOfTheAncientsGraveyard.West, teamId);
                    break;
                default:
                    break;
            }
        }

        void TitanRelicActivated(Player clicker)
        {
            if (clicker == null)
                return;

            int clickerTeamId = Battleground.GetTeamIndexByTeamId(battleground.GetPlayerTeam(clicker.GetGUID()));
            if (clickerTeamId == _attackers)
            {
                if (clickerTeamId == BattleGroundTeamId.Alliance)
                    battleground.SendBroadcastText((uint)BroadcastTextIds.AllianceCapturedTitanPortal, ChatMsg.BgSystemAlliance);
                else
                    battleground.SendBroadcastText((uint)BroadcastTextIds.HordeCapturedTitanPortal, ChatMsg.BgSystemHorde);

                if (_status == Status.RoundOne)
                {
                    EndRound();
                    // Achievement Storm the Beach (1310)
                    foreach (var (guid, _) in battleground.GetPlayers())
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player != null)
                            if (Battleground.GetTeamIndexByTeamId(battleground.GetPlayerTeam(player.GetGUID())) == _attackers)
                                player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                    }

                    Creature c = FindKanrethad();
                    if (c != null)
                        battleground.SendChatMessage(c, (byte)TextIds.Round1Finished);
                }
                else if (_status == Status.RoundTwo)
                {
                    _roundScores[1].Winner = _attackers;
                    _roundScores[1].Time = _totalTime;
                    ToggleTimer();
                    // Achievement Storm the Beach (1310)
                    foreach (var (guid, _) in battleground.GetPlayers())
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player != null)
                            if (Battleground.GetTeamIndexByTeamId(battleground.GetPlayerTeam(player.GetGUID())) == _attackers && _roundScores[1].Winner == _attackers)
                                player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                    }

                    if (_roundScores[0].Time == _roundScores[1].Time)
                        battleground.EndBattleground(Team.Other);
                    else if (_roundScores[0].Time < _roundScores[1].Time)
                        battleground.EndBattleground(_roundScores[0].Winner == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                    else
                        battleground.EndBattleground(_roundScores[1].Winner == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                }
            }
        }

        void ToggleTimer()
        {
            _timerEnabled = !_timerEnabled;
            UpdateWorldState((int)WorldStateIds.EnableTimer, _timerEnabled);
        }

        public override void OnEnd(Team winner)
        {
            base.OnEnd(winner);

            // honor reward for winning
            if (winner == Team.Alliance)
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Alliance);
            else if (winner == Team.Horde)
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(1), Team.Horde);

            // complete map_end rewards (even if no team wins)
            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(2), Team.Alliance);
            battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(2), Team.Horde);
        }

        void SendTransportInit(Player player)
        {
            foreach (ObjectGuid boatGuid in _boatGUIDs[_attackers])
            {
                GameObject boat = battlegroundMap.GetGameObject(boatGuid);
                if (boat != null)
                    boat.SendUpdateToPlayer(player);
            }
        }

        bool IsGateDestroyed(GateInfo gateInfo)
        {
            if (gateInfo == null)
                return false;

            int value = battlegroundMap.GetWorldStateValue(gateInfo.WorldState);
            return value == (int)GateState.AllianceGateDestroyed || value == (int)GateState.HordeGateDestroyed;
        }

        void HandleCaptureGraveyardAction(GameObject graveyardBanner, Player player)
        {
            if (graveyardBanner == null || player == null)
                return;

            int teamId = Battleground.GetTeamIndexByTeamId(battleground.GetPlayerTeam(player.GetGUID()));
            // Only attackers can capture graveyard by gameobject action
            if (teamId != _attackers)
                return;

            switch ((GameObjectIds)graveyardBanner.GetEntry())
            {
                case GameObjectIds.GraveyardWestA:
                case GameObjectIds.GraveyardWestH:
                    CaptureGraveyard(StrandOfTheAncientsGraveyard.West, teamId);
                    break;
                case GameObjectIds.GraveyardEastA:
                case GameObjectIds.GraveyardEastH:
                    CaptureGraveyard(StrandOfTheAncientsGraveyard.East, teamId);
                    break;
                case GameObjectIds.GraveyardCentralA:
                case GameObjectIds.GraveyardCentralH:
                    CaptureGraveyard(StrandOfTheAncientsGraveyard.Central, teamId);
                    break;
                default:
                    break;
            }
        }

        void MakeObjectsInteractable(DefenseLine defenseLine)
        {
            var makeInteractable = (ObjectGuid guid) =>
            {
                GameObject gameObject = battlegroundMap.GetGameObject(guid);
                if (gameObject != null)
                    gameObject.RemoveFlag(GameObjectFlags.NotSelectable);
            };

            switch (defenseLine)
            {
                case DefenseLine.First:
                    makeInteractable(_graveyardWest);
                    makeInteractable(_graveyardEast);
                    break;
                case DefenseLine.Second:
                    makeInteractable(_graveyardCentral);
                    break;
                case DefenseLine.Third:
                    break;
                case DefenseLine.Last:
                    // make titan orb interactable
                    GameObject door = battlegroundMap.GetGameObject(_collisionDoorGUID);
                    if (door != null)
                        door.UseDoorOrButton();

                    makeInteractable(_titanRelicGUID);
                    break;
                default:
                    break;
            }
        }

        Creature FindKanrethad()
        {
            return battlegroundMap.GetCreature(_kanrethadGUID);
        }

        void EndRound()
        {
            _roundScores[0].Winner = _attackers;
            _roundScores[0].Time = Math.Min(_totalTime, Misc.RoundTime);

            _attackers = (_attackers == BattleGroundTeamId.Alliance) ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance;
            _status = Status.SecondWarmup;
            _totalTime = 0;
            ToggleTimer();

            _updateWaitTimer = 5000;
            _signaledRoundTwo = false;
            _signaledRoundTwoHalfMin = false;
            _initSecondRound = true;
            ResetObjs();
            battlegroundMap.UpdateAreaDependentAuras();

            battleground.CastSpellOnTeam((uint)SpellIds.EndOfRound, Team.Alliance);
            battleground.CastSpellOnTeam((uint)SpellIds.EndOfRound, Team.Horde);

            battleground.RemoveAuraOnTeam((uint)SpellIds.CarryingSeaforiumCharge, Team.Horde);
            battleground.RemoveAuraOnTeam((uint)SpellIds.CarryingSeaforiumCharge, Team.Alliance);
        }

        public override void OnGameObjectCreate(GameObject gameobject)
        {
            base.OnGameObjectCreate(gameobject);

            if (gameobject.GetGoType() == GameObjectTypes.DestructibleBuilding)
                _gateGUIDs.Add(gameobject.GetGUID());

            switch ((GameObjectIds)gameobject.GetEntry())
            {
                case GameObjectIds.BoatOneA:
                    _boatGUIDs[BattleGroundTeamId.Alliance][0] = gameobject.GetGUID();
                    break;
                case GameObjectIds.BoatTwoA:
                    _boatGUIDs[BattleGroundTeamId.Alliance][1] = gameobject.GetGUID();
                    break;
                case GameObjectIds.BoatOneH:
                    _boatGUIDs[BattleGroundTeamId.Horde][0] = gameobject.GetGUID();
                    break;
                case GameObjectIds.BoatTwoH:
                    _boatGUIDs[BattleGroundTeamId.Horde][1] = gameobject.GetGUID();
                    break;
                case GameObjectIds.SeaforiumBombA:
                    _staticBombGUIDs[BattleGroundTeamId.Alliance].Add(gameobject.GetGUID());
                    if (_status != Status.SecondWarmup && _status != Status.Warmup)
                        gameobject.RemoveFlag(GameObjectFlags.NotSelectable);
                    break;
                case GameObjectIds.SeaforiumBombH:
                    _staticBombGUIDs[BattleGroundTeamId.Horde].Add(gameobject.GetGUID());
                    if (_status != Status.SecondWarmup && _status != Status.Warmup)
                        gameobject.RemoveFlag(GameObjectFlags.NotSelectable);
                    break;
                case GameObjectIds.SeaforiumChargeA:
                case GameObjectIds.SeaforiumChargeH:
                    _dynamicBombGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.GraveyardEastA:
                case GameObjectIds.GraveyardEastH:
                    _graveyardEast = gameobject.GetGUID();
                    break;
                case GameObjectIds.GraveyardWestA:
                case GameObjectIds.GraveyardWestH:
                    _graveyardWest = gameobject.GetGUID();
                    break;
                case GameObjectIds.GraveyardCentralA:
                case GameObjectIds.GraveyardCentralH:
                    _graveyardCentral = gameobject.GetGUID();
                    break;
                case GameObjectIds.CollisionDoor:
                    _collisionDoorGUID = gameobject.GetGUID();
                    break;
                case GameObjectIds.TitanRelicA:
                case GameObjectIds.TitanRelicH:
                    _titanRelicGUID = gameobject.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override void DoAction(uint actionId, WorldObject source, WorldObject target)
        {
            base.DoAction(actionId, source, target);

            switch (actionId)
            {
                case 0:
                    HandleCaptureGraveyardAction(target?.ToGameObject(), source?.ToPlayer());
                    break;
                default:
                    break;
            }
        }

        public override void OnCreatureCreate(Creature creature)
        {
            base.OnCreatureCreate(creature);
            switch ((CreatureIds)creature.GetEntry())
            {
                case CreatureIds.Demolisher:
                    creature.SetFaction(Misc.Factions[_attackers]);
                    break;
                case CreatureIds.AntipersonnelCannon:
                    creature.SetFaction(Misc.Factions[_attackers == BattleGroundTeamId.Horde ? BattleGroundTeamId.Alliance : BattleGroundTeamId.Horde]);
                    break;
                case CreatureIds.Kanrethad:
                    _kanrethadGUID = creature.GetGUID();
                    break;
                case CreatureIds.RiggerSparklight:
                case CreatureIds.GorgrilRigspark:
                    creature.GetAI().Talk((uint)TextIds.SparklightRigsparkSpawn);
                    break;
                default:
                    break;
            }
        }

        public override uint GetData(uint dataId)
        {
            switch (dataId)
            {
                case Misc.DataAttackers:
                    return (uint)_attackers;
                case Misc.DataStatus:
                    return (uint)_status;
                default:
                    return base.GetData(dataId);
            }
        }

        static GateInfo GetGate(uint entry)
        {
            foreach (GateInfo gate in Misc.Gates)
                if (gate.GameObjectId == entry)
                    return gate;

            return null;
        }
    }
}