using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Dynamic;
using Game.Scripting;
using Game.BattleGrounds;
using Game.Maps;
using Framework.Constants;

namespace Scripts.Battlegrounds.RingOfValor
{
    enum GameObjectIds
    {
        Buff1 = 184663,
        Buff2 = 184664,
        Fire1 = 192704,
        Fire2 = 192705,

        Firedoor2 = 192387,
        Firedoor1 = 192388,
        Pulley1 = 192389,
        Pulley2 = 192390,
        Gear1 = 192393,
        Gear2 = 192394,
        Elevator1 = 194582,
        Elevator2 = 194586,

        PilarCollision1 = 194580, // Axe
        PilarCollision2 = 194579, // Arena // Big
        PilarCollision3 = 194581, // Lightning
        PilarCollision4 = 194578, // Ivory // Big

        Pilar1 = 194583, // Axe
        Pilar2 = 194584, // Arena
        Pilar3 = 194585, // Lightning
        Pilar4 = 194587  // Ivory
    }

    enum Data
    {
        StateOpenFences,
        StateSwitchPillars,
        StateCloseFire,

        PillarSwitchTimer = 25000,
        FireToPillarTimer = 20000,
        CloseFireTimer = 5000,
        FirstTimer = 20133,
    }

    [Script(nameof(arena_ring_of_valor), 618)]
    class arena_ring_of_valor : ArenaScript
    {
        List<ObjectGuid> _elevatorGUIDs = new();
        List<ObjectGuid> _gearGUIDs = new();
        List<ObjectGuid> _fireGUIDs = new();
        List<ObjectGuid> _firedoorGUIDs = new();
        List<ObjectGuid> _pillarSmallCollisionGUIDs = new();
        List<ObjectGuid> _pillarBigCollisionGUIDs = new();
        List<ObjectGuid> _pillarSmallGUIDs = new();
        List<ObjectGuid> _pillarBigGUIDs = new();
        List<ObjectGuid> _pulleyGUIDs = new();

        uint _timer;
        uint _state;
        bool _pillarCollision;

        public arena_ring_of_valor(BattlegroundMap map) : base(map) { }

        public override void OnUpdate(uint diff)
        {
            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                return;

            _scheduler.Update(diff);

            if (_timer < diff)
            {
                switch (_state)
                {
                    case (uint)Data.StateOpenFences:
                        // Open fire (only at game start)
                        foreach (ObjectGuid guid in _fireGUIDs)
                        {
                            GameObject go = battlegroundMap.GetGameObject(guid);
                            if (go != null)
                                go.UseDoorOrButton();
                        }
                        foreach (ObjectGuid guid in _firedoorGUIDs)
                        {
                            GameObject go = battlegroundMap.GetGameObject(guid);
                            if (go != null)
                                go.UseDoorOrButton();
                        }
                        _timer = (uint)Data.CloseFireTimer;
                        _state = (uint)Data.StateCloseFire;
                        break;
                    case (uint)Data.StateCloseFire:
                        foreach (ObjectGuid guid in _fireGUIDs)
                        {
                            GameObject go = battlegroundMap.GetGameObject(guid);
                            if (go != null)
                                go.ResetDoorOrButton();
                        }
                        foreach (ObjectGuid guid in _firedoorGUIDs)
                        {
                            GameObject go = battlegroundMap.GetGameObject(guid);
                            if (go != null)
                                go.ResetDoorOrButton();
                        }
                        // Fire got closed after five seconds, leaves twenty seconds before toggling pillars
                        _timer = (uint)Data.FireToPillarTimer;
                        _state = (uint)Data.StateSwitchPillars;
                        break;
                    case (uint)Data.StateSwitchPillars:
                        TogglePillarCollision();
                        _timer = (uint)Data.PillarSwitchTimer;
                        break;
                    default:
                        break;
                }
            }
            else
                _timer -= diff;
        }

        public override void OnInit()
        {
            CreateObject((uint)GameObjectIds.Elevator1, 763.536377f, -294.535767f, 0.505383f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Elevator2, 763.506348f, -273.873352f, 0.505383f, 0.000000f, 0.0f, 0.0f, 0.0f, 0.0f);

            CreateObject((uint)GameObjectIds.Fire1, 743.543457f, -283.799469f, 28.286655f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Fire2, 782.971802f, -283.799469f, 28.286655f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Firedoor1, 743.711060f, -284.099609f, 27.542587f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Firedoor2, 783.221252f, -284.133362f, 27.535686f, 0.000000f, 0.0f, 0.0f, 0.0f, 0.0f);

            CreateObject((uint)GameObjectIds.Gear1, 763.664551f, -261.872986f, 26.686588f, 0.000000f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Gear2, 763.578979f, -306.146149f, 26.665222f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Pulley1, 700.722290f, -283.990662f, 39.517582f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Pulley2, 826.303833f, -283.996429f, 39.517582f, 0.000000f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Pilar1, 763.632385f, -306.162384f, 25.909504f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Pilar2, 723.644287f, -284.493256f, 24.648525f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Pilar3, 763.611145f, -261.856750f, 25.909504f, 0.000000f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.Pilar4, 802.211609f, -284.493256f, 24.648525f, 0.000000f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.PilarCollision1, 763.632385f, -306.162384f, 30.639660f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.PilarCollision2, 723.644287f, -284.493256f, 32.382710f, 0.000000f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.PilarCollision3, 763.611145f, -261.856750f, 30.639660f, 0.000000f, 0.0f, 0.0f, 0.0f, 0.0f);
            CreateObject((uint)GameObjectIds.PilarCollision4, 802.211609f, -284.493256f, 32.382710f, 3.141593f, 0.0f, 0.0f, 0.0f, 0.0f);
        }

        public override void OnStart()
        {
            foreach (ObjectGuid guid in _elevatorGUIDs)
            {
                GameObject door = battlegroundMap.GetGameObject(guid);
                if (door != null)
                {
                    door.UseDoorOrButton();
                    door.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
                }
            }

            _state = (uint)Data.StateOpenFences;
            _timer = (uint)Data.FirstTimer;

            // Should be false at first, TogglePillarCollision will do it.
            _pillarCollision = true;
            TogglePillarCollision();

            _scheduler.Schedule(TimeSpan.FromMinutes(1), _ =>
            {
                CreateObject((uint)GameObjectIds.Buff1, 735.551819f, -284.794678f, 28.276682f, 0.034906f, 0.0f, 0.0f, 0.0f, 0.0f);
                CreateObject((uint)GameObjectIds.Buff2, 791.224487f, -284.794464f, 28.276682f, 2.600535f, 0.0f, 0.0f, 0.0f, 0.0f);
            });
        }

        void TogglePillarCollision()
        {
            // Toggle visual pillars, pulley, gear, and collision based on previous state

            List<ObjectGuid> smallPillarGuids = [.. _pillarSmallGUIDs, .. _gearGUIDs];
            foreach (ObjectGuid guid in smallPillarGuids)
            {
                GameObject go = battlegroundMap.GetGameObject(guid);
                if (go != null)
                {
                    if (_pillarCollision)
                        go.UseDoorOrButton();
                    else
                        go.ResetDoorOrButton();
                }
            }

            List<ObjectGuid> bigPillarGuids = [.. _pillarBigGUIDs, .. _pulleyGUIDs];
            foreach (ObjectGuid guid in bigPillarGuids)
            {
                GameObject go = battlegroundMap.GetGameObject(guid);
                if (go != null)
                {
                    if (_pillarCollision)
                        go.ResetDoorOrButton();
                    else
                        go.UseDoorOrButton();
                }
            }

            List<ObjectGuid> allObjects = [.. smallPillarGuids, .. bigPillarGuids, .. _pillarSmallCollisionGUIDs, .. _pillarBigCollisionGUIDs];
            foreach (ObjectGuid guid in allObjects)
            {
                GameObject go = battlegroundMap.GetGameObject(guid);
                if (go != null)
                {
                    bool isCollision = false;
                    switch ((GameObjectIds)go.GetEntry())
                    {
                        case GameObjectIds.PilarCollision1:
                        case GameObjectIds.PilarCollision2:
                        case GameObjectIds.PilarCollision3:
                        case GameObjectIds.PilarCollision4:
                            isCollision = true;
                            break;
                        default:
                            break;
                    }

                    if (isCollision)
                    {
                        GameObjectState state = ((go.GetGoInfo().Door.startOpen != 0) == _pillarCollision) ? GameObjectState.Active : GameObjectState.Ready;
                        go.SetGoState(state);
                    }

                    foreach (var (playerGuid, _) in battleground.GetPlayers())
                    {
                        Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                        if (player != null)
                            go.SendUpdateToPlayer(player);
                    }
                }
            }

            _pillarCollision = !_pillarCollision;
        }

        public override void OnGameObjectCreate(GameObject gameobject)
        {
            base.OnGameObjectCreate(gameobject);

            switch ((GameObjectIds)gameobject.GetEntry())
            {
                case GameObjectIds.Elevator1:
                case GameObjectIds.Elevator2:
                    _elevatorGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.Fire1:
                case GameObjectIds.Fire2:
                    _fireGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.Firedoor1:
                case GameObjectIds.Firedoor2:
                    _firedoorGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.Gear1:
                case GameObjectIds.Gear2:
                    _gearGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.Pilar1:
                case GameObjectIds.Pilar3:
                    _pillarSmallGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.Pilar2:
                case GameObjectIds.Pilar4:
                    _pillarBigGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.PilarCollision1:
                case GameObjectIds.PilarCollision3:
                    _pillarSmallCollisionGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.PilarCollision2:
                case GameObjectIds.PilarCollision4:
                    _pillarBigCollisionGUIDs.Add(gameobject.GetGUID());
                    break;
                case GameObjectIds.Pulley1:
                case GameObjectIds.Pulley2:
                    _pulleyGUIDs.Add(gameobject.GetGUID());
                    break;
                default:
                    break;
            }
        }
    }
}
