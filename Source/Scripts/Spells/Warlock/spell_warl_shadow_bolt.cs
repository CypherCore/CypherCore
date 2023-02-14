// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(686)] // 686 - Shadow Bolt
	internal class spell_warl_shadow_bolt : SpellScript, ISpellAfterCast
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarlockSpells.SHADOW_BOLT_SHOULSHARD);
		}

		public void AfterCast()
		{
			GetCaster().CastSpell(GetCaster(), WarlockSpells.SHADOW_BOLT_SHOULSHARD, true);
		}
	}
}