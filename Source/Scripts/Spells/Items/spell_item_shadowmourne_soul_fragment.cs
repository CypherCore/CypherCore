// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 71905 - Soul Fragment
internal class spell_item_shadowmourne_soul_fragment : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.ShadowmourneVisualLow, ItemSpellIds.ShadowmourneVisualHigh, ItemSpellIds.ShadowmourneChaosBaneBuff);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnStackChange, 0, AuraType.ModStat, AuraEffectHandleModes.Real | AuraEffectHandleModes.Reapply, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModStat, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnStackChange(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();

		switch (GetStackAmount())
		{
			case 1:
				target.CastSpell(target, ItemSpellIds.ShadowmourneVisualLow, true);

				break;
			case 6:
				target.RemoveAura(ItemSpellIds.ShadowmourneVisualLow);
				target.CastSpell(target, ItemSpellIds.ShadowmourneVisualHigh, true);

				break;
			case 10:
				target.RemoveAura(ItemSpellIds.ShadowmourneVisualHigh);
				target.CastSpell(target, ItemSpellIds.ShadowmourneChaosBaneBuff, true);

				break;
			default:
				break;
		}
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();
		target.RemoveAura(ItemSpellIds.ShadowmourneVisualLow);
		target.RemoveAura(ItemSpellIds.ShadowmourneVisualHigh);
	}
}