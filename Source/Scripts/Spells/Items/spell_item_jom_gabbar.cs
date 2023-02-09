using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 29602 - Jom Gabbar
internal class spell_item_jom_gabbar : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.JomGabbar);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAurasDueToSpell(effect.GetSpellEffectInfo().TriggerSpell);
	}
}