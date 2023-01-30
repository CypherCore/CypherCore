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
    internal struct BattlegroundEYLosingPointStruct
    {
        public BattlegroundEYLosingPointStruct(int _SpawnNeutralObjectType, int _DespawnObjectTypeAlliance, uint _MessageIdAlliance, int _DespawnObjectTypeHorde, uint _MessageIdHorde)
        {
            SpawnNeutralObjectType = _SpawnNeutralObjectType;
            DespawnObjectTypeAlliance = _DespawnObjectTypeAlliance;
            MessageIdAlliance = _MessageIdAlliance;
            DespawnObjectTypeHorde = _DespawnObjectTypeHorde;
            MessageIdHorde = _MessageIdHorde;
        }

        public int SpawnNeutralObjectType;
        public int DespawnObjectTypeAlliance;
        public uint MessageIdAlliance;
        public int DespawnObjectTypeHorde;
        public uint MessageIdHorde;
    }
}