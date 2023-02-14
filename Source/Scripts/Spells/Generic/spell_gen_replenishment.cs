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
internal class spell_gen_replenishment : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 255, Targets.UnitCasterAreaRaid));
	}

	private void RemoveInvalidTargets(List<WorldObject> targets)
	{
		// In arenas Replenishment may only affect the caster
		var caster = GetCaster().ToPlayer();

		if (caster)
			if (caster.InArena())
			{
				targets.Clear();
				targets.Add(caster);

				return;
			}

		targets.RemoveAll(obj =>
		                  {
			                  var target = obj.ToUnit();

			                  if (target)
				                  return target.GetPowerType() != PowerType.Mana;

			                  return true;
		                  });

		byte maxTargets = 10;

		if (targets.Count > maxTargets)
		{
			targets.Sort(new PowerPctOrderPred(PowerType.Mana));
			targets.Resize(maxTargets);
		}
	}
}