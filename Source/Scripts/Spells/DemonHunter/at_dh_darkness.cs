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

		if (caster.IsFriendlyTo(unit) && !unit.HasAura(DemonHunterSpells.SPELL_DH_DARKNESS_ABSORB))
		{
			entered = true;

			if (entered)
			{
				caster.CastSpell(unit, DemonHunterSpells.SPELL_DH_DARKNESS_ABSORB, true);
				entered = false;
			}
		}
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (unit.HasAura(DemonHunterSpells.SPELL_DH_DARKNESS_ABSORB))
			unit.RemoveAurasDueToSpell(DemonHunterSpells.SPELL_DH_DARKNESS_ABSORB, caster.GetGUID());
	}
}