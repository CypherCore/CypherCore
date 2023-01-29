// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.BattleGrounds.Zones.ArathiBasin
{
    #region Consts

    internal struct ABWorldStates
    {
        public const int OCCUPIED_BASES_HORDE = 1778;
        public const int OCCUPIED_BASES_ALLY = 1779;
        public const int RESOURCES_ALLY = 1776;
        public const int RESOURCES_HORDE = 1777;
        public const int RESOURCES_MAX = 1780;
        public const int RESOURCES_WARNING = 1955;

        public const int STABLE_ICON = 1842;             // Stable Map Icon (None)
        public const int STABLE_STATE_ALIENCE = 1767;     // Stable Map State (Alience)
        public const int STABLE_STATE_HORDE = 1768;       // Stable Map State (Horde)
        public const int STABLE_STATE_CON_ALI = 1769;      // Stable Map State (Con Alience)
        public const int STABLE_STATE_CON_HOR = 1770;      // Stable Map State (Con Horde)
        public const int FARM_ICON = 1845;               // Farm Map Icon (None)
        public const int FARM_STATE_ALIENCE = 1772;       // Farm State (Alience)
        public const int FARM_STATE_HORDE = 1773;         // Farm State (Horde)
        public const int FARM_STATE_CON_ALI = 1774;        // Farm State (Con Alience)
        public const int FARM_STATE_CON_HOR = 1775;        // Farm State (Con Horde)
        public const int BLACKSMITH_ICON = 1846;         // Blacksmith Map Icon (None)
        public const int BLACKSMITH_STATE_ALIENCE = 1782; // Blacksmith Map State (Alience)
        public const int BLACKSMITH_STATE_HORDE = 1783;   // Blacksmith Map State (Horde)
        public const int BLACKSMITH_STATE_CON_ALI = 1784;  // Blacksmith Map State (Con Alience)
        public const int BLACKSMITH_STATE_CON_HOR = 1785;  // Blacksmith Map State (Con Horde)
        public const int LUMBERMILL_ICON = 1844;         // Lumber Mill Map Icon (None)
        public const int LUMBERMILL_STATE_ALIENCE = 1792; // Lumber Mill Map State (Alience)
        public const int LUMBERMILL_STATE_HORDE = 1793;   // Lumber Mill Map State (Horde)
        public const int LUMBERMILL_STATE_CON_ALI = 1794;  // Lumber Mill Map State (Con Alience)
        public const int LUMBERMILL_STATE_CON_HOR = 1795;  // Lumber Mill Map State (Con Horde)
        public const int GOLDMINE_ICON = 1843;           // Gold Mine Map Icon (None)
        public const int GOLDMINE_STATE_ALIENCE = 1787;   // Gold Mine Map State (Alience)
        public const int GOLDMINE_STATE_HORDE = 1788;     // Gold Mine Map State (Horde)
        public const int GOLDMINE_STATE_CON_ALI = 1789;    // Gold Mine Map State (Con Alience
        public const int GOLDMINE_STATE_CON_HOR = 1790;    // Gold Mine Map State (Con Horde)

        public const int HAD_500_DISADVANTAGE_ALLIANCE = 3644;
        public const int HAD_500_DISADVANTAGE_HORDE = 3645;

        public const int FARM_ICON_NEW = 8808;       // Farm Map Icon
        public const int LUMBER_MILL_ICON_NEW = 8805; // Lumber Mill Map Icon
        public const int BLACKSMITH_ICON_NEW = 8799; // Blacksmith Map Icon
        public const int GOLD_MINE_ICON_NEW = 8809;   // Gold Mine Map Icon
        public const int STABLES_ICON_NEW = 5834;    // Stable Map Icon

        public const int FARM_HORDE_CONTROL_STATE = 17328;
        public const int FARM_ALLIANCE_CONTROL_STATE = 17325;
        public const int LUMBER_MILL_HORDE_CONTROL_STATE = 17330;
        public const int LUMBER_MILL_ALLIANCE_CONTROL_STATE = 17326;
        public const int BLACKSMITH_HORDE_CONTROL_STATE = 17327;
        public const int BLACKSMITH_ALLIANCE_CONTROL_STATE = 17324;
        public const int GOLD_MINE_HORDE_CONTROL_STATE = 17329;
        public const int GOLD_MINE_ALLIANCE_CONTROL_STATE = 17323;
        public const int STABLES_HORDE_CONTROL_STATE = 17331;
        public const int STABLES_ALLIANCE_CONTROL_STATE = 17322;
    }

    #endregion
}