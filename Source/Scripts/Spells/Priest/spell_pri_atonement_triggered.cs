using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 194384, 214206 - Atonement
internal class spell_pri_atonement_triggered : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.Atonement);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit caster = GetCaster();

		if (caster)
		{
			Aura atonement = caster.GetAura(PriestSpells.Atonement);

			if (atonement != null)
			{
				var script = atonement.GetScript<spell_pri_atonement>();

				script?.AddAtonementTarget(GetTarget().GetGUID());
			}
		}
	}

	private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit caster = GetCaster();

		if (caster)
		{
			Aura atonement = caster.GetAura(PriestSpells.Atonement);

			if (atonement != null)
			{
				var script = atonement.GetScript<spell_pri_atonement>();

				script?.RemoveAtonementTarget(GetTarget().GetGUID());
			}
		}
	}
}