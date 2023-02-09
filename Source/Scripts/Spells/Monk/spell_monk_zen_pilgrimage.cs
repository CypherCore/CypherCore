using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(194011)]
public class spell_monk_zen_pilgrimage : SpellScript, ISpellOnCast, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (GetSpellInfo().Id == 194011)
			return SpellCastResult.SpellCastOk;

		var caster = GetCaster();

		if (caster != null)
		{
			var _player = caster.ToPlayer();

			if (_player != null)
				if (_player.IsQuestRewarded(40236)) // Check quest for port to oplot
				{
					caster.CastSpell(caster, 194011, false);

					return SpellCastResult.DontReport;
				}
		}

		return SpellCastResult.SpellCastOk;
	}

	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var _player = caster.ToPlayer();

			if (_player != null)
			{
				_player.SaveRecallPosition();
				_player.CastSpell(_player, 126896, true);
			}
		}
	}
}