﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(232893)]
public class spell_dh_felblade : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleOnHit(uint UnnamedParameter)
	{
		if (!GetCaster() || !GetHitUnit())
			return;

		if (GetCaster().GetDistance2d(GetHitUnit()) <= 15.0f)
		{
			GetCaster().CastSpell(GetHitUnit(), DemonHunterSpells.SPELL_DH_FELBLADE_CHARGE, true);
			GetCaster().CastSpell(GetHitUnit(), DemonHunterSpells.SPELL_DH_FELBLADE_DAMAGE, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}