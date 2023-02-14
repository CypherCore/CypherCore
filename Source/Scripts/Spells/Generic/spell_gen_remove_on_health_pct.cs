// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 62418 Impale
internal class spell_gen_remove_on_health_pct : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
	}

	private void PeriodicTick(AuraEffect aurEff)
	{
		// they apply Damage so no need to check for ticks here

		if (GetTarget().HealthAbovePct(GetEffectInfo(1).CalcValue()))
		{
			Remove(AuraRemoveMode.EnemySpell);
			PreventDefaultAction();
		}
	}
}