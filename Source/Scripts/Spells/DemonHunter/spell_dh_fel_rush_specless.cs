// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(344865)]
public class spell_dh_fel_rush_specless : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		caster.CastSpell(DemonHunterSpells.FEL_RUSH, true);
	}
}