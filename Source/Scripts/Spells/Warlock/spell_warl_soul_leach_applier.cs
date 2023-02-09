using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Soul Leach appliers - 137046, 137044, 137043
	[SpellScript(new uint[]
	             {
		             137046, 137044, 137043
	             })]
	public class spell_warl_soul_leach_applier : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, WarlockSpells.SOUL_LEECH, true);
		}
	}
}