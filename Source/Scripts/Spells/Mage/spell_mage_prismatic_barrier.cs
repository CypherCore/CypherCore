using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 235450 - Prismatic Barrier
internal class spell_mage_prismatic_barrier : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
	}

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		canBeRecalculated = false;
		Unit caster = GetCaster();

		if (caster)
			amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 7.0f);
	}
}