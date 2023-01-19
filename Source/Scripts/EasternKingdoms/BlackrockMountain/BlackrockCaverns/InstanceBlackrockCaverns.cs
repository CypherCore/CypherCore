// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

        static DungeonEncounterData[] encounters =
        {
            new DungeonEncounterData(DataTypes.RomoggBonecrusher, 1040),
            new DungeonEncounterData(DataTypes.Corla, 1038),
            new DungeonEncounterData(DataTypes.KarshSteelbender, 1039),
            new DungeonEncounterData(DataTypes.Beauty, 1037),
            new DungeonEncounterData(DataTypes.AscendantLordObsidius, 1036)
        };

        public instance_blackrock_caverns() : base(nameof(instance_blackrock_caverns), 645) { }

        class instance_blackrock_caverns_InstanceMapScript : InstanceScript
        {
            public instance_blackrock_caverns_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("BRC");
                SetBossNumber(5);
                LoadObjectData(creatureData, null);
                LoadDungeonEncounterData(encounters);
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

