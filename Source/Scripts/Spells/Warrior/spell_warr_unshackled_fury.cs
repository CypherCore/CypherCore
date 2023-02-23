// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// Unshackled Fury - 76856
	[SpellScript(76856)]
	public class spell_warr_unshackled_fury : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				if (!caster.HasAuraState(AuraStateType.Enraged))
					amount = 0;
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.AddPctModifier));
		}
	}
}