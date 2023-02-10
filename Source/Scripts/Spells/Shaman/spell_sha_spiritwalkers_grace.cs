using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	[SpellScript(79206)]
	public class spell_sha_spiritwalkers_grace : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();

			if (caster.HasAura(159651))
				caster.CastSpell(caster, 159652, true);
		}
	}
}