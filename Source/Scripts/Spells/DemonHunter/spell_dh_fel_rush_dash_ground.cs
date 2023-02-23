// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(197922)]
public class spell_dh_fel_rush_dash_ground : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster != null)
			caster.SetDisableGravity(false);
	}

	private void CalcSpeed(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		amount = 1250;
		RefreshDuration();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 1, AuraType.ModSpeedNoControl));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 3, AuraType.ModMinimumSpeed));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 6, AuraType.ModMinimumSpeedRate, AuraEffectHandleModes.SendForClientMask, AuraScriptHookType.EffectAfterRemove));
	}
}