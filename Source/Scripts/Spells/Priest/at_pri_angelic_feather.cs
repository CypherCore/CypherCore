// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Priest;

[Script]
public class at_pri_angelic_feather : AreaTriggerAI
{
	public at_pri_angelic_feather(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnInitialize()
	{
		var caster = at.GetCaster();

		if (caster != null)
		{
			var areaTriggers = caster.GetAreaTriggers(PriestSpells.ANGELIC_FEATHER_AREATRIGGER);

			if (areaTriggers.Count >= 3)
				areaTriggers[0].SetDuration(0);
		}
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster != null)
			if (caster.IsFriendlyTo(unit) && unit.IsPlayer())
			{
				// If target already has aura, increase duration to max 130% of initial duration
				caster.CastSpell(unit, PriestSpells.ANGELIC_FEATHER_AURA, true);
				at.SetDuration(0);
			}
	}
}