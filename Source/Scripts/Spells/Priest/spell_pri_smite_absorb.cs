using System;
using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(208771)]
public class spell_pri_smite_absorb : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandleAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		Unit caster   = GetCaster();
		Unit attacker = dmgInfo.GetAttacker();
		if (caster == null || attacker == null)
		{
			return;
		}

		if (!attacker.HasAura(PriestSpells.SPELL_PRIEST_SMITE_AURA, caster.GetGUID()))
		{
			absorbAmount = 0;
		}
		else
		{
			Aura aur = attacker.GetAura(PriestSpells.SPELL_PRIEST_SMITE_AURA, caster.GetGUID());
			if (aur != null)
			{
				AuraEffect aurEff = aur.GetEffect(0);
				if (aurEff != null)
				{
					int absorb = Math.Max(0, aurEff.GetAmount() - (int)dmgInfo.GetDamage());
					if (absorb <= 0)
					{
						absorbAmount = (uint)aurEff.GetAmount();
						aur.Remove();
					}
					else
					{
						aurEff.SetAmount(absorb);
						aur.SetNeedClientUpdateForTargets();
					}
				}
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
	}
}