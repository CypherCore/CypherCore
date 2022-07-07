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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns
{
    struct DataTypes
    {
        // Encounter States // Boss GUIDs
        public const uint RomoggBonecrusher = 0;
        public const uint Corla = 1;
        public const uint KarshSteelbender = 2;
        public const uint Beauty = 3;
        public const uint AscendantLordObsidius = 4;

        // Additional Objects
        public const uint RazTheCrazed = 5;
    }

    struct CreatureIds
    {
        public const uint TwilightFlameCaller = 39708;
        public const uint RazTheCrazed = 39670;
        public const uint RomoggBonecrusher = 39665;
    }

    [Script]
    class instance_blackrock_caverns : InstanceMapScript
    {
        static ObjectData[] creatureData =
        {
            new ObjectData(CreatureIds.RazTheCrazed, DataTypes.RazTheCrazed)
        };

        public instance_blackrock_caverns() : base(nameof(instance_blackrock_caverns), 645) { }

        class instance_blackrock_caverns_InstanceMapScript : InstanceScript
        {
            public instance_blackrock_caverns_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("BRC");
                SetBossNumber(5);
                LoadObjectData(creatureData, null);
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case DataTypes.RomoggBonecrusher:
                    case DataTypes.Corla:
                    case DataTypes.KarshSteelbender:
                    case DataTypes.Beauty:
                    case DataTypes.AscendantLordObsidius:
                        break;
                    default:
                        break;
                }

                return true;
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_blackrock_caverns_InstanceMapScript(map);
        }
    }
}

