// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 781 - Disengage
internal class spell_hun_posthaste : SpellScript, ISpellAfterCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.PosthasteTalent, HunterSpells.PosthasteIncreaseSpeed);
	}

	public void AfterCast()
	{
		if (GetCaster().HasAura(HunterSpells.PosthasteTalent))
		{
			GetCaster().RemoveMovementImpairingAuras(true);
			GetCaster().CastSpell(GetCaster(), HunterSpells.PosthasteIncreaseSpeed, GetSpell());
		}
	}
}