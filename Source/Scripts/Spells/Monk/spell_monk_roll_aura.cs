// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script] // 109131 - Roll (backward)
internal class spell_monk_roll_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		// Values need manual correction
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcMovementAmount, 0, AuraType.ModSpeedNoControl));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcMovementAmount, 2, AuraType.ModMinimumSpeed));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcImmunityAmount, 5, AuraType.MechanicImmunity));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcImmunityAmount, 6, AuraType.MechanicImmunity));

		// This is a special aura that sets backward run speed equal to forward speed
		AuraEffects.Add(new AuraEffectApplyHandler(ChangeRunBackSpeed, 4, AuraType.UseNormalMovementSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(RestoreRunBackSpeed, 4, AuraType.UseNormalMovementSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void CalcMovementAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		amount += 100;
	}

	private void CalcImmunityAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		amount -= 100;
	}

	private void ChangeRunBackSpeed(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().SetSpeed(UnitMoveType.RunBack, GetTarget().GetSpeed(UnitMoveType.Run));
	}

	private void RestoreRunBackSpeed(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().UpdateSpeed(UnitMoveType.RunBack);
	}
}