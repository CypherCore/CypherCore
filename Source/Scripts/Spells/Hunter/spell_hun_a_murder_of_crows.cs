// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 131894 - A Murder of Crows
internal class spell_hun_a_murder_of_crows : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.AMurderOfCrowsDamage, HunterSpells.AMurderOfCrowsVisual1, HunterSpells.AMurderOfCrowsVisual2, HunterSpells.AMurderOfCrowsVisual3);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
		AuraEffects.Add(new AuraEffectApplyHandler(RemoveEffect, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleDummyTick(AuraEffect aurEff)
	{
		var target = GetTarget();
		var caster = GetCaster();

		caster?.CastSpell(target, HunterSpells.AMurderOfCrowsDamage, true);

		target.CastSpell(target, HunterSpells.AMurderOfCrowsVisual1, true);
		target.CastSpell(target, HunterSpells.AMurderOfCrowsVisual2, true);
		target.CastSpell(target, HunterSpells.AMurderOfCrowsVisual3, true);
		target.CastSpell(target, HunterSpells.AMurderOfCrowsVisual3, true); // not a mistake, it is intended to cast twice
	}

	private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death)
		{
			var caster = GetCaster();

			caster?.GetSpellHistory().ResetCooldown(GetId(), true);
		}
	}
}