using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 35201 - Paralytic Poison
internal class spell_gen_paralytic_poison : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.Paralysis);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleStun, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleStun(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
			return;

		GetTarget().CastSpell((Unit)null, GenericSpellIds.Paralysis, new CastSpellExtraArgs(aurEff));
	}
}