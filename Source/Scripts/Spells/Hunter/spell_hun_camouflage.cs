using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(199483)]
public class spell_hun_camouflage : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (GetCaster() && GetCaster().IsPlayer())
		{
			Unit pet = GetCaster().GetGuardianPet();

			if (pet != null)
				pet.CastSpell(pet, HunterSpells.SPELL_HUNTER_CAMOUFLAGE, true);
		}
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (GetCaster() && GetCaster().IsPlayer())
		{
			Unit pet = GetCaster().GetGuardianPet();

			if (pet != null)
				pet.RemoveAurasDueToSpell(HunterSpells.SPELL_HUNTER_CAMOUFLAGE);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModStealth, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModStealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}