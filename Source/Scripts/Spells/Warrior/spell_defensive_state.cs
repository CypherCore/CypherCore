// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//197690
	[SpellScript(197690)]
	public class spell_defensive_state : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void OnApply(AuraEffect aura, AuraEffectHandleModes auraMode)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var defensiveState = caster?.GetAura(197690)?.GetEffect(0);

				if (defensiveState != null)
					defensiveState.GetAmount();
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));
		}
	}
}