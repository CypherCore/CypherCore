using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(197632)]
public class spell_dru_balance_affinity_resto : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void LearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		Player player = caster.ToPlayer();
		if (player != null)
		{
			player.AddTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
			player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
			player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
		}
	}

	private void UnlearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		Player player = caster.ToPlayer();
		if (player != null)
		{
			player.RemoveTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
			player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
			player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectApplyHandler(UnlearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new EffectApplyHandler(LearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
	}
}