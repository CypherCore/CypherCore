using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(116705)]
public class spell_monk_spear_hand_strike : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		Player _player = GetCaster().ToPlayer();
		if (_player != null)
		{
			Unit target = GetHitUnit();
			if (target != null)
			{
				if (target.IsInFront(_player))
				{
					_player.CastSpell(target, MonkSpells.SPELL_MONK_SPEAR_HAND_STRIKE_SILENCE, true);
					_player.GetSpellHistory().AddCooldown(116705, 0, TimeSpan.FromSeconds(15));
				}
			}
		}
	}
}