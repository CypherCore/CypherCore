// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.AI
{
    internal struct ValidTargetSelectPredicate : ICheck<Unit>
	{
		private readonly UnitAI _ai;

		public ValidTargetSelectPredicate(UnitAI ai)
		{
			_ai = ai;
		}

		public bool Invoke(Unit target)
		{
			return _ai.CanAIAttack(target);
		}
	}
}