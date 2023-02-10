using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	[Script] // 6262 - Healthstone
	internal class spell_warl_healthstone_heal : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var heal = (int)MathFunctions.CalculatePct(GetCaster().GetCreateHealth(), GetHitHeal());
			SetHitHeal(heal);
		}
	}
}