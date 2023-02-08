using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 109304 - Exhilaration
internal class spell_hun_exhilaration : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.ExhilarationR2, HunterSpells.Lonewolf);
	}

	public void OnHit()
	{
		if (GetCaster().HasAura(HunterSpells.ExhilarationR2) && !GetCaster().HasAura(HunterSpells.Lonewolf))
			GetCaster().CastSpell(null, HunterSpells.ExhilarationPet, true);
	}
}