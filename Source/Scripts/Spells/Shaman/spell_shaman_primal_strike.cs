// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	//73899
	[SpellScript(73899)]
	public class spell_shaman_primal_strike : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var player = GetCaster().ToPlayer();

			if (player != null)
				GetHitUnit().CastSpell(GetHitUnit(), 73899, (int)(player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.34f));
		}
	}
}