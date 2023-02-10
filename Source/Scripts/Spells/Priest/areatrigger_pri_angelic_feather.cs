using System.Linq;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Priest;

[Script] // Angelic Feather areatrigger - created by SPELL_PRIEST_ANGELIC_FEATHER_AREATRIGGER
internal class areatrigger_pri_angelic_feather : AreaTriggerAI
{
	public areatrigger_pri_angelic_feather(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	// Called when the AreaTrigger has just been initialized, just before added to map
	public override void OnInitialize()
	{
		var caster = at.GetCaster();

		if (caster)
		{
			var areaTriggers = caster.GetAreaTriggers(PriestSpells.AngelicFeatherAreatrigger);

			if (areaTriggers.Count >= 3)
				areaTriggers.First().SetDuration(0);
		}
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster)
			if (caster.IsFriendlyTo(unit))
			{
				// If Target already has aura, increase duration to max 130% of initial duration
				caster.CastSpell(unit, PriestSpells.AngelicFeatherAura, true);
				at.SetDuration(0);
			}
	}
}