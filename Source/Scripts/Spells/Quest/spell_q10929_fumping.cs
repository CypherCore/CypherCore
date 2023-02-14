// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 39238 - Fumping
internal class spell_q10929_fumping : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(QuestSpellIds.SummonSandGnome, QuestSpellIds.SummonBoneSlicer);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
			return;

		var caster = GetCaster();

		if (caster)
			caster.CastSpell(caster, RandomHelper.URand(QuestSpellIds.SummonSandGnome, QuestSpellIds.SummonBoneSlicer), true);
	}
}