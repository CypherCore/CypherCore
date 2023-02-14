// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	[SpellScript(58094)]
	public class spell_warl_glyph_of_soulwell : SpellScript, ISpellAfterCast
	{
		public void AfterCast()
		{
			if (!GetCaster())
				return;

			if (GetExplTargetDest() != null)
				return;

			if (!GetCaster().HasAura(WarlockSpells.GLYPH_OF_SOULWELL))
				return;

			GetCaster().CastSpell(new Position(GetExplTargetDest().GetPositionX(), GetExplTargetDest().GetPositionY(), GetExplTargetDest().GetPositionZ()), WarlockSpells.GLYPH_OF_SOULWELL_VISUAL, true);
		}
	}
}