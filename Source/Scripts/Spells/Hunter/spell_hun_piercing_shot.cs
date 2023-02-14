// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(198670)]
public class spell_hun_piercing_shot : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var damage = (uint)GetHitDamage();
		damage *= 2;
		SetHitDamage(damage);

		var caster = GetCaster();

		if (caster != null)
		{
			var target = GetHitUnit();

			if (target == null)
				return;

			var targets = new List<Unit>();

			caster.GetAnyUnitListInRange(targets, caster.GetDistance(target));

			foreach (var otherTarget in targets)
				if (otherTarget != target)
					if (!caster.IsFriendlyTo(otherTarget))
						if (otherTarget.IsInBetween(caster, target, 5.0f))
							caster.CastSpell(otherTarget, 213678, true);
		}
	}
}