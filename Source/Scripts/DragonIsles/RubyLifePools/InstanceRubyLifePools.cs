// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;
using Game.Scripting;

namespace Scripts.DragonIsles.RubyLifePools
{
    struct DataTypes
    {
        // Encounters
        public const uint MelidrussaChillworn = 0;
        public const uint KokiaBlazehoof = 1;
        public const uint KyrakkaAndErkhartStormvein = 2;
    }

    struct CreatureIds
    {
        // Bosses
        public const uint MelidrussaChillworn = 188252;
        public const uint KokiaBlazehoof = 189232;
        public const uint Kyrakka = 190484;
    }

    struct GameObjectIds
    {
        public const uint FireWall = 377194;
    }

    [Script]
    class instance_ruby_life_pools : InstanceMapScript
    {
        public static ObjectData[] creatureData =
        {
            new ObjectData(CreatureIds.MelidrussaChillworn, DataTypes.MelidrussaChillworn),
            new ObjectData(CreatureIds.KokiaBlazehoof, DataTypes.KokiaBlazehoof),
            new ObjectData(CreatureIds.Kyrakka, DataTypes.KyrakkaAndErkhartStormvein),
        };

        public static DoorData[] doorData =
        {
            new DoorData(GameObjectIds.FireWall, DataTypes.KokiaBlazehoof, DoorType.Passage),
        };

        public static DungeonEncounterData[] encounters =
        {
            new DungeonEncounterData(DataTypes.MelidrussaChillworn, 2609 ),
            new DungeonEncounterData(DataTypes.KokiaBlazehoof, 2606 ),
            new DungeonEncounterData(DataTypes.KyrakkaAndErkhartStormvein, 2623 )
        };

        public instance_ruby_life_pools() : base(nameof(instance_ruby_life_pools), 2521) { }

        class instance_ruby_life_pools_InstanceMapScript : InstanceScript
        {
            public instance_ruby_life_pools_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("RLP");
                SetBossNumber(3);
                LoadObjectData(creatureData, null);
                LoadDoorData(doorData);
                LoadDungeonEncounterData(encounters);
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_ruby_life_pools_InstanceMapScript(map);
        }
    }
}
