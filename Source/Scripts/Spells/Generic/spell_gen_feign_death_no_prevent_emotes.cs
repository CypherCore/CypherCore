using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 58951 - Permanent Feign Death
internal class spell_gen_feign_death_no_prevent_emotes : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();
		target.SetUnitFlag3(UnitFlags3.FakeDead);
		target.SetUnitFlag2(UnitFlags2.FeignDeath);

		var creature = target.ToCreature();

		creature?.SetReactState(ReactStates.Passive);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();
		target.RemoveUnitFlag3(UnitFlags3.FakeDead);
		target.RemoveUnitFlag2(UnitFlags2.FeignDeath);

		var creature = target.ToCreature();

		creature?.InitializeReactState();
	}
}