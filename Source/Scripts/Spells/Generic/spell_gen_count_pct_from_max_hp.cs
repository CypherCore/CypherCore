using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script("spell_gen_default_count_pct_from_max_hp", 0)]
[Script("spell_gen_50pct_count_pct_from_max_hp", 50)]
internal class spell_gen_count_pct_from_max_hp : SpellScript, ISpellOnHit
{
	private int _damagePct;

	public spell_gen_count_pct_from_max_hp(int damagePct)
	{
		_damagePct = damagePct;
	}

	public void OnHit()
	{
		if (_damagePct == 0)
			_damagePct = GetHitDamage();

		SetHitDamage((int)GetHitUnit().CountPctFromMaxHealth(_damagePct));
	}
}