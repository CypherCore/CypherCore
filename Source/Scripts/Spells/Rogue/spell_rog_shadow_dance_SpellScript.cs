// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(185313)]
public class spell_rog_shadow_dance_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.HasAura(RogueSpells.MASTER_OF_SHADOWS))
			caster.ModifyPower(PowerType.Energy, +30);

		caster.CastSpell(caster, RogueSpells.SHADOW_DANCE_AURA, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHit));
	}
}