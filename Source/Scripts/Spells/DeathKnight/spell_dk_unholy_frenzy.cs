// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207289)]
public class spell_dk_unholy_frenzy : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var target = GetTarget();
		var caster = GetCaster();

		if (target == null || caster == null)
			return;

		caster.m_Events.AddRepeatEventAtOffset(() =>
		                                       {
			                                       if (target == null || caster == null)
				                                       return default;

			                                       if (target.HasAura(156004))
				                                       caster.CastSpell(target, DeathKnightSpells.FESTERING_WOUND_DAMAGE, true);

			                                       if (caster.HasAura(156004))
				                                       return TimeSpan.FromSeconds(2);

			                                       return default;
		                                       },
		                                       TimeSpan.FromMilliseconds(100));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.MeleeSlow, AuraEffectHandleModes.Real));
	}
}