using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_mark_of_kazrogal_hellfire : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RemoveAll(target =>
		                  {
			                  Unit unit = target.ToUnit();

			                  if (unit)
				                  return unit.GetPowerType() != PowerType.Mana;

			                  return false;
		                  });
	}
}