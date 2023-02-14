// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
			                  var unit = target.ToUnit();

			                  if (unit)
				                  return unit.GetPowerType() != PowerType.Mana;

			                  return false;
		                  });
	}
}