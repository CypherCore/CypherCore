// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(221514)]
public class spell_dru_skull_bash_charge : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleCharge(int effIndex)
	{
		if (!GetCaster())
			return;

		if (!GetHitUnit())
			return;

		GetCaster().CastSpell(GetHitUnit(), 93985, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleCharge, 0, SpellEffectName.Charge, SpellScriptHookType.EffectHitTarget));
	}
}