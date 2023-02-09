using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(198030)]
public class spell_demon_hunter_eye_beam_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void FilterTargets(List<WorldObject> unitList)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		unitList.Clear();
		var units = new List<Unit>();
		caster.GetAttackableUnitListInRange(units, 25.0f);


		units.RemoveIf((Unit unit) => { return !caster.HasInLine(unit, 5.0f, caster.GetObjectScale()); });

		foreach (var unit in units)
			unitList.Add(unit);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitRectCasterEnemy));
	}
}