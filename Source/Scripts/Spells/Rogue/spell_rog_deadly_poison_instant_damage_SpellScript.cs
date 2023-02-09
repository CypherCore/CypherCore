using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(2823)]
public class spell_rog_deadly_poison_instant_damage_SpellScript : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Player _player = GetCaster().ToPlayer();
		if (_player != null)
		{
			Unit target = GetExplTargetUnit();
			if (target != null)
			{
				if (target.HasAura(RogueSpells.SPELL_ROGUE_DEADLY_POISON_DOT, _player.GetGUID()))
				{
					_player.CastSpell(target, RogueSpells.SPELL_ROGUE_DEADLY_POISON_INSTANT_DAMAGE, true);
				}
			}
		}
	}
}