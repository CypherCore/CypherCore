// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[SpellScript(63560)]
public class spell_dk_dark_transformation_form : SpellScript
{
	public void OnHit()
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			var pet = GetHitUnit();

			if (pet != null)
				if (pet.HasAura(DeathKnightSpells.DARK_INFUSION_STACKS))
				{
					_player.RemoveAura(DeathKnightSpells.DARK_INFUSION_STACKS);
					pet.RemoveAura(DeathKnightSpells.DARK_INFUSION_STACKS);
				}
		}
	}
}