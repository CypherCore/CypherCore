// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 121916 - Glyph of the Geist (Unholy)
internal class spell_dk_pet_geist_transform : SpellScript, ISpellCheckCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.GlyphOfTheGeist);
	}

	public override bool Load()
	{
		return GetCaster().IsPet();
	}

	public SpellCastResult CheckCast()
	{
		var owner = GetCaster().GetOwner();

		if (owner)
			if (owner.HasAura(DeathKnightSpells.GlyphOfTheGeist))
				return SpellCastResult.SpellCastOk;

		return SpellCastResult.SpellUnavailable;
	}
}