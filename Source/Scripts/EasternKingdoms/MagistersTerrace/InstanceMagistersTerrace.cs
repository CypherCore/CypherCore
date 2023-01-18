// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.MagistersTerrace
{
    struct DataTypes
    {
        // Encounter states
        public const uint SelinFireheart = 0;
        public const uint Vexallus = 1;
        public const uint PriestessDelrissa = 2;
        public const uint KaelthasSunstrider = 3;

        // Encounter related
        public const uint KaelthasIntro = 4;
        public const uint DelrissaDeathCount = 5;

        // Additional data
        public const uint Kalecgos = 6;
        public const uint EscapeOrb = 7;
    }

    struct CreatureIds
    {
        // Bosses
        public const uint KaelthasSunstrider = 24664;
        public const uint SelinFireheart = 24723;
        public const uint Vexallus = 24744;
        public const uint PriestessDelrissa = 24560;

        // Encounter related
        // Kael'thas Sunstrider
        public const uint ArcaneSphere = 24708;
        public const uint FlameStrike = 24666;
        public const uint Phoenix = 24674;
        public const uint PhoenixEgg = 24675;

        // Selin Fireheart
        public const uint FelCrystal = 24722;

        // Event related
        public const uint Kalecgos = 24844;
        public const uint HumanKalecgos = 24848;
        public const uint CoilskarWitch = 24696;
        public const uint SunbladeWarlock = 24686;
        public const uint SunbladeMageGuard = 24683;
        public const uint SisterOfTorment = 24697;
        public const uint EthereumSmuggler = 24698;
        public const uint SunbladeBloodKnight = 24684;
    }

    struct GameObjectIds
    {
        public const uint AssemblyChamberDoor = 188065;
        public const uint SunwellRaidGate2 = 187979;
        public const uint SunwellRaidGate4 = 187770;
        public const uint SunwellRaidGate5 = 187896;
        public const uint AsylumDoor = 188064;
        public const uint EscapeOrb = 188173;
    }

    struct MiscConst
    {
        public const uint EventSpawnKalecgos = 16547;

        public const uint SayKalecgosSpawn = 0;

        public const uint PathKalecgosFlight = 248440;

        public static ObjectData[] creatureData =
        {
            new ObjectData(CreatureIds.SelinFireheart, DataTypes.SelinFireheart),
            new ObjectData(CreatureIds.Vexallus, DataTypes.Vexallus),
            new ObjectData(CreatureIds.PriestessDelrissa, DataTypes.PriestessDelrissa),
            new ObjectData(CreatureIds.KaelthasSunstrider, DataTypes.KaelthasSunstrider),
            new ObjectData(CreatureIds.Kalecgos, DataTypes.Kalecgos),
            new ObjectData(CreatureIds.HumanKalecgos, DataTypes.Kalecgos),
        };

        public static ObjectData[] gameObjectData =
        {
            new ObjectData(GameObjectIds.EscapeOrb, DataTypes.EscapeOrb),
        };

        public static DoorData[] doorData =
        {
            new DoorData(GameObjectIds.SunwellRaidGate2, DataTypes.SelinFireheart, DoorType.Passage),
            new DoorData(GameObjectIds.AssemblyChamberDoor, DataTypes.SelinFireheart, DoorType.Room),
            new DoorData(GameObjectIds.SunwellRaidGate5, DataTypes.Vexallus, DoorType.Passage),
            new DoorData(GameObjectIds.SunwellRaidGate4, DataTypes.PriestessDelrissa, DoorType.Passage),
            new DoorData(GameObjectIds.AsylumDoor, DataTypes.KaelthasSunstrider, DoorType.Room),
        };

        public static Position KalecgosSpawnPos = new Position(164.3747f, -397.1197f, 2.151798f, 1.66219f);
        public static Position KaelthasTrashGroupDistanceComparisonPos = new Position(150.0f, 141.0f, -14.4f);
    }

    [Script]
    class instance_magisters_terrace : InstanceMapScript
    {
        static DungeonEncounterData[] encounters =
        {
            new DungeonEncounterData(DataTypes.SelinFireheart, 1897),
            new DungeonEncounterData(DataTypes.Vexallus, 1898),
            new DungeonEncounterData(DataTypes.PriestessDelrissa, 1895),
            new DungeonEncounterData(DataTypes.KaelthasSunstrider, 1894)
        };
        
        public instance_magisters_terrace() : base(nameof(instance_magisters_terrace), 585) { }

        class instance_magisters_terrace_InstanceMapScript : InstanceScript
        {
            List<ObjectGuid> _kaelthasPreTrashGUIDs = new();
            byte _delrissaDeathCount;

            public instance_magisters_terrace_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("MT");
                SetBossNumber(4);
                LoadObjectData(MiscConst.creatureData, MiscConst.gameObjectData);
                LoadDoorData(MiscConst.doorData);
                LoadDungeonEncounterData(encounters);
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DataTypes.DelrissaDeathCount:
                        return _delrissaDeathCount;
                    default:
                        break;
                }
                return 0;
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case DataTypes.DelrissaDeathCount:
                        if (data == (uint)EncounterState.Special)
                            _delrissaDeathCount++;
                        else
                            _delrissaDeathCount = 0;
                        break;
                    default:
                        break;
                }
            }

            public override void OnCreatureCreate(Creature creature)
            {
                base.OnCreatureCreate(creature);

                switch (creature.GetEntry())
                {
                    case CreatureIds.CoilskarWitch:
                    case CreatureIds.SunbladeWarlock:
                    case CreatureIds.SunbladeMageGuard:
                    case CreatureIds.SisterOfTorment:
                    case CreatureIds.EthereumSmuggler:
                    case CreatureIds.SunbladeBloodKnight:
                        if (creature.GetDistance(MiscConst.KaelthasTrashGroupDistanceComparisonPos) < 10.0f)
                            _kaelthasPreTrashGUIDs.Add(creature.GetGUID());
                        break;
                    default:
                        break;
                }
            }

            public override void OnUnitDeath(Unit unit)
            {
                if (!unit.IsCreature())
                    return;

                switch (unit.GetEntry())
                {
                    case CreatureIds.CoilskarWitch:
                    case CreatureIds.SunbladeWarlock:
                    case CreatureIds.SunbladeMageGuard:
                    case CreatureIds.SisterOfTorment:
                    case CreatureIds.EthereumSmuggler:
                    case CreatureIds.SunbladeBloodKnight:
                        if (_kaelthasPreTrashGUIDs.Contains(unit.GetGUID()))
                        {
                            _kaelthasPreTrashGUIDs.Remove(unit.GetGUID());
                            if (_kaelthasPreTrashGUIDs.Count == 0)
                            {
                                Creature kaelthas = GetCreature(DataTypes.KaelthasSunstrider);
                                if (kaelthas)
                                    kaelthas.GetAI().SetData(DataTypes.KaelthasIntro, (uint)EncounterState.InProgress);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                base.OnGameObjectCreate(go);

                switch (go.GetEntry())
                {
                    case GameObjectIds.EscapeOrb:
                        if (GetBossState(DataTypes.KaelthasSunstrider) == EncounterState.Done)
                            go.RemoveFlag(GameObjectFlags.NotSelectable);
                        break;
                    default:
                        break;
                }
            }

            public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker)
            {
                if (eventId == MiscConst.EventSpawnKalecgos)
                    if (!GetCreature(DataTypes.Kalecgos) && _events.Empty())
                        _events.ScheduleEvent(MiscConst.EventSpawnKalecgos, TimeSpan.FromMinutes(1));
            }

            public override void Update(uint diff)
            {
                _events.Update(diff);

                if (_events.ExecuteEvent() == MiscConst.EventSpawnKalecgos)
                {
                    Creature kalecgos = instance.SummonCreature(CreatureIds.Kalecgos, MiscConst.KalecgosSpawnPos);
                    if (kalecgos)
                    {
                        kalecgos.GetMotionMaster().MovePath(MiscConst.PathKalecgosFlight, false);
                        kalecgos.GetAI().Talk(MiscConst.SayKalecgosSpawn);
                    }
                }
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case DataTypes.PriestessDelrissa:
                        if (state == EncounterState.InProgress)
                            _delrissaDeathCount = 0;
                        break;
                    case DataTypes.KaelthasSunstrider:
                        if (state == EncounterState.Done)
                        {
                            GameObject orb = GetGameObject(DataTypes.EscapeOrb);
                            if (orb != null)
                                orb.RemoveFlag(GameObjectFlags.NotSelectable);
                        }
                        break;
                    default:
                        break;
                }
                return true;
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_magisters_terrace_InstanceMapScript(map);
        }
    }
}

