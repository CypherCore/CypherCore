﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_artifical_damage : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1;
	}

	public override bool Load()
	{
		return GetOwner().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModDamagePercentDone));
	}

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		var artifact = GetOwner().ToPlayer().GetItemByGuid(GetAura().GetCastItemGUID());

		if (artifact)
			amount = (int)(GetSpellInfo().GetEffect(1).BasePoints * artifact.GetTotalPurchasedArtifactPowers() / 100);
	}
}