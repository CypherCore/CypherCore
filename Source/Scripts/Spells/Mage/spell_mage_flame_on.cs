// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

internal class spell_mage_flame_on : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.FireBlast) && CliDB.SpellCategoryStorage.HasRecord(Global.SpellMgr.GetSpellInfo(MageSpells.FireBlast, Difficulty.None).ChargeCategoryId) && spellInfo.GetEffects().Count > 2;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.ChargeRecoveryMultiplier));
	}

	private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		canBeRecalculated = false;
		amount = -MathFunctions.GetPctOf(GetEffectInfo(2).CalcValue() * Time.InMilliseconds, CliDB.SpellCategoryStorage.LookupByKey(Global.SpellMgr.GetSpellInfo(MageSpells.FireBlast, Difficulty.None).ChargeCategoryId).ChargeRecoveryTime);
	}
}