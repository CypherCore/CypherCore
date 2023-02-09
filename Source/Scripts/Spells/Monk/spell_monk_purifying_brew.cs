using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(119582)]
public class spell_monk_purifying_brew : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var _player = caster.ToPlayer();

			if (_player != null)
			{
				var staggerAmount = _player.GetAura(MonkSpells.SPELL_MONK_LIGHT_STAGGER);

				if (staggerAmount == null)
					staggerAmount = _player.GetAura(MonkSpells.SPELL_MONK_MODERATE_STAGGER);

				if (staggerAmount == null)
					staggerAmount = _player.GetAura(MonkSpells.SPELL_MONK_HEAVY_STAGGER);

				if (staggerAmount != null)
				{
					var newStagger = staggerAmount.GetEffect(1).GetAmount();
					newStagger = (int)(newStagger * 0.5);
					staggerAmount.GetEffect(1).ChangeAmount(newStagger);
				}
			}
		}
	}
}