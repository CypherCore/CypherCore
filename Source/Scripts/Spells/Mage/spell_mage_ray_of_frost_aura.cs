using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
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
		Unit caster = GetCaster();

		if (caster != null)
			if (aurEff.GetTickNumber() > 1) // First tick should deal base Damage
				caster.CastSpell(caster, MageSpells.RayOfFrostBonus, true);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit caster = GetCaster();

		caster?.RemoveAurasDueToSpell(MageSpells.RayOfFrostFingersOfFrost);
	}
}