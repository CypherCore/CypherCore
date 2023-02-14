// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(1953)]
public class spell_mage_blink : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (GetCaster().HasAura(MageSpells.SPELL_MAGE_BLAZING_SOUL))
			GetCaster().AddAura(MageSpells.SPELL_MAGE_BLAZING_BARRIER, caster);

		if (GetCaster().HasAura(MageSpells.SPELL_MAGE_PRISMATIC_CLOAK))
			GetCaster().AddAura(MageSpells.SPELL_MAGE_PRISMATIC_CLOAK_BUFF, caster);
	}
}