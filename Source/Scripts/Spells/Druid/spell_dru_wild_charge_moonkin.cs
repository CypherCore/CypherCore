using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102383)]
public class spell_dru_wild_charge_moonkin : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (GetCaster())
		{
			if (!GetCaster().IsInCombat())
				return SpellCastResult.DontReport;
		}
		else
		{
			return SpellCastResult.DontReport;
		}

		return SpellCastResult.SpellCastOk;
	}
}