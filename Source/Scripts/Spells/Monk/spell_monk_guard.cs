// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(202162)]
public class spell_monk_guard : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void CalcAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		amount = (int)(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 18);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.SchoolAbsorb));
	}
}