using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_trigger_exclude_caster_aura_spell : SpellScript, ISpellAfterCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(spellInfo.ExcludeCasterAuraSpell);
	}

	public void AfterCast()
	{
		// Blizz seems to just apply aura without bothering to cast
		GetCaster().AddAura(GetSpellInfo().ExcludeCasterAuraSpell, GetCaster());
	}
}