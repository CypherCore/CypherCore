using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115288)]
public class spell_monk_energizing_brew : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (!GetCaster().IsInCombat())
		{
			return SpellCastResult.CasterAurastate;
		}
		return SpellCastResult.SpellCastOk;
	}
}