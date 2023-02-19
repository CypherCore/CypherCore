// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 127517 - Army Transform
internal class spell_dk_army_transform : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.GlyphOfFoulMenagerie);
	}

	public override bool Load()
	{
		return GetCaster().IsGuardian();
	}

	public SpellCastResult CheckCast()
	{
		var owner = GetCaster().GetOwner();

		if (owner)
			if (owner.HasAura(DeathKnightSpells.GlyphOfFoulMenagerie))
				return SpellCastResult.SpellCastOk;

		return SpellCastResult.SpellUnavailable;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(int effIndex)
	{
		GetCaster().CastSpell(GetCaster(), DeathKnightSpells.ArmyTransforms.SelectRandom(), true);
	}
}