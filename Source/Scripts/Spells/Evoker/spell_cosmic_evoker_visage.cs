// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(351239)]
public class spell_cosmic_evoker_visage : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster.HasAura(372014))
		{
			// Dracthyr Form
			caster.RemoveAura(372014);
			caster.CastSpell(caster, 97709, true);
			caster.SendPlaySpellVisual(caster, 118328, 0, 0, 60, false);
			caster.SetDisplayId(108590);
		}
		else
		{
			// Visage Form
			if (caster.HasAura(97709))
				caster.RemoveAura(97709);

			caster.CastSpell(caster, 372014, true);
			caster.SendPlaySpellVisual(caster, 118328, 0, 0, 60, false);
			caster.SetDisplayId(104597);
		}
	}
}