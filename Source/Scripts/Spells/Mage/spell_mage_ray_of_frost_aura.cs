// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script]
internal class spell_mage_ray_of_frost_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.RayOfFrostBonus, MageSpells.RayOfFrostFingersOfFrost);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDamage));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (caster != null)
			if (aurEff.GetTickNumber() > 1) // First tick should deal base Damage
				caster.CastSpell(caster, MageSpells.RayOfFrostBonus, true);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = GetCaster();

		caster?.RemoveAura(MageSpells.RayOfFrostFingersOfFrost);
	}
}