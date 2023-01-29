/*
 * Copyright (C) 2012-2016 CypherCore <http://github.com/CypherCore>
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

namespace Game.BattleGrounds.Zones.EyeOfTheStorm
{
    internal struct EotSWorldStateIds
    {
        public const uint ALLIANCE_RESOURCES = 1776;
        public const uint HORDE_RESOURCES = 1777;
        public const uint MAX_RESOURCES = 1780;
        public const uint ALLIANCE_BASE = 2752;
        public const uint HORDE_BASE = 2753;
        public const uint DRAENEI_RUINS_HORDE_CONTROL = 2733;
        public const uint DRAENEI_RUINS_ALLIANCE_CONTROL = 2732;
        public const uint DRAENEI_RUINS_UNCONTROL = 2731;
        public const uint MAGE_TOWER_ALLIANCE_CONTROL = 2730;
        public const uint MAGE_TOWER_HORDE_CONTROL = 2729;
        public const uint MAGE_TOWER_UNCONTROL = 2728;
        public const uint FEL_REAVER_HORDE_CONTROL = 2727;
        public const uint FEL_REAVER_ALLIANCE_CONTROL = 2726;
        public const uint FEL_REAVER_UNCONTROL = 2725;
        public const uint BLOOD_ELF_HORDE_CONTROL = 2724;
        public const uint BLOOD_ELF_ALLIANCE_CONTROL = 2723;
        public const uint BLOOD_ELF_UNCONTROL = 2722;
        public const uint PROGRESS_BAR_PERCENT_GREY = 2720; //100 = Empty (Only Grey); 0 = Blue|Red (No Grey)
        public const uint PROGRESS_BAR_STATUS = 2719;      //50 Init!; 48 ... Hordak Bere .. 33 .. 0 = Full 100% Hordacky; 100 = Full Alliance
        public const uint PROGRESS_BAR_SHOW = 2718;        //1 Init; 0 Druhy Send - Bez Messagu; 1 = Controlled Aliance

        public const uint NETHERSTORM_FLAG = 8863;

        //Set To 2 When Flag Is Picked Up; And To 1 If It Is Dropped
        public const uint NETHERSTORM_FLAG_STATE_ALLIANCE = 9808;
        public const uint NETHERSTORM_FLAG_STATE_HORDE = 9809;

        public const uint DRAENEI_RUINS_HORDE_CONTROL_STATE = 17362;
        public const uint DRAENEI_RUINS_ALLIANCE_CONTROL_STATE = 17366;
        public const uint MAGE_TOWER_HORDE_CONTROL_STATE = 17361;
        public const uint MAGE_TOWER_ALLIANCE_CONTROL_STATE = 17368;
        public const uint FEL_REAVER_HORDE_CONTROL_STATE = 17364;
        public const uint FEL_REAVER_ALLIANCE_CONTROL_STATE = 17367;
        public const uint BLOOD_ELF_HORDE_CONTROL_STATE = 17363;
        public const uint BLOOD_ELF_ALLIANCE_CONTROL_STATE = 17365;
    }
}