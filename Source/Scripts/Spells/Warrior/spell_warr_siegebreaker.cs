using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	//280772 - Siegebreaker
	[SpellScript(280772)]
	public class spell_warr_siegebreaker : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();
			caster.CastSpell(null, WarriorSpells.SIEGEBREAKER_BUFF, true);
		}
	}
}