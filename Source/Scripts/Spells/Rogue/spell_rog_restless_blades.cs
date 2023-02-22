// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 79096 - Restless Blades
internal class spell_rog_restless_blades : AuraScript, IHasAuraEffects
{
	private static readonly uint[] Spells =
	{
		RogueSpells.AdrenalineRush, RogueSpells.BetweenTheEyes, RogueSpells.Sprint, RogueSpells.GrapplingHook, RogueSpells.Vanish, RogueSpells.KillingSpree, RogueSpells.MarkedForDeath, RogueSpells.DeathFromAbove
	};

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(Spells);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		var spentCP = procInfo.GetProcSpell()?.GetPowerTypeCostAmount(PowerType.ComboPoints);

		if (spentCP.HasValue)
		{
			var cdExtra = (int)-((double)(aurEff.GetAmount() * spentCP.Value) * 0.1f);

			var history = GetTarget().GetSpellHistory();

			foreach (var spellId in Spells)
				history.ModifyCooldown(spellId, TimeSpan.FromSeconds(cdExtra), true);
		}
	}
}