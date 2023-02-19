// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(369536)]
public class spell_soar : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(EvokerSpells.SOAR_RACIAL, EvokerSpells.SKYWARD_ASCENT, EvokerSpells.SURGE_FORWARD);
	}

	private void HandleOnHit(int effIndex)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		// Increase flight speed by 830540%
		caster.SetSpeedRate(UnitMoveType.Flight, 83054.0f);

		var player = GetHitPlayer();
		// Add "Skyward Ascent" and "Surge Forward" to the caster's spellbook
		player.LearnSpell(EvokerSpells.SKYWARD_ASCENT, false);
		player.LearnSpell(EvokerSpells.SURGE_FORWARD, false);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}