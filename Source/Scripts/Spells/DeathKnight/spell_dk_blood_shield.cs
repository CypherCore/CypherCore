// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

/// Blood Shield - 77535
[SpellScript(77535)]
public class spell_dk_blood_shield : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private struct eSpells
	{
		public const uint T17Blood4P = 165571;
	}

	private void AfterAbsorb(AuraEffect p_AurEff, DamageInfo UnnamedParameter, ref double p_AbsorbAmount)
	{
		var l_Target = GetTarget();

		if (l_Target != null)
		{
			/// While Vampiric Blood is active, your Blood Shield cannot be reduced below 3% of your maximum health.
			var l_AurEff = l_Target.GetAuraEffect(eSpells.T17Blood4P, 0);

			if (l_AurEff != null)
			{
				var l_FutureAbsorb = Convert.ToInt32(p_AurEff.GetAmount() - p_AbsorbAmount);
				var l_MinimaAbsorb = Convert.ToInt32(l_Target.CountPctFromMaxHealth(l_AurEff.GetAmount()));

				/// We need to add some absorb amount to correct the absorb amount after that, and set it to 3% of max health
				if (l_FutureAbsorb < l_MinimaAbsorb)
				{
					var l_AddedAbsorb = l_MinimaAbsorb - l_FutureAbsorb;
					p_AurEff.ChangeAmount(p_AurEff.GetAmount() + l_AddedAbsorb);
				}
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectAbsorbHandler(AfterAbsorb, 0));
	}
}