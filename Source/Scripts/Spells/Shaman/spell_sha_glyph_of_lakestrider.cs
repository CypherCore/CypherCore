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
				if (_player.HasAura(ShamanSpells.SPELL_SHAMAN_GLYPH_OF_LAKESTRIDER))
					_player.CastSpell(_player, ShamanSpells.SPELL_SHAMAN_WATER_WALKING, true);
		}
	}
}