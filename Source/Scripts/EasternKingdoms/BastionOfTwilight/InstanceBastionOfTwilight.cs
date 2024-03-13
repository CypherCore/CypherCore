// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BastionOfTwilight
{
    enum DataTypes
    {
        // Encounters
        HalfusWyrmbreaker = 0,
        TheralionAndValiona = 1,
        AscendantCouncil = 2,
        Chogall = 3,
        Sinestra = 4
    }

    enum CreatureIds
    {
        // Bosses
        HalfusWyrmbreaker = 44600,
        Theralion = 45993,
        Valiona = 45992,
        Ignacious = 43686,
        Feludius = 43687,
        Terrastra = 43689,
        Arion = 43688,
        ElementiumMonstrosity = 43735,
        Chogall = 43324,
        Sinestra = 45213
    }

    enum GameobjectIds
    {
        HalfusEntrance = 205222,
        HalfusExit = 205223,
        DragonSiblingsDoorEntrance = 205224,
        DragonSiblingsDoorExit = 205225,
        AscendantCouncilEntrance = 205226,
        AscendantCouncilExit = 205227,
        ChogallEntrance = 205228,
        GrimBatolRaidTrapDoor = 205898
    }

    [Script]
    class instance_bastion_of_twilight : InstanceMapScript
    {
        static ObjectData[] creatureData =
        {
            new ObjectData((uint)CreatureIds.HalfusWyrmbreaker, (uint)DataTypes.HalfusWyrmbreaker),
            new ObjectData((uint)CreatureIds.Chogall, (uint)DataTypes.Chogall),
            new ObjectData((uint)CreatureIds.Sinestra, (uint)DataTypes.Sinestra),
        };

        static DoorData[] doorData =
        {
            new DoorData((uint)GameobjectIds.HalfusEntrance, (uint)DataTypes.HalfusWyrmbreaker, EncounterDoorBehavior.OpenWhenNotInProgress),
            new DoorData((uint)GameobjectIds.HalfusExit, (uint)DataTypes.HalfusWyrmbreaker, EncounterDoorBehavior.OpenWhenDone),
            new DoorData((uint)GameobjectIds.DragonSiblingsDoorEntrance, (uint)DataTypes.TheralionAndValiona, EncounterDoorBehavior.OpenWhenNotInProgress),
            new DoorData((uint)GameobjectIds.DragonSiblingsDoorExit, (uint)DataTypes.TheralionAndValiona, EncounterDoorBehavior.OpenWhenDone),
            new DoorData((uint)GameobjectIds.AscendantCouncilEntrance, (uint)DataTypes.AscendantCouncil, EncounterDoorBehavior.OpenWhenNotInProgress),
            new DoorData((uint)GameobjectIds.AscendantCouncilExit, (uint)DataTypes.AscendantCouncil, EncounterDoorBehavior.OpenWhenDone),
            new DoorData((uint)GameobjectIds.ChogallEntrance, (uint)DataTypes.Chogall, EncounterDoorBehavior.OpenWhenNotInProgress),
        };

        static DungeonEncounterData[] encounters =
        {
            new DungeonEncounterData((uint)DataTypes.HalfusWyrmbreaker, 1030),
            new DungeonEncounterData((uint)DataTypes.TheralionAndValiona, 1032),
            new DungeonEncounterData((uint)DataTypes.AscendantCouncil, 1028),
            new DungeonEncounterData((uint)DataTypes.Chogall, 1029),
            new DungeonEncounterData((uint)DataTypes.Sinestra, 1082, 1083)
        };

        public instance_bastion_of_twilight() : base(nameof(instance_bastion_of_twilight), 671) { }

        class instance_bastion_of_twilight_InstanceMapScript : InstanceScript
        {
            public instance_bastion_of_twilight_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("BOT");
                SetBossNumber(5);
                LoadObjectData(creatureData, null);
                LoadDoorData(doorData);
                LoadDungeonEncounterData(encounters);
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_bastion_of_twilight_InstanceMapScript(map);
        }
    }
}
