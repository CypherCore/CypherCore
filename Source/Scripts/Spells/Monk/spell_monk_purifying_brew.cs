// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(119582)]
public class spell_monk_purifying_brew : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var _player = caster.ToPlayer();

			if (_player != null)
			{
				var staggerAmount = _player.GetAura(MonkSpells.LIGHT_STAGGER);

				if (staggerAmount == null)
					staggerAmount = _player.GetAura(MonkSpells.MODERATE_STAGGER);

				if (staggerAmount == null)
					staggerAmount = _player.GetAura(MonkSpells.HEAVY_STAGGER);

				if (staggerAmount != null)
				{
					var newStagger = staggerAmount.GetEffect(1).GetAmount();
					newStagger = (int)(newStagger * 0.5);
					staggerAmount.GetEffect(1).ChangeAmount(newStagger);
				}
			}
		}
	}
}