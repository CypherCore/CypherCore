using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Maps;
using Framework.Constants;
using Framework.Dynamic;

namespace Scripts.Northrend.FrozenHalls.PitOfSaron
{
    struct Misc
    {
        // positions for Martin Victus (37591) and Gorkun Ironskull (37592)
        public static Position SlaveLeaderPos = new Position(689.7158f, -104.8736f, 513.7360f, 0.0f);
        // position for Jaina and Sylvanas
        public static Position EventLeaderPos2 = new Position(1054.368f, 107.14620f, 628.4467f, 0.0f);

        public static DoorData[] Doors =
        {
            new DoorData(GameObjectIds.IceWall,                     DataTypes.Garfrost,  DoorType.Passage),
            new DoorData(GameObjectIds.IceWall,                     DataTypes.Ick,       DoorType.Passage),
            new DoorData(GameObjectIds.HallsOfReflectionPortcullis, DataTypes.Tyrannus,  DoorType.Passage),
        };
    }

    [Script]
    class instance_pit_of_saron : InstanceMapScript
    {
        public instance_pit_of_saron() : base(nameof(instance_pit_of_saron), 658) { }

        class instance_pit_of_saron_InstanceScript : InstanceScript
        {
            public instance_pit_of_saron_InstanceScript(InstanceMap map) : base(map)
            {
                SetHeaders("POS");
                SetBossNumber(3);
                LoadDoorData(Misc.Doors);
                _teamInInstance = 0;
                _cavernActive = 0;
                _shardsHit = 0;
            }

            public override void OnPlayerEnter(Player player)
            {
                if (_teamInInstance == 0)
                    _teamInInstance = player.GetTeam();
            }

            public override void OnCreatureCreate(Creature creature)
            {
                if (_teamInInstance == 0)
                {
                    var players = instance.GetPlayers();
                    if (!players.Empty())
                    {
                        Player player = players[0];
                        if (player)
                            _teamInInstance = player.GetTeam();
                    }
                }

                switch (creature.GetEntry())
                {
                    case CreatureIds.Garfrost:
                        _garfrostGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Krick:
                        _krickGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Ick:
                        _ickGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Tyrannus:
                        _tyrannusGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Rimefang:
                        _rimefangGUID = creature.GetGUID();
                        break;
                    case CreatureIds.TyrannusEvents:
                        _tyrannusEventGUID = creature.GetGUID();
                        break;
                    case CreatureIds.SylvanasPart1:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.JainaPart1);
                        _jainaOrSylvanas1GUID = creature.GetGUID();
                        break;
                    case CreatureIds.SylvanasPart2:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.JainaPart2);
                        _jainaOrSylvanas2GUID = creature.GetGUID();
                        break;
                    case CreatureIds.Kilara:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Elandra);
                        break;
                    case CreatureIds.Koralen:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Korlaen);
                        break;
                    case CreatureIds.Champion1Horde:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Champion1Alliance);
                        break;
                    case CreatureIds.Champion2Horde:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Champion2Alliance);
                        break;
                    case CreatureIds.Champion3Horde: // No 3rd set for Alliance?
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.Champion2Alliance);
                        break;
                    case CreatureIds.HordeSlave1:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.AllianceSlave1);
                        break;
                    case CreatureIds.HordeSlave2:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.AllianceSlave2);
                        break;
                    case CreatureIds.HordeSlave3:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.AllianceSlave3);
                        break;
                    case CreatureIds.HordeSlave4:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.AllianceSlave4);
                        break;
                    case CreatureIds.FreedSlave1Horde:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.FreedSlave1Alliance);
                        break;
                    case CreatureIds.FreedSlave2Horde:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.FreedSlave2Alliance);
                        break;
                    case CreatureIds.FreedSlave3Horde:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.FreedSlave3Alliance);
                        break;
                    case CreatureIds.RescuedSlaveHorde:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.RescuedSlaveAlliance);
                        break;
                    case CreatureIds.MartinVictus1:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.MartinVictus1);
                        break;
                    case CreatureIds.MartinVictus2:
                        if (_teamInInstance == Team.Alliance)
                            creature.UpdateEntry(CreatureIds.MartinVictus2);
                        break;
                    case CreatureIds.CavernEventTrigger:
                        _cavernstriggersVector.Add(creature.GetGUID());
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.IceWall:
                    case GameObjectIds.HallsOfReflectionPortcullis:
                        AddDoor(go, true);
                        break;
                }
            }

            public override void OnGameObjectRemove(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.IceWall:
                    case GameObjectIds.HallsOfReflectionPortcullis:
                        AddDoor(go, false);
                        break;
                }
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case DataTypes.Garfrost:
                        if (state == EncounterState.Done)
                        {
                            Creature summoner = instance.GetCreature(_garfrostGUID);
                            if (summoner)
                            {
                                if (_teamInInstance == Team.Alliance)
                                    summoner.SummonCreature(CreatureIds.MartinVictus1, Misc.SlaveLeaderPos, TempSummonType.ManualDespawn);
                                else
                                    summoner.SummonCreature(CreatureIds.GorkunIronskull2, Misc.SlaveLeaderPos, TempSummonType.ManualDespawn);
                            }
                        }
                        break;
                    case DataTypes.Tyrannus:
                        if (state == EncounterState.Done)
                        {
                            Creature summoner = instance.GetCreature(_tyrannusGUID);
                            if (summoner)
                            {
                                if (_teamInInstance == Team.Alliance)
                                    summoner.SummonCreature(CreatureIds.JainaPart2, Misc.EventLeaderPos2, TempSummonType.ManualDespawn);
                                else
                                    summoner.SummonCreature(CreatureIds.SylvanasPart2, Misc.EventLeaderPos2, TempSummonType.ManualDespawn);
                            }
                        }
                        break;
                    default:
                        break;
                }

                return true;
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DataTypes.TeamInInstance:
                        return (uint)_teamInInstance;
                    case DataTypes.IceShardsHit:
                        return _shardsHit;
                    case DataTypes.CavernActive:
                        return _cavernActive;
                    default:
                        break;
                }

                return 0;
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case DataTypes.IceShardsHit:
                        _shardsHit = (byte)data;
                        break;
                    case DataTypes.CavernActive:
                        if (data != 0)
                        {
                            _cavernActive = (byte)data;
                            HandleCavernEventTrigger(true);
                        }
                        else
                            HandleCavernEventTrigger(false);
                        break;
                    default:
                        break;
                }
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case DataTypes.Garfrost:
                        return _garfrostGUID;
                    case DataTypes.Krick:
                        return _krickGUID;
                    case DataTypes.Ick:
                        return _ickGUID;
                    case DataTypes.Tyrannus:
                        return _tyrannusGUID;
                    case DataTypes.Rimefang:
                        return _rimefangGUID;
                    case DataTypes.TyrannusEvent:
                        return _tyrannusEventGUID;
                    case DataTypes.JainaSylvanas1:
                        return _jainaOrSylvanas1GUID;
                    case DataTypes.JainaSylvanas2:
                        return _jainaOrSylvanas2GUID;
                    default:
                        break;
                }

                return ObjectGuid.Empty;
            }

            void HandleCavernEventTrigger(bool activate)
            {
                foreach (ObjectGuid guid in _cavernstriggersVector)
                {
                    Creature trigger = instance.GetCreature(guid);
                    if (trigger)
                    {
                        if (activate)
                            trigger.m_Events.AddEvent(new ScheduledIcicleSummons(trigger), trigger.m_Events.CalculateTime(1000));
                        else
                            trigger.m_Events.KillAllEvents(false);
                    }
                }
            }

            ObjectGuid _garfrostGUID;
            ObjectGuid _krickGUID;
            ObjectGuid _ickGUID;
            ObjectGuid _tyrannusGUID;
            ObjectGuid _rimefangGUID;

            ObjectGuid _tyrannusEventGUID;
            ObjectGuid _jainaOrSylvanas1GUID;
            ObjectGuid _jainaOrSylvanas2GUID;
            List<ObjectGuid> _cavernstriggersVector = new List<ObjectGuid>();

            Team _teamInInstance;
            byte _shardsHit;
            byte _cavernActive;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_pit_of_saron_InstanceScript(map);
        }
    }

    class ScheduledIcicleSummons : BasicEvent
    {
        public ScheduledIcicleSummons(Creature trigger)
        {
            _trigger = trigger;
        }

        public override bool Execute(ulong time, uint diff)
        {
            if (RandomHelper.randChance(12))
            {
                _trigger.CastSpell(_trigger, SpellIds.IcicleSummon, true);
                _trigger.m_Events.AddEvent(new ScheduledIcicleSummons(_trigger), _trigger.m_Events.CalculateTime(RandomHelper.URand(20000, 35000)));
            }
            else
                _trigger.m_Events.AddEvent(new ScheduledIcicleSummons(_trigger), _trigger.m_Events.CalculateTime(RandomHelper.URand(1000, 20000)));

            return true;
        }

        Creature _trigger;
    }
}
