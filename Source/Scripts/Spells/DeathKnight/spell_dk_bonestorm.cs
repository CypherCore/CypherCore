using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194844)]
public class spell_dk_bonestorm : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private int m_ExtraSpellCost;

	public override bool Load()
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return false;
		}

		int availablePower = Math.Min(caster.GetPower(PowerType.RunicPower), 90);

		//Round down to nearest multiple of 10
		m_ExtraSpellCost = availablePower - (availablePower % 10);
		return true;
	}

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		int m_newDuration = GetDuration() + (m_ExtraSpellCost / 10);
		SetDuration(m_newDuration);

		Unit caster = GetCaster();
		if (caster != null)
		{
			int m_newPower = caster.GetPower(PowerType.RunicPower) - m_ExtraSpellCost;
			if (m_newPower < 0)
			{
				m_newPower = 0;
			}
			caster.SetPower(PowerType.RunicPower, m_newPower);
		}
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		caster.CastSpell(caster, DeathKnightSpells.SPELL_DK_BONESTORM_HEAL, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicTriggerSpell));
	}
}