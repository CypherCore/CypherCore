// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(203975)]
public class spell_druid_earthwarden_triggered : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private struct Spells
	{
		public static readonly uint EARTHWARDEN = 203974;
		public static readonly uint EARTHWARDEN_TRIGGERED = 203975;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.EARTHWARDEN, Spells.EARTHWARDEN_TRIGGERED);
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}

	private void Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, ref double absorbAmount)
	{
		if (dmgInfo.GetDamageType() == DamageEffectType.Direct)
		{
			var earthwarden = Global.SpellMgr.AssertSpellInfo(Spells.EARTHWARDEN, Difficulty.None);

			absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), earthwarden.GetEffect(0).BasePoints);
			GetCaster().RemoveAura(Spells.EARTHWARDEN_TRIGGERED);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
	}
}