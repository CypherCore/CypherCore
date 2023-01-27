// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IUnit
{
	public interface IUnitOnDamage : IScriptObject
	{
		void OnDamage(Unit attacker, Unit victim, ref uint damage);
	}
}