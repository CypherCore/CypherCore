using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 71610, 71641 - Echoes of Light (Althor's Abacus)
internal class spell_item_echoes_of_light : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		if (targets.Count < 2)
			return;

		targets.Sort(new HealthPctOrderPred());

		var target = targets.FirstOrDefault();
		targets.Clear();
		targets.Add(target);
	}
}