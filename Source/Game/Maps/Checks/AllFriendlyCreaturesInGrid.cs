// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    internal class AllFriendlyCreaturesInGrid : ICheck<Unit>
    {
        private readonly Unit unit;

        public AllFriendlyCreaturesInGrid(Unit obj)
        {
            unit = obj;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() &&
                u.IsVisible() &&
                u.IsFriendlyTo(unit))
                return true;

            return false;
        }
    }
}