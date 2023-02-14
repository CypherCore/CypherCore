// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 71564 - Deadly Precision
internal class spell_item_deadly_precision : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleStackDrop, 0, AuraType.ModRating, AuraScriptHookType.EffectProc));
	}

	private void HandleStackDrop(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		GetTarget().RemoveAuraFromStack(GetId(), GetTarget().GetGUID());
	}
}