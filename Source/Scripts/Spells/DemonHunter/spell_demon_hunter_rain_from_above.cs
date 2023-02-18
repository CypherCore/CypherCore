// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206803)]
public class spell_demon_hunter_rain_from_above : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var caster = GetCaster();

		if (caster == null || !caster.ToPlayer())
			return;

		caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(caster, DemonHunterSpells.RAIN_FROM_ABOVE_SLOWFALL); }, TimeSpan.FromMilliseconds(1750));
	}
}