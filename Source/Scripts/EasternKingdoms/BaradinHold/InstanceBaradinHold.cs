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
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BaradinHold
{
    struct DataTypes
    {
        public const uint Argaloth = 0;
        public const uint Occuthar = 1;
        public const uint Alizabal = 2;
    }

    struct CreatureIds
    {
        public const uint EyeOfOccuthar = 52389;
        public const uint FocusFireDummy = 52369;
        public const uint OccutharEye = 52368;
    }

    struct BossIds
    {
        public const uint Argaloth = 47120;
        public const uint Occuthar = 52363;
        public const uint Alizabal = 55869;
    }

    struct GameObjectIds
    {
        public const uint ArgalothDoor = 207619;
        public const uint OccutharDoor = 208953;
        public const uint AlizabalDoor = 209849;
    }

    [Script]
    class instance_baradin_hold : InstanceMapScript
    {
        static DoorData[] doorData =
        {
            new DoorData(GameObjectIds.ArgalothDoor,  DataTypes.Argaloth, DoorType.Room),
            new DoorData(GameObjectIds.OccutharDoor,  DataTypes.Occuthar, DoorType.Room),
            new DoorData(GameObjectIds.AlizabalDoor,  DataTypes.Alizabal, DoorType.Room),
        };

        static DungeonEncounterData[] encounters =
        {
            new DungeonEncounterData(DataTypes.Argaloth, 1033),
            new DungeonEncounterData(DataTypes.Occuthar, 1250),
            new DungeonEncounterData(DataTypes.Alizabal, 1332)
        };

        public instance_baradin_hold() : base(nameof(instance_baradin_hold), 757) { }

        class instance_baradin_hold_InstanceMapScript : InstanceScript
        {
            ObjectGuid ArgalothGUID;
            ObjectGuid OccutharGUID;
            ObjectGuid AlizabalGUID;

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

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_baradin_hold_InstanceMapScript(map);
        }
    }
}

