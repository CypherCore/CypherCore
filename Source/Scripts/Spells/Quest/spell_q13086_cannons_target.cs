// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 57412 - Reckoning Bomb
internal class spell_q13086_cannons_target : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return !spellInfo.GetEffects().Empty() && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleEffectDummy(uint effIndex)
	{
		var pos = GetExplTargetDest();

		if (pos != null)
			GetCaster().CastSpell(pos.GetPosition(), (uint)GetEffectValue(), new CastSpellExtraArgs(true));
	}
}