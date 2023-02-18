// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(212105)]
public class spell_dh_fel_devastation_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private bool firstHit = true;

	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (firstHit)
		{
			firstHit = false;
			caster.CastSpell(caster, DemonHunterSpells.FEL_DEVASTATION_HEAL, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}