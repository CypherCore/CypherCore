// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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