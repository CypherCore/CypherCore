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