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
		Unit caster = at.GetCaster();

		if (caster == null || unit == null)
		{
			return;
		}

		if (!caster.ToPlayer())
		{
			return;
		}

		if (caster.IsFriendlyTo(unit))
		{
			caster.CastSpell(unit, PriestSpells.SPELL_PRIEST_POWER_WORD_BARRIER_BUFF, true);
		}
	}

	public override void OnUnitExit(Unit unit)
	{
		Unit caster = at.GetCaster();

		if (caster == null || unit == null)
		{
			return;
		}

		if (!caster.ToPlayer())
		{
			return;
		}

		if (unit.HasAura(PriestSpells.SPELL_PRIEST_POWER_WORD_BARRIER_BUFF, caster.GetGUID()))
		{
			unit.RemoveAurasDueToSpell(PriestSpells.SPELL_PRIEST_POWER_WORD_BARRIER_BUFF, caster.GetGUID());
		}
	}
}