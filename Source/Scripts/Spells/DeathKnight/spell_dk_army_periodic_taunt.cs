using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

/// 6.x, does this belong here or in spell_generic? apply this in creature_template_addon? sniffs say this is always cast army of the dead ghouls.
[SpellScript(43264)]
public class spell_dk_army_periodic_taunt : SpellScript, ISpellCheckCast
{
	public override bool Load()
	{
		return GetCaster().IsGuardian();
	}

	public SpellCastResult CheckCast()
	{
		var owner = GetCaster().GetOwner();

		if (owner != null)
			if (!owner.HasAura(DeathKnightSpells.SPELL_DK_GLYPH_OF_ARMY_OF_THE_DEAD))
				return SpellCastResult.SpellCastOk;

		return SpellCastResult.SpellUnavailable;
	}
}