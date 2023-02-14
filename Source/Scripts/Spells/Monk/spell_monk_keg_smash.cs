// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(121253)]
public class spell_monk_keg_smash : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var _player = caster.ToPlayer();

			if (_player != null)
			{
				var target = GetHitUnit();

				if (target != null)
				{
					_player.CastSpell(target, MonkSpells.SPELL_MONK_KEG_SMASH_VISUAL, true);
					_player.CastSpell(target, MonkSpells.SPELL_MONK_WEAKENED_BLOWS, true);
					_player.CastSpell(_player, MonkSpells.SPELL_MONK_KEG_SMASH_ENERGIZE, true);
					// Prevent to receive 2 CHI more than once time per cast
					_player.GetSpellHistory().AddCooldown(MonkSpells.SPELL_MONK_KEG_SMASH_ENERGIZE, 0, TimeSpan.FromSeconds(1));
					_player.CastSpell(target, MonkSpells.SPELL_MONK_DIZZYING_HAZE, true);
				}
			}
		}
	}
}