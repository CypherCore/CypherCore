// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script]
internal class spell_hun_misdirection : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.MisdirectionProc);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Default ||
		    GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Interrupt)
			return;

		if (!GetTarget().HasAura(HunterSpells.MisdirectionProc))
			GetTarget().GetThreatManager().UnregisterRedirectThreat(HunterSpells.Misdirection);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		GetTarget().CastSpell(GetTarget(), HunterSpells.MisdirectionProc, new CastSpellExtraArgs(aurEff));
	}
}