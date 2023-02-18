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
			                (uint)TrueBearingIDs.SPELL_ROGUE_BETWEEN_THE_EYES,
			                (uint)RogueSpells.SPELL_ROGUE_ROLL_THE_BONES,
			                (uint)RogueSpells.SPELL_ROGUE_EVISCERATE
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
			               (uint)RogueSpells.SPELL_ROGUE_ADRENALINE_RUSH,
			               (uint)RogueSpells.SPELL_ROGUE_SPRINT,
			               (uint)TrueBearingIDs.SPELL_ROGUE_BETWEEN_THE_EYES,
			               (uint)TrueBearingIDs.SPELL_ROGUE_VANISH,
			               (uint)TrueBearingIDs.SPELL_ROGUE_BLIND,
			               (uint)TrueBearingIDs.SPELL_ROGUE_CLOAK_OF_SHADOWS,
			               (uint)TrueBearingIDs.SPELL_ROGUE_RIPOSTE,
			               (uint)TrueBearingIDs.SPELL_ROGUE_GRAPPLING_HOOK,
			               (uint)RogueSpells.SPELL_ROGUE_KILLING_SPREE,
			               (uint)TrueBearingIDs.SPELL_ROGUE_MARKED_FOR_DEATH,
			               (uint)TrueBearingIDs.SPELL_ROGUE_DEATH_FROM_ABOVE
		               };

		foreach (var spell in spellIds)
			caster.GetSpellHistory().ModifyCooldown(spell, TimeSpan.FromSeconds(-2000 * cp));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
	}
}