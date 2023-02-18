// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	[SpellScript(55448)]
	public class spell_sha_glyph_of_lakestrider : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var _player = GetCaster().ToPlayer();

			if (_player != null)
				if (_player.HasAura(ShamanSpells.GLYPH_OF_LAKESTRIDER))
					_player.CastSpell(_player, ShamanSpells.WATER_WALKING, true);
		}
	}
}