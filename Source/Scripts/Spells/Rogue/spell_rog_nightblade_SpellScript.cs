// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(195452)]
public class spell_rog_nightblade_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleLaunch(int effIndex)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		target.RemoveAurasDueToSpell(RogueSpells.NIGHTBLADE, caster.GetGUID());
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleLaunch, 0, SpellEffectName.ApplyAura, SpellScriptHookType.LaunchTarget));
	}
}