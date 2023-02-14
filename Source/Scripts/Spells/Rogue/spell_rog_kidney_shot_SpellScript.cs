// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(408)]
public class spell_rog_kidney_shot_SpellScript : SpellScript, ISpellAfterHit, ISpellOnTakePower
{
	private int _cp = 0;

	public void TakePower(SpellPowerCost powerCost)
	{
		if (powerCost.Power == PowerType.ComboPoints)
			_cp = powerCost.Amount + 1;
	}

	public void AfterHit()
	{
		var target = GetHitUnit();

		if (target != null)
		{
			var aura = target.GetAura(RogueSpells.SPELL_ROGUE_KIDNEY_SHOT, GetCaster().GetGUID());

			if (aura != null)
				aura.SetDuration(_cp * Time.InMilliseconds);
		}
	}
}