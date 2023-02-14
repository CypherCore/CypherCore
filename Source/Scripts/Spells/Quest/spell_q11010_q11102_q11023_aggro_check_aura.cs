// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 40113 Knockdown Fel Cannon: The Aggro Check Aura
internal class spell_q11010_q11102_q11023_aggro_check_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTriggerSpell, 0, AuraType.PeriodicTriggerSpell));
	}

	private void HandleTriggerSpell(AuraEffect aurEff)
	{
		var target = GetTarget();

		if (target)
			// On trigger proccing
			target.CastSpell(target, QuestSpellIds.AggroCheck);
	}
}