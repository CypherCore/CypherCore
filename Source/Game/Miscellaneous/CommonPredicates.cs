// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Miscellaneous
{
	/// Only returns true for the given attacker's current victim, if any
	public class IsVictimOf : ICheck<WorldObject>
	{
		private WorldObject _victim;

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