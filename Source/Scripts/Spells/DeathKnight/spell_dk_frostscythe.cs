// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207230)]
public class spell_dk_frostscythe : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(uint UnnamedParameter)
	{
		if (GetCaster().HasAura(DeathKnightSpells.SPELL_DK_INEXORABLE_ASSAULT_STACK))
			GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.SPELL_DK_INEXORABLE_ASSAULT_DAMAGE, true);

		if (GetCaster().HasAura(DeathKnightSpells.SPELL_DK_KILLING_MACHINE))
		{
			GetCaster().RemoveAura(DeathKnightSpells.SPELL_DK_KILLING_MACHINE);
			SetHitDamage(GetHitDamage() * 4);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}
}