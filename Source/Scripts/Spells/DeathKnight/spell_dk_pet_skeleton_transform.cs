using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 147157 Glyph of the Skeleton (Unholy)
internal class spell_dk_pet_skeleton_transform : SpellScript, ISpellCheckCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.GlyphOfTheSkeleton);
	}

	public SpellCastResult CheckCast()
	{
		Unit owner = GetCaster().GetOwner();

		if (owner)
			if (owner.HasAura(DeathKnightSpells.GlyphOfTheSkeleton))
				return SpellCastResult.SpellCastOk;

		return SpellCastResult.SpellUnavailable;
	}
}