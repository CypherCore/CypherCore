// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(527)]
public class spell_pri_purify : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (caster != target && target.IsFriendlyTo(caster))
			return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}

	private void AfterEffectHit(int effIndex)
	{
		if (GetHitUnit().IsFriendlyTo(GetCaster()))
		{
			GetCaster().CastSpell(GetHitUnit(), PriestSpells.DISPEL_MAGIC_HOSTILE, true);
			GetCaster().CastSpell(GetHitUnit(), PriestSpells.CURE_DISEASE, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(AfterEffectHit, 0, SpellEffectName.Dispel, SpellScriptHookType.EffectHitTarget));
	}
}