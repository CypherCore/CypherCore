﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 41213, 43416, 69222, 73076 - Throw Shield
internal class spell_gen_throw_shield : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
	}
}