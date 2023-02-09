using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(197908)]
public class spell_monk_mana_tea : SpellScript, ISpellAfterCast, ISpellBeforeCast
{
	private SpellModifier mod = null;

	public void BeforeCast()
	{
		Player _player = GetCaster().ToPlayer();
		if (_player != null)
		{
			int stacks = 0;

			Aura manaTeaStacks = _player.GetAura(MonkSpells.SPELL_MONK_MANA_TEA_STACKS);
			if (manaTeaStacks != null)
			{
				stacks = manaTeaStacks.GetStackAmount();

				int newDuration = stacks * Time.InMilliseconds;


				SpellModifierByClassMask mod = new SpellModifierByClassMask(manaTeaStacks);
				mod.op                                = SpellModOp.Duration;
				mod.type                              = SpellModType.Flat;
				mod.spellId                           = MonkSpells.SPELL_MONK_MANA_TEA_REGEN;
				((SpellModifierByClassMask)mod).value = newDuration;
				mod.mask[1]                           = 0x200000;
				mod.mask[2]                           = 0x1;

				_player.AddSpellMod(mod, true);
			}
		}
	}

	public void AfterCast()
	{
		if (mod != null)
		{
			Player _player = GetCaster().ToPlayer();
			if (_player != null)
			{
				_player.AddSpellMod(mod, false);
			}
		}
	}
}