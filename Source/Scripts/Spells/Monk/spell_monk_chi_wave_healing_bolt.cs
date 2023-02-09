using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(132464)]
public class spell_monk_chi_wave_healing_bolt : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		if (!GetOriginalCaster())
		{
			return;
		}

		Player _player = GetOriginalCaster().ToPlayer();
		if (_player != null)
		{
			Unit target = GetHitUnit();
			if (target != null)
			{
				_player.CastSpell(target, MonkSpells.SPELL_MONK_CHI_WAVE_HEAL, true);
			}
		}
	}
}