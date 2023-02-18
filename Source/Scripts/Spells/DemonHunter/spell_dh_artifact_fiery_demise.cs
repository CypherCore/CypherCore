// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(212817)]
public class spell_dh_artifact_fiery_demise : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();
		var target = eventInfo.GetActionTarget();

		if (caster == null || target == null || !caster.IsValidAttackTarget(target))
			return;

		caster.CastSpell(target, ShatteredSoulsSpells.FIERY_DEMISE_DEBUFF, aurEff.GetAmount());
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}