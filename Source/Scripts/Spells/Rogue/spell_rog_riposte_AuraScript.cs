// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(199754)]
public class spell_rog_riposte_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo procInfo)
	{
		PreventDefaultAction();

		var caster = GetCaster();

		if (caster == null)
			return;

		var target = procInfo.GetActionTarget();

		if (target == null)
			return;

		caster.CastSpell(target, RogueSpells.RIPOSTE_DAMAGE, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}