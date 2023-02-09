using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	[Script] // 48181 - Haunt
	internal class spell_warl_haunt : SpellScript, ISpellAfterHit
	{
		public void AfterHit()
		{
			var aura = GetHitAura();

			if (aura != null)
			{
				var aurEff = aura.GetEffect(1);

				aurEff?.SetAmount(MathFunctions.CalculatePct(GetHitDamage(), aurEff.GetAmount()));
			}
		}
	}
}