using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_allow_cast_from_item_only : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		if (!GetCastItem())
			return SpellCastResult.CantDoThatRightNow;

		return SpellCastResult.SpellCastOk;
	}
}