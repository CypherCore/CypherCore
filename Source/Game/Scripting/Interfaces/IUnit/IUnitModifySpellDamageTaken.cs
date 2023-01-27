// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IUnit
{
	public interface IUnitModifySpellDamageTaken : IScriptObject
	{
		void ModifySpellDamageTaken(Unit target, Unit attacker, ref int damage, SpellInfo spellInfo);
	}
}