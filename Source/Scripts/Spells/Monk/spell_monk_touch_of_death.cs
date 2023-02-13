﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115080)]
public class spell_monk_touch_of_death : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		canBeRecalculated = true;
		var caster = GetCaster();

		if (caster != null)
		{
			var effInfo = GetAura().GetSpellInfo().GetEffect(1).CalcValue();

			if (effInfo != 0)
			{
				amount = (int)caster.CountPctFromMaxHealth(effInfo);

				aurEff.SetAmount(amount);
			}
		}
	}

	private void OnTick(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var damage = aurEff.GetAmount();

			// Damage reduced to Players, need to check reduction value
			if (GetTarget().GetTypeId() == TypeId.Player)
				damage /= 2;

			caster.CastSpell(GetTarget(), MonkSpells.SPELL_MONK_TOUCH_OF_DEATH_DAMAGE, new CastSpellExtraArgs().AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDummy));
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
	}
}