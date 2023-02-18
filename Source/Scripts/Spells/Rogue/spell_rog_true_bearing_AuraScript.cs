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

[SpellScript(193359)]
public class spell_rog_true_bearing_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var finishers = new List<uint>()
		                {
			                (uint)TrueBearingIDs.BETWEEN_THE_EYES,
			                (uint)RogueSpells.ROLL_THE_BONES,
			                (uint)RogueSpells.EVISCERATE
		                };

		foreach (var finisher in finishers)
			if (eventInfo.GetSpellInfo().Id == finisher)
				return true;

		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var cp = caster.GetPower(PowerType.ComboPoints) + 1;

		var spellIds = new List<uint>()
		               {
			               (uint)RogueSpells.ADRENALINE_RUSH,
			               (uint)RogueSpells.SPRINT,
			               (uint)TrueBearingIDs.BETWEEN_THE_EYES,
			               (uint)TrueBearingIDs.VANISH,
			               (uint)TrueBearingIDs.BLIND,
			               (uint)TrueBearingIDs.CLOAK_OF_SHADOWS,
			               (uint)TrueBearingIDs.RIPOSTE,
			               (uint)TrueBearingIDs.GRAPPLING_HOOK,
			               (uint)RogueSpells.KILLING_SPREE,
			               (uint)TrueBearingIDs.MARKED_FOR_DEATH,
			               (uint)TrueBearingIDs.DEATH_FROM_ABOVE
		               };

		foreach (var spell in spellIds)
			caster.GetSpellHistory().ModifyCooldown(spell, TimeSpan.FromSeconds(-2000 * cp));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
	}
}