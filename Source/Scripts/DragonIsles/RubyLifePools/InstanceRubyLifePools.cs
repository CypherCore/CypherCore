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
