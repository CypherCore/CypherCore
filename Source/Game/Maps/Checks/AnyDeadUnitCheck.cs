// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class AnyDeadUnitCheck : ICheck<Unit>
    {
        public bool Invoke(Unit u)
        {
            return !u.IsAlive();
        }
    }
}