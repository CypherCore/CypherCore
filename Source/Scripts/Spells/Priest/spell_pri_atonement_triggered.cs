// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
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
		var caster = GetCaster();

		if (caster)
		{
			var atonement = caster.GetAura(PriestSpells.Atonement);

			if (atonement != null)
			{
				var script = atonement.GetScript<spell_pri_atonement>();

				script?.AddAtonementTarget(GetTarget().GetGUID());
			}
		}
	}

	private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = GetCaster();

		if (caster)
		{
			var atonement = caster.GetAura(PriestSpells.Atonement);

			if (atonement != null)
			{
				var script = atonement.GetScript<spell_pri_atonement>();

				script?.RemoveAtonementTarget(GetTarget().GetGUID());
			}
		}
	}
}