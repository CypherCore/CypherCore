// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(357208)]
public class FireBreath : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var target = GetHitUnit();

		if (target != null)
		{
			var damage    = (ulong)GetHitDamage();
			var maxHealth = target.GetMaxHealth();

			if (target.GetHealth() + damage > maxHealth)
				damage = maxHealth - target.GetHealth();

			SetHitDamage(damage);
			target.SetHealth(target.GetHealth() - damage);
		}
	}
}