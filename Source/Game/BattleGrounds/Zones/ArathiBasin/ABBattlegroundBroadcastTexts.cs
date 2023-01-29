// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.BattleGrounds.Zones.ArathiBasin
{
    internal struct ABBattlegroundBroadcastTexts
    {
        public const uint ALLIANCE_NEAR_VICTORY = 10598;
        public const uint HORDE_NEAR_VICTORY = 10599;

        public static ABNodeInfo[] ABNodes =
        {
            new(ABBattlegroundNodes.NODE_STABLES, 10199, 10200, 10203, 10204, 10201, 10202, 10286, 10287), new(ABBattlegroundNodes.NODE_BLACKSMITH, 10211, 10212, 10213, 10214, 10215, 10216, 10290, 10291), new(ABBattlegroundNodes.NODE_FARM, 10217, 10218, 10219, 10220, 10221, 10222, 10288, 10289), new(ABBattlegroundNodes.NODE_LUMBER_MILL, 10224, 10225, 10226, 10227, 10228, 10229, 10284, 10285), new(ABBattlegroundNodes.NODE_GOLD_MINE, 10230, 10231, 10232, 10233, 10234, 10235, 10282, 10283)
        };
    }

}