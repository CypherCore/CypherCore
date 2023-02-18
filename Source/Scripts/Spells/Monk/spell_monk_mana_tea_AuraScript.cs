// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(197908)]
public class spell_monk_mana_tea_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void OnTick(AuraEffect UnnamedParameter)
	{
		if (GetCaster())
		{
			// remove one charge per tick instead of remove aura on cast
			// "Cancelling the channel will not waste stacks"
			var manaTea = GetCaster().GetAura(MonkSpells.SPELL_MONK_MANA_TEA_STACKS);

			if (manaTea != null)
			{
				if (manaTea.GetStackAmount() > 1)
					manaTea.ModStackAmount(-1);
				else
					GetCaster().RemoveAura(MonkSpells.SPELL_MONK_MANA_TEA_STACKS);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.ModPowerCostSchoolPct));
	}
}