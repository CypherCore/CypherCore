// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.IMap;

namespace Scripts.DragonIsles.RubyLifePools
{
    internal struct DataTypes
    {
        // Encounters
        public const uint MelidrussaChillworn = 0;
        public const uint KokiaBlazehoof = 1;
        public const uint KyrakkaAndErkhartStormvein = 2;
    }

    internal struct CreatureIds
    {
        // Bosses
        public const uint MelidrussaChillworn = 188252;
        public const uint KokiaBlazehoof = 189232;
        public const uint Kyrakka = 190484;
    }

    internal struct GameObjectIds
    {
        public const uint FireWall = 377194;
    }

    [Script]
    internal class instance_ruby_life_pools : InstanceMapScript, IInstanceMapGetInstanceScript
    {
        private class instance_ruby_life_pools_InstanceMapScript : InstanceScript
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

        public static ObjectData[] creatureData =
        {
            new(CreatureIds.MelidrussaChillworn, DataTypes.MelidrussaChillworn), new(CreatureIds.KokiaBlazehoof, DataTypes.KokiaBlazehoof), new(CreatureIds.Kyrakka, DataTypes.KyrakkaAndErkhartStormvein)
        };

        public static DoorData[] doorData =
        {
            new(GameObjectIds.FireWall, DataTypes.KokiaBlazehoof, DoorType.Passage)
        };

        public static DungeonEncounterData[] encounters =
        {
            new(DataTypes.MelidrussaChillworn, 2609), new(DataTypes.KokiaBlazehoof, 2606), new(DataTypes.KyrakkaAndErkhartStormvein, 2623)
        };

        public instance_ruby_life_pools() : base(nameof(instance_ruby_life_pools), 2521)
        {
        }

        public InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_ruby_life_pools_InstanceMapScript(map);
        }
    }
}