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
    internal struct BattlegroundEYPointIconsStruct
    {
        public BattlegroundEYPointIconsStruct(uint worldStateControlIndex, uint worldStateAllianceControlledIndex, uint worldStateHordeControlledIndex, uint worldStateAllianceStatusBarIcon, uint worldStateHordeStatusBarIcon)
        {
            WorldStateControlIndex = worldStateControlIndex;
            WorldStateAllianceControlledIndex = worldStateAllianceControlledIndex;
            WorldStateHordeControlledIndex = worldStateHordeControlledIndex;
            WorldStateAllianceStatusBarIcon = worldStateAllianceStatusBarIcon;
            WorldStateHordeStatusBarIcon = worldStateHordeStatusBarIcon;
        }

        public uint WorldStateControlIndex;
        public uint WorldStateAllianceControlledIndex;
        public uint WorldStateHordeControlledIndex;
        public uint WorldStateAllianceStatusBarIcon;
        public uint WorldStateHordeStatusBarIcon;
    }
}