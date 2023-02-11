﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(61391)]
public class spell_dru_typhoon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleKnockBack(uint effIndex)
	{
		// Glyph of Typhoon
		if (GetCaster().HasAura(DruidSpells.SPELL_DRUID_GLYPH_OF_TYPHOON))
			PreventHitDefaultEffect(effIndex);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleKnockBack, 0, SpellEffectName.KnockBack, SpellScriptHookType.EffectHitTarget));
	}
}