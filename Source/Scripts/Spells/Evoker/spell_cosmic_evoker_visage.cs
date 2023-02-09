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
			caster.RemoveAurasDueToSpell(372014);
			caster.CastSpell(caster, 97709, true);
			caster.SendPlaySpellVisual(caster, 118328, 0, 0, 60, false);
			caster.SetDisplayId(108590);
		}
		else
		{
			// Visage Form
			if (caster.HasAura(97709))
				caster.RemoveAurasDueToSpell(97709);

			caster.CastSpell(caster, 372014, true);
			caster.SendPlaySpellVisual(caster, 118328, 0, 0, 60, false);
			caster.SetDisplayId(104597);
		}
	}
}