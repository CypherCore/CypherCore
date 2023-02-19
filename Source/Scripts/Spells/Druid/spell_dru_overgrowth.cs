// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(203651)]
public class spell_dru_overgrowth : SpellScript, IHasSpellEffects
{
	private const int REJUVENATION = 774;
	private const int WILD_GROWTH = 48438;
	private const int LIFE_BLOOM = 33763;
	private const int REGROWTH = 8936;

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(int effIndex)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				caster.AddAura(REJUVENATION, target);
				caster.AddAura(WILD_GROWTH, target);
				caster.AddAura(LIFE_BLOOM, target);
				caster.AddAura(REGROWTH, target);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}