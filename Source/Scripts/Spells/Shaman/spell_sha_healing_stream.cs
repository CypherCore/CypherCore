// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	[SpellScript(52042)]
	public class spell_sha_healing_stream : SpellScript, ISpellOnHit
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.HEALING_STREAM, Difficulty.None) != null)
				return false;

			return true;
		}

		public void OnHit()
		{
			if (!GetCaster().GetOwner())
				return;

			var _player = GetCaster().GetOwner().ToPlayer();

			if (_player != null)
			{
				var target = GetHitUnit();

				if (target != null)
					// Glyph of Healing Stream Totem
					if (target.GetGUID() != _player.GetGUID() && _player.HasAura(ShamanSpells.GLYPH_OF_HEALING_STREAM_TOTEM))
						_player.CastSpell(target, ShamanSpells.GLYPH_OF_HEALING_STREAM, true);
			}
		}
	}
}