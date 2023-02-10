using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(5384)]
public class spell_hun_feign_death : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private ulong health;
	private int focus;

	private void HandleEffectApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		health = GetTarget().GetHealth();
		focus  = GetTarget().GetPower(PowerType.Focus);
	}

	private void HandleEffectRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (health != 0 && focus != 0)
		{
			GetTarget().SetHealth(health);
			GetTarget().SetPower(PowerType.Focus, focus);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.FeignDeath, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.FeignDeath, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}