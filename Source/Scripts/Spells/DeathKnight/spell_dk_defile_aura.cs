using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(156004)]
public class spell_dk_defile_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var target = GetTarget();
		var caster = GetCaster();

		if (target == null || caster == null)
			return;

		var oneSec = TimeSpan.FromSeconds(1);

		caster.m_Events.AddRepeatEventAtOffset(() =>
		                                       {
			                                       if (target == null || caster == null)
				                                       return default;

			                                       caster.CastSpell(target, DeathKnightSpells.SPELL_DK_DEFILE_DAMAGE, true);

			                                       if (target.HasAura(156004) && caster != null)
				                                       return oneSec;

			                                       return default;
		                                       },
		                                       oneSec);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
	}
}