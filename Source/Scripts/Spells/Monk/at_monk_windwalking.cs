using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_windwalking : AreaTriggerAI
{
	public at_monk_windwalking(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		var aur = unit.GetAura(MonkSpells.SPELL_MONK_WINDWALKER_AURA);

		if (aur != null)
			aur.SetDuration(-1);
		else if (caster.IsFriendlyTo(unit))
			caster.CastSpell(unit, MonkSpells.SPELL_MONK_WINDWALKER_AURA, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (unit.HasAura(MonkSpells.SPELL_MONK_WINDWALKING) && unit != caster) // Don't remove from other WW monks.
			return;

		var aur = unit.GetAura(MonkSpells.SPELL_MONK_WINDWALKER_AURA, caster.GetGUID());

		if (aur != null)
		{
			aur.SetMaxDuration(10 * Time.InMilliseconds);
			aur.SetDuration(10 * Time.InMilliseconds);
		}
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (!caster.ToPlayer())
			return;

		foreach (var guid in at.GetInsideUnits())
		{
			var unit = ObjectAccessor.Instance.GetUnit(caster, guid);

			if (unit != null)
			{
				if (unit.HasAura(MonkSpells.SPELL_MONK_WINDWALKING) && unit != caster) // Don't remove from other WW monks.
					continue;

				var aur = unit.GetAura(MonkSpells.SPELL_MONK_WINDWALKER_AURA, caster.GetGUID());

				if (aur != null)
				{
					aur.SetMaxDuration(10 * Time.InMilliseconds);
					aur.SetDuration(10 * Time.InMilliseconds);
				}
			}
		}
	}
}