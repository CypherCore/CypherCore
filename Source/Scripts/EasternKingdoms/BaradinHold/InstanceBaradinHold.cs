// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.IMap;

namespace Scripts.EasternKingdoms.BaradinHold
{
    internal struct DataTypes
    {
        public const uint Argaloth = 0;
        public const uint Occuthar = 1;
        public const uint Alizabal = 2;
    }

    internal struct CreatureIds
    {
        public const uint EyeOfOccuthar = 52389;
        public const uint FocusFireDummy = 52369;
        public const uint OccutharEye = 52368;
    }

    internal struct BossIds
    {
        public const uint Argaloth = 47120;
        public const uint Occuthar = 52363;
        public const uint Alizabal = 55869;
    }

    internal struct GameObjectIds
    {
        public const uint ArgalothDoor = 207619;
        public const uint OccutharDoor = 208953;
        public const uint AlizabalDoor = 209849;
    }

    [Script]
    internal class instance_baradin_hold : InstanceMapScript, IInstanceMapGetInstanceScript
    {
        private class instance_baradin_hold_InstanceMapScript : InstanceScript
        {
            private ObjectGuid AlizabalGUID;
            private ObjectGuid ArgalothGUID;
            private ObjectGuid OccutharGUID;

            public instance_baradin_hold_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("BH");
                SetBossNumber(3);
                LoadDoorData(doorData);
                LoadDungeonEncounterData(encounters);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case BossIds.Argaloth:
                        ArgalothGUID = creature.GetGUID();

                        break;
                    case BossIds.Occuthar:
                        OccutharGUID = creature.GetGUID();

                        break;
                    case BossIds.Alizabal:
                        AlizabalGUID = creature.GetGUID();

                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.ArgalothDoor:
                    case GameObjectIds.OccutharDoor:
                    case GameObjectIds.AlizabalDoor:
                        AddDoor(go, true);

                        break;
                }
            }

            public override ObjectGuid GetGuidData(uint data)
            {
                switch (data)
                {
                    case DataTypes.Argaloth:
                        return ArgalothGUID;
                    case DataTypes.Occuthar:
                        return OccutharGUID;
                    case DataTypes.Alizabal:
                        return AlizabalGUID;
                    default:
                        break;
                }

                return ObjectGuid.Empty;
            }

            public override void OnGameObjectRemove(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.ArgalothDoor:
                    case GameObjectIds.OccutharDoor:
                    case GameObjectIds.AlizabalDoor:
                        AddDoor(go, false);

                        break;
                }
            }
        }

        private static readonly DoorData[] doorData =
        {
            new(GameObjectIds.ArgalothDoor, DataTypes.Argaloth, DoorType.Room), new(GameObjectIds.OccutharDoor, DataTypes.Occuthar, DoorType.Room), new(GameObjectIds.AlizabalDoor, DataTypes.Alizabal, DoorType.Room)
        };

        private static readonly DungeonEncounterData[] encounters =
        {
            new(DataTypes.Argaloth, 1033), new(DataTypes.Occuthar, 1250), new(DataTypes.Alizabal, 1332)
        };

        public instance_baradin_hold() : base(nameof(instance_baradin_hold), 757)
        {
        }

        public InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_baradin_hold_InstanceMapScript(map);
        }
    }
}