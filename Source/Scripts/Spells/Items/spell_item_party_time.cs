// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_party_time : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
	}

	private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var player = GetOwner().ToPlayer();

		if (player == null)
			return;

		player.m_Events.AddEventAtOffset(new PartyTimeEmoteEvent(player), TimeSpan.FromSeconds(RandomHelper.RAND(5, 10, 15)));
	}
}