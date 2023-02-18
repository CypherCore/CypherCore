// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201472)]
public class spell_dh_artifact_rage_of_the_illidari : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();

		if (caster == null || eventInfo.GetDamageInfo() != null)
			return;

		var damage = MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetSpellEffectInfo().BasePoints);

		if (damage == 0)
			return;

		// damage += caster->VariableStorage.GetValue<int32>("Spells.RageOfTheIllidariDamage");

		//  caster->VariableStorage.Set("Spells.RageOfTheIllidariDamage", damage);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}