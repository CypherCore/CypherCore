// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 37594 - Greater Heal Refund
internal class spell_pri_t5_heal_2p_bonus : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.ItemEfficiency);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var healInfo = eventInfo.GetHealInfo();

		if (healInfo != null)
		{
			var healTarget = healInfo.GetTarget();

			if (healTarget)
				// @todo: fix me later if (healInfo.GetEffectiveHeal())
				if (healTarget.GetHealth() >= healTarget.GetMaxHealth())
					return true;
		}

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		GetTarget().CastSpell(GetTarget(), PriestSpells.ItemEfficiency, new CastSpellExtraArgs(aurEff));
	}
}