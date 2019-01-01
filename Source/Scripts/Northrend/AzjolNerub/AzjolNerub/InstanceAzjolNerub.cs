/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

namespace Scripts.Northrend.AzjolNerub.AzjolNerub
{
    struct ANDataTypes
    {
        // Encounter States/Boss Guids
        public const uint KrikthirTheGatewatcher = 0;
        public const uint Hadronox = 1;
        public const uint Anubarak = 2;

        // Additional Data
        public const uint WatcherNarjil = 3;
        public const uint WatcherGashra = 4;
        public const uint WatcherSilthik = 5;
        public const uint AnubarakWall = 6;
        public const uint AnubarakWall2 = 7;
    }

    struct ANCreatureIds
    {
        public const uint Krikthir = 28684;
        public const uint Hadronox = 28921;
        public const uint Anubarak = 29120;

        public const uint WatcherNarjil = 28729;
        public const uint WatcherGashra = 28730;
        public const uint WatcherSilthik = 28731;
    }

    struct ANGameObjectIds
    {
        public const uint KrikthirDoor = 192395;
        public const uint AnubarakDoor1 = 192396;
        public const uint AnubarakDoor2 = 192397;
        public const uint AnubarakDoor3 = 192398;
    }

    // These are passed as -action to AI's DoAction to differentiate between them and boss scripts' own actions
    struct ANInstanceMisc
    {
        public const string DataHeader = "AN";
        public const uint EncounterCount = 3;

        public const int ActionGatewatcherGreet = 1;

        public static DoorData[] doorData =
        {
            new DoorData(ANGameObjectIds.KrikthirDoor,    ANDataTypes.KrikthirTheGatewatcher, DoorType.Passage),
            new DoorData(ANGameObjectIds.AnubarakDoor1,   ANDataTypes.Anubarak,               DoorType.Room   ),
            new DoorData(ANGameObjectIds.AnubarakDoor2,   ANDataTypes.Anubarak,               DoorType.Room   ),
            new DoorData(ANGameObjectIds.AnubarakDoor3,   ANDataTypes.Anubarak,               DoorType.Room   )
        };

        public static ObjectData[] creatureData =
        {
            new ObjectData(ANCreatureIds.Krikthir,       ANDataTypes.KrikthirTheGatewatcher ),
            new ObjectData(ANCreatureIds.Hadronox,       ANDataTypes.Hadronox               ),
            new ObjectData(ANCreatureIds.Anubarak,       ANDataTypes.Anubarak               ),
            new ObjectData(ANCreatureIds.WatcherNarjil,  ANDataTypes.WatcherGashra          ),
            new ObjectData(ANCreatureIds.WatcherGashra,  ANDataTypes.WatcherSilthik         ),
            new ObjectData(ANCreatureIds.WatcherSilthik, ANDataTypes.WatcherNarjil          )
        };

        public static ObjectData[] gameobjectData =
        {
            new ObjectData(ANGameObjectIds.AnubarakDoor1, ANDataTypes.AnubarakWall),
            new ObjectData(ANGameObjectIds.AnubarakDoor3, ANDataTypes.AnubarakWall2)
        };

        public static BossBoundaryEntry[] boundaries =
        {
            new BossBoundaryEntry(ANDataTypes.KrikthirTheGatewatcher, new RectangleBoundary(400.0f, 580.0f, 623.5f, 810.0f)),
            new BossBoundaryEntry(ANDataTypes.Hadronox, new ZRangeBoundary(666.0f, 776.0f)),
            new BossBoundaryEntry(ANDataTypes.Anubarak, new CircleBoundary(new Position(550.6178f, 253.5917f), 26.0f))
        };
    }

    [Script]
    class instance_azjol_nerub : InstanceMapScript
    {
        public instance_azjol_nerub() : base(nameof(instance_azjol_nerub), 601) { }

        class instance_azjol_nerub_InstanceScript : InstanceScript
        {
            public instance_azjol_nerub_InstanceScript(InstanceMap map) : base(map)
            {
                SetHeaders(ANInstanceMisc.DataHeader);
                SetBossNumber(ANInstanceMisc.EncounterCount);
                LoadBossBoundaries(ANInstanceMisc.boundaries);
                LoadDoorData(ANInstanceMisc.doorData);
                LoadObjectData(ANInstanceMisc.creatureData, ANInstanceMisc.gameobjectData);
            }

            public override void OnUnitDeath(Unit who)
            {
                base.OnUnitDeath(who);
                Creature creature = who.ToCreature();
                if (!creature || creature.IsCritter() || creature.IsControlledByPlayer())
                    return;

                Creature gatewatcher = GetCreature(ANDataTypes.KrikthirTheGatewatcher);
                if (gatewatcher)
                    gatewatcher.GetAI().DoAction(-ANInstanceMisc.ActionGatewatcherGreet);
            }

            public override bool CheckRequiredBosses(uint bossId, Player player)
            {
                if (_SkipCheckRequiredBosses(player))
                    return true;

                if (bossId > ANDataTypes.KrikthirTheGatewatcher && GetBossState(ANDataTypes.KrikthirTheGatewatcher) != EncounterState.Done)
                    return false;

                return true;
            }
    }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_azjol_nerub_InstanceScript(map);
        }
    }
}
