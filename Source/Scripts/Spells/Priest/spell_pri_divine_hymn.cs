// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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