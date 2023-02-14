// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 45051 - Mad Alchemist's Potion (34440)
internal class spell_item_mad_alchemists_potion : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		List<uint> availableElixirs = new()
		                              {
			                              // Battle Elixirs
			                              33720, // Onslaught Elixir (28102)
			                              54452, // Adept's Elixir (28103)
			                              33726, // Elixir of Mastery (28104)
			                              28490, // Elixir of Major Strength (22824)
			                              28491, // Elixir of Healing Power (22825)
			                              28493, // Elixir of Major Frost Power (22827)
			                              54494, // Elixir of Major Agility (22831)
			                              28501, // Elixir of Major Firepower (22833)
			                              28503, // Elixir of Major Shadow Power (22835)
			                              38954, // Fel Strength Elixir (31679)
			                              // Guardian Elixirs
			                              39625, // Elixir of Major Fortitude (32062)
			                              39626, // Earthen Elixir (32063)
			                              39627, // Elixir of Draenic Wisdom (32067)
			                              39628, // Elixir of Ironskin (32068)
			                              28502, // Elixir of Major Defense (22834)
			                              28514, // Elixir of Empowerment (22848)
			                              // Other
			                              28489, // Elixir of Camouflage (22823)
			                              28496  // Elixir of the Searching Eye (22830)
		                              };

		var target = GetCaster();

		if (target.GetPowerType() == PowerType.Mana)
			availableElixirs.Add(28509); // Elixir of Major Mageblood (22840)

		var chosenElixir = availableElixirs.SelectRandom();

		var useElixir = true;

		var chosenSpellGroup = SpellGroup.None;

		if (Global.SpellMgr.IsSpellMemberOfSpellGroup(chosenElixir, SpellGroup.ElixirBattle))
			chosenSpellGroup = SpellGroup.ElixirBattle;

		if (Global.SpellMgr.IsSpellMemberOfSpellGroup(chosenElixir, SpellGroup.ElixirGuardian))
			chosenSpellGroup = SpellGroup.ElixirGuardian;

		// If another spell of the same group is already active the elixir should not be cast
		if (chosenSpellGroup != 0)
		{
			var Auras = target.GetAppliedAuras();

			foreach (var pair in Auras.KeyValueList)
			{
				var spell_id = pair.Value.GetBase().GetId();

				if (Global.SpellMgr.IsSpellMemberOfSpellGroup(spell_id, chosenSpellGroup) &&
				    spell_id != chosenElixir)
				{
					useElixir = false;

					break;
				}
			}
		}

		if (useElixir)
			target.CastSpell(target, chosenElixir, new CastSpellExtraArgs(GetCastItem()));
	}
}