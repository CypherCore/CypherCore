﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(212084)]
public class spell_dh_fel_devastation : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void PeriodicTick(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (aurEff.GetTickNumber() == 1)
			return;

		caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_FEL_DEVASTATION_DAMAGE, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
	}
}