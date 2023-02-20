// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 92833 - Leap of Faith
internal class spell_pri_leap_of_faith_effect_trigger : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.LEAP_OF_FAITH_EFFECT);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleEffectDummy(int effIndex)
	{
		var destPos = GetHitDest().GetPosition();

		SpellCastTargets targets = new();
		targets.SetDst(destPos);
		targets.SetUnitTarget(GetCaster());
		GetHitUnit().CastSpell(targets, (uint)GetEffectValue(), new CastSpellExtraArgs(GetCastDifficulty()));
	}
}