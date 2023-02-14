// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 59262 Grievous Wound
internal class spell_gen_remove_on_full_health : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
	}

	private void PeriodicTick(AuraEffect aurEff)
	{
		// if it has only periodic effect, allow 1 tick
		var onlyEffect = GetSpellInfo().GetEffects().Count == 1;

		if (onlyEffect && aurEff.GetTickNumber() <= 1)
			return;

		if (GetTarget().IsFullHealth())
		{
			Remove(AuraRemoveMode.EnemySpell);
			PreventDefaultAction();
		}
	}
}