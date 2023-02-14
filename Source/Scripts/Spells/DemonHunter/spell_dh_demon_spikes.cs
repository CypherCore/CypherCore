// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(203720)]
public class spell_dh_demon_spikes : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();
		caster.CastSpell(203819, true);
	}
}