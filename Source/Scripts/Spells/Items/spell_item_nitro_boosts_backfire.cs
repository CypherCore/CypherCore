﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_nitro_boosts_backfire : AuraScript, IHasAuraEffects
{
	private float lastZ = MapConst.InvalidHeight;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.NitroBoostsParachute);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicDummy, 1, AuraType.PeriodicTriggerSpell));
	}

	private void HandleApply(AuraEffect effect, AuraEffectHandleModes mode)
	{
		lastZ = GetTarget().GetPositionZ();
	}

	private void HandlePeriodicDummy(AuraEffect effect)
	{
		PreventDefaultAction();
		var curZ = GetTarget().GetPositionZ();

		if (curZ < lastZ)
		{
			if (RandomHelper.randChance(80)) // we don't have enough sniffs to verify this, guesstimate
				GetTarget().CastSpell(GetTarget(), ItemSpellIds.NitroBoostsParachute, new CastSpellExtraArgs(effect));

			GetAura().Remove();
		}
		else
		{
			lastZ = curZ;
		}
	}
}