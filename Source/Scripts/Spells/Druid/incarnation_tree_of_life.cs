// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(33891)]
public class incarnation_tree_of_life : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var caster = GetCaster();
		var tree   = caster.GetAura(33891);

		if (tree != null)
			tree.SetDuration(30000, true);
	}
}