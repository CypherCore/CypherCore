using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 198069 - Power of the Dark Side
internal class spell_pri_power_of_the_dark_side : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.PowerOfTheDarkSideTint);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = GetCaster();

		caster?.CastSpell(caster, PriestSpells.PowerOfTheDarkSideTint, true);
	}

	private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = GetCaster();

		caster?.RemoveAura(PriestSpells.PowerOfTheDarkSideTint);
	}
}