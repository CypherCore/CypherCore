// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116095)]
public class spell_monk_disable : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.DISABLE, MonkSpells.DISABLE_ROOT);
	}

	private void OnHitTarget(int effIndex)
	{
		var target = GetExplTargetUnit();

		if (target != null)
			if (target.HasAuraType(AuraType.ModDecreaseSpeed))
				GetCaster().CastSpell(target, MonkSpells.DISABLE_ROOT, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnHitTarget, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
	}
}