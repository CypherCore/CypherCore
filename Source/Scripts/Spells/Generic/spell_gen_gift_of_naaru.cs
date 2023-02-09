using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_gift_of_naaru : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicHeal));
	}

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		if (!GetCaster() ||
		    aurEff.GetTotalTicks() == 0)
			return;

		var healPct  = GetEffectInfo(1).CalcValue() / 100.0f;
		var heal     = healPct * GetCaster().GetMaxHealth();
		var healTick = (int)Math.Floor(heal / aurEff.GetTotalTicks());
		amount += healTick;
	}
}