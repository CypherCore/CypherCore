using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	//197995
	[SpellScript(197995)]
	public class spell_sha_wellspring : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			caster.CastSpell(target, ShamanSpells.SPELL_SHAMAN_WELLSPRING_MISSILE, true);
		}
	}
}