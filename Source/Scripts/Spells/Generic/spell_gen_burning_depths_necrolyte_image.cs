using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 48750 - Burning Depths Necrolyte Image
internal class spell_gen_burning_depths_necrolyte_image : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 2 && ValidateSpellInfo((uint)spellInfo.GetEffect(2).CalcValue());
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = GetCaster();

		if (caster)
			caster.CastSpell(GetTarget(), (uint)GetEffectInfo(2).CalcValue());
	}

	private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAurasDueToSpell((uint)GetEffectInfo(2).CalcValue(), GetCasterGUID());
	}
}