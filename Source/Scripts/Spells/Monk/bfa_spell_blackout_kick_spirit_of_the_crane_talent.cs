using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(210802)]
public class bfa_spell_blackout_kick_spirit_of_the_crane_talent : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		Player caster = GetCaster().ToPlayer();
		if (caster == null)
		{
			return;
		}

		if (caster.HasAura(MonkSpells.SPELL_SPIRIT_OF_THE_CRANE))
		{
			caster.SetPower(PowerType.Mana, caster.GetPower(PowerType.Mana) + ((caster.GetMaxPower(PowerType.Mana) * 0.65f) / 100));
		}
	}
}