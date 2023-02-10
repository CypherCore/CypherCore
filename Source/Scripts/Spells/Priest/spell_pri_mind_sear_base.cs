using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(48045)]
public class spell_pri_mind_sear_base : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var explTarget = GetExplTargetUnit();

		if (explTarget != null)
			if (explTarget == GetCaster())
				return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}
}