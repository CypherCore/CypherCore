using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115181)]
public class spell_monk_breath_of_fire : SpellScript, ISpellAfterHit
{


	public void AfterHit()
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			Player _player = caster.ToPlayer();
			if (_player != null)
			{
				Unit target = GetHitUnit();
				if (target != null)
				{
					// if Dizzying Haze is on the target, they will burn for an additionnal damage over 8s
					if (target.HasAura(MonkSpells.SPELL_MONK_DIZZYING_HAZE))
					{
						_player.CastSpell(target, MonkSpells.SPELL_MONK_BREATH_OF_FIRE_DOT, true);
					}
					if (target.HasAura(MonkSpells.SPELL_MONK_KEG_SMASH_AURA))
					{
						_player.CastSpell(target, MonkSpells.SPELL_MONK_BREATH_OF_FIRE_DOT, true);
					}
				}
			}
		}
	}
}