/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using Game.Maps;
using Game.Scripting;
using Game.Entities;
using Framework.Constants;

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

    struct BossIds
    {
        // Bosses
        public const uint GarothiWorldbreaker = 122450;
        public const uint EncounterCount = 10;
    }

    struct CreatureIds
    {
        // Garothi Worldbreaker
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

    [Script]
    class instance_antorus_the_burning_throne : InstanceMapScript
    {
        static ObjectData[] creatureData =
        {
            new ObjectData(BossIds.GarothiWorldbreaker, DataTypes.GarothiWorldbreaker),
            new ObjectData(CreatureIds.Decimator, DataTypes.Decimator),
            new ObjectData(CreatureIds.Annihilator, DataTypes.Annihilator)
        };

        static DoorData[] doorData =
        {
            new DoorData(GameObjectIds.Collision, DataTypes.GarothiWorldbreaker, DoorType.Passage),
            new DoorData(GameObjectIds.Rock, DataTypes.GarothiWorldbreaker, DoorType.Passage)
        };

        static DungeonEncounterData[] encounters =
        {
            new DungeonEncounterData(DataTypes.GarothiWorldbreaker, 2076),
            new DungeonEncounterData(DataTypes.FelhoundsOfSageras, 2074),
            new DungeonEncounterData(DataTypes.AntoranHighCommand, 2070),
            new DungeonEncounterData(DataTypes.PortalKeeperHasabel, 2064),
            new DungeonEncounterData(DataTypes.EonarTheLifeBinder, 2075),
            new DungeonEncounterData(DataTypes.ImonarTheSoulhunter, 2082),
            new DungeonEncounterData(DataTypes.Kingaroth, 2088),
            new DungeonEncounterData(DataTypes.Varimathras, 2069),
            new DungeonEncounterData(DataTypes.TheCovenOfShivarra, 2073),
            new DungeonEncounterData(DataTypes.Aggramar, 2063),
            new DungeonEncounterData(DataTypes.ArgusTheUnmaker, 2092)
        };

        public instance_antorus_the_burning_throne() : base("instance_antorus_the_burning_throne", 1712) { }

        class instance_antorus_the_burning_throne_InstanceMapScript : InstanceScript
        {
            public instance_antorus_the_burning_throne_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("ABT");
                SetBossNumber(BossIds.EncounterCount);
                LoadObjectData(creatureData, null);
                LoadDoorData(doorData);
                LoadDungeonEncounterData(encounters);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                base.OnCreatureCreate(creature);

                switch (creature.GetEntry())
                {
                    case CreatureIds.Annihilation:
                        Creature garothi = GetCreature(DataTypes.GarothiWorldbreaker);
                        if (garothi)
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

