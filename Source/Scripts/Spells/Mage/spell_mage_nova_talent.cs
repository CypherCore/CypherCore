using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[SpellScript(new uint[] { 157997, 157980 })]
public class spell_mage_nova_talent : SpellScript
{


	public void OnHit()
	{
		Unit caster     = GetCaster();
		Unit target     = GetHitUnit();
		Unit explTarget = GetExplTargetUnit();
		if (target == null || caster == null || explTarget == null)
		{
			return;
		}

		int eff2 = GetSpellInfo().GetEffect(2).CalcValue();
		if (eff2 != 0)
		{
			int dmg = GetHitDamage();
			if (target == explTarget)
			{
				dmg = MathFunctions.CalculatePct(dmg, eff2);
			}
			SetHitDamage(dmg);
		}
	}


}