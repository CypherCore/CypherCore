using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 35491 - Furious Rage
internal class spell_gen_furious_rage : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.Exhaustion) &&
		       CliDB.BroadcastTextStorage.HasRecord(EmoteIds.FuriousRage) &&
		       CliDB.BroadcastTextStorage.HasRecord(EmoteIds.Exhausted);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.ModDamagePercentDone, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();
		target.TextEmote(EmoteIds.FuriousRage, target, false);
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
			return;

		var target = GetTarget();
		target.TextEmote(EmoteIds.Exhausted, target, false);
		target.CastSpell(target, GenericSpellIds.Exhaustion, true);
	}
}