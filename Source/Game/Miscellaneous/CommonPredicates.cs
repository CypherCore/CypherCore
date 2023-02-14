// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Miscellaneous
{
    /// Only returns true for the given attacker's current victim, if any
    public class IsVictimOf : ICheck<WorldObject>
    {
        WorldObject _victim;

        public IsVictimOf(Unit attacker) 
        {
            _victim = attacker?.GetVictim();
        }

        public bool Invoke(WorldObject obj)
        {
            return obj != null && (_victim == obj);
        }
    }
}
