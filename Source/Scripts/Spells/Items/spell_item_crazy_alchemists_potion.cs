// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 53750 - Crazy Alchemist's Potion (40077)
internal class spell_item_crazy_alchemists_potion : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		List<uint> availableElixirs = new()
		                              {
			                              43185, // Runic Healing Potion (33447)
			                              53750, // Crazy Alchemist's Potion (40077)
			                              53761, // Powerful Rejuvenation Potion (40087)
			                              53762, // Indestructible Potion (40093)
			                              53908, // Potion of Speed (40211)
			                              53909, // Potion of Wild Magic (40212)
			                              53910, // Mighty Arcane Protection Potion (40213)
			                              53911, // Mighty Fire Protection Potion (40214)
			                              53913, // Mighty Frost Protection Potion (40215)
			                              53914, // Mighty Nature Protection Potion (40216)
			                              53915  // Mighty Shadow Protection Potion (40217)
		                              };

		var target = GetCaster();

		if (!target.IsInCombat())
			availableElixirs.Add(53753); // Potion of Nightmares (40081)

		if (target.GetPowerType() == PowerType.Mana)
			availableElixirs.Add(43186); // Runic Mana Potion(33448)

		var chosenElixir = availableElixirs.SelectRandom();

		target.CastSpell(target, chosenElixir, new CastSpellExtraArgs(GetCastItem()));
	}
}