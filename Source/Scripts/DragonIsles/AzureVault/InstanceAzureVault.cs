// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.DragonIsles.AzureVault
{
    struct DataTypes
    {
        // Encounters
        public const uint Leymor = 0;
        public const uint Azureblade = 1;
        public const uint TelashGreywing = 2;
        public const uint Umbrelskul = 3;

        public const uint LeymorIntroDone = 4;
    }

    struct CreatureIds
    {
        // Bosses
        public const uint BossLeymor = 186644;
        public const uint BossAzureblade = 186739;
        public const uint BossTelashGreywing = 199614;
        public const uint BossUmbrelskul = 186738;

        // Leymor
        public const uint ArcaneTender = 191164;
    }

    struct GameObjectIds
    {
        public const uint ArcaneVaultsDoorLeymorEntrance = 380536;
        public const uint ArcaneVaultsDoorLeymorExit = 377951;
    }

    [Script]
    class instance_azure_vault : InstanceMapScript
    {
        static BossBoundaryEntry[] boundaries =
        {
            new(DataTypes.Leymor, new CircleBoundary(new Position(-5129.39f, 1253.30f), 75.0f))
        };

        static ObjectData[] creatureData =
        {
            new(CreatureIds.BossLeymor, DataTypes.Leymor),
            new(CreatureIds.BossAzureblade, DataTypes.Azureblade),
            new(CreatureIds.BossTelashGreywing, DataTypes.TelashGreywing),
            new(CreatureIds.BossUmbrelskul, DataTypes.Umbrelskul),
        };

        static DoorData[] doorData =
        {
            new(GameObjectIds.ArcaneVaultsDoorLeymorEntrance, DataTypes.Leymor, DoorType.Room),
            new(GameObjectIds.ArcaneVaultsDoorLeymorExit, DataTypes.Leymor, DoorType.Passage),
        };

        static DungeonEncounterData[] encounters =
        {
            new(DataTypes.Leymor, 2582),
            new(DataTypes.Azureblade, 2585),
            new(DataTypes.TelashGreywing, 2583),
            new(DataTypes.Umbrelskul, 2584)
        };

        public instance_azure_vault() : base(nameof(instance_azure_vault), 2515) { }

        class instance_azure_vault_InstanceMapScript : InstanceScript
        {
            public instance_azure_vault_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("AzureVault");
                SetBossNumber(4);
                LoadObjectData(creatureData, null);
                LoadDoorData(doorData);
                LoadBossBoundaries(boundaries);
                LoadDungeonEncounterData(encounters);

                _leymorIntroDone = false;
            }

            public override uint GetData(uint dataId)
            {
                switch (dataId)
                {
                    case DataTypes.LeymorIntroDone:
                        return _leymorIntroDone ? 1 : 0u;
                    default:
                        break;
                }
                return 0;
            }

            public override void SetData(uint dataId, uint value)
            {
                switch (dataId)
                {
                    case DataTypes.LeymorIntroDone:
                        _leymorIntroDone = true; // no need to pass value, it will never reset to false
                        break;
                    default:
                        break;
                }
            }


            bool _leymorIntroDone;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_azure_vault_InstanceMapScript(map);
        }
    }
}