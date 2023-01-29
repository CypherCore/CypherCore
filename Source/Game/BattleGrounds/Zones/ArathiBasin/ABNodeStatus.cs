// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.BattleGrounds.Zones.ArathiBasin
{
    internal enum ABNodeStatus
    {
        Neutral = 0,
        Contested = 1,
        AllyContested = 1,
        HordeContested = 2,
        Occupied = 3,
        AllyOccupied = 3,
        HordeOccupied = 4
    }
}