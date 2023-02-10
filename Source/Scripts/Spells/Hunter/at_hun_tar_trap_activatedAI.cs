using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_tar_trap_activatedAI : AreaTriggerAI
{
	public int timeInterval;

	public enum UsedSpells
	{
		SPELL_HUNTER_TAR_TRAP_SLOW = 135299
	}

	public at_hun_tar_trap_activatedAI(AreaTrigger areatrigger) : base(areatrigger)
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
				caster.CastSpell(target, UsedSpells.SPELL_HUNTER_TAR_TRAP_SLOW, true);
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
			caster.CastSpell(unit, UsedSpells.SPELL_HUNTER_TAR_TRAP_SLOW, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (unit.HasAura(UsedSpells.SPELL_HUNTER_TAR_TRAP_SLOW) && unit.GetAura(UsedSpells.SPELL_HUNTER_TAR_TRAP_SLOW).GetCaster() == caster)
			unit.RemoveAura(UsedSpells.SPELL_HUNTER_TAR_TRAP_SLOW);
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (!caster.ToPlayer())
			return;

		foreach (var itr in at.GetInsideUnits())
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (target.HasAura(UsedSpells.SPELL_HUNTER_TAR_TRAP_SLOW) && target.GetAura(UsedSpells.SPELL_HUNTER_TAR_TRAP_SLOW).GetCaster() == caster)
				target.RemoveAura(UsedSpells.SPELL_HUNTER_TAR_TRAP_SLOW);
		}
	}
}