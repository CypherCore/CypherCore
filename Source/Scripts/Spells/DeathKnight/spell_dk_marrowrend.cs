using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(195182)]
public class spell_dk_marrowrend : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			caster.CastSpell(null, DeathKnightSpells.SPELL_DK_BONE_SHIELD, true);
			var boneShield = caster.GetAura(DeathKnightSpells.SPELL_DK_BONE_SHIELD);

			if (boneShield != null)
				boneShield.SetStackAmount(3);
		}
	}
}