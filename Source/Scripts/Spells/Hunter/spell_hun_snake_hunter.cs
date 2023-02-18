// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(201078)]
public class spell_hun_snake_hunter : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(HunterSpells.MONGOOSE_BITE, Difficulty.None) != null)
			return false;

		return true;
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		var player = GetCaster().ToPlayer();

		if (player == null)
			return;

		player.GetSpellHistory().ResetCharges(Global.SpellMgr.GetSpellInfo(HunterSpells.MONGOOSE_BITE, Difficulty.None).ChargeCategoryId);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}