// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 48707 - Anti-Magic Shell
internal class spell_dk_anti_magic_shell : AuraScript, IHasAuraEffects
{
	private double absorbedAmount;
	private double absorbPct;
	private long maxHealth;

	public spell_dk_anti_magic_shell()
	{
		absorbPct      = 0;
		maxHealth      = 0;
		absorbedAmount = 0;
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.RunicPowerEnergize, DeathKnightSpells.VolatileShielding) && spellInfo.GetEffects().Count > 1;
	}

	public override bool Load()
	{
		absorbPct      = GetEffectInfo(1).CalcValue(GetCaster());
		maxHealth      = GetCaster().GetMaxHealth();
		absorbedAmount = 0;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Trigger, 0, false, AuraScriptHookType.EffectAfterAbsorb));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
	{
		amount = (int)MathFunctions.CalculatePct(maxHealth, absorbPct);
	}

	private void Trigger(AuraEffect aurEff, DamageInfo dmgInfo, ref double absorbAmount)
	{
		absorbedAmount += absorbAmount;

		if (!GetTarget().HasAura(DeathKnightSpells.VolatileShielding))
		{
			CastSpellExtraArgs args = new(aurEff);
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(absorbAmount, 2 * absorbAmount * 100 / maxHealth));
			GetTarget().CastSpell(GetTarget(), DeathKnightSpells.RunicPowerEnergize, args);
		}
	}

	private void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var volatileShielding = GetTarget().GetAuraEffect(DeathKnightSpells.VolatileShielding, 1);

		if (volatileShielding != null)
		{
			CastSpellExtraArgs args = new(volatileShielding);
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(absorbedAmount, volatileShielding.GetAmount()));
			GetTarget().CastSpell((Unit)null, DeathKnightSpells.VolatileShieldingDamage, args);
		}
	}
}