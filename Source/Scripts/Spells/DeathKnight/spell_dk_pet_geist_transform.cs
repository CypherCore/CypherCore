using Framework.Constants;
using Game.Entities;
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
		Unit owner = GetCaster().GetOwner();

		if (owner)
			if (owner.HasAura(DeathKnightSpells.GlyphOfTheGeist))
				return SpellCastResult.SpellCastOk;

		return SpellCastResult.SpellUnavailable;
	}
}