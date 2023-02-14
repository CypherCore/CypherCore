// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	[Script] // 710 - Banish
	internal class spell_warl_banish : SpellScript, ISpellBeforeHit
	{
		public void BeforeHit(SpellMissInfo missInfo)
		{
			if (missInfo != SpellMissInfo.Immune)
				return;

			var target = GetHitUnit();

			if (target)
			{
				// Casting Banish on a banished Target will Remove applied aura
				var banishAura = target.GetAura(GetSpellInfo().Id, GetCaster().GetGUID());

				banishAura?.Remove();
			}
		}
	}
}