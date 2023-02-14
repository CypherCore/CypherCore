// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	// 202430 - Nature's Balance
	[SpellScript(202430)]
	public class spell_dru_natures_balance : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private struct Spells
		{
			public const uint SPELL_DRUID_NATURES_BALANCE = 202430;
		}

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(Spells.SPELL_DRUID_NATURES_BALANCE);
		}

		private void HandlePeriodic(AuraEffect aurEff)
		{
			var caster = GetCaster();

			if (caster == null || !caster.IsAlive() || caster.GetMaxPower(PowerType.LunarPower) == 0)
				return;

			if (caster.IsInCombat())
			{
				var amount = Math.Max(caster.GetAuraEffect(Spells.SPELL_DRUID_NATURES_BALANCE, 0).GetAmount(), 0);

				// don't regen when permanent aura target has full power
				if (caster.GetPower(PowerType.LunarPower) == caster.GetMaxPower(PowerType.LunarPower))
					return;

				caster.ModifyPower(PowerType.LunarPower, amount);
			}
			else
			{
				if (caster.GetPower(PowerType.LunarPower) > 500)
					return;

				caster.SetPower(PowerType.LunarPower, 500);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicEnergize));
		}
	}
}