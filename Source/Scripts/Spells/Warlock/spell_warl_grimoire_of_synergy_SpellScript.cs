// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Grimoire of Synergy - 171975
	[SpellScript(171975, "spell_warl_grimoire_of_synergy")]
	public class spell_warl_grimoire_of_synergy_SpellScript : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var player = caster.ToPlayer();

			if (caster.ToPlayer())
			{
				var pet = player.GetGuardianPet();
				player.AddAura(GetSpellInfo().Id, player);

				if (pet != null)
					player.AddAura(GetSpellInfo().Id, pet);
			}
		}
	}
}