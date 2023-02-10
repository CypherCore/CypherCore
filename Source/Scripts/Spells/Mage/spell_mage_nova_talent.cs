using Game.Scripting;

namespace Scripts.Spells.Mage;

[SpellScript(new uint[]
             {
	             157997, 157980
             })]
public class spell_mage_nova_talent : SpellScript
{
	public void OnHit()
	{
		var caster     = GetCaster();
		var target     = GetHitUnit();
		var explTarget = GetExplTargetUnit();

		if (target == null || caster == null || explTarget == null)
			return;

		var eff2 = GetSpellInfo().GetEffect(2).CalcValue();

		if (eff2 != 0)
		{
			var dmg = GetHitDamage();

			if (target == explTarget)
				dmg = MathFunctions.CalculatePct(dmg, eff2);

			SetHitDamage(dmg);
		}
	}
}