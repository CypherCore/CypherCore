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
		uint damage = (uint)GetHitDamage();
		damage *= 2;
		SetHitDamage(damage);

		Unit caster = GetCaster();
		if (caster != null)
		{
			Unit target = GetHitUnit();

			if (target == null)
			{
				return;
			}

			List<Unit> targets = new List<Unit>();

			caster.GetAnyUnitListInRange(targets, caster.GetDistance(target));

			foreach (var otherTarget in targets)
			{
				if (otherTarget != target)
				{
					if (!caster.IsFriendlyTo(otherTarget))
					{
						if (otherTarget.IsInBetween(caster, target, 5.0f))
						{
							caster.CastSpell(otherTarget, 213678, true);
						}
					}
				}
			}
		}
	}
}