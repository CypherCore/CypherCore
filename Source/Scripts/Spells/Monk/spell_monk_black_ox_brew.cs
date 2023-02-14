// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115399)]
public class spell_monk_black_ox_brew : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.GetSpellHistory().ResetCharges(Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_PURIFYING_BREW, Difficulty.None).ChargeCategoryId);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
	}
}