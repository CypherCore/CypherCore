// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(78203)]
public class spell_pri_shadowy_apparitions : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_SHADOWY_APPARITION_MISSILE, PriestSpells.SPELL_PRIEST_SHADOW_WORD_PAIN);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == PriestSpells.SPELL_PRIEST_SHADOW_WORD_PAIN)
			if ((eventInfo.GetHitMask() & ProcFlagsHit.Critical) != 0)
				return true;

		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		if (GetTarget() && eventInfo.GetActionTarget())
		{
			GetTarget().CastSpell(eventInfo.GetActionTarget(), PriestSpells.SPELL_PRIEST_SHADOWY_APPARITION_MISSILE, true);
			GetTarget().SendPlaySpellVisual(eventInfo.GetActionTarget().GetPosition(), GetCaster().GetOrientation(), MiscSpells.SPELL_VISUAL_SHADOWY_APPARITION, 0, 0, MiscSpells.SHADOWY_APPARITION_TRAVEL_SPEED, false);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}