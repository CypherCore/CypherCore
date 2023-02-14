// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(13877)]
public class spell_rogue_blade_flurry_SpellScript : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		if (GetHitUnit() == GetExplTargetUnit())
			SetHitDamage(0);
	}
}