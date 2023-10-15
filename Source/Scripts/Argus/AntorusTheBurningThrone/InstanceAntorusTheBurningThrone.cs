// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.Argus.AntorusTheBurningThrone
{
    struct DataTypes
    {
        // Encounters
        public const uint GarothiWorldbreaker = 0;
        public const uint FelhoundsOfSageras = 1;
        public const uint AntoranHighCommand = 2;
        public const uint PortalKeeperHasabel = 3;
        public const uint EonarTheLifeBinder = 4;
        public const uint ImonarTheSoulhunter = 5;
        public const uint Kingaroth = 6;
        public const uint Varimathras = 7;
        public const uint TheCovenOfShivarra = 8;
        public const uint Aggramar = 9;
        public const uint ArgusTheUnmaker = 10;

        // Encounter related data
        public const uint Decimator = 11;
        public const uint Annihilator = 12;
    }

    struct CreatureIds
    {
        // Bosses
        public const uint BossGarothiWorldbreaker = 122450;

        // Encounter related creatures
        //Garothi Worldbreaker
        public const uint Decimator = 122773;
        public const uint Annihilator = 122778;
        public const uint Annihilation = 122818;
        public const uint GarothiWorldbreaker = 124167;
    }

    struct GameObjectIds
    {
        public const uint Collision = 277365;
        public const uint Rock = 278488;
    }

    struct MiscConst
    {
        public static ObjectData[] creatureData =
        {
            new(CreatureIds.BossGarothiWorldbreaker, DataTypes.GarothiWorldbreaker),
            new(CreatureIds.Decimator, DataTypes.Decimator),
            new(CreatureIds.Annihilator, DataTypes.Annihilator)
        };

        public static DoorData[] doorData =
        {
            new(GameObjectIds.Collision, DataTypes.GarothiWorldbreaker, Framework.Constants.DoorType.Passage),
            new(GameObjectIds.Rock, DataTypes.GarothiWorldbreaker, Framework.Constants.DoorType.Passage)
        };

        public static DungeonEncounterData[] encounters =
        {
            new(DataTypes.GarothiWorldbreaker, 2076),
            new(DataTypes.FelhoundsOfSageras, 2074),
            new(DataTypes.AntoranHighCommand, 2070),
            new(DataTypes.PortalKeeperHasabel, 2064),
            new(DataTypes.EonarTheLifeBinder, 2075),
            new(DataTypes.ImonarTheSoulhunter, 2082),
            new(DataTypes.Kingaroth, 2088),
            new(DataTypes.Varimathras, 2069),
            new(DataTypes.TheCovenOfShivarra, 2073),
            new(DataTypes.Aggramar, 2063),
            new(DataTypes.ArgusTheUnmaker, 2092)
        };
    }

    [Script]
    class instance_antorus_the_burning_throne : InstanceMapScript
    {
        public instance_antorus_the_burning_throne() : base(nameof(instance_antorus_the_burning_throne), 1712) { }

        class instance_antorus_the_burning_throne_InstanceMapScript : InstanceScript
        {
            public instance_antorus_the_burning_throne_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("ABT");
                SetBossNumber(10);
                LoadObjectData(MiscConst.creatureData, null);
                LoadDoorData(MiscConst.doorData);
                LoadDungeonEncounterData(MiscConst.encounters);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                base.OnCreatureCreate(creature);

                switch (creature.GetEntry())
                {
                    case CreatureIds.Annihilation:
                        Creature garothi = GetCreature(DataTypes.GarothiWorldbreaker);
                        if (garothi != null)
                            garothi.GetAI().JustSummoned(creature);
                        break;
                    default:
                        break;
                }
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_antorus_the_burning_throne_InstanceMapScript(map);
        }
    }
}

