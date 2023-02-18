// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(69369)]
public class spell_dru_predatory_swiftness_aura : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
			if (player.HasAura(PredatorySwiftnessSpells.PREDATORY_SWIFTNESS_AURA))
				player.RemoveAura(PredatorySwiftnessSpells.PREDATORY_SWIFTNESS_AURA);
	}
}