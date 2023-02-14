// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 171017 - Meteor Strike
	[SpellScript(171017)]
	public class spell_warlock_infernal_meteor_strike : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var player = caster.GetCharmerOrOwnerPlayerOrPlayerItself();

			if (player != null)
				if (player.HasAura(WarlockSpells.LORD_OF_THE_FLAMES) && !player.HasAura(WarlockSpells.LORD_OF_THE_FLAMES_CD))
				{
					for (uint i = 0; i < 3; ++i)
						player.CastSpell(caster, WarlockSpells.LORD_OF_THE_FLAMES_SUMMON, true);

					player.CastSpell(player, WarlockSpells.LORD_OF_THE_FLAMES_CD, true);
				}
		}
	}
}