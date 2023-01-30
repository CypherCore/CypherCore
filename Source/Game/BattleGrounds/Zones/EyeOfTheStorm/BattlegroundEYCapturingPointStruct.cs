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
    internal struct BattlegroundEYCapturingPointStruct
    {
        public BattlegroundEYCapturingPointStruct(int _DespawnNeutralObjectType, int _SpawnObjectTypeAlliance, uint _MessageIdAlliance, int _SpawnObjectTypeHorde, uint _MessageIdHorde, uint _GraveYardId)
        {
            DespawnNeutralObjectType = _DespawnNeutralObjectType;
            SpawnObjectTypeAlliance = _SpawnObjectTypeAlliance;
            MessageIdAlliance = _MessageIdAlliance;
            SpawnObjectTypeHorde = _SpawnObjectTypeHorde;
            MessageIdHorde = _MessageIdHorde;
            GraveYardId = _GraveYardId;
        }

        public int DespawnNeutralObjectType;
        public int SpawnObjectTypeAlliance;
        public uint MessageIdAlliance;
        public int SpawnObjectTypeHorde;
        public uint MessageIdHorde;
        public uint GraveYardId;
    }
}