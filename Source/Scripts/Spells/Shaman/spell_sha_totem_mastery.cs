// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	//210643 Totem Mastery
	[SpellScript(210643)]
	public class spell_sha_totem_mastery : SpellScript, ISpellOnCast
	{
		public void OnCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var player = caster.ToPlayer();

			if (player == null)
				return;

			//Unsummon any Resonance Totem that the player already has. ID : 102392
			var totemResoList = player.GetCreatureListWithEntryInGrid(102392, 500.0f);

			totemResoList.RemoveAll(creature =>
			                        {
				                        var owner = creature.GetOwner();

				                        return owner == null || owner != player || !creature.IsSummon();
			                        });

			if ((int)totemResoList.Count > 0)
				totemResoList.Last().ToTempSummon().UnSummon();

			//Unsummon any Storm Totem that the player already has. ID : 106317
			var totemStormList = player.GetCreatureListWithEntryInGrid(106317, 500.0f);

			totemStormList.RemoveAll(creature =>
			                         {
				                         var owner = creature.GetOwner();

				                         return owner == null || owner != player || !creature.IsSummon();
			                         });

			if ((int)totemStormList.Count > 0)
				totemStormList.Last().ToTempSummon().UnSummon();

			//Unsummon any Ember Totem that the player already has. ID : 106319
			var totemEmberList = player.GetCreatureListWithEntryInGrid(106319, 500.0f);

			totemEmberList.RemoveAll(creature =>
			                         {
				                         var owner = creature.GetOwner();

				                         return owner == null || owner != player || !creature.IsSummon();
			                         });

			if ((int)totemEmberList.Count > 0)
				totemEmberList.Last().ToTempSummon().UnSummon();

			//Unsummon any Tailwind Totem that the player already has. ID : 106321
			var totemTailwindList = player.GetCreatureListWithEntryInGrid(106321, 500.0f);

			totemTailwindList.RemoveAll(creature =>
			                            {
				                            var owner = creature.GetOwner();

				                            return owner == null || owner != player || !creature.IsSummon();
			                            });

			if ((int)totemTailwindList.Count > 0)
				totemTailwindList.Last().ToTempSummon().UnSummon();
		}
	}
}