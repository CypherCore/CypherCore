// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(215969)]
public class spell_dk_epidemic_aoe : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void FilterTargets(List<WorldObject> targets)
	{
		var target = GetHitUnit();

		if (target != null)
		{
			var caster = GetCaster();

			if (caster != null)
				if (caster.GetDistance2d(target) > 30.0f)
					targets.Remove(target);
		}

		if (targets.Count > 7)
			targets.Resize(7);
	}

	private void HandleOnHitMain(int effIndex)
	{
		var target = GetHitUnit();

		if (target != null)
			explicitTarget = target.GetGUID();
	}

	private void HandleOnHitAOE(int effIndex)
	{
		var target = GetHitUnit();

		if (target != null)
			if (target.GetGUID() == explicitTarget)
				PreventHitDamage();
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleOnHitMain, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleOnHitAOE, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private ObjectGuid explicitTarget = new();
}