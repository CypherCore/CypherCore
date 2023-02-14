// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(203650)]
public class spell_dh_prepared : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void PeriodicTick(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.ModifyPower(PowerType.Fury, aurEff.GetAmount() / 10.0f);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.ModPowerRegen));
	}
}