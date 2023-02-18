// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(197492)]
public class aura_dru_restoration_affinity : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	private readonly List<uint> LearnedSpells = new()
	                                            {
		                                            (uint)DruidSpells.YSERA_GIFT,
		                                            (uint)DruidSpells.REJUVENATION,
		                                            (uint)DruidSpells.HEALING_TOUCH,
		                                            (uint)DruidSpells.SWIFTMEND
	                                            };

	private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var target = GetTarget().ToPlayer();

		if (target != null)
			foreach (var spellId in LearnedSpells)
				target.LearnSpell(spellId, false);
	}

	private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var target = GetTarget().ToPlayer();

		if (target != null)
			foreach (var spellId in LearnedSpells)
				target.RemoveSpell(spellId);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}