using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207289)]
public class spell_dk_unholy_frenzy : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit target = GetTarget();
		Unit caster = GetCaster();
		if (target == null || caster == null)
		{
			return;
		}

		caster.m_Events.AddRepeatEventAtOffset(() =>
		                                       {
			                                       if (target == null || caster == null)
			                                       {
				                                       return default;
			                                       }
			                                       if (target.HasAura(156004))
			                                       {
				                                       caster.CastSpell(target, DeathKnightSpells.SPELL_DK_FESTERING_WOUND_DAMAGE, true);
			                                       }
			                                       if (caster.HasAura(156004))
			                                       {
				                                       return TimeSpan.FromSeconds(2);
			                                       }

			                                       return default;
		                                       }, TimeSpan.FromMilliseconds(100));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.MeleeSlow, AuraEffectHandleModes.Real));
	}
}