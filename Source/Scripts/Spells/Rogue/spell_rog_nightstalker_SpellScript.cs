// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(14062)]
public class spell_rog_nightstalker_SpellScript : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			if (_player.HasAura(RogueSpells.NIGHTSTALKER_AURA))
				_player.CastSpell(_player, RogueSpells.NIGHTSTALKER_DAMAGE_DONE, true);

			if (_player.HasAura(RogueSpells.SHADOW_FOCUS))
				_player.CastSpell(_player, RogueSpells.SHADOW_FOCUS_EFFECT, true);
		}
	}
}