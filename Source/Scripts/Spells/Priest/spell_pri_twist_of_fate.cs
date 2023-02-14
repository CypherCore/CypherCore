// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 265259 - Twist of Fate (Discipline)
internal class spell_pri_twist_of_fate : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.ProcTriggerSpell));
	}

	private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		return eventInfo.GetProcTarget().GetHealthPct() < aurEff.GetAmount();
	}
}