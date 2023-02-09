using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 52593 - Bloated Abomination Feign Death
internal class spell_q13264_q13276_q13288_q13289_bloated_abom_feign_death : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit target = GetTarget();
		target.SetUnitFlag3(UnitFlags3.FakeDead);
		target.SetUnitFlag2(UnitFlags2.FeignDeath);

		Creature creature = target.ToCreature();

		creature?.SetReactState(ReactStates.Passive);
	}

	private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit     target   = GetTarget();
		Creature creature = target.ToCreature();

		creature?.DespawnOrUnsummon();
	}
}