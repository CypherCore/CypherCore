// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_wg_water : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (!GetSpellInfo().CheckTargetCreatureType(GetCaster()))
			return SpellCastResult.DontReport;

		return SpellCastResult.SpellCastOk;
	}
}