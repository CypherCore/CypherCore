using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 32065 - Fungal Decay
internal class spell_gen_decay_over_time_fungal_decay_SpellScript : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		Aura aur = GetHitAura();

		aur?.SetStackAmount((byte)GetSpellInfo().StackAmount);
	}
}