// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 28200 - Ascendance
internal class spell_item_talisman_of_ascendance : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.TalismanOfAscendance);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnRemove(AuraEffect effect, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAura(effect.GetSpellEffectInfo().TriggerSpell);
	}
}