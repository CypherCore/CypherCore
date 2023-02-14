// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(116680)]
public class spell_monk_item_s12_4p_mistweaver : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
			if (_player.HasAura(MonkSpells.SPELL_MONK_ITEM_4_S12_MISTWEAVER))
				_player.CastSpell(_player, MonkSpells.SPELL_MONK_ZEN_FOCUS, true);
	}
}