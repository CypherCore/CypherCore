using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_darkflight : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		GetCaster().CastSpell(GetCaster(), GenericSpellIds.AlteredForm, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}