// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[Script] // 57934 - Tricks of the Trade
internal class spell_rog_tricks_of_the_trade : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var aura = GetHitAura();

		if (aura != null)
		{
			var script = aura.GetScript<spell_rog_tricks_of_the_trade_aura>();

			if (script != null)
			{
				var explTarget = GetExplTargetUnit();

				if (explTarget != null)
					script.SetRedirectTarget(explTarget.GetGUID());
				else
					script.SetRedirectTarget(ObjectGuid.Empty);
			}
		}
	}
}