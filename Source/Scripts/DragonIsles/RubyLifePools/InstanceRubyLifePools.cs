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
        public const uint BossMelidrussaChillworn = 188252;
        public const uint BossKokiaBlazehoof = 189232;
        public const uint BossKyrakka = 190484;
    }

    struct GameObjectIds
    {
        public const uint FireWall = 377194;
    }

    class instance_ruby_life_pools : InstanceMapScript
    {
        static ObjectData[] creatureData =
        {
            new(CreatureIds.BossMelidrussaChillworn, DataTypes.MelidrussaChillworn),
            new(CreatureIds.BossKokiaBlazehoof, DataTypes.KokiaBlazehoof),
            new(CreatureIds.BossKyrakka, DataTypes.KyrakkaAndErkhartStormvein)
        };

        static DoorData[] doorData =
        {
            new(GameObjectIds.FireWall, DataTypes.KokiaBlazehoof, DoorType.Passage)
        };

        static DungeonEncounterData[] encounters =
        {
            new(DataTypes.MelidrussaChillworn, 2609),
            new(DataTypes.KokiaBlazehoof, 2606),
            new(DataTypes.KyrakkaAndErkhartStormvein, 2623)
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