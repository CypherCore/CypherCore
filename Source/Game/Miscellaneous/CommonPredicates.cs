/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
