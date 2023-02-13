﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(263165)]
public class spell_priest_void_torrent : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnTick(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
			caster.ModifyPower(PowerType.Insanity, +600);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDamage));
	}
}