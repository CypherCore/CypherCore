using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
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
		Unit target = GetTarget();

		switch (GetStackAmount())
		{
			case 1:
				target.CastSpell(target, ItemSpellIds.ShadowmourneVisualLow, true);

				break;
			case 6:
				target.RemoveAurasDueToSpell(ItemSpellIds.ShadowmourneVisualLow);
				target.CastSpell(target, ItemSpellIds.ShadowmourneVisualHigh, true);

				break;
			case 10:
				target.RemoveAurasDueToSpell(ItemSpellIds.ShadowmourneVisualHigh);
				target.CastSpell(target, ItemSpellIds.ShadowmourneChaosBaneBuff, true);

				break;
			default:
				break;
		}
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit target = GetTarget();
		target.RemoveAurasDueToSpell(ItemSpellIds.ShadowmourneVisualLow);
		target.RemoveAurasDueToSpell(ItemSpellIds.ShadowmourneVisualHigh);
	}
}