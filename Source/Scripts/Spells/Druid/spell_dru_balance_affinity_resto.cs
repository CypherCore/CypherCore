// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(197632)]
public class spell_dru_balance_affinity_resto : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void LearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var player = caster.ToPlayer();

		if (player != null)
		{
			player.AddTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
			player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
			player.AddTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
		}
	}

	private void UnlearnSpells(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var player = caster.ToPlayer();

		if (player != null)
		{
			player.RemoveTemporarySpell(ShapeshiftFormSpells.SPELL_DRUID_MOONKIN_FORM);
			player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_STARSURGE);
			player.RemoveTemporarySpell(BalanceAffinitySpells.SPELL_DRUID_LUNAR_STRIKE);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(UnlearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectApplyHandler(LearnSpells, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
	}
}