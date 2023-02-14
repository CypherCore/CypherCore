// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115288)]
public class spell_monk_energizing_brew : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (!GetCaster().IsInCombat())
			return SpellCastResult.CasterAurastate;

		return SpellCastResult.SpellCastOk;
	}
}