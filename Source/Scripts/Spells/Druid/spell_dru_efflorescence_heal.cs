using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(81269)]
public class spell_dru_efflorescence_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void SortTargets(List<WorldObject> targets)
	{
		targets.Sort(new HealthPctOrderPred());

		if (targets.Count > 3)
			targets.Resize(3);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(SortTargets, 0, Targets.UnitDestAreaAlly));
	}
}