using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(197531)]
public class spell_dk_bloodworms : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.Clear();

		var caster = GetCaster();

		if (caster != null)
			foreach (var itr in caster.m_Controlled)
			{
				var unit = ObjectAccessor.Instance.GetUnit(caster, itr.GetGUID());

				if (unit != null)
					if (unit.GetEntry() == 99773)
						targets.Add(unit);
			}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
	}
}