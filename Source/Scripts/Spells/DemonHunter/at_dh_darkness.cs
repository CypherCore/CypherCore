// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_dh_darkness : AreaTriggerAI
{
	public at_dh_darkness(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	private bool entered;

	public override void OnInitialize()
	{
		at.SetDuration(8000);
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (caster.IsFriendlyTo(unit) && !unit.HasAura(DemonHunterSpells.DARKNESS_ABSORB))
		{
			entered = true;

			if (entered)
			{
				caster.CastSpell(unit, DemonHunterSpells.DARKNESS_ABSORB, true);
				entered = false;
			}
		}
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (unit.HasAura(DemonHunterSpells.DARKNESS_ABSORB))
			unit.RemoveAurasDueToSpell(DemonHunterSpells.DARKNESS_ABSORB, caster.GetGUID());
	}
}