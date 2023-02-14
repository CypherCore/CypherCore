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
	private const int SPELL_DRUID_REJUVENATION = 774;
	private const int SPELL_DRUID_WILD_GROWTH = 48438;
	private const int SPELL_DRUID_LIFE_BLOOM = 33763;
	private const int SPELL_DRUID_REGROWTH = 8936;

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				caster.AddAura(SPELL_DRUID_REJUVENATION, target);
				caster.AddAura(SPELL_DRUID_WILD_GROWTH, target);
				caster.AddAura(SPELL_DRUID_LIFE_BLOOM, target);
				caster.AddAura(SPELL_DRUID_REGROWTH, target);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}