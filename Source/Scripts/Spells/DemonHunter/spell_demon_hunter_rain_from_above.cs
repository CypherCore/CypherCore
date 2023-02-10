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

		caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(caster, DemonHunterSpells.SPELL_DK_RAIN_FROM_ABOVE_SLOWFALL); }, TimeSpan.FromMilliseconds(1750));
	}
}