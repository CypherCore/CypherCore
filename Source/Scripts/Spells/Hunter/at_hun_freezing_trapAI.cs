using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_freezing_trapAI : AreaTriggerAI
{
	public int timeInterval;

	public enum UsedSpells
	{
		SPELL_HUNTER_FREEZING_TRAP_STUN = 3355
	}

	public at_hun_freezing_trapAI(AreaTrigger areatrigger) : base(areatrigger)
	{
		timeInterval = 200;
	}

	public override void OnCreate()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (!caster.ToPlayer())
			return;

		foreach (var itr in at.GetInsideUnits())
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (!caster.IsFriendlyTo(target))
			{
				caster.CastSpell(target, UsedSpells.SPELL_HUNTER_FREEZING_TRAP_STUN, true);
				at.Remove();

				return;
			}
		}
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (!caster.IsFriendlyTo(unit))
		{
			caster.CastSpell(unit, UsedSpells.SPELL_HUNTER_FREEZING_TRAP_STUN, true);
			at.Remove();

			return;
		}
	}
}