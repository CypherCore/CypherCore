using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 45043 - Power Circle (Shifting Naaru Sliver)
internal class spell_item_power_circle : AuraScript, IAuraCheckAreaTarget, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.LimitlessPower);
	}

	public bool CheckAreaTarget(Unit target)
	{
		return target.GetGUID() == GetCasterGUID();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().CastSpell(null, ItemSpellIds.LimitlessPower, true);
		Aura buff = GetTarget().GetAura(ItemSpellIds.LimitlessPower);

		buff?.SetDuration(GetDuration());
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAurasDueToSpell(ItemSpellIds.LimitlessPower);
	}
}