// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 27746 -  Nitrous Boost
internal class spell_gen_nitrous_boost : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
	}

	private void PeriodicTick(AuraEffect aurEff)
	{
		PreventDefaultAction();

		if (GetCaster() != null &&
		    GetTarget().GetPower(PowerType.Mana) >= 10)
			GetTarget().ModifyPower(PowerType.Mana, -10);
		else
			Remove();
	}
}