using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(125883)]
public class spell_monk_zen_flight_check : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			if (_player.GetMap().IsBattlegroundOrArena())
				return SpellCastResult.NotInBattleground;

			// In Kalimdor or Eastern Kingdom with Flight Master's License
			if (!_player.HasSpell(90267) && (_player.GetMapId() == 1 || _player.GetMapId() == 0))
				return SpellCastResult.NotHere;

			// In Pandaria with Wisdom of the Four Winds
			if (!_player.HasSpell(115913) && (_player.GetMapId() == 870))
				return SpellCastResult.NotHere;

			// Legion, Broken Isles
			if (_player.GetMapId() == 1220)
				return SpellCastResult.NotHere;

			// In BfA Content not yet
			if (_player.GetMapId() == 1642 || _player.GetMapId() == 1643)
				return SpellCastResult.NotHere;
		}

		return SpellCastResult.SpellCastOk;
	}
}