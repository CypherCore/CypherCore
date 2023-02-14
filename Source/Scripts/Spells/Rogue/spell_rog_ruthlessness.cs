// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 14161 - Ruthlessness
internal class spell_rog_ruthlessness : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		var target = GetTarget();

		var cost = procInfo.GetProcSpell()?.GetPowerTypeCostAmount(PowerType.ComboPoints);

		if (cost.HasValue)
			if (RandomHelper.randChance(aurEff.GetSpellEffectInfo().PointsPerResource * (cost.Value)))
				target.ModifyPower(PowerType.ComboPoints, 1);
	}
}