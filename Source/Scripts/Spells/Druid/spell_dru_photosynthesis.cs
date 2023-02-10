using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(274902)]
public class spell_dru_photosynthesis : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		////  if (!GetCaster()->HasAura(SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS))
		//    GetCaster()->AddAura(SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS);
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (GetCaster().HasAura(DruidSpells.SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS))
			GetCaster().RemoveAura(DruidSpells.SPELL_DRU_PHOTOSYNTHESIS_MOD_HEAL_TICKS);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}