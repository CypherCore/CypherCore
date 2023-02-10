using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Called by Dark Soul - 77801 ( Generic ), 113858 ( Instability ), 113860 ( Misery ), 113861 ( Knowledge )
	[Script]
	public class spell_warlock_t15_2p_bonus : SpellScript, ISpellAfterCast
	{
		public void AfterCast()
		{
			var caster = GetCaster();

			if (caster != null)
				if (caster.HasAura(WarlockSpells.T15_2P_BONUS)) // Check if caster has bonus aura
					caster.AddAura(WarlockSpells.T15_2P_BONUS_TRIGGERED, caster);
		}
	}
}