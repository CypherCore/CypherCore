// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(49020)]
public class spell_dk_obliterate : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleHit(uint UnnamedParameter)
	{
		GetCaster().RemoveAurasDueToSpell(DeathKnightSpells.KILLING_MACHINE);

		if (GetCaster().HasAura(DeathKnightSpells.ICECAP))
			if (GetCaster().GetSpellHistory().HasCooldown(DeathKnightSpells.PILLAR_OF_FROST))
				GetCaster().GetSpellHistory().ModifyCooldown(DeathKnightSpells.PILLAR_OF_FROST, TimeSpan.FromSeconds(-3000));

		if (GetCaster().HasAura(DeathKnightSpells.INEXORABLE_ASSAULT_STACK))
			GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.INEXORABLE_ASSAULT_DAMAGE, true);

		if (GetCaster().HasAura(DeathKnightSpells.RIME) && RandomHelper.randChance(45))
			GetCaster().CastSpell(null, DeathKnightSpells.RIME_BUFF, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}
}