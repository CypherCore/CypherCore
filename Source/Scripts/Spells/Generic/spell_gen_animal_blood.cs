using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 46221 - Animal Blood
internal class spell_gen_animal_blood : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.SpawnBloodPool);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// Remove all Auras with spell Id 46221, except the one currently being applied
		Aura aur;

		while ((aur = GetUnitOwner().GetOwnedAura(GenericSpellIds.AnimalBlood, ObjectGuid.Empty, ObjectGuid.Empty, 0, GetAura())) != null)
			GetUnitOwner().RemoveOwnedAura(aur);
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit owner = GetUnitOwner();

		if (owner)
			owner.CastSpell(owner, GenericSpellIds.SpawnBloodPool, true);
	}
}