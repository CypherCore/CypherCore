// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class PlayerAtMinimumRangeAway : ICheck<Player>
    {
        private readonly float _fRange;

        private readonly Unit _unit;

        public PlayerAtMinimumRangeAway(Unit _unit, float fMinRange)
        {
            this._unit = _unit;
            _fRange = fMinRange;
        }

        public bool Invoke(Player player)
        {
            //No threat list check, must be done explicit if expected to be in combat with creature
            if (!player.IsGameMaster() &&
                player.IsAlive() &&
                !_unit.IsWithinDist(player, _fRange, false))
                return true;

            return false;
        }
    }
}