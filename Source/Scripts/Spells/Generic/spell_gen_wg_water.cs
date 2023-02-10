using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_wg_water : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (!GetSpellInfo().CheckTargetCreatureType(GetCaster()))
			return SpellCastResult.DontReport;

		return SpellCastResult.SpellCastOk;
	}
}