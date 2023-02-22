// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(190319)]
public class spell_mage_combustion : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void CalcAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (!caster.IsPlayer())
			return;

		var crit = caster.ToPlayer().GetRatingBonusValue(CombatRating.CritSpell);
		amount += (int)crit;
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		GetCaster().RemoveAura(MageSpells.INFERNO);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 1, AuraType.ModRating));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.ModRating, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}