using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(new uint[] { 190984, 194153 })]
public class spell_dru_blessing_of_elune : SpellScript, ISpellOnHit
{


	public void OnHit()
	{
		Unit caster = GetCaster();

		if (caster == null)
		{
			return;
		}

		var power = GetHitDamage();

		Aura aura = caster.GetAura(202737);
		if (aura != null)
		{
			AuraEffect aurEff = aura.GetEffect(0);
			if (aurEff != null)
			{
				power += MathFunctions.CalculatePct(power, aurEff.GetAmount());
			}
		}

		SetHitDamage(power);
	}


}