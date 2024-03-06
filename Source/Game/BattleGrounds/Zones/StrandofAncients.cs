// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones.StrandOfAncients
{
    public class BgStrandOfAncients : Battleground
    {
        /// Id of attacker team
        int Attackers;
        /// Totale elapsed time of current round
        uint TotalTime;
        /// Max time of round
        uint EndRoundTimer;
        /// For know if boats has start moving or not yet
        bool ShipsStarted;
        /// Statu of battle (Start or not, and what round)
        SAStatus Status;
        /// Score of each round
        RoundScore[] RoundScores = new RoundScore[2];
        /// used for know we are in timer phase or not (used for worldstate update)
        bool TimerEnabled;
        /// 5secs before starting the 1min countdown for second round
        uint UpdateWaitTimer;
        /// for know if warning about second round start has been sent
        bool SignaledRoundTwo;
        /// for know if warning about second round start has been sent
        bool SignaledRoundTwoHalfMin;
        /// for know if second round has been init
        bool InitSecondRound;

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

        public BgStrandOfAncients(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            StartMessageIds[BattlegroundConst.EventIdFourth] = 0;

            Attackers = RandomHelper.IRand(BattleGroundTeamId.Alliance, BattleGroundTeamId.Horde);

            for (var i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                _boatGUIDs[i] = new ObjectGuid[2];
                _staticBombGUIDs[i] = new();
            }
        }

        public override void Reset()
        {
            TotalTime = 0;
            ShipsStarted = false;
            Status = SAStatus.Warmup;
        }

        public override bool SetupBattleground()
        {
            return true;
        }

        bool ResetObjs()
        {
            foreach (ObjectGuid bombGuid in _dynamicBombGUIDs)
            {
                GameObject bomb = GetBgMap().GetGameObject(bombGuid);
                if (bomb != null)
                    bomb.Delete();
            }

            _dynamicBombGUIDs.Clear();

            int defenders = Attackers == BattleGroundTeamId.Alliance ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance;

            TotalTime = 0;
            ShipsStarted = false;

            UpdateWorldState((int)WorldStateIds.AllyAttacks, Attackers == BattleGroundTeamId.Alliance);
            UpdateWorldState((int)WorldStateIds.HordeAttacks, Attackers == BattleGroundTeamId.Horde);

            UpdateWorldState((int)WorldStateIds.RightAttTokenAll, Attackers == BattleGroundTeamId.Alliance);
            UpdateWorldState((int)WorldStateIds.LeftAttTokenAll, Attackers == BattleGroundTeamId.Alliance);

            UpdateWorldState((int)WorldStateIds.RightAttTokenHrd, Attackers == BattleGroundTeamId.Horde);
            UpdateWorldState((int)WorldStateIds.LeftAttTokenHrd, Attackers == BattleGroundTeamId.Horde);

            UpdateWorldState((int)WorldStateIds.HordeDefenceToken, defenders == BattleGroundTeamId.Horde);
            UpdateWorldState((int)WorldStateIds.AllianceDefenceToken, defenders == BattleGroundTeamId.Alliance);

            CaptureGraveyard(StrandOfTheAncientsGraveyard.Central, defenders);
            CaptureGraveyard(StrandOfTheAncientsGraveyard.West, defenders);
            CaptureGraveyard(StrandOfTheAncientsGraveyard.East, defenders);

            UpdateWorldState((int)WorldStateIds.AttackerTeam, Attackers);

            foreach (ObjectGuid guid in _gateGUIDs)
            {
                GameObject gate = GetBgMap().GetGameObject(guid);
                if (gate != null)
                    gate.SetDestructibleState(GameObjectDestructibleState.Intact, null, true);
            }

            int state = (int)(Attackers == BattleGroundTeamId.Alliance ? GateState.HordeGateOk : GateState.AllianceGateOk);
            UpdateWorldState((int)WorldStateIds.PurpleGate, state);
            UpdateWorldState((int)WorldStateIds.RedGate, state);
            UpdateWorldState((int)WorldStateIds.BlueGate, state);
            UpdateWorldState((int)WorldStateIds.GreenGate, state);
            UpdateWorldState((int)WorldStateIds.YellowGate, state);
            UpdateWorldState((int)WorldStateIds.AncientGate, state);

            GetBgMap().UpdateSpawnGroupConditions();

            GameObject door = GetBgMap().GetGameObject(_collisionDoorGUID);
            if (door != null)
                door.ResetDoorOrButton();

            SetStatus(BattlegroundStatus.WaitJoin);
            GetBgMap().DoOnPlayers(SendTransportInit);

            TeleportPlayers();
            return true;
        }

        void StartShips()
        {
            if (ShipsStarted)
                return;

            foreach (ObjectGuid guid in _boatGUIDs[Attackers])
            {
                GameObject boat = GetBgMap().GetGameObject(guid);
                if (boat != null)
                {
                    boat.SetGoState(GameObjectState.TransportStopped);

                    foreach (var (playerGuid, _) in GetPlayers())
                    {
                        Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                        if (player != null)
                        {
                            UpdateData data = new(player.GetMapId());
                            boat.BuildValuesUpdateBlockForPlayer(data, player);

                            data.BuildPacket(out var pkt);
                            player.SendPacket(pkt);
                        }
                    }
                }
            }
            ShipsStarted = true;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (InitSecondRound)
            {
                if (UpdateWaitTimer < diff)
                {
                    if (!SignaledRoundTwo)
                    {
                        SignaledRoundTwo = true;
                        InitSecondRound = false;
                        SendBroadcastText((uint)BroadcastTextIds.RoundTwoStartOneMinute, ChatMsg.BgSystemNeutral);
                    }
                }
                else
                {
                    UpdateWaitTimer -= diff;
                    return;
                }
            }
            TotalTime += diff;

            if (Status == SAStatus.Warmup)
            {
                EndRoundTimer = MiscConst.RoundlengthTimer;
                UpdateWorldState((int)WorldStateIds.Timer, (int)(GameTime.GetGameTime() + EndRoundTimer));
                if (TotalTime >= MiscConst.WarmuplengthTimer)
                {
                    Creature creature = FindKanrethad();
                    if (creature != null)
                        SendChatMessage(creature, (byte)TextIds.RoundStarted);

                    TotalTime = 0;
                    ToggleTimer();
                    Status = SAStatus.RoundOne;
                    TriggerGameEvent(Attackers == BattleGroundTeamId.Alliance ? 23748 : 21702u);
                }
                if (TotalTime >= MiscConst.BoatStartTimer)
                    StartShips();
                return;
            }
            else if (Status == SAStatus.SecondWarmup)
            {
                if (RoundScores[0].time < MiscConst.RoundlengthTimer)
                    EndRoundTimer = RoundScores[0].time;
                else
                    EndRoundTimer = MiscConst.RoundlengthTimer;

                UpdateWorldState((int)WorldStateIds.Timer, (int)(GameTime.GetGameTime() + EndRoundTimer / 1000));
                if (TotalTime >= 60000)
                {
                    Creature creature = FindKanrethad();
                    if (creature != null)
                        SendChatMessage(creature, (byte)TextIds.RoundStarted);

                    TotalTime = 0;
                    ToggleTimer();
                    Status = SAStatus.RoundTwo;
                    TriggerGameEvent(Attackers == BattleGroundTeamId.Alliance ? 23748 : 21702u);
                    // status was set to STATUS_WAIT_JOIN manually for Preparation, set it back now
                    SetStatus(BattlegroundStatus.InProgress);
                    foreach (var (playerGuid, _) in GetPlayers())
                    {
                        Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                        if (player != null)
                            player.RemoveAurasDueToSpell(BattlegroundConst.SpellPreparation);
                    }
                }
                if (TotalTime >= 30000)
                {
                    if (!SignaledRoundTwoHalfMin)
                    {
                        SignaledRoundTwoHalfMin = true;
                        SendBroadcastText((uint)BroadcastTextIds.RoundTwoStartHalfMinute, ChatMsg.BgSystemNeutral);
                    }
                }
                StartShips();
                return;
            }
            else if (GetStatus() == BattlegroundStatus.InProgress)
            {
                if (Status == SAStatus.RoundOne)
                {
                    if (TotalTime >= MiscConst.RoundlengthTimer)
                    {
                        EndRound();
                        return;
                    }
                }
                else if (Status == SAStatus.RoundTwo)
                {
                    if (TotalTime >= EndRoundTimer)
                    {
                        CastSpellOnTeam((uint)SpellIds.EndOfRound, Team.Alliance);
                        CastSpellOnTeam((uint)SpellIds.EndOfRound, Team.Horde);
                        RoundScores[1].time = MiscConst.RoundlengthTimer;
                        RoundScores[1].winner = (uint)((Attackers == BattleGroundTeamId.Alliance) ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance);
                        if (RoundScores[0].time == RoundScores[1].time)
                            EndBattleground(Team.Other);
                        else if (RoundScores[0].time < RoundScores[1].time)
                            EndBattleground(RoundScores[0].winner == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                        else
                            EndBattleground(RoundScores[1].winner == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                        return;
                    }
                }
            }
        }

        public override void AddPlayer(Player player, BattlegroundQueueTypeId queueId)
        {
            bool isInBattleground = IsPlayerInBattleground(player.GetGUID());
            base.AddPlayer(player, queueId);

            SendTransportInit(player);

            if (!isInBattleground)
                TeleportToEntrancePosition(player);
        }

        public override void RemovePlayer(Player player, ObjectGuid guid, Team team) { }

        void TeleportPlayers()
        {
            foreach (var (playerGuid, _) in GetPlayers())
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

        void TeleportToEntrancePosition(Player player)
        {
            if (GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID())) == Attackers)
            {
                ObjectGuid boatGUID = _boatGUIDs[Attackers][RandomHelper.URand(0, 1)];
                GameObject boat = GetBgMap().GetGameObject(boatGUID);
                if (boat != null)
                {
                    ITransport transport = boat.ToTransportBase();
                    if (transport != null)
                    {
                        player.Relocate(MiscConst.spawnPositionOnTransport[Attackers]);
                        transport.AddPassenger(player);
                        player.m_movementInfo.transport.pos.Relocate(MiscConst.spawnPositionOnTransport[Attackers]);
                        MiscConst.spawnPositionOnTransport[Attackers].GetPosition(out float x, out float y, out float z, out float o);
                        transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                        player.Relocate(x, y, z, o);

                        if (player.IsInWorld)
                            player.NearTeleportTo(x, y, z, o);
                    }
                }
            }
            else
            {
                WorldSafeLocsEntry defenderSpawn = Global.ObjectMgr.GetWorldSafeLoc(MiscConst.SpawnDefenders);
                if (defenderSpawn != null)
                {
                    if (player.IsInWorld)
                        player.TeleportTo(defenderSpawn.Loc);
                    else
                        player.WorldRelocate(defenderSpawn.Loc);
                }
            }
        }

        public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker = null)
        {
            switch (eventId)
            {
                case MiscConst.EventAllianceAssaultStarted:
                    foreach (ObjectGuid bombGuid in _staticBombGUIDs[BattleGroundTeamId.Alliance])
                    {
                        GameObject bomb = GetBgMap().GetGameObject(bombGuid);
                        if (bomb != null)
                            bomb.RemoveFlag(GameObjectFlags.NotSelectable);
                    }
                    break;
                case MiscConst.EventHordeAssaultStarted:
                    foreach (ObjectGuid bombGuid in _staticBombGUIDs[BattleGroundTeamId.Horde])
                    {
                        GameObject bomb = GetBgMap().GetGameObject(bombGuid);
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
                            if (eventId == (uint)MiscConst.EventTitanRelicActivated)
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
                                GateState gateState = Attackers == BattleGroundTeamId.Horde ? GateState.AllianceGateDamaged : GateState.HordeGateDamaged;

                                Creature creature = obj.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);
                                if (creature != null)
                                    SendChatMessage(creature, (byte)gate.DamagedText, invoker);

                                PlaySoundToAll((uint)(Attackers == BattleGroundTeamId.Alliance ? SoundIds.WallAttackedAlliance : SoundIds.WallAttackedHorde));

                                UpdateWorldState(gate.WorldState, (int)gateState);
                            }
                            // destroyed
                            else if (eventId == go.GetGoInfo().DestructibleBuilding.DestroyedEvent)
                            {
                                GateState gateState = Attackers == BattleGroundTeamId.Horde ? GateState.AllianceGateDestroyed : GateState.HordeGateDestroyed;

                                Creature creature = obj.FindNearestCreature(SharedConst.WorldTrigger, 500.0f);
                                if (creature != null)
                                    SendChatMessage(creature, (byte)gate.DestroyedText, invoker);

                                PlaySoundToAll((uint)(Attackers == BattleGroundTeamId.Alliance ? SoundIds.WallDestroyedAlliance : SoundIds.WallDestroyedHorde));

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
                                            UpdatePvpStat(player, MiscConst.PvPStatGatesDestroyed, 1);
                                            if (rewardHonor)
                                                UpdatePlayerScore(player, ScoreType.BonusHonor, GetBonusHonorFromKill(1));
                                        }
                                    }
                                }

                                if (rewardHonor)
                                    MakeObjectsInteractable(gate.LineOfDefense);

                                UpdateWorldState(gate.WorldState, (int)gateState);
                                GetBgMap().UpdateSpawnGroupConditions();
                            }
                        }
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        public override void HandleKillUnit(Creature creature, Unit killer)
        {
            if (creature.GetEntry() == (uint)CreatureIds.Demolisher)
            {
                Player killerPlayer = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (killerPlayer != null)
                    UpdatePvpStat(killerPlayer, MiscConst.PvpStatDemolishersDestroyed, 1);

                int worldStateId = (int)(Attackers == BattleGroundTeamId.Horde ? WorldStateIds.DestroyedHordeVehicles : WorldStateIds.DestroyedAllianceVehicles);
                int currentDestroyedVehicles = Global.WorldStateMgr.GetValue(worldStateId, GetBgMap());
                UpdateWorldState(worldStateId, currentDestroyedVehicles + 1);
            }
        }

        public override void DestroyGate(Player player, GameObject go) { }

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

            int clickerTeamId = GetTeamIndexByTeamId(GetPlayerTeam(clicker.GetGUID()));
            if (clickerTeamId == Attackers)
            {
                if (clickerTeamId == BattleGroundTeamId.Alliance)
                    SendBroadcastText((uint)BroadcastTextIds.AllianceCapturedTitanPortal, ChatMsg.BgSystemNeutral);
                else
                    SendBroadcastText((uint)BroadcastTextIds.HordeCapturedTitanPortal, ChatMsg.BgSystemNeutral);

                if (Status == SAStatus.RoundOne)
                {
                    EndRound();
                    // Achievement Storm the Beach (1310)
                    foreach (var (playerGuid, _) in GetPlayers())
                    {
                        Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                        if (player != null)
                            if (GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID())) == Attackers)
                                player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                    }

                    Creature creature = FindKanrethad();
                    if (creature != null)
                        SendChatMessage(creature, (byte)TextIds.Round1Finished);
                }
                else if (Status == SAStatus.RoundTwo)
                {
                    RoundScores[1].winner = (uint)Attackers;
                    RoundScores[1].time = TotalTime;
                    ToggleTimer();
                    // Achievement Storm the Beach (1310)
                    foreach (var (playerGuid, _) in GetPlayers())
                    {
                        Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                        if (player != null)
                            if (GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID())) == Attackers && RoundScores[1].winner == Attackers)
                                player.UpdateCriteria(CriteriaType.BeSpellTarget, 65246);
                    }

                    if (RoundScores[0].time == RoundScores[1].time)
                        EndBattleground(Team.Other);
                    else if (RoundScores[0].time < RoundScores[1].time)
                        EndBattleground(RoundScores[0].winner == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                    else
                        EndBattleground(RoundScores[1].winner == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                }
            }
        }

        void ToggleTimer()
        {
            TimerEnabled = !TimerEnabled;
            UpdateWorldState((int)WorldStateIds.EnableTimer, TimerEnabled);
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

        void SendTransportInit(Player player)
        {
            UpdateData transData = new(player.GetMapId());
            foreach (ObjectGuid boatGuid in _boatGUIDs[Attackers])
            {
                GameObject boat = GetBgMap().GetGameObject(boatGuid);
                if (boat != null)
                    boat.BuildCreateUpdateBlockForPlayer(transData, player);
            }

            transData.BuildPacket(out var packet);
            player.SendPacket(packet);
        }

        void SendTransportsRemove(Player player)
        {
            UpdateData transData = new(player.GetMapId());
            foreach (ObjectGuid boatGuid in _boatGUIDs[Attackers])
            {
                GameObject boat = GetBgMap().GetGameObject(boatGuid);
                if (boat != null)
                    boat.BuildOutOfRangeUpdateBlock(transData);
            }

            transData.BuildPacket(out var packet);
            player.SendPacket(packet);
        }

        bool IsGateDestroyed(GateInfo gateInfo)
        {
            if (gateInfo == null)
                return false;

            int value = GetBgMap().GetWorldStateValue(gateInfo.WorldState);
            return value == (int)GateState.AllianceGateDestroyed || value == (int)GateState.HordeGateDestroyed;
        }

        void HandleCaptureGraveyardAction(GameObject graveyardBanner, Player player)
        {
            if (graveyardBanner == null || player == null)
                return;

            int teamId = GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID()));
            // Only attackers can capture graveyard by gameobject action
            if (teamId != Attackers)
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
                GameObject gameObject = GetBgMap().GetGameObject(guid);
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
                    GameObject door = GetBgMap().GetGameObject(_collisionDoorGUID);
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
            return GetBgMap().GetCreature(_kanrethadGUID);
        }

        void EndRound()
        {
            RoundScores[0].winner = (uint)Attackers;
            RoundScores[0].time = Math.Min(TotalTime, MiscConst.RoundlengthTimer);

            Attackers = (Attackers == BattleGroundTeamId.Alliance) ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance;
            Status = SAStatus.SecondWarmup;
            TotalTime = 0;
            ToggleTimer();

            UpdateWaitTimer = 5000;
            SignaledRoundTwo = false;
            SignaledRoundTwoHalfMin = false;
            InitSecondRound = true;
            ResetObjs();
            GetBgMap().UpdateAreaDependentAuras();

            CastSpellOnTeam((uint)SpellIds.EndOfRound, Team.Alliance);
            CastSpellOnTeam((uint)SpellIds.EndOfRound, Team.Horde);

            RemoveAuraOnTeam((uint)SpellIds.CarryingSeaforiumCharge, Team.Horde);
            RemoveAuraOnTeam((uint)SpellIds.CarryingSeaforiumCharge, Team.Alliance);
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
                    if (Status != SAStatus.SecondWarmup && Status != SAStatus.Warmup)
                        gameobject.RemoveFlag(GameObjectFlags.NotSelectable);
                    break;
                case GameObjectIds.SeaforiumBombH:
                    _staticBombGUIDs[BattleGroundTeamId.Horde].Add(gameobject.GetGUID());
                    if (Status != SAStatus.SecondWarmup && Status != SAStatus.Warmup)
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

        public override void DoAction(uint actionId, WorldObject source = null, WorldObject target = null)
        {
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
                    creature.SetFaction(MiscConst.Factions[Attackers]);
                    break;
                case CreatureIds.AntipersonnelCannon:
                    creature.SetFaction(MiscConst.Factions[Attackers == BattleGroundTeamId.Horde ? BattleGroundTeamId.Alliance : BattleGroundTeamId.Horde]);
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

        public override void OnMapSet(BattlegroundMap map)
        {
            base.OnMapSet(map);
            ResetObjs();
        }

        public override uint GetData(uint dataId)
        {
            switch (dataId)
            {
                case MiscConst.DataAttackers:
                    return (uint)Attackers;
                case MiscConst.DataStatus:
                    return (uint)Status;
                default:
                    return base.GetData(dataId);
            }
        }

        // Return GateInfo, relative to bg data, according to gameobject entry
        GateInfo GetGate(uint entry)
        {
            foreach (GateInfo gate in MiscConst.Gates)
                if (gate.GameObjectId == entry)
                    return gate;

            return null;
        }
    }

    struct RoundScore
    {
        public uint winner;
        public uint time;
    }

    class GateInfo
    {
        public GateInfo(GameObjectIds gameObjectId, WorldStateIds worldState, TextIds damagedText, TextIds destroyedText, DefenseLine defenseLine)
        {
            GameObjectId = (uint)gameObjectId;
            WorldState = (int)worldState;
            DamagedText = (uint)damagedText;
            DestroyedText = (uint)destroyedText;
            LineOfDefense = defenseLine;
        }

        public uint GameObjectId;
        public int WorldState;
        public uint DamagedText;
        public uint DestroyedText;
        public DefenseLine LineOfDefense;
    }

    #region Consts
    struct MiscConst
    {
        public static uint[] Factions = { 1732, 1735, };

        public static GateInfo[] Gates =
        {
            new GateInfo(GameObjectIds.GateOfTheGreenEmerald,   WorldStateIds.GreenGate,   TextIds.GreenGateUnderAttack,   TextIds.GreenGateDestroyed,      DefenseLine.First),
            new GateInfo(GameObjectIds.GateOfTheYellowMoon,     WorldStateIds.YellowGate,  TextIds.YellowGateUnderAttack,  TextIds.YellowGateDestroyed,     DefenseLine.Third),
            new GateInfo(GameObjectIds.GateOfTheBlueSapphire,   WorldStateIds.BlueGate,    TextIds.BlueGateUnderAttack,    TextIds.BlueGateDestroyed,       DefenseLine.First),
            new GateInfo(GameObjectIds.GateOfTheRedSun,         WorldStateIds.RedGate,     TextIds.RedGateUnderAttack,     TextIds.RedGateDestroyed,        DefenseLine.Second),
            new GateInfo(GameObjectIds.GateOfThePurpleAmethyst, WorldStateIds.PurpleGate,  TextIds.PurpleGateUnderAttack,  TextIds.PurpleGateDestroyed,     DefenseLine.Second),
            new GateInfo(GameObjectIds.ChamberOfAncientRelics,  WorldStateIds.AncientGate, TextIds.AncientGateUnderAttack, TextIds.AncientGateDestroyed,    DefenseLine.Last)
        };

        public const uint PvPStatGatesDestroyed = 231;
        public const uint PvpStatDemolishersDestroyed = 232;

        public const uint EventHordeAssaultStarted = 21702;
        public const uint EventAllianceAssaultStarted = 23748;
        public const uint EventTitanRelicActivated = 20572;

        public const uint SpawnDefenders = 1399;

        public const uint DataAttackers = 1;
        public const uint DataStatus = 2;

        public const uint BoatStartTimer = 60 * Time.InMilliseconds;
        public const uint WarmuplengthTimer = 120 * Time.InMilliseconds;
        public const uint RoundlengthTimer = 600 * Time.InMilliseconds;

        public static Position[] spawnPositionOnTransport =
        {
            new Position(0.0f, 5.0f, 9.6f, 3.14f),
            new Position(-6.0f, -3.0f, 8.6f, 0.0f)
        };
    }

    enum SAStatus
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

    enum CreatureIds
    {
        Kanrethad = 29,
        Demolisher = 28781,
        AntipersonnelCannon = 27894,
        RiggerSparklight = 29260,
        GorgrilRigspark = 29262,
        WorldTrigger = 22515,
        DemolisherSa = 28781
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
    #endregion
}
