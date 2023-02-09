using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[Script] // 64844 - Divine Hymn
internal class spell_pri_divine_hymn : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, SpellConst.EffectAll, Targets.UnitSrcAreaAlly));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RemoveAll(obj =>
		                  {
			                  var target = obj.ToUnit();

			                  if (target)
				                  return !GetCaster().IsInRaidWith(target);

			                  return true;
		                  });

		uint maxTargets = 3;

		if (targets.Count > maxTargets)
		{
			targets.Sort(new HealthPctOrderPred());
			targets.Resize(maxTargets);
		}
	}
}