// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// 215864 Rainfall
	[SpellScript(215864)]
	public class spell_sha_rainfall_SpellScript : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var pos = GetHitDest();

			if (pos != null)
				GetCaster().SummonCreature(ShamanNpcs.NPC_RAINFALL, pos);
		}
	}
}