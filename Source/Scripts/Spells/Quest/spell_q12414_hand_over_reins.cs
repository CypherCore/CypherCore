// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 49285 - Hand Over Reins
internal class spell_q12414_hand_over_reins : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		var caster = GetCaster().ToCreature();
		GetHitUnit().ExitVehicle();

		if (caster)
			caster.DespawnOrUnsummon();
	}
}