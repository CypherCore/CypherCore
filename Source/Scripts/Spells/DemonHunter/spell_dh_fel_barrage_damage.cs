// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(211052)]
public class spell_dh_fel_barrage_damage : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var chargesUsed = GetSpellValue().EffectBasePoints[0];
		var dmg         = GetHitDamage();
		SetHitDamage((double)(dmg * chargesUsed) / 5.0f);
	}
}