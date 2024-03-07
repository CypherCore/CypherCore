// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;

namespace Game.BattleGrounds.Zones.IsleOfConquest
{
    class BgIsleofConquest : Battleground
    {
        ushort[] _factionReinforcements = new ushort[SharedConst.PvpTeamsCount];
        GateState[] _gateStatus = new GateState[6];
        ICNodePoint[] _nodePoints = new ICNodePoint[7];
        ObjectGuid[] _gunshipGUIDs = new ObjectGuid[SharedConst.PvpTeamsCount];
        List<ObjectGuid> _teleporterGUIDs;
        List<ObjectGuid> _teleporterEffectGUIDs;
        List<ObjectGuid> _mainGateDoorGUIDs;
        List<ObjectGuid> _portcullisGUIDs;
        List<ObjectGuid> _wallGUIDs;
        List<ObjectGuid>[] _cannonGUIDs = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
        List<ObjectGuid>[] _keepGateGUIDs = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
        ObjectGuid[] _keepBannerGUIDs = new ObjectGuid[SharedConst.PvpTeamsCount];
        ObjectGuid _gunshipTeleportTarget;

        TaskScheduler _scheduler;
        TimeTracker _resourceTimer;

        public BgIsleofConquest(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
        {
            _factionReinforcements = [MiscConst.MaxReinforcements, MiscConst.MaxReinforcements];

            _gateStatus = [GateState.Ok, GateState.Ok, GateState.Ok, GateState.Ok, GateState.Ok, GateState.Ok];

            for (var i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                _cannonGUIDs[i] = new();
                _keepGateGUIDs[i] = new();
            }

            _nodePoints[(int)NodePointType.Refinery] = new ICNodePoint(NodeState.Neutral, MiscConst.NodePointInitial[(int)NodePointType.Refinery]);
            _nodePoints[(int)NodePointType.Quarry] = new ICNodePoint(NodeState.Neutral, MiscConst.NodePointInitial[(int)NodePointType.Quarry]);
            _nodePoints[(int)NodePointType.Docks] = new ICNodePoint(NodeState.Neutral, MiscConst.NodePointInitial[(int)NodePointType.Docks]);
            _nodePoints[(int)NodePointType.Hangar] = new ICNodePoint(NodeState.Neutral, MiscConst.NodePointInitial[(int)NodePointType.Hangar]);
            _nodePoints[(int)NodePointType.Workshop] = new ICNodePoint(NodeState.Neutral, MiscConst.NodePointInitial[(int)NodePointType.Workshop]);
            _nodePoints[(int)NodePointType.GraveyardA] = new ICNodePoint(NodeState.ControlledA, MiscConst.NodePointInitial[(int)NodePointType.GraveyardA]);
            _nodePoints[(int)NodePointType.GraveyardH] = new ICNodePoint(NodeState.ControlledH, MiscConst.NodePointInitial[(int)NodePointType.GraveyardH]);

            _scheduler = new();
            _resourceTimer = new(MiscConst.ResourceTimer);
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            _scheduler.Update(diff);
            _resourceTimer.Update(diff);
            if (_resourceTimer.Passed())
            {
                for (byte i = 0; i < (int)NodePointType.Docks; ++i)
                {
                    if (_nodePoints[i].GetLastControlledTeam() != BattleGroundTeamId.Neutral && !_nodePoints[i].IsContested())
                    {
                        _factionReinforcements[_nodePoints[i].GetLastControlledTeam()] += 1;
                        RewardHonorToTeam(MiscConst.ResourceHonorAmount, _nodePoints[i].GetLastControlledTeam() == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                        UpdateWorldState((int)(_nodePoints[i].GetLastControlledTeam() == BattleGroundTeamId.Alliance ? WorldStateIds.AllianceReinforcements : WorldStateIds.HordeReinforcements), _factionReinforcements[_nodePoints[i].GetLastControlledTeam()]);
                    }
                }

                _resourceTimer.Reset(MiscConst.ResourceTimer);
            }
        }

        public override void StartingEventOpenDoors()
        {
            var gameobjectAction = (List<ObjectGuid> guids, Action<GameObject> action) =>
            {
                foreach (ObjectGuid guid in guids)
                {
                    GameObject gameObject = GetBgMap().GetGameObject(guid);
                    if (gameObject != null)
                        action(gameObject);
                }
            };

            gameobjectAction(_mainGateDoorGUIDs, gameobject =>
            {
                gameobject.UseDoorOrButton();
                gameobject.DespawnOrUnsummon(TimeSpan.FromSeconds(20));
            });

            gameobjectAction(_portcullisGUIDs, gameobject =>
            {
                gameobject.UseDoorOrButton();
            });

            gameobjectAction(_teleporterGUIDs, gameobject =>
            {
                gameobject.RemoveFlag(GameObjectFlags.NotSelectable);
            });

            gameobjectAction(_teleporterEffectGUIDs, gameobject =>
            {
                gameobject.SetGoState(GameObjectState.Active);
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(20), _ =>
            {
                foreach (ObjectGuid guid in _wallGUIDs)
                {
                    GameObject gameobject = GetBgMap().GetGameObject(guid);
                    if (gameobject != null)
                        gameobject.SetDestructibleState(GameObjectDestructibleState.Damaged);
                }
            });
        }

        public override void HandleKillUnit(Creature unit, Unit killer)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            uint entry = unit.GetEntry();
            if (entry == (uint)CreatureIds.HighCommanderHalfordWyrmbane)
            {
                RewardHonorToTeam(MiscConst.WinnerHonorAmount, Team.Horde);
                EndBattleground(Team.Horde);
            }
            else if (entry == (uint)CreatureIds.OverlordAgmar)
            {
                RewardHonorToTeam(MiscConst.WinnerHonorAmount, Team.Alliance);
                EndBattleground(Team.Alliance);
            }

            //Achievement Mowed Down
            // TO-DO: This should be done on the script of each vehicle of the BG.
            if (unit.IsVehicle())
            {
                Player killerPlayer = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (killerPlayer != null)
                    killerPlayer.CastSpell(killerPlayer, (uint)SpellIds.DestroyedVehicleAchievement, true);
            }
        }

        public override void HandleKillPlayer(Player player, Player killer)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            base.HandleKillPlayer(player, killer);

            int victimTeamId = GetTeamIndexByTeamId(GetPlayerTeam(player.GetGUID()));
            _factionReinforcements[victimTeamId] -= 1;

            UpdateWorldState((int)(GetPlayerTeam(player.GetGUID()) == Team.Alliance ? WorldStateIds.AllianceReinforcements : WorldStateIds.HordeReinforcements), _factionReinforcements[victimTeamId]);

            // we must end the battleground
            if (_factionReinforcements[victimTeamId] < 1)
                EndBattleground(GetPlayerTeam(killer.GetGUID()));
        }

        DoorList GetGateIDFromEntry(uint id)
        {
            switch ((GameObjectIds)id)
            {
                case GameObjectIds.HordeGate1:
                    return DoorList.HFront;
                case GameObjectIds.HordeGate2:
                    return DoorList.HWest;
                case GameObjectIds.HordeGate3:
                    return DoorList.HEast;
                case GameObjectIds.AllianceGate3:
                    return DoorList.AFront;
                case GameObjectIds.AllianceGate1:
                    return DoorList.AWest;
                case GameObjectIds.AllianceGate2:
                    return DoorList.AEast;
                default:
                    return 0;
            }
        }

        int GetWorldStateFromGateEntry(uint id, bool open)
        {
            int uws = 0;

            switch ((GameObjectIds)id)
            {
                case GameObjectIds.HordeGate1:
                    uws = (int)(open ? WorldStateIds.GateFrontHWsOpen : WorldStateIds.GateFrontHWsClosed);
                    break;
                case GameObjectIds.HordeGate2:
                    uws = (int)(open ? WorldStateIds.GateWestHWsOpen : WorldStateIds.GateWestHWsClosed);
                    break;
                case GameObjectIds.HordeGate3:
                    uws = (int)(open ? WorldStateIds.GateEastHWsOpen : WorldStateIds.GateEastHWsClosed);
                    break;
                case GameObjectIds.AllianceGate3:
                    uws = (int)(open ? WorldStateIds.GateFrontAWsOpen : WorldStateIds.GateFrontAWsClosed);
                    break;
                case GameObjectIds.AllianceGate1:
                    uws = (int)(open ? WorldStateIds.GateWestAWsOpen : WorldStateIds.GateWestAWsClosed);
                    break;
                case GameObjectIds.AllianceGate2:
                    uws = (int)(open ? WorldStateIds.GateEastAWsOpen : WorldStateIds.GateEastAWsClosed);
                    break;
                default:
                    break;
            }
            return uws;
        }

        void UpdateNodeWorldState(ICNodePoint node)
        {
            UpdateWorldState(node.GetNodeInfo().WorldStateConflictA, node.GetState() == NodeState.ConflictA);
            UpdateWorldState(node.GetNodeInfo().WorldStateConflictH, node.GetState() == NodeState.ConflictH);
            UpdateWorldState(node.GetNodeInfo().WorldStateControlledA, node.GetState() == NodeState.ControlledA);
            UpdateWorldState(node.GetNodeInfo().WorldStateControlledH, node.GetState() == NodeState.ControlledH);
            UpdateWorldState(node.GetNodeInfo().WorldStateUncontrolled, node.GetState() == NodeState.Neutral);
        }

        NodePointType BannerToNodeType(uint bannerId)
        {
            switch ((GameObjectIds)bannerId)
            {
                case GameObjectIds.BannerAllianceKeepContestedA:
                case GameObjectIds.BannerAllianceKeepContestedH:
                case GameObjectIds.BannerAllianceKeepControlledA:
                case GameObjectIds.BannerAllianceKeepControlledH:
                    return NodePointType.GraveyardA;
                case GameObjectIds.BannerHordeKeepContestedA:
                case GameObjectIds.BannerHordeKeepContestedH:
                case GameObjectIds.BannerHordeKeepControlledA:
                case GameObjectIds.BannerHordeKeepControlledH:
                    return NodePointType.GraveyardH;
                case GameObjectIds.BannerDocksContestedA:
                case GameObjectIds.BannerDocksContestedH:
                case GameObjectIds.BannerDocksControlledA:
                case GameObjectIds.BannerDocksControlledH:
                case GameObjectIds.BannerDocksControlledN:
                    return NodePointType.Docks;
                case GameObjectIds.BannerHangarContestedA:
                case GameObjectIds.BannerHangarContestedH:
                case GameObjectIds.BannerHangarControlledA:
                case GameObjectIds.BannerHangarControlledH:
                case GameObjectIds.BannerHangarControlledN:
                    return NodePointType.Hangar;
                case GameObjectIds.BannerWorkshopContestedA:
                case GameObjectIds.BannerWorkshopContestedH:
                case GameObjectIds.BannerWorkshopControlledA:
                case GameObjectIds.BannerWorkshopControlledH:
                case GameObjectIds.BannerWorkshopControlledN:
                    return NodePointType.Workshop;
                case GameObjectIds.BannerQuarryContestedA:
                case GameObjectIds.BannerQuarryContestedH:
                case GameObjectIds.BannerQuarryControlledA:
                case GameObjectIds.BannerQuarryControlledH:
                case GameObjectIds.BannerQuarryControlledN:
                    return NodePointType.Quarry;
                case GameObjectIds.BannerRefineryContestedA:
                case GameObjectIds.BannerRefineryContestedH:
                case GameObjectIds.BannerRefineryControlledA:
                case GameObjectIds.BannerRefineryControlledH:
                case GameObjectIds.BannerRefineryControlledN:
                default:
                    break;
            }

            return NodePointType.Max;
        }

        void HandleCapturedNodes(ICNodePoint node)
        {
            if (node.GetLastControlledTeam() == BattleGroundTeamId.Neutral)
                return;

            switch (node.GetNodeInfo().NodeType)
            {
                case NodePointType.Quarry:
                case NodePointType.Refinery:
                    GetBgMap().UpdateAreaDependentAuras();
                    break;
                case NodePointType.Hangar:
                    Transport transport = GetBgMap().GetTransport(_gunshipGUIDs[node.GetLastControlledTeam()]);
                    if (transport != null)
                    {
                        // Can't have this in spawngroup, creature is on a transport
                        TempSummon trigger = transport.SummonPassenger((uint)CreatureIds.WorldTriggerNotFloating, MiscConst.GunshipTeleportTriggerPosition[node.GetLastControlledTeam()], TempSummonType.ManualDespawn);
                        if (trigger != null)
                            _gunshipTeleportTarget = trigger.GetGUID();

                        transport.EnableMovement(true);
                    }

                    foreach (ObjectGuid guid in _cannonGUIDs[node.GetLastControlledTeam()])
                    {
                        Creature cannon = GetBgMap().GetCreature(guid);
                        if (cannon != null)
                            cannon.SetUninteractible(false);
                    }
                    break;
                default:
                    break;
            }
        }

        public override WorldSafeLocsEntry GetExploitTeleportLocation(Team team)
        {
            return Global.ObjectMgr.GetWorldSafeLoc(team == Team.Alliance ? MiscConst.ExploitTeleportLocationAlliance : MiscConst.ExploitTeleportLocationHorde);
        }

        public override void OnCreatureCreate(Creature creature)
        {
            base.OnCreatureCreate(creature);
            if (creature.HasStringId("bg_ioc_faction_1735"))
                creature.SetFaction(FactionTemplates.HordeGenericWg);
            else if (creature.HasStringId("bg_ioc_faction_1732"))
                creature.SetFaction(FactionTemplates.AllianceGenericWg);

            switch ((CreatureIds)creature.GetEntry())
            {
                case CreatureIds.AllianceGunshipCannon:
                    _cannonGUIDs[BattleGroundTeamId.Alliance].Add(creature.GetGUID());
                    creature.SetUninteractible(true);
                    creature.SetControlled(true, UnitState.Root);
                    break;
                case CreatureIds.HordeGunshipCannon:
                    _cannonGUIDs[BattleGroundTeamId.Horde].Add(creature.GetGUID());
                    creature.SetUninteractible(true);
                    creature.SetControlled(true, UnitState.Root);
                    break;
                default:
                    break;
            }
        }

        public override void OnGameObjectCreate(GameObject gameobject)
        {
            base.OnGameObjectCreate(gameobject);

            if (gameobject.IsDestructibleBuilding())
                _wallGUIDs.Add(gameobject.GetGUID());

            if (gameobject.HasStringId("bg_ioc_faction_1735"))
                gameobject.SetFaction(FactionTemplates.HordeGenericWg);
            else if (gameobject.HasStringId("bg_ioc_faction_1732"))
                gameobject.SetFaction(FactionTemplates.AllianceGenericWg);

            switch ((GameObjectIds)gameobject.GetEntry())
            {
                case GameObjectIds.Teleporter1:
                case GameObjectIds.Teleporter2:
                case GameObjectIds.Teleporter3:
                case GameObjectIds.Teleporter4:
                    _teleporterGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.TeleporterEffectsA:
                case GameObjectIds.TeleporterEffectsH:
                    _teleporterEffectGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.DoodadNdHumanGateClosedfxDoor01:
                case GameObjectIds.DoodadNdWinterorcWallGatefxDoor01:
                    _mainGateDoorGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.DoodadHuPortcullis01:
                case GameObjectIds.DoodadVrPortcullis01:
                    _portcullisGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.KeepGateH:
                    _keepGateGUIDs[BattleGroundTeamId.Horde].Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.KeepGateA:
                case GameObjectIds.KeepGate2A:
                    _keepGateGUIDs[BattleGroundTeamId.Alliance].Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.BannerAllianceKeepControlledA:
                    _keepBannerGUIDs[BattleGroundTeamId.Alliance] = gameobject.GetGUID();
                    break;
                case GameObjectIds.BannerHordeKeepControlledH:
                    _keepBannerGUIDs[BattleGroundTeamId.Horde] = gameobject.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override void OnMapSet(BattlegroundMap map)
        {
            base.OnMapSet(map);

            Transport transport = Global.TransportMgr.CreateTransport((uint)GameObjectIds.HordeGunship, map);
            if (transport != null)
            {
                _gunshipGUIDs[BattleGroundTeamId.Horde] = transport.GetGUID();
                transport.EnableMovement(false);
            }

            transport = Global.TransportMgr.CreateTransport((uint)GameObjectIds.AllianceGunship, map);
            if (transport != null)
            {
                _gunshipGUIDs[BattleGroundTeamId.Alliance] = transport.GetGUID();
                transport.EnableMovement(false);
            }
        }

        public override void DoAction(uint actionId, WorldObject source, WorldObject target)
        {

            switch (actionId)
            {
                case MiscConst.ActionInteractCapturableObject:
                    OnPlayerInteractWithBanner(source?.ToPlayer(), target?.ToGameObject());
                    break;
                case MiscConst.ActionCaptureCapturableObject:
                    HandleCaptureNodeAction(target?.ToGameObject());
                    break;
                default:
                    break;
            }
        }

        void OnPlayerInteractWithBanner(Player player, GameObject banner)
        {
            if (player == null || banner == null)
                return;

            Team playerTeam = GetPlayerTeam(player.GetGUID());
            int playerTeamId = GetTeamIndexByTeamId(playerTeam);
            NodePointType nodeType = BannerToNodeType(banner.GetEntry());
            if (nodeType == NodePointType.Max)
                return;

            ICNodePoint node = _nodePoints[(int)nodeType];

            bool assault = false;
            bool defend = false;

            switch (node.GetState())
            {
                case NodeState.Neutral:
                    assault = true;
                    break;
                case NodeState.ControlledH:
                    assault = playerTeamId != BattleGroundTeamId.Horde;
                    break;
                case NodeState.ControlledA:
                    assault = playerTeamId != BattleGroundTeamId.Alliance;
                    break;
                case NodeState.ConflictA:
                    defend = playerTeamId == node.GetLastControlledTeam();
                    assault = !defend && playerTeamId == BattleGroundTeamId.Horde;
                    break;
                case NodeState.ConflictH:
                    defend = playerTeamId == node.GetLastControlledTeam();
                    assault = !defend && playerTeamId == BattleGroundTeamId.Alliance;
                    break;
            }

            if (assault)
                OnPlayerAssaultNode(player, node);
            else if (defend)
                OnPlayerDefendNode(player, node);

            GetBgMap().UpdateSpawnGroupConditions();
        }

        void OnPlayerAssaultNode(Player player, ICNodePoint node)
        {
            if (player == null)
                return;

            Team playerTeam = GetPlayerTeam(player.GetGUID());
            int playerTeamId = GetTeamIndexByTeamId(playerTeam);

            NodeState newState = playerTeamId == BattleGroundTeamId.Horde ? NodeState.ConflictH : NodeState.ConflictA;
            node.UpdateState(newState);

            UpdatePvpStat(player, MiscConst.PvpStatBasesAssaulted, 1);

            ChatMsg messageType = playerTeamId == BattleGroundTeamId.Alliance ? ChatMsg.BgSystemAlliance : ChatMsg.BgSystemHorde;
            SendBroadcastText(node.GetNodeInfo().AssaultedTextId, messageType, player);
            UpdateNodeWorldState(node);

            // apply side effects of each node, only if it wasn't neutral before
            if (node.GetLastControlledTeam() == BattleGroundTeamId.Neutral)
                return;

            switch (node.GetNodeInfo().NodeType)
            {
                case NodePointType.Hangar:
                    Transport transport = GetBgMap().GetTransport(_gunshipGUIDs[node.GetLastControlledTeam()]);
                    if (transport != null)
                        transport.EnableMovement(false);

                    foreach (ObjectGuid guid in _cannonGUIDs[node.GetLastControlledTeam()])
                    {
                        Creature cannon = GetBgMap().GetCreature(guid);
                        if (cannon != null)
                        {
                            cannon.GetVehicleKit().RemoveAllPassengers();
                            cannon.SetUninteractible(true);
                        }
                    }

                    // Despawn teleport trigger target
                    Creature creature = FindBgMap().GetCreature(_gunshipTeleportTarget);
                    if (creature != null)
                        creature.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }

        void OnPlayerDefendNode(Player player, ICNodePoint node)
        {
            if (player == null)
                return;

            Team playerTeam = GetPlayerTeam(player.GetGUID());
            int playerTeamId = GetTeamIndexByTeamId(playerTeam);

            node.UpdateState(playerTeamId == BattleGroundTeamId.Horde ? NodeState.ControlledH : NodeState.ControlledA);
            HandleCapturedNodes(node);
            UpdatePvpStat(player, MiscConst.PvpStatBasesDefended, 1);

            ChatMsg messageType = playerTeamId == BattleGroundTeamId.Alliance ? ChatMsg.BgSystemAlliance : ChatMsg.BgSystemHorde;
            SendBroadcastText(node.GetNodeInfo().DefendedTextId, messageType, player);
            UpdateNodeWorldState(node);
        }

        public override void ProcessEvent(WorldObject target, uint eventId, WorldObject invoker)
        {
            base.ProcessEvent(target, eventId, invoker);

            GameObject obj = target?.ToGameObject();
            if (obj != null && obj.GetGoType() == GameObjectTypes.DestructibleBuilding)
                if (obj.GetGoInfo().DestructibleBuilding.DestroyedEvent == eventId)
                    OnGateDestroyed(obj, invoker);
        }

        void HandleCaptureNodeAction(GameObject banner)
        {
            if (banner == null)
                return;

            NodePointType nodeType = BannerToNodeType(banner.GetEntry());
            if (nodeType == NodePointType.Max)
                return;

            ICNodePoint node = _nodePoints[(int)nodeType];
            if (node.GetState() == NodeState.ConflictH)
                node.UpdateState(NodeState.ControlledH);
            else if (node.GetState() == NodeState.ConflictA)
                node.UpdateState(NodeState.ControlledA);

            HandleCapturedNodes(node);

            ChatMsg messageType = node.GetLastControlledTeam() == BattleGroundTeamId.Alliance ? ChatMsg.BgSystemAlliance : ChatMsg.BgSystemHorde;
            uint textId = node.GetLastControlledTeam() == BattleGroundTeamId.Alliance ? node.GetNodeInfo().AllianceTakenTextId : node.GetNodeInfo().HordeTakenTextId;
            SendBroadcastText(textId, messageType);
            UpdateNodeWorldState(node);
        }

        void OnGateDestroyed(GameObject gate, WorldObject destroyer)
        {
            _gateStatus[(int)GetGateIDFromEntry(gate.GetEntry())] = GateState.Destroyed;
            int wsGateOpen = GetWorldStateFromGateEntry(gate.GetEntry(), true);
            int wsGateClosed = GetWorldStateFromGateEntry(gate.GetEntry(), false);
            if (wsGateOpen != 0)
            {
                UpdateWorldState(wsGateClosed, 0);
                UpdateWorldState(wsGateOpen, 1);
            }

            int teamId;
            BroadcastTextIds textId;
            ChatMsg msgType;
            switch ((GameObjectIds)gate.GetEntry())
            {
                case GameObjectIds.HordeGate1:
                    textId = BroadcastTextIds.FrontGateHordeDestroyed;
                    msgType = ChatMsg.BgSystemAlliance;
                    teamId = BattleGroundTeamId.Horde;
                    break;
                case GameObjectIds.HordeGate2:
                    textId = BroadcastTextIds.WestGateHordeDestroyed;
                    msgType = ChatMsg.BgSystemAlliance;
                    teamId = BattleGroundTeamId.Horde;
                    break;
                case GameObjectIds.HordeGate3:
                    textId = BroadcastTextIds.EastGateHordeDestroyed;
                    msgType = ChatMsg.BgSystemAlliance;
                    teamId = BattleGroundTeamId.Horde;
                    break;
                case GameObjectIds.AllianceGate1:
                    textId = BroadcastTextIds.WestGateAllianceDestroyed;
                    msgType = ChatMsg.BgSystemHorde;
                    teamId = BattleGroundTeamId.Alliance;
                    break;
                case GameObjectIds.AllianceGate2:
                    textId = BroadcastTextIds.EastGateAllianceDestroyed;
                    msgType = ChatMsg.BgSystemHorde;
                    teamId = BattleGroundTeamId.Alliance;
                    break;
                case GameObjectIds.AllianceGate3:
                    textId = BroadcastTextIds.FrontGateAllianceDestroyed;
                    msgType = ChatMsg.BgSystemHorde;
                    teamId = BattleGroundTeamId.Alliance;
                    break;
                default:
                    return;
            }

            if (teamId != BattleGroundTeamId.Neutral)
            {
                List<ObjectGuid> keepGates = _keepGateGUIDs[teamId];
                ObjectGuid bannerGuid = _keepBannerGUIDs[teamId];

                foreach (ObjectGuid guid in keepGates)
                {
                    GameObject keepGate = GetBgMap().GetGameObject(guid);
                    if (keepGate != null)
                        keepGate.UseDoorOrButton();
                }

                GameObject banner = GetBgMap().GetGameObject(bannerGuid);
                if (banner != null)
                    banner.RemoveFlag(GameObjectFlags.NotSelectable);
            }

            SendBroadcastText((uint)textId, msgType, destroyer);
        }
    }

    struct IoCStaticNodeInfo
    {
        public NodePointType NodeType;
        public uint AssaultedTextId;
        public uint DefendedTextId;
        public uint AllianceTakenTextId;
        public uint HordeTakenTextId;

        public int WorldStateUncontrolled;
        public int WorldStateConflictA;
        public int WorldStateConflictH;
        public int WorldStateControlledA;
        public int WorldStateControlledH;

        public IoCStaticNodeInfo(NodePointType nodeType, uint assaultedTextId, uint defendedTextId, uint allianceTakenTextId, uint hordeTakenTextId, WorldStateIds worldStateUncontrolled,
            WorldStateIds worldStateConflictA, WorldStateIds worldStateConflictH, WorldStateIds worldStateControlledA, WorldStateIds worldStateControlledH)
        {
            NodeType = nodeType;
            AssaultedTextId = assaultedTextId;
            DefendedTextId = defendedTextId;
            AllianceTakenTextId = allianceTakenTextId;
            HordeTakenTextId = hordeTakenTextId;

            WorldStateUncontrolled = (int)worldStateUncontrolled;
            WorldStateConflictA = (int)worldStateConflictA;
            WorldStateConflictH = (int)worldStateConflictH;
            WorldStateControlledA = (int)worldStateControlledA;
            WorldStateControlledH = (int)worldStateControlledH;
        }
    }

    class ICNodePoint
    {
        public NodeState _state;
        public int _lastControlled;
        public IoCStaticNodeInfo _nodeInfo;

        public ICNodePoint(NodeState state, IoCStaticNodeInfo nodeInfo)
        {
            _state = state;
            _nodeInfo = nodeInfo;

            switch (state)
            {
                case NodeState.ControlledH:
                    _lastControlled = BattleGroundTeamId.Horde;
                    break;
                case NodeState.ControlledA:
                    _lastControlled = BattleGroundTeamId.Alliance;
                    break;
                case NodeState.ConflictA:
                case NodeState.ConflictH:
                case NodeState.Neutral:
                    _lastControlled = BattleGroundTeamId.Neutral;
                    break;
            }
        }

        public NodeState GetState() { return _state; }

        public bool IsContested()
        {
            return _state == NodeState.ConflictA || _state == NodeState.ConflictH;
        }

        public int GetLastControlledTeam() { return _lastControlled; }

        public IoCStaticNodeInfo GetNodeInfo() { return _nodeInfo; }

        public void UpdateState(NodeState state)
        {
            switch (state)
            {
                case NodeState.ControlledA:
                    _lastControlled = BattleGroundTeamId.Alliance;
                    break;
                case NodeState.ControlledH:
                    _lastControlled = BattleGroundTeamId.Horde;
                    break;
                case NodeState.Neutral:
                    _lastControlled = BattleGroundTeamId.Neutral;
                    break;
                case NodeState.ConflictA:
                case NodeState.ConflictH:
                    break;
            }

            _state = state;
        }
    }

    #region Constants
    struct MiscConst
    {
        public const ushort MaxReinforcements = 400;

        public const uint ExploitTeleportLocationAlliance = 3986;
        public const uint ExploitTeleportLocationHorde = 3983;

        public const uint ResourceHonorAmount = 12;
        public const uint WinnerHonorAmount = 500;

        public const uint PvpStatBasesAssaulted = 245;
        public const uint PvpStatBasesDefended = 246;

        public static TimeSpan ResourceTimer = TimeSpan.FromSeconds(45);

        public const int ActionGunshipReady = 1;
        public const int ActionInteractCapturableObject = 2;
        public const int ActionCaptureCapturableObject = 3;

        public static uint[] Factions =
        {
            1732, // Alliance
            1735  // Horde
        };

        public static IoCStaticNodeInfo[] NodePointInitial =
        {
            new IoCStaticNodeInfo(NodePointType.Refinery, 35377, 35378, 35379, 35380, WorldStateIds.RefineryUncontrolled, WorldStateIds.RefineryConflictA, WorldStateIds.RefineryConflictH, WorldStateIds.RefineryControlledA, WorldStateIds.RefineryControlledH),
            new IoCStaticNodeInfo(NodePointType.Quarry, 35373, 35374, 35375, 35376, WorldStateIds.QuarryUncontrolled, WorldStateIds.QuarryConflictA, WorldStateIds.QuarryConflictH, WorldStateIds.QuarryControlledA, WorldStateIds.QuarryControlledH),
            new IoCStaticNodeInfo(NodePointType.Docks, 35365, 35366, 35367, 35368, WorldStateIds.DocksUncontrolled, WorldStateIds.DocksConflictA, WorldStateIds.DocksConflictH, WorldStateIds.DocksControlledA, WorldStateIds.DocksControlledH),
            new IoCStaticNodeInfo(NodePointType.Hangar, 35369, 35370, 35371, 35372, WorldStateIds.HangarUncontrolled, WorldStateIds.HangarConflictA, WorldStateIds.HangarConflictH, WorldStateIds.HangarControlledA, WorldStateIds.HangarControlledH),
            new IoCStaticNodeInfo(NodePointType.Workshop, 35278, 35286, 35279, 35280, WorldStateIds.WorkshopUncontrolled, WorldStateIds.WorkshopConflictA, WorldStateIds.WorkshopConflictH, WorldStateIds.WorkshopControlledA, WorldStateIds.WorkshopControlledH),
            new IoCStaticNodeInfo(NodePointType.GraveyardA, 35461, 35459, 35463, 35466, WorldStateIds.AllianceKeepUncontrolled, WorldStateIds.AllianceKeepConflictA, WorldStateIds.AllianceKeepConflictH, WorldStateIds.AllianceKeepControlledA, WorldStateIds.AllianceKeepControlledH),
            new IoCStaticNodeInfo(NodePointType.GraveyardH, 35462, 35460, 35464, 35465, WorldStateIds.HordeKeepUncontrolled, WorldStateIds.HordeKeepConflictA, WorldStateIds.HordeKeepConflictH, WorldStateIds.HordeKeepControlledA, WorldStateIds.HordeKeepControlledH)
        };

        public static Position[] SpiritGuidePos =
        {
            new Position(0.0f, 0.0f, 0.0f, 0.0f),                     // no grave
            new Position(0.0f, 0.0f, 0.0f, 0.0f),                     // no grave
            new Position(629.57f, -279.83f, 11.33f, 0.0f),            // dock
            new Position(780.729f, -1103.08f, 135.51f, 2.27f),        // hangar
            new Position(775.74f, -652.77f, 9.31f, 4.27f),            // workshop
            new Position(278.42f, -883.20f, 49.89f, 1.53f),           // alliance starting base
            new Position(1300.91f, -834.04f, 48.91f, 1.69f),          // horde starting base
            new Position(438.86f, -310.04f, 51.81f, 5.87f),           // last resort alliance
            new Position(1148.65f, -1250.98f, 16.60f, 1.74f),         // last resort horde
        };

        public static Position[] GunshipTeleportTriggerPosition =
        {
            new Position(11.69964981079101562f, 0.034145999699831008f, 20.62075996398925781f, 3.211405754089355468f),
            new Position(7.30560922622680664f, -0.09524600207805633f, 34.51021575927734375f, 3.159045934677124023f)
        };
    }

    enum CreatureIds
    {
        HighCommanderHalfordWyrmbane = 34924, // Alliance Boss
        OverlordAgmar = 34922, // Horde Boss
        KorKronGuard = 34918, // Horde Guard
        SevenThLegionInfantry = 34919, // Alliance Guard
        KeepCannon = 34944,
        Demolisher = 34775,
        SiegeEngineH = 35069,
        SiegeEngineA = 34776,
        GlaiveThrowerA = 34802,
        GlaiveThrowerH = 35273,
        Catapult = 34793,
        HordeGunshipCannon = 34935,
        AllianceGunshipCannon = 34929,
        HordeGunshipCaptain = 35003,
        AllianceGunshipCaptain = 34960,
        WorldTriggerNotFloating = 34984,
        WorldTriggerAllianceFriendly = 20213,
        WorldTriggerHordeFriendly = 20212
    };

    enum BannersTypes
    {
        AControlled,
        AContested,
        HControlled,
        HContested
    }

    enum SpellIds
    {
        OilRefinery = 68719,
        Quarry = 68720,
        Parachute = 66656,
        SlowFall = 12438,
        DestroyedVehicleAchievement = 68357,
        BackDoorJobAchievement = 68502,
        DrivingCreditDemolisher = 68365,
        DrivingCreditGlaive = 68363,
        DrivingCreditSiege = 68364,
        DrivingCreditCatapult = 68362,
        SimpleTeleport = 12980,
        TeleportVisualOnly = 51347,
        ParachuteIc = 66657,
        LaunchNoFallingDamage = 66251
    }

    enum WorldStateIds
    {
        AllianceReinforcementsSet = 4221,
        HordeReinforcementsSet = 4222,
        AllianceReinforcements = 4226,
        HordeReinforcements = 4227,
        MaxReinforcements = 17377,

        GateFrontHWsClosed = 4317,
        GateWestHWsClosed = 4318,
        GateEastHWsClosed = 4319,
        GateFrontAWsClosed = 4328,
        GateWestAWsClosed = 4327,
        GateEastAWsClosed = 4326,
        GateFrontHWsOpen = 4322,
        GateWestHWsOpen = 4321,
        GateEastHWsOpen = 4320,
        GateFrontAWsOpen = 4323,
        GateWestAWsOpen = 4324,
        GateEastAWsOpen = 4325,

        DocksUncontrolled = 4301,
        DocksConflictA = 4305,
        DocksConflictH = 4302,
        DocksControlledA = 4304,
        DocksControlledH = 4303,

        HangarUncontrolled = 4296,
        HangarConflictA = 4300,
        HangarConflictH = 4297,
        HangarControlledA = 4299,
        HangarControlledH = 4298,

        QuarryUncontrolled = 4306,
        QuarryConflictA = 4310,
        QuarryConflictH = 4307,
        QuarryControlledA = 4309,
        QuarryControlledH = 4308,

        RefineryUncontrolled = 4311,
        RefineryConflictA = 4315,
        RefineryConflictH = 4312,
        RefineryControlledA = 4314,
        RefineryControlledH = 4313,

        WorkshopUncontrolled = 4294,
        WorkshopConflictA = 4228,
        WorkshopConflictH = 4293,
        WorkshopControlledA = 4229,
        WorkshopControlledH = 4230,

        AllianceKeepUncontrolled = 4341,
        AllianceKeepConflictA = 4342,
        AllianceKeepConflictH = 4343,
        AllianceKeepControlledA = 4339,
        AllianceKeepControlledH = 4340,

        HordeKeepUncontrolled = 4346,
        HordeKeepConflictA = 4347,
        HordeKeepConflictH = 4348,
        HordeKeepControlledA = 4344,
        HordeKeepControlledH = 4345
    }

    enum GateState
    {
        Ok = 1,
        Damaged = 2,
        Destroyed = 3
    }

    enum DoorList
    {
        HFront,
        HWest,
        HEast,
        AFront,
        AWest,
        AEast,
        Max
    }

    enum NodePointType
    {
        Refinery,
        Quarry,
        Docks,
        Hangar,
        Workshop,

        // Graveyards
        GraveyardA,
        GraveyardH,

        Max
    }

    enum NodeState
    {
        Neutral,
        ConflictA,
        ConflictH,
        ControlledA,
        ControlledH
    }

    enum BroadcastTextIds
    {
        FrontGateHordeDestroyed = 35409,
        FrontGateAllianceDestroyed = 35410,
        WestGateHordeDestroyed = 35411,
        WestGateAllianceDestroyed = 35412,
        EastGateHordeDestroyed = 35413,
        EastGateAllianceDestroyed = 35414
    }

    enum GameObjectIds
    {
        Teleporter1 = 195314, // 195314 H-Out 66549
        Teleporter2 = 195313, // 195313 H-In 66548

        Teleporter3 = 195315, // 195315 A-Out 66549
        Teleporter4 = 195316, // 195316 A-In 66548

        TeleporterEffectsA = 195701,
        TeleporterEffectsH = 195702,

        DoodadHuPortcullis01 = 195436,
        DoodadNdHumanGateClosedfxDoor01 = 195703,
        DoodadPortcullisactive02 = 195452,
        DoodadVrPortcullis01 = 195437,

        HordeGate1 = 195494,
        HordeGate2 = 195495,
        HordeGate3 = 195496,

        AllianceGate1 = 195699,
        AllianceGate2 = 195700,
        AllianceGate3 = 195698,

        DoodadNdWinterorcWallGatefxDoor01 = 195491,

        // Banners
        BannerWorkshopControlledH = 195130,
        BannerWorkshopControlledA = 195132,
        BannerWorkshopControlledN = 195133,
        BannerWorkshopContestedA = 195144,
        BannerWorkshopContestedH = 195145,

        BannerDocksControlledA = 195149,
        BannerDocksContestedA = 195150,
        BannerDocksControlledH = 195151,
        BannerDocksContestedH = 195152,
        BannerDocksControlledN = 195157,

        BannerHangarControlledA = 195153,
        BannerHangarContestedA = 195154,
        BannerHangarControlledH = 195155,
        BannerHangarContestedH = 195156,
        BannerHangarControlledN = 195158,

        BannerQuarryControlledA = 195334,
        BannerQuarryControlledH = 195336,
        BannerQuarryContestedA = 195335,
        BannerQuarryContestedH = 195337,
        BannerQuarryControlledN = 195338,

        BannerRefineryControlledA = 195339,
        BannerRefineryControlledH = 195341,
        BannerRefineryContestedA = 195340,
        BannerRefineryContestedH = 195342,
        BannerRefineryControlledN = 195343,

        BannerHordeKeepControlledA = 195391,
        BannerHordeKeepControlledH = 195393,
        BannerHordeKeepContestedA = 195392,
        BannerHordeKeepContestedH = 195394,

        BannerAllianceKeepControlledA = 195396,
        BannerAllianceKeepControlledH = 195398,
        BannerAllianceKeepContestedA = 195397,
        BannerAllianceKeepContestedH = 195399,

        KeepGateH = 195223,
        KeepGateA = 195451,
        KeepGate2A = 195452,

        HordeGunship = 195276,
        AllianceGunship = 195121
    }
    #endregion
}