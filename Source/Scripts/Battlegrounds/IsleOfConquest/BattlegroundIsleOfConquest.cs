// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.BattleGrounds;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.IsleOfConquest
{
    enum BannersTypes
    {
        AControlled,
        AContested,
        HControlled,
        HContested
    }

    enum ExploitTeleportLocations
    {
        Alliance = 3986,
        Horde = 3983
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
        Maxdoor
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

        MaxNodeTypes
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

    enum Actions
    {
        GunshipReady = 1,
        InteractCapturableObject = 2,
        CaptureCapturableObject = 3
    }

    enum HonorRewards
    {
        ResourceAmount = 12,
        WinnerAmount = 500
    }

    enum PvpStats
    {
        BasesAssaulted = 245,
        BasesDefended = 246
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

    struct Misc
    {
        public const ushort MAX_REINFORCEMENTS = 400;

        public static TimeSpan IOC_RESOURCE_TIMER = TimeSpan.FromSeconds(45);

        public static Position[] GunshipTeleportTriggerPosition =
        {
            new(11.69964981079101562f, 0.034145999699831008f, 20.62075996398925781f, 3.211405754089355468f),
            new(7.30560922622680664f, -0.09524600207805633f, 34.51021575927734375f, 3.159045934677124023f)
        };

        public static IoCStaticNodeInfo[] nodePointInitial =
        {
            new(NodePointType.Refinery, 35377, 35378, 35379, 35380, WorldStateIds.RefineryUncontrolled, WorldStateIds.RefineryConflictA, WorldStateIds.RefineryConflictH, WorldStateIds.RefineryControlledA, WorldStateIds.RefineryControlledH),
            new(NodePointType.Quarry, 35373, 35374, 35375, 35376, WorldStateIds.QuarryUncontrolled, WorldStateIds.QuarryConflictA, WorldStateIds.QuarryConflictH, WorldStateIds.QuarryControlledA, WorldStateIds.QuarryControlledH),
            new(NodePointType.Docks, 35365, 35366, 35367, 35368, WorldStateIds.DocksUncontrolled, WorldStateIds.DocksConflictA, WorldStateIds.DocksConflictH, WorldStateIds.DocksControlledA, WorldStateIds.DocksControlledH),
            new(NodePointType.Hangar, 35369, 35370, 35371, 35372, WorldStateIds.HangarUncontrolled, WorldStateIds.HangarConflictA, WorldStateIds.HangarConflictH, WorldStateIds.HangarControlledA, WorldStateIds.HangarControlledH),
            new(NodePointType.Workshop, 35278, 35286, 35279, 35280, WorldStateIds.WorkshopUncontrolled, WorldStateIds.WorkshopConflictA, WorldStateIds.WorkshopConflictH, WorldStateIds.WorkshopControlledA, WorldStateIds.WorkshopControlledH),
            new(NodePointType.GraveyardA, 35461, 35459, 35463, 35466, WorldStateIds.AllianceKeepUncontrolled, WorldStateIds.AllianceKeepConflictA, WorldStateIds.AllianceKeepConflictH, WorldStateIds.AllianceKeepControlledA, WorldStateIds.AllianceKeepControlledH),
            new(NodePointType.GraveyardH, 35462, 35460, 35464, 35465, WorldStateIds.HordeKeepUncontrolled, WorldStateIds.HordeKeepConflictA, WorldStateIds.HordeKeepConflictH, WorldStateIds.HordeKeepControlledA, WorldStateIds.HordeKeepControlledH)
        };
    }

    // I.E: Hangar, Quarry, Graveyards .. etc
    struct IoCStaticNodeInfo
    {
        public NodePointType NodeType;
        public uint AssaultedTextId;
        public uint DefendedTextId;
        public uint AllianceTakenTextId;
        public uint HordeTakenTextId;
        public int UncontrolledWorldState;
        public int ConflictAWorldState;
        public int ConflictHWorldState;
        public int ControlledAWorldState;
        public int ControlledHWorldState;

        public IoCStaticNodeInfo(NodePointType nodeType, uint assaultedTextId, uint defendedTextId, uint allianceTakenTextId, uint hordeTakenTextId, WorldStateIds uncontrolledWorldState, WorldStateIds conflictAWorldState, WorldStateIds conflictHWorldState, WorldStateIds controlledAWorldState, WorldStateIds controlledHWorldState)
        {
            NodeType = nodeType;
            AssaultedTextId = assaultedTextId;
            DefendedTextId = defendedTextId;
            AllianceTakenTextId = allianceTakenTextId;
            HordeTakenTextId = hordeTakenTextId;
            UncontrolledWorldState = (int)uncontrolledWorldState;
            ConflictAWorldState = (int)conflictAWorldState;
            ConflictHWorldState = (int)conflictHWorldState;
            ControlledAWorldState = (int)controlledAWorldState;
            ControlledHWorldState = (int)controlledHWorldState;
        }
    }

    class ICNodePoint
    {
        NodeState _state;
        int _lastControlled;
        IoCStaticNodeInfo _nodeInfo;

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

    [Script(nameof(battleground_isle_of_conquest), 628)]
    class battleground_isle_of_conquest : BattlegroundScript
    {
        ushort[] _factionReinforcements = new ushort[SharedConst.PvpTeamsCount];
        GateState[] _gateStatus = new GateState[6];
        ICNodePoint[] _nodePoints = new ICNodePoint[7];
        ObjectGuid[] _gunshipGUIDs = new ObjectGuid[SharedConst.PvpTeamsCount];
        List<ObjectGuid> _teleporterGUIDs = new();
        List<ObjectGuid> _teleporterEffectGUIDs = new();
        List<ObjectGuid> _mainGateDoorGUIDs = new();
        List<ObjectGuid> _portcullisGUIDs = new();
        List<ObjectGuid> _wallGUIDs = new();
        List<ObjectGuid>[] _cannonGUIDs = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
        List<ObjectGuid>[] _keepGateGUIDs = new List<ObjectGuid>[SharedConst.PvpTeamsCount];
        ObjectGuid[] _keepBannerGUIDs = new ObjectGuid[SharedConst.PvpTeamsCount];
        ObjectGuid _gunshipTeleportTarget;

        TimeTracker _resourceTimer;

        public battleground_isle_of_conquest(BattlegroundMap map) : base(map)
        {
            _factionReinforcements = [Misc.MAX_REINFORCEMENTS, Misc.MAX_REINFORCEMENTS];

            _gateStatus = [GateState.Ok, GateState.Ok, GateState.Ok, GateState.Ok, GateState.Ok, GateState.Ok];

            for (var i = 0; i < SharedConst.PvpTeamsCount; i++)
            {
                _cannonGUIDs[i] = new List<ObjectGuid>();
                _keepGateGUIDs[i] = new List<ObjectGuid>();
            }

            _nodePoints[(int)NodePointType.Refinery] = new(NodeState.Neutral, Misc.nodePointInitial[(int)NodePointType.Refinery]);
            _nodePoints[(int)NodePointType.Quarry] = new(NodeState.Neutral, Misc.nodePointInitial[(int)NodePointType.Quarry]);
            _nodePoints[(int)NodePointType.Docks] = new(NodeState.Neutral, Misc.nodePointInitial[(int)NodePointType.Docks]);
            _nodePoints[(int)NodePointType.Hangar] = new(NodeState.Neutral, Misc.nodePointInitial[(int)NodePointType.Hangar]);
            _nodePoints[(int)NodePointType.Workshop] = new(NodeState.Neutral, Misc.nodePointInitial[(int)NodePointType.Workshop]);
            _nodePoints[(int)NodePointType.GraveyardA] = new(NodeState.ControlledA, Misc.nodePointInitial[(int)NodePointType.GraveyardA]);
            _nodePoints[(int)NodePointType.GraveyardH] = new(NodeState.ControlledH, Misc.nodePointInitial[(int)NodePointType.GraveyardH]);

            _resourceTimer.Reset(Misc.IOC_RESOURCE_TIMER);
        }

        public override void OnUpdate(uint diff)
        {
            base.OnUpdate(diff);

            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
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
                        battleground.RewardHonorToTeam((uint)HonorRewards.ResourceAmount, _nodePoints[i].GetLastControlledTeam() == BattleGroundTeamId.Alliance ? Team.Alliance : Team.Horde);
                        UpdateWorldState((int)(_nodePoints[i].GetLastControlledTeam() == BattleGroundTeamId.Alliance ? WorldStateIds.AllianceReinforcements : WorldStateIds.HordeReinforcements), _factionReinforcements[_nodePoints[i].GetLastControlledTeam()]);;
                    }
                }

                _resourceTimer.Reset(Misc.IOC_RESOURCE_TIMER);
            }
        }

        public override void OnStart()
        {
            base.OnStart();

            void gameobjectAction(List<ObjectGuid> guids, Action<GameObject> action)
            {
                foreach (ObjectGuid guid in guids)
                {
                    GameObject gameObject = battlegroundMap.GetGameObject(guid);
                    if (gameObject != null)
                        action(gameObject);
                }
            }

            gameobjectAction(_mainGateDoorGUIDs, gameobject =>
            {
                gameobject.UseDoorOrButton();
                gameobject.DespawnOrUnsummon(TimeSpan.FromSeconds(20));
            });

            gameobjectAction(_portcullisGUIDs, gameobject => gameobject.UseDoorOrButton());

            gameobjectAction(_teleporterGUIDs, gameobject => gameobject.RemoveFlag(GameObjectFlags.NotSelectable));

            gameobjectAction(_teleporterEffectGUIDs, gameobject => gameobject.SetGoState(GameObjectState.Active));

            _scheduler.Schedule(TimeSpan.FromSeconds(20), _ =>
            {
                foreach (ObjectGuid guid in _wallGUIDs)
                {
                    GameObject gameobject = battlegroundMap.GetGameObject(guid);
                    if (gameobject != null)
                        gameobject.SetDestructibleState(GameObjectDestructibleState.Damaged);
                }
            });
        }

        public override void OnUnitKilled(Creature unit, Unit killer)
        {
            base.OnUnitKilled(unit, killer);

            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                return;

            switch ((CreatureIds)unit.GetEntry())
            {
                case CreatureIds.HighCommanderHalfordWyrmbane:
                    battleground.RewardHonorToTeam((uint)HonorRewards.WinnerAmount, Team.Horde);
                    battleground.EndBattleground(Team.Horde);

                    break;
                case CreatureIds.OverlordAgmar:
                    battleground.RewardHonorToTeam((uint)HonorRewards.WinnerAmount, Team.Alliance);
                    battleground.EndBattleground(Team.Alliance);
                    break;
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

        public override void OnPlayerKilled(Player player, Player killer)
        {
            base.OnPlayerKilled(player, killer);

            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                return;

            int victimTeamId = Battleground.GetTeamIndexByTeamId(battleground.GetPlayerTeam(player.GetGUID()));
            _factionReinforcements[victimTeamId] -= 1;

            UpdateWorldState((int)(battleground.GetPlayerTeam(player.GetGUID()) == Team.Alliance ? WorldStateIds.AllianceReinforcements : WorldStateIds.HordeReinforcements), _factionReinforcements[victimTeamId]);

            // we must end the battleground
            if (_factionReinforcements[victimTeamId] < 1)
                battleground.EndBattleground(battleground.GetPlayerTeam(killer.GetGUID()));
        }

        static uint GetGateIDFromEntry(uint id)
        {
            switch ((GameObjectIds)id)
            {
                case GameObjectIds.HordeGate1:
                    return (uint)DoorList.HFront;
                case GameObjectIds.HordeGate2:
                    return (uint)DoorList.HWest;
                case GameObjectIds.HordeGate3:
                    return (uint)DoorList.HEast;
                case GameObjectIds.AllianceGate3:
                    return (uint)DoorList.AFront;
                case GameObjectIds.AllianceGate1:
                    return (uint)DoorList.AWest;
                case GameObjectIds.AllianceGate2:
                    return (uint)DoorList.AEast;
                default:
                    return 0;
            }
        }

        static int GetWorldStateFromGateEntry(uint id, bool open)
        {
            WorldStateIds uws = 0;

            switch ((GameObjectIds)id)
            {
                case GameObjectIds.HordeGate1:
                    uws = (open ? WorldStateIds.GateFrontHWsOpen : WorldStateIds.GateFrontHWsClosed);
                    break;
                case GameObjectIds.HordeGate2:
                    uws = (open ? WorldStateIds.GateWestHWsOpen : WorldStateIds.GateWestHWsClosed);
                    break;
                case GameObjectIds.HordeGate3:
                    uws = (open ? WorldStateIds.GateEastHWsOpen : WorldStateIds.GateEastHWsClosed);
                    break;
                case GameObjectIds.AllianceGate3:
                    uws = (open ? WorldStateIds.GateFrontAWsOpen : WorldStateIds.GateFrontAWsOpen);
                    break;
                case GameObjectIds.AllianceGate1:
                    uws = (open ? WorldStateIds.GateWestAWsOpen : WorldStateIds.GateWestAWsClosed);
                    break;
                case GameObjectIds.AllianceGate2:
                    uws = (open ? WorldStateIds.GateEastAWsOpen : WorldStateIds.GateEastAWsClosed);
                    break;
                default:
                    break;
            }
            return (int)uws;
        }

        void UpdateNodeWorldState(ICNodePoint node)
        {
            UpdateWorldState(node.GetNodeInfo().ConflictAWorldState, node.GetState() == NodeState.ConflictA);
            UpdateWorldState(node.GetNodeInfo().ConflictHWorldState, node.GetState() == NodeState.ConflictH);
            UpdateWorldState(node.GetNodeInfo().ControlledAWorldState, node.GetState() == NodeState.ControlledA);
            UpdateWorldState(node.GetNodeInfo().ControlledHWorldState, node.GetState() == NodeState.ControlledH);
            UpdateWorldState(node.GetNodeInfo().UncontrolledWorldState, node.GetState() == NodeState.Neutral);
        }

        static NodePointType BannerToNodeType(uint bannerId)
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
                    return NodePointType.Refinery;
                default:
                    break;
            }

            return NodePointType.MaxNodeTypes;
        }

        void HandleCapturedNodes(ICNodePoint node)
        {
            if (node.GetLastControlledTeam() == BattleGroundTeamId.Neutral)
                return;

            switch (node.GetNodeInfo().NodeType)
            {
                case NodePointType.Quarry:
                case NodePointType.Refinery:
                    battlegroundMap.UpdateAreaDependentAuras();
                    break;
                case NodePointType.Hangar:
                    Transport transport = battlegroundMap.GetTransport(_gunshipGUIDs[node.GetLastControlledTeam()]);
                    if (transport != null)
                    {
                        // Can't have this in spawngroup, creature is on a transport
                        TempSummon trigger = transport.SummonPassenger((uint)CreatureIds.WorldTriggerNotFloating, Misc.GunshipTeleportTriggerPosition[node.GetLastControlledTeam()], TempSummonType.ManualDespawn);
                        if (trigger != null)
                            _gunshipTeleportTarget = trigger.GetGUID();

                        transport.EnableMovement(true);
                    }

                    foreach (ObjectGuid guid in _cannonGUIDs[node.GetLastControlledTeam()])
                    {
                        Creature cannon = battlegroundMap.GetCreature(guid);
                        if (cannon != null)
                            cannon.SetUninteractible(false);
                    }
                    break;
                default:
                    break;
            }
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
                case GameObjectIds.BannerAllianceKeepControlledH:
                    _keepBannerGUIDs[BattleGroundTeamId.Horde] = gameobject.GetGUID();
                    break;
                default:
                    break;
            }
        }

        public override void OnInit()
        {
            base.OnInit();

            Transport transport = Global.TransportMgr.CreateTransport((uint)GameObjectIds.HordeGunship, battlegroundMap);
            if (transport != null)
            {
                _gunshipGUIDs[BattleGroundTeamId.Horde] = transport.GetGUID();
                transport.EnableMovement(false);
            }

            transport = Global.TransportMgr.CreateTransport((uint)GameObjectIds.AllianceGunship, battlegroundMap);
            if (transport != null)
            {
                _gunshipGUIDs[BattleGroundTeamId.Alliance] = transport.GetGUID();
                transport.EnableMovement(false);
            }
        }

        public override void DoAction(uint actionId, WorldObject source, WorldObject target)
        {
            base.DoAction(actionId, source, target);

            switch ((Actions)actionId)
            {
                case Actions.InteractCapturableObject:
                    OnPlayerInteractWithBanner(source?.ToPlayer(), target?.ToGameObject());
                    break;
                case Actions.CaptureCapturableObject:
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

            Team playerTeam = battleground.GetPlayerTeam(player.GetGUID());
            int playerTeamId = Battleground.GetTeamIndexByTeamId(playerTeam);
            NodePointType nodeType = BannerToNodeType(banner.GetEntry());
            if (nodeType == NodePointType.MaxNodeTypes)
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

            battlegroundMap.UpdateSpawnGroupConditions();
        }

        void OnPlayerAssaultNode(Player player, ICNodePoint node)
        {
            if (player == null)
                return;

            Team playerTeam = battleground.GetPlayerTeam(player.GetGUID());
            int playerTeamId = Battleground.GetTeamIndexByTeamId(playerTeam);

            NodeState newState = playerTeamId == BattleGroundTeamId.Horde ? NodeState.ConflictH : NodeState.ConflictA;
            node.UpdateState(newState);

            battleground.UpdatePvpStat(player, (uint)PvpStats.BasesAssaulted, 1);

            ChatMsg messageType = playerTeamId == BattleGroundTeamId.Alliance ? ChatMsg.BgSystemAlliance : ChatMsg.BgSystemHorde;
            battleground.SendBroadcastText(node.GetNodeInfo().AssaultedTextId, messageType, player);
            UpdateNodeWorldState(node);

            // apply side effects of each node, only if it wasn't neutral before
            if (node.GetLastControlledTeam() == BattleGroundTeamId.Neutral)
                return;

            switch (node.GetNodeInfo().NodeType)
            {
                case NodePointType.Hangar:
                    Transport transport = battlegroundMap.GetTransport(_gunshipGUIDs[node.GetLastControlledTeam()]);
                    if (transport != null)
                        transport.EnableMovement(false);

                    foreach (ObjectGuid guid in _cannonGUIDs[node.GetLastControlledTeam()])
                    {
                        Creature cannon = battlegroundMap.GetCreature(guid);
                        if (cannon != null)
                        {
                            cannon.GetVehicleKit().RemoveAllPassengers();
                            cannon.SetUninteractible(true);
                        }
                    }

                    // Despawn teleport trigger target
                    Creature creature = battlegroundMap.GetCreature(_gunshipTeleportTarget);
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

            Team playerTeam = battleground.GetPlayerTeam(player.GetGUID());
            int playerTeamId = Battleground.GetTeamIndexByTeamId(playerTeam);

            node.UpdateState(playerTeamId == BattleGroundTeamId.Horde ? NodeState.ControlledH : NodeState.ControlledA);
            HandleCapturedNodes(node);
            battleground.UpdatePvpStat(player, (uint)PvpStats.BasesDefended, 1);

            ChatMsg messageType = playerTeamId == BattleGroundTeamId.Alliance ? ChatMsg.BgSystemAlliance : ChatMsg.BgSystemHorde;
            battleground.SendBroadcastText(node.GetNodeInfo().DefendedTextId, messageType, player);
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
            if (nodeType == NodePointType.MaxNodeTypes)
                return;

            ICNodePoint node = _nodePoints[(int)nodeType];
            if (node.GetState() == NodeState.ConflictH)
                node.UpdateState(NodeState.ControlledH);
            else if (node.GetState() == NodeState.ConflictA)
                node.UpdateState(NodeState.ControlledA);

            HandleCapturedNodes(node);

            ChatMsg messageType = node.GetLastControlledTeam() == BattleGroundTeamId.Alliance ? ChatMsg.BgSystemAlliance : ChatMsg.BgSystemHorde;
            uint textId = node.GetLastControlledTeam() == BattleGroundTeamId.Alliance ? node.GetNodeInfo().AllianceTakenTextId : node.GetNodeInfo().HordeTakenTextId;
            battleground.SendBroadcastText(textId, messageType);
            UpdateNodeWorldState(node);
        }

        void OnGateDestroyed(GameObject gate, WorldObject destroyer)
        {
            _gateStatus[GetGateIDFromEntry(gate.GetEntry())] = GateState.Destroyed;
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
                var keepGates = _keepGateGUIDs[teamId];
                ObjectGuid bannerGuid = _keepBannerGUIDs[teamId];

                foreach (ObjectGuid guid in keepGates)
                {
                    GameObject keepGate = battlegroundMap.GetGameObject(guid);
                    if (keepGate != null)
                        keepGate.UseDoorOrButton();
                }

                GameObject banner = battlegroundMap.GetGameObject(bannerGuid);
                if (banner != null)
                    banner.RemoveFlag(GameObjectFlags.NotSelectable);
            }

            battleground.SendBroadcastText((uint)textId, msgType, destroyer);
        }
    }
}