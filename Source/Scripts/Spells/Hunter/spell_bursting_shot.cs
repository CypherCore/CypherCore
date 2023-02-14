// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(186387)]
public class spell_bursting_shot : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var caster = GetCaster();

		if (caster != null)
			caster.CastSpell(GetHitUnit(), HunterSpells.SPELL_HUNTER_AURA_SHOOTING, true);
	}
}