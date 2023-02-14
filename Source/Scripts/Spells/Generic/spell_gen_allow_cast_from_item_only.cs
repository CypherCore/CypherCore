// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_allow_cast_from_item_only : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (!GetCastItem())
			return SpellCastResult.CantDoThatRightNow;

		return SpellCastResult.SpellCastOk;
	}
}