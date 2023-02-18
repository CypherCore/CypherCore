// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	//197995
	[SpellScript(197995)]
	public class spell_sha_wellspring : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			caster.CastSpell(target, ShamanSpells.WELLSPRING_MISSILE, true);
		}
	}
}