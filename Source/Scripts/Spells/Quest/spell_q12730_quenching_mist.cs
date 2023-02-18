// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 53350 - Quenching Mist
internal class spell_q12730_quenching_mist : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(QuestSpellIds.FlickeringFlames);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicHeal));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		GetTarget().RemoveAura(QuestSpellIds.FlickeringFlames);
	}
}