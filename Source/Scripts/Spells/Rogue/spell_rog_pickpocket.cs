using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[Script]
internal class spell_rog_pickpocket : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (!GetExplTargetUnit() ||
		    !GetCaster().IsValidAttackTarget(GetExplTargetUnit(), GetSpellInfo()))
			return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}
}