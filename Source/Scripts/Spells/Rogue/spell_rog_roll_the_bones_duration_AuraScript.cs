using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(new uint[] { 199603, 193358, 193357, 193359, 199600, 193356 })]
public class spell_rog_roll_the_bones_duration_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		Aura rtb = caster.GetAura(RogueSpells.SPELL_ROGUE_ROLL_THE_BONES);
		if (rtb == null)
		{
			caster.RemoveAurasDueToSpell(GetSpellInfo().Id); //sometimes it remains on the caster after relog incorrectly.
			return;
		}

		Aura aur = caster.GetAura(GetSpellInfo().Id);
		if (aur != null)
		{
			aur.SetMaxDuration(rtb.GetDuration());
			aur.SetDuration(rtb.GetDuration());
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Any, AuraEffectHandleModes.Real));
	}
}