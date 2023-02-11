﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(19577)]
public class spell_hun_intimidation : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleDummy(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = caster.ToPlayer().GetSelectedUnit();

		if (caster == null || target == null)
			return;

		caster.CastSpell(target, HunterSpells.SPELL_HUNTER_INTIMIDATION_STUN, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}