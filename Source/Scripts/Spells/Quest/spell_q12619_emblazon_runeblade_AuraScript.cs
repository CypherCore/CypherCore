// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 51769 - Emblazon Runeblade
internal class spell_q12619_emblazon_runeblade_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		PreventDefaultAction();
		var caster = GetCaster();

		if (caster)
			caster.CastSpell(caster, aurEff.GetSpellEffectInfo().TriggerSpell, new CastSpellExtraArgs(aurEff));
	}
}