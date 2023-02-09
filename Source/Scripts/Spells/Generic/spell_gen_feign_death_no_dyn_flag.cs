using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 51329 - Feign Death
internal class spell_gen_feign_death_no_dyn_flag : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit target = GetTarget();
		target.SetUnitFlag2(UnitFlags2.FeignDeath);
		target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);

		Creature creature = target.ToCreature();

		creature?.SetReactState(ReactStates.Passive);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit target = GetTarget();
		target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
		target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

		Creature creature = target.ToCreature();

		creature?.InitializeReactState();
	}
}