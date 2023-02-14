// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_chi_burst : AreaTriggerAI
{
	public at_monk_chi_burst(AreaTrigger at) : base(at)
	{
	}

	public override void OnUnitEnter(Unit target)
	{
		if (!at.GetCaster())
			return;

		if (at.GetCaster().IsValidAssistTarget(target))
			at.GetCaster().CastSpell(target, MonkSpells.SPELL_MONK_CHI_BURST_HEAL, true);

		if (at.GetCaster().IsValidAttackTarget(target))
			at.GetCaster().CastSpell(target, MonkSpells.SPELL_MONK_CHI_BURST_DAMAGE, true);
	}
}