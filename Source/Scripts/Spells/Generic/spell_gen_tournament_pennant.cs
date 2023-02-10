using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_tournament_pennant : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster() && GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApplyEffect, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
	}

	private void HandleApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = GetCaster();

		if (caster)
			if (!caster.GetVehicleBase())
				caster.RemoveAurasDueToSpell(GetId());
	}
}