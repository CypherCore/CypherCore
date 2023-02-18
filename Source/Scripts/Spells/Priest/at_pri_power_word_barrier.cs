// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Priest;

[Script]
public class at_pri_power_word_barrier : AreaTriggerAI
{
	public at_pri_power_word_barrier(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (caster.IsFriendlyTo(unit))
			caster.CastSpell(unit, PriestSpells.POWER_WORD_BARRIER_BUFF, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (unit.HasAura(PriestSpells.POWER_WORD_BARRIER_BUFF, caster.GetGUID()))
			unit.RemoveAurasDueToSpell(PriestSpells.POWER_WORD_BARRIER_BUFF, caster.GetGUID());
	}
}